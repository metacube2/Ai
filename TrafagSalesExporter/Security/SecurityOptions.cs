namespace TrafagSalesExporter.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public bool Enabled { get; set; } = true;
    public bool DevelopmentBypass { get; set; }
    public bool DevelopmentUserIsAdmin { get; set; }
    public string DevelopmentUserName { get; set; } = "DEV\\TrafagDeveloper";
    public List<string> AccessGroups { get; set; } = [];
    public List<string> AdminGroups { get; set; } = [];
}
