"""
bildeffekte.api – Schnittstellen, Datentypen und Container.

Plugins hängen nur an diesem Modul, nie am Programm selbst. Der Container
arbeitet wie ein kleiner Autofac-Builder: Implementierungen werden zu
Schnittstellen registriert, Abhängigkeiten über Typangaben im Konstruktor
aufgelöst. Registrierungen tragen eine Herkunft ("tag"), damit ein Plugin
komplett wieder entladen werden kann.
"""

import inspect
import threading
from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Any, Callable, Dict, List, Optional, Tuple, Type

import numpy as np

APP_TAG = "app"
PROFILE_ROWS = 96          # Auflösung des Helligkeitsprofils über die Bildhöhe
AUDIO_BANDS = 32           # Bänder der Ton-Analyse


@dataclass
class Registration:
    """Eine Registrierung im Container"""
    implementation: Optional[Type] = None
    instance: Any = None
    singleton: bool = True
    tag: str = APP_TAG


class Container:
    """
    Kleiner Dependency-Injection-Container im Stil von Autofac.

    register()           eine Implementierung je Schnittstelle
    register_instance()  fertiges Objekt
    register_many()      mehrere Implementierungen je Schnittstelle (resolve_all)
    resolve()/resolve_all()  Auflösung samt Konstruktor-Injektion
    remove_tag()         alles einer Herkunft entfernen (Plugin entladen)
    """

    _registrations: Dict[Type, Registration] = {}
    _many: Dict[Type, List[Registration]] = {}
    _lock = threading.RLock()

    @classmethod
    def register(cls, interface: Type, implementation: Type, singleton: bool = True,
                 tag: str = APP_TAG) -> None:
        with cls._lock:
            cls._registrations[interface] = Registration(implementation, None, singleton, tag)

    @classmethod
    def register_instance(cls, interface: Type, instance: Any, tag: str = APP_TAG) -> None:
        with cls._lock:
            cls._registrations[interface] = Registration(None, instance, True, tag)

    @classmethod
    def register_many(cls, interface: Type, implementation: Type, singleton: bool = True,
                      tag: str = APP_TAG) -> None:
        """Mehrere Implementierungen zur selben Schnittstelle (Reihenfolge bleibt erhalten)"""
        with cls._lock:
            cls._many.setdefault(interface, []).append(Registration(implementation, None, singleton, tag))

    @classmethod
    def register_instance_many(cls, interface: Type, instance: Any, tag: str = APP_TAG) -> None:
        """Fertiges Objekt zu einer Liste hinzufügen (z. B. ein geprüfter Plugin-Effekt)"""
        with cls._lock:
            cls._many.setdefault(interface, []).append(Registration(None, instance, True, tag))

    @classmethod
    def resolve(cls, interface: Type) -> Any:
        with cls._lock:
            registration = cls._registrations.get(interface)
        if registration is None:
            raise ValueError(f"Keine Registrierung für {getattr(interface, '__name__', interface)} gefunden")
        return cls._instance_of(registration)

    @classmethod
    def resolve_all(cls, interface: Type,
                    on_error: Optional[Callable[[Registration, Exception], None]] = None) -> List[Any]:
        """
        Alle Implementierungen einer Schnittstelle. Mit on_error werden fehlerhafte
        Einträge gemeldet und übersprungen, statt alles scheitern zu lassen.
        """
        with cls._lock:
            registrations = list(cls._many.get(interface, []))
        instances = []
        for registration in registrations:
            try:
                instances.append(cls._instance_of(registration))
            except Exception as e:
                if on_error is None:
                    raise
                on_error(registration, e)
        return instances

    @classmethod
    def remove_tag(cls, tag: str) -> None:
        """Entfernt alle Registrierungen einer Herkunft, z. B. beim Neuladen eines Plugins"""
        with cls._lock:
            cls._registrations = {k: r for k, r in cls._registrations.items() if r.tag != tag}
            for interface, registrations in list(cls._many.items()):
                kept = [r for r in registrations if r.tag != tag]
                if kept:
                    cls._many[interface] = kept
                else:
                    cls._many.pop(interface)

    @classmethod
    def reset(cls) -> None:
        with cls._lock:
            cls._registrations.clear()
            cls._many.clear()

    @classmethod
    def _instance_of(cls, registration: Registration) -> Any:
        if registration.instance is not None:
            return registration.instance
        instance = cls.create(registration.implementation)
        if registration.singleton:
            registration.instance = instance
        return instance

    @classmethod
    def create(cls, implementation: Type) -> Any:
        """Erzeugt ein Objekt und füllt alle Konstruktor-Parameter mit Typangabe"""
        parameters = list(inspect.signature(implementation.__init__).parameters.items())[1:]
        arguments = {}
        for name, parameter in parameters:
            if parameter.annotation is inspect.Parameter.empty:
                continue
            annotation = parameter.annotation
            if isinstance(annotation, str):          # "IEventBus" statt IEventBus
                annotation = cls._by_name(annotation)
            if annotation is None:
                continue
            try:
                arguments[name] = cls.resolve(annotation)
            except ValueError:
                if parameter.default is inspect.Parameter.empty:
                    raise
        return implementation(**arguments)

    @classmethod
    def _by_name(cls, name: str) -> Optional[Type]:
        with cls._lock:
            for interface in list(cls._registrations) + list(cls._many):
                if getattr(interface, '__name__', None) == name:
                    return interface
        return None


class PluginModule(ABC):
    """
    Wie ein Autofac-Module: Ein Plugin kann damit eigene Dienste registrieren,
    statt nur Effekt-Klassen bereitzustellen.
    """

    @abstractmethod
    def load(self, container: Type[Container], tag: str) -> None:
        """Registrierungen vornehmen; `tag` beim Registrieren durchreichen"""


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


class IVideoFeatures(ABC):
    """Liefert Bildmerkmale für Ton-Effekte"""

    enabled: bool = False

    @abstractmethod
    def update(self, frame: np.ndarray) -> None:
        """Merkmale aus einem Bild berechnen (Aufnahme-Thread)"""

    @abstractmethod
    def snapshot(self) -> VideoFeatureSnapshot:
        """Letzter Stand (beliebiger Thread)"""


@dataclass(frozen=True)
class AudioFeatureSnapshot:
    valid: bool = False
    level: float = 0.0          # 0..1, automatisch normiert
    bass: float = 0.0
    mid: float = 0.0
    treble: float = 0.0
    onset_count: int = 0
    bands: Optional[np.ndarray] = None   # 32 Bänder, 0..1


class IAudioFeatures(ABC):
    """Liefert Ton-Merkmale für Bild-Effekte"""

    @abstractmethod
    def snapshot(self) -> AudioFeatureSnapshot:
        """Letzter Stand (beliebiger Thread)"""


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
