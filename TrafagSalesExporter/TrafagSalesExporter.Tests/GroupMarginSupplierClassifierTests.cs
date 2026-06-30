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
    [InlineData("Trafag GmbH", "DE")]            // any Trafag company is intercompany
    [InlineData("Trafag France", "FR")]
    public void Resolve_ReturnsInternal_WhenNameOrCodeContainsTrafag(string supplierName, string supplierCountry)
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, supplierName, supplierCountry);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result);
    }

    [Theory]
    [InlineData("Magnetic Sense GmbH", "DE")]    // not a "Trafag" name -> 3rd party here
    [InlineData("Bosch Sensortec", "DE")]
    [InlineData("External Supplier", "DE")]
    public void Resolve_ReturnsExternal_ForNonTrafagSuppliers(string supplierName, string supplierCountry)
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, supplierName, supplierCountry);

        Assert.Equal(GroupMarginSupplierClassifier.External, result);
    }

    [Fact]
    public void Resolve_MatchesTrafagViaSupplierNumber()
    {
        var result = GroupMarginSupplierClassifier.Resolve("TRAFAG-IND-001", null, null);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result);
    }

    [Fact]
    public void Resolve_ReturnsUnclear_WhenAllSupplierFieldsAreEmpty()
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, "", "   ");

        Assert.Equal(GroupMarginSupplierClassifier.Unclear, result);
    }
}
