using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ISupplyChainAnalysisService
{
    Task<SupplyChainAnalysisResult> LoadAsync(
        SupplyChainAnalysisKind kind,
        SupplyChainAnalysisFilter? filter = null,
        CancellationToken cancellationToken = default);
}
