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

    [Fact]
    public void Compose_NormalizesMatnrForProductReferenceLeftJoin()
    {
        var composer = new MappedSalesRecordComposer();
        var site = new Site { TSC = "TRCH", Land = "Schweiz" };
        var sources = new[]
        {
            new SapSourceDefinition { Alias = "Z", EntitySet = "Sales", IsPrimary = true, IsActive = true },
            new SapSourceDefinition { Alias = "P", EntitySet = "ProductDivisionRefSet", IsActive = true, SortOrder = 1 }
        };
        var joins = new[]
        {
            new SapJoinDefinition { LeftAlias = "Z", RightAlias = "P", LeftKeys = "Matnr", RightKeys = "Matnr", IsActive = true, SortOrder = 1 }
        };
        var mappings = new[]
        {
            Mapping(nameof(SalesRecord.Material), "Z.Matnr"),
            Mapping(nameof(SalesRecord.ProductHierarchyCode), "P.Paph1"),
            Mapping(nameof(SalesRecord.ProductHierarchyText), "P.Paph1Text"),
            Mapping(nameof(SalesRecord.ProductFamilyCode), "P.Wwpfa"),
            Mapping(nameof(SalesRecord.ProductFamilyText), "P.WwpfaText"),
            Mapping(nameof(SalesRecord.ProductDivisionCode), "P.Wwpsp"),
            Mapping(nameof(SalesRecord.ProductDivisionText), "P.WwpspText"),
            Mapping(nameof(SalesRecord.ProductMappingAssigned), "P.IsAssigned")
        };
        var rows = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Z"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Matnr"] = "6"
                }
            ],
            ["P"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Matnr"] = "000000000000000006",
                    ["Paph1"] = "0414",
                    ["Paph1Text"] = "Industat innen",
                    ["Wwpfa"] = "0004",
                    ["WwpfaText"] = "Industat",
                    ["Wwpsp"] = "0001",
                    ["WwpspText"] = "Thermostate",
                    ["IsAssigned"] = true
                }
            ]
        };

        var result = composer.Compose(site, sources, joins, mappings, rows, "SAP");

        Assert.Single(result);
        Assert.Equal("6", result[0].Material);
        Assert.Equal("0414", result[0].ProductHierarchyCode);
        Assert.Equal("Industat innen", result[0].ProductHierarchyText);
        Assert.Equal("0004", result[0].ProductFamilyCode);
        Assert.Equal("Industat", result[0].ProductFamilyText);
        Assert.Equal("0001", result[0].ProductDivisionCode);
        Assert.Equal("Thermostate", result[0].ProductDivisionText);
        Assert.Equal("True", result[0].ProductMappingAssigned);
    }

    [Fact]
    public void Compose_UsesFirstNonEmptyFallbackWhenPrimaryProductReferenceIsMissing()
    {
        var composer = new MappedSalesRecordComposer();
        var site = new Site { TSC = "TRAT", Land = "Oesterreich" };
        var sources = new[]
        {
            new SapSourceDefinition { Alias = "Z", EntitySet = "Sales", IsPrimary = true, IsActive = true },
            new SapSourceDefinition { Alias = "P", EntitySet = "ProductDivisionRefSet", IsActive = true, SortOrder = 1 },
            new SapSourceDefinition { Alias = "M", EntitySet = "ProductDivisionMapSet", IsActive = true, SortOrder = 2 }
        };
        var joins = new[]
        {
            new SapJoinDefinition { LeftAlias = "Z", RightAlias = "P", LeftKeys = "Matnr", RightKeys = "Matnr", IsActive = true, SortOrder = 1 },
            new SapJoinDefinition { LeftAlias = "Z", RightAlias = "M", LeftKeys = "Prodh", RightKeys = "Paph1", IsActive = true, SortOrder = 2 }
        };
        var mappings = new[]
        {
            Mapping(nameof(SalesRecord.Material), "Z.Matnr"),
            Mapping(nameof(SalesRecord.ProductGroup), "Z.Prodh"),
            Mapping(nameof(SalesRecord.ProductHierarchyCode), "FirstNonEmpty(P.Paph1, M.Paph1)"),
            Mapping(nameof(SalesRecord.ProductFamilyCode), "FirstNonEmpty(P.Wwpfa, M.Wwpfa)"),
            Mapping(nameof(SalesRecord.ProductDivisionCode), "FirstNonEmpty(P.Wwpsp, M.Wwpsp)"),
            Mapping(nameof(SalesRecord.ProductMappingAssigned), "FirstNonEmpty(P.IsAssigned, M.IsAssigned)")
        };
        var rows = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Z"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Matnr"] = "900720",
                    ["Prodh"] = "9999"
                }
            ],
            ["P"] = [],
            ["M"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Paph1"] = "9999",
                    ["Wwpfa"] = "0043",
                    ["Wwpsp"] = "0008",
                    ["IsAssigned"] = true
                }
            ]
        };

        var result = composer.Compose(site, sources, joins, mappings, rows, "SAP");

        Assert.Single(result);
        Assert.Equal("900720", result[0].Material);
        Assert.Equal("9999", result[0].ProductGroup);
        Assert.Equal("9999", result[0].ProductHierarchyCode);
        Assert.Equal("0043", result[0].ProductFamilyCode);
        Assert.Equal("0008", result[0].ProductDivisionCode);
        Assert.Equal("True", result[0].ProductMappingAssigned);
    }

    [Fact]
    public void Compose_FirstNonEmptyKeepsMaterialReferenceBeforeProductHierarchyFallback()
    {
        var composer = new MappedSalesRecordComposer();
        var site = new Site { TSC = "TRCH", Land = "Schweiz" };
        var sources = new[]
        {
            new SapSourceDefinition { Alias = "Z", EntitySet = "Sales", IsPrimary = true, IsActive = true },
            new SapSourceDefinition { Alias = "P", EntitySet = "ProductDivisionRefSet", IsActive = true, SortOrder = 1 },
            new SapSourceDefinition { Alias = "M", EntitySet = "ProductDivisionMapSet", IsActive = true, SortOrder = 2 }
        };
        var joins = new[]
        {
            new SapJoinDefinition { LeftAlias = "Z", RightAlias = "P", LeftKeys = "Matnr", RightKeys = "Matnr", IsActive = true, SortOrder = 1 },
            new SapJoinDefinition { LeftAlias = "Z", RightAlias = "M", LeftKeys = "Prodh", RightKeys = "Paph1", IsActive = true, SortOrder = 2 }
        };
        var mappings = new[]
        {
            Mapping(nameof(SalesRecord.ProductHierarchyCode), "FirstNonEmpty(P.Paph1, M.Paph1)"),
            Mapping(nameof(SalesRecord.ProductFamilyCode), "FirstNonEmpty(P.Wwpfa, M.Wwpfa)"),
            Mapping(nameof(SalesRecord.ProductDivisionCode), "FirstNonEmpty(P.Wwpsp, M.Wwpsp)")
        };
        var rows = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Z"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Matnr"] = "6",
                    ["Prodh"] = "9999"
                }
            ],
            ["P"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Matnr"] = "6",
                    ["Paph1"] = "0414",
                    ["Wwpfa"] = "0004",
                    ["Wwpsp"] = "0001"
                }
            ],
            ["M"] =
            [
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Paph1"] = "9999",
                    ["Wwpfa"] = "0043",
                    ["Wwpsp"] = "0008"
                }
            ]
        };

        var result = composer.Compose(site, sources, joins, mappings, rows, "SAP");

        Assert.Single(result);
        Assert.Equal("0414", result[0].ProductHierarchyCode);
        Assert.Equal("0004", result[0].ProductFamilyCode);
        Assert.Equal("0001", result[0].ProductDivisionCode);
    }

    private static SapFieldMapping Mapping(string targetField, string sourceExpression)
        => new()
        {
            TargetField = targetField,
            SourceExpression = sourceExpression,
            IsActive = true
        };
}
