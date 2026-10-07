using System.Text.Json;
using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public sealed class ShopfloorSapSyncTests
{
    private static List<JsonElement> Rows(string json)
        => JsonDocument.Parse(json).RootElement.EnumerateArray().Select(e => e.Clone()).ToList();

    [Fact]
    public void Zd05_Zeilen_Wie_Der_Excel_Import()
    {
        var rows = ShopfloorSapSync.Zd05Rows(Rows("""
            [{"Werks":"1100","Matnr":"B44222","Dispo":"003","Maktx":"PCBA","Udek":"20261023","Dismm":"Z2","Lzcode":"N0X0","VerbrWbz":5203,"SibeOpt":0,"SibeAkt":0,"AntPlan":176,"Tendenz":"G"},
             {"Werks":"1100","Matnr":"000000000000063500","Dispo":"001","Maktx":"X","Udek":"00000000","Dismm":"PD","Lzcode":"","VerbrWbz":"12","SibeOpt":3,"SibeAkt":5,"AntPlan":0,"Tendenz":"R"}]
            """));

        Assert.Equal(2, rows.Count);
        var first = (JsonObject)rows[0]!;
        Assert.Equal("B44222", (string?)first["material"]);
        Assert.Equal("2026-10-23", (string?)first["unterdeck"]);
        Assert.Equal("Z2", (string?)first["dmk"]);
        Assert.Equal(5203, (int)first["verbr_wbz"]!);
        Assert.Equal(176, (int)first["ant_plan"]!);
        var second = (JsonObject)rows[1]!;
        Assert.Equal("63500", (string?)second["material"]);
        Assert.Equal("", (string?)second["unterdeck"]);
        Assert.Equal(12, (int)second["verbr_wbz"]!);
    }

    [Fact]
    public void Auftraege_Fuer_Den_Forecast_Bis_Zum_Horizont()
    {
        var rows = ShopfloorSapSync.OrderRows(Rows("""
            [{"Aufnr":"0002360008","Typ":"PA","Werks":"1100","Datum":"20261013","Matnr":"000000000000063500","Dispo":"EL2","Menge":60,"Meins":"ST"},
             {"Aufnr":"000001234567","Typ":"FE","Werks":"1100","Datum":"20261201","Matnr":"64494","Dispo":"EK1","Menge":10,"Meins":"ST"}]
            """), new DateOnly(2026, 11, 10));

        var only = Assert.Single(rows);
        var o = (JsonObject)only!;
        Assert.Equal("2026-10-13", (string?)o["day"]);
        Assert.Equal("2360008", (string?)o["auftrag"]);
        Assert.Equal("63500", (string?)o["material"]);
        Assert.Equal("EL2", (string?)o["disponent"]);
        Assert.Equal(60, (int)o["menge"]!);
    }

    [Fact]
    public void Abgleich_Nur_An_Werktagen_Und_Einmal_Je_Zeitpunkt()
    {
        var times = new[] { "05:45", "12:45" };
        var mittwoch = new DateTime(2026, 10, 7, 13, 0, 0);
        Assert.Equal(new DateTime(2026, 10, 7, 12, 45, 0), ShopfloorSapSync.DueSlot(times, mittwoch, DateTime.MinValue));
        Assert.Null(ShopfloorSapSync.DueSlot(times, mittwoch, new DateTime(2026, 10, 7, 12, 45, 0)));
        Assert.Null(ShopfloorSapSync.DueSlot(times, new DateTime(2026, 10, 10, 13, 0, 0), DateTime.MinValue));
        Assert.Null(ShopfloorSapSync.DueSlot(times, new DateTime(2026, 10, 7, 5, 0, 0), DateTime.MinValue));
    }
}
