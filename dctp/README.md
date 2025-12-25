# DCTP - Delta Code Transfer Protocol

Ein Tool um KI-generierten Code effizient in lokale Dateien zu uebertragen. Statt bei jeder Korrektur den kompletten Code neu zu senden, werden nur Aenderungen (Deltas) uebertragen.

## Das Problem

Claude generiert 500 Zeilen Code. Eine kleine Korrektur = nochmal 500 Zeilen. Verschwendung.

## Die Loesung

Zeilennummerierter Code + Steueranweisungen fuer gezielte Aenderungen.

## Installation

```bash
# Requirements installieren
pip install -r requirements.txt

# GUI starten
python dctp_gui.py
```

## Schnellstart

### 1. Projektverzeichnis waehlen

Klicke auf "Waehlen" und waehle dein Projektverzeichnis.

### 2. KI-Output einfuegen

Kopiere den DCTP-formatierten Output aus deinem Claude-Chat in das Input-Feld.

### 3. Analysieren

Klicke "Analysieren" um eine Vorschau der Operationen zu sehen.

### 4. Ausfuehren

Klicke "Ausfuehren" um die Aenderungen auf deine Dateien anzuwenden.

## DCTP-Format

### Neue Datei erstellen

```
###FILE:src/calculator.py
###NEW
def add(a, b):  #Z1
    return a + b  #Z2
###END
```

### Zeilen ersetzen

```
###FILE:src/calculator.py
###REPLACE:Z1-Z2
def add(a: int, b: int) -> int:  #Z1
    """Addiert zwei Zahlen."""  #Z2
    return a + b  #Z3
###END
###RENUMBER
```

### Zeilen einfuegen

```
###FILE:src/calculator.py
###INSERT_AFTER:Z2
  #Z3
def subtract(a, b):  #Z4
    return a - b  #Z5
###END
###RENUMBER
```

### Zeilen loeschen

```
###FILE:src/calculator.py
###DELETE:Z10-Z15
###RENUMBER
```

## Zeilennummern-Format

Die Zeilennummern werden automatisch entsprechend der Programmiersprache formatiert:

| Sprache | Format | Beispiel |
|---------|--------|----------|
| Python | `#Z1` | `code  #Z1` |
| JavaScript | `//Z1` | `code  //Z1` |
| HTML | `<!--Z1-->` | `code  <!--Z1-->` |
| CSS | `/*Z1*/` | `code  /*Z1*/` |
| SQL | `--Z1` | `code  --Z1` |

## Befehle

| Befehl | Beschreibung |
|--------|--------------|
| `###FILE:pfad` | Zieldatei angeben |
| `###NEW` | Neue Datei erstellen |
| `###DELETE:Z5-Z12` | Zeilen loeschen |
| `###INSERT_AFTER:Z5` | Nach Zeile einfuegen |
| `###REPLACE:Z5-Z8` | Zeilen ersetzen |
| `###END` | Block beenden |
| `###RENUMBER` | Zeilennummern aktualisieren |
| `###CHECKSUM:hash` | Datei-Hash validieren |

## Features

- **Vorschau**: Zeigt was passieren wird, bevor es ausgefuehrt wird
- **Diff-Ansicht**: Zeigt Aenderungen farbig markiert (alt vs neu)
- **Undo**: Stellt den letzten Zustand wieder her
- **Backup**: Automatische Backups vor jeder Aenderung
- **Multi-File**: Mehrere Dateien in einem Durchgang bearbeiten
- **Checksum**: Optionale Validierung gegen externe Aenderungen

## Einstellungen

- **Projektpfad**: Standard-Projektverzeichnis
- **Backup-Verzeichnis**: Wo Backups gespeichert werden
- **Auto-Renumber**: Zeilennummern automatisch aktualisieren
- **Checksum-Validierung**: Externe Aenderungen erkennen
- **Theme**: Hell oder dunkel

## Claude-Integration

Kopiere den Inhalt von `CLAUDE.md` in deine Claude-Chats (als Custom Instructions oder am Anfang des Gespraechs), damit Claude im DCTP-Format antwortet.

## Architektur

```
dctp/
├── dctp_gui.py          # Hauptfenster (CustomTkinter)
├── dctp_parser.py       # Core-Logik: parse Steueranweisungen
├── dctp_executor.py     # Fuehrt Operationen aus
├── dctp_backup.py       # Undo/Backup-Verwaltung
├── dctp_diff.py         # Diff-Berechnung fuer Vorschau
├── requirements.txt     # Dependencies
├── CLAUDE.md            # Anweisung fuer KI
└── README.md            # Diese Datei
```

## Lizenz

MIT License
