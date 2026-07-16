using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services.DataSources;

public sealed class SapGatewayDataSourceAdapter : IDataSourceAdapter
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ISapCompositionService _sapCompositionService;
    private readonly ISapGatewayStandardCostReader _standardCostReader;
    private readonly IAppEventLogService _appEventLogService;

    public SapGatewayDataSourceAdapter(
        IDbContextFactory<AppDbContext> dbFactory,
        ISapCompositionService sapCompositionService,
        ISapGatewayStandardCostReader standardCostReader,
        IAppEventLogService appEventLogService)
    {
        _dbFactory = dbFactory;
        _sapCompositionService = sapCompositionService;
        _standardCostReader = standardCostReader;
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
            credentials.Username, credentials.Password, context.PreferredImportYear);
        if (records.Count == 0)
        {
            var yearText = context.PreferredImportYear?.ToString() ?? "ohne Jahresfilter";
            throw new InvalidOperationException(
                $"SAP-OData fuer Standort '{site.TSC}' lieferte 0 Umsatzzeilen ({yearText}). " +
                "Import abgebrochen, damit bestehende Dashboard-Daten nicht leer ueberschrieben werden.");
        }

        await EnrichStandardCostsAsync(
            records, sapServiceUrl, credentials.Username, credentials.Password, site, context);

        return new DataSourceFetchResult { Records = records };
    }

    /// <summary>
    /// Holt die Standardpreise (MBEW) fuer die CH/AT-Gesellschaften nach und setzt die
    /// Kostenbasis je Umsatzzeile. Scheitert das, bleibt der Umsatzimport gueltig — die
    /// Kosten sind eine Anreicherung, kein Muss. Ein Ausfall wuerde sonst den taeglichen
    /// Umsatzexport eines ganzen Landes verhindern.
    /// </summary>
    private async Task EnrichStandardCostsAsync(
        List<SalesRecord> records,
        string sapServiceUrl,
        string username,
        string password,
        Site site,
        DataSourceFetchContext context)
    {
        var valuationAreas = records
            .Select(record => StandardCostEnricher.ResolveValuationArea(record.Land))
            .Where(area => area is not null)
            .Select(area => area!)
            .Distinct()
            .ToList();

        if (valuationAreas.Count == 0)
            return;

        try
        {
            context.UpdateStatus?.Invoke("SAP Standardpreise laden...");
            var standardCosts = await _standardCostReader.GetStandardCostsAsync(
                sapServiceUrl, username, password, valuationAreas, site.Land);

            // Fuer ZSCHWEIZ (CH/AT) liefert die Mapping-Stufe bereits eine Kostenbasis aus
            // VBRP-WAVWR/MBEW-STPRS (siehe SapCompositionService.ResolveZschweizStandardCostRows,
            // docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md). WAVWR bleibt fuehrend, weil zum
            // Verkaufszeitpunkt eingefroren; MBEW-STPRS hier ist der AKTUELLE Preis und wuerde
            // das sonst rueckwirkungsfrei ueberschreiben. Nur Zeilen ohne bereits aufgeloeste
            // Kostenbasis bekommen diesen (aelteren) Fallback.
            var recordsWithoutCostBasis = records.Where(r => r.StandardCost == 0m).ToList();
            var result = StandardCostEnricher.Apply(recordsWithoutCostBasis, standardCosts);
            await _appEventLogService.WriteAsync("Export", "Standardpreise zugeordnet",
                siteId: site.Id, land: site.Land,
                details: $"Materialpreise={standardCosts.Count} | Zeilen mit Kosten={result.Matched} " +
                         $"({result.MatchedPercent} %) | ohne Kosten={result.Missing}");

            await PersistGroupStandardCostsAsync(standardCosts);
        }
        catch (Exception ex)
        {
            await _appEventLogService.WriteAsync("Export", "Standardpreise nicht ermittelbar", "Warning",
                siteId: site.Id, land: site.Land,
                details: $"Umsatzimport laeuft ohne Kostenbasis weiter. Grund: {ex.Message}");
        }
    }

    /// <summary>
    /// Persistiert die beim CH/AT-Import ohnehin geladenen MBEW-Standardpreise fuer TR AG
    /// (Bewertungskreis 1100) in <see cref="GroupStandardCost"/>, damit andere TSC mit TR AG
    /// als internem Lieferanten die echte Konzern-Kostenbasis nutzen koennen (Mappe1.xlsx).
    /// Ersetzt den TR-AG-Bestand vollstaendig je Lauf (Full Replace, wie bei anderen
    /// Standort-Importen), damit veraltete Materialien nicht stehen bleiben.
    /// </summary>
    private async Task PersistGroupStandardCostsAsync(
        IReadOnlyDictionary<StandardCostKey, StandardCostEntry> standardCosts)
    {
        var trAgArea = GroupStandardCostAreas.ByEntity[GroupStandardCostEntities.TrAg];
        var trAgCurrency = GroupStandardCostAreas.CurrencyByEntity[GroupStandardCostEntities.TrAg];
        var trAgCosts = standardCosts
            .Where(entry => string.Equals(entry.Key.ValuationArea, trAgArea, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (trAgCosts.Count == 0)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM GroupStandardCosts WHERE ValuationArea = {trAgArea}");
        db.GroupStandardCosts.AddRange(trAgCosts.Select(entry => new GroupStandardCost
        {
            MaterialKey = entry.Key.MaterialKey,
            ValuationArea = entry.Key.ValuationArea,
            UnitCost = entry.Value.UnitCost,
            Currency = trAgCurrency,
            RefreshedAtUtc = now
        }));
        await db.SaveChangesAsync();
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
