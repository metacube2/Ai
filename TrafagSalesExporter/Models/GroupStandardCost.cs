namespace TrafagSalesExporter.Models;

/// <summary>
/// Konzern-Standardkosten je Material und Bewertungskreis der liefernden Trafag-
/// Gesellschaft (aktuell nur TR AG / MBEW-STPRS, Bewertungskreis 1100), unabhaengig
/// davon, welche Gesellschaft die Umsatzzeile verkauft hat. Wird beim CH/AT-SAP-Import
/// zusaetzlich zur bestehenden CH/AT-Kostenanreicherung befuellt (siehe
/// SapGatewayDataSourceAdapter), damit Gruppenmarge-Zeilen anderer TSC mit TR AG als
/// internem Lieferanten die echte Konzern-Herstellkostenbasis nutzen koennen
/// (Mappe1.xlsx-Spezifikation, siehe docs/FINANCE_GRUPPENMARGE_2026-06-16.md).
///
/// TR IN/TR IT sind bewusst noch nicht enthalten: laut Live-Stichprobe 2026-07-15 hat
/// TR IT in SAP B1 keinen befuellten Standardkosten-/Durchschnittspreis-Wert je Material
/// (OITM/OITW.AvgPrice und OITM.PrdStdCst sind bei aktiv gefuehrten Materialien 0); TR IN
/// war vom Netzwerk aus nicht pruefbar. Ohne verifizierte Quelle wird hier bewusst nichts
/// erraten.
/// </summary>
public class GroupStandardCost
{
    public int Id { get; set; }

    /// <summary>Normalisierte Materialnummer (siehe MaterialKeyNormalizer), Schluesselteil.</summary>
    public string MaterialKey { get; set; } = string.Empty;

    /// <summary>MBEW-Bewertungskreis der liefernden Gesellschaft (aktuell nur "1100" = TR AG).</summary>
    public string ValuationArea { get; set; } = string.Empty;

    /// <summary>Stueckkosten (STPRS bereits durch PEINH geteilt).</summary>
    public decimal UnitCost { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime RefreshedAtUtc { get; set; }
}
