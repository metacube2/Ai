using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>Ein Kunde mit seinem Umsatzgewicht und der aktuellen Segmentzuordnung.</summary>
public sealed record MarketSegmentCustomerRow(
    string Tsc,
    string CustomerNumber,
    string CustomerName,
    string CustomerCountry,
    int SalesRows,
    decimal SalesValue,
    string Currency,
    int ProductFamilies,
    string Segment,
    string Source);

public interface IMarketSegmentPageService
{
    /// <summary>
    /// Kunden nach Umsatzgewicht, optional gefiltert. Sortiert nach Zeilenzahl, weil die
    /// Zuordnung dort am meisten bewirkt.
    /// </summary>
    Task<List<MarketSegmentCustomerRow>> SearchCustomersAsync(
        string? nameFilter, string? tscFilter, bool onlyAssigned, int take = 200);

    /// <summary>Setzt oder ersetzt die Zuordnung eines Kunden.</summary>
    Task AssignAsync(string tsc, string customerNumber, string customerName, string segment, string source);

    /// <summary>Entfernt die Zuordnung. Die Zeile verschwindet, statt ein leeres Segment zu tragen.</summary>
    Task ClearAsync(string tsc, string customerNumber);

    /// <summary>Bereits gepflegte Zuordnungen, je Segment gezaehlt.</summary>
    Task<List<(string Segment, int Customers, int SalesRows)>> GetSummaryAsync();

    /// <summary>Segmente, die schon verwendet werden, plus die vorgeschlagenen Standardwerte.</summary>
    Task<List<string>> GetKnownSegmentsAsync();
}

/// <summary>
/// Pflege der Kunden-Segment-Zuordnung fuer die Weboberflaeche.
///
/// Bewusst dieselbe Datengrundlage wie das zentrale Excel: die Kundenliste kommt aus
/// <see cref="AppDbContext.CentralSalesRecords"/>, damit der Pflegende genau die Kunden
/// sieht, die auch im Export erscheinen. Die Aufloesung selbst bleibt in
/// <see cref="MarketSegmentResolver"/>; dieser Dienst kennt nur Lesen und Schreiben.
/// </summary>
public sealed class MarketSegmentPageService : IMarketSegmentPageService
{
    /// <summary>Vorschlagswerte fuer das Auswahlfeld. Frei ergaenzbar, keine harte Schranke.</summary>
    public static readonly string[] DefaultSegments =
        ["Railway", "Ship Building", "Hydrogen", "Industrial", "Energy", "Mobile Hydraulics"];

    /// <summary>
    /// Standortkuerzel fuer den Filter. Bewusst hier und nicht in der Razor-Datei: der
    /// Uebersetzungstest scannt `Components/*.razor` nach benachbarten Zeichenkettenpaaren
    /// und wuerde `"TRCH", "TRAT"` faelschlich als uebersetzungspflichtigen Text lesen.
    /// </summary>
    public static readonly string[] Sites =
        ["TRCH", "TRAT", "TRDE", "TRES", "TRFR", "TRIN", "TRIT", "TRUK", "TRUS"];

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppEventLogService _log;

    public MarketSegmentPageService(IDbContextFactory<AppDbContext> dbFactory, IAppEventLogService log)
    {
        _dbFactory = dbFactory;
        _log = log;
    }

    public async Task<List<MarketSegmentCustomerRow>> SearchCustomersAsync(
        string? nameFilter, string? tscFilter, bool onlyAssigned, int take = 200)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var assignments = await db.CustomerMarketSegments.AsNoTracking().ToListAsync();
        var lookup = MarketSegmentResolver.BuildLookup(assignments);

        var query = db.CentralSalesRecords.AsNoTracking()
            .Where(row => row.CustomerNumber != null && row.CustomerNumber.Trim() != "");

        var name = nameFilter?.Trim();
        if (!string.IsNullOrEmpty(name))
            query = query.Where(row => row.CustomerName != null && EF.Functions.Like(row.CustomerName, $"%{name}%"));

        var tsc = tscFilter?.Trim();
        if (!string.IsNullOrEmpty(tsc))
            query = query.Where(row => row.Tsc == tsc);

        // Gruppierung in der Datenbank, damit bei knapp 100'000 Zeilen nicht alles in den
        // Speicher gezogen wird. StandardCost und SalesPriceValue sind TEXT-Spalten; der
        // Umsatz wird deshalb bewusst in der Anwendung summiert, nicht per SQL-SUM.
        var grouped = await query
            .GroupBy(row => new { row.Tsc, row.CustomerNumber })
            .Select(g => new
            {
                g.Key.Tsc,
                g.Key.CustomerNumber,
                CustomerName = g.Max(x => x.CustomerName) ?? string.Empty,
                CustomerCountry = g.Max(x => x.CustomerCountry) ?? string.Empty,
                SalesRows = g.Count(),
                // SQLite kann kein SUM ueber decimal, der Provider wirft dort
                // NotSupportedException. Die Summe dient hier nur der Priorisierung in der
                // Liste und wird gerundet angezeigt, deshalb ist double ausreichend genau.
                // Fuer Finanzwerte gilt weiterhin der Weg ueber die Finance-Spalten.
                SalesValue = g.Sum(x => (double)x.SalesPriceValue),
                Currency = g.Max(x => x.SalesCurrency) ?? string.Empty,
                Families = g.Select(x => x.ProductFamilyText).Distinct().Count()
            })
            .OrderByDescending(x => x.SalesRows)
            .Take(onlyAssigned ? 2000 : take)
            .ToListAsync();

