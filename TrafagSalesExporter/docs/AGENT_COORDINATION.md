# Agenten-Koordination

Stand: 2026-10-27

Diese Datei koordiniert gleichzeitig arbeitende Entwicklungsagenten im gemeinsamen
Workspace. Vor jeder Aenderung bitte vollstaendig lesen und den eigenen Eintrag
aktualisieren. Die Root-Dateien `AGENTS.md` und `CLAUDE.md` sowie `router.md` machen
diesen Schritt fuer neue Codex-/Claude-Sitzungen ausdruecklich verpflichtend.

**Diese Datei enthaelt nur laufende Arbeit und einen Kurzindex der juengsten Eintraege** (Ziel
sieben Tage; beim naechsten Aufraeumen gehoeren aeltere Zeilen in die Historie). Alles
Abgeschlossene, die frueheren Reservierungen gemeinsamer Dateien und die
Uebergabeprotokolle stehen in `docs/AGENT_COORDINATION_HISTORIE.md`: bis zum 2026-09-29 im
Volltext, seither in der Regel nur als verschobene Kurzindex-Zeile, deren Nachweise in der
genannten Fachdatei, in `lastchange.md` und in `docs/rag/DEPLOYMENT.md` (bzw. `docs/archiv/`)
liegen. Das ist am 2026-09-09 getrennt worden, weil die Datei auf 236 KB gewachsen war und
damit vor jeder Analyse gelesen werden musste, obwohl fast alles darin auf
„abgeschlossen, Reservierung frei" stand. Wer eine abgeschlossene Arbeit sucht,
findet sie ueber den Kurzindex unten oder ueber die Historiendatei; nichts ist
geloescht. Zuletzt aufgeraeumt am 2026-10-22 (51 Zeilen vor dem 2026-10-15 verschoben).

## Aktive Bereiche

Hier steht ausschliesslich, was gerade laeuft oder reserviert ist. Eintraege mit
`abgeschlossen`, `deployed`, `frei` oder `Historie` gehoeren nicht hierher, sondern
in die Historiendatei.

| Agent | Bereich | Reservierte Dateien / Ordner | Status |
|---|---|---|---|
| Claude | Einkaufs-Dashboard: Feedback Erich/Sandro/Marco vom 22.10. umsetzen (Auftrag Ingo „setze alles um“, P76/T76 lesend erlaubt) | `Services/Purchasing*`, `Components/Pages/Purchasing*`, Einkauf-Seiten, `docs/PURCHASING_DASHBOARD_2026-06-05.md`, `docs/rag/PURCHASING.md`, Tests Purchasing* | **Abgeschlossen, deployed 22.10. 13:38 (`77778a5`), bitgleich, ohne Alarm. Offen: Rueckfrage Sandro Zeitraum, Ansicht im Browser** |
| Claude | Handbestellwesen: Abloesung der PHP-Anwendung durch Modul im Dashboard (Auftrag Ingo 22.10.) | `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md`, kuenftig `Components/Pages/Handbestellwesen*`, `Services/Handbestellwesen/`; `handbestellwesen/` bleibt ungetrackt (Passwoerter) | **Phase 1 programmiert und committet (22.10.), Tests 1274/1274, nicht deployed, nicht im Browser gesehen. Entscheid 22.10.: weiterbetreiben, Nutzung festhalten (umgesetzt, Kachel Nutzung, Suite 1275/1275). Deployed 22.10. 13:38 (Reiter unsichtbar). Dump und neueste PHP-Dateien liegen vor; naechster Schritt Import-Werkzeug.** Zusaetzlich: `Modules/Handbestellwesen/`, `wwwroot/css/handbestellwesen.css`, ein Eintrag in `Services/DatabaseSeedService.cs` (Menue) und eine Zeile in `Program.cs` (nur eigener Hunk, fremde Aenderung dort bleibt unangetastet), Tests `TrafagSalesExporter.Tests/Handbestellwesen*` |
| Claude | ZLO03 FIX 19: Spalten REACH, SVHC, PFAS (Klasse `Z_PACK_REGULATION`, AUSP), Wunsch Florian Waechter; **Nachtrag 2026-10-22: Spalte RoHS** (Merkmal `ROHS`, von Adil am 21.10. angelegt) | `Z_ZLO03_TURBO2` in T76 (Auftrag `T76K912742`, Variante B), `zlo03/` | **`T76K912742` (FIX 12, 19 inkl. RoHS, 20) am 22.10. von Ingo freigegeben. FIX 21 (.xlsx) in T76 aktiv, neuer Auftrag `T76K912752`, getestet; offen: Freigabe/Import `T76K912752` (Ingo), Test mit Werten, Programm hinter ZLO03 in P76** |

