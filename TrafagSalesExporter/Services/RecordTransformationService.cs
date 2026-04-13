using System.Reflection;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class RecordTransformationService : IRecordTransformationService
{
    private static readonly Dictionary<string, PropertyInfo> PropertyMap = typeof(SalesRecord)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyDictionary<string, ITransformationStrategy> _strategies;

    public RecordTransformationService(IEnumerable<ITransformationStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.TransformationType, StringComparer.OrdinalIgnoreCase);
    }

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

    private void ApplyRule(SalesRecord record, FieldTransformationRule rule)
    {
        if (!PropertyMap.TryGetValue(rule.SourceField, out var sourceProp)) return;
        if (!PropertyMap.TryGetValue(rule.TargetField, out var targetProp)) return;

        var sourceValue = sourceProp.GetValue(record);
        object? result = _strategies.TryGetValue(rule.TransformationType, out var strategy)
            ? strategy.Transform(sourceValue, rule.Argument)
            : sourceValue;

        SetPropertyValue(record, targetProp, result);
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
