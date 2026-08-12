using MudBlazor;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Models;

public sealed record PurchasingSectionKpi(string LabelDe, string LabelEn, string Value, string DetailDe, string DetailEn);

public sealed record PurchasingSectionChartRow(string Label, string Value, double Percent, string Color);

/// <summary>
/// Zusaetzlicher Balkenblock unter dem Hauptdiagramm einer Section (z.B. Volumen nach
/// Warengruppe / Region / ABC / XYZ). Wird nur gerendert, wenn Rows befuellt ist. Optionaler
/// Hinweistext fuer Datenlage-Vorbehalte (z.B. MARA-MATKL-Fuellgrad).
/// </summary>
public sealed record PurchasingSectionExtraChart(
    string TitleDe,
    string TitleEn,
    string HintDe,
    string HintEn,
    IReadOnlyList<PurchasingSectionChartRow> Rows);

public sealed record PurchasingSectionStatusRow(string LabelDe, string LabelEn, string Value, string Icon, Color Color);

public sealed record PurchasingSectionDetailRow(string LabelDe, string LabelEn, string Value, string Dimension, string Source);

public sealed record PurchasingSupplierYearSpendRow(string Supplier, IReadOnlyDictionary<int, decimal> YearValues, decimal Total)
{
    /// <summary>
    /// Drilldown-Ebene je Lieferant (Marco/Armin-Review 2026-07-17): Spend des Lieferanten
    /// aufgerissen nach Warengruppe. Bevorzugt die aktuelle Materialstamm-Warengruppe
    /// (MaraMatkl), faellt auf die Beleg-Warengruppe (EKPO.Matkl) zurueck, solange SAP
    /// MARA-MATKL noch nicht liefert.
    /// </summary>
    public IReadOnlyList<PurchasingSpendGroupYearRow> MaterialGroups { get; init; } = [];
}

public sealed record PurchasingSpendGroupYearRow(string MaterialGroup, IReadOnlyDictionary<int, decimal> YearValues, decimal Total)
{
    /// <summary>
    /// Dritte Aufriss-Ebene der Spend-Matrix (Entscheid Marco, Sitzung 2026-07-30): unter der
    /// Warengruppe die einzelnen Materialnummern. Marcos Zweck ist das Aufspueren der
    /// Dummy-Zuordnungen - "wenn im Drilldown die Materialnummer drin ist, dann findest du einen".
    /// Genau deshalb ist diese Ebene unter der Warengruppe "01 - Dummy" die eigentlich
    /// interessante. Auf <see cref="PurchasingSpendArticleYearRow"/> gedeckelt, Rest in einer
    /// "uebrige (n)"-Zeile.
    /// </summary>
    public IReadOnlyList<PurchasingSpendArticleYearRow> Articles { get; init; } = [];
}

/// <summary>
/// Blattebene der Spend-Matrix: Materialnummer (Fallback Kurztext, dann "ohne Artikel") mit
/// Jahreswerten. <see cref="IsRemainder"/> markiert die Sammelzeile "uebrige (n)", damit die UI
/// sie unaufklappbar und optisch ruhiger darstellen kann und damit klar bleibt, dass die Summe
/// der Kinder weiterhin der Warengruppensumme entspricht.
/// </summary>
public sealed record PurchasingSpendArticleYearRow(
    string Article,
    IReadOnlyDictionary<int, decimal> YearValues,
    decimal Total,
    bool IsRemainder = false);

/// <summary>
/// Rekursiver Knoten fuer den mehrstufigen Spend-Aufriss (Reiter „Spend-Aufriss" 2026-07-24):
/// feste Kaskade Lieferant -> Warengruppe -> Artikel. Jede Ebene traegt Jahreswerte + Gesamt;
/// <see cref="Children"/> ist die naechste Aufriss-Stufe (leer auf der Artikel-/Blattebene).
/// Jede Ebene ist auf Top-N gekappt, der Rest steckt in einer „uebrige (n)"-Zeile, damit
/// Elternsumme = Summe der Kinder bleibt (Pivot-Eigenschaft) und der Baum bei &gt;170k
/// Positionen nicht explodiert (Blazor Server rendert den Baum serverseitig).
/// </summary>
public sealed record PurchasingSpendCascadeNode(
    string Label,
    IReadOnlyDictionary<int, decimal> YearValues,
    decimal Total,
    IReadOnlyList<PurchasingSpendCascadeNode> Children);

/// <summary>
/// Ergebnis einer Aufriss-Perspektive fuer die UI (Reiter „Spend-Aufriss", waehlbare
/// Einstiegsdimension seit 2026-07-30). <see cref="Key"/> identifiziert die Perspektive im
/// Umschalter, <see cref="LevelLabelsDe"/>/<see cref="LevelLabelsEn"/> beschreiben die Ebenenfolge
/// (z.B. „Beschaffungsregion > Lieferant > Warengruppe > Material") fuer Spaltenkopf und Hinweis.
/// Die SQL-Definition der Dimensionen bleibt absichtlich im Service - die UI braucht nur die
/// Beschriftungen.
/// </summary>
public sealed record PurchasingSpendPerspectiveResult(
    string Key,
    string LabelDe,
    string LabelEn,
    IReadOnlyList<string> LevelLabelsDe,
    IReadOnlyList<string> LevelLabelsEn,
    IReadOnlyList<PurchasingSpendCascadeNode> Rows);

/// <summary>
/// Region-Anteil je Warengruppe fuer die Kuchendiagramme im Spend-Aufriss (Marco-Wunsch:
/// „Kuchendiagramm je Warengruppe -> Anteil je Beschaffungsregion/Land"). <see cref="Slices"/>
/// sind die Regionen absteigend nach CHF-Anteil. Fuellt sich erst mit dem naechsten
/// Einkauf-Full-Load (Lieferantenland <c>SupplierCountry</c>).
/// </summary>
public sealed record PurchasingRegionPieGroup(
    string MaterialGroup,
    decimal Total,
    IReadOnlyList<PurchasingLiveChartPoint> Slices);
