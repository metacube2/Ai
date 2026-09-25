# Bildeffekte — Bedienung

Stand: 2026-09-22

## Starten

- **Schreibtisch:** Doppelklick auf `Bildeffekte.command`.
- **Terminal:** `cd Ai && ../venv/bin/python video.py`

Beim Start werden derzeit 67 Plugin-Effekte aus 21 Dateien geladen. Mit der fest
eingebauten Bildstrategie „Original“ sind es 49 Bild- und 19 Ton-Effekte.
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
| **Quelle** | Standard ist „Bildschirm“. Kameras erscheinen mit ihrem Namen, etwa „FaceTime HD-Kamera“. Die Reihenfolge ist die von OpenCV, nach Geräte-ID sortiert, nicht die von macOS. „OBS Virtual Camera“ ist als eigene Ausgabe markiert und lässt sich nicht als Quelle starten, sonst entstünde eine Rückkopplung. Ohne `pyobjc-framework-AVFoundation` erscheinen nur „Kamera 0–3“ |
| **Bereich** | Nur bei „Bildschirm": `x,y,Breite,Höhe` in Bildschirmpunkten, mindestens 16×16 |
| **Bereich wählen…** | Bildschirm wird abgedunkelt, Rahmen mit der Maus aufziehen. Esc oder Rechtsklick bricht ab |
| **Start / Stop** | Aufnahme mit 30 Bildern pro Sekunde |
| **Nächste Zelle** | Nur beim Effekt „Mosaik": schaltet die aktive Kachel weiter |
| **Effekt** | Menü der Bild-Effekte, nach Gruppen geordnet (Grundlagen, Glitch, Retro & VHS, Natur & Material, Verwandlungen, Gesichter, Raum & Illusion, Audio-reaktiv). „Original“ steht oben, der Knopf zeigt Gruppe › Effekt |
| **Kette** | Mehrere Bild-Effekte nacheinander. **＋ Effekt anhängen** setzt einen Effekt hinter den ausgewählten. Die Chips zeigen die Reihenfolge; ein Klick auf einen Chip wählt ihn aus, dann ändern **Effekt**, Gesichtsmorph und Dramatik diesen Effekt. **◀ ▶** verschieben, **✕** entfernt ihn. Jeder Effekt kann nur einmal vorkommen. Rechenzeiten addieren sich |
| **Kontur-Labor** | Eigene Sektion mit vier aus Kanten und Kontrast berechneten Effekten |
| **Gesichtsmorph** | Erscheint beim Matrix-Agenten: eigenes JPG/JPEG wählen oder mit „Standard-Agent“ zur mitgelieferten Vorlage zurückkehren. **🎭 Dramatik** verformt so stark wie in der Filmszene (rot = an) |
| **⛶ Vollbild** | Ausgabebild als eigenes Vollbildfenster öffnen; Esc oder Klick auf das Bild beendet es |
| **⏺ Aufnehmen** | Nur bei laufendem Video: speichert das Ausgabebild als MP4 (H.264, 30 fps, ohne Ton) nach `~/Movies/Bildeffekte/Bildeffekte_<Datum_Uhrzeit>_<Effekt>.mp4`. Der Knopf zeigt „REC mm:ss“; nochmal klicken oder Stop beendet und speichert, die Statuszeile nennt den Pfad. Rechnet ein Effekt langsamer als 30 fps, werden Bilder wiederholt, damit das Video in Echtzeit abläuft |
| **📷 Als Kamera** | Sendet das Ausgabebild als virtuelle Kamera **„OBS Virtual Camera“** (1280×720, 30 fps, andere Formate mit schwarzem Rand). Teams, Zoom und FaceTime wählen sie wie eine normale Kamera, siehe „In Teams zeigen“. Läuft unabhängig von Start/Stop: Ohne neues Bild wird das letzte wiederholt |
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

## Effekte kombinieren

Beispiel Matrix-Agent mit VHS-Look:
1. **Effekt** → Verwandlungen › **Matrix-Agent – Gesichtsmorph**
2. **＋ Effekt anhängen** → Retro & VHS › **VHS Kopierkassette**
3. Start. Die Kette zeigt `Matrix-Agent – Gesichtsmorph → VHS Kopierkassette`.

