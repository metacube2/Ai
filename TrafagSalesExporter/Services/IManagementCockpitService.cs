using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IManagementCockpitService
{
    Task<List<ManagementCockpitFileOption>> GetAvailableFilesAsync();
    Task<ManagementCockpitResult> AnalyzeAsync(string filePath);
    Task<List<int>> GetAvailableCentralYearsAsync();
    Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month);
}
