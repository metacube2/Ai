using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AgentMonitor.Core;

namespace AgentMonitor.App;

public abstract class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public static class Format
{
    public static string Duration(TimeSpan t)
    {
        if (t < TimeSpan.Zero) t = TimeSpan.Zero;
        if (t.TotalSeconds < 10) return $"{t.TotalSeconds:0.0} s";
        if (t.TotalSeconds < 60) return $"{(int)t.TotalSeconds} s";
        if (t.TotalMinutes < 60) return $"{(int)t.TotalMinutes} min {t.Seconds:00} s";
        return $"{(int)t.TotalHours} h {t.Minutes:00} min";
    }

    public static string PhaseText(AgentPhase p) => p switch
    {
        AgentPhase.Thinking => "denkt …",
        AgentPhase.Working => "arbeitet",
        AgentPhase.Waiting => "wartet auf dich",
        AgentPhase.Done => "fertig",
        AgentPhase.Failed => "Fehler",
        _ => "bereit",
    };

    /// <summary>Schluessel fuer die Farbe in MainWindow.xaml (Trigger auf PhaseKey).</summary>
    public static string PhaseKey(AgentPhase p) => p.ToString();
}

public static class CollectionSync
{
    /// <summary>
    /// Gleicht eine ObservableCollection mit einer Modell-Liste ab, ohne vorhandene Elemente neu
    /// anzulegen. So laufen Animationen (z. B. unbestimmte Fortschrittsbalken) ohne Flackern weiter.
    /// </summary>
    public static void Sync<TVm, TModel>(
        ObservableCollection<TVm> target, IReadOnlyList<TModel> models,
        Func<TModel, string> modelKey, Func<TVm, string> vmKey,
        Func<TModel, TVm> create, Action<TVm, TModel> update)
    {
        for (var i = 0; i < models.Count; i++)
        {
            var key = modelKey(models[i]);
            var existing = -1;
            for (var j = i; j < target.Count; j++)
            {
                if (vmKey(target[j]) == key) { existing = j; break; }
            }

            if (existing < 0)
            {
                target.Insert(i, create(models[i]));
            }
            else
            {
                if (existing != i) target.Move(existing, i);
                update(target[i], models[i]);
            }
        }
        while (target.Count > models.Count) target.RemoveAt(target.Count - 1);
    }
}

public sealed class SessionVm : Observable
{
    private string _title = "";
    private string _subtitle = "";
    private bool _isBusy;
    private bool _ended;

    public string Id { get; }
    public ObservableCollection<AgentVm> Agents { get; } = new();

    public string Title { get => _title; private set => Set(ref _title, value); }
    public string Subtitle { get => _subtitle; private set => Set(ref _subtitle, value); }
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value); }
    public bool Ended { get => _ended; private set => Set(ref _ended, value); }

    public SessionVm(SessionView s) { Id = s.Id; Update(s, DateTimeOffset.Now); }

    public void Update(SessionView s, DateTimeOffset now)
    {
        Title = s.Title is { Length: > 0 } t ? $"{s.Project} · {t}" : s.Project;
        Subtitle = s.Ended
            ? "Sitzung beendet"
            : $"Sitzung {s.Id[..Math.Min(8, s.Id.Length)]} · letztes Ereignis vor {Format.Duration(now - s.LastEvent)}";
        IsBusy = s.IsBusy;
        Ended = s.Ended;
        CollectionSync.Sync(Agents, s.Agents, a => a.Id, vm => vm.Id, a => new AgentVm(a),
            (vm, a) => vm.Update(a, now, s.Model, s.ModelFamily));
    }
}

public sealed class AgentVm : Observable
{
    /// <summary>So viele Aktionen zeigt der Hauptagent, Unteragenten weniger.</summary>
    private const int MainVisible = 6, SubVisible = 4;

    private string _label = "";
    private string _phaseText = "";
    private string _phaseKey = "";
    private string _elapsed = "";
    private string? _detail;
    private bool _hasProgress;
    private double _progress;
    private string _progressText = "";
    private int _hiddenActions;

    public string Id { get; }
    public bool IsMain { get; }
    public ObservableCollection<ActionVm> Actions { get; } = new();

    public string Label { get => _label; private set => Set(ref _label, value); }
    public string PhaseText { get => _phaseText; private set => Set(ref _phaseText, value); }
    public string PhaseKey { get => _phaseKey; private set => Set(ref _phaseKey, value); }
    public string Elapsed { get => _elapsed; private set => Set(ref _elapsed, value); }
    public string? Detail { get => _detail; private set => Set(ref _detail, value); }
    private string? _lastMessage;
    public string? LastMessage { get => _lastMessage; private set => Set(ref _lastMessage, value); }
    public bool HasProgress { get => _hasProgress; private set => Set(ref _hasProgress, value); }
    public double Progress { get => _progress; private set => Set(ref _progress, value); }
    public string ProgressText { get => _progressText; private set => Set(ref _progressText, value); }
    public int HiddenActions { get => _hiddenActions; private set => Set(ref _hiddenActions, value); }

    private string _progressTip = "";
    public string ProgressTip { get => _progressTip; private set => Set(ref _progressTip, value); }

