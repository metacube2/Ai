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
    public async Task LoadAsync_SpendCascade_Caps_Article_Level_With_Remainder_Row()
    {
        // Artikelebene ist auf 10 gedeckelt: 12 Artikel -> 10 einzeln + 1 „uebrige (2)"-Zeile,
        // Pivot-Summe bleibt trotz Deckelung exakt erhalten.
        await ExecuteAsync("INSERT INTO PurchasingEkkoCache (Ebeln, Bedat, Lifnr, SupplierName, Bstyp, LastLoadedAtUtc) VALUES ('C2', '2025-03-01', 'L1', 'Lieferant Zwei', 'F', '2026-01-01');");
        for (var i = 1; i <= 12; i++)
            await ExecuteAsync($"INSERT INTO PurchasingEkpoCache (Ebeln, Ebelp, Matnr, MaraMatkl, Menge, Netwr, LastLoadedAtUtc) VALUES ('C2', '{i:00}', 'ART-{i:00}', 'WG', '1', '{i * 10}', '2026-01-01');");
        await ExecuteAsync("INSERT INTO PurchasingEketCache (Ebeln, Ebelp, Etenr, Eindt, Menge, Wemng, LastLoadedAtUtc) VALUES ('C2', '01', '1', '2025-04-01', '1', '1', '2026-01-01');");

        var filter = new PurchasingDashboardFilter(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var state = await _service.LoadAsync(filter);

        var supplier = Assert.Single(state.SpendCascadeRows, node => node.Label.Contains("Lieferant Zwei"));
        var group = Assert.Single(supplier.Children);
        Assert.Equal(11, group.Children.Count);

        var expectedTotal = (decimal)Enumerable.Range(1, 12).Sum(i => i * 10);
        Assert.Equal(expectedTotal, group.Total);
        Assert.Equal(group.Total, group.Children.Sum(child => child.Total));

        var remainder = Assert.Single(group.Children, child => child.Label == "uebrige (2)");
        Assert.Empty(remainder.Children);
        Assert.Equal(30m, remainder.Total);
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
