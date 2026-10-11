using System.ComponentModel.DataAnnotations;

namespace TrafagSalesExporter.Models;

public class SourceSystemDefinition
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string ConnectionKind { get; set; } = SourceSystemConnectionKinds.Hana;

    public bool IsActive { get; set; } = true;

    public string CentralServiceUrl { get; set; } = string.Empty;

    public string CentralUsername { get; set; } = string.Empty;

    public string CentralPassword { get; set; } = string.Empty;
}

public static class SourceSystemConnectionKinds
{
    public const string Hana = "HANA";
    public const string SapGateway = "SAP_GATEWAY";
    public const string ManualExcel = "MANUAL_EXCEL";

    public static readonly string[] All = [Hana, SapGateway, ManualExcel];
}
