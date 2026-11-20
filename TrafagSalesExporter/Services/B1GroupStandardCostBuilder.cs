using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Baut die Konzernkosten-Sicht fuer TR IT/TR IN aus den eigenen B1-Verkaufszeilen.
/// B1 liefert die belastbare Kostenbasis auf Belegebene als INV1/RIN1.StockPrice;
/// deshalb gilt je Material der juengste positive beobachtete Stueckpreis.
/// </summary>
public static class B1GroupStandardCostBuilder
{
    public static IReadOnlyList<GroupStandardCost> Build(
        string? tsc,
        IEnumerable<SalesRecord> records,
        DateTime refreshedAtUtc,
        string? costMode = null)
    {
        if (!GroupStandardCostAreas.TryResolveB1Source(tsc, out var area, out var expectedCurrency))
            return [];

        var mode = B1GroupStandardCostModes.Normalize(costMode);
        var candidates = records
            .Select(record => ToCandidate(record, MaterialKeyNormalizer.Normalize(
                string.IsNullOrWhiteSpace(record.GroupMaterialNumber)
                    ? record.Material
                    : record.GroupMaterialNumber)))
            .Where(candidate => IsUsable(candidate, expectedCurrency))
            .ToList();

        var primary = candidates
            .GroupBy(candidate => candidate.MaterialKey, StringComparer.Ordinal)
            .Select(group => BuildCost(group, mode, area, expectedCurrency, refreshedAtUtc))
            .ToList();

        // AP1, Entscheid Ingo 19.11.2026 (Plan docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md Abschnitt 5): Die Kosten
        // von TR IN zusaetzlich unter der INDISCHEN Artikelnummer ablegen. UK und US verkaufen Ware aus Indien unter dieser
        // Nummer (z. B. TS000095), Indien fuehrt den Preis aber unter der Trafag-Sachnummer (U_TASC_OMN, z. B. 35560);
        // gemessen 18.11.: UK 70 und US 2 Zeilen fanden deshalb keine Kosten (docs/FINANCE_SUPPLIER.md 12a).
        // Der Primaerschluessel (Sachnummer) gewinnt immer: ein Zweitschluessel entsteht nur, wo kein Primaerkandidat
        // denselben Schluessel liefert. Nur TRIN, weil nur Indien eigene Nummern neben der Sachnummer fuehrt.
        if (!string.Equals(area, GroupStandardCostAreas.ByEntity[GroupStandardCostEntities.TrIn], StringComparison.Ordinal))
            return WithKnownKeysWithoutCost(primary, records, includeIndianNumber: false, area, expectedCurrency, refreshedAtUtc);

        var primaryKeys = new HashSet<string>(primary.Select(cost => cost.MaterialKey), StringComparer.Ordinal);
        var secondary = records
            .Where(record => !string.IsNullOrWhiteSpace(record.GroupMaterialNumber))
            .Select(record => ToCandidate(record, MaterialKeyNormalizer.Normalize(record.Material)))
            .Where(candidate => IsUsable(candidate, expectedCurrency) && !primaryKeys.Contains(candidate.MaterialKey))
            .GroupBy(candidate => candidate.MaterialKey, StringComparer.Ordinal)
            .Select(group => BuildCost(group, mode, area, expectedCurrency, refreshedAtUtc));

        return WithKnownKeysWithoutCost(primary.Concat(secondary).ToList(), records, includeIndianNumber: true, area,
            expectedCurrency, refreshedAtUtc);
    }

