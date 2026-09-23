"""
Plugin: Analoge Nostalgie: VHS-Störung und uralter Film

Ausgelagert aus video.py – Effekte sind reine Plugins und kennen nur das Paket
`bildeffekte`.
"""

from typing import Any, Dict, List, Optional, Tuple

import time

import cv2
import numpy as np

from bildeffekte import (IEffectStrategy, NoisePool, odd, rand_int, scale_image, shift_image)
from bildeffekte.analog import old_film_detail, vhs_signal


class VHSGlitchEffectStrategy(IEffectStrategy):
    """Klassische VHS-Störungen: Tracking-Fehler, Scan-Lines, Farbverzerrungen"""

    category = "Retro & VHS"

    DROPOUT_COLORS = np.array([[255, 255, 255], [0, 0, 0], [255, 0, 0], [0, 0, 255]], dtype=np.uint8)

    def __init__(self):
        self.rng = np.random.default_rng()
        self.last_frame: Optional[np.ndarray] = None
        self.tracking_offset = 0
        self.tracking_direction = 1
        self.frame_counter = 0
        self.last_major_glitch_time = 0.0
        self.brightness_pump_phase = 0.0
        # Körnung mit kanalabhängiger Stärke (R, G, B)
        self.noise = NoisePool(lambda rng, h, w: rng.integers(0, np.array([12, 8, 15]), size=(h, w, 3),
                                                              dtype=np.uint8))

    @property
    def name(self) -> str:
        return "VHS Störung"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        now = time.time()
        rng = self.rng
        self.frame_counter += 1
        output_size = (frame.shape[1], frame.shape[0])
        frame = vhs_signal(frame, rng, self.frame_counter, wear=1.1, native=True)
        height, width = frame.shape[:2]
        if self.last_frame is None or self.last_frame.shape != frame.shape:
            self.last_frame = frame.copy()

        # 1. Immer: Scanlines und leichtes Grundrauschen
        result = self._apply_scanlines(frame)
        result = self._apply_noise(result)

        # 2. Tracking-Fehler und Farbbluten nur jeden 2. Frame
        if self.frame_counter % 2 == 0:
            result = self._apply_tracking_issues(result)
            result = self._apply_color_bleeding(result)

        # 3. Auffällige Effekte selten
        if rng.random() < 0.07 and now - self.last_major_glitch_time > 1.5:
            result = self._apply_horizontal_jitter(result)
            self.last_major_glitch_time = now
        if rng.random() < 0.15:
            result = self._apply_dropouts(result)

        # 4. VHS-typische Bildqualität
        result = self._adjust_color_saturation(result)
        result = self._apply_image_pumping(result)

        # 5. Variable Unschärfe
        blur = odd(3 + int(np.sin(self.frame_counter * 0.03) * 2))
        result = cv2.GaussianBlur(result, (blur, blur), 0)

        # 6. Ghosting mit vorherigem Frame
        if self.frame_counter > 1:
            alpha = 0.85 + 0.05 * np.sin(self.frame_counter * 0.02)
            result = cv2.addWeighted(result, alpha, self.last_frame, 1 - alpha, 0)

        if self.frame_counter % 2 == 0:
            self.last_frame = result.copy()
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)

    def _apply_color_bleeding(self, frame: np.ndarray) -> np.ndarray:
        """Rot und Blau laufen gegeneinander auseinander"""
        shift = int(np.sin(self.frame_counter * 0.03) * 3.5)
        r, g, b = cv2.split(frame)
        return cv2.merge([shift_image(r, shift, 0), g, shift_image(b, -shift, 0)])

    def _apply_scanlines(self, frame: np.ndarray) -> np.ndarray:
        """Horizontale Scan-Lines, Head-Switching-Band und seltene Störlinien"""
        rng = self.rng
        height = frame.shape[0]
        result = frame.copy()
        result[::2] = cv2.convertScaleAbs(result[::2], alpha=0.92)

        center = int(height * 0.1 * np.sin(self.frame_counter * 0.02) + height * 0.4)
        half = int(height * 0.05) // 2
        y0, y1 = max(0, center - half), min(height, center + half)
        if y1 > y0:
            result[y0:y1] = cv2.convertScaleAbs(result[y0:y1], alpha=0.85)

        if rng.random() < 0.015:
            line = int(rng.integers(0, height))
            y1 = min(line + int(rng.integers(1, 3)), height)
            kind = rng.random()
            if kind < 0.3:
                result[line:y1] = 255
            elif kind < 0.6:
                result[line:y1] = 0
            else:
                result[line:y1] = np.roll(result[line:y1], int(rng.integers(-15, 15)), axis=1)
        return result

    def _apply_tracking_issues(self, frame: np.ndarray) -> np.ndarray:
        """Langsam wandernder vertikaler Versatz"""
        rng = self.rng
        if rng.random() < 0.01:
            self.tracking_direction *= -1
        if self.frame_counter % 120 == 0:
            self.tracking_offset = 0
        self.tracking_offset += self.tracking_direction * int(rng.integers(0, 2))
        self.tracking_offset = max(-12, min(12, self.tracking_offset))
        return shift_image(frame, 0, self.tracking_offset)

    def _apply_horizontal_jitter(self, frame: np.ndarray) -> np.ndarray:
        """Zeilenweiser Versatz in einem schmalen Band, stark in der Mitte"""
        rng = self.rng
        height = frame.shape[0]
        result = frame.copy()
        start = rand_int(rng, 0, height - 20)
        band = int(rng.integers(5, 15))
        end = min(start + band, height)
        mid = (start + end) / 2
        for y in range(start, end):
            jitter_max = int(15 * (1 - abs(y - mid) / (band / 2)))
            if jitter_max > 0:
                result[y] = np.roll(result[y], int(rng.integers(-jitter_max, jitter_max)), axis=0)
        return result

    def _apply_noise(self, frame: np.ndarray) -> np.ndarray:
        """Körnung mit kanalabhängiger Stärke und seltene vertikale Rauschstreifen"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = cv2.add(frame, self.noise.get(rng, height, width))

        if rng.random() < 0.1:
            for _ in range(int(rng.integers(1, 3))):
                stripe = int(rng.integers(1, 3))
                x = rand_int(rng, 0, width - stripe)
                intensity = int(rng.integers(10, 30))
                result[:, x:x + stripe] = cv2.add(
                    result[:, x:x + stripe], rng.integers(0, intensity, (height, stripe, 3), dtype=np.uint8))
        return result

    def _apply_dropouts(self, frame: np.ndarray) -> np.ndarray:
        """Vereinzelte helle, dunkle oder farbige Flecken"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        for _ in range(int(rng.integers(2, 10))):
            color = tuple(int(c) for c in self.DROPOUT_COLORS[rng.integers(0, len(self.DROPOUT_COLORS))])
            center = (int(rng.integers(0, width)), int(rng.integers(0, height)))
            cv2.circle(result, center, int(rng.integers(1, 3)), color, -1)
        return result

    def _adjust_color_saturation(self, frame: np.ndarray) -> np.ndarray:
        """Schwankende Sättigung und etwas mehr Kontrast"""
        hsv = cv2.cvtColor(frame, cv2.COLOR_RGB2HSV)
        saturation = 1.1 + 0.1 * np.sin(self.frame_counter * 0.05)
        hsv[:, :, 1] = cv2.convertScaleAbs(hsv[:, :, 1], alpha=saturation)
        hsv[:, :, 2] = cv2.convertScaleAbs(hsv[:, :, 2], alpha=1.05, beta=-5)
        return cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)

    def _apply_image_pumping(self, frame: np.ndarray) -> np.ndarray:
        """Periodisches Helligkeitspumpen und flackernde Zeilen"""
        rng = self.rng
        self.brightness_pump_phase += 0.03
        pump = 1.0 + 0.08 * (np.sin(self.brightness_pump_phase) + 0.3 * np.sin(self.brightness_pump_phase * 2.7))
        result = scale_image(frame, pump)

        if rng.random() < 0.2:
            height = frame.shape[0]
            for _ in range(int(rng.integers(1, 3))):
                y = int(rng.integers(0, height))
                y1 = min(y + int(rng.integers(1, 3)), height)
                result[y:y1] = scale_image(result[y:y1], 1.0 + rng.uniform(-0.2, 0.2))
        return result


