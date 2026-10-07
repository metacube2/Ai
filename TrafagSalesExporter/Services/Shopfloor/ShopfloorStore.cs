using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services.Shopfloor;

/// <summary>Eingang der Schnittstelle (SAP, MES, MCP-Agent). Items wie im Body von POST /api/integration/push.</summary>
public sealed class ShopfloorPush
{
    public JsonArray Items { get; init; } = [];
}

/// <summary>
/// Erfassung und Abfrage der Shopfloor-Daten (Port von server.py): Listen-Eintraege, Tageswerte, Verlauf,
/// Uebersicht, CSV und Schnittstelle. Die Zugriffsregel liegt in ShopfloorAccess/ShopfloorEndpoints, nicht hier.
/// </summary>
public sealed class ShopfloorStore
{
    private static readonly Regex DayRx = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
    private static readonly Regex DayPrefixRx = new(@"^\d{4}-\d{2}-\d{2}", RegexOptions.Compiled);

    public ShopfloorDb Db { get; }
    public ShopfloorConfig Config { get; }
    private readonly Func<string> _baseUrl;

    public ShopfloorStore(ShopfloorDb db, ShopfloorConfig config, Func<string>? baseUrl = null)
    {
        Db = db;
        Config = config;
        _baseUrl = baseUrl ?? (() => string.Empty);
    }

    public string BaseUrl => _baseUrl() ?? string.Empty;

