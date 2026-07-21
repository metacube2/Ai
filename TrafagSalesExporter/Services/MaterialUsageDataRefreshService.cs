using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Laedt MaterialUsageSet/MaterialParentSet - siehe docs/abap/README_LZCODE_WEBSERVICE.md und
/// docs/abap/ZCL_LZCODE_PROVIDER.abap. Diese EntitySets existieren im Gateway-Service
/// ZPOWERBI_EINKAUF_SRV noch NICHT (Entwurf fuer Lucas); dieser Dienst prueft das per
/// Metadaten-Abfrage und meldet fachlich klar, wenn sie fehlen, statt einen rohen HTTP-Fehler
/// zu zeigen (gleiches Muster wie FinancialJournalRefreshService fuer FinanzJournalSet).
/// Nutzt dieselbe SAP-Verbindung wie der Einkauf-Full-Load (Site PURCHASING_SAP), weil beide
/// EntitySets am selben Gateway-Service haengen.
/// </summary>
public sealed class MaterialUsageDataRefreshService : IMaterialUsageDataRefreshService
{
    private const int PageSize = 1000;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ISapGatewayService _sapGatewayService;
    private readonly IAppEventLogService _logService;

    public MaterialUsageDataRefreshService(
        IDbContextFactory<AppDbContext> dbFactory,
        ISapGatewayService sapGatewayService,
        IAppEventLogService logService)
    {
        _dbFactory = dbFactory;
        _sapGatewayService = sapGatewayService;
        _logService = logService;
    }

