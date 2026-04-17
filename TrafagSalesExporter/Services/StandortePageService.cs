using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IStandortePageService
{
    Task<StandortePageState> LoadAsync();
    Task SaveServerAsync(HanaServer server, IEnumerable<string> hanaSourceSystemCodes);
    Task DeleteServerAsync(HanaServer server);
    Task<ConnectionTestResult> TestServerConnectionAsync(HanaServer server);
    Task<StandortEditorState> LoadSiteEditorAsync(Site site, IEnumerable<SourceSystemDefinition> sourceSystems);
    Task SaveSiteAsync(Site site, bool usesHanaConnection, bool isSapSite, List<SapSourceDefinition> sapSources, List<SapJoinDefinition> sapJoins, List<SapFieldMapping> sapMappings, List<string> sapEntitySetsCache);
    Task DeleteSiteAsync(Site site);
    Task<List<string>> LoadAvailableSchemasAsync(Site site);
    Task<SapEntitySetRefreshResult> RefreshSapEntitySetsAsync(Site site);
    Task<SapSourceFieldRefreshResult> RefreshSapSourceFieldsAsync(Site site, List<SapSourceDefinition> sapSources, List<SapFieldMapping> sapMappings);
    Task<DateTime> ValidateManualImportPathAsync(string manualImportFilePath);
}

