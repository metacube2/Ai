using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.DataSources;

public sealed class DataSourceAdapterResolver : IDataSourceAdapterResolver
{
    private readonly Dictionary<string, IDataSourceAdapter> _adapters;

    public DataSourceAdapterResolver(IEnumerable<IDataSourceAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(
            a => a.ConnectionKind,
            StringComparer.OrdinalIgnoreCase);
    }

    public IDataSourceAdapter Resolve(string connectionKind)
    {
        if (string.IsNullOrWhiteSpace(connectionKind))
            connectionKind = SourceSystemConnectionKinds.Hana;

        if (_adapters.TryGetValue(connectionKind, out var adapter))
            return adapter;

        throw new InvalidOperationException(
            $"Kein DataSourceAdapter fuer ConnectionKind '{connectionKind}' registriert.");
    }
}
