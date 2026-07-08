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
    public bool AuditCsvEnabled { get; set; } = true;
    public bool UseAuditCsvAsCentralSource { get; set; }
    public string LocalAuditCsvFolder { get; set; } = string.Empty;
    public string ExchangeRateDateField { get; set; } = ExchangeRateDateFields.PostingDate;

    /// <summary>
    /// Zeitpunkt (UTC) des letzten automatischen Timer-Exports. Dient dem Nachhol-Lauf:
    /// War der Prozess zur geplanten Zeit nicht aktiv, wird beim naechsten Start erkannt,
    /// dass heute noch kein Lauf stattfand, und der Export einmalig nachgeholt.
    /// </summary>
    public DateTime? LastTimerRunUtc { get; set; }
}

public static class ExchangeRateDateFields
{
    public const string PostingDate = nameof(PostingDate);
    public const string InvoiceDate = nameof(InvoiceDate);
    public const string ExtractionDate = nameof(ExtractionDate);
}
