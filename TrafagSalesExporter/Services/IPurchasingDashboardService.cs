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
    // Volumen (CHF) je Beschaffungsregion (Lieferantenland), absteigend (PowerBI "Vol/Region").
    public List<PurchasingLiveChartPoint> RegionSpendRows { get; set; } = [];
    // Volumen je Belegwaehrung (EKKO.Waers), CHF-bewertet plus Originalsumme. Braucht kein
    // SAP-Feld und keinen Full Load - Waers/Wkurs liegen im EKKO-Cache und werden ohnehin fuer
    // die CHF-Bewertung genutzt. Siehe PurchasingCurrencySpendRow zur Abgrenzung gegen die Region.
    public List<PurchasingCurrencySpendRow> CurrencySpendRows { get; set; } = [];
    // Reiter „Spend-Aufriss" 2026-07-24: mehrstufige Kaskade Lieferant -> Warengruppe -> Artikel
    // (gedeckelt je Ebene, Rest in „uebrige"-Zeile). Nutzt vorhandene Cache-Daten (Beleg-WG/Matnr).
    public List<PurchasingSpendCascadeNode> SpendCascadeRows { get; set; } = [];
    // Waehlbare Einstiegsperspektiven (Lieferant / Beschaffungsregion / Warengruppe / Waehrung),
    // Marco-Wunsch 2026-07-30 - die am 24.07. bewusst offen gelassene Rueckfrage. Alle Perspektiven
    // werden beim Datenladen vorberechnet, damit das Umschalten in der UI ohne DB-Runde geht.
    // SpendCascadeRows bleibt die Lieferanten-Perspektive (Standardeinstieg).
    public List<PurchasingSpendPerspectiveResult> SpendPerspectiveRows { get; set; } = [];
    // Produktgruppen-Zurechnung aus ZLO03-Komponente -> Kopfmaterial-Disponent -> optionaler
    // ZC23-Map. Mehrfachverwendungen werden gleichmaessig auf unterschiedliche Gruppen verteilt,
    // damit der zugerechnete Spend die Einkaufs-Gesamtsumme nicht vervielfacht.
    public PurchasingProductGroupAllocationSummary ProductGroupAllocation { get; set; } =
        PurchasingProductGroupAllocationSummary.Empty;
    // Region-Anteil je (Top-)Warengruppe fuer die Kuchendiagramme. Fuellt sich erst mit dem
    // naechsten Einkauf-Full-Load (SupplierCountry).
    public List<PurchasingRegionPieGroup> RegionByMaterialGroupRows { get; set; } = [];
    // Volumen (CHF) je ABC-Klasse (MARC-MAABC -> MaraAbc). Fuellt sich erst nach dem Full-Load.
    public List<PurchasingLiveChartPoint> AbcSpendRows { get; set; } = [];
    // Volumen (CHF) je XYZ-Klasse (ZCA_MAT_ABC_XYZ -> MaraXyz). Fuellt sich erst nach dem Full-Load.
    public List<PurchasingLiveChartPoint> XyzSpendRows { get; set; } = [];
    // Konkrete Handlungsableitung aus der Kombination ABC (Wertbedeutung) und XYZ
    // (Bedarfsregelmaessigkeit), statt zwei isolierter Balkendiagramme ohne Aussage.
    public List<PurchasingAbcXyzActionRow> AbcXyzActionRows { get; set; } = [];
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

/// <summary>
/// Einkaufsvolumen je Belegwaehrung (Marco-Wunsch aus der Sitzung 2026-07-30: „wieviel Umsatz
/// machen wir in welcher Waehrung", ausdruecklich auch fuer die Finanzen interessant).
/// <see cref="ChfValue"/> ist der nach CHF bewertete Betrag - damit sind die Waehrungen
/// untereinander und mit allen anderen Bloecken vergleichbar. <see cref="OriginalValue"/> ist die
/// Summe in der Belegwaehrung selbst, also das tatsaechliche Waehrungsexposure.
///
/// Wichtige Abgrenzung, die in der Sitzung ausdruecklich geklaert wurde: Diese Sicht ist NICHT die
/// Beschaffungsregion. BIPRO liegt in der Beschaffungsregion Schweiz, fakturiert aber in EUR
/// (Marco: „das hat nichts mit nach Waehrung zu tun") - Region kommt aus dem Lieferantenland
/// (LFA1.Land1), die Waehrung aus dem Bestellkopf (EKKO.Waers).
/// </summary>
public sealed record PurchasingCurrencySpendRow(string Currency, decimal ChfValue, decimal OriginalValue);

public sealed record PurchasingProductGroupAllocationSummary(
    decimal AssignedSpendChf,
    decimal UnassignedSpendChf,
    decimal MultiGroupSpendChf,
    int AssignedMaterialCount,
    int UnassignedMaterialCount,
    int MultiGroupMaterialCount,
    int MappedDispatcherCount,
    int UnmappedDispatcherCount)
{
    public static PurchasingProductGroupAllocationSummary Empty { get; } =
        new(0m, 0m, 0m, 0, 0, 0, 0, 0);
}

public sealed record PurchasingAbcXyzActionRow(
    string Classification,
    string Abc,
    string Xyz,
    decimal SpendChf,
    int MaterialCount,
    int SupplierCount,
    string ActionDe,
    string ActionEn,
    string Severity);

public sealed record PurchasingIdeaAnalysisRow(string Label, string Value, string Detail, string Severity);
