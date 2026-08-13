using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>Welche Kunden die Pflegeliste zeigen soll.</summary>
public static class MarketSegmentFilterModes
{
    /// <summary>Alle Kunden nach Umsatzgewicht, unabhaengig von der Zuordnung.</summary>
    public const string All = "all";

    /// <summary>Nur maschinelle Vorschlaege, die noch niemand bestaetigt hat.</summary>
    public const string Proposals = "proposals";

    /// <summary>Nur fachlich bestaetigte Zuordnungen. Nur diese wirken im Export.</summary>
    public const string Confirmed = "confirmed";
}

/// <summary>Ein Kunde mit Umsatzgewicht und aktuellem Zuordnungsstand.</summary>
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
    string Source,
    bool IsConfirmed,
    string ProposalNote);

/// <summary>Eine Zeile der Ergebnissicht: Segment je Land und Waehrung.</summary>
public sealed record MarketSegmentResultRow(
    string Segment,
    string Tsc,
    string Currency,
    int Customers,
    int SalesRows,
    decimal SalesValue);

/// <summary>Wie weit die Pflege insgesamt ist.</summary>
public sealed record MarketSegmentProgress(
    int ConfirmedCustomers,
    int ProposalCustomers,
    int ConfirmedSalesRows,
    int ProposalSalesRows,
    int TotalSalesRows);

public interface IMarketSegmentPageService
{
    Task<List<MarketSegmentCustomerRow>> SearchCustomersAsync(
        string? nameFilter, string? tscFilter, string filterMode, int take = 200);

    Task AssignAsync(
        string tsc, string customerNumber, string customerName, string segment, string source,
        bool isConfirmed = true);

    /// <summary>Bestaetigt einen Vorschlag, ohne Segment oder Quelle zu aendern.</summary>
    Task ConfirmAsync(string tsc, string customerNumber);

    Task ClearAsync(string tsc, string customerNumber);

    /// <summary>Ergebnissicht: welcher Umsatz haengt je Segment, Land und Waehrung daran.</summary>
    Task<List<MarketSegmentResultRow>> GetResultAsync(bool confirmedOnly = true);

    Task<MarketSegmentProgress> GetProgressAsync();

    Task<List<string>> GetKnownSegmentsAsync();
}

