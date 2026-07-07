using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class PurchasingDashboardServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;
    private readonly PurchasingDashboardService _service;

    public PurchasingDashboardServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        CreatePurchasingCacheTables();

        _dbFactory = new TestDbContextFactory(options);
        _service = new PurchasingDashboardService(_dbFactory);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task LoadAsync_Excludes_Loekz_And_MaraMstae_98_99_From_Spend_When_DeletionFlagFilterActive()
    {
        await SeedAsync();

        var filter = new PurchasingDashboardFilter(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 31),
            ExcludeDeletedItems: true);

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Nur die aktive Position (Loekz leer, Mstae leer) bleibt: Netwr 100.
        Assert.Equal(100m, state.SpendChfSample);
    }

    [Fact]
    public async Task LoadAsync_Includes_All_Positions_When_DeletionFlagFilterInactive()
    {
        await SeedAsync();

        var filter = new PurchasingDashboardFilter(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 31),
            ExcludeDeletedItems: false);

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Alle vier Positionen: 100 + 200 (Mstae 99) + 300 (Mstae 98) + 400 (Loekz L).
        Assert.Equal(1000m, state.SpendChfSample);
    }

    [Fact]
    public async Task LoadAsync_Converts_ForeignCurrency_Spend_To_Chf_Using_Wkurs()
    {
        // K1: CHF-Beleg bleibt unveraendert, EUR-Beleg wird mit Wkurs nach CHF bewertet.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Waers, Wkurs, LastLoadedAtUtc) VALUES ('C1', '2025-03-01', 'L1', 'CHF', '1', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Waers, Wkurs, LastLoadedAtUtc) VALUES ('C2', '2025-04-01', 'L2', 'EUR', '0.95', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('C1', '10', 'M1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('C2', '10', 'M2', '1', '200', '2026-01-01');");
        // Damit der Cache als gefuellt erkannt wird (EKET muss vorhanden sein).
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('C1', '10', '1', '2025-03-15', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // 100 CHF + 200 EUR * 0.95 = 100 + 190 = 290.
        Assert.Equal(290m, state.SpendChfSample);
    }

    [Fact]
    public async Task LoadAsync_Includes_Future_Schedules_In_OpenValue_Even_When_ToDate_Is_Earlier()
    {
        // K3: Zukuenftiger Zulauf darf nicht am ToDate abgeschnitten werden.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, LastLoadedAtUtc) VALUES ('F1', '2025-06-01', 'L1', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('F1', '10', 'M1', '10', '100', '2026-01-01');");
        // Eindt liegt nach ToDate (2027 vs. 2025) -> Stueckwert 100/10 = 10, offene Menge 10, offener Wert 100.
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('F1', '10', '1', '2027-01-01', '10', '0', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        Assert.Equal(10m, state.OpenQuantitySample);
        Assert.Equal(100m, state.OpenValueSample);
    }

    [Fact]
    public async Task LoadAsync_ContractValue_Counts_Only_Positions_With_Konnr()
    {
        // K4: Kontrakt-Restwert nur fuer Abrufe zu Rahmenkontrakten (EKKO.Konnr gesetzt),
        // nicht mehr eine blosse Kopie des offenen Bestellwerts.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Konnr, LastLoadedAtUtc) VALUES ('K1', '2025-06-01', 'L1', '', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Konnr, LastLoadedAtUtc) VALUES ('K2', '2025-06-01', 'L2', '4600000123', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('K1', '10', 'M1', '5', '500', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('K2', '10', 'M2', '5', '500', '2026-01-01');");
        // Beide Belege haben offene Menge 5 -> Stueckwert 100, offener Wert je 500.
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('K1', '10', '1', '2025-08-01', '5', '0', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('K2', '10', '1', '2025-08-01', '5', '0', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Offener Wert gesamt = 1000, aber nur der Kontrakt-Beleg K2 zaehlt zum Kontrakt-Restwert.
        Assert.Equal(1000m, state.OpenValueSample);
        Assert.Equal(500m, state.ContractValueSample);
    }

    private async Task SeedAsync()
    {
        await ExecuteAsync(
            "INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, LastLoadedAtUtc) VALUES ('E1', '2025-06-01', 'L1', 'Lieferant 1', '2026-01-01');");

        // Aktiv | MARA-MSTAE 99 | MARA-MSTAE 98 | Loekz gesetzt
        await ExecuteAsync(
            "INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Loekz, Mstae, LastLoadedAtUtc) VALUES ('E1', '10', 'M1', '1', '100', '', '', '2026-01-01');");
        await ExecuteAsync(
            "INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Loekz, Mstae, LastLoadedAtUtc) VALUES ('E1', '20', 'M2', '1', '200', '', '99', '2026-01-01');");
        await ExecuteAsync(
            "INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Loekz, Mstae, LastLoadedAtUtc) VALUES ('E1', '30', 'M3', '1', '300', '', '98', '2026-01-01');");
        await ExecuteAsync(
            "INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Loekz, Mstae, LastLoadedAtUtc) VALUES ('E1', '40', 'M4', '1', '400', 'L', '', '2026-01-01');");

        await ExecuteAsync(
            "INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('E1', '10', '1', '2025-06-15', '1', '0', '2026-01-01');");
    }

    private void CreatePurchasingCacheTables()
    {
        ExecuteSync(@"
CREATE TABLE PurchasingEkkoCache (
    Ebeln TEXT NOT NULL PRIMARY KEY,
    Bedat TEXT NULL,
    Aedat TEXT NULL,
    Lifnr TEXT NOT NULL DEFAULT '',
    SupplierName TEXT NOT NULL DEFAULT '',
    Bukrs TEXT NOT NULL DEFAULT '',
    Bsart TEXT NOT NULL DEFAULT '',
    Konnr TEXT NOT NULL DEFAULT '',
    Waers TEXT NOT NULL DEFAULT '',
    Wkurs TEXT NOT NULL DEFAULT '0',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL
);");
        ExecuteSync(@"
CREATE TABLE PurchasingEkpoCache (
    Ebeln TEXT NOT NULL,
    Ebelp TEXT NOT NULL,
    Matnr TEXT NOT NULL DEFAULT '',
    Txz01 TEXT NOT NULL DEFAULT '',
    Matkl TEXT NOT NULL DEFAULT '',
    Menge TEXT NOT NULL DEFAULT '0',
    Meins TEXT NOT NULL DEFAULT '',
    Netwr TEXT NOT NULL DEFAULT '0',
    Loekz TEXT NOT NULL DEFAULT '',
    Mstae TEXT NOT NULL DEFAULT '',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Ebeln, Ebelp)
);");
        ExecuteSync(@"
CREATE TABLE PurchasingEketCache (
    Ebeln TEXT NOT NULL,
    Ebelp TEXT NOT NULL,
    Etenr TEXT NOT NULL,
    Eindt TEXT NULL,
    Menge TEXT NOT NULL DEFAULT '0',
    Wemng TEXT NOT NULL DEFAULT '0',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Ebeln, Ebelp, Etenr)
);");
        ExecuteSync(@"
CREATE TABLE PurchasingSyncState (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Mode TEXT NOT NULL DEFAULT '',
    Status TEXT NOT NULL DEFAULT '',
    StartedAtUtc TEXT NULL,
    CompletedAtUtc TEXT NULL,
    FromDate TEXT NULL,
    ToDate TEXT NULL,
    LastSuccessfulDeltaAtUtc TEXT NULL,
    EkkoRows INTEGER NOT NULL DEFAULT 0,
    EkpoRows INTEGER NOT NULL DEFAULT 0,
    EketRows INTEGER NOT NULL DEFAULT 0,
    Message TEXT NOT NULL DEFAULT ''
);");
    }

    private void ExecuteSync(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
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
