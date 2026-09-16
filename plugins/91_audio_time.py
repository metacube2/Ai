"""
Plugin: Ton-Effekte im Zeitbereich

Ausgelagert aus video.py – Effekte sind reine Plugins und kennen nur das Paket
`bildeffekte`.
"""

from typing import Any, Dict, List, Optional, Tuple

import time

import cv2
import numpy as np

from bildeffekte import IAudioEffect, OnsetDetector, VideoFeatureSnapshot


class PremonitionEffect(IAudioEffect):
    description = ("Man hört jedes Wort leise rückwärts anschwellen, bevor es kommt – "
                   "ein Geist aus der Zukunft. Der Ton läuft dafür 1,5 s verzögert.")
    LOOKAHEAD = 1.5
    GHOST = 0.4
    GHOST_GAIN = 0.7

    @property
    def name(self) -> str:
        return "Vorahnung"

    @property
    def latency(self) -> int:
        return int(self.LOOKAHEAD * self.sample_rate)

    def reset(self, sample_rate: int) -> None:
        self.sample_rate = sample_rate
        self._delay = int(self.LOOKAHEAD * sample_rate)
        self._ghost_len = int(self.GHOST * sample_rate)
        self._fifo = np.zeros(self._delay, dtype=np.float32)
        self._ghosts = np.zeros(self._delay + self._ghost_len, dtype=np.float32)
        self._in_count = 0
        self._out_count = 0
        self._pending: List[int] = []
        self._detector = OnsetDetector(sample_rate)
        self._envelope = (np.linspace(0, 1, self._ghost_len) ** 3).astype(np.float32)
        smooth = cv2.getGaussianKernel(31, 8).ravel().astype(np.float32)
        self._smooth = smooth / smooth.sum()

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        self._fifo = np.concatenate([self._fifo, block.astype(np.float32)])
        self._in_count += n
        self._pending.extend(self._detector.feed(block))

        fifo_start = self._in_count - len(self._fifo)
        still_pending = []
        for onset in self._pending:
            if self._in_count < onset + self._ghost_len:
                still_pending.append(onset)
                continue
            position = onset - fifo_start
            offset = onset + self._delay - self._ghost_len - self._out_count
            if position < 0 or offset < 0:
                continue
            segment = self._fifo[position:position + self._ghost_len]
            ghost = np.convolve(segment[::-1], self._smooth, mode='same') * self._envelope * self.GHOST_GAIN
            self._ghosts[offset:offset + len(ghost)] += ghost
        self._pending = still_pending

        out = self._fifo[:n] + self._ghosts[:n]
        self._fifo = self._fifo[n:]
        self._ghosts = np.concatenate([self._ghosts[n:], np.zeros(n, dtype=np.float32)])
        self._out_count += n
        return out


