using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using static TrafagSalesExporter.Services.CockpitValueAggregator;

namespace TrafagSalesExporter.Services;

internal sealed class FinanceSummaryAnalyzer
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FinanceSummaryAnalyzer(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ManagementFinanceSummaryResult> AnalyzeFinanceSummaryAsync(int year, string? countryKey, string? currency)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var financeRules = await db.FinanceRules
            .AsNoTracking()
            .Where(rule => rule.IsActive)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.Id)
            .ToListAsync();
        if (financeRules.Count == 0)
            financeRules = FinanceRuleEngine.CreateDefaultRules().ToList();

        var financeRuleEngine = new FinanceRuleEngine(financeRules);
        var records = await db.CentralSalesRecords
            .AsNoTracking()
            .Select(r => new SalesRecord
            {
                Land = r.Land,
                Tsc = r.Tsc,
                DocumentEntry = r.DocumentEntry,
                InvoiceNumber = r.InvoiceNumber,
                PositionOnInvoice = r.PositionOnInvoice,
                Material = r.Material,
                Name = r.Name,
                Quantity = r.Quantity,
                SupplierCountry = r.SupplierCountry,
                CustomerNumber = r.CustomerNumber,
                CustomerName = r.CustomerName,
                SalesCurrency = r.SalesCurrency,
                DocumentCurrency = r.DocumentCurrency,
                CompanyCurrency = r.CompanyCurrency,
                SalesPriceValue = r.SalesPriceValue,
                DocumentType = r.DocumentType,
                PostingDate = r.PostingDate,
                InvoiceDate = r.InvoiceDate,
                ExtractionDate = r.ExtractionDate
            })
            .ToListAsync();

        if (records.Count == 0)
            throw new InvalidOperationException("Die zentrale Tabelle enthaelt noch keine Datensaetze.");

        var allRows = records
            .Select(record =>
            {
                var resolvedCountryKey = ResolveFinanceCountryKey(record.Land, record.Tsc);
                var financeDate = financeRuleEngine.ResolveFinanceDate(record, resolvedCountryKey);
                var rawInclude = financeRuleEngine.ShouldInclude(record, resolvedCountryKey);
                var value = financeRuleEngine.ResolveNetSalesActual(record, resolvedCountryKey, rawInclude);
                var include = rawInclude && value != 0m;
                return new FinanceAggregationRow
                {
                    Year = financeDate.Year,
                    CountryKey = resolvedCountryKey,
                    Currency = ResolveFinanceCurrency(record),
                    Include = include,
                    Value = value
                };
            })
            .ToList();

        var yearOptions = allRows
            .Select(row => row.Year)
            .Distinct()
            .OrderBy(yearValue => yearValue)
            .ToList();
        if (year == 0)
            year = yearOptions.LastOrDefault();

        var countryFilter = NormalizeOptionalFilter(countryKey);
        var currencyFilter = NormalizeOptionalFilter(currency);
        var scopedRows = allRows
            .Where(row => row.Year == year)
            .Where(row => countryFilter is null || row.CountryKey.Equals(countryFilter, StringComparison.OrdinalIgnoreCase))
            .Where(row => currencyFilter is null || row.Currency.Equals(currencyFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var summaryRows = scopedRows
            .GroupBy(row => new { row.Year, row.CountryKey, row.Currency })
            .OrderBy(group => group.Key.CountryKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.Key.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildFinanceSummaryRow(group.Key.Year, group.Key.CountryKey, group.Key.Currency, group))
            .ToList();

        var yearRows = allRows
            .Where(row => countryFilter is null || row.CountryKey.Equals(countryFilter, StringComparison.OrdinalIgnoreCase))
            .Where(row => currencyFilter is null || row.Currency.Equals(currencyFilter, StringComparison.OrdinalIgnoreCase))
            .GroupBy(row => new { row.Year, row.Currency })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildFinanceSummaryRow(group.Key.Year, "Alle", group.Key.Currency, group))
            .ToList();

        var includedRows = scopedRows.Count(row => row.Include);
        var excludedRows = scopedRows.Count(row => !row.Include);
        var resultCurrencies = summaryRows
            .Select(row => row.Currency)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var notices = new List<string>
        {
            "Diese Sicht verwendet dieselbe FinanceRuleEngine wie das zentrale Excel-Blatt Finance Summary.",
            "Jahr, Land und Waehrung werden auf das Endergebnis angewendet.",
            "Finance-Jahr basiert auf PostingDate, danach InvoiceDate, danach ExtractionDate; DE-Regeln koennen das Jahr erzwingen.",
            "Include/Exclude, Gutschriften-Negierung und IT-Deduplizierung folgen den gepflegten Finance Regeln."
        };
        if (scopedRows.Count == 0)
        {
            notices.Insert(0, "Fuer die gewaehlten Finance-Filter gibt es keine Datensaetze im aktuellen Zentraldatenbestand.");
        }

        return new ManagementFinanceSummaryResult
        {
            Filter = new ManagementFinanceSummaryFilter
            {
                Year = year,
                CountryKey = countryFilter,
                Currency = currencyFilter
            },
            YearOptions = yearOptions,
            CountryOptions = allRows
                .Select(row => row.CountryKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CurrencyOptions = allRows
                .Select(row => row.Currency)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Rows = summaryRows,
            YearRows = yearRows,
            IncludedRows = includedRows,
            ExcludedRows = excludedRows,
            CountryCount = summaryRows.Select(row => row.CountryKey).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            CurrencyCount = resultCurrencies.Count,
            NetSalesActual = summaryRows.Sum(row => row.NetSalesActual),
            DisplayCurrency = BuildDisplayCurrencyLabel(resultCurrencies),
            Notices = notices
        };
    }

    private static ManagementFinanceSummaryRow BuildFinanceSummaryRow(
        int year,
        string countryKey,
        string currency,
        IEnumerable<FinanceAggregationRow> rows)
    {
        var rowList = rows.ToList();
        return new ManagementFinanceSummaryRow
        {
            Year = year,
            CountryKey = countryKey,
            Currency = currency,
            IncludedRows = rowList.Count(row => row.Include),
            ExcludedRows = rowList.Count(row => !row.Include),
            NetSalesActual = rowList.Sum(row => row.Value)
        };
    }

    private static string ResolveFinanceCurrency(SalesRecord record)
        => ResolveFinanceCountryKey(record.Land, record.Tsc) switch
        {
            "CH" => "CHF",
            "AT" => "EUR",
            "DE" => "EUR",
            "ES" => "EUR",
            "FR" => "EUR",
            "IN" => "INR",
            "IT" => "EUR",
            "UK" => "GBP",
            "US" => "USD",
            _ => string.IsNullOrWhiteSpace(record.CompanyCurrency) ? record.SalesCurrency : record.CompanyCurrency
        };

    private static string ResolveFinanceCountryKey(string land, string tsc)
    {
        var normalizedLand = (land ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedTsc = (tsc ?? string.Empty).Trim().ToUpperInvariant();

        if (normalizedLand is "AT" or "AUT" || normalizedLand.Contains("OESTER") || normalizedLand.Contains("OSTER") || normalizedLand.Contains("AUSTRIA")) return "AT";
        if (normalizedLand is "CH" or "CHE" || normalizedLand.Contains("SCHWE") || normalizedLand.Contains("SWITZER")) return "CH";
        if (normalizedLand.Contains("FRANK") || normalizedTsc.Contains("FR")) return "FR";
        if (normalizedLand.Contains("IND") || normalizedTsc.Contains("IN")) return "IN";
        if (normalizedLand.Contains("ITAL") || normalizedTsc.Contains("IT")) return "IT";
        if (normalizedLand.Contains("ENGL") || normalizedLand.Contains("KINGDOM") || normalizedTsc.Contains("UK") || normalizedTsc.Contains("GB")) return "UK";
        if (normalizedLand.Contains("USA") || normalizedLand.Contains("UNITED STATES") || normalizedTsc.Contains("US")) return "US";
        if (normalizedLand.Contains("DEUT") || normalizedTsc.Contains("DE")) return "DE";
        if (normalizedLand.Contains("SPAN") || normalizedTsc is "SE" or "ES") return "ES";

        return normalizedTsc.Replace("TR", string.Empty);
    }

    private class FinanceAggregationRow
    {
        public int Year { get; set; }
        public string CountryKey { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public bool Include { get; set; }
        public decimal Value { get; set; }
    }
}
