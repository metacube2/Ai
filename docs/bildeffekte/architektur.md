# Bildeffekte — Architektur

Stand: 2026-09-22

## Grundsatz

Das Programm kennt keine einzelne Effektklasse, ein Effekt kennt das Programm nicht.
Dazwischen liegt das Paket `bildeffekte` mit Schnittstellen, Hilfsmitteln und einem
DI-Container.

```
plugins/*.py  ──hängt an──>  bildeffekte  <──hängt an──  video.py
```

## Container (Stil Autofac)

`bildeffekte/api.py`

| Methode | Zweck |
| --- | --- |
| `register(I, Impl)` | Eine Implementierung je Schnittstelle |
| `register_instance(I, obj)` | Fertiges Objekt |
| `register_many(I, Impl)` | Mehrere Implementierungen je Schnittstelle |
| `register_instance_many(I, obj)` | Geprüftes Objekt in die Liste |
| `resolve(I)` / `resolve_all(I)` | Auflösen, `resolve_all` überspringt auf Wunsch fehlerhafte Einträge |
| `remove_tag(tag)` | Alles einer Herkunft entfernen (Plugin entladen) |
| `create(Impl)` | Objekt erzeugen und Konstruktor-Parameter nach Typangabe füllen |

Jede Registrierung trägt eine Herkunft: `app` für das Programm,
`plugin:<Dateipfad>` für Plugins.

## Schnittstellen

| Schnittstelle | Implementierung im Programm |
| --- | --- |
| `IEventBus` | `EventBus` (thread-sicher) |
| `IScreenCaptureService` | `MssScreenCaptureService` (mss je Thread, Retina-Absicherung) |
| `IAudioLoopService` | `SoundDeviceLoopService` (Duplex-Stream mit Stufen) |
| `IVideoFeatures` | `VideoFeatures` (Bildmerkmale für Ton-Effekte) |
| `IAudioFeatures` | `AudioAnalyzer` (Ton-Merkmale für Bild-Effekte) |
| `IEffectStrategy` | nur Plugins, plus `PassThroughEffectStrategy` als Rückfallebene |
| `IAudioEffect` | nur Plugins; Basisklasse `StftAudioEffect` im Toolkit |

Die Bildschirmquelle bleibt der Standard. Für Kamera 0–3 öffnet der Aufnahme-Thread
stattdessen `OpenCvCameraCaptureService` über AVFoundation unter macOS. Die Kamera
wird im selben Thread gelesen und beim Beenden freigegeben. Beide Quellen liefern
RGB-Bilder an dieselbe Effekt- und Bildmerkmale-Pipeline.

Bild-Plugins ordnen sich über `category` einer Gruppe zu: Jede Kategorie wird ein
Untermenü der Effekt-Auswahl (`EffectPicker` in `video.py`), die Standardkategorie
„Bild-Effekte“ steht auf der obersten Ebene; auch „Kontur-Labor“ ist ein Untermenü.
Mit `supports_drag`
erhält ein Effekt normierte Mausbewegungen aus der Vorschau über den Event-Bus
als `config['warp_drag']`; im Aufnahme-Thread wird daraus die Bildverformung.

Ein Bild-Plugin mit `supports_target_image = True` erhält die zusätzliche
JPG-Auswahl in der Oberfläche. Der `target-image`-Worker ruft
`prepare_target_image(path)` auf und prüft die Bildvorlage. Erst nach erfolgreicher
Prüfung veröffentlicht die Oberfläche das vorbereitete Ziel über `target_image`.
Der `EffectProcessor` hält die Ziele je Effektname und übernimmt ein neues Mapping
in `config['target_images']`; beim Stop/Start wird es in die neue Aufnahme-Konfiguration
übertragen. Ein Ladefehler lässt das vorherige Ziel aktiv. „Standard-Agent“ entfernt
den Eintrag. Das Matrix-Plugin erkennt Änderungen am Ziel im Aufnahme-Thread und
setzt seinen Morphzustand dort zurück.

## Effektkette

Der `EffectProcessor` hält statt eines Effekts eine **Kette** (`chain`, Liste von
`IEffectStrategy`) und den Index des ausgewählten Glieds (`selected`).
`current_strategy` ist das ausgewählte Glied. Darauf beziehen sich Menü,
Zielbild, Dramatik und Ziehen, deshalb verhält sich eine Kette mit einem Glied
genau wie früher.

- Im Aufnahme-Thread bekommt jedes Glied das Ergebnis des vorherigen. Liefert ein
  Glied eine andere Bildgröße, wird auf die Eingangsgröße skaliert. Wirft es einen
  Fehler, reicht es seine Eingabe unverändert weiter; der Rest der Kette läuft
  weiter, die Statuszeile meldet den Fehler höchstens alle 2 s.
- Die Liste wird nie verändert, sondern unter `_lock` ersetzt. Der Aufnahme-Thread
  liest sie ohne Sperre.
- Ereignisse vom Bus: `change_effect` ersetzt das ausgewählte Glied, `add_effect`
  hängt dahinter an, `remove_slot`, `move_slot` (±1) und `select_slot`. Jede Änderung
  meldet `chain_changed` mit `(Namen, Auswahl)`. Ändert sich das ausgewählte Glied
  oder die Effektmenge, kommt zusätzlich `effect_changed`.
