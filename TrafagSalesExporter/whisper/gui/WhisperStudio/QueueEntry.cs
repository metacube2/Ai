using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using WhisperStudio.Core;

namespace WhisperStudio;

public enum EntryStatus { Pending, Running, Done, Error, Cancelled, Skipped }

public sealed class QueueEntry : INotifyPropertyChanged
{
    private static readonly Brush Green = Frozen(0x2E, 0x9E, 0x44);
    private static readonly Brush Red = Frozen(0xC8, 0x10, 0x2E);
    private static readonly Brush Blue = Frozen(0x00, 0x78, 0xD4);
    private static readonly Brush Gray = Frozen(0x80, 0x80, 0x80);

    private DateTime _recordedAt;
    private string _title;
    private EntryStatus _status = EntryStatus.Pending;
    private double _percent;
    private bool _hasProgress;
    private string? _message;
    private readonly Stopwatch _clock = new();

    public QueueEntry(string path, string relativeDir)
    {
        Path = path;
        RelativeDir = relativeDir;
        _recordedAt = OutputNaming.SuggestRecordedAt(path);
        _title = OutputNaming.SuggestTitle(path);
    }

    public string Path { get; }
    public string RelativeDir { get; }
    public string? OutputPath { get; set; }
    public double AudioSeconds { get; set; }
    public string DisplayName => string.IsNullOrEmpty(RelativeDir) ? System.IO.Path.GetFileName(Path) : System.IO.Path.Combine(RelativeDir, System.IO.Path.GetFileName(Path));

    public DateTime RecordedAt
    {
        get => _recordedAt;
        set { _recordedAt = value; Changed(); Changed(nameof(RecordedAtText)); }
    }

    public string RecordedAtText
    {
        get => _recordedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        set
        {
            if (DateTime.TryParseExact(value.Trim(), new[] { "dd.MM.yyyy HH:mm", "d.M.yyyy H:mm", "dd.MM.yyyy" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                RecordedAt = parsed;
            }
            else
            {
                Changed(); // snap the cell back to the valid value
            }
        }
    }

    public string Title
    {
        get => _title;
        set { _title = OutputNaming.Sanitize(value); Changed(); }
    }

    public string Stem => OutputNaming.StemFor(Path, RecordedAt, Title);

    public EntryStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            if (value == EntryStatus.Running)
            {
                _percent = 0;
                _hasProgress = false;
                _message = null;
                _clock.Restart();
            }
            else
            {
                _clock.Stop();
            }
            if (value == EntryStatus.Done)
            {
                _percent = 100;
            }
            RaiseAll();
        }
    }

    public string? Message
    {
        get => _message;
        set { _message = value; Changed(nameof(ProgressText)); Changed(nameof(StatusText)); }
    }

    public double Percent => _percent;
    public bool IsPreparing => _status == EntryStatus.Running && !_hasProgress;
    public TimeSpan Elapsed => _clock.Elapsed;

    public void ReportProgress(double percent, double audioSeconds)
    {
        _percent = percent;
        _hasProgress = true;
        AudioSeconds = audioSeconds;
        Changed(nameof(Percent));
        Changed(nameof(IsPreparing));
        Changed(nameof(ProgressText));
    }

    /// <summary>Linear estimate from the time spent so far; useless before ~2 %.</summary>
    public TimeSpan? Remaining => _hasProgress && _percent >= 2 && _percent < 100
        ? TimeSpan.FromSeconds(_clock.Elapsed.TotalSeconds * (100 - _percent) / _percent)
        : null;

    public void Tick() => Changed(nameof(ProgressText));

    public string ProgressText => _status switch
    {
        EntryStatus.Pending => "wartet",
        EntryStatus.Running when !_hasProgress => $"Modell laden, Audio vorbereiten · {_clock.Elapsed:hh\\:mm\\:ss}",
        EntryStatus.Running => Remaining is { } r ? $"{_percent:0} % · Rest ca. {r:hh\\:mm\\:ss}" : $"{_percent:0} %",
        EntryStatus.Done => _message ?? "fertig",
        EntryStatus.Skipped => _message ?? "übersprungen",
        EntryStatus.Cancelled => "abgebrochen",
        EntryStatus.Error => _message ?? "Fehler",
        _ => "",
    };

    public string StatusText => _status switch
    {
        EntryStatus.Pending => "Wartet",
        EntryStatus.Running => "Läuft",
        EntryStatus.Done => "Fertig",
        EntryStatus.Error => "Fehler" + (_message is null ? "" : ": " + _message),
        EntryStatus.Cancelled => "Abgebrochen",
        EntryStatus.Skipped => "Übersprungen" + (_message is null ? "" : ": " + _message),
        _ => "",
    };

    public string StatusGlyph => _status switch
    {
        EntryStatus.Pending => "",
        EntryStatus.Running => "",
        EntryStatus.Done => "",
        EntryStatus.Error => "",
        EntryStatus.Cancelled => "",
        EntryStatus.Skipped => "",
        _ => "",
    };

    public Brush StatusBrush => _status switch
    {
        EntryStatus.Done => Green,
        EntryStatus.Error => Red,
        EntryStatus.Running => Blue,
        _ => Gray,
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RaiseAll()
    {
        foreach (var n in new[] { nameof(Status), nameof(Percent), nameof(IsPreparing), nameof(ProgressText), nameof(StatusText), nameof(StatusGlyph), nameof(StatusBrush) })
        {
            Changed(n);
        }
    }

    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}

public sealed class HistoryEntry
{
    public required string TxtPath { get; init; }
    public required DateTime Date { get; init; }
    public required string Title { get; init; }
    public required string Folder { get; init; }
    public required string Extras { get; init; }
    public required long Size { get; init; }
    public string DateText => Date.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    public string SizeText => Size < 1024 ? $"{Size} B" : $"{Size / 1024.0:0.#} KB";

    /// <summary>Date from the "2026-10-22_1430_" prefix, else the file time (old names like audio.txt).</summary>
    public static HistoryEntry From(string txtPath, string outputRoot)
    {
        var stem = System.IO.Path.GetFileNameWithoutExtension(txtPath);
        var date = File.GetLastWriteTime(txtPath);
        var title = stem;
        if (stem.Length >= 15 && DateTime.TryParseExact(stem[..15], OutputNaming.StampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            date = d;
            title = stem.Length > 16 ? stem[16..] : "(ohne Titel)";
        }
        var dir = System.IO.Path.GetDirectoryName(txtPath)!;
        var folder = System.IO.Path.GetRelativePath(outputRoot, dir);
        var extras = new List<string>();
        foreach (var (ext, label) in new[] { (".docx", "Word"), (".srt", "SRT"), (".vtt", "VTT") })
        {
            if (File.Exists(System.IO.Path.ChangeExtension(txtPath, ext))) extras.Add(label);
        }
        if (File.Exists(System.IO.Path.Combine(dir, stem + "_protokoll.md"))) extras.Add("Protokoll");
        return new HistoryEntry
        {
            TxtPath = txtPath,
            Date = date,
            Title = title,
            Folder = folder == "." ? "" : folder,
            Extras = string.Join(", ", extras),
            Size = new FileInfo(txtPath).Length,
        };
    }
}
