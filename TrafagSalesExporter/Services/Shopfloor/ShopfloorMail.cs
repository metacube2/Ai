using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace TrafagSalesExporter.Services.Shopfloor;

public sealed class ShopfloorPerson
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Kuerzel { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Typ { get; init; } = string.Empty;
    public List<string> Aliases { get; init; } = [];
    public long Aktiv { get; init; } = 1;
    public long Digest { get; init; }

    public JsonObject ToJson()
    {
        var al = new JsonArray();
        foreach (var a in Aliases) al.Add(a);
        return new JsonObject
        {
            ["id"] = Id, ["name"] = Name, ["kuerzel"] = Kuerzel, ["email"] = Email, ["typ"] = Typ,
            ["aliases"] = al, ["aktiv"] = Aktiv, ["digest"] = Digest
        };
    }
}

/// <summary>
/// Zustaendigkeits-Mails (Port von notify.py): Personenverzeichnis, Empfaenger-Aufloesung, Postausgang und
/// Mailtext. Versand ueber Graph oder SMTP ist bewusst NICHT umgesetzt (Modus bleibt "aus"). Die Mails stehen
/// im Postausgang und lassen sich mit "In Outlook oeffnen" (mailto) versenden. TODO: Graph/SMTP, siehe docs/SHOPFLOOR_2026-10-07.md.
/// </summary>
public static class ShopfloorMail
{
    private static readonly (string Name, string Typ, string[] Aliases)[] Seed =
    [
        ("Unterhalt", "Gruppe", ["UH", "Instandhaltung"]), ("Gebäudeunterhalt", "Gruppe", []), ("PT", "Gruppe", ["Prozesstechnik", "TPT"]),
        ("Software", "Gruppe", ["SW"]), ("IT", "Gruppe", ["itc", "TSW"]), ("Anlagenbau", "Gruppe", []),
        ("PPD", "Gruppe", ["PPD (CH)", "Dispo", "Produktionsplanung"]), ("Einkauf", "Gruppe", ["Einkauf (CH)"]), ("Logistik", "Gruppe", ["Logistik (CH)"]),
        ("CZ", "Gruppe", ["Trafag CZ"]), ("TX", "Abteilung", ["TX (CH)", "TX-Linie", "EL*"]), ("TR5", "Abteilung", ["TR5 (CH)", "EK*"]),
        ("DW", "Abteilung", ["D1*", "D2*", "D3*", "DS*", "DEF"]), ("SEH", "Abteilung", ["SEH (CH)", "012", "SL*"]), ("MTF", "Abteilung", []), ("TL1", "Abteilung", [])
    ];

    private static string Now() => ShopfloorDb.Now();

    public static void SeedPeopleIfEmpty(SqliteConnection con)
    {
        if (ShopfloorDb.QueryOne(con, "SELECT 1 FROM people LIMIT 1") is not null)
            return;
        foreach (var (name, typ, al) in Seed)
            ShopfloorDb.Exec(con, "INSERT INTO people(name,kuerzel,email,typ,aliases) VALUES(?,?,?,?,?)",
                name, string.Empty, string.Empty, typ, SfJson.Dump(ToArray(al)));
    }

    private static JsonArray ToArray(IEnumerable<string> items)
    {
        var a = new JsonArray();
        foreach (var i in items) a.Add(i);
        return a;
    }

    // ---------------------------------------------------------------- Verzeichnis
    public static List<ShopfloorPerson> People(SqliteConnection con)
        => ShopfloorDb.Query(con, "SELECT * FROM people ORDER BY typ DESC, name").Select(r => new ShopfloorPerson
        {
            Id = r.Long("id"),
            Name = r.Str("name") ?? string.Empty,
            Kuerzel = r.Str("kuerzel") ?? string.Empty,
            Email = r.Str("email") ?? string.Empty,
            Typ = r.Str("typ") ?? string.Empty,
            Aliases = SfJson.ParseArray(r.Str("aliases")).Select(a => SfJson.PyStr(a)).ToList(),
            Aktiv = r.Long("aktiv"),
            Digest = r.Long("digest")
        }).ToList();

    public static JsonArray PeopleJson(SqliteConnection con)
    {
        var arr = new JsonArray();
        foreach (var p in People(con)) arr.Add(p.ToJson());
        return arr;
    }

