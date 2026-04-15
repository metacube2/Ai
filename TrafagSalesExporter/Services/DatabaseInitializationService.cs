using System.Data;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DatabaseInitializationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task InitializeAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        ConfigureSqlite(db);
        EnsureSchema(db);
        SeedIfEmpty(db);
    }

    private static void ConfigureSqlite(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using (var wal = conn.CreateCommand())
        {
            wal.CommandText = "PRAGMA journal_mode=WAL;";
            wal.ExecuteNonQuery();
        }

        using (var timeout = conn.CreateCommand())
        {
            timeout.CommandText = "PRAGMA busy_timeout=10000;";
            timeout.ExecuteNonQuery();
        }
    }

    private static void EnsureSchema(AppDbContext db)
    {
        EnsureSitesTableSupportsOptionalHanaServer(db);
        RepairBrokenSiteForeignKeys(db);
        AddColumnIfMissing(db, "HanaServers", "DatabaseName", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "HanaServers", "UseSsl", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "HanaServers", "ValidateCertificate", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "HanaServers", "AdditionalParams", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SourceSystem", "TEXT NOT NULL DEFAULT 'SAP'");
        AddColumnIfMissing(db, "Sites", "UsernameOverride", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "PasswordOverride", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "LocalExportFolderOverride", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SapServiceUrl", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SapEntitySet", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SapEntitySetsCache", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SapEntitySetsRefreshedAtUtc", "TEXT NULL");
        AddColumnIfMissing(db, "ExportSettings", "SapUsername", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportSettings", "SapPassword", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportSettings", "Bi1Username", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportSettings", "Bi1Password", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportSettings", "SageUsername", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportSettings", "SagePassword", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportSettings", "DebugLoggingEnabled", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "ExportSettings", "LocalSiteExportFolder", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportSettings", "LocalConsolidatedExportFolder", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportLogs", "FilePath", "TEXT NOT NULL DEFAULT ''");
        EnsureTransformationTable(db);
        EnsureSapSourceTable(db);
        EnsureSapJoinTable(db);
        EnsureSapFieldMappingTable(db);
        EnsureCentralSalesRecordTable(db);
        EnsureAppEventLogTable(db);
    }

    private static void EnsureSitesTableSupportsOptionalHanaServer(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var hanaServerIdIsRequired = false;
        {
            using var pragma = conn.CreateCommand();
            pragma.CommandText = "PRAGMA table_info(Sites)";
            using var reader = pragma.ExecuteReader();

            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), "HanaServerId", StringComparison.OrdinalIgnoreCase))
                {
                    hanaServerIdIsRequired = Convert.ToInt32(reader["notnull"]) == 1;
                    break;
                }
            }
        }

        if (!hanaServerIdIsRequired)
            return;

        using var disableFk = conn.CreateCommand();
        disableFk.CommandText = "PRAGMA foreign_keys = OFF;";
        disableFk.ExecuteNonQuery();

        using var transaction = conn.BeginTransaction();

        using (var rename = conn.CreateCommand())
        {
            rename.Transaction = transaction;
            rename.CommandText = "ALTER TABLE Sites RENAME TO Sites_old;";
            rename.ExecuteNonQuery();
        }

        using (var create = conn.CreateCommand())
        {
            create.Transaction = transaction;
            create.CommandText = @"
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
    SapServiceUrl TEXT NOT NULL DEFAULT '',
    SapEntitySet TEXT NOT NULL DEFAULT '',
    SapEntitySetsCache TEXT NOT NULL DEFAULT '',
    SapEntitySetsRefreshedAtUtc TEXT NULL,
    IsActive INTEGER NOT NULL,
    CONSTRAINT FK_Sites_HanaServers_HanaServerId FOREIGN KEY (HanaServerId) REFERENCES HanaServers (Id)
);";
            create.ExecuteNonQuery();
        }

        using (var copy = conn.CreateCommand())
        {
            copy.Transaction = transaction;
            copy.CommandText = @"
INSERT INTO Sites (
    Id, HanaServerId, Schema, TSC, Land, SourceSystem,
    UsernameOverride, PasswordOverride, LocalExportFolderOverride, SapServiceUrl, SapEntitySet,
    SapEntitySetsCache, SapEntitySetsRefreshedAtUtc, IsActive
)
SELECT
    Id, HanaServerId, Schema, TSC, Land,
    COALESCE(SourceSystem, 'SAP'),
    COALESCE(UsernameOverride, ''),
    COALESCE(PasswordOverride, ''),
    COALESCE(LocalExportFolderOverride, ''),
    COALESCE(SapServiceUrl, ''),
    COALESCE(SapEntitySet, ''),
    COALESCE(SapEntitySetsCache, ''),
    SapEntitySetsRefreshedAtUtc,
    IsActive
FROM Sites_old;";
            copy.ExecuteNonQuery();
        }

        using (var drop = conn.CreateCommand())
        {
            drop.Transaction = transaction;
            drop.CommandText = "DROP TABLE Sites_old;";
            drop.ExecuteNonQuery();
        }

        transaction.Commit();

        using var enableFk = conn.CreateCommand();
        enableFk.CommandText = "PRAGMA foreign_keys = ON;";
        enableFk.ExecuteNonQuery();
    }

    private static void RepairBrokenSiteForeignKeys(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var tablesToRepair = new[]
        {
            ("ExportLogs", GetExportLogsCreateSql()),
            ("AppEventLogs", GetAppEventLogsCreateSql()),
            ("CentralSalesRecords", GetCentralSalesRecordsCreateSql()),
            ("SapSourceDefinitions", GetSapSourceDefinitionsCreateSql()),
            ("SapJoinDefinitions", GetSapJoinDefinitionsCreateSql()),
            ("SapFieldMappings", GetSapFieldMappingsCreateSql())
        };

        foreach (var (tableName, createSql) in tablesToRepair)
        {
            if (TableReferencesSitesOld(conn, tableName))
                RebuildTable(conn, tableName, createSql);
        }
    }

    private static bool TableReferencesSitesOld(System.Data.Common.DbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $tableName;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var sql = command.ExecuteScalar()?.ToString() ?? string.Empty;
        return sql.Contains("Sites_old", StringComparison.OrdinalIgnoreCase);
    }

    private static void RebuildTable(System.Data.Common.DbConnection connection, string tableName, string createSql)
    {
        using var disableFk = connection.CreateCommand();
        disableFk.CommandText = "PRAGMA foreign_keys = OFF;";
        disableFk.ExecuteNonQuery();

        using var transaction = connection.BeginTransaction();

        var tempTableName = $"{tableName}_repair_old";

        using (var rename = connection.CreateCommand())
        {
            rename.Transaction = transaction;
            rename.CommandText = $"ALTER TABLE {tableName} RENAME TO {tempTableName};";
            rename.ExecuteNonQuery();
        }

        using (var create = connection.CreateCommand())
        {
            create.Transaction = transaction;
            create.CommandText = createSql;
            create.ExecuteNonQuery();
        }

        var columns = GetSharedColumns(connection, transaction, tableName, tempTableName);
        if (columns.Count > 0)
        {
            var columnList = string.Join(", ", columns);

            using var copy = connection.CreateCommand();
            copy.Transaction = transaction;
            copy.CommandText = $"INSERT INTO {tableName} ({columnList}) SELECT {columnList} FROM {tempTableName};";
            copy.ExecuteNonQuery();
        }

        using (var drop = connection.CreateCommand())
        {
            drop.Transaction = transaction;
            drop.CommandText = $"DROP TABLE {tempTableName};";
            drop.ExecuteNonQuery();
        }

        transaction.Commit();

        using var enableFk = connection.CreateCommand();
        enableFk.CommandText = "PRAGMA foreign_keys = ON;";
        enableFk.ExecuteNonQuery();
    }

    private static List<string> GetSharedColumns(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction transaction, string newTableName, string oldTableName)
    {
        var newColumns = GetTableColumns(connection, transaction, newTableName);
        var oldColumns = GetTableColumns(connection, transaction, oldTableName);

        return newColumns.Where(oldColumns.Contains).ToList();
    }

    private static HashSet<string> GetTableColumns(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction transaction, string tableName)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA table_info({tableName})";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var name = reader["name"]?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                columns.Add(name);
        }

        return columns;
    }

    private static string GetExportLogsCreateSql() => @"
