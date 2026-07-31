# RAG Router

Stand: 2026-07-31

Zweck: Diese Datei zuerst laden. Danach nur die Dateien aus dem passenden Themenblock laden.

## Lade-Regel

1. Immer nur diese Router-Datei zuerst lesen.
2. Bei aktuellem Produktivstand, UK-2025, Supplier-Feldern, Konzern-Standardkosten oder Einkauf-Delta zuerst `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md` laden.
3. Diese Live-Datei hat fuer diese Streitpunkte Vorrang vor aelteren Arbeitsnotizen.
4. Danach `lastchange.md` und die passende Kurzdatei aus `docs/rag/` laden.
5. Rohquellen nur laden, wenn Details, alte Zahlen, Codepfade, Mailtexte oder Audit gefragt sind.
6. Arbeitsregeln/Grenzen (Tests, Doku-Pflicht, fachliche Verantwortung): `persona.md` (Repo-Root).
7. SAP-Fakten (Feldnamen, Datenelemente, Tabelleninhalte, ABAP-Quelltexte) NICHT raten und nicht
   nur aus alter Doku uebernehmen: mit dem Werkzeug `SapProbe` direkt am System pruefen, siehe
   Abschnitt „Werkzeug: SAP-Direktzugriff (SapProbe)".

## Themen

| Thema | Wann laden | Standard laden |
| --- | --- | --- |
| Aktueller Livedaten-Stand | UK-2025, Supplier-Fuellung, GroupStandardCosts, Einkauf-Delta, widerspruechliche alte Aussagen | `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md` |
| Aktueller Stand | Projektstatus, letzte Aenderungen, offene Punkte | `docs/AKTUELLER_LIVEDATEN_STAND_2026-07-31.md`, danach `docs/rag/PROJECT.md` |
| Finance Cockpit | Soll/Ist, Finance Summary, Regeln, Laenderlogik | `docs/rag/FINANCE.md` |
| Finance Formeln/Mechanik | Wie rechnet was: Waehrungsumrechnung, Marge/Standardkosten, Land-Formeln, Trafag/Magnetic-Sense/GFS-Filter | `docs/rag/FINANCE_FORMELN.md` |
| Finance Prozess / Excel-Nachweis | Dashboard-Datenfluss, Audit-CSV, Sales_All, Finance Pruefbuch, Andreas-Nachvollziehbarkeit | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| Finance Spezialfaelle | IT, UK, ES, Abweichungen | `docs/rag/FINANCE.md` |
| Manual Import | UK-Deltas, Spanien Basis+Range, DE Alphaplan Full+Delta, Importprozess | `docs/rag/MANUAL_IMPORT.md` |
| HR KPI | HR Dashboard, Formeln, Datenqualitaet, Anwenderstand | `docs/rag/HR_KPI.md` |
| Deployment/IIS | Publish, Server, BiDashboard, TLS, lokaler Uebergang | `docs/rag/DEPLOYMENT.md` |
| Admin/Startseite | Admin Login, Sessions, Landing Page | `docs/rag/ADMIN.md` |
| Architektur | Systemuebersicht, Diagramme, technische Einordnung | `docs/rag/ARCHITECTURE.md` |
| Produktmapping | Group Sales Report, Produkthierarchie, Produktfamilie, Produktsparte | `docs/rag/PRODUCT_MAPPING.md` |
| Einkauf | Einkaufsdashboard, EKKO/EKPO/EKET, Lieferanten, offene Bestellungen/Kontrakte, Spend, Drilldown | `docs/rag/PURCHASING.md` |
| ZLO03/Stuecklistenanalyse-Webservice | ZM_LZCODE20_OPT, MaterialUsageSet/MaterialParentSet, ZCL_LZCODE_PROVIDER, SE11-Strukturen, SapProbe-Live-Verifikation | `docs/abap/README_LZCODE_WEBSERVICE.md` |
| 180-Tage-Roadmap Ingo | Management-Doku, Aufgaben Ingo, Sales/Data-Lake, HR/Einkauf, Abhaengigkeiten | `docs/INGO_TODOS_180_TAGE_2026-06-18.md` |
| Ansprechpartner | Wer ist zustaendig, wie erreichbar, Standortempfaenger, Eskalationspfad, Verwechslungsgefahren | `docs/ANSPRECHPARTNER.md` |

## Rohquellen Nur Bei Bedarf

