namespace TrafagSalesExporter.Services;

public interface IConsolidatedExportService
{
    Task<string?> ExportAsync(Action<string>? updateStatus = null);
}