public sealed class StandortePageService : IStandortePageService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHanaQueryService _hanaService;
    private readonly ISapGatewayService _sapGatewayService;
    private readonly ISharePointUploadService _sharePointService;
    private readonly IAppEventLogService _appEventLogService;

    public StandortePageService(
        IDbContextFactory<AppDbContext> dbFactory,
        IHanaQueryService hanaService,
        ISapGatewayService sapGatewayService,
        ISharePointUploadService sharePointService,
        IAppEventLogService appEventLogService)
    {
        _dbFactory = dbFactory;
        _hanaService = hanaService;
        _sapGatewayService = sapGatewayService;
        _sharePointService = sharePointService;
        _appEventLogService = appEventLogService;
    }

    public async Task<StandortePageState> LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sourceSystems = await db.SourceSystemDefinitions.OrderBy(x => x.Code).ToListAsync();
        var hanaSourceSystemCodes = sourceSystems
            .Where(x => string.Equals(x.ConnectionKind, SourceSystemConnectionKinds.Hana, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Code)
            .ToList();

        return new StandortePageState
        {
            SourceSystems = sourceSystems,
            Servers = await db.HanaServers
                .Where(s => hanaSourceSystemCodes.Contains(s.SourceSystem))
                .OrderBy(s => s.SourceSystem)
                .ThenBy(s => s.Name)
                .ToListAsync(),
            Sites = await db.Sites.Include(s => s.HanaServer).OrderBy(s => s.Land).ToListAsync()
        };
    }

    public async Task SaveServerAsync(HanaServer server, IEnumerable<string> hanaSourceSystemCodes)
    {
        server.SourceSystem = string.IsNullOrWhiteSpace(server.SourceSystem)
            ? hanaSourceSystemCodes.FirstOrDefault() ?? string.Empty
            : server.SourceSystem.Trim().ToUpperInvariant();
        server.Name = string.IsNullOrWhiteSpace(server.Name) ? server.SourceSystem : server.Name.Trim();
        server.Host = server.Host.Trim();
        server.DatabaseName = server.DatabaseName.Trim();
        server.AdditionalParams = server.AdditionalParams.Trim();
        server.Username = string.Empty;
        server.Password = string.Empty;

        await using var db = await _dbFactory.CreateDbContextAsync();
        if (server.Id == 0)
        {
            var existingForSourceSystem = await db.HanaServers
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(x => x.SourceSystem == server.SourceSystem);

            if (existingForSourceSystem is null)
            {
                db.HanaServers.Add(server);
            }
            else
            {
                ApplyServer(existingForSourceSystem, server);
            }
        }
        else
        {
            var existing = await db.HanaServers.FindAsync(server.Id);
            if (existing is not null)
                ApplyServer(existing, server);
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteServerAsync(HanaServer server)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var linkedSites = await db.Sites
            .Where(s => s.HanaServerId == server.Id)
            .OrderBy(s => s.Land)
            .Select(s => $"{s.Land} ({s.TSC})")
            .ToListAsync();

        if (linkedSites.Count > 0)
            throw new InvalidOperationException($"Server kann nicht geloescht werden. Noch verknuepfte Standorte: {string.Join(", ", linkedSites)}");

        var entity = await db.HanaServers.FindAsync(server.Id);
        if (entity is not null)
        {
            db.HanaServers.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task<ConnectionTestResult> TestServerConnectionAsync(HanaServer server)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sourceDefinition = await db.SourceSystemDefinitions
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.Code == server.SourceSystem);

        if (sourceDefinition is null)
            throw new InvalidOperationException($"Quellsystem '{server.SourceSystem}' nicht gefunden.");

        if (string.IsNullOrWhiteSpace(sourceDefinition.CentralUsername) || string.IsNullOrWhiteSpace(sourceDefinition.CentralPassword))
            throw new InvalidOperationException($"Fuer {server.SourceSystem} sind keine zentralen Zugangsdaten im Quellsystem gepflegt.");

        var testServer = new HanaServer
        {
            Id = server.Id,
            SourceSystem = server.SourceSystem,
            Name = server.Name,
            Host = server.Host,
            Port = server.Port,
            Username = sourceDefinition.CentralUsername.Trim(),
            Password = sourceDefinition.CentralPassword,
            DatabaseName = server.DatabaseName,
            UseSsl = server.UseSsl,
            ValidateCertificate = server.ValidateCertificate,
            AdditionalParams = server.AdditionalParams
        };

        await _appEventLogService.WriteAsync("HANA", "Server-Test aus UI gestartet", details: testServer.GetConnectionStringPreview());
        return await _hanaService.TestConnectionDetailedAsync(testServer);
    }

    public async Task<StandortEditorState> LoadSiteEditorAsync(Site site, IEnumerable<SourceSystemDefinition> sourceSystems)
    {
        var effectiveSourceSystem = string.IsNullOrWhiteSpace(site.SourceSystem)
            ? sourceSystems.FirstOrDefault()?.Code ?? "SAP"
            : site.SourceSystem;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var sapSources = await db.SapSourceDefinitions.Where(s => s.SiteId == site.Id).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();
        var sapJoins = await db.SapJoinDefinitions.Where(j => j.SiteId == site.Id).OrderBy(j => j.SortOrder).ThenBy(j => j.Id).ToListAsync();
        var sapMappings = await db.SapFieldMappings.Where(m => m.SiteId == site.Id).OrderBy(m => m.SortOrder).ThenBy(m => m.Id).ToListAsync();

        return new StandortEditorState
        {
            Site = new Site
            {
                Id = site.Id,
                HanaServerId = site.HanaServerId,
                Schema = site.Schema,
                TSC = site.TSC,
                Land = site.Land,
                SourceSystem = effectiveSourceSystem,
                UsernameOverride = site.UsernameOverride,
                PasswordOverride = site.PasswordOverride,
                LocalExportFolderOverride = site.LocalExportFolderOverride,
                ManualImportFilePath = site.ManualImportFilePath,
                ManualImportLastUploadedAtUtc = site.ManualImportLastUploadedAtUtc,
                SapServiceUrl = site.SapServiceUrl,
                SapEntitySet = site.SapEntitySet,
                SapEntitySetsCache = site.SapEntitySetsCache,
                SapEntitySetsRefreshedAtUtc = site.SapEntitySetsRefreshedAtUtc,
                IsActive = site.IsActive
            },
            SapEntitySets = ParseSapEntitySets(site.SapEntitySetsCache),
            SapSources = sapSources,
            SapJoins = sapJoins,
            SapMappings = sapMappings
        };
    }

    public async Task SaveSiteAsync(Site site, bool usesHanaConnection, bool isSapSite, List<SapSourceDefinition> sapSources, List<SapJoinDefinition> sapJoins, List<SapFieldMapping> sapMappings, List<string> sapEntitySetsCache)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var serverId = usesHanaConnection ? await ResolveCentralHanaServerIdAsync(db, site) : (int?)null;
        site.HanaServerId = serverId;
        site.SapEntitySetsCache = JsonSerializer.Serialize(sapEntitySetsCache);

        if (site.Id == 0)
        {
            db.Sites.Add(site);
        }
        else
        {
            var existing = await db.Sites.FindAsync(site.Id);
            if (existing is not null)
                ApplySite(existing, site);
        }

        await db.SaveChangesAsync();
        await SaveSapConfigurationAsync(db, site.Id, isSapSite, sapSources, sapJoins, sapMappings);
    }

    public async Task DeleteSiteAsync(Site site)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Sites.FindAsync(site.Id);
        if (entity is null)
            return;

        var sources = await db.SapSourceDefinitions.Where(s => s.SiteId == site.Id).ToListAsync();
        var joins = await db.SapJoinDefinitions.Where(j => j.SiteId == site.Id).ToListAsync();
        var mappings = await db.SapFieldMappings.Where(m => m.SiteId == site.Id).ToListAsync();
        var centralRows = await db.CentralSalesRecords.Where(r => r.SiteId == site.Id).ToListAsync();
        if (sources.Count > 0) db.SapSourceDefinitions.RemoveRange(sources);
        if (joins.Count > 0) db.SapJoinDefinitions.RemoveRange(joins);
        if (mappings.Count > 0) db.SapFieldMappings.RemoveRange(mappings);
        if (centralRows.Count > 0) db.CentralSalesRecords.RemoveRange(centralRows);
        db.Sites.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<string>> LoadAvailableSchemasAsync(Site site)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sourceDefinition = await db.SourceSystemDefinitions.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Code == site.SourceSystem)
            ?? throw new InvalidOperationException($"Quellsystem '{site.SourceSystem}' nicht gefunden.");

        var centralServer = await db.HanaServers.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.SourceSystem == site.SourceSystem);
        if (centralServer is null || string.IsNullOrWhiteSpace(centralServer.Host))
            throw new InvalidOperationException($"Fuer {site.SourceSystem} ist keine gueltige zentrale HANA-Konfiguration vorhanden.");

        var username = string.IsNullOrWhiteSpace(site.UsernameOverride) ? sourceDefinition.CentralUsername ?? string.Empty : site.UsernameOverride;
        var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? sourceDefinition.CentralPassword ?? string.Empty : site.PasswordOverride;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException($"Fuer {site.SourceSystem} sind weder zentrale Zugangsdaten noch Standort-Overrides gesetzt.");

        var lookupServer = new HanaServer
        {
            Id = centralServer.Id,
            SourceSystem = centralServer.SourceSystem,
            Name = centralServer.Name,
            Host = centralServer.Host,
            Port = centralServer.Port,
            Username = username.Trim(),
            Password = password,
            DatabaseName = centralServer.DatabaseName,
            UseSsl = centralServer.UseSsl,
            ValidateCertificate = centralServer.ValidateCertificate,
            AdditionalParams = centralServer.AdditionalParams
        };

        var schemas = await _hanaService.GetAvailableSchemasAsync(lookupServer);
        return schemas
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<SapEntitySetRefreshResult> RefreshSapEntitySetsAsync(Site site)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sourceDefinition = await db.SourceSystemDefinitions.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Code == site.SourceSystem);
        var serviceUrl = string.IsNullOrWhiteSpace(site.SapServiceUrl) ? sourceDefinition?.CentralServiceUrl ?? string.Empty : site.SapServiceUrl;
        if (string.IsNullOrWhiteSpace(serviceUrl))
            throw new InvalidOperationException("Es ist weder eine zentrale SAP Service URL noch ein Standort-Override gesetzt.");

        var username = string.IsNullOrWhiteSpace(site.UsernameOverride) ? sourceDefinition?.CentralUsername ?? string.Empty : site.UsernameOverride;
        var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? sourceDefinition?.CentralPassword ?? string.Empty : site.PasswordOverride;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Fuer SAP sind weder zentrale Zugangsdaten noch Standort-Overrides gesetzt.");

        await _appEventLogService.WriteAsync("SAP", "Refresh aus UI gestartet", siteId: site.Id, land: site.Land, details: serviceUrl);
        var entitySets = await _sapGatewayService.GetEntitySetsAsync(serviceUrl, username.Trim(), password.Trim());
        await _appEventLogService.WriteAsync("SAP", "Refresh aus UI erfolgreich", siteId: site.Id, land: site.Land, details: $"EntitySets={entitySets.Count}");

        return new SapEntitySetRefreshResult
        {
            EntitySets = entitySets,
            RefreshedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<SapSourceFieldRefreshResult> RefreshSapSourceFieldsAsync(Site site, List<SapSourceDefinition> sapSources, List<SapFieldMapping> sapMappings)
    {
        var activeSources = sapSources
            .Where(s => s.IsActive && !string.IsNullOrWhiteSpace(s.Alias) && !string.IsNullOrWhiteSpace(s.EntitySet))
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToList();

        if (activeSources.Count == 0)
            throw new InvalidOperationException("Es gibt keine aktiven SAP-Quellen mit Alias und Entity Set.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var sourceDefinition = await db.SourceSystemDefinitions.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Code == site.SourceSystem);
        var serviceUrl = string.IsNullOrWhiteSpace(site.SapServiceUrl) ? sourceDefinition?.CentralServiceUrl ?? string.Empty : site.SapServiceUrl;
        if (string.IsNullOrWhiteSpace(serviceUrl))
            throw new InvalidOperationException("Es ist weder eine zentrale SAP Service URL noch ein Standort-Override gesetzt.");

        var username = string.IsNullOrWhiteSpace(site.UsernameOverride) ? sourceDefinition?.CentralUsername ?? string.Empty : site.UsernameOverride;
        var password = string.IsNullOrWhiteSpace(site.PasswordOverride) ? sourceDefinition?.CentralPassword ?? string.Empty : site.PasswordOverride;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Fuer SAP sind weder zentrale Zugangsdaten noch Standort-Overrides gesetzt.");

        var expressions = new List<string> { "=SAP" };
        var sourceFieldMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in activeSources)
        {
            var fieldNames = await _sapGatewayService.GetEntityFieldNamesAsync(serviceUrl, source.EntitySet, username.Trim(), password.Trim());
            sourceFieldMap[source.Alias] = fieldNames;
            expressions.AddRange(fieldNames.Select(field => $"{source.Alias}.{field}"));
        }

        foreach (var current in sapMappings.Select(m => m.SourceExpression).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (!expressions.Contains(current, StringComparer.OrdinalIgnoreCase))
                expressions.Add(current);
        }

        return new SapSourceFieldRefreshResult
        {
            SourceFieldMap = sourceFieldMap,
            SourceExpressions = expressions
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public async Task<DateTime> ValidateManualImportPathAsync(string manualImportFilePath)
    {
        var trimmedPath = manualImportFilePath.Trim();
        if (string.IsNullOrWhiteSpace(trimmedPath))
            throw new InvalidOperationException("Bitte zuerst einen Dateipfad eintragen.");
        if (!string.Equals(Path.GetExtension(trimmedPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Bitte eine Excel-Datei mit Endung .xlsx angeben.");

        if (File.Exists(trimmedPath))
            return File.GetLastWriteTimeUtc(trimmedPath);

        if (!LooksLikeSharePointReference(trimmedPath))
            throw new InvalidOperationException($"Datei nicht gefunden oder nicht erreichbar: {trimmedPath}");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var spConfig = await db.SharePointConfigs.FirstOrDefaultAsync();
        if (spConfig is null ||
            string.IsNullOrWhiteSpace(spConfig.TenantId) ||
            string.IsNullOrWhiteSpace(spConfig.ClientId) ||
            string.IsNullOrWhiteSpace(spConfig.ClientSecret) ||
            string.IsNullOrWhiteSpace(spConfig.SiteUrl))
        {
            throw new InvalidOperationException("Fuer SharePoint-Pruefung fehlt eine vollstaendige SharePoint-Konfiguration in Settings.");
        }

        var tempPath = await _sharePointService.DownloadToTempFileAsync(
            spConfig.TenantId, spConfig.ClientId, spConfig.ClientSecret, spConfig.SiteUrl, trimmedPath);
        try
        {
            return File.GetLastWriteTimeUtc(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static void ApplyServer(HanaServer target, HanaServer source)
    {
        target.SourceSystem = source.SourceSystem;
        target.Name = source.Name;
        target.Host = source.Host;
        target.Port = source.Port;
        target.Username = string.Empty;
        target.Password = string.Empty;
        target.DatabaseName = source.DatabaseName;
        target.UseSsl = source.UseSsl;
        target.ValidateCertificate = source.ValidateCertificate;
        target.AdditionalParams = source.AdditionalParams;
    }

    private static void ApplySite(Site target, Site source)
    {
        target.HanaServerId = source.HanaServerId;
        target.Schema = source.Schema;
        target.TSC = source.TSC;
        target.Land = source.Land;
        target.SourceSystem = source.SourceSystem;
        target.UsernameOverride = source.UsernameOverride;
        target.PasswordOverride = source.PasswordOverride;
        target.LocalExportFolderOverride = source.LocalExportFolderOverride;
        target.ManualImportFilePath = source.ManualImportFilePath;
        target.ManualImportLastUploadedAtUtc = source.ManualImportLastUploadedAtUtc;
        target.SapServiceUrl = source.SapServiceUrl;
        target.SapEntitySet = source.SapEntitySet;
        target.SapEntitySetsCache = source.SapEntitySetsCache;
        target.SapEntitySetsRefreshedAtUtc = source.SapEntitySetsRefreshedAtUtc;
        target.IsActive = source.IsActive;
    }

    private static List<string> ParseSapEntitySets(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static bool LooksLikeSharePointReference(string path)
        => path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("/Shared Documents/", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("Shared Documents/", StringComparison.OrdinalIgnoreCase);

    private static void NormalizeSapConfigCollections(List<SapSourceDefinition> sapSources, List<SapJoinDefinition> sapJoins, List<SapFieldMapping> sapMappings)
    {
        for (var i = 0; i < sapSources.Count; i++)
            sapSources[i].SortOrder = i;
        for (var i = 0; i < sapJoins.Count; i++)
            sapJoins[i].SortOrder = i;
        for (var i = 0; i < sapMappings.Count; i++)
            sapMappings[i].SortOrder = i;

        var selectedPrimaryIndex = sapSources.FindIndex(s => s.IsPrimary);
        var primarySource = selectedPrimaryIndex >= 0 ? sapSources[selectedPrimaryIndex] : sapSources.FirstOrDefault();
        foreach (var source in sapSources)
            source.IsPrimary = primarySource is not null && ReferenceEquals(source, primarySource);
        if (sapSources.Count > 0 && sapSources.All(s => !s.IsPrimary))
            sapSources[0].IsPrimary = true;
    }

    private static async Task SaveSapConfigurationAsync(AppDbContext db, int siteId, bool isSapSite, List<SapSourceDefinition> sapSources, List<SapJoinDefinition> sapJoins, List<SapFieldMapping> sapMappings)
    {
        var oldSources = await db.SapSourceDefinitions.Where(s => s.SiteId == siteId).ToListAsync();
        var oldJoins = await db.SapJoinDefinitions.Where(j => j.SiteId == siteId).ToListAsync();
        var oldMappings = await db.SapFieldMappings.Where(m => m.SiteId == siteId).ToListAsync();
        if (oldSources.Count > 0) db.SapSourceDefinitions.RemoveRange(oldSources);
        if (oldJoins.Count > 0) db.SapJoinDefinitions.RemoveRange(oldJoins);
        if (oldMappings.Count > 0) db.SapFieldMappings.RemoveRange(oldMappings);

        if (isSapSite)
        {
            NormalizeSapConfigCollections(sapSources, sapJoins, sapMappings);
            foreach (var source in sapSources) source.SiteId = siteId;
            foreach (var join in sapJoins) join.SiteId = siteId;
            foreach (var mapping in sapMappings) mapping.SiteId = siteId;
            db.SapSourceDefinitions.AddRange(sapSources);
            db.SapJoinDefinitions.AddRange(sapJoins);
            db.SapFieldMappings.AddRange(sapMappings);
        }

        await db.SaveChangesAsync();
    }

    private static async Task<int> ResolveCentralHanaServerIdAsync(AppDbContext db, Site site)
    {
        site.UsernameOverride = site.UsernameOverride.Trim();
        site.PasswordOverride = site.PasswordOverride.Trim();
        site.LocalExportFolderOverride = site.LocalExportFolderOverride.Trim();
        site.ManualImportFilePath = site.ManualImportFilePath.Trim();
        site.SapServiceUrl = site.SapServiceUrl.Trim();
        site.SapEntitySet = site.SapEntitySet.Trim();

        var normalizedSourceSystem = string.IsNullOrWhiteSpace(site.SourceSystem) ? string.Empty : site.SourceSystem.Trim().ToUpperInvariant();
        var centralServer = await db.HanaServers.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.SourceSystem == normalizedSourceSystem);
        if (centralServer is null || string.IsNullOrWhiteSpace(centralServer.Host))
            throw new InvalidOperationException($"Fuer Quellsystem '{normalizedSourceSystem}' ist keine gueltige zentrale HANA-Konfiguration vorhanden.");

        return centralServer.Id;
    }
}

public sealed class StandortePageState
{
    public List<SourceSystemDefinition> SourceSystems { get; set; } = [];
    public List<HanaServer> Servers { get; set; } = [];
    public List<Site> Sites { get; set; } = [];
}

public sealed class StandortEditorState
{
    public Site Site { get; set; } = new();
    public List<string> SapEntitySets { get; set; } = [];
    public List<SapSourceDefinition> SapSources { get; set; } = [];
    public List<SapJoinDefinition> SapJoins { get; set; } = [];
    public List<SapFieldMapping> SapMappings { get; set; } = [];
}

public sealed class SapEntitySetRefreshResult
{
    public List<string> EntitySets { get; set; } = [];
    public DateTime RefreshedAtUtc { get; set; }
}

public sealed class SapSourceFieldRefreshResult
{
    public List<string> SourceExpressions { get; set; } = [];
    public Dictionary<string, List<string>> SourceFieldMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
