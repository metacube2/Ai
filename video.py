#!/usr/bin/env python3
"""
Bildeffekte in Echtzeit auf einen Bildschirmbereich.

Ein wählbarer Bereich des Bildschirms (z. B. ein Videoplayer-Fenster) wird
laufend abgefilmt, mit einem Effekt verändert und in der Vorschau angezeigt.

Abhängigkeiten: numpy, opencv-python(-headless), mss, pillow, sounddevice, scipy
macOS: Das Terminal braucht die Freigabe "Bildschirmaufnahme", für den
Audio+Video-Glitch zusätzlich "Mikrofon".
"""

import collections
import inspect
import queue
import threading
import time
from abc import ABC, abstractmethod
from typing import Any, Callable, Dict, List, Optional, Tuple, Type

import cv2
import mss
import numpy as np
import sounddevice as sd
import tkinter as tk
from PIL import Image, ImageTk
from scipy import signal

# Alle Bilder in diesem Programm sind RGB (uint8, Form H x B x 3).

# ----------------- Dependency Injection Container -----------------

class Container:
    """Einfacher Dependency Injection Container (ähnlich zu Autofac)"""
    _instances: Dict[Type, Any] = {}
    _registrations: Dict[Type, Tuple[Type, bool]] = {}

    @classmethod
    def register(cls, interface: Type, implementation: Type, singleton: bool = True) -> None:
        """Registriert eine Implementierung für ein Interface"""
        cls._registrations[interface] = (implementation, singleton)

    @classmethod
    def resolve(cls, interface: Type) -> Any:
        """Löst eine Abhängigkeit auf"""
        if interface not in cls._registrations:
            raise ValueError(f"Keine Registrierung für {interface.__name__} gefunden")

        implementation, singleton = cls._registrations[interface]
        if not singleton:
            return cls._create_instance(implementation)
        if interface not in cls._instances:
            cls._instances[interface] = cls._create_instance(implementation)
        return cls._instances[interface]

    @classmethod
    def _create_instance(cls, implementation: Type) -> Any:
        """Erstellt eine Instanz mit automatischer Dependency Injection"""
        constructor = inspect.signature(implementation.__init__)
        args = {}
        for param_name, param in list(constructor.parameters.items())[1:]:  # 'self' überspringen
            if param.annotation != inspect.Parameter.empty:
                args[param_name] = cls.resolve(param.annotation)
        return implementation(**args)

# ----------------- Interfaces -----------------

class IScreenCaptureService(ABC):
    """Interface für den Bildschirmaufnahme-Service"""

    @abstractmethod
    def capture(self, region: Dict[str, int]) -> np.ndarray:
        """Nimmt die Region auf und liefert ein RGB-Bild in genau Regionsgröße"""

    def close(self) -> None:
        """Gibt Ressourcen des aufrufenden Threads frei"""


