using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.DataSources;

public sealed class DataSourceFetchContext
{
    public required Site Site { get; init; }
    public required SourceSystemDefinition SourceDefinition { get; init; }
    public required ExportSettings Settings { get; init; }
    public SharePointConfig? SharePointConfig { get; init; }
    public Action<string>? UpdateStatus { get; init; }
}
