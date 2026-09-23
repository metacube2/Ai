# Bildeffekte — Effektliste

Stand: 2026-09-22

Rechenzeiten: Mittelwert bei 800×450 Pixeln auf diesem MacBook Air. Die
überarbeiteten VHS- und Film-Effekte wurden mit 16 Bildern gemessen (erste 3
verworfen), das Kontur-Labor mit 30 Bildern (erste 5 verworfen); die älteren
Werte mit bewegtem Testvideo. Budget sind 33 ms pro Bild (30 fps). Weitere
Messverfahren siehe `messwerte.md`.

## Bild-Effekte

| Effekt | Was passiert | Ø ms | Plugin |
| --- | --- | ---: | --- |
| Original | Bild unverändert (Rückfallebene, fest eingebaut) | 0 | – |
| Mosaik | 3×3-Raster, nur die aktive Kachel zeigt das Live-Bild | 4 | `10_video_basic` |
| Blur | Gaußsche Unschärfe | 0,3 | `10_video_basic` |
| Kantenerkennung | Canny-Kanten in Schwarzweiß | 0,5 | `10_video_basic` |
| Slow Motion Snapshot | Alle 0,2 s ein neues Standbild | 0,03 | `10_video_basic` |
| Realtime Glitch | Kanalversatz, Störlinien, Blockartefakte, Wellen, Ghosting | 3 | `20_video_glitch` |
| Datamosh | Bewegung schiebt alte Pixel in 16er-Blöcken weiter, seltene Keyframes | 4 | `20_video_glitch` |
| Pixel-Sorting | Helle Abschnitte werden nach Helligkeit sortiert, Richtung wechselt | 22 | `20_video_glitch` |
| VHS Störung | Begrenzte Bild- und Farbauflösung, Tracking-Fehler, Bandrauschen, Dropouts, Bildpumpen | 11,6 | `30_video_retro` |
| VHS Tracking-Riss | Weiches Bandbild, wandernde Synchronisationsbänder, Zeilenversatz und Rauschlinien | 10,4 | `31_video_vhs` |
| VHS Farbbluten | Stark begrenzte Farbauflösung, versetzte Farbkanäle und Farbnachziehen | 12,9 | `31_video_vhs` |
| VHS Kopierkassette | Mehrfach verlorene Details, blasse Farben, Geisterbild und Bandrauschen | 12,4 | `31_video_vhs` |
| VHS Pause | Weiches Standbild mit wanderndem Störbalken und Pause-Anzeige | 11,3 | `31_video_vhs` |
| Uralter Film (1910er) | Reduzierte Details, Sepia, Korn, Flackern, Vignette, Kratzer, Staub, Bildsprünge | 6,4 | `30_video_retro` |
| Glitch-Dimension | Fünf „Dimensionen": Korruption, digitaler Zerfall, Quantenunschärfe, Leere, hyperbolische Verzerrung | 9–30 | `40_video_dimension` |
| Matrix-Code | Bild aus grünen, herabregnenden Zeichen | 12 | `50_video_matrix` |
| Kaleidoskop-Tunnel | 4–12 Spiegelsegmente mit farbwechselnder Rückkopplung | 17 | `60_video_space` |
| Zeitwelle | Jede Bildstelle zeigt einen anderen Moment der letzten 1,5 s | 12 | `60_video_space` |
| Droste-Spirale | Bild enthält sich endlos selbst, rotierender Zoom | 8 | `60_video_space` |
| Sand-Zerfall | Bewegtes zerbröselt zu Sand, der zu Dünen fällt | 10 | `70_video_nature` |
| Bio-Wucherung | Reaktionsdiffusion wächst aus den Kanten: Würmer, Labyrinth, Zellen, Koralle | 23 | `70_video_nature` |
| Audio+Video Glitch | Kanalversatz im Bild plus Glitches auf dem Mikrofonton | 0,3 | `80_video_audio_reactive` |
| Klang-Beben | **Ton steuert Bild:** Bass-Beben, Farbspaltung, Scherben bei Einsätzen, Spektrum-Ring | 9 | `80_video_audio_reactive` |

## Kontur-Labor

