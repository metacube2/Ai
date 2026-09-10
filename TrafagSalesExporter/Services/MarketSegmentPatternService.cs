using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

/// <summary>Ein Treffer eines Musters auf einen Kunden, vor dem Schreiben.</summary>
public sealed record SegmentPatternHit(
    string Tsc,
    string CustomerNumber,
    string CustomerName,
    string Segment,
    string Pattern,
    SegmentPatternHitKind Kind,
    string ExistingSegment);

public enum SegmentPatternHitKind
{
    /// <summary>Kunde hat noch keine Zuordnung; der Vorschlag waere neu.</summary>
    Neu,

    /// <summary>Ein unbestaetigter Vorschlag mit demselben Segment steht schon da.</summary>
    BereitsVorgeschlagen,

    /// <summary>Ein Mensch hat dasselbe Segment bereits bestaetigt.</summary>
    BereitsBestaetigt,

    /// <summary>
    /// Ein Mensch hat ein ANDERES Segment bestaetigt. Das Muster wird nicht angewendet;
    /// der Fall gehoert angesehen, weil entweder das Muster oder der Entscheid falsch ist.
    /// </summary>
    KonfliktMitBestaetigung
}

public sealed record SegmentPatternPreview(
    IReadOnlyList<SegmentPatternHit> Hits,
    int PatternsApplied)
{
    public int Neu => Hits.Count(x => x.Kind == SegmentPatternHitKind.Neu);
    public int BereitsVorgeschlagen => Hits.Count(x => x.Kind == SegmentPatternHitKind.BereitsVorgeschlagen);
    public int BereitsBestaetigt => Hits.Count(x => x.Kind == SegmentPatternHitKind.BereitsBestaetigt);
    public int Konflikte => Hits.Count(x => x.Kind == SegmentPatternHitKind.KonfliktMitBestaetigung);
}

