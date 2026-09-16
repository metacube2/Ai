"""
bildeffekte.toolkit – Bausteine für eigene Effekte.

Kleine Helfer und fertige Basisklassen: StftAudioEffect für Ton-Effekte im
Frequenzbereich, OnsetDetector für Einsätze, dazu Bild-Hilfsfunktionen.
"""

import threading
import time
from abc import abstractmethod
from typing import Any, Callable, Dict, List, Optional, Tuple

import cv2
import numpy as np

from .api import IAudioEffect, VideoFeatureSnapshot


def odd(value: float) -> int:
    """Nächste ungerade Zahl >= 1 (für Blur-Kernel)"""
    v = max(1, int(value))
    return v if v % 2 else v + 1


def rand_int(rng: np.random.Generator, low: int, high: int) -> int:
    """Wie rng.integers(low, high), liefert aber low statt eines Fehlers bei leerem Bereich"""
    return int(rng.integers(low, high)) if high > low else int(low)


def random_box(rng: np.random.Generator, width: int, height: int,
               min_size: int, max_size: int) -> Tuple[int, int, int, int]:
    """Zufälliges Rechteck (x, y, b, h), das auch bei kleinen Regionen ins Bild passt"""
    def span(total: int) -> Tuple[int, int]:
        lo = max(1, min(min_size, total))
        hi = max(lo, min(max_size, total))
        size = rand_int(rng, lo, hi + 1)
        return rand_int(rng, 0, total - size + 1), size

    x, bw = span(width)
    y, bh = span(height)
    return x, y, bw, bh


def shift_image(img: np.ndarray, dx: float, dy: float, border: int = cv2.BORDER_REPLICATE) -> np.ndarray:
    """Verschiebt ein Bild bzw. einen Kanal um (dx, dy) Pixel"""
    if dx == 0 and dy == 0:
        return img
    h, w = img.shape[:2]
    return cv2.warpAffine(img, np.float32([[1, 0, dx], [0, 1, dy]]), (w, h), borderMode=border)


def scale_image(img: np.ndarray, factor) -> np.ndarray:
    """Multipliziert Helligkeit mit Faktor (Skalar oder H x B), mit Sättigung auf 0..255"""
    if np.isscalar(factor):
        return cv2.convertScaleAbs(img, alpha=float(factor))
    result = img.astype(np.float32) * factor[..., None]
    return np.clip(result, 0, 255).astype(np.uint8)


def to_rgb_colormap(gray: np.ndarray, colormap: int) -> np.ndarray:
    return cv2.cvtColor(cv2.applyColorMap(gray, colormap), cv2.COLOR_BGR2RGB)


class PixelGrid:
    """Zwischengespeicherte Pixelkoordinaten für Remapping-Effekte"""

    def __init__(self):
        self._shape = None
        self.xs = self.ys = None

    def get(self, height: int, width: int) -> Tuple[np.ndarray, np.ndarray]:
        if self._shape != (height, width):
            self.xs, self.ys = np.meshgrid(np.arange(width, dtype=np.float32),
                                           np.arange(height, dtype=np.float32))
            self._shape = (height, width)
        return self.xs, self.ys

class NoisePool:
    """
    Vorberechnete Rauschtexturen. Pro Bild wird eine zufällig gewählt und
    zufällig verschoben — bei 30 fps nicht von frisch erzeugtem Rauschen zu
    unterscheiden, aber ein Vielfaches schneller.
    """

    def __init__(self, generator: Callable[[np.random.Generator, int, int], np.ndarray], count: int = 8):
        self._generator = generator
        self._count = count
        self._shape = None
        self._textures: List[np.ndarray] = []

    def get(self, rng: np.random.Generator, height: int, width: int) -> np.ndarray:
        if self._shape != (height, width):
            self._textures = [self._generator(rng, height, width) for _ in range(self._count)]
            self._shape = (height, width)
        texture = self._textures[int(rng.integers(0, self._count))]
        return np.roll(texture, (int(rng.integers(0, height)), int(rng.integers(0, width))), axis=(0, 1))


