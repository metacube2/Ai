using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class SiteExportService : ISiteExportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHanaQueryService _hanaService;
    private readonly ISapGatewayService _sapGatewayService;
    private readonly ISapCompositionService _sapCompositionService;
    private readonly IExcelExportService _excelService;
    private readonly ISharePointUploadService _sharePointService;
    private readonly IRecordTransformationService _transformationService;
    private readonly ICentralSalesRecordService _centralSalesRecordService;
    private readonly IManualExcelImportService _manualExcelImportService;
    private readonly IAppEventLogService _appEventLogService;
    private readonly ILogger<SiteExportService> _logger;

    public SiteExportService(
        IDbContextFactory<AppDbContext> dbFactory,
        IHanaQueryService hanaService,
        ISapGatewayService sapGatewayService,
        ISapCompositionService sapCompositionService,
        IExcelExportService excelService,
        ISharePointUploadService sharePointService,
        IRecordTransformationService transformationService,
        ICentralSalesRecordService centralSalesRecordService,
        IManualExcelImportService manualExcelImportService,
        IAppEventLogService appEventLogService,
        ILogger<SiteExportService> logger)
    {
        _dbFactory = dbFactory;
        _hanaService = hanaService;
        _sapGatewayService = sapGatewayService;
        _sapCompositionService = sapCompositionService;
        _excelService = excelService;
        _sharePointService = sharePointService;
        _transformationService = transformationService;
        _centralSalesRecordService = centralSalesRecordService;
        _manualExcelImportService = manualExcelImportService;
        _appEventLogService = appEventLogService;
        _logger = logger;
    }

    public async Task<SiteExportResult> ExportAsync(Site site, Action<string>? updateStatus = null)
    {
        var sw = Stopwatch.StartNew();
        var log = new ExportLog
        {
            Timestamp = DateTime.Now,
            SiteId = site.Id,
            Land = site.Land,
            TSC = site.TSC
        };

        try
        {
            await _appEventLogService.WriteAsync("Export", "Export gestartet", siteId: site.Id, land: site.Land,
                details: $"Quelle={NormalizeSourceSystem(site.SourceSystem)} | TSC={site.TSC}");
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.ExportSettings.FirstOrDefaultAsync() ?? new ExportSettings();
            var spConfig = await db.SharePointConfigs.FirstOrDefaultAsync();
            var outputDir = ResolveSiteOutputDirectory(settings, site);
            var sourceSystem = NormalizeSourceSystem(site.SourceSystem);
            var sourceDefinition = await db.SourceSystemDefinitions
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(x => x.Code == sourceSystem)
                ?? throw new InvalidOperationException($"Quellsystem '{sourceSystem}' ist nicht konfiguriert.");
            var records = new List<SalesRecord>();
            string filePath;

            if (string.Equals(sourceDefinition.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase))
            {
                var credentials = ResolveCredentials(site, sourceDefinition);
                var sapServiceUrl = ResolveSapServiceUrl(site, sourceDefinition);
                if (string.IsNullOrWhiteSpace(sapServiceUrl))
                    throw new InvalidOperationException($"Standort '{site.Land}' hat keine SAP Service URL.");
                var sapSources = await db.SapSourceDefinitions.Where(s => s.SiteId == site.Id).ToListAsync();
                var sapJoins = await db.SapJoinDefinitions.Where(j => j.SiteId == site.Id).ToListAsync();
                var sapMappings = await db.SapFieldMappings.Where(m => m.SiteId == site.Id).ToListAsync();
                if (sapSources.Count == 0)
                    throw new InvalidOperationException($"Standort '{site.Land}' hat keine SAP-Quellen konfiguriert.");
                if (sapMappings.Count == 0)
                    throw new InvalidOperationException($"Standort '{site.Land}' hat keine SAP-Feldmappings.");

                updateStatus?.Invoke("SAP Quellen laden...");
                await _appEventLogService.WriteAsync("Export", "SAP Quellen laden", siteId: site.Id, land: site.Land,
                    details: $"Sources={sapSources.Count} | Mappings={sapMappings.Count}");
                var effectiveSite = CloneSiteWithSapServiceUrl(site, sapServiceUrl);
                records = await _sapCompositionService.BuildSalesRecordsAsync(effectiveSite, sapSources, sapJoins, sapMappings, credentials.Username, credentials.Password);
                updateStatus?.Invoke("Transformationen anwenden...");
                await _appEventLogService.WriteAsync("Export", "Transformationen anwenden", siteId: site.Id, land: site.Land,
                    details: $"Records vor Transformation={records.Count}");
                var rules = await db.FieldTransformationRules
                    .Where(r => r.IsActive && r.SourceSystem == sourceSystem)
                    .OrderBy(r => r.SortOrder)
                    .ToListAsync();
                _transformationService.Apply(records, rules);
                updateStatus?.Invoke("Excel erstellen...");
                await _appEventLogService.WriteAsync("Export", "Excel erstellen", siteId: site.Id, land: site.Land,
                    details: $"Records={records.Count}");
                filePath = _excelService.CreateExcelFile(outputDir, site.TSC, DateTime.UtcNow.Date, records);
                log.RowCount = records.Count;
            }
            else if (string.Equals(sourceDefinition.ConnectionKind, SourceSystemConnectionKinds.ManualExcel, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(site.ManualImportFilePath))
                    throw new InvalidOperationException($"Standort '{site.Land}' hat keine manuelle Excel-Datei.");
                if (!File.Exists(site.ManualImportFilePath))
                    throw new InvalidOperationException($"Die manuelle Excel-Datei wurde nicht gefunden: {site.ManualImportFilePath}");

                updateStatus?.Invoke("Manuelle Excel lesen...");
                await _appEventLogService.WriteAsync("Export", "Manuelle Excel lesen", siteId: site.Id, land: site.Land,
                    details: site.ManualImportFilePath);
                records = await _manualExcelImportService.ReadSalesRecordsAsync(site.ManualImportFilePath, site);

                updateStatus?.Invoke("Transformationen anwenden...");
                await _appEventLogService.WriteAsync("Export", "Transformationen anwenden", siteId: site.Id, land: site.Land,
                    details: $"Records vor Transformation={records.Count}");
                var rules = await db.FieldTransformationRules
                    .Where(r => r.IsActive && r.SourceSystem == sourceSystem)
                    .OrderBy(r => r.SortOrder)
                    .ToListAsync();
                _transformationService.Apply(records, rules);

                filePath = site.ManualImportFilePath;
                log.RowCount = records.Count;
            }
            else
            {
                var exportServer = await BuildEffectiveServerAsync(db, site, sourceDefinition);
                updateStatus?.Invoke("HANA Abfrage...");
                await _appEventLogService.WriteAsync("Export", "HANA Abfrage gestartet", siteId: site.Id, land: site.Land,
                    details: exportServer.GetConnectionStringPreview());
                records = await Task.Run(() => _hanaService.GetSalesRecords(
                    exportServer, site.Schema, site.TSC, site.Land, settings.DateFilter));

                updateStatus?.Invoke("Transformationen anwenden...");
                await _appEventLogService.WriteAsync("Export", "Transformationen anwenden", siteId: site.Id, land: site.Land,
                    details: $"Records vor Transformation={records.Count}");
                var rules = await db.FieldTransformationRules
                    .Where(r => r.IsActive && r.SourceSystem == sourceSystem)
                    .OrderBy(r => r.SortOrder)
                    .ToListAsync();
                _transformationService.Apply(records, rules);

                updateStatus?.Invoke("Excel erstellen...");
                await _appEventLogService.WriteAsync("Export", "Excel erstellen", siteId: site.Id, land: site.Land,
                    details: $"Records={records.Count}");
                filePath = _excelService.CreateExcelFile(outputDir, site.TSC, DateTime.UtcNow.Date, records);
                log.RowCount = records.Count;
            }

            updateStatus?.Invoke("Zentrale Tabelle aktualisieren...");
            await _appEventLogService.WriteAsync("Export", "Zentrale Tabelle aktualisieren", siteId: site.Id, land: site.Land,
                details: $"Records={records.Count}");
            await _centralSalesRecordService.ReplaceForSiteAsync(site, records, updateStatus);

            var fileName = Path.GetFileName(filePath);

            if (spConfig is not null &&
                !string.IsNullOrWhiteSpace(spConfig.TenantId) &&
                !string.IsNullOrWhiteSpace(spConfig.ClientId) &&
                !string.IsNullOrWhiteSpace(spConfig.ClientSecret))
            {
                updateStatus?.Invoke("SharePoint Upload...");
                await _appEventLogService.WriteAsync("Export", "SharePoint Upload gestartet", siteId: site.Id, land: site.Land,
                    details: $"{spConfig.SiteUrl} | {spConfig.ExportFolder}");
                await _sharePointService.UploadAsync(
                    spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                    spConfig.SiteUrl, spConfig.ExportFolder, site.Land, filePath);
            }

            sw.Stop();
            log.Status = "OK";
            log.FileName = fileName;
            log.FilePath = filePath;
            log.DurationSeconds = sw.Elapsed.TotalSeconds;

            _logger.LogInformation("Export OK: {Land} ({TSC}) - {Rows} Zeilen in {Duration:F1}s",
                site.Land, site.TSC, log.RowCount, sw.Elapsed.TotalSeconds);
            await _appEventLogService.WriteAsync("Export", "Export erfolgreich", siteId: site.Id, land: site.Land,
                details: $"Rows={log.RowCount} | Datei={fileName} | Pfad={filePath} | Dauer={sw.Elapsed.TotalSeconds:F1}s");

            return new SiteExportResult
            {
                Records = records,
                Log = log,
                FilePath = filePath
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            log.Status = "Error";
            log.ErrorMessage = ex.Message;
            log.FileName = string.Empty;
            log.FilePath = string.Empty;
            log.DurationSeconds = sw.Elapsed.TotalSeconds;

            _logger.LogError(ex, "Export Fehler: {Land} ({TSC})", site.Land, site.TSC);
            await _appEventLogService.WriteAsync("Export", "Export fehlgeschlagen", "Error", siteId: site.Id, land: site.Land,
                details: ex.ToString());

            return new SiteExportResult
            {
                Records = [],
                Log = log,
                FilePath = null
            };
        }
    }

    private static async Task<HanaServer> BuildEffectiveServerAsync(AppDbContext db, Site site, SourceSystemDefinition sourceDefinition)
    {
        var centralServer = await db.HanaServers
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.SourceSystem == sourceDefinition.Code);

        if (centralServer is null)
            throw new InvalidOperationException($"Fuer Quellsystem '{sourceDefinition.Code}' ist keine zentrale HANA-Konfiguration vorhanden.");

        var credentials = ResolveCredentials(site, sourceDefinition);

        return new HanaServer
        {
            Id = centralServer.Id,
            SourceSystem = centralServer.SourceSystem,
            Name = centralServer.Name,
            Host = centralServer.Host,
            Port = centralServer.Port,
            Username = credentials.Username,
            Password = credentials.Password,
            DatabaseName = centralServer.DatabaseName,
            UseSsl = centralServer.UseSsl,
            ValidateCertificate = centralServer.ValidateCertificate,
            AdditionalParams = centralServer.AdditionalParams
        };
    }

    private static (string Username, string Password) ResolveCredentials(Site site, SourceSystemDefinition sourceDefinition)
        => (FirstNonEmpty(site.UsernameOverride, sourceDefinition.CentralUsername),
            FirstNonEmpty(site.PasswordOverride, sourceDefinition.CentralPassword));

    private static string ResolveSapServiceUrl(Site site, SourceSystemDefinition sourceDefinition)
        => FirstNonEmpty(site.SapServiceUrl, sourceDefinition.CentralServiceUrl);

    private static string NormalizeSourceSystem(string? sourceSystem)
        => string.IsNullOrWhiteSpace(sourceSystem) ? "SAP" : sourceSystem.Trim().ToUpperInvariant();

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static string ResolveSiteOutputDirectory(ExportSettings settings, Site site)
    {
        var configured = FirstNonEmpty(site.LocalExportFolderOverride, settings.LocalSiteExportFolder);
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "output")
            : configured;
    }

    private static Site CloneSiteWithSapServiceUrl(Site site, string sapServiceUrl)
    {
        return new Site
        {
            Id = site.Id,
            HanaServerId = site.HanaServerId,
            HanaServer = site.HanaServer,
            Schema = site.Schema,
            TSC = site.TSC,
            Land = site.Land,
            SourceSystem = site.SourceSystem,
            UsernameOverride = site.UsernameOverride,
            PasswordOverride = site.PasswordOverride,
            LocalExportFolderOverride = site.LocalExportFolderOverride,
            ManualImportFilePath = site.ManualImportFilePath,
            ManualImportLastUploadedAtUtc = site.ManualImportLastUploadedAtUtc,
            SapServiceUrl = sapServiceUrl,
            SapEntitySet = site.SapEntitySet,
            SapEntitySetsCache = site.SapEntitySetsCache,
            SapEntitySetsRefreshedAtUtc = site.SapEntitySetsRefreshedAtUtc,
            IsActive = site.IsActive
        };
    }
}