class BeatQuantizerEffect(IAudioEffect):
    description = ("Silben und Geräusche rasten auf Achtelnoten ein – Sprache wird zu Sprechgesang. "
                   "Das Tempo folgt den Schnitten im Video (sonst 120 BPM).")
    MIN_DELAY = 0.06
    HISTORY = 1.5
    CROSSFADE = 256
    GATE_OPEN = 0.65      # Anteil des Rasterschritts, danach Staccato
    GATE_FLOOR = 0.15

    @property
    def name(self) -> str:
        return "Takt-Zwang"

    @property
    def latency(self) -> int:
        return int(self.MIN_DELAY * self.sample_rate)

    def reset(self, sample_rate: int) -> None:
        self.sample_rate = sample_rate
        self._ring = np.zeros(int(self.HISTORY * sample_rate), dtype=np.float32)
        self._count = 0                      # geschriebene = ausgegebene Samples (absolut)
        self._delay = float(self.MIN_DELAY * sample_rate)
        self._target_delay: Optional[float] = None
        self._fade = 0
        self._events: List[float] = [0.0]    # Ausgabezeitpunkte der eingerasteten Einsätze
        self._detector = OnsetDetector(sample_rate)
        self.bpm = 120.0

    def _update_tempo(self, video: VideoFeatureSnapshot) -> None:
        recent = [t for t in video.cut_times if time.time() - t < 12.0]
        if len(recent) >= 3:
            interval = float(np.median(np.diff(recent)))
            if interval > 0:
                bpm = 60.0 / interval
                while bpm < 80:
                    bpm *= 2
                while bpm > 160:
                    bpm /= 2
                self.bpm += 0.2 * (bpm - self.bpm)

    def _read(self, positions: np.ndarray) -> np.ndarray:
        index = np.floor(positions).astype(np.int64)
        return self._ring[index % len(self._ring)]

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        ring_len = len(self._ring)
        write = (self._count + np.arange(n)) % ring_len
        self._ring[write] = block
        self._update_tempo(video)
        step = 60.0 / self.bpm / 2.0 * self.sample_rate          # Achtelnote in Samples

        for onset in self._detector.feed(block):
            if self._target_delay is not None:                    # laufende Überblendung abschließen
                self._delay, self._target_delay = self._target_delay, None
            earliest = onset + self.MIN_DELAY * self.sample_rate
            grid = np.ceil(earliest / step) * step
            delay = grid - onset
            if delay < ring_len - n - self.CROSSFADE:
                self._target_delay = float(delay)
                self._fade = 0
                self._events.append(float(grid))
                self._events = self._events[-8:]

        out_positions = self._count + np.arange(n, dtype=np.float64)
        out = self._read(out_positions - self._delay)
        if self._target_delay is not None:
            fade = np.clip((np.arange(n) + self._fade) / self.CROSSFADE, 0, 1).astype(np.float32)
            out = out * (1 - fade) + self._read(out_positions - self._target_delay) * fade
            self._fade += n
            if self._fade >= self.CROSSFADE:
                self._delay, self._target_delay = self._target_delay, None

        # Staccato: nach dem Einsatz offen, dann bis zum nächsten Rasterpunkt abgedunkelt
        events = np.array(sorted(self._events))
        last = events[np.clip(np.searchsorted(events, out_positions, side='right') - 1, 0, len(events) - 1)]
        since = out_positions - last
        attack = np.clip(since / (0.005 * self.sample_rate), 0, 1)
        release = np.clip(1.0 - (since - self.GATE_OPEN * step) / (0.1 * step), 0, 1)
        gate = np.where(since < self.GATE_OPEN * step, attack, self.GATE_FLOOR + (1 - self.GATE_FLOOR) * release)
        self._count += n
        return (out * gate).astype(np.float32)


class GlassCrystalEffect(IAudioEffect):
    description = ("Jede Silbe gefriert zu einem gläsernen Arpeggio aus ihren eigenen Obertönen – Sprache wird zur "
                   "Glasharfe. Helles Bild = längeres Nachklingen.")
    ANALYSIS = 2048
    NOTES_PER_ONSET = 6
    NOTE_SPACING = 0.07
    DRY_LEVEL = 0.3
    MAX_VOICES = 64

    @property
    def name(self) -> str:
        return "Glas-Kristallisation"

    @property
    def latency(self) -> int:
        return 0

    def reset(self, sample_rate: int) -> None:
        self.sample_rate = sample_rate
        self._detector = OnsetDetector(sample_rate)
        self._history = np.zeros(sample_rate, dtype=np.float32)
        self._count = 0
        self._pending: List[int] = []
        self._voices: List[Dict[str, float]] = []
        self._window = np.hanning(self.ANALYSIS)

    def _analyse(self, onset: int, video: VideoFeatureSnapshot) -> None:
        start = onset - (self._count - len(self._history))
        frame = self._history[start:start + self.ANALYSIS]
        if len(frame) < self.ANALYSIS:
            return
        spectrum = np.abs(np.fft.rfft(frame * self._window))
        freqs = np.fft.rfftfreq(self.ANALYSIS, 1.0 / self.sample_rate)
        band = (freqs > 150) & (freqs < 5000)
        spectrum[~band] = 0
        inner = spectrum[1:-1]
        peaks = np.where((inner > spectrum[:-2]) & (inner >= spectrum[2:]))[0] + 1
        if len(peaks) == 0:
            return
        peaks = peaks[np.argsort(spectrum[peaks])[-self.NOTES_PER_ONSET:]]
        loudest = spectrum[peaks].max()
        decay = 0.6 + 2.0 * (video.brightness if video.valid else 0.5)
        for order, peak in enumerate(sorted(peaks)):
            a, b, c = spectrum[peak - 1], spectrum[peak], spectrum[peak + 1]
            offset = 0.5 * (a - c) / (a - 2 * b + c) if (a - 2 * b + c) != 0 else 0.0
            freq = (peak + offset) * self.sample_rate / self.ANALYSIS
            self._voices.append({
                'freq': freq * 2.0,                       # eine Oktave höher klingt gläsern
                'amp': 0.12 * float(spectrum[peak] / loudest),
                'delay': int(order * self.NOTE_SPACING * self.sample_rate),
                'age': 0, 'phase': 0.0, 'decay': decay,
            })
        self._voices = self._voices[-self.MAX_VOICES:]

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        self._history = np.concatenate([self._history[n:], block])
        self._count += n
        self._pending.extend(self._detector.feed(block))
        ready = [o for o in self._pending if self._count >= o + self.ANALYSIS]
        self._pending = [o for o in self._pending if self._count < o + self.ANALYSIS]
        for onset in ready:
            self._analyse(onset, video)

        out = block * self.DRY_LEVEL
        t = np.arange(n)
        alive = []
        for voice in self._voices:
            start = max(0, voice['delay'] - voice['age'])
            if start < n:
                local = t[start:] - start
                seconds = (voice['age'] + start - voice['delay'] + local) / self.sample_rate
                envelope = np.minimum(1.0, seconds / 0.003) * np.exp(-seconds / voice['decay'] * 3.0)
                omega = 2 * np.pi * voice['freq'] / self.sample_rate
                phases = voice['phase'] + omega * local
                # Glockenartig: Grundton plus unharmonischer Teilton
                tone = np.sin(phases) + 0.3 * np.sin(phases * 2.76)
                out[start:] += (voice['amp'] * envelope * tone).astype(np.float32)
                voice['phase'] = float(phases[-1] + omega) if len(phases) else voice['phase']
            voice['age'] += n
            if (voice['age'] - voice['delay']) / self.sample_rate < voice['decay'] * 2.5:
                alive.append(voice)
        self._voices = alive
        return out


