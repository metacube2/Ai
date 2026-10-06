using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class NavigationMenuSeedTests
{
    // 2026-10-06: Weltlage liegt unter Finance Cockpit; alte Installationen mit Weltlage auf oberster Ebene werden verschoben.
    [Fact]
    public void SeedDefaults_Verschiebt_Weltlage_Unter_Finance()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        db.NavigationMenuItems.Add(new NavigationMenuItem
        {
            Key = "world", ParentKey = null, TitleDe = "Weltlage", TitleEn = "World situation", Icon = "TravelExplore",
            ItemType = NavigationMenuItemTypes.Group, SortOrder = 38
        });
        db.SaveChanges();

        new DatabaseSeedService().SeedDefaults(db);

        var world = db.NavigationMenuItems.Single(x => x.Key == "world");
        Assert.Equal("finance", world.ParentKey);
        Assert.Equal(60, world.SortOrder);
        Assert.Equal("world", db.NavigationMenuItems.Single(x => x.Key == "world-radar").ParentKey);
    }

    // 2026-10-06: Benutzerhandbuch, Journal Import, Marktsegmente und Projekte zeigten einen grauen Kreis,
    // weil ihr Seed-Icon im Resolver fehlte.
    [Fact]
    public void SeedDefaults_JedesMenueIcon_Ist_Aufgeloest()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        new DatabaseSeedService().SeedDefaults(db);

        var missing = db.NavigationMenuItems.AsEnumerable()
            .Where(x => !string.IsNullOrEmpty(x.Icon) && NavigationIconResolver.Resolve(x.Icon) == MudBlazor.Icons.Material.Filled.Circle)
            .Select(x => x.Key + ":" + x.Icon)
            .ToList();
        Assert.Empty(missing);
        Assert.Contains(db.NavigationMenuItems, x => x.Key == "user-manual" && x.ParentKey == null && x.Href == "handbuch");
    }

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