    public static JsonArray SavePeople(SqliteConnection con, IEnumerable<JsonNode?> rows)
    {
        ShopfloorDb.Exec(con, "DELETE FROM people");
        foreach (var node in rows)
        {
            if (node is not JsonObject r) continue;
            if (!SfJson.Truthy(SfJson.Get(r, "name")) && !SfJson.Truthy(SfJson.Get(r, "kuerzel")))
                continue;
            var aliases = new List<string>();
            var al = SfJson.Get(r, "aliases");
            if (al is JsonArray arr)
                aliases.AddRange(arr.Select(a => SfJson.PyStr(a)));
            else if (SfJson.Truthy(al))
                aliases.AddRange(Regex.Split(SfJson.PyStr(al), "[,;]").Select(a => a.Trim()).Where(a => a.Length > 0));
            var aktiv = r.ContainsKey("aktiv") ? SfJson.Truthy(r["aktiv"]) : true;
            ShopfloorDb.Exec(con, "INSERT INTO people(name,kuerzel,email,typ,aliases,aktiv,digest) VALUES(?,?,?,?,?,?,?)",
                SfJson.Str(SfJson.Get(r, "name")).Trim(), SfJson.Str(SfJson.Get(r, "kuerzel")).Trim(), SfJson.Str(SfJson.Get(r, "email")).Trim(),
                SfJson.Truthy(SfJson.Get(r, "typ")) ? SfJson.PyStr(SfJson.Get(r, "typ")) : "Person",
                SfJson.Dump(ToArray(aliases)), aktiv ? 1 : 0, SfJson.Truthy(SfJson.Get(r, "digest")) ? 1 : 0);
        }
        return PeopleJson(con);
    }

    public static List<string> Tokens(string? value)
        => Regex.Split(value ?? string.Empty, @"[,;/+&\n()]| und ").Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

    /// <summary>"Unterhalt/PT" ergibt [Unterhalt, PT]. Treffer ueber Kuerzel, Name, Vorname oder Alias (ohne Gross/Klein).</summary>
    public static (List<ShopfloorPerson> Hits, List<string> Missing) Resolve(string? value, List<ShopfloorPerson> people)
    {
        var hits = new List<ShopfloorPerson>();
        var missing = new List<string>();
        foreach (var t in Tokens(value))
        {
            var tl = t.ToLowerInvariant();
            ShopfloorPerson? hit = null;
            foreach (var p in people)
            {
                if (p.Aktiv == 0) continue;
                var keys = new[] { p.Name, p.Kuerzel }.Concat(p.Aliases).Where(k => !string.IsNullOrEmpty(k)).Select(k => k.ToLowerInvariant());
                var first = p.Name.Split(' ')[0].ToLowerInvariant();
                if (keys.Contains(tl) || (p.Typ == "Person" && tl == first))
                {
                    hit = p;
                    break;
                }
            }
            if (hit is null)   // Alias mit * = Anfang, z. B. "EL*" fuer Disponenten EL1 bis EL8
                foreach (var p in people)
                    if (p.Aktiv != 0 && p.Aliases.Any(a => a.EndsWith('*') && tl.StartsWith(a[..^1].ToLowerInvariant(), StringComparison.Ordinal)))
                    {
                        hit = p;
                        break;
                    }
            if (hit is not null)
            {
                if (!hits.Any(h => h.Id == hit.Id)) hits.Add(hit);
            }
            else missing.Add(t);
        }
        return (hits, missing);
    }

    // ---------------------------------------------------------------- Mail erzeugen
    private static string Fmt(JsonNode? v)
    {
        if (v is JsonArray a) return string.Join(", ", a.Select(x => SfJson.PyStr(x)));
        if (v is null) return string.Empty;
        var s = SfJson.PyStr(v);
        if (v is JsonValue jv && jv.GetValueKind() == System.Text.Json.JsonValueKind.String && Regex.IsMatch(s, @"^\d{4}-\d{2}-\d{2}$"))
            return $"{s[8..10]}.{s[5..7]}.{s[0..4]}";
        return s == "None" ? string.Empty : s;
    }

    private static string Esc(string s) => WebUtility.HtmlEncode(s);

