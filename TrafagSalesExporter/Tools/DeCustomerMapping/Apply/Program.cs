using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;
using ClosedXML.Excel;

// Prepare first, then apply a guarded customer-only patch. No sales-row replacement.
// Usage: Apply <db> <mapping.json> <workdir> [--apply <source.csv> <server-output-directory>]
if (args.Length < 3) throw new ArgumentException("db mapping.json workdir [--apply source.csv server-output-directory]");
var folder = Path.GetFullPath(args[2]);
Directory.CreateDirectory(folder);
var map = JsonSerializer.Deserialize<List<Mapping>>(File.ReadAllText(args[1]))!
    .Where(x => x.Basis == "Belegnachweis").ToDictionary(x => (x.InvoiceNumber, x.InternalId));
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(new SqliteConnectionStringBuilder
{ DataSource = args[0], Mode = args.Contains("--apply") ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly }.ToString()).Options;
await using var db = new AppDbContext(options);
var site = await db.Sites.AsNoTracking().SingleAsync(x => x.TSC == "TRDE");
var settings = await db.ExportSettings.AsNoTracking().FirstAsync();
var rows = await db.CentralSalesRecords.AsNoTracking().Where(x => x.SiteId == site.Id).OrderBy(x => x.Id).ToListAsync();
var assignments = await db.CustomerMarketSegments.AsNoTracking().Where(x => x.Tsc == "TRDE").ToListAsync();
Console.WriteLine($"Rows={rows.Count}; Named={rows.Count(x => !string.IsNullOrWhiteSpace(x.CustomerName))}; Direct={rows.Count(x => map.ContainsKey((x.InvoiceNumber,x.CustomerNumber)))}");
Console.WriteLine($"Audit enabled={settings.AuditCsvEnabled}; Folder={settings.LocalSiteExportFolder}; Site override={site.LocalExportFolderOverride}");
if (!args.Contains("--apply") && !args.Contains("--validate"))
{
    File.WriteAllText(Path.Combine(folder, "before-rows.json"), JsonSerializer.Serialize(rows));
    File.WriteAllText(Path.Combine(folder, "before-segments.json"), JsonSerializer.Serialize(assignments));
    return;
}
if (args.Length != 6) throw new ArgumentException("Apply needs source.csv and server-output-directory");
var baseline = JsonSerializer.Deserialize<List<CentralSalesRecord>>(File.ReadAllText(Path.Combine(folder, "before-rows.json")))!;
if (JsonSerializer.Serialize(rows) != JsonSerializer.Serialize(baseline)) throw new InvalidOperationException("Live rows changed since prepare; aborting.");
if (JsonSerializer.Serialize(assignments) != File.ReadAllText(Path.Combine(folder, "before-segments.json"))) throw new InvalidOperationException("Segments changed since prepare.");
var records = await new ManualExcelImportService().ReadSalesRecordsAsync(args[4], site);
if (records.Count != rows.Count) throw new InvalidOperationException("CSV/DB row count differs.");
// Match full line keys and financial values before touching either destination.
LineKey Key(SalesRecord r) => new(r.DocumentEntry,r.InvoiceNumber,r.PositionOnInvoice,r.Material,r.CustomerNumber,r.SalesCurrency,r.SalesPriceValue,r.Quantity,r.StandardCost);
LineKey DbKey(CentralSalesRecord r) => new(r.DocumentEntry,r.InvoiceNumber,r.PositionOnInvoice,r.Material,r.CustomerNumber,r.SalesCurrency,r.SalesPriceValue,r.Quantity,r.StandardCost);
var csvKeys=records.GroupBy(Key).ToDictionary(x=>x.Key,x=>x.Count());
var dbKeys=rows.GroupBy(DbKey).ToDictionary(x=>x.Key,x=>x.Count());
if(csvKeys.Count!=dbKeys.Count || csvKeys.Any(x=>dbKeys.GetValueOrDefault(x.Key)!=x.Value))
{
    File.WriteAllText(Path.Combine(folder,"line-difference.json"),JsonSerializer.Serialize(new{Csv=csvKeys.Keys.Except(dbKeys.Keys).Take(5),Db=dbKeys.Keys.Except(csvKeys.Keys).Take(5)}));
    throw new InvalidOperationException("CSV and DB line contents differ; see line-difference.json. Aborting.");
}
foreach (var r in records)
{
    if (!string.IsNullOrWhiteSpace(r.CustomerName)) continue;
    if (map.TryGetValue((r.InvoiceNumber, r.CustomerNumber), out var m))
    { r.CustomerNumber=m.CustomerNumber; r.CustomerName=m.CustomerName; r.CustomerCountry=m.CustomerCountry; r.CustomerIndustry=m.CustomerIndustry; }
    else if (r.CustomerNumber.Length > 0 && !r.CustomerNumber.StartsWith("ALPHAPLAN-ID:")) r.CustomerNumber="ALPHAPLAN-ID:"+r.CustomerNumber;
}
// Produce files before the transaction; existing product files remain available.
var csv = await new ExportAuditCsvService().WriteSiteAuditCsvAsync(site, new ExportSettings { AuditCsvEnabled=true }, site.SourceSystem, folder, records);
var sourceExcel = Directory.EnumerateFiles(args[5], "Sales_TRDE_*.xlsx")
    .OrderByDescending(File.GetLastWriteTimeUtc).First();
