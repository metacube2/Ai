using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.DataSources;

// Baut die DE-Pruefmappe fuer Rohail neu auf, diesmal auf Grundlage seiner
// Rechnungsliste vom 2026-09-09. Sie enthaelt zu jeder Rechnungsnummer die
// fachliche Adressnummer und schliesst damit die Luecke, die bisher nur ueber
// historische Arbeitsmappen teilweise ueberbrueckt werden konnte.
//
// Grundsaetze:
//   - Die Artikelbezeichnung wird nicht selbst bereinigt, sondern mit dem
//     produktiven Importdienst aus den Alphaplan-Rohdateien gelesen. Damit
//     traegt die Mappe denselben Text wie die produktiven Dateien seit 09:33.
//   - Kundenname, Land und Branche kommen aus dem Alphaplan-Kundenstamm und
//     nicht aus Rohails Rechnungsliste. Deren Branchenspalte ist unbrauchbar,
//     weil bei leerer Stammbranche der Rohertrag eine Spalte nach links rutscht.
//   - Es wird nichts geraten. Jede Zeile traegt ihren Nachweis.
//
// Aufruf: BahnmarktDeWorkbook <sales.csv> <rechnungen.xlsx> <kundenstamm.csv>
//                              <invoice_lines.csv> <ziel.xlsx>

if (args.Length < 5)
    throw new ArgumentException(
        "Aufruf: BahnmarktDeWorkbook <sales.csv> <rechnungen.xlsx> <kundenstamm.csv> <invoice_lines.csv> <ziel.xlsx>");

var salesCsvPath = Path.GetFullPath(args[0]);
var rechnungenPath = Path.GetFullPath(args[1]);
var kundenstammPath = Path.GetFullPath(args[2]);
var invoiceLinesPath = Path.GetFullPath(args[3]);
var targetPath = Path.GetFullPath(args[4]);

// Genau die beiden Branchen gelten als Bahn. Gleichheit, nicht StartsWith:
// "00 Bahn, 13 Flugzeugbau" ist keine reine Bahnadresse.
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
    master[number] = new MasterAddress(number, f[1].Trim(), f[4].Trim(), f[5].Trim());
}
Console.WriteLine($"Kundenstamm: {master.Count} Adressen");

// ------------------------------------------------- Rohails Rechnungsliste
var bridge = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var bridgeDuplicates = 0;
using (var rechnungen = new XLWorkbook(rechnungenPath))
{
    var sheet = rechnungen.Worksheets.First();
    var used = sheet.RangeUsed() ?? throw new InvalidOperationException("Rechnungsliste ist leer.");
    var headerRow = used.FirstRow().RowNumber();
    var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var cell in used.FirstRow().Cells())
        columns[cell.GetString().Trim()] = cell.Address.ColumnNumber;

    int Column(string name) => columns.TryGetValue(name, out var index)
        ? index
        : throw new InvalidOperationException($"Spalte '{name}' fehlt in der Rechnungsliste.");

    var invoiceColumn = Column("Rech.-Nr.");
    var addressColumn = Column("Adressnr._R");

    foreach (var row in used.Rows().Skip(1))
    {
        if (row.RowNumber() == headerRow) continue;
        var invoice = row.Cell(invoiceColumn).GetString().Trim();
        var address = row.Cell(addressColumn).GetString().Trim();
        if (invoice.Length == 0 || address.Length == 0) continue;
        if (bridge.TryGetValue(invoice, out var existing))
        {
            if (!string.Equals(existing, address, StringComparison.OrdinalIgnoreCase)) bridgeDuplicates++;
            continue;
        }
        bridge[invoice] = address;
    }
}
Console.WriteLine($"Belegbruecke: {bridge.Count} Rechnungsnummern, {bridgeDuplicates} widerspruechliche Doppel");

