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

        if (busy) _lastBusy = now;
        if (busy && !_wasBusy) OnBecameBusy();
        if (!busy && _wasBusy) _suppressedUntilIdle = false;
        _wasBusy = busy;

        if (_settings.Mode == DisplayMode.Popup && !busy && IsVisible && !IsMouseOver &&
            now - _lastBusy > TimeSpan.FromSeconds(Math.Max(2, _settings.AutoHideSeconds)))
        {
            Hide();
        }
    }

    private void OnBecameBusy()
    {
        // Ohne den Fokus zu stehlen: ShowActivated=False im XAML.
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
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        RememberPlacement();
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
