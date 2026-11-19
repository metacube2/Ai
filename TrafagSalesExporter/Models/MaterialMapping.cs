namespace TrafagSalesExporter.Models;

/// <summary>
/// Zuordnung einer lokalen Artikelnummer einer Gesellschaft (TSC) auf die Gruppen-Artikelnummer (Sachnummer Trafag AG).
/// Plan docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md Abschnitt 5, Entscheide Ingo 19.11.2026: Ablage in der
/// Dashboard-DB (E6), Regeltreffer wirken sofort, Texttreffer nur im Status „bestaetigt“ (E5). Eine Zeile wirkt nur auf
/// ihren eigenen TSC und nur dort, wo die Quelle keine Gruppennummer liefert; sie ueberschreibt nie.
/// </summary>
public class MaterialMapping
{
    public int Id { get; set; }

    /// <summary>Verkaufende Gesellschaft, fuer die die Zeile gilt (z. B. TRUK).</summary>
    public string Tsc { get; set; } = string.Empty;

    /// <summary>Lokale Artikelnummer wie in Sales_All.</summary>
    public string LocalMaterial { get; set; } = string.Empty;

    /// <summary>Normalisierter Schluessel der lokalen Nummer (MaterialKeyNormalizer), eindeutig je TSC.</summary>
    public string LocalMaterialKey { get; set; } = string.Empty;

    public string LocalText { get; set; } = string.Empty;

    /// <summary>Ziel: Sachnummer Trafag AG.</summary>
    public string GroupMaterialNumber { get; set; } = string.Empty;

    /// <summary>Materialtext Trafag AG (deutsch), soweit bekannt.</summary>
    public string GroupText { get; set; } = string.Empty;

    /// <summary>Liefernde Gesellschaft (TR AG, TR IN, TR IT), nur wo deren Nummer abweicht.</summary>
    public string DeliveringEntity { get; set; } = string.Empty;

    public string DeliveringMaterialNumber { get; set; } = string.Empty;

    /// <summary>Regel, Text oder Hand (MaterialMappingMethods).</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Welche Regel bzw. welches Textmerkmal den Vorschlag ergab.</summary>
    public string MethodDetail { get; set; } = string.Empty;

    /// <summary>Sicherheit 0 bis 100; 100 bei Regel, sonst Wert der Textaehnlichkeit.</summary>
    public int Confidence { get; set; }

    /// <summary>Vorschlag, bestaetigt oder abgelehnt (MaterialMappingStatuses).</summary>
    public string Status { get; set; } = string.Empty;

    public string ReviewedBy { get; set; } = string.Empty;
    public DateTime? ReviewedAtUtc { get; set; }

    /// <summary>Gewicht: Zeilen der Gesellschaft mit dieser Nummer beim letzten Vorschlagslauf.</summary>
    public int Rows { get; set; }

    /// <summary>Gewicht: Summe Sales Price/Value in Hauswaehrung beim letzten Vorschlagslauf.</summary>
    public decimal NetSales { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
