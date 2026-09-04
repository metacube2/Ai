# MacYaesu Development Notes

Projektordner: `/Users/metacube/Desktop/git/Ai/FT991A-Remote`

Aktueller sichtbarer App-Name: `MacYaesu`  
Aktuelle Projektdatei: `MacYesu.xcodeproj`  
Wichtig: Projektdatei/Target/Scheme heißen aktuell noch `MacYesu` mit Tippfehler, der Source-Ordner und der sichtbare Produktname heißen aber `MacYaesu`.

## Ziel

Native macOS-Steuerung eines Yaesu FT-991A über USB-CAT mit moderner GUI, Speicherverwaltung, Scan-Funktionen, Repeater-/Tone-Unterstützung und robuster Diagnose.

## Wichtigste CAT-Erkenntnisse auf macOS

Der FT-991A hängt auf macOS als `CP2105 Dual USB to UART Bridge Controller` am System. Dieses Gerät stellt zwei serielle Ports bereit:

- `Standard Com Port` -> `/dev/cu.usbserial-0082AAF11`
- `Enhanced Com Port` -> `/dev/cu.usbserial-0082AAF10`

Für dieses Gerät war der funktionierende native CAT-Pfad:

- Port: `/dev/cu.usbserial-0082AAF10`
- Baudrate: `38400`
- `RTS an`
- `DTR aus`

Mit `RTS aus` kam auf dem richtigen Port keine Antwort. Auf dem falschen Port kam gar nichts. Auf einem anderen `usbmodem`-Port kam nur Echo.

## Entscheidender externer Test

Direkter Python-Test außerhalb der App:

- `/dev/cu.usbserial-0082AAF10`, `rts=off` -> keine Antwort
- `/dev/cu.usbserial-0082AAF10`, `rts=on` -> echte Antwort, z. B.

```text
ID0670;?;SM0010;FA021074000;SM0010;MD0C;IF080021074000+000000C02
```

- `/dev/cu.usbserial-0082AAF11` -> keine Antwort
- `usbmodemM4AE000C646` -> nur Echo

Das beweist:

- App-Problem war teilweise real
- aber die eigentliche Hardware-/Leitungskombination musste exakt stimmen
- `Enhanced + RTS an` war der Schlüssel

## CAT-/Serial-Probleme, die gefunden und behoben wurden

- Port offen wurde anfangs als echte Verbindung behandelt, obwohl noch keine gültige CAT-Antwort vorlag
- Echo-Antworten wie `RX: FA;` wurden anfangs als gültige Antworten interpretiert
- Logs zeigten zusätzlich `INFO: Cmd overflow!`, also zu aggressives Polling oder zu viele parallele Anfragen
- RX-Pufferparser konnte mit `removeFirst(...)` abstürzen
- `TX0` wurde fälschlich als `TX an` benutzt; korrekt ist beim FT-991A:
  - `TX1` = Senden an
  - `TX0` = Senden aus

## Was am Ende für CAT stabil funktioniert hat

1. Verbindung erst nach echtem CAT-Handshake als verbunden markieren
2. Echo-Antworten ignorieren
3. Handshake mit `ID;` und `FA;`
4. Für den richtigen CP2105-Port `RTS an` verwenden
5. Polling deutlich entschärfen
6. Benutzerbefehle kurz vor Polling priorisieren
7. Separates CAT-Trace-Logging einbauen
8. `Ping CAT` und `Auto-Ping` ergänzen
9. TX-Kommandos korrigieren

## Diagnose und Logging

Normales App-Log:

- `~/Library/Application Support/FT991A-Remote/Logs/ft991a_YYYY-MM-DD.log`

CAT-Trace:

- `~/Library/Application Support/FT991A-Remote/Logs/ft991a_cat_trace_YYYY-MM-DD.log`

CAT-Trace enthält:

- Session-Start
- Portname
- Portpfad
- Baudrate
- rohe `TX`
- rohe `RX`
- Klassifikation `echo` / `overflow` / `valid`
- Handshake-Modus `rts-off` / `rts-on`

Zusätzliche Diagnosehilfen in der App:

- `CAT alive`-Status
- `Ping CAT`
- `Auto-Ping`
- Debug-Panel
- Support-Bundle-Export auf den Desktop

## UI- und Produktänderungen

- sichtbarer App-Name auf `MacYaesu` geändert
- neues dunkles App-Icon mit Funk-/Meter-Anmutung eingebaut
- Setup-/Support-Karte im Hauptfenster ergänzt
- bevorzugter CAT-Port wird gespeichert
- `Auto-Connect on Launch` ergänzt
- Support-Bundle-Export ergänzt

Wichtig:

- interne Log-/Settings-Pfade heißen noch `FT991A-Remote`
- das wurde bewusst noch nicht migriert, um bestehende Daten und Logs nicht zu brechen

## Speicher, Scan und Funkfunktionen

### 100 Memories

Es gibt jetzt eine persistente Speicherverwaltung mit bis zu 100 Einträgen.

Gespeichert werden:

- Name
- Frequenz
- Modulationsart
- Split
- Offset
- Leistung
- Tone-Modus
- CTCSS-Frequenz
- DCS-Code

Import/Export:

- JSON-Import
- JSON-Export

### Scan

Es gibt jetzt:

- `Scan Up`
- `Scan Down`
- `Stop`
- `Memory Up`
- `Memory Down`

VFO-Scan nutzt die eingestellte Schrittweite.  
Memory-Scan springt durch gespeicherte Kanäle und lädt den kompletten Kanalzustand.

### Repeater / Tone / DCS

