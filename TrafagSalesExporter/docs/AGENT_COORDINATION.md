# Agenten-Koordination

Stand: 2026-09-09

Diese Datei koordiniert gleichzeitig arbeitende Entwicklungsagenten im gemeinsamen
Workspace. Vor jeder Aenderung bitte vollstaendig lesen und den eigenen Eintrag
aktualisieren. Die Root-Dateien `AGENTS.md` und `CLAUDE.md` sowie `router.md` machen
diesen Schritt fuer neue Codex-/Claude-Sitzungen ausdruecklich verpflichtend.

**Diese Datei enthaelt nur laufende Arbeit und die letzten sieben Tage.** Alles
Abgeschlossene, die frueheren Reservierungen gemeinsamer Dateien und die
Uebergabeprotokolle stehen im Volltext in `docs/AGENT_COORDINATION_HISTORIE.md`.
Das ist am 2026-09-09 getrennt worden, weil die Datei auf 236 KB gewachsen war und
damit vor jeder Analyse gelesen werden musste, obwohl fast alles darin auf
„abgeschlossen, Reservierung frei" stand. Wer eine abgeschlossene Arbeit sucht,
findet sie ueber den Kurzindex unten oder ueber die Historiendatei; nichts ist
geloescht.

## Aktive Bereiche

Hier steht ausschliesslich, was gerade laeuft oder reserviert ist. Eintraege mit
`abgeschlossen`, `deployed`, `frei` oder `Historie` gehoeren nicht hierher, sondern
in die Historiendatei.

| Agent | Bereich | Reservierte Dateien / Ordner | Status |
|---|---|---|---|
| Codex | ZZPRDAT / Abschluss vollstaendig nachdokumentieren | `lastchange.md`, `projektmanagement/PROJEKTSTATUS.md` nur PM-03 (PM-02/ZC12 unberuehrt), `projektmanagement/Wochen_Todo.tsv` und `.xlsx` nur ZZPRDAT, `docs/router/sap.md`, `baum.md`, `saptasks/zzprdat/erzeuge_doku.py`, `docs/ZZPRDAT_Loesung_2026-09-03.docx`, `docs/ZZPRDAT_Mail_Abnahme_2026-09-04.html`, `saptasks/ZZPRDAT_TRANSPORTPLAN.md`, `docs/AGENT_COORDINATION.md` | In Arbeit am 2026-09-07 auf „bitte nachdokuemtnieren“. Bereits vorhandene Fachnotizen beibehalten; veraltete Report-Offenmeldungen und Drucktest-Aussagen in Projekt-/Abnahmeunterlagen korrigieren. Keine SAP-Aktion, kein Versand, keine Transportfreigabe. |
| Claude | SAP / PM-02 ZC12: `p_debug` und das Tracing in `ZM_ABGLEICH_KTSCH` scharf schalten | SAP `T76/100`, **schreibend** auf `ZM_ABGLEICH_KTSCH` (Paket `ZPP1`, neuer Transportauftrag), neu `saptasks/zc12/` mit Quelltextkopie vorher und nachher, `.tmp_sap_probe/**`, `projektmanagement/PROJEKTSTATUS.md`, `docs/AGENT_COORDINATION.md` | In Arbeit seit 2026-09-04. Ingo hat auf die Rueckfrage „soll ich `p_debug` scharf schalten" mit „ja mach scharf" geantwortet. Geplant: Quelltext per `SapProbe abap-read` sichern, zwei Stellen aendern (`p_debug` entkommentieren, hartes `return.` in `trace_open` entfernen), mit `abap-check` syntaktisch pruefen und ueber `SapGuiSetReportSource.vbs` im Dialog zurueckschreiben. **Wartet auf die einmalige Passwortablage durch Ingo**, weil der ABAP-Editor ueber die Scripting-Schnittstelle nicht auslesbar ist. |

## Kurzindex der letzten sieben Tage

Abgeschlossene Arbeit seit dem 2026-09-02, eine Zeile je Eintrag. Der Volltext mit
Nachweisen, geaenderten Dateien und Fallen steht in
`docs/AGENT_COORDINATION_HISTORIE.md`.

