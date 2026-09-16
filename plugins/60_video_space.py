"""
Plugin: Raum und Zeit: Kaleidoskop-Tunnel, Droste-Spirale, Zeitwelle

Ausgelagert aus video.py – Effekte sind reine Plugins und kennen nur das Paket
`bildeffekte`.
"""

from typing import Any, Dict, List, Optional, Tuple

import cv2
import numpy as np

from bildeffekte import IEffectStrategy, PixelGrid, rand_int


class KaleidoscopeTunnelEffectStrategy(IEffectStrategy):
    """Rotierendes Spiegelkaleidoskop mit Rückkopplung: zieht in einen farbwechselnden Tunnel"""

    def __init__(self):
        self.rng = np.random.default_rng()
        self.frame_counter = 0
        self.segments = 6
        self.frames_until_change = 240
        self.feedback: Optional[np.ndarray] = None
        self._shape = None
        self._radius = self._angle = None

    @property
    def name(self) -> str:
        return "Kaleidoskop-Tunnel"

    def _polar(self, height: int, width: int) -> Tuple[np.ndarray, np.ndarray]:
        if self._shape != (height, width):
            xs, ys = np.meshgrid(np.arange(width, dtype=np.float32) - width / 2,
                                 np.arange(height, dtype=np.float32) - height / 2)
            self._radius = np.hypot(xs, ys)
            self._angle = np.arctan2(ys, xs)
            self._shape = (height, width)
            self.feedback = None
        return self._radius, self._angle

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.frame_counter += 1
        height, width = frame.shape[:2]
        radius, angle = self._polar(height, width)

        self.frames_until_change -= 1
        if self.frames_until_change <= 0:
            self.segments = int(self.rng.choice([4, 6, 8, 10, 12]))
            self.frames_until_change = int(self.rng.integers(150, 400))

        # Winkel in ein Segment falten und spiegeln
        t = self.frame_counter
        segment = 2 * np.pi / self.segments
        rotation = t * 0.01
        folded = np.abs(np.mod(angle + rotation, segment) - segment / 2)
        zoom = 0.8 + 0.2 * np.sin(t * 0.013)
        src_r = radius * zoom
        source_angle = folded + t * 0.004
        map_x = (width / 2 + src_r * np.cos(source_angle)).astype(np.float32)
        map_y = (height / 2 + src_r * np.sin(source_angle)).astype(np.float32)
        kaleido = cv2.remap(frame, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)

        if self.feedback is None:
            self.feedback = kaleido
            return kaleido

        # Rückkopplung: letztes Bild leicht vergrößert, gedreht und im Farbton verschoben
        matrix = cv2.getRotationMatrix2D((width / 2, height / 2), 1.2 * np.sin(t * 0.005) + 0.6, 1.04)
        tunnel = cv2.warpAffine(self.feedback, matrix, (width, height), borderMode=cv2.BORDER_REFLECT)
        hsv = cv2.cvtColor(tunnel, cv2.COLOR_RGB2HSV)
        hsv[:, :, 0] = hsv[:, :, 0] + np.uint8(3)
        hsv[:, :, 0][hsv[:, :, 0] >= 180] -= 180
        tunnel = cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)

        # Bildmitte zeigt das frische Kaleidoskop, außen dominiert der Tunnel
        weight = np.clip(radius / (0.5 * np.hypot(width, height)), 0, 1)[..., None] * 0.75
        result = (kaleido * (1 - weight) + tunnel * weight).astype(np.uint8)
        self.feedback = result
        return result


