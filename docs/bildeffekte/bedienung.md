# Bildeffekte — Bedienung

Stand: 2026-09-22

## Starten

- **Schreibtisch:** Doppelklick auf `Bildeffekte.command`.
- **Terminal:** `cd Ai && ../venv/bin/python video.py`

Beim Start werden derzeit 57 Plugin-Effekte aus 19 Dateien geladen. Mit der fest
eingebauten Bildstrategie „Original“ sind es 39 Bild- und 19 Ton-Effekte.
Die Statuszeile meldet, wie viele Effekte gefunden wurden und welche Dateien Fehler haben.

**Voraussetzungen im venv:** numpy, scipy, opencv-python-headless, mss, PySide6, sounddevice.
Alle sind installiert.

**macOS-Freigaben:** Für die Quelle „Bildschirm" braucht das Terminal
„Bildschirmaufnahme", für Kameras „Kamera" (Systemeinstellungen → Datenschutz &
Sicherheit). Für Ton-Effekte zusätzlich „Mikrofon". Kommt bei der
Bildschirmquelle nur ein schwarzes Bild, weist die Statuszeile darauf hin.

## Bild

| Bedienelement | Wirkung |
| --- | --- |
| **Quelle** | Standard ist „Bildschirm"; Kamera 0–3 sind die von OpenCV nummerierten Live-Kameras. Der Index hängt von angeschlossenen und virtuellen Geräten ab |
| **Bereich** | Nur bei „Bildschirm": `x,y,Breite,Höhe` in Bildschirmpunkten, mindestens 16×16 |
| **Bereich wählen…** | Bildschirm wird abgedunkelt, Rahmen mit der Maus aufziehen. Esc oder Rechtsklick bricht ab |
| **Start / Stop** | Aufnahme mit 30 Bildern pro Sekunde |
| **Nächste Zelle** | Nur beim Effekt „Mosaik": schaltet die aktive Kachel weiter |
| **Effekt** | Menü der Bild-Effekte, nach Gruppen geordnet (Grundlagen, Glitch, Retro & VHS, Natur & Material, Verwandlungen, Raum & Illusion, Audio-reaktiv). „Original“ steht oben, der Knopf zeigt Gruppe › Effekt |
| **Kontur-Labor** | Eigene Sektion mit vier aus Kanten und Kontrast berechneten Effekten |
| **Gesichtsmorph** | Erscheint beim Matrix-Agenten: eigenes JPG/JPEG wählen oder mit „Standard-Agent“ zur mitgelieferten Vorlage zurückkehren. **🎭 Dramatik** verformt so stark wie in der Filmszene (rot = an) |
| **⛶ Vollbild** | Ausgabebild als eigenes Vollbildfenster öffnen; Esc oder Klick auf das Bild beendet es |
| **⏺ Aufnehmen** | Nur bei laufendem Video: speichert das Ausgabebild als MP4 (H.264, 30 fps, ohne Ton) nach `~/Movies/Bildeffekte/Bildeffekte_<Datum_Uhrzeit>_<Effekt>.mp4`. Der Knopf zeigt „REC mm:ss“; nochmal klicken oder Stop beendet und speichert, die Statuszeile nennt den Pfad. Rechnet ein Effekt langsamer als 30 fps, werden Bilder wiederholt, damit das Video in Echtzeit abläuft |
| **🧩 Plugins neu laden** | Liest den Ordner `plugins` neu ein, ohne Neustart |

**Tastenkürzel:** Leertaste startet und stoppt, **N** schaltet die Mosaik-Zelle weiter.

