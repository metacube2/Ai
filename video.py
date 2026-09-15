import cv2
import sounddevice as sd
import scipy.signal as signal
import queue
import numpy as np
import tkinter as tk
import mss
import collections
import mss.tools
from PIL import Image, ImageTk
import time
import threading
from abc import ABC, abstractmethod
from typing import Dict, List, Any, Callable, Type

# ----------------- Dependency Injection Container -----------------

class Container:
    """Einfacher Dependency Injection Container (ähnlich zu Autofac)"""
    _instances = {}
    _registrations = {}

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
        
        if singleton:
            if interface not in cls._instances:
                cls._instances[interface] = cls._create_instance(implementation)
            return cls._instances[interface]
        else:
            return cls._create_instance(implementation)
    
    @classmethod
    def _create_instance(cls, implementation: Type) -> Any:
        """Erstellt eine Instanz mit automatischer Dependency Injection"""
        import inspect
        constructor = inspect.signature(implementation.__init__)
        args = {}
        
        for param_name, param in list(constructor.parameters.items())[1:]:  # Skip 'self'
            if param.annotation != inspect._empty:
                args[param_name] = cls.resolve(param.annotation)
        
        return implementation(**args)

# ----------------- Interfaces -----------------

class IScreenCaptureService(ABC):
    """Interface für den Bildschirmaufnahme-Service"""
    
    @abstractmethod
    def capture(self, region: Dict[str, int]) -> np.ndarray:
        """Nimmt einen Screenshot der angegebenen Region auf"""
        pass

class IEffectStrategy(ABC):
    """Interface für Bildeffekt-Strategien (Strategie-Pattern)"""
    
    @abstractmethod
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet einen Effekt auf ein Bild an"""
        pass
    
    @property
    @abstractmethod
    def name(self) -> str:
        """Name des Effekts"""
        pass

class IEventBus(ABC):
    """Interface für den Event Bus"""
    
    @abstractmethod
    def subscribe(self, event_name: str, callback: Callable) -> None:
        """Abonniert ein Event"""
        pass
    
    @abstractmethod
    def publish(self, event_name: str, data: Any = None) -> None:
        """Veröffentlicht ein Event"""
        pass






# -------------- Audio-Abstraktion und -Implementierungen --------------

class IAudioCaptureService(ABC):
    """Interface für Audio-Aufnahmedienste"""
    
    @abstractmethod
    def start_capture(self) -> None:
        """Startet die Audio-Aufnahme"""
        pass
    
    @abstractmethod
    def stop_capture(self) -> None:
        """Stoppt die Audio-Aufnahme"""
        pass
    
    @abstractmethod
    def get_audio_chunk(self) -> np.ndarray:
        """Holt den nächsten Audio-Chunk"""
        pass

class IAudioPlaybackService(ABC):
    """Interface für Audio-Wiedergabedienste"""
    
    @abstractmethod
    def play_audio(self, audio_data: np.ndarray) -> None:
        """Gibt Audio-Daten wieder"""
        pass
    
    @abstractmethod
    def stop_playback(self) -> None:
        """Stoppt die Audiowiedergabe"""
        pass




# ----------------- Implementierungen -----------------

class EventBus(IEventBus):
    """Implementierung des Event Bus für die Kommunikation zwischen Komponenten"""
    
    def __init__(self):
        self._subscribers = {}
    
    def subscribe(self, event_name: str, callback: Callable) -> None:
        """Abonniert ein Event"""
        if event_name not in self._subscribers:
            self._subscribers[event_name] = []
        self._subscribers[event_name].append(callback)
    
    def publish(self, event_name: str, data: Any = None) -> None:
        """Veröffentlicht ein Event"""
        if event_name in self._subscribers:
            for callback in self._subscribers[event_name]:
                callback(data)











class SystemAudioCaptureService(IAudioCaptureService):
    """Implementierung für System-Audio-Aufnahme"""
    
    def __init__(self, event_bus: IEventBus):
        self.event_bus = event_bus
        self.audio_queue = queue.Queue(maxsize=10)
        self.is_capturing = False
        self.stream = None
        self.sample_rate = 44100
        self.chunk_size = 1024
        self.channels = 2
    
    def audio_callback(self, indata, frames, time, status):
        """Callback für Audio-Stream"""
        if status:
            self.event_bus.publish('status_update', f"Audio-Status: {status}")
        
        # Versuche, Daten in die Queue zu schreiben (nicht blockierend)
        try:
            self.audio_queue.put(indata.copy(), block=False)
        except queue.Full:
            pass  # Queue ist voll, ignoriere diesen Chunk
    
    def start_capture(self) -> None:
        """Startet die Audio-Aufnahme"""
        if self.is_capturing:
            return
            
        try:
            self.stream = sd.InputStream(
                callback=self.audio_callback,
                channels=self.channels,
                samplerate=self.sample_rate,
                blocksize=self.chunk_size
            )
            self.stream.start()
            self.is_capturing = True
            self.event_bus.publish('status_update', "Audio-Aufnahme gestartet")
        except Exception as e:
            self.event_bus.publish('status_update', f"Audio-Fehler: {str(e)}")
    
    def stop_capture(self) -> None:
        """Stoppt die Audio-Aufnahme"""
        if not self.is_capturing:
            return
            
        if self.stream:
            self.stream.stop()
            self.stream.close()
            self.stream = None
            
        self.is_capturing = False
        self.event_bus.publish('status_update', "Audio-Aufnahme gestoppt")
    
    def get_audio_chunk(self) -> np.ndarray:
        """Holt den nächsten Audio-Chunk"""
        try:
            return self.audio_queue.get(block=False)
        except queue.Empty:
            # Queue ist leer, gib leeres Array zurück
            return np.zeros((self.chunk_size, self.channels), dtype=np.float32)

class SystemAudioPlaybackService(IAudioPlaybackService):
    """Implementierung für System-Audio-Wiedergabe"""
    
    def __init__(self, event_bus: IEventBus):
        self.event_bus = event_bus
        self.stream = None
        self.is_playing = False
        self.sample_rate = 44100
        self.channels = 2
    
    def play_audio(self, audio_data: np.ndarray) -> None:
        """Gibt Audio-Daten wieder"""
        if not self.is_playing:
            try:
                self.stream = sd.OutputStream(
                    samplerate=self.sample_rate,
                    channels=self.channels,
                    blocksize=len(audio_data)
                )
                self.stream.start()
                self.is_playing = True
            except Exception as e:
                self.event_bus.publish('status_update', f"Audio-Wiedergabe-Fehler: {str(e)}")
                return
                
        try:
            # Normalisieren und Clipping vermeiden
            audio_data = np.clip(audio_data, -1.0, 1.0)
            self.stream.write(audio_data)
        except Exception as e:
            self.event_bus.publish('status_update', f"Audio-Wiedergabe-Fehler: {str(e)}")
    
    def stop_playback(self) -> None:
        """Stoppt die Audiowiedergabe"""
        if self.stream:
            self.stream.stop()
            self.stream.close()
            self.stream = None
        self.is_playing = False










class MssScreenCaptureService(IScreenCaptureService):
    """Implementierung des Bildschirmaufnahme-Services mit MSS"""
    
    def capture(self, region: Dict[str, int]) -> np.ndarray:
        """Nimmt einen Screenshot der angegebenen Region auf"""
        with mss.mss() as sct:
            screenshot = sct.grab(region)
            frame = np.array(screenshot)
            # BGRA zu RGB konvertieren
            return cv2.cvtColor(frame, cv2.COLOR_BGRA2RGB)

class MosaicEffectStrategy(IEffectStrategy):
    """Implementierung der Mosaik-Effekt-Strategie"""
    
    @property
    def name(self) -> str:
        return "Mosaik"
    
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet den Mosaik-Effekt an"""
        matrix_frames = config.get('matrix_frames', [None] * 9)
        active_cell = config.get('active_cell', 0)
        region_height = config.get('height', frame.shape[0])
        region_width = config.get('width', frame.shape[1])
        cell_width = region_width // 3
        cell_height = region_height // 3
        
        # Aktive Zelle mit aktuellem Frame aktualisieren
        resized_frame = cv2.resize(frame, (cell_width, cell_height))
        matrix_frames[active_cell] = resized_frame.copy()
        
        # Mosaik zusammensetzen
        mosaic = np.zeros((region_height, region_width, 3), dtype=np.uint8)
        
        for i in range(9):
            if matrix_frames[i] is not None:
                row = i // 3
                col = i % 3
                y_start = row * cell_height
                y_end = min((row + 1) * cell_height, region_height)
                x_start = col * cell_width
                x_end = min((col + 1) * cell_width, region_width)
                
                cell_img = cv2.resize(matrix_frames[i], (x_end - x_start, y_end - y_start))
                mosaic[y_start:y_end, x_start:x_end] = cell_img
        
        # Konfiguration mit den aktualisierten Matrix-Frames zurückgeben
        config['matrix_frames'] = matrix_frames
        
        return mosaic

class BlurEffectStrategy(IEffectStrategy):
    """Implementierung einer Blur-Effekt-Strategie"""
    
    @property
    def name(self) -> str:
        return "Blur"
    
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet einen Blur-Effekt an"""
        blur_amount = config.get('blur_amount', 15)
        return cv2.GaussianBlur(frame, (blur_amount, blur_amount), 0)

class EdgeDetectionEffectStrategy(IEffectStrategy):
    """Implementierung einer Kantenerkennung-Effekt-Strategie"""
    
    @property
    def name(self) -> str:
        return "Kantenerkennung"
    
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet eine Kantenerkennung an"""
        gray = cv2.cvtColor(frame, cv2.COLOR_RGB2GRAY)
        edges = cv2.Canny(gray, 100, 200)
        return cv2.cvtColor(edges, cv2.COLOR_GRAY2RGB)

# ----------------- Kern-Logik -----------------

class EffectProcessor:
    """Verarbeitet Bilder mit dem ausgewählten Effekt"""
    
    def __init__(self, capture_service: IScreenCaptureService, event_bus: IEventBus):
        self.capture_service = capture_service
        self.event_bus = event_bus
        self.effect_strategies = {}
        self.current_strategy = None
        self.region = None
        self.is_running = False
        self.is_capturing = False
        self.config = {
            'matrix_frames': [None] * 9,
            'active_cell': 0,
            'has_started': False
        }
        self.capture_thread = None
        
        # Event-Abonnements einrichten
        self.event_bus.subscribe('start_capture', self.start_capture)
        self.event_bus.subscribe('stop_capture', self.stop_capture)
        self.event_bus.subscribe('next_cell', self.next_cell)
        self.event_bus.subscribe('change_effect', self.change_effect)
        
    def register_effect(self, effect: IEffectStrategy) -> None:
        """Registriert eine neue Effekt-Strategie"""
        self.effect_strategies[effect.name] = effect
        if self.current_strategy is None:
            self.current_strategy = effect
    
    def change_effect(self, effect_name: str) -> None:
        """Wechselt den aktiven Effekt"""
        if effect_name in self.effect_strategies:
            self.current_strategy = self.effect_strategies[effect_name]
            self.event_bus.publish('status_update', f"Effekt gewechselt zu: {effect_name}")
    
    def start_capture(self, region_data: Dict[str, int]) -> None:
        """Startet die Bildschirmerfassung"""
        self.region = region_data
        self.config['width'] = region_data['width']
        self.config['height'] = region_data['height']
        self.config['has_started'] = False
        self.config['active_cell'] = 0
        
        # Starten des Capture-Threads
        self.is_capturing = True
        self.is_running = True
        self.capture_thread = threading.Thread(target=self.capture_loop)
        self.capture_thread.daemon = True
        self.capture_thread.start()
        
        self.event_bus.publish('status_update', f"Erfasst Region: ({region_data['left']},{region_data['top']}) {region_data['width']}x{region_data['height']} px")
    
    def stop_capture(self, _=None) -> None:
        """Stoppt die Bildschirmerfassung"""
        self.is_capturing = False
        self.is_running = False
        if self.capture_thread and self.capture_thread.is_alive():
            self.capture_thread.join(timeout=1.0)
        self.event_bus.publish('status_update', "Gestoppt. Bereit für neuen Start.")
    
    def next_cell(self, _=None) -> None:
        """Wechselt zur nächsten Zelle im Mosaik"""
        if self.config['has_started'] and self.is_running:
            self.config['active_cell'] = (self.config['active_cell'] + 1) % 9
            self.event_bus.publish('status_update', f"Aktive Zelle: {self.config['active_cell'] + 1} von 9")
    
    def capture_loop(self) -> None:
        """Hauptschleife für die Bildschirmerfassung"""
        if not self.region:
            self.event_bus.publish('status_update', "Fehler: Keine gültige Region definiert")
            self.stop_capture()
            return
            
        while self.is_capturing:
            try:
                # Screenshot machen
                frame = self.capture_service.capture(self.region)
                
                # Mosaik-Effekt initialisieren, wenn nötig
                if not self.config['has_started'] and self.current_strategy.name == "Mosaik":
                    cell_width = self.region['width'] // 3
                    cell_height = self.region['height'] // 3
                    resized_frame = cv2.resize(frame, (cell_width, cell_height))
                    
                    for i in range(9):
                        self.config['matrix_frames'][i] = resized_frame.copy()
                    
                    self.config['has_started'] = True
                    self.event_bus.publish('status_update', "Effekt gestartet. Klicke 'Nächste Zelle' für Updates.")
                
                # Effekt anwenden
                processed_frame = self.current_strategy.apply(frame, self.config)
                
                # Verarbeitetes Bild veröffentlichen
                self.event_bus.publish('frame_processed', processed_frame)
                
            except Exception as e:
                self.event_bus.publish('status_update', f"Fehler bei der Erfassung: {str(e)}")
                time.sleep(2)
            
            time.sleep(1/30)  # ~30 FPS

