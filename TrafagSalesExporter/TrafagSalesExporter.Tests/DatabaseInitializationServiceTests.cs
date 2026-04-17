using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class DatabaseInitializationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;

    public DatabaseInitializationServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.Database.EnsureCreated();
        }

        _dbFactory = new TestDbContextFactory(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task InitializeAsync_Migrates_Sites_Without_Shifting_Columns()
    {
        await PrepareLegacySitesTableAsync();

        var service = new DatabaseInitializationService(_dbFactory);
        await service.InitializeAsync();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var site = await db.Sites.SingleAsync();

        Assert.Equal("override-user", site.UsernameOverride);
        Assert.Equal("override-password", site.PasswordOverride);
        Assert.Equal("C:\\exports\\ch", site.LocalExportFolderOverride);
        Assert.Equal("C:\\imports\\manual.xlsx", site.ManualImportFilePath);
        Assert.Equal("https://sap.example.local/service", site.SapServiceUrl);
        Assert.Equal("A_Sales", site.SapEntitySet);
        Assert.Equal("[\"A_Sales\",\"A_Orders\"]", site.SapEntitySetsCache);
        Assert.Equal(new DateTime(2026, 4, 17, 7, 30, 0, DateTimeKind.Utc), site.ManualImportLastUploadedAtUtc?.ToUniversalTime());
        Assert.Equal(new DateTime(2026, 4, 17, 8, 0, 0, DateTimeKind.Utc), site.SapEntitySetsRefreshedAtUtc?.ToUniversalTime());
    }

    [Fact]
    public async Task InitializeAsync_Repairs_Sites_ForeignKey_To_HanaServersRepairOld()
    {
        await PrepareBrokenHanaServerForeignKeyAsync();

        var service = new DatabaseInitializationService(_dbFactory);
        await service.InitializeAsync();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var site = await db.Sites.SingleAsync();
        Assert.Null(await Record.ExceptionAsync(() => db.SaveChangesAsync()));
        Assert.Equal("schema_a", site.Schema);

        var tableSql = await ReadTableSqlAsync("Sites");
        Assert.Contains("REFERENCES HanaServers (Id)", tableSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HanaServers_repair_old", tableSql, StringComparison.OrdinalIgnoreCase);
    }

    private async Task PrepareLegacySitesTableAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM Sites;");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM HanaServers;");
        await db.Database.ExecuteSqlRawAsync("""
INSERT INTO HanaServers (Id, SourceSystem, Name, Host, Port, DatabaseName, UseSsl, ValidateCertificate, AdditionalParams)
VALUES (1, 'SAP', 'SAP', 'hana-host', 30015, 'DB1', 0, 0, '');
""");

        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sites RENAME TO Sites_current;");
        await db.Database.ExecuteSqlRawAsync("""
CREATE TABLE Sites (
    Id INTEGER NOT NULL CONSTRAINT PK_Sites PRIMARY KEY AUTOINCREMENT,
    HanaServerId INTEGER NOT NULL,
    Schema TEXT NOT NULL,
    TSC TEXT NOT NULL,
    Land TEXT NOT NULL,
    SourceSystem TEXT NULL,
    UsernameOverride TEXT NULL,
    PasswordOverride TEXT NULL,
    LocalExportFolderOverride TEXT NULL,
    ManualImportFilePath TEXT NULL,
    ManualImportLastUploadedAtUtc TEXT NULL,
    SapServiceUrl TEXT NULL,
    SapEntitySet TEXT NULL,
    SapEntitySetsCache TEXT NULL,
    SapEntitySetsRefreshedAtUtc TEXT NULL,
    IsActive INTEGER NOT NULL,
    CONSTRAINT FK_Sites_HanaServers_HanaServerId FOREIGN KEY (HanaServerId) REFERENCES HanaServers (Id)
);
""");
        await db.Database.ExecuteSqlRawAsync("""
INSERT INTO Sites (
    Id, HanaServerId, Schema, TSC, Land, SourceSystem,
    UsernameOverride, PasswordOverride, LocalExportFolderOverride, ManualImportFilePath,
    ManualImportLastUploadedAtUtc, SapServiceUrl, SapEntitySet, SapEntitySetsCache,
    SapEntitySetsRefreshedAtUtc, IsActive
)
VALUES (
    1, 1, 'schema_a', 'TRCH', 'Schweiz', 'SAP',
    'override-user', 'override-password', 'C:\exports\ch', 'C:\imports\manual.xlsx',
    '2026-04-17 07:30:00Z', 'https://sap.example.local/service', 'A_Sales', '["A_Sales","A_Orders"]',
    '2026-04-17 08:00:00Z', 1
);
""");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE Sites_current;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
    }

    private async Task PrepareBrokenHanaServerForeignKeyAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM Sites;");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM HanaServers;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Sites RENAME TO Sites_current;");
        await db.Database.ExecuteSqlRawAsync("""
CREATE TABLE Sites (
    Id INTEGER NOT NULL CONSTRAINT PK_Sites PRIMARY KEY AUTOINCREMENT,
    HanaServerId INTEGER NULL,
    Schema TEXT NOT NULL,
    TSC TEXT NOT NULL,
    Land TEXT NOT NULL,
    SourceSystem TEXT NOT NULL DEFAULT 'SAP',
    UsernameOverride TEXT NOT NULL DEFAULT '',
    PasswordOverride TEXT NOT NULL DEFAULT '',
    LocalExportFolderOverride TEXT NOT NULL DEFAULT '',
    ManualImportFilePath TEXT NOT NULL DEFAULT '',
    ManualImportLastUploadedAtUtc TEXT NULL,
    SapServiceUrl TEXT NOT NULL DEFAULT '',
    SapEntitySet TEXT NOT NULL DEFAULT '',
    SapEntitySetsCache TEXT NOT NULL DEFAULT '',
    SapEntitySetsRefreshedAtUtc TEXT NULL,
    IsActive INTEGER NOT NULL,
    CONSTRAINT FK_Sites_HanaServers_HanaServerId FOREIGN KEY (HanaServerId) REFERENCES HanaServers_repair_old (Id)
);
""");
        await db.Database.ExecuteSqlRawAsync("""
INSERT INTO Sites (
    Id, HanaServerId, Schema, TSC, Land, SourceSystem,
    UsernameOverride, PasswordOverride, LocalExportFolderOverride, ManualImportFilePath,
    ManualImportLastUploadedAtUtc, SapServiceUrl, SapEntitySet, SapEntitySetsCache,
    SapEntitySetsRefreshedAtUtc, IsActive
)
VALUES (
    1, NULL, 'schema_a', 'TRUK', 'England', 'MANUAL_EXCEL',
    '', '', '', '',
    NULL, '', '', '',
    NULL, 1
);
""");
        await db.Database.ExecuteSqlRawAsync("DROP TABLE Sites_current;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
    }

    private async Task<string> ReadTableSqlAsync(string tableName)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $tableName;";
        command.Parameters.AddWithValue("$tableName", tableName);
        return (await command.ExecuteScalarAsync())?.ToString() ?? string.Empty;
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }
}
