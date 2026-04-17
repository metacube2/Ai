using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class TransformationCatalogTests
{
    [Fact]
    public void Catalog_Returns_Value_And_Record_Strategies()
    {
        ITransformationStrategy[] valueStrategies =
        [
            new CopyTransformationStrategy(),
            new ConstantTransformationStrategy(),
            new NormalizeCurrencyCodeTransformationStrategy()
        ];
        IRecordTransformationStrategy[] recordStrategies =
        [
            new FirstNonEmptyRecordTransformationStrategy(),
            new ConvertCurrencyRecordTransformationStrategy(new FakeCurrencyExchangeRateService())
        ];

        var catalog = new TransformationCatalog(valueStrategies, recordStrategies);

        var all = catalog.GetAll();

        Assert.Contains(all, x => x.RuleScope == "Value" && x.Key == "Copy");
        Assert.Contains(all, x => x.RuleScope == "Value" && x.Key == "Constant");
        Assert.Contains(all, x => x.RuleScope == "Value" && x.Key == "NormalizeCurrencyCode");
        Assert.Contains(all, x => x.RuleScope == "Record" && x.Key == "FirstNonEmpty");
        Assert.Contains(all, x => x.RuleScope == "Record" && x.Key == "ConvertCurrency");
    }

    private sealed class FakeCurrencyExchangeRateService : ICurrencyExchangeRateService
    {
        public decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate) => 1m;

        public string NormalizeCurrencyCode(string? currencyCode)
            => currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
