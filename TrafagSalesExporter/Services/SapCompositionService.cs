using System.Globalization;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class SapCompositionService : ISapCompositionService
{
    private readonly ISapGatewayService _sapGatewayService;
    private readonly IAppEventLogService _appEventLogService;

    public SapCompositionService(ISapGatewayService sapGatewayService, IAppEventLogService appEventLogService)
    {
        _sapGatewayService = sapGatewayService;
        _appEventLogService = appEventLogService;
    }

    public async Task<List<SalesRecord>> BuildSalesRecordsAsync(
        Site site,
        IReadOnlyList<SapSourceDefinition> sources,
        IReadOnlyList<SapJoinDefinition> joins,
        IReadOnlyList<SapFieldMapping> mappings,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(site.SapServiceUrl))
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine SAP Service URL.");

        var activeSources = sources
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToList();
        if (activeSources.Count == 0)
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine aktiven SAP-Quellen.");

        var primarySource = activeSources.FirstOrDefault(s => s.IsPrimary) ?? activeSources.First();
        var sourceRows = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in activeSources)
        {
            await _appEventLogService.WriteDebugAsync("SAP", "Quelle wird gelesen", site.Id, site.Land,
                $"Alias={source.Alias} | EntitySet={source.EntitySet}");
            var rows = await _sapGatewayService.GetEntityRowsAsync(site.SapServiceUrl, source.EntitySet, username, password, cancellationToken);
            sourceRows[source.Alias] = rows;
            await _appEventLogService.WriteDebugAsync("SAP", "Quelle gelesen", site.Id, site.Land,
                $"Alias={source.Alias} | EntitySet={source.EntitySet} | Zeilen={rows.Count}");
        }

        var composedRows = sourceRows[primarySource.Alias]
            .Select(r => PrefixRow(primarySource.Alias, r))
            .ToList();
        await _appEventLogService.WriteDebugAsync("SAP", "Primärquelle vorbereitet", site.Id, site.Land,
            $"Alias={primarySource.Alias} | Startzeilen={composedRows.Count}");

        foreach (var join in joins.Where(j => j.IsActive).OrderBy(j => j.SortOrder).ThenBy(j => j.Id))
        {
            if (!sourceRows.TryGetValue(join.RightAlias, out var rightRows))
                continue;

            await _appEventLogService.WriteDebugAsync("SAP", "Join gestartet", site.Id, site.Land,
                $"{join.LeftAlias}({join.LeftKeys}) -> {join.RightAlias}({join.RightKeys}) | RightRows={rightRows.Count}");
            composedRows = ApplyLeftJoin(composedRows, join.LeftAlias, join.LeftKeys, join.RightAlias, join.RightKeys, rightRows);
            await _appEventLogService.WriteDebugAsync("SAP", "Join beendet", site.Id, site.Land,
                $"{join.LeftAlias} -> {join.RightAlias} | Ergebniszeilen={composedRows.Count}");
        }

        var result = composedRows
            .Select(row => MapToSalesRecord(site, row, mappings))
            .ToList();
        await _appEventLogService.WriteDebugAsync("SAP", "Mapping ins Zielschema beendet", site.Id, site.Land,
            $"SalesRecords={result.Count} | Mappings={mappings.Count(x => x.IsActive)}");
        return result;
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

    private static SalesRecord MapToSalesRecord(Site site, Dictionary<string, object?> row, IReadOnlyList<SapFieldMapping> mappings)
    {
        var record = new SalesRecord
        {
            ExtractionDate = DateTime.UtcNow,
            Tsc = site.TSC,
            Land = site.Land,
            DocumentType = "SAP"
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

        return record;
    }

    private static object? EvaluateExpression(Dictionary<string, object?> row, string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        var value = expression.Trim();
        if (value.StartsWith('='))
            return value[1..];

        if (row.TryGetValue(value, out var direct))
            return direct;

        return null;
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
                if (int.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
                    property.SetValue(record, intValue);
                return;
            }

            if (property.PropertyType == typeof(decimal))
            {
                if (decimal.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
                    property.SetValue(record, decimalValue);
                return;
            }

            if (property.PropertyType == typeof(DateTime?) || property.PropertyType == typeof(DateTime))
            {
                if (TryParseDate(value?.ToString(), out var date))
                    property.SetValue(record, date);
            }
        }
        catch
        {
            // ignore invalid mappings and continue with remaining fields
        }
    }

    private static bool TryParseDate(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed.StartsWith("/Date(", StringComparison.Ordinal) && trimmed.EndsWith(")/", StringComparison.Ordinal))
        {
            var epochRaw = trimmed[6..^2];
            var separator = epochRaw.IndexOfAny(['+', '-']);
            if (separator > 0)
                epochRaw = epochRaw[..separator];
            if (long.TryParse(epochRaw, out var ms))
            {
                date = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                return true;
            }
        }

        return DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date)
            || DateTime.TryParse(trimmed, CultureInfo.GetCultureInfo("de-CH"), DateTimeStyles.AssumeLocal, out date);
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
