using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IFinanceAllExportService
{
    /// <summary>
    /// Erzeugt <c>Finance_All_&lt;heute&gt;.xlsx</c> und laedt sie in denselben zentralen
    /// SharePoint-Ordner wie <c>Sales_All</c>. Gibt den lokalen Pfad zurueck oder <c>null</c>,
    /// wenn nichts erzeugt wurde.
    /// </summary>
    Task<string?> ExportAsync(Action<string>? updateStatus = null, CancellationToken abbruch = default);
}

/// <summary>
/// Taegliche Erzeugung von <c>Finance_All</c> im Nachtlauf.
///
/// Hintergrund: Bis zum 2026-09-11 entstand diese Datei ausschliesslich von Hand ueber
/// <c>Tools/FinanceAll/finance_all_xlsx.py</c> und <c>Tools/FinanceAllUpload</c>. Kein
/// Anwendungscode erzeugte sie, weshalb ein Klick auf den Journalimport sie nie aktualisieren
/// konnte und im Serverordner <c>output</c> keine einzige lag. Ingo hat am 2026-09-11
/// entschieden, das in die Anwendung zu holen statt in eine geplante Aufgabe auf seinem
/// Arbeitsplatz, damit der Lauf unabhaengig von seinem Notebook ist und Fehler im Admin-Log
/// sichtbar werden.
///
/// Die beiden Werkzeuge bleiben bestehen: fuer gekuerzte Ad-hoc-Auszuege und als Referenz zum
/// Gegenrechnen.
/// </summary>
public class FinanceAllExportService : IFinanceAllExportService
{
    private const string FinanceImportRootFolder = "/Import/Finance";
    private static readonly TimeSpan SharePointUploadTimeout = TimeSpan.FromMinutes(10);

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ISharePointUploadService _sharePointService;
    private readonly IAppEventLogService _appEventLogService;
    private readonly ILogger<FinanceAllExportService> _logger;

    public FinanceAllExportService(
        IDbContextFactory<AppDbContext> dbFactory,
        ISharePointUploadService sharePointService,
        IAppEventLogService appEventLogService,
        ILogger<FinanceAllExportService> logger)
    {
        _dbFactory = dbFactory;
        _sharePointService = sharePointService;
        _appEventLogService = appEventLogService;
        _logger = logger;
    }

