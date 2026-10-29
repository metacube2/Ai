using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

// Smartsheet-Ansicht (2026-10-29, Wunsch Ingo): Raster mit Gliederung und Gantt. Gliederung = Epic -> Aufgabe ->
// Unteraufgabe; Einruecken und Ausruecken setzen EpicId bzw. ParentId, damit Board, Backlog und Liste dieselbe
// Struktur sehen. Jede Terminaenderung, auch aus Panel, Kalender und Wochen_Todo, laeuft ueber ApplyScheduleAsync.
public sealed partial class PmService
{
    public static class SheetFields
    {
        public const string Title = "title";
        public const string Start = "start";
        public const string End = "end";
        public const string Duration = "duration";
        public const string Percent = "percent";
        public const string Milestone = "milestone";
        public const string Predecessors = "predecessors";
        public const string Assignee = "assignee";
        public const string Status = "status";
        public const string Priority = "priority";
    }

    // ---------- Lesen ----------

    public async Task<PmSheet?> GetSheetAsync(int projectId, PmUser user)
    {
        var board = await GetBoardAsync(projectId, user);
        if (board is null)
            return null;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.AsNoTracking().FirstAsync(p => p.Id == projectId);
        var columns = await db.PmColumns.AsNoTracking().Where(c => c.ProjectId == projectId).ToListAsync();
        var tasks = await db.PmTasks.AsNoTracking().Where(t => t.ProjectId == projectId && !t.IsArchived).ToListAsync();
        var links = await db.PmTaskLinks.AsNoTracking().Where(l => l.ProjectId == projectId).ToListAsync();
        var risks = await db.PmRisks.AsNoTracking().Where(r => r.ProjectId == projectId && !r.IsArchived).ToListAsync();

        var nodes = BuildNodes(tasks);
        PmSchedule.Apply(nodes, links.Select(ToScheduleLink).ToList());
        var nodeById = nodes.ToDictionary(n => n.Id);
        var byId = tasks.ToDictionary(t => t.Id);
        var numberById = tasks.ToDictionary(t => t.Id, t => t.Number);
        var doneColumns = columns.Where(c => c.Category == PmCategories.Done).Select(c => c.Id).ToHashSet();
        var activeRisks = risks.Where(r => PmRiskStatuses.IsActive(r.Status)).ToList();

        var rows = new List<PmSheetRow>();
        foreach (var (task, level) in OutlineOrder(tasks))
        {
            var node = nodeById[task.Id];
            var column = columns.FirstOrDefault(c => c.Id == task.ColumnId);
            var preds = links.Where(l => l.SuccessorId == task.Id && numberById.ContainsKey(l.PredecessorId))
                .Select(l => (numberById[l.PredecessorId], l.LagDays));
            var taskRisks = activeRisks.Where(r => r.TaskId == task.Id).ToList();
            var riskLevel = taskRisks.Count == 0 ? string.Empty
                : taskRisks.Select(r => PmRiskLevels.Of(PmRiskLevels.Score(r.Probability, r.Impact))).MaxBy(PmRiskLevels.Rank)!;
            var duration = node.Start is { } s && node.End is { } e ? (task.IsMilestone ? 0 : PmSchedule.Duration(s, e)) : 0;
            rows.Add(new PmSheetRow(task.Id, $"{project.Key}-{task.Number}", task.Number, task.Type, task.Title, level,
                node.RollupParentId, node.HasChildren, node.Start, node.End, duration, task.IsMilestone, node.Percent,
                PmSchedule.FormatPredecessors(preds), node.Critical, node.FloatDays, task.ColumnId, column?.Name ?? string.Empty,
                column?.Category ?? PmCategories.Todo, task.Priority, task.AssigneeLogin, task.AssigneeName, SplitLabels(task.Labels),
                node.End is { } end && end.Date < DateTime.Today && !doneColumns.Contains(task.ColumnId) && !node.HasChildren,
                task.BaselineStart, task.BaselineEnd, riskLevel, taskRisks.Count));
        }
        var dated = rows.Where(r => r.Start is not null && r.End is not null).ToList();
        return new PmSheet(board, rows,
            links.Select(l => new PmSheetLink(l.PredecessorId, l.SuccessorId, l.LagDays)).ToList(),
            dated.Count == 0 ? null : dated.Min(r => r.Start), dated.Count == 0 ? null : dated.Max(r => r.End),
            rows.Count(r => r.IsCritical && !r.HasChildren), SummarizeRisks(risks));
    }

