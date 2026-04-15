namespace TrafagSalesExporter.Models;

public class ExportSettings
{
    public int Id { get; set; }
    public string DateFilter { get; set; } = "2025-01-01";
    public int TimerHour { get; set; } = 3;
    public int TimerMinute { get; set; }
    public bool TimerEnabled { get; set; } = true;
    public bool DebugLoggingEnabled { get; set; }
    public string LocalSiteExportFolder { get; set; } = string.Empty;
    public string LocalConsolidatedExportFolder { get; set; } = string.Empty;
    public string SapUsername { get; set; } = string.Empty;
    public string SapPassword { get; set; } = string.Empty;
    public string Bi1Username { get; set; } = string.Empty;
    public string Bi1Password { get; set; } = string.Empty;
    public string SageUsername { get; set; } = string.Empty;
    public string SagePassword { get; set; } = string.Empty;
}
