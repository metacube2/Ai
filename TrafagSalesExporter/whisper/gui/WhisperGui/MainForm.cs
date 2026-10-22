using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WhisperGui;

public class MainForm : Form
{
    private enum QueueStatus { Pending, Running, Done, Error, Cancelled, Skipped }

    private const int ExitSkipped = 3;

    private sealed class QueueItem
    {
        public required string Path { get; init; }
        // Relative subfolder (below the chosen root folder) that the output should
        // mirror. Empty for files added individually. Uses OS directory separators.
        public string RelativeDir { get; init; } = "";
        public QueueStatus Status { get; set; } = QueueStatus.Pending;
        public string? TargetOutputPath { get; set; }
        // Path the script reports back via "Written:". Normally equal to
        // TargetOutputPath; differs when the script had to dodge a locked target.
        public string? WrittenOutputPath { get; set; }
        public string? OutputPath { get; set; }
        // Share of the audio already transcribed (0-100), from the script's PROGRESS lines.
        public double? Percent { get; set; }
        public string FileName => System.IO.Path.GetFileName(Path);
        public string DisplayName => string.IsNullOrEmpty(RelativeDir)
            ? FileName
            : System.IO.Path.Combine(RelativeDir, FileName);

        public override string ToString()
        {
            var marker = Status switch
            {
                QueueStatus.Pending => "⏳",
                QueueStatus.Running => "▶",
                QueueStatus.Done => "✓",
                QueueStatus.Error => "✗",
                QueueStatus.Cancelled => "⏹",
                QueueStatus.Skipped => "⊘",
                _ => "",
            };
            return Status == QueueStatus.Running && Percent is { } pct
                ? $"{marker}  {pct:0} %  {DisplayName}"
                : $"{marker}  {DisplayName}";
        }
    }

    private static readonly string[] MediaExtensions =
    {
        ".wav", ".mp3", ".m4a", ".mp4", ".mov", ".aac", ".flac",
        ".wma", ".ogg", ".opus", ".mkv", ".webm", ".avi", ".wmv", ".3gp", ".amr",
    };