    // Zeilenfolge: oberste Ebene (Epics und Aufgaben ohne Epic) nach Zeilenfolge, darunter je Epic seine Aufgaben,
    // unter jeder Aufgabe ihre Unteraufgaben. Ohne eigene Zeilenfolge gilt der Rang aus Board und Backlog.
    internal static IEnumerable<(PmTask Task, int Level)> OutlineOrder(IReadOnlyList<PmTask> tasks)
    {
        var ids = tasks.Select(t => t.Id).ToHashSet();
        double Order(PmTask t) => t.SheetOrder != 0 ? t.SheetOrder : t.Rank;
        int? ParentOf(PmTask t) => t.ParentId is { } p && ids.Contains(p) ? p : null;
        int? EpicOf(PmTask t) => t.Type != PmTaskTypes.Epic && ParentOf(t) is null && t.EpicId is { } e && ids.Contains(e) ? e : null;

        var top = tasks.Where(t => ParentOf(t) is null && EpicOf(t) is null).OrderBy(Order).ThenBy(t => t.Number);
        foreach (var t in top)
        {
            yield return (t, 0);
            if (t.Type == PmTaskTypes.Epic)
            {
                foreach (var child in tasks.Where(c => EpicOf(c) == t.Id).OrderBy(Order).ThenBy(c => c.Number))
                {
                    yield return (child, 1);
                    foreach (var sub in tasks.Where(s => ParentOf(s) == child.Id).OrderBy(Order).ThenBy(s => s.Number))
                        yield return (sub, 2);
                }
            }
            else
            {
                foreach (var sub in tasks.Where(s => ParentOf(s) == t.Id).OrderBy(Order).ThenBy(s => s.Number))
                    yield return (sub, 1);
            }
        }
    }

    private static List<PmSchedule.Node> BuildNodes(IReadOnlyList<PmTask> tasks)
    {
        var ids = tasks.Select(t => t.Id).ToHashSet();
        return tasks.Select(t => new PmSchedule.Node
        {
            Id = t.Id,
            RollupParentId = t.ParentId is { } p && ids.Contains(p) ? p
                : t.Type != PmTaskTypes.Epic && t.EpicId is { } e && ids.Contains(e) ? e : null,
            // Nur Faelligkeit gesetzt: der Balken ist ein Tag lang.
            Start = t.StartDate ?? t.DueDate,
            End = t.DueDate ?? t.StartDate,
            Milestone = t.IsMilestone,
            Percent = Math.Clamp(t.PercentComplete, 0, 100)
        }).ToList();
    }

    private static PmSchedule.Link ToScheduleLink(PmTaskLink l) => new(l.PredecessorId, l.SuccessorId, l.LagDays);

    // ---------- Gemeinsamer Einstieg fuer jede Terminaenderung ----------

    // Rechnet Nachfolger, Sammelzeilen und erledigte Zeilen (100 %) eines Projekts nach und schreibt die
    // Aenderungen in die verfolgten Aufgaben. Speichert nicht; der Aufrufer ruft SaveChangesAsync.
    internal static async Task<int> ApplyScheduleAsync(AppDbContext db, int projectId)
    {
        var tasks = await db.PmTasks.Where(t => t.ProjectId == projectId && !t.IsArchived).ToListAsync();
        var links = await db.PmTaskLinks.Where(l => l.ProjectId == projectId).ToListAsync();
        var doneColumns = await db.PmColumns.Where(c => c.ProjectId == projectId && c.Category == PmCategories.Done)
            .Select(c => c.Id).ToListAsync();
        var nodes = BuildNodes(tasks);
        foreach (var n in nodes)
        {
            var t = tasks.First(x => x.Id == n.Id);
            if (doneColumns.Contains(t.ColumnId))
                n.Percent = 100;
        }
        var moved = PmSchedule.Apply(nodes, links.Select(ToScheduleLink).ToList());
        var changed = 0;
        foreach (var n in nodes)
        {
            var t = tasks.First(x => x.Id == n.Id);
            // Termine nur schreiben, wenn die Rechnung sie verschoben hat; eine Zeile mit nur Faelligkeit bleibt so.
            if (moved.Contains(n.Id) && (t.StartDate != n.Start?.Date || t.DueDate != n.End?.Date))
            {
                t.StartDate = n.Start?.Date;
                t.DueDate = n.End?.Date;
                changed++;
            }
            if (t.PercentComplete != n.Percent)
            {
                t.PercentComplete = n.Percent;
                changed++;
            }
        }
        return changed;
    }

