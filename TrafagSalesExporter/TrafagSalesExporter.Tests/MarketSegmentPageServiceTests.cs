using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Pflege der Kunden-Segment-Zuordnung ueber die Weboberflaeche.
/// </summary>
public class MarketSegmentPageServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MarketSegmentPageServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
        // CentralSalesRecords haengt per Fremdschluessel an Sites.
        db.Sites.Add(new Site
        {
            Id = 1,
            HanaServerId = null,
            Schema = "test",
            TSC = "TEST",
            Land = "Testland",
            SourceSystem = "SAP",
            IsActive = true
        });
        db.SaveChanges();
        db.CentralSalesRecords.AddRange(
            Row("TRCH", "10042", "Stadler Rail AG", "CH", 100m, "Pressostat"),
            Row("TRCH", "10042", "Stadler Rail AG", "CH", 150m, "Pressostat"),
            Row("TRCH", "20001", "Andere AG", "CH", 50m, "Transmitter"),
            Row("TRIT", "10042", "Irgendwer SRL", "IT", 70m, "Transmitter"));
        db.SaveChanges();
    }

    private static CentralSalesRecord Row(
        string tsc, string customerNumber, string customerName, string country, decimal value, string family)
        => new()
        {
            SiteId = 1,
            Tsc = tsc,
            Land = country,
            CustomerNumber = customerNumber,
            CustomerName = customerName,
            CustomerCountry = country,
            SalesPriceValue = value,
            SalesCurrency = "CHF",
            ProductFamilyText = family,
            ExtractionDate = new DateTime(2026, 8, 13),
            DocumentType = "Invoice"
        };

    private MarketSegmentPageService CreateService()
        => new(new TestDbContextFactory(_options), new NullAppEventLog());

    [Fact]
    public async Task SearchCustomers_AggregatesByCustomerAndSortsByRows()
    {
        var service = CreateService();

        var rows = await service.SearchCustomersAsync(null, null, onlyAssigned: false);

        Assert.Equal(3, rows.Count);
        var top = rows[0];
        Assert.Equal("10042", top.CustomerNumber);
        Assert.Equal("TRCH", top.Tsc);
        Assert.Equal(2, top.SalesRows);
        Assert.Equal(250m, top.SalesValue);
        Assert.Equal(string.Empty, top.Segment);
    }

    [Fact]
    public async Task Assign_ThenSearch_ShowsSegmentForThatSiteOnly()
    {
        var service = CreateService();

        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Umfrage 2026-05");

        var rows = await service.SearchCustomersAsync(null, null, onlyAssigned: false);
        var swiss = rows.Single(r => r.Tsc == "TRCH" && r.CustomerNumber == "10042");
        var italian = rows.Single(r => r.Tsc == "TRIT" && r.CustomerNumber == "10042");

        Assert.Equal("Railway", swiss.Segment);
        Assert.Equal("Umfrage 2026-05", swiss.Source);
        // Gleiche Kundennummer, anderer Standort: bleibt unzugeordnet.
        Assert.Equal(string.Empty, italian.Segment);
    }

    [Fact]
    public async Task Assign_Twice_UpdatesInsteadOfDuplicating()
    {
        var service = CreateService();

        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "erste Quelle");
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Industrial", "zweite Quelle");

        await using var db = new AppDbContext(_options);
        var stored = db.CustomerMarketSegments.Where(x => x.CustomerNumber == "10042" && x.Tsc == "TRCH").ToList();
        Assert.Single(stored);
        Assert.Equal("Industrial", stored[0].Segment);
        Assert.Equal("zweite Quelle", stored[0].Source);
    }

    [Fact]
    public async Task Assign_WithoutSegment_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "  ", "Quelle"));
    }

    [Fact]
    public async Task Clear_RemovesAssignment()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Quelle");

        await service.ClearAsync("TRCH", "10042");

        var rows = await service.SearchCustomersAsync(null, null, onlyAssigned: false);
        Assert.All(rows, r => Assert.Equal(string.Empty, r.Segment));
    }

    [Fact]
    public async Task OnlyAssigned_FiltersToMaintainedCustomers()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Quelle");

        var rows = await service.SearchCustomersAsync(null, null, onlyAssigned: true);

        Assert.Single(rows);
        Assert.Equal("Railway", rows[0].Segment);
    }

    [Fact]
    public async Task Search_FiltersByNameAndSite()
    {
        var service = CreateService();

        var byName = await service.SearchCustomersAsync("Stadler", null, onlyAssigned: false);
        Assert.Single(byName);
        Assert.Equal("Stadler Rail AG", byName[0].CustomerName);

        var bySite = await service.SearchCustomersAsync(null, "TRIT", onlyAssigned: false);
        Assert.Single(bySite);
        Assert.Equal("TRIT", bySite[0].Tsc);
    }

    [Fact]
    public async Task Summary_CountsCustomersAndRowsPerSegment()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Quelle");
        await service.AssignAsync("TRIT", "10042", "Irgendwer SRL", "Railway", "Quelle");

        var summary = await service.GetSummaryAsync();

        var railway = Assert.Single(summary);
        Assert.Equal("Railway", railway.Segment);
        Assert.Equal(2, railway.Customers);
        Assert.Equal(3, railway.SalesRows);   // 2 Zeilen CH + 1 Zeile IT
    }

    [Fact]
    public async Task KnownSegments_ContainDefaultsAndUsedValues()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Sondermaschinen", "Quelle");

        var segments = await service.GetKnownSegmentsAsync();

        Assert.Contains("Railway", segments);
        Assert.Contains("Sondermaschinen", segments);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(options));
    }

    private sealed class NullAppEventLog : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }
}
