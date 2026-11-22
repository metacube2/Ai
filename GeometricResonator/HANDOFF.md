# Geometric Resonator Studio - vollstaendige Projektuebergabe

Stand: 2. August 2026  
Projektpfad: `/Users/metacube/Projects/Git/Ai/GeometricResonator`

## Auftrag und Zielbild

Die Anwendung soll geometrische Klangkoerper konstruierbar und unmittelbar
hoerbar machen. Die urspruengliche Idee war eine sichtbare Form, die durch eine
simulierte Violine oder einen Floeten-Luftstrom angeregt wird. Danach wurde das
Ziel konkretisiert:

- nicht nur eine fertige Form auswaehlen;
- Formen wie in einem einfachen Paint-/CAD-Programm aufziehen;
- Breite, Hoehe und Position bearbeiten;
- mehrere Teile zusammenfuegen;
- aus der gesamten Geometrie Resonanzmoden ableiten;
- den Klang in Echtzeit im Webbrowser hoeren;
- das Ergebnis als WAV rendern.

Die Web-App ist inzwischen der primaere Produktpfad. Eine eigenstaendige
Tkinter-Desktopversion ist ebenfalls vorhanden und funktionsfaehig, besitzt
aber noch den aelteren Einzelform-Workflow.

## Aktueller Funktionsumfang der Web-App

### Modellierung

- Auswahlwerkzeug
- Rechteck aufziehen
- Ellipse aufziehen
- Dreieck aufziehen
- Objekte anklicken und verschieben
- Mehrfachauswahl mit Shift-Klick
- Breite, Hoehe, X und Y numerisch bearbeiten
- Objekte duplizieren
- Objekte loeschen
- gesamte Zeichenflaeche leeren
- mehrere ausgewaehlte Teile als gekoppelten Klangkoerper verbinden
- Verbindung ausgewaehlter Teile wieder loesen
- Beispielkonstruktion laden
- automatische Speicherung des Entwurfs in `localStorage`

### Klang

- Echtzeitwiedergabe direkt im Browser ueber Web Audio und AudioWorklet
- zwei Anregungsmodelle:
  - `Violine`: harmonische Saite, nichtlineare Bogenanregung, Bogenrauschen
  - `Floete`: Rohrharmonische, Atemrauschen, Vibrato und Ueberblasen
- Referenzton einstellbar
- Bogenstaerke bzw. Luftstrom einstellbar
- Materialdaempfung einstellbar
- Oberflaechenhelligkeit/Klanghelligkeit einstellbar
- Lautstaerke einstellbar
- berechnete Resonanzmoden werden angezeigt
- Klang kann gestartet und gestoppt werden
- fuenf Sekunden Klang koennen im Browser als PCM-WAV heruntergeladen werden

### Darstellung

- dunkle responsive Studio-Oberflaeche
- Zeichenraster
- Markierung ausgewaehlter Objekte
- verbundene Objekte werden gestrichelt/tuerkis dargestellt
- sichtbare Eckmarken fuer ausgewaehlte Objekte
- animierter Resonanzrahmen waehrend der Wiedergabe

## Wichtige Klarstellung zu `Verbinden`

`Verbinden` ist aktuell **keine echte geometrische boolesche Vereinigung**.
Die Objekte behalten ihre eigenen Primitive und erhalten dieselbe Gruppen-ID.
Die Gruppe wird bei der Klanganalyse als gekoppelter Resonator behandelt und
visuell zusammengehoerig dargestellt.

Eine spaetere Version kann echte CSG-/Union-Operationen ergaenzen. Dafuer ist
eine Polygonbibliothek wie Clipper oder eine eigene Konturberechnung sinnvoll.

## Bedienung der Web-App

1. Python-Webserver starten.
2. Rechteck, Ellipse oder Dreieck in der Werkzeugleiste waehlen.
3. Form mit der Maus auf der Zeichenflaeche aufziehen.
4. Mit `Auswaehlen` ein Objekt verschieben oder seine Masse unten numerisch
   bearbeiten.
5. Mit Shift-Klick mehrere Teile auswaehlen.
6. `Verbinden` anklicken, um daraus einen gekoppelten Klangkoerper zu machen.
7. Anregungsmodell und Klangparameter rechts einstellen.
8. `Klang rendern` anklicken.
9. Mit `WAV rendern` eine fuenfsekündige Datei herunterladen.

Der Browser darf Audio erst nach einer Benutzeraktion starten. Der erste Klick
auf `Klang rendern` aktiviert deshalb gleichzeitig den AudioContext.

## Start

### Primaer: Web-App

```bash
cd /Users/metacube/Projects/Git/Ai/GeometricResonator
python3 web_app.py
```

