using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class PurchasingDashboardServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;
    private readonly PurchasingDashboardService _service;

    public PurchasingDashboardServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        CreatePurchasingCacheTables();

        _dbFactory = new TestDbContextFactory(options);
        _service = new PurchasingDashboardService(_dbFactory);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Current_Year_Total_Is_Not_Limited_To_Top_Ten_And_Uses_Consistent_Material_Group()
    {
        for (var i = 1; i <= 11; i++)
        {
            await ExecuteAsync($"INSERT INTO PurchasingEkkoCache (Ebeln,Bedat,Lifnr,Bstyp,Waers,Wkurs,LastLoadedAtUtc) VALUES ('P{i}','{DateTime.Today.Year}-01-01','L{i}','F','CHF','1','2026-01-01');");
            await ExecuteAsync($"INSERT INTO PurchasingEkpoCache (Ebeln,Ebelp,Matnr,Matkl,MaraMatkl,Menge,Netwr,LastLoadedAtUtc) VALUES ('P{i}','10','M{i}','OLD','NEW','1','100','2026-01-01');");
            await ExecuteAsync($"INSERT INTO PurchasingEketCache (Ebeln,Ebelp,Etenr,Eindt,Menge,Wemng,LastLoadedAtUtc) VALUES ('P{i}','10','1','2026-01-01','1','0','2026-01-01');");
        }
        var result = await _service.LoadAsync(new PurchasingDashboardFilter(new(DateTime.Today.Year, 1, 1), DateTime.Today));
        Assert.Equal(1100m, result.CurrentYearSpendChf);
        Assert.Equal(result.SpendChfSample, result.CurrentYearSpendChf);
        Assert.Equal(10, result.CurrentYearSupplierSpendRows.Count);
        Assert.Equal(1000m, result.CurrentYearSupplierSpendRows.Sum(x => x.Value));
        Assert.StartsWith("NEW:", result.TopMaterialGroupLabel);
        Assert.Equal("NEW", Assert.Single(result.MaterialGroupSpendRows).Label);
        Assert.Equal(0, result.MissingExchangeRatePositionCount);
        // Jahresgesamtwert bleibt unabhaengig vom anders gewaehlten Spend-Zeitraum.
        result = await _service.LoadAsync(new PurchasingDashboardFilter(new(2020, 1, 1), new(2020, 12, 31)));
        Assert.Equal(0m, result.SpendChfSample);
        Assert.Equal(1100m, result.CurrentYearSpendChf);
    }

    [Fact]
    public async Task Missing_Foreign_Exchange_Rate_Is_Explicitly_Flagged_Without_Inventing_A_Rate()
    {
        await SeedAsync();
        await ExecuteAsync("UPDATE PurchasingEkkoCache SET Waers='EUR', Wkurs='0';");
        var result = await _service.LoadAsync(new PurchasingDashboardFilter(new(2025, 1, 1), new(2025, 12, 31)));
        Assert.True(result.MissingExchangeRatePositionCount > 0);
        Assert.Contains("nicht belastbar", result.Message);
        Assert.Contains("1:1", result.Message);
    }

    [Fact]
    public async Task Empty_Cache_Does_Not_Publish_A_Live_Sample_As_Complete_Kpis()
    {
        var result = await _service.LoadAsync(new PurchasingDashboardFilter(new(2025, 1, 1), new(2025, 12, 31)));
        Assert.False(result.EkkoLoaded);
        Assert.False(result.EkpoLoaded);
        Assert.False(result.EketLoaded);
        Assert.Contains("Keine Live-Stichprobe", result.Message);
        Assert.Empty(result.SupplierYearSpendRows);
    }

    [Fact]
    public async Task LoadAsync_Spend_Excludes_Only_Loekz_Not_MaraMstae_When_DeletionFlagFilterActive()
    {
        await SeedAsync();

        var filter = new PurchasingDashboardFilter(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 31),
            ExcludeDeletedItems: true);

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Marco-Review 2026-07-10: Der heutige Materialstatus (MSTAE 98/99) filtert den
        // historischen Spend NICHT mehr — nur stornierte Positionen (Loekz) bleiben draussen.
        // Aktiv 100 + Mstae-99 200 + Mstae-98 300 = 600; Loekz-Position 400 raus.
        Assert.Equal(600m, state.SpendChfSample);
    }

    [Fact]
    public async Task LoadAsync_OpenValue_Still_Excludes_MaraMstae_98_99()
    {
        // Offene Werte (Zulauf) schliessen MSTAE 98/99 weiterhin aus: fuer kuenftige Lieferungen
        // ist ein heute auslaufendes/gesperrtes Material relevant.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, LastLoadedAtUtc) VALUES ('S1', '2025-06-01', 'L1', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Mstae, LastLoadedAtUtc) VALUES ('S1', '10', 'M1', '10', '100', '', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Mstae, LastLoadedAtUtc) VALUES ('S1', '20', 'M2', '10', '100', '99', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('S1', '10', '1', '2025-08-01', '10', '0', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('S1', '20', '1', '2025-08-01', '10', '0', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Spend zaehlt beide (200), offen nur die aktive Position (10 * 10 = 100).
        Assert.Equal(200m, state.SpendChfSample);
        Assert.Equal(100m, state.OpenValueSample);
    }

    [Fact]
    public async Task LoadAsync_OpenValue_Is_Period_Independent_Including_Before_FromDate()
    {
        // Marco-Review 2026-07-10: Verpflichtungen/offene Werte sind eine Stand-heute-Sicht und
        // zeitraumunabhaengig — auch Einteilungen VOR dem Von-Datum zaehlen, solange sie offen sind.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, LastLoadedAtUtc) VALUES ('P1', '2020-06-01', 'L1', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('P1', '10', 'M1', '10', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('P1', '10', '1', '2020-07-01', '10', '0', '2026-01-01');");

        // Von-Datum 2025 liegt weit nach der offenen Einteilung von 2020.
        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        Assert.Equal(100m, state.OpenValueSample);
        Assert.Equal(10m, state.OpenQuantitySample);
        // Spend bleibt zeitraumbezogen: Bedat 2020 liegt ausserhalb 2025 -> 0.
        Assert.Equal(0m, state.SpendChfSample);
    }

    [Fact]
    public async Task LoadAsync_Includes_All_Positions_When_DeletionFlagFilterInactive()
    {
        await SeedAsync();

        var filter = new PurchasingDashboardFilter(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 31),
            ExcludeDeletedItems: false);

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Alle vier Positionen: 100 + 200 (Mstae 99) + 300 (Mstae 98) + 400 (Loekz L).
        Assert.Equal(1000m, state.SpendChfSample);
    }

    [Fact]
    public async Task LoadAsync_Converts_ForeignCurrency_Spend_To_Chf_Using_Wkurs()
    {
        // K1: CHF-Beleg bleibt unveraendert, EUR-Beleg wird mit Wkurs nach CHF bewertet.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Waers, Wkurs, LastLoadedAtUtc) VALUES ('C1', '2025-03-01', 'L1', 'CHF', '1', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Waers, Wkurs, LastLoadedAtUtc) VALUES ('C2', '2025-04-01', 'L2', 'EUR', '0.95', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('C1', '10', 'M1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('C2', '10', 'M2', '1', '200', '2026-01-01');");
        // Damit der Cache als gefuellt erkannt wird (EKET muss vorhanden sein).
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('C1', '10', '1', '2025-03-15', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // 100 CHF + 200 EUR * 0.95 = 100 + 190 = 290.
        Assert.Equal(290m, state.SpendChfSample);
    }

    [Fact]
    public async Task LoadAsync_Includes_Future_Schedules_In_OpenValue_Even_When_ToDate_Is_Earlier()
    {
        // K3: Zukuenftiger Zulauf darf nicht am ToDate abgeschnitten werden.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, LastLoadedAtUtc) VALUES ('F1', '2025-06-01', 'L1', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('F1', '10', 'M1', '10', '100', '2026-01-01');");
        // Eindt liegt nach ToDate (2027 vs. 2025) -> Stueckwert 100/10 = 10, offene Menge 10, offener Wert 100.
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('F1', '10', '1', '2027-01-01', '10', '0', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        Assert.Equal(10m, state.OpenQuantitySample);
        Assert.Equal(100m, state.OpenValueSample);
    }

    [Fact]
    public async Task LoadAsync_ContractValue_Counts_Only_Positions_With_Konnr()
    {
        // K4: Kontrakt-Restwert nur fuer Abrufe zu Rahmenkontrakten (EKKO.Konnr gesetzt),
        // nicht mehr eine blosse Kopie des offenen Bestellwerts.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Konnr, LastLoadedAtUtc) VALUES ('K1', '2025-06-01', 'L1', '', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Konnr, LastLoadedAtUtc) VALUES ('K2', '2025-06-01', 'L2', '4600000123', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('K1', '10', 'M1', '5', '500', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('K2', '10', 'M2', '5', '500', '2026-01-01');");
        // Beide Belege haben offene Menge 5 -> Stueckwert 100, offener Wert je 500.
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('K1', '10', '1', '2025-08-01', '5', '0', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('K2', '10', '1', '2025-08-01', '5', '0', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Offener Wert gesamt = 1000, aber nur der Kontrakt-Beleg K2 zaehlt zum Kontrakt-Restwert.
        Assert.Equal(1000m, state.OpenValueSample);
        Assert.Equal(500m, state.ContractValueSample);
    }

    [Fact]
    public async Task LoadAsync_ContractChart_And_TopCommitment_Use_The_Same_Konnr_Basis_As_The_Kpi()
    {
        // Gegenstueck zu LoadAsync_ContractValue_Counts_Only_Positions_With_Konnr: die Kachel
        // "Restwert" filterte auf Konnr, Diagramm und "Top Verpflichtung" daneben nicht. Damit
        // zeigte derselbe Reiter zwei Grundmengen, die sich nicht gegeneinander abstimmen liessen.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Konnr, LastLoadedAtUtc) VALUES ('C1', '2025-06-01', 'L1', 'Ohne Kontrakt', '', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Konnr, LastLoadedAtUtc) VALUES ('C2', '2025-06-01', 'L2', 'Mit Kontrakt', '4600000123', '2026-01-01');");
        // Der Beleg ohne Konnr ist der groessere: ohne Filter wuerde er die Top-Verpflichtung stellen.
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('C1', '10', 'M1', '10', '9000', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('C2', '10', 'M2', '10', '1000', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('C1', '10', '1', '2025-08-01', '10', '0', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('C2', '10', '1', '2025-08-01', '10', '0', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        Assert.Equal(10000m, state.OpenValueSample);
        Assert.Equal(1000m, state.ContractValueSample);
        // Diagramm und Top-Verpflichtung kennen nur noch den Kontraktabruf.
        var chartRow = Assert.Single(state.ContractChartRows);
        Assert.Contains("M2", chartRow.Label, StringComparison.Ordinal);
        Assert.Equal(1000m, chartRow.Value);
        Assert.Contains("Mit Kontrakt", state.TopCommitmentLabel, StringComparison.Ordinal);
        Assert.DoesNotContain("Ohne Kontrakt", state.TopCommitmentLabel, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_Overdue_Counts_Only_Past_Due_Open_Positions()
    {
        // Phase 1.1: Ueberfaelliger Wert/Menge/Anzahl zaehlen nur offene Einteilungen, deren
        // Liefertermin in der Vergangenheit liegt; zukuenftiger Zulauf zaehlt nicht.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, LastLoadedAtUtc) VALUES ('O1', '2020-06-01', 'L1', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('O1', '10', 'M1', '10', '100', '2026-01-01');");
        // Stueckwert 100/10 = 10. Ueberfaellig: offene Menge 10 -> Wert 100.
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('O1', '10', '1', '2020-06-01', '10', '0', '2026-01-01');");
        // Zukuenftige Einteilung derselben Position: offen, aber nicht ueberfaellig.
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('O1', '10', '2', '2099-01-01', '5', '0', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2019, 1, 1), new DateTime(2020, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        Assert.Equal(100m, state.OverdueValueSample);
        Assert.Equal(10m, state.OverdueQuantitySample);
        Assert.Equal(1, state.OverduePositionCount);
        Assert.Single(state.OverduePositionRows);
    }

    [Fact]
    public async Task LoadAsync_ArticlePriceTrend_Computes_YoY_Trend_Per_Article()
    {
        // Phase 1.2: Preisentwicklung je Artikel = mengengewichteter Ø-Stueckpreis je Jahr mit
        // YoY-Trend. M1 steigt von 10 (2023) auf 12 (2024) -> +20% -> Severity High.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, LastLoadedAtUtc) VALUES ('A1', '2023-05-01', 'L1', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, LastLoadedAtUtc) VALUES ('A2', '2024-05-01', 'L1', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('A1', '10', 'M1', '10', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('A2', '10', 'M1', '10', '120', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('A1', '10', '1', '2023-05-15', '10', '10', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2020, 1, 1), new DateTime(2024, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        var row = Assert.Single(state.ArticlePriceTrendRows);
        Assert.Equal("M1", row.Label);
        Assert.Equal("High", row.Severity);
        Assert.Contains("2023", row.Detail);
        Assert.Contains("2024", row.Detail);
    }

    [Fact]
    public async Task LoadAsync_Spend_Counts_Only_Orders_Excluding_Inquiry_Contract_StockTransfer()
    {
        // Beleg-Mix-Trennung: nur echte Bestellungen (Bstyp F, Bsart <> UB) zaehlen zum Spend.
        // Anfrage (A/AN), Kontrakt (K/MK) und Umlagerung (F/UB) werden ausgeschlossen.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, Bsart, LastLoadedAtUtc) VALUES ('O1', '2025-03-01', 'L1', 'F', 'NB', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, Bsart, LastLoadedAtUtc) VALUES ('O2', '2025-03-01', 'L1', 'A', 'AN', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, Bsart, LastLoadedAtUtc) VALUES ('O3', '2025-03-01', 'L1', 'K', 'MK', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, Bsart, LastLoadedAtUtc) VALUES ('O4', '2025-03-01', 'L1', 'F', 'UB', '2026-01-01');");
        foreach (var ebeln in new[] { "O1", "O2", "O3", "O4" })
            await ExecuteAsync($"INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('{ebeln}', '10', 'M1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('O1', '10', '1', '2025-03-15', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Nur die echte Bestellung O1 (F/NB) zaehlt: 100.
        Assert.Equal(100m, state.SpendChfSample);
    }

    [Fact]
    public async Task LoadAsync_Spend_Includes_All_DocTypes_When_OrdersOnly_Disabled()
    {
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, Bsart, LastLoadedAtUtc) VALUES ('O1', '2025-03-01', 'L1', 'F', 'NB', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, Bsart, LastLoadedAtUtc) VALUES ('O2', '2025-03-01', 'L1', 'A', 'AN', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('O1', '10', 'M1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('O2', '10', 'M1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('O1', '10', '1', '2025-03-15', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), OrdersOnly: false);

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Ohne Belegtyp-Trennung zaehlen beide Belege: 200.
        Assert.Equal(200m, state.SpendChfSample);
    }

    [Fact]
    public async Task LoadAsync_OpenValue_Excludes_EndDelivered_Positions_Elikz_X()
    {
        // M7: endgelieferte Position (Elikz='X') zaehlt trotz offener EKET-Menge nicht als offen.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, LastLoadedAtUtc) VALUES ('F1', '2025-06-01', 'L1', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Elikz, LastLoadedAtUtc) VALUES ('F1', '10', 'M1', '10', '100', '', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Elikz, LastLoadedAtUtc) VALUES ('F1', '20', 'M2', '5', '100', 'X', '2026-01-01');");
        // Beide Positionen haben offene Einteilungen; nur die nicht-endgelieferte (10) darf zaehlen.
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('F1', '10', '1', '2025-08-01', '10', '0', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('F1', '20', '1', '2025-08-01', '5', '0', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.True(state.UsesCache);
        // Position 10: offene Menge 10 * Stueckwert 10 = 100. Position 20 (Elikz X) ausgeschlossen.
        Assert.Equal(100m, state.OpenValueSample);
        Assert.Equal(10m, state.OpenQuantitySample);
    }

    private async Task SeedAsync()
    {
        await ExecuteAsync(
            "INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, LastLoadedAtUtc) VALUES ('E1', '2025-06-01', 'L1', 'Lieferant 1', '2026-01-01');");

        // Aktiv | MARA-MSTAE 99 | MARA-MSTAE 98 | Loekz gesetzt
        await ExecuteAsync(
            "INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Loekz, Mstae, LastLoadedAtUtc) VALUES ('E1', '10', 'M1', '1', '100', '', '', '2026-01-01');");
        await ExecuteAsync(
            "INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Loekz, Mstae, LastLoadedAtUtc) VALUES ('E1', '20', 'M2', '1', '200', '', '99', '2026-01-01');");
        await ExecuteAsync(
            "INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Loekz, Mstae, LastLoadedAtUtc) VALUES ('E1', '30', 'M3', '1', '300', '', '98', '2026-01-01');");
        await ExecuteAsync(
            "INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, Loekz, Mstae, LastLoadedAtUtc) VALUES ('E1', '40', 'M4', '1', '400', 'L', '', '2026-01-01');");

        await ExecuteAsync(
            "INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('E1', '10', '1', '2025-06-15', '1', '0', '2026-01-01');");
    }

    [Fact]
    public async Task LoadAsync_SupplierYearSpendRows_Contain_MaterialGroup_Drilldown()
    {
        // Marco/Armin-Review 2026-07-17: Lieferant aufklappen zeigt Spend je Warengruppe.
        // MaraMatkl (aktueller Materialstamm) gewinnt gegen die Beleg-Warengruppe (Matkl);
        // ohne beide faellt die Zeile in 'ohne Warengruppe'.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('D1', '2025-03-01', 'L1', 'Lieferant Eins', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Matkl, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('D1', '10', 'M1', 'ALT1', 'NEU1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Matkl, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('D1', '20', 'M2', 'ALT2', '', '1', '200', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Matkl, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('D1', '30', 'M3', '', '', '1', '300', '2026-01-01');");
        // Cache-Pfad verlangt Zeilen in allen drei Tabellen (TryLoadCacheStateAsync).
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('D1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        var supplierRow = Assert.Single(state.SupplierYearSpendRows, row => row.Supplier.Contains("Lieferant Eins"));
        Assert.Equal(600m, supplierRow.Total);
        Assert.Equal(3, supplierRow.MaterialGroups.Count);

        // MaraMatkl gewinnt: Position 10 landet unter NEU1, nicht unter ALT1.
        var currentGroup = Assert.Single(supplierRow.MaterialGroups, group => group.MaterialGroup == "NEU1");
        Assert.Equal(100m, currentGroup.Total);
        Assert.Equal(100m, currentGroup.YearValues[2025]);

        var documentGroup = Assert.Single(supplierRow.MaterialGroups, group => group.MaterialGroup == "ALT2");
        Assert.Equal(200m, documentGroup.Total);

        var withoutGroup = Assert.Single(supplierRow.MaterialGroups, group => group.MaterialGroup == "ohne Warengruppe");
        Assert.Equal(300m, withoutGroup.Total);

        // Drilldown-Summe muss exakt der Lieferantenzeile entsprechen (Pivot-Eigenschaft).
        Assert.Equal(supplierRow.Total, supplierRow.MaterialGroups.Sum(group => group.Total));
    }

    [Fact]
    public async Task LoadAsync_MaterialGroupSpendRows_Enriches_Known_Codes_With_T023T_Text()
    {
        // Ingo-Lieferung 2026-07-24 (T023T-Export): bekannte Codes zeigen "Code - Text",
        // unbekannte/noch nicht nachgereichte Codes bleiben roher Code (PurchasingMaterialGroupTextCatalog).
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, LastLoadedAtUtc) VALUES ('G1', '2025-03-01', 'L1', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('G1', '10', 'M1', '20.05.00', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('G1', '20', 'M2', 'ZZ_NEU', '1', '200', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('G1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        var known = Assert.Single(state.MaterialGroupSpendRows, row => row.Label.StartsWith("20.05.00"));
        Assert.Equal("20.05.00 – Bälge", known.Label);
        Assert.Equal(100m, known.Value);

        // Noch nicht in der Referenzliste -> bleibt roher Code, verschwindet nicht.
        var unknown = Assert.Single(state.MaterialGroupSpendRows, row => row.Label == "ZZ_NEU");
        Assert.Equal(200m, unknown.Value);
    }

    [Fact]
    public async Task LoadAsync_MaterialGroup_Drilldown_Respects_SpendPeriodFilter()
    {
        // Zeitraumfilter wirkt auf beide Ebenen: Beleg ausserhalb des Zeitraums fehlt auch im Drilldown.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('F1', '2024-03-01', 'L1', 'Lieferant Eins', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('F2', '2025-03-01', 'L1', 'Lieferant Eins', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('F1', '10', 'M1', 'WG1', '1', '111', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('F2', '10', 'M1', 'WG1', '1', '222', '2026-01-01');");
        // Cache-Pfad verlangt Zeilen in allen drei Tabellen (TryLoadCacheStateAsync).
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('F2', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        var supplierRow = Assert.Single(state.SupplierYearSpendRows, row => row.Supplier.Contains("Lieferant Eins"));
        var group = Assert.Single(supplierRow.MaterialGroups);
        Assert.Equal("WG1", group.MaterialGroup);
        Assert.Equal(222m, group.Total);
        Assert.False(group.YearValues.ContainsKey(2024));
    }

    [Fact]
    public async Task LoadAsync_SpendCascade_Builds_Supplier_Group_Article_Levels_With_PivotTotals()
    {
        // Reiter „Spend-Aufriss": Lieferant -> Warengruppe -> Artikel. MaraMatkl gewinnt gegen die
        // Beleg-Warengruppe; Elternsumme = Summe der Kinder auf jeder Ebene (Pivot-Eigenschaft).
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('C1', '2025-03-01', 'L1', 'Lieferant Eins', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Matkl, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('C1', '10', 'ART-A', 'ALT1', 'NEU1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Matkl, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('C1', '20', 'ART-B', 'ALT1', 'NEU1', '1', '150', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('C1', '30', 'ART-C', 'WG2', '1', '250', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('C1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        var supplier = Assert.Single(state.SpendCascadeRows, node => node.Label.Contains("Lieferant Eins"));
        Assert.Equal(500m, supplier.Total);
        Assert.Equal(500m, supplier.Children.Sum(child => child.Total));

        var groupNeu1 = Assert.Single(supplier.Children, child => child.Label == "NEU1");
        Assert.Equal(250m, groupNeu1.Total);
        Assert.Equal(2, groupNeu1.Children.Count);
        Assert.Equal(250m, groupNeu1.Children.Sum(child => child.Total));
        Assert.Contains(groupNeu1.Children, article => article.Label == "ART-A" && article.Total == 100m);
        Assert.Contains(groupNeu1.Children, article => article.Label == "ART-B" && article.Total == 150m);

        var groupWg2 = Assert.Single(supplier.Children, child => child.Label == "WG2");
        var articleC = Assert.Single(groupWg2.Children);
        Assert.Equal("ART-C", articleC.Label);
        Assert.Equal(250m, articleC.Total);
    }

    [Fact]
    public async Task LoadAsync_SpendCascade_Shows_Material_Text_Next_To_Number()
    {
        // Wunsch Ingo 2026-08-18: Auf der Materialebene soll der Materialtext (MAKT-MAKTX) neben
        // der Nummer stehen. Ohne Text bleibt es bei der Nummer allein - es wird nie ein
        // Platzhalter erfunden. Wichtig: die Summen duerfen sich durch das Label nicht aendern.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('T1', '2025-03-01', 'L1', 'BEPRO AG', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Maktx, Menge, Netwr, LastLoadedAtUtc) VALUES ('T1', '10', 'B64880', 'WG1', 'PCBA HYBRID DENSITY 6.5...20mA 56KG/m3', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('T1', '20', 'B99999', 'WG1', '1', '50', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('T1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        var supplier = Assert.Single(state.SpendCascadeRows, node => node.Label.Contains("BEPRO AG"));
        var group = Assert.Single(supplier.Children);

        var withText = Assert.Single(group.Children, child => child.Label.StartsWith("B64880", StringComparison.Ordinal));
        Assert.Equal("B64880 - PCBA HYBRID DENSITY 6.5...20mA 56KG/m3", withText.Label);
        Assert.Equal(100m, withText.Total);

        // Kein Text im Materialstamm: unveraenderte Anzeige, kein angehaengter Trenner.
        var withoutText = Assert.Single(group.Children, child => child.Label.StartsWith("B99999", StringComparison.Ordinal));
        Assert.Equal("B99999", withoutText.Label);

        // Pivot-Eigenschaft bleibt erhalten, das Label aendert nur die Beschriftung.
        Assert.Equal(150m, supplier.Total);
        Assert.Equal(supplier.Total, group.Children.Sum(child => child.Total));
    }

    [Fact]
    public async Task LoadAsync_SpendMatrix_Article_Rows_Show_Material_Text()
    {
        // Dieselbe Materialebene in der Kaskadierungsmatrix Lieferant/Jahr (dritte Ebene).
        // Beide Sichten muessen dasselbe Label zeigen, sonst wirken sie wie verschiedene Daten.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('T2', '2025-03-01', 'L2', 'BEPRO AG Zwei', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Maktx, Menge, Netwr, LastLoadedAtUtc) VALUES ('T2', '10', 'B64336', 'WG9', 'PCBA NAT TR5 MODUL CURRENT STD COLDB Rei', '1', '200', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('T2', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        var supplierRow = Assert.Single(state.SupplierYearSpendRows, row => row.Supplier.Contains("BEPRO AG Zwei"));
        var groupRow = Assert.Single(supplierRow.MaterialGroups);
        var article = Assert.Single(groupRow.Articles);
        Assert.Equal("B64336 - PCBA NAT TR5 MODUL CURRENT STD COLDB Rei", article.Article);
        Assert.Equal(200m, article.Total);
    }

    [Fact]
    public async Task LoadAsync_SpendCascade_Shows_Every_Article_Without_Truncation()
    {
        // Seit dem 2026-08-27 ist keine Ebene mehr gedeckelt (Wunsch Marco/Ingo): 12 Artikel
        // ergeben 12 Zeilen, keine „uebrige"-Sammelzeile, und die Pivot-Summe bleibt exakt.
        // Vorher waren es 10 einzeln plus eine Restzeile.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('C2', '2025-03-01', 'L1', 'Lieferant Zwei', 'F', '2026-01-01');");
        for (var i = 1; i <= 12; i++)
            await ExecuteAsync($"INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('C2', '{i:00}', 'ART-{i:00}', 'WG', '1', '{i * 10}', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('C2', '01', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        var supplier = Assert.Single(state.SpendCascadeRows, node => node.Label.Contains("Lieferant Zwei"));
        var group = Assert.Single(supplier.Children);
        Assert.Equal(12, group.Children.Count);
        Assert.DoesNotContain(group.Children, child => child.Label.StartsWith("uebrige", StringComparison.Ordinal));

        var expectedTotal = (decimal)Enumerable.Range(1, 12).Sum(i => i * 10);
        Assert.Equal(expectedTotal, group.Total);
        Assert.Equal(group.Total, group.Children.Sum(child => child.Total));
    }

    [Fact]
    public async Task LoadAsync_CurrencySpend_Valued_In_Chf_And_Original_Currency()
    {
        // Marco-Wunsch 2026-07-30: Volumen je Belegwaehrung. Der CHF-Wert nutzt den Belegkurs
        // (Wkurs), die Originalsumme bleibt in der Belegwaehrung. Abgrenzung zur Beschaffungsregion:
        // derselbe Schweizer Lieferant fakturiert hier in EUR (Fall BIPRO aus der Sitzung).
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, Waers, Wkurs, SupplierCountry, LastLoadedAtUtc) VALUES ('W1', '2025-03-01', 'L1', 'Bipro AG', 'F', 'EUR', '2', 'CH', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, Waers, Wkurs, SupplierCountry, LastLoadedAtUtc) VALUES ('W2', '2025-03-01', 'L2', 'Inland AG', 'F', 'CHF', '1', 'CH', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('W1', '10', 'M1', 'WG1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('W2', '10', 'M2', 'WG1', '1', '300', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('W1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var state = await _service.LoadAsync(new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)));

        var eur = Assert.Single(state.CurrencySpendRows, row => row.Currency == "EUR");
        Assert.Equal(200m, eur.ChfValue);      // 100 EUR * Kurs 2
        Assert.Equal(100m, eur.OriginalValue); // Originalsumme bleibt in EUR

        var chf = Assert.Single(state.CurrencySpendRows, row => row.Currency == "CHF");
        Assert.Equal(300m, chf.ChfValue);
        Assert.Equal(300m, chf.OriginalValue);

        // Die Region trennt nicht nach Waehrung: beide Belege liegen in der Region CH.
        var region = Assert.Single(state.RegionSpendRows, row => row.Label == "CH");
        Assert.Equal(500m, region.Value);
    }

    [Fact]
    public async Task LoadAsync_SpendMatrix_Drills_Down_To_Material_Under_MaterialGroup()
    {
        // Entscheid Marco 2026-07-30: in der Matrix „Kaskadierung Lieferant / Jahr" muss die
        // Warengruppe selbst weiter aufklappbar sein, damit man unter "01 - Dummy" die einzelnen
        // Materialnummern sieht. Summe der Materialien = Warengruppensumme.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('D1', '2025-03-01', 'L1', 'Bepro AG', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('D1', '10', 'MAT-123', '01', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('D1', '20', 'MAT-2322', '01', '1', '400', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('D1', '30', 'MAT-999', 'WG2', '1', '250', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('D1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var state = await _service.LoadAsync(new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)));

        var supplier = Assert.Single(state.SupplierYearSpendRows, row => row.Supplier.Contains("Bepro AG"));
        var dummyGroup = Assert.Single(supplier.MaterialGroups, group => group.MaterialGroup.StartsWith("01"));

        Assert.Equal(500m, dummyGroup.Total);
        Assert.Equal(2, dummyGroup.Articles.Count);
        Assert.Equal(dummyGroup.Total, dummyGroup.Articles.Sum(article => article.Total));
        // Absteigend nach Betrag, damit der groesste Brocken beim Aufklappen oben steht.
        Assert.Equal("MAT-2322", dummyGroup.Articles[0].Article);
        Assert.Equal(400m, dummyGroup.Articles[0].Total);
        Assert.Equal(2025, Assert.Single(dummyGroup.Articles[0].YearValues.Keys));

        // Andere Warengruppe desselben Lieferanten bleibt getrennt.
        var otherGroup = Assert.Single(supplier.MaterialGroups, group => group.MaterialGroup == "WG2");
        Assert.Equal("MAT-999", Assert.Single(otherGroup.Articles).Article);
    }

    [Fact]
    public async Task LoadAsync_SpendMatrix_Shows_Every_Material_Without_Truncation()
    {
        // Seit dem 2026-08-27 ohne Deckelung: 27 Materialien ergeben 27 Zeilen und keine
        // Restzeile. Die Jahresspalten muessen dabei genauso aufgehen wie die Gesamtspalte.
        // Vorher waren es 25 einzeln plus „uebrige (2)".
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('D2', '2025-03-01', 'L9', 'Viel AG', 'F', '2026-01-01');");
        for (var i = 1; i <= 27; i++)
            await ExecuteAsync($"INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('D2', '{i:00}', 'ART-{i:00}', 'WG', '1', '{i * 10}', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('D2', '01', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var state = await _service.LoadAsync(new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)));

        var supplier = Assert.Single(state.SupplierYearSpendRows, row => row.Supplier.Contains("Viel AG"));
        var group = Assert.Single(supplier.MaterialGroups);

        Assert.Equal(27, group.Articles.Count);
        Assert.DoesNotContain(group.Articles, article => article.IsRemainder);
        Assert.Equal(group.Total, group.Articles.Sum(article => article.Total));
        Assert.Equal(group.YearValues[2025], group.Articles.Sum(article => article.YearValues[2025]));
    }

    [Fact]
    public async Task LoadAsync_SpendPerspectives_Offer_Selectable_Entry_Dimensions()
    {
        // Marco-Wunsch 2026-07-30 (die am 24.07. offen gelassene Rueckfrage): Einstiegsdimension
        // waehlbar. Sein Beispiel war „nach Beschaffungsregion, dann Lieferant, dann Warengruppen
        // und wieder Material".
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, Waers, Wkurs, SupplierCountry, LastLoadedAtUtc) VALUES ('P1', '2025-03-01', 'L1', 'Lieferant Eins', 'F', 'EUR', '1', 'DE', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, Waers, Wkurs, SupplierCountry, LastLoadedAtUtc) VALUES ('P2', '2025-03-01', 'L2', 'Lieferant Zwei', 'F', 'CHF', '1', 'CH', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('P1', '10', 'M1', 'WG1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('P2', '10', 'M2', 'WG2', '1', '400', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('P1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var state = await _service.LoadAsync(new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)));

        Assert.Equal(
            ["supplier", "region", "materialgroup", "productgroup", "currency", "lzcode", "lzsort"],
            state.SpendPerspectiveRows.Select(perspective => perspective.Key));

        // Region-Perspektive steigt beim Lieferantenland ein und geht vier Ebenen tief.
        var region = Assert.Single(state.SpendPerspectiveRows, perspective => perspective.Key == "region");
        Assert.Equal(
            ["Beschaffungsregion", "Lieferant", "Warengruppe", "Material"],
            region.LevelLabelsDe);
        var germany = Assert.Single(region.Rows, node => node.Label == "DE");
        Assert.Equal(100m, germany.Total);
        var supplierUnderGermany = Assert.Single(germany.Children);
        Assert.Contains("Lieferant Eins", supplierUnderGermany.Label);
        Assert.Equal("WG1", Assert.Single(supplierUnderGermany.Children).Label);
        Assert.Equal("M1", Assert.Single(Assert.Single(supplierUnderGermany.Children).Children).Label);

        // Waehrungs-Perspektive steigt bei der Belegwaehrung ein.
        var currency = Assert.Single(state.SpendPerspectiveRows, perspective => perspective.Key == "currency");
        Assert.Equal(400m, Assert.Single(currency.Rows, node => node.Label == "CHF").Total);

        // Die Lieferanten-Perspektive bleibt der Standardeinstieg und speist die bisherige Anzeige.
        var supplierPerspective = Assert.Single(state.SpendPerspectiveRows, perspective => perspective.Key == "supplier");
        Assert.Equal(
            supplierPerspective.Rows.Select(node => node.Label),
            state.SpendCascadeRows.Select(node => node.Label));
    }

    [Fact]
    public async Task LoadAsync_RegionByMaterialGroup_Splits_Group_By_SupplierCountry()
    {
        // Kuchen je Warengruppe: Anteil je Lieferantenland. Slices summieren zur Gruppensumme.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, SupplierCountry, Bstyp, LastLoadedAtUtc) VALUES ('R1', '2025-03-01', 'L1', 'Lief CH', 'CH', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, SupplierCountry, Bstyp, LastLoadedAtUtc) VALUES ('R2', '2025-03-01', 'L2', 'Lief DE', 'DE', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('R1', '10', 'M1', 'WG1', '1', '300', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('R2', '10', 'M2', 'WG1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('R1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        var group = Assert.Single(state.RegionByMaterialGroupRows, row => row.MaterialGroup == "WG1");
        Assert.Equal(400m, group.Total);
        Assert.Equal(400m, group.Slices.Sum(slice => slice.Value));
        Assert.Contains(group.Slices, slice => slice.Label == "CH" && slice.Value == 300m);
        Assert.Contains(group.Slices, slice => slice.Label == "DE" && slice.Value == 100m);
    }

    [Fact]
    public async Task LoadAsync_AbcXyz_Aggregate_Spend_By_MaraAbc_And_MaraXyz()
    {
        // ABC (MARC-MAABC -> MaraAbc) und XYZ (ZCA_MAT_ABC_XYZ -> MaraXyz); leere Klasse faellt in
        // 'ohne ABC' / 'ohne XYZ'.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, Bstyp, LastLoadedAtUtc) VALUES ('A1', '2025-03-01', 'L1', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraAbc, MaraXyz, Menge, Netwr, LastLoadedAtUtc) VALUES ('A1', '10', 'M1', 'A', 'X', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraAbc, MaraXyz, Menge, Netwr, LastLoadedAtUtc) VALUES ('A1', '20', 'M2', 'A', 'Y', '1', '50', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraAbc, MaraXyz, Menge, Netwr, LastLoadedAtUtc) VALUES ('A1', '30', 'M3', 'B', '', '1', '70', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('A1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        Assert.Equal(150m, Assert.Single(state.AbcSpendRows, row => row.Label == "A").Value);
        Assert.Equal(70m, Assert.Single(state.AbcSpendRows, row => row.Label == "B").Value);
        Assert.Equal(100m, Assert.Single(state.XyzSpendRows, row => row.Label == "X").Value);
        Assert.Equal(70m, Assert.Single(state.XyzSpendRows, row => row.Label == "ohne XYZ").Value);

        var ax = Assert.Single(state.AbcXyzActionRows, row => row.Classification == "AX");
        Assert.Equal(100m, ax.SpendChf);
        Assert.Equal(1, ax.MaterialCount);
        Assert.Equal("High", ax.Severity);
        Assert.Contains("Rahmenvertrag", ax.ActionDe);

        var ay = Assert.Single(state.AbcXyzActionRows, row => row.Classification == "AY");
        Assert.Equal(50m, ay.SpendChf);
        Assert.Contains("Sicherheitsbestand", ay.ActionDe);

        var incomplete = Assert.Single(state.AbcXyzActionRows, row => row.Abc == "B" && row.Xyz == "-");
        Assert.Equal(70m, incomplete.SpendChf);
        Assert.Contains("Stammdaten", incomplete.ActionDe);
    }

    [Fact]
    public async Task LoadAsync_ProductGroupPerspective_Allocates_MultiUse_Material_Without_DoubleCounting()
    {
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('PG1', '2025-03-01', 'L1', 'Lieferant Eins', 'F', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('PG1', '10', '000M1', '1', '120', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('PG1', '20', 'M2', '1', '80', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('PG1', '30', 'M3', '1', '50', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, Menge, Netwr, LastLoadedAtUtc) VALUES ('PG1', '40', 'M4', '1', '40', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('PG1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");

        await ExecuteAsync("INSERT INTO MaterialUsageCache (Richtung, Vknr, VknrDispo, Kompnr, LastLoadedAtUtc) VALUES ('V', 'V1', '001', 'M1', '2026-01-01');");
        await ExecuteAsync("INSERT INTO MaterialUsageCache (Richtung, Vknr, VknrDispo, Kompnr, LastLoadedAtUtc) VALUES ('V', 'V2', '002', 'M1', '2026-01-01');");
        await ExecuteAsync("INSERT INTO MaterialUsageCache (Richtung, Vknr, VknrDispo, Kompnr, LastLoadedAtUtc) VALUES ('V', 'V3', '001', 'M2', '2026-01-01');");
        await ExecuteAsync("INSERT INTO MaterialUsageCache (Richtung, Vknr, VknrDispo, Kompnr, LastLoadedAtUtc) VALUES ('V', 'V4', 'EL7', 'M4', '2026-01-01');");
        // Alte manuelle Zuordnungen sind nur noch Legacy-Daten und duerfen die SAP-Regeln
        // nicht uebersteuern.
        await ExecuteAsync("INSERT INTO PurchasingProductGroupMap (Disponent, ProductGroup, ProductGroupText, UpdatedAtUtc) VALUES ('001', 'ALT', 'Legacy darf nicht fuehren', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingSpendDisponentRule (DisponentPattern, ProductGroup, ProductGroupText, Source, UpdatedAtUtc) VALUES ('001', 'XLS', 'Excel darf nicht fuehren', 'zdispo_grp.xlsx', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingSpendDisponentRule (DisponentPattern, ProductGroup, ProductGroupText, Source, UpdatedAtUtc) VALUES ('001', 'PG-A', 'Sensorik', 'SAP OData', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingSpendDisponentRule (DisponentPattern, ProductGroup, ProductGroupText, Source, UpdatedAtUtc) VALUES ('002', 'PG-B', 'Zubehoer', 'SAP OData', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingSpendDisponentRule (DisponentPattern, ProductGroup, ProductGroupText, UpdatedAtUtc) VALUES ('EL*', 'T2', 'FP_TRANSM. TX', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingSpendDisponentRule (DisponentPattern, ProductGroup, ProductGroupText, UpdatedAtUtc) VALUES ('EL*', 'T3', 'Zweite Produktgruppe', '2026-01-01');");

        var state = await _service.LoadAsync(new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)));

        Assert.False(state.Message.StartsWith("SAP Einkauf konnte nicht geladen werden", StringComparison.Ordinal), state.Message);
        var perspective = Assert.Single(state.SpendPerspectiveRows, row => row.Key == "productgroup");
        Assert.Equal(290m, perspective.Rows.Sum(row => row.Total));
        Assert.Equal(140m, Assert.Single(perspective.Rows, row => row.Label == "PG-A - Sensorik").Total);
        Assert.Equal(60m, Assert.Single(perspective.Rows, row => row.Label == "PG-B - Zubehoer").Total);
        Assert.Equal(20m, Assert.Single(perspective.Rows, row => row.Label == "T2 - FP_TRANSM. TX").Total);
        Assert.Equal(20m, Assert.Single(perspective.Rows, row => row.Label == "T3 - Zweite Produktgruppe").Total);
        Assert.DoesNotContain(perspective.Rows, row => row.Label.Contains("Legacy", StringComparison.Ordinal));
        Assert.DoesNotContain(perspective.Rows, row => row.Label.Contains("Excel", StringComparison.Ordinal));
        Assert.Equal(50m, Assert.Single(perspective.Rows, row => row.Label == "ohne Produktgruppe").Total);

        Assert.Equal(240m, state.ProductGroupAllocation.AssignedSpendChf);
        Assert.Equal(50m, state.ProductGroupAllocation.UnassignedSpendChf);
        Assert.Equal(160m, state.ProductGroupAllocation.MultiGroupSpendChf);
        Assert.Equal(3, state.ProductGroupAllocation.AssignedMaterialCount);
        Assert.Equal(1, state.ProductGroupAllocation.UnassignedMaterialCount);
        Assert.Equal(2, state.ProductGroupAllocation.MultiGroupMaterialCount);
        Assert.Equal(3, state.ProductGroupAllocation.MappedDispatcherCount);
        Assert.Equal(0, state.ProductGroupAllocation.UnmappedDispatcherCount);
    }

    private void CreatePurchasingCacheTables()
    {
        ExecuteSync(@"
CREATE TABLE PurchasingEkkoCache (
    Ebeln TEXT NOT NULL PRIMARY KEY,
    Bedat TEXT NULL,
    Aedat TEXT NULL,
    Lifnr TEXT NOT NULL DEFAULT '',
    SupplierName TEXT NOT NULL DEFAULT '',
    SupplierCountry TEXT NOT NULL DEFAULT '',
    Bukrs TEXT NOT NULL DEFAULT '',
    Bstyp TEXT NOT NULL DEFAULT '',
    Bsart TEXT NOT NULL DEFAULT '',
    Konnr TEXT NOT NULL DEFAULT '',
    Waers TEXT NOT NULL DEFAULT '',
    Wkurs TEXT NOT NULL DEFAULT '0',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL
);");
        ExecuteSync(@"
CREATE TABLE PurchasingEkpoCache (
    Ebeln TEXT NOT NULL,
    Ebelp TEXT NOT NULL,
    Matnr TEXT NOT NULL DEFAULT '',
    Txz01 TEXT NOT NULL DEFAULT '',
    Matkl TEXT NOT NULL DEFAULT '',
    MaraMatkl TEXT NOT NULL DEFAULT '',
    MaraAbc TEXT NOT NULL DEFAULT '',
    MaraXyz TEXT NOT NULL DEFAULT '',
    Maktx TEXT NOT NULL DEFAULT '',
    Menge TEXT NOT NULL DEFAULT '0',
    Meins TEXT NOT NULL DEFAULT '',
    Netwr TEXT NOT NULL DEFAULT '0',
    Loekz TEXT NOT NULL DEFAULT '',
    Mstae TEXT NOT NULL DEFAULT '',
    Elikz TEXT NOT NULL DEFAULT '',
    Ktmng TEXT NOT NULL DEFAULT '0',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Ebeln, Ebelp)
);");
        ExecuteSync(@"
CREATE TABLE PurchasingEketCache (
    Ebeln TEXT NOT NULL,
    Ebelp TEXT NOT NULL,
    Etenr TEXT NOT NULL,
    Eindt TEXT NULL,
    Menge TEXT NOT NULL DEFAULT '0',
    Wemng TEXT NOT NULL DEFAULT '0',
    RawJson TEXT NOT NULL DEFAULT '',
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Ebeln, Ebelp, Etenr)
);");
        ExecuteSync(@"
CREATE TABLE PurchasingSyncState (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Mode TEXT NOT NULL DEFAULT '',
    Status TEXT NOT NULL DEFAULT '',
    StartedAtUtc TEXT NULL,
    CompletedAtUtc TEXT NULL,
    FromDate TEXT NULL,
    ToDate TEXT NULL,
    LastSuccessfulDeltaAtUtc TEXT NULL,
    EkkoRows INTEGER NOT NULL DEFAULT 0,
    EkpoRows INTEGER NOT NULL DEFAULT 0,
    EketRows INTEGER NOT NULL DEFAULT 0,
    Message TEXT NOT NULL DEFAULT ''
);");
        ExecuteSync(@"
CREATE TABLE MaterialUsageCache (
    Richtung TEXT NOT NULL,
    Vknr TEXT NOT NULL,
    VknrDispo TEXT NOT NULL DEFAULT '',
    Kompnr TEXT NOT NULL,
    LastLoadedAtUtc TEXT NOT NULL,
    PRIMARY KEY (Richtung, Vknr, Kompnr)
);");
        ExecuteSync(@"
CREATE TABLE PurchasingProductGroupMap (
    Disponent TEXT NOT NULL PRIMARY KEY,
    ProductGroup TEXT NOT NULL DEFAULT '',
    ProductGroupText TEXT NOT NULL DEFAULT '',
    Source TEXT NOT NULL DEFAULT 'ZC23',
    UpdatedAtUtc TEXT NOT NULL DEFAULT ''
);");
        ExecuteSync(@"
CREATE TABLE PurchasingSpendDisponentRule (
    DisponentPattern TEXT NOT NULL,
    ProductGroup TEXT NOT NULL DEFAULT '',
    ProductGroupText TEXT NOT NULL DEFAULT '',
    Source TEXT NOT NULL DEFAULT 'SAP OData',
    UpdatedAtUtc TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (DisponentPattern, ProductGroup)
);");
    }

    private static readonly PurchasingDashboardFilter Year2025 = new(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

    private Task InsertContractAsync(
        string ebeln, string bsart = "MK", string bukrs = "1100", string waers = "CHF", string wkurs = "1",
        string? kdate = null, string loekz = "", string ktmng = "0", string abmng = "0", string netpr = "0", string peinh = "1",
        string lifnr = "L1", string supplierName = "Lieferant 1")
        => ExecuteAsync(
            "INSERT INTO PurchasingContractCache (Ebeln, Ebelp, Bukrs, Bsart, Lifnr, SupplierName, Matnr, Txz01, Waers, Wkurs, Kdate, Loekz, Meins, Ktmng, Netpr, Peinh, Abmng, LastLoadedAtUtc) " +
            $"VALUES ('{ebeln}', '10', '{bukrs}', '{bsart}', '{lifnr}', '{supplierName}', 'M-{ebeln}', 'Text {ebeln}', '{waers}', '{wkurs}', {(kdate is null ? "NULL" : $"'{kdate}'")}, '{loekz}', 'ST', '{ktmng}', '{netpr}', '{peinh}', '{abmng}', '2026-10-08T06:00:00.0000000Z');");

    [Fact]
    public async Task QuantityContract_OpenValue_Includes_Expired_Contracts_And_Reports_Them_Separately()
    {
        // Wunsch Armin 2026-10-08 (wie ME3L): Zielmenge minus Abruf, mal Preis/Preiseinheit, in CHF,
        // abgelaufene Kontrakte zaehlen mit. EKKO/EKPO/EKET-Caches sind hier bewusst leer: die
        // Kontraktkennzahl haengt nicht an ihnen.
        ExecuteSync(DatabaseSchemaSql.GetPurchasingContractCacheCreateSql());
        // EUR mit Kurs 0.9: (1000 - 400) * 10 = 6000 EUR = 5400 CHF, laeuft noch.
        await InsertContractAsync("K1", waers: "EUR", wkurs: "0.9", kdate: "2099-12-31", ktmng: "1000", abmng: "400", netpr: "10");
        // Ueberabgerufen: offene Menge nie negativ.
        await InsertContractAsync("K2", ktmng: "100", abmng: "150", netpr: "10");
        // Preis je 100 Stueck, abgelaufen: 200 * (500 / 100) = 1000 CHF.
        await InsertContractAsync("K3", kdate: "2020-01-31", ktmng: "200", netpr: "500", peinh: "100", lifnr: "L2", supplierName: "Lieferant 2");
        // Nicht eingerechnet: Wertkontrakt, geloescht, anderer Buchungskreis.
        await InsertContractAsync("K4", bsart: "WK", ktmng: "10", netpr: "10");
        await InsertContractAsync("K5", loekz: "L", ktmng: "10", netpr: "10");
        await InsertContractAsync("K6", bukrs: "1200", ktmng: "10", netpr: "10");

        var state = await _service.LoadAsync(Year2025);

        Assert.True(state.ContractDataAvailable);
        Assert.Equal(6400m, state.QuantityContractOpenValueChf);
        Assert.Equal(1000m, state.QuantityContractExpiredValueChf);
        Assert.Equal(2, state.QuantityContractCount);
        Assert.Equal(2, state.QuantityContractItemCount);
        Assert.Equal(1, state.QuantityContractExpiredCount);
        Assert.Equal(1, state.QuantityContractOtherDocTypeItemCount);
        Assert.Equal(1, state.QuantityContractOtherCompanyItemCount);
        Assert.Equal(["K1", "K3"], state.QuantityContractTopRows.Select(row => row.Ebeln));
        Assert.True(state.QuantityContractTopRows[1].IsExpired);
        Assert.Equal("Lieferant 1", state.QuantityContractSupplierRows[0].Label);
        Assert.Equal(5400m, state.QuantityContractSupplierRows[0].Value);
        // Die Abrufbestellungs-Kennzahl bleibt davon unberuehrt.
        Assert.Equal(0m, state.ContractValueSample);
    }

    [Fact]
    public async Task QuantityContract_Is_Reported_As_Unavailable_Not_As_Zero_Without_Data()
    {
        // Tabelle fehlt (aeltere Datenbank): kein Fehler, nur "nicht verfuegbar".
        var withoutTable = await _service.LoadAsync(Year2025);
        Assert.False(withoutTable.ContractDataAvailable);

        // Tabelle da, aber leer (EinkKontraktSet noch nicht in P76): ebenfalls nicht verfuegbar, keine 0.
        ExecuteSync(DatabaseSchemaSql.GetPurchasingContractCacheCreateSql());
        var empty = await _service.LoadAsync(Year2025);
        Assert.False(empty.ContractDataAvailable);
        Assert.Equal(0m, empty.QuantityContractOpenValueChf);
    }

    private async Task SeedLzSpendAsync()
    {
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, Waers, Wkurs, LastLoadedAtUtc) VALUES ('Z1', '2025-03-01', 'L1', 'Lieferant Eins', 'F', 'CHF', '1', '2026-01-01');");
        // M1 hat fuehrende Nullen im Beleg, der LZ-Cache fuehrt die normalisierte Nummer.
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('Z1', '10', '000000000000000M1', 'WG1', '1', '100', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('Z1', '20', 'M2', 'WG1', '1', '200', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('Z1', '30', 'M3', 'WG2', '1', '300', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('Z1', '40', '', 'WG2', '1', '400', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('Z1', '10', '1', '2025-04-01', '1', '1', '2026-01-01');");
    }

    [Fact]
    public async Task LzPerspectives_Group_By_Code_With_Ohne_Code_And_Keep_The_Total()
    {
        ExecuteSync(DatabaseSchemaSql.GetPurchasingMaterialLzCacheCreateSql());
        await SeedLzSpendAsync();
        await ExecuteAsync("INSERT INTO PurchasingMaterialLzCache (Matnr, Lzcode, Lzsort, LastLoadedAtUtc) VALUES ('M1', 'A1', 'S1', '2026-10-08');");
        await ExecuteAsync("INSERT INTO PurchasingMaterialLzCache (Matnr, Lzcode, Lzsort, LastLoadedAtUtc) VALUES ('M2', 'A1', '', '2026-10-08');");

        var state = await _service.LoadAsync(Year2025);

        var supplierTotal = Assert.Single(state.SpendPerspectiveRows, row => row.Key == "supplier").Rows.Sum(row => row.Total);
        Assert.Equal(1000m, supplierTotal);

        var lz = Assert.Single(state.SpendPerspectiveRows, row => row.Key == "lzcode");
        Assert.Equal(["Lebenszyklus-Code", "Lieferant", "Material"], lz.LevelLabelsDe);
        Assert.Equal(supplierTotal, lz.Rows.Sum(row => row.Total));
        Assert.Equal(["ohne Code", "A1"], lz.Rows.Select(row => row.Label));
        Assert.Equal(700m, lz.Rows[0].Total);
        Assert.Equal(300m, lz.Rows[1].Total);
        // Drill-down bis zum Material.
        Assert.Contains(lz.Rows[1].Children.Single().Children, child => child.Label.Contains("M1"));

        var sort = Assert.Single(state.SpendPerspectiveRows, row => row.Key == "lzsort");
        Assert.Equal(supplierTotal, sort.Rows.Sum(row => row.Total));
        Assert.Equal(["ohne Code", "S1"], sort.Rows.Select(row => row.Label));
        Assert.Equal(100m, sort.Rows[1].Total);

        Assert.Equal(["ohne Code", "A1"], state.LzCodeSpendRows.Select(row => row.Label));
        Assert.Equal(supplierTotal, state.LzCodeSpendRows.Sum(row => row.Value));
        Assert.Equal(supplierTotal, state.LzSortSpendRows.Sum(row => row.Value));
    }

    [Fact]
    public async Task LzPerspectives_Fall_Back_To_Ohne_Code_When_The_Cache_Table_Is_Missing()
    {
        await SeedLzSpendAsync();

        var state = await _service.LoadAsync(Year2025);

        var lz = Assert.Single(state.SpendPerspectiveRows, row => row.Key == "lzcode");
        var node = Assert.Single(lz.Rows);
        Assert.Equal("ohne Code", node.Label);
        Assert.Equal(1000m, node.Total);
        Assert.Equal("ohne Code", Assert.Single(state.LzCodeSpendRows).Label);
    }

    private void ExecuteSync(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }
}
