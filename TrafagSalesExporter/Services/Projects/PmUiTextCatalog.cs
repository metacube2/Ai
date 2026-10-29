namespace TrafagSalesExporter.Services.Projects;

// Uebersetzungen der Trafag-Projekte-Erweiterung vom 2026-10-29 (Smartsheet-Ansicht, Risiken, Guide). Jede Ansicht
// fuehrt ihre Texte in einer eigenen Teildatei (PmUiTextCatalog.<Teil>.cs), damit parallele Arbeit nicht in dieselbe
// Datei schreibt. Ein Eintrag traegt alle Sprachen ausser de/en, Berndeutsch (gsw) eingeschlossen.
internal static partial class PmUiTextCatalog
{
    internal sealed record Entry(string De, string Es, string It, string Hi, string Sq, string Tr, string Tlh, string Gsw);

    internal static readonly string[] Languages = ["es", "it", "hi", "sq", "tr", "tlh", "gsw"];

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> Catalog = new(Build);

    // Sprache -> deutscher Text -> Uebersetzung.
    internal static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> All => Catalog.Value;

    internal static IEnumerable<Entry> Entries => SheetEntries.Concat(RiskEntries).Concat(GuideEntries);

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Build()
    {
        var result = Languages.ToDictionary(l => l, _ => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
        foreach (var e in Entries)
        {
            result["es"].TryAdd(e.De, e.Es);
            result["it"].TryAdd(e.De, e.It);
            result["hi"].TryAdd(e.De, e.Hi);
            result["sq"].TryAdd(e.De, e.Sq);
            result["tr"].TryAdd(e.De, e.Tr);
            result["tlh"].TryAdd(e.De, e.Tlh);
            result["gsw"].TryAdd(e.De, e.Gsw);
        }
        return result.ToDictionary(p => p.Key, p => (IReadOnlyDictionary<string, string>)p.Value, StringComparer.OrdinalIgnoreCase);
    }
}
