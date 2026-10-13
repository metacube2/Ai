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
    // Seit 2026-10-13 (Wunsch Ingo): Public sehen alle, Private nur das Team (auch Admins nicht).
    public string Visibility { get; set; } = PmVisibility.Public;
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
    // Herkunft einer importierten Aufgabe, z. B. "WT-95" fuer Zeile 95 der Wochen_Todo (2026-10-13).
    public string ExternalRef { get; set; } = string.Empty;
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

// HERMES zugeschnitten (2026-10-13, Wunsch Ingo): eine Zeile je Projekt mit denselben Angaben wie
// projektmanagement/Vorhaben_HERMES.tsv. Eigene Tabelle, damit PmProjects unveraendert bleibt.
public sealed class PmHermes
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Sponsor { get; set; } = string.Empty;
    public string UserRepresentative { get; set; } = string.Empty;
    public int EffortDays { get; set; }
    public string Traffic { get; set; } = string.Empty;
    public string StatusLine { get; set; } = string.Empty;
    public string NextDecision { get; set; } = string.Empty;
    public string DoneWhen { get; set; } = string.Empty;
    public DateTime? ReleasedOn { get; set; }
    public DateTime? AcceptedOn { get; set; }
    public DateTime? ClosedOn { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

// Einstufung wie im Blatt "HERMES Übersicht": Kleinauftrag unter ca. 5 PT, Vorhaben 5 bis 40 PT, Projekt darueber.
public static class PmHermesStages
{
    public const string SmallOrder = "SmallOrder";
    public const string Undertaking = "Undertaking";
    public const string Project = "Project";

    public static readonly string[] All = [SmallOrder, Undertaking, Project];

    public static string Suggest(int effortDays) => effortDays switch
    {
        <= 0 => string.Empty,
        < 5 => SmallOrder,
        <= 40 => Undertaking,
        _ => Project
    };
}

public static class PmTraffic
{
    public const string Green = "Green";
    public const string Yellow = "Yellow";
    public const string Red = "Red";

    public static readonly string[] All = [Green, Yellow, Red];
}

// Phase aus den drei Entscheidungspunkten: vor (1) Freigabe Initialisierung, danach Umsetzung,
// nach (2) Abnahme Einfuehrung, nach (3) Abschluss abgeschlossen.
public static class PmHermesPhases
{
    public const string Initiation = "Initiation";
    public const string Execution = "Execution";
    public const string Introduction = "Introduction";
    public const string Closed = "Closed";

    public static string Of(DateTime? releasedOn, DateTime? acceptedOn, DateTime? closedOn) =>
        closedOn.HasValue ? Closed : acceptedOn.HasValue ? Introduction : releasedOn.HasValue ? Execution : Initiation;
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
    // Leser (2026-10-13): sieht ein privates Projekt, aendert aber nichts.
    public const string Viewer = "Viewer";
}

public static class PmVisibility
{
    public const string Public = "Public";
    public const string Private = "Private";

    public static readonly string[] All = [Public, Private];
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
