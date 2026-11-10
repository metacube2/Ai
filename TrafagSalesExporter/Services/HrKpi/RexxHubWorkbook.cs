using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace TrafagSalesExporter.Services;

/// <summary>Was am Zielpfad liegt: ob die Datei vom Abruf stammt (Blattname) und wie viele Datenzeilen sie hat.</summary>
public sealed record RexxHubExistingFile(bool Exists, bool FromHub, int? Rows);

/// <summary>
/// Schreibt eine Rexx-Hub-Antwort als Excel-Datei mit denselben Spaltenueberschriften wie der Handexport, damit
/// <c>HrKpiDashboardBuilder</c> sie unveraendert liest (Rundreise-Test in <c>RexxHubTests</c>).
///
/// Umsetzung je Spalte, nicht nach dem Aussehen eines Wertes:
/// - Spalten, die der Leser als Datum liest (Eintritts-, Austritts-, Geburtsdatum, Abwesenheit von/bis): ISO-Datum der API
///   („2009-11-04“ oder „2009-11-04T19:55:41Z“, Produktdokument Kapitel 3.3) oder ein Schweizer Datumstext wird ein
///   Excel-Datum. Alles andere bleibt LEER: eine Zahl (z. B. 20200101) oder ein Text aus Ziffern liest <c>ReadDate</c> als
///   Excel-Seriennummer, und ausserhalb des gueltigen Bereichs wirft das; dann liest sich die ganze Datei leer (gemessen
///   2026-11-10: „Not a legal OleAut date.“, 0 Zeilen). Die Pruefung vor dem Schreiben zaehlt solche Werte.
/// - Spalten, die der Leser als Zahl liest (Urlaub, Ferien, Sollzeit, Abwesenheit Tage): JSON-Zahlen und Texte mit
///   Dezimalpunkt (Produktdokument 3.3) oder -komma werden Zahlen. Als Text liess ClosedXML sie nach der Kultur des Servers
///   lesen (gemessen 2026-11-10: „4.5“ unter de-DE als 45, „4,5“ unter en-US als 45).
/// - Sonst: JSON-Zahlen werden Zahlen; Texte bleiben Texte, auch „12:30“, Zeitraum-Texte und Personalnummern mit fuehrenden
///   Nullen. null und leere Texte werden leere Zellen; true/false und verschachtelte Werte bleiben als Text stehen.
/// Das Blatt heisst „Rexx775“ bzw. „Rexx777“; daran erkennt der Abruf seine eigene Datei.
/// </summary>
public static class RexxHubWorkbook
{
    private static readonly Regex IsoDate = new(
        @"^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2})(?::(\d{2}))?(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?)?$",
        RegexOptions.CultureInvariant);

    private static readonly Regex DeDate = new(@"^\d{1,2}\.\d{1,2}\.\d{4}$", RegexOptions.CultureInvariant);

    /// <summary>Excel erlaubt hoechstens so viele Zeichen je Zelle.</summary>
    private const int MaxCellText = 32767;

    public static string SheetName(RexxHubQueryKind kind) => kind == RexxHubQueryKind.Gesamt ? "Rexx775" : "Rexx777";

