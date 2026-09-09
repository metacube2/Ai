using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;

// Zieht die fachlichen Kundenfelder der deutschen Verkaufszeilen nach, Grundlage ist
// Rohails Rechnungsliste mit der fachlichen Adressnummer je Rechnungsnummer.
// Fachlicher Nachweis: docs/BAHNMARKT_DE_ANBINDUNG_2026-09-08.md Abschnitt 9.
//
// Angefasst werden ausschliesslich Zeilen, die heute keine fachliche Nummer tragen,
// also leer sind oder mit ALPHAPLAN-ID: beginnen. Eine bereits belegte Nummer wird
// nie ueberschrieben. Damit bleiben auch die drei Doppelnummern Sonepar, EMS und
// Magnetic Sense unberuehrt; dort steht eine fachliche Entscheidung aus.
//
// Aufruf: DeCustomerBackfill <db> <rechnungen.xlsx> <kundenstamm.csv> <arbeitsordner> [--apply]
//   ohne --apply wird nur gemessen und der Vorherstand gesichert.

if (args.Length < 4)
    throw new ArgumentException("Aufruf: DeCustomerBackfill <db> <rechnungen.xlsx> <kundenstamm.csv> <arbeitsordner> [--apply]");

var apply = args.Contains("--apply");
var databasePath = Path.GetFullPath(args[0]);
var rechnungenPath = Path.GetFullPath(args[1]);
var kundenstammPath = Path.GetFullPath(args[2]);
var workFolder = Path.GetFullPath(args[3]);
Directory.CreateDirectory(workFolder);

var appliedMarker = Path.Combine(workFolder, "database-applied.txt");
if (apply && File.Exists(appliedMarker))
    throw new InvalidOperationException($"{appliedMarker} vorhanden. Kein blinder zweiter Lauf.");

var railwayIndustries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "00 Bahn",
    "05 rw Railways / Bahntechnik"
};

// ---------------------------------------------------------------- Kundenstamm
var master = new Dictionary<string, MasterAddress>(StringComparer.OrdinalIgnoreCase);
foreach (var line in File.ReadLines(kundenstammPath, Encoding.UTF8).Skip(1))
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var f = SplitDelimited(line, ';');
    if (f.Count < 6) continue;
    var number = f[0].Trim();
    if (number.Length == 0) continue;
    master[number] = new MasterAddress(f[1].Trim(), f[4].Trim(), f[5].Trim());
}
Console.WriteLine($"Kundenstamm: {master.Count} Adressen");

// ------------------------------------------------- Rohails Rechnungsliste
var bridge = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
using (var workbook = new XLWorkbook(rechnungenPath))
{
    var sheet = workbook.Worksheets.First();
    var used = sheet.RangeUsed() ?? throw new InvalidOperationException("Rechnungsliste ist leer.");
    var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var cell in used.FirstRow().Cells())
        columns[cell.GetString().Trim()] = cell.Address.ColumnNumber;

    int Column(string name) => columns.TryGetValue(name, out var index)
        ? index
        : throw new InvalidOperationException($"Spalte '{name}' fehlt in der Rechnungsliste.");

    var invoiceColumn = Column("Rech.-Nr.");
    var addressColumn = Column("Adressnr._R");
    var conflicting = 0;

    foreach (var row in used.Rows().Skip(1))
    {
        var invoice = row.Cell(invoiceColumn).GetString().Trim();
        var address = row.Cell(addressColumn).GetString().Trim();
        if (invoice.Length == 0 || address.Length == 0) continue;
        if (bridge.TryGetValue(invoice, out var existing))
        {
            if (!string.Equals(existing, address, StringComparison.OrdinalIgnoreCase)) conflicting++;
            continue;
        }
        bridge[invoice] = address;
    }

    Console.WriteLine($"Belegbruecke: {bridge.Count} Rechnungsnummern, {conflicting} widerspruechliche Doppel");
    if (conflicting > 0)
        throw new InvalidOperationException("Die Rechnungsliste fuehrt dieselbe Rechnung auf zwei Adressnummern. Abbruch.");
}

// ------------------------------------------------------------------ Datenbank
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = apply ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly
}.ToString()).Options;

await using var db = new AppDbContext(options);

// Integritaet vor jeder Aussage ueber den Inhalt.
await db.Database.OpenConnectionAsync();
await using (var check = ((SqliteConnection)db.Database.GetDbConnection()).CreateCommand())
{
    check.CommandText = "PRAGMA quick_check;";
    var result = (await check.ExecuteScalarAsync())?.ToString() ?? "";
    Console.WriteLine($"quick_check: {result}");
    if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Die Datenbank besteht quick_check nicht. Abbruch.");
}

