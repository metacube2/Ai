using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

public class CurrencyExchangeRateService : ICurrencyExchangeRateService
{
    private static readonly Dictionary<string, string> BuiltInCurrencyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["$"] = "USD",
        ["US$"] = "USD",
        ["USD"] = "USD",
        ["€"] = "EUR",
        ["EUR"] = "EUR",
        ["CHF"] = "CHF",
        ["SFR"] = "CHF",
        ["INR"] = "INR",
        ["RS"] = "INR",
        ["GBP"] = "GBP",
        ["CAD"] = "CAD"
    };

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CurrencyExchangeRateService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public decimal? ResolveRate(string fromCurrency, string toCurrency, DateTime? effectiveDate)
    {
        var normalizedFrom = NormalizeCurrencyCode(fromCurrency);
        var normalizedTo = NormalizeCurrencyCode(toCurrency);
        if (string.IsNullOrWhiteSpace(normalizedFrom) || string.IsNullOrWhiteSpace(normalizedTo))
            return null;

        if (string.Equals(normalizedFrom, normalizedTo, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var date = (effectiveDate ?? DateTime.UtcNow).Date;

        using var db = _dbFactory.CreateDbContext();
        var directRate = db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(x => x.IsActive
                && x.FromCurrency.ToUpper() == normalizedFrom
                && x.ToCurrency.ToUpper() == normalizedTo
                && x.ValidFrom.Date <= date
                && (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= date))
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefault();

        if (directRate is not null)
            return directRate.Rate;

        var inverseRate = db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(x => x.IsActive
                && x.FromCurrency.ToUpper() == normalizedTo
                && x.ToCurrency.ToUpper() == normalizedFrom
                && x.ValidFrom.Date <= date
                && (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= date))
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefault();

        if (inverseRate is not null && inverseRate.Rate != 0m)
            return 1m / inverseRate.Rate;

        var fromToEur = ResolveDirectOrInverseRate(db, normalizedFrom, "EUR", date);
        var eurToTarget = ResolveDirectOrInverseRate(db, "EUR", normalizedTo, date);
        if (fromToEur.HasValue && eurToTarget.HasValue)
            return fromToEur.Value * eurToTarget.Value;

        return null;
    }

    public string NormalizeCurrencyCode(string? currencyCode)
    {
        var normalized = currencyCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        return BuiltInCurrencyAliases.TryGetValue(normalized, out var mapped)
            ? mapped
            : normalized.ToUpperInvariant();
    }

    private static decimal? ResolveDirectOrInverseRate(AppDbContext db, string fromCurrency, string toCurrency, DateTime date)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var directRate = db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(x => x.IsActive
                && x.FromCurrency.ToUpper() == fromCurrency
                && x.ToCurrency.ToUpper() == toCurrency
                && x.ValidFrom.Date <= date
                && (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= date))
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefault();

        if (directRate is not null)
            return directRate.Rate;

        var inverseRate = db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(x => x.IsActive
                && x.FromCurrency.ToUpper() == toCurrency
                && x.ToCurrency.ToUpper() == fromCurrency
                && x.ValidFrom.Date <= date
                && (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= date))
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefault();

        if (inverseRate is not null && inverseRate.Rate != 0m)
            return 1m / inverseRate.Rate;

        return null;
    }
}
