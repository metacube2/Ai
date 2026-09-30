using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class SapGatewayHrKpiReaderTests
{
    [Fact]
    public void ParseRows_Liest_Die_Felder_Von_HrKpiSet()
    {
        const string json = """
            {"d":{"results":[
              {"__metadata":{"type":"x"},"Pernr":"00001001","Gjahr":"2026","Monat":"09","Bukrs":"1100",
               "Werks":"CH01","Btrtl":"0001","Persg":"1","Persk":"15","Teilk":"X","Empct":"80.00",
               "Gesch":"2","Plans":"50000123","Stell":"50000999","NbuTage":"1.50","BuTage":"0.00","Abkrs":"01"},
              {"Pernr":"","Gjahr":"2026","Monat":"09"}
            ]}}
            """;

        var row = Assert.Single(SapGatewayHrKpiReader.ParseRows(json));

        Assert.Equal("00001001", row.Personalnummer);
        Assert.Equal(80m, row.Beschaeftigungsgrad);
        Assert.Equal(1.5m, row.NbuTage);
        Assert.Equal("X", row.Teilzeitkennzeichen);
        Assert.Equal("15", row.Mitarbeiterkreis);
    }

    [Fact]
    public void BuildPageUrl_Filtert_Je_Feld_Und_Blaettert()
    {
        var url = SapGatewayHrKpiReader.BuildPageUrl("https://sap/odata/", 2026, 9, 1000);

        Assert.StartsWith("https://sap/odata/HrKpiSet?$format=json&$top=1000&$skip=1000&$filter=", url);
        Assert.Contains(Uri.EscapeDataString("Gjahr eq '2026' and Monat eq '09'"), url);
    }

    [Fact]
    public void Tagesabruf_Laeuft_Einmal_Je_Tag_Ab_Fuenf_Uhr()
    {
        var morning = new DateTime(2026, 10, 1, 6, 0, 0);
        var yesterday = new DateTime(2026, 9, 30, 7, 0, 0);

        Assert.False(HrKpiSapRefreshService.IsDue(new DateTime(2026, 10, 1, 4, 59, 0), yesterday, default));
        Assert.True(HrKpiSapRefreshService.IsDue(morning, yesterday, default));
        Assert.True(HrKpiSapRefreshService.IsDue(morning, null, default));
        // Heute schon versucht, etwa nach einem 404: nicht noch einmal.
        Assert.False(HrKpiSapRefreshService.IsDue(morning, yesterday, DateOnly.FromDateTime(morning)));
        // Datei heute nach fuenf Uhr geschrieben: fertig.
        Assert.False(HrKpiSapRefreshService.IsDue(morning, new DateTime(2026, 10, 1, 5, 30, 0), default));
    }
}
