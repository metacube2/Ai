using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>
/// Einkauf, Fehlteileliste aus dem SAP-Report ZD05 (Tages-Snapshots mit Uebernahme der
/// Einkaufs-Bemerkungen). Port von zd05.py. Excel/CSV wird im Browser gelesen, hier kommen Zeilen als JSON an.
/// </summary>
public static class ShopfloorZd05
{
    public static readonly string[] Auto = ["text", "unterdeck", "dmk", "lzcode", "verbr_wbz", "opt_sibe", "akt_sibe", "ant_plan"];
    public static readonly string[] Manual = ["bemerkung", "liefertermin", "code"];
    public const int HorizonWorkdays = 3;   // "Aktueller Tag + 3 AT"
    public const double Target = 1.20;      // Ziel Bewertungskennzahl

    private static string Now() => ShopfloorDb.Now();

    public static DateTime AddWorkdays(DateTime d, int n)
    {
        while (n > 0)
        {
            d = d.AddDays(1);
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                n--;
        }
        return d;
    }

    public static DateTime ParseDay(string? day)
    {
        if (day is not null && DateTime.TryParseExact(day.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        throw new ShopfloorException("Datum ungültig");
    }

    /// <summary>Code 0, 1 oder 2; alles andere (leer, Text) ist "ohne Code".</summary>
    public static int? CodeOf(JsonNode? v)
    {
        double? d;
        if (SfJson.IsNumber(v)) d = SfJson.AsDouble(v);
        else if (v is null) d = null;
        else
        {
            d = double.TryParse(SfJson.PyStr(v).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var p) ? p : null;
        }
        if (d is null || !double.IsFinite(d.Value))
            return null;
        var c = (int)Math.Truncate(d.Value);
        return c is 0 or 1 or 2 ? c : null;
    }

    public static JsonObject ComputeKpi(string day, List<JsonObject> rows, Dictionary<string, (string? Abc, string? Xyz)> abc)
    {
        var horizon = AddWorkdays(ParseDay(day), HorizonWorkdays).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var inH = rows.Where(r =>
        {
            var u = SfJson.Get(r, "unterdeck");
            if (!SfJson.Truthy(u)) return true;
            var s = SfJson.PyStr(u);
            return string.CompareOrdinal(s.Length > 10 ? s[..10] : s, horizon) <= 0;
        }).ToList();
        var codes = inH.Select(r => CodeOf(SfJson.Get(r, "code"))).ToList();
        var coded = codes.Where(c => c is not null).Select(c => c!.Value).ToList();
        var k = new JsonObject
        {
            ["horizon"] = horizon,
            ["positionen"] = rows.Count,
            ["im_horizont"] = inH.Count,
            ["anzahl"] = coded.Count,
            ["summe"] = coded.Sum(),
            ["kennzahl"] = coded.Count > 0 ? Math.Round((double)coded.Sum() / coded.Count, 4) : null,
            ["code2"] = codes.Count(c => c == 2),
            ["ohne_code"] = rows.Count(r => CodeOf(SfJson.Get(r, "code")) is null)
        };
        foreach (var cls in "ABCXYZ")
            k["c2_" + cls] = 0;
        foreach (var r in rows)
        {
            if (CodeOf(SfJson.Get(r, "code")) != 2)
                continue;
            abc.TryGetValue(SfJson.PyStr(SfJson.Get(r, "material")), out var a);
            Bump(k, a.Abc);
            Bump(k, a.Xyz);
        }
        return k;
    }

    private static void Bump(JsonObject k, string? cls)
    {
        if (string.IsNullOrEmpty(cls)) return;
        var key = "c2_" + cls.ToUpperInvariant();
        k[key] = (k[key] is { } n ? (int)SfJson.AsDouble(n)! : 0) + 1;
    }

    public static Dictionary<string, (string? Abc, string? Xyz)> AbcMap(SqliteConnection con)
    {
        var map = new Dictionary<string, (string?, string?)>(StringComparer.Ordinal);
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM abc_xyz"))
            map[r.Str("material") ?? string.Empty] = (r.Str("abc"), r.Str("xyz"));
        return map;
    }

    public static List<JsonObject> LoadDay(SqliteConnection con, string day)
    {
        var list = new List<JsonObject>();
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM zd05_rows WHERE day=? ORDER BY material", day))
        {
            var d = SfJson.ParseObject(r.Str("data"));
            d["material"] = r.Str("material");
            d["updated_by"] = r.Str("updated_by");
            d["updated_at"] = r.Str("updated_at");
            list.Add(d);
        }
        return list;
    }

    private static string? PrevDay(SqliteConnection con, string day)
        => ShopfloorDb.QueryOne(con, "SELECT max(day) d FROM zd05_days WHERE day<?", day)?.Str("d");

    public static void RefreshKpi(SqliteConnection con, string day)
    {
        var d = ShopfloorDb.QueryOne(con, "SELECT kpi_fixed FROM zd05_days WHERE day=?", day);
        if (d is not null && d.Long("kpi_fixed") != 0)
            return;
        var k = ComputeKpi(day, LoadDay(con, day), AbcMap(con));
        ShopfloorDb.Exec(con, "UPDATE zd05_days SET kpi=? WHERE day=?", SfJson.Dump(k), day);
    }

    /// <summary>
    /// Ersetzt den Snapshot eines Tages. Manuelle Felder (Bemerkung, Liefertermin, Code) werden vom gleichen
    /// Tag (falls schon vorhanden) oder vom letzten frueheren Tag pro Material uebernommen.
    /// </summary>
    public static JsonObject ImportRows(SqliteConnection con, string day, IEnumerable<JsonNode?> rows, string user,
        string source = "Upload", string filename = "", JsonObject? fixedKpi = null, bool keepManualFromRows = false)
    {
        var ts = Now();
        var same = LoadDay(con, day).ToDictionary(r => SfJson.PyStr(r["material"]), r => r);
        var prevDay = PrevDay(con, day);
        var prev = prevDay is null
            ? new Dictionary<string, JsonObject>()
            : LoadDay(con, prevDay).ToDictionary(r => SfJson.PyStr(r["material"]), r => r);
        ShopfloorDb.Exec(con, "DELETE FROM zd05_rows WHERE day=?", day);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var neu = 0;
        foreach (var node in rows)
        {
            if (node is not JsonObject r) continue;
            var m = SfJson.Str(SfJson.Get(r, "material")).Trim();
            if (m.Length == 0 || !seen.Add(m)) continue;

            var d = new JsonObject();
            foreach (var k in Auto)
                if (Present(SfJson.Get(r, k)))
                    d[k] = SfJson.Clone(SfJson.Get(r, k));
            same.TryGetValue(m, out var s1);
            prev.TryGetValue(m, out var s2);
            var src = s1 ?? s2;
            foreach (var k in Manual)
            {
                if (keepManualFromRows && Present(SfJson.Get(r, k)))
                    d[k] = SfJson.Clone(SfJson.Get(r, k));
                else if (src is not null && Present(SfJson.Get(src, k)))
                    d[k] = SfJson.Clone(SfJson.Get(src, k));
            }
            if (!prev.ContainsKey(m))
            {
                d["neu"] = true;
                neu++;
            }
            ShopfloorDb.Exec(con, "INSERT INTO zd05_rows(day,material,data,updated_at,updated_by) VALUES(?,?,?,?,?)",
                day, m, SfJson.Dump(d), ts, user);
        }

        var resolved = new JsonArray();
        foreach (var m in prev.Keys)
            if (!seen.Contains(m))
                resolved.Add(m);

        ShopfloorDb.Exec(con, "INSERT OR REPLACE INTO zd05_days(day,imported_at,imported_by,source,filename,kpi,kpi_fixed) VALUES(?,?,?,?,?,?,?)",
            day, ts, user, source, filename, fixedKpi is null ? null : SfJson.Dump(fixedKpi), fixedKpi is null ? 0 : 1);
        if (fixedKpi is null)
            RefreshKpi(con, day);
        return new JsonObject
        {
            ["day"] = day, ["positionen"] = seen.Count, ["neu"] = neu, ["erledigt"] = resolved, ["vortag"] = prevDay
        };
    }

    private static bool Present(JsonNode? n)
        => n is not null && !(n is JsonValue v && (v.GetValueKind() == System.Text.Json.JsonValueKind.Null ||
                                                   (v.GetValueKind() == System.Text.Json.JsonValueKind.String && v.GetValue<string>().Length == 0)));

    public static JsonObject GetDay(SqliteConnection con, string? day = null)
    {
        day = string.IsNullOrEmpty(day) ? ShopfloorDb.QueryOne(con, "SELECT max(day) d FROM zd05_days")?.Str("d") : day;
        var days = new JsonArray();
        foreach (var r in ShopfloorDb.Query(con, "SELECT day FROM zd05_days ORDER BY day DESC LIMIT 400"))
            days.Add(r.Str("day"));
        if (day is null)
            return new JsonObject { ["day"] = null, ["rows"] = new JsonArray(), ["days"] = days, ["kpi"] = null };

        var meta = ShopfloorDb.QueryOne(con, "SELECT * FROM zd05_days WHERE day=?", day);
        var rows = LoadDay(con, day);
        var abc = AbcMap(con);
        foreach (var r in rows)
        {
            abc.TryGetValue(SfJson.PyStr(r["material"]), out var a);
            r["abc"] = a.Abc;
            r["xyz"] = a.Xyz;
        }
        var prevDay = PrevDay(con, day);
        var resolved = new JsonArray();
        if (prevDay is not null)
        {
            var cur = rows.Select(r => SfJson.PyStr(r["material"])).ToHashSet(StringComparer.Ordinal);
            foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM zd05_rows WHERE day=?", prevDay))
            {
                var mat = r.Str("material") ?? string.Empty;
                if (cur.Contains(mat)) continue;
                var d = SfJson.ParseObject(r.Str("data"));
                d["material"] = mat;
                resolved.Add(d);
            }
        }
        var live = ComputeKpi(day, rows, abc);
        JsonNode kpi = meta is not null && !meta.IsNull("kpi") && (meta.Str("kpi") ?? "").Length > 0
            ? JsonNode.Parse(meta.Str("kpi")!)!
            : live.DeepClone();
        var rowsArr = new JsonArray();
        foreach (var r in rows) rowsArr.Add(r);
        return new JsonObject
        {
            ["day"] = day,
            ["rows"] = rowsArr,
            ["days"] = days,
            ["kpi"] = kpi,
            ["kpi_live"] = live,
            ["kpi_fixed"] = meta is not null && meta.Long("kpi_fixed") != 0,
            ["imported_at"] = meta?.Str("imported_at"),
            ["imported_by"] = meta?.Str("imported_by"),
            ["source"] = meta?.Str("source"),
            ["prev_day"] = prevDay,
            ["resolved"] = resolved,
            ["target"] = Target,
            ["horizon_days"] = HorizonWorkdays
        };
    }

    public static JsonObject SaveRow(SqliteConnection con, string day, string material, JsonObject changes, string user)
    {
        var r = ShopfloorDb.QueryOne(con, "SELECT * FROM zd05_rows WHERE day=? AND material=?", day, material)
            ?? throw new ShopfloorException("Position nicht gefunden");
        var d = SfJson.ParseObject(r.Str("data"));
        var old = new Dictionary<string, JsonNode?>();
        foreach (var kv in changes)
            old[kv.Key] = SfJson.Clone(SfJson.Get(d, kv.Key));
        foreach (var kv in changes)
        {
            if (!Manual.Contains(kv.Key)) continue;
            var v = kv.Value;
            if (kv.Key == "code")
            {
                var c = CodeOf(v);
                v = c is null ? null : JsonValue.Create(c.Value);
            }
            if (!Present(v)) d.Remove(kv.Key);
            else d[kv.Key] = SfJson.Clone(v);
        }
        var ts = Now();
        ShopfloorDb.Exec(con, "UPDATE zd05_rows SET data=?, updated_at=?, updated_by=? WHERE day=? AND material=?",
            SfJson.Dump(d), ts, user, day, material);
        var ch = new JsonObject();
        foreach (var kv in changes)
            ch[kv.Key] = SfJson.Pair(old[kv.Key], SfJson.Get(d, kv.Key));
        ShopfloorDb.Exec(con, "INSERT INTO history(module,ref,ts,user,source,changes) VALUES(?,?,?,?,?,?)",
            "zd05", $"{day}/{material}", ts, user, "manuell", SfJson.Dump(ch));
        // Tages-Kennzahl neu rechnen (Excel-Werte gelten nur, bis jemand den Tag bearbeitet)
        ShopfloorDb.Exec(con, "UPDATE zd05_days SET kpi_fixed=0 WHERE day=?", day);
        RefreshKpi(con, day);
        d["material"] = material;
        d["updated_by"] = user;
        d["updated_at"] = ts;
        return d;
    }

    public static JsonObject History(SqliteConnection con, int limit = 260)
    {
        var days = new List<JsonObject>();
        foreach (var r in ShopfloorDb.Query(con, "SELECT day,kpi FROM zd05_days ORDER BY day DESC LIMIT ?", limit))
            days.Add(new JsonObject
            {
                ["day"] = r.Str("day"),
                ["kpi"] = string.IsNullOrEmpty(r.Str("kpi")) ? null : JsonNode.Parse(r.Str("kpi")!)
            });
        days.Reverse();
        var weeks = new JsonArray();
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM zd05_week ORDER BY kw"))
        {
            var w = new JsonObject { ["kw"] = r.Str("kw") };
            foreach (var kv in SfJson.ParseObject(r.Str("data")))
                w[kv.Key] = SfJson.Clone(kv.Value);
            weeks.Add(w);
        }
        var arr = new JsonArray();
        foreach (var d in days) arr.Add(d);
        return new JsonObject { ["days"] = arr, ["weeks"] = weeks, ["target"] = Target };
    }

    public static JsonObject SaveWeek(SqliteConnection con, string kw, JsonObject changes, string user)
    {
        var r = ShopfloorDb.QueryOne(con, "SELECT data FROM zd05_week WHERE kw=?", kw);
        var d = SfJson.ParseObject(r?.Str("data"));
        foreach (var kv in changes)
        {
            if (kv.Key is not ("mat_verf" or "off_best")) continue;
            if (!Present(kv.Value))
            {
                d.Remove(kv.Key);
                continue;
            }
            var s = SfJson.PyStr(kv.Value).Replace(",", ".").Replace("'", "").Replace("%", "");
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                throw new ShopfloorException($"Zahl ungültig: {s}");
            d[kv.Key] = num;
        }
        ShopfloorDb.Exec(con, "INSERT OR REPLACE INTO zd05_week(kw,data,updated_at,updated_by) VALUES(?,?,?,?)",
            kw, SfJson.Dump(d), Now(), user);
        d["kw"] = kw;
        return d;
    }

    public static int ReplaceAbc(SqliteConnection con, IEnumerable<JsonNode?> rows)
    {
        ShopfloorDb.Exec(con, "DELETE FROM abc_xyz");
        var n = 0;
        foreach (var node in rows)
        {
            if (node is not JsonObject r) continue;
            var m = SfJson.Str(SfJson.Get(r, "material")).Trim();
            if (m.Length == 0) continue;
            ShopfloorDb.Exec(con, "INSERT OR REPLACE INTO abc_xyz(material,abc,xyz,text,art) VALUES(?,?,?,?,?)",
                m, First(SfJson.Str(SfJson.Get(r, "abc"))), First(SfJson.Str(SfJson.Get(r, "xyz"))),
                SfJson.GetString(r, "text"), SfJson.GetString(r, "art"));
            n++;
        }
        return n;
    }

    private static string First(string s)
    {
        var t = s.Trim().ToUpperInvariant();
        return t.Length > 0 ? t[..1] : string.Empty;
    }

    public static JsonObject StatusToday(SqliteConnection con, string day)
    {
        var r = ShopfloorDb.QueryOne(con, "SELECT * FROM zd05_days WHERE day=?", day);
        var last = ShopfloorDb.QueryOne(con, "SELECT max(day) d FROM zd05_days")?.Str("d");
        JsonNode? k = r is not null && !string.IsNullOrEmpty(r.Str("kpi")) ? JsonNode.Parse(r.Str("kpi")!) : null;
        return new JsonObject
        {
            ["imported"] = r is not null,
            ["at"] = r?.Str("imported_at"),
            ["by"] = r?.Str("imported_by"),
            ["kpi"] = k,
            ["last"] = last
        };
    }
}
