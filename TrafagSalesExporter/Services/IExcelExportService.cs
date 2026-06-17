using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IExcelExportService
{
    string CreateExcelFile(string outputDirectory, string tsc, DateTime fileDate, List<SalesRecord> records);
    string CreateConsolidatedExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records);
    string CreateDashboardProofExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records, bool useAuditCsvAsCentralSource, string? fileScope = null);
    string CreateGenericExcelFile(string outputDirectory, string filePrefix, DateTime fileDate, string worksheetName, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows);
}
