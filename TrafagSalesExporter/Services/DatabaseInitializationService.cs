using System.Data;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public partial class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDatabaseSchemaMaintenanceService _schemaMaintenanceService;
    private readonly IDatabaseSeedService _seedService;

    public DatabaseInitializationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IDatabaseSchemaMaintenanceService schemaMaintenanceService,
        IDatabaseSeedService seedService)
    {
        _dbFactory = dbFactory;
        _schemaMaintenanceService = schemaMaintenanceService;
        _seedService = seedService;
    }

    public async Task InitializeAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        ConfigureSqlite(db);
        _schemaMaintenanceService.EnsureSchema(db);
        _seedService.SeedDefaults(db);
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
}
