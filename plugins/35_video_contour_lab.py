"""Kontur-Labor: vier berechnete Transformationen aus Kanten und Konturabstand."""

from typing import Any, Dict, List, Optional, Tuple

import cv2
import numpy as np

from bildeffekte import IEffectStrategy

_CLAHE = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))


def _analysis(frame: np.ndarray) -> Tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray, np.ndarray]:
    """Lokal kalibrierte Kanten, Kontrast, Gradienten und Konturabstand."""
    height, width = frame.shape[:2]
    scale = min(1.0, 640.0 / max(width, height))
    size = (max(16, round(width * scale)), max(16, round(height * scale)))
    small = cv2.resize(frame, size, interpolation=cv2.INTER_AREA) if scale < 1 else frame
    gray = cv2.cvtColor(small, cv2.COLOR_RGB2GRAY)
    gray = cv2.GaussianBlur(_CLAHE.apply(gray), (5, 5), 0)
    gx = cv2.Sobel(gray, cv2.CV_32F, 1, 0, ksize=3)
    gy = cv2.Sobel(gray, cv2.CV_32F, 0, 1, ksize=3)
    magnitude = cv2.magnitude(gx, gy)
    reference = max(1.0, float(np.percentile(magnitude[::4, ::4], 95)))
    strength = np.clip(magnitude / reference, 0.0, 1.0)
    high = float(np.clip(reference * 0.55, 12, 180))
    edges = cv2.Canny(gray, high * 0.42, high)
    distance = cv2.distanceTransform(255 - edges, cv2.DIST_L2, 3)
    return edges, strength, gx, gy, distance


def _expand(field: np.ndarray, frame: np.ndarray) -> np.ndarray:
    height, width = frame.shape[:2]
    if field.shape[:2] == (height, width):
        return field
    return cv2.resize(field, (width, height), interpolation=cv2.INTER_LINEAR)


class _Grid:
    def __init__(self):
        self.shape = None
        self.x = self.y = None

    def get(self, height: int, width: int) -> Tuple[np.ndarray, np.ndarray]:
        if self.shape != (height, width):
            self.x, self.y = np.meshgrid(np.arange(width, dtype=np.float32),
                                          np.arange(height, dtype=np.float32))
            self.shape = (height, width)
        return self.x, self.y


class KonturMagnetfeld(IEffectStrategy):
    """Kanten drücken Pixel wie ein Kraftfeld; Ziehen in der Vorschau verformt lokal."""

    name = "Kontur-Magnetfeld"
    category = "Kontur-Labor"
    supports_drag = True
    description = "Kanten verformen das Bild; in der Vorschau auf Augen, Mund oder andere Stellen klicken und ziehen."

    def __init__(self):
        self.tick = 0
        self.grid = _Grid()
        self.drag: Optional[Tuple[float, float, float, float]] = None

    def activate(self) -> None:
        self.drag = None

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        height, width = frame.shape[:2]
        _, strength, _, _, distance = _analysis(frame)
        dx = cv2.Sobel(distance, cv2.CV_32F, 1, 0, ksize=3)
        dy = cv2.Sobel(distance, cv2.CV_32F, 0, 1, ksize=3)
        length = cv2.magnitude(dx, dy) + 1e-4
        contrast = cv2.GaussianBlur(strength, (0, 0), 3)
        force = np.exp(-distance / 9.0) * (2.0 + 14.0 * contrast)
        pulse = np.sin(distance * 0.22 - self.tick * 0.12)
        fx = _expand(dx / length * force * pulse, frame)
        fy = _expand(dy / length * force * pulse, frame)
        x, y = self.grid.get(height, width)
        map_x = x + fx
        map_y = y + fy

        incoming = config.get('warp_drag')
        if incoming is not None:
            self.drag = incoming
        elif self.drag is not None:
            cx, cy, vx, vy = self.drag
            vx *= 0.93
            vy *= 0.93
            self.drag = (cx, cy, vx, vy) if abs(vx) + abs(vy) > 0.002 else None
        if self.drag is not None:
            cx, cy, vx, vy = self.drag
            radius = max(12.0, min(width, height) * 0.18)
            radius_sq = (x - cx * width) ** 2 + (y - cy * height) ** 2
            influence = np.exp(-radius_sq / (2.0 * radius * radius))
            influence *= 0.45 + 0.55 * np.clip(_expand(contrast, frame) * 2.0, 0, 1)
            map_x -= vx * width * influence
            map_y -= vy * height * influence
        return cv2.remap(frame, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)


