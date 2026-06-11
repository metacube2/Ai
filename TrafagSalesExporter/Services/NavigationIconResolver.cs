using MudBlazor;

namespace TrafagSalesExporter.Services;

public static class NavigationIconResolver
{
    public static string Resolve(string icon) => icon switch
    {
        "AccountTree" => Icons.Material.Filled.AccountTree,
        "AdminPanelSettings" => Icons.Material.Filled.AdminPanelSettings,
        "Analytics" => Icons.Material.Filled.Analytics,
        "Assignment" => Icons.Material.Filled.Assignment,
        "AssignmentReturn" => Icons.Material.Filled.AssignmentReturn,
        "Checklist" => Icons.Material.Filled.Checklist,
        "CompareArrows" => Icons.Material.Filled.CompareArrows,
        "Dashboard" => Icons.Material.Filled.Dashboard,
        "FactCheck" => Icons.Material.Filled.FactCheck,
        "Groups" => Icons.Material.Filled.Groups,
        "Hub" => Icons.Material.Filled.Hub,
        "InsertChart" => Icons.Material.Filled.InsertChart,
        "Lightbulb" => Icons.Material.Filled.Lightbulb,
        "List" => Icons.Material.Filled.List,
        "LocationOn" => Icons.Material.Filled.LocationOn,
        "Lock" => Icons.Material.Filled.Lock,
        "Payments" => Icons.Material.Filled.Payments,
        "PeopleAlt" => Icons.Material.Filled.PeopleAlt,
        "PendingActions" => Icons.Material.Filled.PendingActions,
        "PieChart" => Icons.Material.Filled.PieChart,
        "Public" => Icons.Material.Filled.Public,
        "QueryStats" => Icons.Material.Filled.QueryStats,
        "Rule" => Icons.Material.Filled.Rule,
        "School" => Icons.Material.Filled.School,
        "Settings" => Icons.Material.Filled.Settings,
        "ShoppingCart" => Icons.Material.Filled.ShoppingCart,
        "Speed" => Icons.Material.Filled.Speed,
        "Storage" => Icons.Material.Filled.Storage,
        "Transform" => Icons.Material.Filled.Transform,
        "TrendingUp" => Icons.Material.Filled.TrendingUp,
        "Tune" => Icons.Material.Filled.Tune,
        "UploadFile" => Icons.Material.Filled.UploadFile,
        "Verified" => Icons.Material.Filled.Verified,
        "ViewInAr" => Icons.Material.Filled.ViewInAr,
        "WarningAmber" => Icons.Material.Filled.WarningAmber,
        _ => Icons.Material.Filled.Circle
    };
}
