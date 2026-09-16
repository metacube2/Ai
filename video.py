#!/usr/bin/env python3
"""
Bildeffekte in Echtzeit auf einen Bildschirmbereich.

Ein wählbarer Bereich des Bildschirms (z. B. ein Videoplayer-Fenster) wird
laufend abgefilmt, mit einem Effekt verändert und in der Vorschau angezeigt.
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
import sys
import threading
import time
from concurrent.futures import ThreadPoolExecutor
from typing import Any, Callable, Dict, List, Optional, Tuple

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
from PySide6.QtWidgets import (QApplication, QComboBox, QHBoxLayout, QLabel, QLineEdit, QMainWindow,
                               QPushButton, QSlider, QVBoxLayout, QWidget)
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

    def remove_effect(self, name: str) -> None:
        """Entfernt einen Effekt (Plugin neu geladen). Läuft er gerade, wird umgeschaltet."""
        effect = self.effect_strategies.pop(name, None)
        if effect is None:
            return
        if self.current_strategy is effect:
            self.current_strategy = None
            fallback = next(iter(self.effect_strategies.values()), None)
            if fallback is not None:
                self.change_effect(fallback.name)
            if self.is_running:
                self._lifecycle.submit(effect.deactivate)

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

    def __init__(self, event_bus: IEventBus, effect_names: List[str], audio: AudioController,
                 plugins: Optional[PluginManager] = None, processor: Optional[EffectProcessor] = None):
        super().__init__()
        self.event_bus = event_bus
        self.audio = audio
        self.plugins = plugins
        self.processor = processor
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

        self.btn_plugins = QPushButton("🧩 Plugins neu laden")
        self.btn_plugins.setToolTip("Effekte aus dem Ordner 'plugins' neu einlesen")
        self.btn_plugins.clicked.connect(self.reload_plugins)
        self.btn_plugins.setEnabled(self.plugins is not None and self.processor is not None)
        controls.addWidget(self.btn_plugins)
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

    def reload_plugins(self) -> None:
        if self.plugins is None or self.processor is None:
            return
        self.plugins.reload()
        sync_effects(self.processor, self.audio.engine, self.audio)
        self._refresh_effect_list(self.effect_combo, self.processor.effect_names)
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