class VHSGlitchEffectStrategy(IEffectStrategy):
    """Implementierung eines klassischen VHS-Störungseffekts mit Tracking-Fehlern,
    Scan-Lines und Farbverzerrungen"""
    
    def __init__(self):
        self.last_frame = None
        self.tracking_offset = 0
        self.tracking_direction = 1
        # self.noise_frames entfernt (ungenutzt)
        self.frame_counter = 0
        self.last_major_glitch_time = 0
        self.brightness_pump_phase = 0
        self.last_error_time = 0
        
    @property
    def name(self) -> str:
        return "VHS Störung"
    
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet VHS-Störungseffekte auf das Bild an"""
        try:
            current_time = time.time()
            self.frame_counter += 1
            
            # Initialisierung beim ersten Aufruf
            if self.last_frame is None:
                self.last_frame = frame.copy()
                
            # Kopie des aktuellen Frames erstellen
            result = frame.copy()
            height, width = frame.shape[:2]
            
            # Performance-Optimierung: Effekte verteilen
            
            # 1. Immer anwenden: Scanlines und Grundrauschen als Basis
            result = self._apply_scanlines(result)
            result = self._apply_noise(result, light_only=True)
            
            # 2. Effekte mit variierender Auftrittswahrscheinlichkeit
            # Tracking-Fehler nicht bei jedem Frame (verhindert CPU-Überlastung)
            if self.frame_counter % 2 == 0:
                result = self._apply_tracking_issues(result, height)
                result = self._apply_color_bleeding(result)
            
            # 3. Sehr auffällige Effekte nur selten anwenden
            if np.random.random() < 0.07 and (current_time - self.last_major_glitch_time) > 1.5:
                result = self._apply_horizontal_jitter(result, width, height)
                self.last_major_glitch_time = current_time
                
            # 4. VHS-typische Bildqualitätsmerkmale
            # Bildpumpen-Effekt: Periodische Helligkeits-/Kontraständerungen
            result = self._apply_image_pumping(result)
            
            # 5. Unschärfe (typisch für VHS) - intensiver und variabler
            blur_strength = 3 + int(np.sin(self.frame_counter * 0.03) * 2)
            blur_strength = max(1, blur_strength)
            if blur_strength % 2 == 0:  # Blur-Kernel muss ungerade sein
                blur_strength += 1
            result = cv2.GaussianBlur(result, (blur_strength, blur_strength), 0)
            
            # Vermischung mit vorherigem Frame für "Ghosting"-Effekt (typisch für VHS)
            if self.last_frame is not None and self.frame_counter > 1:
                alpha = 0.85 + 0.05 * np.sin(self.frame_counter * 0.02)
                result = cv2.addWeighted(result, alpha, self.last_frame, 1-alpha, 0)
            
            # Speichere aktuellen Frame für den nächsten Durchlauf
            # Aber nicht bei jedem Frame (Speicheroptimierung)
            if self.frame_counter % 2 == 0:
                self.last_frame = result.copy()
            
            return result
            
        except Exception as e:
            # Fehlerbehandlung mit Rate-Limiting für Meldungen
            current_time = time.time()
            if current_time - self.last_error_time > 1.0:
                print(f"Fehler im VHS-Effekt: {e}")
                self.last_error_time = current_time
            return frame  # Original zurückgeben bei Fehler
    
    def _apply_color_bleeding(self, frame: np.ndarray) -> np.ndarray:
        """Erzeugt den typischen Farbbluten-Effekt von VHS-Kassetten"""
        try:
            b, g, r = cv2.split(frame)
            
            # Weniger aggressives Farbbluten
            shift_amount_x = int(np.sin(self.frame_counter * 0.03) * 3.5)
            
            # Translation-Matrizen
            r_shift = np.float32([[1, 0, shift_amount_x], [0, 1, 0]])
            b_shift = np.float32([[1, 0, -shift_amount_x], [0, 1, 0]])
            
            # Kanäle verschieben
            height, width = frame.shape[:2]
            r = cv2.warpAffine(r, r_shift, (width, height), borderMode=cv2.BORDER_REPLICATE)
            b = cv2.warpAffine(b, b_shift, (width, height), borderMode=cv2.BORDER_REPLICATE)
            
            # Kanäle wieder zusammenführen
            return cv2.merge([b, g, r])
        except Exception as e:
            print(f"Fehler bei Farbbluten-Effekt: {e}")
            return frame
    
    def _apply_scanlines(self, frame: np.ndarray) -> np.ndarray:
        """Fügt horizontale Scan-Lines hinzu"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Leichte horizontale Scan-Lines (jeden 2. Pixel abdunkeln)
            for y in range(0, height, 2):
                result[y, :] = result[y, :] * 0.92
            
            # Periodisch stärkere Abdunklung für gewisse Regionen (Head-Switching-Effekt)
            scan_region = int(height * 0.1 * np.sin(self.frame_counter * 0.02) + height * 0.4)
            region_size = int(height * 0.05)
            region_start = max(0, scan_region - region_size // 2)
            region_end = min(height, scan_region + region_size // 2)
            result[region_start:region_end, :] = result[region_start:region_end, :] * 0.85
                
            # Zufällige stärkere horizontale Störungen (seltener)
            if np.random.random() < 0.015:
                line_pos = np.random.randint(0, height)
                line_width = np.random.randint(1, 3)  # Nicht zu breit
                line_type = np.random.random()
                
                if line_type < 0.3:
                    # Weiße Störlinie
                    result[line_pos:min(line_pos+line_width, height), :] = 255
                elif line_type < 0.6:
                    # Schwarze Störlinie
                    result[line_pos:min(line_pos+line_width, height), :] = 0
                else:
                    # Verzerrte Linie mit begrenztem Shift
                    line_shift = np.random.randint(-15, 15)
                    for i in range(min(line_width, height-line_pos)):
                        result[line_pos+i, :] = np.roll(result[line_pos+i, :], line_shift, axis=0)
            
            return result
        except Exception as e:
            print(f"Fehler bei Scanlines-Effekt: {e}")
            return frame
    
    def _apply_tracking_issues(self, frame: np.ndarray, height: int) -> np.ndarray:
        """Simuliert Tracking-Probleme (vertikaler Versatz)"""
        try:
            # Tracking-Offset berechnen (langsame Bewegung)
            if np.random.random() < 0.01:
                self.tracking_direction *= -1
                
            # Periodische Zurücksetzung um Drift zu vermeiden
            if self.frame_counter % 120 == 0:
                self.tracking_offset = 0
                
            self.tracking_offset += self.tracking_direction * np.random.randint(0, 2)
            self.tracking_offset = max(-12, min(12, self.tracking_offset))
            
            # Matrix für vertikale Verschiebung
            shift_matrix = np.float32([[1, 0, 0], [0, 1, self.tracking_offset]])
            return cv2.warpAffine(frame, shift_matrix, (frame.shape[1], height), borderMode=cv2.BORDER_REPLICATE)
        except Exception as e:
            print(f"Fehler bei Tracking-Effekt: {e}")
            return frame
    
    def _apply_horizontal_jitter(self, frame: np.ndarray, width: int, height: int) -> np.ndarray:
        """Erzeugt horizontale Bildstörungen (zeilenweiser Versatz)"""
        try:
            result = frame.copy()
            
            # Weniger Regionen für bessere Performance
            num_regions = 1  # Fest auf 1 statt zufällig 1-4
            
            for _ in range(num_regions):
                # Region für Jitter auswählen
                start_y = np.random.randint(0, height - 20)
                region_height = np.random.randint(5, 15)
                end_y = min(start_y + region_height, height)
                
                # Für jede Zeile in der Region einen zufälligen Versatz
                for y in range(start_y, end_y):
                    # Stärkere Störungen in der Mitte der Region
                    mid_point = (start_y + end_y) / 2
                    dist_from_mid = abs(y - mid_point) / (region_height / 2) if region_height > 0 else 1
                    jitter_max = int(15 * (1 - dist_from_mid))
                    
                    if jitter_max > 0:
                        jitter_amount = np.random.randint(-jitter_max, jitter_max)
                        result[y, :] = np.roll(result[y, :], jitter_amount, axis=0)
            
            return result
        except Exception as e:
            print(f"Fehler bei Horizontal-Jitter-Effekt: {e}")
            return frame
    
    def _apply_noise(self, frame: np.ndarray, light_only: bool = False) -> np.ndarray:
        """Fügt VHS-typisches Rauschen hinzu"""
        try:
            height, width = frame.shape[:2]
            
            # VHS-spezifisches Rauschen (Körnigkeit)
            # Grundrauschen mit unterschiedlicher Intensität je nach Kanal (RGB)
            noise_r = np.random.randint(0, 12 if light_only else 20, (height, width), dtype=np.uint8)
            noise_g = np.random.randint(0, 8 if light_only else 15, (height, width), dtype=np.uint8)
            noise_b = np.random.randint(0, 15 if light_only else 25, (height, width), dtype=np.uint8)
            
            noise = np.stack([noise_b, noise_g, noise_r], axis=2)
            result = cv2.add(frame, noise)
            
            # Zufällige vertikale Rausch-Streifen (typisch für VHS)
            if np.random.random() < 0.1:
                num_stripes = np.random.randint(1, 3 if light_only else 5)
                for _ in range(num_stripes):
                    stripe_width = np.random.randint(1, 3 if light_only else 6)
                    stripe_x = np.random.randint(0, width - stripe_width)
                    noise_intensity = np.random.randint(10, 30 if light_only else 50)
                    stripe_noise = np.random.randint(0, noise_intensity, (height, stripe_width, 3), dtype=np.uint8)
                    result[:, stripe_x:stripe_x+stripe_width] = cv2.add(
                        result[:, stripe_x:stripe_x+stripe_width], stripe_noise)
            
            # Vereinzelte helle Flecken/Punkte ("Dropouts")
            if not light_only and np.random.random() < 0.15:
                num_spots = np.random.randint(2, 10)
                for _ in range(num_spots):
                    x = np.random.randint(0, width)
                    y = np.random.randint(0, height)
                    spot_size = np.random.randint(1, 3)
                    color = np.random.choice([
                        [255, 255, 255],  # Weiß
                        [0, 0, 0],        # Schwarz
                        [0, 0, 255],      # Rot (BGR)
                        [255, 0, 0]       # Blau (BGR)
                    ])
                    cv2.circle(result, (x, y), spot_size, color, -1)
            
            return result
        except Exception as e:
            print(f"Fehler bei Noise-Effekt: {e}")
            return frame
    
    def _adjust_color_saturation(self, frame: np.ndarray) -> np.ndarray:
        """Passt die Farbsättigung an (typische VHS-Farbprobleme)"""
        try:
            # Konvertiere zu HSV für einfachere Manipulation der Sättigung
            hsv = cv2.cvtColor(frame, cv2.COLOR_RGB2HSV)
            
            # Leicht variierende Sättigung über die Zeit (typisches Farbschwanken)
            saturation_factor = 1.1 + 0.1 * np.sin(self.frame_counter * 0.05)
            hsv[:, :, 1] = np.clip(hsv[:, :, 1] * saturation_factor, 0, 255).astype(np.uint8)
            
            # Kontrast leicht erhöhen (typisch für VHS)
            hsv[:, :, 2] = np.clip(hsv[:, :, 2] * 1.05 - 5, 0, 255).astype(np.uint8)
            
            return cv2.cvtColor(hsv, cv2.COLOR_HSV2RGB)
        except Exception as e:
            print(f"Fehler bei Farbsättigungsanpassung: {e}")
            return frame
    
    def _apply_image_pumping(self, frame: np.ndarray) -> np.ndarray:
        """Erzeugt VHS-typisches Bildpumpen (periodische Helligkeitsänderungen)"""
        try:
            # Update der Bildpumpen-Phase
            self.brightness_pump_phase += 0.03
            
            # Periodischer Helligkeits-/Kontrast-Faktor
            # Langsamer Sinus für natürlicheres Pumpen mit zwei überlagerten Frequenzen
            primary_wave = np.sin(self.brightness_pump_phase)
            secondary_wave = 0.3 * np.sin(self.brightness_pump_phase * 2.7)
            pump_factor = 1.0 + 0.08 * (primary_wave + secondary_wave)
            
            # Auf Bild anwenden
            result = np.clip(frame * pump_factor, 0, 255).astype(np.uint8)
            
            # Zufällige horizontale Linienhelligkeit (flackernde Zeilen)
            if np.random.random() < 0.2:
                num_lines = np.random.randint(1, 3)
                for _ in range(num_lines):
                    y = np.random.randint(0, frame.shape[0])
                    line_width = np.random.randint(1, 3)
                    brightness = 1.0 + np.random.uniform(-0.2, 0.2)
                    for i in range(line_width):
                        if 0 <= y+i < frame.shape[0]:
                            result[y+i, :] = np.clip(result[y+i, :] * brightness, 0, 255).astype(np.uint8)
            
            return result
        except Exception as e:
            print(f"Fehler bei Bildpumpen-Effekt: {e}")
            return frame



class RealTimeGlitchEffectStrategy(IEffectStrategy):
    """Implementierung eines modernen Echtzeit-Glitch-Effekts mit digitalen Störungen"""
    
    def __init__(self):
        self.last_frame = None
        self.glitch_intensity = 0.7  # Intensität des Effekts (0.0 - 1.0)
        self.last_major_glitch_time = 0
        self.major_glitch_interval = 0.8  # Intervall für stärkere Glitches in Sekunden
        
    @property
    def name(self) -> str:
        return "Realtime Glitch"
    
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet einen Echtzeit-Glitch-Effekt an"""
        current_time = time.time()
        
        # Initialisieren des letzten Frames, falls noch nicht geschehen
        if self.last_frame is None:
            self.last_frame = frame.copy()
        
        # Canvas-Größe
        height, width = frame.shape[:2]
        
        # Kopie des aktuellen Frames erstellen, um ihn zu modifizieren
        glitched = frame.copy()
        
        # Zufallsgenerator mit aktuellem Timestamp initialisieren für zufällige Effekte
        np.random.seed(int(current_time * 1000) % 10000)
        
        # 1. RGB-Kanalverschiebung (häufiger Glitch-Effekt)
        if np.random.random() < 0.7:
            # Separiere Farbkanäle
            b, g, r = cv2.split(glitched)
            
            # Zufällige Verschiebungen für die Kanäle
            r_offset_x = int(np.random.randint(-10, 10) * self.glitch_intensity)
            r_offset_y = int(np.random.randint(-5, 5) * self.glitch_intensity)
            g_offset_x = int(np.random.randint(-10, 10) * self.glitch_intensity)
            g_offset_y = int(np.random.randint(-5, 5) * self.glitch_intensity)
            
            # Kanäle verschieben mit Translation-Matrix
            r_shift = np.float32([[1, 0, r_offset_x], [0, 1, r_offset_y]])
            g_shift = np.float32([[1, 0, g_offset_x], [0, 1, g_offset_y]])
            
            r = cv2.warpAffine(r, r_shift, (width, height), borderMode=cv2.BORDER_WRAP)
            g = cv2.warpAffine(g, g_shift, (width, height), borderMode=cv2.BORDER_WRAP)
            
            # Kanäle wieder zusammenführen
            glitched = cv2.merge([b, g, r])
        
        # 2. Horizontale Scanlines und Störungen (wie bei alten Fernsehgeräten)
        if np.random.random() < 0.4:
            for i in range(0, height, np.random.randint(2, 10)):
                if np.random.random() < 0.3:
                    # Helle Störlinien
                    glitched[i:i+1, :] = 255
                elif np.random.random() < 0.2:
                    # Dunkle Störlinien
                    glitched[i:i+1, :] = 0
                elif np.random.random() < 0.1:
                    # Farbige Störlinien
                    glitched[i:i+1, :] = np.random.randint(0, 255, 3)
        
        # 3. Blockige Artefakte / Pixelation (wie bei Kompressionsfehlern)
        if np.random.random() < 0.2:
            block_size = np.random.randint(5, 20)
            # Verkleinerung und dann Vergrößerung erzeugt Pixelationseffekt
            temp = cv2.resize(glitched, (width // block_size, height // block_size))
            glitched = cv2.resize(temp, (width, height), interpolation=cv2.INTER_NEAREST)
            
        # 4. Große Glitches in Intervallen (horizontale Verschiebungen von Bildteilen)
        if current_time - self.last_major_glitch_time > self.major_glitch_interval:
            if np.random.random() < 0.6:
                # Bestimme die horizontale Linie für den Glitch
                glitch_line = np.random.randint(height // 4, height * 3 // 4)
                glitch_width = np.random.randint(10, 50)
                
                # Selektiere den zu verschiebenden Bereich
                slice_to_shift = glitched[glitch_line:glitch_line+glitch_width, :].copy()
                
                # Horizontale Verschiebung
                shift_amount = np.random.randint(-100, 100)
                
                # Arrays für die Verschiebung vorbereiten
                if shift_amount > 0:
                    slice_to_shift = np.hstack([slice_to_shift[:, -shift_amount:], slice_to_shift[:, :-shift_amount]])
                else:
                    shift_amount = abs(shift_amount)
                    slice_to_shift = np.hstack([slice_to_shift[:, shift_amount:], slice_to_shift[:, :shift_amount]])
                
                # Bereich ersetzen
                glitched[glitch_line:glitch_line+glitch_width, :] = slice_to_shift
                
                self.last_major_glitch_time = current_time
        
        # 5. Rauschen hinzufügen
        if np.random.random() < 0.3:
            noise = np.random.randint(0, 50, glitched.shape, dtype=np.uint8)
            glitched = cv2.add(glitched, noise)
            
        # 6. Bildverzerrung durch Welleneffekt
        if np.random.random() < 0.2:
            rows, cols = glitched.shape[:2]
            
            # Wellenparameter
            amplitude = np.random.randint(3, 10)
            period = np.random.randint(30, 100)
            
            # Mapping-Array erstellen
            map_x = np.zeros((rows, cols), np.float32)
            map_y = np.zeros((rows, cols), np.float32)
            
            for i in range(rows):
                for j in range(cols):
                    map_x[i, j] = j
                    map_y[i, j] = i + amplitude * np.sin(j / period)
            
            glitched = cv2.remap(glitched, map_x, map_y, cv2.INTER_LINEAR)
        
        # 7. Glitchy Farbumkehrung in zufälligen Bereichen
        if np.random.random() < 0.1:
            x1, x2 = sorted(np.random.randint(0, width, 2))
            y1, y2 = sorted(np.random.randint(0, height, 2))
            
            if x2 > x1 and y2 > y1:
                glitched[y1:y2, x1:x2] = 255 - glitched[y1:y2, x1:x2]
        
        # 8. Gelegentlich Bildteile vom letzten Frame behalten (Ghosting)
        if np.random.random() < 0.3:
            ghost_strength = np.random.random() * 0.3  # 0-30% Ghosting
            glitched = cv2.addWeighted(glitched, 1 - ghost_strength, self.last_frame, ghost_strength, 0)
        
        # Aktuellen Frame für den nächsten Durchlauf speichern
        self.last_frame = glitched.copy()
        
        return glitched
class OldFilmEffectStrategy(IEffectStrategy):
    """Implementierung eines uralten Filmlooks aus den 1910er Jahren mit 
    typischen Zelluloidfilm-Effekten wie Kratzern, Staub und Flackern"""
    
    def __init__(self):
        self.frame_counter = 0
        self.last_frame = None
        self.flicker_value = 1.0
        self.frame_jump_offset = 0
        self.last_jump_time = 0
        self.last_filmburn_time = 0
        self.last_error_time = 0
        self.scratches = []
        self.dust_particles = []
        self._generate_scratches(5)  # Generiere permanente Kratzer
        self._generate_dust(30)      # Generiere permanente Staubpartikel
        self.fps_factor = 1.0
        
    @property
    def name(self) -> str:
        return "Uralter Film (1910er)"
    
    def _generate_scratches(self, count):
        """Generiert dauerhafte vertikale Kratzer für den Film"""
        self.scratches = []
        for _ in range(count):
            # Position, Breite, Intensität, Länge
            self.scratches.append({
                'x': np.random.randint(0, 100),  # Prozentuale Position
                'width': np.random.randint(1, 4),
                'intensity': np.random.uniform(0.3, 0.9),
                'length': np.random.uniform(0.3, 1.0)  # Prozentuale Länge
            })
    
    def _generate_dust(self, count):
        """Generiert dauerhafte Staubpartikel für den Film"""
        self.dust_particles = []
        for _ in range(count):
            # Position, Größe, Intensität
            self.dust_particles.append({
                'x': np.random.randint(0, 100),  # Prozentuale X-Position
                'y': np.random.randint(0, 100),  # Prozentuale Y-Position
                'size': np.random.randint(1, 6),
                'intensity': np.random.uniform(0.1, 1.0)
            })
    
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet den uralten Filmeffekt auf das Bild an"""
        try:
            current_time = time.time()
            self.frame_counter += 1
            
            # Initialisierung beim ersten Aufruf
            if self.last_frame is None:
                self.last_frame = frame.copy()
                
            # 1. Konvertiere zu Schwarzweiß mit angepasstem Kontrast
            result = self._convert_to_bw(frame)
            
            # 2. Anwenden von Filmkörnung (Filmgrain)
            result = self._apply_grain(result)
            
            # 3. Flackern und ungleichmäßige Belichtung
            result = self._apply_flicker(result)
            
            # 4. Vignettierung (Abdunklung der Ränder)
            result = self._apply_vignette(result)
            
            # 5. Filmkratzer und Schäden
            result = self._apply_scratches(result)
            
            # 6. Staub und Verunreinigungen
            result = self._apply_dust(result)
            
            # 7. Frame-Sprünge (jumping frames)
            result = self._apply_frame_jump(result)
            
            # 8. Filmbrucheffekt (selten)
            if np.random.random() < 0.005:
                result = self._apply_film_break(result)
            
            # 9. Filmverbrennung/Burn Marks (sehr selten)
            if np.random.random() < 0.002 and (current_time - self.last_filmburn_time > 10.0):
                result = self._apply_film_burn(result)
                self.last_filmburn_time = current_time
            
            # 10. "Flackernde Frames" - Periodisch Frames überspringen
            if self.frame_counter % int(20 * self.fps_factor) == 0:
                # Simuliert das Überspringen eines Frames
                if self.last_frame is not None:
                    result = self.last_frame.copy()
            
            # Simuliere schwankende Bildrate
            self.fps_factor = 1.0 + 0.2 * np.sin(self.frame_counter * 0.03)
            
            # Speichere aktuellen Frame für nächsten Durchlauf
            if self.frame_counter % 2 == 0:  # Performance-Optimierung
                self.last_frame = result.copy()
                
            return result
            
        except Exception as e:
            current_time = time.time()
            if current_time - self.last_error_time > 1.0:
                print(f"Fehler im Altfilm-Effekt: {e}")
                self.last_error_time = current_time
            return frame  # Original zurückgeben bei Fehler
    
    def _convert_to_bw(self, frame: np.ndarray) -> np.ndarray:
        """Konvertiert das Bild zu Schwarzweiß mit angepasstem Kontrast"""
        try:
            # Zu Graustufen konvertieren (gewichtete Summe)
            gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
            
            # Kontrast erhöhen (alte Filme hatten oft starken Kontrast)
            contrast_factor = 1.3 + 0.15 * np.sin(self.frame_counter * 0.02)
            mean = np.mean(gray)
            gray = np.clip((gray - mean) * contrast_factor + mean, 0, 255).astype(np.uint8)
            
            # Sepiatonung hinzufügen (typisch für alte Filme)
            sepia = np.array([0.272, 0.534, 0.131])
            gray_3c = cv2.cvtColor(gray, cv2.COLOR_GRAY2BGR)
            sepia_tone = np.array([20, 40, 60])  # Blau, Grün, Rot für Sepiaton
            return np.clip(gray_3c + sepia_tone, 0, 255).astype(np.uint8)
        except Exception as e:
            print(f"Fehler bei Schwarzweiß-Konvertierung: {e}")
            return frame
    
    def _apply_grain(self, frame: np.ndarray) -> np.ndarray:
        """Wendet starke Filmkörnung auf das Bild an"""
        try:
            height, width = frame.shape[:2]
            
            # Starkes, variables Filmkorn
            grain_intensity = 20 + int(5 * np.sin(self.frame_counter * 0.1))
            grain = np.random.normal(0, grain_intensity, frame.shape).astype(np.int16)
            
            # Körnung strukturierter und gröber machen
            for c in range(grain.shape[2]):
                grain[:,:,c] = cv2.GaussianBlur(grain[:,:,c], (3, 3), 0)
            
            # Anwenden der Körnung
            result = np.clip(frame.astype(np.int16) + grain, 0, 255).astype(np.uint8)
            
            return result
        except Exception as e:
            print(f"Fehler bei Korneffekt: {e}")
            return frame
    
    def _apply_flicker(self, frame: np.ndarray) -> np.ndarray:
        """Simuliert das Flackern alter Projektoren mit ungleichmäßiger Belichtung"""
        try:
            # Flackerwert aktualisieren (simuliert instabile Lichtquelle)
            flicker_speed = 0.2
            self.flicker_value = max(0.5, min(1.5, 
                                          self.flicker_value + np.random.normal(0, 0.05) +
                                          0.02 * np.sin(self.frame_counter * flicker_speed)))
            
            # Belichtungsmuster (horizontale Variationen)
            height, width = frame.shape[:2]
            exposure_pattern = np.ones((height, width, 1), dtype=np.float32)
            
            # Horizontale Belichtungsschwankungen
            for y in range(height):
                exposure_pattern[y,:,0] *= 0.95 + 0.1 * np.sin(y * 0.01 + self.frame_counter * 0.05)
            
            # Vertikale Belichtungsvariation
            vertical_pattern = np.ones((height, width, 1), dtype=np.float32)
            for x in range(width):
                vertical_pattern[:,x,0] *= 0.95 + 0.1 * np.sin(x * 0.005 + self.frame_counter * 0.02)
            
            # Belichtungsmuster kombinieren
            exposure_pattern = exposure_pattern * vertical_pattern * self.flicker_value
            
            # Auf Frame anwenden
            result = np.clip(frame.astype(np.float32) * exposure_pattern, 0, 255).astype(np.uint8)
            
            return result
        except Exception as e:
            print(f"Fehler bei Flackereffekt: {e}")
            return frame
    
    def _apply_vignette(self, frame: np.ndarray) -> np.ndarray:
        """Wendet eine starke Vignettierung (Abdunklung der Bildecken) an"""
        try:
            height, width = frame.shape[:2]
            
            # Zentrum des Bildes
            center_x, center_y = width / 2, height / 2
            
            # Mache die Vignette ungleichmäßig (typisch für alte Linsen)
            vignette_shift_x = 0.1 * np.sin(self.frame_counter * 0.02)
            vignette_shift_y = 0.1 * np.cos(self.frame_counter * 0.03)
            Y, X = np.ogrid[:height, :width]
            dist_from_center = np.sqrt((X - center_x * (1 + vignette_shift_x))**2 + 
                                      (Y - center_y * (1 + vignette_shift_y))**2)
            
            # Normalisierung und Anpassung der Vignettenstärke
            max_dist = np.sqrt(center_x**2 + center_y**2)
            vignette = 1 - np.clip(dist_from_center / (max_dist * 0.65), 0, 1)
            
            # Vignette anwenden (3 Kanäle)
            vignette = np.dstack([vignette] * 3)
            
            # Starke Vignette
            vignette = vignette ** 2.5
            
            # Auf Frame anwenden
            result = (frame.astype(np.float32) * vignette).astype(np.uint8)
            
            return result
        except Exception as e:
            print(f"Fehler bei Vignetteneffekt: {e}")
            return frame
    
    def _apply_scratches(self, frame: np.ndarray) -> np.ndarray:
        """Fügt vertikale Kratzer und Filmschäden hinzu"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Bestehende dauerhafte Kratzer anwenden
            for scratch in self.scratches:
                # Scratch-Position berechnen
                x = int(width * scratch['x'] / 100)
                scratch_width = scratch['width']
                intensity = scratch['intensity']
                # Kratzer kann variabel lang sein
                length_factor = scratch['length']
                start_y = int((1 - length_factor) * height * np.random.random())
                end_y = int(start_y + length_factor * height)
                
                # Vertikalen Kratzer erstellen
                for y in range(start_y, end_y):
                    if 0 <= x < width and 0 <= y < height:
                        # Hellen Kratzer erstellen
                        for w in range(scratch_width):
                            if x + w < width:
                                # Mache den Kratzer heller oder dunkler
                                if np.random.random() < 0.7:  # 70% helle Kratzer
                                    value = int(255 * intensity)
                                else:
                                    value = int(50 * intensity)
                                result[y, x + w] = value
            
            # Zufällige temporäre Kratzer (sehr selten)
            if np.random.random() < 0.05:
                num_random_scratches = np.random.randint(1, 3)
                for _ in range(num_random_scratches):
                    x = np.random.randint(0, width)
                    scratch_width = np.random.randint(1, 3)
                    length_factor = np.random.uniform(0.2, 0.9)
                    start_y = int((1 - length_factor) * height * np.random.random())
                    end_y = int(start_y + length_factor * height)
                    
                    for y in range(start_y, end_y):
                        if 0 <= x < width and 0 <= y < height:
                            for w in range(scratch_width):
                                if x + w < width:
                                    if np.random.random() < 0.8:
                                        result[y, x + w] = 255
                                    else:
                                        result[y, x + w] = 0
            
            return result
        except Exception as e:
            print(f"Fehler bei Kratzereffekt: {e}")
            return frame
    
    def _apply_dust(self, frame: np.ndarray) -> np.ndarray:
        """Fügt Staub und Verunreinigungen hinzu"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Bestehende Staubpartikel anwenden
            for dust in self.dust_particles:
                x = int(width * dust['x'] / 100)
                y = int(height * dust['y'] / 100)
                size = dust['size']
                intensity = dust['intensity']
                
                # Staubpartikel ist nur in einigen Frames sichtbar
                if np.random.random() < 0.3:
                    color = int(255 * intensity) if np.random.random() < 0.7 else int(30 * intensity)
                    cv2.circle(result, (x, y), size, (color, color, color), -1)
            
            # Zufällige neue temporäre Staubpartikel
            if np.random.random() < 0.2:
                num_particles = np.random.randint(5, 20)
                for _ in range(num_particles):
                    x = np.random.randint(0, width)
                    y = np.random.randint(0, height)
                    size = np.random.randint(1, 4)
                    if np.random.random() < 0.7:
                        color = np.random.randint(200, 256)
                    else:
                        color = np.random.randint(0, 50)
                    cv2.circle(result, (x, y), size, (color, color, color), -1)
            
            return result
        except Exception as e:
            print(f"Fehler bei Staubeffekt: {e}")
            return frame
    
    def _apply_frame_jump(self, frame: np.ndarray) -> np.ndarray:
        """Simuliert die mechanische Ungenauigkeit alter Kameras und Projektoren"""
        try:
            current_time = time.time()
            height, width = frame.shape[:2]
            
            # Sprunghafte Änderung des Frames in vertikaler Richtung
            if np.random.random() < 0.02 or (current_time - self.last_jump_time > 2.0 and np.random.random() < 0.3):
                self.frame_jump_offset = np.random.randint(-20, 21)
                self.last_jump_time = current_time
            
            # Langsame Stabilisierung zurück
            if self.frame_jump_offset != 0:
                if abs(self.frame_jump_offset) > 1:
                    self.frame_jump_offset = int(self.frame_jump_offset * 0.8)
                else:
                    self.frame_jump_offset = 0
            
            # Anwenden der Verschiebung
            if self.frame_jump_offset != 0:
                shift_matrix = np.float32([[1, 0, 0], [0, 1, self.frame_jump_offset]])
                result = cv2.warpAffine(frame, shift_matrix, (width, height), 
                                        borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0))
            else:
                result = frame
            
            # Leichtes horizontales Wackeln (Projektorungenauigkeit)
            horizontal_wobble = int(2 * np.sin(self.frame_counter * 0.2))
            if horizontal_wobble != 0:
                shift_matrix = np.float32([[1, 0, horizontal_wobble], [0, 1, 0]])
                result = cv2.warpAffine(result, shift_matrix, (width, height),
                                      borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0))
            
            return result
        except Exception as e:
            print(f"Fehler bei Frame-Jump-Effekt: {e}")
            return frame
    
    def _apply_film_break(self, frame: np.ndarray) -> np.ndarray:
        """Simuliert einen Filmriss oder eine Beschädigung"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Entscheide über Art des Filmrisses
            break_type = np.random.random()
            
            if break_type < 0.4:
                # Horizontaler Riss (heller Streifen)
                y_pos = np.random.randint(0, height)
                stripe_height = np.random.randint(5, 20)
                result[y_pos:min(y_pos + stripe_height, height), :] = 255
                
                # Zerrissene Kanten
                for x in range(0, width, 2):
                    tear_height = np.random.randint(-3, 4)
                    y_tear_start = max(0, y_pos + tear_height)
                    y_tear_end = min(height, y_pos + stripe_height + tear_height)
                    result[y_tear_start:y_tear_end, x:x+1] = 255
                
            elif break_type < 0.7:
                # Vertikaler Riss
                x_pos = np.random.randint(0, width)
                stripe_width = np.random.randint(5, 30)
                result[:, x_pos:min(x_pos + stripe_width, width)] = 0
                
                # Filmverzerrung in der Nähe des Risses
                for y in range(0, height, 3):
                    shift = np.random.randint(-10, 11)
                    start_x = max(0, x_pos - 20)
                    end_x = min(width, x_pos + stripe_width + 20)
                    if 0 <= y < height and start_x < end_x:
                        segment = result[y, start_x:end_x].copy()
                        result[y, start_x:end_x] = np.roll(segment, shift)
                
            else:
                # Diagonaler Riss
                start_x = np.random.randint(0, width//2)
                end_x = np.random.randint(width//2, width)
                start_y = np.random.randint(0, height//2)
                end_y = np.random.randint(height//2, height)
                
                # Riss zeichnen
                rip_width = np.random.randint(2, 8)
                points = np.array([[start_x, start_y], [end_x, end_y]], np.int32)
                cv2.polylines(result, [points], False, (255, 255, 255), rip_width)
                
                # Verzerrung entlang des Risses
                for t in np.linspace(0, 1, 20):
                    x = int(start_x + t * (end_x - start_x))
                    y = int(start_y + t * (end_y - start_y))
                    
                    # Verzerrungsbereich um die Risslinie
                    blur_radius = np.random.randint(10, 30)
                    x_start = max(0, x - blur_radius)
                    x_end = min(width, x + blur_radius)
                    y_start = max(0, y - blur_radius)
                    y_end = min(height, y + blur_radius)
                    
                    if x_start < x_end and y_start < y_end:
                        # Lokale Verzerrung
                        region = result[y_start:y_end, x_start:x_end].copy()
                        if region.size > 0:
                            # Zufällige Verzerrung
                            shift_x = np.random.randint(-5, 6)
                            shift_y = np.random.randint(-5, 6)
                            transform = np.float32([[1, 0, shift_x], [0, 1, shift_y]])
                            region = cv2.warpAffine(region, transform, 
                                                   (region.shape[1], region.shape[0]),
                                                   borderMode=cv2.BORDER_REPLICATE)
                            result[y_start:y_end, x_start:x_end] = region
            
            return result
        except Exception as e:
            print(f"Fehler bei Film-Break-Effekt: {e}")
            return frame
    
    def _apply_film_burn(self, frame: np.ndarray) -> np.ndarray:
        """Simuliert Filmverbrennung, wie sie in alten Projektoren vorkam"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Position der Verbrennung festlegen
            burn_type = np.random.random()
            
            if burn_type < 0.7:
                # Zentrale Verbrennung
                center_x = np.random.randint(width // 4, width * 3 // 4)
                center_y = np.random.randint(height // 4, height * 3 // 4)
                max_radius = np.random.randint(30, max(50, min(width, height) // 3))
                
                # Verbrennung von innen nach außen
                for radius in range(1, max_radius + 1):
                    intensity = 1.0 - (radius / max_radius)
                    cv2.circle(result, (center_x, center_y), radius, 
                              (0, 0, 0) if np.random.random() < 0.7 else (255, 255, 255),
                              thickness=1)
                
                # Kreisförmige Maske für unregelmäßige Verbrennung
                burn_mask = np.zeros((height, width), dtype=np.uint8)
                cv2.circle(burn_mask, (center_x, center_y), max_radius, 255, -1)
                
                # Ungleichmäßige Kanten
                contours = np.random.randint(max_radius // 2, max_radius, size=(8, 2))
                contours = contours * np.array([np.cos(np.linspace(0, 2*np.pi, 8)), 
                                             np.sin(np.linspace(0, 2*np.pi, 8))]).T
                contours = contours.astype(np.int32) + np.array([center_x, center_y])
                cv2.fillPoly(burn_mask, [contours], 255)
                
                # Schwarze Verbrennung im Inneren
                inner_radius = max_radius // 2
                cv2.circle(result, (center_x, center_y), inner_radius, (0, 0, 0), -1)
                
                # Helle Ränder (Verbrennung)
                outer_ring = np.zeros_like(result)
                cv2.circle(outer_ring, (center_x, center_y), max_radius, (255, 255, 255), 
                           thickness=np.random.randint(2, 6))
                result = cv2.addWeighted(result, 1.0, outer_ring, 0.8, 0)
                
            else:
                # Seitliche/Ecken-Verbrennung
                corner = np.random.randint(0, 4)  # 0: oben links, 1: oben rechts, 2: unten rechts, 3: unten links
                
                if corner == 0:
                    center_x, center_y = 0, 0
                elif corner == 1:
                    center_x, center_y = width - 1, 0
                elif corner == 2:
                    center_x, center_y = width - 1, height - 1
                else:
                    center_x, center_y = 0, height - 1
                
                # Größe der Verbrennung
                burn_size = np.random.randint(30, 100)
                
                # Schwarzer Kern
                cv2.rectangle(result, 
                             (max(0, center_x - burn_size//2), max(0, center_y - burn_size//2)),
                             (min(width, center_x + burn_size//2), min(height, center_y + burn_size//2)),
                             (0, 0, 0), -1)
                
                # Verbrennungseffekt (helle Ränder)
                for i in range(5):
                    thickness = np.random.randint(1, 4)
                    size_offset = np.random.randint(5, 20)
                    cv2.rectangle(result,
                                (max(0, center_x - burn_size//2 - size_offset), 
                                max(0, center_y - burn_size//2 - size_offset)),
                                (min(width, center_x + burn_size//2 + size_offset), 
                                min(height, center_y + burn_size//2 + size_offset)),
                                (255, 255, 255), thickness)
            
            # Unregelmäßige Flecken hinzufügen
            for _ in range(np.random.randint(3, 10)):
                fleck_x = np.random.randint(0, width)
                fleck_y = np.random.randint(0, height)
                fleck_size = np.random.randint(2, 10)
                fleck_color = (0, 0, 0) if np.random.random() < 0.7 else (255, 255, 255)
                cv2.circle(result, (fleck_x, fleck_y), fleck_size, fleck_color, -1)
            
            return result
        except Exception as e:
            print(f"Fehler bei Film-Burn-Effekt: {e}")
            return frame




class GlitchDimensionEffectStrategy(IEffectStrategy):
    """Implementiert einen extremen Glitch-Effekt, der aussieht als würde die Realität
    zwischen verschiedenen Dimensionen kollabieren, mit starken Farbverschiebungen,
    räumlichen Verzerrungen und Datenkorruption."""
    
    def __init__(self):
        self.frame_counter = 0
        self.last_frames = collections.deque(maxlen=8)  # Speichert die letzten 8 Frames
        self.dimension_shift_time = time.time()
        self.reality_stability = 1.0  # Stabilität der Realität (wird mit der Zeit schwanken)
        self.current_dimension = 0
        self.dimension_blend = 0.0
        self.last_error_time = 0
        self.cached_noise_textures = {}
        self.glitch_blocks = []
        self._generate_glitch_blocks()
        self.rgb_shift_values = [0, 0, 0]
        self.frequency_domains = {}
        self.last_reality_breach = 0
        self.breach_coordinates = []
        self.saved_regions = []
        
        # Initialisiere zufällige Fragmentierungsparameter
        self.fragmentation_level = np.random.uniform(0.2, 0.8)
        self.fragmentation_speed = np.random.uniform(0.01, 0.05)
        
        # Laden und Vorbereiten von Assets für verschiedene "Dimensionen"
        self.dimension_effects = [
            self._corrupt_reality,         # Dimension 0: Realitätskorruption
            self._digital_decay,           # Dimension 1: Digitaler Zerfall
            self._quantum_uncertainty,     # Dimension 2: Quantenunsicherheit
            self._void_intrusion,          # Dimension 3: Eindringen der Leere
            self._hyperbolic_distortion    # Dimension 4: Hyperbolische Verzerrung
        ]
    
    @property
    def name(self) -> str:
        return "Glitch-Dimension"
    
    def _generate_glitch_blocks(self, count=15):
        """Generiert Blöcke für Glitch-Effekte"""
        self.glitch_blocks = []
        for _ in range(count):
            # Relative Position, Größe, Verzerrungsgrad, Lebenszeit
            self.glitch_blocks.append({
                'x': np.random.uniform(0, 1),          # X-Position
                'y': np.random.uniform(0, 1),          # Y-Position
                'width': np.random.uniform(0.05, 0.3), # Relative Breite
                'height': np.random.uniform(0.05, 0.3),# Relative Höhe
                'corruption': np.random.uniform(0.3, 1.0),  # Korruptionsgrad
                'lifetime': np.random.randint(5, 30),  # Lebensdauer in Frames
                'current_age': 0,                      # Aktuelles Alter
                'shift_values': [np.random.randint(-30, 31) for _ in range(3)],
                'direction': np.random.randint(0, 4),  # 0: rechts, 1: links, 2: oben, 3: unten
                'type': np.random.randint(0, 5)        # Verschiedene Glitch-Typen
            })
    
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet den Glitch-Dimension-Effekt auf das Bild an"""
        try:
            current_time = time.time()
            self.frame_counter += 1
            
            # Originalbild für Notfälle speichern
            original_frame = frame.copy()
            
            # Füge aktuellen Frame zur Geschichte hinzu
            if len(self.last_frames) < 8 or self.frame_counter % 2 == 0:
                self.last_frames.append(frame.copy())
            
            # Aktualisiere die "Realitätsstabilität"
            self._update_reality_stability()
            
            # 1. Basiseffekt: Farbkanalverschiebung
            result = self._apply_rgb_channel_shift(frame)
            
            # 2. Glitch-Blöcke und Fragmentierung
            result = self._apply_glitch_blocks(result)
            
            # 3. Hochfrequenzstörungen (Rauschen, Flimmern, Scanlines)
            result = self._apply_high_frequency_noise(result)
            
            # 4. Dimensionssprünge (plötzliche Veränderungen)
            result = self._apply_dimension_jumps(result)
            
            # 5. Zeitanomalien (Vergangenheits- und Zukunftsfragmente)
            result = self._apply_temporal_anomalies(result)
            
            # 6. Anwenden des aktuellen Dimensionseffekts
            result = self.dimension_effects[self.current_dimension](result)
            
            # 7. Realitätsrisse (plötzliche Destabilisierung)
            if np.random.random() < 0.01 * (2.0 - self.reality_stability) and current_time - self.last_reality_breach > 3.0:
                result = self._apply_reality_breach(result)
                self.last_reality_breach = current_time
            
            # 8. Eplilepsy Safety - Reduziere zu extreme Effekte
            if np.random.random() < 0.05:  # 5% Chance auf Stabilisierung (Sicherheitsmechanismus)
                intensity = np.random.uniform(0.3, 0.7)
                result = cv2.addWeighted(result, intensity, original_frame, 1 - intensity, 0)
            
            # Aktualisiere Zufallswerte für nächsten Frame
            self._update_glitch_parameters()
            
            return result
            
        except Exception as e:
            current_time = time.time()
            if current_time - self.last_error_time > 1.0:
                print(f"Fehler im Glitch-Dimension-Effekt: {e}")
                self.last_error_time = current_time
            return frame  # Bei Fehler Original zurückgeben
    
    def _update_reality_stability(self):
        """Aktualisiert die Stabilitätswerte der simulierten 'Realität'"""
        # Langsame Änderung der Stabilität über die Zeit
        sine_component = 0.3 * np.sin(self.frame_counter * 0.01)
        noise_component = 0.05 * np.random.normal()
        
        # Plötzliche Stabilitätssprünge
        if np.random.random() < 0.005:  # 0.5% Chance pro Frame
            jump_magnitude = np.random.uniform(-0.4, 0.4)
            self.reality_stability = max(0.2, min(1.0, self.reality_stability + jump_magnitude))
        else:
            # Normalerweise langsame Änderung
            self.reality_stability = max(0.2, min(1.0, self.reality_stability + sine_component + noise_component))
    
    def _update_glitch_parameters(self):
        """Aktualisiert verschiedene Glitch-Parameter für nächsten Frame"""
        # RGB-Kanalverschiebung aktualisieren
        channel_stability = self.reality_stability * 0.8
        for i in range(3):
            if np.random.random() < (1.0 - channel_stability) * 0.3:
                # Plötzlicher Sprung
                self.rgb_shift_values[i] = np.random.randint(-int(30 * (1.0 - channel_stability)), 
                                                           int(30 * (1.0 - channel_stability)))
            else:
                # Graduelles Zurückkehren zur Normalität
                self.rgb_shift_values[i] = int(self.rgb_shift_values[i] * 0.85)
        
        # Aktualisiere Glitchblöcke
        for block in self.glitch_blocks:
            block['current_age'] += 1
            if block['current_age'] >= block['lifetime']:
                # Block erneuern
                block['x'] = np.random.uniform(0, 1)
                block['y'] = np.random.uniform(0, 1)
                block['width'] = np.random.uniform(0.05, 0.3)
                block['height'] = np.random.uniform(0.05, 0.3)
                block['corruption'] = np.random.uniform(0.3, 1.0)
                block['lifetime'] = np.random.randint(5, 30)
                block['current_age'] = 0
                block['shift_values'] = [np.random.randint(-30, 31) for _ in range(3)]
                block['type'] = np.random.randint(0, 5)
            
            # Bewege Block je nach Richtung
            move_speed = 0.01 * (1.0 - self.reality_stability) * block['corruption']
            if block['direction'] == 0:
                block['x'] = min(1.1, block['x'] + move_speed)
            elif block['direction'] == 1:
                block['x'] = max(-0.1, block['x'] - move_speed)
            elif block['direction'] == 2:
                block['y'] = max(-0.1, block['y'] - move_speed)
            else:
                block['y'] = min(1.1, block['y'] + move_speed)
        
        # Dimensions-Wechsel?
        current_time = time.time()
        if current_time - self.dimension_shift_time > 5.0 and np.random.random() < 0.02:
            # Dimensionswechsel einleiten
            self.dimension_blend = 0.0
            self.next_dimension = np.random.randint(0, len(self.dimension_effects))
            self.dimension_shift_time = current_time
        
        # Dimensions-Blending aktualisieren
        if self.dimension_blend < 1.0:
            self.dimension_blend += 0.05
            if self.dimension_blend >= 1.0:
                self.current_dimension = self.next_dimension
                self.dimension_blend = 1.0
        
        # Aktualisiere Fragmentierungslevel
        self.fragmentation_level += self.fragmentation_speed * np.random.normal(0, 0.1)
        if self.fragmentation_level > 0.9 or self.fragmentation_level < 0.1:
            self.fragmentation_speed = -self.fragmentation_speed
        self.fragmentation_level = max(0.1, min(0.9, self.fragmentation_level))
    
    def _apply_rgb_channel_shift(self, frame: np.ndarray) -> np.ndarray:
        """Verschiebt die RGB-Kanäle für einen chromatischen Aberrations-Effekt"""
        try:
            if all(shift == 0 for shift in self.rgb_shift_values):
                return frame
            
            height, width = frame.shape[:2]
            result = np.zeros_like(frame)
            
            # Extrahiere Kanäle
            b, g, r = cv2.split(frame)
            
            # Verschiebe Kanäle unterschiedlich
            for y in range(height):
                for x in range(width):
                    # Blau-Kanal
                    b_x = max(0, min(width-1, x + self.rgb_shift_values[0]))
                    result[y, x, 0] = b[y, b_x]
                    
                    # Grün-Kanal
                    g_x = max(0, min(width-1, x + self.rgb_shift_values[1]))
                    result[y, x, 1] = g[y, g_x]
                    
                    # Rot-Kanal
                    r_x = max(0, min(width-1, x + self.rgb_shift_values[2]))
                    result[y, x, 2] = r[y, r_x]
            
            # Performance-Optimierung: Nutze NumPy für Massenoperationen
            # Dies ist ein Hack, aber funktioniert gut für kleine Verschiebungen
            
            # Für größere Bilder eine weniger rechenintensive Alternative:
            if max(abs(shift) for shift in self.rgb_shift_values) > 5 or width * height > 480000:  # z.B. 800x600
                # Weniger präzise, aber viel schneller für große Bilder
                channels = []
                for i, c in enumerate(cv2.split(frame)):
                    shift = self.rgb_shift_values[i]
                    if shift == 0:
                        channels.append(c)
                    else:
                        M = np.float32([[1, 0, shift], [0, 1, 0]])
                        channels.append(cv2.warpAffine(c, M, (width, height)))
                result = cv2.merge(channels)
            
            return result
            
        except Exception as e:
            print(f"Fehler bei RGB-Kanalverschiebung: {e}")
            return frame
    
    def _apply_glitch_blocks(self, frame: np.ndarray) -> np.ndarray:
        """Fügt korrupte, verschobene und verzerrte Blöcke hinzu"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            for block in self.glitch_blocks:
                if np.random.random() > block['corruption'] * (1.0 - self.reality_stability):
                    continue  # Block überspringen
                
                # Block-Koordinaten berechnen
                x1 = max(0, min(width-1, int(width * block['x'])))
                y1 = max(0, min(height-1, int(height * block['y'])))
                x2 = max(0, min(width-1, int(x1 + width * block['width'])))
                y2 = max(0, min(height-1, int(y1 + height * block['height'])))
                
                if x2 <= x1 or y2 <= y1:
                    continue
                
                # Block-Region extrahieren
                block_region = frame[y1:y2, x1:x2].copy()
                
                # Je nach Typ verschiedene Effekte anwenden
                if block['type'] == 0:
                    # Farbkanalverschiebung
                    b, g, r = cv2.split(block_region)
                    for i, channel in enumerate([b, g, r]):
                        shift = block['shift_values'][i]
                        if shift != 0:
                            M = np.float32([[1, 0, shift], [0, 1, 0]])
                            channel = cv2.warpAffine(channel, M, (block_region.shape[1], block_region.shape[0]))
                        if i == 0:
                            b = channel
                        elif i == 1:
                            g = channel
                        else:
                            r = channel
                    block_region = cv2.merge([b, g, r])
                
                elif block['type'] == 1:
                    # Pixelierung
                    scale = max(1, int(10 * block['corruption']))
                    temp = cv2.resize(block_region, (block_region.shape[1]//scale, block_region.shape[0]//scale), 
                                    interpolation=cv2.INTER_LINEAR)
                    block_region = cv2.resize(temp, (block_region.shape[1], block_region.shape[0]), 
                                            interpolation=cv2.INTER_NEAREST)
                
                elif block['type'] == 2:
                    # Verzerrung/Wellen
                    rows, cols = block_region.shape[:2]
                    x_shift = np.sin(np.arange(rows)/5) * 10 * block['corruption']
                    for i in range(rows):
                        block_region[i,:] = np.roll(block_region[i,:], int(x_shift[i]), axis=0)
                
                elif block['type'] == 3:
                    # Korrupte Daten (zufällige Pixel)
                    mask = np.random.random(block_region.shape[:2]) < (0.2 * block['corruption'])
                    noise = np.random.randint(0, 256, size=block_region.shape, dtype=np.uint8)
                    mask_3d = np.stack([mask]*3, axis=2)
                    block_region = np.where(mask_3d, noise, block_region)
                
                else:  # block['type'] == 4
                    # Umgekehrte Farben und Solarisation
                    block_region = 255 - block_region  # Invertieren
                    # Solarisation-Effekt
                    threshold = int(128 * block['corruption'])
                    mask = block_region > threshold
                    block_region[mask] = 255 - block_region[mask]
                
                # Veränderten Block zurück ins Bild einfügen
                result[y1:y2, x1:x2] = block_region
            
            return result
            
        except Exception as e:
            print(f"Fehler bei Glitch-Blocks-Effekt: {e}")
            return frame
    



    def _hyperbolic_distortion(self, frame: np.ndarray) -> np.ndarray:
        """Erzeugt hyperbolische Verzerrungen, die Raum und Perspektive verbiegen"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Parameter für hyperbolische Verzerrung
            distortion_intensity = np.random.uniform(0.3, 0.9)
            
            # 1. Hyperbolische Koordinatenverzerrung
            if np.random.random() < 0.8:
                # Erzeuge Verzerrungskarten für die Remapping-Funktion
                map_x = np.zeros((height, width), dtype=np.float32)
                map_y = np.zeros((height, width), dtype=np.float32)
                
                # Zentrum der Verzerrung
                center_x = width // 2 + np.random.randint(-width//4, width//4)
                center_y = height // 2 + np.random.randint(-height//4, height//4)
                
                # Parameter für die hyperbolische Verzerrung
                k1 = np.random.uniform(-0.3, 0.3) * distortion_intensity  # Radialer Parameter
                k2 = np.random.uniform(-0.2, 0.2) * distortion_intensity  # Tangentialer Parameter
                rotation = np.random.uniform(0, np.pi * 2)                # Rotation
                scale = np.random.uniform(0.7, 1.3)                       # Skalierungsfaktor
                
                # Berechne hyperbolische Verzerrung
                for y in range(height):
                    for x in range(width):
                        # Normalisierte Koordinaten relativ zum Zentrum
                        norm_x = (x - center_x) / width
                        norm_y = (y - center_y) / height
                        
                        # Abstand vom Zentrum
                        r = np.sqrt(norm_x * norm_x + norm_y * norm_y)
                        
                        if r > 0:
                            # Hyperbolische Verzerrung basierend auf dem Kehrwert des Abstands
                            theta = np.arctan2(norm_y, norm_x) + rotation
                            
                            # Nicht-lineare Radiusänderung - hyperbolische Funktion
                            r_distorted = r + k1 * np.tanh(r * 4) + k2 * (1 / (r + 0.1) - 1)
                            
                            # Konvertiere zurück zu kartesischen Koordinaten
                            new_norm_x = r_distorted * np.cos(theta)
                            new_norm_y = r_distorted * np.sin(theta)
                            
                            # Skalieren und zum Zentrum hinzufügen
                            map_x[y, x] = center_x + new_norm_x * width * scale
                            map_y[y, x] = center_y + new_norm_y * height * scale
                        else:
                            map_x[y, x] = x
                            map_y[y, x] = y
                
                # Wende die Verzerrungskarte auf das Bild an
                result = cv2.remap(result, map_x, map_y, cv2.INTER_LINEAR, 
                                    borderMode=cv2.BORDER_REFLECT)
            
            # 2. Perspektivische Verzerrung - Bildteile in verschiedenen "Raumebenen"
            if np.random.random() < 0.5:
                num_planes = np.random.randint(2, 5)
                
                for _ in range(num_planes):
                    # Zufällige Region auswählen
                    reg_width = np.random.randint(width // 4, width // 2)
                    reg_height = np.random.randint(height // 4, height // 2)
                    reg_x = np.random.randint(0, width - reg_width)
                    reg_y = np.random.randint(0, height - reg_height)
                    
                    # Region extrahieren
                    region = result[reg_y:reg_y+reg_height, reg_x:reg_x+reg_width].copy()
                    
                    # Quellpunkte
                    src_points = np.array([
                        [0, 0],
                        [reg_width-1, 0],
                        [reg_width-1, reg_height-1],
                        [0, reg_height-1]
                    ], dtype=np.float32)
                    
                    # Zufällige Verzerrung der Zielpunkte für perspektivischen Effekt
                    distort_factor = int(min(reg_width, reg_height) * 0.3 * distortion_intensity)
                    dst_points = np.array([
                        [np.random.randint(-distort_factor, distort_factor),
                            np.random.randint(-distort_factor, distort_factor)],
                        [reg_width-1 + np.random.randint(-distort_factor, distort_factor),
                            np.random.randint(-distort_factor, distort_factor)],
                        [reg_width-1 + np.random.randint(-distort_factor, distort_factor),
                            reg_height-1 + np.random.randint(-distort_factor, distort_factor)],
                        [np.random.randint(-distort_factor, distort_factor),
                            reg_height-1 + np.random.randint(-distort_factor, distort_factor)]
                    ], dtype=np.float32)
                    
                    # Perspektivische Transformation berechnen
                    perspective_matrix = cv2.getPerspectiveTransform(src_points, dst_points)
                    
                    # Transformation anwenden
                    warped_region = cv2.warpPerspective(region, perspective_matrix, 
                                                        (reg_width, reg_height))
                    
                    # Veränderte Region zurück ins Bild einfügen
                    result[reg_y:reg_y+reg_height, reg_x:reg_x+reg_width] = warped_region
            
            # 3. Möbiusband-ähnliche Transformationen (lokal invertierte Bereiche)
            if np.random.random() < 0.3:
                num_inversions = np.random.randint(1, 4)
                
                for _ in range(num_inversions):
                    # Position und Größe der Inversion
                    inv_x = np.random.randint(0, width - 100)
                    inv_y = np.random.randint(0, height - 100)
                    inv_width = np.random.randint(50, min(200, width - inv_x))
                    inv_height = np.random.randint(50, min(200, height - inv_y))
                    
                    # Bereich extrahieren
                    inv_region = result[inv_y:inv_y+inv_height, inv_x:inv_x+inv_width].copy()
                    
                    # Zufällige Transformation anwenden:
                    transform_type = np.random.randint(0, 3)
                    
                    if transform_type == 0:
                        # Horizontal spiegeln
                        inv_region = cv2.flip(inv_region, 1)
                    elif transform_type == 1:
                        # Vertikal spiegeln
                        inv_region = cv2.flip(inv_region, 0)
                    else:
                        # 180-Grad-Drehung
                        inv_region = cv2.rotate(inv_region, cv2.ROTATE_180)
                    
                    # Zusätzliche Farbmanipulation für Möbius-Effekt
                    if np.random.random() < 0.5:
                        hsv = cv2.cvtColor(inv_region, cv2.COLOR_BGR2HSV)
                        # Farbton umkehren (180° verschieben)
                        hsv[:,:,0] = (hsv[:,:,0] + 90) % 180
                        inv_region = cv2.cvtColor(hsv, cv2.COLOR_HSV2BGR)
                    
                    # Zurück ins Bild einfügen
                    result[inv_y:inv_y+inv_height, inv_x:inv_x+inv_width] = inv_region
            
            # 4. Streifenweise Manipulationen (Nicht-Euklidische Geometrieeffekte)
            if np.random.random() < 0.4:
                stripe_orientation = np.random.randint(0, 2)  # 0: horizontal, 1: vertikal
                num_stripes = np.random.randint(4, 12)
                
                if stripe_orientation == 0:  # Horizontale Streifen
                    stripe_height = height // num_stripes
                    
                    for i in range(num_stripes):
                        if np.random.random() < 0.5:  # 50% der Streifen bearbeiten
                            y_start = i * stripe_height
                            y_end = min((i + 1) * stripe_height, height)
                            
                            # Streifentyp
                            stripe_effect = np.random.randint(0, 3)
                            
                            if stripe_effect == 0:
                                # Streifenweise Verschiebung
                                shift = np.random.randint(-width//4, width//4)
                                result[y_start:y_end, :] = np.roll(result[y_start:y_end, :], shift, axis=1)
                            
                            elif stripe_effect == 1:
                                # Streifenweise Skalierung
                                scale_factor = np.random.uniform(0.7, 1.3)
                                stripe = result[y_start:y_end, :].copy()
                                
                                # Horizontale Skalierung
                                new_width = int(width * scale_factor)
                                resized = cv2.resize(stripe, (new_width, y_end - y_start))
                                
                                # Zentrieren und zurück in den Rahmen einfügen
                                if scale_factor < 1.0:
                                    padding = (width - new_width) // 2
                                    padded = np.zeros_like(stripe)
                                    padded[:, padding:padding+new_width] = resized
                                    result[y_start:y_end, :] = padded
                                else:
                                    # Beschneiden und zentrieren
                                    crop_start = (new_width - width) // 2
                                    result[y_start:y_end, :] = resized[:, crop_start:crop_start+width]
                            
                            else:
                                # Streifenweise Rotation/Verzerrung
                                center_x = width // 2
                                angle = np.random.uniform(-10, 10)
                                scale = np.random.uniform(0.9, 1.1)
                                
                                M = cv2.getRotationMatrix2D((center_x, (y_start+y_end)//2), angle, scale)
                                stripe = result[y_start:y_end, :].copy()
                                result[y_start:y_end, :] = cv2.warpAffine(stripe, M, (width, y_end-y_start),
                                                                        borderMode=cv2.BORDER_REFLECT)
                else:  # Vertikale Streifen
                    stripe_width = width // num_stripes
                    
                    for i in range(num_stripes):
                        if np.random.random() < 0.5:  # 50% der Streifen bearbeiten
                            x_start = i * stripe_width
                            x_end = min((i + 1) * stripe_width, width)
                            
                            # Streifentyp
                            stripe_effect = np.random.randint(0, 3)
                            
                            if stripe_effect == 0:
                                # Streifenweise vertikale Verschiebung
                                shift = np.random.randint(-height//4, height//4)
                                result[:, x_start:x_end] = np.roll(result[:, x_start:x_end], shift, axis=0)
                            
                            elif stripe_effect == 1:
                                # Streifenweise Skalierung
                                scale_factor = np.random.uniform(0.7, 1.3)
                                stripe = result[:, x_start:x_end].copy()
                                
                                # Vertikale Skalierung
                                new_height = int(height * scale_factor)
                                resized = cv2.resize(stripe, (x_end - x_start, new_height))
                                
                                # Zentrieren und zurück in den Rahmen einfügen
                                if scale_factor < 1.0:
                                    padding = (height - new_height) // 2
                                    padded = np.zeros_like(stripe)
                                    padded[padding:padding+new_height, :] = resized
                                    result[:, x_start:x_end] = padded
                                else:
                                    # Beschneiden und zentrieren
                                    crop_start = (new_height - height) // 2
                                    result[:, x_start:x_end] = resized[crop_start:crop_start+height, :]
                            
                            else:
                                # Streifenweise Rotation/Verzerrung
                                center_y = height // 2
                                angle = np.random.uniform(-10, 10)
                                scale = np.random.uniform(0.9, 1.1)
                                
                                M = cv2.getRotationMatrix2D(((x_start+x_end)//2, center_y), angle, scale)
                                stripe = result[:, x_start:x_end].copy()
                                warped = cv2.warpAffine(stripe, M, (x_end-x_start, height), 
                                                        borderMode=cv2.BORDER_REFLECT)
                                result[:, x_start:x_end] = warped
            
            # 5. Raum-Faltungseffekte - erzeugt "gekrümmte" Farbflächen
            if np.random.random() < 0.6:
                # Erzeuge Farbverlaufsvektoren, die den Raum zu "falten" scheinen
                
                # Erstelle Farbverlaufsmaske
                gradient_mask = np.zeros((height, width), dtype=np.float32)
                
                # Parameter für die Faltung
                fold_type = np.random.randint(0, 3)
                
                if fold_type == 0:
                    # Radiale Faltung
                    center_x = width // 2 + np.random.randint(-width//4, width//4)
                    center_y = height // 2 + np.random.randint(-height//4, height//4)
                    max_radius = np.sqrt(width*width + height*height) / 2
                    
                    # Erzeuge radialen Gradienten
                    for y in range(height):
                        for x in range(width):
                            dx = x - center_x
                            dy = y - center_y
                            distance = np.sqrt(dx*dx + dy*dy)
                            # Sinus-Welle entlang des Radius
                            value = 0.5 + 0.5 * np.sin(distance / max_radius * np.pi * np.random.randint(2, 5))
                            gradient_mask[y, x] = value
                
                elif fold_type == 1:
                    # Lineare Faltung mit mehreren Wellen
                    angle = np.random.uniform(0, np.pi)
                    frequency = np.random.uniform(5, 15) * np.pi / max(width, height)
                    
                    sin_comp = np.sin(angle)
                    cos_comp = np.cos(angle)
                    
                    for y in range(height):
                        for x in range(width):
                            # Projektion auf die Achse
                            projection = x * cos_comp + y * sin_comp
                            # Sinus-Welle entlang der Projektion
                            value = 0.5 + 0.5 * np.sin(projection * frequency)
                            gradient_mask[y, x] = value
                
                else:
                    # Spiralförmige Faltung
                    center_x = width // 2
                    center_y = height // 2
                    max_radius = np.sqrt(width*width + height*height) / 2
                    
                    for y in range(height):
                        for x in range(width):
                            dx = x - center_x
                            dy = y - center_y
                            distance = np.sqrt(dx*dx + dy*dy)
                            angle = np.arctan2(dy, dx)
                            # Spiralförmige Welle
                            value = 0.5 + 0.5 * np.sin(angle * np.random.randint(3, 8) + distance / 20)
                            gradient_mask[y, x] = value
                
                # Mache die Maske etwas weicher für natürlichere Übergänge
                gradient_mask = cv2.GaussianBlur(gradient_mask, (15, 15), 0)
                
                # Färbe Bereiche basierend auf der Maske
                heatmap = cv2.applyColorMap(
                    (gradient_mask * 255).astype(np.uint8), 
                    np.random.choice([cv2.COLORMAP_JET, cv2.COLORMAP_VIRIDIS, cv2.COLORMAP_PLASMA])
                )
                
                # Mische den Farbverlauf mit dem Originalbild
                alpha = np.random.uniform(0.3, 0.7) * distortion_intensity
                result = cv2.addWeighted(result, 1-alpha, heatmap, alpha, 0)

            return result
        
        except Exception as e:
            print(f"Fehler bei hyperbolischer Verzerrung: {e}")
            return frame
        
    def _apply_high_frequency_noise(self, frame: np.ndarray) -> np.ndarray:
        """Fügt hochfrequentes Rauschen, Scanlines und andere Störungen hinzu"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Störungslevel basierend auf Realitätsstabilität
            noise_level = int(20 * (1.0 - self.reality_stability))
            
            # 1. Scanlines (horizontale Linien)
            if np.random.random() < 0.7:
                for y in range(0, height, 2):
                    darkness = np.random.uniform(0.7, 0.9)
                    result[y, :] = (result[y, :] * darkness).astype(np.uint8)
            
            # 2. Vertikale Synchronisationsstörungen
            if np.random.random() < 0.3:
                num_vsync_errors = np.random.randint(1, 5)
                for _ in range(num_vsync_errors):
                    y_start = np.random.randint(0, height)
                    y_height = np.random.randint(1, 10)
                    y_end = min(height, y_start + y_height)
                    x_shift = np.random.randint(-width//5, width//5)
                    
                    if x_shift > 0:
                        result[y_start:y_end, x_shift:] = result[y_start:y_end, :-x_shift]
                        result[y_start:y_end, :x_shift] = 0
                    elif x_shift < 0:
                        x_shift = abs(x_shift)
                        result[y_start:y_end, :-x_shift] = result[y_start:y_end, x_shift:]
                        result[y_start:y_end, -x_shift:] = 0
            
            # 3. CRT-ähnliches Rauschen
            if np.random.random() < 0.5:
                noise = np.random.normal(0, noise_level, frame.shape).astype(np.int16)
                result = np.clip(result.astype(np.int16) + noise, 0, 255).astype(np.uint8)
            
            # 4. Zufällige Farbflecken
            if np.random.random() < 0.2:
                num_spots = np.random.randint(10, 50)
                for _ in range(num_spots):
                    x = np.random.randint(0, width)
                    y = np.random.randint(0, height)
                    color = [np.random.randint(0, 256) for _ in range(3)]
                    radius = np.random.randint(1, 5)
                    cv2.circle(result, (x, y), radius, color, -1)
            
            # 5. "Bad Signal" Effekt - horizontale Störungslinien
            if np.random.random() < 0.15 * (1.0 - self.reality_stability):
                num_bad_lines = np.random.randint(1, 10)
                for _ in range(num_bad_lines):
                    y_pos = np.random.randint(0, height)
                    line_height = np.random.randint(1, 5)
                    for i in range(line_height):
                        if y_pos + i < height:
                            if np.random.random() < 0.5:
                                # Weißes Rauschen
                                result[y_pos + i, :] = np.random.randint(0, 256, size=(width, 3))
                            else:
                                # Schwarze/weiße Linie
                                result[y_pos + i, :] = [0, 0, 0] if np.random.random() < 0.5 else [255, 255, 255]
            
            return result
            
        except Exception as e:
            print(f"Fehler bei Hochfrequenz-Rauschen: {e}")
            return frame
    
    def _apply_dimension_jumps(self, frame: np.ndarray) -> np.ndarray:
        """Simuliert plötzliche Sprünge zwischen verschiedenen 'Dimensionen'"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Seltene starke Verzerrungen
            if np.random.random() < 0.02 * (1.0 - self.reality_stability):
                # Extreme Verzerrung - Wirbel-Effekt
                center_x = width // 2 + np.random.randint(-width//4, width//4)
                center_y = height // 2 + np.random.randint(-height//4, height//4)
                
                # Affine Transformation Matrix (Drehung und Skalierung)
                angle = np.random.uniform(0, 360)
                scale = np.random.uniform(0.7, 1.3)
                M = cv2.getRotationMatrix2D((center_x, center_y), angle, scale)
                
                # Verändere die Matrix für verzerrendere Transformation
                M[0, 0] *= np.random.uniform(0.8, 1.2)  # Skalierung X
                M[1, 1] *= np.random.uniform(0.8, 1.2)  # Skalierung Y
                
                # Wende Transformation an
                result = cv2.warpAffine(result, M, (width, height))
                
                # Zentriere um Transformationspunkt
                center_mask = np.zeros((height, width), dtype=np.uint8)
                radius = int(min(width, height) * np.random.uniform(0.1, 0.3))
                cv2.circle(center_mask, (center_x, center_y), radius, 255, -1)
                
                # Farbverschiebung im Zentrum
                hsv = cv2.cvtColor(result, cv2.COLOR_BGR2HSV)
                hsv_shift = hsv.copy()
                h, s, v = cv2.split(hsv_shift)
                h = (h + np.random.randint(0, 180)) % 180
                hsv_shift = cv2.merge([h, s, v])
                shifted_color = cv2.cvtColor(hsv_shift, cv2.COLOR_HSV2BGR)
                
                # Füge veränderte Farbe zum Ergebnis hinzu
                mask_3d = cv2.cvtColor(center_mask, cv2.COLOR_GRAY2BGR) / 255.0
                result = (result * (1 - mask_3d) + shifted_color * mask_3d).astype(np.uint8)
            
            # Zufällige "Portal"-Effekte - Fenster zu anderen Dimensionen
            if np.random.random() < 0.03 * (1.0 - self.reality_stability) and len(self.last_frames) > 0:
                num_portals = np.random.randint(1, 4)
                for _ in range(num_portals):
                    # Position und Größe des "Portals"
                    x = np.random.randint(0, width - 20)
                    y = np.random.randint(0, height - 20)
                    portal_width = np.random.randint(30, min(200, width - x))
                    portal_height = np.random.randint(30, min(200, height - y))
                    
                    # Wähle Quelldimension (früherer Frame)
                    source_frame_idx = np.random.randint(0, len(self.last_frames))
                    if source_frame_idx < len(self.last_frames):
                        source_frame = self.last_frames[source_frame_idx]
                        
                        # Extrahiere Region
                        portal_content = source_frame[y:y+portal_height, x:x+portal_width].copy()
                        
                        # Optional: Füge Effekte zum Portal hinzu
                        if np.random.random() < 0.5:
                            # Farb-Invertierung
                            portal_content = 255 - portal_content
                        
                        # Erstelle Maske für weichen Übergang
                        mask = np.ones((portal_height, portal_width), dtype=np.float32)
                        mask_center = (portal_width//2, portal_height//2)
                        for i in range(portal_height):
                            for j in range(portal_width):
                                dist = np.sqrt((j - mask_center[0])**2 + (i - mask_center[1])**2)
                                max_dist = np.sqrt(mask_center[0]**2 + mask_center[1]**2)
                                mask[i, j] *= max(0, 1 - dist/max_dist)
                        
                        mask = cv2.GaussianBlur(mask, (7, 7), 0)
                        mask = np.dstack([mask] * 3)
                        
                        # Füge Portal zum Frame hinzu
                        result[y:y+portal_height, x:x+portal_width] = (
                            portal_content * mask + 
                            result[y:y+portal_height, x:x+portal_width] * (1 - mask)
                        ).astype(np.uint8)
                        
                        # Rahmen um das Portal
                        border_color = [np.random.randint(0, 256) for _ in range(3)]
                        cv2.rectangle(result, (x, y), (x + portal_width, y + portal_height), 
                                    border_color, 2)
            
            return result
            
        except Exception as e:
            print(f"Fehler bei Dimensions-Sprung-Effekt: {e}")
            return frame
    
    def _apply_temporal_anomalies(self, frame: np.ndarray) -> np.ndarray:
        """Fügt Zeitanomalien durch Überlagerung früherer/späterer Frames hinzu"""
        try:
            # Muss genügend Frames haben
            if len(self.last_frames) < 3:
                return frame
            
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Entscheide, ob eine Zeitanomalie auftreten soll
            if np.random.random() < 0.1 * (1.0 - self.reality_stability):
                # Wähle zufällig einen früheren Frame
                past_frame_idx = np.random.randint(0, len(self.last_frames))
                past_frame = self.last_frames[past_frame_idx]
                
                # Erstelle eine Maske für die Anomalie
                anomaly_mask = np.zeros((height, width), dtype=np.float32)
                
                # Art der Zeitanomalie wählen
                anomaly_type = np.random.randint(0, 4)
                
                if anomaly_type == 0:
                    # Zeitrisse - vertikale oder horizontale Streifen
                    is_vertical = np.random.random() < 0.5
                    num_stripes = np.random.randint(3, 10)
                    stripe_width = (width if is_vertical else height) // num_stripes
                    
                    for i in range(num_stripes):
                        if np.random.random() < 0.4:  # 40% der Streifen zeigen Vergangenheit
                            start_pos = i * stripe_width
                            end_pos = min((i + 1) * stripe_width, width if is_vertical else height)
                            if is_vertical:
                                anomaly_mask[:, start_pos:end_pos] = 1.0
                            else:
                                anomaly_mask[start_pos:end_pos, :] = 1.0
                
                elif anomaly_type == 1:
                    # Konzentrische Ringe
                    center_x = width // 2
                    center_y = height // 2
                    max_radius = int(np.sqrt(center_x**2 + center_y**2))
                    num_rings = np.random.randint(3, 8)
                    
                    for i in range(num_rings):
                        if np.random.random() < 0.5:  # 50% der Ringe zeigen Vergangenheit
                            inner_radius = int(max_radius * i / num_rings)
                            outer_radius = int(max_radius * (i + 1) / num_rings)
                            
                            Y, X = np.ogrid[:height, :width]
                            dist_squared = (X - center_x)**2 + (Y - center_y)**2
                            
                            ring_mask = (dist_squared >= inner_radius**2) & (dist_squared < outer_radius**2)
                            anomaly_mask[ring_mask] = 1.0
                
                elif anomaly_type == 2:
                    # Zufällige Bereiche/Blobs
                    num_blobs = np.random.randint(2, 6)
                    
                    for _ in range(num_blobs):
                        x = np.random.randint(0, width)
                        y = np.random.randint(0, height)
                        radius = np.random.randint(20, 100)
                        
                        Y, X = np.ogrid[:height, :width]
                        dist_squared = (X - x)**2 + (Y - y)**2
                        blob_mask = dist_squared <= radius**2
                        
                        anomaly_mask[blob_mask] = 1.0
                
                else:  # anomaly_type == 3
                    # Verzerrte/wellenförmige Zeitfragmente
                    frequency_x = np.random.uniform(1, 5) * np.pi / width
                    frequency_y = np.random.uniform(1, 5) * np.pi / height
                    phase_x = np.random.uniform(0, 2*np.pi)
                    phase_y = np.random.uniform(0, 2*np.pi)
                    
                    Y, X = np.ogrid[:height, :width]
                    wave_pattern = np.sin(X * frequency_x + phase_x) * np.cos(Y * frequency_y + phase_y)
                    wave_pattern = (wave_pattern + 1) / 2  # Normalisieren auf 0-1
                    
                    anomaly_mask = (wave_pattern > 0.6).astype(np.float32)
                
                # Weiche Übergänge
                anomaly_mask = cv2.GaussianBlur(anomaly_mask, (15, 15), 0)
                
                # Anomalie auf 3 Kanäle erweitern
                anomaly_mask_3d = np.dstack([anomaly_mask] * 3)
                
                # Vergangenheit mit Gegenwart kombinieren
                result = (past_frame * anomaly_mask_3d + frame * (1 - anomaly_mask_3d)).astype(np.uint8)
            
            return result
            
        except Exception as e:
            print(f"Fehler bei Zeitanomalien: {e}")
            return frame
        





    def _digital_decay(self, frame: np.ndarray) -> np.ndarray:
        """Simuliert digitalen Zerfall wie beschädigte Videodaten oder korrumpierte Dateien"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Zufallsfaktor für Verfallsintensität
            decay_intensity = np.random.uniform(0.3, 0.7)
            
            # 1. Blockweise Kompressionsartefakte
            if np.random.random() < 0.8:
                # Anzahl der Artefaktblöcke
                num_blocks = int(10 * decay_intensity)
                for _ in range(num_blocks):
                    # Zufällige Position und Größe des Blocks
                    block_size = np.random.randint(15, 60)
                    x = np.random.randint(0, width - block_size)
                    y = np.random.randint(0, height - block_size)
                    
                    # Block extrahieren
                    block = result[y:y+block_size, x:x+block_size].copy()
                    
                    # Art des Artefakts
                    artifact_type = np.random.random()
                    
                    if artifact_type < 0.25:
                        # JPEG-ähnliche Blockverzerrung - starke Kompression
                        block = cv2.resize(cv2.resize(block, (block_size//8, block_size//8), 
                                                    interpolation=cv2.INTER_LINEAR),
                                        (block_size, block_size), interpolation=cv2.INTER_NEAREST)
                    elif artifact_type < 0.5:
                        # Quantisierungsfehler - Farbwerte reduzieren
                        factor = np.random.randint(3, 8)
                        block = ((block // factor) * factor).astype(np.uint8)
                    elif artifact_type < 0.75:
                        # Farbverlust - zu Graustufen mit verminderter Farbtiefe
                        gray = cv2.cvtColor(cv2.cvtColor(block, cv2.COLOR_BGR2GRAY), cv2.COLOR_GRAY2BGR)
                        factor = np.random.randint(2, 6)
                        block = ((gray // factor) * factor).astype(np.uint8)
                    else:
                        # Extreme Blockverschiebung (B-Frame Error)
                        rows, cols = block.shape[:2]
                        M = np.float32([[1, 0, np.random.randint(-10, 11)], 
                                    [0, 1, np.random.randint(-10, 11)]])
                        block = cv2.warpAffine(block, M, (cols, rows))
                    
                    # Veränderten Block zurücksetzen
                    result[y:y+block_size, x:x+block_size] = block
            
            # 2. Datenverlust und "Corruption" simulieren
            if np.random.random() < 0.7:
                # Horizontale Linien mit Datenkorruption
                num_corrupt_lines = int(20 * decay_intensity)
                for _ in range(num_corrupt_lines):
                    y = np.random.randint(0, height)
                    length = np.random.randint(width // 5, width)
                    start_x = np.random.randint(0, width - length)
                    
                    # Art der Korruption
                    corrupt_type = np.random.random()
                    
                    if corrupt_type < 0.3:
                        # Vollständiger Datenverlust (schwarze/weiße Linie)
                        color = 0 if np.random.random() < 0.6 else 255
                        result[y:y+1, start_x:start_x+length] = color
                    elif corrupt_type < 0.6:
                        # Zufälliges Rauschen
                        noise = np.random.randint(0, 256, (1, length, 3)).astype(np.uint8)
                        result[y:y+1, start_x:start_x+length] = noise
                    else:
                        # Verwürfelter Inhalt
                        line = result[y:y+1, start_x:start_x+length].copy()
                        shuffled_indices = np.arange(length)
                        np.random.shuffle(shuffled_indices)
                        for i, idx in enumerate(shuffled_indices):
                            if i < length:
                                result[y, start_x + i] = line[0, idx]
            
            # 3. Bit-Flip-Fehler simulieren
            if np.random.random() < 0.5:
                # Anzahl der Bitfehler
                num_bit_errors = int(100 * decay_intensity)
                for _ in range(num_bit_errors):
                    # Zufällige Position
                    x = np.random.randint(0, width)
                    y = np.random.randint(0, height)
                    
                    # Zufälliger Kanal
                    channel = np.random.randint(0, 3)
                    
                    # Bit-Flip simulieren (XOR mit einer Bit-Maske)
                    bit_pos = 1 << np.random.randint(0, 8)  # 2^0 bis 2^7
                    result[y, x, channel] = result[y, x, channel] ^ bit_pos
            
            # 4. DCT-Block-Artefakte (wie bei starker JPEG-Kompression)
            if np.random.random() < 0.6:
                # Anzahl der Blöcke mit DCT-Artefakten
                num_dct_blocks = int(15 * decay_intensity)
                for _ in range(num_dct_blocks):
                    # Standard-Blockgröße für JPEG ist 8x8
                    block_size = 8
                    
                    # Zufällige Position
                    x = np.random.randint(0, width - block_size)
                    y = np.random.randint(0, height - block_size)
                    
                    # Matrix für ersetzende DCT-Koeffizienten (typisches Muster)
                    dct_pattern = np.zeros((block_size, block_size, 3), dtype=np.uint8)
                    
                    # Einfache Muster füllen (simuliert beschädigte DCT-Koeffizienten)
                    pattern_type = np.random.random()
                    
                    if pattern_type < 0.33:
                        # Schachbrettmuster
                        for i in range(block_size):
                            for j in range(block_size):
                                if (i + j) % 2 == 0:
                                    dct_pattern[i, j] = [0, 0, 0]
                                else:
                                    dct_pattern[i, j] = [255, 255, 255]
                    elif pattern_type < 0.66:
                        # Gradient
                        for i in range(block_size):
                            val = int(255 * i / (block_size - 1))
                            for j in range(block_size):
                                dct_pattern[i, j] = [val, val, val]
                    else:
                        # Konstanter Block (typische Manifestation eines Koeffizienten-Verlusts)
                        val = np.random.randint(0, 256)
                        dct_pattern[:, :] = [val, val, val]
                    
                    # Block durch Muster ersetzen
                    result[y:y+block_size, x:x+block_size] = dct_pattern
            
            # 5. Interlacing-Artefakte
            if np.random.random() < 0.4:
                # Jede zweite Zeile von einem früheren Frame (simuliert durch Verschiebung)
                shift = np.random.randint(5, width // 3)
                direction = 1 if np.random.random() > 0.5 else -1
                
                # Auf jede zweite Zeile anwenden
                for y in range(0, height, 2):
                    result[y, :] = np.roll(result[y, :], shift * direction, axis=0)
            
            return result
        
        except Exception as e:
            print(f"Fehler bei Digital Decay: {e}")
            return frame






    def _quantum_uncertainty(self, frame: np.ndarray) -> np.ndarray:
        """Simuliert Quanteneffekte wie Superposition, Unschärfe und Wahrscheinlichkeitswolken"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Quantenunsicherheits-Parameter
            uncertainty_level = np.random.uniform(0.2, 0.8)
            
            # 1. Wahrscheinlichkeitswolken (Gaussian Blur mit variablen Stärken)
            if np.random.random() < 0.7:
                # Teile das Bild in Regionen unterschiedlicher "Unschärfe"
                num_regions = np.random.randint(3, 8)
                for _ in range(num_regions):
                    # Zufällige Region
                    region_x = np.random.randint(0, width - 50)
                    region_y = np.random.randint(0, height - 50)
                    region_w = np.random.randint(50, min(width - region_x, 200))
                    region_h = np.random.randint(50, min(height - region_y, 200))
                    
                    # Extrahiere Region
                    region = result[region_y:region_y+region_h, region_x:region_x+region_w].copy()
                    
                    # Berechne Unschärfestärke basierend auf Heisenberg'scher Unschärferelation-Metapher
                    # Je präziser die Position (kleiner Bereich), desto stärker die Unschärfe
                    uncertainty = max(3, int(15 * (1.0 - (region_w * region_h) / (width * height)) * uncertainty_level))
                    
                    # Wende Gaussian Blur als "Wahrscheinlichkeitswolke" an
                    blurred = cv2.GaussianBlur(region, (uncertainty * 2 + 1, uncertainty * 2 + 1), 0)
                    
                    # Alpha-Blending zwischen Original und Unschärfe
                    alpha = np.random.uniform(0.4, 0.9)
                    region = cv2.addWeighted(region, 1-alpha, blurred, alpha, 0)
                    
                    # Zurück ins Bild einfügen
                    result[region_y:region_y+region_h, region_x:region_x+region_w] = region
            
            # 2. Quantensprünge (plötzliche Positionsänderungen einzelner Pixel-Cluster)
            if np.random.random() < 0.6:
                num_jumps = int(20 * uncertainty_level)
                for _ in range(num_jumps):
                    # Quell- und Zielposition für den Sprung
                    src_x = np.random.randint(0, width - 20)
                    src_y = np.random.randint(0, height - 20)
                    src_w = np.random.randint(5, 20)
                    src_h = np.random.randint(5, 20)
                    
                    # Zielposition (kann überlappen)
                    dst_x = np.random.randint(0, width - src_w)
                    dst_y = np.random.randint(0, height - src_h)
                    
                    # Extrahiere Quellregion
                    if src_x + src_w <= width and src_y + src_h <= height:
                        source_region = result[src_y:src_y+src_h, src_x:src_x+src_w].copy()
                        
                        # Einfügen mit Probability-Blending
                        if dst_x + src_w <= width and dst_y + src_h <= height:
                            target_region = result[dst_y:dst_y+src_h, dst_x:dst_x+src_w].copy()
                            
                            # Wahrscheinlichkeitsbasiertes Mischen zwischen Quelle und Ziel
                            prob_mask = np.random.random((src_h, src_w, 1)) < 0.7
                            prob_mask = np.repeat(prob_mask, 3, axis=2)
                            
                            mixed_region = np.where(prob_mask, source_region, target_region)
                            result[dst_y:dst_y+src_h, dst_x:dst_x+src_w] = mixed_region
            
            # 3. Quanten-Interferenzmuster
            if np.random.random() < 0.4:
                # Erzeuge Wellen-Interferenzmuster
                pattern_scale = np.random.uniform(10, 30)
                x = np.arange(width)
                y = np.arange(height)
                X, Y = np.meshgrid(x, y)
                
                # Mehrere überlagerte Wellen für Interferenz
                wave1 = np.sin(X / pattern_scale + np.random.random() * 10)
                wave2 = np.sin(Y / pattern_scale + np.random.random() * 10)
                wave3 = np.cos((X + Y) / pattern_scale + np.random.random() * 10)
                
                # Kombiniere Wellen zu Interferenzmuster
                interference = (wave1 + wave2 + wave3) / 3.0
                
                # Skaliere und wende als Intensitätsmodifikation an
                intensity = np.ones_like(result) * (1.0 + interference[:,:,None] * 0.15 * uncertainty_level)
                result = np.clip(result.astype(np.float32) * intensity, 0, 255).astype(np.uint8)
            
            # 4. Quantentunnel-Effekt (Pixel erscheinen an unerwarteten Stellen)
            if np.random.random() < 0.5:
                num_tunnels = int(100 * uncertainty_level)
                for _ in range(num_tunnels):
                    # Quellpixel
                    src_x = np.random.randint(0, width)
                    src_y = np.random.randint(0, height)
                    
                    # Tunnelziel (mit gewisser maximaler "Tunneldistanz")
                    tunnel_distance = int(min(width, height) * 0.3 * uncertainty_level)
                    dst_x = max(0, min(width-1, src_x + np.random.randint(-tunnel_distance, tunnel_distance+1)))
                    dst_y = max(0, min(height-1, src_y + np.random.randint(-tunnel_distance, tunnel_distance+1)))
                    
                    # Tunneln: Pixel an Zielposition erscheint
                    if 0 <= src_x < width and 0 <= src_y < height and 0 <= dst_x < width and 0 <= dst_y < height:
                        result[dst_y, dst_x] = result[src_y, src_x]
            
            # 5. Schrödingers Katze Effekt - Überlagerungszustände von Bildbereichen
            if np.random.random() < 0.3:
                num_superpositions = np.random.randint(1, 4)
                for _ in range(num_superpositions):
                    # Region für Überlagerungszustand
                    super_x = np.random.randint(0, width - 100)
                    super_y = np.random.randint(0, height - 100)
                    super_w = np.random.randint(50, min(width - super_x, 200))
                    super_h = np.random.randint(50, min(height - super_y, 200))
                    
                    # Extrahiere Region
                    if super_x + super_w <= width and super_y + super_h <= height:
                        region = result[super_y:super_y+super_h, super_x:super_x+super_w].copy()
                        
                        # "Alternativer Zustand" - hier invertiertes Bild als Metapher
                        alt_state = 255 - region
                        
                        # Erzeuge Maske für Überlagerung (komplexes Muster)
                        mask = np.zeros((super_h, super_w), dtype=np.float32)
                        for i in range(10):  # Mehrere überlagerte Wellenfunktionen
                            freq = np.random.uniform(0.01, 0.1)
                            phase = np.random.uniform(0, 2*np.pi)
                            for y in range(super_h):
                                for x in range(super_w):
                                    mask[y, x] += 0.1 * np.sin(freq * x + phase) * np.cos(freq * y + phase)
                        
                        # Normalisiere Maske auf [0,1]
                        mask = (mask - np.min(mask)) / (np.max(mask) - np.min(mask))
                        mask = np.repeat(mask[:,:,np.newaxis], 3, axis=2)
                        
                        # Mische die beiden Zustände basierend auf Maske
                        superposition = region * (1 - mask) + alt_state * mask
                        result[super_y:super_y+super_h, super_x:super_x+super_w] = superposition.astype(np.uint8)
            
            return result
            
        except Exception as e:
            print(f"Fehler bei Quantenunsicherheit: {e}")
            return frame


    def _void_intrusion(self, frame: np.ndarray) -> np.ndarray:
        """Simuliert das Eindringen der Leere in die Bildebene - dunkle, absorbierende Bereiche"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Parameter für die Leere
            void_intensity = np.random.uniform(0.4, 0.9)  # Wie stark die Leere erscheint
            
            # 1. Hauptleere - ein großer, sich ausbreitender dunkler Bereich
            if np.random.random() < 0.8:
                # Position der Leere
                void_center_x = np.random.randint(width // 4, width * 3 // 4)
                void_center_y = np.random.randint(height // 4, height * 3 // 4)
                
                # Größe und Form der Leere
                max_radius = int(min(width, height) * 0.4 * void_intensity)
                
                # Erstelle eine Maske für die Leere mit unregelmäßiger Form
                void_mask = np.zeros((height, width), dtype=np.float32)
                
                # Generiere eine unregelmäßige Form durch Überlagerung mehrerer Kreise
                num_circles = np.random.randint(3, 8)
                for _ in range(num_circles):
                    # Offset vom Zentrum
                    offset_x = np.random.randint(-max_radius//2, max_radius//2)
                    offset_y = np.random.randint(-max_radius//2, max_radius//2)
                    
                    # Position und Größe des Teilkreises
                    cx = void_center_x + offset_x
                    cy = void_center_y + offset_y
                    radius = np.random.randint(max_radius//3, max_radius)
                    
                    # Erstelle temporären Kreis
                    temp_mask = np.zeros((height, width), dtype=np.float32)
                    cv2.circle(temp_mask, (cx, cy), radius, 1.0, -1)
                    
                    # Füge zur Hauptmaske hinzu
                    void_mask = np.maximum(void_mask, temp_mask)
                
                # Weiche Kanten für die Leere
                void_mask = cv2.GaussianBlur(void_mask, (21, 21), max_radius/5)
                
                # Normalisiere die Maske auf [0,1]
                if np.max(void_mask) > 0:
                    void_mask = void_mask / np.max(void_mask)
                
                # Erstelle einen 'Leere'-Effekt - fast schwarz mit leichtem Blaustich
                void_color = np.zeros_like(result)
                void_color[:, :] = [10, 0, 5]  # Dunkler Blau-/Violettton
                
                # Bereich um die Leere verzerren
                distortion_map_x = np.zeros((height, width), np.float32)
                distortion_map_y = np.zeros((height, width), np.float32)
                
                # Initialisiere mit normaler Gitterstruktur
                for y in range(height):
                    for x in range(width):
                        distortion_map_x[y, x] = x
                        distortion_map_y[y, x] = y
                
                # Verzerrung: Sog zum Zentrum der Leere
                for y in range(height):
                    for x in range(width):
                        # Abstand vom Zentrum der Leere
                        dx = x - void_center_x
                        dy = y - void_center_y
                        distance = np.sqrt(dx*dx + dy*dy)
                        
                        if distance < max_radius * 2:
                            # Stärke der Verzerrung basierend auf Abstand und Maskenwert
                            mask_val = void_mask[y, x]
                            pull_factor = 5.0 * mask_val * (1.0 - distance / (max_radius * 2))
                            
                            # Sog zum Zentrum
                            if distance > 0:
                                distortion_map_x[y, x] -= dx / distance * pull_factor
                                distortion_map_y[y, x] -= dy / distance * pull_factor
                
                # Wende Verzerrung auf das Bild an
                result = cv2.remap(result, distortion_map_x, distortion_map_y, 
                                cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)
                
                # Erweitere die Maske auf 3 Kanäle
                void_mask_3c = np.stack([void_mask] * 3, axis=2)
                
                # Mische das Originalbild mit der 'Leere'
                result = result * (1.0 - void_mask_3c * void_intensity) + void_color * void_mask_3c * void_intensity
            
            # 2. Kleinere 'Void Spots' - vereinzelte Einbrüche der Leere
            num_void_spots = np.random.randint(2, 8)
            for _ in range(num_void_spots):
                # Position des Spots
                spot_x = np.random.randint(0, width)
                spot_y = np.random.randint(0, height)
                
                # Größe des Spots
                spot_radius = np.random.randint(5, 30)
                
                # Generiere eine Minimaske für den Spot
                spot_mask = np.zeros((height, width), dtype=np.float32)
                cv2.circle(spot_mask, (spot_x, spot_y), spot_radius, 1.0, -1)
                
                # Unregelmäßige Ränder
                for angle in range(0, 360, 20):
                    noise_radius = spot_radius * (0.7 + 0.6 * np.random.random())
                    noise_x = int(spot_x + noise_radius * np.cos(np.radians(angle)))
                    noise_y = int(spot_y + noise_radius * np.sin(np.radians(angle)))
                    
                    # Begrenze auf Bildgrenzen
                    noise_x = max(0, min(width-1, noise_x))
                    noise_y = max(0, min(height-1, noise_y))
                    
                    # Zeichne zusätzliche Kreise für unregelmäßige Form
                    cv2.circle(spot_mask, (noise_x, noise_y), int(spot_radius * 0.4), 1.0, -1)
                
                # Weiche Kanten
                spot_mask = cv2.GaussianBlur(spot_mask, (15, 15), spot_radius/3)
                
                # Normalisiere die Maske
                if np.max(spot_mask) > 0:
                    spot_mask = spot_mask / np.max(spot_mask)
                    
                # Erweitere auf 3 Kanäle
                spot_mask_3c = np.stack([spot_mask] * 3, axis=2)
                
                # Wähle zwischen Leere und Invertierung (ein anderer Typ der "Void"-Störung)
                if np.random.random() < 0.7:
                    # Dunkler "Void"-Effekt
                    void_spot_color = np.zeros_like(result)
                    void_spot_color[:, :] = [np.random.randint(0, 20), 0, np.random.randint(0, 15)]
                    result = result * (1.0 - spot_mask_3c * 0.9) + void_spot_color * spot_mask_3c * 0.9
                else:
                    # Invertierter Bereich - eine andere Art von "Leere"
                    inverted = 255 - result
                    result = result * (1.0 - spot_mask_3c * 0.7) + inverted * spot_mask_3c * 0.7
            
            # 3. "Löcher" in der Realität - komplett schwarze Stellen
            if np.random.random() < 0.4:
                num_holes = np.random.randint(3, 10)
                for _ in range(num_holes):
                    hole_x = np.random.randint(0, width)
                    hole_y = np.random.randint(0, height)
                    hole_radius = np.random.randint(2, 8)
                    
                    # Zufällige Form (Kreis oder Ellipse)
                    if np.random.random() < 0.5:
                        # Kreis
                        cv2.circle(result, (hole_x, hole_y), hole_radius, (0, 0, 0), -1)
                    else:
                        # Ellipse
                        axes_length = (hole_radius, int(hole_radius * np.random.uniform(0.5, 2.0)))
                        angle = np.random.randint(0, 180)
                        cv2.ellipse(result, (hole_x, hole_y), axes_length, angle, 0, 360, (0, 0, 0), -1)
            
            # 4. Lichtabsorbierende Linien - wie schwarze Adern
            if np.random.random() < 0.6:
                num_lines = np.random.randint(2, 6)
                for _ in range(num_lines):
                    # Startpunkt
                    start_x = np.random.randint(0, width)
                    start_y = np.random.randint(0, height)
                    
                    # Rekursives Zeichnen von verästelten schwarzen Linien
                    self._draw_void_veins(result, start_x, start_y, 0, np.random.randint(3, 7))
            
            # 5. Sättigungsverlust um die Leere herum - Farben werden von der Leere "aufgesogen"
            if np.random.random() < 0.7:
                # Konvertiere zu HSV für einfachere Sättigungsmanipulation
                hsv = cv2.cvtColor(result, cv2.COLOR_BGR2HSV)
                
                # Erstelle eine Maske für Sättigungsverlust - besonders um Void-Bereiche
                saturation_mask = np.zeros((height, width), dtype=np.float32)
                
                # Identifiziere dunkle Bereiche als potenzielle Void-Regionen
                brightness = cv2.cvtColor(result, cv2.COLOR_BGR2GRAY)
                dark_areas = (brightness < 30).astype(np.float32)
                
                # Erweitere die dunklen Bereiche mit einem Blur
                dark_areas = cv2.GaussianBlur(dark_areas, (21, 21), 0)
                
                # Erzeuge einen Sättigungsverlust basierend auf der Nähe zu dunklen Bereichen
                saturation_loss = dark_areas * 0.8  # Wie stark die Sättigung reduziert wird
                
                # Reduziere die Sättigung
                hsv[:,:,1] = hsv[:,:,1] * (1.0 - saturation_loss[:,:])
                
                # Zurück zu BGR
                result = cv2.cvtColor(hsv, cv2.COLOR_HSV2BGR)
            
            return result.astype(np.uint8)
        
        except Exception as e:
            print(f"Fehler bei Void Intrusion: {e}")
            return frame
        
    def _draw_void_veins(self, image, x, y, depth, max_depth):
        """Zeichnet rekursiv verästelte schwarze "Adern" der Leere"""
        if depth >= max_depth or not (0 <= x < image.shape[1] and 0 <= y < image.shape[0]):
            return
        
        # Zeichne eine schwarze Linie in eine zufällige Richtung
        length = np.random.randint(10, 40)
        angle = np.random.uniform(0, 2 * np.pi)
        end_x = int(x + length * np.cos(angle))
        end_y = int(y + length * np.sin(angle))
        
        # Begrenze auf Bildgrenzen
        end_x = max(0, min(image.shape[1]-1, end_x))
        end_y = max(0, min(image.shape[0]-1, end_y))
        
        # Zeichne die Linie mit variierender Dicke
        thickness = max(1, 3 - depth)
        cv2.line(image, (x, y), (end_x, end_y), (0, 0, 0), thickness)
        
        # Verzweigungen
        num_branches = np.random.randint(1, 4)
        for _ in range(num_branches):
            # Startpunkt der Verzweigung etwas entlang der Linie
            t = np.random.uniform(0.3, 1.0)
            branch_x = int(x + t * (end_x - x))
            branch_y = int(y + t * (end_y - y))
            
            # Rekursiver Aufruf für die Verzweigung
            self._draw_void_veins(image, branch_x, branch_y, depth + 1, max_depth)





    def _corrupt_reality(self, frame: np.ndarray) -> np.ndarray:
        """Korruption der Bildwirklichkeit durch digitale Glitches und Realitätsverschiebungen"""
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Zufällige Intensität des Effekts
            corruption_intensity = np.random.uniform(0.2, 0.8)
            
            # 1. Digitale Glitches - Pixelverschiebungen
            num_glitches = np.random.randint(3, 12)
            for _ in range(num_glitches):
                # Zufällige Position und Größe für Glitch
                glitch_height = np.random.randint(10, height // 4)
                glitch_y = np.random.randint(0, height - glitch_height)
                
                # Horizontale Verschiebung in diesem Streifen
                shift_amount = np.random.randint(5, width // 3)
                shift_direction = 1 if np.random.random() > 0.5 else -1
                
                # Verschiebe Pixel
                result[glitch_y:glitch_y+glitch_height, :] = np.roll(
                    result[glitch_y:glitch_y+glitch_height, :], 
                    shift_amount * shift_direction, 
                    axis=1
                )
                
            # 2. Farbkanal-Verzerrung
            if np.random.random() < 0.7:
                # Spalte RGB-Kanäle
                b, g, r = cv2.split(result)
                
                # Verschiebe RGB-Kanäle unterschiedlich
                shift_r = np.random.randint(5, 20)
                shift_g = np.random.randint(5, 20)
                
                # Wende Verschiebungen auf Kanäle an
                r = np.roll(r, shift_r, axis=1)
                g = np.roll(g, shift_g, axis=0)
                
                # Kombiniere Kanäle wieder
                result = cv2.merge([b, g, r])
                
            # 3. Pixelierung bestimmter Bereiche
            num_pixelated = np.random.randint(2, 6)
            for _ in range(num_pixelated):
                # Zufälliger Bereich
                px_x = np.random.randint(0, width - 100)
                px_y = np.random.randint(0, height - 100)
                px_w = np.random.randint(50, 150)
                px_h = np.random.randint(50, 150)
                
                # Begrenze auf Bildgröße
                px_w = min(px_w, width - px_x)
                px_h = min(px_h, height - px_y)
                
                if px_w > 0 and px_h > 0:
                    # Extrahiere Region
                    region = result[px_y:px_y+px_h, px_x:px_x+px_w].copy()
                    
                    # Pixelierung durch Downsample und Upsample
                    pixel_size = np.random.randint(5, 20)
                    small = cv2.resize(region, (px_w//pixel_size, px_h//pixel_size))
                    pixelated = cv2.resize(small, (px_w, px_h), interpolation=cv2.INTER_NEAREST)
                    
                    # Zurück in das Bild einsetzen
                    result[px_y:px_y+px_h, px_x:px_x+px_w] = pixelated
            
            # 4. Zeilenverschiebung (wie bei einem TV-Signal)
            if np.random.random() < 0.5:
                num_shifted_lines = np.random.randint(10, 50)
                for _ in range(num_shifted_lines):
                    line_y = np.random.randint(0, height)
                    shift_amount = np.random.randint(10, width // 2)
                    if 0 <= line_y < height:
                        result[line_y, :] = np.roll(result[line_y, :], shift_amount, axis=0)
            
            # 5. Rauschen hinzufügen
            noise = np.random.normal(0, 25 * corruption_intensity, result.shape).astype(np.int16)
            result = np.clip(result.astype(np.int16) + noise, 0, 255).astype(np.uint8)
            
            return result
            
        except Exception as e:
            print(f"Fehler bei Realitätskorruption: {e}")
            return frame


    def _apply_reality_breach(self, frame: np.ndarray) -> np.ndarray:
        try:
            height, width = frame.shape[:2]
            result = frame.copy()
            
            # Speichere Koordinaten des Risses für spätere Verwendung
            self.breach_coordinates = []
            
            # Erstelle eine Art "Riss" mit zufälligen Eigenschaften
            breach_points = []
            
            # Startpunkt des Risses (zufällig am Bildrand)
            side = np.random.randint(0, 4)  # 0: oben, 1: rechts, 2: unten, 3: links
            if side == 0:
                start_x, start_y = np.random.randint(0, width), 0
            elif side == 1:
                start_x, start_y = width-1, np.random.randint(0, height)
            elif side == 2:
                start_x, start_y = np.random.randint(0, width), height-1
            else:
                start_x, start_y = 0, np.random.randint(0, height)
                
            # Zielpunkt (tendiert zur Bildmitte)
            target_x = width // 2 + np.random.randint(-width//4, width//4)
            target_y = height // 2 + np.random.randint(-height//4, height//4)
            
            # Generiere eine chaotische Rissform mit fraktalen Eigenschaften
            current_x, current_y = start_x, start_y
            breach_points.append((int(current_x), int(current_y)))
            self.breach_coordinates.append((int(current_x), int(current_y)))
            
            # Chaos-Parameter für den Riss
            chaos_factor = np.random.uniform(0.3, 0.8)
            num_segments = np.random.randint(10, 30)
            
            for i in range(num_segments):
                # Berechne Richtung zum Ziel
                dx = target_x - current_x
                dy = target_y - current_y
                
                # Normalisiere und füge Chaos hinzu
                length = np.sqrt(dx*dx + dy*dy)
                if length > 0:
                    dx /= length
                    dy /= length
                    
                # Zufälliger Winkel mit Tendenz zum Ziel
                angle = np.arctan2(dy, dx)
                angle += np.random.uniform(-np.pi/3, np.pi/3) * chaos_factor
                
                # Segmentlänge
                segment_length = np.random.randint(10, max(11, int(min(width, height) * 0.1)))
                
                # Neuer Punkt
                current_x += segment_length * np.cos(angle)
                current_y += segment_length * np.sin(angle)
                
                # Begrenze auf Bildgrenzen
                current_x = max(0, min(width-1, current_x))
                current_y = max(0, min(height-1, current_y))
                
                breach_points.append((int(current_x), int(current_y)))
                self.breach_coordinates.append((int(current_x), int(current_y)))
                
                # In einigen Punkten erstelle Verzweigungen
                if np.random.random() < 0.3 and i > 1:
                    branch_points = []
                    branch_x, branch_y = current_x, current_y
                    
                    # Kleinere Verzweigung
                    branch_segments = np.random.randint(3, max(4, num_segments // 3))
                    branch_angle = angle + np.random.uniform(-np.pi/2, np.pi/2)
                    
                    for j in range(branch_segments):
                        # Zufälliger Winkel mit Tendenz zur Verzweigungsrichtung
                        branch_angle += np.random.uniform(-np.pi/4, np.pi/4) * chaos_factor
                        branch_length = np.random.randint(5, max(6, segment_length))
                        
                        # Neuer Punkt in Verzweigung
                        branch_x += branch_length * np.cos(branch_angle)
                        branch_y += branch_length * np.sin(branch_angle)
                        
                        # Begrenze auf Bildgrenzen
                        branch_x = max(0, min(width-1, branch_x))
                        branch_y = max(0, min(height-1, branch_y))
                        
                        branch_points.append((int(branch_x), int(branch_y)))
                        self.breach_coordinates.append((int(branch_x), int(branch_y)))
                    
                    # Zeichne Verzweigung
                    if len(branch_points) > 1:
                        branch_points = np.array(branch_points, dtype=np.int32)
                        # Hauptriss: weiß mit Farbabweichungen
                        branch_color = (
                            np.random.randint(100, 256),  # Blau
                            np.random.randint(50, 200),   # Grün
                            np.random.randint(50, 150)    # Rot
                        )
                        cv2.polylines(result, [branch_points], False, branch_color, 
                                        thickness=np.random.randint(1, 4))
            
            # Zeichne den Hauptriss als Polylinie
            breach_points = np.array(breach_points, dtype=np.int32)
            rip_width = np.random.randint(3, 9)
            
            # Hauptriss: weiß mit Farbabweichungen
            rip_color = (
                np.random.randint(200, 256),  # Blau
                np.random.randint(200, 256),  # Grün
                np.random.randint(150, 256)   # Rot
            )
            cv2.polylines(result, [breach_points], False, rip_color, thickness=rip_width)
            
            # Effekt-Overlay für außergewöhnliche Erscheinung
            overlay = np.zeros_like(result)
            
            # Aura um den Riss herum
            for point in self.breach_coordinates:
                x, y = point
                
                # Sicherstellen, dass wir im Bild bleiben
                if 0 <= x < width and 0 <= y < height:
                    # Radius für Gloweffekt
                    glow_radius = np.random.randint(5, 20)
                    
                    # Farbe der Aura - unnatürliche Farben für den Realitätsbruch
                    glow_color = (
                        np.random.randint(0, 256),  # B
                        np.random.randint(0, 256),  # G
                        np.random.randint(0, 256)   # R
                    )
                    
                    # Zeichne leuchtende Aura
                    cv2.circle(overlay, (x, y), glow_radius, glow_color, -1)
            
            # Wende Gaussian Blur auf die Aura an
            overlay = cv2.GaussianBlur(overlay, (21, 21), 0)
            
            # Überlagerung mit Frame
            result = cv2.addWeighted(result, 0.7, overlay, 0.5, 0)
            
            # Verzerrungseffekte in der Nähe des Risses
            if len(self.breach_coordinates) > 0:
                # Erstelle eine Verzerrungsmaske
                distortion_map_x = np.zeros((height, width), np.float32)
                distortion_map_y = np.zeros((height, width), np.float32)
                
                # Initialisiere mit normaler Gitterstruktur
                for y in range(height):
                    for x in range(width):
                        distortion_map_x[y, x] = x
                        distortion_map_y[y, x] = y
                
                # Füge Verzerrung in der Nähe des Risses hinzu
                for point in self.breach_coordinates:
                    px, py = point
                    
                    # Bereich um den Punkt
                    radius = np.random.randint(10, 50)
                    strength = np.random.uniform(5.0, 15.0)
                    
                    # Verzerrung hinzufügen
                    for y in range(max(0, py-radius), min(height, py+radius+1)):
                        for x in range(max(0, px-radius), min(width, px+radius+1)):
                            # Abstand zum Risspunkt
                            distance = np.sqrt((x-px)**2 + (y-py)**2)
                            
                            if distance < radius:
                                # Stärke der Verzerrung basierend auf Abstand
                                factor = (1.0 - distance/radius) * strength
                                
                                # Winkel vom Risspunkt zum aktuellen Pixel
                                angle = np.arctan2(y-py, x-px)
                                
                                # Verzerrung hinzufügen (twisted)
                                distortion_map_x[y, x] += factor * np.cos(angle + np.pi/2)
                                distortion_map_y[y, x] += factor * np.sin(angle + np.pi/2)
                
                # Wende Verzerrungskarte an
                result = cv2.remap(result, distortion_map_x, distortion_map_y, 
                                    cv2.INTER_LINEAR, borderMode=cv2.BORDER_REFLECT)
            
            # Farbanomalien - punktuelle chromatische Aberrationen
            if np.random.random() < 0.7:
                # Kanäle trennen
                b, g, r = cv2.split(result)
                
                # Verschiebe die Kanäle leicht
                for point in self.breach_coordinates:
                    x, y = point
                    radius = np.random.randint(20, 60)
                    
                    # Bereich um den Risspunkt
                    roi_x_start = max(0, x - radius)
                    roi_x_end = min(width, x + radius)
                    roi_y_start = max(0, y - radius)
                    roi_y_end = min(height, y + radius)
                    
                    if roi_x_end > roi_x_start and roi_y_end > roi_y_start:
                        # Extrahiere ROI für jeden Kanal
                        roi_b = b[roi_y_start:roi_y_end, roi_x_start:roi_x_end].copy()
                        roi_g = g[roi_y_start:roi_y_end, roi_x_start:roi_x_end].copy()
                        roi_r = r[roi_y_start:roi_y_end, roi_x_start:roi_x_end].copy()
                        
                        # Verschiebe die Kanäle
                        shift_x_r = np.random.randint(-5, 6)
                        shift_y_r = np.random.randint(-5, 6)
                        shift_x_g = np.random.randint(-5, 6)
                        shift_y_g = np.random.randint(-5, 6)
                        
                        # Transformationsmatrizen
                        M_r = np.float32([[1, 0, shift_x_r], [0, 1, shift_y_r]])
                        M_g = np.float32([[1, 0, shift_x_g], [0, 1, shift_y_g]])
                        
                        # Wende Verschiebung an
                        roi_height, roi_width = roi_r.shape[:2]
                        if roi_height > 0 and roi_width > 0:
                            roi_r_shifted = cv2.warpAffine(roi_r, M_r, (roi_width, roi_height))
                            roi_g_shifted = cv2.warpAffine(roi_g, M_g, (roi_width, roi_height))
                            
                            # Setze zurück in die Kanäle
                            r[roi_y_start:roi_y_end, roi_x_start:roi_x_end] = roi_r_shifted
                            g[roi_y_start:roi_y_end, roi_x_start:roi_x_end] = roi_g_shifted
                
                # Kombiniere die Kanäle wieder
                result = cv2.merge([b, g, r])
            
            # Füge unnatürliche Pixel-Anomalien hinzu (digital glitch look)
            if np.random.random() < 0.5:
                num_glitches = np.random.randint(3, 10)
                for _ in range(num_glitches):
                    # Wähle einen zufälligen Punkt in der Nähe des Risses
                    if len(self.breach_coordinates) > 0:
                        point_idx = np.random.randint(0, len(self.breach_coordinates))
                        center_x, center_y = self.breach_coordinates[point_idx]
                        
                        # Größe des Glitch-Bereichs
                        glitch_width = np.random.randint(10, 50)
                        glitch_height = np.random.randint(10, 50)
                        
                        # Positionieren mit Offset
                        pos_x = max(0, min(width - glitch_width, center_x + np.random.randint(-30, 31)))
                        pos_y = max(0, min(height - glitch_height, center_y + np.random.randint(-30, 31)))
                        
                        # Extrahiere Region
                        if pos_x + glitch_width <= width and pos_y + glitch_height <= height:
                            region = result[pos_y:pos_y+glitch_height, pos_x:pos_x+glitch_width].copy()
                            
                            # Art des Glitches
                            glitch_type = np.random.random()
                            
                            if glitch_type < 0.33:
                                # Pixelverschiebung
                                shift = np.random.randint(5, 20)
                                region = np.roll(region, shift, axis=1)
                            elif glitch_type < 0.66:
                                # Farbkanalmanipulation
                                channel = np.random.randint(0, 3)
                                region[:,:,channel] = np.random.randint(0, 256)
                            else:
                                # Pixelierung
                                scale = np.random.randint(5, 15)
                                small = cv2.resize(region, (glitch_width//scale, glitch_height//scale))
                                region = cv2.resize(small, (glitch_width, glitch_height), 
                                                    interpolation=cv2.INTER_NEAREST)
                            
                            # Setze manipulierte Region zurück
                            result[pos_y:pos_y+glitch_height, pos_x:pos_x+glitch_width] = region
            
            return result
    
        except Exception as e:
            print(f"Fehler bei Reality-Breach-Effekt: {e}")
            return frame

    






# -------------- Audio-Glitch-Effekte --------------

class AudioGlitchEffectStrategy(IEffectStrategy):
    """Implementierung eines Audio+Video Glitch-Effekts"""
    
    def __init__(
        self, 
        audio_capture_service: IAudioCaptureService,
        audio_playback_service: IAudioPlaybackService
    ):
        self.audio_capture = audio_capture_service
        self.audio_playback = audio_playback_service
        self.last_frame = None
        self.is_effect_active = False
        
        # Audio-Effekt-Parameter
        self.buffer_size = 5  # Puffergröße für Audio-Chunks
        self.audio_buffer = []
        self.glitch_prob = 0.3  # Wahrscheinlichkeit für Audio-Glitches
        self.last_glitch_time = 0
        self.glitch_cooldown = 0.5  # Minimale Zeit zwischen Audio-Glitches
        
    @property
    def name(self) -> str:
        return "Audio+Video Glitch"
    
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet Audio+Video Glitch-Effekte an"""
        current_time = time.time()
        
        # Beim ersten Aufruf oder beim Starten des Effekts
        if not self.is_effect_active:
            self.audio_capture.start_capture()
            self.is_effect_active = True
            
        # Video-Effekte (ähnlich wie im RealTimeGlitchEffectStrategy)
        if self.last_frame is None:
            self.last_frame = frame.copy()
            
        # Kopie des aktuellen Frames erstellen
        glitched = frame.copy()
            
        # Zufallsgenerator mit aktuellem Timestamp initialisieren
        np.random.seed(int(current_time * 1000) % 10000)
        
        # Verschiedene visuelle Glitch-Effekte anwenden
        height, width = frame.shape[:2]
        
        # RGB-Kanalverschiebung
        if np.random.random() < 0.5:
            b, g, r = cv2.split(glitched)
            
            r_offset_x = int(np.random.randint(-10, 10))
            r_offset_y = int(np.random.randint(-5, 5))
            
            r_shift = np.float32([[1, 0, r_offset_x], [0, 1, r_offset_y]])
            r = cv2.warpAffine(r, r_shift, (width, height), borderMode=cv2.BORDER_WRAP)
            
            glitched = cv2.merge([b, g, r])
        
        # Audio-Verarbeitung
        audio_chunk = self.audio_capture.get_audio_chunk()
        
        # Audio-Chunk zum Puffer hinzufügen
        if len(audio_chunk) > 0:
            self.audio_buffer.append(audio_chunk.copy())
            if len(self.audio_buffer) > self.buffer_size:
                self.audio_buffer.pop(0)
        
        # Audio-Glitch-Effekte anwenden, wenn genug Audio im Puffer ist
        if len(self.audio_buffer) > 0:
            # Chunk zum Abspielen auswählen (zufällig aus dem Puffer oder aktuell)
            if np.random.random() < self.glitch_prob and current_time - self.last_glitch_time > self.glitch_cooldown:
                # Einen Audio-Glitch anwenden
                chunk_to_play = self.audio_buffer[np.random.randint(0, len(self.audio_buffer))]
                
                # Zufällig verschiedene Audio-Glitch-Effekte anwenden
                glitch_type = np.random.randint(0, 5)
                
                if glitch_type == 0:
                    # Umkehrung (Reverse)
                    chunk_to_play = np.flipud(chunk_to_play)
                elif glitch_type == 1:
                    # Bit-Crushing (Quantisierung reduzieren)
                    bits = np.random.randint(2, 8)
                    chunk_to_play = np.round(chunk_to_play * (2**bits)) / (2**bits)
                elif glitch_type == 2:
                    # Stottern / Wiederholung
                    repeat_count = np.random.randint(2, 5)
                    slice_pos = len(chunk_to_play) // 2
                    slice_size = len(chunk_to_play) // 10
                    slice_to_repeat = chunk_to_play[slice_pos:slice_pos+slice_size]
                    for i in range(repeat_count):
                        pos = slice_pos + i*slice_size
                        if pos + slice_size < len(chunk_to_play):
                            chunk_to_play[pos:pos+slice_size] = slice_to_repeat
                elif glitch_type == 3:
                    # Frequenz-Verzerrung (Pitch-Shift)
                    factor = np.random.uniform(0.8, 1.2)
                    chunk_to_play = signal.resample(chunk_to_play, int(len(chunk_to_play) * factor))
                    # Anpassen der Größe auf die ursprüngliche Länge
                    if len(chunk_to_play) > len(audio_chunk):
                        chunk_to_play = chunk_to_play[:len(audio_chunk)]
                    else:
                        pad_size = len(audio_chunk) - len(chunk_to_play)
                        chunk_to_play = np.pad(chunk_to_play, ((0, pad_size), (0, 0)))
                else:
                    # Rauschen hinzufügen
                    noise = np.random.normal(0, 0.1, chunk_to_play.shape)
                    chunk_to_play = np.clip(chunk_to_play + noise, -1.0, 1.0)
                    
                self.last_glitch_time = current_time
            else:
                # Normalen Audio-Chunk verwenden
                chunk_to_play = audio_chunk
                
            # Audio abspielen
            self.audio_playback.play_audio(chunk_to_play)
        
        # Aktuellen Frame für den nächsten Durchlauf speichern
        self.last_frame = glitched.copy()
        
        return glitched
        
    def __del__(self):
        """Aufräumen beim Löschen des Objekts"""
        self.audio_capture.stop_capture()
        self.audio_playback.stop_playback()

class SlowMotionSnapshotEffectStrategy(IEffectStrategy):
    """Effekt, der nur alle halbe Sekunde ein neues Standbild zeigt"""
    
    def __init__(self):
        self.last_update_time = 0
        self.current_frame = None
        self.update_interval = 0.2  # Halbe Sekunde zwischen Updates
    
    @property
    def name(self) -> str:
        return "Slow Motion Snapshot"
    
    def apply(self, frame: np.ndarray, config: Dict[str, Any]) -> np.ndarray:
        """Wendet den Slow Motion Snapshot-Effekt an"""
        current_time = time.time()
        
        # Bei erstem Aufruf oder nach Ablauf des Intervalls neues Bild speichern
        if (self.current_frame is None or 
            current_time - self.last_update_time >= self.update_interval):
            self.current_frame = frame.copy()
            self.last_update_time = current_time
            
            # Optionales visuelles Feedback über die Aktualisierung
            # z.B. einen kleinen roten Punkt in der Ecke für 0.1s
            cv2.circle(self.current_frame, (20, 20), 10, (255, 0, 0), -1)
        
        return self.current_frame


# ----------------- GUI -----------------

class EffectView:
    """Die Benutzeroberfläche der Anwendung"""
    
    def __init__(self, event_bus: IEventBus):
        self.event_bus = event_bus
        self.root = tk.Tk()
        self.root.title("Bildeffekte")
        
        # GUI-Komponenten initialisieren
        self._init_ui()
        
        # Event-Abonnements einrichten
        self.event_bus.subscribe('status_update', self.update_status)
        self.event_bus.subscribe('frame_processed', self.update_frame)
        
        # Schließen-Handler konfigurieren
        self.root.protocol("WM_DELETE_WINDOW", self.on_closing)
    
    def _init_ui(self) -> None:
        """Initialisiert die UI-Komponenten"""
        # Konfigurationsbereich
        self.config_frame = tk.Frame(self.root)
        self.config_frame.pack(pady=10)
        
        tk.Label(self.config_frame, text="Bereich (x,y,breite,höhe):").grid(row=0, column=0, padx=5)
        self.region_entry = tk.Entry(self.config_frame, width=20)
        self.region_entry.insert(0, "100,100,800,450")  # Standardwerte
        self.region_entry.grid(row=0, column=1, padx=5)
        
        self.btn_start = tk.Button(self.config_frame, text="Start", command=self.start_effect)
        self.btn_start.grid(row=0, column=2, padx=5)
        
        self.btn_next = tk.Button(self.config_frame, text="Nächste Zelle", command=self.next_cell, state=tk.DISABLED)
        self.btn_next.grid(row=0, column=3, padx=5)
        
        self.btn_stop = tk.Button(self.config_frame, text="Stop", command=self.stop_effect, state=tk.DISABLED)
        self.btn_stop.grid(row=0, column=4, padx=5)
        
        # Effekt-Auswahl
        self.effect_frame = tk.Frame(self.root)
        self.effect_frame.pack(pady=5)
        
        tk.Label(self.effect_frame, text="Effekt:").grid(row=0, column=0, padx=5)
        self.effect_var = tk.StringVar(value="Mosaik")
        self.effect_dropdown = tk.OptionMenu(self.effect_frame, self.effect_var, "Mosaik", "Blur", "Kantenerkennung",   "Slow Motion Snapshot","Realtime Glitch"  , "Audio+Video Glitch","VHS Störung", "Uralter Film (1910er)" , "Glitch-Dimension")
        self.effect_dropdown.grid(row=0, column=1, padx=5)
        self.effect_var.trace_add("write", self.change_effect)
        
        # Status
        self.status_var = tk.StringVar()
        self.status_var.set("Bereit. Bitte Region eingeben und Start klicken.")
        self.status_label = tk.Label(self.root, textvariable=self.status_var, bd=1, relief=tk.SUNKEN, anchor=tk.W)
        self.status_label.pack(side=tk.BOTTOM, fill=tk.X)
        
        # Canvas für die Vorschau
        self.canvas = tk.Canvas(self.root, bg='black', width=800, height=450)
        self.canvas.pack(expand=True, fill=tk.BOTH, padx=10, pady=10)
    
    def run(self) -> None:
        """Startet die Anwendung"""
        self.root.geometry("820x550")  # Startgröße
        self.root.mainloop()
    
    def start_effect(self) -> None:
        """Startet den Effekt"""
        try:
            # Region parsen
            coords = self.region_entry.get().strip().split(',')
            if len(coords) != 4:
                raise ValueError("Format muss sein: x,y,breite,höhe")
            
            x, y, width, height = map(int, coords)
            region = {'left': x, 'top': y, 'width': width, 'height': height}
            
            # Canvas-Größe anpassen
            self.canvas.config(width=width, height=height)
            
            # Buttons aktualisieren
            self.btn_start.config(state=tk.DISABLED)
            self.btn_next.config(state=tk.NORMAL)
            self.btn_stop.config(state=tk.NORMAL)
            
            # Event veröffentlichen
            self.event_bus.publish('start_capture', region)
            
        except ValueError as e:
            self.status_var.set(f"Fehler: {e}")
    
    def stop_effect(self) -> None:
        """Stoppt den Effekt"""
        # Buttons aktualisieren
        self.btn_start.config(state=tk.NORMAL)
        self.btn_next.config(state=tk.DISABLED)
        self.btn_stop.config(state=tk.DISABLED)
        
        # Event veröffentlichen
        self.event_bus.publish('stop_capture')
    
    def next_cell(self) -> None:
        """Wechselt zur nächsten Zelle"""
        self.event_bus.publish('next_cell')
    
    def change_effect(self, *args) -> None:
        """Ändert den aktiven Effekt"""
        effect_name = self.effect_var.get()
        self.event_bus.publish('change_effect', effect_name)
    
    def update_status(self, message: str) -> None:
        """Aktualisiert die Statusanzeige"""
        self.status_var.set(message)
    
    def update_frame(self, frame: np.ndarray) -> None:
        """Aktualisiert das angezeigte Bild"""
        img = Image.fromarray(frame)
        img_tk = ImageTk.PhotoImage(image=img)
        
        # Canvas aktualisieren
        self.canvas.delete("all")
        self.canvas.create_image(0, 0, anchor=tk.NW, image=img_tk)
        self.canvas.image = img_tk  # Referenz behalten!
    
    def on_closing(self) -> None:
        """Aufräumen beim Schließen"""
        self.event_bus.publish('stop_capture')
        self.root.destroy()

# ----------------- Anwendungsstart -----------------

def main():
    # DI Container konfigurieren
    Container.register(IEventBus, EventBus)
    Container.register(IScreenCaptureService, MssScreenCaptureService)
    Container.register(IAudioCaptureService, SystemAudioCaptureService)
    Container.register(IAudioPlaybackService, SystemAudioPlaybackService)

    # Komponenten erstellen
    event_bus = Container.resolve(IEventBus)
    
    # Effekt-Processor erstellen und Effekte registrieren
    effect_processor = EffectProcessor(
        Container.resolve(IScreenCaptureService), 
        event_bus
    )
    effect_processor.register_effect(MosaicEffectStrategy())
    effect_processor.register_effect(BlurEffectStrategy())
    effect_processor.register_effect(EdgeDetectionEffectStrategy())
    effect_processor.register_effect(SlowMotionSnapshotEffectStrategy())  # Neuer Effekt
    effect_processor.register_effect(RealTimeGlitchEffectStrategy())  
    effect_processor.register_effect(VHSGlitchEffectStrategy())  # Neuer VHS-Effekt
    effect_processor.register_effect(OldFilmEffectStrategy())  # Neuer Oldfilm Effekt
    effect_processor.register_effect(GlitchDimensionEffectStrategy()) 

  # Audio+Video Glitch-Effekt registrieren
    audio_glitch = AudioGlitchEffectStrategy(
        Container.resolve(IAudioCaptureService),
        Container.resolve(IAudioPlaybackService)
    )
    effect_processor.register_effect(audio_glitch)


    # View erstellen und starten
    view = EffectView(event_bus)
    view.run()

if __name__ == "__main__":
    main()