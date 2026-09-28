using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class CurrencyExchangeRateServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;
    private readonly CurrencyExchangeRateService _service;

    public CurrencyExchangeRateServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        _dbFactory = new TestDbContextFactory(options);
        _service = new CurrencyExchangeRateService(_dbFactory);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task ResolveRate_Returns_Direct_Rate_For_Valid_Date()
    {
        await SeedRatesAsync(new CurrencyExchangeRate
        {
            FromCurrency = "USD",
            ToCurrency = "EUR",
            Rate = 0.92m,
            ValidFrom = new DateTime(2026, 1, 1),
            ValidTo = null,
            IsActive = true
        });

        var rate = _service.ResolveRate("USD", "EUR", new DateTime(2026, 4, 1));

        Assert.Equal(0.92m, rate);
    }

    [Fact]
    public async Task ResolveRate_Uses_Inverse_Rate_When_Only_Reverse_Rate_Exists()
    {
        await SeedRatesAsync(new CurrencyExchangeRate
        {
            FromCurrency = "EUR",
            ToCurrency = "CHF",
            Rate = 1.10m,
            ValidFrom = new DateTime(2026, 1, 1),
            ValidTo = null,
            IsActive = true
        });

        var rate = _service.ResolveRate("CHF", "EUR", new DateTime(2026, 4, 1));

        Assert.NotNull(rate);
        Assert.Equal(1m / 1.10m, rate!.Value, 6);
    }

    [Fact]
    public async Task ResolveRate_Uses_Eur_Cross_Rate_When_No_Direct_Rate_Exists()
    {
        await SeedRatesAsync(
            new CurrencyExchangeRate
            {
                FromCurrency = "CHF",
                ToCurrency = "EUR",
                Rate = 0.95m,
                ValidFrom = new DateTime(2026, 1, 1),
                ValidTo = null,
                IsActive = true
            },
            new CurrencyExchangeRate
            {
                FromCurrency = "EUR",
                ToCurrency = "USD",
                Rate = 1.08m,
                ValidFrom = new DateTime(2026, 1, 1),
                ValidTo = null,
                IsActive = true
            });

        var rate = _service.ResolveRate("CHF", "USD", new DateTime(2026, 4, 1));

        Assert.NotNull(rate);
        Assert.Equal(1.026m, rate!.Value, 6);
    }

    [Theory]
    [InlineData("$", "USD")]
    [InlineData("US$", "USD")]
    [InlineData("EUR", "EUR")]
    [InlineData("sfr", "CHF")]
    [InlineData("cad", "CAD")]
    [InlineData("xyz", "XYZ")]
    public void NormalizeCurrencyCode_Normalizes_Known_And_Unknown_Codes(string input, string expected)
    {
        var normalized = _service.NormalizeCurrencyCode(input);

        Assert.Equal(expected, normalized);
    }

    // ---- Speicherstand (2026-09-28): Kurse nicht mehr je Aufruf per SQL ----

    [Fact]
    public async Task ResolveRate_Liest_Neue_Kurse_Sofort_Nach_NotifyRatesChanged()
    {
        await SeedRatesAsync(Rate("USD", "EUR", 0.92m, new DateTime(2026, 1, 1)));
        Assert.Equal(0.92m, _service.ResolveRate("USD", "EUR", new DateTime(2026, 4, 1)));

        await SeedRatesAsync(Rate("USD", "EUR", 0.95m, new DateTime(2026, 1, 1)));
        CurrencyExchangeRateService.NotifyRatesChanged();

        Assert.Equal(0.95m, _service.ResolveRate("USD", "EUR", new DateTime(2026, 4, 1)));
    }

    [Fact]
    public async Task ResolveRate_Nimmt_Den_Juengsten_Gueltigen_Kurs_Und_Beachtet_ValidTo()
    {
        await SeedRatesAsync(
            Rate("USD", "EUR", 0.90m, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)),
            Rate("USD", "EUR", 0.92m, new DateTime(2026, 1, 1)),
            Rate("USD", "EUR", 0.99m, new DateTime(2027, 1, 1)));
        CurrencyExchangeRateService.NotifyRatesChanged();

        Assert.Equal(0.90m, _service.ResolveRate("USD", "EUR", new DateTime(2025, 6, 30)));
        Assert.Equal(0.92m, _service.ResolveRate("usd", "eur", new DateTime(2026, 6, 30)));
        Assert.Null(_service.ResolveRate("USD", "EUR", new DateTime(2024, 6, 30)));
    }

    [Fact]
    public async Task ResolveRate_Fragt_Die_Datenbank_Nicht_Mehr_Je_Aufruf_Ab()
    {
        await SeedRatesAsync(Rate("USD", "EUR", 0.92m, new DateTime(2026, 1, 1)));
        CurrencyExchangeRateService.NotifyRatesChanged();
        _service.ResolveRate("USD", "EUR", new DateTime(2026, 4, 1));
        var contextsBefore = _dbFactory.CreatedContexts;

        for (var i = 0; i < 1000; i++)
            _service.ResolveRate(i % 2 == 0 ? "USD" : "EUR", i % 2 == 0 ? "EUR" : "USD", new DateTime(2026, 1, 1).AddDays(i % 200));

        Assert.Equal(contextsBefore, _dbFactory.CreatedContexts);
    }

    private static CurrencyExchangeRate Rate(string from, string to, decimal rate, DateTime validFrom, DateTime? validTo = null)
        => new() { FromCurrency = from, ToCurrency = to, Rate = rate, ValidFrom = validFrom, ValidTo = validTo, IsActive = true };

    private async Task SeedRatesAsync(params CurrencyExchangeRate[] rates)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.CurrencyExchangeRates.RemoveRange(db.CurrencyExchangeRates);
        await db.SaveChangesAsync();
        db.CurrencyExchangeRates.AddRange(rates);
        await db.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public int CreatedContexts { get; private set; }

        public AppDbContext CreateDbContext()
        {
            CreatedContexts++;
            return new(_options);
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
