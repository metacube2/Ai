#!/usr/bin/env python3
"""
Bildeffekte in Echtzeit auf einen Bildschirmbereich oder eine Kamera.

Ein wählbarer Bereich des Bildschirms (z. B. ein Videoplayer-Fenster) oder
eine Live-Kamera wird mit einem Effekt verändert und in der Vorschau angezeigt.
Unabhängig davon verändern Ton-Effekte den Ton – gesteuert vom Bild.

Ton des Videos statt Mikrofon: virtuelles Audiogerät BlackHole installieren
(brew install blackhole-2ch), Systemausgabe auf BlackHole stellen, im Programm
BlackHole als Eingang und die Lautsprecher als Ausgang wählen.

Alle Effekte sind Plugins: .py-Dateien im Ordner "plugins" neben diesem Programm
oder in ~/.bildeffekte/plugins, siehe plugins/README.md. Sie hängen nur am Paket
`bildeffekte` (Schnittstellen, Hilfsmittel, DI-Container); dieses Programm kennt
keine einzelne Effektklasse.

Abhängigkeiten: numpy, opencv-python(-headless), mss, PySide6, sounddevice, scipy
macOS: Das Terminal braucht die Freigabe "Bildschirmaufnahme", für den
Audio+Video-Glitch zusätzlich "Mikrofon".
"""

import collections
import importlib.util
import inspect
import os
import queue
import sys
import threading
import time
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime
from typing import Any, Callable, Dict, List, Optional, Sequence, Tuple

import cv2
import mss
import numpy as np
import sounddevice as sd

# Das API-Paket liegt neben diesem Programm
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from bildeffekte import (AUDIO_BANDS, PROFILE_ROWS, AudioFeatureSnapshot, Container, IAudioEffect,
                         IAudioFeatures, IAudioLoopService, IEffectStrategy, IEventBus, IScreenCaptureService,
                         IVideoFeatures, NoisePool, OnsetDetector, PixelGrid, PluginModule, StftAudioEffect,
                         VideoFeatureSnapshot, odd, rand_int, random_box, scale_image, shift_image,
                         to_rgb_colormap)
from PySide6.QtCore import QObject, QPoint, QRect, QSize, Qt, QTimer, Signal
from PySide6.QtGui import QColor, QCursor, QGuiApplication, QImage, QKeySequence, QPainter, QPalette, QPen, QShortcut
from PySide6.QtWidgets import (QApplication, QComboBox, QFileDialog, QGroupBox, QHBoxLayout, QLabel, QLineEdit, QMainWindow,
                               QMenu, QPushButton, QSizePolicy, QSlider, QVBoxLayout, QWidget)
from scipy import signal

# Alle Bilder in diesem Programm sind RGB (uint8, Form H x B x 3).

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


class OpenCvCameraCaptureService:
    """Kamera öffnen, lesen und freigeben, jeweils im Aufnahme-Thread."""

    def __init__(self, index: int):
        self.index = index
        self.camera: Optional[cv2.VideoCapture] = None

    def open(self) -> None:
        backend = cv2.CAP_AVFOUNDATION if sys.platform == 'darwin' else cv2.CAP_ANY
        camera = cv2.VideoCapture(self.index, backend)
        if not camera.isOpened():
            camera.release()
            raise RuntimeError(f"Kamera {self.index} konnte nicht geöffnet werden. Kamera-Freigabe prüfen.")
        self.camera = camera
        camera.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
        camera.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
        camera.set(cv2.CAP_PROP_FPS, 30)

    def capture(self) -> np.ndarray:
        if self.camera is None:
            raise RuntimeError("Kamera ist nicht geöffnet")
        ok, frame = self.camera.read()
        if not ok or frame is None:
            raise RuntimeError(f"Kamera {self.index} liefert kein Bild")
        return cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

    def close(self) -> None:
        if self.camera is not None:
            self.camera.release()
            self.camera = None


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
            if self._stages:
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


# ----------------- Bildmerkmale für Ton-Effekte -----------------

class VideoFeatures(IVideoFeatures):
    """
    Leichte Bildanalyse für die Ton-Effekte. update() läuft im Aufnahme-Thread
    (nur wenn ein Ton-Effekt aktiv ist), snapshot() liest im Audio-Thread.
    """

    ROWS = PROFILE_ROWS
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


# ----------------- Ton-Analyse für Bild-Effekte -----------------

class AudioAnalyzer(IAudioFeatures):
    """
    Stufe der Audio-Pipeline, die nur misst. Läuft sie allein, bleibt der Ausgang
    stumm (keine Rückkopplung vom Mikrofon).
    """

    silent_if_alone = True
    BANDS = AUDIO_BANDS
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

    def __init__(self, video_features: IVideoFeatures):
        self.video_features = video_features
        self.effects: Dict[str, IAudioEffect] = {}
        self.mix = 1.0
        self.sample_rate = 48000
        self.load = 0.0                          # Anteil der Blockzeit, den der Effekt braucht
        self._effect: Optional[IAudioEffect] = None
        self._requested: Optional[Tuple[Optional[str]]] = None
        self._dry = np.zeros((0, 1), dtype=np.float32)

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
        requested = self._requested
        if requested is not None:
            (name,) = requested
            self._requested = None
        else:
            name = self._effect.name if self._effect is not None else None
        # Neuladen ersetzt Instanzen unter demselben Namen. Erst im Audio-Thread
        # übernehmen und initialisieren, damit reset() nicht mit process() konkurriert.
        effect = self.effects.get(name) if name else None
        if effect is not None and (requested is not None or effect is not self._effect
                                   or self._dry.shape[1] != block.shape[1]):
            effect.reset(self.sample_rate)
            self._dry = np.zeros((effect.latency, block.shape[1]), dtype=np.float32)
        self._effect = effect
        if effect is None:
            return block

        started = time.perf_counter()
        n = len(block)
        mono = block.mean(axis=1).astype(np.float32)
        wet = effect.process(mono, self.video_features.snapshot())
        # Nur der Effekt bekommt Mono; der verzögerte Originalton behält alle Kanäle.
        self._dry = np.concatenate([self._dry, block])
        dry, self._dry = self._dry[:n], self._dry[n:]
        out = dry * (1.0 - self.mix) + wet[:, None] * self.mix
        self.load = 0.9 * self.load + 0.1 * (time.perf_counter() - started) / (n / self.sample_rate)
        return out


def list_cameras() -> List[str]:
    """
    Kameranamen in der Reihenfolge der OpenCV-Indizes (macOS). OpenCV zählt die
    AVFoundation-Geräte nach ihrer uniqueID sortiert, nicht in der Reihenfolge von
    macOS. Ohne pyobjc-framework-AVFoundation: leere Liste (Anzeige "Kamera N").
    """
    if sys.platform != 'darwin':
        return []
    try:
        import AVFoundation
        devices = AVFoundation.AVCaptureDevice.devicesWithMediaType_(AVFoundation.AVMediaTypeVideo)
        return [str(d.localizedName()) for d in sorted(devices, key=lambda d: str(d.uniqueID()))]
    except Exception:
        return []


def is_virtual_camera(name: str) -> bool:
    """Eigene Ausgabe als Quelle würde eine Rückkopplung erzeugen."""
    return 'virtual camera' in name.lower()


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
                 event_bus: IEventBus, video_features: IVideoFeatures):
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


# ----------------- Kern-Logik -----------------

