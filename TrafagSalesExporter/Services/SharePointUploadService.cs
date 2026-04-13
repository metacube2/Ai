using Azure.Identity;
using Microsoft.Graph;

namespace TrafagSalesExporter.Services;

public class SharePointUploadService : ISharePointUploadService
{
    public async Task UploadAsync(string tenantId, string clientId, string clientSecret,
        string siteUrl, string exportFolder, string land, string localFilePath)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);

        var uri = new Uri(siteUrl);
        var sitePath = uri.AbsolutePath;
        var site = await graphClient.Sites[$"{uri.Host}:{sitePath}"].GetAsync();

        if (site?.Id is null)
            throw new InvalidOperationException("SharePoint Site konnte nicht gefunden werden.");

        var drive = await graphClient.Sites[site.Id].Drive.GetAsync();
        if (drive?.Id is null)
            throw new InvalidOperationException("SharePoint Dokumentenbibliothek konnte nicht gefunden werden.");

        var fileName = Path.GetFileName(localFilePath);
        var folderPath = exportFolder.Trim('/').Trim();
        var remotePath = $"{folderPath}/{land}/{fileName}";

        await using var stream = File.OpenRead(localFilePath);
        await graphClient.Drives[drive.Id].Root.ItemWithPath(remotePath).Content.PutAsync(stream);
    }

    public async Task TestConnectionAsync(string tenantId, string clientId, string clientSecret, string siteUrl)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);

        var uri = new Uri(siteUrl);
        var sitePath = uri.AbsolutePath;
        var site = await graphClient.Sites[$"{uri.Host}:{sitePath}"].GetAsync();

        if (site?.Id is null)
            throw new InvalidOperationException("SharePoint Site konnte nicht gefunden werden.");
    }
}
