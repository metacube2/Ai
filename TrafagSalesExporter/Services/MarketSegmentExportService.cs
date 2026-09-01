using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

namespace TrafagSalesExporter.Services;

public interface IMarketSegmentExportService
{
    /// <summary>
    /// Baut die Arbeitsmappe fuer den Knopf auf `/marktsegmente` als Blattliste.
    ///
    /// <paramref name="year"/> und <paramref name="tscFilter"/> sind die auf der Seite
    /// eingestellten Filter. Der Namensfilter des Suchfeldes wird BEWUSST nicht
    /// durchgereicht: er dient dem Finden eines einzelnen Kunden, und eine Datei, die nach
    /// einer stehengebliebenen Sucheingabe still nur ein paar Kunden enthaelt, sieht aus wie
    /// ein vollstaendiger Export.
    /// </summary>
    Task<IReadOnlyList<ExcelSheetData>> BuildSheetsAsync(int? year, string? tscFilter, string requestedBy);
}

/// <summary>
/// Erzeugt die Excel-Arbeitsdatei zur fachlichen Pruefung der Kunden-Segment-Zuordnung.
///
/// Konzept: `docs/KONZEPT_RAILWAY_EXPORT_2026-09-01.md`. Der Dienst liest ausschliesslich
/// und schreibt weder in die Datenbank noch auf die Platte; die Mappe entsteht im Speicher
/// und geht direkt in den Browser-Download.
///
/// Bewusst ein eigener Dienst und nicht Teil von <see cref="MarketSegmentPageService"/>:
/// dieser kennt nur die Pflege mit ihrer Seitenbegrenzung von 200 Zeilen, der Export
/// braucht dagegen den vollstaendigen Bestand.
/// </summary>
public sealed class MarketSegmentExportService : IMarketSegmentExportService
{
    /// <summary>Ab wie vielen verschiedenen Produktsparten ein Kunde als breit einkaufend gilt.</summary>
    public const int BroadPortfolioThreshold = 4;

    /// <summary>Spaltenname der Ersatzzeile, wenn ein Blatt keine Daten hat.</summary>
    public const string HintColumn = "Hinweis";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public MarketSegmentExportService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Eine Verkaufszeile, reduziert auf die fuer den Export erlaubten Felder. Kosten- und
    /// Margenfelder fehlen hier absichtlich: die Datei verlaesst das Dashboard.
    /// </summary>
    private sealed record ExportLine(
        string Tsc,
        string CustomerNumber,
        string CustomerName,
        string CustomerCountry,
        DateTime? PostingDate,
        DateTime? InvoiceDate,
        DateTime ExtractionDate,
        string InvoiceNumber,
        string Material,
        string ProductFamilyText,
        decimal Quantity,
        string SalesCurrency,
        decimal SalesPriceValue)
    {
        /// <summary>
        /// Dieselbe Periodenregel wie das zentrale Excel und wie die Seite selbst
        /// (<see cref="MarketSegmentPageService"/>). Waere sie hier eine andere, stuende
        /// dieselbe Zeile im Export in einem anderen Jahr als in der Ergebnisansicht.
        /// </summary>
        public int Year => (PostingDate ?? InvoiceDate ?? ExtractionDate).Year;
    }

    public async Task<IReadOnlyList<ExcelSheetData>> BuildSheetsAsync(int? year, string? tscFilter, string requestedBy)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var assignments = await db.CustomerMarketSegments.AsNoTracking().ToListAsync();
        var assignmentByKey = new Dictionary<(string Tsc, string Customer), CustomerMarketSegment>();
        foreach (var assignment in assignments)
        {
            var key = (MarketSegmentResolver.NormalizeTsc(assignment.Tsc),
                       MarketSegmentResolver.NormalizeCustomerNumber(assignment.CustomerNumber));
            assignmentByKey[key] = assignment;
        }

        var tsc = string.IsNullOrWhiteSpace(tscFilter) ? null : tscFilter.Trim();
        var lines = await LoadLinesAsync(db, assignmentByKey.Keys, year, tsc);

