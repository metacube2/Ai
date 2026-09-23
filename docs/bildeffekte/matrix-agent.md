# Matrix-Agent – Gesichtsmorph

## Bedienung

App neu starten oder **Plugins neu laden**. In der Bild-Effektliste
**Matrix-Agent – Gesichtsmorph** wählen; Kamera als Quelle verwenden.
Ein Gesicht frontal und gut beleuchtet zeigen. Der Effekt erfasst das größte Gesicht.

Unter **Gesichtsmorph → JPG wählen …** kann ein eigenes JPG/JPEG als Ziel
gewählt werden. Die Gesichtserkennung prüft es im Hintergrund; bei ungültigen Bildern
oder fehlendem Gesicht bleibt das bisherige Ziel erhalten. **Standard-Agent** entfernt
die Auswahl. Ohne eigene Auswahl wird immer der mitgelieferte Agent verwendet.
Die Auswahl bleibt bei Stop/Start, Effektwechsel und Plugin-Neuladen innerhalb der
laufenden App erhalten; nach einem App-Neustart gilt wieder der Standard.
Am besten eignet sich ein frontales Porträt mit gut sichtbarem Gesicht. Bei mehreren
Gesichtern wird das größte gewählt; ersetzt wird weiterhin nur das Kameragesicht.

Zehn-Sekunden-Schleife: Original → Übernahme in 2,6 Sekunden → Agent halten →
zurückverwandeln → Original. Erneutes Anwählen startet neu.

**🎭 Dramatik** (Knopf unter Gesichtsmorph, rot = an) verformt so stark wie in der
Pilotszene von `morph.mp4`: Eine Schwellung rollt vom Kinn über das Gesicht bis über
die Augen, die Wangen steigen auf, das Untergesicht wird breit, der Mund reißt auf und
die Winkel werden weit auseinandergezogen. Die alte Person wehrt sich: Die Schwellungen
zucken, und der Agent wird zweimal zurückgedrängt, bevor er gewinnt. Die Übernahme dauert
4,2 statt 2,6 Sekunden, der Rückweg 2 Sekunden. Umschalten startet die Schleife neu.
Die Einstellung bleibt bis zum App-Neustart erhalten.
Kein Gesicht, starkes Profil oder veraltete Analyse: unverändertes Kamerabild.

Die Referenz `morph.mp4` zeigt eine körperliche Verformung, bevor die neue Identität
vollständig erscheint. Deshalb werden Wangen und Kiefer leicht ungleichmäßig
ausgebeult, der Mund etwas gedehnt und die Nase seitlich verschoben. Die Stärke ist
bewusst gering (etwa 4 % der Gesichtsbreite). Stärkere Werte ließen Kiefer und Mund
wie eine Schnauze wirken. Die Agententextur übernimmt die
Gesichtspartien zeitversetzt. Es gibt keinen Code-Regen über dem Gesicht.

## Umsetzung

- `plugins/51_video_matrix_agent.py`: eigenständiges Plugin, Kategorie Verwandlungen.
- `bildeffekte/face_morph.py`: lokale Apple-Vision-Gesichtspunkte; OpenCV-Gesichtsbox
  als einfacherer Rückfall ohne Vision. Keine Tiefenschätzung pro Bild.
- 28 Mesh-Punkte einschließlich Augen, Nase, Mundwinkeln und Lippenrändern.
  Dreiecks-Warp verformt sowohl das Kamerabild als auch das Zielgesicht.
- Optischer Fluss verfolgt den Kopf zwischen den Hintergrundanalysen. Alte Ergebnisse
  werden beim Neustart und bei anderem Bildformat verworfen.
- Eine neue Erkennung korrigiert das Mesh nur zu 30 % (`DETECTION_GAIN`); die Bewegung
  liefert der optische Fluss. Vorher wurde sie zu 75 % übernommen, ihr Rauschen verriss
  das Gesicht etwa 11-mal pro Sekunde.
- Ober- und Unterlippe sind das Mittel der Lippenkontur um die Mundmitte. Der höchste
  Einzelpunkt sprang zwischen den Spitzen des Amorbogens (bis 10 px bei 70 px
  Augenabstand). `matrix_agent.json` enthält beide Punkte im selben Verfahren.
- Ersetzt wird nur die innere Gesichtsfläche: die Rim-Ellipse, an den Seiten und am Kinn
  auf 80 % eingezogen, an der Stirn stärker. Dazu kommt ein breites Brillenband.
  Die volle Ellipse enthielt beim Agenten Ohren, Haare, grauen Hintergrund und
  weißen Kragen, was wie reingestanzt wirkte. Kiefer, Ohren und Haaransatz bleiben echt.
