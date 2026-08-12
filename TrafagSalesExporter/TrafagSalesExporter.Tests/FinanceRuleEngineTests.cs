using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class FinanceRuleEngineTests
{
    [Fact]
    public void ResolveFinanceDate_De_UsesInvoiceDateYear_NotForced2025()
    {
        var engine = new FinanceRuleEngine(FinanceRuleEngine.CreateDefaultRules());
        var record = new SalesRecord
        {
            Land = "Deutschland",
            Tsc = "TRDE",
            InvoiceDate = new DateTime(2026, 3, 15),
            ExtractionDate = new DateTime(2026, 6, 1)
        };

        var financeDate = engine.ResolveFinanceDate(record, "DE");

        Assert.Equal(2026, financeDate.Year);
    }

    [Fact]
    public void CreateDefaultRules_NoLongerForcesDeYear()
    {
        var forced = FinanceRuleEngine.CreateDefaultRules()
            .Any(rule => rule.ScopeKey == "DE"
                && rule.RuleType == FinanceRuleTypes.ForceYear);

        Assert.False(forced);
    }
}
