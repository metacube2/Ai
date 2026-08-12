namespace TrafagSalesExporter.Models;

public class SharePointConfig
{
    public int Id { get; set; }
    public string SiteUrl { get; set; } = string.Empty;
    public string ExportFolder { get; set; } = string.Empty;
    public string CentralExportFolder { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
