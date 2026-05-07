using System.Globalization;
using System.Net;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

var builder = WebApplication.CreateBuilder(args);

var databasePath = ResolveDatabasePath(builder.Configuration["FinanceProbe:DatabasePath"]);
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath};Default Timeout=60"));
builder.Services.AddSingleton<IFinanceReconciliationService, FinanceReconciliationService>();

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/finance"));
app.MapGet("/finance", async (IFinanceReconciliationService finance) =>
{
    var rows = await finance.BuildNetSalesReferenceRowsAsync(2025);
    var excelReferences = LoadCheckedExcelReferences(ResolveCheckedExcelPath());
    var spainCsv = LoadSpainSalesCsvProbe(ResolveSpainSalesCsvPath());
    var germanySample = LoadGermanyExcelProbe(ResolveGermanySamplePath());
    return Results.Content(BuildPage(rows, databasePath, excelReferences, spainCsv, germanySample), "text/html; charset=utf-8");
});

app.Run();

static string ResolveDatabasePath(string? configuredPath)
{
    if (!string.IsNullOrWhiteSpace(configuredPath))
        return Path.GetFullPath(configuredPath);

    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "trafag_exporter.db");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }
    }

    return Path.Combine(Directory.GetCurrentDirectory(), "trafag_exporter.db");
}

static string? ResolveCheckedExcelPath()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "check.xlsx");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }
    }

    return null;
}

static string? ResolveSpainSalesCsvPath()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            var directCandidate = Path.Combine(directory.FullName, "sagespain", "v2", "Spain_Sales_2025.csv");
            if (File.Exists(directCandidate))
                return directCandidate;

            var recursiveCandidate = Directory
                .EnumerateFiles(directory.FullName, "Spain_Sales_2025.csv", System.IO.SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(recursiveCandidate))
                return recursiveCandidate;

            directory = directory.Parent;
        }
    }

    return null;
}

static string? ResolveGermanySamplePath()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            var directCandidate = Path.Combine(directory.FullName, "DE_Beispiel_Export_Daten.xlsx");
            if (File.Exists(directCandidate))
                return directCandidate;

            var recursiveCandidate = Directory
                .EnumerateFiles(directory.FullName, "DE_Beispiel_Export_Daten.xlsx", System.IO.SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(recursiveCandidate))
                return recursiveCandidate;

            directory = directory.Parent;
        }
    }

    return null;
}

static Dictionary<string, CheckedExcelReference> LoadCheckedExcelReferences(string? path)
{
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        return [];

    using var workbook = new XLWorkbook(path);
    var worksheet = workbook.Worksheets.First();
    var references = new Dictionary<string, CheckedExcelReference>(StringComparer.OrdinalIgnoreCase);

    foreach (var row in worksheet.RowsUsed().Skip(1))
    {
        var label = row.Cell(1).GetString().Trim();
        if (string.IsNullOrWhiteSpace(label) || label.Equals("Total TR Gruppe", StringComparison.OrdinalIgnoreCase))
            continue;

        references[label] = new CheckedExcelReference
        {
            Label = label,
            LocalCurrencyValue = ReadNullableDecimal(row.Cell(2)),
            ChfValue = ReadNullableDecimal(row.Cell(3)),
            PowerBiValue = ReadNullableDecimal(row.Cell(5)),
            Status = row.Cell(6).GetString().Trim()
        };
    }

    return references;
}

static decimal? ReadNullableDecimal(IXLCell cell)
{
    if (cell.IsEmpty())
        return null;

    return cell.TryGetValue<decimal>(out var value) ? value : null;
}

