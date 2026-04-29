using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IManagementCockpitService
{
    Task<List<ManagementCockpitFileOption>> GetAvailableFilesAsync();
    IReadOnlyList<ManagementCockpitValueFieldOption> GetValueFieldOptions();
    Task<ManagementCockpitResult> AnalyzeAsync(string filePath);
    Task<ManagementCockpitResult> AnalyzeAsync(string filePath, ManagementCockpitAnalysisOptions? options);
    Task<List<int>> GetAvailableCentralYearsAsync();
    Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month);
    Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month, ManagementCockpitAnalysisOptions? options);
}
