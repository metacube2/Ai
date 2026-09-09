using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;
using TrafagSalesExporter.Services.DataSources;

// Zieht ausschliesslich die Spalte Name der TRDE-Verkaufszeilen nach, nachdem
// NormalizeAlphaplanText die RTF-Schrift- und Farbtabelle klammerbalanciert entfernt.
// Der neue Wert wird aus den Alphaplan-Rohdateien mit demselben Importdienst berechnet,
// der auch produktiv laeuft. Keine Zeile wird geloescht, angelegt oder umsortiert,
// kein anderes Feld wird angefasst.
//
// Aufruf: DeNameFix <db> <arbeitsordner> [invoice_lines.csv ...] [--sharepoint] [--aus-plan] [--apply]
//   --sharepoint  laedt die Rohdateien ueber denselben Adapter wie der produktive Import
//   --aus-plan    verwendet beim Anwenden den Plan des Probelaufs statt neu zu lesen

var flags = new[] { "--apply", "--sharepoint", "--aus-plan" };
var apply = args.Contains("--apply");
var fromSharePoint = args.Contains("--sharepoint");
var fromPlan = args.Contains("--aus-plan");
var positional = args.Where(a => !flags.Contains(a)).ToArray();
if (positional.Length < 2 || (positional.Length < 3 && !fromSharePoint && !fromPlan))
    throw new ArgumentException("Aufruf: DeNameFix <db> <arbeitsordner> [invoice_lines.csv ...] [--sharepoint] [--aus-plan] [--apply]");

var databasePath = Path.GetFullPath(positional[0]);
var workFolder = Path.GetFullPath(positional[1]);
Directory.CreateDirectory(workFolder);
var appliedMarker = Path.Combine(workFolder, "database-applied.txt");
if (apply && File.Exists(appliedMarker))
    throw new InvalidOperationException($"{appliedMarker} vorhanden. Kein blinder zweiter Lauf.");

var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = apply ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly
}.ToString()).Options;

await using var db = new AppDbContext(options);
var site = await db.Sites.AsNoTracking().SingleAsync(x => x.TSC == "TRDE");
var rows = await db.CentralSalesRecords.AsNoTracking()
    .Where(x => x.SiteId == site.Id).OrderBy(x => x.Id).ToListAsync();

// Rohzeilen mit dem produktiven Importdienst lesen. Spaetere Dateien gewinnen,
// genau wie beim Import mit Vollbestand und anschliessenden Deltas.
// Die zentrale Tabelle fuehrt keine SourceLineId. Schluessel ist deshalb die
// fachliche Zeilenidentitaet aus Beleg, Rechnungsnummer, Position und Artikel.
var service = new ManualExcelImportService();
var cleanNames = new Dictionary<LineKey, HashSet<string>>();

void Collect(IEnumerable<SalesRecord> records)
{
    foreach (var record in records)
    {
        var key = new LineKey(record.DocumentEntry, record.InvoiceNumber, record.PositionOnInvoice, record.Material);
        if (!cleanNames.TryGetValue(key, out var values))
        {
            values = new HashSet<string>(StringComparer.Ordinal);
            cleanNames[key] = values;
        }

        values.Add(record.Name);
    }
}

if (fromSharePoint && !fromPlan)
{
    // Genau der Weg des produktiven Imports: Vollbestand und Delta-Archive aus dem
    // konfigurierten SharePoint-Ordner, gelesen mit demselben Importdienst.
    var settings = await db.ExportSettings.AsNoTracking().FirstAsync();
    var sourceDefinition = await db.SourceSystemDefinitions.AsNoTracking()
        .OrderBy(x => x.Id).FirstAsync(x => x.Code == site.SourceSystem);
    var sharePointConfig = await db.SharePointConfigs.AsNoTracking().FirstOrDefaultAsync();
    var adapter = new ManualExcelDataSourceAdapter(new SharePointUploadService(), service, new ConsoleEventLog());
    var fetched = await adapter.FetchAsync(new DataSourceFetchContext
    {
        Site = site,
        SourceDefinition = sourceDefinition,
        Settings = settings,
        SharePointConfig = sharePointConfig,
        UpdateStatus = message => Console.WriteLine($"  {message}")
    });

    Collect(fetched.Records);
    Console.WriteLine($"SharePoint-Rohlauf: {fetched.Records.Count} Zeilen gelesen, Schluessel gesamt {cleanNames.Count}");
}

