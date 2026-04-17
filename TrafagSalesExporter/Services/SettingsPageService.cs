using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface ISettingsPageService
{
    Task<SettingsPageState> LoadAsync();
    Task SaveSharePointAsync(SharePointConfig config);
    Task<string> BuildSharePointTestPreviewAsync(SharePointConfig config);
    Task SaveExportSettingsAsync(ExportSettings settings);
    Task<List<SourceSystemDefinition>> SaveSourceSystemsAsync(List<SourceSystemDefinition> sourceSystems);
    Task<List<CurrencyExchangeRate>> SaveExchangeRatesAsync(List<CurrencyExchangeRate> exchangeRates);
    Task<SettingsExchangeRateRefreshResult> RefreshEcbRatesAsync();
    Task<string> ExportConfigurationAsync(bool includeSecrets);
    Task<SettingsPageState> ImportConfigurationAsync(string json);
    Task<PageActionResult> TestCentralCredentialsAsync(SourceSystemDefinition definition);
}

public sealed class SettingsPageService : ISettingsPageService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ISharePointUploadService _sharePointService;
    private readonly TimerBackgroundService _timerService;
    private readonly IHanaQueryService _hanaService;
    private readonly ISapGatewayService _sapGatewayService;
    private readonly IConfigTransferService _configTransferService;
    private readonly IExchangeRateImportService _exchangeRateImportService;

    public SettingsPageService(
        IDbContextFactory<AppDbContext> dbFactory,
        ISharePointUploadService sharePointService,
        TimerBackgroundService timerService,
        IHanaQueryService hanaService,
        ISapGatewayService sapGatewayService,
        IConfigTransferService configTransferService,
        IExchangeRateImportService exchangeRateImportService)
    {
        _dbFactory = dbFactory;
        _sharePointService = sharePointService;
        _timerService = timerService;
        _hanaService = hanaService;
        _sapGatewayService = sapGatewayService;
        _configTransferService = configTransferService;
        _exchangeRateImportService = exchangeRateImportService;
    }

    public async Task<SettingsPageState> LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return new SettingsPageState
        {
            SharePointConfig = await db.SharePointConfigs.FirstOrDefaultAsync() ?? new SharePointConfig(),
            ExportSettings = await db.ExportSettings.FirstOrDefaultAsync() ?? new ExportSettings(),
            SourceSystems = await db.SourceSystemDefinitions.OrderBy(x => x.Code).ToListAsync(),
            ExchangeRates = await LoadExchangeRatesAsync(db)
        };
    }

    public async Task SaveSharePointAsync(SharePointConfig config)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.SharePointConfigs.FirstOrDefaultAsync();
        if (existing is null)
        {
            db.SharePointConfigs.Add(config);
        }
        else
        {
            existing.SiteUrl = config.SiteUrl;
            existing.ExportFolder = config.ExportFolder;
            existing.CentralExportFolder = config.CentralExportFolder;
            existing.TenantId = config.TenantId;
            existing.ClientId = config.ClientId;
            existing.ClientSecret = config.ClientSecret;
        }

        await db.SaveChangesAsync();
    }

    public async Task<string> BuildSharePointTestPreviewAsync(SharePointConfig config)
    {
        var tenantId = NormalizeConfigValue(config.TenantId);
        var clientId = NormalizeConfigValue(config.ClientId);
        var clientSecret = NormalizeConfigValue(config.ClientSecret);
        var siteUrl = NormalizeConfigValue(config.SiteUrl);

        await _sharePointService.TestConnectionAsync(tenantId, clientId, clientSecret, siteUrl);
        return BuildSharePointTestPreview(tenantId, clientId, clientSecret, siteUrl);
    }

    public async Task SaveExportSettingsAsync(ExportSettings settings)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.ExportSettings.FirstOrDefaultAsync();
        if (existing is null)
        {
            db.ExportSettings.Add(settings);
        }
        else
        {
            existing.DateFilter = settings.DateFilter;
            existing.TimerHour = settings.TimerHour;
            existing.TimerMinute = settings.TimerMinute;
            existing.TimerEnabled = settings.TimerEnabled;
            existing.DebugLoggingEnabled = settings.DebugLoggingEnabled;
            existing.LocalSiteExportFolder = settings.LocalSiteExportFolder;
            existing.LocalConsolidatedExportFolder = settings.LocalConsolidatedExportFolder;
        }

        await db.SaveChangesAsync();
        _timerService.Recalculate();
    }

    public async Task<List<SourceSystemDefinition>> SaveSourceSystemsAsync(List<SourceSystemDefinition> sourceSystems)
    {
        var normalized = sourceSystems
            .Select(x => new SourceSystemDefinition
            {
                Id = x.Id,
                Code = NormalizeSourceSystemCode(x.Code),
                DisplayName = NormalizeConfigValue(x.DisplayName),
                ConnectionKind = NormalizeConnectionKind(x.ConnectionKind),
                IsActive = x.IsActive,
                CentralServiceUrl = NormalizeConfigValue(x.CentralServiceUrl),
                CentralUsername = NormalizeConfigValue(x.CentralUsername),
                CentralPassword = x.CentralPassword ?? string.Empty
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .ToList();

        if (normalized.Any(x => string.IsNullOrWhiteSpace(x.DisplayName)))
            throw new InvalidOperationException("Jedes Quellsystem braucht einen Anzeigenamen.");

        var duplicates = normalized.GroupBy(x => x.Code).FirstOrDefault(g => g.Count() > 1);
        if (duplicates is not null)
            throw new InvalidOperationException($"Quellsystem-Code doppelt vorhanden: {duplicates.Key}");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.SourceSystemDefinitions.ToListAsync();
        if (existing.Count > 0)
            db.SourceSystemDefinitions.RemoveRange(existing);

        db.SourceSystemDefinitions.AddRange(normalized);
        await db.SaveChangesAsync();
        return await db.SourceSystemDefinitions.OrderBy(x => x.Code).ToListAsync();
    }

    public async Task<List<CurrencyExchangeRate>> SaveExchangeRatesAsync(List<CurrencyExchangeRate> exchangeRates)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existingRates = await db.CurrencyExchangeRates.ToListAsync();
        if (existingRates.Count > 0)
            db.CurrencyExchangeRates.RemoveRange(existingRates);

        db.CurrencyExchangeRates.AddRange(exchangeRates.Select(rate => new CurrencyExchangeRate
        {
            FromCurrency = NormalizeConfigValue(rate.FromCurrency).ToUpperInvariant(),
            ToCurrency = NormalizeConfigValue(rate.ToCurrency).ToUpperInvariant(),
            Rate = rate.Rate,
            ValidFrom = rate.ValidFrom.Date,
            ValidTo = rate.ValidTo?.Date,
            Notes = NormalizeConfigValue(rate.Notes),
            IsActive = rate.IsActive
        }).Where(rate => !string.IsNullOrWhiteSpace(rate.FromCurrency)
            && !string.IsNullOrWhiteSpace(rate.ToCurrency)
            && rate.Rate > 0m));

        await db.SaveChangesAsync();
        return await LoadExchangeRatesAsync(db);
    }

    public async Task<SettingsExchangeRateRefreshResult> RefreshEcbRatesAsync()
    {
        var result = await _exchangeRateImportService.RefreshEcbRatesAsync();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return new SettingsExchangeRateRefreshResult
        {
            ImportedCount = result.ImportedCount,
            RateDate = result.RateDate,
            ExchangeRates = await LoadExchangeRatesAsync(db)
        };
    }

    public Task<string> ExportConfigurationAsync(bool includeSecrets)
        => _configTransferService.ExportJsonAsync(includeSecrets);

    public async Task<SettingsPageState> ImportConfigurationAsync(string json)
    {
        await _configTransferService.ImportJsonAsync(json);
        _timerService.Recalculate();
        return await LoadAsync();
    }

    public async Task<PageActionResult> TestCentralCredentialsAsync(SourceSystemDefinition definition)
    {
        if (string.Equals(definition.ConnectionKind, SourceSystemConnectionKinds.SapGateway, StringComparison.OrdinalIgnoreCase))
            return await TestCentralSapCredentialsAsync(definition);

        if (string.Equals(definition.ConnectionKind, SourceSystemConnectionKinds.Hana, StringComparison.OrdinalIgnoreCase))
            return await TestCentralHanaCredentialsAsync(definition);

            return PageActionResult.WarningResult($"Quellsystem '{definition.Code}' hat keinen testbaren Verbindungstyp.");
    }

    private async Task<PageActionResult> TestCentralHanaCredentialsAsync(SourceSystemDefinition definition)
    {
        var sourceSystem = definition.Code;
        var username = definition.CentralUsername;
        var password = definition.CentralPassword;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return PageActionResult.WarningResult($"Fuer {sourceSystem} sind keine zentralen Zugangsdaten gepflegt.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var centralServer = await db.HanaServers
            .Where(s => s.SourceSystem == sourceSystem)
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync();

        if (centralServer is null || string.IsNullOrWhiteSpace(centralServer.Host))
            return PageActionResult.WarningResult($"Keine zentrale HANA-Konfiguration fuer {sourceSystem} gefunden.");

        var testServer = new HanaServer
        {
            SourceSystem = sourceSystem,
            Name = $"{sourceSystem} Central Test",
            Host = centralServer.Host,
            Port = centralServer.Port,
            Username = username.Trim(),
            Password = password.Trim(),
            DatabaseName = centralServer.DatabaseName,
            UseSsl = centralServer.UseSsl,
            ValidateCertificate = centralServer.ValidateCertificate,
            AdditionalParams = centralServer.AdditionalParams
        };

        var result = await Task.Run(() => _hanaService.TestConnectionDetailed(testServer));
        return result.Success
            ? PageActionResult.SuccessResult($"{sourceSystem}: Zentrale HANA-Verbindung erfolgreich.")
            : PageActionResult.ErrorResult($"{sourceSystem}: {result.ExceptionType} - {result.ErrorMessage}");
    }

    private async Task<PageActionResult> TestCentralSapCredentialsAsync(SourceSystemDefinition definition)
    {
        var sourceSystem = definition.Code;
        var username = definition.CentralUsername;
        var password = definition.CentralPassword;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return PageActionResult.WarningResult("Fuer SAP sind keine zentralen Gateway-Zugangsdaten gepflegt.");

        if (string.IsNullOrWhiteSpace(definition.CentralServiceUrl))
            return PageActionResult.WarningResult($"Fuer {sourceSystem} ist keine zentrale SAP Service URL gepflegt.");

        try
        {
            await _sapGatewayService.TestConnectionAsync(definition.CentralServiceUrl, username.Trim(), password.Trim());
            return PageActionResult.SuccessResult($"{sourceSystem}: Zentrale SAP Gateway-Verbindung erfolgreich.");
        }
        catch (Exception ex)
        {
            return PageActionResult.ErrorResult($"{sourceSystem}: {ex.Message}");
        }
    }

    private static async Task<List<CurrencyExchangeRate>> LoadExchangeRatesAsync(AppDbContext db)
        => await db.CurrencyExchangeRates
            .OrderBy(x => x.FromCurrency)
            .ThenBy(x => x.ToCurrency)
            .ThenByDescending(x => x.ValidFrom)
            .ToListAsync();

    public static string NormalizeSourceSystemCode(string? code) => NormalizeConfigValue(code).ToUpperInvariant();

    public static string NormalizeConnectionKind(string? connectionKind)
        => SourceSystemConnectionKinds.All.Contains(connectionKind ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            ? (connectionKind ?? string.Empty).Trim().ToUpperInvariant()
            : SourceSystemConnectionKinds.Hana;

    public static string NormalizeConfigValue(string? value) => value?.Trim() ?? string.Empty;

    public static string BuildSharePointTestPreview(string tenantId, string clientId, string clientSecret, string siteUrl)
    {
        var maskedSecret = string.IsNullOrEmpty(clientSecret)
            ? "<leer>"
            : $"{new string('*', Math.Min(clientSecret.Length, 8))} (len={clientSecret.Length})";

        return string.Join(Environment.NewLine,
        [
            $"Tenant ID: {tenantId}",
            $"Client ID: {clientId}",
            $"Client Secret: {maskedSecret}",
            $"Site URL: {siteUrl}"
        ]);
    }
}

public sealed class SettingsPageState
{
    public SharePointConfig SharePointConfig { get; set; } = new();
    public ExportSettings ExportSettings { get; set; } = new();
    public List<SourceSystemDefinition> SourceSystems { get; set; } = [];
    public List<CurrencyExchangeRate> ExchangeRates { get; set; } = [];
}

public sealed class SettingsExchangeRateRefreshResult
{
    public int ImportedCount { get; set; }
    public DateTime RateDate { get; set; }
    public List<CurrencyExchangeRate> ExchangeRates { get; set; } = [];
}

public sealed class PageActionResult
{
    public bool Success { get; init; }
    public bool Warning { get; init; }
    public string Message { get; init; } = string.Empty;

    public static PageActionResult SuccessResult(string message) => new() { Success = true, Message = message };
    public static PageActionResult WarningResult(string message) => new() { Warning = true, Message = message };
    public static PageActionResult ErrorResult(string message) => new() { Message = message };
}
