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

/// <summary>Eine Spalte, die der Leser im Cockpit liest, mit allen Schreibweisen, die er annimmt.</summary>
public sealed record RexxHubReaderColumn(string Name, RexxHubColumnKind Kind, bool Pflicht, params string[] Aliases)
{
    public IEnumerable<string> AllNames => new[] { Name }.Concat(Aliases);
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

    /// <summary>Geschrieben wird nur mit mindestens einer Zeile, allen Pflichtspalten und ohne doppelte Spalten.</summary>
    public bool KannSchreiben => Zeilen > 0 && FehlendePflichtspalten.Count == 0 && DoppelteSpalten.Count == 0;

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
/// ohne Austrittsart keine fluktuationsrelevanten Austritte. #777: Personalnummer, Abwesenheit von (sonst verwirft der Leser
/// die Zeile), Abwesenheit Buchung (Art) und Abwesenheit Status (nur „Genommen“/„Genehmigt“ zaehlen).
/// </summary>
public static class RexxHubColumns
{
    public static readonly IReadOnlyList<RexxHubReaderColumn> Gesamt =
    [
        new("Personalnummer", RexxHubColumnKind.Ganzzahl, true),
        new("Nachname, Vorname (Link Personal)", RexxHubColumnKind.Text, false),
        new("Personal Status", RexxHubColumnKind.Text, true),
        new("Organisation", RexxHubColumnKind.Text, false),
        new("Kostenstelle", RexxHubColumnKind.Text, false),
        new("Stelle", RexxHubColumnKind.Text, false),
        new("Eintrittsdatum", RexxHubColumnKind.Datum, true),
        new("Austrittsdatum", RexxHubColumnKind.Datum, true),
        new("Austrittsart", RexxHubColumnKind.Text, true),
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
        new("Personalnummer", RexxHubColumnKind.Ganzzahl, true),
        new("Abwesenheit von", RexxHubColumnKind.Datum, true),
        new("Abwesenheit bis", RexxHubColumnKind.Datum, false),
        new("Abwesenheit Buchung", RexxHubColumnKind.Text, true),
        new("Abwesenheit Status", RexxHubColumnKind.Text, true),
        new("Abwesenheit Tage", RexxHubColumnKind.Zahl, false),
        new("Abwesenheit Tagessoll", RexxHubColumnKind.Stunden, false)
    ];

    public static IReadOnlyList<RexxHubReaderColumn> For(RexxHubQueryKind kind)
        => kind == RexxHubQueryKind.Gesamt ? Gesamt : Faelle;

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

    /// <summary>Prueft Spalten und Formate einer Antwort; liest Werte nur zum Zaehlen.</summary>
    public static RexxHubColumnCheck Check(RexxHubQueryKind kind, RexxHubTable table)
    {
        var present = table.Columns.Select(NormalizeHeader).ToHashSet(StringComparer.Ordinal);
        var readerColumns = For(kind);
        bool Has(RexxHubReaderColumn c) => c.AllNames.Any(n => present.Contains(NormalizeHeader(n)));

        var formats = new List<RexxHubFormatCheck>();
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
            }
            formats.Add(new RexxHubFormatCheck(column, reader.Kind.ToString(), filled, recognized));
        }

        return new RexxHubColumnCheck
        {
            Zeilen = table.Rows.Count,
            FehlendePflichtspalten = readerColumns.Where(c => c.Pflicht && !Has(c)).Select(c => c.Name).ToList(),
            FehlendeLeserspalten = readerColumns.Where(c => !Has(c)).Select(c => c.Name).ToList(),
            ZusaetzlicheSpalten = table.Columns.Where(c => Find(kind, c) is null).ToList(),
            DoppelteSpalten = table.Columns.GroupBy(NormalizeHeader).Where(g => g.Count() > 1).Select(g => g.First()).ToList(),
            Formatpruefung = formats
        };
    }

    private static bool IsEmpty(object? value)
        => value is null || value is string s && string.IsNullOrWhiteSpace(s);

    private static readonly Regex HoursPattern = new(@"^-?\d+(:\d{1,2}(:\d{1,2})?)?([.,]\d+)?$", RegexOptions.CultureInvariant);
    private static readonly Regex PeriodPattern = new(@"\d{2}\.\d{2}\.\d{4}", RegexOptions.CultureInvariant);

    /// <summary>Erkennt ein Wert das Format, das der Leser erwartet? Datum: Datumswert der Antwort oder ein lesbarer Text.</summary>
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
            RexxHubColumnKind.Zahl => value is decimal || decimal.TryParse(text.Replace("'", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out _)
                || decimal.TryParse(text.Replace("'", ""), NumberStyles.Number, CultureInfo.GetCultureInfo("de-DE"), out _),
            RexxHubColumnKind.Datum => RexxHubWorkbook.TryParseIsoDate(text, out _)
                || DateTime.TryParse(text, CultureInfo.GetCultureInfo("de-CH"), DateTimeStyles.None, out _),
            RexxHubColumnKind.Stunden => value is decimal || HoursPattern.IsMatch(text),
            RexxHubColumnKind.Zeitraum => PeriodPattern.IsMatch(text),
            _ => true
        };
    }
}
