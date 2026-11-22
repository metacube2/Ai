using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafagSalesExporter.Models;

public class SapFieldMapping
{
    public int Id { get; set; }

    public int SiteId { get; set; }

    [ForeignKey(nameof(SiteId))]
    public Site? Site { get; set; }

    [Required]
    public string TargetField { get; set; } = nameof(SalesRecord.Material);

    [Required]
    public string SourceExpression { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
