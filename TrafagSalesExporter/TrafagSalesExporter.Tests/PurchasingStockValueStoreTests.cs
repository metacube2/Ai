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
            command.CommandText = DatabaseSchemaSql.GetPurchasingStockValueHistoryCreateSql();
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

    // ---- Verlauf (Wunsch Einkauf September 2026: Lagerwert woechentlich als Trend) ----

    private static readonly string[] Planners = ["001", "002", "003", "004", "005"];

    private static StockValueSnapshot Snapshot(DateTime readAtUtc, params StockValueByPlannerRow[] rows)
        => new("1100", rows, readAtUtc);

    [Fact]
    public async Task LoadHistoryAsync_Ohne_Verlauf_Ist_Leer()
    {
        Assert.Empty(await _store.LoadHistoryAsync("1100"));
    }

    [Fact]
    public async Task SaveAsync_Zweimal_Am_Selben_Tag_Ergibt_Einen_Tagesstand_Mit_Dem_Neueren_Wert()
    {
        await _store.SaveAsync(Snapshot(new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc),
            new StockValueByPlannerRow("001", 100m, 1m, 1),
            new StockValueByPlannerRow("009", 50m, 1m, 1)));
        await _store.SaveAsync(Snapshot(new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc),
            new StockValueByPlannerRow("001", 120m, 1m, 1)));

        var history = await _store.LoadHistoryAsync("1100");

        var day = Assert.Single(history);
        Assert.Equal(new DateOnly(2026, 9, 28), day.SnapshotDate);
        // Der Disponent 009 aus dem ersten Lauf darf nicht stehenbleiben, sonst mischt der
        // Tagesstand zwei Lesezeitpunkte.
        var row = Assert.Single(day.Rows);
        Assert.Equal(120m, row.Value);
        Assert.Equal(new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc), day.ReadAtUtc);
    }

    [Fact]
    public async Task SaveAsync_An_Verschiedenen_Tagen_Behaelt_Den_Verlauf_Und_Ersetzt_Nur_Die_Kachel()
    {
        await _store.SaveAsync(Snapshot(new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc),
            new StockValueByPlannerRow("001", 100m, 1m, 1)));
        await _store.SaveAsync(Snapshot(new DateTime(2026, 9, 29, 10, 0, 0, DateTimeKind.Utc),
            new StockValueByPlannerRow("001", 110m, 1m, 1)));

        var history = await _store.LoadHistoryAsync("1100");
        var tile = await _store.LoadAsync("1100");

        Assert.Equal([new DateOnly(2026, 9, 28), new DateOnly(2026, 9, 29)], history.Select(day => day.SnapshotDate));
        Assert.Equal(110m, tile!.TotalValue);
    }

    [Fact]
    public async Task SaveAsync_Ordnet_Einen_Lauf_Nach_Mitternacht_Schweizer_Zeit_Dem_Neuen_Tag_Zu()
    {
        // 22:30 UTC im Sommer ist 00:30 in Zuerich, also schon der 29.09.
        await _store.SaveAsync(Snapshot(new DateTime(2026, 9, 28, 22, 30, 0, DateTimeKind.Utc),
            new StockValueByPlannerRow("001", 100m, 1m, 1)));

        var day = Assert.Single(await _store.LoadHistoryAsync("1100"));
        Assert.Equal(new DateOnly(2026, 9, 29), day.SnapshotDate);
    }

    [Fact]
    public async Task SeedHistoryFromCache_Uebernimmt_Den_Bestehenden_Stand_Einmal()
    {
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = @"
INSERT INTO PurchasingStockValueCache
    (ValuationArea, Planner, Value, Quantity, MaterialCount, ReadAtUtc)
VALUES ('1100', '001', '500.5', '1', 7, '2026-09-25T10:00:00.0000000Z');";
            command.ExecuteNonQuery();
        }

        PurchasingStockValueStore.SeedHistoryFromCache(_connection);
        PurchasingStockValueStore.SeedHistoryFromCache(_connection);

        var day = Assert.Single(await _store.LoadHistoryAsync("1100"));
        Assert.Equal(new DateOnly(2026, 9, 25), day.SnapshotDate);
        var row = Assert.Single(day.Rows);
        Assert.Equal(500.5m, row.Value);
        Assert.Equal(7, row.MaterialCount);
    }

    [Fact]
    public async Task SeedHistoryFromCache_Ueberschreibt_Keinen_Vorhandenen_Tagesstand()
    {
        await _store.SaveAsync(Snapshot(new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc),
            new StockValueByPlannerRow("001", 999m, 1m, 1)));
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = "UPDATE PurchasingStockValueCache SET Value = '1';";
            command.ExecuteNonQuery();
        }

        PurchasingStockValueStore.SeedHistoryFromCache(_connection);

        var day = Assert.Single(await _store.LoadHistoryAsync("1100"));
        Assert.Equal(999m, Assert.Single(day.Rows).Value);
    }

    [Fact]
    public void ToWeekly_Nimmt_Den_Letzten_Tag_Der_Woche_Und_Filtert_Die_Disponenten()
    {
        var days = new[]
        {
            // KW 40/2026: Montag 28.09. bis Sonntag 04.10.
            new StockValueHistoryDay(new DateOnly(2026, 9, 28), new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc),
                [new StockValueByPlannerRow("001", 100m, 1m, 1)]),
            new StockValueHistoryDay(new DateOnly(2026, 10, 2), new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc),
                [new StockValueByPlannerRow("001", 150m, 1m, 3), new StockValueByPlannerRow("002", 50m, 1m, 2),
                 new StockValueByPlannerRow("099", 10_000m, 1m, 9)]),
            // KW 41/2026
            new StockValueHistoryDay(new DateOnly(2026, 10, 5), new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc),
                [new StockValueByPlannerRow("001", 210m, 1m, 4)])
        };

        var weekly = StockValueHistory.ToWeekly(days, Planners);

        Assert.Equal(2, weekly.Count);
        Assert.Equal((2026, 40), (weekly[0].IsoYear, weekly[0].IsoWeek));
        Assert.Equal(new DateOnly(2026, 10, 2), weekly[0].SnapshotDate);
        // 099 gehoert nicht zu den Einkaufsteilen und darf nicht in die Summe.
        Assert.Equal(200m, weekly[0].Value);
        Assert.Equal(5, weekly[0].MaterialCount);
        Assert.Equal((2026, 41), (weekly[1].IsoYear, weekly[1].IsoWeek));
        Assert.Equal(210m, weekly[1].Value);
    }

    [Fact]
    public void ToWeekly_Ordnet_Den_Jahreswechsel_Nach_ISO_Wochen_Zu()
    {
        var readAt = new DateTime(2027, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var days = new[]
        {
            new StockValueHistoryDay(new DateOnly(2026, 12, 31), readAt, [new StockValueByPlannerRow("001", 1m, 1m, 1)]),
            new StockValueHistoryDay(new DateOnly(2027, 1, 1), readAt, [new StockValueByPlannerRow("001", 2m, 1m, 1)]),
            new StockValueHistoryDay(new DateOnly(2027, 1, 4), readAt, [new StockValueByPlannerRow("001", 3m, 1m, 1)])
        };

        var weekly = StockValueHistory.ToWeekly(days, Planners);

        // Der 31.12.2026 und der 01.01.2027 liegen beide in KW 53/2026; das Kalenderjahr waere
        // hier falsch und wuerde die Woche in zwei Punkte zerreissen.
        Assert.Equal(2, weekly.Count);
        Assert.Equal((2026, 53, 2m), (weekly[0].IsoYear, weekly[0].IsoWeek, weekly[0].Value));
        Assert.Equal((2027, 1, 3m), (weekly[1].IsoYear, weekly[1].IsoWeek, weekly[1].Value));
    }

    [Fact]
    public void ToWeekly_Laesst_Wochen_Ohne_Lauf_Aus_Statt_Zu_Interpolieren()
    {
        var readAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var days = new[]
        {
            new StockValueHistoryDay(new DateOnly(2026, 9, 28), readAt, [new StockValueByPlannerRow("001", 1m, 1m, 1)]),
            new StockValueHistoryDay(new DateOnly(2026, 10, 14), readAt, [new StockValueByPlannerRow("001", 3m, 1m, 1)])
        };

        var weekly = StockValueHistory.ToWeekly(days, Planners);

        Assert.Equal([40, 42], weekly.Select(point => point.IsoWeek));
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
