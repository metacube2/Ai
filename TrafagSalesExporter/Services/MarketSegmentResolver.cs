using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Loest das Marktsegment einer Verkaufszeile ueber die gepflegte Kundenzuordnung auf.
///
/// Rein und statisch, damit Excel-Export, Cockpit und Tests dieselbe Regel verwenden. Die
/// Klasse kennt bewusst nur EINE Quelle: die Tabelle <see cref="CustomerMarketSegment"/>.
///
/// KEIN Rueckfall auf das Quellfeld `CustomerIndustry`. Am 2026-08-12 gemessen: die Spalte
/// ist bei CH, AT, DE, ES, UK und US zu 0 % gefuellt, und wo sie gefuellt ist, benutzt der
/// Standort seine eigene Taxonomie — von 210 franzoesischen Zeilen tragen 150
/// `Ship Building`. Ein solcher Wert unter einer Spalte, die Leser als verbindlich
/// verstehen, waere schlimmer als ein leeres Feld. Unzugeordnet bleibt deshalb leer.
/// </summary>
public static class MarketSegmentResolver
{
    /// <summary>Quellenkennzeichen fuer eine Zeile, die ueber die Kundenzuordnung gefunden wurde.</summary>
    public const string SourceCustomerMap = "Kundenzuordnung";

    /// <summary>
    /// Baut die Nachschlagetabelle. Doppelte Schluessel gewinnen nach dem jueng*sten*
    /// <see cref="CustomerMarketSegment.UpdatedAtUtc"/>, damit eine Korrektur eine aeltere
    /// Zuordnung ueberstimmt, ohne dass die Historie geloescht werden muss.
    /// </summary>
    public static IReadOnlyDictionary<(string Tsc, string CustomerNumber), CustomerMarketSegment> BuildLookup(
        IEnumerable<CustomerMarketSegment>? rows)
    {
        var lookup = new Dictionary<(string, string), CustomerMarketSegment>();
        if (rows is null) return lookup;

        foreach (var row in rows)
        {
            var tsc = NormalizeTsc(row.Tsc);
            var customer = NormalizeCustomerNumber(row.CustomerNumber);
            if (tsc.Length == 0 || customer.Length == 0) continue;
            if (string.IsNullOrWhiteSpace(row.Segment)) continue;

            var key = (tsc, customer);
            if (lookup.TryGetValue(key, out var existing) && existing.UpdatedAtUtc >= row.UpdatedAtUtc)
                continue;
            lookup[key] = row;
        }

        return lookup;
    }

    /// <summary>
    /// Segment und Quelle einer Verkaufszeile. Beides leer, wenn keine Zuordnung existiert
    /// oder Standort beziehungsweise Kundennummer fehlen.
    /// </summary>
    public static (string Segment, string Source) Resolve(
        string? tsc,
        string? customerNumber,
        IReadOnlyDictionary<(string Tsc, string CustomerNumber), CustomerMarketSegment>? lookup)
    {
        if (lookup is null || lookup.Count == 0) return (string.Empty, string.Empty);

        var normalizedTsc = NormalizeTsc(tsc);
        var normalizedCustomer = NormalizeCustomerNumber(customerNumber);
        if (normalizedTsc.Length == 0 || normalizedCustomer.Length == 0) return (string.Empty, string.Empty);

        if (!lookup.TryGetValue((normalizedTsc, normalizedCustomer), out var hit))
            return (string.Empty, string.Empty);

        var source = string.IsNullOrWhiteSpace(hit.Source) ? SourceCustomerMap : hit.Source.Trim();
        return (hit.Segment.Trim(), source);
    }

    public static string NormalizeTsc(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>
    /// Kundennummer nur trimmen und in Grossschreibung. Fuehrende Nullen werden BEWUSST
    /// nicht entfernt: beide Seiten stammen aus demselben Feld derselben Quelle, und ein
    /// Abschneiden koennte zwei verschiedene Kunden zusammenfuehren.
    /// </summary>
    public static string NormalizeCustomerNumber(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();
}
