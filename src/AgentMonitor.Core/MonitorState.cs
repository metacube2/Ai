namespace AgentMonitor.Core;

public enum AgentPhase
{
    /// <summary>Wartet auf eine neue Eingabe, nichts laeuft.</summary>
    Idle,
    /// <summary>Das Modell denkt bzw. schreibt, kein Werkzeug laeuft.</summary>
    Thinking,
    /// <summary>Mindestens ein Werkzeug laeuft.</summary>
    Working,
    /// <summary>Wartet auf eine Freigabe oder Antwort des Menschen.</summary>
    Waiting,
    /// <summary>Unteragent ist fertig.</summary>
    Done,
    /// <summary>Abbruch mit Fehler (StopFailure).</summary>
    Failed,
}

public enum ActionStatus { Running, Done, Failed }

/// <param name="EstimateSeconds">Geschaetzte Dauer aus frueheren gleichartigen Aktionen, oder null.</param>
/// <param name="FullDetail">Ganzer Befehl, Pfad oder Auftrag (Klick auf die Aktion, Kopieren); nur im Speicher.</param>
/// <param name="LearnedSeconds">Gelernte Dauer genau dieser Aktionsart (ohne Rueckfall), Grundlage der Warnung „laeuft ungewoehnlich lange“.</param>
public sealed record ActionView(
    string Id, string Tool, string Summary, ActionStatus Status,
    DateTimeOffset Start, DateTimeOffset? End, string? Detail = null, double? EstimateSeconds = null,
    string? FullDetail = null, double? LearnedSeconds = null)
{
    /// <summary>Ab so viel Sekunden darf eine Aktion ueberhaupt als „ungewoehnlich lang“ gelten.</summary>
    public const double OverdueMinSeconds = 60;
    /// <summary>Ab diesem Vielfachen der gelernten Dauer gilt eine laufende Aktion als ungewoehnlich lang.</summary>
    public const double OverdueFactor = 3;

    /// <summary>
    /// Laeuft die Aktion ungewoehnlich lange? Nur mit gelernter Dauer genau dieser Art, erst ab 60 s und nie,
    /// solange der Agent auf eine Freigabe wartet (dann ist die Wartezeit kein Haenger).
    /// </summary>
    public bool IsOverdue(DateTimeOffset now, bool agentWaiting) =>
        Status == ActionStatus.Running && !agentWaiting && LearnedSeconds is { } learned && learned > 0 &&
        (now - Start).TotalSeconds >= Math.Max(OverdueMinSeconds, OverdueFactor * learned);
}

/// <param name="Detail">Hinweis in Warnfarbe, z. B. „wartet auf Freigabe“ oder ein Fehler.</param>
/// <param name="LastMessage">Erste Zeile der letzten Antwort des Agenten (nach Stop/SubagentStop).</param>
public sealed record AgentView(
    string Id, string Label, bool IsMain, AgentPhase Phase, DateTimeOffset PhaseSince,
    int TasksDone, int TasksTotal, string? Detail, IReadOnlyList<ActionView> Actions,
    string? LastMessage = null, DateTimeOffset? TurnStart = null, double? TurnEstimateSeconds = null);

/// <param name="Model">Kurzname des Modells aus der Statuszeile, z. B. „Fable 5.2“.</param>
/// <param name="ModelFamily">fable, opus, sonnet oder haiku (fuer Farben).</param>
public sealed record SessionView(
    string Id, string Project, string? Title, bool Ended, DateTimeOffset LastEvent,
    IReadOnlyList<AgentView> Agents, string? Model = null, string? ModelFamily = null)
{
    /// <summary>Gilt eine Sitzung als aktiv (Monitor soll sichtbar sein)?</summary>
    public bool IsBusy => !Ended && Agents.Any(a => a.Phase is AgentPhase.Thinking or AgentPhase.Working or AgentPhase.Waiting);

    /// <summary>Wartet ein Agent dieser Sitzung auf eine Freigabe oder Antwort?</summary>
    public bool IsWaiting => !Ended && Agents.Any(a => a.Phase == AgentPhase.Waiting);
}

