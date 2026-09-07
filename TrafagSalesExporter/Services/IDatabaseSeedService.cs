using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public interface IDatabaseSeedService
{
    void SeedDefaults(AppDbContext db);
}
