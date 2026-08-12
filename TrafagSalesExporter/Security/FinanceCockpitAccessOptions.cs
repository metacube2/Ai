namespace TrafagSalesExporter.Security;

public sealed class FinanceCockpitAccessOptions
{
    public const string SectionName = "FinanceCockpitAccess";

    public bool Enabled { get; set; } = true;
    public string Username { get; set; } = "finance";
    public string PasswordHash { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
