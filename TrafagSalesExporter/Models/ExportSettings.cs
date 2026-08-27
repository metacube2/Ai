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
    /// Verhalten der Gruppenmarge, wenn die Standardkostenwaehrung von der Verkaufswaehrung
    /// abweicht: "Convert" rechnet die Kostenbasis mit dem Tageskurs in die Verkaufswaehrung um,
    /// "Mask" zeigt Marge/% als offen ("-"). Wirkt auf Dashboard, Pruefbuch, zentrale Excel und
    /// Nachweis-Excel gleichermassen.
    ///
    /// Default seit dem Beschluss von Andreas vom 2026-08-27 ist "Convert"; bis dahin war es
    /// "Mask", weil der Fachentscheid ausstand. "Mask" bleibt als bewusst waehlbare Ausnahme
    /// bestehen. Siehe docs/FINANCE_STANDARDKOSTEN.md Abschnitt 11, B5/B6.
    /// </summary>
    public string GroupMarginCostCurrencyMode { get; set; } = GroupMarginCostCurrencyModes.Convert;

    /// <summary>
    /// Material-Fallback fuer leere Supplier-Felder: neuer CH-Werkstamm (MARC 1100)
    /// oder bisherige CH-Kostentabelle (MBEW/GroupStandardCosts 1100).
    /// </summary>
    public string SupplierFallbackMode { get; set; } = SupplierFallbackModes.ChPlantMaster;

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

public static class GroupMarginCostCurrencyModes
{
    public const string Mask = nameof(Mask);
    public const string Convert = nameof(Convert);
}

public static class SupplierFallbackModes
{
    public const string ChPlantMaster = nameof(ChPlantMaster);
    public const string GroupStandardCosts = nameof(GroupStandardCosts);

    public static string Normalize(string? mode)
        => string.Equals(mode?.Trim(), GroupStandardCosts, StringComparison.OrdinalIgnoreCase)
            ? GroupStandardCosts
            : ChPlantMaster;
}
