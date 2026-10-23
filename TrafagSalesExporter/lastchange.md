# Last Change

Stand: 2026-10-23

## Einkauf: Spend-Aufriss Produktgruppe : Warengruppe, 2026-10-23

- Zweiter Produktgruppen-Einstieg Produktgruppe -> Warengruppe -> Material (Wunsch Einkauf, Budgeterstellung); der
  bisherige heisst jetzt „Produktgruppe : Lieferant“. Gleiche Summen, gleiche 1/n-Verteilung. Suite 1288/1288. Deployed 2026-10-23 07:46.

## Whisper Studio: neue Oberflaeche, lokale KI, `whisper/` versioniert, 2026-10-22

- Auftrag Ingo: Fortschrittsanzeige, dann Ausbau zum „Whisper Studio“ mit moderner Oberflaeche. Entscheide Ingo:
  Dateiname `JJJJ-MM-TT_HHMM_Titel`, KI nur lokal, Funktionen Aufnahme, Word/SRT/VTT, Editor und Verlauf, KI-Protokoll.
- Neu `whisper/gui/WhisperStudio` (WPF, Fluent) und `whisper/gui/WhisperStudio.Tests` (31 Tests gruen, darunter echte
  Laeufe von KI, Whisper und Aufnahme). `transcribe.py`/`.ps1`: Fortschrittszeilen, `-Task translate`, `.json` mit Segmenten.
- Lokale KI: llama.cpp `llama-server` b11799 und Qwen2.5-3B-Instruct Q4 (2,1 GB) in `whisper/llm/` (nicht versioniert).
  Protokoll eines kurzen Testtextes in 25 s.
- Entwickelt in der Kopie `.tmp_tools/whisper_kopie`, weil im Original ein Lauf aktiv war; uebernommen per Skript mit
  Zip-Sicherung `whisper/backup/whisper_quellen_20261022_130852.zip`. Seither sind die Quellen von `whisper/` in Git.
- Nicht geprueft: Bedienung mit der Maus (Drag&Drop, Tabelle bearbeiten, KI-Knoepfe). Kein Deploy, Werkzeug laeuft lokal.
- Nachtrag: Alte WinForms-GUI `whisper/gui/WhisperGui` auf Wunsch Ingo entfernt (Quellen bis `ae599c2` in Git), die
  Desktop-Verknuepfung „Whisper“ startet jetzt das Studio.
- Nachtrag: Aufnahmeseite mit analogem VU-Meter (Zeiger, roter Bereich ab 0 VU, PEAK-Lampe ab -1 dBFS) statt Pegelbalken,
  Wunsch Ingo. `Controls/VuMeter.cs`, `Recorder.Measure` (Spitze und RMS), 35 Tests gruen, Release neu gebaut.
- Details: `whisper/HANDOFF.md`.

## Doku: Markdown-Aufraeumen, Archiv `docs/archiv/`, 2026-10-22

- Auftrag Ingo: alle Markdown-Dateien aufraeumen, keine Redundanzen, keine Informationen verlieren. Nur Doku,
  kein Code, kein Deploy. Entscheide Ingo: neues Archiv `docs/archiv/` statt Wiederherstellung von
  `docs/raw_md_archive/` (bleibt nur in Git); `rag/DEPLOYMENT.md` behaelt Deploys ab 2026-10-13.
