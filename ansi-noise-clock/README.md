---
title: ANSI Noise Clock
topic: apps.ansi-noise-clock
tags: [terminal, ansi, uhr, animation, python]
status: current
updated: 2026-09-04
---

# ANSI Noise Clock

Terminal-Uhr, die `HH:MM:SS` als *geordnetes Rauschen* zeichnet: das ganze
Raster flimmert, aber innerhalb der Ziffern ist das Rauschen dicht und hell,
ausserhalb duenn und dunkel. Die Zeit entsteht also als Dichteunterschied im
Rauschen, nicht als gemalter Block.

Bei jedem Sekundentakt zoomt die Sekundengruppe voll auf, wandert dabei in die
Bildmitte und faellt weich in ihre Normalposition zurueck. `HH:MM` bleibt
stehen und wird beim Zoom kurz ueberlagert.

## Start

Eigenes neues Fenster (das ist der Normalfall):

```bash
./clock-window.sh
./clock-window.sh --palette amber --zoom 3.2
CLOCK_ROWS=45 CLOCK_COLS=160 ./clock-window.sh
```

Im aktuellen Terminal (praktisch zum Entwickeln):

```bash
python3 noise_clock.py
python3 noise_clock.py --no-alt-screen --seconds 10
```

Beenden mit `Strg-C`. Der Alternativschirm wird immer zurueckgegeben, auch bei
`SIGTERM`.

## Optionen

| Option | Standard | Wirkung |
|---|---|---|
| `--fps` | `30` | Bildrate, 1-120 |
| `--zoom` | `2.6` | maximaler Sekundenzoom relativ zur Normalgroesse |
| `--decay` | `0.55` | Anteil der Sekunde fuer den Rueckweg in die Normalposition |
| `--churn` | `0.30` | Anteil der Zellen, die pro Bild neu rauschen |
| `--density` | `0.34` | Grunddichte des Hintergrundrauschens |
| `--palette` | `green` | `green`, `amber`, `ice`, `mono`, `plasma` |
| `--format` | `%H:%M:%S` | strftime-Format; die letzten zwei Stellen sind die Zoomgruppe |
| `--seconds` | `0` | nach n Sekunden beenden, `0` = endlos |
| `--no-alt-screen` | aus | im aktuellen Puffer zeichnen statt im Alternativschirm |

Umgebungsvariablen fuer den Launcher: `CLOCK_ROWS`, `CLOCK_COLS`,
`CLOCK_TERM_APP` (`Terminal` oder `iTerm`), `PYTHON`.

## Aufbau

```text
ansi-noise-clock/
├── noise_clock.py     # gesamte Uhr, Python 3 ohne Fremdpakete
├── clock-window.sh    # oeffnet ein neues Terminal-Fenster per osascript
└── README.md
```

Ablauf pro Bild:

1. `Layout` bestimmt aus der Terminalgroesse die groesste Basisskalierung, bei
   der `HH:MM:SS` mit Rand passt.
2. `pulse()` leitet die Animationsphase aus der Wanduhr ab
   (`time.time() % 1.0`, danach `smoothstep`), nie aus einem Bildzaehler.
   Ein Zaehler wuerde binnen einer Minute vom Sekundentakt weglaufen.
3. `build_cells()` schreibt die Ziffernmasken in ein Zellenraster:
   `HH:MM:` fest, `SS` mit eigener Skalierung und eigenem Zentrum.
   Die Masken werden verhaeltnisbasiert abgetastet
   (`src_y = y * 7 // dst_h`), nicht ganzzahlig wiederholt, sonst ruckelt der
   Zoom sichtbar.
4. `frame()` legt das Rauschen darueber und baut genau einen String, der mit
   einem `write` rausgeht.

## Terminal-Details, die den Unterschied machen

- Farbe wird nur bei Wechsel als SGR-Sequenz ausgegeben, und die Farbstufen
  werden pro Zeile vorberechnet. Per-Zelle-Farbe waere bei 200x55 rund
  10x so viel Ausgabe und wuerde die Animation sichtbar stottern lassen.
- 256-Farben (`38;5;N`) statt Truecolor, damit es in Terminal.app genauso
  aussieht wie in iTerm2 oder tmux.
- Blockzeichen `█▓▒░` gehoeren exklusiv den Ziffern, das Hintergrundrauschen
  nutzt nur `·.:'`,˚`. Sonst faellt der Kontrast zusammen und die Zeit ist im
  Rauschen nicht mehr lesbar.
- Jede Zeile wird per `CSI y;1H` adressiert statt per Zeilenumbruch, und die
  letzte Zelle der letzten Zeile bleibt frei. Beides verhindert Autoscroll.
- Kein `clear` pro Bild, nur ueberschreiben, sonst flackert es.
- `shutil.get_terminal_size()` wird jedes Bild geprueft; bei Groessenaenderung
  werden Layout und Rauschfeld neu aufgebaut.
- Der Zoomfaktor wird an der Terminalhoehe gekappt.

Messwerte auf diesem Mac: 200x55 Zellen kosten rund 1,7 ms pro Bild
(entspraeche 585 fps) bei 21 KB Ausgabe pro Bild.

## Stand

Fertig und lauffaehig. Offene Ideen: Rauschfeld mit zeitlicher Kohaerenz statt
unabhaengigem Neuwuerfeln, optionaler Datumsstreifen, Fokus-/Unschaerfe-Effekt
am Zoom-Scheitel.
