namespace TrafagSalesExporter.Models;

// Trafag Projekte (2026-10-09, Wunsch Ingo): ersetzt die "Poor Man's Project Management Suite".
// Das Beste aus Jira (Schluessel LOG-12, Typen, Epics, Backlog, Sprints, Burndown, Filter),
// Trello (Board mit Listen, Karten ziehen, Farben, Labels, Checklisten) und Microsoft Planner
// (Buckets, Fortschritt, Meine Aufgaben, Diagramme, Kalender). Doku docs/TRAFAG_PROJEKTE_2026-10-09.md.

public sealed class PmProject
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#C8501E";
    public string Icon { get; set; } = "ViewKanban";
    public string Template { get; set; } = PmTemplates.Kanban;
    public string LeadLogin { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int NextNumber { get; set; } = 1;
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class PmMember
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = PmRoles.Member;
    public DateTime AddedAtUtc { get; set; }
}

public sealed class PmColumn
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = PmCategories.Todo;
    public int SortOrder { get; set; }
    public int WipLimit { get; set; }
}

public sealed class PmSprint
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string State { get; set; } = PmSprintStates.Planned;
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class PmTask
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int Number { get; set; }
    public string Type { get; set; } = PmTaskTypes.Task;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ColumnId { get; set; }
    public string Priority { get; set; } = PmPriorities.Medium;
    public string AssigneeLogin { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public string ReporterLogin { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string Labels { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int StoryPoints { get; set; }
    public int? ParentId { get; set; }
    public int? EpicId { get; set; }
    public int? SprintId { get; set; }
    public double Rank { get; set; }
    public string Cover { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class PmChecklistItem
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PmComment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string AuthorLogin { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PmActivity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? TaskId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime AtUtc { get; set; }
}

public static class PmTemplates
{
    public const string Kanban = "Kanban";
    public const string Scrum = "Scrum";
    public const string Planner = "Planner";

    public static readonly string[] All = [Kanban, Scrum, Planner];
}

public static class PmRoles
{
    public const string Lead = "Lead";
    public const string Member = "Member";
}

public static class PmCategories
{
    public const string Todo = "Todo";
    public const string InProgress = "InProgress";
    public const string Done = "Done";

    public static readonly string[] All = [Todo, InProgress, Done];
}

public static class PmSprintStates
{
    public const string Planned = "Planned";
    public const string Active = "Active";
    public const string Closed = "Closed";
}

public static class PmTaskTypes
{
    public const string Task = "Task";
    public const string Story = "Story";
    public const string Bug = "Bug";
    public const string Epic = "Epic";
    public const string Subtask = "Subtask";

    public static readonly string[] All = [Task, Story, Bug, Epic, Subtask];
}

public static class PmPriorities
{
    public const string Highest = "Highest";
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";
    public const string Lowest = "Lowest";

    public static readonly string[] All = [Highest, High, Medium, Low, Lowest];

    public static int Weight(string? priority) => priority switch
    {
        Highest => 5,
        High => 4,
        Medium => 3,
        Low => 2,
        Lowest => 1,
        _ => 0
    };
}
