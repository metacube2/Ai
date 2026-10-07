using System.Text;
using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public class ShopfloorStoreTests
{
    [Fact]
    public void Eintrag_Rundlauf_Anlegen_Aendern_Verlauf_Loeschen()
    {
        using var env = new ShopfloorTestEnv();

        var created = env.Store.PostRecord(ShopfloorTestEnv.Obj("""
            {"module":"pendenzen","changes":{"datum":"2026-10-05","beschreibung":"Pumpe pruefen","status":"offen","zieltermin":"2026-10-01","unbekannt":"x"}}
            """), "ik");

        var nr = (string)created["nr"]!;
        Assert.Matches(@"^SFP-26-001$", nr);
        Assert.Equal("Pumpe pruefen", (string)created["data"]!["beschreibung"]!);
        Assert.Null(created["data"]!["unbekannt"]);          // nur Felder aus der Konfiguration
        Assert.Equal(1L, (long)created["version"]!);

        var id = (long)created["id"]!;
        var changed = env.Store.PostRecord(ShopfloorTestEnv.Obj("{\"module\":\"pendenzen\",\"id\":" + id + ",\"changes\":{\"status\":\"erledigt\"}}"), "ab");
        Assert.Equal(2L, (long)changed["version"]!);
        Assert.Equal("ab", (string)changed["updated_by"]!);
        Assert.Equal("ik", (string)changed["created_by"]!);

        var second = env.Store.PostRecord(ShopfloorTestEnv.Obj("""{"module":"pendenzen","changes":{"datum":"2026-10-06","beschreibung":"Zweite"}}"""), "ik");
        Assert.Equal("SFP-26-002", (string)second["nr"]!);   // Nummernkreis zaehlt hoch

        var history = env.Store.GetHistory("pendenzen", nr);
        Assert.Equal(2, history.Count);
        Assert.Equal("erledigt", (string)history[0]!["changes"]!["status"]![1]!);

        var agenda = env.Store.AgendaStatus("2026-10-06");
        Assert.Equal(2, (int)agenda["pendenzen"]!["total"]!);
        Assert.Equal(1L, (long)agenda["pendenzen"]!["open"]!);
        Assert.Equal(1L, (long)agenda["pendenzen"]!["new_today"]!);

        env.Store.DeleteRecord(id, "ik");
        var left = env.Store.GetRecords("pendenzen");
        Assert.Single(left);
        Assert.Equal("SFP-26-002", (string)left[0]!["nr"]!);
    }

    [Fact]
    public void Tageswerte_Rundlauf_Mit_Zahlenbereinigung_Und_Quelle()
    {
        using var env = new ShopfloorTestEnv();

        env.Store.PostDaily(ShopfloorTestEnv.Obj("""{"module":"tx","day":"2026-10-05","changes":{"vorrat_total":"51'258","offen_kw":"3776,5","geraete_tag_1m":99}}"""), "ik");
        var res = env.Store.ApplyPush(new ShopfloorPush
        {
            Items = ShopfloorTestEnv.Arr("""[{"module":"tx","day":"2026-10-05","fields":{"vorrat_3m":34732}}, {"module":"gibtesnicht","fields":{}}]""")
        }, "sap");

        Assert.True((bool)res[0]!["ok"]!);
        Assert.False((bool)res[1]!["ok"]!);

        var daily = env.Store.GetDaily("tx", "2026-10-01", "2026-10-31")["tx"]!["2026-10-05"]!.AsObject();
        var data = daily["data"]!.AsObject();
        Assert.Equal(51258, (double)data["vorrat_total"]!, 3);
        Assert.Equal(3776.5, (double)data["offen_kw"]!, 3);
        Assert.Null(data["geraete_tag_1m"]);                          // berechnete Felder werden nicht gespeichert
        Assert.Equal(34732, (double)data["vorrat_3m"]!, 3);
        Assert.Equal("SAP", (string)daily["meta"]!["vorrat_3m"]!["src"]!);   // Quelle sichtbar
        Assert.Null(daily["meta"]!["vorrat_total"]);                  // manuell: kein Quellenpunkt
        Assert.Equal("SAP-Schnittstelle", (string)daily["updated_by"]!);
    }

    [Fact]
    public void Push_Findet_Liste_Per_Match_Und_Legt_Mit_Create_An()
    {
        using var env = new ShopfloorTestEnv();
        env.Store.PostRecord(ShopfloorTestEnv.Obj("""{"module":"rs_tx","changes":{"datum":"2026-10-05","ka_nr":"415689","ka_pos":"10","kunde":"ACME"}}"""), "ik");

        var res = env.Store.ApplyPush(new ShopfloorPush
        {
            Items = ShopfloorTestEnv.Arr("""
                [{"module":"rs_tx","match":{"ka_nr":415689,"ka_pos":"10"},"fields":{"we_geplant":"2026-10-20"}},
                 {"module":"rs_tx","match":{"ka_nr":"1"},"fields":{"we_geplant":"2026-10-21"}},
                 {"module":"rs_tx","match":{"ka_nr":"2"},"create":true,"fields":{"datum":"2026-10-05","ka_nr":"2","we_geplant":"2026-10-22"}}]
                """)
        }, "mes");

        Assert.True((bool)res[0]!["ok"]!);
        Assert.False((bool)res[1]!["ok"]!);
        Assert.True((bool)res[2]!["ok"]!);
        var rows = env.Store.GetRecords("rs_tx");
        Assert.Equal(2, rows.Count);
        var acme = rows.Single(r => (string)r!["data"]!["kunde"]! == "ACME")!;
        Assert.Equal("2026-10-20", (string)acme["data"]!["we_geplant"]!);
        Assert.Equal("MES", (string)acme["meta"]!["we_geplant"]!["src"]!);
    }

    [Fact]
    public void Falsche_Eingaben_Liefern_Fachfehler()
    {
        using var env = new ShopfloorTestEnv();
        Assert.Equal(400, Assert.Throws<ShopfloorException>(() => env.Store.GetRecords("nix")).StatusCode);
        Assert.Equal(400, Assert.Throws<ShopfloorException>(() => env.Store.PostDaily(ShopfloorTestEnv.Obj("""{"module":"tx","day":"05.10.2026","changes":{}}"""), "ik")).StatusCode);
        Assert.Equal(400, Assert.Throws<ShopfloorException>(() => env.Store.PostRecord(ShopfloorTestEnv.Obj("""{"module":"tx","changes":{}}"""), "ik")).StatusCode);
    }

    [Fact]
    public void Csv_Export_Hat_Bom_Semikolon_Und_Deutsche_Zahlen()
    {
        using var env = new ShopfloorTestEnv();
        env.Store.PostRecord(ShopfloorTestEnv.Obj("""{"module":"pendenzen","changes":{"datum":"2026-10-05","beschreibung":"a;b"}}"""), "ik");

        var csv = Encoding.UTF8.GetString(env.Store.ExportCsv("pendenzen"));

        Assert.StartsWith("﻿Nr.;", csv);
        Assert.Contains("05.10.2026", csv);
        Assert.Contains("\"a;b\"", csv);
    }

    [Fact]
    public void Mail_Zustaendigkeit_Landet_Im_Postausgang_Und_Eigene_Aenderung_Nicht()
    {
        using var env = new ShopfloorTestEnv();
        env.Write(con =>
        {
            ShopfloorMail.SavePeople(con, ShopfloorTestEnv.Arr("""[{"name":"Samuel Muster","kuerzel":"smu","email":"s@example.com","typ":"Person","aliases":"Sam"}]"""));
            return true;
        });

        var created = env.Store.PostRecord(ShopfloorTestEnv.Obj("""{"module":"pendenzen","changes":{"datum":"2026-10-05","beschreibung":"Test","verantwortlich":"Samuel"}}"""), "ik");
        Assert.Single(created["mails"]!.AsArray());

        var own = env.Store.PostRecord(ShopfloorTestEnv.Obj("""{"module":"pendenzen","changes":{"datum":"2026-10-05","beschreibung":"Eigene","verantwortlich":"Samuel"}}"""), "smu");
        Assert.Empty(own["mails"]!.AsArray());

        var mail = env.Store.GetMail()!.AsObject();
        Assert.Equal("aus", (string)mail["status"]!["modus"]!);
        var outbox = mail["outbox"]!.AsArray();
        Assert.Single(outbox);
        Assert.Equal("wartend", (string)outbox[0]!["status"]!);
        Assert.Contains("Test", env.Store.MailPreviewHtml(((long)outbox[0]!["id"]!).ToString())!);
    }

    [Fact]
    public void Startdatenbank_Wird_Nur_Kopiert_Wenn_Die_Betriebsdatenbank_Fehlt()
    {
        using var env = new ShopfloorTestEnv(withSeed: true);
        var count = env.Store.GetRecords("pendenzen").Count;
        Assert.True(count > 0, "Startstand mit den Excel-Daten erwartet");

        env.Store.DeleteRecord((long)env.Store.GetRecords("pendenzen")[0]!["id"]!, "ik");
        env.Db.Initialize(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "seed", "shopfloor.seed.db"));   // zweiter Start

        Assert.Equal(count - 1, env.Store.GetRecords("pendenzen").Count);   // nichts ueberschrieben
    }

    [Fact]
    public void Tagessicherung_Legt_Kopie_An_Und_Raeumt_Alte_Auf()
    {
        using var env = new ShopfloorTestEnv();
        var dir = Path.Combine(env.Dir, "backups");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "shopfloor-2020-01-01.db"), "alt");

        env.Db.Backup(dir, 60);

        Assert.Single(Directory.GetFiles(dir));
        Assert.True(File.Exists(Path.Combine(dir, $"shopfloor-{DateTime.Today:yyyy-MM-dd}.db")));
    }

    [Fact]
    public void Konfiguration_Enthaelt_Module_Und_Schnittstellenschema()
    {
        using var env = new ShopfloorTestEnv();
        Assert.True(env.Store.Config.Modules.Count > 10);
        Assert.True(env.Store.Config.NotifyRules.ContainsKey("pendenzen"));
        var schema = env.Store.IntegrationSchema();
        Assert.Contains(schema, m => (string)m!["module"]! == "tx");
        Assert.DoesNotContain(schema, m => (string)m!["module"]! == "grafik");   // Seiten ohne Felder
        Assert.StartsWith("{", env.Store.Config.RawJson.TrimStart());
    }
}
