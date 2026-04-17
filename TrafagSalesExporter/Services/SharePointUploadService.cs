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

    public async Task<string> DownloadToTempFileAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string fileReference)
    {
        var normalizedTenantId = Normalize(tenantId);
        var normalizedClientId = Normalize(clientId);
        var normalizedClientSecret = Normalize(clientSecret);
        var normalizedSiteUrl = Normalize(siteUrl);
        var normalizedReference = Normalize(fileReference);

        if (string.IsNullOrWhiteSpace(normalizedReference))
            throw new InvalidOperationException("SharePoint-Dateireferenz fehlt.");

        var credential = new ClientSecretCredential(normalizedTenantId, normalizedClientId, normalizedClientSecret);
        var graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);

        var siteUri = new Uri(normalizedSiteUrl);
        var sitePath = siteUri.AbsolutePath.TrimEnd('/');
        var site = await graphClient.Sites[$"{siteUri.Host}:{sitePath}"].GetAsync();

        if (site?.Id is null)
            throw new InvalidOperationException("SharePoint Site konnte nicht gefunden werden.");

        var drive = await graphClient.Sites[site.Id].Drive.GetAsync();
        if (drive?.Id is null)
            throw new InvalidOperationException("SharePoint Dokumentenbibliothek konnte nicht gefunden werden.");

        var remotePath = ResolveRemotePath(normalizedReference, siteUri);
        var fileName = Path.GetFileName(remotePath);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("Aus der SharePoint-Dateireferenz konnte kein Dateiname gelesen werden.");

        await using var contentStream = await graphClient.Drives[drive.Id].Root.ItemWithPath(remotePath).Content.GetAsync()
            ?? throw new InvalidOperationException("SharePoint-Datei konnte nicht gelesen werden.");

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_{fileName}");
        await using var targetStream = File.Create(tempPath);
        await contentStream.CopyToAsync(targetStream);
        return tempPath;
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

    private static string ResolveRemotePath(string fileReference, Uri siteUri)
    {
        if (Uri.TryCreate(fileReference, UriKind.Absolute, out var fileUri))
        {
            if (!string.Equals(fileUri.Host, siteUri.Host, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Die SharePoint-Datei muss auf derselben SharePoint-Site liegen wie die zentrale Konfiguration.");

            var sitePath = siteUri.AbsolutePath.TrimEnd('/');
            var absolutePath = Uri.UnescapeDataString(fileUri.AbsolutePath);
            if (absolutePath.StartsWith(sitePath, StringComparison.OrdinalIgnoreCase))
                absolutePath = absolutePath[sitePath.Length..];

            return absolutePath.Trim('/').Trim();
        }

        return fileReference.Trim('/').Trim();
    }

    private static string BuildInputPreview(string tenantId, string clientId, string clientSecret, string siteUrl)
    {
        var maskedSecret = string.IsNullOrEmpty(clientSecret)
            ? "<leer>"
            : $"{new string('*', Math.Min(clientSecret.Length, 8))} (len={clientSecret.Length})";

        return $"Uebergeben: TenantId='{tenantId}', ClientId='{clientId}', ClientSecret={maskedSecret}, SiteUrl='{siteUrl}'";
    }
}
