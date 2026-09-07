namespace TrafagSalesExporter.Services;

/// <summary>
/// Liest die Disponent-zu-Produktgruppe-Zuordnung direkt aus dem SAP-Gateway-Service.
/// Unterstuetzt sowohl ein bereits zusammengefuehrtes EntitySet als auch die zwei
/// SAP-Customizing-EntitySets ZDISPO_GRP und ZDISPO_SPART.
/// </summary>
public interface IPurchasingProductGroupSapReader
{
    Task<PurchasingProductGroupSapResult> ReadAsync(
        string serviceUrl,
        string username,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed class PurchasingProductGroupSapReader : IPurchasingProductGroupSapReader
{
    private static readonly string[] CombinedEntityNeedles =
        ["purchasingproductgroup", "productgroupmap", "zc23productgroup", "zstrproductgroup"];
    private static readonly string[] GroupEntityNeedles = ["zdispogrp", "zdispogroup"];
    private static readonly string[] TextEntityNeedles = ["zdispospart", "dispospart"];

    private readonly ISapGatewayService _sapGatewayService;

    public PurchasingProductGroupSapReader(ISapGatewayService sapGatewayService)
    {
        _sapGatewayService = sapGatewayService;
    }

    public async Task<PurchasingProductGroupSapResult> ReadAsync(
        string serviceUrl,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var entitySets = await _sapGatewayService.GetEntitySetsAsync(
            serviceUrl, username, password, cancellationToken);

        var combinedSet = ResolveEntitySetName(entitySets, CombinedEntityNeedles);
        if (combinedSet is not null)
        {
            var rows = await _sapGatewayService.GetEntityRowsAsync(
                serviceUrl, combinedSet, username, password, cancellationToken: cancellationToken);
            var rules = ParseCombinedRows(rows);
            EnsureRules(rules, combinedSet);
            return new PurchasingProductGroupSapResult(combinedSet, rules);
        }

        var groupSet = ResolveEntitySetName(entitySets, GroupEntityNeedles);
        var textSet = ResolveEntitySetName(entitySets, TextEntityNeedles);
        if (groupSet is null || textSet is null)
        {
            throw new InvalidOperationException(
                "SAP-Gateway-Service liefert keine Produktgruppen-Zuordnung. Erwartet wird entweder " +
                "ein EntitySet ProductGroupMap/ZC23ProductGroup oder beide EntitySets " +
                "ZDISPO_GRP und ZDISPO_SPART. Excel wird bewusst nicht als Fallback verwendet. " +
                $"Verfuegbare EntitySets: {string.Join(", ", entitySets)}");
        }

        var groupRows = await _sapGatewayService.GetEntityRowsAsync(
            serviceUrl, groupSet, username, password, cancellationToken: cancellationToken);
        var textRows = await _sapGatewayService.GetEntityRowsAsync(
            serviceUrl, textSet, username, password, cancellationToken: cancellationToken);
        var joinedRules = JoinSapRows(groupRows, textRows);
        EnsureRules(joinedRules, $"{groupSet} + {textSet}");
        return new PurchasingProductGroupSapResult($"{groupSet} + {textSet}", joinedRules);
    }

    internal static string? ResolveEntitySetName(
        IReadOnlyCollection<string> entitySets,
        params string[] normalizedNeedles)
    {
        foreach (var candidate in entitySets)
        {
            var normalized = NormalizeName(candidate);
            if (normalizedNeedles.Any(needle => normalized.Contains(NormalizeName(needle))))
                return candidate;
        }

        return null;
    }

    internal static IReadOnlyList<PurchasingProductGroupSapRule> ParseCombinedRows(
        IReadOnlyList<Dictionary<string, object?>> rows) =>
        NormalizeRules(rows.Select(row => new PurchasingProductGroupSapRule(
            GetText(row, "DisponentPattern", "DispoKz", "Disponent", "VknrDispo"),
            GetText(row, "ProductGroup", "Dispo", "Produktgruppe"),
            GetText(row, "ProductGroupText", "Descr", "Description", "ProduktgruppenText"))));

    internal static IReadOnlyList<PurchasingProductGroupSapRule> JoinSapRows(
        IReadOnlyList<Dictionary<string, object?>> groupRows,
        IReadOnlyList<Dictionary<string, object?>> textRows)
    {
        var descriptions = textRows
            .Select(row => new
            {
                ProductGroup = NormalizeValue(GetText(row, "Dispo", "ProductGroup", "Produktgruppe")),
                Text = GetText(row, "Descr", "ProductGroupText", "Description", "ProduktgruppenText").Trim()
            })
            .Where(row => row.ProductGroup.Length > 0)
            .GroupBy(row => row.ProductGroup, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => row.Text).FirstOrDefault(text => text.Length > 0) ?? group.Key,
                StringComparer.OrdinalIgnoreCase);

        return NormalizeRules(groupRows.Select(row =>
        {
            var productGroup = NormalizeValue(GetText(row, "Dispo", "ProductGroup", "Produktgruppe"));
            return new PurchasingProductGroupSapRule(
                GetText(row, "DispoKz", "DisponentPattern", "Disponent", "VknrDispo"),
                productGroup,
                descriptions.TryGetValue(productGroup, out var text) ? text : productGroup);
        }));
    }

    private static IReadOnlyList<PurchasingProductGroupSapRule> NormalizeRules(
        IEnumerable<PurchasingProductGroupSapRule> rules) => rules
        .Select(rule => new PurchasingProductGroupSapRule(
            NormalizeValue(rule.DisponentPattern),
            NormalizeValue(rule.ProductGroup),
            rule.ProductGroupText.Trim()))
        .Where(rule => rule.DisponentPattern.Length > 0 && rule.ProductGroup.Length > 0)
        .Select(rule => rule with
        {
            ProductGroupText = rule.ProductGroupText.Length > 0 ? rule.ProductGroupText : rule.ProductGroup
        })
        // Der SQLite-Key ist (DisponentPattern, ProductGroup). Falls ein zusammengefuehrtes
        // SAP-Set dieselbe fachliche Zuordnung mehrfach liefert, gewinnt deterministisch die
        // erste Zeile; dadurch kann ein doppelter Text keine komplette Refresh-Transaktion
        // wegen eines Primary-Key-Verstosses abbrechen.
        .GroupBy(rule => (rule.DisponentPattern, rule.ProductGroup))
        .Select(group => group.First())
        .OrderBy(rule => rule.DisponentPattern, StringComparer.OrdinalIgnoreCase)
        .ThenBy(rule => rule.ProductGroup, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static void EnsureRules(IReadOnlyCollection<PurchasingProductGroupSapRule> rules, string source)
    {
        if (rules.Count == 0)
        {
            throw new InvalidDataException(
                $"SAP-Produktgruppenquelle {source} lieferte keine gueltigen Zuordnungen. " +
                "Der bestehende Cache bleibt unveraendert; Excel wird nicht verwendet.");
        }
    }

    private static string GetText(Dictionary<string, object?> row, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            var normalizedAlias = NormalizeName(alias);
            var match = row.FirstOrDefault(pair => NormalizeName(pair.Key) == normalizedAlias);
            if (!string.IsNullOrWhiteSpace(Convert.ToString(match.Value)))
                return Convert.ToString(match.Value)?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string NormalizeName(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string NormalizeValue(string value) => value.Trim().ToUpperInvariant();
}

public sealed record PurchasingProductGroupSapResult(
    string SourceEntitySets,
    IReadOnlyList<PurchasingProductGroupSapRule> Rules);

public sealed record PurchasingProductGroupSapRule(
    string DisponentPattern,
    string ProductGroup,
    string ProductGroupText);
