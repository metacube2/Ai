using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

// Zieht die Kundenfelder in den bereits veroeffentlichten DE-Verkaufsdateien nach.
// Grundlage ist der Plan, den Tools/DeCustomerBackfill beim produktiven Lauf
// geschrieben hat. Es wird nichts neu berechnet: alle Finanzblaetter, Formeln und
// uebrigen Zellen bleiben Zelle fuer Zelle stehen.
//
// Aufruf: DeCustomerBackfillFiles <planned-changes.json> <csv> <xlsx> <arbeitsordner> [--apply]
//   ohne --apply entstehen nur die korrigierten Kopien im Arbeitsordner.

if (args.Length < 4)
    throw new ArgumentException("Aufruf: DeCustomerBackfillFiles <planned-changes.json> <csv> <xlsx> <arbeitsordner> [--apply]");

var apply = args.Contains("--apply");
var planPath = Path.GetFullPath(args[0]);
var csvPath = Path.GetFullPath(args[1]);
var excelPath = Path.GetFullPath(args[2]);
var workFolder = Path.GetFullPath(args[3]);
Directory.CreateDirectory(workFolder);

var railwayIndustries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "00 Bahn",
    "05 rw Railways / Bahntechnik"
};

var plan = JsonSerializer.Deserialize<List<Change>>(File.ReadAllText(planPath))!;
var map = new Dictionary<(string Invoice, string Old), Change>();
foreach (var change in plan)
{
    var key = (change.InvoiceNumber.Trim(), change.OldCustomerNumber.Trim());
    if (map.TryGetValue(key, out var existing) && existing.CustomerNumber != change.CustomerNumber)
        throw new InvalidOperationException($"Rechnung {key.Item1} mit alter Nummer {key.Item2} fuehrt auf zwei neue Nummern.");
    map[key] = change;
}
Console.WriteLine($"Plan: {plan.Count} Zeilen, {map.Count} eindeutige Abbildungen");

// ------------------------------------------------------------------- CSV
const char Delimiter = ';';
var csvLines = File.ReadAllLines(csvPath, new UTF8Encoding(true));
var header = SplitDelimited(csvLines[0].TrimStart('﻿'), Delimiter);
int Field(string name)
{
    var index = header.FindIndex(x => string.Equals(x.Trim(), name, StringComparison.OrdinalIgnoreCase));
    if (index < 0) throw new InvalidOperationException($"Spalte '{name}' fehlt in der CSV.");
    return index;
}

var iInvoice = Field("InvoiceNumber");
var iNumber = Field("CustomerNumber");
var iName = Field("CustomerName");
var iCountry = Field("CustomerCountry");
var iIndustry = Field("CustomerIndustry");

var csvChanged = 0;
var csvOut = new List<string> { csvLines[0] };
for (var index = 1; index < csvLines.Length; index++)
{
    var line = csvLines[index];
    if (string.IsNullOrWhiteSpace(line)) { csvOut.Add(line); continue; }
    var fields = SplitDelimited(line, Delimiter);
    if (fields.Count <= iIndustry) { csvOut.Add(line); continue; }

    if (!map.TryGetValue((fields[iInvoice].Trim(), fields[iNumber].Trim()), out var change))
    { csvOut.Add(line); continue; }

    fields[iNumber] = change.CustomerNumber;
    fields[iName] = change.CustomerName;
    fields[iCountry] = change.CustomerCountry;
    fields[iIndustry] = change.CustomerIndustry;
    csvOut.Add(JoinDelimited(fields, Delimiter));
    csvChanged++;
}

Console.WriteLine($"CSV: {csvLines.Length - 1} Datenzeilen, {csvChanged} geaendert");
if (csvChanged != plan.Count)
    throw new InvalidOperationException($"CSV: {csvChanged} geaendert, erwartet {plan.Count}. Abbruch.");

var csvTarget = Path.Combine(workFolder, "korrigiert-" + Path.GetFileName(csvPath));
File.WriteAllLines(csvTarget, csvOut, new UTF8Encoding(true));

// Gegenprobe: ausser den Kundenspalten darf sich keine Zelle bewegt haben.
var writtenBack = File.ReadAllLines(csvTarget, new UTF8Encoding(true));
if (writtenBack.Length != csvLines.Length) throw new InvalidOperationException("CSV: Zeilenzahl weicht ab.");
var otherCellDifferences = 0;
for (var index = 1; index < csvLines.Length; index++)
{
    var before = SplitDelimited(csvLines[index], Delimiter);
    var after = SplitDelimited(writtenBack[index], Delimiter);
    if (before.Count != after.Count) throw new InvalidOperationException($"CSV Zeile {index + 1}: Spaltenzahl weicht ab.");
    for (var column = 0; column < before.Count; column++)
    {
        if (column == iNumber || column == iName || column == iCountry || column == iIndustry) continue;
        if (!string.Equals(before[column], after[column], StringComparison.Ordinal)) otherCellDifferences++;
    }
}
Console.WriteLine($"CSV: Abweichungen ausserhalb der vier Kundenspalten: {otherCellDifferences}");
if (otherCellDifferences > 0) throw new InvalidOperationException("CSV: fremde Zellen veraendert. Abbruch.");

