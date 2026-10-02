using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>Ein Verbindungsereignis eines Browsers, nur mit /24-Netzbereich, ohne Person und ohne volle IP.</summary>
public sealed record NetworkClientEvent(DateTime TimestampUtc, string Subnet, string EventType);

/// <summary>
/// Liest und schreibt die drei Netzwerk-Tabellen (Schema in DatabaseSchemaMaintenanceService
/// .EnsureNetworkTables). Schreibt nur eigene Daten des Reiters, nie Fremddaten.
/// </summary>
public sealed class NetworkStore
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public NetworkStore(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    internal const string ProbeTableSql = @"
CREATE TABLE IF NOT EXISTS NetworkProbeResults (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    TimestampUtc TEXT NOT NULL,
    TargetKey TEXT NOT NULL,
    Ok INTEGER NOT NULL,
    LatencyMs REAL NULL,
    Detail TEXT NOT NULL DEFAULT ''
);";

    internal const string WatchTableSql = @"
CREATE TABLE IF NOT EXISTS NetworkWatchTargets (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Label TEXT NOT NULL DEFAULT '',
    Host TEXT NOT NULL DEFAULT '',
    Port INTEGER NOT NULL DEFAULT 443,
    WorkCenter TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1
);";

    internal const string ClientTableSql = @"
CREATE TABLE IF NOT EXISTS NetworkClientEvents (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    TimestampUtc TEXT NOT NULL,
    Subnet TEXT NOT NULL DEFAULT '',
    EventType TEXT NOT NULL DEFAULT ''
);";

    internal static readonly string[] IndexSql =
    [
        "CREATE INDEX IF NOT EXISTS IX_NetworkProbeResults_Time ON NetworkProbeResults (TimestampUtc);",
        "CREATE INDEX IF NOT EXISTS IX_NetworkProbeResults_Target ON NetworkProbeResults (TargetKey, TimestampUtc);",
        "CREATE INDEX IF NOT EXISTS IX_NetworkClientEvents_Time ON NetworkClientEvents (TimestampUtc);"
    ];

    public async Task SaveProbeResultsAsync(IEnumerable<NetworkProbeResult> results, CancellationToken ct)
    {
        await WithConnectionAsync(async conn =>
        {
            await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
            foreach (var r in results)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO NetworkProbeResults (TimestampUtc, TargetKey, Ok, LatencyMs, Detail) VALUES ($t, $k, $o, $l, $d)";
                cmd.Parameters.AddWithValue("$t", r.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("$k", r.TargetKey);
                cmd.Parameters.AddWithValue("$o", r.Ok ? 1 : 0);
                cmd.Parameters.AddWithValue("$l", (object?)r.LatencyMs ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$d", r.Detail);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct);
        }, ct);
    }

    public async Task<List<NetworkProbeResult>> LoadProbeResultsAsync(DateTime sinceUtc, CancellationToken ct)
    {
        var list = new List<NetworkProbeResult>();
        await WithConnectionAsync(async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TimestampUtc, TargetKey, Ok, LatencyMs, Detail FROM NetworkProbeResults WHERE TimestampUtc >= $s ORDER BY TimestampUtc";
            cmd.Parameters.AddWithValue("$s", sinceUtc.ToString("O", CultureInfo.InvariantCulture));
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                list.Add(new NetworkProbeResult(
                    DateTime.Parse(r.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    r.GetString(1), r.GetInt32(2) == 1, r.IsDBNull(3) ? null : r.GetDouble(3), r.GetString(4)));
        }, ct);
        return list;
    }

    public async Task SaveClientEventAsync(NetworkClientEvent e, CancellationToken ct)
        => await WithConnectionAsync(async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO NetworkClientEvents (TimestampUtc, Subnet, EventType) VALUES ($t, $s, $e)";
            cmd.Parameters.AddWithValue("$t", e.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$s", e.Subnet);
            cmd.Parameters.AddWithValue("$e", e.EventType);
            await cmd.ExecuteNonQueryAsync(ct);
        }, ct);

    public async Task<List<NetworkClientEvent>> LoadClientEventsAsync(DateTime sinceUtc, CancellationToken ct)
    {
        var list = new List<NetworkClientEvent>();
        await WithConnectionAsync(async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TimestampUtc, Subnet, EventType FROM NetworkClientEvents WHERE TimestampUtc >= $s";
            cmd.Parameters.AddWithValue("$s", sinceUtc.ToString("O", CultureInfo.InvariantCulture));
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                list.Add(new NetworkClientEvent(DateTime.Parse(r.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), r.GetString(1), r.GetString(2)));
        }, ct);
        return list;
    }

    public async Task<List<NetworkWatchTarget>> LoadWatchTargetsAsync(CancellationToken ct)
    {
        var list = new List<NetworkWatchTarget>();
        await WithConnectionAsync(async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Label, Host, Port, WorkCenter, IsActive FROM NetworkWatchTargets ORDER BY Label";
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                list.Add(new NetworkWatchTarget { Id = r.GetInt32(0), Label = r.GetString(1), Host = r.GetString(2), Port = r.GetInt32(3), WorkCenter = r.GetString(4), IsActive = r.GetInt32(5) == 1 });
        }, ct);
        return list;
    }

    public async Task SaveWatchTargetAsync(NetworkWatchTarget t, CancellationToken ct)
        => await WithConnectionAsync(async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = t.Id == 0
                ? "INSERT INTO NetworkWatchTargets (Label, Host, Port, WorkCenter, IsActive) VALUES ($l, $h, $p, $w, $a)"
                : "UPDATE NetworkWatchTargets SET Label = $l, Host = $h, Port = $p, WorkCenter = $w, IsActive = $a WHERE Id = $id";
            cmd.Parameters.AddWithValue("$l", t.Label.Trim());
            cmd.Parameters.AddWithValue("$h", t.Host.Trim());
            cmd.Parameters.AddWithValue("$p", t.Port);
            cmd.Parameters.AddWithValue("$w", t.WorkCenter.Trim());
            cmd.Parameters.AddWithValue("$a", t.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", t.Id);
            await cmd.ExecuteNonQueryAsync(ct);
        }, ct);

    public async Task DeleteWatchTargetAsync(int id, CancellationToken ct)
        => await WithConnectionAsync(async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM NetworkWatchTargets WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            await cmd.ExecuteNonQueryAsync(ct);
        }, ct);

    /// <summary>Loescht Pruefergebnisse und Verbindungsereignisse aelter als die Aufbewahrung.</summary>
    public async Task CleanupAsync(int retentionDays, CancellationToken ct)
        => await WithConnectionAsync(async conn =>
        {
            var before = DateTime.UtcNow.AddDays(-Math.Max(1, retentionDays)).ToString("O", CultureInfo.InvariantCulture);
            foreach (var table in new[] { "NetworkProbeResults", "NetworkClientEvents" })
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"DELETE FROM {table} WHERE TimestampUtc < $b";
                cmd.Parameters.AddWithValue("$b", before);
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }, ct);

    /// <summary>Einfache Lesehilfe fuer andere Tabellen der App (ExportLogs, Sites ...), nur SELECT.</summary>
    public async Task<List<object?[]>> QueryAsync(string selectSql, IReadOnlyDictionary<string, object>? parameters, CancellationToken ct)
    {
        if (!selectSql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Nur SELECT erlaubt.");
        var rows = new List<object?[]>();
        await WithConnectionAsync(async conn =>
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = selectSql;
            if (parameters is not null)
                foreach (var (k, v) in parameters)
                    cmd.Parameters.AddWithValue(k, v);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var row = new object?[r.FieldCount];
                for (var i = 0; i < r.FieldCount; i++)
                    row[i] = r.IsDBNull(i) ? null : r.GetValue(i);
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