var excel = Path.Combine(folder, $"Sales_TRDE_{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
// Preserve all existing financial sheets and amounts. Only customer cells change.
using (var workbook = new XLWorkbook(sourceExcel))
{
    foreach(var sheet in workbook.Worksheets)
    {
        var headers=sheet.FirstRowUsed()?.CellsUsed().ToDictionary(c=>c.GetString().Trim().ToLowerInvariant(),c=>c.Address.ColumnNumber);
        if(headers is null || !headers.TryGetValue("customer number",out var nc) || !headers.TryGetValue("invoice number",out var ic)) continue;
        foreach(var row in sheet.RowsUsed().Skip(1))
        {
            var number=row.Cell(nc).GetString();
            if(map.TryGetValue((row.Cell(ic).GetString(),number),out var m))
            {
                row.Cell(nc).Value=m.CustomerNumber;
                foreach(var pair in new[]{("customer name",m.CustomerName),("customer country",m.CustomerCountry),("customer industry",m.CustomerIndustry)})
                    if(headers.TryGetValue(pair.Item1,out var col)) row.Cell(col).Value=pair.Item2;
                if(m.CustomerIndustry.Trim() is "00 Bahn" or "05 rw Railways / Bahntechnik" && !assignments.Any(x=>x.CustomerNumber==m.CustomerNumber && x.IsConfirmed && x.Segment!="Railway"))
                {
                    if(headers.TryGetValue("market segment",out var sc)) row.Cell(sc).Value="Railway";
                    if(headers.TryGetValue("market segment source",out var sourceCol)) row.Cell(sourceCol).Value="Alphaplan Kundenstamm / Branche";
                }
            }
            else if(number.Length>0 && !number.StartsWith("ALPHAPLAN-ID:") && (!headers.TryGetValue("customer name",out var nameCol) || row.Cell(nameCol).IsEmpty())) row.Cell(nc).Value="ALPHAPLAN-ID:"+number;
        }
    }
    workbook.SaveAs(excel);
}
foreach (var file in new[] {csv!,excel})
    if (File.Exists(Path.Combine(args[5],Path.GetFileName(file)))) throw new IOException("Target already exists; inspect before replacing: "+Path.GetFileName(file));
if(args.Contains("--validate")) { Console.WriteLine("Validation OK: full line keys and financial values agree; prepared files only."); return; }
await db.Database.OpenConnectionAsync();
var connection = (SqliteConnection)db.Database.GetDbConnection();
await using (var tx = connection.BeginTransaction())
{
    foreach (var r in rows.Where(x => string.IsNullOrWhiteSpace(x.CustomerName)))
    {
        map.TryGetValue((r.InvoiceNumber,r.CustomerNumber),out var m);
        var number=m?.CustomerNumber ?? (r.CustomerNumber.Length == 0 || r.CustomerNumber.StartsWith("ALPHAPLAN-ID:") ? r.CustomerNumber : "ALPHAPLAN-ID:"+r.CustomerNumber);
        await using var cmd=connection.CreateCommand(); cmd.Transaction=tx;
        cmd.CommandText="UPDATE CentralSalesRecords SET CustomerNumber=$n, CustomerName=$name, CustomerCountry=$country, CustomerIndustry=$industry WHERE Id=$id AND SiteId=$site AND CustomerNumber=$old AND CustomerName=$oldname";
        cmd.Parameters.AddWithValue("$n",number);cmd.Parameters.AddWithValue("$name",m?.CustomerName??r.CustomerName);cmd.Parameters.AddWithValue("$country",m?.CustomerCountry??r.CustomerCountry);cmd.Parameters.AddWithValue("$industry",m?.CustomerIndustry??r.CustomerIndustry);
        cmd.Parameters.AddWithValue("$id",r.Id);cmd.Parameters.AddWithValue("$site",site.Id);cmd.Parameters.AddWithValue("$old",r.CustomerNumber);cmd.Parameters.AddWithValue("$oldname",r.CustomerName);
        if(await cmd.ExecuteNonQueryAsync()!=1) throw new InvalidOperationException("Concurrent customer change; transaction rolled back.");
    }
    foreach(var r in records.Where(x => x.CustomerIndustry.Trim() is "00 Bahn" or "05 rw Railways / Bahntechnik").GroupBy(x=>x.CustomerNumber).Select(x=>x.First()))
    {
        if(assignments.Any(x=>x.CustomerNumber==r.CustomerNumber && x.IsConfirmed && x.Segment!="Railway")) continue;
        await using var cmd=connection.CreateCommand();cmd.Transaction=tx;
        cmd.CommandText="""
            INSERT INTO CustomerMarketSegments (Tsc,CustomerNumber,CustomerName,Segment,IsConfirmed,ProposalNote,Source,UpdatedAtUtc)
            SELECT 'TRDE',$number,$name,'Railway',1,'','Alphaplan Kundenstamm / Branche',$now
            WHERE NOT EXISTS(SELECT 1 FROM CustomerMarketSegments WHERE Tsc='TRDE' AND CustomerNumber=$number);
            UPDATE CustomerMarketSegments SET CustomerName=$name, Segment='Railway',IsConfirmed=1,ProposalNote='',Source='Alphaplan Kundenstamm / Branche',UpdatedAtUtc=$now
            WHERE Tsc='TRDE' AND CustomerNumber=$number AND (IsConfirmed=0 OR Segment='Railway');
            """;
        cmd.Parameters.AddWithValue("$number",r.CustomerNumber);cmd.Parameters.AddWithValue("$name",r.CustomerName);cmd.Parameters.AddWithValue("$now",DateTime.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }
    await tx.CommitAsync();
}
File.WriteAllText(Path.Combine(folder,"database-applied.txt"),DateTime.UtcNow.ToString("O"));
foreach(var file in new[]{csv!,excel})
{
    var target=Path.Combine(args[5],Path.GetFileName(file));
    var staging=target+".de-repair-upload";
    File.Copy(file,staging,false); File.Move(staging,target,false);
    if(!System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file)).SequenceEqual(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(target)))) throw new IOException("Copy verification failed");
}
Console.WriteLine($"Applied customer-only correction; names={records.Count(x=>!string.IsNullOrWhiteSpace(x.CustomerName))}; CSV={csv}; Excel={excel}");

record Mapping(string InvoiceNumber,string InternalId,string CustomerNumber,string CustomerName,string CustomerCountry,string CustomerIndustry,string Basis);
record LineKey(int Entry,string Invoice,int Position,string Material,string Customer,string Currency,decimal Value,decimal Quantity,decimal Cost);
