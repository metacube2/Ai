# Peakfinder: Gipfelnamen im Aurora-Livebild

Live seit 2026-10-28 auf <https://www.aurora-weather-livecam.com/>. Über dem Live-Video sitzen anklickbare Punkte auf
den sichtbaren Gipfeln. Ein Klick zeigt Name, Höhe, Distanz und Richtung, zum Beispiel
„Etzel · 1'098 m · 13.2 km · Richtung 217°“, dazu „© OpenStreetMap-Mitwirkende“. Der Knopf **„⛰ Gipfel“** unten rechts
im Video schaltet die Ebene ein und aus. Beim ersten Besuch ist sie eingeschaltet, danach merkt sich jeder Browser die
Wahl (`localStorage` `auroraGipfel`). Daneben sitzt ein Vollbild-Knopf für den ganzen Videobereich; die Marken bleiben
im Vollbild, beim Zoomen und auf Handybreite auf derselben Bildstelle.

Betrieb, Deploy und Rückbau stehen kurz in `instructions/CAURORA_RUNBOOK.md` (Abschnitt Peakfinder). Diese Datei
erklärt, **wie** die Berge gefunden und Standort und Blickwinkel der Kamera bestimmt wurden.

## Kurzfassung

Die Berge werden **nicht im Bild erkannt**. Sie werden aus Landkarte und Höhenmodell **vorausberechnet**. Damit das
stimmt, wurden vorher Standort und Ausrichtung der Kamera aus dem Bild selbst bestimmt. Ein Dienst auf cAurora gleicht
nur noch das leichte Wandern der Kamera aus.

## 1. Welche Berge es gibt

- **Gipfel:** OpenStreetMap, alle Punkte `natural=peak` mit Namen im Rechteck 46,3–47,7 N / 7,4–9,9 E (etwa Wallis
  bis Schaffhausen, Bern bis hinter Chur). Abgefragt über Overpass, das sind 5872 Gipfel, davon 5639 mit Höhenangabe.
  Je Gipfel sind Name, Höhe und Lage bekannt (`gelaende.py`, `gipfel_holen`).
- **Gelände:** Höhenmodell aus den freien Terrarium-Kacheln (AWS Open Data „elevation-tiles-prod“; Quellen u. a. SRTM,
  EU-DEM, swisstopo). Bis 8 km Zoomstufe 13, bis 60 km Stufe 12, weiter Stufe 11. Damit ist die Geländehöhe an jedem
  Punkt bekannt.

## 2. Was von der Kamera aus sichtbar ist

- **Sichtstrahlen:** Für jede Richtung läuft von der Kamera ein Strahl bis 160 km über das Gelände. Die scheinbare
  Höhe eines Punkts in Distanz d ist `dz = h − h_kamera − d² / (2R) · (1 − k)` mit Erdradius R und Refraktion k = 0,13
  (Erdkrümmung und Lichtbrechung der Luft).
- **Vorauswahl:** Nur Gipfel, die im Bildfeld liegen (Azimut ≈ 182°–273°) und 8–150 km entfernt sind. Das sind gut
  2500. Unter 8 km wird nicht beschriftet, weil dort schon 300 m Standortfehler einige Grad ausmachen.
- **Sichtbarkeit:** Ein Gipfel zählt, wenn auf dem Strahl davor nichts höher liegt **und** er mindestens 0,15° über
  das Gelände davor ragt. Zudem muss er im Bild zwischen 30 % und 90 % der Höhe liegen.
- **Ergebnis:** 31 Gipfel. Beispiele dafür, was fehlt: Der Titlis (211°, 65 km) liegt zwar im Bildfeld, ist laut
  Rechnung aber verdeckt oder ragt zu wenig heraus. Der Säntis (94°) und der Bachtel (21°, 2,8 km) liegen ausserhalb des
  Bildfelds; die Kamera schaut nach Südwesten.

| Richtung | Gipfel |
|---|---|
| 182°–192° (linker Rand, „Lage ungenau“) | Chli Aubrig, Pfiiffegg, Höch Hund, Chläbdächer, Piet, Druesberg, Forstberg, Gross Windgällen |
| 197°–214° | Stöcklichrüz, Gitschenhöreli, Grosser Mythen, Brunnistock, Blackenstock, Klein Mythen, Haggenspitz, Urirotstock, Schlieren, Schlossstock, Wissigstock, Engelberger Rotstock, Höch Tändli, Nüsellstock |
| 217°–236° | Etzel, Tomislochhöchi, Ängihöchi, Wildspitz, Sparenfirst, Bruusthöchi |
| 264°–268° | Albishorn, Bürglen, Schnabel |

Die vollständige Liste mit Höhe, Distanz, Richtung und Bildposition steht in `gipfel.json`.

