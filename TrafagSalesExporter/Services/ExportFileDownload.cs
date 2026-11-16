namespace TrafagSalesExporter.Services;

/// <summary>
/// Download von Exportdateien in den Browser (Export-Seite, seit 2026-11-16 statt Process.Start auf dem Server).
/// </summary>
public static class ExportFileDownload
{
    /// <summary>Inhaltstyp nach Dateiendung, damit der Browser die Datei mit Excel bzw. dem Texteditor verbindet.</summary>
    public static string ContentType(string filePath)
        => Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".csv" => "text/csv",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
}