class StftAudioEffect(IAudioEffect):
    """
    Basis für spektrale Effekte: Kurzzeit-Fourier-Transformation mit 4-facher
    Überlappung. Unterklassen bearbeiten pro Analyseschritt Betrag und Phase.
    """

    FRAME = 2048
    HOP = 512

    def reset(self, sample_rate: int) -> None:
        n, hop = self.FRAME, self.HOP
        self.sample_rate = sample_rate
        self.window = (0.5 - 0.5 * np.cos(2 * np.pi * np.arange(n) / n)).astype(np.float32)
        self.norm = float(np.sum(self.window ** 2) / hop)
        self.freqs = np.fft.rfftfreq(n, 1.0 / sample_rate)
        self.bins = np.arange(len(self.freqs), dtype=np.float64)
        self.bin_advance = 2 * np.pi * self.bins * hop / n      # Phasenfortschritt der Bin-Mitten
        self.hops_per_second = sample_rate / hop
        self._frame = np.zeros(n, dtype=np.float32)
        self._pending = np.zeros(0, dtype=np.float32)
        self._accum = np.zeros(n, dtype=np.float32)
        self._out = np.zeros(hop, dtype=np.float32)            # Vorlauf: Ausgabe läuft nie leer
        self.rng = np.random.default_rng()
        self.on_reset()

    @property
    def latency(self) -> int:
        return self.FRAME

    def on_reset(self) -> None:
        pass

    @abstractmethod
    def process_frame(self, mag: np.ndarray, phase: np.ndarray,
                      video: VideoFeatureSnapshot) -> Tuple[np.ndarray, np.ndarray]:
        """Betrag und Phase eines Analyseschritts verändern"""

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n, hop = self.FRAME, self.HOP
        self._pending = np.concatenate([self._pending, block.astype(np.float32)])
        produced = [self._out]
        while len(self._pending) >= hop:
            self._frame[:-hop] = self._frame[hop:]
            self._frame[-hop:] = self._pending[:hop]
            self._pending = self._pending[hop:]
            spectrum = np.fft.rfft(self._frame * self.window)
            mag, phase = self.process_frame(np.abs(spectrum), np.angle(spectrum), video)
            frame = np.fft.irfft(mag * np.exp(1j * phase), n).astype(np.float32)
            self._accum += frame * self.window / self.norm
            produced.append(self._accum[:hop].copy())
            self._accum[:-hop] = self._accum[hop:]
            self._accum[-hop:] = 0.0
        out = np.concatenate(produced)
        self._out = out[len(block):]
        return out[:len(block)]


class OnsetDetector:
    """Erkennt Einsätze (Silben, Schläge) über den spektralen Fluss"""

    def __init__(self, sample_rate: int, frame: int = 1024, hop: int = 512, min_gap: float = 0.12):
        self.frame, self.hop = frame, hop
        self.window = np.hanning(frame).astype(np.float32)
        self.min_gap = int(min_gap * sample_rate)
        self._buffer = np.zeros(frame, dtype=np.float32)
        self._pending = np.zeros(0, dtype=np.float32)
        self._prev = np.zeros(frame // 2 + 1)
        self._count = 0                 # verarbeitete Samples (absolut)
        self._flux_avg = 1.0
        self._last_onset = -10 ** 9

    def feed(self, block: np.ndarray) -> List[int]:
        onsets = []
        self._pending = np.concatenate([self._pending, block])
        while len(self._pending) >= self.hop:
            self._buffer[:-self.hop] = self._buffer[self.hop:]
            self._buffer[-self.hop:] = self._pending[:self.hop]
            self._pending = self._pending[self.hop:]
            self._count += self.hop
            mag = np.abs(np.fft.rfft(self._buffer * self.window))
            flux = float(np.sum(np.maximum(0.0, np.log1p(mag * 10) - np.log1p(self._prev * 10))))
            self._prev = mag
            loud = float(np.sqrt(np.mean(self._buffer[-self.hop:] ** 2))) > 0.005
            position = self._count - self.hop
            if loud and flux > 2.2 * self._flux_avg + 1.0 and position - self._last_onset > self.min_gap:
                onsets.append(position)
                self._last_onset = position
            self._flux_avg = 0.9 * self._flux_avg + 0.1 * flux
        return onsets
