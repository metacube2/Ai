using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VideoDoku.Windows;

/// <summary>Einstellungen unter %APPDATA%\VideoDoku. Der Schluessel wird mit Windows-DPAPI verschluesselt.</summary>
public sealed class AppSettings
{
    public string ServiceUrl { get; set; } = VideoDoku.Core.IngokTtsClient.DefaultBaseUrl;
    public string? LastVoice { get; set; }
    public string Language { get; set; } = "de";
    public bool Subtitles { get; set; } = true;
    public bool KenBurns { get; set; } = true;
    public string? LastFolder { get; set; }
    public double Scale { get; set; } = 1.3;

    public static string Folder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoDoku");

    private static string FilePath => Path.Combine(Folder, "einstellungen.json");
    private static string KeyPath => Path.Combine(Folder, "schluessel.bin");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath)) return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch (Exception) { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        }
        catch (Exception) { }
    }

    /// <summary>Schluessel fuer den Sprachdienst, nur fuer diesen Windows-Benutzer lesbar.</summary>
    public static string? LoadKey()
    {
        try
        {
            if (!File.Exists(KeyPath)) return null;
            var plain = ProtectedData.Unprotect(File.ReadAllBytes(KeyPath), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void SaveKey(string? key)
    {
        Directory.CreateDirectory(Folder);
        if (string.IsNullOrWhiteSpace(key))
        {
            if (File.Exists(KeyPath)) File.Delete(KeyPath);
            return;
        }
        var cipher = ProtectedData.Protect(Encoding.UTF8.GetBytes(key.Trim()), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(KeyPath, cipher);
    }
}
