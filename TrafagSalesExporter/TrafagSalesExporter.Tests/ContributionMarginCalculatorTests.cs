using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class ContributionMarginCalculatorTests
{
    [Fact]
    public void Resolve_WithoutVariableCost_StaysOpen()
    {
        var result = ContributionMarginCalculator.Resolve(
            quantity: 5m, salesValue: 100m, variableUnitCost: null,
            salesCurrency: "CHF", costCurrency: "CHF", year: 2026,
            costCurrencyMode: null, resolveRate: null);

        Assert.Null(result.VariableCostBasis);
        Assert.Null(result.ContributionMargin);
        Assert.Null(result.ContributionMarginPercent);
    }

    [Fact]
    public void Resolve_WithVariableCost_ComputesContributionMargin()
    {
        var result = ContributionMarginCalculator.Resolve(
            quantity: 5m, salesValue: 100m, variableUnitCost: 12m,
            salesCurrency: "CHF", costCurrency: "CHF", year: 2026,
            costCurrencyMode: null, resolveRate: null);

        Assert.Equal(60m, result.VariableCostBasis);
        Assert.Equal(40m, result.ContributionMargin);
        Assert.Equal(40m, result.ContributionMarginPercent);
    }

    [Fact]
    public void Resolve_CreditNote_ReversesVariableCostBasis()
    {
        // Gutschrift: Umsatz -100, variable Kosten 60 -> DB -40, nicht -160.
        var result = ContributionMarginCalculator.Resolve(
            quantity: -5m, salesValue: -100m, variableUnitCost: 12m,
            salesCurrency: "CHF", costCurrency: "CHF", year: 2026,
            costCurrencyMode: null, resolveRate: null);

        Assert.Equal(-60m, result.VariableCostBasis);
        Assert.Equal(-40m, result.ContributionMargin);
        Assert.Equal(40m, result.ContributionMarginPercent);
    }

    [Fact]
    public void Resolve_ZeroSales_UsesQuantitySign_And_NoPercent()
    {
        var result = ContributionMarginCalculator.Resolve(
            quantity: -2m, salesValue: 0m, variableUnitCost: 10m,
            salesCurrency: "CHF", costCurrency: "CHF", year: 2026,
            costCurrencyMode: null, resolveRate: null);

        Assert.Equal(-20m, result.VariableCostBasis);
        Assert.Equal(20m, result.ContributionMargin);
        Assert.Null(result.ContributionMarginPercent);
    }

    [Fact]
    public void Resolve_ZeroQuantity_UsesUnitCostAsBasis()
    {
        // Gleiche Sonderregel wie die Margen-Kostenbasis: Menge 0 -> Stueckpreis als Basis.
        var result = ContributionMarginCalculator.Resolve(
            quantity: 0m, salesValue: 100m, variableUnitCost: 12m,
            salesCurrency: "CHF", costCurrency: "CHF", year: 2026,
            costCurrencyMode: null, resolveRate: null);

        Assert.Equal(12m, result.VariableCostBasis);
        Assert.Equal(88m, result.ContributionMargin);
    }

    [Fact]
    public void Resolve_CurrencyMismatch_MaskMode_StaysOpen()
    {
        var result = ContributionMarginCalculator.Resolve(
            quantity: 5m, salesValue: 100m, variableUnitCost: 12m,
            salesCurrency: "EUR", costCurrency: "CHF", year: 2026,
            costCurrencyMode: "Mask", resolveRate: (_, _, _) => 0.5m);

        Assert.Null(result.VariableCostBasis);
        Assert.Null(result.ContributionMargin);
    }

    [Fact]
    public void Resolve_CurrencyMismatch_ConvertMode_UsesYearRate()
    {
        var result = ContributionMarginCalculator.Resolve(
            quantity: 5m, salesValue: 100m, variableUnitCost: 12m,
            salesCurrency: "EUR", costCurrency: "CHF", year: 2026,
            costCurrencyMode: "Convert", resolveRate: (from, to, date) =>
            {
                Assert.Equal("CHF", from);
                Assert.Equal("EUR", to);
                Assert.Equal(new DateTime(2026, 12, 31), date);
                return 0.5m;
            });

        Assert.Equal(30m, result.VariableCostBasis);
        Assert.Equal(70m, result.ContributionMargin);
    }

    [Fact]
    public void Resolve_CurrencyMismatch_ConvertMode_WithoutRate_FallsBackToOpen()
    {
        var result = ContributionMarginCalculator.Resolve(
            quantity: 5m, salesValue: 100m, variableUnitCost: 12m,
            salesCurrency: "EUR", costCurrency: "CHF", year: 2026,
            costCurrencyMode: "Convert", resolveRate: (_, _, _) => null);

        Assert.Null(result.ContributionMargin);
    }
}