    public static (string Subject, string Html, string Text) Build(ShopfloorModuleDef mod, ShopfloorNotifyRule? rule, JsonObject rec,
        string reason, string baseUrl, string user)
    {
        var d = rec["data"] as JsonObject ?? new JsonObject();
        var nr = SfJson.PyStr(rec["nr"]);
        var title = rule is null ? string.Empty : Fmt(SfJson.Get(d, rule.Title));
        var shortTitle = title.Length > 70 ? title[..70] + "…" : title;
        var lead = reason switch
        {
            "neu" => "Ihnen wurde eine neue Aufgabe zugewiesen",
            "zust" => "Die Zuständigkeit wurde Ihnen übertragen",
            "done" => "Ihr Eintrag wurde abgeschlossen",
            _ => "Bitte beachten Sie folgenden Eintrag"
        };
        var subj = reason == "done" ? "Erledigt" : "Zuständigkeit";
        var subject = shortTitle.Length > 0 ? $"[Shop-Floor] {subj}: {nr} – {shortTitle}" : $"[Shop-Floor] {subj}: {nr}";
        var link = baseUrl.Length > 0 ? $"{baseUrl.TrimEnd('/')}/#/m/{mod.Key}/{Uri.EscapeDataString(nr)}" : string.Empty;
        var rows = mod.Fields.Where(f => f.Type != "calc")
            .Select(f => (f.Label, Value: Fmt(SfJson.Get(d, f.Key))))
            .Where(x => x.Value.Length > 0).ToList();

        var html = new StringBuilder();
        html.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#22262A\">");
        html.Append("<div style=\"border-left:4px solid #E8531A;padding:2px 0 2px 12px;margin-bottom:14px\">");
        html.Append($"<div style=\"font-size:12px;color:#7D868F\">PPA Shop-Floor · {Esc(mod.Title)}</div>");
        html.Append($"<div style=\"font-size:18px;font-weight:600\">{Esc(nr)}{(shortTitle.Length > 0 ? " – " + Esc(shortTitle) : "")}</div></div>");
        html.Append($"<p>{lead} (geändert von {Esc(user)}).</p>");
        html.Append("<table style=\"border-collapse:collapse;font-size:13px\">");
        foreach (var (label, value) in rows)
            html.Append($"<tr><td style=\"padding:3px 14px 3px 0;color:#7D868F;vertical-align:top\">{Esc(label)}</td><td style=\"padding:3px 0\">{Esc(value).Replace("\n", "<br>")}</td></tr>");
        html.Append("</table>");
        if (link.Length > 0)
            html.Append($"<p style=\"margin-top:18px\"><a href=\"{Esc(link)}\" style=\"background:#E8531A;color:#fff;padding:8px 14px;border-radius:6px;text-decoration:none\">Im Shop-Floor öffnen</a></p>");
        html.Append("<p style=\"font-size:12px;color:#7D868F;margin-top:20px\">Automatische Nachricht aus dem PPA Shop-Floor. Bitte nicht direkt antworten.</p></div>");

        var text = $"{lead} (geändert von {user}).\n\n{nr} – {title}\n\n" +
                   string.Join("\n", rows.Select(x => $"{x.Label}: {x.Value}")) +
                   (link.Length > 0 ? $"\n\nÖffnen: {link}" : string.Empty);
        return (subject, html.ToString(), text);
    }

    private static bool ClosedForMail(string status, ShopfloorModuleDef mod)
    {
        var st = status.ToLowerInvariant();
        var list = mod.Closed.Count > 0 ? mod.Closed : ShopfloorConfig.DefaultClosed.ToList();
        return st.StartsWith("erl", StringComparison.Ordinal) || list.Contains(st);
    }

