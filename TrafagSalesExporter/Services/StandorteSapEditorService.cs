using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IStandorteSapEditorService
{
    void AddSapSource(List<SapSourceDefinition> sapSources, List<string> sapEntitySetsCache);
    void RemoveSapSource(List<SapSourceDefinition> sapSources, SapSourceDefinition source);
    void AddSapJoin(List<SapJoinDefinition> sapJoins);
    SapAutoMatchResult AutoMatchSapJoins(List<SapSourceDefinition> sapSources, List<SapJoinDefinition> sapJoins, Dictionary<string, List<string>> sapSourceFieldMap);
    void RemoveSapJoin(List<SapJoinDefinition> sapJoins, SapJoinDefinition join);
    void AddSapMapping(List<SapFieldMapping> sapMappings, IReadOnlyList<string> salesRecordFields, List<string> sapAvailableSourceExpressions);
    void RemoveSapMapping(List<SapFieldMapping> sapMappings, SapFieldMapping mapping);
    List<string> BuildSourceExpressionsFromMappings(List<SapFieldMapping> sapMappings);
    Dictionary<string, List<string>> BuildSourceFieldMapFromJoins(List<SapJoinDefinition> sapJoins);
    IEnumerable<string> GetSapAliases(List<SapSourceDefinition> sapSources);
    IEnumerable<string> GetAvailableSourceExpressions(List<string> sapAvailableSourceExpressions, string? currentValue);
    IEnumerable<string> GetAvailableJoinFields(Dictionary<string, List<string>> sapSourceFieldMap, string? alias, string? currentKeys);
    void NormalizeSapConfigCollections(List<SapSourceDefinition> sapSources, List<SapJoinDefinition> sapJoins, List<SapFieldMapping> sapMappings);
}

public sealed class StandorteSapEditorService : IStandorteSapEditorService
{
    public void AddSapSource(List<SapSourceDefinition> sapSources, List<string> sapEntitySetsCache)
    {
        sapSources.Add(new SapSourceDefinition
        {
            Alias = $"SRC{sapSources.Count + 1}",
            EntitySet = sapEntitySetsCache.FirstOrDefault() ?? string.Empty,
            IsActive = true,
            IsPrimary = sapSources.Count == 0,
            SortOrder = sapSources.Count
        });
    }

    public void RemoveSapSource(List<SapSourceDefinition> sapSources, SapSourceDefinition source)
        => sapSources.Remove(source);

    public void AddSapJoin(List<SapJoinDefinition> sapJoins)
    {
        sapJoins.Add(new SapJoinDefinition
        {
            JoinType = "Left",
            IsActive = true,
            SortOrder = sapJoins.Count
        });
    }

    public SapAutoMatchResult AutoMatchSapJoins(List<SapSourceDefinition> sapSources, List<SapJoinDefinition> sapJoins, Dictionary<string, List<string>> sapSourceFieldMap)
    {
        var activeSources = sapSources
            .Where(s => s.IsActive && !string.IsNullOrWhiteSpace(s.Alias))
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToList();

        if (activeSources.Count < 2)
            return SapAutoMatchResult.WarningResult("Fuer Auto-Match werden mindestens zwei aktive SAP-Quellen benoetigt.");

        if (sapSourceFieldMap.Count == 0)
            return SapAutoMatchResult.WarningResult("Bitte zuerst 'Felder aus Quellen laden' ausfuehren.");

        var primary = activeSources.FirstOrDefault(s => s.IsPrimary) ?? activeSources.First();
        var createdOrUpdated = 0;

        foreach (var source in activeSources.Where(s => !string.Equals(s.Alias, primary.Alias, StringComparison.OrdinalIgnoreCase)))
        {
            if (!sapSourceFieldMap.TryGetValue(primary.Alias, out var leftFields) || leftFields.Count == 0)
                continue;
            if (!sapSourceFieldMap.TryGetValue(source.Alias, out var rightFields) || rightFields.Count == 0)
                continue;

            var matchingFields = leftFields
                .Intersect(rightFields, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matchingFields.Count == 0)
                continue;

            var existingJoin = sapJoins.FirstOrDefault(j =>
                string.Equals(j.LeftAlias, primary.Alias, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(j.RightAlias, source.Alias, StringComparison.OrdinalIgnoreCase));

            var keyList = string.Join(',', matchingFields);
            if (existingJoin is null)
            {
                sapJoins.Add(new SapJoinDefinition
                {
                    LeftAlias = primary.Alias,
                    RightAlias = source.Alias,
                    LeftKeys = keyList,
                    RightKeys = keyList,
                    JoinType = "Left",
                    IsActive = true,
                    SortOrder = sapJoins.Count
                });
            }
            else
            {
                existingJoin.LeftKeys = keyList;
                existingJoin.RightKeys = keyList;
                existingJoin.JoinType = "Left";
                existingJoin.IsActive = true;
            }

            createdOrUpdated++;
        }

        if (createdOrUpdated == 0)
            return SapAutoMatchResult.InfoResult("Kein passender Join-Vorschlag gefunden.");

        NormalizeSapConfigCollections(sapSources, sapJoins, []);
        return SapAutoMatchResult.SuccessResult($"{createdOrUpdated} Join-Vorschlaege gesetzt.");
    }

    public void RemoveSapJoin(List<SapJoinDefinition> sapJoins, SapJoinDefinition join)
        => sapJoins.Remove(join);

    public void AddSapMapping(List<SapFieldMapping> sapMappings, IReadOnlyList<string> salesRecordFields, List<string> sapAvailableSourceExpressions)
    {
        sapMappings.Add(new SapFieldMapping
        {
            TargetField = salesRecordFields.First(),
            SourceExpression = sapAvailableSourceExpressions.FirstOrDefault() ?? "=SAP",
            IsActive = true,
            SortOrder = sapMappings.Count
        });
    }

    public void RemoveSapMapping(List<SapFieldMapping> sapMappings, SapFieldMapping mapping)
        => sapMappings.Remove(mapping);

    public List<string> BuildSourceExpressionsFromMappings(List<SapFieldMapping> sapMappings)
        => sapMappings
            .Select(m => m.SourceExpression)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public Dictionary<string, List<string>> BuildSourceFieldMapFromJoins(List<SapJoinDefinition> sapJoins)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var join in sapJoins)
        {
            AddJoinKeysToFieldMap(result, join.LeftAlias, join.LeftKeys);
            AddJoinKeysToFieldMap(result, join.RightAlias, join.RightKeys);
        }

