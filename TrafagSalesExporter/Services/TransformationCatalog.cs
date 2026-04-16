namespace TrafagSalesExporter.Services;

public class TransformationCatalog : ITransformationCatalog
{
    private readonly IReadOnlyList<TransformationCatalogItem> _items;

    public TransformationCatalog(IEnumerable<ITransformationStrategy> valueStrategies, IEnumerable<IRecordTransformationStrategy> recordStrategies)
    {
        _items = valueStrategies
            .Select(x => new TransformationCatalogItem
            {
                Key = x.TransformationType,
                RuleScope = "Value",
                Description = x.Description
            })
            .Concat(recordStrategies.Select(x => new TransformationCatalogItem
            {
                Key = x.TransformationType,
                RuleScope = "Record",
                Description = x.Description
            }))
            .OrderBy(x => x.RuleScope, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<TransformationCatalogItem> GetAll() => _items;

    public IReadOnlyList<TransformationCatalogItem> GetByScope(string ruleScope)
        => _items
            .Where(x => string.Equals(x.RuleScope, ruleScope, StringComparison.OrdinalIgnoreCase))
            .ToList();
}
