using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using VideoDoku.Core;

namespace VideoDoku.App;

/// <summary>Eine Szene in der Liste: Vorschaubild, bearbeitbarer Text, Fortschritt.</summary>
public sealed class SceneVm : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public Scene Scene { get; }
    public int Number => Scene.Number;
    public string Header => $"Szene {Scene.Number}" + (Scene.Origin is { } o ? $" · {o}" : "") +
                            (Scene.Kind == VisualKind.Video ? " · Clip" : "");
    public string FileName => Path.GetFileName(Scene.VisualPath);

    /// <summary>Sprechertext; Aenderungen gehen direkt in die Szene.</summary>
    public string Text
    {
        get => Scene.Text;
        set
        {
            if (Scene.Text == value) return;
            Scene.Text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CharInfo)));
        }
    }

    public string CharInfo => Scene.HasText ? $"{Scene.Text.Length} Zeichen" : "kein Text";

    private BitmapImage? _thumb;
    public BitmapImage? Thumb => _thumb ??= LoadThumb();

    private string _status = "wartet";
    private double _progress;
    private string _progressText = "";
    private string _statusKey = "Waiting";
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string StatusKey { get => _statusKey; private set => Set(ref _statusKey, value); }
    public double Progress { get => _progress; private set => Set(ref _progress, value); }
    public string ProgressText { get => _progressText; private set => Set(ref _progressText, value); }

    public SceneVm(Scene scene) { Scene = scene; }

    public void Reset()
    {
        Status = "wartet";
        StatusKey = "Waiting";
        Progress = 0;
        ProgressText = "";
    }

    public void Apply(SceneProgress p)
    {
        // Stimme 0–40 %, Szene 40–100 %
        (Status, StatusKey, Progress) = (p.Voice, p.Render) switch
        {
            (StepState.Failed, _) => ($"Stimme: {p.Error}", "Failed", 0.0),
            (_, StepState.Failed) => ($"Szene: {p.Error}", "Failed", 40.0),
            (_, StepState.Done) => ("fertig", "Done", 100.0),
            (_, StepState.Running) => ("Szene wird gerendert", "Running", 40 + 60 * p.RenderFraction),
            (StepState.Running, _) => ("Stimme wird geholt", "Running", 10.0),
            (StepState.Done, _) => ("Stimme da", "Running", 40.0),
            (StepState.Skipped, _) => ("ohne Stimme", "Running", 40.0),
            _ => ("wartet", "Waiting", 0.0),
        };
        ProgressText = Progress > 0 ? $"{Progress:0} %" : "";
    }

    private BitmapImage? LoadThumb()
    {
        if (Scene.Kind != VisualKind.Image || !File.Exists(Scene.VisualPath)) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // Datei nicht sperren
            bmp.DecodePixelWidth = 240;
            bmp.UriSource = new Uri(Scene.VisualPath);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
