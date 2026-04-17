using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.DataSources;

public sealed class DataSourceFetchResult
{
    public required List<SalesRecord> Records { get; init; }

    /// <summary>
    /// Wenn gesetzt, liefert der Adapter bereits eine Referenz-Datei (z. B. manueller Excel-Import).
    /// SiteExportService erzeugt dann keine neue Excel-Datei.
    /// </summary>
    public string? ReferenceFilePath { get; init; }
}
