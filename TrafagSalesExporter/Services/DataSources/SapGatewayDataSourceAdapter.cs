using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.DataSources;

public sealed class SapGatewayDataSourceAdapter : IDataSourceAdapter
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ISapCompositionService _sapCompositionService;
    private readonly IAppEventLogService _appEventLogService;

    public SapGatewayDataSourceAdapter(
        IDbContextFactory<AppDbContext> dbFactory,
        ISapCompositionService sapCompositionService,
        IAppEventLogService appEventLogService)
    {
        _dbFactory = dbFactory;
        _sapCompositionService = sapCompositionService;
        _appEventLogService = appEventLogService;
    }

    public string ConnectionKind => SourceSystemConnectionKinds.SapGateway;

    public async Task<DataSourceFetchResult> FetchAsync(DataSourceFetchContext context)
    {
        var site = context.Site;
        var sourceDefinition = context.SourceDefinition;

        var credentials = DataSourceCredentials.Resolve(site, sourceDefinition);
        var sapServiceUrl = DataSourceCredentials.ResolveSapServiceUrl(site, sourceDefinition);
        if (string.IsNullOrWhiteSpace(sapServiceUrl))
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine SAP Service URL.");

        using var db = await _dbFactory.CreateDbContextAsync();
        var sapSources = await db.SapSourceDefinitions.Where(s => s.SiteId == site.Id).ToListAsync();
        var sapJoins = await db.SapJoinDefinitions.Where(j => j.SiteId == site.Id).ToListAsync();
        var sapMappings = await db.SapFieldMappings.Where(m => m.SiteId == site.Id).ToListAsync();

        if (sapSources.Count == 0)
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine SAP-Quellen konfiguriert.");
        if (sapMappings.Count == 0)
            throw new InvalidOperationException($"Standort '{site.Land}' hat keine SAP-Feldmappings.");

        context.UpdateStatus?.Invoke("SAP Quellen laden...");
        await _appEventLogService.WriteAsync("Export", "SAP Quellen laden",
            siteId: site.Id, land: site.Land,
            details: $"Sources={sapSources.Count} | Mappings={sapMappings.Count}");

        var effectiveSite = CloneSiteWithSapServiceUrl(site, sapServiceUrl);
        var records = await _sapCompositionService.BuildSalesRecordsAsync(
            effectiveSite, sapSources, sapJoins, sapMappings,
            credentials.Username, credentials.Password);

        return new DataSourceFetchResult { Records = records };
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
