using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class BomBrainGraphTests
{
    [Fact]
    public void Netz_Mit_Stufen_Risiko_LzCode_Und_Kanten_Von_Oben_Nach_Unten()
    {
        // P1 und P2 Endprodukte, B Baugruppe in P1, K1 in B und P2 (Kreuzpunkt), K2 nur in B
        var result = new BomInheritanceResult
        {
            ParentsOf = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["B"] = ["P1"],
                ["K1"] = ["B", "P2"],
                ["K2"] = ["B"]
            },
            ComponentImpacts =
            [
                new BomComponentImpact("B", "Baugruppe", 1, 1, 1, 1, 0, "", 0),
                new BomComponentImpact("K1", "Gleichteil", 2, 3, 2, 2, 1, "Lieferant A", 0),
                new BomComponentImpact("K2", "Feder", 1, 2, 1, 2, 2, "A, B", 3)
            ],
            Texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["P1"] = "Transmitter" }
        };
        var lz = new Dictionary<string, (string Code, int Sort)>(StringComparer.OrdinalIgnoreCase)
        {
            ["K1"] = ("C", 3),
            ["K2"] = ("A", 1)
        };

        var g = BomBrainGraph.Build(result, lz);

        var byId = g.Nodes.ToDictionary(n => n.Id);
        Assert.Equal(5, g.Nodes.Count);
        Assert.Equal(0, byId["P1"].Role);            // Endprodukt
        Assert.Equal(1, byId["B"].Role);             // Baugruppe
        Assert.Equal(2, byId["K1"].Role);            // Komponente
        Assert.Equal(3, byId["B"].Risk);             // kein Einkauf
        Assert.Equal(1, byId["K1"].Risk);            // eine Quelle
        Assert.Equal(2, byId["K2"].Risk);            // ueberfaellig geht vor
        Assert.Equal(-1, byId["P1"].Risk);           // nur Eltern
        Assert.Equal(2, byId["K1"].Parents);         // Kreuzpunkt mit zwei Verwendungen
        Assert.Equal("Transmitter", byId["P1"].Text);
        Assert.Equal(BomBrainGraph.NoCode, byId["P1"].Lz);
        Assert.Equal(["A", "C"], g.LzCodes);          // nach LZ-Sortierung
        Assert.Equal(4, g.Links.Count);
        var names = g.Nodes.Select(n => n.Id).ToList();
        Assert.Contains(g.Links, l => names[l[0]] == "P2" && names[l[1]] == "K1");   // Eltern -> Kind
    }
}
