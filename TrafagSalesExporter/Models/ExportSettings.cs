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
    /// Material-Fallback fuer leere Supplier-Felder: CH-Werkstamm (MARC 1100), bisherige
    /// CH-Kostentabelle (MBEW/GroupStandardCosts 1100) oder bewusst lokale Standardkosten.
    /// </summary>
    public string SupplierFallbackMode { get; set; } = SupplierFallbackModes.ChPlantMaster;

    /// <summary>
    /// Kostenquelle bei internem Lieferanten und Schweizer Werkstammtreffer (MARC 1100):
    /// bisherige Kosten der liefernden Gesellschaft oder bewusst Schweizer MBEW-STPRS.
    /// </summary>
    public string InternalSupplierCostSourceMode { get; set; } = InternalSupplierCostSourceModes.DeliveringEntityCosts;

    /// <summary>
    /// Verdichtung der eigenen B1-Belegkosten fuer Trafag Italien und Indien. Wirkt erst beim
    /// naechsten Import dieser Gesellschaft, weil dann <c>GroupStandardCosts</c> neu aufgebaut wird.
    /// </summary>
    public string B1GroupStandardCostMode { get; set; } = B1GroupStandardCostModes.LatestPositive;

    /// <summary>
    /// Kursprofil fuer alle CHF-Finance-Umrechnungen: Gruppenmarge im Cockpit,
    /// Finance-Pruefbuch sowie Nachweis- und Sales_All-Excel.
    /// </summary>
    public string GroupMarginChfRateMode { get; set; } = GroupMarginChfRateModes.CurrentDailyRate;

    /// <summary>
    /// ISS-003.4: Verhalten der CH/AT-Herstellerregel (jede TRCH/TRAT-Verkaufszeile ist per
    /// TSC-Definition "Intern / TR_AG", siehe <see cref="GroupMarginSupplierClassifier"/>), wenn
    /// zum selben Material ein aktiver, nicht geloeschter externer Einkaufsbeleg existiert.
    /// Default belaesst die Regel unveraendert; die Alternative klassifiziert eine solche Zeile
    /// als "Extern". Grundlage ist der bereits geladene Einkauf-Cache (EKKO/EKPO), kein
    /// zusaetzlicher SAP-Zugriff. Siehe docs/FINANCE_SUPPLIER.md Abschnitt 6.
    /// </summary>
    public string MarcForeignProcurementMode { get; set; } = MarcForeignProcurementModes.Ignore;

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
    public const string LocalStandardCosts = nameof(LocalStandardCosts);

    public static string Normalize(string? mode)
        => string.Equals(mode?.Trim(), GroupStandardCosts, StringComparison.OrdinalIgnoreCase)
            ? GroupStandardCosts
            : string.Equals(mode?.Trim(), LocalStandardCosts, StringComparison.OrdinalIgnoreCase)
                ? LocalStandardCosts
                : ChPlantMaster;
}

public static class InternalSupplierCostSourceModes
{
    public const string DeliveringEntityCosts = nameof(DeliveringEntityCosts);
    public const string SwissStprsForChPlantMaterial = nameof(SwissStprsForChPlantMaterial);

    public static string Normalize(string? mode)
        => string.Equals(mode?.Trim(), SwissStprsForChPlantMaterial, StringComparison.OrdinalIgnoreCase)
            ? SwissStprsForChPlantMaterial
            : DeliveringEntityCosts;
}

public static class B1GroupStandardCostModes
{
    public const string LatestPositive = nameof(LatestPositive);
    public const string AveragePositive = nameof(AveragePositive);

    public static string Normalize(string? mode)
        => string.Equals(mode?.Trim(), AveragePositive, StringComparison.OrdinalIgnoreCase)
            ? AveragePositive
            : LatestPositive;

    public static string Describe(string? mode)
        => Normalize(mode) == AveragePositive
            ? "Durchschnitt positiver B1 StockPrice-Werte des Imports"
            : "Juengster positiver B1 StockPrice";
}

public static class GroupMarginChfRateModes
{
    public const string CurrentDailyRate = nameof(CurrentDailyRate);
    public const string FinanceYearEndRate = nameof(FinanceYearEndRate);

    public static string Normalize(string? mode)
        => string.Equals(mode?.Trim(), FinanceYearEndRate, StringComparison.OrdinalIgnoreCase)
            ? FinanceYearEndRate
            : CurrentDailyRate;

    public static DateTime ResolveRateDate(string? mode, int financeYear, DateTime? today = null)
        => Normalize(mode) == FinanceYearEndRate
            ? new DateTime(financeYear, 12, 31)
            : (today ?? DateTime.Today).Date;
}

public static class MarcForeignProcurementModes
{
    public const string Ignore = nameof(Ignore);
    public const string OverridesToExternal = nameof(OverridesToExternal);

    public static string Normalize(string? mode)
        => string.Equals(mode?.Trim(), OverridesToExternal, StringComparison.OrdinalIgnoreCase)
            ? OverridesToExternal
            : Ignore;
}