    private readonly ListBox _queueList = new() { Dock = DockStyle.Fill, SelectionMode = SelectionMode.MultiExtended, Font = new Font("Segoe UI", 9F), IntegralHeight = false };
    private readonly Button _addFilesButton = new() { Text = "Dateien hinzufügen...", AutoSize = true, Margin = new Padding(3) };
    private readonly Button _addFolderButton = new() { Text = "Ordner hinzufügen...", AutoSize = true, Margin = new Padding(3) };
    private readonly Button _removeSelectedButton = new() { Text = "Entfernen", AutoSize = true, Margin = new Padding(3) };
    private readonly Button _clearQueueButton = new() { Text = "Liste leeren", AutoSize = true, Margin = new Padding(3) };
    private readonly ComboBox _languageBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
    private readonly ComboBox _modelBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly CheckBox _timestampsBox = new() { Text = "Zeitstempel", AutoSize = true, Margin = new Padding(15, 8, 3, 3) };
    private readonly CheckBox _skipExistingBox = new() { Text = "Vorhandene überspringen", AutoSize = true, Margin = new Padding(15, 8, 3, 3) };
    private readonly Button _startButton = new() { Text = "Start Batch-Transkription", AutoSize = true };
    private readonly Button _cancelButton = new() { Text = "Abbrechen", AutoSize = true, Enabled = false };
    private readonly Button _openFolderButton = new() { Text = "Ausgabeordner öffnen", AutoSize = true, Enabled = false };
    private readonly Button _openResultButton = new() { Text = "Ergebnis im Editor öffnen", AutoSize = true, Enabled = false };
    private const int ProgressScale = 1000;
    private readonly ProgressBar _progressBar = new() { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 0, Dock = DockStyle.Fill, Maximum = ProgressScale };
    private readonly ProgressBar _totalProgressBar = new() { Style = ProgressBarStyle.Continuous, Dock = DockStyle.Fill, Maximum = ProgressScale };
    private readonly Label _totalLabel = new() { Text = "", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly TextBox _logBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 9F), WordWrap = false };
    private readonly TextBox _resultBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
    private readonly Label _statusLabel = new() { Text = "Bereit.", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly TabControl _tabControl = new() { Dock = DockStyle.Fill };
    private readonly TabPage _resultTab = new("Ergebnis");
    private readonly System.Windows.Forms.Timer _elapsedTimer = new() { Interval = 1000 };
    private readonly Stopwatch _stopwatch = new();
    // Time spent on the current file only; the ETA must not include earlier files.
    private readonly Stopwatch _itemStopwatch = new();
    private double _itemAudioSeconds;

    private readonly string? _whisperRoot;
    private readonly string? _settingsPath;
    private readonly GuiSettings _settings;

    private Process? _process;
    private string? _outputTxtPath;
    private QueueItem? _currentItem;
    private bool _batchRunning;
    private bool _cancelRequested;

    public MainForm()
    {
        Text = "Whisper Transkription";
        Width = 900;
        Height = 700;
        MinimumSize = new Size(700, 500);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        _whisperRoot = FindWhisperRoot();
        _settingsPath = _whisperRoot != null ? Path.Combine(_whisperRoot, "cache", "gui_settings.json") : null;
        _settings = _settingsPath != null ? GuiSettings.Load(_settingsPath) : new GuiSettings();

        _languageBox.Items.AddRange(new object[] { "auto", "de", "en" });
        _languageBox.SelectedItem = _languageBox.Items.Contains(_settings.Language) ? _settings.Language : "de";

        _modelBox.Items.AddRange(new object[] { "tiny", "small", "medium", "large-v3" });
        _modelBox.SelectedItem = _modelBox.Items.Contains(_settings.Model) ? _settings.Model : "medium";

        _timestampsBox.Checked = _settings.Timestamps;
        _skipExistingBox.Checked = _settings.SkipExisting;

        BuildLayout();
        WireEvents();
        UpdateStartButtonState();

        if (_whisperRoot is null)
        {
            AppendLog("WARNUNG: transcribe.ps1 wurde nicht gefunden. Bitte die GUI innerhalb des 'whisper'-Ordners belassen.");
            _startButton.Enabled = false;
        }
    }

    private void BuildLayout()
    {
        var queuePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(8, 8, 8, 4) };
        queuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        queuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var queueButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(6, 0, 0, 0) };
        queueButtons.Controls.Add(_addFilesButton);
        queueButtons.Controls.Add(_addFolderButton);
        queueButtons.Controls.Add(_removeSelectedButton);
        queueButtons.Controls.Add(_clearQueueButton);
        queuePanel.Controls.Add(_queueList, 0, 0);
        queuePanel.Controls.Add(queueButtons, 1, 0);

        var optionsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Padding = new Padding(8, 4, 8, 4) };
        optionsPanel.Controls.Add(new Label { Text = "Sprache:", AutoSize = true, Margin = new Padding(0, 8, 3, 3) });
        optionsPanel.Controls.Add(_languageBox);
        optionsPanel.Controls.Add(new Label { Text = "Modell:", AutoSize = true, Margin = new Padding(15, 8, 3, 3) });
        optionsPanel.Controls.Add(_modelBox);
        optionsPanel.Controls.Add(_timestampsBox);
        optionsPanel.Controls.Add(_skipExistingBox);

        var buttonsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Padding = new Padding(8, 0, 8, 8) };
        buttonsPanel.Controls.Add(_startButton);
        _cancelButton.Margin = new Padding(6, 3, 3, 3);
        buttonsPanel.Controls.Add(_cancelButton);
        _openFolderButton.Margin = new Padding(18, 3, 3, 3);
        buttonsPanel.Controls.Add(_openFolderButton);
        _openResultButton.Margin = new Padding(6, 3, 3, 3);
        buttonsPanel.Controls.Add(_openResultButton);

        var statusTable = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 2, Padding = new Padding(8, 0, 8, 4) };
        statusTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        statusTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        statusTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        statusTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        statusTable.Controls.Add(_progressBar, 0, 0);
        statusTable.Controls.Add(_statusLabel, 1, 0);
        statusTable.Controls.Add(_totalProgressBar, 0, 1);
        statusTable.Controls.Add(_totalLabel, 1, 1);

        _resultTab.Controls.Add(_resultBox);
        var logTab = new TabPage("Log");
        logTab.Controls.Add(_logBox);
        _tabControl.TabPages.Add(logTab);
        _tabControl.TabPages.Add(_resultTab);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        root.Controls.Add(queuePanel, 0, 0);
        root.Controls.Add(optionsPanel, 0, 1);
        root.Controls.Add(buttonsPanel, 0, 2);
        root.Controls.Add(statusTable, 0, 3);
        root.Controls.Add(_tabControl, 0, 4);

        Controls.Add(root);
    }

    private void WireEvents()
    {
        _addFilesButton.Click += (s, e) => BrowseFiles();
        _addFolderButton.Click += (s, e) => BrowseFolder();
        _removeSelectedButton.Click += (s, e) => RemoveSelectedFromQueue();
        _clearQueueButton.Click += (s, e) => ClearQueue();
        _startButton.Click += (s, e) => StartTranscription();
        _cancelButton.Click += (s, e) => CancelTranscription();
        _openFolderButton.Click += (s, e) => OpenOutputFolder();
        _openResultButton.Click += (s, e) => OpenResultInEditor();
        _queueList.DoubleClick += (s, e) =>
        {
            if (_queueList.SelectedItem is QueueItem { OutputPath: { } outputPath } && File.Exists(outputPath))
            {
                Process.Start(new ProcessStartInfo { FileName = outputPath, UseShellExecute = true });
            }
        };
        _elapsedTimer.Tick += (s, e) => UpdateProgressDisplay();

        DragEnter += (s, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
        };
        DragDrop += (s, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
            {
                var loose = new List<string>();
                foreach (var p in paths)
                {
                    if (Directory.Exists(p))
                    {
                        AddFolder(p);
                    }
                    else if (File.Exists(p))
                    {
                        loose.Add(p);
                    }
                }
                if (loose.Count > 0)
                {
                    AddFiles(loose);
                }
            }
        };
    }

    private static string? FindWhisperRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "transcribe.ps1")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private void BrowseFiles()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Audio-/Videodateien auswählen",
            Multiselect = true,
            Filter = "Audio/Video (*.wav;*.mp3;*.m4a;*.mp4;*.mov;*.aac;*.flac)|*.wav;*.mp3;*.m4a;*.mp4;*.mov;*.aac;*.flac|Alle Dateien (*.*)|*.*",
        };
        var initialDir = _settings.LastFolder;
        if ((initialDir is null || !Directory.Exists(initialDir)) && _whisperRoot != null)
        {
            var inputDir = Path.Combine(_whisperRoot, "input");
            if (Directory.Exists(inputDir))
            {
                initialDir = inputDir;
            }
        }
        if (initialDir != null)
        {
            dlg.InitialDirectory = initialDir;
        }
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            AddFiles(dlg.FileNames);
        }
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        string? lastAdded = null;
        foreach (var raw in paths)
        {
            if (string.IsNullOrWhiteSpace(raw) || !File.Exists(raw))
            {
                continue;
            }
            var full = Path.GetFullPath(raw);
            var alreadyQueued = _queueList.Items.Cast<QueueItem>().Any(qi => string.Equals(qi.Path, full, StringComparison.OrdinalIgnoreCase));
            if (alreadyQueued)
            {
                continue;
            }
            _queueList.Items.Add(new QueueItem { Path = full });
            lastAdded = full;
        }

        if (lastAdded != null)
        {
            if (_settingsPath != null)
            {
                _settings.LastFolder = Path.GetDirectoryName(lastAdded);
                _settings.Save(_settingsPath);
            }
            UpdateStartButtonState();
        }
    }

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Ordner auswählen – alle Medien in Unterordnern werden hinzugefügt",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
        };
        var initialDir = _settings.LastFolder;
        if (initialDir != null && Directory.Exists(initialDir))
        {
            dlg.SelectedPath = initialDir;
        }
        else if (_whisperRoot != null && Directory.Exists(Path.Combine(_whisperRoot, "input")))
        {
            dlg.SelectedPath = Path.Combine(_whisperRoot, "input");
        }
        if (dlg.ShowDialog(this) == DialogResult.OK && Directory.Exists(dlg.SelectedPath))
        {
            AddFolder(dlg.SelectedPath);
        }
    }

    private void AddFolder(string rootFolder)
    {
        string[] files;
        try
        {
            files = Directory.EnumerateFiles(rootFolder, "*", SearchOption.AllDirectories)
                .Where(f => MediaExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            AppendLog($"Ordner konnte nicht gelesen werden: {ex.Message}");
            return;
        }

        if (files.Length == 0)
        {
            AppendLog($"Keine Mediendateien in {rootFolder} gefunden.");
            return;
        }

        var rootFull = Path.GetFullPath(rootFolder);
        var added = 0;
        foreach (var raw in files)
        {
            var full = Path.GetFullPath(raw);
            if (_queueList.Items.Cast<QueueItem>().Any(qi => string.Equals(qi.Path, full, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            var relDir = Path.GetRelativePath(rootFull, Path.GetDirectoryName(full)!);
            if (relDir == ".")
            {
                relDir = "";
            }
            _queueList.Items.Add(new QueueItem { Path = full, RelativeDir = relDir });
            added++;
        }

        AppendLog($"{added} Mediendatei(en) aus {rootFolder} hinzugefügt.");
        if (added > 0)
        {
            if (_settingsPath != null)
            {
                _settings.LastFolder = rootFull;
                _settings.Save(_settingsPath);
            }
            UpdateStartButtonState();
        }
    }

    private void RemoveSelectedFromQueue()
    {
        foreach (var item in _queueList.SelectedItems.Cast<QueueItem>().ToList())
        {
            if (item.Status == QueueStatus.Running)
            {
                continue;
            }
            _queueList.Items.Remove(item);
        }
        UpdateStartButtonState();
    }

    private void ClearQueue()
    {
        for (var i = _queueList.Items.Count - 1; i >= 0; i--)
        {
            if (_queueList.Items[i] is QueueItem { Status: QueueStatus.Running })
            {
                continue;
            }
            _queueList.Items.RemoveAt(i);
        }
        UpdateStartButtonState();
    }

    private bool HasPendingItems() => _queueList.Items.Cast<QueueItem>().Any(qi => qi.Status == QueueStatus.Pending);

    private void UpdateStartButtonState()
    {
        _startButton.Enabled = !_batchRunning && _whisperRoot != null && HasPendingItems();
    }

    private void StartTranscription()
    {
        if (_batchRunning || _whisperRoot is null)
        {
            return;
        }
        if (!HasPendingItems())
        {
            MessageBox.Show(this, "Bitte zuerst Dateien zur Warteschlange hinzufügen.", "Whisper", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var language = _languageBox.SelectedItem?.ToString() ?? "auto";
        var model = _modelBox.SelectedItem?.ToString() ?? "medium";
        var timestamps = _timestampsBox.Checked;
        var skipExisting = _skipExistingBox.Checked;
        SaveSettings(language, model, timestamps, skipExisting);

        _logBox.Clear();
        _resultBox.Clear();
        _outputTxtPath = null;
        _openResultButton.Enabled = false;
        _openFolderButton.Enabled = false;

        _batchRunning = true;
        _cancelRequested = false;
        _startButton.Enabled = false;
        _addFilesButton.Enabled = true;
        _cancelButton.Enabled = true;
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.MarqueeAnimationSpeed = 30;
        _stopwatch.Restart();
        _elapsedTimer.Start();

        ProcessNextInQueue(language, model, timestamps, skipExisting);
    }

    private void ProcessNextInQueue(string language, string model, bool timestamps, bool skipExisting)
    {
        QueueItem next;
        while (true)
        {
            var candidate = _queueList.Items.Cast<QueueItem>().FirstOrDefault(qi => qi.Status == QueueStatus.Pending);
            if (candidate is null)
            {
                FinishBatch(cancelled: false);
                return;
            }

            var naturalPath = ComputeNaturalOutputPath(candidate);

            // Resume support: if the transcript already exists (non-empty), skip it
            // instead of re-transcribing. Lets an interrupted batch continue.
            // This has to be decided on the natural name, BEFORE uniquifying below -
            // the uniquified path never exists, so testing that one would silently
            // disable resume and produce a fresh timestamped copy on every run.
            if (skipExisting && File.Exists(naturalPath) && new FileInfo(naturalPath).Length > 0)
            {
                candidate.Status = QueueStatus.Skipped;
                candidate.TargetOutputPath = naturalPath;
                candidate.OutputPath = naturalPath;
                var rel = Path.GetRelativePath(Path.Combine(_whisperRoot!, "output"), naturalPath);
                AppendLog($"Übersprungen (Ausgabe bereits vorhanden): {rel}");
                RefreshQueueItem(candidate);
                continue;
            }

            candidate.TargetOutputPath = ReserveOutputPath(candidate, naturalPath);
            if (!string.Equals(candidate.TargetOutputPath, naturalPath, StringComparison.OrdinalIgnoreCase))
            {
                AppendLog($"  Vorhanden: {Path.GetFileName(naturalPath)} bleibt erhalten.");
            }

            next = candidate;
            break;
        }

        _currentItem = next;
        next.Status = QueueStatus.Running;
        next.Percent = null;
        _itemAudioSeconds = 0;
        _itemStopwatch.Restart();
        // Marquee until the script reports its first PROGRESS line: model loading and
        // VAD come before any segment and give no measurable progress.
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.MarqueeAnimationSpeed = 30;
        RefreshQueueItem(next);
        _queueList.SelectedItem = next;

        var index = _queueList.Items.IndexOf(next) + 1;
        var total = _queueList.Items.Count;
        AppendLog($"> [{index}/{total}] Starte: Modell={model} Sprache={language} Datei={next.DisplayName}");
        AppendLog($"  Ausgabe: {Path.GetRelativePath(Path.Combine(_whisperRoot!, "output"), next.TargetOutputPath!)}");
        _statusLabel.Text = $"Datei {index}/{total}: {next.FileName} – läuft...";

        var scriptPath = Path.Combine(_whisperRoot!, "transcribe.ps1");
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _whisperRoot,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(scriptPath);
        psi.ArgumentList.Add(next.Path);
        psi.ArgumentList.Add("-OutputFile");
        psi.ArgumentList.Add(next.TargetOutputPath!);
        psi.ArgumentList.Add("-Language");
        psi.ArgumentList.Add(language);
        psi.ArgumentList.Add("-Model");
        psi.ArgumentList.Add(model);
        if (timestamps)
        {
            psi.ArgumentList.Add("-Timestamps");
        }

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.OutputDataReceived += (s, e) =>
        {
            if (e.Data == null)
            {
                return;
            }
            const string progressMarker = "PROGRESS ";
            if (e.Data.StartsWith(progressMarker, StringComparison.Ordinal))
            {
                // Not logged: one line per segment would flood the log tab.
                var parts = e.Data[progressMarker.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3
                    && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct)
                    && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var totalSeconds))
                {
                    BeginInvoke(new Action(() => OnProgress(next, pct, totalSeconds)));
                }
                return;
            }
            const string writtenMarker = "Written: ";
            if (e.Data.StartsWith(writtenMarker, StringComparison.Ordinal))
            {
                next.WrittenOutputPath = e.Data[writtenMarker.Length..].Trim();
            }
            AppendLog(e.Data);
        };
        _process.ErrorDataReceived += (s, e) => { if (e.Data != null) AppendLog(e.Data); };
        _process.Exited += (s, e) => BeginInvoke(new Action(() => OnItemProcessExited(language, model, timestamps, skipExisting)));

        try
        {
            _process.Start();
        }
        catch (Exception ex)
        {
            AppendLog("Fehler beim Start: " + ex.Message);
            next.Status = QueueStatus.Error;
            RefreshQueueItem(next);
            _process.Dispose();
            _process = null;
            _currentItem = null;
            ProcessNextInQueue(language, model, timestamps, skipExisting);
            return;
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private void OnItemProcessExited(string language, string model, bool timestamps, bool skipExisting)
    {
        var exitCode = -1;
        try
        {
            // Exited can fire before the redirected streams are drained; without this
            // the "Written:" line may still be in flight.
            _process?.WaitForExit();
            exitCode = _process?.ExitCode ?? -1;
        }
        catch
        {
            // process may already be disposed if cancelled
        }

        var item = _currentItem;
        if (item != null)
        {
            if (exitCode == 0)
            {
                item.Status = QueueStatus.Done;
                AppendLog($"Fertig: {item.DisplayName}");
                TryLoadResultFor(item);
                _openFolderButton.Enabled = true;
            }
            else if (exitCode == ExitSkipped)
            {
                item.Status = QueueStatus.Skipped;
                AppendLog($"Übersprungen (keine Audiospur): {item.DisplayName}");
            }
            else if (item.Status == QueueStatus.Running)
            {
                item.Status = QueueStatus.Error;
                AppendLog($"Fehler bei {item.DisplayName} (Exit-Code {exitCode}).");
            }
            RefreshQueueItem(item);
        }

        _process?.Dispose();
        _process = null;
        _currentItem = null;
        _itemStopwatch.Reset();
        UpdateProgressDisplay();

        if (_cancelRequested)
        {
            _cancelRequested = false;
            FinishBatch(cancelled: true);
            return;
        }

        ProcessNextInQueue(language, model, timestamps, skipExisting);
    }

    private void FinishBatch(bool cancelled)
    {
        _batchRunning = false;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.MarqueeAnimationSpeed = 0;
        _progressBar.Value = cancelled ? 0 : ProgressScale;
        _elapsedTimer.Stop();
        _cancelButton.Enabled = false;
        UpdateStartButtonState();

        var items = _queueList.Items.Cast<QueueItem>().ToList();
        var done = items.Count(qi => qi.Status == QueueStatus.Done);
        var error = items.Count(qi => qi.Status == QueueStatus.Error);
        var skipped = items.Count(qi => qi.Status == QueueStatus.Skipped);
        var total = items.Count;

        if (cancelled)
        {
            _statusLabel.Text = $"Abgebrochen. {done}/{total} fertig.";
            _totalLabel.Text = $"Gesamtdauer {FormatElapsed(_stopwatch.Elapsed)}";
            AppendLog("Batch abgebrochen.");
        }
        else
        {
            _statusLabel.Text = $"Alle Dateien verarbeitet: {done} fertig, {skipped} übersprungen, {error} Fehler (von {total}).";
            _totalProgressBar.Value = ProgressScale;
            _totalLabel.Text = $"Gesamt 100 %, Dauer {FormatElapsed(_stopwatch.Elapsed)}";
            AppendLog("Alle Dateien verarbeitet.");
            if (done > 0)
            {
                _tabControl.SelectedTab = _resultTab;
            }
        }
    }

    // The name a transcript gets when nothing is in the way: output/<subfolder>/<stem>.txt,
    // mirroring the subfolder structure of the chosen root folder.
    private string ComputeNaturalOutputPath(QueueItem item)
    {
        var outDir = Path.Combine(_whisperRoot!, "output");
        if (!string.IsNullOrEmpty(item.RelativeDir))
        {
            outDir = Path.Combine(outDir, item.RelativeDir);
        }
        return Path.Combine(outDir, Path.GetFileNameWithoutExtension(item.Path) + ".txt");
    }

    // Never overwrite an existing transcript and never let two queue items land on the
    // same file (e.g. meeting.wav + meeting.mp3 in one folder). If the natural name is
    // taken, keep the source name and append a timestamp.
    private string ReserveOutputPath(QueueItem item, string naturalPath)
    {
        var taken = _queueList.Items.Cast<QueueItem>()
            .Where(qi => !ReferenceEquals(qi, item) && qi.TargetOutputPath != null)
            .Select(qi => qi.TargetOutputPath!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!taken.Contains(naturalPath) && !File.Exists(naturalPath))
        {
            return naturalPath;
        }

        var dir = Path.GetDirectoryName(naturalPath)!;
        var stem = Path.GetFileNameWithoutExtension(naturalPath);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        // Two same-stem items reserved within the same second share the timestamp,
        // so the counter (not the clock) is what keeps them apart.
        var candidate = Path.Combine(dir, $"{stem}_{stamp}.txt");
        var n = 2;
        while (taken.Contains(candidate) || File.Exists(candidate))
        {
            candidate = Path.Combine(dir, $"{stem}_{stamp}_{n}.txt");
            n++;
        }
        return candidate;
    }

    private void TryLoadResultFor(QueueItem item)
    {
        if (_whisperRoot is null)
        {
            return;
        }

        var expected = item.WrittenOutputPath
            ?? item.TargetOutputPath
            ?? Path.Combine(_whisperRoot, "output", Path.GetFileNameWithoutExtension(item.Path) + ".txt");
        if (!File.Exists(expected))
        {
            AppendLog("Erwartete Ausgabedatei nicht gefunden: " + expected);
            return;
        }

        try
        {
            item.OutputPath = expected;
            _outputTxtPath = expected;
            _resultBox.Text = $"=== {item.DisplayName} ==={Environment.NewLine}{Environment.NewLine}" + File.ReadAllText(expected, Encoding.UTF8);
            _openResultButton.Enabled = true;
        }
        catch (Exception ex)
        {
            AppendLog("Konnte Ergebnis nicht laden: " + ex.Message);
        }
    }

    private void RefreshQueueItem(QueueItem item)
    {
        var idx = _queueList.Items.IndexOf(item);
        if (idx >= 0)
        {
            _queueList.Items[idx] = item;
        }
    }

    private void CancelTranscription()
    {
        if (_process is null || _process.HasExited)
        {
            return;
        }
        _cancelRequested = true;
        if (_currentItem != null)
        {
            _currentItem.Status = QueueStatus.Cancelled;
            RefreshQueueItem(_currentItem);
        }
        try
        {
            _process.Kill(entireProcessTree: true);
            AppendLog("Abgebrochen durch Benutzer.");
        }
        catch (Exception ex)
        {
            AppendLog("Fehler beim Abbrechen: " + ex.Message);
        }
    }

    private void OpenOutputFolder()
    {
        if (_whisperRoot is null)
        {
            return;
        }
        var outDir = Path.Combine(_whisperRoot, "output");
        Directory.CreateDirectory(outDir);
        Process.Start(new ProcessStartInfo { FileName = outDir, UseShellExecute = true });
    }

    private void OpenResultInEditor()
    {
        if (_outputTxtPath != null && File.Exists(_outputTxtPath))
        {
            Process.Start(new ProcessStartInfo { FileName = _outputTxtPath, UseShellExecute = true });
        }
    }

    private void SaveSettings(string language, string model, bool timestamps, bool skipExisting)
    {
        if (_settingsPath is null)
        {
            return;
        }
        _settings.Language = language;
        _settings.Model = model;
        _settings.Timestamps = timestamps;
        _settings.SkipExisting = skipExisting;
        _settings.Save(_settingsPath);
    }

    private static string FormatElapsed(TimeSpan ts) => ts.ToString(@"hh\:mm\:ss");

    private void OnProgress(QueueItem item, double percent, double audioSeconds)
    {
        if (!ReferenceEquals(item, _currentItem) || item.Status != QueueStatus.Running)
        {
            return;
        }
        var changedWholePercent = item.Percent is null || (int)item.Percent != (int)percent;
        item.Percent = percent;
        _itemAudioSeconds = audioSeconds;
        if (_progressBar.Style != ProgressBarStyle.Continuous)
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.MarqueeAnimationSpeed = 0;
        }
        _progressBar.Value = Math.Clamp((int)(percent / 100.0 * ProgressScale), 0, ProgressScale);
        if (changedWholePercent)
        {
            // Rewriting a ListBox item flickers, so only do it when the visible number changes.
            RefreshQueueItem(item);
        }
        UpdateProgressDisplay();
    }

    private void UpdateProgressDisplay()
    {
        var items = _queueList.Items.Cast<QueueItem>().ToList();
        var total = items.Count;
        if (total == 0)
        {
            return;
        }

        // Finished, skipped and failed files all count as handled; the running one
        // contributes its own share.
        var handled = items.Count(qi => qi.Status is QueueStatus.Done or QueueStatus.Skipped or QueueStatus.Error);
        var current = _currentItem?.Percent ?? 0;
        var overall = (handled + current / 100.0) / total;
        _totalProgressBar.Value = Math.Clamp((int)(overall * ProgressScale), 0, ProgressScale);

        if (!_batchRunning)
        {
            return;
        }

        _totalLabel.Text = $"Gesamt {overall * 100:0} % ({handled}/{total} Dateien erledigt), verstrichen {FormatElapsed(_stopwatch.Elapsed)}";

        if (_currentItem is null)
        {
            return;
        }
        var idx = items.IndexOf(_currentItem) + 1;
        var head = $"Datei {idx}/{total}: {_currentItem.FileName}";
        var itemElapsed = _itemStopwatch.Elapsed;
        if (_currentItem.Percent is not { } pct)
        {
            _statusLabel.Text = $"{head} – Modell laden / Audio vorbereiten... ({FormatElapsed(itemElapsed)})";
            return;
        }

        var text = $"{head} – {pct:0.0} %";
        if (_itemAudioSeconds > 0)
        {
            text += $" von {FormatElapsed(TimeSpan.FromSeconds(_itemAudioSeconds))} Audio";
        }
        text += $", läuft {FormatElapsed(itemElapsed)}";
        // Extrapolating from the first percent or two is wildly off, wait for a base.
        if (pct >= 2 && pct < 100)
        {
            var remaining = TimeSpan.FromSeconds(itemElapsed.TotalSeconds * (100 - pct) / pct);
            text += $", Rest ca. {FormatElapsed(remaining)}";
        }
        _statusLabel.Text = text;
    }

    private void AppendLog(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(AppendLog), line);
            return;
        }
        _logBox.AppendText(line + Environment.NewLine);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_process != null && !_process.HasExited)
        {
            var result = MessageBox.Show(this, "Transkription läuft noch. Wirklich beenden und abbrechen?", "Whisper", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch
            {
                // best effort cleanup on close
            }
        }
        base.OnFormClosing(e);
    }
}
