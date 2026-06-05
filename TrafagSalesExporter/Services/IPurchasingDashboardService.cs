namespace TrafagSalesExporter.Services;

public interface IPurchasingDashboardService
{
    Task<PurchasingDashboardLiveState> LoadAsync(CancellationToken cancellationToken = default);
}

public sealed class PurchasingDashboardLiveState
{
    public bool SapReachable { get; set; }
    public bool EkkoLoaded { get; set; }
    public bool EkpoLoaded { get; set; }
    public bool EketLoaded { get; set; }
    public int PurchaseOrderCount { get; set; }
    public int SupplierCount { get; set; }
    public DateTime? LatestOrderDate { get; set; }
    public int PositionSampleCount { get; set; }
    public int ScheduleSampleCount { get; set; }
    public decimal SpendChfSample { get; set; }
    public decimal OpenQuantitySample { get; set; }
    public decimal OpenValueSample { get; set; }
    public decimal ContractValueSample { get; set; }
    public string TopSupplierLabel { get; set; } = string.Empty;
    public string TopMaterialGroupLabel { get; set; } = string.Empty;
    public string TopArticleLabel { get; set; } = string.Empty;
    public List<PurchasingLiveChartPoint> SpendChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> OpenValueChartRows { get; set; } = [];
    public List<PurchasingLiveChartPoint> ContractChartRows { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}

public sealed record PurchasingLiveChartPoint(string Label, decimal Value);
