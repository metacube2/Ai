using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Pflege der Marktumfrage in der Anwendung. Kernpunkt ist, dass eine Umfragezeile OHNE
/// verknuepften Verkaufskunden gueltig bleibt: 90 der 269 Railway-Zeilen beschreiben
/// Interessenten ohne Umsatz, und die duerfen nicht verloren gehen.
/// </summary>
public class MarketSurveyPageServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MarketSurveyPageServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    private MarketSurveyPageService CreateService()
        => new(new TestDbContextFactory(_options), new NullAppEventLog());

    private static MarketSurveyEntry Entry(
        string customer,
        string country = "AT",
        string application = "Brakes",
        string status = "Existing Customer",
        string survey = "Railway 2026-05")
        => new()
        {
            SurveyName = survey,
            Country = country,
            CustomerName = customer,
            Application = application,
            Status = status
        };

    [Fact]
    public async Task Save_CreatesEntryWithoutLinkedSalesCustomer()
    {
        var service = CreateService();

        var saved = await service.SaveAsync(Entry("Zillertalbahn"));

        Assert.True(saved.Id > 0);
        Assert.Equal("Zillertalbahn", saved.CustomerName);
        // Interessent ohne Umsatz: die Verknuepfung bleibt leer und das ist gueltig.
        Assert.Equal(string.Empty, saved.LinkedCustomerNumber);
        Assert.Equal(string.Empty, saved.LinkedTsc);
    }

    [Fact]
    public async Task Save_WithoutCustomerName_Throws()
    {
        var service = CreateService();
        var entry = Entry("  ");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync(entry));
    }

    [Fact]
    public async Task Save_NormalizesCountryAndLink()
    {
        var service = CreateService();
        var entry = Entry("Schunk");
        entry.Country = " at ";
        entry.LinkedTsc = " trch ";
        entry.LinkedCustomerNumber = " ab100 ";

        var saved = await service.SaveAsync(entry);

        Assert.Equal("AT", saved.Country);
        Assert.Equal("TRCH", saved.LinkedTsc);
        Assert.Equal("AB100", saved.LinkedCustomerNumber);
    }

    [Fact]
    public async Task Save_UpdatesExistingInsteadOfDuplicating()
    {
        var service = CreateService();
        var saved = await service.SaveAsync(Entry("PJM"));

        saved.Competitor = "Gefran";
        saved.EstimatedQuantity = "500-600 pcs";
        await service.SaveAsync(saved);

        var rows = await service.SearchAsync(null, null, null, null);
        var single = Assert.Single(rows);
        Assert.Equal("Gefran", single.Competitor);
        // Freitext bleibt unveraendert erhalten, es wird nichts in eine Zahl gepresst.
        Assert.Equal("500-600 pcs", single.EstimatedQuantity);
    }

    [Fact]
    public async Task SameCustomerCanAppearTwiceForDifferentApplications()
    {
        // In der Railway-Umfrage stehen 269 Zeilen fuer 236 Kunden. Ein eindeutiger Index
        // auf Umfrage plus Kunde wuerde diese Faelle abweisen.
        var service = CreateService();
        await service.SaveAsync(Entry("PJM", application: "Test benches"));
        await service.SaveAsync(Entry("PJM", application: "Brakes"));

        var rows = await service.SearchAsync(null, "PJM", null, null);

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Application == "Test benches");
        Assert.Contains(rows, r => r.Application == "Brakes");
    }

    [Fact]
    public async Task Search_FiltersByTextCountryAndStatus()
    {
        var service = CreateService();
        await service.SaveAsync(Entry("Wiener Linien", country: "AT", status: "Existing Customer"));
        await service.SaveAsync(Entry("Alstom", country: "FR", status: "Opportunity", application: "HVAC"));

        Assert.Single(await service.SearchAsync(null, "Wiener", null, null));
        Assert.Single(await service.SearchAsync(null, null, "FR", null));
        Assert.Single(await service.SearchAsync(null, null, null, "Opportunity"));
        Assert.Equal(2, (await service.SearchAsync(null, null, null, null)).Count);
    }

    [Fact]
    public async Task Search_FindsByApplicationProductCompetitorAndComment()
    {
        var service = CreateService();
        var entry = Entry("Aquasys", application: "Fire protection");
        entry.Product = "DPS";
        entry.Competitor = "Suko";
        entry.Comments = "haben umgestellt auf EPR";
        await service.SaveAsync(entry);

        Assert.Single(await service.SearchAsync(null, "Fire", null, null));
        Assert.Single(await service.SearchAsync(null, "DPS", null, null));
        Assert.Single(await service.SearchAsync(null, "Suko", null, null));
        Assert.Single(await service.SearchAsync(null, "EPR", null, null));
    }

    [Fact]
    public async Task Summary_CountsLinkedAndUnlinked()
    {
        var service = CreateService();
        var linked = Entry("Siemens Mobility");
        linked.LinkedTsc = "TRCH";
        linked.LinkedCustomerNumber = "22987";
        await service.SaveAsync(linked);
        await service.SaveAsync(Entry("Zillertalbahn"));

        var summary = Assert.Single(await service.GetSummariesAsync());

        Assert.Equal("Railway 2026-05", summary.SurveyName);
        Assert.Equal(2, summary.Entries);
        Assert.Equal(2, summary.Customers);
        Assert.Equal(1, summary.LinkedEntries);
        Assert.Equal(1, summary.WithoutSales);
    }

    [Fact]
    public async Task Delete_RemovesEntry()
    {
        var service = CreateService();
        var saved = await service.SaveAsync(Entry("DAKO-CZ"));

        await service.DeleteAsync(saved.Id);

        Assert.Empty(await service.SearchAsync(null, null, null, null));
    }

    [Fact]
    public async Task DistinctValues_MergeStoredAndDefaults()
    {
        var service = CreateService();
        await service.SaveAsync(Entry("Sondertyp", status: "In Klaerung"));

        var statuses = await service.GetDistinctValuesAsync("status");

        Assert.Contains("In Klaerung", statuses);
        Assert.Contains("Existing Customer", statuses);
        Assert.Contains("No Potential", statuses);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(options));
    }

    private sealed class NullAppEventLog : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }
}
