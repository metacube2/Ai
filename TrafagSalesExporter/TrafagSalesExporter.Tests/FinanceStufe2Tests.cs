using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Plan docs/FINANCE_PLAN_MAPPING_STUFE2_2026-11-20.md: Paket C (TR IT/TR IN kennen Artikel ohne Kosten, Topic 3 statt 2),
/// Paket B (Mapping auf die Artikelnummer der liefernden Gesellschaft TR IT), Paket D (Supplier Origin).
/// Jeder Rechenfall prueft mit, dass Kosten und Status nur dort wechseln, wo das Paket es will.
/// </summary>
public class FinanceStufe2Tests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private const string TrItArea = "TRIT";
    private const string TrInArea = "TRIN";
    private const string ItalySupplier = "Trafag Italia S.r.l";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public FinanceStufe2Tests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private static GroupMarginLine Line(string tsc = "TRFR", string material = "FR-1", string? supplierName = ItalySupplier,
        decimal standardCost = 60m)
        => new()
        {
            SupplierName = supplierName,
            Tsc = tsc,
            Material = material,
            Quantity = 1m,
            StandardCost = standardCost,
            StandardCostCurrency = "EUR",
            NetSalesValue = 100m
        };

    private static Dictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> Costs(
        params (string Key, string Area, decimal Cost)[] entries)
        => entries.ToDictionary(
            entry => (MaterialKeyNormalizer.Normalize(entry.Key), entry.Area),
            entry => new GroupStandardCost
            {
                MaterialKey = MaterialKeyNormalizer.Normalize(entry.Key), ValuationArea = entry.Area, UnitCost = entry.Cost, Currency = "EUR"
            });

    private static GroupMarginEvaluation Evaluate(GroupMarginLine line,
        Dictionary<(string, string), GroupStandardCost> costs, MaterialMappingLookup? mappings = null)
        => GroupMarginCalculator.Evaluate(line, costs, chPlantMaterialKeys: new HashSet<string>(["FILLER"], StringComparer.Ordinal),
            supplierFallbackMode: SupplierFallbackModes.ChPlantMaster, materialMappings: mappings);

    private static MaterialMapping ItMapping(string tsc = "TRFR", string local = "FR-1", string status = MaterialMappingStatuses.Confirmed,
        string entity = "TR IT", string deliveringNumber = "M_IT01_000931")
        => new()
        {
            Tsc = tsc, LocalMaterial = local, LocalMaterialKey = MaterialKeyNormalizer.Normalize(local), GroupMaterialNumber = string.Empty,
            DeliveringEntity = entity, DeliveringMaterialNumber = deliveringNumber, Method = MaterialMappingMethods.Text, Status = status,
            CreatedAtUtc = Now, UpdatedAtUtc = Now
        };

    private static SalesRecord B1Row(string material, decimal cost, string currency = "EUR", string? group = null)
        => new()
        {
            Material = material, GroupMaterialNumber = group ?? string.Empty, StandardCost = cost, StandardCostCurrency = currency,
            CompanyCurrency = currency, PostingDate = new DateTime(2026, 7, 1), ExtractionDate = new DateTime(2026, 7, 2)
        };

    // ----- Paket C -----

    [Fact]
    public void PaketC_Builder_legt_Artikel_ohne_Kosten_mit_0_ab_positiver_Wert_gewinnt()
    {
        var result = B1GroupStandardCostBuilder.Build("TRIT", [B1Row("A1", 0m), B1Row("A2", 0m), B1Row("A2", 12m)], Now)
            .ToDictionary(cost => cost.MaterialKey, cost => cost.UnitCost);

        Assert.Equal(0m, result["A1"]);
        Assert.Equal(12m, result["A2"]);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void PaketC_Builder_TRIN_legt_auch_die_indische_Nummer_ohne_Kosten_ab()
    {
        var result = B1GroupStandardCostBuilder.Build("TRIN", [B1Row("TS000777", 0m, "INR", group: "35561")], Now)
            .ToDictionary(cost => cost.MaterialKey, cost => cost.UnitCost);

        Assert.Equal(0m, result["35561"]);
        Assert.Equal(0m, result["TS000777"]);
    }

    [Fact]
    public async Task PaketC_ReplaceAsync_ersetzt_nicht_wenn_nur_Kosten_0_kommen()
    {
        await using var db = new AppDbContext(_options);
        db.GroupStandardCosts.Add(new GroupStandardCost { MaterialKey = "OLD", ValuationArea = TrItArea, UnitCost = 5m, Currency = "EUR", RefreshedAtUtc = Now });
        await db.SaveChangesAsync();

        var result = await B1GroupStandardCostStore.ReplaceAsync(db, "TRIT", [B1Row("A1", 0m)], Now);

        Assert.False(result.Updated);
        Assert.Equal(["OLD"], await db.GroupStandardCosts.Select(cost => cost.MaterialKey).ToListAsync());
    }

    [Fact]
    public void PaketC_TR_IT_Artikel_bekannt_ohne_Kosten_ergibt_Topic_3_Kosten_unveraendert_lokal()
    {
        var known = Evaluate(Line(material: "IT-7"), Costs(("IT-7", TrItArea, 0m)));
        var unknown = Evaluate(Line(material: "IT-7"), Costs(("OTHER", TrItArea, 9m)));

        Assert.Equal(IssueTopics.SupplierFoundNoCost, known.IssueTopic);
        Assert.Equal(IssueTopics.SupplierNotFound, unknown.IssueTopic);
        Assert.Equal((unknown.CostBasis, unknown.CostSource, unknown.Status, unknown.IsGroupCost),
            (known.CostBasis, known.CostSource, known.Status, known.IsGroupCost));
        Assert.Equal(60m, known.CostBasis);
    }

    [Fact]
    public void PaketC_voller_Schluessel_mit_0_verdeckt_die_I_Praefix_Regel_nicht()
    {
        var line = Line(tsc: "TRIT", material: "I53513", supplierName: "Trafag Controls India");

        var result = Evaluate(line, Costs(("I53513", TrInArea, 0m), ("53513", TrInArea, 30m)));

        Assert.True(result.IsGroupCost);
        Assert.Equal(30m, result.CostBasis);
        Assert.Equal(IssueTopics.None, result.IssueTopic);
        Assert.Contains(IssueTopics.ItalianIndiaPrefixRuleLabel, result.Mapping);
    }

    // ----- Paket B -----

    [Fact]
    public void PaketB_bestaetigte_TR_IT_Zeile_findet_Kosten_TR_IT_und_laesst_die_Gruppennummer_leer()
    {
        var mappings = new MaterialMappingLookup([ItMapping()]);
        var costs = Costs(("M_IT01_000931", TrItArea, 25m));

        var without = Evaluate(Line(), costs);
        var with = Evaluate(Line(), costs, mappings);

        Assert.Equal(IssueTopics.SupplierNotFound, without.IssueTopic);
        Assert.False(without.IsGroupCost);
        Assert.True(with.IsGroupCost);
        Assert.Equal(25m, with.CostBasis);
        Assert.Equal(IssueTopics.None, with.IssueTopic);
        Assert.Equal(GroupStandardCostEntities.TrIt, with.DeliveringEntity);
        Assert.Equal(MaterialKeyNormalizer.Normalize("FR-1"), with.MaterialKey);
        Assert.Contains("(TR IT,", with.Mapping);
    }

    [Fact]
    public void PaketB_wirkt_nicht_ohne_Lieferant_TR_IT_Lesart_A_bleibt()
    {
        var mappings = new MaterialMappingLookup([ItMapping()]);
        var costs = Costs(("M_IT01_000931", TrItArea, 25m));

        var result = Evaluate(Line(supplierName: null), costs, mappings);

        Assert.False(result.IsGroupCost);
        Assert.Equal(string.Empty, result.Mapping);
    }

    [Theory]
    [InlineData(MaterialMappingStatuses.Proposed, "TRFR")]
    [InlineData(MaterialMappingStatuses.Rejected, "TRFR")]
    [InlineData(MaterialMappingStatuses.Confirmed, "TRAT")]
    public void PaketB_unbestaetigt_oder_TRAT_wirkt_nicht(string status, string tsc)
    {
        var mapping = ItMapping(tsc: tsc, status: status);

        Assert.False(MaterialMappingStore.IsEffective(mapping));
        Assert.Equal(0, new MaterialMappingLookup([mapping]).Count);
    }

    [Theory]
    [InlineData("TR IT", true)]
    [InlineData("TR_IT", true)]
    [InlineData("tr in", true)]
    [InlineData("", false)]
    [InlineData("XY", false)]
    public void PaketB_Ziel_braucht_Gruppennummer_oder_Gesellschaft_mit_Nummer(string entity, bool expected)
        => Assert.Equal(expected, MaterialMappingStore.HasTarget(string.Empty, entity, "M_IT01_1"));

    [Fact]
    public async Task PaketB_Import_TR_IT_ohne_Nummer_abgelehnt_mit_Nummer_wirksam_unbekannte_Gesellschaft_abgelehnt()
    {
        await using var db = new AppDbContext(_options);
        var known = new HashSet<string>(["TRFR", "TRES"], StringComparer.OrdinalIgnoreCase);
        MaterialMappingImportRow Row(int n, string local, string entity, string number)
            => new(n, "TRFR", local, "Text", "", "", entity, number, "Hand", "", "", "bestaetigt", "");

        var bad = await MaterialMappingStore.ImportAsync(db, [Row(2, "FR-1", "TR IT", ""), Row(3, "FR-2", "XY", "1")], known, "koi", Now);
        Assert.False(bad.Success);
        Assert.Contains(bad.Errors, error => error.Contains("Zeile 2") && error.Contains("TR IT"));
        Assert.Contains(bad.Errors, error => error.Contains("Zeile 3") && error.Contains("unbekannt"));

        var good = await MaterialMappingStore.ImportAsync(db, [Row(2, "FR-1", "TR IT", "M_IT01_000931")], known, "koi", Now);
        Assert.True(good.Success);
        var stored = await db.MaterialMappings.SingleAsync();
        Assert.True(MaterialMappingStore.IsEffective(stored));
    }

    [Fact]
    public void PaketB_Vorschlag_gegen_TR_IT_fuer_Kandidaten_mit_Lieferant_Italien()
    {
        var candidates = new[]
        {
            new MaterialMappingCandidate("TRFR", "FR-1", "Druckschalter Industrie 24V Typ NA", "TR IT", 3, 900m, "EUR"),
            new MaterialMappingCandidate("TRUK", "UK-1", "Druckschalter Industrie 24V Typ NA", "TR AG", 1, 10m, "GBP")
        };
        var trIt = new Dictionary<string, string>(StringComparer.Ordinal) { ["MIT01000931"] = "Druckschalter Industrie 24V Typ NA" };

        var result = MaterialMappingSuggester.Suggest(candidates, new Dictionary<string, string>(StringComparer.Ordinal), Now, trIt);

        var suggestion = Assert.Single(result);
        Assert.Equal(("TRFR", string.Empty, "TR IT", "MIT01000931", MaterialMappingStatuses.Proposed),
            (suggestion.Tsc, suggestion.GroupMaterialNumber, suggestion.DeliveringEntity, suggestion.DeliveringMaterialNumber, suggestion.Status));
        Assert.Equal(MaterialMappingSuggester.TextSimilarityTrIt, suggestion.MethodDetail);
        Assert.False(MaterialMappingStore.IsEffective(suggestion));
    }

    [Fact]
    public void PaketB_TR_IT_Ziele_nur_mit_positiven_Kosten()
    {
        var records = new[]
        {
            new SalesRecord { Tsc = "TRIT", Material = "A", Name = "Mit Kosten" },
            new SalesRecord { Tsc = "TRIT", Material = "B", Name = "Ohne Kosten" },
            new SalesRecord { Tsc = "TRFR", Material = "C", Name = "Andere Gesellschaft" }
        };

        var result = MaterialMappingSuggestionService.BuildTrItMaterials(records, Costs(("A", TrItArea, 4m), ("B", TrItArea, 0m), ("C", TrItArea, 4m)));

        Assert.Equal(["A"], result.Keys.ToArray());
    }

    // ----- Paket D -----

    [Theory]
    [InlineData("TRCH", "", "", "", "Intern", "TR_AG", SupplierOrigins.ChAtRule)]
    [InlineData("TRIN", "LRD", "", "", "Intern", "TR_AG", SupplierOrigins.SalesTypeRule)]
    [InlineData("TRFR", "", "Trafag AG", "", "Intern", "TR_AG", SupplierOrigins.Source)]
    [InlineData("TRFR", "", "Trafag AG", SupplierSources.GoodsReceipt, "Intern", "TR_AG", SupplierOrigins.GoodsReceipt)]
    [InlineData("TRIT", "", "Fornitore", SupplierSources.OverrideList, "Extern", "", SupplierOrigins.OverrideList)]
    [InlineData("TRDE", "", "", "", "Intern", "TR_AG", SupplierOrigins.ChPlantMaster)]
    [InlineData("TRDE", "", "", "", "Lokal", "", SupplierOrigins.None)]
    public void PaketD_Supplier_Origin(string tsc, string salesType, string supplierName, string source, string supplierType,
        string entity, string expected)
        => Assert.Equal(expected, GroupMarginCalculator.ResolveSupplierOrigin(
            null, supplierName, null, tsc, salesType, source, supplierType, entity.Length == 0 ? null : entity));

    [Fact]
    public async Task PaketD_Uebergangsliste_setzt_die_Herkunft()
    {
        await using var db = new AppDbContext(_options);
        db.SupplierMaterialOverrides.Add(new SupplierMaterialOverride
        {
            Tsc = "TRIT", MaterialKey = "X1", SupplierNumber = "F1", SupplierName = "Fornitore", SupplierCountry = "IT"
        });
        await db.SaveChangesAsync();
        var record = new SalesRecord { Tsc = "TRIT", Material = "X1", SupplierFromGoodsReceipt = true, SupplierSource = SupplierSources.GoodsReceipt,
            SupplierName = "WE-Lieferant" };

        await SupplierMaterialOverrideStore.ApplyAsync(db, "TRIT", [record]);

        Assert.Equal((SupplierSources.OverrideList, "Fornitore"), (record.SupplierSource, record.SupplierName));
    }
}
