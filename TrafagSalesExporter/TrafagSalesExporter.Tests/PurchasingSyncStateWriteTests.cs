using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Ein Einkauf-Lauf hinterlaesst genau eine Zeile in PurchasingSyncState (Befund 2026-09-28: vorher
/// zwei, und die Running-Zeile wurde beim naechsten Start als "Abgebrochen" markiert).
/// </summary>
public sealed class PurchasingSyncStateWriteTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly PurchasingDataRefreshService _service;

    public PurchasingSyncStateWriteTests()
    {
        _connection.Open();
        using (var command = _connection.CreateCommand())
        {
            command.CommandText = DatabaseSchemaSql.GetPurchasingSyncStateCreateSql();
            command.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _service = new PurchasingDataRefreshService(new Factory(options), null!, null!);
    }

    public void Dispose() => _connection.Dispose();

    private Task Write(string mode, string status, DateTime started, DateTime? completed, string message)
        => (Task)typeof(PurchasingDataRefreshService)
            .GetMethod("WriteStatusAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(_service, [mode, status, started, completed, null, null, completed, 1, 2, 3, message, CancellationToken.None])!;

    private List<(string Status, string Message)> Rows()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT Status, Message FROM PurchasingSyncState ORDER BY Id;";
        using var reader = command.ExecuteReader();
        var rows = new List<(string, string)>();
        while (reader.Read())
            rows.Add((reader.GetString(0), reader.GetString(1)));
        return rows;
    }

    [Fact]
    public async Task Erfolg_Schliesst_Die_Eigene_Running_Zeile_Ab()
    {
        var started = new DateTime(2026, 9, 28, 10, 32, 44, DateTimeKind.Utc);
        await Write("Delta", "Running", started, null, "Delta gestartet.");
        await Write("Delta", "Success", started, started.AddMinutes(47), "Delta abgeschlossen");

        var row = Assert.Single(Rows());
        Assert.Equal(("Success", "Delta abgeschlossen"), row);
    }

    [Fact]
    public async Task Ohne_Passende_Running_Zeile_Wird_Wie_Bisher_Neu_Geschrieben()
    {
        var started = new DateTime(2026, 9, 28, 10, 32, 44, DateTimeKind.Utc);
        await Write("Delta", "Running", started, null, "Delta gestartet.");
        // Anderer Start: gehoert nicht zu dieser Running-Zeile.
        await Write("Delta", "Error", started.AddMinutes(5), started.AddMinutes(6), "Fehler");

        Assert.Equal([("Running", "Delta gestartet."), ("Error", "Fehler")], Rows());
    }

    private sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
