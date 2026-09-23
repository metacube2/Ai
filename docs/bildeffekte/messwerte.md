# Bildeffekte — Messwerte und Prüfstand

Stand: 2026-09-22

Die älteren Zahlen stammen von diesem MacBook Air (M-Chip, damals macOS 25.6) und wurden mit
Python 3.11 aus `../../../venv` gemessen. Die Prüfskripte lagen in einem
Arbeitsverzeichnis außerhalb des Repositories und sind **nicht eingecheckt**; die
Verfahren stehen unten, damit die Messung wiederholbar ist.

## Aktueller Prüfstand vom 22.09.2026

**47 Regressionstests bestanden** mit
`../venv/bin/python -m unittest discover -s tests -q`. Der Plugin-Rundum-Test
erfasst 39 Bildstrategien einschließlich „Original“ und 19 Ton-Effekte aus
19 Plugin-Dateien. Ein gesonderter Qt-Test ohne sichtbares Fenster prüfte die
JPG-Auswahl für den Matrix-Agenten mit einem echten Porträt, die lokale
Gesichtserkennung, die Übergabe an den Effekt und das Zurücksetzen auf den
Standard-Agenten. Dieser Test ersetzt keinen Live-Kameratest.

Beim Matrix-Gesichtsmorph ergab ein Lauf mit einem 512×512-Porträt, 28 Mesh-Punkten
und einer vollständigen Zehn-Sekunden-Schleife **17,1 ms Median und 27,9 ms p95**
für das Rendern während des Übergangs. Kameraaufnahme und asynchrone
Gesichtserkennung sind nicht enthalten. Verfahren, Bildvorlage und Grenzen:
`matrix-agent.md`.

## Verwandlungen vom 22.09.2026

Flüssigmetall II und neun Verwandlungen, 1280×720: 16–35 ms pro Bild, Simulation im
Nebenprozess 0,05–0,07 s hinter Echtzeit; Einzelwerte und Verfahren in
`verwandlungen.md`. Nur mit nachgestellten Bildfolgen, nicht mit einer echten Webcam
geprüft. Bei diesem damaligen Messstand bestanden **38 Regressionstests**; der
Plugin-Rundum-Test umfasste 38 Bildstrategien einschließlich „Original“ und
19 Ton-Effekte. Den aktuellen Prüfstand enthält der Abschnitt oben.

## Flüssigmetall-Schmelze vom 22.09.2026

Die überarbeitete Fassung nutzt auf macOS 26.6.2 Apple Vision für Personenmaske
und Gesichtsmerkmale. Nase, Augenhöhlen, Kopf und Körper bilden ein angenähertes
Relief. Daraus berechnete Oberflächennormalen bestimmen die Chromreflexionen.
Die Form sinkt und verbreitert sich geometrisch zur Pfütze; sie wird dabei
nicht mehr durchsichtig ausgeblendet.

Die Materialüberarbeitung ergänzt erkennbare Umgebungsstrukturen mit räumlichem
Versatz, kontrastreiche Lichtflächen samt Deckenreflexion, gerichtete Glanzlichter
und kleine Wellen in der fertigen Pfütze. Damit bleibt auch vor einem einfarbigen
Hintergrund die Oberfläche deutlich unterschiedlich hell und die Spiegelung bewegt
sich weiter. Der interne Bildbereich ist auf 512×400 begrenzt; die Ausgabe behält
ihre ursprüngliche Größe.

