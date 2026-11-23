using TrafagSalesExporter.Services;

namespace TrafagSalesExporter.Tests;

/// <summary>
/// Deploy-Pruefung „laeuft ein Einkauf-Lauf?“ aus dem Serverprotokoll. Die Zeilen stammen aus den
/// stdout-Logs vom 2026-11-20 (Deploy 12:51 brach den Delta von 12:31 ab, Nachhol-Lauf 13:39 bis 15:09).
/// </summary>
public class PurchasingRunLogTests
{
    private static readonly TimeSpan MaxAge = PurchasingRunLog.DefaultMaxRunAge;

    private static readonly string[] MorningLog =
    [
        "info: TrafagSalesExporter.Services.TimerBackgroundService[0]",
        "      Timer-Export gestartet um 11/20/2026 12:00:03",
        "info: TrafagSalesExporter.Services.TimerBackgroundService[0]",
        "      Einkauf-Delta (naechtlich) gestartet um 11/20/2026 12:31:47",
        "info: TrafagSalesExporter.Services.TimerBackgroundService[0]",
        "      Einkauf-Standardansicht vorberechnet in 84.1 s",
    ];

    private static readonly string[] AfternoonLog =
    [
        "info: TrafagSalesExporter.Services.TimerBackgroundService[0]",
        "      Einkauf-Delta Nachhol-Lauf gestartet um 11/20/2026 13:39:42, letzter Erfolg 19.11.2026 17:36",
        "info: TrafagSalesExporter.Services.TimerBackgroundService[0]",
        "      Einkauf-Delta Nachhol-Lauf nicht gestartet (es laeuft bereits ein Einkauf-Lauf) um 11/20/2026 13:54:43, letzter Erfolg 19.11.2026 17:36",
        "info: TrafagSalesExporter.Services.TimerBackgroundService[0]",
        "      Einkauf-Standardansicht vorberechnet in 196.7 s",
    ];

    [Fact]
    public void Deploy_um_1251_haette_den_laufenden_Delta_gesehen()
    {
        var state = PurchasingRunLog.Evaluate(MorningLog, new DateTime(2026, 11, 20, 12, 50, 0), MaxAge);

        Assert.True(state.Running);
        Assert.Equal(new DateTime(2026, 11, 20, 12, 31, 47), state.StartedLocal);
    }

    [Fact]
    public void Prozessende_beendet_den_Lauf()
    {
        var lines = MorningLog.Append("      Application is shutting down...");

        Assert.False(PurchasingRunLog.Evaluate(lines, new DateTime(2026, 11, 20, 12, 52, 0), MaxAge).Running);
    }

    [Fact]
    public void Nachhol_Lauf_laeuft_bis_zur_Ende_Zeile()
    {
        var at1430 = PurchasingRunLog.Evaluate(AfternoonLog, new DateTime(2026, 11, 20, 14, 30, 0), MaxAge);
        Assert.True(at1430.Running);
        Assert.Equal(new DateTime(2026, 11, 20, 13, 39, 42), at1430.StartedLocal);

        var finished = AfternoonLog.Append(
            "      Einkauf-Lauf Delta beendet: Delta abgeschlossen: geaenderte Belege=178'505, offene Belege nachgeladen=28'723");
        Assert.False(PurchasingRunLog.Evaluate(finished, new DateTime(2026, 11, 20, 15, 20, 0), MaxAge).Running);
    }

    [Fact]
    public void Nicht_gestartet_zaehlt_nicht_als_Start()
    {
        string[] lines =
        [
            "      Einkauf-Lauf Delta beendet: Delta abgeschlossen",
            "      Einkauf-Delta Nachhol-Lauf nicht gestartet (es laeuft bereits ein Einkauf-Lauf) um 11/20/2026 15:30:00, letzter Erfolg 20.11.2026 15:09",
        ];

        Assert.False(PurchasingRunLog.Evaluate(lines, new DateTime(2026, 11, 20, 15, 31, 0), MaxAge).Running);
    }

    [Fact]
    public void Neue_Runner_Zeile_wird_erkannt()
    {
        string[] lines = ["      Einkauf-Lauf Full gestartet um 11/21/2026 03:00:12"];

        Assert.True(PurchasingRunLog.Evaluate(lines, new DateTime(2026, 11, 21, 3, 30, 0), MaxAge).Running);
    }

    [Fact]
    public void Alter_Start_ohne_Ende_gilt_nach_drei_Stunden_als_beendet()
    {
        // Bis 20.11. schrieb der direkt gestartete Nachtlauf keine Ende-Zeile.
        var state = PurchasingRunLog.Evaluate(MorningLog, new DateTime(2026, 11, 20, 15, 45, 0), MaxAge);

        Assert.False(state.Running);
        Assert.Contains("ohne Ende-Zeile", state.Reason);
    }

    [Fact]
    public void Unlesbare_Startzeit_gilt_vorsichtshalber_als_laufend()
    {
        string[] lines = ["      Einkauf-Lauf Delta gestartet um morgen frueh"];

        Assert.True(PurchasingRunLog.Evaluate(lines, new DateTime(2026, 11, 20, 12, 0, 0), MaxAge).Running);
    }

    [Fact]
    public void Ohne_Einkauf_Zeilen_laeuft_nichts()
    {
        Assert.False(PurchasingRunLog.Evaluate(["      Now listening on: http://127.0.0.1:25060"], DateTime.Now, MaxAge).Running);
    }
}
