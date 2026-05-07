using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class MappedSalesRecordComposerTests
{
    [Fact]
    public void Compose_MapsPrimarySourceConstantsAndODataDate()
    {
        var composer = new MappedSalesRecordComposer();
        var site = new Site { TSC = "TRCH", Land = "Schweiz" };
        var sources = new[]
        {
            new SapSourceDefinition { Alias = "Z", EntitySet = "ZSCHWEIZSet", IsPrimary = true, IsActive = true }
        };
        var mappings = new[]
        {
            Mapping(nameof(SalesRecord.InvoiceNumber), "Z.VBELN"),
            Mapping(nameof(SalesRecord.PositionOnInvoice), "Z.POSNR"),
            Mapping(nameof(SalesRecord.InvoiceDate), "Z.FKDAT"),
            Mapping(nameof(SalesRecord.SalesPriceValue), "Z.NETWR_HC"),
            Mapping(nameof(SalesRecord.SalesCurrency), "Z.HWAER"),
            Mapping(nameof(SalesRecord.DocumentType), "=SAP")
        };
        var rows = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Z"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["VBELN"] = "900001",
                    ["POSNR"] = "10",
                    ["FKDAT"] = "/Date(1735689600000)/",
                    ["NETWR_HC"] = "1234.50",
                    ["HWAER"] = "CHF"
                }
            ]
        };

        var result = composer.Compose(site, sources, [], mappings, rows, "SAP");

        Assert.Single(result);
        Assert.Equal("TRCH", result[0].Tsc);
        Assert.Equal("Schweiz", result[0].Land);
        Assert.Equal("900001", result[0].InvoiceNumber);
        Assert.Equal(10, result[0].PositionOnInvoice);
        Assert.Equal(new DateTime(2025, 1, 1), result[0].InvoiceDate);
        Assert.Equal(1234.50m, result[0].SalesPriceValue);
        Assert.Equal("CHF", result[0].SalesCurrency);
        Assert.Equal("SAP", result[0].DocumentType);
    }

    [Fact]
    public void Compose_AppliesLeftJoinAndDefaultDocumentType()
    {
        var composer = new MappedSalesRecordComposer();
        var site = new Site { TSC = "TRAT", Land = "Oesterreich" };
        var sources = new[]
        {
            new SapSourceDefinition { Alias = "H", EntitySet = "Header", IsPrimary = true, IsActive = true },
            new SapSourceDefinition { Alias = "C", EntitySet = "Customer", IsActive = true, SortOrder = 1 }
        };
        var joins = new[]
        {
            new SapJoinDefinition
            {
                LeftAlias = "H",
                RightAlias = "C",
                LeftKeys = "KUNNR",
                RightKeys = "KUNNR",
                IsActive = true
            }
        };
        var mappings = new[]
        {
            Mapping(nameof(SalesRecord.InvoiceNumber), "H.VBELN"),
            Mapping(nameof(SalesRecord.CustomerName), "C.NAME1"),
            Mapping(nameof(SalesRecord.CustomerCountry), "C.LAND1")
        };
        var rows = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["H"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["VBELN"] = "910001",
                    ["KUNNR"] = "1000"
                }
            ],
            ["C"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["KUNNR"] = "1000",
                    ["NAME1"] = "Trafag AG",
                    ["LAND1"] = "CH"
                }
            ]
        };

        var result = composer.Compose(site, sources, joins, mappings, rows, "HANA");

        Assert.Single(result);
        Assert.Equal("910001", result[0].InvoiceNumber);
        Assert.Equal("Trafag AG", result[0].CustomerName);
        Assert.Equal("CH", result[0].CustomerCountry);
        Assert.Equal("HANA", result[0].DocumentType);
    }

    private static SapFieldMapping Mapping(string targetField, string sourceExpression)
        => new()
        {
            TargetField = targetField,
            SourceExpression = sourceExpression,
            IsActive = true
        };
}