class EffectProcessor:
    """Nimmt im Hintergrund-Thread auf, wendet den aktiven Effekt an und veröffentlicht die Bilder"""

    TARGET_FPS = 30
    ERROR_MESSAGE_INTERVAL = 2.0

    def __init__(self, capture_service: IScreenCaptureService, event_bus: IEventBus,
                 video_features: IVideoFeatures):
        self.capture_service = capture_service
        self.event_bus = event_bus
        self.video_features = video_features
        self.effect_strategies: Dict[str, IEffectStrategy] = {}
        # Effektkette: wird der Reihe nach angewendet. Die Liste wird nie verändert,
        # sondern ersetzt, damit der Aufnahme-Thread ohne Sperre lesen kann.
        self.chain: List[IEffectStrategy] = []
        self.selected = 0
        self.config: Dict[str, Any] = {}
        self._target_images = {}
        self._effect_options: Dict[str, Dict[str, Any]] = {}
        self._lock = threading.Lock()
        self._thread: Optional[threading.Thread] = None
        self._stop_event = threading.Event()
        # activate()/deactivate() können dauern (Audiogerät öffnen): geordnet im Hintergrund ausführen
        self._lifecycle = ThreadPoolExecutor(max_workers=1, thread_name_prefix="effect-lifecycle")

        self.event_bus.subscribe('start_capture', self.start_capture)
        self.event_bus.subscribe('stop_capture', self.stop_capture)
        self.event_bus.subscribe('next_cell', self.next_cell)
        self.event_bus.subscribe('change_effect', self.change_effect)
        self.event_bus.subscribe('warp_drag', self.set_warp_drag)
        self.event_bus.subscribe('target_image', self.set_target_image)
        self.event_bus.subscribe('effect_option', self.set_effect_option)
        self.event_bus.subscribe('add_effect', self.add_effect)
        self.event_bus.subscribe('remove_slot', self.remove_slot)
        self.event_bus.subscribe('move_slot', self.move_slot)
        self.event_bus.subscribe('select_slot', self.select_slot)

    @property
    def is_running(self) -> bool:
        return self._thread is not None and self._thread.is_alive()

    @property
    def effect_names(self) -> List[str]:
        return list(self.effect_strategies)

    @property
    def current_strategy(self) -> Optional[IEffectStrategy]:
        """Der ausgewählte Effekt der Kette; auf ihn beziehen sich Menü und Einstellungen."""
        chain = self.chain
        return chain[min(self.selected, len(chain) - 1)] if chain else None

    @property
    def chain_names(self) -> List[str]:
        return [effect.name for effect in self.chain]

    def register_effect(self, effect: IEffectStrategy) -> None:
        self.effect_strategies[effect.name] = effect
        if not self.chain:
            with self._lock:
                self.chain, self.selected = [effect], 0
            return
        for index, old in enumerate(self.chain):
            # Auch bei gleichem Namen den laufenden Effekt samt Lifecycle ersetzen.
            if old.name == effect.name and old is not effect:
                self._set_chain(self._replaced(index, effect), self.selected, stop=[old], start=[effect])

    def remove_effect(self, name: str) -> None:
        """Entfernt einen Effekt (Plugin neu geladen). Läuft er gerade, wird umgeschaltet."""
        effect = self.effect_strategies.pop(name, None)
        if effect is None or effect not in self.chain:
            return
        chain = [e for e in self.chain if e is not effect]
        start = []
        if not chain:
            fallback = next(iter(self.effect_strategies.values()), None)
            chain, start = ([fallback], [fallback]) if fallback is not None else ([], [])
        self._set_chain(chain, min(self.selected, max(0, len(chain) - 1)), stop=[effect], start=start)
        if start:
            self.event_bus.publish('status_update', f"Effekt gewechselt zu: {start[0].name}")

    def change_effect(self, effect_name: str) -> None:
        """Ersetzt den ausgewählten Effekt der Kette."""
        new = self.effect_strategies.get(effect_name)
        if new is None:
            return
        if new in self.chain:
            if self.chain.index(new) != self.selected:
                self.select_slot(self.chain.index(new))
            return
        if not self.chain:
            self._set_chain([new], 0, start=[new])
        else:
            old = self.chain[self.selected]
            self._set_chain(self._replaced(self.selected, new), self.selected, stop=[old], start=[new])
        self.event_bus.publish('status_update', f"Effekt gewechselt zu: {effect_name}")

    def add_effect(self, effect_name: str) -> None:
        """Hängt einen Effekt hinter dem ausgewählten an und wählt ihn aus."""
        new = self.effect_strategies.get(effect_name)
        if new is None:
            return
        if new in self.chain:
            # Ein Effekt hat Zustand (Tracking, Einfrieren): nur einmal in der Kette.
            self.select_slot(self.chain.index(new))
            self.event_bus.publish('status_update', f"„{effect_name}“ ist schon in der Kette.")
            return
        position = self.selected + 1 if self.chain else 0
        chain = self.chain[:position] + [new] + self.chain[position:]
        self._set_chain(chain, position, start=[new])
        self.event_bus.publish('status_update', "Kette: " + " → ".join(self.chain_names))

    def remove_slot(self, index: Optional[int] = None) -> None:
        """Nimmt einen Effekt aus der Kette; der letzte bleibt immer stehen."""
        index = self.selected if index is None else index
        if len(self.chain) <= 1 or not 0 <= index < len(self.chain):
            return
        effect = self.chain[index]
        chain = self.chain[:index] + self.chain[index + 1:]
        self._set_chain(chain, min(self.selected if self.selected < index else max(0, self.selected - 1),
                                   len(chain) - 1), stop=[effect])
        self.event_bus.publish('status_update', "Kette: " + " → ".join(self.chain_names))

    def move_slot(self, delta: int) -> None:
        """Verschiebt den ausgewählten Effekt in der Reihenfolge (-1 früher, +1 später)."""
        target = self.selected + delta
        if not self.chain or not 0 <= target < len(self.chain):
            return
        chain = list(self.chain)
        chain[self.selected], chain[target] = chain[target], chain[self.selected]
        self._set_chain(chain, target)

    def select_slot(self, index: int) -> None:
        if 0 <= index < len(self.chain) and index != self.selected:
            self._set_chain(self.chain, index)

    def _replaced(self, index: int, effect: IEffectStrategy) -> List[IEffectStrategy]:
        chain = list(self.chain)
        chain[index] = effect
        return chain

    def _set_chain(self, chain: List[IEffectStrategy], selected: int,
                   stop: Sequence[IEffectStrategy] = (), start: Sequence[IEffectStrategy] = ()) -> None:
        with self._lock:
            before = self.current_strategy
            self.chain, self.selected = chain, selected
        if self.is_running:
            for effect in stop:
                self._lifecycle.submit(effect.deactivate)
            for effect in start:
                self._lifecycle.submit(effect.activate)
        current = self.current_strategy
        if current is not None and (current is not before or start or stop):
            self.event_bus.publish('effect_changed', current)
        self.event_bus.publish('chain_changed', (self.chain_names, self.selected))

    def set_target_image(self, selection) -> None:
        name, image = selection
        with self._lock:
            targets = dict(self._target_images)
            if image is None:
                targets.pop(name, None)
            else:
                targets[name] = image
            self._target_images = targets
            self.config['target_images'] = targets

    def set_effect_option(self, option) -> None:
        """option: (Effektname, Schlüssel, Wert); der Effekt liest config['effect_options'][Name]."""
        name, key, value = option
        with self._lock:
            options = {effect: dict(values) for effect, values in self._effect_options.items()}
            options.setdefault(name, {})[key] = value
            self._effect_options = options
            self.config['effect_options'] = options

    def start_capture(self, source: Dict[str, Any]) -> None:
        if self.is_running:
            self.stop_capture(quiet=True)
        source = dict(source)
        kind = source.get('kind', 'screen')
        width = source['width'] if kind == 'screen' else 0
        height = source['height'] if kind == 'screen' else 0
        self.config = {'matrix_frames': [None] * 9, 'active_cell': 0, 'has_started': False,
                       'width': width, 'height': height, 'target_images': self._target_images,
                       'effect_options': self._effect_options}
        self._stop_event = threading.Event()
        self._thread = threading.Thread(target=self._capture_loop, args=(source, self._stop_event),
                                        daemon=True)
        if kind == 'screen':
            self.event_bus.publish('status_update',
                                   f"Erfasse Region ({source['left']},{source['top']}) "
                                   f"{width}x{height} px")
        else:
            self.event_bus.publish('status_update', f"Starte Kamera {source['index']} …")
        self._thread.start()
        for effect in self.chain:
            self._lifecycle.submit(effect.activate)

    def stop_capture(self, payload=None, quiet: bool = False) -> None:
        quiet = quiet or (isinstance(payload, dict) and payload.get('quiet', False))
        self._stop_event.set()
        if self._thread is not None:
            self._thread.join(timeout=3.0)
        self._thread = None
        for effect in self.chain:
            self._lifecycle.submit(effect.deactivate)
        if not quiet:
            self.event_bus.publish('status_update', "Gestoppt. Bereit für neuen Start.")

    def next_cell(self, _=None) -> None:
        if self.is_running and self.config.get('has_started'):
            self.config['active_cell'] = (self.config['active_cell'] + 1) % 9
            self.event_bus.publish('status_update', f"Aktive Zelle: {self.config['active_cell'] + 1} von 9")

    def set_warp_drag(self, drag: Optional[Tuple[float, float, float, float]]) -> None:
        self.config['warp_drag'] = drag

    def _capture_loop(self, source: Dict[str, Any], stop_event: threading.Event) -> None:
        interval = 1.0 / self.TARGET_FPS
        last_error_time = 0.0
        black_frames = 0
        read_errors = 0
        fps_frames, fps_start = 0, time.perf_counter()
        next_frame_time = time.perf_counter()
        camera = None

        try:
            if source.get('kind', 'screen') == 'camera':
                camera = OpenCvCameraCaptureService(source['index'])
                camera.open()
            while not stop_event.is_set():
                try:
                    frame = (camera.capture() if camera is not None
                             else self.capture_service.capture(source))
                    read_errors = 0
                except Exception as e:
                    self.event_bus.publish('status_update', f"Aufnahme-Fehler: {e}")
                    read_errors += 1
                    if camera is not None and read_errors >= 3:
                        break
                    stop_event.wait(1.0)
                    continue

                # Ohne Freigabe "Bildschirmaufnahme" liefert macOS schwarze Bilder
                if camera is None and black_frames >= 0:
                    black_frames = black_frames + 1 if frame.max() < 8 else -1
                    if black_frames == self.TARGET_FPS:
                        self.event_bus.publish('status_update',
                                               "Nur schwarzes Bild: Bildschirmaufnahme-Freigabe fürs Terminal prüfen "
                                               "(Systemeinstellungen > Datenschutz & Sicherheit)")
                        black_frames = -1

                if self.video_features.enabled:
                    self.video_features.update(frame)

                if camera is not None:
                    self.config['width'], self.config['height'] = frame.shape[1], frame.shape[0]

                output = frame
                for strategy in self.chain:
                    # Jeder Effekt bekommt das Ergebnis des vorherigen.
                    was_started = self.config.get('has_started')
                    try:
                        result = strategy.apply(output, self.config)
                        if result.shape[:2] != output.shape[:2]:
                            result = cv2.resize(result, (output.shape[1], output.shape[0]))
                        output = result
                    except Exception as e:
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
        except Exception as e:
            self.event_bus.publish('status_update', f"Aufnahme-Fehler: {e}")
        finally:
            if camera is not None:
                camera.close()
            else:
                self.capture_service.close()
            self.event_bus.publish('capture_finished', source.get('session'))