static GermanyExcelProbe? LoadGermanyExcelProbe(string? path)
{
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        return null;

    using var workbook = new XLWorkbook(path);
    var worksheet = workbook.Worksheets.FirstOrDefault();
    var usedRange = worksheet?.RangeUsed();
    if (worksheet is null || usedRange is null)
        return null;

    var headerRow = usedRange.FirstRow();
    var headers = headerRow.CellsUsed()
        .ToDictionary(cell => cell.GetString().Trim(), cell => cell.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

    if (!headers.TryGetValue("NettoPreisGesamtX", out var amountColumn))
        return null;

    headers.TryGetValue("Währung", out var currencyColumn);
    headers.TryGetValue("Belegdatum-Rechnung", out var invoiceDateColumn);

    var total = 0m;
    var rowsWithAmount = 0;
    var rowsIn2025 = 0;
    var totalIn2025 = 0m;
    var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var row in usedRange.RowsUsed().Skip(1))
    {
        var value = ReadProbeDecimal(row.Cell(amountColumn));
        if (value == 0m)
            continue;

        total += value;
        rowsWithAmount++;

        if (currencyColumn > 0)
        {
            var currency = row.Cell(currencyColumn).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(currency))
                currencies.Add(currency);
        }

        if (invoiceDateColumn > 0 && TryReadProbeDate(row.Cell(invoiceDateColumn), out var invoiceDate) && invoiceDate.Year == 2025)
        {
            totalIn2025 += value;
            rowsIn2025++;
        }
    }

    return new GermanyExcelProbe
    {
        Path = path,
        RowsWithAmount = rowsWithAmount,
        SalesPriceValue = total,
        RowsIn2025 = rowsIn2025,
        SalesPriceValueIn2025 = totalIn2025,
        Currencies = string.Join(", ", currencies.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
    };
}

static decimal ReadProbeDecimal(IXLCell cell)
{
    if (cell.TryGetValue<decimal>(out var decimalValue))
        return decimalValue;

    var text = cell.GetString().Trim();
    if (string.IsNullOrWhiteSpace(text))
        return 0m;

    text = text
        .Replace("'", string.Empty)
        .Replace("’", string.Empty)
        .Replace(" ", string.Empty)
        .Replace(",", ".");

    return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
        ? parsed
        : 0m;
}

static bool TryReadProbeDate(IXLCell cell, out DateTime value)
{
    if (cell.TryGetValue<DateTime>(out value))
        return true;

    return DateTime.TryParse(cell.GetString(), CultureInfo.GetCultureInfo("de-CH"), DateTimeStyles.None, out value) ||
           DateTime.TryParse(cell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
}

static SpainSalesCsvProbe? LoadSpainSalesCsvProbe(string? path)
{
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        return null;

    using var parser = new TextFieldParser(path)
    {
        TextFieldType = FieldType.Delimited,
        HasFieldsEnclosedInQuotes = true,
        TrimWhiteSpace = false
    };
    parser.SetDelimiters(";");

    var header = parser.ReadFields();
    if (header is null)
        return null;

    var headerMap = header
        .Select((name, index) => new { Name = name.Trim(), Index = index })
        .ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);

    if (!headerMap.TryGetValue("SalesPriceValue", out var salesIndex))
        return null;

    headerMap.TryGetValue("DocumentType", out var documentTypeIndex);
    headerMap.TryGetValue("InvoiceSeries", out var invoiceSeriesIndex);

    var rows = 0;
    var total = 0m;
    var byDocumentType = new Dictionary<string, (int Rows, decimal Sales)>(StringComparer.OrdinalIgnoreCase);
    var bySeries = new Dictionary<string, (int Rows, decimal Sales)>(StringComparer.OrdinalIgnoreCase);

    while (!parser.EndOfData)
    {
        var fields = parser.ReadFields();
        if (fields is null || fields.All(string.IsNullOrWhiteSpace))
            continue;

        var sales = salesIndex < fields.Length ? ParseProbeDecimal(fields[salesIndex]) : 0m;
        var documentType = documentTypeIndex < fields.Length && !string.IsNullOrWhiteSpace(fields[documentTypeIndex])
            ? fields[documentTypeIndex]
            : "-";
        var series = invoiceSeriesIndex < fields.Length && !string.IsNullOrWhiteSpace(fields[invoiceSeriesIndex])
            ? fields[invoiceSeriesIndex]
            : "-";

        rows++;
        total += sales;
        AddGroupValue(byDocumentType, documentType, sales);
        AddGroupValue(bySeries, series, sales);
    }

    const decimal reference = 3102333.61m;
    return new SpainSalesCsvProbe
    {
        Path = path,
        Rows = rows,
        SalesPriceValue = total,
        ReferenceValue = reference,
        Difference = total - reference,
        ByDocumentType = byDocumentType
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new SpainSalesCsvGroup(x.Key, x.Value.Rows, x.Value.Sales))
            .ToList(),
        BySeries = bySeries
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new SpainSalesCsvGroup(x.Key, x.Value.Rows, x.Value.Sales))
            .ToList()
    };
}

