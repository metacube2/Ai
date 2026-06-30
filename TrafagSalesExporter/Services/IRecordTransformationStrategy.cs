using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IRecordTransformationStrategy
{
    string TransformationType { get; }
    string Description => string.Empty;
    void Transform(SalesRecord record, FieldTransformationRule rule);
}
