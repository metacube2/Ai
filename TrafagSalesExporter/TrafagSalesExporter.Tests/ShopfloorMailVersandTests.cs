using System.Net.Mail;
using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>Mailversand aus dem Board (Fabio 26.11.2026 Punkt 11): Schalter aus, kein Nachversand alter Protokollmails, Ereignisse je Liste.</summary>
public class ShopfloorMailVersandTests
{
    private static ShopfloorMailOptions Bereit() => new()
    {
        Modus = "smtp", Host = "relay.test", Absender = "shopfloor@trafag.com", SendenAb = "2026-11-26 12:00", MaxVersuche = 2
    };

    private static long Outbox(Microsoft.Data.Sqlite.SqliteConnection con, string ts, string email, string status = "wartend")
        => ShopfloorDb.Insert(con, "INSERT INTO mail_outbox(ts,user,module,ref,rcpt,subject,html,text,status,reason) VALUES(?,?,?,?,?,?,?,?,?,?)",
            ts, "test", "pendenzen", "SFP-26-001", $$"""[{"name":"Fabio","email":"{{email}}"}]""", "[Shop-Floor] Test", "<p>x</p>", "x", status, "neu");

    private static string Status(ShopfloorTestEnv env, long id)
        => env.Write(con => ShopfloorDb.QueryOne(con, "SELECT status FROM mail_outbox WHERE id=?", id)!.Str("status")!);

    [Fact]
    public void Standard_Ist_Aus_Und_Versendet_Nichts()
    {
        using var env = new ShopfloorTestEnv();
        var id = env.Write(con => Outbox(con, "2026-11-27 08:00:00", "fabio.palma@trafag.com"));
        var sent = new List<MailMessage>();

        var o = new ShopfloorMailOptions();
        Assert.False(o.Bereit);
        Assert.Contains("Modus nicht „smtp“", o.Fehlt());
        Assert.Equal((0, 0, 0), ShopfloorMailVersand.Lauf(env.Db, o, sent.Add));
        Assert.Empty(sent);
        Assert.Equal("wartend", Status(env, id));

        // eingeschaltet, aber ohne SendenAb: weiterhin nichts
        var ohneAb = Bereit(); ohneAb.SendenAb = "";
        Assert.False(ohneAb.Bereit);
        Assert.Equal((0, 0, 0), ShopfloorMailVersand.Lauf(env.Db, ohneAb, sent.Add));
        Assert.Empty(sent);
    }

    [Fact]
    public void Alte_Protokollmails_Gehen_Nie_Raus_Neue_Werden_Gesendet()
    {
        using var env = new ShopfloorTestEnv();
        var (alt, neu, ohne) = env.Write(con => (
            Outbox(con, "2026-10-20 09:00:00", "a@trafag.com"),
            Outbox(con, "2026-11-26 12:05:00", "fabio.palma@trafag.com"),
            Outbox(con, "2026-11-26 12:06:00", "")));
        var sent = new List<MailMessage>();

        var r = ShopfloorMailVersand.Lauf(env.Db, Bereit(), sent.Add);

        Assert.Equal((1, 0, 1), r);
        Assert.Single(sent);
        Assert.Equal("fabio.palma@trafag.com", sent[0].To.Single().Address);
        Assert.Equal("shopfloor@trafag.com", sent[0].From!.Address);
        Assert.True(sent[0].IsBodyHtml);
        Assert.Equal(ShopfloorMailVersand.NichtGesendet, Status(env, alt));
        Assert.Equal("gesendet", Status(env, neu));
        Assert.Equal("keine Adresse", Status(env, ohne));

        // zweiter Lauf: nichts mehr offen, die alte Mail bleibt unversendet
        Assert.Equal((0, 0, 0), ShopfloorMailVersand.Lauf(env.Db, Bereit(), sent.Add));
        Assert.Single(sent);
    }

    [Fact]
    public void Fehler_Wird_Wiederholt_Und_Nach_MaxVersuche_Als_Fehler_Markiert()
    {
        using var env = new ShopfloorTestEnv();
        var id = env.Write(con => Outbox(con, "2026-11-26 13:00:00", "fabio.palma@trafag.com"));
        void Kaputt(MailMessage _) => throw new SmtpException("Relay verweigert");

        Assert.Equal((0, 1, 0), ShopfloorMailVersand.Lauf(env.Db, Bereit(), Kaputt));
        Assert.Equal("wartend", Status(env, id));
        Assert.Equal((0, 1, 0), ShopfloorMailVersand.Lauf(env.Db, Bereit(), Kaputt));
        Assert.Equal("Fehler", Status(env, id));
        var err = env.Write(con => ShopfloorDb.QueryOne(con, "SELECT error FROM mail_outbox WHERE id=?", id)!.Str("error"));
        Assert.Contains("Relay verweigert", err);
    }