Diese vier Bild-Effekte stehen in einer eigenen Sektion der Oberfläche. Die
Kanten werden nach lokalem Kontrastausgleich mit an das Bild angepassten
Schwellen gemessen. Das erkennt auch schwache Konturen, aber keine Körperteile
als solche. Beim Magnetfeld kann man eine Stelle im Vorschaubild mit der Maus
ziehen, etwa ein Auge oder den Mund.

| Effekt | Was passiert | Ø ms | Plugin |
| --- | --- | ---: | --- |
| Kontur-Magnetfeld | Kanten erzeugen ein Kraftfeld; Mausziehen verformt Bildbereiche lokal | 12,6 | `35_video_contour_lab` |
| Silhouetten-Wellen | Konturabstand steuert Farbringe und eine Bildbrechung | 15,7 | `35_video_contour_lab` |
| Lichtskulptur | Kanten und Konturabstand erzeugen ein beleuchtetes Relief | 18,2 | `35_video_contour_lab` |
| Kontur-Partikel | Funken entstehen an starken Kanten und folgen ihrer Richtung | 11,3 | `35_video_contour_lab` |

## Standbild-Transformation

| Effekt | Was passiert | Ø ms | Plugin |
| --- | --- | ---: | --- |
| Flüssigmetall-Schmelze | Personenmaske und Gesichtsrelief; bewegte Umgebungsreflexionen, Glanzlichter und silbrige Pfütze | 27,4 bei 1280×720, einmalige Analyse separat | `36_video_liquid_metal` |

## Matrix-Agent – Gesichtsmorph

Neuer Live-Effekt aus `51_video_matrix_agent.py`: Wangen und Kiefer verformen sich,
Mund und Nase verschieben sich; das Gesicht geht in einen Agenten mit Sonnenbrille
über und anschließend zurück. Lokale Gesichtserkennung, Bewegungsverfolgung und
Hautton-Angleichung. Ersetzt wird nur die innere Gesichtsfläche mit weichem Übergang;
Kiefer, Ohren und Haaransatz bleiben echt (überarbeitet 2026-09-23). Optional kann ein eigenes JPG/JPEG als Zielgesicht gewählt
werden; ohne Auswahl gilt die mitgelieferte Agentenvorlage. Der finale Messlauf
auf einem 512×512-Porträt ergab **17,1 ms Median und 27,9 ms p95** für das Rendern
während des Morphs, ohne Kamera und asynchrone Gesichtserkennung. Details und
Grenzen: [matrix-agent.md](matrix-agent.md).

## Verwandlungen

Personenerkennung, gemerkter Hintergrund, Kreislauf vorwärts/rückwärts; Details in
`verwandlungen.md`. Werte bei 1280×720.

| Effekt | Was passiert | Ø ms | Plugin |
| --- | --- | ---: | --- |
| Flüssigmetall-Schmelze II | Chrom mit gelernter Tiefe, FLIP-Strömung in eine Bodenpfütze | 26–34 | `37_video_liquid_metal_v2` |
| Flüssiggold | Poliertes Gold, schmilzt zur goldenen Pfütze | 24–35 | `38_video_verwandlung_material` |
| Flüssigglas | Klares Glas mit Brechung und Dispersion | 18–29 | `38_video_verwandlung_material` |
| Lava-Schmelze | Kruste mit glühenden Rissen, zäh, erstarrt in der Pfütze | 21–31 | `38_video_verwandlung_material` |
| Honig-Schmelze | Bernsteinfarben, durchscheinend, sehr zäh | 20–33 | `38_video_verwandlung_material` |
| Aus der Pfütze | Person formt sich aus einer Metallpfütze | 20,7 | `39_video_verwandlung_spezial` |
| Zerfall zu Asche | Zerfällt zu Asche, setzt sich rückwärts zusammen | 21,9 | `39_video_verwandlung_spezial` |
| Eis & Zerspringen | Klares Eis mit Raureif und Brechung, Risse, zerspringt, fliegt zurück | 16,1¹ | `39_video_verwandlung_spezial` |
| Formwandler | Metall fließt zwischen zwei Posen | 18,4 | `39_video_verwandlung_spezial` |
| Predator-Tarnung | Live: durchsichtig mit flimmernder Brechung | 32,6 | `39_video_verwandlung_spezial` |

