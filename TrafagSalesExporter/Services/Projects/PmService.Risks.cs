using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

// Risikoregister je Projekt (2026-10-29, Wunsch Ingo „auch Risikobewertung, Kritikalitaet“). Wahrscheinlichkeit und
// Auswirkung 1 bis 5, Kritikalitaet aus dem Produkt (PmRiskLevels). Aendern duerfen Mitglieder, wie bei Aufgaben.
public sealed partial class PmService
{
    public async Task<IReadOnlyList<PmRiskView>> GetRisksAsync(int projectId, PmUser user, bool includeArchived = false)
    {
        if (await GetBoardAsync(projectId, user) is null)
            return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        var key = await db.PmProjects.Where(p => p.Id == projectId).Select(p => p.Key).FirstAsync();
        var risks = await db.PmRisks.AsNoTracking().Where(r => r.ProjectId == projectId && (includeArchived || !r.IsArchived)).ToListAsync();
        var taskIds = risks.Where(r => r.TaskId is not null).Select(r => r.TaskId!.Value).ToList();
        var tasks = await db.PmTasks.AsNoTracking().Where(t => taskIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        return risks.Select(r => ToView(r, key, r.TaskId is { } id && tasks.TryGetValue(id, out var t) ? t : null))
            .OrderByDescending(r => PmRiskStatuses.IsActive(r.Status)).ThenByDescending(r => r.Score).ThenBy(r => r.Number).ToList();
    }

    private static PmRiskView ToView(PmRisk r, string projectKey, PmTask? task)
    {
        var score = PmRiskLevels.Score(r.Probability, r.Impact);
        return new PmRiskView(r.Id, r.Number, $"{projectKey}-R{r.Number}", r.Title, r.Description, r.Category, r.Probability, r.Impact,
            score, PmRiskLevels.Of(score), r.Status, r.OwnerLogin, r.OwnerName, r.Mitigation, r.Contingency,
            r.TaskId, task is null ? string.Empty : $"{projectKey}-{task.Number}", task?.Title ?? string.Empty,
            r.ReviewDate, r.ReviewDate is { } d && d.Date < DateTime.Today && PmRiskStatuses.IsActive(r.Status), r.UpdatedAtUtc,
            r.CostImpactChf, PmRiskLevels.ExpectedValue(r.Probability, r.CostImpactChf), r.ContingencyChf,
            PmRiskLevels.ProbabilityPercent(r.Probability), IsArchived: r.IsArchived);
    }

    internal static PmRiskSummary SummarizeRisks(IEnumerable<PmRisk> risks)
    {
        var active = risks.Where(r => !r.IsArchived && PmRiskStatuses.IsActive(r.Status)).ToList();
        var levels = active.Select(r => PmRiskLevels.Of(PmRiskLevels.Score(r.Probability, r.Impact))).ToList();
        if (levels.Count == 0)
            return PmRiskSummary.Empty;
        return new PmRiskSummary(levels.Count,
            levels.Count(l => l == PmRiskLevels.Critical), levels.Count(l => l == PmRiskLevels.High),
            levels.Count(l => l == PmRiskLevels.Medium), levels.Count(l => l == PmRiskLevels.Low),
            levels.MaxBy(PmRiskLevels.Rank)!,
            Math.Round(active.Sum(r => PmRiskLevels.ExpectedValue(r.Probability, r.CostImpactChf)), 2),
            Math.Round(active.Sum(r => Math.Max(r.ContingencyChf, 0)), 2));
    }

    // Tagesstand fuer den Risiko-Burndown: eine Zeile je Projekt und Tag, bei jeder Aenderung ueberschrieben.
    // Speichert nicht.
    internal static async Task UpsertRiskSnapshotAsync(AppDbContext db, int projectId)
    {
        var risks = await db.PmRisks.Where(r => r.ProjectId == projectId).ToListAsync();
        var active = risks.Where(r => !r.IsArchived && PmRiskStatuses.IsActive(r.Status)).ToList();
        var today = DateTime.Today;
        var row = await db.PmRiskSnapshots.FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Day == today);
        if (row is null)
        {
            row = new PmRiskSnapshot { ProjectId = projectId, Day = today };
            db.PmRiskSnapshots.Add(row);
        }
        row.ActiveCount = active.Count;
        row.CriticalCount = active.Count(r => PmRiskLevels.Of(PmRiskLevels.Score(r.Probability, r.Impact)) == PmRiskLevels.Critical);
        row.TotalScore = active.Sum(r => PmRiskLevels.Score(r.Probability, r.Impact));
        row.ExpectedValueChf = Math.Round(active.Sum(r => PmRiskLevels.ExpectedValue(r.Probability, r.CostImpactChf)), 2);
        row.ContingencyChf = Math.Round(active.Sum(r => Math.Max(r.ContingencyChf, 0)), 2);
    }

