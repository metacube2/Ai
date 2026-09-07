namespace TrafagSalesExporter.Services.DataSources;

public interface IDataSourceAdapter
{
    /// <summary>
    /// Der Wert aus <see cref="Models.SourceSystemConnectionKinds"/>, den dieser Adapter behandelt.
    /// </summary>
    string ConnectionKind { get; }

    Task<DataSourceFetchResult> FetchAsync(DataSourceFetchContext context);
}
