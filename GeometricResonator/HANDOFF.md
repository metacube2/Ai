# Geometric Resonator Studio — vollständige Projektübergabe

Stand: 2. August 2026
Projektpfad: `/Users/metacube/Projects/Git/Ai/GeometricResonator`

## Auftrag

Die Anwendung soll geometrische Klangkörper konstruierbar und unmittelbar
hörbar machen: Formen wie in einem Zeichenprogramm aufziehen, zusammenfügen,
und aus der Gesamtgeometrie den Klang ableiten.

Die Rückmeldung zur Vorversion war: **der Klang ist immer gleich.** Das war
zutreffend und hatte vier konkrete Ursachen im Code, die alle behoben sind.

## Was vorher falsch war

Die Vorversion hat nicht simuliert, sondern nachgeschlagen.

1. **Tabelle statt Rechnung.** `geometryAnalysis()` schlug feste
   Modenverhältnisse nach — `rect:[1,1.41,2,2.24,2.83]`,
   `ellipse:[1,1.59,…]` — und skalierte sie. Für ein einzelnes Objekt war
   der Skalenfaktor `sqrt(totalArea/area)` exakt 1, die Verhältnisse also
   konstant.
2. **Der Korpus folgte der Tonhöhe.** `bodySample()` trieb die Moden mit
   `frequency * ratio` an. Ein realer Klangkörper hat feste Eigenfrequenzen;
   dieser hier transponierte mit. Damit gab es überhaupt keine Formanten.
3. **Feste spektrale Hüllkurve.** Die Amplitude jeder Mode war
   `exp(-index * decay)` — abhängig vom *Index*, nicht von der Mode. Egal
   welche Frequenzen herauskamen, das Spektrum sah gleich aus.
4. **Der Korpus war 20–28 % des Signals.** Den Rest bestritt ein fester
   Oberton-Oszillator ganz ohne Geometriebezug.

Zusätzlich verwarf eine Bereinigung mit Toleranz 0,035 gerade die entarteten
Modenpaare, die einen Körper lebendig klingen lassen, und stellte jeder
Modenliste eine literale `1` voran.

## Was jetzt gerechnet wird

Gelöst wird das Eigenwertproblem der 2D-Membran auf der gezeichneten Fläche:

```text
-Δφ = λφ    im Gebiet,     φ = 0    am Rand (Dirichlet)
```

- **Diskretisierung:** finite Differenzen, Fünf-Punkte-Stern, regelmäßiges
  Raster. Kein FEM-Netz, aber dieselbe Differentialgleichung.
- **Verfahren:** blockweise inverse Iteration mit Rayleigh-Ritz-Projektion.
  Die Systemlösung erfolgt exakt über eine Band-Cholesky-Zerlegung statt
  iterativ, weil die Bandbreite bei zeilenweiser Knotennummerierung durch
  die Rasterbreite beschränkt bleibt. Für die kleine dichte Projektion
  dient ein zyklisches Jacobi-Verfahren.
- **Frequenz:** `f_i = c · √λ_i / 2π`, mit 1 px = 1 mm und `c` aus dem
  Material. Absolut, unabhängig vom Tonhöhenregler.
- **Lautstärke einer Mode:** `g_i = φ_i(Anregung) · φ_i(Abnahme)`. Eine Mode
  mit einem Knoten am Anregungspunkt bleibt stumm. Das ist der Punkt, an dem
  die Form tatsächlich hörbar wird.
- **Güte:** `Q_i = Q₀ · (f_i/f₁)^(−α) · Dämpfung`, mit `Q₀` und `α` aus dem
  Material.

Die Anregung erzeugt nur noch das Spektrum, das der Körper filtert. Daraus
folgt der Test, an dem sich das Modell messen lässt:

| Aktion | Erwartung |
|---|---|
| Tonhöhenregler bewegen | Grundton wandert, Resonanzen bleiben stehen |
| Form ändern | Resonanzen wandern, Grundton bleibt |

Beides ist in `tests/synth.test.mjs` als Prüfung hinterlegt.

## Genauigkeit

Gemessen in `tests/eigen.test.mjs` gegen geschlossene Lösungen:

| Prüfung | Ergebnis |
|---|---|
| Quadrat, Verhältnisse gegen `√((m²+n²)/2)` | max. 0,33 % Fehler |
| Kreis, Verhältnisse gegen Besselnullstellen | max. 0,28 % Fehler |
| Entartetes Paar (1,2)/(2,1) | beide vorhanden, 0,00 % Abstand |
| Rechteck 2:1 | max. 2,61 % Fehler |
| Verfeinerung 32 → 44 Zellen | Drift unter 0,5 % |
| Vierfache Kantenlänge | Frequenz exakt auf ein Viertel |

Der **absolute** Grundeigenwert liegt rund 9 % unter dem analytischen Wert.
Das ist erwartbar: der Dirichlet-Rand sitzt eine halbe Zelle außerhalb der
wahren Kontur, das Gebiet ist also effektiv etwas zu groß. Die Verhältnisse
— und damit die Klangfarbe — sind davon fast nicht betroffen, und die
absolute Lage wird ohnehin über die Materialkonstante `c` eingestellt.

## Grenzen, die ehrlich benannt gehören

- Es ist ein **2D-Membranmodell**, keine 3D-Simulation eines
  Instrumentenkorpus mit Decke, Boden und eingeschlossenem Luftvolumen.
- Die Materialwerte sind so gewählt, dass typische Zeichnungen musikalisch
  brauchbar klingen. Sie stammen nicht aus Werkstoffdatenblättern. Wandstärke
  und Dichte gibt es nicht.
- Anregungsmodelle sind Signalmodelle, keine Wellenleiter: der Bogen ist ein
  bandbegrenzter Sägezahn mit Bogenrauschen, der Luftstrom gefiltertes
  Rauschen mit schwachem Rohrton, der Anschlag ein kurzer Impuls. Physikalisch
  simuliert ist der **Körper**, nicht die Anregung.
- Eine reine Eckberührung überträgt auf einer echten Membran nichts, erzeugt
  im Raster aber eine Brücke von einer Zellbreite. Bewusst so belassen und in
  `tests/eigen.test.mjs` als Toleranz festgehalten.
- Die Rasterauflösung begrenzt, wie fein Konturen wirken. Bei 38 Zellen auf
  der längsten Seite ist der Umriss sichtbar treppig — das entspricht dem,
  was tatsächlich gerechnet wird.

## Dateistruktur

```text
GeometricResonator/
├── HANDOFF.md              # diese Übergabe
├── README.md               # kurze Anleitung
├── package.json            # type: module, Testskripte
├── web_app.py              # statischer Python-Server
├── app.py, synth.py        # Tkinter-Desktop-App (unverändert, alte Synthese)
├── test_synth.py           # Python-Tests der Desktop-App
├── web/
│   ├── index.html          # Oberfläche
│   ├── eigen.js            # Eigenlöser: Jacobi, Band-Cholesky, inverse Iteration
│   ├── geometry.js         # Rasterung, Zusammenhangskomponenten
│   ├── physics.js          # Eigenwerte -> Frequenz, Güte, Modenverstärkung
│   ├── modes-worker.js     # Modul-Worker um geometry + physics
│   ├── editor.js           # Treffertests, Einrasten, Darstellung
│   ├── modeler.js          # Zustand, Eingabe, Audio, Verdrahtung
│   ├── synth-processor.js  # AudioWorklet: Anregung + Modalresonatorbank
│   └── wav.js              # Offline-Rendering und WAV-Kodierung
└── tests/
    ├── eigen.test.mjs      # Löser gegen analytische Lösungen
    ├── synth.test.mjs      # DSP und Kette Geometrie -> Klang
    ├── browser-smoke.html  # Probe, die im Browser läuft
    └── browser-smoke.mjs   # Runner mit eigenem Server und Chrome
```

`eigen.js`, `geometry.js` und `physics.js` sind reine ES-Module ohne DOM- und
ohne Worklet-Abhängigkeit. Deshalb laufen sie unter node und sind gegen
geschlossene Lösungen prüfbar — das war die Voraussetzung dafür, der
Physik überhaupt trauen zu können.

## Architektur

```text
modeler.js  ──Objekte, Material──▶  modes-worker.js
    ▲                                    │
    │                             geometry.js (Rasterung, Komponenten)
    │                             eigen.js    (Eigenlöser)
    │                             physics.js  (Frequenz, Güte, Verstärkung)
    │                                    │
    └────Moden + Schwingungsformen───────┘
    │
    ├──▶ editor.js          zeichnet Komponenten und Schwingung
    ├──▶ synth-processor.js Echtzeit im AudioWorklet
    └──▶ wav.js             derselbe Worklet im OfflineAudioContext
```