        var rows = grouped.Select(x =>
        {
            var (segment, source) = MarketSegmentResolver.Resolve(x.Tsc, x.CustomerNumber, lookup);
            return new MarketSegmentCustomerRow(
                x.Tsc,
                (x.CustomerNumber ?? string.Empty).Trim(),
                x.CustomerName.Trim(),
                x.CustomerCountry.Trim(),
                x.SalesRows,
                (decimal)x.SalesValue,
                x.Currency.Trim(),
                x.Families,
                segment,
                source);
        });

        if (onlyAssigned)
            rows = rows.Where(r => r.Segment.Length > 0).Take(take);

        return rows.ToList();
    }

    public async Task AssignAsync(string tsc, string customerNumber, string customerName, string segment, string source)
    {
        var normalizedTsc = MarketSegmentResolver.NormalizeTsc(tsc);
        var normalizedCustomer = MarketSegmentResolver.NormalizeCustomerNumber(customerNumber);
        var normalizedSegment = (segment ?? string.Empty).Trim();

        if (normalizedTsc.Length == 0 || normalizedCustomer.Length == 0)
            throw new InvalidOperationException("Standort und Kundennummer sind Pflicht.");
        if (normalizedSegment.Length == 0)
            throw new InvalidOperationException("Ohne Segment bitte die Zuordnung entfernen statt sie zu leeren.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.CustomerMarketSegments
            .FirstOrDefaultAsync(x => x.Tsc == normalizedTsc && x.CustomerNumber == normalizedCustomer);

        var previous = existing?.Segment ?? string.Empty;
        if (existing is null)
        {
            db.CustomerMarketSegments.Add(new CustomerMarketSegment
            {
                Tsc = normalizedTsc,
                CustomerNumber = normalizedCustomer,
                CustomerName = (customerName ?? string.Empty).Trim(),
                Segment = normalizedSegment,
                Source = (source ?? string.Empty).Trim(),
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.CustomerName = (customerName ?? string.Empty).Trim();
            existing.Segment = normalizedSegment;
            existing.Source = (source ?? string.Empty).Trim();
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        // Nachvollziehbarkeit: die Zuordnung veraendert eine ausgewiesene Kennzahl, deshalb
        // gehoert jede Aenderung ins Protokoll.
        await _log.WriteAsync(
            "Marktsegment",
            existing is null ? "Segment zugeordnet" : "Segment geaendert",
            details: $"{normalizedTsc}/{normalizedCustomer} | {previous} -> {normalizedSegment} | Quelle={source}");
    }

    public async Task ClearAsync(string tsc, string customerNumber)
    {
        var normalizedTsc = MarketSegmentResolver.NormalizeTsc(tsc);
        var normalizedCustomer = MarketSegmentResolver.NormalizeCustomerNumber(customerNumber);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.CustomerMarketSegments
            .FirstOrDefaultAsync(x => x.Tsc == normalizedTsc && x.CustomerNumber == normalizedCustomer);
        if (existing is null) return;

        var previous = existing.Segment;
        db.CustomerMarketSegments.Remove(existing);
        await db.SaveChangesAsync();

        await _log.WriteAsync(
            "Marktsegment",
            "Segment entfernt",
            details: $"{normalizedTsc}/{normalizedCustomer} | war {previous}");
    }

    public async Task<List<(string Segment, int Customers, int SalesRows)>> GetSummaryAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var assignments = await db.CustomerMarketSegments.AsNoTracking().ToListAsync();
        if (assignments.Count == 0) return [];

        var keys = assignments
            .Select(a => new
            {
                Tsc = MarketSegmentResolver.NormalizeTsc(a.Tsc),
                Customer = MarketSegmentResolver.NormalizeCustomerNumber(a.CustomerNumber),
                a.Segment
            })
            .ToList();

        var counts = await db.CentralSalesRecords.AsNoTracking()
            .Where(row => row.CustomerNumber != null && row.CustomerNumber.Trim() != "")
            .GroupBy(row => new { row.Tsc, row.CustomerNumber })
            .Select(g => new { g.Key.Tsc, g.Key.CustomerNumber, Rows = g.Count() })
            .ToListAsync();

        var rowsByKey = counts.ToDictionary(
            c => (MarketSegmentResolver.NormalizeTsc(c.Tsc),
                  MarketSegmentResolver.NormalizeCustomerNumber(c.CustomerNumber)),
            c => c.Rows);

        return keys
            .GroupBy(k => k.Segment, StringComparer.OrdinalIgnoreCase)
            .Select(g => (
                Segment: g.Key,
                Customers: g.Count(),
                SalesRows: g.Sum(k => rowsByKey.GetValueOrDefault((k.Tsc, k.Customer)))))
            .OrderByDescending(x => x.SalesRows)
            .ToList();
    }

    public async Task<List<string>> GetKnownSegmentsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var used = await db.CustomerMarketSegments.AsNoTracking()
            .Select(x => x.Segment)
            .Distinct()
            .ToListAsync();

        return used
            .Concat(DefaultSegments)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
