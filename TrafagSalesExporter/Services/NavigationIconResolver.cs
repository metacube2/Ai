using MudBlazor;

namespace TrafagSalesExporter.Services;

public static class NavigationIconResolver
{
    public static string Resolve(string icon) => icon switch
    {
        "AccountTree" => Icons.Material.Filled.AccountTree,
        "AdminPanelSettings" => Icons.Material.Filled.AdminPanelSettings,
        "Analytics" => Icons.Material.Filled.Analytics,
        "AssignmentReturn" => Icons.Material.Filled.AssignmentReturn,
        "CompareArrows" => Icons.Material.Filled.CompareArrows,
        "Dashboard" => Icons.Material.Filled.Dashboard,
        "FactCheck" => Icons.Material.Filled.FactCheck,
        "Groups" => Icons.Material.Filled.Groups,
        "List" => Icons.Material.Filled.List,
        "LocationOn" => Icons.Material.Filled.LocationOn,
        "Lock" => Icons.Material.Filled.Lock,
        "PeopleAlt" => Icons.Material.Filled.PeopleAlt,
        "PieChart" => Icons.Material.Filled.PieChart,
        "Public" => Icons.Material.Filled.Public,
        "QueryStats" => Icons.Material.Filled.QueryStats,
        "Rule" => Icons.Material.Filled.Rule,
        "School" => Icons.Material.Filled.School,
        "Settings" => Icons.Material.Filled.Settings,
        "ShoppingCart" => Icons.Material.Filled.ShoppingCart,
        "Speed" => Icons.Material.Filled.Speed,
        "Transform" => Icons.Material.Filled.Transform,
        "Tune" => Icons.Material.Filled.Tune,
        "UploadFile" => Icons.Material.Filled.UploadFile,
        "ViewInAr" => Icons.Material.Filled.ViewInAr,
        "WarningAmber" => Icons.Material.Filled.WarningAmber,
        _ => Icons.Material.Filled.Circle
    };
}