CREATE TABLE ExportLogs (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Timestamp TEXT NOT NULL,
    SiteId INTEGER NOT NULL,
    Land TEXT NOT NULL,
    TSC TEXT NOT NULL,
    Status TEXT NOT NULL,
    RowCount INTEGER NOT NULL,
    ErrorMessage TEXT NULL,
    FileName TEXT NOT NULL DEFAULT '',
    FilePath TEXT NOT NULL DEFAULT '',
    DurationSeconds REAL NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    private static string GetAppEventLogsCreateSql() => @"
CREATE TABLE AppEventLogs (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Timestamp TEXT NOT NULL,
    Level TEXT NOT NULL,
    Category TEXT NOT NULL,
    SiteId INTEGER NULL,
    Land TEXT NOT NULL,
    Message TEXT NOT NULL,
    Details TEXT NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    private static string GetCentralSalesRecordsCreateSql() => @"
CREATE TABLE CentralSalesRecords (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    StoredAtUtc TEXT NOT NULL,
    SiteId INTEGER NOT NULL,
    SourceSystem TEXT NOT NULL,
    ExtractionDate TEXT NOT NULL,
    Tsc TEXT NOT NULL,
    InvoiceNumber TEXT NOT NULL,
    PositionOnInvoice INTEGER NOT NULL,
    Material TEXT NOT NULL,
    Name TEXT NOT NULL,
    ProductGroup TEXT NOT NULL,
    Quantity TEXT NOT NULL,
    SupplierNumber TEXT NOT NULL,
    SupplierName TEXT NOT NULL,
    SupplierCountry TEXT NOT NULL,
    CustomerNumber TEXT NOT NULL,
    CustomerName TEXT NOT NULL,
    CustomerCountry TEXT NOT NULL,
    CustomerIndustry TEXT NOT NULL,
    StandardCost TEXT NOT NULL,
    StandardCostCurrency TEXT NOT NULL,
    PurchaseOrderNumber TEXT NOT NULL,
    SalesPriceValue TEXT NOT NULL,
    SalesCurrency TEXT NOT NULL,
    Incoterms2020 TEXT NOT NULL,
    SalesResponsibleEmployee TEXT NOT NULL,
    InvoiceDate TEXT NULL,
    OrderDate TEXT NULL,
    Land TEXT NOT NULL,
    DocumentType TEXT NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    private static string GetSapSourceDefinitionsCreateSql() => @"
CREATE TABLE SapSourceDefinitions (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    Alias TEXT NOT NULL,
    EntitySet TEXT NOT NULL,
    IsPrimary INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    private static string GetSapJoinDefinitionsCreateSql() => @"
CREATE TABLE SapJoinDefinitions (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    LeftAlias TEXT NOT NULL,
    RightAlias TEXT NOT NULL,
    LeftKeys TEXT NOT NULL,
    RightKeys TEXT NOT NULL,
    JoinType TEXT NOT NULL DEFAULT 'Left',
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    private static string GetSapFieldMappingsCreateSql() => @"
CREATE TABLE SapFieldMappings (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    TargetField TEXT NOT NULL,
    SourceExpression TEXT NOT NULL,
    IsRequired INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";

    private static void AddColumnIfMissing(AppDbContext db, string table, string column, string type)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var exists = false;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"PRAGMA table_info({table})";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), column, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (!exists)
        {
            using var alter = conn.CreateCommand();
            alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}";
            alter.ExecuteNonQuery();
        }
    }

    private static void EnsureTransformationTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS FieldTransformationRules (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SourceSystem TEXT NOT NULL DEFAULT 'SAP',
    SourceField TEXT NOT NULL,
    TargetField TEXT NOT NULL,
    TransformationType TEXT NOT NULL,
    Argument TEXT NOT NULL DEFAULT '',
    SortOrder INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1
);";
        cmd.ExecuteNonQuery();
    }

    private static void EnsureSapSourceTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS SapSourceDefinitions (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    Alias TEXT NOT NULL,
    EntitySet TEXT NOT NULL,
    IsPrimary INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";
        cmd.ExecuteNonQuery();
    }

    private static void EnsureSapJoinTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS SapJoinDefinitions (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    LeftAlias TEXT NOT NULL,
    RightAlias TEXT NOT NULL,
    LeftKeys TEXT NOT NULL,
    RightKeys TEXT NOT NULL,
    JoinType TEXT NOT NULL DEFAULT 'Left',
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";
        cmd.ExecuteNonQuery();
    }

    private static void EnsureSapFieldMappingTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS SapFieldMappings (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    TargetField TEXT NOT NULL,
    SourceExpression TEXT NOT NULL,
    IsRequired INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";
        cmd.ExecuteNonQuery();
    }

    private static void EnsureCentralSalesRecordTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS CentralSalesRecords (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    StoredAtUtc TEXT NOT NULL,
    SiteId INTEGER NOT NULL,
    SourceSystem TEXT NOT NULL,
    ExtractionDate TEXT NOT NULL,
    Tsc TEXT NOT NULL,
    InvoiceNumber TEXT NOT NULL,
    PositionOnInvoice INTEGER NOT NULL,
    Material TEXT NOT NULL,
    Name TEXT NOT NULL,
    ProductGroup TEXT NOT NULL,
    Quantity TEXT NOT NULL,
    SupplierNumber TEXT NOT NULL,
    SupplierName TEXT NOT NULL,
    SupplierCountry TEXT NOT NULL,
    CustomerNumber TEXT NOT NULL,
    CustomerName TEXT NOT NULL,
    CustomerCountry TEXT NOT NULL,
    CustomerIndustry TEXT NOT NULL,
    StandardCost TEXT NOT NULL,
    StandardCostCurrency TEXT NOT NULL,
    PurchaseOrderNumber TEXT NOT NULL,
    SalesPriceValue TEXT NOT NULL,
    SalesCurrency TEXT NOT NULL,
    Incoterms2020 TEXT NOT NULL,
    SalesResponsibleEmployee TEXT NOT NULL,
    InvoiceDate TEXT NULL,
    OrderDate TEXT NULL,
    Land TEXT NOT NULL,
    DocumentType TEXT NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";
        cmd.ExecuteNonQuery();
    }

    private static void EnsureAppEventLogTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS AppEventLogs (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Timestamp TEXT NOT NULL,
    Level TEXT NOT NULL,
    Category TEXT NOT NULL,
    SiteId INTEGER NULL,
    Land TEXT NOT NULL,
    Message TEXT NOT NULL,
    Details TEXT NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites (Id)
);";
        cmd.ExecuteNonQuery();
    }

    private static void SeedIfEmpty(AppDbContext db)
    {
        if (db.HanaServers.Any())
            return;

        var serverInternal = new HanaServer { Name = "Internal", Host = "travtrp0", Port = 30015, Username = "", Password = "" };
        var serverIndia = new HanaServer { Name = "India", Host = "20.197.20.60", Port = 30015, Username = "", Password = "" };
        db.HanaServers.AddRange(serverInternal, serverIndia);
        db.SaveChanges();

        db.Sites.AddRange(
            new Site { HanaServerId = serverInternal.Id, Schema = "fr01_p", TSC = "TRFR", Land = "Frankreich", IsActive = true },
            new Site { HanaServerId = serverInternal.Id, Schema = "it01_p", TSC = "TRIT", Land = "Italien", IsActive = true },
            new Site { HanaServerId = serverInternal.Id, Schema = "us01_p", TSC = "TRUS", Land = "USA", IsActive = true },
            new Site { HanaServerId = serverIndia.Id, Schema = "TRAFAG_LIVE", TSC = "TRIN", Land = "Indien", IsActive = true }
        );

        db.SharePointConfigs.Add(new SharePointConfig
        {
            SiteUrl = "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform",
            ExportFolder = "/Shared Documents/Exports/",
            TenantId = "",
            ClientId = "",
            ClientSecret = ""
        });

        db.ExportSettings.Add(new ExportSettings
        {
            DateFilter = "2025-01-01",
            TimerHour = 3,
            TimerMinute = 0,
            TimerEnabled = true,
            DebugLoggingEnabled = false,
            LocalSiteExportFolder = "",
            LocalConsolidatedExportFolder = ""
        });

        db.SaveChanges();
    }
}
