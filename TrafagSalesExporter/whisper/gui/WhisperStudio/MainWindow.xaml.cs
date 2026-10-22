using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using WhisperStudio.Core;

namespace WhisperStudio;

public partial class MainWindow : Window
{
    private static readonly string[] MediaExtensions =
    {
        ".wav", ".mp3", ".m4a", ".mp4", ".mov", ".aac", ".flac", ".wma", ".ogg", ".opus",
        ".mkv", ".webm", ".avi", ".wmv", ".3gp", ".amr",
    };

    private readonly StudioPaths? _paths;
    private readonly StudioSettings _settings;
    private readonly ObservableCollection<QueueEntry> _queue = new();
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch _batchClock = new();
    private CancellationTokenSource? _batchCts;
    private QueueEntry? _current;

    private readonly Recorder _recorder = new();
    private DateTime _recordStart;

    private string? _editorPath;
    private bool _editorDirty;
    private CancellationTokenSource? _aiCts;
    private string? _aiKind;
    private LlamaHost? _llama;

    private List<HistoryEntry> _history = new();

    public MainWindow()
    {
        InitializeComponent();
        _paths = StudioPaths.Find();
        _settings = _paths != null ? StudioSettings.Load(_paths.Settings) : new StudioSettings();
        if (_paths != null)
        {
            _llama = new LlamaHost(_paths);
        }

        LanguageBox.ItemsSource = new[] { "de", "en", "auto" };
        LanguageBox.SelectedItem = _settings.Language;
        ModelBox.ItemsSource = new[] { "tiny", "small", "medium", "large-v3" };
        ModelBox.SelectedItem = _settings.Model;
        TaskBox.ItemsSource = new[] { "Transkribieren", "Direkt nach Englisch übersetzen" };
        TaskBox.SelectedIndex = _settings.Task == "translate" ? 1 : 0;
        TimestampsBox.IsChecked = _settings.Timestamps;
        SkipExistingBox.IsChecked = _settings.SkipExisting;
        ExportDocxBox.IsChecked = _settings.ExportDocx;
        ExportSrtBox.IsChecked = _settings.ExportSrt;
        ExportVttBox.IsChecked = _settings.ExportVtt;
        SystemAudioBox.IsChecked = _settings.RecordSystemAudio;
        AutoTranscribeBox.IsChecked = _settings.TranscribeAfterRecording;
        AiLanguageBox.ItemsSource = AiTasks.Languages;
        AiLanguageBox.SelectedItem = "Englisch";
        ThemeBox.ItemsSource = new[] { "System", "Hell", "Dunkel" };
        ThemeBox.SelectedItem = _settings.Theme;
        LlmUrlBox.Text = _settings.LlmBaseUrl;
        LlmModelBox.Text = _settings.LlmModel;
        LlmAutoStartBox.IsChecked = _settings.LlmAutoStart;

        QueueGrid.ItemsSource = _queue;
        _queue.CollectionChanged += (_, _) => UpdateQueueState();
        _ticker.Tick += (_, _) => OnTick();
        _ticker.Start();
        _recorder.Level += (peak, rms) => Dispatcher.BeginInvoke(() => { LevelMeter.Peak = peak; LevelMeter.Rms = rms; });
        RecordTitleBox.TextChanged += (_, _) => UpdateRecordHint();

        Drop += OnDrop;
        DragOver += (_, e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;

        try
        {
            MicBox.ItemsSource = Recorder.Microphones();
            MicBox.SelectedItem = (MicBox.ItemsSource as List<AudioDevice>)?.FirstOrDefault(d => d.Id == _settings.MicDeviceId)
                                  ?? (MicBox.ItemsSource as List<AudioDevice>)?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            RecordStatus.Text = "Keine Audiogeräte gefunden: " + ex.Message;
        }

        if (_paths is null)
        {
            BatchStatus.Text = "transcribe.ps1 nicht gefunden. Das Studio muss im Ordner whisper liegen.";
            StartButton.IsEnabled = false;
        }
        else
        {
            PathsInfo.Text = $"Whisper:  {_paths.Root}\nAusgabe:  {_paths.Output}\nAufnahmen: {_paths.Input}\nKI:       {_paths.LlmDir}";
            NavFooter.Text = _paths.Root;
        }
        UpdateLlmInstallState();
        UpdateRecordHint();
        UpdateQueueState();
        Nav.SelectedIndex = 0;
    }

    // ---------------------------------------------------------------- navigation

    private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Nav.SelectedItem is not ListBoxItem { Tag: string tag })
        {
            return;
        }
        PageTranscribe.Visibility = tag == "Transcribe" ? Visibility.Visible : Visibility.Collapsed;
        PageRecord.Visibility = tag == "Record" ? Visibility.Visible : Visibility.Collapsed;
        PageEditor.Visibility = tag == "Editor" ? Visibility.Visible : Visibility.Collapsed;
        PageHistory.Visibility = tag == "History" ? Visibility.Visible : Visibility.Collapsed;
        PageSettings.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        if (tag == "History")
        {
            LoadHistory();
        }
        if (tag == "Editor")
        {
            _ = RefreshAiStateAsync();
        }
    }

    private void Navigate(string tag) => Nav.SelectedItem = Nav.Items.Cast<ListBoxItem>().First(i => (string)i.Tag == tag);

    private void Log(string line)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => Log(line));
            return;
        }
        LogBox.AppendText($"{DateTime.Now:HH:mm:ss}  {line}\n");
        LogBox.ScrollToEnd();
    }

    private void SaveSettings()
    {
        _settings.Language = LanguageBox.SelectedItem as string ?? "de";
        _settings.Model = ModelBox.SelectedItem as string ?? "medium";
        _settings.Task = TaskBox.SelectedIndex == 1 ? "translate" : "transcribe";
        _settings.Timestamps = TimestampsBox.IsChecked == true;
        _settings.SkipExisting = SkipExistingBox.IsChecked == true;
        _settings.ExportDocx = ExportDocxBox.IsChecked == true;
        _settings.ExportSrt = ExportSrtBox.IsChecked == true;
        _settings.ExportVtt = ExportVttBox.IsChecked == true;
        _settings.RecordSystemAudio = SystemAudioBox.IsChecked == true;
        _settings.TranscribeAfterRecording = AutoTranscribeBox.IsChecked == true;
        _settings.MicDeviceId = (MicBox.SelectedItem as AudioDevice)?.Id;
        _settings.Theme = ThemeBox.SelectedItem as string ?? "System";
        if (_paths != null)
        {
            _settings.Save(_paths.Settings);
        }
    }

    // ---------------------------------------------------------------- queue

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return;
        }
        Navigate("Transcribe");
        foreach (var p in paths)
        {
            if (Directory.Exists(p)) AddFolder(p);
            else if (File.Exists(p)) AddFile(p, "");
        }
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Audio- oder Videodateien auswählen",
            Multiselect = true,
            Filter = "Audio/Video|" + string.Join(";", MediaExtensions.Select(x => "*" + x)) + "|Alle Dateien|*.*",
            InitialDirectory = Directory.Exists(_settings.LastFolder) ? _settings.LastFolder : _paths?.Input,
        };
        if (dlg.ShowDialog(this) == true)
        {
            foreach (var f in dlg.FileNames) AddFile(f, "");
            _settings.LastFolder = Path.GetDirectoryName(dlg.FileNames[0]);
            SaveSettings();
        }
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Ordner wählen, Unterordner werden mit durchsucht" };
        if (Directory.Exists(_settings.LastFolder)) dlg.InitialDirectory = _settings.LastFolder;
        if (dlg.ShowDialog(this) == true)
        {
            AddFolder(dlg.FolderName);
            _settings.LastFolder = dlg.FolderName;
            SaveSettings();
        }
    }

    private void AddFolder(string root)
    {
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => MediaExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
        var added = 0;
        foreach (var f in files)
        {
            var rel = Path.GetRelativePath(root, Path.GetDirectoryName(f)!);
            if (AddFile(f, rel == "." ? "" : rel)) added++;
        }
        Log($"{added} Mediendatei(en) aus {root} hinzugefügt.");
    }

    private bool AddFile(string path, string relativeDir)
    {
        var full = Path.GetFullPath(path);
        if (_queue.Any(q => string.Equals(q.Path, full, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }
        _queue.Add(new QueueEntry(full, relativeDir));
        return true;
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in QueueGrid.SelectedItems.Cast<QueueEntry>().Where(q => q.Status != EntryStatus.Running).ToList())
        {
            _queue.Remove(item);
        }
    }

    private void ClearDone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _queue.Where(q => q.Status is EntryStatus.Done or EntryStatus.Skipped).ToList())
        {
            _queue.Remove(item);
        }
    }

    private void OpenOutput_Click(object sender, RoutedEventArgs e)
    {
        if (_paths is null) return;
        Directory.CreateDirectory(_paths.Output);
        Process.Start(new ProcessStartInfo { FileName = _paths.Output, UseShellExecute = true });
    }

    private void QueueGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (QueueGrid.SelectedItem is QueueEntry { OutputPath: { } p } && File.Exists(p))
        {
            OpenInEditor(p);
        }
    }

    private void UpdateQueueState()
    {
        QueueEmptyHint.Visibility = _queue.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        StartButton.IsEnabled = _paths != null && _batchCts == null && _queue.Any(q => q.Status == EntryStatus.Pending);
    }

    /// <summary>output\[subfolder]\2026-10-22_1430_Titel.txt, deterministic per entry.</summary>
    private string NaturalOutputPath(QueueEntry q)
    {
        var dir = string.IsNullOrEmpty(q.RelativeDir) ? _paths!.Output : Path.Combine(_paths!.Output, q.RelativeDir);
        return Path.Combine(dir, q.Stem + ".txt");
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_paths is null || _batchCts != null) return;
        // Commit a cell that is still being edited, otherwise its title is lost for this run.
        QueueGrid.CommitEdit(DataGridEditingUnit.Row, true);
        SaveSettings();
        var opts = new RunOptions(_settings.Language, _settings.Model, _settings.Task, _settings.Timestamps);

        _batchCts = new CancellationTokenSource();
        CancelButton.IsEnabled = true;
        StartButton.IsEnabled = false;
        _batchClock.Restart();
        var runner = new WhisperRunner(_paths);
        runner.Log += Log;
        runner.Progress += (pct, total) => Dispatcher.BeginInvoke(() => { _current?.ReportProgress(pct, total); UpdateBatchProgress(); });

        int done = 0, skipped = 0, failed = 0;
        try
        {
            while (!_batchCts.IsCancellationRequested && _queue.FirstOrDefault(q => q.Status == EntryStatus.Pending) is { } q)
            {
                var natural = NaturalOutputPath(q);
                var existing = !_settings.SkipExisting ? null
                    : File.Exists(natural) && new FileInfo(natural).Length > 0 ? natural
                    : TranscriptMeta.FindExistingTranscript(q.Path, Path.GetDirectoryName(natural)!);
                if (existing != null)
                {
                    q.OutputPath = existing;
                    q.Message = "Transkript schon vorhanden";
                    q.Status = EntryStatus.Skipped;
                    skipped++;
                    continue;
                }
                _current = q;
                q.Status = EntryStatus.Running;
                QueueGrid.ScrollIntoView(q);
                Log($"Starte {q.DisplayName} -> {Path.GetFileName(natural)} ({opts.Model}, {opts.Language}, {opts.Task})");
                UpdateBatchProgress();

                // transcribe.py never overwrites: an occupied name becomes name_<timestamp>.txt.
                var result = await runner.RunAsync(q.Path, natural, opts, _batchCts.Token);
                if (result.Cancelled)
                {
                    q.Status = EntryStatus.Cancelled;
                }
                else if (result.ExitCode == 0 && result.WrittenPath != null)
                {
                    q.OutputPath = result.WrittenPath;
                    var extra = ExportExtras(result.WrittenPath);
                    q.Message = extra.Count > 0 ? "fertig · " + string.Join(", ", extra.Select(Path.GetExtension)) : "fertig";
                    q.Status = EntryStatus.Done;
                    done++;
                }
                else if (result.ExitCode == WhisperRunner.ExitSkipped)
                {
                    q.Message = "keine Audiospur";
                    q.Status = EntryStatus.Skipped;
                    skipped++;
                }
                else
                {
                    q.Message = $"Exit-Code {result.ExitCode}, Details unter Einstellungen > Protokoll";
                    q.Status = EntryStatus.Error;
                    failed++;
                }
            }
        }
        catch (Exception ex)
        {
            Log("Fehler: " + ex.Message);
            if (_current != null) { _current.Message = ex.Message; _current.Status = EntryStatus.Error; }
        }
        finally
        {
            var cancelled = _batchCts.IsCancellationRequested;
            _current = null;
            _batchCts.Dispose();
            _batchCts = null;
            _batchClock.Stop();
            CancelButton.IsEnabled = false;
            UpdateQueueState();
            UpdateBatchProgress();
            BatchStatus.Text = cancelled
                ? $"Abgebrochen. {done} fertig."
                : $"Fertig: {done} transkribiert, {skipped} übersprungen, {failed} Fehler.";
            BatchDetail.Text = $"Dauer {_batchClock.Elapsed:hh\\:mm\\:ss}";
            var last = _queue.LastOrDefault(x => x.Status == EntryStatus.Done);
            if (!cancelled && last?.OutputPath != null && done == 1)
            {
                OpenInEditor(last.OutputPath);
            }
        }
    }

    private List<string> ExportExtras(string txtPath)
    {
        try
        {
            var files = TranscriptExport.ExportAll(txtPath, _settings.ExportSrt, _settings.ExportVtt, _settings.ExportDocx);
            foreach (var f in files) Log("Export: " + f);
            return files;
        }
        catch (Exception ex)
        {
            Log("Export fehlgeschlagen: " + ex.Message);
            return new List<string>();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _batchCts?.Cancel();

    private void UpdateBatchProgress()
    {
        var total = _queue.Count;
        if (total == 0)
        {
            BatchProgress.Value = 0;
            return;
        }
        var handled = _queue.Count(q => q.Status is EntryStatus.Done or EntryStatus.Skipped or EntryStatus.Error);
        var overall = (handled + (_current?.Percent ?? 0) / 100.0) / total;
        BatchProgress.Value = overall * 1000;
        if (_batchCts == null)
        {
            return;
        }
        if (_current != null)
        {
            var idx = _queue.IndexOf(_current) + 1;
            BatchStatus.Text = $"Datei {idx} von {total}: {_current.Stem}";
            var audio = _current.AudioSeconds > 0 ? $" von {TimeSpan.FromSeconds(_current.AudioSeconds):hh\\:mm\\:ss} Audio" : "";
            BatchDetail.Text = $"Gesamt {overall * 100:0} % · Datei {_current.Percent:0} %{audio} · läuft {_current.Elapsed:hh\\:mm\\:ss}"
                               + (_current.Remaining is { } r ? $" · Rest dieser Datei ca. {r:hh\\:mm\\:ss}" : "")
                               + $" · gesamt {_batchClock.Elapsed:hh\\:mm\\:ss}";
        }
    }

    private void OnTick()
    {
        _current?.Tick();
        if (_batchCts != null) UpdateBatchProgress();
        if (_recorder.IsRecording) RecordTime.Text = _recorder.Elapsed.ToString(@"hh\:mm\:ss");
    }

    // ---------------------------------------------------------------- recording

    private void UpdateRecordHint()
    {
        var title = string.IsNullOrWhiteSpace(RecordTitleBox.Text) ? "Aufnahme" : RecordTitleBox.Text;
        RecordFileHint.Text = $"Wird gespeichert als input\\{OutputNaming.BuildStem(DateTime.Now, title)}.wav";
    }

    private async void Record_Click(object sender, RoutedEventArgs e)
    {
        if (_paths is null) return;
        if (!_recorder.IsRecording)
        {
            try
            {
                SaveSettings();
                _recordStart = DateTime.Now;
                _recorder.Start((MicBox.SelectedItem as AudioDevice)?.Id, SystemAudioBox.IsChecked == true, _paths.Temp);
                RecordGlyph.Text = "";
                RecordStatus.Text = SystemAudioBox.IsChecked == true ? "Aufnahme läuft: Mikrofon und Systemton." : "Aufnahme läuft: Mikrofon.";
                MicBox.IsEnabled = SystemAudioBox.IsEnabled = false;
                Log("Aufnahme gestartet.");
            }
            catch (Exception ex)
            {
                RecordStatus.Text = "Aufnahme nicht möglich: " + ex.Message;
                Log("Aufnahme-Fehler: " + ex);
            }
            return;
        }

        RecordButton.IsEnabled = false;
        RecordStatus.Text = "Speichere und mische die Spuren...";
        try
        {
            var title = string.IsNullOrWhiteSpace(RecordTitleBox.Text) ? "Aufnahme" : RecordTitleBox.Text;
            var target = Path.Combine(_paths.Input, OutputNaming.BuildStem(_recordStart, title) + ".wav");
            var n = 2;
            while (File.Exists(target))
            {
                target = Path.Combine(_paths.Input, $"{OutputNaming.BuildStem(_recordStart, title)}_{n++}.wav");
            }
            var wav = await _recorder.StopAsync(target);
            RecordStatus.Text = "Gespeichert: " + wav;
            Log("Aufnahme gespeichert: " + wav);
            if (AutoTranscribeBox.IsChecked == true)
            {
                AddFile(wav, "");
                var entry = _queue.Last();
                entry.RecordedAt = _recordStart;
                entry.Title = title;
                Navigate("Transcribe");
                if (_batchCts == null) Start_Click(this, new RoutedEventArgs());
            }
        }
        catch (Exception ex)
        {
            RecordStatus.Text = "Fehler beim Speichern: " + ex.Message;
            Log("Aufnahme-Fehler: " + ex);
        }
        finally
        {
            RecordGlyph.Text = "";
            RecordButton.IsEnabled = true;
            MicBox.IsEnabled = SystemAudioBox.IsEnabled = true;
            LevelMeter.Peak = LevelMeter.Rms = 0;
            UpdateRecordHint();
        }
    }

    // ---------------------------------------------------------------- editor

    private void OpenInEditor(string txtPath)
    {
        if (_editorDirty && !ConfirmDiscard()) return;
        _editorPath = txtPath;
        EditorBox.Text = File.ReadAllText(txtPath, Encoding.UTF8);
        _editorDirty = false;
        EditorFileLabel.Text = txtPath;
        EditorSaveButton.IsEnabled = false;
        EditorExportButton.IsEnabled = EditorShowButton.IsEnabled = true;
        AiOutputBox.Text = "";
        AiSaveButton.IsEnabled = false;
        AiStatus.Text = "";
        Navigate("Editor");
    }

    private bool ConfirmDiscard() =>
        MessageBox.Show(this, "Ungespeicherte Änderungen im Editor verwerfen?", "Whisper Studio", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private void EditorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_editorPath == null) return;
        _editorDirty = true;
        EditorSaveButton.IsEnabled = true;
    }

    private void EditorOpen_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Transkripte|*.txt;*.md|Alle Dateien|*.*", InitialDirectory = _paths?.Output };
        if (dlg.ShowDialog(this) == true) OpenInEditor(dlg.FileName);
    }

    private void EditorSave_Click(object sender, RoutedEventArgs e)
    {
        if (_editorPath == null) return;
        File.WriteAllText(_editorPath, EditorBox.Text.Replace("\r\n", "\n"), new UTF8Encoding(false));
        _editorDirty = false;
        EditorSaveButton.IsEnabled = false;
        AiStatus.Text = "Transkript gespeichert.";
    }

    private void EditorExport_Click(object sender, RoutedEventArgs e)
    {
        if (_editorPath == null) return;
        if (_editorDirty) EditorSave_Click(sender, e);
        var files = TranscriptExport.ExportAll(_editorPath, srt: true, vtt: true, docx: true);
        AiStatus.Text = "Exportiert: " + string.Join(", ", files.Select(Path.GetFileName))
                        + (files.Any(f => f.EndsWith(".srt")) ? "" : " (SRT/VTT brauchen die .json mit Zeiten)");
    }

    private void EditorShow_Click(object sender, RoutedEventArgs e)
    {
        if (_editorPath != null) Process.Start("explorer.exe", $"/select,\"{_editorPath}\"");
    }

    // ---------------------------------------------------------------- local AI

    private LlmClient Llm() => new(_settings.LlmBaseUrl, _settings.LlmModel);

    private async Task RefreshAiStateAsync()
    {
        var ok = await Llm().IsReachableAsync();
        AiState.Text = ok ? "bereit" : (_llama?.IsInstalled == true ? "wird bei Bedarf gestartet" : "nicht eingerichtet, siehe Einstellungen");
    }

    private async Task<bool> EnsureLlmAsync()
    {
        var llm = Llm();
        if (await llm.IsReachableAsync()) return true;
        if (_settings.LlmAutoStart && _llama != null && Uri.TryCreate(_settings.LlmBaseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback)
        {
            if (!_llama.TryStart(uri.Port, out var msg))
            {
                AiStatus.Text = "Lokale KI " + msg;
                return false;
            }
            Log("llama-server " + msg);
            AiStatus.Text = "Lokale KI startet (Modell wird geladen)...";
            for (var i = 0; i < 90; i++)
            {
                await Task.Delay(1000);
                if (await llm.IsReachableAsync()) return true;
            }
        }
        AiStatus.Text = "Lokale KI nicht erreichbar. Einstellungen prüfen.";
        return false;
    }

    private async Task RunAiAsync(string kind, Func<LlmClient, string, Action<string>, Action<string>, CancellationToken, Task<string>> work)
    {
        if (_editorPath == null || string.IsNullOrWhiteSpace(EditorBox.Text))
        {
            AiStatus.Text = "Zuerst ein Transkript öffnen.";
            return;
        }
        SetAiBusy(true);
        AiOutputBox.Text = "";
        _aiKind = kind;
        _aiCts = new CancellationTokenSource();
        var clock = Stopwatch.StartNew();
        try
        {
            if (!await EnsureLlmAsync()) return;
            var text = TranscriptExport.BodyOf(EditorBox.Text);
            void OnToken(string t) => Dispatcher.BeginInvoke(() => { AiOutputBox.AppendText(t); AiOutputBox.ScrollToEnd(); });
            void OnStatus(string s) => Dispatcher.BeginInvoke(() => AiStatus.Text = s);
            OnStatus("KI arbeitet...");
            var result = await work(Llm(), text, OnToken, OnStatus, _aiCts.Token);
            AiOutputBox.Text = AiTasks.StripFence(result);
            AiStatus.Text = $"Fertig nach {clock.Elapsed:mm\\:ss}. Bitte prüfen, die KI kann sich irren.";
            AiSaveButton.IsEnabled = true;
        }
        catch (OperationCanceledException)
        {
            AiStatus.Text = "Abgebrochen.";
        }
        catch (Exception ex)
        {
            AiStatus.Text = "KI-Fehler: " + ex.Message;
            Log("KI-Fehler: " + ex);
        }
        finally
        {
            _aiCts?.Dispose();
            _aiCts = null;
            SetAiBusy(false);
        }
    }

    private void SetAiBusy(bool busy)
    {
        AiProtocolButton.IsEnabled = AiCleanButton.IsEnabled = AiTranslateButton.IsEnabled = !busy;
        AiCancelButton.IsEnabled = busy;
    }

    private async void AiProtocol_Click(object sender, RoutedEventArgs e) =>
        await RunAiAsync("protokoll", (llm, text, tok, st, ct) => AiTasks.ProtocolAsync(llm, text, tok, st, ct));

    private async void AiClean_Click(object sender, RoutedEventArgs e) =>
        await RunAiAsync("geglaettet", (llm, text, tok, st, ct) => AiTasks.CleanUpAsync(llm, text, tok, st, ct));

    private async void AiTranslate_Click(object sender, RoutedEventArgs e)
    {
        var lang = AiLanguageBox.SelectedItem as string ?? "Englisch";
        await RunAiAsync("uebersetzung_" + lang, (llm, text, tok, st, ct) => AiTasks.TranslateAsync(llm, text, lang, tok, st, ct));
    }

    private void AiCancel_Click(object sender, RoutedEventArgs e) => _aiCts?.Cancel();

    private void AiSave_Click(object sender, RoutedEventArgs e)
    {
        if (_editorPath == null || _aiKind == null) return;
        var dir = Path.GetDirectoryName(_editorPath)!;
        var stem = Path.GetFileNameWithoutExtension(_editorPath);
        var target = Path.Combine(dir, $"{stem}_{_aiKind}.md");
        if (File.Exists(target))
        {
            target = Path.Combine(dir, $"{stem}_{_aiKind}_{DateTime.Now:yyyyMMdd_HHmmss}.md");
        }
        File.WriteAllText(target, AiOutputBox.Text.Replace("\r\n", "\n"), new UTF8Encoding(false));
        AiStatus.Text = "Gespeichert: " + Path.GetFileName(target);
    }

    // ---------------------------------------------------------------- history

    private void LoadHistory()
    {
        if (_paths is null || !Directory.Exists(_paths.Output))
        {
            _history = new();
        }
        else
        {
            _history = Directory.EnumerateFiles(_paths.Output, "*.txt", SearchOption.AllDirectories)
                .Select(f => HistoryEntry.From(f, _paths.Output))
                .OrderByDescending(h => h.Date)
                .ToList();
        }
        ApplyHistoryFilter();
    }

    private void ApplyHistoryFilter()
    {
        var q = HistorySearch.Text.Trim();
        if (q.Length == 0)
        {
            HistoryGrid.ItemsSource = _history;
            return;
        }
        HistoryGrid.ItemsSource = _history.Where(h =>
            h.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
            || h.Folder.Contains(q, StringComparison.OrdinalIgnoreCase)
            || (q.Length >= 3 && FileContains(h.TxtPath, q))).ToList();
    }

    private static bool FileContains(string path, string q)
    {
        try { return File.ReadAllText(path, Encoding.UTF8).Contains(q, StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }

    private void HistorySearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyHistoryFilter();

    private void HistoryGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (HistoryGrid.SelectedItem is HistoryEntry h) OpenInEditor(h.TxtPath);
    }

    // ---------------------------------------------------------------- settings

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Application.Current.ThemeMode = (ThemeBox.SelectedItem as string) switch
        {
            "Hell" => ThemeMode.Light,
            "Dunkel" => ThemeMode.Dark,
            _ => ThemeMode.System,
        };
        if (IsLoaded) SaveSettings();
    }

    private void UpdateLlmInstallState()
    {
        if (_llama == null)
        {
            LlmInstallState.Text = "";
            return;
        }
        LlmInstallState.Text = _llama.IsInstalled
            ? $"Installiert: {Path.GetFileName(_llama.ModelFile)}. Läuft nur auf diesem PC, kein Internet nötig."
            : $"Noch nicht installiert. Erwartet werden {Path.GetRelativePath(_paths!.Root, _llama.ServerExe)} und ein .gguf-Modell in llm\\models (siehe HANDOFF.md).";
    }

    private void LlmSave_Click(object sender, RoutedEventArgs e)
    {
        _settings.LlmBaseUrl = LlmUrlBox.Text.Trim();
        _settings.LlmModel = LlmModelBox.Text.Trim();
        _settings.LlmAutoStart = LlmAutoStartBox.IsChecked == true;
        SaveSettings();
        LlmInstallState.Text = "Gespeichert.";
    }

    private async void LlmTest_Click(object sender, RoutedEventArgs e)
    {
        LlmSave_Click(sender, e);
        LlmInstallState.Text = "Prüfe...";
        var ok = await EnsureLlmAsync();
        LlmInstallState.Text = ok ? "Verbindung ok." : "Keine Verbindung. " + AiStatus.Text;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_batchCts != null || _recorder.IsRecording)
        {
            if (MessageBox.Show(this, "Transkription oder Aufnahme läuft noch. Trotzdem beenden?", "Whisper Studio",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            _batchCts?.Cancel();
        }
        if (_editorDirty && !ConfirmDiscard())
        {
            e.Cancel = true;
            return;
        }
        SaveSettings();
        _recorder.Dispose();
        _llama?.Dispose();
        base.OnClosing(e);
    }
}
