using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class B1GroupStandardCostBuilderTests
{
    private static readonly DateTime RefreshedAtUtc = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("TRIT", "TRIT", "EUR")]
    [InlineData("TRIN", "TRIN", "INR")]
    public void Build_UsesLatestPositiveB1CostPerMaterial(string tsc, string expectedArea, string currency)
    {
        var records = new[]
        {
            Row("000A1", 30m, currency, new DateTime(2026, 1, 10), documentEntry: 1),
            Row("000A1", 40m, currency, new DateTime(2026, 7, 10), documentEntry: 2),
            Row("000A1", 0m, currency, new DateTime(2026, 8, 10), documentEntry: 3),
            Row("000A2", -15m, currency, new DateTime(2026, 6, 1), documentEntry: 4)
        };

        var result = B1GroupStandardCostBuilder.Build(tsc, records, RefreshedAtUtc);

        Assert.Collection(result,
            cost => AssertCost(cost, "A1", expectedArea, 40m, currency),
            cost => AssertCost(cost, "A2", expectedArea, 15m, currency));
    }

    [Fact]
    public void Build_PrefersGroupMaterialNumber_AndRejectsWrongCurrency()
    {
        var records = new[]
        {
            Row("IC15415", 40m, "INR", new DateTime(2026, 7, 10), groupMaterialNumber: "008896.10.10"),
            Row("IC15415", 999m, "EUR", new DateTime(2026, 8, 10), groupMaterialNumber: "008896.10.10")
        };

        // Seit AP1 (19.11.2026) zusaetzlich unter der indischen Nummer; die falsche Waehrung bleibt in beiden draussen.
        var result = B1GroupStandardCostBuilder.Build("TRIN", records, RefreshedAtUtc);

        Assert.Collection(result,
            cost => AssertCost(cost, "8896.10.10", "TRIN", 40m, "INR"),
            cost => AssertCost(cost, "IC15415", "TRIN", 40m, "INR"));
    }

    [Fact]
    public void Build_TRIN_StoresCostsAlsoUnderIndianNumber_PrimaryKeyWins()
    {
        // AP1, Entscheid Ingo 19.11.2026: UK/US verkaufen unter der indischen Nummer (TS000095), Indien fuehrt den Preis unter
        // der Sachnummer (35560). Kollidiert eine indische Nummer mit einer Sachnummer, gewinnt die Sachnummer.
        var records = new[]
        {
            Row("TS000095", 120m, "INR", new DateTime(2026, 7, 10), documentEntry: 1, groupMaterialNumber: "35560"),
            Row("77777", 50m, "INR", new DateTime(2026, 7, 11), documentEntry: 2, groupMaterialNumber: "88888"),
            Row("DM1", 9m, "INR", new DateTime(2026, 7, 12), documentEntry: 3, groupMaterialNumber: "77777"),
            Row("PS5", 30m, "INR", new DateTime(2026, 7, 13), documentEntry: 4)
        };

        var result = B1GroupStandardCostBuilder.Build("TRIN", records, RefreshedAtUtc)
            .ToDictionary(cost => cost.MaterialKey, cost => cost.UnitCost);

        Assert.Equal(120m, result["35560"]);
        Assert.Equal(120m, result["TS000095"]);
        Assert.Equal(9m, result["77777"]);       // Sachnummer von DM1, nicht die indische Nummer 77777 (50)
        Assert.Equal(50m, result["88888"]);
        Assert.Equal(9m, result["DM1"]);
        Assert.Equal(30m, result["PS5"]);
        Assert.Equal(6, result.Count);
    }

    [Fact]
    public void Build_TRIT_HasNoSecondaryKey()
    {
        var records = new[] { Row("IT-LOCAL", 10m, "EUR", new DateTime(2026, 7, 10), groupMaterialNumber: "53513") };

        var cost = Assert.Single(B1GroupStandardCostBuilder.Build("TRIT", records, RefreshedAtUtc));

        AssertCost(cost, "53513", "TRIT", 10m, "EUR");
    }

    [Fact]
    public void Build_AveragesPositiveB1Costs_WhenFinanceSwitchIsSet()
    {
        var records = new[]
        {
            Row("A1", 30m, "EUR", new DateTime(2026, 1, 10), documentEntry: 1),
            Row("A1", 60m, "EUR", new DateTime(2026, 7, 10), documentEntry: 2),
            Row("A1", 0m, "EUR", new DateTime(2026, 8, 10), documentEntry: 3)
        };

        var cost = Assert.Single(B1GroupStandardCostBuilder.Build(
            "TRIT", records, RefreshedAtUtc, B1GroupStandardCostModes.AveragePositive));

        AssertCost(cost, "A1", "TRIT", 45m, "EUR");
    }

    [Fact]
    public void Build_IgnoresSitesWithoutOwnGroupCostSource()
    {
        Assert.Empty(B1GroupStandardCostBuilder.Build(
            "TRFR", [Row("A1", 30m, "EUR", new DateTime(2026, 7, 10))], RefreshedAtUtc));
    }

    [Fact]
    public async Task ReplaceAsync_ReplacesOnlyTheEntityArea()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.GroupStandardCosts.AddRange(
            Existing("OLD-IT", "TRIT", 1m, "EUR"),
            Existing("KEEP-CH", "1100", 2m, "CHF"));
        await db.SaveChangesAsync();

        var result = await B1GroupStandardCostStore.ReplaceAsync(
            db, "TRIT", [Row("NEW-IT", 55m, "EUR", new DateTime(2026, 8, 1))], RefreshedAtUtc);

        Assert.True(result.Updated);
        Assert.Equal(1, result.MaterialCount);
        var stored = await db.GroupStandardCosts.AsNoTracking().OrderBy(cost => cost.ValuationArea).ToListAsync();
        Assert.Collection(stored,
            cost => AssertCost(cost, "KEEP-CH", "1100", 2m, "CHF"),
            cost => AssertCost(cost, "NEW-IT", "TRIT", 55m, "EUR"));
    }

    [Fact]
    public async Task ReplaceAsync_PreservesExistingAreaWhenSourceHasNoPositiveCosts()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.GroupStandardCosts.Add(Existing("KEEP-IN", "TRIN", 40m, "INR"));
        await db.SaveChangesAsync();

        var result = await B1GroupStandardCostStore.ReplaceAsync(
            db, "TRIN", [Row("KEEP-IN", 0m, "INR", new DateTime(2026, 8, 1))], RefreshedAtUtc);

        Assert.False(result.Updated);
        var stored = Assert.Single(await db.GroupStandardCosts.AsNoTracking().ToListAsync());
        AssertCost(stored, "KEEP-IN", "TRIN", 40m, "INR");
    }

    private static SalesRecord Row(
        string material,
        decimal unitCost,
        string currency,
        DateTime postingDate,
        int documentEntry = 1,
        string groupMaterialNumber = "")
        => new()
        {
            Material = material,
            GroupMaterialNumber = groupMaterialNumber,
            StandardCost = unitCost,
            StandardCostCurrency = currency,
            CompanyCurrency = currency,
            PostingDate = postingDate,
            InvoiceDate = postingDate,
            ExtractionDate = postingDate.AddHours(1),
            DocumentEntry = documentEntry,
            PositionOnInvoice = 1,
            SourceLineId = $"{documentEntry}-1"
        };

    private static GroupStandardCost Existing(
        string materialKey, string area, decimal unitCost, string currency)
        => new()
        {
            MaterialKey = materialKey,
            ValuationArea = area,
            UnitCost = unitCost,
            Currency = currency,
            RefreshedAtUtc = RefreshedAtUtc
        };

    private static void AssertCost(
        GroupStandardCost actual,
        string materialKey,
        string area,
        decimal unitCost,
        string currency)
    {
        Assert.Equal(materialKey, actual.MaterialKey);
        Assert.Equal(area, actual.ValuationArea);
        Assert.Equal(unitCost, actual.UnitCost);
        Assert.Equal(currency, actual.Currency);
        Assert.Equal(RefreshedAtUtc, actual.RefreshedAtUtc);
    }
}
