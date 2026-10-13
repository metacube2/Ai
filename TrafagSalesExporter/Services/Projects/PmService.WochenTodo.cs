using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

// Abgleich mit projektmanagement/Wochen_Todo.tsv (2026-10-13, Wunsch Ingo): Die TSV bleibt fuehrend. Jede Zeile wird
// ueber ihre Nr. zu genau einer Aufgabe (ExternalRef "WT-<Nr>"); ein erneuter Abgleich legt Neues an und
// ueberschreibt Titel, Beschreibung, Prioritaet, Termin und Labels. Die Spalte wechselt nur, wenn sich die Art
// (offen, in Arbeit, erledigt) aendert, damit eine eigene Feinsortierung innerhalb der Art erhalten bleibt.
// Zeilen, die aus der TSV verschwinden, bleiben unangetastet.
public sealed partial class PmService
{
    public const string WochenTodoPrefix = "WT-";

    internal sealed record WochenTodoRow(
        string Nr, string Urgency, string Area, string Id, string Topic, string Who, string Priority, string Status,
        DateTime? Due, string NextStep, DateTime? DoneOn, string Undertaking, string Stage);

    public async Task<PmImportResult> SyncWochenTodoAsync(PmUser user, int projectId, string tsv)
    {
        RequireUser(user);
        var rows = ParseWochenTodo(tsv);
        if (rows.Count == 0)
            throw new PmException("Die Datei enthält keine Zeilen der Wochen_Todo (erwartet: Spalten Nr, Thema, Status …).",
                "The file holds no weekly to-do rows (expected columns Nr, Thema, Status …).");

        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsArchived) ?? throw NotFound();
            await RequireManageAsync(db, user, project);
            var columns = await db.PmColumns.Where(c => c.ProjectId == projectId).OrderBy(c => c.SortOrder).ToListAsync();
            if (columns.Count == 0)
                throw new PmException("Das Projekt hat keine Spalten.", "The project has no columns.");
            var existing = await db.PmTasks.Where(t => t.ProjectId == projectId && t.ExternalRef.StartsWith(WochenTodoPrefix)).ToListAsync();
            var byRef = existing.GroupBy(t => t.ExternalRef).ToDictionary(g => g.Key, g => g.First());
            var leadIsMember = await db.PmMembers.AnyAsync(m => m.ProjectId == projectId && m.Login == user.Login);
            var maxRank = await db.PmTasks.Where(t => t.ProjectId == projectId).Select(t => (double?)t.Rank).MaxAsync() ?? 0;
            var now = DateTime.UtcNow;
            int created = 0, updated = 0, unchanged = 0, skipped = 0;

