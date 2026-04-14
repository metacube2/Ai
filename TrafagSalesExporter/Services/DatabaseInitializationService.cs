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
        EnsureSchema(db);
        SeedIfEmpty(db);
    }

    private static void EnsureSchema(AppDbContext db)
    {
        EnsureSitesTableSupportsOptionalHanaServer(db);
        AddColumnIfMissing(db, "HanaServers", "DatabaseName", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "HanaServers", "UseSsl", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "HanaServers", "ValidateCertificate", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "HanaServers", "AdditionalParams", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SourceSystem", "TEXT NOT NULL DEFAULT 'SAP'");
        AddColumnIfMissing(db, "Sites", "UsernameOverride", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "PasswordOverride", "TEXT NOT NULL DEFAULT ''");
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
        EnsureTransformationTable(db);
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
    UsernameOverride, PasswordOverride, SapServiceUrl, SapEntitySet,
    SapEntitySetsCache, SapEntitySetsRefreshedAtUtc, IsActive
)
SELECT
    Id, HanaServerId, Schema, TSC, Land,
    COALESCE(SourceSystem, 'SAP'),
    COALESCE(UsernameOverride, ''),
    COALESCE(PasswordOverride, ''),
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
            TimerEnabled = true
        });

        db.SaveChanges();
    }
}
