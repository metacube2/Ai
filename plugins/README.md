# Plugins

**Alle** Effekte sind Plugins: Jede `.py`-Datei in diesem Ordner (oder in
`~/.bildeffekte/plugins`) bringt Effekte mit. Beim Start werden sie automatisch
geladen, im laufenden Programm über den Knopf **🧩 Plugins neu laden**.

Ein Plugin kennt nur das Paket `bildeffekte` (Schnittstellen, Hilfsmittel,
Container) – niemals `video.py`. Umgekehrt kennt das Programm keine einzelne
Effektklasse.

Dateien, die mit `_` beginnen, werden übersprungen. Die Zahl am Dateianfang
bestimmt nur die Reihenfolge in der Auswahlliste.

## Mitgelieferte Dateien

| Datei | Inhalt |
|---|---|
| `10_video_basic.py` | Mosaik, Blur, Kantenerkennung, Slow Motion Snapshot |
| `20_video_glitch.py` | Realtime Glitch, Datamosh, Pixel-Sorting |
| `30_video_retro.py` | VHS Störung, Uralter Film (1910er) |
| `40_video_dimension.py` | Glitch-Dimension |
| `50_video_matrix.py` | Matrix-Code |
| `60_video_space.py` | Kaleidoskop-Tunnel, Zeitwelle, Droste-Spirale |
| `70_video_nature.py` | Sand-Zerfall, Bio-Wucherung |
| `80_video_audio_reactive.py` | Audio+Video Glitch, Klang-Beben |
| `90_audio_spectral.py` | Bild-Schablone, Audio-Datamosh, Farb-Vokale, Negativ-Ton, Harmonie-Magnet, Schwarzes Loch, Traum-Gedächtnis |
| `91_audio_time.py` | Vorahnung, Takt-Zwang, Glas-Kristallisation, Antimaterie, Klang-Schwarm |
| `92_audio_phosphor.py` | Phosphor-Nachleuchten |

## Bild-Effekt

```python
import cv2
import numpy as np
from bildeffekte import IEffectStrategy


class InvertEffect(IEffectStrategy):
    @property
    def name(self) -> str:
        return "Negativ"                      # Name in der Auswahlliste

    def apply(self, frame, config):           # frame: RGB uint8, H x B x 3
        return 255 - frame                    # gleiche Form und gleicher Typ zurück
```

## Ton-Effekt

Entweder direkt von `IAudioEffect` erben (Zeitbereich) oder von `StftAudioEffect`
(Frequenzbereich, 2048er Fenster mit 4-facher Überlappung):

```python
import numpy as np
from bildeffekte import StftAudioEffect


class TelephoneEffect(StftAudioEffect):
    description = "Kurze Beschreibung für den Tooltip"

    @property
    def name(self) -> str:
        return "Telefon"

    def on_reset(self) -> None:               # optional: Zustand aufbauen
        self._gain = ((self.freqs > 300) & (self.freqs < 3400)).astype(float)

    def process_frame(self, mag, phase, video):
        return mag * self._gain, phase        # video: Bildmerkmale, siehe unten
```

## Das Bild steuert den Ton

`process_frame(...)` und `process(block, video)` bekommen eine Momentaufnahme der
Bildmerkmale: `video.valid`, `row_profile` (Helligkeit je Bildzeile),
`brightness`, `hue`, `saturation`, `motion_x`, `motion_y`, `activity`,
`cut_count`, `cut_times`. Läuft gerade kein Video, ist `valid` False.

## Dienste anfordern

Braucht ein Effekt einen Dienst, genügt eine Typangabe im Konstruktor:

```python
from bildeffekte import IEffectStrategy, VideoFeatures, AudioAnalyzer, IAudioLoopService


class MyEffect(IEffectStrategy):
    def __init__(self, features: VideoFeatures):
        self.features = features
```

Verfügbar sind unter anderem `VideoFeatures`, `AudioAnalyzer` (Ton-Merkmale für
Bild-Effekte), `IAudioLoopService` und `IEventBus`.

## Hinweise

- Ein Bild-Effekt sollte unter 30 ms pro Bild bleiben (30 fps), ein Ton-Effekt
  unter 10 ms pro Block (512 Samples bei 48 kHz).
- Fehler in einem Plugin werden in der Statuszeile gemeldet, das Programm läuft
  weiter.
- Als Vorlage eignen sich `50_video_matrix.py` (Bild) und `92_audio_phosphor.py` (Ton).

## Eigene Dienste registrieren (wie ein Autofac-Module)

```python
from bildeffekte import Container, IEffectStrategy, PluginModule


class MyModule(PluginModule):
    def load(self, container, tag):
        container.register(IMyService, MyService, tag=tag)
        container.register_many(IEffectStrategy, MyEffect, tag=tag)
```

`tag` durchreichen: Damit lässt sich das Plugin sauber wieder entladen.
