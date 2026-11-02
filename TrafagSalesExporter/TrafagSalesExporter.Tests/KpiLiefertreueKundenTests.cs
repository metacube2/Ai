using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

/// <summary>Rubrik Kunden der Liefertreue (Spezifikation Fabio 2026-11-02, Tab „Erweiterung Kundensicht“).</summary>
public class KpiLiefertreueKundenTests
{
    private static LtOrderRow Pos(string so, string item, string cmtd, string status, string cust, string reqd = "", string product = "M1")
        => new(so, item, "20260105", cmtd, cmtd[..6], (reqd.Length > 0 ? reqd : cmtd)[..6], reqd.Length > 0 ? reqd : cmtd, status, cust, product, "8736", 100, double.NaN);

    private static readonly LtOrderRow[] Rows =
    [
        Pos("1", "10", "20260302", "C", "682"),                        // Hitachi CH, puenktlich
        Pos("1", "20", "20260310", "A", "682"),                        // offen: Auftrag 1 nicht OTIF, faellig im Maerz
        Pos("2", "10", "20260115", "C", "9204", reqd: "20260105"),     // Hitachi SE, OTD ok, Wunsch 8 AT zu frueh verlangt
        Pos("3", "10", "20260320", "C", "777"),                        // zu frueh geliefert
        Pos("4", "10", "20250120", "C", "555", reqd: "20260210"),      // bestaetigt im Vorjahr, Wunsch 2026: nur OTP
        Pos("5", "10", "20260302", "C", "302906"),                     // ausgeschlossener Kunde
        Pos("6", "10", "20260302", "C", "682", product: "H70201"),     // Zertifikat
        Pos("7", "10", "20260402", "C", "682")                         // nach dem Berichtsmonat
    ];

    private static readonly LtDeliveryRow[] Deliveries =
    [
        new("20260302", "1", "10"),
        new("20260115", "2", "10"),
        new("20260306", "3", "10"),
        new("20250120", "4", "10"),
        new("20260302", "5", "10"),
        new("20260302", "6", "10")
    ];

    private static JsonObject Lt() => KpiLiefertreue.Compute(Rows, Deliveries, "202603", new DateOnly(2026, 4, 2),
        [new LtExclusion(1, "Kunde", "302906", "Trafag Spol. s.r.o. (CZ)", "konzerninterne Lieferungen")], ["H70201"], []);

    private static List<string[]> Kunden(JsonObject lt) => lt["kunden"]!.AsArray().Select(x => x!.GetValue<string>().Split(';')).ToList();

    [Fact]
    public void Pruefregel_1_Summe_Ueber_Alle_Kunden_Ergibt_Die_Monatswerte()
    {
        var lt = Lt();
        var k = Kunden(lt);
        int S(string month, int idx) => k.Where(x => x[1] == month).Sum(x => int.Parse(x[idx]));
        // otd: Monat;n;streng;Tol;Auftraege;OTIF streng;OTIF Tol;zu frueh;ohne WA  -> kunden: Kunde;Monat;n;s;t;e5;on;os;ot;pn;ps;pt
        foreach (var o in lt["otd"]!.AsArray().Select(x => x!.GetValue<string>().Split(';')))
        {
            var m = o[0];
            Assert.Equal((o[1], o[2], o[3], o[7]), (S(m, 2).ToString(), S(m, 3).ToString(), S(m, 4).ToString(), S(m, 5).ToString()));
            Assert.Equal((o[4], o[5], o[6]), (S(m, 6).ToString(), S(m, 7).ToString(), S(m, 8).ToString()));
        }
        foreach (var p in lt["otp"]!.AsArray().Select(x => x!.GetValue<string>().Split(';')))
            Assert.Equal((p[1], p[2], p[3]), (S(p[0], 9).ToString(), S(p[0], 10).ToString(), S(p[0], 11).ToString()));
    }

    [Fact]
    public void Otif_Gehoert_Dem_Auftraggeber_Im_Monat_Des_Spaetesten_Termins()
    {
        var k = Kunden(Lt()).ToDictionary(x => x[0] + ";" + x[1], x => x);
        // Auftrag 1 (682): Positionen 02.03. und 10.03., offen -> 1 Auftrag im Maerz, nicht komplett
        Assert.Equal(("2", "1", "0", "0"), (k["682;202603"][2], k["682;202603"][6], k["682;202603"][7], k["682;202603"][8]));
        // Kunde nur mit Wunschtermin im Jahr (bestaetigt im Vorjahr) erscheint mit OTP, ohne OTD
        Assert.Equal(("0", "1"), (k["555;202602"][2], k["555;202602"][9]));
        // ausgeschlossene Kunden und Zertifikate fehlen
        Assert.DoesNotContain(k.Keys, x => x.StartsWith("302906;", StringComparison.Ordinal));
        Assert.Equal("2", k["682;202603"][2]);
    }

    [Theory]
    [InlineData("hitachi", "682", "Hitachi Energy Switzerland Ltd", true)]
    [InlineData("HITACHI", "682", "Hitachi Energy Switzerland Ltd", true)]
    [InlineData("68", "682", "x", true)]                 // Anfang der Nummer
    [InlineData("0682", "682", "x", true)]               // fuehrende Null egal
    [InlineData("82", "682", "x", false)]                // Ziffern nur am Anfang
    [InlineData("682, 9204", "9204", "x", true)]         // mehrere Begriffe
    [InlineData("", "1", "x", true)]                     // ohne Suche alle
    [InlineData("Sweden", "9204", "Hitachi Energy Sweden AB", true)]
    [InlineData("Sweden", "682", "Hitachi Energy Switzerland Ltd", false)]
    public void Suche_Wie_Auf_Der_Seite(string q, string nr, string name, bool expected)
        => Assert.Equal(expected, KpiLiefertreue.MatchCustomer(q, nr, name));

    [Fact]
    public void Excel_Hat_Kunden_Ytd_Und_Kunden_Pro_Monat()
    {
        var lt = Lt();
        ((JsonObject)lt["names"]!)["c"] = new JsonObject { ["682"] = "Hitachi Energy Switzerland Ltd", ["9204"] = "Hitachi Energy Sweden AB", ["777"] = "Andere AG" };
        using var wb = new ClosedXML.Excel.XLWorkbook(new MemoryStream(KpiLiefertreue.ExportCustomersXlsx(lt, "hitachi")!));
        var ytd = wb.Worksheet("Kunden YTD");
        Assert.Equal(3, ytd.LastRowUsed()!.RowNumber());                 // Kopf + 682 + 9204
        Assert.Equal(682, ytd.Cell(2, 1).GetValue<int>());                // nach Positionen absteigend
        Assert.Equal(2, ytd.Cell(2, 3).GetValue<int>());
        Assert.Equal(0.5, ytd.Cell(2, 5).GetValue<double>(), 3);         // OTD 1 von 2
        Assert.True(wb.Worksheets.Contains("Kunden pro Monat"));
        Assert.Null(KpiLiefertreue.ExportCustomersXlsx(new JsonObject { ["cut"] = "202609", ["stand"] = "2026-10-13" }));
    }

    [Fact]
    public void Seite_Bekommt_Kundenblock_Und_Alte_Staende_Null()
    {
        const string template = "<script>/*__CU__*/</script>";
        var html = KpiLiefertreue.BuildBoard(Lt(), template);
        Assert.Contains("const CU={\"m\":[\"Jan\",\"Feb\",\"Mär\"]", html);
        var alt = Lt();
        alt.Remove("kunden");
        Assert.Contains("const CU=null;", KpiLiefertreue.BuildBoard(alt, template));
    }
}
