using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public class DatabaseSchemaMaintenanceService : IDatabaseSchemaMaintenanceService
{
    public void EnsureSchema(AppDbContext db)
    {
        EnsureSitesTableSupportsOptionalHanaServer(db);
        EnsureExportSettingsTableSupportsCurrentSchema(db);
        EnsureHanaServersTableSupportsCurrentSchema(db);
        RepairBrokenForeignKeys(db);
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
    }

    private static void EnsureExportSettingsTableSupportsCurrentSchema(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        var columns = DatabaseSchemaTools.GetTableColumns(conn, transaction: null, "ExportSettings");
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

        DatabaseSchemaTools.RebuildTable(conn, "ExportSettings", DatabaseSchemaSql.GetExportSettingsCreateSql());
    }

    private static void EnsureHanaServersTableSupportsCurrentSchema(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        var columns = DatabaseSchemaTools.GetTableColumns(conn, transaction: null, "HanaServers");
        if (columns.Count == 0)
            return;

        if (!columns.Contains("Username") && !columns.Contains("Password"))
            return;

        DatabaseSchemaTools.RebuildTable(conn, "HanaServers", DatabaseSchemaSql.GetHanaServersCreateSql());
    }

    private static void EnsureSitesTableSupportsOptionalHanaServer(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
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
            create.CommandText = DatabaseSchemaSql.GetSitesCreateSql();
            create.ExecuteNonQuery();
        }

        using (var copy = conn.CreateCommand())
        {
            copy.Transaction = transaction;
            copy.CommandText = @"
INSERT INTO Sites (
    Id, HanaServerId, Schema, TSC, Land, SourceSystem,
    UsernameOverride, PasswordOverride, LocalExportFolderOverride, ManualImportFilePath,
    ManualImportLastUploadedAtUtc, SapServiceUrl, SapEntitySet, SapEntitySetsCache,
    SapEntitySetsRefreshedAtUtc, IsActive
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

    private static void RepairBrokenForeignKeys(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        var siteDependentTables = new[]
        {
            ("ExportLogs", DatabaseSchemaSql.GetExportLogsCreateSql()),
            ("AppEventLogs", DatabaseSchemaSql.GetAppEventLogsCreateSql()),
            ("CentralSalesRecords", DatabaseSchemaSql.GetCentralSalesRecordsCreateSql()),
            ("SapSourceDefinitions", DatabaseSchemaSql.GetSapSourceDefinitionsCreateSql()),
            ("SapJoinDefinitions", DatabaseSchemaSql.GetSapJoinDefinitionsCreateSql()),
            ("SapFieldMappings", DatabaseSchemaSql.GetSapFieldMappingsCreateSql())
        };

        foreach (var (tableName, createSql) in siteDependentTables)
        {
            if (DatabaseSchemaTools.TableReferences(conn, tableName, "Sites_old"))
                DatabaseSchemaTools.RebuildTable(conn, tableName, createSql);
        }

        if (DatabaseSchemaTools.TableReferences(conn, "Sites", "HanaServers_repair_old"))
            DatabaseSchemaTools.RebuildTable(conn, "Sites", DatabaseSchemaSql.GetSitesCreateSql());
    }

    private static void AddColumnIfMissing(AppDbContext db, string table, string column, string type)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
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
        if (conn.State != System.Data.ConnectionState.Open)
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
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = DatabaseSchemaSql.GetSapSourceDefinitionsCreateSql().Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
        cmd.ExecuteNonQuery();
    }

    private static void EnsureCurrencyExchangeRateTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
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
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = DatabaseSchemaSql.GetSapJoinDefinitionsCreateSql().Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
        cmd.ExecuteNonQuery();
    }

    private static void EnsureSapFieldMappingTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = DatabaseSchemaSql.GetSapFieldMappingsCreateSql().Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
        cmd.ExecuteNonQuery();
    }

    private static void EnsureCentralSalesRecordTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = DatabaseSchemaSql.GetCentralSalesRecordsCreateSql().Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
        cmd.ExecuteNonQuery();
    }

    private static void EnsureAppEventLogTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = DatabaseSchemaSql.GetAppEventLogsCreateSql().Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
        cmd.ExecuteNonQuery();
    }

    private static void EnsureSourceSystemDefinitionTable(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
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
}

internal static class DatabaseSchemaTools
{
    internal static bool TableReferences(System.Data.Common.DbConnection connection, string tableName, string referencedTableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $tableName;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var sql = command.ExecuteScalar()?.ToString() ?? string.Empty;
        return sql.Contains(referencedTableName, StringComparison.OrdinalIgnoreCase);
    }

    internal static void RebuildTable(System.Data.Common.DbConnection connection, string tableName, string createSql)
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

    internal static List<string> GetSharedColumns(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction? transaction, string newTableName, string oldTableName)
    {
        var newColumns = GetTableColumns(connection, transaction, newTableName);
        var oldColumns = GetTableColumns(connection, transaction, oldTableName);

        return newColumns.Where(oldColumns.Contains).ToList();
    }

    internal static HashSet<string> GetTableColumns(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction? transaction, string tableName)
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
}
