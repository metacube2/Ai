#!/usr/bin/env python3
"""
Bildeffekte in Echtzeit auf einen Bildschirmbereich.

Ein wählbarer Bereich des Bildschirms (z. B. ein Videoplayer-Fenster) wird
laufend abgefilmt, mit einem Effekt verändert und in der Vorschau angezeigt.
Unabhängig davon verändern Ton-Effekte den Ton – gesteuert vom Bild.

Ton des Videos statt Mikrofon: virtuelles Audiogerät BlackHole installieren
(brew install blackhole-2ch), Systemausgabe auf BlackHole stellen, im Programm
BlackHole als Eingang und die Lautsprecher als Ausgang wählen.

Abhängigkeiten: numpy, opencv-python(-headless), mss, PySide6, sounddevice, scipy
macOS: Das Terminal braucht die Freigabe "Bildschirmaufnahme", für den
Audio+Video-Glitch zusätzlich "Mikrofon".
"""

import collections
import inspect
import sys
import threading
import time
from abc import ABC, abstractmethod
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from typing import Any, Callable, Dict, List, Optional, Tuple, Type

import cv2
import mss
import numpy as np
import sounddevice as sd
from PySide6.QtCore import QObject, QPoint, QRect, QSize, Qt, QTimer, Signal
from PySide6.QtGui import QColor, QCursor, QGuiApplication, QImage, QKeySequence, QPainter, QPalette, QPen, QShortcut
from PySide6.QtWidgets import (QApplication, QComboBox, QHBoxLayout, QLabel, QLineEdit, QMainWindow,
                               QPushButton, QSlider, QVBoxLayout, QWidget)
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
    """
    Audio-Schleife Eingang -> Stufen -> Ausgang. Eine Stufe ist ein Objekt mit
    process(block N x Kanäle) und optional prepare(sample_rate, channels).
    Der Stream läuft, solange mindestens eine Stufe gesetzt ist.
    """

    @abstractmethod
    def set_stage(self, name: str, stage: Optional[Any]) -> None:
        """Setzt oder entfernt ('None') eine benannte Stufe"""

    @abstractmethod
    def set_devices(self, input_device: Optional[int], output_device: Optional[int]) -> None:
        """Wählt die Geräte (None = Systemstandard)"""

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
    """Duplex-Stream mit Stufen: Ton-Analyse, Ton-Effekt, Audio+Video-Glitch"""

    STAGE_ORDER = ('analysis', 'effect', 'video_glitch')
    PREFERRED_RATES = (48000, 44100)
    BLOCK_SIZE = 512

    def __init__(self, event_bus: IEventBus):
        self.event_bus = event_bus
        self.input_device: Optional[int] = None
        self.output_device: Optional[int] = None
        self.sample_rate = self.PREFERRED_RATES[0]
        self.in_channels = self.out_channels = 1
        self._stages: Dict[str, Any] = {}
        self._chain: Tuple[Any, ...] = ()
        self._stream: Optional[sd.Stream] = None
        self._lock = threading.Lock()

    def set_stage(self, name: str, stage: Optional[Any]) -> None:
        with self._lock:
            if stage is None:
                self._stages.pop(name, None)
            else:
                if self._stream is not None and hasattr(stage, 'prepare'):
                    stage.prepare(self.sample_rate, self.in_channels)
                self._stages[name] = stage
            self._rebuild_chain()
            if self._stages and self._stream is None:
                self._start()
            elif not self._stages and self._stream is not None:
                self._stop()

    def set_devices(self, input_device: Optional[int], output_device: Optional[int]) -> None:
        with self._lock:
            self.input_device, self.output_device = input_device, output_device
            if self._stream is not None:
                self._stop()
                self._start()

    def _rebuild_chain(self) -> None:
        # Tupel wird atomar ersetzt: der Audio-Thread braucht keine Sperre
        self._chain = tuple(self._stages[n] for n in self.STAGE_ORDER if n in self._stages)

    def _callback(self, indata, outdata, frames, time_info, status):
        try:
            block = indata.copy()
            chain = self._chain
            for stage in chain:
                block = stage.process(block)
            if all(getattr(stage, 'silent_if_alone', False) for stage in chain):
                outdata.fill(0)          # nur Messung aktiv: nichts ausgeben (keine Rückkopplung)
                return
            if block.shape[1] != outdata.shape[1]:
                block = np.repeat(block.mean(axis=1, keepdims=True), outdata.shape[1], axis=1)
            outdata[:] = np.clip(block, -1.0, 1.0)
        except Exception:
            outdata.fill(0)

    def _start(self) -> None:
        try:
            inp = sd.query_devices(self.input_device, 'input')
            out = sd.query_devices(self.output_device, 'output')
            self.in_channels = max(1, min(2, int(inp['max_input_channels'])))
            self.out_channels = max(1, min(2, int(out['max_output_channels'])))
            rate = int(inp['default_samplerate'])
            for candidate in self.PREFERRED_RATES:
                try:
                    sd.check_input_settings(self.input_device, channels=self.in_channels, samplerate=candidate)
                    sd.check_output_settings(self.output_device, channels=self.out_channels, samplerate=candidate)
                    rate = candidate
                    break
                except Exception:
                    continue
            self.sample_rate = rate
            for stage in self._stages.values():
                if hasattr(stage, 'prepare'):
                    stage.prepare(rate, self.in_channels)
            self._stream = sd.Stream(samplerate=rate, blocksize=self.BLOCK_SIZE,
                                     device=(self.input_device, self.output_device),
                                     channels=(self.in_channels, self.out_channels),
                                     dtype='float32', latency='low', callback=self._callback)
            self._stream.start()
            only_analysis = all(getattr(st, 'silent_if_alone', False) for st in self._stages.values())
            hint = "" if only_analysis or 'blackhole' in inp['name'].lower() else "  –  Kopfhörer benutzen, sonst Rückkopplung!"
            self.event_bus.publish('status_update', f"Ton: {inp['name']} -> {out['name']}, {rate} Hz{hint}")
        except Exception as e:
            self._stream = None
            self.event_bus.publish('status_update', f"Audio-Fehler: {e}")

    def _stop(self) -> None:
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

# ----------------- Datamosh -----------------

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

# ----------------- Pixel-Sorting -----------------

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

# ----------------- Matrix-Code -----------------

class MatrixCodeEffectStrategy(IEffectStrategy):
    """Das Bild wird aus grünen, herabregnenden Zeichen aufgebaut"""

    CELL_W, CELL_H = 10, 14
    CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ$#*+=<>:;!?%&@"

    def __init__(self):
        self.rng = np.random.default_rng()
        self.glyphs = self._build_glyphs()
        self._shape = None
        self.glyph_index: Optional[np.ndarray] = None
        self.heads: Optional[np.ndarray] = None
        self.speeds: Optional[np.ndarray] = None
        self.trails: Optional[np.ndarray] = None

    @property
    def name(self) -> str:
        return "Matrix-Code"

    def _build_glyphs(self) -> np.ndarray:
        """Zeichen-Atlas (Anzahl x Höhe x Breite), gespiegelt wie im Film"""
        glyphs = []
        for char in self.CHARS:
            canvas = np.zeros((self.CELL_H, self.CELL_W), dtype=np.uint8)
            cv2.putText(canvas, char, (1, self.CELL_H - 3), cv2.FONT_HERSHEY_PLAIN, 0.8, 255, 1, cv2.LINE_AA)
            glyphs.append(cv2.flip(canvas, 1))
        return np.stack(glyphs).astype(np.float32) / 255.0

    def _reset(self, rows: int, cols: int) -> None:
        rng = self.rng
        self._shape = (rows, cols)
        self.glyph_index = rng.integers(0, len(self.glyphs), (rows, cols))
        self.heads = rng.uniform(-rows, rows, cols)
        self.speeds = rng.uniform(0.3, 1.2, cols)
        self.trails = rng.uniform(6, 20, cols)

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        height, width = frame.shape[:2]
        rows, cols = max(1, height // self.CELL_H), max(1, width // self.CELL_W)
        if self._shape != (rows, cols):
            self._reset(rows, cols)

        # Helligkeit des Bildes pro Zelle
        gray = cv2.cvtColor(frame, cv2.COLOR_RGB2GRAY)
        luminance = cv2.resize(gray, (cols, rows), interpolation=cv2.INTER_AREA).astype(np.float32) / 255.0

        # Fallende Regenspuren pro Spalte
        self.heads += self.speeds
        restart = self.heads - self.trails > rows
        self.heads[restart] = rng.uniform(-20, 0, int(restart.sum()))
        self.speeds[restart] = rng.uniform(0.3, 1.2, int(restart.sum()))
        distance = self.heads[None, :] - np.arange(rows)[:, None]
        rain = np.where(distance >= 0, np.clip(1.0 - distance / self.trails[None, :], 0, 1), 0.0)
        head = (distance >= 0) & (distance < 1)

        # Zeichen flackern
        change = rng.random((rows, cols)) < 0.03
        self.glyph_index[change] = rng.integers(0, len(self.glyphs), int(change.sum()))
        self.glyph_index[head] = rng.integers(0, len(self.glyphs), int(head.sum()))

        # Bildhelligkeit bestimmt, wie hell die Zeichen sind; der Regen moduliert nur
        intensity = np.clip(0.03 + 1.1 * luminance ** 1.4, 0, 1) * (0.45 + 0.55 * rain)
        tiles = self.glyphs[self.glyph_index] * intensity[..., None, None]       # rows, cols, h, w
        mask = tiles.transpose(0, 2, 1, 3).reshape(rows * self.CELL_H, cols * self.CELL_W)
        head_mask = (self.glyphs[self.glyph_index] * head[..., None, None]).transpose(0, 2, 1, 3) \
            .reshape(rows * self.CELL_H, cols * self.CELL_W)

        result = np.zeros_like(frame)
        h, w = mask.shape
        green = np.stack([mask * 60, mask * 255, mask * 90], axis=-1)
        white = head_mask[..., None] * 200
        result[:h, :w] = np.clip(green + white, 0, 255).astype(np.uint8)
        return result

# ----------------- Kaleidoskop-Tunnel -----------------

class KaleidoscopeTunnelEffectStrategy(IEffectStrategy):
    """Rotierendes Spiegelkaleidoskop mit Rückkopplung: zieht in einen farbwechselnden Tunnel"""

    def __init__(self):
        self.rng = np.random.default_rng()
        self.frame_counter = 0
        self.segments = 6
        self.frames_until_change = 240
        self.feedback: Optional[np.ndarray] = None
        self._shape = None
        self._radius = self._angle = None

    @property
    def name(self) -> str:
        return "Kaleidoskop-Tunnel"

    def _polar(self, height: int, width: int) -> Tuple[np.ndarray, np.ndarray]:
        if self._shape != (height, width):
            xs, ys = np.meshgrid(np.arange(width, dtype=np.float32) - width / 2,
                                 np.arange(height, dtype=np.float32) - height / 2)
            self._radius = np.hypot(xs, ys)
            self._angle = np.arctan2(ys, xs)
            self._shape = (height, width)
            self.feedback = None
        return self._radius, self._angle

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        self.frame_counter += 1
        height, width = frame.shape[:2]
        radius, angle = self._polar(height, width)

        self.frames_until_change -= 1
        if self.frames_until_change <= 0:
            self.segments = int(self.rng.choice([4, 6, 8, 10, 12]))
            self.frames_until_change = int(self.rng.integers(150, 400))

        # Winkel in ein Segment falten und spiegeln
        t = self.frame_counter
        segment = 2 * np.pi / self.segments
        rotation = t * 0.01
        folded = np.abs(np.mod(angle + rotation, segment) - segment / 2)
        zoom = 0.8 + 0.2 * np.sin(t * 0.013)
        src_r = radius * zoom
        source_angle = folded + t * 0.004
        map_x = (width / 2 + src_r * np.cos(source_angle)).astype(np.float32)
        map_y = (height / 2 + src_r * np.sin(source_angle)).astype(np.float32)
        kaleido = cv2.remap(frame, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)

        if self.feedback is None:
            self.feedback = kaleido
            return kaleido

        # Rückkopplung: letztes Bild leicht vergrößert, gedreht und im Farbton verschoben
        matrix = cv2.getRotationMatrix2D((width / 2, height / 2), 1.2 * np.sin(t * 0.005) + 0.6, 1.04)
        tunnel = cv2.warpAffine(self.feedback, matrix, (width, height), borderMode=cv2.BORDER_REFLECT)
        hsv = cv2.cvtColor(tunnel, cv2.COLOR_RGB2HSV)
        hsv[:, :, 0] = hsv[:, :, 0] + np.uint8(3)
        hsv[:, :, 0][hsv[:, :, 0] >= 180] -= 180
        tunnel = cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)

        # Bildmitte zeigt das frische Kaleidoskop, außen dominiert der Tunnel
        weight = np.clip(radius / (0.5 * np.hypot(width, height)), 0, 1)[..., None] * 0.75
        result = (kaleido * (1 - weight) + tunnel * weight).astype(np.uint8)
        self.feedback = result
        return result

