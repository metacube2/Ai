using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IConsolidatedExportService
{
    Task<string?> ExportAsync(List<SalesRecord> records);
}