static void AddGroupValue(Dictionary<string, (int Rows, decimal Sales)> groups, string key, decimal sales)
{
    groups.TryGetValue(key, out var current);
    groups[key] = (current.Rows + 1, current.Sales + sales);
}

static decimal ParseProbeDecimal(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        return 0m;

    return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
        ? value
        : 0m;
}

static string BuildPage(
    IReadOnlyList<NetSalesReferenceRow> rows,
    string databasePath,
    IReadOnlyDictionary<string, CheckedExcelReference> excelReferences,
    SpainSalesCsvProbe? spainCsv,
    GermanyExcelProbe? germanySample)
{
    var generatedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo("de-CH"));
    var okCount = rows.Count(r => r.Status == "OK");
    var checkCount = rows.Count(r => r.Status == "Pruefen");
    var missingCount = rows.Count(r => r.Status == "Keine Daten");
    var excelCount = excelReferences.Count;
    var executiveBriefing = BuildExecutiveBriefing(rows, excelReferences, spainCsv, germanySample);
    var detailRows = BuildDetailRows(rows, excelReferences, spainCsv);
    var spainCsvSection = BuildSpainCsvSection(spainCsv);
    var germanySampleSection = BuildGermanySampleSection(germanySample, excelReferences);

    return $$"""
<!doctype html>
<html lang="de">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Finance Probe</title>
  <style>
    :root {
      color-scheme: light;
      --bg: #f6f7f9;
      --panel: #ffffff;
      --text: #17202a;
      --muted: #667085;
      --line: #d8dee8;
      --ok: #147a3d;
      --check: #a15c00;
      --missing: #667085;
      --head: #22324a;
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font-family: "Segoe UI", Arial, sans-serif;
      font-size: 14px;
    }
    header {
      padding: 18px 24px 12px;
      background: var(--panel);
      border-bottom: 1px solid var(--line);
    }
    h1 {
      margin: 0 0 8px;
      font-size: 22px;
      font-weight: 650;
      letter-spacing: 0;
    }
    .meta {
      color: var(--muted);
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      line-height: 1.5;
    }
    nav {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-top: 12px;
    }
    nav a {
      color: #1f4f7a;
      text-decoration: none;
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 6px 10px;
      background: #f8fafc;
      font-weight: 600;
    }
    main { padding: 18px 24px 28px; }
    .summary {
      display: grid;
      grid-template-columns: repeat(4, minmax(140px, 1fr));
      gap: 10px;
      margin-bottom: 14px;
    }
    .metric {
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 10px 12px;
    }
    .metric strong {
      display: block;
      font-size: 20px;
      margin-bottom: 2px;
    }
    .metric span { color: var(--muted); }
    table {
      width: 100%;
      border-collapse: collapse;
      background: var(--panel);
      border: 1px solid var(--line);
    }
    th {
      text-align: left;
      background: var(--head);
      color: #fff;
      font-weight: 600;
      padding: 8px 10px;
      position: sticky;
      top: 0;
      z-index: 1;
    }
    td {
      padding: 7px 10px;
      border-top: 1px solid var(--line);
      vertical-align: top;
    }
    tbody tr:nth-child(even) { background: #fafbfc; }
    .num {
      text-align: right;
      font-variant-numeric: tabular-nums;
      white-space: nowrap;
    }
    .status {
      display: inline-block;
      min-width: 78px;
      padding: 3px 8px;
      border-radius: 999px;
      color: #fff;
      text-align: center;
      font-size: 12px;
      font-weight: 650;
    }
    .OK { background: var(--ok); }
    .Pruefen { background: var(--check); }
    .KeineDaten { background: var(--missing); }
    details { min-width: 360px; }
    summary { cursor: pointer; color: #234d7d; }
    .candidate-table {
      margin-top: 8px;
      border: 1px solid var(--line);
      font-size: 13px;
    }
    .candidate-table th {
      background: #eef2f7;
      color: var(--text);
      position: static;
    }
    .small { color: var(--muted); font-size: 12px; }
    .briefing {
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 12px;
      margin-bottom: 14px;
    }
    .briefing h2 {
      margin: 0 0 6px;
      font-size: 18px;
      letter-spacing: 0;
    }
    .briefing-note {
      color: var(--muted);
      margin: 0 0 10px;
      line-height: 1.45;
    }
    .ampel {
      display: inline-flex;
      align-items: center;
      gap: 7px;
      white-space: nowrap;
      font-weight: 650;
    }
    .ampel::before {
      content: "";
      width: 12px;
      height: 12px;
      border-radius: 999px;
      display: inline-block;
      box-shadow: 0 0 0 2px rgba(0, 0, 0, 0.06);
    }
    .ampel-ok::before { background: #168a48; }
    .ampel-check::before { background: #e6a100; }
    .ampel-missing::before { background: #9aa4b2; }
    .wrap {
      min-width: 240px;
      max-width: 420px;
      line-height: 1.35;
    }
    @media (max-width: 900px) {
      main, header { padding-left: 12px; padding-right: 12px; }
      .summary { grid-template-columns: repeat(2, minmax(120px, 1fr)); }
      .table-wrap { overflow-x: auto; }
    }
  </style>
</head>
<body>
  <header>
    <h1>Finance Probe - Net Sales Actuals 2025</h1>
    <div class="meta">
      <span>Vergleich gegen gepruefte Sollwerte aus check.xlsx Stand 29.04.2026</span>
      <span>DB: {{Html(databasePath)}}</span>
      <span>Excel-Referenzen gelesen: {{excelCount}}</span>
      <span>Aktualisiert: {{Html(generatedAt)}}</span>
    </div>
    <nav aria-label="Finance Probe Navigation">
      <a href="#briefing">Meeting Ampel</a>
      <a href="#all-sites">Detail alle Laender</a>
      <a href="#germany-sample">Germany Excel</a>
      <a href="#spain-csv">Spain CSV</a>
    </nav>
  </header>
  <main>
    {{executiveBriefing}}
    <section class="summary">
      <div class="metric"><strong>{{rows.Count}}</strong><span>Standorte</span></div>
      <div class="metric"><strong>{{okCount}}</strong><span>OK</span></div>
      <div class="metric"><strong>{{checkCount}}</strong><span>Pruefen</span></div>
      <div class="metric"><strong>{{missingCount}}</strong><span>Keine Daten</span></div>
    </section>
    <div id="all-sites" class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Status</th>
            <th>Firma</th>
            <th>Gewaehlte Abgrenzung</th>
            <th>Ist-Waehrung</th>
            <th class="num">Ist 2025</th>
            <th>Referenz-Waehrung</th>
            <th class="num">Referenz</th>
            <th class="num">Excel LC</th>
            <th class="num">Excel CHF</th>
            <th class="num">Excel Sollwert</th>
            <th>Excel Status</th>
            <th class="num">Differenz</th>
            <th class="num">Ohne 2nd-party Diff.</th>
            <th>Waehrung</th>
            <th class="num">Zeilen</th>
            <th>Varianten</th>
          </tr>
        </thead>
        <tbody>
          {{detailRows}}
        </tbody>
      </table>
    </div>
    {{germanySampleSection}}
    {{spainCsvSection}}
  </main>
</body>
</html>
""";
}