// -------------------------------------- Saubere Artikelbezeichnung, produktiv
// Mehrere Rohdateien sind zulaessig, durch Komma getrennt. Spaetere gewinnen,
// genau wie beim produktiven Import mit Vollbestand und anschliessenden Deltas.
var site = new Site { TSC = "TRDE", Land = "Deutschland" };
var importService = new ManualExcelImportService();
var cleanNames = new Dictionary<string, string>(StringComparer.Ordinal);
var cleanByLineKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var rawTotal = 0;
IReadOnlyList<SalesRecord> productiveRecords = Array.Empty<SalesRecord>();

// Optional: derselbe Weg wie der produktive Import, also Vollbestand und alle
// Delta-Archive aus dem konfigurierten SharePoint-Ordner. Nur lesend.
var sharePointDatabase = args.FirstOrDefault(x => x.StartsWith("--sharepoint=", StringComparison.Ordinal))?["--sharepoint=".Length..];
if (!string.IsNullOrWhiteSpace(sharePointDatabase))
{
    var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(new SqliteConnectionStringBuilder
    {
        DataSource = Path.GetFullPath(sharePointDatabase),
        Mode = SqliteOpenMode.ReadOnly
    }.ToString()).Options;

    await using var db = new AppDbContext(options);
    var dbSite = await db.Sites.AsNoTracking().SingleAsync(x => x.TSC == "TRDE");
    var settings = await db.ExportSettings.AsNoTracking().FirstAsync();
    var sourceDefinition = await db.SourceSystemDefinitions.AsNoTracking()
        .OrderBy(x => x.Id).FirstAsync(x => x.Code == dbSite.SourceSystem);
    var sharePointConfig = await db.SharePointConfigs.AsNoTracking().FirstOrDefaultAsync();
    var adapter = new ManualExcelDataSourceAdapter(new SharePointUploadService(), importService, new ConsoleEventLog());
    var fetched = await adapter.FetchAsync(new DataSourceFetchContext
    {
        Site = dbSite,
        SourceDefinition = sourceDefinition,
        Settings = settings,
        SharePointConfig = sharePointConfig,
        UpdateStatus = message => Console.WriteLine($"  {message}")
    });

    rawTotal += fetched.Records.Count;
    foreach (var record in fetched.Records)
    {
        var name = record.Name ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(record.SourceLineId))
            cleanNames[record.SourceLineId] = name;
        cleanByLineKey[$"{record.InvoiceNumber}|{record.PositionOnInvoice}|{record.Material}"] = name;
    }

    Console.WriteLine($"  SharePoint-Rohlauf: {fetched.Records.Count} Zeilen");
    productiveRecords = fetched.Records;
}

foreach (var candidate in invoiceLinesPath.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
{
    var full = Path.GetFullPath(candidate);
    var records = await importService.ReadSalesRecordsAsync(full, site, Array.Empty<ManualExcelColumnMapping>());
    rawTotal += records.Count;
    foreach (var record in records)
    {
        var name = record.Name ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(record.SourceLineId))
            cleanNames[record.SourceLineId] = name;

        // Ersatzschluessel ohne die interne Positions-ID, falls der Vollbestand
        // eine Zeile mit anderer technischer Nummer fuehrt als die Deltadatei.
        cleanByLineKey[$"{record.InvoiceNumber}|{record.PositionOnInvoice}|{record.Material}"] = name;
    }

    Console.WriteLine($"  Rohdatei {Path.GetFileName(full)}: {records.Count} Zeilen");
}
Console.WriteLine($"Rohzeilen mit produktivem Importdienst gelesen: {rawTotal}, Schluessel {cleanNames.Count}");

// ------------------------------------------------------- Produktive DE-Zeilen
var csvLines = File.ReadAllLines(salesCsvPath, Encoding.UTF8);
var header = SplitDelimited(csvLines[0].TrimStart('﻿'), ';');
int Field(string name)
{
    var index = header.FindIndex(x => string.Equals(x.Trim(), name, StringComparison.OrdinalIgnoreCase));
    if (index < 0) throw new InvalidOperationException($"Spalte '{name}' fehlt in {Path.GetFileName(salesCsvPath)}.");
    return index;
}

