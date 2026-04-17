using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public sealed class SiteExportResult
{
    public required List<SalesRecord> Records { get; init; }
    public required ExportLog Log { get; init; }
    public string? FilePath { get; init; }
}
