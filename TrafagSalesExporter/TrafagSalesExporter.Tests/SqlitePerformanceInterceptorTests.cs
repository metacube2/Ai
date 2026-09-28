using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Der groessere Seitencache muss auch auf Verbindungen ankommen, die ein Dienst selbst oeffnet
/// (GetDbConnection().OpenAsync()), denn so arbeiten Einkauf, Lagerwert und Timer (2026-09-28).
/// </summary>
public sealed class SqlitePerformanceInterceptorTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"sqlite-perf-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Fact]
    public async Task Selbst_Geoeffnete_Verbindung_Bekommt_Den_Grossen_Cache()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_path}")
            .AddInterceptors(new SqlitePerformanceInterceptor())
            .Options;

        await using var db = new AppDbContext(options);
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA cache_size;";
        var cacheSize = Convert.ToInt64(await command.ExecuteScalarAsync());
        command.CommandText = "PRAGMA temp_store;";
        var tempStore = Convert.ToInt64(await command.ExecuteScalarAsync());

        Assert.Equal(-SqlitePerformanceInterceptor.CacheSizeKiB, cacheSize);
        Assert.Equal(2, tempStore); // 2 = MEMORY
    }
}