    /// <summary>Prueft nach dem Speichern, wer informiert werden muss, und legt Mails in den Postausgang.</summary>
    public static JsonArray OnRecordSaved(SqliteConnection con, ShopfloorConfig cfg, ShopfloorModuleDef mod, JsonObject rec, JsonObject changes,
        string user, bool isNew, string baseUrl, bool want = true)
    {
        var queued = new JsonArray();
        if (!cfg.NotifyRules.TryGetValue(mod.Key, out var rule) || !want)
            return queued;
        var people = People(con);
        var d = rec["data"] as JsonObject ?? new JsonObject();
        var targets = new Dictionary<string, (ShopfloorPerson P, string Reason)>(StringComparer.Ordinal);
        var order = new List<string>();
        void Add(ShopfloorPerson p, string reason)
        {
            var key = p.Email.Length > 0 ? p.Email : p.Name;
            if (targets.TryAdd(key, (p, reason)))
                order.Add(key);
        }
        foreach (var f in rule.Notify)
            if ((isNew && SfJson.Truthy(SfJson.Get(d, f))) || (!isNew && changes.ContainsKey(f) && SfJson.Truthy(SfJson.Get(d, f))))
                foreach (var p in Resolve(SfJson.Str(SfJson.Get(d, f)), people).Hits)
                    Add(p, isNew ? "neu" : "zust");
        var sf = mod.StatusField;
        if (!string.IsNullOrEmpty(sf) && changes.ContainsKey(sf) && !isNew && ClosedForMail(SfJson.Str(SfJson.Get(d, sf)), mod))
            foreach (var f in rule.NotifyDone)
                foreach (var p in Resolve(SfJson.Str(SfJson.Get(d, f)), people).Hits)
                    Add(p, "done");

        var byReason = new List<(string Reason, List<ShopfloorPerson> People)>();
        foreach (var key in order)
        {
            var (p, reason) = targets[key];
            if (p.Kuerzel.Length > 0 && string.Equals(p.Kuerzel, user ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                continue;   // wer selbst aendert, bekommt keine Mail
            var slot = byReason.FirstOrDefault(x => x.Reason == reason);
            if (slot.People is null)
            {
                slot = (reason, []);
                byReason.Add(slot);
            }
            slot.People.Add(p);
        }
        foreach (var (reason, list) in byReason)
            queued.Add(Enqueue(con, cfg, mod, rec, list, reason, user ?? string.Empty, baseUrl));
        return queued;
    }

    public static JsonObject Enqueue(SqliteConnection con, ShopfloorConfig cfg, ShopfloorModuleDef mod, JsonObject rec, List<ShopfloorPerson> people,
        string reason, string user, string baseUrl)
    {
        cfg.NotifyRules.TryGetValue(mod.Key, out var rule);
        var (subject, html, text) = Build(mod, rule, rec, reason, baseUrl, user);
        var rcpt = new JsonArray();
        foreach (var p in people)
            rcpt.Add(new JsonObject { ["name"] = p.Name, ["email"] = p.Email });
        var noMail = people.Where(p => p.Email.Length == 0).Select(p => p.Name).ToList();
        var status = people.Any(p => p.Email.Length > 0) ? "wartend" : "keine Adresse";
        var id = ShopfloorDb.Insert(con,
            "INSERT INTO mail_outbox(ts,user,module,ref,rcpt,subject,html,text,status,error,reason) VALUES(?,?,?,?,?,?,?,?,?,?,?)",
            Now(), user, mod.Key, SfJson.PyStr(rec["nr"]), SfJson.Dump(rcpt), subject, html, text, status,
            noMail.Count > 0 ? "Keine E-Mail hinterlegt: " + string.Join(", ", noMail) : null, reason);
        return new JsonObject { ["id"] = id, ["to"] = rcpt.DeepClone(), ["subject"] = subject, ["status"] = status };
    }

    public static JsonArray Outbox(SqliteConnection con, int limit = 200)
    {
        var arr = new JsonArray();
        foreach (var r in ShopfloorDb.Query(con, "SELECT * FROM mail_outbox ORDER BY id DESC LIMIT ?", limit))
            arr.Add(new JsonObject
            {
                ["id"] = r.Long("id"), ["ts"] = r.Str("ts"), ["user"] = r.Str("user"), ["module"] = r.Str("module"), ["ref"] = r.Str("ref"),
                ["rcpt"] = SfJson.ParseArray(r.Str("rcpt")), ["subject"] = r.Str("subject"), ["html"] = null, ["text"] = r.Str("text"),
                ["status"] = r.Str("status"), ["error"] = r.Str("error"), ["tries"] = r.Long("tries"), ["sent_at"] = r.Str("sent_at"),
                ["reason"] = r.Str("reason")
            });
        return arr;
    }

    /// <summary>Versandstatus. Graph/SMTP sind nicht umgesetzt, der Modus bleibt "aus".</summary>
    public static JsonObject Status(string baseUrl)
        => new() { ["modus"] = "aus", ["absender"] = string.Empty, ["base_url"] = baseUrl, ["bereit"] = false };
}
