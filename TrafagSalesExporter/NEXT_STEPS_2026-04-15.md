# Next Steps

Stand: 2026-04-15

## Nachtrag 2026-04-17 Refactoring-Fortschritt

Mehrere frueher als hoch priorisiert markierte Architekturpunkte sind inzwischen bereits umgesetzt.

Erledigt:

- DataSourceAdapter-Pattern fuer `HANA`, `SAP_GATEWAY`, `MANUAL_EXCEL`
- `SiteExportService` deutlich verschlankt
- Page-Services auf `Scoped`
- `DatabaseInitializationService` in Schema-/Seed-/Orchestrator-Bloecke getrennt
- `Dashboard`, `Logs` und `Transformations` von direktem `DbContext`-Zugriff befreit
- HANA-SQL-Injection-Pfad geschlossen
- blockierende `.GetAwaiter().GetResult()`-Aufrufe im HANA-Pfad entfernt

Neuer verifizierter Stand:

- `dotnet build .\TrafagSalesExporter.csproj --verbosity minimal` erfolgreich
- `dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal`
- `36/36` Tests gruen

### Neue Top-Prioritaeten ab jetzt

#### 1. Adapter- und Resolver-Tests nachziehen

Prio hoch.

Warum:

- das neue `DataSourceAdapter`-Pattern ist architektonisch wichtig
- genau dieser neue Schnitt hat aktuell noch keine gezielten Unit-Tests

Sinnvoll waeren:

- `DataSourceAdapterResolver`-Tests
- `HanaDataSourceAdapter`-Tests
- `SapGatewayDataSourceAdapter`-Tests
- `ManualExcelDataSourceAdapter`-Tests

#### 2. Retry-/Robustheitslayer

Prio hoch.

Vor allem fuer:

- SharePoint
- SAP Gateway
- HANA-nahe Netzpfade

Aktuell brechen diese Integrationen bei transienten Problemen zu direkt ab.

#### 3. Secret-Store-Konzept

Prio hoch bis mittel.

Aktuell liegen Zugangsdaten weiterhin in der App-/DB-Konfiguration.
Langfristig sollte entschieden werden:

- Windows Credential Manager
- DPAPI / verschluesselte Ablage
- externer Secret Store

#### 4. `DatabaseInitializationService` weiter haerten, aber nicht mehr blind gross refactoren

Prio mittel.

Der schlimmste Architekturteil ist deutlich besser als vorher.
Weitere Arbeit dort sollte jetzt nur noch zielgerichtet passieren:

- Regressionstests fuer konkrete Legacy-/Repair-Zustaende
- spaeter moeglichst versionierte Migrationen

#### 5. MudBlazor-Analyzer-Warnungen bereinigen

Prio mittel.

Nicht kritisch fuer Produktion, aber sinnvoll fuer sauberen Build:

- `Logs.razor`
- `Transformations.razor`
- `Standorte.razor`

### Was im Vergleich zu frueher nicht mehr Top-Prioritaet ist

Nicht mehr ganz oben:

- generisches weiteres Page-Service-Refactoring um des Refactorings willen
- noch mehr strukturelles Verschieben ohne Risikoreduktion

Der wirtschaftlich sinnvolle Fokus liegt jetzt eher auf:

- Absicherung
- Robustheit
- Integrationsstabilitaet

## Nachtrag 2026-04-17

Der Punkt `CHF-Umrechnung / Wechselkurse` ist nicht mehr komplett offen.

Der aktuelle Ist-Stand ist:

- `CurrencyExchangeRateService` ist implementiert
- `ExchangeRateImportService` importiert ECB-Kurse
- `NormalizeCurrencyCode` und `ConvertCurrency` sind im Transformationssystem registriert
- fehlende Unit-Tests dafuer wurden am 2026-04-17 ergaenzt

Neuer Teststand:

- `dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal`
- erfolgreich
- `31/31` Tests gruen

Was fuer Waehrungen trotzdem noch offen bleibt:

- fachlicher Einsatz der `ConvertCurrency`-Regeln in echten Standortkonfigurationen pruefen
- UI-Flow fuer Wechselkurspflege in `Settings.razor` manuell gegenpruefen
- ECB-Import einmal real ueber die UI bzw. App-Funktion pruefen
- bestaetigen, fuer welche Sichten CHF die Zielwaehrung sein soll
- Management-Cockpit-Rohsicht nur dann auf CHF umstellen, wenn fachlich gewuenscht

## Architektur-Nachtrag 2026-04-17

Nach einer separaten Architekturpruefung wurden die naechsten Schritte neu priorisiert.

Wichtig:

- neue Fachfeatures sind aktuell **nicht** der erste Engpass
- zuerst muessen die Architektur-Risiken in Initialisierung, Config-Import und UI-Service-Schnitt bereinigt werden

