using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Setzt die Kostenbasis (`StandardCost`) fuer die CH/AT-Umsatzzeilen aus den
/// SAP-Standardpreisen (MBEW). Der Umsatz-Service liefert keinen Kostenwert;
/// ohne diese Anreicherung bleibt `StandardCost` 0 und die Gruppenmarge fuer
/// CH/AT ist nicht berechenbar.
///
/// `StandardCost` ist ein STUECKpreis — die Margenlogik rechnet `Menge x StandardCost`
/// (siehe `ManagementCockpitService.ResolveGroupMarginCostBasis`). Der Reader hat die
/// Preiseinheit bereits herausgerechnet.
/// </summary>
public static class StandardCostEnricher
{
    /// <summary>
    /// Land -> Bewertungskreis (MBEW-BWKEY). Bestaetigt durch den ABAP-Analysereport
    /// (T001K): Buchungskreis 1100 = Trafag AG (CH, CHF), 1200 = Trafag Ges.m.b.H. (AT, EUR);
    /// der Bewertungskreis ist jeweils gleich dem Buchungskreis.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ValuationAreaByCountry =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CH"] = "1100",
            ["AT"] = "1200"
        };

    public static IReadOnlyCollection<string> ValuationAreas => ValuationAreaByCountry.Values.Distinct().ToList();

    /// <summary>Liefert den Bewertungskreis zu einer Umsatzzeile, oder null wenn das Land unbekannt ist.</summary>
    public static string? ResolveValuationArea(string? land)
    {
        if (string.IsNullOrWhiteSpace(land))
            return null;
        return ValuationAreaByCountry.TryGetValue(land.Trim(), out var area) ? area : null;
    }

    /// <summary>
    /// Reichert die Zeilen an und liefert zurueck, wie viele Zeilen eine Kostenbasis bekommen
    /// haben. Zeilen ohne Treffer bleiben unveraendert (StandardCost 0) — es wird bewusst
    /// nichts geschaetzt, damit die Gruppenmarge fehlende Kosten als offen ausweisen kann.
    /// </summary>
    public static StandardCostEnrichmentResult Apply(
        IReadOnlyCollection<SalesRecord> records,
        IReadOnlyDictionary<StandardCostKey, StandardCostEntry> standardCosts)
    {
        var matched = 0;
        var missing = 0;

        foreach (var record in records)
        {
            var area = ResolveValuationArea(record.Land);
            if (area is null)
            {
                missing++;
                continue;
            }

            var materialKey = MaterialKeyNormalizer.Normalize(record.Material);
            if (string.IsNullOrWhiteSpace(materialKey))
            {
                missing++;
                continue;
            }

            if (!standardCosts.TryGetValue(new StandardCostKey(materialKey, area), out var entry))
            {
                missing++;
                continue;
            }

            record.StandardCost = entry.UnitCost;
            // Die Bewertung erfolgt in der Hauswaehrung des Buchungskreises; die ist
            // fuer diese Zeilen bereits als Hauswaehrung gemappt (CH = CHF, AT = EUR).
            if (string.IsNullOrWhiteSpace(record.StandardCostCurrency))
                record.StandardCostCurrency = record.CompanyCurrency;
            matched++;
        }

        return new StandardCostEnrichmentResult(matched, missing);
    }
}

public sealed record StandardCostEnrichmentResult(int Matched, int Missing)
{
    public int Total => Matched + Missing;
    public decimal MatchedPercent => Total == 0 ? 0m : Math.Round(Matched * 100m / Total, 1);
}
