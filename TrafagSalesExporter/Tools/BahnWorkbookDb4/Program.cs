using System.Globalization;
using ClosedXML.Excel;

if (args.Length != 3)
    throw new ArgumentException("Aufruf: BahnWorkbookDb4 <bahn.xlsx> <adressen.xlsx> <rechnungen.xlsx>");

var targetPath = Path.GetFullPath(args[0]);
var addressPath = Path.GetFullPath(args[1]);
var invoicePath = Path.GetFullPath(args[2]);

var railwayIndustries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "00 Bahn",
    "05 rw Railways / Bahntechnik"
};

var addresses = new Dictionary<string, Address>(StringComparer.OrdinalIgnoreCase);
using (var source = new XLWorkbook(addressPath))
{
    var sheet = source.Worksheets.First();
    var columns = Columns(sheet);
    var numberColumn = Required(columns, "Adress-Nr.");
    var name1Column = Required(columns, "Name 1");
    var name2Column = Required(columns, "Name 2");
    var countryColumn = Required(columns, "Land");
    var industryColumn = Required(columns, "Branche");

    foreach (var row in DataRows(sheet))
    {
        var number = Text(row.Cell(numberColumn));
        if (number.Length == 0) continue;
        var name = string.Join(" ", new[] { Text(row.Cell(name1Column)), Text(row.Cell(name2Column)) }
            .Where(x => x.Length > 0));
        addresses[number] = new Address(number, name, Text(row.Cell(countryColumn)), Text(row.Cell(industryColumn)));
    }
}

var railwayAddresses = addresses.Values
    .Where(x => railwayIndustries.Contains(x.Industry))
    .ToDictionary(x => x.Number, StringComparer.OrdinalIgnoreCase);

var invoices = new List<Invoice>();
var documentTypes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var invoiceAddressKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var unmatchedAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
using (var source = new XLWorkbook(invoicePath))
{
    var sheet = source.Worksheets.First();
    var columns = Columns(sheet);
    var dateColumn = Required(columns, "Datum");
    var invoiceColumn = Required(columns, "Rech.-Nr.");
    var addressColumn = Required(columns, "Adressnr._R");
    var netColumn = Required(columns, "Netto (SW)");
    var profitColumn = Required(columns, "RohertragEndSumme");

    foreach (var row in DataRows(sheet))
    {
        var invoiceNumber = Text(row.Cell(invoiceColumn));
        if (invoiceNumber.Length == 0) continue;
        var addressNumber = Text(row.Cell(addressColumn));
        if (addressNumber.Length > 0) invoiceAddressKeys.Add(addressNumber);
        var prefix = invoiceNumber.Length >= 2 ? invoiceNumber[..2] : invoiceNumber;
        documentTypes[prefix] = documentTypes.GetValueOrDefault(prefix) + 1;
        if (!addresses.ContainsKey(addressNumber)) unmatchedAddresses.Add(addressNumber);
        if (!railwayAddresses.TryGetValue(addressNumber, out var address)) continue;

        var date = Date(row.Cell(dateColumn), invoiceNumber);
        var net = NullableDecimal(row.Cell(netColumn), $"Netto (SW), Beleg {invoiceNumber}");
        var profit = NullableDecimal(row.Cell(profitColumn), $"RohertragEndSumme, Beleg {invoiceNumber}");
        invoices.Add(new Invoice(date, invoiceNumber, address, net, profit));
    }
}

if (invoices.Count == 0) throw new InvalidOperationException("Keine Bahnrechnungen gefunden.");
if (documentTypes.ContainsKey("GS"))
    throw new InvalidOperationException("Die Quelle enthaelt wider Erwarten GS-Belege; die Hinweistexte muessen angepasst werden.");

var duplicateInvoices = invoices.GroupBy(x => x.Number, StringComparer.OrdinalIgnoreCase)
    .Where(x => x.Count() > 1).Select(x => x.Key).ToList();
if (duplicateInvoices.Count > 0)
    throw new InvalidOperationException($"Doppelte Rechnungsnummern in der Bahnauswahl: {string.Join(", ", duplicateInvoices.Take(10))}");

using var workbook = new XLWorkbook(targetPath);
var summary = workbook.Worksheet("Railway_Umsatz");
var existingColumns = Columns(summary);
var yearColumn = Required(existingColumns, "Jahr");
var customerColumn = Required(existingColumns, "Kundennummer");
var targetYears = summary.RowsUsed().Skip(1)
    .Select(x => int.TryParse(Text(x.Cell(yearColumn)), out var year) ? year : 0)
    .Where(x => x > 0).ToHashSet();
invoices = invoices.Where(x => targetYears.Contains(x.Date.Year)).ToList();
if (invoices.Count == 0) throw new InvalidOperationException("Keine Bahnrechnungen fuer die Jahre der Zielmappe gefunden.");

