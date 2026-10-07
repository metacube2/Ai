using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public class ShopfloorForecastTests
{
    [Theory]
    [InlineData("EL3", "TX")]
    [InlineData("EK1", "TR5")]
    [InlineData("DS2", "DW")]
    [InlineData("D1", "DW")]
    [InlineData("012", "SEH")]
    [InlineData("CZ7", "CZ")]
    [InlineData("XY", "?")]
    public void Disponent_Wird_Der_Abteilung_Zugeordnet(string disponent, string erwartet)
    {
        var order = new JsonObject { ["disponent"] = disponent };
        Assert.Equal(erwartet, ShopfloorForecast.AssignDept(order));
    }

    [Fact]
    public void Explizite_Abteilung_Hat_Vorrang()
    {
        var order = new JsonObject { ["abteilung"] = "seh", ["disponent"] = "EL1" };
        Assert.Equal("SEH", ShopfloorForecast.AssignDept(order));
    }

    [Fact]
    public void Import_Ersetzt_Datumsbereich_Und_Liefert_Eine_Abteilung_Und_Einen_Tag()
    {
        using var env = new ShopfloorTestEnv();
        env.Write(con => ShopfloorForecast.ImportOrders(con, ShopfloorTestEnv.Arr("""
            [
              {"day":"2026-10-13","auftrag":"1","material":"M1","menge":100,"disponent":"EL2"},
              {"day":"2026-10-13","auftrag":"2","material":"M2","menge":"50","disponent":"EL5"},
              {"day":"2026-10-14","auftrag":"3","material":"M3","menge":20,"disponent":"EK1"},
              {"day":"2026-10-15","auftrag":"4","material":"M4","menge":null,"disponent":"EK1"}
            ]
            """), "tst", "Upload", "plan.xlsx"));

        // Zweiter Import ueberlappt: ersetzt nur 13. bis 14., nicht frueher.
        var r = env.Write(con => ShopfloorForecast.ImportOrders(con, ShopfloorTestEnv.Arr("""
            [{"day":"2026-10-13","auftrag":"9","material":"M9","menge":70,"disponent":"EL1"}]
            """), "tst"));
        Assert.Equal("2026-10-13", (string)r["von"]!);
        Assert.Equal(1, (int)r["pro_abteilung"]!["TX"]!);

        var week = env.Store.GetForecast("2026-10-12")!.AsObject();
        var orders = week["orders"]!.AsArray().Select(x => x!.AsObject()).ToList();
        // 13.10. wurde ersetzt (ein TX-Auftrag), der TR5-Auftrag vom 14.10. bleibt bestehen
        Assert.Equal(2, orders.Count);
        var tx = orders.Single(o => (string)o["dept"]! == "TX" && (string)o["day"]! == "2026-10-13");
        Assert.Equal(70, (double)tx["menge"]!, 3);
        Assert.Equal("TR5", (string)orders.Single(o => (string)o["day"]! == "2026-10-14")["dept"]!);
        Assert.NotNull(week["import"]);
    }

    [Fact]
    public void Import_Ohne_Auftraege_Wirft_Fachfehler()
    {
        using var env = new ShopfloorTestEnv();
        var ex = Assert.Throws<ShopfloorException>(() => env.Write(con => ShopfloorForecast.ImportOrders(con, ShopfloorTestEnv.Arr("""[{"day":"2026-10-13"}]"""), "tst")));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public void Planwerte_Und_Einstellungen_Werden_Pro_Abteilung_Und_Tag_Gespeichert()
    {
        using var env = new ShopfloorTestEnv();
        env.Store.PostForecast("plan", ShopfloorTestEnv.Obj("""{"dept":"TX","day":"2026-10-13","changes":{"ma1":"4,5","ma2":3,"plan":"1'200","notiz":"Schicht 2 verstaerken","unbekannt":9}}"""), "tst");
        env.Store.PostForecast("settings", ShopfloorTestEnv.Obj("""{"dept":"TX","changes":{"leistung":12.5,"ziel_auslastung":""}}"""), "tst");
        env.Store.PostForecast("plan", ShopfloorTestEnv.Obj("""{"dept":"TX","day":"2026-10-13","changes":{"ma2":null}}"""), "tst");

        var week = env.Store.GetForecast("2026-10-12")!.AsObject();
        var plan = week["plan"]!["TX"]!["2026-10-13"]!.AsObject();

        Assert.Equal(4.5, (double)plan["ma1"]!, 3);
        Assert.Equal(1200, (double)plan["plan"]!, 3);
        Assert.Null(plan["ma2"]);                       // geleert
        Assert.Null(plan["unbekannt"]);                 // nur bekannte Felder
        Assert.Equal("Schicht 2 verstaerken", (string)plan["notiz"]!);
        Assert.Equal(12.5, (double)week["settings"]!["TX"]!["leistung"]!, 3);
        Assert.Equal("tst", (string)plan["updated_by"]!);
    }
}
