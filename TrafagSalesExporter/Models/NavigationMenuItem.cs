namespace TrafagSalesExporter.Models;

public class NavigationMenuItem
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? ParentKey { get; set; }
    public string TitleDe { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string ItemType { get; set; } = NavigationMenuItemTypes.Link;
    public string Match { get; set; } = "Prefix";
    public string RequiredPolicy { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public bool IsExpanded { get; set; }
    public bool IsSystem { get; set; } = true;
    public int SortOrder { get; set; }
}

public static class NavigationMenuItemTypes
{
    public const string Group = "Group";
    public const string Link = "Link";
    public const string Action = "Action";
}
