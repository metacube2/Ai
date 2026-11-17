using System.Text.Json;
using AgentMonitor.Core;

namespace AgentMonitor.Tests;

public class DurationEstimatorTests
{
    private static JsonElement J(string json) => JsonDocument.Parse(json).RootElement.Clone();
    private static readonly DateTimeOffset T0 = new(2026, 11, 17, 15, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Schluessel_unterscheidet_Befehle_nicht_Argumente()
    {
        Assert.Equal("Bash:dotnet test", DurationEstimator.Key("Bash", J("""{"command":"dotnet test tests/A --no-build"}""")));
        Assert.Equal("Bash:git push", DurationEstimator.Key("Bash", J("""{"command":"git push origin main"}""")));
        // Umgebungsvariablen und cd zaehlen nicht zum Befehl.
        Assert.Equal("Bash:dotnet build", DurationEstimator.Key("Bash", J("""{"command":"cd /x && FOO=1 dotnet build"}""")));
        Assert.Equal("Agent:Explore", DurationEstimator.Key("Agent", J("""{"subagent_type":"Explore"}""")));
        Assert.Equal("Read", DurationEstimator.Key("Read", J("""{"file_path":"a.cs"}""")));
    }

    [Fact]
    public void Schaetzung_ist_der_Median_und_unbekannt_ist_null()
    {
        var e = new DurationEstimator();
        Assert.Null(e.Estimate("Bash:dotnet test"));
        foreach (var s in new[] { 10.0, 12, 100, 11 }) e.Record("Bash:dotnet test", s);
        Assert.Equal(11.5, e.Estimate("Bash:dotnet test"));
    }

    [Fact]
    public void Nur_die_neuesten_Messungen_zaehlen()
    {
        var e = new DurationEstimator();
        for (var i = 0; i < 30; i++) e.Record("k", 100);
        for (var i = 0; i < DurationEstimator.SamplesPerKey; i++) e.Record("k", 5);
        Assert.Equal(5, e.Estimate("k"));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 45)]
    [InlineData(10, 90)]
    public void Prozent_linear_bis_zur_Schaetzung(double elapsed, double expected) =>
        Assert.Equal(expected, DurationEstimator.Percent(TimeSpan.FromSeconds(elapsed), 10), 3);

    [Fact]
    public void Prozent_ueberschreitet_95_nie()
    {
        Assert.InRange(DurationEstimator.Percent(TimeSpan.FromSeconds(20), 10), 90, 95);
        Assert.True(DurationEstimator.Percent(TimeSpan.FromHours(5), 10) <= 95);
    }

    [Fact]
    public void Speichern_und_Laden()
    {
        var a = new DurationEstimator();
        a.Record("Read", 0.4);
        Assert.True(a.IsDirty);
        var b = new DurationEstimator();
        b.LoadJson(a.ToJson());
        Assert.Equal(0.4, b.Estimate("Read"));
    }

    [Fact]
    public void Rueckfall_genau_dann_Werkzeug_dann_Startwert()
    {
        var e = new DurationEstimator();
        Assert.Equal(10, e.EstimateWithFallback("Bash:neuer befehl"));   // Startwert
        Assert.Equal(1, e.EstimateWithFallback("Read"));

        e.Record("Bash:git status", 2);
        e.Record("Bash:git push", 6);
        Assert.Equal(4, e.EstimateWithFallback("Bash:neuer befehl"));    // Median aller Bash-Messungen
        Assert.Equal(6, e.EstimateWithFallback("Bash:git push"));        // genaue Art geht vor
        Assert.Equal(10, e.EstimateWithFallback("PowerShell:x"));        // anderes Werkzeug: Startwert
    }

    [Fact]
    public void Zustand_lernt_aus_fertigen_Aktionen_und_gibt_die_Schaetzung_weiter()
    {
        var m = new MonitorState();
        HookEvent Ev(string json) => HookEvent.Parse(json)!;

        m.Apply(Ev("""{"hook_event_name":"PreToolUse","session_id":"s","tool_name":"Bash","tool_use_id":"1","tool_input":{"command":"dotnet test"}}"""), T0);
        Assert.Equal(10, m.Snapshot()[0].Agents[0].Actions[0].EstimateSeconds); // erstes Mal: Startwert fuer Bash
        m.Apply(Ev("""{"hook_event_name":"PostToolUse","session_id":"s","tool_name":"Bash","tool_use_id":"1","duration_ms":100}"""), T0.AddSeconds(8));

        // Fehlschlaege lernen nicht mit.
        m.Apply(Ev("""{"hook_event_name":"PreToolUse","session_id":"s","tool_name":"Bash","tool_use_id":"2","tool_input":{"command":"dotnet test"}}"""), T0.AddSeconds(9));
        m.Apply(Ev("""{"hook_event_name":"PostToolUseFailure","session_id":"s","tool_name":"Bash","tool_use_id":"2","error":"x"}"""), T0.AddSeconds(10));

        m.Apply(Ev("""{"hook_event_name":"PreToolUse","session_id":"s","tool_name":"Bash","tool_use_id":"3","tool_input":{"command":"dotnet test --no-build"}}"""), T0.AddSeconds(20));
        var latest = m.Snapshot()[0].Agents[0].Actions[^1];
        Assert.Equal("3", latest.Id);
        Assert.Equal(8, latest.EstimateSeconds); // Wanduhr 8 s, nicht duration_ms
    }
}
