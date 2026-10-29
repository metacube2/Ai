using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

// Projektstrukturplan fuer SAP PS und Decision Log (Sitzung 2026-10-29). Die Schnittstelle zwischen Projektwerkzeug und
// SAP ist der Projektstrukturplan: je Element Soll-Stunden, Soll-Franken und Kostenart (CAPEX/OPEX). Der Export ist die
// spaetere Bruecke zu PS; geschrieben wird nach SAP nichts.
public sealed partial class PmService
{
    public static class WbsFields
    {
        public const string Hours = "hours";
        public const string Cost = "cost";
        public const string CostType = "costtype";
    }

    // ---------- Projektstrukturplan ----------

    public async Task<PmWbs?> GetWbsAsync(int projectId, PmUser user)
    {
        if (await GetBoardAsync(projectId, user) is null)
            return null;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.AsNoTracking().FirstAsync(p => p.Id == projectId);
        var tasks = await db.PmTasks.AsNoTracking().Where(t => t.ProjectId == projectId && !t.IsArchived).ToListAsync();
        return BuildWbs(project, tasks);
    }

    internal static PmWbs BuildWbs(PmProject project, IReadOnlyList<PmTask> tasks)
    {
        var outline = OutlineOrder(tasks).ToList();
        var parents = OutlineParents(outline);
        var children = outline.ToLookup(x => parents[x.Task.Id], x => x.Task.Id);
        var byId = tasks.ToDictionary(t => t.Id);

        (double Hours, double Cost) Total(int id)
        {
            var kids = children[id].ToList();
            if (kids.Count == 0)
                return (byId[id].PlannedHours, byId[id].PlannedCostChf);
            var sum = kids.Select(Total).Aggregate((0.0, 0.0), (a, b) => (a.Item1 + b.Item1, a.Item2 + b.Item2));
            // Ein Element mit eigenen Werten und Kindern: eigene Werte zaehlen zusaetzlich (z. B. Phasenleitung).
            return (sum.Item1 + byId[id].PlannedHours, sum.Item2 + byId[id].PlannedCostChf);
        }

        var counters = new Dictionary<int, int>();
        var codes = new Dictionary<int, string>();
        var rows = new List<PmWbsRow>();
        foreach (var (task, level) in outline)
        {
            var parent = parents[task.Id];
            var slot = parent ?? 0;
            counters[slot] = counters.GetValueOrDefault(slot) + 1;
            var code = (parent is { } p ? codes[p] : project.Key) + "." + counters[slot].ToString("00", CultureInfo.InvariantCulture);
            codes[task.Id] = code;
            var total = Total(task.Id);
            var start = task.StartDate ?? task.DueDate;
            var end = task.DueDate ?? task.StartDate;
            rows.Add(new PmWbsRow(task.Id, code, level, $"{project.Key}-{task.Number}", task.Type, task.Title,
                children[task.Id].Any(), task.IsMilestone, start, end, task.PlannedHours, task.PlannedCostChf,
                Math.Round(total.Hours, 2), Math.Round(total.Cost, 2), task.CostType, task.AssigneeName));
        }
        var top = rows.Where(r => parents[r.TaskId] is null).ToList();
        // Kostenart: jede Zeile mit eigenen Franken zaehlt einmal, damit Summen nicht doppelt laufen.
        var capex = rows.Where(r => r.CostType == PmCostTypes.Capex).Sum(r => r.OwnCostChf);
        var opex = rows.Where(r => r.CostType == PmCostTypes.Opex).Sum(r => r.OwnCostChf);
        var open = rows.Where(r => r.CostType.Length == 0).Sum(r => r.OwnCostChf);
        return new PmWbs(project.Key, project.Name, rows, Math.Round(top.Sum(r => r.TotalHours), 2), Math.Round(top.Sum(r => r.TotalCostChf), 2),
            Math.Round(capex, 2), Math.Round(opex, 2), Math.Round(open, 2));
    }

