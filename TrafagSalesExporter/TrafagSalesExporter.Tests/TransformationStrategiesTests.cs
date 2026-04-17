using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class TransformationStrategiesTests
{
    [Fact]
    public void ReplaceStrategy_Replaces_Text_Using_Argument_Syntax()
    {
        var strategy = new ReplaceTransformationStrategy();

        var result = strategy.Transform("Intercompany Kunde", "Intercompany=>Extern");

        Assert.Equal("Extern Kunde", result);
    }

    [Fact]
    public void ConstantStrategy_Returns_Argument_Ignoring_SourceValue()
    {
        var strategy = new ConstantTransformationStrategy();

        var result = strategy.Transform("ignored", "CHF");

        Assert.Equal("CHF", result);
    }

    [Fact]
    public void FirstNonEmptyRecordStrategy_Uses_First_Non_Empty_Field_From_Argument_List()
    {
        var strategy = new FirstNonEmptyRecordTransformationStrategy();
        var record = new SalesRecord
        {
            CustomerName = "",
            SupplierName = "Fallback Supplier",
            Name = "Article Name"
        };
        var rule = new FieldTransformationRule
        {
            RuleScope = "Record",
            TargetField = nameof(SalesRecord.CustomerName),
            TransformationType = "FirstNonEmpty",
            Argument = $"{nameof(SalesRecord.CustomerName)}|{nameof(SalesRecord.SupplierName)}|{nameof(SalesRecord.Name)}"
        };

        strategy.Transform(record, rule);

        Assert.Equal("Fallback Supplier", record.CustomerName);
    }

    [Fact]
    public void NormalizeCurrencyCodeStrategy_Uses_BuiltIn_And_Custom_Aliases()
    {
        var strategy = new NormalizeCurrencyCodeTransformationStrategy();

        var normalizedDollar = strategy.Transform("$", null);
        var normalizedRupee = strategy.Transform("rs", null);
        var normalizedCustom = strategy.Transform("fr.", "fr.=>CHF");

        Assert.Equal("USD", normalizedDollar);
        Assert.Equal("INR", normalizedRupee);
        Assert.Equal("CHF", normalizedCustom);
    }

    [Fact]
    public void ConvertCurrencyRecordStrategy_Converts_Amount_And_Updates_Target_Currency()
    {
        var exchangeRateService = new FakeCurrencyExchangeRateService(rate: 0.95m);
        var strategy = new ConvertCurrencyRecordTransformationStrategy(exchangeRateService);
        var record = new SalesRecord
        {
            SalesPriceValue = 100m,
            SalesCurrency = "CHF",
            InvoiceDate = new DateTime(2026, 4, 17)
        };
        var rule = new FieldTransformationRule
        {
            RuleScope = "Record",
            TargetField = nameof(SalesRecord.SalesPriceValue),
            TransformationType = "ConvertCurrency",
            Argument = "amountField=SalesPriceValue;currencyField=SalesCurrency;targetCurrency=EUR;dateField=InvoiceDate;targetCurrencyField=SalesCurrency;round=2"
        };

        strategy.Transform(record, rule);

        Assert.Equal("CHF", exchangeRateService.LastFromCurrency);
        Assert.Equal("EUR", exchangeRateService.LastToCurrency);
        Assert.Equal(new DateTime(2026, 4, 17), exchangeRateService.LastEffectiveDate);
        Assert.Equal(95.00m, record.SalesPriceValue);
        Assert.Equal("EUR", record.SalesCurrency);
    }

    private sealed class FakeCurrencyExchangeRateService : ICurrencyExchangeRateService
    {
        private readonly decimal? _rate;

        public FakeCurrencyExchangeRateService(decimal? rate)
        {
            _rate = rate;
        }

        public string LastFromCurrency { get; private set; } = string.Empty;
        public string LastToCurrency { get; private set; } = string.Empty;
        public DateTime? LastEffectiveDate { get; private set; }

        public decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate)
        {
            LastFromCurrency = fromCurrency;
            LastToCurrency = toCurrency;
            LastEffectiveDate = effectiveDate;
            return _rate;
        }

        public string NormalizeCurrencyCode(string? currencyCode)
        {
            var trimmed = currencyCode?.Trim() ?? string.Empty;
            return trimmed switch
            {
                "$" => "USD",
                "SFR" => "CHF",
                _ => trimmed.ToUpperInvariant()
            };
        }
    }
}