/// <summary>Tageszahlen fuer die Zusammenfassung im Tray. Bleiben beim Aufraeumen erhalten, beginnen um Mitternacht neu.</summary>
/// <param name="Turns">Beendete Runden des Hauptagenten (Auftrag bis Antwort).</param>
/// <param name="Subagents">Beendete Unteragenten.</param>
/// <param name="Actions">Beendete Aktionen (Werkzeugaufrufe).</param>
/// <param name="FailedActions">Davon fehlgeschlagen.</param>
/// <param name="Errors">Abbrueche mit Fehler (StopFailure).</param>
/// <param name="Waits">Rueckfragen um eine Freigabe.</param>
/// <param name="WorkSeconds">Summe der Rundendauern des Hauptagenten ueber alle Sitzungen (parallele Sitzungen zaehlen doppelt).</param>
public sealed record DayStats(
    DateOnly Day, int Turns = 0, int Subagents = 0, int Actions = 0, int FailedActions = 0,
    int Errors = 0, int Waits = 0, double WorkSeconds = 0);

/// <summary>Anlaesse fuer einen Klang.</summary>
public enum MonitorSignal
{
    /// <summary>Hauptagent hat seine Runde beendet (Stop).</summary>
    AgentDone,
    /// <summary>Wartet auf eine Freigabe (permission_prompt).</summary>
    NeedsYou,
    /// <summary>Abbruch mit Fehler (StopFailure).</summary>
    Failed,
    /// <summary>Eine Aufgabe der Aufgabenliste ist erledigt (TaskCompleted).</summary>
    TaskDone,
}

/// <summary>Planverbrauch, wie die Statuszeile ihn zuletzt gemeldet hat.</summary>
public sealed record PlanUsage(UsageWindow? FiveHour, UsageWindow? SevenDay, DateTimeOffset UpdatedAt);

/// <summary>
/// Zustand aller beobachteten Claude-Code-Sitzungen. Wird mit Hook-Ereignissen gefuettert und
/// liefert unveraenderliche Schnappschuesse fuer die Oberflaeche. Threadsicher.
/// Die Uhr wird von aussen uebergeben, damit die Tests deterministisch sind.
/// </summary>
public sealed class MonitorState
{
    public const string MainAgentId = "main";