    public async Task<MaterialUsageRefreshStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        return await ReadLatestStatusAsync(conn, cancellationToken);
    }

    public async Task<MaterialUsageRefreshStatus> RunFullLoadAsync(bool topDown = true, string? materialFilter = null, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        await WriteStatusAsync("Running", started, null, 0, 0, "Full Load gestartet.", cancellationToken);
        await _logService.WriteAsync("MaterialUsage", "Stuecklistenanalyse Full Load gestartet",
            details: (topDown ? "Richtung=TOPDOWN" : "Richtung=BOTTOMUP") +
                     (string.IsNullOrWhiteSpace(materialFilter) ? string.Empty : $" | Materialfilter={materialFilter}"));

        try
        {
            var connection = await ResolveConnectionAsync(cancellationToken);

            // EntitySet-Namen dynamisch aufloesen: SEGW hat die Sets nach den DDIC-Strukturen
            // benannt (ZSTR_LZCODE_USAGE..., exakte Schreibweise inkl. Set-Suffix/Kuerzung je
            // nach Anlage), nicht nach dem urspruenglichen Doku-Vorschlag MaterialUsageSet.
            // Der normalisierte Vergleich (ohne Unterstriche, case-insensitiv) findet beide
            // Namenswelten, ohne dass hier je nach SEGW-Stand nachgezogen werden muss.
            var entitySets = await _sapGatewayService.GetEntitySetsAsync(connection.BaseUrl, connection.Username, connection.Password, cancellationToken);
            var usageSetName = ResolveEntitySetName(entitySets, "lzcodeusage", "materialusage");
            var parentSetName = ResolveEntitySetName(entitySets, "lzcodeparent", "materialparent");
            if (usageSetName is null || parentSetName is null)
            {
                var missingMessage = "SAP-Gateway-Service liefert (noch) kein passendes EntitySet fuer die Stuecklistenanalyse " +
                    $"(gesucht: Name enthaelt LZCODE_USAGE/LZCODE_PARENT oder MaterialUsage/MaterialParent; verfuegbar: {entitySets.Count} Sets). " +
                    "SEGW-Anlage laut docs/abap/README_LZCODE_WEBSERVICE.md pruefen, ggf. Gateway-Metadaten-Cache leeren. Kein Datenschaden, kein Retry noetig.";
                await WriteStatusAsync("Error", started, DateTime.UtcNow, 0, 0, missingMessage, cancellationToken);
                await _logService.WriteAsync("MaterialUsage", "Stuecklistenanalyse Full Load uebersprungen - EntitySet fehlt", "Warning", details: missingMessage);
                return await GetStatusAsync(cancellationToken);
            }

            // Die SAP-Seite ERZWINGT einen Vknr-/Kompnr-Filter (Guard gegen versehentliche
            // Vollselektion auf MARA, analog Report-Meldung "Bitte Selektion eingeben").
            // Ein Full Load ohne konkrete Materialliste schickt deshalb bewusst den
            // Catch-all "gt ''" mit - das ist die explizite "ja, wirklich alles"-Ansage.
            var materialProperty = topDown ? "Vknr" : "Kompnr";
            var materialValues = (materialFilter ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            var materialClause = materialValues.Count == 0
                ? $"{materialProperty} gt ''"
                : "(" + string.Join(" or ", materialValues.Select(value => $"{materialProperty} eq '{value}'")) + ")";
            var usageFilter = $"Richtung eq '{(topDown ? "TOPDOWN" : "BOTTOMUP")}' and {materialClause}";

            using var client = CreateClient(connection.Username, connection.Password);
            var usageRows = await ReadAllRowsAsync(client, connection.BaseUrl, usageSetName, usageFilter, cancellationToken);
            var parentRows = topDown
                ? await ReadAllRowsAsync(client, connection.BaseUrl, parentSetName, "Kompnr gt ''", cancellationToken)
                : [];

            var nowText = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var conn = (SqliteConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(cancellationToken);

            await using var transaction = (SqliteTransaction)await conn.BeginTransactionAsync(cancellationToken);
            await ExecuteAsync(conn, transaction, "DELETE FROM MaterialUsageCache WHERE Richtung = $Richtung;",
                new() { ["$Richtung"] = topDown ? "TOPDOWN" : "BOTTOMUP" }, cancellationToken);
            await UpsertUsageAsync(conn, transaction, usageRows, nowText, cancellationToken);
            if (topDown)
            {
                await ExecuteAsync(conn, transaction, "DELETE FROM MaterialParentCache;", null, cancellationToken);
                await UpsertParentAsync(conn, transaction, parentRows, nowText, cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);

            var completed = DateTime.UtcNow;
            var message = $"Full Load abgeschlossen: {usageSetName}={usageRows.Count:N0}, {parentSetName}={parentRows.Count:N0}.";
            if (usageRows.Count == 0 && parentRows.Count == 0)
                message += " Hinweis: 0 Zeilen ist gegen travt762 (TEST) erwartet - ZAT_VC ist dort leer " +
                           "(live verifiziert 2026-07-21), echte Daten liegen auf travp762 (PROD).";
            await WriteStatusAsync("Success", started, completed, usageRows.Count, parentRows.Count, message, cancellationToken);
            await _logService.WriteAsync("MaterialUsage", "Stuecklistenanalyse Full Load erfolgreich", details: message);
            return await GetStatusAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var message = $"Full Load fehlgeschlagen: {ex.Message}";
            await WriteStatusAsync("Error", started, DateTime.UtcNow, 0, 0, message, cancellationToken);
            await _logService.WriteAsync("MaterialUsage", "Stuecklistenanalyse Full Load fehlgeschlagen", "Error", details: ex.ToString());
            return await GetStatusAsync(cancellationToken);
        }
    }

    public async Task<List<MaterialUsagePreviewRow>> GetCachedUsageRowsAsync(string? materialFilter = null, int limit = 200, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        var hasFilter = !string.IsNullOrWhiteSpace(materialFilter);
        await using var command = conn.CreateCommand();
        command.CommandText = $@"
SELECT Richtung, Vknr, Kompnr, KompnrMaktx, KompnrMeins, Menge, Exklusiv,
       Labst, Endbestand, Stueckkosten, WertEndbestand, Mstae, Zzlzcod
FROM MaterialUsageCache
{(hasFilter ? "WHERE Vknr LIKE $Filter OR Kompnr LIKE $Filter" : string.Empty)}
ORDER BY Vknr, Kompnr
LIMIT $Limit;";
        if (hasFilter)
            command.Parameters.AddWithValue("$Filter", "%" + materialFilter!.Trim() + "%");
        command.Parameters.AddWithValue("$Limit", limit);

        var rows = new List<MaterialUsagePreviewRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new MaterialUsagePreviewRow(
                Richtung: reader.GetString(0),
                Vknr: reader.GetString(1),
                Kompnr: reader.GetString(2),
                KompnrMaktx: reader.GetString(3),
                KompnrMeins: reader.GetString(4),
                Menge: reader.GetString(5),
                Exklusiv: !reader.IsDBNull(6) && reader.GetInt32(6) != 0,
                Labst: reader.GetString(7),
                Endbestand: reader.GetString(8),
                Stueckkosten: reader.GetString(9),
                WertEndbestand: reader.GetString(10),
                Mstae: reader.GetString(11),
                Zzlzcod: reader.GetString(12)));
        }

        return rows;
    }

    private async Task<MaterialUsageSapConnection> ResolveConnectionAsync(CancellationToken cancellationToken)
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
        return new MaterialUsageSapConnection(serviceUrl.TrimEnd('/') + "/", username, password);
    }

    private static HttpClient CreateClient(string username, string password)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadAllRowsAsync(HttpClient client, string baseUrl, string entitySet, string filter, CancellationToken cancellationToken)
    {
        var rows = new List<Dictionary<string, object?>>();
        for (var skip = 0; ; skip += PageSize)
        {
            var url = $"{baseUrl}{entitySet}?$format=json&$top={PageSize}&$skip={skip}";
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

    /// <summary>
    /// Findet in der EntitySet-Liste des Gateway-Service das Set, dessen normalisierter Name
    /// (nur Buchstaben/Ziffern, lowercase) eine der Suchnadeln enthaelt. Noetig, weil der
    /// exakte SEGW-Name nicht festgelegt ist: je nach Anlage heisst das Set
    /// "ZSTR_LZCODE_USAGESet", "ZSTR_LZCODE_USAGE" oder "MaterialUsageSet".
    /// </summary>
    public static string? ResolveEntitySetName(IReadOnlyCollection<string> entitySets, params string[] normalizedNeedles)
    {
        foreach (var candidate in entitySets)
        {
            var normalized = new string(candidate.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            if (normalizedNeedles.Any(normalized.Contains))
                return candidate;
        }

        return null;
    }

    private static List<Dictionary<string, object?>> ParseRows(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("d", out var d) ||
            !d.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
            return [];

        // Property-Keys ohne Unterstriche ablegen: je nach SEGW-Anlage heissen die OData-
        // Properties "VknrMstae" (CamelCase-Konvention wie bei EKKOSet/WavwrDc) ODER roh
        // "VKNR_MSTAE" (Strukturfeldname). Das Dictionary ist ohnehin case-insensitiv;
        // mit dem Unterstrich-Strip treffen die GetText("VknrMstae")-Reads beide Varianten.
        return results.EnumerateArray()
            .Select(item => item.EnumerateObject()
                .Where(property => property.Name != "__metadata")
                .ToDictionary(
                    property => property.Name.Replace("_", string.Empty),
                    property => ConvertJsonValue(property.Value),
                    StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static async Task UpsertUsageAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO MaterialUsageCache (
    Richtung, Vknr, VknrMstae, VknrVerbrauch, Kompnr, KompnrMaktx, KompnrMeins, Menge, Exklusiv,
    Verbrauch, Labst, FesteZugang, GeplZugang, FesteAbgang, GeplAbgang, Endbestand, Omeng, Mkmng,
    Stueckkosten, WertFesteZug, WertGeplZug, WertFesteAbg, WertGeplAbg, WertEndbestand, Owert, Omkwr,
    Dismm, Minbe, Disls, Bstfe, Eisbe, Mstae, Mstav, Beskz, Zzlzcod, Zzlzcodsort, Baugruppe, Waers,
    RawJson, LastLoadedAtUtc)
VALUES (
    $Richtung, $Vknr, $VknrMstae, $VknrVerbrauch, $Kompnr, $KompnrMaktx, $KompnrMeins, $Menge, $Exklusiv,
    $Verbrauch, $Labst, $FesteZugang, $GeplZugang, $FesteAbgang, $GeplAbgang, $Endbestand, $Omeng, $Mkmng,
    $Stueckkosten, $WertFesteZug, $WertGeplZug, $WertFesteAbg, $WertGeplAbg, $WertEndbestand, $Owert, $Omkwr,
    $Dismm, $Minbe, $Disls, $Bstfe, $Eisbe, $Mstae, $Mstav, $Beskz, $Zzlzcod, $Zzlzcodsort, $Baugruppe, $Waers,
    $RawJson, $LastLoadedAtUtc);";

        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Richtung"] = GetText(row, "Richtung"),
                ["$Vknr"] = GetText(row, "Vknr"),
                ["$VknrMstae"] = GetText(row, "VknrMstae"),
                ["$VknrVerbrauch"] = GetText(row, "VknrVerbrauch"),
                ["$Kompnr"] = GetText(row, "Kompnr"),
                ["$KompnrMaktx"] = GetText(row, "KompnrMaktx"),
                ["$KompnrMeins"] = GetText(row, "KompnrMeins"),
                ["$Menge"] = GetText(row, "Menge"),
                ["$Exklusiv"] = IsTruthy(GetText(row, "Exklusiv")) ? 1 : 0,
                ["$Verbrauch"] = GetText(row, "Verbrauch"),
                ["$Labst"] = GetText(row, "Labst"),
                ["$FesteZugang"] = GetText(row, "FesteZugang"),
                ["$GeplZugang"] = GetText(row, "GeplZugang"),
                ["$FesteAbgang"] = GetText(row, "FesteAbgang"),
                ["$GeplAbgang"] = GetText(row, "GeplAbgang"),
                ["$Endbestand"] = GetText(row, "Endbestand"),
                ["$Omeng"] = GetText(row, "Omeng"),
                ["$Mkmng"] = GetText(row, "Mkmng"),
                ["$Stueckkosten"] = GetText(row, "Stueckkosten"),
                ["$WertFesteZug"] = GetText(row, "WertFesteZug"),
                ["$WertGeplZug"] = GetText(row, "WertGeplZug"),
                ["$WertFesteAbg"] = GetText(row, "WertFesteAbg"),
                ["$WertGeplAbg"] = GetText(row, "WertGeplAbg"),
                ["$WertEndbestand"] = GetText(row, "WertEndbestand"),
                ["$Owert"] = GetText(row, "Owert"),
                ["$Omkwr"] = GetText(row, "Omkwr"),
                ["$Dismm"] = GetText(row, "Dismm"),
                ["$Minbe"] = GetText(row, "Minbe"),
                ["$Disls"] = GetText(row, "Disls"),
                ["$Bstfe"] = GetText(row, "Bstfe"),
                ["$Eisbe"] = GetText(row, "Eisbe"),
                ["$Mstae"] = GetText(row, "Mstae"),
                ["$Mstav"] = GetText(row, "Mstav"),
                ["$Beskz"] = GetText(row, "Beskz"),
                ["$Zzlzcod"] = GetText(row, "Zzlzcod"),
                ["$Zzlzcodsort"] = GetText(row, "Zzlzcodsort"),
                ["$Baugruppe"] = IsTruthy(GetText(row, "Baugruppe")) ? 1 : 0,
                ["$Waers"] = GetText(row, "Waers"),
                ["$RawJson"] = JsonSerializer.Serialize(row),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    private static async Task UpsertParentAsync(SqliteConnection conn, SqliteTransaction transaction, IReadOnlyList<Dictionary<string, object?>> rows, string loadedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT OR REPLACE INTO MaterialParentCache (Kompnr, ElternMatnr, LastLoadedAtUtc)
VALUES ($Kompnr, $ElternMatnr, $LastLoadedAtUtc);";
        foreach (var row in rows)
            await ExecuteWithParametersAsync(conn, transaction, sql, new()
            {
                ["$Kompnr"] = GetText(row, "Kompnr"),
                ["$ElternMatnr"] = GetText(row, "ElternMatnr"),
                ["$LastLoadedAtUtc"] = loadedAtUtc
            }, cancellationToken);
    }

    private async Task WriteStatusAsync(string status, DateTime? startedAtUtc, DateTime? completedAtUtc, int usageRows, int parentRows, string message, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var conn = (SqliteConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);
        const string sql = @"
INSERT INTO MaterialUsageSyncState (Status, StartedAtUtc, CompletedAtUtc, UsageRows, ParentRows, Message)
VALUES ($Status, $StartedAtUtc, $CompletedAtUtc, $UsageRows, $ParentRows, $Message);";
        await ExecuteWithParametersAsync(conn, null, sql, new()
        {
            ["$Status"] = status,
            ["$StartedAtUtc"] = FormatDateTime(startedAtUtc),
            ["$CompletedAtUtc"] = FormatDateTime(completedAtUtc),
            ["$UsageRows"] = usageRows,
            ["$ParentRows"] = parentRows,
            ["$Message"] = message
        }, cancellationToken);
    }

    private static async Task<MaterialUsageRefreshStatus> ReadLatestStatusAsync(SqliteConnection conn, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.CommandText = @"
SELECT Status, StartedAtUtc, CompletedAtUtc, UsageRows, ParentRows, Message
FROM MaterialUsageSyncState
ORDER BY Id DESC
LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new MaterialUsageRefreshStatus { Status = "Empty", Message = "Noch kein Stuecklistenanalyse Full Load ausgefuehrt." };

        return new MaterialUsageRefreshStatus
        {
            Status = reader.GetString(0),
            StartedAtUtc = ParseDateTime(reader.GetString(1)),
            CompletedAtUtc = ParseDateTime(reader.GetString(2)),
            UsageRows = reader.GetInt32(3),
            ParentRows = reader.GetInt32(4),
            Message = reader.GetString(5)
        };
    }

    private static async Task ExecuteAsync(SqliteConnection conn, SqliteTransaction? transaction, string sql, Dictionary<string, object?>? parameters, CancellationToken cancellationToken)
    {
        await using var command = conn.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        if (parameters is not null)
            foreach (var (key, value) in parameters)
                command.Parameters.AddWithValue(key, value ?? DBNull.Value);
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

    private static bool IsTruthy(string value)
        => value.Equals("X", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
           value.Equals("1", StringComparison.OrdinalIgnoreCase);

    private static string TrimForLog(string value)
        => value.Length <= 1000 ? value : value[..1000] + "...";

    private static string FormatDateTime(DateTime? value)
        => value?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;

    private static DateTime? ParseDateTime(string value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;

    private sealed record MaterialUsageSapConnection(string BaseUrl, string Username, string Password);
}
