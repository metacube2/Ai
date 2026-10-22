using System.Diagnostics;
using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace WhisperStudio.Core;

public sealed record AudioDevice(string Id, string Name, bool IsDefault)
{
    public override string ToString() => IsDefault ? $"{Name} (Standard)" : Name;
}

/// <summary>
/// Records the microphone and, optionally, the system sound (what Teams plays back) via
/// WASAPI loopback, then mixes both into one 16 kHz mono WAV for Whisper.
/// Stopping is cooperative: the writers are disposed so the WAV headers are complete.
/// </summary>
public sealed class Recorder : IDisposable
{
    private WasapiCapture? _mic;
    private WasapiLoopbackCapture? _loop;
    private WaveFileWriter? _micWriter;
    private WaveFileWriter? _loopWriter;
    private readonly Stopwatch _clock = new();
    private long _loopFramesWritten;
    private string? _micTemp;
    private string? _loopTemp;
    private TaskCompletionSource? _micStopped;
    private TaskCompletionSource? _loopStopped;

    public event Action<float>? Level; // peak 0..1 of the microphone
    public TimeSpan Elapsed => _clock.Elapsed;
    public bool IsRecording => _clock.IsRunning;

    public static List<AudioDevice> Microphones()
    {
        using var en = new MMDeviceEnumerator();
        string? defId = null;
        try { defId = en.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).ID; } catch { /* no mic */ }
        return en.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(d => new AudioDevice(d.ID, d.FriendlyName, d.ID == defId))
            .OrderByDescending(d => d.IsDefault)
            .ToList();
    }

    public void Start(string? micDeviceId, bool withSystemAudio, string tempDir)
    {
        Directory.CreateDirectory(tempDir);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        using var en = new MMDeviceEnumerator();
        var micDevice = micDeviceId != null
            ? en.GetDevice(micDeviceId)
            : en.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);

        _micTemp = Path.Combine(tempDir, $"rec_{stamp}_mic.wav");
        _mic = new WasapiCapture(micDevice);
        _micWriter = new WaveFileWriter(_micTemp, _mic.WaveFormat);
        _micStopped = new TaskCompletionSource();
        _mic.DataAvailable += (_, e) =>
        {
            _micWriter?.Write(e.Buffer, 0, e.BytesRecorded);
            Level?.Invoke(Peak(e.Buffer, e.BytesRecorded, _mic.WaveFormat));
        };
        _mic.RecordingStopped += (_, _) => _micStopped.TrySetResult();

        if (withSystemAudio)
        {
            _loopTemp = Path.Combine(tempDir, $"rec_{stamp}_system.wav");
            _loop = new WasapiLoopbackCapture();
            _loopWriter = new WaveFileWriter(_loopTemp, _loop.WaveFormat);
            _loopFramesWritten = 0;
            _loopStopped = new TaskCompletionSource();
            _loop.DataAvailable += (_, e) => WriteLoopback(e.Buffer, e.BytesRecorded);
            _loop.RecordingStopped += (_, _) => _loopStopped.TrySetResult();
        }

        _clock.Restart();
        _mic.StartRecording();
        _loop?.StartRecording();
    }

    // WASAPI loopback delivers nothing while the system is silent. Without padding,
    // the system track would shrink and drift against the microphone track.
    private void WriteLoopback(byte[] buffer, int bytes)
    {
        if (_loopWriter is null || _loop is null)
        {
            return;
        }
        var fmt = _loop.WaveFormat;
        var incomingFrames = bytes / fmt.BlockAlign;
        PadLoopbackTo((long)(_clock.Elapsed.TotalSeconds * fmt.SampleRate) - incomingFrames);
        _loopWriter.Write(buffer, 0, bytes);
        _loopFramesWritten += incomingFrames;
    }

    private void PadLoopbackTo(long targetFrames)
    {
        if (_loopWriter is null || _loop is null)
        {
            return;
        }
        var fmt = _loop.WaveFormat;
        var missing = targetFrames - _loopFramesWritten;
        // Ignore jitter below 50 ms; only real gaps are silence.
        if (missing <= fmt.SampleRate / 20)
        {
            return;
        }
        var silence = new byte[Math.Min(missing, fmt.SampleRate) * fmt.BlockAlign];
        while (missing > 0)
        {
            var frames = (int)Math.Min(missing, silence.Length / fmt.BlockAlign);
            _loopWriter.Write(silence, 0, frames * fmt.BlockAlign);
            _loopFramesWritten += frames;
            missing -= frames;
        }
    }

    /// <summary>Stops, finalizes the WAVs and writes the mixed 16 kHz mono file.</summary>
    public async Task<string> StopAsync(string targetWav)
    {
        var total = _clock.Elapsed;
        _clock.Stop();
        _mic?.StopRecording();
        _loop?.StopRecording();
        if (_micStopped != null) await _micStopped.Task;
        if (_loopStopped != null) await _loopStopped.Task;
        if (_loop != null)
        {
            PadLoopbackTo((long)(total.TotalSeconds * _loop.WaveFormat.SampleRate));
        }
        _micWriter?.Dispose();
        _loopWriter?.Dispose();
        _micWriter = _loopWriter = null;
        _mic?.Dispose();
        _loop?.Dispose();
        _mic = null;
        _loop = null;

        Directory.CreateDirectory(Path.GetDirectoryName(targetWav)!);
        await Task.Run(() => Mix(_micTemp!, _loopTemp, targetWav));
        TryDelete(_micTemp);
        TryDelete(_loopTemp);
        return targetWav;
    }

    internal static void Mix(string micPath, string? loopPath, string target)
    {
        using var micReader = new WaveFileReader(micPath);
        var sources = new List<ISampleProvider> { To16kMono(micReader.ToSampleProvider()) };
        WaveFileReader? loopReader = null;
        if (loopPath != null && File.Exists(loopPath))
        {
            loopReader = new WaveFileReader(loopPath);
            sources.Add(To16kMono(loopReader.ToSampleProvider()));
        }
        try
        {
            var mixer = new MixingSampleProvider(sources) { ReadFully = false };
            WaveFileWriter.CreateWaveFile16(target, mixer);
        }
        finally
        {
            loopReader?.Dispose();
        }
    }

    private static ISampleProvider To16kMono(ISampleProvider src)
    {
        ISampleProvider mono = src.WaveFormat.Channels switch
        {
            1 => src,
            2 => new StereoToMonoSampleProvider(src) { LeftVolume = 0.5f, RightVolume = 0.5f },
            _ => new DownmixToMono(src),
        };
        return mono.WaveFormat.SampleRate == 16000 ? mono : new WdlResamplingSampleProvider(mono, 16000);
    }

    private static float Peak(byte[] buffer, int bytes, WaveFormat fmt)
    {
        float max = 0;
        if (fmt.Encoding == WaveFormatEncoding.IeeeFloat || (fmt is WaveFormatExtensible && fmt.BitsPerSample == 32))
        {
            for (var i = 0; i + 4 <= bytes; i += 4)
            {
                max = Math.Max(max, Math.Abs(BitConverter.ToSingle(buffer, i)));
            }
        }
        else if (fmt.BitsPerSample == 16)
        {
            for (var i = 0; i + 2 <= bytes; i += 2)
            {
                max = Math.Max(max, Math.Abs(BitConverter.ToInt16(buffer, i) / 32768f));
            }
        }
        return Math.Min(1f, max);
    }

    private static void TryDelete(string? path)
    {
        try { if (path != null) File.Delete(path); } catch { /* temp file, not critical */ }
    }

    public void Dispose()
    {
        _micWriter?.Dispose();
        _loopWriter?.Dispose();
        _mic?.Dispose();
        _loop?.Dispose();
    }

    /// <summary>Averages any channel count down to mono (loopback devices can be 5.1/7.1).</summary>
    private sealed class DownmixToMono : ISampleProvider
    {
        private readonly ISampleProvider _src;
        private float[] _buf = Array.Empty<float>();
        public DownmixToMono(ISampleProvider src)
        {
            _src = src;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(src.WaveFormat.SampleRate, 1);
        }
        public WaveFormat WaveFormat { get; }
        public int Read(float[] buffer, int offset, int count)
        {
            var ch = _src.WaveFormat.Channels;
            if (_buf.Length < count * ch) _buf = new float[count * ch];
            var read = _src.Read(_buf, 0, count * ch) / ch;
            for (var i = 0; i < read; i++)
            {
                float sum = 0;
                for (var c = 0; c < ch; c++) sum += _buf[i * ch + c];
                buffer[offset + i] = sum / ch;
            }
            return read;
        }
    }
}
