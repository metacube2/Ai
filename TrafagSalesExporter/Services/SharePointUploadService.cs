using Azure.Identity;
using Microsoft.Graph;

namespace TrafagSalesExporter.Services;

public class SharePointUploadService
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _siteUrl;
    private readonly string _exportFolder;

    public SharePointUploadService(string tenantId, string clientId, string clientSecret, string siteUrl, string exportFolder)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        _graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
        _siteUrl = siteUrl;
        _exportFolder = exportFolder;
    }

    public async Task UploadAsync(string land, string localFilePath)
    {
        var uri = new Uri(_siteUrl);
        var sitePath = uri.AbsolutePath;
        var site = await _graphClient.Sites[$"{uri.Host}:{sitePath}"].GetAsync();

        if (site?.Id is null)
        {
            throw new InvalidOperationException("SharePoint Site konnte nicht gefunden werden.");
        }

        var drive = await _graphClient.Sites[site.Id].Drive.GetAsync();
        if (drive?.Id is null)
        {
            throw new InvalidOperationException("SharePoint Dokumentenbibliothek konnte nicht gefunden werden.");
        }

        var fileName = Path.GetFileName(localFilePath);
        var folderPath = $"{_exportFolder.Trim('/').Trim()}";
        var remotePath = $"{folderPath}/{land}/{fileName}";

        await using var stream = File.OpenRead(localFilePath);
        await _graphClient.Drives[drive.Id].Root.ItemWithPath(remotePath).Content.PutAsync(stream);
    }
}
