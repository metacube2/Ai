using System.Globalization;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Components.Network;

/// <summary>SVG-Hilfen und Excel-Zeilen der Netzwerk-Reiter (2026-10-02).</summary>
public static class NetSvg
{
    /// <summary>Kommaliste; in Razor statt string.Join, weil der Uebersetzungstest benachbarte Zeichenketten als Textpaar liest.</summary>
    public static string Csv(IEnumerable<string> items) => string.Join(", ", items);

    /// <summary>Kurzform eines Produktnamens fuer Beschriftungen in Grafiken.</summary>
    public static string ShortProduct(string product) => product.Replace("Windows ", "Win ").Replace("Enterprise", "Ent.").Replace("Professional", "Pro").Replace("Datacenter", "DC");

    public static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Ringsegment von Anteil <paramref name="from"/> bis <paramref name="to"/> (0..1, im Uhrzeigersinn ab 12 Uhr).</summary>
    public static string Ring(double cx, double cy, double r0, double r1, double from, double to)
    {
        if (to - from >= 0.9999) to = from + 0.9999;
        (double, double) P(double f, double r) => (cx + r * Math.Sin(2 * Math.PI * f), cy - r * Math.Cos(2 * Math.PI * f));
        var (x1, y1) = P(from, r1);
        var (x2, y2) = P(to, r1);
        var (x3, y3) = P(to, r0);
        var (x4, y4) = P(from, r0);
        var large = to - from > 0.5 ? 1 : 0;
        return $"M{F(x1)},{F(y1)} A{F(r1)},{F(r1)} 0 {large} 1 {F(x2)},{F(y2)} L{F(x3)},{F(y3)} A{F(r0)},{F(r0)} 0 {large} 0 {F(x4)},{F(y4)} Z";
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> ComputerRows(IEnumerable<AdComputer> computers)
        => computers.Select(c =>
        {
            var life = AdAnalysis.Lifecycle(c.OperatingSystem, c.OsVersion);
            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["Name"] = c.Name,
                ["DNS"] = c.DnsHostName,
                ["Betriebssystem"] = life.Product + (AdAnalysis.IsEsuOnly(life, DateOnly.FromDateTime(DateTime.Today)) ? " (nur ESU)" : string.Empty),
                ["Supportende"] = life.EndOfSupport?.ToDateTime(TimeOnly.MinValue),
                ["Aktiv"] = c.Enabled ? "ja" : "nein",
                ["Letzte Anmeldung"] = c.LastLogonUtc?.ToLocalTime(),
                ["Passwort gesetzt"] = c.PwdLastSetUtc?.ToLocalTime(),
                ["Angelegt"] = c.CreatedUtc?.ToLocalTime(),
                ["OU"] = AdAnalysis.ContainerLabel(c.Container),
                ["Domaenencontroller"] = c.IsDomainController ? "ja" : "",
                ["Delegation uneingeschraenkt"] = c.UnconstrainedDelegation ? "ja" : "",
                ["Dienste"] = string.Join(", ", c.Services)
            };
        }).ToList();

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows<T>(IEnumerable<T> items, Func<T, IReadOnlyDictionary<string, object?>> map)
        => items.Select(map).ToList();
}
