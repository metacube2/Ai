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

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }
}
