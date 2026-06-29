using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IExcelExportService
{
    string CreateExcelFile(string outputDirectory, string tsc, DateTime fileDate, List<SalesRecord> records);
    string CreateConsolidatedExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records);
    string CreateDashboardProofExcelFile(string outputDirectory, DateTime fileDate, List<SalesRecord> records, bool useAuditCsvAsCentralSource, string? fileScope = null);
    string CreateGenericExcelFile(string outputDirectory, string filePrefix, DateTime fileDate, string worksheetName, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows);

    /// <summary>
    /// Builds a multi-sheet workbook entirely in memory and returns it as bytes for a
    /// browser download. Used by the per-tab "Export to Excel" buttons in the cockpit.
    /// </summary>
    byte[] CreateWorkbookBytes(IReadOnlyList<ExcelSheetData> sheets);
}

/// <summary>One worksheet for an in-memory export: a sheet name and its rows (column name -&gt; value).</summary>
public sealed record ExcelSheetData(string SheetName, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);
