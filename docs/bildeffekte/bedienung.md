# Bildeffekte — Bedienung

Stand: 2026-09-16

## Starten

- **Schreibtisch:** Doppelklick auf `Bildeffekte.command`.
- **Terminal:** `cd Ai && ../venv/bin/python video.py`

Beim Start werden alle Plugins geladen (gemessen: 31 Effekte aus 11 Dateien in 34 ms).
Die Statuszeile meldet, wie viele Effekte gefunden wurden und welche Dateien Fehler haben.

**Voraussetzungen im venv:** numpy, scipy, opencv-python-headless, mss, PySide6, sounddevice.
Alle sind installiert.

**macOS-Freigaben:** Das Terminal braucht „Bildschirmaufnahme" (Systemeinstellungen →
Datenschutz & Sicherheit). Für Ton-Effekte zusätzlich „Mikrofon". Kommt nur ein schwarzes
Bild, weist die Statuszeile nach einer Sekunde darauf hin.

## Bild

| Bedienelement | Wirkung |
| --- | --- |
| **Bereich** | `x,y,Breite,Höhe` in Bildschirmpunkten, mindestens 16×16 |
| **Bereich wählen…** | Bildschirm wird abgedunkelt, Rahmen mit der Maus aufziehen. Esc oder Rechtsklick bricht ab |
| **Start / Stop** | Aufnahme mit 30 Bildern pro Sekunde |
| **Nächste Zelle** | Nur beim Effekt „Mosaik": schaltet die aktive Kachel weiter |
| **Effekt** | Auswahl aus allen geladenen Bild-Effekten |
| **🧩 Plugins neu laden** | Liest den Ordner `plugins` neu ein, ohne Neustart |

**Tastenkürzel:** Leertaste startet und stoppt, **N** schaltet die Mosaik-Zelle weiter.

Die Vorschau skaliert mit dem Fenster und behält das Seitenverhältnis. Rechts unten
stehen Bildrate und, bei aktivem Ton-Effekt, dessen Rechenlast.

## Ton

| Bedienelement | Wirkung |
| --- | --- |
| **Ton-Effekt** | „Aus" oder einer der 13 Effekte; die Beschreibung erscheint als Tooltip |
| **Mix** | 0 % = Originalton, 100 % = nur Effekt. Das trockene Signal wird passend zur Effektlatenz verzögert |
| **Eingang / Ausgang** | Audiogeräte. Wechsel läuft im Hintergrund und blockiert die Oberfläche nicht |

Ton-Effekt und Bild-Effekt sind unabhängig und lassen sich beliebig kombinieren.

**Rückkopplung vermeiden:** Läuft der Ton vom Mikrofon auf die Lautsprecher, pfeift es.
Kopfhörer benutzen. Die Statuszeile weist beim Start darauf hin.

## Ton des Videos statt Mikrofon (BlackHole)

macOS kann den Systemton nicht direkt aufnehmen. Dafür ist ein virtuelles Audiogerät
nötig. **BlackHole 2ch ist auf diesem Mac installiert.**

1. Systemeinstellungen → Ton → Ausgabe: **BlackHole 2ch**
2. Im Programm: Eingang **BlackHole 2ch**, Ausgang Kopfhörer oder Lautsprecher
3. Video starten und Ton-Effekt wählen

Den Originalton hörst du dann nur noch über das Programm. Ohne BlackHole:
`brew install blackhole-2ch`.

**Bluetooth-Kopfhörer:** Wird deren Mikrofon als Eingang benutzt, schaltet macOS auf
den Telefonmodus mit deutlich schlechterer Tonqualität. Besser das eingebaute Mikrofon
oder BlackHole wählen.

## Stolpersteine

| Symptom | Ursache und Abhilfe |
| --- | --- |
| Nur schwarzes Bild | Freigabe „Bildschirmaufnahme" fehlt |
| Pfeifen | Mikrofon auf Lautsprecher, Kopfhörer benutzen |
| Kein Ton im Effekt | Beim Effekt „Klang-Beben" wird nur gemessen, der Ausgang bleibt absichtlich stumm |
| Ruckeln bei großem Bereich | Rechenzeit hängt an der Pixelzahl, siehe `messwerte.md` |
| Plugin fehlt in der Liste | Statuszeile prüfen, dort steht der Fehler der Datei |