class OldFilmEffectStrategy(IEffectStrategy):
    """Filmlook der 1910er: Sepia, Korn, Flackern, Kratzer, Staub, Bildsprünge"""

    category = "Retro & VHS"

    SEPIA = (60, 40, 20)   # R, G, B

    def __init__(self):
        self.rng = np.random.default_rng()
        self.frame_counter = 0
        self.last_frame: Optional[np.ndarray] = None
        self.flicker_value = 1.0
        self.frame_jump_offset = 0
        self.last_jump_time = 0.0
        self.last_filmburn_time = 0.0
        self.fps_factor = 1.0
        # Grobes Korn mit Standardabweichung 1, Stärke wird pro Bild skaliert
        self.grain = NoisePool(self._make_grain)
        self.scratches = [{
            'x': self.rng.uniform(0, 1),
            'width': int(self.rng.integers(1, 4)),
            'intensity': self.rng.uniform(0.3, 0.9),
            'length': self.rng.uniform(0.3, 1.0),
        } for _ in range(5)]
        self.dust_particles = [{
            'x': self.rng.uniform(0, 1),
            'y': self.rng.uniform(0, 1),
            'size': int(self.rng.integers(1, 6)),
            'intensity': self.rng.uniform(0.1, 1.0),
        } for _ in range(30)]

    @property
    def name(self) -> str:
        return "Uralter Film (1910er)"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        now = time.time()
        rng = self.rng
        self.frame_counter += 1
        output_size = (frame.shape[1], frame.shape[0])
        frame = old_film_detail(frame)
        if self.last_frame is None or self.last_frame.shape != frame.shape:
            self.last_frame = frame.copy()

        result = self._convert_to_sepia(frame)
        result = self._apply_grain(result)
        result = self._apply_exposure(result)      # Flackern + Vignette in einem Schritt
        result = self._apply_scratches(result)
        result = self._apply_dust(result)
        result = self._apply_frame_jump(result, now)

        if rng.random() < 0.005:
            result = self._apply_film_break(result)
        if rng.random() < 0.002 and now - self.last_filmburn_time > 10.0:
            result = self._apply_film_burn(result)
            self.last_filmburn_time = now

        # Übersprungene Frames bei schwankender Bildrate
        if self.frame_counter % int(20 * self.fps_factor) == 0:
            result = self.last_frame.copy()
        self.fps_factor = 1.0 + 0.2 * np.sin(self.frame_counter * 0.03)

        if self.frame_counter % 2 == 0:
            self.last_frame = result.copy()
        return cv2.resize(result, output_size, interpolation=cv2.INTER_LINEAR)

    def _convert_to_sepia(self, frame: np.ndarray) -> np.ndarray:
        """Graustufen mit schwankendem Kontrast und Sepiaton"""
        gray = cv2.cvtColor(frame, cv2.COLOR_RGB2GRAY)
        contrast = 1.3 + 0.15 * np.sin(self.frame_counter * 0.02)
        mean = float(np.mean(gray))
        gray = cv2.convertScaleAbs(gray, alpha=contrast, beta=mean * (1 - contrast))
        return cv2.add(cv2.cvtColor(gray, cv2.COLOR_GRAY2RGB), self.SEPIA + (0,))

    @staticmethod
    def _make_grain(rng: np.random.Generator, height: int, width: int) -> np.ndarray:
        grain = cv2.GaussianBlur(rng.standard_normal((height, width), dtype=np.float32), (3, 3), 0)
        return grain / max(float(grain.std()), 1e-6)

    def _apply_grain(self, frame: np.ndarray) -> np.ndarray:
        """Grobes Filmkorn (Helligkeitskorn wie bei Schwarzweißfilm)"""
        height, width = frame.shape[:2]
        intensity = 20 + 5 * np.sin(self.frame_counter * 0.1)
        grain = (self.grain.get(self.rng, height, width) * intensity).astype(np.int16)
        return np.clip(frame.astype(np.int16) + grain[..., None], 0, 255).astype(np.uint8)

    def _apply_exposure(self, frame: np.ndarray) -> np.ndarray:
        """Projektorflackern, ungleichmäßige Belichtung und wandernde Vignette"""
        height, width = frame.shape[:2]
        self.flicker_value = float(np.clip(
            self.flicker_value + self.rng.normal(0, 0.05) + 0.02 * np.sin(self.frame_counter * 0.2), 0.5, 1.5))

        # Alle Muster sind weich: in 1/4 Auflösung rechnen und hochskalieren
        small_h, small_w = max(2, height // 4), max(2, width // 4)
        ys = np.linspace(0, height - 1, small_h, dtype=np.float32)
        xs = np.linspace(0, width - 1, small_w, dtype=np.float32)
        rows = 0.95 + 0.1 * np.sin(ys * 0.01 + self.frame_counter * 0.05)
        cols = 0.95 + 0.1 * np.sin(xs * 0.005 + self.frame_counter * 0.02)

        cx = width / 2 * (1 + 0.1 * np.sin(self.frame_counter * 0.02))
        cy = height / 2 * (1 + 0.1 * np.cos(self.frame_counter * 0.03))
        max_dist = np.hypot(width / 2, height / 2)
        dist = np.hypot(xs[None, :] - cx, ys[:, None] - cy)
        vignette = (1 - np.clip(dist / (max_dist * 0.65), 0, 1)) ** 2.5

        exposure = (rows[:, None] * cols[None, :] * self.flicker_value *
                    (0.32 + 0.68 * vignette)).astype(np.float32)
        exposure = cv2.resize(exposure, (width, height), interpolation=cv2.INTER_LINEAR)
        return scale_image(frame, exposure)

    def _apply_scratches(self, frame: np.ndarray) -> np.ndarray:
        """Dauerhafte und gelegentliche vertikale Kratzer"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()

        def draw(x: int, scratch_width: int, length: float, bright: int, dark: int, bright_share: float):
            start = int((1 - length) * height * rng.random())
            end = min(height, int(start + length * height))
            x1 = min(width, x + scratch_width)
            if end <= start or x1 <= x:
                return
            values = np.where(rng.random((end - start, x1 - x)) < bright_share, bright, dark).astype(np.uint8)
            result[start:end, x:x1] = values[..., None]

        for s in self.scratches:
            draw(int(width * s['x']), s['width'], s['length'],
                 int(255 * s['intensity']), int(50 * s['intensity']), 0.7)

        if rng.random() < 0.05:
            for _ in range(int(rng.integers(1, 3))):
                draw(int(rng.integers(0, width)), int(rng.integers(1, 3)), rng.uniform(0.2, 0.9), 255, 0, 0.8)
        return result

    def _apply_dust(self, frame: np.ndarray) -> np.ndarray:
        """Dauerhafte und zufällige Staubpartikel"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        for dust in self.dust_particles:
            if rng.random() < 0.3:
                value = int(255 * dust['intensity']) if rng.random() < 0.7 else int(30 * dust['intensity'])
                center = (int(width * dust['x']), int(height * dust['y']))
                cv2.circle(result, center, dust['size'], (value, value, value), -1)

        if rng.random() < 0.2:
            for _ in range(int(rng.integers(5, 20))):
                value = int(rng.integers(200, 256)) if rng.random() < 0.7 else int(rng.integers(0, 50))
                center = (int(rng.integers(0, width)), int(rng.integers(0, height)))
                cv2.circle(result, center, int(rng.integers(1, 4)), (value, value, value), -1)
        return result

    def _apply_frame_jump(self, frame: np.ndarray, now: float) -> np.ndarray:
        """Mechanische Bildsprünge und Projektorwackeln"""
        rng = self.rng
        if rng.random() < 0.02 or (now - self.last_jump_time > 2.0 and rng.random() < 0.3):
            self.frame_jump_offset = int(rng.integers(-20, 21))
            self.last_jump_time = now
        if abs(self.frame_jump_offset) > 1:
            self.frame_jump_offset = int(self.frame_jump_offset * 0.8)
        else:
            self.frame_jump_offset = 0

        wobble = int(2 * np.sin(self.frame_counter * 0.2))
        if wobble == 0 and self.frame_jump_offset == 0:
            return frame
        return shift_image(frame, wobble, self.frame_jump_offset, cv2.BORDER_CONSTANT)

    def _apply_film_break(self, frame: np.ndarray) -> np.ndarray:
        """Filmriss: heller Querstreifen, dunkler Längsriss oder diagonaler Riss"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        kind = rng.random()

        if kind < 0.4:
            y = int(rng.integers(0, height))
            stripe = int(rng.integers(5, 20))
            result[y:y + stripe] = 255
            for x in range(0, width, 2):
                tear = int(rng.integers(-3, 4))
                result[max(0, y + tear):min(height, y + stripe + tear), x] = 255
        elif kind < 0.7:
            x = int(rng.integers(0, width))
            stripe = int(rng.integers(5, 30))
            result[:, x:x + stripe] = 0
            x0, x1 = max(0, x - 20), min(width, x + stripe + 20)
            for y in range(0, height, 3):
                result[y, x0:x1] = np.roll(result[y, x0:x1], int(rng.integers(-10, 11)), axis=0)
        else:
            start = (int(rng.integers(0, max(1, width // 2))), int(rng.integers(0, max(1, height // 2))))
            end = (rand_int(rng, width // 2, width), rand_int(rng, height // 2, height))
            cv2.line(result, start, end, (255, 255, 255), int(rng.integers(2, 8)))
            for t in np.linspace(0, 1, 20):
                cx = int(start[0] + t * (end[0] - start[0]))
                cy = int(start[1] + t * (end[1] - start[1]))
                radius = int(rng.integers(10, 30))
                x0, x1 = max(0, cx - radius), min(width, cx + radius)
                y0, y1 = max(0, cy - radius), min(height, cy + radius)
                if x1 > x0 and y1 > y0:
                    result[y0:y1, x0:x1] = shift_image(result[y0:y1, x0:x1],
                                                       int(rng.integers(-5, 6)), int(rng.integers(-5, 6)))
        return result

    def _apply_film_burn(self, frame: np.ndarray) -> np.ndarray:
        """Durchgebranntes Filmbild: Brandloch in der Mitte oder an einer Ecke"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()

        if rng.random() < 0.7:
            cx = rand_int(rng, width // 4, width * 3 // 4)
            cy = rand_int(rng, height // 4, height * 3 // 4)
            radius = rand_int(rng, 30, max(50, min(width, height) // 3))
            for r in range(1, radius + 1):
                color = (0, 0, 0) if rng.random() < 0.7 else (255, 255, 255)
                cv2.circle(result, (cx, cy), r, color, 1)
            cv2.circle(result, (cx, cy), radius // 2, (0, 0, 0), -1)
            ring = np.zeros_like(result)
            cv2.circle(ring, (cx, cy), radius, (255, 255, 255), int(rng.integers(2, 6)))
            result = cv2.addWeighted(result, 1.0, ring, 0.8, 0)
        else:
            cx, cy = [(0, 0), (width - 1, 0), (width - 1, height - 1), (0, height - 1)][int(rng.integers(0, 4))]
            size = int(rng.integers(30, 100))
            cv2.rectangle(result, (cx - size // 2, cy - size // 2), (cx + size // 2, cy + size // 2), (0, 0, 0), -1)
            for _ in range(5):
                grow = size // 2 + int(rng.integers(5, 20))
                cv2.rectangle(result, (cx - grow, cy - grow), (cx + grow, cy + grow),
                              (255, 255, 255), int(rng.integers(1, 4)))

        for _ in range(int(rng.integers(3, 10))):
            color = (0, 0, 0) if rng.random() < 0.7 else (255, 255, 255)
            center = (int(rng.integers(0, width)), int(rng.integers(0, height)))
            cv2.circle(result, center, int(rng.integers(2, 10)), color, -1)
        return result
