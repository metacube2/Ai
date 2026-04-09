using Microsoft.Extensions.Configuration;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter;

internal static class Program
{
    private static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var appConfig = config.Get<AppConfig>() ?? throw new InvalidOperationException("Konfiguration konnte nicht geladen werden.");

        var hanaService = new HanaQueryService();
        var excelService = new ExcelExportService();
        var sharePointService = new SharePointUploadService(
            appConfig.SharePoint.TenantId,
            appConfig.SharePoint.ClientId,
            appConfig.SharePoint.ClientSecret,
            appConfig.SharePoint.SiteUrl,
            appConfig.SharePoint.ExportFolder);

        var outputDir = Path.Combine(AppContext.BaseDirectory, "output");

        foreach (var site in appConfig.Sites)
        {
            try
            {
                Log($"Starte Standort: {site.Land} ({site.Schema})");

                if (!appConfig.HanaServers.TryGetValue(site.Server, out var serverConfig))
                {
                    throw new InvalidOperationException($"HANA Server-Konfiguration '{site.Server}' nicht gefunden.");
                }

                var records = hanaService.GetSalesRecords(
                    serverConfig.Host,
                    serverConfig.Port,
                    serverConfig.Username,
                    serverConfig.Password,
                    site.Schema,
                    site.TSC,
                    site.Land);

                var filePath = excelService.CreateExcelFile(outputDir, site.TSC, DateTime.UtcNow.Date, records);
                Log($"Excel erzeugt: {filePath}");

                await sharePointService.UploadAsync(site.Land, filePath);
                Log($"Upload abgeschlossen: {site.Land}");
            }
            catch (Exception ex)
            {
                Log($"Fehler bei Standort {site.Land}: {ex.Message}");
            }
        }

        Log("Export beendet.");
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }
}

public class AppConfig
{
    public Dictionary<string, HanaServerConfig> HanaServers { get; set; } = new();
    public List<SiteConfig> Sites { get; set; } = new();
    public SharePointConfig SharePoint { get; set; } = new();
    public string DateFilter { get; set; } = "2025-01-01";
}

public class HanaServerConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SiteConfig
{
    public string Schema { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string TSC { get; set; } = string.Empty;
    public string Land { get; set; } = string.Empty;
}

public class SharePointConfig
{
    public string SiteUrl { get; set; } = string.Empty;
    public string ExportFolder { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
