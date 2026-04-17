using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IManagementCockpitPageService
{
    Task<ManagementCockpitPageState> InitializeAsync(string? selectedFilePath, int selectedCentralYear);
    Task<List<ManagementCockpitFileOption>> LoadFilesAsync();
    Task<List<int>> LoadCentralYearsAsync();
    Task<ManagementCockpitResult> AnalyzeAsync(string filePath);
    Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month);
}

public sealed class ManagementCockpitPageService : IManagementCockpitPageService
{
    private readonly IManagementCockpitService _cockpitService;

    public ManagementCockpitPageService(IManagementCockpitService cockpitService)
    {
        _cockpitService = cockpitService;
    }

    public async Task<ManagementCockpitPageState> InitializeAsync(string? selectedFilePath, int selectedCentralYear)
    {
        var files = await _cockpitService.GetAvailableFilesAsync();
        var years = await _cockpitService.GetAvailableCentralYearsAsync();

        return new ManagementCockpitPageState
        {
            Files = files,
            CentralYears = years,
            SelectedFilePath = selectedFilePath ?? files.FirstOrDefault()?.Path,
            SelectedCentralYear = selectedCentralYear == 0 ? years.LastOrDefault() : selectedCentralYear
        };
    }

    public Task<List<ManagementCockpitFileOption>> LoadFilesAsync()
        => _cockpitService.GetAvailableFilesAsync();

    public Task<List<int>> LoadCentralYearsAsync()
        => _cockpitService.GetAvailableCentralYearsAsync();

    public Task<ManagementCockpitResult> AnalyzeAsync(string filePath)
        => _cockpitService.AnalyzeAsync(filePath);

    public Task<ManagementCockpitCentralResult> AnalyzeCentralAsync(int year, int? month)
        => _cockpitService.AnalyzeCentralAsync(year, month);
}

public sealed class ManagementCockpitPageState
{
    public List<ManagementCockpitFileOption> Files { get; set; } = [];
    public List<int> CentralYears { get; set; } = [];
    public string? SelectedFilePath { get; set; }
    public int SelectedCentralYear { get; set; }
}
