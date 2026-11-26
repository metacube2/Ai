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

    /// <summary>Arbeitstage nach dem Fabrikkalender CH von Werk 1100 (Wunsch Marco/Fabio 24.11.2026, vorher nur Mo bis Fr).</summary>
    public static DateTime AddWorkdays(DateTime d, int n)
    {
        while (n > 0)
        {
            d = d.AddDays(1);
            if (ShopfloorKalender.IstArbeitstag(DateOnly.FromDateTime(d)))
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
        var byKw = new SortedDictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM zd05_week ORDER BY kw"))
        {
            var w = new JsonObject { ["kw"] = r.Str("kw") };
            foreach (var kv in SfJson.ParseObject(r.Str("data")))
                w[kv.Key] = SfJson.Clone(kv.Value);
            byKw[r.Str("kw") ?? string.Empty] = w;
        }
        foreach (var z in WeekCounters(days))
        {
            if (!byKw.TryGetValue(z.Kw, out var w))
                byKw[z.Kw] = w = new JsonObject { ["kw"] = z.Kw };
            w["c2_max"] = z.Max;
            w["c2_max_day"] = z.Day;
            w["c2_tage"] = z.Tage;
        }
        foreach (var w in byKw.Values)
        {
            // Nenner (Entscheid Fabio E3, 25.11.2026): SAP-Wert ist Standard, ein Handwert geht vor und wird gekennzeichnet
            var hand = SfJson.AsDouble(w["nenner"]);
            var sap = SfJson.AsDouble(w["nenner_sap"]);
            var nenner = hand ?? sap;
            if (nenner is not null) w["nenner_eff"] = nenner;
            w["nenner_quelle"] = hand is not null ? (sap is not null ? "hand_ueber_sap" : "hand") : sap is not null ? "sap" : null;
            var mv = Materialverfuegbarkeit(SfJson.AsDouble(w["c2_max"]), nenner);
            if (mv is not null) w["mv"] = mv;
        }
        var weeks = new JsonArray();
        foreach (var w in byKw.Values) weeks.Add(w);
        var arr = new JsonArray();
        foreach (var d in days) arr.Add(d);
        return new JsonObject
        {
            ["days"] = arr, ["weeks"] = weeks, ["target"] = Target,
            ["mv_ytd"] = MvYtd(byKw.Values, DateTime.Today.Year)
        };
    }

    /// <summary>
    /// Kennzahl Materialverfuegbarkeit Produktion (Marco Widmer / Fabio 24.11.2026): je KW der hoechste Tageswert „Code 2 im
    /// Horizont“ (Montag bis Samstag). Tage ohne Code-2-Zaehlung (z. B. Kennzahl aus dem Excel) zaehlen nicht.
    /// </summary>
    public static IEnumerable<(string Kw, int Max, string Day, int Tage)> WeekCounters(IEnumerable<JsonObject> days)
    {
        return days
            .Select(d => (Day: SfJson.PyStr(d["day"]), C2: d["kpi"] is JsonObject k ? SfJson.AsDouble(SfJson.Get(k, "code2")) : null))
            .Where(x => x.C2 is not null && x.Day.Length == 10)
            .Select(x => (x.Day, C2: (int)x.C2!.Value, Date: ParseDay(x.Day)))
            .Where(x => x.Date.DayOfWeek != DayOfWeek.Sunday)
            .GroupBy(x => KwKey(x.Date))
            .Select(g =>
            {
                var top = g.OrderByDescending(x => x.C2).ThenBy(x => x.Day, StringComparer.Ordinal).First();
                return (g.Key, top.C2, top.Day, g.Count());
            });
    }

    public static string KwKey(DateTime d)
        => $"{ISOWeek.GetYear(d)}-W{ISOWeek.GetWeekOfYear(d):00}";

    /// <summary>1 − Zaehler ÷ Nenner in Prozent, 2 Stellen; Nenner = verschiedene Materialien mit Bewegungsart 201/261 der KW.</summary>
    public static double? Materialverfuegbarkeit(double? zaehler, double? nenner)
        => zaehler is null || nenner is null || nenner <= 0 ? null : Math.Round((1 - zaehler.Value / nenner.Value) * 100, 2);

    /// <summary>Durchschnitt YTD der berechneten Wochenwerte (Quartalsabfrage Marco).</summary>
    public static JsonObject MvYtd(IEnumerable<JsonObject> weeks, int year)
    {
        var vals = weeks.Where(w => SfJson.PyStr(w["kw"]).StartsWith(year + "-W", StringComparison.Ordinal))
            .Select(w => SfJson.AsDouble(w["mv"])).Where(v => v is not null).Select(v => v!.Value).ToList();
        return new JsonObject
        {
            ["jahr"] = year,
            ["wochen"] = vals.Count,
            ["mv"] = vals.Count > 0 ? Math.Round(vals.Average(), 2) : null
        };
    }

    public static JsonObject SaveWeek(SqliteConnection con, string kw, JsonObject changes, string user)
    {
        var r = ShopfloorDb.QueryOne(con, "SELECT data FROM zd05_week WHERE kw=?", kw);
        var d = SfJson.ParseObject(r?.Str("data"));
        foreach (var kv in changes)
        {
            if (kv.Key is not ("mat_verf" or "off_best" or "nenner")) continue;
            if (!Present(kv.Value))
            {
                d.Remove(kv.Key);
                continue;
            }
            var s = SfJson.PyStr(kv.Value).Replace(",", ".").Replace("'", "").Replace("%", "");
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                throw new ShopfloorException($"Zahl ungültig: {s}");
            // gleich wie der SAP-Wert = kein Handwert (sonst stuende jeder bestaetigte SAP-Wert als „von Hand ueberschrieben“)
            if (kv.Key == "nenner" && SfJson.AsDouble(d["nenner_sap"]) is { } sapWert && Math.Abs(sapWert - num) < 0.0001)
            {
                d.Remove(kv.Key);
                continue;
            }
            d[kv.Key] = num;
        }
        ShopfloorDb.Exec(con, "INSERT OR REPLACE INTO zd05_week(kw,data,updated_at,updated_by) VALUES(?,?,?,?)",
            kw, SfJson.Dump(d), Now(), user);
        d["kw"] = kw;
        return d;
    }

    /// <summary>
    /// Nenner aus SAP (ShopMatBezugSet) fuer die KW schreiben (<c>nenner_sap</c>); ein Handwert (<c>nenner</c>) bleibt stehen. Ist der
    /// Handwert gleich dem SAP-Wert, faellt er weg (dann gilt er nicht als ueberschrieben). Liefert true, wenn sich etwas geaendert hat.
    /// </summary>
    public static bool SaveWeekSap(SqliteConnection con, string kw, int anzahl, string user = "SAP")
    {
        var r = ShopfloorDb.QueryOne(con, "SELECT data FROM zd05_week WHERE kw=?", kw);
        var d = SfJson.ParseObject(r?.Str("data"));
        var alt = SfJson.AsDouble(d["nenner_sap"]);
        var hand = SfJson.AsDouble(d["nenner"]);
        if (alt is { } a && Math.Abs(a - anzahl) < 0.0001 && !(hand is { } h0 && Math.Abs(h0 - anzahl) < 0.0001))
            return false;
        d["nenner_sap"] = anzahl;
        if (hand is { } h && Math.Abs(h - anzahl) < 0.0001)
            d.Remove("nenner");
        ShopfloorDb.Exec(con, "INSERT OR REPLACE INTO zd05_week(kw,data,updated_at,updated_by) VALUES(?,?,?,?)",
            kw, SfJson.Dump(d), Now(), user);
        return true;
    }

    /// <summary>KW-Schluessel, fuer die schon ein SAP-Nenner steht.</summary>
    public static HashSet<string> WeeksWithSapNenner(SqliteConnection con)
        => ShopfloorDb.Query(con, "SELECT kw, data FROM zd05_week")
            .Where(r => SfJson.AsDouble(SfJson.ParseObject(r.Str("data"))["nenner_sap"]) is not null)
            .Select(r => r.Str("kw") ?? string.Empty).ToHashSet(StringComparer.Ordinal);

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