- Verschoben, jeweils verbatim und mit Zeilenvergleich belegt (jede nichtleere Originalzeile ist in Haupt- oder
  Archivdatei vorhanden): 51 Kurzindex-Zeilen vor dem 2026-10-15 aus `docs/AGENT_COORDINATION.md` in die
  Historie (146 -> 95 Zeilen); 58 Abschnitte 2026-07-30 bis 2026-09-30 aus dieser Datei nach
  `docs/archiv/LASTCHANGE_ARCHIV_2026-08-01_bis_2026-09-30.md` (2'643 -> 560 Zeilen); 101 Kurzstand-Eintraege
  2026-08-03 bis 2026-10-09 aus `docs/rag/DEPLOYMENT.md` nach `docs/archiv/DEPLOYMENT_KURZSTAND_ARCHIV_bis_2026-10-12.md`
  (1'440 -> 270 Zeilen, Fallen bleiben).
- Befunde (Doku-Drift, in LEARNINGS): die Historie hatte seit dem 2026-09-30 keinen Volltext mehr, obwohl
  Hauptdatei und `router.md` ihn versprachen; `rag/DEPLOYMENT.md` behauptete „nur den aktuell verifizierten
  Deploy“ bei 1'340 Zeilen Historie; drei Deploy-Eintraege (10.09., 09.09.) trugen Reste eines PowerShell-Strings
  (`" . "`) am Zeilenanfang (im Archiv so belassen, vermerkt); `baum.md` fehlten 17 versionierte Dateien
  (Handbuchkapitel, `saptasks/*/NOTES.md`, Berechtigungsfall AT) und fuehrte `docs/rag/init.md` als nicht versioniert.
- Nachgefuehrt: `baum.md` (Ast Projekt nach Themen aufgeteilt, Abschnitt Benutzerhandbuch, ZLO03, Archiv),
  `router.md` Regel 6, `docs/rag/init.md`, `docs/router/projekt.md`, `docs/rag/{ARCHITECTURE,FINANCE,PROJECT}.md`
  (Archivverweise), `docs/DEPLOYMENT.md` (Sicherungsregel seit 13.10. als ueberholt markiert), `docs/archiv/README.md`.
- Nicht angefasst, als Folgearbeit benannt: die Koordinations- und Abschlussregel steht viermal (`CLAUDE.md`,
  `AGENTS.md`, `router.md` Regel 6, `docs/router/projekt.md`); `CLAUDE.md` war von einer anderen Sitzung geaendert.

## HR-Cockpit: Kader = SAP-Mitarbeiterkreis 10, 8,2 h, 2026-10-22

- Entscheid Ingo: Kader ist der Mitarbeiterkreis 10 „ML mit Kadervertrag“ (T503T) mit 8,2 h je Tag statt Rexx „Leitung j/n“
  mit 8,1 h. Wirkt auf FTE aus Sollzeit, Krankheitstage und Langzeitgrenze. Suite 1288/1288. `docs/HR_KPI.md` 8.7.

## Handbestellwesen: moderne Oberflaeche /bestellwesen, 2026-10-22

- Neue Seite mit MudBlazor, gleiche Logik: Kacheln, Kreditor-Suche, Live-Pruefung der Berechtigung, Freigeber-Vorschlag,
  Doppelbestellungs-Hinweis, Nummer kopieren, Filter offene, Stammdaten/Nutzung fuer Admin. Deploy 14:52 (`d7478cd`), ohne
  Alarm. Konzept 6.6.

## Handbestellwesen: Datenpruefung und Bereinigung, 2026-10-22

- Nach Ingos Import (14:27) geprueft: keine doppelten Nummern; 25 Doppelklick-Doubletten, rund 930 offene aelter als 1 Jahr.
  Admin-Kachel „Bereinigung“ (Vorschau, Doubletten stornieren, alte offene ausbuchen, protokolliert). Deploy 2026-10-22 14:40 (37e5d7b), bitgleich, zehn Routen 200, Alarm nur WAL/SHM-Checkpoint beim Herunterfahren.

## Handbestellwesen: Altdaten-Import, 2026-10-22

- Deployed 14:22 (`3609b3e`), bitgleich, Alarm nur SHM. Dump liegt auf dem Server unter `hbdaten\import\db.sql`; Pruefen und
  Importieren macht Ingo unter `/handbestellwesen/import`.

- Admin-Kachel „Altdaten-Import“ (Pruefen, dann Importieren in leere DB), Fehler der PHP-Daten korrigiert statt uebernommen
  (Platzhalter, Text im Betrag als `WertText`, Datumstexte, Doubletten, Zaehler). Probelauf echter Dump: 12'629 Bestellungen.
  Konzept 6.3. Suite 1285/1285.

## Deploy 2026-10-22 13:38: Einkauf-Feedback und Modul Handbestellwesen

- `77778a5` produktiv nach Ende des Einkauf-Deltas, Wochensicherung, bitgleich, zehn Routen 200, ohne Alarm. Reiter
  Handbestellwesen unsichtbar angelegt. Dump (`db.sql`, 8 Tabellen) und neueste PHP-Dateien erhalten, Befund im Konzept 6.2
  (Basic-Auth-Datei heisst `htaccess` ohne Punkt, Schutz nicht aktiv).

## Einkauf: Feedback Erich/Sandro/Marco umgesetzt, 2026-10-22

- Kontraktpositionen „vermutlich ersetzt“ (46009671 Pos. 10, insgesamt 4 Pos. CHF 1,29 Mio.) nicht mehr im offenen Wert;
  Filter abgelaufene Kontrakte; Altposten (Textpositionen, Termin > 1 Jahr vorbei, 168 Pos. CHF 3,1 Mio.) nicht mehr
  offen/ueberfaellig; Hinweis „Zeitraum nicht angewendet“ und „Rechnet“; Lagerwert je Disponent; Top-Spend-Zeile weg;
  Bestellbedarf & Deckung, Materialabhaengigkeit, Interaktiv ausgeblendet (Verwendungs-Cache nur 5 Materialien),
  „Gehirn“ eingeklappt. Reiter Handbestellwesen wird unsichtbar angelegt. Suite 1279/1279.
  `docs/PURCHASING_DASHBOARD_2026-06-05.md` Nachtrag 2026-10-22.

## Einkauf: Handbestellwesen lebt weiter, Nutzung wird festgehalten, 2026-10-22

- Entscheid nach der Sitzung (Ingo): weiterbetreiben, aber festhalten, wer es wann wie viel braucht. Modul ergaenzt:
  Aufruf je Konto und Tag im Protokoll, Kachel „Nutzung“ (nur Admin) mit Auswertung je Person, Kostenstelle und Monat.
  Test neu, Suite 1275/1275. Nicht deployed.

## Einkauf: Sitzung 2026-10-22

- Ergebnisse festgehalten (`docs/EINKAUF_SITZUNG_2026-10-22.md`): Einkaufs-Dashboard bis 28.10. stabilisieren, keine
  neuen Funktionen; Feedbackliste Erich/Sandro/Marco kommt; neuer Wunsch Lieferantenbewertung/-entwicklung (Pilot
  danach, Datenquellen offen); Handbestellwesen: Abschalten statt Abloesen steht im Raum, Entscheid offen.
  Wochen_Todo 120 angepasst, 121 und 122 neu.

## Einkauf: Handbestellwesen, Konzept zur Abloesung der PHP-Anwendung, 2026-10-22

- **Phase 1 programmiert, nicht deployed:** neuer Reiter `/handbestellwesen` (Kacheln und Farben wie im PHP), Modul
  `Modules/Handbestellwesen/`, eigene DB `hbdaten/handbestellwesen.db` mit Sicherung, Limit/Freigabe/Wareneingang/Loeschen
  auf dem Server geprueft, Protokoll je Aktion. Tests 9 neu, Suite 1274/1274. Import der Altdaten folgt mit dem Dump.

- Alte PHP-Anwendung analysiert (Passwoerter im Code, SQL-Injection, Freigabelimit nur im Browser, privater Hoster,
  Nummernkollision). Entscheide Ingo: Modul im Dashboard, Altdaten uebernehmen, Stammdaten erst lokal dann SAP,
  Oberflaeche wie im PHP. Nachtrag: Betrieb lokal bei Trafag (nicht externer Hoster), Benutzer ohne SAP-Konto, Plugin/KISS,
  AD, spaeter Schreiben nach SAP (Lizenzfrage). Kein Code. Wartet auf MySQL-Dump. Wochen_Todo 98 geschlossen (Delta 14.10.
  war erfolgreich). `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md`;
  `handbestellwesen/` in `.gitignore`.

## SAP: ZLO03 FIX 20, „defekte Excel“ ist eine Excel-Sperre, Download als .txt, 2026-10-22

- Meldung Ingo: ZLO03 erzeugt seit einiger Zeit defekte Excel. Befund: Die Datei ist Tab-Text unter `.xls`; Excel weist
  das ab, seit die Gruppenrichtlinie `ExtensionHardening = 2` gilt (auch alte Dateien vom August). Als `.txt` oeffnet
  dieselbe Datei korrekt (331 x 2'097). Sofortloesung: umbenennen in `.txt`.
- FIX 20 in `Z_ZLO03_TURBO2` (`download_dialog`): Vorschlag `.txt`, Oeffnen mit `excel.exe`, Rueckfall Standardprogramm.
  T76 aktiv, rueckgelesen identisch, Lauf `8475*` oeffnete Excel direkt, Inhalt byteweise gleich. Auftrag `T76K912742`.
- Offen: welches Programm ZLO03 in P76 startet (ungeprueft), andere Z-Reports mit gleichem Muster.
  Doku `zlo03/FIX20_DOWNLOAD_TXT_2026-10-22.md`.
- Nachtrag: `T76K912742` am 22.10. von Ingo freigegeben (mit FIX 12, 19, 20). Auf Wunsch Ingo FIX 21 vorbereitet
  (echtes `.xlsx` per `CL_ABAP_ZIP`, Prototyp zellgleich mit dem Text-Import). Danach auf Auftrag Ingo eingespielt:
  neuer Auftrag `T76K912752`, aktiv, rueckgelesen, Lauf oeffnet Excel direkt, 0 Unterschiede zum Textinhalt, Lasttest
  722 x 3'114 Zellen in 14 s. Import nach `T76K912742` (Ingo).

## SAP: ZLO03 Spalte RoHS (Nachtrag FIX 19), 2026-10-22

- Adil hat in T76 das Merkmal `ROHS` angelegt („RoHS Compliant“, CHAR 3, COM/UNC/UND, Klasse `Z_PACK_REGULATION`).
  ZLO03 (`Z_ZLO03_TURBO2`) gibt jetzt vier Spalten hinter `Postyp` aus: RoHS, REACH, SVHC, PFAS-Gehalt.
- T76 aktiv (22.10. 08:00), rueckgelesen identisch, im offenen Auftrag `T76K912742`. Lauf `8475*` fehlerfrei, 182 Spalten,
  Werte leer (T76 ohne gepflegte Materialien). Ob `ROHS` in P76 existiert, ist ungeprueft.
- Doku `zlo03/FIX19_STOFFCOMPLIANCE_SPALTEN_2026-10-20.md` Abschnitt 6, Quelltext `zlo03/Z_ZLO03_TURBO2_FIX19_ROHS_2026-10-22.abap`.

## SAP PS: Rolle je Zeitbuchung (Frage Philip Steiger), Demo T76, 2026-10-21

- Antwort: Rolle als eigene Leistungsart (gleicher Tarif moeglich). In T76/100 auf Freigabe Ingo: KL01 `PL`, `ENG`, `TST`, KP26
  66 CHF/h auf Kostenstelle 240, KB21N Beleg 311258908 (Personalnummer 3216: 2 h PL, 4 h ENG, 3 h TST), CJI3 zeigt Rolle,
  Stunden und Person. Doku Abschnitt 4b in `docs/SAP_PS_EINFUEHRUNG_DEMO_2026-10-20.md`, Bilder 19-22, Folien 17-19 in
  `20260223 - Abgleich TPG - Trafag SAP PS.pptx` (nicht versioniert).

## Status: Wochen_Todo, Abgleich Code gegen Doku, Qdrant-Index, 2026-10-21

- Code-Commits seit 01.10. gegen Doku geprueft: nach dem letzten Deploy `c3c45cd` (19.10.) gibt es keinen App-Code; produktiver Stand
  und HEAD stimmen ueberein. Einzige Abweichung ist die fremde, unkommittierte Finance_All-Automatik (Wochen_Todo 117, Entscheid Ingo).
- Qdrant-Index neu aufgebaut: 4245 Abschnitte statt 3884, vorheriger Stand 13.10. Reindex-Skript bricht bei umgeleiteter
  Fehlerausgabe ab (Wochen_Todo 118, LEARNINGS). Zeile 15 (Journal-Ladelauf CH/AT) mit Pruefhinweis versehen.
- Mail und Teams nicht durchgesehen (M365-Sitzung abgelaufen).

## Doku: HR-Cockpit SAP-Abruf als produktiv nachgefuehrt, 2026-10-20

- `docs/rag/HR_KPI.md` und lastchange sagten noch „`HrKpiSet` nicht in P76“ bzw. „SAP-Datei nicht automatisiert“. Live geprueft:
  beide SAP-Dateien in `hrdata` taeglich um 05:07 geschrieben (heute), Rexx-Dateien vom 08.07. Aussagen als ueberholt markiert.

## Projekte: Abschlussrunde Dashboard und Data Lake eingeladen, 2026-10-20

- Ingo hat die Runde mit Finance, Einkauf, Supply Chain, HR und Sales (90 Min.) am 2026-10-20 in Outlook verschickt.
  Agenda: Ziel (5), Stand Dashboard (15), Runde durch die Bereiche (40), Data Lake Zielbild und Datenverantwortung (15),
  Abschluss und Abnahme je Bereich (10), naechste Schritte (5). Entwurf `Downloads/Einladung_Abschlussrunde_Dashboard_DataLake_2026-10-20.eml`.
  Nachgefuehrt in Status_kurz, Wochen_Todo 116 und PROJEKTSTATUS (PM-10, PM-12).

## SAP PS: Einfuehrung und Demo in T76, 2026-10-20

- Auftrag Chef: wie funktioniert PS (Zeit/Leistung auf Projekte, Teilprojekte, externe Kosten), einfach oder schwierig.
  In T76/100 Projekt `DEMO-PS` mit zwei Teilprojekten angelegt und freigegeben; Befund: PS fuer Trafag nicht eingerichtet
  (Standardprofil auf 0001, Komponente Projekte im KoKrs 1000 aus, CATS-Profil ohne Projektspalte). Auf Wunsch Ingo
  „Projekte aktiv“ in OKKP nur in T76, lokaler Auftrag `T76K912750` ohne Ziel. KB21N 6 h auf `DEMO-PS.1` (396 CHF, Beleg
  311258907), in CJI3 sichtbar. Doku `docs/SAP_PS_EINFUEHRUNG_DEMO_2026-10-20.md`, Bilder `docs/img/ps_demo_2026-10-20/`.
- Nachtrag: externe Kosten durchgespielt (Bestellung ME21N mit Kontierung P; in T76 brach die Verbuchung an der Nachricht ab,
  deshalb Rechnung FB60 1'500 CHF auf `DEMO-PS.2`, Beleg 19102004); CJI3 zeigt zusammen 1'896 CHF. P76 lesend geprueft:
  PS nicht eingerichtet, keine Projekte, keine CATS-Zeiten, Projekte laufen als Innenauftraege (44 seit 2025).
- Folien 11 bis 16 in der Kopie `20260223 - Abgleich TPG - Trafag (mit SAP PS).pptx` (18 Folien, Original unveraendert).

## SAP: ZLO03 FIX 19 Spalten REACH, SVHC, PFAS-Gehalt, 2026-10-20

- Wunsch Florian Waechter: Stoff-Compliance in ZLO03. Befund: in P76 nur Klasse `Z_PACK_REGULATION` (8 Merkmale, kein RoHS),
  `ZCOMP_STOFF` nur in T76 und leer. FIX 19 liest `REACHCOM`, `SVHCCOMP`, `PFAS` aus `AUSP` und gibt drei Spalten hinter
  `Postyp` aus. T76 aktiv, im Auftrag `T76K912742` zusammen mit FIX 12 (Entscheid Ingo). Lauf `8475*` fehlerfrei, Spalten leer.
  Doku `zlo03/FIX19_STOFFCOMPLIANCE_SPALTEN_2026-10-20.md`. RoHS: Adil soll ein Merkmal anlegen (*erledigt, Spalte seit 22.10.*).

## SAP: Testuser UBIG mit GIB-DCP-Rolle in T76/200, 2026-10-19

- Auftrag Ingo: User `UBIG` in T76 Mandant 200 angelegt, nur Rolle `/GIB/DCP_SUPERVISOR` (Profil generiert, abgeglichen).
  Kennwort von SAP zufaellig erzeugt; Ingo setzt das Initialkennwort selbst. Bestand der GIB-Rollen und Transaktionen in
  `saptasks/gib_dcp/NOTES.md`. Laut Ingo sind die GIB-Module installiert und die Pilotphase ist abgeschlossen.

## Doku: Word GIB Dispo-Cockpit DCP, 2026-10-19

- `docs/GIB_DispoCockpit_DCP_Zusammenfassung_2026-10-19.docx` (3 Seiten): Module DCC/DCO/DCP/DCF, was DCP macht, Technik, Rollout-Schritte,
  Ueberschneidung mit Operations, ZM_OFFENE_FAUF, Logistik live und Einkauf, offene Fragen. Aus oeffentlichen Quellen, Trafag-Umfang offen.

## Doku: Word ZC23 Materialanteil, Beschleunigung, 2026-10-19

- `saptasks/zc23_materialanteil/ZC23_Materialanteil_Beschleunigung_2026-10-19.docx` (4 Seiten) mit Generator `word_beschleunigung.js`. Nur Doku.

## SAP ZLO03: FIX 12 in T76 am nachgestellten Fall bestaetigt, 2026-10-19

- Testdaten `ZPOWERBI_VC_TXT` in T76 gefuellt, Textposition B63485/46123 vor die echte Position gelegt. Alte Fassung: B63485 fehlt;
  mit FIX 12: MK 5'009 wie `ZMD04_CALC`, uebrige 122 Zeilen gleich. `T76K912742` bereit fuer P76. `zlo03/MK_B63485_FIX12_2026-10-16.md` Abschnitt 6.

## Operations: Mengen aus SAP auf dem Produktionstag, DW prod, 2026-10-19 (`ded2b65`, `c4dca10`, `c3c45cd`)

- Logistik und Produktionsmengen stehen auf dem Produktionstag (ERSDA/Istendtermin), Montagsregel weg; Abgleich schreibt heute und
  drei Tage zurueck; Bereich ohne Rueckmeldung bleibt leer. Neu DW prod aus DL790. 15. bis 19.10. umgebucht. Produktiv 10:04.

## SAP ZLO03: FIX 12 in `Z_ZLO03_TURBO2` (MK-Abweichung B63485), 2026-10-16, T76 `T76K912742`

- Mechanismus in P76 belegt (nur lesend), Nachweis am Fall erst nach Import: B63485 hat vier Textpositionen (46123/24/26/28); die Kennzahlen wurden nur beim ersten Treffer
  gefuellt, bei einer Textposition blieben MK, Bestand und Kosten 0. Fix in beiden Zweigen, in T76 aktiv; Import P76 durch Ingo.
- Ueberholt: die Aussage in `zlo03/CLAUDE.md`, die Textpositions-These sei widerlegt. `zlo03/MK_B63485_FIX12_2026-10-16.md`.

## Doku: Arbeitsplatz Ingo, iPhone-Hotspot trennt alle zwei Minuten, 2026-10-16

- Nachtrag im Abschnitt „wiederkehrende WLAN-Aussetzer“ in `docs/router/plattform.md`: Muster aus dem Ereignisprotokoll, Ausschluesse,
  Loesung durch neuen Hotspot-Namen (frisches Profil), naechste Schritte falls es zurueckkehrt. Nur Doku.

## Einkauf: Stücklisten-Netz „Gehirn“ im Spend-Aufriss, 2026-10-16 (`d188806`, `d6ca646`)

- Prototyp zuoberst im Spend-Aufriss: Stuecklisten als leuchtendes Netz (WebGL, three.js), Endprodukte aussen, Komponenten innen,
  Kreuzpunkte gross; Farbe umschaltbar LZ-Code/Lieferantenrisiko/Stufe; Klick hebt Bahnen hervor. 11'005 Materialien, 24'979 Straenge.
- Handbuch Einkauf um den Abschnitt ergaenzt.

## Menue: alle Hauptbereiche farbig, Admin grau, Home pulsiert, 2026-10-16 (`1931084`)

- *Ueberholt (10:17, „dezenter, kein Rosa“):* Finance Backstein, HR Rostrot, Einkauf Dunkelrot, Verkauf Terrakotta, Logistik Ziegelbraun, leiser animiert.
- Erste Fassung 10:13: Finance (Bordeaux), HR (Himbeere), Einkauf (Weinrot), Verkauf (Korallrot), Logistik (Ziegelrot) wie Operations hervorgehoben
  und animiert; Admin Bereich grau hinterlegt; Home im Trafag-Orange mit Pulsieren. `NavMenuNode.razor`, `app.css`.

## Operations: Mengen aus SAP live, Handbuch mit Prozessgrafik und Schnellueberblick, 2026-10-16

- Logistik (ZC27), Tagesmengen SEH/TR5 und FPY SEH kommen seit 09:51 automatisch aus P76 (`9d6b5d2`); vorher gegen Fabios
  Kontrollwerte geprueft, alle gleich. Erstlauf fuer den 16.10. geschrieben.
- Handbuch Kapitel Operations: neuer Abschnitt „Schnellueberblick“ mit Prozessgrafik im Trafag-CI (Quellen, Cockpit, Meeting,
  Rueckwirkung auf SAP), Seitentabelle und Tagesrhythmus; Handbuch kann jetzt Bilder (Reiter SVG, Word PNG).

## Benutzerhandbuch: Kapitel Operations erweitert, 2026-10-16

- Neuer Abschnitt zum Geschaeftsprozess (taegliches Shop-Floor-Meeting, Stand D-1), jede Shopfloor-Seite mit Wozu, Inhalt,
  Rechnung, Einsatz nach Rolle, Interpretation und Grenzen (Ampel, Lieferzeiten, Rueckstaende, PPD, Forecast mit DS, Logistik nach
  ZC27, Verkauf mit Book to Bill, CZ/MTF/Sensoren, Schnittstelle); Glossar von 9 auf 21 Begriffe. Word-Datei neu.

## Operations (Shopfloor): Mengen aus SAP vorbereitet, 2026-10-16 (`2bc54bd`, T76)

- Neue SAP-Sets `ShopRmSet` (ZC27-Logik) und `ShopProdSet` in T76, gegen ZC27 gleich. Das Cockpit rechnet daraus Logistik
  (MLE01/02/04, MLM01, MLE08+09), Tagesmengen SEH/TR5 und FPY SEH; Vorschau `sap/mengen` produktiv, Schreiben aus bis zum
  Transport nach P76 und dem Abgleich mit Fabios Kontrollwerten. Details `saptasks/shopfloor_sap/NOTES.md`.

## Operations ohne Login, 2026-10-16 07:48 (`70a02b1`)

- Auftrag Ingo: `Shopfloor:OpenForAll = true`, alle Windows-Benutzer sehen und bearbeiten den Shopfloor ohne Anmeldung. Der Admin-Schalter
  bleibt und haette Vorrang, wenn ihn jemand setzt.

## Operations (Shopfloor): Paket Fabio 2026-10-16, Ampel, Lieferzeiten, Forecast DS (`b2103a4`, produktiv 2026-10-16 07:06)

- Fabios Patch 03 (Wuensche D. Tobler) und Config-Patch 04 eingespielt: Verlaufskacheln, Ampel pro Bereich, Menue
  „Lieferzeiten“ mit Grund, PPD ohne Verzug MTS, Forecast mit eigener Zeile DS. Zwei Korrekturen am Paket (FPY SEH ueber
  `src`, DW1/DL6 ohne Abteilung). ZD05-Abgleich seit 07:28 um 04:00 und 12:45 (`a285009`). Ampel per zweitem Klick loeschbar, `planned_source` aus Fabios Paketen wird gelesen (`e6c007f`). Die SAP-Jobs J2 bis J11 sind nicht gebaut. Details `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 14.

## SAP Fiori: App ZC23 live mit Selektion, 2026-10-15 (T76 fertig, Transport `T76K912738` offen)

- `http://travt762.sap.trafag.com:8000/sap/bc/ui5_ui5/sap/zfi_zc23/index.html?sap-client=100`: Selektion wie ZC23,
  Kacheln (Positionen, offener Wert, Standardwert, Materialanteil, Arbeit CZ, Kunden), Liste mit Suche und CSV, Trafag-Logo.
- Die Logik bleibt in ZC23: neues EntitySet `Zc23Set` in `ZPOWERBI_EINKAUF` startet `ZM_OFFENE_KDAUF` mit `P_APP`
  (Ergebnis nach dem Materialanteil ins Memory, keine Liste, keine Power-BI-Fortschreibung). Q2-Abgleich gleich
  `ZPOWERBI_KDAUF` (4'392 Zeilen, alle drei Summen). Details `saptasks/fiori_zc23/NOTES.md`.

## SAP Fiori: erste Test-App `ZFI_ERSTE_APP`, 2026-10-15 (in T76 hochgeladen, laeuft im Browser)

- Kleine SAPUI5-App als Harness für künftige Erfassungs-Apps: liest 50 Zeilen `ProductDivisionRefSet` aus
  `ZPOWERBI_EINKAUF_SRV`, nur lesen, T76 `$TMP`. Quelle und Vorgehen `saptasks/fiori_erste_app/` (NOTES.md).
- Läuft im Browser: `http://travt762.sap.trafag.com:8000/sap/bc/ui5_ui5/sap/zfi_erste_app/index.html?sap-client=100`,
  Hochgeladen per RFC `/UI5/REPO_LOAD_FROM_ZIP` mit SapProbe (`--set "NAME=@datei:<Pfad>"`, neu), ICF-Knoten aktiv.
  Geprueft in Chrome: 50 Zeilen. Fallen: Seite ohne `sap.m.App` 0 px hoch, `$top` wird vom Dienst ignoriert,
  UI5-Dateien ein Jahr im Browser-Cache.

## SAP ZC23: neue Spalten Materialanteil, 2026-10-15 (T76 fertig, Transport `T76K912736`, Import P76 offen)

- ZC23 zeigt neben dem Standardwert den Materialanteil (Zukaufmaterial aus der Standardkalkulation, Regel wie ZM03),
  je Einheit und gesamt; Block „Materialanteil“ im Selektionsbild: ein/aus, freigegebene oder jüngste Kalkulation,
  Fremdleistung zum Material. Standardwert und Materialanteil auch in `ZPOWERBI_KDAUF` und im OData-Dienst (Power BI).
  Dazu Spalte „Arbeit CZ“ (Eigenleistung an CZ-Arbeitsplätzen wie ZM03), Spalten direkt hinter dem Standardwert,
  Hauswährung des Werks in der Überschrift. Details `saptasks/zc23_materialanteil/NOTES.md`.
- Anwenderdoku als Word: `docs/ZC23_Materialanteil_Formeln_2026-10-15.docx` (Felder, Formeln, Beispiel B87813,
  Bedienung, Grenzen, Prüfung), Generator `Tools/BuildZc23Formeln`. Formal geprüft (OpenXML, 21 Pflichttexte),
  visuell noch nicht gesichtet.

## Einkauf Spend-Aufriss: Platzhalter „NA - N/A“ nimmt keinen Anteil mehr, 2026-10-15 (`7b62544`, produktiv 12:48)

- Materialien mit einer echten Produktgruppe und dem SAP-Platzhalter `NA` („N/A“) wurden halb/halb verteilt
  (gemeldet an B53173: T2 und NA). Jetzt zählt `NA` neben einer echten Gruppe nicht mit; gilt für alle Materialien.
  Nur unter `NA` verwendete Materialien bleiben dort sichtbar. Mehrere echte Gruppen werden weiter 1/n verteilt.

## Operations: Login per Admin-Schalter abschaltbar, 2026-10-15 (`8bfe424`, produktiv 12:15; Freigabe Produktionsleitung laut Ingo erteilt, Schalter legt Ingo um)

- Admin Bereich > Passwörter, Schalter „Operations: Login nötig“ (Wunsch Fabio). Aus: Alle mit Windows-Anmeldung sehen und
  bearbeiten den Shopfloor ohne eigenes Login. Gespeichert in der Datenbank, übersteht Deploys. Standard unverändert: Login nötig.

## Shopfloor: Schnittstelle nimmt Fabios Push-Format an, 2026-10-15 (`f4e3f1b`, produktiv 11:06)

- Ein einzelnes Item ohne `items`-Hülle und `values` statt `fields` werden angenommen; zusätzliche Adresse
  `/BiDashboard/api/integration/push`. Fabio muss am Format seines Pakets nichts ändern, er braucht nur Server-Adresse und Token.

## Admin: Passwörter aller Bereiche an einer Stelle, 2026-10-15 (`e1afd49`, produktiv 10:56)

- Neue Seite Admin Bereich > Passwörter (`/admin/passwoerter`): Der entsperrte Admin setzt die Passwörter von Admin, Finance,
  HR KPI (auch `hradmin`), Operations und Logistik (Namen anzeigen) neu, ohne das alte zu kennen.
- Gespeichert wird nur der SHA-256-Hash, in der Tabelle `AccessPasswords` der Cockpit-DB. Ein Deploy überschreibt ihn nicht mehr;
  fehlt ein Eintrag, gilt weiter `appsettings.json`. Die bisherigen Felder „Passwort ändern“ bei Admin, Finance und HR schreiben
  ebenfalls in die DB.
- Nach einer Änderung müssen sich alle im Bereich neu anmelden, jetzt auch bei Operations. Einmalig nach dem Deploy gilt das für
  alle Operations-Anmeldungen, weil das Cookie ein neues Format hat.

## Shopfloor: Patches von Fabio, 2026-10-15 (`52b3b75`, produktiv 08:50)

- ZD05: Filter „Zu besprechen“ (neu, ohne Code oder Liefertermin überschritten), im Meeting automatisch aktiv.
- Verkauf: Kacheln mit Go-for-it-Ziel des Monats, Umsatz IST in % vom Ziel, Rest und Book to Bill.

## Anonyme Nutzungsmessung, 2026-10-14 (`4615268`, produktiv)

- Admin Bereich > Nutzung: Aufrufe und Nutzertage je Seite, Personen pro Tag, ohne Namen; Ingos eigene Aufrufe zählen nicht.
  Befund und Massnahmen in `docs/NUTZUNG_2026-10-14.md`.

## Startseite: Manometer abschaltbar, Titel „Trafag Dashboard“, 2026-10-14 (`cc9c6ab`, produktiv)

- Manometer auf der Startseite standardmässig aus; Schalter „Manometer anzeigen“ unter Admin Bereich > Aktive Logins,
  in der Datenbank gespeichert (übersteht Deploys).
- Kopfleiste „Trafag Dashboard“ statt „Trafag Cockpit“, Mitte der Standortgrafik „BI Dashboard“.
- Überschrift der Startseite „Trafag Global BI Dashboard“, Browser-Tab „Trafag Dashboard“ (`b5b4727`).

## Startseite: animierte Standortgrafik, 2026-10-14 (`ce5d27d`, produktiv)

- Schwarze, animierte Grafik statt des festen Bildes: neun Länder mit Flagge rund um „Trafag Global BI Cockpit“, Ring in der
  Farbe des letzten Exports (grün aktuell, gelb veraltet, rot Fehler), Datenpakete zur Mitte, Infobox beim Darüberfahren,
  Klick öffnet den Datenstatus.

## Operations KPI: Monatsabruf abgesichert, 2026-10-14 (`55b42d9`, produktiv)

- Abrufversuche bleiben über einen Neustart gespeichert, Warnhinweis wenn der Vormonat fehlt, Einträge in den Admin-Logs,
  neuer Versuch alle zwei Stunden (Wunsch Fabio).

## Kopfleiste nach Designprüfung, 2026-10-14 (`5b51011`)

- Dimmer zeigt jetzt eine helle Schiene, CPU-Anzeige flacher mit besser lesbarer Skala, Logo mit Abstand zum rechten Rand.

## Benutzerhandbuch: Kapitel Operations, 2026-10-14 (`f38020f`, produktiv 11:13)

- Neues Kapitel 10 Operations: KPI Übersicht, OTD / OTIF / OTP, Shopfloor (Ablauf, Listen, Abteilungen, ZD05, Forecast,
  PPA-Grafik) mit Glossar. Einleitung: Rolle Produktion/Operations im Wegweiser, Abschnitt Kopfleiste mit der CPU-Anzeige.
  Word-Datei neu erzeugt.

## Startseite: neuer Untertitel, 2026-10-14 (`d911cb1`, produktiv 11:04)

- „Die Trafag-Gruppe auf einen Blick: alle Standorte, alle Bereiche, täglich aktuell.“ statt „Automatisch angebundene
  Länderabfragen mit täglichem Refresh“; Bildbeschreibung entsprechend. In allen Sprachen inklusive Berndeutsch.

## Kopfleiste: CPU-Last als Fluoreszenz-Pegelanzeige, 2026-10-14 (`a735b41`, `5235e16`, produktiv 10:51 und 11:00)

- Ganz links in der rechten Kopfleiste, für alle: zwei Balken wie das VU-Meter eines Kassettendecks, S = Server gesamt,
  C = Cockpit-Prozess, Skala 0 bis 100 %, Beschriftung „CPU LOAD“, Spitze hält 2 Sekunden.
- Messung im Server alle 1,5 Sekunden (ein Takt für alle Besucher), unter 900 px Breite ausgeblendet.

## Operations: KPI Liefertreue OTD / OTIF / OTP und neues Menü, 2026-10-14 (`89e9c7c`, produktiv 10:04, live aus SAP seit 10:33, Abnahme bestanden)

- Menü Operations mit den Unterreitern KPI (Übersicht, OTD / OTIF / OTP) und Shopfloor.
- Liefertreue der Trafag AG nach Fabios Spezifikation: Kennzahlen, Diagramm, Monatstabelle, Machbarkeit des Wunschtermins,
  Abweichungsliste mit Excel-Export. Abnahmewerte Jan–Sep 2026 werden genau getroffen.
- Monatlicher Abruf aus SAP am 2. Kalendertag, Abruf auf Knopfdruck, Ausschlusstabelle auf der KPI-Seite pflegbar.

## Operations (Shopfloor): Feld-Mapping aus dem Excel, 2026-10-14 (`b613f7c`, produktiv 10:04)

- Abteilungen über den Disponenten: SEH = S*, TR5 = EK*, TX = EL*, DW = D*, CZ = 017 bis 028; 012 nicht mehr SEH.
- Aus dem MES kommen nur noch die FPY-Werte, die übrigen 129 Felder aus SAP.
- Seite „Schnittstelle SAP / MES“ zeigt je Feld SAP-Tabelle und -Feld (Vorschlag kursiv), Selektion, Status und Bemerkung.

## Menü-Hervorhebung, Deploy ohne Sicherung, Deploy-Monitor, 2026-10-13

- Menü: Trafag Projekte und Operations rot, Netzwerk rot mit zuckendem Kabel und Funke, Trafag Reddit orange,
  Benutzerhandbuch hellgrau; alle mit weisser bzw. dunkler fetter Schrift und Animation (`NavMenuNode.razor`,
  CSS `nav-hl*`). Produktiv bis `c8b91a7`.
- Deploy sichert die DB nur noch mit `--backup` (Entscheid Ingo); 96 alte Sicherungen (36,8 GB) gelöscht.
- Neues Monitor-Fenster `.tmp_tools/DeployMonitor/` mit Fortschrittsbalken; Runner beendet sich jetzt selbst.
  Ablauf in `docs/rag/DEPLOYMENT.md` und `router.md` Regel 11.

## Trafag Projekte: HERMES-Angaben, Portfolio und Demoprojekt, 2026-10-13 (`7e20f93`, produktiv 09:34; Fix `47990ad` produktiv 09:47)

- Je Projekt ein HERMES-Block in den Einstellungen: Aufwand mit Skala und Stufenvorschlag (unter 5 / 5 bis 40 /
  über 40 Personentage), Stufe, Ampel, Auftraggeber, Anwendervertreter, Stand in einem Satz, nächster Entscheid,
  „Erledigt, wenn“ und die Entscheidungspunkte ① Freigabe, ② Abnahme, ③ Abschluss; die Phase folgt daraus.
- Grafik: HERMES-Pfad mit animierter aktueller Phase, Projektkarten mit Ampel, Stufe, Phase und Rauten,
  neuer Reiter „Portfolio (HERMES)“ mit Ampelring, Stufenbalken, Phasenlandkarte und Tabelle für die
  Geschäftsleitung, alles per Klick filterbar.
- Demoprojekt `HDEMO` wird beim Start einmalig angelegt. Neue Tabelle `PmHermes`, 49 Texte in allen Sprachen,
  8 neue Tests (1173/1173). Doku `docs/TRAFAG_PROJEKTE_2026-10-09.md`, Handbuch Kapitel 8.

## SAP ZZPRDAT: Abnahmetest verschoben, 2026-10-12

- Abnahmetest mit Lucas, Marco, Adil und Ingo: war auf 12.10.2026 16:00 gelegt, am 12.10. um zwei Wochen verschoben (voraussichtlich Woche ab 26.10.2026, genauer Termin offen). Stand in T76 am 12.10. live geprüft: seit 07.09. unverändert, T76K912490/T76K912491 offen, BAdI Z_ZZPRDAT_UPDATE aktiv, Z_ZZPRDAT_SET als V2.
- Nachgeführt: `saptasks/ZZPRDAT_TRANSPORTPLAN.md`, `saptasks/zzprdat-kontext.md`, `docs/rag/PROJECT.md`,
  `docs/router/sap.md`, `projektmanagement/Wochen_Todo.tsv` Zeile 56 (fällig 2026-10-26).

## SAP T76: ZM_OFFENE_KDAUF (offene Kundenaufträge) schnell und geprüft, 2026-10-12 (in T76 eingesetzt, Transport T76K912728 bereit)

- Schnelle Kopie `ZM_OFFENE_KDAUF_OPT` liefert dieselbe Liste wie das Original, Feld für Feld gegen eine Referenzkopie
  verglichen: April bis Juni 2026 5 s statt 187 s, Variante `BI UPDATE` 24 s statt rund 20 Minuten.
- Wege: gesammeltes Vorablesen statt rund 20 Einzelzugriffen je Zeile, Parallelrechnung über den neuen RFC-Baustein
  `Z_KDAUF_PAR_RECHNEN` (Funktionsgruppe `ZKDAUF_PAR`), Kurzfassung der Basispreisliste, eigene Belegfluss-Lesung mit
  Rückfall auf den SAP-Baustein bei gleich datierten Rechnungen, IDs für `ZPOWERBI_KDAUF` in einem Sammelaufruf.
- Befunde: Das Original vergibt Fertigungsaufträge bei gleichem Lieferdatum zufällig (jetzt ETENR-Sortierung) und
  bricht als Hintergrundjob in der ALV-Liste ab (Feldkatalog mit nicht vorhandenem Feld `NETWR`). Korrekturschalter
  im Selektionsbild (Vorgabe „korrigiert“, „alte Logik“ für Vergleiche). Alles `$TMP`, Original unverändert.
  Doku `saptasks/zm_offene_kdauf/NOTES.md`.

## Trafag Projekte ersetzt die Poor Man's Project Management Suite, 2026-10-09 (produktiv 10:18)

- Projektmanagement wie Jira, Trello und Planner: Board mit Ziehen und Swimlanes, Backlog und Sprints mit Burndown,
  Liste mit CSV, Zeitachse nach Epic, Kalender, Diagramme, „Meine Aufgaben“ über alle Projekte, Aufgabenpanel mit
  Checkliste, Unteraufgaben, Kommentaren und Verlauf. Alle sehen, Mitglieder bearbeiten, Leitung verwaltet.
  Die alte Suite (nie genutzt) ist entfernt. Doku `docs/TRAFAG_PROJEKTE_2026-10-09.md`, Handbuch Kapitel 8 neu.

## Neuer Reiter Trafag Reddit, 2026-10-09 (produktiv 09:20)

- Internes Forum für alles, was in SharePoint keinen Platz hat: Communities, Abstimmen hoch/runter, Sortierung Angesagt/Neu/Top/Aktiv/
  Offene Fragen/Kontrovers, verschachtelte Kommentare, Fragen mit akzeptierter Antwort, Tags, Reputation mit Stufen, ähnliche Beiträge
  beim Tippen, Speichern. Autor ist der Windows-Name, für alle sichtbar (Entscheid Ingo). Doku `docs/TRAFAG_REDDIT_2026-10-09.md`,
  Handbuchkapitel 9.

## Einkauf: Produktgruppe im Spend-Aufriss über ZLO03, 2026-10-08 (produktiv 15:03)

- Gespräch Armin Punkt 4: Fast der ganze Einkauf stand unter „ohne Produktgruppe“, weil die Zuordnung nur den Cache der
  Stücklistenanalyse las (86 Zeilen, nur von Hand eingegebene Nummern).
- Neu fragt der Einkauf-Lauf je bestellter Komponente ZLO03 Bottom-Up ab (verkürzte Nummer und deren Disponent), höchstens
  8 Minuten je Lauf, jede Komponente höchstens einmal pro Woche. Keine SAP-Änderung.
- Erwartete Abdeckung nach dem Aufbau rund drei Viertel des Spends; der Rest hat in ZLO03 keine verkürzte Nummer.

## Einkauf: offener Mengenkontraktwert und LZ-/Sortiments-Code, 2026-10-08 (produktiv 15:03, SAP-Sets erst nach P76-Import)

- Gespräch Armin: offener Kontraktwert wie ME3L (Zielmenge minus EKAB-Abrufe × Nettopreis, inkl. abgelaufen) auf Kontrakte-Seite und
  Dashboard-Kachel; bisherige 5.2 Mio heissen jetzt „Abrufbestellungen zu Kontrakten“.
- Spend-Aufriss: Ebenen Lebenszyklus-Code und Sortiments-Code (bis Material) mit Grafik.
- SAP T76: EinkKontraktSet, EinkMatLzSet (Strukturen T76K912724, Klassen T76K912718). Bis zum P76-Import zeigt das Cockpit „noch nicht verfügbar“.

## Operations (Shopfloor): Cockpit-Design, Grafiken und Login, 2026-10-08, produktiv 09:23

- Reiter für alle sichtbar, Zugang mit eigenem Login (Benutzer `operations`, analog Finance/HR); Ingo ohne Login.

- Shopfloor sieht aus wie das Cockpit (CI-Orange, Open Sans, Karten, Kacheln) und folgt Hell/Dunkel und Dimmer des Cockpits.
- Grafiken: Fortschrittsring Pflichtpunkte, Rückstände je Abteilung, ZD05-Kennzahl gegen Ziel 1.20, Trendkacheln je Abteilung,
  Statusbalken in Listen, Auslastung je Abteilung im Forecast, Abteilungskacheln in der PPA-Grafik.

## Operations (Shopfloor), 2026-10-07, produktiv 2026-10-08 06:39

- Neuer Reiter `/operations`: PPA-Shopfloor im Cockpit (Oberfläche übernommen, Python-Backend in C#), Daten aus dem Excel-Stand.
- Zugriff vorerst nur Ingo, bis die Leitung Produktion/Operations freigibt.
- SAP: ShopZd05Set (ZD05) und ShopAufSet (Forecast) in T76 fertig (T76K912718); Abgleich im Cockpit gebaut, ausgeschaltet bis P76.

## SAP: ZM_OFFENE_FAUF Bedarfsverursacher sichtbar und mit Werk, 2026-10-08 (T76)

- Wunsch Ingo/Fabio: Die Spalten sind jetzt standardmässig eingeblendet; neues Feld „Werk“ (Werk des Bedarfsverursachers, z. B. 1200 bei einer BANF in 1200).
- Das globale Standardlayout `/ P00 TX` versteckte neue Spalten; in T76 um DE, NR-Disp, Daten zum Dispoelement und Werk ergänzt.
- Alles im selben Auftrag T76K912714. In P76 nach dem Import das Layout noch einmal anpassen (Layouts reisen nicht mit).

## ZLO03: Bedeutung von „Exklusiv“ geklärt, 2026-10-08

- Frage Ann-Katrin (B63491): „Exklusiv“ gilt relativ zur Selektion, ohne Status 99 und Nicht-FERT; Bottom-Up zählt alles. Notiz `zlo03/EXKLUSIV_LOGIK_2026-10-08.md`, Antwort als .eml.

## SAP: ZM_OFFENE_FAUF mit Bedarfsverursacher, 2026-10-07 (T76)

- Neun einblendbare Felder (Dispoelement, Nummer, Position, Einteilung, Daten zum Dispoelement, Dispobereich, Termin,
  Menge, Anzahl) über MD_PEGGING_NODIALOG; Transport T76K912714, in T76 getestet, P76 offen.

## Doku: eine Stufe und Lieferantenerkennung, 2026-10-07

- `FINANCE_FORMELN.md`: Vorrang, Regex intern/extern, Zuordnung TR AG/IT/IN, gemessene Lücken (TR IT mit Indien-Ware, TR DE ohne Lieferant).
- Neu `ISS-007.4`, drei Todos (Brücke Sachnummer Italien, Andreas informieren, Kostenwährung Budget oder Tageskurs).

## Budgetkurse und farbige Standardkosten-Spalten, 2026-10-07, produktiv 10:06

- Entscheid Ingo: Finance rechnet immer mit Budgetkursen. CHF in Cockpit, Pruefbuch, Nachweis und Sales_All jetzt mit dem
  Budgetkurs des Finance-Jahres (2025 = Budget 2025, EUR 2026 = 0.94 statt EZB 0.923); der CHF-Schalter folgt demselben Profil.
- Sales_All: alle Spalten mit Standardkosten-Bezug farbig (Kopf kraeftig orange, Spalte hell).

## Finance Details mit Standardkosten in CHF, 2026-10-06, produktiv 15:45

- Sales_All, Blatt Finance Details: Standardkosten je Stück in CHF, Kostenbasis und Marge in CHF aus der Gruppenmarge,
  Kostenquelle, Status und Kostenstand (historisch/aktuell). Wunsch Andreas aus dem Gespräch vom 06.10.

## Weltlage unter Finance Cockpit, 2026-10-06, produktiv 14:36

- Weltlage ist keine Hauptgruppe mehr, sondern liegt unter Finance Cockpit und verlangt das Finance-Passwort.
  Auftraggeber ist Ingo, Rückfragen landeten bisher bei Andreas.

## Benutzerhandbuch, 2026-10-06, produktiv 10:16

- Neuer Reiter **Benutzerhandbuch** auf oberster Ebene (`/handbuch`, nach Home): alle Module aus betriebswirtschaftlicher
  Sicht, je Seite Wozu, Lesart, Rechnung, Einsatz je Rolle und Fallstricke, Kapitelnavigation und Suche.
- Word-Datei im Trafag-CI (105 Seiten) zum Herunterladen; Quelle und Neuerzeugung in `docs/handbuch/README.md`.

## Pruefbefund-Fixes, 2026-10-05, produktiv 2026-10-06 10:16

- **Endlieferkennzeichen:** EKPO.Elikz steht im Cache als `True`/`False`, nicht als `X`. Der Ausschluss endgelieferter
  Positionen (M7) im Einkaufsdashboard und in der Supply Chain griff deshalb nie, und die Weltlage zaehlte nichts als offen
  (Logistik 0.0). Auf der Deploy-Sicherung sinkt der offene Bestellwert von 28.7 auf 12.8 Mio CHF (10'757 auf 1'548 Einteilungen).
- **Nur Bestellungen** jetzt auch in Einkauf Interaktiv und Weltlage; Weltlage offen nach offener Menge, ohne Konzernlieferanten,
  gleiches 12-Monats-Fenster fuer Verkauf und Einkauf.
- **Logistik live:** Timeout als Meldung statt Absturz, Fehler nur 1 Minute gemerkt, nur heute quittierte Positionen zaehlen,
  Ausblick mit heute und ueberfaellig, Lgnum 110, Ausbringung je letztem Vorgang. ABAP-Teil (Warenausgangszeit, offene TAs,
  STZHL, Kapazitaet verteilt und nach KEINH) liegt in `docs/abap/`, wirkt erst nach neuem T76-Transport.
- **Netzwerk/AD:** Windows 11 nach Build, Abbruch nur bei Wiederverbindung, eine Inaktiv-Definition, GPO-Vererbung, BitLocker,
  /24-Abdeckung, DNS-Alterung und weitere Einzelpunkte.
- **Verkauf/Controlling/Interaktiv/Weltlage:** Referenzmonat nach letztem Werktag, Galaxie ohne +100 % im ersten Quartal,
  Landschaft ohne NaN, Bruecke rundungsgenau, Prognoseband ab 0, UK/EL normiert, Ereignisse dedupliziert, Eurostat-Arrayform.

## Einkauf: Interaktiv, 2026-10-05, produktiv 15:11

- Menü Einkauf → Interaktiv mit denselben neun Ansichten auf den Bestellungen; Korrekturen Verkauf Interaktiv (Galaxie-Achse,
  Versandpauschalen).

## Verkauf: Unterreiter Interaktiv, 2026-10-05, produktiv 14:25

- Neun animierte Ansichten (Kunden-Galaxie, Rennen, Umsatzfluss, Sonnenstrahl, Was wäre wenn, Bestellrhythmus, Kalender,
  Artikel-Netzwerk, 3D-Landschaft) unter `/verkauf/interaktiv`, bestehende Unterreiter unverändert.

## Logistik live: Produktivität, Kapazität, Namen nach Anmeldung, 2026-10-05, produktiv 10:31 bis 12:40

- Umschalter Produktivität (Durchlaufzeit je Lieferung, Leistung je Stunde, anonym, produktiv 10:31) und Kapazität (tägliche
  Erfassung je Bereich nach Nevas Kennzahlen: Produktivität, Bedarf, Belastung, Lücke). SAP-Teil für Namen und Warenausgang in T76
  (`T76K912658`), Aktivierung offen. `docs/LOGISTIK_LIVE_2026-10-01.md`.

## Logistik live: 3D-Lagerplatzansicht, 2026-10-05, produktiv 09:21

- Umschalter Original | 3D-Lager: heute bewegte Plätze als Regale mit Paletten, Klick zeigt einen Platz mit seinen
  Bewegungen. `docs/LOGISTIK_LIVE_2026-10-01.md`.

## Reiter Weltlage, 2026-10-05, produktiv 09:21 (Währungskorrektur danach)

- Neuer Reiter (Radar je Abteilung mit drehendem Strahl, Länder, Rohstoffe und Währungen, Ereignisse) aus frei
  abrufbaren Quellen, verknüpft mit Verkauf, Einkauf und Kursen; Firewall-Zustand je Quelle. `docs/WELTLAGE_2026-10-05.md`.

## Finance Cockpit: Unterreiter Controlling, 2026-10-05, produktiv 08:18

- `cb08f18`: Umsatzbruecke gegen Vorjahr (Menge, Mix, Preis, neue/weggefallene Artikel, Waehrung, Uebrige) und
  Hochrechnung des laufenden Jahres; offene Fragen separat (Eigeninitiative, kein Auftraggeber).

## Reiter Verkauf: ohne Passwort, Vergleich gleicher Zeitraeume, Korrekturen, 2026-10-02, produktiv 14:05 bis 14:46

- `84fd1fd` ohne Finance-Passwort (Ingo), `fa83ad1` Vergleich 01–09.2026 gegen 01–09.2025 statt 12 gegen 12 Monate (Daten ab 01.2025),
  `a2bd605` Veraenderung je Gesellschaft, Verluste erst nach 6 Monaten, Preisstreuung ohne Platzhalter/Leistungen,
  alter Stand waehrend Neuladen, Achsanker; `81a4132` Prognoseachse.

## Reiter Verkauf, 2026-10-02, produktiv 13:24

- `e2fb8ce`: acht Unterreiter (Kunden, Rueckgang, neue und verlorene Kunden, Konzentration, Cross-Selling, Preisstreuung,
  Saison und Prognose, Weltkarte) auf den Finance-Daten, ohne Konzernkunden, CHF zum Belegdatum, hinter der
  Finance-Freischaltung. `docs/VERKAUF_2026-10-02.md`.

## Netzwerk: Migration, Bericht, Geraetelandkarte, Altlasten-Score, 2026-10-02, produktiv 12:00

- `c6b2d29`, `7d31df5`: Unterreiter Migration (Wellen, Zeitachse, Fortschritt) und Bericht (PDF ueber Browser-Druck),
  Geraetelandkarte in AD-Infrastruktur/Standorte, Altlasten-Score in Active Directory.

## Netzwerk: AD-Infrastruktur, Gruppenrichtlinien, DNS, Verlauf, 2026-10-02, produktiv 10:14

- `50b6c4a`: vier neue Unterreiter (Replikationsnetz, Sonnenstrahl der OUs, DNS-Alterung, Countdown und Verlauf),
  Drucker in Arbeitsplaetze, Excel-Export, taeglicher AD-Schnappschuss. Enthaelt `65e3a88` (Veeam gruppiert).

## Netzwerk AD: Korrekturen, 2026-10-02, produktiv 08:29 und 08:43

- `ce9e06a`: Klassen `dir-` statt `ad-` (Edge-Werbefilter blendete die inneren Reiter aus). `3e21220`: LAPS/BitLocker
  „nicht lesbar“ statt 0 %, RODC als DC, Turmhoehe nach Wurzel. `65e3a88` (Veeam gruppiert): Deploy offen, VPN weg.

## Netzwerk: Unterreiter Active Directory, 2026-10-02, produktiv 08:07

- `b57d20f`: 3D-OU-Stadt, Lebenszyklus, Aufraeumen, Sicherheit (LAPS, BitLocker, Delegation), Dienste je Server,
  Subnetze ohne AD-Standort, DNS und DCs; DCs als Pruefziele. Erster Deploy-Versuch scheiterte, Laptop nicht im Firmennetz; mit VPN um 08:07 deployed.

## Netzwerk: AD-Computer eingeschaltet, 2026-10-02, produktiv 07:34

- `48121df`: `NetworkProbe:AdEnabled = true` nach Erlaubnis der IT (laut Ingo). Nur Computerkonten, keine Personen.

## Reiter Netzwerk, 2026-10-02, produktiv 07:20

- `1635b46`: Netzkarte, Verfuegbarkeit mit SAP-Fruehwarnung, Netz oder Daten, Sicherheit, Arbeitsplaetze. Pruefung alle
  5 Minuten, nur feste Ziele, SAP nur ueber den oeffentlichen Ping. AD-Auswertung ausgeschaltet. `docs/NETZWERK_2026-10-02.md`.

## Logistik: Verwendung & Risiko, 2026-10-01, produktiv 16:05

- `1a754e0`: Seite `/logistik/verwendung-risiko`, mehrstufige Verwendung und vererbtes Lieferantenrisiko fuer die
  LZ-Code-Komponenten; kein Umsatz je Komponente, weil nur 6 % des CH-Umsatzes erreichbar sind.

## Logistik live, 2026-10-01, produktiv 15:01

- `2be8623`: Seite `/logistik/live` mit Kommissionierung und Produktion aus SAP, Abruf alle 30 s nur bei offener
  Seite, ohne Personendaten. SAP-Sets in `T76K912650`. `docs/LOGISTIK_LIVE_2026-10-01.md`.

## Cockpit: Berndeutsch, Dimmer, Doppel-LED, Logo auf Orange, 2026-10-01, produktiv 14:26

- `92e012e`, `102c20c`. `docs/rag/ARCHITECTURE.md`.

## Cockpit: neuer Skin Trafag CI, 2026-10-01, produktiv 14:14

- `7d1e5b5`: orange Kopfleiste, Logo im hellblauen Kasten mit „Cockpit“ daneben; das bisherige Rot ist ueber den
  Paletten-Knopf weiter waehlbar. `docs/rag/ARCHITECTURE.md`.

## Einkauf: alter Stand waehrend der Neuberechnung, Texte D1/D5, 2026-10-01, produktiv 11:21

- `4e2b55a`: Nach „Delta aktualisieren“ kommt die Seite sofort mit dem Stand vor dem Lauf und einem Hinweis und
  wechselt von selbst auf die neuen Zahlen (Rueckmeldung Armin, `PLATTFORM_TEMPO_2026-09-28.md` 10).
- `3407868`: D1 = FP_DICHTESEN, D5 = FP_DICHTESEN1/2 im Dashboard, solange SAP keinen Text hat.

## HR-Cockpit: Krankheit und Ferien je Fall aus SAP, 2026-10-01, produktiv 10:50

- `c749d32`: neues EntitySet `HrAbsenzSet` (T76, Transport `T76K912644`) liefert jede Abwesenheit aus
  `PA2001` mit Von/Bis. Das Cockpit schneidet Krankheitsfaelle auf den Zeitraum zu und zeigt Ferien,
  Kompensation und Militaer im Zeitraum. Ohne die neue Datei bleibt alles bei Rexx. `docs/HR_KPI.md` 8.7.

## HR-Cockpit: Unfalltage als laufender Monat, 2026-10-01, produktiv 09:44

- `931f151`: BU/NBU kommen nur fuer den laufenden Monat aus SAP; die Kacheln sagen das jetzt. Erster SAP-Abruf
  produktiv um 09:46 mit 1'168 Personen. `docs/HR_KPI.md` 8.6, Absenzen aus `PA2001` 8.7.

## HR-Cockpit: Kader mit 8,1 h je Arbeitstag, 2026-10-01, produktiv 09:12

- `c51445c`: Kader ist, wer in Rexx `Leitung j/n` = ja hat (Ingo, Wochen_Todo 66). Fuer sie gelten 8,1 h
  statt 8,0 h bei Krankheitstagen, Langzeitgrenze und FTE aus der Sollzeit. 800/800. `docs/HR_KPI.md` 8.5.

## Offene Punkte aus aelteren Eintraegen (Original im Archiv)

Status nach `router.md` Regel 2 immer aus dem Issue-Log, nicht von hier. Die Liste „Offene Punkte (nicht erledigt)“ vom August 2026 steht verbatim im Archiv `docs/archiv/LASTCHANGE_ARCHIV_2026-08-01_bis_2026-09-30.md`.

- Server/IIS (seit 2026-07-08, nur direkt am Server moeglich, WinRM gesperrt): App-Pool `startMode=AlwaysRunning` + `processModel.idleTimeout=00:00:00` setzen, damit der 12:00-Timer ohne vorherigen HTTP-Request laeuft. Bis dahin holt `CatchUpMissedRunAsync` verpasste Tageslaeufe beim naechsten Prozessstart nach.
- Betriebshinweis DE/Alphaplan (seit 2026-07-03): Der Alphaplan-Upload nach SharePoint muss VOR dem 12:00-Timer laufen, sonst verwendet der Tagesexport noch den vorherigen ZIP-Stand.

## Aeltere Eintraege / Historie

- Abschnitte 2026-07-30 bis 2026-09-30 (einschliesslich der alten Listen „Offene Punkte (nicht erledigt)“ und „Aktueller Kurzstand“): verbatim in `docs/archiv/LASTCHANGE_ARCHIV_2026-08-01_bis_2026-09-30.md` (verschoben am 2026-10-22).
- Aeltere Archive bis 2026-07-30 (`LASTCHANGE_ARCHIV_bis_2026-07-12.md`, `LASTCHANGE_ARCHIV_2026-07-13_bis_2026-07-30.md`, `HISTORY_CANONICAL.md.raw`, `original_history_raws.zip`) liegen seit `835b317` (2026-09-01) nur noch in Git: `git show 835b317^:TrafagSalesExporter/docs/raw_md_archive/<Datei>`. Entscheid Ingo 2026-10-22: nicht wiederherstellen, neue Archive unter `docs/archiv/`.

## Einstieg / Router

- Ladeweg (verbindlich, `router.md` und `CLAUDE.md`): `router.md` -> Unterrouter `docs/router/` -> Detaildatei. Diese Datei ist nur Aenderungsstand und wird bei Bedarf gelesen (`docs/rag/init.md`).
- Fuehrender Kurzkontext zum Projektstand: `docs/rag/PROJECT.md`.