static string BuildDetailRows(
    IReadOnlyList<NetSalesReferenceRow> rows,
    IReadOnlyDictionary<string, CheckedExcelReference> excelReferences,
    SpainSalesCsvProbe? spainCsv)
{
    var detailRows = rows
        .Where(row => spainCsv is null || !row.Key.Equals("ES", StringComparison.OrdinalIgnoreCase))
        .Select(row => (Label: row.Label, Html: BuildRow(row, excelReferences)))
        .ToList();

    if (spainCsv is not null)
    {
        excelReferences.TryGetValue("Trafag ES", out var excelReference);
        detailRows.Add(("Trafag ES", BuildSpainDetailRow(spainCsv, excelReference)));
    }

    return string.Join(
        Environment.NewLine,
        detailRows
            .OrderBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .Select(row => row.Html));
}

static string BuildExecutiveBriefing(
    IReadOnlyList<NetSalesReferenceRow> rows,
    IReadOnlyDictionary<string, CheckedExcelReference> excelReferences,
    SpainSalesCsvProbe? spainCsv,
    GermanyExcelProbe? germanySample)
{
    var briefingRows = rows
        .Where(row => spainCsv is null || !row.Key.Equals("ES", StringComparison.OrdinalIgnoreCase))
        .Select(row => (Label: row.Label, Html: BuildExecutiveRow(row, germanySample)))
        .ToList();

    if (spainCsv is not null)
        briefingRows.Add(("Trafag ES", BuildSpainExecutiveRow(spainCsv)));

    var existingLabels = briefingRows
        .Select(row => row.Label)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var reference in excelReferences.Values)
    {
        if (existingLabels.Contains(reference.Label))
            continue;

        briefingRows.Add((reference.Label, BuildMissingExecutiveRow(reference)));
    }

    var tableRows = string.Join(
        Environment.NewLine,
        briefingRows
            .OrderBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .Select(row => row.Html));

    return $$"""
    <section id="briefing" class="briefing">
      <h2>Meeting Ampel 2025</h2>
      <p class="briefing-note">Gruen = Zahl passt rechnerisch. Gelb = Differenz oder fachliche Abgrenzung offen. Grau = keine belastbaren Importdaten. Fachliche Regel: Net Sales Actuals werden in Hauswaehrung aus dem Nettofakturawert abgegrenzt; CHF-Ausweis nutzt Budgetkurse 2025 und wird pro Belegposition gerechnet, sobald die Positionswerte in Hauswaehrung verfuegbar sind.</p>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Ampel</th>
              <th>Land</th>
              <th class="num">Ist</th>
              <th class="num">Soll</th>
              <th class="num">Differenz</th>
              <th>Passender Wert</th>
              <th>Waehrung / CHF</th>
              <th>Warum / offen</th>
            </tr>
          </thead>
          <tbody>{{tableRows}}</tbody>
        </table>
      </div>
    </section>
""";
}

