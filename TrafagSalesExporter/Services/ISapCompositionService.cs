using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ISapCompositionService
{
    Task<List<SalesRecord>> BuildSalesRecordsAsync(
        Site site,
        IReadOnlyList<SapSourceDefinition> sources,
        IReadOnlyList<SapJoinDefinition> joins,
        IReadOnlyList<SapFieldMapping> mappings,
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
