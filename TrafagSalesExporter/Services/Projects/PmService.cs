using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

public interface IPmService
{
    Task<IReadOnlyList<PmProjectSummary>> GetProjectsAsync(PmUser user, bool includeArchived = false);
    Task<int> CreateProjectAsync(PmUser user, PmProjectInput input);
    Task UpdateProjectAsync(PmUser user, int projectId, PmProjectInput input);
    Task SetProjectArchivedAsync(PmUser user, int projectId, bool archived);
    Task<PmBoard?> GetBoardAsync(int projectId, PmUser user);
    Task<int?> FindProjectIdAsync(string key);
    Task<PmTaskDetail?> GetTaskAsync(int taskId, PmUser user);
    Task<int> CreateTaskAsync(PmUser user, int projectId, PmTaskInput input);
    Task UpdateTaskAsync(PmUser user, int taskId, PmTaskInput input);
    Task MoveTaskAsync(PmUser user, int taskId, int columnId, int? beforeTaskId);
    Task PlanTaskAsync(PmUser user, int taskId, int? sprintId, int? beforeTaskId);
    Task SetTaskArchivedAsync(PmUser user, int taskId, bool archived);
    Task<int> AddChecklistItemAsync(PmUser user, int taskId, string text);
    Task ToggleChecklistItemAsync(PmUser user, int itemId);
    Task DeleteChecklistItemAsync(PmUser user, int itemId);
    Task<int> AddCommentAsync(PmUser user, int taskId, string body);
    Task AddMemberAsync(PmUser user, int projectId, string login, string name);
    Task RemoveMemberAsync(PmUser user, int projectId, string login);
    Task<int> AddColumnAsync(PmUser user, int projectId, string name, string category);
    Task UpdateColumnAsync(PmUser user, int columnId, string name, string category, int wipLimit);
    Task MoveColumnAsync(PmUser user, int columnId, int direction);
    Task DeleteColumnAsync(PmUser user, int columnId);
    Task<int> CreateSprintAsync(PmUser user, int projectId, string name, string goal, DateTime? start, DateTime? end);
    Task StartSprintAsync(PmUser user, int sprintId, DateTime start, DateTime end, string goal);
    Task<int> CompleteSprintAsync(PmUser user, int sprintId, int? moveOpenToSprintId);
    Task<IReadOnlyList<PmMyTask>> GetMyTasksAsync(PmUser user);
    Task<IReadOnlyList<PmActivityView>> GetActivityAsync(int projectId, int take = 40);
    Task<IReadOnlyList<PmPerson>> GetKnownPeopleAsync();
}

/// <summary>
/// Trafag Projekte: Projekte, Spalten (Trello-Listen / Jira-Status / Planner-Buckets), Aufgaben mit Rang,
/// Sprints, Checklisten, Kommentare und Verlauf. Rechte: alle sehen alles; Aufgaben aendern duerfen
/// Mitglieder und Admins; Projekt, Mitglieder und Spalten verwaltet die Projektleitung oder ein Admin.
/// Rang ist eine Kommazahl: Einfuegen zwischen zwei Karten nimmt die Mitte, ans Ende +1000.
/// </summary>
public sealed partial class PmService : IPmService
{
    public const int TitleMax = 200;
    public const int DescriptionMax = 20000;
    public const int CommentMax = 10000;

    // Ziehen und Nummern vergeben laufen nacheinander, damit zwei gleichzeitige Aenderungen nicht dieselbe
    // Nummer oder denselben Rang erzeugen. SQLite schreibt ohnehin nur einzeln.
    private static readonly SemaphoreSlim WriteGate = new(1, 1);

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly PmNotifier _notifier;

    public PmService(IDbContextFactory<AppDbContext> dbFactory, PmNotifier notifier)
    {
        _dbFactory = dbFactory;
        _notifier = notifier;
    }

    // ---------- Projekte ----------

    public async Task<IReadOnlyList<PmProjectSummary>> GetProjectsAsync(PmUser user, bool includeArchived = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var projects = await db.PmProjects.AsNoTracking().Where(p => includeArchived || !p.IsArchived).ToListAsync();
        var members = await db.PmMembers.AsNoTracking().ToListAsync();
        var columns = await db.PmColumns.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Category);
        var tasks = await db.PmTasks.AsNoTracking().Where(t => !t.IsArchived && t.Type != PmTaskTypes.Epic)
            .Select(t => new { t.ProjectId, t.ColumnId, t.DueDate }).ToListAsync();
        var today = DateTime.Today;