| Datei | Nur laden fuer |
| --- | --- |
| `docs/raw_md_archive/HISTORY_CANONICAL.md.raw` | kanonische Detailhistorie mit Quellenangaben |
| `docs/raw_md_archive/original_history_raws.zip` | exakte Originaldateien nur zur Wiederherstellung, nicht fuer RAG laden |
| `docs/MD_DOKUMENTENSTATUS_2026-05-20.md` | Einordnung alter Dokumente |
| `docs/FINANCE_ENTSCHEIDE.md` | Finance-Entscheide im Detail |
| `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` | aktuelle Finance-Schulung, Prozessgrafiken, Audit-CSV und Waehrungsfluss |
| `docs/FINANCE_BERECHNUNGSFORMELN_LAENDER_2026-05-19.md` | Formeln pro Land |
| `docs/FINANCE_DATENFLUSS_ANDREAS_2026-06-08.md` | technischer Finance-Datenfluss inklusive Audit-CSV |
| `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` | Prozessablauf Finance Dashboard, operative Audit-CSV-Quelle, Rolle von `Sales_All`, Finance Pruefbuch |
| `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` | isolierter Kurs-/Umrechnungsworkflow vom Land bis Dashboard |
| `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` | offene Finance-Fragen fuer Budget-CHF-Spalten |
| `docs/FINANCE_BUDGET_CHF_MULTIPLE_CHOICE_2026-06-16.docx` | Multiple-Choice-Entscheidungsbogen fuer Finanzchef |
| `docs/INGO_TODOS_180_TAGE_2026-06-18.md` | editierbare Quelle fuer die 180-Tage-Roadmap von Ingo |
| `docs/INGO_TODOS_180_TAGE_2026-06-18.docx` | Word-Fassung der 180-Tage-Roadmap fuer Management-/IT-Dokumentation |
| `docs/MANUAL_IMPORT_DELTA_STAND_2026-05-21.md` | Manual-Import-Details |
| `docs/HR_KPI_NACHDOKU_2026-05-13.md` | HR-KPI-Details |
| `docs/DEPLOYMENT_IIS_HANDOFF_2026-05-19.md` | IIS-/Publish-Details |
| `docs/LOCAL_DEV_SERVER_UEBERGANG_2026-05-21.md` | lokaler Server im Detail |
| `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` | Admin-/Landing-Details |
| `docs/PRODUCT_SPARTEN_MAPPING_2026-05-27.md` | Produktsparten-Mapping im Detail |
| `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` | Gruppenmarge-Fachlogik, Andreas-Entscheide, Kostenwaehrungsschalter (Entscheid D) im Detail |
| `docs/FINANCE_GRUPPENMARGE_PROZESSFLUSS_2026-07-27.svg` | Visuelles Prozessfluss-/Filterdiagramm zur Gruppenmarge-Logik (Lieferant-Filter, Kostenbasis-Herkunft, Status, Aggregation) fuer Nicht-Finanzler |
| `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md` | Standardkosten-/MBEW-STPRS-Anbindung CH/AT und DE im Detail, inkl. Nachtrag 2026-07-16 (haengender mbewSet-Import, Kostenquoten-Verifikation) |
| `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` | SAP-OData-Spezifikation `Wavwr`-Feld in `FinanzdataSchweizOeSet` (Ersatz fuer haengenden mbewSet-Scan, CH/AT-Kostenbasis) |
| `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md` | SAP-OData-Spezifikation `FinanzJournalSet` fuer CH/AT-Journal-Import |
| `docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md` | B1-Journal-Import (Hauptbuch) Feldmapping im Detail |
| `docs/FINANCE_SAP_B1_KONNEKTOREN_ANDREAS_2026-07-01.md` | SAP-B1-Konnektoren-Uebersicht fuer Andreas |
| `docs/FINANCE_DASHBOARD_NACHWEIS_2026-06-17.md` | Aufbau/Formeln der `Finance_Dashboard_Nachweis_*.xlsx` im Detail |
| `docs/CODEX_ANWEISUNG_FINANCE_DATEN_HEARTBEAT_2026-07-13.md` | Umsetzungsanweisung Daten-Heartbeat im Detail |
| `docs/PURCHASING_DASHBOARD_2026-06-05.md` | Einkaufs-Hauptdoku mit allen Nachtraegen (Historie, Reviews, Fixes) |
| `docs/PURCHASING_DASHBOARD_KORREKTUREN_2026-07-06.md` | Einkaufs-Formel-/Logik-Korrekturen 2026-07-06 im Detail |
| `docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md` | Vorbereitung Einkauf-Review durch Ingo |
| `docs/PURCHASING_DASHBOARD_UMSETZUNGSPLAN_MARCO_2026-07-09.md` | Umsetzungsplan aus Marcos Einkauf-Review |
| `docs/PURCHASING_DASHBOARD_REVIEW_MARCO_2026-07-10.md` | Marcos Einkauf-Review im Detail, inkl. travp762-Feldrisiko |
| `docs/abap/README_LZCODE_WEBSERVICE.md` | ZLO03/ZM_LZCODE20_OPT als Webservice (Entwurf fuer Lucas): EntityStruktur, Determinismus-Fix, Gateway-Anlage; C#-Seite in `Services/MaterialUsageDataRefreshService.cs` |
| `docs/FINANCE_STANDARDKOSTEN_ARBEITSNOTIZ_2026-07-17.md` | Arbeitsnotiz Standardkosten/Margenreporting mit Andreas (Stichproben, fix/variabel-Frage) |
| `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` | Sitzungsmitschrift Andreas: 3-Tabellen-Architektur TR AG/IT/IN bestaetigt, Supplier-Country-Widerspruch, SupplierNumber-Luecke, Aktionspunkte/Deadlines |
| `docs/FINANCE_SUPPLIER_LUECKE_ANALYSE_2026-07-28.md` | Supplier-Luecke auf PRODUKTIVdaten quantifiziert: 69'919 von 84'788 Zeilen ohne Lieferant, 63'008 Zeilen mit Kosten aber maskierter Marge, Aufschluesselung je TSC |
| `docs/FINANCE_DATENLUECKEN_ANDREAS_2026-07-28.md` | Andreas' rote Pivot-Markierungen geprueft: CH/AT liest vom TEST-Server travt762 (Datenschnitt Mitte April 2026), ES-Range-Export erst ab 28.05.2026, UK ohne 2025 |
| `docs/FINANCE_CHAT_2026_LUECKE_ROOTCAUSE_2026-07-28.md` | Root Cause CH/AT-2026-Luecke: Report `Z_TRAFAG_DACH_EXPORT` nie auf P76 fuer 2026 gelaufen; Beweiskette T76/P76, Fix-Reihenfolge, Namensfalle Z_TRAFAG_SCHWEIZ_EXPORT |
| `docs/FINANCE_ISSUE_LOG_ANDREAS_2026-07-28.md` | Andreas' Issue-Log mit Status/Owner/Nachweis je Punkt; Detailbefund Laendercodes (Spanien Klartextnamen, TRDE leer) und PostingDate-Luecke TR ES |
| `docs/FINANCE_BACKFILL_UK_ES_2026-07-28.md` | Backfill UK-2025 aus App-eigenem Export: bewiesene Verdopplungsfalle (`SageNetSales` vs. Exportspalte), Namensregeln fuer Basisdateien, warum die Spanien-Datei redundant ist |
| `docs/FINANCE_IT_VORGEHEN_2026-05-18.md` | IT/Italien-Finance-Vorgehen im Detail |
| `docs/FINANCE_UK_QUELLE_KORREKTUR_2026-05-18.md` | UK-Quellkorrektur (Sage) im Detail |
| `SAGE_SPAIN_EXPORT_2026-05-05.md` (Repo-Root) | Spanien-Sage-Export im Detail |
| `docs/SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03.md` | Spanien-rclone-Upload-Anleitung (All-in-one) |
| `docs/HR_KPI_KORREKTUREN_2026-07-06.md` | HR-KPI-Formel-/Logik-Korrekturen 2026-07-06 im Detail |
| `docs/HR_KPI_PRUEFUNG_SWISS_BEST_PRACTICES.md` | HR-KPI-Pruefung gegen Schweizer Best Practices |
| `docs/ALPHAPLAN_DISCOVERY_EXPORTER_GUIDE_2026-06-08.md` | Alphaplan-Discovery-Exporter (DE-Server-Seite) |
| `docs/ALPHAPLAN_SQL_RCLONE_KONZEPT_DE_2026-06-08.md` | Alphaplan-SQL/rclone-Konzept Deutschland |
| `AlphaplanExportPackage/CLAUDE_ODATA_DASHBOARD_KONTEXT.md` | Kontextdatei im Alphaplan-Exportpaket (DE-Server) |
| `AlphaplanExportPackage/scripte/ANLEITUNG_KORREKTUR_2026-06-24.md` | Korrektur-Anleitung Alphaplan-Exportskripte |
| `docs/REQUIREMENTS.md` | Anforderungsuebersicht/Backlog |
| `docs/PROGRAMM_DIAGRAMME.md` | Programm-/Ablaufdiagramme |
| `docs/abap/README_FIN_ANALYSE_STPRS_JOURNAL.md` | ABAP-Analysereport STPRS/Journal (CH/AT-Nachweise) |
| `docs/abap/README_PRODSPARTE.md` | ABAP-Produktsparten-Provider |
| `spartenlogic/UEBERGABE_PRODUKTSPARTEN_ZUORDNUNG.md` | Uebergabe-Doku Produktsparten-Zuordnung (Analyse-Historie) |
| `docs/CCUSAGE_INSTALL_ANLEITUNG.md` | Tooling (ccusage), nicht projektfachlich |
| `docs/raw_md_archive/LASTCHANGE_ARCHIV_bis_2026-07-12.md` | archivierte `lastchange.md`-Eintraege bis 2026-07-12 (verbatim) |
| `docs/raw_md_archive/LASTCHANGE_ARCHIV_2026-07-13_bis_2026-07-30.md` | archivierte `lastchange.md`-Eintraege 2026-07-13 bis 2026-07-30 (verbatim) |
| `docs/raw_md_archive/RAG_KURZDATEIEN_ARCHIV_ueberholte_eintraege.md` | archivierte, durch neuere ersetzte Kurzstand-Eintraege aus FINANCE/PROJECT/DEPLOYMENT (verbatim) |