        var linesByKey = lines
            .GroupBy(line => (MarketSegmentResolver.NormalizeTsc(line.Tsc),
                              MarketSegmentResolver.NormalizeCustomerNumber(line.CustomerNumber)))
            .ToDictionary(g => g.Key, g => g.ToList());

        var proposals = BuildAssignmentRows(assignmentByKey, linesByKey, confirmed: false, tsc);
        var confirmedRows = BuildAssignmentRows(assignmentByKey, linesByKey, confirmed: true, tsc);

        return
        [
            new ExcelSheetData("Anleitung",
                BuildInstructions(year, tsc, requestedBy, assignmentByKey, proposals.Count, confirmedRows.Count, lines.Count)),
            new ExcelSheetData("Pruefung", WithHintIfEmpty(proposals,
                "Kein offener Vorschlag im gewaehlten Filter. Leer heisst hier: nichts zu pruefen, nicht: nichts gefunden.")),
            new ExcelSheetData("Bestaetigt", WithHintIfEmpty(confirmedRows,
                "Noch keine bestaetigte Zuordnung. Leer heisst hier: noch nichts fachlich geprueft, nicht: kein Umsatz.")),
            new ExcelSheetData("Verworfen (Protokoll)", WithHintIfEmpty(await BuildRemovalLogAsync(db),
                "Im Anwendungsprotokoll ist keine entfernte Zuordnung verzeichnet.")),
            new ExcelSheetData("Umsatz_Summen", WithHintIfEmpty(BuildSalesTotals(assignmentByKey, linesByKey),
                "Zu den zugeordneten Kunden gibt es im gewaehlten Filter keine Verkaufszeile.")),
            new ExcelSheetData("Umsatz_Detail", WithHintIfEmpty(BuildSalesDetail(assignmentByKey, lines),
                "Zu den zugeordneten Kunden gibt es im gewaehlten Filter keine Verkaufszeile.")),
            new ExcelSheetData("Marktumfrage", WithHintIfEmpty(await BuildSurveyAsync(db),
                "Es ist keine Marktumfrage erfasst.")),
            new ExcelSheetData("Datenluecken", WithHintIfEmpty(await BuildDataGapsAsync(db, year, tsc),
                "Keine Datenluecke gemessen."))
        ];
    }

    private static async Task<List<ExportLine>> LoadLinesAsync(
        AppDbContext db, IReadOnlyCollection<(string Tsc, string Customer)> keys, int? year, string? tsc)
    {
        if (keys.Count == 0) return [];

        var tscKeys = keys.Select(k => k.Tsc).Distinct().ToList();
        var customerKeys = keys.Select(k => k.Customer).Distinct().ToList();

        var query = db.CentralSalesRecords.AsNoTracking()
            .Where(row => row.CustomerNumber != null && row.CustomerNumber.Trim() != "");
        if (tsc is not null)
            query = query.Where(row => row.Tsc == tsc);
        query = FilterByYear(query, year);

        // Grobfilter in der Datenbank, exakte Paarpruefung danach in der Anwendung: SQLite
        // kann keinen Tupel-IN-Vergleich, und zwei getrennte IN-Listen liessen sonst falsche
        // Kombinationen durch. Dieselbe Falle wie in MarketSegmentPageService.
        var candidates = await query
            .Where(row => tscKeys.Contains(row.Tsc) && customerKeys.Contains(row.CustomerNumber!.Trim()))
            .Select(row => new ExportLine(
                row.Tsc,
                row.CustomerNumber!,
                row.CustomerName,
                row.CustomerCountry,
                row.PostingDate,
                row.InvoiceDate,
                row.ExtractionDate,
                row.InvoiceNumber,
                row.Material,
                row.ProductFamilyText,
                row.Quantity,
                row.SalesCurrency,
                row.SalesPriceValue))
            .ToListAsync();

        var exact = new HashSet<(string, string)>(keys);
        return candidates
            .Where(line => exact.Contains((MarketSegmentResolver.NormalizeTsc(line.Tsc),
                                           MarketSegmentResolver.NormalizeCustomerNumber(line.CustomerNumber))))
            .ToList();
    }

    /// <summary>Periodenregel des zentralen Excel: Buchungsdatum, sonst Rechnungsdatum, sonst Extraktionsdatum.</summary>
    private static IQueryable<CentralSalesRecord> FilterByYear(IQueryable<CentralSalesRecord> query, int? year)
        => year is null
            ? query
            : query.Where(row => (row.PostingDate ?? row.InvoiceDate ?? row.ExtractionDate).Year == year.Value);

    private static List<IReadOnlyDictionary<string, object?>> BuildAssignmentRows(
        Dictionary<(string Tsc, string Customer), CustomerMarketSegment> assignmentByKey,
        Dictionary<(string, string), List<ExportLine>> linesByKey,
        bool confirmed,
        string? tsc)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var (key, assignment) in assignmentByKey
                     .Where(pair => pair.Value.IsConfirmed == confirmed)
                     .OrderBy(pair => pair.Key.Tsc, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(pair => pair.Key.Customer, StringComparer.OrdinalIgnoreCase))
        {
            if (tsc is not null && !string.Equals(key.Tsc, MarketSegmentResolver.NormalizeTsc(tsc), StringComparison.OrdinalIgnoreCase))
                continue;

            linesByKey.TryGetValue(key, out var lines);
            lines ??= [];
            var families = lines
                .Select(line => line.ProductFamilyText?.Trim() ?? string.Empty)
                .Where(family => family.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            rows.Add(new Dictionary<string, object?>
            {
                ["TSC"] = key.Tsc,
                ["Kundennummer"] = key.Customer,
                // Name aus dem Verkaufsbestand, sonst der bei der Zuordnung festgehaltene.
                // Fuer TRDE ist beides leer, das ist die dokumentierte Datenluecke.
                ["Kundenname"] = lines.Select(line => line.CustomerName?.Trim() ?? string.Empty)
                    .FirstOrDefault(name => name.Length > 0) ?? assignment.CustomerName.Trim(),
                ["Kundenland"] = lines.Select(line => line.CustomerCountry?.Trim() ?? string.Empty)
                    .FirstOrDefault(land => land.Length > 0) ?? string.Empty,
                ["Segment"] = assignment.Segment.Trim(),
                ["Status"] = assignment.IsConfirmed ? "Bestaetigt" : "Vorschlag",
                ["Quelle"] = assignment.Source.Trim(),
                ["Hinweis"] = assignment.ProposalNote.Trim(),
                ["Verkaufszeilen"] = lines.Count,
                ["Produktsparten"] = families,
                // Keine Umsatzsumme: ein Kunde kann in mehreren Waehrungen fakturiert sein,
                // eine Zahl daneben waere still falsch. Das Geld steht waehrungsrein im
                // Blatt Umsatz_Summen, verknuepfbar ueber TSC und Kundennummer.
                ["Warnung"] = families >= BroadPortfolioThreshold
                    ? "Breites Sortiment, bitte einzeln pruefen"
                    : string.Empty,
                ["Zuletzt geaendert"] = assignment.UpdatedAtUtc
            });
        }

        return rows;
    }

    private static List<IReadOnlyDictionary<string, object?>> BuildSalesTotals(
        Dictionary<(string Tsc, string Customer), CustomerMarketSegment> assignmentByKey,
        Dictionary<(string, string), List<ExportLine>> linesByKey)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var (key, lines) in linesByKey.OrderBy(pair => pair.Key.Item1, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(pair => pair.Key.Item2, StringComparer.OrdinalIgnoreCase))
        {
            // Der Schluessel existiert immer: `LoadLinesAsync` laedt ausschliesslich Zeilen,
            // deren Paar aus Standort und Kundennummer exakt einer Zuordnung entspricht.
            var assignment = assignmentByKey[key];

            // Waehrung ist Teil des Schluessels. Ueber Waehrungen zu summieren waere ohne
            // Kurse falsch, und die Umrechnung ist eine Finance-Entscheidung.
            var groups = lines
                .GroupBy(line => new { line.Year, Currency = line.SalesCurrency?.Trim() ?? string.Empty })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Currency, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                rows.Add(new Dictionary<string, object?>
                {
                    ["TSC"] = key.Item1,
                    ["Kundennummer"] = key.Item2,
                    ["Kundenname"] = lines.Select(line => line.CustomerName?.Trim() ?? string.Empty)
                        .FirstOrDefault(name => name.Length > 0) ?? string.Empty,
                    ["Segment"] = assignment.Segment.Trim(),
                    ["Status"] = assignment.IsConfirmed ? "Bestaetigt" : "Vorschlag",
                    ["Jahr"] = group.Key.Year,
                    ["Waehrung"] = group.Key.Currency,
                    ["Verkaufszeilen"] = group.Count(),
                    ["Umsatz"] = group.Sum(line => line.SalesPriceValue)
                });
            }
        }

        return rows;
    }

    private static List<IReadOnlyDictionary<string, object?>> BuildSalesDetail(
        Dictionary<(string Tsc, string Customer), CustomerMarketSegment> assignmentByKey,
        List<ExportLine> lines)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var line in lines
                     .OrderBy(line => line.Tsc, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(line => line.CustomerNumber, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(line => line.Year))
        {
            // Wie im Summenblatt: nur Zeilen mit exakt passender Zuordnung sind ueberhaupt
            // geladen worden, der Schluessel existiert also immer.
            var key = (MarketSegmentResolver.NormalizeTsc(line.Tsc),
                       MarketSegmentResolver.NormalizeCustomerNumber(line.CustomerNumber));
            var assignment = assignmentByKey[key];

            rows.Add(new Dictionary<string, object?>
            {
                ["TSC"] = line.Tsc,
                ["Kundennummer"] = line.CustomerNumber.Trim(),
                ["Kundenname"] = line.CustomerName?.Trim() ?? string.Empty,
                ["Segment"] = assignment.Segment.Trim(),
                ["Status"] = assignment.IsConfirmed ? "Bestaetigt" : "Vorschlag",
                ["Jahr"] = line.Year,
                ["Buchungsdatum"] = line.PostingDate,
                ["Rechnungsdatum"] = line.InvoiceDate,
                ["Rechnungsnummer"] = line.InvoiceNumber?.Trim() ?? string.Empty,
                ["Material"] = line.Material?.Trim() ?? string.Empty,
                ["Produktsparte"] = line.ProductFamilyText?.Trim() ?? string.Empty,
                ["Menge"] = line.Quantity,
                ["Waehrung"] = line.SalesCurrency?.Trim() ?? string.Empty,
                ["Umsatz"] = line.SalesPriceValue
            });
        }

        return rows;
    }

    /// <summary>
    /// Protokollspur der entfernten Zuordnungen, KEIN gepflegter Zustand: `ClearAsync`
    /// loescht die Zeile, ein Zustand `verworfen` wird nirgends gespeichert.
    ///
    /// `AppEventLog` kennt keinen Nutzer, deshalb gibt es hier keine Spalte dafuer. TSC,
    /// Kundennummer und der vorherige Stand stehen zusammen in einem Textfeld der Form
    /// `TSC/Kundennummer | war Segment/Status`; passt eine Zeile nicht auf dieses Muster,
    /// bleibt sie unzerlegt stehen statt geraten zu werden.
    /// </summary>
    private static async Task<List<IReadOnlyDictionary<string, object?>>> BuildRemovalLogAsync(AppDbContext db)
    {
        var events = await db.AppEventLogs.AsNoTracking()
            .Where(entry => entry.Category == "Marktsegment" && entry.Message == "Segment entfernt")
            .OrderByDescending(entry => entry.Timestamp)
            .ToListAsync();

        return events.Select(entry =>
        {
            var details = entry.Details ?? string.Empty;
            var separator = details.IndexOf(" | war ", StringComparison.Ordinal);
            var keyPart = separator >= 0 ? details[..separator] : string.Empty;
            var previous = separator >= 0 ? details[(separator + " | war ".Length)..] : details;

            var slash = keyPart.IndexOf('/');
            var tsc = slash >= 0 ? keyPart[..slash] : string.Empty;
            var customer = slash >= 0 ? keyPart[(slash + 1)..] : string.Empty;

            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["Zeitpunkt"] = entry.Timestamp,
                ["TSC"] = tsc,
                ["Kundennummer"] = customer,
                ["Vorheriger Stand"] = previous
            };
        }).ToList();
    }

    private static async Task<List<IReadOnlyDictionary<string, object?>>> BuildSurveyAsync(AppDbContext db)
    {
        var entries = await db.MarketSurveyEntries.AsNoTracking()
            .OrderBy(entry => entry.SurveyName)
            .ThenBy(entry => entry.Country)
            .ThenBy(entry => entry.CustomerName)
            .ToListAsync();

        return entries.Select(entry => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["Umfrage"] = entry.SurveyName,
            ["Land"] = entry.Country,
            ["Kundenname"] = entry.CustomerName,
            ["Kurzform"] = entry.CustomerShort,
            ["Kundentyp"] = entry.CustomerType,
            ["Geschaeftsart"] = entry.BusinessType,
            ["Anwendung"] = entry.Application,
            ["Anwendungsbeschreibung"] = entry.ApplicationDescription,
            ["Status"] = entry.Status,
            ["Trafag Argument"] = entry.TrafagUsp,
            ["Wettbewerber"] = entry.Competitor,
            ["Produkt"] = entry.Product,
            ["Materialnummer"] = entry.MaterialNumber,
            // Menge und Preis bleiben Text: die Quelle fuehrt Bereiche und gemischte
            // Waehrungen wie `500-600 pcs` oder `15k€`. Eine Zahl waere Scheingenauigkeit.
            ["Geschaetzte Menge"] = entry.EstimatedQuantity,
            ["Geschaetzter Preis"] = entry.EstimatedPrice,
            ["Bemerkung"] = entry.Comments,
            ["Verknuepfter TSC"] = entry.LinkedTsc,
            ["Verknuepfte Kundennummer"] = entry.LinkedCustomerNumber
        }).ToList();
    }

    /// <summary>
    /// Weist die bekannten Luecken offen aus, damit ein leeres Ergebnis nicht als Umsatz von
    /// null gelesen wird. Fuer TRDE fehlen Kundenname und Kundenland in allen Verkaufszeilen.
    /// </summary>
    private static async Task<List<IReadOnlyDictionary<string, object?>>> BuildDataGapsAsync(
        AppDbContext db, int? year, string? tsc)
    {
        var query = db.CentralSalesRecords.AsNoTracking();
        if (tsc is not null)
            query = query.Where(row => row.Tsc == tsc);
        query = FilterByYear(query, year);

        var perSite = await query
            .GroupBy(row => row.Tsc)
            .Select(g => new
            {
                Tsc = g.Key,
                Rows = g.Count(),
                WithoutName = g.Count(row => row.CustomerName == null || row.CustomerName.Trim() == ""),
                WithoutCountry = g.Count(row => row.CustomerCountry == null || row.CustomerCountry.Trim() == ""),
                WithoutCustomerNumber = g.Count(row => row.CustomerNumber == null || row.CustomerNumber.Trim() == "")
            })
            .OrderBy(x => x.Tsc)
            .ToListAsync();

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var site in perSite)
        {
            void Add(string measure, int value) => rows.Add(new Dictionary<string, object?>
            {
                ["Bereich"] = "Verkaufsdaten",
                ["Schluessel"] = site.Tsc,
                ["Kennzahl"] = measure,
                ["Wert"] = value
            });

            Add("Verkaufszeilen im Filter", site.Rows);
            Add("Zeilen ohne Kundenname", site.WithoutName);
            Add("Zeilen ohne Kundenland", site.WithoutCountry);
            Add("Zeilen ohne Kundennummer", site.WithoutCustomerNumber);
        }

        var surveys = await db.MarketSurveyEntries.AsNoTracking()
            .GroupBy(entry => entry.SurveyName)
            .Select(g => new
            {
                Survey = g.Key,
                Entries = g.Count(),
                Unlinked = g.Count(entry => entry.LinkedCustomerNumber == null || entry.LinkedCustomerNumber.Trim() == "")
            })
            .OrderBy(x => x.Survey)
            .ToListAsync();

        foreach (var survey in surveys)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["Bereich"] = "Marktumfrage",
                ["Schluessel"] = survey.Survey,
                ["Kennzahl"] = "Umfragezeilen",
                ["Wert"] = survey.Entries
            });
            rows.Add(new Dictionary<string, object?>
            {
                ["Bereich"] = "Marktumfrage",
                ["Schluessel"] = survey.Survey,
                ["Kennzahl"] = "Zeilen ohne verknuepften Kunden",
                ["Wert"] = survey.Unlinked
            });
        }

        return rows;
    }

    private static List<IReadOnlyDictionary<string, object?>> BuildInstructions(
        int? year,
        string? tsc,
        string requestedBy,
        Dictionary<(string Tsc, string Customer), CustomerMarketSegment> assignmentByKey,
        int proposalCount,
        int confirmedCount,
        int lineCount)
    {
        var segments = assignmentByKey.Values
            .Select(assignment => assignment.Segment.Trim())
            .Where(segment => segment.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(segment => segment, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        void Add(string field, object? value) => rows.Add(new Dictionary<string, object?>
        {
            ["Feld"] = field,
            ["Wert"] = value
        });

        Add("Erzeugt am", DateTime.Now);
        Add("Erzeugt von", requestedBy);
        Add("Gewaehltes Jahr", year?.ToString() ?? "alle Jahre");
        Add("Gewaehlter Standort", string.IsNullOrWhiteSpace(tsc) ? "alle Standorte" : tsc);
        Add("Namensfilter", "bewusst nicht angewendet, der Export enthaelt die vollstaendige Pruefmenge");
        Add("Enthaltene Segmente", segments.Count == 0 ? "keine Zuordnung gepflegt" : string.Join(", ", segments));
        Add("Offene Vorschlaege", proposalCount);
        Add("Bestaetigte Zuordnungen", confirmedCount);
        Add("Verkaufszeilen im Export", lineCount);
        Add("Periodenregel", "Jahr = Buchungsdatum, sonst Rechnungsdatum, sonst Extraktionsdatum. Gleiche Regel wie das zentrale Excel.");
        Add("Vorschlag", "Maschineller Treffer aus dem Namensabgleich mit der Marktumfrage. Ein unbestaetigter Vorschlag ist KEINE Reporting-Aussage.");
        Add("Bestaetigt", "Von einem Menschen fachlich bestaetigt. Nur diese Zuordnungen wirken im zentralen Excel.");
        Add("Waehrung", "Umsatz steht als Rohwert je Waehrung. Es wird ausschliesslich innerhalb derselben Waehrung summiert.");
        Add("Blatt Verworfen (Protokoll)", "Protokollspur aus dem Anwendungsprotokoll, kein gepflegter Zustand. Ein Verwerfen loescht die Zuordnung, es wird nicht als Zustand gespeichert.");
        Add("Datenluecke TRDE", "In den deutschen Verkaufszeilen fehlen Kundenname und Kundenland. Ein leeres deutsches Ergebnis ist eine Datenluecke und kein Umsatz von null. Siehe Blatt Datenluecken.");
        Add("Leeres Kundenland", "Fehlt der Name in den Verkaufszeilen, zeigen die Blaetter Pruefung und Bestaetigt den bei der Zuordnung festgehaltenen Namen. Fuer das Kundenland gibt es keinen solchen Ersatz: ein leeres Kundenland bei gefuelltem Namen ist genau diese Quellluecke und kein fehlender Einzelfall.");
        Add("Pflege", "Bestaetigen und Verwerfen geschieht im Dashboard unter /marktsegmente. Diese Datei ist eine Arbeitskopie zum Lesen und wird nicht zurueckgelesen.");

        return rows;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> WithHintIfEmpty(
        List<IReadOnlyDictionary<string, object?>> rows, string hint)
    {
        // Ein Blatt ohne Zeilen haette auch keine Kopfzeile, weil die Spalten aus den
        // Zeilenschluesseln entstehen. Ein leeres, unbeschriftetes Blatt sieht nach Fehler
        // aus; der Hinweis sagt stattdessen, was leer hier bedeutet.
        if (rows.Count > 0) return rows;
        return [new Dictionary<string, object?> { [HintColumn] = hint }];
    }
}
