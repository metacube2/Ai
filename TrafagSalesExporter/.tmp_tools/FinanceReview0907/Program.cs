using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrafagSalesExporter.Data;
using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

// Synthetic, in-memory diagnostic only. No production connection, imports or app start.
using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
using (var db = new AppDbContext(options))
{
    db.Database.EnsureCreated();
    db.Sites.Add(new Site { Id=1, TSC="TRIT", Land="Italien", Schema="test", SourceSystem="BI1" });
    db.ExportSettings.Add(new ExportSettings { GroupMarginChfRateMode=GroupMarginChfRateModes.FinanceYearEndRate });
    db.CurrencyExchangeRates.AddRange(
        new CurrencyExchangeRate { FromCurrency="EUR", ToCurrency="CHF", Rate=0.9m, ValidFrom=new(2025,1,1), ValidTo=new(2025,12,31), IsActive=true },
        new CurrencyExchangeRate { FromCurrency="EUR", ToCurrency="CHF", Rate=0.8m, ValidFrom=new(2026,1,1), IsActive=true });
    db.GroupStandardCosts.Add(new GroupStandardCost { MaterialKey="M1", ValuationArea="1100", UnitCost=60m, Currency="CHF" });
    db.CentralSalesRecords.Add(new CentralSalesRecord {
        SiteId=1, Tsc="TRIT", Land="Italien", SourceSystem="BI1", Material="M1", InvoiceNumber="INV-TEST", PositionOnInvoice=1,
        CustomerName="External Customer", CustomerNumber="C1", SupplierName="Trafag AG", SupplierNumber="S1", SupplierCountry="CH",
        Quantity=1m, SalesPriceValue=100m, SalesCurrency="EUR", CompanyCurrency="EUR", StandardCost=70m, StandardCostCurrency="EUR", StandardCostVariable=20m,
        InvoiceDate=new(2025,3,1), PostingDate=new(2025,3,1), ExtractionDate=new(2025,3,2), StoredAtUtc=DateTime.UtcNow });
    db.SaveChanges();
}
var cockpit = new ManagementCockpitService(new Factory(options));
var result = await cockpit.AnalyzeFinanceSummaryAsync(2025,"IT",null);
var detail=result.GroupMarginDetailRows.Single();
var ledger=result.FinanceAuditLedgerRows.Single();
Console.WriteLine($"CHF_LABEL: summary sales={result.GroupMarginSummary.SalesValue}, label={result.GroupMarginSummary.DisplayCurrency}; expected 90 CHF");
Console.WriteLine($"RATE_PROFILES: group cost CHF={detail.CostBasisValueChf}, margin CHF={detail.MarginValueChf}; ledger cost CHF={ledger.CostBasisChf}, margin CHF={ledger.MarginChf}; expected cost 60 CHF, margin 30 CHF");
Console.WriteLine($"CONTRIBUTION_SUM: {result.GroupMarginSummary.ContributionMarginValue}; original currency DB 80 EUR, CHF expected 72");
var internalLine = new GroupMarginLine { Tsc="TRIT", Material="MISSING", SupplierName="Trafag AG", Quantity=1, StandardCost=70, StandardCostCurrency="EUR", NetSalesValue=100 };
var fallback=GroupMarginCalculator.Evaluate(internalLine);
Console.WriteLine($"IC_MISSING: status={fallback.Status}, source={fallback.CostSource}, cost={fallback.CostBasis}; LRD status={GroupMarginCalculator.Evaluate(internalLine with {SalesType="LRD"}).Status}");
var zero=GroupMarginCalculator.Evaluate(internalLine with { SupplierName="External",Quantity=0,NetSalesValue=-10 });
Console.WriteLine($"ZERO_QUANTITY_CREDIT: cost={zero.CostBasis}, margin={-10-zero.CostBasis}, status={zero.Status}; no goods returned: expected cost 0, margin -10");
var absentCurrency=GroupMarginCostCurrencyConverter.Resolve(60,"EUR","",null,(_,_,_)=>null);
Console.WriteLine($"MISSING_COST_CURRENCY: masked={absentCurrency.IsMasked}, basis={absentCurrency.CostBasis}");
var rows = new[] {new SalesRecord {Material="M1",Quantity=1,StandardCost=100,StandardCostCurrency="EUR"},new SalesRecord {Material="M1",Quantity=99,StandardCost=10,StandardCostCurrency="EUR"}};
Console.WriteLine($"AVERAGE: {B1GroupStandardCostBuilder.Build("TRIT",rows,DateTime.UtcNow,B1GroupStandardCostModes.AveragePositive).Single().UnitCost}; quantity weighted comparator=10.9");
var engine=new FinanceRuleEngine(FinanceRuleEngine.CreateDefaultRules());
var undated=new SalesRecord {Tsc="TRES",Land="Spanien",InvoiceNumber="0",SalesPriceValue=100,ExtractionDate=new(2026,9,7)};
Console.WriteLine($"UNDATED_ES: include={engine.ShouldInclude(undated,"ES")}, period={engine.ResolveFinanceDate(undated,"ES"):yyyy-MM-dd}");
using (var db = new AppDbContext(options))
{
    db.CentralSalesRecords.Add(new CentralSalesRecord {
        SiteId=1, Tsc="TRIT", Land="Italien", SourceSystem="BI1", Material="M1", InvoiceNumber="FREE-GOODS", PositionOnInvoice=1,
        CustomerName="External Customer", SupplierName="Trafag AG", Quantity=1, SalesPriceValue=0,
        SalesCurrency="EUR", CompanyCurrency="EUR", StandardCost=70, StandardCostCurrency="EUR",
        InvoiceDate=new(2025,3,1), PostingDate=new(2025,3,1), ExtractionDate=new(2025,3,2), StoredAtUtc=DateTime.UtcNow });
    db.SaveChanges();
}
var withFreeGoods=await cockpit.AnalyzeFinanceSummaryAsync(2025,"IT",null);
Console.WriteLine($"FREE_GOODS: group details={withFreeGoods.GroupMarginDetailRows.Count}, includes free goods={withFreeGoods.GroupMarginDetailRows.Any(r=>r.InvoiceNumber=="FREE-GOODS")}; cost unchanged={withFreeGoods.GroupMarginSummary.CostBasisValue==result.GroupMarginSummary.CostBasisValue}");
if (result.GroupMarginSummary.DisplayCurrency != "EUR" || detail.CostBasisValueChf != 67.5m || ledger.CostBasisChf != 60m || fallback.Status!="OK" || zero.CostBasis!=-70m)
    throw new Exception("Observed behavior changed: reassess findings.");

sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()=>new(options);
    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken=default)=>Task.FromResult(CreateDbContext());
}
