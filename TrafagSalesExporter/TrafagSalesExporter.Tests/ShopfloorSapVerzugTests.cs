using System.Text.Json;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public class ShopfloorSapVerzugTests
{
    private static List<JsonElement> Rows(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    [Theory]
    [InlineData("SE1", "SEH")]
    [InlineData("SK3", "SEH")]
    [InlineData("EK1", "TR5")]
    [InlineData("EL9", "TX")]
    [InlineData("DW2", "DW")]
    [InlineData("DS1", "DW")]
    [InlineData("DB1", "DW")]
    [InlineData("D10", "DW")]
    [InlineData("DW1", "DW")]   // seit Paket Fabio 13.11.: D* ohne Ausnahmen (DW1 faellt ueber Material 1-99999 weg)
    [InlineData("DL6", "DW")]
    [InlineData("001", null)]
    public void Abteilung_Wie_Fabios_Disponenten(string dispo, string? erwartet)
        => Assert.Equal(erwartet, ShopfloorSapVerzug.Abteilung(dispo));

    [Theory]
    [InlineData("000000000000041019", true)]
    [InlineData("41019", true)]
    [InlineData("000000000000099999", true)]
    [InlineData("000000000000100000", false)]
    [InlineData("000000000000000000", false)]
    [InlineData("B62622", false)]   // alphanumerisch (SEH-Material in T76): nicht im Bereich 1-99999
    public void Material_Nur_Numerisch_Von_1_Bis_99999(string matnr, bool erwartet)
        => Assert.Equal(erwartet, ShopfloorSapVerzug.MaterialImBereich(matnr));

    [Fact]
    public void Filter_Fragt_Den_Verzugsmodus_Zum_Abfragetag_Ab()
        => Assert.Equal("Werks eq '1100' and Datum eq '20261102' and Typ eq 'VZ'", ShopfloorSapVerzug.Filter("1100", new DateOnly(2026, 11, 2)));

    [Fact]
    public void Summiert_Positiv_Je_Abteilung_Und_Laesst_Fremde_Weg()
    {
        var r = ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 2), Rows("""
            [{"Typ":"VF","Dispo":"EK1","Matnr":"000000000000041019","Menge":10},
             {"Typ":"VP","Dispo":"EK4","Matnr":"000000000000041020","Menge":"5"},
             {"Typ":"VF","Dispo":"DS1","Matnr":"000000000000070000","Menge":7},
             {"Typ":"VF","Dispo":"DW1","Matnr":"000000000000070001","Menge":100},
             {"Typ":"VF","Dispo":"SE1","Matnr":"B62622","Menge":60},
             {"Typ":"VP","Dispo":"EL2","Matnr":"000000000000200000","Menge":3}]
            """));

        Assert.Equal("2026-11-02", (string)r["day"]!);
        var ppd = r["werte"]!["ppd"]!;
        Assert.Equal(60L, (long)ppd["verzug_seh"]!);   // SEH ohne Materialfilter (Paket Fabio 13.11.)
        Assert.Equal(15L, (long)ppd["verzug_tr5"]!);
        Assert.Equal(3L, (long)ppd["verzug_tx"]!);    // TX ohne Materialfilter
        Assert.Equal(107L, (long)ppd["verzug_dw"]!);   // DS1 und DW1 mit numerischem Material zaehlen (D* ohne Ausnahmen)
        Assert.Equal(2, (int)r["detail"]!["TR5"]!["auftraege"]!);
    }

    [Fact]
    public void Alter_DPC_Ohne_Verzugsmodus_Gibt_Fehler_Statt_Falscher_Zahl()
    {
        // Ein System ohne T76K912797 ignoriert Typ eq 'VZ' und liefert Forecast-Zeilen (FE/PA) der naechsten 60 Tage.
        var ex = Assert.Throws<InvalidOperationException>(() => ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 2), Rows("""
            [{"Typ":"FE","Dispo":"EK1","Matnr":"000000000000041019","Menge":10}]
            """)));
        Assert.Contains("T76K912797", ex.Message);
    }

    [Fact]
    public void Materialfilter_Je_Abteilung_Schaltbar()
    {
        var rows = Rows("""
            [
              {"Typ":"VF","Aufnr":"1","Dispo":"SE1","Matnr":"B54670","Datum":"20261101","Menge":"6"},
              {"Typ":"VF","Aufnr":"2","Dispo":"DW2","Matnr":"C12345","Datum":"20261101","Menge":"9"},
              {"Typ":"VF","Aufnr":"3","Dispo":"DW2","Matnr":"40123","Datum":"20261101","Menge":"4"}
            ]
            """);
        // Vorgabe seit Paket Fabio 13.11.: Materialfilter nur TR5 und DW
        var standard = ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 2), rows);
        Assert.Equal(6L, (long)standard["werte"]!["ppd"]!["verzug_seh"]!);
        Assert.Equal(4L, (long)standard["werte"]!["ppd"]!["verzug_dw"]!);
        var alle = ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 2), rows, ["SEH", "TR5", "TX", "DW"]);
        Assert.Equal(0L, (long)alle["werte"]!["ppd"]!["verzug_seh"]!);
        var ohne = ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 2), rows, []);
        Assert.Equal(13L, (long)ohne["werte"]!["ppd"]!["verzug_dw"]!);
    }

    [Fact]
    public void Untergrenze_Laesst_Aeltere_Auftraege_Weg()
    {
        // Paket Fabio 13.11. Punkt 6: Eckendtermin 01.01.2026 bis Vortag
        var rows = Rows("""
            [
              {"Typ":"VF","Aufnr":"1","Dispo":"EL1","Matnr":"64708","Datum":"20251231","Menge":"50"},
              {"Typ":"VF","Aufnr":"2","Dispo":"EL1","Matnr":"64708","Datum":"20260101","Menge":"30"},
              {"Typ":"VF","Aufnr":"3","Dispo":"EL1","Matnr":"64708","Datum":"20261112","Menge":"7"}
            ]
            """);
        var mit = ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 13), rows, [], new DateOnly(2026, 1, 1));
        Assert.Equal(37L, (long)mit["werte"]!["ppd"]!["verzug_tx"]!);
        Assert.Equal(1, (int)mit["vor_untergrenze"]!);
        Assert.Equal("2026-01-01", (string)mit["ab"]!);
        var ohne = ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 13), rows, []);
        Assert.Equal(87L, (long)ohne["werte"]!["ppd"]!["verzug_tx"]!);
    }
}
