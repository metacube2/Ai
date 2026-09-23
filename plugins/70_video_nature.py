"""
Plugin: Natur: Sand-Zerfall und biologische Wucherung

Ausgelagert aus video.py – Effekte sind reine Plugins und kennen nur das Paket
`bildeffekte`.
"""

from typing import Any, Dict, List, Optional, Tuple

import time

import cv2
import numpy as np

from bildeffekte import IEffectStrategy, scale_image, to_rgb_colormap


class SandDisintegrationEffectStrategy(IEffectStrategy):
    """Bewegte Bildteile zerbröseln zu Sand, der herabrieselt und sich unten zu Dünen häuft"""

    category = "Natur & Material"

    MAX_PARTICLES = 60000
    SPAWN_PER_FRAME = 2500
    GRAVITY = 0.35

    def __init__(self):
        self.rng = np.random.default_rng()
        self._shape = None
        self._prev_gray: Optional[np.ndarray] = None

    @property
    def name(self) -> str:
        return "Sand-Zerfall"

    def _reset(self, height: int, width: int) -> None:
        self._shape = (height, width)
        self._pos = np.zeros((0, 2), dtype=np.float32)       # x, y
        self._vel = np.zeros((0, 2), dtype=np.float32)
        self._color = np.zeros((0, 3), dtype=np.uint8)
        self._eaten = np.zeros((height, width), dtype=np.float32)
        self._dune = np.zeros(width, dtype=np.float32)
        self._dune_color = np.zeros((width, 3), dtype=np.float32)
        self._prev_gray = None

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        height, width = frame.shape[:2]
        if self._shape != (height, width):
            self._reset(height, width)

        gray = cv2.cvtColor(frame, cv2.COLOR_RGB2GRAY)
        if self._prev_gray is None:
            self._prev_gray = gray
        motion = cv2.absdiff(gray, self._prev_gray)
        self._prev_gray = gray

        # Wo sich etwas bewegt (oder an hellen Kanten), lösen sich Körner.
        # Stichproben statt jedes Pixel einzeln zu würfeln.
        samples = min(height * width, self.SPAWN_PER_FRAME * 12)
        ys = rng.integers(0, height, samples)
        xs = rng.integers(0, width, samples)
        edges = cv2.Canny(gray, 80, 160)
        movement = np.maximum(motion[ys, xs].astype(np.float32) - 12.0, 0.0)   # leichtes Flimmern zählt nicht
        chance = movement / 30.0 + (edges[ys, xs] > 0) * 0.03 + 0.001
        accept = rng.random(samples, dtype=np.float32) < chance
        ys, xs = ys[accept][:self.SPAWN_PER_FRAME], xs[accept][:self.SPAWN_PER_FRAME]
        if len(xs):
            self._eaten[ys, xs] = 1.0
            new_pos = np.column_stack([xs, ys]).astype(np.float32)
            new_vel = np.column_stack([rng.normal(0.8, 0.8, len(xs)), rng.normal(-1.0, 0.8, len(xs))]).astype(np.float32)
            self._pos = np.vstack([self._pos, new_pos])[-self.MAX_PARTICLES:]
            self._vel = np.vstack([self._vel, new_vel])[-self.MAX_PARTICLES:]
            self._color = np.vstack([self._color, frame[ys, xs]])[-self.MAX_PARTICLES:]

        # Physik: Schwerkraft, böiger Wind, Aufprall auf die Düne
        wind = 0.15 * np.sin(time.time() * 0.7) + rng.normal(0, 0.05, len(self._vel))
        self._vel[:, 0] = self._vel[:, 0] * 0.99 + wind
        self._vel[:, 1] += self.GRAVITY
        self._pos += self._vel
        self._pos[:, 0] %= width
        column = np.clip(self._pos[:, 0].astype(np.int64), 0, width - 1)
        landed = self._pos[:, 1] >= height - 1 - self._dune[column]
        if landed.any():
            cols = column[landed]
            np.add.at(self._dune, cols, 0.35)
            counts = np.bincount(cols, minlength=width)[:, None]
            colors = np.zeros((width, 3), dtype=np.float32)
            np.add.at(colors, cols, self._color[landed].astype(np.float32))
            has = counts[:, 0] > 0
            self._dune_color[has] = self._dune_color[has] * 0.7 + (colors[has] / counts[has]) * 0.3
            keep = ~landed
            self._pos, self._vel, self._color = self._pos[keep], self._vel[keep], self._color[keep]

        # Dünen rutschen seitlich ab und sacken langsam ein
        self._dune = cv2.GaussianBlur(self._dune[None, :], (9, 1), 0).ravel() * 0.998
        self._dune = np.minimum(self._dune, height * 0.45)
        self._eaten *= 0.93

        # Zeichnen: ausgefressenes Bild, fliegender Sand, Dünen
        holes = cv2.GaussianBlur(self._eaten, (5, 5), 0)
        result = scale_image(frame, np.clip(1.0 - holes * 1.6, 0.05, 1.0).astype(np.float32))
        px = np.clip(self._pos[:, 0].astype(np.int64), 0, width - 1)
        py = self._pos[:, 1].astype(np.int64)
        visible = (py >= 0) & (py < height)
        result[py[visible], px[visible]] = self._color[visible]
        dune_top = (height - self._dune).astype(np.int64)
        first = int(np.clip(dune_top.min(), 0, height))
        if first < height:                                   # nur den Bereich unter der höchsten Düne zeichnen
            rows = np.arange(first, height)[:, None]
            in_dune = rows >= dune_top[None, :]
            shade = np.clip(0.6 + 0.4 * (rows - dune_top[None, :]) / 40.0, 0.6, 1.0)[..., None]
            dune_img = (self._dune_color[None, :, :] * (1.3 - shade * 0.5)).astype(np.uint8)
            result[first:] = np.where(in_dune[..., None], dune_img, result[first:])
        return result


