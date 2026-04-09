using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<HanaServer> HanaServers => Set<HanaServer>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SharePointConfig> SharePointConfigs => Set<SharePointConfig>();
    public DbSet<ExportSettings> ExportSettings => Set<ExportSettings>();
    public DbSet<ExportLog> ExportLogs => Set<ExportLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HanaServer>().HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<Site>()
            .HasOne(x => x.HanaServer)
            .WithMany(x => x.Sites)
            .HasForeignKey(x => x.HanaServerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExportLog>()
            .HasOne(x => x.Site)
            .WithMany()
            .HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public static class DbInitializer
{
    public static async Task SeedDefaultsAsync(AppDbContext db, CryptoService cryptoService)
    {
        if (!await db.HanaServers.AnyAsync())
        {
            db.HanaServers.AddRange(
                new HanaServer
                {
                    Name = "Internal",
                    Host = "travtrp0",
                    Port = 30015,
                    Username = string.Empty,
                    EncryptedPassword = cryptoService.Encrypt(string.Empty)
                },
                new HanaServer
                {
                    Name = "India",
                    Host = "20.197.20.60",
                    Port = 30015,
                    Username = string.Empty,
                    EncryptedPassword = cryptoService.Encrypt(string.Empty)
                });

            await db.SaveChangesAsync();
        }

        if (!await db.Sites.AnyAsync())
        {
            var internalServer = await db.HanaServers.SingleAsync(x => x.Name == "Internal");
            var indiaServer = await db.HanaServers.SingleAsync(x => x.Name == "India");

            db.Sites.AddRange(
                new Site { HanaServerId = internalServer.Id, Schema = "fr01_p", TSC = "TRFR", Land = "Frankreich", IsActive = true },
                new Site { HanaServerId = internalServer.Id, Schema = "it01_p", TSC = "TRIT", Land = "Italien", IsActive = true },
                new Site { HanaServerId = internalServer.Id, Schema = "us01_p", TSC = "TRUS", Land = "USA", IsActive = true },
                new Site { HanaServerId = indiaServer.Id, Schema = "TRAFAG_LIVE", TSC = "TRIN", Land = "Indien", IsActive = true });

            await db.SaveChangesAsync();
        }

        if (!await db.SharePointConfigs.AnyAsync())
        {
            db.SharePointConfigs.Add(new SharePointConfig
            {
                SiteUrl = "https://trafagag.sharepoint.com/sites/WorldwideBIPlatform",
                ExportFolder = "/Shared Documents/Exports/",
                TenantId = string.Empty,
                ClientId = string.Empty,
                EncryptedClientSecret = cryptoService.Encrypt(string.Empty)
            });
            await db.SaveChangesAsync();
        }

        if (!await db.ExportSettings.AnyAsync())
        {
            db.ExportSettings.Add(new ExportSettings
            {
                DateFilter = "2025-01-01",
                TimerHour = 3,
                TimerMinute = 0,
                TimerEnabled = true
            });
            await db.SaveChangesAsync();
        }
    }
}
