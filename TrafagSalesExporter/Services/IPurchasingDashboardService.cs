using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IPurchasingDashboardService
{
    Task<PurchasingDashboardLiveState> LoadAsync(PurchasingDashboardFilter? filter = null, CancellationToken cancellationToken = default);
}

public sealed record PurchasingDashboardFilter(
    DateTime FromDate,
    DateTime ToDate,
    bool ExcludeDeletedItems = true,
    // Nur echte Bestellungen (EKKO.Bstyp='F' ohne Umlagerung UB) in Spend/Offen-KPIs; schliesst
    // Anfragen (A/AN), Kontrakte (K/MK) und Umlagerungen (UB) aus (Marcos Forderung nach Trennung).
    bool OrdersOnly = true,
    // Endgelieferte Positionen (EKPO.Elikz='X') nicht mehr als offen zaehlen (M7).
    bool ExcludeEndDelivered = true)
{
    public string Label => $"{FromDate:yyyy-MM-dd} bis {ToDate:yyyy-MM-dd}";
}

public sealed class PurchasingDashboardLiveState
{
    public bool SapReachable { get; set; }
    public bool EkkoLoaded { get; set; }
    public bool EkpoLoaded { get; set; }
    public bool EketLoaded { get; set; }
    public int PurchaseOrderCount { get; set; }
    public int SupplierCount { get; set; }
    public DateTime? LatestOrderDate { get; set; }
    public int PositionSampleCount { get; set; }
    public int ScheduleSampleCount { get; set; }
    public bool UsesCache { get; set; }
    public string CacheStatus { get; set; } = string.Empty;
    public DateTime? CacheCompletedAtUtc { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public decimal SpendChfSample { get; set; }
    public decimal OpenQuantitySample { get; set; }
    public decimal OpenValueSample { get; set; }
    public decimal ContractValueSample { get; set; }
    // Ueberfaellige offene Positionen (EKET.Eindt < heute, offene Menge > 0). Eigene KPI fuer die
    // Offene-Bestellungen-Sicht, damit Rueckstand getrennt vom disponierten Zulauf sichtbar ist.
    public decimal OverdueValueSample { get; set; }
    public decimal OverdueQuantitySample { get; set; }
    public int OverduePositionCount { get; set; }
    public string TopSupplierLabel { get; set; } = string.Empty;
    public string TopMaterialGroupLabel { get; set; } = string.Empty;
    public string TopArticleLabel { get; set; } = string.Empty;
    public string TopCommitmentLabel { get; set; } = string.Empty;
    public List<int> SpendYears { get; set; } = [];
    public List<PurchasingSupplierYearSpendRow> SupplierYearSpendRows { get; set; } = [];
    // Volumen (CHF) je Warengruppe im Zeitraum, absteigend (PowerBI "Diagramm Vol./WG").
    // Warengruppe = COALESCE(MaraMatkl, Matkl, 'ohne Warengruppe') wie in der Spend-Matrix.
    public List<PurchasingLiveChartPoint> MaterialGroupSpendRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> CurrentYearSupplierSpendRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> SpendChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> OpenValueChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> ContractChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> CommitmentDetailChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> DeliveryRiskChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> PriceVarianceChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> SpendConcentrationChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> DataQualityChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> PriceTrendChartRows { get; set; } = [];
    public List<PurchasingIdeaAnalysisRow> DeliveryRiskRows { get; set; } = [];
    public List<PurchasingIdeaAnalysisRow> OverduePositionRows { get; set; } = [];
    public List<PurchasingIdeaAnalysisRow> PriceVarianceRows { get; set; } = [];
    public List<PurchasingIdeaAnalysisRow> ArticlePriceTrendRows { get; set; } = [];
    public List<PurchasingIdeaAnalysisRow> SpendConcentrationRows { get; set; } = [];
    public List<PurchasingIdeaAnalysisRow> DataQualityRows { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}

public sealed record PurchasingLiveChartPoint(string Label, decimal Value);
public sealed record PurchasingIdeaAnalysisRow(string Label, string Value, string Detail, string Severity);
