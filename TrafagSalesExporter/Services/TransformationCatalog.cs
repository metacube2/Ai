namespace TrafagSalesExporter.Services;

public class TransformationCatalog : ITransformationCatalog
{
    private readonly IReadOnlyList<TransformationCatalogItem> _items;

    public TransformationCatalog(IEnumerable<ITransformationStrategy> valueStrategies, IEnumerable<IRecordTransformationStrategy> recordStrategies)
    {
        _items = valueStrategies
            .Select(x => BuildItem(x.TransformationType, "Value", x.Description, x.GetType()))
            .Concat(recordStrategies.Select(x => BuildItem(x.TransformationType, "Record", x.Description, x.GetType())))
            .OrderBy(x => x.RuleScope, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<TransformationCatalogItem> GetAll() => _items;

    public IReadOnlyList<TransformationCatalogItem> GetByScope(string ruleScope)
        => _items
            .Where(x => string.Equals(x.RuleScope, ruleScope, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static TransformationCatalogItem BuildItem(string key, string ruleScope, string description, Type implementationType)
        => new()
        {
            Key = key,
            RuleScope = ruleScope,
            Description = description,
            TypeName = implementationType.Name,
            SourceFile = implementationType == typeof(FirstNonEmptyRecordTransformationStrategy)
                ? "Services/TransformationStrategies.cs"
                : "Services/TransformationStrategies.cs",
            CodeSnippet = GetCodeSnippet(key, ruleScope)
        };

    private static string GetCodeSnippet(string key, string ruleScope)
        => (ruleScope, key) switch
        {
            ("Value", "Copy") => """
                public object? Transform(object? sourceValue, string? argument)
                    => sourceValue;
                """,
            ("Value", "Uppercase") => """
                public object? Transform(object? sourceValue, string? argument)
                    => sourceValue?.ToString()?.ToUpperInvariant();
                """,
            ("Value", "Lowercase") => """
                public object? Transform(object? sourceValue, string? argument)
                    => sourceValue?.ToString()?.ToLowerInvariant();
                """,
            ("Value", "Prefix") => """
                public object? Transform(object? sourceValue, string? argument)
                    => $"{argument}{sourceValue}";
                """,
            ("Value", "Suffix") => """
                public object? Transform(object? sourceValue, string? argument)
                    => $"{sourceValue}{argument}";
                """,
            ("Value", "Replace") => """
                public object? Transform(object? sourceValue, string? argument)
                {
                    var input = sourceValue?.ToString();
                    var parts = argument?.Split("=>", 2, StringSplitOptions.TrimEntries);
                    return parts?.Length == 2
                        ? input?.Replace(parts[0], parts[1], StringComparison.OrdinalIgnoreCase)
                        : input;
                }
                """,
            ("Value", "Constant") => """
                public object? Transform(object? sourceValue, string? argument)
                    => argument;
                """,
            ("Value", "NormalizeCurrencyCode") => """
                public object? Transform(object? sourceValue, string? argument)
                {
                    var input = sourceValue?.ToString()?.Trim();
                    return aliases.TryGetValue(input ?? "", out var mapped)
                        ? mapped
                        : input?.ToUpperInvariant();
                }
                """,
            ("Record", "FirstNonEmpty") => """
                public void Transform(SalesRecord record, FieldTransformationRule rule)
                {
                    var sourceFields = rule.Argument.Split(['|', ',', ';'], StringSplitOptions.TrimEntries);
                    foreach (var sourceField in sourceFields)
                    {
                        var value = sourceProperty.GetValue(record);
                        if (IsMeaningfulValue(value))
                        {
                            SetPropertyValue(record, targetProperty, value);
                            return;
                        }
                    }
                }
                """,
            ("Record", "ConvertCurrency") => """
                public void Transform(SalesRecord record, FieldTransformationRule rule)
                {
                    var options = ParseOptions(rule.Argument);
                    var rate = exchangeRateService.ResolveRate(sourceCurrency, targetCurrency, effectiveDate);
                    if (rate.HasValue)
                        SetPropertyValue(record, targetAmountProperty, sourceAmount * rate.Value);
                }
                """,
            _ => "// Kein Snippet hinterlegt."
        };
}
