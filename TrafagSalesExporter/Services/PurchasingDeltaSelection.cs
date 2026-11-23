using System.Globalization;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Welche Bestellungen ein Einkauf-Delta neu laden muss, ausgewaehlt in der App statt in SAP.
///
/// BEFUND 2026-11-20: <c>EKKOSet</c> in <c>ZPOWERBI_EINKAUF_SRV</c> reicht <c>$filter</c> nicht an
/// den Data Provider durch (<c>saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md</c>, „set_filterable ...
/// sind reine Metadaten“). Der Filter <c>Aedat ge '...'</c> lieferte deshalb alle Belege: am
/// 19.11. 178'479, am 20.11. 178'505. Der Delta lud danach EKPO und EKET fuer JEDEN Beleg in
/// Paketen zu 20 nach, rund 18'000 Einzelanfragen, und lief 90 Minuten.
///
/// Neu geladen werden jetzt nur Belege, die im Cache fehlen oder deren <c>Aedat</c> ab dem Stichtag
/// liegt; dazu wie bisher alle Belege mit offener Menge (Wareneingaenge aendern <c>Aedat</c> nicht).
/// Eine <c>Aedat</c>, die sich nicht lesen laesst, zaehlt als geaendert, damit kein Beleg verloren geht.
/// </summary>
public static class PurchasingDeltaSelection
{
    private static readonly Regex JsonDate = new(@"^/Date\((?<ms>-?\d+)([+-]\d{4})?\)/$", RegexOptions.CultureInvariant);

    public static List<string> SelectChanged(
        IEnumerable<(string Ebeln, string Aedat)> sapHeaders,
        IReadOnlySet<string> cachedEbelns,
        DateTime changedSince)
    {
        var since = changedSince.Date;
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (ebeln, aedat) in sapHeaders)
        {
            if (string.IsNullOrWhiteSpace(ebeln) || !seen.Add(ebeln))
                continue;

            if (!cachedEbelns.Contains(ebeln))
            {
                result.Add(ebeln);
                continue;
            }

            var date = ParseSapDate(aedat);
            if (date is null || date.Value >= since)
                result.Add(ebeln);
        }

        return result;
    }

    /// <summary>
    /// Ob ein Full Load den Bestand ersetzen darf: nur, wenn SAP mindestens die Haelfte der bisherigen
    /// Bestellkoepfe liefert. Bestellungen verschwinden in SAP praktisch nie in grosser Zahl; eine leere
    /// oder halbe Antwort deutet auf Berechtigung, Transport oder Ausfall (Review 20.11. E9).
    /// </summary>
    public static bool IsPlausibleReplacement(int newCount, int cachedCount)
        => cachedCount == 0 || (newCount > 0 && newCount * 2L >= cachedCount);

    /// <summary>Liest <c>yyyy-MM-dd</c>, ISO mit Zeit, <c>yyyyMMdd</c> und OData-V2 <c>/Date(ms)/</c>.</summary>
    public static DateTime? ParseSapDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var text = value.Trim();

        var json = JsonDate.Match(text);
        if (json.Success && long.TryParse(json.Groups["ms"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms))
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.Date;

        if (DateTime.TryParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var compact))
            return compact.Date;
        if (text == "00000000")
            return null;

        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.Date
            : null;
    }
}
