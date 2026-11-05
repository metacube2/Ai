using System.Globalization;
using System.Text;
using System.Text.Json;

namespace TrafagSalesExporter.Services;

// Monthly Review, Abschnitt 4 "Qualitaet · offene Q-Faelle" (2026-11-05): offene QM-Meldungen Q1 live aus SAP statt aus
// Power BI "RMA Statistic". Quelle GET /sap/opu/odata/sap/ZPOWERBI_EINKAUF_SRV/QmMeldungSet (wird parallel in T76
// gebaut). Properties alle Edm.String ausser Menge (Edm.Decimal); Datumsfelder JJJJMMTT oder leer.
// Doku docs/monthly_review/MONTHLY_REVIEW_2026-11-04.md, Vertrag Spezifikation 1.8, 1.11, 2.2, 2.3 (10-13), 2.4 (12-14), 3.5.

/// <summary>Einstellungen der Monthly Review (appsettings, Abschnitt "MonthlyReview").</summary>
public sealed class MonthlyReviewOptions
{
    public const string SectionName = "MonthlyReview";

    /// <summary>
    /// SAP-Abruf der Q-Faelle (QmMeldungSet). Vorgabe aus: erst einschalten, wenn der Transport mit dem Set in P76 ist
    /// und die Gateway-Caches geleert sind. Aus = Seite zeigt das Panel "noch nicht angebunden", kein SAP-Aufruf.
    /// </summary>
    public bool QmEnabled { get; set; }

    /// <summary>Entity-Set im Einkauf-Service (Verbindung wie Logistik live, Shopfloor, Einkauf).</summary>
    public string QmEntitySet { get; set; } = "QmMeldungSet";

    /// <summary>Optionaler OData-$filter, z. B. "Qmart eq 'Q1'". Leer = ohne Filter, die Auswahl macht das Set.</summary>
    public string QmFilter { get; set; } = string.Empty;

    /// <summary>Erfolgreiche Abrufe werden so lange fuer alle Betrachter gemerkt.</summary>
    public int QmCacheMinutes { get; set; } = 15;

    /// <summary>Fehler werden nur kurz gemerkt (LEARNINGS 2026-10-05: Fehler kurz, Erfolge lang).</summary>
    public int QmErrorCacheSeconds { get; set; } = 60;

    public int QmTimeoutSeconds { get; set; } = 60;

    /// <summary>Meldungsarten, die zaehlen (Qmart). Leer = alle, die das Set liefert.</summary>
    public List<string> QmNotificationTypes { get; set; } = ["Q1"];

    /// <summary>
    /// Systemstatus, bei denen eine Meldung nicht mehr offen ist (Sysstat enthaelt den Code). Das Set liefert die Kurztexte
    /// auf Deutsch (STATUS_TEXT_EDIT, Sprache D, in T76 belegt 2026-11-05): MMAB Meldung abgeschlossen (I0072),
    /// LOeVM Loeschvormerkung (I0076); NOCO/DLFL sind die englischen Gegenstuecke als Rueckfall. Schutz, falls das Set auch
    /// Erledigtes liefert; der SAP-Zweig filtert diese Status schon selbst (saptasks/qm_meldungen/NOTES.md).
    /// </summary>
    public List<string> QmExcludeSystemStatus { get; set; } = ["MMAB", "LÖVM", "NOCO", "DLFL"];

    /// <summary>
    /// Zusaetzliche Zuordnungen Kunde -> Gesellschaft. Werden vor den eingebauten Vorgaben
    /// (<see cref="MonthlyReviewQm.DefaultCompanyCustomers"/>) geprueft; so laesst sich eine fehlende Kundennummer ergaenzen,
    /// ohne die Vorgaben zu wiederholen.
    /// </summary>
    public List<MonthlyReviewCompanyCustomer> QmCompanyCustomers { get; set; } = [];
}