## Werkzeug: SAP-Direktzugriff (SapProbe)

Stand: 2026-07-20

Ort: `.tmp_sap_probe/` (Repo-Wurzel `TrafagSalesExporter`). Quellcode ist in Git
(`Program.cs`, `SapProbe.csproj`, `RunSapProbeInteractive.ps1`), die **kompilierte EXE nicht** —
nach frischem Clone erst bauen (Ziel `bin\x86\Release\net48\SapProbe.exe`).

Zweck: Direkter SAP-Zugriff per **RFC/NCo** (SAP .NET Connector) — unabhaengig von der
OData-Strecke der App. Damit lassen sich SAP-Fakten pruefen, statt sie zu vermuten.

**Wann nutzen:** Immer wenn eine Aussage ueber SAP getroffen werden soll, die man sonst raten
wuerde — existiert ein Feld, wie heisst das Datenelement, was steht wirklich in einer Z-Tabelle,
wie sieht der aktuelle ABAP-Quelltext aus. Die Ergebnisse gehoeren danach in die betroffene
Doku (z. B. offene Punkte in `docs/abap/README_LZCODE_WEBSERVICE.md`).

| Befehl | Zweck |
| --- | --- |
| `system-info` | Ping + Systeminfo (Default-Befehl) |
| `table-read <tab> --fields A,B --where "..." --rowcount n` | Tabelleninhalte lesen (`RFC_READ_TABLE`) |
| `table-fields <tab> [feld]` | DDIC-Metadaten inkl. Spalte `ROLLNAME` = Datenelementname (`DDIF_FIELDINFO_GET`) |
| `field-exists <tab> <feld>` | Existiert ein Feld? |
| `function-info` / `function-search` / `rfc-call` | RFC-Bausteine inspizieren/aufrufen; `function-info` zeigt bei TABLE/STRUCTURE-Parametern auch die verschachtelten Feldnamen |
| `rfc-call ... --table NAME=datei.csv` / `--struct NAME=datei.csv` | Generische Tabellen-/Strukturparameter aus CSV befuellen (2026-07-20 ergaenzt) |
| `abap-read <prog> [--out datei]` | ABAP-Quelltext aus dem System lesen (`RPY_PROGRAM_READ`) |
| `abap-check <prog> [--source-file ...]` | Syntaxpruefung im System |
| `abap-write` / `abap-activate` | Schreiben/Aktivieren, gesperrt hinter `--confirm-write` |

