using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Services;

if(args.Length!=2) throw new ArgumentException("Consistent database snapshot and output.xlsx required");
var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={args[0]};Mode=ReadOnly").Options;
var service=new MarketSegmentExportService(new PooledDbContextFactory<AppDbContext>(options));
var sheets=await service.BuildSheetsAsync(null,null,"Rohail / Abgleich 09.09.2026");
File.WriteAllBytes(args[1],new ExcelExportService().CreateWorkbookBytes(sheets));
foreach(var sheet in sheets) Console.WriteLine($"{sheet.SheetName}: {sheet.Rows.Count}");
