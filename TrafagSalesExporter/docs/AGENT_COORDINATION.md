# Agenten-Koordination

Stand: 2026-08-12

Diese Datei koordiniert gleichzeitig arbeitende Entwicklungsagenten im gemeinsamen
Workspace. Vor jeder Aenderung bitte kurz lesen und den eigenen Eintrag aktualisieren.
Die Root-Dateien `AGENTS.md` und `CLAUDE.md` sowie `docs/RAG_ROUTER.md` machen
diesen Schritt fuer neue Codex-/Claude-Sitzungen ausdruecklich verpflichtend.

## Aktive Bereiche

| Agent | Bereich | Reservierte Dateien / Ordner | Status |
|---|---|---|---|
| Codex | Pausenspiel / FPV-Fernpilot | `wwwroot/js/pausegame.js`, `Tools/PauseGame.Probe/probe.mjs`, `docs/PAUSENSPIEL_*.md` | Produktiv deployed; 28/28 FPV- und 18/18 MOD-Probes gruen, manueller Spieltest offen |
| Codex | Finance / CH-AT Eigenfertigungspruefung | `.tmp_tools/CheckChAtOrigin/**`, `.tmp_tools/CheckChAtCosts/**`, `.tmp_tools/BuildSupplierReport/**`, `docs/SUPPLIER_LAENDERSTATUS_CH_AT_PRUEFUNG_2026-08-11.md`, `docs/Supplier_Laenderstatus_CH_AT_Pruefung_2026-08-11.docx` | Abgeschlossen; nur read-only Analyse und neue Nachweisdokumente, keine Produktivdaten- oder Anwendungsaenderung |
| Codex | Finance / Supplier-Fallback CH-Werkstamm | `Models/GroupMaterialMaster.cs`, `Services/SapGatewayPlantMaterialReader.cs`, Supplier-Klassifikation, Settings/Schema/Excel/Cockpit/Tests, `docs/FINANCE_SUPPLIER_FALLBACK_UMSCHALTER_2026-08-11.md` | Produktiv deployed; Modus ChPlantMaster und 66.049 MARC-1100-Materialien nach Neustart bestaetigt |
| Codex | Finance / Andreas-Nachtrag lokale Standardkosten | Supplier-Klassifikation/-Rechnung, Management-Cockpit, Excel-Hilfe, Tests, `docs/FINANCE_ANDREAS_BESCHLUSS_LOKALE_STANDARDKOSTEN_2026-08-11.md` | Produktiv deployed am 2026-08-12 10:23 durch Claude nach Freigabe durch Ingo; 478/478 Tests gruen, Wirkung nachgemessen |
| Claude | Finance / Marktsegment Railway in Sales_All | **RESERVIERT ab 2026-08-12:** `Models/CustomerMarketSegment.cs` (neu), `Services/MarketSegmentResolver.cs` (neu), `Data/AppDbContext.cs`, `Services/DatabaseInitializationService.SchemaSql.cs`, `Services/DatabaseSchemaMaintenanceService.cs`, `Services/ExcelExportService.cs`, Tests, `.tmp_tools/BuildRailwaySegmentProposal/**` (neu) | Code fertig, `491/491` Tests gruen, NICHT deployed und NICHT committed. Additiv: neue Tabelle `CustomerMarketSegments` plus zwei am Ende angehaengte Excel-Spalten. Keine bestehende Spalte verschoben, per Kopfzeilentest abgesichert. Ohne gepflegte Zuordnung bleiben beide Spalten leer. Offen: Pflegemaske und Uebernahme der bestaetigten Liste. Sales Type und Trafag-Sachnummer (ISS-013) sind BEWUSST NICHT Teil dieser Aenderung. Reservierung frei |
| Claude | Finance / Deploy des Andreas-Nachtrags und Offene-Punkte-Liste | `.tmp_tools/DeployAndreasLocal/**` (neu), `docs/rag/DEPLOYMENT.md`, `lastchange.md`, `docs/rag/FINANCE.md`, `docs/FINANCE_ANDREAS_BESCHLUSS_LOKALE_STANDARDKOSTEN_2026-08-11.md`, `docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md`, `docs/FINANCE_OFFENE_PUNKTE_2026-08-12.md` (neu), `docs/Issue_Log_Konsolidiert_2026-08-12.tsv` (neu), `docs/RAG_ROUTER.md` | Deploy abgeschlossen und dokumentiert; kein Anwendungscode geaendert, nur Dokumentation. Reservierung frei |
| Codex | UI / Admin-Menues zusammenfuehren | `Services/DatabaseSeedService.cs`, `TrafagSalesExporter.Tests/NavigationMenuSeedTests.cs`, `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md` | Produktiv deployed und in der DB verifiziert; 461/461 Tests gruen |
| Codex | Einkauf / Produktgruppen direkt aus SAP | neue SAP-Refresh-Services, `Program.cs`, `Services/DatabaseInitializationService.cs`, Produktgruppenabfragen/UI/Tests, `TrafagSalesExporter.csproj`, `docs/abap/**`, Einkaufsdokumentation | Produktiv abgeschlossen: Delta Success, 45 SAP-OData-Regeln, 0 Excel-Regeln; SAP-Key-/Textpflege D1/D5 offen |
| Claude | Finance / UK-2025-Wertfix | `.tmp_tools/CheckUk2025Result/**` (neu, nur Analysewerkzeug), lesend `neu.xlsx` und `docs/FINANCE_UK2025_WERTFEHLER_2026-08-10.md` | Abnahmepruefung abgeschlossen, kein Anwendungscode geaendert |