    [Fact]
    public void Erneut_Setzt_Die_Zeit_Neu_Damit_Die_Mail_Nicht_Als_Alt_Gilt()
    {
        using var env = new ShopfloorTestEnv();
        var id = env.Write(con => Outbox(con, "2026-10-20 09:00:00", "fabio.palma@trafag.com", "Fehler"));
        env.Store.PostMail("retry", new JsonObject { ["id"] = id }, "koi");
        var o = Bereit(); o.SendenAb = DateTime.Now.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm");
        var sent = new List<MailMessage>();
        Assert.Equal((1, 0, 0), ShopfloorMailVersand.Lauf(env.Db, o, sent.Add));
    }

    [Fact]
    public void Status_Nennt_Was_Fehlt_Und_Kein_Passwort()
    {
        using var env = new ShopfloorTestEnv();
        var o = Bereit(); o.Host = ""; o.Passwort = "geheim";
        var st = env.Store.GetMail(o)["status"]!.AsObject();
        Assert.Equal("smtp", (string)st["modus"]!);
        Assert.False((bool)st["bereit"]!);
        Assert.Contains("Mailserver", st["fehlt"]!.AsArray().Select(x => (string)x!));
        Assert.DoesNotContain("geheim", st.ToJsonString());
        Assert.Equal("aus", (string)env.Store.GetMail()["status"]!["modus"]!);
    }

    // ---------------------------------------------------------------- Ereignisse je Liste

    private static ShopfloorStore StoreMitEreignissen(ShopfloorTestEnv env, params string[] ereignisse)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json")))!.AsObject();
        var ev = new JsonArray(); foreach (var e in ereignisse) ev.Add(e);
        root["notify_rules"]!["pendenzen"]!["ereignisse"] = ev;
        var path = Path.Combine(env.Dir, "cfg.json");
        File.WriteAllText(path, root.ToJsonString());
        var store = new ShopfloorStore(env.Db, ShopfloorConfig.Load(path), () => "https://test/shopfloor/index.html");
        store.PostMail("people", new JsonObject
        {
            ["rows"] = new JsonArray(new JsonObject { ["name"] = "Fabio Palma", ["kuerzel"] = "FP", ["email"] = "fabio.palma@trafag.com", ["typ"] = "Person" })
        }, "koi");
        return store;
    }

    private static JsonArray Speichern(ShopfloorStore s, long? id, JsonObject changes)
    {
        var body = new JsonObject { ["module"] = "pendenzen", ["changes"] = changes };
        if (id is not null) body["id"] = id;
        return s.PostRecord(body, "koi")["mails"]!.AsArray();
    }

    [Fact]
    public void Ohne_Eintrag_Gilt_Neu_Zust_Done_Wie_Bisher()
    {
        var rule = ShopfloorConfig.Load(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json")).NotifyRules["pendenzen"];
        Assert.Equal(new[] { "neu", "zust", "done" }, rule.Ereignisse);
    }

    [Fact]
    public void Statuswechsel_Und_Terminaenderung_Nur_Wenn_Eingeschaltet()
    {
        using var env = new ShopfloorTestEnv();
        var aus = StoreMitEreignissen(env, "neu");
        var rec = aus.PostRecord(new JsonObject
        {
            ["module"] = "pendenzen",
            ["changes"] = new JsonObject { ["beschreibung"] = "Test", ["verantwortlich"] = "Fabio Palma", ["status"] = "offen" }
        }, "koi");
        Assert.Single(rec["mails"]!.AsArray());                                     // neu
        var id = (long)(double)SfJson.AsDouble(rec["id"])!;
        Assert.Empty(Speichern(aus, id, new JsonObject { ["status"] = "in Arbeit" }));   // status nicht eingeschaltet
        Assert.Empty(Speichern(aus, id, new JsonObject { ["zieltermin"] = "2026-12-01" }));

        var ein = StoreMitEreignissen(env, "neu", "status", "termin");
        var m1 = Speichern(ein, id, new JsonObject { ["status"] = "wartet" });
        Assert.Single(m1);
        Assert.Contains("Statuswechsel", (string)m1[0]!["subject"]!);
        var m2 = Speichern(ein, id, new JsonObject { ["zieltermin"] = "2026-12-03" });
        Assert.Single(m2);
        Assert.Contains("Terminänderung", (string)m2[0]!["subject"]!);
        Assert.Empty(Speichern(ein, id, new JsonObject { ["status"] = "erledigt" }));    // done nicht eingeschaltet
    }
}
