using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class PurchasingElikzTests
{
    // Befund 2026-10-05: der OData-Cache speichert EKPO.Elikz als 'True'/'False', nicht als 'X'.
    [Theory]
    [InlineData("False", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("True", false)]
    [InlineData("X", false)]
    [InlineData("1", false)]
    public void Endlieferkennzeichen_Beide_Formen(string? elikz, bool open)
        => Assert.Equal(open, WorldImpactService.IsOpen(elikz));
}
