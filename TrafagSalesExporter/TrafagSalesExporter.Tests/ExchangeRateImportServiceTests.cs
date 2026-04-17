using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class ExchangeRateImportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _dbFactory;

    public ExchangeRateImportServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();

        _dbFactory = new TestDbContextFactory(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task RefreshEcbRatesAsync_Imports_Rates_And_Replaces_Previous_Ecb_Rows_For_Same_Day()
    {
        const string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01" xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <Cube time="2026-04-17">
                  <Cube currency="USD" rate="1.1200" />
                  <Cube currency="CHF" rate="0.9700" />
                </Cube>
              </Cube>
            </gesmes:Envelope>
            """;

        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            db.CurrencyExchangeRates.Add(new()
            {
                FromCurrency = "EUR",
                ToCurrency = "USD",
                Rate = 1.10m,
                ValidFrom = new DateTime(2026, 4, 17),
                Notes = "ECB daily reference rate",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var service = new ExchangeRateImportService(
            new FakeHttpClientFactory(xml),
            _dbFactory);

        var result = await service.RefreshEcbRatesAsync();

        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(new DateTime(2026, 4, 17), result.RateDate);
        Assert.Equal("ECB", result.SourceName);

        await using var verifyDb = await _dbFactory.CreateDbContextAsync();
        var rates = await verifyDb.CurrencyExchangeRates
            .OrderBy(x => x.ToCurrency)
            .ToListAsync();

        Assert.Equal(2, rates.Count);
        Assert.Collection(rates,
            chf =>
            {
                Assert.Equal("EUR", chf.FromCurrency);
                Assert.Equal("CHF", chf.ToCurrency);
                Assert.Equal(0.97m, chf.Rate);
            },
            usd =>
            {
                Assert.Equal("EUR", usd.FromCurrency);
                Assert.Equal("USD", usd.ToCurrency);
                Assert.Equal(1.12m, usd.Rate);
            });
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

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly string _xml;

        public FakeHttpClientFactory(string xml)
        {
            _xml = xml;
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new FakeHttpMessageHandler(_xml));
        }
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _xml;

        public FakeHttpMessageHandler(string xml)
        {
            _xml = xml;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_xml, Encoding.UTF8, "application/xml")
            });
        }
    }
}
