namespace TrafagSalesExporter.Services;

public interface IConfigTransferService
{
    Task<string> ExportJsonAsync(bool includeSecrets);
    Task ImportJsonAsync(string json);
}
