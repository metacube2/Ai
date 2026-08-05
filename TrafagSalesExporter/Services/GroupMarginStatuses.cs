namespace TrafagSalesExporter.Services;

/// <summary>
/// Statuswerte der Gruppenmarge, die an mehreren Stellen gleich lauten muessen: im Excel-Nachweis
/// (<see cref="ExcelExportService"/>), im Management-Cockpit (<see cref="ManagementCockpitService"/>)
/// und in der Pruefsummen-Formel des Arbeitsblatts. Als Konstante, damit die drei Stellen nicht
/// auseinanderlaufen.
/// </summary>
public static class GroupMarginStatuses
{
    /// <summary>
    /// Der Standort verkauft Ware einer anderen Konzerngesellschaft (Sales Type <c>LRD</c>), die
    /// Konzern-Standardkosten sind fuer das Material aber nicht auffindbar. Der lokale
    /// Standardpreis ist in diesem Fall der IC-Einkaufspreis und wird bewusst NICHT als
    /// Kostenbasis verwendet - die Zeile bleibt offen statt eine Marge auf dem Verrechnungspreis
    /// auszuweisen. Siehe docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md Abschnitt 6a.
    /// </summary>
    public const string GroupCostMissing = "Konzernkosten fehlen";

    /// <summary>Beschriftung der Kostenquelle fuer <see cref="GroupCostMissing"/>.</summary>
    public const string GroupCostMissingSource = "Konzernkosten fehlen (lokaler Wert ist IC-Preis)";
}
