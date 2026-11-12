# Doku-Pruefung aller Markdown-Dateien vom 2026-11-12

Stand: 2026-11-12 (Pruefung und Einarbeitung der eindeutigen Befunde abgeschlossen; Entscheide Ingo offen).

Auftrag Ingo 12.11. 11:45: „pruefe alle md ob alles passt: keine Redundanzen, nicht veraltet, Gesamtbild stimmt, keine
Bugs, Konsistenz“. Vorgaenger: `docs/MD_KONSISTENZPRUEFUNG_2026-09-29.md`.

## 1. Vorgehen

1. **Mechanisch (Skript, nur lesend):** `baum.md` gegen `git ls-files '*.md'` in beiden Richtungen, Pfadverweise in
   Backticks, zitierte Commit-Nummern, Kopfzeile „Stand:“ gegen das juengste Datum im Text, Spaltenzahl und Statuswerte der
   TSV-Dateien, Tageszeit-Etiketten, „nicht deployed“ neben ausgelieferten Commits.
2. **Fuenf Pruefer, je Themenast, nur lesend:** Finance/Standortdaten, Einkauf/Logistik, Plattform/HR/Operations, SAP/ABAP,
   Kern (Regelwerk, RAG, Statusquellen, `baum.md`). Jeder Befund mit woertlichem Zitat, Gegenbeleg und Vorschlag, in drei
   Klassen: A eindeutig belegt, B braucht Ingos Entscheid oder eine Live-Pruefung, C Redundanz und Struktur.
3. **Je Ast ein zweiter, unabhaengiger Agent** hat jeden A-Befund nachgelesen (Zitat und Beleg) und nur Bestaetigtes
   eingearbeitet. Wo der Beleg nicht hielt, wurde nicht oder vorsichtiger geaendert.
4. **Hauptsitzung:** eigene Fehler vom 12.11. vorab berichtigt, die Diffs der RAG-, Router-, Deploy- und SAP-Regeldateien
   gelesen, Stichproben gegen den Code, Commits je Ast.

Nicht geprueft wurden `docs/archiv/**` und `docs/AGENT_COORDINATION_HISTORIE.md` (Historie). Grosse datierte Detaildateien
sind teils nur ueber Kopf, Gliederung und Stichwortsuche gelesen; die Liste je Ast steht am Ende der Befunddateien (nicht
versioniert, Scratchpad der Sitzung). Live-Staende (SAP, Produktion) wurden nicht nachgemessen: Transport- und
Deploystaende stammen aus Wochen_Todo, Status_kurz und datierten Nachtraegen.

## 2. Zahlen

| Ast | Befunde | A | B | C |
| --- | ---: | ---: | ---: | ---: |
| Einkauf und Logistik | 45 | 33 | 8 | 4 |
| Kern, Regelwerk, Statusquellen | 40 | 21 | 11 | 8 |
| SAP und ABAP | 42 | 35 | 5 | 2 |
| Plattform, HR, Operations | 45 | 35 | 7 | 3 |
| Finance und Standortdaten | 45 | 32 | 8 | 5 |
| **Summe** | **217** | **156** | **39** | **22** |

Mechanisch: alle zitierten Commit-Nummern existieren; die TSV-Dateien haben durchgehend die richtige Spaltenzahl; 41 Dateien
hatten einen Kopfstand, der aelter war als Nachtraege im Text (je Ast nachgeprueft, wo der Kopf etwas verschwieg).

## 3. Eingearbeitet

| Commit | Inhalt |
| --- | --- |
| `c55df28` | Eigene Nachtraege vom 12.11.: ISS-003.3 DE-Schritt wiederhergestellt, Uhrzeiten statt „nachmittags/abends“, ZC12-, Spanien- und Deploystaende, `lastchange.md` oben neu gefasst, Router und RAG PROJECT/PURCHASING, Agentendefinitionen „keine Anmeldeseiten“ |
| `f7f0cf7` | SAP: Arbeitsweise (Passworteingabe, Sitzungsindex 0, Gateway-Cache-Reihenfolge als ueberholt markiert; neuer Unterabschnitt „Gateway-Caches“ aus vorhandenen Quellen), Router, Transportstaende |
| `043eae7` | Einkauf: RAG PURCHASING (Abschnitt Materialanteil, FIX 26/22, Kontrakte, Altposten, Logistik live), Router (Nachpflege ohne Full Load), ZLO03-FIX-Dateien, Handbestellwesen |
| `c77d26a` | `zlo03/CLAUDE.md` Fix-Tabelle auf P76-Stand, Agentendefinition Einkauf (Basismenge seit FIX 26, Belegkurs statt Kurstabelle); Freigabe Ingo |
| `13ba643` | Finance: RAG FINANCE (Budgetkurs als Standard, ISS-007.4, ISS-032, ISS-003.3), FORMELN, MANUAL_IMPORT, Detaildateien |
| `4ac3746` | Agentendefinition Finance: Spanien-Ausnahme der Jahresabgrenzung |
| `daf8df0` | Plattform/HR: Agentendefinition HR (Rexx-Hub-Stand, vom Nachpruefer geaendert), RAG ADMIN mit aktuellem Kurzstand, Deploy-Doku auf `CLAUDE.md` 5, Rexx Hub Produktivstand, Weltlage hinter Finance-Login, Routerluecken |
| `9e27a70` | Gemeinsame Dateien: LEARNINGS, PROJECT, init, `router.md`, `baum.md`, Issue-Log, Projektlisten, dieser Bericht |

