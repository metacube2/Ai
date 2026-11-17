using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using VideoDoku.Core;

namespace VideoDoku.Windows;

/// <summary>
/// Liest eine PowerPoint-Datei ueber das installierte PowerPoint (COM): jede sichtbare Folie wird als
/// PNG (1920×1080) exportiert, die Sprechernotizen werden zum Sprechertext. Ausgeblendete Folien
/// werden uebersprungen. Ein von Ingo bereits geoeffnetes PowerPoint wird nicht beendet.
/// </summary>
public static class PowerPointImport
{
    private const int MsoTrue = -1, MsoFalse = 0;
    private const int PpPlaceholderBody = 2;

    public static DokuProject Import(string pptxPath, string slideFolder, IProgress<string>? progress, CancellationToken ct)
    {
        // PowerPoint scheitert beim Export an Pfaden mit „/“ („kann ^0 auf ^1 nicht speichern“): normalisieren.
        var full = Path.GetFullPath(pptxPath);
        slideFolder = Path.GetFullPath(slideFolder);
        Directory.CreateDirectory(slideFolder);
        var wasRunning = Process.GetProcessesByName("POWERPNT").Length > 0;

        var type = Type.GetTypeFromProgID("PowerPoint.Application")
                   ?? throw new InvalidOperationException("PowerPoint ist nicht installiert.");
        dynamic? app = null;
        dynamic? pres = null;
        try
        {
            app = Activator.CreateInstance(type)!;
            // Open(FileName, ReadOnly, Untitled, WithWindow)
            pres = app.Presentations.Open(full, MsoTrue, MsoFalse, MsoFalse);
            var project = new DokuProject
            {
                Title = Path.GetFileNameWithoutExtension(full),
                BaseFolder = Path.GetDirectoryName(full) ?? "",
            };

            int count = pres.Slides.Count;
            for (var i = 1; i <= count; i++)
            {
                ct.ThrowIfCancellationRequested();
                dynamic slide = pres.Slides[i];
                if ((int)slide.SlideShowTransition.Hidden == MsoTrue) continue;

                progress?.Report($"Folie {i}/{count} exportieren");
                var png = Path.Combine(slideFolder, $"folie-{i:000}.png");
                slide.Export(png, "PNG", 1920, 1080);

                project.Scenes.Add(new Scene
                {
                    Number = project.Scenes.Count + 1,
                    VisualPath = png,
                    Kind = VisualKind.Image,
                    Text = Notes(slide),
                    Origin = $"Folie {i}",
                });
            }
            return project;
        }
        finally
        {
            try { pres?.Close(); } catch (Exception) { }
            if (pres is not null) Marshal.FinalReleaseComObject(pres);
            if (app is not null)
            {
                if (!wasRunning)
                {
                    try { app.Quit(); } catch (Exception) { }
                }
                Marshal.FinalReleaseComObject(app);
            }
        }
    }

    private static string Notes(dynamic slide)
    {
        try
        {
            if ((int)slide.HasNotesPage != MsoTrue) return "";
            foreach (dynamic shape in slide.NotesPage.Shapes)
            {
                if ((int)shape.Type != 14) continue; // msoPlaceholder
                if ((int)shape.PlaceholderFormat.Type != PpPlaceholderBody) continue;
                if ((int)shape.HasTextFrame != MsoTrue) continue;
                string text = shape.TextFrame.TextRange.Text ?? "";
                // PowerPoint trennt Absaetze mit \r.
                return ScriptNormalize(text.Replace("\r", "\n"));
            }
        }
        catch (Exception)
        {
            // Folie ohne lesbare Notizen: ohne Text weiter (Bild steht ein paar Sekunden).
        }
        return "";
    }

    private static string ScriptNormalize(string text)
    {
        var paragraphs = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return string.Join("\n\n", paragraphs);
    }
}
