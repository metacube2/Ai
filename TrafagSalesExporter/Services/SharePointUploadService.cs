using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;

namespace TrafagSalesExporter.Services;

public class SharePointUploadService : ISharePointUploadService
{
    public async Task UploadAsync(string tenantId, string clientId, string clientSecret,
        string siteUrl, string exportFolder, string land, string localFilePath)
    {
        var normalizedTenantId = Normalize(tenantId);
        var normalizedClientId = Normalize(clientId);
        var normalizedClientSecret = Normalize(clientSecret);
        var normalizedSiteUrl = Normalize(siteUrl);
        var normalizedExportFolder = Normalize(exportFolder);
        var normalizedLand = Normalize(land);

        var credential = new ClientSecretCredential(normalizedTenantId, normalizedClientId, normalizedClientSecret);
        var graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);

        var uri = new Uri(normalizedSiteUrl);
        var sitePath = uri.AbsolutePath;
        var site = await graphClient.Sites[$"{uri.Host}:{sitePath}"].GetAsync();

        if (site?.Id is null)
            throw new InvalidOperationException("SharePoint Site konnte nicht gefunden werden.");

        var drive = await graphClient.Sites[site.Id].Drive.GetAsync();
        if (drive?.Id is null)
            throw new InvalidOperationException("SharePoint Dokumentenbibliothek konnte nicht gefunden werden.");

        var fileName = Path.GetFileName(localFilePath);
        var remotePath = string.Join("/",
            new[]
            {
                normalizedExportFolder.Trim('/').Trim(),
                normalizedLand.Trim('/').Trim(),
                fileName
            }.Where(segment => !string.IsNullOrWhiteSpace(segment)));

        await using var stream = File.OpenRead(localFilePath);
        await graphClient.Drives[drive.Id].Root.ItemWithPath(remotePath).Content.PutAsync(stream);
    }

    public async Task TestConnectionAsync(string tenantId, string clientId, string clientSecret, string siteUrl)
    {
        var normalizedTenantId = Normalize(tenantId);
        var normalizedClientId = Normalize(clientId);
        var normalizedClientSecret = Normalize(clientSecret);
        var normalizedSiteUrl = Normalize(siteUrl);
        var inputPreview = BuildInputPreview(normalizedTenantId, normalizedClientId, normalizedClientSecret, normalizedSiteUrl);

        if (string.IsNullOrWhiteSpace(normalizedTenantId))
            throw new InvalidOperationException($"Tenant ID fehlt. {inputPreview}");
        if (string.IsNullOrWhiteSpace(normalizedClientId))
            throw new InvalidOperationException($"Client ID fehlt. {inputPreview}");
        if (string.IsNullOrWhiteSpace(normalizedClientSecret))
            throw new InvalidOperationException($"Client Secret fehlt. {inputPreview}");
        if (string.IsNullOrWhiteSpace(normalizedSiteUrl))
            throw new InvalidOperationException($"Site URL fehlt. {inputPreview}");

        var credential = new ClientSecretCredential(normalizedTenantId, normalizedClientId, normalizedClientSecret);

        try
        {
            await credential.GetTokenAsync(
                new TokenRequestContext(["https://graph.microsoft.com/.default"]),
                CancellationToken.None);
        }
        catch (AuthenticationFailedException ex)
        {
            throw new InvalidOperationException(
                $"ClientSecretCredential authentication failed: {ex.Message}{Environment.NewLine}{inputPreview}",
                ex);
        }

        var graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
        var uri = new Uri(normalizedSiteUrl);
        var sitePath = uri.AbsolutePath;
        var site = await graphClient.Sites[$"{uri.Host}:{sitePath}"].GetAsync();

        if (site?.Id is null)
            throw new InvalidOperationException($"SharePoint Site konnte nicht gefunden werden. {inputPreview}");
    }

    private static string Normalize(string value) => value?.Trim() ?? string.Empty;

    private static string BuildInputPreview(string tenantId, string clientId, string clientSecret, string siteUrl)
    {
        var maskedSecret = string.IsNullOrEmpty(clientSecret)
            ? "<leer>"
            : $"{new string('*', Math.Min(clientSecret.Length, 8))} (len={clientSecret.Length})";

        return $"Uebergeben: TenantId='{tenantId}', ClientId='{clientId}', ClientSecret={maskedSecret}, SiteUrl='{siteUrl}'";
    }
}
