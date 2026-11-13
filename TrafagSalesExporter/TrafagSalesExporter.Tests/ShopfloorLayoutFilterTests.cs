using System.Text.Json;
using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>P76-Befund 13.11.2026: Filter der Standardlayouts von ZC22 (/ P00 TX) und ZC27 (/NIN) im Cockpit nachbilden, schaltbar.</summary>
public class ShopfloorLayoutFilterTests
{
    private static List<JsonElement> Rows(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static readonly ShopfloorLayoutFilter Status = new(["FREI DRUC RÜCK VOKL*"], []);
    private static readonly ShopfloorLayoutFilter StatusUndArbpl = new(["FREI DRUC RÜCK VOKL*"], ["M*"]);

    [Theory]
    [InlineData("FREI DRUC RÜCK VOKL MACM NMVP", "FREI DRUC RÜCK VOKL*", true)]
    [InlineData("FREI DRUC RÜCK VOKL", "FREI DRUC RÜCK VOKL*", true)]
    [InlineData("FREI DRUC TRÜC VOKL MACM", "FREI DRUC RÜCK VOKL*", false)]   // teilrueckgemeldet bleibt
    [InlineData("FREI RÜCK VOKL", "FREI DRUC RÜCK VOKL*", false)]             // Muster ist ein Anfang, nicht „enthaelt RÜCK“
    [InlineData("MLE02", "M*", true)]
    [InlineData("mle08", "M*", true)]                                           // CP ohne Gross-/Kleinschreibung
    [InlineData("EDF72", "M*", false)]
    [InlineData("C30115", "C*", true)]
    [InlineData("000000000000041019", "C*", false)]
    [InlineData("MLE02", "MLE+2", true)]
    public void Passt_Wie_Abap_CP(string wert, string muster, bool erwartet)
        => Assert.Equal(erwartet, ShopfloorLayoutFilter.Passt(wert, muster));

    [Fact]
    public void Muster_Trennt_Nur_An_Komma_Und_Semikolon()
        => Assert.Equal(["FREI DRUC RÜCK VOKL*", "M*"], ShopfloorLayoutFilter.Muster(" FREI DRUC RÜCK VOKL* ; M* "));

    [Fact]
    public void Vorrat_Blendet_Rueckgemeldete_Auftraege_Aus_Und_Zaehlt_Sie()
    {
        var rows = Rows("""
            [
              {"Typ":"VF","Aufnr":"1209407","Dispo":"SE1","Matnr":"B54670","Datum":"20251128","Menge":"20","Sttxt":"FREI DRUC RÜCK VOKL MACM"},
              {"Typ":"VF","Aufnr":"1267312","Dispo":"SE2","Matnr":"B54452","Datum":"20261110","Menge":"24","Sttxt":"FREI DRUC RÜCK VOKL"},
              {"Typ":"VF","Aufnr":"1270000","Dispo":"SE1","Matnr":"B54670","Datum":"20261112","Menge":"33","Sttxt":"FREI DRUC TRÜC VOKL"},
              {"Typ":"VP","Aufnr":"0000123","Dispo":"SL5","Matnr":"B64997","Datum":"20261114","Menge":"30","Sttxt":""}
            ]
            """);
        var ohne = ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 13), rows);
        Assert.Equal(107L, (long)ohne["werte"]!["seh"]!["vorrat_total"]!);
        var mit = ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 13), rows, layout: Status);
        Assert.Equal(63L, (long)mit["werte"]!["seh"]!["vorrat_total"]!);   // 107 - 44, wie SEH bei Fabio (156'723 gegen 156'679)
        Assert.Equal(63L, (long)mit["werte"]!["seh"]!["offen_kw"]!);
        Assert.Equal(2, (int)mit["detail"]!["SEH"]!["layout_ausgeblendet"]!["auftraege"]!);
        Assert.Equal(44L, (long)mit["detail"]!["SEH"]!["layout_ausgeblendet"]!["menge"]!);
    }

    [Fact]
    public void Verzug_Mit_Statusfilter_Und_Arbeitsplatz()
    {
        var rows = Rows("""
            [
              {"Typ":"VF","Aufnr":"1","Dispo":"EK1","Matnr":"63526","Datum":"20260910","Menge":"239","Sttxt":"FREI DRUC RÜCK VOKL","Arbpl":"EDF72","Avo":"X"},
              {"Typ":"VF","Aufnr":"2","Dispo":"EK1","Matnr":"63527","Datum":"20261105","Menge":"180","Sttxt":"FREI DRUC TRÜC VOKL","Arbpl":"MLE02","Avo":"X"},
              {"Typ":"VF","Aufnr":"3","Dispo":"EK1","Matnr":"63528","Datum":"20261111","Menge":"304","Sttxt":"FREI DRUC VOKL","Arbpl":"EDF72","Avo":"X"},
              {"Typ":"VP","Aufnr":"4","Dispo":"EK1","Matnr":"63529","Datum":"20261111","Menge":"5","Sttxt":"","Arbpl":""}
            ]
            """);
        var ab = new DateOnly(2026, 1, 1);
        Assert.Equal(728L, (long)ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 13), rows, [], ab)["werte"]!["ppd"]!["verzug_tr5"]!);
        var status = ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 13), rows, [], ab, Status);
        Assert.Equal(489L, (long)status["werte"]!["ppd"]!["verzug_tr5"]!);
        var beide = ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 13), rows, [], ab, StatusUndArbpl);
        Assert.Equal(309L, (long)beide["werte"]!["ppd"]!["verzug_tr5"]!);   // Planauftrag ohne Arbeitsplatz bleibt
        Assert.Equal(419L, (long)beide["detail"]!["TR5"]!["layout_ausgeblendet"]!["menge"]!);
    }

    [Fact]
    public void Aktiver_Filter_Ohne_Sttxt_Aus_SAP_Gibt_Fehler_Statt_Falscher_Zahl()
    {
        var rows = Rows("""[ {"Typ":"VF","Aufnr":"1","Dispo":"EL1","Matnr":"64708","Datum":"20261110","Menge":"30"} ]""");
        var ex = Assert.Throws<InvalidOperationException>(() => ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 13), rows, layout: Status));
        Assert.Contains("Sttxt", ex.Message);
        Assert.Throws<InvalidOperationException>(() => ShopfloorSapVerzug.Compute(new DateOnly(2026, 11, 13), rows, [], null,
            new ShopfloorLayoutFilter([], ["M*"])));
        // Arbpl ohne Echo Avo = X (Transport da, aber ohne Vorgangsdaten gelesen): ebenfalls Fehler
        var ohneEcho = Rows("""[ {"Typ":"VF","Aufnr":"1","Dispo":"EL1","Matnr":"64708","Datum":"20261110","Menge":"30","Sttxt":"FREI","Arbpl":"","Avo":""} ]""");
        Assert.Throws<InvalidOperationException>(() => ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 13), ohneEcho, layout: StatusUndArbpl));
        Assert.Equal(30L, (long)ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 13), ohneEcho, layout: Status)["werte"]!["tx"]!["vorrat_total"]!);
        // Filter aus: rechnet wie bisher
        Assert.Equal(30L, (long)ShopfloorSapVorrat.Compute(new DateOnly(2026, 11, 13), rows)["werte"]!["tx"]!["vorrat_total"]!);
    }

    [Fact]
    public void Arbeitsplatz_Nur_Mit_Avo_Angefordert()
    {
        Assert.Equal("Werks eq '1100' and Datum eq '20261113' and Typ eq 'VR'", ShopfloorSapVorrat.Filter("1100", new DateOnly(2026, 11, 13)));
        Assert.Equal("Werks eq '1100' and Datum eq '20261113' and Typ eq 'VR' and Avo eq 'X'",
            ShopfloorSapVorrat.Filter("1100", new DateOnly(2026, 11, 13), StatusUndArbpl.Vorgangsdaten));
        Assert.Equal("Werks eq '1100' and Datum eq '20261113' and Typ eq 'VZ'",
            ShopfloorSapVerzug.Filter("1100", new DateOnly(2026, 11, 13), Status.Vorgangsdaten));
    }

    [Fact]
    public async Task Mengen_Schicken_Den_Materialausschluss_Nur_Wenn_Gesetzt()
    {
        var filter = new List<string>();
        Task<List<JsonElement>> Read(string set, string f)
        {
            if (set == ShopfloorSapMengen.RmSet)
                filter.Add(f);
            return Task.FromResult(new List<JsonElement>());
        }
        await ShopfloorSapMengen.ComputeAsync(new DateOnly(2026, 11, 5), "1100", Read);
        Assert.All(filter, f => Assert.DoesNotContain("Matex", f));
        filter.Clear();
        var r = await ShopfloorSapMengen.ComputeAsync(new DateOnly(2026, 11, 5), "1100", Read, " E*, D*,F*,C* ");
        Assert.All(filter, f => Assert.EndsWith(" and Matex eq 'E*,D*,F*,C*'", f));
        Assert.Equal(ShopfloorSapMengen.RmGroups.Length, filter.Count);
    }

    [Fact]
    public void Schalter_Stehen_Aus_Bis_Der_Transport_In_P76_Ist()
    {
        var options = new ShopfloorOptions();
        Assert.False(options.Zc22Layout().Aktiv);
        Assert.Equal("", options.RmMaterialAusschluss);
        var appsettings = JsonNode.Parse(File.ReadAllText(Path.Combine(ShopfloorTestEnv.ProjectRoot, "appsettings.json")))!["Shopfloor"]!;
        Assert.Equal("", (string)appsettings["Zc22StatusAusschluss"]!);
        Assert.Equal("", (string)appsettings["Zc22ArbplAusschluss"]!);
        Assert.Equal("", (string)appsettings["RmMaterialAusschluss"]!);
    }
}
