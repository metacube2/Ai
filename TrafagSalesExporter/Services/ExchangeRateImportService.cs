using System.Globalization;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public class ExchangeRateImportService : IExchangeRateImportService
{
    private const string EcbXmlUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
    private const string EcbSourceNote = "ECB daily reference rate";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ExchangeRateImportService(IHttpClientFactory httpClientFactory, IDbContextFactory<AppDbContext> dbFactory)
    {
        _httpClientFactory = httpClientFactory;
        _dbFactory = dbFactory;
    }

    public async Task<ExchangeRateImportResult> RefreshEcbRatesAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(nameof(ExchangeRateImportService));
        using var response = await client.GetAsync(EcbXmlUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = XDocument.Parse(xml);

        var rateEntries = ParseRates(document);
        if (rateEntries.Count == 0)
            throw new InvalidOperationException("ECB response did not contain any exchange rates.");

        var rateDate = rateEntries[0].RateDate;

        using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var existingRates = await db.CurrencyExchangeRates
            .Where(x => x.Notes == EcbSourceNote && x.ValidFrom == rateDate)
            .ToListAsync(cancellationToken);

        if (existingRates.Count > 0)
            db.CurrencyExchangeRates.RemoveRange(existingRates);

        db.CurrencyExchangeRates.AddRange(rateEntries.Select(entry => new CurrencyExchangeRate
        {
            FromCurrency = "EUR",
            ToCurrency = entry.Currency,
            Rate = entry.Rate,
            ValidFrom = entry.RateDate,
            ValidTo = null,
            Notes = EcbSourceNote,
            IsActive = true
        }));

        await db.SaveChangesAsync(cancellationToken);

        return new ExchangeRateImportResult
        {
            ImportedCount = rateEntries.Count,
            RateDate = rateDate,
            SourceName = "ECB"
        };
    }

    private static List<EcbRateEntry> ParseRates(XDocument document)
    {
        var cubes = document
            .Descendants()
            .Where(x => x.Name.LocalName == "Cube")
            .ToList();

        var datedCube = cubes.FirstOrDefault(x => x.Attribute("time") is not null)
            ?? throw new InvalidOperationException("ECB response did not contain a dated rate section.");

        var dateText = datedCube.Attribute("time")?.Value
            ?? throw new InvalidOperationException("ECB rate date is missing.");

        var rateDate = DateTime.ParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        return datedCube.Elements()
            .Where(x => x.Name.LocalName == "Cube")
            .Select(x => new EcbRateEntry(
                Currency: (x.Attribute("currency")?.Value ?? string.Empty).Trim().ToUpperInvariant(),
                Rate: decimal.Parse(x.Attribute("rate")?.Value ?? "0", CultureInfo.InvariantCulture),
                RateDate: rateDate))
            .Where(x => !string.IsNullOrWhiteSpace(x.Currency) && x.Rate > 0m)
            .ToList();
    }

    private sealed record EcbRateEntry(string Currency, decimal Rate, DateTime RateDate);
}
