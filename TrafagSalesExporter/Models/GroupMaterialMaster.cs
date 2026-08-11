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
}
