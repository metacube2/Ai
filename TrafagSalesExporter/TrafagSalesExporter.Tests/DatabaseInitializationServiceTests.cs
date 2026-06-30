using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
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

        var service = CreateService();
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

        var service = CreateService();
        await service.InitializeAsync();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var site = await db.Sites.SingleAsync();
        Assert.Null(await Record.ExceptionAsync(() => db.SaveChangesAsync()));
        Assert.Equal("schema_a", site.Schema);

        var tableSql = await ReadTableSqlAsync("Sites");
        Assert.Contains("REFERENCES HanaServers (Id)", tableSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HanaServers_repair_old", tableSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InitializeAsync_Seeds_Default_SourceSystems_And_Central_HanaServers()
    {
        var service = CreateService();

        await service.InitializeAsync();

        await using var db = await _dbFactory.CreateDbContextAsync();

        Assert.Contains(db.SourceSystemDefinitions, x => x.Code == "SAP" && x.ConnectionKind == SourceSystemConnectionKinds.SapGateway);
        Assert.Contains(db.SourceSystemDefinitions, x => x.Code == "BI1" && x.ConnectionKind == SourceSystemConnectionKinds.Hana);
        Assert.Contains(db.SourceSystemDefinitions, x => x.Code == "SAGE" && x.ConnectionKind == SourceSystemConnectionKinds.Hana);
        Assert.Contains(db.SourceSystemDefinitions, x => x.Code == "MANUAL_EXCEL" && x.ConnectionKind == SourceSystemConnectionKinds.ManualExcel);

        Assert.Contains(db.HanaServers, x => x.SourceSystem == "BI1");
        Assert.Contains(db.HanaServers, x => x.SourceSystem == "SAGE");
        var germany = Assert.Single(db.Sites, x => x.TSC == "TRDE" && x.Land == "Deutschland");
        Assert.Equal("MANUAL_EXCEL", germany.SourceSystem);
        Assert.False(germany.IsActive);
        Assert.Contains(db.ManualExcelColumnMappings, x =>
            x.SiteId == germany.Id &&
            x.TargetField == nameof(SalesRecord.SalesPriceValue) &&
            x.SourceHeader == "NettoPreisGesamtX" &&
            x.IsRequired);
        Assert.Contains(db.ManualExcelColumnMappings, x =>
            x.SiteId == germany.Id &&
            x.TargetField == nameof(SalesRecord.DocumentType) &&
            x.SourceHeader == "=Alphaplan Excel");
        Assert.Equal(2, db.FieldTransformationRules.Count(x => x.SourceSystem == "MANUAL_EXCEL"));

        var purchasing = Assert.Single(db.Sites, x => x.TSC == PurchasingDataSourcePageService.PurchasingTsc);
        Assert.Equal("SAP", purchasing.SourceSystem);
        Assert.Contains(db.SapSourceDefinitions, x => x.SiteId == purchasing.Id && x.Alias == "EKKO" && x.EntitySet == "EKKOSet" && x.IsPrimary);
        Assert.Contains(db.SapSourceDefinitions, x => x.SiteId == purchasing.Id && x.Alias == "EKPO" && x.EntitySet == "EKPOSet");
        Assert.Contains(db.SapSourceDefinitions, x => x.SiteId == purchasing.Id && x.Alias == "EKET" && x.EntitySet == "eketSet");
        Assert.Contains(db.SapJoinDefinitions, x => x.SiteId == purchasing.Id && x.LeftAlias == "EKKO" && x.RightAlias == "EKPO");
        Assert.Contains(db.SapFieldMappings, x => x.SiteId == purchasing.Id && x.TargetField == "NetValueChf" && x.SourceExpression == "EKPO.NetwrChf");

        var dach = Assert.Single(db.Sites, x => x.TSC == "ZSCHWEIZ");
        Assert.Equal("SAP", dach.SourceSystem);
        Assert.Contains(db.SapSourceDefinitions, x => x.SiteId == dach.Id && x.Alias == "P" && x.EntitySet == "ProductDivisionRefSet" && x.IsActive && !x.IsPrimary);
        Assert.Contains(db.SapSourceDefinitions, x => x.SiteId == dach.Id && x.Alias == "M" && x.EntitySet == "ProductDivisionMapSet" && !x.IsActive && !x.IsPrimary);
        Assert.Contains(db.SapJoinDefinitions, x => x.SiteId == dach.Id && x.LeftAlias == "Z" && x.RightAlias == "P" && x.LeftKeys == "Matnr" && x.RightKeys == "Matnr" && x.IsActive);
        Assert.Contains(db.SapJoinDefinitions, x => x.SiteId == dach.Id && x.LeftAlias == "Z" && x.RightAlias == "M" && x.LeftKeys == "Prodh" && x.RightKeys == "Paph1" && !x.IsActive);
        Assert.Contains(db.SapFieldMappings, x => x.SiteId == dach.Id && x.TargetField == nameof(SalesRecord.ProductHierarchyCode) && x.SourceExpression == "P.Paph1" && x.IsActive);
        Assert.Contains(db.SapFieldMappings, x => x.SiteId == dach.Id && x.TargetField == nameof(SalesRecord.ProductFamilyCode) && x.SourceExpression == "P.Wwpfa" && x.IsActive);
        Assert.Contains(db.SapFieldMappings, x => x.SiteId == dach.Id && x.TargetField == nameof(SalesRecord.ProductDivisionCode) && x.SourceExpression == "P.Wwpsp" && x.IsActive);
        Assert.Contains(db.SapFieldMappings, x => x.SiteId == dach.Id && x.TargetField == nameof(SalesRecord.ProductMappingAssigned) && x.SourceExpression == "P.IsAssigned" && x.IsActive);
        Assert.DoesNotContain(db.SapFieldMappings, x => x.SiteId == dach.Id && x.SourceExpression.Contains("FirstNonEmpty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task InitializeAsync_Repairs_India_Sage_Hana_Mapping()
    {
        await PrepareIndiaSourceSystemDriftAsync();

        var service = CreateService();
        await service.InitializeAsync();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var india = Assert.Single(db.Sites.Include(x => x.HanaServer), x => x.TSC == "TRIN");
        var sageServer = db.HanaServers
            .OrderBy(x => x.Id)
            .First(x => x.SourceSystem == "SAGE");

        Assert.Equal("SAGE", india.SourceSystem);
        Assert.Equal("TRAFAG_LIVE", india.Schema);
        Assert.Equal("india-user", india.UsernameOverride);
        Assert.Equal("india-password", india.PasswordOverride);
        Assert.Equal("20.197.20.60", sageServer.Host);
        Assert.Equal(30015, sageServer.Port);
        Assert.Equal(sageServer.Id, india.HanaServerId);
    }

    [Fact]
    public async Task InitializeAsync_Repairs_Spain_Manual_Import_File_To_Folder()
    {
        await PrepareSpainManualImportFilePathAsync();

        var service = CreateService();
        await service.InitializeAsync();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var spain = Assert.Single(db.Sites, x => x.TSC == "TRSE");

        Assert.Equal("MANUAL_EXCEL", spain.SourceSystem);
        Assert.Equal(
            "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/Spanien",
            spain.ManualImportFilePath);
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

    private async Task PrepareIndiaSourceSystemDriftAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        db.HanaServers.RemoveRange(db.HanaServers);
        db.Sites.RemoveRange(db.Sites);
        await db.SaveChangesAsync();

        var bi1Server = new HanaServer
        {
            SourceSystem = "BI1",
            Name = "Internal",
            Host = "travtrp0",
            Port = 30015,
            Username = string.Empty,
            Password = string.Empty
        };
        var indiaServer = new HanaServer
        {
            SourceSystem = string.Empty,
            Name = "India",
            Host = "20.197.20.60",
            Port = 30015,
            Username = string.Empty,
            Password = string.Empty
        };
        var emptySageServer = new HanaServer
        {
            SourceSystem = "SAGE",
            Name = "SAGE",
            Host = string.Empty,
            Port = 30015,
            Username = string.Empty,
            Password = string.Empty
        };

        db.HanaServers.AddRange(bi1Server, indiaServer, emptySageServer);
        await db.SaveChangesAsync();

        db.Sites.Add(new Site
        {
            HanaServerId = indiaServer.Id,
            Schema = "TRAFAG_LIVE",
            TSC = "TRIN",
            Land = "Indien",
            SourceSystem = "BI1",
            UsernameOverride = "india-user",
            PasswordOverride = "india-password",
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private async Task PrepareSpainManualImportFilePathAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        db.HanaServers.RemoveRange(db.HanaServers);
        db.Sites.RemoveRange(db.Sites);
        await db.SaveChangesAsync();

        db.Sites.AddRange(
            new Site
            {
                Schema = "fr01_p",
                TSC = "TRFR",
                Land = "Frankreich",
                SourceSystem = "BI1",
                IsActive = true
            },
            new Site
            {
                Schema = "Spanien",
                TSC = "TRSE",
                Land = "Spanien",
                SourceSystem = "MANUAL_EXCEL",
                ManualImportFilePath = "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform/Import/Finance/Spanien/Spain_Sales_2025.csv",
                IsActive = true
            });
        await db.SaveChangesAsync();
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

    private DatabaseInitializationService CreateService()
        => new(_dbFactory, new DatabaseSchemaMaintenanceService(), new DatabaseSeedService());

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
