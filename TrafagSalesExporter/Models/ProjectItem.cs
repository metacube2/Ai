namespace TrafagSalesExporter.Models;

public sealed class ProjectItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = ProjectStatuses.Idea;
    public string Priority { get; set; } = ProjectPriorities.Normal;
    public string Owner { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int ProgressPercent { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public static class ProjectStatuses
{
    public const string Idea = "Idea";
    public const string Planned = "Planned";
    public const string InProgress = "InProgress";
    public const string Blocked = "Blocked";
    public const string Completed = "Completed";

    public static readonly string[] All = [Idea, Planned, InProgress, Blocked, Completed];
}

public static class ProjectPriorities
{
    public const string Low = "Low";
    public const string Normal = "Normal";
    public const string High = "High";
    public const string Critical = "Critical";

    public static readonly string[] All = [Low, Normal, High, Critical];
}
