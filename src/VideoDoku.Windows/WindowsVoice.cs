using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VideoDoku.Core;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;

namespace VideoDoku.Windows;

/// <summary>
/// Lokale Windows-Stimmen (z. B. „Microsoft Karsten – German (Switzerland)“). Ersatz, solange der
/// Sprachdienst tts.ingok.ch noch nicht steht, und zum Ausprobieren ohne Netz. Liefert WAV.
/// </summary>
public sealed class WindowsVoiceProvider : ITtsProvider
{
    public string Name => "Windows-Stimme";
    public string Extension => ".wav";

    public Task<IReadOnlyList<VoiceOption>> VoicesAsync(CancellationToken ct)
    {
        IReadOnlyList<VoiceOption> list = SpeechSynthesizer.AllVoices
            .OrderByDescending(v => v.Language.StartsWith("de", StringComparison.OrdinalIgnoreCase))
            .ThenBy(v => v.DisplayName)
            .Select(v => new VoiceOption(Name, v.Id, $"{v.DisplayName.Replace("Microsoft ", "")} ({v.Language}, Windows lokal)"))
            .ToList();
        return Task.FromResult(list);
    }

    public async Task<byte[]> SpeakAsync(string text, string voiceId, string language, CancellationToken ct)
    {
        using var synth = new SpeechSynthesizer();
        var voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Id == voiceId)
                    ?? SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Language.StartsWith(language, StringComparison.OrdinalIgnoreCase))
                    ?? SpeechSynthesizer.DefaultVoice;
        synth.Voice = voice;
        // Absaetze (Leerzeile im Skript) als kurze Pause sprechen.
        var spoken = text.Replace("\n\n", ". ").Replace('\n', ' ');
        using SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(spoken).AsTask(ct);
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        var size = (uint)stream.Size;
        await reader.LoadAsync(size).AsTask(ct);
        var bytes = new byte[size];
        reader.ReadBytes(bytes);
        return bytes;
    }
}