    private IReadOnlyDictionary<string, string[]>? Prefixes
        => Config.ForecastPrefix.Count == 0 ? null : Config.ForecastPrefix.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());

    private ShopfloorModuleDef Module(string? key)
        => key is not null && Config.Modules.TryGetValue(key, out var m) ? m : throw new ShopfloorException("Modul unbekannt");

    // ------------------------------------------------------------ Hilfen
    public static bool IsValidDay(string? day) => day is not null && DayRx.IsMatch(day);

    private static JsonObject RecToJson(SfRow r) => new()
    {
        ["id"] = r.Long("id"), ["nr"] = r.Str("nr"),
        ["data"] = SfJson.ParseObject(r.Str("data")), ["meta"] = SfJson.ParseObject(r.Str("meta")),
        ["version"] = r.Long("version"), ["created_at"] = r.Str("created_at"), ["created_by"] = r.Str("created_by"),
        ["updated_at"] = r.Str("updated_at"), ["updated_by"] = r.Str("updated_by")
    };

    private static JsonObject Diff(JsonObject old, JsonObject @new)
    {
        var ch = new JsonObject();
        foreach (var kv in @new)
        {
            var o = SfJson.Get(old, kv.Key);
            if (!SfJson.Equal(o, kv.Value))
                ch[kv.Key] = SfJson.Pair(o, kv.Value);
        }
        return ch;
    }

    private static void ApplyMeta(JsonObject meta, IEnumerable<string> changedKeys, string source)
    {
        var ts = ShopfloorDb.Now();
        foreach (var k in changedKeys)
        {
            if (!string.IsNullOrEmpty(source) && source is not ("manuell" or "Excel-Import"))
                meta[k] = new JsonObject { ["src"] = source, ["ts"] = ts };
            else
                meta.Remove(k);   // manuell ueberschrieben -> Quellen-Hinweis entfernen
        }
    }

    private static void AddHistory(SqliteConnection con, string module, string refKey, string ts, string user, string source, string changesJson)
        => ShopfloorDb.Exec(con, "INSERT INTO history(module,ref,ts,user,source,changes) VALUES(?,?,?,?,?,?)",
            module, refKey, ts, user, source, changesJson);

    // ------------------------------------------------------------ Nummernkreise
    /// <summary>Naechste Nummer gemaess id_format, z. B. SFP-{yy}-{n:03} ergibt SFP-26-058.</summary>
    public static string NextNr(SqliteConnection con, ShopfloorModuleDef mod, DateTime? refDay = null)
    {
        var fmt = mod.IdFormat ?? throw new ShopfloorException("Modul ohne Nummernkreis");
        var yy = (refDay ?? DateTime.Today).ToString("yy", CultureInfo.InvariantCulture);
        var head = fmt.Replace("{yy}", yy);
        var prefix = head.Split("{n")[0];
        var width = int.Parse(Regex.Match(fmt, @"\{n:0(\d)\}").Groups[1].Value, CultureInfo.InvariantCulture);
        var n = 0L;
        foreach (var r in ShopfloorDb.Query(con, "SELECT nr FROM records WHERE module=? AND nr LIKE ?", mod.Key, prefix + "%"))
        {
            var nr = r.Str("nr") ?? string.Empty;
            var tail = nr.Length >= prefix.Length ? nr[prefix.Length..] : string.Empty;
            if (tail.Length >= width && tail.All(char.IsAsciiDigit) && long.TryParse(tail, out var v))
                n = Math.Max(n, v);
        }
        return prefix + (n + 1).ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
    }

    // ------------------------------------------------------------ Werte bereinigen
    /// <summary>Werte typgerecht ablegen (Zahlen mit Komma/Apostroph erlauben).</summary>
    public static JsonNode? CleanValue(ShopfloorFieldDef field, JsonNode? v)
    {
        if (v is null || (v is JsonValue jv && (jv.GetValueKind() == System.Text.Json.JsonValueKind.Null ||
                                                (jv.GetValueKind() == System.Text.Json.JsonValueKind.String && jv.GetValue<string>().Length == 0))))
            return null;
        if (field.Type == "num")
        {
            if (SfJson.IsNumber(v))
                return SfJson.Clone(v);
            var d = SfJson.ParseFlexibleNumber(v);
            if (d is null)
                return SfJson.PyStr(v);   // unveraendert behalten statt Daten zu verlieren
            return SfJson.Num(d.Value);
        }
        if (field.Type == "flags")
        {
            if (v is JsonArray a)
                return SfJson.Clone(a);
            var list = new JsonArray();
            foreach (var x in SfJson.PyStr(v).Split(',').Select(s => s.Trim()).Where(s => s.Length > 0))
                list.Add(x);
            return list;
        }
        return SfJson.Clone(v);
    }

    public JsonObject CleanChanges(string module, JsonObject? changes)
    {
        var fields = Module(module).Fields.ToDictionary(f => f.Key, f => f);
        var o = new JsonObject();
        foreach (var kv in changes ?? new JsonObject())
            if (fields.TryGetValue(kv.Key, out var f) && f.Type != "calc")
                o[kv.Key] = CleanValue(f, kv.Value);
        return o;
    }

    // ------------------------------------------------------------ Speichern
    public SfRow SaveRecord(SqliteConnection con, string module, string? nr, JsonObject changes, string user, string source = "manuell", long? recId = null)
    {
        var mod = Module(module);
        SfRow? row;
        if (recId is null && !string.IsNullOrEmpty(nr))
            row = ShopfloorDb.QueryOne(con, "SELECT * FROM records WHERE module=? AND nr=?", module, nr);
        else if (recId is not null)
            row = ShopfloorDb.QueryOne(con, "SELECT * FROM records WHERE id=?", recId.Value);
        else
            row = null;
        var ts = ShopfloorDb.Now();
        if (row is null)
        {
            if (string.IsNullOrEmpty(nr))
            {
                DateTime? refDay = null;
                var df = mod.DateField;
                if (df is not null && SfJson.Truthy(SfJson.Get(changes, df)))
                {
                    var s = SfJson.PyStr(SfJson.Get(changes, df));
                    if (DateTime.TryParseExact(s.Length > 10 ? s[..10] : s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var rd))
                        refDay = rd;
                }
                nr = NextNr(con, mod, refDay);
            }
            var meta = new JsonObject();
            ApplyMeta(meta, changes.Select(kv => kv.Key), source);
            var id = ShopfloorDb.Insert(con,
                "INSERT INTO records(module,nr,data,meta,created_at,created_by,updated_at,updated_by) VALUES(?,?,?,?,?,?,?,?)",
                module, nr, SfJson.Dump(changes), SfJson.Dump(meta), ts, user, ts, user);
            var hist = new JsonObject();
            foreach (var kv in changes)
                hist[kv.Key] = SfJson.Pair(null, kv.Value);
            AddHistory(con, module, nr!, ts, user, source, SfJson.Dump(hist));
            return ShopfloorDb.QueryOne(con, "SELECT * FROM records WHERE id=?", id)!;
        }

        var data = SfJson.ParseObject(row.Str("data"));
        var meta2 = SfJson.ParseObject(row.Str("meta"));
        var ch = Diff(data, changes);
        if (ch.Count == 0)
            return row;
        foreach (var kv in changes)
            data[kv.Key] = SfJson.Clone(kv.Value);
        ApplyMeta(meta2, ch.Select(kv => kv.Key).ToList(), source);
        ShopfloorDb.Exec(con, "UPDATE records SET data=?, meta=?, version=version+1, updated_at=?, updated_by=?, deleted=0 WHERE id=?",
            SfJson.Dump(data), SfJson.Dump(meta2), ts, user, row.Long("id"));
        AddHistory(con, module, row.Str("nr") ?? string.Empty, ts, user, source, SfJson.Dump(ch));
        return ShopfloorDb.QueryOne(con, "SELECT * FROM records WHERE id=?", row.Long("id"))!;
    }

    public JsonObject SaveDaily(SqliteConnection con, string module, string day, JsonObject changes, string user, string source = "manuell")
    {
        var row = ShopfloorDb.QueryOne(con, "SELECT * FROM daily WHERE module=? AND day=?", module, day);
        var ts = ShopfloorDb.Now();
        var data = row is null ? new JsonObject() : SfJson.ParseObject(row.Str("data"));
        var meta = row is null ? new JsonObject() : SfJson.ParseObject(row.Str("meta"));
        var ch = Diff(data, changes);
        if (ch.Count == 0)
            return new JsonObject { ["day"] = day, ["data"] = data, ["meta"] = meta };
        foreach (var kv in changes)
            data[kv.Key] = SfJson.Clone(kv.Value);
        foreach (var k in data.Select(kv => kv.Key).ToList())
        {
            var v = data[k];
            if (v is null || (v is JsonValue jv && (jv.GetValueKind() == System.Text.Json.JsonValueKind.Null ||
                                                    (jv.GetValueKind() == System.Text.Json.JsonValueKind.String && jv.GetValue<string>().Length == 0))))
                data.Remove(k);
        }
        ApplyMeta(meta, ch.Select(kv => kv.Key).ToList(), source);
        if (row is not null)
            ShopfloorDb.Exec(con, "UPDATE daily SET data=?, meta=?, version=version+1, updated_at=?, updated_by=? WHERE module=? AND day=?",
                SfJson.Dump(data), SfJson.Dump(meta), ts, user, module, day);
        else
            ShopfloorDb.Exec(con, "INSERT INTO daily(module,day,data,meta,updated_at,updated_by) VALUES(?,?,?,?,?,?)",
                module, day, SfJson.Dump(data), SfJson.Dump(meta), ts, user);
        AddHistory(con, module, day, ts, user, source, SfJson.Dump(ch));
        return new JsonObject { ["day"] = day, ["data"] = data, ["meta"] = meta, ["updated_at"] = ts, ["updated_by"] = user };
    }

    // ------------------------------------------------------------ Endpunkt-Logik (Lesen)
    public JsonArray GetRecords(string? module)
    {
        Module(module);
        using var con = Db.Open();
        var arr = new JsonArray();
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM records WHERE module=? AND deleted=0 ORDER BY nr DESC", module))
            arr.Add(RecToJson(r));
        return arr;
    }

    public JsonObject GetDaily(string? modules, string? from, string? to)
    {
        var mods = (modules ?? string.Empty).Split(',').Where(m => Config.Modules.ContainsKey(m)).ToList();
        var frm = string.IsNullOrEmpty(from) ? "2000-01-01" : from;
        var upTo = string.IsNullOrEmpty(to) ? "2999-12-31" : to;
        using var con = Db.Open();
        var o = new JsonObject();
        foreach (var m in mods)
        {
            var days = new JsonObject();
            foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM daily WHERE module=? AND day BETWEEN ? AND ? ORDER BY day", m, frm, upTo))
                days[r.Str("day") ?? string.Empty] = new JsonObject
                {
                    ["data"] = SfJson.ParseObject(r.Str("data")), ["meta"] = SfJson.ParseObject(r.Str("meta")),
                    ["updated_by"] = r.Str("updated_by"), ["updated_at"] = r.Str("updated_at")
                };
            o[m] = days;
        }
        return o;
    }

    public JsonArray GetHistory(string? module, string? refKey)
    {
        using var con = Db.Open();
        var arr = new JsonArray();
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM history WHERE module=? AND ref=? ORDER BY id DESC LIMIT 200", module, refKey))
            arr.Add(new JsonObject
            {
                ["ts"] = r.Str("ts"), ["user"] = r.Str("user"), ["source"] = r.Str("source"),
                ["changes"] = JsonNode.Parse(r.Str("changes") ?? "{}")
            });
        return arr;
    }

    /// <summary>Startseite: Zahlen pro Modul fuer den Tag.</summary>
    public JsonObject AgendaStatus(string day)
    {
        using var con = Db.Open();
        var res = new JsonObject();
        foreach (var m in Config.ModuleList)
        {
            if (m.Kind == "list")
            {
                var sf = m.StatusField;
                var rows = ShopfloorDb.Query(con, "SELECT data FROM records WHERE module=? AND deleted=0", m.Key);
                long open = 0, newToday = 0, overdue = 0;
                foreach (var r in rows)
                {
                    var d = SfJson.ParseObject(r.Str("data"));
                    if (!string.IsNullOrEmpty(sf) && !Config.IsClosed(SfJson.Str(SfJson.Get(d, sf)), m))
                    {
                        open++;
                        var t = SfJson.Get(d, m.DueField ?? "-");
                        if (t is JsonValue tv && tv.GetValueKind() == System.Text.Json.JsonValueKind.String)
                        {
                            var ts = tv.GetValue<string>();
                            if (DayPrefixRx.IsMatch(ts) && string.CompareOrdinal(ts[..10], day) < 0)
                                overdue++;
                        }
                    }
                    var dv = SfJson.Str(SfJson.Get(d, m.DateField ?? "datum"));
                    if ((dv.Length > 10 ? dv[..10] : dv) == day)
                        newToday++;
                }
                res[m.Key] = new JsonObject
                {
                    ["total"] = rows.Count, ["open"] = string.IsNullOrEmpty(sf) ? null : open, ["new_today"] = newToday, ["overdue"] = overdue
                };
            }
            else if (m.Kind == "daily")
            {
                var r = ShopfloorDb.QueryOne(con, "SELECT data, updated_by, updated_at FROM daily WHERE module=? AND day=?", m.Key, day);
                var inputs = m.Fields.Count(f => f.Type != "calc");
                var filled = r is null ? 0 : SfJson.ParseObject(r.Str("data")).Count;
                res[m.Key] = new JsonObject { ["filled"] = filled, ["fields"] = inputs, ["by"] = r?.Str("updated_by"), ["at"] = r?.Str("updated_at") };
            }
        }
        res["zd05"] = ShopfloorZd05.StatusToday(con, day);
        return res;
    }

    public static string FmtCsv(JsonNode? v)
    {
        if (v is null) return string.Empty;
        if (v is JsonArray a) return string.Join(", ", a.Select(x => SfJson.PyStr(x)));
        if (v is JsonValue jv)
        {
            var kind = jv.GetValueKind();
            if (kind == System.Text.Json.JsonValueKind.Null) return string.Empty;
            if (kind == System.Text.Json.JsonValueKind.Number)
                return jv.ToJsonString().Replace('.', ',');
            if (kind == System.Text.Json.JsonValueKind.String)
            {
                var s = jv.GetValue<string>();
                return DayRx.IsMatch(s) ? $"{s[8..10]}.{s[5..7]}.{s[0..4]}" : s;
            }
        }
        return SfJson.PyStr(v);
    }

    private static string CsvCell(string s)
        => s.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    /// <summary>CSV mit Semikolon und BOM wie in server.py (Excel-tauglich).</summary>
    public byte[] ExportCsv(string? module)
    {
        var mod = Module(module);
        var fields = mod.Fields.Where(f => f.Type != "calc").ToList();
        var sb = new StringBuilder("\uFEFF");
        void Line(IEnumerable<string> cells) => sb.Append(string.Join(';', cells.Select(CsvCell))).Append("\r\n");
        using var con = Db.Open();
        if (mod.Kind == "list")
        {
            Line(new[] { "Nr." }.Concat(fields.Select(f => f.Label)).Concat(["Geändert am", "Geändert von"]));
            foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM records WHERE module=? AND deleted=0 ORDER BY nr", mod.Key))
            {
                var d = SfJson.ParseObject(r.Str("data"));
                Line(new[] { r.Str("nr") ?? string.Empty }.Concat(fields.Select(f => FmtCsv(SfJson.Get(d, f.Key)))).Concat([r.Str("updated_at") ?? string.Empty, r.Str("updated_by") ?? string.Empty]));
            }
        }
        else
        {
            Line(new[] { "Datum" }.Concat(fields.Select(f => f.Label)).Concat(["Geändert am", "Geändert von"]));
            foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM daily WHERE module=? ORDER BY day", mod.Key))
            {
                var d = SfJson.ParseObject(r.Str("data"));
                Line(new[] { r.Str("day") ?? string.Empty }.Concat(fields.Select(f => FmtCsv(SfJson.Get(d, f.Key)))).Concat([r.Str("updated_at") ?? string.Empty, r.Str("updated_by") ?? string.Empty]));
            }
        }
        return new UTF8Encoding(false).GetBytes(sb.ToString());
    }

    /// <summary>Maschinenlesbare Feldliste fuer SAP-/MES-Connectoren.</summary>
    public JsonArray IntegrationSchema()
    {
        var arr = new JsonArray();
        foreach (var m in Config.ModuleList.Where(m => m.Kind is "list" or "daily"))
        {
            var fields = new JsonArray();
            foreach (var f in m.Fields.Where(f => f.Type != "calc"))
                fields.Add(new JsonObject { ["key"] = f.Key, ["label"] = f.Label, ["type"] = f.Type, ["planned_source"] = f.Src, ["unit"] = f.Unit });
            arr.Add(new JsonObject { ["module"] = m.Key, ["title"] = m.Title, ["kind"] = m.Kind, ["fields"] = fields });
        }
        return arr;
    }

    // ------------------------------------------------------------ Endpunkt-Logik (Schreiben)
    public JsonObject PostRecord(JsonObject body, string user)
    {
        var module = SfJson.GetString(body, "module");
        if (module is null || !Config.Modules.TryGetValue(module, out var mod) || mod.Kind != "list")
            throw new ShopfloorException("Modul unbekannt");
        var changes = CleanChanges(module, SfJson.Get(body, "changes") as JsonObject);
        long? id = SfJson.Truthy(SfJson.Get(body, "id")) ? (long?)SfJson.AsDouble(SfJson.Get(body, "id")) : null;
        var want = !body.ContainsKey("notify") || SfJson.Truthy(body["notify"]);
        return Db.Write(con =>
        {
            var row = SaveRecord(con, module, null, changes, user, "manuell", id);
            var res = RecToJson(row);
            res["mails"] = ShopfloorMail.OnRecordSaved(con, Config, mod, res, changes, user, id is null, BaseUrl, want);
            return res;
        });
    }

    public JsonObject PostDaily(JsonObject body, string user)
    {
        var module = SfJson.GetString(body, "module");
        var day = SfJson.GetString(body, "day");
        if (module is null || !Config.Modules.TryGetValue(module, out var mod) || mod.Kind != "daily")
            throw new ShopfloorException("Modul unbekannt");
        if (!IsValidDay(day))
            throw new ShopfloorException("Datum ungültig");
        var changes = CleanChanges(module, SfJson.Get(body, "changes") as JsonObject);
        return Db.Write(con => SaveDaily(con, module, day!, changes, user));
    }

    public void DeleteRecord(long id, string user)
        => Db.Write(con =>
        {
            var r = ShopfloorDb.QueryOne(con, "SELECT module, nr FROM records WHERE id=?", id);
            if (r is not null)
            {
                var ts = ShopfloorDb.Now();
                ShopfloorDb.Exec(con, "UPDATE records SET deleted=1, updated_at=?, updated_by=? WHERE id=?", ts, user, id);
                AddHistory(con, r.Str("module") ?? string.Empty, r.Str("nr") ?? string.Empty, ts, user, "manuell", "{\"_geloescht\":[false,true]}");
            }
            return true;
        });

    private static string Req(JsonObject b, string key)
        => SfJson.GetString(b, key) ?? throw new ShopfloorException($"Feld {key} fehlt");

    private static JsonObject Changes(JsonObject b) => SfJson.Get(b, "changes") as JsonObject ?? new JsonObject();

    private static JsonArray Rows(JsonObject b) => SfJson.Get(b, "rows") as JsonArray ?? [];

    public JsonNode PostZd05(string action, JsonObject b, string user)
        => Db.Write<JsonNode>(con => action switch
        {
            "import" => IsValidDay(SfJson.GetString(b, "day"))
                ? ShopfloorZd05.ImportRows(con, Req(b, "day"), Rows(b), user, "Upload", SfJson.GetString(b, "filename") ?? string.Empty)
                : throw new ShopfloorException("Datum ungültig"),
            "row" => ShopfloorZd05.SaveRow(con, Req(b, "day"), Req(b, "material"), Changes(b), user),
            "week" => ShopfloorZd05.SaveWeek(con, Req(b, "kw"), Changes(b), user),
            "abc" => new JsonObject { ["anzahl"] = ShopfloorZd05.ReplaceAbc(con, Rows(b)) },
            _ => throw new ShopfloorException("unbekannt", 404)
        });

    public JsonNode PostForecast(string action, JsonObject b, string user)
        => Db.Write<JsonNode>(con => action switch
        {
            "import" => ShopfloorForecast.ImportOrders(con, Rows(b), user, "Upload", SfJson.GetString(b, "filename") ?? string.Empty, Prefixes),
            "plan" => ShopfloorForecast.SavePlan(con, Req(b, "dept"), Req(b, "day"), Changes(b), user),
            "settings" => ShopfloorForecast.SaveSettings(con, Req(b, "dept"), Changes(b), user),
            _ => throw new ShopfloorException("unbekannt", 404)
        });

    public JsonNode GetZd05(string? day)
    {
        using var con = Db.Open();
        return ShopfloorZd05.GetDay(con, day);
    }

    public JsonNode GetZd05History()
    {
        using var con = Db.Open();
        return ShopfloorZd05.History(con);
    }

    public JsonNode GetForecast(string? monday)
    {
        using var con = Db.Open();
        return ShopfloorForecast.GetWeek(con, monday, Prefixes);
    }

    // ------------------------------------------------------------ Mail
    public JsonNode GetPeople()
    {
        using var con = Db.Open();
        return ShopfloorMail.PeopleJson(con);
    }

    public JsonNode GetMail()
    {
        using var con = Db.Open();
        return new JsonObject { ["status"] = ShopfloorMail.Status(BaseUrl), ["outbox"] = ShopfloorMail.Outbox(con) };
    }

    public string? MailPreviewHtml(string? id)
    {
        if (!long.TryParse(id, out var n)) return null;
        using var con = Db.Open();
        return ShopfloorDb.QueryOne(con, "SELECT html FROM mail_outbox WHERE id=?", n)?.Str("html");
    }

    private static long ReqId(JsonObject b)
        => (long)(SfJson.AsDouble(SfJson.Get(b, "id")) ?? throw new ShopfloorException("Feld id fehlt"));

    public JsonNode PostMail(string action, JsonObject b, string user)
        => Db.Write<JsonNode>(con =>
        {
            switch (action)
            {
                case "people":
                    return ShopfloorMail.SavePeople(con, Rows(b));
                case "test":
                    var to = new JsonArray();
                    to.Add(new JsonObject { ["name"] = "Test", ["email"] = SfJson.Str(SfJson.Get(b, "to")) });
                    ShopfloorDb.Exec(con, "INSERT INTO mail_outbox(ts,user,module,ref,rcpt,subject,html,text,status,reason) VALUES(?,?,?,?,?,?,?,?,?,?)",
                        ShopfloorDb.Now(), user, "test", "-", SfJson.Dump(to), "[Shop-Floor] Testmail",
                        "<p>Der Mailversand aus dem PPA Shop-Floor funktioniert.</p>", "Der Mailversand funktioniert.", "wartend", "test");
                    return new JsonObject { ["ok"] = true };
                case "send":
                    var row = ShopfloorDb.QueryOne(con, "SELECT * FROM records WHERE id=?", ReqId(b))
                              ?? throw new ShopfloorException("Eintrag nicht gefunden");
                    var mod = Module(row.Str("module"));
                    var rec = RecToJson(row);
                    var people = ShopfloorMail.People(con);
                    var pl = new List<ShopfloorPerson>();
                    if (Config.NotifyRules.TryGetValue(mod.Key, out var rule))
                        foreach (var f in rule.Notify)
                            foreach (var p in ShopfloorMail.Resolve(SfJson.Str(SfJson.Get((JsonObject)rec["data"]!, f)), people).Hits)
                                if (!pl.Any(x => x.Id == p.Id)) pl.Add(p);
                    if (pl.Count == 0)
                        throw new ShopfloorException("Keine Zuständigen im Verzeichnis gefunden");
                    return ShopfloorMail.Enqueue(con, Config, mod, rec, pl, "manuell", user, BaseUrl);
                case "retry":
                    ShopfloorDb.Exec(con, "UPDATE mail_outbox SET status='wartend', tries=0 WHERE id=?", ReqId(b));
                    return new JsonObject { ["ok"] = true };
                default:
                    throw new ShopfloorException("unbekannt", 404);
            }
        });

    // ------------------------------------------------------------ Schnittstelle
    /// <summary>
    /// Schnittstelle fuer SAP, MES und MCP-Agenten ohne HTTP (Port von integration_push in server.py).
    /// Wird vom Endpunkt POST /shopfloor/api/integration/push und spaeter von den OData-Sets ShopZd05Set/ShopAufSet genutzt.
    /// Schreibt als Benutzer "{SOURCE}-Schnittstelle". Liefert pro Item ein Ergebnisobjekt (ok/error).
    /// </summary>
    public JsonArray ApplyPush(ShopfloorPush push, string source)
    {
        source = (source ?? "extern").ToUpperInvariant();
        if (source.Length > 20) source = source[..20];
        var src = source;
        var user = $"{src}-Schnittstelle";
        return Db.Write(con =>
        {
            var results = new JsonArray();
            foreach (var node in push.Items)
            {
                if (node is not JsonObject it) continue;
                var module = SfJson.GetString(it, "module");
                if (module == "planauftraege")
                {
                    var r = ShopfloorForecast.ImportOrders(con, Rows(it), user, src, string.Empty, Prefixes);
                    var res = new JsonObject { ["ok"] = true, ["module"] = module };
                    foreach (var kv in r) res[kv.Key] = SfJson.Clone(kv.Value);
                    results.Add(res);
                    continue;
                }
                if (module == "zd05")
                {
                    var day = SfJson.Truthy(SfJson.Get(it, "day")) ? SfJson.PyStr(SfJson.Get(it, "day")) : Today();
                    var r = ShopfloorZd05.ImportRows(con, day, Rows(it), user, src);
                    results.Add(new JsonObject { ["ok"] = true, ["module"] = "zd05", ["day"] = r["day"]!.DeepClone(), ["positionen"] = r["positionen"]!.DeepClone() });
                    continue;
                }
                if (module is null || !Config.Modules.TryGetValue(module, out var mod))
                {
                    results.Add(new JsonObject { ["ok"] = false, ["error"] = $"Modul {module ?? "None"} unbekannt" });
                    continue;
                }
                var ch = CleanChanges(module, SfJson.Get(it, "fields") as JsonObject);
                var fieldNames = new JsonArray();
                foreach (var kv in ch) fieldNames.Add(kv.Key);
                if (mod.Kind == "daily")
                {
                    var day = SfJson.Truthy(SfJson.Get(it, "day")) ? SfJson.PyStr(SfJson.Get(it, "day")) : Today();
                    SaveDaily(con, module, day, ch, user, src);
                    results.Add(new JsonObject { ["ok"] = true, ["module"] = module, ["day"] = day, ["fields"] = fieldNames });
                    continue;
                }
                // Liste: "match" findet einen bestehenden Eintrag, mit "create": true wird sonst ein neuer angelegt
                var match = SfJson.Get(it, "match") as JsonObject;
                long? recId = null;
                if (match is { Count: > 0 })
                {
                    foreach (var r in ShopfloorDb.Query(con, "SELECT id, data FROM records WHERE module=? AND deleted=0", module))
                    {
                        var d = SfJson.ParseObject(r.Str("data"));
                        if (match.All(kv => SfJson.PyStr(SfJson.Get(d, kv.Key)) == SfJson.PyStr(kv.Value)))
                        {
                            recId = r.Long("id");
                            break;
                        }
                    }
                    if (recId is null && !SfJson.Truthy(SfJson.Get(it, "create")))
                    {
                        results.Add(new JsonObject { ["ok"] = false, ["error"] = "kein passender Eintrag", ["match"] = match.DeepClone() });
                        continue;
                    }
                }
                var row = SaveRecord(con, module, SfJson.GetString(it, "nr"), ch, user, src, recId);
                results.Add(new JsonObject { ["ok"] = true, ["module"] = module, ["nr"] = row.Str("nr"), ["fields"] = fieldNames });
            }
            return results;
        });
    }

    private static string Today() => DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
