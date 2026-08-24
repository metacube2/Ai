using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Sichert die zwei Zusagen des Runners ab, die aus dem Vorfall vom 2026-08-19 bis 2026-08-24
/// entstanden sind: ein Lauf haengt nicht mehr am Blazor-Circuit, und liegengebliebene
/// <c>Running</c>-Eintraege werden beim Start als abgebrochen erkannt statt weiter "Laeuft" zu
/// behaupten.
/// </summary>
public class PurchasingRefreshRunnerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;

    public PurchasingRefreshRunnerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var command = _connection.CreateCommand())
        {
            command.CommandText = DatabaseSchemaSql.GetPurchasingSyncStateCreateSql();
            command.ExecuteNonQuery();
        }

        _dbFactory = new TestDbContextFactory(options);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task TryStart_Weist_Einen_Zweiten_Lauf_Ab_Solange_Der_Erste_Laeuft()
    {
        var refresh = new BlockingRefreshService();
        var runner = CreateRunner(refresh);

        Assert.True(runner.TryStart(PurchasingRefreshRunner.ModeFull));
        Assert.True(await refresh.WaitUntilStartedAsync());

        // Zwei gleichzeitige Volllaeufe gegen dasselbe SAP waeren fachlich sinnlos und wuerden
        // sich beim Schreiben in die Einkaufstabellen in die Quere kommen.
        Assert.False(runner.TryStart(PurchasingRefreshRunner.ModeFull));
        Assert.False(runner.TryStart(PurchasingRefreshRunner.ModeDelta));
        Assert.True(runner.Current.IsRunning);
        Assert.Equal(PurchasingRefreshRunner.ModeFull, runner.Current.Mode);

        refresh.Release();
        Assert.True(await WaitUntilIdleAsync(runner));

        Assert.False(runner.Current.IsRunning);
        Assert.NotNull(runner.Current.FinishedAtUtc);
        // Nach dem Ende ist wieder ein Lauf moeglich.
        Assert.True(runner.TryStart(PurchasingRefreshRunner.ModeDelta));
    }

    [Fact]
    public async Task TryStart_Kehrt_Sofort_Zurueck_Und_Wartet_Den_Lauf_Nicht_Ab()
    {
        // Genau das war der Fehler: der Klick-Handler hat einen rund fuenfzig Minuten langen
        // Lauf abgewartet und ist mit der Verbindung gestorben.
        var refresh = new BlockingRefreshService();
        var runner = CreateRunner(refresh);

        var started = runner.TryStart(PurchasingRefreshRunner.ModeFull);

        Assert.True(started);
        Assert.False(refresh.Completed);
        refresh.Release();
        Assert.True(await WaitUntilIdleAsync(runner));
    }

    [Fact]
    public async Task TryStart_Uebergibt_Den_Modus_Und_Das_Startdatum()
    {
        var refresh = new BlockingRefreshService();
        var runner = CreateRunner(refresh);
        var from = new DateTime(2026, 1, 1);

        runner.TryStart(PurchasingRefreshRunner.ModeDelta, from);
        Assert.True(await refresh.WaitUntilStartedAsync());
        refresh.Release();
        Assert.True(await WaitUntilIdleAsync(runner));

        Assert.Equal(PurchasingRefreshRunner.ModeDelta, refresh.LastMode);
        Assert.Equal(from, refresh.LastFromDate);
    }

    [Fact]
    public async Task TryStart_Bleibt_Nicht_Auf_Laeuft_Stehen_Wenn_Der_Lauf_Wirft()
    {
        var refresh = new ThrowingRefreshService();
        var runner = CreateRunner(refresh);

        Assert.True(runner.TryStart(PurchasingRefreshRunner.ModeFull));
        Assert.True(await WaitUntilIdleAsync(runner));

        Assert.False(runner.Current.IsRunning);
        Assert.Contains("Absicht", runner.Current.Message);
    }

    [Fact]
    public async Task StartAsync_Setzt_Liegengebliebene_Laeufe_Auf_Abgebrochen()
    {
        await ExecuteAsync(
            "INSERT INTO PurchasingSyncState (Mode, Status, StartedAtUtc, Message) " +
            "VALUES ('Full', 'Running', '2026-08-21T08:32:50Z', 'Full Load gestartet.');");
        await ExecuteAsync(
            "INSERT INTO PurchasingSyncState (Mode, Status, StartedAtUtc, CompletedAtUtc, Message) " +
            "VALUES ('Delta', 'Success', '2026-08-18T10:25:41Z', '2026-08-18T11:15:31Z', 'Delta abgeschlossen.');");

        var runner = CreateRunner(new BlockingRefreshService());
        await runner.StartAsync(CancellationToken.None);

        var rows = await ReadStatusAsync();

        Assert.Equal(PurchasingRefreshRunner.StatusAborted, rows[0].Status);
        Assert.NotEqual(string.Empty, rows[0].CompletedAtUtc);
        Assert.Contains("Full Load gestartet.", rows[0].Message);
        Assert.Contains("abgebrochen erkannt", rows[0].Message);

        // Ein abgeschlossener Lauf wird nicht angetastet.
        Assert.Equal("Success", rows[1].Status);
        Assert.Equal("Delta abgeschlossen.", rows[1].Message);
    }

    [Fact]
    public async Task StartAsync_Ohne_Liegengebliebene_Laeufe_Aendert_Nichts()
    {
        await ExecuteAsync(
            "INSERT INTO PurchasingSyncState (Mode, Status, Message) VALUES ('Full', 'Success', 'ok');");

        var runner = CreateRunner(new BlockingRefreshService());
        await runner.StartAsync(CancellationToken.None);

        var rows = await ReadStatusAsync();
        Assert.Single(rows);
        Assert.Equal("Success", rows[0].Status);
        Assert.Equal("ok", rows[0].Message);
    }

    [Fact]
    public async Task StartAsync_Verhindert_Den_Start_Nicht_Wenn_Die_Tabelle_Fehlt()
    {
        // Ein Fehler beim Aufraeumen darf die Anwendung nicht am Starten hindern.
        await ExecuteAsync("DROP TABLE PurchasingSyncState;");

        var runner = CreateRunner(new BlockingRefreshService());

        await runner.StartAsync(CancellationToken.None);
    }

    private PurchasingRefreshRunner CreateRunner(IPurchasingDataRefreshService refresh)
        => new(
            new StubScopeFactory(new Dictionary<Type, object>
            {
                [typeof(IPurchasingDataRefreshService)] = refresh,
                [typeof(IDbContextFactory<AppDbContext>)] = _dbFactory
            }),
            new StubLifetime(),
            NullLogger<PurchasingRefreshRunner>.Instance);

    private static async Task<bool> WaitUntilIdleAsync(PurchasingRefreshRunner runner)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (!runner.Current.IsRunning && runner.Current.FinishedAtUtc.HasValue)
                return true;

            await Task.Delay(25);
        }

        return false;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<(string Status, string CompletedAtUtc, string Message)>> ReadStatusAsync()
    {
        var rows = new List<(string, string, string)>();
        await using var command = _connection.CreateCommand();
        command.CommandText =
            "SELECT Status, COALESCE(CompletedAtUtc, ''), Message FROM PurchasingSyncState ORDER BY Id;";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));

        return rows;
    }

    private sealed class BlockingRefreshService : IPurchasingDataRefreshService
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string LastMode { get; private set; } = string.Empty;
        public DateTime? LastFromDate { get; private set; }
        public bool Completed { get; private set; }

        public void Release() => _release.TrySetResult();

        public async Task<bool> WaitUntilStartedAsync()
        {
            var finished = await Task.WhenAny(_started.Task, Task.Delay(5000));
            return finished == _started.Task;
        }

        public Task<PurchasingDataRefreshStatus> GetStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PurchasingDataRefreshStatus());

        public Task<PurchasingDataRefreshStatus> RunFullLoadAsync(
            DateTime? fromDate = null, CancellationToken cancellationToken = default)
            => RunAsync(PurchasingRefreshRunner.ModeFull, fromDate);

        public Task<PurchasingDataRefreshStatus> RunDeltaAsync(
            DateTime? fromDate = null, CancellationToken cancellationToken = default)
            => RunAsync(PurchasingRefreshRunner.ModeDelta, fromDate);

        private async Task<PurchasingDataRefreshStatus> RunAsync(string mode, DateTime? fromDate)
        {
            LastMode = mode;
            LastFromDate = fromDate;
            _started.TrySetResult();
            await _release.Task;
            Completed = true;
            return new PurchasingDataRefreshStatus { Status = "Success", Message = "fertig" };
        }
    }

    private sealed class ThrowingRefreshService : IPurchasingDataRefreshService
    {
        public Task<PurchasingDataRefreshStatus> GetStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PurchasingDataRefreshStatus());

        public Task<PurchasingDataRefreshStatus> RunFullLoadAsync(
            DateTime? fromDate = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Absicht");

        public Task<PurchasingDataRefreshStatus> RunDeltaAsync(
            DateTime? fromDate = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Absicht");
    }

    private sealed class StubScopeFactory : IServiceScopeFactory, IServiceScope, IServiceProvider
    {
        private readonly Dictionary<Type, object> _services;

        public StubScopeFactory(Dictionary<Type, object> services)
        {
            _services = services;
        }

        public IServiceScope CreateScope() => this;

        public IServiceProvider ServiceProvider => this;

        public object? GetService(Type serviceType)
            => _services.TryGetValue(serviceType, out var service) ? service : null;

        public void Dispose()
        {
        }
    }

    private sealed class StubLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
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
