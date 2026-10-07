using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Forecast naechste Woche: geplante Auftraege (SAP) und manuelle Personalplanung pro Abteilung und Tag.
/// Die Berechnung (Leistung aus Historie, Kapazitaet, Bedarf, Empfehlungen) passiert wie im Original in der
/// Oberflaeche. Hier werden nur Auftraege und Planwerte gespeichert. Port von forecast.py.
/// </summary>
public static class ShopfloorForecast
{
    /// <summary>Zuordnung Disponent/Fertigungssteuerer zu Abteilung (laengster passender Anfang gewinnt).</summary>
    public static readonly Dictionary<string, string[]> DefaultPrefix = new()
    {
        ["TX"] = ["EL", "TX"],
        ["TR5"] = ["EK", "TR5"],
        ["DW"] = ["D", "DS", "DEF", "DW"],
        ["SEH"] = ["012", "SK", "SEH"],
        ["CZ"] = ["CZ"]
    };

    public static readonly string[] Depts = ["SEH", "TR5", "TX", "DW", "CZ"];

    public static string AssignDept(JsonObject r, IReadOnlyDictionary<string, string[]>? prefixes = null)
    {
        prefixes ??= DefaultPrefix;
        var d = SfJson.Str(SfJson.Get(r, "abteilung")).Trim().ToUpperInvariant();
        if (Depts.Contains(d))
            return d;
        var disp = SfJson.Str(SfJson.Get(r, "disponent")).Trim().ToUpperInvariant();
        string? best = null;
        var bl = 0;
        foreach (var (dept, list) in prefixes)
            foreach (var p in list)
                if (disp.StartsWith(p, StringComparison.Ordinal) && p.Length > bl)
                {
                    best = dept;
                    bl = p.Length;
                }
        return best ?? "?";
    }

    private static readonly string[] OrderFields = ["auftrag", "material", "text", "menge", "disponent", "arbeitsplatz", "art", "kunde"];

