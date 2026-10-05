using System.Globalization;
using System.Text.Json;

namespace TrafagSalesExporter.Services;

/// <summary>Reine Parser fuer die Weltlage-Quellen (2026-10-05), testbar ohne Netz.</summary>
public static class WorldParsers
{
    /// <summary>GDELT nutzt FIPS-10-4-Laendercodes; Zuordnung zu ISO-2 fuer die Laender, die fuer uns zaehlen.</summary>
    public static readonly IReadOnlyDictionary<string, string> FipsToIso = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["SZ"] = "CH", ["GM"] = "DE", ["AU"] = "AT", ["IT"] = "IT", ["FR"] = "FR", ["SP"] = "ES", ["PO"] = "PT", ["UK"] = "GB",
        ["EI"] = "IE", ["NL"] = "NL", ["BE"] = "BE", ["LU"] = "LU", ["DA"] = "DK", ["SW"] = "SE", ["NO"] = "NO", ["FI"] = "FI",
        ["PL"] = "PL", ["EZ"] = "CZ", ["LO"] = "SK", ["HU"] = "HU", ["SI"] = "SI", ["HR"] = "HR", ["RI"] = "RS", ["RO"] = "RO",
        ["BU"] = "BG", ["GR"] = "GR", ["TU"] = "TR", ["UP"] = "UA", ["RS"] = "RU", ["BO"] = "BY", ["EN"] = "EE", ["LG"] = "LV",
        ["LH"] = "LT", ["US"] = "US", ["CA"] = "CA", ["MX"] = "MX", ["BR"] = "BR", ["AR"] = "AR", ["CI"] = "CL", ["CO"] = "CO",
        ["PE"] = "PE", ["CH"] = "CN", ["JA"] = "JP", ["KS"] = "KR", ["TW"] = "TW", ["HK"] = "HK", ["IN"] = "IN", ["PK"] = "PK",
        ["TH"] = "TH", ["VM"] = "VN", ["MY"] = "MY", ["SN"] = "SG", ["ID"] = "ID", ["RP"] = "PH", ["AE"] = "AE", ["SA"] = "SA",
        ["QA"] = "QA", ["KU"] = "KW", ["IS"] = "IL", ["IR"] = "IR", ["IZ"] = "IQ", ["EG"] = "EG", ["MO"] = "MA", ["SF"] = "ZA",
        ["NI"] = "NG", ["KE"] = "KE", ["AS"] = "AU", ["NZ"] = "NZ", ["BG"] = "BD", ["CE"] = "LK", ["KZ"] = "KZ", ["AG"] = "DZ",
        ["TS"] = "TN", ["JO"] = "JO", ["LE"] = "LB", ["OM"] = "OM", ["BA"] = "BH", ["SY"] = "SY", ["YM"] = "YE", ["LY"] = "LY",
        ["VE"] = "VE", ["EC"] = "EC", ["UY"] = "UY", ["MT"] = "MT", ["CY"] = "CY", ["IC"] = "IS", ["LS"] = "LI", ["MK"] = "MK",
        ["BK"] = "BA", ["MJ"] = "ME", ["AL"] = "AL", ["MD"] = "MD", ["GG"] = "GE", ["AM"] = "AM", ["AJ"] = "AZ", ["UZ"] = "UZ"
    };

    /// <summary>Eine Zeile der GDELT-2.0-Ereignisdatei (61 Spalten, Tab). Null, wenn das Land nicht zugeordnet ist.</summary>
    public static WorldEvent? ParseGdeltEvent(string line)
    {
        var c = line.Split('\t');
        if (c.Length < 61) return null;
        if (!FipsToIso.TryGetValue(c[53], out var iso)) return null;
        var time = DateTime.TryParseExact(c[59], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t) ? t : DateTime.MinValue;
        return new WorldEvent(time, iso, c[52],
            int.TryParse(c[29], out var q) ? q : 0, c[28],
            double.TryParse(c[30], NumberStyles.Float, CultureInfo.InvariantCulture, out var g) ? g : 0,
            double.TryParse(c[34], NumberStyles.Float, CultureInfo.InvariantCulture, out var tone) ? tone : 0,
            int.TryParse(c[31], out var m) ? m : 0, c[60]);
    }

    /// <summary>Tageswerte je Land: QuadClass 3/4 = Konflikt, Ereignisart 14 = Protest.</summary>
    public static IReadOnlyList<WorldEventDay> Aggregate(IEnumerable<WorldEvent> events)
        => events.GroupBy(e => (Day: DateOnly.FromDateTime(e.TimeUtc), e.Country))
            .Select(g => new WorldEventDay(g.Key.Day, g.Key.Country, g.Count(), g.Count(e => e.QuadClass >= 3), g.Count(e => e.RootCode == "14"),
                g.Sum(e => (long)e.Mentions), g.Sum(e => e.Tone), g.Sum(e => e.Goldstein)))
            .ToList();

    /// <summary>Adresse der juengsten Ereignisdatei aus lastupdate.txt.</summary>
    public static string? LatestExportUrl(string lastUpdate)
        => lastUpdate.Split('\n').Select(l => l.Trim().Split(' ').LastOrDefault() ?? "")
            .FirstOrDefault(u => u.EndsWith(".export.CSV.zip", StringComparison.OrdinalIgnoreCase));

    /// <summary>Zeitstempel der Datei, z. B. 20261005063000.</summary>
    public static DateTime? StampOf(string url)
    {
        var name = url[(url.LastIndexOf('/') + 1)..];
        return name.Length >= 14 && DateTime.TryParseExact(name[..14], "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t) ? t : null;
    }

    public static string ExportUrl(DateTime stampUtc) => $"http://data.gdeltproject.org/gdeltv2/{stampUtc:yyyyMMddHHmmss}.export.CSV.zip";

    /// <summary>FRED-CSV "observation_date,SERIE"; Werte "." (fehlend) werden uebersprungen.</summary>
    public static IReadOnlyList<(DateOnly, double)> ParseFredCsv(string csv)
        => csv.Split('\n').Skip(1)
            .Select(l => l.Trim().Split(','))
            .Where(p => p.Length >= 2 && DateOnly.TryParse(p[0], CultureInfo.InvariantCulture, out _)
                        && double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .Select(p => (DateOnly.Parse(p[0], CultureInfo.InvariantCulture), double.Parse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture)))
            .ToList();

    /// <summary>
    /// Eurostat JSON-stat 2.0 mit einer Zeitdimension (Monate "2026-07") fuer ein Land. JSON-stat kennt beide Formen:
    /// <c>value</c> als Objekt (Position als Schluessel, nur vorhandene Werte) oder als Array (Position = Index, null = fehlt);
    /// <c>index</c> der Zeit als Objekt (Name zu Position) oder als Array der Namen (Position = Index).
    /// </summary>
    public static IReadOnlyList<(DateOnly, double)> ParseEurostat(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("value", out var values) || !root.TryGetProperty("dimension", out var dims)) return [];
        var index = dims.GetProperty("time").GetProperty("category").GetProperty("index");

        JsonElement? ValueAt(int pos)
        {
            if (values.ValueKind == JsonValueKind.Object)
                return values.TryGetProperty(pos.ToString(CultureInfo.InvariantCulture), out var o) ? o : null;
            if (values.ValueKind == JsonValueKind.Array && pos >= 0 && pos < values.GetArrayLength())
                return values[pos];
            return null;
        }

        IEnumerable<(string Name, int Pos)> Periods()
        {
            if (index.ValueKind == JsonValueKind.Object)
                foreach (var t in index.EnumerateObject())
                    yield return (t.Name, t.Value.GetInt32());
            else if (index.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var t in index.EnumerateArray())
                    yield return (t.GetString() ?? "", i++);
            }
        }

        var result = new List<(DateOnly, double)>();
        foreach (var (name, pos) in Periods())
        {
            if (ValueAt(pos) is not { ValueKind: JsonValueKind.Number } v) continue;
            var parts = name.Split('-', 'M');
            if (parts.Length < 2 || !int.TryParse(parts[0], out var y) || !int.TryParse(parts[^1], out var mo)) continue;
            result.Add((new DateOnly(y, mo, 1), v.GetDouble()));
        }
        return result.OrderBy(r => r.Item1).ToList();
    }

    /// <summary>Doppelte Meldungen (dieselbe Quelle erscheint in mehreren 15-Minuten-Dateien oder fuer mehrere Orte) auf eine je Adresse und Land und Tag reduzieren; die meistbeachtete bleibt.</summary>
    public static IReadOnlyList<WorldEvent> DedupeEvents(IEnumerable<WorldEvent> events)
        => events.GroupBy(e => e.Url.Length > 0
                ? e.Url.Trim().ToLowerInvariant() + "|" + e.Country
                : $"{e.Country}|{e.Place}|{e.RootCode}|{DateOnly.FromDateTime(e.TimeUtc):yyyy-MM-dd}")
            .Select(g => g.OrderByDescending(e => e.Mentions).First())
            .ToList();

    /// <summary>IMF DataMapper NGDP_RPCH: Wachstum je ISO-3-Land fuer zwei Jahre.</summary>
    public static IReadOnlyList<WorldGrowth> ParseImf(string json, IEnumerable<string> iso2, int firstYear)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("values", out var values) || !values.TryGetProperty("NGDP_RPCH", out var series)) return [];
        var result = new List<WorldGrowth>();
        foreach (var country in iso2.Distinct())
        {
            var iso3 = Iso3(country);
            if (iso3 is null || !series.TryGetProperty(iso3, out var years)) continue;
            double? Year(int y) => years.TryGetProperty(y.ToString(CultureInfo.InvariantCulture), out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
            result.Add(new WorldGrowth(country, Year(firstYear), Year(firstYear + 1), firstYear));
        }
        return result;
    }

    /// <summary>EZB-Referenzkurse (eurofxref-hist-90d.xml): je Waehrung Kurs pro EUR und Tag, dazu EUR = 1.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<(DateOnly, double)>> ParseEcbHistory(string xml)
    {
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var result = new Dictionary<string, List<(DateOnly, double)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var day in doc.Descendants().Where(e => e.Name.LocalName == "Cube" && e.Attribute("time") is not null))
        {
            if (!DateOnly.TryParseExact(day.Attribute("time")!.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) continue;
            (result.TryGetValue("EUR", out var eur) ? eur : result["EUR"] = []).Add((date, 1));
            foreach (var c in day.Elements().Where(e => e.Attribute("currency") is not null))
                if (double.TryParse(c.Attribute("rate")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate) && rate > 0)
                    (result.TryGetValue(c.Attribute("currency")!.Value, out var l) ? l : result[c.Attribute("currency")!.Value] = []).Add((date, rate));
        }
        return result.ToDictionary(r => r.Key, r => (IReadOnlyList<(DateOnly, double)>)r.Value.OrderBy(p => p.Item1).ToList(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Kursveraenderung einer Waehrung gegen CHF in Prozent vom ersten bis zum letzten gemeinsamen Tag.</summary>
    public static double? ChangeAgainstChf(IReadOnlyDictionary<string, IReadOnlyList<(DateOnly, double)>> perEur, string currency)
    {
        if (!perEur.TryGetValue("CHF", out var chf) || !perEur.TryGetValue(currency, out var cur)) return null;
        var common = cur.Join(chf, a => a.Item1, b => b.Item1, (a, b) => (a.Item1, Value: b.Item2 / a.Item2)).OrderBy(x => x.Item1).ToList();
        if (common.Count < 2 || common[0].Value == 0) return null;
        return (common[^1].Value - common[0].Value) / common[0].Value * 100;
    }

    public static string? Iso3(string iso2)
    {
        try { return new RegionInfo(iso2).ThreeLetterISORegionName; }
        catch (ArgumentException) { return null; }
    }

    /// <summary>Eurostat verwendet EL fuer Griechenland; GB und CH sind nicht in der Industrieproduktion der EU.</summary>
    public static string EurostatGeo(string iso2) => iso2 == "GR" ? "EL" : iso2;
}
