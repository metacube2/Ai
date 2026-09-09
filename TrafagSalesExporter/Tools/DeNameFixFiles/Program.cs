using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

// Korrigiert in den bereits veroeffentlichten DE-Verkaufsdateien ausschliesslich die Spalte
// Name. Grundlage ist der Plan von Tools/DeNameFix; die Abbildung alter Text -> bereinigter
// Text ist eindeutig, weil die Bereinigung eine reine Funktion des RTF-Textes ist.
// Es wird nichts neu berechnet: die Finanzblaetter und alle uebrigen Felder bleiben Zelle
// fuer Zelle und Byte fuer Byte stehen.
//
// Aufruf: DeNameFixFiles <plan.json> <csv> <xlsx> <arbeitsordner> [--apply]
//   ohne --apply werden nur die korrigierten Kopien im Arbeitsordner erzeugt.

if (args.Length < 4)
    throw new ArgumentException("Aufruf: DeNameFixFiles <plan.json> <csv> <xlsx> <arbeitsordner> [--apply]");

var apply = args.Contains("--apply");
var planPath = Path.GetFullPath(args[0]);
var csvPath = args[1];
var excelPath = args[2];
var workFolder = Path.GetFullPath(args[3]);
Directory.CreateDirectory(workFolder);

var plan = JsonSerializer.Deserialize<List<Change>>(File.ReadAllText(planPath))!;
var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var change in plan)
{
    if (mapping.TryGetValue(change.OldName, out var existing) && existing != change.NewName)
        throw new InvalidOperationException("Derselbe alte Text fuehrt zu zwei verschiedenen neuen Texten: " + change.OldName);
    mapping[change.OldName] = change.NewName;
}

Console.WriteLine($"Plan: {plan.Count} Zeilen, {mapping.Count} eindeutige Textabbildungen");

// ------------------------- Sicherungskopien -------------------------
string Secure(string source)
{
    // Die Endung muss hinten bleiben, sonst weist ClosedXML die Sicherung ab.
    var target = Path.Combine(workFolder,
        Path.GetFileNameWithoutExtension(source) + ".vor-namefix" + Path.GetExtension(source));
    if (File.Exists(target))
        throw new IOException("Sicherung existiert bereits: " + target);
    File.Copy(source, target);
    return target;
}

var csvBackup = Secure(csvPath);
var excelBackup = Secure(excelPath);
Console.WriteLine($"Sicherungen: {Path.GetFileName(csvBackup)}, {Path.GetFileName(excelBackup)}");

// ------------------------- CSV -------------------------
const char Delimiter = ';';

static List<string> ParseLine(string line)
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
        if (current == Delimiter) { values.Add(builder.ToString()); builder.Clear(); continue; }
        builder.Append(current);
    }

    values.Add(builder.ToString());
    return values;
}

static string Escape(string value)
    => value.Contains(Delimiter) || value.Contains('"') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;

var csvLines = File.ReadAllLines(csvBackup, Encoding.UTF8);
var header = ParseLine(csvLines[0]);
var nameIndex = header.FindIndex(x => string.Equals(x, "Name", StringComparison.OrdinalIgnoreCase));
if (nameIndex < 0) throw new InvalidOperationException("Spalte Name fehlt in der CSV.");
Console.WriteLine($"CSV: {csvLines.Length - 1} Datenzeilen, Spalte Name an Position {nameIndex + 1}");

var csvChanged = 0;
for (var i = 1; i < csvLines.Length; i++)
{
    var fields = ParseLine(csvLines[i]);
    if (fields.Count != header.Count)
        throw new InvalidOperationException($"Zeile {i + 1} hat {fields.Count} statt {header.Count} Felder.");

    var rebuilt = string.Join(Delimiter, fields.Select(Escape));
    if (rebuilt != csvLines[i])
        throw new InvalidOperationException($"Zeile {i + 1} laesst sich nicht unveraendert zurueckschreiben. Abbruch.");

    if (!mapping.TryGetValue(fields[nameIndex], out var clean)) continue;
    fields[nameIndex] = clean;
    csvLines[i] = string.Join(Delimiter, fields.Select(Escape));
    csvChanged++;
}