Python liegt nicht im Audiopfad und nicht im Rechenpfad. Es liefert nur
Dateien aus.

Der Worker rechnet während des Ziehens grob (24 Zellen) und nach dem
Loslassen fein (38). Eine feine Lösung braucht im Browser rund 50 ms.

## Verbinden

Verbunden ist, was sich berührt. Es gibt keinen Verbinden-Knopf und keine
Gruppen-ID mehr.

Die Rasterung prüft je Zelle neun Teilproben gegen die **Vereinigung** aller
Objekte und markiert die Zelle bei Mehrheit als belegt. Zwei Teile mit
gemeinsamer Kante belegen dadurch zuverlässig benachbarte Zellen. Die
Zerlegung in Zusammenhangskomponenten läuft mit Vier-Nachbarschaft.

Damit ist Verbundenheit keine Metadaten-Eigenschaft mehr, sondern eine
geometrische Tatsache, die unmittelbar in die Physik eingeht: der
zusammengesetzte Körper hat einen tieferen Grundton und neue Moden.

Beim Verschieben rasten Kanten mit 9 px Toleranz ein; Kontaktkanten haben
Vorrang vor bloßer Ausrichtung. Ohne das müsste man pixelgenau zielen.

Entwürfe der Vorversion enthalten ein Feld `group`. Sie laden weiterhin, das
Feld wird ignoriert. Der Speicherschlüssel ist neu:
`geometric-resonator-design-v2`.

## Echtzeitsynthese

`web/synth-processor.js`, Prozessorname `geometric-synth`.

Signalweg: Anregung → Modalresonatorbank → weiche Begrenzung → Ausgang.

Jede Mode ist ein Zweipolresonator mit Nullstellen bei ±1 und
Spitzenverstärkung etwa 1, im Zustand durchgehend `Float64` — mit `Float32`
werden hochgütige tiefe Resonatoren instabil.

Bei einer Geometrieänderung gleiten die Koeffizienten über etwa 20 ms auf die
neuen Werte, statt zu springen. Ein noch stummer Resonator bekommt seine Pole
sofort, damit er nicht hörbar von einer alten Frequenz auf die neue wandert.

Bemerkenswert am Anschlag: ein kurzer Impuls regt einen schmalbandigen
Resonator um etwa `1/Q` schwächer an als eine Dauererregung. Ohne Ausgleich
wäre der Anschlag bei Holz rund 30 dB leiser als Bogen und Luftstrom. Der
Ausgleich in `ModalBank.setModes` ist gütengewichtet und hält die
Anschlagslautstärke über alle Materialien vergleichbar; der hörbare
Kontaktanteil bleibt davon unberührt.

Parameter kommen beim Erzeugen über `processorOptions`, nicht nur per
Nachricht. Im `OfflineAudioContext` erreicht eine vor `startRendering()`
gesendete Nachricht den Prozessor nicht zuverlässig vor dem ersten Block —
das äußerte sich als vollständig stille WAV-Dateien.

## WAV-Export

`web/wav.js` rendert fünf Sekunden in einem `OfflineAudioContext` mit
demselben Worklet wie die Echtzeitwiedergabe. Der frühere zweite,
getrennt gepflegte Synthesepfad ist entfallen; damit kann er auch nicht mehr
auseinanderlaufen.

Der Ausklang wird über eine Verstärkungsautomation gesteuert, nicht über eine
Nachricht: Automationen sind abtastgenau, Nachrichtenzustellung nicht.

## Tests

```bash
npm test                       # alle drei Suiten
node tests/eigen.test.mjs      # 15 Prüfungen
node tests/synth.test.mjs      # 15 Prüfungen
node tests/browser-smoke.mjs   # 9 Prüfungen
python3 -m unittest -v         # 4 Prüfungen der Desktop-App
```

Letzter Stand: 15/15, 15/15, 9/9, 4/4.

`tests/synth.test.mjs` lädt die **echte** Datei `web/synth-processor.js` mit
einem Schim für `AudioWorkletProcessor` und `registerProcessor`. Es gibt
keinen zweiten Nachbau der Synthese im Testcode.

