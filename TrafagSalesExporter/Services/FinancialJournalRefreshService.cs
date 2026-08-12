using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services.DataSources;

namespace TrafagSalesExporter.Services;

public interface IFinancialJournalRefreshService
{
    Task<List<FinancialJournalSiteStatus>> GetSiteStatusAsync();
    Task<FinancialJournalRefreshResult> RefreshSiteAsync(int siteId, CancellationToken cancellationToken = default);
}

public sealed class FinancialJournalSiteStatus
{
    public int SiteId { get; init; }
    public string Land { get; init; } = string.Empty;
    public string Tsc { get; init; } = string.Empty;
    public string Schema { get; init; } = string.Empty;
    public string SourceSystem { get; init; } = string.Empty;
    public int RowCount { get; init; }
    public DateTime? LastLoadedAtUtc { get; init; }
    public DateTime? MinPostingDate { get; init; }
    public DateTime? MaxPostingDate { get; init; }
}

public sealed class FinancialJournalRefreshResult
{
    public int SiteId { get; init; }
    public string Tsc { get; init; } = string.Empty;
    public string Land { get; init; } = string.Empty;
    public int DeletedRows { get; init; }
    public int InsertedRows { get; init; }
    public string DateFilter { get; init; } = string.Empty;
}

/// <summary>
/// Laedt Hauptbuch-Journalzeilen der B1-Gesellschaften in die separate Tabelle
/// FinancialJournalEntries - bewusst getrennt von CentralSalesRecords und ohne
/// Eintraege in ExportLogs (die speisen den Daten-Heartbeat der Sales-Strecke).
/// Ein Lauf ersetzt den Bestand der Gesellschaft komplett (Full Load mit DateFilter).
/// </summary>
public class FinancialJournalRefreshService : IFinancialJournalRefreshService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IFinancialJournalReader _journalReader;
    private readonly ISapGatewayFinancialJournalReader _sapJournalReader;
    private readonly IAppEventLogService _appEventLogService;

    public FinancialJournalRefreshService(
        IDbContextFactory<AppDbContext> dbFactory,
        IFinancialJournalReader journalReader,
        ISapGatewayFinancialJournalReader sapJournalReader,
        IAppEventLogService appEventLogService)
    {
        _dbFactory = dbFactory;
        _journalReader = journalReader;
        _sapJournalReader = sapJournalReader;
        _appEventLogService = appEventLogService;
    }

    public async Task<List<FinancialJournalSiteStatus>> GetSiteStatusAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var sourceSystems = await db.SourceSystemDefinitions.AsNoTracking().ToListAsync();
        var sites = await db.Sites.AsNoTracking().OrderBy(s => s.Land).ToListAsync();
        var journalStats = await db.FinancialJournalEntries
            .AsNoTracking()
            .GroupBy(e => e.Tsc)
            .Select(g => new
            {
                Tsc = g.Key,
                RowCount = g.Count(),
                LastLoadedAtUtc = g.Max(e => e.StoredAtUtc),
                MinPostingDate = g.Min(e => e.PostingDate),
                MaxPostingDate = g.Max(e => e.PostingDate)
            })
            .ToListAsync();

        return sites
            .Where(site => IsJournalSite(site, sourceSystems))
            .Select(site =>
            {
                var stats = journalStats.FirstOrDefault(s => string.Equals(s.Tsc, site.TSC, StringComparison.OrdinalIgnoreCase));
                return new FinancialJournalSiteStatus
                {
                    SiteId = site.Id,
                    Land = site.Land,
                    Tsc = site.TSC,
                    Schema = site.Schema,
                    SourceSystem = site.SourceSystem,
                    RowCount = stats?.RowCount ?? 0,
                    LastLoadedAtUtc = stats?.LastLoadedAtUtc,
                    MinPostingDate = stats?.MinPostingDate,
                    MaxPostingDate = stats?.MaxPostingDate
                };
            })
            .ToList();
    }

    public async Task<FinancialJournalRefreshResult> RefreshSiteAsync(int siteId, CancellationToken cancellationToken = default)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken)
            ?? throw new InvalidOperationException($"Standort mit Id {siteId} wurde nicht gefunden.");
        var sourceSystems = await db.SourceSystemDefinitions.AsNoTracking().ToListAsync(cancellationToken);
        if (!IsJournalSite(site, sourceSystems))
            throw new InvalidOperationException($"Standort '{site.Land}' ({site.TSC}) ist keine Journalquelle (weder B1/HANA noch SAP-Gateway).");

        var sourceDefinition = sourceSystems.First(s =>
            string.Equals(s.Code, site.SourceSystem, StringComparison.OrdinalIgnoreCase));
        var settings = await db.ExportSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new ExportSettings();

        await _appEventLogService.WriteAsync("Journal", "Journal-Import gestartet",
            siteId: site.Id, land: site.Land,
            details: $"TSC={site.TSC} | Anschluss={sourceDefinition.ConnectionKind} | dateFilter={settings.DateFilter}");

        List<FinancialJournalEntry> entries;
        try
        {
            entries = await ReadJournalEntriesAsync(db, site, sourceDefinition, settings, cancellationToken);
        }
        catch (Exception ex)
        {
            await _appEventLogService.WriteAsync("Journal", "Journal-Import fehlgeschlagen", "Error",
                siteId: site.Id, land: site.Land, details: ex.ToString());
            throw;
        }

        // Guardrail analog Sales-Import: eine leere Journalantwort ueberschreibt
        // keinen vorhandenen Bestand (Verbindungs-/Berechtigungsfehler enden ohnehin als Exception).
        var existingCount = await db.FinancialJournalEntries.CountAsync(e => e.Tsc == site.TSC, cancellationToken);
        if (entries.Count == 0 && existingCount > 0)
        {
            await _appEventLogService.WriteAsync("Journal", "Journal-Import abgebrochen: Quelle lieferte 0 Zeilen", "Warning",
                siteId: site.Id, land: site.Land,
                details: $"Bestand ({existingCount} Zeilen) bleibt unveraendert.");
            return new FinancialJournalRefreshResult
            {
                SiteId = site.Id,
                Tsc = site.TSC,
                Land = site.Land,
                DeletedRows = 0,
                InsertedRows = 0,
                DateFilter = settings.DateFilter
            };
        }

        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var deleted = await db.FinancialJournalEntries
            .Where(e => e.Tsc == site.TSC)
            .ExecuteDeleteAsync(cancellationToken);
        db.FinancialJournalEntries.AddRange(entries);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _appEventLogService.WriteAsync("Journal", "Journal-Import abgeschlossen",
            siteId: site.Id, land: site.Land,
            details: $"TSC={site.TSC} | Geloescht={deleted} | Neu={entries.Count}");

        return new FinancialJournalRefreshResult
        {
            SiteId = site.Id,
            Tsc = site.TSC,
            Land = site.Land,
            DeletedRows = deleted,
            InsertedRows = entries.Count,
            DateFilter = settings.DateFilter
        };
    }

    /// <summary>
    /// Journalquelle = aktiver Standort mit einer Buchhaltungs-Datenbankquelle:
    ///
    /// 1. HANA-Anschluss mit eigenem Schema — die SAP-B1-Gesellschaften FR/IT/US und
    ///    Indien (`TRAFAG_LIVE`; fachlich ebenfalls B1, in der Konfiguration aber
    ///    historisch unter dem irrefuehrenden Code `SAGE`). Bewusst NICHT ueber den
    ///    Quellsystem-Code eingegrenzt; ob `OJDT`/`JDT1` existieren, prueft der Reader.
    /// 2. SAP-Gateway-Anschluss mit aufloesbarer Service-URL — ZSCHWEIZ (CH/AT).
    ///    Das Hauptbuch kommt dort aus BKPF/BSEG ueber das EntitySet `FinanzJournalSet`;
    ///    solange das EntitySet auf SAP-Seite fehlt, meldet der Reader das klar.
    ///
    /// Aussen vor bleiben die Manual-Excel-Laender DE/UK/ES (keine Buchhaltungsquelle).
    /// </summary>
    public static bool IsJournalSite(Site site, IReadOnlyCollection<SourceSystemDefinition> sourceSystems)
    {
        if (site is null || !site.IsActive)
            return false;

        var definition = sourceSystems.FirstOrDefault(s =>
            string.Equals(s.Code, site.SourceSystem, StringComparison.OrdinalIgnoreCase));
        if (definition is null || !definition.IsActive)
            return false;

        if (string.Equals(definition.ConnectionKind, SourceSystemConnectionKinds.Hana, StringComparison.OrdinalIgnoreCase))
            return !string.IsNullOrWhiteSpace(site.Schema);

        if (string.Equals(definition.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase))
            return !string.IsNullOrWhiteSpace(DataSourceCredentials.ResolveSapServiceUrl(site, definition));

        return false;
    }

    private async Task<List<FinancialJournalEntry>> ReadJournalEntriesAsync(
        AppDbContext db,
        Site site,
        SourceSystemDefinition sourceDefinition,
        ExportSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.Equals(sourceDefinition.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase))
        {
            var serviceUrl = DataSourceCredentials.ResolveSapServiceUrl(site, sourceDefinition);
            var credentials = DataSourceCredentials.Resolve(site, sourceDefinition);
            if (string.IsNullOrWhiteSpace(serviceUrl) ||
                string.IsNullOrWhiteSpace(credentials.Username) ||
                string.IsNullOrWhiteSpace(credentials.Password))
            {
                throw new InvalidOperationException(
                    $"Fuer Standort '{site.Land}' ({site.TSC}) fehlen SAP-Service-URL oder Zugangsdaten.");
            }

            return await _sapJournalReader.GetJournalEntriesAsync(
                serviceUrl, credentials.Username, credentials.Password,
                site.TSC, site.Land, sourceDefinition.Code, settings.DateFilter, cancellationToken);
        }

        var server = await BuildEffectiveServerAsync(db, site, sourceDefinition, cancellationToken);
        return await _journalReader.GetJournalEntriesAsync(
            server, site.Schema, site.TSC, site.Land, sourceDefinition.Code, settings.DateFilter, cancellationToken);
    }

    private static async Task<HanaServer> BuildEffectiveServerAsync(
        AppDbContext db, Site site, SourceSystemDefinition sourceDefinition, CancellationToken cancellationToken)
    {
        var centralServer = await db.HanaServers
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.SourceSystem == sourceDefinition.Code, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Fuer Quellsystem '{sourceDefinition.Code}' ist keine zentrale HANA-Konfiguration vorhanden.");

        var credentials = DataSourceCredentials.Resolve(site, sourceDefinition);

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
}
