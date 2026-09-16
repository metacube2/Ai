# Bildeffekte — Effektliste

Stand: 2026-09-16

Rechenzeiten: Mittelwert bei 800×450 Pixeln auf diesem MacBook Air, gemessen mit
bewegtem Testvideo. Budget sind 33 ms pro Bild (30 fps). Messverfahren siehe
`messwerte.md`.

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
| VHS Störung | Tracking-Fehler, Scanlines, Farbbluten, Dropouts, Bildpumpen | 5 | `30_video_retro` |
| Uralter Film (1910er) | Sepia, Korn, Flackern, Vignette, Kratzer, Staub, Bildsprünge | 12 | `30_video_retro` |
| Glitch-Dimension | Fünf „Dimensionen": Korruption, digitaler Zerfall, Quantenunschärfe, Leere, hyperbolische Verzerrung | 9–30 | `40_video_dimension` |
| Matrix-Code | Bild aus grünen, herabregnenden Zeichen | 12 | `50_video_matrix` |
| Kaleidoskop-Tunnel | 4–12 Spiegelsegmente mit farbwechselnder Rückkopplung | 17 | `60_video_space` |
| Zeitwelle | Jede Bildstelle zeigt einen anderen Moment der letzten 1,5 s | 12 | `60_video_space` |
| Droste-Spirale | Bild enthält sich endlos selbst, rotierender Zoom | 8 | `60_video_space` |
| Sand-Zerfall | Bewegtes zerbröselt zu Sand, der zu Dünen fällt | 10 | `70_video_nature` |
| Bio-Wucherung | Reaktionsdiffusion wächst aus den Kanten: Würmer, Labyrinth, Zellen, Koralle | 23 | `70_video_nature` |
| Audio+Video Glitch | Kanalversatz im Bild plus Glitches auf dem Mikrofonton | 0,3 | `80_video_audio_reactive` |
| Klang-Beben | **Ton steuert Bild:** Bass-Beben, Farbspaltung, Scherben bei Einsätzen, Spektrum-Ring | 9 | `80_video_audio_reactive` |

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

## Bildmerkmale, die Ton-Effekte nutzen

Der Aufnahme-Thread berechnet sie in 1,6–1,8 ms pro Bild, unabhängig von der Bildgröße,
und nur solange ein Ton-Effekt aktiv ist:

`row_profile` (Helligkeit je Bildzeile), `brightness`, `hue`, `saturation`,
`motion_x`, `motion_y`, `activity`, `cut_count`, `cut_times`.

Läuft kein Video, arbeitet jeder Ton-Effekt mit sinnvollem Ersatzverhalten, zum
Beispiel wandernden Vokalen oder der Akkordfolge C–Am–F–G.
