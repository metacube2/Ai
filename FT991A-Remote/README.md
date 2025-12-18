# FT-991A Remote Control App für macOS

Eine native macOS-Anwendung zur Fernsteuerung des Yaesu FT-991A Amateurfunk-Transceivers über USB (CAT-Protokoll).

## Features

### Verbindung
- USB virtueller COM-Port (Silicon Labs CP210x)
- Auto-Reconnect bei Verbindungsabbruch
- Unterstützte Baudraten: 4800, 9600, 19200, 38400 (Standard), 57600, 115200

### Benutzeroberfläche
- **Modern View**: Modernes, abstraktes UI-Design
- **Skeuomorph View**: Originalgetreue Nachbildung des FT-991A Frontpanels
- Abdockbare Panels (Log, Debug, Audio, Metering)
- Menüleisten-Betrieb für Hintergrundbetrieb
- Lokalisierung: Deutsch & Englisch

### Steuerung
- VFO A/B Frequenzsteuerung
- Betriebsarten: LSB, USB, CW, FM, AM, RTTY, DATA, C4FM
- Pegel: AF Gain, RF Gain, Squelch, MIC Gain, Power
- Funktionen: NB, NR, DNF, Contour, ATU, Split, IPO
- S-Meter, Power-Meter, SWR-Meter Anzeige
- PTT-Steuerung (Shift-Taste)

### Logging
- QSO-Log im CSV-Format
- Felder: Call, Datum, Zeit, Frequenz, Mode, RST TX/RX, Name, QTH, Locator, Power, Notizen
- Wählbarer Speicherort (Standard: ~/Documents/FT991A-Logs/)
- Automatisches Speichern

### Audio
- BlackHole Integration für digitale Betriebsarten
- Audio-Routing für WSJT-X, fldigi, etc.

### Tastaturkürzel
| Taste | Funktion |
|-------|----------|
| ⌘K | Verbinden/Trennen |
| Shift (halten) | PTT |
| ↑ | ATU Tune |
| ← / → | Frequenz -/+ |
| ⇧⌘S | VFO A/B tauschen |
| ⇧⌘E | A=B |
| ⌥⌘D | Debug-Panel |
| ⌥⌘L | Log-Panel |

## Systemanforderungen

- macOS 15.0 (Sequoia) oder neuer
- Yaesu FT-991A mit USB-Kabel
- Silicon Labs CP210x Treiber (normalerweise automatisch installiert)

## FT-991A Einstellungen

Stelle sicher, dass im Radio-Menü folgende Einstellungen aktiv sind:

```
Menu → CAT RATE: 38400 bps
Menu → CAT TOT: 100 ms
Menu → CAT RTS: OFF
```

## Installation

1. Projekt in Xcode öffnen
2. Build & Run (⌘R)

Oder für Release-Build:
1. Product → Archive
2. Distribute App → Copy App

## Projektstruktur

```
FT991A-Remote/
├── FT991A_RemoteApp.swift          # App Entry Point
├── Models/
│   ├── RadioState.swift            # Gerätezustand
│   ├── CATCommand.swift            # CAT-Befehle
│   ├── QSOEntry.swift              # Log-Einträge
│   └── Settings.swift              # Einstellungen
├── Services/
│   ├── SerialPortManager.swift     # USB Serial
│   ├── CATProtocol.swift           # CAT Parser
│   ├── CSVManager.swift            # Log-Dateien
│   └── AudioRouter.swift           # BlackHole
├── ViewModels/
│   ├── RadioViewModel.swift        # Radio-Logik
│   ├── LogViewModel.swift          # Log-Logik
│   └── SettingsController.swift    # Einstellungen
├── Views/
│   ├── MainView.swift              # Hauptfenster
│   ├── ModernView/                 # Moderne UI
│   ├── SkeuomorphView/             # Frontpanel
│   ├── Panels/                     # Abdockbare Panels
│   ├── Settings/                   # Einstellungen
│   └── MenuBar/                    # Menüleiste
└── Utilities/
    ├── Logger.swift                # Logging
    └── Localization/               # DE/EN
```

## CAT-Befehle

Die App verwendet das Yaesu CAT-Protokoll. Wichtige Befehle:

| Befehl | Funktion |
|--------|----------|
| FA; | VFO-A Frequenz lesen |
| FA014250000; | VFO-A auf 14.250 MHz setzen |
| MD02; | Mode auf USB setzen |
| TX0; | PTT ein (MIC) |
| RX; | PTT aus |
| SM0; | S-Meter lesen |

## Entwicklung

### Phase 1 (aktuell)
- ✅ Projekt-Setup
- ✅ SerialPortManager
- ✅ CAT-Protokoll Parser
- ✅ RadioState Model
- ✅ Debug-UI
- ✅ Logging-System

### Phase 2-6 (geplant)
- Vollständiger CAT-Befehlssatz
- Erweiterte UI (Skeuomorph-Ansicht)
- QSO-Logging & CSV
- BlackHole Audio-Routing
- Tastaturkürzel
- Testing & Polish

## Lizenz

MIT License

## Autor

Entwickelt für Amateurfunk-Enthusiasten.

73!