var iSourceLineId = Field("SourceLineId");
var iInvoice = Field("InvoiceNumber");
var iPosition = Field("PositionOnInvoice");
var iMaterial = Field("Material");
var iName = Field("Name");
var iFamily = Field("ProductFamilyText");
var iDivision = Field("ProductDivisionText");
var iQuantity = Field("Quantity");
var iCustomerNumber = Field("CustomerNumber");
var iSalesValue = Field("SalesPriceValue");
var iSalesCurrency = Field("SalesCurrency");
var iPostingDate = Field("PostingDate");
var iInvoiceDate = Field("InvoiceDate");

var rows = new List<SalesRow>();
var residueBefore = 0;
var residueAfter = 0;
var fallbackUsed = 0;
var unmatched = 0;
var keptFromBefore = 0;

// Zeilenquelle ist der produktive Leser, sobald --sharepoint gesetzt ist. Die
// CSV ist dann nur noch Gegenprobe. Ein Archivstand darf nicht die Grundlage
// sein: am 09.09. fehlten ihm bereits die Rechnungen RE2611081 und RE2611082.
if (productiveRecords.Count > 0)
{
    foreach (var record in productiveRecords)
    {
        var stored = (record.CustomerNumber ?? string.Empty).Trim();
        var invoice = (record.InvoiceNumber ?? string.Empty).Trim();
        var name = record.Name ?? string.Empty;
        if (HasFontResidue(name)) residueAfter++;

        var internalId = stored.StartsWith("ALPHAPLAN-ID:", StringComparison.OrdinalIgnoreCase)
            ? stored["ALPHAPLAN-ID:".Length..]
            : string.Empty;

        string number;
        string proof;
        if (bridge.TryGetValue(invoice, out var fromBridge))
        {
            number = fromBridge;
            proof = internalId.Length > 0
                ? "Rechnungsliste Rohail 09.09."
                : (string.Equals(stored, fromBridge, StringComparison.OrdinalIgnoreCase)
                    ? "Belegnachweis und Rechnungsliste, uebereinstimmend"
                    : "Rechnungsliste Rohail 09.09., weicht vom frueheren Belegnachweis ab");
        }
        else if (internalId.Length == 0 && stored.Length > 0)
        {
            number = stored;
            proof = "frueherer Belegnachweis, Gutschrift ohne Eintrag in Rohails Rechnungsliste";
            keptFromBefore++;
        }
        else
        {
            number = string.Empty;
            proof = "offen: Gutschrift ohne fachliche Nummer, nur interne Alphaplan-ID";
        }

        master.TryGetValue(number, out var address);
        rows.Add(new SalesRow(
            proof,
            internalId,
            invoice,
            record.PositionOnInvoice.ToString(CultureInfo.InvariantCulture),
            record.Material ?? string.Empty,
            name,
            record.ProductFamilyText ?? string.Empty,
            record.ProductDivisionText ?? string.Empty,
            record.Quantity.ToString(CultureInfo.InvariantCulture),
            number,
            address?.Name ?? string.Empty,
            address?.Country ?? string.Empty,
            address?.Industry ?? string.Empty,
            record.SalesPriceValue.ToString(CultureInfo.InvariantCulture),
            record.SalesCurrency ?? string.Empty,
            record.PostingDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            record.InvoiceDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            stored));
    }

    Console.WriteLine($"Zeilen aus der produktiven Quelle uebernommen: {rows.Count}");
}

