using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ICentralSalesRecordService
{
    Task ReplaceForSiteAsync(Site site, IEnumerable<SalesRecord> records);
    Task<List<SalesRecord>> GetAllAsync();
}
