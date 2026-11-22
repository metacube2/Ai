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
            db.SourceSystemDefinitions.AddRange(
                new SourceSystemDefinition { Code = "BI1", DisplayName = "SAP B1 HANA", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
                // Indien ist fachlich SAP B1, in der Konfiguration aber historisch als SAGE angeschrieben.
                new SourceSystemDefinition { Code = "SAGE", DisplayName = "SAGE", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
                new SourceSystemDefinition { Code = "SAP", DisplayName = "SAP OData", ConnectionKind = SourceSystemConnectionKinds.SapGateway, IsActive = true, CentralServiceUrl = "http://travp762:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/", CentralUsername = "user", CentralPassword = "pass" },
                new SourceSystemDefinition { Code = "MANUAL_EXCEL", DisplayName = "Manual Excel", ConnectionKind = SourceSystemConnectionKinds.ManualExcel, IsActive = true });
            db.HanaServers.AddRange(
                new HanaServer { SourceSystem = "BI1", Name = "Test B1", Host = "localhost", Port = 30015 },
                new HanaServer { SourceSystem = "SAGE", Name = "India", Host = "20.197.20.60", Port = 30015 });
            db.Sites.AddRange(
                new Site { Id = 1, Schema = "fr01_p", TSC = "TRFR", Land = "Frankreich", SourceSystem = "BI1", IsActive = true },
                new Site { Id = 2, Schema = "TRAFAG_LIVE", TSC = "TRIN", Land = "Indien", SourceSystem = "SAGE", IsActive = true },
                new Site { Id = 3, Schema = string.Empty, TSC = "TRUK", Land = "England", SourceSystem = "MANUAL_EXCEL", IsActive = true },
                new Site { Id = 4, Schema = string.Empty, TSC = "ZSCHWEIZ", Land = "Schweiz/Oesterreich", SourceSystem = "SAP", IsActive = true });
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
    public void IsJournalSite_Selects_Hana_Sites_And_Gateway_Sites_With_Url()
    {
        var sourceSystems = new List<SourceSystemDefinition>
        {
            new() { Code = "BI1", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
            new() { Code = "SAGE", ConnectionKind = SourceSystemConnectionKinds.Hana, IsActive = true },
            new() { Code = "SAP", ConnectionKind = SourceSystemConnectionKinds.SapGateway, IsActive = true, CentralServiceUrl = "http://travp762:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/" },
            new() { Code = "SAP_NO_URL", ConnectionKind = SourceSystemConnectionKinds.SapGateway, IsActive = true },
            new() { Code = "MANUAL_EXCEL", ConnectionKind = SourceSystemConnectionKinds.ManualExcel, IsActive = true }
        };

        Assert.True(FinancialJournalRefreshService.IsJournalSite(
            new Site { SourceSystem = "BI1", Schema = "fr01_p", IsActive = true }, sourceSystems));
        // Indien ist fachlich B1, laeuft aber unter dem irrefuehrenden Code SAGE.
        Assert.True(FinancialJournalRefreshService.IsJournalSite(
            new Site { SourceSystem = "SAGE", Schema = "TRAFAG_LIVE", IsActive = true }, sourceSystems));
        // CH/AT: SAP-Gateway-Standort mit aufloesbarer Service-URL ist Journalquelle (BKPF/BSEG via EntitySet).
        Assert.True(FinancialJournalRefreshService.IsJournalSite(
            new Site { SourceSystem = "SAP", Schema = "", IsActive = true }, sourceSystems));
        // Gateway ohne zentrale URL und ohne Site-Override bleibt draussen.
        Assert.False(FinancialJournalRefreshService.IsJournalSite(
            new Site { SourceSystem = "SAP_NO_URL", Schema = "", IsActive = true }, sourceSystems));
        // Site-Override der URL reicht auch ohne zentrale URL.
        Assert.True(FinancialJournalRefreshService.IsJournalSite(
            new Site { SourceSystem = "SAP_NO_URL", SapServiceUrl = "http://host/sap/opu/odata/sap/ZX_SRV/", Schema = "", IsActive = true }, sourceSystems));
        Assert.False(FinancialJournalRefreshService.IsJournalSite(
            new Site { SourceSystem = "MANUAL_EXCEL", Schema = "x", IsActive = true }, sourceSystems));
        Assert.False(FinancialJournalRefreshService.IsJournalSite(
            new Site { SourceSystem = "BI1", Schema = "fr01_p", IsActive = false }, sourceSystems));
        Assert.False(FinancialJournalRefreshService.IsJournalSite(
            new Site { SourceSystem = "BI1", Schema = "", IsActive = true }, sourceSystems));
    }

    [Fact]
    public void MapRow_Maps_Bseg_Fields_With_Sign_And_Composite_Key()
    {
        var debitRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bukrs"] = "0001",
            ["Belnr"] = "0100000042",
            ["Gjahr"] = "2026",
            ["Buzei"] = "001",
            ["Budat"] = "/Date(1767139200000)/", // 31.12.2025 UTC
            ["Monat"] = "12",
            ["Hkont"] = "0000470050",
            ["HkontTxt"] = "Umsatzerloese Inland",
            ["Shkzg"] = "S",
            ["Dmbtr"] = "1250.50",
            ["Wrbtr"] = "1300.00",
            ["Hwaer"] = "CHF",
            ["Waers"] = "EUR",
            ["Kostl"] = "0000001200",
            ["Prctr"] = "0000009100",
            ["Sgtxt"] = "Rechnung 404110",
            ["Blart"] = "RV",
            ["Xblnr"] = "404110",
            ["Stblg"] = ""
        };

        var entry = SapGatewayFinancialJournalReader.MapRow(debitRow, "ZSCHWEIZ", "Schweiz/Oesterreich", "SAP");

        Assert.Equal("0001/2026/0100000042", entry.JournalEntryId);
        Assert.Equal(1, entry.JournalEntryLineId);
        Assert.Equal("0001", entry.CompanyCode);
        Assert.Equal(2026, entry.FiscalYear);
        Assert.Equal(12, entry.FiscalPeriod);
        Assert.Equal("470050", entry.AccountCode);
        Assert.Equal(1250.50m, entry.DebitAmount);
        Assert.Equal(0m, entry.CreditAmount);
        Assert.Equal(1250.50m, entry.SignedAmountLocal);
        Assert.Equal("CHF", entry.LocalCurrency);
        Assert.Equal("EUR", entry.TransactionCurrency);
        Assert.Equal(1300.00m, entry.SignedAmountTransaction);
        Assert.Equal("1200", entry.CostCenter);
        Assert.Equal("9100", entry.Dimension2);
        Assert.Equal(new DateTime(2025, 12, 31), entry.PostingDate);
        Assert.False(entry.IsManual);
        Assert.False(entry.IsReversal);
    }

    [Fact]
    public void MapRow_Credit_Manual_And_Reversal_Flags()
    {
        var creditRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bukrs"] = "0002",
            ["Belnr"] = "0100000001",
            ["Gjahr"] = "2026",
            ["Buzei"] = "002",
            ["Budat"] = "2026-01-15T00:00:00",
            ["Monat"] = "1",
            ["Hkont"] = "0000010100",
            ["Shkzg"] = "H",
            ["Dmbtr"] = "500",
            ["Wrbtr"] = "500",
            ["Hwaer"] = "EUR",
            ["Waers"] = "EUR",
            ["Blart"] = "SA",
            ["Stblg"] = "0100000099"
        };

        var entry = SapGatewayFinancialJournalReader.MapRow(creditRow, "ZSCHWEIZ", "Schweiz/Oesterreich", "SAP");

        Assert.Equal(0m, entry.DebitAmount);
        Assert.Equal(500m, entry.CreditAmount);
        Assert.Equal(-500m, entry.SignedAmountLocal);
        Assert.Equal(-500m, entry.SignedAmountTransaction);
        // Belegwaehrung = Hauswaehrung -> keine Transaktionswaehrung ausweisen.
        Assert.Equal(string.Empty, entry.TransactionCurrency);
        Assert.True(entry.IsManual);   // Belegart SA
        Assert.True(entry.IsReversal); // Storno-Belegnummer gesetzt
        Assert.Equal(new DateTime(2026, 1, 15), entry.PostingDate);
    }

    [Fact]
    public async Task GetSiteStatusAsync_Lists_Hana_And_Gateway_Sites_With_Row_Stats()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.FinancialJournalEntries.AddRange(
                CreateStoredEntry("TRFR", "100", 0, new DateTime(2026, 1, 10)),
                CreateStoredEntry("TRFR", "100", 1, new DateTime(2026, 2, 11)));
            await db.SaveChangesAsync();
        }

        var service = CreateService(new FakeJournalReader([]));
        var status = await service.GetSiteStatusAsync();

        Assert.Equal(3, status.Count);
        var fr = Assert.Single(status, row => row.Tsc == "TRFR");
        Assert.Equal(2, fr.RowCount);
        Assert.Equal(new DateTime(2026, 1, 10), fr.MinPostingDate);
        Assert.Equal(new DateTime(2026, 2, 11), fr.MaxPostingDate);

        var india = Assert.Single(status, row => row.Tsc == "TRIN");
        Assert.Equal("TRAFAG_LIVE", india.Schema);
        Assert.Equal(0, india.RowCount);
        Assert.Null(india.LastLoadedAtUtc);

        // CH/AT erscheint als Gateway-Journalquelle (Service-URL zentral konfiguriert).
        var chat = Assert.Single(status, row => row.Tsc == "ZSCHWEIZ");
        Assert.Equal(0, chat.RowCount);
    }

    [Fact]
    public async Task RefreshSiteAsync_Loads_Zschweiz_Via_Gateway_Reader()
    {
        var gatewayEntries = new List<FinancialJournalEntry>
        {
            new()
            {
                StoredAtUtc = DateTime.UtcNow,
                ExtractionDate = DateTime.UtcNow,
                Tsc = "ZSCHWEIZ",
                Land = "Schweiz/Oesterreich",
                CompanyCode = "0001",
                SourceSystem = "SAP",
                JournalEntryId = "0001/2026/0100000042",
                JournalEntryLineId = 1,
                PostingDate = new DateTime(2026, 3, 5),
                FiscalYear = 2026,
                FiscalPeriod = 3,
                AccountCode = "470050",
                DebitAmount = 100m,
                SignedAmountLocal = 100m,
                LocalCurrency = "CHF"
            }
        };
        var gatewayReader = new FakeGatewayJournalReader(gatewayEntries);
        var service = CreateService(new FakeJournalReader([]), gatewayReader);

        var result = await service.RefreshSiteAsync(4);

        Assert.Equal("ZSCHWEIZ", result.Tsc);
        Assert.Equal(1, result.InsertedRows);
        Assert.Equal("http://travp762:8000/sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/", gatewayReader.LastServiceUrl);
        await using var verify = await _dbFactory.CreateDbContextAsync();
        var row = Assert.Single(await verify.FinancialJournalEntries.Where(e => e.Tsc == "ZSCHWEIZ").ToListAsync());
        Assert.Equal("0001", row.CompanyCode);
    }

    [Fact]
    public async Task RefreshSiteAsync_Loads_India_As_B1_Journal_Site()
    {
        var indiaEntries = new List<FinancialJournalEntry>
        {
            CreateStoredEntry("TRIN", "900", 0, new DateTime(2026, 4, 2))
        };
        var service = CreateService(new FakeJournalReader(indiaEntries));

        var result = await service.RefreshSiteAsync(2);

        Assert.Equal("TRIN", result.Tsc);
        Assert.Equal(1, result.InsertedRows);
        await using var verify = await _dbFactory.CreateDbContextAsync();
        Assert.Equal(1, await verify.FinancialJournalEntries.CountAsync(e => e.Tsc == "TRIN"));
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
        var service = CreateService(new FakeJournalReader(freshEntries));

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

        var service = CreateService(new FakeJournalReader([]));
        var result = await service.RefreshSiteAsync(1);

        Assert.Equal(0, result.DeletedRows);
        Assert.Equal(0, result.InsertedRows);
        await using var verify = await _dbFactory.CreateDbContextAsync();
        Assert.Equal(1, await verify.FinancialJournalEntries.CountAsync(e => e.Tsc == "TRFR"));
    }

    [Fact]
    public async Task RefreshSiteAsync_Rejects_Non_Journal_Sites()
    {
        var service = CreateService(new FakeJournalReader([]));
        // Manual-Excel (UK) hat keine Buchhaltungsquelle.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RefreshSiteAsync(3));
    }

    private static FinancialJournalEntry CreateStoredEntry(string tsc, string journalEntryId, int lineId, DateTime postingDate)
        => new()
        {
            StoredAtUtc = DateTime.UtcNow,
            ExtractionDate = DateTime.UtcNow,
            Tsc = tsc,
            Land = tsc == "TRIN" ? "Indien" : "Frankreich",
            CompanySchema = tsc == "TRIN" ? "TRAFAG_LIVE" : "fr01_p",
            SourceSystem = tsc == "TRIN" ? "SAGE" : "BI1",
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

    private FinancialJournalRefreshService CreateService(
        IFinancialJournalReader hanaReader,
        ISapGatewayFinancialJournalReader? gatewayReader = null)
        => new(_dbFactory, hanaReader, gatewayReader ?? new FakeGatewayJournalReader([]), new NoopAppEventLogService());

    private sealed class FakeJournalReader(List<FinancialJournalEntry> entries) : IFinancialJournalReader
    {
        public Task<List<FinancialJournalEntry>> GetJournalEntriesAsync(
            HanaServer server, string schema, string tsc, string land, string sourceSystem, string dateFilter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(entries);
    }

    private sealed class FakeGatewayJournalReader(List<FinancialJournalEntry> entries) : ISapGatewayFinancialJournalReader
    {
        public string? LastServiceUrl { get; private set; }

        public Task<List<FinancialJournalEntry>> GetJournalEntriesAsync(
            string serviceUrl, string username, string password, string tsc, string land, string sourceSystem,
            string dateFilter, CancellationToken cancellationToken = default)
        {
            LastServiceUrl = serviceUrl;
            return Task.FromResult(entries);
        }
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
