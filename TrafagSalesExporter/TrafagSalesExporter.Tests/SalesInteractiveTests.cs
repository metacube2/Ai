using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class SalesInteractiveTests
{
    private static readonly DateOnly RefEnd = new(2026, 10, 1);
    private static readonly DateOnly Start = new(2025, 1, 1);

    private static SalesFact F(DateOnly d, string customer, decimal chf, string division = "Transmitters", string article = "A1", string country = "CH",
        string tsc = "TRCH", string invoice = "R1", string currency = "CHF")
        => new(d, tsc, "CH", customer, customer + " AG", country, article, article, division, chf, 1, invoice, chf, currency);

    [Fact]
    public void Galaxie_Je_Quartal_Mit_Wachstum()
    {
        var facts = new[] { F(new(2026, 4, 10), "K1", 100), F(new(2026, 7, 10), "K1", 150, invoice: "R2") };

        var frames = SalesInteractive.Galaxy(facts, RefEnd, new DateOnly(2026, 4, 1));

        Assert.Equal(2, frames.Count);
        var q3 = frames[1].Points.Single();
        Assert.Equal(150, q3.Revenue);
        Assert.Equal(50, q3.Growth, 6);
        Assert.Equal(1, q3.Invoices);
    }

    [Fact]
    public void Rennen_Gleitende_Drei_Monate()
    {
        var facts = new[] { F(new(2026, 1, 5), "K1", 10), F(new(2026, 2, 5), "K1", 20), F(new(2026, 3, 5), "K1", 30), F(new(2026, 3, 5), "K2", 100) };

        var frames = SalesInteractive.Race(facts, "customer", new DateOnly(2026, 4, 1), new DateOnly(2026, 1, 1));

        var march = frames.Single(f => f.Month == new DateOnly(2026, 3, 1));
        Assert.Equal("K2", march.Bars[0].Key);
        Assert.Equal(60, march.Bars.Single(b => b.Key == "K1").Value);
    }

    [Fact]
    public void Fluss_Summen_Je_Spalte_Gleich()
    {
        var facts = new[] { F(new(2026, 5, 1), "K1", 100, "D1", country: "DE"), F(new(2026, 6, 1), "K2", 50, "D2", country: "FR", tsc: "TRFR") };

        var (nodes, links) = SalesInteractive.Sankey(facts, RefEnd);

        Assert.Equal(150, nodes.Where(n => n.Column == 0).Sum(n => n.Value));
        Assert.Equal(150, nodes.Where(n => n.Column == 2).Sum(n => n.Value));
        Assert.Equal(4, links.Count);
    }

    [Fact]
    public void Sonnenstrahl_Rest_Als_Uebrige()
    {
        var facts = Enumerable.Range(1, 10).Select(i => F(new(2026, 5, 1), "K" + i, i)).ToList();

        var root = SalesInteractive.Sunburst(facts, RefEnd, topCustomers: 8);

        var customers = root.Children.Single().Children.Single().Children;
        Assert.Equal(9, customers.Count);
        Assert.Equal(3, customers.Single(c => c.Label == "Übrige").Value);
        Assert.Equal(55, root.Value);
    }

    [Fact]
    public void Simulator_Hebel_Wirken_Auf_Rest()
    {
        var b = new SimulatorBase(2026, 1000, 500, 1200, [("D1", 300m, 0.5m), ("D2", 200m, 0m)]);

        var none = SalesInteractive.Simulate(b, [], 0);
        var r = SalesInteractive.Simulate(b, [new SimulatorLever("D1", 10, 0), new SimulatorLever("*", 0, -50)], -10);

        Assert.Equal(1500, none.SimYear);
        Assert.Equal(330 + 100, r.SimRest);
        Assert.Equal(-16.5m, r.FxEffect);
        Assert.Equal(1000 + 430 - 16.5m, r.SimYear);
    }

    [Fact]
    public void Rhythmus_Erkennt_Ueberfaellige()
    {
        var facts = new[] { 1, 31, 61, 91 }.Select(d => F(new DateOnly(2026, 1, 1).AddDays(d), "K1", 100)).ToList();
        facts.Add(F(new(2026, 9, 30), "K2", 1));

        var r = SalesInteractive.Rhythm(facts, RefEnd).Single(c => c.Key == "K1");

        Assert.Equal(30, r.MedianDays);
        Assert.Equal(new DateOnly(2026, 4, 2).AddDays(30), r.Expected);
        Assert.True(r.OverdueDays > 100);
        Assert.True(r.OverdueRatio > 3);
    }

    [Fact]
    public void Netzwerk_Kante_Ab_Gemeinsamen_Kunden()
    {
        var facts = new List<SalesFact>();
        foreach (var k in new[] { "K1", "K2", "K3" })
        {
            facts.Add(F(new(2026, 5, 1), k, 10, article: "ART100"));
            facts.Add(F(new(2026, 5, 1), k, 10, article: "ART200"));
        }
        facts.Add(F(new(2026, 5, 1), "K4", 10, article: "ART300"));

        var (nodes, edges) = SalesInteractive.ArticleNetwork(facts, RefEnd, minBoth: 3);

        var e = Assert.Single(edges);
        Assert.Equal(3, e.Both);
        Assert.Equal(3, nodes.Count);
        Assert.All(nodes, n => Assert.InRange(n.X, 30, 970));
        Assert.NotEqual(nodes.Single(n => n.Id == "ART100").Group, nodes.Single(n => n.Id == "ART300").Group);
    }

    [Fact]
    public void Kalender_Und_Landschaft()
    {
        var facts = new[] { F(new(2026, 3, 2), "K1", 10, invoice: "R1"), F(new(2026, 3, 2), "K2", 5, invoice: "R2"), F(new(2025, 3, 2), "K1", 7) };

        var day = Assert.Single(SalesInteractive.Calendar(facts, 2026));
        Assert.Equal(15, day.Value);
        Assert.Equal(2, day.Invoices);

        var cells = SalesInteractive.Landscape(facts, RefEnd, Start);
        Assert.Equal(15, cells.Single(c => c.Month == new DateOnly(2026, 3, 1)).Value);
    }

    [Fact]
    public void Einkauf_Hauptwarengruppe_Aus_Warengruppe()
    {
        Assert.StartsWith("10.00.00", PurchasingInteractiveService.MainGroup("10.04.00"));
        Assert.Contains("Elektronik", PurchasingInteractiveService.MainGroup("10.04.00"));
        Assert.Equal("ohne Warengruppe", PurchasingInteractiveService.MainGroup(""));
        Assert.Equal("ABC", PurchasingInteractiveService.MainGroup("ABC"));
    }

    [Fact]
    public void Versandpauschale_Ist_Kein_Artikel()
    {
        Assert.False(SalesAnalytics.IsComparableArticle("123456", "Versand bis 2,9 kg, EP/DHL"));
        Assert.True(SalesAnalytics.IsComparableArticle("123456", "Drucktransmitter NAH 8254"));
    }
}
