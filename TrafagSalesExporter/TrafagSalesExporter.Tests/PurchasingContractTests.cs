using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Offener Mengenkontraktwert (wie ME3L): Rechenregeln, Parsen der SAP-Zeilen und Toleranz gegen
/// ein noch nicht vorhandenes SAP-Set (404). Der Dashboard-Teil liegt in PurchasingDashboardServiceTests.
/// </summary>
public class PurchasingContractTests : IDisposable
{
    private static readonly DateTime Today = new(2026, 10, 8);
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public PurchasingContractTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        Execute(DatabaseSchemaSql.GetPurchasingContractCacheCreateSql());
        Execute(DatabaseSchemaSql.GetPurchasingMaterialLzCacheCreateSql());
    }

    public void Dispose() => _connection.Dispose();

    private static PurchasingContractItem Item(
        decimal target = 100m, decimal called = 0m, decimal netPrice = 10m, decimal priceUnit = 1m,
        string currency = "CHF", decimal rate = 1m, DateTime? validTo = null,
        string bsart = "MK", string bukrs = "1100", string deletion = "")
        => new("4600000001", "10", bukrs, bsart, "L1", "Lieferant", "M1", "Text", "ST", currency, rate,
            validTo, deletion, target, netPrice, priceUnit, called);

    // ---------- Rechenregeln ----------

    [Theory]
    [InlineData(100, 40, 60)]
    [InlineData(100, 100, 0)]
    [InlineData(100, 150, 0)] // ueberabgerufen: nie negativ
    [InlineData(0, 0, 0)]
    public void OpenQuantity_Is_Target_Minus_CalledOff_And_Never_Negative(decimal target, decimal called, decimal expected)
        => Assert.Equal(expected, PurchasingContractCalculator.OpenQuantity(target, called));

    [Theory]
    [InlineData(500, 100, 5)]
    [InlineData(10, 1, 10)]
    [InlineData(10, 0, 10)]   // Preiseinheit 0 gilt als 1
    [InlineData(10, -5, 10)]  // unsinnige Preiseinheit ebenfalls
    public void UnitPrice_Divides_By_PriceUnit_And_Treats_Zero_As_One(decimal netPrice, decimal priceUnit, decimal expected)
        => Assert.Equal(expected, PurchasingContractCalculator.UnitPrice(netPrice, priceUnit));

    [Fact]
    public void ToChf_Follows_The_Dashboard_Rule_For_Positive_Negative_And_Missing_Rates()
    {
        // CHF und leere Waehrung unveraendert, auch ohne Kurs.
        Assert.Equal(100m, PurchasingContractCalculator.ToChf(100m, "CHF", 0m, out var missingChf));
        Assert.False(missingChf);
        Assert.Equal(100m, PurchasingContractCalculator.ToChf(100m, "", 0m, out var missingEmpty));
        Assert.False(missingEmpty);
        // Positiver Kurs multipliziert (direkte Notierung).
        Assert.Equal(90m, PurchasingContractCalculator.ToChf(100m, "EUR", 0.9m, out var missingPositive));
        Assert.False(missingPositive);
        // Negativer Kurs teilt durch den Absolutwert (indirekte Notierung).
        Assert.Equal(50m, PurchasingContractCalculator.ToChf(100m, "EUR", -2m, out var missingNegative));
        Assert.False(missingNegative);
        // Fremdwaehrung ohne Kurs: 1:1 und als nicht belastbar gemeldet.
        Assert.Equal(100m, PurchasingContractCalculator.ToChf(100m, "USD", 0m, out var missingRate));
        Assert.True(missingRate);
    }

    [Fact]
    public void Calculate_Uses_Open_Quantity_Price_Unit_And_Currency()
    {
        // (1000 - 400) * (500 / 100) = 3000 EUR, Kurs 0.95 -> 2850 CHF.
        var row = PurchasingContractCalculator.Calculate(
            Item(target: 1000m, called: 400m, netPrice: 500m, priceUnit: 100m, currency: "EUR", rate: 0.95m), Today);

        Assert.Equal(600m, row.OpenQuantity);
        Assert.Equal(3000m, row.OpenValueOriginal);
        Assert.Equal(2850m, row.OpenValueChf);
        Assert.Equal("EUR", row.Currency);
        Assert.False(row.MissingRate);
    }

    [Fact]
    public void Calculate_Does_Not_Flag_A_Missing_Rate_When_Nothing_Is_Open()
    {
        var row = PurchasingContractCalculator.Calculate(Item(target: 10m, called: 10m, currency: "USD", rate: 0m), Today);

        Assert.Equal(0m, row.OpenValueChf);
        Assert.False(row.MissingRate);
    }

    [Theory]
    [InlineData("2026-10-07", true)]   // gestern
    [InlineData("2026-10-08", false)]  // heute laeuft noch
    [InlineData("2099-12-31", false)]
    public void IsExpired_Means_Validity_End_Before_Today(string validTo, bool expected)
        => Assert.Equal(expected, PurchasingContractCalculator.IsExpired(DateTime.Parse(validTo), Today));

    [Fact]
    public void IsExpired_Is_False_Without_A_Validity_End()
        => Assert.False(PurchasingContractCalculator.IsExpired(null, Today));

    [Fact]
    public void Evaluate_Includes_Expired_Contracts_Splits_Them_And_Skips_Foreign_Types_Companies_And_Deleted_Items()
    {
        var items = new[]
        {
            Item(target: 100m, called: 20m, netPrice: 10m, validTo: new DateTime(2027, 1, 1)),                 // 800 offen
            Item(target: 100m, called: 0m, netPrice: 10m, validTo: new DateTime(2025, 1, 1)),                  // 1000 abgelaufen
            Item(target: 100m, called: 100m, netPrice: 10m),                                                   // nichts offen
            Item(bsart: "WK"), Item(bukrs: "1200"), Item(deletion: "L")                                        // ausgeschlossen
        };

        var result = PurchasingContractCalculator.Evaluate(items, Today);

        Assert.Equal(1800m, result.Summary.TotalChf);
        Assert.Equal(1000m, result.Summary.ExpiredChf);
        Assert.Equal(2, result.Summary.ItemCount);
        Assert.Equal(1, result.Summary.ExpiredItemCount);
        Assert.Equal(1, result.OtherDocTypeItemCount);
        Assert.Equal(1, result.OtherCompanyItemCount);
        Assert.Equal(3, result.Rows.Count);
    }

    [Theory]
    [InlineData("4080.000", 4080)]
    [InlineData("0.900", 0.9)]
    [InlineData("1.075-", -1.075)]
    [InlineData("-2.5", -2.5)]
    [InlineData("", 0)]
    [InlineData("abc", 0)]
    public void ParseSapDecimal_Handles_Point_Trailing_Minus_And_Garbage(string text, decimal expected)
        => Assert.Equal(expected, PurchasingContractCalculator.ParseSapDecimal(text));

    [Theory]
    [InlineData("20261231", "2026-12-31")]
    [InlineData("2026-12-31", "2026-12-31")]
    public void ParseSapDate_Reads_Both_Formats(string text, string expected)
        => Assert.Equal(DateTime.Parse(expected), PurchasingContractCalculator.ParseSapDate(text));

    [Theory]
    [InlineData("")]
    [InlineData("00000000")]
    [InlineData(null)]
    public void ParseSapDate_Treats_Empty_And_Zero_Dates_As_No_Date_So_They_Are_Not_Expired(string? text)
        => Assert.Null(PurchasingContractCalculator.ParseSapDate(text));

    // ---------- Parsen und Schreiben ----------

    [Fact]
    public void ParseContractRow_Normalizes_Numbers_Dates_And_Rejects_Rows_Without_Key()
    {
        var parsed = PurchasingContractLoader.ParseContractRow(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ebeln"] = "4600000001", ["Ebelp"] = "00010", ["Bukrs"] = "1100", ["Bsart"] = "MK", ["Lifnr"] = "L1",
            ["Waers"] = "EUR", ["Wkurs"] = "1.075-", ["Kdatb"] = "20240101", ["Kdate"] = "00000000",
            ["Ktmng"] = "4080.000", ["Netpr"] = "12.50", ["Peinh"] = "", ["Abmng"] = "100.000", ["Abwrt"] = "1250.00"
        });

        Assert.NotNull(parsed);
        Assert.Equal("-1.075", parsed!.Wkurs);
        Assert.Equal("2024-01-01", parsed.Kdatb);
        Assert.Null(parsed.Kdate);
        Assert.Equal("4080.000", parsed.Ktmng);
        Assert.Equal("0", parsed.Peinh);
        Assert.Null(PurchasingContractLoader.ParseContractRow(new Dictionary<string, object?> { ["Ebeln"] = "4600000001" }));
    }

    [Theory]
    [InlineData("000000000000012345", "12345")]
    [InlineData(" ab-12 ", "AB-12")]
    [InlineData("0000", "0")]
    [InlineData("", "")]
    public void NormalizeMaterialKey_Matches_The_Sql_Normalization(string input, string expected)
        => Assert.Equal(expected, PurchasingContractLoader.NormalizeMaterialKey(input));

    [Fact]
    public void ParseLzRows_Keeps_Only_Materials_With_A_Code_And_Normalizes_The_Key()
    {
        var rows = new[]
        {
            new Dictionary<string, object?> { ["Matnr"] = "000000000000000100", ["Lzcode"] = "A1", ["Lzsort"] = "" },
            new Dictionary<string, object?> { ["Matnr"] = "200", ["Lzcode"] = "", ["Lzsort"] = "S9" },
            new Dictionary<string, object?> { ["Matnr"] = "300", ["Lzcode"] = "", ["Lzsort"] = "" },
            new Dictionary<string, object?> { ["Matnr"] = "", ["Lzcode"] = "A1", ["Lzsort"] = "" }
        };

        var parsed = PurchasingContractLoader.ParseLzRows(rows);

        Assert.Equal(2, parsed.Count);
        Assert.Contains(parsed, row => row.Matnr == "100" && row.Lzcode == "A1");
        Assert.Contains(parsed, row => row.Matnr == "200" && row.Lzsort == "S9");
    }

    // ---------- Toleranz gegen ein fehlendes SAP-Set ----------

    [Fact]
    public async Task TryRead_Turns_A_404_Into_A_Warning_Instead_Of_Throwing()
    {
        SapEntitySetReader reader = (_, _, _, _, _) =>
            throw new HttpRequestException("SAP OData EinkKontraktSet fehlgeschlagen (404 Not Found)");

        var result = await PurchasingContractLoader.TryReadAsync(reader, "EinkKontraktSet", "Ebeln", "Ebeln", CancellationToken.None);

        Assert.False(result.Available);
        Assert.Empty(result.Rows);
        Assert.Contains("404", result.Warning);
        Assert.Contains("unveraendert", result.Warning);
    }

    [Fact]
    public async Task TryRead_Treats_An_Empty_Set_As_Not_Available_So_The_Old_Cache_Stays()
    {
        SapEntitySetReader reader = (_, _, _, _, _) => Task.FromResult(new List<Dictionary<string, object?>>());

        var result = await PurchasingContractLoader.TryReadAsync(reader, "EinkKontraktSet", "Ebeln", "Ebeln", CancellationToken.None);

        Assert.False(result.Available);
    }

    [Fact]
    public async Task TryRead_Does_Not_Swallow_The_Cancellation_Of_The_Run_Itself()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        SapEntitySetReader reader = (_, _, _, _, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new List<Dictionary<string, object?>>());
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => PurchasingContractLoader.TryReadAsync(reader, "EinkKontraktSet", "Ebeln", "Ebeln", cts.Token));
    }

    [Fact]
    public async Task Refresh_With_404_Keeps_The_Previous_Contract_And_Lz_Cache_And_Does_Not_Throw()
    {
        Execute("INSERT INTO PurchasingContractCache (Ebeln, Ebelp, Bsart, Ktmng, Netpr, LastLoadedAtUtc) VALUES ('K-ALT', '10', 'MK', '5', '5', '2026-10-01');");
        Execute("INSERT INTO PurchasingMaterialLzCache (Matnr, Lzcode, Lzsort, LastLoadedAtUtc) VALUES ('ALT', 'A1', 'S1', '2026-10-01');");
        var log = new RecordingLog();
        var service = CreateService(log);
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("not found") }));

        var message = await service.RefreshContractsAndLzSafeAsync(
            client, "http://sap.invalid/", new Dictionary<string, PurchasingDataRefreshService.SupplierInfo>(), CancellationToken.None);

        Assert.Contains("nicht verfuegbar", message);
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM PurchasingContractCache;"));
        Assert.Equal("K-ALT", ScalarText("SELECT Ebeln FROM PurchasingContractCache;"));
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM PurchasingMaterialLzCache;"));
        Assert.Contains(log.Entries, entry => entry.Level == "Warning");
    }

    [Fact]
    public async Task Refresh_Replaces_Both_Caches_When_The_Sets_Deliver()
    {
        Execute("INSERT INTO PurchasingContractCache (Ebeln, Ebelp, Bsart, Ktmng, Netpr, LastLoadedAtUtc) VALUES ('K-ALT', '10', 'MK', '5', '5', '2026-10-01');");
        var log = new RecordingLog();
        var service = CreateService(log);
        using var client = new HttpClient(new StubHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            var json = path.EndsWith("EinkKontraktSet", StringComparison.Ordinal)
                ? """{"d":{"results":[{"Ebeln":"4600000009","Ebelp":"10","Bukrs":"1100","Bsart":"MK","Lifnr":"0000100200","Waers":"EUR","Wkurs":"0.95","Kdate":"20201231","Ktmng":"1000.000","Netpr":"10.00","Peinh":"1","Abmng":"250.000"}]}}"""
                : """{"d":{"results":[{"Matnr":"000000000000000077","Lzcode":"A2","Lzsort":"S3"}]}}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }));
        var suppliers = new Dictionary<string, PurchasingDataRefreshService.SupplierInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["100200"] = new PurchasingDataRefreshService.SupplierInfo("Beispiel AG", "DE")
        };

        var message = await service.RefreshContractsAndLzSafeAsync(client, "http://sap.invalid/", suppliers, CancellationToken.None);

        Assert.Contains("Kontraktpositionen=1", message);
        Assert.Contains("LZ-Codes=1", message);
        Assert.Equal("4600000009", ScalarText("SELECT Ebeln FROM PurchasingContractCache;"));
        Assert.Equal("2020-12-31", ScalarText("SELECT Kdate FROM PurchasingContractCache;"));
        Assert.Equal("Beispiel AG", ScalarText("SELECT SupplierName FROM PurchasingContractCache;"));
        Assert.Equal("77", ScalarText("SELECT Matnr FROM PurchasingMaterialLzCache;"));
        Assert.Equal("A2", ScalarText("SELECT Lzcode FROM PurchasingMaterialLzCache;"));
    }

    // ---------- Hilfen ----------

    private PurchasingDataRefreshService CreateService(IAppEventLogService log)
        => new(new TestFactory(_options), log, null!);

    private void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private long Scalar(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private string ScalarText(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(command.ExecuteScalar()) ?? string.Empty;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }

    private sealed class RecordingLog : IAppEventLogService
    {
        public List<(string Category, string Message, string Level)> Entries { get; } = [];

        public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
        {
            Entries.Add((category, message, level));
            return Task.CompletedTask;
        }

        public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
            => Task.CompletedTask;
    }

    private sealed class TestFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(options));
    }
}
