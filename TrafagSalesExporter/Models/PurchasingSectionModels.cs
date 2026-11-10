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

public enum PurchasingSectionDataState
{
    Waiting,
    Simulation,
    Loaded
}

/// <summary>
/// Deutet die Spalte „Quelle“ einer Section (F8 Pruefung 2026-11-05). Bis dahin verglich <c>PurchasingSection</c> mit dem
/// Literal „SAP live“; seit <c>d5bc321</c> (2026-09-10) steht dort aber <c>DataSourceLabel</c> („SAP-Datenbestand“ bzw.
/// „SAP-Stichprobe“, uebersetzt), und jede Section mit geladenen Daten zeigte „Wartet auf SAP“. Jetzt gilt umgekehrt:
/// geladen ist jede Quelle, die weder der Wartehinweis noch „Simulation“ ist. Die zwei Marker sind feste deutsche
/// Literale aus <c>PurchasingDashboard.razor</c> und werden nicht uebersetzt uebergeben.
/// </summary>
public static class PurchasingSectionSource
{
    public const string WaitingMarker = "Wartet auf SAP";
    public const string SimulationMarker = "Simulation";

    public static PurchasingSectionDataState Classify(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || source.Trim().StartsWith("wartet", StringComparison.OrdinalIgnoreCase))
            return PurchasingSectionDataState.Waiting;
        return source.Trim().Equals(SimulationMarker, StringComparison.OrdinalIgnoreCase)
            ? PurchasingSectionDataState.Simulation
            : PurchasingSectionDataState.Loaded;
    }

    /// <summary>Gesamtstatus einer Section: geladen, sobald eine Zeile geladene SAP-Daten traegt.</summary>
    public static PurchasingSectionDataState Resolve(IEnumerable<PurchasingSectionDetailRow> rows)
    {
        var states = rows.Select(row => Classify(row.Source)).ToList();
        if (states.Contains(PurchasingSectionDataState.Loaded))
            return PurchasingSectionDataState.Loaded;
        return states.Contains(PurchasingSectionDataState.Simulation)
            ? PurchasingSectionDataState.Simulation
            : PurchasingSectionDataState.Waiting;
    }
}

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
