using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using static TrafagSalesExporter.Services.CockpitValueAggregator;

namespace TrafagSalesExporter.Services;

internal sealed class CentralCockpitAnalyzer
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly CockpitValueAggregator _aggregator;

    public CentralCockpitAnalyzer(IDbContextFactory<AppDbContext> dbFactory, CockpitValueAggregator aggregator)
    {
        _dbFactory = dbFactory;
        _aggregator = aggregator;
    }

    public async Task<List<int>> GetAvailableCentralYearsAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var years = await db.CentralSalesRecords
            .Select(r => r.InvoiceDate.HasValue ? r.InvoiceDate.Value.Year : r.ExtractionDate.Year)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return years;
    }

    public async Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month, ManagementCockpitAnalysisOptions? options)
    {
        var aggregation = _aggregator.ResolveAggregation(options);

        using var db = await _dbFactory.CreateDbContextAsync();
        var baseRows = await db.CentralSalesRecords
            .Select(r => new CentralCockpitRow
            {
                SourceSystem = r.SourceSystem,
                Land = r.Land,
                Tsc = r.Tsc,
                InvoiceNumber = r.InvoiceNumber,
                SalesCurrency = string.IsNullOrWhiteSpace(r.SalesCurrency) ? "-" : r.SalesCurrency,
                StandardCostCurrency = string.IsNullOrWhiteSpace(r.StandardCostCurrency) ? "-" : r.StandardCostCurrency,
                Quantity = r.Quantity,
                StandardCost = r.StandardCost,
                SalesValue = r.SalesPriceValue,
                PeriodDate = r.InvoiceDate ?? r.ExtractionDate
            })
            .ToListAsync();

        if (baseRows.Count == 0)
            throw new InvalidOperationException("Die zentrale Tabelle enthält noch keine Datensätze.");

        var aggregatedRows = baseRows
            .Select(row => BuildCentralAggregationRow(row, aggregation))
            .ToList();

        var scopedRows = ApplyCentralDimensionFilters(aggregatedRows, options)
            .ToList();

        var selectedRows = scopedRows
            .Where(r => r.PeriodDate.Year == year && (!month.HasValue || r.PeriodDate.Month == month.Value))
            .ToList();

        if (selectedRows.Count == 0)
            throw new InvalidOperationException("Für den gewählten Zeitraum gibt es keine Datensätze in der zentralen Tabelle.");

        var yearlyRows = scopedRows;

        var dailyBaseRows = selectedRows
            .Where(r => month.HasValue)
            .ToList();

        return new ManagementCockpitCentralResult
        {
            Filter = new ManagementCockpitCentralFilter
            {
                Year = year,
                Month = month,
                ValueField = aggregation.ValueField.Key,
                TargetCurrency = aggregation.TargetCurrency,
                Land = NormalizeOptionalFilter(options?.LandFilter),
                Tsc = NormalizeOptionalFilter(options?.TscFilter)
            },
            Summary = new ManagementCockpitCentralSummary
            {
                RowCount = selectedRows.Count,
                InvoiceCount = selectedRows.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                SiteCount = selectedRows.Select(x => x.Tsc).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                CountryCount = selectedRows.Select(x => x.Land).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                CurrencyCount = selectedRows.Select(x => x.DisplayCurrency).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                ValueFieldKey = aggregation.ValueField.Key,
                ValueFieldLabel = aggregation.ValueField.Label,
                DisplayCurrency = BuildDisplayCurrencyLabel(selectedRows.Select(x => x.DisplayCurrency)),
                ValueTotal = selectedRows.Sum(x => x.Value),
                MissingExchangeRateCount = selectedRows.Count(x => x.MissingExchangeRate),
                PeriodStart = selectedRows.Min(x => x.PeriodDate),
                PeriodEnd = selectedRows.Max(x => x.PeriodDate)
            },
            AdditionalValueFields = aggregation.AdditionalValueFields
                .Select(ToValueFieldOption)
                .ToList(),
            Notices = BuildCentralNotices(aggregation, selectedRows.Count(x => x.MissingExchangeRate), options),
            YearlyTotals = yearlyRows
                .GroupBy(x => new { x.PeriodDate.Year, x.DisplayCurrency })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => BuildTimeValueRow(g, aggregation, g.Key.Year.ToString(), g.Key.Year, null, null, g.Key.DisplayCurrency))
                .ToList(),
            MonthlyTotals = selectedRows
                .GroupBy(x => new { x.PeriodDate.Year, x.PeriodDate.Month, x.DisplayCurrency })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => BuildTimeValueRow(g, aggregation, $"{g.Key.Year:D4}-{g.Key.Month:D2}", g.Key.Year, g.Key.Month, null, g.Key.DisplayCurrency))
                .ToList(),
            DailyTotals = dailyBaseRows
                .GroupBy(x => new { x.PeriodDate.Year, x.PeriodDate.Month, x.PeriodDate.Day, x.DisplayCurrency })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ThenBy(g => g.Key.Day)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => BuildTimeValueRow(g, aggregation, $"{g.Key.Year:D4}-{g.Key.Month:D2}-{g.Key.Day:D2}", g.Key.Year, g.Key.Month, g.Key.Day, g.Key.DisplayCurrency))
                .ToList(),
            SourceSystemTotals = selectedRows
                .GroupBy(x => new { x.SourceSystem, x.DisplayCurrency })
                .OrderBy(g => g.Key.SourceSystem, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ManagementCockpitDimensionValueRow
                {
                    Label = g.Key.SourceSystem,
                    Currency = g.Key.DisplayCurrency,
                    SalesValue = g.Sum(x => x.Value),
                    RowCount = g.Count(),
                    InvoiceCount = g.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                })
                .ToList(),
            CountryTotals = selectedRows
                .GroupBy(x => new { x.Land, x.DisplayCurrency })
                .OrderByDescending(g => g.Sum(x => x.Value))
                .ThenBy(g => g.Key.Land, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Key.DisplayCurrency, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ManagementCockpitDimensionValueRow
                {
                    Label = g.Key.Land,
                    Currency = g.Key.DisplayCurrency,
                    SalesValue = g.Sum(x => x.Value),
                    RowCount = g.Count(),
                    InvoiceCount = g.Select(x => x.InvoiceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                })
                .ToList()
        };
    }

    private static IEnumerable<CentralAggregationRow> ApplyCentralDimensionFilters(
        IEnumerable<CentralAggregationRow> rows,
        ManagementCockpitAnalysisOptions? options)
    {
        var landFilter = NormalizeOptionalFilter(options?.LandFilter);
        var tscFilter = NormalizeOptionalFilter(options?.TscFilter);

        return rows.Where(row =>
            (landFilter is null || string.Equals(row.Land, landFilter, StringComparison.OrdinalIgnoreCase)) &&
            (tscFilter is null || string.Equals(row.Tsc, tscFilter, StringComparison.OrdinalIgnoreCase)));
    }

    private CentralAggregationRow BuildCentralAggregationRow(CentralCockpitRow row, AggregationSelection aggregation)
    {
        var value = ResolveValue(row, aggregation.ValueField);
        var currency = ResolveCurrency(row, aggregation.ValueField);
        var converted = _aggregator.ConvertValue(value, currency, aggregation.ValueField, aggregation, row.PeriodDate);
        var additionalValues = aggregation.AdditionalValueFields.ToDictionary(
            field => field.Key,
            field =>
            {
                var additionalValue = ResolveValue(row, field);
                var additionalCurrency = ResolveCurrency(row, field);
                return _aggregator.ConvertValue(additionalValue, additionalCurrency, field, aggregation, row.PeriodDate);
            },
            StringComparer.OrdinalIgnoreCase);

        return new CentralAggregationRow
        {
            SourceSystem = row.SourceSystem,
            Land = row.Land,
            Tsc = row.Tsc,
            InvoiceNumber = row.InvoiceNumber,
            PeriodDate = row.PeriodDate,
            Value = converted.Value,
            DisplayCurrency = converted.DisplayCurrency,
            MissingExchangeRate = converted.MissingExchangeRate,
            AdditionalValues = additionalValues
        };
    }

    private static decimal ResolveValue(CentralCockpitRow row, ValueFieldDefinition field)
        => field.Key switch
        {
            ManagementCockpitValueFieldKeys.Quantity => row.Quantity,
            ManagementCockpitValueFieldKeys.StandardCost => row.StandardCost,
            ManagementCockpitValueFieldKeys.StandardCostTotal => row.Quantity != 0m ? row.Quantity * row.StandardCost : row.StandardCost,
            _ => row.SalesValue
        };

    private static string ResolveCurrency(CentralCockpitRow row, ValueFieldDefinition field)
        => field.CurrencySource switch
        {
            ValueCurrencySource.StandardCost => row.StandardCostCurrency,
            ValueCurrencySource.Sales => row.SalesCurrency,
            _ => "-"
        };

    private static List<string> BuildCentralNotices(
        AggregationSelection aggregation,
        int missingExchangeRateCount,
        ManagementCockpitAnalysisOptions? options)
    {
        var notices = new List<string>
        {
            "Roh-Auswertung aus CentralSalesRecords.",
            $"Summenfeld: {aggregation.ValueField.Label}.",
            "Keine Intercompany-Bereinigung angewendet.",
            "Kein Budget- und kein Spartemapping angewendet.",
            "Periodenlogik basiert auf Invoice Date, falls vorhanden, sonst auf Extraction Date."
        };

        var landFilter = NormalizeOptionalFilter(options?.LandFilter);
        var tscFilter = NormalizeOptionalFilter(options?.TscFilter);
        if (landFilter is not null || tscFilter is not null)
        {
            notices.Add($"Filter aus Auswahl: Land {(landFilter ?? "alle")}, TSC {(tscFilter ?? "alle")}.");
        }

        if (aggregation.AdditionalValueFields.Count > 0)
            notices.Add($"Weitere Summenfelder: {string.Join(", ", aggregation.AdditionalValueFields.Select(x => x.Label))}.");

        if (!aggregation.ValueField.IsCurrencyAmount)
        {
            notices.Add("Das gewaehlte Summenfeld ist kein Waehrungsbetrag; die Anzeige-Waehrung wird ignoriert.");
        }
        else if (aggregation.TargetCurrency == ManagementCockpitCurrencyOptions.Native)
        {
            notices.Add("Keine Waehrungsumrechnung angewendet; Werte bleiben in der jeweiligen Quellwaehrung.");
        }
        else
        {
            notices.Add($"Betragswerte werden in {aggregation.TargetCurrency} angezeigt.");
            if (missingExchangeRateCount > 0)
                notices.Add($"{missingExchangeRateCount} Zeilen hatten keinen passenden Wechselkurs und sind in den Summen mit 0 enthalten.");
        }

        return notices;
    }

    private static ManagementCockpitTimeValueRow BuildTimeValueRow(
        IEnumerable<CentralAggregationRow> groupRows,
        AggregationSelection aggregation,
        string label,
        int? year,
        int? month,
        int? day,
        string currency)
    {
        var rows = groupRows.ToList();
        return new ManagementCockpitTimeValueRow
        {
            Label = label,
            Year = year,
            Month = month,
            Day = day,
            Currency = currency,
            SalesValue = rows.Sum(x => x.Value),
            AdditionalValues = BuildAdditionalValues(rows, aggregation),
            RowCount = rows.Count
        };
    }

    private static Dictionary<string, ManagementCockpitAggregatedFieldValue> BuildAdditionalValues(
        IReadOnlyCollection<CentralAggregationRow> rows,
        AggregationSelection aggregation)
    {
        var result = new Dictionary<string, ManagementCockpitAggregatedFieldValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in aggregation.AdditionalValueFields)
        {
            var values = rows
                .Select(row => row.AdditionalValues.TryGetValue(field.Key, out var value) ? value : new ConvertedValue(0m, "-", false))
                .ToList();

            result[field.Key] = new ManagementCockpitAggregatedFieldValue
            {
                FieldKey = field.Key,
                Label = field.Label,
                Currency = BuildDisplayCurrencyLabel(values.Select(x => x.DisplayCurrency)),
                Value = values.Sum(x => x.Value),
                MissingExchangeRateCount = values.Count(x => x.MissingExchangeRate)
            };
        }

        return result;
    }

    private class CentralCockpitRow
    {
        public string SourceSystem { get; set; } = string.Empty;
        public string Land { get; set; } = string.Empty;
        public string Tsc { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string SalesCurrency { get; set; } = string.Empty;
        public string StandardCostCurrency { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal StandardCost { get; set; }
        public decimal SalesValue { get; set; }
        public DateTime PeriodDate { get; set; }
    }

    private class CentralAggregationRow
    {
        public string SourceSystem { get; set; } = string.Empty;
        public string Land { get; set; } = string.Empty;
        public string Tsc { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime PeriodDate { get; set; }
        public decimal Value { get; set; }
        public string DisplayCurrency { get; set; } = string.Empty;
        public bool MissingExchangeRate { get; set; }
        public Dictionary<string, ConvertedValue> AdditionalValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
