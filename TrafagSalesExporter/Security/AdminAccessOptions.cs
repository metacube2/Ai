namespace TrafagSalesExporter.Security;

public sealed class AdminAccessOptions
{
    public const string SectionName = "AdminAccess";

    public bool Enabled { get; set; } = true;
    public string Username { get; set; } = "admin";
    public string PasswordHash { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