var csvTarget = Path.Combine(workFolder, Path.GetFileName(csvPath));
File.WriteAllText(csvTarget, string.Join("\r\n", csvLines) + "\r\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

// Gegenprobe: nur die Spalte Name darf abweichen.
var originalLines = File.ReadAllLines(csvBackup, Encoding.UTF8);
var writtenLines = File.ReadAllLines(csvTarget, Encoding.UTF8);
if (originalLines.Length != writtenLines.Length)
    throw new InvalidOperationException("Zeilenzahl der CSV hat sich geaendert.");
var otherFieldDifferences = 0;
var nameDifferences = 0;
for (var i = 0; i < originalLines.Length; i++)
{
    var before = ParseLine(originalLines[i]);
    var after = ParseLine(writtenLines[i]);
    for (var f = 0; f < before.Count; f++)
    {
        if (before[f] == after[f]) continue;
        if (f == nameIndex) nameDifferences++; else otherFieldDifferences++;
    }
}

Console.WriteLine($"CSV: {csvChanged} Namen ersetzt, {nameDifferences} Felder in Spalte Name abweichend, {otherFieldDifferences} Abweichungen in anderen Spalten");
if (otherFieldDifferences > 0) throw new InvalidOperationException("Andere Spalten haben sich geaendert. Abbruch.");

// ------------------------- Excel -------------------------
var excelTarget = Path.Combine(workFolder, Path.GetFileName(excelPath));
var excelChanged = 0;
using (var workbook = new XLWorkbook(excelBackup))
{
    foreach (var sheet in workbook.Worksheets)
    {
        var headerRow = sheet.FirstRowUsed();
        if (headerRow is null) continue;
        var column = headerRow.CellsUsed()
            .FirstOrDefault(c => string.Equals(c.GetString().Trim(), "Name", StringComparison.OrdinalIgnoreCase));
        if (column is null) continue;

        var columnNumber = column.Address.ColumnNumber;
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var cell = row.Cell(columnNumber);
            var text = cell.GetString();
            if (!mapping.TryGetValue(text, out var clean)) continue;
            cell.Value = clean;
            excelChanged++;
        }
    }

    workbook.SaveAs(excelTarget);
}

// Gegenprobe Zelle fuer Zelle: nur Namenszellen duerfen abweichen.
using (var before = new XLWorkbook(excelBackup))
using (var after = new XLWorkbook(excelTarget))
{
    if (before.Worksheets.Count != after.Worksheets.Count) throw new IOException("Blattzahl weicht ab.");
    var cellDifferences = 0;
    var nameCellDifferences = 0;
    foreach (var sheet in before.Worksheets)
    {
        var other = after.Worksheet(sheet.Name);
        if (sheet.CellsUsed().Count() != other.CellsUsed().Count()) throw new IOException("Zellzahl weicht ab: " + sheet.Name);
        var headerRow = sheet.FirstRowUsed();
        var nameColumn = headerRow?.CellsUsed()
            .FirstOrDefault(c => string.Equals(c.GetString().Trim(), "Name", StringComparison.OrdinalIgnoreCase))?
            .Address.ColumnNumber ?? -1;
        foreach (var cell in sheet.CellsUsed())
        {
            var match = other.Cell(cell.Address.RowNumber, cell.Address.ColumnNumber);
            if (cell.Value.Equals(match.Value) && cell.FormulaA1 == match.FormulaA1) continue;
            if (cell.Address.ColumnNumber == nameColumn && cell.Address.RowNumber > 1) nameCellDifferences++;
            else cellDifferences++;
        }
    }

    Console.WriteLine($"Excel: {excelChanged} Zellen ersetzt, {nameCellDifferences} Namenszellen abweichend, {cellDifferences} sonstige Abweichungen");
    if (cellDifferences > 0) throw new InvalidOperationException("Andere Zellen haben sich geaendert. Abbruch.");
}

if (!apply)
{
    Console.WriteLine("Probelauf beendet. Korrigierte Kopien liegen im Arbeitsordner, die veroeffentlichten Dateien sind unveraendert.");
    return;
}

// ------------------------- Bereitstellen -------------------------
static void Replace(string corrected, string target)
{
    var staging = target + ".namefix-staging";
    File.Copy(corrected, staging, true);
    File.Move(staging, target, true);
    var a = SHA256.HashData(File.ReadAllBytes(corrected));
    var b = SHA256.HashData(File.ReadAllBytes(target));
    if (!a.SequenceEqual(b)) throw new IOException("Kopie stimmt nicht ueberein: " + target);
    Console.WriteLine($"Ersetzt: {target} | SHA256 {Convert.ToHexString(b)}");
}

Replace(csvTarget, csvPath);
Replace(excelTarget, excelPath);
Console.WriteLine("Fertig. Nur die Spalte Name wurde ersetzt.");

internal sealed record Change(int Id, string LineKey, string InvoiceNumber, string Material, string OldName, string NewName);