static string BuildMissingExecutiveRow(CheckedExcelReference reference)
{
    var referenceValue = reference.PowerBiValue ?? reference.LocalCurrencyValue;
    var source = reference.PowerBiValue.HasValue ? "Sollwert" : "LC";

    return $$"""
<tr>
  <td><span class="ampel ampel-missing">Grau</span></td>
  <td><strong>{{Html(reference.Label)}}</strong><div class="small">check.xlsx</div></td>
  <td class="num">-</td>
  <td class="num">{{Amount(referenceValue)}}</td>
  <td class="num">-</td>
  <td>Kein Ist-Import (check.xlsx {{Html(source)}})</td>
  <td class="wrap">Waehrung aus Quelle noch nicht belegbar. CHF nur wenn check.xlsx-Spalte CHF verwendet wird.</td>
  <td class="wrap">In check.xlsx vorhanden, aber im aktuellen Import/aktiven Standort nicht belastbar. Export oder Standortaktivierung pruefen.</td>
</tr>
""";
}

static string BuildExecutiveRow(NetSalesReferenceRow row, GermanyExcelProbe? germanySample)
{
    var ampelClass = row.Status switch
    {
        "OK" => "ampel-ok",
        "Pruefen" => "ampel-check",
        _ => "ampel-missing"
    };
    var ampelText = row.Status switch
    {
        "OK" => "Gruen",
        "Pruefen" => "Gelb",
        _ => "Grau"
    };
    var matchingValue = string.IsNullOrWhiteSpace(row.ValueField)
        ? "Noch kein Wert gewaehlt"
        : $"{row.ValueField} ({row.ReferenceSource})";

    return $$"""
<tr>
  <td><span class="ampel {{ampelClass}}">{{ampelText}}</span></td>
  <td><strong>{{Html(row.Label)}}</strong><div class="small">{{Html(row.Key)}}</div></td>
  <td class="num">{{Amount(row.ActualValue)}}</td>
  <td class="num">{{Amount(row.ReferenceValue)}}</td>
  <td class="num">{{Amount(row.Difference)}}</td>
  <td>{{Html(matchingValue)}}</td>
  <td class="wrap">{{Html(BuildCurrencyNote(row))}}</td>
  <td class="wrap">{{Html(BuildExecutiveReason(row, germanySample))}}</td>
</tr>
""";
}

