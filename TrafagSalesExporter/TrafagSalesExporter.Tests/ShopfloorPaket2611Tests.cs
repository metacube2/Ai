using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>Mail Fabio 26.11.2026: Punkt 10 (TV MTS entfernt) und Meeting-Modus „nur Abweichungen“ (Punkt 6, SEH ausgenommen).</summary>
public class ShopfloorPaket2611Tests
{
    private static void Tag(Microsoft.Data.Sqlite.SqliteConnection con, string module, string day, double? prod, double? ziel)
    {
        var d = new JsonObject();
        if (prod is not null) d["prod"] = prod;
        if (ziel is not null) d["ziel"] = ziel;
        ShopfloorDb.Exec(con, "INSERT INTO daily(module,day,data) VALUES(?,?,?)", module, day, d.ToJsonString());
    }

    private static JsonObject Row(JsonNode r, string key)
        => r["rows"]!.AsArray().Select(x => x!.AsObject()).Single(x => (string)x["key"]! == key);

    [Theory]
    [InlineData(800.0, 800.0, "ok")]
    [InlineData(721.0, 800.0, "ok")]        // -9,9 %
    [InlineData(720.0, 800.0, "gelb")]      // -10,0 %: „ab 10 % darunter“ zaehlt mit
    [InlineData(641.0, 800.0, "gelb")]      // -19,9 %
    [InlineData(640.0, 800.0, "rot")]       // -20,0 %
    [InlineData(959.0, 800.0, "ok")]        // +19,9 %
    [InlineData(960.0, 800.0, "hinweis")]   // +20,0 %
    public void Stufe_Grenzen_Zaehlen_Mit(double prod, double ziel, string erwartet)
        => Assert.Equal(erwartet, ShopfloorMeeting.Stufe(prod, ziel, ShopfloorMeeting.Standard));

    [Theory]
    [InlineData(null, 800.0)]
    [InlineData(500.0, null)]
    [InlineData(500.0, 0.0)]
    public void Fehlender_Wert_Ist_Keine_Bewertung_Und_Nie_Unter_Ziel(double? prod, double? ziel)
        => Assert.Equal("keine_daten", ShopfloorMeeting.Stufe(prod, ziel, ShopfloorMeeting.Standard));

    [Fact]
    public void Bewertet_Den_Vortag_Zaehlt_Serie_Und_Laesst_Seh_Und_Alte_Werte_Aus()
    {
        using var env = new ShopfloorTestEnv();
        env.Write(con =>
        {
            // TR5: Mi 18. bis Fr 20.11. und Mo 23.11. unter Ziel, Wochenende ohne Werte (wird uebersprungen) -> Serie 4
            Tag(con, "tr5", "2026-11-17", 820, 800);   // Di im Ziel, beendet die Serie
            Tag(con, "tr5", "2026-11-18", 700, 800);
            Tag(con, "tr5", "2026-11-19", 790, 800);
            Tag(con, "tr5", "2026-11-20", 600, 800);
            Tag(con, "tr5", "2026-11-23", 790, 800);   // Bewertungstag fuer Stichtag 24.11.
            Tag(con, "tr5", "2026-11-24", 100, 800);   // Stichtag selbst (laufender Tag) zaehlt nicht
            // DW: Vortag im Ziel, keine Serie
            Tag(con, "dw", "2026-11-23", 1000, 800);
            // TX: letzter Wert aelter als vier Tage (MES fehlt) -> keine Bewertung
            Tag(con, "tx", "2026-11-13", 100, 1300);
            Tag(con, "tx", "2026-11-23", null, 1300);
            // SEH: ausgenommen, auch wenn Werte da sind
            Tag(con, "seh", "2026-11-23", 1, 3000);
            return true;
        });

        var r = env.Store.GetMeeting("2026-11-24");

        var tr5 = Row(r, "tr5");
        Assert.Equal("2026-11-23", (string)tr5["tag"]!);
        Assert.Equal("ok", (string)tr5["stufe"]!);              // -1,25 % ist im Ziel ...
        Assert.Equal(4, (int)tr5["serie"]!);                    // ... liegt aber zum vierten Mal in Folge darunter
        Assert.True((bool)tr5["serie_alarm"]!);

        var dw = Row(r, "dw");
        Assert.Equal("hinweis", (string)dw["stufe"]!);           // +25 %
        Assert.Equal(0, (int)dw["serie"]!);
        Assert.False((bool)dw["serie_alarm"]!);

        var tx = Row(r, "tx");
        Assert.Equal("keine_daten", (string)tx["stufe"]!);
        Assert.Equal("2026-11-13", (string)tx["letzter_tag"]!);

        Assert.Equal("ausgenommen", (string)Row(r, "seh")["stufe"]!);
        Assert.Equal("TR5", (string)tr5["label"]!);
    }

    [Fact]
    public void Konfiguration_Meeting_Modus_Wie_Mit_Fabio_Vereinbart()
    {
        var cfg = ShopfloorConfig.Load(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json"));
        var s = ShopfloorMeeting.Lesen(cfg.RawJson);
        Assert.Equal(10, s.GelbUnterPct);
        Assert.Equal(20, s.RotUnterPct);
        Assert.Equal(20, s.HinweisUeberPct);
        Assert.Equal(3, s.TageInFolge);
        Assert.Equal(new[] { "seh" }, s.Ausnehmen);
        Assert.Equal(new[] { "seh", "tr5", "tx", "dw" }, s.Abteilungen);
        // Fehlteile Code 2 und Rueckstaende zuerst; alle genannten Module stehen in der Agenda
        Assert.Equal("zd05", s.Zuerst[0]);
        var agenda = (JsonNode.Parse(cfg.RawJson)!["agenda"] as JsonArray)!.Select(x => (string)x!["module"]!).ToHashSet();
        Assert.All(s.Zuerst, k => Assert.Contains(k, agenda));
        Assert.All(s.Abteilungen, k => Assert.Equal("daily", cfg.Modules[k].Kind));
    }

    [Fact]
    public void Ohne_Eintrag_In_Der_Konfiguration_Gelten_Die_Standardwerte()
        => Assert.Equal(ShopfloorMeeting.Standard, ShopfloorMeeting.Lesen("""{"modules":[]}"""));

    [Fact]
    public void Tv_Mts_Ist_Entfernt_Und_Das_Total_Rechnet_Ohne_Mts()
    {
        var cfg = ShopfloorConfig.Load(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json"));
        var raw = cfg.RawJson;
        Assert.DoesNotContain("tv_mts", raw);
        Assert.DoesNotContain("TV MTS", raw);
        Assert.DoesNotContain("verzug_mts", raw);
        var ppd = (JsonNode.Parse(raw)!["modules"] as JsonArray)!.Single(m => (string)m!["key"]! == "ppd")!;
        var total = ppd["fields"]!.AsArray().Single(f => (string)f!["key"]! == "tv_total")!;
        Assert.Equal("v.tv_seh + v.tv_tr5 + v.tv_tx + v.tv_dw", (string)total["calc"]!);
        // die Abteilung MTS bleibt als Auswahlwert in Listen bestehen
        Assert.Contains("MTS", JsonNode.Parse(raw)!["lists"]!["abteilung"]!.AsArray().Select(x => (string)x!));
    }
}
