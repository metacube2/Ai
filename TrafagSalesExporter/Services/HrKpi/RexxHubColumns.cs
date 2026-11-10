using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TrafagSalesExporter.Services;

/// <summary>Welche Rexx-Abfrage: #775 (eine Zeile je Person) oder #777 (eine Zeile je Abwesenheit).</summary>
public enum RexxHubQueryKind
{
    Gesamt,
    Faelle
}

/// <summary>Wie der Leser im HR-Cockpit eine Spalte liest; danach richtet sich die Formatpruefung und das Schreiben.</summary>
public enum RexxHubColumnKind
{
    Text,
    Ganzzahl,
    Zahl,
    Datum,
    /// <summary>„hh:mm“ (auch negativ) oder Zahl, gelesen ueber ParseTimeBalance.</summary>
    Stunden,
    /// <summary>Text mit „TT.MM.JJJJ“ oder „TT.MM.JJJJ - TT.MM.JJJJ“, gelesen ueber ParseRexxPeriod.</summary>
    Zeitraum
}

/// <summary>Wie voll eine Pflichtspalte sein muss, damit geschrieben wird.</summary>
public enum RexxHubFill
{
    /// <summary>Keine Vorgabe.</summary>
    Keine,
    /// <summary>Mindestens ein Wert (die Spalte ist nicht nur dem Namen nach da).</summary>
    Mindestens1,
    /// <summary>Nahezu jede Zeile (Anteil <c>HrKpi:RexxHub:MinShare</c>).</summary>
    Fast,
    /// <summary>Mindestens ein Wert unter den Zeilen mit Status „Ausgeschieden“, falls es solche gibt (#775).</summary>
    BeiAustritt
}

/// <summary>Eine Spalte, die der Leser im Cockpit liest, mit allen Schreibweisen, die er annimmt.</summary>
public sealed record RexxHubReaderColumn(string Name, RexxHubColumnKind Kind, bool Pflicht, params string[] Aliases)
{
    public IEnumerable<string> AllNames => new[] { Name }.Concat(Aliases);

    public RexxHubFill Fuellung { get; init; } = RexxHubFill.Keine;
}

/// <summary>Formatpruefung einer Spalte: nur Zaehlwerte, nie Inhalte (Personendaten).</summary>
public sealed record RexxHubFormatCheck(string Spalte, string Art, int Gefuellt, int Erkannt);

/// <summary>Ergebnis der Pruefung vor dem Schreiben.</summary>
public sealed class RexxHubColumnCheck
{
    public int Zeilen { get; init; }
    public List<string> FehlendePflichtspalten { get; init; } = [];
    /// <summary>Spalten, die der Leser kennt (Handexport) und die die API nicht liefert, Pflicht oder nicht.</summary>
    public List<string> FehlendeLeserspalten { get; init; } = [];
    /// <summary>Spalten der API, die der Leser nicht liest (werden trotzdem geschrieben, wie im Handexport).</summary>
    public List<string> ZusaetzlicheSpalten { get; init; } = [];
    /// <summary>Spalten, die nach ColumnMap doppelt vorkommen; der Leser naehme nur die erste.</summary>
    public List<string> DoppelteSpalten { get; init; } = [];
    public List<RexxHubFormatCheck> Formatpruefung { get; init; } = [];

    /// <summary>
    /// Wertpruefung der Auswahlfelder, nur Zaehlwerte: wie viele gefuellte Werte das Cockpit als bekannten Status bzw. als
    /// ausgewertete Art erkennt. Das Produktdokument (3.3) erlaubt je Endpunkt ein „Auswahlwerte-Mapping“, dann kaemen Codes
    /// statt der Rexx-Texte und das Cockpit faende niemanden.
    /// </summary>
    public List<RexxHubFormatCheck> Wertpruefung { get; init; } = [];

    /// <summary>#775: Zeilen, deren Personalnummer schon in einer frueheren Zeile steht (z. B. eine Seite doppelt geliefert).</summary>
    public int DoppeltePersonalnummern { get; init; }

