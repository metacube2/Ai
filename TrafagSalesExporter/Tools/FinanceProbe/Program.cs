using System.Globalization;
using System.Net;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
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
    return Results.Content(BuildPage(rows, databasePath, excelReferences), "text/html; charset=utf-8");
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

static string BuildPage(
    IReadOnlyList<NetSalesReferenceRow> rows,
    string databasePath,
    IReadOnlyDictionary<string, CheckedExcelReference> excelReferences)
{
    var generatedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo("de-CH"));
    var okCount = rows.Count(r => r.Status == "OK");
    var checkCount = rows.Count(r => r.Status == "Pruefen");
    var missingCount = rows.Count(r => r.Status == "Keine Daten");
    var excelCount = excelReferences.Count;

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
      <span>Vergleich gegen geprüfte Referenzwerte aus check.xlsx / Power BI Stand 29.04.2026</span>
      <span>DB: {{Html(databasePath)}}</span>
      <span>Excel-Referenzen gelesen: {{excelCount}}</span>
      <span>Aktualisiert: {{Html(generatedAt)}}</span>
    </div>
  </header>
  <main>
    <section class="summary">
      <div class="metric"><strong>{{rows.Count}}</strong><span>Standorte</span></div>
      <div class="metric"><strong>{{okCount}}</strong><span>OK</span></div>
      <div class="metric"><strong>{{checkCount}}</strong><span>Pruefen</span></div>
      <div class="metric"><strong>{{missingCount}}</strong><span>Keine Daten</span></div>
    </section>
    <div class="table-wrap">
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
            <th class="num">Excel Power BI</th>
            <th>Excel Status</th>
            <th class="num">Differenz</th>
            <th class="num">Ohne IC Diff.</th>
            <th>Waehrung</th>
            <th class="num">Zeilen</th>
            <th>Varianten</th>
          </tr>
        </thead>
        <tbody>
          {{string.Join(Environment.NewLine, rows.Select(row => BuildRow(row, excelReferences)))}}
        </tbody>
      </table>
    </div>
  </main>
</body>
</html>
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
        <th class="num">IC</th>
        <th class="num">Diff. ohne IC</th>
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