/// <summary>
/// Pflege und Auswertung der Kunden-Segment-Zuordnung fuer die Weboberflaeche.
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
        string? nameFilter, string? tscFilter, string filterMode, int take = 200)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var assignments = await db.CustomerMarketSegments.AsNoTracking().ToListAsync();
        var lookup = MarketSegmentResolver.BuildLookup(assignments, confirmedOnly: false);

        var query = db.CentralSalesRecords.AsNoTracking()
            .Where(row => row.CustomerNumber != null && row.CustomerNumber.Trim() != "");

        var name = nameFilter?.Trim();
        if (!string.IsNullOrEmpty(name))
            query = query.Where(row => row.CustomerName != null && EF.Functions.Like(row.CustomerName, $"%{name}%"));

        var tsc = tscFilter?.Trim();
        if (!string.IsNullOrEmpty(tsc))
            query = query.Where(row => row.Tsc == tsc);

        // Bei den beiden Zuordnungsmodi wird die Menge VOR dem Kappen auf die betroffenen
        // Kunden eingeschraenkt. Frueher wurden erst die obersten 2'000 nach Zeilenzahl
        // geholt und danach gefiltert; ein zugeordneter kleiner Kunde von rund 4'900 fiel
        // dabei still aus der Liste. Genau das sah wie ein leerer Schalter aus.
        var wantsAssigned = filterMode is MarketSegmentFilterModes.Proposals or MarketSegmentFilterModes.Confirmed;
        if (wantsAssigned)
        {
            var keys = assignments
                .Where(a => filterMode == MarketSegmentFilterModes.Confirmed ? a.IsConfirmed : !a.IsConfirmed)
                .Select(a => new
                {
                    Tsc = MarketSegmentResolver.NormalizeTsc(a.Tsc),
                    Customer = MarketSegmentResolver.NormalizeCustomerNumber(a.CustomerNumber)
                })
                .ToList();

            if (keys.Count == 0) return [];

            var tscKeys = keys.Select(k => k.Tsc).Distinct().ToList();
            var customerKeys = keys.Select(k => k.Customer).Distinct().ToList();
            // Grobfilter in der Datenbank, exakte Paarpruefung danach in der Anwendung:
            // SQLite kann keinen Tupel-IN-Vergleich, und zwei getrennte IN-Listen wuerden
            // sonst falsche Kombinationen durchlassen.
            query = query.Where(row =>
                tscKeys.Contains(row.Tsc) && customerKeys.Contains(row.CustomerNumber!.Trim()));
        }

        // Gruppierung in der Datenbank, damit bei knapp 100'000 Zeilen nicht alles in den
        // Speicher gezogen wird.
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
                // NotSupportedException. Die Summe dient nur der Priorisierung und wird
                // gerundet angezeigt, deshalb ist double ausreichend genau. Fuer
                // Finanzwerte gilt weiterhin der Weg ueber die Finance-Spalten.
                SalesValue = g.Sum(x => (double)x.SalesPriceValue),
                Currency = g.Max(x => x.SalesCurrency) ?? string.Empty,
                Families = g.Select(x => x.ProductFamilyText).Distinct().Count()
            })
            .OrderByDescending(x => x.SalesRows)
            .Take(wantsAssigned ? 4000 : take)
            .ToListAsync();

        var rows = grouped
            .Select(x =>
            {
                var key = (MarketSegmentResolver.NormalizeTsc(x.Tsc),
                           MarketSegmentResolver.NormalizeCustomerNumber(x.CustomerNumber));
                lookup.TryGetValue(key, out var hit);
                return new MarketSegmentCustomerRow(
                    x.Tsc,
                    (x.CustomerNumber ?? string.Empty).Trim(),
                    x.CustomerName.Trim(),
                    x.CustomerCountry.Trim(),
                    x.SalesRows,
                    (decimal)x.SalesValue,
                    x.Currency.Trim(),
                    x.Families,
                    hit?.Segment.Trim() ?? string.Empty,
                    hit?.Source.Trim() ?? string.Empty,
                    hit?.IsConfirmed ?? false,
                    hit?.ProposalNote.Trim() ?? string.Empty);
            })
            .Where(r => filterMode switch
            {
                MarketSegmentFilterModes.Confirmed => r.Segment.Length > 0 && r.IsConfirmed,
                MarketSegmentFilterModes.Proposals => r.Segment.Length > 0 && !r.IsConfirmed,
                _ => true
            })
            .Take(take)
            .ToList();

        return rows;
    }

    public async Task AssignAsync(
        string tsc, string customerNumber, string customerName, string segment, string source,
        bool isConfirmed = true)
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

        var previous = existing is null
            ? "keine"
            : $"{existing.Segment}/{(existing.IsConfirmed ? "bestaetigt" : "Vorschlag")}";

        if (existing is null)
        {
            db.CustomerMarketSegments.Add(new CustomerMarketSegment
            {
                Tsc = normalizedTsc,
                CustomerNumber = normalizedCustomer,
                CustomerName = (customerName ?? string.Empty).Trim(),
                Segment = normalizedSegment,
                Source = (source ?? string.Empty).Trim(),
                IsConfirmed = isConfirmed,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.CustomerName = (customerName ?? string.Empty).Trim();
            existing.Segment = normalizedSegment;
            existing.Source = (source ?? string.Empty).Trim();
            existing.IsConfirmed = isConfirmed;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        // Nachvollziehbarkeit: die Zuordnung veraendert eine ausgewiesene Kennzahl, deshalb
        // gehoert jede Aenderung ins Protokoll.
        await _log.WriteAsync(
            "Marktsegment",
            existing is null ? "Segment zugeordnet" : "Segment geaendert",
            details: $"{normalizedTsc}/{normalizedCustomer} | {previous} -> {normalizedSegment}/" +
                     $"{(isConfirmed ? "bestaetigt" : "Vorschlag")} | Quelle={source}");
    }

    public async Task ConfirmAsync(string tsc, string customerNumber)
    {
        var normalizedTsc = MarketSegmentResolver.NormalizeTsc(tsc);
        var normalizedCustomer = MarketSegmentResolver.NormalizeCustomerNumber(customerNumber);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.CustomerMarketSegments
            .FirstOrDefaultAsync(x => x.Tsc == normalizedTsc && x.CustomerNumber == normalizedCustomer);
        if (existing is null)
            throw new InvalidOperationException("Zu diesem Kunden gibt es keinen Vorschlag.");
        if (existing.IsConfirmed) return;

        existing.IsConfirmed = true;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await _log.WriteAsync(
            "Marktsegment",
            "Vorschlag bestaetigt",
            details: $"{normalizedTsc}/{normalizedCustomer} | {existing.Segment}");
    }

    public async Task ClearAsync(string tsc, string customerNumber)
    {
        var normalizedTsc = MarketSegmentResolver.NormalizeTsc(tsc);
        var normalizedCustomer = MarketSegmentResolver.NormalizeCustomerNumber(customerNumber);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.CustomerMarketSegments
            .FirstOrDefaultAsync(x => x.Tsc == normalizedTsc && x.CustomerNumber == normalizedCustomer);
        if (existing is null) return;

        var previous = $"{existing.Segment}/{(existing.IsConfirmed ? "bestaetigt" : "Vorschlag")}";
        db.CustomerMarketSegments.Remove(existing);
        await db.SaveChangesAsync();

        await _log.WriteAsync(
            "Marktsegment",
            "Segment entfernt",
            details: $"{normalizedTsc}/{normalizedCustomer} | war {previous}");
    }

    public async Task<List<MarketSegmentResultRow>> GetResultAsync(bool confirmedOnly = true)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var assignments = await db.CustomerMarketSegments.AsNoTracking().ToListAsync();
        var relevant = assignments.Where(a => !confirmedOnly || a.IsConfirmed).ToList();
        if (relevant.Count == 0) return [];

        var segmentByKey = new Dictionary<(string, string), string>();
        foreach (var a in relevant)
        {
            var key = (MarketSegmentResolver.NormalizeTsc(a.Tsc),
                       MarketSegmentResolver.NormalizeCustomerNumber(a.CustomerNumber));
            segmentByKey[key] = a.Segment.Trim();
        }

        var tscKeys = segmentByKey.Keys.Select(k => k.Item1).Distinct().ToList();
        var customerKeys = segmentByKey.Keys.Select(k => k.Item2).Distinct().ToList();

        var grouped = await db.CentralSalesRecords.AsNoTracking()
            .Where(row => row.CustomerNumber != null && row.CustomerNumber.Trim() != ""
                          && tscKeys.Contains(row.Tsc)
                          && customerKeys.Contains(row.CustomerNumber.Trim()))
            .GroupBy(row => new { row.Tsc, row.CustomerNumber, row.SalesCurrency })
            .Select(g => new
            {
                g.Key.Tsc,
                g.Key.CustomerNumber,
                Currency = g.Key.SalesCurrency ?? string.Empty,
                SalesRows = g.Count(),
                SalesValue = g.Sum(x => (double)x.SalesPriceValue)
            })
            .ToListAsync();

        // Waehrungen werden BEWUSST nicht ueber Laender addiert. Jede Zeile bleibt in ihrer
        // Originalwaehrung; eine Konzernsumme braucht Kurse und gehoert in die Finance-Sicht.
        return grouped
            .Select(x => new
            {
                Key = (MarketSegmentResolver.NormalizeTsc(x.Tsc),
                       MarketSegmentResolver.NormalizeCustomerNumber(x.CustomerNumber)),
                x.Tsc,
                x.Currency,
                x.SalesRows,
                x.SalesValue
            })
            .Where(x => segmentByKey.ContainsKey(x.Key))
            .GroupBy(x => (Segment: segmentByKey[x.Key], x.Tsc, x.Currency))
            .Select(g => new MarketSegmentResultRow(
                g.Key.Segment,
                g.Key.Tsc,
                g.Key.Currency.Trim(),
                g.Select(x => x.Key).Distinct().Count(),
                g.Sum(x => x.SalesRows),
                (decimal)g.Sum(x => x.SalesValue)))
            .OrderBy(r => r.Segment, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(r => r.SalesRows)
            .ToList();
    }

    public async Task<MarketSegmentProgress> GetProgressAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var assignments = await db.CustomerMarketSegments.AsNoTracking().ToListAsync();
        var totalRows = await db.CentralSalesRecords.CountAsync();

        if (assignments.Count == 0)
            return new MarketSegmentProgress(0, 0, 0, 0, totalRows);

        var tscKeys = assignments.Select(a => MarketSegmentResolver.NormalizeTsc(a.Tsc)).Distinct().ToList();
        var customerKeys = assignments
            .Select(a => MarketSegmentResolver.NormalizeCustomerNumber(a.CustomerNumber)).Distinct().ToList();

        var counts = await db.CentralSalesRecords.AsNoTracking()
            .Where(row => row.CustomerNumber != null && tscKeys.Contains(row.Tsc)
                          && customerKeys.Contains(row.CustomerNumber.Trim()))
            .GroupBy(row => new { row.Tsc, row.CustomerNumber })
            .Select(g => new { g.Key.Tsc, g.Key.CustomerNumber, Rows = g.Count() })
            .ToListAsync();

        var rowsByKey = new Dictionary<(string, string), int>();
        foreach (var c in counts)
        {
            var key = (MarketSegmentResolver.NormalizeTsc(c.Tsc),
                       MarketSegmentResolver.NormalizeCustomerNumber(c.CustomerNumber));
            rowsByKey[key] = c.Rows;
        }

        int RowsFor(CustomerMarketSegment a) => rowsByKey.GetValueOrDefault(
            (MarketSegmentResolver.NormalizeTsc(a.Tsc),
             MarketSegmentResolver.NormalizeCustomerNumber(a.CustomerNumber)));

        var confirmed = assignments.Where(a => a.IsConfirmed).ToList();
        var proposals = assignments.Where(a => !a.IsConfirmed).ToList();

        return new MarketSegmentProgress(
            confirmed.Count,
            proposals.Count,
            confirmed.Sum(RowsFor),
            proposals.Sum(RowsFor),
            totalRows);
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
