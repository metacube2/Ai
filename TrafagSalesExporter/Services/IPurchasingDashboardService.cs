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
    public string Message { get; set; } = string.Empty;
}
