"""
Plugin: Matrix-Code

Zeigt das Bild als grünen Zeichenregen. Dient zugleich als Beispiel, wie ein
Bild-Effekt als Plugin aussieht: eine Klasse, die von IEffectStrategy erbt.
"""

import cv2
import numpy as np

from typing import Any, Dict, Optional

from bildeffekte import IEffectStrategy


class MatrixCodeEffectStrategy(IEffectStrategy):
    """Das Bild wird aus grünen, herabregnenden Zeichen aufgebaut"""

    CELL_W, CELL_H = 10, 14
    CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ$#*+=<>:;!?%&@"

    def __init__(self):
        self.rng = np.random.default_rng()
        self.glyphs = self._build_glyphs()
        self._shape = None
        self.glyph_index: Optional[np.ndarray] = None
        self.heads: Optional[np.ndarray] = None
        self.speeds: Optional[np.ndarray] = None
        self.trails: Optional[np.ndarray] = None

    @property
    def name(self) -> str:
        return "Matrix-Code"

    def _build_glyphs(self) -> np.ndarray:
        """Zeichen-Atlas (Anzahl x Höhe x Breite), gespiegelt wie im Film"""
        glyphs = []
        for char in self.CHARS:
            canvas = np.zeros((self.CELL_H, self.CELL_W), dtype=np.uint8)
            cv2.putText(canvas, char, (1, self.CELL_H - 3), cv2.FONT_HERSHEY_PLAIN, 0.8, 255, 1, cv2.LINE_AA)
            glyphs.append(cv2.flip(canvas, 1))
        return np.stack(glyphs).astype(np.float32) / 255.0

    def _reset(self, rows: int, cols: int) -> None:
        rng = self.rng
        self._shape = (rows, cols)
        self.glyph_index = rng.integers(0, len(self.glyphs), (rows, cols))
        self.heads = rng.uniform(-rows, rows, cols)
        self.speeds = rng.uniform(0.3, 1.2, cols)
        self.trails = rng.uniform(6, 20, cols)

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        height, width = frame.shape[:2]
        rows, cols = max(1, height // self.CELL_H), max(1, width // self.CELL_W)
        if self._shape != (rows, cols):
            self._reset(rows, cols)

        # Helligkeit des Bildes pro Zelle
        gray = cv2.cvtColor(frame, cv2.COLOR_RGB2GRAY)
        luminance = cv2.resize(gray, (cols, rows), interpolation=cv2.INTER_AREA).astype(np.float32) / 255.0

        # Fallende Regenspuren pro Spalte
        self.heads += self.speeds
        restart = self.heads - self.trails > rows
        self.heads[restart] = rng.uniform(-20, 0, int(restart.sum()))
        self.speeds[restart] = rng.uniform(0.3, 1.2, int(restart.sum()))
        distance = self.heads[None, :] - np.arange(rows)[:, None]
        rain = np.where(distance >= 0, np.clip(1.0 - distance / self.trails[None, :], 0, 1), 0.0)
        head = (distance >= 0) & (distance < 1)

        # Zeichen flackern
        change = rng.random((rows, cols)) < 0.03
        self.glyph_index[change] = rng.integers(0, len(self.glyphs), int(change.sum()))
        self.glyph_index[head] = rng.integers(0, len(self.glyphs), int(head.sum()))

        # Bildhelligkeit bestimmt, wie hell die Zeichen sind; der Regen moduliert nur
        intensity = np.clip(0.03 + 1.1 * luminance ** 1.4, 0, 1) * (0.45 + 0.55 * rain)
        tiles = self.glyphs[self.glyph_index] * intensity[..., None, None]       # rows, cols, h, w
        mask = tiles.transpose(0, 2, 1, 3).reshape(rows * self.CELL_H, cols * self.CELL_W)
        head_mask = (self.glyphs[self.glyph_index] * head[..., None, None]).transpose(0, 2, 1, 3) \
            .reshape(rows * self.CELL_H, cols * self.CELL_W)

        result = np.zeros_like(frame)
        h, w = mask.shape
        green = np.stack([mask * 60, mask * 255, mask * 90], axis=-1)
        white = head_mask[..., None] * 200
        result[:h, :w] = np.clip(green + white, 0, 255).astype(np.uint8)
        return result
