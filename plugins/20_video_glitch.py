"""
Plugin: Digitale Störungen: Realtime Glitch, Datamosh, Pixel-Sorting

Ausgelagert aus video.py – Effekte sind reine Plugins und kennen nur das Paket
`bildeffekte`.
"""

from typing import Any, Dict, List, Optional, Tuple

import time

import cv2
import numpy as np

from bildeffekte import IEffectStrategy, PixelGrid, rand_int, random_box, shift_image


class RealTimeGlitchEffectStrategy(IEffectStrategy):
    """Moderner Echtzeit-Glitch mit digitalen Störungen"""

    def __init__(self):
        self.rng = np.random.default_rng()
        self.last_frame: Optional[np.ndarray] = None
        self.glitch_intensity = 0.7
        self.last_major_glitch_time = 0.0
        self.major_glitch_interval = 0.8

    @property
    def name(self) -> str:
        return "Realtime Glitch"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        now = time.time()
        height, width = frame.shape[:2]
        if self.last_frame is None or self.last_frame.shape != frame.shape:
            self.last_frame = frame.copy()

        glitched = frame.copy()

        # 1. RGB-Kanalverschiebung
        if rng.random() < 0.7:
            r, g, b = cv2.split(glitched)
            k = self.glitch_intensity
            r = shift_image(r, int(rng.integers(-10, 10) * k), int(rng.integers(-5, 5) * k), cv2.BORDER_WRAP)
            g = shift_image(g, int(rng.integers(-10, 10) * k), int(rng.integers(-5, 5) * k), cv2.BORDER_WRAP)
            glitched = cv2.merge([r, g, b])

        # 2. Horizontale Störlinien (hell, dunkel, farbig)
        if rng.random() < 0.4:
            rows = np.arange(0, height, rng.integers(2, 10))
            u = rng.random(len(rows))
            glitched[rows[u < 0.3]] = 255
            glitched[rows[(u >= 0.3) & (u < 0.44)]] = 0
            colored = rows[(u >= 0.44) & (u < 0.496)]
            if len(colored):
                glitched[colored] = rng.integers(0, 255, (len(colored), 1, 3), dtype=np.uint8)

        # 3. Blockige Artefakte / Pixelation
        if rng.random() < 0.2:
            block = int(rng.integers(5, 20))
            small = cv2.resize(glitched, (max(1, width // block), max(1, height // block)))
            glitched = cv2.resize(small, (width, height), interpolation=cv2.INTER_NEAREST)

        # 4. Große Glitches: horizontale Verschiebung eines Streifens
        if now - self.last_major_glitch_time > self.major_glitch_interval and rng.random() < 0.6:
            line = rand_int(rng, height // 4, max(height // 4 + 1, height * 3 // 4))
            band = int(rng.integers(10, 50))
            glitched[line:line + band] = np.roll(glitched[line:line + band], int(rng.integers(-100, 100)), axis=1)
            self.last_major_glitch_time = now

        # 5. Rauschen
        if rng.random() < 0.3:
            glitched = cv2.add(glitched, rng.integers(0, 50, glitched.shape, dtype=np.uint8))

        # 6. Welleneffekt
        if rng.random() < 0.2:
            amplitude = rng.integers(3, 10)
            period = rng.integers(30, 100)
            map_x = np.broadcast_to(np.arange(width, dtype=np.float32), (height, width))
            map_y = (np.arange(height, dtype=np.float32)[:, None]
                     + amplitude * np.sin(np.arange(width, dtype=np.float32) / period)[None, :])
            glitched = cv2.remap(glitched, np.ascontiguousarray(map_x), map_y.astype(np.float32), cv2.INTER_LINEAR)

        # 7. Farbumkehrung in zufälligem Bereich
        if rng.random() < 0.1:
            x, y, bw, bh = random_box(rng, width, height, 1, width)
            glitched[y:y + bh, x:x + bw] = 255 - glitched[y:y + bh, x:x + bw]

        # 8. Ghosting mit vorherigem Frame
        if rng.random() < 0.3:
            ghost = rng.random() * 0.3
            glitched = cv2.addWeighted(glitched, 1 - ghost, self.last_frame, ghost, 0)

        self.last_frame = glitched.copy()
        return glitched


class DatamoshEffectStrategy(IEffectStrategy):
    """
    Datamosh wie bei Videos ohne Keyframes: Die Bewegung des aktuellen Bildes
    schiebt die alten Pixel weiter, in 16er-Makroblöcken wie bei einem Codec.
    Nur gelegentlich kommt ein 'Keyframe' mit dem echten Bild.
    """

    BLOCK = 16
    FLOW_SCALE = 4          # Optical Flow in 1/4 Auflösung

    def __init__(self):
        self.rng = np.random.default_rng()
        self.grid = PixelGrid()
        self.flow = cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_ULTRAFAST)
        self.prev_small: Optional[np.ndarray] = None
        self.mosh: Optional[np.ndarray] = None
        self.frames_until_keyframe = 0
        self.bloom = 1.0

    @property
    def name(self) -> str:
        return "Datamosh"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        height, width = frame.shape[:2]
        # DIS-Flow braucht mindestens 12 Pixel; kleine Regionen werden dafür hochskaliert
        small_size = (max(16, width // self.FLOW_SCALE), max(16, height // self.FLOW_SCALE))
        small = cv2.resize(cv2.cvtColor(frame, cv2.COLOR_RGB2GRAY), small_size, interpolation=cv2.INTER_AREA)

        if self.mosh is None or self.mosh.shape != frame.shape or self.frames_until_keyframe <= 0:
            self.mosh = frame.copy()
            self.prev_small = small
            self.frames_until_keyframe = int(rng.integers(90, 240))   # 3–8 s bei 30 fps
            return self.mosh
        self.frames_until_keyframe -= 1

        flow = self.flow.calc(self.prev_small, small, None)
        self.prev_small = small
        # Vektoren von der Flow-Auflösung auf Bildpixel umrechnen
        flow[..., 0] *= width / small_size[0]
        flow[..., 1] *= height / small_size[1]

        # Makroblöcke: ein Bewegungsvektor pro 16x16-Block
        blocks = (max(1, width // self.BLOCK), max(1, height // self.BLOCK))
        flow = cv2.resize(flow, blocks, interpolation=cv2.INTER_AREA)
        flow = cv2.resize(flow, (width, height), interpolation=cv2.INTER_NEAREST)

        # Gelegentliches "Aufblühen": Bewegung wird für ein bis zwei Sekunden verstärkt verschmiert
        if rng.random() < 0.01:
            self.bloom = rng.uniform(1.5, 3.0)
        self.bloom = 1.0 + (self.bloom - 1.0) * 0.97
        scale = self.bloom

        xs, ys = self.grid.get(height, width)
        self.mosh = cv2.remap(self.mosh, xs - flow[..., 0] * scale, ys - flow[..., 1] * scale,
                              cv2.INTER_NEAREST, borderMode=cv2.BORDER_REPLICATE)

        # Einzelne Blöcke bekommen frische Pixel (wie verlorene Referenzblöcke)
        if rng.random() < 0.3:
            for _ in range(int(rng.integers(1, 6))):
                bx = int(rng.integers(0, max(1, width // self.BLOCK))) * self.BLOCK
                by = int(rng.integers(0, max(1, height // self.BLOCK))) * self.BLOCK
                self.mosh[by:by + self.BLOCK, bx:bx + self.BLOCK] = frame[by:by + self.BLOCK, bx:bx + self.BLOCK]
        return self.mosh


class PixelSortEffectStrategy(IEffectStrategy):
    """
    Pixel-Sorting: Zusammenhängende Pixel in einem Helligkeitsband werden
    innerhalb ihres Abschnitts nach Helligkeit sortiert – schmelzende Farbschlieren.
    Richtung wechselt gelegentlich zwischen horizontal und vertikal.
    """

    def __init__(self):
        self.rng = np.random.default_rng()
        self.frame_counter = 0
        self.vertical = False
        self.frames_until_switch = 150

    @property
    def name(self) -> str:
        return "Pixel-Sorting"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.frame_counter += 1
        self.frames_until_switch -= 1
        if self.frames_until_switch <= 0:
            self.vertical = not self.vertical
            self.frames_until_switch = int(self.rng.integers(120, 300))

        work = np.ascontiguousarray(frame.transpose(1, 0, 2)) if self.vertical else frame
        result = self._sort_rows(work)
        return np.ascontiguousarray(result.transpose(1, 0, 2)) if self.vertical else result

    def _sort_rows(self, image: np.ndarray) -> np.ndarray:
        brightness = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY).astype(np.int32)
        t = self.frame_counter * 0.02
        low = int(70 + 50 * np.sin(t))
        high = int(215 + 25 * np.sin(t * 0.7))
        mask = (brightness > low) & (brightness < high)

        # Abschnitts-Nummer: erhöht sich an jedem Pixel außerhalb der Maske und an jedem Maskenbeginn,
        # so bleiben unmaskierte Pixel an ihrem Platz und jeder Abschnitt wird für sich sortiert
        starts = mask & ~np.pad(mask, ((0, 0), (1, 0)))[:, :-1]
        segment = np.cumsum(~mask | starts, axis=1, dtype=np.int32)
        key = segment * 256 + np.where(mask, 255 - brightness, 0)
        order = np.argsort(key, axis=1)
        rows_with_mask = mask.any(axis=1)

        result = image.copy()
        if rows_with_mask.any():
            sub = order[rows_with_mask]
            result[rows_with_mask] = np.take_along_axis(image[rows_with_mask], sub[..., None], axis=1)
        return result
