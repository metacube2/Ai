using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IExportLogService
{
    Task WriteAsync(ExportLog log);
}
