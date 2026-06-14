# FT-991A Remote Control App für macOS

Eine native macOS-Anwendung zur Fernsteuerung des Yaesu FT-991A Amateurfunk-Transceivers über USB (CAT-Protokoll).

## Features

### Verbindung
- USB virtueller COM-Port (Silicon Labs CP210x)
- Auto-Reconnect bei Verbindungsabbruch
- Optionaler Auto-Connect beim Start
- Unterstützte Baudraten: 4800, 9600, 19200, 38400 (Standard), 57600, 115200
- Bevorzugter FT-991A CAT-Pfad unter macOS wird gespeichert und beim Start wiederhergestellt

### Benutzeroberfläche
- **Modern View**: Modernes, abstraktes UI-Design
- **Skeuomorph View**: Originalgetreue Nachbildung des FT-991A Frontpanels
- Abdockbare Panels (Log, Debug, Audio, Metering)
- Menüleisten-Betrieb für Hintergrundbetrieb
- Lokalisierung: Deutsch & Englisch
- Setup-Assistent im Hauptfenster für Erstinstallation und Support

### Steuerung
- VFO A/B Frequenzsteuerung
- Betriebsarten: LSB, USB, CW, FM, AM, RTTY, DATA, C4FM
- Pegel: AF Gain, RF Gain, Squelch, MIC Gain, Power
- Funktionen: NB, NR, Auto-Notch, Contour, ATU, Split (TX VFO A/B), IPO
- S-Meter, Power-Meter, SWR-Meter Anzeige
- PTT-Steuerung (Shift-Taste)

### Logging
- QSO-Log im CSV-Format
- Felder: Call, Datum, Zeit, Frequenz, Mode, RST TX/RX, Name, QTH, Locator, Power, Notizen
- Wählbarer Speicherort (Standard: ~/Documents/FT991A-Logs/)
- Automatisches Speichern
- Separates CAT-Trace-Log für Diagnose
- Export eines lokalen Support-Bundles mit Settings, App-Log und CAT-Trace

### Audio
- BlackHole Integration für digitale Betriebsarten
- Audio-Routing für WSJT-X, fldigi, etc.

### Tastaturkürzel

Hinweis: Die folgenden Tastaturkürzel sind App-Funktionen. Sie stammen nicht aus der Yaesu-CAT-Spezifikation.

| Taste | Funktion |
|-------|----------|
| ⌘K | Verbinden/Trennen |
| Shift (halten) | PTT |
| ↑ | ATU Tune |
| ← / → | Frequenz -/+ |
| + / - | Frequenz -/+ |
| ⇧⌘S | VFO A/B tauschen |
| ⇧⌘E | A=B |
| ⌥⌘D | Debug-Panel |
| ⌥⌘L | Log-Panel |

## Systemanforderungen

- macOS 15.0 (Sequoia) oder neuer
- Yaesu FT-991A mit USB-Kabel
- Silicon Labs CP210x Treiber (normalerweise automatisch installiert)

## FT-991A Einstellungen

Für native macOS-Nutzung sollte das Funkgerät und die Portwahl zu diesem Profil passen:

```
CP2105 Enhanced Port wählen
Menu → CAT RATE: 38400 bps
Menu → CAT TOT: 100 ms
Menu → CAT RTS: ON
DTR bleibt aus
```

Hinweis: Unter Windows oder in virtuellen Maschinen kann der funktionierende Port-/Handshake-Pfad abweichen. Für diese macOS-App ist der native Enhanced-Port maßgeblich.

## Installation

1. Projekt in Xcode öffnen
2. Build & Run (⌘R)

Oder für Release-Build:
1. Product → Archive
2. Distribute App → Copy App

## Vertriebsseite

Eine eigenständige Landingpage für Marketing/Vertrieb liegt unter:

- `marketing-site/index.html`

Die Seite ist auf FT-991A-Funkamateure ausgerichtet und enthält bereits eine klare Empfehlung zum Vertrieb per `Developer ID + notarisiertem DMG` statt Mac App Store.

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
| AB; | VFO-A nach VFO-B kopieren (A=B) |
| AC001; | ATU einschalten |
| AC002; | ATU-Abstimmung starten/stoppen |
| BC01; / BC00; | Auto-Notch ein / aus |
| FT3; / FT2; | TX ueber VFO-B / VFO-A |
| TX1; | CAT-TX ein |
| TX0; | CAT-TX aus |
| SM0; | S-Meter lesen |

Geprueft gegen das offizielle Yaesu FT-991A CAT Operation Reference Manual:
https://www.yaesu.com/Files/4CB893D7-1018-01AF-FA97E9E9AD48B50C/FT-991A_CAT_OM_ENG_1711-D.pdf

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
