using System.Data;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<HanaServer> HanaServers => Set<HanaServer>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SharePointConfig> SharePointConfigs => Set<SharePointConfig>();
    public DbSet<ExportSettings> ExportSettings => Set<ExportSettings>();
    public DbSet<ExportLog> ExportLogs => Set<ExportLog>();
    public DbSet<FieldTransformationRule> FieldTransformationRules => Set<FieldTransformationRule>();

    /// <summary>
    /// Fügt Spalten zu existierenden Tabellen hinzu, die bei neueren Versionen
    /// hinzugekommen sind. EnsureCreated aktualisiert das Schema nicht automatisch.
    /// </summary>
    public static void EnsureSchema(AppDbContext db)
    {
        AddColumnIfMissing(db, "HanaServers", "DatabaseName", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "HanaServers", "UseSsl", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "HanaServers", "ValidateCertificate", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(db, "HanaServers", "AdditionalParams", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "Sites", "SourceSystem", "TEXT NOT NULL DEFAULT 'SAP'");
        EnsureTransformationTable(db);
    }

    private static void AddColumnIfMissing(AppDbContext db, string table, string column, string type)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) conn.Open();

        bool exists = false;
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
        if (conn.State != ConnectionState.Open) conn.Open();

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

    public static void SeedIfEmpty(AppDbContext db)
    {
        if (db.HanaServers.Any()) return;

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
