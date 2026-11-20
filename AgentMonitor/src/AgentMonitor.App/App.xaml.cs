using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows;
using AgentMonitor.Core;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace AgentMonitor.App;

public partial class App : System.Windows.Application
{
    private const string MutexName = "AgentMonitor-1C6F2B7E-Einzelinstanz";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "AgentMonitor";

    private Mutex? _mutex;
    private HookServer? _server;
    private MonitorState? _state;
    private MainWindow? _window;
    private Forms.NotifyIcon? _tray;
    private MonitorSettings _settings = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(initiallyOwned: true, MutexName, out var isNew);
        if (!isNew)
        {
            // Schon gestartet: still beenden, die laufende Instanz empfaengt weiter.
            Shutdown();
            return;
        }

        _settings = MonitorSettings.Load();
        if (!_settings.AutostartInitialized)
        {
            // Die Hooks melden Fehler, wenn der Monitor nicht laeuft; deshalb beim ersten Start Autostart ein.
            SetAutostart(true);
            _settings.AutostartInitialized = true;
            _settings.Save();
        }
        SoundEvents.Register();
        var state = new MonitorState { Estimator = MonitorSettings.LoadEstimator() };
        if (MonitorSettings.LoadPlan() is { } savedPlan) state.RestorePlan(savedPlan, DateTimeOffset.Now);
        if (MonitorSettings.LoadToday() is { } savedToday) state.RestoreToday(savedToday, DateTimeOffset.Now);
        _state = state;
        _server = new HookServer(state, _settings);
        await _server.StartAsync();

        _window = new MainWindow(state, _server, _settings);
        CreateTray();

