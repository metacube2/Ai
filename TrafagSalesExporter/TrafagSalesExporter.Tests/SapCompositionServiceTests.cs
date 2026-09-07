using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class SapCompositionServiceTests
{
    [Fact]
    public async Task BuildSalesRecordsAsync_Rejects_Large_ProductReference_When_All_Rows_Are_Unassigned()
    {
        var service = CreateService(new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FinanzdataSchweizOeSet"] =
            [
                Row(("Matnr", "6"))
            ],
            ["ProductDivisionRefSet"] = Enumerable.Range(1, 101)
                .Select(i => Row(
                    ("Matnr", i.ToString()),
                    ("Wwpsp", "UNASS"),
                    ("WwpspText", "Nicht zugeordnet"),
                    ("IsAssigned", false)))
                .ToList()
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.BuildSalesRecordsAsync(
            CreateSite(),
            CreateSources(),
            CreateJoins(),
            CreateMappings(),
            "user",
            "password"));

        Assert.Contains("ProductDivisionRefSet", exception.Message);
        Assert.Contains("keine zugeordneten Sparten", exception.Message);
        Assert.Contains("Import abgebrochen", exception.Message);
    }

    [Fact]
    public async Task BuildSalesRecordsAsync_Allows_Misc_ProductReference_Code_0008()
    {
        var productRows = Enumerable.Range(1, 101)
            .Select(i => Row(
                ("Matnr", i.ToString()),
                ("Wwpsp", "0008"),
                ("WwpspText", "Übrige"),
                ("Wwpfa", ""),
                ("WwpfaText", "Übrige"),
                ("IsAssigned", true)))
            .ToList();

        var service = CreateService(new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FinanzdataSchweizOeSet"] =
            [
                Row(("Matnr", "000000000000000006"))
            ],
            ["ProductDivisionRefSet"] = productRows
        });

        var result = await service.BuildSalesRecordsAsync(
            CreateSite(),
            CreateSources(),
            CreateJoins(),
            CreateMappings(),
            "user",
            "password");

        var record = Assert.Single(result);
        Assert.Equal("000000000000000006", record.Material);
        Assert.Equal("0008", record.ProductDivisionCode);
        Assert.Equal("Übrige", record.ProductDivisionText);
        Assert.Equal("", record.ProductFamilyCode);
        Assert.Equal("Übrige", record.ProductFamilyText);
        Assert.Equal("True", record.ProductMappingAssigned);
    }

    [Fact]
    public async Task BuildSalesRecordsAsync_Zschweiz_Uses_Wavwr_DividedByQuantity_As_StandardCost()
    {
        var service = CreateService(new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FinanzdataSchweizOeSet"] =
            [
                Row(
                    ("Matnr", "6"),
                    ("WavwrDc", "24398.33"),
                    ("Fkimg", "100"),
                    ("Waerk", "CHF"),
                    ("StprsHc", "1.01"),
                    ("Hwaer", "CHF"))
            ]
        });

        var result = await service.BuildSalesRecordsAsync(
            CreateSite(), CreateStandardCostSources(), [], CreateStandardCostMappings(), "user", "password");

        var record = Assert.Single(result);
        Assert.Equal(243.9833m, record.StandardCost);
        Assert.Equal("CHF", record.StandardCostCurrency);
    }

    [Fact]
    public async Task BuildSalesRecordsAsync_Zschweiz_Falls_Back_To_Stprs_When_Wavwr_Is_Zero()
    {
        var service = CreateService(new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FinanzdataSchweizOeSet"] =
            [
                Row(
                    ("Matnr", "6"),
                    ("WavwrDc", "0"),
                    ("Fkimg", "10"),
                    ("Waerk", "CHF"),
                    ("StprsHc", "1.01"),
                    ("Hwaer", "CHF"))
            ]
        });

        var result = await service.BuildSalesRecordsAsync(
            CreateSite(), CreateStandardCostSources(), [], CreateStandardCostMappings(), "user", "password");

        var record = Assert.Single(result);
        Assert.Equal(1.01m, record.StandardCost);
        Assert.Equal("CHF", record.StandardCostCurrency);
    }

    [Fact]
    public async Task BuildSalesRecordsAsync_Zschweiz_Zero_When_Neither_Wavwr_Nor_Stprs_Available()
    {
        var service = CreateService(new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FinanzdataSchweizOeSet"] =
            [
                Row(
                    ("Matnr", "6"),
                    ("WavwrDc", "0"),
                    ("Fkimg", "10"),
                    ("Waerk", "CHF"),
                    ("StprsHc", "0"),
                    ("Hwaer", "CHF"))
            ]
        });

        var result = await service.BuildSalesRecordsAsync(
            CreateSite(), CreateStandardCostSources(), [], CreateStandardCostMappings(), "user", "password");

        var record = Assert.Single(result);
        Assert.Equal(0m, record.StandardCost);
        Assert.Equal("CHF", record.StandardCostCurrency);
    }

    [Fact]
    public async Task BuildSalesRecordsAsync_Zschweiz_Corrects_Scaled_HouseCurrency_Netwr()
    {
        var service = CreateService(new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FinanzdataSchweizOeSet"] =
            [
                Row(
                    ("Matnr", "6"),
                    ("Waerk", "USD"),
                    ("Hwaer", "CHF"),
                    ("Kurrf", "0.85"),
                    ("NetwrDc", "19000.00"),
                    ("NetwrHc", "161.50"))
            ]
        });

        var result = await service.BuildSalesRecordsAsync(
            CreateSite(), CreateStandardCostSources(),
            [], [Mapping(nameof(SalesRecord.Material), "Z.Matnr"), Mapping(nameof(SalesRecord.SalesPriceValue), "Z.NetwrHc")],
            "user", "password");

        var record = Assert.Single(result);
        Assert.Equal(16150.00m, record.SalesPriceValue);
    }

    [Fact]
    public async Task BuildSalesRecordsAsync_Zschweiz_Leaves_Correct_HouseCurrency_Netwr_Unchanged()
    {
        var service = CreateService(new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FinanzdataSchweizOeSet"] =
            [
                Row(
                    ("Matnr", "6"),
                    ("Waerk", "USD"),
                    ("Hwaer", "CHF"),
                    ("Kurrf", "0.85"),
                    ("NetwrDc", "19000.00"),
                    ("NetwrHc", "16150.00"))
            ]
        });

        var result = await service.BuildSalesRecordsAsync(
            CreateSite(), CreateStandardCostSources(),
            [], [Mapping(nameof(SalesRecord.Material), "Z.Matnr"), Mapping(nameof(SalesRecord.SalesPriceValue), "Z.NetwrHc")],
            "user", "password");

        var record = Assert.Single(result);
        Assert.Equal(16150.00m, record.SalesPriceValue);
    }

    [Fact]
    public async Task BuildSalesRecordsAsync_Zschweiz_Leaves_SameCurrency_Netwr_Unchanged()
    {
        var service = CreateService(new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["FinanzdataSchweizOeSet"] =
            [
                Row(
                    ("Matnr", "6"),
                    ("Waerk", "CHF"),
                    ("Hwaer", "CHF"),
                    ("Kurrf", "1.0"),
                    ("NetwrDc", "315.20"),
                    ("NetwrHc", "315.20"))
            ]
        });

        var result = await service.BuildSalesRecordsAsync(
            CreateSite(), CreateStandardCostSources(),
            [], [Mapping(nameof(SalesRecord.Material), "Z.Matnr"), Mapping(nameof(SalesRecord.SalesPriceValue), "Z.NetwrHc")],
            "user", "password");

        var record = Assert.Single(result);
        Assert.Equal(315.20m, record.SalesPriceValue);
    }

    private static List<SapSourceDefinition> CreateStandardCostSources()
        => [new() { Id = 1, Alias = "Z", EntitySet = "FinanzdataSchweizOeSet", IsPrimary = true, IsActive = true, SortOrder = 0 }];

    private static List<SapFieldMapping> CreateStandardCostMappings()
        =>
        [
            Mapping(nameof(SalesRecord.Material), "Z.Matnr"),
            Mapping(nameof(SalesRecord.StandardCost), "Z.ResolvedStandardCost"),
            Mapping(nameof(SalesRecord.StandardCostCurrency), "Z.ResolvedStandardCostCurrency")
        ];

    private static SapCompositionService CreateService(IReadOnlyDictionary<string, List<Dictionary<string, object?>>> rowsByEntitySet)
        => new(
            new FakeSapGatewayService(rowsByEntitySet),
            new MappedSalesRecordComposer(),
            new NoopAppEventLogService());

    private static Site CreateSite()
        => new()
        {
            Id = 9,
            TSC = "ZSCHWEIZ",
            Land = "Schweiz/Oesterreich",
            SapServiceUrl = "http://sap.example.local/sap/opu/odata/sap/Z_SERVICE/"
        };

    private static List<SapSourceDefinition> CreateSources()
        =>
        [
            new() { Id = 1, Alias = "Z", EntitySet = "FinanzdataSchweizOeSet", IsPrimary = true, IsActive = true, SortOrder = 0 },
            new() { Id = 2, Alias = "P", EntitySet = "ProductDivisionRefSet", IsPrimary = false, IsActive = true, SortOrder = 1 }
        ];

    private static List<SapJoinDefinition> CreateJoins()
        =>
        [
            new() { LeftAlias = "Z", RightAlias = "P", LeftKeys = "Matnr", RightKeys = "Matnr", JoinType = "Left", IsActive = true, SortOrder = 1 }
        ];

    private static List<SapFieldMapping> CreateMappings()
        =>
        [
            Mapping(nameof(SalesRecord.Material), "Z.Matnr"),
            Mapping(nameof(SalesRecord.ProductFamilyCode), "P.Wwpfa"),
            Mapping(nameof(SalesRecord.ProductFamilyText), "P.WwpfaText"),
            Mapping(nameof(SalesRecord.ProductDivisionCode), "P.Wwpsp"),
            Mapping(nameof(SalesRecord.ProductDivisionText), "P.WwpspText"),
            Mapping(nameof(SalesRecord.ProductMappingAssigned), "P.IsAssigned")
        ];

    private static SapFieldMapping Mapping(string targetField, string sourceExpression)
        => new()
        {
            TargetField = targetField,
            SourceExpression = sourceExpression,
            IsActive = true
        };

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
            row[key] = value;
        return row;
    }

    private sealed class FakeSapGatewayService : ISapGatewayService
    {
        private readonly IReadOnlyDictionary<string, List<Dictionary<string, object?>>> _rowsByEntitySet;

        public FakeSapGatewayService(IReadOnlyDictionary<string, List<Dictionary<string, object?>>> rowsByEntitySet)
        {
            _rowsByEntitySet = rowsByEntitySet;
        }

        public Task TestConnectionAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<string>> GetEntitySetsAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<string>());

        public Task<List<string>> GetEntityFieldNamesAsync(string serviceUrl, string entitySet, string username, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<string>());

        public Task<List<Dictionary<string, object?>>> GetEntityRowsAsync(
            string serviceUrl,
            string entitySet,
            string username,
            string password,
            string? filter = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_rowsByEntitySet.TryGetValue(entitySet, out var rows) ? rows : []);
    }

    private sealed class NoopAppEventLogService : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }
}