static string BuildSpainExecutiveRow(SpainSalesCsvProbe spainCsv)
{
    var ampelClass = Math.Abs(spainCsv.Difference) <= 1m ? "ampel-ok" : "ampel-check";
    var ampelText = Math.Abs(spainCsv.Difference) <= 1m ? "Gruen" : "Gelb";

    return $$"""
<tr>
  <td><span class="ampel {{ampelClass}}">{{ampelText}}</span></td>
  <td><strong>Trafag ES</strong><div class="small">ES / Sage Spain v2</div></td>
  <td class="num">{{Amount(spainCsv.SalesPriceValue)}}</td>
  <td class="num">{{Amount(spainCsv.ReferenceValue)}}</td>
  <td class="num">{{Amount(spainCsv.Difference)}}</td>
  <td>SalesPriceValue aus Spain_Sales_2025.csv</td>
  <td class="wrap">EUR Hauswaehrung. CHF ueber Budgetkurs 2025.</td>
  <td class="wrap">Export technisch lesbar, aber noch Differenz. Klaeren: Datumsabgrenzung, Serien REG/LAT/PRO/REC und Gutschriften.</td>
</tr>
""";
}

static string BuildCurrencyNote(NetSalesReferenceRow row)
{
    var actualCurrency = row.ActualCurrency.Trim();
    var currencies = row.Currencies.Trim();

    if (string.IsNullOrWhiteSpace(actualCurrency) && string.IsNullOrWhiteSpace(currencies))
        return "Waehrung noch nicht belegt.";

    if (actualCurrency.Contains("CHF", StringComparison.OrdinalIgnoreCase) &&
        !actualCurrency.Contains(',', StringComparison.Ordinal))
    {
        return "CHF direkt aus Quelle.";
    }

    if (actualCurrency.Contains(',', StringComparison.Ordinal) || currencies.Contains(',', StringComparison.Ordinal))
        return $"Gemischte Quellwaehrungen ({PreferNonBlank(actualCurrency, currencies)}). Fachlich ist Hauswaehrung fuehrend; Mapping/Quelle pruefen.";

    return $"{PreferNonBlank(actualCurrency, currencies)} Hauswaehrung. CHF ueber Budgetkurs 2025.";
}

static string BuildExecutiveReason(NetSalesReferenceRow row, GermanyExcelProbe? germanySample)
{
    if (row.Key.Equals("DE", StringComparison.OrdinalIgnoreCase) && germanySample is not null)
    {
        return $"DE-Beispielfile gefunden und lesbar: {germanySample.RowsWithAmount} Betragszeilen, Summe {Amount(germanySample.SalesPriceValue)} {germanySample.Currencies}. Das ist ein Sample, kein finaler Jahresexport.";
    }

    if (row.Status == "OK")
        return "Passt rechnerisch gegen check.xlsx. Hauswaehrung ist fachlich fuehrend.";

    if (row.Status == "Keine Daten")
        return "Keine belastbaren Daten im Import. Standort/Export/Mapping pruefen.";

    if (row.DifferenceExcludingIntercompany.HasValue &&
        Math.Abs(row.DifferenceExcludingIntercompany.Value) <= 1m)
    {
        return "Differenz ist nach 2nd-party/Intercompany-Abzug rechnerisch erklaerbar. IC-Kunden sollen spaeter als eigenes Feld gepflegt werden.";
    }

    if (row.Candidates.Count > 1)
        return "Mehrere technische Summen sichtbar. Gewaehlter Wert folgt der Fachregel: Hauswaehrung / Nettofakturawert.";

    return "Differenz offen. Quelle, Periodenabgrenzung, Gutschriften und 2nd-party/3rd-party-Abgrenzung pruefen.";
}

static string PreferNonBlank(string first, string second)
    => !string.IsNullOrWhiteSpace(first) ? first : second;

static string BuildGermanySampleSection(
    GermanyExcelProbe? germanySample,
    IReadOnlyDictionary<string, CheckedExcelReference> excelReferences)
{
    if (germanySample is null)
    {
        return """
    <section id="germany-sample" class="metric" style="margin-top:14px;">
      <strong>Germany Excel</strong>
      <span>Keine DE_Beispiel_Export_Daten.xlsx im Repo gefunden.</span>
    </section>
""";
    }

    excelReferences.TryGetValue("Trafag DE", out var reference);
    var referenceValue = reference?.PowerBiValue ?? reference?.LocalCurrencyValue;
    var difference = referenceValue.HasValue ? germanySample.SalesPriceValue - referenceValue.Value : (decimal?)null;

    return $$"""
    <section id="germany-sample" style="margin-top:18px;">
      <h2 style="font-size:18px;margin:0 0 8px;">Germany Excel sample check</h2>
      <div class="summary">
        <div class="metric"><strong>{{germanySample.RowsWithAmount}}</strong><span>Betragszeilen</span></div>
        <div class="metric"><strong>{{Amount(germanySample.SalesPriceValue)}}</strong><span>NettoPreisGesamtX {{Html(germanySample.Currencies)}}</span></div>
        <div class="metric"><strong>{{Amount(referenceValue)}}</strong><span>check.xlsx DE Referenz</span></div>
        <div class="metric"><strong>{{Amount(difference)}}</strong><span>Differenz nur Sample</span></div>
      </div>
      <div class="small">Datei: {{Html(germanySample.Path)}}</div>
      <div class="small">Interpretation: Mapping funktioniert technisch. Diese Datei heisst Beispielfile und enthaelt nur {{germanySample.RowsWithAmount}} Betragszeilen; sie darf deshalb nicht als finale Deutschland-Jahreszahl verwendet werden.</div>
    </section>
""";
}