**Beschriftung auf der Seite:** Jeder Gipfel hat einen Rang: Am stärksten zählt, wie weit die Spitze über das Gelände
davor ragt; dazu kommen ein Bonus für einen Wikipedia-Eintrag, eine kleine Liste bevorzugter Namen in
`passpunkte.json` und die Höhe. Die Seite setzt die Namen in dieser Reihenfolge; ein Name, der einen anderen überdecken
würde, bleibt ein Punkt ohne Schrift. Die Abstände gelten in Bildschirmpixeln, deshalb zeigt Hineinzoomen mehr Namen.

## 3. Wo die Kamera steht und wohin sie schaut

Das war der eigentliche Aufwand. Die Kamera hat ein Fischauge, gerade Linien sind also krumm. Gesucht waren
**11 Werte gleichzeitig** (`modell.py`):

- **Ausrichtung:** Blickrichtung (Gier), Neigung, Rollwinkel.
- **Objektiv:** Brennweite und zwei Werte für die Fischaugen-Krümmung. Das Modell ist Kannala-Brandt
  `r = f · θ · (1 + k1 θ² + k2 θ⁴)`. Dazu kommt die Lage der optischen Achse gegenüber der Bildmitte.
- **Lage:** Verschiebung Nord/Ost und Höhe gegenüber einem Startpunkt.

Drei voneinander unabhängige Messungen wurden gemeinsam ausgeglichen (`kalibrieren.py`, robuste kleinste Quadrate):

1. **Horizontlinie:** Im Referenzbild vom 25.10.2026 09:00 (`referenz_20261025_090059.jpg`) wurde die Kante zwischen
   hellem Himmel und dunklem Gelände automatisch gesucht. Das Höhenmodell sagt, wie diese Linie von einem bestimmten
   Standort aus verlaufen müsste. Verglichen wurde nur mit Graten bis 35 km, weil die fernen Alpen oft im Dunst
   liegen. Die Werte wurden verschoben, bis beide Linien übereinander lagen.
2. **Feste Punkte:** Die beiden Mythen-Zacken sind unverwechselbar. Ihre Spitzen wurden von Hand im 4-fach
   vergrösserten Referenzbild abgelesen (`passpunkte.json`, mit Herkunft je Punkt). Sie geben die Richtung genau vor.
3. **Sonne als Messlatte:** Die Sonnenposition ist für jede Sekunde astronomisch exakt berechenbar und hängt nicht vom
   Höhenmodell ab. Benutzt wurden 39 Sonnenpunkte aus Bildern vom Sonnenuntergang am 22.10. und vom Tagesverlauf am
   27.10. Jedes dieser Bilder wurde vorher um seine eigene Kameraverschiebung gegenüber der Referenz korrigiert.

**Ergebnis** (`gipfel.json`, Feld `kamera`/`standort`):

| Wert | Ergebnis |
|---|---|
| Standort | 47.27157 N / 8.87329 E, 576 m, auf etwa ±100 m |
| Blickrichtung der optischen Achse | 230,6°; Bildmitte auf Horizonthöhe ≈ 229° (Südwest) |
| Bildfeld auf Horizonthöhe | etwa 182°–273° (≈ 91°) |
| Neigung / Rollwinkel | 8,7° / 0,9° |
| Brennweite, Fischauge | f = 2253 px, k1 = −0,268, k2 = 0,073 |
| Optische Achse gegen Bildmitte | +69 / −10 px |

**Standort weicht ab:** Die bei windy und in der App eingetragene Koordinate „Dorfkern Oberdürnten“
(47.278682 / 8.860951) liegt etwa 1,2 km nordwestlich. Mit ihr passt der Horizont nicht, die Mythen lägen etwa 75 px
daneben. Die Korrektur bei windy (dort Anmeldung nötig) und in der App ist noch offen. Werte zum Eintragen: Standort
oben, Blickrichtung 229°.

## 4. Wie gut es passt

Pixelangaben bei 3072 px Bildbreite; 10 px sind etwa 4 px in einem 1200 px breiten Player.

| Prüfung | Abweichung |
|---|---|
| Mythen, Ausgleich **ohne** die Mythen gerechnet (Gegenprobe) | 6–9 px waagrecht, ≤ 1 px senkrecht |
| Sonnenuntergang 22.10., aus dem Ausgleich genommen | waagrecht 2–15 px (Blendung begrenzt die Ablesung) |
| Horizont gegen Höhenmodell | Median 2,1 px, p90 6,2 px |
| Etzel, Wildspitz | senkrecht ≤ 2 px; waagrecht nur ±10–20 px ablesbar (runde Kuppen) |
| Mythen in der Kameralage vom 27.10. (von Hand abgelesen, nach Versatz) | −0,1 / +1,6 px |
| Mythen innerhalb des Ausgleichs | ≤ 1 px |

Der linke Rand (x < 300 px, 8 Gipfel) ist nicht unabhängig geprüft. Druesberg und Forstberg sitzen sichtbar auf
Schneegipfeln, aber welcher Gipfel welcher ist, lässt sich nicht gegenprüfen. Diese Karten zeigen „Lage am Bildrand
ungenau“.

