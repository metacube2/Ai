# Geometric Resonator

Gezeichnete Flächen als Klangkörper: der Umriss wird zu einem Rechengitter,
darauf werden die Eigenmoden der schwingenden Fläche gelöst, und diese Moden
sind im Browser in Echtzeit hörbar.

Vollständige technische Übergabe: [HANDOFF.md](HANDOFF.md)

## Was hier wirklich gerechnet wird

Gelöst wird das Eigenwertproblem der zweidimensionalen Membran auf dem
gerasterten Umriss:

```text
-Δφ = λφ   im Gebiet,    φ = 0   am Rand
```

Diskretisiert mit finiten Differenzen (Fünf-Punkte-Stern), gelöst über eine
Band-Cholesky-Zerlegung mit blockweiser inverser Iteration. Die Eigenwerte
ergeben die absoluten Resonanzfrequenzen, die Eigenvektoren die
Schwingungsformen — und aus diesen folgt, wie laut jede Mode bei einem
gegebenen Anregungspunkt überhaupt anspricht.

Der Löser ist gegen die analytisch bekannten Lösungen von Quadrat und Kreis
geprüft: die Modenverhältnisse stimmen auf unter 0,5 % genau, entartete Paare
kommen korrekt doppelt zurück.

**Was es nicht ist:** kein FEM-Netz, keine dreidimensionale Simulation eines
Instrumentenkorpus mit Decke, Boden und Luftvolumen, keine Werkstoffdaten aus
einem Datenblatt. Es ist ein 2D-Membranmodell der gezeichneten Fläche. Die
absolute Tonlage hängt an einer frei gewählten Materialkonstante; die
*Verhältnisse* der Moden sind physikalisch bestimmt.

## Verbinden

Es gibt keinen Verbinden-Knopf mehr. Teile, die sich berühren oder
überlappen, belegen benachbarte Rasterzellen und bilden dadurch einen
einzigen Klangkörper — mit tieferem Grundton und neuen Moden. Beim Verschieben
rasten Kanten aneinander ein, damit man dafür nicht pixelgenau zielen muss.

Getrennte Teile bleiben getrennte Körper. Sie werden in eigener Farbe
umrandet; es klingt der Körper, in dem der Anregungspunkt liegt.

## Bedienung

1. Werkzeug wählen, Form auf der Fläche aufziehen.
2. Weitere Teile zeichnen und so verschieben, dass sie sich berühren.
3. Anregungs- und Abnahmepunkt verschieben — eine Mode mit einem
   Schwingungsknoten am Anregungspunkt bleibt stumm.
4. Material und Dämpfung wählen.
5. Anregung wählen: Bogen, Luftstrom oder Anschlag.
6. `Klang rendern`, optional `WAV rendern`.

Der Anschlag zeigt den Körper am deutlichsten: nach dem Impuls schwingt nur
noch die Fläche selbst.

## Start

```bash
cd GeometricResonator
python3 web_app.py
```

Danach `http://localhost:8765`. Die gesamte Berechnung und Synthese läuft im
Browser; Python liefert nur die Dateien aus.

Für Zugriff aus dem LAN `python3 web_app.py --host 0.0.0.0`. Achtung:
`AudioWorklet` verlangt außerhalb von `localhost` meist einen sicheren
Kontext, reines HTTP über die LAN-IP kann die Audioerzeugung blockieren.

## Tests

```bash
npm test                       # alle vier Suiten
node tests/eigen.test.mjs      # Eigenlöser gegen analytische Lösungen
node tests/editor.test.mjs     # Einrasten und Treffertest
node tests/synth.test.mjs      # Synthese und Kette Geometrie -> Klang
node tests/browser-smoke.mjs   # Modul-Worker und AudioWorklet in Chrome
python3 -m unittest -v         # Desktop-App
```

Die Browserprobe überspringt sich selbst, wenn kein Chrome gefunden wird;
ein abweichender Pfad lässt sich über die Umgebungsvariable `CHROME` setzen.

Die entscheidende Prüfung ist die Quelle-Filter-Trennung, denn genau sie
fehlte vorher:

- Tonhöhenregler bewegen → Grundton wandert, Resonanzen bleiben stehen.
- Form ändern → Resonanzen wandern, Grundton bleibt.

## Desktop-App

```bash
python3 -m pip install -r requirements.txt
python3 app.py
```

Die Tkinter-App ist unverändert und arbeitet weiterhin mit vier festen Formen
und fest hinterlegten Modenverhältnissen. Sie hat die hier beschriebene
Simulation **nicht**; wer den Unterschied hören will, nimmt die Web-App.
