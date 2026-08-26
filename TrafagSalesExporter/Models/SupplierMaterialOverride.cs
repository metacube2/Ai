namespace TrafagSalesExporter.Models;

/// <summary>
/// Ersatzquelle fuer den Lieferanten eines Artikels, wenn der Artikelstamm der Quelle ihn
/// (noch) nicht fuehrt. Bewusst eine UEBERGANGSLOESUNG mit begrenzter Lebensdauer:
///
/// Trafag Italia hat am 2026-08-26 die 941 TR-IT-Artikel ohne <c>OITM.CardCode</c> geprueft
/// und den Lieferanten geliefert, konnte ihn aber mangels DTW-Zugriff noch nicht im
/// Artikelstamm pflegen; das geschieht manuell ueber die naechsten Wochen
/// (Paola Castagna, Finance &amp; Administration, siehe docs/FINANCE_SUPPLIER.md).
///
/// Deshalb gilt strikt: Diese Tabelle greift NUR, wenn die Quelle alle drei Lieferantenfelder
/// leer laesst. Sobald der Artikelstamm gepflegt ist, gewinnt automatisch wieder die Quelle und
/// der Eintrag hier wird wirkungslos. Es wird nie ein vorhandener Wert ueberschrieben.
/// </summary>
public class SupplierMaterialOverride
{
    public int Id { get; set; }

    /// <summary>Standort, fuer den die Zuordnung gilt. Eine Zuordnung ist nie standortuebergreifend.</summary>
    public string Tsc { get; set; } = string.Empty;

    /// <summary>Normalisierte Materialnummer (siehe <see cref="Services.MaterialKeyNormalizer"/>), Schluesselteil.</summary>
    public string MaterialKey { get; set; } = string.Empty;

    /// <summary>Lieferantennummer wie sie die Quelle fuehren wuerde (B1: <c>OITM.CardCode</c>).</summary>
    public string SupplierNumber { get; set; } = string.Empty;

    /// <summary>Lieferantenname. Traegt die Klassifikation, siehe GroupMarginSupplierClassifier.</summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>Laenderkennzeichen des Lieferanten.</summary>
    public string SupplierCountry { get; set; } = string.Empty;

    /// <summary>Herkunft im Klartext, damit im Nachhinein belegbar bleibt, wer den Wert verantwortet.</summary>
    public string Source { get; set; } = string.Empty;

    public DateTime ImportedAtUtc { get; set; }
}
