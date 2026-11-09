using System.Text.Json;
using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public class ShopfloorSapVorratTests
{
    private static List<JsonElement> Rows(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    [Fact]
    public void Filter_FragtTypVrMitAbfragetag()
        => Assert.Equal("Werks eq '1100' and Datum eq '20261109' and Typ eq 'VR'", ShopfloorSapVorrat.Filter("1100", new DateOnly(2026, 11, 9)));

    [Fact]
    public void Grenzen_WieFabiosKontrolle07November()
    {
        // Fabio 07.11.2026 (Sa): 1 Mte bis 30.11., 2 Mte bis 31.12., 3 Mte bis 31.01.2027, KW bis So 08.11.
        var g = ShopfloorSapVorrat.Grenzen(new DateOnly(2026, 11, 7));
        Assert.Equal(new DateOnly(2026, 11, 30), g.M1);
        Assert.Equal(new DateOnly(2026, 12, 31), g.M2);
        Assert.Equal(new DateOnly(2027, 1, 31), g.M3);
        Assert.Equal(new DateOnly(2026, 11, 8), g.KwEnde);
        // Sonntag selbst: KW endet am selben Tag; Montag: bis zum folgenden Sonntag
        Assert.Equal(new DateOnly(2026, 11, 8), ShopfloorSapVorrat.Grenzen(new DateOnly(2026, 11, 8)).KwEnde);
        Assert.Equal(new DateOnly(2026, 11, 15), ShopfloorSapVorrat.Grenzen(new DateOnly(2026, 11, 9)).KwEnde);
    }

    [Fact]
    public void Compute_KumuliertJeAbteilungOhneMaterialfilter()
    {
        var r = ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 9), Rows("""
            [
              {"Typ":"VF","Aufnr":"000002521596","Dispo":"SE1","Matnr":"B54670","Datum":"20261105","Menge":"6"},
              {"Typ":"VP","Aufnr":"0000123","Dispo":"SL5","Matnr":"B64997","Datum":"20261109","Menge":"30"},
              {"Typ":"VF","Aufnr":"000002523740","Dispo":"SE1","Matnr":"B62667","Datum":"20261113","Menge":"60"},
              {"Typ":"VF","Aufnr":"000002523080","Dispo":"SE2","Matnr":"B54452","Datum":"20261130","Menge":"10"},
              {"Typ":"VP","Aufnr":"0000124","Dispo":"SB1","Matnr":"C11095","Datum":"20261215","Menge":"100"},
              {"Typ":"VP","Aufnr":"0000125","Dispo":"SB1","Matnr":"C11095","Datum":"20270131","Menge":"1000"},
              {"Typ":"VP","Aufnr":"0000126","Dispo":"SB1","Matnr":"C11095","Datum":"20270201","Menge":"5000"},
              {"Typ":"VF","Aufnr":"000002000001","Dispo":"EK1","Matnr":"63526","Datum":"20261110","Menge":"7"},
              {"Typ":"VF","Aufnr":"000002000002","Dispo":"DW1","Matnr":"40000","Datum":"20261110","Menge":"999"},
              {"Typ":"VF","Aufnr":"000002000003","Dispo":"001","Matnr":"40000","Datum":"20261110","Menge":"999"}
            ]
            """));
        var seh = (JsonObject)r["werte"]!["seh"]!;
        Assert.Equal(6206L, (long)seh["vorrat_total"]!);
        Assert.Equal(1206L, (long)seh["vorrat_3m"]!);   // bis 31.01.2027
        Assert.Equal(206L, (long)seh["vorrat_2m"]!);    // bis 31.12.
        Assert.Equal(106L, (long)seh["vorrat_1m"]!);    // bis 30.11.
        Assert.Equal(96L, (long)seh["offen_kw"]!);      // bis So 15.11., inkl. ueberfaellig
        Assert.Equal(36L, (long)seh["ziel"]!);          // bis Abfragetag 09.11., inkl. ueberfaellig
        var tr5 = (JsonObject)r["werte"]!["tr5"]!;
        Assert.Equal(7L, (long)tr5["vorrat_total"]!);
        Assert.Null(tr5["ziel"]);                        // Tagesmenge nur SEH
        Assert.Equal(0L, (long)r["werte"]!["dw"]!["vorrat_total"]!);   // DW1 gehoert zu keiner Abteilung
        Assert.Equal(7, (int)r["detail"]!["SEH"]!["auftraege"]!);
    }

    [Fact]
    public void Compute_AlterDpcOhneVorratsmodus_Fehler()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 9), Rows("""
            [ {"Typ":"FE","Aufnr":"1","Dispo":"SE1","Matnr":"B1","Datum":"20261110","Menge":"5"} ]
            """)));
        Assert.Contains("T76K912860", ex.Message);
    }
}