// Wiederholbare Ausfuehrung: eine fruehere, von diesem Werkzeug geschriebene
// Spaltengruppe wird ersetzt statt rechts ein zweites Mal angehaengt.
if (existingColumns.TryGetValue("Rechnungen (ohne GS)", out var existingExtension))
{
    var first = existingExtension.Single();
    summary.Columns(first, first + 6).Delete();
    existingColumns = Columns(summary);
}

var invoiceGroups = invoices.GroupBy(x => (x.Date.Year, x.Address.Number), new YearCustomerComparer())
    .ToDictionary(
        x => x.Key,
        x => new InvoiceGroup(x.Count(), x.Count(y => y.NetSales.HasValue), x.Count(y => y.GrossProfit.HasValue), x.Sum(y => y.NetSales ?? 0m), x.Sum(y => y.GrossProfit ?? 0m)),
        new YearCustomerComparer());

var firstNewColumn = summary.LastColumnUsed()!.ColumnNumber() + 1;
var newHeaders = new[]
{
    "Rechnungen (ohne GS)",
    "Rechnungen mit Umsatz",
    "Rechnungen mit Rohertrag",
    "Umsatz Rechnungen (SW, ohne GS)",
    "RohertragEndSumme (ohne GS)",
    "Rohertrag % (ohne GS)",
    "DB4-Status"
};
for (var index = 0; index < newHeaders.Length; index++)
    summary.Cell(1, firstNewColumn + index).Value = newHeaders[index];

var matchedSummaryRows = 0;
foreach (var row in summary.RowsUsed().Skip(1))
{
    if (!int.TryParse(Text(row.Cell(yearColumn)), out var year)) continue;
    var customer = Text(row.Cell(customerColumn));
    if (!invoiceGroups.TryGetValue((year, customer), out var values)) continue;
    matchedSummaryRows++;
    row.Cell(firstNewColumn).Value = values.InvoiceCount;
    row.Cell(firstNewColumn + 1).Value = values.NetInvoiceCount;
    row.Cell(firstNewColumn + 2).Value = values.ProfitInvoiceCount;
    row.Cell(firstNewColumn + 3).Value = values.NetSales;
    row.Cell(firstNewColumn + 4).Value = values.GrossProfit;
    if (values.NetInvoiceCount == values.InvoiceCount && values.ProfitInvoiceCount == values.InvoiceCount && values.NetSales != 0)
    {
        row.Cell(firstNewColumn + 5).Value = values.GrossProfit / values.NetSales;
        row.Cell(firstNewColumn + 5).Style.NumberFormat.Format = "0.0%";
    }
    row.Cell(firstNewColumn + 6).Value = values.NetInvoiceCount == values.InvoiceCount && values.ProfitInvoiceCount == values.InvoiceCount
        ? "Rohertrag aus Alphaplan; fachlich nicht als DB4 bestaetigt"
        : "Umsatz/Rohertrag unvollstaendig; fachlich nicht als DB4 bestaetigt";
}

if (matchedSummaryRows == 0)
    throw new InvalidOperationException("Keine Rechnungssumme konnte einer Zeile in Railway_Umsatz zugeordnet werden.");

summary.Row(1).Style.Font.Bold = true;
summary.Range(1, 1, summary.LastRowUsed()!.RowNumber(), summary.LastColumnUsed()!.ColumnNumber()).SetAutoFilter();
summary.SheetView.FreezeRows(1);
summary.Columns(firstNewColumn, firstNewColumn + 5).AdjustToContents();
summary.Column(firstNewColumn + 6).Width = 58;