static string BuildSpainCsvSection(SpainSalesCsvProbe? spainCsv)
{
    if (spainCsv is null)
    {
        return """
    <section id="spain-csv" class="metric" style="margin-top:14px;">
      <strong>Spain CSV</strong>
      <span>Keine Spain_Sales_2025.csv im Repo gefunden.</span>
    </section>
""";
    }

    var documentRows = string.Join(Environment.NewLine, spainCsv.ByDocumentType.Select(group => $$"""
          <tr><td>{{Html(group.Label)}}</td><td class="num">{{group.Rows}}</td><td class="num">{{Amount(group.Sales)}}</td></tr>
"""));
    var seriesRows = string.Join(Environment.NewLine, spainCsv.BySeries.Select(group => $$"""
          <tr><td>{{Html(group.Label)}}</td><td class="num">{{group.Rows}}</td><td class="num">{{Amount(group.Sales)}}</td></tr>
"""));

    return $$"""
    <section id="spain-csv" style="margin-top:18px;">
      <h2 style="font-size:18px;margin:0 0 8px;">Spain CSV direct check</h2>
      <div class="summary">
        <div class="metric"><strong>{{spainCsv.Rows}}</strong><span>CSV-Zeilen</span></div>
        <div class="metric"><strong>{{Amount(spainCsv.SalesPriceValue)}}</strong><span>SalesPriceValue EUR</span></div>
        <div class="metric"><strong>{{Amount(spainCsv.ReferenceValue)}}</strong><span>check.xlsx ES</span></div>
        <div class="metric"><strong>{{Amount(spainCsv.Difference)}}</strong><span>Differenz</span></div>
      </div>
      <div class="small">Datei: {{Html(spainCsv.Path)}}</div>
      <div class="table-wrap" style="margin-top:10px;">
        <table>
          <thead><tr><th>DocumentType</th><th class="num">Zeilen</th><th class="num">Sales</th></tr></thead>
          <tbody>{{documentRows}}</tbody>
        </table>
      </div>
      <div class="table-wrap" style="margin-top:10px;">
        <table>
          <thead><tr><th>InvoiceSeries</th><th class="num">Zeilen</th><th class="num">Sales</th></tr></thead>
          <tbody>{{seriesRows}}</tbody>
        </table>
      </div>
    </section>
""";
}

static string BuildRow(NetSalesReferenceRow row, IReadOnlyDictionary<string, CheckedExcelReference> excelReferences)
{
    var statusClass = row.Status.Replace(" ", string.Empty);
    excelReferences.TryGetValue(row.Label, out var excelReference);

    return $$"""
<tr>
  <td><span class="status {{Html(statusClass)}}">{{Html(row.Status)}}</span></td>
  <td><strong>{{Html(row.Label)}}</strong><div class="small">{{Html(row.Key)}} / {{Html(row.ReferenceSource)}}</div></td>
  <td>{{Html(row.ValueField)}}</td>
  <td>{{Html(row.ActualCurrency)}}</td>
  <td class="num">{{Amount(row.ActualValue)}}</td>
  <td>{{Html(row.ReferenceCurrency)}}</td>
  <td class="num">{{Amount(row.ReferenceValue)}}</td>
  <td class="num">{{Amount(excelReference?.LocalCurrencyValue)}}</td>
  <td class="num">{{Amount(excelReference?.ChfValue)}}</td>
  <td class="num">{{Amount(excelReference?.PowerBiValue)}}</td>
  <td>{{Html(excelReference?.Status)}}</td>
  <td class="num">{{Amount(row.Difference)}}</td>
  <td class="num">{{Amount(row.DifferenceExcludingIntercompany)}}</td>
  <td>{{Html(row.Currencies)}}</td>
  <td class="num">{{row.RowCount}}</td>
  <td>{{BuildCandidateDetails(row)}}</td>
</tr>
""";
}

