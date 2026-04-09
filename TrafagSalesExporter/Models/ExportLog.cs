using System.ComponentModel.DataAnnotations.Schema;

namespace TrafagSalesExporter.Models;

public class ExportLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int SiteId { get; set; }

    [ForeignKey(nameof(SiteId))]
    public Site? Site { get; set; }

    public string Land { get; set; } = string.Empty;
    public string TSC { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string FileName { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
}
