"""Live-Gesichtspunkte für Gesichtseffekte: asynchrone Erkennung plus optischer Fluss.

Gleiches Verfahren wie beim Matrix-Agenten (siehe docs/bildeffekte/matrix-agent.md),
hier als wiederverwendbarer Baustein. Punktreihenfolge wie `face_points`:
0–19 Gesichtsrand (0 rechts entlang der Augenachse, 5 Kinn, 15 Stirn), 20/21 Augen,
22 Nase, 23 linker Mundwinkel, 24 Mundmitte, 25 rechter Mundwinkel, 26 Ober-, 27 Unterlippe.
"""
import time

import cv2
import numpy as np
from scipy.spatial import Delaunay

from bildeffekte import face_morph


def template_points() -> np.ndarray:
    """Ein Standardgesicht mit derselben Punktreihenfolge wie die Erkennung."""
    return face_morph.face_points((0, 0, 100, 120), {})


def template_triangles() -> np.ndarray:
    """Feste Dreiecke: Die Punktreihenfolge ist immer gleich, die Topologie bleibt stabil."""
    return Delaunay(template_points()).simplices


class FaceGeometry:
    """Achsen und Maße eines erkannten Gesichts."""

    def __init__(self, points: np.ndarray):
        self.points = np.float32(points)
        eyes = self.points[20:22]
        vector = eyes[1] - eyes[0]
        self.eye_span = max(float(np.linalg.norm(vector)), 1e-5)
        self.axis = vector / self.eye_span
        self.down = np.float32([-self.axis[1], self.axis[0]])
        self.width = max(float(np.linalg.norm(self.points[0] - self.points[10])), 1.0)
        self.center = (eyes.mean(0) + self.points[26]) / 2
        self.angle = float(np.degrees(np.arctan2(vector[1], vector[0])))

    def to_image(self, u, v) -> np.ndarray:
        """Gesichtskoordinaten (in Gesichtsbreiten ab Mitte) in Bildpunkte."""
        u, v = np.asarray(u, np.float32), np.asarray(v, np.float32)
        return (self.center + u[..., None] * self.axis * self.width
                + v[..., None] * self.down * self.width).astype(np.float32)

    def uv_grid(self, shape):
        """Gesichtskoordinaten jedes Bildpunkts."""
        yy, xx = np.indices(shape, dtype=np.float32)
        dx, dy = xx - self.center[0], yy - self.center[1]
        u = (dx * self.axis[0] + dy * self.axis[1]) / self.width
        v = (dx * self.down[0] + dy * self.down[1]) / self.width
        return u, v


def face_mask(points: np.ndarray, shape, shrink: float = 0.85, feather: float = 0.12) -> np.ndarray:
    """Weiche Maske der inneren Gesichtsfläche (ohne Haare, Ohren und Kragen)."""
    geometry = FaceGeometry(points)
    center = geometry.center
    angles = np.arange(20) * 2 * np.pi / 20
    # Stirn stärker einziehen, damit der Haaransatz echt bleibt.
    factor = shrink - 0.12 * np.clip(-np.sin(angles), 0, 1) ** 2
    rim = center + (points[:20] - center) * factor[:, None]
    mask = np.zeros(shape, np.uint8)
    cv2.fillPoly(mask, [np.rint(rim).astype(np.int32)], 255)
    edge = cv2.distanceTransform(mask, cv2.DIST_L2, 3)
    value = np.clip(edge / max(3.0, geometry.width * feather), 0, 1)
    return (value * value * (3 - 2 * value)).astype(np.float32)


class FaceTracker:
    """Liefert pro Bild die 28 Gesichtspunkte im verkleinerten Arbeitsbild oder None."""

    DETECTION_GAIN = 0.3
    INTERVAL = 0.09
    MAX_AGE = 0.5

    def __init__(self, max_side: int = 512, clock=time.monotonic):
        self.max_side = max_side
        self.clock = clock
        self.reset()

    def reset(self) -> None:
        pending = getattr(self, '_pending', None)
        if pending is not None:
            pending.cancel()
        self._pending = None
        self._points = None
        self._gray = None
        self._submitted_gray = None
        self._seen = 0.0
        self._submitted = -1e9
        self.since = None  # Zeitpunkt, seit dem das Gesicht ohne Abbruch verfolgt wird

    def working_size(self, frame: np.ndarray):
        height, width = frame.shape[:2]
        scale = min(1.0, self.max_side / max(height, width))
        return max(1, round(width * scale)), max(1, round(height * scale))

    def update(self, small: np.ndarray):
        """`small` ist das Arbeitsbild; Rückgabe: Punkte oder None."""
        gray = cv2.cvtColor(small, cv2.COLOR_RGB2GRAY)
        now = self.clock()
        if self._gray is not None and self._gray.shape != gray.shape:
            self.reset()
        if self._points is not None and self._gray is not None:
            self._points = face_morph.track_points(self._gray, gray, self._points)
            if self._points is None:
                self.since = None
        self._gray = gray
        if self._pending is not None and self._pending.done():
            try:
                points = self._pending.result()
            except Exception:
                points = None
            self._pending = None
            if points is None or now - self._submitted > 0.7:
                self._points = None
                self.since = None
            else:
                if self._submitted_gray is not None:
                    tracked = face_morph.track_points(self._submitted_gray, gray, points)
                    if tracked is not None:
                        points = tracked
                if self._points is not None:
                    jump = float(np.linalg.norm(points[20:22].mean(0) - self._points[20:22].mean(0)))
                    span = float(np.linalg.norm(points[20] - points[21]))
                    if jump < span * 0.6:
                        # Bewegung kommt aus dem optischen Fluss, die Erkennung korrigiert Drift.
                        points = self._points + (points - self._points) * self.DETECTION_GAIN
                    else:
                        self.since = None
                self._points = np.float32(points)
                self._seen = now
                if self.since is None:
                    self.since = now
        if self._pending is None and now - self._submitted >= self.INTERVAL:
            self._submitted = now
            self._submitted_gray = gray.copy()
            self._pending = face_morph.submit_face(small)
        if self._points is None or now - self._seen > self.MAX_AGE or self.since is None:
            return None
        return self._points


def face_crop(points: np.ndarray, shape, margin: float):
    """Ausschnitt um das Gesicht, `margin` in Gesichtsbreiten; None, wenn zu klein."""
    height, width = shape[:2]
    extra = max(8.0, float(np.ptp(points[:, 0])) * margin)
    low = np.floor(points.min(0) - extra).astype(int)
    high = np.ceil(points.max(0) + extra).astype(int)
    x0, y0 = max(0, low[0]), max(0, low[1])
    x1, y1 = min(width, high[0]), min(height, high[1])
    if x1 - x0 < 16 or y1 - y0 < 16:
        return None
    return x0, y0, x1, y1
