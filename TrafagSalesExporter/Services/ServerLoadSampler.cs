using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TrafagSalesExporter.Services;

/// <summary>
/// CPU-Last fuer die Pegelanzeige in der Kopfleiste (Wunsch Ingo 2026-10-14, Components/ServerLoadMeter.razor).
/// Server gesamt ueber GetSystemTimes (kernel32, keine Zusatzrechte fuer den App-Pool noetig), das Cockpit selbst ueber
/// die Prozesszeit. Ein gemeinsamer Takt fuer alle Besucher: gemessen wird hoechstens alle <see cref="Interval"/>,
/// egal wie viele Browser offen sind. Bewusst statisch, damit Program.cs unberuehrt bleibt.
/// </summary>
public static class ServerLoadSampler
{
    public static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(1500);

    private static readonly object Gate = new();
    private static DateTime _lastSample = DateTime.MinValue;
    private static ulong _idle, _kernel, _user;
    private static TimeSpan _procCpu;
    private static DateTime _procWall;
    private static (double? Machine, double App) _current = (null, 0);

    /// <summary>Juengster Messwert in Prozent (0-100). Machine ist null, wenn das Betriebssystem keinen Wert liefert.</summary>
    public static (double? Machine, double App) Current
    {
        get
        {
            lock (Gate)
            {
                if (DateTime.UtcNow - _lastSample >= Interval) Sample();
                return _current;
            }
        }
    }

    private static void Sample()
    {
        var now = DateTime.UtcNow;
        double? machine = null;
        if (OperatingSystem.IsWindows() && GetSystemTimes(out var idleFt, out var kernelFt, out var userFt))
        {
            ulong idle = Ticks(idleFt), kernel = Ticks(kernelFt), user = Ticks(userFt);
            if (_lastSample != DateTime.MinValue)
            {
                // Kernelzeit enthaelt die Leerlaufzeit
                var total = (kernel - _kernel) + (user - _user);
                var busy = total - (idle - _idle);
                machine = total == 0 ? 0 : Math.Clamp(100.0 * busy / total, 0, 100);
            }
            _idle = idle; _kernel = kernel; _user = user;
        }

        using var p = Process.GetCurrentProcess();
        var cpu = p.TotalProcessorTime;
        var app = 0.0;
        if (_procWall != default)
        {
            var wall = (now - _procWall).TotalMilliseconds * Environment.ProcessorCount;
            app = wall <= 0 ? 0 : Math.Clamp(100.0 * (cpu - _procCpu).TotalMilliseconds / wall, 0, 100);
        }
        _procCpu = cpu;
        _procWall = now;
        // Beim allerersten Aufruf gibt es noch keine Differenz: alten Wert behalten statt 0 anzuzeigen.
        if (_lastSample != DateTime.MinValue)
            _current = (machine ?? (OperatingSystem.IsWindows() ? _current.Machine : null), app);
        _lastSample = now;
    }

    private static ulong Ticks(FILETIME f) => ((ulong)(uint)f.High << 32) | (uint)f.Low;

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public int Low;
        public int High;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

    /// <summary>Anzahl leuchtender Segmente fuer einen Prozentwert (aufgerundet, ab dem ersten Prozent mindestens eines).</summary>
    public static int LitSegments(double percent, int segments)
        => percent <= 0 ? 0 : Math.Clamp((int)Math.Ceiling(percent / 100.0 * segments), 1, segments);
}
