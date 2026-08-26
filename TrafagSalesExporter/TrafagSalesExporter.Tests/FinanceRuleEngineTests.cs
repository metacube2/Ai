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

    // --- ISS-004.2: Spanien ordnet die Periode ueber das Rechnungsdatum zu.
    // Fachentscheid Andreas Stoller vom 2026-08-26. Ausdruecklich NUR Spanien.

    private static SalesRecord SpanishRow(DateTime? posting, DateTime? invoice) => new()
    {
        Land = "Spanien",
        Tsc = "TRES",
        PostingDate = posting,
        InvoiceDate = invoice,
        ExtractionDate = new DateTime(2026, 8, 26)
    };

    [Fact]
    public void ResolveFinanceDate_Es_NimmtDasRechnungsdatum_AuchWennEinBuchungsdatumDasteht()
    {
        var engine = new FinanceRuleEngine(FinanceRuleEngine.CreateDefaultRules());

        // Der kritische Fall an der Jahresgrenze: fakturiert am 31.12.2025, gebucht am 02.01.2026.
        var financeDate = engine.ResolveFinanceDate(
            SpanishRow(new DateTime(2026, 1, 2), new DateTime(2025, 12, 31)), "ES");

        Assert.Equal(new DateTime(2025, 12, 31), financeDate);
    }

    [Fact]
    public void ResolveFinanceDate_Es_FaelltOhneRechnungsdatumAufDasBuchungsdatumZurueck()
    {
        var engine = new FinanceRuleEngine(FinanceRuleEngine.CreateDefaultRules());

        var financeDate = engine.ResolveFinanceDate(
            SpanishRow(new DateTime(2026, 1, 2), null), "ES");

        Assert.Equal(new DateTime(2026, 1, 2), financeDate);
    }

    [Fact]
    public void ResolveFinanceDate_Es_OhneJedesDatum_BleibtBeimExtraktionsdatum()
    {
        var engine = new FinanceRuleEngine(FinanceRuleEngine.CreateDefaultRules());

        var financeDate = engine.ResolveFinanceDate(SpanishRow(null, null), "ES");

        Assert.Equal(new DateTime(2026, 8, 26), financeDate);
    }

    [Theory]
    [InlineData("CH")]
    [InlineData("AT")]
    [InlineData("DE")]
    [InlineData("IT")]
    [InlineData("IN")]
    [InlineData("FR")]
    [InlineData("UK")]
    [InlineData("US")]
    public void ResolveFinanceDate_AndereStandorte_BleibenBeimBuchungsdatum(string countryKey)
    {
        // Die Kernabsicherung zur Anweisung "invoicedate soll nur fuer spanien gelten".
        var engine = new FinanceRuleEngine(FinanceRuleEngine.CreateDefaultRules());
        var record = new SalesRecord
        {
            Tsc = "TR" + countryKey,
            PostingDate = new DateTime(2026, 1, 2),
            InvoiceDate = new DateTime(2025, 12, 31),
            ExtractionDate = new DateTime(2026, 8, 26)
        };

        var financeDate = engine.ResolveFinanceDate(record, countryKey);

        Assert.Equal(new DateTime(2026, 1, 2), financeDate);
    }

    [Fact]
    public void CreateDefaultRules_EnthaeltGenauEineUseInvoiceDateRegel_UndZwarFuerSpanien()
    {
        var rules = FinanceRuleEngine.CreateDefaultRules()
            .Where(rule => rule.RuleType == FinanceRuleTypes.UseInvoiceDate)
            .ToList();

        Assert.Single(rules);
        Assert.Equal("ES", rules[0].ScopeKey);
        Assert.Equal(FinanceRuleMatchTypes.Always, rules[0].MatchType);
        Assert.True(rules[0].IsActive);
    }

    [Fact]
    public void ResolveFinanceDate_Es_ForceYearSchlaegtDasRechnungsdatum()
    {
        // Reihenfolge festhalten: ein ausdruecklich erzwungenes Jahr bleibt die staerkste Regel.
        var rules = FinanceRuleEngine.CreateDefaultRules().ToList();
        rules.Add(new FinanceRule
        {
            ScopeKey = "ES",
            RuleType = FinanceRuleTypes.ForceYear,
            Year = 2024,
            MatchType = FinanceRuleMatchTypes.Always
        });

        var engine = new FinanceRuleEngine(rules);
        var financeDate = engine.ResolveFinanceDate(
            SpanishRow(new DateTime(2026, 1, 2), new DateTime(2025, 12, 31)), "ES");

        Assert.Equal(2024, financeDate.Year);
    }
}
