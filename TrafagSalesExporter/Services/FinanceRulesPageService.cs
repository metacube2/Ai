using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IFinanceRulesPageService
{
    Task<List<FinanceRule>> LoadAsync();
    Task<List<FinanceRule>> SaveAllAsync(List<FinanceRule> rules);
}

public sealed class FinanceRulesPageService : IFinanceRulesPageService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FinanceRulesPageService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<FinanceRule>> LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rules = await db.FinanceRules
            .AsNoTracking()
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.Id)
            .ToListAsync();

        if (rules.Count > 0)
            return rules;

        return FinanceRuleEngine.CreateDefaultRules()
            .Select(CloneRule)
            .ToList();
    }

    public async Task<List<FinanceRule>> SaveAllAsync(List<FinanceRule> rules)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.FinanceRules.RemoveRange(db.FinanceRules);
        db.FinanceRules.AddRange(rules
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.Id)
            .Select(CloneRule));
        await db.SaveChangesAsync();

        return await db.FinanceRules
            .AsNoTracking()
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.Id)
            .ToListAsync();
    }

    private static FinanceRule CloneRule(FinanceRule rule)
        => new()
        {
            ScopeKey = rule.ScopeKey.Trim().ToUpperInvariant(),
            Year = rule.Year,
            RuleType = string.IsNullOrWhiteSpace(rule.RuleType) ? FinanceRuleTypes.Exclude : rule.RuleType,
            FieldName = rule.FieldName ?? string.Empty,
            MatchType = string.IsNullOrWhiteSpace(rule.MatchType) ? FinanceRuleMatchTypes.Contains : rule.MatchType,
            MatchValue = rule.MatchValue ?? string.Empty,
            NumericValue = rule.NumericValue,
            Notes = rule.Notes ?? string.Empty,
            SortOrder = rule.SortOrder,
            IsActive = rule.IsActive
        };
}
