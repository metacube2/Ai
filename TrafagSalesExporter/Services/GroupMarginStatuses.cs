namespace TrafagSalesExporter.Services;

/// <summary>
/// Statuswerte der Gruppenmarge. Sie muessen an mehreren Stellen ZEICHENGLEICH lauten: im
/// Excel-Nachweis (<see cref="ExcelExportService"/>), im Management-Cockpit
/// (<see cref="ManagementCockpitService"/>), in der Pruefsummen-Formel des Arbeitsblatts und
/// in der Statusfarbe der Cockpit-Tabelle. Ein Tippfehler an einer dieser Stellen faellt nicht
/// auf - die Zeile bekommt nur still keine Farbe mehr oder faellt aus einer COUNTIF-Summe.
///
/// Deshalb stehen hier nicht nur die Texte, sondern auch die beiden Listen, die sonst
/// auseinanderlaufen: was als OFFEN gilt (<see cref="IsOpen"/>) und in welcher Reihenfolge die
/// Stati angezeigt werden (<see cref="Sort"/>). Genau das ist am 2026-08-05 passiert - der neue
/// Status <see cref="GroupCostMissing"/> war im Excel-Nachweis sortiert und gezaehlt, im Cockpit
/// aber weder das eine noch das andere. Ein neuer Status wird jetzt genau einmal hier eingetragen.
/// </summary>
public static class GroupMarginStatuses
{
    /// <summary>Marge belastbar.</summary>
    public const string Ok = "OK";

    /// <summary>Lieferantennummer, -name und -land sind alle drei leer - Typ nicht erkennbar.</summary>
    public const string SupplierUnclear = "Lieferant unklar";

    /// <summary>Kostenbasis der Zeile ist 0 - das Quellsystem hat keinen Standardpreis geliefert.</summary>
    public const string StandardCostMissing = "Standardpreis fehlt";

    /// <summary>
    /// Der Standort verkauft Ware einer anderen Konzerngesellschaft (Sales Type <c>LRD</c>), die
    /// Konzern-Standardkosten sind fuer das Material aber nicht auffindbar. Der lokale
    /// Standardpreis ist in diesem Fall der IC-Einkaufspreis und wird bewusst NICHT als
    /// Kostenbasis verwendet - die Zeile bleibt offen statt eine Marge auf dem Verrechnungspreis
    /// auszuweisen. Siehe docs/FINANCE_TRIN_EIGENFERTIGUNG_2026-08-05.md Abschnitt 6a.
    /// </summary>
    public const string GroupCostMissing = "Konzernkosten fehlen";

    /// <summary>Zeile hat Wert 0 - Diagnosefall, z. B. reine Mengen-/Korrekturzeile.</summary>
    public const string SalesMissing = "Umsatz fehlt";

    /// <summary>Fuer Jahr/Waehrung ist kein CHF-Kurs gepflegt. Nur im Audit-Ledger moeglich.</summary>
    public const string ExchangeRateMissing = "Kurs fehlt";

    /// <summary>Beschriftung der Kostenquelle fuer <see cref="GroupCostMissing"/>.</summary>
    public const string GroupCostMissingSource = "Konzernkosten fehlen (lokaler Wert ist IC-Preis)";

    /// <summary>
    /// Stati mit nicht belastbarer Kostenbasis, in Anzeigereihenfolge. Diese Liste ist zugleich
    /// die Definition von „offen" (<see cref="IsOpen"/>) und der Anfang der Sortierung
    /// (<see cref="Sort"/>) - damit ein neuer Status nicht in der einen Liste auftaucht und in
    /// der anderen fehlt. <see cref="SalesMissing"/> steht bewusst NICHT drin: dort ist die
    /// Kostenbasis in Ordnung, es fehlt der Umsatz.
    /// </summary>
    public static readonly IReadOnlyList<string> Open = new[]
    {
        StandardCostMissing,
        SupplierUnclear,
        GroupMarginCostCurrencyConverter.OpenStatus,
        GroupCostMissing
    };

    /// <summary>Anzeigereihenfolge: erst die offenen Stati, dann <see cref="SalesMissing"/>, zuletzt OK.</summary>
    private static readonly string[] SortOrder = Open.Append(SalesMissing).ToArray();

    /// <summary>
    /// Reihenfolge im Audit-Ledger. Dort kann zusaetzlich der Umrechnungskurs fehlen, was jede
    /// weitere Aussage zur Zeile ueberlagert und deshalb vorne steht.
    /// </summary>
    private static readonly string[] AuditLedgerSortOrder =
        new[] { ExchangeRateMissing }.Concat(SortOrder).ToArray();

    /// <summary>Ist die Kostenbasis dieser Zeile nicht belastbar?</summary>
    public static bool IsOpen(string? status)
        => status is not null && Open.Contains(status, StringComparer.Ordinal);

    /// <summary>
    /// Stati OHNE Kostenbasis: die Zeile traegt Kosten 0, „Umsatz minus Kosten" waere also der
    /// volle Umsatz als Marge. Wer eine Marge rechnet, muss diese Zeilen leer lassen.
    ///
    /// Bewusst NICHT dabei ist <see cref="GroupMarginCostCurrencyConverter.OpenStatus"/>: dort ist
    /// die Kostenbasis bekannt, nur in einer anderen Waehrung als der Umsatz. Die Marge in
    /// Originalwaehrung bleibt offen (man wuerde Waehrungen mischen), die CHF-Marge ist aber
    /// rechenbar, weil beide Seiten einzeln nach CHF umgerechnet werden. Deshalb reicht
    /// <see cref="IsOpen"/> als Pruefung fuer eine Marge nicht aus.
    /// </summary>
    private static readonly string[] CostBasisUnknown =
    {
        StandardCostMissing,
        SupplierUnclear,
        GroupCostMissing
    };

    /// <summary>Liegt fuer diese Zeile ueberhaupt eine Kostenbasis vor? Siehe <see cref="CostBasisUnknown"/>.</summary>
    public static bool IsCostBasisKnown(string? status)
        => status is null || !CostBasisUnknown.Contains(status, StringComparer.Ordinal);

    /// <summary>Sortierschluessel der Gruppenmargen-Detailliste. OK und Unbekanntes sortiert nach hinten.</summary>
    public static int Sort(string? status) => IndexOrLast(SortOrder, status);

    /// <summary>Sortierschluessel des Audit-Ledgers.</summary>
    public static int AuditLedgerSort(string? status) => IndexOrLast(AuditLedgerSortOrder, status);

    private static int IndexOrLast(string[] order, string? status)
    {
        if (status is null)
            return order.Length;
        var index = Array.IndexOf(order, status);
        return index < 0 ? order.Length : index;
    }
}
