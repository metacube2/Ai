using System.Reflection;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public sealed class FinanceRuleEngine
{
    private readonly IReadOnlyList<FinanceRule> _rules;
    private readonly Dictionary<string, HashSet<string>> _deduplicationKeys = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, PropertyInfo> SalesRecordProperties = typeof(SalesRecord)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

    public FinanceRuleEngine(IEnumerable<FinanceRule> rules)
    {
        _rules = rules
            .Where(rule => rule.IsActive)
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.Id)
            .ToList();
    }

    public DateTime ResolveFinanceDate(SalesRecord record, string countryKey)
    {
        var forceYear = _rules.FirstOrDefault(rule =>
            IsRuleInScope(rule, countryKey) &&
            rule.RuleType.Equals(FinanceRuleTypes.ForceYear, StringComparison.OrdinalIgnoreCase) &&
            RuleMatches(rule, record));

        if (forceYear?.Year is > 0)
            return new DateTime(forceYear.Year.Value, 12, 31);

        return record.PostingDate ?? record.InvoiceDate ?? record.ExtractionDate;
    }

    public bool ShouldInclude(SalesRecord record, string countryKey)
    {
        foreach (var rule in _rules.Where(rule => IsRuleInScope(rule, countryKey)))
        {
            if (!RuleMatches(rule, record))
                continue;

            if (rule.RuleType.Equals(FinanceRuleTypes.Exclude, StringComparison.OrdinalIgnoreCase))
                return false;

            if (rule.RuleType.Equals(FinanceRuleTypes.DeduplicateBlankSupplierCountry, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(record.SupplierCountry))
            {
                var seen = GetDeduplicationSet(rule, countryKey);
                return seen.Add(BuildBlankSupplierCountryDeduplicationKey(record));
            }
        }

        return true;
    }

    public decimal ResolveNetSalesActual(SalesRecord record, string countryKey, bool include)
    {
        if (!include)
            return 0m;

        foreach (var rule in _rules.Where(rule => IsRuleInScope(rule, countryKey)))
        {
            if (!rule.RuleType.Equals(FinanceRuleTypes.NegateAmount, StringComparison.OrdinalIgnoreCase) ||
                !RuleMatches(rule, record))
                continue;

            return -Math.Abs(record.SalesPriceValue);
        }

        return record.SalesPriceValue;
    }

    public string ResolveExclusionReason(SalesRecord record, string countryKey)
    {
        foreach (var rule in _rules.Where(rule => IsRuleInScope(rule, countryKey)))
        {
            if (!RuleMatches(rule, record))
                continue;

            if (rule.RuleType.Equals(FinanceRuleTypes.Exclude, StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(rule.Notes) ? $"Excluded {countryKey}" : rule.Notes;

            if (rule.RuleType.Equals(FinanceRuleTypes.DeduplicateBlankSupplierCountry, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(record.SupplierCountry))
                return string.IsNullOrWhiteSpace(rule.Notes) ? $"Excluded {countryKey} duplicate without Supplier country" : rule.Notes;
        }

        return $"Excluded {countryKey}";
    }

    public static IReadOnlyList<FinanceRule> CreateDefaultRules()
        =>
        [
            new FinanceRule
            {
                ScopeKey = "DE",
                Year = 2025,
                RuleType = FinanceRuleTypes.ForceYear,
                MatchType = FinanceRuleMatchTypes.Always,
                Notes = "DE Alphaplan Jahresfile 2025",
                SortOrder = 100
            },
            new FinanceRule
            {
                ScopeKey = "DE",
                RuleType = FinanceRuleTypes.Exclude,
                FieldName = nameof(SalesRecord.CustomerName),
                MatchType = FinanceRuleMatchTypes.Equal,
                MatchValue = "Trafag AG",
                Notes = "Excluded DE Weiterberechnung Trafag AG",
                SortOrder = 110
            },
            new FinanceRule
            {
                ScopeKey = "DE",
                RuleType = FinanceRuleTypes.Exclude,
                FieldName = nameof(SalesRecord.CustomerName),
                MatchType = FinanceRuleMatchTypes.Contains,
                MatchValue = "Magnetic Sense",
                Notes = "Excluded DE Weiterberechnung Magnetic Sense",
                SortOrder = 120
            },
            new FinanceRule
            {
                ScopeKey = "DE",
                RuleType = FinanceRuleTypes.Exclude,
                FieldName = nameof(SalesRecord.InvoiceNumber),
                MatchType = FinanceRuleMatchTypes.Equal,
                MatchValue = "GS2510095",
                Notes = "Excluded DE GS2510095 already captured in 2024",
                SortOrder = 130
            },
            new FinanceRule
            {
                ScopeKey = "DE",
                RuleType = FinanceRuleTypes.NegateAmount,
                FieldName = nameof(SalesRecord.InvoiceNumber),
                MatchType = FinanceRuleMatchTypes.StartsWith,
                MatchValue = "GS",
                Notes = "DE Gutschriften negativ",
                SortOrder = 140
            },
            new FinanceRule
            {
                ScopeKey = "IT",
                RuleType = FinanceRuleTypes.Exclude,
                FieldName = nameof(SalesRecord.CustomerName),
                MatchType = FinanceRuleMatchTypes.Contains,
                MatchValue = "Trafag Italia",
                Notes = "Excluded IT customer: Trafag Italia",
                SortOrder = 200
            },
            new FinanceRule
            {
                ScopeKey = "IT",
                RuleType = FinanceRuleTypes.DeduplicateBlankSupplierCountry,
                FieldName = nameof(SalesRecord.SupplierCountry),
                MatchType = FinanceRuleMatchTypes.IsBlank,
                Notes = "Excluded IT duplicate without Supplier country",
                SortOrder = 210
            }
        ];

    private HashSet<string> GetDeduplicationSet(FinanceRule rule, string countryKey)
    {
        var key = $"{countryKey}|{rule.Id}|{rule.SortOrder}|{rule.RuleType}";
        if (!_deduplicationKeys.TryGetValue(key, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _deduplicationKeys[key] = set;
        }

        return set;
    }

    private static bool IsRuleInScope(FinanceRule rule, string countryKey)
        => string.IsNullOrWhiteSpace(rule.ScopeKey) ||
           rule.ScopeKey.Equals(countryKey, StringComparison.OrdinalIgnoreCase);

    private static bool RuleMatches(FinanceRule rule, SalesRecord record)
    {
        if (rule.MatchType.Equals(FinanceRuleMatchTypes.Always, StringComparison.OrdinalIgnoreCase))
            return true;

        var value = ReadRecordValue(record, rule.FieldName);
        var normalizedValue = NormalizeFinanceText(value);
        var normalizedMatch = NormalizeFinanceText(rule.MatchValue);

        return rule.MatchType switch
        {
            FinanceRuleMatchTypes.Equal => normalizedValue.Equals(normalizedMatch, StringComparison.OrdinalIgnoreCase),
            FinanceRuleMatchTypes.Contains => normalizedValue.Contains(normalizedMatch, StringComparison.OrdinalIgnoreCase),
            FinanceRuleMatchTypes.StartsWith => normalizedValue.StartsWith(normalizedMatch, StringComparison.OrdinalIgnoreCase),
            FinanceRuleMatchTypes.IsBlank => string.IsNullOrWhiteSpace(value),
            _ => false
        };
    }

    private static string ReadRecordValue(SalesRecord record, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return string.Empty;

        return SalesRecordProperties.TryGetValue(fieldName, out var property)
            ? property.GetValue(record)?.ToString() ?? string.Empty
            : string.Empty;
    }

    private static string BuildBlankSupplierCountryDeduplicationKey(SalesRecord record)
        => string.Join("|",
            record.Tsc,
            record.DocumentType,
            record.DocumentEntry,
            record.InvoiceNumber,
            record.PositionOnInvoice,
            record.Material,
            record.Name,
            record.Quantity,
            record.CustomerNumber,
            record.CustomerName,
            record.SalesPriceValue,
            record.DocumentTotalForeignCurrency,
            record.DocumentTotalLocalCurrency,
            record.VatSumForeignCurrency,
            record.VatSumLocalCurrency,
            record.PostingDate?.ToString("O") ?? string.Empty,
            record.InvoiceDate?.ToString("O") ?? string.Empty);

    private static string NormalizeFinanceText(string value)
        => (value ?? string.Empty)
            .Replace("\u00e4", "ae", StringComparison.OrdinalIgnoreCase)
            .Replace("\u00f6", "oe", StringComparison.OrdinalIgnoreCase)
            .Replace("\u00fc", "ue", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToUpperInvariant();
}