Die Vorschau skaliert mit dem Fenster und behält das Seitenverhältnis. Rechts unten
stehen Bildrate und, bei aktivem Ton-Effekt, dessen Rechenlast.
Ein Klick direkt auf die Vorschau öffnet ebenfalls das Vollbild. Beim
Kontur-Magnetfeld bleibt der einfache Klick zum Verformen reserviert: dort
öffnet der Vollbildknopf oder ein Doppelklick die große Ansicht; im Vollbild
beendet ein Doppelklick oder Esc die Ansicht.
Bei einer Kamera nutzt die App deren Bildgröße; der Bildschirmbereich wird ausgeblendet.
Die Kamera wird beim Stoppen wieder freigegeben. Kameraaufnahme und Ton-Eingang
sind unabhängig: Ton kommt weiterhin vom gewählten Audiogerät.

**Kontur-Magnetfeld:** In der laufenden Vorschau auf eine Stelle klicken und
ziehen. So lassen sich etwa Auge oder Mund wie bei einem Goo-Effekt verformen.
Nach dem Loslassen klingt die Verformung ab. Die App passt den lokalen
Kontrast für das Kontur-Labor automatisch an; sie erkennt aber keine Augen
oder Münder als benannte Objekte.

**Flüssigmetall-Schmelze:** Beim Anwählen wird ein Bild eingefroren. Die App
erkennt auf dem Mac die Person und Gesichtsmerkmale lokal mit Apple Vision.
Während dieser einmaligen Analyse bleibt das Standbild sichtbar (beim ersten
Mal einige Sekunden). Danach wird die Person zu reflektierendem Chrom und
schmilzt in etwa acht Sekunden zu einer Pfütze. Zum Wiederholen einen anderen
Effekt und dann erneut „Flüssigmetall-Schmelze“ wählen. Die Reflexionen folgen
einem geschätzten räumlichen Relief; das ist keine vollständige 3D-Rekonstruktion.
Der hinter der Person verdeckte Hintergrund wird angenähert und kann verschmiert
aussehen. Ein einfacher Hintergrund liefert die besten Ergebnisse.
Die Metalloberfläche spiegelt Bildstrukturen und ergänzte helle Lichtflächen.
Feine Oberflächenwellen bewegen die Reflexe auch in der fertigen Pfütze;
deren Spiegelrichtung berücksichtigt die liegende Oberfläche.

**Matrix-Agent – Gesichtsmorph:** Kamera als Quelle wählen und das Gesicht frontal
zeigen. Das Gesicht verformt sich zum Agenten und wieder zurück. Unter
**Gesichtsmorph → JPG wählen …** kann stattdessen ein Foto mit gut sichtbarem
Gesicht geladen werden. Die App prüft es im Hintergrund; bei einem Fehler bleibt
das bisherige Ziel aktiv. **Standard-Agent** setzt auf die mitgelieferte Vorlage
zurück. Die Auswahl bleibt während Stop/Start, Effektwechsel und Plugin-Neuladen
erhalten, wird beim App-Neustart aber zurückgesetzt. Details und Grenzen stehen in
`matrix-agent.md`.

Die zusätzliche macOS-Abhängigkeit ist hier installiert. Für eine neue Installation:
`../venv/bin/python -m pip install -r bildeffekte/requirements-macos.txt`.
Ohne Apple Vision nutzt der Effekt die einfachere Kontrastmaske. Findet er
keine brauchbare Silhouette, zeigt er das eingefrorene Bild.

## Ton

| Bedienelement | Wirkung |
| --- | --- |
| **Ton-Effekt** | „Aus" oder einer der 19 Effekte; die Beschreibung erscheint als Tooltip |
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
| Kamera öffnet nicht | Kamera-Freigabe prüfen, anderen Kameraindex wählen oder die Kamera in einer anderen App schließen |
| Pfeifen | Mikrofon auf Lautsprecher, Kopfhörer benutzen |
| Kein Ton im Effekt | Beim Effekt „Klang-Beben" wird nur gemessen, der Ausgang bleibt absichtlich stumm |
| Ruckeln bei großem Bereich | Rechenzeit hängt an der Pixelzahl, siehe `messwerte.md` |
| Plugin fehlt in der Liste | Statuszeile prüfen, dort steht der Fehler der Datei |
