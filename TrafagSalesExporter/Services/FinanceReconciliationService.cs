using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IFinanceReconciliationService
{
    Task<List<NetSalesReferenceRow>> BuildNetSalesReferenceRowsAsync(int year = 2025);
}

public sealed class FinanceReconciliationService : IFinanceReconciliationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FinanceReconciliationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<NetSalesReferenceRow>> BuildNetSalesReferenceRowsAsync(int year = 2025)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var financeReferences = await db.FinanceReferences
            .AsNoTracking()
            .Where(r => r.IsActive && r.Year == year)
            .OrderBy(r => r.Label)
            .ToListAsync();
        var budgetRatesToChf = await LoadBudgetRatesToChfAsync(db, year);
        var intercompanyRules = await db.FinanceIntercompanyRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync();

        var centralRows = await db.CentralSalesRecords
            .AsNoTracking()
            .Where(r => (r.InvoiceDate ?? r.ExtractionDate).Year == year)
            .Select(r => new NetSalesActualSourceRow(
                r.Land,
                r.Tsc,
                r.DocumentEntry,
                r.InvoiceNumber,
                r.DocumentType,
                r.CustomerNumber,
                r.CustomerName,
                r.SalesCurrency,
                r.DocumentCurrency,
                r.CompanyCurrency,
                r.SalesPriceValue,
                r.DocumentTotalForeignCurrency,
                r.DocumentTotalLocalCurrency,
                r.VatSumForeignCurrency,
                r.VatSumLocalCurrency))
            .ToListAsync();

        var groupedActuals = centralRows
            .GroupBy(r => ResolveReferenceKey(r.Land, r.Tsc), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                rows => BuildNetSalesActual(rows, budgetRatesToChf, intercompanyRules),
                StringComparer.OrdinalIgnoreCase);

        var activeSiteKeys = (await db.Sites
                .AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => new { s.Land, s.TSC })
                .ToListAsync())
            .Select(s => ResolveReferenceKey(s.Land, s.TSC))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return financeReferences
            .Where(reference => activeSiteKeys.Contains(reference.Key) || groupedActuals.ContainsKey(reference.Key))
            .Select(reference => BuildReferenceRow(reference, groupedActuals))
            .OrderBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static NetSalesReferenceRow BuildReferenceRow(
        FinanceReference reference,
        IReadOnlyDictionary<string, NetSalesActual> groupedActuals)
    {
        groupedActuals.TryGetValue(reference.Key, out var actual);
        var referenceValue = reference.CheckValue ?? reference.LocalCurrencyValue;
        var selected = actual?.Candidates
            .OrderByDescending(candidate => candidate.Key == "NetDocumentLocalCurrency")
            .ThenByDescending(candidate => candidate.Key == "SalesPriceValue")
            .FirstOrDefault();
        var difference = selected is null || !referenceValue.HasValue ? (decimal?)null : selected.Value - referenceValue.Value;
        var intercompanyAdjustedDifference = selected is null || !referenceValue.HasValue
            ? (decimal?)null
            : selected.ValueExcludingIntercompany - referenceValue.Value;

        return new NetSalesReferenceRow
        {
            Key = reference.Key,
            Label = reference.Label,
            ActualValue = selected?.Value,
            IntercompanyDeduction = selected?.IntercompanyValue,
            ActualValueExcludingIntercompany = selected?.ValueExcludingIntercompany,
            ReferenceValue = referenceValue,
            Difference = difference,
            DifferenceExcludingIntercompany = intercompanyAdjustedDifference,
            RowCount = actual?.RowCount ?? 0,
            Currencies = actual?.Currencies ?? string.Empty,
            ValueField = selected?.Label ?? string.Empty,
            ActualCurrency = selected?.Currency ?? string.Empty,
            ReferenceSource = "check.xlsx Soll",
            ReferenceCurrency = reference.CheckValue.HasValue ? "Sollwert" : "LC",
            Status = BuildReferenceStatus(difference),
            Candidates = actual?.Candidates.Select(candidate => new NetSalesCandidateRow
            {
                Key = candidate.Key,
                Label = candidate.Label,
                Currency = candidate.Currency,
                Value = candidate.Value,
                IntercompanyValue = candidate.IntercompanyValue,
                ValueExcludingIntercompany = candidate.ValueExcludingIntercompany,
                Difference = referenceValue.HasValue ? candidate.Value - referenceValue.Value : null,
                DifferenceExcludingIntercompany = referenceValue.HasValue
                    ? candidate.ValueExcludingIntercompany - referenceValue.Value
                    : null
            }).ToList() ?? []
        };
    }

    private static async Task<IReadOnlyDictionary<string, decimal>> LoadBudgetRatesToChfAsync(AppDbContext db, int year)
    {
        var validFrom = new DateTime(year, 1, 1);
        var rates = await db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(r => r.IsActive && r.Notes == $"Budget {year}" && r.ValidFrom <= validFrom && (!r.ValidTo.HasValue || r.ValidTo >= validFrom))
            .ToListAsync();

        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["CHF"] = 1m
        };

        foreach (var rate in rates)
        {
            if (rate.ToCurrency.Equals("CHF", StringComparison.OrdinalIgnoreCase))
                result[rate.FromCurrency] = rate.Rate;
            else if (rate.FromCurrency.Equals("CHF", StringComparison.OrdinalIgnoreCase) && rate.Rate != 0m)
                result[rate.ToCurrency] = 1m / rate.Rate;
        }

        return result;
    }

    private static NetSalesActual BuildNetSalesActual(
        IEnumerable<NetSalesActualSourceRow> rows,
        IReadOnlyDictionary<string, decimal> budgetRatesToChf,
        IReadOnlyList<FinanceIntercompanyRule> intercompanyRules)
    {
        var rowList = rows.ToList();
        var documentRows = rowList
            .GroupBy(row => BuildDocumentKey(row.Tsc, row.DocumentType, row.DocumentEntry, row.InvoiceNumber), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var candidates = new List<NetSalesCandidate>
        {
            new(
                "SalesPriceValue",
                "Sales Price/Value",
                ResolveCurrencyLabel(rowList.Select(row => row.SalesCurrency)),
                rowList.Sum(row => row.SalesPriceValue),
                rowList.Where(row => IsIntercompanyCustomer(row, intercompanyRules)).Sum(row => row.SalesPriceValue))
        };

        var netDocumentForeignCurrency = documentRows.Sum(row => row.DocumentTotalForeignCurrency - row.VatSumForeignCurrency);
        if (netDocumentForeignCurrency != 0m)
            candidates.Add(new(
                "NetDocumentForeignCurrency",
                "DocTotalFC - VatSumFC",
                ResolveCurrencyLabel(rowList.Select(row => row.DocumentCurrency)),
                netDocumentForeignCurrency,
                documentRows.Where(row => IsIntercompanyCustomer(row, intercompanyRules)).Sum(row => row.DocumentTotalForeignCurrency - row.VatSumForeignCurrency)));

        var netDocumentLocalCurrency = documentRows.Sum(row => row.DocumentTotalLocalCurrency - row.VatSumLocalCurrency);
        if (netDocumentLocalCurrency != 0m)
            candidates.Add(new(
                "NetDocumentLocalCurrency",
                "Nettofakturawert Hauswaehrung",
                ResolveCurrencyLabel(rowList.Select(row => row.CompanyCurrency)),
                netDocumentLocalCurrency,
                documentRows.Where(row => IsIntercompanyCustomer(row, intercompanyRules)).Sum(row => row.DocumentTotalLocalCurrency - row.VatSumLocalCurrency)));

        var budgetChf = documentRows.Sum(row => ConvertHouseCurrencyNetToBudgetChf(row, row.DocumentTotalLocalCurrency - row.VatSumLocalCurrency, budgetRatesToChf));
        if (budgetChf != 0m)
            candidates.Add(new(
                "NetDocumentLocalCurrencyBudgetChf",
                "Nettofakturawert Hauswaehrung -> CHF Budget 2025",
                "CHF",
                budgetChf,
                documentRows.Where(row => IsIntercompanyCustomer(row, intercompanyRules)).Sum(row => ConvertHouseCurrencyNetToBudgetChf(row, row.DocumentTotalLocalCurrency - row.VatSumLocalCurrency, budgetRatesToChf))));

        return new NetSalesActual
        {
            RowCount = rowList.Count,
            Currencies = string.Join(", ", rowList.Select(row => string.IsNullOrWhiteSpace(row.CompanyCurrency) ? row.SalesCurrency : row.CompanyCurrency)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
            Candidates = candidates
        };
    }

    private static decimal ConvertHouseCurrencyNetToBudgetChf(
        NetSalesActualSourceRow row,
        decimal value,
        IReadOnlyDictionary<string, decimal> budgetRatesToChf)
    {
        var currency = (row.CompanyCurrency ?? string.Empty).Trim().ToUpperInvariant();
        return budgetRatesToChf.TryGetValue(currency, out var rate) ? value * rate : 0m;
    }

    private static bool IsIntercompanyCustomer(NetSalesActualSourceRow row, IReadOnlyList<FinanceIntercompanyRule> rules)
    {
        var customerNumber = row.CustomerNumber?.Trim() ?? string.Empty;
        var customerName = row.CustomerName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(customerNumber) && string.IsNullOrWhiteSpace(customerName))
            return false;

        var normalizedCustomerName = NormalizeRuleText(customerName);
        var referenceKey = ResolveReferenceKey(row.Land, row.Tsc);

        foreach (var rule in rules)
        {
            if (!string.IsNullOrWhiteSpace(rule.ScopeKey) &&
                !rule.ScopeKey.Equals(referenceKey, StringComparison.OrdinalIgnoreCase) &&
                !rule.ScopeKey.Equals(row.Tsc, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(rule.CustomerNumber) &&
                customerNumber.Equals(rule.CustomerNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(rule.CustomerNameContains) &&
                normalizedCustomerName.Contains(NormalizeRuleText(rule.CustomerNameContains), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string NormalizeRuleText(string value)
        => (value ?? string.Empty)
            .Replace("\u00e4", "ae", StringComparison.OrdinalIgnoreCase)
            .Replace("\u00f6", "oe", StringComparison.OrdinalIgnoreCase)
            .Replace("\u00fc", "ue", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToUpperInvariant();

    private static string ResolveCurrencyLabel(IEnumerable<string> currencies)
    {
        var distinct = currencies
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinct.Count == 0 ? "-" : string.Join(", ", distinct);
    }

    private static string BuildDocumentKey(string tsc, string documentType, int documentEntry, string invoiceNumber)
        => documentEntry > 0
            ? $"{tsc}|{documentType}|{documentEntry}"
            : $"{tsc}|{documentType}|{invoiceNumber}";

    private static string BuildReferenceStatus(decimal? difference)
    {
        if (!difference.HasValue)
            return "Keine Daten";

        return Math.Abs(difference.Value) <= 1m ? "OK" : "Pruefen";
    }

    private static string ResolveReferenceKey(string land, string tsc)
    {
        var normalizedLand = (land ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedTsc = (tsc ?? string.Empty).Trim().ToUpperInvariant();

        if (normalizedLand.Contains("FRANK") || normalizedTsc.Contains("FR")) return "FR";
        if (normalizedLand.Contains("IND") || normalizedTsc.Contains("IN")) return "IN";
        if (normalizedLand.Contains("ITAL") || normalizedTsc.Contains("IT")) return "IT";
        if (normalizedLand.Contains("ENGL") || normalizedLand.Contains("KINGDOM") || normalizedTsc.Contains("UK") || normalizedTsc.Contains("GB")) return "UK";
        if (normalizedLand.Contains("USA") || normalizedLand.Contains("UNITED STATES") || normalizedTsc.Contains("US")) return "US";
        if (normalizedLand.Contains("DEUT") || normalizedTsc.Contains("DE")) return "DE";
        if (normalizedLand.Contains("SPAN") || normalizedTsc is "SE" or "ES") return "ES";
        if (normalizedLand.Contains("SCHWE") || normalizedTsc.Contains("CH")) return "CH";

        return normalizedTsc.Replace("TR", string.Empty);
    }
}

public sealed class NetSalesReferenceRow
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal? ActualValue { get; set; }
    public decimal? IntercompanyDeduction { get; set; }
    public decimal? ActualValueExcludingIntercompany { get; set; }
    public decimal? ReferenceValue { get; set; }
    public decimal? Difference { get; set; }
    public decimal? DifferenceExcludingIntercompany { get; set; }
    public int RowCount { get; set; }
    public string Currencies { get; set; } = string.Empty;
    public string ValueField { get; set; } = string.Empty;
    public string ActualCurrency { get; set; } = string.Empty;
    public string ReferenceSource { get; set; } = string.Empty;
    public string ReferenceCurrency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<NetSalesCandidateRow> Candidates { get; set; } = [];
}

public sealed class NetSalesCandidateRow
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal IntercompanyValue { get; set; }
    public decimal ValueExcludingIntercompany { get; set; }
    public decimal? Difference { get; set; }
    public decimal? DifferenceExcludingIntercompany { get; set; }
}

internal sealed class NetSalesActual
{
    public int RowCount { get; set; }
    public string Currencies { get; set; } = string.Empty;
    public List<NetSalesCandidate> Candidates { get; set; } = [];
}

internal sealed record NetSalesActualSourceRow(
    string Land,
    string Tsc,
    int DocumentEntry,
    string InvoiceNumber,
    string DocumentType,
    string CustomerNumber,
    string CustomerName,
    string SalesCurrency,
    string DocumentCurrency,
    string CompanyCurrency,
    decimal SalesPriceValue,
    decimal DocumentTotalForeignCurrency,
    decimal DocumentTotalLocalCurrency,
    decimal VatSumForeignCurrency,
    decimal VatSumLocalCurrency);

internal sealed record NetSalesCandidate(string Key, string Label, string Currency, decimal Value, decimal IntercompanyValue)
{
    public decimal ValueExcludingIntercompany => Value - IntercompanyValue;
}
