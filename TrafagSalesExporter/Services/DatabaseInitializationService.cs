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
        EnsureRecommendedTransformationRules(db);
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
        EnsureExportSettingsTableSupportsCurrentSchema(db);
        EnsureHanaServersTableSupportsCurrentSchema(db);
        RepairBrokenSiteForeignKeys(db);
        AddColumnIfMissing(db, "HanaServers", "SourceSystem", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "HanaServers", "DatabaseName", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "HanaServers", "UseSsl", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "HanaServers", "ValidateCertificate", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "HanaServers", "AdditionalParams", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SourceSystem", "TEXT NOT NULL DEFAULT 'SAP'");
        AddColumnIfMissing(db, "Sites", "UsernameOverride", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "PasswordOverride", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "LocalExportFolderOverride", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "ManualImportFilePath", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "ManualImportLastUploadedAtUtc", "TEXT NULL");
        AddColumnIfMissing(db, "Sites", "SapServiceUrl", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SapEntitySet", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SapEntitySetsCache", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SapEntitySetsRefreshedAtUtc", "TEXT NULL");
        AddColumnIfMissing(db, "ExportSettings", "DebugLoggingEnabled", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "ExportSettings", "LocalSiteExportFolder", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportSettings", "LocalConsolidatedExportFolder", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "SharePointConfigs", "CentralExportFolder", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "ExportLogs", "FilePath", "TEXT NOT NULL DEFAULT ''");
        EnsureTransformationTable(db);
        AddColumnIfMissing(db, "FieldTransformationRules", "RuleScope", "TEXT NOT NULL DEFAULT 'Value'");
        EnsureCurrencyExchangeRateTable(db);
        EnsureSourceSystemDefinitionTable(db);
        AddColumnIfMissing(db, "SourceSystemDefinitions", "CentralServiceUrl", "TEXT NOT NULL DEFAULT ''");
        EnsureSapSourceTable(db);
        EnsureSapJoinTable(db);
        EnsureSapFieldMappingTable(db);
        EnsureCentralSalesRecordTable(db);
        EnsureAppEventLogTable(db);
        EnsureSourceSystemDefinitions(db);
        EnsureCentralHanaServerRecords(db);
    }

    private static void EnsureExportSettingsTableSupportsCurrentSchema(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var columns = GetTableColumns(conn, transaction: null, "ExportSettings");
        if (columns.Count == 0)
            return;

        var legacyColumns = new[]
        {
            "SapUsername",
            "SapPassword",
            "Bi1Username",
            "Bi1Password",
            "SageUsername",
            "SagePassword"
        };

        if (!legacyColumns.Any(columns.Contains))
            return;

        RebuildTable(conn, "ExportSettings", GetExportSettingsCreateSql());
    }

    private static void EnsureHanaServersTableSupportsCurrentSchema(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var columns = GetTableColumns(conn, transaction: null, "HanaServers");
        if (columns.Count == 0)
            return;

        if (!columns.Contains("Username") && !columns.Contains("Password"))
            return;

        RebuildTable(conn, "HanaServers", GetHanaServersCreateSql());
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
    ManualImportFilePath TEXT NOT NULL DEFAULT '',
    ManualImportLastUploadedAtUtc TEXT NULL,
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
    ManualImportFilePath, ManualImportLastUploadedAtUtc, SapEntitySetsCache, SapEntitySetsRefreshedAtUtc, IsActive
)
SELECT
    Id, HanaServerId, Schema, TSC, Land,
    COALESCE(SourceSystem, 'SAP'),
    COALESCE(UsernameOverride, ''),
    COALESCE(PasswordOverride, ''),
    COALESCE(LocalExportFolderOverride, ''),
    COALESCE(ManualImportFilePath, ''),
    ManualImportLastUploadedAtUtc,
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

    private static List<string> GetSharedColumns(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction? transaction, string newTableName, string oldTableName)
    {
        var newColumns = GetTableColumns(connection, transaction, newTableName);
        var oldColumns = GetTableColumns(connection, transaction, oldTableName);

        return newColumns.Where(oldColumns.Contains).ToList();
    }

    private static HashSet<string> GetTableColumns(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction? transaction, string tableName)
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

    private static string GetExportSettingsCreateSql() => @"
CREATE TABLE ExportSettings (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    DateFilter TEXT NOT NULL,
    TimerHour INTEGER NOT NULL,
    TimerMinute INTEGER NOT NULL,
    TimerEnabled INTEGER NOT NULL,
    DebugLoggingEnabled INTEGER NOT NULL DEFAULT 0,
    LocalSiteExportFolder TEXT NOT NULL DEFAULT '',
    LocalConsolidatedExportFolder TEXT NOT NULL DEFAULT ''
);";

    private static string GetHanaServersCreateSql() => @"
CREATE TABLE HanaServers (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SourceSystem TEXT NOT NULL,
    Name TEXT NOT NULL,
    Host TEXT NOT NULL,
    Port INTEGER NOT NULL,
    DatabaseName TEXT NOT NULL DEFAULT '',
    UseSsl INTEGER NOT NULL DEFAULT 0,
    ValidateCertificate INTEGER NOT NULL DEFAULT 0,
    AdditionalParams TEXT NOT NULL DEFAULT ''
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
    RuleScope TEXT NOT NULL DEFAULT 'Value',
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

    private static void EnsureCurrencyExchangeRateTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS CurrencyExchangeRates (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    FromCurrency TEXT NOT NULL,
    ToCurrency TEXT NOT NULL,
    Rate REAL NOT NULL,
    ValidFrom TEXT NOT NULL,
    ValidTo TEXT NULL,
    Notes TEXT NOT NULL DEFAULT '',
    IsActive INTEGER NOT NULL DEFAULT 1
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

    private static void EnsureSourceSystemDefinitionTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS SourceSystemDefinitions (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL,
    DisplayName TEXT NOT NULL,
    ConnectionKind TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CentralServiceUrl TEXT NOT NULL DEFAULT '',
    CentralUsername TEXT NOT NULL DEFAULT '',
    CentralPassword TEXT NOT NULL DEFAULT ''
);";
        cmd.ExecuteNonQuery();
    }

    private static void SeedIfEmpty(AppDbContext db)
    {
        if (db.Sites.Any() || db.HanaServers.Any() || db.SharePointConfigs.Any() || db.ExportSettings.Any())
            return;

        var serverBi1 = new HanaServer { SourceSystem = "BI1", Name = "BI1", Host = "travtrp0", Port = 30015, Username = "", Password = "" };
        var serverSage = new HanaServer { SourceSystem = "SAGE", Name = "SAGE", Host = "20.197.20.60", Port = 30015, Username = "", Password = "" };
        db.HanaServers.AddRange(serverBi1, serverSage);
        db.SaveChanges();

        db.Sites.AddRange(
            new Site { HanaServerId = serverBi1.Id, Schema = "fr01_p", TSC = "TRFR", Land = "Frankreich", SourceSystem = "BI1", IsActive = true },
            new Site { HanaServerId = serverBi1.Id, Schema = "it01_p", TSC = "TRIT", Land = "Italien", SourceSystem = "BI1", IsActive = true },
            new Site { HanaServerId = serverBi1.Id, Schema = "us01_p", TSC = "TRUS", Land = "USA", SourceSystem = "BI1", IsActive = true },
            new Site { HanaServerId = serverSage.Id, Schema = "TRAFAG_LIVE", TSC = "TRIN", Land = "Indien", SourceSystem = "SAGE", IsActive = true }
        );

        db.SharePointConfigs.Add(new SharePointConfig
        {
            SiteUrl = "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform",
            ExportFolder = "/Shared Documents/Exports/",
            CentralExportFolder = "",
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

    private static void EnsureRecommendedTransformationRules(AppDbContext db)
    {
        var recommendedRules = new[]
        {
            new FieldTransformationRule
            {
                SourceSystem = "MANUAL_EXCEL",
                SourceField = nameof(SalesRecord.SalesCurrency),
                TargetField = nameof(SalesRecord.SalesCurrency),
                TransformationType = "Replace",
                RuleScope = "Value",
                Argument = "$=>USD",
                SortOrder = 100,
                IsActive = true
            },
            new FieldTransformationRule
            {
                SourceSystem = "MANUAL_EXCEL",
                SourceField = nameof(SalesRecord.StandardCostCurrency),
                TargetField = nameof(SalesRecord.StandardCostCurrency),
                TransformationType = "Replace",
                RuleScope = "Value",
                Argument = "$=>USD",
                SortOrder = 110,
                IsActive = true
            }
        };

        var hasChanges = false;

        foreach (var rule in recommendedRules)
        {
            var exists = db.FieldTransformationRules.Any(existing =>
                existing.SourceSystem == rule.SourceSystem &&
                existing.RuleScope == rule.RuleScope &&
                existing.SourceField == rule.SourceField &&
                existing.TargetField == rule.TargetField &&
                existing.TransformationType == rule.TransformationType &&
                existing.Argument == rule.Argument);

            if (exists)
                continue;

            db.FieldTransformationRules.Add(rule);
            hasChanges = true;
        }

        if (hasChanges)
            db.SaveChanges();
    }

    private static void EnsureCentralHanaServerRecords(AppDbContext db)
    {
        var centralSystems = db.SourceSystemDefinitions
            .AsNoTracking()
            .Where(x => x.ConnectionKind == SourceSystemConnectionKinds.Hana)
            .OrderBy(x => x.Code)
            .Select(x => x.Code)
            .ToList();
        var changed = false;

        foreach (var sourceSystem in centralSystems)
        {
            var existingCentral = db.HanaServers
                .OrderBy(x => x.Id)
                .FirstOrDefault(x => x.SourceSystem == sourceSystem);

            if (existingCentral is not null)
            {
                if (string.IsNullOrWhiteSpace(existingCentral.Name))
                {
                    existingCentral.Name = sourceSystem;
                    changed = true;
                }

                continue;
            }

            var linkedServer = db.Sites
                .Include(x => x.HanaServer)
                .Where(x => x.SourceSystem == sourceSystem && x.HanaServerId != null && x.HanaServer != null)
                .Select(x => x.HanaServer!)
                .OrderBy(x => x.Id)
                .FirstOrDefault();

            if (linkedServer is not null)
            {
                linkedServer.SourceSystem = sourceSystem;
                if (string.IsNullOrWhiteSpace(linkedServer.Name))
                    linkedServer.Name = sourceSystem;
                changed = true;
                continue;
            }

            db.HanaServers.Add(new HanaServer
            {
                SourceSystem = sourceSystem,
                Name = sourceSystem,
                Host = string.Empty,
                Port = 30015,
                Username = string.Empty,
                Password = string.Empty,
                DatabaseName = string.Empty,
                AdditionalParams = string.Empty
            });
            changed = true;
        }

        if (changed)
            db.SaveChanges();
    }

    private static void EnsureSourceSystemDefinitions(AppDbContext db)
    {
        var defaults = new[]
        {
            new SourceSystemDefinition { Code = "SAP", DisplayName = "SAP", ConnectionKind = SourceSystemConnectionKinds.SapGateway, IsActive = true },
            new SourceSystemDefinition { Code = "BI1", DisplayName = "BI1", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
            new SourceSystemDefinition { Code = "SAGE", DisplayName = "SAGE", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
            new SourceSystemDefinition { Code = "MANUAL_EXCEL", DisplayName = "Manual Excel", ConnectionKind = SourceSystemConnectionKinds.ManualExcel, IsActive = true }
        };

        var existing = db.SourceSystemDefinitions.ToList();
        var changed = false;

        foreach (var item in defaults)
        {
            var current = existing.FirstOrDefault(x => x.Code == item.Code);
            if (current is null)
            {
                db.SourceSystemDefinitions.Add(item);
                existing.Add(item);
                changed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(current.DisplayName))
            {
                current.DisplayName = item.DisplayName;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(current.ConnectionKind))
            {
                current.ConnectionKind = item.ConnectionKind;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(current.CentralServiceUrl) &&
                string.Equals(current.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase))
            {
                var sapSite = db.Sites
                    .Where(x => x.SourceSystem == current.Code && !string.IsNullOrWhiteSpace(x.SapServiceUrl))
                    .OrderBy(x => x.Id)
                    .FirstOrDefault();

                if (sapSite is not null)
                {
                    current.CentralServiceUrl = sapSite.SapServiceUrl;
                    changed = true;
                }
            }
        }

        if (changed)
            db.SaveChanges();
    }
}
