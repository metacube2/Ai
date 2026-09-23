# Verwandlungen

Stand: 2026-09-22

Zehn Effekte, die eine Person im Kamerabild verwandeln: Flüssigmetall II und neun
Varianten auf demselben Kern. Die Kategorie ist „Verwandlungen“, in der Oberfläche
stehen sie in der normalen Effektliste (eine eigene Auswahlbox gibt es bisher nur
für das Kontur-Labor).

## Effekte

| Effekt | Plugin | Was passiert | ms¹ |
| --- | --- | --- | ---: |
| Flüssigmetall-Schmelze II | `37_…liquid_metal_v2` | Chrom wie T-1000, fließt in eine Bodenpfütze | 26–34 |
| Flüssiggold | `38_…material` | Poliertes Gold, goldene Pfütze | 24–35 |
| Flüssigglas | `38_…material` | Durchsichtig, Hintergrund gebrochen mit Farbsäumen | 18–29 |
| Lava-Schmelze | `38_…material` | Kruste mit glühenden Rissen, zäh, erstarrt in der Pfütze | 21–31 |
| Honig-Schmelze | `38_…material` | Bernstein, durchscheinend, sehr zäh | 20–33 |
| Aus der Pfütze | `39_…spezial` | Person formt sich aus einer Metallpfütze | 21 |
| Zerfall zu Asche | `39_…spezial` | Verkohlt und verweht von einer Seite her | 22 |
| Eis & Zerspringen | `39_…spezial` | Klares Eis mit Raureif und Brechung, Risse laufen vom Aufprall aus, zerspringt | 16³ |
| Formwandler | `39_…spezial` | Metall fließt zwischen zwei Posen | 18 |
| Predator-Tarnung | `39_…spezial` | **Live**, ohne Einfrieren: durchsichtig mit Flimmern | 33 |

¹ 1280×720 auf dem M1. Materialien: reines Rendern am formatfüllenden Porträt,
übrige: live mit Person vor einem Raum. Die Simulation läuft in einem Nebenprozess
und kostet keine Bildzeit.
³ Messwert vor der Überarbeitung vom 2026-09-23. Neue Werte stehen unter „Verfahren → Eis“.

Das Original `36_video_liquid_metal.py` ist unabhängig davon und wird separat
weiterentwickelt. Deshalb nutzen die Verwandlungen eigene Dateien
(`portrait2.py` statt `portrait.py`). „Sand-Zerfall“ (`70_video_nature`) ist ein
älterer Effekt ohne Personenerkennung.

## Bedienung

1. Kurz **aus dem Bild gehen**, Effekt wählen. Hinweis: „Hintergrund wird gemerkt –
   jetzt ins Bild treten“.
2. **Ins Bild treten.** Nach zwei Erkennungen (alle 0,3 s) friert das Bild ein, nach
   etwa 3 s liegt die Analyse vor.
3. Danach **Kreislauf:** Verwandlung → Pause → rückwärts bis zum unveränderten
   Startbild → Pause → von vorn. Erneut anwählen friert neu ein.

Steht beim Anwählen schon jemand im Bild, friert der Effekt sofort ein. Der
Hintergrund wird dann per Inpainting angenähert und nachgelernt, sobald die Person
sich bewegt.

- **Aus der Pfütze:** Die Pfütze liegt sofort da, nach etwa 3,5 s wächst die Person heraus.
- **Formwandler:** Nach der Chrom-Pose zeigt das Livebild „Neue Pose! 3 … 2 … 1“.
  Wird die zweite Pose nicht erkannt, startet der Countdown neu.
- **Predator-Tarnung:** schaltet sich bei erkannter Person ein und setzt alle 6–10 s
  kurz aus. Ohne gemerkten Hintergrund wirkt sie deutlich weniger unsichtbar.

Einen Klick-Auslöser gibt es nicht: Der Klick ist in `video.py` für das Vollbild belegt.

## Installation