Es wurde vollständige Tone-Steuerung ergänzt:

- `Off`
- `CTCSS Encode`
- `CTCSS Encode + Decode`
- `DCS Encode`
- `DCS Encode + Decode`

Zusätzlich:

- CTCSS-Frequenz auswählbar
- DCS-Code auswählbar
- Repeater-Schnellsektion für VHF/UHF
- Shift/Offset/Tone als zusammenhängendes Repeater-Setup
- alles in Memories mitspeicherbar

## Tastatursteuerung

Neu umgesetzt:

- `↑ / ↓` = Frequenz hoch/runter mit normaler eingestellter Schrittweite
- `← / →` = Feinabstimmung hoch/runter, aktuell 1/10 der eingestellten Schrittweite, mindestens `10 Hz`

Vorher war `↑` noch auf `ATU Tune` gebunden. Diese Bindung wurde in der Modern-Ansicht entfernt.

## Wichtige Codebereiche

Serielle Schicht:

- `MacYaesu/Services/SerialPortManager.swift`
- Port-Erkennung verbessert
- CP2105-Ports unterscheiden
- bevorzugten Leitungsmodus pro Port hinterlegt
- Handshake-Timeout eingebaut
- automatischer Retry mit anderem RTS-Modus
- saubere RX-Pufferverarbeitung
- separates CAT-Trace-Logging

CAT-Protokoll:

- `MacYaesu/Services/CATProtocol.swift`
- Echo/Overflow-Erkennung
- weniger aggressives Polling
- Round-robin-Statusabfragen statt Burst
- Benutzerbefehle pausieren Polling kurz
- `Ping CAT`
- `Auto-Ping`
- Tone-/Split-/TX-Steuerung

CAT-Response-Modell:

- `MacYaesu/Models/CATCommand.swift`
- Echo-Erkennung
- Overflow-Erkennung
- TX-Kommando korrigiert
- Tone-/CN-/CT-Kommandos

UI/State:

- `MacYaesu/ViewModels/RadioViewModel.swift`
- `MacYaesu/Views/MainView.swift`
- `MacYaesu/Views/ModernView/ModernRadioView.swift`
- `MacYaesu/Views/SkeuomorphView/SkeuomorphRadioView.swift`
- `MacYaesu/Views/MenuBar/MenuBarView.swift`
- `MacYaesu/Views/Panels/DebugPanel.swift`
- `MacYaesu/Views/Settings/SettingsView.swift`

Settings/Memory:

- `MacYaesu/ViewModels/SettingsController.swift`
- enthält auch `MemoryStore`

Logger:

- `MacYaesu/Utilities/Logger.swift`

Externes Testtool:

- `cat_probe.py`

## Build- und Projektstand

Projektstatus:

- sichtbarer Produktname: `MacYaesu`
- Projektdatei: `MacYesu.xcodeproj`
- Target/Scheme: `MacYesu`
- Modul-/Produktmix ist aktuell also noch nicht vollständig umbenannt

Schon korrigiert:

- `INFOPLIST_FILE` zeigt jetzt auf `MacYaesu/Info.plist`
- `CODE_SIGN_ENTITLEMENTS` zeigt jetzt auf `MacYaesu/FT991A_Remote.entitlements`
- Produktname/Bundle-ID wurden angepasst
- App-Ausgabe heißt `MacYaesu.app`

Aktuelle Build-Situation:

- die neuen Änderungen für CAT, Speicher, Scan, Tone und Keyboard sind fachlich eingebaut
- `xcodebuild` scheitert in dieser Umgebung weiterhin vor allem an den vorhandenen `#Preview`-Makros
- die zuletzt geprüften Änderungen in `ModernRadioView.swift`, `RadioViewModel.swift` und `SettingsView.swift` verursachten keinen separaten neuen Buildfehler

## Verkaufen / Distribution

Mac App Store wurde als weniger passend eingeschätzt, weil die App auf direkten Serial-/CAT-Zugriff angewiesen ist. Der realistischere Weg ist:

- native macOS-App außerhalb des Stores
- `Developer ID`
- `Notarisierung`
- `.app`, `.zip` oder `.dmg`

Noch offen für eine saubere Vertriebsversion:

- Target/Scheme/Projektdatei konsistent auf `MacYaesu` umbenennen
- `#Preview`-Thema bereinigen oder für Headless-Builds umgehen
- ggf. interne Support-/Log-Verzeichnisse von `FT991A-Remote` nach `MacYaesu` migrieren
- Release-Signing / Notarisierung
- breitere Testmatrix

## Lehren für andere KI / Entwickler

Wenn ein CAT-/Serial-Programm `verbunden aber ohne Wirkung` zeigt:

- nicht `open()` oder Port-Connect mit echter Gerätekonnektivität verwechseln
- immer echten Protokoll-Handshake verlangen
- Echo-Antworten separat erkennen
- bei Funkgeräten Polling konservativ halten
- `RTS/DTR` explizit testen, nicht implizit dem OS überlassen
- Dual-UART-Chips wie CP2105 können unterschiedliche Funktionskanäle haben
- Windows funktionierend bedeutet nicht, dass dieselbe native macOS-Leitungseinstellung funktioniert
- immer ein rohes Trace-Log bauen, nicht nur UI-Status
- bei FT-991A auf macOS zuerst `Enhanced Port + RTS an + 38400` annehmen

## Git-Stand

Lokaler Commit erstellt:

- `0cb6f9a`

Nicht gepusht, weil GitHub-HTTPS-Auth in der Shell nicht eingerichtet war.
