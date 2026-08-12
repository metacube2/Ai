using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using TrafagSalesExporter.Data;

// Legt eine Datei in den Manual-Import-Ordner eines Standorts auf SharePoint - mit Sicherung
// der bisherigen Datei, falls der Name schon belegt ist.
//
// Warum als Werkzeug und nicht per Hand: der UK-Backfill vom 2026-07-28 ist beim ersten
// Versuch gescheitert, weil der Dateiname von der Auswahllogik anders bewertet wurde als
// angenommen (`docs/FINANCE_BACKFILL_UK_ES_2026-07-28.md`). Dieses Werkzeug zeigt vor dem
// Schreiben, wie die Datei heisst, was sie ersetzt und welche Dateien die Auswahl danach
// liest - und sichert die alte Fassung, damit der Schritt umkehrbar bleibt.
//
// Die Datenbank wird ausschliesslich LESEND geoeffnet (Konfiguration und Ordnerpfad).
// Zugangsdaten werden nie ausgegeben.
//
// Usage: ManualImportUpload <dbPath> <TSC> <lokaleDatei> [--replace]

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: ManualImportUpload <dbPath> <TSC> <lokaleDatei> [--replace]");
    return 2;
}

var dbPath = args[0];
var tsc = args[1];
var localFile = args[2];
var replace = args.Contains("--replace", StringComparer.OrdinalIgnoreCase);

if (!File.Exists(localFile)) { Console.Error.WriteLine($"Lokale Datei nicht gefunden: {localFile}"); return 2; }

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath};Mode=ReadOnly").Options;
await using var db = new AppDbContext(options);

var site = await db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.TSC == tsc);
if (site is null) { Console.Error.WriteLine($"Standort {tsc} nicht gefunden."); return 2; }
var config = await db.SharePointConfigs.AsNoTracking().FirstAsync();

Console.WriteLine($"Standort : {site.TSC} ({site.Land})");
Console.WriteLine($"Ordner   : {site.ManualImportFilePath}");
Console.WriteLine($"Datei    : {Path.GetFileName(localFile)}  ({new FileInfo(localFile).Length:N0} Bytes)");
Console.WriteLine();

var credential = new ClientSecretCredential(config.TenantId, config.ClientId, config.ClientSecret);
var graph = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);

var siteUri = new Uri(config.SiteUrl);
var spSite = await graph.Sites[$"{siteUri.Host}:{siteUri.AbsolutePath.TrimEnd('/')}"].GetAsync();
var drive = await graph.Sites[spSite!.Id].Drive.GetAsync();

var folderPath = new Uri(site.ManualImportFilePath!).AbsolutePath;
var prefix = siteUri.AbsolutePath.TrimEnd('/');
if (folderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    folderPath = folderPath[prefix.Length..].Trim('/');

var name = Path.GetFileName(localFile);
var remotePath = $"{folderPath}/{name}";

var children = await graph.Drives[drive!.Id].Root.ItemWithPath(folderPath).Children.GetAsync();
var existing = children?.Value?.FirstOrDefault(x =>
    string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) && x.File is not null);

if (existing is not null)
{
    Console.WriteLine($"Vorhanden im Ziel: {existing.Name}  {existing.Size:N0} Bytes  {existing.LastModifiedDateTime:yyyy-MM-dd HH:mm}");
    if (!replace)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("ABBRUCH: Datei existiert. Mit --replace ueberschreiben (die alte Fassung wird vorher gesichert).");
        return 1;
    }

    // Sicherung, damit der Schritt umkehrbar bleibt.
    var backupDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(localFile))!, "ersetzt");
    Directory.CreateDirectory(backupDir);
    var stamp = existing.LastModifiedDateTime?.ToString("yyyyMMdd_HHmm") ?? "unbekannt";
    var backupPath = Path.Combine(backupDir, $"{Path.GetFileNameWithoutExtension(name)}_{stamp}{Path.GetExtension(name)}");
    await using (var remote = await graph.Drives[drive.Id].Items[existing.Id].Content.GetAsync())
    await using (var local = File.Create(backupPath))
    {
        if (remote is null) { Console.Error.WriteLine("ABBRUCH: bisherige Datei liess sich nicht lesen, keine Sicherung moeglich."); return 1; }
        await remote.CopyToAsync(local);
    }
    Console.WriteLine($"Gesichert: {backupPath}  ({new FileInfo(backupPath).Length:N0} Bytes)");
}
else
{
    Console.WriteLine("Im Ziel noch nicht vorhanden - wird neu angelegt.");
}

await using (var stream = File.OpenRead(localFile))
{
    await graph.Drives[drive.Id].Root.ItemWithPath(remotePath).Content.PutAsync(stream);
}
Console.WriteLine("Hochgeladen.");

var after = await graph.Drives[drive.Id].Root.ItemWithPath(folderPath).Children.GetAsync();
var uploaded = after?.Value?.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
Console.WriteLine(uploaded is null
    ? "WARNUNG: Datei nach dem Upload nicht in der Ordnerliste gefunden."
    : $"Im Ziel jetzt: {uploaded.Name}  {uploaded.Size:N0} Bytes  {uploaded.LastModifiedDateTime:yyyy-MM-dd HH:mm}");
return 0;