    /// <summary>
    /// Datumswerte mit Uhrzeit oder Zeitzone („…T23:00:00Z“). Sie werden mit dem Datum geschrieben, wie es dasteht, ohne
    /// Umrechnung nach Europe/Zurich; ob Rexx ein Schweizer Datum als UTC-Zeitstempel schickt, ist offen. Nur Zaehlwert.
    /// </summary>
    public int DatumswerteMitZeitanteil { get; init; }

    /// <summary>Zeilen der bisherigen Datei am Zielpfad, falls lesbar.</summary>
    public int? ZeilenBisher { get; init; }

    /// <summary>Weitere Gruende, nicht zu schreiben (Fuellung, Format der Pflichtspalten, Werte, Zeilenzahl), nur Namen und Zahlen.</summary>
    public List<string> Sperrgruende { get; init; } = [];

    /// <summary>Hinweise, die das Schreiben nicht verhindern (gehen als Warnung ins Protokoll).</summary>
    public List<string> Hinweise { get; init; } = [];

    /// <summary>Geschrieben wird nur mit mindestens einer Zeile, allen Pflichtspalten, ohne doppelte Spalten und ohne Sperrgrund.</summary>
    public bool KannSchreiben => Zeilen > 0 && FehlendePflichtspalten.Count == 0 && DoppelteSpalten.Count == 0 && Sperrgruende.Count == 0;

    /// <summary>Spalten, deren Werte nicht alle im erwarteten Format kommen (nur Namen und Zahlen).</summary>
    public IEnumerable<string> Formatabweichungen
        => Formatpruefung.Where(x => x.Erkannt < x.Gefuellt).Select(x => $"{x.Spalte} ({x.Erkannt} von {x.Gefuellt} {x.Art})");

    /// <summary>Warum nicht geschrieben wird, als ein Satz ohne Werte.</summary>
    public string Ablehnungsgrund()
    {
        var parts = new List<string>();
        if (Zeilen == 0)
            parts.Add("keine Zeile geliefert");
        if (FehlendePflichtspalten.Count > 0)
            parts.Add("Pflichtspalten fehlen: " + string.Join("; ", FehlendePflichtspalten));
        if (DoppelteSpalten.Count > 0)
            parts.Add("Spalten doppelt: " + string.Join("; ", DoppelteSpalten));
        parts.AddRange(Sperrgruende);
        return string.Join(". ", parts);
    }
}