/// <summary>
/// Erzeugt Segment-VORSCHLAEGE aus kuratierten Namensmustern, siehe
/// <see cref="SegmentNamePattern"/>.
///
/// <para>
/// Drei Regeln, die nicht verhandelbar sind:
/// </para>
/// <list type="number">
/// <item>Ein Muster schreibt nie <c>IsConfirmed = true</c>. Es schlaegt vor, mehr nicht.</item>
/// <item>Eine bestaetigte Zuordnung wird nie ueberschrieben, auch nicht mit demselben
/// Segment. Der menschliche Entscheid hat Vorrang vor jedem Muster.</item>
/// <item>Ein Muster, das auf einen anders bestaetigten Kunden trifft, wird als Konflikt
/// gemeldet statt still uebergangen. Sonst bliebe ein falsches Muster unbemerkt.</item>
/// </list>
/// </summary>
public sealed class MarketSegmentPatternService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppEventLogService _log;

    public MarketSegmentPatternService(IDbContextFactory<AppDbContext> dbFactory, IAppEventLogService log)
    {
        _dbFactory = dbFactory;
        _log = log;
    }

    public async Task<List<SegmentNamePattern>> GetPatternsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.SegmentNamePatterns.AsNoTracking()
            .OrderBy(x => x.Segment).ThenBy(x => x.Pattern).ToListAsync();
    }

    public async Task SavePatternAsync(SegmentNamePattern pattern)
    {
        var normalized = (pattern.Pattern ?? string.Empty).Trim();

        // Drei Zeichen ist die gemessene Untergrenze: kuerzere Teilzeichenfolgen treffen
        // quer durch den Bestand. "abb" fing bereits ABBOTT LABS US.
        if (normalized.Length < 3)
            throw new ArgumentException("Ein Muster braucht mindestens drei Zeichen. Kuerzere Muster treffen zu breit.");
        if (string.IsNullOrWhiteSpace(pattern.Segment))
            throw new ArgumentException("Ein Muster ohne Segment ergibt keinen Vorschlag.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = pattern.Id > 0
            ? await db.SegmentNamePatterns.FirstOrDefaultAsync(x => x.Id == pattern.Id)
            : null;

        if (existing is null)
        {
            db.SegmentNamePatterns.Add(new SegmentNamePattern
            {
                Pattern = normalized,
                Segment = pattern.Segment.Trim(),
                Tsc = (pattern.Tsc ?? string.Empty).Trim(),
                IsActive = pattern.IsActive,
                Note = (pattern.Note ?? string.Empty).Trim(),
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Pattern = normalized;
            existing.Segment = pattern.Segment.Trim();
            existing.Tsc = (pattern.Tsc ?? string.Empty).Trim();
            existing.IsActive = pattern.IsActive;
            existing.Note = (pattern.Note ?? string.Empty).Trim();
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeletePatternAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var found = await db.SegmentNamePatterns.FirstOrDefaultAsync(x => x.Id == id);
        if (found is null) return;
        db.SegmentNamePatterns.Remove(found);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Zeigt, was die aktiven Muster treffen wuerden. Schreibt nichts.
    /// </summary>
    public async Task<SegmentPatternPreview> PreviewAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var patterns = await db.SegmentNamePatterns.AsNoTracking()
            .Where(x => x.IsActive).ToListAsync();
        if (patterns.Count == 0)
            return new SegmentPatternPreview([], 0);

        // Ein Kunde je Standort, nicht je Verkaufszeile.
        var customers = await db.CentralSalesRecords.AsNoTracking()
            .Join(db.Sites.AsNoTracking(), r => r.SiteId, s => s.Id, (r, s) => new
            {
                s.TSC,
                r.CustomerNumber,
                r.CustomerName
            })
            .Where(x => x.CustomerName != "" && x.CustomerNumber != "")
            .Distinct()
            .ToListAsync();

        var existing = await db.CustomerMarketSegments.AsNoTracking().ToListAsync();
        var byKey = new Dictionary<(string, string), CustomerMarketSegment>(SiteCustomerComparer.Instance);
        foreach (var entry in existing)
            byKey[(entry.Tsc ?? string.Empty, entry.CustomerNumber ?? string.Empty)] = entry;

        var hits = new List<SegmentPatternHit>();
        var seen = new HashSet<(string, string, string)>();
        foreach (var pattern in patterns)
        {
            var needle = pattern.Pattern.Trim();
            foreach (var customer in customers)
            {
                if (pattern.Tsc.Length > 0 &&
                    !string.Equals(pattern.Tsc, customer.TSC, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (customer.CustomerName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (!seen.Add((customer.TSC, customer.CustomerNumber, pattern.Segment)))
                    continue;

                byKey.TryGetValue((customer.TSC, customer.CustomerNumber), out var already);
                var sameSegment = already is not null
                    && string.Equals(already.Segment, pattern.Segment, StringComparison.OrdinalIgnoreCase);

                var kind = already is null
                    ? SegmentPatternHitKind.Neu
                    : sameSegment
                        ? already.IsConfirmed
                            ? SegmentPatternHitKind.BereitsBestaetigt
                            : SegmentPatternHitKind.BereitsVorgeschlagen
                        : already.IsConfirmed
                            ? SegmentPatternHitKind.KonfliktMitBestaetigung
                            : SegmentPatternHitKind.BereitsVorgeschlagen;

                hits.Add(new SegmentPatternHit(customer.TSC, customer.CustomerNumber,
                    customer.CustomerName, pattern.Segment, needle, kind, already?.Segment ?? string.Empty));
            }
        }

        return new SegmentPatternPreview(
            hits.OrderBy(x => x.Kind).ThenBy(x => x.Tsc).ThenBy(x => x.CustomerName).ToList(),
            patterns.Count);
    }

    /// <summary>
    /// Schreibt die neuen Vorschlaege. Bestehende Zeilen bleiben unangetastet, gleich ob
    /// bestaetigt oder nicht. Gibt die Zahl der angelegten Vorschlaege zurueck.
    /// </summary>

    /// <summary>
    /// Startsatz fuer das Segment `Railway`, gemessen am 2026-09-10 gegen den
    /// Produktivbestand. Bewusst eng gefasst: `siemens mobility` statt `siemens`, weil
    /// `siemens` auch `SIEMENS ENERGY S.A` faengt, und ohne `abb`, weil das `ABBOTT LABS US`
    /// treffen wuerde. `voith` bleibt auf TRDE begrenzt, weil `Voith Hydro` in der Schweiz
    /// Wasserkraft ist und nicht Bahn.
    ///
    /// Der Satz ist ein Vorschlag zum Weiterpflegen, keine abschliessende Liste.
    /// </summary>
    public async Task<int> SeedRailwayStarterSetAsync()
    {
        (string Pattern, string Tsc, string Note)[] starter =
        [
            ("deutsche bahn", "", "Konzern Deutsche Bahn"),
            ("db fahrzeug", "", "DB Fahrzeuginstandhaltung"),
            ("siemens mobility", "", "bewusst nicht 'siemens': das faengt Siemens Energy"),
            ("alstom", "", "16 Kunden ueber sechs Standorte"),
            ("stadler", "", "Stadler Rail"),
            ("knorr-bremse", "", "Bremstechnik Bahn"),
            ("wabtec", "", "11 Kunden ueber fuenf Standorte"),
            ("bombardier", "", "Bombardier Transportation"),
            ("faiveley", "", "Faiveley Transport"),
            ("vossloh", "", "Bahninfrastruktur"),
            ("windhoff", "", "Windhoff Bahn und Anlagentechnik"),
            ("trenitalia", "", "Bahnbetreiber Italien"),
            ("sncf", "", "Bahnbetreiber Frankreich"),
            ("talgo", "", "Bahnhersteller Spanien"),
            ("voith", "TRDE", "nur DE: 'Voith Hydro' in der Schweiz ist Wasserkraft"),
        ];

        var added = 0;
        foreach (var item in starter)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var exists = await db.SegmentNamePatterns.AnyAsync(x =>
                x.Pattern == item.Pattern && x.Segment == "Railway" && x.Tsc == item.Tsc);
            if (exists) continue;

            db.SegmentNamePatterns.Add(new SegmentNamePattern
            {
                Pattern = item.Pattern,
                Segment = "Railway",
                Tsc = item.Tsc,
                IsActive = true,
                Note = item.Note,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            added++;
        }

        return added;
    }
    public async Task<int> ApplyAsync()
    {
        var preview = await PreviewAsync();
        var neu = preview.Hits.Where(x => x.Kind == SegmentPatternHitKind.Neu).ToList();
        if (neu.Count == 0) return 0;

        await using var db = await _dbFactory.CreateDbContextAsync();
        foreach (var hit in neu)
        {
            // Zwischen Vorschau und Schreiben kann ein Mensch zugeordnet haben.
            var exists = await db.CustomerMarketSegments
                .AnyAsync(x => x.Tsc == hit.Tsc && x.CustomerNumber == hit.CustomerNumber);
            if (exists) continue;

            db.CustomerMarketSegments.Add(new CustomerMarketSegment
            {
                Tsc = hit.Tsc,
                CustomerNumber = hit.CustomerNumber,
                CustomerName = hit.CustomerName,
                Segment = hit.Segment,
                IsConfirmed = false,
                ProposalNote = "Namensmuster: " + hit.Pattern,
                Source = "Kuratiertes Namensmuster, noch nicht bestaetigt",
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        var written = await db.SaveChangesAsync();
        await _log.WriteAsync("Marktsegmente", "Vorschlaege aus Namensmustern erzeugt",
            details: $"Muster={preview.PatternsApplied}; neue Vorschlaege={written}; "
                     + $"bereits vorgeschlagen={preview.BereitsVorgeschlagen}; "
                     + $"bestaetigt={preview.BereitsBestaetigt}; Konflikte={preview.Konflikte}");
        return written;
    }

    private sealed class SiteCustomerComparer : IEqualityComparer<(string, string)>
    {
        internal static readonly SiteCustomerComparer Instance = new();

        public bool Equals((string, string) x, (string, string) y)
            => string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase)
               && string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string, string) obj)
            => HashCode.Combine(
                obj.Item1?.ToUpperInvariant() ?? string.Empty,
                obj.Item2?.ToUpperInvariant() ?? string.Empty);
    }
}
