namespace TrafagSalesExporter.Services;

public interface ISharePointUploadService
{
    Task UploadAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string exportFolder, string land, string localFilePath, bool uploadTimestampedCopyIfLocked = false);
    Task<string> DownloadToTempFileAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string fileReference);
    Task<SharePointFileReference> ResolveLatestFileInFolderAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string folderReference, string siteTsc, int? preferredYear = null);
    Task<IReadOnlyList<SharePointFileReference>> ResolveManualImportFilesInFolderAsync(string tenantId, string clientId, string clientSecret, string siteUrl, string folderReference, string siteTsc, int? preferredYear = null);
    Task TestConnectionAsync(string tenantId, string clientId, string clientSecret, string siteUrl);
}

public sealed record SharePointFileReference(string FileReference, DateTimeOffset? LastModifiedUtc);
