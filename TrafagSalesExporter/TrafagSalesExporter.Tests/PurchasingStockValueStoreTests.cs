using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Sichert den Speicher des Lagerwerts ab. Er ist die Antwort auf den Befund vom 2026-08-24:
/// der gelesene Wert lag nur im Arbeitsspeicher und war nach jedem Neustart des IIS-Workers
/// verloren, weshalb die KPI-Kachel praktisch jeden Morgen auf "wartet auf Einkauf-Lauf" stand.
/// </summary>
public class PurchasingStockValueStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;
    private readonly PurchasingStockValueStore _store;

    public PurchasingStockValueStoreTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var command = _connection.CreateCommand())
        {
            command.CommandText = DatabaseSchemaSql.GetPurchasingStockValueCacheCreateSql();
            command.ExecuteNonQuery();
        }

        _dbFactory = new TestDbContextFactory(options);
        _store = new PurchasingStockValueStore(_dbFactory);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task LoadAsync_Ohne_Gespeicherten_Stand_Liefert_Null()
    {
        Assert.Null(await _store.LoadAsync("1100"));
    }

    [Fact]
    public async Task SaveAsync_Dann_LoadAsync_Liefert_Denselben_Stand()
    {
        var readAt = new DateTime(2026, 8, 24, 5, 30, 0, DateTimeKind.Utc);
        var snapshot = new StockValueSnapshot("1100",
        [
            new StockValueByPlannerRow("001", 1234.56m, 10.5m, 3),
            new StockValueByPlannerRow("002", 99.99m, 2m, 1)
        ], readAt);

        await _store.SaveAsync(snapshot);
        var loaded = await _store.LoadAsync("1100");

        Assert.NotNull(loaded);
        Assert.Equal("1100", loaded!.ValuationArea);
        Assert.Equal(readAt, loaded.ReadAtUtc);
        Assert.Equal(DateTimeKind.Utc, loaded.ReadAtUtc.Kind);
        Assert.Equal(1334.55m, loaded.TotalValue);
        Assert.Equal(4, loaded.TotalMaterialCount);

        var first = loaded.Rows[0];
        Assert.Equal("001", first.Planner);
        Assert.Equal(1234.56m, first.Value);
        Assert.Equal(10.5m, first.Quantity);
        Assert.Equal(3, first.MaterialCount);
    }

    [Fact]
    public async Task LoadAsync_Sortiert_Absteigend_Nach_Wert()
    {
        var snapshot = new StockValueSnapshot("1100",
        [
            new StockValueByPlannerRow("001", 10m, 1m, 1),
            new StockValueByPlannerRow("002", 500m, 1m, 1),
            new StockValueByPlannerRow("003", 100m, 1m, 1)
        ], DateTime.UtcNow);

        await _store.SaveAsync(snapshot);
        var loaded = await _store.LoadAsync("1100");

        Assert.NotNull(loaded);
        Assert.Equal(["002", "003", "001"], loaded!.Rows.Select(row => row.Planner));
    }

    [Fact]
    public async Task SaveAsync_Ersetzt_Den_Vorherigen_Stand_Vollstaendig()
    {
        await _store.SaveAsync(new StockValueSnapshot("1100",
        [
            new StockValueByPlannerRow("001", 10m, 1m, 1),
            new StockValueByPlannerRow("009", 20m, 1m, 1)
        ], new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc)));

        await _store.SaveAsync(new StockValueSnapshot("1100",
        [
            new StockValueByPlannerRow("001", 30m, 1m, 1)
        ], new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc)));

        var loaded = await _store.LoadAsync("1100");

        Assert.NotNull(loaded);
        // Der Disponent 009 aus dem alten Stand darf NICHT stehenbleiben, sonst waere die
        // Summe eine Mischung aus zwei Lesezeitpunkten.
        Assert.Single(loaded!.Rows);
        Assert.Equal(30m, loaded.TotalValue);
        Assert.Equal(new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc), loaded.ReadAtUtc);
    }

    [Fact]
    public async Task SaveAsync_Laesst_Andere_Bewertungskreise_Unberuehrt()
    {
        await _store.SaveAsync(new StockValueSnapshot("1200",
            [new StockValueByPlannerRow("001", 777m, 1m, 1)], DateTime.UtcNow));
        await _store.SaveAsync(new StockValueSnapshot("1100",
            [new StockValueByPlannerRow("001", 111m, 1m, 1)], DateTime.UtcNow));

        var austria = await _store.LoadAsync("1200");
        var switzerland = await _store.LoadAsync("1100");

        Assert.Equal(777m, austria!.TotalValue);
        Assert.Equal(111m, switzerland!.TotalValue);
    }

    [Fact]
    public async Task SaveAsync_Ohne_Bewertungskreis_Wird_Abgewiesen()
    {
        var snapshot = new StockValueSnapshot("  ",
            [new StockValueByPlannerRow("001", 1m, 1m, 1)], DateTime.UtcNow);

        await Assert.ThrowsAsync<ArgumentException>(() => _store.SaveAsync(snapshot));
    }

    [Fact]
    public async Task LoadAsync_Ohne_Bewertungskreis_Liefert_Null()
    {
        Assert.Null(await _store.LoadAsync(string.Empty));
    }

    [Fact]
    public async Task LoadAsync_Liefert_Null_Wenn_Der_Lesezeitpunkt_Unlesbar_Ist()
    {
        // Ein Wert ohne einordenbaren Zeitpunkt wird nicht angezeigt. Ein erfundenes "jetzt"
        // waere schlicht falsch, und ein Betrag ohne Datum in der Kachel ist wertlos.
        using var command = _connection.CreateCommand();
        command.CommandText = @"
INSERT INTO PurchasingStockValueCache
    (ValuationArea, Planner, Value, Quantity, MaterialCount, ReadAtUtc)
VALUES ('1100', '001', '500', '1', 1, 'kein Datum');";
        command.ExecuteNonQuery();

        Assert.Null(await _store.LoadAsync("1100"));
    }

    [Fact]
    public async Task SaveAsync_Ueberlebt_Einen_Doppelten_Disponenten_In_Der_Eingabe()
    {
        // Der Aggregator liefert je Disponent nur eine Zeile. Sollte sich das je aendern, darf
        // der Einkauf-Lauf nicht an einem Schluesselkonflikt abbrechen.
        var snapshot = new StockValueSnapshot("1100",
        [
            new StockValueByPlannerRow("001", 10m, 1m, 1),
            new StockValueByPlannerRow("001", 20m, 2m, 2)
        ], DateTime.UtcNow);

        await _store.SaveAsync(snapshot);
        var loaded = await _store.LoadAsync("1100");

        Assert.Single(loaded!.Rows);
        Assert.Equal(20m, loaded.TotalValue);
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
