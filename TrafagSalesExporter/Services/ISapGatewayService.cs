namespace TrafagSalesExporter.Services;

public interface ISapGatewayService
{
    Task TestConnectionAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default);
    Task<List<string>> GetEntitySetsAsync(string serviceUrl, string username, string password, CancellationToken cancellationToken = default);
    Task<List<string>> GetEntityFieldNamesAsync(string serviceUrl, string entitySet, string username, string password, CancellationToken cancellationToken = default);
    Task<List<Dictionary<string, object?>>> GetEntityRowsAsync(string serviceUrl, string entitySet, string username, string password, CancellationToken cancellationToken = default);
}