            foreach (var row in rows)
            {
                if (row.Topic.Length == 0)
                {
                    skipped++;
                    continue;
                }
                var category = WochenTodoCategory(row.Status);
                var column = columns.FirstOrDefault(c => c.Category == category) ?? columns[0];
                var title = row.Topic.Length > TitleMax ? row.Topic[..TitleMax] : row.Topic;
                var description = Limit(WochenTodoDescription(row), DescriptionMax);
                var priority = WochenTodoPriority(row.Priority, row.Urgency);
                var labels = string.Join(",", NormalizeLabels(string.Join(",", new[] { row.Area, row.Status.Replace(",", " "), row.Urgency }.Where(x => x.Length > 0))));
                var assignee = leadIsMember && row.Who.Contains("Ingo", StringComparison.OrdinalIgnoreCase) ? user : null;
                var reference = WochenTodoPrefix + row.Nr;

                if (!byRef.TryGetValue(reference, out var task))
                {
                    maxRank += 1000;
                    task = new PmTask
                    {
                        ProjectId = projectId, Number = project.NextNumber++, Type = PmTaskTypes.Task, Title = title,
                        Description = description, ColumnId = column.Id, Priority = priority, Labels = labels,
                        DueDate = row.Due, ReporterLogin = user.Login, ReporterName = user.DisplayName,
                        AssigneeLogin = assignee?.Login ?? string.Empty, AssigneeName = assignee?.DisplayName ?? string.Empty,
                        Rank = maxRank, ExternalRef = reference, CreatedAtUtc = now, UpdatedAtUtc = now,
                        CompletedAtUtc = category == PmCategories.Done ? (row.DoneOn ?? now) : null
                    };
                    db.PmTasks.Add(task);
                    byRef[reference] = task;
                    created++;
                    continue;
                }

                var changed = false;
                void Set<T>(T current, T value, Action<T> apply)
                {
                    if (EqualityComparer<T>.Default.Equals(current, value))
                        return;
                    apply(value);
                    changed = true;
                }
                Set(task.Title, title, v => task.Title = v);
                Set(task.Description, description, v => task.Description = v);
                Set(task.Priority, priority, v => task.Priority = v);
                Set(task.Labels, labels, v => task.Labels = v);
                Set(task.DueDate, row.Due, v => task.DueDate = v);
                var currentCategory = columns.FirstOrDefault(c => c.Id == task.ColumnId)?.Category;
                if (currentCategory != category)
                {
                    task.ColumnId = column.Id;
                    task.CompletedAtUtc = category == PmCategories.Done ? (row.DoneOn ?? now) : null;
                    changed = true;
                }
                if (task.IsArchived)
                {
                    task.IsArchived = false;
                    changed = true;
                }
                if (changed)
                {
                    task.UpdatedAtUtc = now;
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }

            project.UpdatedAtUtc = now;
            Log(db, projectId, null, user, $"hat die Wochen_Todo abgeglichen: {created} neu, {updated} geändert, {unchanged} unverändert");
            await db.SaveChangesAsync();
            _notifier.Publish(projectId);
            _notifier.Publish(0);
            return new PmImportResult(created, updated, unchanged, skipped);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    internal static IReadOnlyList<WochenTodoRow> ParseWochenTodo(string? tsv)
    {
        var lines = (tsv ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        if (lines.Length < 2)
            return [];
        var header = lines[0].TrimStart('﻿').Split('\t').Select(h => h.Trim()).ToList();
        int Col(params string[] names) => header.FindIndex(h => names.Any(n => string.Equals(h, n, StringComparison.OrdinalIgnoreCase)));
        var nr = Col("Nr");
        var topic = Col("Thema");
        if (nr < 0 || topic < 0)
            return [];
        var urgency = Col("Dringlichkeit");
        var area = Col("Bereich");
        var id = Col("ID");
        var who = Col("Wer");
        var prio = Col("Prio");
        var status = Col("Status");
        var due = Col("Fällig", "Faellig");
        var next = Col("Nächster Schritt", "Naechster Schritt");
        var done = Col("Erledigt am");
        var undertaking = Col("Vorhaben");
        var stage = Col("HERMES-Stufe");

        var result = new List<WochenTodoRow>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var cells = line.Split('\t');
            string Cell(int index) => index >= 0 && index < cells.Length ? cells[index].Trim() : string.Empty;
            var number = Cell(nr);
            if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out _))
                continue;
            result.Add(new WochenTodoRow(number, Cell(urgency), Cell(area), Cell(id), Cell(topic), Cell(who), Cell(prio), Cell(status),
                ParseDay(Cell(due)), Cell(next), ParseDay(Cell(done)), Cell(undertaking), Cell(stage)));
        }
        return result;
    }

    internal static string WochenTodoCategory(string status)
    {
        var s = status.Trim();
        if (s.StartsWith("Erledigt", StringComparison.OrdinalIgnoreCase))
            return PmCategories.Done;
        if (s.StartsWith("Läuft", StringComparison.OrdinalIgnoreCase) || s.StartsWith("Laeuft", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("Umgesetzt", StringComparison.OrdinalIgnoreCase) || s.StartsWith("In ", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("Teilweise", StringComparison.OrdinalIgnoreCase))
            return PmCategories.InProgress;
        return PmCategories.Todo;
    }

    internal static string WochenTodoPriority(string priority, string urgency)
    {
        if (urgency.Equals("Heute", StringComparison.OrdinalIgnoreCase))
            return PmPriorities.Highest;
        return priority.Trim().ToLowerInvariant() switch
        {
            "hoch" => PmPriorities.High,
            "niedrig" or "tief" => PmPriorities.Low,
            _ => PmPriorities.Medium
        };
    }

    private static string WochenTodoDescription(WochenTodoRow row)
    {
        var parts = new List<string>();
        if (row.NextStep.Length > 0) parts.Add(row.NextStep);
        var facts = new List<string> { $"Wochen_Todo Zeile {row.Nr}" };
        if (row.Area.Length > 0) facts.Add($"Bereich {row.Area}");
        if (row.Id.Length > 0) facts.Add($"ID {row.Id}");
        if (row.Who.Length > 0) facts.Add($"Wer {row.Who}");
        if (row.Undertaking.Length > 0) facts.Add($"Vorhaben {row.Undertaking}");
        if (row.Stage.Length > 0) facts.Add($"HERMES {row.Stage}");
        if (row.DoneOn is { } d) facts.Add($"erledigt am {d:dd.MM.yyyy}");
        parts.Add(string.Join(" · ", facts));
        return string.Join("\n\n", parts);
    }

    private static DateTime? ParseDay(string text)
    {
        if (text.Length == 0)
            return null;
        string[] formats = ["yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy"];
        return DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day.Date : null;
    }
}