    // ---------- Gantt: Balken ziehen und Laenge aendern ----------

    public async Task RescheduleAsync(PmUser user, int taskId, DateTime start, DateTime end)
    {
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsArchived) ?? throw NotFound();
            await RequireEditAsync(db, user, task.ProjectId);
            if (await HasChildrenAsync(db, task))
                throw new PmException("Eine Sammelzeile folgt ihren Unteraufgaben; verschiebe die Unteraufgaben.",
                    "A summary row follows its children; move the child rows instead.");

            var duration = task.IsMilestone ? 0 : Math.Max(PmSchedule.Duration(PmSchedule.SnapForward(start), PmSchedule.SnapBackward(end)), 1);
            var newStart = PmSchedule.SnapForward(start);
            var predecessors = await db.PmTaskLinks.Where(l => l.SuccessorId == taskId).ToListAsync();
            if (predecessors.Count > 0)
            {
                var predTasks = await db.PmTasks.Where(t => predecessors.Select(p => p.PredecessorId).Contains(t.Id) && !t.IsArchived).ToListAsync();
                PmTaskLink? binding = null;
                DateTime? earliest = null;
                foreach (var link in predecessors)
                {
                    var pred = predTasks.FirstOrDefault(t => t.Id == link.PredecessorId);
                    var predEnd = pred?.DueDate ?? pred?.StartDate;
                    if (predEnd is null)
                        continue;
                    var candidate = PmSchedule.EarliestStartAfter(predEnd.Value, link.LagDays);
                    if (earliest is null || candidate > earliest)
                    {
                        earliest = candidate;
                        binding = link;
                    }
                }
                if (earliest is { } min && binding is not null)
                {
                    if (newStart < min)
                        newStart = min;
                    else if (newStart > min)
                        // Wie Smartsheet: spaeter als erlaubt heisst mehr Versatz zum bestimmenden Vorgaenger.
                        binding.LagDays += PmSchedule.Duration(min, newStart) - 1;
                }
            }
            task.StartDate = newStart;
            task.DueDate = task.IsMilestone ? newStart : PmSchedule.EndFor(newStart, duration);
            task.UpdatedAtUtc = DateTime.UtcNow;
            await ApplyScheduleAsync(db, task.ProjectId);
            var key = await db.PmProjects.Where(p => p.Id == task.ProjectId).Select(p => p.Key).FirstAsync();
            Log(db, task.ProjectId, task.Id, user,
                $"hat {key}-{task.Number} terminiert: {task.StartDate:dd.MM.yyyy} bis {task.DueDate:dd.MM.yyyy}");
            await db.SaveChangesAsync();
            _notifier.Publish(task.ProjectId);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    // ---------- Raster: eine Zelle aendern ----------

    public async Task UpdateSheetFieldAsync(PmUser user, int taskId, string field, string? value)
    {
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsArchived) ?? throw NotFound();
            await RequireEditAsync(db, user, task.ProjectId);
            var project = await db.PmProjects.FirstAsync(p => p.Id == task.ProjectId);
            var key = $"{project.Key}-{task.Number}";
            value = (value ?? string.Empty).Trim();
            var isSummary = await HasChildrenAsync(db, task);
            string change;

