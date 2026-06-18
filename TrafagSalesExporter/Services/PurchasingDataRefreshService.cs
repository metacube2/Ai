using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public sealed class PurchasingDataRefreshService : IPurchasingDataRefreshService
{
    private const int PageSize = 1000;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppEventLogService _logService;

    public PurchasingDataRefreshService(IDbContextFactory<AppDbContext> dbFactory, IAppEventLogService logService)
    {
        _dbFactory = dbFactory;
        _logService = logService;
    }

    public async Task<PurchasingDataRefreshStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        var status = await ReadLatestStatusAsync(conn, cancellationToken);
        status.EkkoRows = await CountTableAsync(conn, "PurchasingEkkoCache", cancellationToken);
        status.EkpoRows = await CountTableAsync(conn, "PurchasingEkpoCache", cancellationToken);
        status.EketRows = await CountTableAsync(conn, "PurchasingEketCache", cancellationToken);
        return status;
    }

    public async Task<PurchasingDataRefreshStatus> RunFullLoadAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        await WriteStatusAsync("Full", "Running", started, null, fromDate, null, null, 0, 0, 0, "Full Load gestartet.", cancellationToken);
        await _logService.WriteAsync("Purchasing", "Einkauf Full Load gestartet", details: fromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        try
        {
            var connection = await ResolveConnectionAsync(cancellationToken);
            using var client = CreateClient(connection.Username, connection.Password);
            var nowText = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var ekkoFilter = fromDate.HasValue ? $"Bedat ge '{fromDate.Value:yyyy-MM-dd}'" : string.Empty;

            var ekkoRows = await ReadAllRowsAsync(client, connection.BaseUrl, "EKKOSet", "Ebeln,Bedat,Aedat,Lifnr,Bukrs,Konnr,Waers,Wkurs", ekkoFilter, "Ebeln", cancellationToken);
            var ekpoRows = await ReadAllRowsAsync(client, connection.BaseUrl, "EKPOSet", "Ebeln,Ebelp,Matnr,Txz01,Matkl,Menge,Ktmng,Netwr,Loekz,Bukrs,Werks", string.Empty, "Ebeln,Ebelp", cancellationToken);
            var eketRows = await ReadAllRowsAsync(client, connection.BaseUrl, "eketSet", "Ebeln,Ebelp,Etenr,Eindt,Menge,Wemng", string.Empty, "Ebeln,Ebelp,Etenr", cancellationToken);

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
            await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingEkkoCache;", cancellationToken);
            await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingEkpoCache;", cancellationToken);
            await ExecuteAsync(conn, transaction, "DELETE FROM PurchasingEketCache;", cancellationToken);
            await UpsertEkkoAsync(conn, transaction, ekkoRows, nowText, cancellationToken);
            await UpsertEkpoAsync(conn, transaction, ekpoRows, nowText, cancellationToken);
            await UpsertEketAsync(conn, transaction, eketRows, nowText, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var completed = DateTime.UtcNow;
            var message = $"Full Load abgeschlossen: EKKO={ekkoRows.Count:N0}, EKPO={ekpoRows.Count:N0}, EKET={eketRows.Count:N0}.";
            await WriteStatusAsync("Full", "Success", started, completed, fromDate, null, completed, ekkoRows.Count, ekpoRows.Count, eketRows.Count, message, cancellationToken);
            await _logService.WriteAsync("Purchasing", "Einkauf Full Load erfolgreich", details: message);
            return await GetStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var message = $"Full Load fehlgeschlagen: {ex.Message}";
            await WriteStatusAsync("Full", "Error", started, DateTime.UtcNow, fromDate, null, null, 0, 0, 0, message, cancellationToken);
            await _logService.WriteAsync("Purchasing", "Einkauf Full Load fehlgeschlagen", "Error", details: ex.ToString());
            return await GetStatusAsync(cancellationToken);
        }
    }

    public async Task<PurchasingDataRefreshStatus> RunDeltaAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var current = await GetStatusAsync(cancellationToken);
        var deltaFrom = fromDate ?? current.LastSuccessfulDeltaAtUtc ?? current.CompletedAtUtc ?? DateTime.UtcNow.AddDays(-7);
        var started = DateTime.UtcNow;
        await WriteStatusAsync("Delta", "Running", started, null, deltaFrom, null, current.LastSuccessfulDeltaAtUtc, current.EkkoRows, current.EkpoRows, current.EketRows, "Delta gestartet.", cancellationToken);

        try
        {
            var connection = await ResolveConnectionAsync(cancellationToken);
            using var client = CreateClient(connection.Username, connection.Password);
            var filter = $"Aedat ge '{deltaFrom:yyyy-MM-dd}'";
            var changedEkko = await ReadAllRowsAsync(client, connection.BaseUrl, "EKKOSet", "Ebeln,Bedat,Aedat,Lifnr,Bukrs,Konnr,Waers,Wkurs", filter, "Ebeln", cancellationToken);
            var ebelnKeys = changedEkko
                .Select(row => GetText(row, "Ebeln"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ekpoRows = new List<Dictionary<string, object?>>();
            var eketRows = new List<Dictionary<string, object?>>();
            foreach (var ebeln in ebelnKeys)
            {
                ekpoRows.AddRange(await ReadAllRowsAsync(client, connection.BaseUrl, "EKPOSet", "Ebeln,Ebelp,Matnr,Txz01,Matkl,Menge,Ktmng,Netwr,Loekz,Bukrs,Werks", $"Ebeln eq '{ebeln}'", "Ebelp", cancellationToken));
                eketRows.AddRange(await ReadAllRowsAsync(client, connection.BaseUrl, "eketSet", "Ebeln,Ebelp,Etenr,Eindt,Menge,Wemng", $"Ebeln eq '{ebeln}'", "Ebelp,Etenr", cancellationToken));
            }

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            var nowText = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
            await UpsertEkkoAsync(conn, transaction, changedEkko, nowText, cancellationToken);
            await UpsertEkpoAsync(conn, transaction, ekpoRows, nowText, cancellationToken);
            await UpsertEketAsync(conn, transaction, eketRows, nowText, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var completed = DateTime.UtcNow;
            var status = await GetStatusAsync(cancellationToken);
            var message = $"Delta abgeschlossen: geaenderte Belege={ebelnKeys.Count:N0}, EKPO={ekpoRows.Count:N0}, EKET={eketRows.Count:N0}.";
            await WriteStatusAsync("Delta", "Success", started, completed, deltaFrom, null, completed, status.EkkoRows, status.EkpoRows, status.EketRows, message, cancellationToken);
            await _logService.WriteAsync("Purchasing", "Einkauf Delta erfolgreich", details: message);
            return await GetStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await WriteStatusAsync("Delta", "Error", started, DateTime.UtcNow, deltaFrom, null, current.LastSuccessfulDeltaAtUtc, current.EkkoRows, current.EkpoRows, current.EketRows, $"Delta fehlgeschlagen: {ex.Message}", cancellationToken);
            await _logService.WriteAsync("Purchasing", "Einkauf Delta fehlgeschlagen", "Error", details: ex.ToString());
            return await GetStatusAsync(cancellationToken);
        }
    }

    private async Task<PurchasingSapConnection> ResolveConnectionAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var sap = await db.SourceSystemDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Code == "SAP", cancellationToken)
            ?? throw new InvalidOperationException("SAP Quelle fehlt.");
        var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(x => x.TSC == PurchasingDataSourcePageService.PurchasingTsc, cancellationToken)
            ?? throw new InvalidOperationException("Einkauf SAP Site fehlt.");
        var serviceUrl = string.IsNullOrWhiteSpace(site.SapServiceUrl) ? sap.CentralServiceUrl : site.SapServiceUrl;
        var username = string.IsNullOrWhiteSpace(site.UsernameOverride) ? sap.CentralUsername : site.UsernameOverride;
        var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? sap.CentralPassword : site.PasswordOverride;
        if (string.IsNullOrWhiteSpace(serviceUrl) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SAP URL oder Zugangsdaten fehlen.");
        return new PurchasingSapConnection(serviceUrl.TrimEnd('/') + "/", username, password);
    }

    private static HttpClient CreateClient(string username, string password)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadAllRowsAsync(HttpClient client, string baseUrl, string entitySet, string select, string filter, string orderBy, CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();
        for (var skip = 0; ; skip += PageSize)
        {
            var url = $"{baseUrl}{entitySet}?$format=json&$top={PageSize}&$skip={skip}&$select={Uri.EscapeDataString(select)}";
            if (!string.IsNullOrWhiteSpace(orderBy))
                url += $"&$orderby={Uri.EscapeDataString(orderBy)}";
            if (!string.IsNullOrWhiteSpace(filter))
                url += $"&$filter={Uri.EscapeDataString(filter)}";

            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"SAP OData {entitySet} fehlgeschlagen ({(int)response.StatusCode} {response.ReasonPhrase}) URL={url} Antwort={TrimForLog(error)}");
            }
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = ParseRows(json);
            if (page.Count == 0)
                return rows;
            rows.AddRange(page);
            if (page.Count < PageSize)
                return rows;
        }
    }

    private static List<Dictionary<string, object?>> ParseRows(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("d", out var d) ||
            !d.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
            return [];

        return results.EnumerateArray()
            .Select(item => item.EnumerateObject()
                .Where(property => property.Name != "__metadata")
                .ToDictionary(property => property.Name, property => ConvertJsonValue(property.Value), StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static async Task UpsertEkkoAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT OR REPLACE INTO PurchasingEkkoCache (Ebeln, Bedat, Aedat, Lifnr, SupplierName, Bukrs, Bsart, RawJson, LastLoadedAtUtc)
VALUES ($Ebeln, $Bedat, $Aedat, $Lifnr, $SupplierName, $Bukrs, $Bsart, $RawJson, $LastLoadedAtUtc);";
        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Ebeln"] = GetText(row, "Ebeln"),
                ["$Bedat"] = NormalizeSapDate(GetText(row, "Bedat")),
                ["$Aedat"] = NormalizeSapDate(GetText(row, "Aedat")),
                ["$Lifnr"] = GetText(row, "Lifnr"),
                ["$SupplierName"] = FirstNonEmpty(GetText(row, "SupplierName"), GetText(row, "Name1"), GetText(row, "Name")),
                ["$Bukrs"] = GetText(row, "Bukrs"),
                ["$Bsart"] = GetText(row, "Bsart"),
                ["$RawJson"] = JsonSerializer.Serialize(row),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    private static async Task UpsertEkpoAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT OR REPLACE INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Txz01, Matkl, Menge, Meins, Netwr, Loekz, Mstae, RawJson, LastLoadedAtUtc)
VALUES ($Ebeln, $Ebelp, $Matnr, $Txz01, $Matkl, $Menge, $Meins, $Netwr, $Loekz, $Mstae, $RawJson, $LastLoadedAtUtc);";
        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Ebeln"] = GetText(row, "Ebeln"),
                ["$Ebelp"] = GetText(row, "Ebelp"),
                ["$Matnr"] = GetText(row, "Matnr"),
                ["$Txz01"] = GetText(row, "Txz01"),
                ["$Matkl"] = GetText(row, "Matkl"),
                ["$Menge"] = GetText(row, "Menge"),
                ["$Meins"] = GetText(row, "Meins"),
                ["$Netwr"] = GetText(row, "Netwr"),
                ["$Loekz"] = GetText(row, "Loekz"),
                ["$Mstae"] = GetText(row, "Mstae"),
                ["$RawJson"] = JsonSerializer.Serialize(row),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    private static async Task UpsertEketAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT OR REPLACE INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, RawJson, LastLoadedAtUtc)
VALUES ($Ebeln, $Ebelp, $Etenr, $Eindt, $Menge, $Wemng, $RawJson, $LastLoadedAtUtc);";
        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Ebeln"] = GetText(row, "Ebeln"),
                ["$Ebelp"] = GetText(row, "Ebelp"),
                ["$Etenr"] = GetText(row, "Etenr"),
                ["$Eindt"] = NormalizeSapDate(GetText(row, "Eindt")),
                ["$Menge"] = GetText(row, "Menge"),
                ["$Wemng"] = GetText(row, "Wemng"),
                ["$RawJson"] = JsonSerializer.Serialize(row),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    private async Task WriteStatusAsync(string mode, string status, DateTime? startedAtUtc, DateTime? completedAtUtc, DateTime? fromDate, DateTime? toDate, DateTime? lastSuccessfulDeltaAtUtc, int ekkoRows, int ekpoRows, int eketRows, string message, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);
        const string sql = @"
INSERT INTO PurchasingSyncState (Mode, Status, StartedAtUtc, CompletedAtUtc, FromDate, ToDate, LastSuccessfulDeltaAtUtc, EkkoRows, EkpoRows, EketRows, Message)
VALUES ($Mode, $Status, $StartedAtUtc, $CompletedAtUtc, $FromDate, $ToDate, $LastSuccessfulDeltaAtUtc, $EkkoRows, $EkpoRows, $EketRows, $Message);";
        await ExecuteWithParametersAsync(conn, null, sql, new()
        {
            ["$Mode"] = mode,
            ["$Status"] = status,
            ["$StartedAtUtc"] = FormatDateTime(startedAtUtc),
            ["$CompletedAtUtc"] = FormatDateTime(completedAtUtc),
            ["$FromDate"] = FormatDate(fromDate),
            ["$ToDate"] = FormatDate(toDate),
            ["$LastSuccessfulDeltaAtUtc"] = FormatDateTime(lastSuccessfulDeltaAtUtc),
            ["$EkkoRows"] = ekkoRows,
            ["$EkpoRows"] = ekpoRows,
            ["$EketRows"] = eketRows,
            ["$Message"] = message
        }, cancellationToken);
    }

    private static async Task<PurchasingDataRefreshStatus> ReadLatestStatusAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT Mode, Status, StartedAtUtc, CompletedAtUtc, FromDate, ToDate, LastSuccessfulDeltaAtUtc, EkkoRows, EkpoRows, EketRows, Message
