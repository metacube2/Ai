using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public class ShopfloorZd05Tests
{
    private static JsonArray Day1() => ShopfloorTestEnv.Arr("""
        [
          {"material":"A100","text":"Teil A","unterdeck":"2026-10-01","dmk":"Z2","lzcode":"A9T0"},
          {"material":"B200","text":"Teil B","unterdeck":"2026-10-02"}
        ]
        """);

    private static JsonArray Day2() => ShopfloorTestEnv.Arr("""
        [
          {"material":"A100","text":"Teil A","unterdeck":"2026-10-01"},
          {"material":"C300","text":"Teil C","unterdeck":"2026-10-20"}
        ]
        """);

    [Fact]
    public void Import_Uebernimmt_Bemerkung_Liefertermin_Code_Und_Markiert_Neu_Und_Erledigt()
    {
        using var env = new ShopfloorTestEnv();
        env.Write(con =>
        {
            ShopfloorZd05.ImportRows(con, "2026-10-05", Day1(), "tst");
            ShopfloorZd05.SaveRow(con, "2026-10-05", "A100", ShopfloorTestEnv.Obj("""{"bemerkung":"Lieferant mahnen","liefertermin":"2026-10-12","code":2}"""), "tst");
            return true;
        });

        var r = env.Write(con => ShopfloorZd05.ImportRows(con, "2026-10-06", Day2(), "tst"));

        Assert.Equal(2, (int)r["positionen"]!);
        Assert.Equal(1, (int)r["neu"]!);   // nur C300 ist neu
        Assert.Equal("2026-10-05", (string)r["vortag"]!);
        Assert.Equal(["B200"], r["erledigt"]!.AsArray().Select(x => (string)x!).ToArray());

        var day = env.Store.GetZd05("2026-10-06")!.AsObject();
        var rows = day["rows"]!.AsArray().Select(x => x!.AsObject()).ToDictionary(x => (string)x["material"]!);
        Assert.Equal("Lieferant mahnen", (string)rows["A100"]["bemerkung"]!);
        Assert.Equal("2026-10-12", (string)rows["A100"]["liefertermin"]!);
        Assert.Equal(2, (int)rows["A100"]["code"]!);
        Assert.Null(rows["A100"]["neu"]);
        Assert.True((bool)rows["C300"]["neu"]!);
        Assert.Equal("B200", (string)day["resolved"]!.AsArray()[0]!["material"]!);
    }

    [Fact]
    public void Kennzahl_Zaehlt_Nur_Positionen_Im_Horizont_Mit_Code()
    {
        // 2026-10-06 ist ein Dienstag, drei Arbeitstage spaeter ist Freitag 2026-10-09.
        var rows = new List<JsonObject>
        {
            ShopfloorTestEnv.Obj("""{"material":"A","unterdeck":"2026-10-01","code":2}"""),
            ShopfloorTestEnv.Obj("""{"material":"B","unterdeck":"2026-10-09","code":1}"""),
            ShopfloorTestEnv.Obj("""{"material":"C","code":0}"""),                              // ohne Unterdeckung zaehlt immer
            ShopfloorTestEnv.Obj("""{"material":"D","unterdeck":"2026-10-20","code":2}"""),   // ausserhalb Horizont
            ShopfloorTestEnv.Obj("""{"material":"E","unterdeck":"2026-10-02"}""")             // ohne Code
        };
        var abc = new Dictionary<string, (string? Abc, string? Xyz)> { ["A"] = ("A", "X"), ["D"] = ("B", "Z") };

        var k = ShopfloorZd05.ComputeKpi("2026-10-06", rows, abc);

        Assert.Equal("2026-10-09", (string)k["horizon"]!);
        Assert.Equal(5, (int)k["positionen"]!);
        Assert.Equal(4, (int)k["im_horizont"]!);
        Assert.Equal(3, (int)k["anzahl"]!);
        Assert.Equal(3, (int)k["summe"]!);
        Assert.Equal(1.0, (double)k["kennzahl"]!, 4);
        Assert.Equal(1, (int)k["code2"]!);          // D liegt ausserhalb des Horizonts
        Assert.Equal(1, (int)k["ohne_code"]!);
        Assert.Equal(1, (int)k["c2_A"]!);
        Assert.Equal(1, (int)k["c2_B"]!);          // c2_* zaehlt ueber alle Positionen
        Assert.Equal(1, (int)k["c2_X"]!);
        Assert.Equal(1, (int)k["c2_Z"]!);
    }

    [Fact]
    public void Bearbeiten_Rechnet_Tageskennzahl_Neu()
    {
        using var env = new ShopfloorTestEnv();
        env.Write(con => ShopfloorZd05.ImportRows(con, "2026-10-06", Day2(), "tst"));
        env.Write(con => ShopfloorZd05.SaveRow(con, "2026-10-06", "A100", ShopfloorTestEnv.Obj("""{"code":"2"}"""), "tst"));

        var day = env.Store.GetZd05("2026-10-06")!.AsObject();

        Assert.Equal(2.0, (double)day["kpi"]!["kennzahl"]!, 4);
        Assert.Equal(1.2, (double)day["target"]!, 4);
    }

    [Fact]
    public void Arbeitstage_Ueberspringen_Das_Wochenende()
    {
        Assert.Equal(new DateTime(2026, 10, 14), ShopfloorZd05.AddWorkdays(new DateTime(2026, 10, 9), 3));
        Assert.Null(ShopfloorZd05.CodeOf(JsonValue.Create("x")));
        Assert.Null(ShopfloorZd05.CodeOf(JsonValue.Create(5)));
        Assert.Equal(1, ShopfloorZd05.CodeOf(JsonValue.Create("1.0")));
    }

    [Fact]
    public void Ohne_Position_Wirft_SaveRow_Fachfehler()
    {
        using var env = new ShopfloorTestEnv();
        var ex = Assert.Throws<ShopfloorException>(() => env.Store.PostZd05("row", ShopfloorTestEnv.Obj("""{"day":"2026-10-06","material":"X","changes":{}}"""), "tst"));
        Assert.Equal(400, ex.StatusCode);
    }
}
