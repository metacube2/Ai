namespace TrafagSalesExporter.Services;

public interface ITransformationStrategy
{
    string TransformationType { get; }
    object? Transform(object? sourceValue, string? argument);
}