**Hinweis:** Im Arbeitsbaum liegt die unfertige Finance_All-Automatik aus der am 2026-09-29 geschlossenen Sitzung vom 2026-09-11 (unkommittiert; Stand 2026-09-29: `Services/FinanceAllExportService.cs`, `Services/FinanceAllWorkbookWriter.cs`, Testdatei und Aenderungen an `TimerBackgroundService.cs`, `Program.cs`, `.csproj`; ob fertig, ist ungeprueft). Nicht mit anderer Arbeit committen oder deployen. Einzelheiten in der Historie.

## Kurzindex der juengsten Eintraege

Abgeschlossene Arbeit der letzten sieben Tage, eine Zeile je Eintrag. Aeltere Zeilen stehen
unveraendert in `docs/AGENT_COORDINATION_HISTORIE.md` (letzter Abschnitt); ob es dort einen
Volltext gibt, steht im Kopf der Historiendatei.

| Agent | Bereich | Letztes Datum | Ergebnis in Kurzform |
|---|---|---|---|
| Claude | Doku: Gesamtlandkarte aller Loesungen (Auftrag Ingo „md nachführen“) | 2026-10-27 | abgeschlossen, nur Doku, kein Code, kein Deploy, Reservierung frei. `docs/rag/ARCHITECTURE.md` (Abschnitt Gesamtlandkarte, Kopf berichtigt), `docs/LOESUNGSLANDKARTE_2026-10-27.html` (neu, Kopie des Artefakts), `docs/router/plattform.md`, `docs/router/projekt.md`, `baum.md`; Commit `1c9b362`; Qdrant-Reindex offen (Auto-Modus lehnte den Start ab, Ingo startet ihn selbst) |
| Claude (9e) | Wochen_Todo nachfuehren (Auftrag Ingo „aktualisiere weekly todo“) | 2026-10-27 | abgeschlossen, Reservierung frei, nur Doku. `projektmanagement/Wochen_Todo.tsv/.xlsx`, `Status_kurz.xlsx`: Zeile 120 (Handbestellwesen AD/Altbestand, offene Punkte) und 121 (Einkauf 27.10.: Spend-Schalter, Produktgruppe nur Text); kein Deploy. |
| Claude (9e) | Handbestellwesen: AD- und Altbestand-Mitarbeitende, neue nur aus dem AD (Auftrag Ingo) | 2026-10-27 | abgeschlossen, Reservierung frei. `HbModels` (`AusAd`), `HbRegistration` (Spalte nachziehen), `HbService`, beide Masken, Test, Konzept 6.10; Commit `3ff16f5`, Suite 1294/1294; deployed 2026-10-27 13:45 (von Ingo gestartet). |
| Claude (9e) | Einkauf Produktgruppe nur SAP-Text; Handbestellwesen Stammdaten loeschen und AD-Uebernahme (Auftrag Ingo) | 2026-10-27 | abgeschlossen, Reservierung frei. `PurchasingDashboardService.cs` + Test (`0917d9e`); `Modules/Handbestellwesen/*` (`HbAdSuche` neu), CSS, Tests (`a8e9eec`); Suite 1293/1293; deployed 2026-10-27 11:05 (von Ingo gestartet). |
| Claude (9e) | Handbestellwesen: Freigabe wie PHP, umschaltbar `FreigabeNurGleicheKostenstelle` (Auftrag Ingo, 1a) | 2026-10-27 | abgeschlossen, Reservierung frei. `HbService`, `HbModels`, beide Seiten, `appsettings.json`, Test, Konzept 6.8; Commit `a0030e8`, Suite 1291/1291; deployed 2026-10-27 10:02 (von Ingo gestartet). |
| Claude (9e) | Handbestellwesen klassisch: Link „Zur neuen Version“ auf `/bestellwesen` (Auftrag Ingo) | 2026-10-27 | abgeschlossen, Reservierung frei. `Modules/Handbestellwesen/HandbestellwesenPage.razor`, `wwwroot/css/handbestellwesen.css`, Konzept 6.6; Commit `9ef7787`, Suite 1290/1290; deployed 2026-10-27 09:47 (von Ingo gestartet). |
| Claude | ZZPRDAT: Live-Pruefung T76 vom 26.10. und fremde Testauftraege nachdokumentiert (Auftrag Ingo „fuehre punkte nach“) | 2026-10-27 | **Abgeschlossen, nur Doku, SAP nur lesend, kein Deploy, Reservierung frei.** Transportplan (neuer Abschnitt in 6a), Kontext, `rag/PROJECT.md`, PM-03, Status_kurz, Wochen_Todo 56 samt `.xlsx`, `lastchange.md`, zwei Zeilen in LEARNINGS. Issue-Log bewusst nicht beruehrt (fuehrt keine SAP-Vorhaben) |
| Claude (9e) | Einkauf Spend-Aufriss: Schalter „Nur Einkaufsteile (Disponent 001 bis 005)“, Vorgabe an | 2026-10-26 | abgeschlossen, Reservierung frei. Service/State/Explorer/Dashboard, Uebersetzungen, Test; Commit `c3147e1`, Suite 1290/1290; deployed 2026-10-27 08:26 (von Ingo gestartet). |
| Claude (9e) | Einkauf Spend-Aufriss: Einstiege „Disponent“ (DISPOKENNZ wie ZC23) und „Disponent Verwendung (ZLO03)“ | 2026-10-26 | abgeschlossen, Reservierung frei. `Services/PurchasingDashboardService.cs`, Explorer-Hinweise, Uebersetzungen, Test; Commit `bddf9f9`, Suite 1289/1289; deployed 14:38 (von Ingo gestartet). |
| Claude (9e) | Einkauf Produktgruppe wie ZM_OFFENE_KDAUF (eigener Disponent, eine Gruppe) | 2026-10-26 | abgeschlossen, Reservierung frei. Refresh, Schema (`MarcDispo`), Dashboard-Zuordnung, Spend-Aufriss-Texte, Tests; Suite 1288/1288; Deploy siehe `docs/rag/DEPLOYMENT.md`. |
| Claude (9e) | Einkauf Spend-Aufriss: Produktgruppen-Sichten ohne Deckelung | 2026-10-23 | abgeschlossen, Reservierung frei. `Services/PurchasingDashboardService.cs` (`NoCap` statt 20/15/10), Test neu; Suite 1289/1289; Deploy siehe `docs/rag/DEPLOYMENT.md`. |
| Claude | Whisper Studio: analoges VU-Meter auf der Aufnahmeseite | 2026-10-22 | abgeschlossen, Reservierung frei. `whisper/gui/WhisperStudio/Controls/VuMeter.cs`, `Recorder.Measure`, Doku HANDOFF/ANLEITUNG; 35 Tests gruen; kein Deploy (lokales Werkzeug). |
| Claude | Whisper Studio: Fortschrittsanzeige, dann Ausbau (WPF, Datum_Uhrzeit_Titel, Word/SRT/VTT, Aufnahme mit Systemton, Editor, Verlauf, lokale KI) | 2026-10-22 | **Abgeschlossen, kein Deploy (lokales Werkzeug), Reservierung frei.** Entwickelt in `.tmp_tools/whisper_kopie`, nach Freigabe Ingo ins Original uebernommen (Zip-Sicherung in `whisper/backup/`). `whisper/` seit `ae599c2` versioniert (nur Quellen/Doku). 31 Tests gruen inkl. echter KI-, Whisper- und Aufnahmelaeufe; Bedienung mit Maus ungeprueft. Doku `whisper/HANDOFF.md`, `lastchange.md`, `baum.md` |
| Claude (Sitzung e8) | Markdown-Aufraeumen: Archiv `docs/archiv/`, Historie, baum.md (Auftrag Ingo) | 2026-10-22 | **Abgeschlossen, nur Doku, kein Code, kein Deploy, Reservierung frei.** Commits `fdc1f6f` (51 Kurzindex-Zeilen in die Historie, Kopftexte berichtigt) und `286ec97` (lastchange 2'643 -> 560 Zeilen, rag/DEPLOYMENT 1'440 -> 270, beides verbatim nach `docs/archiv/`; baum.md 17 fehlende Dateien, Aeste neu sortiert; Archivverweise in router, init, projekt, rag ARCHITECTURE/FINANCE/PROJECT; docs/DEPLOYMENT Sicherungsregel ueberholt markiert; LEARNINGS). Erhaltung je Datei per Zeilenvergleich belegt. Offen (Folgearbeit, nicht begonnen): Koordinations-/Abschlussregel steht viermal (`CLAUDE.md`, `AGENTS.md`, `router.md` 6, `projekt.md`). Qdrant neu indexiert, siehe `lastchange.md` |
| Claude | ZLO03 FIX 21: Download als echte `.xlsx` (Wunsch Ingo) | 2026-10-22 | **Abgeschlossen in T76, kein Deploy.** Neuer Auftrag `T76K912752`, aktiv, rueckgelesen, 0 Unterschiede zum Textinhalt, Lasttest 2,2 Mio. Zellen 14 s. `zlo03/FIX20_DOWNLOAD_TXT_2026-10-22.md` Abschnitt 6 |
| Claude | ZLO03 FIX 20: „defekte Excel“ = Excel-Sperre fuer Text-.xls (`ExtensionHardening = 2`), Download als `.txt` | 2026-10-22 | **Abgeschlossen in T76, kein Deploy.** `download_dialog` `.txt` + `excel.exe`, aktiv, rueckgelesen identisch, Lauf oeffnet Excel, Inhalt gleich; in `T76K912742`. `zlo03/FIX20_DOWNLOAD_TXT_2026-10-22.md` |
| Claude | ZLO03 Spalte RoHS (Nachtrag FIX 19, Mail Adil) | 2026-10-22 | **Abgeschlossen in T76, kein Deploy.** Merkmal `ROHS` (T76, 21.10.) gelesen, Spalte RoHS vor REACH, aktiv 08:00, rueckgelesen identisch, Lauf `8475*` 182 Spalten; in `T76K912742`. FIX-19-Reservierung bleibt aktiv (Test mit Werten, P76) |
| Claude | SAP PS: Frage Philip „Rolle je Zeitbuchung“, Demo T76/100 (eigene Sitzung, Freigabe Ingo), Folien 17-19 | 2026-10-21 | **Abgeschlossen, kein Deploy, Reservierung frei.** KL01 PL/ENG/TST, KP26 KoSt 240, KB21N Beleg 311258908, in CJI3 geprueft; Doku 4b, Bilder 19-22, Aufraeumen in Abschnitt 7; Sitzung geschlossen |
| Claude | Wochen_Todo nachgefuehrt, Abgleich Code gegen Doku seit 01.10., Qdrant-Index neu aufgebaut | 2026-10-21 | **Abgeschlossen, nur Doku/Status, kein Deploy, Reservierung frei.** Kein App-Code nach dem letzten Deploy `c3c45cd`; Abweichung nur die fremde, unkommittierte Finance_All-Automatik (neu Zeile 117). Index 4245 Abschnitte (vorher 3884, Stand 13.10.); Neu Zeile 118 (Reindex-Skript robust machen), Notiz Zeile 15. Mail nicht geprueft (M365-Sitzung abgelaufen) |
| Claude | Doku: HR-Cockpit SAP-Abruf (`HrKpiSet`, `HrAbsenzSet`) als produktiv nachgefuehrt, live auf dem Server geprueft | 2026-10-20 | **Abgeschlossen, nur Doku, kein Deploy, Reservierung frei.** `docs/rag/HR_KPI.md`, `lastchange.md`; Dateien in `hrdata` von heute 05:07 |
| Claude | Doku: Abschlussrunde Dashboard/Data Lake und PS-Nachtrag in lastchange, PROJEKTSTATUS (PM-10 bis PM-13), Vorhaben_HERMES | 2026-10-20 | **Abgeschlossen, nur Doku, kein Deploy, Reservierung frei.** PM-11 an HERMES-Stand (erledigt 13.10.) angeglichen |
| Claude | SAP PS Einfuehrung (Auftrag Chef): Demo T76/100, Doku, Folien | 2026-10-20 | **Abgeschlossen, kein Deploy, Reservierung frei.** `DEMO-PS` + Teilprojekte, OKKP „Projekte aktiv“ nur T76 im lokalen Auftrag `T76K912750` (NICHT FREIGEBEN), KB21N-Beleg 311258907, Folien 11-15 in pptx-Kopie. `docs/SAP_PS_EINFUEHRUNG_DEMO_2026-10-20.md` |
| Claude | T76/200: Testuser `UBIG` nur mit `/GIB/DCP_SUPERVISOR` (Auftrag Ingo) | 2026-10-19 | **Abgeschlossen in T76/200, kein Deploy, Reservierung frei.** Eigene Sitzung, Kennwort zufaellig erzeugt (setzt Ingo), Profil `T-P4550180` generiert und abgeglichen. `saptasks/gib_dcp/NOTES.md` |
| Claude | Word-Doku ZC23 Materialanteil: Beschleunigung 47,7 s -> 13,5 s | 2026-10-19 | **Abgeschlossen, nur Doku, kein Deploy, Reservierung frei.** `saptasks/zc23_materialanteil/ZC23_Materialanteil_Beschleunigung_2026-10-19.docx`, in Word als PDF geprueft |
| Claude | ZLO03 FIX 12: Test in T76 mit nachgestelltem Fall | 2026-10-19 | **Abgeschlossen, nur T76 (lokale Testobjekte, Testdaten), Reservierung frei.** Alt: B63485 fehlt, neu: MK 5009; sonst gleich. Import P76 Ingo |
| Claude | Shopfloor-Mengen: Produktionstag statt Abfragetag, DW prod (DL790), Umbuchung 15. bis 19.10. | 2026-10-19 | **Abgeschlossen, deployed 10:04 (`c3c45cd`), Reservierung frei.** 1250/1250, bitgleich, Alarm nur WAL/SHM; Sa/So-Nullen korrigiert (LEARNINGS). TX aus ZC22 offen |
| Claude | ZLO03: MK-Abweichung B63485, Mechanismus belegt, FIX 12 in `Z_ZLO03_TURBO2` | 2026-10-16 | **Abgeschlossen in T76, Reservierung frei.** Transport `T76K912742`, aktiv, rueckgelesen; Funktionstest erst in P76 moeglich (T76 ohne Daten), Import Ingo. `zlo03/MK_B63485_FIX12_2026-10-16.md` |
| Claude | Arbeitsplatz Ingo: iPhone-Hotspot trennt alle 2 Min (Ereignisprotokoll, nur lesend) | 2026-10-16 | **Abgeschlossen, nur Doku, kein Deploy, Reservierung frei.** Geloest durch neuen Hotspot-Namen (frisches Profil). `docs/router/plattform.md` Abschnitt WLAN-Aussetzer, Nachtrag |
| Claude | Einkauf Spend-Aufriss: Stuecklisten-Netz „Gehirn“ (WebGL-Prototyp) | 2026-10-16 | **Abgeschlossen, deployed 10:48 (`d6ca646`), Reservierung frei.** In Chrome geprueft; Fehler parallel Deploy/Test in LEARNINGS |
| Claude | Menue: alle Hauptbereiche farbig (Rottoene), Admin grau, Home pulsiert | 2026-10-16 | **Abgeschlossen, deployed 10:13 (`1931084`), Reservierung frei.** In Chrome geprueft |
| Claude | Handbuch Operations: Prozessgrafik (Trafag CI) und Schnellueberblick; Handbuch kann Bilder | 2026-10-16 | **Abgeschlossen, Reservierung frei.** SVG/PNG aus `Tools/HandbuchGrafik`, Parser/Reiter/Word, Test; Deploy siehe `rag/DEPLOYMENT.md` |
| Claude | Shopfloor-Mengen aus SAP (J5 Logistik, J8 SEH/TR5, J11 FPY SEH) | 2026-10-16 | **Abgeschlossen, produktiv 09:51 (`9d6b5d2`), Reservierung frei.** P76 gleich Kontrollwerten, Erstlauf geschrieben. `saptasks/shopfloor_sap/NOTES.md` |
| Claude | Benutzerhandbuch Kapitel Operations erweitert (Geschaeftsprozess, alle Shopfloor-Seiten mit Interpretation, Glossar) | 2026-10-16 | **Abgeschlossen, Reservierung frei.** `10_operations.md`, Word neu (Verzeichnis aktualisiert), Handbuch-Tests 56/56; Deploy siehe `rag/DEPLOYMENT.md` |
| Claude | Operations-Reiter ohne Login (`Shopfloor:OpenForAll = true`) | 2026-10-16 | **Abgeschlossen, deployed 07:48 (`70a02b1`, Tests `1c03156`), Reservierung frei.** 1241/1241, bitgleich, ohne Alarm. Endpunkt-Tests setzen `OpenForAll=false` selbst |
| Claude | Shopfloor: Ampel loeschbar, Config liest `planned_source`, korrigierte Kopie `config_patch.js` | 2026-10-16 | **Abgeschlossen, deployed 07:37 (`e6c007f`), Reservierung frei.** 1241/1241, bitgleich, Alarm nur WAL/SHM, in Chrome geprueft. `SHOPFLOOR_2026-10-07.md` Abschnitt 14 |
| Claude | Shopfloor ZD05-Abgleich 04:00 und 12:45 (Entscheid Ingo), Speichertest Ampel produktiv | 2026-10-16 | **Abgeschlossen, deployed 07:28 (`a285009`), Reservierung frei.** 1240/1240, bitgleich, Alarm nur WAL/SHM; Testampel gesetzt und wieder entfernt. `SHOPFLOOR_2026-10-07.md` Abschnitt 14 |
| Claude | Shopfloor-Paket Fabio 2026-10-16 (Patch 03 Tobler, Config-Patch 04, zwei Korrekturen) | 2026-10-16 | **Abgeschlossen, deployed 07:06 (`b2103a4`), Reservierung frei.** 1240/1240, bitgleich, Alarm nur WAL/SHM, im Chrome geprueft. Offen: Jobs J2 bis J11 nicht gebaut, ZD05 04:00 (Entscheid Ingo). `SHOPFLOOR_2026-10-07.md` Abschnitt 14 |
| Claude | Fiori-App ZC23 live mit Selektion (Zc23Set, P_APP, ZSTR_ZC23_APP), Trafag-Logo | 2026-10-15 | **Abgeschlossen in T76, Reservierung frei.** Transport `T76K912738` offen (Report, Struktur, MPC/DPC), App `ZFI_ZC23` `$TMP`; Q2 gleich `ZPOWERBI_KDAUF`; Freigabe/P76 Ingo. `saptasks/fiori_zc23/NOTES.md` |
| Claude | Erste Fiori-App `ZFI_ERSTE_APP` (nur Lesen, `ZPOWERBI_EINKAUF_SRV/ProductDivisionRefSet`), T76 `$TMP`, Harness fuer Erfassungs-Apps | 2026-10-15 | **Abgeschlossen, Reservierung frei.** Upload per RFC `/UI5/REPO_LOAD_FROM_ZIP` (SapProbe `@datei:`), im Browser geprueft (50 Zeilen); kein Transport. `saptasks/fiori_erste_app/NOTES.md` |
| Claude | ZC23 Formel-Doku als Word (`docs/ZC23_Materialanteil_Formeln_2026-10-15.docx`), Generator `Tools/BuildZc23Formeln` | 2026-10-15 | **Abgeschlossen, nur Doku, Reservierung frei.** Commit `5fef8d6`, Nachtrag Generator und Doku-Verweise; OpenXML geprueft, visuell nicht gesichtet |
| Claude | ZC23 Spalte „Arbeit CZ“ wie ZM03, Spalten neben Standardwert, Waehrung in der Ueberschrift | 2026-10-15 | **Abgeschlossen in T76, Reservierung frei.** Transport `T76K912736` (4 Objekte), Werte = ZM03 und Nachrechnung; Freigabe/Import P76 Ingo |
| Claude | ZC23 Materialanteil gebaut (Kalkulation/Fremdleistung waehlbar, Schalter, schnelle Fassung, Standardwert+Materialanteil in ZPOWERBI_KDAUF und OData) | 2026-10-15 | **Abgeschlossen in T76, Reservierung frei.** Transport `T76K912736` (Struktur, Tabelle, MPC_EXT DEFINE, ZM_OFFENE_KDAUF), CMP identisch, Werte = ZM03 und Nachrechnung, Q2 13,5 s; Freigabe/Import P76 Ingo. `saptasks/zc23_materialanteil/NOTES.md` |
| Claude | Einkauf Spend-Aufriss: Platzhalter NA (N/A) ohne Anteil neben echter Produktgruppe (Fall B53173) | 2026-10-15 | **Abgeschlossen, deployed 12:48, Reservierung frei.** `7b62544`, 1235/1235, bitgleich; auch Lieferkettenanalyse |
| Claude | Operations: Admin-Schalter „Login noetig“ (Wunsch Fabio, Freigabe Produktionsleitung) | 2026-10-15 | **Abgeschlossen, deployed 12:15, Reservierung frei.** `8bfe424`, 1234/1234, bitgleich, ohne Alarm. Tabelle `AccessSettings`, PostConfigure ueber `Shopfloor:OpenForAll`; Schalter legt Ingo um |
| Claude | ZC23 Materialanteil: Analyse ZM03 (`ZLO_CALC_CZ_PREISE`) und CK11N, nur lesend in T76 per SapProbe | 2026-10-15 | **Abgeschlossen, nur Analyse, nichts in SAP geaendert, Reservierung frei.** `saptasks/zc23_materialanteil/NOTES.md`, Quelltext ZM03; ZK11N gibt es nicht (CK11N gemeint); offene Fragen in NOTES und Wochen_Todo 104 |
| Claude | Zusammenfassung aller Arbeiten 14./15.10. als Markdown | 2026-10-15 | **Abgeschlossen, nur Doku, kein Deploy, Reservierung frei.** `docs/ARBEITSSTAND_2026-10-14_15.md`, Router Projekt, `baum.md` |
| Claude | Shopfloor-Schnittstelle nimmt Fabios Push-Format an (ohne items-Huelle, `values`, Adresse `/api/integration/push`) | 2026-10-15 | **Abgeschlossen, deployed 11:06, Reservierung frei.** `f4e3f1b`, 1233/1233, bitgleich, beide Adressen mit falschem Token 401. Geaendert: `ShopfloorEndpoints.cs`, `ShopfloorStore.cs`, `ShopfloorEndpointTests.cs`, Doku SHOPFLOOR Abschnitt 13 |
| Claude | Passwoerter aller Bereiche im Admin setzbar (Admin, Finance, HR inkl. hradmin, Operations, Logistik), Hashes in der DB | 2026-10-15 | **Abgeschlossen, deployed 2026-10-15 10:56, Reservierung frei.** `e1afd49` + Doku `1519cb3`, 1232/1232, bitgleich, `/admin/passwoerter` 200. Seite `/admin/passwoerter`, Tabelle `AccessPasswords`, PostConfigure ueber appsettings, Operations-Cookie mit Hash-Fingerabdruck. Admin-Passwort setzt Ingo selbst auf der neuen Seite |
| Claude | Themenlandkarte als SVG (Quellen, Themen, Abhaengigkeiten, Ansprechpartner, Blocker) | 2026-10-15 | **Abgeschlossen, nur Doku, kein Deploy, Reservierung frei.** `docs/THEMENLANDKARTE_2026-10-15.svg`, Generator `projektmanagement/themenlandkarte.py`, Router Projekt, `baum.md` |
| Claude | Wochen_Todo durchgesehen (Mail/Teams seit 08.10. nur gelesen, Repo, Sitzung) | 2026-10-15 | **Abgeschlossen, nur Status, kein Deploy, Reservierung frei.** Nachgefuehrt 36, 87, 95, 96 (faellig 30.10.); neu 98 Einkauf-Delta 14.10. pruefen, 99 Zusammenarbeit Fabio, 100 Passwort-Hashes aus Repo, 101 Startseite/Kopfleiste (erledigt). Geaendert: Wochen_Todo.tsv/.xlsx, Status_kurz.xlsx |
| Claude | Shopfloor: Patches von Fabio (ZD05 Zu besprechen, Verkauf Go-for-it), Paket in docs, Push-Format-Abweichung dokumentiert | 2026-10-15 08:55 | **Abgeschlossen, produktiv 08:50 (`52b3b75`), Reservierung frei.** `wwwroot/shopfloor/app.js`, `style.css`, `docs/shopfloor_paket_2026-10-14/`, `SHOPFLOOR_2026-10-07.md` Abschnitt 13. Offen: Feldliste aus dem Paket noch nicht in die Konfiguration uebernommen |

## Regeln

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
