using System.ComponentModel.DataAnnotations;

namespace TrafagSalesExporter.Models;

public class FieldTransformationRule
{
    public int Id { get; set; }

    [Required]
    public string SourceSystem { get; set; } = "SAP";

    [Required]
    public string SourceField { get; set; } = nameof(SalesRecord.Material);

    [Required]
    public string TargetField { get; set; } = nameof(SalesRecord.Material);

    [Required]
    public string TransformationType { get; set; } = "Copy";

    public string Argument { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
