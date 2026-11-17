using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AgentMonitor.Core;

namespace AgentMonitor.App;

public partial class MainWindow : Window
{
    private readonly MonitorState _state;
    private readonly HookServer _server;
    private readonly MonitorSettings _settings;
    private readonly ObservableCollection<SessionVm> _sessions = new();
    private readonly DispatcherTimer _timer;

    private bool _wasBusy;
    private DateTimeOffset _lastBusy = DateTimeOffset.MinValue;
    /// <summary>Hat der Mensch das Fenster waehrend einer Arbeitsphase weggeklickt, bleibt es bis zur naechsten zu.</summary>
    private bool _suppressedUntilIdle;
    private DateTimeOffset _lastPrune = DateTimeOffset.MinValue;

    public bool AllowClose { get; set; }

    public MainWindow(MonitorState state, HookServer server, MonitorSettings settings)
    {
        _state = state;
        _server = server;
        _settings = settings;
        InitializeComponent();

        SessionList.ItemsSource = _sessions;
        Width = settings.Width;
        Height = settings.Height;
        Topmost = settings.Topmost;
        ApplyScale(settings.Scale, save: false);
        PlaceWindow();
        UpdatePinButton();

        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => Tick(), Dispatcher);
        _timer.Start();
        Tick();
    }

    // ------------------------------------------------------------------ Ablauf

    private void Tick()
    {
        var now = DateTimeOffset.Now;
        if (now - _lastPrune > TimeSpan.FromSeconds(15))
        {
            _state.Prune(now);
            _lastPrune = now;
            // Gelernte Dauern sichern (auch wenn der Monitor spaeter hart beendet wird).
            if (_state.Estimator.IsDirty) MonitorSettings.SaveEstimator(_state.Estimator);
        }

        // Auch ohne neues Ereignis abgleichen, damit Laufzeiten weiterzaehlen.
        var snapshot = _state.Snapshot();
        CollectionSync.Sync(_sessions, snapshot, s => s.Id, vm => vm.Id, s => new SessionVm(s), (vm, s) => vm.Update(s, now));

        var busyCount = snapshot.Count(s => s.IsBusy);
        var busy = busyCount > 0;
        EmptyHint.Visibility = snapshot.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HeaderInfo.Text = snapshot.Count == 0 ? "" : $"{busyCount} aktiv · {snapshot.Count} Sitzung{(snapshot.Count == 1 ? "" : "en")}";
        ActivityDot.Fill = (Brush)FindResource(busy ? "Working" : "Idle");
        Footer.Text = FooterText(now);

        UpdatePlan(now);
        if (_settings.Sounds)
            foreach (var signal in _state.DrainSignals()) SoundEvents.Play(signal);
        else
            _state.DrainSignals();

        if (busy) _lastBusy = now;
        if (busy && !_wasBusy) OnBecameBusy();
        if (!busy && _wasBusy) _suppressedUntilIdle = false;
        _wasBusy = busy;

        // Minimiert bleibt das Fenster in der Taskleiste, statt sich auszublenden.
        if (_settings.Mode == DisplayMode.Popup && !busy && IsVisible && !IsMouseOver && WindowState != WindowState.Minimized &&
            now - _lastBusy > TimeSpan.FromSeconds(Math.Max(2, _settings.AutoHideSeconds)))
        {
            Hide();
        }
    }

    // ------------------------------------------------------------------ Planverbrauch und Tokens

    private readonly TokenCounter _tokens = new(TokenCounter.DefaultRoot);
    private DateTimeOffset _lastTokenRefresh = DateTimeOffset.MinValue;
    private bool _tokenRefreshRunning;
    private DateTimeOffset _savedPlanAt;

    private void UpdatePlan(DateTimeOffset now)
    {
        var plan = _state.Plan;
        var any = plan is { FiveHour: not null } or { SevenDay: not null };

        // Letzten Stand sichern, damit er nach einem Neustart sofort wieder da ist.
        if (plan is not null && plan.UpdatedAt != _savedPlanAt)
        {
            _savedPlanAt = plan.UpdatedAt;
            MonitorSettings.SavePlan(plan);
        }
        // Aelter als 2 Minuten: Stand dazuschreiben, damit klar ist, dass es nicht live ist.
        PlanAge.Text = plan is not null && now - plan.UpdatedAt > TimeSpan.FromMinutes(2)
            ? $"Stand von {ResetText(plan.UpdatedAt, now).Replace("um ", "")}"
            : "";
        PlanHint.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
        PlanRows.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
        if (any)
        {
            SetUsage(FiveHourBar, FiveHourReset, plan!.FiveHour, now);
            SetUsage(WeekBar, WeekReset, plan.SevenDay, now);
        }

        // Tokens alle 10 s im Hintergrund nachzaehlen (Dateien lesen soll die Oberflaeche nicht bremsen).
        if (!_tokenRefreshRunning && now - _lastTokenRefresh > TimeSpan.FromSeconds(10))
        {
            _tokenRefreshRunning = true;
            _lastTokenRefresh = now;
            System.Threading.Tasks.Task.Run(() =>
            {
                try { _tokens.Refresh(DateTimeOffset.Now); } catch (Exception) { }
            }).ContinueWith(_ => Dispatcher.BeginInvoke(() =>
            {
                _tokenRefreshRunning = false;
                var t = _tokens.Today;
                TokensToday.Text = t.Total == 0
                    ? "Tokens heute: noch keine"
                    : $"Tokens heute: {Tokens(t.Total)}  (Eingabe {Tokens(t.Input)} · Ausgabe {Tokens(t.Output)} · " +
                      $"Cache schreiben {Tokens(t.CacheWrite)} · Cache lesen {Tokens(t.CacheRead)})";
            }));
        }
    }

    private void SetUsage(System.Windows.Controls.ProgressBar bar, System.Windows.Controls.TextBlock reset, UsageWindow? w, DateTimeOffset now)
    {
        if (w is null)
        {
            bar.Value = 0;
            bar.Opacity = 0.4;
            reset.Text = "keine Angabe";
            return;
        }
        bar.Opacity = 1;
        bar.Value = w.UsedPercent;
        // Gruen unter 70 %, orange ab 70 %, rot ab 90 % (Wunsch Ingo).
        bar.Foreground = (Brush)FindResource(w.UsedPercent >= 90 ? "Failed" : w.UsedPercent >= 70 ? "Waiting" : "Done");
        reset.Text = w.ResetsAt is { } r ? "erneuert " + ResetText(r, now) : "";
    }

    internal static string ResetText(DateTimeOffset reset, DateTimeOffset now)
    {
        var local = reset.ToLocalTime();
        if (local.Date == now.LocalDateTime.Date) return $"um {local:HH:mm}";
        var day = local.ToString("ddd", new System.Globalization.CultureInfo("de-CH")).TrimEnd('.');
        return $"{day} {local:dd.MM. HH:mm}";
    }

    internal static string Tokens(long n) => n switch
    {
        >= 1_000_000_000 => $"{n / 1e9:0.0} Mrd.",
        >= 1_000_000 => $"{n / 1e6:0.0} Mio.",
        >= 1_000 => $"{n / 1e3:0} Tsd.",
        _ => n.ToString(),
    };

    private void OnBecameBusy()
    {
        // Ohne den Fokus zu stehlen: ShowActivated=False im XAML. Ein von Hand minimiertes Fenster bleibt minimiert.
        if (_settings.Mode == DisplayMode.Popup && !_suppressedUntilIdle && !IsVisible) Show();
    }

    private string FooterText(DateTimeOffset now)
    {
        if (_server.StartError is { } err)
            return $"Empfang gestört (Port {_settings.Port}): {err}";
        var last = _server.LastEvent is { } t ? $"letztes Ereignis vor {Format.Duration(now - t)}" : "noch kein Ereignis";
        var mode = _settings.Mode == DisplayMode.Popup ? "Aufpoppen" : "Immer offen";
        return $"127.0.0.1:{_settings.Port} · {_server.EventCount} Ereignisse · {last} · {mode}";
    }

    // ------------------------------------------------------------------ Bedienung

    public void ShowFromTray()
    {
        _suppressedUntilIdle = false;
        _lastBusy = DateTimeOffset.Now; // nicht sofort wieder ausblenden
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }

    public void SetMode(DisplayMode mode)
    {
        _settings.Mode = mode;
        _settings.Save();
        UpdatePinButton();
        if (mode == DisplayMode.Pinned) ShowFromTray();
    }

    /// <summary>Setzt die Vergroesserung und passt die Fenstergroesse im gleichen Verhaeltnis an.</summary>
    public void ApplyScale(double scale, bool save = true)
    {
        scale = Math.Round(Math.Clamp(scale, MonitorSettings.MinScale, MonitorSettings.MaxScale), 2);
        var old = Zoom.ScaleX;
        Zoom.ScaleX = Zoom.ScaleY = scale;
        if (save && old > 0 && Math.Abs(old - scale) > 0.001)
        {
            var f = scale / old;
            var area = SystemParameters.WorkArea;
            Width = Math.Min(Width * f, area.Width);
            Height = Math.Min(Height * f, area.Height);
            if (Left + Width > area.Right) Left = Math.Max(area.Left, area.Right - Width);
            if (Top + Height > area.Bottom) Top = Math.Max(area.Top, area.Bottom - Height);
        }
        _settings.Scale = scale;
        if (save) RememberPlacement();
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        ApplyScale(_settings.Scale + (e.Delta > 0 ? 0.1 : -0.1));
        e.Handled = true;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        switch (e.Key)
        {
            case Key.OemPlus or Key.Add: ApplyScale(_settings.Scale + 0.1); e.Handled = true; break;
            case Key.OemMinus or Key.Subtract: ApplyScale(_settings.Scale - 0.1); e.Handled = true; break;
            case Key.D0 or Key.NumPad0: ApplyScale(1.4); e.Handled = true; break;
        }
    }

    public void SetTopmost(bool value)
    {
        _settings.Topmost = value;
        Topmost = value;
        _settings.Save();
    }

    private void UpdatePinButton()
    {
        PinButton.Opacity = _settings.Mode == DisplayMode.Pinned ? 1.0 : 0.45;
        PinButton.ToolTip = _settings.Mode == DisplayMode.Pinned
            ? "Immer offen (klicken: Aufpoppen)"
            : "Aufpoppen (klicken: immer offen)";
    }

    private void PinButton_Click(object sender, RoutedEventArgs e) =>
        SetMode(_settings.Mode == DisplayMode.Pinned ? DisplayMode.Popup : DisplayMode.Pinned);

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wasBusy) _suppressedUntilIdle = true;
        Hide();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        if (_maximized) return; // Maximiert nicht verschieben, wie bei normalen Fenstern
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        RememberPlacement();
    }

    // ------------------------------------------------------------------ Minimieren / Maximieren
    // Das Fenster ist rahmenlos; Maximieren fuellt deshalb von Hand den Arbeitsbereich des Bildschirms,
    // auf dem es gerade steht (ohne die Taskleiste zu verdecken).

    private bool _maximized;
    private Rect _restoreBounds;

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void ToggleMaximize()
    {
        if (_maximized)
        {
            _maximized = false;
            Left = _restoreBounds.Left;
            Top = _restoreBounds.Top;
            Width = _restoreBounds.Width;
            Height = _restoreBounds.Height;
        }
        else
        {
            _restoreBounds = new Rect(Left, Top, Width, Height);
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var screen = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;
            var dpi = VisualTreeHelper.GetDpi(this);
            Left = screen.Left / dpi.DpiScaleX;
            Top = screen.Top / dpi.DpiScaleY;
            Width = screen.Width / dpi.DpiScaleX;
            Height = screen.Height / dpi.DpiScaleY;
            _maximized = true;
        }
        MaximizeButton.Content = _maximized ? "" : "";
        MaximizeButton.ToolTip = _maximized ? "Wiederherstellen" : "Maximieren";
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            if (_wasBusy) _suppressedUntilIdle = true;
            Hide();
            return;
        }
        RememberPlacement();
        _timer.Stop();
        base.OnClosing(e);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (IsLoaded) RememberPlacement();
    }

    private void RememberPlacement()
    {
        // Maximiert oder minimiert nicht merken, sonst startet der Monitor naechstes Mal bildschirmfuellend.
        if (_maximized || WindowState != WindowState.Normal) return;
        _settings.Left = Left;
        _settings.Top = Top;
        _settings.Width = ActualWidth > 0 ? ActualWidth : Width;
        _settings.Height = ActualHeight > 0 ? ActualHeight : Height;
        _settings.Save();
    }

    private void PlaceWindow()
    {
        if (_settings.Left is { } l && _settings.Top is { } t &&
            l >= SystemParameters.VirtualScreenLeft && t >= SystemParameters.VirtualScreenTop &&
            l + 80 <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth &&
            t + 40 <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
        {
            Left = l;
            Top = t;
            return;
        }
        // Standard: unten rechts im Arbeitsbereich.
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 12;
        Top = area.Bottom - Height - 12;
    }
}