/// <summary>
/// Die Spalten, die <c>HrKpiDashboardBuilder.LoadGesamtRows</c> (#775) und <c>LoadRexxCases</c> (#777) lesen, mit denselben
/// Schreibweisen (Aliase) wie dort. Bei einer Aenderung der Leser hier nachziehen; der Rundreise-Test in
/// <c>RexxHubTests</c> faellt sonst auf.
///
/// PFLICHT heisst: ohne diese Spalte rechnet das Cockpit Kernzahlen still falsch, statt nur eine Kachel leer zu lassen.
/// #775: ohne Personalnummer kein Abgleich mit SAP, ohne Personal Status keine aktive Person und kein Austritt
/// (<c>IsActiveStatus</c>/<c>IsLeftStatus</c>), ohne Eintritts-/Austrittsdatum kein Headcount-Verlauf und keine Fluktuation,
/// ohne Austrittsart keine fluktuationsrelevanten Austritte. Seit der Pruefung 2026-11-10 auch: ohne „Stelle“ zaehlt
/// <c>BuildEmployeeType("")</c> jede Person als „Festangestellt“ (Praktikanten, Lehrlinge und Aushilfen kaemen in Headcount
/// und Fluktuation), ohne „Nachname, Vorname (Link Personal)“ schliesst <c>IsExcludedTestPerson</c> keine Testperson aus.
/// #777: Personalnummer, Abwesenheit von (sonst verwirft der Leser die Zeile), Abwesenheit Buchung (Art) und Abwesenheit
/// Status (nur „Genommen“/„Genehmigt“ zaehlen).
///
/// Vorhanden heisst nicht nur dem Namen nach: <see cref="RexxHubReaderColumn.Fuellung"/> verlangt Werte (eine Kopfzeile
/// aus „columns“ kann Namen nennen, die in den Zeilen nie vorkommen).
/// </summary>
public static class RexxHubColumns
{
    public static readonly IReadOnlyList<RexxHubReaderColumn> Gesamt =
    [
        new("Personalnummer", RexxHubColumnKind.Ganzzahl, true) { Fuellung = RexxHubFill.Fast },
        new("Nachname, Vorname (Link Personal)", RexxHubColumnKind.Text, true) { Fuellung = RexxHubFill.Mindestens1 },
        new("Personal Status", RexxHubColumnKind.Text, true) { Fuellung = RexxHubFill.Fast },
        new("Organisation", RexxHubColumnKind.Text, false),
        new("Kostenstelle", RexxHubColumnKind.Text, false),
        new("Stelle", RexxHubColumnKind.Text, true) { Fuellung = RexxHubFill.Mindestens1 },
        new("Eintrittsdatum", RexxHubColumnKind.Datum, true) { Fuellung = RexxHubFill.Fast },
        new("Austrittsdatum", RexxHubColumnKind.Datum, true) { Fuellung = RexxHubFill.BeiAustritt },
        new("Austrittsart", RexxHubColumnKind.Text, true) { Fuellung = RexxHubFill.BeiAustritt },
        new("Geburtsdatum", RexxHubColumnKind.Datum, false),
        new("Ø tägliche Sollarbeitszeit (Woche)", RexxHubColumnKind.Zahl, false, "O taegliche Sollarbeitszeit (Woche)"),
        new("Arbeitszeitmodell", RexxHubColumnKind.Text, false),
        new("Stunden Saldo", RexxHubColumnKind.Stunden, false),
        new("Urlaubsanspruch", RexxHubColumnKind.Zahl, false),
        new("Urlaub Rest", RexxHubColumnKind.Zahl, false),
        new("Ferien ausstehend (Tage)", RexxHubColumnKind.Zahl, false),
        new("Ferien angetreten (Tage)", RexxHubColumnKind.Zahl, false),
        new("Krankheit angetreten (Stunden)", RexxHubColumnKind.Stunden, false),
        new("Krankheit (über 60 Tage) angetreten (Stunden)", RexxHubColumnKind.Stunden, false, "Krankheit (ueber 60 Tage) angetreten (Stunden)"),
        new("Unfall angetreten (Stunden)", RexxHubColumnKind.Stunden, false),
        new("Kompensation angetreten (Stunden)", RexxHubColumnKind.Stunden, false),
        new("Militär / Zivildienst angetreten (Stunden)", RexxHubColumnKind.Stunden, false, "Militaer / Zivildienst angetreten (Stunden)"),
        new("Krankheit angetreten (Zeitraum)", RexxHubColumnKind.Zeitraum, false),
        new("Krankheit (über 60 Tage) angetreten (Zeitraum)", RexxHubColumnKind.Zeitraum, false, "Krankheit (ueber 60 Tage) angetreten (Zeitraum)"),
        new("Unfall angetreten (Zeitraum)", RexxHubColumnKind.Zeitraum, false)
    ];

    public static readonly IReadOnlyList<RexxHubReaderColumn> Faelle =
    [
        new("Personalnummer", RexxHubColumnKind.Ganzzahl, true) { Fuellung = RexxHubFill.Fast },
        new("Abwesenheit von", RexxHubColumnKind.Datum, true) { Fuellung = RexxHubFill.Fast },
        new("Abwesenheit bis", RexxHubColumnKind.Datum, false),
        new("Abwesenheit Buchung", RexxHubColumnKind.Text, true) { Fuellung = RexxHubFill.Fast },
        new("Abwesenheit Status", RexxHubColumnKind.Text, true) { Fuellung = RexxHubFill.Fast },
        new("Abwesenheit Tage", RexxHubColumnKind.Zahl, false),
        new("Abwesenheit Tagessoll", RexxHubColumnKind.Stunden, false)
    ];

    public static IReadOnlyList<RexxHubReaderColumn> For(RexxHubQueryKind kind)
        => kind == RexxHubQueryKind.Gesamt ? Gesamt : Faelle;

