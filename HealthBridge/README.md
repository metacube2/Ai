# HealthBridge

Intelligente Health-Daten-Synchronisation für iOS – Eine "Single Source of Truth" für Gesundheitsdaten.

## Übersicht

HealthBridge liest alle Quellen aus Apple Health, erkennt Konflikte zwischen verschiedenen Geräten und Apps, merged intelligent basierend auf konfigurierbaren Regeln und schreibt bereinigte Daten zurück.

## Features

### Source Discovery
- Automatische Erkennung aller verbundenen Datenquellen
- Klassifizierung nach Gerätetyp (iPhone, Apple Watch, Drittanbieter-Watch, Apps)
- Übersicht über Fähigkeiten und unterstützte Datentypen pro Quelle

### Konflikt-Erkennung
- Automatische Erkennung von Datenkonflikten zwischen Quellen
- Zeitfenster-basierte Analyse (15-Minuten-Intervalle)
- Schweregrad-Klassifizierung (minor, moderate, significant, major)

### Merge-Strategien
- **Exclusive**: Nur eine Quelle möglich (z.B. Blutdruck, SpO2)
- **Priority**: Fixe Rangfolge basierend auf Benutzereinstellungen
- **Higher Wins**: Grösserer Wert gewinnt (ideal für Schritte)
- **Coverage**: Quelle mit Daten für Zeitfenster gewinnt
- **Coverage Then Higher**: Erst Abdeckung, dann höherer Wert
- **Average**: Durchschnitt aller Quellen
- **Manual**: Benutzer entscheidet bei jedem Konflikt

### UI-Komponenten
- **Dashboard**: Tagesübersicht aller Gesundheitsdaten mit Sync-Status
- **Konflikte**: Liste offener Konflikte mit One-Tap-Auflösung
- **Regeln**: Konfiguration der Merge-Strategien pro Datentyp
- **Quellen**: Übersicht aller erkannten Datenquellen

### Background Sync
- Automatische Synchronisierung im Hintergrund
- Konfigurierbares Intervall (15 Min bis 2 Stunden)
- Push-Benachrichtigungen bei neuen Konflikten

## Unterstützte Datentypen

| Datentyp | Primärquelle | Strategie |
|----------|--------------|-----------|
| Schritte | Watch | Coverage + Higher |
| Herzfrequenz | Watch | Exclusive |
| Blutdruck | Watch D2 | Exclusive |
| SpO2 | Watch | Exclusive |
| Schlaf | Watch | Priority |
| Distanz | Watch/iPhone | Coverage + Higher |
| Stockwerke | iPhone | Exclusive |
| Aktive Energie | Watch | Coverage + Higher |

## Architektur

```
┌─────────────────────────────────────────────────────────────────┐
│                        Apple Health                             │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │
┌─────────────────────────────────────────────────────────────────┐
│                      HealthBridge App                           │
│                                                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐            │
│  │ DataReader  │→ │ MergeEngine │→ │ DataWriter  │            │
│  └─────────────┘  └─────────────┘  └─────────────┘            │
│         ▲               ▲                                      │
│  ┌─────────────┐  ┌─────────────┐                             │
│  │SourceManager│  │ RuleEngine  │                             │
│  └─────────────┘  └─────────────┘                             │
│                                                                │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │                   SyncCoordinator                        │  │
│  └─────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Projektstruktur

```
HealthBridge/
├── App/
│   └── HealthBridgeApp.swift      # App-Entry, Background Tasks
├── Models/
│   ├── HealthDataTypes.swift      # Datentypen, TimeWindow
│   ├── Source.swift               # HealthSource, SourceReading
│   └── Conflict.swift             # Conflict, MergeStrategy, MergeRule
├── Services/
│   ├── HealthKitManager.swift     # HealthKit-Integration
│   ├── SourceManager.swift        # Source Discovery & Management
│   ├── DataReader.swift           # Daten lesen, Konflikte erkennen
│   ├── RuleEngine.swift           # Merge-Regeln verwalten & anwenden
│   ├── MergeEngine.swift          # Konflikte analysieren & lösen
│   ├── DataWriter.swift           # Daten zurückschreiben
│   └── SyncCoordinator.swift      # Orchestrierung aller Services
├── Views/
│   ├── ContentView.swift          # Tab-Navigation
│   ├── DashboardView.swift        # Hauptübersicht
│   ├── ConflictsView.swift        # Konflikt-Liste & Detail
│   ├── RulesView.swift            # Regelwerk-Editor
│   ├── SourcesView.swift          # Quellen-Übersicht
│   ├── SettingsView.swift         # Einstellungen
│   └── Components/
│       └── HealthChart.swift      # Diagramm-Komponenten
├── ViewModels/
│   └── DashboardViewModel.swift   # Dashboard-Logik
├── Utils/
│   ├── Extensions.swift           # Swift-Erweiterungen
│   └── NotificationManager.swift  # Push-Benachrichtigungen
└── Resources/
    ├── Info.plist                 # App-Konfiguration
    └── HealthBridge.entitlements  # HealthKit-Berechtigungen
```

## Voraussetzungen

- iOS 16.0+
- Xcode 15.0+
- Apple Developer Account (für HealthKit-Entitlements)
- Physisches Gerät (HealthKit nicht im Simulator verfügbar)

## Installation

1. Projekt in Xcode öffnen
2. Team für Code Signing auswählen
3. HealthKit-Capability aktivieren
4. Auf physischem Gerät ausführen

## Berechtigungen

Die App benötigt folgende Berechtigungen:
- **HealthKit Read**: Lesen aller Gesundheitsdaten
- **HealthKit Write**: Schreiben gemergter Daten
- **Background App Refresh**: Für automatische Synchronisierung
- **Notifications**: Für Konflikt-Benachrichtigungen

## Lizenz

MIT License
