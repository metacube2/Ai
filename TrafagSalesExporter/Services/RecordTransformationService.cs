using System.Reflection;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class RecordTransformationService
{
    private static readonly Dictionary<string, PropertyInfo> PropertyMap = typeof(SalesRecord)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public void Apply(List<SalesRecord> records, IEnumerable<FieldTransformationRule> rules)
    {
        var orderedRules = rules.Where(r => r.IsActive).OrderBy(r => r.SortOrder).ToList();
        if (orderedRules.Count == 0 || records.Count == 0) return;

        foreach (var record in records)
        {
            foreach (var rule in orderedRules)
            {
                ApplyRule(record, rule);
            }
        }
    }

    private static void ApplyRule(SalesRecord record, FieldTransformationRule rule)
    {
        if (!PropertyMap.TryGetValue(rule.SourceField, out var sourceProp)) return;
        if (!PropertyMap.TryGetValue(rule.TargetField, out var targetProp)) return;

        var sourceValue = sourceProp.GetValue(record);
        object? result = rule.TransformationType switch
        {
            "Copy" => sourceValue,
            "Uppercase" => sourceValue?.ToString()?.ToUpperInvariant(),
            "Lowercase" => sourceValue?.ToString()?.ToLowerInvariant(),
            "Prefix" => $"{rule.Argument}{sourceValue}",
            "Suffix" => $"{sourceValue}{rule.Argument}",
            "Replace" => ApplyReplace(sourceValue?.ToString(), rule.Argument),
            "Constant" => rule.Argument,
            _ => sourceValue
        };

        SetPropertyValue(record, targetProp, result);
    }

    private static string ApplyReplace(string? input, string? argument)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        if (string.IsNullOrWhiteSpace(argument)) return input;

        var parts = argument.Split("=>", 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return input;
        return input.Replace(parts[0], parts[1], StringComparison.OrdinalIgnoreCase);
    }

    private static void SetPropertyValue(SalesRecord record, PropertyInfo property, object? value)
    {
        try
        {
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(record, value?.ToString() ?? string.Empty);
                return;
            }

            if (property.PropertyType == typeof(int))
            {
                if (int.TryParse(value?.ToString(), out var parsedInt)) property.SetValue(record, parsedInt);
                return;
            }

            if (property.PropertyType == typeof(decimal))
            {
                if (decimal.TryParse(value?.ToString(), out var parsedDecimal)) property.SetValue(record, parsedDecimal);
                return;
            }

            if (property.PropertyType == typeof(DateTime?) || property.PropertyType == typeof(DateTime))
            {
                if (DateTime.TryParse(value?.ToString(), out var parsedDate)) property.SetValue(record, parsedDate);
                return;
            }

            property.SetValue(record, value);
        }
        catch
        {
            // skip invalid conversion to keep export running
        }
    }
}