    /// <summary>Liest das Cockpit eine Spalte dieses Namens in #775 oder #777? Fuer die Kopfzeilen-Erkennung des Parsers.</summary>
    public static bool IsKnown(string header)
        => !string.IsNullOrWhiteSpace(header) && (Find(RexxHubQueryKind.Gesamt, header) is not null || Find(RexxHubQueryKind.Faelle, header) is not null);

    /// <summary>
    /// Gleiche Normalisierung wie <c>HrKpiDashboardBuilder.NormalizeHeader</c> (dort privat und bewusst nicht geaendert):
    /// Akzente weg, „ø“ als „o“, nur Buchstaben und Ziffern, klein. Damit gilt eine Spalte als vorhanden, wenn der Leser
    /// sie findet.
    /// </summary>
    public static string NormalizeHeader(string value)
    {
        var decomposed = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            var lower = char.ToLowerInvariant(ch);
            if (lower == 'ø')
                builder.Append('o');
            else if (char.IsLetterOrDigit(lower))
                builder.Append(lower);
        }
        return builder.ToString();
    }

    /// <summary>Die Leserspalte zu einer Ueberschrift der API, oder null, wenn der Leser sie nicht liest.</summary>
    public static RexxHubReaderColumn? Find(RexxHubQueryKind kind, string header)
    {
        var key = NormalizeHeader(header);
        return For(kind).FirstOrDefault(c => c.AllNames.Any(n => NormalizeHeader(n) == key));
    }

    /// <summary>
    /// Prueft Spalten, Formate und Werte einer Antwort; liest Werte nur zum Zaehlen. <paramref name="previousRows"/> ist die
    /// Zeilenzahl der bisherigen Datei (null = keine oder nicht lesbar). Schwellen aus <c>HrKpi:RexxHub</c>.
    /// </summary>
    public static RexxHubColumnCheck Check(
        RexxHubQueryKind kind, RexxHubTable table, int? previousRows = null, double minShare = 0.95, double minRowShareOfPrevious = 0.8,
        int maxDuplicates = 0)
    {
        var present = table.Columns.Select(NormalizeHeader).ToHashSet(StringComparer.Ordinal);
        var readerColumns = For(kind);
        bool Has(RexxHubReaderColumn c) => c.AllNames.Any(n => present.Contains(NormalizeHeader(n)));
        // Die Spalte der Antwort, unter der der Leser die Leserspalte findet (die erste, wie FindHeader im Leser).
        string? ColumnOf(RexxHubReaderColumn c) => table.Columns.FirstOrDefault(x => Find(kind, x) == c);
        var share = Math.Clamp(minShare, 0, 1);

        var formats = new List<RexxHubFormatCheck>();
        var withTime = 0;
        foreach (var column in table.Columns)
        {
            var reader = Find(kind, column);
            if (reader is null || reader.Kind == RexxHubColumnKind.Text)
                continue;
            var filled = 0;
            var recognized = 0;
            foreach (var row in table.Rows)
            {
                if (!row.TryGetValue(column, out var value) || IsEmpty(value))
                    continue;
                filled++;
                if (Recognized(reader.Kind, value))
                    recognized++;
                if (reader.Kind == RexxHubColumnKind.Datum && value is string s && RexxHubWorkbook.HasTimePart(s))
                    withTime++;
            }
            formats.Add(new RexxHubFormatCheck(column, reader.Kind.ToString(), filled, recognized));
        }

        var blocks = new List<string>();
        var notes = new List<string>();
        var values = new List<RexxHubFormatCheck>();
        var duplicates = 0;

        // Zeilen mit Status „Ausgeschieden“ (#775); fuer die Fuellung von Austrittsdatum und Austrittsart.
        var statusColumn = kind == RexxHubQueryKind.Gesamt ? ColumnOf(Gesamt[2]) : null;
        var leftRows = statusColumn is null
            ? []
            : table.Rows.Where(r => HrKpiDashboardBuilder.IsLeftStatus(Text(r, statusColumn))).ToList();

        foreach (var reader in readerColumns.Where(c => c.Pflicht && Has(c)))
        {
            var column = ColumnOf(reader);
            if (column is null)
                continue;
            var filled = table.Rows.Count(r => r.TryGetValue(column, out var v) && !IsEmpty(v));
            switch (reader.Fuellung)
            {
                case RexxHubFill.Mindestens1 when filled == 0:
                    blocks.Add($"{reader.Name}: Spalte vorhanden, aber in keiner Zeile gefuellt");
                    break;
                case RexxHubFill.Fast when table.Rows.Count > 0 && filled < share * table.Rows.Count:
                    blocks.Add($"{reader.Name}: nur {filled} von {table.Rows.Count} Zeilen gefuellt (verlangt {share:P0})");
                    break;
                case RexxHubFill.BeiAustritt when leftRows.Count > 0 &&
                                                 !leftRows.Any(r => r.TryGetValue(column, out var v) && !IsEmpty(v)):
                    blocks.Add($"{reader.Name}: bei keiner der {leftRows.Count} ausgeschiedenen Personen gefuellt");
                    break;
            }

            // Pflichtspalten als Ganzzahl oder Datum: liest der Leser sie nicht, ist die Person nicht zuzuordnen bzw. die Datei
            // liest sich leer (eine Zahl ausserhalb des Excel-Datumsbereichs laesst ReadDate werfen, gemessen 2026-11-10).
            if (reader.Kind is RexxHubColumnKind.Ganzzahl or RexxHubColumnKind.Datum &&
                formats.FirstOrDefault(f => f.Spalte == column) is { Gefuellt: > 0 } format &&
                format.Erkannt < share * format.Gefuellt)
                blocks.Add($"{reader.Name}: nur {format.Erkannt} von {format.Gefuellt} Werten als {format.Art} erkannt (verlangt {share:P0})");
        }

        if (kind == RexxHubQueryKind.Gesamt)
        {
            if (statusColumn is not null)
            {
                var filled = table.Rows.Count(r => !string.IsNullOrWhiteSpace(Text(r, statusColumn)));
                var active = table.Rows.Count(r => HrKpiDashboardBuilder.IsActiveStatus(Text(r, statusColumn)));
                values.Add(new RexxHubFormatCheck(statusColumn, "angestellt (" + string.Join("/", HrKpiDashboardBuilder.ActiveStatuses) + ")", filled, active));
                values.Add(new RexxHubFormatCheck(statusColumn, "Ausgeschieden", filled, leftRows.Count));
                if (filled > 0 && active == 0)
                    blocks.Add($"Personal Status: kein Wert als angestellt erkannt (0 von {filled}; erwartet {string.Join(", ", HrKpiDashboardBuilder.ActiveStatuses)}), Headcount waere 0");
                else if (filled > 0 && leftRows.Count == 0)
                    notes.Add("Personal Status: keine Person mit „Ausgeschieden“, Austritte und Fluktuation waeren 0");
            }

            if (ColumnOf(Gesamt[0]) is { } pernrColumn)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var row in table.Rows)
                {
                    var key = Text(row, pernrColumn).TrimStart('0');
                    if (key.Length > 0 && !seen.Add(key))
                        duplicates++;
                }
                if (duplicates > Math.Max(0, maxDuplicates))
                    blocks.Add($"Personalnummer: {duplicates} Zeilen mit einer Nummer, die schon vorkommt (Seite doppelt geliefert? Headcount zaehlte doppelt)");
            }
        }
        else
        {
            var typeColumn = ColumnOf(Faelle[3]);
            var statusCaseColumn = ColumnOf(Faelle[4]);
            if (typeColumn is not null && statusCaseColumn is not null)
            {
                bool KnownType(Dictionary<string, object?> r) => HrKpiDashboardBuilder.RexxCaseTypes.ContainsKey(Text(r, typeColumn));
                bool KnownStatus(Dictionary<string, object?> r)
                    => HrKpiDashboardBuilder.RexxCaseStatuses.Any(s => string.Equals(s, Text(r, statusCaseColumn), StringComparison.OrdinalIgnoreCase));
                var typesFilled = table.Rows.Count(r => !string.IsNullOrWhiteSpace(Text(r, typeColumn)));
                var statusFilled = table.Rows.Count(r => !string.IsNullOrWhiteSpace(Text(r, statusCaseColumn)));
                var both = table.Rows.Count(r => KnownType(r) && KnownStatus(r));
                values.Add(new RexxHubFormatCheck(typeColumn, "ausgewertete Art", typesFilled, table.Rows.Count(KnownType)));
                values.Add(new RexxHubFormatCheck(statusCaseColumn, "bestaetigt (" + string.Join("/", HrKpiDashboardBuilder.RexxCaseStatuses) + ")", statusFilled, table.Rows.Count(KnownStatus)));
                if (table.Rows.Count > 0 && both == 0)
                    blocks.Add($"Abwesenheit Buchung/Status: kein Fall mit ausgewerteter Art und bestaetigtem Status (0 von {table.Rows.Count}), alle Rexx-Faelle fielen weg");
            }
        }

        if (previousRows is > 0 && minRowShareOfPrevious > 0 && table.Rows.Count < minRowShareOfPrevious * previousRows.Value)
            blocks.Add($"nur {table.Rows.Count} Zeilen, die bisherige Datei hat {previousRows} (verlangt mindestens {minRowShareOfPrevious:P0}; Teilabruf oder eingeschraenkte Rechte?)");

        if (withTime > 0)
            notes.Add($"{withTime} Datumswerte mit Uhrzeit oder Zeitzone, ohne Umrechnung als Datum geschrieben (offen, ob Rexx UTC schickt)");

        return new RexxHubColumnCheck
        {
            Zeilen = table.Rows.Count,
            FehlendePflichtspalten = readerColumns.Where(c => c.Pflicht && !Has(c)).Select(c => c.Name).ToList(),
            FehlendeLeserspalten = readerColumns.Where(c => !Has(c)).Select(c => c.Name).ToList(),
            ZusaetzlicheSpalten = table.Columns.Where(c => Find(kind, c) is null).ToList(),
            DoppelteSpalten = table.Columns.GroupBy(NormalizeHeader).Where(g => g.Count() > 1).Select(g => g.First()).ToList(),
            Formatpruefung = formats,
            Wertpruefung = values,
            DoppeltePersonalnummern = duplicates,
            DatumswerteMitZeitanteil = withTime,
            ZeilenBisher = previousRows,
            Sperrgruende = blocks,
            Hinweise = notes
        };
    }

    private static string Text(Dictionary<string, object?> row, string column)
        => row.TryGetValue(column, out var value) && value is not null
            ? (value is decimal d ? d.ToString(CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Trim()
            : string.Empty;

    private static bool IsEmpty(object? value)
        => value is null || value is string s && string.IsNullOrWhiteSpace(s);

    private static readonly Regex HoursPattern = new(@"^-?\d+(:\d{1,2}(:\d{1,2})?)?([.,]\d+)?$", RegexOptions.CultureInvariant);
    private static readonly Regex PeriodPattern = new(@"\d{2}\.\d{2}\.\d{4}", RegexOptions.CultureInvariant);

    /// <summary>
    /// Erkennt ein Wert das Format, das der Leser erwartet? Gleiche Regeln wie beim Schreiben (<see cref="RexxHubWorkbook"/>):
    /// Datum als ISO-Text oder Schweizer Datumstext, Zahl als Zahl oder Text mit Dezimalpunkt bzw. -komma.
    /// </summary>
    internal static bool Recognized(RexxHubColumnKind kind, object? value)
    {
        var text = value switch
        {
            string s => s.Trim(),
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
        };
        return kind switch
        {
            RexxHubColumnKind.Ganzzahl => value is decimal d2 ? d2 == decimal.Truncate(d2) : text.All(char.IsDigit) && text.Length > 0,
            RexxHubColumnKind.Zahl => value is decimal || RexxHubWorkbook.TryParseNumber(text, out _),
            RexxHubColumnKind.Datum => value is string && RexxHubWorkbook.TryParseDate(text, out _),
            RexxHubColumnKind.Stunden => value is decimal || HoursPattern.IsMatch(text),
            RexxHubColumnKind.Zeitraum => PeriodPattern.IsMatch(text),
            _ => true
        };
    }
}