| Agent | Bereich | Letztes Datum | Ergebnis in Kurzform |
|---|---|---|---|
| Claude | Finance Journal / Ausgleichsfelder (Clearing Date) aus B1 einbauen und in `Finance_All` ausweisen | 2026-09-09 | Abgeschlossen, Reservierung frei, **nicht deployed**. Auftrag von Ingo: „kannst du die felder einbauen, in Finance_All waere ja korrekt oder". Fuenf Felder je Buchungszeile: `ClearingDate`, `ReconciliationDate`, `ClearingReference`, `ClearingCount`, `IsClearingCancelled`. Zwei Live-Befunde haben die Umsetzung umgeworfen: `JDT1.IntrnMatch` ist in FR/IT/US durchgehend 0/-1 und unbrauchbar (Bruecke ist `ITR1.TransId`/`TransRowId`), und eine Zeile kann bis zu neunmal ausgeglichen sein, weshalb vor dem Join aggregiert wird. Produktive Query live gegengezaehlt: FR 20'198, IT 162'833, US 21'816 — identisch zur Basis, keine Vervielfachung. Indien nicht erreichbar. Geaendert: `Models/FinancialJournalEntry.cs`, `Services/DatabaseInitializationService.SchemaSql.cs`, `Services/DatabaseSchemaMaintenanceService.cs`, `Services/HanaFinancialJournalReader.cs`, `Services/SapGatewayFinancialJournalReader.cs`, `Tools/FinanceAll/finance_all_xlsx.py` samt Test, `TrafagSalesExporter.Tests/FinancialJournalTests.cs`, `docs/FINANCE_JOURNAL.md`, `docs/rag/FINANCE.md`, `docs/Issue_Log_Konsolidiert_2026-08-12.tsv`, `projektmanagement/Wochen_Todo.tsv` und `.xlsx`. Nachtrag: eine dritte Messung zeigte, dass `ReconNum` nicht in Datumsreihenfolge vergeben wird (FR 374 von 1'412), Nummer und Datum haetten also verschiedene Ausgleiche beschrieben; die Unterabfrage laeuft nun zweistufig. Tests 690/690 gruen plus Python-Exportpruefung. **Offen: die zweistufige Fassung ist noch nicht live gegengezaehlt**, weil das Firmennetz ausfiel — vor dem Deploy `.tmp_tools/JournalClearingProbe0909` erneut laufen lassen, ein HANA-Syntaxfehler war dort schon einmal aufgetreten und die Unit-Tests fangen ihn nicht. Ausserdem offen: Fachentscheid Andreas zu `date paid`, Messung Indien, Deploy und Ladelauf je Gesellschaft. |
| Claude | Finance / Entscheid Andreas zur Konzernkostenkaskade nachdokumentieren | 2026-09-09 | Abgeschlossen, Reservierung frei. Entscheid, akzeptierte Thermostat-Ungenauigkeit und zurueckgestellte zweite Stufe festgehalten; 688/688 Tests gruen nach der Kommentaraenderung. |
| Claude | (Absatzeintrag) | 2026-09-09 | ABGESCHLOSSEN einschliesslich Messung, Reservierung frei. Andreas Stoller fragt heute im Italien-Mailverlauf, ob sich je Artikel eine Referenzkost aus Bestandswert geteilt durch Bestandsmenge aus B1 Ital… |
| Claude | (Absatzeintrag) | 2026-09-09 | ABGESCHLOSSEN, Reservierung frei. RTF-Schriftmuell in der Alphaplan-Artikelbezeichnung. Ursache: NormalizeAlphaplanText entfernte die RTF-Steuerworte, aber nicht den Inhalt der Zielgruppen fonttbl un… |
| Codex | (Absatzeintrag) | 2026-09-09 | ABGESCHLOSSEN, Reservierung frei - Abschlussdokumentation zur produktiven DE-Kundenzuordnung und Rohail-Ausgabe. Nachgefuehrt: `docs/UEBERGABE_2026-09-08.md`, `docs/router/finance.md`, `docs/rag/FINA… |
| Codex | (Absatzeintrag) | 2026-09-09 | ABGESCHLOSSEN, Reservierung frei. DE-Belegbruecke produktiv (8a0cee6, 687/687 Tests, DLL bitgleich, vier Routen HTTP 200). Kundenfelder fuer 4'549 von 7'615 Zeilen nachgezogen, 19 DE-Railway-Kunden b… |
| Codex | (Absatzeintrag) | 2026-09-09 | Schluesselanalyse abgeschlossen, Reservierung frei. Sieben DE-Arbeitsmappen gegen Rohkoepfe und Sales-Stand 08.09. abgeglichen: 4'549 Zeilen direkt belegbar, 2'427 ueber historische ID-Bruecke ableit… |
| Codex | Bahnmarkt Deutschland: korrekte Kundenfelder produktiv laden und Bahnbranche bestaetigen | 2026-09-08 | App-seitig abgeschlossen und deployed am 2026-09-08 23:51, Reservierung frei; Quelllauf wartet auf DE. Funkti… |
| Codex | Bahnmarkt Deutschland: Kundenschluessel nach Ingos Live-Hinweis korrigieren | 2026-09-08 | Abgeschlossen am 2026-09-08, Reservierung frei. Screenshot, hinterlegter deutscher Excel-Header und App-Mappi… |
| Codex | CH/AT Journal: produktives EntitySet anbinden und DueDate im Gateway-Leser ergaenzen | 2026-09-08 | App-seitig abgeschlossen und deployed am 2026-09-08 15:12, Reservierung frei; SAP-seitig wartet CH/AT auf den… |
| Codex | Finance_All nach SharePoint am Sales_All-Ziel bereitstellen | 2026-09-08 | Abgeschlossen am 2026-09-08, Reservierung frei. `Finance_All_2026-09-08.xlsx` mit 469'708 Zeilen nach `Import… |
| Codex | Finance Journal / DueDate produktiv nachladen und Finance_All neu erzeugen | 2026-09-08 | Abgeschlossen am 2026-09-08, Reservierung frei. Produktiven UI-Lauf nach Deploy selbst ausgeloest: TRFR 20'17… |
| Claude | Deploy des Journal-Faelligkeitsdatums und Nachdokumentation | 2026-09-08 | Abgeschlossen am 2026-09-08. Reservierung frei. Auftrag von Ingo: „kannst du deployen, md nachfuehren und fin… |
| Codex Astra | Finance Journal / fehlende Konsolidierungsfelder vervollstaendigen | 2026-09-08 | Abgeschlossen am 2026-09-08, Reservierung frei. Nicht deployed. Live-HANA: DueDate in FR/IT/US/IN 469'661 von… |
| Codex | Journal-Import / sechs leere Felder ursächlich prüfen | 2026-09-08 | Abgeschlossen am 2026-09-08, Reservierung frei. Ursache je Feld gegen Modell, B1-/SAP-Reader, Schema, Finance… |
| Codex | Konzernkontenplan / heutigen Konsolidierungsstand parallel lesen und einordnen | 2026-09-08 | Abgeschlossen am 2026-09-08, Reservierung frei. Heutige Konsolidierungsdatei, Hauptdokument `FINANCE_JOURNAL.… |
| Claude | Bahnmarkt Deutschland, Journal Indien und `Finance_All` | 2026-09-08 | Abgeschlossen am 2026-09-08. Reservierung frei. Auftrag von Ingo aus dem laufenden Tagesgeschaeft: Rohail bra… |
| Claude | ZC12 / Umsetzungsstand richtigstellen (PM-02, **nicht** ZZPRDAT) | 2026-09-07 | Abgeschlossen am 2026-09-07. Reservierung frei. Auftrag von Ingo: „alles nachdokumentieren". ZZPRDAT ist ausg… |
| Codex Finance-Revie… | Finance-Dashboard vollstaendig fachlich und gegen Code pruefen | 2026-09-07 | Abgeschlossen am 2026-09-07, Reservierung frei. Bericht als Umsetzungsvorlage fuer ein einfacheres Modell ers… |
| Codex | ZZPRDAT / Abschluss vollstaendig nachdokumentieren | 2026-09-07 | In Arbeit am 2026-09-07 auf „bitte nachdokuemtnieren“. Bereits vorhandene Fachnotizen beibehalten; veraltete… |
| Codex | ZZPRDAT / verbleibende Reportfehler nach Gegenpruefung beheben | 2026-09-07 | Abgeschlossen am 2026-09-07. Reservierung frei. Ingo: „ansonst kannst du das fixe wenn nicht behoben“. Report… |
| Claude | ZZPRDAT / Freigabepruefung in `BEFORE_UPDATE` einbauen und nachmessen | 2026-09-07 | Abgeschlossen am 2026-09-07. Reservierung frei. Auftrag von Ingo: „ja mach das, wenn die neuen Aenderungen ni… |
| Claude | ZZPRDAT / Gegenpruefung eines zweiten Modells nachvollzogen, Abnahme angehalten | 2026-09-07 | Abgeschlossen am 2026-09-07. Reservierung frei. Ingo hat den Befund eines zweiten Modells vorgelegt. An den Q… |
| Claude | ZZPRDAT / Testanleitung fuer den Fachbereich ergaenzen | 2026-09-07 | Abgeschlossen am 2026-09-07. Reservierung frei. Ingo hat gefragt, ob ein Testvorgehen im Word steht. Es stand… |
| Claude | SAP / PM-02 ZC12: `p_debug` und das Tracing in `ZM_ABGLEICH_KTSCH` scharf schalten | 2026-09-04 | In Arbeit seit 2026-09-04. Ingo hat auf die Rueckfrage „soll ich `p_debug` scharf schalten" mit „ja mach scha… |
| Claude | SAP / PM-02 ZC12: Vorfrage und Regressionsfrage rein lesend klaeren | 2026-09-04 | Abgeschlossen am 2026-09-04. Reservierung frei. Auftrag von Ingo: „kannst du das thema mit dem verbesserten s… |
| Claude | Projektmanagement / Projektstand und Wochen-Todo auf den ZZPRDAT-Stand vom 2026-09-03 und… | 2026-09-04 | Abgeschlossen am 2026-09-04. Reservierung frei. Auftrag von Ingo: „aktualisiere projektmanagement stand excel… |
| Claude | Finance / TR IT Bewertungsmethode: Paolas Antwort vom 2026-09-04 nachfuehren (ISS-007.1) | 2026-09-04 | Abgeschlossen am 2026-09-04. Reservierung frei. Ingo hat Paolas Antwortmail weitergegeben und „ja bitte nachf… |
| Claude | Abnahme-Anschreiben ZZPRDAT und Doku-Nachfuehrung | 2026-09-04 | Abgeschlossen am 2026-09-04. Reservierung frei. Anschreiben an den Fachbereich verfasst: was vorher falsch wa… |
| Claude | SAP / ZZPRDAT MD04, CO41 und PP22 gemessen | 2026-09-04 | Abgeschlossen am 2026-09-04. Reservierung frei. Die drei bis dahin ungemessenen Punkte sind auf dem ZPP1-Stan… |
| Claude | SAP / ZZPRDAT in Paket ZPP1 und Transport gebaut | 2026-09-04 | Abgeschlossen am 2026-09-04. Reservierung frei. Die am 2026-09-03 in `$TMP` erprobte Loesung ist mit produkti… |
| Claude | SAP / ZZPRDAT geloest in T76/100 | 2026-09-04 | Abgeschlossen am 2026-09-03, nichts nach P76 transportiert. Reservierung frei. Uebernahme von Codex nach dess… |
| Codex | SAP / ZZPRDAT-Prototyp und CO01-Test in T76/100 | 2026-09-03 | Uebergeben/frei am 2026-09-03 fuer Fortsetzung mit Claude. In T76/100 lokal angelegt: `Z_ZZPRDAT_REL_TEST`, i… |
| Codex | SAP / ZZPRDAT-Vorpruefung im Testsystem | 2026-09-03 | Read-only-Vorpruefung abgeschlossen und Reservierung frei am 2026-09-03. T76/100 live verbunden. `AUFK-ZZPRDA… |
| Claude | Lokale Vektorsuche fuer die Projektdokumentation aufsetzen (Qdrant plus MCP-Server), dami… | 2026-09-03 | Abgeschlossen am 2026-09-03, nichts deployed. Auftrag von Ingo: „kannst du qdrant installieren und meine md d… |
| Codex | Plattform / Einkauf beschleunigen, Sicherheitsupdates testen und deployen | 2026-09-03 | Abgeschlossen, deployed und frei am 2026-09-03 08:20. Commits `756e931` und `2444731`. Gemeinsamer filterabha… |
| Codex | Projektmanagement / lesbare Word-Erlaeuterungen fuer offene Wochen-Todos erstellen | 2026-09-02 | Abgeschlossen am 2026-09-02. Lesbares Word-Dokument mit 24 nicht erledigten Punkten erstellt, nach Finance, S… |
| Codex | Projektmanagement / Erlaeuterungsblatt fuer alle nicht erledigten Wochen-Todos erstellen | 2026-09-02 | Abgeschlossen am 2026-09-02. In `Wochen_Todo.xlsx` das neue letzte Blatt `Erläuterungen` angelegt. Es enthael… |
| Codex | Finance / Entscheidungsbedarf bei Innenumsatz und Doppelzaehlung erklaeren | 2026-09-02 | Abgeschlossen am 2026-09-02. Entscheidungsfrage herausgearbeitet: Soll die fuehrende Umsatz-KPI die Summe der… |
| Codex | Projektmanagement / Spanien-Punkt auf erledigt setzen, Entscheid 7-Tage-Export plus Invoi… | 2026-09-02 | Abgeschlossen am 2026-09-02. ISS-004.2 im Wochen-Todo auf `Erledigt` gesetzt, Erledigungsdatum 02.09.2026. En… |
| Codex | Spanien / Ursache der 22 Zeilen ohne InvoiceDate untersuchen | 2026-09-02 | Abgeschlossen am 2026-09-02. Alle 22 Zeilen ohne InvoiceDate tragen `BillingStatus=0`, `InvoiceYear=0`, leere… |
| Codex | Spanien / aktuellen Rueckblick der gestrigen und vorgestrigen Exporte sowie Relevanz von… | 2026-09-02 | Abgeschlossen am 2026-09-02. Paketbefund: korrektes `Run-SpainRangeExportAndUpload-AllInOne.ps1` selektiert p… |
| Codex | Projekt-Dashboard im Chat anzeigen | 2026-09-08 | Abgeschlossen am 2026-09-02. Projekt-Dashboard erneut nach Themen sortiert und Finance zusammengefasst. Aktue… |
| Claude | Spanien / veralteten Doku-Stand zum Buchungsdatum aktualisieren (ISS-004.2) | 2026-09-02 | In Arbeit seit 2026-09-02. Auftrag von Ingo: „ja alles soll aktuell sein". Ausloeser war seine Rueckfrage, ob… |
| Claude | Finance / Widerspruchssuche Code gegen Doku, danach Reparatur M2 und M3 | 2026-09-02 | In Arbeit seit 2026-09-02. Erst read-only Widerspruchssuche auf Auftrag von Ingo („liste alles auf, nichts pr… |
| Claude | Finance / Workflow Lieferantenfeld und Standardkostenlogik als SVG, danach Live-Nachmessu… | 2026-09-02 | Abgeschlossen am 2026-09-02, nichts deployed. Auftrag von Ingo: die Lieferantenklassifikation und die daran h… |
| Codex | Finance / Ursachen der Supplier-Feldluecken fuer Indien, Frankreich, USA und CH/AT aus Do… | 2026-09-02 | Abgeschlossen am 2026-09-02. Doku bestaetigt ein Quellen-/Mappingproblem statt einzelner unvollstaendiger Sup… |
| Codex | Finance / Erledigt-Status und Laenderabdeckung ab 2025 in Sales_All 2026-09-01 pruefen | 2026-09-02 | Abgeschlossen am 2026-09-02. Read-only 105'282 Verkaufszeilen geprueft. Alle neun TSC haben in 2025 jeden Mon… |
| Codex | FinanceDashboard-Dokumentation lesen und zusammenfassen | 2026-09-02 | Abgeschlossen am 2026-09-02. Prozessablauf vollstaendig gelesen: operative Quelle sind die neuesten Standort-… |
| Claude | Projektmanagement / Railway-Auswertung mit Termin 2026-09-08 aufnehmen | 2026-09-08 | Abgeschlossen am 2026-08-27. Auftrag von Ingo: Rohail Munir aus Deutschland braucht den Railway-Export bis sp… |
| Claude | Projektmanagement / strukturierte Aufgaben- und Verantwortungsuebersicht fuer den neuen I… | 2026-09-02 | Abgeschlossen am 2026-09-02. Auftrag von Ingo: neuer Vorgesetzter ad interim fuer sechs Monate, 75 Jahre, zug… |

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
