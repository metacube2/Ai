using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        var state = new MonitorState { Estimator = MonitorSettings.LoadEstimator() };
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

        popup.Click += (_, _) => _window?.SetMode(DisplayMode.Popup);
        pinned.Click += (_, _) => _window?.SetMode(DisplayMode.Pinned);
        topmost.CheckedChanged += (_, _) => _window?.SetTopmost(topmost.Checked);
        autostart.CheckedChanged += (_, _) => SetAutostart(autostart.Checked);
        menu.Opening += (_, _) =>
        {
            popup.Checked = _settings.Mode == DisplayMode.Popup;
            pinned.Checked = _settings.Mode == DisplayMode.Pinned;
        };

        menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            show, new Forms.ToolStripSeparator(), popup, pinned, size, topmost, autostart, new Forms.ToolStripSeparator(), exit,
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
    }

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
        if (_state is not null) MonitorSettings.SaveEstimator(_state.Estimator);
        _mutex?.ReleaseMutex();
        Shutdown();
    }
}