- Die Maske läuft über etwa 14 % der Gesichtsbreite weich aus. Poisson-Blending gibt es nicht mehr.
- Hautton-Angleichung im Lab-Raum: Die Helligkeit wird multiplikativ angepasst, damit die
  Brille schwarz bleibt. Farbton und Streuung übernehmen den Mittelwert der Kamerahaut
  (ohne Brille, Schatten, Zähne und Glanzlichter). Die Vorlage wird leicht weichgezeichnet,
  damit ihre Schärfe zur Kamera passt.
- Dramatik: stärkere Mesh-Verformung (Kiefer ±14 %, Mundwinkel 17 %, Unterlippe 16 % der
  Gesichtsbreite) und danach ein dichtes Verformungsfeld aus vier wandernden Beulen
  (`DRAMA_BULGES`): Aufblähen wie eine Lupe plus Schub mit 2–3 breiten Wülsten. Das Feld
  läuft zu Helm, Haaren und Bildrand hin auf null und wird in halber Auflösung gerechnet.
  Die Optionen kommen über `config['effect_options']` (Event `effect_option`).
- Nase und Mund liegen zur Hälfte an der echten Position. Der Mund folgt der Person,
  die Proportionen bleiben menschlich.
- Berechnet wird nur ein Ausschnitt um das Gesicht; Kamera intern maximal 512 Pixel.
- Vorhandene `smooth`-Interpolation aus dem Verwandlungskern wiederverwendet.

## Bildvorlage und Herkunft

`bildeffekte/assets/matrix_agent.png` ist eine mit dem eingebauten **image_gen**-Tool
neu erzeugte fotografische Vorlage. `matrix_agent.json` enthält die dazugehörigen
lokal erkannten Gesichtspunkte. Keine Filmbilder werden als Textur verwendet.
Im laufenden Betrieb werden keine Kamerabilder hochgeladen.

Verwendeter Generierungsprompt:

> Use case: photorealistic-natural. Asset type: photographic face texture for a local webcam face morph effect. Generate a perfectly frontal centered headshot of a stern Matrix-style male agent, age 40, angular pale face, short slicked back dark hair, clean shaven, narrow rectangular black sunglasses with subtle glass reflections, black suit white shirt black tie. Photoreal skin pores and natural facial texture. Neutral expression mouth closed, no head tilt, both ears visible, symmetrical soft diffuse frontal studio lighting, plain middle gray background. Square image, entire head including hair and chin within central 70 percent of image, shoulders visible at bottom. No text no rain no code no green lighting no collage. This is a single photographic asset, suitable for geometrically warping onto a webcam portrait. Save the generated image for use in the project.

## Prüfung und Grenzen

Automatisierte Tests prüfen unverändertes Original, unveränderten Hintergrund,
angeschnittene Gesichter, Bewegungstracking und Neustart mit noch laufender Analyse.
Gesamtsuite: 49 Tests bestanden (2026-09-23, mit Dramatik-Modus), einschließlich JPG-Laden, ungültigem JPG und
Rückkehr zum Standard-Agenten. Zusätzlich wurde die Qt-Bedienung ohne sichtbares
Fenster mit einem echten JPG und lokaler Gesichtserkennung geprüft.
Eine vollständige 10-Sekunden-Schleife wurde anhand eines Porträtfotos gerendert.
Die Überarbeitung von Maske und Hautton (2026-09-23) wurde an zwei Fotos mit
unterschiedlichem Hautton und unterschiedlicher Beleuchtung sichtgeprüft. Live-Webcam
ist ungeprüft.

Zittermessung (2026-09-23, ruhiger Kopf mit Kamerarauschen, 30 fps, Apple Vision, nicht
eingecheckt): größter Punktsprung je Bild von 10,3 auf 1,3 px, Mittel von 0,36 auf
0,15 px. Bei ±25 px Kopfbewegung folgt das Mesh ohne zusätzliche Verzögerung.

Dramatik (512 × 384, Gesicht 157 px breit): 25 ms Median, 29 ms p95 gegenüber 15,5 ms ohne.
Sichtgeprüft als Bildfolge auf dem Soldatengesicht aus `morph.mp4`, nicht live.

Der finale Messlauf mit 28 Punkten und 512 × 512 Pixeln ergab 17,1 ms Median und
27,9 ms p95 für das Rendern während des Morphs. Das schließt Kamerazugriff und
asynchrone Gesichtserkennung nicht ein und ist keine garantierte Bildrate.

Der Effekt ersetzt das Gesicht; Haare, Kleidung und Körper bleiben aus der Kamera.
Es handelt sich um einen 2D-Morph mit fotografischer Textur, nicht um ein animiertes
3D-Gesicht. Starke Kopfdrehung, Hände vor dem Gesicht und extremes Sprechen können
sichtbare Fehler erzeugen. Filmqualität und Live-Webcam-Verhalten sind nicht durch
den Fototest bestätigt. Ohne Apple Vision ist die Ausrichtung gröber.
