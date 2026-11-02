using System.Text.Json;
using System.Text.Json.Nodes;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public class ShopfloorSapVerkaufTests
{
    [Theory]
    [InlineData(2026, 11, 1, "202610")]   // Monatserster: Vormonat
    [InlineData(2026, 11, 2, "202611")]
    [InlineData(2027, 1, 1, "202612")]    // Jahreswechsel
    public void Monat_Ist_Der_Monat_Des_Vortags(int y, int m, int d, string erwartet)
        => Assert.Equal(erwartet, ShopfloorSapVerkauf.ReportMonth(new DateOnly(y, m, d)));

    [Fact]
    public void Url_Fragt_Nur_Die_Kennzahlen_Mit_Verkaufsorganisation_Und_Monat_Ab()
    {
        var url = ShopfloorSapVerkauf.Url("http://travp762.sap.trafag.com:8000/", "100", "202610");
        Assert.StartsWith("http://travp762.sap.trafag.com:8000/sap/opu/odata/sap/C_SALESANALYTICSQRY_1_CDS/" +
                          "C_SALESANALYTICSQRY_1(P_DisplayCurrency='CHF',P_ExchangeRateType='M')/Results?sap-client=100&$format=json", url);
        Assert.Contains("$select=SlsVolumeNetAmtInDspCrcy,OpnOutbDelivsForInvcNetAmtInDC,OpenSalesOrdersNetAmountInDC,CustCrdtMemoNetAmtInDspCrcy", url);
        Assert.Contains("$filter=SalesOrganization%20eq%20'1100'%20and%20BillingDocDateYearMonth%20eq%20'202610'", url);
    }

    [Fact]
    public void Erwarteter_Umsatz_Ist_Umsatz_Plus_Offene_Lieferungen_Plus_Offene_Auftraege()
    {
        // Edm.Decimal kommt in OData V2 als Zeichenkette; zwei Zeilen werden summiert
        using var doc = JsonDocument.Parse("""
            [{"SlsVolumeNetAmtInDspCrcy":"8000000.50","OpnOutbDelivsForInvcNetAmtInDC":"500000.00","OpenSalesOrdersNetAmountInDC":"300000.25","CustCrdtMemoNetAmtInDspCrcy":"100000.00"},
             {"SlsVolumeNetAmtInDspCrcy":"27623.25","OpnOutbDelivsForInvcNetAmtInDC":"0","OpenSalesOrdersNetAmountInDC":"0","CustCrdtMemoNetAmtInDspCrcy":"10412.40"}]
            """);
        var r = ShopfloorSapVerkauf.Compute(new DateOnly(2026, 11, 1), doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList());

        Assert.Equal("2026-11-01", (string)r["day"]!);
        Assert.Equal("202610", (string)r["monat"]!);
        Assert.Equal(8827624.00m, (decimal)r["werte"]!["verkauf"]!["umsatz_plan"]!);
        Assert.Equal(110412.40m, (decimal)r["werte"]!["verkauf"]!["gutschriften"]!);
        Assert.Equal(8027623.75m, (decimal)r["detail"]!["umsatz"]!);
    }

    [Fact]
    public void Ohne_Zeile_Werden_Keine_Werte_Geschrieben()
    {
        var r = ShopfloorSapVerkauf.Compute(new DateOnly(2026, 11, 2), []);
        Assert.Empty(r["werte"]!.AsObject());
        var (items, _) = ShopfloorSapMengen.BuildItems(r, _ => null);
        Assert.Empty(items);
    }

    [Fact]
    public void Werte_Landen_Im_Verkauf_Am_Abfragetag_Manueller_Wert_Bleibt()
    {
        using var env = new ShopfloorTestEnv();
        // Fabio hat am Tag schon einen ZC23-Wert von Hand erfasst; Gutschriften sind leer
        env.Store.ApplyPush(new ShopfloorPush
        {
            Items = [new JsonObject { ["module"] = "verkauf", ["day"] = "2026-11-02", ["fields"] = new JsonObject { ["umsatz_plan"] = 7000000 } }]
        }, "Fabio");
        using var doc = JsonDocument.Parse("""
            [{"SlsVolumeNetAmtInDspCrcy":"6000000","OpnOutbDelivsForInvcNetAmtInDC":"400000","OpenSalesOrdersNetAmountInDC":"184145","CustCrdtMemoNetAmtInDspCrcy":"5000"}]
            """);
        var r = ShopfloorSapVerkauf.Compute(new DateOnly(2026, 11, 2), doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList());
        var (items, skipped) = ShopfloorSapMengen.BuildItems(r,
            module => env.Store.GetDaily(module, "2026-11-02", "2026-11-02")[module]?["2026-11-02"] as JsonObject);
        var res = env.Store.ApplyPush(new ShopfloorPush { Items = items }, "SAP");

        Assert.All(res, x => Assert.True((bool)x!["ok"]!));
        Assert.Equal(["verkauf.umsatz_plan"], skipped.Select(x => (string)x!));
        var day = env.Store.GetDaily("verkauf", "2026-11-02", "2026-11-02")["verkauf"]!["2026-11-02"]!;
        Assert.Equal(7000000, (double)day["data"]!["umsatz_plan"]!, 3);
        Assert.Equal(5000, (double)day["data"]!["gutschriften"]!, 3);
        Assert.Equal("SAP", (string)day["meta"]!["gutschriften"]!["src"]!);
    }
}
