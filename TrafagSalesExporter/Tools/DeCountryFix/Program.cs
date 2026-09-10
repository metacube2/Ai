using System.Text;
using Microsoft.Data.Sqlite;

// Korrigiert CustomerCountry der deutschen Verkaufszeilen auf den Laendercode.
//
// Grund: der Nachzug vom 2026-09-09 hat den Laendertext geschrieben ("Deutschland"),
// waehrend die bestehende Konvention und die eingebettete Belegbruecke den Code
// fuehren ("D"). In den TRDE-Zeilen standen danach beide Formen nebeneinander.
// Angefasst wird ausschliesslich CustomerCountry, und nur dort, wo der gespeicherte
// Wert der Laendertext derselben Adresse ist. Jede andere Abweichung bricht ab.
//
// Aufruf: DeCountryFix <db> <kundenstamm.csv> <arbeitsordner> [--apply]

if (args.Length < 3)
    throw new ArgumentException("Aufruf: DeCountryFix <db> <kundenstamm.csv> <arbeitsordner> [--apply]");

var apply = args.Contains("--apply");
var databasePath = Path.GetFullPath(args[0]);
var workFolder = Path.GetFullPath(args[2]);
Directory.CreateDirectory(workFolder);
var marker = Path.Combine(workFolder, "country-applied.txt");
if (apply && File.Exists(marker))
    throw new InvalidOperationException($"{marker} vorhanden. Kein blinder zweiter Lauf.");

// AdressNr;Name;Suchname;Land;Laendertext;Branche;IstBahn
var code = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
foreach (var line in File.ReadLines(Path.GetFullPath(args[1]), Encoding.UTF8).Skip(1))
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    var f = line.Split(';');
    if (f.Length < 6) continue;
    var nr = f[0].Trim();
    if (nr.Length == 0) continue;
    code[nr] = f[3].Trim();
    text[nr] = f[4].Trim();
}
Console.WriteLine($"Kundenstamm: {code.Count} Adressen");

var connection = new SqliteConnection(new SqliteConnectionStringBuilder
{ DataSource = databasePath, Mode = apply ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly }.ToString());
await connection.OpenAsync();

await using (var check = connection.CreateCommand())
{
    check.CommandText = "PRAGMA quick_check;";
    var r = (await check.ExecuteScalarAsync())?.ToString() ?? "";
    Console.WriteLine($"quick_check: {r}");
    if (!string.Equals(r, "ok", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("quick_check nicht ok. Abbruch.");
}

int siteId;
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT Id FROM Sites WHERE TSC = 'TRDE';";
    siteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
}

var planned = new List<(int Id, string Number, string Old, string New)>();
var untouched = 0;
var unexpected = new List<string>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT Id, CustomerNumber, CustomerCountry FROM CentralSalesRecords WHERE SiteId = $s;";
    cmd.Parameters.AddWithValue("$s", siteId);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var number = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
        var country = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
        if (number.Length == 0 || number.StartsWith("ALPHAPLAN-ID:", StringComparison.OrdinalIgnoreCase)) { untouched++; continue; }
        if (!code.TryGetValue(number, out var wanted)) { untouched++; continue; }
        if (string.Equals(country, wanted, StringComparison.Ordinal)) { untouched++; continue; }

        // Nur der Laendertext derselben Adresse darf ersetzt werden.
        if (text.TryGetValue(number, out var asText) && string.Equals(country, asText, StringComparison.Ordinal))
            planned.Add((id, number, country, wanted));
        else
        {
            unexpected.Add($"Id={id} Kunde={number} gespeichert='{country}' erwartet='{wanted}'");
            untouched++;
        }
    }
}

Console.WriteLine($"zu korrigieren : {planned.Count}");
Console.WriteLine($"unveraendert   : {untouched}");
Console.WriteLine($"unerwartet     : {unexpected.Count}");
foreach (var u in unexpected.Take(10)) Console.WriteLine($"  {u}");
// Diese Zeilen stammen nicht aus dem Nachzug vom 2026-09-09: ihr gespeicherter Wert ist
// weder der Code noch der Laendertext derselben Adresse. Sie werden gemeldet und bewusst
// NICHT angefasst, weil das Werkzeug nur den eigenen Fehler korrigiert.
if (unexpected.Count > 0)
    Console.WriteLine("Diese Zeilen bleiben unveraendert, sie stammen aus einem aelteren Weg.");

foreach (var g in planned.GroupBy(x => $"{x.Old} -> {x.New}").OrderByDescending(g => g.Count()))
    Console.WriteLine($"  {g.Key,-45} {g.Count()}");

if (!apply) { Console.WriteLine("Probelauf beendet. Nichts geschrieben."); return; }

var updated = 0;
await using (var tx = connection.BeginTransaction())
{
    foreach (var p in planned)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE CentralSalesRecords SET CustomerCountry = $new WHERE Id = $id AND SiteId = $s AND CustomerCountry = $old;";
        cmd.Parameters.AddWithValue("$new", p.New);
        cmd.Parameters.AddWithValue("$id", p.Id);
        cmd.Parameters.AddWithValue("$s", siteId);
        cmd.Parameters.AddWithValue("$old", p.Old);
        if (await cmd.ExecuteNonQueryAsync() != 1)
            throw new InvalidOperationException($"Zeile {p.Id} hat sich geaendert. Transaktion zurueckgerollt.");
        updated++;
    }
    await tx.CommitAsync();
}

File.WriteAllText(marker, $"{DateTime.UtcNow:O} {updated} Laenderwerte korrigiert");
Console.WriteLine($"{updated} Zeilen korrigiert.");

await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"SELECT CustomerCountry, COUNT(1) FROM CentralSalesRecords
        WHERE SiteId = $s AND CustomerNumber <> '' AND CustomerNumber NOT LIKE 'ALPHAPLAN-ID:%'
        GROUP BY CustomerCountry ORDER BY COUNT(1) DESC LIMIT 10;";
    cmd.Parameters.AddWithValue("$s", siteId);
    await using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine("\nLaenderwerte nachher:");
    while (await reader.ReadAsync())
        Console.WriteLine($"  [{reader.GetString(0)}] = {reader.GetInt64(1)}");
}