class TimeWaveEffectStrategy(IEffectStrategy):
    """
    Jede Bildstelle zeigt einen anderen Moment der letzten 1,5 Sekunden.
    Die Zeitverteilung wechselt: Vorhang, Ringe, Wellen, Spirale.
    """

    HISTORY = 45
    MAX_PIXELS = 640 * 360        # größere Regionen werden verkleinert gepuffert
    MODES = ("Vorhang", "Ringe", "Wellen", "Spirale")

    def __init__(self):
        self.rng = np.random.default_rng()
        self.frame_counter = 0
        self.mode = 0
        self.frames_until_switch = 240
        self._buffer: Optional[np.ndarray] = None
        self._head = 0
        self._filled = 0
        self._grid = PixelGrid()

    @property
    def name(self) -> str:
        return "Zeitwelle"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        height, width = frame.shape[:2]
        scale = min(1.0, np.sqrt(self.MAX_PIXELS / (height * width)))
        size = (max(8, int(width * scale)), max(8, int(height * scale)))
        small = frame if scale >= 1.0 else cv2.resize(frame, size, interpolation=cv2.INTER_AREA)
        sh, sw = small.shape[:2]

        if self._buffer is None or self._buffer.shape[1:3] != (sh, sw):
            self._buffer = np.repeat(small[None], self.HISTORY, axis=0)
            self._head, self._filled = 0, 1
        self._head = (self._head + 1) % self.HISTORY
        self._buffer[self._head] = small
        self._filled = min(self._filled + 1, self.HISTORY)

        self.frame_counter += 1
        self.frames_until_switch -= 1
        if self.frames_until_switch <= 0:
            self.mode = (self.mode + 1) % len(self.MODES)
            self.frames_until_switch = int(self.rng.integers(200, 360))

        delay = self._delay_map(sh, sw)
        delay = np.clip(delay * (self._filled - 1), 0, self._filled - 1).astype(np.int64)
        index = (self._head - delay) % self.HISTORY
        ys = np.arange(sh)[:, None]
        xs = np.arange(sw)[None, :]
        result = self._buffer[index, ys, xs]
        if scale < 1.0:
            result = cv2.resize(result, (width, height), interpolation=cv2.INTER_LINEAR)
        return result

    def _delay_map(self, height: int, width: int) -> np.ndarray:
        """Verzögerung je Pixel, 0 = jetzt, 1 = älteste Aufnahme"""
        t = self.frame_counter * 0.03
        xs, ys = self._grid.get(height, width)
        nx, ny = xs / width, ys / height
        mode = self.MODES[self.mode]
        if mode == "Vorhang":
            return 0.5 + 0.5 * np.sin(2 * np.pi * (nx * 1.5 - t * 0.3))
        dx, dy = nx - 0.5, (ny - 0.5) * height / width
        radius = np.hypot(dx, dy)
        if mode == "Ringe":
            return 0.5 + 0.5 * np.sin(radius * 18 - t * 2)
        if mode == "Wellen":
            return 0.5 + 0.25 * np.sin(nx * 9 + t) + 0.25 * np.sin(ny * 7 - t * 1.3)
        return (np.arctan2(dy, dx) / (2 * np.pi) + 0.5 + radius * 2 - t * 0.2) % 1.0


class DrosteSpiralEffectStrategy(IEffectStrategy):
    """Das Bild enthält sich unendlich oft selbst – eine rotierende Escher-Spirale mit endlosem Zoom"""

    SCALE = 3.0

    def __init__(self):
        self.frame_counter = 0
        self._shape = None
        self._log_z: Optional[np.ndarray] = None

    @property
    def name(self) -> str:
        return "Droste-Spirale"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        height, width = frame.shape[:2]
        self.frame_counter += 1
        outer = 0.5 * min(width, height)
        log_scale = np.log(self.SCALE)
        inner_log = np.log(outer / self.SCALE)

        if self._shape != (height, width):
            xs, ys = np.meshgrid(np.arange(width, dtype=np.float32) - width / 2,
                                 np.arange(height, dtype=np.float32) - height / 2)
            z = (xs + 1j * ys).astype(np.complex64)
            z[np.abs(z) < 1e-3] = 1e-3
            # Ein Umlauf (Winkel + 2π) entspricht einer Vergrößerung um SCALE
            beta = np.complex64(1.0 - 1j * log_scale / (2 * np.pi))
            self._log_z = (np.log(z) * beta).astype(np.complex64)
            self._shape = (height, width)

        t = self.frame_counter
        radial = self._log_z.real - t * 0.012 * log_scale             # endloser Zoom
        angle = self._log_z.imag + t * 0.01                            # langsame Drehung
        radial = np.mod(radial - inner_log, log_scale) + inner_log     # in den Bildring zurückfalten
        r = np.exp(radial)
        map_x = (width / 2 + r * np.cos(angle)).astype(np.float32)
        map_y = (height / 2 + r * np.sin(angle)).astype(np.float32)
        return cv2.remap(frame, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)
