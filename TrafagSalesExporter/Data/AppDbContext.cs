using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<HanaServer> HanaServers => Set<HanaServer>();
    public DbSet<SourceSystemDefinition> SourceSystemDefinitions => Set<SourceSystemDefinition>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SharePointConfig> SharePointConfigs => Set<SharePointConfig>();
    public DbSet<ExportSettings> ExportSettings => Set<ExportSettings>();
    public DbSet<ExportLog> ExportLogs => Set<ExportLog>();
    public DbSet<AppEventLog> AppEventLogs => Set<AppEventLog>();
    public DbSet<FieldTransformationRule> FieldTransformationRules => Set<FieldTransformationRule>();
    public DbSet<CurrencyExchangeRate> CurrencyExchangeRates => Set<CurrencyExchangeRate>();
    public DbSet<FinanceReference> FinanceReferences => Set<FinanceReference>();
    public DbSet<FinanceIntercompanyRule> FinanceIntercompanyRules => Set<FinanceIntercompanyRule>();
    public DbSet<FinanceRule> FinanceRules => Set<FinanceRule>();
    public DbSet<SapSourceDefinition> SapSourceDefinitions => Set<SapSourceDefinition>();
    public DbSet<SapJoinDefinition> SapJoinDefinitions => Set<SapJoinDefinition>();
    public DbSet<SapFieldMapping> SapFieldMappings => Set<SapFieldMapping>();
    public DbSet<ManualExcelColumnMapping> ManualExcelColumnMappings => Set<ManualExcelColumnMapping>();
    public DbSet<CentralSalesRecord> CentralSalesRecords => Set<CentralSalesRecord>();
    public DbSet<NavigationMenuItem> NavigationMenuItems => Set<NavigationMenuItem>();
}
