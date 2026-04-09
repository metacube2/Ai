using System.ComponentModel.DataAnnotations;

namespace TrafagSalesExporter.Models;

public class SharePointConfig
{
    public int Id { get; set; }

    [Required]
    public string SiteUrl { get; set; } = string.Empty;

    [Required]
    public string ExportFolder { get; set; } = "/Shared Documents/Exports/";

    [Required]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string EncryptedClientSecret { get; set; } = string.Empty;
}