```bash
cd Ai
../venv/bin/python -m pip install -r bildeffekte/requirements-liquid2.txt
mkdir -p ~/.bildeffekte/models && curl -L -o ~/.bildeffekte/models/depth_anything_v2_small_fp16.onnx \
  https://huggingface.co/onnx-community/depth-anything-v2-small/resolve/main/onnx/model_fp16.onnx
```

- Pakete: `pyobjc-framework-Vision`, `onnxruntime` 1.30.0, `scipy`.
- Modell: Depth Anything V2 Small fp16, 49,6 MB, Apache-2.0, SHA-256
  `2df6223f206b5164e21f664ace61dabeb9bb6a49b8b5a3e00510b4807d0f5b04`, Pfad
  änderbar über `BILDEFFEKTE_DEPTH_MODEL`.
- Rückfälle: ohne Modell geschätztes Relief, ohne Vision GrabCut-Maske, ohne
  Nebenprozess ein Thread.

## Dateien

| Datei | Inhalt |
| --- | --- |
| `bildeffekte/verwandlung.py` | Kern: `BackgroundPlate`, `Verwandlung`, `FluidVerwandlung` |
| `bildeffekte/liquid_sim.py` | FLIP-Löser, Echtzeit-Nebenprozess, Vorausberechnung |
| `bildeffekte/depth2.py` | Tiefenschätzung (ONNX) |
| `bildeffekte/portrait2.py` | Vision: Maske, Gesichtspunkte, Tiefe, schnelle Live-Maske |
| `plugins/37_…`, `38_…`, `39_…` | Die Effekte (37 ist nur noch eine kurze Unterklasse) |
| `tests/test_liquid_metal_v2.py`, `test_verwandlungen.py` | 10 + 9 Tests |

## Kern `bildeffekte/verwandlung.py`

```
Livebild ─► Live-Maske (Thread, 0,3 s) ─► Hintergrundspeicher
Einfrieren ─► Analyse (Thread): Maske + Gesicht + Tiefe
          ─► Relief · Hintergrund hinter der Person · Studio-Umgebung · _setup()
Anzeige   ─► _render_live(t): erster Durchlauf aufzeichnen, dann _timeline()
```

Intern höchstens 512×400 px. Eigene Verwandlung = Unterklasse mit diesen Haken:

| Haken | Zweck |
| --- | --- |
| `_setup()` | Eigene Vorbereitung nach der Analyse |
| `_render_live(elapsed)` | Bild zur Zeit seit dem Einfrieren |
| `_on_stop()`, `_on_reset()` | Hintergrundarbeit beenden, Zustand verwerfen |
| `hold_end`, `hold_start` | Pausen im Kreislauf |
| `_surface(s)`, `_reflection(s)`, `_finish(…)` | Nur `FluidVerwandlung`: Material, Umgebungsspiegelung, Nachbearbeitung |
| `fluid`, `melt_start`, `melt_ramp` | Nur `FluidVerwandlung`: Stoffwerte und Schmelzverlauf |

## Verfahren

**Form.** Tiefe aus Depth Anything (518², CPU, 1,3 s), innerhalb der Person auf das
3.–97. Perzentil normiert: `Relief = Tiefe · Breite · 0,20 + Randrundung · 0,45`.
Die Randrundung ist die Wurzel des geglätteten Kantenabstands. Das Glätten verhindert
eine Naht auf der Mittelachse. Dazu kommen ein leichter Foto-Hochpass und senkrecht
gestreckte Beulen.

**Strömung (FLIP, Zhu & Bridson 2005).**
- Masse in Partikeln (4 je Zelle, exakt erhalten), MAC-Gitter mit Höhe/64 px.
- Druck über Poisson-Gleichung, CG mit Jacobi-Vorkonditionierer und Warmstart.
- Schwerkraft mit sichtbarem Körper ≈ 1 m, Zähigkeit über Reibung
  `1,2 + 110·(1−w)²` und PIC-Anteil `0,04 + 0,5·(1−w)` (w = Weichheit).
