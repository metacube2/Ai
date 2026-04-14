using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IExcelExportService
{
    string CreateExcelFile(string outputDirectory, string tsc, DateTime fileDate, List<SalesRecord> records);
    string CreateConsolidatedExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records);
    string CreateGenericExcelFile(string outputDirectory, string filePrefix, DateTime fileDate, string worksheetName, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows);
}
