namespace TrafagSalesExporter.Services;

public interface ITransformationCatalog
{
    IReadOnlyList<TransformationCatalogItem> GetAll();
    IReadOnlyList<TransformationCatalogItem> GetByScope(string ruleScope);
}

public sealed class TransformationCatalogItem
{
    public string Key { get; init; } = string.Empty;
    public string RuleScope { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public string SourceFile { get; init; } = string.Empty;
    public string CodeSnippet { get; init; } = string.Empty;
}
