namespace TrafagSalesExporter.Services;

public interface ISharePointUploadService
{
    Task UploadAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string exportFolder, string land, string localFilePath);
    Task<string> DownloadToTempFileAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string fileReference);
    Task TestConnectionAsync(string tenantId, string clientId, string clientSecret, string siteUrl);
}
