using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafagSalesExporter.Models;

public class SapJoinDefinition
{
    public int Id { get; set; }

    public int SiteId { get; set; }

    [ForeignKey(nameof(SiteId))]
    public Site? Site { get; set; }

    [Required]
    public string LeftAlias { get; set; } = string.Empty;

    [Required]
    public string RightAlias { get; set; } = string.Empty;

    [Required]
    public string LeftKeys { get; set; } = string.Empty;

    [Required]
    public string RightKeys { get; set; } = string.Empty;

    public string JoinType { get; set; } = "Left";

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
