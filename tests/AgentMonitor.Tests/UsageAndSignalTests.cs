using AgentMonitor.Core;

namespace AgentMonitor.Tests;

public class UsageAndSignalTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 17, 16, 0, 0, TimeSpan.FromHours(1));

    private const string StatusJson = """
        {"session_id":"s1","cwd":"C:\\repo\\Projekt",
         "model":{"id":"claude-fable-5-2","display_name":"Fable 5.2"},
         "context_window":{"used_percentage":45.5},
         "cost":{"total_cost_usd":1.23},
         "rate_limits":{"five_hour":{"used_percentage":34,"resets_at":1795455600},
                        "seven_day":{"used_percentage":12.5,"resets_at":1795800000}}}
        """;

    [Fact]
    public void Statuszeile_wird_gelesen()
    {
        var s = StatusInfo.Parse(StatusJson)!;
        Assert.Equal("s1", s.SessionId);
        Assert.Equal("Fable 5.2", s.ModelName);
        Assert.Equal(45.5, s.ContextPercent);
        Assert.Equal(34, s.FiveHour!.UsedPercent);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1795455600), s.FiveHour.ResetsAt);
        Assert.Equal(12.5, s.SevenDay!.UsedPercent);
    }

    [Fact]
    public void Statuszeile_ohne_Planfenster_und_kaputtes_JSON()
    {
        var s = StatusInfo.Parse("""{"session_id":"x","model":{"id":"claude-opus-5-5"}}""")!;
        Assert.Null(s.FiveHour);
        Assert.Null(s.SevenDay);
        Assert.Null(StatusInfo.Parse("kein json"));
        Assert.Null(StatusInfo.Parse("""{"model":{}}"""));
    }

    [Theory]
    [InlineData("claude-opus-5-5", null, "Opus 5.5", "opus")]
    [InlineData("claude-fable-5-2", null, "Fable 5.2", "fable")]
    [InlineData("claude-sonnet-5-5", "Claude Sonnet 5.5", "Sonnet 5.5", "sonnet")]
    [InlineData("claude-haiku-5-5-20261001", null, "Haiku 5.5", "haiku")]
    public void Modellnamen_und_Familie(string id, string? display, string kurz, string familie)
    {
        Assert.Equal(kurz, StatusInfo.ShortModel(id, display));
        Assert.Equal(familie, StatusInfo.Family(id, display));
    }

    [Fact]
    public void Zustand_uebernimmt_Modell_und_Planfenster()
    {
        var m = new MonitorState();
        m.ApplyStatus(StatusInfo.Parse(StatusJson)!, T0);
        var s = m.Snapshot().Single();
        Assert.Equal("Fable 5.2", s.Model);
        Assert.Equal("fable", s.ModelFamily);
        Assert.Equal("Projekt", s.Project);
        Assert.Equal(34, m.Plan!.FiveHour!.UsedPercent);
    }

    [Fact]
    public void Fehlendes_Fenster_bleibt_bis_zur_Erneuerung()
    {
        var m = new MonitorState();
        var reset = T0.AddHours(2);
        m.ApplyStatus(new StatusInfo { SessionId = "a", FiveHour = new UsageWindow(50, reset), SevenDay = new UsageWindow(10, T0.AddDays(3)) }, T0);
        m.ApplyStatus(new StatusInfo { SessionId = "b", SevenDay = new UsageWindow(11, T0.AddDays(3)) }, T0.AddMinutes(5));
        Assert.Equal(50, m.Plan!.FiveHour!.UsedPercent);   // vom letzten Stand
        Assert.Equal(11, m.Plan.SevenDay!.UsedPercent);    // neu

        m.ApplyStatus(new StatusInfo { SessionId = "b", SevenDay = new UsageWindow(12, T0.AddDays(3)) }, T0.AddHours(3));
        Assert.Null(m.Plan!.FiveHour);                     // Fenster erneuert, ohne neue Meldung kein Wert
    }

    [Fact]
    public void Signale_fuer_Klaenge()
    {
        var m = new MonitorState();
        HookEvent Ev(string json) => HookEvent.Parse(json)!;
        m.Apply(Ev("""{"hook_event_name":"Notification","session_id":"s","notification_type":"permission_prompt"}"""), T0);
        m.Apply(Ev("""{"hook_event_name":"TaskCompleted","session_id":"s"}"""), T0);
        m.Apply(Ev("""{"hook_event_name":"Stop","session_id":"s"}"""), T0);
        m.Apply(Ev("""{"hook_event_name":"Stop","session_id":"s","agent_id":"sub"}"""), T0); // kein Signal fuer Unteragent
        m.Apply(Ev("""{"hook_event_name":"StopFailure","session_id":"s","error":"x"}"""), T0);

        Assert.Equal(new[] { MonitorSignal.NeedsYou, MonitorSignal.TaskDone, MonitorSignal.AgentDone, MonitorSignal.Failed }, m.DrainSignals());
        Assert.Empty(m.DrainSignals());
    }

    [Fact]
    public void Tokens_je_Antwort_einmal_und_nur_von_heute()
    {
        var c = new TokenCounter("x");
        var today = new DateOnly(2026, 11, 17);
        string Line(string id, int o, string ts = "2026-11-17T10:00:00Z") =>
            "{\"type\":\"assistant\",\"timestamp\":\"" + ts + "\",\"message\":{\"id\":\"" + id +
            "\",\"usage\":{\"input_tokens\":2,\"output_tokens\":" + o +
            ",\"cache_creation_input_tokens\":100,\"cache_read_input_tokens\":1000}}}";

        c.Consume(Line("m1", 10), today);
        c.Consume(Line("m1", 50), today);                           // Streaming: dieselbe Antwort, neuerer Stand
        c.Consume(Line("m2", 5), today);
        c.Consume(Line("m3", 999, "2026-11-16T10:00:00Z"), today);  // gestern
        c.Consume("""{"type":"user","message":{"usage":{"input_tokens":5}}}""", today);
        c.Consume("kaputt", today);

        var t = c.Sum();
        Assert.Equal(new TokenTotals(4, 55, 200, 2000), t);
        Assert.Equal(2259, t.Total);
    }

    [Fact]
    public void Tokens_aus_Dateien_inkrementell()
    {
        var dir = Path.Combine(Path.GetTempPath(), "am-tok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "proj"));
        try
        {
            var f = Path.Combine(dir, "proj", "s.jsonl");
            var now = DateTimeOffset.Now;
            var ts = now.UtcDateTime.ToString("o");
            string Line(string id, int o) =>
                "{\"type\":\"assistant\",\"timestamp\":\"" + ts + "\",\"message\":{\"id\":\"" + id +
                "\",\"usage\":{\"input_tokens\":1,\"output_tokens\":" + o + "}}}";

            File.WriteAllText(f, Line("a", 10) + "\n");
            var c = new TokenCounter(dir);
            Assert.Equal(11, c.Refresh(now).Total);

            File.AppendAllText(f, Line("b", 20) + "\n" + "{\"type\":\"assist"); // halbe Zeile am Ende
            Assert.Equal(32, c.Refresh(now).Total);

            File.AppendAllText(f, "ant\",\"timestamp\":\"" + ts + "\",\"message\":{\"id\":\"c\",\"usage\":{\"output_tokens\":5}}}\n");
            Assert.Equal(37, c.Refresh(now).Total);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
