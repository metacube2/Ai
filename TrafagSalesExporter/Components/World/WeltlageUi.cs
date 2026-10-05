using System.Globalization;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Components.World;

/// <summary>Kleine Hilfen des Reiters Weltlage (2026-10-05).</summary>
public static class WeltlageUi
{
    public static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Farbe nach Signal: rot Gegenwind, gruen Rueckenwind, grau neutral.</summary>
    public static string Tone(double signal) => signal <= -0.15 ? "#EF5350" : signal >= 0.15 ? "#66BB6A" : "#B0BEC5";

    public static string Points(double score) => (score > 0 ? "+" : string.Empty) + score.ToString("0.0", CultureInfo.GetCultureInfo("de-CH"));

    /// <summary>Kreissektor (Kuchenstueck) von a1 bis a2 Grad im Uhrzeigersinn ab oben.</summary>
    public static string Sector(double cx, double cy, double r, double a1, double a2)
    {
        var (x1, y1) = Polar(cx, cy, r, a1);
        var (x2, y2) = Polar(cx, cy, r, a2);
        var large = a2 - a1 > 180 ? 1 : 0;
        return $"M{F(cx)},{F(cy)} L{F(x1)},{F(y1)} A{F(r)},{F(r)} 0 {large} 1 {F(x2)},{F(y2)} Z";
    }

    public static (double X, double Y) Polar(double cx, double cy, double r, double deg)
    {
        var rad = (deg - 90) * Math.PI / 180;
        return (cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }

    /// <summary>Linienpunkte einer Reihe in einem Rechteck w x h.</summary>
    public static string Spark(IReadOnlyList<(DateOnly Date, double Value)> points, double w, double h)
    {
        if (points.Count < 2) return string.Empty;
        var min = points.Min(p => p.Value);
        var max = points.Max(p => p.Value);
        var span = max - min == 0 ? 1 : max - min;
        return string.Join(" ", points.Select((p, i) => $"{F(w * i / (points.Count - 1))},{F(h - 2 - (h - 4) * (p.Value - min) / span)}"));
    }

    /// <summary>Anzeigename: Laender mit Namen statt ISO-Code.</summary>
    public static string Title(WorldImpact i) => i.Subject.Length == 2 ? TrafagSalesExporter.Components.Sales.WorldGeo.Name(i.Subject) : i.Title;

    public static IEnumerable<WorldImpact> Of(IEnumerable<WorldImpact> impacts, string department) => impacts.Where(i => i.Department == department);
}
