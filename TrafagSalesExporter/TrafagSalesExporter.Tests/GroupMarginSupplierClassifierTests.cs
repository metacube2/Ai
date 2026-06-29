using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class GroupMarginSupplierClassifierTests
{
    [Theory]
    [InlineData("Trafag AG", "CH")]
    [InlineData("TRCH", "")]
    [InlineData("", "TR-AG")]
    [InlineData("Trafag Italy S.r.l.", "IT")]
    [InlineData("Trafag Italia", "IT")]
    [InlineData("TRIT", "")]
    [InlineData("Trafag India Private Limited", "IN")]
    [InlineData("TRIN", "")]
    public void Resolve_ReturnsInternal_ForTheThreeTrafagEntities(string supplierName, string supplierCountry)
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, supplierName, supplierCountry);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result);
    }

    [Theory]
    [InlineData("Trafag GmbH", "DE")]            // other Trafag entity -> 3rd party
    [InlineData("Magnetic Sense GmbH", "DE")]    // intercompany but not one of the three
    [InlineData("Bosch Sensortec", "DE")]        // genuine 3rd party
    [InlineData("External Supplier", "DE")]
    public void Resolve_ReturnsExternal_ForEveryOtherSupplier(string supplierName, string supplierCountry)
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, supplierName, supplierCountry);

        Assert.Equal(GroupMarginSupplierClassifier.External, result);
    }

    [Fact]
    public void Resolve_ReturnsUnclear_WhenAllSupplierFieldsAreEmpty()
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, "", "   ");

        Assert.Equal(GroupMarginSupplierClassifier.Unclear, result);
    }

    [Fact]
    public void Resolve_DoesNotClassifyBareTrafagWithoutEntityMarker_AsInternal()
    {
        // A plain "Trafag" reference that does not match one of the three entity markers
        // must be treated as 3rd party per the Gruppenmarge decision.
        var result = GroupMarginSupplierClassifier.Resolve(null, "Trafag France", "FR");

        Assert.Equal(GroupMarginSupplierClassifier.External, result);
    }
}