    private bool _isFable;
    /// <summary>Fable-Sitzung: Aufgabenbalken dunkler gruen.</summary>
    public bool IsFable { get => _isFable; private set => Set(ref _isFable, value); }

    public AgentVm(AgentView a) { Id = a.Id; IsMain = a.IsMain; Update(a, DateTimeOffset.Now); }

    public void Update(AgentView a, DateTimeOffset now, string? model = null, string? family = null)
    {
        // Das Modell meldet die Statuszeile nur fuer die Hauptsitzung; Unteragenten koennen ein anderes haben.
        Label = a.IsMain ? (model is null ? "Hauptagent" : $"Hauptagent · {model}") : a.Label;
        IsFable = a.IsMain && family == "fable";
        PhaseText = Format.PhaseText(a.Phase);
        PhaseKey = Format.PhaseKey(a.Phase);
        Elapsed = a.Phase is AgentPhase.Idle ? "" : Format.Duration(now - a.PhaseSince);
        Detail = a.Detail;
        LastMessage = a.LastMessage is { } m && a.Phase is AgentPhase.Idle or AgentPhase.Done ? $"Ergebnis: {m}" : null;

        // Echte Aufgaben gehen vor. Ohne Aufgabenliste zeigt jeder arbeitende Agent eine geschaetzte Rundendauer
        // (gelernt wie bei den Aktionen), damit auch der Hauptagent einen Prozentbalken hat (Wunsch Ingo 17.11.).
        if (a.TasksTotal > 0)
        {
            HasProgress = true;
            Progress = 100.0 * a.TasksDone / a.TasksTotal;
            ProgressText = $"{a.TasksDone}/{a.TasksTotal} Aufgaben";
            ProgressTip = "Echter Fortschritt aus der Aufgabenliste des Agenten.";
        }
        else if (a.TurnStart is { } turnStart && a.TurnEstimateSeconds is { } est)
        {
            HasProgress = true;
            Progress = DurationEstimator.Percent(now - turnStart, est);
            ProgressText = "Runde (geschätzt)";
            ProgressTip = $"Geschätzt aus der Dauer früherer Runden (üblich etwa {Format.Duration(TimeSpan.FromSeconds(est))}); " +
                          "höchstens 95 %, bis der Agent wirklich fertig ist.";
        }
        else
        {
            HasProgress = false;
            Progress = 0;
            ProgressText = "";
            ProgressTip = "";
        }

        var max = IsMain ? MainVisible : SubVisible;
        var start = Math.Max(0, a.Actions.Count - max);
        HiddenActions = start;
        var visible = new List<ActionView>(a.Actions.Count - start);
        // Neueste oben.
        for (var i = a.Actions.Count - 1; i >= start; i--) visible.Add(a.Actions[i]);
        CollectionSync.Sync(Actions, visible, x => x.Id, vm => vm.Id, x => new ActionVm(x), (vm, x) => vm.Update(x, now));
    }
}

public sealed class ActionVm : Observable
{
    private string _summary = "";
    private string _duration = "";
    private string _statusKey = "";
    private string _glyph = "";
    private bool _isRunning;

    public string Id { get; }
    public string Tool { get; }

    public string Summary { get => _summary; private set => Set(ref _summary, value); }
    public string Duration { get => _duration; private set => Set(ref _duration, value); }
    public string StatusKey { get => _statusKey; private set => Set(ref _statusKey, value); }
    public string Glyph { get => _glyph; private set => Set(ref _glyph, value); }
    public bool IsRunning { get => _isRunning; private set => Set(ref _isRunning, value); }

    public string? Detail { get; }

    private double _barValue;
    private string _percentText = "";
    private bool _isIndeterminate;

    /// <summary>Balkenstand 0..100: laufend die Schaetzung, fertig 100.</summary>
    public double BarValue { get => _barValue; private set => Set(ref _barValue, value); }
    /// <summary>„≈ 40 %“ waehrend einer geschaetzten Aktion, sonst leer.</summary>
    public string PercentText { get => _percentText; private set => Set(ref _percentText, value); }
    /// <summary>Laufend, aber noch keine Erfahrungswerte: wandernder Lichtstreifen statt Prozent.</summary>
    public bool IsIndeterminate { get => _isIndeterminate; private set => Set(ref _isIndeterminate, value); }

    public ActionVm(ActionView a) { Id = a.Id; Tool = a.Tool; Detail = a.Detail; Update(a, DateTimeOffset.Now); }

    public void Update(ActionView a, DateTimeOffset now)
    {
        var running = a.Status == ActionStatus.Running;
        var estimate = running && a.EstimateSeconds is > 0 ? a.EstimateSeconds : null;
        IsIndeterminate = running && estimate is null;
        BarValue = !running ? 100 : estimate is { } est ? DurationEstimator.Percent(now - a.Start, est) : 0;
        PercentText = estimate is not null ? $"{BarValue:0} %" : "";

        Summary = a.Summary;
        Duration = Format.Duration((a.End ?? now) - a.Start);
        StatusKey = a.Status.ToString();
        IsRunning = a.Status == ActionStatus.Running;
        Glyph = a.Status switch
        {
            ActionStatus.Running => "⏳",
            ActionStatus.Done => "✓",
            _ => "✗",
        };
    }
}
