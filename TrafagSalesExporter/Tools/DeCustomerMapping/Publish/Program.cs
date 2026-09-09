using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using System.IO.Compression;
using System.Security.Cryptography;
using TrafagSalesExporter.Data;
using ClosedXML.Excel;

if(args.Length<2) throw new ArgumentException("snapshot.db corrected-files...");
using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={args[0]};Mode=ReadOnly").Options);
var config=await db.SharePointConfigs.AsNoTracking().SingleAsync();
var de=await db.Sites.AsNoTracking().SingleAsync(x=>x.TSC=="TRDE");
var uri=new Uri(config.SiteUrl);
var graph=new GraphServiceClient(new ClientSecretCredential(config.TenantId,config.ClientId,config.ClientSecret),["https://graph.microsoft.com/.default"]);
var site=await graph.Sites[$"{uri.Host}:{uri.AbsolutePath.TrimEnd('/')}"] .GetAsync();
var drive=await graph.Sites[site!.Id].Drive.GetAsync();
var folder=new Uri(de.ManualImportFilePath).AbsolutePath[uri.AbsolutePath.TrimEnd('/').Length..].Trim('/');
var verifyOnly=args.Contains("--verify-only");
foreach(var file in args.Skip(1).Where(x=>x!="--verify-only"))
{
    var name=Path.GetFileName(file);
    if(!name.StartsWith("Sales_TRDE_") && !name.StartsWith("Sales_ProcessedMergeInput_TRDE_")) throw new ArgumentException("DE sales files only");
    var request=graph.Drives[drive!.Id].Root.ItemWithPath(folder+"/"+name);
    if(!verifyOnly)
    {
        try { if(await request.GetAsync() is not null) throw new IOException("Already exists; no overwrite: "+name); }
        catch(ODataError e) when(e.ResponseStatusCode==404) {}
        await using(var source=File.OpenRead(file)) await request.Content.PutAsync(source,c=>c.Headers.Add("If-None-Match","*"));
    }
    using var returned=new MemoryStream();
    await using(var stream=await request.Content.GetAsync()) await stream!.CopyToAsync(returned);
    var original=File.ReadAllBytes(file);
    if(name.EndsWith(".csv"))
    { if(!original.SequenceEqual(returned.ToArray())) throw new IOException("CSV verification failed"); }
    else
    {
        // SharePoint can add classification relationships. Compare all actual cells
        // and formulas rather than assuming the ZIP container stays byte-identical.
        using var a=new XLWorkbook(new MemoryStream(original));
        using var b=new XLWorkbook(new MemoryStream(returned.ToArray()));
        if(a.Worksheets.Count!=b.Worksheets.Count) throw new IOException("Worksheet count differs");
        foreach(var sheet in a.Worksheets)
        {
            var other=b.Worksheet(sheet.Name);
            if(sheet.CellsUsed().Count()!=other.CellsUsed().Count()) throw new IOException("Cell count differs: "+sheet.Name);
            foreach(var cell in sheet.CellsUsed())
            {
                var match=other.Cell(cell.Address.RowNumber,cell.Address.ColumnNumber);
                if(!cell.Value.Equals(match.Value) || cell.FormulaA1!=match.FormulaA1) throw new IOException("Excel cell differs: "+sheet.Name+"/"+cell.Address);
            }
        }
    }
    Console.WriteLine($"Verified {folder}/{name}; {returned.Length} bytes");
}
