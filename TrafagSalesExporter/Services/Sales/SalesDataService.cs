using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Liefert die Verkaufszeilen fuer den Reiter Verkauf (2026-10-02) aus derselben Quelle und mit denselben Regeln wie
/// Finance: <see cref="ICentralSalesDataProvider"/> (verarbeitete CSV je Standort), <see cref="FinanceRuleEngine"/> fuer
/// Einschluss, Nettoumsatz und Finance-Datum, Konzernkunden ueber die Intercompany-Regeln ausgeschlossen.
/// CHF zum Kurs des Finance-Datums der Zeile (wie die zentrale Auswertung, nicht der 31.12.-Schalter des Cockpits);
/// fehlt ein Kurs, faellt die Zeile weg und wird gezaehlt. Leere Sparten werden ueber die Materialnummer aus anderen
/// Zeilen mit Sparte ergaenzt. Gemerkt, bis sich der Quellstand aendert, hoechstens 2 Stunden.
/// Doku docs/VERKAUF_2026-10-02.md.
/// </summary>
public sealed class SalesDataService
{
    private readonly ICentralSalesDataProvider _provider;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ICurrencyExchangeRateService _rates;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SalesDataset? _cache;
    private string? _stamp;

    public SalesDataService(ICentralSalesDataProvider provider, IDbContextFactory<AppDbContext> dbFactory, ICurrencyExchangeRateService rates)
    {
        _provider = provider;
        _dbFactory = dbFactory;
        _rates = rates;
    }

    public async Task<SalesDataset> GetAsync()
    {
        var stamp = await _provider.GetSourceStampAsync();
        var current = _cache;
        // Ohne Quellstempel (null) gilt allein die Frist von 2 Stunden; mit Stempel zusaetzlich: Stempel unveraendert.
        if (current is { Error: null } && IsFresh(current) && (stamp is null || stamp == _stamp))
            return current;
        // Neuer Quellstand: bisherigen Stand sofort zeigen, neu im Hintergrund rechnen (wie Einkauf, PM-04).
        if (current is { Error: null })
        {
            _ = RefreshAsync(stamp);
            return current;
        }
        return await RefreshAsync(stamp);
    }

    private static bool IsFresh(SalesDataset d) => d.LoadedAt > DateTime.Now.AddHours(-2);

    private async Task<SalesDataset> RefreshAsync(string? stamp)
    {
        if (!await _gate.WaitAsync(0))
        {
            // Ein Ladevorgang laeuft schon; auf ihn warten und dessen Ergebnis nehmen.
            await _gate.WaitAsync();
            _gate.Release();
            // Fehlerergebnisse werden nie gemerkt (_cache wird nur bei Erfolg gesetzt): der naechste Aufruf versucht es neu.
            return _cache ?? new SalesDataset { LoadedAt = DateTime.Now, Error = "Laden fehlgeschlagen." };
        }
        try
        {
            if (_cache is { Error: null } c && IsFresh(c) && (stamp is null || stamp == _stamp))
                return c;
            var loaded = await Task.Run(LoadAsync);
            _cache = loaded;
            _stamp = stamp;
            return loaded;
        }
        catch (Exception ex)
        {
            return _cache ?? new SalesDataset { LoadedAt = DateTime.Now, Error = ex.Message };
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SalesDataset> LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rules = await db.FinanceRules.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.SortOrder).ThenBy(r => r.Id).ToListAsync();
        if (rules.Count == 0)
            rules = FinanceRuleEngine.CreateDefaultRules().ToList();
        var intercompany = await db.FinanceIntercompanyRules.AsNoTracking().Where(r => r.IsActive).ToListAsync();
        var engine = new FinanceRuleEngine(rules);
        var records = await _provider.GetRecordsAsync();

        var divisionByMaterial = records
            .Where(r => !string.IsNullOrWhiteSpace(r.ProductDivisionText) && !string.IsNullOrWhiteSpace(r.Material))
            .GroupBy(r => MaterialKeyNormalizer.Normalize(r.Material))
            .ToDictionary(g => g.Key, g => g.GroupBy(r => r.ProductDivisionText.Trim()).OrderByDescending(d => d.Count()).First().Key);

        var facts = new List<SalesFact>(records.Count);
        var missingRate = 0;
        var intercompanyRows = 0;
        foreach (var r in records)
        {
            var countryKey = ManagementCockpitService.ResolveFinanceCountryKey(r.Land, r.Tsc);
            var include = engine.ShouldInclude(r, countryKey);
            var value = engine.ResolveNetSalesActual(r, countryKey, include);
            if (!include || value == 0m)
                continue;
            if (ManagementCockpitService.IsIntercompanyCustomer(r, intercompany))
            {
                intercompanyRows++;
                continue;
            }
            var date = engine.ResolveFinanceDate(r, countryKey);
            var rate = _rates.ResolveRate(ManagementCockpitService.ResolveFinanceCurrency(r), "CHF", date);
            if (rate is null)
            {
                missingRate++;
                continue;
            }
            var division = r.ProductDivisionText?.Trim() ?? "";
            if (division.Length == 0 && !string.IsNullOrWhiteSpace(r.Material))
                division = divisionByMaterial.GetValueOrDefault(MaterialKeyNormalizer.Normalize(r.Material)) ?? "";
            var country = SalesAnalytics.NormalizeCountry(r.CustomerCountry);
            facts.Add(new SalesFact(DateOnly.FromDateTime(date), r.Tsc.Trim().ToUpperInvariant(), countryKey,
                SalesAnalytics.CustomerKey(r.CustomerName, r.Tsc, r.CustomerNumber), (r.CustomerName ?? "").Trim(),
                country.Length == 2 && country.All(char.IsLetter) ? country : "",
                (r.Material ?? "").Trim(), (r.Name ?? "").Trim(), division.Length == 0 ? "Nicht zugeordnet" : division,
                Math.Round(value * rate.Value, 2), r.Quantity, r.InvoiceNumber ?? "",
                value, ManagementCockpitService.ResolveFinanceCurrency(r)));
        }

        var referenceEnd = SalesAnalytics.ReferenceEnd(facts, DateOnly.FromDateTime(DateTime.Today));
        var dataStart = SalesAnalytics.DataStart(facts);
        return new SalesDataset
        {
            LoadedAt = DateTime.Now,
            Facts = facts,
            ReferenceEnd = referenceEnd,
            DataStart = dataStart,
            CompareMonths = SalesAnalytics.CompareMonths(dataStart, referenceEnd),
            MissingRateRows = missingRate,
            IntercompanyRowsExcluded = intercompanyRows
        };
    }
}
