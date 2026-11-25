using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>Paket Fabio 24.11.2026: Radar Verzug Einkauf/GFS, Materialverfuegbarkeit je KW, entfernte Module und Felder.</summary>
public class ShopfloorPaket2411Tests
{
    private static void Rs(Microsoft.Data.Sqlite.SqliteConnection con, string module, string nr, string json)
        => ShopfloorDb.Exec(con, "INSERT INTO records(module,nr,data) VALUES(?,?,?)", module, nr, json);

    private static void Prod(Microsoft.Data.Sqlite.SqliteConnection con, string module, string day, double prod)
        => ShopfloorDb.Exec(con, "INSERT INTO daily(module,day,data) VALUES(?,?,?)", module, day, $$"""{"prod":{{prod}}}""");

    [Fact]
    public void Radar_Summiert_Verzug_Je_Ursache_Und_Zaehlt_Beide_Ursachen_Im_Total_Einmal()
    {
        using var env = new ShopfloorTestEnv();
        env.Write(con =>
        {
            Prod(con, "tr5", "2025-12-31", 999);          // Vorjahr zaehlt nicht
            Prod(con, "tr5", "2026-01-05", 1000);
            Prod(con, "tr5", "2026-10-28", 500);
            Prod(con, "tr5", "2026-10-29", 700);          // Stichtag selbst zaehlt nicht (Stand Vortag wie Marcos Excel)
            Rs(con, "rs_tr5", "A", """{"datum":"2026-03-01","menge_rs":10,"verursacher":["Einkauf"],"status":"erledigt"}""");
            Rs(con, "rs_tr5", "B", """{"datum":"2026-06-01","menge_rs":20,"verursacher":["GFS DE","Einkauf"]}""");
            Rs(con, "rs_tr5", "C", """{"datum":"2026-07-01","menge_rs":40,"verursacher":["Dispo"]}""");
            Rs(con, "rs_tr5", "D", """{"datum":"2025-12-20","menge_rs":80,"verursacher":["Einkauf"]}""");   // Vorjahr
            Rs(con, "rs_tr5", "E", """{"datum":"2026-10-30","menge_rs":160,"verursacher":["Einkauf"]}"""); // nach Stichtag
            return true;
        });

        var r = env.Write(con => ShopfloorRadar.Compute(con, "2026-10-29"));
        var tr5 = r["rows"]!.AsArray().Select(x => x!.AsObject()).Single(x => (string)x["key"]! == "tr5");
        var total = r["rows"]!.AsArray().Select(x => x!.AsObject()).Single(x => (string)x["key"]! == "total");

        Assert.Equal(1500, (double)tr5["prod_ytd"]!);
        Assert.Equal(30, (double)tr5["verzug_einkauf"]!);
        Assert.Equal(20, (double)tr5["verzug_gfs"]!);
        Assert.Equal(30, (double)tr5["verzug_total"]!);     // B mit beiden Ursachen nur einmal
        Assert.Equal(1, (int)tr5["zeilen_beide"]!);
        Assert.Equal(2.0, (double)tr5["prozent"]!, 2);
        Assert.Equal(1500, (double)total["prod_ytd"]!);
        Assert.Equal("2026-10-28", (string)r["prod_bis"]!);
    }

    [Fact]
    public void Radar_Ohne_Produktion_Hat_Kein_Prozent()
    {
        using var env = new ShopfloorTestEnv();
        var r = env.Write(con => ShopfloorRadar.Compute(con, "2026-11-25"));
        Assert.All(r["rows"]!.AsArray(), x => Assert.Null(x!["prozent"]));
        Assert.Equal(5, r["rows"]!.AsArray().Count);   // SEH, TR5, TX, DW, Total
    }

