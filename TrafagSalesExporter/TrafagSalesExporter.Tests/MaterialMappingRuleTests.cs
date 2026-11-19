using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Entscheide Ingo 19.11.2026 14:20: Regeltreffer wirken erst nach Bestaetigung; Nummern hinter „ersetzt“ und aehnlichen
/// Woertern zaehlen nicht; neue Regel TRDE Typencode/Sachnummer (Nummer nach dem letzten Schraegstrich).
/// </summary>
public class MaterialMappingRuleTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 19, 14, 30, 0, DateTimeKind.Utc);
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MaterialMappingRuleTests()
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
        ["47269"] = "Drucktransmitter alte Ausfuehrung",
        ["60077"] = "Drucktransmitter NAT",
        ["55441"] = "Druckschalter",
        ["40281"] = "Thermostat"
    };

    private static MaterialMappingCandidate Candidate(string tsc, string material, string text)
        => new(tsc, material, text, "", 2, 250m, "EUR");

    [Theory]
    [InlineData("NAT 0-10 bar ersetzt 47269")]
    [InlineData("NAT 0-10 bar, ersetzt durch: 47269")]
    [InlineData("NAT alt 47269")]
    [InlineData("NAT replaces 47269")]
    [InlineData("NAT Kundenartikelnummer 47269")]
    [InlineData("NAT Art.-Nr. 47269")]
    public void Nummer_hinter_ersetzt_alt_oder_Kundennummer_ist_kein_Treffer(string text)
    {
        Assert.Empty(MaterialMappingSuggester.NumbersInText(text));
        Assert.DoesNotContain(MaterialMappingSuggester.Suggest([Candidate("TRDE", "N10.0", text)], TrAg, Now),
            suggestion => suggestion.Method == MaterialMappingMethods.Rule);
    }

    [Fact]
    public void Fehltreffer_60077_nimmt_nicht_mehr_die_ersetzte_Nummer()
    {
        // TRDE 8256.29.2315/60077: der Text nannte 47269 hinter „ersetzt“; die Schraegstrich-Regel trifft die eigene Nummer.
        var result = Assert.Single(MaterialMappingSuggester.Suggest(
            [Candidate("TRDE", "8256.29.2315/60077", "Drucktransmitter ersetzt 47269")], TrAg, Now));

        Assert.Equal(("60077", MaterialMappingSuggester.RuleDeSlash), (result.GroupMaterialNumber, result.MethodDetail));
    }

    [Fact]
    public void Nummer_ohne_Ausschlusswort_bleibt_Kandidat()
    {
        Assert.Equal(["47269"], MaterialMappingSuggester.NumbersInText("Transmitter 47269 Ersatzteil").ToArray());
        Assert.Equal(["55441"], MaterialMappingSuggester.NumbersInText("Alternativ 55441").ToArray());   // „alt“ nur als ganzes Wort
    }

    [Theory]
    [InlineData("8258.85.2317/55441", "55441")]
    [InlineData("9B0.2076.753/40281", "40281")]
    public void DE_Schraegstrich_mit_Werkstamm_Treffer_ergibt_Regel_Vorschlag(string material, string expected)
    {
        var result = Assert.Single(MaterialMappingSuggester.Suggest([Candidate("TRDE", material, "")], TrAg, Now));

        Assert.Equal((expected, MaterialMappingMethods.Rule, 100, MaterialMappingStatuses.Proposed),
            (result.GroupMaterialNumber, result.Method, result.Confidence, result.Status));
        Assert.False(MaterialMappingStore.IsEffective(result));
    }

    [Theory]
    [InlineData("TRDE", "8258.85.2317/99999")]   // nicht im Werkstamm
    [InlineData("TRDE", "8258.85.2317/")]        // nichts nach dem Schraegstrich
    [InlineData("TRUK", "8258.85.2317/55441")]   // nur TRDE
    public void DE_Schraegstrich_ohne_Werkstamm_Treffer_oder_anderer_TSC_ergibt_keinen_Regeltreffer(string tsc, string material)
    {
        Assert.DoesNotContain(MaterialMappingSuggester.Suggest([Candidate(tsc, material, "")], TrAg, Now),
            suggestion => suggestion.MethodDetail == MaterialMappingSuggester.RuleDeSlash);
    }

    [Fact]
    public void Regeltreffer_wirkt_erst_nach_Bestaetigung()
    {
        var rule = new MaterialMapping { Tsc = "TRDE", LocalMaterialKey = "8258.85.2317/55441", GroupMaterialNumber = "55441",
            Method = MaterialMappingMethods.Rule, Status = MaterialMappingStatuses.Proposed };
        var line = new GroupMarginLine { Tsc = "TRDE", Material = "8258.85.2317/55441", Quantity = 1m, StandardCost = 5m, StandardCostCurrency = "EUR", NetSalesValue = 10m };
        var costs = new Dictionary<(string, string), GroupStandardCost>
        {
            [("55441", "1100")] = new() { MaterialKey = "55441", ValuationArea = "1100", UnitCost = 3m, Currency = "EUR" }
        };
        var plant = new HashSet<string>(["55441"]);

        var before = GroupMarginCalculator.Evaluate(line, costs, chPlantMaterialKeys: plant, supplierFallbackMode: SupplierFallbackModes.ChPlantMaster,
            materialMappings: new MaterialMappingLookup([rule]));
        rule.Status = MaterialMappingStatuses.Confirmed;
        var after = GroupMarginCalculator.Evaluate(line, costs, chPlantMaterialKeys: plant, supplierFallbackMode: SupplierFallbackModes.ChPlantMaster,
            materialMappings: new MaterialMappingLookup([rule]));

        Assert.False(before.IsGroupCost);
        Assert.True(after.IsGroupCost);
        Assert.Equal(3m, after.CostBasis);
    }

    [Fact]
    public async Task Nie_geprueft_bestaetigte_Regelzeilen_gehen_auf_Vorschlag_zurueck()
    {
        await using var db = new AppDbContext(_options);
        MaterialMapping Row(string key, string method, string reviewedBy, DateTime? reviewedAt)
            => new() { Tsc = "TRDE", LocalMaterialKey = key, GroupMaterialNumber = "1", Method = method, Status = MaterialMappingStatuses.Confirmed,
                ReviewedBy = reviewedBy, ReviewedAtUtc = reviewedAt, CreatedAtUtc = Now, UpdatedAtUtc = Now };
        db.MaterialMappings.AddRange(
            Row("A", MaterialMappingMethods.Rule, "", null),
            Row("B", MaterialMappingMethods.Rule, "koi", Now),
            Row("C", MaterialMappingMethods.Text, "", null));
        await db.SaveChangesAsync();

        var reset = await MaterialMappingStore.ResetUnreviewedRuleRowsAsync(db, Now);

        Assert.Equal(1, reset);
        var byKey = (await db.MaterialMappings.AsNoTracking().ToListAsync()).ToDictionary(m => m.LocalMaterialKey, m => m.Status);
        Assert.Equal(MaterialMappingStatuses.Proposed, byKey["A"]);
        Assert.Equal(MaterialMappingStatuses.Confirmed, byKey["B"]);
        Assert.Equal(MaterialMappingStatuses.Confirmed, byKey["C"]);
    }
}
