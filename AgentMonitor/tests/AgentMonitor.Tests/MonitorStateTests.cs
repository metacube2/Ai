using AgentMonitor.Core;

namespace AgentMonitor.Tests;

public class MonitorStateTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 17, 14, 0, 0, TimeSpan.FromHours(1));

    private static HookEvent Ev(string name, string json = "{}", string session = "s1")
    {
        // Genau eine aeussere Klammer entfernen; TrimEnd('}') wuerde auch verschachtelte Objekte abschneiden.
        var t = json.Trim();
        var body = t[1..^1].Trim();
        var full = $"{{\"hook_event_name\":\"{name}\",\"session_id\":\"{session}\",\"cwd\":\"C:\\\\repo\\\\Projekt\"" +
                   (body.Length > 0 ? "," + body : "") + "}";
        return HookEvent.Parse(full) ?? throw new InvalidOperationException(full);
    }

    private static AgentView MainOf(MonitorState m) => m.Snapshot().Single().Agents.Single(a => a.IsMain);

    [Fact]
    public void Prompt_macht_Hauptagent_denkend_und_Projektname_aus_cwd()
    {
        var m = new MonitorState();
        m.Apply(Ev("UserPromptSubmit", """{"prompt":"x","session_title":"Monitor bauen"}"""), T0);

        var s = m.Snapshot().Single();
        Assert.Equal("Projekt", s.Project);
        Assert.Equal("Monitor bauen", s.Title);
        Assert.Equal(AgentPhase.Thinking, MainOf(m).Phase);
        Assert.True(s.IsBusy);
    }

    [Fact]
    public void Werkzeug_laeuft_und_endet_mit_Dauer_aus_dem_Hook()
    {
        var m = new MonitorState();
        m.Apply(Ev("UserPromptSubmit"), T0);
        m.Apply(Ev("PreToolUse", """{"tool_name":"Bash","tool_use_id":"t1","tool_input":{"command":"dotnet test","description":"Tests laufen lassen"}}"""), T0.AddSeconds(1));

        var running = MainOf(m);
        Assert.Equal(AgentPhase.Working, running.Phase);
        var a = Assert.Single(running.Actions);
        Assert.Equal(ActionStatus.Running, a.Status);
        Assert.Equal("Tests laufen lassen", a.Summary);

        m.Apply(Ev("PostToolUse", """{"tool_name":"Bash","tool_use_id":"t1","duration_ms":2500}"""), T0.AddSeconds(5));
        var after = MainOf(m);
        Assert.Equal(AgentPhase.Thinking, after.Phase);
        Assert.Equal(ActionStatus.Done, after.Actions[0].Status);
        Assert.Equal(T0.AddSeconds(3.5), after.Actions[0].End);
    }

    [Fact]
    public void Fehlgeschlagenes_Werkzeug_wird_als_Fehler_markiert_Abbruch_nicht()
    {
        var m = new MonitorState();
        m.Apply(Ev("PreToolUse", """{"tool_name":"Bash","tool_use_id":"a"}"""), T0);
        m.Apply(Ev("PreToolUse", """{"tool_name":"Bash","tool_use_id":"b"}"""), T0);
        m.Apply(Ev("PostToolUseFailure", """{"tool_name":"Bash","tool_use_id":"a","error":"exit 1"}"""), T0.AddSeconds(1));
        m.Apply(Ev("PostToolUseFailure", """{"tool_name":"Bash","tool_use_id":"b","is_interrupt":true}"""), T0.AddSeconds(1));

        var acts = MainOf(m).Actions;
        Assert.Equal(ActionStatus.Failed, acts.Single(x => x.Id == "a").Status);
        Assert.Equal(ActionStatus.Done, acts.Single(x => x.Id == "b").Status);
    }

    [Fact]
    public void Parallele_Werkzeuge_halten_Phase_Working_bis_das_letzte_endet()
    {
        var m = new MonitorState();
        m.Apply(Ev("PreToolUse", """{"tool_name":"Read","tool_use_id":"a"}"""), T0);
        m.Apply(Ev("PreToolUse", """{"tool_name":"Grep","tool_use_id":"b"}"""), T0);
        m.Apply(Ev("PostToolUse", """{"tool_name":"Read","tool_use_id":"a"}"""), T0.AddSeconds(1));
        Assert.Equal(AgentPhase.Working, MainOf(m).Phase);
        m.Apply(Ev("PostToolUse", """{"tool_name":"Grep","tool_use_id":"b"}"""), T0.AddSeconds(2));
        Assert.Equal(AgentPhase.Thinking, MainOf(m).Phase);
    }

    [Fact]
    public void Unteragent_bekommt_eigene_Zeile_mit_Beschreibung_aus_dem_Agent_Aufruf()
    {
        var m = new MonitorState();
        m.Apply(Ev("PreToolUse", """{"tool_name":"Agent","tool_use_id":"ag1","tool_input":{"subagent_type":"Explore","description":"Hooks suchen"}}"""), T0);
        m.Apply(Ev("SubagentStart", """{"agent_id":"x7","agent_type":"Explore"}"""), T0.AddSeconds(1));
        m.Apply(Ev("PreToolUse", """{"agent_id":"x7","agent_type":"Explore","tool_name":"Grep","tool_use_id":"g1","tool_input":{"pattern":"hook"}}"""), T0.AddSeconds(2));

        var agents = m.Snapshot().Single().Agents;
        Assert.Equal(2, agents.Count);
        var sub = agents.Single(a => !a.IsMain);
        Assert.Equal("Explore: Hooks suchen", sub.Label);
        Assert.Equal(AgentPhase.Working, sub.Phase);
        Assert.Equal("sucht „hook“", Assert.Single(sub.Actions).Summary);

        // Das Werkzeug des Unteragenten gehoert nicht dem Hauptagenten.
        Assert.Single(agents.Single(a => a.IsMain).Actions); // nur der Agent-Aufruf

        m.Apply(Ev("SubagentStop", """{"agent_id":"x7","agent_type":"Explore"}"""), T0.AddSeconds(9));
        sub = m.Snapshot().Single().Agents.Single(a => !a.IsMain);
        Assert.Equal(AgentPhase.Done, sub.Phase);
        Assert.Equal(ActionStatus.Done, sub.Actions[0].Status);
    }

    [Fact]
    public void Stop_setzt_Hauptagent_untaetig_und_Sitzung_nicht_mehr_beschaeftigt()
    {
        var m = new MonitorState();
        m.Apply(Ev("UserPromptSubmit"), T0);
        m.Apply(Ev("Stop"), T0.AddSeconds(3));
        Assert.Equal(AgentPhase.Idle, MainOf(m).Phase);
        Assert.False(m.Snapshot().Single().IsBusy);
    }

    [Fact]
    public void Freigabeabfrage_setzt_Wartezustand()
    {
        var m = new MonitorState();
        m.Apply(Ev("PreToolUse", """{"tool_name":"Bash","tool_use_id":"a"}"""), T0);
        m.Apply(Ev("Notification", """{"notification_type":"permission_prompt","message":"Claude needs your permission"}"""), T0.AddSeconds(1));
        var main = MainOf(m);
        Assert.Equal(AgentPhase.Waiting, main.Phase);
        Assert.Equal("wartet auf Freigabe", main.Detail);

        m.Apply(Ev("PostToolUse", """{"tool_name":"Bash","tool_use_id":"a"}"""), T0.AddSeconds(4));
        Assert.Equal(AgentPhase.Thinking, MainOf(m).Phase);
    }

    [Fact]
    public void Fortschritt_aus_TodoWrite_und_aus_Task_Ereignissen()
    {
        var m = new MonitorState();
        m.Apply(Ev("PreToolUse", """{"tool_name":"TodoWrite","tool_use_id":"t","tool_input":{"todos":[{"content":"a","status":"completed"},{"content":"b","status":"in_progress"},{"content":"c","status":"pending"}]}}"""), T0);
        Assert.Equal((1, 3), (MainOf(m).TasksDone, MainOf(m).TasksTotal));

        var m2 = new MonitorState();
        m2.Apply(Ev("TaskCreated", """{"task_id":"1"}"""), T0);
        m2.Apply(Ev("TaskCreated", """{"task_id":"2"}"""), T0);
        m2.Apply(Ev("TaskCompleted", """{"task_id":"1"}"""), T0);
        Assert.Equal((1, 2), (MainOf(m2).TasksDone, MainOf(m2).TasksTotal));

        // Erledigte Aufgabe, deren Anlage der Monitor verpasst hat, zieht die Gesamtzahl mit.
        m2.Apply(Ev("TaskCompleted", """{"task_id":"2"}"""), T0);
        m2.Apply(Ev("TaskCompleted", """{"task_id":"0"}"""), T0);
        Assert.Equal((3, 3), (MainOf(m2).TasksDone, MainOf(m2).TasksTotal));
    }

    [Fact]
    public void Neue_Runde_setzt_erledigten_Fortschritt_zurueck()
    {
        var m = new MonitorState();
        m.Apply(Ev("TaskCreated"), T0);
        m.Apply(Ev("TaskCompleted"), T0);
        m.Apply(Ev("UserPromptSubmit"), T0.AddMinutes(1));
        Assert.Equal((0, 0), (MainOf(m).TasksDone, MainOf(m).TasksTotal));
    }

    [Fact]
    public void StopFailure_markiert_Fehler_und_schliesst_laufende_Aktionen()
    {
        var m = new MonitorState();
        m.Apply(Ev("PreToolUse", """{"tool_name":"Bash","tool_use_id":"a"}"""), T0);
        m.Apply(Ev("StopFailure", """{"error":"rate_limit"}"""), T0.AddSeconds(1));
        var main = MainOf(m);
        Assert.Equal(AgentPhase.Failed, main.Phase);
        Assert.Equal("rate_limit", main.Detail);
        Assert.Equal(ActionStatus.Failed, main.Actions[0].Status);
    }

    [Fact]
    public void Aktionsliste_ist_begrenzt()
    {
        var m = new MonitorState { MaxActionsPerAgent = 5 };
        for (var i = 0; i < 12; i++)
            m.Apply(Ev("PreToolUse", $$"""{"tool_name":"Read","tool_use_id":"r{{i}}"}"""), T0.AddSeconds(i));
        var acts = MainOf(m).Actions;
        Assert.Equal(5, acts.Count);
        Assert.Equal("r11", acts[^1].Id);
    }

    [Fact]
    public void Prune_entfernt_beendete_Sitzungen_und_alte_Unteragenten()
    {
        var m = new MonitorState { EndedRetention = TimeSpan.FromMinutes(1), DoneAgentRetention = TimeSpan.FromMinutes(1) };
        m.Apply(Ev("SubagentStart", """{"agent_id":"x","agent_type":"Explore"}""", "s1"), T0);
        m.Apply(Ev("SubagentStop", """{"agent_id":"x","agent_type":"Explore"}""", "s1"), T0);
        m.Apply(Ev("SessionEnd", """{"reason":"other"}""", "s2"), T0);

        m.Prune(T0.AddSeconds(30));
        Assert.Equal(2, m.Snapshot().Count);

        m.Prune(T0.AddMinutes(2));
        var s = Assert.Single(m.Snapshot());
        Assert.Equal("s1", s.Id);
        Assert.Single(s.Agents); // nur noch der Hauptagent
    }

    [Fact]
    public void Version_steigt_bei_jedem_Ereignis()
    {
        var m = new MonitorState();
        var v0 = m.Version;
        m.Apply(Ev("UserPromptSubmit"), T0);
        Assert.True(m.Version > v0);
    }

    [Fact]
    public void Mehrere_Sitzungen_beschaeftigte_zuerst()
    {
        var m = new MonitorState();
        m.Apply(Ev("Stop", session: "ruhig"), T0.AddSeconds(10));
        m.Apply(Ev("UserPromptSubmit", session: "aktiv"), T0);
        Assert.Equal(new[] { "aktiv", "ruhig" }, m.Snapshot().Select(s => s.Id));
    }
}
