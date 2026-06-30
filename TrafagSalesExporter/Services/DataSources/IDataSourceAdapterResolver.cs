namespace TrafagSalesExporter.Services.DataSources;

public interface IDataSourceAdapterResolver
{
    IDataSourceAdapter Resolve(string connectionKind);
}