# ----------------- Plugins -----------------

def plugin_directories() -> List[str]:
    """Ordner, in denen nach Plugins gesucht wird"""
    here = os.path.dirname(os.path.abspath(__file__))
    return [os.path.join(here, "plugins"), os.path.expanduser("~/.bildeffekte/plugins")]


class PluginManager:
    """
    Lädt Effekte aus einzelnen .py-Dateien. Ein Plugin kennt nur das Paket
    `bildeffekte`, niemals dieses Programm.

    Registriert werden Klassen, die von IEffectStrategy oder IAudioEffect erben,
    sowie PluginModule-Klassen, die eigene Dienste eintragen – wie ein
    Autofac-Module. Jede Datei bekommt eine eigene Herkunft ("tag"), damit sie
    vollständig wieder entladen werden kann.
    """

    TAG_PREFIX = "plugin:"

    def __init__(self, event_bus: IEventBus):
        self.event_bus = event_bus
        self.errors: List[str] = []
        self.loaded: Dict[str, List[str]] = {}       # Datei -> registrierte Klassen
        self._counter = 0

    @property
    def tags(self) -> List[str]:
        return [self.TAG_PREFIX + path for path in self.loaded]

    def files(self) -> List[str]:
        found = []
        for directory in plugin_directories():
            if not os.path.isdir(directory):
                continue
            for entry in sorted(os.listdir(directory)):
                if entry.endswith('.py') and not entry.startswith('_'):
                    found.append(os.path.join(directory, entry))
        return found

    def load(self) -> None:
        """Alle Plugin-Dateien einlesen und im Container registrieren"""
        self.errors = []
        self.loaded = {}
        for path in self.files():
            tag = self.TAG_PREFIX + path
            Container.remove_tag(tag)
            try:
                module = self._import(path)
            except Exception as e:
                self.errors.append(f"{os.path.basename(path)}: {e}")
                continue
            self.loaded[path] = self._register(module, tag, path)
        self.event_bus.publish('status_update', self.summary())

    def unload(self) -> None:
        for tag in self.tags:
            Container.remove_tag(tag)
        self.loaded = {}

    def reload(self) -> None:
        self.unload()
        self.load()

    def summary(self) -> str:
        found = sum(len(names) for names in self.loaded.values())
        message = f"Plugins: {found} Effekte aus {len(self.loaded)} Datei(en)"
        if self.errors:
            message += "  |  Fehler: " + "; ".join(self.errors)
        return message

    def _import(self, path: str):
        self._counter += 1
        stem = os.path.splitext(os.path.basename(path))[0]
        name = f"bildeffekte_plugin_{stem}_{self._counter}"
        spec = importlib.util.spec_from_file_location(name, path)
        if spec is None or spec.loader is None:
            raise ImportError("Datei kann nicht geladen werden")
        module = importlib.util.module_from_spec(spec)
        sys.modules[name] = module
        try:
            spec.loader.exec_module(module)
        except Exception:
            sys.modules.pop(name, None)
            raise
        return module

    def _register(self, module, tag: str, path: str) -> List[str]:
        names = []
        for attribute, obj in vars(module).items():
            if (not inspect.isclass(obj) or obj.__module__ != module.__name__
                    or inspect.isabstract(obj) or attribute.startswith('_')):
                continue
            try:
                if issubclass(obj, PluginModule):
                    Container.create(obj).load(Container, tag)
                    names.append(attribute)
                elif issubclass(obj, (IEffectStrategy, IAudioEffect)):
                    # Gleich erzeugen: ein Fehler fällt hier auf und nicht erst im Betrieb
                    effect = Container.create(obj)
                    interface = IEffectStrategy if isinstance(effect, IEffectStrategy) else IAudioEffect
                    Container.register_instance_many(interface, effect, tag=tag)
                    names.append(effect.name)
            except Exception as e:
                self.errors.append(f"{os.path.basename(path)}/{attribute}: {e}")
        return names


def sync_effects(processor: EffectProcessor, engine: AudioEffectEngine,
                 audio: Optional[AudioController] = None) -> List[str]:
    """
    Gleicht die Effektlisten mit dem Container ab (nach dem Laden von Plugins).
    Rückgabe: Meldungen zu Einträgen, die sich nicht erzeugen ließen.
    """
    problems: List[str] = []

    def failed(registration, error) -> None:
        source = getattr(registration.implementation, '__name__', registration.implementation)
        problems.append(f"{source}: {error}")

    video = {effect.name: effect for effect in Container.resolve_all(IEffectStrategy, failed)}
    for name in list(processor.effect_strategies):
        if name not in video:
            processor.remove_effect(name)
    for effect in video.values():
        if processor.effect_strategies.get(effect.name) is not effect:
            processor.register_effect(effect)

    sounds = {effect.name: effect for effect in Container.resolve_all(IAudioEffect, failed)}
    for name in list(engine.effects):
        if name not in sounds:
            if audio is not None and audio.active == name:
                audio.select_effect(None)
            engine.effects.pop(name)
    for effect in sounds.values():
        if engine.effects.get(effect.name) is not effect:
            engine.register(effect)
    return problems


class VideoRecorder:
    """
    Speichert das Ausgabebild als MP4 (H.264, sonst MPEG-4), ohne Ton.
    Die Bildrate ist fest: Bilder werden nach der Uhr wiederholt oder ausgelassen,
    damit das Video in Echtzeit abläuft, auch wenn ein Effekt langsamer rechnet.
    Geschrieben wird in einem eigenen Thread, damit die Aufnahme nicht bremst.
    """

    FPS = 30
    CODECS = ('avc1', 'mp4v')

    def __init__(self, path: str):
        self.path = path
        self.frames = 0
        self.dropped = 0
        self.error: Optional[str] = None
        self.started = time.monotonic()
        self._queue: "queue.Queue" = queue.Queue(maxsize=self.FPS * 3)
        self._thread = threading.Thread(target=self._run, daemon=True, name='video-recorder')
        self._thread.start()

    def add(self, frame: np.ndarray) -> None:
        """Aus dem Aufnahme-Thread; blockiert nie."""
        try:
            self._queue.put_nowait((time.monotonic(), frame))
        except queue.Full:
            self.dropped += 1

    def stop(self) -> None:
        self._queue.put((None, None))
        self._thread.join(timeout=10)

    @property
    def seconds(self) -> float:
        return self.frames / self.FPS

    def _run(self) -> None:
        writer, size, start = None, None, None
        while True:
            stamp, frame = self._queue.get()
            if frame is None:
                break
            if self.error is not None:
                continue
            if writer is None:
                h, w = frame.shape[:2]
                size = (w - w % 2, h - h % 2)   # H.264 braucht gerade Kantenlängen
                for codec in self.CODECS:
                    writer = cv2.VideoWriter(self.path, cv2.VideoWriter_fourcc(*codec), self.FPS, size)
                    if writer.isOpened():
                        break
                    writer.release()
                    writer = None
                if writer is None:
                    self.error = "Videodatei konnte nicht angelegt werden"
                    continue
                start = stamp
            image = cv2.cvtColor(frame, cv2.COLOR_RGB2BGR)
            if (image.shape[1], image.shape[0]) != size:
                image = cv2.resize(image, size, interpolation=cv2.INTER_AREA)
            due = int((stamp - start) * self.FPS) + 1
            while self.frames < due:
                writer.write(image)
                self.frames += 1
        if writer is not None:
            writer.release()


