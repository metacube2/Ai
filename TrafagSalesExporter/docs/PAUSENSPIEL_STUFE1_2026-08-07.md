# Pausenspiel „Drohnen-Duell" — Stufe 1 gebaut

Stand: 2026-08-07. Konzept: `docs/PAUSENSPIEL_DROHNEN_KONZEPT_2026-08-07.md`.

**Nicht deployt.** Ingo publiziert selbst im Manual Mode.

## Was entstanden ist

| Datei | Zweck |
|---|---|
| `Components/Pages/PauseGame.razor` | Route `/pause`, laedt das Spielmodul, sonst fast leer |
| `Services/PauseGameOptions.cs` | Schalter `Pause:Enabled` |
| `wwwroot/js/pausegame.js` | Spielkern: Gelaende, Physik, 3D-Szene, Gegner, Ton |
| `wwwroot/js/modplayer.js` | ProTracker-MOD-Abspieler, selbst geschrieben |
| `Tools/PauseGame.Probe/probe.mjs` | kopfloser Test des Spielkerns (18 Pruefungen) |
| `Tools/PauseGame.Probe/modprobe.mjs` | kopfloser Test des MOD-Abspielers (18 Pruefungen) |

Geaendert: `Program.cs` (+1 Zeile DI), `appsettings.json` (Abschnitt `Pause`),
`DatabaseSeedService.cs` (+1 Menueintrag), `UiTextGeneratedTranslations.cs`
(12 Eintraege). Keine bestehende Seite und keine Berechnung angefasst.

## Ausblenden

Drei Ebenen, wie im Konzept:

1. `"Pause": { "Enabled": false }` in `appsettings.json` — die Seite zeigt nur noch
   einen Hinweis, das Spielmodul wird gar nicht geladen.
2. Menueintrag `pause-game` auf `IsVisible = false` — Eintrag weg, Route bleibt.
3. `RequiredPolicy` am Menueintrag.

## Ton

**Standardmaessig aus.** Beide Haken stehen beim ersten Start auf leer und werden im
`localStorage` gemerkt; der Ton geht erst mit dem Klick auf „Partie starten" an —
vorher laesst kein Browser einen AudioContext zu.

**Geraeusche** sind vollstaendig synthetisiert, es liegt keine einzige Audiodatei im
Repository: Explosion (Rauschstoss durch absinkenden Tiefpass plus Bassschlag),
Drohnenstart, Rotorsurren, Sprung, Wasserplatscher, Treffer.

**Musik** spielt eine `.mod`-Datei, die der Anwender selbst auswaehlt.

- **Warum selbst geschrieben und keine Bibliothek:** alles muss lokal liegen. Der
  Produktivserver laedt nichts nach — three.js liegt aus demselben Grund unter
  `wwwroot/js/vendor`. Ein WASM-Blob von einem CDN waere das Gegenteil davon.
  `modplayer.js` ist ein vollstaendiger ProTracker-Abspieler in rund 400 Zeilen:
  31- und 15-Sample-Module, 4 bis 32 Kanaele, lineare Interpolation, weiche
  Begrenzung, Effekte `0` Arpeggio, `1`/`2` Portamento, `3`/`5` Tonportamento,
  `4`/`6` Vibrato, `9` Sample-Offset, `A` Volume-Slide, `B` Positionssprung,
  `C` Lautstaerke, `D` Pattern-Break, `E1/E2/E9/EA/EB/EC/ED/EE`, `F` Tempo.
- **Warum blockweise gerendert und eingereiht:** ein `ScriptProcessorNode` ist
  veraltet und laeuft im Haupt-Thread — er knackst, sobald WebGL ruckelt. Ein
  `AudioWorklet` braucht eine eigene Moduldatei, in der der Mischer ein zweites Mal
  liegen muesste. Stattdessen werden 0.25-Sekunden-Bloecke rund 0.9 Sekunden im
  Voraus gerendert und eingereiht; Ruckler bis 0.9 s sind dadurch unhoerbar.
- **Es liegt keine MOD-Datei bei.** Ich lade keine aus dem Netz (externer Zugriff und
  ungeklaerte Rechte). Wer Musik will, waehlt eine eigene Datei aus.
- Beim Verlassen der Seite wird der AudioContext geschlossen — sonst spielt er
  weiter, waehrend jemand im Cockpit arbeitet.

## Nachweis

Es gibt in dieser Umgebung **keine Browser-Automatisierung**. Geprueft ist deshalb
der rechnende Teil, kopflos in Node — also genau das, was ueber Funktionieren oder
Nicht-Funktionieren entscheidet. Licht, Kamera und Spielgefuehl sind es nicht.

```
node Tools/PauseGame.Probe/probe.mjs      18 Pruefungen gruen
node Tools/PauseGame.Probe/modprobe.mjs   18 Pruefungen gruen
dotnet test                               455/455 gruen (unveraendert)
```

**Spielkern** (`probe.mjs`): Gelaende ueber 40 Zufallskarten durchgehend begehbar
(groesste Stufe 7 px bei 14 px erlaubt) und ueber Wasser; Einschlag entfernt Boden an
der richtigen Stelle und laesst 120 px daneben alles stehen; Wurf landet im Gelaende;
Rechnergegner trifft 35 von 40 Zufallslagen im Wirkradius bei einem Median von 6 px
gegen 174 px bei einem sturen 45-Grad-Schuss; leichter Grad trifft 22 von 40, also
spuerbar schlechter; kein Schuss ist staerker, als ein Mensch schiessen kann;
Anmarsch laeuft in beide Richtungen und macht 434 px in 7 Sekunden gut.

