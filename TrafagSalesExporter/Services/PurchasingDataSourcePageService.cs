using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public sealed class PurchasingDataSourcePageService : IPurchasingDataSourcePageService
{
    public const string PurchasingTsc = "PURCHASING_SAP";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ISapGatewayService _sapGatewayService;

    public PurchasingDataSourcePageService(IDbContextFactory<AppDbContext> dbFactory, ISapGatewayService sapGatewayService)
    {
        _dbFactory = dbFactory;
        _sapGatewayService = sapGatewayService;
    }

    public async Task<PurchasingDataSourceState> LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await EnsureDefaultsAsync(db);
        return await LoadStateAsync(db);
    }

    public async Task<PurchasingDataSourceState> SaveAsync(PurchasingDataSourceState state)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var site = await GetOrCreateSiteAsync(db);

        site.SapServiceUrl = state.Site.SapServiceUrl.Trim();
        site.UsernameOverride = state.Site.UsernameOverride.Trim();
        site.PasswordOverride = state.Site.PasswordOverride;
        site.IsActive = state.Site.IsActive;

        Replace(db, db.SapSourceDefinitions.Where(x => x.SiteId == site.Id), state.Sources.Select((x, i) => new SapSourceDefinition
        {
            SiteId = site.Id,
            Alias = x.Alias.Trim(),
            EntitySet = x.EntitySet.Trim(),
            IsPrimary = x.IsPrimary,
            IsActive = x.IsActive,
            SortOrder = x.SortOrder == 0 ? i * 10 : x.SortOrder
        }));

        Replace(db, db.SapJoinDefinitions.Where(x => x.SiteId == site.Id), state.Joins.Select((x, i) => new SapJoinDefinition
        {
            SiteId = site.Id,
            LeftAlias = x.LeftAlias.Trim(),
            RightAlias = x.RightAlias.Trim(),
            LeftKeys = x.LeftKeys.Trim(),
            RightKeys = x.RightKeys.Trim(),
            JoinType = string.IsNullOrWhiteSpace(x.JoinType) ? "Left" : x.JoinType.Trim(),
            IsActive = x.IsActive,
            SortOrder = x.SortOrder == 0 ? i * 10 : x.SortOrder
        }));

        Replace(db, db.SapFieldMappings.Where(x => x.SiteId == site.Id), state.Mappings.Select((x, i) => new SapFieldMapping
        {
            SiteId = site.Id,
            TargetField = x.TargetField.Trim(),
            SourceExpression = x.SourceExpression.Trim(),
            IsRequired = x.IsRequired,
            IsActive = x.IsActive,
            SortOrder = x.SortOrder == 0 ? i * 10 : x.SortOrder
        }));

        await db.SaveChangesAsync();
        return await LoadStateAsync(db);
    }

    public async Task<PurchasingDataSourceState> ResetDefaultsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var site = await GetOrCreateSiteAsync(db);

        db.SapSourceDefinitions.RemoveRange(db.SapSourceDefinitions.Where(x => x.SiteId == site.Id));
        db.SapJoinDefinitions.RemoveRange(db.SapJoinDefinitions.Where(x => x.SiteId == site.Id));
        db.SapFieldMappings.RemoveRange(db.SapFieldMappings.Where(x => x.SiteId == site.Id));
        await db.SaveChangesAsync();

        AddDefaultSources(db, site.Id);
        AddDefaultJoins(db, site.Id);
        AddDefaultMappings(db, site.Id);
        await db.SaveChangesAsync();

        return await LoadStateAsync(db);
    }

    public async Task<PageActionResult> TestConnectionAsync(PurchasingDataSourceState state)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sourceSystem = await db.SourceSystemDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.Code == "SAP");

        var serviceUrl = string.IsNullOrWhiteSpace(state.Site.SapServiceUrl)
            ? sourceSystem?.CentralServiceUrl ?? string.Empty
            : state.Site.SapServiceUrl;
        var username = string.IsNullOrWhiteSpace(state.Site.UsernameOverride)
            ? sourceSystem?.CentralUsername ?? string.Empty
            : state.Site.UsernameOverride;
        var password = string.IsNullOrWhiteSpace(state.Site.PasswordOverride)
            ? sourceSystem?.CentralPassword ?? string.Empty
            : state.Site.PasswordOverride;

        if (string.IsNullOrWhiteSpace(serviceUrl))
            return PageActionResult.WarningResult("Keine SAP Service URL gepflegt.");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return PageActionResult.WarningResult("Keine SAP Gateway Zugangsdaten gepflegt.");

        try
        {
            await _sapGatewayService.TestConnectionAsync(serviceUrl.Trim(), username.Trim(), password);
            return PageActionResult.SuccessResult("SAP OData Verbindung erfolgreich.");
        }
        catch (Exception ex)
        {
            return PageActionResult.ErrorResult($"SAP OData Verbindung fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task<PurchasingDataSourceState> LoadStateAsync(AppDbContext db)
    {
        var site = await GetOrCreateSiteAsync(db);
        var sourceSystem = await db.SourceSystemDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.Code == "SAP");

        return new PurchasingDataSourceState
        {
            Site = Clone(site),
            SourceSystem = sourceSystem,
            Sources = await db.SapSourceDefinitions.AsNoTracking().Where(x => x.SiteId == site.Id).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(),
            Joins = await db.SapJoinDefinitions.AsNoTracking().Where(x => x.SiteId == site.Id).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(),
            Mappings = await db.SapFieldMappings.AsNoTracking().Where(x => x.SiteId == site.Id).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync()
        };
    }

    private static async Task EnsureDefaultsAsync(AppDbContext db)
    {
        var site = await GetOrCreateSiteAsync(db);
        var hasSources = await db.SapSourceDefinitions.AnyAsync(x => x.SiteId == site.Id);
        if (hasSources)
            return;

        AddDefaultSources(db, site.Id);
        AddDefaultJoins(db, site.Id);
        AddDefaultMappings(db, site.Id);
        await db.SaveChangesAsync();
    }

    private static async Task<Site> GetOrCreateSiteAsync(AppDbContext db)
    {
        var site = await db.Sites.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.TSC == PurchasingTsc);
        if (site is not null)
            return site;

        site = new Site
        {
            Schema = string.Empty,
            TSC = PurchasingTsc,
            Land = "Einkauf SAP",
            SourceSystem = "SAP",
            IsActive = false
        };
        db.Sites.Add(site);
        await db.SaveChangesAsync();
        return site;
    }

    private static void AddDefaultSources(AppDbContext db, int siteId)
    {
        db.SapSourceDefinitions.AddRange(
            new SapSourceDefinition { SiteId = siteId, Alias = "EKKO", EntitySet = "EKKOSet", IsPrimary = true, IsActive = true, SortOrder = 10 },
            new SapSourceDefinition { SiteId = siteId, Alias = "EKPO", EntitySet = "EKPOSet", IsPrimary = false, IsActive = true, SortOrder = 20 },
            new SapSourceDefinition { SiteId = siteId, Alias = "EKET", EntitySet = "eketSet", IsPrimary = false, IsActive = true, SortOrder = 30 },
            new SapSourceDefinition { SiteId = siteId, Alias = "LIEF", EntitySet = "Data", IsPrimary = false, IsActive = true, SortOrder = 40 },
            new SapSourceDefinition { SiteId = siteId, Alias = "WG", EntitySet = "Data2", IsPrimary = false, IsActive = true, SortOrder = 50 },
            new SapSourceDefinition { SiteId = siteId, Alias = "MARA", EntitySet = "MARA001Set", IsPrimary = false, IsActive = true, SortOrder = 60 });
    }

    private static void AddDefaultJoins(AppDbContext db, int siteId)
    {
        db.SapJoinDefinitions.AddRange(
            new SapJoinDefinition { SiteId = siteId, LeftAlias = "EKKO", RightAlias = "EKPO", LeftKeys = "Ebeln", RightKeys = "Ebeln", JoinType = "Left", IsActive = true, SortOrder = 10 },
            new SapJoinDefinition { SiteId = siteId, LeftAlias = "EKPO", RightAlias = "EKET", LeftKeys = "Ebeln,Ebelp", RightKeys = "Ebeln,Ebelp", JoinType = "Left", IsActive = true, SortOrder = 20 },
            new SapJoinDefinition { SiteId = siteId, LeftAlias = "EKKO", RightAlias = "LIEF", LeftKeys = "Lifnr", RightKeys = "Lifnr", JoinType = "Left", IsActive = true, SortOrder = 30 },
            new SapJoinDefinition { SiteId = siteId, LeftAlias = "EKPO", RightAlias = "WG", LeftKeys = "Matkl", RightKeys = "Matkl", JoinType = "Left", IsActive = true, SortOrder = 40 },
            new SapJoinDefinition { SiteId = siteId, LeftAlias = "EKPO", RightAlias = "MARA", LeftKeys = "Matnr", RightKeys = "Matnr", JoinType = "Left", IsActive = true, SortOrder = 50 });
    }

    private static void AddDefaultMappings(AppDbContext db, int siteId)
    {
        db.SapFieldMappings.AddRange(
            new SapFieldMapping { SiteId = siteId, TargetField = "PurchaseOrder", SourceExpression = "EKKO.Ebeln", IsRequired = true, IsActive = true, SortOrder = 10 },
            new SapFieldMapping { SiteId = siteId, TargetField = "PurchaseOrderDate", SourceExpression = "EKKO.Bedat", IsRequired = true, IsActive = true, SortOrder = 20 },
            new SapFieldMapping { SiteId = siteId, TargetField = "SupplierNumber", SourceExpression = "EKKO.Lifnr", IsRequired = false, IsActive = true, SortOrder = 30 },
            new SapFieldMapping { SiteId = siteId, TargetField = "SupplierName", SourceExpression = "LIEF.Name", IsRequired = false, IsActive = true, SortOrder = 40 },
            new SapFieldMapping { SiteId = siteId, TargetField = "Position", SourceExpression = "EKPO.Ebelp", IsRequired = true, IsActive = true, SortOrder = 50 },
            new SapFieldMapping { SiteId = siteId, TargetField = "Material", SourceExpression = "EKPO.Matnr", IsRequired = false, IsActive = true, SortOrder = 60 },
            new SapFieldMapping { SiteId = siteId, TargetField = "MaterialText", SourceExpression = "EKPO.Txz01", IsRequired = false, IsActive = true, SortOrder = 70 },
            new SapFieldMapping { SiteId = siteId, TargetField = "MaterialGroup", SourceExpression = "EKPO.Matkl", IsRequired = false, IsActive = true, SortOrder = 80 },
            new SapFieldMapping { SiteId = siteId, TargetField = "MaterialGroupText", SourceExpression = "WG.WgKomplett", IsRequired = false, IsActive = true, SortOrder = 90 },
            new SapFieldMapping { SiteId = siteId, TargetField = "NetValueChf", SourceExpression = "EKPO.NetwrChf", IsRequired = false, IsActive = true, SortOrder = 100 },
            new SapFieldMapping { SiteId = siteId, TargetField = "NetValueChfPerPiece", SourceExpression = "EKPO.NetwrChfStk", IsRequired = false, IsActive = true, SortOrder = 110 },
            new SapFieldMapping { SiteId = siteId, TargetField = "OrderQuantity", SourceExpression = "EKPO.Menge", IsRequired = false, IsActive = true, SortOrder = 120 },
            new SapFieldMapping { SiteId = siteId, TargetField = "ScheduleDate", SourceExpression = "EKET.Eindt", IsRequired = false, IsActive = true, SortOrder = 130 },
            new SapFieldMapping { SiteId = siteId, TargetField = "ScheduleQuantity", SourceExpression = "EKET.Menge", IsRequired = false, IsActive = true, SortOrder = 140 },
            new SapFieldMapping { SiteId = siteId, TargetField = "MaterialStatus", SourceExpression = "MARA.Mstae", IsRequired = false, IsActive = true, SortOrder = 150 });
    }

    private static void Replace<TEntity>(AppDbContext db, IQueryable<TEntity> oldRows, IEnumerable<TEntity> newRows)
        where TEntity : class
    {
        var set = db.Set<TEntity>();
        set.RemoveRange(oldRows);
        set.AddRange(newRows);
    }

    private static Site Clone(Site site) => new()
    {
        Id = site.Id,
        HanaServerId = site.HanaServerId,
        Schema = site.Schema,
        TSC = site.TSC,
        Land = site.Land,
        SourceSystem = site.SourceSystem,
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
    };
}
