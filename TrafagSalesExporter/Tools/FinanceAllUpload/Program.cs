using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using System.IO.Compression;
using System.Security.Cryptography;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

// Laedt Finance_All in genau denselben zentralen SharePoint-Ordner wie Sales_All.
// Die produktive Konfiguration wird nur lesend geoeffnet; Zugangsdaten werden nie ausgegeben.
// Eine vorhandene gleichnamige Datei wird nur mit --replace ersetzt und vorher lokal gesichert.
// Nach dem Upload wird die SharePoint-Datei erneut heruntergeladen und per SHA-256 verglichen.
//
// Usage: FinanceAllUpload <dbPath> <Finance_All.xlsx> [--replace|--verify-only]

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: FinanceAllUpload <dbPath> <Finance_All.xlsx> [--replace]");
    return 2;
}

var dbPath = Path.GetFullPath(args[0]);
var localFile = Path.GetFullPath(args[1]);
var replace = args.Contains("--replace", StringComparer.OrdinalIgnoreCase);
var verifyOnly = args.Contains("--verify-only", StringComparer.OrdinalIgnoreCase);
var fileName = Path.GetFileName(localFile);

if (!File.Exists(dbPath)) { Console.Error.WriteLine($"Datenbank nicht gefunden: {dbPath}"); return 2; }
if (!File.Exists(localFile)) { Console.Error.WriteLine($"Datei nicht gefunden: {localFile}"); return 2; }
if (!fileName.StartsWith("Finance_All_", StringComparison.OrdinalIgnoreCase) ||
    !Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Nur Dateien Finance_All_*.xlsx werden hochgeladen.");
    return 2;
}

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath};Mode=ReadOnly").Options;
await using var db = new AppDbContext(options);
var config = await db.SharePointConfigs.AsNoTracking().FirstOrDefaultAsync();
if (config is null || !IsComplete(config))
{
    Console.Error.WriteLine("Die SharePoint-Konfiguration ist unvollstaendig.");
    return 2;
}

var (folder, subfolder) = ResolveCentralTarget(config);
var remoteFolder = string.Join("/", new[] { folder.Trim('/').Trim(), subfolder.Trim('/').Trim() }
    .Where(part => !string.IsNullOrWhiteSpace(part)));
var remotePath = $"{remoteFolder}/{fileName}";
var localInfo = new FileInfo(localFile);

Console.WriteLine($"Datei : {fileName} ({localInfo.Length:N0} Bytes)");
Console.WriteLine($"Ziel  : {remoteFolder}");

var credential = new ClientSecretCredential(config.TenantId.Trim(), config.ClientId.Trim(), config.ClientSecret.Trim());
var graph = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
var siteUri = new Uri(config.SiteUrl.Trim());
var site = await graph.Sites[$"{siteUri.Host}:{siteUri.AbsolutePath.TrimEnd('/')}"] .GetAsync();
if (site?.Id is null) throw new InvalidOperationException("SharePoint Site konnte nicht gefunden werden.");
var drive = await graph.Sites[site.Id].Drive.GetAsync();
if (drive?.Id is null) throw new InvalidOperationException("SharePoint Dokumentenbibliothek konnte nicht gefunden werden.");

Microsoft.Graph.Models.DriveItem? existing = null;
try
{
    existing = await graph.Drives[drive.Id].Root.ItemWithPath(remotePath).GetAsync();
}
catch (ODataError ex) when (ex.ResponseStatusCode == 404)
{
    // Neue Datei.
}

if (existing is not null && !verifyOnly)
{
    Console.WriteLine($"Vorhanden: {existing.Name} ({existing.Size:N0} Bytes, {existing.LastModifiedDateTime:yyyy-MM-dd HH:mm})");
    if (!replace)
    {
        Console.Error.WriteLine("ABBRUCH: Zieldatei existiert. Mit --replace sichern und ersetzen.");
        return 1;
    }

    var backupDirectory = Path.Combine(Path.GetDirectoryName(localFile)!, "ersetzt");
    Directory.CreateDirectory(backupDirectory);
    var stamp = existing.LastModifiedDateTime?.ToString("yyyyMMdd_HHmmss") ?? "unbekannt";
    var backupPath = Path.Combine(backupDirectory,
        $"{Path.GetFileNameWithoutExtension(fileName)}_SharePoint_{stamp}{Path.GetExtension(fileName)}");
    await using var previous = await graph.Drives[drive.Id].Root.ItemWithPath(remotePath).Content.GetAsync()
        ?? throw new InvalidOperationException("Vorhandene SharePoint-Datei konnte nicht gesichert werden.");
    await using var backup = File.Create(backupPath);
    await previous.CopyToAsync(backup);
    Console.WriteLine($"Gesichert: {backupPath} ({new FileInfo(backupPath).Length:N0} Bytes)");
}
else if (existing is null && !verifyOnly)
{
    Console.WriteLine("Im Ziel noch nicht vorhanden; wird neu angelegt.");
}

if (verifyOnly && existing is null)
    throw new InvalidOperationException("Die Zieldatei existiert nicht und kann nicht geprueft werden.");

