using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>Notschalter: <c>LogisticsLive:Enabled</c> in appsettings.json; false stoppt jeden SAP-Abruf.</summary>
public sealed class LogisticsLiveOptions
{
    public const string SectionName = "LogisticsLive";
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
}

/// <summary>Fertig gerechneter Stand fuer die Seite; unveraenderlich, wird je Abruf ersetzt.</summary>
public sealed class LogisticsLiveSnapshot
{
    public DateOnly Day { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? Error { get; init; }
    public bool Paused { get; init; }
    public IReadOnlyList<LiveTransferItem> Transfers { get; init; } = [];
    public IReadOnlyList<LiveDelivery> Deliveries { get; init; } = [];
    public IReadOnlyList<LiveConfirmation> Confirmations { get; init; } = [];

    public int TransferOrdersCreated => Transfers.Select(x => (x.Lgnum, x.Tanum)).Distinct().Count();
    public int TransferItemsOpen => Transfers.Count(x => !x.Confirmed);
    public int TransferItemsConfirmed => Transfers.Count(x => x.Confirmed);
    public int DeliveriesPicked => Deliveries.Count(x => x.Positions > 0 && x.PositionsPicked == x.Positions);
    public int DeliveriesWithPicking => Deliveries.Count(x => x.Positions > 0);
    public IEnumerable<LiveConfirmation> ValidConfirmations => Confirmations.Where(x => !x.Cancelled);
}

/// <summary>Arbeitsplatz mit letzter Rueckmeldung, fuer die farbige Kachel.</summary>
public sealed record LiveWorkCenter(string WorkCenter, string Plant, DateTime LastAt, int ConfirmationsToday, decimal YieldToday);

/// <summary>Auftrag mit Fortschritt Gutmenge gegen Soll.</summary>
public sealed record LiveOrderProgress(string Order, string Material, decimal Target, decimal Confirmed, decimal YieldToday, DateTime LastAt);

/// <summary>
/// Haelt den Live-Stand von Kommissionierung und Produktion fuer ALLE Betrachter gemeinsam und
/// fragt SAP nur ab, solange mindestens eine Seite offen ist (Wunsch Ingo 2026-10-01: "Abfrage nur,
/// wenn jemand schaut"). Ein Abruf zur Zeit, Timeout 20 s; nach einem Fehler oder einer Antwort
/// ueber 10 s fuenf Minuten Pause, damit P76 nie unter Last geraet ("wenn SAP steht, steht die
/// Produktion"). Nach dem ersten Abruf des Tages nur noch, was seit dem letzten Abruf neu ist
/// (zwei Minuten Ueberlappung, Duplikate werden ueber den Schluessel zusammengefuehrt).
/// </summary>
public sealed class LogisticsLiveService : IDisposable
{
    internal static readonly TimeSpan Overlap = TimeSpan.FromMinutes(2);
    internal static readonly TimeSpan SlowAnswer = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan PauseAfterProblem = TimeSpan.FromMinutes(5);

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IOptionsMonitor<LogisticsLiveOptions> _options;
    private readonly IAppEventLogService _log;
    private readonly SapGatewayLogisticsLiveReader _reader = new();
    private readonly object _gate = new();
    private int _viewers;
    private CancellationTokenSource? _loop;
    private DateTime _pausedUntil = DateTime.MinValue;
    private DateTime? _lastPollStart;

    private readonly Dictionary<(string, string, string), LiveTransferItem> _transfers = new();
    private readonly Dictionary<(string, string), LiveConfirmation> _confirmations = new();
    private IReadOnlyList<LiveDelivery> _deliveries = [];
    private DateOnly _day;

    public LogisticsLiveService(
        IDbContextFactory<AppDbContext> dbFactory,
        IOptionsMonitor<LogisticsLiveOptions> options,
        IAppEventLogService log)
    {
        _dbFactory = dbFactory;
        _options = options;
        _log = log;
    }

    public LogisticsLiveSnapshot Snapshot { get; private set; } = new();

    public event Action? Changed;

    /// <summary>Seite geoeffnet: startet den Abruf, falls er nicht schon laeuft.</summary>
    public void Subscribe()
    {
        lock (_gate)
        {
            _viewers++;
            if (_loop is not null)
                return;
            _loop = new CancellationTokenSource();
            _ = RunAsync(_loop.Token);
        }
    }

