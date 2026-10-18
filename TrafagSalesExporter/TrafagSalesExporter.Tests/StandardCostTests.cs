using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Tests fuer die Kostenbasis der Gruppenmarge: SAP-Standardpreise (CH/AT) und die
/// Alphaplan-Ableitung (DE). Der rote Faden aller Tests: `StandardCost` MUSS ein
/// Stueckpreis sein, weil die Margenlogik `Menge x StandardCost` rechnet.
/// </summary>
public class StandardCostTests
{
    private static Dictionary<string, object?> MbewRow(
        string matnr, string bwkey, string stprs, string peinh)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Matnr"] = matnr,
            ["Bwkey"] = bwkey,
            ["Stprs"] = stprs,
            ["Peinh"] = peinh
        };

    // ---------- SAP: MBEW-Zeilen lesen ----------

    [Fact]
    public void MapRow_UsesStandardPriceAsUnitCost_WhenPriceUnitIsOne()
    {
        var mapped = SapGatewayStandardCostReader.MapRow(MbewRow("B52825", "1100", "4.70", "1"));

        Assert.NotNull(mapped);
        Assert.Equal("B52825", mapped!.Value.Key.MaterialKey);
        Assert.Equal("1100", mapped.Value.Key.ValuationArea);
        Assert.Equal(4.70m, mapped.Value.Entry.UnitCost);
    }

    [Fact]
    public void MapRow_DividesByPriceUnit()
    {
        // STPRS gilt pro PEINH Stueck. Ohne Division waere die Kostenbasis
        // hier um Faktor 100 zu hoch.
        var mapped = SapGatewayStandardCostReader.MapRow(MbewRow("H90317", "1100", "1600.00", "100"));

        Assert.NotNull(mapped);
        Assert.Equal(16m, mapped!.Value.Entry.UnitCost);
    }

    [Fact]
    public void MapRow_TreatsMissingPriceUnitAsOne()
    {
        var mapped = SapGatewayStandardCostReader.MapRow(MbewRow("5523", "1200", "196.44", "0"));

        Assert.NotNull(mapped);
        Assert.Equal(196.44m, mapped!.Value.Entry.UnitCost);
    }

    [Fact]
    public void MapRow_NormalizesMaterialKey_SoLeadingZerosMatchTheSalesRows()
    {
        var mapped = SapGatewayStandardCostReader.MapRow(MbewRow("000000000000043125", "1100", "10.00", "1"));

        Assert.NotNull(mapped);
        Assert.Equal("43125", mapped!.Value.Key.MaterialKey);
    }

    [Fact]
    public void MapRow_SkipsMaterialsWithoutStandardPrice()
    {
        Assert.Null(SapGatewayStandardCostReader.MapRow(MbewRow("B52825", "1100", "0", "1")));
        Assert.Null(SapGatewayStandardCostReader.MapRow(MbewRow("", "1100", "5.00", "1")));
        Assert.Null(SapGatewayStandardCostReader.MapRow(MbewRow("B52825", "", "5.00", "1")));
    }

    // ---------- Zuordnung auf die Umsatzzeilen ----------

    [Fact]
    public void ResolveValuationArea_MapsSwitzerlandAndAustriaToTheirValuationAreas()
    {
        Assert.Equal("1100", StandardCostEnricher.ResolveValuationArea("CH"));
        Assert.Equal("1200", StandardCostEnricher.ResolveValuationArea("AT"));
        Assert.Null(StandardCostEnricher.ResolveValuationArea("DE"));
        Assert.Null(StandardCostEnricher.ResolveValuationArea(""));
    }

    [Fact]
    public void Apply_SetsUnitCostPerCountry_AndKeepsSwissAndAustrianPricesApart()
    {
        // Gleiches Material, unterschiedlicher Preis je Bewertungskreis. Genau dafuer
        // gehoert der Bewertungskreis in den Schluessel — sonst bekaeme die CH-Zeile
        // den oesterreichischen Preis.
        var costs = new Dictionary<StandardCostKey, StandardCostEntry>
        {
            [new StandardCostKey("43125", "1100")] = new(12.50m, string.Empty),
            [new StandardCostKey("43125", "1200")] = new(19.90m, string.Empty)
        };

        var records = new List<SalesRecord>
        {
            new() { Land = "CH", Material = "43125", CompanyCurrency = "CHF" },
            new() { Land = "AT", Material = "43125", CompanyCurrency = "EUR" }
        };

        var result = StandardCostEnricher.Apply(records, costs);

        Assert.Equal(2, result.Matched);
        Assert.Equal(0, result.Missing);
        Assert.Equal(12.50m, records[0].StandardCost);
        Assert.Equal("CHF", records[0].StandardCostCurrency);
        Assert.Equal(19.90m, records[1].StandardCost);
        Assert.Equal("EUR", records[1].StandardCostCurrency);
    }

    [Fact]
    public void Apply_LeavesRowsWithoutMatchUntouched_InsteadOfGuessing()
    {
        var costs = new Dictionary<StandardCostKey, StandardCostEntry>
        {
            [new StandardCostKey("43125", "1100")] = new(12.50m, string.Empty)
        };

        var records = new List<SalesRecord>
        {
            new() { Land = "CH", Material = "99999", CompanyCurrency = "CHF" },
            new() { Land = "DE", Material = "43125", CompanyCurrency = "EUR" }
        };

        var result = StandardCostEnricher.Apply(records, costs);

        Assert.Equal(0, result.Matched);
        Assert.Equal(2, result.Missing);
        Assert.All(records, record => Assert.Equal(0m, record.StandardCost));
    }

    [Fact]
    public void Apply_MatchesEvenWhenTheSalesRowCarriesLeadingZeros()
    {
        var costs = new Dictionary<StandardCostKey, StandardCostEntry>
        {
            [new StandardCostKey("43125", "1100")] = new(12.50m, string.Empty)
        };

        var records = new List<SalesRecord>
        {
            new() { Land = "CH", Material = "000043125", CompanyCurrency = "CHF" }
        };

        var result = StandardCostEnricher.Apply(records, costs);

        Assert.Equal(1, result.Matched);
        Assert.Equal(12.50m, records[0].StandardCost);
    }

    // ---------- Deutschland / Alphaplan ----------

    [Fact]
    public void DeriveAlphaplanUnitCost_DerivesCostFromNetAndGrossProfit()
    {
        // Echte erste Zeile aus invoice_lines.csv: Netto 220.80, Rohertrag 57.06, Menge 1.
        var unitCost = ManualExcelImportService.DeriveAlphaplanUnitCost(220.80m, 57.060173941919999m, 1m);

        Assert.Equal(163.74m, Math.Round(unitCost, 2));
    }

    [Fact]
    public void DeriveAlphaplanUnitCost_DividesLineTotalByQuantity()
    {
        // Nettoumsatz und Rohertrag sind ZEILENSUMMEN. Ohne die Division waere die
        // Kostenbasis um den Mengenfaktor zu hoch, weil die Margenlogik nochmals
        // mit der Menge multipliziert.
        var unitCost = ManualExcelImportService.DeriveAlphaplanUnitCost(1000m, 400m, 10m);

        Assert.Equal(60m, unitCost);
    }

    [Fact]
    public void DeriveAlphaplanUnitCost_ReturnsPositiveMagnitudeForCreditNotes()
    {
        // Gutschrift: negative Betraege. Das Vorzeichen dreht die Margenlogik selbst,
        // die Kostenbasis bleibt ein Betrag.
        var unitCost = ManualExcelImportService.DeriveAlphaplanUnitCost(-1000m, -400m, -10m);

        Assert.Equal(60m, unitCost);
    }

    [Fact]
    public void DeriveAlphaplanUnitCost_FallsBackToLineTotal_WhenQuantityIsZero()
    {
        var unitCost = ManualExcelImportService.DeriveAlphaplanUnitCost(500m, 200m, 0m);

        Assert.Equal(300m, unitCost);
    }

    [Fact]
    public void DeriveAlphaplanUnitCost_ReturnsZero_WhenThereIsNoCost()
    {
        // Rohertrag = Nettoumsatz -> kein Einstandswert bekannt.
        Assert.Equal(0m, ManualExcelImportService.DeriveAlphaplanUnitCost(300m, 300m, 5m));
    }
}
