using System.Globalization;

namespace VideoDoku.Core;

/// <summary>
/// Baut die ffmpeg-Argumente. Reine Funktionen ohne Prozessaufruf, damit sie testbar sind.
/// Alle Szenen werden mit denselben Parametern codiert (H.264 yuv420p, AAC 48 kHz Stereo),
/// damit das Zusammensetzen danach ohne Neucodierung ueber den concat-Demuxer geht.
/// </summary>
public static class FfmpegCommands
{
    /// <summary>Dauer einer Szene in Sekunden.</summary>
    public static double SceneDuration(Scene scene, double? voiceSeconds, double? clipSeconds, RenderOptions o)
    {
        if (scene.HasText && voiceSeconds is { } v)
            return Round(o.LeadIn + v + scene.PauseAfter);
        if (scene.Kind == VisualKind.Video && clipSeconds is { } c)
            return Round(c);
        return Round(Math.Max(1.0, scene.SilentSeconds));
    }

    /// <summary>Argumente fuer eine Szene. <paramref name="voicePath"/> ist null, wenn die Szene keinen Text hat.</summary>
    public static IReadOnlyList<string> Scene(
        Scene scene, string? voicePath, bool clipHasAudio, double duration, string output, RenderOptions o)
    {
        var args = new List<string> { "-hide_banner", "-y", "-nostdin", "-loglevel", "error", "-progress", "pipe:1" };
        var d = F(duration);
        var fadeOut = F(Math.Max(0, duration - o.Fade));
        var size = $"{o.Width}:{o.Height}";
        var fades = $"fade=t=in:st=0:d={F(o.Fade)},fade=t=out:st={fadeOut}:d={F(o.Fade)}";
        var fit = $"scale={size}:force_original_aspect_ratio=decrease,pad={size}:(ow-iw)/2:(oh-ih)/2:color=black,setsar=1";
        string video;

        // Eingang 0: Bild oder Clip
        if (scene.Kind == VisualKind.Image)
        {
            if (o.KenBurns)
            {
                var frames = (int)Math.Ceiling(duration * o.Fps);
                args.AddRange(["-i", scene.VisualPath]);
                // Erst auf doppelte Groesse fuellend skalieren, dann langsam von 100 % auf 110 % zoomen.
                video = $"[0:v]scale={o.Width * 2}:{o.Height * 2}:force_original_aspect_ratio=increase,crop={o.Width * 2}:{o.Height * 2}," +
                        $"zoompan=z='1+0.10*on/{frames}':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d={frames}:s={o.Width}x{o.Height}:fps={o.Fps}," +
                        $"setsar=1,format=yuv420p,{fades}[v]";
            }
            else
            {
                args.AddRange(["-loop", "1", "-framerate", o.Fps.ToString(CultureInfo.InvariantCulture), "-i", scene.VisualPath]);
                video = $"[0:v]{fit},format=yuv420p,{fades}[v]";
            }
        }
        else
        {
            args.AddRange(["-i", scene.VisualPath]);
            // Ist der Clip kuerzer als die Szene, bleibt das letzte Bild stehen.
            video = $"[0:v]{fit},fps={o.Fps},format=yuv420p,tpad=stop_mode=clone:stop_duration={d},trim=0:{d},setpts=PTS-STARTPTS,{fades}[v]";
        }

        // Eingang 1: Stimme (optional)
        var hasVoice = voicePath is not null;
        if (hasVoice) args.AddRange(["-i", voicePath!]);

        var ms = ((int)Math.Round(o.LeadIn * 1000)).ToString(CultureInfo.InvariantCulture);
        var fmt = $"aresample=48000,aformat=sample_fmts=fltp:channel_layouts=stereo";
        string audio;
        var useClipAudio = scene.Kind == VisualKind.Video && clipHasAudio;

        if (hasVoice && useClipAudio)
            audio = $"[0:a]{fmt},volume={F(o.ClipVolumeUnderVoice)},apad[ca];[1:a]{fmt},adelay={ms}|{ms},apad[va];" +
                    $"[ca][va]amix=inputs=2:duration=longest:normalize=0,atrim=0:{d}[a]";
        else if (hasVoice)
            audio = $"[1:a]{fmt},adelay={ms}|{ms},apad,atrim=0:{d}[a]";
        else if (useClipAudio)
            audio = $"[0:a]{fmt},apad,atrim=0:{d},afade=t=in:st=0:d={F(o.Fade)},afade=t=out:st={fadeOut}:d={F(o.Fade)}[a]";
        else
            audio = $"anullsrc=r=48000:cl=stereo,atrim=0:{d},{fmt}[a]";

        args.AddRange(["-filter_complex", $"{video};{audio}", "-map", "[v]", "-map", "[a]", "-t", d]);
        args.AddRange(Encode(o));
        args.Add(output);
        return args;
    }

    /// <summary>
    /// Setzt die Szenen zusammen, legt optional Musik (beim Sprechen automatisch leiser) und
    /// Untertitel (als abschaltbare Spur) dazu.
    /// </summary>
    public static IReadOnlyList<string> Final(
        string concatListPath, string? musicPath, string? srtPath, string title, string output, RenderOptions o)
    {
        var args = new List<string> { "-hide_banner", "-y", "-nostdin", "-loglevel", "error", "-progress", "pipe:1",
            "-f", "concat", "-safe", "0", "-i", concatListPath };
        var next = 1;
        int? music = null, subs = null;
        if (musicPath is not null) { args.AddRange(["-stream_loop", "-1", "-i", musicPath]); music = next++; }
        if (srtPath is not null) { args.AddRange(["-i", srtPath]); subs = next++; }

        args.AddRange(["-map", "0:v", "-c:v", "copy"]);
        if (music is { } m)
        {
            // Musik wird ueber die Stimme „geduckt“ (sidechaincompress) und endet mit dem Video.
            args.AddRange(["-filter_complex",
                $"[{m}:a]aresample=48000,aformat=channel_layouts=stereo,volume={F(o.MusicVolume)}[mu];" +
                "[0:a]asplit=2[voice][key];" +
                "[mu][key]sidechaincompress=threshold=0.02:ratio=10:attack=15:release=600[duck];" +
                "[voice][duck]amix=inputs=2:duration=first:normalize=0[a]",
                "-map", "[a]", "-c:a", "aac", "-b:a", "192k"]);
        }
        else
        {
            args.AddRange(["-map", "0:a", "-c:a", "copy"]);
        }
        if (subs is { } s)
            args.AddRange(["-map", $"{s}:s", "-c:s", "mov_text", "-metadata:s:s:0", "language=ger"]);

        args.AddRange(["-metadata", $"title={title}", "-movflags", "+faststart", output]);
        return args;
    }

    /// <summary>Inhalt der Liste fuer den concat-Demuxer.</summary>
    public static string ConcatList(IEnumerable<string> segmentPaths) =>
        string.Concat(segmentPaths.Select(p => $"file '{p.Replace('\\', '/').Replace("'", @"'\''")}'\n"));

    public static IReadOnlyList<string> Probe(string path) =>
        ["-v", "error", "-show_entries", "format=duration:stream=codec_type", "-of", "default=noprint_wrappers=1", path];

    private static IEnumerable<string> Encode(RenderOptions o) =>
    [
        "-r", o.Fps.ToString(CultureInfo.InvariantCulture),
        "-c:v", "libx264", "-preset", o.Preset, "-crf", o.Crf.ToString(CultureInfo.InvariantCulture),
        "-pix_fmt", "yuv420p", "-profile:v", "high",
        "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2",
    ];

    private static double Round(double s) => Math.Round(s, 3);

    internal static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
