using System.Reflection;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Sichert den Cache der zentralen Datensaetze im Management-Cockpit (2026-09-28): gleiche Quelle
/// wird nicht neu eingelesen, eine neu geschriebene Audit-CSV sofort.
/// </summary>
public sealed class ManagementCockpitCentralRecordsCacheTests
{
    private sealed class CountingProvider : ICentralSalesDataProvider
    {
        public int Reads { get; private set; }
        public string? Stamp { get; set; } = "csv|a";

        public Task<List<SalesRecord>> GetRecordsAsync()
        {
            Reads++;
            return Task.FromResult(new List<SalesRecord>
            {
                new() { InvoiceDate = new DateTime(2025, 5, 1), ExtractionDate = new DateTime(2025, 5, 2) }
            });
        }

        public Task<List<SalesRecord>> GetLatestRecordsBySiteAsync() => GetRecordsAsync();
        public Task<bool> UsesAuditCsvAsync() => Task.FromResult(true);
        public Task<string?> GetSourceStampAsync() => Task.FromResult(Stamp);
    }

    private static void AgeCache(ManagementCockpitService service, TimeSpan age)
        => typeof(ManagementCockpitService)
            .GetField("_centralRecordsCacheAtUtc", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(service, DateTime.UtcNow - age);

    private static ManagementCockpitService Create(CountingProvider provider)
        => new(null!, new CurrencyExchangeRateService(null!), provider);

    [Fact]
    public async Task Gleiche_Quelle_Wird_Nach_Zehn_Sekunden_Nicht_Neu_Eingelesen()
    {
        var provider = new CountingProvider();
        var service = Create(provider);

        await service.GetAvailableCentralYearsAsync();
        AgeCache(service, TimeSpan.FromMinutes(5));
        await service.GetAvailableCentralYearsAsync();

        Assert.Equal(1, provider.Reads);
    }

    [Fact]
    public async Task Neue_Audit_Csv_Wird_Sofort_Eingelesen()
    {
        var provider = new CountingProvider();
        var service = Create(provider);

        await service.GetAvailableCentralYearsAsync();
        AgeCache(service, TimeSpan.FromMinutes(5));
        provider.Stamp = "csv|b";
        await service.GetAvailableCentralYearsAsync();

        Assert.Equal(2, provider.Reads);
    }

    [Fact]
    public async Task Nach_Dreissig_Minuten_Oder_Ohne_Kennzeichen_Wird_Neu_Eingelesen()
    {
        var provider = new CountingProvider();
        var service = Create(provider);

        await service.GetAvailableCentralYearsAsync();
        AgeCache(service, TimeSpan.FromMinutes(31));
        await service.GetAvailableCentralYearsAsync();
        Assert.Equal(2, provider.Reads);

        provider.Stamp = null;
        AgeCache(service, TimeSpan.FromSeconds(11));
        await service.GetAvailableCentralYearsAsync();
        Assert.Equal(3, provider.Reads);
    }
}
