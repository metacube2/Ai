using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>Anonyme Nutzungsmessung (2026-10-14).</summary>
public sealed class PageUsageTrackerTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    public PageUsageTrackerTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    private sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }

    [Theory]
    [InlineData("", "/")]
    [InlineData("einkauf/spend?jahr=2026", "einkauf/spend")]
    [InlineData("management-cockpit?section=status", "management-cockpit")]
    [InlineData("projekte/123/board", "projekte/{id}")]
    [InlineData("Operations/KPI/Liefertreue#x", "operations/kpi")]
    public void Seite_Wird_Normalisiert(string path, string expected) => Assert.Equal(expected, PageUsageTracker.NormalizeRoute(path));

    [Fact]
    public void Zaehlt_Aufrufe_Und_Personen_Ohne_Namen_Und_Ohne_Betreuer()
    {
        var factory = new Factory(_options);
        var day = new DateTime(2026, 10, 14, 9, 0, 0);
        PageUsageTracker.Record("TRAFAG\\Fabio", "einkauf", factory, day);
        PageUsageTracker.Record("TRAFAG\\Fabio", "einkauf?x=1", factory, day);
        PageUsageTracker.Record("TRAFAG\\Armin", "einkauf", factory, day);
        PageUsageTracker.Record("TRAFAG\\koi", "einkauf", factory, day);      // Betreuer zaehlt nicht
        PageUsageTracker.Record(null, "einkauf", factory, day);                // ohne Anmeldung nichts
        PageUsageTracker.Flush(factory);

        using var db = new AppDbContext(_options);
        var (rows, days) = PageUsageReport.Load(db, new DateOnly(2026, 10, 14), new Dictionary<string, string> { ["einkauf"] = "Einkauf Dashboard" });
        var einkauf = rows.Single(r => r.Route == "einkauf");
        Assert.Equal(("Einkauf Dashboard", 3, 2), (einkauf.Title, einkauf.Views30, einkauf.UserDays30));
        Assert.Equal(2, days.Single(d => d.Day == "2026-10-14").Users);

        // In der Datenbank stehen keine Namen, nur Schluessel
        var con = (SqliteConnection)db.Database.GetDbConnection();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT group_concat(UserKey, '|') FROM UsagePageUsers";
        var keys = (string)cmd.ExecuteScalar()!;
        Assert.DoesNotContain("fabio", keys, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("armin", keys, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => _connection.Dispose();
}