/// <summary>Zuordnung von Kunden einer QM-Meldung zu einer Gesellschaft: Kundennummer zuerst, dann Namensanfang.</summary>
public sealed class MonthlyReviewCompanyCustomer
{
    public string Company { get; set; } = string.Empty;
    public List<string> CustomerNumbers { get; set; } = [];
    public List<string> NamePrefixes { get; set; } = [];
    /// <summary>Herkunft der Zuordnung (Beleg), nur fuer die Doku.</summary>
    public string Source { get; set; } = string.Empty;
}

/// <summary>Eine Zeile aus QmMeldungSet, wie geliefert (Datumsfelder schon gelesen).</summary>
public sealed record MonthlyReviewQmRow(
    string Number, string Type, string Status, string SystemStatus, string CustomerNumber, string CustomerName,
    string Material, string MaterialText, decimal? Quantity, string Unit, string Defect, DateOnly? Created,
    DateOnly? Returned, DateOnly? Production, DateOnly? MeasureCreated, string Processor, string ProductLine);

/// <summary>Offene Meldung mit Gesellschaft und Alter am Stichtag.</summary>
public sealed record MonthlyReviewQmCase(MonthlyReviewQmRow Row, string Company, int? AgeDays, int? DaysSinceReturn, int? DaysToMeasure)
{
    public string Number => Row.Number;
    public string Status => Row.Status;
    public string Processor => Row.Processor;
}

public sealed record MonthlyReviewQmCount(string Label, int Count);

public sealed record MonthlyReviewQmStatusAge(string Status, int Cases, double? AverageDays, double? MedianDays, int Over90);

/// <summary>Kennzahlen einer Menge offener Meldungen.</summary>
public sealed record MonthlyReviewQmStats(
    int Count, double? AverageDays, double? MedianDays, int? MaxDays, int Over90, MonthlyReviewQmCase? Oldest, int ReturnOpen)
{
    public double? Over90Share => Count == 0 ? null : (double)Over90 / Count;
}

public static class MonthlyReviewQm
{
    /// <summary>Gesellschaft fuer Meldungen, deren Kunde keine Konzerngesellschaft ist (Direktgeschaeft TRCH).</summary>
    public const string EndCustomer = "Endkunde (TRCH)";
    /// <summary>Kunde ist die Trafag AG selbst.</summary>
    public const string TrchInternal = "TRCH intern";
    public const string WithoutProductLine = "ohne Produktlinie";
    public const int OverdueDays = 90;

    /// <summary>
    /// Eingebaute Zuordnungen. Kundennummern nur, wo im Repo belegt: TRIN, TRCZ und GFS aus der Liefertreue-Spezifikation
    /// (KpiLiefertreueSync.DefaultExclusions, docs/OPERATIONS_KPI_2026-10-14.md). Namensanfaenge aus dem Entwurf
    /// (maske.html, RMA_CO, Spezifikation 3.5). Fuer TRAT, TRUK, TRFR, TRIT, TRUS, TRES, TRDE und TRJP ist keine
    /// SAP-Kundennummer belegt (Luecke, Doku); "trafag gmbh" -> TRDE ist aus dem Entwurf uebernommen, aber nicht geprueft.
    /// Reihenfolge zaehlt: der erste passende Namensanfang gewinnt.
    /// </summary>
    public static readonly IReadOnlyList<MonthlyReviewCompanyCustomer> DefaultCompanyCustomers =
    [
        new() { Company = "TRIN", CustomerNumbers = ["500731", "301940", "302215", "301873"], NamePrefixes = ["trafag controls india"], Source = "KpiLiefertreueSync; Entwurf" },
        new() { Company = "TRCZ", CustomerNumbers = ["302906", "301497", "24687"], NamePrefixes = [], Source = "KpiLiefertreueSync" },
        new() { Company = "GFS", CustomerNumbers = ["19914"], NamePrefixes = [], Source = "KpiLiefertreueSync" },
        new() { Company = "TRAT", NamePrefixes = ["trafag gesellschaft"], Source = "Entwurf" },
        new() { Company = "TRUK", NamePrefixes = ["trafag (uk)"], Source = "Entwurf" },
        new() { Company = "TRFR", NamePrefixes = ["trafag s.a.r.l"], Source = "Entwurf" },
        new() { Company = "TRIT", NamePrefixes = ["trafag italia"], Source = "Entwurf" },
        new() { Company = "TRUS", NamePrefixes = ["trafag inc"], Source = "Entwurf" },
        new() { Company = "TRES", NamePrefixes = ["trafag españa", "trafag espana"], Source = "Entwurf" },
        new() { Company = "TRJP", NamePrefixes = ["trafag japan"], Source = "Entwurf" },
        new() { Company = TrchInternal, NamePrefixes = ["trafag ag"], Source = "Entwurf" },
        new() { Company = "TRDE", NamePrefixes = ["trafag gmbh"], Source = "Entwurf, ungeprueft" },
    ];

