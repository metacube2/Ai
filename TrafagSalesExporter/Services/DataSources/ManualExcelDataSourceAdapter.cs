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
        string? tempManualImportPath = null;
        try
        {
            if (File.Exists(manualImportPath))
            {
                filePath = manualImportPath;
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

                tempManualImportPath = await _sharePointService.DownloadToTempFileAsync(
                    spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                    spConfig.SiteUrl, manualImportPath);
                filePath = manualImportPath;
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
                ReferenceFilePath = filePath
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
}
