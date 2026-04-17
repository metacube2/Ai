namespace TrafagSalesExporter.Services;

public interface ITransformationStrategy
{
    string TransformationType { get; }
    string Description => string.Empty;
    object? Transform(object? sourceValue, string? argument);
}
