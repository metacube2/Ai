using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IHanaQueryService
{
    Task<List<SalesRecord>> GetSalesRecordsAsync(HanaServer server, string schema, string tsc, string land, string dateFilter, CancellationToken cancellationToken = default);
    Task<List<SalesRecord>> GetMappedSalesRecordsAsync(HanaServer server, string schema, Site site, IReadOnlyList<SapSourceDefinition> sources, IReadOnlyList<SapJoinDefinition> joins, IReadOnlyList<SapFieldMapping> mappings, string dateFilter, CancellationToken cancellationToken = default);
    Task<List<string>> GetAvailableSchemasAsync(HanaServer server, CancellationToken cancellationToken = default);
    Task<List<string>> GetAvailableTablesAsync(HanaServer server, string schema, CancellationToken cancellationToken = default);
    Task<List<string>> GetTableFieldNamesAsync(HanaServer server, string schema, string tableName, CancellationToken cancellationToken = default);
    Task<ConnectionTestResult> TestConnectionDetailedAsync(HanaServer server, CancellationToken cancellationToken = default);
    Task TestConnectionAsync(HanaServer server, CancellationToken cancellationToken = default);
}
