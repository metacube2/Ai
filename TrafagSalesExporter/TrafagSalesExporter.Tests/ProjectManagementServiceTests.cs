using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class ProjectManagementServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;

    public ProjectManagementServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("DROP TABLE ProjectItems");
        new DatabaseSchemaMaintenanceService().EnsureSchema(db);
        Assert.True(db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type='table' AND name='ProjectItems'").Single() == 1);

        _dbFactory = new TestDbContextFactory(options);
    }

    [Fact]
    public async Task Save_Load_And_Archive_Project()
    {
        var service = new ProjectManagementService(_dbFactory);
        var saved = await service.SaveAsync(new ProjectItem
        {
            Title = "Lieferantenbesuch vorbereiten",
            Owner = "Eric",
            Status = ProjectStatuses.InProgress,
            Priority = ProjectPriorities.High,
            ProgressPercent = 130,
            DueDate = new DateTime(2026, 8, 15)
        });

        Assert.True(saved.Id > 0);
        Assert.Equal(100, saved.ProgressPercent);
        var active = await service.GetProjectsAsync();
        Assert.Single(active);
        Assert.Equal("Eric", active[0].Owner);

        await service.ArchiveAsync(saved.Id);
        Assert.Empty(await service.GetProjectsAsync());
        Assert.Single(await service.GetProjectsAsync(includeArchived: true));
    }

    public void Dispose() => _connection.Dispose();
}