Wo der zweite Agent dem ersten nicht gefolgt ist (Auswahl): Importtag 29.10. fuer `T76K912742`/`752` ist nirgends belegt
(eingetragen „laut Ingo, Importliste P76 vom 10.11.“); `<>` in `table-read` nur mit Warnvermerk, nicht auf `NE`
umgeschrieben; alle vier `candidate_objects.csv` sind bytegleich (kein „vollstaendigerer Auszug“); `KOM_MSTAE` als
Elternmaterial ist nur fuer T76 belegt; FIX 27 behebt den Wafer-Fehler nicht, nur FIX 26.

## 4. Offene Entscheide fuer Ingo (Klasse B)

### Sofort

| Nr | Thema | Frage |
| --- | --- | --- |
| P29 | `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` Zeile 13 | Das initiale Admin-Passwort steht im Klartext in einer versionierten, gepushten Datei. Gilt es noch (Wochen_Todo 102 „In Arbeit“)? Falls ja: auf Admin > Passwoerter aendern; danach die Zeile aus der Datei nehmen (aus der Git-Historie verschwindet es dadurch nicht) |
| F30 | Spanien, Mail Gang 1 an Santi | Welches Skript ruft Santis taeglicher Task auf (All-in-One 35 Tage oder 7-Tage-Fassung)? Abschnitt 3 und 7 von `docs/STANDORT_ES_SAGE.md` widersprechen sich; die Frage gehoert in die Mail, bevor sie hinausgeht |

### Regelwerk und SAP-Arbeitsweise

| Nr | Thema | Frage |
| --- | --- | --- |
| K1, K2 | `CLAUDE.md` Abschnitt 3a und Ausnahme in 2 | Seit 29.10. nur im Arbeitsbaum, nie committet; vier committete Dateien verweisen darauf. Committen? Dabei „im Volltext in der Historie“ berichtigen |
| K8 | `docs/rag/init.md` gegen `CLAUDE.md` | Die Kurzform kennt den Qdrant-Reindex als Abschlussschritt, die verbindliche Fassung nicht. In `CLAUDE.md` 3 aufnehmen oder aus `init.md` streichen? |
| K24 | Kostenregel und Anrede | Modellwahl steht in `AGENTS.md` als feste Vorgabe, in `persona.md` als abgeschaltet; „Captain“/duzen steht nur in den Agentendefinitionen. Wo soll es gelten? |
| S8 | `docs/router/sap.md` Rangfolge | „Zuerst ein ABAP-Report, den Ingo ausfuehrt“ (Stand September) oder RFC-Lesen als erste Wahl? |
| S20 | GUI-Scripting | Bleibt es dauerhaft an, oder wird es nach jeder Arbeit abgeschaltet, wie die Arbeitsweise verlangt? |
| S21 | KOI-RFC-Rechte | „KOI kann per RFC alles“ gegen `PERMISSION_ERROR` bei `abap-write`: welche Aussage stimmt? |
| S24 | Bereinigung `.tmp_sap_probe` | Die Zahl „14 ungeschuetzte Skripte“ ist nirgends belegt, die Textsuche findet acht; Plan dazu (auch `P76Pruefung_*.ps1` loeschen, `Read-Host`-Zweig) |
| S27 | `travp762` | Laeuft das OData in P76 ueber `travp762` oder gibt es nur `trasp76s`? |
| neu | P76-Caches | Agentendefinition: „in P76 nie Caches leeren“; am 02.11. und 09.11. auf Ingos Auftrag geschehen. „Nie“ oder „nur auf ausdruecklichen Auftrag“? |
| K4 | LEARNINGS | Zwei Regelpaare widersprechen sich (Sitzungswahl ueber Index; Shell-Ersetzungen verboten oder mit Vorsicht) |
| K13 | LEARNINGS, Groesse | 106 KB Pflichtlektuere, viele Doppelungen, SAP-Zeilen in allen Abschnitten: verdichten? |

