namespace TrafagSalesExporter.Services;

public interface IMaterialUsageDataRefreshService
{
    Task<MaterialUsageRefreshStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <param name="materialFilter">
    /// Optional: Materialnummern, getrennt durch Komma, Semikolon, Leerzeichen, Tab oder
    /// Zeilenumbruch (eine Excel-Spalte kann also direkt eingefuegt werden), auch als Bereich
    /// "35-40"; werden als Vknr- (Top-Down) bzw. Kompnr-Filter (Bottom-Up) an SAP durchgereicht.
    /// Je Nummer wird eine EIGENE SAP-Anfrage gestellt, siehe
    /// <see cref="MaterialUsageDataRefreshService.BuildMaterialClauses"/>. Leer = Catch-all
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
    Task<List<MaterialUsagePreviewRow>> GetCachedUsageRowsAsync(string? materialFilter = null, int limit = 200, bool? topDown = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregiert den gesamten gefilterten Cache fuer die Logistik-Darstellung.
    /// Anders als die Rohdatentabelle ist diese Auswertung nicht auf 200 Zeilen begrenzt.
    /// </summary>
    Task<MaterialUsageAnalysisResult> GetCachedAnalysisAsync(bool topDown, string? materialFilter = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Eine Selektionsbedingung fuer genau EIN Eingabe-Token. <see cref="Token"/> ist die
/// Nutzereingabe (leer beim Catch-all) und wird nur fuer die Rueckmeldung "diese Nummern haben
/// keine Treffer" gebraucht; <see cref="Clause"/> ist die fertige OData-$filter-Teilbedingung.
/// </summary>
public sealed record MaterialSelectionClause(string Token, string Clause);

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

public sealed record MaterialUsageAnalysisGroup(
    string Key,
    string Description,
    int RelationCount,
    int CounterpartCount,
    int NegativeStockComponentCount,
    int ExclusiveComponentCount);

public sealed record MaterialUsageCodeDistribution(string Code, int ComponentCount);

public sealed record MaterialUsageAnalysisResult(
    bool IsTopDown,
    int RelationCount,
    int HeaderCount,
    int ComponentCount,
    int ExclusiveComponentCount,
    int ReusedComponentCount,
    int SingleUseComponentCount,
    int PositiveStockComponentCount,
    int ZeroStockComponentCount,
    int NegativeStockComponentCount,
    int MissingStockComponentCount,
    IReadOnlyList<MaterialUsageAnalysisGroup> Groups,
    IReadOnlyList<MaterialUsageCodeDistribution> LzCodes)
{
    public static MaterialUsageAnalysisResult Empty(bool topDown) =>
        new(topDown, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, [], []);
}

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