            switch (field)
            {
                case SheetFields.Title:
                    task.Title = ValidateTitle(value);
                    change = "Titel";
                    break;
                case SheetFields.Start:
                case SheetFields.End:
                case SheetFields.Duration:
                    if (isSummary)
                        throw new PmException("Start, Ende und Dauer einer Sammelzeile kommen aus ihren Unteraufgaben.",
                            "Start, end and duration of a summary row come from its children.");
                    change = ApplyDateField(task, field, value);
                    break;
                case SheetFields.Percent:
                    if (isSummary)
                        throw new PmException("Der Fortschritt einer Sammelzeile wird aus den Unteraufgaben gerechnet.",
                            "The progress of a summary row is calculated from its children.");
                    if (!int.TryParse(value.TrimEnd('%', ' '), NumberStyles.Integer, CultureInfo.InvariantCulture, out var percent))
                        throw new PmException("Fortschritt bitte als Zahl von 0 bis 100.", "Progress must be a number from 0 to 100.");
                    task.PercentComplete = Math.Clamp(percent, 0, 100);
                    change = $"{task.PercentComplete} % erledigt";
                    break;
                case SheetFields.Milestone:
                    if (isSummary)
                        throw new PmException("Eine Sammelzeile kann kein Meilenstein sein.", "A summary row cannot be a milestone.");
                    task.IsMilestone = value is "1" or "true" or "True";
                    if (task.IsMilestone && (task.StartDate ?? task.DueDate) is { } day)
                    {
                        task.StartDate = PmSchedule.SnapForward(day);
                        task.DueDate = task.StartDate;
                    }
                    change = task.IsMilestone ? "als Meilenstein markiert" : "Meilenstein entfernt";
                    break;
                case SheetFields.Predecessors:
                    change = await ApplyPredecessorsAsync(db, task, value);
                    break;
                case SheetFields.Assignee:
                    await ApplyAssigneeAsync(db, task, value);
                    change = task.AssigneeLogin.Length == 0 ? "Zuweisung entfernt" : $"zugewiesen an {task.AssigneeName}";
                    break;
                case SheetFields.Status:
                    if (!int.TryParse(value, out var columnId))
                        throw NotFound();
                    var column = await db.PmColumns.FirstOrDefaultAsync(c => c.Id == columnId && c.ProjectId == task.ProjectId) ?? throw NotFound();
                    SetColumn(task, column);
                    change = $"Status {column.Name}";
                    break;
                case SheetFields.Priority:
                    if (!PmPriorities.All.Contains(value))
                        throw NotFound();
                    task.Priority = value;
                    change = $"Priorität {PriorityDe(value)}";
                    break;
                default:
                    throw NotFound();
            }

            task.UpdatedAtUtc = DateTime.UtcNow;
            project.UpdatedAtUtc = task.UpdatedAtUtc;
            await ApplyScheduleAsync(db, task.ProjectId);
            Log(db, task.ProjectId, task.Id, user, $"hat {key} geändert: {change}");
            await db.SaveChangesAsync();
            _notifier.Publish(task.ProjectId);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    private static string ApplyDateField(PmTask task, string field, string value)
    {
        var start = task.StartDate ?? task.DueDate;
        var end = task.DueDate ?? task.StartDate;
        var duration = start is { } s0 && end is { } e0 ? Math.Max(PmSchedule.Duration(s0, e0), 1) : 1;
        if (field == SheetFields.Duration)
        {
            if (!int.TryParse(value.Replace("T", string.Empty).Replace("d", string.Empty).Trim(), out var days) || days < 0 || days > 2000)
                throw new PmException("Dauer bitte in Arbeitstagen von 0 bis 2000.", "Duration must be 0 to 2000 working days.");
            var from = PmSchedule.SnapForward(start ?? DateTime.Today);
            task.IsMilestone = days == 0;
            task.StartDate = from;
            task.DueDate = days == 0 ? from : PmSchedule.EndFor(from, days);
            return days == 0 ? "als Meilenstein markiert" : $"Dauer {days} Arbeitstage";
        }
        if (value.Length == 0)
        {
            if (field == SheetFields.Start) task.StartDate = null; else task.DueDate = null;
            return field == SheetFields.Start ? "Start entfernt" : "Ende entfernt";
        }
        if (!TryParseDate(value, out var date))
            throw new PmException("Datum bitte als TT.MM.JJJJ.", "Date must be DD.MM.YYYY.");
        if (field == SheetFields.Start)
        {
            // Wie Smartsheet: ein neuer Start verschiebt den Balken, die Dauer bleibt.
            var newStart = PmSchedule.SnapForward(date);
            task.StartDate = newStart;
            task.DueDate = task.IsMilestone ? newStart : PmSchedule.EndFor(newStart, duration);
            return $"Start {newStart:dd.MM.yyyy}";
        }
        var newEnd = PmSchedule.SnapBackward(date);
        var startDay = start ?? newEnd;
        if (newEnd < startDay)
            startDay = newEnd;
        task.StartDate = startDay;
        task.DueDate = task.IsMilestone ? startDay : newEnd;
        return $"Ende {newEnd:dd.MM.yyyy}";
    }