### Statusquellen und Projektlisten

| Nr | Thema | Frage |
| --- | --- | --- |
| K33 | PM-13 (SAP PS) | Fehlt in `Vorhaben_HERMES.tsv`, Wochen_Todo 115 und 126 verweisen darauf; aufnehmen? (fertige Zeile liegt vor) |
| K34 | Wochen_Todo | 33 offene Zeilen ohne Vorhaben fehlen im HERMES-Blatt; Regel festlegen und nachtragen? |
| K37 | `Status_kurz.tsv` (Chef-Sicht) | Handbestellwesen, IC-Preisfindung, Spanien Supplier, Lieferantenbewertung fehlen als Punkte |
| K26 | Issue-Log gegen Wochen_Todo 40 | „versendet“ gegen „nur Entwurf“: was stimmt? |
| K15 | Koordination | Zeile Zeichnungsaenderung steht seit 02.11. unter „Aktive Bereiche“, obwohl nichts laeuft; verschieben? |
| K21 | Geloeschte Dateien | `segmente.png`, `login.png`, `manometer.png`, `Bahnmarkt_DE_Kundenzuordnung_2026-09-09.xlsx` sind im Arbeitsbaum geloescht, nicht committet; wiederherstellen oder Loeschung committen? (auch F39: zitierte Belegdateien) |
| F26 | Issue-Log | ISS-021 und ISS-003.1 erledigt mit offenem Schritt; ISS-007 und ISS-029 seit Wochen ohne Update |
| F18 | Deutschland | `alphaplanExport.ps1`, `alphaplandeltaexport.ps1`, `fullquery.sql` sind nicht versioniert, die Doku sagt „in diesem Repo“ |
| F20 | `PRODUCT_MAPPING.md` | Methodendatei vom 10.06. im Repo: entspricht sie der in SAP aktiven Methode? |

### Einkauf, HR, Plattform

| Nr | Thema | Frage |
| --- | --- | --- |
| E6, E38 | Lagerwert und WKURS | MB5L-Abgleich und WKURS-Richtung sind offen, aber in keiner Statusquelle gefuehrt (Wochen_Todo 23 „Erledigt“): Zeile anlegen oder bewusst schliessen? |
| E22 | `zlo03/CLAUDE.md` | Transportziel B76 oder P76? |
| E36 | Termintreue, Lieferanten-Factsheet | Gehen sie in Paket 1 der Lieferantenbewertung auf? |
| P9 | `docs/router/hr.md` | Begruendung nennt die Austrittsdatei, die seit 04.11. nicht mehr gelesen wird |
| P41 | Nutzungsmessung | Messfrist seit 11.11. abgelaufen, keine Auswertung dokumentiert |
| P42 | Trafag Projekte | Fachdatei endet am 29.10.; PM14 und die Frage „Projektmanagement deaktivieren?“ fehlen |
| P32 | Ansprechpartner | Rolle von Ramon Castillo (einmal genannt, ohne Rolle) |
| P19 | SAP-OData-Adresse | `.29` in den Deploy-Dateien gegen `.30` in REQUIREMENTS und Firewall-Liste: welche gilt? |

### Handbuch (wird mit der App ausgeliefert, Aenderung braucht Deploy)

| Nr | Kapitel | Befund |
| --- | --- | --- |
| E40, P43 | 02 Einkauf | Matrix Lieferant/Jahr an zwei Stellen noch als vorhanden beschrieben |
| E41 | 02 Einkauf | „sobald `ZloTopDownSet` in P76 ist“ (ist es seit 02.11.) |
| E42 | 02 Einkauf | „Nachts laeuft ein Delta“ (laeuft im Timer-Slot nach dem Verkaufsexport); gleicher Text in `PurchasingDataSources.razor` |
| E45 | 02 Einkauf | ABC/XYZ „nur so vollstaendig wie der letzte Full Load“ (stimmt seit dem Delta-Fix nicht mehr) |
| F40 | 01 Finance | Glossar und Pruefbuch nennen noch den Kurs zum 31.12. statt des Budgetkurses |
| F42 | 01 Finance, Schulungsseite | verlangen fuer ES/DE „vollstaendige Dateien“, obwohl Deltas gelesen werden |
| F41 | 03 Verkauf | Monthly Review Gesellschaften fehlt |
| P44 | 10 Operations | Auftragsvorrat aus SAP (09.11.) und Schreibschutz Patch 07 fehlen |
| P45 | Einleitung | Standangabe endet am 29.10.; Handbestellwesen, Datenverantwortung, Monthly Review fehlen |

