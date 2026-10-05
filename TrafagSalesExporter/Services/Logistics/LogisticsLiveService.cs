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
    /// <summary>Am Tag selbst quittiert; frueher quittierte Positionen (offene TA von Vortagen) zaehlen nicht als heutige Leistung.</summary>
    public int TransferItemsConfirmed => Transfers.Count(x => x.Confirmed && (Day == default || !x.ConfirmedAt.HasValue || DateOnly.FromDateTime(x.ConfirmedAt.Value) == Day));
    public int TransferItemsTotal => TransferItemsOpen + TransferItemsConfirmed;
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

    private readonly SemaphoreSlim _capacityGate = new(1, 1);
    private (DateTime At, IReadOnlyList<LiveCapacityDay> Days, string? Error)? _capacity;

    /// <summary>Werk der Produktionskapazitaet (Teil C), wie die Rueckmeldungen.</summary>
    public const string CapacityPlant = "1100";

    /// <summary>
    /// Kapazitaet je Kapazitaet und Tag, naechste 14 Tage (LogKapSet, Teil C 2026-10-05). Ein Aufruf, hoechstens alle
    /// 15 Minuten, gemeinsam fuer alle. Fehlt das Set in P76 noch, kommt der Fehlertext statt einer Ausnahme.
    /// </summary>
    public async Task<(IReadOnlyList<LiveCapacityDay> Days, string? Error, DateTime? At)> GetCapacityAsync(CancellationToken ct = default)
    {
        await _capacityGate.WaitAsync(ct);
        try
        {
            if (_capacity is { } c && DateTime.Now - c.At < CacheTtl(c.Error))
                return (c.Days, c.Error, c.At);
            IReadOnlyList<LiveCapacityDay> days = [];
            string? error = null;
            if (!_options.CurrentValue.Enabled)
                return (days, null, null);
            try
            {
                var (baseUrl, user, password) = await ResolveConnectionAsync(ct);
                using var client = CreateClient(user, password);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                days = await _reader.ReadCapacityAsync(client, baseUrl, CapacityPlant, DateOnly.FromDateTime(DateTime.Today), 14, timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // HttpClient-Timeout oder eigene Frist: kein Abbruch durch den Betrachter, sondern ein Fehler.
                error = TimeoutText;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                error = ex.GetBaseException().Message;
            }
            _capacity = (DateTime.Now, days, error);
            return (days, error, DateTime.Now);
        }
        finally
        {
            _capacityGate.Release();
        }
    }

    /// <summary>Lagernummer der Kommissionierung fuer die Vorschau (LIKP-LGNUM), sonst kaeme jedes Lager.</summary>
    public const string OutlookWarehouse = "110";

    internal const string TimeoutText = "SAP hat nicht rechtzeitig geantwortet (Timeout).";
    private bool _overdueUnsupported;

    /// <summary>Erfolg 15 Minuten merken, einen Fehler nur 1 Minute: sonst zeigt die Seite nach einem Import noch lange "fehlt".</summary>
    internal static TimeSpan CacheTtl(string? error) => error is null ? TimeSpan.FromMinutes(15) : TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _outlookGate = new(1, 1);
    private (DateTime At, IReadOnlyList<LiveDelivery> Deliveries, string? Error)? _outlook;

    /// <summary>
    /// Warenausgang nach Termin (2026-10-05): Lieferungen der naechsten 14 Tage, ein Tag je Abfrage, eigener Takt
    /// (hoechstens alle 15 Minuten, gemeinsam fuer alle Betrachter), nicht im 30-Sekunden-Abruf. Liefert P76 die Zukunft
    /// noch nicht (Transport T76K912658 fehlt), bleibt die Liste leer statt eines Fehlers.
    /// </summary>
    public async Task<(IReadOnlyList<LiveDelivery> Deliveries, string? Error, DateTime? At)> GetOutlookAsync(CancellationToken ct = default)
    {
        await _outlookGate.WaitAsync(ct);
        try
        {
            if (_outlook is { } c && DateTime.Now - c.At < CacheTtl(c.Error))
                return (c.Deliveries, c.Error, c.At);
            var list = new List<LiveDelivery>();
            string? error = null;
            if (!_options.CurrentValue.Enabled)
                return (list, null, null);
            try
            {
                var (baseUrl, user, password) = await ResolveConnectionAsync(ct);
                using var client = CreateClient(user, password);
                // 15 Abfragen nacheinander: eine gemeinsame Frist, damit ein haengendes SAP nicht 15 x 20 s blockiert.
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(60));
                var today = DateOnly.FromDateTime(DateTime.Today);
                // Heute (i = 0) und die naechsten 14 Tage; nur Lagernummer 110 (Kommissionierung), sonst kommen alle Lager.
                for (var i = 0; i <= 14; i++)
                    list.AddRange((await _reader.ReadDeliveriesAsync(client, baseUrl, today.AddDays(i), timeout.Token, OutlookWarehouse))
                        .Select(d => d with { PlannedGoodsIssue = d.PlannedGoodsIssue ?? today.AddDays(i).ToDateTime(TimeOnly.MinValue) }));
                // Eine Lieferung kann an zwei Tagen vorkommen (WADAT und WADAT_IST): je Lieferung nur die erste behalten.
                var unique = list.GroupBy(d => d.Delivery).Select(g => g.First()).ToList();
                list.Clear();
                list.AddRange(unique);
                // Ueberfaellige (Termin vor heute, Warenausgang nicht gebucht): braucht das neue Feld Ueberf in P76.
                // Kennt P76 es noch nicht (HTTP 400), bleibt der Eimer leer und wir fragen bis zum Neustart nicht mehr.
                if (!_overdueUnsupported)
                {
                    try
                    {
                        var overdue = await _reader.ReadDeliveriesAsync(client, baseUrl, today, timeout.Token, OutlookWarehouse, overdue: true);
                        var known = list.Select(d => d.Delivery).ToHashSet();
                        list.AddRange(overdue.Where(d => d.PlannedGoodsIssue.HasValue && d.PlannedGoodsIssue.Value.Date < today.ToDateTime(TimeOnly.MinValue)
                                                          && d.GoodsIssueStatus != "C" && known.Add(d.Delivery)));
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        _overdueUnsupported = true;
                    }
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                error = TimeoutText;
                await _log.WriteAsync("Logistik", "Logistik live: Vorschau Warenausgang fehlgeschlagen", "Warning", details: error);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                error = ex.GetBaseException().Message;
                await _log.WriteAsync("Logistik", "Logistik live: Vorschau Warenausgang fehlgeschlagen", "Warning", details: error);
            }
            _outlook = (DateTime.Now, list, error);
            return (list, error, DateTime.Now);
        }
        finally
        {
            _outlookGate.Release();
        }
    }

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
            MergeConfirmations(_confirmations, confirmations);
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

    /// <summary>
    /// Fuehrt Rueckmeldungen zusammen und macht Stornos wirksam: Der Stornosatz traegt den Zaehler des Originals (STZHL).
    /// Das Original selbst ist beim inkrementellen Abruf nicht mehr dabei (sein Zeitstempel ist alt), bliebe also als
    /// gueltig stehen. Deshalb wird es ueber den Schluessel (Rueck, Rmzhl) als storniert markiert.
    /// </summary>
    internal static void MergeConfirmations(Dictionary<(string, string), LiveConfirmation> target, IEnumerable<LiveConfirmation> items)
    {
        foreach (var item in items)
        {
            target[(item.Rueck, item.Rmzhl)] = item;
            if (!item.HasStornoCounter)
                continue;
            var stzhl = item.Stzhl.Trim().TrimStart('0');
            var original = target.Keys.FirstOrDefault(k => k.Item1 == item.Rueck && k.Item2.TrimStart('0') == stzhl && k.Item2 != item.Rmzhl);
            if (original != default && !target[original].Cancelled)
                target[original] = target[original] with { Cancelled = true };
        }
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
            // Gutmenge nur in der haeufigsten Einheit des Arbeitsplatzes summieren (Stueck und Stunden nicht mischen).
            .Select(g => new LiveWorkCenter(g.Key.WorkCenter, g.Key.Plant, g.Max(x => x.At!.Value), g.Count(),
                g.GroupBy(x => x.Unit).OrderByDescending(u => u.Count()).First().Sum(x => x.Yield)))
            .OrderBy(x => x.Plant).ThenBy(x => x.WorkCenter)
            .ToList();

    public static IReadOnlyList<LiveOrderProgress> Orders(LogisticsLiveSnapshot snapshot, int top = 15)
        => snapshot.ValidConfirmations
            .Where(x => x.At.HasValue && x.Order.Length > 0)
            .GroupBy(x => x.Order)
            .Select(g =>
            {
                var latest = g.MaxBy(x => x.At)!;
                // Gutmenge je Auftrag = Menge des zuletzt zurueckgemeldeten Vorgangs (Teilrueckmeldungen dieses Vorgangs
                // und dieser Einheit summiert); Vorgaenge nacheinander zu addieren zaehlte jedes Stueck mehrfach.
                var lastOperation = g.Where(x => x.Operation == latest.Operation && x.Unit == latest.Unit).Sum(x => x.Yield);
                return new LiveOrderProgress(g.Key, latest.Material, latest.OrderTarget, latest.OrderConfirmed,
                    lastOperation, latest.At!.Value);
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