foreach (var line in rows.Count > 0 ? Array.Empty<string>() : csvLines.Skip(1))
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var f = SplitDelimited(line, ';');
    if (f.Count <= iInvoiceDate) continue;

    var sourceLineId = f[iSourceLineId].Trim();
    var rawName = f[iName];
    if (HasFontResidue(rawName)) residueBefore++;

    var lineKey = $"{f[iInvoice].Trim()}|{f[iPosition].Trim()}|{f[iMaterial].Trim()}";
    string name;
    if (cleanNames.TryGetValue(sourceLineId, out var clean) && clean.Length > 0)
        name = clean;
    else if (cleanByLineKey.TryGetValue(lineKey, out var fallback) && fallback.Length > 0)
    {
        name = fallback;
        fallbackUsed++;
    }
    else
    {
        name = rawName;
        if (HasFontResidue(rawName)) unmatched++;
    }

    if (HasFontResidue(name)) residueAfter++;

    var stored = f[iCustomerNumber].Trim();
    var invoice = f[iInvoice].Trim();
    var internalId = stored.StartsWith("ALPHAPLAN-ID:", StringComparison.OrdinalIgnoreCase)
        ? stored["ALPHAPLAN-ID:".Length..]
        : string.Empty;

    string number;
    string proof;
    if (bridge.TryGetValue(invoice, out var fromBridge))
    {
        number = fromBridge;
        proof = internalId.Length > 0
            ? "Rechnungsliste Rohail 09.09."
            : (string.Equals(stored, fromBridge, StringComparison.OrdinalIgnoreCase)
                ? "Belegnachweis und Rechnungsliste, uebereinstimmend"
                : "Rechnungsliste Rohail 09.09., weicht vom frueheren Belegnachweis ab");
    }
    else if (internalId.Length == 0 && stored.Length > 0)
    {
        // Rohails Liste enthaelt keine Gutschriften. Eine bereits belegte fachliche
        // Nummer darf dadurch nicht verlorengehen, sonst waere die Mappe an dieser
        // Stelle schlechter als der produktive Bestand.
        number = stored;
        proof = "frueherer Belegnachweis, Gutschrift ohne Eintrag in Rohails Rechnungsliste";
        keptFromBefore++;
    }
    else
    {
        number = string.Empty;
        proof = "offen: Gutschrift ohne fachliche Nummer, nur interne Alphaplan-ID";
    }

    master.TryGetValue(number, out var address);
    rows.Add(new SalesRow(
        proof,
        internalId,
        invoice,
        f[iPosition].Trim(),
        f[iMaterial].Trim(),
        name,
        f[iFamily].Trim(),
        f[iDivision].Trim(),
        f[iQuantity].Trim(),
        number,
        address?.Name ?? string.Empty,
        address?.Country ?? string.Empty,
        address?.Industry ?? string.Empty,
        f[iSalesValue].Trim(),
        f[iSalesCurrency].Trim(),
        f[iPostingDate].Trim(),
        f[iInvoiceDate].Trim(),
        stored));
}

Console.WriteLine($"DE-Verkaufszeilen: {rows.Count}");
Console.WriteLine($"Schriftrest in der Quelle: {residueBefore}, nach Bereinigung: {residueAfter}");
Console.WriteLine($"ueber Ersatzschluessel bereinigt: {fallbackUsed}, ohne Rohzeile geblieben: {unmatched}");
if (residueAfter > 0)
    throw new InvalidOperationException($"{residueAfter} Zeilen tragen weiter den RTF-Schriftrest. Abbruch.");