    /// <summary>Seite geschlossen: mit dem letzten Betrachter endet der Abruf.</summary>
    public void Unsubscribe()
    {
        lock (_gate)
        {
            _viewers = Math.Max(0, _viewers - 1);
            if (_viewers > 0 || _loop is null)
                return;
            _loop.Cancel();
            _loop.Dispose();
            _loop = null;
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var options = _options.CurrentValue;
                if (!options.Enabled)
                    Publish(error: "Der Live-Abruf ist abgeschaltet (LogisticsLive:Enabled = false).", paused: true);
                else if (DateTime.Now < _pausedUntil)
                    Publish(error: Snapshot.Error, paused: true);
                else
                    await PollOnceAsync(ct);

                await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(options.IntervalSeconds, 15, 300)), ct);
            }
        }
        catch (OperationCanceledException)
        {
            // letzter Betrachter weg
        }
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        var started = DateTime.Now;
        var day = DateOnly.FromDateTime(started);
        if (day != _day)
        {
            // Tageswechsel: alles neu, erster Abruf liest den ganzen Tag.
            _day = day;
            _transfers.Clear();
            _confirmations.Clear();
            _deliveries = [];
            _lastPollStart = null;
        }

        var fromTime = FromTimeFor(_lastPollStart, started);
        try
        {
            var (baseUrl, user, password) = await ResolveConnectionAsync(ct);
            using var client = CreateClient(user, password);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));

            var transfers = await _reader.ReadTransfersAsync(client, baseUrl, day, fromTime, timeout.Token);
            var confirmations = await _reader.ReadConfirmationsAsync(client, baseUrl, day, fromTime, timeout.Token);
            // Lieferungen sind wenige je Tag; der Kommissionierstand aendert sich, deshalb immer ganz.
            var deliveries = await _reader.ReadDeliveriesAsync(client, baseUrl, day, timeout.Token);

            Merge(_transfers, transfers, x => (x.Lgnum, x.Tanum, x.Tapos));
            Merge(_confirmations, confirmations, x => (x.Rueck, x.Rmzhl));
            _deliveries = deliveries;
            _lastPollStart = started;

            var duration = DateTime.Now - started;
            if (duration > SlowAnswer)
            {
                _pausedUntil = DateTime.Now + PauseAfterProblem;
                await _log.WriteAsync("Logistik", "Logistik live: SAP langsam, Pause", "Warning",
                    details: $"Abruf dauerte {duration.TotalSeconds:N1} s; naechster Versuch in {PauseAfterProblem.TotalMinutes:N0} Minuten.");
            }

            Publish(error: null, paused: false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _pausedUntil = DateTime.Now + PauseAfterProblem;
            await _log.WriteAsync("Logistik", "Logistik live: Abruf fehlgeschlagen", "Warning",
                details: $"Pause {PauseAfterProblem.TotalMinutes:N0} Minuten. {ex.Message}");
            Publish(error: $"SAP-Abruf fehlgeschlagen, neuer Versuch in {PauseAfterProblem.TotalMinutes:N0} Minuten: {ex.Message}", paused: true);
        }
    }

    /// <summary>Erster Abruf des Tages ab 00:00, danach ab dem letzten Abruf minus Ueberlappung.</summary>
    internal static TimeOnly FromTimeFor(DateTime? lastPollStart, DateTime now)
    {
        if (lastPollStart is null || lastPollStart.Value.Date != now.Date)
            return TimeOnly.MinValue;
        var from = lastPollStart.Value - Overlap;
        return from.Date != now.Date ? TimeOnly.MinValue : TimeOnly.FromDateTime(from);
    }

    internal static void Merge<TKey, TItem>(Dictionary<TKey, TItem> target, IEnumerable<TItem> items, Func<TItem, TKey> key)
        where TKey : notnull
    {
        foreach (var item in items)
            target[key(item)] = item;
    }

    private void Publish(string? error, bool paused)
    {
        Snapshot = new LogisticsLiveSnapshot
        {
            Day = _day == default ? DateOnly.FromDateTime(DateTime.Now) : _day,
            UpdatedAt = error is null && !paused ? DateTime.Now : Snapshot.UpdatedAt,
            Error = error,
            Paused = paused,
            Transfers = _transfers.Values.ToList(),
            Deliveries = _deliveries,
            Confirmations = _confirmations.Values.ToList()
        };
        Changed?.Invoke();
    }

    /// <summary>Durchsatz je Stunde: angelegte und quittierte TA-Positionen.</summary>
    public static IReadOnlyList<(int Hour, int Created, int Confirmed)> TransferThroughputByHour(LogisticsLiveSnapshot snapshot)
        => Enumerable.Range(0, 24)
            .Select(h => (h,
                snapshot.Transfers.Count(x => x.CreatedAt?.Date == snapshot.Day.ToDateTime(TimeOnly.MinValue) && x.CreatedAt?.Hour == h),
                snapshot.Transfers.Count(x => x.Confirmed && x.ConfirmedAt?.Date == snapshot.Day.ToDateTime(TimeOnly.MinValue) && x.ConfirmedAt?.Hour == h)))
            .ToList();

    /// <summary>Gutmenge und Anzahl Rueckmeldungen je Stunde.</summary>
    public static IReadOnlyList<(int Hour, int Count)> ConfirmationsByHour(LogisticsLiveSnapshot snapshot)
        => Enumerable.Range(0, 24)
            .Select(h => (h, snapshot.ValidConfirmations.Count(x => x.At?.Hour == h)))
            .ToList();

    public static IReadOnlyList<LiveWorkCenter> WorkCenters(LogisticsLiveSnapshot snapshot)
        => snapshot.ValidConfirmations
            .Where(x => x.At.HasValue && x.WorkCenter.Length > 0)
            .GroupBy(x => (x.WorkCenter, x.Plant))
            .Select(g => new LiveWorkCenter(g.Key.WorkCenter, g.Key.Plant, g.Max(x => x.At!.Value), g.Count(), g.Sum(x => x.Yield)))
            .OrderBy(x => x.Plant).ThenBy(x => x.WorkCenter)
            .ToList();

    public static IReadOnlyList<LiveOrderProgress> Orders(LogisticsLiveSnapshot snapshot, int top = 15)
        => snapshot.ValidConfirmations
            .Where(x => x.At.HasValue && x.Order.Length > 0)
            .GroupBy(x => x.Order)
            .Select(g =>
            {
                var latest = g.MaxBy(x => x.At)!;
                return new LiveOrderProgress(g.Key, latest.Material, latest.OrderTarget, latest.OrderConfirmed,
                    g.Sum(x => x.Yield), latest.At!.Value);
            })
            .OrderByDescending(x => x.LastAt)
            .Take(top)
            .ToList();

    /// <summary>Gruen bis 15 Minuten seit der letzten Rueckmeldung, gelb bis 60, sonst ruhig.</summary>
    public static string ActivityClass(DateTime lastAt, DateTime now)
    {
        var minutes = (now - lastAt).TotalMinutes;
        return minutes <= 15 ? "aktiv" : minutes <= 60 ? "eben" : "ruhig";
    }

    /// <summary>Gleiche Verbindung wie Einkauf und HR, weil die Sets im selben Service liegen.</summary>
    private async Task<(string Url, string User, string Password)> ResolveConnectionAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var sap = await db.SourceSystemDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Code == "SAP", ct)
            ?? throw new InvalidOperationException("SAP Quelle fehlt.");
        var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(x => x.TSC == PurchasingDataSourcePageService.PurchasingTsc, ct)
            ?? throw new InvalidOperationException("Einkauf SAP Site fehlt.");
        var url = string.IsNullOrWhiteSpace(site.SapServiceUrl) ? sap.CentralServiceUrl : site.SapServiceUrl;
        var user = string.IsNullOrWhiteSpace(site.UsernameOverride) ? sap.CentralUsername : site.UsernameOverride;
        var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? sap.CentralPassword : site.PasswordOverride;
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SAP URL oder Zugangsdaten fehlen.");
        return (url, user, password);
    }

    private static HttpClient CreateClient(string user, string password)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _loop?.Cancel();
            _loop?.Dispose();
            _loop = null;
        }
    }
}
