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
    [InlineData("GFS", "DE")]
    [InlineData("GFS Sensorik", "DE")]
    [InlineData("Gesellschaft fuer Sensorik", "DE")]
    [InlineData("Gesellschaft fur Sensorik", "DE")]
    public void Resolve_ReturnsInternal_WhenNameOrCodeContainsGfs(string supplierName, string supplierCountry)
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, supplierName, supplierCountry);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result);
    }

    [Fact]
    public void Resolve_MatchesGfsViaSupplierNumber()
    {
        var result = GroupMarginSupplierClassifier.Resolve("GFS-001", null, null);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result);
    }

    [Theory]
    [InlineData("Magnetic Sense GmbH", "DE")]    // not a Trafag/GFS name -> 3rd party here
    [InlineData("Bosch Sensortec", "DE")]
    [InlineData("External Supplier", "DE")]
    public void Resolve_ReturnsExternal_ForNonInternalSuppliers(string supplierName, string supplierCountry)
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, supplierName, supplierCountry);

        Assert.Equal(GroupMarginSupplierClassifier.External, result);
    }

    [Theory]
    // Marker sind nur auf Wortgrenzen intern; diese Namen enthalten sie nur als Teilstring
    // und muessen extern bleiben (sonst wandert ein 3rd party faelschlich in die Intercompany-Marge).
    [InlineData("Triton S.r.l.", "IT")]          // enthaelt "TRIT"
    [InlineData("Trinity Instruments", "GB")]    // enthaelt "TRIN"
    [InlineData("Nutrition Systems", "US")]      // enthaelt "TRIT"
    [InlineData("Patagonia AG", "AR")]           // enthaelt "TR-AG"? nein: "TAG" -> extern
    public void Resolve_ReturnsExternal_WhenMarkerIsOnlyASubstring(string supplierName, string supplierCountry)
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, supplierName, supplierCountry);

        Assert.Equal(GroupMarginSupplierClassifier.External, result);
    }

    [Theory]
    [InlineData("AGFS-100")]                     // "GFS" nur als Teilstring einer Nummer
    [InlineData("LOGFS 42")]                      // "GFS" mitten im Token
    public void Resolve_ReturnsExternal_WhenGfsIsOnlyASubstringOfNumber(string supplierNumber)
    {
        var result = GroupMarginSupplierClassifier.Resolve(supplierNumber, null, null);

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

    [Theory]
    [InlineData("Trafag AG", GroupStandardCostEntities.TrAg)]
    [InlineData("Trafag Italia S.r.l.", GroupStandardCostEntities.TrIt)]
    [InlineData("Trafag Italy S.r.l.", GroupStandardCostEntities.TrIt)]
    [InlineData("Trafag Controls India Pvt. Ltd.", GroupStandardCostEntities.TrIn)]
    [InlineData("Trafag India Private Limited", GroupStandardCostEntities.TrIn)]
    public void ResolveDeliveringEntity_MatchesKnownEntities(string supplierName, string expectedEntity)
    {
        var result = GroupMarginSupplierClassifier.ResolveDeliveringEntity(supplierName);

        Assert.Equal(expectedEntity, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Gesellschaft fuer Sensorik")]  // intern, aber keine verifizierte Kostenquelle
    [InlineData("Bosch Sensortec")]
    [InlineData("Triton S.r.l.")]               // enthaelt "TRIT" als Substring, nicht "Trafag Italia"
    public void ResolveDeliveringEntity_ReturnsNull_WhenNoKnownEntityMatches(string? supplierName)
    {
        var result = GroupMarginSupplierClassifier.ResolveDeliveringEntity(supplierName);

        Assert.Null(result);
    }
}
