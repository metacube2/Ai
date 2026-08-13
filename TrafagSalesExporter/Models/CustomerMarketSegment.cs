namespace TrafagSalesExporter.Models;

/// <summary>
/// Zuordnung eines Kunden zu einem Marktsegment, zum Beispiel "Railway".
///
/// Entscheid Ingo, 2026-08-12: das Segment haengt am KUNDEN, nicht am Produkt und nicht an
/// der Anwendung. Gemessen auf dem Gesamtexport vom selben Tag: von 105 zugeordneten
/// Bahnkunden kaufen 91 hoechstens drei Produktfamilien, der Kunde ist also fast immer
/// eindeutig. Die Produktkuerzel der Marktumfrage dagegen sind Standardfamilien, die in
/// alle Branchen gehen (`NAT` in 7'417 Zeilen, `8252` in 6'217) und trennen das Segment
/// nicht. Eine Anwendung wie "Brakes" ist Verwendungszweck beim Kunden und in keiner
/// Verkaufszeile als Stammdatum vorhanden.
///
/// Schluessel ist <see cref="Tsc"/> plus <see cref="CustomerNumber"/>, nicht der Name.
/// Kundennummern sind produktiv in allen neun Standorten zu 100 % gefuellt (4'888
/// verschiedene). Namen dagegen kollabieren beim Abgleich nachweislich: "BROT" trifft
/// "K.S. &amp; BROTHERS", und "Stadler Rail" aus der Schweiz landet auf der spanischen
/// "Stadler Rail Valencia".
/// </summary>
public class CustomerMarketSegment
{
    public int Id { get; set; }

    /// <summary>Standortkuerzel der Verkaufszeile, zum Beispiel `TRCH`.</summary>
    public string Tsc { get; set; } = string.Empty;

    /// <summary>Lokale Kundennummer aus dem Kundenstamm des Standortsystems.</summary>
    public string CustomerNumber { get; set; } = string.Empty;

    /// <summary>
    /// Kundenname zum Zeitpunkt der Zuordnung. Reine Lesehilfe fuer die Pflege und den
    /// Audit; fuer die Aufloesung wird er bewusst NICHT verwendet.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Marktsegment, zum Beispiel `Railway`.</summary>
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// Woher die Zuordnung stammt, zum Beispiel
    /// `Marktumfrage Railway 2026-05, bestaetigt`. Dokumentiert, wer die Aussage traegt.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}
