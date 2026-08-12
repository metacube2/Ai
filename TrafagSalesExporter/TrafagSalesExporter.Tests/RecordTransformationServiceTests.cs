using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class RecordTransformationServiceTests
{
    private readonly RecordTransformationService _service;

    public RecordTransformationServiceTests()
    {
        ITransformationStrategy[] valueStrategies =
        [
            new CopyTransformationStrategy(),
            new UppercaseTransformationStrategy(),
            new LowercaseTransformationStrategy(),
            new PrefixTransformationStrategy(),
            new SuffixTransformationStrategy(),
            new ReplaceTransformationStrategy(),
            new ConstantTransformationStrategy()
        ];
        IRecordTransformationStrategy[] recordStrategies =
        [
            new FirstNonEmptyRecordTransformationStrategy(),
            new ConvertCurrencyRecordTransformationStrategy(new FakeCurrencyExchangeRateService())
        ];

        _service = new RecordTransformationService(valueStrategies, recordStrategies);
    }

    [Fact]
    public void Apply_Ignores_Inactive_Rules()
    {
        var records = new List<SalesRecord>
        {
            new() { Material = "abc" }
        };
        var rules = new[]
        {
            new FieldTransformationRule
            {
                IsActive = false,
                RuleScope = "Value",
                SourceField = nameof(SalesRecord.Material),
                TargetField = nameof(SalesRecord.Material),
                TransformationType = "Uppercase",
                SortOrder = 10
            }
        };

        _service.Apply(records, rules);

        Assert.Equal("abc", records[0].Material);
    }

    [Fact]
    public void Apply_Uses_SortOrder_For_Multiple_Value_Rules()
    {
        var records = new List<SalesRecord>
        {
            new() { Material = "abc" }
        };
        var rules = new[]
        {
            new FieldTransformationRule
            {
                IsActive = true,
                RuleScope = "Value",
                SourceField = nameof(SalesRecord.Material),
                TargetField = nameof(SalesRecord.Material),
                TransformationType = "Uppercase",
                SortOrder = 20
            },
            new FieldTransformationRule
            {
                IsActive = true,
                RuleScope = "Value",
                SourceField = nameof(SalesRecord.Material),
                TargetField = nameof(SalesRecord.Material),
                TransformationType = "Prefix",
                Argument = "X-",
                SortOrder = 10
            }
        };

        _service.Apply(records, rules);

        Assert.Equal("X-ABC", records[0].Material);
    }

    [Fact]
    public void Apply_Uses_Record_Strategy_When_RuleScope_Is_Record()
    {
        var records = new List<SalesRecord>
        {
            new()
            {
                CustomerName = "",
                SupplierName = "Supplier A",
                Name = "Name A"
            }
        };
        var rules = new[]
        {
            new FieldTransformationRule
            {
                IsActive = true,
                RuleScope = "Record",
                TargetField = nameof(SalesRecord.CustomerName),
                TransformationType = "FirstNonEmpty",
                Argument = $"{nameof(SalesRecord.CustomerName)}|{nameof(SalesRecord.SupplierName)}|{nameof(SalesRecord.Name)}",
                SortOrder = 10
            }
        };

        _service.Apply(records, rules);

        Assert.Equal("Supplier A", records[0].CustomerName);
    }

    [Fact]
    public void Apply_Converts_Value_To_Target_Type()
    {
        var records = new List<SalesRecord>
        {
            new() { Material = "42" }
        };
        var rules = new[]
        {
            new FieldTransformationRule
            {
                IsActive = true,
                RuleScope = "Value",
                SourceField = nameof(SalesRecord.Material),
                TargetField = nameof(SalesRecord.PositionOnInvoice),
                TransformationType = "Copy",
                SortOrder = 10
            }
        };

        _service.Apply(records, rules);

        Assert.Equal(42, records[0].PositionOnInvoice);
    }

    [Fact]
    public void Apply_Uses_ConvertCurrency_Record_Strategy()
    {
        var records = new List<SalesRecord>
        {
            new()
            {
                SalesPriceValue = 100m,
                SalesCurrency = "CHF",
                InvoiceDate = new DateTime(2026, 4, 17)
            }
        };
        var rules = new[]
        {
            new FieldTransformationRule
            {
                IsActive = true,
                RuleScope = "Record",
                TargetField = nameof(SalesRecord.SalesPriceValue),
                TransformationType = "ConvertCurrency",
                Argument = "amountField=SalesPriceValue;currencyField=SalesCurrency;targetCurrency=EUR;dateField=InvoiceDate;targetCurrencyField=SalesCurrency",
                SortOrder = 10
            }
        };

        _service.Apply(records, rules);

        Assert.Equal(95m, records[0].SalesPriceValue);
        Assert.Equal("EUR", records[0].SalesCurrency);
    }

    private sealed class FakeCurrencyExchangeRateService : ICurrencyExchangeRateService
    {
        public decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate) => 0.95m;

        public string NormalizeCurrencyCode(string? currencyCode)
            => currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
