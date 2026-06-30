namespace TrafagSalesExporter.Services;

public interface IPurchasingDataRefreshService
{
    Task<PurchasingDataRefreshStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<PurchasingDataRefreshStatus> RunFullLoadAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task<PurchasingDataRefreshStatus> RunDeltaAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default);
}

public sealed class PurchasingDataRefreshStatus
{
    public string Mode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? LastSuccessfulDeltaAtUtc { get; set; }
    public int EkkoRows { get; set; }
    public int EkpoRows { get; set; }
    public int EketRows { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsComplete => string.Equals(Status, "Success", StringComparison.OrdinalIgnoreCase);
}
