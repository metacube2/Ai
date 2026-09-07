namespace TrafagSalesExporter.Models;

/// <summary>
/// Eine Zeile einer Marktumfrage, zum Beispiel der Railway-Umfrage vom Mai 2026.
///
/// Ziel dieser Tabelle ist die Abloesung der Excel-Datei: die Umfrage soll in der
/// Anwendung pflegbar sein, damit nicht mehrere Fassungen parallel herumliegen.
///
/// Wichtig zur Abgrenzung: eine Umfragezeile ist MARKTWISSEN und kein Umsatz. Sie kann
/// einen Interessenten beschreiben, zu dem es gar keinen Kunden im ERP gibt; in der
/// Railway-Umfrage trifft das auf 83 von 234 Namen zu. Deshalb ist die Verknuepfung zu
/// einem Verkaufskunden ueber <see cref="LinkedTsc"/> und
/// <see cref="LinkedCustomerNumber"/> optional. Ohne diese Trennung wuerden Interessenten
/// beim Import verloren gehen.
/// </summary>
public class MarketSurveyEntry
{
    public int Id { get; set; }

    /// <summary>
    /// Name und Stand der Umfrage, zum Beispiel `Railway 2026-05`. Erlaubt spaeter weitere
    /// Umfragen nebeneinander, ohne das Modell zu aendern.
    /// </summary>
    public string SurveyName { get; set; } = string.Empty;

    /// <summary>Befragtes Land als ISO-2, zum Beispiel `AT`.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Kundenname wie in der Umfrage geschrieben, nicht aus dem Kundenstamm.</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Kurzform des Kundennamens aus der Umfrage.</summary>
    public string CustomerShort { get; set; } = string.Empty;

    /// <summary>Rolle im Markt, zum Beispiel `Tier 1`, `OEM` oder `Operator / Maintenance`.</summary>
    public string CustomerType { get; set; } = string.Empty;

    /// <summary>`Project` oder `Serial`.</summary>
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>Anwendung beim Kunden, zum Beispiel `Brakes`, `Pantographs` oder `HVAC`.</summary>
    public string Application { get; set; } = string.Empty;

    public string ApplicationDescription { get; set; } = string.Empty;

    /// <summary>`New`, `Opportunity`, `Existing Customer` oder `No Potential`.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Verkaufsargument von Trafag in dieser Anwendung.</summary>
    public string TrafagUsp { get; set; } = string.Empty;

    public string Competitor { get; set; } = string.Empty;

    /// <summary>
    /// Produktkuerzel aus der Umfrage, zum Beispiel `EPR`, `NAR` oder `9B4`. Das ist eine
    /// Produktfamilie und KEINE Materialnummer; ein Abgleich auf Verkaufszeilen ist damit
    /// nicht moeglich, weil dieselben Familien in alle Branchen gehen.
    /// </summary>
    public string Product { get; set; } = string.Empty;

    /// <summary>
    /// Materialnummer, falls die Umfrage eine nennt. In der Railway-Umfrage vom Mai 2026 ist
    /// diese Spalte in 0 von 269 Zeilen gefuellt.
    /// </summary>
    public string MaterialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Geschaetzte Jahresmenge als TEXT. Die Quelle enthaelt Bereiche und Einheiten wie
    /// `500-600 pcs`, deshalb waere eine Zahl eine Scheingenauigkeit.
    /// </summary>
    public string EstimatedQuantity { get; set; } = string.Empty;

    /// <summary>
    /// Geschaetzter Stueckpreis als TEXT. Die Spalte heisst in der Quelle „In CHF", enthaelt
    /// aber gemischte Waehrungen wie `15k€` neben `CHF 45`. Als Zahl gespeichert wuerde das
    /// zu falschen Summen verleiten.
    /// </summary>
    public string EstimatedPrice { get; set; } = string.Empty;

    public string Comments { get; set; } = string.Empty;

    /// <summary>Standort des verknuepften Verkaufskunden, leer wenn kein Umsatz existiert.</summary>
    public string LinkedTsc { get; set; } = string.Empty;

    /// <summary>Kundennummer des verknuepften Verkaufskunden, leer wenn nicht verknuepft.</summary>
    public string LinkedCustomerNumber { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}