## Absprachen

1. Ein Agent bearbeitet nur seinen eingetragenen Bereich.
2. Vor Aenderungen an gemeinsam genutzten Dateien zuerst hier reservieren. Dazu
   gehoeren insbesondere `Program.cs`, `appsettings.json`, Projektdateien,
   Navigation, Datenbankinitialisierung und zentrale RAG-Dokumente.
3. Keine fremden Aenderungen zuruecksetzen, ueberschreiben, formatieren oder in
   einen eigenen Commit aufnehmen.
4. Projektweite Formatierungen, Paketupdates, Migrationen, Deployments und
   App-Starts werden seriell ausgefuehrt und vorher hier angekuendigt.
5. Vollstaendige Builds und Gesamttests moeglichst nacheinander ausfuehren. Lokale,
   bereichsspezifische Tests duerfen parallel laufen.
6. Beim Abschluss Status, geaenderte Dateien und Testergebnis eintragen. Danach die
   Reservierung als frei markieren, aber den Eintrag als kurze Historie stehen lassen.

## Gemeinsame Dateien / Reservierungen

| Datei oder Aktion | Reserviert durch | Seit | Zweck / Status |
|---|---|---|---|
| `Program.cs`, `TrafagSalesExporter.csproj`, `Services/DatabaseInitializationService.cs`, Produktgruppen-Services/UI/Tests und Einkaufs-/ABAP-Dokumentation | frei | 2026-08-12 | SAP-Aktivierung und produktiver Delta abgeschlossen; nur SAP-Key-/Textpflege und fachliche Stichprobe offen |
| Produktivdeploy des gesamten aktuellen Workspace-Stands | frei | 2026-08-11 | Abgeschlossen und dokumentiert in `docs/DEPLOY_GESAMTSTAND_2026-08-11.md` |

Historie: Claude hat am 2026-08-11 nach Freigabe durch Ingo die UK-2025-Abnahme
nachgetragen. Geaendert wurden `lastchange.md`,
`docs/FINANCE_UK2025_WERTFEHLER_2026-08-10.md`,
`docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md` (Punkt 3 fuehrte den Wertfehler als bereits
erledigt), `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md` (fuehrte den ueberholten Wert
`394'439.16 GBP` und hat laut Router Vorrang) und `docs/rag/FINANCE.md` (Einstiegsdatei fuer
Finance-Fragen). Reservierung wieder frei. Kein Anwendungscode, kein Commit, kein Deploy.

## Letzte Uebergaben

### Codex - Einkauf / Produktgruppen direkt aus SAP

