# Bildeffekte — Messwerte und Prüfstand

Stand: 2026-09-16

Alle Zahlen stammen von diesem MacBook Air (M-Chip, macOS 25.6) und wurden mit
Python 3.11 aus `../../../venv` gemessen. Die Prüfskripte lagen in einem
Arbeitsverzeichnis außerhalb des Repositories und sind **nicht eingecheckt**; die
Verfahren stehen unten, damit die Messung wiederholbar ist.

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