class AntimatterEffect(IAudioEffect):
    description = ("Antimaterie: Die Lautstärkekurve jedes Klangs wird umgedreht, der Inhalt bleibt vorwärts. "
                   "Anschläge schwellen langsam an und brechen abrupt ab. 0,6 s Verzögerung.")
    LOOKAHEAD = 0.6
    MAX_SEGMENT = 0.5
    MAX_GAIN = 8.0

    @property
    def name(self) -> str:
        return "Antimaterie"

    @property
    def latency(self) -> int:
        return int(self.LOOKAHEAD * self.sample_rate)

    def reset(self, sample_rate: int) -> None:
        self.sample_rate = sample_rate
        self._detector = OnsetDetector(sample_rate, min_gap=0.08)
        self._segment = np.zeros(0, dtype=np.float32)
        self._segment_start = 0
        self._count = 0
        self._ready = np.zeros(self.latency, dtype=np.float32)
        self._window = max(8, int(0.01 * sample_rate))

    def _flush(self, length: int) -> None:
        segment, self._segment = self._segment[:length], self._segment[length:]
        self._segment_start += length
        if len(segment) == 0:
            return
        kernel = np.ones(min(self._window, len(segment))) / min(self._window, len(segment))
        envelope = np.sqrt(np.convolve(segment.astype(np.float64) ** 2, kernel, mode='same')) + 1e-4
        gain = np.minimum(envelope[::-1] / envelope, self.MAX_GAIN)
        gain = np.convolve(gain, kernel, mode='same')
        out = segment * gain
        peak = np.abs(out).max()
        limit = max(float(np.abs(segment).max()), 0.05) * 1.2
        if peak > limit:
            out *= limit / peak
        self._ready = np.concatenate([self._ready, out.astype(np.float32)])

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        self._segment = np.concatenate([self._segment, block])
        self._count += n
        for onset in self._detector.feed(block):
            if onset > self._segment_start:
                self._flush(onset - self._segment_start)
        max_len = int(self.MAX_SEGMENT * self.sample_rate)
        while len(self._segment) >= max_len:
            self._flush(max_len)

        if len(self._ready) < n:      # sollte nicht vorkommen; Sicherheitsnetz
            self._flush(len(self._segment))
        out, self._ready = self._ready[:n], self._ready[n:]
        if len(out) < n:
            out = np.pad(out, (0, n - len(out)))
        return out


