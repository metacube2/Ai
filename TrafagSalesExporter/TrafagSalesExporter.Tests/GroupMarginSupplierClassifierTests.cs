using TrafagSalesExporter.Models;
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
    [InlineData("TRCH")]
    [InlineData("TRAT")]
    [InlineData("trch")]                          // TSC-Vergleich ist case-insensitive
    public void Resolve_ReturnsInternal_ForChAtEvenWithoutSupplierFields(string tsc)
    {
        // CH/AT (FinanzdataSchweizOeSet) hat kein Lieferantenfeld - die Zeile ist trotzdem
        // per Definition intercompany, weil CH/AT immer als Trafag AG selbst verkauft.
        var result = GroupMarginSupplierClassifier.Resolve(null, "", "", tsc);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result);
    }

    [Fact]
    public void Resolve_ReturnsUnclear_ForOtherTscWithEmptySupplierFields()
    {
        // Die neue TSC-Regel darf nur CH/AT betreffen - DE/ES bleiben unveraendert "Unklar".
        var result = GroupMarginSupplierClassifier.Resolve(null, "", "", "TRDE");

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

    [Theory]
    [InlineData("TRCH")]
    [InlineData("TRAT")]
    public void ResolveDeliveringEntity_ReturnsTrAg_ForChAtEvenWithoutSupplierName(string tsc)
    {
        var result = GroupMarginSupplierClassifier.ResolveDeliveringEntity(null, tsc);

        Assert.Equal(GroupStandardCostEntities.TrAg, result);
    }

    [Fact]
    public void ResolveDeliveringEntity_ReturnsNull_ForOtherTscWithoutSupplierName()
    {
        var result = GroupMarginSupplierClassifier.ResolveDeliveringEntity(null, "TRDE");

        Assert.Null(result);
    }

    // Uebergangsregel Meeting 2026-07-30: Material ohne jede Supplier-Angabe, aber mit
    // Treffer in der Konzern-Kostentabelle (GroupStandardCosts) -> intern, statt "Unklar".

    private static IReadOnlyDictionary<(string MaterialKey, string ValuationArea), GroupStandardCost> GroupCostsWith(
        string materialKey, string valuationArea = "1100")
        => new Dictionary<(string, string), GroupStandardCost>
        {
            [(materialKey, valuationArea)] = new GroupStandardCost
            {
                MaterialKey = materialKey,
                ValuationArea = valuationArea,
                UnitCost = 12.5m,
                Currency = "CHF"
            }
        };

    [Fact]
    public void Resolve_ReturnsInternal_WhenMaterialKeyMatchesGroupCostTable_AndSupplierFieldsEmpty()
    {
        var costs = GroupCostsWith("ART123");

        var result = GroupMarginSupplierClassifier.Resolve(null, "", "", "TRDE", "ART123", costs);

        Assert.Equal(GroupMarginSupplierClassifier.Internal, result);
    }

    [Fact]
    public void Resolve_ReturnsUnclear_WhenMaterialKeyDoesNotMatchGroupCostTable()
    {
        var costs = GroupCostsWith("ART123");

        var result = GroupMarginSupplierClassifier.Resolve(null, "", "", "TRDE", "OTHER-ART", costs);

        Assert.Equal(GroupMarginSupplierClassifier.Unclear, result);
    }

    [Fact]
    public void Resolve_ReturnsUnclear_WhenGroupCostTableIsEmptyOrMissing()
    {
        var result = GroupMarginSupplierClassifier.Resolve(null, "", "", "TRDE", "ART123", null);

        Assert.Equal(GroupMarginSupplierClassifier.Unclear, result);
    }

    [Fact]
    public void Resolve_DoesNotOverrideExplicitExternalClassification_WithGroupCostMatch()
    {
        // Die Kostentabellen-Regel ist nur ein Fallback fuer den Unklar-Fall - eine per
        // Supplier-Text bereits als extern erkannte Zeile darf nicht ueberschrieben werden.
        var costs = GroupCostsWith("ART123");

        var result = GroupMarginSupplierClassifier.Resolve("V-001", "Bosch Sensortec", "DE", "TRDE", "ART123", costs);

        Assert.Equal(GroupMarginSupplierClassifier.External, result);
    }

    [Fact]
    public void ResolveDeliveringEntity_ReturnsMatchedEntity_ViaGroupCostTable_WhenSupplierNameEmpty()
    {
        var costs = GroupCostsWith("ART123");

        var result = GroupMarginSupplierClassifier.ResolveDeliveringEntity(null, "TRDE", "ART123", costs);

        Assert.Equal(GroupStandardCostEntities.TrAg, result);
    }

    [Fact]
    public void ResolveDeliveringEntity_ReturnsNull_WhenMaterialKeyNotInGroupCostTable()
    {
        var costs = GroupCostsWith("ART123");

        var result = GroupMarginSupplierClassifier.ResolveDeliveringEntity(null, "TRDE", "OTHER-ART", costs);

        Assert.Null(result);
    }
}
