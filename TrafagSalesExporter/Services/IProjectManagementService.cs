using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IProjectManagementService
{
    Task<List<ProjectItem>> GetProjectsAsync(bool includeArchived = false);
    Task<ProjectItem> SaveAsync(ProjectItem project);
    Task ArchiveAsync(int id);
}
