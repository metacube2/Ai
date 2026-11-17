using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using VideoDoku.Core;
using VideoDoku.Windows;

namespace VideoDoku.App;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly Ffmpeg _ffmpeg = new(Ffmpeg.DefaultBinFolder);
    private readonly WindowsVoiceProvider _windowsVoice = new();
    private IngokTtsClient? _service;
    private readonly ObservableCollection<SceneVm> _scenes = new();
    private DokuProject? _project;
    private string? _sourcePath;
    private CancellationTokenSource? _cts;
    private string? _lastVideo;

    public MainWindow()
    {
        InitializeComponent();
        // Nie groesser als der Bildschirm, auf dem das Fenster aufgeht (Taskleiste frei lassen).
        // SystemParameters.WorkArea gilt nur fuer den Hauptbildschirm; auf dem kleineren Laptop-Bildschirm
        // war der Bereich „Video erstellen“ deshalb abgeschnitten (17.11.).
        SourceInitialized += (_, _) => FitToMonitor();
        SceneList.ItemsSource = _scenes;
        Zoom.ScaleX = Zoom.ScaleY = Math.Clamp(_settings.Scale, 0.8, 2.5);
        SubtitlesBox.IsChecked = _settings.Subtitles;
        KenBurnsBox.IsChecked = _settings.KenBurns;
        Loaded += async (_, _) =>
        {
            if (!_ffmpeg.IsAvailable)
                SetStatus($"ffmpeg fehlt unter {Ffmpeg.DefaultBinFolder}. Ohne ffmpeg kann kein Video entstehen.", error: true);
            UpdateKeyState();
            await LoadVoicesAsync();
            // Datei als Startargument (z. B. „Öffnen mit“ oder Verknuepfung): gleich laden.
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && File.Exists(args[1])) await LoadSourceAsync(args[1]);
        };
        Closing += (_, _) =>
        {
            _settings.Subtitles = SubtitlesBox.IsChecked == true;
            _settings.KenBurns = KenBurnsBox.IsChecked == true;
            _settings.Scale = Zoom.ScaleX;
            _settings.Save();
            _cts?.Cancel();
        };
    }

    // ------------------------------------------------------------------ Stimmen

    private async Task LoadVoicesAsync()
    {
        var options = new List<VoiceOption>();
        var key = AppSettings.LoadKey();
        _service = null;
        if (key is not null)
        {
            _service = new IngokTtsClient(new HttpClient { BaseAddress = new Uri(_settings.ServiceUrl), Timeout = TimeSpan.FromMinutes(5) }, key);
            try
            {
                options.AddRange(await _service.VoicesAsync(CancellationToken.None));
            }
            catch (Exception ex)
            {
                SetStatus($"Sprachdienst nicht erreichbar: {ex.Message} Windows-Stimmen stehen trotzdem zur Wahl.", error: true);
            }
        }
        options.AddRange(await _windowsVoice.VoicesAsync(CancellationToken.None));

        VoiceBox.ItemsSource = options;
        VoiceBox.SelectedItem = options.FirstOrDefault(o => o.Id == _settings.LastVoice)
                                ?? options.FirstOrDefault(o => o.Provider == _service?.Name)
                                ?? options.FirstOrDefault(o => o.Label.Contains("Karsten", StringComparison.OrdinalIgnoreCase))
                                ?? options.FirstOrDefault();
    }

    private ITtsProvider ProviderFor(VoiceOption v) =>
        _service is not null && v.Provider == _service.Name ? _service : _windowsVoice;

    private async void ReloadVoices_Click(object sender, RoutedEventArgs e) => await LoadVoicesAsync();

    private void VoiceBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (VoiceBox.SelectedItem is VoiceOption v)
        {
            _settings.LastVoice = v.Id;
            _settings.Save();
        }
    }

    private async void SaveKey_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AppSettings.SaveKey(KeyBox.Password);
            KeyBox.Clear();
            UpdateKeyState();
            await LoadVoicesAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"Schlüssel konnte nicht gespeichert werden: {ex.Message}", error: true);
        }
    }

    private void UpdateKeyState() =>
        KeyState.Text = AppSettings.LoadKey() is null
            ? "kein Schlüssel gespeichert (nur Windows-Stimmen)"
            : "Schlüssel gespeichert (verschlüsselt)";

    // ------------------------------------------------------------------ Quelle

    private async void PickPowerPoint_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "PowerPoint|*.pptx;*.ppt", InitialDirectory = _settings.LastFolder ?? "" };
        if (dlg.ShowDialog(this) == true) await LoadSourceAsync(dlg.FileName);
    }

    private async void PickScript_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Skript|*.txt;*.md", InitialDirectory = _settings.LastFolder ?? "" };
        if (dlg.ShowDialog(this) == true) await LoadSourceAsync(dlg.FileName);
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files) await LoadSourceAsync(files[0]);
    }

    private async Task LoadSourceAsync(string path)
    {
        if (_cts is not null) return; // waehrend eines Laufs nicht wechseln
        var ext = Path.GetExtension(path).ToLowerInvariant();
        _settings.LastFolder = Path.GetDirectoryName(path);
        _settings.Save();
        SourcePath.Text = path;
        _scenes.Clear();
        _project = null;
        BuildButton.IsEnabled = false;

        try
        {
            if (ext is ".pptx" or ".ppt")
            {
                SetStatus("PowerPoint wird gelesen …");
                var slides = Path.Combine(WorkFolderFor(DefaultOutput(path)), "folien");
                var progress = new Progress<string>(s => SetStatus(s));
                _project = await RunStaAsync(() => PowerPointImport.Import(path, slides, progress, CancellationToken.None));
                KenBurnsBox.IsChecked = false; // Folien ohne Zoom, sonst wird Text unscharf und angeschnitten
            }
            else
            {
                var r = ScriptParser.ParseFile(path);
                _project = r.Project;
                if (r.Errors.Count > 0)
                {
                    SetStatus("Skript mit Hinweisen: " + string.Join("  ", r.Errors), error: true);
                    MessageBox.Show(this, string.Join(Environment.NewLine, r.Errors), "Hinweise zum Skript", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            _sourcePath = path;
            foreach (var s in _project.Scenes) _scenes.Add(new SceneVm(s));
            OutputPath.Text = DefaultOutput(path);
            MusicPath.Text = _project.MusicPath ?? "(keine)";
            var missing = ScriptParser.CheckFiles(_project);
            if (missing.Count > 0) SetStatus(string.Join("  ", missing), error: true);
            else if (_project.Scenes.Count > 0) SetStatus($"„{_project.Title}“ geladen: {_project.Scenes.Count} Szenen.");
            BuildButton.IsEnabled = _project.Scenes.Count > 0 && missing.Count == 0 && _ffmpeg.IsAvailable;
        }
        catch (Exception ex)
        {
            SetStatus($"Konnte {Path.GetFileName(path)} nicht lesen: {ex.Message}", error: true);
        }
        UpdateSceneInfo();
    }

    private void UpdateSceneInfo()
    {
        EmptyHint.Visibility = _scenes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        var chars = _scenes.Sum(s => s.Scene.Text.Length);
        SceneInfo.Text = _scenes.Count == 0 ? "" : $"{_scenes.Count} Szenen · {chars} Zeichen Sprechertext";
    }

    private static string DefaultOutput(string source) =>
        Path.Combine(Path.GetDirectoryName(source) ?? "", Path.GetFileNameWithoutExtension(source) + ".mp4");

    private static string WorkFolderFor(string output) =>
        Path.Combine(Path.GetDirectoryName(output) ?? "", Path.GetFileNameWithoutExtension(output) + "_arbeit");

    private void PickMusic_Click(object sender, RoutedEventArgs e)
    {
        if (_project is null) return;
        var dlg = new OpenFileDialog { Filter = "Musik|*.mp3;*.wav;*.m4a;*.aac;*.ogg;*.flac" };
        if (dlg.ShowDialog(this) != true) return;
        _project.MusicPath = dlg.FileName;
        MusicPath.Text = dlg.FileName;
    }

    private void RemoveMusic_Click(object sender, RoutedEventArgs e)
    {
        if (_project is null) return;
        _project.MusicPath = null;
        MusicPath.Text = "(keine)";
    }

    private void PickOutput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "MP4-Video|*.mp4", FileName = Path.GetFileName(OutputPath.Text) };
        if (dlg.ShowDialog(this) == true) OutputPath.Text = dlg.FileName;
    }

    // ------------------------------------------------------------------ Erstellen

    private async void Build_Click(object sender, RoutedEventArgs e)
    {
        if (_project is null || VoiceBox.SelectedItem is not VoiceOption voice) return;
        var output = OutputPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(output)) return;
        if (!output.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) output += ".mp4";

        var options = new VideoDoku.Core.RenderOptions
        {
            Subtitles = SubtitlesBox.IsChecked == true,
            KenBurns = KenBurnsBox.IsChecked == true,
        };
        _cts = new CancellationTokenSource();
        SetBusy(true);
        foreach (var s in _scenes) s.Reset();
        _lastVideo = null;

        var progress = new Progress<BuildProgress>(p =>
        {
            OverallBar.Value = p.Overall * 100;
            OverallBar.Tag = $"{p.Overall * 100:0} %";
            StatusText.Text = p.Message;
            if (p.Scene is { } sp && _scenes.FirstOrDefault(v => v.Number == sp.Number) is { } vm) vm.Apply(sp);
        });

        try
        {
            using var monitor = new MonitorReporter(Path.GetDirectoryName(output) ?? "");
            var builder = new DokuBuilder(_ffmpeg, ProviderFor(voice));
            var result = await Task.Run(() => builder.BuildAsync(_project, voice.Id, _settings.Language, output,
                WorkFolderFor(output), options, progress, monitor, _cts.Token));
            _lastVideo = result.VideoPath;
            SetStatus($"Fertig: {Path.GetFileName(result.VideoPath)} · {result.Duration:mm\\:ss} · {new FileInfo(result.VideoPath).Length / 1048576.0:0.0} MB");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Abgebrochen.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetBusy(false);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void OpenVideo_Click(object sender, RoutedEventArgs e)
    {
        if (_lastVideo is not null && File.Exists(_lastVideo))
            Process.Start(new ProcessStartInfo(_lastVideo) { UseShellExecute = true });
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_lastVideo is not null && File.Exists(_lastVideo))
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_lastVideo}\"") { UseShellExecute = true });
    }

    private void SetBusy(bool busy)
    {
        BuildButton.IsEnabled = !busy && _project is { Scenes.Count: > 0 };
        CancelButton.IsEnabled = busy;
        OpenVideoButton.IsEnabled = OpenFolderButton.IsEnabled = !busy && _lastVideo is not null;
        VoiceBox.IsEnabled = !busy;
    }

    private void SetStatus(string text, bool error = false)
    {
        StatusText.Text = text;
        StatusText.Foreground = (Brush)FindResource(error ? "Failed" : "Muted");
        StatusText.ToolTip = text;
    }

    // ------------------------------------------------------------------ Beispiel

    /// <summary>Legt einen Beispielordner mit Skript und drei erzeugten Bildern an und laedt ihn.</summary>
    private async void CreateSample_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "Skript|*.txt", FileName = "skript.txt", Title = "Ordner für das Beispiel wählen" };
        if (dlg.ShowDialog(this) != true) return;
        var folder = Path.GetDirectoryName(dlg.FileName)!;
        var bilder = Directory.CreateDirectory(Path.Combine(folder, "bilder")).FullName;
        var colors = new[] { ("#1E3A8A", "#2563EB"), ("#065F46", "#10B981"), ("#7C2D12", "#F97316") };
        var titles = new[] { "Willkommen", "So funktioniert es", "Fertig" };
        for (var i = 0; i < 3; i++)
            SaveCard(Path.Combine(bilder, $"0{i + 1}.png"), titles[i], colors[i].Item1, colors[i].Item2);

        File.WriteAllText(dlg.FileName, """
            titel: Beispiel Video-Doku
            # musik: musik/hintergrund.mp3

            [bild: bilder/01.png]
            Willkommen zur Beispiel-Doku. Dieses Video wurde vollautomatisch aus einem kurzen Skript erstellt.

            [bild: bilder/02.png]
            Zu jedem Bild steht im Skript der Sprechertext. Die Stimme kommt vom Sprachdienst oder von Windows,
            und das Werkzeug setzt alles mit Untertiteln zu einem Video zusammen.
            [pause: 1]

            [bild: bilder/03.png]
            Das war es schon. Viel Spass beim Erstellen eigener Dokus.
            """, new UTF8Encoding(false));
        await LoadSourceAsync(dlg.FileName);
    }

    private static void SaveCard(string path, string title, string from, string to)
    {
        const int w = 1920, h = 1080;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var bg = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(from), (Color)ColorConverter.ConvertFromString(to), 45);
            dc.DrawRectangle(bg, null, new Rect(0, 0, w, h));
            var text = new FormattedText(title, System.Globalization.CultureInfo.GetCultureInfo("de-CH"), FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"), 140, Brushes.White, 1.0);
            dc.DrawText(text, new Point((w - text.Width) / 2, (h - text.Height) / 2));
        }
        var bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var fs = File.Create(path);
        enc.Save(fs);
    }

    // ------------------------------------------------------------------ Hilfen

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        var s = Math.Clamp(Zoom.ScaleX + (e.Delta > 0 ? 0.1 : -0.1), 0.8, 2.5);
        Zoom.ScaleX = Zoom.ScaleY = s;
        e.Handled = true;
    }

    private void FitToMonitor()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var mon = MonitorFromWindow(hwnd, 2 /* MONITOR_DEFAULTTONEAREST */);
        var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(mon, ref info)) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var left = info.rcWork.Left / dpi.DpiScaleX;
        var top = info.rcWork.Top / dpi.DpiScaleY;
        var w = (info.rcWork.Right - info.rcWork.Left) / dpi.DpiScaleX;
        var h = (info.rcWork.Bottom - info.rcWork.Top) / dpi.DpiScaleY;
        Width = Math.Min(Width, w);
        Height = Math.Min(Height, h);
        Left = left + (w - Width) / 2;
        Top = top + (h - Height) / 2;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

    /// <summary>PowerPoint per COM sicher auf einem eigenen STA-Thread ansprechen.</summary>
    private static Task<T> RunStaAsync<T>(Func<T> work)
    {
        var tcs = new TaskCompletionSource<T>();
        var t = new Thread(() =>
        {
            try { tcs.SetResult(work()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.IsBackground = true;
        t.Start();
        return tcs.Task;
    }
}