    public async Task<IReadOnlyList<PmRiskBurndownPoint>> GetRiskBurndownAsync(int projectId, PmUser user)
    {
        if (await GetBoardAsync(projectId, user) is null)
            return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PmRiskSnapshots.AsNoTracking().Where(s => s.ProjectId == projectId).OrderBy(s => s.Day)
            .Select(s => new PmRiskBurndownPoint(s.Day, s.ActiveCount, s.CriticalCount, s.TotalScore, s.ExpectedValueChf, s.ContingencyChf))
            .ToListAsync();
    }

    // PMO-Vorlage: Standardrisiken der Projektart uebernehmen; schon vorhandene Titel werden uebersprungen.
    public async Task<int> AddStandardRisksAsync(PmUser user, int projectId, string template)
    {
        var standard = PmRiskTemplates.StandardRisks(template);
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsArchived) ?? throw NotFound();
            await RequireEditAsync(db, user, projectId);
            var added = await AddStandardRisksCoreAsync(db, project, standard);
            if (added > 0)
            {
                Log(db, projectId, null, user, $"hat {added} Standardrisiken aus der PMO-Vorlage übernommen");
                await db.SaveChangesAsync();
                await UpsertRiskSnapshotAsync(db, projectId);
                await db.SaveChangesAsync();
                _notifier.Publish(projectId);
            }
            return added;
        }
        finally
        {
            WriteGate.Release();
        }
    }

    private static async Task<int> AddStandardRisksCoreAsync(AppDbContext db, PmProject project, IReadOnlyList<PmRiskTemplates.StandardRisk> standard)
    {
        var existing = (await db.PmRisks.Where(r => r.ProjectId == project.Id).Select(r => r.Title).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var next = (await db.PmRisks.Where(r => r.ProjectId == project.Id).Select(r => (int?)r.Number).MaxAsync() ?? 0) + 1;
        var now = DateTime.UtcNow;
        var added = 0;
        foreach (var s in standard.Where(s => !existing.Contains(s.Title)))
        {
            db.PmRisks.Add(new PmRisk
            {
                ProjectId = project.Id, Number = next++, Title = s.Title, Description = s.Description, Category = s.Category,
                Probability = s.Probability, Impact = s.Impact, Mitigation = s.Mitigation, Status = PmRiskStatuses.Open,
                CreatedAtUtc = now, UpdatedAtUtc = now
            });
            added++;
        }
        return added;
    }

    public async Task<int> SaveRiskAsync(PmUser user, int projectId, PmRiskInput input)
    {
        RequireUser(user);
        var title = (input.Title ?? string.Empty).Trim();
        if (title.Length is 0 or > TitleMax)
            throw new PmException($"Das Risiko braucht einen Titel mit 1 bis {TitleMax} Zeichen.", $"The risk needs a title of 1 to {TitleMax} characters.");
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsArchived) ?? throw NotFound();
            await RequireEditAsync(db, user, projectId);
            var now = DateTime.UtcNow;
            PmRisk risk;
            if (input.Id is { } id)
            {
                risk = await db.PmRisks.FirstOrDefaultAsync(r => r.Id == id && r.ProjectId == projectId) ?? throw NotFound();
            }
            else
            {
                var next = (await db.PmRisks.Where(r => r.ProjectId == projectId).Select(r => (int?)r.Number).MaxAsync() ?? 0) + 1;
                risk = new PmRisk { ProjectId = projectId, Number = next, CreatedAtUtc = now };
                db.PmRisks.Add(risk);
            }
            var before = risk.Id == 0 ? null : PmRiskLevels.Of(PmRiskLevels.Score(risk.Probability, risk.Impact));
            risk.Title = title;
            risk.Description = Limit(input.Description, DescriptionMax);
            risk.Category = PmRiskCategories.All.Contains(input.Category) ? input.Category : PmRiskCategories.Schedule;
            risk.Probability = Math.Clamp(input.Probability, 1, 5);
            risk.Impact = Math.Clamp(input.Impact, 1, 5);
            risk.Status = PmRiskStatuses.All.Contains(input.Status) ? input.Status : PmRiskStatuses.Open;
            risk.Mitigation = Limit(input.Mitigation, DescriptionMax);
            risk.Contingency = Limit(input.Contingency, DescriptionMax);
            risk.ReviewDate = input.ReviewDate?.Date;
            if (input.CostImpactChf < 0 || input.ContingencyChf < 0 || input.CostImpactChf > 1e9 || input.ContingencyChf > 1e9)
                throw new PmException("Franken bitte als Betrag von 0 bis 1 Milliarde.", "Francs must be an amount from 0 to 1 billion.");
            risk.CostImpactChf = Math.Round(input.CostImpactChf, 2);
            risk.ContingencyChf = Math.Round(input.ContingencyChf, 2);
            risk.TaskId = input.TaskId is { } tid && await db.PmTasks.AnyAsync(t => t.Id == tid && t.ProjectId == projectId) ? tid : null;
            var owner = NormalizeLogin(input.OwnerLogin);
            if (owner.Length == 0)
            {
                risk.OwnerLogin = string.Empty;
                risk.OwnerName = string.Empty;
            }
            else if (!Same(owner, risk.OwnerLogin))
            {
                var member = await db.PmMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.Login == owner)
                    ?? throw new PmException("Verantwortlich kann nur ein Mitglied des Projekts sein.", "Only a project member can own a risk.");
                risk.OwnerLogin = member.Login;
                risk.OwnerName = member.Name;
            }
            risk.UpdatedAtUtc = now;
            project.UpdatedAtUtc = now;
            await db.SaveChangesAsync();
            var level = PmRiskLevels.Of(PmRiskLevels.Score(risk.Probability, risk.Impact));
            Log(db, projectId, risk.TaskId, user, before is null
                ? $"hat Risiko {project.Key}-R{risk.Number} erfasst: {risk.Title} ({LevelDe(level)})"
                : $"hat Risiko {project.Key}-R{risk.Number} geändert ({LevelDe(level)}, {StatusDe(risk.Status)})");
            await db.SaveChangesAsync();
            await UpsertRiskSnapshotAsync(db, projectId);
            await db.SaveChangesAsync();
            _notifier.Publish(projectId);
            return risk.Id;
        }
        finally
        {
            WriteGate.Release();
        }
    }

    public async Task SetRiskArchivedAsync(PmUser user, int riskId, bool archived)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var risk = await db.PmRisks.FirstOrDefaultAsync(r => r.Id == riskId) ?? throw NotFound();
        await RequireEditAsync(db, user, risk.ProjectId);
        risk.IsArchived = archived;
        risk.UpdatedAtUtc = DateTime.UtcNow;
        var key = await db.PmProjects.Where(p => p.Id == risk.ProjectId).Select(p => p.Key).FirstAsync();
        Log(db, risk.ProjectId, risk.TaskId, user, archived ? $"hat Risiko {key}-R{risk.Number} archiviert" : $"hat Risiko {key}-R{risk.Number} wiederhergestellt");
        await db.SaveChangesAsync();
        await UpsertRiskSnapshotAsync(db, risk.ProjectId);
        await db.SaveChangesAsync();
        _notifier.Publish(risk.ProjectId);
    }

    // Risikolage aller sichtbaren Projekte fuer Portfolio und Uebersicht.
    public async Task<IReadOnlyDictionary<int, PmRiskSummary>> GetRiskSummariesAsync(PmUser user)
    {
        var visible = (await GetProjectsAsync(user, includeArchived: false)).Select(p => p.Id).ToHashSet();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var risks = await db.PmRisks.AsNoTracking().Where(r => !r.IsArchived).ToListAsync();
        return risks.Where(r => visible.Contains(r.ProjectId)).GroupBy(r => r.ProjectId)
            .ToDictionary(g => g.Key, g => SummarizeRisks(g));
    }

    private static string LevelDe(string level) => level switch
    {
        PmRiskLevels.Critical => "kritisch",
        PmRiskLevels.High => "hoch",
        PmRiskLevels.Medium => "mittel",
        _ => "tief"
    };

    private static string StatusDe(string status) => status switch
    {
        PmRiskStatuses.Mitigating => "in Bearbeitung",
        PmRiskStatuses.Occurred => "eingetreten",
        PmRiskStatuses.Closed => "geschlossen",
        _ => "offen"
    };
}
