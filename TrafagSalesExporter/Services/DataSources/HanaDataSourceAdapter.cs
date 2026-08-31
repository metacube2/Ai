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
        var exportServer = await HanaServerResolver.BuildEffectiveServerAsync(db, site, sourceDefinition);
        var sourceMappings = await db.SapSourceDefinitions
            .Where(s => s.SiteId == site.Id)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync();
        var joins = await db.SapJoinDefinitions
            .Where(j => j.SiteId == site.Id)
            .OrderBy(j => j.SortOrder)
            .ThenBy(j => j.Id)
            .ToListAsync();
        var fieldMappings = await db.SapFieldMappings
            .Where(m => m.SiteId == site.Id)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .ToListAsync();

        context.UpdateStatus?.Invoke("HANA Abfrage...");
        await _appEventLogService.WriteAsync("Export", "HANA Abfrage gestartet",
            siteId: site.Id, land: site.Land,
            details: exportServer.GetConnectionStringPreview());

        var records = sourceMappings.Count > 0 && fieldMappings.Count > 0
            ? await _hanaService.GetMappedSalesRecordsAsync(
                exportServer, site.Schema, site, sourceMappings, joins, fieldMappings, context.Settings.DateFilter)
            : await _hanaService.GetSalesRecordsAsync(
                exportServer, site.Schema, site.TSC, site.Land, context.Settings.DateFilter);

        await ApplySupplierOverridesAsync(db, site, records);
        await PersistB1GroupStandardCostsAsync(db, site, records, context.Settings.B1GroupStandardCostMode);

        return new DataSourceFetchResult { Records = records };
    }

    /// <summary>
    /// Fuellt Lieferantenfelder, die der B1-Artikelstamm noch nicht fuehrt, aus der
    /// uebergangsweisen Zuordnung (siehe <see cref="SupplierMaterialOverrideStore"/>).
    /// Laeuft VOR der Konzernkosten-Fortschreibung, damit beide Schritte dieselbe Sicht auf die
    /// Zeilen haben. Bleibt wirkungslos, sobald der Artikelstamm gepflegt ist.
    /// </summary>
    private async Task ApplySupplierOverridesAsync(
        AppDbContext db,
        Site site,
        IReadOnlyCollection<SalesRecord> records)
    {
        var result = await SupplierMaterialOverrideStore.ApplyAsync(db, site.TSC, records);
        if (result.RowsFilled == 0)
            return;

        await _appEventLogService.WriteAsync(
            "Export",
            "Lieferant aus Uebergangsliste ergaenzt",
            siteId: site.Id,
            land: site.Land,
            details: $"{SupplierMaterialOverrideStore.Describe(result)} | Quelle={SupplierMaterialOverrideStore.SourceLabel}");
    }

    private async Task PersistB1GroupStandardCostsAsync(
        AppDbContext db,
        Site site,
        IReadOnlyCollection<SalesRecord> records,
        string? costMode)
    {
        if (!GroupStandardCostAreas.TryResolveB1Source(site.TSC, out var area, out _))
            return;

        var normalizedCostMode = B1GroupStandardCostModes.Normalize(costMode);
        var result = await B1GroupStandardCostStore.ReplaceAsync(
            db, site.TSC, records, DateTime.UtcNow, normalizedCostMode);
        if (!result.Updated)
        {
            await _appEventLogService.WriteAsync(
                "Export",
                "Konzernkosten nicht aktualisiert",
                "Warning",
                site.Id,
                site.Land,
                $"Kostenbereich={area} | keine positive B1-Belegkostenbasis; vorhandener Bestand bleibt erhalten");
            return;
        }

        await _appEventLogService.WriteAsync(
            "Export",
            "Konzernkosten aktualisiert",
            siteId: site.Id,
            land: site.Land,
            details: $"Kostenbereich={area} | Materialien={result.MaterialCount} | Quelle={B1GroupStandardCostModes.Describe(normalizedCostMode)}");
    }
}
