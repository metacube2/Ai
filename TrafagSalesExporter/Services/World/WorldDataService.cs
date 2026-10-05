using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Holt die Weltlage-Daten (2026-10-05) aus frei abrufbaren Quellen ohne Anmeldung, nur lesend, feste Adressen:
/// GDELT 2.0 Ereignisdateien (alle 15 Min. eine Datei, hier alle 30 Min. nachgeholt, 24 h zurueck), FRED (Rohstoffe,
/// Brent, Finanzstress), Eurostat (Industrieproduktion je EU-Land), IMF DataMapper (BIP-Wachstum). Nur in Produktion.
/// Jede Quelle meldet ihren Zustand; ist sie nicht erreichbar, heisst das „vermutlich Firewall“ und die Seite zeigt es.
/// Tageswerte der Ereignisse in <c>WorldEventDaily</c> (60 Tage), uebrige Quellen als JSON in <c>WorldSourceCache</c>.
/// Doku docs/WELTLAGE_2026-10-05.md.
/// </summary>
public sealed class WorldDataService : BackgroundService
{
    public static readonly (string Key, string Name, string Unit)[] FredSeries =
    [
        ("PCOPPUSDM", "Kupfer", "USD/t"),
        ("PNICKUSDM", "Nickel", "USD/t"),
        ("PALUMUSDM", "Aluminium", "USD/t"),
        ("PIORECRUSDM", "Eisenerz", "USD/t"),
        ("PNGASEUUSDM", "Erdgas Europa", "USD/MMBtu"),
        ("DCOILBRENTEU", "Erdöl Brent", "USD/bbl"),
        ("STLFSI4", "Finanzstress-Index", "Index")
    ];

    internal const string EventTableSql = @"
CREATE TABLE IF NOT EXISTS WorldEventDaily (
    Day TEXT NOT NULL, Country TEXT NOT NULL, Events INTEGER NOT NULL, Conflict INTEGER NOT NULL, Protest INTEGER NOT NULL,
    Mentions INTEGER NOT NULL, ToneSum REAL NOT NULL, GoldsteinSum REAL NOT NULL, PRIMARY KEY (Day, Country));";
    internal const string FileTableSql = "CREATE TABLE IF NOT EXISTS WorldGdeltFiles (Stamp TEXT NOT NULL PRIMARY KEY, Rows INTEGER NOT NULL);";
    internal const string CacheTableSql = @"
CREATE TABLE IF NOT EXISTS WorldSourceCache (
    Key TEXT NOT NULL PRIMARY KEY, FetchedAtUtc TEXT NOT NULL, State TEXT NOT NULL, Message TEXT NOT NULL DEFAULT '', Payload TEXT NOT NULL DEFAULT '');";

    private readonly IHttpClientFactory _http;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<WorldDataService> _logger;
    private readonly SalesDataService _sales;
    private readonly object _lock = new();
    private List<WorldEvent> _topEvents = [];
    private bool _gdeltFilled;
    private DateTime _lastSlowRefresh = DateTime.MinValue;

    public WorldDataService(IHttpClientFactory http, IDbContextFactory<AppDbContext> dbFactory, IHostEnvironment environment,
        ILogger<WorldDataService> logger, SalesDataService sales)
    {
        _http = http;
        _dbFactory = dbFactory;
        _environment = environment;
        _logger = logger;
        _sales = sales;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_environment.IsProduction())
            return;
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshGdeltAsync(stoppingToken);
                if (DateTime.UtcNow - _lastSlowRefresh > TimeSpan.FromHours(6))
                {
                    await RefreshSlowSourcesAsync(stoppingToken);
                    _lastSlowRefresh = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Weltlage: Abruf fehlgeschlagen");
            }
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private HttpClient Client()
    {
        var c = _http.CreateClient("Weltlage");
        c.Timeout = TimeSpan.FromSeconds(30);
        c.DefaultRequestHeaders.UserAgent.ParseAdd("TrafagCockpit/1.0 (Weltlage)");
        return c;
    }

