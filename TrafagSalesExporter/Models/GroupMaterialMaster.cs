namespace TrafagSalesExporter.Models;

/// <summary>
/// Material, das im Werkstamm einer liefernden Trafag-Gesellschaft gefuehrt wird.
/// Fuer den Supplier-Fallback wird aktuell MARC/WERKS 1100 der Trafag AG verwendet.
/// Die Tabelle enthaelt bewusst keine Kosten; diese bleiben in GroupStandardCosts.
/// </summary>
public class GroupMaterialMaster
{
    public int Id { get; set; }
    public string MaterialKey { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public DateTime RefreshedAtUtc { get; set; }

    /// <summary>
    /// Materialtext Trafag AG, Sprache fest Deutsch (MAKT SPRAS = D), seit 2026-11-19 fuer den Mapping-Vorschlagslauf.
    /// Leer, solange der Gateway-Abzug das Feld nicht liefert (siehe SapGatewayPlantMaterialReader.MaterialTextProperty).
    /// </summary>
    public string MaterialText { get; set; } = string.Empty;
}
