using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.Projects;

public sealed record PmUser(string Login, string DisplayName, bool IsAdmin)
{
    public static readonly PmUser Anonymous = new(string.Empty, string.Empty, false);
    public bool IsKnown => Login.Length > 0;
}

public sealed record PmPerson(string Login, string Name);

public sealed record PmProjectSummary(
    int Id, string Key, string Name, string Description, string Color, string Icon, string Template,
    string LeadLogin, string LeadName, IReadOnlyList<PmPerson> Members,
    DateTime? StartDate, DateTime? DueDate, bool IsArchived,
    int Total, int Open, int InProgress, int Done, int Overdue, int ProgressPercent,
    DateTime UpdatedAtUtc, bool IsMember);

public sealed record PmColumnView(int Id, string Name, string Category, int SortOrder, int WipLimit);

public sealed record PmSprintView(
    int Id, string Name, string Goal, DateTime? StartDate, DateTime? EndDate, string State, DateTime? CompletedAtUtc);

public sealed record PmCard(
    int Id, int ProjectId, string Key, int Number, string Type, string Title,
    int ColumnId, string ColumnName, string Category, string Priority,
    string AssigneeLogin, string AssigneeName, IReadOnlyList<string> Labels,
    DateTime? StartDate, DateTime? DueDate, int StoryPoints,
    int? ParentId, string ParentKey, int? EpicId, string EpicTitle, int? SprintId, double Rank, string Cover,
    int ChecklistDone, int ChecklistTotal, int CommentCount, int SubtasksDone, int SubtasksTotal,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? CompletedAtUtc, bool IsOverdue);

public sealed record PmBoard(
    PmProjectSummary Project, IReadOnlyList<PmColumnView> Columns, IReadOnlyList<PmSprintView> Sprints,
    IReadOnlyList<PmCard> Tasks, IReadOnlyList<PmCard> Epics, IReadOnlyList<string> Labels,
    bool CanEdit, bool CanManage);

public sealed record PmChecklistView(int Id, string Text, bool IsDone);

public sealed record PmCommentView(int Id, string Body, string AuthorLogin, string AuthorName, DateTime CreatedAtUtc);

public sealed record PmActivityView(int Id, int? TaskId, string TaskKey, string Login, string Name, string Text, DateTime AtUtc);

public sealed record PmTaskDetail(
    PmCard Card, string Description, string ReporterLogin, string ReporterName,
    IReadOnlyList<PmChecklistView> Checklist, IReadOnlyList<PmCommentView> Comments,
    IReadOnlyList<PmActivityView> Activity, IReadOnlyList<PmCard> Subtasks, bool CanEdit);

public sealed record PmMyTask(PmCard Card, string ProjectName, string ProjectColor);

public sealed record PmBurndownPoint(DateTime Day, double Ideal, double? Remaining);

public sealed class PmProjectInput
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#C8501E";
    public string Icon { get; set; } = "ViewKanban";
    public string Template { get; set; } = PmTemplates.Kanban;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public sealed class PmTaskInput
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = PmTaskTypes.Task;
    public string Description { get; set; } = string.Empty;
    public int? ColumnId { get; set; }
    public string Priority { get; set; } = PmPriorities.Medium;
    public string AssigneeLogin { get; set; } = string.Empty;
    public string Labels { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int StoryPoints { get; set; }
    public int? ParentId { get; set; }
    public int? EpicId { get; set; }
    public int? SprintId { get; set; }
    public string Cover { get; set; } = string.Empty;
}

public sealed class PmException(string german, string english) : Exception(english)
{
    public string German { get; } = german;
    public string English { get; } = english;
}

/// <summary>Meldet offenen Boards, dass sich in einem Projekt etwas geaendert hat (0 = Projektliste).</summary>
public sealed class PmNotifier
{
    public event Action<int>? Changed;

    public void Publish(int projectId)
    {
        if (Changed is not { } handlers)
            return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<int>>())
        {
            try { handler(projectId); }
            catch { /* eine abgestuerzte Seite darf die anderen nicht stoeren */ }
        }
    }
}

public static class PmStyle
{
    public static readonly string[] Colors =
        ["#C8501E", "#1E88E5", "#2E7D32", "#8E24AA", "#F9A825", "#00897B", "#D81B60", "#546E7A", "#6D4C41", "#3949AB"];

    public static readonly string[] Covers = ["", "#F06A6A", "#F5A623", "#F8E16C", "#7ED321", "#4AC1D9", "#5A8DEE", "#B57BEE", "#8E99A4"];

    public static readonly string[] Icons =
        ["ViewKanban", "Rocket", "Build", "Factory", "Inventory", "ShoppingCart", "Science", "School", "Groups", "Campaign", "Computer", "Public"];

    public static string LabelColor(string label)
    {
        var hash = 17;
        foreach (var ch in (label ?? string.Empty).ToLowerInvariant())
            hash = unchecked(hash * 31 + ch);
        return Covers[1 + Math.Abs(hash % (Covers.Length - 1))];
    }

    public static string ResolveIcon(string? icon) => icon switch
    {
        "Rocket" => MudBlazor.Icons.Material.Filled.RocketLaunch,
        "Build" => MudBlazor.Icons.Material.Filled.Build,
        "Factory" => MudBlazor.Icons.Material.Filled.Factory,
        "Inventory" => MudBlazor.Icons.Material.Filled.Inventory,
        "ShoppingCart" => MudBlazor.Icons.Material.Filled.ShoppingCart,
        "Science" => MudBlazor.Icons.Material.Filled.Science,
        "School" => MudBlazor.Icons.Material.Filled.School,
        "Groups" => MudBlazor.Icons.Material.Filled.Groups,
        "Campaign" => MudBlazor.Icons.Material.Filled.Campaign,
        "Computer" => MudBlazor.Icons.Material.Filled.Computer,
        "Public" => MudBlazor.Icons.Material.Filled.Public,
        _ => MudBlazor.Icons.Material.Filled.ViewKanban
    };

    public static string TypeIcon(string type) => type switch
    {
        PmTaskTypes.Bug => MudBlazor.Icons.Material.Filled.BugReport,
        PmTaskTypes.Story => MudBlazor.Icons.Material.Filled.Bookmark,
        PmTaskTypes.Epic => MudBlazor.Icons.Material.Filled.Bolt,
        PmTaskTypes.Subtask => MudBlazor.Icons.Material.Filled.SubdirectoryArrowRight,
        _ => MudBlazor.Icons.Material.Filled.CheckBox
    };

    public static string PriorityIcon(string priority) => priority switch
    {
        PmPriorities.Highest => MudBlazor.Icons.Material.Filled.KeyboardDoubleArrowUp,
        PmPriorities.High => MudBlazor.Icons.Material.Filled.KeyboardArrowUp,
        PmPriorities.Low => MudBlazor.Icons.Material.Filled.KeyboardArrowDown,
        PmPriorities.Lowest => MudBlazor.Icons.Material.Filled.KeyboardDoubleArrowDown,
        _ => MudBlazor.Icons.Material.Filled.DragHandle
    };
}
