using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Die Felder aus dem Artikelstamm der Quelle muessen den Schreibweg ueberleben. Der Schreibweg
/// ist ein Bulk-INSERT mit ausdruecklicher Spaltenliste - ein neues Feld am Modell allein genuegt
/// dort nicht, es fehlt sonst still in der Datenbank.
/// </summary>
public class CentralSalesRecordSalesTypeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;

    public CentralSalesRecordSalesTypeTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        _dbFactory = new TestDbContextFactory(options);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task ReplaceForSiteAsync_speichert_SalesType_und_Trafag_Sachnummer()
    {
        var site = new Site
        {
            Id = 1,
            Schema = "TRAFAG_LIVE",
            TSC = "TRIN",
            Land = "Indien",
            SourceSystem = "SAGE",
            IsActive = true
        };

        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.Sites.Add(site);
            await db.SaveChangesAsync();
        }

        var service = new CentralSalesRecordService(_dbFactory, new NullAppEventLogService());
        await service.ReplaceForSiteAsync(site, [
            new SalesRecord
            {
                ExtractionDate = new DateTime(2026, 8, 5),
                Tsc = "TRIN",
                DocumentEntry = 1,
                InvoiceNumber = "IN-1",
                PositionOnInvoice = 1,
                Material = "PT000003",
                Name = "EPR10.0A(57291)-8283",
                Quantity = 1m,
                SalesType = "LRD",
                GroupMaterialNumber = "57291"
            },
            new SalesRecord
            {
                ExtractionDate = new DateTime(2026, 8, 5),
                Tsc = "TRIN",
                DocumentEntry = 2,
                InvoiceNumber = "IN-2",
                PositionOnInvoice = 1,
                Material = "DM000001",
                Name = "DENSITYMONITOR_59831",
                Quantity = 1m,
                SalesType = "FFM",
                GroupMaterialNumber = "59831"
            }
        ]);

        await using var check = await _dbFactory.CreateDbContextAsync();
        var stored = await check.CentralSalesRecords
            .AsNoTracking()
            .OrderBy(r => r.DocumentEntry)
            .ToListAsync();

        Assert.Equal(2, stored.Count);
        Assert.Equal("LRD", stored[0].SalesType);
        Assert.Equal("57291", stored[0].GroupMaterialNumber);
        Assert.Equal("FFM", stored[1].SalesType);
        Assert.Equal("59831", stored[1].GroupMaterialNumber);
    }

    [Fact]
    public async Task ReplaceForSiteAsync_speichert_leere_Felder_als_leer_nicht_als_NULL()
    {
        // Standorte, deren Quelle die Felder nicht fuehrt (Italien, Frankreich, USA), liefern
        // leere Werte - die Spalten sind NOT NULL.
        var site = new Site
        {
            Id = 1,
            Schema = "it01_p",
            TSC = "TRIT",
            Land = "Italien",
            SourceSystem = "BI1",
            IsActive = true
        };

        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.Sites.Add(site);
            await db.SaveChangesAsync();
        }

        var service = new CentralSalesRecordService(_dbFactory, new NullAppEventLogService());
        await service.ReplaceForSiteAsync(site, [
            new SalesRecord
            {
                ExtractionDate = new DateTime(2026, 8, 5),
                Tsc = "TRIT",
                DocumentEntry = 1,
                InvoiceNumber = "IT-1",
                PositionOnInvoice = 1,
                Material = "M_IT01_000971",
                Quantity = 1m
            }
        ]);

        await using var check = await _dbFactory.CreateDbContextAsync();
        var stored = await check.CentralSalesRecords.AsNoTracking().SingleAsync();

        Assert.Equal(string.Empty, stored.SalesType);
        Assert.Equal(string.Empty, stored.GroupMaterialNumber);
    }

    private sealed class NullAppEventLogService : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }
}
