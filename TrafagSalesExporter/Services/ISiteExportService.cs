using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ISiteExportService
{
    Task<SiteExportResult> ExportAsync(Site site, Action<string>? updateStatus = null);
}
