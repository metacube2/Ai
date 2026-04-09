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
