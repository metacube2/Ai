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
    public void Grenzen_RollendWieFabiosKontrolle13November()
    {
        // Paket Fabio 13.11.2026 Punkt 1: rollend ab dem Abfragetag (SEH 1 Mte bis 13.12., 2 Mte bis 13.01., 3 Mte bis 13.02.);
        // die Spezifikation vom 07.11. (bis Monatsende) war falsch.
        var g = ShopfloorSapVorrat.Grenzen(new DateOnly(2026, 11, 13));
        Assert.Equal(new DateOnly(2026, 12, 13), g.M1);
        Assert.Equal(new DateOnly(2027, 1, 13), g.M2);
        Assert.Equal(new DateOnly(2027, 2, 13), g.M3);
        Assert.Equal(new DateOnly(2026, 11, 15), g.KwEnde);
        // DW in Wochen: + 4 / 8 / 12 Wochen
        var w = ShopfloorSapVorrat.Grenzen(new DateOnly(2026, 11, 13), wochen: true);
        Assert.Equal(new DateOnly(2026, 12, 11), w.M1);
        Assert.Equal(new DateOnly(2027, 1, 8), w.M2);
        Assert.Equal(new DateOnly(2027, 2, 5), w.M3);
        Assert.Equal(new DateOnly(2026, 11, 15), w.KwEnde);
        // Monatsende: 31.01. + 1 Monat = 28.02.
        Assert.Equal(new DateOnly(2027, 2, 28), ShopfloorSapVorrat.Grenzen(new DateOnly(2027, 1, 31)).M1);
        // Sonntag selbst: KW endet am selben Tag; Montag: bis zum folgenden Sonntag
        Assert.Equal(new DateOnly(2026, 11, 8), ShopfloorSapVorrat.Grenzen(new DateOnly(2026, 11, 8)).KwEnde);
        Assert.Equal(new DateOnly(2026, 11, 15), ShopfloorSapVorrat.Grenzen(new DateOnly(2026, 11, 9)).KwEnde);
    }

    [Fact]
    public void Compute_KumuliertJeAbteilungRollend()
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
        Assert.Equal(6206L, (long)seh["vorrat_3m"]!);   // bis 09.02.2027 (rollend), 01.02.2027 gehoert dazu
        Assert.Equal(206L, (long)seh["vorrat_2m"]!);    // bis 09.01.2027
        Assert.Equal(106L, (long)seh["vorrat_1m"]!);    // bis 09.12.
        Assert.Equal(96L, (long)seh["offen_kw"]!);      // bis So 15.11., inkl. ueberfaellig
        Assert.Equal(36L, (long)seh["ziel"]!);          // bis Abfragetag 09.11., inkl. ueberfaellig
        var tr5 = (JsonObject)r["werte"]!["tr5"]!;
        Assert.Equal(7L, (long)tr5["vorrat_total"]!);
        Assert.Null(tr5["ziel"]);                        // Tagesmenge nur SEH
        Assert.Equal(999L, (long)r["werte"]!["dw"]!["vorrat_total"]!);   // seit 13.11.: D* ohne Ausnahmen, ohne Materialfilter gezaehlt
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

    [Fact]
    public void Materialfilter_TR5_Und_DW_Wochenfenster()
    {
        // Paket Fabio 13.11. Punkte 1 bis 3: TR5 und DW nur Material 1-99999, DW-Fenster in Wochen (D + 28 / 56 / 84 Tage)
        var r = ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 13), Rows("""
            [
              {"Typ":"VF","Aufnr":"1","Dispo":"EK4","Matnr":"63526","Datum":"20261120","Menge":"10"},
              {"Typ":"VF","Aufnr":"2","Dispo":"EK4","Matnr":"P63526","Datum":"20261120","Menge":"500"},
              {"Typ":"VF","Aufnr":"3","Dispo":"EKB","Matnr":"H01234","Datum":"20261120","Menge":"700"},
              {"Typ":"VF","Aufnr":"4","Dispo":"DS1","Matnr":"C36554","Datum":"20260918","Menge":"60"},
              {"Typ":"VF","Aufnr":"5","Dispo":"DEF","Matnr":"66068","Datum":"20261211","Menge":"5"},
              {"Typ":"VF","Aufnr":"6","Dispo":"D30","Matnr":"41000","Datum":"20261212","Menge":"7"},
              {"Typ":"VF","Aufnr":"7","Dispo":"SE1","Matnr":"B54670","Datum":"20261120","Menge":"3"}
            ]
            """), ShopfloorSapVerzug.StandardMaterialFilter);
        Assert.Equal(10L, (long)r["werte"]!["tr5"]!["vorrat_total"]!);
        var dw = r["werte"]!["dw"]!;
        Assert.Equal(12L, (long)dw["vorrat_total"]!);
        Assert.Equal(5L, (long)dw["vorrat_1m"]!);    // bis 11.12. (+ 4 Wochen)
        Assert.Equal(12L, (long)dw["vorrat_2m"]!);
        Assert.Equal(3L, (long)r["werte"]!["seh"]!["vorrat_total"]!);   // SEH ohne Materialfilter
        Assert.Equal("2026-12-11", (string)r["detail"]!["DW"]!["grenzen"]!["1m"]!);
        Assert.True((bool)r["detail"]!["TR5"]!["materialfilter"]!);
        Assert.False((bool)r["detail"]!["SEH"]!["materialfilter"]!);
    }

    [Fact]
    public void Plan5_KumuliertMitUeberfaelligenUndHeutigen()
    {
        Assert.Equal(new DateOnly(2026, 11, 16), ShopfloorSapVorrat.Plan5Ende(new DateOnly(2026, 11, 9)));   // Mo -> Mo
        Assert.Equal(new DateOnly(2026, 11, 13), ShopfloorSapVorrat.Plan5Ende(new DateOnly(2026, 11, 7)));   // Sa -> Fr
        var r = ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 9), Rows("""
            [
              {"Typ":"VF","Aufnr":"1","Dispo":"EL1","Matnr":"1","Datum":"20261105","Menge":"100"},
              {"Typ":"VF","Aufnr":"2","Dispo":"EL1","Matnr":"1","Datum":"20261109","Menge":"10"},
              {"Typ":"VF","Aufnr":"3","Dispo":"EL1","Matnr":"1","Datum":"20261110","Menge":"20"},
              {"Typ":"VF","Aufnr":"4","Dispo":"EL1","Matnr":"1","Datum":"20261116","Menge":"30"},
              {"Typ":"VF","Aufnr":"5","Dispo":"EL1","Matnr":"1","Datum":"20261117","Menge":"40"},
              {"Typ":"VF","Aufnr":"6","Dispo":"SE1","Matnr":"1","Datum":"20261110","Menge":"5"}
            ]
            """));
        Assert.Equal(160L, (long)r["werte"]!["tx"]!["plan5"]!);   // bis 16.11. kumuliert, mit ueberfaelligen und heutigen (Paket Fabio 13.11. Punkt 5)
        Assert.Null(r["werte"]!["seh"]!["plan5"]);
    }
}
