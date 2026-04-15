using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ICentralSalesRecordService
{
    Task ReplaceForSiteAsync(Site site, IEnumerable<SalesRecord> records, Action<string>? updateStatus = null);
    Task<List<SalesRecord>> GetAllAsync();
}