- Live-Abschluss 2026-08-12: beide ZDISPO-Sets HTTP 200; Delta `Success` um
  10:03:42 MESZ; Cache `45` SAP-OData-Regeln und `0` Nicht-SAP-/Excel-Regeln.
- Spend-Aufriss und Materialdisposition danach HTTP 200.
- Nicht blockierende SAP-Nacharbeit: Texte fuer D1/D5 sowie zusammengesetzter
  SEGW-Key `DISPO_KZ + DISPO` fuer `ZDISPO_GRP`.

- App-Start importiert keine `zdispo*.xlsx` mehr; die Dateien werden nicht mehr
  gebaut oder publiziert.
- Einkauf-Full-Load und Delta lesen `ZDISPO_GRP` + `ZDISPO_SPART` direkt aus SAP,
  validieren die Liste und ersetzen den Cache atomar. Nur `Source = SAP OData: ...`
  wird in Spend-Aufriss und Supply Chain ausgewertet.
- SAP-Methodenruempfe und SEGW-Anleitung liegen unter
  `docs/abap/README_PRODUCT_GROUP_SAP_ODATA.md`.
- Historischer Vor-Aktivierungsnachweis vom 2026-08-11: sechs gezielte Tests und
  464/464 Gesamttests gruen; produktives `$metadata` damals HTTP 200 mit 60
  EntitySets, aber noch ohne ZDISPO-/ProductGroup-Set.
- Der am 2026-08-11 bewusst akzeptierte Zwischenstand ohne SAP-Sets wurde durch
  den oben dokumentierten erfolgreichen Live-Abschluss vom 2026-08-12 ersetzt.
- Vollstaendiger Wiederaufnahmestand:
  `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md`.

### Codex - UI / Admin-Menues zusammengefuehrt

- Die bisherige Untergruppe `Finance Cockpit > Admin` und der separate Root-Link
  `Admin Bereich` wurden zu genau einer aeusseren Root-Gruppe `Admin Bereich` vereinigt.
- Kinder: Aktive Logins, Standorte, Transformationen, Finance Regeln, Settings,
  Menuestruktur und Logs.
- Neue Datenbanken und `Standard wiederherstellen` verwenden direkt die neue Struktur.
- Bestehende Installationen werden nur migriert, wenn noch exakt die alte Standardstruktur
  vorliegt; individuelle Menueverschiebungen werden nicht ueberschrieben.
- Geaendert: `Services/DatabaseSeedService.cs`, neuer Test
  `TrafagSalesExporter.Tests/NavigationMenuSeedTests.cs` und
  `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md`.
- Nachweis: 2/2 gezielte Navigationstests und 461/461 Gesamttests gruen.
- Produktiv deployed am 11.08.2026 11:23; Root-Gruppe und alle sieben Kinder read-only in
  der produktiven DB verifiziert. Vorher konsistentes DB-Backup angelegt. Keine Route oder
  Berechtigung geaendert.

### Codex - Finance / Supplier CH-AT und Laenderstatus

- Aktuellen Export `neu.xlsx` mit 96'233 Sales-Zeilen und die produktive SQLite-DB
  ausschliesslich read-only ausgewertet; keine Daten, App-Dateien oder Deployments geaendert.
- Supplier-Vollstaendigkeit je TSC: UK 100.0 %, IT 71.2 %, IN 11.6 %, FR 5.2 %, US 0.4 %,
  CH/AT/DE/ES 0.0 %; insgesamt 18'241 von 96'233 Zeilen (19.0 %).
- Indien-Zusatznachweis live: Sales Type auf 6'686 von 7'116 Zeilen (94.0 %), daher keine
  pauschale Supplier-Massenpflege empfehlen.
- CH/AT: 47'350 von 48'932 Zeilen (96.8 %) mit Standardkosten. 18'068 Schweizer
  Fremdwaehrungszeilen folgen dem WAVWR-/Belegwaehrungspfad; 104 positive Faelle nutzen den
  CHF-Fallback. Die zentrale DB speichert nur den aufgeloesten Wert, nicht WAVWR_DC roh.
