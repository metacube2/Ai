using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class SiteExportService : ISiteExportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHanaQueryService _hanaService;
    private readonly IExcelExportService _excelService;
    private readonly ISharePointUploadService _sharePointService;
    private readonly IRecordTransformationService _transformationService;
    private readonly ILogger<SiteExportService> _logger;

    public SiteExportService(
        IDbContextFactory<AppDbContext> dbFactory,
        IHanaQueryService hanaService,
        IExcelExportService excelService,
        ISharePointUploadService sharePointService,
        IRecordTransformationService transformationService,
        ILogger<SiteExportService> logger)
    {
        _dbFactory = dbFactory;
        _hanaService = hanaService;
        _excelService = excelService;
        _sharePointService = sharePointService;
        _transformationService = transformationService;
        _logger = logger;
    }

    public async Task<SiteExportResult> ExportAsync(Site site, Action<string>? updateStatus = null)
    {
        if (site.HanaServer is null)
            throw new InvalidOperationException($"Standort '{site.Land}' hat keinen HANA-Server.");

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
            using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.ExportSettings.FirstOrDefaultAsync() ?? new ExportSettings();
            var spConfig = await db.SharePointConfigs.FirstOrDefaultAsync();
            var exportServer = BuildEffectiveServer(site, settings);

            updateStatus?.Invoke("HANA Abfrage...");
            var records = await Task.Run(() => _hanaService.GetSalesRecords(
                exportServer, site.Schema, site.TSC, site.Land, settings.DateFilter));

            updateStatus?.Invoke("Transformationen anwenden...");
            var rules = await db.FieldTransformationRules
                .Where(r => r.IsActive && r.SourceSystem == (string.IsNullOrWhiteSpace(site.SourceSystem) ? "SAP" : site.SourceSystem))
                .OrderBy(r => r.SortOrder)
                .ToListAsync();
            _transformationService.Apply(records, rules);

            updateStatus?.Invoke("Excel erstellen...");
            var outputDir = Path.Combine(AppContext.BaseDirectory, "output");
            var filePath = _excelService.CreateExcelFile(outputDir, site.TSC, DateTime.UtcNow.Date, records);
            var fileName = Path.GetFileName(filePath);

            if (spConfig is not null &&
                !string.IsNullOrWhiteSpace(spConfig.TenantId) &&
                !string.IsNullOrWhiteSpace(spConfig.ClientId) &&
                !string.IsNullOrWhiteSpace(spConfig.ClientSecret))
            {
                updateStatus?.Invoke("SharePoint Upload...");
                await _sharePointService.UploadAsync(
                    spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret,
                    spConfig.SiteUrl, spConfig.ExportFolder, site.Land, filePath);
            }

            sw.Stop();
            log.Status = "OK";
            log.RowCount = records.Count;
            log.FileName = fileName;
            log.DurationSeconds = sw.Elapsed.TotalSeconds;

            _logger.LogInformation("Export OK: {Land} ({TSC}) - {Rows} Zeilen in {Duration:F1}s",
                site.Land, site.TSC, records.Count, sw.Elapsed.TotalSeconds);

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
            log.DurationSeconds = sw.Elapsed.TotalSeconds;

            _logger.LogError(ex, "Export Fehler: {Land} ({TSC})", site.Land, site.TSC);

            return new SiteExportResult
            {
                Records = [],
                Log = log,
                FilePath = null
            };
        }
    }

    private static HanaServer BuildEffectiveServer(Site site, ExportSettings settings)
    {
        if (site.HanaServer is null)
            throw new InvalidOperationException($"Standort '{site.Land}' hat keinen HANA-Server.");

        var sourceSystem = string.IsNullOrWhiteSpace(site.SourceSystem) ? "SAP" : site.SourceSystem.Trim().ToUpperInvariant();
        var inheritedUsername = GetCentralUsername(sourceSystem, settings);
        var inheritedPassword = GetCentralPassword(sourceSystem, settings);

        return new HanaServer
        {
            Id = site.HanaServer.Id,
            Name = site.HanaServer.Name,
            Host = site.HanaServer.Host,
            Port = site.HanaServer.Port,
            Username = FirstNonEmpty(site.UsernameOverride, inheritedUsername, site.HanaServer.Username),
            Password = FirstNonEmpty(site.PasswordOverride, inheritedPassword, site.HanaServer.Password),
            DatabaseName = site.HanaServer.DatabaseName,
            UseSsl = site.HanaServer.UseSsl,
            ValidateCertificate = site.HanaServer.ValidateCertificate,
            AdditionalParams = site.HanaServer.AdditionalParams
        };
    }

    private static string GetCentralUsername(string sourceSystem, ExportSettings settings) => sourceSystem switch
    {
        "BI1" => settings.Bi1Username,
        "SAGE" => settings.SageUsername,
        _ => settings.SapUsername
    };

    private static string GetCentralPassword(string sourceSystem, ExportSettings settings) => sourceSystem switch
    {
        "BI1" => settings.Bi1Password,
        "SAGE" => settings.SagePassword,
        _ => settings.SapPassword
    };

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }
}
