using TrafagSalesExporter.Services.Shopfloor;

namespace TrafagSalesExporter.Tests;

public class ShopfloorAccessTests
{
    private static ShopfloorOptions Options(bool openForAll = false, params string[] users)
        => new() { OpenForAll = openForAll, AllowedUsers = users.ToList() };

    [Fact]
    public void Standard_Ist_Gesperrt_Fuer_Alle()
    {
        Assert.False(ShopfloorAccess.IsAllowed(new ShopfloorOptions(), "TRAFAG\\koi"));
        Assert.False(ShopfloorAccess.IsAllowed(new ShopfloorOptions(), null));
    }

    [Theory]
    [InlineData("koi", "TRAFAG\\koi", true)]          // Eintrag ohne Domaene passt auf jede Domaene
    [InlineData("KOI", "TRAFAG\\koi", true)]          // ohne Gross-/Kleinschreibung
    [InlineData("TRAFAG\\koi", "TRAFAG\\KOI", true)]  // Eintrag mit Domaene
    [InlineData("OTHER\\koi", "TRAFAG\\koi", false)]  // andere Domaene
    [InlineData("kohler", "TRAFAG\\koi", false)]
    [InlineData("koi", "koi", true)]                  // Konto ohne Domaene (Entwicklung)
    [InlineData("koi", "koi@trafag.com", true)]
    [InlineData("", "TRAFAG\\koi", false)]
    public void Freigegebene_Konten_Werden_Mit_Und_Ohne_Domaene_Erkannt(string entry, string windowsName, bool expected)
        => Assert.Equal(expected, ShopfloorAccess.IsAllowed(Options(false, entry), windowsName));

    [Fact]
    public void OpenForAll_Oeffnet_Fuer_Alle_Angemeldeten()
        => Assert.True(ShopfloorAccess.IsAllowed(Options(true), "TRAFAG\\irgendwer"));

    [Fact]
    public void Nicht_Angemeldet_Ist_Gesperrt_Auch_Mit_Eintrag()
        => Assert.False(ShopfloorAccess.IsAllowed(Options(false, "koi"), null));

    [Theory]
    [InlineData("TRAFAG\\Koi", "koi")]
    [InlineData("koi@trafag.com", "koi")]
    [InlineData(null, "anonym")]
    public void Editor_Kennung_Ist_Das_Windows_Konto_Ohne_Domaene(string? name, string expected)
        => Assert.Equal(expected, ShopfloorAccess.EditorName(name));
}
