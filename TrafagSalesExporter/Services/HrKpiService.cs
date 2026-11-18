using Microsoft.Extensions.Options;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IHrKpiService
{
    Task<HrKpiResult> BuildAsync(HrKpiOptions options);
}

public sealed class HrKpiService : IHrKpiService
{
    private readonly HrKpiDataSourceOptions _dataSources;

    public HrKpiService(IOptions<HrKpiDataSourceOptions>? dataSources = null)
    {
        _dataSources = (dataSources?.Value ?? new HrKpiDataSourceOptions()).Normalize();
    }

    public Task<HrKpiResult> BuildAsync(HrKpiOptions options)
        => Task.FromResult(new HrKpiDashboardBuilder(_dataSources).Build(options));
}
