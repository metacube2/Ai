namespace TrafagSalesExporter.Models;

public enum SupplyChainAnalysisKind
{
    MaterialDisposition,
    PurchaseCoverage,
    MaterialDependency,
    PlanningParameterAudit,
    DeliveryPerformance
}

public sealed record SupplyChainAnalysisFilter(
    string Search = "",
    string Dispatcher = "",
    string ProductGroup = "",
    bool OnlyActionable = true);

public sealed record SupplyChainKpi(string LabelDe, string LabelEn, string Value, string DetailDe, string DetailEn);

public sealed record SupplyChainRiskBucket(string LabelDe, string LabelEn, int Count, string Color);

public sealed record SupplyChainAnalysisRow(
    string Material,
    string Description,
    string Dispatcher,
    string ProductGroup,
    string RiskCode,
    string RiskDe,
    string RiskEn,
    decimal Stock,
    decimal Consumption,
    decimal FixedReceipts,
    decimal PlannedReceipts,
    decimal FixedIssues,
    decimal PlannedIssues,
    decimal FinalStock,
    decimal SafetyStock,
    decimal ReorderPoint,
    decimal ShortageQuantity,
    decimal ShortageValueChf,
    // false = fuer dieses Material sind in ZLO03/ZMD04 keine Stueckkosten gepflegt. Dann ist
    // ShortageValueChf keine bewertete 0, sondern unbekannt, und wird in der GUI als "-" gezeigt.
    bool HasUnitCost,
    decimal OpenOrderQuantity,
    decimal OpenOrderValueChf,
    decimal OverdueQuantity,
    DateTime? NextDeliveryDate,
    string Supplier,
    int SupplierCount,
    decimal TopSupplierSharePercent,
    int ParentMaterialCount,
    bool Exclusive,
    string MrpType,
    string LotSize,
    decimal FixedLotSize,
    string ProcurementType,
    string MaterialStatus,
    string LzCode,
    string IssueDe,
    string IssueEn,
    string Source);

public sealed class SupplyChainAnalysisResult
{
    public SupplyChainAnalysisKind Kind { get; init; }
    public DateTime? MaterialUsageLoadedAtUtc { get; init; }
    public DateTime? PurchasingLoadedAtUtc { get; init; }
    public int SourceMaterialCount { get; init; }
    public int FilteredMaterialCount { get; init; }
    public bool ActualReceiptDateAvailable { get; init; }
    public string NoticeDe { get; init; } = string.Empty;
    public string NoticeEn { get; init; } = string.Empty;
    public IReadOnlyList<SupplyChainKpi> Kpis { get; init; } = [];
    public IReadOnlyList<SupplyChainRiskBucket> RiskBuckets { get; init; } = [];
    public IReadOnlyList<SupplyChainAnalysisRow> Rows { get; init; } = [];
}