    /// <summary>So viele Aktionen je Agent werden behalten; aeltere fallen weg.</summary>
    public int MaxActionsPerAgent { get; init; } = 40;
    /// <summary>Beendete Sitzungen verschwinden nach dieser Zeit.</summary>
    public TimeSpan EndedRetention { get; init; } = TimeSpan.FromMinutes(2);
    /// <summary>Untaetige Sitzungen ohne jedes Ereignis verschwinden nach dieser Zeit.</summary>
    public TimeSpan IdleRetention { get; init; } = TimeSpan.FromHours(3);
    /// <summary>Fertige Unteragenten bleiben so lange sichtbar.</summary>
    public TimeSpan DoneAgentRetention { get; init; } = TimeSpan.FromMinutes(10);
    /// <summary>
    /// Fertige Aktionen verschwinden so lange nach ihrem Ende von selbst (beim naechsten Prune); null = nie.
    /// Einstellbar im Tray, Standard 2 Minuten (Wunsch Ingo 20.11.).
    /// </summary>
    public TimeSpan? FinishedRetention { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Lernt Dauern und schaetzt den Fortschritt laufender Aktionen.</summary>
    public DurationEstimator Estimator { get; init; } = new();

    private readonly object _lock = new();
    private readonly Dictionary<string, Session> _sessions = new();
    private long _version;

    private readonly System.Collections.Concurrent.ConcurrentQueue<MonitorSignal> _signals = new();
    private PlanUsage? _plan;
    private DayStats? _today;

    /// <summary>Tageszahlen von heute (bezogen auf <paramref name="now"/>); nach Mitternacht leer.</summary>
    public DayStats Today(DateTimeOffset now)
    {
        lock (_lock) return RollDay(now);
    }

    /// <summary>Laedt gespeicherte Tageszahlen nach einem Neustart, wenn sie von heute sind.</summary>
    public void RestoreToday(DayStats saved, DateTimeOffset now)
    {
        lock (_lock)
        {
            if (saved.Day == DayOf(now) && _today is null) _today = saved;
        }
    }

    private static DateOnly DayOf(DateTimeOffset now) => DateOnly.FromDateTime(now.Date);

    private DayStats RollDay(DateTimeOffset now)
    {
        var day = DayOf(now);
        if (_today is null || _today.Day != day) _today = new DayStats(day);
        return _today;
    }

    private void Count(DateTimeOffset now, Func<DayStats, DayStats> change) => _today = change(RollDay(now));

    /// <summary>Steigt bei jeder Aenderung; die Oberflaeche erkennt so, ob neu gezeichnet werden muss.</summary>
    public long Version => Interlocked.Read(ref _version);

    /// <summary>Zuletzt gemeldeter Planverbrauch (aus der Statuszeile), oder null.</summary>
    public PlanUsage? Plan { get { lock (_lock) return _plan; } }

    /// <summary>
    /// Laedt einen gespeicherten Planverbrauch (z. B. nach einem Neustart des Monitors), solange noch
    /// keine frische Meldung da ist. Fenster, deren Erneuerung schon vorbei ist, werden verworfen.
    /// </summary>
    public void RestorePlan(PlanUsage saved, DateTimeOffset now)
    {
        static UsageWindow? Valid(UsageWindow? w, DateTimeOffset now) =>
            w is { ResetsAt: { } r } && r > now ? w : null;
        lock (_lock)
        {
            if (_plan is not null) return;
            var five = Valid(saved.FiveHour, now);
            var week = Valid(saved.SevenDay, now);
            if (five is null && week is null) return;
            _plan = new PlanUsage(five, week, saved.UpdatedAt);
            Interlocked.Increment(ref _version);
        }
    }

    /// <summary>Holt die seit dem letzten Aufruf angefallenen Klang-Anlaesse ab.</summary>
    public IReadOnlyList<MonitorSignal> DrainSignals()
    {
        var list = new List<MonitorSignal>();
        while (_signals.TryDequeue(out var s)) list.Add(s);
        return list;
    }

    /// <summary>
    /// Nimmt eine Meldung der Statuszeile auf: Modell der Sitzung und Planfenster.
    /// Ein Fenster, das in der Meldung fehlt, bleibt auf dem letzten bekannten Stand,
    /// bis seine Erneuerungszeit vorbei ist.
    /// </summary>
    public void ApplyStatus(StatusInfo st, DateTimeOffset now)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(st.SessionId, out var s))
            {
                s = new Session(st.SessionId, ProjectName(st.Cwd), now);
                _sessions[st.SessionId] = s;
            }
            s.Model = StatusInfo.ShortModel(st.ModelId, st.ModelName) ?? s.Model;
            s.ModelFamily = StatusInfo.Family(st.ModelId, st.ModelName) ?? s.ModelFamily;

            static UsageWindow? Keep(UsageWindow? neu, UsageWindow? alt, DateTimeOffset now) =>
                neu ?? (alt is { ResetsAt: { } r } && r > now ? alt : null);