DeleteIfPresent(workbook, "Railway_Belege_DB");
var detail = workbook.Worksheets.Add("Railway_Belege_DB");
var detailHeaders = new[]
{
    "Jahr", "Rechnungsdatum", "Rechnungsnummer", "Kundennummer", "Kundenname",
    "Land", "Branche", "Waehrung", "Umsatz Netto (SW)", "RohertragEndSumme",
    "Rohertrag %", "DB4-Status"
};
for (var index = 0; index < detailHeaders.Length; index++) detail.Cell(1, index + 1).Value = detailHeaders[index];
var detailRow = 2;
foreach (var invoice in invoices.OrderBy(x => x.Date).ThenBy(x => x.Number))
{
    detail.Cell(detailRow, 1).Value = invoice.Date.Year;
    detail.Cell(detailRow, 2).Value = invoice.Date;
    detail.Cell(detailRow, 2).Style.DateFormat.Format = "dd.MM.yyyy";
    detail.Cell(detailRow, 3).Value = invoice.Number;
    detail.Cell(detailRow, 4).Value = invoice.Address.Number;
    detail.Cell(detailRow, 5).Value = invoice.Address.Name;
    detail.Cell(detailRow, 6).Value = invoice.Address.Country;
    detail.Cell(detailRow, 7).Value = invoice.Address.Industry;
    detail.Cell(detailRow, 8).Value = "EUR";
    if (invoice.NetSales.HasValue) detail.Cell(detailRow, 9).Value = invoice.NetSales.Value;
    if (invoice.GrossProfit.HasValue)
    {
        detail.Cell(detailRow, 10).Value = invoice.GrossProfit.Value;
        if (invoice.NetSales.HasValue && invoice.NetSales.Value != 0)
        {
            detail.Cell(detailRow, 11).Value = invoice.GrossProfit.Value / invoice.NetSales.Value;
            detail.Cell(detailRow, 11).Style.NumberFormat.Format = "0.0%";
        }
    }
    detail.Cell(detailRow, 12).Value = invoice.GrossProfit.HasValue
        ? "Rohertrag aus Alphaplan; fachlich nicht als DB4 bestaetigt"
        : "Rohertrag fehlt; fachlich nicht als DB4 bestaetigt";
    detailRow++;
}
Finish(detail, detailHeaders.Length);
detail.Column(12).Width = 58;

DeleteIfPresent(workbook, "DB4_Hinweis");
var note = workbook.Worksheets.Add("DB4_Hinweis", 2);
var notes = new (string Field, string Value)[]
{
    ("Zweck", "Ergaenzung der deutschen Bahn-Pruefmappe um die Werte aus Rohails Rechnungsliste."),
    ("Kundenquelle", Path.GetFileName(addressPath)),
    ("Rechnungsquelle", Path.GetFileName(invoicePath)),
    ("Bahnregel", "Exakt 00 Bahn oder 05 rw Railways / Bahntechnik im Alphaplan-Adressstamm."),
    ("Umsatz", "Netto (SW) aus der Rechnungsliste; SW ist die Alphaplan-Systemwaehrung Deutschland (EUR)."),
    ("Ergebniswert", "RohertragEndSumme aus der Rechnungsliste auf Belegebene."),
    ("DB4-Fachstatus", "Nicht bestaetigt. Die Quelldatei bezeichnet den Wert als Rohertrag, nicht als DB4."),
    ("Gutschriften", "Die Rechnungsliste enthaelt keine GS-Belege. Umsatz und Rohertrag aus dieser Quelle sind deshalb nicht um Gutschriften bereinigt."),
    ("Warum separates Detailblatt", "RohertragEndSumme ist ein Belegkopfwert. Eine Uebernahme je Verkaufsposition wuerde ihn bei mehrzeiligen Rechnungen vervielfachen."),
    ("Globaler Umfang", "Diese beiden Quellen decken ausschliesslich Alphaplan Deutschland ab. Andere Standorte sind damit nicht ergaenzt."),
    ("Railway-Adressen", railwayAddresses.Count.ToString(CultureInfo.InvariantCulture)),
    ("Railway-Rechnungen", invoices.Count.ToString(CultureInfo.InvariantCulture)),
    ("Railway-Rechnungen mit Umsatz", invoices.Count(x => x.NetSales.HasValue).ToString(CultureInfo.InvariantCulture)),
    ("Railway-Rechnungen mit Rohertrag", invoices.Count(x => x.GrossProfit.HasValue).ToString(CultureInfo.InvariantCulture)),
    ("Railway-Kunden mit Rechnung", invoices.Select(x => x.Address.Number).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString(CultureInfo.InvariantCulture)),
    ("Adressabdeckung Rechnungsliste", $"{invoiceAddressKeys.Count - unmatchedAddresses.Count} von {invoiceAddressKeys.Count} verwendeten Rechnungs-Adressnummern im Stamm; {unmatchedAddresses.Count} fehlen."),
    ("Belegarten", string.Join(", ", documentTypes.OrderBy(x => x.Key).Select(x => $"{x.Key}: {x.Value}")))
};
note.Cell(1, 1).Value = "Feld";
note.Cell(1, 2).Value = "Angabe";
for (var index = 0; index < notes.Length; index++)
{
    note.Cell(index + 2, 1).Value = notes[index].Field;
    note.Cell(index + 2, 2).Value = notes[index].Value;
}
note.Row(1).Style.Font.Bold = true;
note.Column(1).Width = 34;
note.Column(2).Width = 115;
note.Column(2).Style.Alignment.WrapText = true;

workbook.Save();

Console.WriteLine($"Adressen: {addresses.Count}, davon Bahn: {railwayAddresses.Count}");
foreach (var group in railwayAddresses.Values.GroupBy(x => x.Industry).OrderBy(x => x.Key))
    Console.WriteLine($"  {group.Key}: {group.Count()} Adressen");
