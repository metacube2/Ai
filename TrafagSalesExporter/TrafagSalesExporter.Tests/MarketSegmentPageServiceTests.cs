using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Pflege der Kunden-Segment-Zuordnung ueber die Weboberflaeche.
/// </summary>
public class MarketSegmentPageServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MarketSegmentPageServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
        // CentralSalesRecords haengt per Fremdschluessel an Sites.
        db.Sites.Add(new Site
        {
            Id = 1,
            HanaServerId = null,
            Schema = "test",
            TSC = "TEST",
            Land = "Testland",
            SourceSystem = "SAP",
            IsActive = true
        });
        db.SaveChanges();
        db.CentralSalesRecords.AddRange(
            Row("TRCH", "10042", "Stadler Rail AG", "CH", 100m, "Pressostat"),
            Row("TRCH", "10042", "Stadler Rail AG", "CH", 150m, "Pressostat"),
            Row("TRCH", "20001", "Andere AG", "CH", 50m, "Transmitter"),
            Row("TRIT", "10042", "Irgendwer SRL", "IT", 70m, "Transmitter"));
        db.SaveChanges();
    }

    private static CentralSalesRecord Row(
        string tsc, string customerNumber, string customerName, string country, decimal value, string family,
        DateTime? postingDate = null)
        => new()
        {
            SiteId = 1,
            Tsc = tsc,
            Land = country,
            CustomerNumber = customerNumber,
            CustomerName = customerName,
            CustomerCountry = country,
            SalesPriceValue = value,
            SalesCurrency = "CHF",
            ProductFamilyText = family,
            PostingDate = postingDate,
            ExtractionDate = new DateTime(2026, 8, 13),
            DocumentType = "Invoice"
        };

    private MarketSegmentPageService CreateService()
        => new(new TestDbContextFactory(_options), new NullAppEventLog());

    [Fact]
    public async Task SearchCustomers_AggregatesByCustomerAndSortsByRows()
    {
        var service = CreateService();

        var rows = await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.All);

        Assert.Equal(3, rows.Count);
        var top = rows[0];
        Assert.Equal("10042", top.CustomerNumber);
        Assert.Equal("TRCH", top.Tsc);
        Assert.Equal(2, top.SalesRows);
        Assert.Equal(250m, top.SalesValue);
        Assert.Equal(string.Empty, top.Segment);
    }

    [Fact]
    public async Task Assign_ThenSearch_ShowsSegmentForThatSiteOnly()
    {
        var service = CreateService();

        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Umfrage 2026-05");

        var rows = await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.All);
        var swiss = rows.Single(r => r.Tsc == "TRCH" && r.CustomerNumber == "10042");
        var italian = rows.Single(r => r.Tsc == "TRIT" && r.CustomerNumber == "10042");

        Assert.Equal("Railway", swiss.Segment);
        Assert.Equal("Umfrage 2026-05", swiss.Source);
        // Gleiche Kundennummer, anderer Standort: bleibt unzugeordnet.
        Assert.Equal(string.Empty, italian.Segment);
    }

    [Fact]
    public async Task Assign_Twice_UpdatesInsteadOfDuplicating()
    {
        var service = CreateService();

        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "erste Quelle");
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Industrial", "zweite Quelle");

        await using var db = new AppDbContext(_options);
        var stored = db.CustomerMarketSegments.Where(x => x.CustomerNumber == "10042" && x.Tsc == "TRCH").ToList();
        Assert.Single(stored);
        Assert.Equal("Industrial", stored[0].Segment);
        Assert.Equal("zweite Quelle", stored[0].Source);
    }

    [Fact]
    public async Task Assign_WithoutSegment_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "  ", "Quelle"));
    }

    [Fact]
    public async Task Clear_RemovesAssignment()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Quelle");

        await service.ClearAsync("TRCH", "10042");

        var rows = await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.All);
        Assert.All(rows, r => Assert.Equal(string.Empty, r.Segment));
    }

    [Fact]
    public async Task OnlyAssigned_FiltersToMaintainedCustomers()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Quelle");

        var rows = await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.Confirmed);

        Assert.Single(rows);
        Assert.Equal("Railway", rows[0].Segment);
    }

    [Fact]
    public async Task Search_FiltersByNameAndSite()
    {
        var service = CreateService();

        var byName = await service.SearchCustomersAsync("Stadler", null, MarketSegmentFilterModes.All);
        Assert.Single(byName);
        Assert.Equal("Stadler Rail AG", byName[0].CustomerName);

        var bySite = await service.SearchCustomersAsync(null, "TRIT", MarketSegmentFilterModes.All);
        Assert.Single(bySite);
        Assert.Equal("TRIT", bySite[0].Tsc);
    }

    [Fact]
    public async Task Result_GroupsBySegmentSiteAndCurrency()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Quelle");
        await service.AssignAsync("TRIT", "10042", "Irgendwer SRL", "Railway", "Quelle");

        var result = await service.GetResultAsync();

        // Zwei Standorte, deshalb zwei Zeilen: Waehrungen werden nicht ueber Laender addiert.
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal("Railway", r.Segment));
        var swiss = result.Single(r => r.Tsc == "TRCH");
        Assert.Equal(2, swiss.SalesRows);
        Assert.Equal(250m, swiss.SalesValue);
        Assert.Equal("CHF", swiss.Currency);
        Assert.Equal(1, swiss.Customers);
        // Ohne Buchungs- und Rechnungsdatum zaehlt das Extraktionsdatum, genau wie im
        // zentralen Excel.
        Assert.Equal(2026, swiss.Year);
    }

    [Fact]
    public async Task Result_SplitsSameCustomerIntoOneRowPerYear()
    {
        await using (var db = new AppDbContext(_options))
        {
            db.CentralSalesRecords.Add(
                Row("TRCH", "10042", "Stadler Rail AG", "CH", 900m, "Pressostat", new DateTime(2025, 6, 1)));
            await db.SaveChangesAsync();
        }

        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Quelle");

        var result = await service.GetResultAsync();

        var y2026 = result.Single(r => r.Tsc == "TRCH" && r.Year == 2026);
        var y2025 = result.Single(r => r.Tsc == "TRCH" && r.Year == 2025);
        Assert.Equal(250m, y2026.SalesValue);
        Assert.Equal(900m, y2025.SalesValue);
        // Jahre werden nicht addiert; derselbe Kunde erscheint in jedem Jahr einmal.
        Assert.Equal(1, y2025.Customers);
    }

    [Fact]
    public async Task YearFilter_NarrowsResultSearchAndProgress()
    {
        await using (var db = new AppDbContext(_options))
        {
            db.CentralSalesRecords.Add(
                Row("TRCH", "10042", "Stadler Rail AG", "CH", 900m, "Pressostat", new DateTime(2025, 6, 1)));
            await db.SaveChangesAsync();
        }

        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Quelle");

        var result = Assert.Single(await service.GetResultAsync(confirmedOnly: true, year: 2025));
        Assert.Equal(2025, result.Year);
        Assert.Equal(900m, result.SalesValue);

        var rows = Assert.Single(await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.All, 2025));
        Assert.Equal("10042", rows.CustomerNumber);
        Assert.Equal(1, rows.SalesRows);

        var progress = await service.GetProgressAsync(2025);
        Assert.Equal(1, progress.ConfirmedSalesRows);
        Assert.Equal(1, progress.TotalSalesRows);
    }

    [Fact]
    public async Task AvailableYears_AreDistinctAndNewestFirst()
    {
        await using (var db = new AppDbContext(_options))
        {
            db.CentralSalesRecords.Add(
                Row("TRCH", "10042", "Stadler Rail AG", "CH", 900m, "Pressostat", new DateTime(2025, 6, 1)));
            await db.SaveChangesAsync();
        }

        var years = await CreateService().GetAvailableYearsAsync();

        Assert.Equal([2026, 2025], years);
    }

    [Fact]
    public async Task Result_ExcludesUnconfirmedProposals()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Vorschlag",
            isConfirmed: false);

        // Der Vorschlag darf im Ergebnis NICHT auftauchen, sonst waere im Export nicht
        // unterscheidbar, was geprueft ist und was der Namensabgleich geraten hat.
        Assert.Empty(await service.GetResultAsync());
        Assert.Single(await service.GetResultAsync(confirmedOnly: false));
    }

    [Fact]
    public async Task Proposals_AppearOnlyInProposalFilter_UntilConfirmed()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Vorschlag",
            isConfirmed: false);

        Assert.Single(await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.Proposals));
        Assert.Empty(await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.Confirmed));

        await service.ConfirmAsync("TRCH", "10042");

        Assert.Empty(await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.Proposals));
        var confirmed = Assert.Single(await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.Confirmed));
        Assert.True(confirmed.IsConfirmed);
        // Bestaetigen darf Segment und Quelle nicht veraendern.
        Assert.Equal("Railway", confirmed.Segment);
        Assert.Equal("Vorschlag", confirmed.Source);
    }

    [Fact]
    public async Task Confirm_WithoutProposal_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmAsync("TRCH", "99999"));
    }

    [Fact]
    public async Task Progress_SeparatesConfirmedFromProposals()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Railway", "Quelle");
        await service.AssignAsync("TRIT", "10042", "Irgendwer SRL", "Railway", "Vorschlag",
            isConfirmed: false);

        var progress = await service.GetProgressAsync();

        Assert.Equal(1, progress.ConfirmedCustomers);
        Assert.Equal(1, progress.ProposalCustomers);
        Assert.Equal(2, progress.ConfirmedSalesRows);
        Assert.Equal(1, progress.ProposalSalesRows);
        Assert.Equal(4, progress.TotalSalesRows);
    }

    [Fact]
    public async Task AssignedFilter_FindsSmallCustomerOutsideTheTopRows()
    {
        // Regression: frueher wurden erst die obersten Kunden nach Zeilenzahl geholt und
        // danach gefiltert. Ein zugeordneter kleiner Kunde fiel dadurch still aus der Liste,
        // was wie ein leerer Schalter aussah.
        var service = CreateService();
        await service.AssignAsync("TRIT", "10042", "Irgendwer SRL", "Railway", "Quelle");

        var rows = await service.SearchCustomersAsync(null, null, MarketSegmentFilterModes.Confirmed);

        var hit = Assert.Single(rows);
        Assert.Equal("TRIT", hit.Tsc);
        Assert.Equal(1, hit.SalesRows);
    }

    [Fact]
    public async Task KnownSegments_ContainDefaultsAndUsedValues()
    {
        var service = CreateService();
        await service.AssignAsync("TRCH", "10042", "Stadler Rail AG", "Sondermaschinen", "Quelle");

        var segments = await service.GetKnownSegmentsAsync();

        Assert.Contains("Railway", segments);
        Assert.Contains("Sondermaschinen", segments);
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
