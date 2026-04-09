namespace TrafagSalesExporter.Models;

public class ExportSettings
{
    public int Id { get; set; }
    public string DateFilter { get; set; } = "2025-01-01";
    public int TimerHour { get; set; } = 3;
    public int TimerMinute { get; set; }
    public bool TimerEnabled { get; set; } = true;
}