Ziel-Default: `travt762.sap.trafag.com` (SID `T76`, Client 100, User `KOI`) — das ist der
**TEST**-Server, derselbe, auf den `PURCHASING_SAP` zeigt. Lesezugriffe dort sind risikoarm.
Prod waere `travp762` (`--ashost` ueberschreiben, vorher abstimmen).

### Was das Tool kann und was nicht (Stand 2026-07-21, am Live-System T76 geprueft)

| Aufgabe | Automatisch per SapProbe? | Von Hand noetig | Womit / Bemerkung |
| --- | --- | --- | --- |
| SAP pingen, Systeminfo | Ja | — | `system-info` |
| Tabelleninhalte lesen | Ja | — | `table-read` (`RFC_READ_TABLE`) |
| DDIC-Feldmetadaten lesen (Datenelement, Laenge, Typ) | Ja | — | `table-fields`/`field-exists` (`DDIF_FIELDINFO_GET`) |
| RFC-Bausteine inspizieren/suchen | Ja | — | `function-info`/`function-search` |
| Beliebigen **RFC-faehigen** Baustein aufrufen (Skalare, Tabellen, Strukturen) | Ja | — | `rfc-call` (+ `--table`/`--struct` aus CSV, seit 2026-07-20) |
| ABAP-Quelltext eines Reports lesen | Ja | — | `abap-read` (`RPY_PROGRAM_READ`) |
| ABAP-Syntax im System pruefen | Ja | — | `abap-check` |
| ABAP-**Programm**/Report/Include schreiben + aktivieren | Ja | — | `abap-write`/`abap-activate` (`RPY_PROGRAM_INSERT`), gesperrt hinter `--confirm-write` |
| DDIC-Struktur/Tabelle anlegen (SE11) | **Nein** | **Ja, SE11** | `DDIF_TABL_PUT`/`DDIF_TABL_ACTIVATE` existieren, sind aber auf T76 **nicht RFC-freigegeben** (Invoke-Test: „ist nicht 'remote' aufrufbar", 2026-07-21 verifiziert). `DDIF_STRU_PUT` existiert entgegen erster Annahme gar nicht. |
| Globale Klasse anlegen (z. B. `ZCL_LZCODE_PROVIDER`) | **Nein** | **Ja, SE24/ADT** | `RPY_PROGRAM_INSERT` legt nur Programme an, keine `SEOCLASS`/`SEOCOMPO`-Metadaten - das kennt SE24 sonst nicht als Klasse. |
| Gateway-Service/EntityType/EntitySet anlegen, aktivieren (SEGW) | **Nein** | **Ja, SEGW** | Kein RFC-Baustein dafuer bekannt/geprueft; Modellaenderung + Codegenerierung laeuft nur ueber die SEGW-UI. |
| Metadaten-Cache am Gateway leeren | Nicht geprueft | Vermutlich ja | z. B. `/IWFND/CACHE_CLEANUP` o.ae. - noch nicht verifiziert. |
| SAP-Passwort eingeben | — | Immer der Mensch | Interaktiv maskiert oder `SAP_NCO_PASSWORD`/`SAP_T76_PASSWORD` als Env-Var - eine Claude-Session kann den interaktiven Prompt nicht bedienen. |

**Fuer `ZSTR_LZCODE_USAGE`/`ZSTR_LZCODE_PARENT` konkret:** Struktur-Anlage (SE11) und
Klassen-Anlage (SE24) bleiben manuell durch Lucas/Ingo. Die Feldliste dafuer ist 2026-07-21 am
Live-System verifiziert (Datenelemente `ZZLZCOD`/`ZZLZCODSORT` existieren als CHAR 4,
`KOM_MSTAE` ist ein MATNR-Feld, `ZAT_VC`/`ZMD04_CALC` lesbar) - siehe
`docs/abap/README_LZCODE_WEBSERVICE.md`, Abschnitt „Live-Verifikation 2026-07-21", und
`.tmp_sap_probe/ddic_lzcode/` als Abtippvorlage.

### Ausfuehrung (wichtig fuer Chat-Sessions)

Das Passwort wird maskiert **interaktiv** abgefragt (alternativ `SAP_NCO_PASSWORD` bzw.
`SAP_T76_PASSWORD` als Umgebungsvariable), und das PS1 wartet am Ende auf Enter. Eine
Claude-Session kann diese Prompts nicht bedienen — Ingo muss die Befehle selbst mit
`!`-Praefix starten, dann landet die Ausgabe im Chat:

```text
! powershell -NoProfile -ExecutionPolicy Bypass -File .\.tmp_sap_probe\RunSapProbeInteractive.ps1 table-fields MARA ZZLZCOD
! powershell -NoProfile -ExecutionPolicy Bypass -File .\.tmp_sap_probe\RunSapProbeInteractive.ps1 table-read ZAT_VC --fields KOMPNR,MATNR,KOM_MSTAE --rowcount 10
```

Diese Proben wurden 2026-07-21 bereits ausgefuehrt (Ergebnisse in
`docs/abap/README_LZCODE_WEBSERVICE.md`, Abschnitt „Live-Verifikation 2026-07-21"). Alternativ
liest die Env-Variablen-Variante das Passwort aus `SAP_NCO_PASSWORD` (dann `--no-password-prompt`),
dann kann auch die Claude-Session die reinen Lese-Befehle selbst fahren, ohne interaktiven Prompt.

Ergaenzend liegen dort zwei reine OData-Probe-Skripte (kein NCo, ohne Build nutzbar):
`probe_travp762_odata.ps1` und `probe_travp762_stprs.ps1`.

## Werkzeug: HANA-/SAP-B1-Direktzugriff (HanaQ)

Stand: 2026-07-28 (neu)

Ort: `.tmp_tools/HanaQ/` (`Program.cs`, `HanaQ.csproj`). Build mit `dotnet build`; braucht den
SAP-HANA-.NET-Client unter `C:\Program Files\sap\hdbclient\dotnetcore\v2.1\`.

Zweck: Ad-hoc **read-only** SQL gegen die B1-/HANA-Quellsysteme der Tochtergesellschaften —
um SAP-B1-Fakten (Feldinhalte, Fuellgrade, Bewertungsmethoden) direkt am System zu pruefen,
statt sie aus alter Doku zu uebernehmen. Ergaenzt `SapProbe` (das nur SAP ERP per RFC kann).

**Wann nutzen:** Immer wenn eine Aussage ueber B1-Daten getroffen werden soll — existiert ein
Feld, wie hoch ist der Fuellgrad, welche Bewertungsmethode laeuft, stimmt eine Zahl aus einem
alten Doku-Eintrag noch. Ergebnisse gehoeren danach in die betroffene Doku.

Aufruf:

```text
.tmp_tools/HanaQ/bin/Debug/net8.0/HanaQ.exe <TSC> <sqlFile> [dbPath]
```

- `<TSC>` z. B. `TRIT`, `TRFR`, `TRUS`, `TRIN` — Verbindung, Schema, Credentials werden aus dem
  lokalen SQLite-Snapshot aufgeloest (`Sites` + `SourceSystemDefinitions` + `HanaServers`),
  genau wie die App es tut. Kein Passwort im Klartext noetig.
- `<sqlFile>` Textdatei mit mehreren Statements, getrennt durch eine Zeile die mit `;;` beginnt.
  Platzhalter `{schema}` (Original, z. B. `it01_p`) und `{SCHEMA}` (uppercase, fuer
  `SYS.TABLE_COLUMNS`-Abfragen) werden ersetzt.
- Guardrail: alles was nicht mit `SELECT` oder `WITH` beginnt, wird uebersprungen —
  Schreibzugriffe sind nicht moeglich. Fehler je Statement werden abgefangen, der Rest laeuft
  weiter.

| Aufgabe | Beispiel |
| --- | --- |
| Existierende Spalten finden | `SELECT TABLE_NAME, COLUMN_NAME FROM SYS.TABLE_COLUMNS WHERE SCHEMA_NAME='{SCHEMA}' AND TABLE_NAME='OITM'` |
| Fuellgrad messen | `SELECT COUNT(*), SUM(CASE WHEN "AvgPrice">0 THEN 1 ELSE 0 END) FROM {schema}."OITM"` |
| Beleg gegen Stammdaten vergleichen | Join `{schema}."INV1"` / `{schema}."OINV"` / `{schema}."OITM"` |

**Wichtige Lehren aus dem ersten Einsatz (2026-07-27/28, TR IT):**
- **Grundgesamtheit immer mitfiltern.** `OITM` enthaelt auch Nicht-Lagerartikel und inaktive
  Artikel. Fuer fachliche Prozentzahlen `WHERE "InvntItem"='Y' AND "validFor"='Y'` setzen —
  ohne diesen Filter waren zwei Aussagen verzerrt (97.8 % statt 99.1 %, 40 % statt 75.7 %).
- **Zahlen schwanken** zwischen zwei Abfragen um wenige Stueck (Produktivsystem, laufende
  Artikelanlage) — in Aussagen runden bzw. „ca." schreiben.
- **B1-Kosten liegen auf zwei Ebenen**, nicht verwechseln: `INV1.StockPrice` (Belegposition,
  gefuellt) vs. `OITM.AvgPrice`/`PrdStdCst`/`OITW.AvgPrice` (Artikelstamm, bei
  Chargenbewertung strukturell leer).
- **Indien (`TRIN`, `20.197.20.60:30015`) ist vom Entwicklungsrechner nicht erreichbar**
  (Timeout `rc=10060`, verifiziert 2026-07-15 und 2026-07-28) — dafuer braucht es
  VPN-/Firewall-Freigabe. Der Produktivserver erreicht die Quelle dagegen taeglich.

Genutzte SQL-Dateien liegen im Scratchpad der jeweiligen Session, nicht im Repo — die
Ergebnisse sind in `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` Abschnitt 5b
festgehalten.

## Entfernt 2026-07-15 / nur noch im Raw-Archiv

Acht Dateien wurden am 2026-07-15 aus der aktiven Struktur entfernt (reine Pointer-Stubs
oder inhaltlich vollstaendig durch spaetere Entscheide/Features ersetzt: ES-Referenz,
DE-Jahr, IC-Diagnose, Gruppenmarge sind laengst umgesetzt). Volltext liegt in
`docs/raw_md_archive/HISTORY_CANONICAL.md.raw` (Bloecke H324-H331); aktueller Stand steht
in `docs/rag/FINANCE.md` und `docs/FINANCE_GRUPPENMARGE_2026-06-16.md`. Details/Gruende:
`docs/MD_DOKUMENTENSTATUS_2026-05-20.md` Abschnitt „Archiviert / aus aktiver Doku entfernt".

| Entfernte Datei | Ersetzt durch |
| --- | --- |
| `HANDOFF_2026-04-15.md`, `NEXT_STEPS_2026-04-15.md`, `LLM_SYSTEM_GUIDE.md` (root) | diese Router-Datei selbst |
| `entscheide.md` (root) | `docs/FINANCE_ENTSCHEIDE.md` |
| `docs/FINANCE_STATUS_OFFENE_PUNKTE_2026-06-01.md` | `docs/rag/FINANCE.md` „Offene Fachpunkte" |
| `docs/FINANCE_MEMO_ANDREAS_2026-06-01.md` | `docs/rag/FINANCE.md`, `docs/FINANCE_ENTSCHEIDE.md` |
| `docs/FINANCE_SITZUNGSPUNKTE_ANDREAS_2026-06-02.md` | `docs/rag/FINANCE.md`, `docs/FINANCE_ENTSCHEIDE.md` |
| `docs/HANDOFF_2026-06-16.md` | `docs/rag/PROJECT.md` |

## Suchwoerter

| Suchwort | Thema |
| --- | --- |
| `Finance Summary`, `Soll/Ist`, `check.xlsx`, `FinanceRuleEngine` | Finance Cockpit |
| `Finance Pruefbuch`, `Pruefbuch`, `Sales_All`, `Sales_ProcessedMergeInput`, `Andreas`, `Excel-Nachweis`, `Dashboard-Quelle` | `docs/FINANCE_DASHBOARD_PROZESSABLAUF_2026-06-30.md` |
| `Schulung`, `Training`, `Audit-CSV`, `Sales_ProcessedMergeInput`, `Auswertungsquelle`, `Wirtschaftspruefung` | `docs/FINANCE_SCHULUNG_FINANZ_2026-06-11.md` |
| `Wechselkurs`, `Umrechnungskurs`, `CurrencyExchangeRates`, `DocumentRate`, `ConvertCurrency`, `Anzeige-Waehrung` | `docs/FINANCE_KURS_WORKFLOW_2026-06-09.md` |
| `Budget-CHF`, `Budgetkurs`, `Net Sales Actual CHF Budget`, `Finanzchef`, `Multiple Choice` | `docs/FINANCE_BUDGET_CHF_FRAGEN_FINANZCHEF_2026-06-15.md` / `docs/FINANCE_BUDGET_CHF_MULTIPLE_CHOICE_2026-06-16.docx` |
| `180 Tage`, `Roadmap`, `Ingo`, `Sales Cockpit`, `Data-Lake`, `Einkaufs Dashboard`, `HR Dashboard`, `Management-Doku` | `docs/INGO_TODOS_180_TAGE_2026-06-18.md` |
| `Ansprechpartner`, `Kontakt`, `Mailadresse`, `wer ist zustaendig`, `Eskalation`, `Owner`, `Verteiler`, `Standortempfaenger` | `docs/ANSPRECHPARTNER.md` |
| `Standort-Mail`, `Mailtext`, `Preferred Vendor Anfrage`, `OITM.CardCode Bitte`, `Versandstand` | `docs/FINANCE_FELDLUECKEN_MAILS_2026-07-31.md` |
| `TRUK`, `UK_B1`, `Delta`, `Manual Excel` | Manual Import / Finance Spezialfaelle |
| `TRDE`, `Alphaplan`, `invoice_headers`, `invoice_lines`, `BelegePositionenID`, `NettoPreisGesamt`, `ArtikelNummer`, `MATNR` | Finance Cockpit / Manual Import |
| `TRSE`, `Spain`, `Sage`, `ImporteNeto` | Finance Spezialfaelle |
| `TRIN`, `Indien`, `India`, `SAGE`, `20.197.20.60`, `TRAFAGCONTROLS` | Deployment/IIS oder Finance Spezialfaelle |
| `Spain rclone`, `Spanien SharePoint`, `Run-SpainRangeExportAndUpload-AllInOne`, `trafag-bi` | `docs/SAGE_SPAIN_RCLONE_UPLOAD_GUIDE_2026-06-03.md` |
| `3D Datenanalyse`, `Experten`, `Schnelluebersicht`, `Simulation`, `Wechselkurs-Schieberegler` | Finance Cockpit |
| `HR KPI`, `Rexx`, `Austritte`, `Absenzen` | HR KPI |
| `IIS`, `BiDashboard`, `Publish`, `TLS`, `Client certificate` | Deployment/IIS |
| `Upgreat`, `Firewall`, `Freigabe`, `10.120.1.17`, `30015`, `8000` | Deployment/IIS |
| `Admin Bereich`, `AdminAccess`, `LandingPage` | Admin/Startseite |
| `Group Sales Report`, `Produkthierarchie`, `Produktfamilie`, `Produktsparte`, `Z.Prodh` | Produktmapping |
| `Gruppenmarge`, `Standardkosten`, `STPRS`, `MBEW`, `Kostenwaehrung`, `GroupMarginCostCurrencyMode` | `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` / `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md` |
| `WAVWR`, `VBRP`, `mbewSet haengt`, `Standardpreis-Read`, `GroupStandardCosts` | `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` / `docs/FINANCE_STANDARDKOSTEN_2026-07-14.md` Nachtrag 2026-07-16 |
| `Journal Import`, `Hauptbuch`, `BKPF`, `BSEG`, `OJDT`, `JDT1`, `FinancialJournalEntries`, `FinanzJournalSet` | `docs/FINANCE_B1_JOURNAL_IMPORT_2026-07-14.md` / `docs/FINANCE_JOURNAL_SAP_ODATA_SPEZ_2026-07-14.md` |
| `Daten-Heartbeat`, `Heartbeat`, `ExportLogs`, `Datenkontinuitaet` | `docs/CODEX_ANWEISUNG_FINANCE_DATEN_HEARTBEAT_2026-07-13.md` / Finance Cockpit |
| `EKKO`, `EKPO`, `EKET`, `Einkauf`, `Lieferanten`, `offene Bestellungen`, `Kontrakte`, `Spend` | Einkauf |
| `Drilldown`, `Warengruppe`, `MaraMatkl`, `maracalc`, `MARA001Set`, `Mstae`, `LFA1`, `SupplierName`, `ABC`, `XYZ`, `MAABC` | Einkauf |
| `Deckungsbeitrag`, `DB %`, `StandardCostVariable`, `StandardCostFixed`, `fix/variabel`, `ContributionMarginCalculator` | Finance Cockpit / `docs/FINANCE_STANDARDKOSTEN_ARBEITSNOTIZ_2026-07-17.md` |
| `NETWR_HC`, `Kurrf`, `Faktor 100`, `CorrectHouseCurrencyScaling` | `docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md` |
| `Supplier-Felder leer`, `Lieferant unklar`, `GroupMarginSupplierClassifier` | Finance Cockpit / `docs/FINANCE_GRUPPENMARGE_2026-06-16.md` |
| `Formel`, `Kostenbasis-Formel`, `Umrechnungsformel`, `Wie rechnet`, `Magnetic Sense`, `GFS`, `Wortgrenze`, `FinanceIntercompanyRule`, `ResolveRate` | `docs/rag/FINANCE_FORMELN.md` |
| `travt762`, `travp762`, `Test-Server`, `SapServiceUrl` | Finance Cockpit + Einkauf (offener Punkt in beiden) |
| `Sitzung Andreas`, `3-Tabellen-Architektur`, `SupplierNumber-Luecke`, `Trafag Italien Tabelle`, `Trafag Indien Tabelle` | `docs/FINANCE_STANDARDKOSTEN_SITZUNG_ANDREAS_2026-07-27.md` |
| `ZLO03`, `ZM_LZCODE20_OPT`, `MaterialUsageSet`, `MaterialParentSet`, `ZCL_LZCODE_PROVIDER`, `Stuecklistenanalyse` | `docs/abap/README_LZCODE_WEBSERVICE.md` |
