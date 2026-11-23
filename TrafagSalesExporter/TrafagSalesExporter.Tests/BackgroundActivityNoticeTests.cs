using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

public class BackgroundActivityNoticeTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 13, 0, 0, DateTimeKind.Utc);

    private static PurchasingRefreshRunInfo Idle => new(false, string.Empty, null, null, string.Empty);

    [Fact]
    public void Ohne_Lauf_kein_Hinweis()
    {
        Assert.Null(BackgroundActivityNotice.Describe(Idle, exportRunning: false, Now));
    }

    [Fact]
    public void Einkauf_Delta_mit_Dauer()
    {
        var run = new PurchasingRefreshRunInfo(true, PurchasingRefreshRunner.ModeDelta, Now.AddMinutes(-21), null, string.Empty);

        var notice = BackgroundActivityNotice.Describe(run, exportRunning: false, Now);

        Assert.NotNull(notice);
        Assert.Contains("Delta", notice!.German);
        Assert.Contains("21 Min.", notice.German);
        Assert.Contains("langsamer", notice.German);
        Assert.Contains("more slowly", notice.English);
    }

    [Fact]
    public void Export_und_Full_Load_zusammen()
    {
        var run = new PurchasingRefreshRunInfo(true, PurchasingRefreshRunner.ModeFull, Now.AddMinutes(-5), null, string.Empty);

        var notice = BackgroundActivityNotice.Describe(run, exportRunning: true, Now);

        Assert.Contains("Full Load", notice!.German);
        Assert.Contains("Verkaufsexport", notice.German);
    }

    [Fact]
    public void Nur_Export()
    {
        var notice = BackgroundActivityNotice.Describe(Idle, exportRunning: true, Now);

        Assert.StartsWith("Achtung: Verkaufsexport laeuft.", notice!.German);
    }
}