        return result;
    }

    public IEnumerable<string> GetSapAliases(List<SapSourceDefinition> sapSources)
        => sapSources.Where(s => !string.IsNullOrWhiteSpace(s.Alias)).Select(s => s.Alias).Distinct(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<string> GetAvailableSourceExpressions(List<string> sapAvailableSourceExpressions, string? currentValue)
    {
        var expressions = new List<string>(sapAvailableSourceExpressions);
        if (!string.IsNullOrWhiteSpace(currentValue) && !expressions.Contains(currentValue, StringComparer.OrdinalIgnoreCase))
            expressions.Insert(0, currentValue);

        return expressions;
    }

    public IEnumerable<string> GetAvailableJoinFields(Dictionary<string, List<string>> sapSourceFieldMap, string? alias, string? currentKeys)
    {
        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(alias) && sapSourceFieldMap.TryGetValue(alias, out var fields))
            values.AddRange(fields);

        foreach (var key in GetSelectedJoinKeys(currentKeys))
        {
            if (!values.Contains(key, StringComparer.OrdinalIgnoreCase))
                values.Add(key);
        }

        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void NormalizeSapConfigCollections(List<SapSourceDefinition> sapSources, List<SapJoinDefinition> sapJoins, List<SapFieldMapping> sapMappings)
    {
        for (var i = 0; i < sapSources.Count; i++)
            sapSources[i].SortOrder = i;
        for (var i = 0; i < sapJoins.Count; i++)
            sapJoins[i].SortOrder = i;
        for (var i = 0; i < sapMappings.Count; i++)
            sapMappings[i].SortOrder = i;

        var selectedPrimaryIndex = sapSources.FindIndex(s => s.IsPrimary);
        var primarySource = selectedPrimaryIndex >= 0 ? sapSources[selectedPrimaryIndex] : sapSources.FirstOrDefault();
        foreach (var source in sapSources)
            source.IsPrimary = primarySource is not null && ReferenceEquals(source, primarySource);
        if (sapSources.Count > 0 && sapSources.All(s => !s.IsPrimary))
            sapSources[0].IsPrimary = true;
    }

    private static void AddJoinKeysToFieldMap(Dictionary<string, List<string>> target, string alias, string keys)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        if (!target.TryGetValue(alias, out var fields))
        {
            fields = [];
            target[alias] = fields;
        }

        foreach (var key in GetSelectedJoinKeys(keys))
        {
            if (!fields.Contains(key, StringComparer.OrdinalIgnoreCase))
                fields.Add(key);
        }

        fields.Sort(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> GetSelectedJoinKeys(string? keys)
        => keys?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
}

public sealed class SapAutoMatchResult
{
    public bool Success { get; init; }
    public bool Warning { get; init; }
    public bool Info { get; init; }
    public string Message { get; init; } = string.Empty;

    public static SapAutoMatchResult WarningResult(string message) => new() { Warning = true, Message = message };
    public static SapAutoMatchResult InfoResult(string message) => new() { Info = true, Message = message };
    public static SapAutoMatchResult SuccessResult(string message) => new() { Success = true, Message = message };
}
