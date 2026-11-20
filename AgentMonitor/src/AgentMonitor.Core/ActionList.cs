namespace AgentMonitor.Core;

/// <summary>Wie fertige Aktionen angezeigt werden (Tray „Fertige Aktionen“).</summary>
public enum FinishedDisplay
{
    /// <summary>Jede fertige Aktion einzeln (bis zur Hoechstzahl je Agent).</summary>
    All,
    /// <summary>Die letzten zwei fertigen einzeln, die uebrigen in einer Zeile zusammengefasst (Standard).</summary>
    Compact,
    /// <summary>Nur laufende Aktionen einzeln, alle fertigen in einer Zeile.</summary>
    RunningOnly,
}

/// <summary>Zusammengefasste fertige Aktionen: „✓ 5 erledigt · 1 Fehler · in 42 s“.</summary>
/// <param name="Span">Wanduhrzeit vom ersten Beginn bis zum letzten Ende (parallele Aktionen zaehlen nicht doppelt).</param>
public sealed record ActionDigest(int Done, int Failed, TimeSpan Span)
{
    public int Count => Done + Failed;
}

public static class ActionList
{
    /// <summary>So viele fertige Aktionen bleiben im Modus <see cref="FinishedDisplay.Compact"/> einzeln sichtbar.</summary>
    public const int CompactKeep = 2;

    /// <summary>
    /// Teilt die Aktionen eines Agenten (aelteste zuerst) in die einzeln angezeigten (neueste oben) und die
    /// zusammengefassten. Laufende Aktionen werden nie zusammengefasst.
    /// </summary>
    public static (IReadOnlyList<ActionView> Visible, ActionDigest? Digest) Arrange(
        IReadOnlyList<ActionView> actions, FinishedDisplay mode, int maxVisible)
    {
        var running = actions.Where(a => a.Status == ActionStatus.Running).ToList();
        var finished = actions.Where(a => a.Status != ActionStatus.Running).Reverse().ToList(); // neueste zuerst

        var keep = mode switch
        {
            FinishedDisplay.RunningOnly => 0,
            FinishedDisplay.Compact => Math.Min(CompactKeep, maxVisible),
            _ => maxVisible,
        };
        keep = Math.Max(0, Math.Min(keep, maxVisible - running.Count));

        var shown = finished.Take(keep).ToList();
        var rest = finished.Skip(keep).ToList();

        var visible = running.Concat(shown)
            .OrderByDescending(a => a.Start)
            .ThenByDescending(a => a.Id, StringComparer.Ordinal)
            .ToList();

        ActionDigest? digest = null;
        if (rest.Count > 0)
        {
            var from = rest.Min(a => a.Start);
            var to = rest.Max(a => a.End ?? a.Start);
            digest = new ActionDigest(
                rest.Count(a => a.Status == ActionStatus.Done),
                rest.Count(a => a.Status == ActionStatus.Failed),
                to > from ? to - from : TimeSpan.Zero);
        }
        return (visible, digest);
    }
}
