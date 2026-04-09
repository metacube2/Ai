using System.ComponentModel.DataAnnotations;

namespace TrafagSalesExporter.Models;

public class Site
{
    public int Id { get; set; }
    public int HanaServerId { get; set; }

    public HanaServer? HanaServer { get; set; }

    [Required]
    public string Schema { get; set; } = string.Empty;

    [Required]
    public string TSC { get; set; } = string.Empty;

    [Required]
    public string Land { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