    /// <summary>Ersetzt alle Auftraege im Datumsbereich der Datei.</summary>
    public static JsonObject ImportOrders(SqliteConnection con, IEnumerable<JsonNode?> rows, string user, string source = "Upload",
        string filename = "", IReadOnlyDictionary<string, string[]>? prefixes = null)
    {
        var valid = rows.OfType<JsonObject>()
            .Where(r => SfJson.Truthy(SfJson.Get(r, "day")) && !IsEmpty(SfJson.Get(r, "menge")))
            .ToList();
        if (valid.Count == 0)
            throw new ShopfloorException("Keine Aufträge mit Datum und Menge gefunden");
        var days = valid.Select(r => Day10(r)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var von = days[0];
        var bis = days[^1];
        ShopfloorDb.Exec(con, "DELETE FROM fc_orders WHERE day BETWEEN ? AND ?", von, bis);
        var ts = ShopfloorDb.Now();
        var per = new JsonObject();
        foreach (var r in valid)
        {
            var dept = SfJson.Truthy(SfJson.Get(r, "dept")) ? SfJson.PyStr(SfJson.Get(r, "dept")) : AssignDept(r, prefixes);
            var d = new JsonObject();
            foreach (var k in OrderFields)
                if (!IsEmpty(SfJson.Get(r, k)))
                    d[k] = SfJson.Clone(SfJson.Get(r, k));
            ShopfloorDb.Exec(con, "INSERT INTO fc_orders(day,dept,data,imported_at,imported_by) VALUES(?,?,?,?,?)",
                Day10(r), dept, SfJson.Dump(d), ts, user);
            per[dept] = (per[dept] is { } n ? (int)SfJson.AsDouble(n)! : 0) + 1;
        }
        ShopfloorDb.Exec(con, "INSERT INTO fc_imports(ts,user,source,filename,von,bis,anzahl) VALUES(?,?,?,?,?,?,?)",
            ts, user, source, filename, von, bis, valid.Count);
        return new JsonObject { ["anzahl"] = valid.Count, ["von"] = von, ["bis"] = bis, ["pro_abteilung"] = per };
    }

    private static string Day10(JsonObject r)
    {
        var s = SfJson.PyStr(SfJson.Get(r, "day"));
        return s.Length > 10 ? s[..10] : s;
    }

    private static bool IsEmpty(JsonNode? n)
        => n is null || (n is JsonValue v && (v.GetValueKind() == System.Text.Json.JsonValueKind.Null ||
                                              (v.GetValueKind() == System.Text.Json.JsonValueKind.String && v.GetValue<string>().Length == 0)));

    public static JsonObject GetWeek(SqliteConnection con, string? monday, IReadOnlyDictionary<string, string[]>? prefixes = null)
    {
        if (monday is null || !DateTime.TryParseExact(monday, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var mon))
            throw new ShopfloorException("Datum ungültig");
        var sunday = mon.AddDays(6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var orders = new JsonArray();
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM fc_orders WHERE day BETWEEN ? AND ? ORDER BY day, dept", monday, sunday))
        {
            var d = SfJson.ParseObject(r.Str("data"));
            d["id"] = r.Long("id");
            d["day"] = r.Str("day");
            d["dept"] = r.Str("dept");
            orders.Add(d);
        }
        var plan = new JsonObject();
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM fc_plan WHERE day BETWEEN ? AND ?", monday, sunday))
        {
            var dept = r.Str("dept") ?? string.Empty;
            if (plan[dept] is not JsonObject byDay)
                plan[dept] = byDay = new JsonObject();
            var d = SfJson.ParseObject(r.Str("data"));
            d["updated_by"] = r.Str("updated_by");
            d["updated_at"] = r.Str("updated_at");
            byDay[r.Str("day") ?? string.Empty] = d;
        }
        var settings = new JsonObject();
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM fc_settings"))
            settings[r.Str("dept") ?? string.Empty] = SfJson.ParseObject(r.Str("data"));

        var imp = ShopfloorDb.QueryOne(con, "SELECT * FROM fc_imports WHERE bis>=? AND von<=? ORDER BY id DESC LIMIT 1", monday, sunday);
        JsonNode? impNode = null;
        if (imp is not null)
            impNode = new JsonObject
            {
                ["id"] = imp.Long("id"), ["ts"] = imp.Str("ts"), ["user"] = imp.Str("user"), ["source"] = imp.Str("source"),
                ["filename"] = imp.Str("filename"), ["von"] = imp.Str("von"), ["bis"] = imp.Str("bis"), ["anzahl"] = imp.Long("anzahl")
            };

        var prefix = new JsonObject();
        foreach (var (dept, list) in prefixes ?? DefaultPrefix)
        {
            var arr = new JsonArray();
            foreach (var p in list) arr.Add(p);
            prefix[dept] = arr;
        }
        return new JsonObject
        {
            ["monday"] = monday, ["orders"] = orders, ["plan"] = plan, ["settings"] = settings, ["import"] = impNode, ["prefix"] = prefix
        };
    }

    private static double? Num(JsonNode? v)
    {
        if (IsEmpty(v)) return null;
        var s = SfJson.PyStr(v).Replace("'", "").Replace(",", ".");
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && double.IsFinite(d) ? d : null;
    }

    public static JsonObject SavePlan(SqliteConnection con, string dept, string day, JsonObject changes, string user)
    {
        var r = ShopfloorDb.QueryOne(con, "SELECT data FROM fc_plan WHERE dept=? AND day=?", dept, day);
        var d = SfJson.ParseObject(r?.Str("data"));
        foreach (var kv in changes)
        {
            if (kv.Key is "ma1" or "ma2" or "ma3" or "plan" or "frei")
            {
                var n = Num(kv.Value);
                if (n is null) d.Remove(kv.Key);
                else d[kv.Key] = SfJson.Num(n.Value);
            }
            else if (kv.Key == "notiz")
            {
                if (SfJson.Truthy(kv.Value))
                {
                    var s = SfJson.PyStr(kv.Value);
                    d[kv.Key] = s.Length > 500 ? s[..500] : s;
                }
                else d.Remove(kv.Key);
            }
        }
        var ts = ShopfloorDb.Now();
        ShopfloorDb.Exec(con, "INSERT OR REPLACE INTO fc_plan(dept,day,data,updated_at,updated_by) VALUES(?,?,?,?,?)",
            dept, day, SfJson.Dump(d), ts, user);
        var ch = new JsonObject();
        foreach (var kv in changes)
            ch[kv.Key] = SfJson.Pair(null, kv.Value);
        ShopfloorDb.Exec(con, "INSERT INTO history(module,ref,ts,user,source,changes) VALUES(?,?,?,?,?,?)",
            "forecast", $"{dept}/{day}", ts, user, "manuell", SfJson.Dump(ch));
        return d;
    }

    public static JsonObject SaveSettings(SqliteConnection con, string dept, JsonObject changes, string user)
    {
        var r = ShopfloorDb.QueryOne(con, "SELECT data FROM fc_settings WHERE dept=?", dept);
        var d = SfJson.ParseObject(r?.Str("data"));
        foreach (var kv in changes)
        {
            if (kv.Key is not ("leistung" or "ma_pro_schicht" or "ziel_auslastung")) continue;
            var n = Num(kv.Value);
            if (n is null) d.Remove(kv.Key);
            else d[kv.Key] = SfJson.Num(n.Value);
        }
        ShopfloorDb.Exec(con, "INSERT OR REPLACE INTO fc_settings(dept,data,updated_at,updated_by) VALUES(?,?,?,?)",
            dept, SfJson.Dump(d), ShopfloorDb.Now(), user);
        return d;
    }
}