Console.WriteLine($"Bahnrechnungen: {invoices.Count}, Kunden: {invoices.Select(x => x.Address.Number).Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
Console.WriteLine($"Railway_Umsatz: {matchedSummaryRows} Zeilen um Rechnungswerte erweitert");
foreach (var group in invoices.GroupBy(x => x.Date.Year).OrderBy(x => x.Key))
    Console.WriteLine($"{group.Key}: {group.Count()} Rechnungen, davon {group.Count(x => x.NetSales.HasValue)} mit Umsatz und {group.Count(x => x.GrossProfit.HasValue)} mit Rohertrag, Umsatz SW {group.Sum(x => x.NetSales ?? 0m):N2}, Rohertrag {group.Sum(x => x.GrossProfit ?? 0m):N2}");

return;

static Dictionary<string, List<int>> Columns(IXLWorksheet sheet)
{
    var header = sheet.FirstRowUsed() ?? throw new InvalidOperationException($"Blatt {sheet.Name} ist leer.");
    var result = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
    foreach (var cell in header.CellsUsed())
    {
        var name = Text(cell);
        if (!result.TryGetValue(name, out var positions)) result[name] = positions = new List<int>();
        positions.Add(cell.Address.ColumnNumber);
    }
    return result;
}

static int Required(Dictionary<string, List<int>> columns, string name, int occurrence = 1)
    => columns.TryGetValue(name, out var positions) && positions.Count >= occurrence
        ? positions[occurrence - 1]
        : throw new InvalidOperationException($"Spalte '{name}' (Vorkommen {occurrence}) fehlt.");

static IEnumerable<IXLRangeRow> DataRows(IXLWorksheet sheet)
{
    var used = sheet.RangeUsed() ?? throw new InvalidOperationException($"Blatt {sheet.Name} ist leer.");
    return used.Rows().Skip(1);
}

static string Text(IXLCell cell)
{
    if (cell.IsEmpty()) return string.Empty;
    if (cell.DataType == XLDataType.Number)
    {
        var number = cell.GetDouble();
        if (Math.Abs(number - Math.Round(number)) < 0.0000001)
            return Math.Round(number).ToString("0", CultureInfo.InvariantCulture);
        return number.ToString(CultureInfo.InvariantCulture);
    }
    return cell.GetFormattedString().Trim();
}

static decimal Decimal(IXLCell cell, string context)
{
    if (cell.DataType == XLDataType.Number) return Convert.ToDecimal(cell.GetDouble());
    var text = Text(cell);
    if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant)) return invariant;
    if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("de-DE"), out var german)) return german;
    throw new InvalidOperationException($"Kein Zahlenwert in {context}: '{text}'.");
}

static decimal? NullableDecimal(IXLCell cell, string context)
{
    if (cell.IsEmpty() || Text(cell).Length == 0) return null;
    return Decimal(cell, context);
}

static DateTime Date(IXLCell cell, string invoiceNumber)
{
    if (cell.TryGetValue<DateTime>(out var date)) return date.Date;
    var text = Text(cell);
    if (DateTime.TryParse(text, CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out date)) return date.Date;
    if (invoiceNumber.Length >= 4 && int.TryParse(invoiceNumber.Substring(2, 2), out var shortYear))
        return new DateTime(2000 + shortYear, 1, 1);
    throw new InvalidOperationException($"Kein Datum fuer Beleg {invoiceNumber}.");
}

static void DeleteIfPresent(XLWorkbook workbook, string name)
{
    var sheet = workbook.Worksheets.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    sheet?.Delete();
}

static void Finish(IXLWorksheet sheet, int columns)
{
    sheet.Row(1).Style.Font.Bold = true;
    sheet.SheetView.FreezeRows(1);
    var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
    sheet.Range(1, 1, last, columns).SetAutoFilter();
    sheet.Columns(1, columns).AdjustToContents(1, Math.Min(last, 300), 8, 55);
}

sealed record Address(string Number, string Name, string Country, string Industry);
sealed record Invoice(DateTime Date, string Number, Address Address, decimal? NetSales, decimal? GrossProfit);
sealed record InvoiceGroup(int InvoiceCount, int NetInvoiceCount, int ProfitInvoiceCount, decimal NetSales, decimal GrossProfit);

sealed class YearCustomerComparer : IEqualityComparer<(int Year, string Number)>
{
    public bool Equals((int Year, string Number) x, (int Year, string Number) y)
        => x.Year == y.Year && StringComparer.OrdinalIgnoreCase.Equals(x.Number, y.Number);

    public int GetHashCode((int Year, string Number) value)
        => HashCode.Combine(value.Year, StringComparer.OrdinalIgnoreCase.GetHashCode(value.Number));
}
