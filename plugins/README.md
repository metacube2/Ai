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
| `31_video_vhs.py` | VHS Tracking-Riss, VHS Farbbluten, VHS Kopierkassette, VHS Pause |
| `35_video_contour_lab.py` | Kontur-Magnetfeld, Silhouetten-Wellen, Lichtskulptur, Kontur-Partikel |
| `36_video_liquid_metal.py` | Flüssigmetall-Schmelze: lokale Personenmaske, Gesichtsrelief, räumliche Chromreflexionen und Schmelze |
| `37_video_liquid_metal_v2.py` | Flüssigmetall-Schmelze II (Kern: `bildeffekte/verwandlung.py`, siehe `docs/bildeffekte/verwandlungen.md`) |
| `38_video_verwandlung_material.py` | Flüssiggold, Flüssigglas, Lava-Schmelze, Honig-Schmelze (Strömung wie Flüssigmetall II, eigene Oberfläche und Stoffwerte) |
| `39_video_verwandlung_spezial.py` | Aus der Pfütze, Zerfall zu Asche, Eis & Zerspringen, Formwandler, Predator-Tarnung (live) |
| `40_video_dimension.py` | Glitch-Dimension |
| `50_video_matrix.py` | Matrix-Code |
| `51_video_matrix_agent.py` | Matrix-Agent – Gesichtsmorph: lokale Gesichtspunkte, optischer Fluss, fotografisches Standardziel und eigene JPG-Ziele |
| `60_video_space.py` | Kaleidoskop-Tunnel, Zeitwelle, Droste-Spirale |
| `70_video_nature.py` | Sand-Zerfall, Bio-Wucherung |
| `80_video_audio_reactive.py` | Audio+Video Glitch, Klang-Beben |
| `90_audio_spectral.py` | Bild-Schablone, Audio-Datamosh, Farb-Vokale, Negativ-Ton, Harmonie-Magnet, Schwarzes Loch, Traum-Gedächtnis |
| `91_audio_time.py` | Vorahnung, Takt-Zwang, Glas-Kristallisation, Antimaterie, Klang-Schwarm |
| `92_audio_phosphor.py` | Phosphor-Nachleuchten |
| `93_audio_experimental.py` | Frequenz-Origami, Spektral-Regen, Phasen-Wirbel, Puls-Mosaik, Harmonische Fata Morgana, Zeit-Iris |

## Bild-Effekt

Flüssigmetall nutzt auf macOS optional `bildeffekte/portrait.py` mit Apple Vision
(`pip install -r bildeffekte/requirements-macos.txt`). Die einmalige Analyse
läuft im Hintergrund; ohne Vision bleibt eine Kontrastmaske als Rückfall.

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

Bild-Effekte können optional `category = "…"` setzen. Jede Kategorie erscheint als
Untermenü der Effekt-Auswahl (z. B. `"Glitch"`, `"Retro & VHS"`, `"Verwandlungen"`);
ohne Angabe steht der Effekt auf der obersten Ebene. `category = "Kontur-Labor"`
bekommt eine eigene Sektion. `supports_drag = True` aktiviert das Ziehen in der
Vorschau; die letzte Mausbewegung steht als `(x, y, dx, dy)` in
`config['warp_drag']`, mit normierten Koordinaten und `None` nach dem Loslassen.

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


## Optionales Zielbild

Ein Bild-Plugin kann `supports_target_image = True` und
`prepare_target_image(path)` anbieten. Die Oberfläche zeigt dann eine JPG-Auswahl
und ruft die Vorbereitung im Hintergrund auf. Die Methode liefert ein fertig
validiertes Ziel oder meldet per `ValueError` einen verständlichen Fehler.
`apply()` liest die Auswahl unter `config['target_images'].get(self.name)`;
`None` bedeutet Standardziel. Die Auswahl wird über Stop/Start, Effektwechsel
und Plugin-Neuladen hinweg erhalten, solange die App läuft. Nach einem Neustart
gilt wieder das Standardziel. Fehler beim Laden oder bei der Gesichtserkennung
ersetzen eine gültige Auswahl nicht. `prepare_target_image()` soll Laden und
Gesichtserkennung außerhalb von `apply()` erledigen, damit der Aufnahme-Thread
nicht auf Dateizugriff wartet. Beispiel: `51_video_matrix_agent.py`.