            if (st.FiveHour is not null || st.SevenDay is not null || _plan is not null)
            {
                _plan = new PlanUsage(Keep(st.FiveHour, _plan?.FiveHour, now), Keep(st.SevenDay, _plan?.SevenDay, now),
                    st.FiveHour is not null || st.SevenDay is not null ? now : _plan!.UpdatedAt);
            }
            Interlocked.Increment(ref _version);
        }
    }

    public void Apply(HookEvent e, DateTimeOffset now)
    {
        lock (_lock)
        {
            var s = GetSession(e, now);
            s.LastEvent = now;
            if (e.SessionTitle is { } t) s.Title = t;

            var agent = e.AgentId is { } aid ? GetSubagent(s, aid, e.AgentType, now) : s.Main;

            switch (e.Event)
            {
                case "SessionStart":
                    s.Ended = false;
                    s.Main.SetPhase(AgentPhase.Idle, now);
                    break;

                case "UserPromptSubmit":
                    s.Main.SetPhase(AgentPhase.Thinking, now);
                    s.Main.Detail = null;
                    s.Main.LastMessage = null;
                    // Neue Runde: Aufgabenzaehler der vorigen Runde zuruecksetzen, wenn alles erledigt war.
                    if (s.Main.TasksTotal > 0 && s.Main.TasksDone >= s.Main.TasksTotal)
                        s.Main.TasksDone = s.Main.TasksTotal = 0;
                    s.Main.TurnStarted ??= now; // eine Rueckfrage mitten in der Runde startet keine neue
                    break;

                case "PreToolUse":
                    StartAction(s, agent, e, now);
                    break;

                case "PostToolUse":
                    if (FinishAction(agent, e, ActionStatus.Done, now, Estimator))
                        Count(now, d => d with { Actions = d.Actions + 1 });
                    break;

                case "PostToolUseFailure":
                    var failStatus = e.IsInterrupt ? ActionStatus.Done : ActionStatus.Failed;
                    if (FinishAction(agent, e, failStatus, now, Estimator))
                        Count(now, d => d with
                        {
                            Actions = d.Actions + 1,
                            FailedActions = d.FailedActions + (failStatus == ActionStatus.Failed ? 1 : 0),
                        });
                    break;

                case "SubagentStart":
                    agent.SetPhase(AgentPhase.Thinking, now);
                    agent.TurnStarted ??= now;
                    break;

                case "SubagentStop":
                    if (!agent.IsMain)
                    {
                        if (agent.Ended is null) Count(now, d => d with { Subagents = d.Subagents + 1 });
                        EndTurn(agent, now, learn: true);
                        CloseRunning(agent, ActionStatus.Done, now);
                        agent.SetPhase(AgentPhase.Done, now);
                        agent.Ended = now;
                        agent.LastMessage = ToolSummary.FirstLine(e.LastAssistantMessage) ?? agent.LastMessage;
                    }
                    break;

                case "Stop":
                    if (agent.IsMain)
                    {
                        var turn = EndTurn(agent, now, learn: true);
                        Count(now, d => d with { Turns = d.Turns + 1, WorkSeconds = d.WorkSeconds + turn });
                        CloseRunning(agent, ActionStatus.Done, now);
                        agent.SetPhase(AgentPhase.Idle, now);
                        agent.Detail = null;
                        agent.LastMessage = ToolSummary.FirstLine(e.LastAssistantMessage) ?? agent.LastMessage;
                        _signals.Enqueue(MonitorSignal.AgentDone);
                    }
                    break;

                case "StopFailure":
                    _signals.Enqueue(MonitorSignal.Failed);
                    var failedTurn = EndTurn(s.Main, now, learn: false);
                    Count(now, d => d with { Errors = d.Errors + 1, WorkSeconds = d.WorkSeconds + failedTurn });
                    CloseRunning(s.Main, ActionStatus.Failed, now);
                    s.Main.SetPhase(AgentPhase.Failed, now);
                    s.Main.Detail = ToolSummary.Shorten(e.Error ?? "Fehler");
                    break;

                case "Notification":
                    if (e.NotificationType == "permission_prompt")
                    {
                        agent.SetPhase(AgentPhase.Waiting, now);
                        agent.Detail = "wartet auf Freigabe";
                        _signals.Enqueue(MonitorSignal.NeedsYou);
                        Count(now, d => d with { Waits = d.Waits + 1 });
                    }
                    else if (e.NotificationType == "idle_prompt")
                    {
                        s.Main.SetPhase(AgentPhase.Idle, now);
                    }
                    break;

                case "TaskCreated":
                    agent.TasksTotal++;
                    break;

                case "TaskCompleted":
                    // Wurde die Aufgabe vor dem Start des Monitors angelegt, kennt er sie nicht: Gesamtzahl mitziehen.
                    agent.TasksDone++;
                    if (agent.TasksDone > agent.TasksTotal) agent.TasksTotal = agent.TasksDone;
                    _signals.Enqueue(MonitorSignal.TaskDone);
                    break;

                case "PreCompact":
                    agent.Detail = "fasst Verlauf zusammen";
                    break;

                case "PostCompact":
                    agent.Detail = null;
                    break;

                case "SessionEnd":
                    EndTurn(s.Main, now, learn: false);
                    CloseRunning(s.Main, ActionStatus.Done, now);
                    foreach (var a in s.Subagents.Values)
                    {
                        EndTurn(a, now, learn: false);
                        CloseRunning(a, ActionStatus.Done, now);
                    }
                    s.Main.SetPhase(AgentPhase.Idle, now);
                    s.Ended = true;
                    break;
            }

            // Ausgeblendete Sitzung kommt erst zurueck, wenn sie wieder arbeitet. Ein spaetes „idle_prompt“
            // oder die Statuszeile (ApplyStatus) holen sie bewusst nicht zurueck.
            if (s.Hidden && s.IsBusy) s.Hidden = false;

            Interlocked.Increment(ref _version);
        }
    }

    /// <summary>
    /// Knopf „Aufräumen“: entfernt fertige Aktionen, fertige Unteragenten und beendete Sitzungen und blendet
    /// untaetige Sitzungen aus, bis sie wieder arbeiten. Laufende Aktionen, Aufgaben- und Rundenbalken
    /// arbeitender Agenten, gelernte Dauern und Tageszahlen bleiben.
    /// </summary>
    /// <returns>Anzahl entfernter Aktionen (fuer die Rueckmeldung).</returns>
    public int ClearFinished(DateTimeOffset now)
    {
        lock (_lock)
        {
            var removed = 0;
            foreach (var id in _sessions.Keys.ToList()) removed += ClearSessionCore(id, hide: false);
            Interlocked.Increment(ref _version);
            return removed;
        }
    }

    /// <summary>
    /// Rechtsklick „Sitzung ausblenden“: raeumt die Sitzung auf und blendet sie aus, auch wenn sie
    /// scheinbar arbeitet (z. B. haengengeblieben). Sie erscheint wieder, sobald sie wirklich weiterarbeitet.
    /// </summary>
    public void HideSession(string sessionId)
    {
        lock (_lock)
        {
            ClearSessionCore(sessionId, hide: true);
            Interlocked.Increment(ref _version);
        }
    }

    private int ClearSessionCore(string id, bool hide)
    {
        if (!_sessions.TryGetValue(id, out var s)) return 0;
        if (s.Ended)
        {
            var n = s.Main.Actions.Count + s.Subagents.Values.Sum(a => a.Actions.Count);
            _sessions.Remove(id);
            return n;
        }

        var removed = 0;
        foreach (var a in new[] { s.Main }.Concat(s.Subagents.Values))
            removed += a.Actions.RemoveAll(x => x.Status != ActionStatus.Running);
        foreach (var (aid, a) in s.Subagents.ToList())
        {
            if (!a.IsBusy) s.Subagents.Remove(aid);
        }
        if (!s.Main.IsBusy)
        {
            s.Main.LastMessage = null;
            if (s.Main.Phase == AgentPhase.Failed)
            {
                s.Main.SetPhase(AgentPhase.Idle, s.LastEvent);
                s.Main.Detail = null;
            }
            if (s.Main.TasksTotal > 0 && s.Main.TasksDone >= s.Main.TasksTotal) s.Main.TasksDone = s.Main.TasksTotal = 0;
        }
        if (hide || !s.IsBusy) s.Hidden = true;
        return removed;
    }

    /// <summary>Entfernt beendete und lange untaetige Sitzungen sowie alte fertige Unteragenten.</summary>
    public void Prune(DateTimeOffset now)
    {
        lock (_lock)
        {
            var changed = false;
            foreach (var (id, s) in _sessions.ToList())
            {
                var age = now - s.LastEvent;
                if ((s.Ended && age > EndedRetention) || (!s.IsBusy && age > IdleRetention))
                {
                    _sessions.Remove(id);
                    changed = true;
                    continue;
                }
                foreach (var (aid, a) in s.Subagents.ToList())
                {
                    if (a.Ended is { } end && now - end > DoneAgentRetention)
                    {
                        s.Subagents.Remove(aid);
                        changed = true;
                    }
                }
                if (FinishedRetention is { } keep)
                {
                    foreach (var a in new[] { s.Main }.Concat(s.Subagents.Values))
                    {
                        if (a.Actions.RemoveAll(x => x.Status != ActionStatus.Running && x.End is { } e && now - e > keep) > 0)
                            changed = true;
                    }
                }
            }
            if (changed) Interlocked.Increment(ref _version);
        }
    }

    /// <summary>
    /// Sichtbare Sitzungen: ausgeblendete fehlen. Wartende zuerst (Freigabe nicht uebersehen),
    /// dann arbeitende, dann nach letztem Ereignis.
    /// </summary>
    public IReadOnlyList<SessionView> Snapshot()
    {
        lock (_lock)
        {
            return _sessions.Values
                .Where(s => !s.Hidden)
                .OrderByDescending(s => s.IsWaiting)
                .ThenByDescending(s => s.IsBusy)
                .ThenByDescending(s => s.LastEvent)
                .Select(s => new SessionView(
                    s.Id, s.Project, s.Title, s.Ended, s.LastEvent,
                    new[] { s.Main }.Concat(s.Subagents.Values.OrderBy(a => a.Started)).Select(ToView).ToList(),
                    s.Model, s.ModelFamily))
                .ToList();
        }
    }

    // ---------------------------------------------------------------- intern

    private Session GetSession(HookEvent e, DateTimeOffset now)
    {
        if (!_sessions.TryGetValue(e.SessionId, out var s))
        {
            s = new Session(e.SessionId, ProjectName(e.Cwd), now);
            _sessions[e.SessionId] = s;
        }
        else if (s.Project == "?" && e.Cwd is not null)
        {
            s.Project = ProjectName(e.Cwd);
        }
        return s;
    }

    private static Agent GetSubagent(Session s, string agentId, string? agentType, DateTimeOffset now)
    {
        if (!s.Subagents.TryGetValue(agentId, out var a))
        {
            var label = agentType ?? "Unteragent";
            // Beschreibung aus dem passenden, noch offenen Agent-Aufruf des Hauptagenten uebernehmen.
            var pending = s.PendingAgentCalls.FirstOrDefault(p =>
                string.Equals(p.Type, agentType, StringComparison.OrdinalIgnoreCase)) ?? s.PendingAgentCalls.FirstOrDefault();
            if (pending is not null)
            {
                s.PendingAgentCalls.Remove(pending);
                if (!string.IsNullOrWhiteSpace(pending.Description)) label = $"{label}: {pending.Description}";
            }
            a = new Agent(agentId, label, isMain: false, now) { AgentType = agentType };
            s.Subagents[agentId] = a;
        }
        else if (agentType is not null && a.Label == "Unteragent")
        {
            a.Label = agentType;
        }
        return a;
    }

    private void StartAction(Session s, Agent agent, HookEvent e, DateTimeOffset now)
    {
        var id = e.ToolUseId ?? $"{e.ToolName}-{now.ToUnixTimeMilliseconds()}";
        var key = DurationEstimator.Key(e.ToolName, e.ToolInput);
        agent.Actions.Add(new ActionItem
        {
            EstimateKey = key,
            EstimateSeconds = Estimator.EstimateWithFallback(key),
            LearnedSeconds = Estimator.Estimate(key),
            FullDetail = ToolSummary.FullText(e.ToolName, e.ToolInput),
            Id = id,
            Tool = ToolSummary.DisplayName(e.ToolName),
            Summary = ToolSummary.Describe(e.ToolName, e.ToolInput),
            Detail = ToolSummary.Detail(e.ToolName, e.ToolInput),
            Status = ActionStatus.Running,
            Start = now,
        });
        if (agent.Actions.Count > MaxActionsPerAgent)
            agent.Actions.RemoveRange(0, agent.Actions.Count - MaxActionsPerAgent);

        agent.SetPhase(AgentPhase.Working, now);
        agent.Detail = null;

        // Runde verpasst (z. B. Monitor mitten in der Arbeit neu gestartet): ab jetzt zaehlen, aber nicht daraus lernen.
        if (agent.TurnStarted is null)
        {
            agent.TurnStarted = now;
            agent.TurnPartial = true;
        }

        if (e.ToolName == "TodoWrite" && ToolSummary.TodoProgress(e.ToolInput) is { } p)
        {
            agent.TasksDone = p.Done;
            agent.TasksTotal = p.Total;
        }

        if (e.ToolName is "Agent" or "Task" && e.ToolInput is { } ti)
        {
            string? Get(string n) => ti.ValueKind == System.Text.Json.JsonValueKind.Object &&
                                     ti.TryGetProperty(n, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
                ? v.GetString() : null;
            s.PendingAgentCalls.Add(new PendingAgentCall(Get("subagent_type") ?? "general-purpose", Get("description")));
            if (s.PendingAgentCalls.Count > 20) s.PendingAgentCalls.RemoveAt(0);
        }
    }

    /// <returns>true, wenn eine laufende Aktion damit beendet wurde.</returns>
    private static bool FinishAction(Agent agent, HookEvent e, ActionStatus status, DateTimeOffset now, DurationEstimator estimator)
    {
        var item = e.ToolUseId is { } id
            ? agent.Actions.FindLast(a => a.Id == id)
            : agent.Actions.FindLast(a => a.Status == ActionStatus.Running && a.Tool == ToolSummary.DisplayName(e.ToolName));
        var finished = item is { Status: ActionStatus.Running };
        if (item is { Status: ActionStatus.Running })
        {
            // Lernen aus der Zeit, die der Balken tatsaechlich abbildet (Anfordern bis Ende).
            if (status == ActionStatus.Done) estimator.Record(item.EstimateKey, (now - item.Start).TotalSeconds);
            item.Status = status;
            item.End = e.DurationMs is { } ms && ms >= 0 ? item.Start.AddMilliseconds(ms) : now;
            if (item.End > now) item.End = now;
        }

        if (!agent.Actions.Any(a => a.Status == ActionStatus.Running) && agent.Phase is AgentPhase.Working or AgentPhase.Waiting)
        {
            agent.SetPhase(AgentPhase.Thinking, now);
            agent.Detail = null;
        }
        return finished;
    }

    /// <summary>Beendet die laufende Runde eines Agenten; bei normalem Ende lernt die Schaetzung ihre Dauer.</summary>
    /// <returns>Dauer der Runde in Sekunden (0, wenn keine lief).</returns>
    private double EndTurn(Agent agent, DateTimeOffset now, bool learn)
    {
        if (agent.TurnStarted is not { } start) return 0;
        var seconds = Math.Max(0, (now - start).TotalSeconds);
        if (learn && !agent.TurnPartial) Estimator.Record(agent.TurnKey, seconds);
        agent.TurnStarted = null;
        agent.TurnPartial = false;
        return seconds;
    }

    private static void CloseRunning(Agent agent, ActionStatus status, DateTimeOffset now)
    {
        foreach (var a in agent.Actions.Where(a => a.Status == ActionStatus.Running))
        {
            a.Status = status;
            a.End = now;
        }
    }

    private AgentView ToView(Agent a) => new(
        a.Id, a.Label, a.IsMain, a.Phase, a.PhaseSince, a.TasksDone, a.TasksTotal, a.Detail,
        a.Actions.Select(x => new ActionView(x.Id, x.Tool, x.Summary, x.Status, x.Start, x.End, x.Detail, x.EstimateSeconds,
            x.FullDetail, x.LearnedSeconds)).ToList(),
        a.LastMessage, a.TurnStarted, a.TurnStarted is null ? null : Estimator.EstimateWithFallback(a.TurnKey));

    internal static string ProjectName(string? cwd)
    {
        if (string.IsNullOrWhiteSpace(cwd)) return "?";
        var p = cwd.Replace('\\', '/').TrimEnd('/');
        var idx = p.LastIndexOf('/');
        return idx >= 0 ? p[(idx + 1)..] : p;
    }

    private sealed class Session(string id, string project, DateTimeOffset now)
    {
        public string Id { get; } = id;
        public string Project { get; set; } = project;
        public string? Title { get; set; }
        public string? Model { get; set; }
        public string? ModelFamily { get; set; }
        public bool Ended { get; set; }
        /// <summary>Per „Aufräumen“ oder Rechtsklick ausgeblendet, bis die Sitzung wieder arbeitet.</summary>
        public bool Hidden { get; set; }
        public DateTimeOffset LastEvent { get; set; } = now;
        public Agent Main { get; } = new(MainAgentId, "Hauptagent", isMain: true, now);
        public Dictionary<string, Agent> Subagents { get; } = new();
        public List<PendingAgentCall> PendingAgentCalls { get; } = new();

        public bool IsBusy => !Ended && (Main.IsBusy || Subagents.Values.Any(a => a.IsBusy));
        public bool IsWaiting => !Ended && (Main.Phase == AgentPhase.Waiting || Subagents.Values.Any(a => a.Phase == AgentPhase.Waiting));
    }

    private sealed record PendingAgentCall(string Type, string? Description);

    private sealed class Agent(string id, string label, bool isMain, DateTimeOffset now)
    {
        public string Id { get; } = id;
        public string Label { get; set; } = label;
        public bool IsMain { get; } = isMain;
        public AgentPhase Phase { get; private set; } = isMain ? AgentPhase.Idle : AgentPhase.Thinking;
        public DateTimeOffset PhaseSince { get; private set; } = now;
        public DateTimeOffset Started { get; } = now;
        public DateTimeOffset? Ended { get; set; }
        public string? AgentType { get; init; }
        /// <summary>Beginn der laufenden Runde (Auftrag bzw. Start des Unteragenten), sonst null.</summary>
        public DateTimeOffset? TurnStarted { get; set; }
        /// <summary>Beginn der Runde wurde verpasst; ihre Dauer taugt nicht zum Lernen.</summary>
        public bool TurnPartial { get; set; }
        /// <summary>Art der Runde fuer die Dauer-Schaetzung: „Runde:main“ oder „Runde:&lt;Typ&gt;“.</summary>
        public string TurnKey => IsMain ? "Runde:main" : $"Runde:{AgentType ?? "Unteragent"}";
        public string? Detail { get; set; }
        public string? LastMessage { get; set; }
        public int TasksDone { get; set; }
        public int TasksTotal { get; set; }
        public List<ActionItem> Actions { get; } = new();

        public bool IsBusy => Phase is AgentPhase.Thinking or AgentPhase.Working or AgentPhase.Waiting;

        public void SetPhase(AgentPhase phase, DateTimeOffset now)
        {
            if (Phase == phase) return;
            Phase = phase;
            PhaseSince = now;
        }
    }

    private sealed class ActionItem
    {
        public required string Id { get; init; }
        public required string Tool { get; init; }
        public required string Summary { get; init; }
        public string? Detail { get; init; }
        public ActionStatus Status { get; set; }
        public DateTimeOffset Start { get; init; }
        public DateTimeOffset? End { get; set; }
        public string EstimateKey { get; init; } = "";
        public double? EstimateSeconds { get; init; }
        public double? LearnedSeconds { get; init; }
        public string? FullDetail { get; init; }
    }
}