class VirtualCamera:
    """
    Sendet das Ausgabebild an die "OBS Virtual Camera" (macOS: OBS ab Version 30,
    Kamera-Erweiterung einmal in OBS aktiviert). Teams, Zoom oder FaceTime wählen sie
    dann wie eine normale Kamera. Das Format ist fest 1280×720 bei 30 Bildern/s,
    andere Seitenverhältnisse bekommen schwarze Ränder. Ohne neues Bild wird das
    letzte wiederholt, damit die Kamera im Meeting nicht einfriert oder abbricht.
    """

    WIDTH, HEIGHT, FPS = 1280, 720, 30

    def __init__(self):
        import pyvirtualcam   # optional: nur nötig, wenn die Kamera eingeschaltet wird
        self._camera = pyvirtualcam.Camera(self.WIDTH, self.HEIGHT, self.FPS, backend='obs')
        self.device = self._camera.device
        self.error: Optional[str] = None
        self._frame = np.zeros((self.HEIGHT, self.WIDTH, 3), np.uint8)
        self._pending: Optional[np.ndarray] = None
        self._stop = threading.Event()
        self._thread = threading.Thread(target=self._run, daemon=True, name='virtual-camera')
        self._thread.start()

    def add(self, frame: np.ndarray) -> None:
        """Aus dem Aufnahme-Thread; blockiert nie, nur das neueste Bild zählt."""
        self._pending = frame

    def stop(self) -> None:
        self._stop.set()
        self._thread.join(timeout=3)

    @classmethod
    def fit(cls, frame: np.ndarray) -> np.ndarray:
        """Seitenverhältnis erhalten, mittig auf 1280×720 mit schwarzem Rand."""
        h, w = frame.shape[:2]
        scale = min(cls.WIDTH / w, cls.HEIGHT / h)
        size = (max(1, round(w * scale)), max(1, round(h * scale)))
        if (w, h) == (cls.WIDTH, cls.HEIGHT):
            return np.ascontiguousarray(frame[..., :3])
        image = cv2.resize(frame[..., :3], size,
                           interpolation=cv2.INTER_AREA if scale < 1 else cv2.INTER_LINEAR)
        out = np.zeros((cls.HEIGHT, cls.WIDTH, 3), np.uint8)
        x, y = (cls.WIDTH - size[0]) // 2, (cls.HEIGHT - size[1]) // 2
        out[y:y + size[1], x:x + size[0]] = image
        return out

    def _run(self) -> None:
        try:
            while not self._stop.is_set():
                frame, self._pending = self._pending, None
                if frame is not None:
                    self._frame = self.fit(frame)
                self._camera.send(self._frame)
                self._camera.sleep_until_next_frame()
        except Exception as e:
            self.error = str(e)
        finally:
            self._camera.close()


def recording_path(effect_name: Optional[str]) -> str:
    """~/Movies/Bildeffekte/Bildeffekte_<Datum_Uhrzeit>_<Effekt>.mp4"""
    folder = os.path.join(os.path.expanduser('~/Movies'), 'Bildeffekte')
    os.makedirs(folder, exist_ok=True)
    slug = ''.join(c if c.isalnum() else '-' for c in (effect_name or 'Original')).strip('-')
    while '--' in slug:
        slug = slug.replace('--', '-')
    return os.path.join(folder, f"Bildeffekte_{datetime.now():%Y-%m-%d_%H-%M-%S}_{slug}.mp4")


# ----------------- GUI (PySide6) -----------------

class UiBridge(QObject):
    """
    Signale aus dem Aufnahme-Thread. Qt stellt sie automatisch in die
    Ereignisschleife des GUI-Threads ein (queued connection).
    """
    status = Signal(str)
    fps = Signal(float)
    effect_changed = Signal(object)
    chain_changed = Signal(object)
    frame_ready = Signal()
    capture_finished = Signal(object)


class EffectPicker(QPushButton):
    """
    Effektauswahl als Menü statt langer Liste: Effekte ohne eigene Kategorie
    stehen oben, jede Kategorie ist ein Untermenü.
    """

    PLACEHOLDER = "— Effekt wählen —"
    effect_selected = Signal(str)

    def __init__(self, parent: Optional[QWidget] = None):
        super().__init__(self.PLACEHOLDER, parent)
        self._menu = QMenu(self)
        self._menu.setToolTipsVisible(True)
        self.setMenu(self._menu)
        self.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        self.setStyleSheet("text-align: left; padding-left: 8px;")
        self._actions: Dict[str, Any] = {}
        self._groups: Dict[str, Optional[str]] = {}
        self._current: Optional[str] = None

    def set_effects(self, groups: List[Tuple[Optional[str], List[Tuple[str, str]]]]) -> None:
        """groups: [(Kategorie oder None für oberste Ebene, [(Name, Beschreibung), ...]), ...]"""
        self._menu.clear()
        self._actions, self._groups = {}, {}
        for group, items in groups:
            if group is None:
                menu = self._menu
            else:
                menu = self._menu.addMenu(f"{self._escape(group)}  ({len(items)})")
                menu.setToolTipsVisible(True)
            for name, description in items:
                action = menu.addAction(self._escape(name))
                action.setCheckable(True)
                if description:
                    action.setToolTip(description)
                action.triggered.connect(lambda _=False, n=name: self._choose(n))
                self._actions[name] = action
                self._groups[name] = group
        self.set_current(self._current)

    def names(self) -> List[str]:
        return list(self._actions)

    def current(self) -> Optional[str]:
        return self._current

    def set_current(self, name: Optional[str]) -> None:
        """Zeigt die Auswahl an, ohne effect_selected auszulösen; unbekannt = keine Auswahl."""
        self._current = name if name in self._actions else None
        for action_name, action in self._actions.items():
            action.setChecked(action_name == self._current)
        if self._current is None:
            self.setText(self.PLACEHOLDER)
        else:
            group = self._groups[self._current]
            text = self._current if group is None else f"{group}  ›  {self._current}"
            self.setText(self._escape(text))

    @staticmethod
    def _escape(text: str) -> str:
        """Qt liest "&" in Menüs und Knöpfen als Tastenkürzel ("Retro & VHS" -> "Retro _VHS")."""
        return text.replace('&', '&&')

    def _choose(self, name: str) -> None:
        changed = name != self._current
        self.set_current(name)
        if changed:   # wie bei der Combobox: erneutes Wählen startet den Effekt nicht neu
            self.effect_selected.emit(name)


