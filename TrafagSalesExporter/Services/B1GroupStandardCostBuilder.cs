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
        return records
            .Select(record => new Candidate(
                MaterialKey: MaterialKeyNormalizer.Normalize(
                    string.IsNullOrWhiteSpace(record.GroupMaterialNumber)
                        ? record.Material
                        : record.GroupMaterialNumber),
                UnitCost: Math.Abs(record.StandardCost),
                Currency: FirstNonEmpty(record.StandardCostCurrency, record.CompanyCurrency),
                EffectiveDate: record.PostingDate ?? record.InvoiceDate ?? record.ExtractionDate,
                ExtractionDate: record.ExtractionDate,
                DocumentEntry: record.DocumentEntry,
                PositionOnInvoice: record.PositionOnInvoice,
                SourceLineId: record.SourceLineId ?? string.Empty))
            .Where(candidate => candidate.MaterialKey.Length > 0 &&
                                candidate.UnitCost > 0m &&
                                candidate.Currency.Equals(expectedCurrency, StringComparison.OrdinalIgnoreCase))
            .GroupBy(candidate => candidate.MaterialKey, StringComparer.Ordinal)
            .Select(group => BuildCost(group, mode, area, expectedCurrency, refreshedAtUtc))
            .OrderBy(cost => cost.MaterialKey, StringComparer.Ordinal)
            .ToList();
    }

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
        if (costs.Count == 0)
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
