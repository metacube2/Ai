using System.Globalization;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Components.Sales;

/// <summary>Formatierung und kleine SVG-Hilfen des Reiters Verkauf (2026-10-02).</summary>
public static class SalesUi
{
    private static readonly CultureInfo Swiss = CultureInfo.GetCultureInfo("de-CH");

    public static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Chf(decimal value) => value.ToString("N0", Swiss);

    /// <summary>Kurzform: 1.23 Mio, 456k, 789.</summary>
    public static string Short(decimal value)
    {
        var a = Math.Abs(value);
        return a >= 1_000_000m ? (value / 1_000_000m).ToString("0.00", Swiss) + " Mio"
            : a >= 10_000m ? (value / 1_000m).ToString("0", Swiss) + "k"
            : a >= 1_000m ? (value / 1_000m).ToString("0.0", Swiss) + "k"
            : value.ToString("0", Swiss);
    }

    public static string Percent(decimal? value) => value is null ? "–" : (value > 0 ? "+" : string.Empty) + value.Value.ToString("0", Swiss) + " %";

    public static string Percent(double value) => (value * 100).ToString("0.0", Swiss) + " %";

    /// <summary>Polyline-Punkte einer Verlaufslinie in einem Rechteck der Breite w und Hoehe h.</summary>
    public static string Spark(IReadOnlyList<decimal> values, double w, double h)
    {
        if (values.Count == 0) return string.Empty;
        var max = (double)Math.Max(1, values.Max());
        return string.Join(" ", values.Select((v, i) =>
            $"{F(values.Count == 1 ? w / 2 : w * i / (values.Count - 1))},{F(h - 2 - (h - 4) * Math.Max(0, (double)v) / max)}"));
    }

    public static IReadOnlyList<decimal> MonthlyValues(IEnumerable<SalesFact> facts, DateOnly refEnd, int months)
        => SalesAnalytics.Monthly(facts, refEnd.AddMonths(-months), refEnd).Select(m => m.Value).ToList();

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows<T>(IEnumerable<T> items, Func<T, IReadOnlyDictionary<string, object?>> map)
        => items.Select(map).ToList();

    public static string Csv(IEnumerable<string> items) => string.Join(", ", items);

    /// <summary>Vergleichszeitraum, z. B. "01.2026–09.2026".</summary>
    public static string CurrentPeriod(SalesDataset d) => $"{d.ReferenceEnd.AddMonths(-d.CompareMonths):MM.yyyy}–{d.ReferenceEnd.AddDays(-1):MM.yyyy}";

    /// <summary>Derselbe Zeitraum ein Jahr frueher.</summary>
    public static string PreviousPeriod(SalesDataset d) => $"{d.ReferenceEnd.AddMonths(-12 - d.CompareMonths):MM.yyyy}–{d.ReferenceEnd.AddMonths(-12).AddDays(-1):MM.yyyy}";
}