foreach (var linesPath in positional.Skip(2))
{
    var full = Path.GetFullPath(linesPath);
    var raw = await service.ReadSalesRecordsAsync(full, site);
    Collect(raw);
    Console.WriteLine($"Rohdatei {Path.GetFileName(full)}: {raw.Count} Zeilen gelesen, Schluessel gesamt {cleanNames.Count}");
}

bool HasFontResidue(string value)
    => value.Contains("MS Shell Dlg", StringComparison.OrdinalIgnoreCase)
       || value.Contains("Microsoft Sans Serif", StringComparison.OrdinalIgnoreCase);

var planned = new List<Change>();
var plannedPath = Path.Combine(workFolder, "planned-name-changes.json");
var notMatched = new List<CentralSalesRecord>();
var ambiguous = new List<CentralSalesRecord>();
var notSuffix = new List<Change>();

foreach (var row in rows)
{
    var key = new LineKey(row.DocumentEntry, row.InvoiceNumber, row.PositionOnInvoice, row.Material);
    if (!cleanNames.TryGetValue(key, out var candidates))
    {
        if (HasFontResidue(row.Name)) notMatched.Add(row);
        continue;
    }

    if (candidates.Count != 1)
    {
        // Mehrdeutige Rohzeilen werden nicht angefasst.
        if (HasFontResidue(row.Name)) ambiguous.Add(row);
        continue;
    }

    var clean = candidates.Single();
    var current = row.Name;
    if (string.Equals(current, clean, StringComparison.Ordinal)) continue;

    var change = new Change(row.Id, key.ToString(), row.InvoiceNumber, row.Material, current, clean);
    // Die entfernten Gruppen stehen am Anfang des RTF. Der bereinigte Wert muss deshalb
    // ein Endstueck des bisherigen Wertes sein; alles andere waere Textverlust.
    if (!current.EndsWith(clean, StringComparison.Ordinal)) notSuffix.Add(change);
    planned.Add(change);
}

Console.WriteLine($"TRDE-Zeilen: {rows.Count}");
Console.WriteLine($"Zeilen mit Schriftrest bisher: {rows.Count(x => HasFontResidue(x.Name ?? string.Empty))}");
Console.WriteLine($"Rohtreffer ueber Zeilenschluessel: {rows.Count(x => cleanNames.ContainsKey(new LineKey(x.DocumentEntry, x.InvoiceNumber, x.PositionOnInvoice, x.Material)))}");
Console.WriteLine($"Mehrdeutige Rohzeilen mit Schriftrest: {ambiguous.Count}");
Console.WriteLine($"Geplante Namensaenderungen: {planned.Count}");
Console.WriteLine($"Davon ohne Endstueck-Eigenschaft: {notSuffix.Count}");
Console.WriteLine($"Schriftrest ohne Rohtreffer (bleibt stehen): {notMatched.Count}");
Console.WriteLine($"Schriftrest nach Plan verbleibend: {planned.Count(x => HasFontResidue(x.NewName)) + notMatched.Count + ambiguous.Count}");