`tests/browser-smoke.mjs` bringt einen eigenen Server mit, startet Chrome
headless und nimmt das Ergebnis per POST entgegen. Ein bloßes `--dump-dom`
genügt nicht, weil die Ergebnisse asynchron entstehen, und
`--virtual-time-budget` verfälscht jede Zeitmessung im Worker. Ohne Chrome
überspringt sich die Probe.

Worauf bei neuen Tests zu achten ist: „unterschiedliche Formen erzeugen
unterschiedliche Signale" ist **keine** brauchbare Prüfung — das erfüllt
schon reines Rauschen. Geprüft werden muss, dass die Resonanzen dort liegen,
wo die Rechnung sie vorhersagt, und dass sie sich nicht mit der Tonhöhe
verschieben.

Ebenso taugt der Abstand benachbarter Abtastwerte nicht als Knacktest: die
Anregung enthält Rauschen mit von Natur aus großen Sprüngen. Ein Knacken
zeigt sich als Ausschlag über den bisherigen Spitzenwert hinaus.

## Offene Punkte

### Editor

- Eckmarken sind sichtbar, aber keine ziehbaren Griffe; Größe ändert man
  numerisch oder beim ersten Aufziehen.
- Kein Undo/Redo, keine Rotation, kein Auswahlrahmen, keine Polygone.
- Kein Projektformat für Import und Export.
- Treffertests laufen über die Bounding Box, bei Ellipse und Dreieck also
  nicht pixelgenau.

### Physik

- Wandstärke und Dichte fehlen; nur eine effektive Ausbreitungs-
  geschwindigkeit je Material.
- Kein Plattenmodell (Biegesteifigkeit) als Alternative zur Membran. Für
  Metall wäre das der nächste sinnvolle Schritt, weil eine Platte eine andere
  Dispersionsrelation hat als eine Membran.
- Die Anregung koppelt einseitig in den Körper; es gibt keine Rückwirkung des
  Körpers auf die Anregung. Eine echte Streichsimulation bräuchte einen
  Wellenleiter mit Reibungskennlinie.
- Nur der Körper unter dem Anregungspunkt klingt. Mehrere gleichzeitig
  angeregte Körper wären möglich, sind aber nicht umgesetzt.

### Werkzeug

- Nur die Chrome-Rauchprobe; Safari und Firefox sind ungetestet.
- Kein Test der Mausbedienung selbst (Zeichnen, Einrasten, Punkte ziehen).
- Die Desktop-App hat die neue Physik nicht. Das wäre ein eigenes Stück
  Arbeit, weil der Eigenlöser dafür in Python vorliegen müsste.

## Hinweise für das nächste Modell

- Vor Änderungen `README.md` und diese Datei lesen, dann `npm test`.
- Die Physik lebt in `eigen.js`, `geometry.js` und `physics.js` — bewusst ohne
  Browserabhängigkeit. Diese Trennung nicht aufgeben; sie ist der Grund,
  warum die Rechnung überprüfbar ist.
- Wer die Geometrieanalyse ändert, prüft `tests/eigen.test.mjs` gegen die
  analytischen Lösungen, bevor er dem Klang traut.
- Keine exakte physikalische Simulation eines Instruments behaupten. Es ist
  eine 2D-Membranrechnung der gezeichneten Fläche — das ist echt, aber
  begrenzt.
- Beim Anfassen der Anregungsmodelle daran denken, dass sie breitbandig
  bleiben müssen. Eine Anregung ohne Anteil oberhalb weniger Kilohertz lässt
  die hohen Körpermoden stumm, und dann klingt wieder alles gleich.

## Kurzfassung für einen neuen Chat

> Arbeite im Projekt `/Users/metacube/Projects/Git/Ai/GeometricResonator`.
> Lies zuerst `HANDOFF.md` und `README.md`, dann `npm test`. Die Web-App
> löst das Eigenwertproblem der 2D-Membran auf der gezeichneten Fläche
> (finite Differenzen, `web/eigen.js`) und macht die Eigenmoden über eine
> Modalresonatorbank im AudioWorklet hörbar. Frequenzen sind absolut,
> Modenlautstärken kommen aus den Eigenvektoren an Anregungs- und
> Abnahmepunkt. Berührende Teile sind automatisch ein Körper. Nächste
> sinnvolle Schritte: ziehbare Skaliergriffe, Undo/Redo, ein Plattenmodell
> als Alternative zur Membran.