class IEffectStrategy(ABC):
    """Interface für Bildeffekt-Strategien (Strategie-Pattern)"""

    @abstractmethod
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet einen Effekt auf ein RGB-Bild an"""

    @property
    @abstractmethod
    def name(self) -> str:
        """Name des Effekts"""

    @property
    def uses_cells(self) -> bool:
        """True, wenn der Effekt die Schaltfläche 'Nächste Zelle' nutzt"""
        return False

    def activate(self) -> None:
        """Wird aufgerufen, wenn der Effekt aktiv wird (z. B. Audio starten)"""

    def deactivate(self) -> None:
        """Wird aufgerufen, wenn der Effekt nicht mehr aktiv ist"""


class IEventBus(ABC):
    """Interface für den Event Bus"""

    @abstractmethod
    def subscribe(self, event_name: str, callback: Callable) -> None:
        """Abonniert ein Event"""

    @abstractmethod
    def publish(self, event_name: str, data: Any = None) -> None:
        """Veröffentlicht ein Event (aus beliebigem Thread)"""


class IAudioLoopService(ABC):
    """Interface für eine Audio-Schleife Eingang -> Verarbeitung -> Ausgang"""

    @abstractmethod
    def start(self, processor: Callable[[np.ndarray], np.ndarray]) -> None:
        """Startet die Schleife; processor bekommt Blöcke (N x Kanäle)"""

    @abstractmethod
    def stop(self) -> None:
        """Stoppt die Schleife"""

# ----------------- Hilfsfunktionen -----------------

def odd(value: float) -> int:
    """Nächste ungerade Zahl >= 1 (für Blur-Kernel)"""
    v = max(1, int(value))
    return v if v % 2 else v + 1


def rand_int(rng: np.random.Generator, low: int, high: int) -> int:
    """Wie rng.integers(low, high), liefert aber low statt eines Fehlers bei leerem Bereich"""
    return int(rng.integers(low, high)) if high > low else int(low)


def random_box(rng: np.random.Generator, width: int, height: int,
               min_size: int, max_size: int) -> Tuple[int, int, int, int]:
    """Zufälliges Rechteck (x, y, b, h), das auch bei kleinen Regionen ins Bild passt"""
    def span(total: int) -> Tuple[int, int]:
        lo = max(1, min(min_size, total))
        hi = max(lo, min(max_size, total))
        size = rand_int(rng, lo, hi + 1)
        return rand_int(rng, 0, total - size + 1), size

    x, bw = span(width)
    y, bh = span(height)
    return x, y, bw, bh


def shift_image(img: np.ndarray, dx: float, dy: float, border: int = cv2.BORDER_REPLICATE) -> np.ndarray:
    """Verschiebt ein Bild bzw. einen Kanal um (dx, dy) Pixel"""
    if dx == 0 and dy == 0:
        return img
    h, w = img.shape[:2]
    return cv2.warpAffine(img, np.float32([[1, 0, dx], [0, 1, dy]]), (w, h), borderMode=border)


def scale_image(img: np.ndarray, factor) -> np.ndarray:
    """Multipliziert Helligkeit mit Faktor (Skalar oder H x B), mit Sättigung auf 0..255"""
    if np.isscalar(factor):
        return cv2.convertScaleAbs(img, alpha=float(factor))
    result = img.astype(np.float32) * factor[..., None]
    return np.clip(result, 0, 255).astype(np.uint8)


def to_rgb_colormap(gray: np.ndarray, colormap: int) -> np.ndarray:
    return cv2.cvtColor(cv2.applyColorMap(gray, colormap), cv2.COLOR_BGR2RGB)


class PixelGrid:
    """Zwischengespeicherte Pixelkoordinaten für Remapping-Effekte"""

    def __init__(self):
        self._shape = None
        self.xs = self.ys = None

    def get(self, height: int, width: int) -> Tuple[np.ndarray, np.ndarray]:
        if self._shape != (height, width):
            self.xs, self.ys = np.meshgrid(np.arange(width, dtype=np.float32),
                                           np.arange(height, dtype=np.float32))
            self._shape = (height, width)
        return self.xs, self.ys

class NoisePool:
    """
    Vorberechnete Rauschtexturen. Pro Bild wird eine zufällig gewählt und
    zufällig verschoben — bei 30 fps nicht von frisch erzeugtem Rauschen zu
    unterscheiden, aber ein Vielfaches schneller.
    """

    def __init__(self, generator: Callable[[np.random.Generator, int, int], np.ndarray], count: int = 8):
        self._generator = generator
        self._count = count
        self._shape = None
        self._textures: List[np.ndarray] = []

    def get(self, rng: np.random.Generator, height: int, width: int) -> np.ndarray:
        if self._shape != (height, width):
            self._textures = [self._generator(rng, height, width) for _ in range(self._count)]
            self._shape = (height, width)
        texture = self._textures[int(rng.integers(0, self._count))]
        return np.roll(texture, (int(rng.integers(0, height)), int(rng.integers(0, width))), axis=(0, 1))

# ----------------- Implementierungen -----------------

class EventBus(IEventBus):
    """Thread-sicherer Event Bus. Callbacks laufen im Thread des Publishers."""

    def __init__(self):
        self._subscribers: Dict[str, List[Callable]] = {}
        self._lock = threading.Lock()

    def subscribe(self, event_name: str, callback: Callable) -> None:
        with self._lock:
            self._subscribers.setdefault(event_name, []).append(callback)

    def publish(self, event_name: str, data: Any = None) -> None:
        with self._lock:
            callbacks = list(self._subscribers.get(event_name, []))
        for callback in callbacks:
            callback(data)


class MssScreenCaptureService(IScreenCaptureService):
    """
    Bildschirmaufnahme mit MSS.
    mss-Objekte gehören zu ihrem Thread und werden pro Thread wiederverwendet.
    Auf Retina-Displays liefert mss doppelte Auflösung; das Bild wird auf die
    Regionsgröße zurückskaliert.
    """

    def __init__(self):
        self._local = threading.local()

    def capture(self, region: Dict[str, int]) -> np.ndarray:
        sct = getattr(self._local, 'sct', None)
        if sct is None:
            sct = self._local.sct = mss.mss()
        frame = cv2.cvtColor(np.asarray(sct.grab(region)), cv2.COLOR_BGRA2RGB)
        width, height = region['width'], region['height']
        if frame.shape[:2] != (height, width):
            frame = cv2.resize(frame, (width, height), interpolation=cv2.INTER_AREA)
        return frame

    def close(self) -> None:
        sct = getattr(self._local, 'sct', None)
        if sct is not None:
            sct.close()
            self._local.sct = None


class SoundDeviceLoopService(IAudioLoopService):
    """Mikrofon -> Verarbeitung -> Lautsprecher in einem Duplex-Stream"""

    SAMPLE_RATE = 44100
    BLOCK_SIZE = 1024

    def __init__(self, event_bus: IEventBus):
        self.event_bus = event_bus
        self._stream: Optional[sd.Stream] = None
        self.channels = 1

    def start(self, processor: Callable[[np.ndarray], np.ndarray]) -> None:
        if self._stream is not None:
            return

        def callback(indata, outdata, frames, time_info, status):
            try:
                outdata[:] = np.clip(processor(indata.copy()), -1.0, 1.0)
            except Exception:
                outdata.fill(0)

        try:
            inp = sd.query_devices(kind='input')
            out = sd.query_devices(kind='output')
            self.channels = max(1, min(2, int(inp['max_input_channels']), int(out['max_output_channels'])))
            self._stream = sd.Stream(samplerate=self.SAMPLE_RATE, blocksize=self.BLOCK_SIZE,
                                     channels=self.channels, dtype='float32', callback=callback)
            self._stream.start()
            self.event_bus.publish('status_update',
                                   "Audio: Mikrofon -> Lautsprecher. Kopfhörer benutzen, sonst Rückkopplung!")
        except Exception as e:
            self._stream = None
            self.event_bus.publish('status_update', f"Audio-Fehler: {e}")

    def stop(self) -> None:
        stream, self._stream = self._stream, None
        if stream is not None:
            try:
                stream.stop()
                stream.close()
            except Exception:
                pass

# ----------------- Einfache Effekte -----------------

class MosaicEffectStrategy(IEffectStrategy):
    """3x3-Mosaik: Nur die aktive Zelle zeigt das Live-Bild, die anderen bleiben stehen"""

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

    @property
    def name(self) -> str:
        return "Blur"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        size = odd(config.get('blur_amount', 15))
        return cv2.GaussianBlur(frame, (size, size), 0)


class EdgeDetectionEffectStrategy(IEffectStrategy):
    """Kantenerkennung (Canny)"""

    @property
    def name(self) -> str:
        return "Kantenerkennung"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        gray = cv2.cvtColor(frame, cv2.COLOR_RGB2GRAY)
        edges = cv2.Canny(gray, 100, 200)
        return cv2.cvtColor(edges, cv2.COLOR_GRAY2RGB)


class SlowMotionSnapshotEffectStrategy(IEffectStrategy):
    """Zeigt nur alle 0,2 Sekunden ein neues Standbild"""

    UPDATE_INTERVAL = 0.2
    MARKER_DURATION = 0.1

    def __init__(self):
        self.last_update_time = 0.0
        self.current_frame: Optional[np.ndarray] = None
        self.marked_frame: Optional[np.ndarray] = None

    @property
    def name(self) -> str:
        return "Slow Motion Snapshot"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        now = time.time()
        if (self.current_frame is None or self.current_frame.shape != frame.shape
                or now - self.last_update_time >= self.UPDATE_INTERVAL):
            self.current_frame = frame.copy()
            self.marked_frame = frame.copy()
            # Roter Punkt zeigt kurz an, dass ein neues Bild übernommen wurde
            cv2.circle(self.marked_frame, (20, 20), 10, (255, 0, 0), -1)
            self.last_update_time = now
        if now - self.last_update_time < self.MARKER_DURATION:
            return self.marked_frame
        return self.current_frame

# ----------------- Realtime Glitch -----------------

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

# ----------------- VHS -----------------

class VHSGlitchEffectStrategy(IEffectStrategy):
    """Klassische VHS-Störungen: Tracking-Fehler, Scan-Lines, Farbverzerrungen"""

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
        return result

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

# ----------------- Alter Film -----------------

class OldFilmEffectStrategy(IEffectStrategy):
    """Filmlook der 1910er: Sepia, Korn, Flackern, Kratzer, Staub, Bildsprünge"""

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
        return result

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

        exposure = (rows[:, None] * cols[None, :] * self.flicker_value * vignette).astype(np.float32)
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

# ----------------- Glitch-Dimension -----------------

class GlitchDimensionEffectStrategy(IEffectStrategy):
    """
    Extremer Glitch: Die Realität kollabiert zwischen Dimensionen, mit
    Farbverschiebungen, räumlichen Verzerrungen und Datenkorruption.
    Achtung: starkes Flackern.
    """

    def __init__(self):
        self.rng = np.random.default_rng()
        self.grid = PixelGrid()
        self.frame_counter = 0
        self.last_frames: collections.deque = collections.deque(maxlen=8)
        self.dimension_shift_time = time.time()
        self.reality_stability = 1.0
        self._stability_offset = 0.4       # Start ruhig, danach Pendeln um 0.6
        self.current_dimension = 0
        self.transition = 0.0              # Überblendung nach Dimensionswechsel (1 -> 0)
        self.last_output: Optional[np.ndarray] = None
        self.last_reality_breach = 0.0
        self.breach_coordinates: List[Tuple[int, int]] = []
        self.rgb_shift_values = [0, 0, 0]
        self.glitch_blocks = [self._new_glitch_block() for _ in range(15)]
        self.dimension_effects = [
            self._corrupt_reality,        # 0: Realitätskorruption
            self._digital_decay,          # 1: Digitaler Zerfall
            self._quantum_uncertainty,    # 2: Quantenunsicherheit
            self._void_intrusion,         # 3: Eindringen der Leere
            self._hyperbolic_distortion,  # 4: Hyperbolische Verzerrung
        ]

    @property
    def name(self) -> str:
        return "Glitch-Dimension"

    def _new_glitch_block(self) -> Dict[str, Any]:
        rng = self.rng
        return {
            'x': rng.uniform(0, 1), 'y': rng.uniform(0, 1),
            'width': rng.uniform(0.05, 0.3), 'height': rng.uniform(0.05, 0.3),
            'corruption': rng.uniform(0.3, 1.0),
            'lifetime': int(rng.integers(5, 30)), 'current_age': 0,
            'shift_values': [int(v) for v in rng.integers(-30, 31, 3)],
            'direction': int(rng.integers(0, 4)),   # 0: rechts, 1: links, 2: oben, 3: unten
            'type': int(rng.integers(0, 5)),
        }

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        now = time.time()
        rng = self.rng
        self.frame_counter += 1

        if self.last_frames and self.last_frames[-1].shape != frame.shape:
            self.last_frames.clear()
            self.last_output = None
        if len(self.last_frames) < 8 or self.frame_counter % 2 == 0:
            self.last_frames.append(frame.copy())

        self._update_reality_stability()

        result = self._apply_rgb_channel_shift(frame)
        result = self._apply_glitch_blocks(result)
        result = self._apply_high_frequency_noise(result)
        result = self._apply_dimension_jumps(result)
        result = self._apply_temporal_anomalies(result)
        result = self.dimension_effects[self.current_dimension](result)

        if rng.random() < 0.01 * (2.0 - self.reality_stability) and now - self.last_reality_breach > 3.0:
            result = self._apply_reality_breach(result)
            self.last_reality_breach = now

        # Sicherheitsmechanismus gegen zu extreme Bilder
        if rng.random() < 0.05:
            intensity = rng.uniform(0.3, 0.7)
            result = cv2.addWeighted(result, intensity, frame, 1 - intensity, 0)

        # Weicher Übergang nach einem Dimensionswechsel
        if self.transition > 0 and self.last_output is not None:
            result = cv2.addWeighted(result, 1 - self.transition, self.last_output, self.transition, 0)
            self.transition = max(0.0, self.transition - 0.1)

        self.last_output = result
        self._update_glitch_parameters(now)
        return result

    def _update_reality_stability(self) -> None:
        """Stabilität pendelt langsam um 0.6, mit Rauschen und seltenen Sprüngen"""
        rng = self.rng
        if rng.random() < 0.005:
            self._stability_offset += rng.uniform(-0.4, 0.4)
        self._stability_offset = 0.97 * self._stability_offset + rng.normal(0, 0.03)
        self.reality_stability = float(np.clip(
            0.6 + 0.3 * np.sin(self.frame_counter * 0.01) + self._stability_offset, 0.2, 1.0))

    def _update_glitch_parameters(self, now: float) -> None:
        rng = self.rng
        instability = 1.0 - self.reality_stability * 0.8
        for i in range(3):
            limit = int(30 * instability)
            if limit > 0 and rng.random() < instability * 0.3:
                self.rgb_shift_values[i] = int(rng.integers(-limit, limit))
            else:
                self.rgb_shift_values[i] = int(self.rgb_shift_values[i] * 0.85)

        for block in self.glitch_blocks:
            block['current_age'] += 1
            if block['current_age'] >= block['lifetime']:
                direction = block['direction']
                block.update(self._new_glitch_block())
                block['direction'] = direction
            speed = 0.01 * (1.0 - self.reality_stability) * block['corruption']
            if block['direction'] == 0:
                block['x'] = min(1.1, block['x'] + speed)
            elif block['direction'] == 1:
                block['x'] = max(-0.1, block['x'] - speed)
            elif block['direction'] == 2:
                block['y'] = max(-0.1, block['y'] - speed)
            else:
                block['y'] = min(1.1, block['y'] + speed)

        if now - self.dimension_shift_time > 5.0 and rng.random() < 0.02:
            new_dimension = int(rng.integers(0, len(self.dimension_effects)))
            if new_dimension != self.current_dimension:
                self.current_dimension = new_dimension
                self.transition = 1.0
            self.dimension_shift_time = now

    # --- Basiseffekte ---

    def _apply_rgb_channel_shift(self, frame: np.ndarray) -> np.ndarray:
        """Chromatische Aberration: Kanäle horizontal gegeneinander verschoben"""
        if not any(self.rgb_shift_values):
            return frame
        channels = cv2.split(frame)
        return cv2.merge([shift_image(c, -s, 0) for c, s in zip(channels, self.rgb_shift_values)])

    def _apply_glitch_blocks(self, frame: np.ndarray) -> np.ndarray:
        """Korrupte, verschobene und verzerrte Blöcke"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()

        for block in self.glitch_blocks:
            if rng.random() > block['corruption'] * (1.0 - self.reality_stability):
                continue
            x1 = int(np.clip(width * block['x'], 0, width - 1))
            y1 = int(np.clip(height * block['y'], 0, height - 1))
            x2 = int(np.clip(x1 + width * block['width'], 0, width - 1))
            y2 = int(np.clip(y1 + height * block['height'], 0, height - 1))
            if x2 <= x1 or y2 <= y1:
                continue

            region = frame[y1:y2, x1:x2].copy()
            bh, bw = region.shape[:2]
            kind = block['type']

            if kind == 0:
                region = cv2.merge([shift_image(c, s, 0, cv2.BORDER_CONSTANT)
                                    for c, s in zip(cv2.split(region), block['shift_values'])])
            elif kind == 1:
                scale = max(1, int(10 * block['corruption']))
                small = cv2.resize(region, (max(1, bw // scale), max(1, bh // scale)))
                region = cv2.resize(small, (bw, bh), interpolation=cv2.INTER_NEAREST)
            elif kind == 2:
                shifts = (np.sin(np.arange(bh) / 5) * 10 * block['corruption']).astype(int)
                cols = (np.arange(bw)[None, :] - shifts[:, None]) % bw
                region = region[np.arange(bh)[:, None], cols]
            elif kind == 3:
                mask = rng.random((bh, bw)) < 0.2 * block['corruption']
                region[mask] = rng.integers(0, 256, (int(mask.sum()), 3), dtype=np.uint8)
            else:
                region = 255 - region
                over = region > int(128 * block['corruption'])
                region[over] = 255 - region[over]

            result[y1:y2, x1:x2] = region
        return result

    def _apply_high_frequency_noise(self, frame: np.ndarray) -> np.ndarray:
        """Scanlines, Sync-Störungen, CRT-Rauschen, Farbflecken, Störlinien"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        noise_level = 20 * (1.0 - self.reality_stability)

        if rng.random() < 0.7:
            darkness = rng.uniform(0.7, 0.9, size=(len(range(0, height, 2)), 1, 1)).astype(np.float32)
            result[::2] = (result[::2] * darkness).astype(np.uint8)

        if rng.random() < 0.3:
            for _ in range(int(rng.integers(1, 5))):
                y0 = int(rng.integers(0, height))
                y1 = min(height, y0 + int(rng.integers(1, 10)))
                shift = rand_int(rng, -width // 5, width // 5)
                if shift > 0:
                    result[y0:y1, shift:] = result[y0:y1, :-shift].copy()
                    result[y0:y1, :shift] = 0
                elif shift < 0:
                    result[y0:y1, :shift] = result[y0:y1, -shift:].copy()
                    result[y0:y1, shift:] = 0

        if noise_level >= 1 and rng.random() < 0.5:
            noise = rng.standard_normal((height, width, 3), dtype=np.float32) * noise_level
            result = np.clip(result.astype(np.float32) + noise, 0, 255).astype(np.uint8)

        if rng.random() < 0.2:
            for _ in range(int(rng.integers(10, 50))):
                color = tuple(int(c) for c in rng.integers(0, 256, 3))
                center = (int(rng.integers(0, width)), int(rng.integers(0, height)))
                cv2.circle(result, center, int(rng.integers(1, 5)), color, -1)

        if rng.random() < 0.15 * (1.0 - self.reality_stability):
            for _ in range(int(rng.integers(1, 10))):
                y = int(rng.integers(0, height))
                y1 = min(height, y + int(rng.integers(1, 5)))
                if rng.random() < 0.5:
                    result[y:y1] = rng.integers(0, 256, (y1 - y, width, 3), dtype=np.uint8)
                else:
                    result[y:y1] = 0 if rng.random() < 0.5 else 255
        return result

    def _apply_dimension_jumps(self, frame: np.ndarray) -> np.ndarray:
        """Wirbel mit Farbsprung und 'Portale' in frühere Frames"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame

        if rng.random() < 0.02 * (1.0 - self.reality_stability):
            cx = width // 2 + rand_int(rng, -width // 4, width // 4)
            cy = height // 2 + rand_int(rng, -height // 4, height // 4)
            matrix = cv2.getRotationMatrix2D((cx, cy), rng.uniform(0, 360), rng.uniform(0.7, 1.3))
            matrix[0, 0] *= rng.uniform(0.8, 1.2)
            matrix[1, 1] *= rng.uniform(0.8, 1.2)
            result = cv2.warpAffine(result, matrix, (width, height))

            hsv = cv2.cvtColor(result, cv2.COLOR_RGB2HSV)
            hsv[:, :, 0] = ((hsv[:, :, 0].astype(np.int32) + int(rng.integers(0, 180))) % 180).astype(np.uint8)
            shifted = cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)
            mask = np.zeros((height, width), dtype=np.uint8)
            cv2.circle(mask, (cx, cy), int(min(width, height) * rng.uniform(0.1, 0.3)), 255, -1)
            result = np.where(mask[..., None] > 0, shifted, result)

        if rng.random() < 0.03 * (1.0 - self.reality_stability) and self.last_frames:
            result = result.copy()
            for _ in range(int(rng.integers(1, 4))):
                x, y, pw, ph = random_box(rng, width, height, 30, 200)
                source = self.last_frames[int(rng.integers(0, len(self.last_frames)))]
                portal = source[y:y + ph, x:x + pw]
                if rng.random() < 0.5:
                    portal = 255 - portal
                py, px = np.ogrid[:ph, :pw]
                dist = np.hypot(px - pw / 2, py - ph / 2) / max(1e-6, np.hypot(pw / 2, ph / 2))
                mask = cv2.GaussianBlur(np.clip(1 - dist, 0, 1).astype(np.float32), (7, 7), 0)[..., None]
                target = result[y:y + ph, x:x + pw]
                result[y:y + ph, x:x + pw] = (portal * mask + target * (1 - mask)).astype(np.uint8)
                color = tuple(int(c) for c in rng.integers(0, 256, 3))
                cv2.rectangle(result, (x, y), (x + pw, y + ph), color, 2)
        return result

    def _apply_temporal_anomalies(self, frame: np.ndarray) -> np.ndarray:
        """Bereiche zeigen frühere Frames: Streifen, Ringe, Blobs oder Wellen"""
        rng = self.rng
        if len(self.last_frames) < 3 or rng.random() >= 0.1 * (1.0 - self.reality_stability):
            return frame

        height, width = frame.shape[:2]
        past = self.last_frames[int(rng.integers(0, len(self.last_frames)))]
        mask = np.zeros((height, width), dtype=np.float32)
        kind = int(rng.integers(0, 4))

        if kind == 0:
            vertical = rng.random() < 0.5
            count = int(rng.integers(3, 10))
            size = max(1, (width if vertical else height) // count)
            for i in range(count):
                if rng.random() < 0.4:
                    if vertical:
                        mask[:, i * size:(i + 1) * size] = 1.0
                    else:
                        mask[i * size:(i + 1) * size, :] = 1.0
        elif kind == 1:
            xs, ys = self.grid.get(height, width)
            dist = np.hypot(xs - width // 2, ys - height // 2)
            max_radius = np.hypot(width // 2, height // 2)
            count = int(rng.integers(3, 8))
            for i in range(count):
                if rng.random() < 0.5:
                    mask[(dist >= max_radius * i / count) & (dist < max_radius * (i + 1) / count)] = 1.0
        elif kind == 2:
            for _ in range(int(rng.integers(2, 6))):
                center = (int(rng.integers(0, width)), int(rng.integers(0, height)))
                cv2.circle(mask, center, int(rng.integers(20, 100)), 1.0, -1)
        else:
            wave = (np.sin(np.arange(width, dtype=np.float32) * rng.uniform(1, 5) * np.pi / width
                           + rng.uniform(0, 2 * np.pi))[None, :]
                    * np.cos(np.arange(height, dtype=np.float32) * rng.uniform(1, 5) * np.pi / height
                             + rng.uniform(0, 2 * np.pi))[:, None])
            mask = ((wave + 1) / 2 > 0.6).astype(np.float32)

        mask = cv2.GaussianBlur(mask, (15, 15), 0)[..., None]
        return (past * mask + frame * (1 - mask)).astype(np.uint8)

    # --- Dimensionen ---

    def _corrupt_reality(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 0: verschobene Streifen, Kanalversatz, Pixelblöcke, Rauschen"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        intensity = rng.uniform(0.2, 0.8)

        for _ in range(int(rng.integers(3, 12))):
            band = rand_int(rng, 10, height // 4)
            y = rand_int(rng, 0, height - band)
            shift = rand_int(rng, 5, width // 3) * (1 if rng.random() > 0.5 else -1)
            result[y:y + band] = np.roll(result[y:y + band], shift, axis=1)

        if rng.random() < 0.7:
            r, g, b = cv2.split(result)
            r = np.roll(r, int(rng.integers(5, 20)), axis=1)
            g = np.roll(g, int(rng.integers(5, 20)), axis=0)
            result = cv2.merge([r, g, b])

        for _ in range(int(rng.integers(2, 6))):
            x, y, bw, bh = random_box(rng, width, height, 50, 150)
            pixel = int(rng.integers(5, 20))
            small = cv2.resize(result[y:y + bh, x:x + bw], (max(1, bw // pixel), max(1, bh // pixel)))
            result[y:y + bh, x:x + bw] = cv2.resize(small, (bw, bh), interpolation=cv2.INTER_NEAREST)

        if rng.random() < 0.5:
            for _ in range(int(rng.integers(10, 50))):
                y = int(rng.integers(0, height))
                result[y] = np.roll(result[y], rand_int(rng, 10, width // 2), axis=0)

        noise = rng.standard_normal(result.shape, dtype=np.float32) * (25 * intensity)
        return np.clip(result.astype(np.float32) + noise, 0, 255).astype(np.uint8)

    def _digital_decay(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 1: Kompressionsartefakte, Datenverlust, Bitfehler, Interlacing"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        decay = rng.uniform(0.3, 0.7)

        if rng.random() < 0.8:
            for _ in range(int(10 * decay)):
                x, y, size, _ = random_box(rng, width, height, 15, 60)
                size = min(size, height - y)
                if size < 2:
                    continue
                block = result[y:y + size, x:x + size]
                kind = rng.random()
                if kind < 0.25:
                    small = cv2.resize(block, (max(1, size // 8), max(1, size // 8)))
                    block = cv2.resize(small, (size, size), interpolation=cv2.INTER_NEAREST)
                elif kind < 0.5:
                    factor = int(rng.integers(3, 8))
                    block = (block // factor) * factor
                elif kind < 0.75:
                    gray = cv2.cvtColor(cv2.cvtColor(block, cv2.COLOR_RGB2GRAY), cv2.COLOR_GRAY2RGB)
                    factor = int(rng.integers(2, 6))
                    block = (gray // factor) * factor
                else:
                    block = shift_image(block, int(rng.integers(-10, 11)), int(rng.integers(-10, 11)),
                                        cv2.BORDER_CONSTANT)
                result[y:y + size, x:x + size] = block

        if rng.random() < 0.7:
            for _ in range(int(20 * decay)):
                y = int(rng.integers(0, height))
                length = rand_int(rng, max(1, width // 5), width)
                x = rand_int(rng, 0, width - length)
                kind = rng.random()
                if kind < 0.3:
                    result[y, x:x + length] = 0 if rng.random() < 0.6 else 255
                elif kind < 0.6:
                    result[y, x:x + length] = rng.integers(0, 256, (length, 3), dtype=np.uint8)
                else:
                    result[y, x:x + length] = result[y, x:x + length][rng.permutation(length)]

        if rng.random() < 0.5:
            count = int(100 * decay)
            ys = rng.integers(0, height, count)
            xs = rng.integers(0, width, count)
            channels = rng.integers(0, 3, count)
            result[ys, xs, channels] ^= (1 << rng.integers(0, 8, count)).astype(np.uint8)

        if rng.random() < 0.6 and height >= 8 and width >= 8:
            checker = ((np.indices((8, 8)).sum(axis=0) % 2) * 255).astype(np.uint8)
            gradient = np.repeat((np.arange(8) * 255 // 7).astype(np.uint8)[:, None], 8, axis=1)
            for _ in range(int(15 * decay)):
                x = int(rng.integers(0, width - 7))
                y = int(rng.integers(0, height - 7))
                kind = rng.random()
                if kind < 0.33:
                    pattern = checker
                elif kind < 0.66:
                    pattern = gradient
                else:
                    pattern = np.full((8, 8), int(rng.integers(0, 256)), dtype=np.uint8)
                result[y:y + 8, x:x + 8] = pattern[..., None]

        if rng.random() < 0.4:
            shift = rand_int(rng, 5, max(6, width // 3)) * (1 if rng.random() > 0.5 else -1)
            result[::2] = np.roll(result[::2], shift, axis=1)
        return result

    def _quantum_uncertainty(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 2: Unschärfewolken, Quantensprünge, Interferenz, Tunneln, Überlagerung"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        uncertainty = rng.uniform(0.2, 0.8)

        if rng.random() < 0.7:
            for _ in range(int(rng.integers(3, 8))):
                x, y, bw, bh = random_box(rng, width, height, 50, 200)
                strength = max(3, int(15 * (1.0 - (bw * bh) / (width * height)) * uncertainty))
                region = result[y:y + bh, x:x + bw]
                blurred = cv2.GaussianBlur(region, (strength * 2 + 1, strength * 2 + 1), 0)
                alpha = rng.uniform(0.4, 0.9)
                result[y:y + bh, x:x + bw] = cv2.addWeighted(region, 1 - alpha, blurred, alpha, 0)

        if rng.random() < 0.6:
            for _ in range(int(20 * uncertainty)):
                sx, sy, bw, bh = random_box(rng, width, height, 5, 20)
                dx = rand_int(rng, 0, width - bw + 1)
                dy = rand_int(rng, 0, height - bh + 1)
                source = result[sy:sy + bh, sx:sx + bw].copy()
                target = result[dy:dy + bh, dx:dx + bw]
                keep = rng.random((bh, bw, 1)) < 0.7
                result[dy:dy + bh, dx:dx + bw] = np.where(keep, source, target)

        if rng.random() < 0.4:
            scale = rng.uniform(10, 30)
            x = np.arange(width, dtype=np.float32)
            y = np.arange(height, dtype=np.float32)
            interference = (np.sin(x / scale + rng.random() * 10)[None, :]
                            + np.sin(y / scale + rng.random() * 10)[:, None]
                            + np.cos((x[None, :] + y[:, None]) / scale + rng.random() * 10)) / 3.0
            result = scale_image(result, (1.0 + interference * 0.15 * uncertainty).astype(np.float32))

        if rng.random() < 0.5:
            count = int(100 * uncertainty)
            distance = max(1, int(min(width, height) * 0.3 * uncertainty))
            sx = rng.integers(0, width, count)
            sy = rng.integers(0, height, count)
            tx = np.clip(sx + rng.integers(-distance, distance + 1, count), 0, width - 1)
            ty = np.clip(sy + rng.integers(-distance, distance + 1, count), 0, height - 1)
            result[ty, tx] = result[sy, sx]

        if rng.random() < 0.3:
            for _ in range(int(rng.integers(1, 4))):
                x, y, bw, bh = random_box(rng, width, height, 50, 200)
                region = result[y:y + bh, x:x + bw]
                xs = np.arange(bw, dtype=np.float32)
                ys = np.arange(bh, dtype=np.float32)
                mask = np.zeros((bh, bw), dtype=np.float32)
                for _ in range(10):
                    freq = rng.uniform(0.01, 0.1)
                    phase = rng.uniform(0, 2 * np.pi)
                    mask += 0.1 * np.sin(freq * xs + phase)[None, :] * np.cos(freq * ys + phase)[:, None]
                span = mask.max() - mask.min()
                mask = ((mask - mask.min()) / span if span > 0 else np.zeros_like(mask))[..., None]
                result[y:y + bh, x:x + bw] = (region * (1 - mask) + (255 - region) * mask).astype(np.uint8)
        return result

    def _void_intrusion(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 3: Die Leere saugt das Bild an, mit dunklen Flecken und Adern"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        void_intensity = rng.uniform(0.4, 0.9)

        if rng.random() < 0.8:
            cx = rand_int(rng, width // 4, width * 3 // 4)
            cy = rand_int(rng, height // 4, height * 3 // 4)
            max_radius = max(4, int(min(width, height) * 0.4 * void_intensity))

            mask = np.zeros((height, width), dtype=np.float32)
            for _ in range(int(rng.integers(3, 8))):
                center = (cx + rand_int(rng, -max_radius // 2, max_radius // 2),
                          cy + rand_int(rng, -max_radius // 2, max_radius // 2))
                cv2.circle(mask, center, rand_int(rng, max_radius // 3, max_radius), 1.0, -1)
            mask = cv2.GaussianBlur(mask, (21, 21), max_radius / 5)
            if mask.max() > 0:
                mask /= mask.max()

            # Sog zum Zentrum
            xs, ys = self.grid.get(height, width)
            dx, dy = xs - cx, ys - cy
            dist = np.hypot(dx, dy)
            pull = np.where(dist < max_radius * 2, 5.0 * mask * (1.0 - dist / (max_radius * 2)), 0.0)
            safe = np.maximum(dist, 1e-6)
            result = cv2.remap(result, xs - dx / safe * pull, ys - dy / safe * pull,
                               cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)

            weight = (mask * void_intensity)[..., None]
            void_color = np.array([5, 0, 10], dtype=np.float32)
            result = (result * (1.0 - weight) + void_color * weight).astype(np.uint8)

        for _ in range(int(rng.integers(2, 8))):
            result = self._void_spot(result)

        if rng.random() < 0.4:
            for _ in range(int(rng.integers(3, 10))):
                center = (int(rng.integers(0, width)), int(rng.integers(0, height)))
                radius = int(rng.integers(2, 8))
                if rng.random() < 0.5:
                    cv2.circle(result, center, radius, (0, 0, 0), -1)
                else:
                    axes = (radius, int(radius * rng.uniform(0.5, 2.0)))
                    cv2.ellipse(result, center, axes, int(rng.integers(0, 180)), 0, 360, (0, 0, 0), -1)

        if rng.random() < 0.6:
            budget = [300]   # begrenzt die Zahl der Aderlinien
            for _ in range(int(rng.integers(2, 6))):
                self._draw_void_veins(result, int(rng.integers(0, width)), int(rng.integers(0, height)),
                                      0, int(rng.integers(3, 7)), budget)

        if rng.random() < 0.7:
            hsv = cv2.cvtColor(result, cv2.COLOR_RGB2HSV)
            dark = cv2.GaussianBlur((cv2.cvtColor(result, cv2.COLOR_RGB2GRAY) < 30).astype(np.float32), (21, 21), 0)
            hsv[:, :, 1] = (hsv[:, :, 1] * (1.0 - dark * 0.8)).astype(np.uint8)
            result = cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)
        return result

    def _void_spot(self, image: np.ndarray) -> np.ndarray:
        """Ein unregelmäßiger dunkler oder invertierter Fleck (nur im betroffenen Ausschnitt gerechnet)"""
        rng = self.rng
        height, width = image.shape[:2]
        sx, sy = int(rng.integers(0, width)), int(rng.integers(0, height))
        radius = int(rng.integers(5, 30))
        pad = radius * 2 + 16
        x0, x1 = max(0, sx - pad), min(width, sx + pad)
        y0, y1 = max(0, sy - pad), min(height, sy + pad)

        mask = np.zeros((y1 - y0, x1 - x0), dtype=np.float32)
        cv2.circle(mask, (sx - x0, sy - y0), radius, 1.0, -1)
        for angle in np.radians(np.arange(0, 360, 20)):
            r = radius * (0.7 + 0.6 * rng.random())
            px = int(np.clip(sx + r * np.cos(angle), 0, width - 1)) - x0
            py = int(np.clip(sy + r * np.sin(angle), 0, height - 1)) - y0
            cv2.circle(mask, (px, py), int(radius * 0.4), 1.0, -1)
        mask = cv2.GaussianBlur(mask, (15, 15), radius / 3)
        if mask.max() <= 0:
            return image
        mask = (mask / mask.max())[..., None]

        region = image[y0:y1, x0:x1].astype(np.float32)
        if rng.random() < 0.7:
            color = np.array([int(rng.integers(0, 15)), 0, int(rng.integers(0, 20))], dtype=np.float32)
            blended = region * (1.0 - mask * 0.9) + color * mask * 0.9
        else:
            blended = region * (1.0 - mask * 0.7) + (255 - region) * mask * 0.7
        image[y0:y1, x0:x1] = blended.astype(np.uint8)
        return image

    def _draw_void_veins(self, image: np.ndarray, x: int, y: int, depth: int, max_depth: int,
                         budget: List[int]) -> None:
        """Rekursiv verästelte schwarze Adern"""
        height, width = image.shape[:2]
        if depth >= max_depth or budget[0] <= 0 or not (0 <= x < width and 0 <= y < height):
            return
        budget[0] -= 1
        rng = self.rng
        length = int(rng.integers(10, 40))
        angle = rng.uniform(0, 2 * np.pi)
        end_x = int(np.clip(x + length * np.cos(angle), 0, width - 1))
        end_y = int(np.clip(y + length * np.sin(angle), 0, height - 1))
        cv2.line(image, (x, y), (end_x, end_y), (0, 0, 0), max(1, 3 - depth))
        for _ in range(int(rng.integers(1, 4))):
            t = rng.uniform(0.3, 1.0)
            self._draw_void_veins(image, int(x + t * (end_x - x)), int(y + t * (end_y - y)),
                                  depth + 1, max_depth, budget)

    def _hyperbolic_distortion(self, frame: np.ndarray) -> np.ndarray:
        """Dimension 4: gekrümmter Raum, Perspektivebenen, Spiegelungen, Streifen, Farbfaltung"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        intensity = rng.uniform(0.3, 0.9)

        # 1. Hyperbolische Koordinatenverzerrung
        if rng.random() < 0.8:
            cx = width // 2 + rand_int(rng, -width // 4, width // 4)
            cy = height // 2 + rand_int(rng, -height // 4, height // 4)
            k1 = rng.uniform(-0.3, 0.3) * intensity
            k2 = rng.uniform(-0.2, 0.2) * intensity
            rotation = rng.uniform(0, 2 * np.pi)
            scale = rng.uniform(0.7, 1.3)

            xs, ys = self.grid.get(height, width)
            nx = (xs - cx) / width
            ny = (ys - cy) / height
            r = np.hypot(nx, ny)
            theta = np.arctan2(ny, nx) + rotation
            rd = r + k1 * np.tanh(r * 4) + k2 * (1 / (r + 0.1) - 1)
            map_x = np.where(r > 0, cx + rd * np.cos(theta) * width * scale, xs).astype(np.float32)
            map_y = np.where(r > 0, cy + rd * np.sin(theta) * height * scale, ys).astype(np.float32)
            result = cv2.remap(result, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)

        # 2. Bildteile in verschiedenen Raumebenen
        if rng.random() < 0.5:
            for _ in range(int(rng.integers(2, 5))):
                x, y, rw, rh = random_box(rng, width, height, max(2, width // 4), max(2, width // 2))
                rh = min(rh, max(2, height // 2))
                y = min(y, height - rh)
                factor = int(min(rw, rh) * 0.3 * intensity)
                if factor < 1 or rw < 2 or rh < 2:
                    continue
                src = np.float32([[0, 0], [rw - 1, 0], [rw - 1, rh - 1], [0, rh - 1]])
                dst = (src + rng.integers(-factor, factor, (4, 2))).astype(np.float32)
                matrix = cv2.getPerspectiveTransform(src, dst)
                result[y:y + rh, x:x + rw] = cv2.warpPerspective(result[y:y + rh, x:x + rw], matrix, (rw, rh))

        # 3. Möbius-artige Spiegelungen mit Farbtonumkehr
        if rng.random() < 0.3:
            for _ in range(int(rng.integers(1, 4))):
                x, y, bw, bh = random_box(rng, width, height, 50, 200)
                region = result[y:y + bh, x:x + bw]
                kind = int(rng.integers(0, 3))
                region = cv2.flip(region, 1) if kind == 0 else cv2.flip(region, 0) if kind == 1 \
                    else cv2.rotate(region, cv2.ROTATE_180)
                if rng.random() < 0.5:
                    hsv = cv2.cvtColor(region, cv2.COLOR_RGB2HSV)
                    hsv[:, :, 0] = ((hsv[:, :, 0].astype(np.int32) + 90) % 180).astype(np.uint8)
                    region = cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)
                result[y:y + bh, x:x + bw] = region

        # 4. Streifenweise nicht-euklidische Manipulationen
        if rng.random() < 0.4:
            vertical = rng.random() < 0.5
            # Vertikale Streifen = horizontale Streifen im transponierten Bild
            work = np.ascontiguousarray(result.transpose(1, 0, 2)) if vertical else result
            self._manipulate_stripes(work, int(rng.integers(4, 12)))
            result = np.ascontiguousarray(work.transpose(1, 0, 2)) if vertical else work

        # 5. Farbige Raumfaltung
        if rng.random() < 0.6:
            xs, ys = self.grid.get(height, width)
            kind = int(rng.integers(0, 3))
            if kind == 0:
                cx = width // 2 + rand_int(rng, -width // 4, width // 4)
                cy = height // 2 + rand_int(rng, -height // 4, height // 4)
                max_radius = np.hypot(width, height) / 2
                gradient = 0.5 + 0.5 * np.sin(np.hypot(xs - cx, ys - cy) / max_radius * np.pi * rng.integers(2, 5))
            elif kind == 1:
                angle = rng.uniform(0, np.pi)
                frequency = rng.uniform(5, 15) * np.pi / max(width, height)
                gradient = 0.5 + 0.5 * np.sin((xs * np.cos(angle) + ys * np.sin(angle)) * frequency)
            else:
                dx, dy = xs - width // 2, ys - height // 2
                gradient = 0.5 + 0.5 * np.sin(np.arctan2(dy, dx) * rng.integers(3, 8) + np.hypot(dx, dy) / 20)
            gradient = cv2.GaussianBlur(gradient.astype(np.float32), (15, 15), 0)
            colormap = [cv2.COLORMAP_JET, cv2.COLORMAP_VIRIDIS, cv2.COLORMAP_PLASMA][int(rng.integers(0, 3))]
            heatmap = to_rgb_colormap((gradient * 255).astype(np.uint8), colormap)
            alpha = rng.uniform(0.3, 0.7) * intensity
            result = cv2.addWeighted(result, 1 - alpha, heatmap, alpha, 0)
        return result

    def _manipulate_stripes(self, image: np.ndarray, count: int) -> None:
        """Verschiebt, skaliert oder dreht horizontale Streifen (in place)"""
        rng = self.rng
        height, width = image.shape[:2]
        stripe = max(1, height // count)
        for i in range(count):
            if rng.random() >= 0.5:
                continue
            y0, y1 = i * stripe, min((i + 1) * stripe, height)
            if y1 <= y0:
                continue
            band = image[y0:y1].copy()
            kind = int(rng.integers(0, 3))
            if kind == 0:
                image[y0:y1] = np.roll(band, rand_int(rng, -width // 4, width // 4), axis=1)
            elif kind == 1:
                new_width = max(1, int(width * rng.uniform(0.7, 1.3)))
                resized = cv2.resize(band, (new_width, y1 - y0))
                if new_width < width:
                    pad = (width - new_width) // 2
                    image[y0:y1] = 0
                    image[y0:y1, pad:pad + new_width] = resized
                else:
                    crop = (new_width - width) // 2
                    image[y0:y1] = resized[:, crop:crop + width]
            else:
                # Drehpunkt im Streifen selbst (nicht in Bildkoordinaten)
                matrix = cv2.getRotationMatrix2D((width // 2, (y1 - y0) // 2),
                                                 rng.uniform(-10, 10), rng.uniform(0.9, 1.1))
                image[y0:y1] = cv2.warpAffine(band, matrix, (width, y1 - y0), borderMode=cv2.BORDER_REFLECT)

    def _apply_reality_breach(self, frame: np.ndarray) -> np.ndarray:
        """Leuchtender, verästelter Riss mit Verwirbelung und Farbfehlern"""
        rng = self.rng
        height, width = frame.shape[:2]
        result = frame.copy()
        self.breach_coordinates = []

        side = int(rng.integers(0, 4))
        x, y = [(rng.integers(0, width), 0), (width - 1, rng.integers(0, height)),
                (rng.integers(0, width), height - 1), (0, rng.integers(0, height))][side]
        x, y = float(x), float(y)
        target_x = width // 2 + rand_int(rng, -width // 4, width // 4)
        target_y = height // 2 + rand_int(rng, -height // 4, height // 4)
        chaos = rng.uniform(0.3, 0.8)
        segments = int(rng.integers(10, 30))
        main_points = [(int(x), int(y))]

        for i in range(segments):
            angle = np.arctan2(target_y - y, target_x - x) + rng.uniform(-np.pi / 3, np.pi / 3) * chaos
            length = rand_int(rng, 10, max(11, int(min(width, height) * 0.1)))
            x = float(np.clip(x + length * np.cos(angle), 0, width - 1))
            y = float(np.clip(y + length * np.sin(angle), 0, height - 1))
            main_points.append((int(x), int(y)))

            if rng.random() < 0.3 and i > 1:
                bx, by, branch_angle = x, y, angle + rng.uniform(-np.pi / 2, np.pi / 2)
                branch = []
                for _ in range(rand_int(rng, 3, max(4, segments // 3))):
                    branch_angle += rng.uniform(-np.pi / 4, np.pi / 4) * chaos
                    step = rand_int(rng, 5, max(6, length))
                    bx = float(np.clip(bx + step * np.cos(branch_angle), 0, width - 1))
                    by = float(np.clip(by + step * np.sin(branch_angle), 0, height - 1))
                    branch.append((int(bx), int(by)))
                self.breach_coordinates.extend(branch)
                if len(branch) > 1:
                    color = (int(rng.integers(50, 150)), int(rng.integers(50, 200)), int(rng.integers(100, 256)))
                    cv2.polylines(result, [np.array(branch, np.int32)], False, color, int(rng.integers(1, 4)))

        self.breach_coordinates.extend(main_points)
        color = (int(rng.integers(150, 256)), int(rng.integers(200, 256)), int(rng.integers(200, 256)))
        cv2.polylines(result, [np.array(main_points, np.int32)], False, color, int(rng.integers(3, 9)))

        # Aura
        overlay = np.zeros_like(result)
        for px, py in self.breach_coordinates:
            glow = tuple(int(c) for c in rng.integers(0, 256, 3))
            cv2.circle(overlay, (px, py), int(rng.integers(5, 20)), glow, -1)
        result = cv2.addWeighted(result, 0.7, cv2.GaussianBlur(overlay, (21, 21), 0), 0.5, 0)

        # Verwirbelung um die Risspunkte
        xs, ys = self.grid.get(height, width)
        map_x, map_y = xs.copy(), ys.copy()
        for px, py in self.breach_coordinates:
            radius = int(rng.integers(10, 50))
            strength = rng.uniform(5.0, 15.0)
            x0, x1 = max(0, px - radius), min(width, px + radius + 1)
            y0, y1 = max(0, py - radius), min(height, py + radius + 1)
            lx, ly = xs[y0:y1, x0:x1] - px, ys[y0:y1, x0:x1] - py
            dist = np.hypot(lx, ly)
            factor = np.where(dist < radius, (1.0 - dist / radius) * strength, 0.0)
            angle = np.arctan2(ly, lx) + np.pi / 2
            map_x[y0:y1, x0:x1] += factor * np.cos(angle)
            map_y[y0:y1, x0:x1] += factor * np.sin(angle)
        result = cv2.remap(result, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)

        # Punktuelle chromatische Aberration
        if rng.random() < 0.7:
            r, g, b = cv2.split(result)
            for px, py in self.breach_coordinates:
                radius = int(rng.integers(20, 60))
                x0, x1 = max(0, px - radius), min(width, px + radius)
                y0, y1 = max(0, py - radius), min(height, py + radius)
                if x1 > x0 and y1 > y0:
                    r[y0:y1, x0:x1] = shift_image(r[y0:y1, x0:x1].copy(), int(rng.integers(-5, 6)),
                                                  int(rng.integers(-5, 6)), cv2.BORDER_CONSTANT)
                    g[y0:y1, x0:x1] = shift_image(g[y0:y1, x0:x1].copy(), int(rng.integers(-5, 6)),
                                                  int(rng.integers(-5, 6)), cv2.BORDER_CONSTANT)
            result = cv2.merge([r, g, b])

        # Pixel-Anomalien entlang des Risses
        if rng.random() < 0.5 and self.breach_coordinates:
            for _ in range(int(rng.integers(3, 10))):
                cx, cy = self.breach_coordinates[int(rng.integers(0, len(self.breach_coordinates)))]
                gw, gh = min(int(rng.integers(10, 50)), width), min(int(rng.integers(10, 50)), height)
                x = int(np.clip(cx + rng.integers(-30, 31), 0, width - gw))
                y = int(np.clip(cy + rng.integers(-30, 31), 0, height - gh))
                region = result[y:y + gh, x:x + gw]
                kind = rng.random()
                if kind < 0.33:
                    region = np.roll(region, int(rng.integers(5, 20)), axis=1)
                elif kind < 0.66:
                    region = region.copy()
                    region[:, :, int(rng.integers(0, 3))] = int(rng.integers(0, 256))
                else:
                    pixel = int(rng.integers(5, 15))
                    small = cv2.resize(region, (max(1, gw // pixel), max(1, gh // pixel)))
                    region = cv2.resize(small, (gw, gh), interpolation=cv2.INTER_NEAREST)
                result[y:y + gh, x:x + gw] = region
        return result

# ----------------- Audio+Video Glitch -----------------

class AudioGlitcher:
    """
    Zufällige Audio-Glitches, blockweise im Audio-Thread.
    Ein Glitch dauert mehrere Blöcke, damit er hörbar ist.
    """

    GLITCH_PROBABILITY = 0.3
    COOLDOWN_SECONDS = 0.5

    def __init__(self, sample_rate: int = SoundDeviceLoopService.SAMPLE_RATE):
        self.sample_rate = sample_rate
        self.rng = np.random.default_rng()
        self.history: collections.deque = collections.deque(maxlen=5)
        self._samples_since_glitch = 0
        self._kind = None
        self._blocks_left = 0
        self._params: Dict[str, Any] = {}

    def process(self, block: np.ndarray) -> np.ndarray:
        rng = self.rng
        n = len(block)
        self.history.append(block.copy())
        self._samples_since_glitch += n

        if self._blocks_left == 0:
            if (self._samples_since_glitch < self.COOLDOWN_SECONDS * self.sample_rate
                    or rng.random() >= self.GLITCH_PROBABILITY):
                return np.clip(block, -1.0, 1.0).astype(np.float32)
            self._kind = int(rng.integers(0, 5))
            self._blocks_left = int(rng.integers(2, 8))
            self._params = {
                'bits': int(rng.integers(2, 8)),
                'factor': rng.uniform(0.8, 1.2),
                'source': self.history[int(rng.integers(0, len(self.history)))].copy(),
            }
            self._samples_since_glitch = 0

        self._blocks_left -= 1
        kind, p = self._kind, self._params
        if kind == 0:      # Rückwärts
            out = p['source'][::-1]
        elif kind == 1:    # Bit-Crushing
            levels = 2 ** p['bits']
            out = np.round(block * levels) / levels
        elif kind == 2:    # Stottern: kurzes Stück wiederholen
            piece = p['source'][len(p['source']) // 2:len(p['source']) // 2 + max(1, n // 10)]
            out = np.resize(piece, block.shape)
        elif kind == 3:    # Tonhöhe verschieben
            stretched = signal.resample(block, max(1, int(n * p['factor'])), axis=0)
            out = np.zeros_like(block)
            m = min(n, len(stretched))
            out[:m] = stretched[:m]
        else:              # Rauschen
            out = block + rng.normal(0, 0.1, block.shape)

        if out.shape != block.shape:
            out = np.resize(out, block.shape)
        return np.clip(out, -1.0, 1.0).astype(np.float32)


class AudioGlitchEffectStrategy(IEffectStrategy):
    """Video-Kanalversatz plus Mikrofon-Glitches auf dem Lautsprecher"""

    def __init__(self, audio_loop_service: IAudioLoopService):
        self.audio_loop = audio_loop_service
        self.glitcher = AudioGlitcher()
        self.rng = np.random.default_rng()

    @property
    def name(self) -> str:
        return "Audio+Video Glitch"

    def activate(self) -> None:
        self.audio_loop.start(self.glitcher.process)

    def deactivate(self) -> None:
        self.audio_loop.stop()

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        if rng.random() >= 0.5:
            return frame
        r, g, b = cv2.split(frame)
        r = shift_image(r, int(rng.integers(-10, 10)), int(rng.integers(-5, 5)), cv2.BORDER_WRAP)
        return cv2.merge([r, g, b])

# ----------------- Kern-Logik -----------------

class EffectProcessor:
    """Nimmt im Hintergrund-Thread auf, wendet den aktiven Effekt an und veröffentlicht die Bilder"""

    TARGET_FPS = 30
    ERROR_MESSAGE_INTERVAL = 2.0

    def __init__(self, capture_service: IScreenCaptureService, event_bus: IEventBus):
        self.capture_service = capture_service
        self.event_bus = event_bus
        self.effect_strategies: Dict[str, IEffectStrategy] = {}
        self.current_strategy: Optional[IEffectStrategy] = None
        self.config: Dict[str, Any] = {}
        self._lock = threading.Lock()
        self._thread: Optional[threading.Thread] = None
        self._stop_event = threading.Event()

        self.event_bus.subscribe('start_capture', self.start_capture)
        self.event_bus.subscribe('stop_capture', self.stop_capture)
        self.event_bus.subscribe('next_cell', self.next_cell)
        self.event_bus.subscribe('change_effect', self.change_effect)

    @property
    def is_running(self) -> bool:
        return self._thread is not None and self._thread.is_alive()

    @property
    def effect_names(self) -> List[str]:
        return list(self.effect_strategies)

    def register_effect(self, effect: IEffectStrategy) -> None:
        self.effect_strategies[effect.name] = effect
        if self.current_strategy is None:
            self.current_strategy = effect

    def change_effect(self, effect_name: str) -> None:
        new = self.effect_strategies.get(effect_name)
        if new is None:
            return
        with self._lock:
            old, self.current_strategy = self.current_strategy, new
        if old is new:
            return
        if self.is_running:
            if old is not None:
                old.deactivate()
            new.activate()
        self.event_bus.publish('effect_changed', new)
        self.event_bus.publish('status_update', f"Effekt gewechselt zu: {effect_name}")

    def start_capture(self, region: Dict[str, int]) -> None:
        if self.is_running:
            self.stop_capture(quiet=True)
        self.config = {'matrix_frames': [None] * 9, 'active_cell': 0, 'has_started': False,
                       'width': region['width'], 'height': region['height']}
        self._stop_event = threading.Event()
        self._thread = threading.Thread(target=self._capture_loop, args=(dict(region), self._stop_event),
                                        daemon=True)
        self._thread.start()
        if self.current_strategy is not None:
            self.current_strategy.activate()
        self.event_bus.publish('status_update',
                               f"Erfasse Region ({region['left']},{region['top']}) "
                               f"{region['width']}x{region['height']} px")

    def stop_capture(self, _=None, quiet: bool = False) -> None:
        self._stop_event.set()
        if self._thread is not None:
            self._thread.join(timeout=3.0)
        self._thread = None
        if self.current_strategy is not None:
            self.current_strategy.deactivate()
        if not quiet:
            self.event_bus.publish('status_update', "Gestoppt. Bereit für neuen Start.")

    def next_cell(self, _=None) -> None:
        if self.is_running and self.config.get('has_started'):
            self.config['active_cell'] = (self.config['active_cell'] + 1) % 9
            self.event_bus.publish('status_update', f"Aktive Zelle: {self.config['active_cell'] + 1} von 9")

    def _capture_loop(self, region: Dict[str, int], stop_event: threading.Event) -> None:
        interval = 1.0 / self.TARGET_FPS
        last_error_time = 0.0
        black_frames = 0
        fps_frames, fps_start = 0, time.perf_counter()
        next_frame_time = time.perf_counter()

        try:
            while not stop_event.is_set():
                try:
                    frame = self.capture_service.capture(region)
                except Exception as e:
                    self.event_bus.publish('status_update', f"Aufnahme-Fehler: {e}")
                    stop_event.wait(1.0)
                    continue

                # Ohne Freigabe "Bildschirmaufnahme" liefert macOS schwarze Bilder
                if black_frames >= 0:
                    black_frames = black_frames + 1 if frame.max() < 8 else -1
                    if black_frames == self.TARGET_FPS:
                        self.event_bus.publish('status_update',
                                               "Nur schwarzes Bild: Bildschirmaufnahme-Freigabe fürs Terminal prüfen "
                                               "(Systemeinstellungen > Datenschutz & Sicherheit)")
                        black_frames = -1

                with self._lock:
                    strategy = self.current_strategy
                was_started = self.config.get('has_started')
                try:
                    output = strategy.apply(frame, self.config)
                except Exception as e:
                    output = frame
                    now = time.time()
                    if now - last_error_time > self.ERROR_MESSAGE_INTERVAL:
                        self.event_bus.publish('status_update', f"Fehler in '{strategy.name}': {e}")
                        last_error_time = now
                if strategy.uses_cells and not was_started and self.config.get('has_started'):
                    self.event_bus.publish('status_update', "Effekt gestartet. 'Nächste Zelle' schaltet weiter.")

                self.event_bus.publish('frame_processed', output)

                fps_frames += 1
                elapsed_fps = time.perf_counter() - fps_start
                if elapsed_fps >= 1.0:
                    self.event_bus.publish('fps', fps_frames / elapsed_fps)
                    fps_frames, fps_start = 0, time.perf_counter()

                # Feste Taktzeitpunkte: Ungenauigkeiten beim Warten summieren sich nicht auf
                next_frame_time += interval
                delay = next_frame_time - time.perf_counter()
                if delay < -interval:
                    next_frame_time = time.perf_counter()   # zu langsam: Takt neu aufsetzen
                stop_event.wait(max(0.0, delay))
        finally:
            self.capture_service.close()

# ----------------- GUI -----------------

class EffectView:
    """
    Benutzeroberfläche. Tkinter darf nur vom Haupt-Thread bedient werden:
    Events aus dem Aufnahme-Thread landen in Queues und werden per after() abgeholt.
    """

    POLL_MS = 15
    MIN_REGION = 16

    def __init__(self, event_bus: IEventBus, effect_names: List[str]):
        self.event_bus = event_bus
        self.effect_names = effect_names
        self._frames: queue.Queue = queue.Queue(maxsize=1)
        self._messages: queue.Queue = queue.Queue()
        self._photo: Optional[ImageTk.PhotoImage] = None
        self._image_item = None
        self._running = False
        self._closed = False

        self.root = tk.Tk()
        self.root.title("Bildeffekte")
        self._init_ui()

        self.event_bus.subscribe('status_update', lambda msg: self._messages.put(('status', msg)))
        self.event_bus.subscribe('fps', lambda fps: self._messages.put(('fps', fps)))
        self.event_bus.subscribe('effect_changed', lambda effect: self._messages.put(('effect', effect)))
        self.event_bus.subscribe('frame_processed', self._offer_frame)

        self.root.protocol("WM_DELETE_WINDOW", self.on_closing)
        self.root.after(self.POLL_MS, self._poll)

    def _init_ui(self) -> None:
        self.config_frame = tk.Frame(self.root)
        self.config_frame.pack(pady=10)

        tk.Label(self.config_frame, text="Bereich (x,y,breite,höhe):").grid(row=0, column=0, padx=5)
        self.region_entry = tk.Entry(self.config_frame, width=20)
        self.region_entry.insert(0, "100,100,800,450")
        self.region_entry.grid(row=0, column=1, padx=5)

        self.btn_pick = tk.Button(self.config_frame, text="Bereich wählen…", command=self.pick_region)
        self.btn_pick.grid(row=0, column=2, padx=5)

        self.btn_start = tk.Button(self.config_frame, text="Start", command=self.start_effect)
        self.btn_start.grid(row=0, column=3, padx=5)

        self.btn_next = tk.Button(self.config_frame, text="Nächste Zelle", command=self.next_cell, state=tk.DISABLED)
        self.btn_next.grid(row=0, column=4, padx=5)

        self.btn_stop = tk.Button(self.config_frame, text="Stop", command=self.stop_effect, state=tk.DISABLED)
        self.btn_stop.grid(row=0, column=5, padx=5)

        self.effect_frame = tk.Frame(self.root)
        self.effect_frame.pack(pady=5)
        tk.Label(self.effect_frame, text="Effekt:").grid(row=0, column=0, padx=5)
        self.effect_var = tk.StringVar(value=self.effect_names[0] if self.effect_names else "")
        self.effect_dropdown = tk.OptionMenu(self.effect_frame, self.effect_var, *self.effect_names)
        self.effect_dropdown.grid(row=0, column=1, padx=5)
        self.effect_var.trace_add("write", self.change_effect)

        status_bar = tk.Frame(self.root, bd=1, relief=tk.SUNKEN)
        status_bar.pack(side=tk.BOTTOM, fill=tk.X)
        self.status_var = tk.StringVar(value="Bereit. Bereich eingeben oder wählen, dann Start klicken.")
        tk.Label(status_bar, textvariable=self.status_var, anchor=tk.W).pack(side=tk.LEFT, fill=tk.X, expand=True)
        self.fps_var = tk.StringVar(value="")
        tk.Label(status_bar, textvariable=self.fps_var, anchor=tk.E, width=10).pack(side=tk.RIGHT)

        self.canvas = tk.Canvas(self.root, bg='black', width=800, height=450, highlightthickness=0)
        self.canvas.pack(expand=True, fill=tk.BOTH, padx=10, pady=10)

    def run(self) -> None:
        self.root.geometry("900x560")
        self.root.mainloop()

    # --- Bereichswahl ---

    def pick_region(self) -> None:
        """Halbtransparente Fläche über dem Bildschirm: Bereich mit der Maus aufziehen"""
        overlay = tk.Toplevel(self.root)
        overlay.overrideredirect(True)
        overlay.geometry(f"{self.root.winfo_screenwidth()}x{self.root.winfo_screenheight()}+0+0")
        overlay.attributes('-alpha', 0.3)
        overlay.attributes('-topmost', True)
        canvas = tk.Canvas(overlay, bg='gray20', cursor='crosshair', highlightthickness=0)
        canvas.pack(fill=tk.BOTH, expand=True)
        canvas.create_text(self.root.winfo_screenwidth() // 2, 40, fill='white', font=('Helvetica', 24),
                           text="Bereich aufziehen  –  Esc oder Rechtsklick bricht ab")
        state: Dict[str, Any] = {}

        def press(event):
            state['start'] = (event.x_root, event.y_root, event.x, event.y)
            state['rect'] = canvas.create_rectangle(event.x, event.y, event.x, event.y, outline='red', width=3)

        def drag(event):
            if 'rect' in state:
                _, _, cx, cy = state['start']
                canvas.coords(state['rect'], cx, cy, event.x, event.y)

        def release(event):
            if 'start' not in state:
                return
            sx, sy, _, _ = state['start']
            x0, x1 = sorted((sx, event.x_root))
            y0, y1 = sorted((sy, event.y_root))
            overlay.destroy()
            if x1 - x0 >= self.MIN_REGION and y1 - y0 >= self.MIN_REGION:
                self.region_entry.delete(0, tk.END)
                self.region_entry.insert(0, f"{x0},{y0},{x1 - x0},{y1 - y0}")
                self.status_var.set("Bereich übernommen. Start klicken.")

        canvas.bind('<ButtonPress-1>', press)
        canvas.bind('<B1-Motion>', drag)
        canvas.bind('<ButtonRelease-1>', release)
        for sequence in ('<Escape>', '<ButtonPress-2>', '<ButtonPress-3>'):
            overlay.bind(sequence, lambda e: overlay.destroy())
        overlay.focus_force()

    # --- Aktionen ---

    def start_effect(self) -> None:
        try:
            coords = [c.strip() for c in self.region_entry.get().split(',')]
            if len(coords) != 4:
                raise ValueError("Format muss sein: x,y,breite,höhe")
            x, y, width, height = map(int, coords)
            if width < self.MIN_REGION or height < self.MIN_REGION:
                raise ValueError(f"Breite und Höhe müssen mindestens {self.MIN_REGION} px sein")
        except ValueError as e:
            self.status_var.set(f"Fehler: {e}")
            return

        self.canvas.config(width=width, height=height)
        self._running = True
        self._update_buttons()
        self.event_bus.publish('start_capture', {'left': x, 'top': y, 'width': width, 'height': height})

    def stop_effect(self) -> None:
        self._running = False
        self._update_buttons()
        self.event_bus.publish('stop_capture')
        self.fps_var.set("")

    def next_cell(self) -> None:
        self.event_bus.publish('next_cell')

    def change_effect(self, *args) -> None:
        self.event_bus.publish('change_effect', self.effect_var.get())

    def _update_buttons(self, effect: Optional[IEffectStrategy] = None) -> None:
        uses_cells = effect.uses_cells if effect is not None else self.effect_var.get() == "Mosaik"
        self.btn_start.config(state=tk.DISABLED if self._running else tk.NORMAL)
        self.btn_stop.config(state=tk.NORMAL if self._running else tk.DISABLED)
        self.btn_next.config(state=tk.NORMAL if self._running and uses_cells else tk.DISABLED)

    # --- Anzeige (nur Haupt-Thread) ---

    def _offer_frame(self, frame: np.ndarray) -> None:
        """Aufnahme-Thread: nur das neueste Bild behalten"""
        try:
            self._frames.get_nowait()
        except queue.Empty:
            pass
        try:
            self._frames.put_nowait(frame)
        except queue.Full:
            pass

    def _poll(self) -> None:
        if self._closed:
            return
        while True:
            try:
                kind, data = self._messages.get_nowait()
            except queue.Empty:
                break
            if kind == 'status':
                self.status_var.set(data)
            elif kind == 'fps' and self._running:
                self.fps_var.set(f"{data:4.1f} FPS")
            elif kind == 'effect':
                self._update_buttons(data)

        try:
            frame = self._frames.get_nowait()
        except queue.Empty:
            frame = None
        if frame is not None and self._running:
            self._photo = ImageTk.PhotoImage(image=Image.fromarray(frame))
            if self._image_item is None:
                self._image_item = self.canvas.create_image(0, 0, anchor=tk.NW, image=self._photo)
            else:
                self.canvas.itemconfig(self._image_item, image=self._photo)

        self.root.after(self.POLL_MS, self._poll)

    def on_closing(self) -> None:
        self._closed = True
        self.event_bus.publish('stop_capture')
        self.root.destroy()

# ----------------- Anwendungsstart -----------------

def build_processor(event_bus: IEventBus) -> EffectProcessor:
    processor = EffectProcessor(Container.resolve(IScreenCaptureService), event_bus)
    for effect in (
        MosaicEffectStrategy(),
        BlurEffectStrategy(),
        EdgeDetectionEffectStrategy(),
        SlowMotionSnapshotEffectStrategy(),
        RealTimeGlitchEffectStrategy(),
        AudioGlitchEffectStrategy(Container.resolve(IAudioLoopService)),
        VHSGlitchEffectStrategy(),
        OldFilmEffectStrategy(),
        GlitchDimensionEffectStrategy(),
    ):
        processor.register_effect(effect)
    return processor


def main():
    Container.register(IEventBus, EventBus)
    Container.register(IScreenCaptureService, MssScreenCaptureService)
    Container.register(IAudioLoopService, SoundDeviceLoopService)

    event_bus = Container.resolve(IEventBus)
    processor = build_processor(event_bus)
    view = EffectView(event_bus, processor.effect_names)
    view.run()


if __name__ == "__main__":
    main()