**Die Reihenfolge zählt: Verwandlungen und Gesichtseffekte zuerst, Look-Effekte (VHS,
Retro, Glitch) ans Ende.** Dann liegt das Bandrauschen auch über dem Agentengesicht,
wie bei einer echten alten Aufnahme. Umgekehrt ist das Gesicht scharf auf einem
verrauschten Bild und wirkt eingeklebt, und die Gesichtserkennung arbeitet auf dem
gestörten Bild schlechter.

Gemessen am 2026-09-24 (Standbild 960×540, M1, echte Aufnahmeschleife mit 30-fps-Takt):
Matrix-Agent → VHS 28,7 Bilder/s, VHS → Matrix-Agent 24,8 Bilder/s. Mit schweren
Verwandlungen (Flüssigmetall, Eis) plus weiteren Effekten sinkt die Bildrate
entsprechend. Die Werte der Einzeleffekte stehen in `effekte.md`.

## In Teams zeigen (virtuelle Kamera)

Voraussetzungen, auf diesem Mac erfüllt (Stand 2026-09-24):
- OBS ab Version 30 (installiert: 32.0.4).
- Die Kamera-Erweiterung von OBS ist aktiviert. Auf einem neuen Mac dafür OBS einmal
  starten, **Virtuelle Kamera starten** klicken und die Systemerweiterung in den
  Systemeinstellungen erlauben.
- `pyvirtualcam` und für die Kameranamen `pyobjc-framework-AVFoundation`:
  `../venv/bin/python -m pip install -r bildeffekte/requirements-macos.txt`

Ablauf:
1. Im Programm eine **echte** Kamera als Quelle wählen, Effekt wählen und **Start** drücken.
2. **📷 Als Kamera** einschalten. Die Statuszeile meldet „Sende als Kamera …“.
3. In Teams unter **Einstellungen → Geräte → Kamera** die **OBS Virtual Camera**
   wählen. Im Meeting geht das auch über den Pfeil neben dem Kamera-Symbol.

OBS selbst muss nicht laufen. Läuft dort gleichzeitig die eigene virtuelle Kamera,
sendet nur einer von beiden. Teams spiegelt die eigene Vorschau, die anderen sehen
das Bild richtig herum. Solange **Als Kamera** aus ist, zeigt die OBS-Kamera ein
Standbild von OBS. Den Ton überträgt Teams weiter vom normalen Mikrofon.

Geprüft: Ein Testbild ging über die OBS-Kamera und kam per OpenCV unverändert zurück
(Farben und Ränder stimmen). Das geschah auch über die echte Oberfläche ohne
sichtbares Fenster. Ein Anruf in Teams selbst ist ungeprüft.

## Stolpersteine

| Symptom | Ursache und Abhilfe |
| --- | --- |
| Nur schwarzes Bild | Freigabe „Bildschirmaufnahme" fehlt |
| Kamera öffnet nicht | Kamera-Freigabe prüfen, anderen Kameraindex wählen oder die Kamera in einer anderen App schließen |
| Pfeifen | Mikrofon auf Lautsprecher, Kopfhörer benutzen |
| Teams zeigt das OBS-Logo statt der Effekte | **📷 Als Kamera** ist aus, oder OBS sendet gerade selbst. Knopf einschalten und in OBS die virtuelle Kamera stoppen |
| „OBS Virtual Camera“ fehlt in Teams | Kamera-Erweiterung von OBS nicht aktiviert (siehe oben) oder Teams lief schon vorher: Teams neu starten |
| Kein Ton im Effekt | Beim Effekt „Klang-Beben" wird nur gemessen, der Ausgang bleibt absichtlich stumm |
| Ruckeln bei großem Bereich | Rechenzeit hängt an der Pixelzahl, siehe `messwerte.md` |
| Plugin fehlt in der Liste | Statuszeile prüfen, dort steht der Fehler der Datei |
