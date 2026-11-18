# Geometric Resonator

Vollstaendige technische Projektuebergabe fuer die Weiterarbeit:
[HANDOFF.md](HANDOFF.md)

Interaktive Python-Klangskulptur fuer macOS und den Webbrowser. Ein sichtbares geometrisches
Objekt wird entweder durch Bogenreibung (Violine) oder Luftstrom (Floete)
angeregt. Form, Groesse und Daempfung bestimmen die modalen Resonanzen.

## Funktionen

- Kreis, Dreieck, Quadrat und Sechseck mit eigenen Resonanzverhaeltnissen
- physikalisch inspirierte Violin- und Floetensynthese
- Maussteuerung: horizontal Tonhoehe, vertikal Bogen/Luftstrom
- Echtzeit-Audio mit `sounddevice`
- animierte Resonanzdarstellung mit Tkinter
- Export des aktuellen Klangs als viersekündige WAV-Datei
- Browser-Version mit Echtzeitklang ueber Web Audio
- 2D-Klangkoerper-Editor wie in einem einfachen Zeichenprogramm
- Rechteck, Ellipse und Dreieck frei aufziehen
- Objekte verschieben sowie Breite, Hoehe und Position numerisch bearbeiten
- mehrere Teile zu einem gekoppelten Resonator verbinden
- Geometrieanalyse und WAV-Rendering des konstruierten Klangkoerpers

Die Synthese ist bewusst ein musikalisches Modell und keine exakte
Finite-Elemente-Simulation realer Instrumente.

## Start

### Web-App

```bash
cd /Users/metacube/Projects/Git/Ai/GeometricResonator
python3 web_app.py
```

Danach oeffnet sich `http://localhost:8765`. Die Audiosynthese laeuft direkt
im Browser. Zuerst werden Formen gezeichnet und optional miteinander verbunden;
`Klang rendern` regt den konstruierten Koerper in Echtzeit an. Fuer Zugriff aus
dem LAN kann der Server mit `python3 web_app.py --host 0.0.0.0` gestartet
werden; dann muss Port `8765` in der lokalen Firewall erreichbar sein.

### Desktop-App

```bash
cd /Users/metacube/Projects/Git/Ai/GeometricResonator
python3 -m pip install -r requirements.txt
python3 app.py
```

Bedienung:

- Objekt anklicken und gedrueckt halten, um Klang zu erzeugen.
- Maus horizontal bewegen, um die Tonhoehe zu aendern.
- Maus vertikal bewegen, um Bogenreibung oder Luftstrom zu dosieren.
- Alternativ Leertaste halten oder `Ton halten` aktivieren.

## Tests

```bash
python3 -m unittest -v
```

Die Tests pruefen beide Instrumente, die Geometrieabhaengigkeit, die
Groesse-/Frequenzkopplung und den WAV-Export. Fuer die Tests wird kein
Audiogeraet benoetigt.