    /// <summary>Netzwerkfehler, Zeitueberschreitung, 403/407 = nicht erreichbar (vermutlich Firewall); sonst Fehler.</summary>
    internal static (WorldSourceState State, string Message) Classify(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.ProxyAuthenticationRequired } h => (WorldSourceState.Blocked, $"HTTP {(int)h.StatusCode!}: vermutlich Firewall oder Proxy"),
        HttpRequestException { StatusCode: null } h => (WorldSourceState.Blocked, "nicht erreichbar, vermutlich Firewall: " + h.GetBaseException().Message),
        TaskCanceledException => (WorldSourceState.Blocked, "keine Antwort in 30 s, vermutlich Firewall"),
        HttpRequestException h => (WorldSourceState.Error, $"HTTP {(int)h.StatusCode!}"),
        _ => (WorldSourceState.Error, ex.GetBaseException().Message)
    };

    private async Task RefreshGdeltAsync(CancellationToken ct)
    {
        using var client = Client();
        try
        {
            var last = await client.GetStringAsync("http://data.gdeltproject.org/gdeltv2/lastupdate.txt", ct);
            var latestUrl = WorldParsers.LatestExportUrl(last) ?? throw new InvalidDataException("lastupdate.txt ohne Ereignisdatei");
            var latest = WorldParsers.StampOf(latestUrl) ?? throw new InvalidDataException("Zeitstempel unbekannt");
            // Nach dem Start einmal 24 h fuer die Top-Ereignisse (im Speicher), danach nur noch neue Dateien.
            var stored = await LoadProcessedStampsAsync(latest.AddHours(-26), ct);
            var done = _gdeltFilled ? stored : new HashSet<string>();
            var fresh = new List<WorldEvent>();
            int files = 0, failed = 0;
            for (var stamp = latest.AddHours(-24); stamp <= latest; stamp = stamp.AddMinutes(15))
            {
                var key = stamp.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                if (done.Contains(key)) continue;
                List<WorldEvent>? events;
                try
                {
                    events = await ReadExportAsync(client, WorldParsers.ExportUrl(stamp), ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
                {
                    failed++;
                    continue;
                }
                if (events is null) continue;
                fresh.AddRange(events.Where(e => e.QuadClass >= 3 || e.Goldstein <= -5));
                if (!stored.Contains(key))
                {
                    await AddDailyAsync(WorldParsers.Aggregate(events), key, events.Count, ct);
                    files++;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
            }
            _gdeltFilled = true;
            lock (_lock)
            {
                // Stark beachtete Konflikte und negative Ereignisse der letzten 24 h; nur Link, kein Artikeltext.
                var since = latest.AddHours(-24);
                _topEvents = _topEvents.Concat(fresh).Where(e => e.TimeUtc >= since || e.TimeUtc == DateTime.MinValue)
                    .OrderByDescending(e => e.Mentions).Take(400).ToList();
            }
            var msg = $"{files} neue Dateien" + (failed > 0 ? $", {failed} nicht lesbar" : "");
            await SaveCacheAsync("gdelt", WorldSourceState.Ok, msg, "", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            var (state, message) = Classify(ex);
            await SaveCacheAsync("gdelt", state, message, "", ct);
        }
    }

    private static async Task<List<WorldEvent>?> ReadExportAsync(HttpClient client, string url, CancellationToken ct)
    {
        using var response = await client.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = zip.Entries.FirstOrDefault();
        if (entry is null) return null;
        using var reader = new StreamReader(entry.Open());
        var list = new List<WorldEvent>();
        while (await reader.ReadLineAsync(ct) is { } line)
            if (WorldParsers.ParseGdeltEvent(line) is { } e)
                list.Add(e);
        return list;
    }

    private async Task RefreshSlowSourcesAsync(CancellationToken ct)
    {
        using var client = Client();
        // FRED: Rohstoffe, Oel, Finanzstress (CSV ohne Schluessel).
        try
        {
            var series = new List<WorldSeries>();
            foreach (var (key, name, unit) in FredSeries)
            {
                var csv = await client.GetStringAsync($"https://fred.stlouisfed.org/graph/fredgraph.csv?id={key}", ct);
                var points = WorldParsers.ParseFredCsv(csv).Where(p => p.Item1 >= DateOnly.FromDateTime(DateTime.Today).AddYears(-3)).ToList();
                series.Add(new WorldSeries(key, name, unit, points));
            }
            await SaveCacheAsync("fred", WorldSourceState.Ok, $"{series.Count} Reihen", JsonSerializer.Serialize(series.Select(s => new SeriesDto(s.Key, s.Name, s.Unit, s.Points.Select(p => new PointDto(p.Date, p.Value)).ToList()))), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            var (state, message) = Classify(ex);
            await SaveCacheAsync("fred", state, message, null, ct);
        }

        // Laender, die fuer uns zaehlen: Kunden- und Lieferlaender mit Umsatz bzw. Einkauf.
        List<string> countries;
        try
        {
            countries = await RelevantCountriesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Weltlage: Laenderliste aus Verkauf/Einkauf nicht lesbar, feste Liste");
            countries = [.. BaseCountries];
        }

        // Eurostat: Industrieproduktion je EU-Land (Index 2021=100, saisonbereinigt).
        try
        {
            var industry = new List<WorldSeries>();
            var skipped = new List<string>();
            Exception? first = null;
            foreach (var c in countries.Where(c => EuCountries.Contains(c)))
            {
                try
                {
                    var json = await client.GetStringAsync(
                        $"https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/sts_inpr_m?geo={WorldParsers.EurostatGeo(c)}&nace_r2=B-D&s_adj=SCA&unit=I21&lastTimePeriod=24&format=JSON", ct);
                    industry.Add(new WorldSeries(c, c, "2021=100", WorldParsers.ParseEurostat(json)));
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
                {
                    first ??= ex;
                    skipped.Add(c);
                    // Erste Antwort schon nicht erreichbar: nicht jedes Land 30 s abwarten.
                    if (industry.Count == 0 && Classify(ex).State == WorldSourceState.Blocked) throw;
                }
                await Task.Delay(300, ct);
            }
            // Keine einzige Antwort: Zustand der ersten Ausnahme (meist Firewall).
            if (industry.Count == 0 && first is not null) throw first;
            await SaveCacheAsync("eurostat", WorldSourceState.Ok, $"{industry.Count} Laender" + (skipped.Count > 0 ? ", ohne Reihe: " + string.Join(" ", skipped) : ""), JsonSerializer.Serialize(industry.Select(s => new SeriesDto(s.Key, s.Name, s.Unit, s.Points.Select(p => new PointDto(p.Date, p.Value)).ToList()))), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            var (state, message) = Classify(ex);
            await SaveCacheAsync("eurostat", state, message, null, ct);
        }

        // IMF: BIP-Wachstum laufendes und naechstes Jahr.
        try
        {
            var year = DateTime.Today.Year;
            var json = await client.GetStringAsync($"https://www.imf.org/external/datamapper/api/v1/NGDP_RPCH?periods={year},{year + 1}", ct);
            var growth = WorldParsers.ParseImf(json, countries, year);
            await SaveCacheAsync("imf", WorldSourceState.Ok, $"{growth.Count} Laender", JsonSerializer.Serialize(growth), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            var (state, message) = Classify(ex);
            await SaveCacheAsync("imf", state, message, null, ct);
        }
    }

    /// <summary>Unsere Standortlaender, immer dabei.</summary>
    internal static readonly string[] BaseCountries = ["CH", "AT", "DE", "IT", "FR", "ES", "GB", "IN", "US"];

    internal static readonly HashSet<string> EuCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE", "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE"
    };

    private async Task<List<string>> RelevantCountriesAsync(CancellationToken ct)
    {
        var data = await _sales.GetAsync();
        var fromSales = data.Facts.Where(f => f.CustomerCountry.Length == 2 && f.Date >= data.ReferenceEnd.AddMonths(-12))
            .GroupBy(f => f.CustomerCountry).OrderByDescending(g => g.Sum(f => f.ValueChf)).Take(20).Select(g => g.Key);
        var fromPurchasing = (await QueryAsync("SELECT SupplierCountry FROM PurchasingEkkoCache WHERE COALESCE(SupplierCountry,'') <> '' GROUP BY SupplierCountry ORDER BY COUNT(*) DESC LIMIT 15", ct))
            .Select(r => Convert.ToString(r[0]) ?? "");
        return fromSales.Concat(fromPurchasing).Concat(BaseCountries)
            .Where(c => c.Length == 2).Select(c => c.ToUpperInvariant()).Distinct().ToList();
    }

    public sealed record PointDto(DateOnly Date, double Value);
    public sealed record SeriesDto(string Key, string Name, string Unit, List<PointDto> Points);

    /// <summary>Stand fuer die Seiten: Quellenzustand, Ereignistage (14 Tage), Top-Ereignisse, Reihen.</summary>
    public async Task<WorldSnapshot> GetAsync(CancellationToken ct = default)
    {
        var cache = (await QueryAsync("SELECT Key, FetchedAtUtc, State, Message, Payload FROM WorldSourceCache", ct))
            .ToDictionary(r => Convert.ToString(r[0])!, r => (At: DateTime.Parse(Convert.ToString(r[1])!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                State: Enum.Parse<WorldSourceState>(Convert.ToString(r[2])!), Message: Convert.ToString(r[3]) ?? "", Payload: Convert.ToString(r[4]) ?? ""));
        WorldSourceStatus Status(string key, string name, string host)
            => cache.TryGetValue(key, out var c) ? new WorldSourceStatus(key, name, host, c.State, c.Message, c.At) : new WorldSourceStatus(key, name, host, WorldSourceState.Pending, "ausstehend", null);
        IReadOnlyList<WorldSeries> Series(string key)
            => cache.TryGetValue(key, out var c) && c.Payload.Length > 2
                ? (JsonSerializer.Deserialize<List<SeriesDto>>(c.Payload) ?? []).Select(s => new WorldSeries(s.Key, s.Name, s.Unit, s.Points.Select(p => (p.Date, p.Value)).ToList())).ToList()
                : [];
        var days = (await QueryAsync("SELECT Day, Country, Events, Conflict, Protest, Mentions, ToneSum, GoldsteinSum FROM WorldEventDaily WHERE Day >= $d",
                ct, ("$d", DateTime.UtcNow.Date.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))))
            .Select(r => new WorldEventDay(DateOnly.ParseExact(Convert.ToString(r[0])!, "yyyy-MM-dd", CultureInfo.InvariantCulture), Convert.ToString(r[1])!,
                Convert.ToInt32(r[2]), Convert.ToInt32(r[3]), Convert.ToInt32(r[4]), Convert.ToInt64(r[5]), Convert.ToDouble(r[6]), Convert.ToDouble(r[7])))
            .ToList();
        List<WorldEvent> top;
        lock (_lock) top = _topEvents.ToList();
        return new WorldSnapshot
        {
            UpdatedAtUtc = cache.Values.Select(c => (DateTime?)c.At).DefaultIfEmpty(null).Max(),
            Sources =
            [
                Status("gdelt", "GDELT Ereignisse", "data.gdeltproject.org"),
                Status("fred", "FRED Rohstoffe und Finanzstress", "fred.stlouisfed.org"),
                Status("eurostat", "Eurostat Industrieproduktion", "ec.europa.eu"),
                Status("imf", "IMF Wirtschaftswachstum", "www.imf.org"),
                new WorldSourceStatus("ecb", "EZB Wechselkurse", "www.ecb.europa.eu", WorldSourceState.Ok, "aus der Kurstabelle des Cockpits", null),
                new WorldSourceStatus("boerse", "Börsenindizes (SMI, DAX)", "–", WorldSourceState.Error, "keine freie Quelle ohne Anmeldung; Ersatz: Finanzstress-Index", null)
            ],
            EventDays = days,
            TopEvents = top,
            Series = Series("fred"),
            Industry = Series("eurostat"),
            Growth = cache.TryGetValue("imf", out var imf) && imf.Payload.Length > 2 ? JsonSerializer.Deserialize<List<WorldGrowth>>(imf.Payload) ?? [] : []
        };
    }

    private async Task<HashSet<string>> LoadProcessedStampsAsync(DateTime sinceUtc, CancellationToken ct)
        => (await QueryAsync("SELECT Stamp FROM WorldGdeltFiles WHERE Stamp >= $s", ct, ("$s", sinceUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture))))
            .Select(r => Convert.ToString(r[0])!).ToHashSet();

    private async Task AddDailyAsync(IReadOnlyList<WorldEventDay> days, string stamp, int rows, CancellationToken ct)
        => await WithConnectionAsync(async conn =>
        {
            await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
            foreach (var d in days)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO WorldEventDaily (Day, Country, Events, Conflict, Protest, Mentions, ToneSum, GoldsteinSum)
VALUES ($d, $c, $e, $k, $p, $m, $t, $g)
ON CONFLICT(Day, Country) DO UPDATE SET Events = Events + $e, Conflict = Conflict + $k, Protest = Protest + $p,
    Mentions = Mentions + $m, ToneSum = ToneSum + $t, GoldsteinSum = GoldsteinSum + $g;";
                cmd.Parameters.AddWithValue("$d", d.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("$c", d.Country);
                cmd.Parameters.AddWithValue("$e", d.Events);
                cmd.Parameters.AddWithValue("$k", d.Conflict);
                cmd.Parameters.AddWithValue("$p", d.Protest);
                cmd.Parameters.AddWithValue("$m", d.Mentions);
                cmd.Parameters.AddWithValue("$t", d.ToneSum);
                cmd.Parameters.AddWithValue("$g", d.GoldsteinSum);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            await using (var f = conn.CreateCommand())
            {
                f.Transaction = tx;
                f.CommandText = "INSERT OR IGNORE INTO WorldGdeltFiles (Stamp, Rows) VALUES ($s, $r)";
                f.Parameters.AddWithValue("$s", stamp);
                f.Parameters.AddWithValue("$r", rows);
                await f.ExecuteNonQueryAsync(ct);
            }
            await using (var clean = conn.CreateCommand())
            {
                clean.Transaction = tx;
                clean.CommandText = "DELETE FROM WorldEventDaily WHERE Day < $d; DELETE FROM WorldGdeltFiles WHERE Stamp < $s;";
                clean.Parameters.AddWithValue("$d", DateTime.UtcNow.Date.AddDays(-60).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                clean.Parameters.AddWithValue("$s", DateTime.UtcNow.AddDays(-3).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
                await clean.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct);
        }, ct);

    private async Task SaveCacheAsync(string key, WorldSourceState state, string message, string? payload, CancellationToken ct)
        => await WithConnectionAsync(async conn =>
        {
            await using var cmd = conn.CreateCommand();
            // Bei Fehler bleibt die letzte gute Nutzlast stehen (payload null), nur Zustand und Meldung aendern sich.
            cmd.CommandText = payload is null
                ? @"INSERT INTO WorldSourceCache (Key, FetchedAtUtc, State, Message, Payload) VALUES ($k, $a, $s, $m, '')
                    ON CONFLICT(Key) DO UPDATE SET FetchedAtUtc = $a, State = $s, Message = $m;"
                : @"INSERT OR REPLACE INTO WorldSourceCache (Key, FetchedAtUtc, State, Message, Payload) VALUES ($k, $a, $s, $m, $p);";
            cmd.Parameters.AddWithValue("$k", key);
            cmd.Parameters.AddWithValue("$a", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$s", state.ToString());
            cmd.Parameters.AddWithValue("$m", message);
            if (payload is not null) cmd.Parameters.AddWithValue("$p", payload);
            await cmd.ExecuteNonQueryAsync(ct);
        }, ct);

    internal async Task<List<object?[]>> QueryAsync(string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        var rows = new List<object?[]>();
        await WithConnectionAsync(async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (n, v) in parameters) cmd.Parameters.AddWithValue(n, v);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var row = new object?[r.FieldCount];
                for (var i = 0; i < r.FieldCount; i++) row[i] = r.IsDBNull(i) ? null : r.GetValue(i);
                rows.Add(row);
            }
        }, ct);
        return rows;
    }

    private async Task WithConnectionAsync(Func<SqliteConnection, Task> work, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);
        await work(conn);
    }
}