- Oberflächenspannung nach CSF, Zeitschritt nach CFL, höchstens 1/45 s.
- Übertragen wird die *Verschiebung* der Partikel: ohne Bewegung exakt null, also
  rauschfrei.
- Im Nebenprozess (`spawn`), weil ein Thread das Rendern über die GIL von 24 auf
  45 ms bremste.

**Bodenpfütze.** Die Bildebene hat keine Tiefe. Metall aus der untersten Schicht
läuft deshalb mit `1 + 0,15·Säulenhöhe` pro Sekunde in eine Pfütze ab. Ihre Fläche
ist Volumen / (6 % Körperhöhe), perspektivisch 0,22-mal so hoch wie breit. Sie
spiegelt hinten die Szene kopfüber, vorn die Deckenlampen.

**Chrom.**
- Dunkle Stahlblau-Umgebung mit wenigen harten Lichtflächen, Kamerabild nur zu 12 %.
- Eine zur Kamera gewandte Fläche spiegelt den Raum *hinter* der Kamera, deshalb
  erscheint die Wand hinter der Person nur am Rand.
- Dazu Fresnel, zwei Glanzlichter und eine kühle Tonkurve. Referenz war das
  T-1000-Bild aus Wikipedia (nur lokal).

**Hintergrundspeicher.**
- Lernt freie Pixel aus Livebildern: Abstand Breite/60, neue direkt, bekannte
  gleitend (25 %).
- Median-Abweichung über 22 bedeutet Kamera bewegt, dann wird verworfen.
- In der eingefrorenen Form wird nur gelernt, was sich deutlich geändert hat (> 35).
  Sonst würde ein gehaltener Gegenstand zum Geist.
- Läuft im Analyse-Thread, wird atomar ersetzt und höchstens einmal pro Sekunde
  aufgefrischt.

**Kreislauf.** Die Strömung ist nicht umkehrbar. Deshalb werden die Zustände des
ersten Durchlaufs aufgezeichnet und abgespielt (13,3 s Schmelze = 338 Zustände,
7,7 MB). Asche und Eis sind geschlossene Formeln der Zeit und brauchen keine
Aufzeichnung.

**Materialien** (nur Oberfläche und Stoffwerte anders):
- **Gold:** Spiegelung mal Goldfarbe (1,00/0,77/0,34) nach Schlick.
- **Glas:** Hintergrund durch die Normalen versetzt, je Farbkanal etwas anders;
  Fresnel 4 %, dunkle Kanten.
- **Lava:** Glühfarbe nach Temperatur; Risse fließen mit. Beim Fließen glüht es
  breit, in der Pfütze erstarrt es. Lichthof auf die Umgebung, Reibung 150,
  Schmelzdauer 5,5 s.
- **Honig:** Brechung plus Absorption nach Dicke (Blau stark) und Streulicht,
  Reibung 200, PIC bis 0,95, Schmelzdauer 6,5 s.

**Aus der Pfütze.** Die Schmelze wird ohne Echtzeit-Takt vorausberechnet (2,5 s für
13,8 s) und gebündelt geschickt. Bis dahin liegt schon die Pfütze mit dem vollen
Volumen. Die Aufzeichnung endet, sobald weniger als 1,5 % Metall übrig sind, denn
die letzten Tropfen sehen rückwärts wie Stillstand aus.

**Asche.**
- Rauschige Front von rechts nach links (3,8 s), vor der Front verkohlt mit Glutsaum.
- Ein Korn je 2×2 px: Wind (τ^1,6), Wirbel, 2,4 s Lebensdauer, grau-braun.

**Eis** (überarbeitet 2026-09-23; vorher bläulich getöntes Graubild mit weißen
Schnörkeln und weißem Wabennetz, das comichaft wirkte).
- Frost von unten (0,4–2,6 s). Die Front hat drei Rauschoktaven und ist deshalb am
  Rand fein verästelt. Vor der Front legt sich körniger, matter Raureif an. Dahinter
  wird das Eis in 0,55 s klar, einzelne Reifflecken bleiben an dünnen Stellen liegen.