# ----------------- Audio+Video Glitch -----------------

class AudioGlitcher:
    """
    Zufällige Audio-Glitches, blockweise im Audio-Thread.
    Ein Glitch dauert mehrere Blöcke, damit er hörbar ist.
    """

    GLITCH_PROBABILITY = 0.3
    COOLDOWN_SECONDS = 0.5

    def __init__(self, sample_rate: int = SoundDeviceLoopService.PREFERRED_RATES[0]):
        self.sample_rate = sample_rate
        self.rng = np.random.default_rng()
        self.history: collections.deque = collections.deque(maxlen=5)
        self._samples_since_glitch = 0
        self._kind = None
        self._blocks_left = 0
        self._params: Dict[str, Any] = {}

    def prepare(self, sample_rate: int, channels: int) -> None:
        self.sample_rate = sample_rate
        self.history.clear()

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
        self.audio_loop.set_stage('video_glitch', self.glitcher)

    def deactivate(self) -> None:
        self.audio_loop.set_stage('video_glitch', None)

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        if rng.random() >= 0.5:
            return frame
        r, g, b = cv2.split(frame)
        r = shift_image(r, int(rng.integers(-10, 10)), int(rng.integers(-5, 5)), cv2.BORDER_WRAP)
        return cv2.merge([r, g, b])

# ----------------- Bildmerkmale für Ton-Effekte -----------------

@dataclass(frozen=True)
class VideoFeatureSnapshot:
    """Momentaufnahme der Bildmerkmale. valid=False, wenn gerade kein Video läuft."""
    valid: bool = False
    row_profile: Optional[np.ndarray] = None   # Helligkeit je Bildzeile (oben -> unten), 0..1
    brightness: float = 0.5
    hue: float = 0.0                           # Farbton in Grad (0 = Rot)
    saturation: float = 0.0                    # 0 = grau, 1 = kräftig einfarbig
    motion_x: float = 0.0                      # Bewegung pro Bild, Anteil der Bildbreite
    motion_y: float = 0.0                      # positiv = nach unten
    activity: float = 0.0                      # mittlere Bildänderung 0..1
    cut_count: int = 0
    cut_times: Tuple[float, ...] = ()