**MOD-Abspieler** (`modprobe.mjs`): baut ein vollstaendiges Modul im Speicher und
rechnet am Ausgang nach — Tonhoehe ueber Nulldurchgaenge **129.8 Hz gegen 129.5 Hz
erwartet**, kleinere Periode klingt hoeher, `C00` schaltet stumm, `F` setzt das
Tempo, Kanal 1 liegt links und Kanal 2 rechts, 30 Sekunden Dauerlauf ueber drei
Durchlaeufe ohne Aussetzer und ohne uebersteuerte Werte, zu kurze Datei wird
abgelehnt, beliebige Datei stuerzt nicht ab.

## Vier Fehler, die die Pruefsonden gefunden haben

Alle vier waren in Code, den ich fuer richtig hielt, und keiner davon waere ohne den
kopflosen Test vor dem ersten Spielen aufgefallen.

1. **Explosionen stanzten spiegelverkehrt.** `carve` schrieb Canvas-Zeilen (y nach
   unten) in ein Array, das in Welt-y (nach oben) indiziert ist. Krater waeren an der
   gespiegelten Stelle entstanden.
2. **Der Rechnergegner schoss aus unerreichbarer Entfernung ins Leere.** Die groesste
   Wurfweite liegt bei rund 560 px, die Karte ist 1200 px breit. Er zielte nur und
   lief nie — in der Messung war das die Haelfte aller Schuesse. Jetzt marschiert er
   an, solange kein brauchbarer Schuss existiert (Zeitbudget 7 s je Zug).
3. **Bergab blieb der Wurm in der Luft haengen.** `walk` hob ihn nur an
   (`Math.max`), heruntergezogen hat ihn allein die Schwerkraft — dauerhaft rund
   2 px ueber Grund und damit ausserhalb der Toleranz von `onGround`. Folge waere
   gewesen: Laufen setzt mitten im Zug aus, und jedes Gefaelle loest Fallschaden aus.
4. **Eine beliebige Datei riss den MOD-Abspieler mit** (`RangeError` bei negativer
   Arraylaenge). Jeder kann im Dateidialog irgendetwas auswaehlen; jetzt sind
   Musterzahl und Sampledaten gegen die tatsaechliche Dateigroesse begrenzt.

Dazu zwei Fehler in den Tests selbst, die ich korrigiert habe statt sie
wegzudefinieren: der Panorama-Test liess beide Kanaele gleichzeitig klingen und war
dadurch symmetrisch — er konnte grundsaetzlich nichts zeigen. Und der erste
Gegner-Test stellte das Ziel genau auf eine Kuppe; dort ist die Flugbahn zweigipflig
und **jeder** Schuetze verfehlt regelmaessig, auch ein perfekter. Der Test mass die
Landschaft, nicht den Gegner. Jetzt: Median ueber 40 Zufallslagen und ein Vergleich
gegen einen sturen Schuetzen.

## Die Lokalisierungsfalle ist eingetreten

Wie im Konzept vorhergesagt. Der Scanner verlangte drei Schluessel, darunter
`import` — ein Kunstpaar aus den benachbarten Zeichenkettenargumenten von
`InvokeAsync("import", "./js/pausegame.js")`, genau dasselbe Muster wie beim
Einkauf-Deploy. Behoben, indem der Pfad eine Konstante wurde; damit stehen keine zwei
Literale mehr nebeneinander. Uebrig blieben zwei echte Schluessel (`Pause`,
`Der Pausenreiter ist ausgeschaltet.`), ergaenzt in sechs Sprachen = 12 Eintraege.
Aller Spieltext liegt in `pausegame.js` und kennt nur de/en — waere er in der Razor,
waeren es rund 240 Uebersetzungen.

## Ausdruecklich NICHT geprueft

- **Nichts davon lief je in einem Browser.** Es gibt hier keine
  Browser-Automatisierung. Ob die Szene erscheint, ob die Kamera brauchbar ist, ob
  sich das Zielen gut anfuehlt, ob die Geraeusche passen und ob die Musik
  unterbrechungsfrei laeuft — das kann nur Ingo sehen. Der erste Aufruf ist ein
  echter Test, kein Formalakt.
- Die drei Drohnentypen sind nur rechnerisch geprueft. Ueberflug und freier Flug
  haengen an Tastendruecken, die kopflos nicht ausloesbar sind.
- Leistung auf aelteren Arbeitsplatzrechnern: nicht gemessen. Schattenkarte
  1024 x 1024, `setPixelRatio` auf hoechstens 2 begrenzt. Der Schalter „Effekte
  reduzieren" aus dem Konzept ist **noch nicht gebaut** (Stufe 2).
- Ob der Produktiv-IIS die neuen `.js`-Dateien zwischenspeichert und wie sich das auf
  Aktualisierungen auswirkt. Fuer `finance3d.js` besteht dasselbe Thema.
- Der MOD-Abspieler ist gegen ein selbst gebautes Testmodul geprueft, **nicht gegen
  eine echte Musikdatei aus freier Wildbahn**. Reale Module nutzen Effekte in
  Kombinationen, die mein Testmodul nicht enthaelt.

## Offen fuer Stufe 2

Restliche drei Drohnentypen (Schwarm, Bohr, Berge), Schwierigkeitsgrade im
Startbildschirm auch fuer `Schwarmfuehrer`/`Luftwacht` durchreichen (die Stufen
rechnen bereits, die Auswahl steht), Sudden Death sichtbar machen, Bestenliste
anzeigen (sie wird bereits geschrieben), Gelaende mit `ExtrudeGeometry` statt
Alphaplatte, Schalter „Effekte reduzieren".
