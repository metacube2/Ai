using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class GroupMarginCostCurrencyConverterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mask")]
    [InlineData("irgendwas")]
    public void NormalizeMode_Falls_Back_To_Mask(string? mode)
        => Assert.Equal(GroupMarginCostCurrencyModes.Mask, GroupMarginCostCurrencyConverter.NormalizeMode(mode));

    [Theory]
    [InlineData("Convert")]
    [InlineData("convert")]
    [InlineData(" CONVERT ")]
    public void NormalizeMode_Accepts_Convert(string mode)
        => Assert.Equal(GroupMarginCostCurrencyModes.Convert, GroupMarginCostCurrencyConverter.NormalizeMode(mode));

    [Fact]
    public void Resolve_Same_Currency_Stays_Unchanged()
    {
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "CHF", 2025, GroupMarginCostCurrencyModes.Convert, (_, _, _) => 0.5m);

        Assert.Equal(60m, result.CostBasis);
        Assert.False(result.IsMismatch);
        Assert.False(result.IsMasked);
    }

    [Fact]
    public void Resolve_Blank_Cost_Currency_Is_Not_A_Mismatch()
    {
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "", 2025, GroupMarginCostCurrencyModes.Mask, null);

        Assert.False(result.IsMismatch);
        Assert.False(result.IsMasked);
    }

    [Fact]
    public void Resolve_Mismatch_Masks_By_Default()
    {
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "EUR", 2025, GroupMarginCostCurrencyModes.Mask, (_, _, _) => 0.95m);

        Assert.True(result.IsMismatch);
        Assert.True(result.IsMasked);
        Assert.Equal(60m, result.CostBasis);
    }

    [Fact]
    public void Resolve_Mismatch_Converts_With_Yearly_Rate()
    {
        DateTime? seenDate = null;
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "EUR", 2025, GroupMarginCostCurrencyModes.Convert,
            (from, to, date) =>
            {
                Assert.Equal("EUR", from);
                Assert.Equal("CHF", to);
                seenDate = date;
                return 0.95m;
            });

        Assert.Equal(new DateTime(2025, 12, 31), seenDate);
        Assert.False(result.IsMasked);
        Assert.Equal(57m, result.CostBasis);
        Assert.Equal(0.95m, result.AppliedRate);
    }

    [Fact]
    public void Resolve_Convert_Without_Rate_Falls_Back_To_Mask()
    {
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "EUR", 2025, GroupMarginCostCurrencyModes.Convert, (_, _, _) => null);

        Assert.True(result.IsMasked);
        Assert.Equal(60m, result.CostBasis);
    }

    [Fact]
    public void Resolve_Negative_Cost_Basis_Keeps_Sign_When_Converting()
    {
        // Gutschriften tragen eine negative Kostenbasis; die Umrechnung darf das Vorzeichen
        // nicht kippen (-60 EUR -> -57 CHF).
        var result = GroupMarginCostCurrencyConverter.Resolve(
            -60m, "CHF", "EUR", 2025, GroupMarginCostCurrencyModes.Convert, (_, _, _) => 0.95m);

        Assert.Equal(-57m, result.CostBasis);
    }
}
