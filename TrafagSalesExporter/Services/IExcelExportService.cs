using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IExcelExportService
{
    string CreateExcelFile(string outputDirectory, string tsc, DateTime fileDate, List<SalesRecord> records);
    string CreateConsolidatedExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records);
}
