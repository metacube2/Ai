# Admin-Menüs zusammengeführt

**Stand:** 11.08.2026  
**Status:** produktiv deployed und verifiziert am 11.08.2026, 11:23 Uhr  
**Zweck:** Wiederaufnahme nach Chatabbruch und technischer Änderungsnachweis

## Auftrag

In der linken Navigation bestanden zwei getrennte Admin-Einstiege:

1. `Admin` als Untergruppe des `Finance Cockpit` mit den technischen Konfigurationsseiten;
2. `Admin Bereich` als einzelner Hauptmenü-Link zu den aktiven Logins.

Der Inhalt des Finance-Untermenüs sollte in den äußeren, nicht in eine andere Menüstruktur
eingebetteten `Admin Bereich` verschoben werden. Danach darf es nur noch einen Admin-Einstieg
geben.

## Alte Struktur

```text
Finance Cockpit
└─ Admin
   ├─ Standorte
   ├─ Transformationen
   ├─ Finance Regeln
   ├─ Settings
   ├─ Menüstruktur
   └─ Logs

Admin Bereich
└─ öffnet direkt Aktive Logins
```

## Neue Struktur

```text
Admin Bereich                    ← Hauptmenü, ParentKey = null
├─ Aktive Logins
├─ Standorte
├─ Transformationen
├─ Finance Regeln
├─ Settings
├─ Menüstruktur
└─ Logs
```

Unter `Finance Cockpit` gibt es danach keinen zweiten Eintrag `Admin` mehr.

## Technische Umsetzung

### Standard-Menü

Geändert in `Services/DatabaseSeedService.cs`:

- der bestehende Schlüssel `finance-admin` bleibt erhalten, wird aber zur Root-Gruppe;
- deutscher Titel: `Admin Bereich`;
- englischer Titel: `Admin area`;
- `ParentKey = null`;
- Symbol: `AdminPanelSettings`;
- Sortierung im Hauptmenü: `90`;
- `admin-sessions` ist kein Root-Link mehr, sondern das erste Kind der Gruppe;
- der Link heißt jetzt `Aktive Logins` / `Active logins`;
- die übrigen Admin-Seiten bleiben unverändert erreichbar und werden darunter sortiert.

Die bestehenden Routen und Seiten wurden nicht geändert:

| Eintrag | Route |
| --- | --- |
| Aktive Logins | `admin/sessions` |
| Standorte | `standorte` |
| Transformationen | `transformations` |
| Finance Regeln | `finance-rules` |
| Settings | `settings` |
| Menüstruktur | `admin/menu-structure` |
| Logs | `logs` |

### Migration bestehender Installationen

Nur die alte Standardstruktur wird automatisch migriert. Die Migration greift, wenn:

- `finance-admin` noch unter `finance` hängt; und
- `admin-sessions` noch ohne Parent im Hauptmenü steht.

Dann werden die beiden Einträge zur neuen Struktur zusammengeführt und die Kinder eindeutig
sortiert. Wenn jemand die beiden Einträge über die Seite `Menüstruktur` bereits individuell
verschoben hat, wird diese individuelle Struktur nicht automatisch überschrieben.

Das bedeutet:

- neue Datenbanken erhalten direkt die neue Struktur;
- produktive Datenbanken mit dem bisherigen Standard werden beim nächsten Anwendungsstart
  migriert;
- ein Klick auf `Standard wiederherstellen` erzeugt ebenfalls die neue Struktur.

## Tests

Neu: `TrafagSalesExporter.Tests/NavigationMenuSeedTests.cs`

Abgedeckt sind:

1. eine neue Datenbank erzeugt genau eine äußere Admin-Gruppe mit allen sieben Kindern;
2. eine simulierte alte Standardstruktur wird korrekt zur neuen Struktur migriert;
3. `Aktive Logins` behält die Route `admin/sessions`;
4. Reihenfolge und ParentKey aller Admin-Kinder sind eindeutig.

Ausgeführt am 11.08.2026:

```powershell
dotnet test 'TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj' `
  --filter FullyQualifiedName~NavigationMenuSeedTests --no-restore
```

Ergebnis: **2/2 Tests grün**.

Anschließend vollständige Suite:

```powershell
dotnet test 'TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj' `
  --no-restore --nologo
```

Ergebnis: **461/461 Tests grün**, 0 fehlgeschlagen, 0 übersprungen.

Beim Build wurden bereits vorhandene Warnungen ausgegeben, insbesondere zwei `NU1903`-Hinweise
für `Microsoft.AspNetCore.Authentication.Negotiate 8.0.24`. Diese Änderung hat das Paket nicht
angefasst.

## Geänderte Dateien

- `Services/DatabaseSeedService.cs`
- `TrafagSalesExporter.Tests/NavigationMenuSeedTests.cs`
- `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md`
- `docs/AGENT_COORDINATION.md`

## Bewusst nicht geändert

- keine Admin-Seite und keine Route;
- keine Berechtigungsrichtlinie;
- keine manuelle Produktivdatenänderung außerhalb der gezielten, getesteten Menü-Seed-Migration;
- keine Finance-Berechnung;
- keine Änderungen aus dem parallelen Claude-Bereich.

## Produktivdeploy und Wirknachweis

Der Nutzer hat den Deploy ausdrücklich beauftragt. Ausgeführt am 11.08.2026 um 11:23 Uhr:

- `461/461` Tests im Release-Lauf grün;
- konsistentes Vorher-Backup:
  `trafag_exporter.db.before-admin-menu-merge-20260811-112250.bak`, 340.369.408 Bytes;
- Publish über den geschützten Runner nach
  `\\trch-webapp-bidashboard.trafagch.local\BiDashboard$`;
- `app_offline.htm` nur während des Publish aktiv und danach deaktiviert;
- Server-DLL und lokaler Release-Build bitgleich, SHA256
  `D1A82215B25A3D5A86E74EDFBD11F7E5E810E2A2B77A739C5C550B74D19FD7AB`;
- `/`, `/admin/sessions`, `/pause`, `/js/pausegame.js`, `/management-cockpit` und
  `/einkauf/aufriss` liefern HTTPS 200;
- keine Zieldatei verschwunden; geschützte Datenbestände im Publishvergleich unverändert.

Produktive Datenbank anschließend read-only geprüft:

```text
finance-admin   | parent=              | Admin Bereich    | Group | sort=90
admin-sessions  | parent=finance-admin | Aktive Logins    | Link  | sort=10
sites           | parent=finance-admin | Standorte        | Link  | sort=20
transformations | parent=finance-admin | Transformationen | Link  | sort=30
finance-rules   | parent=finance-admin | Finance Regeln   | Link  | sort=40
settings        | parent=finance-admin | Settings         | Link  | sort=50
menu-structure  | parent=finance-admin | Menuestruktur    | Link  | sort=60
logs            | parent=finance-admin | Logs             | Link  | sort=70
```

Damit ist die Zusammenführung produktiv wirksam. Offen bleibt nur ein visueller Test im
angemeldeten Browser; Struktur, Routen, Seed/Migration und Erreichbarkeit sind technisch
belegt.

## Wiederaufnahme nach dem Deploy

Nach einem Chatabbruch zuerst diese Datei und danach `docs/AGENT_COORDINATION.md` lesen.
Für dieses Thema ist keine weitere technische Arbeit offen, solange kein visueller oder
fachlicher Änderungswunsch folgt.
