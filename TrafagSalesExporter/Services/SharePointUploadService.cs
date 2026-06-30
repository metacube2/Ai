using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

public class SharePointUploadService : ISharePointUploadService
{
    public async Task UploadAsync(string tenantId, string clientId, string clientSecret,
        string siteUrl, string exportFolder, string land, string localFilePath, bool uploadTimestampedCopyIfLocked = false)
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

        var remotePath = BuildUploadPath(normalizedExportFolder, normalizedLand, Path.GetFileName(localFilePath));
        try
        {
            await UploadWithLockRetryAsync(graphClient, drive.Id, remotePath, localFilePath);
        }
        catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (uploadTimestampedCopyIfLocked && IsLockedSharePointResource(ex))
        {
            var timestampedPath = BuildUploadPath(normalizedExportFolder, normalizedLand, BuildTimestampedFileName(localFilePath));
            await UploadWithLockRetryAsync(graphClient, drive.Id, timestampedPath, localFilePath);
        }
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

    public async Task<SharePointFileReference> ResolveLatestFileInFolderAsync(
        string tenantId,
        string clientId,
        string clientSecret,
        string siteUrl,
        string folderReference,
        string siteTsc,
        int? preferredYear = null)
    {
        var files = await ResolveManualImportFilesInFolderAsync(
            tenantId, clientId, clientSecret, siteUrl, folderReference, siteTsc, preferredYear);
        return files.First();
    }

    public async Task<IReadOnlyList<SharePointFileReference>> ResolveManualImportFilesInFolderAsync(
        string tenantId,
        string clientId,
        string clientSecret,
        string siteUrl,
        string folderReference,
        string siteTsc,
        int? preferredYear = null)
    {
        var normalizedTenantId = Normalize(tenantId);
        var normalizedClientId = Normalize(clientId);
        var normalizedClientSecret = Normalize(clientSecret);
        var normalizedSiteUrl = Normalize(siteUrl);
        var normalizedReference = Normalize(folderReference);
        var normalizedTsc = Normalize(siteTsc).ToUpperInvariant();
        var isSpainImport = IsSpainManualImport(normalizedTsc, normalizedReference);
        var isAlphaplanImport = IsAlphaplanManualImport(normalizedTsc, normalizedReference);

        if (string.IsNullOrWhiteSpace(normalizedReference))
            throw new InvalidOperationException("SharePoint-Ordnerreferenz fehlt.");

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

        var folderPath = ResolveRemotePath(normalizedReference, siteUri);
        var children = await graphClient.Drives[drive.Id].Root.ItemWithPath(folderPath).Children.GetAsync();
        if (isAlphaplanImport)
        {
            var alphaplanReferences = await ResolveAlphaplanManualImportFilesAsync(graphClient, drive.Id, folderPath);
            if (alphaplanReferences.Count > 0)
                return alphaplanReferences;
        }

        var allCandidates = children?.Value?
            .Where(item => item.File is not null)
            .Where(item => IsSupportedManualImportFile(item.Name))
            .Where(item => isSpainImport ? IsSpainSalesFile(item.Name) : MatchesTsc(item.Name, normalizedTsc))
            .Select(item =>
            {
                var hasSpainRange = TryParseSpainSalesRangeFileName(item.Name, out var rangeStart, out var rangeEnd);
                return new
                {
                    Item = item,
                    FileDate = TryParseDatedSiteFileName(item.Name, normalizedTsc, out var fileDate) ? fileDate : (DateTime?)null,
                    SpainRangeStart = hasSpainRange ? rangeStart : (DateTime?)null,
                    SpainRangeEnd = hasSpainRange ? rangeEnd : (DateTime?)null,
                    AnnualYear = TryParseAnnualSiteFileName(item.Name, normalizedTsc, out var annualYear) ? annualYear : (int?)null,
                    SnapshotDate = TryParseSnapshotDate(item.Name, out var snapshotDate) ? snapshotDate : (DateTime?)null
                };
            })
            .ToList() ?? [];

        if (isSpainImport)
        {
            var spainCandidates = allCandidates
                .OrderBy(x => x.SpainRangeStart is null ? 0 : 1)
                .ThenBy(x => x.SpainRangeStart ?? DateTime.MinValue)
                .ThenBy(x => x.SpainRangeEnd ?? DateTime.MinValue)
                .ThenBy(x => x.Item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (spainCandidates.Count == 0)
                throw new InvalidOperationException($"Im SharePoint-Ordner '{folderPath}' wurde keine Spain_Sales*.csv gefunden.");

            return spainCandidates
                .Select(x => new SharePointFileReference(
                    string.Join("/", folderPath.Trim('/'), x.Item.Name).Trim('/'),
                    x.Item.LastModifiedDateTime))
                .ToList();
        }

        if (preferredYear is not null)
        {
            var annual = allCandidates
                .Where(x => x.AnnualYear == preferredYear.Value)
                .OrderByDescending(x => x.SnapshotDate ?? x.Item.LastModifiedDateTime?.UtcDateTime ?? DateTime.MinValue)
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"Im SharePoint-Ordner '{folderPath}' wurde keine Jahresdatei fuer '{normalizedTsc}' und Jahr {preferredYear.Value} gefunden.");

            var references = new List<SharePointFileReference>
            {
                new(string.Join("/", folderPath.Trim('/'), annual.Item.Name).Trim('/'), annual.Item.LastModifiedDateTime)
            };

            if (preferredYear.Value >= DateTime.Today.Year)
            {
                var baseDate = annual.SnapshotDate
                    ?? annual.Item.LastModifiedDateTime?.UtcDateTime.Date
                    ?? new DateTime(preferredYear.Value, 1, 1);

                references.AddRange(allCandidates
                    .Where(x => x.FileDate is not null)
                    .Where(x => x.FileDate!.Value.Year == preferredYear.Value)
                    .Where(x => x.FileDate!.Value.Date > baseDate.Date)
                    .OrderBy(x => x.FileDate)
                    .Select(x => new SharePointFileReference(
                        string.Join("/", folderPath.Trim('/'), x.Item.Name).Trim('/'),
                        x.Item.LastModifiedDateTime)));
            }

            return references;
        }

        var candidates = allCandidates
            .OrderByDescending(x => x.FileDate ?? x.Item.LastModifiedDateTime?.UtcDateTime ?? DateTime.MinValue)
            .ThenByDescending(x => x.Item.LastModifiedDateTime?.UtcDateTime ?? DateTime.MinValue)
            .ToList();

        var selected = candidates.FirstOrDefault()
            ?? throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(normalizedTsc)
                    ? $"Im SharePoint-Ordner '{folderPath}' wurde keine Excel-/CSV-Datei gefunden."
                    : $"Im SharePoint-Ordner '{folderPath}' wurde keine Excel-/CSV-Datei fuer '{normalizedTsc}' gefunden.");