    public async Task<string?> ExportAsync(Action<string>? updateStatus = null, CancellationToken abbruch = default)
    {
        using var db = await _dbFactory.CreateDbContextAsync(abbruch);
        var spConfig = await db.SharePointConfigs.FirstOrDefaultAsync(abbruch);
        var settings = await db.ExportSettings.FirstOrDefaultAsync(abbruch) ?? new ExportSettings();

        // Die Anwendung oeffnet die Datenbank ueber einen relativen Pfad im eigenen Ordner.
        // Auf dem Server ist das eine lokale Datei, deshalb entfaellt die lokale Kopie, die das
        // Python-Werkzeug mit --lokal anlegt: die war nur noetig, weil ein Arbeitsplatz ueber
        // SMB liest und das der gemessene Engpass war.
        var datenbankPfad = db.Database.GetDbConnection().DataSource;
        if (string.IsNullOrWhiteSpace(datenbankPfad) || !File.Exists(datenbankPfad))
        {
            await _appEventLogService.WriteAsync(
                "Finance_All",
                "Uebersprungen: Datenbankdatei nicht gefunden",
                "Warning",
                details: $"Pfad={datenbankPfad}");
            _logger.LogWarning("Finance_All uebersprungen, Datenbankdatei nicht gefunden: {Pfad}", datenbankPfad);
            return null;
        }

        var zielOrdner = ResolveOutputDirectory(settings);
        var dateiname = $"Finance_All_{DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.xlsx";
        var zielPfad = Path.Combine(zielOrdner, dateiname);

        updateStatus?.Invoke("Finance_All Excel erzeugen...");
        await _appEventLogService.WriteAsync(
            "Finance_All",
            "Erzeugung gestartet",
            details: $"Datei={dateiname}; Ordner={zielOrdner}");

        var uhr = Stopwatch.StartNew();
        FinanceAllWorkbookWriter.Ergebnis ergebnis;
        try
        {
            ergebnis = await Task.Run(
                () => FinanceAllWorkbookWriter.Schreibe(datenbankPfad, zielPfad), abbruch);
        }
        catch (Exception ex)
        {
            await _appEventLogService.WriteAsync(
                "Finance_All",
                "Erzeugung fehlgeschlagen",
                "Error",
                details: $"Datei={dateiname}; Fehler={ex.Message}");
            throw;
        }

        uhr.Stop();
        await _appEventLogService.WriteAsync(
            "Finance_All",
            "Erzeugung abgeschlossen",
            details: $"Datei={dateiname}; Buchungszeilen={ergebnis.Buchungszeilen:N0}; " +
                     $"Groesse={ergebnis.Bytes / 1024d / 1024d:N1} MB; Dauer={uhr.Elapsed:hh\\:mm\\:ss}");
        _logger.LogInformation(
            "Finance_All erzeugt: {Datei}, {Zeilen} Buchungszeilen, {Dauer}",
            dateiname, ergebnis.Buchungszeilen, uhr.Elapsed);

        if (!HasCompleteSharePointConfig(spConfig))
        {
            // Kein stiller Aussteiger: Ohne diesen Eintrag bliebe unsichtbar, dass die Datei
            // zwar erzeugt, aber nie veroeffentlicht wurde.
            await _appEventLogService.WriteAsync(
                "Finance_All",
                "SharePoint-Upload uebersprungen: Konfiguration unvollstaendig",
                "Warning",
                details: $"Datei={dateiname}");
            return zielPfad;
        }

        var (ordner, unterordner) = ResolveCentralSharePointUploadTarget(spConfig!);
        updateStatus?.Invoke("Finance_All nach SharePoint laden...");
        await _appEventLogService.WriteAsync(
            "Finance_All",
            "SharePoint Upload gestartet",
            details: $"Datei={dateiname}; Ziel={$"{ordner}/{unterordner}".TrimEnd('/')}");

        await _sharePointService.UploadAsync(
            spConfig!.TenantId,
            spConfig.ClientId,
            spConfig.ClientSecret,
            spConfig.SiteUrl,
            ordner,
            unterordner,
            zielPfad,
            uploadTimestampedCopyIfLocked: true).WaitAsync(SharePointUploadTimeout, abbruch);

        await _appEventLogService.WriteAsync(
            "Finance_All",
            "SharePoint Upload abgeschlossen",
            details: $"Datei={dateiname}; Ziel={$"{ordner}/{unterordner}".TrimEnd('/')}");

        return zielPfad;
    }

    private static string ResolveOutputDirectory(ExportSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.LocalConsolidatedExportFolder))
            return settings.LocalConsolidatedExportFolder.Trim();

        if (!string.IsNullOrWhiteSpace(settings.LocalSiteExportFolder))
            return settings.LocalSiteExportFolder.Trim();

        return Path.Combine(AppContext.BaseDirectory, "output");
    }

    // Gleiche Zielaufloesung wie ConsolidatedExportService fuer Sales_All und wie
    // Tools/FinanceAllUpload. Finance_All landet damit im selben zentralen Ordner.
    private static (string Folder, string LandSubfolder) ResolveCentralSharePointUploadTarget(SharePointConfig config)
    {
        var configuredFolder = !string.IsNullOrWhiteSpace(config.CentralExportFolder)
            ? config.CentralExportFolder
            : IsLegacyExportFolder(config.ExportFolder)
                ? FinanceImportRootFolder
                : config.ExportFolder;

        var normalizedFolder = configuredFolder.Trim().TrimEnd('/', '\\');
        return normalizedFolder.EndsWith("/Alle", StringComparison.OrdinalIgnoreCase) ||
               normalizedFolder.EndsWith("\\Alle", StringComparison.OrdinalIgnoreCase)
            ? (normalizedFolder, string.Empty)
            : (configuredFolder, "Alle");
    }

    private static bool IsLegacyExportFolder(string folder)
    {
        var normalized = folder.Trim().TrimEnd('/', '\\');
        return normalized.Equals("/Shared Documents/Exports", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasCompleteSharePointConfig(SharePointConfig? config)
        => config is not null &&
           !string.IsNullOrWhiteSpace(config.TenantId) &&
           !string.IsNullOrWhiteSpace(config.ClientId) &&
           !string.IsNullOrWhiteSpace(config.ClientSecret) &&
           !string.IsNullOrWhiteSpace(config.SiteUrl);
}
