using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IManualExcelImportService
{
    Task<List<SalesRecord>> ReadSalesRecordsAsync(string filePath, Site site);
}
