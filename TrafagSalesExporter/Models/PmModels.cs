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
    // Herkunft eines uebernommenen Vorhabens, z. B. "PM-01" aus der Wochen_Todo (2026-10-29).
    public string ExternalRef { get; set; } = string.Empty;
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
    // Smartsheet-Ansicht (2026-10-29): Fortschritt 0 bis 100, Meilenstein (Dauer 0) und Zeilenfolge im Raster.
    // Die Gliederung kommt aus Epic -> Aufgabe -> Unteraufgabe, nicht aus einer eigenen Spalte.
    public int PercentComplete { get; set; }
    public bool IsMilestone { get; set; }
    public double SheetOrder { get; set; }
    // Basisplan: eingefrorene Termine zum Vergleich mit dem aktuellen Plan (Phase 3).
    public DateTime? BaselineStart { get; set; }
    public DateTime? BaselineEnd { get; set; }
    // Projektstrukturplan fuer SAP PS (Sitzung 2026-10-29): Soll-Stunden, Soll-Franken und Kostenart je Element.
    public double PlannedHours { get; set; }
    public double PlannedCostChf { get; set; }
    public string CostType { get; set; } = string.Empty;
}

// Abhaengigkeit Ende-Anfang (Smartsheet "FS") mit Versatz in Arbeitstagen, 2026-10-29.
public sealed class PmTaskLink
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int PredecessorId { get; set; }
    public int SuccessorId { get; set; }
    public int LagDays { get; set; }
}

// Risikoregister je Projekt (2026-10-29, Wunsch Ingo): Wahrscheinlichkeit und Auswirkung 1 bis 5,
// Kritikalitaet aus dem Produkt (PmRiskLevels).
public sealed class PmRisk
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = PmRiskCategories.Schedule;
    public int Probability { get; set; } = 3;
    public int Impact { get; set; } = 3;
    public string Status { get; set; } = PmRiskStatuses.Open;
    public string OwnerLogin { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Mitigation { get; set; } = string.Empty;
    public string Contingency { get; set; } = string.Empty;
    public int? TaskId { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsArchived { get; set; }
    // Quantitativ (Sitzung 2026-10-29): Kosten bei Eintritt und eingeplante Reserve (Contingency) in CHF.
    public double CostImpactChf { get; set; }
    public double ContingencyChf { get; set; }
}

// Tagesstand des Risikoregisters fuer den Risiko-Burndown (eine Zeile je Projekt und Tag).
public sealed class PmRiskSnapshot
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime Day { get; set; }
    public int ActiveCount { get; set; }
    public int CriticalCount { get; set; }
    public int TotalScore { get; set; }
    public double ExpectedValueChf { get; set; }
    public double ContingencyChf { get; set; }
}

// Decision Log je Projekt (Sitzung 2026-10-29): wer hat wann was entschieden und warum.
public sealed class PmDecision
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string Alternatives { get; set; } = string.Empty;
    public string Status { get; set; } = PmDecisionStatuses.Proposed;
    public DateTime? DecidedOn { get; set; }
    public string DecidedBy { get; set; } = string.Empty;
    public int? TaskId { get; set; }
    public string AuthorLogin { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsArchived { get; set; }
}

public static class PmDecisionStatuses
{
    public const string Proposed = "Proposed";
    public const string Decided = "Decided";
    public const string Revoked = "Revoked";
    public const string Superseded = "Superseded";
    public static readonly string[] All = [Proposed, Decided, Revoked, Superseded];
}

public static class PmCostTypes
{
    public const string Capex = "Capex";
    public const string Opex = "Opex";
    public static readonly string[] All = [Capex, Opex];
}

public static class PmRiskCategories
{
    public const string Schedule = "Schedule";
    public const string Cost = "Cost";
    public const string Technology = "Technology";
    public const string Resources = "Resources";
    public const string Quality = "Quality";
    public const string External = "External";
    public const string Compliance = "Compliance";
    public static readonly string[] All = [Schedule, Cost, Technology, Resources, Quality, External, Compliance];
}

public static class PmRiskStatuses
{
    public const string Open = "Open";
    public const string Mitigating = "Mitigating";
    public const string Occurred = "Occurred";
    public const string Closed = "Closed";
    public static readonly string[] All = [Open, Mitigating, Occurred, Closed];
    public static bool IsActive(string status) => status is Open or Mitigating or Occurred;
}

// Kritikalitaet aus Wahrscheinlichkeit x Auswirkung (5x5-Matrix): 1-4 tief, 5-9 mittel, 10-15 hoch, 16-25 kritisch.
public static class PmRiskLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";
    public static readonly string[] All = [Low, Medium, High, Critical];
    public static int Score(int probability, int impact) => Math.Clamp(probability, 1, 5) * Math.Clamp(impact, 1, 5);
    public static string Of(int score) => score switch
    {
        >= 16 => Critical,
        >= 10 => High,
        >= 5 => Medium,
        _ => Low
    };
    public static int Rank(string level) => Array.IndexOf(All, level);
    // Wahrscheinlichkeit als Prozent fuer den Erwartungswert (PMO-Skala): 1 = 10 %, 2 = 30 %, 3 = 50 %, 4 = 70 %, 5 = 90 %.
    public static int ProbabilityPercent(int probability) => Math.Clamp(probability, 1, 5) * 20 - 10;
    public static double ExpectedValue(int probability, double costImpactChf) => Math.Round(Math.Max(costImpactChf, 0) * ProbabilityPercent(probability) / 100.0, 2);
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
    // Sitzung 2026-10-29: Produktentwicklung nach Philips Phasen (M2 Design-Freeze, M3 produktiv) und IT-Projekt nach HERMES.
    public const string ProductDevelopment = "ProductDevelopment";
    public const string ItProject = "ItProject";

    public static readonly string[] All = [Kanban, Scrum, Planner, ProductDevelopment, ItProject];
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
