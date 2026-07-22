namespace TrafagSalesExporter.Services;

public interface IMaterialUsageDataRefreshService
{
    Task<MaterialUsageRefreshStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <param name="materialFilter">
    /// Optional: kommagetrennte Materialnummern, auch als Bereich "35-40"; werden als Vknr-
    /// (Top-Down) bzw. Kompnr-Filter (Bottom-Up) an SAP durchgereicht. Leer = Catch-all
    /// ("Vknr gt ''"), weil die SAP-Seite einen Materialfilter erzwingt (Guard gegen
    /// versehentliche Vollselektion).
    /// </param>
    /// <param name="includeDeleted">
    /// Analog Report-Checkbox p_lvorm: bezieht auch loeschvorgemerkte Kopf-/Filtermaterialien
    /// (MARA-LVORM gesetzt) mit ein. Ohne diese Option liefert Top-Down fuer solche Materialien
    /// 0 Zeilen, obwohl die Verwendung in ZPOWERBI_VC_TXT noch vorhanden ist (befund 2026-07-22:
    /// alte, numerische Vknr wie "2217" sind nur ueber Bottom-Up auffindbar). Wird ohne DDIC-
    /// Aenderung ueber den Richtung-Wert transportiert ("TOPDOWNALLE"/"BOTTOMUPALLE"), siehe
    /// docs/abap/README_LZCODE_WEBSERVICE.md.
    /// </param>
    Task<MaterialUsageRefreshStatus> RunFullLoadAsync(bool topDown = true, string? materialFilter = null, bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liest gecachte Zeilen aus MaterialUsageCache fuer die Anzeige auf der
    /// Stuecklistenanalyse-Seite. materialFilter matcht per LIKE auf Vknr/Kompnr.
    /// </summary>
    Task<List<MaterialUsagePreviewRow>> GetCachedUsageRowsAsync(string? materialFilter = null, int limit = 200, CancellationToken cancellationToken = default);
}

public sealed record MaterialUsagePreviewRow(
    string Richtung,
    string Vknr,
    string Kompnr,
    string KompnrMaktx,
    string KompnrMeins,
    string Menge,
    bool Exklusiv,
    string Labst,
    string Endbestand,
    string Stueckkosten,
    string WertEndbestand,
    string Mstae,
    string Zzlzcod);

public sealed class MaterialUsageRefreshStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int UsageRows { get; set; }
    public int ParentRows { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsComplete => string.Equals(Status, "Success", StringComparison.OrdinalIgnoreCase);
}
