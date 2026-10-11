using System.Globalization;
using ClosedXML.Excel;

// Baut aus einem App-eigenen Standortexport eine importfaehige UK-Jahresdatei.
//
// Ersetzt .tmp_tools/BuildUkBaseFile, das am 2026-07-28 die Produktivwerte fuer UK 2025 um
// den Faktor der Menge zu klein gemacht hat. Der Fehler war nicht die Rechnung, sondern eine
// ANNAHME: das alte Werkzeug ging davon aus, dass die Spalte "Sales Price/Value" im Export
// immer den fertigen Zeilenwert enthaelt, und teilte sie deshalb durch die Menge, um die
// Multiplikation des UK-Mappings (SageNetSales = Betrag * Menge) vorzukompensieren.
// In `Sales_TRUK_2026-05-11.xlsx` stand dort aber schon der STUECKPREIS, weil dieser Export
// von vor der Mapping-Umstellung stammt. Die Kompensation war damit doppelt.
//
// Deshalb rechnet dieses Werkzeug NICHTS um, sondern PRUEFT, welche der beiden Bedeutungen
// die Spalte hat, und schreibt die Datei nur, wenn das Ergebnis den erwarteten Jahreswert
// trifft. Genau diese Pruefung fehlte: die alte Kontrollrechnung verglich den Import gegen
// die QUELLDATEI und belegte damit nur, dass wir die Datei reproduzieren.
//
// Usage: UkBackfillFile <quelleXlsx> <zielXlsx> <erwarteterJahreswert> [TSC] [--force]

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: UkBackfillFile <quelleXlsx> <zielXlsx> <erwarteterJahreswert> [TSC] [--force]");
    Console.Error.WriteLine("  erwarteterJahreswert: Soll/Referenz des Standortjahres, z. B. 3538972");
    return 2;
}

var src = args[0];
var dst = args[1];
if (!decimal.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var expectedTotal) || expectedTotal <= 0m)
{
    Console.Error.WriteLine($"Erwarteter Jahreswert unlesbar: {args[2]}");
    return 2;
}
var expectedTsc = args.Length > 3 && !args[3].StartsWith("--") ? args[3] : "TRUK";
var force = args.Contains("--force", StringComparer.OrdinalIgnoreCase);

if (!File.Exists(src)) { Console.Error.WriteLine($"Quelle nicht gefunden: {src}"); return 2; }

using var wb = new XLWorkbook(src);
var ws = wb.Worksheets.Contains("Sales") ? wb.Worksheet("Sales") : wb.Worksheet(1);
var rows = ws.RowsUsed().ToList();
if (rows.Count < 2) { Console.Error.WriteLine("Datei enthaelt keine Datenzeilen."); return 2; }

var header = rows[0];
var lastCol = header.LastCellUsed()?.Address.ColumnNumber ?? 1;
var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
for (var c = 1; c <= lastCol; c++)
{
    var name = header.Cell(c).GetString().Trim();
    if (!string.IsNullOrWhiteSpace(name) && !idx.ContainsKey(name)) idx[name] = c;
}
foreach (var required in new[] { "Sales Price/Value", "Quantity", "Invoice Number", "Document Type", "TSC" })
{
    if (!idx.ContainsKey(required)) { Console.Error.WriteLine($"Spalte fehlt: {required}"); return 2; }
}

var spvCol = idx["Sales Price/Value"];
var qtyCol = idx["Quantity"];
var docCol = idx["Document Type"];
var tscCol = idx["TSC"];

// Der App-Export schreibt unter die Kopfzeile eine LEGENDENZEILE mit Feldbeschreibungen.
// Am 2026-07-28 wurde sie als Datensatz importiert und haengt seither als eigener "Standort"
// in den Produktivdaten - Manual-Importe ersetzen je TSC und fassen diese TSC nie an.
var legendRows = new List<int>();
decimal sumAsIs = 0m, sumTimesQty = 0m;
int dataRows = 0, qtyZero = 0, signFlipRisk = 0;
var signFlipExamples = new List<string>();

for (var r = 1; r < rows.Count; r++)
{
    var row = rows[r];
    var tsc = row.Cell(tscCol).GetString().Trim();
    if (!string.Equals(tsc, expectedTsc, StringComparison.OrdinalIgnoreCase))
    {
        legendRows.Add(row.RowNumber());
        continue;
    }

    dataRows++;
    var value = Dec(row.Cell(spvCol));
    var qty = Dec(row.Cell(qtyCol));
    var docType = row.Cell(docCol).GetString();
    if (qty == 0m) qtyZero++;

    // Vorzeichenrisiko: Gutschrift-Typ mit positivem Wert - SageNetSales dreht beim Import
    // auf negativ und veraendert damit den Wert.
    if (IsCreditNote(docType) && value > 0m)
    {
        signFlipRisk++;
        if (signFlipExamples.Count < 5)
            signFlipExamples.Add($"Zeile {row.RowNumber()}: Typ='{docType.Trim()}', Wert={value}");
    }

    sumAsIs += Signed(value, docType);
    sumTimesQty += Signed(qty == 0m ? value : value * qty, docType);
}

