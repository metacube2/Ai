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
        var localManualImportPaths = new List<string>();
        var tempManualImportPaths = new List<string>();
        try
        {
            if (File.Exists(manualImportPath))
            {
                filePath = manualImportPath;
                localOutputDirectory = Path.GetDirectoryName(Path.GetFullPath(manualImportPath));
            }
            else if (Directory.Exists(manualImportPath))
            {
                localManualImportPaths.AddRange(ResolveLocalManualImportFilesInFolder(manualImportPath, site));
                filePath = manualImportPath;
                localOutputDirectory = Path.GetFullPath(manualImportPath);
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
                var sharePointFileReferences = new List<string>();
                if (LooksLikeSharePointFolderReference(manualImportPath))
                {
                    var files = await _sharePointService.ResolveManualImportFilesInFolderAsync(
                        spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                        spConfig.SiteUrl, manualImportPath, site.TSC, context.PreferredImportYear);
                    sharePointFileReferences.AddRange(files.Select(file => file.FileReference));
                    sharePointFileReference = sharePointFileReferences.FirstOrDefault() ?? manualImportPath;
                    await _appEventLogService.WriteAsync("Export", "Neueste SharePoint-Datei ausgewaehlt",
                        siteId: site.Id, land: site.Land, details: string.Join(" | ", sharePointFileReferences));
                }
                else
                {
                    sharePointFileReferences.Add(sharePointFileReference);
                }

                foreach (var fileReference in sharePointFileReferences)
                {
                    tempManualImportPaths.Add(await _sharePointService.DownloadToTempFileAsync(
                        spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                        spConfig.SiteUrl, fileReference));
                }
                filePath = sharePointFileReference;
                sharePointUploadFolder = ResolveSharePointParentFolder(sharePointFileReference, spConfig.SiteUrl);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Die manuelle Excel-Datei wurde nicht gefunden: {manualImportPath}");
            }

            context.UpdateStatus?.Invoke("Manuelle Excel lesen...");
            await _appEventLogService.WriteAsync("Export", "Manuelle Excel lesen",
                siteId: site.Id, land: site.Land, details: filePath);

            var records = new List<SalesRecord>();
            var readPaths = tempManualImportPaths.Count > 0
                ? tempManualImportPaths
                : localManualImportPaths.Count > 0
                    ? localManualImportPaths
                    : [filePath];
            foreach (var readPath in readPaths)
                records.AddRange(await _manualExcelImportService.ReadSalesRecordsAsync(readPath, site));
            if (IsSpainSite(site))
                records = DeduplicateSpainSalesRecords(records);
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
            foreach (var tempManualImportPath in tempManualImportPaths)
            {
                if (File.Exists(tempManualImportPath))
                    File.Delete(tempManualImportPath);
            }
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

    private static List<string> ResolveLocalManualImportFilesInFolder(string folderPath, Site site)
    {
        var files = Directory.EnumerateFiles(folderPath)
            .Where(IsSupportedManualImportFile)
            .Where(path => !IsSpainSite(site) || IsSpainSalesFile(path))
            .OrderBy(GetManualImportFileSortKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            var expected = IsSpainSite(site) ? "Spain_Sales*.csv" : "*.xlsx/*.csv";
            throw new InvalidOperationException($"Im Ordner '{folderPath}' wurde keine passende Importdatei gefunden ({expected}).");
        }

        return files;
    }

    private static bool IsSupportedManualImportFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSpainSite(Site site)
        => string.Equals(site.TSC, "TRES", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(site.TSC, "TRSE", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(site.Land, "Spanien", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(site.Land, "Spain", StringComparison.OrdinalIgnoreCase);

    private static bool IsSpainSalesFile(string path)
        => Path.GetFileName(path).StartsWith("Spain_Sales", StringComparison.OrdinalIgnoreCase) &&
           Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    private static string GetManualImportFileSortKey(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var rangeIndex = name.IndexOf("_range_", StringComparison.OrdinalIgnoreCase);
        if (rangeIndex >= 0)
            return "1_" + name[(rangeIndex + "_range_".Length)..];

        return "0_" + name;
    }

    private static List<SalesRecord> DeduplicateSpainSalesRecords(IEnumerable<SalesRecord> records)
    {
        var ordered = records.ToList();
        var keyed = new Dictionary<string, SalesRecord>(StringComparer.OrdinalIgnoreCase);
        var unkeyed = new List<SalesRecord>();

        foreach (var record in ordered)
        {
            var key = BuildSpainSalesRecordKey(record);
            if (string.IsNullOrWhiteSpace(key))
                unkeyed.Add(record);
            else
                keyed[key] = record;
        }

        return keyed.Values.Concat(unkeyed).ToList();
    }

    private static string BuildSpainSalesRecordKey(SalesRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.SourceLineId))
            return $"source:{record.SourceLineId.Trim()}";

        if (!string.IsNullOrWhiteSpace(record.InvoiceNumber))
            return string.Join("|",
                "invoice",
                record.Tsc?.Trim() ?? string.Empty,
                record.InvoiceNumber.Trim(),
                record.PositionOnInvoice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                record.Material?.Trim() ?? string.Empty);

        return string.Empty;
    }

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
