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

    /// <summary>Eurostat JSON-stat 2.0 mit einer Zeitdimension (Monate "2026-07") fuer ein Land.</summary>
    public static IReadOnlyList<(DateOnly, double)> ParseEurostat(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("value", out var values) || !root.TryGetProperty("dimension", out var dims)) return [];
        var index = dims.GetProperty("time").GetProperty("category").GetProperty("index");
        var result = new List<(DateOnly, double)>();
        foreach (var t in index.EnumerateObject())
        {
            var pos = t.Value.GetInt32().ToString(CultureInfo.InvariantCulture);
            if (!values.TryGetProperty(pos, out var v) || v.ValueKind != JsonValueKind.Number) continue;
            var parts = t.Name.Split('-', 'M');
            if (parts.Length < 2 || !int.TryParse(parts[0], out var y) || !int.TryParse(parts[^1], out var mo)) continue;
            result.Add((new DateOnly(y, mo, 1), v.GetDouble()));
        }
        return result.OrderBy(r => r.Item1).ToList();
    }

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

    public static string? Iso3(string iso2)
    {
        try { return new RegionInfo(iso2).ThreeLetterISORegionName; }
        catch (ArgumentException) { return null; }
    }

    /// <summary>Eurostat verwendet EL fuer Griechenland; GB und CH sind nicht in der Industrieproduktion der EU.</summary>
    public static string EurostatGeo(string iso2) => iso2 == "GR" ? "EL" : iso2;
}
