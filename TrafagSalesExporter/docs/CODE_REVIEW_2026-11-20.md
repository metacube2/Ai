# Code-Review 2 mit Clean-Code- und Testprüfung (20.11.2026)

Stand: 2026-11-20 14:45 (Review abgeschlossen, am Code nichts geändert; Entscheide Ingo offen).

Auftrag Ingo 20.11.: „führe ein Codereview durch über den gesamten Quellcode, dann Analyse und Doings“, danach „schreib
das alles in ein Dokument“ und „hast du auch Clean Code, TDD geprüft?“. Geprüfter Stand: `ad4f437` (produktiv ist
`26d23a9`, die Commits dazwischen sind reine Doku). Dieses Dokument ergänzt `docs/CODE_REVIEW_2026-11-12.md` und ersetzt
es nicht: Abschnitt 2 sagt je Befund vom 12.11., was heute gilt, Abschnitt 3 enthält nur, was neu ist.

## 0. Kurzfassung

1. **Der Zugriffsschutz ist das grösste Risiko, nicht die Rechenlogik.** Laut Repo ist die Windows-Berechtigung
   abgeschaltet (`appsettings.json:11`), es schützen nur die Bereichspasswörter, und mehrere Admin- und Schreibseiten haben
   gar keine Sperre.
2. **Vom Review am 12.11. ist ein Befund erledigt, zehn sind am Code bestätigt offen**, darunter alle vier hohen; zwei
   (Nr. 9 und 11) sind nicht erneut geprüft, ihre Dateien sind unverändert. Wochen_Todo 159 wartet seit acht Tagen auf
   Entscheid.
3. **Wirklich neu sind 5 hohe Befunde** (P3 Secrets-Export, S1 Shopfloor-XSS, A1 HR-Sets ohne Berechtigungsprüfung, A2
   `FinanzJournalSet` ohne Pflichtfilter, A3 P76-Werkzeug) **und rund 30 mittlere**, vor allem aus Bereichen, die am 12.11.
   nicht im Umfang waren: Shopfloor, HR-Rechenlogik, Handbestellwesen, ABAP und Exportskripte. Drei weitere hohe Zeilen
   (P1, P2, E1) erweitern Befunde, die seit dem 12.11. bekannt sind.
4. **Clean Code: gelb.** Der Code ist fachlich sorgfältig kommentiert und meist sauber parametrisiert, aber 15 Dateien
   haben über 1000 Zeilen, rund 42 Methoden über 100 Zeilen, und zentrale Regeln stehen mehrfach.
5. **Tests: grün für Rechenlogik, rot für Zugriff, Oberfläche und JavaScript.** Die Suite ist gross und grün (1785 von
   1785), die Dienste sind zu rund 70 % abgedeckt, HR und Finance zu 80 bis 91 %, und 77 % der Code-Commits bringen Tests
   mit. Razor-Komponenten haben 0,6 %, JavaScript, ABAP und Skripte keine Tests. Test-first lässt sich aus der Historie
   nicht belegen, und die Stellen der hohen Befunde sind ungetestet oder ihre Tests schreiben das heutige Verhalten fest.

| Bereich | Ampel | Befund in einem Satz |
| --- | --- | --- |
| Plattform, Sicherheit | 🔴 | Admin- und Schreibseiten faktisch offen, Cookie aus dem Hash berechenbar |
| Shopfloor, Operations | 🔴 | Gespeichertes XSS, OData-Filter ohne Maskierung, statische Dateien ohne Anmeldung |
| SAP, ABAP | 🔴 | Personaldaten ohne Berechtigungsprüfung im Einkaufs-Service, P76-Werkzeug nicht technisch „nur lesen“ |
| Einkauf, Logistik | 🟡 | SAP-Zugangsdaten über den Verbindungstest abgreifbar, Lücken im Delta, Betrag falsch gelesen |
| Finance, Verkauf | 🟡 | Bestände werden nicht atomar ersetzt, Group Currency rechnet Kosten nur teilweise um |
| HR-KPI | 🟡 | Seite serverseitig geprüft, aber Upload, Lesefehler und Zeitraumlogik schwach |
| Forum, Projekte, Netzwerk | 🟢 | Rechte serverseitig geprüft, kleinere Mängel |
| Clean Code | 🟡 | Grosse Dateien und Methoden, Doppelungen, kein Analyzer, keine Pipeline |
| Tests | 🟡 | 1785 Tests grün, Dienste rund 70 % abgedeckt; Razor 0,6 %, keine Tests für Zugriff, JavaScript, ABAP, Skripte |

## 1. Umfang, Vorgehen, Grenzen