if (!verifyOnly)
{
    var uploader = new SharePointUploadService();
    await uploader.UploadAsync(config.TenantId, config.ClientId, config.ClientSecret, config.SiteUrl,
        folder, subfolder, localFile);
}

var uploaded = await graph.Drives[drive.Id].Root.ItemWithPath(remotePath).GetAsync()
    ?? throw new InvalidOperationException("Hochgeladene Datei wurde im Ziel nicht gefunden.");

var verificationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_{fileName}");
try
{
    await using (var remote = await graph.Drives[drive.Id].Root.ItemWithPath(remotePath).Content.GetAsync()
        ?? throw new InvalidOperationException("Hochgeladene Datei konnte nicht zur Pruefung gelesen werden."))
    await using (var verification = File.Create(verificationPath))
        await remote.CopyToAsync(verification);

    var localHash = await Sha256Async(localFile);
    var remoteHash = await Sha256Async(verificationPath);
    var verificationText = "gesamte Datei SHA-256-identisch";
    if (!string.Equals(localHash, remoteHash, StringComparison.OrdinalIgnoreCase))
    {
        if (!await WorkbookPayloadMatchesAsync(localFile, verificationPath))
        {
            var diagnosticPath = Path.Combine(Path.GetDirectoryName(localFile)!,
                $"{Path.GetFileNameWithoutExtension(fileName)}_SharePointPruefung{Path.GetExtension(fileName)}");
            File.Copy(verificationPath, diagnosticPath, overwrite: true);
            throw new InvalidOperationException(
                $"Excel-Nutzdaten weichen nach dem Upload ab. Heruntergeladene Datei: {diagnosticPath}");
        }

        Console.WriteLine(
            $"SharePoint hat Office-Metadaten ergaenzt (lokal {localInfo.Length:N0}, SharePoint {uploaded.Size:N0} Bytes); " +
            "alle Excel-Nutzdaten unter xl/ sind bytegleich.");
        verificationText = "Excel-Nutzdaten bytegleich; nur SharePoint-Metadaten ergaenzt";
    }

    Console.WriteLine($"Hochgeladen und bestaetigt ({verificationText}): {uploaded.Name} ({uploaded.Size:N0} Bytes, {uploaded.LastModifiedDateTime:yyyy-MM-dd HH:mm})");
}
finally
{
    if (File.Exists(verificationPath)) File.Delete(verificationPath);
}

return 0;

static bool IsComplete(SharePointConfig config)
    => !string.IsNullOrWhiteSpace(config.TenantId) &&
       !string.IsNullOrWhiteSpace(config.ClientId) &&
       !string.IsNullOrWhiteSpace(config.ClientSecret) &&
       !string.IsNullOrWhiteSpace(config.SiteUrl);

static (string Folder, string Subfolder) ResolveCentralTarget(SharePointConfig config)
{
    const string financeRoot = "/Import/Finance";
    var configured = !string.IsNullOrWhiteSpace(config.CentralExportFolder)
        ? config.CentralExportFolder
        : config.ExportFolder.Trim().TrimEnd('/', '\\').Equals("/Shared Documents/Exports", StringComparison.OrdinalIgnoreCase)
            ? financeRoot
            : config.ExportFolder;
    var normalized = configured.Trim().TrimEnd('/', '\\');
    return normalized.EndsWith("/Alle", StringComparison.OrdinalIgnoreCase) ||
           normalized.EndsWith("\\Alle", StringComparison.OrdinalIgnoreCase)
        ? (normalized, string.Empty)
        : (configured, "Alle");
}

static async Task<string> Sha256Async(string path)
{
    await using var stream = File.OpenRead(path);
    return Convert.ToHexString(await SHA256.HashDataAsync(stream));
}

static async Task<bool> WorkbookPayloadMatchesAsync(string localPath, string downloadedPath)
{
    using var local = ZipFile.OpenRead(localPath);
    using var downloaded = ZipFile.OpenRead(downloadedPath);
    var downloadedEntries = downloaded.Entries.ToDictionary(entry => entry.FullName, StringComparer.Ordinal);

    // SharePoint darf Paket-/Compliance-Metadaten ergaenzen. Die eigentlichen Workbook-
    // Nutzdaten muessen unveraendert bleiben. workbook.xml.rels ist ausgenommen, weil
    // SharePoint dort nur Beziehungen zu seinen customXml-Metadaten eintraegt.
    foreach (var entry in local.Entries.Where(entry =>
                 entry.FullName.StartsWith("xl/", StringComparison.Ordinal) &&
                 !entry.FullName.Equals("xl/_rels/workbook.xml.rels", StringComparison.Ordinal)))
    {
        if (!downloadedEntries.TryGetValue(entry.FullName, out var remoteEntry)) return false;
        await using var localStream = entry.Open();
        await using var remoteStream = remoteEntry.Open();
        var localHash = Convert.ToHexString(await SHA256.HashDataAsync(localStream));
        var remoteHash = Convert.ToHexString(await SHA256.HashDataAsync(remoteStream));
        if (!string.Equals(localHash, remoteHash, StringComparison.OrdinalIgnoreCase)) return false;
    }

    return true;
}