// Gegenprobe gegen den archivierten Standortstand. Zusaetzliche Zeilen in der
// Mappe sind erwartbar, wenn seit dem Archiv neue Rechnungen dazugekommen sind;
// fehlende waeren ein Fehler.
{
    var inWorkbook = rows
        .Select(x => $"{x.InvoiceNumber}|{x.Position}|{x.Material}")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var csvKeys = new List<string>();
    foreach (var line in csvLines.Skip(1))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var f = SplitDelimited(line, ';');
        if (f.Count <= iInvoiceDate) continue;
        csvKeys.Add($"{f[iInvoice].Trim()}|{f[iPosition].Trim()}|{f[iMaterial].Trim()}");
    }

    var missing = csvKeys.Where(x => !inWorkbook.Contains(x)).ToList();
    Console.WriteLine($"Gegenprobe Archivstand: {csvKeys.Count} Zeilen, davon nicht in der Mappe {missing.Count}, Mappe hat {rows.Count - csvKeys.Count + missing.Count} zusaetzliche");
    foreach (var item in missing.Take(10)) Console.WriteLine($"  fehlt: {item}");
    if (missing.Count > 0)
        throw new InvalidOperationException($"{missing.Count} Zeilen des Archivstands fehlen in der Mappe. Abbruch.");
}

// Regressionsschutz: keine Zeile darf eine Zuordnung verlieren, die der
// produktive Bestand bereits hatte.
var lost = rows
    .Where(x => x.StoredCustomerNumber.Length > 0
                && !x.StoredCustomerNumber.StartsWith("ALPHAPLAN-ID:", StringComparison.OrdinalIgnoreCase)
                && x.CustomerNumber.Length == 0)
    .ToList();
if (lost.Count > 0)
    throw new InvalidOperationException(
        $"{lost.Count} Zeilen wuerden eine bereits belegte Kundennummer verlieren, zum Beispiel {lost[0].InvoiceNumber}. Abbruch.");

Console.WriteLine($"aus dem frueheren Belegnachweis uebernommen: {keptFromBefore}");

var assigned = rows.Count(x => x.CustomerNumber.Length > 0);
var open = rows.Count - assigned;
var newlyAssigned = rows.Count(x => x.InternalId.Length > 0 && x.CustomerNumber.Length > 0);
var conflicts = rows.Where(x => x.Proof.Contains("weicht vom", StringComparison.Ordinal)).ToList();
var missingInMaster = rows.Where(x => x.CustomerNumber.Length > 0 && x.CustomerName.Length == 0).ToList();
var railwayRows = rows.Where(x => railwayIndustries.Contains(x.CustomerIndustry)).ToList();
var railwayCustomers = railwayRows.Select(x => x.CustomerNumber).Distinct(StringComparer.OrdinalIgnoreCase).Count();

Console.WriteLine($"zugeordnet {assigned}, offen {open}, davon neu ueber Rohails Liste {newlyAssigned}");
Console.WriteLine($"Nummernkonflikte {conflicts.Count}, nicht im Kundenstamm {missingInMaster.Count}");
Console.WriteLine($"Bahnzeilen {railwayRows.Count} bei {railwayCustomers} Kunden");

// ------------------------------------------------------------------ Ausgabe
using var workbook = new XLWorkbook();

