using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class PurchasingMaterialGroupTextCatalogTests
{
    [Fact]
    public void Resolve_Known_Code_Returns_Code_And_Text()
    {
        Assert.Equal("20.05.00 – Bälge", PurchasingMaterialGroupTextCatalog.Resolve("20.05.00"));
    }

    [Fact]
    public void Resolve_Unknown_Code_Falls_Back_To_Bare_Code()
    {
        // Neue/noch nicht nachgereichte Codes duerfen nie verschwinden - reiner Code als Fallback.
        Assert.Equal("ZZZ", PurchasingMaterialGroupTextCatalog.Resolve("ZZZ"));
    }

    [Fact]
    public void Resolve_OhneWarengruppe_Passes_Through_Unchanged()
    {
        Assert.Equal("ohne Warengruppe", PurchasingMaterialGroupTextCatalog.Resolve("ohne Warengruppe"));
    }

    [Fact]
    public void Resolve_Is_Case_Insensitive_On_Matkl_Code()
    {
        Assert.Equal("ts_auto – Typenschild automat.", PurchasingMaterialGroupTextCatalog.Resolve("ts_auto"));
    }
}
