using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class TransformationStrategiesTests
{
    [Fact]
    public void ReplaceStrategy_Replaces_Text_Using_Argument_Syntax()
    {
        var strategy = new ReplaceTransformationStrategy();

        var result = strategy.Transform("Intercompany Kunde", "Intercompany=>Extern");

        Assert.Equal("Extern Kunde", result);
    }

    [Fact]
    public void ConstantStrategy_Returns_Argument_Ignoring_SourceValue()
    {
        var strategy = new ConstantTransformationStrategy();

        var result = strategy.Transform("ignored", "CHF");

        Assert.Equal("CHF", result);
    }

    [Fact]
    public void FirstNonEmptyRecordStrategy_Uses_First_Non_Empty_Field_From_Argument_List()
    {
        var strategy = new FirstNonEmptyRecordTransformationStrategy();
        var record = new SalesRecord
        {
            CustomerName = "",
            SupplierName = "Fallback Supplier",
            Name = "Article Name"
        };
        var rule = new FieldTransformationRule
        {
            RuleScope = "Record",
            TargetField = nameof(SalesRecord.CustomerName),
            TransformationType = "FirstNonEmpty",
            Argument = $"{nameof(SalesRecord.CustomerName)}|{nameof(SalesRecord.SupplierName)}|{nameof(SalesRecord.Name)}"
        };

        strategy.Transform(record, rule);

        Assert.Equal("Fallback Supplier", record.CustomerName);
    }
}
