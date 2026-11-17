using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using AgentMonitor.Core;
using Microsoft.Win32;

namespace AgentMonitor.App;

/// <summary>
/// Eigene Windows-Klangereignisse. Sie erscheinen in der Systemsteuerung unter Sound › Sounds beim
/// Programm „Agenten-Monitor“ und lassen sich dort wie jeder Windows-Klang belegen oder abschalten.
/// Beim ersten Start werden sinnvolle Vorgaben eingetragen; spaetere Wahl des Benutzers wird nie ueberschrieben.
/// </summary>
public static class SoundEvents
{
    /// <summary>Muss dem Namen der .exe ohne Endung entsprechen (SND_APPLICATION).</summary>
    private const string AppKey = "AgentMonitor";

    private sealed record Def(MonitorSignal Signal, string Name, string Label, string? DefaultWav);

    private static readonly Def[] Defs =
    [
        new(MonitorSignal.AgentDone, "AgentMonitorFertig", "Agent fertig", "Windows Notify System Generic.wav"),
        new(MonitorSignal.NeedsYou, "AgentMonitorWartet", "Agent wartet auf dich", "Windows Notify Messaging.wav"),
        new(MonitorSignal.Failed, "AgentMonitorFehler", "Agent mit Fehler abgebrochen", "Windows Critical Stop.wav"),
        // Standardmaessig still, sonst klingelt es bei langen Aufgabenlisten staendig.
        new(MonitorSignal.TaskDone, "AgentMonitorAufgabe", "Aufgabe erledigt", null),
    ];

    private static readonly Dictionary<MonitorSignal, DateTime> LastPlayed = new();

    /// <summary>Traegt Programm und Ereignisse in der Registry ein, falls sie fehlen.</summary>
    public static void Register()
    {
        try
        {
            using (var app = Registry.CurrentUser.CreateSubKey($@"AppEvents\Schemes\Apps\{AppKey}"))
                app.SetValue("", "Agenten-Monitor");

            var media = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");
            foreach (var d in Defs)
            {
                using (var label = Registry.CurrentUser.CreateSubKey($@"AppEvents\EventLabels\{d.Name}"))
                    label.SetValue("", d.Label);

                var wav = d.DefaultWav is null ? "" : Path.Combine(media, d.DefaultWav);
                if (wav.Length > 0 && !File.Exists(wav)) wav = "";

                using var ev = Registry.CurrentUser.CreateSubKey($@"AppEvents\Schemes\Apps\{AppKey}\{d.Name}");
                using (var def = ev.CreateSubKey(".Default")) def.SetValue("", wav);
                using var cur = ev.CreateSubKey(".Current");
                // Nur setzen, wenn noch nie etwas eingetragen wurde (Wahl des Benutzers bleibt).
                if (cur.GetValue("") is null) cur.SetValue("", wav);
            }
        }
        catch (Exception)
        {
            // Klaenge sind Komfort.
        }
    }

    /// <summary>Spielt den Klang zum Anlass, hoechstens einmal je 2 s und Anlass.</summary>
    public static void Play(MonitorSignal signal)
    {
        var now = DateTime.UtcNow;
        if (LastPlayed.TryGetValue(signal, out var last) && now - last < TimeSpan.FromSeconds(2)) return;
        LastPlayed[signal] = now;

        foreach (var d in Defs)
        {
            if (d.Signal != signal) continue;
            // SND_NODEFAULT: Ist das Ereignis in der Systemsteuerung auf „(Kein)“ gestellt, bleibt es still.
            PlaySound(d.Name, IntPtr.Zero, SND_ALIAS | SND_APPLICATION | SND_ASYNC | SND_NODEFAULT);
            return;
        }
    }

    private const uint SND_ASYNC = 0x0001, SND_NODEFAULT = 0x0002, SND_ALIAS = 0x00010000, SND_APPLICATION = 0x0080;

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);
}