var site = await db.Sites.AsNoTracking().SingleAsync(x => x.TSC == "TRDE");
var rows = await db.CentralSalesRecords.AsNoTracking()
    .Where(x => x.SiteId == site.Id).OrderBy(x => x.Id).ToListAsync();
var segments = await db.CustomerMarketSegments.AsNoTracking()
    .Where(x => x.Tsc == "TRDE").ToListAsync();

Console.WriteLine($"TRDE-Zeilen: {rows.Count}, Segmenteintraege: {segments.Count}");
Console.WriteLine($"davon mit fachlicher Nummer: {rows.Count(HasBusinessNumber)}");

var salesTotalBefore = rows.Sum(x => x.SalesPriceValue);
Console.WriteLine($"SalesPriceValue gesamt vorher: {salesTotalBefore.ToString("0.00", CultureInfo.InvariantCulture)}");

// Vorherstand sichern, bevor irgendetwas geschrieben wird.
var beforeRowsPath = Path.Combine(workFolder, "before-rows.json");
var beforeSegmentsPath = Path.Combine(workFolder, "before-segments.json");
if (!File.Exists(beforeRowsPath))
{
    File.WriteAllText(beforeRowsPath, JsonSerializer.Serialize(rows));
    File.WriteAllText(beforeSegmentsPath, JsonSerializer.Serialize(segments));
    Console.WriteLine("Vorherstand gesichert.");
}
else if (apply)
{
    // Beim Anwenden muss der Live-Stand noch der gemessene sein.
    if (JsonSerializer.Serialize(rows) != File.ReadAllText(beforeRowsPath))
        throw new InvalidOperationException("Die Verkaufszeilen haben sich seit der Messung geaendert. Abbruch.");
    if (JsonSerializer.Serialize(segments) != File.ReadAllText(beforeSegmentsPath))
        throw new InvalidOperationException("Die Segmentzuordnungen haben sich seit der Messung geaendert. Abbruch.");
    Console.WriteLine("Vorherstand unveraendert, Anwendung freigegeben.");
}

// ------------------------------------------------------------------- Plan
var planned = new List<Change>();
var withoutBridge = new List<string>();
foreach (var row in rows)
{
    if (HasBusinessNumber(row)) continue;
    var invoice = (row.InvoiceNumber ?? string.Empty).Trim();
    if (!bridge.TryGetValue(invoice, out var number))
    {
        withoutBridge.Add(invoice);
        continue;
    }

    if (!master.TryGetValue(number, out var address))
        throw new InvalidOperationException($"Adressnummer {number} aus Rechnung {invoice} fehlt im Kundenstamm. Abbruch.");

    planned.Add(new Change(row.Id, invoice, row.CustomerNumber ?? string.Empty, number,
        address.Name, address.Country, address.Industry));
}

var plannedPath = Path.Combine(workFolder, "planned-changes.json");
File.WriteAllText(plannedPath, JsonSerializer.Serialize(planned, new JsonSerializerOptions { WriteIndented = true }));

var newRailwayCustomers = planned
    .Where(x => railwayIndustries.Contains(x.CustomerIndustry))
    .Select(x => x.CustomerNumber)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .Where(x => !segments.Any(s => string.Equals(s.CustomerNumber, x, StringComparison.OrdinalIgnoreCase)
                                   && s.IsConfirmed && s.Segment == "Railway"))
    .ToList();

var blockedByDecision = planned
    .Where(x => railwayIndustries.Contains(x.CustomerIndustry))
    .Select(x => x.CustomerNumber)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .Where(x => segments.Any(s => string.Equals(s.CustomerNumber, x, StringComparison.OrdinalIgnoreCase)
                                  && s.IsConfirmed && s.Segment != "Railway"))
    .ToList();

