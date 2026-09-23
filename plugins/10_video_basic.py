"""
Plugin: Grundeffekte: Mosaik, Unschärfe, Kantenerkennung, Standbild

Ausgelagert aus video.py – Effekte sind reine Plugins und kennen nur das Paket
`bildeffekte`.
"""

import time
from typing import Any, Dict, List, Optional, Tuple

import cv2
import numpy as np

from bildeffekte import IEffectStrategy, odd


class MosaicEffectStrategy(IEffectStrategy):
    """3x3-Mosaik: Nur die aktive Zelle zeigt das Live-Bild, die anderen bleiben stehen"""

    category = "Grundlagen"

    @property
    def name(self) -> str:
        return "Mosaik"

    @property
    def uses_cells(self) -> bool:
        return True

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        height, width = frame.shape[:2]
        cells = config.setdefault('matrix_frames', [None] * 9)
        active_cell = config.get('active_cell', 0)
        # Grenzen per linspace: auch bei nicht durch 3 teilbarer Größe kein schwarzer Rand
        xs = np.linspace(0, width, 4).astype(int)
        ys = np.linspace(0, height, 4).astype(int)

        if any(cell is None for cell in cells):
            for i in range(9):
                cells[i] = self._cell_image(frame, xs, ys, i)
            config['has_started'] = True

        mosaic = np.empty_like(frame)
        for i in range(9):
            row, col = divmod(i, 3)
            y0, y1, x0, x1 = ys[row], ys[row + 1], xs[col], xs[col + 1]
            if x1 <= x0 or y1 <= y0:
                continue
            if i == active_cell:
                cells[i] = self._cell_image(frame, xs, ys, i)
            cell = cells[i]
            if cell.shape[:2] != (y1 - y0, x1 - x0):
                cell = cv2.resize(cell, (x1 - x0, y1 - y0))
            mosaic[y0:y1, x0:x1] = cell
        return mosaic

    @staticmethod
    def _cell_image(frame: np.ndarray, xs: np.ndarray, ys: np.ndarray, index: int) -> np.ndarray:
        row, col = divmod(index, 3)
        size = (max(1, xs[col + 1] - xs[col]), max(1, ys[row + 1] - ys[row]))
        return cv2.resize(frame, size, interpolation=cv2.INTER_AREA)


class BlurEffectStrategy(IEffectStrategy):
    """Gaußsche Unschärfe"""

    category = "Grundlagen"

    @property
    def name(self) -> str:
        return "Blur"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        size = odd(config.get('blur_amount', 15))
        return cv2.GaussianBlur(frame, (size, size), 0)


class EdgeDetectionEffectStrategy(IEffectStrategy):
    """Kantenerkennung (Canny)"""

    category = "Grundlagen"

    @property
    def name(self) -> str:
        return "Kantenerkennung"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        gray = cv2.cvtColor(frame, cv2.COLOR_RGB2GRAY)
        edges = cv2.Canny(gray, 100, 200)
        return cv2.cvtColor(edges, cv2.COLOR_GRAY2RGB)


class SlowMotionSnapshotEffectStrategy(IEffectStrategy):
    """Zeigt nur alle 0,2 Sekunden ein neues Standbild"""

    category = "Grundlagen"

    UPDATE_INTERVAL = 0.2

    def __init__(self):
        self.last_update_time = 0.0
        self.current_frame: Optional[np.ndarray] = None

    @property
    def name(self) -> str:
        return "Slow Motion Snapshot"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        now = time.time()
        if (self.current_frame is None or self.current_frame.shape != frame.shape
                or now - self.last_update_time >= self.UPDATE_INTERVAL):
            self.current_frame = frame.copy()
            self.last_update_time = now
        return self.current_frame
