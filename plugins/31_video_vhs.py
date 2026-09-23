"""Vier eigenständige VHS-Looks für das Bildeffekte-Plugin-System."""

from typing import Any, Dict, Optional

import cv2
import numpy as np

from bildeffekte import IEffectStrategy
from bildeffekte.analog import vhs_signal


def _grain(frame: np.ndarray, rng: np.random.Generator, strength: int) -> np.ndarray:
    """Gleiches Helligkeitsrauschen auf allen Farbkanälen."""
    noise = rng.integers(-strength, strength + 1, frame.shape[:2], dtype=np.int16)
    return np.clip(frame.astype(np.int16) + noise[..., None], 0, 255).astype(np.uint8)


class VHSTrackingRiss(IEffectStrategy):
    """Wandernde Synchronisationsbänder reißen Bildzeilen auseinander."""

    category = "Retro & VHS"

    name = "VHS Tracking-Riss"

    def __init__(self):
        self.rng = np.random.default_rng()
        self.tick = 0

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        output_size = (frame.shape[1], frame.shape[0])
        frame = vhs_signal(frame, self.rng, self.tick, wear=1.15, native=True)
        height, width = frame.shape[:2]
        result = frame.copy()
        band_height = max(2, height // 9)
        top = (self.tick * 5) % (height + band_height) - band_height
        for offset, amount in ((0, max(2, width // 25)), (band_height, -max(1, width // 60))):
            y0 = max(0, top + offset)
            y1 = min(height, top + offset + band_height)
            if y1 > y0:
                result[y0:y1] = np.roll(frame[y0:y1], amount, axis=1)
                result[y0] = 235
        if self.tick % 13 < 3:
            tear = int(self.rng.integers(0, height))
            thickness = max(1, height // 90)
            result[tear:min(height, tear + thickness)] = self.rng.integers(
                15, 240, (min(thickness, height - tear), width, 1), dtype=np.uint8)
        result[::3] = (result[::3].astype(np.uint16) * 85 // 100).astype(np.uint8)
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)


class VHSFarbbluten(IEffectStrategy):
    """Grobe, versetzte Farbinformation mit Nachziehen aus dem letzten Bild."""

    category = "Retro & VHS"

    name = "VHS Farbbluten"

    def __init__(self):
        self.rng = np.random.default_rng()
        self.tick = 0
        self.previous_chroma: Optional[np.ndarray] = None

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        output_size = (frame.shape[1], frame.shape[0])
        frame = vhs_signal(frame, self.rng, self.tick, wear=0.9, native=True)
        height, width = frame.shape[:2]
        ycc = cv2.cvtColor(frame, cv2.COLOR_RGB2YCrCb)
        chroma = ycc[:, :, 1:3]
        small = cv2.resize(chroma, (max(1, width // 18), height), interpolation=cv2.INTER_AREA)
        chroma = cv2.resize(small, (width, height), interpolation=cv2.INTER_LINEAR)
        red_shift = max(1, width // 100)
        blue_shift = max(1, width // 65)
        chroma[:, :, 0] = np.roll(chroma[:, :, 0], red_shift, axis=1)
        chroma[:, :, 1] = np.roll(chroma[:, :, 1], -blue_shift, axis=1)
        if self.previous_chroma is not None and self.previous_chroma.shape == chroma.shape:
            chroma = cv2.addWeighted(chroma, 0.72, self.previous_chroma, 0.28, 0)
        self.previous_chroma = chroma.copy()
        ycc[:, :, 1:3] = chroma
        ycc[::4, :, 0] = (ycc[::4, :, 0].astype(np.uint16) * 88 // 100).astype(np.uint8)
        if self.tick % 19 < 3:
            y = (self.tick * 17) % height
            ycc[y:min(height, y + max(1, height // 20)), :, 1] = 175
        return cv2.resize(cv2.cvtColor(ycc, cv2.COLOR_YCrCb2RGB), output_size,
                          interpolation=cv2.INTER_LINEAR)


class VHSKopierkassette(IEffectStrategy):
    """Mehrfach kopiertes Band: weiche Details, blasse Farben und Geisterbild."""

    category = "Retro & VHS"

    name = "VHS Kopierkassette"

    def __init__(self):
        self.rng = np.random.default_rng()
        self.tick = 0
        self.previous: Optional[np.ndarray] = None

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        output_size = (frame.shape[1], frame.shape[0])
        # Mehrere Kopiergenerationen verlieren vor allem horizontale Auflösung.
        frame = vhs_signal(frame, self.rng, self.tick, wear=1.8, native=True)
        height, width = frame.shape[:2]
        small = cv2.resize(frame, (max(1, width // 3), height), interpolation=cv2.INTER_AREA)
        result = cv2.resize(small, (width, height), interpolation=cv2.INTER_LINEAR)
        result = cv2.GaussianBlur(result, (5, 3), 0)
        gray = cv2.cvtColor(result, cv2.COLOR_RGB2GRAY)
        result = cv2.addWeighted(result, 0.62, cv2.cvtColor(gray, cv2.COLOR_GRAY2RGB), 0.38, -3)
        if self.previous is not None and self.previous.shape == result.shape:
            ghost = np.roll(self.previous, max(1, width // 120), axis=1)
            result = cv2.addWeighted(result, 0.82, ghost, 0.18, 0)
        self.previous = result.copy()
        result = _grain(result, self.rng, 8)
        result[::2] = (result[::2].astype(np.uint16) * 92 // 100).astype(np.uint8)
        if self.tick % 27 < 4:
            y0 = max(0, height - max(2, height // 12))
            result[y0:] = np.roll(result[y0:], max(1, width // 45), axis=1)
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)


class VHSPause(IEffectStrategy):
    """Band angehalten: eingefrorenes Bild mit wanderndem Störbalken."""

    category = "Retro & VHS"

    name = "VHS Pause"

    def __init__(self):
        self.rng = np.random.default_rng()
        self.tick = 0
        self.held: Optional[np.ndarray] = None

    def activate(self) -> None:
        self.tick = 0
        self.held = None

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        output_size = (frame.shape[1], frame.shape[0])
        low = vhs_signal(frame, self.rng, self.tick, wear=1.2, native=True)
        height, width = low.shape[:2]
        if self.held is None or self.held.shape != low.shape or self.tick % 75 == 0:
            self.held = low
        self.tick += 1
        result = self.held.copy()
        band_height = max(2, height // 13)
        top = (self.tick * 3) % (height + band_height) - band_height
        y0, y1 = max(0, top), min(height, top + band_height)
        if y1 > y0:
            result[y0:y1] = np.roll(result[y0:y1], max(2, width // 35), axis=1)
            result[y0:y1] = cv2.addWeighted(result[y0:y1], 0.65,
                                              np.full_like(result[y0:y1], 210), 0.35, 0)
        result = _grain(result, self.rng, 5)
        result[::3] = (result[::3].astype(np.uint16) * 90 // 100).astype(np.uint8)
        if width >= 100 and height >= 50:
            cv2.putText(result, "PAUSE  II", (max(5, width // 30), max(20, height // 12)),
                        cv2.FONT_HERSHEY_SIMPLEX, max(0.45, min(width, height) / 800),
                        (230, 230, 230), 1, cv2.LINE_AA)
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)
