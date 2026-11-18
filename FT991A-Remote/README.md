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

## Weitergabe (notarisiert)

Seit 2026-10-19 mit Developer ID (Team 3HF7RG7BPA) signiert und von Apple notarisiert.
`Tools/verteilen.sh` baut Release, signiert mit Hardened Runtime und Entitlements, packt das DMG, notarisiert
und heftet das Ticket an (`dist/MacYaesu.dmg`). Für den Website-Deploy `dist/MacYaesu.dmg` nach `MacYaesu.dmg`
kopieren (siehe `instructions/homelab/aiscom-website.md`). Live auf www.aiscom.ch/macyaesu/: **1.1 (4)** seit 2026-11-08 (notarisiert, Ed25519-Lizenz, Demo-Modus, Leistungsanzeige
`RM1` behoben; auch im Sparkle-Kanal `macyaesu`, Käufer von 1.1 (3) bekommen das Update automatisch). Vorher 1.1 (3) ab
2026-11-06, 1.1 (2) ab 2026-10-19. Offen: Testkauf, MWST/Merchant of Record, Forenposts, übrige Fehler aus der
Windows-Portierung (Speicherlisten, CSV-Zeiten, Shift-PTT beim Tippen, siehe `instructions/desktop-ports.md`).

**Demo-Modus** (seit 2026-11-08, verteilt ab 1.1 (4)): In der Portliste steht als letzter Eintrag
„Demo-Funkgerät (ohne FT-991A)“ (`demo://ft-991a`). Damit verbindet MacYaesu ohne Gerät mit einem simulierten FT-991A
(`DemoFunkgeraet` in `Services/SerialPortManager.swift`): gleicher Handshake (`ID;`/`FA;`), Antworten im FT-991A-Format
durch denselben Parser, gesetzte Werte bleiben gespeichert, S-Meter als ruhiges Zufallssignal, Unbekanntes mit `?;`.
Das Demo-Gerät wird nie automatisch gewählt und nie von der Portsuche probiert. Testzeit (15 min) und
Lizenzfreischaltung gelten unverändert. `MACYAESU_DEMO=1` (z. B. `open -n --env MACYAESU_DEMO=1 MacYaesu.app`) verbindet
beim Start direkt mit dem Demo-Gerät und merkt sich den Port nicht (für Vorführung und Bildschirmfotos).
Geprüft 2026-11-08 (Release-Bau, ad hoc signiert): „Verbunden · CAT alive“, CAT-Trace nur `RX class=valid`, Frontpanel mit
14.195.000 Hz USB und S-Meter. `RM1` (Leistung beim Senden) liest seit 1.1 (4) die Instrumentziffer nicht mehr mit.

**Tester-Fassung** (seit 2026-10-24): `Tools/verteilen.sh --tester` baut mit Compile-Schalter `MACYAESU_TESTER`
(`SettingsController.istTester`: `isActivated` immer wahr, Einstellungen zeigen „Vollversion (Testfassung)“) nach
`dist/MacYaesu-Tester.dmg` für www.aiscom.ch/tester/ (eigener Build-Ordner `build/verteilen-tester`, Verkaufs-DMG bleibt
unberührt; Rauchtest per `strings`). `--ohne-notar` baut und signiert nur. Nie verkaufen.

**Lizenzschlüssel** (seit 2026-11-06): Ed25519-Signatur statt Hash mit eingebautem Geheimnis; die App enthält nur
den öffentlichen Schlüssel (`LizenzPruefung` in `SettingsController.swift`). Ausstellen mit
`swift ~/Projects/Git/aiscom-lizenz/lizenz.swift ausstellen macyaesu <e-mail>`. Alte Schlüssel gelten nicht mehr.

## Korrekturen aus der Windows-Fassung (1.1 (5), 2026-11-18)

Drei Fehler, die beim Tauri-Port (`/Volumes/ExtOsX/Entwicklung/desktop/ft991a-remote-win`) gefunden wurden, waren
auch hier vorhanden und sind nachgezogen. Verteilt als Tester-Fassung 1.1 (5); Website und Sparkle-Kanal `macyaesu`
noch auf 1.1 (4).

