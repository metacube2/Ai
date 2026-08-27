using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class GroupMarginCostCurrencyConverterTests
{
    // Beschluss Andreas vom 2026-08-27 (B5/B6): Umrechnen ist die Regel, Maskieren die
    // ausdruecklich gewaehlte Ausnahme. Bis dahin war es umgekehrt.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("irgendwas")]
    [InlineData("Convert")]
    [InlineData(" CONVERT ")]
    public void NormalizeMode_Falls_Back_To_Convert(string? mode)
        => Assert.Equal(GroupMarginCostCurrencyModes.Convert, GroupMarginCostCurrencyConverter.NormalizeMode(mode));

    [Theory]
    [InlineData("Mask")]
    [InlineData("mask")]
    [InlineData(" MASK ")]
    public void NormalizeMode_Keeps_Explicit_Mask(string mode)
        => Assert.Equal(GroupMarginCostCurrencyModes.Mask, GroupMarginCostCurrencyConverter.NormalizeMode(mode));

    [Fact]
    public void Resolve_Same_Currency_Stays_Unchanged()
    {
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "CHF", GroupMarginCostCurrencyModes.Convert, (_, _, _) => 0.5m);

        Assert.Equal(60m, result.CostBasis);
        Assert.False(result.IsMismatch);
        Assert.False(result.IsMasked);
    }

    [Fact]
    public void Resolve_Blank_Cost_Currency_Is_Not_A_Mismatch()
    {
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "", GroupMarginCostCurrencyModes.Mask, null);

        Assert.False(result.IsMismatch);
        Assert.False(result.IsMasked);
    }

    [Fact]
    public void Resolve_Mismatch_Masks_Only_When_Mask_Is_Chosen()
    {
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "EUR", GroupMarginCostCurrencyModes.Mask, (_, _, _) => 0.95m);

        Assert.True(result.IsMismatch);
        Assert.True(result.IsMasked);
        Assert.Equal(60m, result.CostBasis);
    }

    [Fact]
    public void Resolve_Mismatch_Converts_By_Default()
    {
        // Ohne gesetzten Modus gilt seit dem Beschluss Convert, nicht mehr Mask.
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "EUR", mode: null, (_, _, _) => 0.95m);

        Assert.False(result.IsMasked);
        Assert.Equal(57m, result.CostBasis);
    }

    [Fact]
    public void Resolve_Mismatch_Converts_With_Todays_Rate_Not_The_Year_End_Rate()
    {
        // B6: kein Rueckrechnen auf historische Kurse. Frueher wurde hier der 31.12. des
        // Finance-Jahres angefragt; jetzt muss es der laufende Tag sein.
        DateTime? seenDate = null;
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "EUR", GroupMarginCostCurrencyModes.Convert,
            (from, to, date) =>
            {
                Assert.Equal("EUR", from);
                Assert.Equal("CHF", to);
                seenDate = date;
                return 0.95m;
            });

        Assert.Equal(DateTime.Today, seenDate);
        Assert.NotEqual(new DateTime(DateTime.Today.Year, 12, 31), seenDate);
        Assert.False(result.IsMasked);
        Assert.Equal(57m, result.CostBasis);
        Assert.Equal(0.95m, result.AppliedRate);
    }

    [Fact]
    public void Resolve_Uses_The_Injected_Day_When_Given()
    {
        DateTime? seenDate = null;
        GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "EUR", GroupMarginCostCurrencyModes.Convert,
            (_, _, date) => { seenDate = date; return 0.95m; },
            today: new DateTime(2026, 3, 4, 17, 30, 0));

        Assert.Equal(new DateTime(2026, 3, 4), seenDate);
    }

    [Fact]
    public void Resolve_Convert_Without_Rate_Falls_Back_To_Mask()
    {
        // Ein fehlender Kurs darf keine erfundene Marge ergeben; die Zeile bleibt offen.
        var result = GroupMarginCostCurrencyConverter.Resolve(
            60m, "CHF", "EUR", GroupMarginCostCurrencyModes.Convert, (_, _, _) => null);

        Assert.True(result.IsMasked);
        Assert.Equal(60m, result.CostBasis);
    }

    [Fact]
    public void Resolve_Negative_Cost_Basis_Keeps_Sign_When_Converting()
    {
        // Gutschriften tragen eine negative Kostenbasis; die Umrechnung darf das Vorzeichen
        // nicht kippen (-60 EUR -> -57 CHF).
        var result = GroupMarginCostCurrencyConverter.Resolve(
            -60m, "CHF", "EUR", GroupMarginCostCurrencyModes.Convert, (_, _, _) => 0.95m);

        Assert.Equal(-57m, result.CostBasis);
    }
}
