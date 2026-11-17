using System.Globalization;
using System.Text;

namespace VideoDoku.Core;

/// <summary>
/// Schreibt ein Projekt im Skript-Format (Gegenstueck zu <see cref="ScriptParser"/>), z. B. nachdem
/// eine KI ueber MCP Texte geaendert hat. Pfade werden, wo moeglich, relativ zum Skriptordner geschrieben.
/// </summary>
public static class ScriptWriter
{
    public static string Write(DokuProject project, string scriptFolder)
    {
        var sb = new StringBuilder();
        sb.Append("titel: ").Append(project.Title).Append('\n');
        if (project.MusicPath is { } m) sb.Append("musik: ").Append(Rel(m, scriptFolder)).Append('\n');

        foreach (var s in project.Scenes)
        {
            sb.Append('\n');
            sb.Append(s.Kind == VisualKind.Video ? "[clip: " : "[bild: ").Append(Rel(s.VisualPath, scriptFolder)).Append("]\n");
            if (s.HasText)
            {
                foreach (var paragraph in s.Text.Split("\n\n"))
                    sb.Append(paragraph.Trim()).Append("\n\n");
                sb.Length -= 1; // letzte Leerzeile des Textes weglassen
            }
            if (Math.Abs(s.PauseAfter - 0.8) > 0.001) sb.Append("[pause: ").Append(F(s.PauseAfter)).Append("]\n");
            if (!s.HasText && s.Kind == VisualKind.Image && Math.Abs(s.SilentSeconds - 4) > 0.001)
                sb.Append("[dauer: ").Append(F(s.SilentSeconds)).Append("]\n");
        }
        return sb.ToString();
    }

    public static void Save(DokuProject project, string scriptPath) =>
        File.WriteAllText(scriptPath, Write(project, Path.GetDirectoryName(Path.GetFullPath(scriptPath)) ?? ""), new UTF8Encoding(false));

    private static string Rel(string path, string folder)
    {
        var rel = Path.GetRelativePath(folder, path);
        // Auf einem anderen Laufwerk liefert GetRelativePath den absoluten Pfad: dann in Anfuehrungszeichen.
        if (Path.IsPathRooted(rel)) return rel.Contains(' ') ? $"\"{rel}\"" : rel;
        return rel.Replace('\\', '/');
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
