using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class NavigationMenuSeedTests
{
    private static readonly string[] AdminChildKeys =
    [
        "admin-sessions",
        "sites",
        "transformations",
        "finance-rules",
        "settings",
        "menu-structure",
        "logs"
    ];

    [Fact]
    public void SeedDefaults_CreatesSingleRootAdminAreaWithAllAdminChildren()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        new DatabaseSeedService().SeedDefaults(db);

        var adminArea = db.NavigationMenuItems.Single(x => x.Key == "finance-admin");
        Assert.Null(adminArea.ParentKey);
        Assert.Equal("Admin Bereich", adminArea.TitleDe);
        Assert.Equal(NavigationMenuItemTypes.Group, adminArea.ItemType);
        Assert.Empty(adminArea.Href);

        var children = db.NavigationMenuItems
            .Where(x => AdminChildKeys.Contains(x.Key))
            .OrderBy(x => x.SortOrder)
            .ToList();
        Assert.Equal(AdminChildKeys, children.Select(x => x.Key));
        Assert.All(children, child => Assert.Equal("finance-admin", child.ParentKey));

        var sessions = children.Single(x => x.Key == "admin-sessions");
        Assert.Equal("Aktive Logins", sessions.TitleDe);
        Assert.Equal("admin/sessions", sessions.Href);
    }

    [Fact]
    public void SeedDefaults_MigratesLegacyDefaultAdminStructure()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        var seed = new DatabaseSeedService();
        seed.SeedDefaults(db);

        var adminArea = db.NavigationMenuItems.Single(x => x.Key == "finance-admin");
        adminArea.ParentKey = "finance";
        adminArea.TitleDe = "Admin";
        adminArea.TitleEn = "Admin";
        adminArea.SortOrder = 60;
        var sessions = db.NavigationMenuItems.Single(x => x.Key == "admin-sessions");
        sessions.ParentKey = null;
        sessions.TitleDe = "Admin Bereich";
        sessions.TitleEn = "Admin area";
        sessions.SortOrder = 90;
        db.SaveChanges();

        seed.SeedDefaults(db);

        Assert.Null(adminArea.ParentKey);
        Assert.Equal("Admin Bereich", adminArea.TitleDe);
        Assert.Equal(90, adminArea.SortOrder);
        Assert.Equal("finance-admin", sessions.ParentKey);
        Assert.Equal("Aktive Logins", sessions.TitleDe);
        Assert.Equal(10, sessions.SortOrder);
    }
}
