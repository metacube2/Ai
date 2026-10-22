using System.IO;
using System.Text.Json;

namespace WhisperStudio.Core;

public sealed class StudioSettings
{
    public string Language { get; set; } = "de";
    public string Model { get; set; } = "medium";
    public string Task { get; set; } = "transcribe";
    public bool Timestamps { get; set; }
    public bool SkipExisting { get; set; } = true;
    public bool ExportSrt { get; set; }
    public bool ExportVtt { get; set; }
    public bool ExportDocx { get; set; } = true;
    public string? LastFolder { get; set; }
    public string? MicDeviceId { get; set; }
    public bool RecordSystemAudio { get; set; } = true;
    public bool TranscribeAfterRecording { get; set; } = true;
    public string Theme { get; set; } = "System";

    // Local LLM (llama.cpp llama-server, OpenAI-compatible). Base URL + model are plain
    // settings, so another OpenAI-compatible endpoint is a config change, not a code change.
    public string LlmBaseUrl { get; set; } = "http://127.0.0.1:8088/v1";
    public string LlmModel { get; set; } = "qwen2.5-3b-instruct";
    public bool LlmAutoStart { get; set; } = true;

    public static StudioSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<StudioSettings>(File.ReadAllText(path)) ?? new StudioSettings();
            }
        }
        catch
        {
            // corrupt settings fall back to defaults
        }
        return new StudioSettings();
    }

    public void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // best effort
        }
    }
}

/// <summary>Locations inside the portable whisper folder.</summary>
public sealed class StudioPaths
{
    public string Root { get; }
    public string Output => Path.Combine(Root, "output");
    public string Input => Path.Combine(Root, "input");
    public string Temp => Path.Combine(Root, "temp");
    public string Settings => Path.Combine(Root, "cache", "studio_settings.json");
    public string TranscribeScript => Path.Combine(Root, "transcribe.ps1");
    public string LlmDir => Path.Combine(Root, "llm");

    private StudioPaths(string root) => Root = root;

    /// <summary>Walks up from the exe to the folder holding transcribe.ps1 (works from bin\ and publish\).</summary>
    public static StudioPaths? Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "transcribe.ps1")))
            {
                return new StudioPaths(dir.FullName);
            }
        }
        return null;
    }
}
