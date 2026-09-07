using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IRecordTransformationService
{
    void Apply(List<SalesRecord> records, IEnumerable<FieldTransformationRule> rules);
}
