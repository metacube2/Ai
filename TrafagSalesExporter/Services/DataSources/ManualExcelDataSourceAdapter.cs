using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.DataSources;

public sealed class ManualExcelDataSourceAdapter : IDataSourceAdapter
{
    private readonly ISharePointUploadService _sharePointService;
    private readonly IManualExcelImportService _manualExcelImportService;
    private readonly IAppEventLogService _appEventLogService;

    public ManualExcelDataSourceAdapter(
        ISharePointUploadService sharePointService,
        IManualExcelImportService manualExcelImportService,
        IAppEventLogService appEventLogService)
    {
        _sharePointService = sharePointService;
        _manualExcelImportService = manualExcelImportService;
        _appEventLogService = appEventLogService;
    }

    public string ConnectionKind => SourceSystemConnectionKinds.ManualExcel;

    public async Task<DataSourceFetchResult> FetchAsync(DataSourceFetchContext context)
    {
        var site = context.Site;

        if (string.IsNullOrWhiteSpace(site.ManualImportFilePath))
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine manuelle Excel-Datei.");

        var manualImportPath = site.ManualImportFilePath.Trim();
        string filePath;
        string? localOutputDirectory = null;
        string? sharePointUploadFolder = null;
        string? tempManualImportPath = null;
        try
        {
            if (File.Exists(manualImportPath))
            {
                filePath = manualImportPath;
                localOutputDirectory = Path.GetDirectoryName(Path.GetFullPath(manualImportPath));
            }
            else if (LooksLikeSharePointReference(manualImportPath))
            {
                var spConfig = context.SharePointConfig
                    ?? throw new InvalidOperationException(
                        "Fuer SharePoint-Manuellimport fehlt eine vollstaendige SharePoint-Konfiguration in Settings.");

                if (string.IsNullOrWhiteSpace(spConfig.TenantId) ||
                    string.IsNullOrWhiteSpace(spConfig.ClientId) ||
                    string.IsNullOrWhiteSpace(spConfig.ClientSecret) ||
                    string.IsNullOrWhiteSpace(spConfig.SiteUrl))
                {
                    throw new InvalidOperationException(
                        "Fuer SharePoint-Manuellimport fehlt eine vollstaendige SharePoint-Konfiguration in Settings.");
                }

                context.UpdateStatus?.Invoke("Manuelle Excel von SharePoint laden...");
                await _appEventLogService.WriteAsync("Export", "Manuelle Excel von SharePoint laden",
                    siteId: site.Id, land: site.Land, details: manualImportPath);

                var sharePointFileReference = manualImportPath;
                if (LooksLikeSharePointFolderReference(manualImportPath))
                {
                    var latestFile = await _sharePointService.ResolveLatestFileInFolderAsync(
                        spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                        spConfig.SiteUrl, manualImportPath, site.TSC);
                    sharePointFileReference = latestFile.FileReference;
                    await _appEventLogService.WriteAsync("Export", "Neueste SharePoint-Datei ausgewaehlt",
                        siteId: site.Id, land: site.Land, details: sharePointFileReference);
                }

                tempManualImportPath = await _sharePointService.DownloadToTempFileAsync(
                    spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                    spConfig.SiteUrl, sharePointFileReference);
                filePath = sharePointFileReference;
                sharePointUploadFolder = ResolveSharePointParentFolder(sharePointFileReference, spConfig.SiteUrl);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Die manuelle Excel-Datei wurde nicht gefunden: {manualImportPath}");
            }

            var readPath = tempManualImportPath ?? filePath;
            context.UpdateStatus?.Invoke("Manuelle Excel lesen...");
            await _appEventLogService.WriteAsync("Export", "Manuelle Excel lesen",
                siteId: site.Id, land: site.Land, details: filePath);

            var records = await _manualExcelImportService.ReadSalesRecordsAsync(readPath, site);
            return new DataSourceFetchResult
            {
                Records = records,
                LocalOutputDirectoryOverride = localOutputDirectory,
                SharePointUploadFolderOverride = sharePointUploadFolder,
                SharePointUploadLandOverride = sharePointUploadFolder is null ? null : string.Empty
            };
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempManualImportPath) && File.Exists(tempManualImportPath))
                File.Delete(tempManualImportPath);
        }
    }

    private static bool LooksLikeSharePointReference(string path)
        => path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("/Shared Documents/", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("Shared Documents/", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeSharePointFolderReference(string path)
        => LooksLikeSharePointReference(path) &&
           string.IsNullOrWhiteSpace(Path.GetExtension(path.TrimEnd('/')));

    private static string ResolveSharePointParentFolder(string fileReference, string siteUrl)
    {
        var remotePath = fileReference.Trim('/').Trim();
        if (Uri.TryCreate(fileReference, UriKind.Absolute, out var fileUri) &&
            Uri.TryCreate(siteUrl, UriKind.Absolute, out var siteUri))
        {
            var absolutePath = Uri.UnescapeDataString(fileUri.AbsolutePath);
            var sitePath = siteUri.AbsolutePath.TrimEnd('/');
            if (absolutePath.StartsWith(sitePath, StringComparison.OrdinalIgnoreCase))
                absolutePath = absolutePath[sitePath.Length..];
            remotePath = absolutePath.Trim('/').Trim();
        }

        var lastSlash = remotePath.LastIndexOf('/');
        return lastSlash <= 0 ? string.Empty : remotePath[..lastSlash];
    }
}