class VideoWidget(QWidget):
    """
    Zeigt das neueste Bild seitenverhältnistreu an.
    Kommt die Anzeige nicht hinterher, werden ältere Bilder verworfen statt gestaut.
    """

    warp_drag = Signal(object)
    fullscreen_requested = Signal()

    def __init__(self, parent: Optional[QWidget] = None):
        super().__init__(parent)
        self._lock = threading.Lock()
        self._pending: Optional[np.ndarray] = None
        self._image: Optional[QImage] = None
        self._drag_origin: Optional[Tuple[float, float]] = None
        self.drag_enabled = False
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

    def mirror_from(self, source: 'VideoWidget') -> None:
        """Dasselbe fertige QImage in einer zweiten Ansicht anzeigen (GUI-Thread)."""
        self._image = source._image
        self.update()

    def _image_rect(self) -> Optional[QRect]:
        if self._image is None:
            return None
        size = QSize(self._image.size())
        size.scale(self.size(), Qt.AspectRatioMode.KeepAspectRatio)
        target = QRect(QPoint(0, 0), size)
        target.moveCenter(self.rect().center())
        return target

    def _image_position(self, point: QPoint) -> Optional[Tuple[float, float]]:
        target = self._image_rect()
        if target is None or target.width() == 0 or target.height() == 0:
            return None
        return (float(np.clip((point.x() - target.x()) / target.width(), 0, 1)),
                float(np.clip((point.y() - target.y()) / target.height(), 0, 1)))

    def mousePressEvent(self, event) -> None:
        if not self.drag_enabled and event.button() == Qt.MouseButton.LeftButton:
            target = self._image_rect()
            if target is not None and target.contains(event.position().toPoint()):
                self.fullscreen_requested.emit()
            return
        if self.drag_enabled and event.button() == Qt.MouseButton.LeftButton:
            point = event.position().toPoint()
            target = self._image_rect()
            if target is not None and target.contains(point):
                self._drag_origin = self._image_position(point)

    def mouseDoubleClickEvent(self, event) -> None:
        if self.drag_enabled and event.button() == Qt.MouseButton.LeftButton:
            if self._drag_origin is not None:
                self._drag_origin = None
                self.warp_drag.emit(None)
            self.fullscreen_requested.emit()

    def mouseMoveEvent(self, event) -> None:
        if self._drag_origin is not None:
            point = self._image_position(event.position().toPoint())
            if point is not None:
                x, y = self._drag_origin
                self.warp_drag.emit((x, y, point[0] - x, point[1] - y))

    def mouseReleaseEvent(self, event) -> None:
        if self._drag_origin is not None:
            self._drag_origin = None
            self.warp_drag.emit(None)

    def paintEvent(self, event) -> None:
        painter = QPainter(self)
        painter.fillRect(self.rect(), QColor(14, 14, 18))
        if self._image is None:
            painter.setPen(QColor(110, 110, 120))
            painter.drawText(self.rect(), Qt.AlignmentFlag.AlignCenter, "Keine Vorschau – Bereich wählen und starten")
            return
        target = self._image_rect()
        painter.setRenderHint(QPainter.RenderHint.SmoothPixmapTransform)
        painter.drawImage(target, self._image)


