using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>Sichtbarer Zustand des laufenden oder letzten Einkauf-Laufs.</summary>
public sealed record PurchasingRefreshRunInfo(
    bool IsRunning,
    string Mode,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string Message);

public interface IPurchasingRefreshRunner
{
    PurchasingRefreshRunInfo Current { get; }

    /// <summary>
    /// Startet einen Lauf im Hintergrund. Gibt <c>false</c> zurueck, wenn bereits einer laeuft —
    /// zwei gleichzeitige Volllaeufe gegen dasselbe SAP waeren fachlich sinnlos und wuerden sich
    /// beim Schreiben in die Einkaufstabellen in die Quere kommen.
    /// </summary>
    bool TryStart(string mode, DateTime? fromDate = null);
}

/// <summary>
/// Fuehrt Einkauf-Laeufe im Hintergrund aus, unabhaengig von der Oberflaeche.
///
/// VORFALL 2026-08-19 bis 2026-08-24, der zu dieser Klasse fuehrte: Der Full Load wurde direkt
/// im Klick-Handler der Blazor-Seite abgewartet (<c>await ...RunFullLoadAsync()</c>). Ein Lauf
/// dauert rund fuenfzig Minuten. Reisst die SignalR-Verbindung in dieser Zeit ab, oder wechselt
/// der Anwender die Seite, oder startet IIS den Worker neu, dann stirbt der Lauf mitten im
/// Ablauf — und weil der Statuseintrag zu diesem Zeitpunkt auf <c>Running</c> steht, bleibt er
/// dort fuer immer stehen. Produktiv standen so vier Eintraege (Ids 31 bis 34) dauerhaft auf
/// <c>Running</c>, und die Oberflaeche behauptete, es laufe etwas.
///
/// Diese Klasse trennt beides: die Seite loest nur aus und fragt den Zustand ab, der Lauf haengt
/// an der Anwendung. Ein Herunterfahren bricht ihn ueber
/// <see cref="IHostApplicationLifetime.ApplicationStopping"/> geordnet ab.
///
/// AUSSERDEM raeumt <see cref="StartAsync"/> beim Start liegengebliebene <c>Running</c>-Eintraege
/// auf. Das ist zulaessig, weil ein Lauf im Prozess lebt: ueberlebt der Prozess nicht, hat auch
/// kein Lauf ueberlebt. Ein Eintrag, der etwas anderes behauptet, ist eine Falschaussage.
/// </summary>
public sealed class PurchasingRefreshRunner : IPurchasingRefreshRunner, IHostedService
{
    public const string ModeFull = "Full";
    public const string ModeDelta = "Delta";

    /// <summary>Statuswert fuer einen Lauf, der einen Neustart nicht ueberlebt hat.</summary>
    public const string StatusAborted = "Abgebrochen";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<PurchasingRefreshRunner> _logger;

    private readonly object _gate = new();
    private bool _running;
    private string _mode = string.Empty;
    private DateTime? _startedAtUtc;
    private DateTime? _finishedAtUtc;
    private string _message = string.Empty;

    public PurchasingRefreshRunner(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        ILogger<PurchasingRefreshRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _lifetime = lifetime;
        _logger = logger;
    }

    public PurchasingRefreshRunInfo Current
    {
        get
        {
            lock (_gate)
                return new PurchasingRefreshRunInfo(_running, _mode, _startedAtUtc, _finishedAtUtc, _message);
        }
    }

    public bool TryStart(string mode, DateTime? fromDate = null)
    {
        var normalized = string.Equals(mode, ModeDelta, StringComparison.OrdinalIgnoreCase)
            ? ModeDelta
            : ModeFull;

        lock (_gate)
        {
            if (_running)
                return false;

            _running = true;
            _mode = normalized;
            _startedAtUtc = DateTime.UtcNow;
            _finishedAtUtc = null;
            _message = string.Empty;
        }

        // Absichtlich ohne await: der Aufrufer ist ein Klick und soll sofort zurueckkommen.
        _ = Task.Run(() => RunAsync(normalized, fromDate));
        return true;
    }

    private async Task RunAsync(string mode, DateTime? fromDate)
    {
        var message = string.Empty;

        try
        {
            // Eigener Scope: IPurchasingDataRefreshService ist Scoped und darf nicht aus dem
            // Singleton-Root aufgeloest werden.
            using var scope = _scopeFactory.CreateScope();
            var refresh = scope.ServiceProvider.GetRequiredService<IPurchasingDataRefreshService>();
            var token = _lifetime.ApplicationStopping;

            var status = mode == ModeDelta
                ? await refresh.RunDeltaAsync(fromDate, token)
                : await refresh.RunFullLoadAsync(fromDate, token);

            message = status.Message;
            _logger.LogInformation("Einkauf-Lauf {Mode} beendet: {Message}", mode, status.Message);
        }
        catch (Exception ex)
        {
            // RunFullLoadAsync/RunDeltaAsync fangen ihre Fehler selbst und schreiben sie in den
            // Status. Landet hier trotzdem etwas, ist es ein Fehler ausserhalb davon und darf
            // den Prozess nicht mitnehmen — dieser Task hat keinen Aufrufer, der ihn abwartet.
            message = $"{ex.GetType().Name}: {ex.Message}";
            _logger.LogError(ex, "Einkauf-Lauf {Mode} ist unerwartet abgebrochen", mode);
        }
        finally
        {
            lock (_gate)
            {
                _running = false;
                _finishedAtUtc = DateTime.UtcNow;
                _message = message;
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            await using var command = conn.CreateCommand();
            command.CommandText = @"
UPDATE PurchasingSyncState
SET Status = $status,
    CompletedAtUtc = $now,
    Message = Message || $suffix
WHERE Status = 'Running';";
            command.Parameters.AddWithValue("$status", StatusAborted);
            command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$suffix",
                " | Beim Start der Anwendung als abgebrochen erkannt: ein Lauf lebt im Prozess und " +
                "kann einen Neustart nicht ueberleben.");

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected > 0)
            {
                _logger.LogWarning(
                    "{Count} liegengebliebene Einkauf-Laeufe auf '{Status}' gesetzt.", affected, StatusAborted);
            }
        }
        catch (Exception ex)
        {
            // Ein Fehler beim Aufraeumen darf den Start der Anwendung nicht verhindern.
            _logger.LogError(ex, "Liegengebliebene Einkauf-Laeufe konnten nicht aufgeraeumt werden");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
