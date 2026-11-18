"""Realtime and offline synthesis for Geometric Resonator."""

from __future__ import annotations

from dataclasses import dataclass, replace
from threading import Lock
import wave

import numpy as np

try:
    import sounddevice as sd
except ImportError:  # Offline rendering still works without PortAudio.
    sd = None


SHAPE_MODES: dict[str, tuple[float, ...]] = {
    "Kreis": (1.00, 1.59, 2.14, 2.30, 2.65, 3.16),
    "Dreieck": (1.00, 1.67, 2.33, 2.81, 3.42, 4.05),
    "Quadrat": (1.00, 1.41, 2.00, 2.24, 2.83, 3.16),
    "Sechseck": (1.00, 1.50, 2.00, 2.50, 3.00, 3.50),
}


@dataclass(slots=True)
class SynthParams:
    instrument: str = "Violine"
    shape: str = "Kreis"
    pitch_hz: float = 220.0
    size: float = 100.0
    excitation: float = 0.55
    damping: float = 0.45
    brightness: float = 0.55
    volume: float = 0.35

    @property
    def frequency(self) -> float:
        # A larger resonator has a lower fundamental frequency.
        return float(np.clip(self.pitch_hz * 100.0 / self.size, 35.0, 2200.0))


class SynthEngine:
    """Small physically inspired modal synthesizer with realtime output."""

    def __init__(self, sample_rate: int = 48_000, block_size: int = 512) -> None:
        self.sample_rate = sample_rate
        self.block_size = block_size
        self.params = SynthParams()
        self._lock = Lock()
        self._stream = None
        self._gate = False
        self._envelope = 0.0
        self._sample_index = 0
        self._rng = np.random.default_rng()
        self._previous_noise = 0.0
        self.level = 0.0

    def update(self, **changes: float | str) -> None:
        with self._lock:
            self.params = replace(self.params, **changes)

    def snapshot(self) -> SynthParams:
        with self._lock:
            return replace(self.params)

    def set_gate(self, enabled: bool) -> None:
        self._gate = enabled

    @property
    def running(self) -> bool:
        return self._stream is not None and self._stream.active

    def start(self) -> None:
        if sd is None:
            raise RuntimeError("sounddevice ist nicht installiert")
        if self.running:
            return
        self._stream = sd.OutputStream(
            samplerate=self.sample_rate,
            blocksize=self.block_size,
            channels=1,
            dtype="float32",
            callback=self._audio_callback,
        )
        self._stream.start()

    def stop(self) -> None:
        self._gate = False
        stream, self._stream = self._stream, None
        if stream is not None:
            stream.stop()
            stream.close()

    def _audio_callback(self, outdata, frames, _time_info, status) -> None:
        if status:
            # Avoid printing from the realtime callback. The stream continues.
            pass
        params = self.snapshot()
        signal = self._generate_block(params, frames, self._gate)
        outdata[:, 0] = signal.astype(np.float32, copy=False)

    def _generate_block(
        self, params: SynthParams, frames: int, gate: bool
    ) -> np.ndarray:
        indices = np.arange(frames, dtype=np.float64) + self._sample_index
        t = indices / self.sample_rate
        frequency = params.frequency

        if params.instrument == "Flöte":
            raw = self._flute(params, t, frequency)
            attack, release = 0.010, 0.018
        else:
            raw = self._violin(params, t, frequency)
            attack, release = 0.004, 0.012

        target = 1.0 if gate else 0.0
        speed = attack if gate else release
        coefficient = 1.0 - np.exp(-1.0 / (self.sample_rate * speed))
        envelope = np.empty(frames, dtype=np.float64)
        value = self._envelope
        for index in range(frames):
            value += (target - value) * coefficient
            envelope[index] = value
        self._envelope = value

        signal = np.tanh(raw * (1.1 + params.excitation * 1.9))
        signal *= envelope * params.volume * 0.85
        self.level = float(np.sqrt(np.mean(signal * signal)))
        self._sample_index += frames
        return signal

    def _modal_body(
        self, params: SynthParams, t: np.ndarray, frequency: float
    ) -> np.ndarray:
        ratios = np.asarray(SHAPE_MODES[params.shape], dtype=np.float64)
        decay = 0.42 + params.damping * 1.8
        weights = np.exp(-np.arange(len(ratios)) * decay)
        phase = 0.21 * np.arange(len(ratios))
        modes = np.sin(2.0 * np.pi * t[:, None] * frequency * ratios + phase)
        return (modes @ weights) / max(float(weights.sum()), 1e-6)

    def _violin(
        self, params: SynthParams, t: np.ndarray, frequency: float
    ) -> np.ndarray:
        harmonics = np.arange(1, 13, dtype=np.float64)
        rolloff = 0.72 + (1.0 - params.brightness) * 1.25
        amplitudes = 1.0 / np.power(harmonics, rolloff)
        bow_pressure = 0.65 + params.excitation * 2.2
        phases = 2.0 * np.pi * t[:, None] * frequency * harmonics
        string = np.sin(phases) @ amplitudes / amplitudes.sum()
        string = np.tanh(string * bow_pressure)

        bow_noise = self._rng.normal(0.0, 1.0, len(t))
        bow_noise = np.concatenate(([self._previous_noise], bow_noise))
        bow_noise = np.diff(bow_noise)
        self._previous_noise = float(bow_noise[-1])
        vibrato = 1.0 + 0.015 * params.excitation * np.sin(2.0 * np.pi * 5.3 * t)
        body = self._modal_body(params, t, frequency)
        return vibrato * (0.76 * string + 0.28 * body) + bow_noise * 0.018

    def _flute(
        self, params: SynthParams, t: np.ndarray, frequency: float
    ) -> np.ndarray:
        pressure = params.excitation
        vibrato_rate = 4.7 + pressure * 1.2
        phase_mod = 0.006 * pressure * np.sin(2.0 * np.pi * vibrato_rate * t)
        phase = 2.0 * np.pi * frequency * t + phase_mod
        overblow = np.clip((pressure - 0.70) / 0.30, 0.0, 1.0)
        pipe = (
            np.sin(phase)
            + (0.10 + 0.25 * params.brightness) * np.sin(2.0 * phase + 0.18)
            + (0.04 + 0.34 * overblow) * np.sin(3.0 * phase + 0.31)
        )
        pipe /= 1.5

        noise = self._rng.normal(0.0, 1.0, len(t))
        breath = noise - np.concatenate(([self._previous_noise], noise[:-1]))
        self._previous_noise = float(noise[-1])
        body = self._modal_body(params, t, frequency)
        return 0.78 * pipe + 0.20 * body + breath * (0.012 + pressure * 0.045)

    def render(self, duration: float = 4.0) -> np.ndarray:
        """Render the current sound offline without changing realtime state."""
        params = self.snapshot()
        renderer = SynthEngine(self.sample_rate, self.block_size)
        renderer.params = params
        total = int(duration * self.sample_rate)
        blocks: list[np.ndarray] = []
        remaining = total
        while remaining:
            count = min(self.block_size, remaining)
            blocks.append(renderer._generate_block(params, count, True))
            remaining -= count
        audio = np.concatenate(blocks)
        fade = min(int(0.08 * self.sample_rate), len(audio) // 2)
        audio[:fade] *= np.linspace(0.0, 1.0, fade)
        audio[-fade:] *= np.linspace(1.0, 0.0, fade)
        return audio

    def export_wav(self, path: str, duration: float = 4.0) -> None:
        audio = self.render(duration)
        pcm = np.int16(np.clip(audio, -1.0, 1.0) * 32767)
        with wave.open(path, "wb") as wav_file:
            wav_file.setnchannels(1)
            wav_file.setsampwidth(2)
            wav_file.setframerate(self.sample_rate)
            wav_file.writeframes(pcm.tobytes())