- **Umfang:** rund 113'000 Zeilen C# und Razor im Kern (`Services`, `Components`, `Modules`, `Models`, `Security`, `Data`,
  `Program.cs`), 5'000 Zeilen JavaScript, 159 ABAP-Dateien unter `saptasks/`, `zlo03/`, `spartenlogic/`, die Exportpakete
  Alphaplan und Sage, die Hilfsskripte unter `.tmp_sap_probe/` (217 versionierte Dateien). Nicht im Umfang: die
  generierten Übersetzungen (rund 22'000 Zeilen), `wwwroot/lib`, `Tools/`, `whisper/`.
- **Vorgehen:** zuerst eine gezählte Mustersuche (Rohtreffer in Abschnitt 4.1), dann sechs parallele Reviewer je Bereich
  (Finance/Verkauf, Einkauf/Logistik, HR, Plattform/Sicherheit, Nebenmodule, ABAP/Skripte), alle nur lesend. Danach hat die
  Hauptsitzung die hohen Befunde und die meisten mittleren selbst an der Codestelle nachgelesen.
- **Kennzeichnung:** **V** = von der Hauptsitzung am Code nachgelesen, **A** = nur Aussage des Reviewers. „Vermutet“ steht
  dort, wo die Wirkung von Laufzeit, Serverkonfiguration oder Daten abhängt.
- **Keine Zeile-für-Zeile-Prüfung.** Die grossen Dateien sind zu etwa einem Drittel gelesen, Abschnitt 7 nennt die Lücken.
- **Nichts produktiv geprüft:** kein Browser, kein SAP, keine Produktiv-DB. `Security:Enabled = false` ist für die
  versionierte Datei belegt und laut `docs/rag/LEARNINGS.md` (10.11.) auch produktiv so; ob der IIS anonyme Zugriffe
  zulässt, zeigt das Repo nicht. Davon hängt ab, ob „jeder Domänenbenutzer“ oder „jeder im Netz“ gilt.
- **Zeilennummern:** Sie gelten für `ad4f437`. Ausnahme `Program.cs`: dort stammen sie aus dem Arbeitsbaum, der ab Zeile 99
  eine fremde, nicht committete Zeile mehr hat; in `ad4f437` liegt jede Angabe ab dort um eins tiefer.
- **ABAP:** alle Aussagen gelten „laut lokaler Datei“. Ob die Datei dem aktiven Stand in T76 oder P76 entspricht, ist
  nicht geprüft.
- **Abweichung vom Auftrag:** Der ABAP-Reviewer hat trotz „nichts ausführen“ einen PowerShell-Einzeiler im Speicher
  laufen lassen (eigene Testfunktion, kein Repo-Skript, kein Netz), um die Parameterbindung in Befund A8 zu belegen.
- **Ausgeführt hat die Hauptsitzung nur** den Gesamttest mit Abdeckung im alten Review-Worktree
  (`C:\TMP\TrafagSalesExporter_review_9e27a70`, auf `ad4f437` gezogen), siehe Abschnitt 5.

## 2. Stand der 13 Befunde vom 12.11.

Die Dateien hinter den Befunden 1, 2, 6, 10 und 11 sind seit dem 12.11. unverändert (`git log`). `Routes.razor` bekam am
19.11. nur den Eintrag `finance-cockpit/mapping`.

| Nr. 12.11. | Befund | Stand 20.11. | Beleg |
| --- | --- | --- | --- |
| 1 | Source Viewer liest jede Datei unter dem Anwendungsordner | **offen** | V, `Components/Pages/SourceViewer.razor:90-104` |
| 2 | Freischalt-Cookie aus dem Hash berechenbar | **offen**, dazu neu P2 | V, `Services/AccessUnlockCookie.cs:39-43` |
| 3 | Schreibende Seiten ohne Freischaltung | **offen**, Liste erweitert in P1 | V, `Components/Routes.razor:40-62` |
| 4 | Zentrale Tabelle je Standort nicht atomar ersetzt | **offen** | V, `Services/CentralSalesRecordService.cs:128-136, 231-251` |
| 5 | Seed zählt auch inaktive Spanien-Zuordnungen | **offen und seit 18.11. 09:31 produktiv** (`c9208b5` in `8d15d48`) | V, `Services/DatabaseSeedService.cs:906-916` |
| 6 | Konfigurationsimport stellt das Kursprofil auf Tageskurs | **offen** | V, `Models/ConfigTransferPackage.cs:62` |
| 7 | Finance-Sperre vergleicht den Pfad als Text | **offen**, weiter nur abgeleitet | V (Vergleich), Umgehung vermutet |
| 8 | `async void` ohne Fehlerbehandlung im Export-Dashboard | **offen** | V, `Components/Pages/ExportDashboard.razor:480` |
| 9 | Spanien-Erbregel behandelt nur ein Symptom | nicht erneut geprüft, **seit 18.11. produktiv** | Datei seit 12.11. unverändert |
| 10 | Ungeschützter DB-Zugriff im Timer beendet die Anwendung | **offen**, *gebaut 23.11. `e76df02` (try/catch), nicht deployed* | V, `Services/TimerBackgroundService.cs:50-65, 77` |
| 11 | Finance-Regel mit leerem Vergleichswert trifft jede Zeile | nicht erneut geprüft, Datei unverändert | – |
| 12 | Einkauf-Delta entfernt gelöschte Einteilungen nie | **offen**, erweitert in E4 | A |
| 13 | „Datei öffnen“ startet einen Prozess auf dem Server | **erledigt 16.11.** (`e10ab3a`, Download in den Browser) | V, `Process.Start` nur noch in Kommentaren |

Zu Befund 3: `finance-cockpit/controlling` ist laut Kommentar in `Services/DatabaseSeedService.cs:405` bewusst offen
(„ohne Passwort wie Verkauf“) und damit kein Befund mehr, sondern ein dokumentierter Entscheid.

## 3. Neue Befunde

Schwere wie am 12.11.: **Hoch** = falsche Zahlen, Datenverlust oder Zugriff auf geschützte Daten möglich. **Mittel** =
tritt unter einer benennbaren Bedingung ein. **Niedrig** = latent oder kleiner Schaden.

**Abgrenzung zum 12.11.:** P1, P2 und E1 sind keine neuen Befunde, sondern Erweiterungen der Befunde 3 und 2 von damals;
die Zeile nennt jeweils, was bekannt war und was dazukommt. Als Fehlerklasse ebenfalls schon am 12.11. genannt (dort
Abschnitt C): leere Kostenwährung (hier F2, zweiter Teil), kulturabhängiges Parsen (F4, H5), leere Quellantwort leert den
Bestand (E9 für den Einkauf).

### 3.1 Plattform und Sicherheit

| Nr. | Schwere | Befund | Stelle | Beleg |
| --- | --- | --- | --- | --- |
| P1 | Hoch, Erweiterung von 12.11. Nr. 3 | Bekannt seit 12.11.: `AdminOnly` lässt bei `Security:Enabled = false` jeden durch; offen sind `/einkauf/verbindungen`, `/manual-imports`, `/finance-journal-import`, `/admin/menu-structure`, `/admin/nutzung`, `/marktsegmente`. **Neu:** alle 13 Routen unter `/netzwerk` (AD-Rechnerliste, Betriebssystemstände) sind ebenfalls ohne Sperre, und `/settings`, `/standorte`, `/transformations`, `/finance-rules` liegen nur hinter dem Finance-Passwort statt hinter Admin. Im Einzelnen: Bei `Security:Enabled = false` liefern `FallbackPolicy` und `AdminOnly` immer „erlaubt“. Die acht `[Authorize(AdminOnly)]`-Attribute sind wirkungslos. Ohne jede Sperre sind damit `/einkauf/verbindungen`, `/manual-imports`, `/finance-journal-import`, `/admin/menu-structure`, `/admin/nutzung`, alle 13 Routen unter `/netzwerk`, `/marktsegmente`. Nur hinter dem Finance-Passwort statt hinter Admin liegen `/settings`, `/standorte`, `/transformations`, `/finance-rules`, `/source-viewer`. | `Security/SecurityPolicyFactory.cs:9-14, 31-36`, `Components/Routes.razor:40-62` | V |
| P2 | Hoch, Erweiterung von 12.11. Nr. 2 | Bekannt seit 12.11.: Cookie aus dem Hash berechenbar, für alle gleich, ohne serverseitigen Ablauf, Hashes im Repo. **Neu** kommt dazu: „Sperren“ löscht das Cookie nicht (im Kern gibt es kein `Cookies.Delete`), es läuft serverseitig nie ab, die Endpunkte `/access/*` haben keine Drosselung, und die Hashes für HR, Finance, Admin und Operations liegen versioniert in `appsettings.json`. Ob sie produktiv noch gelten, ist offen (seit 15.10. kann ein DB-Eintrag sie ersetzen). | `Services/HrKpiAccessService.cs:78-82`, `Services/FinanceCockpitAccessService.cs:51-56`, `Program.cs:249-299` | V (Lock, Cookie), A (Endpunkte) |
| P3 | Hoch, neu | `/settings` bietet „Mit Secrets exportieren“ als Klartext-JSON und lädt Secrets in Passwortfelder; wer das Finance-Passwort kennt, bekommt die SAP- und SharePoint-Zugangsdaten. | `Components/Pages/Settings.razor:543, 668-671`, `Services/ConfigTransferService.cs:55-63, 92-93` | V |
| P4 | Mittel | Zugangsdaten liegen im Klartext in SQLite; jede Kopie oder Sicherung der Datenbank enthält sie. | `Models/SourceSystemDefinition.cs:24`, `Models/Site.cs:29`, `Models/SharePointConfig.cs:11` | A |
| P5 | Mittel | „Standard wiederherstellen“ im Menü löscht alle Menüzeilen samt den darin gespeicherten Migrationsmerkern und lässt den Seed neu laufen. | `Services/NavigationMenuService.cs:44-73`, `Services/DatabaseSeedService.cs:167-180, 321-376` | A |
| P6 | Mittel | Kein `UseExceptionHandler`, keine Sicherheits-Header, keine `ErrorBoundary`; `web.config` schaltet Detailfehler und das stdout-Protokoll ein. Der HR-Endpunkt protokolliert den eingegebenen Benutzernamen. | `Program.cs:238-247`, `web.config:8, 11, 15`, `Program.cs:282-295` | V (web.config, fehlende Middleware) |
| P7 | Mittel | Der Seed setzt die UK-Zuordnung bei jedem Start auf den Codestand zurück; eine in der Oberfläche geänderte Zuordnung ist nach dem nächsten Deploy still wieder alt. | `Services/DatabaseSeedService.cs:1096-1143` | A |
| P8 | Mittel | `RebuildTable` lässt bei einem Fehler die Fremdschlüsselprüfung ausgeschaltet (kein `finally`). | `Services/DatabaseSchemaMaintenanceService.cs:1118-1165` | A |
| P9 | Hinweis | Beide Projekte zielen auf `net8.0`. Nach meinem Wissensstand endete der Support am 10.11.2026; bitte gegen die Microsoft-Lebenszyklusseite prüfen. MudBlazor 7.15.0 liegt eine Hauptversion zurück. Bekannte Lücken: `dotnet list package --vulnerable --include-transitive` laufen lassen. | `TrafagSalesExporter.csproj:3, 39` | V (Versionen) |

Kein Befund nach Prüfung: Die Entwicklungs-Anmeldung wird nur mit Umgebung Development und gesetztem `DevelopmentBypass`
aktiv (`Program.cs:27`). Das interpolierte SQL der Schemapflege enthält nur Konstanten. Der Konfigurationsimport
deserialisiert in feste Typen, läuft in einer Transaktion und ist auf 5 MB begrenzt. Klartext-Passwörter in versionierten
Skripten wurden nicht gefunden.

### 3.2 Einkauf und Logistik

| Nr. | Schwere | Befund | Stelle | Beleg |
| --- | --- | --- | --- | --- |
| E1 | Hoch, Erweiterung von 12.11. Nr. 3 | Bekannt seit 12.11.: `/einkauf/verbindungen` hat keine Admin-Prüfung, jeder kann Verbindung und `PasswordOverride` ändern und Läufe starten. **Neu:** Der Verbindungstest nimmt die Adresse aus dem Formular und schickt bei leerem Override den zentralen SAP-Benutzer samt Passwort als Basic-Auth dorthin. „Speichern“ lenkt alle weiteren Einkauf-Läufe und Logistik live auf diese Adresse. | `Components/Pages/PurchasingDataSources.razor:1`, `Services/PurchasingDataSourcePageService.cs:99-116` | V |
| E2 | Mittel *(gebaut 23.11. `e76df02`, nicht deployed)* | Der planmässige Nachtlauf ruft `RunDeltaAsync` direkt auf, nur der Nachhol-Lauf geht über die Laufsperre `TryStart`. Der Dienst selbst hat keine Sperre, ein zweiter Lauf (Knopf oder Nachhol-Lauf) ist möglich; der Full Load löscht dann den Cache, während das Delta schreibt. | `Services/TimerBackgroundService.cs:188, 235` (Stand HEAD), `Services/PurchasingRefreshRunner.cs:92-102` | V |
| E3 | Mittel | Ein Fehlerzustand des Dashboards wird wie ein gültiger Stand 60 Minuten im Snapshot-Cache gehalten. | `Services/PurchasingDashboardService.cs:288-291`, `Services/PurchasingDashboardSnapshotCache.cs:72-73, 155` | V (catch), A (Cache) |
| E4 | Mittel *(berichtigt 23.11.: in Wahrheit lädt das Delta ALLE Belege, weil `EKKOSet` den Filter ignoriert, 90 Minuten je Lauf; echtes Delta gebaut `e76df02`, nicht deployed; das Löschen bleibt offen)* | Das Delta lädt nur Belege mit `EKKO.Aedat >= Stichtag` und noch offene Belege nach und löscht nie. Nachträgliche Änderungen an voll gelieferten Bestellungen kommen erst mit dem nächsten Full Load. Was SAP in `Aedat` schreibt, ist in T76 zu prüfen. | `Services/PurchasingDataRefreshService.cs:382-396, 420-422` | A |
| E5 | Mittel *(gebaut 23.11. `e76df02`, nicht deployed)* | Der Delta-Stichtag springt nach einem gescheiterten oder durch Deploy abgebrochenen Lauf nach vorn, weil die jüngste Statuszeile ungeachtet ihres Status gilt. | `Services/PurchasingDataRefreshService.cs:374, 1170-1177` | V (Zeile 374), A (Rest) |
| E6 | Mittel | Handbestellwesen liest „1.234,50“ als 1.2345. Die Bestellung liegt damit unter jedem Freigabelimit. Dieselbe Logik im Import. Die neue Maske (`MudNumericField`) ist nicht betroffen. | `Modules/Handbestellwesen/HandbestellwesenPage.razor:956-957`, `Modules/Handbestellwesen/HbImport.cs:390-391` | V |
| E7 | Mittel | Ein auf ein Material gefilterter Stücklisten-Load löscht den ganzen Bestand der Richtung. Dasselbe Muster stand hinter dem LEARNINGS-Fall vom 08.10. | `Services/MaterialUsageDataRefreshService.cs:122-124` | V |
| E8 | Mittel bis niedrig | Löschen und Wareneingang gelingen bei leerem Kurzzeichen (Konto ohne Mitarbeitereintrag gegen importierte Altbestellung ohne Besteller) ohne Berechtigung. | `Modules/Handbestellwesen/HbService.cs:307, 317` | A |
| E9 | Niedrig | Leere SAP-Antworten ersetzen den Bestand ohne Plausibilitätsprüfung (Full Load, Materialanteil). | `Services/PurchasingDataRefreshService.cs:338-345`, `Services/PurchasingMaterialShareLoader.cs:137-139, 174` | V (Löschen), A |
| E10 | Niedrig | Die Stücklistenanalyse blättert ohne Notbremse; ignoriert das Set `$top`/`$skip`, endet die Schleife nie. | `Services/MaterialUsageDataRefreshService.cs:383-403` | A |
| E11 | Niedrig | Statuswechsel im Handbestellwesen ohne Versionsfeld; entzogene Rechte gelten, bis die Seite neu lädt. | `Modules/Handbestellwesen/HbService.cs:324-331` | A |
| E12 | Niedrig | `Meins` steht in keiner `$select`-Liste, die Spalte `PurchasingEkpoCache.Meins` ist immer leer. | `Services/PurchasingDataRefreshService.cs:324, 403, 617` | A |

Offene Fachfragen (keine Befunde): Darf ein Admin die eigene Bestellung freigeben (`HbService.cs:296`)? Ist `Netwr1` aus
ZC23 schon in CHF? Berücksichtigt die CHF-Umrechnung Kurseinheiten wie 100 JPY?

### 3.3 Finance und Verkauf

| Nr. | Schwere | Befund | Stelle | Beleg |
| --- | --- | --- | --- | --- |
| F1 | Mittel | Die Konzernkosten der Trafag AG werden ohne Transaktion und nur mit dem Schutz „nicht leer“ ersetzt. Scheitert das Speichern oder liefert das Gateway nur einen Teil, fällt die Konzernkostenregel still auf den lokalen Standardpreis zurück; die Marge steht dann auf dem IC-Preis mit Status OK. Berührt ISS-007.3. | `Services/DataSources/SapGatewayDataSourceAdapter.cs:207-221` | V |
| F2 | Mittel | Die Group-Currency-Ansicht rechnet nur `StandardCost` nach CHF um. `StandardCostVariable` bleibt lokal und gilt danach als CHF; bei leerer `StandardCostCurrency` wird gar nicht umgerechnet. Beispiel TRIN: Deckungsbeitrag −59'050 statt 380 CHF. Ob solche Zeilen vorkommen, ist ungeprüft. | `Services/ManagementCockpitService.cs:502-527, 1562-1565` | V (Umrechnung), A (Folge) |
| F3 | Mittel | Der Berichtsmonat der Monthly Review hängt am jüngsten Beleg aller Gesellschaften zusammen; eine nachhinkende Gesellschaft zeigt einen halben Monat als Monatswert. | `Services/Sales/SalesAnalytics.cs:52-58` | A |
| F4 | Mittel bis niedrig | Zahlen und Daten werden an mehreren Stellen ohne feste Kultur oder in riskanter Reihenfolge gelesen („12,5“ wird 125). Die App setzt nirgends eine Kultur. Als Fehlerklasse bekannt, weiter offen. | `Services/RecordTransformationService.cs:67-85`, `Services/MappedSalesRecordComposer.cs:264-304`, `Services/ManualExcelImportService.cs:954-967` | A |
| F5 | Niedrig | Der HANA-Abruf wiederholt bei jeder Ausnahme ohne Wareneingang; nach einem Timeout fehlt für diesen Lauf die Lieferantenherkunft, sichtbar nur als Warnung. | `Services/HanaQueryService.cs:65-74` | A |
| F6 | Niedrig | Bei „Kostenwährung abweichend“ läuft die Kostenbasis unumgerechnet in die Summen und in die Kachel „Bekannte Kostenbasis“. Die Marge selbst ist korrekt maskiert. | `Services/ManagementCockpitService.cs:1554-1622` | A |
| F7 | Niedrig | Der Verkaufs-Datendienst verschluckt Ladefehler ohne Protokoll; die Seiten zeigen dann dauerhaft den alten Stand. | `Services/Sales/SalesDataService.cs:68-71` | A |

### 3.4 HR-KPI

Der HR-Zugriff wird serverseitig in der Seite geprüft (`Components/Pages/HrKpi.razor:19, 321, 333, 393`), Download-
Endpunkte für HR-Daten gibt es nicht. Schwach ist das Cookie (P2), nicht die Seite.

| Nr. | Schwere | Befund | Stelle | Beleg |
| --- | --- | --- | --- | --- |
| H1 | Mittel | Das Management-Cockpit rechnet die HR-Ampel aus `C:\temp` statt aus `hrdata`, weil `HrKpiOptions.DataFolder` einen festen Vorgabewert hat und der Aufrufer ihn nicht setzt. Dazu: die HR-Ampelwerte sind dort mit dem Finance-Passwort sichtbar. | `Models/HrKpiModels.cs:5, 23`, `Services/ManagementCockpitPageService.cs:268-272` | V |
| H2 | Mittel | Der Upload überschreibt die produktive Datei direkt, ohne Zwischendatei, Sicherung und Inhaltsprüfung. Der Hub-Weg hat alle drei. | `Components/Pages/HrKpi.razor:415-418` | V |
| H3 | Mittel | Eine unlesbare Datei ergibt eine leere Liste und damit Nullwerte bei grüner Ampel. | `Services/HrKpi/HrKpiDashboardBuilder.cs:2764-2795` | V |
| H4 | Mittel | Der „Exporttag“ der Rexx-Summen ist die Dateizeit; ein Export vom 28.12., hochgeladen am 05.01., zählt die Vorjahressummen ins neue Jahr. | `Services/HrKpi/HrKpiDashboardBuilder.cs:64, 80, 211-212, 728-731` | A |
| H5 | Mittel | Der Excel-Leser hängt von der Serverkultur ab (LEARNINGS 10.11., weiter offen). | `Services/HrKpi/HrKpiDashboardBuilder.cs:2602-2623` | A |
| H6 | Mittel | Fluktuation: bei „Bis“ in der Zukunft entfällt die Hochrechnung, bei „nur Von“ in einem Vorjahr hat der Austrittsfilter keine Obergrenze. | `Services/HrKpi/HrKpiDashboardBuilder.cs:2174-2177, 2222-2223` | A |
| H7 | Mittel bis niedrig | Doppelte Personalnummer im Handexport zählt das Pensum doppelt (FTE 1,6 statt 0,8). | `Services/HrKpi/HrKpiDashboardBuilder.cs:65-69, 396-401` | A |
| H8 | Niedrig | Die letzte Auswahl ist global für alle Benutzer und speichert Suchtext und Datenordner. | `Components/Pages/HrKpi.razor:608-622` | A |
| H9 | Niedrig | Sicherungskopien der Personaldatei ohne Obergrenze und Löschfrist im Webordner. | `Services/HrKpi/RexxHubRefreshService.cs:155-157, 213-225` | A |

Offene Frage an HR: Die Restferien-Ampel wertet ab dem zweiten Quartal jeden Resttag als rot
(`HrKpiDashboardBuilder.cs:2378-2381`); gemeint ist vermutlich der Übertrag aus dem Vorjahr.

### 3.5 Shopfloor, Forum, Projekte, Netzwerk

| Nr. | Schwere | Befund | Stelle | Beleg |
| --- | --- | --- | --- | --- |
| S1 | Hoch, neu | Gespeichertes XSS: `POST /shopfloor/api/zd05/week` übernimmt `kw` ohne Formatprüfung, die Anzeige setzt den Wert ohne `esc()` in SVG-Markup. Ein Shopfloor-Benutzer kann Skript bei allen ausführen, die die ZD05-Ansicht öffnen. | `Services/Shopfloor/ShopfloorZd05.cs:312-331`, `wwwroot/shopfloor/app.js:1472` | V |
| S2 | Mittel | Der Query-Parameter `matex` geht ohne Verdopplung des Hochkommas in den OData-Filter. | `Services/Shopfloor/ShopfloorEndpoints.cs:198`, `Services/Shopfloor/ShopfloorSapMengen.cs:77` | V |
| S3 | Mittel, bedingt hoch | `OpenForAll` (Entscheid 16.10.) lässt im Code auch nicht angemeldete Aufrufer durch, und die Sperre ist der einzige Schutz der statischen Dateien unter `/shopfloor`. Dort liegt versioniert `kpi/seed_liefertreue_2026-09.json` (454 KB, laut Reviewer mit Kundennamen; von der Hauptsitzung nicht geöffnet). Ob anonyme Anfragen die App erreichen, hängt vom IIS ab. | `Services/Shopfloor/ShopfloorOptions.cs:168-171`, `Program.cs:243-245`, `appsettings.json:133` | V |
| S4 | Mittel | Keine Rollentrennung im Shopfloor; die manuellen SAP-POSTs prüfen die Einschalter nicht, `DeleteExclusion` löscht ohne Benutzer und Verlauf. | `Services/Shopfloor/ShopfloorEndpoints.cs:377-394, 455-578` | A |
| S5 | Mittel | Die SAP-Vorschauen per GET sind ungedrosselt; jeder Aufruf fragt SAP mit dem technischen Benutzer ab. | `Services/Shopfloor/ShopfloorEndpoints.cs:454-476` | A |
| S6 | Mittel | Trafag Projekte überschreibt gleichzeitige Änderungen ohne Hinweis (kein Versionsfeld). | `Services/Projects/PmService.cs:372-400` | A |
| S7 | Niedrig bis mittel | Die Liefertreue-Seite bettet SAP-Texte mit `UnsafeRelaxedJsonEscaping` in ein Inline-Skript ein. | `Services/Shopfloor/KpiLiefertreue.cs:35` | A |
| S8 | Niedrig bis mittel | Steuerzeichen im Beitragstext werfen im Forum-Markdown eine Ausnahme; betrifft auch Trafag Projekte. | `Services/Forum/ForumMarkdown.cs:149-150, 175-176` | A |
| S9 | Niedrig | Push-Schnittstelle prüft das Datum nicht; rohe Ausnahmetexte gehen an den Client; CSV-Export ohne Formelschutz; freie Überwachungsziele und AD-Abfragen ohne Zeitlimit im Netzwerk. | `Services/Shopfloor/ShopfloorStore.cs:617, 366`, `Services/Network/NetworkStore.cs:167-181` | A |

Kein Befund: keine LDAP-Injection, LAPS-Passwörter werden nicht gelesen, kein interpoliertes SQL im Shopfloor, Forum
prüft Bearbeiten und Löschen serverseitig, der Verfasser kommt aus der Anmeldung.

### 3.6 ABAP und Exportskripte (laut lokaler Datei)

DPC steht für `saptasks/shopfloor_sap/DPC_EXT_GET_ENTITYSET_neu_2026-11-13_layout.abap`.

| Nr. | Schwere | Befund | Stelle | Beleg |
| --- | --- | --- | --- | --- |
| A1 | Hoch, neu | In der Gateway-Methode steht kein `AUTHORITY-CHECK`. `HrKpiSet` und `HrAbsenzSet` lesen PA0001, PA0002, PA0007 und PA2001 per direktem SELECT, ohne Filter auf Buchungskreis oder Personalbereich. Wer den Service aufrufen darf, bekommt die Abwesenheiten aller Mitarbeitenden. Wer das ausser dem technischen Benutzer ist, zeigt das Repo nicht. | DPC:107-125, 240, 2246 | V |
| A2 | Hoch, neu als Befund | `FinanzJournalSet` hat keinen Pflichtfilter und keine Buchungskreis-Prüfung. Der 500er ohne Filter ist seit dem 01.10. bekannt (LEARNINGS), der Schutz fehlt weiter. Die Muster dafür gibt es bei `Zc23Set` und `CoPaSumSet`. | DPC:2687-2694 | V |
| A3 | Hoch, neu | Die P76-Lesewerkzeuge sind nur per Kommentar „nur lesen“. `SapGuiP76Hilfe.vbs` nimmt jeden OK-Code, Knopf, jedes Feld und jede Funktionstaste an. | `.tmp_sap_probe/SapGuiP76Hilfe.vbs:89-106`, `.tmp_sap_probe/P76Lesen.vbs:76-86` | V |
| A4 | Mittel | Fünf schreibfähige VBS nehmen die erste SAP-Verbindung ohne Vergleich auf T76; `SapProbe` schützt Schreiben nur mit `--confirm-write`. | `.tmp_sap_probe/SapGuiOkCode.vbs:26-33`, `.tmp_sap_probe/Program.cs:634-695` | A |
| A5 | Mittel | `ZPOWERBI_KDAUF` wird ohne WHERE gelöscht und abhängig von der Selektion neu gefüllt; ein Lauf mit einem Kunden und angekreuztem Feld lässt nur diesen Kunden übrig. | `saptasks/zm_offene_kdauf/ZM_OFFENE_KDAUF_produktiv.abap:1194` | V |
| A6 | Mittel | Blättern erst nach dem Volllesen, Reportlauf je Seite, SELECT in Schleifen; das Finanzjournal wird bei 30 Seiten 30-mal ganz gelesen. | DPC:188-199, 2870-2876, 495-569 | A |
| A7 | Mittel | Stille Kappung mit `UP TO n ROWS` ohne Sortierung und ohne Meldung (20000 Kontraktpositionen und weitere). | DPC:346, 490, 898, 1301, 1379, 1457, 1972 | A |
| A8 | Mittel | `Run-SpainExportAndUpload.ps1` übergibt benannte Parameter als Array und bindet sie damit nach Position; das Skript kann so nicht laufen. Welches Skript eingeplant ist, zeigt das Repo nicht. Zeitfenster uneinheitlich (7 gegen 35 Tage). | `SageSpainExportPackage/SageSpainFinalExportPackage/Run-SpainExportAndUpload.ps1:47-63` | V |
| A9 | Mittel | Sage-Export: Filter ohne Bedingung „fakturiert“, Gutschriften pauschal negativ, Vorzeichen bei MwSt und Brutto nicht gedreht. | `SageSpainExportPackage/SageSpainFinalExportPackage/Export-SageSpainSalesCsv.ps1:263-311` | A |
| A10 | Mittel | Alphaplan: das eigentliche Delta-Skript `alphaplandeltaexport.ps1` ist nicht versioniert; festes 7-Tage-Fenster ohne Merker, Schlussprüfung wird nicht ausgewertet. | `AlphaplanExportPackage/scripte/runAlphaplanDailyDelta.ps1:25-126` | A |
| A11 | Mittel bis niedrig | ZZPRDAT schreibt AUFK direkt, ohne Sperre und ohne Auswertung von `sy-subrc`. | `saptasks/zzprdat/produktiv/Z_ZZPRDAT_SET.abap:37-40` | A |
| A12 | Niedrig | Division ohne Nullprüfung, benutzerabhängige Formate und fester Pfad in Altreports. | `saptasks/zc23_materialanteil/ZLO_CALC_CZ_PREISE.abap:135-159` | A |

Kein Befund: kein dynamisches WHERE aus `$filter`, kein `CLIENT SPECIFIED`, kein `COMMIT WORK` in der Gateway-Methode,
`FOR ALL ENTRIES` überall gegen leere Treibertabellen geschützt.

## 4. Clean Code

### 4.1 Messwerte

Gemessen mit `git grep`, `wc` und einer Klammerzählung per `awk` auf dem Stand `ad4f437`. Die Methodenlängen sind eine
Heuristik (Zeilen zwischen Signatur und schliessender Klammer); Methoden mit Klammern in Zeichenketten zählt sie falsch,
ein solcher Ausreisser ist entfernt.

| Messgrösse | Wert |
| --- | --- |
| Produktivdateien C# und Razor im Kern (ohne Übersetzungen) | 466 Dateien, rund 113'000 Zeilen, Mittel 243 Zeilen |
| Dateien über 300 / 500 / 1000 / 2000 Zeilen | 108 / 54 / 15 / 6 |
| Methoden in C# (Heuristik) | rund 2'040, davon über 50 Zeilen 213, über 100 Zeilen 42, über 200 Zeilen 9 |
| Längste Methoden | `ManagementCockpitService.AnalyzeFinanceSummary` 368 Zeilen, `ConfigTransferService.ImportJsonAsync` 356, `PurchasingDashboardService.TryLoadCacheStateAsync` 353, `DatabaseSeedService.EnsureSapODataDachMapping` 349, `HrKpiDashboardBuilder.Build` 328 |
| Pauschale `catch` ohne Typ oder mit `Exception` | 163 Stellen |
| Statische Sammlungen | 171 (Stichprobe: fast nur unveränderliche Nachschlagetabellen) |
| `CommandText` mit Zeichenketten-Interpolation | 33 (alle geprüften: Konstanten, Datumswerte oder Parameter) |
| `async void` | 1 (Befund 12.11. Nr. 8) |
| `new HttpClient(...)` statt `IHttpClientFactory` | 4 im Kern, weitere im Shopfloor und Netzwerk |
| `TODO`/`FIXME`/`HACK` | 1 |
| `Nullable` | eingeschaltet in beiden Projekten |
| `.editorconfig`, Analyzer, `TreatWarningsAsErrors` | nicht vorhanden |
| Pipeline | `.github/workflows` ist leer; Tests laufen nur von Hand vor dem Deploy im Release-Worktree |

### 4.2 Bewertung

**Was gut ist.** Die Kommentare erklären das Warum mit Datum und Anlass statt das Was. SQL ist durchgehend parametrisiert
oder aus Konstanten gebaut. Abhängigkeiten laufen über Schnittstellen und Dependency Injection. `Nullable` ist an, es gibt
praktisch keine liegengebliebenen TODO. Fachregeln sind an vielen Stellen als reine Funktionen gebaut und damit testbar
(`GroupMarginCalculator`, `TimerSchedule`, `PmSchedule`).

**Was nicht Clean Code ist.**

1. **Grösse.** Sechs Dateien über 2000 Zeilen: `ManagementCockpit.razor` (3464), `PurchasingDashboard.razor` (3342),
   `ManagementCockpitService.cs` (3078), `HrKpiDashboardBuilder.cs` (2801), `PurchasingDashboardService.cs` (2404),
   `ExcelExportService.cs` (2127). `PurchasingDashboard.razor` bedient 16 Routen in einer Komponente.
2. **Mehrere Verantwortungen je Klasse.** `ManagementCockpitService` enthält acht trennbare Blöcke (Datei-Analyse,
   zentrale Auswertung, Finance Summary, Heartbeat, Datenstatus, Gruppenmarge, Prüfbuch, Pivot). In SAP bedient eine
   einzige Methode rund 20 EntitySets aus HR, Finanzen, CO-PA, QM, Logistik und Einkauf.
3. **Doppelte Regeln.** Die CHF-Regel steht an sechs Stellen (`PurchasingDashboardService.cs:193-199`,
   `PurchasingInteractiveService.cs:20-22`, `SupplyChainAnalysisService.cs:485, 523, 567`,
   `PurchasingContractCalculator.cs:106`). Die Zugriffsdienste für Admin, Finance und HR sind fast gleich, `Program.cs`
   enthält eine vierte Fassung. Die Zeitraumlogik steht im HR-Builder dreimal mit verschiedener Kappung (daraus Befund
   H6). Das Sage-SQL existiert dreifach. `FinanceAggregationRow` wird zweimal projiziert.
4. **Fachlogik in der Oberfläche.** Der Betragsparser des Handbestellwesens (E6), die Finance-Sperre (`Routes.razor`) und
   die Regeln, wann eine Marge „-“ ist (`ManagementCockpit.razor:3030-3080`), stehen in Razor-Dateien und sind dort nicht
   testbar.
5. **Verborgener Zustand.** `FinanceRuleEngine.ShouldInclude` trägt beim Prüfen in ein Set ein; die Regel „neue Instanz je
   Lauf, ein Aufruf je Zeile“ wird an neun Aufrufstellen nur durch Disziplin gehalten. Drei Zwischenspeicher im Einkauf
   haben verschiedene Entwertung (Lauf, 30 Minuten, Zeitstempel).
6. **Fehlerbehandlung.** 163 pauschale `catch`. Mehrere davon machen aus einem Fehler eine leere Liste oder einen Text
   und damit falsche Zahlen statt einer Meldung (H3, F7, E3).
7. **Toter Code.** `Unlock()` und `UnlockHrKpiAsync()` der Freischaltfelder, `CreateClient`/`ReadRowsAsync`/`ReadCountAsync`
   in `PurchasingDashboardService.cs:1026-1063`, der Zweig `preferredImportYear`.
8. **Handgeschriebene Schemapflege** statt Migrationen, mit Einmal-Merkern als unsichtbare Menüzeilen (P5).
9. **Ablage im Repo.** 32 Vollkopien der Gateway-Methode in sechs Ordnern, 13 ZLO03-Stände, 217 versionierte Dateien unter
   `.tmp_sap_probe`, CSV-Daten und ZIP neben den Skripten. Es gibt keinen eindeutigen aktuellen Stand je SAP-Objekt.
10. **Sprache.** Bezeichner mischen Deutsch und Englisch je nach Modul (`HbService.FreigeberAsync` gegen
    `PurchasingRefreshRunner.TryStart`). Das ist konsistent je Modul und kein Fehler, erschwert aber die Suche.

## 5. Tests und TDD

### 5.1 Messwerte

| Messgrösse | Wert |
| --- | --- |
| Testlauf Stand `ad4f437` im Review-Worktree (20.11. 14:20) | **1785 von 1785 grün**, 0 übersprungen, Dauer 1 Minute 14 Sekunden |
| Zeilenabdeckung laut coverlet, roh | 61,5 % (59'870 von 97'365 Zeilen), Zweige 40,8 % |
| Zeilenabdeckung ohne generierte Übersetzungen | **50 %** (37'727 von 75'222); die 22'143 Übersetzungszeilen zählen roh mit 100 % mit |
| Zeilenabdeckung ohne Übersetzungen und ohne Razor-Komponenten | **rund 70 %** (C#-Dienste, Modelle, Module) |
| Testdateien / Testzeilen | 139 / rund 32'100 (Verhältnis zu Produktivzeilen 0,28) |
| Testmethoden | 1'223 `[Fact]`, 107 `[Theory]` mit 550 `[InlineData]` |
| `Assert`-Aufrufe | 5'598, im Mittel 4,2 je Testmethode; keine Testdatei mit weniger Asserts als Tests |
| Übersprungene Tests (`Skip`) | 0 |
| Werkzeuge | xUnit, coverlet; keine Mocking-Bibliothek, kein bUnit, 45 Testdateien mit SQLite im Speicher |
| Tests mit `DateTime.Now`/`UtcNow` | 63 Stellen in 18 Dateien |
| Tests, die die echte `appsettings.json` lesen | 9 Stellen in 6 Dateien (Fall in LEARNINGS 16.10.) |
| JavaScript | 11 Dateien, rund 5'000 Zeilen, 0 Tests |
| Razor | 163 Dateien, 0 Komponententests |
| ABAP, PowerShell, VBS | 0 Tests |

**Abdeckung je Bereich** (coverlet, Zeilen):

| Bereich | Zeilen | Abdeckung |
| --- | --- | --- |
| HR (`Services/HrKpi`) | 4'049 | 91 % |
| Projekte | 3'140 | 89 % |
| Models | 1'599 | 84 % |
| Verkauf | 1'209 | 83 % |
| Forum | 890 | 81 % |
| Finance und Export | 10'415 | 80 % |
| Shopfloor | 4'342 | 80 % |
| Plattform (Datenbank, Timer, Konfiguration, Texte) | 5'959 | 79 % |
| Einkauf | 5'413 | 75 % |
| Security (Richtlinien) | 71 | 70 % |
| Zugriff und Menü | 646 | 57 % |
| Logistik | 909 | 55 % |
| übrige Dienste | 4'977 | 50 % |
| DataSources | 695 | 45 % |
| Handbestellwesen (mit Razor-Seiten) | 3'378 | 35 % |
| Weltlage | 762 | 33 % |
| Netzwerk | 1'814 | 32 % |
| Lieferantenbewertung (mit Razor-Seite) | 969 | 28 % |
| **Razor-Komponenten** | 21'648 | **0,6 %** |
| `Program.cs` | 273 | 0 % |

**Abdeckung an den Befundstellen:**

| Datei | Abdeckung | Bezug |
| --- | --- | --- |
| `Services/TimerBackgroundService.cs` | 0 % (0 von 222) | E2, 12.11. Nr. 10 |
| `Services/PurchasingDataSourcePageService.cs` | 0 % (0 von 188) | E1 |
| `Services/HrKpiAccessService.cs` | 0 % (0 von 94) | P2 |
| `Services/PurchasingDataRefreshService.cs` | 37 % (293 von 801) | E4, E5, E9 |
| `Services/ConfigTransferService.cs` | 68 % | P3, 12.11. Nr. 6 |
| `Services/Shopfloor/ShopfloorZd05.cs` | 79 % | S1 |
| `Services/ManagementCockpitService.cs` | 82 % | F2, F6 |
| `Services/CentralSalesRecordService.cs` | 96 % | 12.11. Nr. 4 |
| `Services/GroupMarginCalculator.cs` | 98 % | – |
| `Services/AccessUnlockCookie.cs` | 100 % | 12.11. Nr. 2 |
| `Security/SecurityPolicyFactory.cs` | 100 % | P1 |

Die letzten vier Zeilen zeigen die Grenze der Kennzahl: Der Code läuft in Tests vollständig durch, und der Befund besteht
trotzdem, weil die Tests das heutige Verhalten festschreiben (Cookie aus dem Hash, „abgeschaltet heisst erlaubt“) oder
den Fehlerpfad nicht auslösen (Abbruch mitten im Batch).

**Tests im selben Commit wie der Code** (letzte 600 Commits ohne Merges seit 01.09., 141 davon mit Produktivcode):

| Bereich | Commits mit Code | davon mit Tests | Anteil |
| --- | --- | --- | --- |
| Gesamt | 141 | 109 | 77 % |
| Shopfloor | 30 | 29 | 97 % |
| HR | 16 | 14 | 88 % |
| Handbestellwesen | 14 | 12 | 86 % |
| Einkauf | 28 | 19 | 68 % |
| Razor-Komponenten | 74 | 48 | 65 % |
| Projekte | 18 | 11 | 61 % |

**Produktivdateien, deren Klassenname in keinem Test vorkommt:** Netzwerk 9 von 13, DataSources 5 von 10,
Handbestellwesen 4 von 7, Kern-Services 22 von 108, Forum 2 von 5; HR, Security und Einkauf-Unterordner 0.

### 5.2 Bewertung

**Ist das TDD?** Belegen lässt sich, dass Tests zusammen mit dem Code entstehen: gut drei Viertel der Code-Commits
bringen Tests mit, im Shopfloor fast alle. Ob der Test vor dem Code geschrieben wurde, zeigt die Historie nicht, weil
beide im selben Commit liegen. Die Regel aus `persona.md` („kritische Berechnungen mit Tests absichern“) ist für die
Rechenlogik weitgehend erfüllt. Die Suite läuft vor jedem Deploy im Release-Worktree und ist dort durchgehend grün
dokumentiert (20.11. 13:23: 1785/1785).

**Was fehlt, und warum es zu den Befunden passt.** Die hohen Befunde liegen entweder in Code ohne Tests oder in Code,
dessen Tests das heutige Verhalten festschreiben:

1. **Zugriffsschutz.** Kein Test prüft, welche Route wie geschützt ist. `SecurityPolicyFactoryTests` deckt nur die
   Richtlinie selbst ab; `RequiresFinanceUnlock` liegt in einer Razor-Datei, `HrKpiAccessService` hat 0 % Abdeckung, und
   „Sperren“ wird nirgends geprüft. Ein Test, der alle `@page`-Routen gegen eine Soll-Liste prüft, hätte die offenen
   Seiten gefunden.
2. **Oberfläche.** Razor-Komponenten haben 0,6 % Abdeckung bei 21'648 Zeilen, und dort liegt Fachlogik (Abschnitt 4.2
   Punkt 4). E6 wäre mit einem einzigen Testfall „1.234,50“ aufgefallen.
3. **JavaScript.** 5'000 Zeilen ohne Test; das XSS (S1) liegt genau dort.
4. **Lauf-Orchestrierung.** Kein Test ruft `RunDeltaAsync` oder `RunFullLoadAsync` auf, weil das fest eingebaute
   `new HttpClient` keinen Test-Handler zulässt (`PurchasingDataRefreshService.cs:494-501`). Damit sind E2, E4, E5 und E9
   ungetestet.
5. **Fehlerpfade.** Abbruch mitten im Batch (12.11. Nr. 4), Teilantwort (F1), unlesbare Datei (H3), Fehlerzustand im
   Cache (E3): jeweils kein Test.
6. **Shopfloor-Zugriff.** `ShopfloorAccessTests` hat keinen Fall für `OpenForAll` ohne Anmeldung (S3), die Endpunkttests
   keinen für Maskierung von `matex` oder das Format von `kw`.
7. **ABAP und Skripte.** Nicht automatisch geprüft; A8 (Skript bindet Parameter falsch) wäre beim ersten Probelauf
   aufgefallen.

**Risiken in der Suite selbst.** 18 Testdateien hängen an der Systemzeit; am 17.11. ist ein „einmaliger roter Test“
dokumentiert, dessen Name nicht festgehalten wurde. Sechs Testdateien lesen die echte `appsettings.json`, eine
Konfigurationsänderung kann damit Tests kippen (LEARNINGS 16.10.). Es gibt keine Pipeline; ob die Tests laufen, hängt
davon ab, dass die Sitzung es tut.

## 6. Doings

Reihenfolge nach Risiko. Vor jeder Umsetzung gilt „erst Plan, dann ausführen“, Tests in `TrafagSalesExporter.Tests`,
Deploy nur mit dem Werkzeug.

### A. Sofort, ohne Entscheid nötig

| Nr. | Was | Befund | Aufwand |
| --- | --- | --- | --- |
| D1 | `/einkauf/verbindungen` an die Admin-Freischaltung binden, Adresse auf erlaubte Hosts beschränken | E1 | klein |
| D2 | `/manual-imports`, `/finance-journal-import`, `/admin/menu-structure`, `/admin/nutzung`, `/settings`, `/standorte`, `/transformations`, `/finance-rules` in der Seite selbst prüfen wie `AdminPasswords.razor` | P1, P3, 12.11. Nr. 3 | klein |
| D3 | `/source-viewer` auf Quelltext-Endungen beschränken und hinter Admin | 12.11. Nr. 1 | klein |
| D4 | Shopfloor: `kw` serverseitig auf das Wochenformat prüfen und in `app.js` maskieren; Hochkomma in `matex` verdoppeln | S1, S2 | klein |
| D5 | Timer: `RecalculateNextRunAsync` und `HandleStatusChanged` mit try/catch; Nachtlauf über `TryStart` | 12.11. Nr. 8 und 10, E2 | klein |
| D6 | Betrag „1.234,50“ richtig lesen oder ablehnen; Parser aus der Razor-Datei in eine getestete Klasse | E6 | klein |
| D7 | HR-Ampel im Management-Cockpit aus `hrdata`; Vorgabewert `C:\temp` entfernen | H1 | klein |
| D8 | Löschen und Schreiben in einer Transaktion mit Mindestmenge: Konzernkosten, Zentraltabelle, Stücklisten-Cache | F1, 12.11. Nr. 4, E7 | klein bis mittel |
| D9 | Seed: nur aktive Spanien-Zuordnungen zählen; Kursprofil-Vorgabe im Konfigurationsimport auf Budget | 12.11. Nr. 5 und 6 | klein |

### B. Braucht einen Entscheid von Ingo

| Nr. | Frage | Befund |
| --- | --- | --- |
| B1 | Soll `Security:Enabled` wieder an (Windows-Gruppen), oder bleibt es bei Bereichspasswörtern? Stoff für das Berechtigungskonzept, Wochen_Todo 147. | P1 |
| B2 | Sollen `/netzwerk/*` und `/marktsegmente` offen bleiben? | P1 |
| B3 | Heisst `OpenForAll` „alle“ oder „alle Angemeldeten“, und soll die Seed-Datei mit Kundennamen aus `wwwroot`? Braucht der Shopfloor Rollen (lesen, schreiben, SAP-Abruf)? | S3, S4 |
| B4 | Dürfen HR-Ampelwerte im Management-Cockpit mit dem Finance-Passwort sichtbar sein? | H1 |
| B5 | Gilt der Berichtsmonat der Monthly Review je Gesellschaft oder gruppenweit? | F3 |
| B6 | Wer darf den Service `ZPOWERBI_EINKAUF_SRV` aufrufen, und sollen die HR-Sets eine Berechtigungsprüfung bekommen oder in einen eigenen Service? | A1 |
| B7 | Die vermutete Umgehung der Finance-Sperre per prozentkodierter URL einmal lesend im Arbeits-Chrome nachstellen? | 12.11. Nr. 7 |

### C. Danach (zwei bis vier Wochen)

| Nr. | Was | Befund |
| --- | --- | --- |
| D10 | Cookie mit Servergeheimnis und Ablauf (Data Protection), echtes Abmelden, Drosselung der `/access/*`-Endpunkte, Hash mit Salt, Hashes aus dem Repo, danach Passwörter neu setzen | P2, 12.11. Nr. 2 |
| D11 | Group Currency: `StandardCostVariable` und leere Kostenwährung mit umrechnen, maskierte Zeilen aus den Summen | F2, F6 |
| D12 | Einkauf-Delta: Stichtag aus der letzten erfolgreichen Zeile, vor dem Upsert je Beleg löschen, Fehlerzustand nicht cachen, leere Antwort ablehnen | E3, E4, E5, E9 |
| D13 | HR: Upload über Zwischendatei mit Inhaltsprüfung und Sicherung, Lesefehler als roter Punkt, Exporttag nicht aus der Dateizeit, Zeitraumlogik an einer Stelle | H2 bis H6 |
| D14 | Kultur in der App fest setzen und die kulturlosen Parser umstellen | F4, H5 |
| D15 | SAP in T76: Pflichtfilter für `FinanzJournalSet`, `AUTHORITY-CHECK` für die HR-Sets, `ZPOWERBI_KDAUF` gegen eingeschränkte Selektion, Meldung statt stiller Kappung | A1, A2, A5, A7 |
| D16 | P76-Werkzeuge: Erlaubtliste und Sperre der Sichern-Taste im Skript; die fünf schreibfähigen VBS mit Systemvergleich | A3, A4 |
| D17 | Sage und Alphaplan: Parameterübergabe als Hashtabelle, einheitliches Fenster, Delta-Skript versionieren | A8 bis A10 |
| D18 | Secrets in SQLite verschlüsseln, Export mit Secrets nur für Admin, Fehlerseiten und Header | P3, P4, P6 |

### D. Strukturell: Clean Code und Tests

| Nr. | Was | Nutzen |
| --- | --- | --- |
| D19 | Test, der jede `@page`-Route gegen eine Soll-Schutzklasse prüft; Schutz als Attribut an der Seite statt als Pfadliste | verhindert die ganze Befundklasse P1 |
| D20 | Ein Zugriffsdienst statt vier Kopien, mit Tests für Cookie, Sperren und Fehlversuche | P2 |
| D21 | Pipeline: `dotnet test` bei jedem Push (`.github/workflows` ist leer), Abdeckung als Bericht | Tests laufen unabhängig von der Sitzung |
| D22 | `IHttpClientFactory` statt `new HttpClient`, danach Tests für die Einkauf-Läufe | E2, E4, E5, E9 |
| D23 | Fachlogik aus Razor in Klassen ziehen (Betragsparser, Formatierer, Finance-Sperre) | testbar |
| D24 | `TimeProvider` statt `DateTime.Now` in Diensten und Tests | keine Tests an der Systemzeit |
| D25 | Die sechs grössten Dateien entlang der Schnittlinien der Reviewer teilen (Anhang) | Lesbarkeit, kleinere Reviews |
| D26 | `.editorconfig` mit Analyzern, Warnungen als Budget | einheitlicher Stil |
| D27 | Umstieg auf .NET 10, `dotnet list package --vulnerable` | P9 |
| D28 | Je SAP-Objekt ein gültiger Stand im Repo, Zuordnung Datei zu Objekt und Transport; Probe-Code aus `.tmp_sap_probe` ausdünnen | Clean Code 4.2 Punkt 9 |
| D29 | Minimaltests für `wwwroot/shopfloor/app.js` oder ein CSP-Header als Netz | S1 |

## 7. Nicht geprüft

- **Nur in Auszügen gelesen:** `ManagementCockpitService.cs` (etwa ein Drittel), `ExcelExportService.cs` (nur
  Gruppenmargen-Blätter), `PurchasingDashboardService.cs` (ohne 530-1370 und 1880-2404), `PurchasingDashboard.razor`,
  `ManagementCockpit.razor`, `DatabaseSchemaMaintenanceService.cs`, `DatabaseSeedService.cs`, `app.js`.
- **Gar nicht gelesen:** `PurchasingSpendExplorer.razor`, die meisten übrigen Razor-Seiten, `Services/MarketSegment*.cs`,
  die Interaktiv-Ansichten von HR und Verkauf, `PmSheetView.razor`, die SQL-Texte der Journal-Reader, alle Models, die
  MPC-Dateien, 31 ältere Gateway-Stände, 12 ältere ZLO03-Stände, CSV- und ZIP-Dateien.
- **Nur gezählt, nicht einzeln verfolgt:** 163 pauschale `catch`, 171 statische Sammlungen, 47 `Parse`-Aufrufe, 28
  `Task.Run`.
- **Tests:** nur per Suche und Messung, keine Testdatei inhaltlich gelesen. Die Abdeckung sagt, welche Zeilen laufen,
  nicht, ob die Tests das Richtige prüfen.
- **Nicht ausgeführt:** Browser, SAP, Produktiv-DB. Alle Szenarien sind aus dem Code abgeleitet.
- **Arbeitsbaum:** Das nicht committete `Program.cs` und `TimerBackgroundService.cs` (Finance_All-Automatik) hängen an den
  unversionierten Dateien `Services/FinanceAllExportService.cs` und `Services/FinanceAllWorkbookWriter.cs`; siehe 12.11.
  Abschnitt D.

## 8. Verweise

- Erstes Review: `docs/CODE_REVIEW_2026-11-12.md` (Befunde 1 bis 13, Behebungsvorschlag in Abschnitt F), Wochen_Todo 159.
- Berechtigungskonzept: Wochen_Todo 147.
- Fehlerliste: `docs/rag/LEARNINGS.md` (20.11.: vorhandenes Review vor dem Start nicht nachgeschlagen; Worktree ohne
  `trafag_exporter.db`).
- Veraltet gefunden und noch nicht nachgeführt: `.claude/agents/hr.md` nennt den Rexx Hub als ausgeschaltet; er ist laut
  `docs/rag/HR_KPI.md` seit dem 19.11. 13:25 eingeschaltet.

## Anhang: Schnittlinien für D25

- **`Services/ManagementCockpitService.cs`:** Datei-Analyse Excel (150-195, Lesehilfen bis 2925), zentrale Auswertung
  (197-347), Finance Summary (349-716) mit der CHF-Umrechnung (470-544) als eigenem Baustein, Heartbeat (718-1000),
  Datenstatus (1001-1090), Gruppenmarge (1500-1757), Prüfbuch (1775-1898), Pivot (1900-2110).
- **`Components/Pages/ManagementCockpit.razor`:** eigene Komponenten für Gruppenmarge (1133-1314), Rohdaten Diagnose
  (1394-1727), Spartenanalyse (815-1132), Finance Pivot (429-563), Heartbeat (1728-1823); Formatierer (3030-3080) neben
  `GroupMarginStatuses`.
- **`Services/PurchasingDashboardService.cs`:** SQL-Bausteine und Filter (136-241), Kontrakte (296-392), Kennzahlen
  (398-1025), Spend-Aufriss (1210-2221); die CHF-Regel an eine Stelle.
- **`Components/Pages/PurchasingDashboard.razor`:** Datenservice-Block samt Polling (716-722, 2495-2613), Kontrakte
  (1173-1226), Lagerwert-Trend (1227-1340), 3D (2686-2793), Ideen- und Paket-Kataloge (2831-2843) als Datenklasse.
- **`Services/HrKpi/HrKpiDashboardBuilder.cs`:** Excel-Leser und Parser (355-660, 831-890, 1193-1262, 2578-2800),
  Zusammenführung SAP und Rexx (662-1191), Filter (1263-1371), Kennzahlen und Ampeln (1372-1863), Fluktuation und
  Zeitraumlogik (1865-2330), Regeln und Konstanten (2338-2576); `Build` (26-353) bleibt als Ablauf.
