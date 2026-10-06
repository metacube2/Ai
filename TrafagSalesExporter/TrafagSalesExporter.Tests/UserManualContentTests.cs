using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class UserManualContentTests
{
    [Fact]
    public void Kapitel_Wird_In_Abschnitte_Und_Bloecke_Zerlegt()
    {
        var c = UserManualContent.Parse(
        [
            "# Einkauf", "", "Einleitung", "zweite Zeile.", "", "## Spend", "### Wozu", "Text mit **fett**.",
            "- Punkt 1", "- Punkt 2", "", "| A | B |", "|---|---|", "| 1 | 2 |"
        ]);

        Assert.Equal("Einkauf", c.Title);
        Assert.Equal(2, c.Sections.Count);
        Assert.Equal("Einleitung zweite Zeile.", c.Sections[0].Blocks.Single().Text);
        var spend = c.Sections[1];
        Assert.Equal(['h', 'p', 'l', 't'], spend.Blocks.Select(b => b.Kind));
        Assert.Equal(2, spend.Blocks[2].Items.Count);
        Assert.Equal(2, spend.Blocks[3].Rows.Count);
        Assert.True(UserManualContent.Matches(spend, "PUNKT 2"));
    }

    [Fact]
    public void Inline_Kodiert_Html_Und_Setzt_Fett()
        => Assert.Equal("a &lt;b&gt; <strong>c</strong>", UserManualContent.Inline("a <b> **c**").Value);

    [Fact]
    public void Ausgelieferte_Kapitel_Haben_Titel_Und_Abschnitte()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "wwwroot"));
        var chapters = UserManualContent.Load(root);
        Assert.True(chapters.Count >= 9);
        Assert.All(chapters, c => Assert.NotEmpty(c.Sections));
    }
}
