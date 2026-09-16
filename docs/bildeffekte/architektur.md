# Bildeffekte — Architektur

Stand: 2026-09-16

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

## Threads

| Thread | Aufgabe |
| --- | --- |
| **GUI (Haupt)** | Qt, Anzeige, Bedienung. Fasst nur Widgets an |
| **Aufnahme** | Bildschirm lesen, Effekt anwenden, Bildmerkmale berechnen, Bild per Qt-Signal melden. Fester 30-fps-Takt |
| **Audio (PortAudio)** | Duplex-Callback, 512 Samples bei 48 kHz |
| **effect-lifecycle** | `activate`/`deactivate` der Effekte, etwa Audiogerät öffnen |
| **audio-control** | Gerätewechsel und Stufen setzen |

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
Effekts verzögert, damit beides zusammenpasst.

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