    /// <summary>Altersklassen der Grafik "Offen seit".</summary>
    public static readonly IReadOnlyList<(string Label, string English, int From, int To)> AgeBuckets =
    [
        ("≤ 30 Tage", "≤ 30 days", int.MinValue, 30),
        ("31–60 Tage", "31–60 days", 31, 60),
        ("61–90 Tage", "61–90 days", 61, 90),
        ("91–180 Tage", "91–180 days", 91, 180),
        ("> 180 Tage", "> 180 days", 181, int.MaxValue)
    ];

    // ---------- Lesen ----------

    /// <summary>Zeilen aus einer OData-Antwort (V2 <c>d.results</c>, sonst <c>d</c> als Liste oder V4 <c>value</c>).</summary>
    public static IReadOnlyList<MonthlyReviewQmRow> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ParseElements(Results(document.RootElement));
    }

    internal static IEnumerable<JsonElement> Results(JsonElement root)
    {
        if (root.TryGetProperty("d", out var d))
        {
            if (d.ValueKind == JsonValueKind.Object && d.TryGetProperty("results", out var r) && r.ValueKind == JsonValueKind.Array)
                return r.EnumerateArray().Select(e => e.Clone()).ToList();
            if (d.ValueKind == JsonValueKind.Array)
                return d.EnumerateArray().Select(e => e.Clone()).ToList();
        }
        if (root.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Array)
            return v.EnumerateArray().Select(e => e.Clone()).ToList();
        throw new InvalidOperationException("SAP QmMeldungSet: Antwort ohne d.results.");
    }

    public static IReadOnlyList<MonthlyReviewQmRow> ParseElements(IEnumerable<JsonElement> rows)
        => rows.Select(x => new MonthlyReviewQmRow(
                Text(x, "Qmnum").TrimStart('0') is { Length: > 0 } n ? n : Text(x, "Qmnum"),
                Text(x, "Qmart"),
                Text(x, "Status"),
                Text(x, "Sysstat"),
                Text(x, "Kunnr"),
                Text(x, "Kunde"),
                Material(Text(x, "Matnr")),
                Text(x, "Maktx"),
                Decimal(x, "Menge"),
                Text(x, "Mengeneinheit"),
                Text(x, "Fehlerbild"),
                Date(Text(x, "Angelegt")),
                Date(Text(x, "Ruecklieferung")),
                Date(Text(x, "Produktion")),
                Date(Text(x, "Massnahme")),
                Text(x, "Bearbeiter"),
                Text(x, "Produktlinie")))
            .ToList();

    /// <summary>Datum aus JJJJMMTT; leer, 00000000 oder ungueltig ergibt null. ISO (JJJJ-MM-TT) wird ebenfalls gelesen.</summary>
    public static DateOnly? Date(string value)
    {
        var v = value.Trim();
        if (v.Length == 8 && v != "00000000" &&
            DateOnly.TryParseExact(v, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (v.Length >= 10 && DateOnly.TryParseExact(v[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
            return iso;
        return null;
    }

    private static string Text(JsonElement x, string name)
        => x.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.String => v.GetString()?.Trim() ?? string.Empty,
                JsonValueKind.Number => v.GetRawText(),
                _ => string.Empty
            }
            : string.Empty;

    /// <summary>Edm.Decimal kommt in OData V2 als Zeichenkette ("5.000"), manchmal als Zahl.</summary>
    private static decimal? Decimal(JsonElement x, string name)
    {
        if (!x.TryGetProperty(name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var n)) return n;
        if (v.ValueKind == JsonValueKind.String &&
            decimal.TryParse(v.GetString()?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var s))
            return s;
        return null;
    }

    private static string Material(string matnr)
    {
        var m = matnr.Trim();
        return m.Length > 0 && m.All(char.IsAsciiDigit) ? (m.TrimStart('0') is { Length: > 0 } t ? t : "0") : m;
    }

    // ---------- Zuordnung ----------

    /// <summary>Kundennummer ohne fuehrende Nullen (rein numerisch), sonst getrimmt.</summary>
    public static string NormalizeCustomerNumber(string number)
    {
        var n = number.Trim();
        return n.Length > 0 && n.All(char.IsAsciiDigit) ? (n.TrimStart('0') is { Length: > 0 } t ? t : "0") : n.ToUpperInvariant();
    }

    /// <summary>
    /// Gesellschaft zum Kunden der Meldung: zuerst Kundennummer (konfigurierte vor eingebauten), dann Namensanfang
    /// (ohne Gross-/Kleinschreibung); sonst Endkunde (TRCH).
    /// </summary>
    public static string AssignCompany(string customerNumber, string customerName, IReadOnlyList<MonthlyReviewCompanyCustomer> map)
    {
        var number = NormalizeCustomerNumber(customerNumber);
        if (number.Length > 0)
            foreach (var entry in map)
                if (entry.CustomerNumbers.Any(n => NormalizeCustomerNumber(n) == number))
                    return entry.Company.Trim();
        var name = customerName.Trim().ToLowerInvariant();
        if (name.Length > 0)
            foreach (var entry in map)
                if (entry.NamePrefixes.Any(p => p.Trim().Length > 0 && name.StartsWith(p.Trim().ToLowerInvariant(), StringComparison.Ordinal)))
                    return entry.Company.Trim();
        return EndCustomer;
    }

    /// <summary>Zuordnungsliste: konfigurierte Eintraege zuerst, dann die eingebauten Vorgaben.</summary>
    public static IReadOnlyList<MonthlyReviewCompanyCustomer> CompanyMap(MonthlyReviewOptions options)
        => options.QmCompanyCustomers.Where(c => !string.IsNullOrWhiteSpace(c.Company)).Concat(DefaultCompanyCustomers).ToList();

    /// <summary>Meldung zaehlt zur Monthly Review: Meldungsart passt und kein Abschluss-/Loeschstatus.</summary>
    public static bool IsOpenRelevant(MonthlyReviewQmRow row, MonthlyReviewOptions options)
    {
        var types = options.QmNotificationTypes.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (types.Count > 0 && !types.Any(t => string.Equals(t.Trim(), row.Type, StringComparison.OrdinalIgnoreCase)))
            return false;
        var sys = row.SystemStatus.ToUpperInvariant();
        return !options.QmExcludeSystemStatus.Any(s => s.Trim().Length > 0 && sys.Contains(s.Trim().ToUpperInvariant(), StringComparison.Ordinal));
    }

    /// <summary>Offene Faelle mit Gesellschaft und Alter; Stichtag ist das Abrufdatum. Doppelte Meldungsnummern zaehlen einmal.</summary>
    public static IReadOnlyList<MonthlyReviewQmCase> BuildCases(IEnumerable<MonthlyReviewQmRow> rows, MonthlyReviewOptions options, DateOnly reference)
    {
        var map = CompanyMap(options);
        return rows.Where(r => IsOpenRelevant(r, options))
            .GroupBy(r => r.Number.Length > 0 ? r.Number : Guid.NewGuid().ToString())
            .Select(g => g.First())
            .Select(r => new MonthlyReviewQmCase(r, AssignCompany(r.CustomerNumber, r.CustomerName, map),
                Days(r.Created, reference), Days(r.Returned, reference),
                r.MeasureCreated is { } m && r.Created is { } c ? m.DayNumber - c.DayNumber : null))
            .ToList();
    }

    private static int? Days(DateOnly? from, DateOnly reference) => from is { } d ? reference.DayNumber - d.DayNumber : null;

    // ---------- Auswertung ----------

    /// <summary>Faelle einer Gesellschaft; TRCH umfasst Endkunden und die Trafag AG selbst (Spezifikation 2.4 Nr. 12).</summary>
    public static IReadOnlyList<MonthlyReviewQmCase> ForCompany(IEnumerable<MonthlyReviewQmCase> cases, string company)
        => string.Equals(company, "TRCH", StringComparison.OrdinalIgnoreCase)
            ? cases.Where(c => c.Company is EndCustomer or TrchInternal).ToList()
            : cases.Where(c => string.Equals(c.Company, company, StringComparison.OrdinalIgnoreCase)).ToList();

    public static bool IsReturnOpen(string status)
    {
        var s = status.ToLowerInvariant();
        return s.Contains("rücksendung", StringComparison.Ordinal) || s.Contains("ruecksendung", StringComparison.Ordinal);
    }

    public static MonthlyReviewQmStats Stats(IReadOnlyList<MonthlyReviewQmCase> cases)
    {
        var ages = cases.Where(c => c.AgeDays is not null).Select(c => c.AgeDays!.Value).OrderBy(a => a).ToList();
        var oldest = cases.Where(c => c.AgeDays is not null).OrderByDescending(c => c.AgeDays).ThenBy(c => c.Number, StringComparer.Ordinal).FirstOrDefault();
        return new MonthlyReviewQmStats(
            cases.Count,
            ages.Count == 0 ? null : ages.Average(),
            Median(ages),
            ages.Count == 0 ? null : ages[^1],
            ages.Count(a => a > OverdueDays),
            oldest,
            cases.Count(c => IsReturnOpen(c.Status)));
    }

    public static double? Median(IReadOnlyList<int> sorted)
    {
        if (sorted.Count == 0) return null;
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    /// <summary>Anzahl je Wert, absteigend (bei Gleichstand alphabetisch); leere Werte unter <paramref name="emptyLabel"/>.</summary>
    public static IReadOnlyList<MonthlyReviewQmCount> CountBy(IEnumerable<MonthlyReviewQmCase> cases, Func<MonthlyReviewQmCase, string> key, string emptyLabel = "–")
        => cases.GroupBy(c => key(c) is { Length: > 0 } k ? k : emptyLabel)
            .Select(g => new MonthlyReviewQmCount(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>Faelle je Altersklasse; Faelle ohne Anlagedatum zaehlen nicht.</summary>
    public static IReadOnlyList<MonthlyReviewQmCount> ByAge(IEnumerable<MonthlyReviewQmCase> cases)
    {
        var list = cases.Where(c => c.AgeDays is not null).ToList();
        return AgeBuckets.Select(b => new MonthlyReviewQmCount(b.Label, list.Count(c => c.AgeDays >= b.From && c.AgeDays <= b.To))).ToList();
    }

    public static IReadOnlyList<MonthlyReviewQmStatusAge> StatusAge(IEnumerable<MonthlyReviewQmCase> cases)
        => cases.GroupBy(c => c.Status.Length > 0 ? c.Status : "–")
            .Select(g =>
            {
                var s = Stats(g.ToList());
                return new MonthlyReviewQmStatusAge(g.Key, s.Count, s.AverageDays, s.MedianDays, s.Over90);
            })
            .OrderByDescending(x => x.Cases)
            .ThenBy(x => x.Status, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    // ---------- Drilldown-Liste ----------

    public enum SortColumn { Number, Status, Company, Customer, Material, Defect, Quantity, Created, Age, Returned, ReturnAge, Measure, Processor }

    /// <summary>Filter (leer = alle), Suche ueber die angezeigten Spalten, Sortierung; Leerwerte stehen immer am Ende.</summary>
    public static IReadOnlyList<MonthlyReviewQmCase> Query(IEnumerable<MonthlyReviewQmCase> cases, string? company, string? status, string? processor,
        string? search, SortColumn sort, bool descending)
    {
        var q = cases.Where(c =>
            (string.IsNullOrEmpty(company) || c.Company == company) &&
            (string.IsNullOrEmpty(status) || c.Status == status) &&
            (string.IsNullOrEmpty(processor) || c.Processor == processor));
        var term = (search ?? string.Empty).Trim();
        if (term.Length > 0)
            q = q.Where(c => SearchText(c).Contains(term, StringComparison.OrdinalIgnoreCase));
        var list = q.ToList();
        IComparable? Key(MonthlyReviewQmCase c) => sort switch
        {
            SortColumn.Number => c.Number.PadLeft(20, '0'),
            SortColumn.Status => NullIfEmpty(c.Status),
            SortColumn.Company => NullIfEmpty(c.Company),
            SortColumn.Customer => NullIfEmpty(c.Row.CustomerName),
            SortColumn.Material => NullIfEmpty(c.Row.MaterialText),
            SortColumn.Defect => NullIfEmpty(c.Row.Defect),
            SortColumn.Quantity => c.Row.Quantity,
            SortColumn.Created => c.Row.Created,
            SortColumn.Age => c.AgeDays,
            SortColumn.Returned => c.Row.Returned,
            SortColumn.ReturnAge => c.DaysSinceReturn,
            SortColumn.Measure => c.Row.MeasureCreated,
            SortColumn.Processor => NullIfEmpty(c.Processor),
            _ => null
        };
        var withValue = list.Where(c => Key(c) is not null);
        var ordered = descending
            ? withValue.OrderByDescending(Key, Comparer<IComparable?>.Create(CompareKeys))
            : withValue.OrderBy(Key, Comparer<IComparable?>.Create(CompareKeys));
        return ordered.ThenBy(c => c.Number, StringComparer.Ordinal).Concat(list.Where(c => Key(c) is null)).ToList();
    }

    private static IComparable? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    private static int CompareKeys(IComparable? a, IComparable? b)
        => a is string sa && b is string sb ? string.Compare(sa, sb, StringComparison.CurrentCultureIgnoreCase) : Comparer<IComparable?>.Default.Compare(a, b);

    private static string SearchText(MonthlyReviewQmCase c)
        => string.Join(' ', c.Number, c.Status, c.Company, c.Row.CustomerName, c.Row.CustomerNumber, c.Row.Material, c.Row.MaterialText,
            c.Row.Defect, c.Processor, c.Row.ProductLine);

    // ---------- CSV ----------

    public static string Format(DateOnly? d) => d is { } x ? x.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;

    /// <summary>CSV fuer Excel: UTF-8 mit BOM, Trennzeichen Semikolon, CRLF, Spalten wie Spezifikation 2.2.</summary>
    public static byte[] ToCsv(IEnumerable<MonthlyReviewQmCase> cases)
    {
        var sb = new StringBuilder();
        string[] header =
        [
            "Meldung", "Meldungsart", "Status", "Systemstatus", "Gesellschaft", "Kundennummer", "Kunde", "Materialnummer", "Materialkurztext",
            "Reklamierte Menge", "Mengeneinheit", "Fehlerbild", "Produktlinie", "Angelegt am", "Offen seit (Tage)", "Rücklieferungsdatum",
            "Tage seit Rücklieferung", "Produktionsdatum", "Massnahme angelegt am", "Tage bis Massnahme", "Bearbeiter"
        ];
        sb.Append(string.Join(';', header.Select(Csv))).Append("\r\n");
        foreach (var c in cases)
        {
            var r = c.Row;
            string[] cells =
            [
                c.Number, r.Type, r.Status, r.SystemStatus, c.Company, r.CustomerNumber, r.CustomerName, r.Material, r.MaterialText,
                r.Quantity?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty, r.Unit, r.Defect, r.ProductLine,
                Format(r.Created), c.AgeDays?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, Format(r.Returned),
                c.DaysSinceReturn?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, Format(r.Production), Format(r.MeasureCreated),
                c.DaysToMeasure?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, c.Processor
            ];
            sb.Append(string.Join(';', cells.Select(Csv))).Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Csv(string value)
        => value.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}
