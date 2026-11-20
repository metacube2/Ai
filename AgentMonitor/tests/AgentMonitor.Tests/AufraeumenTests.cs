using AgentMonitor.Core;

namespace AgentMonitor.Tests;

/// <summary>Aufräumen, Ausblenden, Auto-Ausblenden, Zusammenfassen, Warnung, Wartende zuerst, Tageszahlen (20.11.2026).</summary>
public class AufraeumenTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 20, 14, 0, 0, TimeSpan.FromHours(1));

    private static HookEvent Ev(string name, string json = "{}", string session = "s1")
    {
        var t = json.Trim();
        var body = t[1..^1].Trim();
        var full = $"{{\"hook_event_name\":\"{name}\",\"session_id\":\"{session}\",\"cwd\":\"C:\\\\repo\\\\Projekt\"" +
                   (body.Length > 0 ? "," + body : "") + "}";
        return HookEvent.Parse(full) ?? throw new InvalidOperationException(full);
    }

    private static string Bash(string id, string cmd = "git status") =>
        $$$"""{"tool_name":"Bash","tool_use_id":"{{{id}}}","tool_input":{"command":"{{{cmd}}}","description":"x"}}""";

    /// <summary>Sitzung s1: Runde mit zwei fertigen Bash-Aktionen, dann Stop (untaetig).</summary>
    private static MonitorState IdleSessionWithTwoActions()
    {
        var m = new MonitorState { FinishedRetention = null };
        m.Apply(Ev("UserPromptSubmit", """{"prompt":"x","session_title":"Monitor"}"""), T0);
        m.Apply(Ev("PreToolUse", Bash("a1")), T0.AddSeconds(1));
        m.Apply(Ev("PostToolUse", Bash("a1")), T0.AddSeconds(2));
        m.Apply(Ev("PreToolUse", Bash("a2")), T0.AddSeconds(3));
        m.Apply(Ev("PostToolUse", Bash("a2")), T0.AddSeconds(4));
        m.Apply(Ev("Stop", """{"last_assistant_message":"fertig"}"""), T0.AddSeconds(5));
        return m;
    }

    [Fact]
    public void Aufraeumen_blendet_untaetige_Sitzung_aus_und_entfernt_fertige_Aktionen()
    {
        var m = IdleSessionWithTwoActions();
        Assert.Single(m.Snapshot());

        var removed = m.ClearFinished(T0.AddSeconds(6));

        Assert.Equal(2, removed);
        Assert.Empty(m.Snapshot());
    }

    [Fact]
    public void Aufraeumen_behaelt_laufende_Aktion_und_arbeitende_Sitzung()
    {
        var m = new MonitorState { FinishedRetention = null };
        m.Apply(Ev("UserPromptSubmit"), T0);
        m.Apply(Ev("PreToolUse", Bash("a1")), T0.AddSeconds(1));
        m.Apply(Ev("PostToolUse", Bash("a1")), T0.AddSeconds(2));
        m.Apply(Ev("PreToolUse", Bash("a2", "dotnet test")), T0.AddSeconds(3));

        m.ClearFinished(T0.AddSeconds(4));

        var main = m.Snapshot().Single().Agents.Single(a => a.IsMain);
        var only = Assert.Single(main.Actions);
        Assert.Equal("a2", only.Id);
        Assert.Equal(ActionStatus.Running, only.Status);
        Assert.NotNull(main.TurnStart); // Rundenbalken laeuft weiter
    }

    [Fact]
    public void Ausgeblendete_Sitzung_bleibt_weg_bei_Statuszeile_und_idle_prompt()
    {
        var m = IdleSessionWithTwoActions();
        m.ClearFinished(T0.AddSeconds(6));

        m.ApplyStatus(StatusInfo.Parse("""{"session_id":"s1","model":{"id":"claude-opus-5-5"}}""")!, T0.AddSeconds(20));
        m.Apply(Ev("Notification", """{"notification_type":"idle_prompt","message":"waiting"}"""), T0.AddSeconds(65));

        Assert.Empty(m.Snapshot());
    }

    [Fact]
    public void Ausgeblendete_Sitzung_kommt_mit_neuem_Auftrag_zurueck_samt_Titel()
    {
        var m = IdleSessionWithTwoActions();
        m.ClearFinished(T0.AddSeconds(6));

        m.Apply(Ev("UserPromptSubmit"), T0.AddSeconds(30));

        var s = Assert.Single(m.Snapshot());
        Assert.Equal("Monitor", s.Title);
        Assert.Empty(s.Agents.Single(a => a.IsMain).Actions);
        Assert.Null(s.Agents.Single(a => a.IsMain).LastMessage);
    }

    [Fact]
    public void Aufraeumen_entfernt_beendete_Sitzung_und_fertige_Unteragenten()
    {
        var m = new MonitorState { FinishedRetention = null };
        m.Apply(Ev("UserPromptSubmit", session: "alt"), T0);
        m.Apply(Ev("SessionEnd", session: "alt"), T0.AddSeconds(1));

        m.Apply(Ev("UserPromptSubmit"), T0);
        m.Apply(Ev("SubagentStart", """{"agent_id":"x1","agent_type":"Explore"}"""), T0.AddSeconds(1));
        m.Apply(Ev("SubagentStop", """{"agent_id":"x1","agent_type":"Explore"}"""), T0.AddSeconds(2));
        m.Apply(Ev("SubagentStart", """{"agent_id":"x2","agent_type":"Plan"}"""), T0.AddSeconds(3));

        m.ClearFinished(T0.AddSeconds(4));

        var s = Assert.Single(m.Snapshot());
        Assert.Equal("s1", s.Id);
        Assert.Equal(new[] { "main", "x2" }, s.Agents.Select(a => a.Id));
    }

    [Fact]
    public void Sitzung_ausblenden_auch_wenn_sie_scheinbar_arbeitet_und_zurueck_beim_naechsten_Werkzeug()
    {
        var m = new MonitorState { FinishedRetention = null };
        m.Apply(Ev("UserPromptSubmit"), T0);
        m.Apply(Ev("PreToolUse", Bash("a1")), T0.AddSeconds(1));

        m.HideSession("s1");
        Assert.Empty(m.Snapshot());

        m.Apply(Ev("PreToolUse", Bash("a2")), T0.AddSeconds(5));
        Assert.Single(m.Snapshot());
    }

    [Fact]
    public void Fertige_Aktionen_verschwinden_nach_Ablauf_von_selbst()
    {
        var m = IdleSessionWithTwoActions();
        m.FinishedRetention = TimeSpan.FromMinutes(2);

        m.Prune(T0.AddSeconds(2 + 119));   // a1 endete bei 2 s: noch nicht abgelaufen
        Assert.Equal(2, m.Snapshot().Single().Agents.Single().Actions.Count);

        m.Prune(T0.AddSeconds(2 + 121));   // a1 abgelaufen, a2 (Ende 4 s) noch nicht
        Assert.Equal("a2", m.Snapshot().Single().Agents.Single().Actions.Single().Id);

        m.FinishedRetention = null;        // „nie“
        m.Prune(T0.AddHours(1));
        Assert.Single(m.Snapshot().Single().Agents.Single().Actions);
    }

    [Fact]
    public void Wartende_Sitzung_steht_zuoberst()
    {
        var m = new MonitorState();
        m.Apply(Ev("UserPromptSubmit", session: "wartet"), T0);
        m.Apply(Ev("UserPromptSubmit", session: "arbeitet"), T0.AddSeconds(1));
        m.Apply(Ev("PreToolUse", Bash("b1"), session: "arbeitet"), T0.AddSeconds(2));
        m.Apply(Ev("Notification", """{"notification_type":"permission_prompt","message":"x"}""", session: "wartet"), T0.AddSeconds(3));
        m.Apply(Ev("PreToolUse", Bash("b2"), session: "arbeitet"), T0.AddSeconds(4)); // neuestes Ereignis

        var list = m.Snapshot();
        Assert.Equal("wartet", list[0].Id);
        Assert.True(list[0].IsWaiting);
        Assert.False(list[1].IsWaiting);
    }

    [Fact]
    public void Warnung_ungewoehnlich_lang_nur_mit_gelernter_Dauer_ab_60_s_und_nicht_beim_Warten()
    {
        var start = T0;
        var a = new ActionView("x", "Bash", "s", ActionStatus.Running, start, null, LearnedSeconds: 10);
        Assert.False(a.IsOverdue(start.AddSeconds(59), agentWaiting: false)); // 3 × 10 s, aber Mindestens 60 s
        Assert.True(a.IsOverdue(start.AddSeconds(60), agentWaiting: false));
        Assert.False(a.IsOverdue(start.AddSeconds(600), agentWaiting: true));

        var lang = a with { LearnedSeconds = 40 };
        Assert.False(lang.IsOverdue(start.AddSeconds(119), false));
        Assert.True(lang.IsOverdue(start.AddSeconds(120), false));

        Assert.False((a with { LearnedSeconds = null }).IsOverdue(start.AddHours(1), false)); // nur Startwert
        Assert.False((a with { Status = ActionStatus.Done }).IsOverdue(start.AddHours(1), false));
    }

    [Fact]
    public void Gelernte_Dauer_wird_nur_bei_echter_Erfahrung_mitgegeben()
    {
        var m = new MonitorState { FinishedRetention = null };
        m.Apply(Ev("UserPromptSubmit"), T0);
        m.Apply(Ev("PreToolUse", Bash("a1")), T0.AddSeconds(1));
        Assert.Null(m.Snapshot().Single().Agents.Single().Actions.Single().LearnedSeconds);

        m.Apply(Ev("PostToolUse", Bash("a1")), T0.AddSeconds(6));
        m.Apply(Ev("PreToolUse", Bash("a2")), T0.AddSeconds(7));
        Assert.Equal(5, m.Snapshot().Single().Agents.Single().Actions.Last().LearnedSeconds);
    }

    [Fact]
    public void Ganzer_Befehl_bleibt_fuer_die_Detailansicht_erhalten()
    {
        var lang = "echo " + new string('x', 400);
        var m = new MonitorState();
        m.Apply(Ev("PreToolUse", Bash("a1", lang)), T0);

        var action = m.Snapshot().Single().Agents.Single().Actions.Single();
        Assert.Equal(lang, action.FullDetail);
        Assert.True(action.Detail!.Length <= ToolSummary.MaxDetailLength);
    }

    [Fact]
    public void Tageszahlen_zaehlen_bleiben_beim_Aufraeumen_und_beginnen_um_Mitternacht_neu()
    {
        var m = IdleSessionWithTwoActions();
        m.Apply(Ev("UserPromptSubmit"), T0.AddSeconds(10));
        m.Apply(Ev("Notification", """{"notification_type":"permission_prompt","message":"x"}"""), T0.AddSeconds(11));
        m.Apply(Ev("PreToolUse", Bash("a3")), T0.AddSeconds(12));
        m.Apply(Ev("PostToolUseFailure", Bash("a3")), T0.AddSeconds(13));
        m.Apply(Ev("StopFailure", """{"error":"rate_limit"}"""), T0.AddSeconds(20));

        m.ClearFinished(T0.AddSeconds(30));
        var d = m.Today(T0.AddSeconds(30));

        Assert.Equal(1, d.Turns);
        Assert.Equal(3, d.Actions);
        Assert.Equal(1, d.FailedActions);
        Assert.Equal(1, d.Errors);
        Assert.Equal(1, d.Waits);
        Assert.Equal(15, d.WorkSeconds); // 5 s erste Runde + 10 s abgebrochene Runde

        var morgen = new DateTimeOffset(2026, 11, 21, 0, 0, 1, TimeSpan.FromHours(1));
        Assert.Equal(0, m.Today(morgen).Actions);

        var neu = new MonitorState();
        neu.RestoreToday(d, T0.AddHours(1));
        Assert.Equal(3, neu.Today(T0.AddHours(1)).Actions);
        var alt = new MonitorState();
        alt.RestoreToday(d, morgen);
        Assert.Equal(0, alt.Today(morgen).Actions);
    }

    private static ActionView A(string id, int startS, int? endS, ActionStatus st = ActionStatus.Done) =>
        new(id, "Bash", id, st, T0.AddSeconds(startS), endS is { } e ? T0.AddSeconds(e) : null);

    [Fact]
    public void Zusammenfassen_zeigt_laufende_und_die_letzten_zwei_fertigen()
    {
        var list = new[]
        {
            A("f1", 0, 10), A("f2", 5, 20, ActionStatus.Failed), A("f3", 21, 22), A("f4", 23, 24), A("f5", 25, 26),
            A("r1", 27, null, ActionStatus.Running),
        };

        var (vis, dig) = ActionList.Arrange(list, FinishedDisplay.Compact, 6);
        Assert.Equal(new[] { "r1", "f5", "f4" }, vis.Select(a => a.Id));
        Assert.Equal(new ActionDigest(2, 1, TimeSpan.FromSeconds(22)), dig); // f1..f3: 0 bis 22 s Wanduhr, nicht 10+15+1

        var (nurLaufend, alle) = ActionList.Arrange(list, FinishedDisplay.RunningOnly, 6);
        Assert.Equal(new[] { "r1" }, nurLaufend.Select(a => a.Id));
        Assert.Equal(5, alle!.Count);

        var (einzeln, rest) = ActionList.Arrange(list, FinishedDisplay.All, 4);
        Assert.Equal(new[] { "r1", "f5", "f4", "f3" }, einzeln.Select(a => a.Id));
        Assert.Equal(2, rest!.Count);

        var (leer, keine) = ActionList.Arrange(Array.Empty<ActionView>(), FinishedDisplay.Compact, 6);
        Assert.Empty(leer);
        Assert.Null(keine);
    }
}
