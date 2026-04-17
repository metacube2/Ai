using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.DataSources;

public sealed class HanaDataSourceAdapter : IDataSourceAdapter
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHanaQueryService _hanaService;
    private readonly IAppEventLogService _appEventLogService;

    public HanaDataSourceAdapter(
        IDbContextFactory<AppDbContext> dbFactory,
        IHanaQueryService hanaService,
        IAppEventLogService appEventLogService)
    {
        _dbFactory = dbFactory;
        _hanaService = hanaService;
        _appEventLogService = appEventLogService;
    }

    public string ConnectionKind => SourceSystemConnectionKinds.Hana;

    public async Task<DataSourceFetchResult> FetchAsync(DataSourceFetchContext context)
    {
        var site = context.Site;
        var sourceDefinition = context.SourceDefinition;

        using var db = await _dbFactory.CreateDbContextAsync();
        var exportServer = await BuildEffectiveServerAsync(db, site, sourceDefinition);

        context.UpdateStatus?.Invoke("HANA Abfrage...");
        await _appEventLogService.WriteAsync("Export", "HANA Abfrage gestartet",
            siteId: site.Id, land: site.Land,
            details: exportServer.GetConnectionStringPreview());

        var records = await Task.Run(() => _hanaService.GetSalesRecords(
            exportServer, site.Schema, site.TSC, site.Land, context.Settings.DateFilter));

        return new DataSourceFetchResult { Records = records };
    }

    private static async Task<HanaServer> BuildEffectiveServerAsync(
        AppDbContext db, Site site, SourceSystemDefinition sourceDefinition)
    {
        var centralServer = await db.HanaServers
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.SourceSystem == sourceDefinition.Code)
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