class FullscreenOutput(QWidget):
    """Eigenes Fenster für das Ausgabebild; die Bedienung bleibt im Hauptfenster."""

    closed = Signal()

    def __init__(self):
        super().__init__()
        self.setWindowTitle("Bildeffekte – Vollbild")
        self.setAttribute(Qt.WidgetAttribute.WA_DeleteOnClose)
        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)
        self.video = VideoWidget(self)
        layout.addWidget(self.video)
        self.video.fullscreen_requested.connect(lambda: QTimer.singleShot(0, self.close))
        QShortcut(QKeySequence(Qt.Key.Key_Escape), self, activated=self.close)

    def closeEvent(self, event) -> None:
        self.closed.emit()
        super().closeEvent(event)


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

    def __init__(self, event_bus: IEventBus, effect_names: List[str], audio: AudioController,
                 plugins: Optional[PluginManager] = None, processor: Optional[EffectProcessor] = None):
        super().__init__()
        self.event_bus = event_bus
        self.audio = audio
        self.plugins = plugins
        self.processor = processor
        self._running = False
        self._session_id = 0
        self._last_fps: Optional[float] = None
        self._uses_cells = effect_names[:1] == ["Mosaik"]
        self._picker: Optional[RegionPicker] = None
        self._fullscreen: Optional[FullscreenOutput] = None
        self._target_loader = ThreadPoolExecutor(max_workers=1, thread_name_prefix='target-image')
        self._target_job = None
        self._target_names = {}
        self._target_errors = {}
        self.setWindowTitle("Bildeffekte")

        self.bridge = UiBridge()
        self._build_ui(effect_names)

        self.bridge.status.connect(self.status_label.setText)
        self.bridge.fps.connect(self._show_fps)
        self.bridge.effect_changed.connect(self._on_effect_changed)
        self.bridge.chain_changed.connect(self._on_chain_changed)
        self.bridge.frame_ready.connect(self._present_frame)
        self.bridge.capture_finished.connect(self._on_capture_finished)
        self.video.warp_drag.connect(lambda drag: self.event_bus.publish('warp_drag', drag))
        self.video.fullscreen_requested.connect(lambda: QTimer.singleShot(0, self.open_fullscreen))

        # Diese Callbacks laufen im Thread des Publishers und senden nur Signale
        event_bus.subscribe('status_update', self.bridge.status.emit)
        event_bus.subscribe('fps', self.bridge.fps.emit)
        event_bus.subscribe('effect_changed', self.bridge.effect_changed.emit)
        event_bus.subscribe('chain_changed', self.bridge.chain_changed.emit)
        event_bus.subscribe('frame_processed', self._on_frame)
        event_bus.subscribe('capture_finished', self.bridge.capture_finished.emit)
        self._refresh_target_controls()

    def _build_ui(self, effect_names: List[str]) -> None:
        central = QWidget()
        layout = QVBoxLayout(central)
        layout.setContentsMargins(12, 12, 12, 6)
        layout.setSpacing(8)

        source_row = QHBoxLayout()
        source_row.addWidget(QLabel("Quelle:"))
        self.source_combo = QComboBox()
        self.source_combo.addItem("Bildschirm", ('screen', None))
        self._camera_names = list_cameras()
        for index, name in enumerate(self._camera_names or [None] * 4):
            if name is None:
                label = f"Kamera {index}" + (" (Standard)" if index == 0 else "")
            elif is_virtual_camera(name):
                label = f"{name} (eigene Ausgabe, nicht als Quelle)"
            else:
                label = name
            self.source_combo.addItem(label, ('camera', index))
        self.source_combo.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        self.source_combo.currentIndexChanged.connect(self._on_source_changed)
        source_row.addWidget(self.source_combo, 1)
        layout.addLayout(source_row)

        region_row = QHBoxLayout()
        self.region_label = QLabel("Bereich:")
        region_row.addWidget(self.region_label)
        self.region_edit = QLineEdit("100,100,800,450")
        self.region_edit.setPlaceholderText("x, y, Breite, Höhe")
        self.region_edit.setMinimumWidth(120)
        self.region_edit.returnPressed.connect(self.start_effect)
        region_row.addWidget(self.region_edit, 1)

        self.btn_pick = QPushButton("Bereich wählen…")
        self.btn_pick.clicked.connect(self.pick_region)
        region_row.addWidget(self.btn_pick)
        layout.addLayout(region_row)

        transport_row = QHBoxLayout()
        self.btn_start = QPushButton("▶  Start")
        self.btn_next = QPushButton("Nächste Zelle")
        self.btn_stop = QPushButton("■  Stop")
        self.btn_start.setObjectName("primary")
        self.btn_start.setToolTip("Start/Stop: Leertaste")
        self.btn_next.setToolTip("Taste N")
        for button, slot in ((self.btn_start, self.start_effect),
                             (self.btn_next, self.next_cell), (self.btn_stop, self.stop_effect)):
            button.clicked.connect(slot)
            transport_row.addWidget(button)
        transport_row.addStretch(1)
        layout.addLayout(transport_row)

        effect_row = QHBoxLayout()
        effect_row.addWidget(QLabel("Effekt:"))
        self.effect_picker = EffectPicker()
        self.effect_picker.effect_selected.connect(lambda name: self._select_video_effect(name, self.contour_picker))
        effect_row.addWidget(self.effect_picker, 1)

        self.btn_plugins = QPushButton("🧩 Plugins neu laden")
        self.btn_plugins.setToolTip("Effekte aus dem Ordner 'plugins' neu einlesen")
        self.btn_plugins.clicked.connect(self.reload_plugins)
        self.btn_plugins.setEnabled(self.plugins is not None and self.processor is not None)
        effect_row.addWidget(self.btn_plugins)
        layout.addLayout(effect_row)

        # Effektkette: Chips in Reihenfolge der Anwendung; der markierte Chip ist
        # der Effekt, den das Menü oben und die Einstellungen darunter bearbeiten.
        chain_row = QHBoxLayout()
        chain_row.addWidget(QLabel("Kette:"))
        self.chain_chips = QHBoxLayout()
        self.chain_chips.setSpacing(4)
        chain_row.addLayout(self.chain_chips)
        chain_row.addStretch(1)
        self.btn_chain_left = QPushButton("◀")
        self.btn_chain_left.setToolTip("Ausgewählten Effekt früher anwenden")
        self.btn_chain_left.clicked.connect(lambda: self.event_bus.publish('move_slot', -1))
        self.btn_chain_right = QPushButton("▶")
        self.btn_chain_right.setToolTip("Ausgewählten Effekt später anwenden")
        self.btn_chain_right.clicked.connect(lambda: self.event_bus.publish('move_slot', 1))
        self.btn_chain_remove = QPushButton("✕")
        self.btn_chain_remove.setToolTip("Ausgewählten Effekt aus der Kette nehmen")
        self.btn_chain_remove.clicked.connect(lambda: self.event_bus.publish('remove_slot', None))
        self.add_picker = EffectPicker()
        self.add_picker.PLACEHOLDER = "＋ Effekt anhängen"
        self.add_picker.setToolTip("Weiteren Effekt hinter dem ausgewählten anwenden, z. B. VHS → Matrix-Agent")
        self.add_picker.setSizePolicy(QSizePolicy.Policy.Preferred, QSizePolicy.Policy.Fixed)
        self.add_picker.effect_selected.connect(self._append_video_effect)
        for button in (self.btn_chain_left, self.btn_chain_right, self.btn_chain_remove):
            button.setFixedWidth(36)
            chain_row.addWidget(button)
        chain_row.addWidget(self.add_picker)
        layout.addLayout(chain_row)

        self.target_group = QGroupBox("Gesichtsmorph")
        target_layout = QVBoxLayout(self.target_group)
        target_row = QHBoxLayout()
        self.btn_target = QPushButton("JPG wählen …")
        self.btn_target.clicked.connect(self._choose_target_image)
        self.btn_target_default = QPushButton("Standard-Agent")
        self.btn_target_default.clicked.connect(self._clear_target_image)
        target_row.addWidget(self.btn_target)
        target_row.addWidget(self.btn_target_default)
        target_row.addStretch(1)
        self.btn_drama = QPushButton("🎭  Dramatik")
        self.btn_drama.setCheckable(True)
        self.btn_drama.setToolTip("Verformung so stark wie in der Filmszene: Schwellungen wandern über das Gesicht, "
                                  "der Mund reißt auf, die Person wehrt sich. Umschalten startet die Übernahme neu.")
        self.btn_drama.toggled.connect(self._toggle_drama)
        target_row.addWidget(self.btn_drama)
        target_layout.addLayout(target_row)
        self._drama = {}
        self.target_label = QLabel("Standard-Agent")
        self.target_label.setWordWrap(True)
        self.target_label.setMinimumWidth(0)
        self.target_label.setSizePolicy(QSizePolicy.Policy.Ignored, QSizePolicy.Policy.Preferred)
        target_layout.addWidget(self.target_label)
        layout.addWidget(self.target_group)
        self.target_group.hide()
        self._target_timer = QTimer(self)
        self._target_timer.setInterval(100)
        self._target_timer.timeout.connect(self._poll_target_image)

        self.contour_group = QGroupBox("Kontur-Labor")
        contour_row = QHBoxLayout(self.contour_group)
        contour_row.addWidget(QLabel("Kontur-Effekt:"))
        self.contour_picker = EffectPicker()
        self.contour_picker.effect_selected.connect(lambda name: self._select_video_effect(name, self.effect_picker))
        contour_row.addWidget(self.contour_picker, 1)
        layout.addWidget(self.contour_group)
        self._populate_video_effects(effect_names)
        layout.addLayout(self._build_audio_controls())

        self.audio_info = QLabel("")
        self.audio_info.setObjectName("hint")
        self.audio_info.setWordWrap(True)
        layout.addWidget(self.audio_info)

        preview_row = QHBoxLayout()
        preview_row.addWidget(QLabel("Ausgabe"))
        preview_row.addStretch(1)
        self.btn_record = QPushButton("⏺  Aufnehmen")
        self.btn_record.setCheckable(True)
        self.btn_record.setToolTip("Ausgabebild als MP4 speichern (ohne Ton) nach ~/Movies/Bildeffekte. "
                                   "Nochmal klicken oder Stop beendet die Aufnahme.")
        self.btn_record.toggled.connect(self._toggle_recording)
        preview_row.addWidget(self.btn_record)
        self._recorder: Optional[VideoRecorder] = None
        self.btn_camera = QPushButton("📷  Als Kamera")
        self.btn_camera.setCheckable(True)
        self.btn_camera.setToolTip("Ausgabebild als virtuelle Kamera senden (OBS Virtual Camera). "
                                   "In Teams/Zoom unter Kamera \"OBS Virtual Camera\" wählen.")
        self.btn_camera.toggled.connect(self._toggle_virtual_camera)
        preview_row.addWidget(self.btn_camera)
        self._virtual_camera: Optional[VirtualCamera] = None
        self.btn_fullscreen = QPushButton("⛶  Vollbild")
        self.btn_fullscreen.setToolTip("Ausgabebild im Vollbild zeigen; Esc beendet das Vollbild")
        self.btn_fullscreen.clicked.connect(self.open_fullscreen)
        preview_row.addWidget(self.btn_fullscreen)
        layout.addLayout(preview_row)

        self.video = VideoWidget()
        layout.addWidget(self.video, 1)
        self.setCentralWidget(central)

        self.status_label = QLabel("Bereit. Bereich eingeben oder wählen, dann Start.")
        self.status_label.setMinimumWidth(0)
        self.status_label.setSizePolicy(QSizePolicy.Policy.Ignored, QSizePolicy.Policy.Preferred)
        self.fps_label = QLabel("")
        self.fps_label.setMinimumWidth(80)
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

    def _on_source_changed(self) -> None:
        is_screen = self.source_combo.currentData()[0] == 'screen'
        self.region_label.setVisible(is_screen)
        self.region_edit.setVisible(is_screen)
        self.btn_pick.setVisible(is_screen)

    def _populate_video_effects(self, names: List[str]) -> None:
        current = (self.processor.current_strategy.name if self.processor is not None
                   and self.processor.current_strategy is not None else names[0] if names else None)
        # Kategorie "Bild-Effekte" (Standard) = oberste Menüebene, sonst Untermenü in Reihenfolge des Auftretens
        regular: Dict[Optional[str], List[Tuple[str, str]]] = {None: []}
        contour: List[Tuple[str, str]] = []
        for name in names:
            effect = self.processor.effect_strategies.get(name) if self.processor is not None else None
            category = getattr(effect, 'category', 'Bild-Effekte')
            entry = (name, getattr(effect, 'description', '') or '')
            if category == 'Kontur-Labor':
                contour.append(entry)
            else:
                regular.setdefault(None if category == 'Bild-Effekte' else category, []).append(entry)
        self.effect_picker.set_effects(list(regular.items()))
        self.add_picker.set_effects(list(regular.items()) + ([('Kontur-Labor', contour)] if contour else []))
        self.add_picker.set_current(None)
        if self.processor is not None:
            self._on_chain_changed((self.processor.chain_names, self.processor.selected))
        self.contour_picker.set_effects([(None, contour)])
        self.contour_group.setVisible(bool(contour))
        self._sync_video_selections(current)

    def _sync_video_selections(self, name: Optional[str]) -> None:
        for picker in (self.effect_picker, self.contour_picker):
            picker.set_current(name)

    def _append_video_effect(self, name: str) -> None:
        self.add_picker.set_current(None)
        self.event_bus.publish('add_effect', name)

    def _on_chain_changed(self, state) -> None:
        names, selected = state
        while self.chain_chips.count():
            item = self.chain_chips.takeAt(0)
            if item.widget() is not None:
                item.widget().deleteLater()
        for index, name in enumerate(names):
            if index:
                self.chain_chips.addWidget(QLabel("→"))
            chip = QPushButton(EffectPicker._escape(name))
            chip.setCheckable(True)
            chip.setChecked(index == selected and len(names) > 1)
            chip.setToolTip("Auswählen: Menü und Einstellungen beziehen sich auf diesen Effekt")
            chip.clicked.connect(lambda _=False, i=index: self.event_bus.publish('select_slot', i))
            self.chain_chips.addWidget(chip)
        several = len(names) > 1
        self.btn_chain_left.setEnabled(several and selected > 0)
        self.btn_chain_right.setEnabled(several and selected < len(names) - 1)
        self.btn_chain_remove.setEnabled(several)

    def _select_video_effect(self, name: str, other: EffectPicker) -> None:
        other.set_current(None)
        self.change_effect(name)

    def _build_audio_controls(self) -> QVBoxLayout:
        controls = QVBoxLayout()
        effect_row = QHBoxLayout()
        effect_row.addWidget(QLabel("Ton-Effekt:"))
        self.audio_combo = QComboBox()
        self.audio_combo.addItem("Aus")
        for name in self.audio.effect_names:
            self.audio_combo.addItem(name)
            self.audio_combo.setItemData(self.audio_combo.count() - 1, self.audio.description(name),
                                         Qt.ItemDataRole.ToolTipRole)
        self.audio_combo.setSizeAdjustPolicy(QComboBox.SizeAdjustPolicy.AdjustToMinimumContentsLengthWithIcon)
        self.audio_combo.setMinimumContentsLength(10)
        self.audio_combo.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        self.audio_combo.currentTextChanged.connect(self._on_audio_effect)
        effect_row.addWidget(self.audio_combo, 1)
        controls.addLayout(effect_row)

        mix_row = QHBoxLayout()
        mix_row.addWidget(QLabel("Mix:"))
        self.mix_slider = QSlider(Qt.Orientation.Horizontal)
        self.mix_slider.setRange(0, 100)
        self.mix_slider.setValue(100)
        self.mix_slider.setToolTip("0 % = Originalton, 100 % = nur Effekt")
        self.mix_label = QLabel("100 %")
        self.mix_label.setMinimumWidth(40)
        self.mix_slider.valueChanged.connect(self._on_mix)
        mix_row.addWidget(self.mix_slider, 1)
        mix_row.addWidget(self.mix_label)
        controls.addLayout(mix_row)

        inputs, outputs = list_audio_devices()
        input_row = QHBoxLayout()
        input_row.addWidget(QLabel("Eingang:"))
        self.input_combo = self._device_combo(inputs)
        input_row.addWidget(self.input_combo, 1)
        controls.addLayout(input_row)

        output_row = QHBoxLayout()
        output_row.addWidget(QLabel("Ausgang:"))
        self.output_combo = self._device_combo(outputs)
        output_row.addWidget(self.output_combo, 1)
        controls.addLayout(output_row)
        self.input_combo.currentIndexChanged.connect(self._on_devices)
        self.output_combo.currentIndexChanged.connect(self._on_devices)
        return controls

    @staticmethod
    def _device_combo(devices: List[Tuple[int, str]]) -> QComboBox:
        combo = QComboBox()
        combo.addItem("Systemstandard", None)
        for index, name in devices:
            combo.addItem(name, index)
        combo.setSizeAdjustPolicy(QComboBox.SizeAdjustPolicy.AdjustToMinimumContentsLengthWithIcon)
        combo.setMinimumContentsLength(10)
        combo.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        return combo

    def reload_plugins(self) -> None:
        if self.plugins is None or self.processor is None:
            return
        self.plugins.reload()
        sync_effects(self.processor, self.audio.engine, self.audio)
        self._populate_video_effects(self.processor.effect_names)
        self._refresh_effect_list(self.audio_combo, ["Aus"] + self.audio.effect_names, tooltips=True)
        self._update_audio_info()

    def _refresh_effect_list(self, combo: QComboBox, names: List[str], tooltips: bool = False) -> None:
        current = combo.currentText()
        combo.blockSignals(True)
        combo.clear()
        combo.addItems(names)
        if tooltips:
            for i, name in enumerate(names):
                combo.setItemData(i, self.audio.description(name), Qt.ItemDataRole.ToolTipRole)
        index = combo.findText(current)
        combo.setCurrentIndex(max(0, index))
        combo.blockSignals(False)
        if index < 0:
            combo.currentTextChanged.emit(combo.currentText())   # Auswahl ist weg: neue übernehmen

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
        self._refresh_record_button()
        parts = []
        if self._running and self._last_fps is not None:
            parts.append(f"{self._last_fps:4.1f} FPS")
        if self.audio.active:
            parts.append(f"Ton {self.audio.engine.load * 100:3.0f} %")
        self.fps_label.setText("   ".join(parts))

    # --- Aufnahme-Thread ---

    def _on_frame(self, frame: np.ndarray) -> None:
        recorder = self._recorder
        if recorder is not None:
            recorder.add(frame)
        camera = self._virtual_camera
        if camera is not None:
            camera.add(frame)
        if self.video.offer(frame):
            self.bridge.frame_ready.emit()

    def _present_frame(self) -> None:
        self.video.show_pending()
        if self._fullscreen is not None:
            self._fullscreen.video.mirror_from(self.video)

    def open_fullscreen(self) -> None:
        if self._fullscreen is not None:
            self._fullscreen.raise_()
            self._fullscreen.activateWindow()
            return
        output = FullscreenOutput()
        output.setGeometry(self.geometry())
        output.video.mirror_from(self.video)
        output.video.drag_enabled = self.video.drag_enabled
        output.video.setCursor(self.video.cursor())
        output.video.setToolTip("Doppelklick oder Esc: Vollbild beenden" if output.video.drag_enabled
                                else "Klick oder Esc: Vollbild beenden")
        output.video.warp_drag.connect(lambda drag: self.event_bus.publish('warp_drag', drag))
        output.closed.connect(self._on_fullscreen_closed)
        self._fullscreen = output
        output.showFullScreen()

    def _on_fullscreen_closed(self) -> None:
        self._fullscreen = None

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
        kind, index = self.source_combo.currentData()
        if kind == 'screen':
            source = self._parse_region()
            if source is None:
                return
        else:
            if self._is_virtual_source(index):
                self.status_label.setText("Das ist die eigene Kamera-Ausgabe (OBS Virtual Camera). "
                                          "Als Quelle eine echte Kamera wählen.")
                return
            source = {'kind': 'camera', 'index': index}
        self._session_id += 1
        source['session'] = self._session_id
        self._running = True
        self._update_buttons()
        self.event_bus.publish('start_capture', source)
        self._update_audio_info()

    def _toggle_recording(self, enabled: bool) -> None:
        if enabled:
            name = "+".join(self.processor.chain_names) if self.processor and self.processor.chain else None
            try:
                self._recorder = VideoRecorder(recording_path(name))
            except OSError as e:
                self.status_label.setText(f"Aufnahme nicht möglich: {e}")
                self.btn_record.blockSignals(True)
                self.btn_record.setChecked(False)
                self.btn_record.blockSignals(False)
                return
            self.status_label.setText(f"Aufnahme läuft: {self._recorder.path}")
        else:
            self._finish_recording()
        self._refresh_record_button()

    def _is_virtual_source(self, index: Optional[int]) -> bool:
        names = self._camera_names
        return index is not None and index < len(names) and is_virtual_camera(names[index])

    def _toggle_virtual_camera(self, enabled: bool) -> None:
        if enabled:
            try:
                self._virtual_camera = VirtualCamera()
            except ImportError:
                message = ("Virtuelle Kamera braucht pyvirtualcam: "
                           "../venv/bin/python -m pip install pyvirtualcam")
            except Exception as e:
                message = (f"Virtuelle Kamera nicht verfügbar ({e}). OBS ab Version 30 installieren, "
                           "einmal starten und \"Virtuelle Kamera starten\" klicken, damit macOS die "
                           "Kamera-Erweiterung zulässt.")
            else:
                message = (f"Sende als Kamera: \"{self._virtual_camera.device}\" "
                           f"({VirtualCamera.WIDTH}×{VirtualCamera.HEIGHT}). In Teams unter "
                           "Einstellungen > Geräte > Kamera wählen.")
                self.btn_camera.setText("📷  Kamera an")
            if self._virtual_camera is None:
                self.btn_camera.blockSignals(True)
                self.btn_camera.setChecked(False)
                self.btn_camera.blockSignals(False)
            self.status_label.setText(message)
        else:
            self._stop_virtual_camera()

    def _stop_virtual_camera(self) -> None:
        camera, self._virtual_camera = self._virtual_camera, None
        if camera is None:
            return
        camera.stop()
        self.btn_camera.blockSignals(True)
        self.btn_camera.setChecked(False)
        self.btn_camera.blockSignals(False)
        self.btn_camera.setText("📷  Als Kamera")
        self.status_label.setText(f"Virtuelle Kamera beendet: {camera.error}" if camera.error
                                  else "Virtuelle Kamera beendet.")

    def _finish_recording(self) -> None:
        recorder, self._recorder = self._recorder, None
        if recorder is None:
            return
        recorder.stop()
        self.btn_record.blockSignals(True)
        self.btn_record.setChecked(False)
        self.btn_record.blockSignals(False)
        # Über den Event-Bus melden: so kommt die Meldung nach "Gestoppt" und wird nicht überschrieben
        if recorder.error:
            message = f"Aufnahme fehlgeschlagen: {recorder.error}"
        elif recorder.frames == 0:
            if os.path.exists(recorder.path):
                os.remove(recorder.path)
            message = "Aufnahme leer (kein Bild), nichts gespeichert."
        else:
            note = f", {recorder.dropped} Bilder übersprungen" if recorder.dropped else ""
            message = f"Gespeichert ({recorder.seconds:.1f} s{note}): {recorder.path}"
        self.event_bus.publish('status_update', message)
        self._refresh_record_button()

    def _refresh_record_button(self) -> None:
        recorder = self._recorder
        if recorder is None:
            self.btn_record.setText("⏺  Aufnehmen")
        else:
            elapsed = int(time.monotonic() - recorder.started)
            self.btn_record.setText(f"⏹  REC {elapsed // 60:02d}:{elapsed % 60:02d}")

    def stop_effect(self) -> None:
        if not self._running:
            self._finish_recording()
            return
        self._running = False
        self.event_bus.publish('stop_capture')
        self._finish_recording()   # nach dem Stopp: das letzte Bild ist mit drin
        self._update_buttons()
        self._last_fps = None
        self._refresh_meter()

    def _on_capture_finished(self, session: object) -> None:
        if self._running and session == self._session_id:
            self._finish_recording()
            self._running = False
            self.event_bus.publish('stop_capture', {'quiet': True})
            self._last_fps = None
            self._refresh_meter()
            self._update_buttons()

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

    def _refresh_target_controls(self) -> None:
        effect = self.processor.current_strategy if self.processor else None
        supported = getattr(effect, 'supports_target_image', False)
        self.target_group.setVisible(supported or getattr(effect, 'supports_drama', False))
        for widget in (self.btn_target, self.btn_target_default, self.target_label):
            widget.setVisible(supported)
        drama = getattr(effect, 'supports_drama', False)
        self.btn_drama.setVisible(drama)
        self.btn_drama.blockSignals(True)
        self.btn_drama.setChecked(drama and self._drama.get(effect.name, False))
        self.btn_drama.blockSignals(False)
        if supported:
            pending = self._target_job is not None and self._target_job[0] == effect.name
            text = ('Zielgesicht wird geprüft …' if pending else
                    self._target_errors.get(effect.name) or self._target_names.get(effect.name, 'Standard-Agent'))
            self.target_label.setText(text)
            self.target_label.setToolTip(text)

    def _toggle_drama(self, enabled: bool) -> None:
        effect = self.processor.current_strategy if self.processor else None
        if not getattr(effect, 'supports_drama', False):
            return
        self._drama[effect.name] = enabled
        self.event_bus.publish('effect_option', (effect.name, 'drama', enabled))
        self.event_bus.publish('status_update', "Dramatik-Modus " + ("an: Übernahme startet neu" if enabled else "aus"))

    def _choose_target_image(self) -> None:
        effect = self.processor.current_strategy if self.processor else None
        if not getattr(effect, 'supports_target_image', False):
            return
        path, _ = QFileDialog.getOpenFileName(self, 'Zielgesicht auswählen', '', 'JPEG-Bilder (*.jpg *.jpeg *.JPG *.JPEG)')
        if not path:
            return
        if self._target_job is not None:
            self._target_job[2].cancel()
        self._target_errors.pop(effect.name, None)
        future = self._target_loader.submit(effect.prepare_target_image, path)
        self._target_job = (effect.name, path, future)
        self._target_timer.start()
        self._refresh_target_controls()

    def _poll_target_image(self) -> None:
        if self._target_job is None or not self._target_job[2].done():
            return
        name, path, future = self._target_job
        self._target_job = None
        self._target_timer.stop()
        try:
            image = future.result()
        except Exception as exc:
            self._target_errors[name] = str(exc) + ' Bisheriges Ziel bleibt aktiv.'
        else:
            self._target_names[name] = os.path.basename(path)
            self._target_errors.pop(name, None)
            self.event_bus.publish('target_image', (name, image))
        self._refresh_target_controls()

    def _clear_target_image(self) -> None:
        effect = self.processor.current_strategy if self.processor else None
        if not getattr(effect, 'supports_target_image', False):
            return
        if self._target_job is not None and self._target_job[0] == effect.name:
            self._target_job[2].cancel()
            self._target_job = None
            self._target_timer.stop()
        self._target_names.pop(effect.name, None)
        self._target_errors.pop(effect.name, None)
        self.event_bus.publish('target_image', (effect.name, None))
        self._refresh_target_controls()

    def _on_effect_changed(self, effect: IEffectStrategy) -> None:
        self._refresh_target_controls()
        self._update_audio_info()
        self._sync_video_selections(effect.name)
        self.video.drag_enabled = effect.supports_drag
        self.video.setCursor(Qt.CursorShape.OpenHandCursor if effect.supports_drag else Qt.CursorShape.ArrowCursor)
        self.video.setToolTip("Zum Verformen im Bild ziehen" if effect.supports_drag else "")
        if self._fullscreen is not None:
            self._fullscreen.video.drag_enabled = effect.supports_drag
            self._fullscreen.video.setCursor(self.video.cursor())
            self._fullscreen.video.setToolTip("Doppelklick oder Esc: Vollbild beenden" if effect.supports_drag
                                             else "Klick oder Esc: Vollbild beenden")
        effects = self.processor.chain if self.processor else [effect]
        self._uses_cells = any(e.uses_cells for e in effects)
        self._update_buttons()

    def _show_fps(self, fps: float) -> None:
        self._last_fps = fps
        self._refresh_meter()

    def _update_buttons(self) -> None:
        self.btn_start.setEnabled(not self._running)
        self.btn_stop.setEnabled(self._running)
        self.btn_next.setEnabled(self._running and self._uses_cells)
        self.source_combo.setEnabled(not self._running)
        is_screen = self.source_combo.currentData()[0] == 'screen'
        self.region_edit.setEnabled(not self._running and is_screen)
        self.btn_pick.setEnabled(not self._running and is_screen)
        self.btn_record.setEnabled(self._running or self._recorder is not None)

    def closeEvent(self, event) -> None:
        self._finish_recording()
        self._stop_virtual_camera()
        self._target_timer.stop()
        self._target_loader.shutdown(wait=False, cancel_futures=True)
        if self._fullscreen is not None:
            self._fullscreen.close()
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
        QPushButton:checked { background: #7a2020; border-color: #d04040; color: white; }
        QLineEdit, QComboBox { padding: 5px 8px; border-radius: 6px; background: #1a1b1f; border: 1px solid #41434d; }
        QStatusBar { color: #b8b8c0; }
        QLabel#hint { color: #8c8c96; }
    """)

# ----------------- Anwendungsstart -----------------

class PassThroughEffectStrategy(IEffectStrategy):
    """Zeigt das Bild unverändert. Einziger fest eingebauter Effekt, damit das
    Programm auch ohne Plugins bedienbar bleibt."""

    @property
    def name(self) -> str:
        return "Original"

    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        return frame


def register_services() -> None:
    """Dienste eintragen. Alle Effekte kommen aus Plugins, siehe plugins/README.md."""
    Container.register(IEventBus, EventBus)
    Container.register(IScreenCaptureService, MssScreenCaptureService)
    Container.register(IAudioLoopService, SoundDeviceLoopService)
    Container.register(IVideoFeatures, VideoFeatures)
    Container.register(IAudioFeatures, AudioAnalyzer)
    Container.register(AudioEffectEngine, AudioEffectEngine)
    Container.register(AudioController, AudioController)
    Container.register_many(IEffectStrategy, PassThroughEffectStrategy)


def build_processor(event_bus: IEventBus) -> EffectProcessor:
    return EffectProcessor(Container.resolve(IScreenCaptureService), event_bus,
                           Container.resolve(IVideoFeatures))


def main():
    register_services()

    app = QApplication(sys.argv)
    app.setApplicationName("Bildeffekte")
    apply_dark_theme(app)

    event_bus = Container.resolve(IEventBus)
    plugins = PluginManager(event_bus)
    plugins.load()

    processor = build_processor(event_bus)
    audio: AudioController = Container.resolve(AudioController)
    sync_effects(processor, audio.engine, audio)

    window = EffectWindow(event_bus, processor.effect_names, audio, plugins, processor)
    window.resize(1040, 700)
    window.show()
    event_bus.publish('status_update', plugins.summary())
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
