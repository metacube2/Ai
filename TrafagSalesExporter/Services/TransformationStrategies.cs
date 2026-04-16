using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public sealed class CopyTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Copy";
    public string Description => "Kopiert Source nach Target.";
    public object? Transform(object? sourceValue, string? argument) => sourceValue;
}

public sealed class UppercaseTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Uppercase";
    public string Description => "Wandelt Text in Grossbuchstaben.";
    public object? Transform(object? sourceValue, string? argument) => sourceValue?.ToString()?.ToUpperInvariant();
}

public sealed class LowercaseTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Lowercase";
    public string Description => "Wandelt Text in Kleinbuchstaben.";
    public object? Transform(object? sourceValue, string? argument) => sourceValue?.ToString()?.ToLowerInvariant();
}

public sealed class PrefixTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Prefix";
    public string Description => "Stellt Argument vor den Source-Wert.";
    public object? Transform(object? sourceValue, string? argument) => $"{argument}{sourceValue}";
}

public sealed class SuffixTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Suffix";
    public string Description => "Haengt Argument an den Source-Wert.";
    public object? Transform(object? sourceValue, string? argument) => $"{sourceValue}{argument}";
}

public sealed class ReplaceTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Replace";
    public string Description => "Ersetzt in Text mit Syntax alt=>neu.";

    public object? Transform(object? sourceValue, string? argument)
    {
        var input = sourceValue?.ToString();
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(argument))
            return input;

        var parts = argument.Split("=>", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return input;

        return input.Replace(parts[0], parts[1], StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ConstantTransformationStrategy : ITransformationStrategy
{
    public string TransformationType => "Constant";
    public string Description => "Setzt das Target auf einen konstanten Wert aus Argument.";
    public object? Transform(object? sourceValue, string? argument) => argument;
}

public sealed class FirstNonEmptyRecordTransformationStrategy : IRecordTransformationStrategy
{
    public string TransformationType => "FirstNonEmpty";
    public string Description => "Record-Strategie: setzt Target aus dem ersten nicht-leeren Feld aus Argument, z.B. CustomerName|SupplierName|Name.";

    public void Transform(SalesRecord record, FieldTransformationRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.TargetField) || string.IsNullOrWhiteSpace(rule.Argument))
            return;

        var propertyMap = RecordTransformationService.PropertyMap;
        if (!propertyMap.TryGetValue(rule.TargetField, out var targetProperty))
            return;

        var sourceFields = rule.Argument
            .Split(['|', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var sourceField in sourceFields)
        {
            if (!propertyMap.TryGetValue(sourceField, out var sourceProperty))
                continue;

            var value = sourceProperty.GetValue(record);
            if (IsMeaningfulValue(value))
            {
                RecordTransformationService.SetPropertyValue(record, targetProperty, value);
                return;
            }
        }
    }

    private static bool IsMeaningfulValue(object? value)
    {
        if (value is null)
            return false;
        if (value is string text)
            return !string.IsNullOrWhiteSpace(text);
        if (value is DateTime date)
            return date != default;
        if (value is decimal decimalNumber)
            return decimalNumber != 0m;
        if (value is int intNumber)
            return intNumber != 0;

        return true;
    }
}