        var startHidden = Array.Exists(e.Args, a => a.Equals("--hidden", StringComparison.OrdinalIgnoreCase));
        if (_settings.Mode == DisplayMode.Pinned || !startHidden) _window.ShowFromTray();
        if (_server.StartError is not null) _window.ShowFromTray();
    }

    private void CreateTray()
    {
        var menu = new Forms.ContextMenuStrip();
        var show = new Forms.ToolStripMenuItem("Anzeigen", null, (_, _) => _window?.ShowFromTray());
        var popup = new Forms.ToolStripMenuItem("Aufpoppen beim Denken");
        var pinned = new Forms.ToolStripMenuItem("Immer offen");
        var topmost = new Forms.ToolStripMenuItem("Im Vordergrund") { CheckOnClick = true, Checked = _settings.Topmost };
        var autostart = new Forms.ToolStripMenuItem("Beim Anmelden starten") { CheckOnClick = true, Checked = IsAutostart() };
        var exit = new Forms.ToolStripMenuItem("Beenden", null, (_, _) => ExitApp());
        var size = new Forms.ToolStripMenuItem("Grösse");
        foreach (var pct in new[] { 100, 125, 150, 175, 200, 250 })
        {
            var item = new Forms.ToolStripMenuItem($"{pct} %") { Tag = pct / 100.0 };
            item.Click += (_, _) => _window?.ApplyScale((double)item.Tag!);
            size.DropDownItems.Add(item);
        }
        size.DropDownOpening += (_, _) =>
        {
            foreach (Forms.ToolStripMenuItem i in size.DropDownItems)
                i.Checked = Math.Abs((double)i.Tag! - _settings.Scale) < 0.05;
        };

        var sounds = new Forms.ToolStripMenuItem("Klänge") { CheckOnClick = true, Checked = _settings.Sounds };
        sounds.CheckedChanged += (_, _) => { _settings.Sounds = sounds.Checked; _settings.Save(); };
        var soundPick = new Forms.ToolStripMenuItem("Klänge auswählen …", null, (_, _) =>
        {
            // Systemsteuerung › Sound › Reiter „Sounds“; dort steht das Programm „Agenten-Monitor“.
            try { Process.Start(new ProcessStartInfo("control.exe", "mmsys.cpl,,2") { UseShellExecute = true }); }
            catch (Exception) { }
        });

        // Tageszusammenfassung (nur Anzeige) und Aufraeumen (Wunsch Ingo 20.11.)
        var today = new Forms.ToolStripMenuItem("Heute") { Enabled = false };
        var clean = new Forms.ToolStripMenuItem("Erledigtes aufräumen", null, (_, _) => _window?.CleanUp());

        var finished = new Forms.ToolStripMenuItem("Fertige Aktionen");
        foreach (var (label, mode) in new[]
                 {
                     ("einzeln zeigen", FinishedDisplay.All),
                     ("zusammenfassen (letzte zwei einzeln)", FinishedDisplay.Compact),
                     ("nur laufende zeigen", FinishedDisplay.RunningOnly),
                 })
        {
            var item = new Forms.ToolStripMenuItem(label) { Tag = mode };
            item.Click += (_, _) => _window?.SetFinishedDisplay((FinishedDisplay)item.Tag!);
            finished.DropDownItems.Add(item);
        }
        finished.DropDownItems.Add(new Forms.ToolStripSeparator());
        foreach (var (label, minutes) in new[] { ("ausblenden nach 1 Minute", 1), ("ausblenden nach 2 Minuten", 2),
                     ("ausblenden nach 5 Minuten", 5), ("ausblenden nach 15 Minuten", 15), ("nie ausblenden", 0) })
        {
            var item = new Forms.ToolStripMenuItem(label) { Tag = minutes };
            item.Click += (_, _) => _window?.SetFinishedRetention((int)item.Tag!);
            finished.DropDownItems.Add(item);
        }
        finished.DropDownOpening += (_, _) =>
        {
            foreach (var i in finished.DropDownItems.OfType<Forms.ToolStripMenuItem>())
                i.Checked = i.Tag switch
                {
                    FinishedDisplay m => m == _settings.FinishedDisplay,
                    int min => min == _settings.FinishedRetentionMinutes,
                    _ => false,
                };
        };

        popup.Click += (_, _) => _window?.SetMode(DisplayMode.Popup);
        pinned.Click += (_, _) => _window?.SetMode(DisplayMode.Pinned);
        topmost.CheckedChanged += (_, _) => _window?.SetTopmost(topmost.Checked);
        autostart.CheckedChanged += (_, _) => SetAutostart(autostart.Checked);
        menu.Opening += (_, _) =>
        {
            popup.Checked = _settings.Mode == DisplayMode.Popup;
            pinned.Checked = _settings.Mode == DisplayMode.Pinned;
            today.Text = _window?.TodayText(DateTimeOffset.Now) ?? "Heute";
        };

        menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            today, new Forms.ToolStripSeparator(),
            show, clean, new Forms.ToolStripSeparator(), popup, pinned, size, topmost, finished, new Forms.ToolStripSeparator(),
            sounds, soundPick, new Forms.ToolStripSeparator(), autostart, new Forms.ToolStripSeparator(), exit,
        });

        // Menue selbst gut lesbar.
        menu.Font = new Font("Segoe UI", 11f);

        _tray = new Forms.NotifyIcon
        {
            Icon = MakeIcon(),
            Text = "Agenten-Monitor",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.MouseClick += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left) _window?.ShowFromTray();
        };

        // Kurzstand im Tooltip des Tray-Symbols (Windows erlaubt hoechstens 127 Zeichen).
        _trayTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _trayTimer.Tick += (_, _) =>
        {
            if (_tray is null || _window is null) return;
            var text = _window.StatusText.Length == 0 ? "Agenten-Monitor" : $"Agenten-Monitor · {_window.StatusText}";
            _tray.Text = text.Length <= 127 ? text : text[..127];
        };
        _trayTimer.Start();
    }

    private System.Windows.Threading.DispatcherTimer? _trayTimer;

    /// <summary>Ein schlichtes Tray-Symbol: violetter Punkt mit blauem Ring, ohne Bilddatei.</summary>
    private static Icon MakeIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var ring = new Pen(Color.FromArgb(0x60, 0xA5, 0xFA), 4);
            g.DrawEllipse(ring, 3, 3, 26, 26);
            using var dot = new SolidBrush(Color.FromArgb(0xA7, 0x8B, 0xFA));
            g.FillEllipse(dot, 10, 10, 12, 12);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    private static bool IsAutostart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(RunValue) is string;
    }

    private static void SetAutostart(bool on)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key is null) return;
        if (on)
        {
            var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (exe is not null) key.SetValue(RunValue, $"\"{exe}\" --hidden");
        }
        else
        {
            key.DeleteValue(RunValue, throwOnMissingValue: false);
        }
    }

    private async void ExitApp()
    {
        if (_window is not null)
        {
            _window.AllowClose = true;
            _window.Close();
        }
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        if (_server is not null) await _server.DisposeAsync();
        _trayTimer?.Stop();
        if (_state is not null)
        {
            MonitorSettings.SaveEstimator(_state.Estimator);
            MonitorSettings.SaveToday(_state.Today(DateTimeOffset.Now));
        }
        _mutex?.ReleaseMutex();
        Shutdown();
    }
}
