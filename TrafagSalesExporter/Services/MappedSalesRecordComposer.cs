using System.Globalization;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public sealed class MappedSalesRecordComposer : IMappedSalesRecordComposer
{
    public List<SalesRecord> Compose(
        Site site,
        IReadOnlyList<SapSourceDefinition> sources,
        IReadOnlyList<SapJoinDefinition> joins,
        IReadOnlyList<SapFieldMapping> mappings,
        IReadOnlyDictionary<string, List<Dictionary<string, object?>>> sourceRows,
        string defaultDocumentType)
    {
        var activeSources = sources
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToList();

        if (activeSources.Count == 0)
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine aktiven Mapping-Quellen.");
        if (!mappings.Any(m => m.IsActive))
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine aktiven Feldmappings.");

        var primarySource = activeSources.FirstOrDefault(s => s.IsPrimary) ?? activeSources.First();
        if (!sourceRows.TryGetValue(primarySource.Alias, out var primaryRows))
            throw new InvalidOperationException($"Primaerquelle '{primarySource.Alias}' wurde nicht geladen.");

        var composedRows = primaryRows
            .Select(r => PrefixRow(primarySource.Alias, r))
            .ToList();

        foreach (var join in joins.Where(j => j.IsActive).OrderBy(j => j.SortOrder).ThenBy(j => j.Id))
        {
            if (!sourceRows.TryGetValue(join.RightAlias, out var rightRows))
                continue;

            composedRows = ApplyLeftJoin(composedRows, join.LeftAlias, join.LeftKeys, join.RightAlias, join.RightKeys, rightRows);
        }

        return composedRows
            .Select(row => MapToSalesRecord(site, row, mappings, defaultDocumentType))
            .ToList();
    }

    private static Dictionary<string, object?> PrefixRow(string alias, Dictionary<string, object?> row)
        => row.ToDictionary(kvp => $"{alias}.{kvp.Key}", kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

    private static List<Dictionary<string, object?>> ApplyLeftJoin(
        List<Dictionary<string, object?>> leftRows,
        string leftAlias,
        string leftKeys,
        string rightAlias,
        string rightKeys,
        List<Dictionary<string, object?>> rightRows)
    {
        var leftKeyParts = SplitKeys(leftKeys);
        var rightKeyParts = SplitKeys(rightKeys);
        if (leftKeyParts.Count == 0 || leftKeyParts.Count != rightKeyParts.Count)
            return leftRows;

        var rightLookup = rightRows
            .GroupBy(r => BuildKey(r, rightKeyParts))
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var results = new List<Dictionary<string, object?>>();
        foreach (var leftRow in leftRows)
        {
            var leftKey = BuildKey(leftRow, leftAlias, leftKeyParts);
            if (rightLookup.TryGetValue(leftKey, out var matches) && matches.Count > 0)
            {
                foreach (var match in matches)
                {
                    var merged = new Dictionary<string, object?>(leftRow, StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in PrefixRow(rightAlias, match))
                        merged[kvp.Key] = kvp.Value;
                    results.Add(merged);
                }
            }
            else
            {
                results.Add(leftRow);
            }
        }

        return results;
    }

    private static SalesRecord MapToSalesRecord(
        Site site,
        Dictionary<string, object?> row,
        IReadOnlyList<SapFieldMapping> mappings,
        string defaultDocumentType)
    {
        var record = new SalesRecord
        {
            ExtractionDate = DateTime.UtcNow,
            Tsc = site.TSC,
            Land = site.Land,
            DocumentType = defaultDocumentType
        };

        foreach (var mapping in mappings.Where(m => m.IsActive).OrderBy(m => m.SortOrder).ThenBy(m => m.Id))
        {
            var value = EvaluateExpression(row, mapping.SourceExpression);
            ApplyValue(record, mapping.TargetField, value);
        }

        if (record.ExtractionDate == default)
            record.ExtractionDate = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(record.Tsc))
            record.Tsc = site.TSC;
        if (string.IsNullOrWhiteSpace(record.Land))
            record.Land = site.Land;
        if (string.IsNullOrWhiteSpace(record.DocumentType))
            record.DocumentType = defaultDocumentType;

        return record;
    }

    private static object? EvaluateExpression(Dictionary<string, object?> row, string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        var value = expression.Trim();
        if (value.StartsWith('='))
            return value[1..];

        if (TryEvaluateFirstNonEmpty(row, value, out var firstNonEmpty))
            return firstNonEmpty;

        if (row.TryGetValue(value, out var direct))
            return direct;

        return null;
    }

    private static bool TryEvaluateFirstNonEmpty(Dictionary<string, object?> row, string expression, out object? result)
    {
        result = null;
        const string functionName = "FirstNonEmpty";
        if (!expression.StartsWith(functionName, StringComparison.OrdinalIgnoreCase))
            return false;

        var openParen = expression.IndexOf('(');
        var closeParen = expression.LastIndexOf(')');
        if (openParen < functionName.Length || closeParen <= openParen)
            return false;

        var arguments = expression[(openParen + 1)..closeParen]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var argument in arguments)
        {
            var value = EvaluateExpression(row, argument);
            if (!IsEmptyValue(value))
            {
                result = value;
                return true;
            }
        }

        return true;
    }

    private static bool IsEmptyValue(object? value)
    {
        if (value is null)
            return true;

        if (value is string text)
            return string.IsNullOrWhiteSpace(text);

        return string.IsNullOrWhiteSpace(value.ToString());
    }

    private static void ApplyValue(SalesRecord record, string targetField, object? value)
    {
        var property = typeof(SalesRecord).GetProperty(targetField);
        if (property is null)
            return;

        try
        {
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(record, value?.ToString() ?? string.Empty);
                return;
            }

            if (property.PropertyType == typeof(int))
            {
                if (TryConvertInt(value, out var intValue))
                    property.SetValue(record, intValue);
                return;
            }

            if (property.PropertyType == typeof(decimal))
            {
                if (TryConvertDecimal(value, out var decimalValue))
                    property.SetValue(record, decimalValue);
                return;
            }

            if (property.PropertyType == typeof(DateTime?) || property.PropertyType == typeof(DateTime))
            {
                if (TryConvertDate(value, out var date))
                    property.SetValue(record, date);
            }
        }
        catch
        {
            // Invalid field mappings should not stop the remaining row mapping.
        }
    }

    private static bool TryConvertInt(object? value, out int result)
    {
        result = default;
        if (TryConvertDecimal(value, out var decimalValue))
        {
            result = (int)Math.Round(decimalValue);
            return true;
        }

        return false;
    }

    private static bool TryConvertDecimal(object? value, out decimal result)
    {
        result = default;
        if (value is null)
            return false;
        if (value is decimal decimalValue)
        {
            result = decimalValue;
            return true;
        }
        if (value is IConvertible convertible && value is not string)
        {
            try
            {
                result = convertible.ToDecimal(CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                // Fall back to culture-aware string parsing below.
            }
        }

        var text = value.ToString()?.Trim();
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("de-CH"), out result)
            || decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("de-DE"), out result);
    }

    private static bool TryConvertDate(object? value, out DateTime date)
    {
        date = default;
        if (value is null)
            return false;
        if (value is DateTime dateTime)
        {
            date = dateTime;
            return true;
        }
        if (value is DateTimeOffset dateTimeOffset)
        {
            date = dateTimeOffset.DateTime;
            return true;
        }

        var text = value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.StartsWith("/Date(", StringComparison.Ordinal) && text.EndsWith(")/", StringComparison.Ordinal))
        {
            var epochRaw = text[6..^2];
            var separator = epochRaw.IndexOfAny(['+', '-']);
            if (separator > 0)
                epochRaw = epochRaw[..separator];
            if (long.TryParse(epochRaw, out var ms))
            {
                date = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                return true;
            }
        }

        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date)
            || DateTime.TryParse(text, CultureInfo.GetCultureInfo("de-CH"), DateTimeStyles.AssumeLocal, out date)
            || DateTime.TryParse(text, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.AssumeLocal, out date);
    }

    private static string BuildKey(Dictionary<string, object?> row, IReadOnlyList<string> keys)
        => string.Join("||", keys.Select(k => NormalizeKeyValue(row.TryGetValue(k, out var value) ? value : null)));

    private static string BuildKey(Dictionary<string, object?> row, string alias, IReadOnlyList<string> keys)
        => string.Join("||", keys.Select(k =>
        {
            row.TryGetValue($"{alias}.{k}", out var value);
            return NormalizeKeyValue(value);
        }));

    private static string NormalizeKeyValue(object? value) => value?.ToString()?.Trim() ?? string.Empty;

    private static List<string> SplitKeys(string keys)
        => keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
