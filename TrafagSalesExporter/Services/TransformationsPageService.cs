using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ITransformationsPageService
{
    Task<TransformationsPageState> LoadAsync();
    Task<List<FieldTransformationRule>> SaveAllAsync(List<FieldTransformationRule> rules);
}

public sealed class TransformationsPageService : ITransformationsPageService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public TransformationsPageService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<TransformationsPageState> LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rules = await db.FieldTransformationRules.OrderBy(r => r.SortOrder).ThenBy(r => r.Id).ToListAsync();

        foreach (var rule in rules)
            rule.RuleScope = string.IsNullOrWhiteSpace(rule.RuleScope) ? "Value" : rule.RuleScope;

        return new TransformationsPageState
        {
            SourceSystems = await db.SourceSystemDefinitions.OrderBy(x => x.Code).ToListAsync(),
            Rules = rules
        };
    }

    public async Task<List<FieldTransformationRule>> SaveAllAsync(List<FieldTransformationRule> rules)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.FieldTransformationRules.RemoveRange(db.FieldTransformationRules);
        await db.SaveChangesAsync();

        db.FieldTransformationRules.AddRange(rules);
        await db.SaveChangesAsync();

        return await db.FieldTransformationRules.OrderBy(r => r.SortOrder).ThenBy(r => r.Id).ToListAsync();
    }
}

public sealed class TransformationsPageState
{
    public List<FieldTransformationRule> Rules { get; set; } = [];
    public List<SourceSystemDefinition> SourceSystems { get; set; } = [];
}