### Neue Top-Prioritaeten

#### 1. `DatabaseInitializationService` absichern

Prio sehr hoch.

Gruende:

- Startlogik enthaelt manuelle Schema-Migrationen
- FK-Reparaturen laufen produktiv beim App-Start
- dort wurde ein konkretes Risiko fuer verschobene Spaltenwerte beim `Sites_old`-Kopierpfad erkannt

Vor weiterer Fachentwicklung:

- Initialisierungspfad genau pruefen
- SQL-Kopierlogik validieren
- moeglichst Richtung versionierte Migrationen bewegen

#### 2. `ConfigTransferService.ImportJsonAsync` neu denken

Prio sehr hoch.

Aktuelles Problem:

- Import loescht sehr viel und baut danach stueckweise neu auf
- nicht atomar
- potenziell teilzerstoerter Zustand bei Fehlern
- `CentralSalesRecords` werden mitimportiert/mitgeloescht, obwohl sie eher Laufzeitdaten als Konfiguration sind

Ziel:

- atomarer Import
- saubere Trennung zwischen Konfiguration und Betriebsdaten

#### 3. Razor-Seiten entlasten

Prio hoch.

Betroffen vor allem:

- `Components/Pages/Settings.razor`
- `Components/Pages/Standorte.razor`

Ziel:

- DB- und Fachlogik aus UI-Code in Services / Application-Layer verschieben
- Seiten nur noch fuer Interaktion und Formularzustand

#### 4. Konsolidierten Export semantisch klaeren

Prio mittel.

Offene Frage:

- zentrale Datei aus laufendem Snapshot
  oder
- zentrale Datei immer aus `CentralSalesRecords`

Aktuell ist die Verantwortung unscharf.

#### 5. Reporting verallgemeinern

Prio mittel.

Erst nach den Infrastrukturthemen:

- hartcodierte Jahreslogik im Cockpit entfernen
- fachlich entscheiden, ob und wo CHF-Rohsicht gebraucht wird

### Praktische Reihenfolge fuer den naechsten Wiedereinstieg

Wenn nach erneutem Absturz oder Kontextverlust weitergemacht wird:

1. `HANDOFF_2026-04-15.md` lesen, speziell die Architekturpruefung vom 2026-04-17
2. `DatabaseInitializationService` als ersten Risikoblock ansehen
3. `ConfigTransferService.ImportJsonAsync` als zweiten Risikoblock ansehen
4. erst danach wieder an Cockpit / CHF / weitere Fachfeatures gehen

## Nachtrag HANA-/Standort-Workflow 2026-04-17

Der doppelte HANA-Workflow wurde inzwischen bereits bereinigt.

Neuer Stand:

- oben zentrale HANA-Konfiguration pro Quellsystem `BI1` / `SAGE`
- unten im Standort keine eigene wirksame Voll-HANA-Konfiguration mehr
- HANA-basierte Standorte ziehen ihre technische Verbindung aus der zentralen Quellsystem-Konfiguration
- Standort bleibt fuer fachliche Daten und optionale Credential-Overrides zustaendig
- die frueher doppelte HANA-UI im Standortdialog ist inzwischen auch sichtbar entfernt
- der Verbindungstest in `Settings.razor` prueft und meldet jetzt die zentrale HANA-Verbindung klar

### Was dazu noch praktisch geprueft werden sollte

- `Standorte`-Seite im UI manuell durchklicken
- pruefen, ob `BI1`- und `SAGE`-Standort beim Speichern sauber auf die zentrale HANA-Konfiguration zeigen
- pruefen, ob Aenderung oben bei zentraler HANA-Konfiguration in nachfolgenden Exporten wirklich greift

### Anschlussarbeiten

- `ConfigTransferService` spaeter auf das neue zentrale HANA-Modell fachlich nachziehen und kritisch pruefen
- `DatabaseInitializationService` weiter konsolidieren, damit die Zuordnung alter HANA-Daten langfristig robuster wird

## Nachtrag Quellsystem-Verwaltung 2026-04-17

Die bisher hart codierten Quellsystem-Listen wurden ersetzt.

Neuer Stand:

- `SourceSystemDefinition` ist jetzt die zentrale Stammdatenquelle fuer Quellsysteme
- `Settings.razor` hat jetzt eine GUI zur Pflege von Quellsystemen
- `Standorte.razor` zieht seine Quellsystem-Auswahl aus diesen Stammdaten
- `Transformations.razor` zieht die Systemauswahl ebenfalls aus diesen Stammdaten
- zentrale Credentials haengen jetzt am Quellsystem selbst
- HANA-Zentralverbindungen werden nur noch fuer Quellsysteme mit Anschlussart `HANA` gezeigt
- alte zentrale Credential-Felder in `ExportSettings` sind aus dem aktiven Codepfad entfernt
- `ExportSettings` wird beim Start auch schematisch auf das neue Feldset bereinigt
- HANA speichert zentral keine eigenen Credentials mehr; dort bleiben nur technische Verbindungsdaten
- `HanaServer.Username` / `Password` sind nur noch Laufzeitfelder und nicht mehr im EF-Schema gemappt
- SAP Service URL wird jetzt zentral im Quellsystem gepflegt; der Standort haelt nur noch ein optionales Override
- Quellsysteme werden jetzt per Dialog bearbeitet statt nur ueber Inline-Tabellenfelder

