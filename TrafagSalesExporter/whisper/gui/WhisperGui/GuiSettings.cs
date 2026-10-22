using System;
using System.IO;
using System.Text.Json;

namespace WhisperGui;

public sealed class GuiSettings
{
    public string Language { get; set; } = "de";
    public string Model { get; set; } = "medium";
    public bool Timestamps { get; set; }
    public bool SkipExisting { get; set; } = true;
    public string? LastFolder { get; set; }

    public static GuiSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<GuiSettings>(json);
                if (settings != null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // ignore corrupt/missing settings file, fall back to defaults
        }
        return new GuiSettings();
    }

    public void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // best effort only, settings persistence is not critical
        }
    }
}
