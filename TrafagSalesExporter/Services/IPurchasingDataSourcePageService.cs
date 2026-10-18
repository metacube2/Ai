using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IPurchasingDataSourcePageService
{
    Task<PurchasingDataSourceState> LoadAsync();
    Task<PurchasingDataSourceState> SaveAsync(PurchasingDataSourceState state);
    Task<PurchasingDataSourceState> ResetDefaultsAsync();
    Task<PageActionResult> TestConnectionAsync(PurchasingDataSourceState state);
}

public sealed class PurchasingDataSourceState
{
    public Site Site { get; set; } = new();
    public SourceSystemDefinition? SourceSystem { get; set; }
    public List<SapSourceDefinition> Sources { get; set; } = [];
    public List<SapJoinDefinition> Joins { get; set; } = [];
    public List<SapFieldMapping> Mappings { get; set; } = [];
}