class SwarmEffect(IAudioEffect):
    description = ("Hunderte Klangkörner fliegen als Vogelschwarm durch Zeit und Tonhöhe. Laut bündelt sich der "
                   "Schwarm zum Originalklang, leise zerstreut er sich zu zwitschernden Partikeln. Bewegung im Bild "
                   "macht ihn unruhig, ein Schnitt sprengt ihn auseinander.")
    BIRDS = 40
    GRAIN = 0.08
    HISTORY = 2.0
    MAX_AGO = 1.5

    @property
    def name(self) -> str:
        return "Klang-Schwarm"

    @property
    def latency(self) -> int:
        return 0

    def reset(self, sample_rate: int) -> None:
        self.sample_rate = sample_rate
        self.rng = np.random.default_rng()
        self._history = np.zeros(int(self.HISTORY * sample_rate), dtype=np.float32)
        self._count = 0
        self._grain_len = int(self.GRAIN * sample_rate)
        # Zustand pro Vogel: Position (Sekunden zurück, Halbtöne) und Geschwindigkeit
        self._pos = np.column_stack([self.rng.uniform(0, self.MAX_AGO, self.BIRDS),
                                     self.rng.uniform(-12, 12, self.BIRDS)])
        self._vel = self.rng.normal(0, 0.5, (self.BIRDS, 2))
        self._next_fire = self.rng.integers(0, self._grain_len // 2, self.BIRDS)
        self._grains = np.zeros((0, 3))                # Startposition im Verlauf, Rate, Alter
        self._level = 0.0
        self._peak = 0.05
        self._last_cut = None

    def _fly(self, dt: float, video: VideoFeatureSnapshot) -> None:
        rng = self.rng
        scale = np.array([1.0 / self.MAX_AGO, 1.0 / 12.0])       # beide Achsen auf ~1 normieren
        pos = self._pos * scale
        loudness = float(np.clip(self._level / self._peak, 0, 1))
        center = pos.mean(axis=0)
        cohesion = (center - pos) * (0.1 + 0.9 * loudness)                 # leise: Schwarm lockert sich
        diff = pos[:, None, :] - pos[None, :, :]
        dist = np.linalg.norm(diff, axis=2) + np.eye(self.BIRDS)
        separation = (diff / dist[..., None] ** 2 * (dist[..., None] < 0.15)).sum(axis=1) * 0.02
        alignment = (self._vel.mean(axis=0) - self._vel) * 0.5
        home = (np.array([0.0, 0.0]) - pos) * 6.0 * loudness ** 2          # laut: zurück zum Original
        agitation = 0.3 + 2.5 * (1.0 - loudness) + (video.activity * 20.0 if video.valid else 0.5)
        noise = rng.normal(0, agitation, self._vel.shape)
        if video.valid and self._last_cut is not None and video.cut_count != self._last_cut:
            noise += rng.normal(0, 25.0, self._vel.shape)                    # Schnitt sprengt den Schwarm
        self._last_cut = video.cut_count

        self._vel += (cohesion + separation + alignment + home + noise) * dt
        self._vel *= 0.96
        self._pos = (pos + self._vel * dt) / scale
        self._pos[:, 0] = np.clip(self._pos[:, 0], 0.0, self.MAX_AGO)
        self._pos[:, 1] = np.clip(self._pos[:, 1], -24.0, 24.0)

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        ring = len(self._history)
        self._history[(self._count + np.arange(n)) % ring] = block
        self._count += n
        rms = float(np.sqrt(np.mean(block ** 2)))
        self._level = 0.8 * self._level + 0.2 * rms
        self._peak = max(0.02, self._peak * 0.9995, rms)
        self._fly(n / self.sample_rate, video)

        # Vögel feuern Körner (50 % Überlappung)
        self._next_fire -= n
        firing = np.where(self._next_fire <= 0)[0]
        if len(firing):
            ago = self._pos[firing, 0] * self.sample_rate + self._grain_len
            rates = 2.0 ** (self._pos[firing, 1] / 12.0)
            starts = self._count - ago
            new = np.column_stack([starts, rates, np.zeros(len(firing))])
            self._grains = np.vstack([self._grains, new])[-self.BIRDS * 3:]
            self._next_fire[firing] += self._grain_len // 2

        out = np.zeros(n, dtype=np.float32)
        if len(self._grains):
            starts, rates, ages = self._grains[:, 0], self._grains[:, 1], self._grains[:, 2]
            steps = ages[:, None] + np.arange(n)[None, :]
            read = starts[:, None] + steps * rates[:, None]
            index = np.floor(read).astype(np.int64)
            frac = (read - index).astype(np.float32)
            a = self._history[index % ring]
            b = self._history[(index + 1) % ring]
            samples = a + (b - a) * frac
            # nur schon geschriebene Samples lesen
            samples[read >= self._count - 1] = 0.0
            phase = np.clip(steps / self._grain_len, 0, 1)
            envelope = np.where(steps < self._grain_len, np.sin(np.pi * phase) ** 2, 0.0)
            out = (samples * envelope).sum(axis=0) / np.sqrt(max(1.0, self.BIRDS / 2))
            self._grains[:, 2] += n
            self._grains = self._grains[self._grains[:, 2] < self._grain_len]
        return out.astype(np.float32)