    internal static bool TryParseDate(string value, out DateTime date) =>
        DateTime.TryParseExact(value, ["dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static async Task<string> ApplyPredecessorsAsync(AppDbContext db, PmTask task, string value)
    {
        var parsed = PmSchedule.ParsePredecessors(value)
            ?? throw new PmException("Vorgänger bitte als Nummern mit Komma, z. B. „3, 5+2“ (Versatz in Arbeitstagen).",
                "Predecessors must be numbers separated by commas, e.g. \"3, 5+2\" (lag in working days).");
        var tasks = await db.PmTasks.Where(t => t.ProjectId == task.ProjectId && !t.IsArchived).ToListAsync();
        var byNumber = tasks.ToDictionary(t => t.Number);
        var unknown = parsed.Where(p => !byNumber.ContainsKey(p.Number)).Select(p => p.Number).ToList();
        if (unknown.Count > 0)
            throw new PmException($"Diese Nummern gibt es im Projekt nicht: {string.Join(", ", unknown)}.",
                $"These numbers do not exist in the project: {string.Join(", ", unknown)}.");
        var existing = await db.PmTaskLinks.Where(l => l.ProjectId == task.ProjectId).ToListAsync();
        var others = existing.Where(l => l.SuccessorId != task.Id).Select(ToScheduleLink).ToList();
        var fresh = new List<PmSchedule.Link>();
        foreach (var (number, lag) in parsed.DistinctBy(p => p.Number))
        {
            var pred = byNumber[number];
            if (PmSchedule.WouldCreateCycle(others.Concat(fresh), pred.Id, task.Id))
                throw new PmException($"{number} als Vorgänger ergäbe einen Kreis.", $"{number} as predecessor would create a loop.");
            fresh.Add(new PmSchedule.Link(pred.Id, task.Id, Math.Clamp(lag, -500, 500)));
        }
        db.PmTaskLinks.RemoveRange(existing.Where(l => l.SuccessorId == task.Id));
        foreach (var l in fresh)
            db.PmTaskLinks.Add(new PmTaskLink { ProjectId = task.ProjectId, PredecessorId = l.PredecessorId, SuccessorId = l.SuccessorId, LagDays = l.LagDays });
        // Ein Nachfolger ohne Termin bekommt einen Tag Dauer am fruehesten Start.
        if (fresh.Count > 0 && task.StartDate is null && task.DueDate is null)
        {
            var latest = fresh.Select(l => byNumber.Values.First(t => t.Id == l.PredecessorId))
                .Select(p => p.DueDate ?? p.StartDate).Where(d => d is not null).Max();
            if (latest is { } last)
            {
                task.StartDate = PmSchedule.EarliestStartAfter(last, 0);
                task.DueDate = task.StartDate;
            }
        }
        return fresh.Count == 0 ? "Vorgänger entfernt" : $"Vorgänger {PmSchedule.FormatPredecessors(parsed)}";
    }

    private static async Task<bool> HasChildrenAsync(AppDbContext db, PmTask task) =>
        await db.PmTasks.AnyAsync(t => !t.IsArchived && t.Id != task.Id &&
            (t.ParentId == task.Id || (task.Type == PmTaskTypes.Epic && t.EpicId == task.Id && t.ParentId == null)));

    // ---------- Gliederung und Zeilenfolge ----------

    public async Task IndentAsync(PmUser user, int taskId) => await OutlineAsync(user, taskId, indent: true);

    public async Task OutdentAsync(PmUser user, int taskId) => await OutlineAsync(user, taskId, indent: false);

    private async Task OutlineAsync(PmUser user, int taskId, bool indent)
    {
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsArchived) ?? throw NotFound();
            await RequireEditAsync(db, user, task.ProjectId);
            var tasks = await db.PmTasks.Where(t => t.ProjectId == task.ProjectId && !t.IsArchived).ToListAsync();
            var outline = OutlineOrder(tasks).ToList();
            var index = outline.FindIndex(x => x.Task.Id == taskId);
            var level = outline[index].Level;
            var key = await db.PmProjects.Where(p => p.Id == task.ProjectId).Select(p => p.Key).FirstAsync();
            string change;

            if (indent)
            {
                if (task.Type == PmTaskTypes.Epic)
                    throw new PmException("Ein Epic bleibt auf der obersten Ebene.", "An epic stays on the top level.");
                // Vorgaenger auf derselben Ebene suchen.
                var above = outline.Take(index).LastOrDefault(x => x.Level == level);
                var parentLevelAbove = outline.Take(index).LastOrDefault(x => x.Level < level);
                if (above.Task is null || (parentLevelAbove.Task is not null && outline.IndexOf(parentLevelAbove) > outline.IndexOf(above)))
                    throw new PmException("Über dieser Zeile steht keine Zeile derselben Ebene zum Einrücken.",
                        "There is no row on the same level above to indent under.");
                if (tasks.Any(t => t.ParentId == task.Id && !t.IsArchived))
                    throw new PmException("Eine Aufgabe mit Unteraufgaben lässt sich nicht weiter einrücken.",
                        "A task with subtasks cannot be indented further.");
                if (above.Task.Type == PmTaskTypes.Epic)
                {
                    task.EpicId = above.Task.Id;
                    task.ParentId = null;
                    if (task.Type == PmTaskTypes.Subtask) task.Type = PmTaskTypes.Task;
                    change = $"unter Epic {key}-{above.Task.Number} eingerückt";
                }
                else if (above.Task.ParentId is null)
                {
                    task.ParentId = above.Task.Id;
                    task.EpicId = above.Task.EpicId;
                    task.Type = PmTaskTypes.Subtask;
                    change = $"als Unteraufgabe von {key}-{above.Task.Number} eingerückt";
                }
                else
                {
                    throw new PmException("Unteraufgaben haben keine weitere Ebene.", "Subtasks have no further level.");
                }
                // Als letzte Zeile unter die neue Elternzeile.
                task.SheetOrder = NextOrder(tasks, task, t => task.ParentId is { } p
                    ? t.ParentId == p
                    : t.EpicId == task.EpicId && t.ParentId == null && t.Type != PmTaskTypes.Epic);
            }
            else
            {
                if (level == 0)
                    throw new PmException("Die Zeile steht schon auf der obersten Ebene.", "The row is already on the top level.");
                if (task.ParentId is { } parentId)
                {
                    var parent = tasks.First(t => t.Id == parentId);
                    task.ParentId = null;
                    task.EpicId = parent.EpicId;
                    task.Type = PmTaskTypes.Task;
                    task.SheetOrder = (parent.SheetOrder != 0 ? parent.SheetOrder : parent.Rank) + 0.5;
                    change = "ausgerückt";
                }
                else
                {
                    var epic = tasks.FirstOrDefault(t => t.Id == task.EpicId);
                    task.EpicId = null;
                    task.SheetOrder = epic is null ? task.SheetOrder : (epic.SheetOrder != 0 ? epic.SheetOrder : epic.Rank) + 0.5;
                    change = "aus dem Epic ausgerückt";
                }
            }
            task.UpdatedAtUtc = DateTime.UtcNow;
            await ApplyScheduleAsync(db, task.ProjectId);
            Log(db, task.ProjectId, task.Id, user, $"hat {key}-{task.Number} {change}");
            await db.SaveChangesAsync();
            _notifier.Publish(task.ProjectId);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    // Elternzeile im Raster: die naechste Zeile weiter oben mit kleinerer Ebene (null = oberste Ebene).
    internal static Dictionary<int, int?> OutlineParents(IReadOnlyList<(PmTask Task, int Level)> outline)
    {
        var result = new Dictionary<int, int?>();
        for (var i = 0; i < outline.Count; i++)
        {
            int? parent = null;
            for (var j = i - 1; j >= 0; j--)
            {
                if (outline[j].Level < outline[i].Level)
                {
                    parent = outline[j].Task.Id;
                    break;
                }
            }
            result[outline[i].Task.Id] = parent;
        }
        return result;
    }

    private static double NextOrder(List<PmTask> tasks, PmTask task, Func<PmTask, bool> sibling) =>
        (tasks.Where(t => t.Id != task.Id && sibling(t)).Select(t => (double?)(t.SheetOrder != 0 ? t.SheetOrder : t.Rank)).Max() ?? 0) + 1000;

    // Zeile vor eine andere Zeile derselben Gruppe setzen (beforeTaskId null = ans Ende der Gruppe).
    public async Task MoveRowAsync(PmUser user, int taskId, int? beforeTaskId)
    {
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsArchived) ?? throw NotFound();
            await RequireEditAsync(db, user, task.ProjectId);
            var tasks = await db.PmTasks.Where(t => t.ProjectId == task.ProjectId && !t.IsArchived).ToListAsync();
            var outline = OutlineOrder(tasks).ToList();
            var parentOf = OutlineParents(outline);
            var group = outline.Where(x => x.Task.Id != taskId && parentOf[x.Task.Id] == parentOf[taskId]).Select(x => x.Task).ToList();
            if (beforeTaskId is { } before && group.All(t => t.Id != before))
                throw new PmException("Zeilen lassen sich nur innerhalb derselben Ebene verschieben.", "Rows can only move within the same level.");
            // Gruppe neu durchnummerieren, damit Raster und Rang nicht mehr gemischt werden.
            var ordered = group.ToList();
            var at = beforeTaskId is { } b ? ordered.FindIndex(t => t.Id == b) : ordered.Count;
            ordered.Insert(at, task);
            for (var i = 0; i < ordered.Count; i++)
                ordered[i].SheetOrder = (i + 1) * 1000;
            await db.SaveChangesAsync();
            _notifier.Publish(task.ProjectId);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    // Neue Zeile unter einer bestehenden, auf derselben Ebene (afterTaskId null = am Ende der obersten Ebene).
    public async Task<int> InsertRowAsync(PmUser user, int projectId, int? afterTaskId, string title)
    {
        int? epicId = null, parentId = null;
        double order = 0;
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            if (afterTaskId is { } after && await db.PmTasks.FirstOrDefaultAsync(t => t.Id == after && t.ProjectId == projectId) is { } anchor)
            {
                parentId = anchor.ParentId;
                epicId = anchor.Type == PmTaskTypes.Epic ? null : anchor.EpicId;
                order = (anchor.SheetOrder != 0 ? anchor.SheetOrder : anchor.Rank) + 0.5;
            }
        }
        var id = await CreateTaskAsync(user, projectId, new PmTaskInput
        {
            Title = title,
            ParentId = parentId,
            EpicId = epicId,
            Type = parentId is null ? PmTaskTypes.Task : PmTaskTypes.Subtask
        });
        if (order != 0)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var task = await db.PmTasks.FirstAsync(t => t.Id == id);
            task.SheetOrder = order;
            await db.SaveChangesAsync();
            _notifier.Publish(projectId);
        }
        return id;
    }

    // ---------- Basisplan ----------

    // Friert die aktuellen Termine als Basisplan ein (clear = Basisplan loeschen). Nur die Projektleitung.
    public async Task SetBaselineAsync(PmUser user, int projectId, bool clear)
    {
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw NotFound();
            await RequireManageAsync(db, user, project);
            foreach (var t in await db.PmTasks.Where(t => t.ProjectId == projectId && !t.IsArchived).ToListAsync())
            {
                t.BaselineStart = clear ? null : t.StartDate ?? t.DueDate;
                t.BaselineEnd = clear ? null : t.DueDate ?? t.StartDate;
            }
            Log(db, projectId, null, user, clear ? "hat den Basisplan gelöscht" : "hat den Basisplan gesetzt");
            await db.SaveChangesAsync();
            _notifier.Publish(projectId);
        }
        finally
        {
            WriteGate.Release();
        }
    }
}