        return projects
            .Select(p =>
            {
                var own = tasks.Where(t => t.ProjectId == p.Id).ToList();
                var done = own.Count(t => columns.GetValueOrDefault(t.ColumnId) == PmCategories.Done);
                var progress = own.Count(t => columns.GetValueOrDefault(t.ColumnId) == PmCategories.InProgress);
                var overdue = own.Count(t => t.DueDate is { } d && d.Date < today && columns.GetValueOrDefault(t.ColumnId) != PmCategories.Done);
                var people = members.Where(m => m.ProjectId == p.Id)
                    .OrderByDescending(m => m.Role == PmRoles.Lead).ThenBy(m => m.Name)
                    .Select(m => new PmPerson(m.Login, m.Name)).ToList();
                return new PmProjectSummary(p.Id, p.Key, p.Name, p.Description, p.Color, p.Icon, p.Template, p.LeadLogin, p.LeadName,
                    people, p.StartDate, p.DueDate, p.IsArchived, own.Count, own.Count - done - progress, progress, done, overdue,
                    own.Count == 0 ? 0 : (int)Math.Round(done * 100d / own.Count), p.UpdatedAtUtc,
                    people.Any(m => Same(m.Login, user.Login)));
            })
            .OrderByDescending(p => p.IsMember)
            .ThenByDescending(p => p.UpdatedAtUtc)
            .ToList();
    }

    public async Task<int> CreateProjectAsync(PmUser user, PmProjectInput input)
    {
        RequireUser(user);
        var (name, key) = ValidateProject(input);
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            if (await db.PmProjects.AnyAsync(p => p.Key == key))
                throw new PmException($"Der Schlüssel {key} ist schon vergeben.", $"The key {key} is already taken.");
            var now = DateTime.UtcNow;
            var template = PmTemplates.All.Contains(input.Template) ? input.Template : PmTemplates.Kanban;
            var project = new PmProject
            {
                Key = key, Name = name, Description = (input.Description ?? string.Empty).Trim(),
                Color = PmStyle.Colors.Contains(input.Color) ? input.Color : PmStyle.Colors[0],
                Icon = PmStyle.Icons.Contains(input.Icon) ? input.Icon : PmStyle.Icons[0],
                Template = template, LeadLogin = user.Login, LeadName = user.DisplayName,
                StartDate = input.StartDate?.Date, DueDate = input.DueDate?.Date, CreatedAtUtc = now, UpdatedAtUtc = now
            };
            db.PmProjects.Add(project);
            await db.SaveChangesAsync();

            db.PmMembers.Add(new PmMember { ProjectId = project.Id, Login = user.Login, Name = user.DisplayName, Role = PmRoles.Lead, AddedAtUtc = now });
            var order = 10;
            foreach (var (columnName, category) in TemplateColumns(template))
            {
                db.PmColumns.Add(new PmColumn { ProjectId = project.Id, Name = columnName, Category = category, SortOrder = order });
                order += 10;
            }
            if (template == PmTemplates.Scrum)
                db.PmSprints.Add(new PmSprint { ProjectId = project.Id, Name = $"{key} Sprint 1", State = PmSprintStates.Planned });
            Log(db, project.Id, null, user, $"hat das Projekt {key} angelegt");
            await db.SaveChangesAsync();
            _notifier.Publish(0);
            return project.Id;
        }
        finally
        {
            WriteGate.Release();
        }
    }

    internal static IEnumerable<(string Name, string Category)> TemplateColumns(string template) => template switch
    {
        PmTemplates.Scrum => [("Zu erledigen", PmCategories.Todo), ("In Arbeit", PmCategories.InProgress), ("Test", PmCategories.InProgress), ("Erledigt", PmCategories.Done)],
        PmTemplates.Planner => [("Ideen", PmCategories.Todo), ("Geplant", PmCategories.Todo), ("In Arbeit", PmCategories.InProgress), ("Erledigt", PmCategories.Done)],
        _ => [("Zu erledigen", PmCategories.Todo), ("In Arbeit", PmCategories.InProgress), ("Review", PmCategories.InProgress), ("Erledigt", PmCategories.Done)]
    };

    public async Task UpdateProjectAsync(PmUser user, int projectId, PmProjectInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw NotFound();
        await RequireManageAsync(db, user, project);
        var name = (input.Name ?? string.Empty).Trim();
        if (name.Length is < 3 or > 80)
            throw new PmException("Der Projektname braucht 3 bis 80 Zeichen.", "The project name needs 3 to 80 characters.");
        project.Name = name;
        project.Description = (input.Description ?? string.Empty).Trim();
        project.Color = PmStyle.Colors.Contains(input.Color) ? input.Color : project.Color;
        project.Icon = PmStyle.Icons.Contains(input.Icon) ? input.Icon : project.Icon;
        project.StartDate = input.StartDate?.Date;
        project.DueDate = input.DueDate?.Date;
        project.UpdatedAtUtc = DateTime.UtcNow;
        Log(db, project.Id, null, user, "hat die Projekteinstellungen geändert");
        await db.SaveChangesAsync();
        _notifier.Publish(projectId);
    }

    public async Task SetProjectArchivedAsync(PmUser user, int projectId, bool archived)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw NotFound();
        await RequireManageAsync(db, user, project);
        project.IsArchived = archived;
        project.UpdatedAtUtc = DateTime.UtcNow;
        Log(db, project.Id, null, user, archived ? "hat das Projekt archiviert" : "hat das Projekt wiederhergestellt");
        await db.SaveChangesAsync();
        _notifier.Publish(0);
        _notifier.Publish(projectId);
    }

    public async Task<int?> FindProjectIdAsync(string key)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var upper = (key ?? string.Empty).ToUpperInvariant();
        return await db.PmProjects.AsNoTracking().Where(p => p.Key == upper).Select(p => (int?)p.Id).FirstOrDefaultAsync();
    }

    // ---------- Board ----------

    public async Task<PmBoard?> GetBoardAsync(int projectId, PmUser user)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null)
            return null;
        var summary = (await GetProjectsAsync(user, includeArchived: true)).First(p => p.Id == projectId);
        var columns = await db.PmColumns.AsNoTracking().Where(c => c.ProjectId == projectId).OrderBy(c => c.SortOrder).ToListAsync();
        var sprints = await db.PmSprints.AsNoTracking().Where(s => s.ProjectId == projectId).OrderBy(s => s.Id).ToListAsync();
        var tasks = await db.PmTasks.AsNoTracking().Where(t => t.ProjectId == projectId && !t.IsArchived).ToListAsync();
        var cards = await CardsAsync(db, project, columns, tasks);
        var labels = cards.SelectMany(c => c.Labels).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(l => l).ToList();
        var isMember = summary.IsMember;
        return new PmBoard(summary,
            columns.Select(c => new PmColumnView(c.Id, c.Name, c.Category, c.SortOrder, c.WipLimit)).ToList(),
            sprints.Select(ToView).ToList(),
            cards.Where(c => c.Type != PmTaskTypes.Epic).OrderBy(c => c.Rank).ToList(),
            cards.Where(c => c.Type == PmTaskTypes.Epic).OrderBy(c => c.Rank).ToList(),
            labels,
            user.IsKnown && (isMember || user.IsAdmin),
            user.IsKnown && (Same(project.LeadLogin, user.Login) || user.IsAdmin));
    }

    private static PmSprintView ToView(PmSprint s) => new(s.Id, s.Name, s.Goal, s.StartDate, s.EndDate, s.State, s.CompletedAtUtc);

    private static async Task<List<PmCard>> CardsAsync(AppDbContext db, PmProject project, List<PmColumn> columns, List<PmTask> tasks)
    {
        var ids = tasks.Select(t => t.Id).ToList();
        var checklist = await db.PmChecklistItems.AsNoTracking().Where(c => ids.Contains(c.TaskId))
            .GroupBy(c => c.TaskId).Select(g => new { g.Key, Done = g.Count(x => x.IsDone), Total = g.Count() }).ToDictionaryAsync(x => x.Key);
        var comments = await db.PmComments.AsNoTracking().Where(c => ids.Contains(c.TaskId))
            .GroupBy(c => c.TaskId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return tasks.Select(t => ToCard(project, columns, tasks, t, checklist.TryGetValue(t.Id, out var cl) ? (cl.Done, cl.Total) : (0, 0),
            comments.GetValueOrDefault(t.Id))).ToList();
    }

    private static PmCard ToCard(PmProject project, List<PmColumn> columns, List<PmTask> all, PmTask t, (int Done, int Total) checklist, int comments)
    {
        var column = columns.FirstOrDefault(c => c.Id == t.ColumnId);
        var category = column?.Category ?? PmCategories.Todo;
        var parent = t.ParentId is { } pid ? all.FirstOrDefault(x => x.Id == pid) : null;
        var epic = t.EpicId is { } eid ? all.FirstOrDefault(x => x.Id == eid) : null;
        var subtasks = all.Where(x => x.ParentId == t.Id && !x.IsArchived).ToList();
        var doneColumns = columns.Where(c => c.Category == PmCategories.Done).Select(c => c.Id).ToHashSet();
        return new PmCard(t.Id, t.ProjectId, $"{project.Key}-{t.Number}", t.Number, t.Type, t.Title,
            t.ColumnId, column?.Name ?? string.Empty, category, t.Priority, t.AssigneeLogin, t.AssigneeName, SplitLabels(t.Labels),
            t.StartDate, t.DueDate, t.StoryPoints, t.ParentId, parent is null ? string.Empty : $"{project.Key}-{parent.Number}",
            t.EpicId, epic?.Title ?? string.Empty, t.SprintId, t.Rank, t.Cover,
            checklist.Done, checklist.Total, comments, subtasks.Count(s => doneColumns.Contains(s.ColumnId)), subtasks.Count,
            t.CreatedAtUtc, t.UpdatedAtUtc, t.CompletedAtUtc,
            t.DueDate is { } due && due.Date < DateTime.Today && category != PmCategories.Done);
    }

    // ---------- Aufgaben ----------

    public async Task<PmTaskDetail?> GetTaskAsync(int taskId, PmUser user)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var task = await db.PmTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
            return null;
        var project = await db.PmProjects.AsNoTracking().FirstAsync(p => p.Id == task.ProjectId);
        var columns = await db.PmColumns.AsNoTracking().Where(c => c.ProjectId == project.Id).ToListAsync();
        var all = await db.PmTasks.AsNoTracking().Where(t => t.ProjectId == project.Id).ToListAsync();
        var cards = await CardsAsync(db, project, columns, all.Where(t => t.Id == taskId || t.ParentId == taskId).ToList());
        // Eltern- und Epic-Namen brauchen die ganze Liste.
        cards = cards.Select(c => ToCard(project, columns, all, all.First(t => t.Id == c.Id), (c.ChecklistDone, c.ChecklistTotal), c.CommentCount)).ToList();
        var card = cards.First(c => c.Id == taskId);
        var checklist = await db.PmChecklistItems.AsNoTracking().Where(c => c.TaskId == taskId).OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new PmChecklistView(c.Id, c.Text, c.IsDone)).ToListAsync();
        var comments = await db.PmComments.AsNoTracking().Where(c => c.TaskId == taskId).OrderBy(c => c.CreatedAtUtc)
            .Select(c => new PmCommentView(c.Id, c.Body, c.AuthorLogin, c.AuthorName, c.CreatedAtUtc)).ToListAsync();
        var activity = await db.PmActivities.AsNoTracking().Where(a => a.TaskId == taskId).OrderByDescending(a => a.AtUtc).Take(50)
            .Select(a => new PmActivityView(a.Id, a.TaskId, card.Key, a.Login, a.Name, a.Text, a.AtUtc)).ToListAsync();
        var subtasks = cards.Where(c => c.ParentId == taskId && c.Id != taskId).OrderBy(c => c.Rank).ToList();
        var canEdit = await CanEditAsync(db, user, project.Id);
        return new PmTaskDetail(card, task.Description, task.ReporterLogin, task.ReporterName, checklist, comments, activity, subtasks, canEdit);
    }

    public async Task<int> CreateTaskAsync(PmUser user, int projectId, PmTaskInput input)
    {
        RequireUser(user);
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsArchived) ?? throw NotFound();
            await RequireEditAsync(db, user, projectId);
            var columns = await db.PmColumns.Where(c => c.ProjectId == projectId).OrderBy(c => c.SortOrder).ToListAsync();
            if (columns.Count == 0)
                throw new PmException("Das Projekt hat keine Spalten.", "The project has no columns.");
            var title = ValidateTitle(input.Title);
            var column = input.ColumnId is { } cid ? columns.FirstOrDefault(c => c.Id == cid) : null;
            column ??= columns.FirstOrDefault(c => c.Category == PmCategories.Todo) ?? columns[0];
            var now = DateTime.UtcNow;
            var maxRank = await db.PmTasks.Where(t => t.ProjectId == projectId).Select(t => (double?)t.Rank).MaxAsync() ?? 0;
            var type = PmTaskTypes.All.Contains(input.Type) ? input.Type : PmTaskTypes.Task;
            if (input.ParentId is not null)
                type = PmTaskTypes.Subtask;
            var task = new PmTask
            {
                ProjectId = projectId,
                Number = project.NextNumber++,
                Type = type,
                Title = title,
                Description = Limit(input.Description, DescriptionMax),
                ColumnId = column.Id,
                Priority = PmPriorities.All.Contains(input.Priority) ? input.Priority : PmPriorities.Medium,
                ReporterLogin = user.Login,
                ReporterName = user.DisplayName,
                Labels = string.Join(",", NormalizeLabels(input.Labels)),
                StartDate = input.StartDate?.Date,
                DueDate = input.DueDate?.Date,
                StoryPoints = Math.Clamp(input.StoryPoints, 0, 100),
                ParentId = await ValidRelationAsync(db, projectId, input.ParentId, false),
                EpicId = type == PmTaskTypes.Epic ? null : await ValidRelationAsync(db, projectId, input.EpicId, true),
                SprintId = await ValidSprintAsync(db, projectId, input.SprintId),
                Rank = maxRank + 1000,
                Cover = PmStyle.Covers.Contains(input.Cover) ? input.Cover : string.Empty,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CompletedAtUtc = column.Category == PmCategories.Done ? now : null
            };
            await ApplyAssigneeAsync(db, task, input.AssigneeLogin);
            db.PmTasks.Add(task);
            project.UpdatedAtUtc = now;
            await db.SaveChangesAsync();
            Log(db, projectId, task.Id, user, $"hat {project.Key}-{task.Number} angelegt: {task.Title}");
            await db.SaveChangesAsync();
            _notifier.Publish(projectId);
            return task.Id;
        }
        finally
        {
            WriteGate.Release();
        }
    }

    public async Task UpdateTaskAsync(PmUser user, int taskId, PmTaskInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId) ?? throw NotFound();
        await RequireEditAsync(db, user, task.ProjectId);
        var project = await db.PmProjects.FirstAsync(p => p.Id == task.ProjectId);
        var key = $"{project.Key}-{task.Number}";
        var changes = new List<string>();

        var title = ValidateTitle(input.Title);
        if (title != task.Title) { changes.Add("Titel"); task.Title = title; }
        var description = Limit(input.Description, DescriptionMax);
        if (description != task.Description) { changes.Add("Beschreibung"); task.Description = description; }
        var type = PmTaskTypes.All.Contains(input.Type) ? input.Type : task.Type;
        if (task.ParentId is null && type == PmTaskTypes.Subtask) type = PmTaskTypes.Task;
        if (type != task.Type) { changes.Add($"Typ {TypeDe(type)}"); task.Type = type; }
        var priority = PmPriorities.All.Contains(input.Priority) ? input.Priority : task.Priority;
        if (priority != task.Priority) { changes.Add($"Priorität {PriorityDe(priority)}"); task.Priority = priority; }
        var labels = string.Join(",", NormalizeLabels(input.Labels));
        if (labels != task.Labels) { changes.Add("Labels"); task.Labels = labels; }
        if (input.StartDate?.Date != task.StartDate) { changes.Add("Start"); task.StartDate = input.StartDate?.Date; }
        if (input.DueDate?.Date != task.DueDate) { changes.Add(input.DueDate is { } d ? $"fällig {d:dd.MM.yyyy}" : "Fälligkeit entfernt"); task.DueDate = input.DueDate?.Date; }
        var points = Math.Clamp(input.StoryPoints, 0, 100);
        if (points != task.StoryPoints) { changes.Add($"{points} Punkte"); task.StoryPoints = points; }
        var cover = PmStyle.Covers.Contains(input.Cover) ? input.Cover : task.Cover;
        if (cover != task.Cover) { changes.Add("Farbe"); task.Cover = cover; }
        var epic = task.Type == PmTaskTypes.Epic ? null : await ValidRelationAsync(db, task.ProjectId, input.EpicId, true);
        if (epic == task.Id) epic = null;
        if (epic != task.EpicId) { changes.Add("Epic"); task.EpicId = epic; }
        // Unveraenderte Verweise nicht neu pruefen: erledigte Aufgaben behalten ihren abgeschlossenen Sprint,
        // und Aufgaben ausgetretener Mitglieder muessen sich trotzdem bearbeiten lassen.
        if (input.SprintId != task.SprintId)
        {
            var sprint = await ValidSprintAsync(db, task.ProjectId, input.SprintId);
            changes.Add(sprint is null ? "zurück in den Backlog" : "Sprint");
            task.SprintId = sprint;
        }
        if (!Same(NormalizeLogin(input.AssigneeLogin), task.AssigneeLogin))
        {
            await ApplyAssigneeAsync(db, task, input.AssigneeLogin);
            changes.Add(task.AssigneeLogin.Length == 0 ? "Zuweisung entfernt" : $"zugewiesen an {task.AssigneeName}");
        }
        if (input.ColumnId is { } columnId && columnId != task.ColumnId)
        {
            var column = await db.PmColumns.FirstOrDefaultAsync(c => c.Id == columnId && c.ProjectId == task.ProjectId) ?? throw NotFound();
            SetColumn(task, column);
            changes.Add($"Status {column.Name}");
        }

        if (changes.Count == 0)
            return;
        task.UpdatedAtUtc = DateTime.UtcNow;
        project.UpdatedAtUtc = task.UpdatedAtUtc;
        Log(db, task.ProjectId, task.Id, user, $"hat {key} geändert: {string.Join(", ", changes)}");
        await db.SaveChangesAsync();
        _notifier.Publish(task.ProjectId);
    }

    public async Task MoveTaskAsync(PmUser user, int taskId, int columnId, int? beforeTaskId)
    {
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId) ?? throw NotFound();
            await RequireEditAsync(db, user, task.ProjectId);
            var column = await db.PmColumns.FirstOrDefaultAsync(c => c.Id == columnId && c.ProjectId == task.ProjectId) ?? throw NotFound();
            var siblings = await db.PmTasks.Where(t => t.ProjectId == task.ProjectId && t.ColumnId == columnId && !t.IsArchived && t.Id != taskId)
                .OrderBy(t => t.Rank).Select(t => new { t.Id, t.Rank }).ToListAsync();
            task.Rank = RankBetween(siblings.Select(s => (s.Id, s.Rank)).ToList(), beforeTaskId);
            var moved = task.ColumnId != columnId;
            SetColumn(task, column);
            task.UpdatedAtUtc = DateTime.UtcNow;
            if (moved)
            {
                var key = await db.PmProjects.Where(p => p.Id == task.ProjectId).Select(p => p.Key).FirstAsync();
                Log(db, task.ProjectId, task.Id, user, $"hat {key}-{task.Number} nach „{column.Name}“ verschoben");
            }
            await db.SaveChangesAsync();
            _notifier.Publish(task.ProjectId);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    public async Task PlanTaskAsync(PmUser user, int taskId, int? sprintId, int? beforeTaskId)
    {
        await WriteGate.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId) ?? throw NotFound();
            await RequireEditAsync(db, user, task.ProjectId);
            var sprint = await ValidSprintAsync(db, task.ProjectId, sprintId);
            var siblings = await db.PmTasks.Where(t => t.ProjectId == task.ProjectId && t.SprintId == sprint && !t.IsArchived && t.Id != taskId && t.Type != PmTaskTypes.Epic)
                .OrderBy(t => t.Rank).Select(t => new { t.Id, t.Rank }).ToListAsync();
            task.Rank = RankBetween(siblings.Select(s => (s.Id, s.Rank)).ToList(), beforeTaskId);
            if (task.SprintId != sprint)
            {
                var key = await db.PmProjects.Where(p => p.Id == task.ProjectId).Select(p => p.Key).FirstAsync();
                var text = sprint is null
                    ? $"hat {key}-{task.Number} zurück in den Backlog gelegt"
                    : $"hat {key}-{task.Number} in {await db.PmSprints.Where(s => s.Id == sprint).Select(s => s.Name).FirstAsync()} eingeplant";
                Log(db, task.ProjectId, task.Id, user, text);
            }
            task.SprintId = sprint;
            task.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            _notifier.Publish(task.ProjectId);
        }
        finally
        {
            WriteGate.Release();
        }
    }

    internal static double RankBetween(IReadOnlyList<(int Id, double Rank)> ordered, int? beforeTaskId)
    {
        if (ordered.Count == 0)
            return 1000;
        var index = beforeTaskId is { } before ? ordered.ToList().FindIndex(x => x.Id == before) : -1;
        if (index < 0)
            return ordered[^1].Rank + 1000;
        var next = ordered[index].Rank;
        var previous = index == 0 ? next - 2000 : ordered[index - 1].Rank;
        return (previous + next) / 2;
    }

    private static void SetColumn(PmTask task, PmColumn column)
    {
        var wasDone = task.CompletedAtUtc is not null;
        task.ColumnId = column.Id;
        if (column.Category == PmCategories.Done && !wasDone)
            task.CompletedAtUtc = DateTime.UtcNow;
        else if (column.Category != PmCategories.Done)
            task.CompletedAtUtc = null;
    }

    public async Task SetTaskArchivedAsync(PmUser user, int taskId, bool archived)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId) ?? throw NotFound();
        await RequireEditAsync(db, user, task.ProjectId);
        task.IsArchived = archived;
        task.UpdatedAtUtc = DateTime.UtcNow;
        if (archived)
        {
            foreach (var child in await db.PmTasks.Where(t => t.ParentId == taskId).ToListAsync())
                child.IsArchived = true;
            foreach (var child in await db.PmTasks.Where(t => t.EpicId == taskId).ToListAsync())
                child.EpicId = null;
        }
        var key = await db.PmProjects.Where(p => p.Id == task.ProjectId).Select(p => p.Key).FirstAsync();
        Log(db, task.ProjectId, task.Id, user, archived ? $"hat {key}-{task.Number} archiviert" : $"hat {key}-{task.Number} wiederhergestellt");
        await db.SaveChangesAsync();
        _notifier.Publish(task.ProjectId);
    }

    // ---------- Checkliste und Kommentare ----------

    public async Task<int> AddChecklistItemAsync(PmUser user, int taskId, string text)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length is 0 or > 300)
            throw new PmException("Ein Checklistenpunkt braucht 1 bis 300 Zeichen.", "A checklist item needs 1 to 300 characters.");
        await using var db = await _dbFactory.CreateDbContextAsync();
        var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId) ?? throw NotFound();
        await RequireEditAsync(db, user, task.ProjectId);
        var order = (await db.PmChecklistItems.Where(c => c.TaskId == taskId).Select(c => (int?)c.SortOrder).MaxAsync() ?? 0) + 10;
        var item = new PmChecklistItem { TaskId = taskId, Text = text, SortOrder = order };
        db.PmChecklistItems.Add(item);
        task.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        _notifier.Publish(task.ProjectId);
        return item.Id;
    }

    public async Task ToggleChecklistItemAsync(PmUser user, int itemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var item = await db.PmChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId) ?? throw NotFound();
        var task = await db.PmTasks.FirstAsync(t => t.Id == item.TaskId);
        await RequireEditAsync(db, user, task.ProjectId);
        item.IsDone = !item.IsDone;
        task.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        _notifier.Publish(task.ProjectId);
    }

    public async Task DeleteChecklistItemAsync(PmUser user, int itemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var item = await db.PmChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId) ?? throw NotFound();
        var task = await db.PmTasks.FirstAsync(t => t.Id == item.TaskId);
        await RequireEditAsync(db, user, task.ProjectId);
        db.PmChecklistItems.Remove(item);
        await db.SaveChangesAsync();
        _notifier.Publish(task.ProjectId);
    }

    public async Task<int> AddCommentAsync(PmUser user, int taskId, string body)
    {
        RequireUser(user);
        body = (body ?? string.Empty).Trim();
        if (body.Length is 0 or > CommentMax)
            throw new PmException($"Ein Kommentar braucht 1 bis {CommentMax} Zeichen.", $"A comment needs 1 to {CommentMax} characters.");
        await using var db = await _dbFactory.CreateDbContextAsync();
        var task = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == taskId) ?? throw NotFound();
        // Kommentieren darf jeder Angemeldete, wie in Jira mit offenem Projekt: Rueckfragen von aussen sind erwuenscht.
        var comment = new PmComment { TaskId = taskId, Body = body, AuthorLogin = user.Login, AuthorName = user.DisplayName, CreatedAtUtc = DateTime.UtcNow };
        db.PmComments.Add(comment);
        task.UpdatedAtUtc = comment.CreatedAtUtc;
        var key = await db.PmProjects.Where(p => p.Id == task.ProjectId).Select(p => p.Key).FirstAsync();
        Log(db, task.ProjectId, task.Id, user, $"hat {key}-{task.Number} kommentiert");
        await db.SaveChangesAsync();
        _notifier.Publish(task.ProjectId);
        return comment.Id;
    }

    // ---------- Mitglieder ----------

    public async Task AddMemberAsync(PmUser user, int projectId, string login, string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw NotFound();
        await RequireManageAsync(db, user, project);
        login = NormalizeLogin(login);
        if (login.Length == 0)
            throw new PmException("Bitte eine Person wählen.", "Please choose a person.");
        if (await db.PmMembers.AnyAsync(m => m.ProjectId == projectId && m.Login == login))
            return;
        name = string.IsNullOrWhiteSpace(name) ? login : name.Trim();
        db.PmMembers.Add(new PmMember { ProjectId = projectId, Login = login, Name = name, Role = PmRoles.Member, AddedAtUtc = DateTime.UtcNow });
        Log(db, projectId, null, user, $"hat {name} ins Team aufgenommen");
        await db.SaveChangesAsync();
        _notifier.Publish(projectId);
    }

    public async Task RemoveMemberAsync(PmUser user, int projectId, string login)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw NotFound();
        await RequireManageAsync(db, user, project);
        if (Same(project.LeadLogin, login))
            throw new PmException("Die Projektleitung kann nicht aus dem Team entfernt werden.", "The project lead stays in the team.");
        var member = await db.PmMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.Login == login);
        if (member is null)
            return;
        db.PmMembers.Remove(member);
        Log(db, projectId, null, user, $"hat {member.Name} aus dem Team genommen");
        await db.SaveChangesAsync();
        _notifier.Publish(projectId);
    }

    public async Task<IReadOnlyList<PmPerson>> GetKnownPeopleAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var members = await db.PmMembers.AsNoTracking().Select(m => new { m.Login, m.Name }).ToListAsync();
        var forum = await db.ForumPosts.AsNoTracking().Select(p => new { Login = p.AuthorLogin, Name = p.AuthorName }).ToListAsync();
        var comments = await db.ForumComments.AsNoTracking().Select(p => new { Login = p.AuthorLogin, Name = p.AuthorName }).ToListAsync();
        return members.Concat(forum).Concat(comments)
            .Where(x => x.Login.Length > 0 && x.Login != "system")
            .GroupBy(x => x.Login, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PmPerson(g.Key, g.First().Name))
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // ---------- Spalten ----------

    public async Task<int> AddColumnAsync(PmUser user, int projectId, string name, string category)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw NotFound();
        await RequireManageAsync(db, user, project);
        name = ValidateColumnName(name);
        var order = (await db.PmColumns.Where(c => c.ProjectId == projectId).Select(c => (int?)c.SortOrder).MaxAsync() ?? 0) + 10;
        var column = new PmColumn { ProjectId = projectId, Name = name, Category = PmCategories.All.Contains(category) ? category : PmCategories.InProgress, SortOrder = order };
        db.PmColumns.Add(column);
        Log(db, projectId, null, user, $"hat die Spalte „{name}“ angelegt");
        await db.SaveChangesAsync();
        _notifier.Publish(projectId);
        return column.Id;
    }

    public async Task UpdateColumnAsync(PmUser user, int columnId, string name, string category, int wipLimit)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var column = await db.PmColumns.FirstOrDefaultAsync(c => c.Id == columnId) ?? throw NotFound();
        var project = await db.PmProjects.FirstAsync(p => p.Id == column.ProjectId);
        await RequireManageAsync(db, user, project);
        column.Name = ValidateColumnName(name);
        if (PmCategories.All.Contains(category))
            column.Category = category;
        column.WipLimit = Math.Clamp(wipLimit, 0, 99);
        await db.SaveChangesAsync();
        _notifier.Publish(project.Id);
    }

    public async Task MoveColumnAsync(PmUser user, int columnId, int direction)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var column = await db.PmColumns.FirstOrDefaultAsync(c => c.Id == columnId) ?? throw NotFound();
        var project = await db.PmProjects.FirstAsync(p => p.Id == column.ProjectId);
        await RequireManageAsync(db, user, project);
        var columns = await db.PmColumns.Where(c => c.ProjectId == project.Id).OrderBy(c => c.SortOrder).ToListAsync();
        var index = columns.FindIndex(c => c.Id == columnId);
        var target = index + Math.Sign(direction);
        if (target < 0 || target >= columns.Count)
            return;
        (columns[index], columns[target]) = (columns[target], columns[index]);
        for (var i = 0; i < columns.Count; i++)
            columns[i].SortOrder = (i + 1) * 10;
        await db.SaveChangesAsync();
        _notifier.Publish(project.Id);
    }

    public async Task DeleteColumnAsync(PmUser user, int columnId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var column = await db.PmColumns.FirstOrDefaultAsync(c => c.Id == columnId) ?? throw NotFound();
        var project = await db.PmProjects.FirstAsync(p => p.Id == column.ProjectId);
        await RequireManageAsync(db, user, project);
        var others = await db.PmColumns.Where(c => c.ProjectId == project.Id && c.Id != columnId).OrderBy(c => c.SortOrder).ToListAsync();
        if (others.Count == 0)
            throw new PmException("Die letzte Spalte kann nicht gelöscht werden.", "The last column cannot be deleted.");
        // Karten wandern in die erste Spalte derselben Art, sonst in die erste Spalte.
        var target = others.FirstOrDefault(c => c.Category == column.Category) ?? others[0];
        foreach (var task in await db.PmTasks.Where(t => t.ColumnId == columnId).ToListAsync())
            SetColumn(task, target);
        db.PmColumns.Remove(column);
        Log(db, project.Id, null, user, $"hat die Spalte „{column.Name}“ gelöscht und die Karten nach „{target.Name}“ verschoben");
        await db.SaveChangesAsync();
        _notifier.Publish(project.Id);
    }

    // ---------- Sprints ----------

    public async Task<int> CreateSprintAsync(PmUser user, int projectId, string name, string goal, DateTime? start, DateTime? end)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = await db.PmProjects.FirstOrDefaultAsync(p => p.Id == projectId) ?? throw NotFound();
        await RequireEditAsync(db, user, projectId);
        var count = await db.PmSprints.CountAsync(s => s.ProjectId == projectId);
        name = string.IsNullOrWhiteSpace(name) ? $"{project.Key} Sprint {count + 1}" : name.Trim();
        var sprint = new PmSprint { ProjectId = projectId, Name = Limit(name, 80), Goal = Limit(goal, 500), StartDate = start?.Date, EndDate = end?.Date, State = PmSprintStates.Planned };
        db.PmSprints.Add(sprint);
        Log(db, projectId, null, user, $"hat {sprint.Name} angelegt");
        await db.SaveChangesAsync();
        _notifier.Publish(projectId);
        return sprint.Id;
    }

    public async Task StartSprintAsync(PmUser user, int sprintId, DateTime start, DateTime end, string goal)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sprint = await db.PmSprints.FirstOrDefaultAsync(s => s.Id == sprintId) ?? throw NotFound();
        await RequireEditAsync(db, user, sprint.ProjectId);
        if (sprint.State != PmSprintStates.Planned)
            throw new PmException("Nur geplante Sprints lassen sich starten.", "Only planned sprints can be started.");
        if (await db.PmSprints.AnyAsync(s => s.ProjectId == sprint.ProjectId && s.State == PmSprintStates.Active))
            throw new PmException("Es läuft bereits ein Sprint. Schliesse ihn zuerst ab.", "A sprint is already running. Complete it first.");
        if (end.Date < start.Date)
            throw new PmException("Das Ende liegt vor dem Start.", "The end is before the start.");
        sprint.State = PmSprintStates.Active;
        sprint.StartDate = start.Date;
        sprint.EndDate = end.Date;
        sprint.Goal = Limit(goal, 500);
        Log(db, sprint.ProjectId, null, user, $"hat {sprint.Name} gestartet");
        await db.SaveChangesAsync();
        _notifier.Publish(sprint.ProjectId);
    }

    public async Task<int> CompleteSprintAsync(PmUser user, int sprintId, int? moveOpenToSprintId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sprint = await db.PmSprints.FirstOrDefaultAsync(s => s.Id == sprintId) ?? throw NotFound();
        await RequireEditAsync(db, user, sprint.ProjectId);
        if (sprint.State != PmSprintStates.Active)
            throw new PmException("Nur ein laufender Sprint lässt sich abschliessen.", "Only an active sprint can be completed.");
        var target = moveOpenToSprintId is { } tid && tid != sprintId
            ? await db.PmSprints.Where(s => s.Id == tid && s.ProjectId == sprint.ProjectId && s.State == PmSprintStates.Planned).Select(s => (int?)s.Id).FirstOrDefaultAsync()
            : null;
        var doneColumns = await db.PmColumns.Where(c => c.ProjectId == sprint.ProjectId && c.Category == PmCategories.Done).Select(c => c.Id).ToListAsync();
        var open = await db.PmTasks.Where(t => t.SprintId == sprintId && !t.IsArchived && !doneColumns.Contains(t.ColumnId)).ToListAsync();
        foreach (var task in open)
            task.SprintId = target;
        sprint.State = PmSprintStates.Closed;
        sprint.CompletedAtUtc = DateTime.UtcNow;
        Log(db, sprint.ProjectId, null, user, $"hat {sprint.Name} abgeschlossen und {open.Count} offene Aufgaben verschoben");
        await db.SaveChangesAsync();
        _notifier.Publish(sprint.ProjectId);
        return open.Count;
    }

    /// <summary>
    /// Burndown wie Jira: Rest an Story Points (ohne Punkte: Anzahl Aufgaben) je Tag des Sprints.
    /// Gerechnet mit der heutigen Sprintbelegung; nachtraeglich hinzugefuegte Aufgaben zaehlen ab Start mit.
    /// </summary>
    public static IReadOnlyList<PmBurndownPoint> Burndown(PmSprintView sprint, IReadOnlyList<PmCard> sprintTasks, DateTime today)
    {
        if (sprint.StartDate is not { } start || sprint.EndDate is not { } end || end < start)
            return [];
        var usePoints = sprintTasks.Sum(t => t.StoryPoints) > 0;
        double Weight(PmCard t) => usePoints ? t.StoryPoints : 1;
        var total = sprintTasks.Sum(Weight);
        var days = (end.Date - start.Date).Days;
        var points = new List<PmBurndownPoint>();
        for (var i = 0; i <= days; i++)
        {
            var day = start.Date.AddDays(i);
            var ideal = days == 0 ? 0 : total * (1 - (double)i / days);
            double? remaining = day > today.Date
                ? null
                : sprintTasks.Where(t => t.CompletedAtUtc is not { } done || done.ToLocalTime().Date > day).Sum(Weight);
            points.Add(new PmBurndownPoint(day, Math.Round(ideal, 1), remaining));
        }
        return points;
    }

    // ---------- Uebersichten ----------

    public async Task<IReadOnlyList<PmMyTask>> GetMyTasksAsync(PmUser user)
    {
        if (!user.IsKnown)
            return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        var mine = await db.PmTasks.AsNoTracking().Where(t => t.AssigneeLogin == user.Login && !t.IsArchived).ToListAsync();
        var projectIds = mine.Select(t => t.ProjectId).Distinct().ToList();
        var projects = await db.PmProjects.AsNoTracking().Where(p => projectIds.Contains(p.Id) && !p.IsArchived).ToListAsync();
        var result = new List<PmMyTask>();
        foreach (var project in projects)
        {
            var columns = await db.PmColumns.AsNoTracking().Where(c => c.ProjectId == project.Id).ToListAsync();
            var all = await db.PmTasks.AsNoTracking().Where(t => t.ProjectId == project.Id).ToListAsync();
            var cards = await CardsAsync(db, project, columns, all);
            result.AddRange(cards.Where(c => Same(c.AssigneeLogin, user.Login) && c.Category != PmCategories.Done && c.Type != PmTaskTypes.Epic && all.First(t => t.Id == c.Id).IsArchived == false)
                .Select(c => new PmMyTask(c, project.Name, project.Color)));
        }
        return result
            .OrderBy(t => t.Card.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => PmPriorities.Weight(t.Card.Priority))
            .ToList();
    }

    public async Task<IReadOnlyList<PmActivityView>> GetActivityAsync(int projectId, int take = 40)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var key = await db.PmProjects.Where(p => p.Id == projectId).Select(p => p.Key).FirstOrDefaultAsync() ?? string.Empty;
        var numbers = await db.PmTasks.Where(t => t.ProjectId == projectId).ToDictionaryAsync(t => t.Id, t => t.Number);
        var list = await db.PmActivities.AsNoTracking().Where(a => a.ProjectId == projectId).OrderByDescending(a => a.AtUtc).Take(take).ToListAsync();
        return list.Select(a => new PmActivityView(a.Id, a.TaskId,
            a.TaskId is { } tid && numbers.TryGetValue(tid, out var n) ? $"{key}-{n}" : string.Empty, a.Login, a.Name, a.Text, a.AtUtc)).ToList();
    }

    // ---------- Hilfen ----------

    private static (string Name, string Key) ValidateProject(PmProjectInput input)
    {
        var name = (input.Name ?? string.Empty).Trim();
        if (name.Length is < 3 or > 80)
            throw new PmException("Der Projektname braucht 3 bis 80 Zeichen.", "The project name needs 3 to 80 characters.");
        var key = string.IsNullOrWhiteSpace(input.Key) ? SuggestKey(name) : input.Key.Trim().ToUpperInvariant();
        if (!KeyRegex().IsMatch(key))
            throw new PmException("Der Schlüssel braucht 2 bis 6 Grossbuchstaben oder Ziffern und beginnt mit einem Buchstaben, z. B. LOG.", "The key needs 2 to 6 capital letters or digits and starts with a letter, e.g. LOG.");
        return (name, key);
    }

    public static string SuggestKey(string name)
    {
        var words = WordRegex().Matches((name ?? string.Empty).ToUpperInvariant()
                .Replace("Ä", "A").Replace("Ö", "O").Replace("Ü", "U"))
            .Select(m => m.Value).Where(w => w.Length > 0).ToList();
        if (words.Count == 0)
            return "PRJ";
        var key = words.Count >= 2
            ? string.Concat(words.Take(4).Select(w => w[0]))
            : words[0][..Math.Min(4, words[0].Length)];
        if (!char.IsLetter(key[0]))
            key = "P" + key;
        if (key.Length < 2)
            key += "X";
        return key[..Math.Min(6, key.Length)];
    }

    private static string ValidateTitle(string? title)
    {
        title = (title ?? string.Empty).Trim();
        if (title.Length is 0 or > TitleMax)
            throw new PmException($"Der Titel braucht 1 bis {TitleMax} Zeichen.", $"The title needs 1 to {TitleMax} characters.");
        return title;
    }

    private static string ValidateColumnName(string? name)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length is 0 or > 40)
            throw new PmException("Der Spaltenname braucht 1 bis 40 Zeichen.", "The column name needs 1 to 40 characters.");
        return name;
    }

    private static string Limit(string? text, int max)
    {
        text = (text ?? string.Empty).Trim();
        return text.Length <= max ? text : text[..max];
    }

    public static IReadOnlyList<string> NormalizeLabels(string? raw) =>
        (raw ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => l.Length > 25 ? l[..25] : l)
            .Where(l => l.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

    private static IReadOnlyList<string> SplitLabels(string? stored) =>
        string.IsNullOrWhiteSpace(stored) ? [] : stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string NormalizeLogin(string? login) => Forum.ForumUserDirectory.ShortName(login);

    private static bool Same(string? a, string? b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private static async Task ApplyAssigneeAsync(AppDbContext db, PmTask task, string? login)
    {
        login = NormalizeLogin(login);
        if (login.Length == 0)
        {
            task.AssigneeLogin = string.Empty;
            task.AssigneeName = string.Empty;
            return;
        }
        var member = await db.PmMembers.FirstOrDefaultAsync(m => m.ProjectId == task.ProjectId && m.Login == login)
            ?? throw new PmException("Aufgaben lassen sich nur Mitgliedern des Projekts zuweisen.", "Tasks can only be assigned to project members.");
        task.AssigneeLogin = member.Login;
        task.AssigneeName = member.Name;
    }

    private static async Task<int?> ValidRelationAsync(AppDbContext db, int projectId, int? id, bool mustBeEpic)
    {
        if (id is not { } value)
            return null;
        var target = await db.PmTasks.FirstOrDefaultAsync(t => t.Id == value && t.ProjectId == projectId && !t.IsArchived)
            ?? throw new PmException("Die verknüpfte Aufgabe existiert nicht mehr.", "The linked task does not exist.");
        if (mustBeEpic && target.Type != PmTaskTypes.Epic)
            throw new PmException("Bitte ein Epic wählen.", "Please choose an epic.");
        if (!mustBeEpic && target.Type is PmTaskTypes.Epic or PmTaskTypes.Subtask)
            throw new PmException("Eine Unteraufgabe gehört zu einer Aufgabe, einer Story oder einem Fehler.", "Subtasks belong to a task, story or bug.");
        return value;
    }

    private static async Task<int?> ValidSprintAsync(AppDbContext db, int projectId, int? sprintId)
    {
        if (sprintId is not { } id)
            return null;
        var ok = await db.PmSprints.AnyAsync(s => s.Id == id && s.ProjectId == projectId && s.State != PmSprintStates.Closed);
        return ok ? id : throw new PmException("Der Sprint ist abgeschlossen oder gehört zu einem anderen Projekt.", "The sprint is closed or belongs to another project.");
    }

    // Der Verlauf wird deutsch gespeichert; interne Werte wie "High" oder "Bug" darin auf Deutsch.
    private static string TypeDe(string type) => type switch
    {
        PmTaskTypes.Story => "Story",
        PmTaskTypes.Bug => "Fehler",
        PmTaskTypes.Epic => "Epic",
        PmTaskTypes.Subtask => "Unteraufgabe",
        _ => "Aufgabe"
    };

    private static string PriorityDe(string priority) => priority switch
    {
        PmPriorities.Highest => "sehr hoch",
        PmPriorities.High => "hoch",
        PmPriorities.Low => "niedrig",
        PmPriorities.Lowest => "sehr niedrig",
        _ => "mittel"
    };

    private static void Log(AppDbContext db, int projectId, int? taskId, PmUser user, string text) =>
        db.PmActivities.Add(new PmActivity { ProjectId = projectId, TaskId = taskId, Login = user.Login, Name = user.DisplayName, Text = text, AtUtc = DateTime.UtcNow });

    private static async Task<bool> CanEditAsync(AppDbContext db, PmUser user, int projectId) =>
        user.IsKnown && (user.IsAdmin || await db.PmMembers.AnyAsync(m => m.ProjectId == projectId && m.Login == user.Login));

    private static async Task RequireEditAsync(AppDbContext db, PmUser user, int projectId)
    {
        RequireUser(user);
        if (!await CanEditAsync(db, user, projectId))
            throw new PmException("Ändern dürfen nur Mitglieder des Projekts. Die Projektleitung kann dich aufnehmen.", "Only project members can make changes. The project lead can add you.");
    }

    private static Task RequireManageAsync(AppDbContext db, PmUser user, PmProject project)
    {
        RequireUser(user);
        if (!user.IsAdmin && !Same(project.LeadLogin, user.Login))
            throw new PmException("Das darf nur die Projektleitung.", "Only the project lead can do this.");
        return Task.CompletedTask;
    }

    private static void RequireUser(PmUser user)
    {
        if (!user.IsKnown)
            throw new PmException("Ohne Windows-Anmeldung kann man nur lesen.", "Without a Windows sign-in you can only read.");
    }

    private static PmException NotFound() => new("Dieser Eintrag existiert nicht mehr.", "This no longer exists.");

    [GeneratedRegex("^[A-Z][A-Z0-9]{1,5}$")] private static partial Regex KeyRegex();
    [GeneratedRegex("[A-Z0-9]+")] private static partial Regex WordRegex();
}
