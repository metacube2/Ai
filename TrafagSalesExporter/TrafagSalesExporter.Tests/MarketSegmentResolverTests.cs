using TrafagSalesExporter.Models;
using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Regeln der Kunden-Segment-Aufloesung. Der wichtigste Test ist der negative: eine Zeile
/// ohne gepflegte Zuordnung bekommt KEIN Segment, auch dann nicht, wenn das Quellfeld
/// CustomerIndustry etwas enthaelt.
/// </summary>
public class MarketSegmentResolverTests
{
    private static CustomerMarketSegment Row(
        string tsc,
        string customerNumber,
        string segment,
        string source = "Marktumfrage Railway 2026-05, bestaetigt",
        string name = "",
        DateTime? updated = null)
        => new()
        {
            Tsc = tsc,
            CustomerNumber = customerNumber,
            Segment = segment,
            Source = source,
            CustomerName = name,
            UpdatedAtUtc = updated ?? new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc)
        };

    [Fact]
    public void Resolve_FindsSegmentByTscAndCustomerNumber()
    {
        var lookup = MarketSegmentResolver.BuildLookup([Row("TRCH", "10042", "Railway")]);

        var (segment, source) = MarketSegmentResolver.Resolve("TRCH", "10042", lookup);

        Assert.Equal("Railway", segment);
        Assert.Equal("Marktumfrage Railway 2026-05, bestaetigt", source);
    }

    [Fact]
    public void Resolve_IsCaseAndWhitespaceInsensitive()
    {
        var lookup = MarketSegmentResolver.BuildLookup([Row("TRCH", "AB100", "Railway")]);

        var (segment, _) = MarketSegmentResolver.Resolve("  trch ", " ab100 ", lookup);

        Assert.Equal("Railway", segment);
    }

    [Fact]
    public void Resolve_SeparatesSameCustomerNumberInDifferentSites()
    {
        // Dieselbe Nummer bedeutet in zwei Standortsystemen zwei verschiedene Kunden.
        var lookup = MarketSegmentResolver.BuildLookup(
        [
            Row("TRCH", "10042", "Railway"),
            Row("TRIT", "10042", "Ship Building")
        ]);

        Assert.Equal("Railway", MarketSegmentResolver.Resolve("TRCH", "10042", lookup).Segment);
        Assert.Equal("Ship Building", MarketSegmentResolver.Resolve("TRIT", "10042", lookup).Segment);
    }

    [Fact]
    public void Resolve_ReturnsEmptyForUnmappedCustomer()
    {
        var lookup = MarketSegmentResolver.BuildLookup([Row("TRCH", "10042", "Railway")]);

        var (segment, source) = MarketSegmentResolver.Resolve("TRCH", "99999", lookup);

        Assert.Equal(string.Empty, segment);
        Assert.Equal(string.Empty, source);
    }

    [Fact]
    public void Resolve_ReturnsEmptyWhenSiteOrCustomerMissing()
    {
        var lookup = MarketSegmentResolver.BuildLookup([Row("TRCH", "10042", "Railway")]);

        Assert.Equal(string.Empty, MarketSegmentResolver.Resolve("", "10042", lookup).Segment);
        Assert.Equal(string.Empty, MarketSegmentResolver.Resolve("TRCH", "", lookup).Segment);
        Assert.Equal(string.Empty, MarketSegmentResolver.Resolve(null, null, lookup).Segment);
    }

    [Fact]
    public void Resolve_ReturnsEmptyWithoutLookup()
    {
        Assert.Equal(string.Empty, MarketSegmentResolver.Resolve("TRCH", "10042", null).Segment);
        Assert.Equal(string.Empty,
            MarketSegmentResolver.Resolve("TRCH", "10042", MarketSegmentResolver.BuildLookup(null)).Segment);
    }

    [Fact]
    public void BuildLookup_IgnoresRowsWithoutSegmentOrKey()
    {
        var lookup = MarketSegmentResolver.BuildLookup(
        [
            Row("TRCH", "10042", ""),
            Row("", "10043", "Railway"),
            Row("TRCH", "", "Railway")
        ]);

        Assert.Empty(lookup);
    }

    [Fact]
    public void BuildLookup_NewerEntryWinsOverOlder()
    {
        var lookup = MarketSegmentResolver.BuildLookup(
        [
            Row("TRCH", "10042", "Railway", updated: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)),
            Row("TRCH", "10042", "Industrie", updated: new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc))
        ]);

        Assert.Equal("Industrie", MarketSegmentResolver.Resolve("TRCH", "10042", lookup).Segment);
    }

    [Fact]
    public void Resolve_FallsBackToDefaultSourceLabelWhenSourceBlank()
    {
        var lookup = MarketSegmentResolver.BuildLookup([Row("TRCH", "10042", "Railway", source: "  ")]);

        var (_, source) = MarketSegmentResolver.Resolve("TRCH", "10042", lookup);

        Assert.Equal(MarketSegmentResolver.SourceCustomerMap, source);
    }

    [Fact]
    public void NormalizeCustomerNumber_KeepsLeadingZeros()
    {
        // Fuehrende Nullen duerfen nicht wegfallen, sonst wuerden 0100 und 100 zu einem
        // Kunden verschmelzen.
        Assert.Equal("00100", MarketSegmentResolver.NormalizeCustomerNumber(" 00100 "));
        Assert.NotEqual(
            MarketSegmentResolver.NormalizeCustomerNumber("100"),
            MarketSegmentResolver.NormalizeCustomerNumber("00100"));
    }
}
