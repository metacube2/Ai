using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class FinancialJournalTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;

    public FinancialJournalTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.Database.EnsureCreated();
            db.SourceSystemDefinitions.Add(new SourceSystemDefinition
            {
                Code = "BI1",
                DisplayName = "SAP B1 HANA",
                ConnectionKind = SourceSystemConnectionKinds.Hana,
                IsActive = true
            });
            db.HanaServers.Add(new HanaServer
            {
                SourceSystem = "BI1",
                Name = "Test B1",
                Host = "localhost",
                Port = 30015
            });
            db.Sites.AddRange(
                new Site { Id = 1, Schema = "fr01_p", TSC = "TRFR", Land = "Frankreich", SourceSystem = "BI1", IsActive = true },
                new Site { Id = 2, Schema = "TRAFAG_LIVE", TSC = "TRIN", Land = "Indien", SourceSystem = "SAGE", IsActive = true },
                new Site { Id = 3, Schema = string.Empty, TSC = "TRUK", Land = "England", SourceSystem = "MANUAL_EXCEL", IsActive = true });
            db.ExportSettings.Add(new ExportSettings { DateFilter = "2025-01-01" });
            db.SaveChanges();
        }

        _dbFactory = new TestDbContextFactory(options);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void CreateEntry_Derives_Sign_FiscalPeriod_Manual_And_Reversal()
    {
        var entry = HanaFinancialJournalReader.CreateEntry(
            tsc: "TRFR", land: "Frankreich", companySchema: "fr01_p", sourceSystem: "BI1",
            extractionDate: new DateTime(2026, 7, 14, 8, 0, 0),
            journalEntryId: "12345", journalEntryLineId: 2,
            postingDate: new DateTime(2026, 3, 17),
            accountCode: "47005010", accountName: "Umsatzerloese",
            debit: 0m, credit: 918m, fcDebit: 0m, fcCredit: 0m,
            localCurrency: "EUR", transactionCurrency: "",
            costCenter: "CC-100", dimension2: "PC-1",
            lineMemo: "Rechnung 404110", transactionType: "13",
            sourceDocumentNumber: "404110", stornoToTrans: "0", autoStorno: "N");

        Assert.Equal(-918m, entry.SignedAmountLocal);
        Assert.Equal(2026, entry.FiscalYear);
        Assert.Equal(3, entry.FiscalPeriod);
        Assert.False(entry.IsManual);
        Assert.False(entry.IsReversal);
        Assert.Equal("12345", entry.JournalEntryId);
        Assert.Equal("EUR", entry.LocalCurrency);
    }

    [Fact]
    public void CreateEntry_Marks_Manual_And_Reversal_Entries()
    {
        var manual = HanaFinancialJournalReader.CreateEntry(
            "TRFR", "Frankreich", "fr01_p", "BI1", DateTime.UtcNow,
            "77", 0, new DateTime(2026, 1, 5), "10100", "Bank",
            500m, 0m, 550m, 0m, "EUR", "USD", "", "", "manuelle Umbuchung", "30", "", "", "N");

        Assert.True(manual.IsManual);
        Assert.Equal(500m, manual.SignedAmountLocal);
        Assert.Equal(550m, manual.SignedAmountTransaction);
        Assert.Equal("USD", manual.TransactionCurrency);

        var stornoByReference = HanaFinancialJournalReader.CreateEntry(
            "TRFR", "Frankreich", "fr01_p", "BI1", DateTime.UtcNow,
            "78", 0, new DateTime(2026, 1, 6), "10100", "Bank",
            0m, 500m, 0m, 0m, "EUR", "", "", "", "", "30", "", "77", "N");
        Assert.True(stornoByReference.IsReversal);

        var stornoByAutoFlag = HanaFinancialJournalReader.CreateEntry(
            "TRFR", "Frankreich", "fr01_p", "BI1", DateTime.UtcNow,
            "79", 0, new DateTime(2026, 1, 7), "10100", "Bank",
            0m, 500m, 0m, 0m, "EUR", "", "", "", "", "13", "", "0", "Y");
        Assert.True(stornoByAutoFlag.IsReversal);
    }

    [Fact]
    public void GetJournalQuery_Reads_Jdt1_Ojdt_Oact_With_Schema_And_DateFilter()
    {
        var query = HanaFinancialJournalReader.GetJournalQuery("fr01_p");

        Assert.Contains(@"fr01_p.""JDT1""", query);
        Assert.Contains(@"fr01_p.""OJDT""", query);
        Assert.Contains(@"fr01_p.""OACT""", query);
        Assert.Contains(@"fr01_p.""OADM""", query);
        Assert.Contains(@"""RefDate"" >= :dateFilter", query);
        Assert.DoesNotContain("47005", query); // kein IT-Umsatzkontenfilter im Hauptbuch
    }

    [Fact]
    public void GetJournalQuery_Rejects_Invalid_Schema()
        => Assert.Throws<InvalidOperationException>(() => HanaFinancialJournalReader.GetJournalQuery("fr01_p; DROP"));

    [Fact]
    public void IsB1JournalSite_Selects_Only_Active_B1_Hana_Sites()
    {
        var sourceSystems = new List<SourceSystemDefinition>
        {
            new() { Code = "BI1", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
            new() { Code = "SAGE", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
            new() { Code = "MANUAL_EXCEL", ConnectionKind = SourceSystemConnectionKinds.ManualExcel, IsActive = true }
        };

        Assert.True(FinancialJournalRefreshService.IsB1JournalSite(
            new Site { SourceSystem = "BI1", Schema = "fr01_p", IsActive = true }, sourceSystems));
        Assert.False(FinancialJournalRefreshService.IsB1JournalSite(
            new Site { SourceSystem = "SAGE", Schema = "TRAFAG_LIVE", IsActive = true }, sourceSystems));
        Assert.False(FinancialJournalRefreshService.IsB1JournalSite(
            new Site { SourceSystem = "MANUAL_EXCEL", Schema = "x", IsActive = true }, sourceSystems));
        Assert.False(FinancialJournalRefreshService.IsB1JournalSite(
            new Site { SourceSystem = "BI1", Schema = "fr01_p", IsActive = false }, sourceSystems));
        Assert.False(FinancialJournalRefreshService.IsB1JournalSite(
            new Site { SourceSystem = "BI1", Schema = "", IsActive = true }, sourceSystems));
    }

    [Fact]
    public async Task GetSiteStatusAsync_Lists_Only_B1_Sites_With_Row_Stats()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.FinancialJournalEntries.AddRange(
                CreateStoredEntry("TRFR", "100", 0, new DateTime(2026, 1, 10)),
                CreateStoredEntry("TRFR", "100", 1, new DateTime(2026, 2, 11)));
            await db.SaveChangesAsync();
        }

        var service = new FinancialJournalRefreshService(_dbFactory, new FakeJournalReader([]), new NoopAppEventLogService());
        var status = await service.GetSiteStatusAsync();

        var fr = Assert.Single(status);
        Assert.Equal("TRFR", fr.Tsc);
        Assert.Equal(2, fr.RowCount);
        Assert.Equal(new DateTime(2026, 1, 10), fr.MinPostingDate);
        Assert.Equal(new DateTime(2026, 2, 11), fr.MaxPostingDate);
    }

    [Fact]
    public async Task RefreshSiteAsync_Replaces_Existing_Rows_For_Site()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.FinancialJournalEntries.Add(CreateStoredEntry("TRFR", "1", 0, new DateTime(2025, 5, 1)));
            await db.SaveChangesAsync();
        }

        var freshEntries = new List<FinancialJournalEntry>
        {
            CreateStoredEntry("TRFR", "200", 0, new DateTime(2026, 6, 1)),
            CreateStoredEntry("TRFR", "200", 1, new DateTime(2026, 6, 1))
        };
        var service = new FinancialJournalRefreshService(_dbFactory, new FakeJournalReader(freshEntries), new NoopAppEventLogService());

        var result = await service.RefreshSiteAsync(1);

        Assert.Equal(1, result.DeletedRows);
        Assert.Equal(2, result.InsertedRows);
        await using var verify = await _dbFactory.CreateDbContextAsync();
        var rows = await verify.FinancialJournalEntries.Where(e => e.Tsc == "TRFR").ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal("200", row.JournalEntryId));
    }

    [Fact]
    public async Task RefreshSiteAsync_Keeps_Existing_Rows_When_Source_Returns_Empty()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.FinancialJournalEntries.Add(CreateStoredEntry("TRFR", "1", 0, new DateTime(2025, 5, 1)));
            await db.SaveChangesAsync();
        }

        var service = new FinancialJournalRefreshService(_dbFactory, new FakeJournalReader([]), new NoopAppEventLogService());
        var result = await service.RefreshSiteAsync(1);

        Assert.Equal(0, result.DeletedRows);
        Assert.Equal(0, result.InsertedRows);
        await using var verify = await _dbFactory.CreateDbContextAsync();
        Assert.Equal(1, await verify.FinancialJournalEntries.CountAsync(e => e.Tsc == "TRFR"));
    }

    [Fact]
    public async Task RefreshSiteAsync_Rejects_Non_B1_Sites()
    {
        var service = new FinancialJournalRefreshService(_dbFactory, new FakeJournalReader([]), new NoopAppEventLogService());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshSiteAsync(2));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshSiteAsync(3));
    }

    private static FinancialJournalEntry CreateStoredEntry(string tsc, string journalEntryId, int lineId, DateTime postingDate)
        => new()
        {
            StoredAtUtc = DateTime.UtcNow,
            ExtractionDate = DateTime.UtcNow,
            Tsc = tsc,
            Land = "Frankreich",
            CompanySchema = "fr01_p",
            SourceSystem = "BI1",
            JournalEntryId = journalEntryId,
            JournalEntryLineId = lineId,
            PostingDate = postingDate,
            FiscalYear = postingDate.Year,
            FiscalPeriod = postingDate.Month,
            AccountCode = "47005010",
            AccountName = "Umsatzerloese",
            DebitAmount = 0m,
            CreditAmount = 100m,
            SignedAmountLocal = -100m,
            LocalCurrency = "EUR"
        };

    private sealed class FakeJournalReader(List<FinancialJournalEntry> entries) : IFinancialJournalReader
    {
        public Task<List<FinancialJournalEntry>> GetJournalEntriesAsync(
            HanaServer server, string schema, string tsc, string land, string sourceSystem, string dateFilter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(entries);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }

    private sealed class NoopAppEventLogService : IAppEventLogService
    {
        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }
}