Console.WriteLine($"Zeilen ohne fachliche Nummer: {rows.Count - rows.Count(HasBusinessNumber)}");
Console.WriteLine($"  davon nachziehbar        : {planned.Count}");
Console.WriteLine($"  davon ohne Belegbruecke  : {withoutBridge.Count} (Gutschriften)");
Console.WriteLine($"betroffene Kundennummern   : {planned.Select(x => x.CustomerNumber).Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
Console.WriteLine($"neue Railway-Segmente      : {newRailwayCustomers.Count}");
if (blockedByDecision.Count > 0)
    Console.WriteLine($"  durch bestaetigten Gegenentscheid geschuetzt: {string.Join(", ", blockedByDecision)}");
Console.WriteLine($"Plan: {plannedPath}");

if (!apply)
{
    Console.WriteLine("Probelauf beendet. Nichts geschrieben.");
    return;
}

// ------------------------------------------------------------------ Anwenden
await db.Database.OpenConnectionAsync();
var connection = (SqliteConnection)db.Database.GetDbConnection();
var updated = 0;
await using (var transaction = connection.BeginTransaction())
{
    foreach (var change in planned)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE CentralSalesRecords
            SET CustomerNumber = $number, CustomerName = $name,
                CustomerCountry = $country, CustomerIndustry = $industry
            WHERE Id = $id AND SiteId = $site AND CustomerNumber = $old
              AND (CustomerName IS NULL OR CustomerName = '')
            """;
        command.Parameters.AddWithValue("$number", change.CustomerNumber);
        command.Parameters.AddWithValue("$name", change.CustomerName);
        command.Parameters.AddWithValue("$country", change.CustomerCountry);
        command.Parameters.AddWithValue("$industry", change.CustomerIndustry);
        command.Parameters.AddWithValue("$id", change.Id);
        command.Parameters.AddWithValue("$site", site.Id);
        command.Parameters.AddWithValue("$old", change.OldCustomerNumber);
        if (await command.ExecuteNonQueryAsync() != 1)
            throw new InvalidOperationException($"Zeile {change.Id} hat sich zwischenzeitlich geaendert. Transaktion zurueckgerollt.");
        updated++;
    }

    foreach (var number in newRailwayCustomers)
    {
        var name = planned.First(x => string.Equals(x.CustomerNumber, number, StringComparison.OrdinalIgnoreCase)).CustomerName;
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO CustomerMarketSegments (Tsc,CustomerNumber,CustomerName,Segment,IsConfirmed,ProposalNote,Source,UpdatedAtUtc)
            SELECT 'TRDE',$number,$name,'Railway',1,'','Alphaplan Kundenstamm / Branche',$now
            WHERE NOT EXISTS(SELECT 1 FROM CustomerMarketSegments WHERE Tsc='TRDE' AND CustomerNumber=$number);
            UPDATE CustomerMarketSegments
            SET CustomerName=$name, Segment='Railway', IsConfirmed=1, ProposalNote='',
                Source='Alphaplan Kundenstamm / Branche', UpdatedAtUtc=$now
            WHERE Tsc='TRDE' AND CustomerNumber=$number AND (IsConfirmed=0 OR Segment='Railway');
            """;
        command.Parameters.AddWithValue("$number", number);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    await transaction.CommitAsync();
}

File.WriteAllText(appliedMarker, $"{DateTime.UtcNow:O} {updated} Zeilen nachgezogen");
Console.WriteLine($"{updated} Zeilen in einer Transaktion nachgezogen.");

// ------------------------------------------------------------- Gegenzaehlung
var after = await db.CentralSalesRecords.AsNoTracking()
    .Where(x => x.SiteId == site.Id).OrderBy(x => x.Id).ToListAsync();
var salesTotalAfter = after.Sum(x => x.SalesPriceValue);
Console.WriteLine($"TRDE-Zeilen nachher: {after.Count} (vorher {rows.Count})");
Console.WriteLine($"mit fachlicher Nummer nachher: {after.Count(HasBusinessNumber)} (vorher {rows.Count(HasBusinessNumber)})");
Console.WriteLine($"SalesPriceValue gesamt nachher: {salesTotalAfter.ToString("0.00", CultureInfo.InvariantCulture)}");
if (after.Count != rows.Count) throw new InvalidOperationException("Zeilenzahl hat sich geaendert.");
if (salesTotalAfter != salesTotalBefore) throw new InvalidOperationException("SalesPriceValue hat sich geaendert.");
Console.WriteLine("Zeilenzahl und Umsatzsumme unveraendert.");

return;

static bool HasBusinessNumber(CentralSalesRecord row)
{
    var number = (row.CustomerNumber ?? string.Empty).Trim();
    return number.Length > 0 && !number.StartsWith("ALPHAPLAN-ID:", StringComparison.OrdinalIgnoreCase);
}

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

internal sealed record MasterAddress(string Name, string Country, string Industry);

internal sealed record Change(
    int Id,
    string InvoiceNumber,
    string OldCustomerNumber,
    string CustomerNumber,
    string CustomerName,
    string CustomerCountry,
    string CustomerIndustry);