URL:

```text
http://localhost:8765/
```

Ohne automatisches Browserfenster:

```bash
python3 web_app.py --no-browser
```

LAN-Bindung:

```bash
python3 web_app.py --host 0.0.0.0
```

Wichtig: `AudioWorklet` wird von Browsern ausserhalb von `localhost` meist nur
in einem sicheren HTTPS-Kontext freigegeben. Die reine LAN-IP ueber HTTP kann
deshalb zwar die Seite anzeigen, aber die AudioWorklet-Erzeugung blockieren.
Fuer einen echten Netzbetrieb sollte HTTPS oder ein lokaler Reverse Proxy
verwendet werden.

### Sekundaer: Tkinter-Desktop-App

```bash
cd /Users/metacube/Projects/Git/Ai/GeometricResonator
python3 -m pip install -r requirements.txt
python3 app.py
```

Die Desktop-App bietet Kreis, Dreieck, Quadrat und Sechseck als einzelne
Resonatoren, Echtzeitklang und WAV-Export. Der Paint-/Modellierworkflow wurde
bisher nur fuer die Web-App umgesetzt.

## Dateistruktur

```text
GeometricResonator/
├── HANDOFF.md               # diese vollstaendige Uebergabe
├── README.md                # kompakte Benutzeranleitung
├── requirements.txt         # NumPy und sounddevice fuer Desktop
├── web_app.py               # statischer Python-HTTP-Server
├── app.py                   # Tkinter-Desktopoberflaeche
├── synth.py                 # Python-Synthese und WAV-Export
├── test_synth.py            # Python-Unittests
└── web/
    ├── index.html           # Weblayout und Bedienelemente
    ├── modeler.js           # 2D-Editor, Analyse, Audio- und WAV-Steuerung
    └── synth-processor.js   # Echtzeitsynthese im AudioWorklet
```

`__pycache__/` ist ein generiertes Python-Artefakt und kein Quellcode.

## Architektur

```text
Python web_app.py
       |
       | liefert statische Dateien
       v
index.html + modeler.js
       |
       | customModes + Klangparameter per MessagePort
       v
AudioWorklet synth-processor.js
       |
       v
Browser-Audioausgang
```

Python liegt beim Webbetrieb nicht im Audiopfad. Das ist beabsichtigt: Maus,
Parameter und Audio bleiben im Browser und benoetigen keinen Netzwerk-Rundweg.
Dadurch ist die Echtzeitreaktion deutlich stabiler als serverseitiges
Audio-Streaming.

## Datenmodell des 2D-Editors

Jedes Objekt in `modeler.js` besitzt sinngemaess:

```javascript
{
  id: 1,
  type: "rect",       // rect, ellipse oder triangle
  x: 100,
  y: 120,
  w: 240,
  h: 160,
  group: 1            // null oder gemeinsame Gruppen-ID
}
```

Weitere zentrale Zustaende:

- `objects`: alle Primitive
- `selected`: Set der ausgewaehlten Objekt-IDs
- `tool`: `select`, `rect`, `ellipse` oder `triangle`
- `operation`: aktueller Draw-/Move-Vorgang
- `nextId`: naechste Objekt-ID
- `nextGroup`: naechste Gruppen-ID

Persistenzschluessel im Browser:

```text
geometric-resonator-design
```

Gespeichert werden Objekte, ID-Zaehler und Klangregler. Es existiert noch kein
Dateiformat fuer Projekt-Import/-Export.

## Geometrie-zu-Klang-Modell

Die Berechnung ist physikalisch inspiriert, aber keine exakte
Finite-Elemente-Simulation.

### Primitive Moden

Grundverhaeltnisse:

```text
Rechteck:  1.00, 1.41, 2.00, 2.24, 2.83
Ellipse:   1.00, 1.59, 2.14, 2.65, 3.16
Dreieck:   1.00, 1.67, 2.33, 2.81, 3.42
```

### Einfluss der Geometrie

- Flaeche beeinflusst die relative Skalierung eines Teilkoerpers.
- Das Seitenverhaeltnis beeinflusst hoehere Moden.
- Kleinere Teile erzeugen tendenziell hoehere Resonanzanteile.
- Gruppen erzeugen zusaetzliche Kopplungsmoden.
- Die Gesamtbreite/-hoehe des Modells beeinflusst den Klanggrundton.
- Maximal zwoelf bereinigte Moden werden an den AudioWorklet gesendet.

Die zentrale Berechnung liegt in `geometryAnalysis()` in `web/modeler.js`.

## Echtzeit-Synthese

`web/synth-processor.js` registriert den AudioWorklet-Prozessor:

```text
geometric-synth
```

Die Weboberflaeche sendet laufend:

```javascript
{
  instrument,
  pitch,
  size,
  excitation,
  damping,
  brightness,
  volume,
  customModes,
  gate
}
```

Der Worklet haelt Oszillatorphasen und die Lautstaerkehuellkurve zwischen den
Audiobloecken. Die Modenanzahl kann dynamisch bis zur gelieferten Liste
wachsen. Ein RMS-aehnlicher Pegel wird periodisch an die Oberflaeche
zurueckgemeldet und treibt die Animation an.

## WAV-Rendering

Der Browser rendert fuenf Sekunden offline in `modeler.js`:

1. Geometrie und aktuelle Parameter lesen.
2. Mono-Float-Audio mit 44.100 Hz berechnen.
3. Attack und Release anwenden.
4. In 16-Bit-PCM wandeln.
5. RIFF/WAV-Header schreiben.
6. Datei als `geometric-resonator.wav` herunterladen.

Das Offline-Modell ist musikalisch sehr aehnlich, aber nicht samplegenau
identisch mit dem AudioWorklet.

## Desktop-Synthese

`synth.py` verwendet:

- NumPy fuer Blockberechnung;
- `sounddevice.OutputStream` fuer Echtzeitaudio;
- Standardbibliothek `wave` fuer PCM-WAV;
- getrennte Violin- und Floetenmodelle;
- modale Koerperresonanzen fuer vier feste Formen.

Die Desktop-App ist nicht fuer den Webserver erforderlich.

## Tests und bisherige Verifikation

Python-Tests:

```bash
cd /Users/metacube/Projects/Git/Ai/GeometricResonator
python3 -m unittest -v
```

Aktuell vier Tests:

- Frequenz veraendert sich mit der Objektgroesse.
- Violine und Floete rendern endliches, nicht leeres Audio.
- unterschiedliche Formen erzeugen unterschiedliche Signale.
- WAV-Export erzeugt eine gueltig grosse Datei.

Letzter Stand: alle vier Tests erfolgreich.

Python-Syntax:

```bash
python3 -m py_compile app.py synth.py web_app.py
```

JavaScript-Syntax:

```bash
node --check web/modeler.js
node --check web/synth-processor.js
```

Letzter Stand: beide JavaScript-Dateien ohne Syntaxfehler.

Serverpruefung:

```bash
curl -fsS http://127.0.0.1:8765/
curl -fsSI http://127.0.0.1:8765/synth-processor.js
```

Die HTML-Seite und der Worklet wurden mit HTTP 200 ausgeliefert.

## Entwicklungsumgebung beim letzten Test

- macOS
- Python `3.14.3`
- Tkinter vorhanden
- NumPy `2.4.4`
- sounddevice `0.5.5`
- Node vorhanden fuer JavaScript-Syntaxpruefung
- Webserver-Port `8765`

Der Server wurde in der letzten Sitzung unter `http://localhost:8765/`
gestartet. Ein neues Modell darf nicht voraussetzen, dass dieser Prozess nach
einem Sitzungswechsel noch laeuft.

## Bekannte Grenzen und offene Fehlerklassen

### Editor

- Eckmarken werden gezeichnet, sind aber noch keine ziehbaren Resize-Handles.
  Groesse wird momentan numerisch oder beim ersten Aufziehen geaendert.
- Hit-Testing verwendet die Bounding Box; bei Ellipse und Dreieck ist es nicht
  pixelgenau.
- Keine Rotationsfunktion.
- Kein Freihand-/Bezier-/Polygonwerkzeug.
- Keine Auswahlbox durch Aufziehen.
- Kein Undo/Redo.
- Keine Ebenenansicht und keine Z-Reihenfolge-Befehle.
- Keine Projektdatei fuer Import/Export.
- `Verbinden` ist Gruppierung/Kopplung, keine boolesche Flaechenunion.

### Akustik

- Keine FEM-, BEM- oder Waveguide-Netzsimulation einer frei gezeichneten
  Kontur.
- Keine Materialdaten wie Dichte, Elastizitaetsmodul oder Wandstaerke.
- Kein frei platzierbarer Anregungs- und Abnahmepunkt.
- Ueberlappung und reale Kontaktflaeche verbundener Teile werden noch nicht
  physikalisch ausgewertet.
- Parameter werden im Worklet direkt aktualisiert; grosse Aenderungen koennen
  leichte Klicks erzeugen. Parameter-Smoothing/Crossfade ist offen.
- Echtzeit- und WAV-Renderer verwenden zwei aehnliche, aber getrennte
  Implementierungen.

### Web und Qualitaet

