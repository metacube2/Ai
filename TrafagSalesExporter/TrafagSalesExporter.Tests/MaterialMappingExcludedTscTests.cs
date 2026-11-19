using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Entscheid Ingo 2026-11-19: TRCH und TRAT sind kein Mapping-Fall. Keine Vorschlaege, keine offenen Zeilen, keine
/// Regeltreffer, und keine Zeile wirkt (auch nicht nach Import). Issue Topic bleibt unveraendert.
/// </summary>
public class MaterialMappingExcludedTscTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 19, 14, 0, 0, DateTimeKind.Utc);
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MaterialMappingExcludedTscTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private static readonly IReadOnlyDictionary<string, string> TrAg = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["37705"] = "Drucktransmitter NAT 8252",
        ["53513"] = "Thermostat"
    };

    [Theory]
    [InlineData("TRAT")]
    [InlineData("trch")]
    public void Kein_Vorschlag_und_kein_Regeltreffer_fuer_CH_AT(string tsc)
    {
        var candidates = new[]
        {
            new MaterialMappingCandidate(tsc, "901130", "Drucktransmitter NAT 8252", "TR AG", 3, 100m, "EUR"),
            new MaterialMappingCandidate(tsc, "901131", "Ersatz fuer 53513", "TR AG", 3, 100m, "EUR")
        };

        Assert.Empty(MaterialMappingSuggester.Suggest(candidates, TrAg, Now));
    }

    [Fact]
    public void Keine_Kandidaten_und_keine_offenen_Zeilen_fuer_TRAT_Issue_Topic_bleibt()
    {
        var record = new SalesRecord { Tsc = "TRAT", Land = "AT", Material = "901130", Name = "Transmitter", StandardCost = 5m, Quantity = 1m, SalesPriceValue = 10m };

        var candidates = MaterialMappingSuggestionService.BuildCandidates(
            [record], new Dictionary<(string, string), GroupStandardCost>(), new HashSet<string>(["37705"]),
            new ExportSettings(), new HashSet<string>(), MaterialMappingLookup.Empty);
        var rows = MaterialMappingStore.WithOpenRows([], [new MaterialMappingCandidate("TRAT", "901130", "T", "TR AG", 1, 10m, "EUR")], Now);
        var topic = GroupMarginCalculator.Evaluate(
            new GroupMarginLine { Tsc = "TRAT", Material = "901130", Quantity = 1m, StandardCost = 5m, StandardCostCurrency = "EUR", NetSalesValue = 10m },
            chPlantMaterialKeys: new HashSet<string>(["37705"])).IssueTopic;

        Assert.Empty(candidates);
        Assert.Empty(rows);
        Assert.Equal(IssueTopics.SupplierNotFound, topic);   // Spalte Issue Topic fuer CH/AT unveraendert
    }

    [Fact]
    public async Task Importierte_bestaetigte_TRAT_Zeile_wirkt_nie()
    {
        await using var db = new AppDbContext(_options);
        var result = await MaterialMappingStore.ImportAsync(db,
            [new MaterialMappingImportRow(2, "TRAT", "901130", "", "37705", "", "", "", "Hand", "", "", "bestaetigt", "koi")],
            new HashSet<string>(["TRAT"]), "koi", Now);
        Assert.True(result.Success);

        var lookup = await MaterialMappingStore.LoadLookupAsync(db);
        var mapping = await db.MaterialMappings.AsNoTracking().SingleAsync();
        var evaluation = GroupMarginCalculator.Evaluate(
            new GroupMarginLine { Tsc = "TRAT", Material = "901130", Quantity = 1m, StandardCost = 5m, StandardCostCurrency = "EUR", NetSalesValue = 10m },
            new Dictionary<(string, string), GroupStandardCost>
            {
                [("37705", "1100")] = new() { MaterialKey = "37705", ValuationArea = "1100", UnitCost = 3m, Currency = "EUR" }
            },
            materialMappings: lookup);

        Assert.False(MaterialMappingStore.IsEffective(mapping));
        Assert.Equal(0, lookup.Count);
        Assert.False(evaluation.IsGroupCost);
        Assert.Equal(string.Empty, evaluation.Mapping);
    }

    [Fact]
    public async Task Vorschlagslauf_raeumt_alte_CH_AT_Zeilen_weg_ausser_bestaetigte_und_Hand()
    {
        await using var db = new AppDbContext(_options);
        MaterialMapping Row(string tsc, string key, string method, string status)
            => new() { Tsc = tsc, LocalMaterialKey = key, GroupMaterialNumber = "37705", Method = method, Status = status, CreatedAtUtc = Now, UpdatedAtUtc = Now };
        db.MaterialMappings.AddRange(
            Row("TRAT", "A", MaterialMappingMethods.Text, MaterialMappingStatuses.Proposed),
            Row("TRAT", "B", MaterialMappingMethods.Rule, MaterialMappingStatuses.Proposed),
            Row("TRCH", "C", string.Empty, MaterialMappingStatuses.Open),
            Row("TRAT", "D", MaterialMappingMethods.Text, MaterialMappingStatuses.Confirmed),
            Row("TRAT", "E", MaterialMappingMethods.Manual, MaterialMappingStatuses.Proposed),
            Row("TRUK", "F", MaterialMappingMethods.Text, MaterialMappingStatuses.Proposed));
        await db.SaveChangesAsync();

        var removed = await MaterialMappingStore.RemoveExcludedTscRowsAsync(db);

        Assert.Equal(3, removed);
        Assert.Equal(["D", "E", "F"], (await db.MaterialMappings.AsNoTracking().Select(m => m.LocalMaterialKey).ToListAsync()).OrderBy(k => k).ToArray());
    }

    [Fact]
    public async Task Vorschlaege_fuer_CH_AT_werden_nicht_gespeichert()
    {
        await using var db = new AppDbContext(_options);

        var (inserted, _, _) = await MaterialMappingStore.UpsertSuggestionsAsync(db,
            [new MaterialMapping { Tsc = "TRAT", LocalMaterialKey = "901130", GroupMaterialNumber = "37705", Method = "Text" }], Now);

        Assert.Equal(0, inserted);
        Assert.Empty(await db.MaterialMappings.ToListAsync());
    }
}
