using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public interface IDatabaseSchemaMaintenanceService
{
    void EnsureSchema(AppDbContext db);
}
