namespace TrafagSalesExporter.Models;

/// <summary>
/// Konzern-Standardkosten je Material und Bewertungskreis der liefernden Trafag-
/// Gesellschaft, unabhaengig
/// davon, welche Gesellschaft die Umsatzzeile verkauft hat. Wird beim Import der
/// kostengebenden Gesellschaft befuellt (TR AG: SapGatewayDataSourceAdapter; TR IT/TR IN:
/// HanaDataSourceAdapter), damit Gruppenmarge-Zeilen anderer TSC mit internem Lieferanten
/// die echte Konzern-Herstellkostenbasis nutzen koennen
/// (Mappe1.xlsx-Spezifikation, siehe docs/FINANCE_GRUPPENMARGE_2026-06-16.md).
///
/// TR IN/TR IT kommen nicht aus dem weitgehend leeren B1-Artikelstamm, sondern aus dem
/// jeweils juengsten positiven INV1/RIN1.StockPrice ihrer eigenen Verkaufszeilen.
/// </summary>
public class GroupStandardCost
{
    public int Id { get; set; }

    /// <summary>Normalisierte Materialnummer (siehe MaterialKeyNormalizer), Schluesselteil.</summary>
    public string MaterialKey { get; set; } = string.Empty;

    /// <summary>Kostenbereich der liefernden Gesellschaft (1100, TRIT oder TRIN).</summary>
    public string ValuationArea { get; set; } = string.Empty;

    /// <summary>Stueckkosten (TR AG: STPRS/PEINH; TR IT/TR IN: B1 StockPrice).</summary>
    public decimal UnitCost { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime RefreshedAtUtc { get; set; }
}