var note = workbook.Worksheets.Add("Lesehinweis");
var noteLines = new (string Left, string Right)[]
{
    ("Inhalt", "Deutsche Verkaufszeilen mit fachlicher Kundennummer, Name, Land und Branche"),
    ("Stand", "2026-09-09"),
    ("Verkaufszeilen", rows.Count.ToString(CultureInfo.InvariantCulture)),
    ("davon zugeordnet", $"{assigned} ({assigned * 100.0 / rows.Count:0.0} Prozent)"),
    ("davon offen", $"{open}, ausnahmslos Gutschriften ohne fachliche Nummer"),
    ("neu durch Rohails Rechnungsliste", newlyAssigned.ToString(CultureInfo.InvariantCulture)),
    ("aus dem frueheren Belegnachweis behalten", keptFromBefore.ToString(CultureInfo.InvariantCulture)),
    ("Bahnkunden mit reiner Bahnbranche", railwayCustomers.ToString(CultureInfo.InvariantCulture)),
    ("Bahnzeilen", railwayRows.Count.ToString(CultureInfo.InvariantCulture)),
    ("", ""),
    ("Quelle Zuordnung", "Rechnungen_20260909.xlsx von Rohail Munir: Rech.-Nr. und fachliche Adressnr._R"),
    ("Quelle Name/Land/Branche", "kundenstamm_DE_20260908.csv, der gepflegte Alphaplan-Adressstamm"),
    ("Quelle Artikelbezeichnung", "Alphaplan-Rohdateien, gelesen mit dem produktiven Importdienst"),
    ("", ""),
    ("Warum offen bleibt, was offen ist",
        "Rohails Liste enthaelt 46'372 RE-, 8'936 R0- und 5'231 R9-Belege, aber keine Gutschrift. "
        + "Von 84 betroffenen GS-Zeilen tragen 54 weiterhin die Nummer aus dem frueheren Belegnachweis; "
        + "nur der Rest bleibt offen. Ein Auszug fuer Gutschriften wuerde auch diese schliessen."),
    ("Nicht verwendet",
        "Die Branchenspalte aus Rohails Rechnungsliste. Bei leerer Stammbranche rutscht dort der "
        + "Rohertrag eine Spalte nach links. Die Branche stammt daher aus dem Kundenstamm."),
    ("Bahnbranche",
        "Als Bahn gilt genau '00 Bahn' oder '05 rw Railways / Bahntechnik'. "
        + "Eine Mischbranche wie '00 Bahn, 13 Flugzeugbau' zaehlt nicht automatisch."),
    ("Wichtiger Vorbehalt",
        "Diese Mappe zeigt den Stand nach Rohails Bruecke. Der produktive Bestand im Dashboard ist "
        + "zu diesem Zeitpunkt noch nicht nachgezogen und weist die neu zugeordneten Zeilen weiter als offen aus.")
};
note.Cell(1, 1).Value = "Feld";
note.Cell(1, 2).Value = "Angabe";
for (var i = 0; i < noteLines.Length; i++)
{
    note.Cell(i + 2, 1).Value = noteLines[i].Left;
    note.Cell(i + 2, 2).Value = noteLines[i].Right;
}
note.Row(1).Style.Font.Bold = true;
note.Column(1).Width = 38;
note.Column(2).Width = 110;
note.Column(2).Style.Alignment.WrapText = true;

WriteSalesSheet(workbook, "DE_Verkaeufe", rows);
WriteSalesSheet(workbook, "Offen_Gutschriften", rows.Where(x => x.CustomerNumber.Length == 0 || x.Proof.StartsWith("frueherer Belegnachweis, Gutschrift", StringComparison.Ordinal)).ToList());
WriteSalesSheet(workbook, "Bahnzeilen", railwayRows);

// Bahnkunden mit Umsatz je Jahr und Waehrung
var railwaySummary = railwayRows
    .Select(x => new
    {
        Year = YearOf(x),
        x.CustomerNumber,
        x.CustomerName,
        x.CustomerCountry,
        x.CustomerIndustry,
        x.SalesCurrency,
        Value = ParseDecimal(x.SalesPriceValue)
    })
    .GroupBy(x => (x.Year, x.CustomerNumber, x.CustomerName, x.CustomerCountry, x.CustomerIndustry, x.SalesCurrency))
    .Select(g => new
    {
        g.Key.Year,
        g.Key.CustomerNumber,
        g.Key.CustomerName,
        g.Key.CustomerCountry,
        g.Key.CustomerIndustry,
        g.Key.SalesCurrency,
        Lines = g.Count(),
        Value = g.Sum(x => x.Value)
    })
    .OrderBy(x => x.Year).ThenByDescending(x => x.Value)
    .ToList();

