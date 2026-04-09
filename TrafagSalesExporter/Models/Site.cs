using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafagSalesExporter.Models;

public class Site
{
    public int Id { get; set; }

    public int HanaServerId { get; set; }

    [ForeignKey(nameof(HanaServerId))]
    public HanaServer? HanaServer { get; set; }

    [Required]
    public string Schema { get; set; } = string.Empty;

    [Required]
    public string TSC { get; set; } = string.Empty;

    [Required]
    public string Land { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
