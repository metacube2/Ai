using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class UiTextServiceTests
{
    [Fact]
    public void Text_Returns_Selected_Language_Or_English_Fallback()
    {
        var service = new UiTextService();

        Assert.Equal("Standorte", service.Text("Standorte", "Sites"));

        service.SetLanguage("en");
        Assert.Equal("Sites", service.Text("Standorte", "Sites"));

        service.SetLanguage("es");
        Assert.Equal("Sitios", service.Text("Standorte", "Sites"));

        service.SetLanguage("it");
        Assert.Equal("Sedi", service.Text("Standorte", "Sites"));

        service.SetLanguage("hi");
        Assert.Equal("साइटें", service.Text("Standorte", "Sites"));
        Assert.Equal("Untranslated English", service.Text("Nicht uebersetzt", "Untranslated English"));
    }
}
