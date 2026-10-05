using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public sealed class ControllingAnalyticsTests
{
    private static readonly DateOnly RefEnd = new(2026, 10, 1);
    private static readonly DateOnly Prev = new(2025, 3, 1);
    private static readonly DateOnly Cur = new(2026, 3, 1);

    private static SalesFact Line(DateOnly date, string material, decimal qty, decimal local, decimal rate = 1m, string currency = "CHF", string tsc = "TRCH")
        => new(date, tsc, "CH", "K", "Kunde", "CH", material, material, "Transmitters", local * rate, qty, "R", local, currency);

    [Fact]
    public void Ein_Artikel_Menge_Und_Preis()
    {
        var b = ControllingAnalytics.Bridge("*", [Line(Prev, "A", 10, 100), Line(Cur, "A", 12, 132)], RefEnd, 9);

        Assert.Equal(100, b.Previous);
        Assert.Equal(132, b.Current);
        Assert.Equal(20, b.Volume);
        Assert.Equal(0, b.Mix);
        Assert.Equal(12, b.Price);
        Assert.Equal(0, b.Other);
    }

    [Fact]
    public void Mixeffekt_Bei_Gleicher_Gesamtmenge()
    {
        var b = ControllingAnalytics.Bridge("*",
        [
            Line(Prev, "A", 10, 100), Line(Prev, "B", 10, 300),
            Line(Cur, "A", 15, 150), Line(Cur, "B", 5, 150)
        ], RefEnd, 9);

        Assert.Equal(0, b.Volume);
        Assert.Equal(-100, b.Mix);
        Assert.Equal(0, b.Price);
        Assert.Equal(-100, b.Change);
    }

    [Fact]
    public void Waehrungseffekt_Bei_Gleichem_Lokalumsatz()
    {
        var b = ControllingAnalytics.Bridge("*", [Line(Prev, "A", 10, 100, 0.9m, "EUR", "TRIT"), Line(Cur, "A", 10, 100, 1.0m, "EUR", "TRIT")], RefEnd, 9);

        Assert.Equal(90, b.Previous);
        Assert.Equal(100, b.Current);
        Assert.Equal(10, b.Currency);
        Assert.Equal(0, b.Price);
        Assert.Equal(0, b.Volume);
    }

    [Fact]
    public void Neue_Weggefallene_Und_Uebrige()
    {
        var b = ControllingAnalytics.Bridge("*",
        [
            Line(Prev, "L", 3, 30),
            Line(Cur, "N", 5, 50),
            Line(Cur, "", 0, 7)
        ], RefEnd, 9);

        Assert.Equal(50, b.NewItems);
        Assert.Equal(-30, b.LostItems);
        Assert.Equal(7, b.Other);
        Assert.Equal(b.Change, b.Volume + b.Mix + b.Price + b.NewItems + b.LostItems + b.Currency + b.Other);
    }

    [Fact]
    public void Hochrechnung_Mit_Wachstum_Und_Vorjahr()
    {
        var facts = new List<SalesFact>();
        for (var m = 1; m <= 12; m++)
            facts.Add(Line(new DateOnly(2025, m, 1), "A", 1, 100));
        for (var m = 1; m <= 9; m++)
            facts.Add(Line(new DateOnly(2026, m, 1), "A", 1, 110));
        var p = ControllingAnalytics.Projection("*", facts, RefEnd, new DateOnly(2025, 1, 1));

        Assert.Equal(2026, p.Year);
        Assert.Equal(990, p.Ytd);
        Assert.Equal(900, p.PreviousYtd);
        Assert.Equal(1.1, p.Growth, 3);
        Assert.Equal(330, p.Rest);
        Assert.Equal(1320, p.Projected);
        Assert.Equal(1200, p.PreviousYear);
        Assert.Equal(10m, p.ChangePercent);
        Assert.Equal(12, p.Forecast.Count);
    }

    [Fact]
    public void Hochrechnung_Ohne_Volles_Vorjahr()
        => Assert.Null(ControllingAnalytics.Projection("*", [Line(Cur, "A", 1, 100)], RefEnd, new DateOnly(2025, 6, 1)).PreviousYear);
}