class VideoFeatures:
    """
    Leichte Bildanalyse für die Ton-Effekte. update() läuft im Aufnahme-Thread
    (nur wenn ein Ton-Effekt aktiv ist), snapshot() liest im Audio-Thread.
    """

    ROWS = 96
    STALE_SECONDS = 1.0

    def __init__(self):
        self.enabled = False
        self._snapshot = VideoFeatureSnapshot()
        self._timestamp = 0.0
        self._prev_small: Optional[np.ndarray] = None
        self._activity_avg = 0.02
        self._cut_times: collections.deque = collections.deque(maxlen=16)
        self._cut_count = 0
        self._last_cut = 0.0

    def update(self, frame: np.ndarray) -> None:
        now = time.time()
        # Erst per Raster ausdünnen, dann weich verkleinern: INTER_AREA direkt vom Vollbild ist teuer
        step = max(1, min(frame.shape[0] // (2 * self.ROWS), frame.shape[1] // 128))
        base = cv2.resize(np.ascontiguousarray(frame[::step, ::step]), (64, self.ROWS), interpolation=cv2.INTER_AREA)
        gray = cv2.cvtColor(base, cv2.COLOR_RGB2GRAY)
        rows = gray.mean(axis=1) / 255.0

        small_rgb = cv2.resize(base, (64, 36), interpolation=cv2.INTER_AREA)
        small = cv2.cvtColor(small_rgb, cv2.COLOR_RGB2GRAY).astype(np.float32) / 255.0
        hsv = cv2.cvtColor(small_rgb, cv2.COLOR_RGB2HSV).astype(np.float32)
        weight = (hsv[..., 1] / 255.0) * (hsv[..., 2] / 255.0)
        angle = hsv[..., 0] * (np.pi / 90.0)            # OpenCV-Farbton 0..180 -> Bogenmaß
        sx, sy = float((weight * np.cos(angle)).sum()), float((weight * np.sin(angle)).sum())
        hue = (np.degrees(np.arctan2(sy, sx)) + 360.0) % 360.0
        saturation = float(np.hypot(sx, sy) / weight.size)

        motion_x = motion_y = activity = 0.0
        if self._prev_small is not None and self._prev_small.shape == small.shape:
            (dx, dy), response = cv2.phaseCorrelate(self._prev_small, small)
            if response > 0.1:
                motion_x, motion_y = dx / small.shape[1], dy / small.shape[0]
            activity = float(np.mean(np.abs(small - self._prev_small)))
            if activity > 0.12 and activity > 4 * self._activity_avg and now - self._last_cut > 0.25:
                self._cut_count += 1
                self._cut_times.append(now)
                self._last_cut = now
            self._activity_avg = 0.95 * self._activity_avg + 0.05 * activity
        self._prev_small = small

        self._snapshot = VideoFeatureSnapshot(
            True, rows.astype(np.float32), float(gray.mean()) / 255.0, float(hue), saturation,
            float(motion_x), float(motion_y), activity, self._cut_count, tuple(self._cut_times))
        self._timestamp = now

    def snapshot(self) -> VideoFeatureSnapshot:
        if time.time() - self._timestamp > self.STALE_SECONDS:
            return VideoFeatureSnapshot(cut_count=self._cut_count, cut_times=tuple(self._cut_times))
        return self._snapshot

# ----------------- Ton-Effekte -----------------

class IAudioEffect(ABC):
    """Ton-Effekt: verarbeitet Mono-Blöcke im Audio-Thread"""

    description = ""

    @property
    @abstractmethod
    def name(self) -> str:
        """Name des Effekts"""

    @property
    @abstractmethod
    def latency(self) -> int:
        """Verzögerung in Samples (für die Ausrichtung des trockenen Signals)"""

    @abstractmethod
    def reset(self, sample_rate: int) -> None:
        """Zustand für eine Samplerate neu aufbauen"""

    @abstractmethod
    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        """Mono float32 (n,) -> Mono float32 (n,)"""


class StftAudioEffect(IAudioEffect):
    """
    Basis für spektrale Effekte: Kurzzeit-Fourier-Transformation mit 4-facher
    Überlappung. Unterklassen bearbeiten pro Analyseschritt Betrag und Phase.
    """

    FRAME = 2048
    HOP = 512

    def reset(self, sample_rate: int) -> None:
        n, hop = self.FRAME, self.HOP
        self.sample_rate = sample_rate
        self.window = (0.5 - 0.5 * np.cos(2 * np.pi * np.arange(n) / n)).astype(np.float32)
        self.norm = float(np.sum(self.window ** 2) / hop)
        self.freqs = np.fft.rfftfreq(n, 1.0 / sample_rate)
        self.bins = np.arange(len(self.freqs), dtype=np.float64)
        self.bin_advance = 2 * np.pi * self.bins * hop / n      # Phasenfortschritt der Bin-Mitten
        self.hops_per_second = sample_rate / hop
        self._frame = np.zeros(n, dtype=np.float32)
        self._pending = np.zeros(0, dtype=np.float32)
        self._accum = np.zeros(n, dtype=np.float32)
        self._out = np.zeros(hop, dtype=np.float32)            # Vorlauf: Ausgabe läuft nie leer
        self.rng = np.random.default_rng()
        self.on_reset()

    @property
    def latency(self) -> int:
        return self.FRAME

    def on_reset(self) -> None:
        pass

    @abstractmethod
    def process_frame(self, mag: np.ndarray, phase: np.ndarray,
                      video: VideoFeatureSnapshot) -> Tuple[np.ndarray, np.ndarray]:
        """Betrag und Phase eines Analyseschritts verändern"""

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n, hop = self.FRAME, self.HOP
        self._pending = np.concatenate([self._pending, block.astype(np.float32)])
        produced = [self._out]
        while len(self._pending) >= hop:
            self._frame[:-hop] = self._frame[hop:]
            self._frame[-hop:] = self._pending[:hop]
            self._pending = self._pending[hop:]
            spectrum = np.fft.rfft(self._frame * self.window)
            mag, phase = self.process_frame(np.abs(spectrum), np.angle(spectrum), video)
            frame = np.fft.irfft(mag * np.exp(1j * phase), n).astype(np.float32)
            self._accum += frame * self.window / self.norm
            produced.append(self._accum[:hop].copy())
            self._accum[:-hop] = self._accum[hop:]
            self._accum[-hop:] = 0.0
        out = np.concatenate(produced)
        self._out = out[len(block):]
        return out[:len(block)]


class OnsetDetector:
    """Erkennt Einsätze (Silben, Schläge) über den spektralen Fluss"""

    def __init__(self, sample_rate: int, frame: int = 1024, hop: int = 512, min_gap: float = 0.12):
        self.frame, self.hop = frame, hop
        self.window = np.hanning(frame).astype(np.float32)
        self.min_gap = int(min_gap * sample_rate)
        self._buffer = np.zeros(frame, dtype=np.float32)
        self._pending = np.zeros(0, dtype=np.float32)
        self._prev = np.zeros(frame // 2 + 1)
        self._count = 0                 # verarbeitete Samples (absolut)
        self._flux_avg = 1.0
        self._last_onset = -10 ** 9

    def feed(self, block: np.ndarray) -> List[int]:
        onsets = []
        self._pending = np.concatenate([self._pending, block])
        while len(self._pending) >= self.hop:
            self._buffer[:-self.hop] = self._buffer[self.hop:]
            self._buffer[-self.hop:] = self._pending[:self.hop]
            self._pending = self._pending[self.hop:]
            self._count += self.hop
            mag = np.abs(np.fft.rfft(self._buffer * self.window))
            flux = float(np.sum(np.maximum(0.0, np.log1p(mag * 10) - np.log1p(self._prev * 10))))
            self._prev = mag
            loud = float(np.sqrt(np.mean(self._buffer[-self.hop:] ** 2))) > 0.005
            position = self._count - self.hop
            if loud and flux > 2.2 * self._flux_avg + 1.0 and position - self._last_onset > self.min_gap:
                onsets.append(position)
                self._last_onset = position
            self._flux_avg = 0.9 * self._flux_avg + 0.1 * flux
        return onsets


class ImageStencilEffect(StftAudioEffect):
    description = ("Das Bild ist der Filter: Jede Bildzeile steuert ein Frequenzband "
                   "(oben hoch, unten tief). Helle Stellen lassen durch, dunkle schlucken.")
    MIN_HZ, MAX_HZ = 80.0, 12000.0
    FLOOR = 0.03

    @property
    def name(self) -> str:
        return "Bild-Schablone"

    def on_reset(self) -> None:
        position = np.clip(np.log(np.maximum(self.freqs, 1.0) / self.MIN_HZ) / np.log(self.MAX_HZ / self.MIN_HZ), 0, 1)
        self._row = (1.0 - position) * (VideoFeatures.ROWS - 1)
        self._gains = np.ones(len(self.freqs))

    def process_frame(self, mag, phase, video):
        if video.valid and video.row_profile is not None:
            profile = video.row_profile.astype(np.float64)
            lo, hi = profile.min(), profile.max()
            stretched = (profile - lo) / (hi - lo) if hi - lo > 0.05 else profile
            profile = 0.3 * profile + 0.7 * stretched
            target = self.FLOOR + (1 - self.FLOOR) * np.interp(self._row, np.arange(len(profile)), profile) ** 1.5
        else:
            target = np.ones(len(self.freqs))
        self._gains += 0.35 * (target - self._gains)
        return mag * self._gains, phase


class AudioDatamoshEffect(StftAudioEffect):
    description = ("Steht das Bild, friert der Klang ein. Bewegung zerreißt ihn: senkrecht verschiebt "
                   "die Tonhöhe, waagrecht verschmiert in Blöcken. Ein Bildschnitt ist ein Keyframe.")
    BLOCK_BINS = 16
    KEYFRAME_HOPS = 8

    @property
    def name(self) -> str:
        return "Audio-Datamosh"

    def on_reset(self) -> None:
        self._held: Optional[np.ndarray] = None
        self._phase = np.zeros(len(self.freqs))
        self._keyframe = 0
        self._last_cut: Optional[int] = None
        self._hops_since_key = 0

    def process_frame(self, mag, phase, video):
        if self._held is None:
            self._keyframe = self.KEYFRAME_HOPS
        if self._last_cut is not None and video.cut_count != self._last_cut:
            self._keyframe = self.KEYFRAME_HOPS
        self._last_cut = video.cut_count
        self._hops_since_key += 1
        if not video.valid and self._hops_since_key > 3 * self.hops_per_second:
            self._keyframe = self.KEYFRAME_HOPS     # ohne Video: alle 3 s ein Keyframe

        if self._keyframe > 0:
            self._keyframe -= 1
            self._hops_since_key = 0
            self._held = mag.copy()
            self._phase = phase.copy()
            return mag, phase

        held = self._held
        if video.valid:
            # Bild nach oben -> Ton höher; 1 % Bildhöhe pro Videobild ≈ 8 Halbtöne pro Sekunde
            semitones = -video.motion_y * 800.0 / self.hops_per_second
            if abs(semitones) > 0.005:
                ratio = 2.0 ** (semitones / 12.0)
                held = np.interp(self.bins / ratio, self.bins, held, left=0.0, right=0.0)
            smear = float(np.clip(abs(video.motion_x) * 12.0, 0, 1))
            if smear > 0.02:
                blended = held * (1 - smear) + mag * smear
                usable = len(blended) // self.BLOCK_BINS * self.BLOCK_BINS
                blocks = blended[:usable].reshape(-1, self.BLOCK_BINS).mean(axis=1).repeat(self.BLOCK_BINS)
                pick = self.rng.random(usable) < smear
                blended[:usable] = np.where(pick, blocks, blended[:usable])
                held = blended
        self._held = held * 0.9995
        self._phase = self._phase + self.bin_advance + self.rng.uniform(-0.2, 0.2, len(self._phase))
        return self._held, self._phase


class PremonitionEffect(IAudioEffect):
    description = ("Man hört jedes Wort leise rückwärts anschwellen, bevor es kommt – "
                   "ein Geist aus der Zukunft. Der Ton läuft dafür 1,5 s verzögert.")
    LOOKAHEAD = 1.5
    GHOST = 0.4
    GHOST_GAIN = 0.7

    @property
    def name(self) -> str:
        return "Vorahnung"

    @property
    def latency(self) -> int:
        return self._delay

    def reset(self, sample_rate: int) -> None:
        self._delay = int(self.LOOKAHEAD * sample_rate)
        self._ghost_len = int(self.GHOST * sample_rate)
        self._fifo = np.zeros(self._delay, dtype=np.float32)
        self._ghosts = np.zeros(self._delay + self._ghost_len, dtype=np.float32)
        self._in_count = 0
        self._out_count = 0
        self._pending: List[int] = []
        self._detector = OnsetDetector(sample_rate)
        self._envelope = (np.linspace(0, 1, self._ghost_len) ** 3).astype(np.float32)
        smooth = cv2.getGaussianKernel(31, 8).ravel().astype(np.float32)
        self._smooth = smooth / smooth.sum()

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        self._fifo = np.concatenate([self._fifo, block.astype(np.float32)])
        self._in_count += n
        self._pending.extend(self._detector.feed(block))

        fifo_start = self._in_count - len(self._fifo)
        still_pending = []
        for onset in self._pending:
            if self._in_count < onset + self._ghost_len:
                still_pending.append(onset)
                continue
            position = onset - fifo_start
            offset = onset + self._delay - self._ghost_len - self._out_count
            if position < 0 or offset < 0:
                continue
            segment = self._fifo[position:position + self._ghost_len]
            ghost = np.convolve(segment[::-1], self._smooth, mode='same') * self._envelope * self.GHOST_GAIN
            self._ghosts[offset:offset + len(ghost)] += ghost
        self._pending = still_pending

        out = self._fifo[:n] + self._ghosts[:n]
        self._fifo = self._fifo[n:]
        self._ghosts = np.concatenate([self._ghosts[n:], np.zeros(n, dtype=np.float32)])
        self._out_count += n
        return out


class PhosphorGlowEffect(StftAudioEffect):
    description = ("Laute Klanganteile glühen nach wie der Leuchtstoff einer Bildröhre und sinken beim "
                   "Abkühlen in der Tonhöhe. Helles Bild = längeres Nachleuchten.")
    COOLING_SEMITONES_PER_SECOND = 3.0

    @property
    def name(self) -> str:
        return "Phosphor-Nachleuchten"

    def on_reset(self) -> None:
        self._glow = np.zeros(len(self.freqs))
        self._phase = np.zeros(len(self.freqs))

    def process_frame(self, mag, phase, video):
        brightness = video.brightness if video.valid else 0.4
        seconds = 0.8 + 2.7 * brightness
        base = 0.001 ** (1.0 / (seconds * self.hops_per_second))
        decay = base ** (1.0 + self.freqs / 3000.0)          # Höhen kühlen schneller ab
        factor = 2.0 ** (-self.COOLING_SEMITONES_PER_SECOND / 12.0 / self.hops_per_second)
        cooled = np.interp(self.bins / factor, self.bins, self._glow, right=0.0)
        self._glow = np.maximum(cooled * decay, mag * 0.9)

        self._phase += self.bin_advance
        glowing = self._glow > mag
        self._phase = np.where(glowing, self._phase, phase)
        return np.maximum(mag, self._glow), self._phase


class ColorVowelEffect(StftAudioEffect):
    description = ("Die Farben im Bild bestimmen den Vokal: Rot 'a', Gelb 'e', Grün 'i', Blau 'o', Violett 'u'. "
                   "Auch Geräusche beginnen zu sprechen. Graues Bild = neutral.")
    # Farbton (Grad) -> Formanten F1, F2, F3 (Hz)
    VOWELS = [(0, (800, 1200, 2500)), (72, (500, 1900, 2500)), (144, (300, 2300, 3000)),
              (216, (500, 900, 2400)), (288, (320, 800, 2300)), (360, (800, 1200, 2500))]
    BANDWIDTHS = np.array([90.0, 110.0, 160.0])
    AMPLITUDES = np.array([1.0, 0.7, 0.35])

    @property
    def name(self) -> str:
        return "Farb-Vokale"

    def on_reset(self) -> None:
        self._formants = np.array(self.VOWELS[0][1], dtype=np.float64)
        self._lfo = 0.0

    def _target(self, hue: float) -> np.ndarray:
        hues = [h for h, _ in self.VOWELS]
        table = np.array([f for _, f in self.VOWELS], dtype=np.float64)
        return np.array([np.interp(hue % 360.0, hues, table[:, i]) for i in range(3)])

    def process_frame(self, mag, phase, video):
        if video.valid:
            hue, strength = video.hue, float(np.clip(video.saturation * 2.5, 0, 1))
        else:
            self._lfo = (self._lfo + 360.0 * 0.1 / self.hops_per_second) % 360.0
            hue, strength = self._lfo, 1.0
        self._formants += 0.08 * (self._target(hue) - self._formants)

        # Eigene Klangfarbe des Eingangs glätten, dann Vokal-Hüllkurve aufprägen
        envelope = np.exp(np.convolve(np.log(mag + 1e-6), np.ones(25) / 25, mode='same'))
        flat = mag / envelope
        vowel = 0.05 + np.sum(self.AMPLITUDES[:, None]
                              / (1.0 + ((self.freqs[None, :] - self._formants[:, None])
                                        / (self.BANDWIDTHS[:, None] / 2)) ** 2), axis=0)
        shaped = flat * vowel
        shaped *= np.sqrt(np.sum(mag ** 2) / (np.sum(shaped ** 2) + 1e-12))
        return mag * (1 - strength) + shaped * strength, phase


class NegativeSoundEffect(StftAudioEffect):
    description = ("Das akustische Negativ: Das Lauteste wird entfernt, das Leise verstärkt – Atem, Raum, "
                   "Nebengeräusche treten hervor. Dunkles Bild = stärker.")
    MAX_BOOST_DB = 24.0

    @property
    def name(self) -> str:
        return "Negativ-Ton"

    def on_reset(self) -> None:
        self._long_rms = 0.01
        self._gains = np.ones(len(self.freqs))
        self._makeup = 1.0

    def process_frame(self, mag, phase, video):
        rms = float(np.sqrt(np.mean(mag ** 2)))
        if rms < 1e-7:
            return mag, phase
        strength = 0.6 + 0.4 * (1.0 - video.brightness) if video.valid else 0.8

        reference = float(np.percentile(mag, 70)) + 1e-9
        over = mag / reference
        target = np.where(over > 1.0, over ** (-1.5 * strength), np.minimum(1.0 / (over + 0.25), 4.0) ** strength)
        self._gains += 0.5 * (target - self._gains)

        self._long_rms = 0.995 * self._long_rms + 0.005 * rms
        dynamic = float(np.clip((self._long_rms / rms) ** strength, 10 ** (-12 / 20), 10 ** (self.MAX_BOOST_DB / 20)))
        out = mag * self._gains * dynamic

        # Was übrig bleibt, hörbar machen: Pegel Richtung 70 % des Eingangs, höchstens +24 dB, geglättet
        out_rms = float(np.sqrt(np.mean(out ** 2))) + 1e-12
        wanted = float(np.clip(0.7 * rms / out_rms, 1.0, 10 ** (self.MAX_BOOST_DB / 20)))
        self._makeup += 0.05 * (wanted - self._makeup)
        return out * self._makeup, phase


class BeatQuantizerEffect(IAudioEffect):
    description = ("Silben und Geräusche rasten auf Achtelnoten ein – Sprache wird zu Sprechgesang. "
                   "Das Tempo folgt den Schnitten im Video (sonst 120 BPM).")
    MIN_DELAY = 0.06
    HISTORY = 1.5
    CROSSFADE = 256
    GATE_OPEN = 0.65      # Anteil des Rasterschritts, danach Staccato
    GATE_FLOOR = 0.15

    @property
    def name(self) -> str:
        return "Takt-Zwang"

    @property
    def latency(self) -> int:
        return int(self.MIN_DELAY * self.sample_rate)

    def reset(self, sample_rate: int) -> None:
        self.sample_rate = sample_rate
        self._ring = np.zeros(int(self.HISTORY * sample_rate), dtype=np.float32)
        self._count = 0                      # geschriebene = ausgegebene Samples (absolut)
        self._delay = float(self.MIN_DELAY * sample_rate)
        self._target_delay: Optional[float] = None
        self._fade = 0
        self._events: List[float] = [0.0]    # Ausgabezeitpunkte der eingerasteten Einsätze
        self._detector = OnsetDetector(sample_rate)
        self.bpm = 120.0

    def _update_tempo(self, video: VideoFeatureSnapshot) -> None:
        recent = [t for t in video.cut_times if time.time() - t < 12.0]
        if len(recent) >= 3:
            interval = float(np.median(np.diff(recent)))
            if interval > 0:
                bpm = 60.0 / interval
                while bpm < 80:
                    bpm *= 2
                while bpm > 160:
                    bpm /= 2
                self.bpm += 0.2 * (bpm - self.bpm)

    def _read(self, positions: np.ndarray) -> np.ndarray:
        index = np.floor(positions).astype(np.int64)
        return self._ring[index % len(self._ring)]

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        ring_len = len(self._ring)
        write = (self._count + np.arange(n)) % ring_len
        self._ring[write] = block
        self._update_tempo(video)
        step = 60.0 / self.bpm / 2.0 * self.sample_rate          # Achtelnote in Samples

        for onset in self._detector.feed(block):
            if self._target_delay is not None:                    # laufende Überblendung abschließen
                self._delay, self._target_delay = self._target_delay, None
            earliest = onset + self.MIN_DELAY * self.sample_rate
            grid = np.ceil(earliest / step) * step
            delay = grid - onset
            if delay < ring_len - n - self.CROSSFADE:
                self._target_delay = float(delay)
                self._fade = 0
                self._events.append(float(grid))
                self._events = self._events[-8:]

        out_positions = self._count + np.arange(n, dtype=np.float64)
        out = self._read(out_positions - self._delay)
        if self._target_delay is not None:
            fade = np.clip((np.arange(n) + self._fade) / self.CROSSFADE, 0, 1).astype(np.float32)
            out = out * (1 - fade) + self._read(out_positions - self._target_delay) * fade
            self._fade += n
            if self._fade >= self.CROSSFADE:
                self._delay, self._target_delay = self._target_delay, None

        # Staccato: nach dem Einsatz offen, dann bis zum nächsten Rasterpunkt abgedunkelt
        events = np.array(sorted(self._events))
        last = events[np.clip(np.searchsorted(events, out_positions, side='right') - 1, 0, len(events) - 1)]
        since = out_positions - last
        attack = np.clip(since / (0.005 * self.sample_rate), 0, 1)
        release = np.clip(1.0 - (since - self.GATE_OPEN * step) / (0.1 * step), 0, 1)
        gate = np.where(since < self.GATE_OPEN * step, attack, self.GATE_FLOOR + (1 - self.GATE_FLOOR) * release)
        self._count += n
        return (out * gate).astype(np.float32)


class HarmonyMagnetEffect(StftAudioEffect):
    description = ("Alle Frequenzen, auch Rauschen und Lärm, werden zu den Tönen eines Akkords gezogen. "
                   "Farbton wählt den Grundton, Helligkeit Dur (hell) oder Moll (dunkel).")
    FRAME = 4096
    HOP = 1024
    MAX_PEAKS = 80
    RESIDUAL = 0.12
    PROGRESSION = [(0, False), (9, True), (5, False), (7, False)]   # C, Am, F, G ohne Video

    @property
    def name(self) -> str:
        return "Harmonie-Magnet"

    def on_reset(self) -> None:
        self._prev_phase = np.zeros(len(self.freqs))
        self._out_phase = np.zeros(len(self.freqs))
        self._chord: Tuple[int, bool] = (0, False)
        self._chord_time = 0.0
        self._targets = self._chord_frequencies(*self._chord)
        self._progression_index = 0

    @staticmethod
    def _chord_frequencies(root: int, minor: bool) -> np.ndarray:
        intervals = (0, 3, 7) if minor else (0, 4, 7)
        notes = [m for m in range(28, 109) if (m - root) % 12 in intervals]   # ~41 Hz bis ~8 kHz
        return 440.0 * 2.0 ** ((np.array(notes) - 69) / 12.0)

    def _choose_chord(self, video: VideoFeatureSnapshot) -> None:
        now = time.time()
        if now - self._chord_time < 1.0:
            return
        if video.valid:
            chord = (int(round(video.hue / 30.0)) % 12, video.brightness < 0.45)
        elif now - self._chord_time >= 4.0:
            self._progression_index = (self._progression_index + 1) % len(self.PROGRESSION)
            chord = self.PROGRESSION[self._progression_index]
        else:
            return
        if chord != self._chord:
            self._chord = chord
            self._targets = self._chord_frequencies(*chord)
        self._chord_time = now

    def process_frame(self, mag, phase, video):
        self._choose_chord(video)
        n_bins = len(mag)
        bin_hz = self.sample_rate / self.FRAME

        # Momentanfrequenz jedes Bins aus der Phasendifferenz (Phase-Vocoder)
        deviation = phase - self._prev_phase - self.bin_advance
        deviation = (deviation + np.pi) % (2 * np.pi) - np.pi
        true_bins = self.bins + deviation * self.FRAME / (2 * np.pi * self.HOP)
        self._prev_phase = phase

        inner = mag[1:-1]
        candidates = np.where((inner > mag[:-2]) & (inner >= mag[2:]) & (inner > mag.max() * 0.01))[0] + 1
        if len(candidates) > self.MAX_PEAKS:
            candidates = candidates[np.argsort(mag[candidates])[-self.MAX_PEAKS:]]

        out_mag = mag * self.RESIDUAL
        out_phase = phase.copy()
        claimed = np.zeros(n_bins, dtype=bool)
        new_out_phase = phase.copy()
        for peak in candidates[np.argsort(-mag[candidates])]:
            freq = true_bins[peak] * bin_hz
            if freq <= 20:
                continue
            index = np.searchsorted(self._targets, freq)
            options = self._targets[max(0, index - 1):index + 1]
            target = options[np.argmin(np.abs(options - freq))]
            center = int(round(target / bin_hz))
            if center < 2 or center >= n_bins - 2 or claimed[center]:
                continue
            peak_phase = self._out_phase[center] + 2 * np.pi * target * self.HOP / self.sample_rate
            for offset in range(-2, 3):
                src, dst = peak + offset, center + offset
                if 0 <= src < n_bins:
                    if not claimed[dst]:
                        out_mag[dst] = 0.0
                    out_mag[dst] += mag[src]
                    out_phase[dst] = peak_phase + (phase[src] - phase[peak])
                    new_out_phase[dst] = out_phase[dst]
                    claimed[dst] = True
        self._out_phase = new_out_phase
        return out_mag, out_phase


class BlackHoleEffect(StftAudioEffect):
    description = ("Ein schwarzes Loch wandert durchs Spektrum: Frequenzen stürzen hinein, werden gedehnt, "
                   "rotverschoben und am Ereignishorizont verschluckt – nur Hawking-Rauschen entkommt. "
                   "Die hellste Bildzeile bestimmt, wo das Loch sitzt.")
    MIN_HZ, MAX_HZ = 120.0, 5000.0
    HORIZON_OCTAVES = 0.12

    @property
    def name(self) -> str:
        return "Schwarzes Loch"

    def on_reset(self) -> None:
        self._hole_hz = 600.0
        self._lfo = 0.0
        self._prev_mag = np.zeros(len(self.freqs))
        self._phase = np.zeros(len(self.freqs))
        self._octave = np.log2(np.maximum(self.freqs, 1.0))

    def _target_hole(self, video: VideoFeatureSnapshot) -> float:
        if video.valid and video.row_profile is not None:
            row = int(np.argmax(video.row_profile))
            position = 1.0 - row / (len(video.row_profile) - 1)       # oben = hoch
        else:
            self._lfo += 0.05 / self.hops_per_second
            position = 0.5 + 0.5 * np.sin(2 * np.pi * self._lfo)
        return self.MIN_HZ * (self.MAX_HZ / self.MIN_HZ) ** position

    def process_frame(self, mag, phase, video):
        self._hole_hz *= (self._target_hole(video) / self._hole_hz) ** 0.02   # träge Umlaufbahn
        distance = self._octave - np.log2(self._hole_hz)                        # in Oktaven
        pull = 0.85 / (1.0 + (np.abs(distance) / 0.6) ** 2)                     # Gravitation

        # Frequenzen fallen Richtung Loch (in Oktaven gestaucht) und verlieren Energie
        target_octave = self._octave - distance * pull
        target_bins = np.clip(np.round(2.0 ** target_octave * self.FRAME / self.sample_rate), 0, len(mag) - 1)
        pulled = np.zeros_like(mag)
        np.add.at(pulled, target_bins.astype(np.int64), mag * (1.0 - 0.6 * pull))

        # Ereignishorizont schluckt alles, Hawking-Strahlung entweicht als feines Rauschen
        horizon = np.abs(distance) < self.HORIZON_OCTAVES
        pulled[horizon] = 0.0
        rim = (np.abs(distance) >= self.HORIZON_OCTAVES) & (np.abs(distance) < 2 * self.HORIZON_OCTAVES)
        pulled[rim] += self.rng.random(int(rim.sum())) * mag.mean() * 0.8

        # Zeitdehnung ("Spaghettifizierung"): nahe am Loch verändert sich der Klang kaum noch
        inertia = np.clip(pull * 1.1, 0, 0.97)
        out = self._prev_mag * inertia + pulled * (1.0 - inertia)
        self._prev_mag = out

        self._phase += self.bin_advance
        near = pull > 0.3
        self._phase = np.where(near, self._phase, phase)
        return out, self._phase


class DreamMemoryEffect(StftAudioEffect):
    description = ("Der Ton wird laufend aus Schnipseln der letzten 30 Sekunden neu zusammengesetzt – jeweils aus dem "
                   "Moment, der am besten zur aktuellen Klangform passt. Dunkles Bild = ältere Erinnerungen.")
    MEMORY_SECONDS = 30.0
    BANDS = 40

    @property
    def name(self) -> str:
        return "Traum-Gedächtnis"

    def on_reset(self) -> None:
        size = int(self.MEMORY_SECONDS * self.hops_per_second)
        self._mags = np.zeros((size, len(self.freqs)), dtype=np.float16)
        self._envs = np.zeros((size, self.BANDS), dtype=np.float32)
        self._rms = np.zeros(size, dtype=np.float32)
        self._stored = 0
        self._write = 0
        self._last_match = -1
        self._phase = np.zeros(len(self.freqs))
        edges = np.geomspace(80, min(12000, self.sample_rate / 2 - 1), self.BANDS + 1)
        self._band_of_bin = np.clip(np.searchsorted(edges, self.freqs) - 1, 0, self.BANDS - 1)

    def _envelope(self, mag: np.ndarray) -> np.ndarray:
        energy = np.bincount(self._band_of_bin, weights=mag, minlength=self.BANDS)
        env = np.log1p(energy * 50.0)
        env -= env.mean()
        return (env / (np.linalg.norm(env) + 1e-9)).astype(np.float32)

    def process_frame(self, mag, phase, video):
        size = len(self._rms)
        env = self._envelope(mag)
        rms = float(np.sqrt(np.mean(mag ** 2)))

        brightness = video.brightness if video.valid else 0.6
        min_age = int((3.0 + (1.0 - brightness) * 20.0) * self.hops_per_second)
        out_mag, out_phase = mag, phase
        if self._stored > min_age + 10 and rms > 1e-6:
            ages = (self._write - np.arange(size) - 1) % size
            usable = (ages >= min_age) & (ages < self._stored)
            scores = self._envs @ env
            scores[~usable] = -np.inf
            if self._last_match >= 0:
                follow = (self._last_match + 1) % size
                if usable[follow]:
                    scores[follow] += 0.25              # Fortsetzung bevorzugen: längere, zusammenhängende Schnipsel
            best = int(np.argmax(scores))
            if np.isfinite(scores[best]):
                self._last_match = best
                memory = self._mags[best].astype(np.float64)
                out_mag = memory * (rms / (float(self._rms[best]) + 1e-9))
                self._phase += self.bin_advance + self.rng.uniform(-0.1, 0.1, len(self._phase))
                out_phase = self._phase

        self._mags[self._write] = mag
        self._envs[self._write] = env
        self._rms[self._write] = rms
        self._write = (self._write + 1) % size
        self._stored = min(self._stored + 1, size)
        return out_mag, out_phase


class GlassCrystalEffect(IAudioEffect):
    description = ("Jede Silbe gefriert zu einem gläsernen Arpeggio aus ihren eigenen Obertönen – Sprache wird zur "
                   "Glasharfe. Helles Bild = längeres Nachklingen.")
    ANALYSIS = 2048
    NOTES_PER_ONSET = 6
    NOTE_SPACING = 0.07
    DRY_LEVEL = 0.3
    MAX_VOICES = 64

    @property
    def name(self) -> str:
        return "Glas-Kristallisation"

    @property
    def latency(self) -> int:
        return 0

    def reset(self, sample_rate: int) -> None:
        self.sample_rate = sample_rate
        self._detector = OnsetDetector(sample_rate)
        self._history = np.zeros(sample_rate, dtype=np.float32)
        self._count = 0
        self._pending: List[int] = []
        self._voices: List[Dict[str, float]] = []
        self._window = np.hanning(self.ANALYSIS)

    def _analyse(self, onset: int, video: VideoFeatureSnapshot) -> None:
        start = onset - (self._count - len(self._history))
        frame = self._history[start:start + self.ANALYSIS]
        if len(frame) < self.ANALYSIS:
            return
        spectrum = np.abs(np.fft.rfft(frame * self._window))
        freqs = np.fft.rfftfreq(self.ANALYSIS, 1.0 / self.sample_rate)
        band = (freqs > 150) & (freqs < 5000)
        spectrum[~band] = 0
        inner = spectrum[1:-1]
        peaks = np.where((inner > spectrum[:-2]) & (inner >= spectrum[2:]))[0] + 1
        if len(peaks) == 0:
            return
        peaks = peaks[np.argsort(spectrum[peaks])[-self.NOTES_PER_ONSET:]]
        loudest = spectrum[peaks].max()
        decay = 0.6 + 2.0 * (video.brightness if video.valid else 0.5)
        for order, peak in enumerate(sorted(peaks)):
            a, b, c = spectrum[peak - 1], spectrum[peak], spectrum[peak + 1]
            offset = 0.5 * (a - c) / (a - 2 * b + c) if (a - 2 * b + c) != 0 else 0.0
            freq = (peak + offset) * self.sample_rate / self.ANALYSIS
            self._voices.append({
                'freq': freq * 2.0,                       # eine Oktave höher klingt gläsern
                'amp': 0.12 * float(spectrum[peak] / loudest),
                'delay': int(order * self.NOTE_SPACING * self.sample_rate),
                'age': 0, 'phase': 0.0, 'decay': decay,
            })
        self._voices = self._voices[-self.MAX_VOICES:]

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        self._history = np.concatenate([self._history[n:], block])
        self._count += n
        self._pending.extend(self._detector.feed(block))
        ready = [o for o in self._pending if self._count >= o + self.ANALYSIS]
        self._pending = [o for o in self._pending if self._count < o + self.ANALYSIS]
        for onset in ready:
            self._analyse(onset, video)

        out = block * self.DRY_LEVEL
        t = np.arange(n)
        alive = []
        for voice in self._voices:
            start = max(0, voice['delay'] - voice['age'])
            if start < n:
                local = t[start:] - start
                seconds = (voice['age'] + start - voice['delay'] + local) / self.sample_rate
                envelope = np.minimum(1.0, seconds / 0.003) * np.exp(-seconds / voice['decay'] * 3.0)
                omega = 2 * np.pi * voice['freq'] / self.sample_rate
                phases = voice['phase'] + omega * local
                # Glockenartig: Grundton plus unharmonischer Teilton
                tone = np.sin(phases) + 0.3 * np.sin(phases * 2.76)
                out[start:] += (voice['amp'] * envelope * tone).astype(np.float32)
                voice['phase'] = float(phases[-1] + omega) if len(phases) else voice['phase']
            voice['age'] += n
            if (voice['age'] - voice['delay']) / self.sample_rate < voice['decay'] * 2.5:
                alive.append(voice)
        self._voices = alive
        return out


class AntimatterEffect(IAudioEffect):
    description = ("Antimaterie: Die Lautstärkekurve jedes Klangs wird umgedreht, der Inhalt bleibt vorwärts. "
                   "Anschläge schwellen langsam an und brechen abrupt ab. 0,6 s Verzögerung.")
    LOOKAHEAD = 0.6
    MAX_SEGMENT = 0.5
    MAX_GAIN = 8.0

    @property
    def name(self) -> str:
        return "Antimaterie"

    @property
    def latency(self) -> int:
        return int(self.LOOKAHEAD * self.sample_rate)

    def reset(self, sample_rate: int) -> None:
        self.sample_rate = sample_rate
        self._detector = OnsetDetector(sample_rate, min_gap=0.08)
        self._segment = np.zeros(0, dtype=np.float32)
        self._segment_start = 0
        self._count = 0
        self._ready = np.zeros(self.latency, dtype=np.float32)
        self._window = max(8, int(0.01 * sample_rate))

    def _flush(self, length: int) -> None:
        segment, self._segment = self._segment[:length], self._segment[length:]
        self._segment_start += length
        if len(segment) == 0:
            return
        kernel = np.ones(min(self._window, len(segment))) / min(self._window, len(segment))
        envelope = np.sqrt(np.convolve(segment.astype(np.float64) ** 2, kernel, mode='same')) + 1e-4
        gain = np.minimum(envelope[::-1] / envelope, self.MAX_GAIN)
        gain = np.convolve(gain, kernel, mode='same')
        out = segment * gain
        peak = np.abs(out).max()
        limit = max(float(np.abs(segment).max()), 0.05) * 1.2
        if peak > limit:
            out *= limit / peak
        self._ready = np.concatenate([self._ready, out.astype(np.float32)])

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        self._segment = np.concatenate([self._segment, block])
        self._count += n
        for onset in self._detector.feed(block):
            if onset > self._segment_start:
                self._flush(onset - self._segment_start)
        max_len = int(self.MAX_SEGMENT * self.sample_rate)
        while len(self._segment) >= max_len:
            self._flush(max_len)

        if len(self._ready) < n:      # sollte nicht vorkommen; Sicherheitsnetz
            self._flush(len(self._segment))
        out, self._ready = self._ready[:n], self._ready[n:]
        if len(out) < n:
            out = np.pad(out, (0, n - len(out)))
        return out


class SwarmEffect(IAudioEffect):
    description = ("Hunderte Klangkörner fliegen als Vogelschwarm durch Zeit und Tonhöhe. Laut bündelt sich der "
                   "Schwarm zum Originalklang, leise zerstreut er sich zu zwitschernden Partikeln. Bewegung im Bild "
                   "macht ihn unruhig, ein Schnitt sprengt ihn auseinander.")
    BIRDS = 40
    GRAIN = 0.08
    HISTORY = 2.0
    MAX_AGO = 1.5

    @property
    def name(self) -> str:
        return "Klang-Schwarm"

    @property
    def latency(self) -> int:
        return 0

    def reset(self, sample_rate: int) -> None:
        self.sample_rate = sample_rate
        self.rng = np.random.default_rng()
        self._history = np.zeros(int(self.HISTORY * sample_rate), dtype=np.float32)
        self._count = 0
        self._grain_len = int(self.GRAIN * sample_rate)
        # Zustand pro Vogel: Position (Sekunden zurück, Halbtöne) und Geschwindigkeit
        self._pos = np.column_stack([self.rng.uniform(0, self.MAX_AGO, self.BIRDS),
                                     self.rng.uniform(-12, 12, self.BIRDS)])
        self._vel = self.rng.normal(0, 0.5, (self.BIRDS, 2))
        self._next_fire = self.rng.integers(0, self._grain_len // 2, self.BIRDS)
        self._grains = np.zeros((0, 3))                # Startposition im Verlauf, Rate, Alter
        self._level = 0.0
        self._peak = 0.05
        self._last_cut = None

    def _fly(self, dt: float, video: VideoFeatureSnapshot) -> None:
        rng = self.rng
        scale = np.array([1.0 / self.MAX_AGO, 1.0 / 12.0])       # beide Achsen auf ~1 normieren
        pos = self._pos * scale
        loudness = float(np.clip(self._level / self._peak, 0, 1))
        center = pos.mean(axis=0)
        cohesion = (center - pos) * (0.1 + 0.9 * loudness)                 # leise: Schwarm lockert sich
        diff = pos[:, None, :] - pos[None, :, :]
        dist = np.linalg.norm(diff, axis=2) + np.eye(self.BIRDS)
        separation = (diff / dist[..., None] ** 2 * (dist[..., None] < 0.15)).sum(axis=1) * 0.02
        alignment = (self._vel.mean(axis=0) - self._vel) * 0.5
        home = (np.array([0.0, 0.0]) - pos) * 6.0 * loudness ** 2          # laut: zurück zum Original
        agitation = 0.3 + 2.5 * (1.0 - loudness) + (video.activity * 20.0 if video.valid else 0.5)
        noise = rng.normal(0, agitation, self._vel.shape)
        if video.valid and self._last_cut is not None and video.cut_count != self._last_cut:
            noise += rng.normal(0, 25.0, self._vel.shape)                    # Schnitt sprengt den Schwarm
        self._last_cut = video.cut_count

        self._vel += (cohesion + separation + alignment + home + noise) * dt
        self._vel *= 0.96
        self._pos = (pos + self._vel * dt) / scale
        self._pos[:, 0] = np.clip(self._pos[:, 0], 0.0, self.MAX_AGO)
        self._pos[:, 1] = np.clip(self._pos[:, 1], -24.0, 24.0)

    def process(self, block: np.ndarray, video: VideoFeatureSnapshot) -> np.ndarray:
        n = len(block)
        ring = len(self._history)
        self._history[(self._count + np.arange(n)) % ring] = block
        self._count += n
        rms = float(np.sqrt(np.mean(block ** 2)))
        self._level = 0.8 * self._level + 0.2 * rms
        self._peak = max(0.02, self._peak * 0.9995, rms)
        self._fly(n / self.sample_rate, video)

        # Vögel feuern Körner (50 % Überlappung)
        self._next_fire -= n
        firing = np.where(self._next_fire <= 0)[0]
        if len(firing):
            ago = self._pos[firing, 0] * self.sample_rate + self._grain_len
            rates = 2.0 ** (self._pos[firing, 1] / 12.0)
            starts = self._count - ago
            new = np.column_stack([starts, rates, np.zeros(len(firing))])
            self._grains = np.vstack([self._grains, new])[-self.BIRDS * 3:]
            self._next_fire[firing] += self._grain_len // 2

        out = np.zeros(n, dtype=np.float32)
        if len(self._grains):
            starts, rates, ages = self._grains[:, 0], self._grains[:, 1], self._grains[:, 2]
            steps = ages[:, None] + np.arange(n)[None, :]
            read = starts[:, None] + steps * rates[:, None]
            index = np.floor(read).astype(np.int64)
            frac = (read - index).astype(np.float32)
            a = self._history[index % ring]
            b = self._history[(index + 1) % ring]
            samples = a + (b - a) * frac
            # nur schon geschriebene Samples lesen
            samples[read >= self._count - 1] = 0.0
            phase = np.clip(steps / self._grain_len, 0, 1)
            envelope = np.where(steps < self._grain_len, np.sin(np.pi * phase) ** 2, 0.0)
            out = (samples * envelope).sum(axis=0) / np.sqrt(max(1.0, self.BIRDS / 2))
            self._grains[:, 2] += n
            self._grains = self._grains[self._grains[:, 2] < self._grain_len]
        return out.astype(np.float32)


# ----------------- Ton-Analyse für Bild-Effekte -----------------

@dataclass(frozen=True)
class AudioFeatureSnapshot:
    valid: bool = False
    level: float = 0.0          # 0..1, automatisch normiert
    bass: float = 0.0
    mid: float = 0.0
    treble: float = 0.0
    onset_count: int = 0
    bands: Optional[np.ndarray] = None   # 32 Bänder, 0..1


class AudioAnalyzer:
    """
    Stufe der Audio-Pipeline, die nur misst. Läuft sie allein, bleibt der Ausgang
    stumm (keine Rückkopplung vom Mikrofon).
    """

    silent_if_alone = True
    BANDS = 32
    STALE_SECONDS = 0.5

    def __init__(self):
        self._snapshot = AudioFeatureSnapshot()
        self._timestamp = 0.0
        self.sample_rate = 48000
        self._prepare_bins(512)

    def _prepare_bins(self, block: int) -> None:
        self._block = block
        freqs = np.fft.rfftfreq(block, 1.0 / self.sample_rate)
        edges = np.geomspace(40, min(16000, self.sample_rate / 2 - 1), self.BANDS + 1)
        self._band_of_bin = np.clip(np.searchsorted(edges, freqs) - 1, 0, self.BANDS - 1)
        self._bass = freqs < 180
        self._mid = (freqs >= 180) & (freqs < 3000)
        self._treble = freqs >= 3000
        self._window = np.hanning(block).astype(np.float32)
        self._prev = np.zeros(len(freqs))
        self._peaks = np.full(4, 1e-3)
        self._band_peak = np.full(self.BANDS, 1e-3)
        self._flux_avg = 1.0
        self._onsets = 0
        self._samples = 0
        self._last_onset = -10 ** 9

    def prepare(self, sample_rate: int, channels: int) -> None:
        self.sample_rate = sample_rate
        self._prepare_bins(self._block)

    def process(self, block: np.ndarray) -> np.ndarray:
        mono = block.mean(axis=1)
        if len(mono) != self._block:
            self._prepare_bins(len(mono))
        mag = np.abs(np.fft.rfft(mono * self._window))
        values = np.array([np.sqrt(np.mean(mono ** 2)), mag[self._bass].sum(), mag[self._mid].sum(),
                           mag[self._treble].sum()])
        self._peaks = np.maximum(self._peaks * 0.999, values)            # automatische Pegelanpassung
        norm = np.clip(values / self._peaks, 0, 1)
        bands = np.bincount(self._band_of_bin, weights=mag, minlength=self.BANDS)
        self._band_peak = np.maximum(self._band_peak * 0.998, bands)
        flux = float(np.sum(np.maximum(0.0, np.log1p(mag * 10) - np.log1p(self._prev * 10))))
        self._prev = mag
        now = time.time()
        self._samples += len(mono)
        if (flux > 2.2 * self._flux_avg + 1.0 and values[0] > 0.003
                and self._samples - self._last_onset > 0.1 * self.sample_rate):
            self._onsets += 1
            self._last_onset = self._samples
        self._flux_avg = 0.9 * self._flux_avg + 0.1 * flux
        self._snapshot = AudioFeatureSnapshot(True, float(norm[0]), float(norm[1]), float(norm[2]),
                                              float(norm[3]), self._onsets,
                                              (bands / self._band_peak).astype(np.float32))
        self._timestamp = now
        return block

    def snapshot(self) -> AudioFeatureSnapshot:
        if time.time() - self._timestamp > self.STALE_SECONDS:
            return AudioFeatureSnapshot(onset_count=self._onsets)
        return self._snapshot

class AudioEffectEngine:
    """Ton-Effekt-Stufe der Audio-Pipeline. Umschalten wirkt beim nächsten Block im Audio-Thread."""

    def __init__(self, video_features: VideoFeatures):
        self.video_features = video_features
        self.effects: Dict[str, IAudioEffect] = {}
        self.mix = 1.0
        self.sample_rate = 48000
        self.load = 0.0                          # Anteil der Blockzeit, den der Effekt braucht
        self._effect: Optional[IAudioEffect] = None
        self._requested: Optional[Tuple[Optional[str]]] = None
        self._dry = np.zeros(0, dtype=np.float32)

    def register(self, effect: IAudioEffect) -> None:
        self.effects[effect.name] = effect

    @property
    def effect_names(self) -> List[str]:
        return list(self.effects)

    def select(self, name: Optional[str]) -> None:
        self._requested = (name,)

    def prepare(self, sample_rate: int, channels: int) -> None:
        self.sample_rate = sample_rate
        current = self._effect.name if self._effect is not None else None
        if self._requested is None:
            self._requested = (current,)

    def process(self, block: np.ndarray) -> np.ndarray:
        if self._requested is not None:
            (name,) = self._requested
            self._requested = None
            effect = self.effects.get(name) if name else None
            if effect is not None:
                effect.reset(self.sample_rate)
                self._dry = np.zeros(effect.latency, dtype=np.float32)
            self._effect = effect
        effect = self._effect
        if effect is None:
            return block

        started = time.perf_counter()
        n = len(block)
        mono = block.mean(axis=1).astype(np.float32)
        wet = effect.process(mono, self.video_features.snapshot())
        self._dry = np.concatenate([self._dry, mono])
        dry, self._dry = self._dry[:n], self._dry[n:]
        out = dry * (1.0 - self.mix) + wet * self.mix
        self.load = 0.9 * self.load + 0.1 * (time.perf_counter() - started) / (n / self.sample_rate)
        return np.repeat(out[:, None], block.shape[1], axis=1)


def list_audio_devices() -> Tuple[List[Tuple[int, str]], List[Tuple[int, str]]]:
    """(Eingänge, Ausgänge) als Listen von (Index, Name)"""
    inputs, outputs = [], []
    try:
        for index, device in enumerate(sd.query_devices()):
            if device['max_input_channels'] > 0:
                inputs.append((index, device['name']))
            if device['max_output_channels'] > 0:
                outputs.append((index, device['name']))
    except Exception:
        pass
    return inputs, outputs


class AudioController:
    """Bedient Ton-Effekte und Geräte im Hintergrund, damit Klicks nie warten"""

    def __init__(self, audio_loop: IAudioLoopService, engine: AudioEffectEngine,
                 event_bus: IEventBus, video_features: VideoFeatures):
        self.audio_loop = audio_loop
        self.engine = engine
        self.event_bus = event_bus
        self.video_features = video_features
        self._executor = ThreadPoolExecutor(max_workers=1, thread_name_prefix="audio-control")
        self.active: Optional[str] = None

    @property
    def effect_names(self) -> List[str]:
        return self.engine.effect_names

    def description(self, name: str) -> str:
        effect = self.engine.effects.get(name)
        return effect.description if effect is not None else ""

    def select_effect(self, name: Optional[str]) -> None:
        self.active = name
        self.engine.select(name)
        self.video_features.enabled = name is not None
        stage = self.engine if name else None
        self._executor.submit(self.audio_loop.set_stage, 'effect', stage)
        if name:
            self.event_bus.publish('status_update', f"Ton-Effekt: {name}")

    def set_mix(self, value: float) -> None:
        self.engine.mix = float(np.clip(value, 0.0, 1.0))

    def set_devices(self, input_device: Optional[int], output_device: Optional[int]) -> None:
        self._executor.submit(self.audio_loop.set_devices, input_device, output_device)

    def shutdown(self) -> None:
        self.video_features.enabled = False
        self._executor.submit(self.audio_loop.set_stage, 'effect', None)
        self._executor.shutdown(wait=True)

# ----------------- Zeitwelle (Slit-Scan) -----------------

class TimeWaveEffectStrategy(IEffectStrategy):
    """
    Jede Bildstelle zeigt einen anderen Moment der letzten 1,5 Sekunden.
    Die Zeitverteilung wechselt: Vorhang, Ringe, Wellen, Spirale.
    """

    HISTORY = 45
    MAX_PIXELS = 640 * 360        # größere Regionen werden verkleinert gepuffert
    MODES = ("Vorhang", "Ringe", "Wellen", "Spirale")

    def __init__(self):
        self.rng = np.random.default_rng()
        self.frame_counter = 0
        self.mode = 0
        self.frames_until_switch = 240
        self._buffer: Optional[np.ndarray] = None
        self._head = 0
        self._filled = 0
        self._grid = PixelGrid()

    @property
    def name(self) -> str:
        return "Zeitwelle"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        height, width = frame.shape[:2]
        scale = min(1.0, np.sqrt(self.MAX_PIXELS / (height * width)))
        size = (max(8, int(width * scale)), max(8, int(height * scale)))
        small = frame if scale >= 1.0 else cv2.resize(frame, size, interpolation=cv2.INTER_AREA)
        sh, sw = small.shape[:2]

        if self._buffer is None or self._buffer.shape[1:3] != (sh, sw):
            self._buffer = np.repeat(small[None], self.HISTORY, axis=0)
            self._head, self._filled = 0, 1
        self._head = (self._head + 1) % self.HISTORY
        self._buffer[self._head] = small
        self._filled = min(self._filled + 1, self.HISTORY)

        self.frame_counter += 1
        self.frames_until_switch -= 1
        if self.frames_until_switch <= 0:
            self.mode = (self.mode + 1) % len(self.MODES)
            self.frames_until_switch = int(self.rng.integers(200, 360))

        delay = self._delay_map(sh, sw)
        delay = np.clip(delay * (self._filled - 1), 0, self._filled - 1).astype(np.int64)
        index = (self._head - delay) % self.HISTORY
        ys = np.arange(sh)[:, None]
        xs = np.arange(sw)[None, :]
        result = self._buffer[index, ys, xs]
        if scale < 1.0:
            result = cv2.resize(result, (width, height), interpolation=cv2.INTER_LINEAR)
        return result

    def _delay_map(self, height: int, width: int) -> np.ndarray:
        """Verzögerung je Pixel, 0 = jetzt, 1 = älteste Aufnahme"""
        t = self.frame_counter * 0.03
        xs, ys = self._grid.get(height, width)
        nx, ny = xs / width, ys / height
        mode = self.MODES[self.mode]
        if mode == "Vorhang":
            return 0.5 + 0.5 * np.sin(2 * np.pi * (nx * 1.5 - t * 0.3))
        dx, dy = nx - 0.5, (ny - 0.5) * height / width
        radius = np.hypot(dx, dy)
        if mode == "Ringe":
            return 0.5 + 0.5 * np.sin(radius * 18 - t * 2)
        if mode == "Wellen":
            return 0.5 + 0.25 * np.sin(nx * 9 + t) + 0.25 * np.sin(ny * 7 - t * 1.3)
        return (np.arctan2(dy, dx) / (2 * np.pi) + 0.5 + radius * 2 - t * 0.2) % 1.0

# ----------------- Sand-Zerfall -----------------

class SandDisintegrationEffectStrategy(IEffectStrategy):
    """Bewegte Bildteile zerbröseln zu Sand, der herabrieselt und sich unten zu Dünen häuft"""

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

# ----------------- Droste-Spirale -----------------

class DrosteSpiralEffectStrategy(IEffectStrategy):
    """Das Bild enthält sich unendlich oft selbst – eine rotierende Escher-Spirale mit endlosem Zoom"""

    SCALE = 3.0

    def __init__(self):
        self.frame_counter = 0
        self._shape = None
        self._log_z: Optional[np.ndarray] = None

    @property
    def name(self) -> str:
        return "Droste-Spirale"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        height, width = frame.shape[:2]
        self.frame_counter += 1
        outer = 0.5 * min(width, height)
        log_scale = np.log(self.SCALE)
        inner_log = np.log(outer / self.SCALE)

        if self._shape != (height, width):
            xs, ys = np.meshgrid(np.arange(width, dtype=np.float32) - width / 2,
                                 np.arange(height, dtype=np.float32) - height / 2)
            z = (xs + 1j * ys).astype(np.complex64)
            z[np.abs(z) < 1e-3] = 1e-3
            # Ein Umlauf (Winkel + 2π) entspricht einer Vergrößerung um SCALE
            beta = np.complex64(1.0 - 1j * log_scale / (2 * np.pi))
            self._log_z = (np.log(z) * beta).astype(np.complex64)
            self._shape = (height, width)

        t = self.frame_counter
        radial = self._log_z.real - t * 0.012 * log_scale             # endloser Zoom
        angle = self._log_z.imag + t * 0.01                            # langsame Drehung
        radial = np.mod(radial - inner_log, log_scale) + inner_log     # in den Bildring zurückfalten
        r = np.exp(radial)
        map_x = (width / 2 + r * np.cos(angle)).astype(np.float32)
        map_y = (height / 2 + r * np.sin(angle)).astype(np.float32)
        return cv2.remap(frame, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)

# ----------------- Bio-Wucherung -----------------

class BioGrowthEffectStrategy(IEffectStrategy):
    """
    Reaktions-Diffusion (Gray-Scott): Ein chemisches Muster wächst wie Korallen
    oder Zellteilung aus den Kanten des Videos. Helle Bereiche nähren, dunkle töten.
    """

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

# ----------------- Klang-Beben (Ton steuert Bild) -----------------

class SoundQuakeEffectStrategy(IEffectStrategy):
    """
    Umgekehrte Kopplung: Der Ton steuert das Bild. Bässe lassen es pumpen und beben,
    Höhen spalten die Farben, jeder Einsatz zersplittert es in fliegende Scherben,
    ein Spektrum-Ring pulsiert. Ohne Ton schlägt ein 120-BPM-Herz.
    """

    TILES_X, TILES_Y = 6, 4

    def __init__(self, audio_loop_service: IAudioLoopService, analyzer: AudioAnalyzer):
        self.audio_loop = audio_loop_service
        self.analyzer = analyzer
        self.rng = np.random.default_rng()
        self.frame_counter = 0
        self._last_onsets = 0
        self._shatter = 0.0
        self._spin = np.zeros(self.TILES_X * self.TILES_Y)
        self._bass = 0.0

    @property
    def name(self) -> str:
        return "Klang-Beben"

    def activate(self) -> None:
        self.audio_loop.set_stage('analysis', self.analyzer)

    def deactivate(self) -> None:
        self.audio_loop.set_stage('analysis', None)

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        rng = self.rng
        height, width = frame.shape[:2]
        self.frame_counter += 1
        audio = self.analyzer.snapshot()
        if audio.valid:
            bass, treble, level, onsets, bands = audio.bass, audio.treble, audio.level, audio.onset_count, audio.bands
        else:
            beat = (self.frame_counter % 15) / 15.0                   # 120 BPM bei 30 fps
            bass = float(np.exp(-beat * 6))
            treble, level = 0.3 * bass, 0.5 * bass
            onsets = self.frame_counter // 60
            bands = (0.3 + 0.7 * bass * np.abs(np.sin(np.arange(AudioAnalyzer.BANDS) * 0.7 + self.frame_counter * 0.1))).astype(np.float32)

        self._bass = max(bass, self._bass * 0.8)
        if onsets != self._last_onsets:
            self._last_onsets = onsets
            self._shatter = 1.0
            self._spin = rng.uniform(-25, 25, len(self._spin))
        else:
            self._shatter *= 0.86

        # Bass: Zoom-Puls und Beben
        zoom = 1.0 + 0.12 * self._bass
        shake = rng.normal(0, 8 * self._bass, 2)
        matrix = cv2.getRotationMatrix2D((width / 2, height / 2), rng.normal(0, 1.5 * self._bass), zoom)
        matrix[:, 2] += shake
        result = cv2.warpAffine(frame, matrix, (width, height), borderMode=cv2.BORDER_REFLECT)

        # Höhen: Farbkanäle spalten
        split = int(14 * treble)
        if split:
            r, g, b = cv2.split(result)
            result = cv2.merge([shift_image(r, split, 0), g, shift_image(b, -split, 0)])

        # Einsätze: Scherben fliegen auseinander und kehren zurück
        if self._shatter > 0.03:
            result = self._shatter_tiles(result)

        # Lautstärke: Blitz
        if level > 0.85:
            result = cv2.addWeighted(result, 1.0, np.full_like(result, 255), 0.25 * (level - 0.85) / 0.15, 0)

        self._draw_ring(result, bands, level)
        return result

    def _shatter_tiles(self, image: np.ndarray) -> np.ndarray:
        height, width = image.shape[:2]
        canvas = np.zeros_like(image)
        tw, th = max(1, width // self.TILES_X), max(1, height // self.TILES_Y)
        amount = self._shatter
        for i in range(self.TILES_Y):
            for j in range(self.TILES_X):
                x0, y0 = j * tw, i * th
                x1 = width if j == self.TILES_X - 1 else x0 + tw
                y1 = height if i == self.TILES_Y - 1 else y0 + th
                tile = image[y0:y1, x0:x1]
                cx, cy = (x0 + x1) / 2 - width / 2, (y0 + y1) / 2 - height / 2
                dx = int(cx * 0.6 * amount)
                dy = int(cy * 0.6 * amount)
                rot = cv2.getRotationMatrix2D(((x1 - x0) / 2, (y1 - y0) / 2),
                                              self._spin[i * self.TILES_X + j] * amount, 1.0 - 0.15 * amount)
                tile = cv2.warpAffine(tile, rot, (x1 - x0, y1 - y0))
                tx0, ty0 = x0 + dx, y0 + dy
                sx0, sy0 = max(0, -tx0), max(0, -ty0)
                tx0c, ty0c = max(0, tx0), max(0, ty0)
                tx1c, ty1c = min(width, tx0 + (x1 - x0)), min(height, ty0 + (y1 - y0))
                if tx1c > tx0c and ty1c > ty0c:
                    part = tile[sy0:sy0 + (ty1c - ty0c), sx0:sx0 + (tx1c - tx0c)]
                    mask = part.any(axis=2, keepdims=True)
                    canvas[ty0c:ty1c, tx0c:tx1c] = np.where(mask, part, canvas[ty0c:ty1c, tx0c:tx1c])
        return canvas

    def _draw_ring(self, image: np.ndarray, bands: Optional[np.ndarray], level: float) -> None:
        if bands is None:
            return
        height, width = image.shape[:2]
        center = np.array([width / 2, height / 2])
        base = min(width, height) * 0.22
        values = np.concatenate([bands, bands[::-1]])                 # symmetrischer Ring
        angles = np.linspace(0, 2 * np.pi, len(values), endpoint=False) + self.frame_counter * 0.02
        radii = base + np.clip(values, 0, 1.5) * base * 0.6
        points = (center + np.column_stack([np.cos(angles), np.sin(angles)]) * radii[:, None]).astype(np.int32)
        hue = int((self.frame_counter * 2) % 180)
        color_hsv = np.uint8([[[hue, 255, 255]]])
        color = tuple(int(c) for c in cv2.cvtColor(color_hsv, cv2.COLOR_HSV2RGB)[0, 0])
        overlay = np.zeros_like(image)
        cv2.polylines(overlay, [points], True, color, 3, cv2.LINE_AA)
        glow = cv2.GaussianBlur(overlay, (0, 0), 5)
        cv2.add(image, glow, dst=image)
        cv2.add(image, overlay, dst=image)

# ----------------- Kern-Logik -----------------

class EffectProcessor:
    """Nimmt im Hintergrund-Thread auf, wendet den aktiven Effekt an und veröffentlicht die Bilder"""

    TARGET_FPS = 30
    ERROR_MESSAGE_INTERVAL = 2.0

    def __init__(self, capture_service: IScreenCaptureService, event_bus: IEventBus,
                 video_features: VideoFeatures):
        self.capture_service = capture_service
        self.event_bus = event_bus
        self.video_features = video_features
        self.effect_strategies: Dict[str, IEffectStrategy] = {}
        self.current_strategy: Optional[IEffectStrategy] = None
        self.config: Dict[str, Any] = {}
        self._lock = threading.Lock()
        self._thread: Optional[threading.Thread] = None
        self._stop_event = threading.Event()
        # activate()/deactivate() können dauern (Audiogerät öffnen): geordnet im Hintergrund ausführen
        self._lifecycle = ThreadPoolExecutor(max_workers=1, thread_name_prefix="effect-lifecycle")

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
                self._lifecycle.submit(old.deactivate)
            self._lifecycle.submit(new.activate)
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
            self._lifecycle.submit(self.current_strategy.activate)
        self.event_bus.publish('status_update',
                               f"Erfasse Region ({region['left']},{region['top']}) "
                               f"{region['width']}x{region['height']} px")

    def stop_capture(self, _=None, quiet: bool = False) -> None:
        self._stop_event.set()
        if self._thread is not None:
            self._thread.join(timeout=3.0)
        self._thread = None
        if self.current_strategy is not None:
            self._lifecycle.submit(self.current_strategy.deactivate)
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

                if self.video_features.enabled:
                    self.video_features.update(frame)

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

# ----------------- GUI (PySide6) -----------------

class UiBridge(QObject):
    """
    Signale aus dem Aufnahme-Thread. Qt stellt sie automatisch in die
    Ereignisschleife des GUI-Threads ein (queued connection).
    """
    status = Signal(str)
    fps = Signal(float)
    effect_changed = Signal(object)
    frame_ready = Signal()


class VideoWidget(QWidget):
    """
    Zeigt das neueste Bild seitenverhältnistreu an.
    Kommt die Anzeige nicht hinterher, werden ältere Bilder verworfen statt gestaut.
    """

    def __init__(self, parent: Optional[QWidget] = None):
        super().__init__(parent)
        self._lock = threading.Lock()
        self._pending: Optional[np.ndarray] = None
        self._image: Optional[QImage] = None
        self.frames_shown = 0
        self.setMinimumSize(320, 180)
        self.setAttribute(Qt.WidgetAttribute.WA_OpaquePaintEvent)

    def offer(self, frame: np.ndarray) -> bool:
        """Beliebiger Thread. True, wenn noch kein Bild auf Anzeige wartet (dann Signal senden)."""
        with self._lock:
            first = self._pending is None
            self._pending = frame
        return first

    def show_pending(self) -> None:
        """GUI-Thread"""
        with self._lock:
            frame, self._pending = self._pending, None
        if frame is None:
            return
        frame = np.ascontiguousarray(frame)
        height, width = frame.shape[:2]
        # copy(): QImage darf nicht auf den numpy-Puffer verweisen
        self._image = QImage(frame.data, width, height, 3 * width, QImage.Format.Format_RGB888).copy()
        self.frames_shown += 1
        self.update()

    def clear(self) -> None:
        with self._lock:
            self._pending = None
        self._image = None
        self.update()

    def paintEvent(self, event) -> None:
        painter = QPainter(self)
        painter.fillRect(self.rect(), QColor(14, 14, 18))
        if self._image is None:
            painter.setPen(QColor(110, 110, 120))
            painter.drawText(self.rect(), Qt.AlignmentFlag.AlignCenter, "Keine Vorschau – Bereich wählen und starten")
            return
        size = QSize(self._image.size())
        size.scale(self.size(), Qt.AspectRatioMode.KeepAspectRatio)
        target = QRect(QPoint(0, 0), size)
        target.moveCenter(self.rect().center())
        painter.setRenderHint(QPainter.RenderHint.SmoothPixmapTransform)
        painter.drawImage(target, self._image)


class RegionPicker(QWidget):
    """Abgedunkelte Fläche über dem Bildschirm: Bereich mit der Maus aufziehen"""

    region_selected = Signal(QRect)
    MIN_SIZE = 16

    def __init__(self):
        super().__init__(None, Qt.WindowType.FramelessWindowHint | Qt.WindowType.WindowStaysOnTopHint
                         | Qt.WindowType.Tool)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.setAttribute(Qt.WidgetAttribute.WA_DeleteOnClose)
        self.setCursor(Qt.CursorShape.CrossCursor)
        screen = QGuiApplication.screenAt(QCursor.pos()) or QGuiApplication.primaryScreen()
        self.setGeometry(screen.geometry())
        self._origin: Optional[QPoint] = None
        self._current: Optional[QPoint] = None

    def _selection(self) -> Optional[QRect]:
        if self._origin is None or self._current is None:
            return None
        return QRect(self._origin, self._current).normalized()

    def paintEvent(self, event) -> None:
        painter = QPainter(self)
        painter.fillRect(self.rect(), QColor(0, 0, 0, 110))
        selection = self._selection()
        if selection is not None:
            painter.setCompositionMode(QPainter.CompositionMode.CompositionMode_Clear)
            painter.fillRect(selection, Qt.GlobalColor.transparent)
            painter.setCompositionMode(QPainter.CompositionMode.CompositionMode_SourceOver)
            painter.setPen(QPen(QColor(255, 70, 70), 2))
            painter.drawRect(selection)
            painter.setPen(QColor(255, 255, 255))
            painter.drawText(selection.bottomLeft() + QPoint(4, 18), f"{selection.width()} × {selection.height()}")
        painter.setPen(QColor(255, 255, 255))
        font = painter.font()
        font.setPointSize(20)
        painter.setFont(font)
        painter.drawText(self.rect().adjusted(0, 30, 0, 0), Qt.AlignmentFlag.AlignHCenter,
                         "Bereich aufziehen  –  Esc oder Rechtsklick bricht ab")

    def showEvent(self, event) -> None:
        super().showEvent(event)
        self.raise_()
        self.activateWindow()

    def mousePressEvent(self, event) -> None:
        if event.button() != Qt.MouseButton.LeftButton:
            self.close()
            return
        self._origin = self._current = event.position().toPoint()
        self.update()

    def mouseMoveEvent(self, event) -> None:
        if self._origin is not None:
            self._current = event.position().toPoint()
            self.update()

    def mouseReleaseEvent(self, event) -> None:
        selection = self._selection()
        self.close()
        if selection is not None and selection.width() >= self.MIN_SIZE and selection.height() >= self.MIN_SIZE:
            # Fensterkoordinaten -> globale Bildschirmkoordinaten (logische Punkte wie bei mss)
            self.region_selected.emit(selection.translated(self.geometry().topLeft()))

    def keyPressEvent(self, event) -> None:
        if event.key() == Qt.Key.Key_Escape:
            self.close()


class EffectWindow(QMainWindow):
    """Hauptfenster. Alle Widgets werden nur im GUI-Thread angefasst."""

    MIN_REGION = 16

    def __init__(self, event_bus: IEventBus, effect_names: List[str], audio: AudioController):
        super().__init__()
        self.event_bus = event_bus
        self.audio = audio
        self._running = False
        self._last_fps: Optional[float] = None
        self._uses_cells = effect_names[:1] == ["Mosaik"]
        self._picker: Optional[RegionPicker] = None
        self.setWindowTitle("Bildeffekte")

        self.bridge = UiBridge()
        self._build_ui(effect_names)

        self.bridge.status.connect(self.status_label.setText)
        self.bridge.fps.connect(self._show_fps)
        self.bridge.effect_changed.connect(self._on_effect_changed)
        self.bridge.frame_ready.connect(self.video.show_pending)

        # Diese Callbacks laufen im Thread des Publishers und senden nur Signale
        event_bus.subscribe('status_update', self.bridge.status.emit)
        event_bus.subscribe('fps', self.bridge.fps.emit)
        event_bus.subscribe('effect_changed', self.bridge.effect_changed.emit)
        event_bus.subscribe('frame_processed', self._on_frame)

    def _build_ui(self, effect_names: List[str]) -> None:
        central = QWidget()
        layout = QVBoxLayout(central)
        layout.setContentsMargins(12, 12, 12, 6)
        layout.setSpacing(8)

        controls = QHBoxLayout()
        controls.addWidget(QLabel("Bereich:"))
        self.region_edit = QLineEdit("100,100,800,450")
        self.region_edit.setPlaceholderText("x, y, Breite, Höhe")
        self.region_edit.setFixedWidth(170)
        self.region_edit.returnPressed.connect(self.start_effect)
        controls.addWidget(self.region_edit)

        self.btn_pick = QPushButton("Bereich wählen…")
        self.btn_start = QPushButton("▶  Start")
        self.btn_next = QPushButton("Nächste Zelle")
        self.btn_stop = QPushButton("■  Stop")
        self.btn_start.setObjectName("primary")
        self.btn_start.setToolTip("Start/Stop: Leertaste")
        self.btn_next.setToolTip("Taste N")
        for button, slot in ((self.btn_pick, self.pick_region), (self.btn_start, self.start_effect),
                             (self.btn_next, self.next_cell), (self.btn_stop, self.stop_effect)):
            button.clicked.connect(slot)
            controls.addWidget(button)

        controls.addSpacing(16)
        controls.addWidget(QLabel("Effekt:"))
        self.effect_combo = QComboBox()
        self.effect_combo.addItems(effect_names)
        self.effect_combo.setMinimumWidth(200)
        self.effect_combo.currentTextChanged.connect(self.change_effect)
        controls.addWidget(self.effect_combo)
        controls.addStretch(1)
        layout.addLayout(controls)
        layout.addLayout(self._build_audio_controls())

        self.audio_info = QLabel("")
        self.audio_info.setObjectName("hint")
        self.audio_info.setWordWrap(True)
        layout.addWidget(self.audio_info)

        self.video = VideoWidget()
        layout.addWidget(self.video, 1)
        self.setCentralWidget(central)

        self.status_label = QLabel("Bereit. Bereich eingeben oder wählen, dann Start.")
        self.fps_label = QLabel("")
        self.fps_label.setMinimumWidth(150)
        self.fps_label.setAlignment(Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)
        self.statusBar().addWidget(self.status_label, 1)
        self.statusBar().addPermanentWidget(self.fps_label)

        self._load_timer = QTimer(self)
        self._load_timer.timeout.connect(self._refresh_meter)
        self._load_timer.start(1000)
        self._update_audio_info()

        QShortcut(QKeySequence(Qt.Key.Key_Space), self, activated=self.toggle_running)
        QShortcut(QKeySequence(Qt.Key.Key_N), self, activated=self.next_cell)
        self._update_buttons()

    def _build_audio_controls(self) -> QHBoxLayout:
        row = QHBoxLayout()
        row.addWidget(QLabel("Ton-Effekt:"))
        self.audio_combo = QComboBox()
        self.audio_combo.addItem("Aus")
        for name in self.audio.effect_names:
            self.audio_combo.addItem(name)
            self.audio_combo.setItemData(self.audio_combo.count() - 1, self.audio.description(name),
                                         Qt.ItemDataRole.ToolTipRole)
        self.audio_combo.setMinimumWidth(190)
        self.audio_combo.currentTextChanged.connect(self._on_audio_effect)
        row.addWidget(self.audio_combo)

        row.addWidget(QLabel("Mix:"))
        self.mix_slider = QSlider(Qt.Orientation.Horizontal)
        self.mix_slider.setRange(0, 100)
        self.mix_slider.setValue(100)
        self.mix_slider.setFixedWidth(110)
        self.mix_slider.setToolTip("0 % = Originalton, 100 % = nur Effekt")
        self.mix_label = QLabel("100 %")
        self.mix_label.setMinimumWidth(40)
        self.mix_slider.valueChanged.connect(self._on_mix)
        row.addWidget(self.mix_slider)
        row.addWidget(self.mix_label)

        row.addSpacing(12)
        inputs, outputs = list_audio_devices()
        row.addWidget(QLabel("Eingang:"))
        self.input_combo = self._device_combo(inputs)
        row.addWidget(self.input_combo)
        row.addWidget(QLabel("Ausgang:"))
        self.output_combo = self._device_combo(outputs)
        row.addWidget(self.output_combo)
        self.input_combo.currentIndexChanged.connect(self._on_devices)
        self.output_combo.currentIndexChanged.connect(self._on_devices)
        row.addStretch(1)
        return row

    @staticmethod
    def _device_combo(devices: List[Tuple[int, str]]) -> QComboBox:
        combo = QComboBox()
        combo.addItem("Systemstandard", None)
        for index, name in devices:
            combo.addItem(name, index)
        combo.setMinimumWidth(170)
        return combo

    def _on_audio_effect(self, name: str) -> None:
        self.audio.select_effect(None if name == "Aus" else name)
        self._update_audio_info()

    def _on_mix(self, value: int) -> None:
        self.mix_label.setText(f"{value} %")
        self.audio.set_mix(value / 100.0)

    def _on_devices(self) -> None:
        self.audio.set_devices(self.input_combo.currentData(), self.output_combo.currentData())
        self._update_audio_info()

    def _update_audio_info(self) -> None:
        name = self.audio_combo.currentText()
        if name != "Aus":
            text = self.audio.description(name)
            if not self._running:
                text += "  Tipp: Video starten, damit das Bild den Ton steuert."
        else:
            has_blackhole = any('blackhole' in self.input_combo.itemText(i).lower()
                                for i in range(self.input_combo.count()))
            text = ("Ton des Videos: Systemausgabe auf BlackHole stellen und hier BlackHole als Eingang wählen."
                    if has_blackhole else
                    "Ton kommt vom Mikrofon. Für den Ton des Videos BlackHole installieren: brew install blackhole-2ch")
        self.audio_info.setText(text)

    def _refresh_meter(self) -> None:
        """Rechts in der Statusleiste: Bildrate und Rechenlast des Ton-Effekts"""
        parts = []
        if self._running and self._last_fps is not None:
            parts.append(f"{self._last_fps:4.1f} FPS")
        if self.audio.active:
            parts.append(f"Ton {self.audio.engine.load * 100:3.0f} %")
        self.fps_label.setText("   ".join(parts))

    # --- Aufnahme-Thread ---

    def _on_frame(self, frame: np.ndarray) -> None:
        if self.video.offer(frame):
            self.bridge.frame_ready.emit()

    # --- Aktionen (GUI-Thread) ---

    def pick_region(self) -> None:
        self._picker = RegionPicker()
        self._picker.region_selected.connect(self._set_region)
        self._picker.show()

    def _set_region(self, rect: QRect) -> None:
        self.region_edit.setText(f"{rect.x()},{rect.y()},{rect.width()},{rect.height()}")
        self.status_label.setText("Bereich übernommen. Start klicken oder Leertaste.")

    def _parse_region(self) -> Optional[Dict[str, int]]:
        try:
            parts = [p.strip() for p in self.region_edit.text().split(',')]
            if len(parts) != 4:
                raise ValueError("Format muss sein: x,y,breite,höhe")
            x, y, width, height = map(int, parts)
            if width < self.MIN_REGION or height < self.MIN_REGION:
                raise ValueError(f"Breite und Höhe müssen mindestens {self.MIN_REGION} px sein")
        except ValueError as e:
            self.status_label.setText(f"Fehler: {e}")
            return None
        return {'left': x, 'top': y, 'width': width, 'height': height}

    def start_effect(self) -> None:
        region = self._parse_region()
        if region is None:
            return
        self._running = True
        self._update_buttons()
        self.event_bus.publish('start_capture', region)
        self._update_audio_info()

    def stop_effect(self) -> None:
        if not self._running:
            return
        self._running = False
        self._update_buttons()
        self.event_bus.publish('stop_capture')
        self._last_fps = None
        self._refresh_meter()

    def toggle_running(self) -> None:
        if self._running:
            self.stop_effect()
        else:
            self.start_effect()

    def next_cell(self) -> None:
        if self._running and self._uses_cells:
            self.event_bus.publish('next_cell')

    def change_effect(self, name: str) -> None:
        self.event_bus.publish('change_effect', name)

    def _on_effect_changed(self, effect: IEffectStrategy) -> None:
        self._update_audio_info()
        self._uses_cells = effect.uses_cells
        self._update_buttons()

    def _show_fps(self, fps: float) -> None:
        self._last_fps = fps
        self._refresh_meter()

    def _update_buttons(self) -> None:
        self.btn_start.setEnabled(not self._running)
        self.btn_stop.setEnabled(self._running)
        self.btn_next.setEnabled(self._running and self._uses_cells)
        self.region_edit.setEnabled(not self._running)
        self.btn_pick.setEnabled(not self._running)

    def closeEvent(self, event) -> None:
        self.event_bus.publish('stop_capture')
        self.audio.shutdown()
        super().closeEvent(event)


def apply_dark_theme(app: QApplication) -> None:
    app.setStyle("Fusion")
    palette = QPalette()
    colors = {
        QPalette.ColorRole.Window: QColor(32, 33, 38),
        QPalette.ColorRole.WindowText: QColor(230, 230, 235),
        QPalette.ColorRole.Base: QColor(22, 23, 27),
        QPalette.ColorRole.AlternateBase: QColor(38, 39, 45),
        QPalette.ColorRole.Text: QColor(230, 230, 235),
        QPalette.ColorRole.Button: QColor(48, 50, 58),
        QPalette.ColorRole.ButtonText: QColor(230, 230, 235),
        QPalette.ColorRole.Highlight: QColor(64, 132, 255),
        QPalette.ColorRole.HighlightedText: QColor(255, 255, 255),
        QPalette.ColorRole.ToolTipBase: QColor(48, 50, 58),
        QPalette.ColorRole.ToolTipText: QColor(230, 230, 235),
    }
    for role, color in colors.items():
        palette.setColor(role, color)
    for role in (QPalette.ColorRole.WindowText, QPalette.ColorRole.Text, QPalette.ColorRole.ButtonText):
        palette.setColor(QPalette.ColorGroup.Disabled, role, QColor(110, 110, 118))
    app.setPalette(palette)
    app.setStyleSheet("""
        QPushButton { padding: 6px 14px; border-radius: 6px; background: #30323a; border: 1px solid #41434d; }
        QPushButton:hover:enabled { background: #3a3d47; }
        QPushButton:disabled { color: #6e6e76; background: #2a2b31; border-color: #33343b; }
        QPushButton#primary:enabled { background: #2f6fea; border-color: #3d7df5; color: white; }
        QPushButton#primary:hover:enabled { background: #3d7df5; }
        QLineEdit, QComboBox { padding: 5px 8px; border-radius: 6px; background: #1a1b1f; border: 1px solid #41434d; }
        QStatusBar { color: #b8b8c0; }
        QLabel#hint { color: #8c8c96; }
    """)

# ----------------- Anwendungsstart -----------------

def build_processor(event_bus: IEventBus) -> EffectProcessor:
    processor = EffectProcessor(Container.resolve(IScreenCaptureService), event_bus,
                                Container.resolve(VideoFeatures))
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
        DatamoshEffectStrategy(),
        PixelSortEffectStrategy(),
        MatrixCodeEffectStrategy(),
        KaleidoscopeTunnelEffectStrategy(),
        TimeWaveEffectStrategy(),
        SandDisintegrationEffectStrategy(),
        DrosteSpiralEffectStrategy(),
        BioGrowthEffectStrategy(),
        SoundQuakeEffectStrategy(Container.resolve(IAudioLoopService), Container.resolve(AudioAnalyzer)),
    ):
        processor.register_effect(effect)
    return processor


def build_audio_controller() -> AudioController:
    engine: AudioEffectEngine = Container.resolve(AudioEffectEngine)
    for effect in (
        ImageStencilEffect(),
        AudioDatamoshEffect(),
        PremonitionEffect(),
        PhosphorGlowEffect(),
        ColorVowelEffect(),
        NegativeSoundEffect(),
        BeatQuantizerEffect(),
        HarmonyMagnetEffect(),
        BlackHoleEffect(),
        DreamMemoryEffect(),
        GlassCrystalEffect(),
        AntimatterEffect(),
        SwarmEffect(),
    ):
        engine.register(effect)
    return Container.resolve(AudioController)


def register_services() -> None:
    Container.register(IEventBus, EventBus)
    Container.register(IScreenCaptureService, MssScreenCaptureService)
    Container.register(IAudioLoopService, SoundDeviceLoopService)
    Container.register(VideoFeatures, VideoFeatures)
    Container.register(AudioAnalyzer, AudioAnalyzer)
    Container.register(AudioEffectEngine, AudioEffectEngine)
    Container.register(AudioController, AudioController)


def main():
    register_services()

    app = QApplication(sys.argv)
    app.setApplicationName("Bildeffekte")
    apply_dark_theme(app)

    event_bus = Container.resolve(IEventBus)
    processor = build_processor(event_bus)
    audio = build_audio_controller()
    window = EffectWindow(event_bus, processor.effect_names, audio)
    window.resize(1040, 700)
    window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