    [Fact]
    public void Materialverfuegbarkeit_Nimmt_Hoechsten_Tageswert_Mo_Bis_Sa_Und_Den_Nenner_Der_KW()
    {
        // KW 47/2026: Mo 16.11. bis Sa 21.11.; Marcos Beispiel: hoechster Wert 4 am Donnerstag, 1'525 Materialien -> 99,74 %
        var days = new[] { ("2026-11-16", 1), ("2026-11-18", 2), ("2026-11-19", 4), ("2026-11-21", 3), ("2026-11-22", 9) }
            .Select(x => new JsonObject { ["day"] = x.Item1, ["kpi"] = new JsonObject { ["code2"] = x.Item2 } }).ToList();
        days.Add(new JsonObject { ["day"] = "2026-11-20", ["kpi"] = new JsonObject { ["kennzahl"] = 1.1 } });   // ohne code2

        var w = ShopfloorZd05.WeekCounters(days).Single();

        Assert.Equal("2026-W47", w.Kw);
        Assert.Equal(4, w.Max);                 // Sonntag 22.11. (9) zaehlt nicht
        Assert.Equal("2026-11-19", w.Day);
        Assert.Equal(4, w.Tage);
        Assert.Equal(99.74, ShopfloorZd05.Materialverfuegbarkeit(4, 1525));
        Assert.Null(ShopfloorZd05.Materialverfuegbarkeit(4, null));
    }

    [Fact]
    public void History_Rechnet_Materialverfuegbarkeit_Aus_Code2_Und_Eingegebenem_Nenner()
    {
        using var env = new ShopfloorTestEnv();
        env.Write(con =>
        {
            ShopfloorZd05.ImportRows(con, "2026-11-19", ShopfloorTestEnv.Arr("""[{"material":"A","unterdeck":"2026-11-19"},{"material":"B"}]"""), "tst");
            ShopfloorZd05.SaveRow(con, "2026-11-19", "A", ShopfloorTestEnv.Obj("""{"code":2}"""), "tst");
            ShopfloorZd05.SaveRow(con, "2026-11-19", "B", ShopfloorTestEnv.Obj("""{"code":2}"""), "tst");
            ShopfloorZd05.SaveWeek(con, "2026-W47", ShopfloorTestEnv.Obj("""{"nenner":"1'000"}"""), "tst");
            return true;
        });

        var h = env.Store.GetZd05History().AsObject();
        var w = h["weeks"]!.AsArray().Select(x => x!.AsObject()).Single(x => (string)x["kw"]! == "2026-W47");

        Assert.Equal(2, (int)w["c2_max"]!);
        Assert.Equal(1000, (double)w["nenner"]!);
        Assert.Equal(99.8, (double)w["mv"]!, 2);
    }

    [Fact]
    public void Horizont_Zaehlt_Arbeitstage_Nach_Fabrikkalender()
    {
        // 24.12.2026 bis 01.01.2027 sind laut Fabrikkalender CH keine Arbeitstage
        Assert.Equal(new DateTime(2027, 1, 4), ShopfloorZd05.AddWorkdays(new DateTime(2026, 12, 23), 1));
        Assert.Equal(new DateTime(2026, 11, 30), ShopfloorZd05.AddWorkdays(new DateTime(2026, 11, 25), 3));
    }

    [Fact]
    public void Kommunikation_Einkauf_Und_Offene_KAuf_Sind_Entfernt()
    {
        var cfg = JsonNode.Parse(File.ReadAllText(Path.Combine(ShopfloorTestEnv.ShopfloorDir, "shopfloor_config.json")))!.AsObject();
        var modules = cfg["modules"]!.AsArray().Select(m => (string)m!["key"]!).ToList();
        Assert.DoesNotContain("einkauf", modules);
        Assert.Contains("zd05", modules);
        Assert.DoesNotContain(cfg["agenda"]!.AsArray(), a => (string?)a!["module"] == "einkauf");
        Assert.False(cfg["notify_rules"]!.AsObject().ContainsKey("einkauf"));
        var logistik = cfg["modules"]!.AsArray().Single(m => (string)m!["key"]! == "logistik")!;
        Assert.DoesNotContain(logistik["fields"]!.AsArray(), f => (string?)f!["key"] == "offene_kauf");

        var app = File.ReadAllText(Path.Combine(ShopfloorTestEnv.ProjectRoot, "wwwroot", "shopfloor", "app.js"));
        Assert.DoesNotContain("#/m/einkauf", app);
        Assert.DoesNotContain("k.c2_A", app);
    }
}