## 5. Was im Betrieb passiert

- **Feste Positionen:** Die 31 Bildpositionen sind vorausberechnet und liegen als `js/peakfinder-daten.js` bei der
  Seite. `js/peakfinder.js` legt die Punkte über das Video und rechnet sie bei Zoom, Vollbild und Fensterbreite um.
- **Nachführung:** Die Kamera wandert leicht. Zwischen 21. und 28.10. sprang das Bild um bis zu 40 px, dazu kommen
  wenige Pixel im Tagesverlauf. Der Dienst `caurora-peakfinder.timer` auf cAurora (alle 5 min,
  `/usr/local/sbin/caurora-peakfinder-abgleich` aus `abgleich.py`) vergleicht `image/current.jpg` mit
  `/var/lib/caurora-peakfinder/referenz_band.png`:
  - Messung über Kantenbild des Horizontbands und Phasenkorrelation;
  - nur bei Tag (Sonne > 3° hoch); nachts und bei Nebel bleibt der letzte gute Wert stehen;
  - übernommen wird ein Wert erst nach drei Messungen, die auf ±3 px übereinstimmen.

  Das Ergebnis steht in `/var/www/html/peakfinder-versatz.txt`; die Seite holt es ohne Cache und verschiebt alle Marken
  gemeinsam. Solange die Datei fehlt, gilt der Versatz bei der Erzeugung (28.10.: −19/+18 px).

## 6. Grenzen

- Ob ein Berg gerade **wirklich zu sehen** ist, wird nicht erkannt. Bei Dunst oder Nebel steht die Marke trotzdem dort,
  wo der Berg wäre; bei den fernen Alpen (Urner Alpen, Gross Windgällen) oft knapp über dem Grat im Himmel.
- Nachts stehen die Marken vor schwarzem Himmel.
- Das iPhone kennt kein Element-Vollbild, dort fehlt der Vollbild-Knopf.
- Ein Kamerawechsel oder grosses Verdrehen braucht eine neue Kalibrierung; der Versatz gleicht nur Verschiebungen aus,
  keine Drehung oder Zoomänderung.

## 7. Dateien

| Datei | Zweck |
|---|---|
| `../js/peakfinder.js`, `../css/peakfinder.css` | Ebene, Knopf, Karte, Vollbild auf der Webseite |
| `../js/peakfinder-daten.js` | erzeugt von `kalibrieren.py`: Gipfel mit Bildposition, Standardversatz |
| `kalibrieren.py` | Ausgleich und Gipfelauswahl, schreibt `gipfel.json`, `peakfinder-daten.js`, `referenz_band.png` |
| `modell.py` | Kameramodell (Drehung + Fischauge), Horizontlinie im Bild |
| `gelaende.py` | Höhenkacheln, OSM-Gipfel, Sichtrechnung |
| `abgleich.py` | Versatzmessung auf cAurora (auch von der Kalibrierung benutzt) |
| `passpunkte.json` | Mythen-Pixel, Sonnenpunkte, bevorzugte Namen, mit Herkunft |
| `gipfel.json` | Ergebnis: Kamera, Standort, Genauigkeit, alle 31 Gipfel |
| `referenz_20261025_090059.jpg`, `referenz_band.png` | Referenzbild und Horizontband für den Abgleich |
| `caurora-peakfinder.service`, `.timer` | systemd-Units für cAurora |

## 8. Neu kalibrieren

Nötig nach Kamerawechsel, neuer Montage oder wenn der Versatz dauerhaft über etwa 100 px liegt.

1. Neues Referenzbild bei klarer Sicht holen und die Mythen-Pixel in `passpunkte.json` neu ablesen (mit Herkunft).
2. `python3 kalibrieren.py --aktuell <aktuelles current.jpg>` mit einem Python, das numpy, scipy, pillow und opencv
   hat. Zwischenspeicher für Kacheln und OSM: `~/.cache/caurora-peakfinder`.
3. Gegenprobe: `python3 kalibrieren.py --ohne-mythen` (schreibt nichts) muss die Mythen auf wenige Pixel treffen.
4. `?v=` der drei Dateien in `index.php` erhöhen, dann wie im Runbook deployen (`referenz_band.png` nicht vergessen).

## Stand

- 2026-10-28 gebaut, lokal im Browser mit Standbildern geprüft (Klick, Knopf, Sprache, Zoom 2×/4×, 900 px, 390 px,
  Vollbild), am selben Abend live geschaltet. Öffentlich geprüft: Dateien per md5 gleich dem Repo, 31 Marken im
  ausgelieferten DOM, Messdienst aktiv (erster Lauf „Nacht“).
- Offen: Blick auf das echte Live-Video bei Tag, echtes iPhone, Koordinate bei windy und in der App.
