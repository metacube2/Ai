using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public interface IFinanceReconciliationService
{
    Task<List<NetSalesReferenceRow>> BuildNetSalesReferenceRowsAsync(int year = 2025);
}

public sealed class FinanceReconciliationService : IFinanceReconciliationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    private static readonly IReadOnlyList<NetSalesReferenceDefinition> NetSalesReferences =
    [
        new("AT", "Trafag AT", 3443863m, null),
        new("CH", "Trafag CH", null, null),
        new("CN", "Trafag CN", null, null),
        new("CZ", "Trafag CZ", 95458782m, null),
        new("DE", "Trafag DE", 3635923m, null),
        new("ES", "Trafag ES", 3102334m, null),
        new("FR", "Trafag FR", 1450582m, 1471218m),
        new("GFS", "Trafag GfS", 6495513m, null),
        new("IN", "Trafag IN", 747341702m, 750936591m),
        new("IT", "Trafag IT", 7669840m, null),
        new("JP", "Trafag JP", 187739814m, null),
        new("MS", "Trafag MS", 1850199m, null),
        new("MSA", "Trafag MSA", 1445258m, null),
        new("PL", "Trafag PL Poltraf", 11279297m, null),
        new("RU", "Rrafag RU", null, null),
        new("UK", "Trafag UK", 3538972m, 3749865m),
        new("US", "Traga US", 3896728m, 3749865m)
    ];

    private static readonly IReadOnlyDictionary<string, decimal> BudgetRatesToChf = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
    {
        ["CHF"] = 1m,
        ["USD"] = 0.85m,
        ["EUR"] = 0.95m,
        ["GBP"] = 1.13m,
        ["CNY"] = 1m / 8.50m,
        ["INR"] = 1m / 90.91m,
        ["CZK"] = 1m / 25.64m,
        ["PLN"] = 0.22m,
        ["JPY"] = 1m / 156.25m
    };

    public FinanceReconciliationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<NetSalesReferenceRow>> BuildNetSalesReferenceRowsAsync(int year = 2025)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

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
                BuildNetSalesActual,
                StringComparer.OrdinalIgnoreCase);

        var activeSiteKeys = (await db.Sites
                .AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => new { s.Land, s.TSC })
                .ToListAsync())
            .Select(s => ResolveReferenceKey(s.Land, s.TSC))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return NetSalesReferences
            .Where(reference => activeSiteKeys.Contains(reference.Key) || groupedActuals.ContainsKey(reference.Key))
            .Select(reference =>
            {
                groupedActuals.TryGetValue(reference.Key, out var actual);
                var referenceValue = reference.PowerBiValue ?? reference.LocalCurrencyValue;
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
                    ReferenceCurrency = reference.PowerBiValue.HasValue ? "Sollwert" : "LC",
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
            })
            .OrderBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static NetSalesActual BuildNetSalesActual(IEnumerable<NetSalesActualSourceRow> rows)
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
                rowList.Where(IsLikelyIntercompanyCustomer).Sum(row => row.SalesPriceValue))
        };

        var netDocumentForeignCurrency = documentRows.Sum(row => row.DocumentTotalForeignCurrency - row.VatSumForeignCurrency);
        if (netDocumentForeignCurrency != 0m)
            candidates.Add(new(
                "NetDocumentForeignCurrency",
                "DocTotalFC - VatSumFC",
                ResolveCurrencyLabel(rowList.Select(row => row.DocumentCurrency)),
                netDocumentForeignCurrency,
                documentRows.Where(IsLikelyIntercompanyCustomer).Sum(row => row.DocumentTotalForeignCurrency - row.VatSumForeignCurrency)));

        var netDocumentLocalCurrency = documentRows.Sum(row => row.DocumentTotalLocalCurrency - row.VatSumLocalCurrency);
        if (netDocumentLocalCurrency != 0m)
            candidates.Add(new(
                "NetDocumentLocalCurrency",
                "Nettofakturawert Hauswaehrung",
                ResolveCurrencyLabel(rowList.Select(row => row.CompanyCurrency)),
                netDocumentLocalCurrency,
                documentRows.Where(IsLikelyIntercompanyCustomer).Sum(row => row.DocumentTotalLocalCurrency - row.VatSumLocalCurrency)));

        var budgetChf = documentRows.Sum(row => ConvertHouseCurrencyNetToBudgetChf(row, row.DocumentTotalLocalCurrency - row.VatSumLocalCurrency));
        if (budgetChf != 0m)
            candidates.Add(new(
                "NetDocumentLocalCurrencyBudgetChf",
                "Nettofakturawert Hauswaehrung -> CHF Budget 2025",
                "CHF",
                budgetChf,
                documentRows.Where(IsLikelyIntercompanyCustomer).Sum(row => ConvertHouseCurrencyNetToBudgetChf(row, row.DocumentTotalLocalCurrency - row.VatSumLocalCurrency))));

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

    private static decimal ConvertHouseCurrencyNetToBudgetChf(NetSalesActualSourceRow row, decimal value)
    {
        var currency = (row.CompanyCurrency ?? string.Empty).Trim().ToUpperInvariant();
        return BudgetRatesToChf.TryGetValue(currency, out var rate)
            ? value * rate
            : 0m;
    }

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

    private static bool IsLikelyIntercompanyCustomer(NetSalesActualSourceRow row)
    {
        var customerNumber = row.CustomerNumber?.Trim() ?? string.Empty;
        var customerName = row.CustomerName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(customerNumber) && string.IsNullOrWhiteSpace(customerName))
            return false;

        var normalizedCustomerName = customerName
            .Replace("ä", "ae", StringComparison.OrdinalIgnoreCase)
            .Replace("ö", "oe", StringComparison.OrdinalIgnoreCase)
            .Replace("ü", "ue", StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();

        if (normalizedCustomerName.Contains("TRAFAG", StringComparison.OrdinalIgnoreCase) ||
            normalizedCustomerName.Contains("MAGNETIC SENSE", StringComparison.OrdinalIgnoreCase) ||
            normalizedCustomerName.Contains("MAGNETS SENSE", StringComparison.OrdinalIgnoreCase) ||
            normalizedCustomerName.Contains("GESELLSCHAFT FUER SENSORIK", StringComparison.OrdinalIgnoreCase) ||
            normalizedCustomerName.Contains("GESELLSCHAFT FUR SENSORIK", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (row.Tsc.Equals("TRIT", StringComparison.OrdinalIgnoreCase))
        {
            return customerNumber.Equals("C_IT01_0306794", StringComparison.OrdinalIgnoreCase) ||
                   customerNumber.Equals("C_CH01_0302179", StringComparison.OrdinalIgnoreCase) ||
                   customerName.Equals("TRAFAG ITALIA S.R.L.", StringComparison.OrdinalIgnoreCase) ||
                   customerName.Equals("Trafag AG", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

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

internal sealed record NetSalesReferenceDefinition(
    string Key,
    string Label,
    decimal? LocalCurrencyValue,
    decimal? PowerBiValue);

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
