using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public sealed class ProjectManagementService : IProjectManagementService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ProjectManagementService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<ProjectItem>> GetProjectsAsync(bool includeArchived = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ProjectItems
            .AsNoTracking()
            .Where(x => includeArchived || !x.IsArchived)
            .OrderBy(x => x.Status == ProjectStatuses.Completed)
            .ThenBy(x => x.DueDate == null)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.Title)
            .ToListAsync();
    }

    public async Task<ProjectItem> SaveAsync(ProjectItem project)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(project.Title);
        project.ProgressPercent = Math.Clamp(project.ProgressPercent, 0, 100);
        var now = DateTime.UtcNow;

        await using var db = await _dbFactory.CreateDbContextAsync();
        ProjectItem entity;
        if (project.Id == 0)
        {
            entity = new ProjectItem { CreatedAtUtc = now };
            db.ProjectItems.Add(entity);
        }
        else
        {
            entity = await db.ProjectItems.FirstAsync(x => x.Id == project.Id);
        }

        entity.Title = project.Title.Trim();
        entity.Description = project.Description?.Trim() ?? string.Empty;
        entity.Status = ProjectStatuses.All.Contains(project.Status) ? project.Status : ProjectStatuses.Idea;
        entity.Priority = ProjectPriorities.All.Contains(project.Priority) ? project.Priority : ProjectPriorities.Normal;
        entity.Owner = project.Owner?.Trim() ?? string.Empty;
        entity.StartDate = project.StartDate;
        entity.DueDate = project.DueDate;
        entity.ProgressPercent = project.ProgressPercent;
        entity.Notes = project.Notes?.Trim() ?? string.Empty;
        entity.IsArchived = project.IsArchived;
        entity.UpdatedAtUtc = now;

        await db.SaveChangesAsync();
        return entity;
    }

    public async Task ArchiveAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.ProjectItems.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return;

        entity.IsArchived = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
