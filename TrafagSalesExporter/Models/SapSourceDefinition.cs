using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafagSalesExporter.Models;

public class SapSourceDefinition
{
    public int Id { get; set; }

    public int SiteId { get; set; }

    [ForeignKey(nameof(SiteId))]
    public Site? Site { get; set; }

    [Required]
    public string Alias { get; set; } = string.Empty;

    [Required]
    public string EntitySet { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
