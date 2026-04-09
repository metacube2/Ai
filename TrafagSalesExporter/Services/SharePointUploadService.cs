using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace TrafagSalesExporter.Services;

public class SharePointUploadService
{
    public async Task UploadAsync(string siteUrl, string exportFolder, string tenantId, string clientId, string clientSecret, string land, string localFilePath)
    {
        var graph = CreateGraphClient(tenantId, clientId, clientSecret);
        var (siteId, driveId) = await ResolveSiteAndDriveAsync(graph, siteUrl);

        var folderPath = $"{exportFolder.Trim('/')}/{land}";
        await EnsureFolderPathAsync(graph, driveId, folderPath);

        var fileName = Path.GetFileName(localFilePath);
        var remotePath = $"{folderPath}/{fileName}";

        await using var stream = File.OpenRead(localFilePath);
        await graph.Drives[driveId].Root.ItemWithPath(remotePath).Content.PutAsync(stream);
    }

    public async Task<bool> TestConnectionAsync(string siteUrl, string tenantId, string clientId, string clientSecret)
    {
        var graph = CreateGraphClient(tenantId, clientId, clientSecret);
        var (siteId, _) = await ResolveSiteAndDriveAsync(graph, siteUrl);
        return !string.IsNullOrWhiteSpace(siteId);
    }

    private static GraphServiceClient CreateGraphClient(string tenantId, string clientId, string clientSecret)
    {
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        return new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
    }

    private static async Task<(string siteId, string driveId)> ResolveSiteAndDriveAsync(GraphServiceClient graph, string siteUrl)
    {
        var uri = new Uri(siteUrl);
        var site = await graph.Sites[$"{uri.Host}:{uri.AbsolutePath}"].GetAsync();
        if (site?.Id is null)
        {
            throw new InvalidOperationException("SharePoint Site nicht gefunden.");
        }

        var drive = await graph.Sites[site.Id].Drive.GetAsync();
        if (drive?.Id is null)
        {
            throw new InvalidOperationException("SharePoint Dokumentenbibliothek nicht gefunden.");
        }

        return (site.Id, drive.Id);
    }

    private static async Task EnsureFolderPathAsync(GraphServiceClient graph, string driveId, string folderPath)
    {
        var segments = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var currentPath = string.Empty;

        foreach (var segment in segments)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

            try
            {
                _ = await graph.Drives[driveId].Root.ItemWithPath(currentPath).GetAsync();
            }
            catch
            {
                var parentPath = currentPath.Contains('/')
                    ? currentPath[..currentPath.LastIndexOf('/')]
                    : string.Empty;

                var parent = string.IsNullOrEmpty(parentPath)
                    ? await graph.Drives[driveId].Root.GetAsync()
                    : await graph.Drives[driveId].Root.ItemWithPath(parentPath).GetAsync();

                if (parent?.Id is null)
                {
                    throw new InvalidOperationException("SharePoint Parent-Ordner konnte nicht ermittelt werden.");
                }

                await graph.Drives[driveId].Items[parent.Id].Children.PostAsync(new DriveItem
                {
                    Name = segment,
                    Folder = new Folder(),
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["@microsoft.graph.conflictBehavior"] = "replace"
                    }
                });
            }
        }
    }
}
