using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Services;

public enum MonthlyReviewQmState
{
    /// <summary>Schalter MonthlyReview:QmEnabled aus; kein SAP-Aufruf.</summary>
    Disabled,
    /// <summary>Abruf fehlgeschlagen (z. B. Set noch nicht transportiert, Zeitlimit, Anmeldung).</summary>
    Error,
    /// <summary>Abruf erfolgreich, aber keine offene Meldung nach den Filtern.</summary>
    Empty,
    Ok
}

/// <summary>Stand der Q-Faelle fuer die Seite; unveraenderlich, wird je Abruf ersetzt.</summary>
public sealed class MonthlyReviewQmSnapshot
{
    public MonthlyReviewQmState State { get; init; }
    public IReadOnlyList<MonthlyReviewQmCase> Cases { get; init; } = [];
    /// <summary>Zeitpunkt des Abrufs; das Datum ist der Stichtag fuer "Offen seit".</summary>
    public DateTime? FetchedAt { get; init; }
    public DateOnly? Reference => FetchedAt is { } f ? DateOnly.FromDateTime(f) : null;
    /// <summary>Zeilen, die SAP geliefert hat, vor den Filtern (Meldungsart, Systemstatus).</summary>
    public int RowsDelivered { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Holt die offenen QM-Meldungen (QmMeldungSet, Service ZPOWERBI_EINKAUF_SRV) fuer die Monthly Review. Verbindung wie
/// Shopfloor, Logistik live und Einkauf (<see cref="ShopfloorSapSync.ResolveConnectionAsync"/>). Ein Abruf zur Zeit,
/// gemeinsam fuer alle Betrachter: Erfolg <see cref="MonthlyReviewOptions.QmCacheMinutes"/> gemerkt, Fehler nur
/// <see cref="MonthlyReviewOptions.QmErrorCacheSeconds"/>. Ausgeschaltet (Vorgabe) gibt es keinen SAP-Aufruf.
/// Doku docs/monthly_review/MONTHLY_REVIEW_2026-11-04.md.
/// </summary>
public sealed class MonthlyReviewQmService
{
    private const int PageSize = 5000;
    private const int MaxPages = 20;

    private readonly IServiceProvider _services;
    private readonly IOptionsMonitor<MonthlyReviewOptions> _options;
    private readonly ILogger<MonthlyReviewQmService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private MonthlyReviewQmSnapshot? _cache;

    public MonthlyReviewQmService(IServiceProvider services, IOptionsMonitor<MonthlyReviewOptions> options, ILogger<MonthlyReviewQmService> logger)
    {
        _services = services;
        _options = options;
        _logger = logger;
    }

    /// <summary>Nur fuer Tests: liefert die Zeilen statt SAP (Parameter: Optionen).</summary>
    internal Func<MonthlyReviewOptions, CancellationToken, Task<IReadOnlyList<JsonElement>>>? FetchOverride { get; set; }

    /// <summary>Nur fuer Tests: aktuelle Zeit.</summary>
    internal Func<DateTime> Now { get; set; } = () => DateTime.Now;

    public async Task<MonthlyReviewQmSnapshot> GetAsync(CancellationToken ct = default)
    {
        var options = _options.CurrentValue;
        if (!options.QmEnabled)
            return new MonthlyReviewQmSnapshot { State = MonthlyReviewQmState.Disabled };
        await _gate.WaitAsync(ct);
        try
        {
            if (_cache is { } c && IsFresh(c, options))
                return c;
            _cache = await LoadAsync(options, ct);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsFresh(MonthlyReviewQmSnapshot s, MonthlyReviewOptions o)
    {
        if (s.FetchedAt is not { } at) return false;
        var ttl = s.State == MonthlyReviewQmState.Error
            ? TimeSpan.FromSeconds(Math.Clamp(o.QmErrorCacheSeconds, 5, 3600))
            : TimeSpan.FromMinutes(Math.Clamp(o.QmCacheMinutes, 1, 24 * 60));
        return Now() - at < ttl;
    }

    private async Task<MonthlyReviewQmSnapshot> LoadAsync(MonthlyReviewOptions options, CancellationToken ct)
    {
        var at = Now();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.QmTimeoutSeconds, 5, 600)));
            var elements = FetchOverride is not null
                ? await FetchOverride(options, timeout.Token)
                : await FetchAsync(options, timeout.Token);
            var rows = MonthlyReviewQm.ParseElements(elements);
            var groups = await LoadProductGroupsAsync(ct);
            if (groups.Count > 0)
                rows = MonthlyReviewQm.FillProductLines(rows, m => groups.GetValueOrDefault(PurchasingComponentDispoLoader.NormalizeKey(m)));
            var cases = MonthlyReviewQm.BuildCases(rows, options, DateOnly.FromDateTime(at));
            return new MonthlyReviewQmSnapshot
            {
                State = cases.Count == 0 ? MonthlyReviewQmState.Empty : MonthlyReviewQmState.Ok,
                Cases = cases,
                FetchedAt = at,
                RowsDelivered = rows.Count
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = ex is OperationCanceledException
                ? $"SAP {options.QmEntitySet}: keine Antwort innerhalb von {options.QmTimeoutSeconds} s."
                : ex.GetBaseException().Message;
            _logger.LogWarning(ex, "Monthly Review: Q-Faelle aus SAP fehlgeschlagen");
            return new MonthlyReviewQmSnapshot { State = MonthlyReviewQmState.Error, FetchedAt = at, Error = message };
        }
    }

    /// <summary>
    /// Produktgruppe je verkuerzter Nummer aus dem Einkauf-Cache (2026-11-10): zuerst ZC23 (PurchasingMaterialShareSales,
    /// Produktgruppe mit dem groessten Umsatz), sonst Disponent der Nummer (PurchasingComponentDispoCache) ueber die
    /// Disponentenregeln (PurchasingSpendDisponentRule). Fehlt die Datenbank oder eine Tabelle, bleibt die Zuordnung leer.
    /// </summary>
    private async Task<Dictionary<string, string>> LoadProductGroupsAsync(CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (FetchOverride is not null || _services.GetService(typeof(IDbContextFactory<AppDbContext>)) is not IDbContextFactory<AppDbContext> factory)
            return result;
        try
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Vknr, Produktgruppe FROM PurchasingMaterialShareSales WHERE Produktgruppe <> '' GROUP BY Vknr, Produktgruppe ORDER BY Vknr, SUM(Netwr) DESC;";
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    result.TryAdd(PurchasingComponentDispoLoader.NormalizeKey(r.GetString(0)), r.GetString(1));
            }
            var rules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DisponentPattern, ProductGroupText FROM PurchasingSpendDisponentRule WHERE ProductGroupText <> '';";
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    rules.TryAdd(r.GetString(0).Trim(), r.GetString(1));
            }
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DISTINCT Vknr, VknrDispo FROM PurchasingComponentDispoCache WHERE VknrDispo <> '';";
                await using var r = await cmd.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    if (rules.TryGetValue(r.GetString(1).Trim(), out var g))
                        result.TryAdd(PurchasingComponentDispoLoader.NormalizeKey(r.GetString(0)), g);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Monthly Review: Produktgruppen fuer Q-Faelle nicht gelesen");
        }
        return result;
    }

    private async Task<IReadOnlyList<JsonElement>> FetchAsync(MonthlyReviewOptions options, CancellationToken ct)
    {
        var (baseUrl, user, password) = await ShopfloorSapSync.ResolveConnectionAsync(_services, ct);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Clamp(options.QmTimeoutSeconds, 5, 600)) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await ReadAllAsync((url, token) => GetStringAsync(client, url, options.QmEntitySet, token), baseUrl, options, ct);
    }

    private static async Task<string> GetStringAsync(HttpClient client, string url, string set, CancellationToken ct)
    {
        using var response = await client.GetAsync(url, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"SAP OData {set}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}" + response.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => " (Set vermutlich noch nicht in diesem SAP-System oder Gateway-Cache nicht geleert).",
                System.Net.HttpStatusCode.Forbidden => " (dem technischen Benutzer fehlt die Berechtigung).",
                System.Net.HttpStatusCode.Unauthorized => " (Anmeldung abgelehnt).",
                _ => "."
            } + KpiLiefertreueSync.SapMessage(body));
        return body;
    }

    /// <summary>
    /// Liest alle Seiten. Eigene GET_ENTITYSET-Methoden ignorieren $top/$skip teils (LEARNINGS 2026-10-15): liefert eine
    /// Seite mehr als <see cref="PageSize"/> Zeilen oder keine neue Meldung, ist alles da. Doppelte Meldungen zaehlen einmal.
    /// </summary>
    internal static async Task<IReadOnlyList<JsonElement>> ReadAllAsync(Func<string, CancellationToken, Task<string>> get, string baseUrl,
        MonthlyReviewOptions options, CancellationToken ct)
    {
        var rows = new List<JsonElement>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var filter = options.QmFilter.Trim();
        for (var page = 0; page < MaxPages; page++)
        {
            var url = $"{baseUrl.TrimEnd('/')}/{options.QmEntitySet.Trim()}?$format=json&$top={PageSize}&$skip={page * PageSize}"
                      + (filter.Length > 0 ? "&$filter=" + Uri.EscapeDataString(filter) : string.Empty);
            using var document = JsonDocument.Parse(await get(url, ct));
            var pageRows = MonthlyReviewQm.Results(document.RootElement).ToList();
            var added = 0;
            foreach (var row in pageRows)
            {
                var key = row.TryGetProperty("Qmnum", out var n) ? n.ToString() : Guid.NewGuid().ToString();
                if (seen.Add(key))
                {
                    rows.Add(row);
                    added++;
                }
            }
            if (pageRows.Count != PageSize || added == 0)
                return rows;
        }
        throw new InvalidOperationException($"{options.QmEntitySet} lieferte mehr als {MaxPages} Seiten; Abbruch.");
    }
}

/// <summary>Registrierung der Monthly Review (eine Zeile in Program.cs).</summary>
public static class MonthlyReviewServiceCollectionExtensions
{
    public static IServiceCollection AddMonthlyReview(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MonthlyReviewOptions>(configuration.GetSection(MonthlyReviewOptions.SectionName));
        services.AddSingleton<MonthlyReviewQmService>();
        return services;
    }
}
