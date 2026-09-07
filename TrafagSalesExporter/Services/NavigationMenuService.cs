using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public sealed class NavigationMenuService : INavigationMenuService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public NavigationMenuService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<NavigationMenuItem>> GetItemsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NavigationMenuItems
            .AsNoTracking()
            .OrderBy(x => x.ParentKey ?? string.Empty)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.TitleDe)
            .Select(x => new NavigationMenuItem
            {
                Id = x.Id,
                Key = x.Key ?? string.Empty,
                ParentKey = string.IsNullOrWhiteSpace(x.ParentKey) ? null : x.ParentKey,
                TitleDe = x.TitleDe ?? string.Empty,
                TitleEn = x.TitleEn ?? string.Empty,
                Icon = x.Icon ?? string.Empty,
                Href = x.Href ?? string.Empty,
                ItemType = x.ItemType ?? NavigationMenuItemTypes.Link,
                Match = x.Match ?? "Prefix",
                RequiredPolicy = x.RequiredPolicy ?? string.Empty,
                IsVisible = x.IsVisible,
                IsExpanded = x.IsExpanded,
                IsSystem = x.IsSystem,
                SortOrder = x.SortOrder
            })
            .ToListAsync();
    }

    public async Task SaveItemsAsync(IEnumerable<NavigationMenuItem> items)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var incoming = items.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var existing = await db.NavigationMenuItems.ToListAsync();

        foreach (var item in existing)
        {
            if (!incoming.TryGetValue(item.Key, out var source))
                continue;

            item.ParentKey = string.IsNullOrWhiteSpace(source.ParentKey) ? null : source.ParentKey;
            item.SortOrder = source.SortOrder;
            item.IsVisible = source.IsVisible;
            item.IsExpanded = source.IsExpanded;
            item.TitleDe = source.TitleDe.Trim();
            item.TitleEn = source.TitleEn.Trim();
        }

        await db.SaveChangesAsync();
    }

    public async Task ResetToDefaultsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.NavigationMenuItems.RemoveRange(db.NavigationMenuItems);
        await db.SaveChangesAsync();

        new DatabaseSeedService().SeedDefaults(db);
    }
}