- Jeder Effekt steht höchstens einmal in der Kette, da Effekte Zustand haben
  (Tracking, Einfrieren). Das letzte Glied lässt sich nicht entfernen.
- `activate`/`deactivate` laufen beim Start und Stopp für alle Glieder, bei
  Änderungen im laufenden Betrieb nur für die betroffenen.
- Beim Neuladen der Plugins werden Glieder mit gleichem Namen durch die neue Instanz
  ersetzt, Reihenfolge und Auswahl bleiben. Ein gelöschtes Plugin fällt aus der
  Kette; wäre sie dann leer, rückt der erste verfügbare Effekt nach.
- Aufnahmen heißen nach der ganzen Kette, etwa `…_Matrix-Agent-Gesichtsmorph-VHS-Kopierkassette.mp4`.

## Threads

| Thread | Aufgabe |
| --- | --- |
| **GUI (Haupt)** | Qt, Anzeige, Bedienung. Fasst nur Widgets an |
| **Aufnahme** | Bildschirm oder Kamera lesen, Effektkette anwenden, Bildmerkmale berechnen, Bild per Qt-Signal melden. Fester 30-fps-Takt |
| **Audio (PortAudio)** | Duplex-Callback, 512 Samples bei 48 kHz |
| **effect-lifecycle** | `activate`/`deactivate` der Effekte, etwa Audiogerät öffnen |
| **audio-control** | Gerätewechsel und Stufen setzen |
| **target-image** | Gewähltes JPG laden, Gesichtspunkte und Mesh vorbereiten |
| **matrix-face** | Gesichtspunkte des Kamerabilds mit Apple Vision oder OpenCV erkennen |

Beim Matrix-Morph verfolgt optischer Fluss die Gesichtspunkte im Aufnahme-Thread
zwischen den asynchronen Erkennungen. Das Mesh rendert nur einen Ausschnitt um
das Gesicht; die Hauptschleife erhält weiterhin RGB-Bilder in Eingangsgröße.

Die beiden letzten sorgen dafür, dass ein Klick nie auf ein Gerät wartet: gemessen
0,4 ms Blockierung, obwohl das Öffnen des Geräts 300 ms dauert.

Bilder wandern über eine Queue mit Platz für genau ein Bild: Hinkt die Anzeige nach,
wird das ältere Bild verworfen statt gestaut.

## Audio-Pipeline

Ein einziger Duplex-Stream mit benannten Stufen, in dieser Reihenfolge:

1. `analysis` — `AudioAnalyzer`, misst nur (für „Klang-Beben")
2. `effect` — `AudioEffectEngine` mit dem gewählten Ton-Effekt
3. `video_glitch` — Glitches des Bild-Effekts „Audio+Video Glitch"

Laufen ausschließlich messende Stufen, bleibt der Ausgang stumm. So kann ein
Bild-Effekt mithören, ohne eine Rückkopplung zu erzeugen.

Die Engine mischt trocken und bearbeitet: Das trockene Signal wird um die Latenz des
Effekts verzögert, damit beides zusammenpasst. Dabei bleiben seine Eingangskanäle
erhalten; nur der Effekt erhält Mono. Bei Mix 0 % bleibt Stereo deshalb erhalten.
Nach einem fehlgeschlagenen Audio-Start versucht ein Gerätewechsel erneut zu starten,
solange mindestens eine Stufe aktiv ist.

## Kopplung Bild ↔ Ton

- **Bild → Ton:** `VideoFeatures.update()` läuft im Aufnahme-Thread, `snapshot()`
  liest der Audio-Thread. Nur aktiv, wenn ein Ton-Effekt läuft.
- **Ton → Bild:** `AudioAnalyzer` misst im Audio-Thread, der Bild-Effekt liest den
  Stand. Pegel werden automatisch normiert, also unabhängig von der Aussteuerung.

Beide Seiten tauschen nur unveränderliche Momentaufnahmen aus, deshalb ist kein
Sperren nötig.

## Plugins laden

`PluginManager` in `video.py`:

1. Dateien aus `plugins/` und `~/.bildeffekte/plugins` sammeln (`_`-Dateien überspringen)
2. Jede Datei als eigenes Modul laden
3. Klassen finden: `IEffectStrategy`, `IAudioEffect` oder `PluginModule`
4. Effekte sofort erzeugen (Fehler fallen hier auf, nicht erst im Betrieb) und unter
   der Herkunft der Datei registrieren
5. `sync_effects()` gleicht die Listen von Bild- und Ton-Effekten mit dem Container ab

Beim Neuladen wird die Herkunft entfernt und alles neu eingelesen. Ein gerade aktiver
Ton-Effekt, der verschwindet, wird abgewählt; ein verschwundener Bild-Effekt schaltet
auf den ersten verbleibenden um.

Bleibt der Name gleich, wird die aktive Instanz trotzdem ersetzt. Bei laufender
Aufnahme wird der alte Bild-Effekt deaktiviert, danach der neue aktiviert. Die
Audio-Engine übernimmt und initialisiert den neuen Ton-Effekt beim nächsten Block
im Audio-Thread. Eine ausstehende Benutzerauswahl hat dabei Vorrang.