class BioGrowthEffectStrategy(IEffectStrategy):
    """
    Reaktions-Diffusion (Gray-Scott): Ein chemisches Muster wächst wie Korallen
    oder Zellteilung aus den Kanten des Videos. Helle Bereiche nähren, dunkle töten.
    """

    category = "Natur & Material"

    SCALE = 4
    ITERATIONS = 16
    SEED_KERNEL = np.ones((5, 5), dtype=np.uint8)       # einzelne Pixel als Keim zerfließen sofort
    LAPLACE = np.array([[0.05, 0.2, 0.05], [0.2, -1.0, 0.2], [0.05, 0.2, 0.05]], dtype=np.float32)
    PRESETS = [(0.039, 0.058), (0.029, 0.057), (0.0367, 0.0649), (0.0545, 0.062)]   # Würmer, Labyrinth, Zellen, Koralle

    def __init__(self):
        self.rng = np.random.default_rng()
        self.frame_counter = 0
        self._shape = None
        self._preset = 0
        self._frames_until_switch = 300

    @property
    def name(self) -> str:
        return "Bio-Wucherung"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        height, width = frame.shape[:2]
        sw, sh = max(32, width // self.SCALE), max(32, height // self.SCALE)
        if self._shape != (sh, sw):
            self._u = np.ones((sh, sw), dtype=np.float32)
            self._v = np.zeros((sh, sw), dtype=np.float32)
            self._shape = (sh, sw)

        self.frame_counter += 1
        self._frames_until_switch -= 1
        if self._frames_until_switch <= 0:
            self._preset = (self._preset + 1) % len(self.PRESETS)
            self._frames_until_switch = int(rng.integers(240, 480))
        feed, kill = self.PRESETS[self._preset]

        small = cv2.resize(frame, (sw, sh), interpolation=cv2.INTER_AREA)
        gray = cv2.cvtColor(small, cv2.COLOR_RGB2GRAY)
        light = gray.astype(np.float32) / 255.0
        edges = cv2.Canny(gray, 60, 140) > 0
        seeds = (edges & (rng.random((sh, sw)) < 0.01)).astype(np.uint8)
        seeds = cv2.dilate(seeds, self.SEED_KERNEL) > 0
        self._u[seeds] = 0.5
        self._v[seeds] = 0.25

        # Nur leichte Modulation: stärkere Änderungen verlassen den Bereich, in dem Muster entstehen
        feed_map = feed * (0.9 + 0.2 * light)
        kill_map = kill + (1.0 - light) * 0.0015
        u, v = self._u, self._v
        for _ in range(self.ITERATIONS):
            lap_u = cv2.filter2D(u, -1, self.LAPLACE, borderType=cv2.BORDER_REFLECT)
            lap_v = cv2.filter2D(v, -1, self.LAPLACE, borderType=cv2.BORDER_REFLECT)
            reaction = u * v * v
            u = u + (1.0 * lap_u - reaction + feed_map * (1.0 - u))
            v = v + (0.5 * lap_v + reaction - (kill_map + feed_map) * v)
            np.clip(u, 0, 1, out=u)
            np.clip(v, 0, 1, out=v)
        self._u, self._v = u, v

        pattern = cv2.resize(v, (width, height), interpolation=cv2.INTER_CUBIC)
        level = np.clip(pattern * 3.5, 0, 1)
        hue_shift = (self.frame_counter // 3) % 256
        colored = to_rgb_colormap(((level * 200 + hue_shift) % 256).astype(np.uint8), cv2.COLORMAP_TWILIGHT_SHIFTED)
        # Leuchten in kleiner Auflösung weichzeichnen: großer Kernel in voller Auflösung ist teuer
        glow_small = to_rgb_colormap(((np.clip(v * 3.5, 0, 1) * 200 + hue_shift) % 256).astype(np.uint8),
                                     cv2.COLORMAP_TWILIGHT_SHIFTED)
        glow = cv2.resize(cv2.GaussianBlur(glow_small, (0, 0), 1.5), (width, height), interpolation=cv2.INTER_LINEAR)
        alpha = (level * 0.85)[..., None]
        result = frame.astype(np.float32) * (1 - alpha) + colored.astype(np.float32) * alpha
        result = cv2.add(np.clip(result, 0, 255).astype(np.uint8), (glow * 0.35).astype(np.uint8))
        return result