- SAP-Vorpruefung: 734 Materialien / 8'045 Zeilen intern gestuetzt; 1'191 Materialien /
  5'910 Zeilen mit echtem Einkaufsbeleg als Fremdbezugs-Pruefliste; 6'632 Materialien /
  34'977 Zeilen ohne Direktnachweis. MaterialUsageCache hat nur 105 Zeilen, daher ist fehlende
  BOM-Evidenz kein Gegenbeweis zur Eigenfertigung.
- Andreas muss nur die Hersteller-Standardregel fuer Materialien mit externem Einkaufsbeleg
  entscheiden; priorisierte Stichprobe im Bericht statt Vollpruefung.
- Nachweise: `docs/SUPPLIER_LAENDERSTATUS_CH_AT_PRUEFUNG_2026-08-11.md`, der vollständige
  Wiederaufnahmestand `docs/FINANCE_SUPPLIER_HANDOFF_2026-08-11.md` und das gestaltete,
  OpenXML-validierte Word-Dokument `docs/Supplier_Laenderstatus_CH_AT_Pruefung_2026-08-11.docx`.

### Codex - Pausenspiel

- Artillerie-/Worms-Spielkern durch direkte FPV-Fernsteuerungsmission ersetzt.
- Geaendert: `wwwroot/js/pausegame.js`, `Tools/PauseGame.Probe/probe.mjs` und die
  beiden `docs/PAUSENSPIEL_*.md`.
- Nicht geaendert: Finance-Code, `Program.cs` und `appsettings.json`.
- Nachweis vor Deploy: 28/28 FPV-Probes, 18/18 MOD-Probes und 461/461 Release-Tests gruen.
- Edge-Nachweis fuer Start, Vorflug und FPV-Szene erbracht; Kamera danach korrigiert.
- Produktiv deployed am 11.08.2026 11:23; JavaScript lokal und auf Server byteidentisch.
  Der Reiter bleibt wegen `Pause:Enabled=false` ausgeblendet; kein lokaler Serverprozess aktiv.

### Claude - Finance

- Auftrag: pruefen, ob der UK-2025-Wertfehler nach dem neuen Standortexport behoben ist.
  Grundlage ist das Abnahmekriterium in `docs/FINANCE_UK2025_WERTFEHLER_2026-08-10.md`.
- Ergebnis: alle drei Kriterien erfuellt. UK 2025 steht bei `3'529'861.80 GBP` gegen den
  Finance-Sollwert `3'538'972` (99.7 %), die Marge dreht von −502.7 % auf +33.8 % und die
  Zeilenzahl bleibt bei `1'867`.
- Neu angelegt: `.tmp_tools/CheckUk2025Result` (read-only Abnahmepruefung; loest die Spalten
  ueber die Kopfzeile auf und vergleicht ausschliesslich gegen den Finance-Sollwert, nie
  gegen die hochgeladene Importdatei).
- Nur gelesen: `neu.xlsx` im Repo-Wurzelverzeichnis, die RAG-Dateien und `persona.md`.
- Nicht geaendert: Anwendungscode, `Program.cs`, `appsettings.json`, Navigation, Datenbank,
  Deployment und alles unterhalb von `wwwroot/`. Kein Gesamtbuild und kein `dotnet test`
  ausgefuehrt, damit der Pausenspiel-Bereich nicht blockiert wird.
- Offen und bewusst NICHT ausgefuehrt: die verwaiste Zeile mit
  `TSC = "Subsidiary abbreviation / company identifier"` (1 Zeile, Jahr 2026, Wert 0.00)
  steht weiterhin in den Produktivdaten und braucht ein gezieltes `DELETE` auf
  `CentralSalesRecords`. Das ist ein eigener Schritt und beruehrt die Produktivdatenbank.
- Hinweis an den zweiten Agenten: `neu.xlsx` ist ein 41-MB-Export im Wurzelverzeichnis und
  gehoert zur Finance-Pruefung. Bitte nicht loeschen oder ueberschreiben. Die Vorgaengerdatei
  `all.xlsx` ist bereits nicht mehr vorhanden, dadurch war ein Vorher-Nachher-Vergleich mit
  demselben Werkzeug nicht moeglich.