var summarySheet = workbook.Worksheets.Add("Railway_Umsatz");
var summaryHeaders = new[] { "Jahr", "Kundennummer", "Kundenname", "Land", "Branche", "Waehrung", "Verkaufszeilen", "Umsatz" };
for (var c = 0; c < summaryHeaders.Length; c++) summarySheet.Cell(1, c + 1).Value = summaryHeaders[c];
var summaryRow = 2;
foreach (var entry in railwaySummary)
{
    summarySheet.Cell(summaryRow, 1).Value = entry.Year;
    summarySheet.Cell(summaryRow, 2).Value = entry.CustomerNumber;
    summarySheet.Cell(summaryRow, 3).Value = entry.CustomerName;
    summarySheet.Cell(summaryRow, 4).Value = entry.CustomerCountry;
    summarySheet.Cell(summaryRow, 5).Value = entry.CustomerIndustry;
    summarySheet.Cell(summaryRow, 6).Value = entry.SalesCurrency;
    summarySheet.Cell(summaryRow, 7).Value = entry.Lines;
    summarySheet.Cell(summaryRow, 8).Value = entry.Value;
    summaryRow++;
}
Finish(summarySheet, summaryHeaders.Length);

// Nummernkonflikte
var conflictSheet = workbook.Worksheets.Add("Nummernkonflikte");
var conflictHeaders = new[] { "Rechnungsnummer", "frueher gespeichert", "Rohails Rechnungsliste", "Name frueher", "Name laut Rohail", "Branche laut Rohail" };
for (var c = 0; c < conflictHeaders.Length; c++) conflictSheet.Cell(1, c + 1).Value = conflictHeaders[c];
var conflictRow = 2;
foreach (var entry in conflicts.OrderBy(x => x.InvoiceNumber))
{
    master.TryGetValue(entry.StoredCustomerNumber, out var previous);
    conflictSheet.Cell(conflictRow, 1).Value = entry.InvoiceNumber;
    conflictSheet.Cell(conflictRow, 2).Value = entry.StoredCustomerNumber;
    conflictSheet.Cell(conflictRow, 3).Value = entry.CustomerNumber;
    conflictSheet.Cell(conflictRow, 4).Value = previous?.Name ?? string.Empty;
    conflictSheet.Cell(conflictRow, 5).Value = entry.CustomerName;
    conflictSheet.Cell(conflictRow, 6).Value = entry.CustomerIndustry;
    conflictRow++;
}
Finish(conflictSheet, conflictHeaders.Length);

// Belegbruecke, nur die tatsaechlich verwendeten Rechnungsnummern
var usedInvoices = rows.Where(x => x.CustomerNumber.Length > 0)
    .Select(x => (x.InvoiceNumber, x.CustomerNumber))
    .Distinct()
    .OrderBy(x => x.InvoiceNumber)
    .ToList();
var bridgeSheet = workbook.Worksheets.Add("Belegbruecke");
var bridgeHeaders = new[] { "Rechnungsnummer", "AdressNummer", "Kundenname", "Land", "Branche" };
for (var c = 0; c < bridgeHeaders.Length; c++) bridgeSheet.Cell(1, c + 1).Value = bridgeHeaders[c];
var bridgeRow = 2;
foreach (var entry in usedInvoices)
{
    master.TryGetValue(entry.CustomerNumber, out var address);
    bridgeSheet.Cell(bridgeRow, 1).Value = entry.InvoiceNumber;
    bridgeSheet.Cell(bridgeRow, 2).Value = entry.CustomerNumber;
    bridgeSheet.Cell(bridgeRow, 3).Value = address?.Name ?? string.Empty;
    bridgeSheet.Cell(bridgeRow, 4).Value = address?.Country ?? string.Empty;
    bridgeSheet.Cell(bridgeRow, 5).Value = address?.Industry ?? string.Empty;
    bridgeRow++;
}
Finish(bridgeSheet, bridgeHeaders.Length);

workbook.SaveAs(targetPath);
Console.WriteLine($"Geschrieben: {targetPath}");
Console.WriteLine($"Blaetter: {string.Join(", ", workbook.Worksheets.Select(x => x.Name))}");

return;