- Klares Eis wird einmal vorab als Bild berechnet:
  - Oberfläche aus 35 % Körpertiefe plus angeschmolzenen Buckeln, daraus Normalen.
  - Brechung: Die Person bleibt verzerrt und leicht weich sichtbar, halb entsättigt.
  - Dickeabhängige Absorption (Rot stärker als Blau) und milchige Streuung.
  - Luftbläschen und feine innere Bruchflächen, an der Silhouette ein helles Streulicht.
  - Fresnel-Spiegelung der Studioumgebung (`environment`) und harte Glanzlichter.
- Risse ab 3,0 s laufen in 0,45 s vom Aufprallpunkt (Brust) nach außen. Die
  Voronoi-Kanten werden per Rauschverschiebung gezackt. Sichtbar sind eine dünne
  dunkle Bruchkante und daneben eine weißliche Bruchfläche. Jede Scherbe sitzt nach
  dem Bruch um etwa 1 px versetzt, sodass das Bild an den Rissen springt.
- Aufprall bei 3,6 s: 30–110 Scherben, dichter an der Brust. Wurfparabel mit Drehung,
  Aufprall und Nachfedern.
- Scherben mit Fase: Die Kante ist zum Licht hin hell und abgewandt dunkel und wandert
  mit der Drehung. Dazu kommen Kantenleuchten, kurzes Aufblitzen beim Drehen, 10 %
  Durchsicht und ein weicher Kontaktschatten am Boden. Textur, Kantenneigung und
  Leuchten liegen vorab im Scherbenraster, pro Bild gibt es einen Warp je Scherbe.
- Eispulver: 200–1500 Splitterpunkte aus den Rissen und ein kurzer Staubnebel am
  Aufprallpunkt, nach 1,5 s verschwunden. Unten bleiben nur die Scherben liegen.
- Messung (M1, Standbild 640×800 → intern 320×400, reines Rendern): Frost und Eis
  7–9 ms, Risse 13 ms, Scherbenflug 24–32 ms (vorher 15–23 bzw. 21–26 ms).
  Der Aufbau kostet einmalig mehr Zeit im Analyseschritt; die Zahlen sind nicht mit
  der 1280×720-Tabelle oben vergleichbar.

**Formwandler.**
- Je 6000 Punkte in beiden Posen, verbunden durch „sliced optimal transport“ mit 60
  Projektionen.
- Weicher Verlauf, unten später, Durchhängen 8 % der Höhe, Wabern je Punkt.
- Maske, Relief und Foto über Verschiebungsfelder aus beiden Aufnahmen.

**Predator.**
- Live-Maske fortlaufend (etwa 20/s, rund 50 ms Verzug), deshalb erweitert.
- Relief aus Körperwölbung plus Bildstruktur, Brechung des gemerkten Hintergrunds
  mit Farbaufspaltung und Flimmern.
- Einschalten von oben mit Blitzen (1,6 s), Aussetzer 0,35 s.

## Physik oder Annäherung

| Echt simuliert | Modell, abgestimmt | Annäherung |
| --- | --- | --- |
| Masse, Inkompressibilität, Schwerkraft | Zähigkeit, Oberflächenspannung | 2D-Schicht statt 3D; Tiefe nur über Pfützenvolumen |
| Scherben-Wurfparabeln | Übergang fest → flüssig (Zeitverlauf) | Form = gelernte relative Tiefe |
| | Lava-Temperatur (Zeitverlauf, keine Wärmeleitung) | Raum hinter der Kamera = Studio-Umgebung |

Hintergrund hinter der Person: exakt, wenn vorher gemerkt, sonst Inpainting.

## Messwerte und Tests

Geprüft mit dem Astronautinnen-Porträt und `rocket.jpg` aus scikit-image 0.24,
freigestellt zu nachgestellten Webcam-Folgen. **Nicht mit einer echten Webcam.**

