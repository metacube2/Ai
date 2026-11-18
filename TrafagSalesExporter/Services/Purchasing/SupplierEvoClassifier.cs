namespace TrafagSalesExporter.Services;

/// <summary>Eine Zeile der Lieferanten-Klasse A/B/C (Einkaufsvolumen EVO je Lieferantennummer).</summary>
public sealed record SupplierEvoClassRow(
    string SupplierKey,
    string Name,
    decimal Evo,
    int Positions,
    string TopPlanner,
    decimal Share,
    decimal CumulativeShare,
    string Class);

/// <summary>
/// Lieferanten-Klasse A/B/C nach Einkaufsvolumen (Lieferantenbewertung Paket 1, Plan
/// <c>saptasks/einkauf_lieferantenbewertung/PLAN_PAKET1_2026-11-18.md</c> Abschnitt 3).
/// Vorgaben Armin (Mail 12.11. und 17./18.11.2026): A ca. 70 %, B weitere 20 %, C Rest, ueber den im Dashboard gewaehlten
/// Zeitraum, Basis Bestellungen der Artikel mit Disponent 001 bis 006. Grenzfallregel wie Demo V6 und SAP-Programm
/// <c>/ITS/CA_MAT_ABC_XYZ</c>: massgebend ist der kumulierte Anteil VOR dem Lieferanten (unter 70 % A, unter 90 % B, sonst C);
/// wer eine Grenze ueberschreitet, bleibt in der Klasse. Lieferanten mit Volumen 0 oder negativ bekommen keine Klasse und
/// zaehlen nicht in der Basis (Entscheid D4 im Plan, Vorschlag), stehen aber in der Liste, damit die Summe gleich bleibt.
/// </summary>
public static class SupplierEvoClassifier
{
    public const decimal ALimit = 0.70m;
    public const decimal BLimit = 0.90m;
    public const string NoClass = "-";

    public static IReadOnlyList<SupplierEvoClassRow> Classify(
        IEnumerable<(string SupplierKey, string Name, decimal Evo, int Positions, string TopPlanner)> rows,
        decimal aLimit = ALimit,
        decimal bLimit = BLimit)
    {
        var list = rows.ToList();
        var positive = list.Where(r => r.Evo > 0)
            .OrderByDescending(r => r.Evo)
            .ThenBy(r => r.SupplierKey, StringComparer.Ordinal)
            .ToList();
        var basis = positive.Sum(r => r.Evo);
        var result = new List<SupplierEvoClassRow>(list.Count);
        var before = 0m;
        foreach (var r in positive)
        {
            var shareBefore = basis == 0 ? 0 : before / basis;
            var cls = shareBefore < aLimit ? "A" : shareBefore < bLimit ? "B" : "C";
            before += r.Evo;
            result.Add(new SupplierEvoClassRow(r.SupplierKey, r.Name, r.Evo, r.Positions, r.TopPlanner,
                basis == 0 ? 0 : r.Evo / basis, basis == 0 ? 0 : before / basis, cls));
        }
        foreach (var r in list.Where(r => r.Evo <= 0).OrderBy(r => r.Evo).ThenBy(r => r.SupplierKey, StringComparer.Ordinal))
            result.Add(new SupplierEvoClassRow(r.SupplierKey, r.Name, r.Evo, r.Positions, r.TopPlanner, 0, 0, NoClass));
        return result;
    }

    /// <summary>Lieferantennummer wie im Einkauf-Lader: ohne Leerzeichen, Grossbuchstaben, ohne fuehrende Nullen.</summary>
    public static string NormalizeSupplier(string? lifnr)
    {
        var s = new string((lifnr ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant().TrimStart('0');
        return s;
    }
}