FROM PurchasingSyncState
ORDER BY Id DESC
LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new PurchasingDataRefreshStatus { Status = "Empty", Message = "Noch kein Einkauf Full Load ausgefuehrt." };

        return new PurchasingDataRefreshStatus
        {
            Mode = reader.GetString(0),
            Status = reader.GetString(1),
            StartedAtUtc = ParseDateTime(reader.GetString(2)),
            CompletedAtUtc = ParseDateTime(reader.GetString(3)),
            FromDate = ParseDate(reader.GetString(4)),
            ToDate = ParseDate(reader.GetString(5)),
            LastSuccessfulDeltaAtUtc = ParseDateTime(reader.GetString(6)),
            EkkoRows = reader.GetInt32(7),
            EkpoRows = reader.GetInt32(8),
            EketRows = reader.GetInt32(9),
            Message = reader.GetString(10)
        };
    }

    private static async Task<int> CountTableAsync(SqliteConnection conn, string tableName, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {tableName};";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteAsync(SqliteConnection conn, SqliteTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteWithParametersAsync(SqliteConnection conn, SqliteTransaction? transaction, string sql, Dictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (key, value) in parameters)
            command.Parameters.AddWithValue(key, value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static object? ConvertJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => value.ToString()
    };

    private static string GetText(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string TrimForLog(string value)
        => value.Length <= 1000 ? value : value[..1000] + "...";

    private static string? NormalizeSapDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
            ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : value;
    }

    private static string FormatDateTime(DateTime? value)
        => value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatDate(DateTime? value)
        => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static DateTime? ParseDateTime(string value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;

    private static DateTime? ParseDate(string value)
        => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;

    private sealed record PurchasingSapConnection(string BaseUrl, string Username, string Password);
}
