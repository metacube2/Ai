using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ManagementCockpitServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;
    private readonly ManagementCockpitService _service;

    public ManagementCockpitServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var db = new AppDbContext(options))
        {
            db.Database.EnsureCreated();
            if (!db.Sites.Any())
            {
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
            }
        }

        _dbFactory = new TestDbContextFactory(options);
        _service = new ManagementCockpitService(_dbFactory);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task GetAvailableCentralYearsAsync_Returns_Distinct_Ordered_Years()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "CH", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)),
            CreateRow("SAP", "CH", "TRCH", "INV-2", "CHF", 200m, new DateTime(2026, 2, 10)),
            CreateRow("SAP", "CH", "TRCH", "INV-3", "CHF", 300m, null, new DateTime(2026, 3, 5)));

        var years = await _service.GetAvailableCentralYearsAsync();

        Assert.Equal([2025, 2026], years);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Uses_InvoiceDate_Or_ExtractionDate_And_Builds_Monthly_Daily_And_Source_Totals()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)),
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-2", "EUR", 50m, new DateTime(2025, 1, 11)),
            CreateRow("SAP", "Deutschland", "TRDE", "INV-3", "EUR", 25m, null, new DateTime(2025, 1, 12)),
            CreateRow("SAP", "Schweiz", "TRCH", "INV-4", "CHF", 70m, new DateTime(2026, 2, 5)));

        var result = await _service.AnalyzeCentralAsync(2025, 1);

        Assert.Equal(2025, result.Filter.Year);
        Assert.Equal(1, result.Filter.Month);
        Assert.Equal(3, result.Summary.RowCount);
        Assert.Equal(3, result.Summary.InvoiceCount);
        Assert.Equal(2, result.Summary.SiteCount);
        Assert.Equal(2, result.Summary.CountryCount);
        Assert.Equal(2, result.Summary.CurrencyCount);
        Assert.Equal(new DateTime(2025, 1, 10), result.Summary.PeriodStart);
        Assert.Equal(new DateTime(2025, 1, 12), result.Summary.PeriodEnd);

        var yearly2025Chf = Assert.Single(result.YearlyTotals, x => x.Year == 2025 && x.Currency == "CHF");
        Assert.Equal(100m, yearly2025Chf.SalesValue);

        var yearly2025Eur = Assert.Single(result.YearlyTotals, x => x.Year == 2025 && x.Currency == "EUR");
        Assert.Equal(75m, yearly2025Eur.SalesValue);

        var januaryChf = Assert.Single(result.MonthlyTotals, x => x.Label == "2025-01" && x.Currency == "CHF");
        Assert.Equal(100m, januaryChf.SalesValue);

        var januaryEur = Assert.Single(result.MonthlyTotals, x => x.Label == "2025-01" && x.Currency == "EUR");
        Assert.Equal(75m, januaryEur.SalesValue);

        Assert.Equal(3, result.DailyTotals.Count);
        Assert.Contains(result.DailyTotals, x => x.Label == "2025-01-12" && x.Currency == "EUR" && x.SalesValue == 25m);

        var sapTotal = Assert.Single(result.SourceSystemTotals, x => x.Label == "SAP" && x.Currency == "CHF");
        Assert.Equal(100m, sapTotal.SalesValue);

        var manualTotal = Assert.Single(result.SourceSystemTotals, x => x.Label == "MANUAL_EXCEL" && x.Currency == "EUR");
        Assert.Equal(50m, manualTotal.SalesValue);

        var germanyEur = Assert.Single(result.CountryTotals, x => x.Label == "Deutschland" && x.Currency == "EUR");
        Assert.Equal(75m, germanyEur.SalesValue);
        Assert.Equal(2, germanyEur.InvoiceCount);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_With_Year_Only_Does_Not_Build_DailyTotals()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)),
            CreateRow("SAP", "Schweiz", "TRCH", "INV-2", "CHF", 150m, new DateTime(2025, 2, 10)));

        var result = await _service.AnalyzeCentralAsync(2025, null);

        Assert.Empty(result.DailyTotals);
        Assert.Equal(2, result.MonthlyTotals.Count);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Can_Convert_Selected_Value_To_Eur()
    {
        await SeedRatesAsync(
            CreateRate("EUR", "CHF", 2m),
            CreateRate("EUR", "USD", 1.25m));
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)),
            CreateRow("SAP", "USA", "TRUS", "INV-2", "USD", 100m, new DateTime(2025, 1, 11)),
            CreateRow("SAP", "Deutschland", "TRDE", "INV-3", "EUR", 100m, new DateTime(2025, 1, 12)));

        var result = await _service.AnalyzeCentralAsync(2025, null, new ManagementCockpitAnalysisOptions
        {
            ValueField = ManagementCockpitValueFieldKeys.SalesPriceValue,
            TargetCurrency = ManagementCockpitCurrencyOptions.Eur
        });

        Assert.Equal("EUR", result.Summary.DisplayCurrency);
        Assert.Equal(230m, result.Summary.ValueTotal);
        Assert.Equal(0, result.Summary.MissingExchangeRateCount);

        Assert.All(result.CountryTotals, row => Assert.Equal("EUR", row.Currency));
        Assert.Equal(50m, Assert.Single(result.CountryTotals, x => x.Label == "Schweiz").SalesValue);
        Assert.Equal(80m, Assert.Single(result.CountryTotals, x => x.Label == "USA").SalesValue);
        Assert.Equal(100m, Assert.Single(result.CountryTotals, x => x.Label == "Deutschland").SalesValue);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Caches_Exchange_Rates_Per_Currency_Target_And_Date()
    {
        var exchangeRates = new CountingCurrencyExchangeRateService();
        var service = new ManagementCockpitService(_dbFactory, exchangeRates);

        await SeedCentralRowsAsync(
            CreateRow("SAP", "USA", "TRUS", "INV-1", "USD", 100m, new DateTime(2025, 1, 10), quantity: 2m, standardCost: 10m),
            CreateRow("SAP", "USA", "TRUS", "INV-2", "USD", 50m, new DateTime(2025, 1, 10), quantity: 3m, standardCost: 20m));

        var result = await service.AnalyzeCentralAsync(2025, 1, new ManagementCockpitAnalysisOptions
        {
            ValueField = ManagementCockpitValueFieldKeys.SalesPriceValue,
            AdditionalValueFields = [ManagementCockpitValueFieldKeys.StandardCostTotal],
            TargetCurrency = ManagementCockpitCurrencyOptions.Eur
        });

        Assert.Equal(300m, result.Summary.ValueTotal);
        Assert.Equal(160m, Assert.Single(result.MonthlyTotals).AdditionalValues[ManagementCockpitValueFieldKeys.StandardCostTotal].Value);
        Assert.Equal(1, exchangeRates.ResolveRateCallCount);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Uses_Configured_Exchange_Rate_Date_Field()
    {
        var exchangeRates = new CountingCurrencyExchangeRateService();
        var service = new ManagementCockpitService(_dbFactory, exchangeRates);

        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.ExportSettings.Add(new ExportSettings
            {
                DateFilter = "2025-01-01",
                ExchangeRateDateField = ExchangeRateDateFields.InvoiceDate
            });
            await db.SaveChangesAsync();
        }

        await SeedCentralRowsAsync(
            CreateRow(
                "SAP",
                "USA",
                "TRUS",
                "INV-1",
                "USD",
                100m,
                new DateTime(2025, 2, 10),
                postingDate: new DateTime(2025, 1, 10)));

        var result = await service.AnalyzeCentralAsync(2025, 2, new ManagementCockpitAnalysisOptions
        {
            ValueField = ManagementCockpitValueFieldKeys.SalesPriceValue,
            TargetCurrency = ManagementCockpitCurrencyOptions.Eur
        });

        Assert.Equal(ExchangeRateDateFields.InvoiceDate, result.Summary.ExchangeRateDateField);
        Assert.Equal(new DateTime(2025, 2, 10), Assert.Single(exchangeRates.EffectiveDates));
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Can_Sum_Quantity_Without_Currency_Conversion()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10), quantity: 2m),
            CreateRow("SAP", "USA", "TRUS", "INV-2", "USD", 100m, new DateTime(2025, 1, 11), quantity: 3m));

        var result = await _service.AnalyzeCentralAsync(2025, null, new ManagementCockpitAnalysisOptions
        {
            ValueField = ManagementCockpitValueFieldKeys.Quantity,
            TargetCurrency = ManagementCockpitCurrencyOptions.Eur
        });

        Assert.Equal(ManagementCockpitValueFieldKeys.Quantity, result.Summary.ValueFieldKey);
        Assert.Equal("-", result.Summary.DisplayCurrency);
        Assert.Equal(5m, result.Summary.ValueTotal);
        Assert.Equal(0, result.Summary.MissingExchangeRateCount);
        Assert.Equal(2m, Assert.Single(result.CountryTotals, x => x.Label == "Schweiz").SalesValue);
        Assert.Equal(3m, Assert.Single(result.CountryTotals, x => x.Label == "USA").SalesValue);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Adds_Selected_Additional_Value_Fields_To_Time_Rows()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Deutschland", "TRDE", "INV-1", "EUR", 100m, new DateTime(2025, 1, 10), quantity: 2m, standardCost: 5m),
            CreateRow("SAP", "Deutschland", "TRDE", "INV-2", "EUR", 50m, new DateTime(2025, 2, 10), quantity: 3m, standardCost: 7m));

        var result = await _service.AnalyzeCentralAsync(2025, null, new ManagementCockpitAnalysisOptions
        {
            ValueField = ManagementCockpitValueFieldKeys.SalesPriceValue,
            AdditionalValueFields =
            [
                ManagementCockpitValueFieldKeys.Quantity,
                ManagementCockpitValueFieldKeys.StandardCostTotal
            ],
            TargetCurrency = ManagementCockpitCurrencyOptions.Eur
        });

        Assert.Equal(2, result.AdditionalValueFields.Count);

        var yearly = Assert.Single(result.YearlyTotals);
        Assert.Equal(150m, yearly.SalesValue);
        Assert.Equal(5m, yearly.AdditionalValues[ManagementCockpitValueFieldKeys.Quantity].Value);
        Assert.Equal("-", yearly.AdditionalValues[ManagementCockpitValueFieldKeys.Quantity].Currency);
        Assert.Equal(31m, yearly.AdditionalValues[ManagementCockpitValueFieldKeys.StandardCostTotal].Value);
        Assert.Equal("EUR", yearly.AdditionalValues[ManagementCockpitValueFieldKeys.StandardCostTotal].Currency);

        Assert.Contains(result.MonthlyTotals, row =>
            row.Label == "2025-01" &&
            row.AdditionalValues[ManagementCockpitValueFieldKeys.Quantity].Value == 2m);
        Assert.Contains(result.MonthlyTotals, row =>
            row.Label == "2025-02" &&
            row.AdditionalValues[ManagementCockpitValueFieldKeys.StandardCostTotal].Value == 21m);
    }

    [Fact]
    public async Task AnalyzeCentralAsync_Throws_When_No_Rows_Exist_For_Selected_Period()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 1, 10)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AnalyzeCentralAsync(2026, 1));

        Assert.Contains("gewählten Zeitraum", ex.Message);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_Returns_Empty_Result_For_Filter_With_No_Rows()
    {
        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-1", "EUR", 100m, new DateTime(2025, 1, 10)));

        var result = await _service.AnalyzeFinanceSummaryAsync(2026, "DE", null);

        Assert.Equal(2026, result.Filter.Year);
        Assert.Equal("DE", result.Filter.CountryKey);
        Assert.Empty(result.Rows);
        Assert.Equal(0m, result.NetSalesActual);
        Assert.Contains("keine Datensaetze", result.Notices[0]);
        Assert.Contains(2025, result.YearOptions);
        Assert.Contains("DE", result.CountryOptions);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_Builds_Dashboard_Tab_Data()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.Sites.Add(new Site
            {
                Id = 2,
                HanaServerId = null,
                Schema = "de",
                TSC = "TRDE",
                Land = "Deutschland",
                SourceSystem = "MANUAL_EXCEL",
                IsActive = true
            });
            db.FinanceReferences.RemoveRange(db.FinanceReferences);
            db.FinanceReferences.Add(new FinanceReference
            {
                Key = "DE",
                Label = "Trafag DE",
                Year = 2025,
                LocalCurrencyValue = 120m,
                IsActive = true
            });
            db.ExportLogs.Add(new ExportLog
            {
                SiteId = 1,
                Timestamp = new DateTime(2025, 1, 20, 10, 0, 0),
                Land = "Deutschland",
                TSC = "TRDE",
                Status = "OK",
                RowCount = 2,
                FileName = "de.xlsx",
                FilePath = "de.xlsx"
            });
            await db.SaveChangesAsync();
        }

        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-1", "EUR", 100m, new DateTime(2025, 1, 10)),
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "GS-1", "EUR", -20m, new DateTime(2025, 1, 11), quantity: -1m),
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-2", "EUR", 0m, new DateTime(2025, 1, 12)));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, "DE", null);

        var country = Assert.Single(result.CountryRows);
        Assert.Equal("DE", country.CountryKey);
        Assert.Equal(80m, country.NetSalesActual);
        Assert.Equal(120m, country.ReferenceValue);
        Assert.Equal(-40m, country.Difference);
        Assert.Equal("Pruefen", country.Status);

        Assert.Single(result.DeviationRows);
        Assert.Contains(result.DataStatusRows, row => row.Tsc == "TRDE" && row.RowCount == 3 && row.LatestExportStatus == "OK");
        Assert.Contains(result.CreditCandidates, row => row.InvoiceNumber == "GS-1" && row.NetSalesActual == -20m);
        Assert.Contains(result.DataQualityRows, row => row.Issue == "Nullwerte im Finance-Wert" && row.Count == 1);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_GroupCurrency_ConvertsValuesToChf()
    {
        await SeedRatesAsync(CreateRate("EUR", "CHF", 0.95m));
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-CH", "CHF", 100m, new DateTime(2025, 6, 1)),
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-DE", "EUR", 200m, new DateTime(2025, 6, 1)));

        var local = await _service.AnalyzeFinanceSummaryAsync(2025, null, null, useGroupCurrency: false);
        var group = await _service.AnalyzeFinanceSummaryAsync(2025, null, null, useGroupCurrency: true);

        // Local view keeps both currencies.
        Assert.Contains(local.Rows, row => row.Currency == "EUR");
        Assert.Contains(local.Rows, row => row.Currency == "CHF");

        // Group view converts everything to CHF: 100 CHF + 200 EUR * 0.95 = 290 CHF.
        Assert.Equal("CHF", group.DisplayCurrency);
        Assert.All(group.Rows, row => Assert.Equal("CHF", row.Currency));
        Assert.Equal(290m, group.NetSalesActual);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_CreditNote_ReversesCostBasisInMargin()
    {
        // CHF -> CHF keeps the rate at 1, so original and CHF amounts are identical and the
        // arithmetic is easy to verify. One normal sale and one credit note for the same article.
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-1", "CHF", 100m, new DateTime(2025, 3, 1),
                quantity: 1m, standardCost: 60m),
            CreateRow("SAP", "Schweiz", "TRCH", "GS-1", "CHF", -100m, new DateTime(2025, 3, 2),
                quantity: -1m, standardCost: 60m));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        var normal = Assert.Single(result.GroupMarginDetailRows, row => row.InvoiceNumber == "INV-1");
        Assert.Equal(60m, normal.CostBasisValue);
        Assert.Equal(40m, normal.MarginValue);

        var credit = Assert.Single(result.GroupMarginDetailRows, row => row.InvoiceNumber == "GS-1");
        // Cost basis must reverse with the negative sale: -100 - (-60) = -40 (not -160).
        Assert.Equal(-60m, credit.CostBasisValue);
        Assert.Equal(-40m, credit.MarginValue);

        // Net of sale + credit cancels out for both sales and cost basis.
        Assert.Equal(0m, result.GroupMarginSummary.SalesValue);
        Assert.Equal(0m, result.GroupMarginSummary.CostBasisValue);
        Assert.Equal(0m, result.GroupMarginSummary.MarginValue);

        // The Finance Pruefbuch (audit ledger) uses the same cost basis for its CHF margin.
        var creditLedger = Assert.Single(result.FinanceAuditLedgerRows, row => row.InvoiceNumber == "GS-1");
        Assert.Equal(-60m, creditLedger.CostBasisChf!.Value);
        Assert.Equal(-40m, creditLedger.MarginChf!.Value);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_CostCurrencyMismatch_Masks_Margin_By_Default()
    {
        // Verkauf in CHF, Standardkosten in EUR: ohne Fachentscheid (Default Mask) bleibt die
        // Marge in Originalwaehrung offen, statt CHF-Umsatz mit EUR-Kosten zu mischen.
        await SeedRatesAsync(CreateRate("EUR", "CHF", 0.95m));
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-MIX", "CHF", 100m, new DateTime(2025, 3, 1),
                quantity: 1m, standardCost: 60m, standardCostCurrency: "EUR"));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        var detail = Assert.Single(result.GroupMarginDetailRows, row => row.InvoiceNumber == "INV-MIX");
        Assert.Equal("Kostenwaehrung abweichend", detail.Status);
        Assert.Equal(1, result.GroupMarginSummary.MissingCostRows);

        var ledger = Assert.Single(result.FinanceAuditLedgerRows, row => row.InvoiceNumber == "INV-MIX");
        Assert.Equal("Kostenwaehrung abweichend", ledger.Status);
        Assert.Null(ledger.MarginOriginal);
        Assert.Null(ledger.MarginPercent);
        // Marge CHF bleibt korrekt rechenbar: 100 CHF - 60 EUR * 0.95 = 43 CHF.
        Assert.Equal(43m, ledger.MarginChf!.Value);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_AuditLedger_LeavesMarginOpen_WhenCostBasisIsMissing()
    {
        // Ohne Standardpreis ist die Kostenbasis 0. "Umsatz minus 0" ergaebe genau den vollen
        // Umsatz als Marge und 100 % — die Fehlinterpretation, die der Status verhindern soll.
        // Das Pruefbuch hat sie bis 2026-08-06 ausgewiesen, direkt neben dem Status.
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-NOCOST", "CHF", 100m, new DateTime(2025, 3, 1),
                quantity: 1m, standardCost: 0m, standardCostCurrency: "CHF"));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        var ledger = Assert.Single(result.FinanceAuditLedgerRows, row => row.InvoiceNumber == "INV-NOCOST");
        Assert.Equal(GroupMarginStatuses.StandardCostMissing, ledger.Status);
        Assert.Null(ledger.MarginOriginal);
        Assert.Null(ledger.MarginPercent);
        Assert.Null(ledger.MarginChf);
        // Der Umsatz selbst bleibt sichtbar — offen ist die Marge, nicht die Zeile.
        Assert.Equal(100m, ledger.OriginalAmount);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_CostCurrencyMismatch_Converts_When_Switch_Active()
    {
        await SeedExportSettingsAsync(GroupMarginCostCurrencyModes.Convert);
        await SeedRatesAsync(CreateRate("EUR", "CHF", 0.95m));
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-MIX", "CHF", 100m, new DateTime(2025, 3, 1),
                quantity: 1m, standardCost: 60m, standardCostCurrency: "EUR"));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        // Kostenbasis 60 EUR wird mit dem Jahreskurs in die Verkaufswaehrung umgerechnet:
        // 60 * 0.95 = 57 CHF -> Marge 43 CHF, Zeile bleibt belastbar (OK).
        var detail = Assert.Single(result.GroupMarginDetailRows, row => row.InvoiceNumber == "INV-MIX");
        Assert.Equal("OK", detail.Status);
        Assert.Equal(57m, detail.CostBasisValue);
        Assert.Equal(43m, detail.MarginValue);
        Assert.Equal(0, result.GroupMarginSummary.MissingCostRows);

        var ledger = Assert.Single(result.FinanceAuditLedgerRows, row => row.InvoiceNumber == "INV-MIX");
        Assert.Equal("OK", ledger.Status);
        Assert.Equal(43m, ledger.MarginOriginal!.Value);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_UsesGroupStandardCost_ForTrAgDeliveringSupplier()
    {
        // TR AG liefert (Mappe1.xlsx): die Konzern-Kostenbasis (MBEW-STPRS, hier 30 CHF/Stk)
        // ersetzt die lokale Verkaufszeilen-Kostenbasis (999). Verkauft in CH, damit Verkaufs-
        // und Kostenwaehrung beide CHF sind (reiner Test der Override-Logik ohne Waehrungsthema).
        await SeedGroupStandardCostAsync("MAT-TRAG", "1100", 30m, "CHF");
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-TRAG", "CHF", 100m, new DateTime(2025, 3, 1),
                quantity: 2m, standardCost: 999m, standardCostCurrency: "CHF", material: "MAT-TRAG", supplierName: "Trafag AG"));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        // Kostenbasis = Menge x Konzernkosten (2 x 30 = 60), NICHT die lokale StandardCost (999).
        var detail = Assert.Single(result.GroupMarginDetailRows, row => row.InvoiceNumber == "INV-TRAG");
        Assert.Equal("OK", detail.Status);
        Assert.Equal(60m, detail.CostBasisValue);
        Assert.Equal(40m, detail.MarginValue);
        Assert.Contains("Konzernkosten TR AG", detail.CostSource);

        var ledger = Assert.Single(result.FinanceAuditLedgerRows, row => row.InvoiceNumber == "INV-TRAG");
        Assert.Equal(60m, ledger.CostBasisOriginal);
        Assert.Contains("Konzernkosten TR AG", ledger.CostSource);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_FallsBackToLocalCost_WhenTrAgSupplierHasNoGroupStandardCostMatch()
    {
        // Kein GroupStandardCosts-Eintrag fuer dieses Material -> unveraendertes bisheriges
        // Verhalten (lokale StandardCost), keine Regression fuer noch nicht erfasste Materialien.
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-NOMATCH", "CHF", 100m, new DateTime(2025, 3, 1),
                quantity: 2m, standardCost: 20m, standardCostCurrency: "CHF", material: "MAT-UNKNOWN", supplierName: "Trafag AG"));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        var detail = Assert.Single(result.GroupMarginDetailRows, row => row.InvoiceNumber == "INV-NOMATCH");
        Assert.Equal("OK", detail.Status);
        Assert.Equal(40m, detail.CostBasisValue); // 2 x 20 (lokale StandardCost)
        Assert.DoesNotContain("Konzernkosten", detail.CostSource);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_GroupStandardCost_CrossCountryCurrencyMismatch_MasksByDefault_ConvertsWhenSwitched()
    {
        // Realistischstes Szenario: TR AG liefert an eine DE-Verkaufszeile (Finance-Waehrung
        // EUR), die Konzernkosten stehen aber in CHF (TR AGs Hauswaehrung) -> derselbe
        // Kostenwaehrungsschalter greift wie bei jeder anderen Waehrungsabweichung.
        await SeedRatesAsync(CreateRate("CHF", "EUR", 1.05m));
        await SeedGroupStandardCostAsync("MAT-TRAG-DE", "1100", 30m, "CHF");
        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-TRAG-DE", "EUR", 100m, new DateTime(2025, 3, 1),
                quantity: 2m, standardCost: 999m, standardCostCurrency: "EUR", material: "MAT-TRAG-DE", supplierName: "Trafag AG"));

        var maskResult = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);
        var maskDetail = Assert.Single(maskResult.GroupMarginDetailRows, row => row.InvoiceNumber == "INV-TRAG-DE");
        Assert.Equal(GroupMarginCostCurrencyConverter.OpenStatus, maskDetail.Status);

        await SeedExportSettingsAsync(GroupMarginCostCurrencyModes.Convert);
        var convertResult = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);
        var convertDetail = Assert.Single(convertResult.GroupMarginDetailRows, row => row.InvoiceNumber == "INV-TRAG-DE");
        // Konzernkosten 60 CHF (2 x 30) werden mit dem Jahreskurs CHF->EUR (1.05) umgerechnet: 63 EUR.
        Assert.Equal("OK", convertDetail.Status);
        Assert.Equal(63m, convertDetail.CostBasisValue);
        Assert.Equal(37m, convertDetail.MarginValue);
        Assert.Contains("Konzernkosten TR AG", convertDetail.CostSource);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_Keeps_Reference_Only_Countries_In_Expert_Mode()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.FinanceReferences.RemoveRange(db.FinanceReferences);
            db.FinanceReferences.AddRange(
                new FinanceReference
                {
                    Key = "DE",
                    Label = "Trafag DE",
                    Year = 2025,
                    LocalCurrencyValue = 120m,
                    IsActive = true
                },
                new FinanceReference
                {
                    Key = "IT",
                    Label = "Trafag IT",
                    Year = 2025,
                    LocalCurrencyValue = 7669840m,
                    IsActive = true
                });
            await db.SaveChangesAsync();
        }

        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-1", "EUR", 100m, new DateTime(2025, 1, 10)));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        var italy = Assert.Single(result.CountryRows, row => row.CountryKey == "IT");
        Assert.Equal(7669840m, italy.ReferenceValue);
        Assert.Equal(0m, italy.NetSalesActual);
        Assert.Equal(0, italy.TotalRows);
        Assert.Equal("Keine Daten", italy.Status);
        Assert.Contains("IT", result.CountryOptions);

        var filteredResult = await _service.AnalyzeFinanceSummaryAsync(2025, "IT", null);
        var filteredItaly = Assert.Single(filteredResult.CountryRows);
        Assert.Equal("IT", filteredItaly.CountryKey);
        Assert.Equal(7669840m, filteredItaly.ReferenceValue);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_Builds_Central_Product_Assignment_Tab_Data()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "ZSCHWEIZ", "CH-1", "CHF", 100m, new DateTime(2025, 1, 10),
                material: "000MAT-OK",
                name: "Reference article",
                productHierarchyCode: "0414",
                productHierarchyText: "Industat innen",
                productFamilyCode: "0004",
                productFamilyText: "Industat",
                productDivisionCode: "0001",
                productDivisionText: "Thermostate",
                productMappingAssigned: "X"),
            CreateRow("SAP", "Schweiz", "ZSCHWEIZ", "CH-2", "CHF", 10m, new DateTime(2025, 1, 10),
                material: "MAT-UNASS",
                productHierarchyCode: "0509",
                productHierarchyText: "Multistat",
                productDivisionCode: "UNASS",
                productDivisionText: "Nicht zugeordnet",
                productMappingAssigned: "false"),
            CreateRow("SAP", "Schweiz", "ZSCHWEIZ", "CH-3", "CHF", 40m, new DateTime(2025, 1, 10),
                material: "000000000000000006",
                name: "Misc article",
                productFamilyText: "Übrige",
                productDivisionCode: "0008",
                productDivisionText: "Übrige",
                productMappingAssigned: "true"),
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "DE-1", "EUR", 80m, new DateTime(2025, 1, 11),
                material: "MAT-OK",
                name: "German article"),
            CreateRow("MANUAL_EXCEL", "Italien", "TRIT", "IT-1", "EUR", 50m, new DateTime(2025, 1, 12),
                material: "MAT-MISSING",
                name: "Unknown article"),
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "DE-2", "EUR", 20m, new DateTime(2025, 1, 13),
                material: "MAT-UNASS",
                name: "Unassigned article"));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        Assert.Equal(6, result.ProductAssignmentSummary.DistinctMaterialCount);
        Assert.Equal(2, result.ProductAssignmentSummary.MatchedMaterialCount);
        Assert.Equal(1, result.ProductAssignmentSummary.MiscMaterialCount);
        Assert.Equal(2, result.ProductAssignmentSummary.UnassignedMaterialCount);
        Assert.Equal(1, result.ProductAssignmentSummary.MissingReferenceMaterialCount);

        var assigned = Assert.Single(result.ProductAssignmentRows, row => row.Material == "MAT-OK" && row.Tsc == "TRDE");
        Assert.Equal("Zugeordnet", assigned.Status);
        Assert.Equal("0414", assigned.ProductHierarchyCode);
        Assert.Equal("0001", assigned.ProductDivisionCode);

        var missing = Assert.Single(result.ProductAssignmentRows, row => row.Material == "MAT-MISSING" && row.Tsc == "TRIT");
        Assert.Equal("Nicht im TR-AG-Stamm", missing.Status);

        var unassigned = Assert.Single(result.ProductAssignmentRows, row => row.Material == "MAT-UNASS" && row.Tsc == "TRDE");
        Assert.Equal("Nicht zugeordnet", unassigned.Status);
        Assert.Equal("UNASS", unassigned.ProductDivisionCode);

        var misc = Assert.Single(result.ProductAssignmentRows, row => row.Material == "000000000000000006" && row.Tsc == "ZSCHWEIZ");
        Assert.Equal("Übrige", misc.Status);
        Assert.Equal("", misc.ProductFamilyCode);
        Assert.Equal("Übrige", misc.ProductFamilyText);
        Assert.Equal("0008", misc.ProductDivisionCode);
        Assert.Equal("Übrige", misc.ProductDivisionText);

        Assert.Contains(result.ProductAssignmentCountryRows, row =>
            row.CountryKey == "DE" &&
            row.Tsc == "TRDE" &&
            row.MatchedMaterialCount == 1 &&
            row.UnassignedMaterialCount == 1);
        Assert.Contains(result.ProductAssignmentCountryRows, row =>
            row.CountryKey == "CH" &&
            row.Tsc == "ZSCHWEIZ" &&
            row.MatchedMaterialCount == 1 &&
            row.MiscMaterialCount == 1 &&
            row.UnassignedMaterialCount == 1);

        Assert.Equal(300m, result.ProductFinanceSummary.TotalValue);
        Assert.Equal(180m, result.ProductFinanceSummary.AssignedValue);
        Assert.Equal(40m, result.ProductFinanceSummary.MiscValue);
        Assert.Equal(30m, result.ProductFinanceSummary.UnassignedValue);
        Assert.Equal(50m, result.ProductFinanceSummary.MissingReferenceValue);
        Assert.Equal(180m * 100m / 300m, result.ProductFinanceSummary.AssignedValuePercent);
        Assert.Equal(40m * 100m / 300m, result.ProductFinanceSummary.MiscValuePercent);

        Assert.Contains(result.ProductDivisionFinanceRows, row =>
            row.ProductDivisionCode == "0001" &&
            row.Currency == "EUR" &&
            row.NetSalesActual == 80m &&
            row.MaterialCount == 1 &&
            row.Countries == "DE");
        Assert.Contains(result.ProductDivisionFinanceRows, row =>
            row.ProductDivisionCode == "0001" &&
            row.Currency == "CHF" &&
            row.NetSalesActual == 100m);
        Assert.Contains(result.ProductDivisionFinanceRows, row =>
            row.ProductDivisionCode == "0008" &&
            row.ProductDivisionText == "Übrige" &&
            row.ProductFamilyCode == "" &&
            row.ProductFamilyText == "Übrige" &&
            row.Currency == "CHF" &&
            row.NetSalesActual == 40m &&
            row.MaterialCount == 1);

        var deFinanceCoverage = Assert.Single(result.ProductFinanceCountryRows, row => row.CountryKey == "DE" && row.Tsc == "TRDE");
        Assert.Equal(100m, deFinanceCoverage.TotalValue);
        Assert.Equal(80m, deFinanceCoverage.AssignedValue);
        Assert.Equal(20m, deFinanceCoverage.UnassignedValue);
        Assert.Equal(80m, deFinanceCoverage.AssignedValuePercent);

        var chFinanceCoverage = Assert.Single(result.ProductFinanceCountryRows, row => row.CountryKey == "CH" && row.Tsc == "ZSCHWEIZ");
        Assert.Equal(150m, chFinanceCoverage.TotalValue);
        Assert.Equal(100m, chFinanceCoverage.AssignedValue);
        Assert.Equal(40m, chFinanceCoverage.MiscValue);
        Assert.Equal(10m, chFinanceCoverage.UnassignedValue);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_Warns_When_Product_Assignment_Coverage_Is_Implausibly_Low()
    {
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "ZSCHWEIZ", "CH-1", "CHF", 10m, new DateTime(2025, 1, 10),
                material: "MAT-OK",
                productHierarchyCode: "0414",
                productFamilyCode: "0004",
                productDivisionCode: "0001",
                productDivisionText: "Thermostate",
                productMappingAssigned: "X"),
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "DE-1", "EUR", 90m, new DateTime(2025, 1, 11),
                material: "MAT-MISSING"));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        Assert.Equal(100m, result.ProductFinanceSummary.TotalValue);
        Assert.Equal(90m, result.ProductFinanceSummary.MissingReferenceValue);
        Assert.Contains(result.Notices, notice =>
            notice.Contains("Spartenanalyse auffaellig", StringComparison.OrdinalIgnoreCase) &&
            notice.Contains("90.0%", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Notices, notice =>
            notice.Contains("ProductDivisionRefSet", StringComparison.OrdinalIgnoreCase) &&
            notice.Contains("fuehrende Nullen", StringComparison.OrdinalIgnoreCase));
    }


    [Fact]
    public void BuildDataHeartbeatDays_Treats_Fresh_Weekday_Zero_As_NoBusiness()
    {
        var days = ManagementCockpitService.BuildDataHeartbeatDays(
            [
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 6), 3, 100m),
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 7), 3, 100m),
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 9), 3, 100m),
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 10), 3, 100m)
            ],
            new DateTime(2026, 7, 12),
            new DateOnly(2026, 7, 6),
            new DateOnly(2026, 7, 12),
            new DateOnly(2026, 7, 12));

        Assert.Equal(HeartbeatDayStatus.WeekendOrNoBusiness, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 8)).Status);
        Assert.Equal(HeartbeatDayStatus.WeekendOrNoBusiness, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 11)).Status);
        Assert.Equal(HeartbeatDayStatus.WeekendOrNoBusiness, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 12)).Status);
    }

    [Fact]
    public void BuildDataHeartbeatDays_Warns_When_Freshness_Is_Missing_After_Latest_Data()
    {
        var days = ManagementCockpitService.BuildDataHeartbeatDays(
            [new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 6), 3, 100m)],
            null,
            new DateOnly(2026, 7, 6),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 10));

        Assert.Equal(HeartbeatDayStatus.Ok, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 6)).Status);
        Assert.Equal(HeartbeatDayStatus.Warn, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 7)).Status);
    }

    [Fact]
    public void BuildDataHeartbeatDays_Stale_LastUpdate_Forces_Gap()
    {
        var days = ManagementCockpitService.BuildDataHeartbeatDays(
            [new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 6), 3, 100m)],
            new DateTime(2026, 7, 7),
            new DateOnly(2026, 7, 6),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 10));

        Assert.Equal(HeartbeatDayStatus.Gap, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 10)).Status);
    }

    [Fact]
    public void BuildDataHeartbeatDays_Does_Not_Count_MidWindow_NoBusiness_As_Gap()
    {
        var days = ManagementCockpitService.BuildDataHeartbeatDays(
            [
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 6), 2, 100m),
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 7), 2, 100m),
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 9), 2, 100m),
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 10), 2, 100m)
            ],
            new DateTime(2026, 7, 10),
            new DateOnly(2026, 7, 6),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 10));

        Assert.Equal(HeartbeatDayStatus.WeekendOrNoBusiness, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 8)).Status);
        Assert.Equal(0, days.Count(day => day.Status == HeartbeatDayStatus.Gap));
    }

    [Fact]
    public void BuildDataHeartbeatDays_Only_Returns_Window_Dates()
    {
        var days = ManagementCockpitService.BuildDataHeartbeatDays(
            [
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 5), 4, 100m),
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 7), 4, 100m)
            ],
            new DateTime(2026, 7, 7),
            new DateOnly(2026, 7, 6),
            new DateOnly(2026, 7, 7),
            new DateOnly(2026, 7, 7));

        Assert.Equal([new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 7)], days.Select(day => day.Date).ToList());
    }

    [Fact]
    public void BuildDataHeartbeatDays_Computes_Rolling_Seven_Day_Sum()
    {
        var days = ManagementCockpitService.BuildDataHeartbeatDays(
            [
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 1), 2, 100m),
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 3), 5, 100m),
                new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 9), 4, 100m)
            ],
            new DateTime(2026, 7, 10),
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 10));

        Assert.Equal(2, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 1)).RollingRowCount7);
        Assert.Equal(7, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 3)).RollingRowCount7);
        // 7-Tage-Fenster 03.07.-09.07. enthaelt die 5 vom 03.07. und die 4 vom 09.07., die 2 vom 01.07. nicht mehr
        Assert.Equal(9, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 9)).RollingRowCount7);
        // Fenster 04.07.-10.07.: nur noch die 4 vom 09.07.
        Assert.Equal(4, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 10)).RollingRowCount7);
    }

    [Fact]
    public void ApplyHeartbeatExportRuns_Maps_Ok_Error_Missed_And_Unknown()
    {
        var days = ManagementCockpitService.BuildDataHeartbeatDays(
            [new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 8), 3, 100m)],
            new DateTime(2026, 7, 10),
            new DateOnly(2026, 7, 6),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 10));

        ManagementCockpitService.ApplyHeartbeatExportRuns(
            days,
            [
                new ManagementCockpitService.HeartbeatExportRunInput(new DateOnly(2026, 7, 7), true, new DateTime(2026, 7, 7, 12, 0, 0)),
                new ManagementCockpitService.HeartbeatExportRunInput(new DateOnly(2026, 7, 8), false, new DateTime(2026, 7, 8, 12, 0, 0)),
                new ManagementCockpitService.HeartbeatExportRunInput(new DateOnly(2026, 7, 8), true, new DateTime(2026, 7, 8, 14, 0, 0)),
                new ManagementCockpitService.HeartbeatExportRunInput(new DateOnly(2026, 7, 9), false, new DateTime(2026, 7, 9, 12, 0, 0))
            ],
            new DateOnly(2026, 7, 10));

        Assert.Equal(HeartbeatExportRunStatus.Unknown, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 6)).ExportRun);
        Assert.Equal(HeartbeatExportRunStatus.Ok, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 7)).ExportRun);
        Assert.Equal(HeartbeatExportRunStatus.Ok, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 8)).ExportRun);
        Assert.Equal(HeartbeatExportRunStatus.Error, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 9)).ExportRun);
        Assert.Equal(HeartbeatExportRunStatus.Missed, Assert.Single(days, day => day.Date == new DateOnly(2026, 7, 10)).ExportRun);
    }

    [Fact]
    public void ApplyHeartbeatExportRuns_Without_Runs_Leaves_All_Unknown()
    {
        var days = ManagementCockpitService.BuildDataHeartbeatDays(
            [new ManagementCockpitService.HeartbeatDailyInput(new DateOnly(2026, 7, 8), 3, 100m)],
            new DateTime(2026, 7, 10),
            new DateOnly(2026, 7, 6),
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 10));

        ManagementCockpitService.ApplyHeartbeatExportRuns(days, [], new DateOnly(2026, 7, 10));

        Assert.All(days, day => Assert.Equal(HeartbeatExportRunStatus.Unknown, day.ExportRun));
    }

    [Fact]
    public async Task AnalyzeDataHeartbeatAsync_Fills_Export_Run_Stripe_From_ExportLogs()
    {
        var financeDate = MostRecentWeekday(DateTime.Today.AddDays(-1));
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Deutschland", "TRDE", "INV-1", "EUR", 50m, financeDate, financeDate));
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.ExportLogs.RemoveRange(db.ExportLogs);
            await db.SaveChangesAsync();
            db.ExportLogs.Add(new ExportLog
            {
                SiteId = 1,
                Timestamp = financeDate.AddHours(12),
                Land = "Deutschland",
                TSC = "TRDE",
                Status = "OK",
                RowCount = 1,
                FileName = "de.xlsx",
                FilePath = "de.xlsx"
            });
            await db.SaveChangesAsync();
        }

        var result = await _service.AnalyzeDataHeartbeatAsync(30);

        var de = Assert.Single(result.Countries, row => row.Tsc == "TRDE");
        Assert.NotNull(de.LastSuccessfulExportUtc);
        Assert.Equal(HeartbeatExportRunStatus.Ok, Assert.Single(de.Days, day => day.Date == DateOnly.FromDateTime(financeDate)).ExportRun);
        Assert.Contains(de.Days, day => day.Date < DateOnly.FromDateTime(financeDate) && day.ExportRun == HeartbeatExportRunStatus.Unknown);
    }

    [Fact]
    public async Task AnalyzeDataHeartbeatAsync_Uses_Posting_Invoice_Extraction_Date_Fallback()
    {
        var financeDate = MostRecentWeekday(DateTime.Today.AddDays(-1));
        var extractionDate = financeDate.AddDays(-5);
        await SeedCentralRowsAsync(
            CreateRow("SAP", "Schweiz", "TRCH", "INV-POST", "CHF", 100m, financeDate.AddDays(-10), extractionDate, postingDate: financeDate),
            CreateRow("SAP", "Deutschland", "TRDE", "INV-INV", "EUR", 50m, financeDate, extractionDate),
            CreateRow("SAP", "Italien", "TRIT", "INV-EXT", "EUR", 70m, null, financeDate));

        var result = await _service.AnalyzeDataHeartbeatAsync(30);

        var ch = Assert.Single(result.Countries, row => row.Tsc == "TRCH");
        var de = Assert.Single(result.Countries, row => row.Tsc == "TRDE");
        var it = Assert.Single(result.Countries, row => row.Tsc == "TRIT");
        Assert.NotNull(ch.LastUpdateUtc);
        Assert.NotNull(de.LastUpdateUtc);
        Assert.NotNull(it.LastUpdateUtc);
        Assert.Contains(ch.Days, day => day.Date == DateOnly.FromDateTime(financeDate) && day.RowCount == 1);
        Assert.Contains(de.Days, day => day.Date == DateOnly.FromDateTime(financeDate) && day.RowCount == 1);
        Assert.Contains(it.Days, day => day.Date == DateOnly.FromDateTime(financeDate) && day.RowCount == 1);
    }
    private async Task SeedCentralRowsAsync(params CentralSalesRecord[] rows)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.CentralSalesRecords.RemoveRange(db.CentralSalesRecords);
        await db.SaveChangesAsync();
        db.CentralSalesRecords.AddRange(rows);
        await db.SaveChangesAsync();
    }

    private async Task SeedRatesAsync(params CurrencyExchangeRate[] rates)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.CurrencyExchangeRates.RemoveRange(db.CurrencyExchangeRates);
        await db.SaveChangesAsync();
        db.CurrencyExchangeRates.AddRange(rates);
        await db.SaveChangesAsync();
    }

    private async Task SeedExportSettingsAsync(string groupMarginCostCurrencyMode)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.ExportSettings.RemoveRange(db.ExportSettings);
        await db.SaveChangesAsync();
        db.ExportSettings.Add(new ExportSettings { GroupMarginCostCurrencyMode = groupMarginCostCurrencyMode });
        await db.SaveChangesAsync();
    }

    private async Task SeedGroupStandardCostAsync(string material, string valuationArea, decimal unitCost, string currency)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.GroupStandardCosts.Add(new GroupStandardCost
        {
            MaterialKey = MaterialKeyNormalizer.Normalize(material),
            ValuationArea = valuationArea,
            UnitCost = unitCost,
            Currency = currency,
            RefreshedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }


    private static DateTime MostRecentWeekday(DateTime start)
    {
        var value = start.Date;
        while (value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            value = value.AddDays(-1);
        return value;
    }
    private static CurrencyExchangeRate CreateRate(string fromCurrency, string toCurrency, decimal rate)
        => new()
        {
            FromCurrency = fromCurrency,
            ToCurrency = toCurrency,
            Rate = rate,
            ValidFrom = new DateTime(2024, 1, 1),
            IsActive = true
        };

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_CountsCountriesWithoutReference_SoTheTilesDoNotHideThem()
    {
        // Ohne Sollwert liefert BuildFinanceStatus "Kein Sollwert" - weder OK noch Pruefen. Die
        // Schnelluebersicht zaehlte nur die beiden anderen Stati und zeigte dann 0/0, was wie
        // "alles sauber" aussieht. Produktiv gemessen 2026-08-07: FinanceReferences enthaelt nur
        // Zeilen fuer 2025, das Standardjahr der Seite ist aber das juengste Jahr der Daten.
        // ACHTUNG zur Aussagekraft: die neue Kachel selbst steht in der Razor. Dieser Test pinnt
        // den Statusvertrag darunter - dass "Kein Sollwert" weder als geprueft noch als OK gilt.
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.FinanceReferences.RemoveRange(db.FinanceReferences);
            await db.SaveChangesAsync();
        }

        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-1", "EUR", 100m, new DateTime(2025, 1, 10)));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, "DE", null);

        var country = Assert.Single(result.CountryRows);
        Assert.Equal(FinanceCountryStatuses.NoReference, country.Status);
        Assert.True(FinanceCountryStatuses.IsUnverified(country.Status));
        Assert.False(FinanceCountryStatuses.IsChecked(country.Status));
        Assert.Empty(result.CountryRows.Where(row => FinanceCountryStatuses.IsChecked(row.Status)));
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_FinancePivot_FollowsTheCountryFilter()
    {
        // Der Pivot rechnete auf allRows statt auf den gefilterten Zeilen. Mit Landfilter zeigte
        // "Net Sales Actual" ein Land und die Pivotkacheln daneben weiterhin alle.
        await SeedRatesAsync(CreateRate("EUR", "CHF", 0.95m));
        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-DE", "EUR", 100m, new DateTime(2025, 3, 10)),
            CreateRow("SAP", "Schweiz", "TRCH", "INV-CH", "CHF", 400m, new DateTime(2025, 3, 11)));

        var unfiltered = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);
        Assert.Equal(2, unfiltered.FinancePivot.TscColumns.Count);
        Assert.Equal(2, unfiltered.FinancePivot.RowCount);
        Assert.Equal(0, unfiltered.FinancePivot.MissingRateRowCount);

        var filtered = await _service.AnalyzeFinanceSummaryAsync(2025, "DE", null);
        Assert.Equal(1, filtered.FinancePivot.RowCount);
        Assert.Equal("TRDE", Assert.Single(filtered.FinancePivot.TscColumns));
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_FinancePivot_CountsRowsItHadToDropForAMissingRate()
    {
        // Ohne CHF-Kurs faellt die Zeile aus der Pivotsicht heraus - sie wird verworfen, nicht mit
        // 0 gerechnet. Ohne diesen Zaehler weicht die Kachel "Zeilenbasis" still von "Enthaltene
        // Zeilen" im Finance Summary ab und niemand kann den Unterschied erklaeren.
        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-DE", "EUR", 100m, new DateTime(2025, 3, 10)));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, "DE", null);

        Assert.Equal(1, result.IncludedRows);
        Assert.Equal(0, result.FinancePivot.RowCount);
        Assert.Equal(1, result.FinancePivot.MissingRateRowCount);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_SeparatesRuleExclusionFromGenuineZeroValue()
    {
        // ResolveNetSalesActual gibt fuer ausgeschlossene Zeilen 0 zurueck. Ohne Trennung meldeten
        // die Pruefpunkte "Nullwerte im Finance-Wert" und "Ausgeschlossene Zeilen" dieselben
        // Zeilen zweimal. Der Fixture-Aufbau ist deshalb bewusst kreuzweise: eine ausgeschlossene
        // Zeile MIT Betrag und eine eingeschlossene Zeile OHNE Betrag.
        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-1", "EUR", 100m, new DateTime(2025, 1, 10)),
            // Standardregel DE: CustomerName "Trafag AG" wird ausgeschlossen - Betrag aber <> 0.
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-EXCL", "EUR", 250m, new DateTime(2025, 1, 11),
                customerName: "Trafag AG"),
            // Echter Nullwert, von keiner Regel betroffen.
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-ZERO", "EUR", 0m, new DateTime(2025, 1, 12)));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, "DE", null);

        var zeroRow = Assert.Single(result.DataQualityRows, row => row.Issue == "Nullwerte im Finance-Wert");
        var excludedRow = Assert.Single(result.DataQualityRows, row => row.Issue == "Ausgeschlossene Zeilen");
        Assert.Equal(1, zeroRow.Count);
        Assert.Equal(1, excludedRow.Count);
        // Nur die Regelzeile und die Nullzeile fallen aus Include heraus, die Umsatzzeile bleibt.
        Assert.Equal(1, result.IncludedRows);
        Assert.Equal(2, result.ExcludedRows);
        Assert.Equal(100m, result.NetSalesActual);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_CountsTheSameMaterialFromTwoSitesOnce()
    {
        // DistinctMaterialCount ist die Zahl der Pruefzeilen (Material x Land x TSC x Quelle x
        // Waehrung) und darf nicht als Materialzahl beschriftet werden. Die echte Materialzahl
        // steht daneben.
        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-DE", "EUR", 100m, new DateTime(2025, 4, 1), material: "M-1"),
            CreateRow("SAP", "Schweiz", "TRCH", "INV-CH", "CHF", 100m, new DateTime(2025, 4, 2), material: "M-1"));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, null, null);

        Assert.Equal(2, result.ProductAssignmentSummary.DistinctMaterialCount);
        Assert.Equal(1, result.ProductAssignmentSummary.DistinctMaterialNumberCount);
    }

    [Fact]
    public async Task AnalyzeFinanceSummaryAsync_FinancePivot_KeepsAMeasuredZeroInsteadOfDroppingIt()
    {
        // Rechnung und Gutschrift heben sich im selben Monat auf. Das Ergebnis 0 ist gemessen und
        // muss als 0 in der Matrix stehen; die GUI zeigte es als "-", also wie fehlende Daten.
        // ACHTUNG zur Aussagekraft: der eigentliche Fehler sass in GetFinancePivotValue in der
        // Razor (`value != 0m ? value : null`). Dieser Test pinnt nur die Dienstseite - dass die
        // 0 ueberhaupt in ValuesByTsc ankommt. Die Anzeige selbst deckt er nicht ab.
        await SeedRatesAsync(CreateRate("EUR", "CHF", 0.95m));
        await SeedCentralRowsAsync(
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "INV-1", "EUR", 100m, new DateTime(2025, 5, 5)),
            CreateRow("MANUAL_EXCEL", "Deutschland", "TRDE", "GS-1", "EUR", -100m, new DateTime(2025, 5, 6), quantity: -1m));

        var result = await _service.AnalyzeFinanceSummaryAsync(2025, "DE", null);

        var monthRow = Assert.Single(result.FinancePivot.MonthlyRows.Where(row => row.Year == 2025 && row.Month == 5));
        Assert.True(monthRow.ValuesByTsc.ContainsKey("TRDE"));
        Assert.Equal(0m, monthRow.ValuesByTsc["TRDE"]);
    }

    private static CentralSalesRecord CreateRow(
        string sourceSystem,
        string land,
        string tsc,
        string invoiceNumber,
        string currency,
        decimal salesValue,
        DateTime? invoiceDate,
        DateTime? extractionDate = null,
        decimal quantity = 1m,
        decimal standardCost = 1m,
        string material = "MAT",
        string name = "Article",
        string productHierarchyCode = "",
        string productHierarchyText = "",
        string productFamilyCode = "",
        string productFamilyText = "",
        string productDivisionCode = "",
        string productDivisionText = "",
        string productMappingAssigned = "",
        DateTime? postingDate = null,
        string? standardCostCurrency = null,
        string supplierName = "Supplier",
        string customerName = "Customer")
    {
        return new CentralSalesRecord
        {
            SiteId = 1,
            StoredAtUtc = DateTime.UtcNow,
            SourceSystem = sourceSystem,
            ExtractionDate = extractionDate ?? invoiceDate ?? DateTime.UtcNow.Date,
            Tsc = tsc,
            InvoiceNumber = invoiceNumber,
            PositionOnInvoice = 1,
            Material = material,
            Name = name,
            ProductGroup = "PG",
            ProductHierarchyCode = productHierarchyCode,
            ProductHierarchyText = productHierarchyText,
            ProductFamilyCode = productFamilyCode,
            ProductFamilyText = productFamilyText,
            ProductDivisionCode = productDivisionCode,
            ProductDivisionText = productDivisionText,
            ProductMappingAssigned = productMappingAssigned,
            Quantity = quantity,
            SupplierNumber = "SUP",
            SupplierName = supplierName,
            SupplierCountry = "CH",
            CustomerNumber = "CUS",
            CustomerName = customerName,
            CustomerCountry = "CH",
            CustomerIndustry = "Industry",
            StandardCost = standardCost,
            StandardCostCurrency = standardCostCurrency ?? currency,
            PurchaseOrderNumber = "PO",
            SalesPriceValue = salesValue,
            SalesCurrency = currency,
            Incoterms2020 = "DAP",
            SalesResponsibleEmployee = "Alice",
            PostingDate = postingDate,
            InvoiceDate = invoiceDate,
            OrderDate = invoiceDate?.AddDays(-2),
            Land = land,
            DocumentType = "Invoice"
        };
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

    private sealed class CountingCurrencyExchangeRateService : ICurrencyExchangeRateService
    {
        public int ResolveRateCallCount { get; private set; }
        public List<DateTime?> EffectiveDates { get; } = [];

        public decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate)
        {
            ResolveRateCallCount++;
            EffectiveDates.Add(effectiveDate);
            return 2m;
        }

        public string NormalizeCurrencyCode(string? currencyCode)
            => string.IsNullOrWhiteSpace(currencyCode) ? string.Empty : currencyCode.Trim().ToUpperInvariant();
    }
}