        return
        [
            new SharePointFileReference(
                string.Join("/", folderPath.Trim('/'), selected.Item.Name).Trim('/'),
                selected.Item.LastModifiedDateTime)
        ];
    }

    public async Task<SharePointFileReference?> ResolveLatestProcessedMergeInputFileAsync(
        string tenantId,
        string clientId,
        string clientSecret,
        string siteUrl,
        string folderReference,
        string siteTsc)
    {
        var normalizedTenantId = Normalize(tenantId);
        var normalizedClientId = Normalize(clientId);
        var normalizedClientSecret = Normalize(clientSecret);
        var normalizedSiteUrl = Normalize(siteUrl);
        var normalizedReference = Normalize(folderReference);
        var normalizedTsc = Normalize(siteTsc).ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedReference))
            return null;

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

        var folderPath = ResolveRemotePath(normalizedReference, siteUri);
        var children = await graphClient.Drives[drive.Id].Root.ItemWithPath(folderPath).Children.GetAsync();
        var latest = children?.Value?
            .Where(item => item.File is not null)
            .Where(item => IsProcessedMergeInputFile(item.Name))
            .Where(item => MatchesProcessedMergeInputTsc(item.Name, normalizedTsc))
            .OrderByDescending(item => item.LastModifiedDateTime?.UtcDateTime ?? DateTime.MinValue)
            .ThenByDescending(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return latest is null
            ? null
            : new SharePointFileReference(BuildRemotePath(folderPath, latest.Name), latest.LastModifiedDateTime);
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

    private static async Task UploadWithLockRetryAsync(GraphServiceClient graphClient, string driveId, string remotePath, string localFilePath)
    {
        const int attempts = 4;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await using var stream = File.OpenRead(localFilePath);
                await graphClient.Drives[driveId].Root.ItemWithPath(remotePath).Content.PutAsync(stream);
                return;
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (attempt < attempts && IsLockedSharePointResource(ex))
            {
                await Task.Delay(TimeSpan.FromSeconds(5 * attempt));
            }
        }
    }

    private static string BuildUploadPath(string exportFolder, string land, string fileName)
        => string.Join("/",
            new[]
            {
                exportFolder.Trim('/').Trim(),
                land.Trim('/').Trim(),
                fileName
            }.Where(segment => !string.IsNullOrWhiteSpace(segment)));

    private static string BuildTimestampedFileName(string localFilePath)
    {
        var name = Path.GetFileNameWithoutExtension(localFilePath);
        var extension = Path.GetExtension(localFilePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return $"{name}_{timestamp}{extension}";
    }

    private static bool IsLockedSharePointResource(Exception ex)
        => ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
           ex.ToString().Contains("locked", StringComparison.OrdinalIgnoreCase);

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

    private static bool IsSupportedManualImportFile(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        return extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesTsc(string? fileName, string normalizedTsc)
    {
        if (string.IsNullOrWhiteSpace(normalizedTsc))
            return true;

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        return nameWithoutExtension.EndsWith($"_{normalizedTsc}", StringComparison.OrdinalIgnoreCase) ||
               Regex.IsMatch(nameWithoutExtension, $@"(^|[^A-Z0-9]){Regex.Escape(normalizedTsc)}([^A-Z0-9]|$)", RegexOptions.IgnoreCase);
    }

    private static bool IsProcessedMergeInputFile(string? fileName)
        => Path.GetFileName(fileName ?? string.Empty).StartsWith("Sales_ProcessedMergeInput_", StringComparison.OrdinalIgnoreCase) &&
           Path.GetExtension(fileName ?? string.Empty).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesProcessedMergeInputTsc(string? fileName, string normalizedTsc)
    {
        if (string.IsNullOrWhiteSpace(normalizedTsc))
            return true;

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        const string prefix = "Sales_ProcessedMergeInput_";
        if (!nameWithoutExtension.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var suffix = nameWithoutExtension[prefix.Length..];
        var lastUnderscore = suffix.LastIndexOf('_');
        var tsc = lastUnderscore <= 0 ? suffix : suffix[..lastUnderscore];
        return string.Equals(tsc, normalizedTsc, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSpainManualImport(string normalizedTsc, string folderReference)
        => string.Equals(normalizedTsc, "TRES", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(normalizedTsc, "TRSE", StringComparison.OrdinalIgnoreCase) ||
           folderReference.Contains("Spanien", StringComparison.OrdinalIgnoreCase) ||
           folderReference.Contains("Spain", StringComparison.OrdinalIgnoreCase);

    private static bool IsAlphaplanManualImport(string normalizedTsc, string folderReference)
        => string.Equals(normalizedTsc, "TRDE", StringComparison.OrdinalIgnoreCase) ||
           folderReference.Contains("Alphaplan", StringComparison.OrdinalIgnoreCase) ||
           folderReference.Contains("Deutschland", StringComparison.OrdinalIgnoreCase) ||
           folderReference.Contains("Germany", StringComparison.OrdinalIgnoreCase);

    private static async Task<IReadOnlyList<SharePointFileReference>> ResolveAlphaplanManualImportFilesAsync(
        GraphServiceClient graphClient,
        string driveId,
        string folderPath)
    {
        var folders = await ResolveFolderTreeAsync(graphClient, driveId, folderPath.Trim('/'), maxDepth: 3);
        var references = new List<(SharePointFileReference Reference, string SortKey)>();

        foreach (var folder in folders)
        {
            var children = await graphClient.Drives[driveId].Root.ItemWithPath(folder).Children.GetAsync();
            var files = children?.Value?
                .Where(item => item.File is not null)
                .Where(item => IsAlphaplanInvoiceFile(item.Name))
                .ToDictionary(item => item.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase) ?? [];

            if (!files.TryGetValue("invoice_headers.csv", out var header) ||
                !files.TryGetValue("invoice_lines.csv", out var line))
                continue;

            var sortKey = BuildAlphaplanFolderSortKey(folderPath, folder);
            references.Add((new SharePointFileReference(BuildRemotePath(folder, header.Name), header.LastModifiedDateTime), $"{sortKey}|0"));
            references.Add((new SharePointFileReference(BuildRemotePath(folder, line.Name), line.LastModifiedDateTime), $"{sortKey}|1"));
        }

        return references
            .OrderBy(x => x.SortKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Reference.FileReference, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Reference)
            .ToList();
    }

    private static async Task<List<string>> ResolveFolderTreeAsync(
        GraphServiceClient graphClient,
        string driveId,
        string rootFolderPath,
        int maxDepth)
    {
        var result = new List<string> { rootFolderPath };
        if (maxDepth <= 0)
            return result;

        var children = await graphClient.Drives[driveId].Root.ItemWithPath(rootFolderPath).Children.GetAsync();
        foreach (var folder in children?.Value?.Where(item => item.Folder is not null) ?? [])
        {
            var childPath = BuildRemotePath(rootFolderPath, folder.Name);
            result.AddRange(await ResolveFolderTreeAsync(graphClient, driveId, childPath, maxDepth - 1));
        }

        return result;
    }

    private static bool IsAlphaplanInvoiceFile(string? fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty);
        return name.Equals("invoice_headers.csv", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("invoice_lines.csv", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildAlphaplanFolderSortKey(string rootFolderPath, string folderPath)
    {
        var relative = folderPath.Trim('/');
        var root = rootFolderPath.Trim('/');
        if (relative.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            relative = relative[root.Length..].Trim('/');

        return string.IsNullOrWhiteSpace(relative)
            ? "0_full"
            : relative.Replace('\\', '/').Contains("delta", StringComparison.OrdinalIgnoreCase)
                ? $"1_{relative}"
                : $"2_{relative}";
    }

    private static string BuildRemotePath(string folderPath, string? fileName)
        => string.Join("/", folderPath.Trim('/'), (fileName ?? string.Empty).Trim('/')).Trim('/');

    private static bool IsSpainSalesFile(string? fileName)
        => Path.GetFileName(fileName ?? string.Empty).StartsWith("Spain_Sales", StringComparison.OrdinalIgnoreCase) &&
           Path.GetExtension(fileName ?? string.Empty).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseSpainSalesRangeFileName(string? fileName, out DateTime rangeStart, out DateTime rangeEnd)
    {
        rangeStart = default;
        rangeEnd = default;
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        var match = Regex.Match(nameWithoutExtension, @"^Spain_Sales_range_(?<from>\d{8})_to_(?<to>\d{8})$", RegexOptions.IgnoreCase);
        if (!match.Success)
            return false;

        return DateTime.TryParseExact(
                   match.Groups["from"].Value,
                   "yyyyMMdd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out rangeStart) &&
               DateTime.TryParseExact(
                   match.Groups["to"].Value,
                   "yyyyMMdd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out rangeEnd);
    }

    private static bool TryParseDatedSiteFileName(string? fileName, string normalizedTsc, out DateTime fileDate)
    {
        fileDate = default;
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        var pattern = string.IsNullOrWhiteSpace(normalizedTsc)
            ? @"^(?<date>\d{6})_[A-Z0-9]+$"
            : $"^(?<date>\\d{{6}})_{Regex.Escape(normalizedTsc)}$";
        var match = Regex.Match(nameWithoutExtension, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
            return false;

        return DateTime.TryParseExact(
            match.Groups["date"].Value,
            "ddMMyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out fileDate);
    }

    private static bool TryParseAnnualSiteFileName(string? fileName, string normalizedTsc, out int year)
    {
        year = default;
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (!Regex.IsMatch(nameWithoutExtension, $@"(^|[^A-Z0-9]){Regex.Escape(normalizedTsc)}([^A-Z0-9]|$)", RegexOptions.IgnoreCase))
            return false;
        if (TryParseDatedSiteFileName(fileName, normalizedTsc, out _))
            return false;

        var match = Regex.Match(nameWithoutExtension, @"(?<!\d)(20\d{2})(?!\d)");
        return match.Success && int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out year);
    }

    private static bool TryParseSnapshotDate(string? fileName, out DateTime snapshotDate)
    {
        snapshotDate = default;
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        var match = Regex.Match(nameWithoutExtension, @"(?<!\d)(?<date>20\d{2}[-_.]\d{2}[-_.]\d{2})(?!\d)");
        return match.Success &&
               DateTime.TryParseExact(
                   match.Groups["date"].Value.Replace('_', '-').Replace('.', '-'),
                   "yyyy-MM-dd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out snapshotDate);
    }

    private static string BuildInputPreview(string tenantId, string clientId, string clientSecret, string siteUrl)
    {
        var maskedSecret = string.IsNullOrEmpty(clientSecret)
            ? "<leer>"
            : $"{new string('*', Math.Min(clientSecret.Length, 8))} (len={clientSecret.Length})";

        return $"Uebergeben: TenantId='{tenantId}', ClientId='{clientId}', ClientSecret={maskedSecret}, SiteUrl='{siteUrl}'";
    }
}