## 5. Vorschlaege zu Redundanz und Struktur (Klasse C)

| Nr | Vorschlag |
| --- | --- |
| K10 | Die Koordinationsregel steht sechsmal in eigenem Wortlaut (`CLAUDE.md`, `AGENTS.md`, `router.md`, `projekt.md`, `init.md`, Koordinationsdatei): eine fuehrende Stelle, sonst Verweise |
| K11 | `router.md` Regel 2 nennt den Issue-Log als Statusquelle schlechthin; er fuehrt nur Finance- und Datenpunkte. Statusquellen je Bereich benennen (Wochen_Todo, Issue-Log, PROJEKTSTATUS) |
| K19 | `docs/rag/PROJECT.md` ist ein Tagesprotokoll, keine Uebersicht je Bereich; ganze Bereiche fehlen |
| K22 | `lastchange.md` hat wieder 121 KB; Abschnitte vor dem 01.11. ins Archiv |
| K35, K36 | Statusvokabular in Wochen_Todo (13 Werte) und Issue-Log (23 Werte) vereinheitlichen und definieren; das Skript faerbt „In Arbeit“ nicht |
| K40 | `baum.md` verspricht „genau einmal“; elf Dateien stehen zusaetzlich in der Tabelle „zusammengefuehrt“ |
| S23 | Skripttabellen der SAP-Arbeitsweise nennen ersetzte Skripte weiter als Werkzeug |
| S31 | SAP-Regeln stehen in allen vier Abschnitten von LEARNINGS |
| E8 | `docs/rag/PURCHASING.md`: Kopfzeile ist eine Chronik von rund 6'000 Zeichen; Rohquellen-Liste zeigt fuenfmal auf dieselbe Datei |
| E33 | `docs/PURCHASING_DASHBOARD_2026-06-05.md` heisst „laufende Hauptdoku“, endet aber am 26.10. und hat keine Stand-Zeile |
| E43 | Transportstand jedes ZLO03-Fixes an bis zu sieben Stellen: fuehrend die Fix-Tabelle in `zlo03/CLAUDE.md` |
| E44 | Materialanteil je Produktgruppe auf vier Dateien verteilt: fuehrend der Bauplan |
| P6 | `docs/rag/ARCHITECTURE.md`: zwei Kurzstand-Punkte haengen am Dateiende |
| P15 | `docs/router/plattform.md`: 367 von 419 Zeilen sind Werkzeugreferenz und Fallberichte; in Detaildateien auslagern |
| P20 | `docs/rag/DEPLOYMENT.md`: Kurzstand fuehrt Deploys, die aelter als zwei Wochen sind (eigene Regel) |
| F21, F22 | Router Standortdaten/Finance: Italien-Eintrag zeigt auf ueberholte Notiz; vier Dateien ohne Routereintrag |
| F43 | Spanisches Buchungsdatum fast wortgleich in vier Dateien |
| F44 | `docs/rag/FINANCE.md` traegt ueberwiegend Messhistorie von August/September |
| F45 | Provider und `Z_PRODSPARTE_MAP_BUILD` liegen doppelt im Repo (`docs/abap/` und `spartenlogic/`) |

## 6. Nebenbefunde im Code (fuer das Code-Review)

- `Models/ConfigTransferPackage.cs:62` hat als Vorgabe `CurrentDailyRate`, `Models/ExportSettings.cs:52` steht auf
  `BudgetRate`; ob das beim Konfigurationsimport wirkt, ist nicht geprueft.
- `Components/Pages/PurchasingDataSources.razor` sagt „Nachts laeuft ein Delta“ (siehe E42).
- Nach dem Deploy von `c9208b5` und Gang 2 bei Santi sind drei „nicht deployed“-Stellen nachzufuehren:
  `docs/rag/FINANCE.md` (Offene Fachpunkte), `docs/rag/MANUAL_IMPORT.md` (Skripthoheit ES), Kopf von
  `docs/PLAN_ES_SUPPLIER_WARENEINGANG_2026-11-12.md`.
