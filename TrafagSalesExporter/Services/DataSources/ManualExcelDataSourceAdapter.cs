using System.Globalization;
using System.IO.Compression;
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
        string? tempManualImportRoot = null;
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

                if (LooksLikeSharePointFolderReference(manualImportPath) && IsAlphaplanGermanySite(site))
                {
                    tempManualImportRoot = Path.Combine(Path.GetTempPath(), $"manual-import-{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempManualImportRoot);
                }

                foreach (var fileReference in sharePointFileReferences)
                {
                    var downloadedPath = await _sharePointService.DownloadToTempFileAsync(
                        spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                        spConfig.SiteUrl, fileReference);

                    if (tempManualImportRoot is null)
                    {
                        tempManualImportPaths.Add(downloadedPath);
                    }
                    else
                    {
                        var preservedPath = PreserveSharePointDownloadPath(
                            downloadedPath, tempManualImportRoot, manualImportPath, fileReference, spConfig.SiteUrl);
                        tempManualImportPaths.Add(preservedPath);
                        if (IsAlphaplanGermanySite(site) && IsZipFile(preservedPath))
                            tempManualImportPaths.AddRange(ExtractAlphaplanZipArchive(preservedPath, tempManualImportRoot, fileReference));
                    }
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
            var candidateReadPaths = tempManualImportPaths.Count > 0
                ? tempManualImportPaths
                : localManualImportPaths.Count > 0
                    ? localManualImportPaths
                    : [filePath];
            var readPaths = SelectManualImportReadPaths(candidateReadPaths, site);
            foreach (var readPath in readPaths)
                records.AddRange(await _manualExcelImportService.ReadSalesRecordsAsync(readPath, site));
            records = await RemoveTemplateDescriptionRowsAsync(records, site, context);
            if (IsSpainSite(site))
                records = DeduplicateSpainSalesRecords(records);
            else if (IsAlphaplanGermanySite(site))
            {
                records = FilterAlphaplanRecordsByDate(records, context);
                records = DeduplicateManualSalesRecords(records);
            }
            else if (readPaths.Count > 1)
            {
                // Basis+Delta-Modell (UK): mehrere Dateien werden zusammengelesen,
                // Delta-Zeilen gewinnen gegen aeltere Zeilen mit demselben Beleg-Schluessel.
                records = DeduplicateManualSalesRecords(records);
            }
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

            if (!string.IsNullOrWhiteSpace(tempManualImportRoot) &&
                Directory.Exists(tempManualImportRoot))
            {
                Directory.Delete(tempManualImportRoot, recursive: true);
            }
        }
    }

    /// <summary>
    /// Entfernt die Legendenzeile, die manche Standort-Vorlagen als ERSTE Datenzeile unter
    /// die Kopfzeile schreiben (`TSC = "Subsidiary abbreviation / company identifier"`,
    /// `Invoice Number = "Unique invoice document number in local ERP"`, ...).
    ///
    /// Sie stammt aus der Vorlage der Standorte, nicht aus dieser App, und wurde am
    /// 2026-07-28 mit dem UK-Backfill als echter Umsatzsatz importiert (Id 2'841'937,
    /// `Land = England`). Ohne diesen Filter kaeme sie bei jedem Reimport zurueck, weil
    /// <see cref="ICentralSalesRecordService.ReplaceForSiteAsync"/> zwar alles zum Standort
    /// loescht, die Datei aber unveraendert bleibt.
    /// </summary>
    private async Task<List<SalesRecord>> RemoveTemplateDescriptionRowsAsync(
        List<SalesRecord> records, Site site, DataSourceFetchContext context)
    {
        var cleaned = records.Where(record => !IsTemplateDescriptionRow(record)).ToList();
        var removed = records.Count - cleaned.Count;
        if (removed > 0)
        {
            context.UpdateStatus?.Invoke($"Legendenzeilen entfernt: {removed}");
            await _appEventLogService.WriteAsync("Export", "Legendenzeile verworfen",
                siteId: site.Id, land: site.Land,
                details: $"Zeilen={removed} | Beschreibungszeile der Standort-Vorlage, kein Umsatzsatz.");
        }

        return cleaned;
    }

    /// <summary>
    /// Erkennt die Beschreibungszeile am TSC-Feld. Echte Werte sind kurze Codes ohne
    /// Leerzeichen (`TRUK`, `TRES`, `TRDE`); die Legende traegt dort einen ganzen Satz.
    /// BEWUSST NICHT gegen <c>site.TSC</c> verglichen: der Spanien-Standort heisst in
    /// <c>Sites</c> `TRSE`, liefert in den Daten aber `TRES` — ein Gleichheitstest wuerde
    /// dort saemtliche Zeilen verwerfen.
    /// </summary>
    public static bool IsTemplateDescriptionRow(SalesRecord record)
    {
        var tsc = record.Tsc?.Trim() ?? string.Empty;
        return tsc.Length > 12 && tsc.Any(char.IsWhiteSpace);
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
        if (IsAlphaplanGermanySite(site))
        {
            var alphaplanFiles = ResolveAlphaplanInvoiceLineFilesInFolder(folderPath);
            if (alphaplanFiles.Count > 0)
                return alphaplanFiles;
        }

        var normalizedTsc = site.TSC?.Trim().ToUpperInvariant() ?? string.Empty;
        var files = Directory.EnumerateFiles(folderPath)
            .Where(IsSupportedManualImportFile)
            .Where(path => !SharePointUploadService.IsOwnExportOutputFile(path, normalizedTsc))
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

    private static List<string> SelectManualImportReadPaths(IEnumerable<string> paths, Site site)
    {
        var pathList = paths.ToList();
        return IsAlphaplanGermanySite(site)
            ? ResolveAlphaplanInvoiceLineFiles(pathList)
            : pathList;
    }

    private static List<string> ExtractAlphaplanZipArchive(string zipPath, string tempRoot, string fileReference)
    {
        var archiveName = Path.GetFileNameWithoutExtension(fileReference);
        if (string.IsNullOrWhiteSpace(archiveName))
            archiveName = Path.GetFileNameWithoutExtension(zipPath);

        var baseFolder = IsAlphaplanDeltaZipFile(fileReference)
            ? Path.Combine(tempRoot, "delta", archiveName)
            : Path.Combine(tempRoot, archiveName);

        Directory.CreateDirectory(baseFolder);
        ZipFile.ExtractToDirectory(zipPath, baseFolder, overwriteFiles: true);
        return Directory.EnumerateFiles(baseFolder, "*.*", SearchOption.AllDirectories)
            .Where(IsSupportedManualImportFile)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsZipFile(string path)
        => Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    private static bool IsAlphaplanDeltaZipFile(string path)
    {
        var normalized = path.Replace('\\', '/');
        var fileName = Path.GetFileName(path);
        return normalized.Contains("/delta/", StringComparison.OrdinalIgnoreCase) ||
               fileName.Contains("Delta", StringComparison.OrdinalIgnoreCase);
    }
    private static List<string> ResolveAlphaplanInvoiceLineFilesInFolder(string folderPath)
    {
        var files = Directory.EnumerateFiles(folderPath, "invoice_lines.csv", SearchOption.AllDirectories)
            .Where(HasSiblingAlphaplanHeaderFile)
            .OrderBy(GetAlphaplanImportFileSortKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return files;
    }

    private static List<string> ResolveAlphaplanInvoiceLineFiles(IEnumerable<string> paths)
    {
        var pathList = paths.ToList();
        var files = pathList
            .Where(path => string.Equals(Path.GetFileName(path), "invoice_lines.csv", StringComparison.OrdinalIgnoreCase))
            .Where(HasSiblingAlphaplanHeaderFile)
            .OrderBy(GetAlphaplanImportFileSortKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count > 0)
            return files;

        if (pathList.Any(path => IsSupportedManualImportFile(path) && !IsAlphaplanInvoiceFileName(path)))
            return pathList;

        throw new InvalidOperationException("Es wurde kein Alphaplan-Paar invoice_headers.csv/invoice_lines.csv gefunden.");
    }

    private static bool HasSiblingAlphaplanHeaderFile(string lineFilePath)
    {
        var folder = Path.GetDirectoryName(lineFilePath);
        return !string.IsNullOrWhiteSpace(folder) &&
               File.Exists(Path.Combine(folder, "invoice_headers.csv"));
    }

    private static bool IsAlphaplanInvoiceFileName(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Equals("invoice_headers.csv", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("invoice_lines.csv", StringComparison.OrdinalIgnoreCase);
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

    private static bool IsAlphaplanGermanySite(Site site)
        => string.Equals(site.TSC, "TRDE", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(site.Land, "Deutschland", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(site.Land, "Germany", StringComparison.OrdinalIgnoreCase);

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

    private static string GetAlphaplanImportFileSortKey(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var parentName = new DirectoryInfo(directory).Name;
        if (parentName.Equals("delta", StringComparison.OrdinalIgnoreCase))
            return "1_delta";

        return directory.Contains($"{Path.DirectorySeparatorChar}delta{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
               directory.Contains($"{Path.AltDirectorySeparatorChar}delta{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            ? "1_delta"
            : "0_full";
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

    private static List<SalesRecord> DeduplicateManualSalesRecords(IEnumerable<SalesRecord> records)
    {
        var keyed = new Dictionary<string, SalesRecord>(StringComparer.OrdinalIgnoreCase);
        var unkeyed = new List<SalesRecord>();

        foreach (var record in records)
        {
            var key = BuildManualSalesRecordKey(record);
            if (string.IsNullOrWhiteSpace(key))
                unkeyed.Add(record);
            else
                keyed[key] = record;
        }

        return keyed.Values.Concat(unkeyed).ToList();
    }

    private static string BuildSpainSalesRecordKey(SalesRecord record)
        => BuildManualSalesRecordKey(record);

    private static string BuildManualSalesRecordKey(SalesRecord record)
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

    private static List<SalesRecord> FilterAlphaplanRecordsByDate(IEnumerable<SalesRecord> records, DataSourceFetchContext context)
    {
        var (startInclusive, endExclusive) = ResolveAlphaplanDateRange(context);
        if (startInclusive is null && endExclusive is null)
            return records.ToList();

        return records
            .Where(record =>
            {
                var date = (record.PostingDate ?? record.InvoiceDate ?? record.ExtractionDate).Date;
                return (startInclusive is null || date >= startInclusive.Value.Date) &&
                       (endExclusive is null || date < endExclusive.Value.Date);
            })
            .ToList();
    }

    private static (DateTime? StartInclusive, DateTime? EndExclusive) ResolveAlphaplanDateRange(DataSourceFetchContext context)
    {
        if (context.PreferredImportYear is > 0)
        {
            var start = new DateTime(context.PreferredImportYear.Value, 1, 1);
            return (start, start.AddYears(1));
        }

        if (DateTime.TryParse(context.Settings.DateFilter, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var invariantDate) ||
            DateTime.TryParse(context.Settings.DateFilter, CultureInfo.GetCultureInfo("de-CH"), DateTimeStyles.AssumeLocal, out invariantDate) ||
            DateTime.TryParse(context.Settings.DateFilter, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.AssumeLocal, out invariantDate))
        {
            return (invariantDate.Date, null);
        }

        return (null, null);
    }

    private static string PreserveSharePointDownloadPath(
        string downloadedPath,
        string tempRoot,
        string folderReference,
        string fileReference,
        string siteUrl)
    {
        var relativePath = ResolveSharePointRelativePath(folderReference, fileReference, siteUrl);
        var localRelativePath = relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(localRelativePath))
            localRelativePath = Path.GetFileName(fileReference);

        var targetPath = Path.Combine(tempRoot, localRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? tempRoot);
        File.Copy(downloadedPath, targetPath, overwrite: true);
        File.Delete(downloadedPath);
        return targetPath;
    }

    private static string ResolveSharePointRelativePath(string folderReference, string fileReference, string siteUrl)
    {
        var folderPath = ResolveSharePointPath(folderReference, siteUrl).Trim('/');
        var filePath = ResolveSharePointPath(fileReference, siteUrl).Trim('/');

        if (!string.IsNullOrWhiteSpace(folderPath) &&
            filePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
        {
            return filePath[folderPath.Length..].Trim('/');
        }

        return Path.GetFileName(filePath);
    }

    private static string ResolveSharePointPath(string reference, string siteUrl)
    {
        var remotePath = reference.Trim('/').Trim();
        if (Uri.TryCreate(reference, UriKind.Absolute, out var fileUri) &&
            Uri.TryCreate(siteUrl, UriKind.Absolute, out var siteUri))
        {
            var absolutePath = Uri.UnescapeDataString(fileUri.AbsolutePath);
            var sitePath = siteUri.AbsolutePath.TrimEnd('/');
            if (absolutePath.StartsWith(sitePath, StringComparison.OrdinalIgnoreCase))
                absolutePath = absolutePath[sitePath.Length..];
            remotePath = absolutePath.Trim('/').Trim();
        }

        return remotePath;
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
