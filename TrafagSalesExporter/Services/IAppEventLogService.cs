namespace TrafagSalesExporter.Services;

public interface IAppEventLogService
{
    Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null);
    Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null);
}
