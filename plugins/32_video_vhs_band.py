"""Fünf weitere VHS-Looks rund um Band, Rekorder und Camcorder."""

from collections import deque
from datetime import datetime
from typing import Any, Deque, Dict, List, Optional

import cv2
import numpy as np

from bildeffekte import IEffectStrategy
from bildeffekte.analog import vhs_signal


def _grain(frame: np.ndarray, rng: np.random.Generator, strength: int) -> np.ndarray:
    """Gleiches Helligkeitsrauschen auf allen Farbkanälen."""
    noise = rng.integers(-strength, strength + 1, frame.shape[:2], dtype=np.int16)
    return np.clip(frame.astype(np.int16) + noise[..., None], 0, 255).astype(np.uint8)


def _snow(rng: np.random.Generator, height: int, width: int) -> np.ndarray:
    """Waagrecht gestreckter Bandschnee als RGB-Bild."""
    noise = rng.integers(0, 256, (height, max(1, width // 2)), dtype=np.uint8)
    noise = cv2.resize(noise, (width, height), interpolation=cv2.INTER_LINEAR)
    return np.repeat(noise[..., None], 3, axis=2)


def _grid(height: int, width: int):
    return np.meshgrid(np.arange(width, dtype=np.float32), np.arange(height, dtype=np.float32))


def _can_write(frame: np.ndarray) -> bool:
    return frame.shape[1] >= 100 and frame.shape[0] >= 50


def _osd_text(frame: np.ndarray, text: str, origin, color=(235, 235, 235), scale=1.0) -> None:
    """Rekorder-Schrift: hell mit dunklem Rand, damit sie auf jedem Bild lesbar bleibt."""
    size = max(0.4, min(frame.shape[:2]) / 420) * scale
    thick = max(1, round(size * 1.6))
    cv2.putText(frame, text, origin, cv2.FONT_HERSHEY_SIMPLEX, size, (10, 10, 10),
                thick + 2, cv2.LINE_AA)
    cv2.putText(frame, text, origin, cv2.FONT_HERSHEY_SIMPLEX, size, color, thick, cv2.LINE_AA)


class VHSBandsalat(IEffectStrategy):
    """Band verheddert sich: Bild zieht Wellen, Farben leiern, kurze Aussetzer."""

    category = "Retro & VHS"

    name = "VHS Bandsalat"

    CYCLE = 300  # 10 s: ruhig, verheddern, Aussetzer, fangen

    def __init__(self):
        self.rng = np.random.default_rng()
        self.tick = 0

    def activate(self) -> None:
        self.tick = 0

    def _intensity(self, phase: float) -> float:
        # 0–0,3 ruhig, dann anschwellen bis 0,75 und schnell wieder einfangen.
        if phase < 0.3:
            return 0.0
        if phase < 0.75:
            return ((phase - 0.3) / 0.45) ** 1.5
        return max(0.0, 1.0 - (phase - 0.75) / 0.12)

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        output_size = (frame.shape[1], frame.shape[0])
        phase = (self.tick % self.CYCLE) / self.CYCLE
        amount = self._intensity(phase)
        low = vhs_signal(frame, self.rng, self.tick, wear=1.0 + amount, native=True)
        height, width = low.shape[:2]
        # Aussetzer: Band hebt vom Kopf ab, der Rekorder zeigt Blau.
        if 0.62 < phase < 0.75 and self.rng.random() < 0.35:
            result = np.empty_like(low)
            result[:] = (20, 40, 190)
            return cv2.resize(_grain(result, self.rng, 6), output_size,
                              interpolation=cv2.INTER_LINEAR)
        result = low
        if amount > 0.01:
            t = self.tick * 0.12
            xs, ys = _grid(height, width)
            # Band dehnt sich ungleichmäßig: Zeilen und Spalten laufen in Wellen.
            wave_y = np.sin(ys * (0.035 + 0.02 * amount) + t) * height * 0.05 * amount
            wave_x = (np.sin(ys * 0.11 - t * 1.7) + 0.6 * np.sin(ys * 0.023 + t * 0.5)) \
                * width * 0.03 * amount
            stretch = 1 - 0.18 * amount * (0.5 + 0.5 * np.sin(t * 0.7))
            map_x = xs * stretch + wave_x
            map_y = ys + wave_y
            result = cv2.remap(low, map_x.astype(np.float32), map_y.astype(np.float32),
                               cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)
            # Leiern: der Farbträger dreht, die Farben pendeln.
            ycc = cv2.cvtColor(result, cv2.COLOR_RGB2YCrCb).astype(np.float32)
            angle = np.sin(ys[:, :1] * 0.05 + t * 1.3) * 0.9 * amount
            cr, cb = ycc[:, :, 1] - 128, ycc[:, :, 2] - 128
            cos, sin = np.cos(angle), np.sin(angle)
            ycc[:, :, 1] = 128 + cr * cos - cb * sin
            ycc[:, :, 2] = 128 + cr * sin + cb * cos
            result = cv2.cvtColor(np.clip(ycc, 0, 255).astype(np.uint8), cv2.COLOR_YCrCb2RGB)
            # Knitterstellen: einzelne Zeilen reißen weiß aus.
            for _ in range(int(amount * 6)):
                y = int(self.rng.integers(0, height))
                x0 = int(self.rng.integers(0, width))
                result[y, x0:min(width, x0 + width // 3)] = 230
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)


class VHSVorspulen(IEffectStrategy):
    """Bildsuchlauf: feste Störbalken, vorwärts live, rückwärts aus dem Puffer."""

    category = "Retro & VHS"

    name = "VHS Vorspulen"

    SWITCH = 150  # Richtung wechselt alle 5 s
    HISTORY = 150

    def __init__(self):
        self.rng = np.random.default_rng()
        self.activate()

    def activate(self) -> None:
        self.tick = 0
        self.history: Deque[np.ndarray] = deque(maxlen=self.HISTORY)
        self.rewind_pos = 0

    def _bars(self, result: np.ndarray, count: int) -> None:
        height, width = result.shape[:2]
        bar = max(2, height // 16)
        drift = (self.tick * 0.6) % height
        for i in range(count):
            center = int((height * (i + 0.6) / count + drift) % height)
            y0, y1 = max(0, center - bar // 2), min(height, center + bar // 2 + 1)
            if y1 <= y0:
                continue
            band = np.roll(result[y0:y1], int(self.rng.integers(-width // 8, width // 8 + 1)), axis=1)
            snow = _snow(self.rng, y1 - y0, width)
            result[y0:y1] = cv2.addWeighted(band, 0.35, snow, 0.65, 0)

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        output_size = (frame.shape[1], frame.shape[0])
        low = vhs_signal(frame, self.rng, self.tick, wear=1.3, native=True)
        if self.history and self.history[-1].shape != low.shape:
            self.history.clear()
        forward = (self.tick // self.SWITCH) % 2 == 0
        if forward:
            self.history.append(low)
            self.rewind_pos = len(self.history) - 1
            result = low.copy()
        else:
            # Rückwärts: die letzten Sekunden dreifach schnell, danach steht der Anfang.
            self.rewind_pos -= 3
            if self.rewind_pos < 0:
                # Pufferanfang erreicht: wieder vom neuesten Bild zurückspulen.
                self.rewind_pos = len(self.history) - 1
            result = (self.history[self.rewind_pos] if self.history else low).copy()
        height, width = result.shape[:2]
        # Schneller Lauf: Bild wirkt hart und leicht gestaucht.
        result = cv2.convertScaleAbs(result, alpha=1.15, beta=-10)
        self._bars(result, 3 if forward else 2)
        result = _grain(result, self.rng, 7)
        if _can_write(result):
            size = max(6, min(height, width) // 22)
            x0, y0 = width - size * 6, max(size + 4, height // 10)
            direction = 1 if forward else -1
            for k in range(2):
                tip = x0 + size * (1.1 * k + (1 if forward else 0))
                points = np.array([[tip, y0], [tip - direction * size, y0 - size // 2 - 2],
                                   [tip - direction * size, y0 + size // 2 + 2]], np.int32)
                cv2.fillPoly(result, [points], (235, 235, 235), cv2.LINE_AA)
            _osd_text(result, "x5", (x0 + int(size * 2.4), y0 + size // 2))
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)


class VHSUeberspielt(IEffectStrategy):
    """Unvollständig gelöschtes Band: eine ältere Aufnahme scheint in Streifen durch."""

    category = "Retro & VHS"

    name = "VHS Überspielte Kassette"

    DELAY = 90  # die „alte Aufnahme“ ist 3 s alt und eingefärbt

    def __init__(self):
        self.rng = np.random.default_rng()
        self.activate()

    def activate(self) -> None:
        self.tick = 0
        self.history: Deque[np.ndarray] = deque(maxlen=self.DELAY)
        self.field: Optional[np.ndarray] = None

    def _old_recording(self, low: np.ndarray) -> np.ndarray:
        old = self.history[0] if self.history else low
        # Ältere Aufnahme: kaum Farbe, etwas Grünstich, weicher.
        gray = cv2.cvtColor(old, cv2.COLOR_RGB2GRAY)
        tint = cv2.merge((gray * 0.85, gray * 1.0, gray * 0.8)).astype(np.uint8)
        mixed = cv2.addWeighted(old, 0.25, tint, 0.75, 0)
        return cv2.GaussianBlur(mixed, (5, 3), 0)

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        output_size = (frame.shape[1], frame.shape[0])
        low = vhs_signal(frame, self.rng, self.tick, wear=1.1, native=True)
        height, width = low.shape[:2]
        if self.history and self.history[0].shape != low.shape:
            self.history.clear()
            self.field = None
        self.history.append(low)
        old = self._old_recording(low)
        # Restmagnetisierung: breite, langsam wandernde Streifen mit ausfransenden Rändern.
        if self.field is None:
            base = self.rng.random((max(2, height // 6), 3)).astype(np.float32)
            self.field = cv2.resize(base, (width, height * 2), interpolation=cv2.INTER_CUBIC)
        scroll = int(self.tick * 0.7) % height
        field = self.field[scroll:scroll + height]
        pulse = 0.5 + 0.5 * np.sin(self.tick * 0.021)
        threshold = 0.62 - 0.22 * pulse
        edge = self.rng.normal(0, 0.03, (height, 1)).astype(np.float32)
        mask = np.clip((field + edge - threshold) * 7, 0, 1)
        # Auch außerhalb der Streifen bleibt ein schwaches Geisterbild.
        alpha = (0.12 + 0.68 * mask)[..., None]
        result = (low * (1 - alpha) + old * alpha).astype(np.uint8)
        # An den Übergängen springt der Rekorder kurz zwischen beiden Spuren.
        border = np.abs(np.diff(mask[:, width // 2])) > 0.2
        rows = np.nonzero(border)[0]
        if len(rows) > 3:
            rows = self.rng.choice(rows, 3, replace=False)
        for y in rows:
            y0, y1 = y, min(height, y + max(1, height // 80) + 1)
            result[y0:y1] = np.roll(result[y0:y1], max(1, width // 40), axis=1)
            result[y0] = np.clip(result[y0].astype(np.int16) + 35, 0, 255).astype(np.uint8)
        result = _grain(result, self.rng, 6)
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)


class VHSCamcorder(IEffectStrategy):
    """Heimvideo: Datumseinblendung, pumpende Belichtung und Schärfe, überstrahlte Lichter."""

    category = "Retro & VHS"

    name = "VHS Camcorder-Heimvideo"

    YEAR = 1994

    def __init__(self):
        self.rng = np.random.default_rng()
        self.activate()

    def activate(self) -> None:
        self.tick = 0
        self.gain = 1.0
        self.gain_speed = 0.0
        self.focus = 0.0
        self.focus_target = 0.0

    def _exposure(self, luma_mean: float) -> float:
        # Träge Belichtungsautomatik mit Überschwingen.
        target = float(np.clip(118 / max(luma_mean, 8), 0.6, 1.8))
        self.gain_speed = 0.86 * self.gain_speed + 0.035 * (target - self.gain)
        self.gain = float(np.clip(self.gain + self.gain_speed, 0.5, 2.0))
        return self.gain

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        output_size = (frame.shape[1], frame.shape[0])
        low = vhs_signal(frame, self.rng, self.tick, wear=0.7, native=True)
        height, width = low.shape[:2]
        image = low.astype(np.float32)
        gain = self._exposure(float(image.mean()))
        image *= gain
        # Autofokus sucht ab und zu nach.
        if self.tick % 150 == 60:
            self.focus_target = float(self.rng.uniform(0.8, 1.8))
        elif self.tick % 150 == 85:
            self.focus_target = 0.0
        self.focus += 0.2 * (self.focus_target - self.focus)
        if self.focus > 0.3 and min(height, width) > 4:
            image = cv2.GaussianBlur(image, (0, 0), self.focus)
        # Röhrenkamera-Look: Lichter überstrahlen weich, Rot-Orange-Stich.
        bright = np.clip(image - 190, 0, None)
        if min(height, width) > 4:
            bright = cv2.GaussianBlur(bright, (0, 0), max(1.0, width / 90))
        image += bright * 1.4
        image *= np.array([1.07, 1.0, 0.88], np.float32)
        result = np.clip(image, 0, 255).astype(np.uint8)
        result = _grain(result, self.rng, 5)
        if _can_write(result):
            now = datetime.now()
            margin = max(6, width // 30)
            _osd_text(result, now.strftime("%b %d ").upper() + str(self.YEAR),
                      (margin, height - margin - max(12, height // 14)), (250, 245, 200))
            _osd_text(result, now.strftime("%H:%M:%S"), (margin, height - margin),
                      (250, 245, 200))
            if (self.tick // 18) % 2 == 0:
                radius = max(3, height // 45)
                center = (margin + radius, margin + radius)
                cv2.circle(result, center, radius, (230, 30, 30), -1, cv2.LINE_AA)
                _osd_text(result, "REC", (center[0] + radius * 2, center[1] + radius))
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)


class VHSVerschimmelt(IEffectStrategy):
    """Schimmel auf dem Band: weiße Flecken und Fäden kehren mit der Bandschleife wieder."""

    category = "Retro & VHS"

    name = "VHS Verschimmeltes Band"

    LOOP = 120  # nach 4 s kommt dieselbe Bandstelle wieder

    def __init__(self):
        self.rng = np.random.default_rng()
        self.activate()

    def activate(self) -> None:
        self.tick = 0
        self.masks: List[np.ndarray] = []

    def _build_masks(self, height: int, width: int) -> None:
        """Schimmel liegt fest auf dem Band; pro Bandstelle eine vorberechnete Maske."""
        rng = np.random.default_rng(7)
        spots = []
        for _ in range(12):
            spots.append((rng.uniform(0, width), rng.uniform(0, self.LOOP),
                          rng.uniform(0.04, 0.09) * width, rng.uniform(0, 1)))
        self.masks = []
        for pos in range(self.LOOP):
            mask = np.zeros((height, width), np.float32)
            for x, start, size, kind in spots:
                # Jeder Fleck ist für einige Bandbilder sichtbar und wandert dabei nach unten.
                age = (pos - start) % self.LOOP
                if age > 22:
                    continue
                y = height * (0.1 + age / 26)
                fade = np.sin(np.pi * age / 22)
                if kind < 0.65:
                    # Schimmelfleck: mehrere weiche Kolonien, dazu feine Myzelfäden.
                    for _ in range(6):
                        cx = int(x + rng.normal(0, size * 0.5))
                        cy = int(y + rng.normal(0, size * 0.35))
                        cv2.circle(mask, (cx, cy), max(1, int(size * rng.uniform(0.2, 0.5))),
                                   fade * rng.uniform(0.5, 0.9), -1, cv2.LINE_AA)
                    for _ in range(3):
                        angle = rng.uniform(0, 2 * np.pi)
                        steps = np.linspace(0, 1, 8)[:, None]
                        bend = rng.normal(0, 0.4)
                        dx = np.cos(angle + bend * steps) * size * 1.8 * steps
                        dy = np.sin(angle + bend * steps) * size * 1.2 * steps
                        points = np.hstack((x + dx, y + dy)).astype(np.int32)
                        cv2.polylines(mask, [points], False, fade * 0.35, 1, cv2.LINE_AA)
                else:
                    # Faden: dünne, gebogene Linie quer durchs Bild.
                    xs = np.linspace(x - size * 4, x + size * 4, 12)
                    ys = y + np.sin(np.linspace(0, 3, 12) + kind * 6) * size
                    points = np.stack((xs, ys), axis=1).astype(np.int32)
                    cv2.polylines(mask, [points], False, fade * 0.7, 1, cv2.LINE_AA)
            # Feste Dropout-Striche derselben Bandstelle.
            for _ in range(int(rng.integers(0, 4))):
                yy = int(rng.integers(0, height))
                xx = int(rng.integers(0, width))
                mask[yy, xx:min(width, xx + int(rng.integers(width // 20 + 1, width // 5 + 2)))] = 1
            if min(height, width) > 4:
                soft = cv2.GaussianBlur(mask, (0, 0), max(1.0, width / 160))
                mask = np.maximum(soft * 1.1, mask * 0.6)
            self.masks.append(np.clip(mask, 0, 1))

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        output_size = (frame.shape[1], frame.shape[0])
        low = vhs_signal(frame, self.rng, self.tick, wear=1.4, native=True)
        height, width = low.shape[:2]
        if not self.masks or self.masks[0].shape != (height, width):
            self._build_masks(height, width)
        mask = self.masks[self.tick % self.LOOP]
        # Befallenes Band wirkt stumpf und leicht grau.
        image = low.astype(np.float32)
        gray = image.mean(axis=2, keepdims=True)
        image = 0.75 * image + 0.25 * gray
        image = image * 0.92 + 12
        fleck = (mask * 215)[..., None]
        image = image * (1 - mask[..., None] * 0.85) + fleck
        result = np.clip(image, 0, 255).astype(np.uint8)
        # Wo Schimmel auf dem Kopf war, verliert die nächste Zeile kurz den Halt.
        rows = np.nonzero(mask.max(axis=1) > 0.9)[0]
        for y in rows[:: max(1, len(rows) // 5 or 1)][:5]:
            result[y] = np.roll(result[y], max(1, width // 50), axis=0)
        result = _grain(result, self.rng, 7)
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)
