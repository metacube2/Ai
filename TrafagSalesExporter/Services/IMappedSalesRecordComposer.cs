using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IMappedSalesRecordComposer
{
    List<SalesRecord> Compose(
        Site site,
        IReadOnlyList<SapSourceDefinition> sources,
        IReadOnlyList<SapJoinDefinition> joins,
        IReadOnlyList<SapFieldMapping> mappings,
        IReadOnlyDictionary<string, List<Dictionary<string, object?>>> sourceRows,
        string defaultDocumentType);
}