| Messung | Wert |
| --- | --- |
| Komplette Analyse (Vision + Tiefe) | 2,6 s, im Hintergrund |
| Live-Maske | 47 ms |
| Nachlauf der Echtzeit-Simulation | 0,05–0,07 s |
| Einfrieren nach Eintreten | ≈ 3 s |
| Hintergrund aus dem Speicher | 100 % in der Nachstellung |
| Umbau zum Kern | Flüssigmetall II pixelgleich (12/12 Vergleichsbilder) |

Tests: `test_liquid_metal_v2.py` (Strömung, Massenerhaltung, rauschfreie Abbildung,
Speicher, Wartephase, Rückwärtslauf) und `test_verwandlungen.py` (Materialeigenschaften,
exakte Start- und Endbilder, Scherben unten, Formwandler-Ziel, Tarnung zeigt Raum).
Zu diesem Prüfzeitpunkt: insgesamt 41 Tests (einschließlich
`test_verwandlung_lifecycle.py`). Der aktuelle Stand der gesamten App steht in
`messwerte.md`.

## Lehren

- **Weiße Pfütze:** Die Normalen waren zu steil. Lösung: liegender Spiegel der Szene.
- **Matt vor einfarbiger Wand:** Die Spiegelquelle war falsch. Lösung: Raum hinter
  der Kamera.
- **Erster Gitterlöser verlor 90 % Volumen:** ersetzt durch FLIP.
- **Alufolien-Rauschen:** Verschiebung statt absoluter Herkunft übertragen.
- **Prozess-Leitung fasst etwa einen Zustand:** erst bis zur aktuellen Zeit rechnen,
  dann senden.
- **`Process.start()` blockierte bei großen Startdaten** und totem Kind: Daten erst
  danach per Hilfs-Thread schicken.
- **Gehaltener Helm wurde als Hintergrund gelernt:** Schutz der eingefrorenen Form.
- **Tests fanden:**
  - Randsaum im Startbild. Lösung: „Anwesenheit“ im ganzen ersetzten Bereich.
  - Zerfallszeit nahe null.
  - Ungleiche Punktzahlen im Formwandler.
  - Auffrischen eines nie berechneten Hintergrunds.


## Nachprüfung am 22.09.2026

- Die damals vorhandenen 41 Tests bestanden nach den Korrekturen.
- Leere Bilder ohne erkannte Form erzeugten nach 16 Sekunden einen Zugriff auf
  eine leere Rückwärts-Aufzeichnung. Sie bleiben jetzt stabil als Standbild sichtbar.
- Predator verwirft beim Neustart und bei geändertem Bildformat die alte Maske
  sowie ausstehende Analyseergebnisse. Das Zurücksetzen erfolgt im Renderablauf.
- Hintergrundbild und Konfidenz werden als gemeinsamer Schnappschuss veröffentlicht
  und gelesen. Auch die Prüfung gegen das Standbild verwendet denselben Schnappschuss.
- Zusätzliche Bildprüfung: sieben Effekte auf einem echten Porträt bei 1,5 / 4 / 8
  Sekunden gerendert. Lokale Personenerkennung samt gelernter Tiefe erfolgreich
  (in diesem Durchlauf 2,33 Sekunden bei 256 × 256 Pixeln).
- Echte Prozesssimulation zusätzlich gestartet, Zustände über die Pipe empfangen
  und beendet: 64 aufgezeichnete Zustände, Simulationszeit 3,93 Sekunden nach etwa
  vier Sekunden Laufzeit. Dies ist eine Funktionsprüfung, kein allgemeiner FPS-Wert.
- Sichtbare Grenze: Ohne vorher aufgenommenen leeren Raum muss Inpainting den
  verdeckten Hintergrund ersetzen. Das erzeugt bei strukturierten Hintergründen
  deutliche Unschärfen. Personenmasken können gehaltene Gegenstände auslassen.
  Die Tiefenschätzung ist ein Relief aus einem Kamerabild, keine vollständige
  3D-Rekonstruktion. Ein Live-Webcam-Abnahmetest steht weiterhin aus.