Visuell geprüft an einem echten [Testporträt aus scikit-image](https://github.com/scikit-image/scikit-image/blob/v0.24.0/skimage/data/astronaut.png),
mit fünf Zwischenständen und einem exportierten neunsekündigen Clip. Bei
1280×720 dauerte die erste Bildannahme **4,9 ms**, die anschließende Analyse
mit bereits geladenem Vision-Modell **0,16 s**. Der erste kalte Vision-Aufruf
in dieser Sitzung brauchte rund **5,3 s** und läuft deshalb im Hintergrund.
Die Ausgabe blieb dabei auf dem eingefrorenen Bild. Nach der Vorbereitung
betrug die Verarbeitung nach der Materialüberarbeitung **27,4 ms pro Bild**
(20 Bilder, erste 3 verworfen).

Grenzen: Das Relief ist keine echte 3D-Rekonstruktion; verdeckter Hintergrund
wird per Inpainting angenähert und kann sichtbare Flecken erzeugen. Ohne Vision
bleibt GrabCut als einfacherer Ersatz. Zu diesem Zeitpunkt bestanden **19 Regressionstests**,
einschließlich Wechsel während noch laufender Personenerkennung und
unverändertem Hintergrund außerhalb der Person sowie bewegten, kontrastreichen
Reflexionen in der fertigen Pfütze vor einem einfarbigen Hintergrund. Der Plugin-Rundum-Test umfasst
28 Bildstrategien einschließlich „Original“ und 19 Ton-Effekte.

API-Grundlage: [Apple Vision Personenmaske](https://developer.apple.com/documentation/vision/vngeneratepersonsegmentationrequest).

## Sechs Audioeffekte und analoger Videolook vom 21.09.2026

Das Plugin `93_audio_experimental.py` ergänzt sechs Effekte. Auf diesem MacBook Air
benötigten sie mit einem 440-Hz-Testton plus Rauschen bei 48 kHz im Mittel
**0,11–0,16 ms pro 512-Sample-Block** (120 Blöcke, erste 8 verworfen). Alle
Ausgänge waren endlich und hatten nach dem Anfangsvorlauf messbaren Pegel.
Ein Hörtest mit echten Mikrofonen steht noch aus.

Alle fünf VHS-Looks begrenzen jetzt die Helligkeitsdetails auf höchstens
384×432 Bildpunkte und die Farbauflösung noch stärker. Zeilenrauschen,
Zeitbasiswackeln und gelegentliche Störbänder entstehen vor den individuellen
Plugin-Effekten. „Uralter Film“ rechnet die Filmkopie mit höchstens 420×330
Bildpunkten und skaliert danach weich hoch. Bei 800×450 lagen die sechs Looks
zwischen **6,4 und 12,9 ms**, bei 1280×720 zwischen **9,8 und 17,4 ms**
(12–16 Testbilder pro Effekt, erste 2–3 verworfen). Ein synthetisches Bild
mit feinen Linien wurde visuell geprüft. Die simulierte Bandauflösung ist
eine gestalterische Annäherung, keine genaue VHS-Dekodierung.

Zu diesem Zeitpunkt bestanden **16 Regressionstests**. Der Plugin-Rundum-Test
umfasste 27 Bildstrategien einschließlich „Original“ und 19 Ton-Effekte.

## Kontur-Labor vom 21.09.2026

Vier neue Bild-Effekte wurden bei 800×450 und 1280×720 Pixeln mit je 30
zufälligen RGB-Bildern durch `apply()` gemessen; die ersten 5 Bilder wurden
verworfen. Mittelwerte in der Reihenfolge Magnetfeld, Wellen, Lichtskulptur,
Partikel: **12,6 / 15,7 / 18,2 / 11,3 ms** bei 800×450 und
**12,9 / 17,9 / 16,0 / 11,4 ms** bei 1280×720. Ein synthetisches Gesicht
wurde visuell geprüft. Ein Test bestätigt Kanten an Augen und Mund bei nur
7 Grauwertstufen Unterschied zum Hintergrund. Die getrennte Qt-Sektion,
Effektwechsel und Mausziehen wurden ohne echte Kamera geprüft. Insgesamt
bestanden zu diesem Zeitpunkt **15 Regressionstests**. Die vier Effekte sind noch nicht mit
bewegtem Live-Kamerabild gemessen.

## Vier VHS-Effekte vom 21.09.2026

Das neue Plugin `31_video_vhs.py` bringt Tracking-Riss, Farbbluten,
Kopierkassette und Pause. Bei 800×450 Pixeln liefen je Effekt 90 zufällige
RGB-Bilder durch `apply()`; die ersten 10 wurden beim Mittelwert verworfen.
Die Mittelwerte betrugen 0,31 / 8,27 / 7,78 / 4,12 ms in dieser Reihenfolge.
Je Effekt wurden außerdem 100 Bilder in den Größen 16×16, 17×31 und 800×450
auf Ausgabeform und Datentyp geprüft. Zu diesem Zeitpunkt umfasste der Plugin-Rundum-Test
23 Bild- und 13 Ton-Effekte; alle 12 Regressionstests bestanden.

## Kameraquelle vom 21.09.2026

Zwei weitere hardwarefreie Tests prüfen BGR→RGB, Freigabe des Kamerageräts und
die gemeinsame Effekt-Pipeline. Zu diesem Zeitpunkt bestanden 14 Regressionstests.
Später wurde die Kameraquelle vom Nutzer mit Kameraindex 3 im Livebetrieb
bestätigt; die Indizes hängen von den angeschlossenen Geräten ab.

## Regressionstests vom 20.09.2026

Die neuen Tests liegen unter `tests/test_video_regressions.py` im Repository und laufen
ohne Bildschirmaufnahme oder echte Audiogeräte. Aufruf aus `Ai/`:

```bash
../venv/bin/python -m unittest discover -s tests -v
```

Geprüft werden Stereo-Erhalt bei trockenem und gemischtem Ton samt Latenzausgleich,
wechselnde Blockgrößen, Kanal- und Sampleratenwechsel, aktive Plugins nach einem
Neuladen, entfernte Plugins und Audio-Neustart nach einem simulierten Gerätefehler.
Zusätzlich durchlaufen alle 31 mitgelieferten Plugin-Effekte und der eingebaute
Original-Effekt einen Test auf Ausgabeform, Datentyp und beim Ton endliche Werte.
Ergebnis: 12 Tests bestanden. Die älteren Laufzeitmessungen unten stammen weiterhin
vom 16.09.; echte Hardware und Hörqualität sind durch diese Tests nicht abgedeckt.

## Bild

**Verfahren:** Bewegtes Testvideo (Farbverlauf, wandernder Kreis, Schrift), 200 Bilder
je Effekt, Zeitmessung um `apply()`. Erstes Bild ausgenommen, weil Effekte dort
Tabellen aufbauen.

| Größe | Aufnahme (mss) | Bildmerkmale | Anzeige |
| --- | ---: | ---: | ---: |
| 800×450 | 4 ms | 1,8 ms | im Qt-Fenster 30 fps stabil |
| 1920×1080 | – | 1,6 ms | – |

Rechenzeiten der einzelnen Effekte: `effekte.md`.

**Bei größeren Bereichen** steigt die Rechenzeit etwa mit der Pixelzahl. Gemessen:
„Glitch-Dimension" schafft bei 1280×720 noch 18–21 fps statt 30. Abhilfe wäre, den
Effekt intern in halber Auflösung zu rechnen; nicht umgesetzt.

## Ton

**Verfahren:** Deutsche Testsprache (macOS `say`), auf 48 kHz gebracht, in Blöcken von
512 und in gemischten Blockgrößen (128 bis 1024) durch den Effekt geschickt. Geprüft
werden Form, Datentyp, endliche Werte und Zeit pro Block. Budget: 10,7 ms je Block.

Höchste gemessene Dauerlast: 1,43 ms („Glas-Kristallisation"). Alle Werte in
`effekte.md`.

**Funktionsnachweise** (je Effekt eine messbare Eigenschaft):

| Effekt | Nachweis |
| --- | --- |
| Bild-Schablone | Helles Bild oben ergibt 100 000-fach mehr Höhen als Tiefen, unten umgekehrt |
| Audio-Datamosh | Standbild: Klang hält nach Eingangsende an; Schnitt setzt zurück; Bewegung nach oben hebt die Tonhöhe |
| Vorahnung | 0,3 s vor dem Einsatz messbare Energie, weit davor nicht |
| Phosphor | Nach Tonende sinkt die Frequenz von 423 auf 377 Hz |
| Farb-Vokale | Rauschen bekommt je nach Farbe die Formanten von a, i, o |
| Negativ-Ton | Sinus/Rausch-Verhältnis fällt von +37 dB auf −28 dB, Ausgangspegel bleibt hörbar |
| Takt-Zwang | Abstand zum Achtelraster sinkt von 67 ms auf 10 ms |
| Harmonie-Magnet | 450 Hz landen bei C-Dur auf 392,6 Hz, bei D-Dur auf 439,5 Hz |
| Schwarzes Loch | 900 Hz werden auf 750 Hz gezogen, ein Ton im Loch verschwindet |
| Traum-Gedächtnis | Lautstärkekurve folgt zu 99–100 %, Signalkorrelation −0,07 (also neu zusammengesetzt) |
| Glas-Kristallisation | Nachklang auf den Obertönen ×2 plus glockenartige Nebentöne |
| Antimaterie | Energieschwerpunkt wandert von 0,09 auf 0,87 (0 = Anfang, 1 = Ende) |
| Klang-Schwarm | Leise streut der Schwarm 5,3 statt 2,3 Halbtöne und fliegt weiter in die Vergangenheit |

## Robustheit

- **Bildgrößen** von 16×16 bis 1920×1080, je 150 Bilder pro Effekt, dazu erzwungene
  Modus- und Größenwechsel: **0 Fehler**.
- **Blockgrößen** im Ton gemischt, Effektwechsel im laufenden Strom: **0 Fehler**.
- **Rundum-Test:** jeder geladene Effekt wird einmal ausgeführt. Damit fielen zwei
  vergessene Importe beim Auslagern in Plugins auf.

## Oberfläche

| Messung | Wert |
| --- | --- |
| Verzögerung der Ereignisschleife im Betrieb (p95 / max) | 2,4 ms / 16 ms |
| Klick auf Ton-Effekt, während das Gerät 300 ms zum Öffnen braucht | 0,4 ms |
| Gerätewechsel | 0,1 ms |
| Stop-Klick | 0,8 ms |
| Plugins neu laden | 2,2 ms |
| Beim Schließen des Fensters | einmalig ~0,3 s, solange das Audiogerät freigegeben wird |

Zum Vergleich die frühere Tkinter-Oberfläche: Klick auf den Audio-Effekt blockierte
301 ms, die Ereignisschleife hing p95 35 ms, im schlechtesten Fall 308 ms.

## Plugin-System

| Prüfung | Ergebnis |
| --- | --- |
| Start mit allen Plugins | 31 Effekte aus 11 Dateien in 34 ms |
| Datei mit Syntaxfehler | wird gemeldet, alles andere lädt |
| Effekt, der beim Erzeugen abstürzt | wird gemeldet, alles andere lädt |
| Dienst-Injektion im Plugin | bestätigt |
| Neuladen: Datei dazu / gelöscht | Effekt erscheint / verschwindet, Auswahl bleibt erhalten |
| Aktiver Ton-Effekt verschwindet | wird sauber abgewählt |

## Nicht getestet

- **Ton mit echtem Videoton angehört.** Alle Ton-Effekte sind nur gemessen.
- **Audio+Video Glitch mit echtem Mikrofon und Lautsprecher** (Rückkopplungsgefahr).
- **Bereich mit echter Maus aufziehen.** Das Overlay öffnet sich im Test, das Ziehen
  selbst wurde nicht ausgeführt.
- **Andere Bildschirme und Auflösungen** als das eingebaute Retina-Display.
