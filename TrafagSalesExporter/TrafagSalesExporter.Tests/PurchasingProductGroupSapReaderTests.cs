using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class PurchasingProductGroupSapReaderTests
{
    [Fact]
    public async Task ReadAsync_Joins_Zdispo_EntitySets_Directly_From_Sap()
    {
        var gateway = new FakeSapGatewayService(
            ["EKKOSet", "ZDISPO_GRPSet", "ZDISPO_SPARTSet"],
            new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["ZDISPO_GRPSet"] =
                [
                    Row(("DISPO_KZ", "001"), ("DISPO", "H")),
                    Row(("DISPO_KZ", "EL*"), ("DISPO", "T2")),
                    Row(("DISPO_KZ", "DS1"), ("DISPO", "D5")),
                    Row(("DISPO_KZ", "DS1"), ("DISPO", "DS"))
                ],
                ["ZDISPO_SPARTSet"] =
                [
                    Row(("DISPO", "H"), ("DESCR", "HW_ZUKAUFSORT")),
                    Row(("DISPO", "T2"), ("DESCR", "FP_TRANSM. TX")),
                    Row(("DISPO", "DS"), ("DESCR", "FP_DICHTESENS"))
                ]
            });

        var result = await new PurchasingProductGroupSapReader(gateway)
            .ReadAsync("https://sap.example/", "user", "pass");

        Assert.Equal("ZDISPO_GRPSet + ZDISPO_SPARTSet", result.SourceEntitySets);
        Assert.Equal(4, result.Rules.Count);
        Assert.Contains(result.Rules, rule => rule == new PurchasingProductGroupSapRule("001", "H", "HW_ZUKAUFSORT"));
        Assert.Contains(result.Rules, rule => rule == new PurchasingProductGroupSapRule("EL*", "T2", "FP_TRANSM. TX"));
        Assert.Contains(result.Rules, rule => rule == new PurchasingProductGroupSapRule("DS1", "D5", "D5"));
        Assert.Contains(result.Rules, rule => rule == new PurchasingProductGroupSapRule("DS1", "DS", "FP_DICHTESENS"));
    }

    [Fact]
    public async Task ReadAsync_Accepts_Prejoined_Sap_EntitySet()
    {
        var gateway = new FakeSapGatewayService(
            ["ZSTR_PRODUCT_GROUPSet"],
            new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["ZSTR_PRODUCT_GROUPSet"] =
                [Row(("DisponentPattern", "el*"), ("ProductGroup", "t2"), ("ProductGroupText", "FP_TRANSM. TX"))]
            });

        var result = await new PurchasingProductGroupSapReader(gateway)
            .ReadAsync("https://sap.example/", "user", "pass");

        var rule = Assert.Single(result.Rules);
        Assert.Equal(new PurchasingProductGroupSapRule("EL*", "T2", "FP_TRANSM. TX"), rule);
    }

    [Fact]
    public async Task ReadAsync_Fails_Clearly_Without_Sap_Source_And_Never_Uses_Excel()
    {
        var reader = new PurchasingProductGroupSapReader(new FakeSapGatewayService(
            ["EKKOSet"],
            new Dictionary<string, List<Dictionary<string, object?>>>()));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.ReadAsync("https://sap.example/", "user", "pass"));

        Assert.Contains("ZDISPO_GRP", error.Message);
        Assert.Contains("Excel wird bewusst nicht", error.Message);
    }

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values) =>
        values.ToDictionary(value => value.Key, value => value.Value, StringComparer.OrdinalIgnoreCase);

    private sealed class FakeSapGatewayService(
        List<string> entitySets,
        IReadOnlyDictionary<string, List<Dictionary<string, object?>>> rowsByEntitySet) : ISapGatewayService
    {
        public Task TestConnectionAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<string>> GetEntitySetsAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(entitySets);

        public Task<List<string>> GetEntityFieldNamesAsync(string serviceUrl, string entitySet, string username, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<string>());

        public Task<List<Dictionary<string, object?>>> GetEntityRowsAsync(
            string serviceUrl,
            string entitySet,
            string username,
            string password,
            string? filter = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(rowsByEntitySet.TryGetValue(entitySet, out var rows) ? rows : []);
    }
}