    /// <summary>
    /// ISO-Datum oder Zeitstempel der API als Datum. Der Zeitstempel wird NICHT in Ortszeit umgerechnet: Datum und Uhrzeit
    /// gelten wie geschrieben. OFFEN (Pruefung 2026-11-10): Schickt Rexx ein Schweizer Datum als UTC-Zeitstempel der lokalen
    /// Mitternacht („2026-11-29T23:00:00Z“ fuer den 30.11.), landet es einen Tag zu frueh. Das Dokument nennt fuer
    /// Datumsfelder „2009-11-04“; der Testabruf zaehlt Datumswerte mit Uhrzeit, entschieden wird nach Sicht auf die Antwort.
    /// </summary>
    public static bool TryParseIsoDate(string? text, out DateTime value)
    {
        value = default;
        var match = IsoDate.Match((text ?? string.Empty).Trim());
        if (!match.Success)
            return false;
        int Part(int group) => match.Groups[group].Success ? int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture) : 0;
        try
        {
            value = new DateTime(Part(1), Part(2), Part(3), Part(4), Part(5), Part(6));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>Datum der API: ISO wie oben oder „TT.MM.JJJJ“ (wie der Handexport); nichts anderes, damit nichts geraten wird.</summary>
    public static bool TryParseDate(string? text, out DateTime value)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (TryParseIsoDate(trimmed, out value))
            return true;
        return DeDate.IsMatch(trimmed) &&
               DateTime.TryParseExact(trimmed, ["d.M.yyyy", "dd.MM.yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    /// <summary>Hat ein ISO-Wert eine Uhrzeit oder Zeitzone? Fuer den Zaehlwert im Testabruf.</summary>
    public static bool HasTimePart(string? text)
    {
        var match = IsoDate.Match((text ?? string.Empty).Trim());
        return match.Success && match.Groups[4].Success;
    }

    /// <summary>
    /// Zahl aus einem Text: Dezimalpunkt (Produktdokument 3.3), sonst Dezimalkomma; Apostroph und Leerzeichen als Tausender
    /// werden entfernt, andere Tausendertrenner nicht angenommen (sonst wuerde „4,5“ zu 45).
    /// </summary>
    public static bool TryParseNumber(string? text, out decimal value)
    {
        var cleaned = (text ?? string.Empty).Trim().Replace("'", "", StringComparison.Ordinal).Replace("’", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (cleaned.Length > 0 && !cleaned.Contains(',') &&
            decimal.TryParse(cleaned, style, CultureInfo.InvariantCulture, out value))
            return true;
        if (cleaned.Length > 0 && !cleaned.Contains('.') &&
            decimal.TryParse(cleaned, style, CultureInfo.GetCultureInfo("de-DE"), out value))
            return true;
        value = 0;
        return false;
    }

    /// <summary>Liest, was am Zielpfad liegt; eine nicht lesbare Datei gilt als von Hand gelegt, ohne Zeilenzahl.</summary>
    public static RexxHubExistingFile Inspect(RexxHubQueryKind kind, string path)
    {
        if (!File.Exists(path))
            return new RexxHubExistingFile(false, false, null);
        try
        {
            // Eigener Stream statt Pfad: scheitert ClosedXML an einer kaputten Datei, bliebe sonst ein Dateihandle offen und
            // das anschliessende Ersetzen der Datei schluege fehl (im Test so aufgetreten).
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var header = sheet.FirstRowUsed();
            var rows = header is null
                ? 0
                : sheet.RowsUsed().Count(r => r.RowNumber() > header.RowNumber() &&
                                              !r.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetFormattedString())));
            return new RexxHubExistingFile(true, string.Equals(sheet.Name, SheetName(kind), StringComparison.Ordinal), rows);
        }
        catch (Exception)
        {
            return new RexxHubExistingFile(true, false, null);
        }
    }

    /// <summary>
    /// Schreibt zuerst unter <c>name.tmp.xlsx</c> und ersetzt dann die alte Datei, wie
    /// <c>SapGatewayHrKpiReader.WriteWorkbook</c>: ein halb geschriebenes Excel wuerde das Cockpit leer rechnen.
    /// </summary>
    public static void Write(RexxHubQueryKind kind, RexxHubTable table, string path)
    {
        // ClosedXML verlangt die Endung .xlsx, deshalb nicht einfach ".tmp" anhaengen (LEARNINGS 2026-09-30).
        var tempPath = Path.Combine(Path.GetDirectoryName(path) ?? ".", Path.GetFileNameWithoutExtension(path) + ".tmp.xlsx");
        var kinds = table.Columns.Select(name => RexxHubColumns.Find(kind, name)?.Kind).ToList();

        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet(SheetName(kind));
            for (var column = 0; column < table.Columns.Count; column++)
                sheet.Cell(1, column + 1).Value = table.Columns[column];

            var line = 2;
            foreach (var row in table.Rows)
            {
                for (var column = 0; column < table.Columns.Count; column++)
                {
                    if (!row.TryGetValue(table.Columns[column], out var value) || value is null ||
                        value is string blank && string.IsNullOrWhiteSpace(blank))
                        continue;
                    SetCell(sheet.Cell(line, column + 1), value, kinds[column]);
                }
                line++;
            }

            workbook.SaveAs(tempPath);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    private static void SetCell(IXLCell cell, object value, RexxHubColumnKind? kind)
    {
        if (kind == RexxHubColumnKind.Datum)
        {
            // Nur erkannte Daten; alles andere bleibt leer (siehe Klassenkommentar).
            if (value is string dateText && TryParseDate(dateText, out var date))
            {
                cell.Value = date;
                cell.Style.DateFormat.Format = date.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm:ss";
            }
            return;
        }

        switch (value)
        {
            case decimal number:
                cell.Value = number;
                break;
            case string text when kind == RexxHubColumnKind.Zahl && TryParseNumber(text, out var parsed):
                cell.Value = parsed;
                break;
            case string text:
                cell.Value = text.Length > MaxCellText ? text[..MaxCellText] : text;
                break;
            case bool flag:
                cell.Value = flag ? "true" : "false";
                break;
            default:
                var raw = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                cell.Value = raw.Length > MaxCellText ? raw[..MaxCellText] : raw;
                break;
        }
    }
}
