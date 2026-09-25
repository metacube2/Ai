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


class VHSSchwacherSender(IEffectStrategy):
    """Analoger Fernsehempfang mit schwankender Signalstärke: Schnee, Geister, Rollen."""

    category = "Retro & VHS"

    name = "VHS Schwacher Sender"

    def __init__(self):
        self.rng = np.random.default_rng()
        self.activate()

    def activate(self) -> None:
        self.tick = 0
        self.drift = 0.0
        self.fade_left = 0
        self.fade_depth = 0.0
        self.roll = 0.0
        self.jitter: Optional[np.ndarray] = None

    def _signal(self) -> float:
        """Signalstärke 0 (nur Schnee) bis 1 (sauberer Empfang), weich schwankend."""
        t = self.tick / 30.0
        self.drift = float(np.clip(self.drift * 0.985 + self.rng.normal(0, 0.018), -0.3, 0.3))
        level = (0.58 + 0.22 * np.sin(t * 2 * np.pi / 9.1)
                 + 0.12 * np.sin(t * 2 * np.pi / 3.3 + 1.3) + self.drift)
        # Gelegentliche Fading-Einbrüche wie bei Überreichweite oder vorbeifahrenden Autos.
        if self.fade_left <= 0 and self.rng.random() < 1 / 160:
            self.fade_left = int(self.rng.integers(20, 70))
            self.fade_depth = float(self.rng.uniform(0.35, 0.7))
        if self.fade_left > 0:
            self.fade_left -= 1
            level -= self.fade_depth * np.sin(np.pi * min(1.0, self.fade_left / 25))
        return float(np.clip(level, 0.0, 1.0))

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        output_size = (frame.shape[1], frame.shape[0])
        strength = self._signal()
        weak = 1.0 - strength
        low = vhs_signal(frame, self.rng, self.tick, wear=0.6, native=True)
        height, width = low.shape[:2]
        ycc = cv2.cvtColor(low, cv2.COLOR_RGB2YCrCb).astype(np.float32)
        luma = ycc[:, :, 0]

        # Mehrwegempfang: leicht versetztes Echo desselben Bildes.
        echo = max(1, width // 22)
        if width > echo:
            luma[:, echo:] = luma[:, echo:] * (1 - 0.12 - 0.2 * weak) + \
                luma[:, :-echo] * (0.12 + 0.2 * weak)

        # Schwaches Signal: weniger Kontrast, die Regelung verstärkt dafür das Rauschen.
        gain = 0.2 + 0.8 * strength
        luma = 118 + (luma - 118) * gain

        # Schnee: waagrecht leicht gestrecktes Rauschen plus einzelne helle Flocken.
        sigma = 5 + 62 * weak ** 1.6
        snow = self.rng.normal(0, 1, (height, max(1, width // 2))).astype(np.float32)
        snow = cv2.resize(snow, (width, height), interpolation=cv2.INTER_LINEAR)
        flakes = np.maximum(snow - 1.9, 0) * (60 * weak)
        luma += snow * sigma + flakes

        # Zeilenweise helligkeitsschwankung und Brummbalken.
        luma += self.rng.normal(0, 3 + 10 * weak, (height, 1)).astype(np.float32)
        hum = np.sin(np.arange(height, dtype=np.float32) * 2 * np.pi / height - self.tick * 0.09)
        luma += hum[:, None] * (2 + 7 * weak)

        # Farbabschaltung: unter einer Schwelle verschwindet die Farbe ganz.
        saturation = float(np.clip((strength - 0.28) / 0.4, 0.0, 1.0))
        chroma = ycc[:, :, 1:3]
        chroma_noise = self.rng.normal(0, 1, (max(1, height // 2), max(1, width // 6), 2))
        chroma_noise = cv2.resize(chroma_noise.astype(np.float32), (width, height),
                                  interpolation=cv2.INTER_LINEAR)
        chroma[:] = 128 + (chroma - 128) * saturation + chroma_noise * (14 * weak * saturation)
        ycc[:, :, 0] = luma
        result = cv2.cvtColor(np.clip(ycc, 0, 255).astype(np.uint8), cv2.COLOR_YCrCb2RGB)

        # Unsichere Zeilensynchronisation: Zeilen zittern seitlich, zusammenhängend.
        amount = width * 0.012 * weak ** 2
        fresh = self.rng.normal(0, 1, height).astype(np.float32)
        if self.jitter is None or self.jitter.shape != fresh.shape:
            self.jitter = fresh
        self.jitter = 0.6 * self.jitter + 0.4 * cv2.GaussianBlur(
            fresh[:, None], (1, 5), 0).ravel()
        if amount > 0.3:
            map_x = np.arange(width, dtype=np.float32)[None, :] + (self.jitter * amount)[:, None]
            map_y = np.repeat(np.arange(height, dtype=np.float32)[:, None], width, axis=1)
            result = cv2.remap(result, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)

        # Fast kein Signal: Bildfang geht verloren, das Bild rollt mit Austastbalken durch.
        if strength < 0.18:
            self.roll += height * (0.03 + 0.1 * (0.18 - strength) / 0.18)
        elif self.roll:
            self.roll = 0.0
        if self.roll and height > 4:
            offset = int(self.roll) % height
            result = np.roll(result, offset, axis=0)
            bar = max(2, height // 14)
            result[offset:min(height, offset + bar)] //= 5
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)
