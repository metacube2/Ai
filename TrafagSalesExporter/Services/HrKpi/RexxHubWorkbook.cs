using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace TrafagSalesExporter.Services;

/// <summary>
/// Schreibt eine Rexx-Hub-Antwort als Excel-Datei mit denselben Spaltenueberschriften wie der Handexport, damit
/// <c>HrKpiDashboardBuilder</c> sie unveraendert liest (Rundreise-Test in <c>RexxHubTests</c>).
///
/// Umsetzung je Spalte, nicht nach dem Aussehen eines Wertes:
/// - Spalten, die der Leser als Datum liest (Eintritts-, Austritts-, Geburtsdatum, Abwesenheit von/bis): ISO-Datum der API
///   („2009-11-04“ oder „2009-11-04T19:55:41Z“, Produktdokument Kapitel 3.3) wird ein Excel-Datum; anderer Text bleibt Text.
/// - JSON-Zahlen werden Zahlen; Texte bleiben Texte, auch „12:30“, Zeitraum-Texte und Personalnummern mit fuehrenden Nullen.
/// - null wird eine leere Zelle; true/false und verschachtelte Werte bleiben als Text stehen (liest der Leser nicht).
/// </summary>
public static class RexxHubWorkbook
{
    private static readonly Regex IsoDate = new(
        @"^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2})(?::(\d{2}))?(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?)?$",
        RegexOptions.CultureInvariant);

    /// <summary>Excel erlaubt hoechstens so viele Zeichen je Zelle.</summary>
    private const int MaxCellText = 32767;

    /// <summary>
    /// ISO-Datum oder Zeitstempel der API als Datum. Der Zeitstempel wird NICHT in Ortszeit umgerechnet: Datum und Uhrzeit
    /// gelten wie geschrieben, damit ein Austritt um Mitternacht UTC nicht auf den Vortag rutscht.
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

    /// <summary>
    /// Schreibt zuerst unter <c>name.tmp.xlsx</c> und ersetzt dann die alte Datei, wie
    /// <c>SapGatewayHrKpiReader.WriteWorkbook</c>: ein halb geschriebenes Excel wuerde das Cockpit leer rechnen.
    /// </summary>
    public static void Write(RexxHubQueryKind kind, RexxHubTable table, string path)
    {
        // ClosedXML verlangt die Endung .xlsx, deshalb nicht einfach ".tmp" anhaengen (LEARNINGS 2026-09-30).
        var tempPath = Path.Combine(Path.GetDirectoryName(path) ?? ".", Path.GetFileNameWithoutExtension(path) + ".tmp.xlsx");
        var dateColumns = table.Columns
            .Select((name, index) => (name, index))
            .Where(c => RexxHubColumns.Find(kind, c.name)?.Kind == RexxHubColumnKind.Datum)
            .Select(c => c.index)
            .ToHashSet();

        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet(kind == RexxHubQueryKind.Gesamt ? "Rexx775" : "Rexx777");
            for (var column = 0; column < table.Columns.Count; column++)
                sheet.Cell(1, column + 1).Value = table.Columns[column];

            var line = 2;
            foreach (var row in table.Rows)
            {
                for (var column = 0; column < table.Columns.Count; column++)
                {
                    if (!row.TryGetValue(table.Columns[column], out var value) || value is null)
                        continue;
                    SetCell(sheet.Cell(line, column + 1), value, dateColumns.Contains(column));
                }
                line++;
            }

            workbook.SaveAs(tempPath);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    private static void SetCell(IXLCell cell, object value, bool dateColumn)
    {
        switch (value)
        {
            case decimal number:
                cell.Value = number;
                break;
            case string text when dateColumn && TryParseIsoDate(text, out var date):
                cell.Value = date;
                cell.Style.DateFormat.Format = date.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm:ss";
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