### Was dazu noch praktisch geprueft werden sollte

- in `Settings` ein neues Quellsystem per GUI anlegen
- pruefen, ob es danach in `Standorte` und `Transformations` sofort auswählbar ist
- pruefen, ob deaktivierte Quellsysteme in neuen Standort-/Regelanlagen nicht mehr normal angeboten werden
- pruefen, ob Aenderung der Anschlussart von `HANA` auf `SAP_GATEWAY` oder `MANUAL_EXCEL` fachlich sauber wirkt
- pruefen, ob bestehende BI1/SAGE/SAP-Daten nach Startmigration korrekt in `SourceSystemDefinitions` stehen
- pruefen, ob Konfiguration-Export/Import ohne die alten Credential-Felder sauber mit `SourceSystemDefinitions` arbeitet
- pruefen, ob zentrale SAP Service URL ohne Override sauber fuer Refresh, Export und Dashboard greift
- pruefen, ob SAP Service URL Override am Standort die zentrale URL erwartungsgemaess uebersteuert

## Nachtrag 2026-04-16

Seit dem letzten Stand kamen mehrere groessere Erweiterungen dazu. Die offenen Punkte unten muessen deshalb im neuen Kontext gelesen werden.

## 0. Neuer Ist-Stand

Zusaetzlich zum alten Stand ist jetzt vorhanden:

- manueller Standort-Import ueber `MANUAL_EXCEL`
- Dashboard mit `Alle exportieren`, `Zentrale Datei neu erzeugen` und zentralem `Excel oeffnen`
- Roh-Auswertung im `Management Cockpit` direkt aus `CentralSalesRecords`
- erweitertes Transformationssystem mit `Value`- und `Record`-Regeln
- HANA-Schema-Lookup im Standortdialog
- Testprojekt mit aktuell 18 gruenden Tests

## 1. Status

Der Export geht jetzt wieder durch.

Die zuletzt gefundene Hauptursache war nicht mehr ein reiner SQLite-Lock beim Batch-Insert, sondern ein kaputter FK-Schemazustand in der bestehenden DB:

- SQLite referenzierte in mindestens einer Tabelle noch `main.Sites_old`
- dadurch scheiterte `SaveChangesAsync()` beim Schreiben z. B. in `AppEventLogs` oder `ExportLogs`
- sichtbarer Effekt: Export blieb nach `Zentrale Tabelle: ... Datensaetze gespeichert.` haengen

## 2. Umgesetzter Fix

Umgesetzt wurde:

- Dashboard-Live-Status liest waehrend laufendem Export nicht mehr staendig aus `AppEventLogs`, sondern nutzt den In-Memory-Status des `ExportOrchestrationService`
- SQLite `Default Timeout` in `Program.cs` auf `60` erhoeht
- `CentralSalesRecordService` setzt nach den Batches explizit `Zentrale Tabelle aktualisiert`
- `DatabaseInitializationService` repariert beim App-Start automatisch Tabellen, deren FK-SQL noch `Sites_old` referenziert

Betroffene Dateien:

- `Program.cs`
- `Components/Pages/Dashboard.razor`
- `Services/CentralSalesRecordService.cs`
- `Services/DatabaseInitializationService.cs`

## 3. Was noch getestet werden sollte

Kurz gegenpruefen:

- Export eines Standorts erneut
- `Excel oeffnen` nach erfolgreichem Export
- `Export erfolgreich` inkl. `Pfad=...`
- Dashboard-Live-Status setzt sich nach Abschluss sauber zurueck
- `Alle exportieren`
- `Zentrale Datei neu erzeugen`
- zentrale Datei im Dashboard oeffnen

## 3a. Manuellen Excel-Import pruefen

Zu testen:

- Standort auf `MANUAL_EXCEL` stellen
- Excel im Standort hochladen
- Standort exportieren
- pruefen, ob `CentralSalesRecords` fuer diesen Standort ersetzt wurden
- pruefen, ob der zentrale Export den Standort korrekt enthaelt

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/ManualExcelImportService.cs`
- `Services/SiteExportService.cs`

## 3b. HANA-Schema-Lookup pruefen

Zu testen:

- bei `BI1`-Standort `Schemas laden`
- bei `SAGE`-Standort `Schemas laden`
- wird ein plausibles B1-Schema angeboten?
- funktioniert danach Export ohne manuelle Schema-Eingabe?
- zeigt England / Spezialstandort jetzt schneller, wenn Schema oder Rechte nicht passen?

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/HanaQueryService.cs`

