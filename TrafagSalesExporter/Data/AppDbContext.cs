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
    public DbSet<SapSourceDefinition> SapSourceDefinitions => Set<SapSourceDefinition>();
    public DbSet<SapJoinDefinition> SapJoinDefinitions => Set<SapJoinDefinition>();
    public DbSet<SapFieldMapping> SapFieldMappings => Set<SapFieldMapping>();
    public DbSet<CentralSalesRecord> CentralSalesRecords => Set<CentralSalesRecord>();
}
