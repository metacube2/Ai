# Zwölfton-Synthesizer (Dodekaphonie)

Ein interaktiver Web-Synthesizer basierend auf Arnold Schönbergs Zwölftontechnik.

## Features

- **Automatische Zwölfton-Komposition**: Generiert fortlaufend Musik nach den Regeln der Dodekaphonie
- **Vier Reihenformen**: Original, Krebs (Retrograde), Umkehrung (Inversion), Krebsumkehrung
- **Reverb-Effekt**: Einstellbarer Hall mit Convolver-basierter Impulsantwort
- **Web Audio API**: Echtzeit-Klangsynthese im Browser
- **Audio-Visualisierung**: Wellenform und Frequenzspektrum in Echtzeit
- **Zwölftonmatrix**: Vollständige 12x12-Matrix aller möglichen Transpositionen

## Die Zwölftontechnik

Die Dodekaphonie wurde von Arnold Schönberg um 1921 entwickelt:

1. Alle 12 chromatischen Töne werden gleichberechtigt verwendet
2. Kein Ton darf wiederholt werden, bevor alle anderen gespielt wurden
3. Die Grundreihe erscheint in vier Formen:
   - **Original (O)**: Die Grundreihe
   - **Krebs (R)**: Rückwärts gespielt
   - **Umkehrung (I)**: Intervalle gespiegelt
   - **Krebsumkehrung (RI)**: Kombination aus Krebs und Umkehrung
4. Jede Form kann auf alle 12 Stufen transponiert werden (48 mögliche Reihen)

## Installation

1. PHP-Server starten (PHP 7.4+ erforderlich):
```bash
cd twelve-tone-synthesizer
php -S localhost:8000
```

2. Browser öffnen: `http://localhost:8000`

## Bedienung

- **Starten**: Startet die automatische Wiedergabe der Zwölftonreihe
- **Stoppen**: Beendet die Wiedergabe
- **Neue Reihe**: Generiert eine zufällige neue Zwölftonreihe

### Klangparameter

- **Tempo (BPM)**: Geschwindigkeit der Notenwiedergabe (40-300)
- **Oktave**: Tonhöhenbereich (2-6)
- **Reverb**: Hallanteil (0-100%)
- **Attack**: Einschwingzeit der Töne
- **Release**: Ausklingzeit der Töne
- **Wellenform**: Sinus, Dreieck, Rechteck, Sägezahn

## Technologie

- **PHP**: Backend für Zwölftonreihen-Generierung und Matrix-Berechnung
- **JavaScript**: Web Audio API für Klangsynthese
- **ConvolverNode**: Realistische Reverb-Simulation
- **Canvas API**: Audio-Visualisierung

## Lizenz

MIT License
