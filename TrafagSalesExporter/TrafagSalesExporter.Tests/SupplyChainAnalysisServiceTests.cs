using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class SupplyChainAnalysisServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SupplyChainAnalysisService _service;

    public SupplyChainAnalysisServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Execute(DatabaseSchemaSql.GetPurchasingEkkoCacheCreateSql());
        Execute(DatabaseSchemaSql.GetPurchasingEkpoCacheCreateSql());
        Execute(DatabaseSchemaSql.GetPurchasingEketCacheCreateSql());
        Execute(DatabaseSchemaSql.GetMaterialUsageCacheCreateSql());
        Execute(DatabaseSchemaSql.GetPurchasingProductGroupMapCreateSql());
        Execute(DatabaseSchemaSql.GetPurchasingSpendDisponentRuleCreateSql());

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _service = new SupplyChainAnalysisService(new TestDbContextFactory(options));
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task MaterialDisposition_Deduplicates_ComponentStock_And_Uses_Additive_ProductGroupFallback()
    {
        SeedUsage("M-1", "K-1", "019", "-5", "10");
        SeedUsage("M-2", "K-1", "019", "-5", "10");
        Execute("INSERT INTO PurchasingSpendDisponentRule (DisponentPattern, ProductGroup, ProductGroupText) VALUES ('019', 'PG1', 'Hybrid');");

        var result = await _service.LoadAsync(
            SupplyChainAnalysisKind.MaterialDisposition,
            new SupplyChainAnalysisFilter(OnlyActionable: true));

        var row = Assert.Single(result.Rows);
        Assert.Equal("P1", row.RiskCode);
        Assert.Equal(5m, row.ShortageQuantity);
        Assert.Equal(50m, row.ShortageValueChf);
        Assert.Equal(2, row.ParentMaterialCount);
        Assert.Equal("PG1 - Hybrid", row.ProductGroup);
    }

    [Fact]
    public async Task PurchaseCoverage_Shows_OpenAndOverdueSchedule_WithoutAddingItToFinalStock()
    {
        SeedUsage("M-1", "K-1", "019", "-5", "10");
        SeedPurchase("PO-1", "K-1", "Supplier A", 10m, 100m, 8m, 2m, DateTime.Today.AddDays(-2));

        var result = await _service.LoadAsync(
            SupplyChainAnalysisKind.PurchaseCoverage,
            new SupplyChainAnalysisFilter(OnlyActionable: true));

        var row = Assert.Single(result.Rows);
        Assert.Equal(-5m, row.FinalStock);
        Assert.Equal(6m, row.OpenOrderQuantity);
        Assert.Equal(6m, row.OverdueQuantity);
        Assert.Equal(60m, row.OpenOrderValueChf);
        Assert.Contains("trotz", row.RiskDe, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Filter_IsAppliedBeforeKpisAndDetailRows()
    {
        SeedUsage("M-1", "K-1", "019", "-5", "10");
        SeedUsage("M-2", "K-2", "020", "-7", "2");

        var result = await _service.LoadAsync(
            SupplyChainAnalysisKind.MaterialDisposition,
            new SupplyChainAnalysisFilter(Search: "K-2", OnlyActionable: true));

        var row = Assert.Single(result.Rows);
        Assert.Equal("K-2", row.Material);
        Assert.Equal(1, result.FilteredMaterialCount);
        Assert.Equal("1", result.Kpis[0].Value);
        Assert.Equal(14m, row.ShortageValueChf);
    }

    [Fact]
    public async Task DeliveryPerformance_ReportsPlannedRisk_ButNeverPretendsActualReceiptDateExists()
    {
        SeedPurchase("PO-1", "K-1", "Supplier A", 10m, 100m, 8m, 2m, DateTime.Today.AddDays(-2));

        var result = await _service.LoadAsync(
            SupplyChainAnalysisKind.DeliveryPerformance,
            new SupplyChainAnalysisFilter(OnlyActionable: true));

        var row = Assert.Single(result.Rows);
        Assert.Equal("P1", row.RiskCode);
        Assert.False(result.ActualReceiptDateAvailable);
        Assert.Contains("Ist-Wareneingangsdatum", result.NoticeDe, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("0%", result.Kpis[3].Value);
    }

    [Fact]
    public async Task PlanningAudit_CreatesSeparateReviewTasks_WithoutChangingSourceRows()
    {
        SeedUsage("M-1", "K-1", "019", "-5", "10", safetyStock: "0", reorderPoint: "0", mrpType: "");

        var result = await _service.LoadAsync(
            SupplyChainAnalysisKind.PlanningParameterAudit,
            new SupplyChainAnalysisFilter(OnlyActionable: true));

        Assert.Contains(result.Rows, row => row.IssueDe.Contains("Sicherheitsbestand", StringComparison.Ordinal));
        Assert.Contains(result.Rows, row => row.IssueDe.Contains("Meldebestand", StringComparison.Ordinal));
        Assert.Contains(result.Rows, row => row.IssueDe.Contains("Dispositionsmerkmal", StringComparison.Ordinal));
        Assert.All(result.Rows, row => Assert.Equal("K-1", row.Material));
    }

    [Fact]
    public async Task MaterialDependency_MarksSingleSupplierWithBroadBomImpact()
    {
        for (var i = 1; i <= 5; i++)
            SeedUsage($"M-{i}", "K-1", "019", "5", "10");
        SeedPurchase("PO-1", "K-1", "Supplier A", 10m, 100m, 10m, 10m, DateTime.Today);

        var result = await _service.LoadAsync(
            SupplyChainAnalysisKind.MaterialDependency,
            new SupplyChainAnalysisFilter(OnlyActionable: true));

        var row = Assert.Single(result.Rows);
        Assert.Equal("P1", row.RiskCode);
        Assert.Equal(1, row.SupplierCount);
        Assert.Equal(5, row.ParentMaterialCount);
        Assert.Equal(100m, row.TopSupplierSharePercent);
    }

    [Fact]
    public async Task MissingFinalStock_IsDataQualityIssue_NotARealZeroOrShortage()
    {
        SeedUsage("M-1", "K-1", "019", "", "10", safetyStock: "0", reorderPoint: "0");

        var result = await _service.LoadAsync(
            SupplyChainAnalysisKind.MaterialDisposition,
            new SupplyChainAnalysisFilter(OnlyActionable: true));

        var row = Assert.Single(result.Rows);
        Assert.Equal("P3", row.RiskCode);
        Assert.Contains("nicht geliefert", row.RiskDe, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0m, row.ShortageQuantity);
        Assert.Equal(0m, row.ShortageValueChf);
    }

    [Fact]
    public async Task MissingUnitCost_LeavesShortageValueUnknown_InsteadOfCountingAValuedZero()
    {
        // Gegenstueck zu MissingFinalStock_...: die Fehlmenge ist echt, aber ohne gepflegte
        // Stueckkosten ist ihr Wert unbekannt. Vorher lief die fehlende Zahl als 0 in die
        // Summe der Kachel "Fehlwert CHF", ohne dass irgendetwas darauf hinwies.
        SeedUsage("M-1", "K-1", "019", "-5", unitCost: "");

        var result = await _service.LoadAsync(
            SupplyChainAnalysisKind.MaterialDisposition,
            new SupplyChainAnalysisFilter(OnlyActionable: true));

        var row = Assert.Single(result.Rows);
        Assert.Equal(5m, row.ShortageQuantity);
        Assert.False(row.HasUnitCost);
        Assert.Contains("nicht bewertet", result.Kpis[2].DetailDe, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RiskBuckets_KeepCountingOkRows_EvenWhenOnlyActionableHidesThemFromTheTable()
    {
        // "Nur Handlungsbedarf" entfernt genau die OK-Zeilen, die der gruene Balken zaehlen
        // soll. Wuerden die Balken nach dem Schalter gezaehlt, stuende "Ohne akuten Hinweis"
        // beim Standardaufruf immer auf 0 und saehe wie eine Messung aus.
        SeedUsage("M-1", "K-1", "019", "-5", "10");
        SeedUsage("M-2", "K-2", "019", "10", "10");

        var result = await _service.LoadAsync(
            SupplyChainAnalysisKind.MaterialDisposition,
            new SupplyChainAnalysisFilter(OnlyActionable: true));

        var row = Assert.Single(result.Rows);
        Assert.Equal("K-1", row.Material);
        var ok = Assert.Single(result.RiskBuckets, bucket => bucket.LabelDe == "Ohne akuten Hinweis");
        Assert.Equal(1, ok.Count);
        // Suche/Disponent/Produktgruppe grenzen dagegen auch die Balken ein.
        var scoped = await _service.LoadAsync(
            SupplyChainAnalysisKind.MaterialDisposition,
            new SupplyChainAnalysisFilter(Search: "K-1", OnlyActionable: true));
        Assert.Equal(0, Assert.Single(scoped.RiskBuckets, bucket => bucket.LabelDe == "Ohne akuten Hinweis").Count);
    }

    private void SeedUsage(
        string header,
        string component,
        string dispatcher,
        string finalStock,
        string unitCost,
        string safetyStock = "2",
        string reorderPoint = "3",
        string mrpType = "PD")
    {
        Execute($@"
INSERT INTO MaterialUsageCache
    (Richtung, Vknr, VknrDispo, Kompnr, KompnrMaktx, Labst, FesteZugang, GeplZugang,
     FesteAbgang, GeplAbgang, Endbestand, Stueckkosten, Minbe, Eisbe, Dismm, Disls,
     Bstfe, Beskz, Mstae, Zzlzcod, Exklusiv, LastLoadedAtUtc)
VALUES
    ('TOPDOWN', '{header}', '{dispatcher}', '{component}', 'Test component', '1', '0', '0',
     '0', '0', '{finalStock}', '{unitCost}', '{reorderPoint}', '{safetyStock}', '{mrpType}', 'EX',
     '0', 'F', '', 'L1', 1, '2026-08-06T12:00:00Z');");
    }

    private void SeedPurchase(
        string order,
        string material,
        string supplier,
        decimal itemQuantity,
        decimal netValue,
        decimal scheduleQuantity,
        decimal receivedQuantity,
        DateTime plannedDate)
    {
        Execute($@"
INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, Bsart, Waers, Wkurs, LastLoadedAtUtc)
VALUES ('{order}', '2026-08-01', 'L1', '{supplier}', 'F', 'NB', 'CHF', '1', '2026-08-06T12:00:00Z');
INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Txz01, Menge, Netwr, LastLoadedAtUtc)
VALUES ('{order}', '10', '{material}', 'Test component', '{itemQuantity.ToString(System.Globalization.CultureInfo.InvariantCulture)}', '{netValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}', '2026-08-06T12:00:00Z');
INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc)
VALUES ('{order}', '10', '1', '{plannedDate:yyyy-MM-dd}', '{scheduleQuantity.ToString(System.Globalization.CultureInfo.InvariantCulture)}', '{receivedQuantity.ToString(System.Globalization.CultureInfo.InvariantCulture)}', '2026-08-06T12:00:00Z');");
    }

    private void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }
}
