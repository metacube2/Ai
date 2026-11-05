using System.Globalization;

namespace TrafagSalesExporter.Components.Sales.MonthlyReview;

/// <summary>Formatierung der Monthly Review (2026-11-05), Schreibweise wie Spezifikation 3.1 (de-CH, echtes Minuszeichen).</summary>
public static class MrvUi
{
    private static readonly CultureInfo Swiss = CultureInfo.GetCultureInfo("de-CH");

    /// <summary>Betrag kurz: 1.23 Mio, 456k (wie der Reiter Verkauf); null ergibt "–".</summary>
    public static string Amount(decimal? value) => value is { } v ? Minus(SalesUi.Short(v)) : "–";

    /// <summary>Veraenderung mit Vorzeichen und einer Nachkommastelle; null ergibt "–" (nie "0.0 %", Spezifikation 6.2 Nr. 14).</summary>
    public static string Pct(decimal? value, int decimals = 1)
        => value is { } v ? (v > 0 ? "+" : string.Empty) + Minus(v.ToString(decimals == 0 ? "0" : "0.0", Swiss)) + " %" : "–";

    /// <summary>Anteil ohne Vorzeichen.</summary>
    public static string Share(double? value) => value is { } v ? (v * 100).ToString("0", Swiss) + " %" : "–";

    public static string Arrow(decimal? value) => value switch
    {
        null => string.Empty,
        > 0.05m => "▲",
        < -0.05m => "▼",
        _ => "●"
    };

    public static string DeltaClass(decimal? value) => value switch
    {
        > 0.05m => "mrv-up",
        < -0.05m => "mrv-dn",
        _ => "mrv-neutral"
    };

    public static string Days(double? value) => value is { } v ? Math.Round(v, 0).ToString("0", Swiss) : "–";

    public static string Days(int? value) => value is { } v ? v.ToString("0", Swiss) : "–";

    public static string Number(decimal? value) => value is { } v ? v.ToString("#,##0.###", Swiss) : "–";

    public static string Date(DateOnly? value) => value is { } d ? d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : "–";

    public static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>SVG-Punktliste "x,y x,y". Hier statt in Razor: zwei Zeichenketten mit Komma liest der Uebersetzungstest als Text.</summary>
    public static string Points(IEnumerable<(double X, double Y)> points)
        => string.Join(" ", points.Select(p => F(p.X) + "," + F(p.Y)));

    /// <summary>Verlaufslinie in 120 x 26 (Spezifikation 4 sparkSvg): Punkte und Hoehe des letzten Punkts.</summary>
    public static (string Points, double LastY) Spark(IReadOnlyList<decimal> values)
    {
        var max = Math.Max(1.0, (double)values.Max());
        var min = Math.Min(0.0, (double)values.Min());
        var range = Math.Max(1.0, max - min);
        double Y(decimal v) => 23 - 20 * ((double)v - min) / range;
        var pts = values.Select((v, i) => (2 + 116.0 * i / Math.Max(1, values.Count - 1), Y(v)));
        return (Points(pts), Y(values[^1]));
    }

    /// <summary>Anzeigename der Gesellschaft "TRCH · Schweiz" (Name aus der festen Liste, sonst nur der Code).</summary>
    public static string CompanyTitle(string code, Func<string, string, string> text)
        => TrafagSalesExporter.Services.MonthlyReviewAnalytics.CompanyNames.TryGetValue(code, out var n)
            ? code + " · " + text(n.German, n.English)
            : code;

    private static string Minus(string text) => text.StartsWith('-') ? "−" + text[1..] : text;
}