- Keine automatisierten Browser-/Interaktionstests.
- Kein Playwright-/Selenium-Test fuer Zeichnen, Gruppieren und AudioContext.
- Audio muss auf realen Browsern noch breit getestet werden, insbesondere
  Safari, Chrome und Firefox.
- HTTP-Zugriff ueber eine LAN-IP kann am Secure-Context-Erfordernis von
  AudioWorklet scheitern.
- Keine PWA, kein Offline-Cache, keine installierbare Web-App.
- `modeler.js` ist funktional, aber stark kompakt formatiert. Vor groesseren
  Erweiterungen sollte es in Module aufgeteilt und lesbarer formatiert werden.

## Empfohlene naechste Schritte

### Prioritaet 1: Editor wirklich Paint-/CAD-artig machen

1. Eck- und Kantenhandles zum direkten Skalieren implementieren.
2. Auswahlrahmen fuer mehrere Objekte implementieren.
3. Undo/Redo mit Command-Stack ergaenzen.
4. Rotation und exakte Winkeleingabe ergaenzen.
5. Projekt als JSON importieren und exportieren.

### Prioritaet 2: Zusammenfuegen verbessern

1. Ueberlappung und Beruehrung von Objekten erkennen.
2. Verbundene Komponenten statt nur Gruppen-ID berechnen.
3. Optional echte Polygon-Union einfuehren.
4. Kontaktflaeche als Kopplungsstaerke in die Resonanzanalyse aufnehmen.

### Prioritaet 3: Klangmodell vertiefen

1. Materialauswahl: Holz, Metall, Glas, Kunststoff.
2. Wandstaerke und Dichte ergaenzen.
3. Anregungspunkt und Abnahmepunkt im Editor platzieren.
4. Moden aus Raster-/Konturanalyse statt nur Primitive-Verhaeltnissen
   approximieren.
5. Floete als Rohr-/Jet-Waveguide und Violine als Bowed-String-Waveguide
   ausbauen.
6. Echtzeit- und Offline-Renderer auf einen gemeinsamen DSP-Kern bringen.

### Prioritaet 4: Produktqualitaet

1. JavaScript in `editor.js`, `geometry.js`, `audio.js` und `wav.js` teilen.
2. Playwright-Tests hinzufuegen.
3. HTTPS-Entwicklungsserver oder Reverse-Proxy dokumentieren.
4. Responsive Bedienung und Touch-Gesten pruefen.
5. Barrierefreiheit und Tastaturbedienung ergaenzen.

## Abnahmekriterien fuer die naechste sinnvolle Version

- Ein Objekt kann durch Ziehen an sichtbaren Handles skaliert werden.
- Mehrere Objekte koennen mit Auswahlrahmen markiert werden.
- Verbinden erkennt reale Kontakte oder Ueberlappungen.
- Ein Projekt kann als JSON gespeichert und wieder geladen werden.
- Klangparameter wechseln ohne Klickartefakte.
- Echtzeitklang funktioniert in Safari und Chrome auf macOS.
- Ein automatisierter Browser-Test zeichnet zwei Formen, verbindet sie,
  prueft die Modenliste und startet den AudioContext.

## Hinweise fuer das naechste Modell

- Primaer an der Web-App weiterarbeiten; die Tkinter-App nicht ungefragt
  entfernen.
- Vor Aenderungen `README.md` und diese Datei vollstaendig lesen.
- Bestehende Nutzerentwuerfe in `localStorage` moeglichst kompatibel halten.
- `customModes` ist die Schnittstelle zwischen Geometrie und Echtzeit-DSP.
- Bei einer Aenderung der Geometrieanalyse auch den WAV-Renderer pruefen.
- Nach jeder Aenderung mindestens Python-Tests, `node --check` und einen
  HTTP-Aufruf ausfuehren.
- Keine exakte physikalische Simulation behaupten, solange keine echte
  Kontur-/Materialsimulation implementiert ist.

## Kurzfassung fuer einen neuen Chat

> Arbeite im Projekt
> `/Users/metacube/Projects/Git/Ai/GeometricResonator`. Lies zuerst
> `HANDOFF.md` und `README.md`. Die primaere Anwendung ist eine lokale
> Web-App, in der Rechteck, Ellipse und Dreieck wie in Paint gezeichnet,
> dimensioniert und akustisch gekoppelt werden. `modeler.js` berechnet aus
> Flaeche, Seitenverhaeltnis und Gruppen Resonanzmoden; diese werden als
> `customModes` an `synth-processor.js` gesendet. Violin- und
> Floetenanregung sind in Echtzeit hoerbar, WAV-Export ist vorhanden. Der
> wichtigste naechste Schritt sind echte Resize-Handles, Undo/Redo und eine
> bessere geometrische Verbindung statt blosser Gruppen-ID.