Console.WriteLine($"Quelle              : {Path.GetFileName(src)}");
Console.WriteLine($"Datenzeilen ({expectedTsc}) : {dataRows}");
Console.WriteLine($"Legendenzeilen raus : {legendRows.Count}" + (legendRows.Count > 0 ? $" (Zeile {string.Join(",", legendRows)})" : ""));
Console.WriteLine($"Menge = 0           : {qtyZero}");
Console.WriteLine();

// Der Kern: welche Bedeutung hat die Spalte? Der Import multipliziert immer mit der Menge,
// die Datei muss also Stueckpreise liefern. Welche der beiden Lesarten den erwarteten
// Jahreswert trifft, entscheidet, ob umgerechnet werden muss oder nicht.
Console.WriteLine($"Erwarteter Jahreswert (Soll): {Fmt(expectedTotal)}");
Console.WriteLine($"  A) Spalte sind STUECKPREISE -> Import ergibt Betrag x Menge : {Fmt(sumTimesQty)}  ({Pct(sumTimesQty, expectedTotal)})");
Console.WriteLine($"  B) Spalte sind ZEILENWERTE  -> Import ergibt Betrag unveraendert: {Fmt(sumAsIs)}  ({Pct(sumAsIs, expectedTotal)})");
Console.WriteLine();

const decimal tolerance = 0.02m;           // 2 % - deckt Dubletten und Rundung ab
var offA = Off(sumTimesQty, expectedTotal);
var offB = Off(sumAsIs, expectedTotal);
var aFits = offA <= tolerance;
var bFits = offB <= tolerance;

string verdict;
var convert = false;
if (aFits && !bFits)
{
    verdict = "Spalte enthaelt STUECKPREISE. Datei unveraendert durchreichen - der Import multipliziert.";
}
else if (bFits && !aFits)
{
    verdict = "Spalte enthaelt ZEILENWERTE. Auf Stueckpreis zuruecksetzen, damit der Import nicht doppelt multipliziert.";
    convert = true;
}
else if (aFits && bFits)
{
    verdict = "UNENTSCHEIDBAR - beide Lesarten treffen den Sollwert. Nicht schreiben, Sollwert oder Quelle pruefen.";
}
else
{
    verdict = "KEINE Lesart trifft den Sollwert. Nicht schreiben - Quelle, Jahr oder Sollwert stimmen nicht zusammen.";
}
Console.WriteLine($"BEFUND: {verdict}");

if (!aFits && !bFits && !force)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("ABBRUCH: keine Datei geschrieben. Mit --force ueberstimmen (nur mit gutem Grund).");
    return 1;
}
if (aFits && bFits && !force)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("ABBRUCH: keine Datei geschrieben (unentscheidbar). Mit --force ueberstimmen.");
    return 1;
}

if (convert)
{
    var converted = 0;
    for (var r = 1; r < rows.Count; r++)
    {
        var row = rows[r];
        if (!string.Equals(row.Cell(tscCol).GetString().Trim(), expectedTsc, StringComparison.OrdinalIgnoreCase)) continue;
        var qty = Dec(row.Cell(qtyCol));
        if (qty == 0m) continue;                    // Stueckpreis nicht bestimmbar, Wert belassen
        row.Cell(spvCol).Value = Dec(row.Cell(spvCol)) / qty;
        converted++;
    }
    Console.WriteLine($"Umgerechnet auf Stueckpreis: {converted} Zeilen");
}
else
{
    Console.WriteLine("Keine Umrechnung - Werte bleiben unveraendert.");
}

foreach (var rowNumber in legendRows.OrderByDescending(x => x))
    ws.Row(rowNumber).Delete();

if (signFlipRisk > 0)
{
    Console.WriteLine();
    Console.WriteLine($"WARNUNG Vorzeichen: {signFlipRisk} Zeilen als Gutschrift typisiert mit POSITIVEM Wert:");
    foreach (var e in signFlipExamples) Console.WriteLine($"  {e}");
    Console.WriteLine("  (In den Summen oben beruecksichtigt.)");
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dst))!);
wb.SaveAs(dst);
Console.WriteLine();
Console.WriteLine($"Geschrieben: {dst}");
Console.WriteLine($"Erwartet nach dem Import: {Fmt(convert ? sumAsIs : sumTimesQty)}");
return 0;

static decimal Signed(decimal value, string docType) => IsCreditNote(docType) ? -Math.Abs(value) : value;
static string Fmt(decimal v) => v.ToString("N2", CultureInfo.InvariantCulture);
static decimal Off(decimal actual, decimal expected) => Math.Abs(actual - expected) / expected;
static string Pct(decimal actual, decimal expected) => (actual / expected).ToString("P1", CultureInfo.InvariantCulture) + " des Solls";

static decimal Dec(IXLCell cell)
{
    if (cell.TryGetValue<double>(out var d)) return (decimal)d;
    var s = cell.GetString().Trim().Replace("'", "").Replace(" ", "");
    return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
}

// Muss identisch zu ManualExcelImportService.IsCreditNote bleiben.
static bool IsCreditNote(string documentType)
{
    var n = documentType.Trim().ToUpperInvariant();
    return n.Contains("CREDIT") || n.Contains("CREDITNOTE") || n.Contains("ABONO") ||
           n.Contains("GUTSCHRIFT") || n == "CRN" || n == "CN";
}