## 4. Falls wieder ein Fehler auftritt

In dieser Reihenfolge pruefen:

1. Exakte Fehlermeldung aus `AppEventLogs` bzw. Console notieren
2. Pruefen, ob die Reparaturlogik beim Start gelaufen ist
3. Pruefen, ob noch weitere Tabellen mit veralteter FK-Referenz existieren
4. Erst danach wieder am Batch-/Commit-Pfad der zentralen Speicherung arbeiten

## 5. SAP-Funktionalitaet kurz gegenpruefen

Zu testen:

- `Quellen refreshen`
- `Felder aus Quellen laden`
- `Auto-Match`
- SAP-Export eines Standorts

Dateien:

- `Components/Pages/Standorte.razor`
- `Services/SapGatewayService.cs`
- `Services/SapCompositionService.cs`

## 6. Management Cockpit pruefen

Zu testen:

- vorhandene Excel-Datei auswaehlbar
- Analyse laeuft
- Kennzahlen plausibel
- Roh-Auswertung aus `CentralSalesRecords` laeuft
- Jahr/Monat-Filter funktionieren
- Summen nach Quelle / Land plausibel

Dateien:

- `Components/Pages/ManagementCockpit.razor`
- `Services/ManagementCockpitService.cs`

## 6a. Fachlich bewusst noch offen

Noch nicht final umsetzen ohne Rueckmeldung Fachseite:

- Intercompany-Filter
- fachliche Nutzung der CHF-Umrechnung in Cockpit / Reports
- Budgetvergleich
- Gruppenlogik
- Spartenlogik
- Margenlogik

Diese Punkte sollen spaeter moeglichst dynamisch auf dem neuen Transformations-/Mapping-Ansatz aufsetzen, aber aktuell nicht hart geraten werden.

## 6b. Naechste sinnvolle Testkandidaten

Wenn weiter in Tests investiert wird, sind die naechsten Kandidaten:

- `ExportOrchestrationService`
- spaeter End-to-End-Tests fuer den Wechselkurs-/Transformationspfad
- spaeter evtl. SQLite-nahe Integrationstests fuer `DatabaseInitializationService`

Aktueller Teststatus:

- `dotnet test .\TrafagSalesExporter.Tests\TrafagSalesExporter.Tests.csproj --verbosity minimal`
- erfolgreich
- `31/31` Tests gruen

## 7. Referenzdatei

Fuer den vollstaendigen Kontext zuerst lesen:

- `HANDOFF_2026-04-15.md`

## 8. Letzte bereinigte UI-Irritation

Stand 2026-04-17:

- In `Standorte` wurde die obere Box auf `Zentrale HANA-Technik` geklaert.
- Dort gibt es keinen `Server hinzufuegen`-Pfad mehr.
- Grund: zentrale HANA-Eintraege werden aus `Quellsystemen` mit Anschlussart `HANA` abgeleitet.
- `SAP` gehoert fachlich nicht in diese Box, sondern in `Settings -> Quellsysteme`.

Wichtig fuer den naechsten Wiedereinstieg:

- Wenn ein Benutzer fragt `wo ist SAP?`, ist die richtige Antwort: nicht in der HANA-Box, sondern in der zentralen Quellsystem-Verwaltung.
- Wenn ein HANA-System oben fehlt, zuerst `Settings -> Quellsysteme` pruefen und dort Anschlussart `HANA` setzen.

## 9. Config-Transfer erneut geprueft

Stand 2026-04-17:

- Der aktuelle Config-Import/-Export passt zum neuen Datenmodell.
- Zentral verwaltete Quellsysteme, SAP-Zentral-URL, HANA-Technik ohne HANA-Credentials und Standort-Overrides werden korrekt im Transferformat abgebildet.
- Die vorhandenen `ConfigTransferServiceTests` bestaetigen den aktuellen Rundlauf.

Fuer den naechsten Wiedereinstieg wichtig:

- Das aktuelle Format ist fuer heutige Exporte konsistent.
- `ImportJsonAsync` ist aber weiterhin nicht atomar und loescht zuerst produktive Konfiguration.
- Zusaetzlich gibt es ein Altformat-Risiko:
  - aeltere JSONs mit `SourceSystemDefinitions`, aber ohne `ConnectionKind`, koennen wegen DTO-Default falsch als `HANA` interpretiert werden.

Naechste saubere Haertung fuer dieses Thema:

- Config-Import transaktional machen
- Legacy-Fallback fuer fehlendes `ConnectionKind` einbauen
