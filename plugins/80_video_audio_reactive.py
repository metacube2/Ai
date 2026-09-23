"""
Plugin: Ton steuert Bild: Klang-Beben, Audio+Video-Glitch

Ausgelagert aus video.py – Effekte sind reine Plugins und kennen nur das Paket
`bildeffekte`.
"""

from typing import Any, Dict, List, Optional, Tuple

import collections
import time

import cv2
import numpy as np
from scipy import signal

from bildeffekte import AUDIO_BANDS, IAudioFeatures, IAudioLoopService, IEffectStrategy, shift_image


class AudioGlitcher:
    """
    Zufällige Audio-Glitches, blockweise im Audio-Thread.
    Ein Glitch dauert mehrere Blöcke, damit er hörbar ist.
    """

    GLITCH_PROBABILITY = 0.3
    COOLDOWN_SECONDS = 0.5

    def __init__(self, sample_rate: int = 48000):
        self.sample_rate = sample_rate
        self.rng = np.random.default_rng()
        self.history: collections.deque = collections.deque(maxlen=5)
        self._samples_since_glitch = 0
        self._kind = None
        self._blocks_left = 0
        self._params: Dict[str, Any] = {}

    def prepare(self, sample_rate: int, channels: int) -> None:
        self.sample_rate = sample_rate
        self.history.clear()

    def process(self, block: np.ndarray) -> np.ndarray:
        rng = self.rng
        n = len(block)
        self.history.append(block.copy())
        self._samples_since_glitch += n

        if self._blocks_left == 0:
            if (self._samples_since_glitch < self.COOLDOWN_SECONDS * self.sample_rate
                    or rng.random() >= self.GLITCH_PROBABILITY):
                return np.clip(block, -1.0, 1.0).astype(np.float32)
            self._kind = int(rng.integers(0, 5))
            self._blocks_left = int(rng.integers(2, 8))
            self._params = {
                'bits': int(rng.integers(2, 8)),
                'factor': rng.uniform(0.8, 1.2),
                'source': self.history[int(rng.integers(0, len(self.history)))].copy(),
            }
            self._samples_since_glitch = 0

        self._blocks_left -= 1
        kind, p = self._kind, self._params
        if kind == 0:      # Rückwärts
            out = p['source'][::-1]
        elif kind == 1:    # Bit-Crushing
            levels = 2 ** p['bits']
            out = np.round(block * levels) / levels
        elif kind == 2:    # Stottern: kurzes Stück wiederholen
            piece = p['source'][len(p['source']) // 2:len(p['source']) // 2 + max(1, n // 10)]
            out = np.resize(piece, block.shape)
        elif kind == 3:    # Tonhöhe verschieben
            stretched = signal.resample(block, max(1, int(n * p['factor'])), axis=0)
            out = np.zeros_like(block)
            m = min(n, len(stretched))
            out[:m] = stretched[:m]
        else:              # Rauschen
            out = block + rng.normal(0, 0.1, block.shape)

        if out.shape != block.shape:
            out = np.resize(out, block.shape)
        return np.clip(out, -1.0, 1.0).astype(np.float32)


class AudioGlitchEffectStrategy(IEffectStrategy):
    """Video-Kanalversatz plus Mikrofon-Glitches auf dem Lautsprecher"""

    category = "Audio-reaktiv"

    def __init__(self, audio_loop_service: IAudioLoopService):
        self.audio_loop = audio_loop_service
        self.glitcher = AudioGlitcher()
        self.rng = np.random.default_rng()

    @property
    def name(self) -> str:
        return "Audio+Video Glitch"

    def activate(self) -> None:
        self.audio_loop.set_stage('video_glitch', self.glitcher)

    def deactivate(self) -> None:
        self.audio_loop.set_stage('video_glitch', None)

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        if rng.random() >= 0.5:
            return frame
        r, g, b = cv2.split(frame)
        r = shift_image(r, int(rng.integers(-10, 10)), int(rng.integers(-5, 5)), cv2.BORDER_WRAP)
        return cv2.merge([r, g, b])


class SoundQuakeEffectStrategy(IEffectStrategy):
    """
    Umgekehrte Kopplung: Der Ton steuert das Bild. Bässe lassen es pumpen und beben,
    Höhen spalten die Farben, jeder Einsatz zersplittert es in fliegende Scherben,
    ein Spektrum-Ring pulsiert. Ohne Ton schlägt ein 120-BPM-Herz.
    """

    category = "Audio-reaktiv"

    TILES_X, TILES_Y = 6, 4

    def __init__(self, audio_loop_service: IAudioLoopService, analyzer: IAudioFeatures):
        self.audio_loop = audio_loop_service
        self.analyzer = analyzer
        self.rng = np.random.default_rng()
        self.frame_counter = 0
        self._last_onsets = 0
        self._shatter = 0.0
        self._spin = np.zeros(self.TILES_X * self.TILES_Y)
        self._bass = 0.0

    @property
    def name(self) -> str:
        return "Klang-Beben"

    def activate(self) -> None:
        self.audio_loop.set_stage('analysis', self.analyzer)

    def deactivate(self) -> None:
        self.audio_loop.set_stage('analysis', None)

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        height, width = frame.shape[:2]
        self.frame_counter += 1
        audio = self.analyzer.snapshot()
        if audio.valid:
            bass, treble, level, onsets, bands = audio.bass, audio.treble, audio.level, audio.onset_count, audio.bands
        else:
            beat = (self.frame_counter % 15) / 15.0                   # 120 BPM bei 30 fps
            bass = float(np.exp(-beat * 6))
            treble, level = 0.3 * bass, 0.5 * bass
            onsets = self.frame_counter // 60
            bands = (0.3 + 0.7 * bass * np.abs(np.sin(np.arange(AUDIO_BANDS) * 0.7 + self.frame_counter * 0.1))).astype(np.float32)

        self._bass = max(bass, self._bass * 0.8)
        if onsets != self._last_onsets:
            self._last_onsets = onsets
            self._shatter = 1.0
            self._spin = rng.uniform(-25, 25, len(self._spin))
        else:
            self._shatter *= 0.86

        # Bass: Zoom-Puls und Beben
        zoom = 1.0 + 0.12 * self._bass
        shake = rng.normal(0, 8 * self._bass, 2)
        matrix = cv2.getRotationMatrix2D((width / 2, height / 2), rng.normal(0, 1.5 * self._bass), zoom)
        matrix[:, 2] += shake
        result = cv2.warpAffine(frame, matrix, (width, height), borderMode=cv2.BORDER_REFLECT)

        # Höhen: Farbkanäle spalten
        split = int(14 * treble)
        if split:
            r, g, b = cv2.split(result)
            result = cv2.merge([shift_image(r, split, 0), g, shift_image(b, -split, 0)])

        # Einsätze: Scherben fliegen auseinander und kehren zurück
        if self._shatter > 0.03:
            result = self._shatter_tiles(result)

        # Lautstärke: Blitz
        if level > 0.85:
            result = cv2.addWeighted(result, 1.0, np.full_like(result, 255), 0.25 * (level - 0.85) / 0.15, 0)

        self._draw_ring(result, bands, level)
        return result

    def _shatter_tiles(self, image: np.ndarray) -> np.ndarray:
        height, width = image.shape[:2]
        canvas = np.zeros_like(image)
        tw, th = max(1, width // self.TILES_X), max(1, height // self.TILES_Y)
        amount = self._shatter
        for i in range(self.TILES_Y):
            for j in range(self.TILES_X):
                x0, y0 = j * tw, i * th
                x1 = width if j == self.TILES_X - 1 else x0 + tw
                y1 = height if i == self.TILES_Y - 1 else y0 + th
                tile = image[y0:y1, x0:x1]
                cx, cy = (x0 + x1) / 2 - width / 2, (y0 + y1) / 2 - height / 2
                dx = int(cx * 0.6 * amount)
                dy = int(cy * 0.6 * amount)
                rot = cv2.getRotationMatrix2D(((x1 - x0) / 2, (y1 - y0) / 2),
                                              self._spin[i * self.TILES_X + j] * amount, 1.0 - 0.15 * amount)
                tile = cv2.warpAffine(tile, rot, (x1 - x0, y1 - y0))
                tx0, ty0 = x0 + dx, y0 + dy
                sx0, sy0 = max(0, -tx0), max(0, -ty0)
                tx0c, ty0c = max(0, tx0), max(0, ty0)
                tx1c, ty1c = min(width, tx0 + (x1 - x0)), min(height, ty0 + (y1 - y0))
                if tx1c > tx0c and ty1c > ty0c:
                    part = tile[sy0:sy0 + (ty1c - ty0c), sx0:sx0 + (tx1c - tx0c)]
                    mask = part.any(axis=2, keepdims=True)
                    canvas[ty0c:ty1c, tx0c:tx1c] = np.where(mask, part, canvas[ty0c:ty1c, tx0c:tx1c])
        return canvas

    def _draw_ring(self, image: np.ndarray, bands: Optional[np.ndarray], level: float) -> None:
        if bands is None:
            return
        height, width = image.shape[:2]
        center = np.array([width / 2, height / 2])
        base = min(width, height) * 0.22
        values = np.concatenate([bands, bands[::-1]])                 # symmetrischer Ring
        angles = np.linspace(0, 2 * np.pi, len(values), endpoint=False) + self.frame_counter * 0.02
        radii = base + np.clip(values, 0, 1.5) * base * 0.6
        points = (center + np.column_stack([np.cos(angles), np.sin(angles)]) * radii[:, None]).astype(np.int32)
        hue = int((self.frame_counter * 2) % 180)
        color_hsv = np.uint8([[[hue, 255, 255]]])
        color = tuple(int(c) for c in cv2.cvtColor(color_hsv, cv2.COLOR_HSV2RGB)[0, 0])
        overlay = np.zeros_like(image)
        cv2.polylines(overlay, [points], True, color, 3, cv2.LINE_AA)
        glow = cv2.GaussianBlur(overlay, (0, 0), 5)
        cv2.add(image, glow, dst=image)
        cv2.add(image, overlay, dst=image)
