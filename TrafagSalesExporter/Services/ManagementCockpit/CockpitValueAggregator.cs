using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

internal sealed class CockpitValueAggregator
{
    private readonly ICurrencyExchangeRateService _exchangeRateService;

    public CockpitValueAggregator(ICurrencyExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    private static readonly List<ValueFieldDefinition> ValueFieldDefinitions =
    [
        new()
        {
            Key = ManagementCockpitValueFieldKeys.SalesPriceValue,
            Label = "Sales Price/Value",
            IsCurrencyAmount = true,
            CurrencySource = ValueCurrencySource.Sales
        },
        new()
        {
            Key = ManagementCockpitValueFieldKeys.StandardCostTotal,
            Label = "Quantity * Standard cost",
            IsCurrencyAmount = true,
            CurrencySource = ValueCurrencySource.StandardCost
        },
        new()
        {
            Key = ManagementCockpitValueFieldKeys.StandardCost,
            Label = "Standard cost",
            IsCurrencyAmount = true,
            CurrencySource = ValueCurrencySource.StandardCost
        },
        new()
        {
            Key = ManagementCockpitValueFieldKeys.Quantity,
            Label = "Quantity",
            IsCurrencyAmount = false,
            CurrencySource = ValueCurrencySource.None
        }
    ];

    public IReadOnlyList<ManagementCockpitValueFieldOption> GetValueFieldOptions()
        => ValueFieldDefinitions
            .Select(ToValueFieldOption)
            .ToList();

    public AggregationSelection ResolveAggregation(ManagementCockpitAnalysisOptions? options)
    {
        var selectedField = ValueFieldDefinitions.FirstOrDefault(x =>
                string.Equals(x.Key, options?.ValueField, StringComparison.OrdinalIgnoreCase))
            ?? ValueFieldDefinitions.First(x => x.Key == ManagementCockpitValueFieldKeys.SalesPriceValue);

        var additionalFields = (options?.AdditionalValueFields ?? [])
            .Select(key => ValueFieldDefinitions.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)))
            .Where(x => x is not null && !string.Equals(x.Key, selectedField.Key, StringComparison.OrdinalIgnoreCase))
            .Cast<ValueFieldDefinition>()
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var targetCurrency = (options?.TargetCurrency ?? ManagementCockpitCurrencyOptions.Native).Trim().ToUpperInvariant();
        if (targetCurrency is not ManagementCockpitCurrencyOptions.Eur and not ManagementCockpitCurrencyOptions.Usd)
            targetCurrency = ManagementCockpitCurrencyOptions.Native;

        return new AggregationSelection(
            selectedField,
            additionalFields,
            targetCurrency,
            new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase));
    }

    public ConvertedValue ConvertValue(decimal value, string sourceCurrency, ValueFieldDefinition field, AggregationSelection aggregation, DateTime? effectiveDate)
    {
        if (!field.IsCurrencyAmount)
            return new ConvertedValue(value, "-", false);

        var normalizedSource = _exchangeRateService.NormalizeCurrencyCode(sourceCurrency);
        if (string.IsNullOrWhiteSpace(normalizedSource) || normalizedSource == "-")
        {
            normalizedSource = "-";
            if (aggregation.TargetCurrency != ManagementCockpitCurrencyOptions.Native)
                return new ConvertedValue(0m, aggregation.TargetCurrency, true);
        }

        if (aggregation.TargetCurrency == ManagementCockpitCurrencyOptions.Native)
            return new ConvertedValue(value, normalizedSource, false);

        if (string.Equals(normalizedSource, aggregation.TargetCurrency, StringComparison.OrdinalIgnoreCase))
            return new ConvertedValue(value, aggregation.TargetCurrency, false);

        var rateDate = (effectiveDate ?? DateTime.UtcNow).Date;
        var cacheKey = BuildRateCacheKey(normalizedSource, aggregation.TargetCurrency, rateDate);
        if (!aggregation.RateCache.TryGetValue(cacheKey, out var rate))
        {
            rate = _exchangeRateService.ResolveRate(normalizedSource, aggregation.TargetCurrency, rateDate);
            aggregation.RateCache[cacheKey] = rate;
        }

        if (!rate.HasValue)
            return new ConvertedValue(0m, aggregation.TargetCurrency, true);

        return new ConvertedValue(value * rate.Value, aggregation.TargetCurrency, false);
    }

    private static string BuildRateCacheKey(string fromCurrency, string toCurrency, DateTime date)
        => $"{fromCurrency}|{toCurrency}|{date:yyyy-MM-dd}";

    public static string BuildDisplayCurrencyLabel(IEnumerable<string> currencies)
    {
        var distinct = currencies
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinct.Count switch
        {
            0 => "-",
            1 => distinct[0],
            _ => "Mixed"
        };
    }

    public static string? NormalizeOptionalFilter(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static ManagementCockpitValueFieldOption ToValueFieldOption(ValueFieldDefinition field)
        => new()
        {
            Key = field.Key,
            Label = field.Label,
            IsCurrencyAmount = field.IsCurrencyAmount
        };
}

internal sealed record AggregationSelection(
    ValueFieldDefinition ValueField,
    IReadOnlyList<ValueFieldDefinition> AdditionalValueFields,
    string TargetCurrency,
    Dictionary<string, decimal?> RateCache);

internal sealed record ConvertedValue(decimal Value, string DisplayCurrency, bool MissingExchangeRate);

internal sealed class ValueFieldDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCurrencyAmount { get; set; }
    public ValueCurrencySource CurrencySource { get; set; }
}

internal enum ValueCurrencySource
{
    None,
    Sales,
    StandardCost
}
