namespace TrafagSalesExporter.Security;

public sealed class HrKpiAccessOptions
{
    public const string SectionName = "HrKpiAccess";

    public bool Enabled { get; set; } = true;
    public string Username { get; set; } = "hr";
    public string PasswordHash { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<HrKpiAccessUserOptions> AdminUsers { get; set; } = [];
}

public sealed class HrKpiAccessUserOptions
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