    public async Task UpdateWbsFieldAsync(PmUser user, int taskId, string field, string? value)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsArchived) ?? throw NotFound();
        await RequireEditAsync(db, user, task.ProjectId);
        var key = await db.PmProjects.Where(p => p.Id == task.ProjectId).Select(p => p.Key).FirstAsync();
        value = (value ?? string.Empty).Trim();
        string change;
        switch (field)
        {
            case WbsFields.Hours:
            case WbsFields.Cost:
                var amount = 0.0;
                if (value.Length > 0 && !double.TryParse(value.Replace("'", string.Empty).Replace("’", string.Empty).Replace(",", "."),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out amount) || amount < 0 || amount > 1e9)
                    throw new PmException("Bitte eine Zahl von 0 bis 1 Milliarde.", "Please enter a number from 0 to 1 billion.");
                if (field == WbsFields.Hours)
                {
                    task.PlannedHours = Math.Round(amount, 2);
                    change = $"Soll-Stunden {task.PlannedHours:0.##}";
                }
                else
                {
                    task.PlannedCostChf = Math.Round(amount, 2);
                    change = $"Soll-Franken {task.PlannedCostChf:0.##}";
                }
                break;
            case WbsFields.CostType:
                if (value.Length > 0 && !PmCostTypes.All.Contains(value))
                    throw NotFound();
                task.CostType = value;
                change = value.Length == 0 ? "Kostenart entfernt" : $"Kostenart {value.ToUpperInvariant()}";
                break;
            default:
                throw NotFound();
        }
        task.UpdatedAtUtc = DateTime.UtcNow;
        Log(db, task.ProjectId, task.Id, user, $"hat {key}-{task.Number} geändert: {change}");
        await db.SaveChangesAsync();
        _notifier.Publish(task.ProjectId);
    }

    // CSV fuer Excel und als Vorlage fuer SAP PS (Semikolon, UTF-8 mit BOM, Dezimalpunkt).
    public async Task<string> ExportWbsCsvAsync(int projectId, PmUser user)
    {
        var wbs = await GetWbsAsync(projectId, user) ?? throw NotFound();
        return WbsCsv(wbs);
    }

    internal static string WbsCsv(PmWbs wbs)
    {
        static string Cell(string value) => value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
        static string Num(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        var sb = new StringBuilder("﻿");
        sb.AppendLine("PSP-Element;Ebene;Bezeichnung;Typ;Schluessel;Start;Ende;Soll-Stunden;Soll-CHF;Soll-Stunden gesamt;Soll-CHF gesamt;Kostenart;Verantwortlich");
        foreach (var r in wbs.Rows)
        {
            sb.AppendLine(string.Join(";",
                Cell(r.Code), (r.Level + 1).ToString(CultureInfo.InvariantCulture), Cell(r.Title),
                r.IsMilestone ? "Meilenstein" : r.Type, r.Key,
                r.Start?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty,
                r.End?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty,
                Num(r.OwnHours), Num(r.OwnCostChf), Num(r.TotalHours), Num(r.TotalCostChf),
                r.CostType.ToUpperInvariant(), Cell(r.AssigneeName)));
        }
        return sb.ToString();
    }

    // ---------- Decision Log ----------

    public async Task<IReadOnlyList<PmDecisionView>> GetDecisionsAsync(int projectId, PmUser user, bool includeArchived = false)
    {
        if (await GetBoardAsync(projectId, user) is null)
            return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        var key = await db.PmProjects.Where(p => p.Id == projectId).Select(p => p.Key).FirstAsync();
        var decisions = await db.PmDecisions.AsNoTracking().Where(d => d.ProjectId == projectId && (includeArchived || !d.IsArchived)).ToListAsync();
        var taskIds = decisions.Where(d => d.TaskId is not null).Select(d => d.TaskId!.Value).ToList();
        var tasks = await db.PmTasks.AsNoTracking().Where(t => taskIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Number);
        return decisions
            .OrderByDescending(d => d.DecidedOn ?? d.CreatedAtUtc.Date).ThenByDescending(d => d.Number)
            .Select(d => new PmDecisionView(d.Id, d.Number, $"{key}-D{d.Number}", d.Title, d.Decision, d.Rationale, d.Alternatives, d.Status,
                d.DecidedOn, d.DecidedBy, d.TaskId, d.TaskId is { } tid && tasks.TryGetValue(tid, out var n) ? $"{key}-{n}" : string.Empty,
                d.AuthorName, d.UpdatedAtUtc, d.IsArchived))
            .ToList();
    }

    public async Task<int> SaveDecisionAsync(PmUser user, int projectId, PmDecisionInput input)
    {
        RequireUser(user);
        var title = (input.Title ?? string.Empty).Trim();
        if (title.Length is 0 or > TitleMax)
            throw new PmException($"Der Entscheid braucht einen Titel mit 1 bis {TitleMax} Zeichen.", $"The decision needs a title of 1 to {TitleMax} characters.");
        var status = PmDecisionStatuses.All.Contains(input.Status) ? input.Status : PmDecisionStatuses.Proposed;
        if (status == PmDecisionStatuses.Decided && input.DecidedOn is null)
            throw new PmException("Ein gefällter Entscheid braucht ein Datum.", "A decision that was made needs a date.");
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsArchived) ?? throw NotFound();
            await RequireEditAsync(db, user, projectId);
            var now = DateTime.UtcNow;
            PmDecision decision;
            var isNew = input.Id is null;
            if (input.Id is { } id)
            {
                decision = await db.PmDecisions.FirstOrDefaultAsync(d => d.Id == id && d.ProjectId == projectId) ?? throw NotFound();
            }
            else
            {
                var next = (await db.PmDecisions.Where(d => d.ProjectId == projectId).Select(d => (int?)d.Number).MaxAsync() ?? 0) + 1;
                decision = new PmDecision { ProjectId = projectId, Number = next, AuthorLogin = user.Login, AuthorName = user.DisplayName, CreatedAtUtc = now };
                db.PmDecisions.Add(decision);
            }
            decision.Title = title;
            decision.Decision = Limit(input.Decision, DescriptionMax);
            decision.Rationale = Limit(input.Rationale, DescriptionMax);
            decision.Alternatives = Limit(input.Alternatives, DescriptionMax);
            decision.Status = status;
            decision.DecidedOn = input.DecidedOn?.Date;
            decision.DecidedBy = Limit(input.DecidedBy, 200);
            decision.TaskId = input.TaskId is { } tid && await db.PmTasks.AnyAsync(t => t.Id == tid && t.ProjectId == projectId) ? tid : null;
            decision.UpdatedAtUtc = now;
            project.UpdatedAtUtc = now;
            await db.SaveChangesAsync();
            Log(db, projectId, decision.TaskId, user, isNew
                ? $"hat Entscheid {project.Key}-D{decision.Number} erfasst: {decision.Title}"
                : $"hat Entscheid {project.Key}-D{decision.Number} geändert ({DecisionStatusDe(decision.Status)})");
            await db.SaveChangesAsync();
            _notifier.Publish(projectId);
            return decision.Id;
        }
        finally
        {
            WriteGate.Release();
        }
    }

    public async Task SetDecisionArchivedAsync(PmUser user, int decisionId, bool archived)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var decision = await db.PmDecisions.FirstOrDefaultAsync(d => d.Id == decisionId) ?? throw NotFound();
        await RequireEditAsync(db, user, decision.ProjectId);
        decision.IsArchived = archived;
        decision.UpdatedAtUtc = DateTime.UtcNow;
        var key = await db.PmProjects.Where(p => p.Id == decision.ProjectId).Select(p => p.Key).FirstAsync();
        Log(db, decision.ProjectId, decision.TaskId, user, archived ? $"hat Entscheid {key}-D{decision.Number} archiviert" : $"hat Entscheid {key}-D{decision.Number} wiederhergestellt");
        await db.SaveChangesAsync();
        _notifier.Publish(decision.ProjectId);
    }

    private static string DecisionStatusDe(string status) => status switch
    {
        PmDecisionStatuses.Decided => "entschieden",
        PmDecisionStatuses.Revoked => "verworfen",
        PmDecisionStatuses.Superseded => "abgelöst",
        _ => "vorgeschlagen"
    };
}