¹ Wert vor der Eis-Überarbeitung vom 2026-09-23. Neue Messung bei internem 320×400:
Eis 7–9 ms, Scherbenflug 24–32 ms (siehe `verwandlungen.md`).

## Ton-Effekte

Rechenzeit je 512-Sample-Block bei 48 kHz, Budget 10,7 ms. „Latenz" ist die
Verzögerung, um die das Programm den Ton verzögert.

| Effekt | Was passiert | Latenz | Ø ms | Plugin |
| --- | --- | ---: | ---: | --- |
| Bild-Schablone | Jede Bildzeile filtert ein Frequenzband: hell lässt durch, dunkel schluckt | 43 ms | 0,13 | `90_audio_spectral` |
| Audio-Datamosh | Standbild friert den Klang ein, Bewegung zerreißt ihn, Schnitt setzt zurück | 43 ms | 0,15 | `90_audio_spectral` |
| Farb-Vokale | Die Bildfarbe bestimmt den Vokal (rot „a", grün „i", blau „o") | 43 ms | 0,18 | `90_audio_spectral` |
| Negativ-Ton | Das Lauteste wird entfernt, das Leise verstärkt | 43 ms | 0,20 | `90_audio_spectral` |
| Harmonie-Magnet | Alle Frequenzen werden auf Akkordtöne gezogen, Farbe wählt den Akkord | 85 ms | 0,32 | `90_audio_spectral` |
| Schwarzes Loch | Frequenzen stürzen in ein wanderndes Loch und werden verschluckt | 43 ms | 0,24 | `90_audio_spectral` |
| Traum-Gedächtnis | Ton wird aus Schnipseln der letzten 30 s neu zusammengesetzt | 43 ms | 0,17 | `90_audio_spectral` |
| Vorahnung | Jedes Wort schwillt vorher rückwärts an | 1500 ms | 0,07 | `91_audio_time` |
| Takt-Zwang | Einsätze rasten auf Achtelnoten ein, Tempo aus den Bildschnitten | 60 ms | 0,08 | `91_audio_time` |
| Glas-Kristallisation | Jede Silbe wird zum gläsernen Arpeggio ihrer Obertöne | 0 ms | 1,43 | `91_audio_time` |
| Antimaterie | Lautstärkekurve umgedreht: anschwellen, abrupt abbrechen | 600 ms | 0,15 | `91_audio_time` |
| Klang-Schwarm | Klangkörner fliegen als Vogelschwarm; laut bündeln sie sich, leise zerstreuen sie sich | 0 ms | 1,31 | `91_audio_time` |
| Phosphor-Nachleuchten | Laute Anteile glühen nach und sinken beim Abkühlen | 43 ms | 0,13 | `92_audio_phosphor` |
| Frequenz-Origami | Spektrum an wandernder Falz spiegeln | 43 ms | 0,11 | `93_audio_experimental` |
| Spektral-Regen | Obertöne tropfen über die Zeit in tiefere Frequenzen | 43 ms | 0,11 | `93_audio_experimental` |
| Phasen-Wirbel | Lokale Phasen drehen sich zu flüssigen Klangwolken | 43 ms | 0,12 | `93_audio_experimental` |
| Puls-Mosaik | Frequenzkacheln öffnen und schließen sich in mehreren Takten | 43 ms | 0,11 | `93_audio_experimental` |
| Harmonische Fata Morgana | Frequenzen erscheinen als verschobene Spiegelungen | 43 ms | 0,12 | `93_audio_experimental` |
| Zeit-Iris | Frequenzabhängiger Rückblick, gesteuert von Bildbewegung | 43 ms | 0,16 | `93_audio_experimental` |

## Bildmerkmale, die Ton-Effekte nutzen

Der Aufnahme-Thread berechnet sie in 1,6–1,8 ms pro Bild, unabhängig von der Bildgröße,
und nur solange ein Ton-Effekt aktiv ist:

`row_profile` (Helligkeit je Bildzeile), `brightness`, `hue`, `saturation`,
`motion_x`, `motion_y`, `activity`, `cut_count`, `cut_times`.

Läuft kein Video, arbeitet jeder Ton-Effekt mit sinnvollem Ersatzverhalten, zum
Beispiel wandernden Vokalen oder der Akkordfolge C–Am–F–G.