// ----------------------------------------------------------------- Excel
var excelTarget = Path.Combine(workFolder, "korrigiert-" + Path.GetFileName(excelPath));
var excelChanged = 0;
var segmentChanged = 0;
using (var workbook = new XLWorkbook(excelPath))
{
    foreach (var sheet in workbook.Worksheets)
    {
        var headers = sheet.FirstRowUsed()?.CellsUsed()
            .ToDictionary(c => c.GetString().Trim().ToLowerInvariant(), c => c.Address.ColumnNumber);
        if (headers is null
            || !headers.TryGetValue("customer number", out var numberColumn)
            || !headers.TryGetValue("invoice number", out var invoiceColumn))
            continue;

        Console.WriteLine($"  Blatt '{sheet.Name}': Kundenspalten gefunden");
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var key = (row.Cell(invoiceColumn).GetString().Trim(), row.Cell(numberColumn).GetString().Trim());
            if (!map.TryGetValue(key, out var change)) continue;

            row.Cell(numberColumn).Value = change.CustomerNumber;
            foreach (var pair in new[]
                     {
                         ("customer name", change.CustomerName),
                         ("customer country", change.CustomerCountry),
                         ("customer industry", change.CustomerIndustry)
                     })
                if (headers.TryGetValue(pair.Item1, out var column)) row.Cell(column).Value = pair.Item2;

            if (railwayIndustries.Contains(change.CustomerIndustry.Trim()))
            {
                if (headers.TryGetValue("market segment", out var segmentColumn))
                {
                    row.Cell(segmentColumn).Value = "Railway";
                    segmentChanged++;
                }

                if (headers.TryGetValue("market segment source", out var sourceColumn))
                    row.Cell(sourceColumn).Value = "Alphaplan Kundenstamm / Branche";
            }

            excelChanged++;
        }
    }

    workbook.SaveAs(excelTarget);
}

Console.WriteLine($"Excel: {excelChanged} Zeilen geaendert, davon {segmentChanged} auf Segment Railway");
if (excelChanged != plan.Count)
    throw new InvalidOperationException($"Excel: {excelChanged} geaendert, erwartet {plan.Count}. Abbruch.");

// Gegenprobe Zelle fuer Zelle: ausser den Kunden- und Segmentspalten darf sich
// nichts bewegt haben, weder ein Wert noch eine Formel.
{
    using var before = new XLWorkbook(excelPath);
    using var after = new XLWorkbook(excelTarget);
    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "customer number", "customer name", "customer country", "customer industry",
        "market segment", "market segment source"
    };

    if (before.Worksheets.Count != after.Worksheets.Count)
        throw new InvalidOperationException("Excel: Blattzahl weicht ab.");

    var foreignDifferences = 0;
    var formulaDifferences = 0;
    foreach (var sheetBefore in before.Worksheets)
    {
        var sheetAfter = after.Worksheet(sheetBefore.Name);
        var allowedColumns = new HashSet<int>();
        var headerRow = sheetBefore.FirstRowUsed();
        if (headerRow is not null)
            foreach (var cell in headerRow.CellsUsed())
                if (allowed.Contains(cell.GetString().Trim())) allowedColumns.Add(cell.Address.ColumnNumber);

        var rangeBefore = sheetBefore.RangeUsed();
        var rangeAfter = sheetAfter.RangeUsed();
        if (rangeBefore is null && rangeAfter is null) continue;
        if (rangeBefore is null || rangeAfter is null
            || rangeBefore.RowCount() != rangeAfter.RowCount()
            || rangeBefore.ColumnCount() != rangeAfter.ColumnCount())
            throw new InvalidOperationException($"Excel: Bereich in Blatt '{sheetBefore.Name}' weicht ab.");

        foreach (var cellBefore in rangeBefore.CellsUsed())
        {
            var address = cellBefore.Address;
            if (allowedColumns.Contains(address.ColumnNumber)) continue;
            var cellAfter = sheetAfter.Cell(address.RowNumber, address.ColumnNumber);
            if (cellBefore.HasFormula != cellAfter.HasFormula
                || (cellBefore.HasFormula && cellBefore.FormulaA1 != cellAfter.FormulaA1))
            { formulaDifferences++; continue; }
            if (!string.Equals(cellBefore.GetFormattedString(), cellAfter.GetFormattedString(), StringComparison.Ordinal))
                foreignDifferences++;
        }
    }

    Console.WriteLine($"Excel: fremde Zellabweichungen {foreignDifferences}, Formelabweichungen {formulaDifferences}");
    if (foreignDifferences > 0 || formulaDifferences > 0)
        throw new InvalidOperationException("Excel: ausserhalb der Kunden- und Segmentspalten hat sich etwas veraendert. Abbruch.");
}

Console.WriteLine($"Korrigierte Dateien:\n  {csvTarget}\n  {excelTarget}");
if (!apply)
{
    Console.WriteLine("Probelauf beendet. Die Originale sind unberuehrt.");
    return;
}

Console.WriteLine("Die korrigierten Dateien sind erzeugt. Das Ersetzen am Zielort erfolgt getrennt.");
return;

static List<string> SplitDelimited(string line, char delimiter)
{
    var values = new List<string>();
    var builder = new StringBuilder();
    var quoted = false;
    for (var index = 0; index < line.Length; index++)
    {
        var current = line[index];
        if (quoted)
        {
            if (current == '"')
            {
                if (index + 1 < line.Length && line[index + 1] == '"') { builder.Append('"'); index++; }
                else quoted = false;
            }
            else builder.Append(current);
            continue;
        }

        if (current == '"') { quoted = true; continue; }
        if (current == delimiter) { values.Add(builder.ToString()); builder.Clear(); continue; }
        builder.Append(current);
    }

    values.Add(builder.ToString());
    return values;
}

static string JoinDelimited(IReadOnlyList<string> values, char delimiter)
{
    var builder = new StringBuilder();
    for (var index = 0; index < values.Count; index++)
    {
        if (index > 0) builder.Append(delimiter);
        var value = values[index];
        if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            builder.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
        else
            builder.Append(value);
    }

    return builder.ToString();
}

internal sealed record Change(
    int Id,
    string InvoiceNumber,
    string OldCustomerNumber,
    string CustomerNumber,
    string CustomerName,
    string CustomerCountry,
    string CustomerIndustry);