    /// <summary>
    /// Paket C (Plan docs/FINANCE_PLAN_MAPPING_STUFE2_2026-11-20.md, 20.11.2026): Artikel, die die Gesellschaft verkauft hat, aber
    /// nur mit Stueckkosten 0, werden mit UnitCost 0 abgelegt. Sonst ist „Artikel gefunden, Kosten fehlen“ (Issue Topic 3/4) nicht
    /// von „nicht gefunden“ (2/7) zu unterscheiden. Die Kostenregel nimmt nur UnitCost &gt; 0 (GroupMarginCostRules.GroupStandardCost),
    /// die Rechnung faellt also wie bisher auf die lokalen Kosten; dasselbe Muster gilt fuer TR AG schon heute (MBEW-Eintraege mit 0).
    /// Ein positiver Eintrag gewinnt immer. Bei 0 spielt die Waehrung keine Rolle, deshalb ohne Waehrungspruefung.
    /// </summary>
    private static IReadOnlyList<GroupStandardCost> WithKnownKeysWithoutCost(
        IReadOnlyList<GroupStandardCost> costs,
        IEnumerable<SalesRecord> records,
        bool includeIndianNumber,
        string area,
        string expectedCurrency,
        DateTime refreshedAtUtc)
    {
        var present = new HashSet<string>(costs.Select(cost => cost.MaterialKey), StringComparer.Ordinal);
        var zero = new List<GroupStandardCost>();
        foreach (var record in records.Where(record => record.StandardCost == 0m))
        {
            var keys = new List<string>
            {
                MaterialKeyNormalizer.Normalize(string.IsNullOrWhiteSpace(record.GroupMaterialNumber)
                    ? record.Material
                    : record.GroupMaterialNumber)
            };
            if (includeIndianNumber && !string.IsNullOrWhiteSpace(record.GroupMaterialNumber))
                keys.Add(MaterialKeyNormalizer.Normalize(record.Material));

            foreach (var key in keys.Where(key => key.Length > 0 && present.Add(key)))
            {
                zero.Add(new GroupStandardCost
                {
                    MaterialKey = key,
                    ValuationArea = area,
                    UnitCost = 0m,
                    Currency = expectedCurrency,
                    RefreshedAtUtc = refreshedAtUtc
                });
            }
        }

        return costs.Concat(zero)
            .OrderBy(cost => cost.MaterialKey, StringComparer.Ordinal)
            .ToList();
    }

    private static Candidate ToCandidate(SalesRecord record, string materialKey)
        => new(
            MaterialKey: materialKey,
            UnitCost: Math.Abs(record.StandardCost),
            Currency: FirstNonEmpty(record.StandardCostCurrency, record.CompanyCurrency),
            EffectiveDate: record.PostingDate ?? record.InvoiceDate ?? record.ExtractionDate,
            ExtractionDate: record.ExtractionDate,
            DocumentEntry: record.DocumentEntry,
            PositionOnInvoice: record.PositionOnInvoice,
            SourceLineId: record.SourceLineId ?? string.Empty);

    private static bool IsUsable(Candidate candidate, string expectedCurrency)
        => candidate.MaterialKey.Length > 0 &&
           candidate.UnitCost > 0m &&
           candidate.Currency.Equals(expectedCurrency, StringComparison.OrdinalIgnoreCase);

    private static GroupStandardCost BuildCost(
        IGrouping<string, Candidate> candidates,
        string mode,
        string area,
        string expectedCurrency,
        DateTime refreshedAtUtc)
    {
        var selected = candidates
            .OrderByDescending(candidate => candidate.EffectiveDate)
            .ThenByDescending(candidate => candidate.ExtractionDate)
            .ThenByDescending(candidate => candidate.DocumentEntry)
            .ThenByDescending(candidate => candidate.PositionOnInvoice)
            .ThenByDescending(candidate => candidate.SourceLineId, StringComparer.Ordinal)
            .First();

        return new GroupStandardCost
            {
                MaterialKey = candidates.Key,
                ValuationArea = area,
                UnitCost = mode == B1GroupStandardCostModes.AveragePositive
                    ? candidates.Average(candidate => candidate.UnitCost)
                    : selected.UnitCost,
                Currency = expectedCurrency,
                RefreshedAtUtc = refreshedAtUtc
            };
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private sealed record Candidate(
        string MaterialKey,
        decimal UnitCost,
        string Currency,
        DateTime EffectiveDate,
        DateTime ExtractionDate,
        int DocumentEntry,
        int PositionOnInvoice,
        string SourceLineId);
}

public static class B1GroupStandardCostStore
{
    public static async Task<B1GroupStandardCostRefreshResult> ReplaceAsync(
        AppDbContext db,
        string? tsc,
        IReadOnlyCollection<SalesRecord> records,
        DateTime refreshedAtUtc,
        string? costMode = null,
        CancellationToken cancellationToken = default)
    {
        if (!GroupStandardCostAreas.TryResolveB1Source(tsc, out var area, out _))
            return new(false, string.Empty, 0);

        var costs = B1GroupStandardCostBuilder.Build(tsc, records, refreshedAtUtc, costMode);
        // Seit Paket C (20.11.2026) enthaelt Build auch Artikel mit Kosten 0. Ein Lauf ganz ohne positive Kosten ersetzt den
        // gespeicherten Stand weiterhin nicht (Schutz vor einem leeren oder fehlerhaften Quelllauf).
        if (!costs.Any(cost => cost.UnitCost > 0m))
            return new(false, area, 0);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.GroupStandardCosts
            .Where(cost => cost.ValuationArea == area)
            .ExecuteDeleteAsync(cancellationToken);
        db.GroupStandardCosts.AddRange(costs);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(true, area, costs.Count);
    }
}

public sealed record B1GroupStandardCostRefreshResult(bool Updated, string Area, int MaterialCount);
