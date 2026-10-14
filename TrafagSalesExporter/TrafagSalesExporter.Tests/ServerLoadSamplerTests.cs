using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>CPU-Pegelanzeige in der Kopfleiste (2026-10-14).</summary>
public class ServerLoadSamplerTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.3, 1)]      // jede Last ab dem ersten Bruchteil zeigt ein Segment
    [InlineData(4, 1)]
    [InlineData(4.1, 2)]
    [InlineData(50, 13)]
    [InlineData(100, 25)]
    [InlineData(130, 25)]
    public void Segmente_Je_Prozent(double percent, int expected)
        => Assert.Equal(expected, ServerLoadSampler.LitSegments(percent, 25));

    [Fact]
    public void Messwerte_Liegen_Zwischen_0_Und_100()
    {
        var (machine, app) = ServerLoadSampler.Current;
        Assert.InRange(app, 0, 100);
        if (machine is { } m) Assert.InRange(m, 0, 100);
    }
}
