using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Prueft die drei Regeln, die das Musterverfahren tragbar machen: es schlaegt nur vor,
/// es ueberschreibt keinen menschlichen Entscheid, und ein Widerspruch wird gemeldet
/// statt verschluckt.
/// </summary>
public class MarketSegmentPatternServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public MarketSegmentPatternServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _factory = new SingleConnectionFactory(options);
        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private MarketSegmentPatternService Service() => new(_factory, new NullEventLog());

    private void Seed(params (string Tsc, string Number, string Name)[] customers)
    {
        using var db = _factory.CreateDbContext();
        var siteIds = new Dictionary<string, int>();
        foreach (var tsc in customers.Select(c => c.Tsc).Distinct())
        {
            var site = new Site { TSC = tsc, Land = tsc, IsActive = true };
            db.Sites.Add(site);
            db.SaveChanges();
            siteIds[tsc] = site.Id;
        }

        foreach (var c in customers)
            db.CentralSalesRecords.Add(new CentralSalesRecord
            {
                SiteId = siteIds[c.Tsc],
                Tsc = c.Tsc,
                CustomerNumber = c.Number,
                CustomerName = c.Name,
                InvoiceNumber = "RE1",
                Material = "M1"
            });

        db.SaveChanges();
    }

    private async Task AddPatternAsync(string pattern, string segment, string tsc = "")
        => await Service().SavePatternAsync(new SegmentNamePattern
        {
            Pattern = pattern,
            Segment = segment,
            Tsc = tsc,
            IsActive = true
        });

    [Fact]
    public async Task PatternProposesButNeverConfirms()
    {
        Seed(("TRFR", "C1", "SIEMENS MOBILITY SAS"));
        await AddPatternAsync("siemens mobility", "Railway");

        var written = await Service().ApplyAsync();
        Assert.Equal(1, written);

        using var db = _factory.CreateDbContext();
        var row = Assert.Single(db.CustomerMarketSegments);
        Assert.Equal("Railway", row.Segment);
        Assert.False(row.IsConfirmed);
        Assert.Contains("Namensmuster", row.ProposalNote);
    }

    [Fact]
    public async Task ConfirmedAssignmentIsNeverOverwritten()
    {
        Seed(("TRFR", "C1", "SIEMENS MOBILITY SAS"));
        using (var db = _factory.CreateDbContext())
        {
            db.CustomerMarketSegments.Add(new CustomerMarketSegment
            {
                Tsc = "TRFR",
                CustomerNumber = "C1",
                CustomerName = "SIEMENS MOBILITY SAS",
                Segment = "Shipbuilding",
                IsConfirmed = true,
                Source = "Mensch",
                UpdatedAtUtc = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        await AddPatternAsync("siemens mobility", "Railway");

        var preview = await Service().PreviewAsync();
        var hit = Assert.Single(preview.Hits);
        Assert.Equal(SegmentPatternHitKind.KonfliktMitBestaetigung, hit.Kind);
        Assert.Equal("Shipbuilding", hit.ExistingSegment);

        Assert.Equal(0, await Service().ApplyAsync());

        using var after = _factory.CreateDbContext();
        var row = Assert.Single(after.CustomerMarketSegments);
        Assert.Equal("Shipbuilding", row.Segment);
        Assert.True(row.IsConfirmed);
    }

    [Fact]
    public async Task PatternRestrictedToOneSiteDoesNotLeakIntoOthers()
    {
        Seed(("TRDE", "10001", "Voith Turbo"), ("ZSCHWEIZ", "9653", "Voith Hydro"));
        await AddPatternAsync("voith", "Railway", "TRDE");

        var preview = await Service().PreviewAsync();
        var hit = Assert.Single(preview.Hits);
        Assert.Equal("TRDE", hit.Tsc);
    }

    [Fact]
    public async Task TooShortPatternIsRefused()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => Service().SavePatternAsync(new SegmentNamePattern { Pattern = "ab", Segment = "Railway" }));
        Assert.Contains("drei Zeichen", error.Message);
    }

    [Fact]
    public async Task RunningTwiceDoesNotDuplicateProposals()
    {
        Seed(("TRFR", "C1", "ALSTOM BELFORT"));
        await AddPatternAsync("alstom", "Railway");

        Assert.Equal(1, await Service().ApplyAsync());
        Assert.Equal(0, await Service().ApplyAsync());

        using var db = _factory.CreateDbContext();
        Assert.Single(db.CustomerMarketSegments);
    }

    [Fact]
    public async Task PreviewSeparatesNewFromAlreadyProposed()
    {
        Seed(("TRFR", "C1", "ALSTOM BELFORT"), ("TRIT", "C2", "ALSTOM FERROVIARIA"));
        await AddPatternAsync("alstom", "Railway");

        var before = await Service().PreviewAsync();
        Assert.Equal(2, before.Neu);
        Assert.Equal(0, before.BereitsVorgeschlagen);

        await Service().ApplyAsync();

        var after = await Service().PreviewAsync();
        Assert.Equal(0, after.Neu);
        Assert.Equal(2, after.BereitsVorgeschlagen);
    }

    private sealed class SingleConnectionFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }

    private sealed class NullEventLog : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info",
            int? siteId = null, string? land = null, string? details = null) => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message,
            int? siteId = null, string? land = null, string? details = null) => Task.CompletedTask;
    }
}