**Kein Rücksprung nach dem Setzen.** Eine Abfrage (z. B. `FA;` aus dem Abfragezyklus), die vor einem Setzbefehl
desselben Schlüssels gesendet wurde, überschrieb mit ihrer späteren Antwort den gesetzten Wert: 14,2 MHz gesetzt, die
alte Antwort `FA007100000` kam danach, der nächste Mausrad-Schritt ging von 7,1 MHz aus (mit dem alten Stand im Test
nachgestellt: `FA007101000` statt `FA014201000`). Die 0,8-s-Pause hielt nur neue Abfragen zurück. Jetzt vermerkt
`CATAbfrageBuch` (`Models/CATCommand.swift`) jede gesendete Abfrage je Schlüssel (nach dem Befehlstext, auch
Rohbefehle der Konsole; zwei Zeichen, bei `CO`/`CN` vier). Ein Setzbefehl markiert alle noch ausstehenden Abfragen
seines Schlüssels als veraltet (`SV` trifft FA und FB, `AB` trifft FB). CAT antwortet der Reihe nach, also gehört jede
Antwort zur ältesten ausstehenden Abfrage; ist sie veraltet, landet die Antwort nur in der Konsole (Trace
`RX class=stale`). Die erste Abfrage nach dem Setzen gilt wieder, so kommt ein vom Gerät abgelehnter Wert zurück.
Unbeantwortete Abfragen verfallen nach 10 s ab dem Senden. Verbinden/Trennen leert das Buch, beim Verbinden wird das
`FA;` des Handshakes vermerkt (es geht an `CATProtocol.send` vorbei). Die Zuordnung geschieht auf dem Hauptthread im
selben Block, der den Zustand schreibt; die Startreihe und das ATU-Abstimmen senden deshalb ebenfalls vom
Hauptthread (vorher eigene Warteschlange `cat.command`), damit Buch und Leitung dieselbe Reihenfolge haben.

**PTT überholt den Rückstau.** Bisher schrieb jede `send`-Anfrage über eine serielle GCD-Warteschlange mit 110 ms
Abstand; ein `TX1`/`TX0` wartete hinter allem, was davor eingereiht war. Jetzt hält `SerialPortManager` eine
`CATAusgangsschlange`: `TX0`/`TX1`/`TX2` gehen an den Anfang (hinter bereits wartende Senderbefehle, deren
Reihenfolge bleibt; die Windows-Fassung stellt jeden ganz nach vorne, dort könnte ein schnelles TX0→TX1 vertauscht
werden), `TX0` verwirft wartende `TX1`/`TX2`. Die Antwort-Zuordnung bleibt gültig, weil Setzbefehle nicht beantwortet
werden. **Trennen beim Senden:** Bisher schloss Trennen den Port ohne `TX0`, das Gerät blieb auf Senden. Jetzt reiht
`RadioViewModel.disconnect` bei laufendem Senden `TX0` ein, und der Port schreibt es vor dem Schliessen sofort (ohne
Abstand), der übrige Rückstau wird verworfen. Gilt auch für das Trennen nach Ablauf der Testzeit. Der Demo-Modus
umgeht die Schlange (antwortet sofort), dort ist der Vorrang nicht sichtbar.

**70 cm FM Standard: −7,6 MHz** (IARU Region 1, Schweiz/Deutschland/Österreich; 438,650 → 431,050) statt +7,6 MHz.
Die Vorgaben der Relaiskarte stehen in `RepeaterPreset` (`Models/RadioState.swift`).

**Kerntests** (neu, es gab vorher keine): `Tools/tests.sh` übersetzt `CATCommand`, `RadioState`, `CATProtocol` und
`SerialPortManager` mit einem Logger-Ersatz per `swiftc` und führt `Tests/Kern/main.swift` aus (kein Xcode-Testziel,
kein Zugriff auf Ports, Einstellungen oder Netz). 19 Tests: 13 zum Rücksprung (sinngemäss die 11 der Windows-Fassung,
dazu Rohbefehl/unverlangte Antwort, PTT-Status, Schlüssel), 4 zur Ausgangsschlange und zum Trennen, 1 zu 70 cm, 1 zum
Abfragebuch direkt. Stichprobe „wird rot“: ohne Verwerfen 11, ohne Rücksetzen 1, ohne Frist 1, Frist 1 s 1, ohne
Handshake-FA 2, ohne SV/AB 2, ohne TX-Vorrang 3, TX0 verwirft nicht 1, TX ganz nach vorne 1, Trennen ohne sofortiges
TX0 1 bzw. ohne TX0 im Protokoll 1, 70 cm mit Plus 1. Nicht abgedeckt: das Schreiben auf einen echten Port und die
Fadenwechsel (Lese-Warteschlange → Hauptthread); offen ist der Test am echten FT-991A.

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

## Automatische Updates (Sparkle, seit 2026-10-24)

`verteilen.sh` bettet Sparkle 2.10.0 ein und veröffentlicht die Updateliste unter
`https://www.aiscom.ch/updates/<kanal>/appcast.xml` (Bausteine und Schlüssel: [`sparkle-aiscom`](../../sparkle-aiscom/README.md)).
Menü „Nach Updates suchen …“. Kanal `macyaesu-tester` (Tester), `macyaesu` (Verkauf, nur `--update-verkauf`). Der Swift-Baustein steht am Ende von `FT991A_RemoteApp.swift` (klassisches Xcode-Projekt).
