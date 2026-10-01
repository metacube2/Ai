using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class SapGatewayHrAbsenceReaderTests
{
    [Fact]
    public void ParseRows_Liest_Faelle_Von_HrAbsenzSet()
    {
        const string json = """
            {"d":{"results":[
              {"__metadata":{"type":"x"},"Pernr":"00001001","Gjahr":"2026","Awart":"0260","Begda":"20260928",
               "Endda":"20261002","Seqnr":"000","Abwtg":"5.00","Stdaz":"40.00","Kaltg":"5.00"},
              {"Pernr":"00001002","Awart":"0100","Begda":"kaputt","Endda":"20261002"},
              {"Pernr":"","Awart":"0100","Begda":"20260101","Endda":"20260101"}
            ]}}
            """;

        var row = Assert.Single(SapGatewayHrAbsenceReader.ParseRows(json));

        Assert.Equal("00001001", row.Personalnummer);
        Assert.Equal("0260", row.Abwesenheitsart);
        Assert.Equal(new DateTime(2026, 9, 28), row.Von);
        Assert.Equal(new DateTime(2026, 10, 2), row.Bis);
        Assert.Equal(40m, row.Stunden);
        Assert.Equal(5m, row.Abwesenheitstage);
    }

    [Fact]
    public void BuildPageUrl_Filtert_Nach_Jahr_Und_Blaettert()
    {
        var url = SapGatewayHrAbsenceReader.BuildPageUrl("http://sap/srv/", 2026, 1000);

        Assert.StartsWith("http://sap/srv/HrAbsenzSet?$format=json&$top=1000&$skip=1000&$filter=", url);
        Assert.Contains(Uri.EscapeDataString("Gjahr eq '2026'"), url);
    }

    [Fact]
    public void Deduplicate_Behaelt_Einen_Fall_Ueber_Den_Jahreswechsel()
    {
        // Ein Fall vom 29.12. bis 05.01. kommt in den Abrufen 2025 und 2026.
        var fall = new HrAbsenceSapCase("00001001", "0260", new(2025, 12, 29), new(2026, 1, 5), "000", 5m, 40m, 8m);

        var cases = SapGatewayHrAbsenceReader.Deduplicate([fall, fall with { }]);

        Assert.Single(cases);
    }

    [Fact]
    public void ParseSapDate_Kennt_Das_Offene_Ende()
    {
        Assert.Equal(new DateTime(9999, 12, 31), SapGatewayHrAbsenceReader.ParseSapDate("99991231"));
        Assert.Null(SapGatewayHrAbsenceReader.ParseSapDate("00000000"));
    }

    [Fact]
    public void ProrateHours_Teilt_Einen_Fall_Ueber_Das_Monatsende_Nach_Arbeitstagen()
    {
        // Mo 28.09. bis Fr 02.10.2026, 5 Arbeitstage, 40 h: im September liegen 3 davon.
        var september = HrKpiDashboardBuilder.ProrateHours(
            40m, 8m, new(2026, 9, 28), new(2026, 10, 2), new(2026, 9, 28), new(2026, 9, 30));

        Assert.Equal(24m, september);
    }

    [Fact]
    public void ProrateHours_Zaehlt_Beim_Offenen_Ende_Je_Arbeitstag()
    {
        // Langzeitkrank ohne Ende: Mo 05.10. bis Fr 09.10.2026 im Zeitraum, je 8,1 h (Kader).
        var stunden = HrKpiDashboardBuilder.ProrateHours(
            0m, 8.1m, new(2026, 9, 1), new(9999, 12, 31), new(2026, 10, 5), new(2026, 10, 9));

        Assert.Equal(40.5m, stunden);
    }
}