static string BuildSpainDetailRow(SpainSalesCsvProbe spainCsv, CheckedExcelReference? excelReference)
{
    var status = Math.Abs(spainCsv.Difference) <= 1m ? "OK" : "Pruefen";

    return $$"""
<tr>
  <td><span class="status {{status}}">{{status}}</span></td>
  <td><strong>Trafag ES</strong><div class="small">ES / Sage Spain v2 CSV</div></td>
  <td>SalesPriceValue CSV</td>
  <td>EUR</td>
  <td class="num">{{Amount(spainCsv.SalesPriceValue)}}</td>
  <td>LC</td>
  <td class="num">{{Amount(spainCsv.ReferenceValue)}}</td>
  <td class="num">{{Amount(excelReference?.LocalCurrencyValue)}}</td>
  <td class="num">{{Amount(excelReference?.ChfValue)}}</td>
  <td class="num">{{Amount(excelReference?.PowerBiValue)}}</td>
  <td>{{Html(excelReference?.Status)}}</td>
  <td class="num">{{Amount(spainCsv.Difference)}}</td>
  <td class="num">-</td>
  <td>EUR</td>
  <td class="num">{{spainCsv.Rows}}</td>
  <td><a href="#spain-csv">CSV-Details anzeigen</a></td>
</tr>
""";
}

static string BuildCandidateDetails(NetSalesReferenceRow row)
{
    if (row.Candidates.Count == 0)
        return "<span class=\"small\">Keine Varianten</span>";

    var candidateRows = string.Join(Environment.NewLine, row.Candidates.Select(candidate => $$"""
<tr>
  <td>{{Html(candidate.Label)}}</td>
  <td>{{Html(candidate.Currency)}}</td>
  <td class="num">{{Amount(candidate.Value)}}</td>
  <td class="num">{{Amount(candidate.Difference)}}</td>
  <td class="num">{{Amount(candidate.IntercompanyValue)}}</td>
  <td class="num">{{Amount(candidate.DifferenceExcludingIntercompany)}}</td>
</tr>
"""));

    return $$"""
<details>
  <summary>{{row.Candidates.Count}} Varianten anzeigen</summary>
  <table class="candidate-table">
    <thead>
      <tr>
        <th>Abgrenzung</th>
        <th>Waehrung</th>
        <th class="num">Wert</th>
        <th class="num">Diff.</th>
        <th class="num">2nd-party/IC</th>
        <th class="num">Diff. ohne 2nd-party</th>
      </tr>
    </thead>
    <tbody>{{candidateRows}}</tbody>
  </table>
</details>
""";
}

static string Amount(decimal? value)
    => value.HasValue ? value.Value.ToString("#,##0.00", CultureInfo.GetCultureInfo("de-CH")) : "-";

static string Html(string? value)
    => WebUtility.HtmlEncode(value ?? string.Empty);

sealed class CheckedExcelReference
{
    public string Label { get; set; } = string.Empty;
    public decimal? LocalCurrencyValue { get; set; }
    public decimal? ChfValue { get; set; }
    public decimal? PowerBiValue { get; set; }
    public string Status { get; set; } = string.Empty;
}

sealed class SpainSalesCsvProbe
{
    public string Path { get; set; } = string.Empty;
    public int Rows { get; set; }
    public decimal SalesPriceValue { get; set; }
    public decimal ReferenceValue { get; set; }
    public decimal Difference { get; set; }
    public List<SpainSalesCsvGroup> ByDocumentType { get; set; } = [];
    public List<SpainSalesCsvGroup> BySeries { get; set; } = [];
}

sealed record SpainSalesCsvGroup(string Label, int Rows, decimal Sales);

sealed class GermanyExcelProbe
{
    public string Path { get; set; } = string.Empty;
    public int RowsWithAmount { get; set; }
    public decimal SalesPriceValue { get; set; }
    public int RowsIn2025 { get; set; }
    public decimal SalesPriceValueIn2025 { get; set; }
    public string Currencies { get; set; } = string.Empty;
}