if (!fromPlan) File.WriteAllText(plannedPath,
    JsonSerializer.Serialize(planned, new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(workFolder, "unmatched-residue.json"),
    JsonSerializer.Serialize(notMatched.Concat(ambiguous).Select(x => new { x.Id, x.DocumentEntry, x.InvoiceNumber, x.PositionOnInvoice, x.Material, x.Name }),
        new JsonSerializerOptions { WriteIndented = true }));

foreach (var sample in planned.Take(3))
    Console.WriteLine($"  Beispiel {sample.InvoiceNumber} {sample.Material}: [{Cut(sample.OldName)}] -> [{Cut(sample.NewName)}]");

if (!apply)
{
    File.WriteAllText(Path.Combine(workFolder, "before-rows.json"), JsonSerializer.Serialize(rows));
    Console.WriteLine("Probelauf beendet. Nichts geaendert.");
    return;
}

if (fromPlan)
{
    planned = JsonSerializer.Deserialize<List<Change>>(File.ReadAllText(plannedPath))!;
    notSuffix = planned.Where(x => !x.OldName.EndsWith(x.NewName, StringComparison.Ordinal)).ToList();
    Console.WriteLine($"Plan aus {plannedPath} geladen: {planned.Count} Aenderungen, {notSuffix.Count} ohne Endstueck-Eigenschaft.");
}

var baselinePath = Path.Combine(workFolder, "before-rows.json");
if (!File.Exists(baselinePath))
    throw new InvalidOperationException("Vor dem Anwenden zuerst den Probelauf im selben Arbeitsordner ausfuehren.");
if (JsonSerializer.Serialize(rows) != File.ReadAllText(baselinePath))
    throw new InvalidOperationException("Die Datenbank hat sich seit dem Probelauf geaendert. Abbruch.");
if (notSuffix.Count > 0)
    throw new InvalidOperationException($"{notSuffix.Count} Aenderungen sind kein Endstueck des bisherigen Textes. Abbruch.");
if (planned.Count == 0)
{
    Console.WriteLine("Nichts zu tun.");
    return;
}

await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite }.ToString());
await connection.OpenAsync();
await using (var transaction = connection.BeginTransaction())
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = "UPDATE CentralSalesRecords SET Name = $name WHERE Id = $id AND Name = $old";
    var nameParameter = command.CreateParameter(); nameParameter.ParameterName = "$name"; command.Parameters.Add(nameParameter);
    var idParameter = command.CreateParameter(); idParameter.ParameterName = "$id"; command.Parameters.Add(idParameter);
    var oldParameter = command.CreateParameter(); oldParameter.ParameterName = "$old"; command.Parameters.Add(oldParameter);

    var updated = 0;
    foreach (var change in planned)
    {
        nameParameter.Value = change.NewName;
        idParameter.Value = change.Id;
        oldParameter.Value = change.OldName;
        updated += await command.ExecuteNonQueryAsync();
    }

    if (updated != planned.Count)
    {
        transaction.Rollback();
        throw new InvalidOperationException($"{updated} von {planned.Count} Zeilen getroffen. Zurueckgerollt.");
    }

    transaction.Commit();
    File.WriteAllText(appliedMarker, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {updated} Namen aktualisiert");
    Console.WriteLine($"{updated} Namen in einer Transaktion aktualisiert.");
}

static string Cut(string value) => value.Length <= 70 ? value : value[..70];

internal sealed class ConsoleEventLog : IAppEventLogService
{
    public Task WriteAsync(string category, string message, string level = "Info", int? siteId = null, string? land = null, string? details = null)
    {
        Console.WriteLine($"  [{category}] {message}{(string.IsNullOrWhiteSpace(details) ? string.Empty : " | " + details)}");
        return Task.CompletedTask;
    }

    public Task WriteDebugAsync(string category, string message, int? siteId = null, string? land = null, string? details = null)
        => Task.CompletedTask;
}

internal sealed record Change(int Id, string LineKey, string InvoiceNumber, string Material, string OldName, string NewName);

internal readonly record struct LineKey(int DocumentEntry, string InvoiceNumber, int PositionOnInvoice, string Material)
{
    public override string ToString() => $"{DocumentEntry}|{InvoiceNumber}|{PositionOnInvoice}|{Material}";
}
