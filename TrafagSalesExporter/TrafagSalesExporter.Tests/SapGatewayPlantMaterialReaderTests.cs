using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class SapGatewayPlantMaterialReaderTests
{
    [Fact]
    public void ParseMaterialKeys_FiltersPlant_NormalizesAndDeduplicates()
    {
        const string json = """
        {
          "d": {
            "results": [
              { "Matnr": "000000000000001234", "Werks": "1100" },
              { "Matnr": "1234", "Werks": "1100" },
              { "Matnr": "000000000000005678", "Werks": "1200" },
              { "Matnr": " A-99 ", "Werks": "1100" }
            ]
          }
        }
        """;

        var result = SapGatewayPlantMaterialReader.ParseMaterialKeys(json, "1100");

        Assert.Equal(2, result.Count);
        Assert.Contains("1234", result);
        Assert.Contains("A-99", result);
        Assert.DoesNotContain("5678", result);
    }
}