class SilhouettenWellen(IEffectStrategy):
    """Konturabstand erzeugt laufende Farbringe und eine leichte Brechung."""

    name = "Silhouetten-Wellen"
    category = "Kontur-Labor"
    description = "Wandernde Farbringe und Bildbrechung folgen dem Abstand zu sichtbaren Konturen."

    def __init__(self):
        self.tick = 0
        self.grid = _Grid()

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        height, width = frame.shape[:2]
        _, strength, _, _, distance = _analysis(frame)
        small_height, small_width = distance.shape
        contrast = cv2.GaussianBlur(strength, (0, 0), 3)
        phase = distance * 0.34 - self.tick * 0.22
        ring = np.sin(phase)
        visible = np.exp(-distance / 34.0) * (0.2 + 0.8 * np.clip(contrast * 4, 0, 1))
        light = np.clip((ring + 1) * 0.5 * visible, 0, 1)
        x, y = self.grid.get(height, width)
        gx = cv2.Sobel(distance, cv2.CV_32F, 1, 0, ksize=3)
        gy = cv2.Sobel(distance, cv2.CV_32F, 0, 1, ksize=3)
        displacement = 4.0 * ring * visible
        shift_x = cv2.resize(gx * displacement, (width, height)) * (width / small_width)
        shift_y = cv2.resize(gy * displacement, (width, height)) * (height / small_height)
        warped = cv2.remap(frame, x + shift_x, y + shift_y,
                           cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)
        overlay = np.stack((light * 40, light * 205, light * 255), axis=-1).astype(np.uint8)
        if overlay.shape[:2] != (height, width):
            overlay = cv2.resize(overlay, (width, height))
        return cv2.addWeighted(warped, 0.76, overlay, 0.9, 0)


class Lichtskulptur(IEffectStrategy):
    """Ein bewegtes Seitenlicht schattiert das aus Konturen abgeleitete Höhenfeld."""

    name = "Lichtskulptur"
    category = "Kontur-Labor"
    description = "Ein wanderndes Licht macht aus Kontrast und Konturabstand ein künstliches Relief."

    def __init__(self):
        self.tick = 0

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.tick += 1
        height, width = frame.shape[:2]
        _, strength, _, _, distance = _analysis(frame)
        small_height, small_width = distance.shape
        distance = np.minimum(distance, 35.0)
        gx = cv2.Sobel(distance, cv2.CV_32F, 1, 0, ksize=3)
        gy = cv2.Sobel(distance, cv2.CV_32F, 0, 1, ksize=3)
        angle = self.tick * 0.025
        light = (gx * np.cos(angle) + gy * np.sin(angle)) / (cv2.magnitude(gx, gy) + 2.0)
        relief = np.clip(0.63 + 0.42 * light + 0.28 * strength, 0.12, 1.3)
        small = cv2.resize(frame, (small_width, small_height), interpolation=cv2.INTER_AREA)
        gray = cv2.cvtColor(small, cv2.COLOR_RGB2GRAY).astype(np.float32)
        metal = np.stack((gray * 1.08 + 18, gray * 0.82 + 20, gray * 0.58 + 24), axis=-1)
        sculpture = np.clip(metal * relief[..., None], 0, 255).astype(np.uint8)
        if sculpture.shape[:2] != (height, width):
            sculpture = cv2.resize(sculpture, (width, height), interpolation=cv2.INTER_LINEAR)
        return cv2.addWeighted(sculpture, 0.87, frame, 0.13, 0)


class KonturPartikel(IEffectStrategy):
    """Funken entstehen an starken Kanten und fließen entlang ihrer Tangente."""

    name = "Kontur-Partikel"
    category = "Kontur-Labor"
    description = "Funken entstehen an starken Kanten und fließen entlang ihrer Richtung."

    def __init__(self):
        self.rng = np.random.default_rng()
        self.particles: List[Tuple[float, float, float, float, int]] = []
        self.trails: Optional[np.ndarray] = None

    def activate(self) -> None:
        self.particles = []
        self.trails = None

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        height, width = frame.shape[:2]
        if self.trails is None or self.trails.shape != frame.shape:
            self.trails = np.zeros_like(frame)
            self.particles = []
        self.trails = cv2.convertScaleAbs(self.trails, alpha=0.87)
        edges, strength, gx, gy, _ = _analysis(frame)
        candidates = np.argwhere((edges > 0) & (strength > 0.16))
        if len(candidates):
            count = min(12, len(candidates), max(0, 220 - len(self.particles)))
            for index in self.rng.choice(len(candidates), size=count, replace=False):
                sy, sx = candidates[index]
                tangent_x, tangent_y = -float(gy[sy, sx]), float(gx[sy, sx])
                norm = max(1.0, np.hypot(tangent_x, tangent_y))
                speed = float(self.rng.uniform(1.2, 3.5))
                self.particles.append((sx * width / edges.shape[1], sy * height / edges.shape[0],
                                       tangent_x / norm * speed, tangent_y / norm * speed,
                                       int(self.rng.integers(18, 50))))
        living = []
        for x, y, vx, vy, life in self.particles:
            nx, ny = x + vx, y + vy
            if 0 <= nx < width and 0 <= ny < height and life > 1:
                cv2.line(self.trails, (int(x), int(y)), (int(nx), int(ny)),
                         (255, 210, 80), 1, cv2.LINE_AA)
                living.append((nx, ny, vx, vy, life - 1))
        self.particles = living
        dark = cv2.convertScaleAbs(frame, alpha=0.36)
        return cv2.add(dark, self.trails)