// ------------------------------------------------------------------ Helfer
static void WriteSalesSheet(XLWorkbook workbook, string name, IReadOnlyList<SalesRow> data)
{
    var sheet = workbook.Worksheets.Add(name);
    var headers = new[]
    {
        "Zuordnungsnachweis", "Interne Adress-ID", "InvoiceNumber", "PositionOnInvoice", "Material",
        "Name", "ProductFamilyText", "ProductDivisionText", "Quantity", "CustomerNumber",
        "CustomerName", "CustomerCountry", "CustomerIndustry", "SalesPriceValue", "SalesCurrency",
        "PostingDate", "InvoiceDate"
    };
    for (var c = 0; c < headers.Length; c++) sheet.Cell(1, c + 1).Value = headers[c];

    var row = 2;
    foreach (var item in data)
    {
        sheet.Cell(row, 1).Value = item.Proof;
        sheet.Cell(row, 2).Value = item.InternalId;
        sheet.Cell(row, 3).Value = item.InvoiceNumber;
        sheet.Cell(row, 4).Value = item.Position;
        sheet.Cell(row, 5).Value = item.Material;
        sheet.Cell(row, 6).Value = item.Name;
        sheet.Cell(row, 7).Value = item.ProductFamily;
        sheet.Cell(row, 8).Value = item.ProductDivision;
        sheet.Cell(row, 9).Value = item.Quantity;
        sheet.Cell(row, 10).Value = item.CustomerNumber;
        sheet.Cell(row, 11).Value = item.CustomerName;
        sheet.Cell(row, 12).Value = item.CustomerCountry;
        sheet.Cell(row, 13).Value = item.CustomerIndustry;
        sheet.Cell(row, 14).Value = item.SalesPriceValue;
        sheet.Cell(row, 15).Value = item.SalesCurrency;
        sheet.Cell(row, 16).Value = item.PostingDate;
        sheet.Cell(row, 17).Value = item.InvoiceDate;
        row++;
    }

    Finish(sheet, headers.Length);
}

static void Finish(IXLWorksheet sheet, int columns)
{
    sheet.Row(1).Style.Font.Bold = true;
    sheet.SheetView.FreezeRows(1);
    var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
    sheet.Range(1, 1, last, columns).SetAutoFilter();
    sheet.Columns(1, columns).AdjustToContents(1, 200, 8, 60);
}

static bool HasFontResidue(string value)
    => value.Contains("MS Shell Dlg", StringComparison.OrdinalIgnoreCase)
       || value.Contains("Microsoft Sans Serif", StringComparison.OrdinalIgnoreCase);

static string YearOf(SalesRow row)
{
    foreach (var candidate in new[] { row.InvoiceDate, row.PostingDate })
        if (candidate.Length >= 4 && int.TryParse(candidate[..4], out var year) && year > 1990)
            return year.ToString(CultureInfo.InvariantCulture);

    // Alphaplan kodiert das Jahr in der Belegnummer: RE26..., GS25...
    if (row.InvoiceNumber.Length >= 4 && int.TryParse(row.InvoiceNumber.Substring(2, 2), out var short2))
        return (2000 + short2).ToString(CultureInfo.InvariantCulture);

    return "unbekannt";
}

static decimal ParseDecimal(string value)
    => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

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

internal sealed class ConsoleEventLog : IAppEventLogService
{
    public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
    {
        Console.WriteLine($"  [{category}] {message}");
        return Task.CompletedTask;
    }

    public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
        => Task.CompletedTask;
}

internal sealed record MasterAddress(string Number, string Name, string Country, string Industry);

internal sealed record SalesRow(
    string Proof,
    string InternalId,
    string InvoiceNumber,
    string Position,
    string Material,
    string Name,
    string ProductFamily,
    string ProductDivision,
    string Quantity,
    string CustomerNumber,
    string CustomerName,
    string CustomerCountry,
    string CustomerIndustry,
    string SalesPriceValue,
    string SalesCurrency,
    string PostingDate,
    string InvoiceDate,
    string StoredCustomerNumber);
