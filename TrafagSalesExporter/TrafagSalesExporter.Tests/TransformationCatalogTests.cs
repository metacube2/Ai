using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class TransformationCatalogTests
{
    [Fact]
    public void Catalog_Returns_Value_And_Record_Strategies()
    {
        ITransformationStrategy[] valueStrategies =
        [
            new CopyTransformationStrategy(),
            new ConstantTransformationStrategy()
        ];
        IRecordTransformationStrategy[] recordStrategies =
        [
            new FirstNonEmptyRecordTransformationStrategy()
        ];

        var catalog = new TransformationCatalog(valueStrategies, recordStrategies);

        var all = catalog.GetAll();

        Assert.Contains(all, x => x.RuleScope == "Value" && x.Key == "Copy");
        Assert.Contains(all, x => x.RuleScope == "Value" && x.Key == "Constant");
        Assert.Contains(all, x => x.RuleScope == "Record" && x.Key == "FirstNonEmpty");
    }
}
