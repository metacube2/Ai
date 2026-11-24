# Last Change

## 2026-11-24: PDF Finance fuer Andreas

- `docs/finance_andreas/Finance_Lieferantenzuordnung_Mapping_Stand_2026-11-24.pdf` (5 Seiten mit Grafiken: Kennzahlen, Zeitstrahl, Prozessgrafik Kostenkette, Issue-Topic-Matrix, Ampel und Balken je Land, Entscheid 1; Kopie in Downloads) mit Generator und `finance_grafiken.py`,
  `finance_stand_andreas_pdf.py`: Neuerungen 13. bis 20.11., Kostenkette, Issue-Topic-Matrix, Mapping, Stand je Land, offene
  Fragen an Andreas. Nur aus der Doku, Zahlen der Exporte 18. und 20.11.

## 2026-11-24 11:01: Datenbanksicherung in der App

- `DatabaseBackupService` (`95c5ad5`, Zweig `db-sicherung-2411`): samstags 03:00 bis 05:00, nie waehrend eines Einkauf-Laufs, vier
  eigene Sicherungen neben der DB; Knopf und Liste unter Admin > Aktive Logins. Das Deploy-Werkzeug erkennt die Sicherungen und
  sichert nicht mehr selbst. Server zusaetzlich mit Veeam (IT). Tests 1'842/1'842, deployed 11:01. `docs/rag/DEPLOYMENT.md`.

## 2026-11-24 10:30: Liste Zukaufteile mit Stueckliste/Arbeitsplan an Armin

- Armin hat B65814 bereinigt (Stueckliste und Arbeitsplan geloescht). Liste aus P76 (nur gelesen, Agent sap-t76): 1'286 F-Teile
  Werk 1100 mit Stueckliste oder Arbeitsplan, 1'243 zu pruefen, 43 Lohnbearbeitung; Mail mit Excel von Ingo gesendet.
  `saptasks/zukauf_stuecklisten/NOTES.md`, Plan FIX 29 Abschnitt 10.

## 2026-11-24 10:08: erste Fall-Datei #777 ueber den Rexx Hub

- Tageslauf brach um 09:13 und 09:48 ab (Rexx beantwortete je eine Seite nicht). Fix `142d304`: Seite bei 500/502/503/504,
  Verbindungsabbruch oder Zeitueberschreitung nach 10/30/60 s wiederholen. Tests 1'829/1'829, deployed 09:58 ohne Wochensicherung.
- Fall-Datei 10:08 geschrieben, 4'417 Abwesenheiten, Handexport als `.manuell.xlsx` gesichert. Rexx-NOTES 22 Punkt 8.

## 2026-11-24 09:10: Rexx Hub #777 eingeschaltet

- Ursache 500 auf Seite 56: Exportfeld `time_away` beim API-Benutzer #2, in Rexx entfernt. Stichtag `CaseFromDate = 2026-04-07`
  (`5b3774f`, deployed 08:57), `CaseEnabled = true` (`21d11b4`, deployed 09:10), beide ohne Wochensicherung (weiter faellig).
- Testabruf 168 Seiten, 4'416 Abwesenheiten; Hub vollstaendiger als der Handexport (der hatte nur Antraege ab 04.05.2026).
  Rexx-NOTES 22, `docs/HR_KPI.md` 8.21.

## 2026-11-24: Sicherung auf GitHub

- Gepusht: `feature/supplier-overrides-trit`, `rexxhub-requests-2311` (Produktivstand `df7b874`), `einkauf-tempo-2011`,
  lokales `main` als `main-laptop-2026-11-24` (GitHub-`main` ist auseinandergelaufen, 144 fremde Commits, nicht gemergt).
- Neu `finance-all-wip` (`623f6f1`): unkommittierte Finance_All-Arbeit als Kopie, kompiliert, ungetestet, nicht deployed;
  Hauptordner unveraendert.
- `Tools/DeployHeadless/`: Kopie des Deploy-Werkzeugs (`.tmp_tools/` ist ignoriert), ohne Passwoerter.
- ZLO03: `ZM_LZCODE20_OPT.abap`, `ZM_LZCODE20_OPT_fixes.md`, `BEFUND_SYSTEMABGLEICH_2026-08-03.md` erstmals committet.

## 2026-11-24: Erster Einkauf-Mitternachtslauf ausgewertet, ZLO03 FIX 29 belegt

- Nachtlauf 24.11. 00:00: 49 Minuten statt 90, davon Materialanteil 37 Minuten (`docs/PLATTFORM_TEMPO_2026-09-28.md` 11.6).
- FIX 29: Im Dashboard fehlt H10040 unter den 878x-Gruppen; ZLO03 in P76 nur gelesen: 40731 87 -> 75 Zeilen, 100.23 -> 86.83 CHF
  je Stueck (`zlo03/PLAN_ZLO03_FREMDBESCHAFFUNG_2026-11-23.md` 9). Nur Doku, kein Code, kein Deploy.

## 2026-11-23 15:00: Rexx Hub #777 ueber GET v1/requests, deployed, Schalter aus

- Rexx: `/v1/requests` bei Benutzer #2 frei, zehn Abwesenheitsfelder. Code `001646b` (Pfad, Feldzuordnung, Antraege ohne Abwesenheit
  entfernen, Verteilung im Testabruf) und `df7b874` (Testabruf im Hintergrund), deployed 13:34 und 14:36, Tests 1'821/1'821.
- `CaseEnabled` bleibt false: Rexx antwortet auf Seite 56 mit 500 (Rexx-NOTES 21). Nicht in den Hauptzweig gemergt.


## 2026-11-23: Antwort Artjom (Rexx) in die HR-Doku uebernommen

- `/v1/requests` liefert alle Abwesenheiten (auch direkt gebuchte), damit hat #777 einen Hub-Weg (noch nicht umgestellt);
  sieben bestehende Schnittstellen mit IDs; Lohnarten-Endpunkt bis Ende 2026; SFTP laut Artjom durch Upgreat.
- Rexx-NOTES 20, `docs/HR_KPI.md` 8.20, rag HR_KPI, Router HR, Wochen_Todo 133 + xlsx. Nur Doku.
## 2026-11-23 10:50: Uebergabe zum Schliessen des Chats

- `docs/UEBERGABE_2026-11-23.md`: erledigt, offen, erste Schritte am 24.11.; verlinkt in rag PROJECT, Router Projekt, baum.

## 2026-11-23 10:45: ZLO03 FIX 29 in P76 importiert

- Ingo hat `T76K912940` freigegeben und um 10:30 nach P76 importiert; P76 nur gelesen: TPALOG alle Schritte RC 0000, Feld
  `P_NOFRM` „Fremdteile nicht auflösen“ auf dem Selektionsbild, keine Laufzeitfehler zu den Objekten. Quelltext in P76 nicht
  lesbar; Beleg der Wirkung nach dem Einkauf-Lauf heute Nacht (40731 ca. 93.04 statt 106.26 CHF). Plan Abschnitt 8.

## 2026-11-23 nachmittags: ZLO03 FIX 29 in T76 gebaut (Auftrag T76K912940, nicht freigegeben)

- **Auftrag Ingo:** „leg los, prod ist auch offen fuer dich“. P76 nur gelesen (Elternkette gefuellt, Schalter wirkt dort).
- **T76:** `Z_ZLO03_TURBO2` FIX 29 (Schalter `p_nofrm`, ab Werk aus) und eine Zeile `P_NOFRM = X` im DPC (ZloTopDownSet). Test 40731:
  ohne Haken unveraendert, mit Haken 12 Komponenten weniger, Materialwert 106.26 -> 93.04 CHF je Stueck; 31318: 74.10 -> 59.78 CHF.
- **Offen:** Selektionstext (max. 30 Zeichen), Befund B65814 (Einkaufspreis 4.62 unter Stuecklistensumme 11.64), Info Armin,
  Freigabe und Import durch Ingo. Plan Abschnitt 7.

## 2026-11-23 10:10: Plan ZLO03 FIX 29 (fremdbeschaffte Teile, Schalter), nur T76 gelesen

- **Auftrag Ingo:** Befund in `Dashboard_Materialanteil pro Produktgruppe.xlsx`, dann „lese t76 mache plan“ und „sonst ein Schalter,
  dass man beide Optionen hat“.
- **Ergebnis:** ZLO03 loest Stuecklisten fremdbeschaffter Drehteile auf (Generator ohne Abbruch nach Beschaffungsart); CK11N nicht.
  Bei 40731 zaehlt H10040 (0.73 CHF) zusaetzlich zu C17389 (1.58 CHF). Schalter zur Laufzeit in `Z_ZLO03_TURBO2` machbar (einfach bis
  mittel), ab Werk aus, Dashboard setzt ihn. Plan `zlo03/PLAN_ZLO03_FREMDBESCHAFFUNG_2026-11-23.md`, Entscheide E1 bis E5.
- T76 nur gelesen (Agent sap-t76, eigene GUI-Sitzungen, RFC vom Auto-Modus abgelehnt, nicht wiederholt); P76 nicht. Doku: zlo03/CLAUDE.md H,
  Router SAP, baum, LEARNINGS, Wochen_Todo 169.

## 2026-11-23 08:40: Einkauf-Tempo deployed, Nachhol-Delta abgebrochen, Slot um Mitternacht

- **Auftrag Ingo:** „mache deploy“, dann 08:30 „beende den Delta, er blockiert weitere Arbeiten, soll Mitternacht laufen“.
- **Deploy 07:37** `d4e9a18` (1'816/1'816, bitgleich, Einkauf-Pruefung im Werkzeug erstmals im Einsatz). Nachhol-Delta ab 07:38,
  Hinweisbalken auf `/einkauf` im Chrome gesehen; nach 58 Minuten noch nicht fertig.
- **Deploy 08:36** `155276e` mit `--trotz-einkauf-lauf`: Slot 00:00, Nachholen nur bis 06:00; Lauf auf „Abgebrochen“, kein neuer
  Start. Einkaufsstand bis heute Nacht vom 20.11. 15:10.
- **Offen:** Dauer des ersten Mitternachtslaufs, Merge in `feature/supplier-overrides-trit` (Finance_All unkommittiert im Hauptordner),
  Messung Vorberechnung. Neu von Ingo: ZLO03 loest Stuecklisten fremdbeschaffter Teile auf (Materialanteil), Plan folgt.

## 2026-11-23: Dashboard zaeh am 20.11., Einkauf-Delta nachts und als echtes Delta (gebaut, *seit 07:37/08:36 deployed*)

- **Auftrag Ingo 20.11.:** „anscheinend ist das Dashboard extrem langsam, was ist los“, dann „mache das alles, ev Anzeige im
  Dashboard Achtung ... laeuft, ist langsam“, am 23.11. „mach weiter“.
- **Ursache:** Deploy 12:51 brach den taeglichen Delta (seit 12:31) ab, Nachhol-Lauf 13:39 bis 15:09 in der Arbeitszeit. Der Delta
  laedt seit langem alle rund 178'500 Belege, weil `EKKOSet` den `$filter` ignoriert (90 Minuten). Deploy-Eintrag 12:51 („lief
  nicht“) berichtigt.
- **Gebaut** im Zweig `einkauf-tempo-2011` (`e76df02`, `d4e9a18`, Worktree `C:\TMP\TrafagSalesExporter_tempo2011`), Tests 1816/1816:
  Nachtslot 03:00 ueber den Runner, Auswahl der Belege in der App, Stichtag aus letztem Erfolg, Deploy-Pruefung aus dem
  Serverprotokoll (`PurchasingRunLog`, im lokalen `DeployHeadless` eingebaut), Hinweisbalken bei laufendem Einkauf-Lauf oder Export.
- **Vorberechnung:** behoben, dass der erste Benutzer nach Ablauf des Stands stuendlich die ganze Neuberechnung abwartete (~100 s;
  Cache startete die Hintergrundberechnung synchron). Offen: die Berechnung selbst ist seit 09.10. dreimal langsamer, Messung mit
  Kopie der Wochensicherung braucht Freigabe.
- **Dazu:** sonntags 03:00 Full Load (das echte Delta frischt abgeschlossene Bestellungen nicht mehr auf), nur bei plausibler
  SAP-Antwort. Fehler der ersten Fassung vor dem Commit behoben (Timer haette nach DB-Fehler einen Export ausgeloest).
- **Offen bei Ingo:** Deploy freigeben, Slotzeit 03:00, Messung Vorberechnung. Doku: `docs/PLATTFORM_TEMPO_2026-09-28.md` 11,
  `docs/rag/DEPLOYMENT.md`, `docs/rag/PURCHASING.md`, Review 20.11. (E2/E4/E5, Nr. 10), LEARNINGS, Wochen_Todo 168.

## 2026-11-20 14:45: Code-Review 2 mit Clean-Code- und Testpruefung, nur Doku

- **Auftrag Ingo:** „fuehre ein codereview durch ueber den gesamten quellcode, dann analyse und doings“, danach „schreib das alles
  in ein dokument“, „auch clean code, tdd geprueft?“. Stand `ad4f437`, sechs lesende Bereichs-Agenten, hohe Befunde von der
  Hauptsitzung am Code nachgelesen. Am Code nichts geaendert, kein SAP, kein Deploy.
- **Ergebnis:** `docs/CODE_REVIEW_2026-11-20.md`. Von den 13 Befunden vom 12.11. ist Nr. 13 erledigt, zehn sind am Code bestaetigt
  offen, Nr. 9 und 11 nicht erneut geprueft (Nr. 5 und 9 seit 18.11. produktiv). Neu 5 hohe Befunde: Secrets-Export, Shopfloor-XSS
  (`kw`), HR-Sets ohne `AUTHORITY-CHECK`, `FinanzJournalSet` ohne Pflichtfilter, P76-Werkzeug nicht technisch „nur lesen“. Drei
  bekannte hohe erweitert (`/einkauf/verbindungen` schickt SAP-Zugangsdaten an eine freie Adresse, `/netzwerk/*` offen, Cookie
  ohne Abmelden und Drosselung). *Berichtigt 14:45:* zuerst als „8 neue hohe, zwoelf offen“ gezaehlt (`960bd62`), ohne die
  Befundtexte vom 12.11. gelesen zu haben.
- **Gemessen:** Gesamttest im Review-Worktree 1785/1785 gruen; Zeilenabdeckung ohne Uebersetzungen 50 %, Dienste rund 70 %,
  Razor-Komponenten 0,6 %; 77 % der Code-Commits mit Tests im selben Commit; 15 Dateien ueber 1000 Zeilen, 42 Methoden ueber 100.
- **Offen bei Ingo:** Doings D1 bis D9 freigeben (ohne Entscheid moeglich), Entscheide B1 bis B7. Wochen_Todo 159 nachgefuehrt.
- **Fehler dabei (LEARNINGS):** Review vom 12.11. erst beim Schreiben gefunden und davor im Chat „nirgends erfasst“ behauptet;
  Worktree ohne `trafag_exporter.db`; leeres `git log` mit falschem Pfad als „unveraendert“ gelesen.

## 2026-11-20 13:25: Finance Stufe 2 deployed

- **Auftrag Ingo:** „mache deploy wenn anderer agent schon fertig ist mit seinem eigenen deploy“. Shopfloor-Sitzung war 13:05 abgeschlossen; Patch 10 (`e3248a4`) in den Finance-Zweig gemergt, Feature-Zweig auf `26d23a9`.
- **Deploy 13:23** `26d23a9`, Release-Worktree 1785/1785, kein Einkauf-Delta, Protokoll `.tmp_deploy_2026-11-20_finance_stufe2.log` bitgleich, nur -wal/-shm-Alarme (wieder da). Wirkt ab dem naechsten Export: Spalte Supplier Origin, Topic 3 bei TR IT/TR IN (nach dem naechsten B1-Lauf), TR-IT-Mapping nach Bestaetigung.

## 2026-11-20 13:20: Lieferantenbewertung im Chrome geprueft, Mailtext fuer Einkauf

- **Auftrag Ingo:** Doku als Mailtext fuer den Einkauf, im Chrome pruefen, „ist alles in md festgehalten“.
- **Befund:** Seite stimmt mit dem Mailtext; Link nur mit `/BiDashboard` (`.../BiDashboard/einkauf/lieferantenbewertung`). A/B/C
  2026-01 bis -11: A 20 / 71.1 %, B 30 / 19.2 %, C 208 / 9.8 %, Summe CHF 32.1 Mio. Keine Eintraege; kein neuer Testeintrag (Entscheid).
- **Offen:** Mail an Armin/Marco senden (Ingo), Summe A+B+C gegen Spend-Aufriss (Armin). Doku EINKAUF_SITZUNG 6.8.

## 2026-11-20 13:05: IC-Go-Live Transport T76K912938 freigegeben, Shopfloor Patch 10 deployed

- **Auftrag Ingo:** Mail Fabio „Go-Live heute: IC-Terminierung 1100 -> 1200“ (Entscheid „Neuer Transport“, ZBA1 wartet auf Fabio) und Patch 10 „ja, mit Deploy“.
- **IC:** Transport T76K912938 (Ziel P76) mit genau T441V 02/AE (BESTP A) und T161V F/NB/1100 (ATPCONFMRP leer), freigegeben 13:00:45, gegengelesen. P76 vorher X/X. Import in P76 durch Ingo (13:11), P76 gegengelesen: nur diese zwei Felder anders. Details `saptasks/ic_preisfindung/NOTES.md` 15.
- **Shopfloor:** Patch 10 Forecast Suche/Filter, deployed 12:51 `e3248a4`, 1759/1759, bitgleich; im Board gesehen, Suche und Filter funktionieren. `docs/SHOPFLOOR_2026-10-07.md` 27.6. Finance Stufe 2 muss `e3248a4` vor Deploy mergen.

## 2026-11-20 12:30: Finance Stufe 2 gebaut (nicht deployed), Export 20.11. geprueft, Mail Andreas vorbereitet

- **Auftrag Ingo:** „alles umsetzen“ (Plan `docs/FINANCE_PLAN_MAPPING_STUFE2_2026-11-20.md`, Vorgaben F1 bis F7, Deploy erst nach Rueckfrage); „lesen auf p ist genehmigt“.
- **Code** (`9c6fe53`, Zweig `finance-mapping-2011`, Worktree fin1911, Tests 1785/1785): Mapping auf Artikelnummer TR IT (B), Topic 3 bei TR IT/TR IN durch Artikel mit Kosten 0 in `GroupStandardCosts` (C, begruendete Abweichung vom Plantext), Spalte Supplier Origin mit neuer Spalte `CentralSalesRecords.SupplierSource` (D), Feldhilfe (G). *(ueberholt: deployed 13:23 als `26d23a9`, mit Shopfloor Patch 10)*
- **Export 20.11.:** Zeilen/Umsatz plausibel, Indien-Ware IT 455 -> 51, UK 88 -> 17, US 7 -> 2, Summary 11 von 18 Gruppen mit Marge (19.11.: 2), leer FR/IN/US und IT 2025 wegen Lieferant unklar oder Konzernkosten fehlen. Entscheid 1 neu 0,84 Mio. CHF (2,5 Mio. vom 16.11. ueberholt).
- **Paket E:** Typencode in `MARA-ZZTYP`/`ZZTYP_F4` (T76); P76-Lesen vom Auto-Modus abgelehnt, offen. SE16N-Dumps in T76 in LEARNINGS.
- **Downloads (nicht gesendet):** Mail Andreas, Mail Indien (6 Artikel), Top 50 DE zur Pruefung.
- **Offen:** P76 `Zztyp`; Nebenbefund 272 Zeilen mit sehr tiefen Konzernkosten TR AG (H7xxxx).

## 2026-11-20 10:50: Finance Plan Stufe 2 Mapping und Issue Topic (nur Doku)

- **Auftrag Ingo:** Abschrift 18.11. gegen Loesung pruefen, danach Plan fuer Opus 5.5. Kein Code, kein Deploy.
- **Befund:** Ausloeser Andreas (Italien liefert an Spanien, verschiedene Nummern) ist nicht abgedeckt, der Vorschlagslauf sucht nur im Stamm Trafag AG; `DeliveringEntity`/`DeliveringMaterialNumber` der Mapping-Tabelle wirken nirgends. Zahl zu Entscheid 1 steht doppelt (2,5 und 0,85 Mio. CHF).
- **Plan:** `docs/FINANCE_PLAN_MAPPING_STUFE2_2026-11-20.md`, Pakete A bis G, 7 Fragen an Ingo, nicht freigegeben.

## 2026-11-20 11:00: Finance Regelset-Abgleich (nur Doku)

- **Auftrag Ingo:** „wo gibt es ein missmatch in der regel“. Regelset-Word-Dateien gegen Issue-Topic-Matrix und `ResolveIssueTopic` gelesen, kein Code, kein Deploy.
- **Befund:** 5 Abweichungen (Indien LRD, Kein Lieferant Regel 4 gegen Beschluss 27.08., lokale Kosten fehlen, Lieferant unklar, Regel 1 im Text). Details und Frageliste: `docs/FINANCE_REGELSET_ABGLEICH_2026-11-20.md`, `docs/FINANCE_SUPPLIER.md` Abschnitt 13, ISS-003.
- **Offen:** Entscheide Ingo/Andreas zu Frage 1 und 2 vor der Mail an Andreas; Export 20.11. 12:10 pruefen.

2026-11-20 10:10 (T161V NB/1100 wieder leer auf Wunsch Fabio, Details NOTES ic_preisfindung 14); davor 2026-11-20 09:50 (Handbestellwesen: AD-Abgleich ausgefuehrt, Nutzung seit 22.10. praktisch null, nur Doku); davor 2026-11-20 09:05 (Rueckbau Fabio-Test T161V; T441V 02/AE wieder A); davor 2026-11-19 15:30 (Finance Mapping: Regeltreffer erst bestaetigt, DE-Schraegstrich-Regel deployed `aade490`, 705 Treffer); davor 2026-11-19 15:15 (Rexx Hub eingeschaltet, P76 Rexx gelesen, Mail Artjom); davor 2026-11-19 14:15 (Finance Mapping: Status offen und TRCH/TRAT ausgenommen deployed 19.11. 14:10 als `47e3e98`); davor 2026-11-19 13:50 (Finance Issue Topic und Mapping deployed `b2a0152`); davor 2026-11-19 13:02 (Einkauf: Lieferantenbewertung Schreibrecht Armin/Marco, Abnahme bestanden, deployed `e8a8743`); davor 2026-11-19 11:41 (Einkauf: eigener Reiter Lieferantenbewertung mit Paket 2, deployed `7b2ff9f`); davor 2026-11-19 10:33 (SAP-Werkzeug: Fabios Paket verglichen, vier GUI-Skripte und drei P76-Exporthilfen uebernommen; Token-Regel „Vorrang 0“ im Router); davor 2026-11-19 10:12 (Fabios Antwort zum Shopfloor-Paket, Entscheide; nur Doku); davor 2026-11-19 09:04 (Shopfloor Paket Fabio deployed `591c96c`); davor Stand: 2026-11-19 08:45 (Materialanteil produktiv bestaetigt; Shopfloor-Paket Fabio 19.11. in Arbeit); davor 2026-11-18 18:24 (Fabio: Pruefumfang 02/AE „Mit Bestellungen“ in T76/100 auf A seit 18:11, Rueckbau steht aus; Analyse ZBA1/902); davor 18:05 (Materialanteil: Sprachkorrektur `1417a3a` deployed 17:57, Pruefung produktiv steht aus; Finance-Plan Mapping/Issue Topic); davor 2026-11-18 12:14 (T161V-Test Fabio in T76 ausgefuehrt, Rueckbau steht aus; Mailentwurf Andreas berichtigt); davor 11:46 (Finance nachgeprueft: sechs Berichtigungen, Entscheid 1 noch 0,85 Mio. CHF, Ursachen UK/IN/Indien-Ware gemessen, Word „Offene Punkte“); davor 10:48 (Deploy 10:45 `e316353` Handbestellwesen; Ampeln Lieferant/HK aus Sales_All 18.11.); davor 10:32 (Handbestellwesen: Mails Sandro/Vinc in der klassischen Ansicht, `3c18493`; Exportlauf nach Deploy geprueft); davor 10:00 (Ampel je Land: Lieferant FINANCE_SUPPLIER 12, Herstellkosten FINANCE_STANDARDKOSTEN 13, neuer Befund UK); davor 09:45 (Outlook-Entwuerfe an Rohail, Cornell, Paola und Andreas angelegt; ZLO03-PCBA-Plan abgelegt); davor 09:35 (Deploy 09:31 `8d15d48`: B1-Lieferant aus dem letzten Wareneingang und Spanien `c9208b5`); davor ~09:20 (Finance: B1-Lieferant gebaut, Regelset-Dokument mit Beispielen; *Uhrzeit dort zuerst als 10:30 geschaetzt, berichtigt*); davor 08:25 (Deploy 08:15 `f2c0267`: Handbestellwesen Stammdaten/Freigabe-Rollen, Lieferanten-Klasse A/B/C); davor 08:10 (Einkauf: Antwort Armin, Plan Paket 1 Lieferantenbewertung; Shopfloor: Stichtagswerte 18.11. fuer Fabio); davor 2026-11-17 17:25 (Uebergabe Sitzung Finance/Handbestellwesen/Shopfloor-Entscheide, Chat geschlossen); davor 17:10 (Shopfloor: Paket A abgeschlossen, P76-Termine und Kalender gelesen); davor 16:50 (Shopfloor: Entscheide Fabio 17.11. aufgenommen, Paket A laeuft); davor 15:29 (Uebergabe, Chat geschlossen); davor 15:17 (Antwort an Fabio gesendet); davor 15:11 (Rexx Hub: Admin-Zugang, Rueckfrage an Artjom gesendet); davor 14:45 (Shopfloor Ergaenzung Fabio 17.11. deployed `32bf356`; Lieferantenbewertung: Mail an Armin); davor 2026-11-17 nachmittags (Shopfloor: Uebergabe, Phase C wartet auf Fabio); davor 12:50 (Handbestellwesen AD-Abgleich Teil 2; Deploy 12:36 `44a3ed1` nahm den Shopfloor-Deploy zurueck, gemeinsamer Stand `2e62163` 12:43); davor 12:40 (Shopfloor: CO-PA schreiben und Arbeitsplatzfilter M* ein, deployed `3298c93`, CO-PA 02. bis 16.11. nachgeladen); davor 11:45 (Handbestellwesen: naechtlicher AD-Abgleich deployed `6a7de50`, Schreiben aus); davor 2026-11-17 (Wochen_Todo nach der Sitzung mit Oscar: Zeilen 164 bis 167 neu); davor 2026-11-16 22:20 (Shopfloor: Status- und Materialfilter eingeschaltet, deployed `7cc1a62`, Logistik 02. bis 12.11. nachgeladen); davor 16:10 (Nachweis Blatt Lieferantenzuordnung am Export 12:11 erbracht, Excel und Mailentwurf fuer Andreas); davor 09:40 (Sales_All Blatt Lieferantenzuordnung deployed `2da2682`; davor 08:50 Export-Seite Download deployed `e10ab3a`); davor 2026-11-13 abends (Shopfloor Paket Fabio 13.11.: Phase A deployed `d754a76`, Phase B Cockpit deployed `9100537`, T76 `T76K912906` fertig und offen; davor 10:50 Finance Lieferantenzuordnung in Sales_All deployed `df48301`, vier Mailentwuerfe; davor 09:30 Einkauf Wuensche Marco deployed `052c5a3`; davor 08:15 Shopfloor Mail Fabio 13.11.: Stichtagswerte, Morgenwerte, Verzug ein, deployed `0e94279`; davor 2026-11-12 14:25: Uebergabe ZLO03 FIX 28, in P76 wirksam, Werkzeugproblem behoben; davor 13:50: Uebergabe, Chat geschlossen: Doku-Pruefung und Code-Review abgeschlossen, offene Punkte in Reihenfolge; davor 13:45: ZLO03 FIX 28 in T76, `T76K912902` *(seit 13:54 in P76, Nachtrag 14:05)*; davor 12:00: Deploy 11:33 `e956183`; Spanien Supplier umgesetzt `c9208b5`, nicht deployed; ZC12 und IC warten; Lieferantenbewertung; WLAN; Doku-Pruefung); davor 2026-11-11 abends (Uebergabe, Chat geschlossen; davor nachmittags RFC-Rechte FPA; davor IC V1 ZICAA1 in T76 umgesetzt; davor 08:14 Wochen_Todo 122/141 zusammengelegt). Die Uebergabe vom
2026-11-10 spaet gilt weiter vor allen aelteren Abschnitten darunter.

## 2026-11-20 09:50: Handbestellwesen, AD-Abgleich ausgefuehrt und Nachpruefung produktiv (nur Doku)

- **Auftrag Ingo:** „pruefe alles, kannst du auf chrome zugreiffen“, dann „ja nachfuehren dann todos fuer mailtext“. Nur gelesen (Windows-Chrome), kein Code, kein SAP, kein Deploy.
- **Befund:** AD-Abgleich am 20.11. 09:08 von Ingo ausgefuehrt: 15 deaktiviert, 94 Altbestand zu AD-Eintraegen, 0 Namen nachgefuehrt. 12 offene Bestellungen Deaktivierter warten auf
  Neuzuweisung. **Seit dem 22.10. praktisch keine Bestellung mehr im Tool** (November: eine ueber CHF 9.99; offene Bestellungen unveraendert 1'289). Freigabe ueber CHF 1'000
  weiter nicht nutzbar (2 von 109 mit Limit ueber 1'000, 22 Leiter ohne Eintrag, 12 mit Kostenstelle; Leiter von 250 und 700 jetzt inaktiv).
- **Offen:** Konto der anderen Person (Kandidat `ran`, nicht belegt, Protokolltabelle lesen); Einkauf fragen, warum nicht mehr bestellt wird; Neuzuweisung der 12; Stammdatenpflege.
- **Doku:** Konzept 6.15 (und 6.12, Kopf, Abschnitt 7 als ueberholt markiert), rag PURCHASING, LEARNINGS, Wochen_Todo 120/165 + xlsx. Einzelheiten Konzept 6.15.
- **Uebergabe, Sitzung geschlossen 20.11.:** Mailentwurf an den Einkauf (sieben Fragen) liegt bei Ingo, nicht gesendet, Inhalt in Konzept 6.15; offen ist,
  ob dort 431 (aelter als drei Jahre) oder rund 930 (aelter als ein Jahr) alte offene Bestellungen stehen. Naechste Sitzung: Antwort Einkauf abwarten,
  Kandidat `ran` nur lesend in einer Kopie der Tagessicherung pruefen (Tabelle `Protokoll`).

## 2026-11-20 09:05: Rueckbau Fabio-Test T161V in T76/100 (T441V 02/AE versehentlich mit zurueck, wieder A)

- **Auftrag Ingo:** „kannst du einstellungen vom customizing zuruecknehmen von fabio“ (Modell Sonnet gewaehlt, Agent sap-t76).
- **Gemacht:** `T161V` F/NB/1100 `ATPCONFMRP` leer -> X (08:50, Auftrag T76K912926). **Irrtum:** `T441V` 02/AE `BESTP` wurde 08:52 ebenfalls auf X gesetzt, obwohl nur T161V gemeint war; 09:02:55 wieder auf A (Auftrag T76K912928), gegengelesen (NOTES 13 Berichtigung). Zuvor:
  beide Tabellen schluesselweise gegen die VORHER-Dateien gelesen, 0 Abweichungen; Zielzeilen von der Hauptsitzung per RFC gegengelesen.
  Auftraege offen, nie freigegeben; P76 und Mandant 090 nicht beruehrt.
- **Offen:** Fabio informieren, ab 09:03 neu aufrufen (Ingo); Auftraege T76K912926 bis 929 loeschen oder stehen lassen.
  Doku `saptasks/ic_preisfindung/NOTES.md` 13.

## 2026-11-19 15:15: Rexx Hub eingeschaltet, P76-Lesen Rexx-Schnittstellen, Mail an Artjom (deployed `b7f52a9`, `85b2449`, `a848f93`)

- **Auftrag Ingo:** Warehouse-Abfragen im Rexx Hub bauen, „mach alles was du machen kannst“, „alles fertigmachen“, P76 und T76 lesen,
  Mail an Artjom in Outlook.
- **Gemacht:** Rexx-DWH sind nur Kennzahlen (`datawarehouse/775` = 404); #775 als Export-Felder von `GET /v1/employees` beim eigenen
  API-Benutzer #2 „BI-Dashboard HR-Cockpit“ (31 Felder, 100 Zeilen je Seite, IP-Sperre 212.243.171.23). Abruf umgestellt, Doppelzeilen
  bereinigt, Summen im Testabruf, Zeitraum gegen Handexport geprueft, eingeschaltet 13:25; erster Abruf 13:28 (395 Zeilen). Quellsystem
  `REXXHUB` (Token von Ingo). P76 gelesen (Agent): Absenzen seit 09.07. nicht nach SAP importiert, Lohnarten-Job unter `EXPRIS_SIWY`.
  Hub hat keinen Schreibweg fuer Lohnarten. Mailentwurf an Artjom in Outlook Web (nicht gesendet).
- **Offen:** #777 (Artjom), Mail senden (Ingo), User #1 Freigaben sperren (Artjom), Verfallsdatum #2, Sichtpruefung HR-Cockpit,
  Sonja wegen Absenz-Import seit Juli. Doku `saptasks/rexx_schnittstelle/NOTES.md` 16 bis 19, `docs/HR_KPI.md` 8.19.

## 2026-11-19 14:24: Finance Mapping, Regeltreffer erst bestaetigt, Regel DE Typencode/Sachnummer (deployed 15:25 als `aade490`)

- **Entscheide Ingo 14:20:** Regeltreffer wirken erst nach Bestaetigung (Fehltreffer TRDE 60077 -> 47269 hinter „ersetzt“); Nummern
  hinter ersetzt/alt/replaces/Kunden-Artikelnummer zaehlen nicht; nie von Hand bestaetigte Regelzeilen gehen im naechsten Lauf auf
  Vorschlag zurueck. Neue Regel TRDE: Nummer nach dem letzten Schraegstrich, im Werkstamm 1100 -> Vorschlag; Zaehlung als Meldung.
- **Commit** `854e4c6` auf `9eccf7c`, Tests 1759/1759. Nicht deployed. Detail Plan-Datei Abschnitt 0.
- **Deploy 15:25** `aade490` (Release-Worktree 1'759/1'759, `-wal` 222 KB, Monitor offen, bitgleich, nur `-wal`/`-shm`-Alarme). Vorschlagslauf produktiv: **705 Regeltreffer DE-Schraegstrich, Umsatz 5'300'214 EUR** (wirken erst nach Bestaetigung); 908 Vorschlaege aus 2'155 Artikeln, Texte Trafag AG 66'600. Gemergt in `feature/supplier-overrides-trit`.


## 2026-11-19 13:02: Lieferantenbewertung, Schreibrecht Armin/Marco, Netzwerkpfade, Abnahme (deployed `e8a8743`)

- **Auftrag Ingo:** „ja ich armin marco“ (Schreibrecht), „2, ja“ (Abnahme mit Testeintrag), „mach weiter alles fertig machen“.
- **Gemacht:** `Lieferantenbewertung:EditUsers` = `koi`, `gun` (Armin), `wid` (Marco, Kontoname im AD nachgeschlagen); Netzwerkpfade
  als Text (`d2f2edb`); Rexx-Stand `b7f52a9` in den Zweig gemergt. Deploy 12:58 ohne Alarm, gemergt `31a2c66`.
- **Abnahme 13:01:** Testbewertung Lecotronic AG gespeichert, Verlauf und Uebersicht richtig, wieder geloescht; kein Testeintrag in der DB.
- **Offen:** Link an Armin und Marco (Ingo), Summe A+B+C/D1, Q2, Liefertreue, SAP-Score/Preis, Sprachen ausser DE/EN, Handbuch.
  Doku EINKAUF_SITZUNG 6.8, rag DEPLOYMENT/PURCHASING/PROJECT, Wochen_Todo 122.

## 2026-11-19 11:41: Einkauf, eigener Reiter Lieferantenbewertung mit Paket 2 (deployed `7ae22c2`, Nachzug `7b2ff9f`)

- **Auftrag Ingo:** Armins Mail (A/B/C ueber den Dashboard-Zeitraum mit Disponent 001-006; Bewertungen jederzeit, Uebersicht zeigt die
  zuletzt erstellte; SAP-Score/Preis Anfang Dezember; „bitte neues Registerblatt unter dem Einkauf einfuegen“), dann „mache
  Lieferantenbewertung … setze alles um, ausser SAP-Score“, „nicht mit anderen Agenten kollidieren wegen Deploy“.
- **Gebaut** (Zweig `lieferantenbewertung-1911`, eigener Worktree, gemergt `843546f`): Reiter `/einkauf/lieferantenbewertung` im Menue
  Einkauf; Klasse A/B/C dorthin verschoben; Paket 2 in `Modules/Lieferantenbewertung/` mit eigener DB `lbdaten\lieferantenbewertung.db`:
  sieben Themenfelder mit Verlauf, Zertifikate mit Ampel, Massnahmen, Reviews/Audits, Dokument-Links. Schreibrecht
  `Lieferantenbewertung:EditUsers` (damals nur `koi`, seit 12:58 auch `gun`/`wid`) und Admins. Tests 1'664/1'664.
- **Deploy** 11:31 und 11:38 nach Koordination (kein anderer Deploy) und WAL-Pruefung (kein Einkauf-Delta); im Chrome geprueft, nichts
  gespeichert. Nachzug: Platzhalter „nicht beurteilt“ in der Bewertungsmaske.
- **Offen (Stand 11:41, ueberholt durch 13:02: Kurzzeichen erledigt):** Kurzzeichen fuer `EditUsers` (Einkauf/Qualitaet, Ingo), Link an Armin/Marco, Q2, Liefertreue, SAP-Score/Preis, Handbuch.
  Doku `docs/EINKAUF_SITZUNG_2026-10-22.md` 6.8, rag PURCHASING/PROJECT/DEPLOYMENT, Router Einkauf, Wochen_Todo 122 + xlsx.
## 2026-11-19 14:15: Finance Mapping Nachtrag deployed (`47e3e98`)

- Merge-Stand `47e3e98` (Status offen `06dd7f0`, TRCH/TRAT ausgenommen `5fece84`), Release-Worktree 1'744/1'744, `-wal` 247 KB, Monitor offen, Protokoll `.tmp_deploy_2026-11-19_finance-mapping4.log` („Commit 47e3e98“, bitgleich, nur `-wal`/`-shm`-Alarme).
- Im Chrome unter `/BiDashboard/finance-cockpit/mapping` geprueft, Vorschlagslauf gestartet: TRAT-Zeilen weg, 2'164 Zeilen (Vorschlaege und offen).
- Befund beim Ansehen (nicht umgebaut): TRDE-Nummern haben die Form `Typencode/Sachnummer` (z. B. `8258.85.2317/55441`); die Nummer nach dem Schraegstrich ist vermutlich die Trafag-Sachnummer und wird von keiner Regel gelesen. Und die sofort wirkende Regel „Trafag-Nummer im Text“ griff bei `8256.29.2315/60077` auf die Nummer hinter „ersetzt“ (47269, alte Ausfuehrung). Beides Entscheid Ingo.

## 2026-11-19 13:57: Finance Mapping, Status offen fuer alle Artikel mit Mapping-Bedarf (*ueberholt: deployed 19.11. 14:10 als `47e3e98`*)

- **Auftrag Ingo:** Mapping-Excel und Seite zeigen jede (TSC, Nummer) mit Issue Topic 1, 2, 6, 7, auch ohne Vorschlag (Status
  „offen“, wirkt nie), mit Gewicht. Gespeichert im Vorschlagslauf, im Export nur in der Datei ergaenzt. Commit `06dd7f0` auf
  `b2a0152`, Tests 1738/1738. Nicht deployed.
- **Befund TRAT (nicht umgebaut):** Vorschlaege fuer TRAT entstehen, weil AT-Material nur im Werk 1200 nicht im CH-Werkstamm 1100
  steht (Topic 2); kein Mapping-Fall. *Entscheid Ingo 2026-11-19 14:01:* TRCH/TRAT ganz aus dem Mapping ausgenommen (`5fece84`, Tests 1744/1744, nicht deployed).

2026-11-19 13:29 (Nachtrag: use 0 rechnet mit Kosten 0, MAKTSet-Texte im Vorschlagslauf, `1576766`); davor 2026-11-19 13:16 (Finance: Issue Topic, Mapping-Tabelle, Indien-Schluesselregel im Zweig `finance-mapping-1911`, nicht deployed); davor 2026-11-19 10:33 (SAP-Werkzeug: Fabios Paket verglichen, vier GUI-Skripte und drei P76-Exporthilfen uebernommen; Token-Regel „Vorrang 0“ im Router); davor 2026-11-19 10:12 (Fabios Antwort zum Shopfloor-Paket, Entscheide; nur Doku); davor 2026-11-19 09:04 (Shopfloor Paket Fabio deployed `591c96c`); davor Stand: 2026-11-19 08:45 (Materialanteil produktiv bestaetigt; Shopfloor-Paket Fabio 19.11. in Arbeit); davor 2026-11-18 18:24 (Fabio: Pruefumfang 02/AE „Mit Bestellungen“ in T76/100 auf A seit 18:11, Rueckbau steht aus; Analyse ZBA1/902); davor 18:05 (Materialanteil: Sprachkorrektur `1417a3a` deployed 17:57, Pruefung produktiv steht aus; Finance-Plan Mapping/Issue Topic); davor 2026-11-18 12:14 (T161V-Test Fabio in T76 ausgefuehrt, Rueckbau steht aus; Mailentwurf Andreas berichtigt); davor 11:46 (Finance nachgeprueft: sechs Berichtigungen, Entscheid 1 noch 0,85 Mio. CHF, Ursachen UK/IN/Indien-Ware gemessen, Word „Offene Punkte“); davor 10:48 (Deploy 10:45 `e316353` Handbestellwesen; Ampeln Lieferant/HK aus Sales_All 18.11.); davor 10:32 (Handbestellwesen: Mails Sandro/Vinc in der klassischen Ansicht, `3c18493`; Exportlauf nach Deploy geprueft); davor 10:00 (Ampel je Land: Lieferant FINANCE_SUPPLIER 12, Herstellkosten FINANCE_STANDARDKOSTEN 13, neuer Befund UK); davor 09:45 (Outlook-Entwuerfe an Rohail, Cornell, Paola und Andreas angelegt; ZLO03-PCBA-Plan abgelegt); davor 09:35 (Deploy 09:31 `8d15d48`: B1-Lieferant aus dem letzten Wareneingang und Spanien `c9208b5`); davor ~09:20 (Finance: B1-Lieferant gebaut, Regelset-Dokument mit Beispielen; *Uhrzeit dort zuerst als 10:30 geschaetzt, berichtigt*); davor 08:25 (Deploy 08:15 `f2c0267`: Handbestellwesen Stammdaten/Freigabe-Rollen, Lieferanten-Klasse A/B/C); davor 08:10 (Einkauf: Antwort Armin, Plan Paket 1 Lieferantenbewertung; Shopfloor: Stichtagswerte 18.11. fuer Fabio); davor 2026-11-17 17:25 (Uebergabe Sitzung Finance/Handbestellwesen/Shopfloor-Entscheide, Chat geschlossen); davor 17:10 (Shopfloor: Paket A abgeschlossen, P76-Termine und Kalender gelesen); davor 16:50 (Shopfloor: Entscheide Fabio 17.11. aufgenommen, Paket A laeuft); davor 15:29 (Uebergabe, Chat geschlossen); davor 15:17 (Antwort an Fabio gesendet); davor 15:11 (Rexx Hub: Admin-Zugang, Rueckfrage an Artjom gesendet); davor 14:45 (Shopfloor Ergaenzung Fabio 17.11. deployed `32bf356`; Lieferantenbewertung: Mail an Armin); davor 2026-11-17 nachmittags (Shopfloor: Uebergabe, Phase C wartet auf Fabio); davor 12:50 (Handbestellwesen AD-Abgleich Teil 2; Deploy 12:36 `44a3ed1` nahm den Shopfloor-Deploy zurueck, gemeinsamer Stand `2e62163` 12:43); davor 12:40 (Shopfloor: CO-PA schreiben und Arbeitsplatzfilter M* ein, deployed `3298c93`, CO-PA 02. bis 16.11. nachgeladen); davor 11:45 (Handbestellwesen: naechtlicher AD-Abgleich deployed `6a7de50`, Schreiben aus); davor 2026-11-17 (Wochen_Todo nach der Sitzung mit Oscar: Zeilen 164 bis 167 neu); davor 2026-11-16 22:20 (Shopfloor: Status- und Materialfilter eingeschaltet, deployed `7cc1a62`, Logistik 02. bis 12.11. nachgeladen); davor 16:10 (Nachweis Blatt Lieferantenzuordnung am Export 12:11 erbracht, Excel und Mailentwurf fuer Andreas); davor 09:40 (Sales_All Blatt Lieferantenzuordnung deployed `2da2682`; davor 08:50 Export-Seite Download deployed `e10ab3a`); davor 2026-11-13 abends (Shopfloor Paket Fabio 13.11.: Phase A deployed `d754a76`, Phase B Cockpit deployed `9100537`, T76 `T76K912906` fertig und offen; davor 10:50 Finance Lieferantenzuordnung in Sales_All deployed `df48301`, vier Mailentwuerfe; davor 09:30 Einkauf Wuensche Marco deployed `052c5a3`; davor 08:15 Shopfloor Mail Fabio 13.11.: Stichtagswerte, Morgenwerte, Verzug ein, deployed `0e94279`; davor 2026-11-12 14:25: Uebergabe ZLO03 FIX 28, in P76 wirksam, Werkzeugproblem behoben; davor 13:50: Uebergabe, Chat geschlossen: Doku-Pruefung und Code-Review abgeschlossen, offene Punkte in Reihenfolge; davor 13:45: ZLO03 FIX 28 in T76, `T76K912902` *(seit 13:54 in P76, Nachtrag 14:05)*; davor 12:00: Deploy 11:33 `e956183`; Spanien Supplier umgesetzt `c9208b5`, nicht deployed; ZC12 und IC warten; Lieferantenbewertung; WLAN; Doku-Pruefung); davor 2026-11-11 abends (Uebergabe, Chat geschlossen; davor nachmittags RFC-Rechte FPA; davor IC V1 ZICAA1 in T76 umgesetzt; davor 08:14 Wochen_Todo 122/141 zusammengelegt). Die Uebergabe vom
2026-11-10 spaet gilt weiter vor allen aelteren Abschnitten darunter.

## 2026-11-19 13:50: Finance Issue Topic und Mapping deployed

- Auftrag Ingo („mach weiter alles fertigmachen“). Zweig `finance-mapping-1911` mit Produktivstand `a848f93` gemergt (`6e394a5`), Release-Worktree 1'732/1'732, kein Einkauf-Delta (`-wal` 424 KB unveraendert), Monitor offen, keine Sicherung (Wochenregel).
- Deploy 13:32 `6e394a5`: Mapping-Seite hing (SQLite: `decimal` in ORDER BY). Davor ein **unnoetiger Rueckbau** auf `a848f93` (13:34), weil die Pruefung die Adresse ohne `/BiDashboard/` aufrief; danach wieder `6e394a5` (13:37).
- Korrektur `b2a0152` (Sortierung im Speicher), Suite 1'732/1'732, deployed 13:41, Commit im Protokoll geprueft. Seite `/BiDashboard/finance-cockpit/mapping` laedt, Vorschlagslauf einmal gestartet: 245 Vorschlaege mit Texten aus MAKTSet.
- Gemergt in `feature/supplier-overrides-trit` als `3268fa1` (fremde Finance_All-Zeile in `Program.cs` voruebergehend beiseite gelegt und unveraendert zurueck).
- Offen: erster Sales_All-Export mit Issue Topic pruefen (IT Ware aus Indien 448 -> ~47, UK 87 -> ~17, Zeilen/Umsatz je Land gleich, Stichprobe je Topic); Vorschlaege bestaetigen; Codes 8/9/10 und E4 mit Andreas; „Saubere Kostenbasis %“ zaehlt use-0-Zeilen weiter als nicht sauber.

## 2026-11-19 13:16: Finance Issue Topic, Mapping-Tabelle, Indien-Schluesselregel (Zweig, nicht deployed; *ueberholt: deployed 13:41 `b2a0152`*)

- **Auftrag Ingo 19.11.** nach Plan `docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md` (AP1 bis AP4), Entscheide E1 A, E5, E6 B, E7 Go, Matrix 1 bis 7 (Korrektur: gefundener Artikel bleibt 3/4).
- **Commits** `75b320d`/`12efb44` im Zweig `finance-mapping-1911` (Worktree `C:\TMP\TrafagSalesExporter_fin1911`), Tests 1700/1700. In `Program.cs` eine DI-Zeile, in `DatabaseSeedService.cs` ein Menuelink (reserviert von der Einkauf-Sitzung, Merge erst danach).
- **Nicht deployed.** Vor dem Deploy: Merge nach dem Einkauf-Deploy, Tests im Release-Worktree, erster Export pruefen (IT/UK Ware aus Indien, Zeilen und Umsatz je Land gleich), Vorschlagslauf einmal starten. Nachtrag 2026-11-19 13:29 (`1576766`, Tests 1707/1707): use 0 rechnet mit Kosten 0; Materialtext Trafag AG aus MAKTSet im Vorschlagslauf (live nach dem Deploy pruefen).
- **Doku:** Plan-Datei Abschnitt 0, FINANCE_SUPPLIER 12a, FINANCE_STANDARDKOSTEN 13a, rag FINANCE, Issue-Log ISS-003, LEARNINGS.

## 2026-11-19 10:33: SAP-Werkzeug aus Fabios Paket, Token-Regel im Router (nur Doku und Skripte)

- **Auftrag Ingo:** `Downloads\sap-werkzeug.zip` (Fabios Stand 19.11. 10:05) ist neuer; uebernehmen, was neuer ist. Dazu: Learnings „Werkzeug statt
  erneuter KI-Abfrage“ prominent in `router.md`, Ziel moeglichst wenig Token (Ingo wieder auf dem kleinen Plan).
- **Verglichen per `diff`:** `P76Lesen.vbs`, `P76SitzungSchliessen.vbs`, `SapGuiP76Hilfe.vbs`, `SapGuiT76Hilfe.vbs` (Fehler `Children(Count-1)` behoben,
  `gridcols`/`tabrow`/`focus`) und `P76Se16Export.vbs`, `P76Se16nExport.vbs`, `P76SitzungAufraeumen.vbs` bytegleich nach `.tmp_sap_probe/`;
  an Ingos GUI noch nicht gelaufen. Nicht uebernommen: Schreibskripte, IC-Wochenlauf-Skripte, `RunSapProbe.ps1`/`SapCredential.ps1`.
- **Doku:** `router.md` neuer Abschnitt „Vorrang 0: Token sparen“, SAP-Arbeitsweise 6/7, LEARNINGS SAP 2026-11-19, Agent `sap-t76`.
- **Offen:** Textvorschlag Token-Regel an Fabio (im Chat, Ingo leitet weiter); beim naechsten P76-Lesen die neuen Skripte beobachten.

## 2026-11-19 10:12: Fabios Antwort zum Shopfloor-Paket, Mailentwuerfe (nur Doku)

- **Rueckmeldung an Fabio:** 09:09 als Antwort-Entwurf in Outlook Web angelegt (je Paketpunkt, dazu T76 02/AE und ZBA1/902); Ingo sendet selbst.
- **Fabios Antwort (Entwurf, Downloads 10:05):** Verkauf 17.11. erklaert (KE30-Abzug 07:35 bis 07:47 mit AE 9'392.51 und Umsatz 10'408.20 vom 17.11., Board richtig);
  BAdI `ZZ_MD_ADD_COL_EZPS` in P76 aktiv. Entschieden: Tagesziele TR5 800/DW 800/TX 1'300 aus dem Excel-Import bis Go-live; woechentlich einlesen nach Fabios
  Freigabe der Vorschau; 155 Widersprueche bereinigen die Teamleiter. **Offen:** Liste je Abteilung an Fabio, erste Vorschau, 20.11. Pruefung; bei Fabio SEH 3'024 und Rueckbau T76.
  `docs/SHOPFLOOR_2026-10-07.md` 27.5, `docs/shopfloor_paket_2026-11-19/Antwort_Fabio_2026-11-19.md`.
- **Armin:** Antwortentwurf als `.eml` in Downloads (nicht gesendet), `zlo03/BEFUND_SPRACHE_MATERIALANTEIL_2026-11-18.md` 8.

## 2026-11-19 08:45: Materialanteil bestaetigt, Shopfloor-Paket Fabio 19.11. in Arbeit

- **Einkauf:** Einkauf-Lauf 18.11. 18:01 bis 19:29 ok, 262'856 statt 112'112 Stuecklistenzeilen. FP_DICHTESENS 10.08.00 273'028 CHF
  (Armin 257'717), Materialanteil 32.3 % statt 8.5 %. `zlo03/BEFUND_SPRACHE_MATERIALANTEIL_2026-11-18.md` 6a. Offen: Antwort an Armin.
- **Shopfloor (Mail Fabio 19.11. 08:17, Auftrag Ingo „paket abarbeiten“): deployed 09:03 `591c96c`.** Tagesziel-Fix, Offen-Regel nach Status mit Widerspruchs-Kennzeichen,
  Excel-Import der Rueckstandslisten (Vorschau, Sicherung, nichts loeschen; in Produktion noch nicht ausgefuehrt), Kalender CH, Auftragsliste je Stichtag (ab 20.11. 04:01),
  Naechster Vorgang ohne SAP-Aenderung (BAdI `ZZ_MD_ADD_COL_EZPS`, nicht `ZXM61U01`). Messbefund und P76-Ursachen: `docs/SHOPFLOOR_2026-10-07.md` 27, `saptasks/shopfloor_sap/P76_URSACHEN_2026-11-19.md`.
  **Offen:** Rueckmeldung an Fabio je Punkt (Ingo), Excel-Import produktiv (Vorschau zuerst), 20.11. Auftragsliste und Forecast DS pruefen, 155 Widersprueche (Fabio).

## 2026-11-18 18:24: Fabio - Pruefumfang 02/AE auf A (T76), Analyse ZBA1 „AB final“

- **Test Fabio (Auftrag Ingo „dann wieder was von Fabio“):** T76/100 `T441V` 02/AE Feld `BESTP` („Mit Bestellungen“) **X -> A,
  aktiv seit 18:11:06**, SM30 `V_441VC`, lokaler Auftrag **T76K912928** (nicht freigeben). Ganze Tabelle vorher/nachher, nur
  dieses Feld anders; von der Hauptsitzung gegengelesen. Testfall 411403/10 (Material 52704, Pruefgruppe 02, Einzelbestand)
  trifft die Zeile. `saptasks/ic_preisfindung/NOTES.md` 11, `PLAN_T441V_MIT_BESTELLUNGEN_2026-11-18.md`.
- **Achtung:** Der Test vom Mittag (T161V NB/1100 `ATPCONFMRP` leer, T76K912926) ist weiter aktiv; gegenueber dem Ausgangsstand
  sind zwei Schalter veraendert. **Offen:** Fabio Bescheid geben (Ingo), beide Rueckbauten auf Zuruf Ingo.
- **Frage Fabio zu ZBA1 / Bedingung 902 (nur gelesen):** ZBA1 ist bei 411403 seit 16:53:30 vorhanden (V_V2-Lauf). „502“ ist die
  Meldung „Bedingung nicht erfuellt“. 902 prueft nicht den Planauftrag, sondern Vollstaendigkeit, Liefer- und Kreditsperre;
  gefehlt hat die bestaetigte Menge der Position. Die Findung laeuft nur beim Sichern des Kundenauftrags (VA02, V_V2), nicht
  beim Fixieren des Planauftrags. `NOTES.md` 12, `ANALYSE_ZBA1_BEDINGUNG_2026-11-18.md` (mit Antwortentwurf, nicht gesendet).

## 2026-11-18 18:05: Materialanteil ohne Leiterplatten (Meldung Armin), Finance-Plan nach der Besprechung mit Andreas

- **Einkauf, Meldung Armin 13:03 (bei FP_DICHTESENS fehlt 10.08.00):** Kein Zusammenhang mit der PCBA-Kopie der ZLO03. Ursache:
  Die Anfrage `ZloTopDownSet` lief englisch, `Z_ZLO03_TURBO2` verwirft Komponenten ohne Kurztext in der Anmeldesprache. Seit dem
  02.11. fehlten alle Einkaufsteile ohne englischen Text (T76: 4'050 von 7'258; produktiv rund 37 % der erwarteten Paare), der
  Materialanteil war in allen Produktgruppen zu tief. **Korrektur 1** (Auftrag Ingo „mach 1“): `sap-language=DE` an der Anfrage,
  `1417a3a`, 1'635 Tests gruen, **deployed 17:57**; Einkauf-Delta 18:01 von Hand gestartet.
  `zlo03/BEFUND_SPRACHE_MATERIALANTEIL_2026-11-18.md`.
- **Offen Einkauf:** Pruefung produktiv nach dem Lauf (10.08.00 bei FP_DICHTESENS, B44212 unter 34887, Zeilenzahl); Antwort an
  Armin erst danach; Korrektur 2 in der ZLO03 (Text mit Rueckfall) als Absicherung, Entscheid Ingo; H90044 hat in T76 nur einen
  englischen Kurztext.
- **Finance, Besprechung Ingo/Andreas 14:37 (Abschrift):** Plan geschrieben, nichts gebaut: `docs/FINANCE_PLAN_MAPPING_ISSUE_TOPIC_2026-11-18.md`. Die besprochene
  Kostenkette ist der produktive Stand; neu waeren die Spalte „Issue Topic“ in Sales_All und eine Mapping-Tabelle fuer
  Artikelnummern. Zugesagt an Andreas: zwei Excel. **Offen:** Entscheide E1 bis E11 (Andreas/Ingo), darunter die schriftliche
  Bestaetigung zu „Entscheid 1“ und das Go fuer die Schluesselregel „Ware aus Indien“.

## 2026-11-18 12:14: Test Fabio in T76, T161V „ATP Best. MRP“ NB/1100 entfernt

- Auftrag Ingo: Fabios Test ausfuehren und alten Stand sichern. In T76/100 `T161V` F/NB/1100 `ATPCONFMRP` X -> leer, **12:06:00**,
  SM30 `VV_161V_VF`, lokaler Customizing-Auftrag **T76K912926** (nicht freigeben). Ganze Tabelle vorher/nachher gelesen, nur dieses
  Feld anders (von der Hauptsitzung gegengelesen). Sicherung und Rueckbau: `saptasks/ic_preisfindung/NOTES.md` 10 und
  `PLAN_T161V_ATPCONFMRP_2026-11-18.md`. P76 und Mandant 090 nicht beruehrt.
- **Offen:** Fabio Bescheid geben (Ingo); Rueckbau auf X auf Zuruf Ingo.
- Nebenbefund: T76K912884 (IC V1) laut E070 seit 12.11. 13:34 freigegeben; NOTES 9 nachgefuehrt.

## 2026-11-18 11:46: Finance vor dem Termin mit Andreas nachgeprueft (Auftrag Ingo, Pruefung mit Fable)

- **Word fuer den Termin:** `Downloads\Offene_Punkte_2026-11-18.docx` (nur Finance, Ampeln je Land fuer Lieferant und Herstellkosten,
  heute Erledigtes, Beispielzeilen). Nicht im Repo.
- **Nachpruefung aller Aussagen gegen Sales_All 17.11./18.11.:** Ampelzahlen stimmten. Berichtigt (Word und Doku): „Rest ohne
  Lieferant laeuft ueber den Werkstamm“ (FR 391 von 409 rechnen lokal), 2,5 Mio. CHF (nach dem Deploy noch 0,85 Mio.: ES 0,55,
  US 0,30), 863/522 (richtig FR 1'199 von 1'290, US 1'042 von 1'053, IT 445 von 514), 44 US-Zeilen (richtig 7 von 70), SCHENKER
  (Artikelname, kein Spediteur), Typencode UK (58 statt 30 %). `docs/FINANCE_SUPPLIER.md` 12a, LEARNINGS.
- **Ursachen gemessen (nur gelesen):** UK 1'586 Zeilen: keine Schweizer Sachnummer, 295 UK-eigene Artikel, Top 50 = 74 %.
  IN 143 Zeilen: 5 Artikel ohne Trafag-Sachnummer. Ware aus Indien (bisher „Entscheid 2“): bei 473 von 539 Zeilen passt nur der
  Kostenschluessel nicht (IT 401 Zeilen „I“ + Nummer, UK/US 72 Zeilen indische Artikelnummer), im Programm loesbar, nicht gebaut. `docs/FINANCE_STANDARDKOSTEN.md` 13a.
- **Offen:** Entscheide Andreas 13:00; Programmkorrektur Indien-Ware UK/US (Go Ingo); Zuordnungsliste UK (Cornell), IT (Paola);
  Regelset-Word in den Downloads nennt bei Entscheid 1 noch den Stand vom 16.11. (8'703 Zeilen, 2,5 Mio.). *Mailentwurf an Andreas
  um 11:54 berichtigt (Exportzahlen, 0,85 Mio., Hinweis auf den alten Stand im Regelset), nicht gesendet; vorher nichts gesendet
  (Gesendete Elemente geprueft).*

## 2026-11-18 10:32: Handbestellwesen-Rueckmeldungen, Exportlauf nach dem Deploy geprueft

- **Exportlauf 10:06-10:13** (von Ingo ueber die Webseite gestartet), `Sales_All_2026-11-18.xlsx` gegen den Vortag: Lieferant gefuellt
  TRFR 120 -> 2'437 von 2'846 Zeilen, TRUS 6 -> 1'129 von 1'790, TRIT 19'696 -> 20'214, TRIN 960 -> 1'380; Zeilen und Umsatz in FR,
  US, DE, UK rappengenau gleich (ES, IT, IN, CH, AT nur neue Rechnungen). Zuordnung FR: Lokal 1'418 -> 391, Extern 48 -> 1'001.
  Herstellkosten kaum veraendert (FR +17'700 CHF). *Nachtrag 10:48 (Auftrag Ingo „beide Ampeln aktualisieren“):* FINANCE_SUPPLIER 11/12 und
  FINANCE_STANDARDKOSTEN 13 aus Sales_All 18.11. neu; Lieferant FR und US jetzt gruen, HK unveraendert in der Ampel, FR Marge 70,4 -> 69,4 %.
- **Handbestellwesen** (Mails Vinc 07:28/10:02, Alex 07:45, Sandro 08:22): Startseite ist die klassische Ansicht, die Neuerungen
  von 08:15 fehlten dort. Nachgezogen in `3c18493` (Suite 1634/1634, *„nicht deployed“ ueberholt: deployed 10:45 als `e316353`, Startseite im Browser geprueft*): Erfassen fuer alle mit Link
  „Kreditor fehlt?“, Freigabeliste mit Rollen, Kreditorliste alphabetisch statt nach Bytes. Befund: nur 2 von 109 mit Limit ueber
  CHF 1'000, 22 Kostenstellenleiter ohne Mitarbeitereintrag; Limits pflegen ist Sache Admin/Einkauf. KONZEPT 6.14, Wochen_Todo 120.
- **Rexx Hub:** Artjom hat 10:14 geantwortet (Basisadresse `https://trafag.rexx-systems.com`, Rechte #1411/#1412/#1417/#1418
  zugewiesen, DWH-Abfragen #775/#777 nur wenn „oeffentlich“). Menuepunkt fuer Ingo zunaechst nicht sichtbar.

## 2026-11-18 09:35: Deploy B1-Lieferant aus Wareneingang und Spanien (`8d15d48`)

- Produktiv seit 09:31: B1 (FR/US/IT/IN) fuellt leere Lieferanten aus dem letzten Wareneingang; Spanien `c9208b5` liest die
  Lieferantenspalten des neuen Sage-Exports (Santi muss noch einen Lauf machen). Zahlen aendern sich mit dem naechsten Exportlauf je Land.
- 09:35-09:43 Outlook-Entwuerfe angelegt (Ingo sendet): Rohail (mit SQL-Anhang; Rohail hat eine Abwesenheitsnotiz), Cornell, Paola (mit Artikelliste), Andreas (kurze Vorab-Info zum Termin heute 13:00 „Lieferanten / HK“, Anhang `Regelset_Lieferantenzuordnung_Group_HK_2026-11-18.docx` in den Downloads). Vorher geprueft: nichts davon war schon gesendet, keine Antworten seit 13.11.
- ZLO03 mit PCBA-Stuecklisten: Plan `zlo03/PLAN_ZLO03_PCBA_2026-11-18.md`, Entscheide E1-E9 bei Ingo.

## 2026-11-18 ~09:20: B1-Lieferant aus dem letzten Wareneingang (gebaut; *Uhrzeit zuerst als 10:30 geschaetzt, berichtigt*)

- Auftrag Ingo: fehlende Lieferanten dort ergaenzen, wo es geht. Gebaut fuer FR/US/IT/IN im Worktree `C:\TMP\TrafagSalesExporter_b1we1811`
  (`6e4d44f`, ab Produktivstand `f2c0267`), live gegen B1 geprueft, Suite 1624/1624. **Deploy und Neuladen 2025/26 entscheidet Ingo.**
  Messung und Folgerung (Wareneingaenge bestaetigen die Werkstamm-Regel) in `docs/FINANCE_SUPPLIER.md` 11.
- `Regelset_Lieferantenzuordnung_mit_Beispielen.docx` im Repo-Ordner (nicht versioniert, wie Ingos Original): Excel-Beispiele je Regel und
  Fragen/Antworten zu Andreas' Feststellungen.

## 2026-11-18 08:25: Deploy Handbestellwesen-Stammdaten und Lieferanten-Klasse A/B/C (`f2c0267`)

- Handbestellwesen: Besteller legen Kreditoren, Kostenstellen, Sachkonten neu an; ueber CHF 1'000 stehen Kostenstellenleiter und
  Projektleiter in der Freigabeliste (freigeben nur mit Limit); Meldung Wareneingang. `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` 6.13.
  **Ingo/Admin:** Leiter je Kostenstelle und Projektleiter-Haken pflegen, sonst wirkt die Rollenliste nicht.
- Einkauf: Lieferanten-Klasse A/B/C auf `/einkauf/lieferanten`. `docs/EINKAUF_SITZUNG_2026-10-22.md` 6.7.
- Deploy sauber (Einzelheiten `docs/rag/DEPLOYMENT.md`); im Browser nicht geprueft, weil nur der Mac-Chrome verbunden war.
- ZLO03 „PCBA Stueckliste“ (Mail Claudia 18.11.): nur Kurzauskunft im Chat, nichts geaendert.

## 2026-11-18 08:10: Einkauf Lieferantenbewertung (Antwort Armin, Plan Paket 1) und Shopfloor-Stichtag 18.11.

- **Armin** (Mail 17./18.11.): A/B/C ueber den Dashboard-Zeitraum, Basis Bestellungen der Artikel mit Disponent 001-006; Themenfelder
  jederzeit bewertbar, Uebersicht zeigt die juengste; SAP-Score/Preis Anfang Dezember. `docs/EINKAUF_SITZUNG_2026-10-22.md` 6.5.
- **Plan Paket 1** liegt vor (`saptasks/einkauf_lieferantenbewertung/PLAN_PAKET1_2026-11-18.md`, 6.6), nichts gebaut; wartet auf
  Freigabe Ingo und Entscheide D1-D14.
- **Shopfloor:** Lauf 18.11. 04:01 ok; Stichtagswerte und Selektion fuer Fabios ZC22-Abgleich in `docs/SHOPFLOOR_2026-10-07.md` 26.7,
  Text fuer Fabio im Chat (Ingo sendet). Damit ist Punkt 1 der Uebergabe vom 17.11. bis auf Fabios Abgleich erledigt.
- **Handbestellwesen** (Mails Alex/Vinc/Sandro 17./18.11.): Stammdaten selbst erfassen ist nicht gebaut (nur Admin), Freigabe ueber
  1'000 CHF mit Kostenstellen- und Projektleiter ebenfalls nicht; Kurzauskunft im Chat, Entscheid Ingo offen.

## 2026-11-17 17:25: Uebergabe, Chat geschlossen (Finance Andreas, Handbestellwesen AD-Abgleich, Shopfloor Entscheide Fabio)

Alle Eintraege dieser Sitzung in `docs/AGENT_COORDINATION.md` sind abgeschlossen, keine Datei reserviert. Produktiv `32bf356`.
**Offen, in dieser Reihenfolge:**
1. **18.11. frueh Shopfloor:** `sap/status` pruefen (erster Stichtag mit ganzem ZC22-Layout) und Fabio die Cockpit-Werte mit Stichtag und
   Selektion fuer den ZC22-Abgleich `/ P00 TX` geben. Danach Einkauf-Delta nachholen, dann Paket B (`docs/SHOPFLOOR_2026-10-07.md` 26.5);
   die Offen-Regel erst nach Fabios Antwort zu den 160 Widerspruechen umstellen.
2. **Outlook-Entwuerfe, Ingo sendet:** an Fabio „Shopfloor und Liefertreue: was ich noch von dir brauche“ (17:08, mit AE TX, Termin-Mapping,
   Kalender CH, 160 Widerspruechen, offenen Punkten); „AW: IC V1 ...“ an Fabio (IC-Preisfindung; laut Ingo erledigt, dann loeschen und
   Wochen_Todo 153 nachfuehren).
3. **Finance Andreas:** Antwortvorschlag im Chat, Entwuerfe Rohail/Paola/Cornell zuerst senden, Termin mit Andreas; `FINANCE_SUPPLIER.md` 10.
4. **Handbestellwesen:** Altbestand-Eintrag, dessen AD-Konto heute einer anderen Person gehoert, entscheidet Ingo
   (`docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` 6.12); Nachtlauf stoppt am Limit 10, bis „Jetzt ausfuehren“ von Hand.
5. **Neuterminierung Auftrag 418985** (Mail Fabio 16.11. 17:15 an Ingo und Lucas): laut Ingo von Lucas erledigt.
6. **Aufraeumen (Entscheid Ingo):** Worktree `C:/TMP/TrafagSalesExporter_hb1711` (gemergt) kann weg; alte Muster-Datei
   `Sales_All_Lieferantenzuordnung_HK_Muster_2026-11-16.xlsx` und das erste Video in den Downloads koennen weg.

## 2026-11-17 17:10: Shopfloor Paket A abgeschlossen (P76 gelesen)

SAP-Agent hat in P76 nur gelesen (16:47-16:57, Freigabe Ingo): Fabios Beispiel 58613 / 419371/10 Feld fuer Feld bestaetigt, Mapping-
Vorschlag in `docs/SHOPFLOOR_2026-10-07.md` 26.6; Fabrikkalender CH von Werk 1100 in 26.4 (P76 massgebend, T76 weicht im Dezember 2026
ab). Nebenwirkung: SE16 hat in P76 Selektionsreports `/1BCDWB/DB*` generiert (Standard, keine Daten; LEARNINGS). Beides steht im
Outlook-Entwurf an Fabio (gespeichert 17:08). Offen: Fabio/Ingo bestaetigen Kalender CH und das Termin-Mapping; 18.11. frueh ZC22-Werte.

## 2026-11-17 16:50: Shopfloor, Entscheide Fabio 17.11. aufgenommen (Paket A)

Fabio hat am 17.11. ~16:30 mit `Shopfloor_Entscheide_2026-11-17.zip` auf Ingos Mail von 15:17 geantwortet. Auftrag Ingo „alles
machen“, Plan freigegeben: Paket A jetzt, Paket B (Bau, Deploy) erst nach dem ZC22-Abgleich 18.11. frueh, Paket C wartet auf den
MES-User. Einzelheiten `docs/SHOPFLOOR_2026-10-07.md` 26, Wochen_Todo 156.
- AE TX 12.11.: Board = SAP (1'151'205.88), nichts zu korrigieren.
- Rueckstandslisten: Fabios Excel hat 68 Eintraege mehr als das Board; 160 Eintraege mit Abschlussdatum, aber offenem Status. Mit der
  Statusregel stiege PPD von 5'161 auf 8'668 Stk.; Rueckfrage an Fabio steht im Outlook-Entwurf.
- Kalender: Fabios Blatt „Kalender“ hat keine Feiertage; SAP-Fabrikkalender Werk 1100 und VF-/WE-Mapping 58613 werden in SAP gelesen
  (P76 nur lesend, Freigabe Ingo 17.11.).
- Entscheid Ingo: Beschriftung „Bestaetigter Liefertermin (VF)“ zurueck auf „Wunschtermin“ (mit Paket B).
- **Outlook-Entwurf an Fabio** („Shopfloor und Liefertreue: was ich noch von dir brauche“) liegt bereit, Ingo sendet.
- **18.11. frueh (wer immer die Sitzung hat):** Shopfloor `sap/status` pruefen und Fabio die Cockpit-Werte mit Stichtag und Selektion
  fuer den ZC22-Abgleich `/ P00 TX` geben. Danach Einkauf-Delta nachholen, dann Paket B.

## 2026-11-17 15:29: Uebergabe, Chat geschlossen (Shopfloor Ergaenzung, Lieferantenbewertung, Rexx Hub, Agenten-Monitor)

Alle Eintraege dieser Sitzung in `docs/AGENT_COORDINATION.md` sind abgeschlossen, keine Reservierung offen. Produktiv ist `32bf356`
(Spanien nur ab `ab04aa5` oder juenger). **Offen, in dieser Reihenfolge:**
1. **Einkauf-Delta nachholen:** beim Deploy 14:36 auf Anweisung Ingo abgebrochen (`docs/rag/DEPLOYMENT.md`).
2. **18.11. morgens Shopfloor `sap/status` pruefen:** erster Stichtag mit ganzem ZC22-Layout (Status- und `M*`-Filter), Werte geschrieben.
3. **Beschriftung „Bestaetigter Liefertermin (VF)“:** vermutlich falsch (VF = Verfuegbarkeitstermin laut MD04); zuruecksetzen oder auf
   Anh Nhis Antwort warten (Entscheid Ingo). `docs/SHOPFLOOR_2026-10-07.md` 25.
4. **Warten auf Antworten:** Fabio (AE 12.11., Feiertage, Status- oder Abschlussregel, Schichten TX, MES-DB-User), Anh Nhi (Bildschirmfoto
   VF-Termin, „Datum WE geplant“), Dispo (Liste der 139 Eintraege), Marco Di Menco (MES-DB-User), Armin (E1, E2, Preis 0 %),
   Artjom (Basisadresse, Berechtigungen, DWH #775/#777; danach API-Benutzer anlegen, Schalter `HrKpi:RexxHub:Enabled`).
5. **Aufraeumen (Entscheid Ingo):** `DokumentationrexxHub_v17.docx` liegt unversioniert in der Repo-Wurzel (Herstellerdokument, nicht
   ins Repo); Worktree `C:/TMP/TrafagSalesExporter_sf1711b` (Zweig `shopfloor-ergaenzung-1711`, gemergt) kann weg.

**Agenten-Monitor (eigenes Repo `C:/Users/koi/source/repos/AgentMonitor`, kein Teil dieses Projekts):** Prozentbalken je Agent mit Zahl
im Balken und Animation, Lauf-Streifen je Aktion; der App-Ordner war wegen der `.gitignore`-Regel `*.app` nie versioniert, jetzt in `108b4ac`.
Installiert unter `%LOCALAPPDATA%/AgentMonitor/app`. Weiterbau (selbstgemeldete Phasen mit Prozent) liegt bei der Sitzung 9a, falls Ingo es will.

## 2026-11-17 15:17: Antwort an Fabio gesendet

Antwort an Fabio 17.11. gesendet (laut Ingo; Text im Chat von Claude entworfen), mit der Dispo-Liste im Anhang: Stand je Punkt; gefragt AE 12.11., Feiertage, Regel offen nach Status oder Abschlussdatum, Entscheid der Dispo je Zeile, Bildschirmfoto Anh Nhi zum VF-Termin und Bedeutung „Datum WE geplant“, Schichtmodell/Nachtschicht/Rundung TX, Termin MES-DB-User; angeboten Vergleich mit ZC22 am 18.11. `docs/SHOPFLOOR_2026-10-07.md` 25, Wochen_Todo 156.

## 2026-11-17 15:11: Rexx Hub Admin-Zugang, Rueckfrage an Artjom

Artjom (rexx) hat Ingo um 13:46 den HR-Admin-Zugang gegeben; keine Testumgebung. Die Anleitung `DokumentationrexxHub_v17` sagt: API-Benutzer
und Token legt der Admin selbst an, `/v1/datawarehouse/{id}` ist vorhanden, 50'000 Anfragen je Monat. Ingo hat Artjom nach Basisadresse,
Berechtigungen #1411/#1412/#1417/#1418 und der Freigabe der DWH-Abfragen #775/#777 gefragt (gesendet). Nichts an Rexx aufgerufen, Schalter
weiter aus. `saptasks/rexx_schnittstelle/NOTES.md` 15, Wochen_Todo 133/167, rag/HR_KPI.

## 2026-11-17 14:45: Shopfloor Ergaenzung Fabio 17.11. deployed; Lieferantenbewertung Mail an Armin

**Shopfloor (Auftrag Ingo „mach mal alles“):** Deploy 14:36 `32bf356` (Zweig ab `2e62163`, gemergt `ab04aa5`; Spanien nur ab `ab04aa5`).
Schnittstellen-Seite mit echtem Status je Feld (44 geliefert, 56 definiert, 3 in Ueberpruefung, 2 offen), Bestellnummer Einkauf
Handeingabe, TX-Mengen aus dem MES (Michaels xlsm ruft `TMS_MES.dbo.getProdQtyPerDayOfMonthPerDisponent`), Rueckstandslisten-Feld
„Bestaetigter Liefertermin (VF)“. Das laufende Einkauf-Delta wurde auf Ingos Anweisung vom Neustart abgebrochen, **Nachholen faellig**.
Rueckstandslisten nur gelesen: alle 596 Eintraege noch Excel-Import, 123 offene mit Abschlussdatum; Liste fuer die Dispo in
`docs/shopfloor_paket_2026-11-17/auswertung_ingo/` und in Ingos Downloads. VF- und WE-Termin in T76 lesend geprueft
(Ergebnis 14:52 in `saptasks/shopfloor_sap/RUECKSTAND_TERMINE_T76_2026-11-17.md`: VF-Termin = Verfuegbarkeitstermin laut MD04, die neue Beschriftung „Bestaetigter Liefertermin (VF)“ passt vermutlich nicht, Rueckfrage an Anh Nhi). Offen bei Fabio: Antwort auf die neue Mail (Entwurf
im Chat), Schichtregeln TX, Dispo-Entscheide, MES-DB-User, FPY-TX-Abgrenzung. **Naechster Schritt:** 18.11. morgens `sap/status`
pruefen (erster Stichtag mit ganzem Layout). `docs/SHOPFLOOR_2026-10-07.md` 25, Wochen_Todo 156.

**Lieferantenbewertung:** Ingo hat Armin am 17.11. geantwortet (nichts gebaut, Vorarbeit erledigt; Fragen E1, E2, Preis 0 %).
`docs/EINKAUF_SITZUNG_2026-10-22.md` 6.4, Wochen_Todo 122.

## 2026-11-17 nachmittags: Shopfloor, Uebergabe (Sitzung geschlossen)

Produktiv ist `2e62163` mit allen Filtern aus Fabios Paket 13.11. (ZC22 Status und `M*`, ZC27 Material, TR5/DW numerisch, Verzug ab
01.01.2026) und CO-PA; um 12:50 nach dem Ruecksetzer geprueft, nichts nachzuladen. **Phase C wartet auf Fabio** (Antworten aus seiner
Mail 2 vom 13.11., Frist 20.11.): 40 Listenfelder (Anh Nhi, Einkauf), MES-DB-User von Marco fuer FPY TR5/TX, 20 offene Quellen
(Michael, Sanel, Sacha), Handfelder bei den Teamleitern; ob die Mail gesendet ist, ist nicht dokumentiert. Ausserdem bei Fabio:
AE-Differenz 12.11., `/PPA DW/S ALLE`, Feiertage, plan5. **Naechster Schritt fuer wer weitermacht:** 18.11. morgens `sap/status`
pruefen (erster Stichtag mit `M*`, `layoutfilter.arbeitsplatz_ausschluss`, Werte geschrieben). Liste in `docs/SHOPFLOOR_2026-10-07.md`
24 (Nachtrag Phase C), Wochen_Todo 156.

## 2026-11-17 12:50: Handbestellwesen, AD-Abgleich Teil 2; gemeinsamer Stand nach Ruecksetzer deployed

Antworten Ingo auf die Frageliste umgesetzt (`44a3ed1`, Zweig ab `6a7de50`): Vermerk des Abgleichs am Eintrag, wieder aktiv nur mit
Vermerk, abgelaufen wie gesperrt, Altbestand mit aktivem AD-Konto wird AD-Eintrag (Kurzzeichen bleibt, auch bei „Aus AD
aktualisieren“), Reiter „Deaktivierte“ fuer Admins mit Hinweis und Neuzuweisung, `Schreiben` an, AD nur gelesen; 1610/1610.
**Deploy 12:36 `44a3ed1` nahm den Shopfloor-Deploy von 12:25 (`3298c93`) zurueck**, weil mein Zweig ihn nicht enthielt; bemerkt
12:40, gemeinsamer Stand `2e62163` deployed 12:43 (gemergt `fd49894`), Shopfloor-Sitzung informiert (LEARNINGS). Vorschau 12:40:
15 inaktiv, 94 werden AD-Eintraege; bei einem Eintrag gehoert das Konto im AD einer anderen Person. **Nicht ausgefuehrt, Entscheid
Ingo.** Konzept 6.12, Wochen_Todo 165. **Produktiv `2e62163`; Spanien nur ab `fd49894`.**

## 2026-11-17 12:40: Shopfloor, CO-PA schreiben und Arbeitsplatzfilter M* ein, deployed

Fabio per Teams: die Zahlen vom Freitag seien nicht drin. Entscheid Ingo „8. ein, 3, 4 so dass es passt schalten“. Vorschau
produktiv mit `zc22arbpl=M*`: TX-Vorrat −457 (trifft Fabios „rund 450 bis 500 zu hoch“), DW −454, TR5 −246 Stk., Laufzeit 24 s.
Zweig `shopfloor-copa-arbpl-1711` ab `6a7de50` (ohne Spanien): `SapCoPaEnabled = true`, `Zc22ArbplAusschluss = M*`, Schaltertest
(`3298c93`, gemergt `684ae25`), 1607/1607. **Deployed 12:25**, bitgleich, ohne Alarm. Startlauf 12:26 mit `M*` und CO-PA geschrieben;
auf Ja von Ingo CO-PA 02. bis 16.11. nachgeladen, gleich Fabios Kontrollwerten ausser AE Stand 12.11. (+3'906.68, offen). Stichtag
17.11. bleibt ohne `M*` (eingefroren 04:01, der Lauf 04:00 fand statt), erster mit ganzem Layout 18.11. Handbuch Kapitel 10 nachgefuehrt
(live mit dem naechsten Deploy). `docs/SHOPFLOOR_2026-10-07.md` 24, rag DEPLOYMENT/PROJECT/ARCHITECTURE/LEARNINGS, Wochen_Todo
36/130/156, Issue-Log ISS-017, `saptasks/copa_verkauf/NOTES.md` 3. **Produktiv `3298c93`; Spanien nur aus dem Arbeitszweig ab `684ae25`.**

## 2026-11-17 11:45: Handbestellwesen, naechtlicher AD-Abgleich, deployed (Schreiben aus)

Auftrag Ingo: naechtlicher Abgleich, gesperrte oder nicht im AD vorhandene Benutzer automatisch inaktiv, Rollen bleiben; „mache
was soweit klar ist, Frageliste was unklar ist“. Gebaut im Zweig `hb-adabgleich-1711` ab `7cc1a62` (`6a7de50`, gemergt `0197661`):
AD komplett lesen (auch gesperrte), Regeln, Nachtlauf ab 02:30 mit Nachholen, Grenze 10, Mindestzahl 50 AD-Konten, Admin-Bereich
`/handbestellwesen/adabgleich` (Vorschau, Ausfuehren nach Rueckfrage, letzte Laeufe), 12 Tests. **Deployed 11:28**, `Schreiben`
aus: der Nachtlauf aendert nichts. Erste Vorschau im Chrome: AD vom Server lesbar (1'539 Konten), 15 von 109 aktiven wuerden
inaktiv (alle Altbestand, im AD gesperrt). Geloescht wird nichts (Frage 1). Acht Fragen in
`docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` 6.11; Wochen_Todo 120/165, rag DEPLOYMENT/PROJECT/ARCHITECTURE, baum, LEARNINGS.
**Produktiv `6a7de50`; Spanien nur aus dem Arbeitszweig ab `0197661`.** *(Ueberholt 12:25: produktiv `3298c93`, ab `684ae25`.)*

## 2026-11-17: Wochen_Todo nach der Sitzung mit Oscar

Auftrag Ingo „notieren mit Datum fuer heute“. Neu in `projektmanagement/Wochen_Todo.tsv`: **164** Phishing-Training, die
Klick-Auswertung der Trainingsfirma (.xls je Auswertung, Name und Vorname) um Vorgesetzte und Hierarchie ergaenzen; offen ist die
Quelle (AD, Microsoft Entra bzw. Teams oder Rexx). **165** Handbestellwesen, Benutzerverwaltung ueber Microsoft Entra (Details folgen).
**166** Alte Z-Objekte loeschen, die niemand mehr braucht: Vorgehen finden. **167** Rexx Hub: bei Rexx nach dem Freigabelink fragen.
Hinweise in 120 (laut Ingo vom Einkauf, Sandro, abgenommen), 133 und 142. `Wochen_Todo.xlsx` und `Status_kurz.xlsx` neu erzeugt. Nur
Doku, kein Code, kein SAP, kein Deploy.

*Nachtrag nachmittags, Entscheide Ingo:* **164** Die Vorgesetzten kommen aus dem Rexx Hub, nicht aus dem AD, weil HR Aenderungen im AD
selten nachtraegt; das Feld steht jetzt als Nachtrag in der Feldliste fuer den Hub (`saptasks/rexx_schnittstelle/NOTES.md` 12), Status
wartet auf die Hub-Daten. **165** Die Benutzerdaten des Handbestellwesens kommen aus dem AD; Entra ist nur lesend und selbst aus dem AD
gespeist. **167** Vorgesetztenfeld zusammen mit dem Freigabelink bei Rexx anfragen.

*Nachtrag spaeter, Rexx Hub:* **Bestellt**, Nadja hat die Offerte AG37140 am 11.11. unterschrieben (Mailverlauf von Ingo). Ingo hat
Artjom um Zugang, Admin-Zugriff, Schnittstellenbeschreibung und Testumgebung gebeten (ohne Vorgesetztenfeld, das kommt mit der
Feldliste). Ueberholte Aussage „Hub nicht bestellt“ in `docs/rag/HR_KPI.md`, `docs/HR_KPI.md` 8.17 und Rexx-NOTES 12 markiert;
Wochen_Todo 133, 164, 167.

## 2026-11-16 22:20: Shopfloor, Status- und Materialfilter eingeschaltet, deployed

Fabio fragte per Teams, ob am Shopfloor weitergearbeitet wird. Ingo hat `T76K912906` nach P76 importiert (Meldung ~16:10, Caches nicht
geleert). **Abnahme in P76, nur gelesen:** Vorschau = ZC22 im selben Moment fuer Verzug und Vorrat aller Abteilungen (90 Wertepaare,
`saptasks/shopfloor_sap/P76_ABNAHME_2026-11-16.md`), Logistik mit Materialfilter 18 von 18 Kontrollwerten Fabios. **Deployed 17:06
`7cc1a62`** (Zweig ab `2da2682`, gemergt `69ed8a0`, 1595/1595, bitgleich, ohne Alarm; Wochensicherung 15 Minuten): `Zc22StatusAusschluss
= FREI DRUC RÜCK VOKL*`, `RmMaterialAusschluss = E*,D*,F*,C*`, `SapCoPaHzdat = true`. Startlauf 17:07 mit Filter sauber; 22:07 Logistik
02. bis 12.11. nachgeladen (18 Kontrollwerte gespeichert, 20 Werte bewegt, sonst nichts; Samstag 07.11. erst 22:26 nach der Gegenpruefung); im Chrome abgelesen. CO-PA-Vorschau mit HZDAT
trifft Fabios Werte an drei von vier Stichtagen exakt (12.11.: Auftragseingang +3'906.68). **Offen:** 17.11. morgens ersten
Stichtagslauf mit Filter pruefen (die Werte vom 16.11. bleiben eingefroren, ohne Statusfilter); Arbeitsplatzfilter `M*` aus (DW 540 Stk.
ueber der Liste der Teamleiter, Frage an Fabio); `SapCoPaEnabled` aus; Historie vor dem 02.11. nicht nachgeladen. Die Pruefung nach dem
Deploy lief erst ab 22:06, fuenf Stunden nach dem Deploy-Ende (Ursache nicht untersucht). `docs/SHOPFLOOR_2026-10-07.md` 23.

## 2026-11-16 09:40: Finance, Sales_All mit Blatt „Lieferantenzuordnung“, deployed

Andreas pivotiert nach den rohen Lieferantenfeldern, die Schweiz ist dort leer (wirksam 100 % Intern / TR AG). **Deployed 09:33
`2da2682`** (Zweig ab `e10ab3a`, gemergt `70507fc`, 1595/1595, bitgleich): neues Blatt `Lieferantenzuordnung` im zentralen Sales_All
(je Gesellschaft Supplier Type, Delivering Entity, Cost Source mit Zeilenzahl, Anteil und Net Sales Actual gesamt und je Monat).
„Group HK“ = Konzern-Herstellkosten; nichts Neues, Regeln seit 27.08./09.09. entschieden. *Nachtrag 16:10: Nachweis am Export 12:11 erbracht (Blatt gegen Blatt Sales ohne Abweichung). Excel
`Group_HK_Regeln_Beispiele_2026-11-16.xlsx` und der aktualisierte Mailentwurf an Andreas (448 von 453 statt 414 von 420) liegen in
Ingos Downloads, nicht gesendet.* **Offen:** Abstimmung mit Andreas, Entscheid „ohne Lieferant“ (Werkstamm oder lokal). `docs/FINANCE_SUPPLIER.md` 9.

## 2026-11-16 08:50: Export-Seite, „Excel oeffnen“ laedt herunter, deployed

Meldung Ingo: Beim Oeffnen von Sales_All auf der Export-Seite kam „Access is denied“. Der Knopf startete die Datei per
`Process.Start` auf dem Webserver. **Deployed 08:45 `e10ab3a`** (Zweig ab `9100537`, gemergt `bbd4ab0`, 1593/1593, bitgleich): Download
in den Browser ueber die Blazor-Verbindung, gilt fuer Standortdateien und Sales_All. Datei vom 13.11. vorher in Ingos Downloads kopiert.
Test `ExportFileDownloadTests` verbietet `Process.Start(` in Seiten. **Offen:** Ingo prueft den Knopf im Browser.

## 2026-11-13 12:40: Shopfloor, Paket Fabio 13.11. (Excel der Teamleiter abloesen), Phase A deployed

Fabio per Teams mit `pakete.zip` (106 Felder gegen P76 und Excel abgeglichen). Entscheide Ingo: Phase A und B, P76 nur lesend, Verzug
ab 01.01.2026, Paket ins Repo (`docs/shopfloor_paket_2026-11-13/`). **Deployed 12:22 `d754a76`** (Zweig ab `df48301`, gemergt
`7a8b97d`, 1569/1569, bitgleich, ohne Alarm): Vorrat rollend (DW in Wochen), `Shopfloor:MaterialFilter = TR5,DW` fuer Vorrat und
Verzug, DW `D*` ohne Ausnahmen, plan5 kumuliert, `Shopfloor:VerzugAb = 2026-01-01`, Verkauf/CO-PA Monat des letzten Arbeitstags,
CO-PA-HZDAT-Filter vorbereitet (`SapCoPaHzdat` aus), Patch 08 und Module CZ/MTF/Sensoren entfernt, Tagesumsatz am Monatswechsel,
Handbuch Kapitel 10. Werte 13.11. bleiben eingefroren, neue Regeln ab dem naechsten Arbeitstag 04:00.
**P76-Befund (nur gelesen):** Fabios Werte = ZC22 mit Standardlayout `/ P00 TX` (Status `FREI DRUC RÜCK VOKL*` und naechster
Arbeitsplatz M* ausgeblendet) und ZC27 mit Standardlayout `/NIN` (Material E*, D*, F*, C* ausgeblendet); Fabios Regel „letzter Vorgang
des ganzen Auftrags“ waere falsch. Entscheide Ingo: beide Filter nachbilden (M*-Teil Schalter aus). **T76 fertig in `T76K912906`**
(offen): CoPaSumSet Hzdat, ShopRmSet Matex, ShopAufSet Sttxt/Arbpl/Vornr/Avo; gegen CE11000, ZC27 und ZC22 je ohne Abweichung.
**Cockpit deployed 14:33 `9100537`** (Filter, Schalter aus). **Offen:** Freigabe/Import `T76K912906` und P76-Caches (Ingo), dann
Vorschau gegen ZC22/ZC27/CO-PA-Kontrollwerte, Schalter setzen und deployen, Logistik-Historie nachladen; Fabio: Vorgangsdaten (M*),
DW 441/555, Arbeitstag 01./02.01., Sparte 0006. Fragen an Fabio: DW-Verzug 441 gegen seinen Abzug 555, Arbeitstag-Annahme
(01./02.01.), Transmitter 0005 oder 0005+0006. `docs/SHOPFLOOR_2026-10-07.md` 22, Wochen_Todo 156.

## 2026-11-13 10:50: Finance, Mail Andreas (Lieferantenzuordnung, Group HK), deployed

Auftrag Ingo „mach alles was du ohne mich machen kannst, du kannst messen“. **Deployed 10:31 `df48301`** (Zweig ab `052c5a3`,
Spanien `c9208b5` weiter draussen; gemergt `15105ed`): Sales_All zeigt die wirksame Lieferantenzuordnung (Blatt Sales Spalten
55 bis 57, Finance Details 43/44). Produktiv-DB nur lesend nachgemessen: Italien wieder 499 Zeilen / 1,07 Mio. EUR ohne
Lieferant (162 Artikel, `ISS-003.6`), UK TR09 ist eine Einzelzeile (`ISS-003.7`), TR DE seit 09/2026 751 Zeilen nur mit
`ALPHAPLAN-ID` (`ISS-016`), Fallback-Schalter steht bewusst auf CH-Werkstamm (Andreas' Variante B1 waehlbar, sein Entscheid offen). Vier Mailentwuerfe in
`C:\Users\koi\Downloads` (Andreas, Rohail, Cornell, Paola), **nicht gesendet**.
**Export 12:10 geprueft (12:12):** Spalten an 55 bis 57 bzw. 43/44, Rest unveraendert, keine Gesellschaft mit weniger Zeilen,
leer nur bei Finance-Ausschluss; Italien ohne Lieferant 503 von 510 Zeilen auf Trafag AG (Mailentwurf an Andreas damit ergaenzt).
**Offen:** (1) Ingo sendet die Mail an Andreas; (2) Entscheide Ingo: DE-Uebergangsregel ueber die eindeutige ID, Spanien-Deploy, Fallback-Schalter,
Lesezugriff B1 TR IT (`ISS-007.4`); (3) Termin Montag mit Andreas. Outlook liess sich im Browser-Tab nicht oeffnen, Andreas'
Anhang ist nicht gelesen. `docs/FINANCE_SUPPLIER.md` 9, Wochen_Todo 163.

## 2026-11-13 09:30: Einkauf, Wuensche Marco Widmer (Mail 12.11.), deployed

Plan im Chat, Entscheide Ingo (Produktgruppe auf Verwendung, Zusatzaufrisse bauen, Register nur ausblenden). **Deployed 09:11
`052c5a3`** (Zweig ab `0e94279`, Spanien `c9208b5` weiter draussen; gemergt `989d401`), im Chrome geprueft. Offene Bestellungen
und Kontrakte ausgeblendet; Spend-Aufriss: keine Kacheln, Kuchen je Einstieg, Anzahl Artikel nach XYZ statt Volumenbalken, Matrix
AX ... -/-, neue Einstiege Region/Disponent > Warengruppe und Produktgruppe nach Verwendung : Lieferant (ersetzt die Sichten nach
eigenem Disponenten, die nur HW_ZUKAUFSORT zeigten), LZ/Sortiment entfernt; Materialanteil mit Herstellkosten HK (Abdeckung 100 %).
SAP wurde nicht gebraucht. Offen: Marco informieren, Lesart HK bestaetigen. `docs/rag/PURCHASING.md`, Wochen_Todo 162.

## 2026-11-13 08:15: Shopfloor, Mail Fabio 13.11. (Stichtagswerte, Verzug ein), deployed

Plan im Chat, Entscheide Ingo 07:50. **Deployed 07:56 `0e94279`** (eigener Zweig ab `e956183`, Spanien `c9208b5` weiter nicht
deployed; gemergt `e7b70f6`). Vorrat total/3/2/1 Mte, offene KW, SEH-Tagesziel, TX `plan5` und PPD-Verzug gelten aus dem ersten
erfolgreichen SAP-Lauf des Tages, spaetere Laeufe und Neustarts lassen sie stehen. Morgenwerte 10.-12.11. per
`POST sap/morgenwerte` aus dem Verlauf zurueck (66 Felder; 09.11. hat keinen Morgenwert). PPD-Verzug ohne Materialfilter
eingeschaltet (13.11.: SEH 89, TR5 559, TX 567, DW 9'575). Cache-Busting `index.html` (`?v=` SHA256). SAP-Badge war schon da.
Offen: CO-PA (Caches P76, Freigabe Ingo), Fabio prueft DW-Verzug und SEH 12.11. (212 %), 04:00 lief am 13.11. nicht (vermutlich ISS-017, nicht untersucht); Entscheid Ingo zum 09.11. (SEH 2'335 %, Abschnitt 21).
Antwort an Fabio noch nicht gesendet. `docs/SHOPFLOOR_2026-10-07.md` 21, Wochen_Todo 36/130/156.

## Uebergabe 2026-11-12 14:25: ZLO03 FIX 28 (Chat geschlossen)

ZLO03 FIX 28 ist **in P76 wirksam**: `T76K912902` importiert 13:54, `ZloTopDownSet` liefert um 14:15 fuer 5501 D13006 = 1 und fuer
63759 H20201 115 MM / 0.06 CHF statt 1'815 CHF. Das lokale Werkzeugproblem (MSB3822 beim Bau von `ProbeSapEntity`, Ursache
ein fremder unversionierter WinForms-Ordner im Projektordner) ist behoben 14:20, der Ordner liegt jetzt unter
`C:\Users\koi\source\repos\`. Offen in dieser Reihenfolge (`zlo03/FIX28_SELBSTSTUECKLISTE_2026-11-12.md` 9, Wochen_Todo 160):
(1) ~~Ingo sendet die Antwort an Armin~~ *(gesendet, laut Ingo 14:25)*;
(2) Einkauf-Lauf neu, sonst bleibt der Materialanteil im Dashboard zu hoch; (3) Selbst-Stuecklisten fachlich klaeren.

## Uebergabe 2026-11-12 13:50 (Chat der Sitzung trafagsalesexporter-32 geschlossen)

Diese Sitzung lief von 09:00 bis 13:50. Alles Offene steht hier in der Reihenfolge, in der es angegangen werden sollte; die
Einzelheiten stehen im Block „Stand 2026-11-12 12:00“ weiter unten und in den genannten Dateien. ZLO03 FIX 28 gehoert einer
anderen Sitzung (Abschnitt direkt darunter).

**Nicht gepusht:** `eb12111` (Code-Review-Bericht), `f021410` und alles danach. Der Bericht beschreibt, wie sich die Sperren
umgehen lassen; pushen erst, wenn Ingo bestaetigt, dass das GitHub-Repository privat ist (`gh` ist auf dem Rechner nicht
installiert, nicht pruefbar). `origin` steht auf `a6a96b2`.

**Offen, wartet jeweils auf Ingo:**

1. **Admin-Passwort im Klartext** in `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` Zeile 13 (versioniert, gepusht). Gilt es
   noch? Dann auf Admin > Passwoerter aendern, danach die Zeile entfernen (Wochen_Todo 102). Dringender durch Code-Review
   Befund 1 und 2 (Hashes in `appsettings.json`, Cookie daraus berechenbar).
2. **Code-Review:** 13 Befunde, 4 hoch, nichts geaendert. Bericht mit Behebungsvorschlag in Abschnitt F:
   `docs/CODE_REVIEW_2026-11-12.md`, Wochen_Todo 159. Naechster Schritt: Ingo waehlt aus, dann Plan je Befund, dann umsetzen.
3. **Spanien Supplier (ISS-003.3, Wochen_Todo 13):** Code `c9208b5` nicht deployed. *(Nachtrag 13.11.: Produktiv ist seit 10:31 `df48301` (Shopfloor, Einkauf Marco, Finance Sales_All) ohne `c9208b5`. Spanien nie allein aus `c9208b5` deployen, sonst faellt der Shopfloor-Fix zurueck; Deploy aus dem Arbeitszweig ab `15105ed`, dort Suite vorher neu laufen lassen.)* *Stand 13.11. abends: produktiv ist `9100537` (zusaetzlich Shopfloor Paket Fabio 13.11.: `appsettings.json`, `shopfloor_config.json`, `app.js`); Spanien nur aus dem Arbeitszweig ab `a207923` oder juenger deployen, nie aus `c9208b5` allein oder ab `15105ed`.* *Stand 16.11. 09:33: produktiv ist `2da2682` (zusaetzlich Export-Download `e10ab3a` und Sales_All Blatt Lieferantenzuordnung); Spanien nur aus dem Arbeitszweig ab `70507fc` oder juenger deployen, sonst fallen beide Deploys vom 16.11. zurueck. Die Angabe „ab `a207923`“ ist damit ueberholt.* *Stand 16.11. 17:06: produktiv ist `7cc1a62` (zusaetzlich Shopfloor Status- und Materialfilter in `appsettings.json`); Spanien nur aus dem Arbeitszweig ab `69ed8a0` oder juenger deployen, sonst faellt auch dieser Deploy zurueck. Die Angabe „ab `70507fc`“ ist damit ueberholt.* Vor dem Deploy: Code-Review Befunde 5 und 9
   beurteilen. Mail Gang 1 an Santi liegt als Entwurf in `C:\Users\koi\Downloads\Spain_Supplier_Check_Gang1_Santi_2026-11-12.eml`,
   nicht gesendet; vorher die Frage ergaenzen, welches Skript sein taeglicher Task aufruft (35 oder 7 Tage, F30). Vor Gang 2
   Lesepruefung der Dashboard-Einstellungen und der SharePoint-Kopfzeilen mit Ingos Anmeldung. Plan
   `docs/PLAN_ES_SUPPLIER_WARENEINGANG_2026-11-12.md`. **Achtung Release-Worktree:**
   `C:\TMP\TrafagSalesExporter_release_eafd1c5` steht auf `c9208b5` *(seit 13.11. 10:30 auf `df48301`, seit 14:30 auf `9100537`)*; vor jedem Deploy auf den gewuenschten Commit ziehen.
4. **CO-PA `T76K912866`:** laut Ingo in P76 importiert. Offen: Gateway-Caches in P76 (`/IWBEP/CACHE_CLEANUP` alle Modelle, dann
   `/IWFND/CACHE_CLEANUP` `ZPOWERBI_EINKAUF_MDL`), Vorschau `shopfloor/api/sap/copa?tag=2026-10-01` gegen September
   (6'531'197.94 / 7'878'114.69), dann `Shopfloor:SapCoPaEnabled = true` und Deploy, Historie ab 01.01.2026 nachladen.
   `saptasks/copa_verkauf/NOTES.md` 3, Wochen_Todo 130. Wer die Caches leert, ist nicht entschieden.
5. **ZC12 (Wochen_Todo 3):** Plan und Einspieldatei liegen vor, wartet auf Ingos Ja zur Ausfuehrung in T76
   (`saptasks/zc12/PLAN_ZC12_NULLWERT_PERFORMANCE_2026-11-12.md` Abschnitt 6).
6. **IC V1 (Wochen_Todo 153):** Freigabe `T76K912884` und Import warten auf Fabios Z999-Preise in P76; Antwort an Fabio zu
   601/908 (RV80HGEN nach Bestaetigung von 908) nicht belegt gesendet. `saptasks/ic_preisfindung/NOTES.md` 9.1.
7. **Lieferantenbewertung (Wochen_Todo 122):** naechster Schritt Plan Paket 1; offen P76 lesen, SLFDT oder EINDT, Zeitraum
   A/B/C, Preis 0 % in Fiori. `docs/EINKAUF_SITZUNG_2026-10-22.md` 6 bis 6.3.
8. **Doku-Pruefung:** abgeschlossen (`9e27a70`); 39 Entscheide und 22 Strukturvorschlaege in
   `docs/MD_KONSISTENZPRUEFUNG_2026-11-12.md` Abschnitt 4 und 5 (Wochen_Todo 161). Das Handbuch unter `wwwroot` ist nur gemeldet.
9. **WLAN `trafag-802.1x-tel`:** Sache der IT; Nachricht an Alex und Handytest offen, `docs/router/plattform.md`.
10. **Mails, nicht belegt gesendet:** Andreas (Rueckfrage Wareneingang), Fabio (601/908), Marco Widmer (Auslegeordnung),
    Philip Steiger (Inventar Smartsheet), Alex (WLAN).

**Aufraeumen (nichts davon ist noetig fuer den Betrieb):** Worktree `C:\TMP\TrafagSalesExporter_review_9e27a70` (Stand
`a6a96b2`, nur fuer das Review) kann mit `git worktree remove` weg. Der lokale Webserver fuer den Entwurf V6
(`127.0.0.1:8765`) ist beendet; die Datei liegt unter `docs/einkauf_lieferantenbewertung/`. Fremde unkommittierte Aenderungen
im Arbeitsbaum (Finance_All, `CLAUDE.md` 3a, geloeschte Wurzeldateien, Office-Dateien) sind unveraendert, Ingo entscheidet.

## Nachtrag 2026-11-12 13:45: ZLO03 FIX 28 (Meldung Armin „ZLO03_Fehler in der Stücklistenmenge“)

D13006 stand in Armins Top-Down-Datei in jedem Geraet mit 2 statt 1. Ursache: Stuecklisten, die nur sich selbst enthalten
(„Rekursiv erlaubt“, seit Herbst 2024 zahlreich), erzeugen in `ZPOWERBI_VC_TXT` eine Zusatzzeile unter sich selbst, und ZLO03
summiert seit FIX 2 (10.03.2026, `T76K911787`) jede Zeile. **FIX 28** zaehlt diese Zeilen nicht mehr (Bedarf, Einheitenwahl,
Elternmaterial-Spalte); T76 aktiv **13:33** in neuem Auftrag **`T76K912902`**, vorher/nachher getestet (5501: 15 Komponenten
2 → 1; 63759: H20201 0.05 statt 1'815 CHF je Geraet, echte Mehrfachverwendungen bleiben summiert; Bottom-Up D13006: 58 Geraete
2 → 1). **Ingo:** `T76K912902` freigeben und importieren, danach Einkauf-Lauf neu und Armin antworten (Wochen_Todo 160; *berichtigt 14:05, hier stand 161*).
*Nachtrag 14:05:* Ingo hat `T76K912902` freigegeben und importiert; in P76 lesend belegt (`E070` 13:54:11, `REPOSRC`
`Z_ZLO03_TURBO2` 13:54:11). ~~Die Wirkung in P76 ist noch nicht gesehen~~ *14:15: Wirkung in P76 belegt (`ZloTopDownSet` 5501 D13006 = 1, 63759 H20201 0.06 CHF; FIX-28-Datei Abschnitt 7).* Offen: Einkauf-Lauf, Antwort Armin.
Einzelheiten `zlo03/FIX28_SELBSTSTUECKLISTE_2026-11-12.md`.

## Stand 2026-11-12 12:00 (eine Sitzung, 09:00 bis 12:00; neu gefasst bei der Doku-Pruefung)

Dieser Block ersetzt die Zwischenstaende, die im Lauf des Vormittags uebereinander gewachsen waren und sich widersprachen
(„Plan laeuft“ neben „umgesetzt“). Alle Uhrzeiten aus `git log`.

**Deploy 11:33, `e956183`:** Kleinigkeiten vom 10.11. (`c5c9b4a`, `19ca282`, `548b10e`) und Rexx Hub vorbereitet (Schalter
`HrKpi:RexxHub:Enabled` aus, `1e99745`, `d99fbe6`) sind produktiv; 1547/1547, bitgleich. `docs/rag/DEPLOYMENT.md` Kurzstand.

**Spanien Supplier (ISS-003.3, Wochen_Todo 13):** Andreas fragte nach dem Zeitpunkt; die Entscheidfrage war ihm bis dahin nie gestellt.
09:20 sein OK: der Lieferant des letzten Wareneingangs gilt als Supplier (seine Rueckfrage „Wareneingang ohne Lieferant?“ meinte
Artikel ohne Wareneingang). Plan `docs/PLAN_ES_SUPPLIER_WARENEINGANG_2026-11-12.md`, Entscheide Ingo in Abschnitt 13 (letzter
Wareneingang am oder vor dem Lieferdatum, nur Lagerartikel, Erbregel, zwei Gaenge mit Santi). **Umgesetzt 11:39 in `c9208b5`**
(Sage-Skripte an allen Stellen, Pruefskript `Analyze-SpainSupplierLookup.ps1`, Erbregel im Spanien-Dedupe, Seed, Suite 1556/1556),
**nicht deployed**. Offen: Deploy-Freigabe Ingo; Mail Gang 1 an Santi (Entwurf
`C:\Users\koi\Downloads\Spain_Supplier_Check_Gang1_Santi_2026-11-12.eml`, nicht gesendet); vor Gang 2 Lesepruefung der
Dashboard-Einstellungen und der SharePoint-Kopfzeilen mit Ingos Anmeldung. `docs/STANDORT_ES_SAGE.md` 2 und 5.

**IC V1 (Fabio, Wochen_Todo 153):** Tests in T76/100 bestanden (IV 90381224 bis 90381226), T76K912884 von Fabio fachlich
freigegeben. Import nach P76 wartet auf Fabios Z999-Preise in P76 und die Abgrenzung der PR00-Spiegelsaetze 820; er haengt
nicht an 601/908. Antwortvorschlag 601/908 an Fabio im Chat (RV80HGEN nach Bestaetigung von 908 durch TRIM/Fabio), nicht belegt
gesendet. Offen Ingo: T683T, 501230, Mandant 090, P76-Lesepruefung. `saptasks/ic_preisfindung/NOTES.md` 9.1.

**ZC12 (PM-02, Wochen_Todo 3):** Auftrag Ingo: Wert 0 bei gesetztem Haken zulassen (Arbeitsstunden; heute Abbruch, alter Wert
bleibt), Popup-Wert linksbuendig, Laufzeit im Report; kein neuer Index auf SAP-Standardtabellen. Plan
`saptasks/zc12/PLAN_ZC12_NULLWERT_PERFORMANCE_2026-11-12.md` mit Ingos Entscheiden (Abschnitt 11, 12), Einspieldatei
`saptasks/zc12/ZM_ABGLEICH_KTSCH_einspiel_2026-11-12.abap` (`bb1c771`). **Wartet auf Ingos Ja zur Ausfuehrung in T76**; in SAP
ist nichts geaendert.

**Lieferantenbewertung (Wochen_Todo 122):** Gespraech mit Marco Widmer (iterativ, Ingo baut die erste Fassung); Rueckmeldung
Armin (V6 ist Grundlage, kein Word-Dokument, Paket 1 SAP-Performance mit Q2 und A/B/C je Lieferant, Paket 2 Pflege im
Dashboard); Oscar hat Eingaben im Dashboard freigegeben; SAP-Quellen in T76 lesend geklaert
(`saptasks/einkauf_lieferantenbewertung/SAP_QUELLEN_2026-11-12.md`: `/ITS/`-ABC ist reine Material-ABC, T76 ist S/4HANA 2023,
Fiori-Bewertung von NTT customized mit Preis 0 %). Offen: P76 lesen (Freigabe), SLFDT oder EINDT, Zeitraum A/B/C, danach Plan
Paket 1. `docs/EINKAUF_SITZUNG_2026-10-22.md` 6 bis 6.3. Zertifikatsordner auf `O:` fuer `koi` nicht lesbar.

**WLAN `trafag-802.1x-tel` (Standort Bubikon):** niemand kommt mehr hinein, seit Entec am 11.11. 12:24 dort Client Isolation
wegen MAC-Flaps in VLAN 16 eingeschaltet hat (Verdacht, nicht gemessen). Flap-Geraet `4c:eb:d6:5a:b3:cc` = Espressif (IoT).
Laptop-WLAN `trafag-802.1x`, Zertifikate und Sperrlisten in Ordnung. `docs/router/plattform.md` Abschnitt „Standort Bubikon:
WLAN …“; Nachricht an Alex vorgeschlagen, nicht belegt gesendet; Ingos Handytest steht aus.

**Oscars Auftraege vom 03.11. (Wochen_Todo 137 bis 147):** Statusuebersicht im Chat; offen bei Ingo vor allem 138 Inventar
(Mail an Philip Steiger formuliert; Anleitung „Excel von Hand pruefen“ in `Tools/Inventar/README.md`), 139 PM14, 140, 142, 147.
Datenverantwortung produktiv, Eintraege noch vorlaeufig.

**Doku-Pruefung (Auftrag Ingo 11:45):** alle Markdown-Dateien auf Veraltetes, Widersprueche, Redundanz; Ergebnis in
`docs/MD_KONSISTENZPRUEFUNG_2026-11-12.md`.

**Doku-Pruefung abgeschlossen (13:11, `9e27a70`):** 217 Befunde, 156 eindeutige eingearbeitet, 39 Entscheide und 22
Strukturvorschlaege in `docs/MD_KONSISTENZPRUEFUNG_2026-11-12.md`.

**Code-Review abgeschlossen (nichts geaendert):** 13 Befunde, 4 hoch und im Code bestaetigt (Source Viewer liest die
Konfiguration, Freischalt-Cookie aus dem gespeicherten Hash berechenbar, schreibende Seiten ohne Freischaltung, Standortbestand
wird nicht atomar ersetzt); zwei mittlere betreffen den nicht deployten Spanien-Commit `c9208b5`. `docs/CODE_REVIEW_2026-11-12.md`,
Wochen_Todo 159. Entscheide Ingo offen.

**CO-PA `T76K912866`:** laut Ingo in P76 importiert (12.11.); offen: Gateway-Caches P76 (`/IWBEP`, dann `/IWFND`), Vorschau
gegen September, `Shopfloor:SapCoPaEnabled` einschalten und deployen (`saptasks/copa_verkauf/NOTES.md` 3).

**Nicht committet (fremd):** Finance_All-Automatik (`Program.cs`, `TimerBackgroundService.cs`, `.csproj`), `CLAUDE.md`, geloeschte
Dateien in der Repo-Wurzel, Office-Dateien; Ingo entscheidet.

## Uebergabe 2026-11-11 abends (Chat geschlossen): Spanien-Lieferant, Fabio 601/908, offene Punkte

**Spanien Supplier (Anfrage Andreas 11.11.):** Im `Sales_All` fehlt bei ES der Supplier. Ursache: unsere Sage-Query liest nur
Verkaufsbelege. Loesbar mit eigener SQL ueber den letzten Wareneingang je Artikel (`LineasAlbaranProveedor` +
`CabeceraAlbaranProveedor`, alle Spalten im Schemaauszug belegt). Wartet auf Entscheid Andreas, ob das als Supplier gilt; danach
Plan fuer Query (alle Stellen), ES-Mapping und Uebergabe an Santi (Einspielen, einmal Bereichsexport). Nachgefuehrt: ISS-003.3,
Wochen_Todo 13, `docs/FINANCE_SUPPLIER.md`, `docs/STANDORT_ES_SAGE.md` Abschnitt 2. Nichts umgesetzt.

**Fabio zu 601/908:** Seine Zusammenfassung deckt sich mit `saptasks/ic_preisfindung/NOTES.md` 8.9, dort eingeordnet (VOFM in P76 =
Weg c; 908 in P76 belegt nicht eingehaengt). Entscheid RV80HGEN weiter bei Ingo.

**RFC-Rechte FPA** (frueher am Nachmittag, Commit f1ec3b2): erledigt, Rollen bei Lucas.

**Offen fuer die naechste Sitzung:** Plan zur Bereinigung des eigenen SAP-Werkzeugs (14 ungeschuetzte Skripte in `.tmp_sap_probe`,
drei ueberholte Beispiele in der SAP-Arbeitsweise) vorlegen, nichts vor Freigabe aendern; IC V1 Tests durch Fabio, Freigabe
T76K912884 erst danach; Lieferperformance-Daten auffrischen (Angebot, Stand 02.09.). Nebenbei: `Wochen_Todo.tsv` Zeile 70 hatte
nur 11 Felder (Altlast), aufgefuellt; LEARNINGS-Zeile zu `perl -a`.


## Nachtrag 2026-11-11 nachmittags: RFC-Rechte von FPA in T76 (Frage Fabio)

Fabio fragte, ob FPA in T76 dieselbe RFC-Rolle wie Ingo bekommen kann. Gelesen per RFC: FPA hat `Z_RFC_ALLES_RW` bereits seit 2010.
Damit gehen `system-info` und `table-read`; `table-fields`, `function-info`/`-search` und `abap-read` brauchen SDIFRUNTIME, RFC1
und SIFP, die nur Ingos breite Rolle `Z_SAP_ALL_OHNE_HR_FI_UM` gibt. Zustaendig fuer Rollen ist Lucas (Hinweis Fabio). LIESMICH im
Paket ergaenzt, Zip neu gebaut (28 Dateien). Nichts in SAP geaendert. Einzelheiten SAP-Arbeitsweise Abschnitt 7.

## Nachtrag 2026-11-11 mittags: SAP-Werkzeugpaket fuer Fabio

Auftrag Ingo („bei mir nichts veraendern“): `C:\Users\koi\Downloads\SAP-Werkzeug_Fabio_2026-11-11.zip` (116 KB, 28 Dateien),
gebaut aus Kopien im Scratchpad nach freigegebenem Plan. `.tmp_sap_probe/` unveraendert (Pruefsummen vorher/nachher gleich).
Einzelheiten und was bewusst fehlt: `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` Abschnitt 7. Offene Befunde fuer Ingo: unser
eigener Skriptbestand hat dieselben ungeschuetzten Skripte; die Quelldatei SAP-Arbeitsweise enthaelt ueberholte Beispiele
(`table-read AUFK 5 ...` mit Positionsargument, BKPF-Filter mit `<>`, „bei echten Syntaxfehlern aktiviert SAP nicht“).

## Nachtrag 2026-11-11 09:45: IC V1 Customizing ZICAA1 in T76/100 umgesetzt (Fabio), Vorgehen „erst Plan, dann Plan ausfuehren“

Fabio meldete, dass das V1-Customizing auch in T76 fehlt. Neues Vorgehen nach Fabios Tipp: Phase 1 Plan nur lesend (Agent sap-t76),
Freigabe Ingo, Phase 2 genau nach Plan. Ergebnis: Customizing-Auftrag **T76K912884** (offen, nicht freigegeben) mit Ausschlussgruppe
ZPR0 (PR00), ZICAA1 010 Z999, 012 Z000, 113 Z001, 115 Z002 (Kaskade wie ZVAA01, Entscheid Ingo), Ausschluss 005/006/010, Routine 601
bei Z999/Z002; VTFL schon B, VKOA nur gelesen. Gegengelesen per RFC. Erster Anlauf brach ab, weil nach dem Neustart kein SAP Logon lief.
Offen: Fabio Stammdaten und Tests T1-T10 in T76, FI Konten Z01/Z02, Ingo T683T-Schluessel/501230/A304, Freigabe erst nach Test.
`saptasks/ic_preisfindung/NOTES.md` 9 und `PLAN_V1_CUSTOMIZING_2026-11-11.md`. Fabios Regel jetzt in SAP-Arbeitsweise Abschnitt 7 und
`.claude/agents/sap-t76.md`. Fabios PDF `IC-Preisboard 1100_1200.pdf` und Excel liegen ungetrackt in der Repo-Wurzel, nicht committet.
Kein Code, kein Deploy.

## Nachtrag 2026-11-11 08:14: Wochen_Todo 122 und 141 (Lieferantenmanagement Einkauf) zusammengelegt

Auftrag Ingo nach der Kurzauskunft zum Stand Lieferantenmanagement. Beide Zeilen fuehrten dasselbe Thema (122 seit der Sitzung
22.10., 141 seit der Abschlussrunde 03.11.). Weitergefuehrt wird **122** mit neuem Titel (Lieferantenmanagement, Projekt Armin,
Entwurf V6, Entscheid Oscar „Logik in SAP, Dashboard zeigt an“), Wer jetzt „Armin / Ingo“, Status unveraendert „Wartet auf
Input“. 141 steht auf „Erledigt“ mit Verweis auf 122. Inhaltlich hat sich nichts bewegt: V6 ist nicht gebaut; offen sind
Word-Dokument, SAP-Tabellen/Transaktionen, Fiori-Parametrisierung mit NTT, Entscheide E1 bis E3 und die Frage an Armin/Oscar,
ob „nur Darstellung“ auch fuer den Eingabeteil gilt. Geaendert: `Wochen_Todo.tsv`/`.xlsx`, `Status_kurz.xlsx`. Kein Code, kein Deploy.

## Uebergabe 2026-11-10 spaet (Ingo startet neu)

**IC-Staffelbasis (Wochen_Todo 153, `saptasks/ic_preisfindung/NOTES.md` 8.9):** In P76 importiert (RC 0), Routine 601 aber **nicht
eingebunden** (`RV62ANNN` leer). VOFM in P76 gesperrt (nicht aenderbar); Transport von `RV62ANNN` unmoeglich (`TADIR GENFLAG X`),
Auftrag T76K912870 angelegt und leer geloescht. `RV80HGEN` (kein Selektionsbild, alle VOFM-Gruppen) bindet 601 **und** FBED 908 ein.
908 = Kopierbedingung Proforma TA→F5/TAW aus TRIM-Auftrag T76K912226 „MDE: Intercompany Umlagerungen“, in T76 seit 05.07. aktiv, in
P76 nie eingebunden. **Naechster Schritt:** Ingo klaert 908 (TRIM/MILI bzw. Fabios „Paket“ zur Bestellart, Mail/Datei nennen), dann
`RV80HGEN` in P76 durch Ingo, danach lesend nachpruefen (`D010INC` RV62A601 und RV60B908, `TRDIR` RV62ANNN/RV60BNNN neues `UDAT`).
Fabio informieren: Staffelbasis wirkt erst danach; seine „restliche Anpassung“ = Schritt 1 ZICAA1 (nicht begonnen). *(Ueberholt 2026-11-11: V1 ZICAA1 in T76/100 umgesetzt, `T76K912884`, Nachtrag 2026-11-11 09:45 oben; am 12.11. von Fabio getestet und fachlich freigegeben.)*

**Sicherheit:** T76-Passwort KOI ist im Sitzungsprotokoll sichtbar geworden (eigener Wrapper). Ingo: Passwort aendern und mit
`Set-SapPassword` neu ablegen. LEARNINGS eingetragen.

**Weiter offen aus der Nachmittagsuebergabe:** Deploy/Push der rund 16 Commits (jetzt mehr, nur Doku dazu), HR-Leser absichern,
Wochen_Todo 40 (Antwort an Paola). Alle SAP-Sitzungen von Claude geschlossen; P76 und T76 unveraendert.

## Nachtrag 2026-11-10 abends: P76-Pruefung IC-Staffelbasis nach dem Import (bestaetigt)

Die am Nachmittag abgebrochene lesende Pruefung ist wiederholt (Claude selbst, eigene SE16-Sitzung, geschlossen, nichts geaendert).
Beide Auftraege sind in P76: `TFRM` PFRS 601 aktiv, `RV62A601` vorhanden (KOI, 10.11.), `T685A` V/PR00 `KOFRS 0000601`. **`RV62ANNN`
ist aber nicht neu erzeugt** (letzte Aenderung TRIM_MILI 16.01.2026), `D010INC` kennt kein Programm mit `RV62A601`. Die Routine wirkt
in P76 also noch nicht; die IV rechnet weiter nach Liefermenge. **Naechster Schritt Ingo:** in P76 VOFM *Formeln > Staffelbasis* 601
aktivieren (oder `RV80HGEN`), danach lesend nachpruefen. Fabios Frage nach der „restlichen Anpassung“ meint Schritt 1 (ZICAA1 um
Z999/Z000/Z001/Z002 mit Konditionsausschluss), der nicht begonnen ist. `saptasks/ic_preisfindung/NOTES.md` 8.9, Wochen_Todo 153.
Punkt 1 unter „Unterbrochen“ in der Uebergabe darunter ist damit erledigt.

*Spaeter am Abend (Auftrag Ingo, die Einbindung zu machen):* geht nicht ueber VOFM, P76 ist „nicht aenderbar“. `RV80HGEN` wuerde alle
VOFM-Gruppen neu erzeugen und dabei auch die bisher ungenerierte Kopierbedingung FBED 908 (Proforma TA→F5, TAW) scharf schalten.
Nichts geaendert; Entscheid Ingo zwischen Transport von `RV62ANNN` und `RV80HGEN` offen (NOTES 8.9).

*Danach (Ingo „mache a“):* Transport von `RV62ANNN` geht nicht, das Include ist generiert (`TADIR GENFLAG X`); Auftrag T76K912870
angelegt und leer wieder geloescht. FBED 908 wirkt in T76 seit Juli, in P76 nicht. Offen: Ingo klaert 908, dann `RV80HGEN` in P76.
Die Empfehlung „VOFM 601 aktivieren“ weiter oben in diesem Nachtrag ist ueberholt. **Sicherheitsvorfall:** T76-Passwort von KOI im
Sitzungsprotokoll sichtbar geworden (eigener Wrapper), Wechsel empfohlen; LEARNINGS.

## Nachtrag 2026-11-10 14:35: IC-Staffelbasis in P76 (Abnahme Fabio)

- Fabio hat am 10.11. per Mail alle Tests in T76 als erfolgreich gemeldet (inkl. MIRO 1200 ohne Preisdifferenz) und fuer P76 freigegeben. Ingo hat
  `T76K912853` (Aufgabe `T76K912854`) und `T76K912856` nach P76 importiert. Damit sind die Zeilen „nicht transportiert“ in den Uebergaben unten ueberholt.
- Offen: Pruefung in P76 (`RV62A601` aktiv, `TFRM 601`, V/06 PR00), erste echte IV mit Unterlieferung, Schritt 1 ZICAA1. `saptasks/ic_preisfindung/NOTES.md` 8.9.

## Nachtrag 2026-11-10 14:35: Rexx-Hub-Abruf gebaut, Schalter aus, nicht deployed

- Auftrag Ingo „alles vorbereiten, nur noch Schalter umlegen“. `1e99745` (Agent im Worktree), `d99fbe6` (Nacharbeit aus vier Pruefungen), per Fast-Forward
  uebernommen; der fremde Finance_All-Hunk in `Program.cs` wurde dafuer kurz beiseitegelegt und unveraendert zurueckgespielt. Suite 1547/1547.
- Tagesdienst holt #775/#777 ueber `v1/datawarehouse/{id}` und schreibt die Handexport-Dateien; Schalter `HrKpi:RexxHub:Enabled` aus, Zugang Quellsystem `REXXHUB`,
  Testabruf `/BiDashboard/admin/rexxhub/test`. Umschaltanleitung `saptasks/rexx_schnittstelle/NOTES.md` 14, `docs/HR_KPI.md` 8.18.
- Befund am bestehenden HR-Leser (Kultur bei Text-Zahlen, ganze Datei verworfen bei kaputtem Datum): festgehalten, nicht geaendert.

## Nachtrag 2026-11-10 13:10: Rexx Hub, Mail AG36071 und Produktbeschreibung gelesen (nur Doku)

- Mailverlauf (Alex 10.11. 09:06 „FYI“): Rexx verlangt fuer die Bestellung eine unterschriebene Auftragsbestaetigung; Nadjas Go wartet seit 19.08. auf die
  IT-Pruefung durch Alex. **Hub nach diesem Verlauf nicht bestellt.** Wochen_Todo 133 auf Wunsch Ingo unveraendert.
- Produktbeschreibung (43 Seiten): REST-API, kein Dateiexport; hoechstens 1 Anfrage gleichzeitig, 50'000 pro Monat; kein Endpunkt fuer Abwesenheiten oder
  Salden, aber `GET /v1/datawarehouse/{id}` fuer Rexx-Abfragen. Vorschlag: #775/#777 darueber abrufen; fuenf Fragen an Rexx.
  `saptasks/rexx_schnittstelle/NOTES.md` 13.

## Nachtrag 2026-11-10 11:30: vier kleine Code-Korrekturen aus der Todo-Pruefung (Auftrag Ingo „das erledigen“), nicht deployed

- **Shopfloor** (`c5c9b4a`, `548b10e`): `ShopfloorSapSync.NotFoundHint` nennt bei HTTP 404 den Auftrag, der das abgefragte Set anlegt (CoPaSumSet `T76K912866`,
  ShopRm/ShopProd `T76K912740` + `T76K912738`, ShopZd05Set/ShopAufSet `T76K912718`), und den Gateway-Cache als zweite Ursache. Tests.
- **Trafag Reddit:** Einzahl bei genau 1 (Beitrag, Kommentar, Stimme, akzeptierte Antwort, offene Frage, Punkt; englisch auch Contributor), neue Schluessel in allen
  Sprachkatalogen. Gegenpruefung fand eine Kollision: „akzeptierte Antwort“ ist in den Katalogen (ohne Gross/Klein) derselbe Schluessel wie „Akzeptierte Antwort“; die neuen
  Eintraege wieder entfernt, es gilt der vorhandene.
- **Railway-Export:** Anleitung „Datenluecken je Standort“ statt „Datenluecke TRDE“ (Nachzug bis 09.09.2026, neuere deutsche Rechnungen koennen wieder ohne Name und Land
  ankommen), Eintrag „Leeres Kundenland“ angepasst, neuer Test.
- **Einkauf** (`19ca282`, Agent im Worktree, per Fast-Forward uebernommen; `548b10e`): ungenutzte Abfragen der entfernten Matrix Lieferant/Jahr samt Feld
  `SupplierYearSpendRows` und drei Datentypen geloescht (drei Abfragen je Snapshot weniger, Laufzeit nicht gemessen). Sieben Tests auf den Spend-Aufriss umgestellt, keiner
  entfallen; Staerke mit sechs absichtlichen Verfaelschungen geprueft.
- Suite 1490/1490 im Release-Worktree auf `548b10e`. **Nicht deployed** (Freigabe Ingo und Pruefung Einkauf-Delta stehen aus). *(Ueberholt: deployed am 2026-11-12 11:33 mit `e956183`.)* Gegenpruefung durch fuenf unabhaengige
  Pruefer, Befunde eingearbeitet. Nicht behoben: Klingonisch/Albanisch nutzen fuer „Kommentar“ den Schluessel des Formularfelds (anderes Wort als die Mehrzahl).

## Nachtrag 2026-11-10 10:55: alle offenen Todos gegen Code und Doku geprueft und nachgefuehrt

- Auftrag Ingo „pruefe alle todos gegen code und doku“, danach „ja korrekturen einarbeiten“. 159 Zeilen nur lesend geprueft (Wochen_Todo 106, Status_kurz 46,
  aktive Koordinationszeilen 7), jeder Befund „veraltet“/„erledigt“ von zwei weiteren Pruefern gegengelesen: 63 stimmen, 25 nicht pruefbar (haengen an Personen),
  70 veraltet oder erledigt, 1 Widerspruch.
- **Eingearbeitet:** 47 Zeilen `Wochen_Todo.tsv` (neuer Stand vorangestellt mit „10.11. (Pruefung gegen Code und Doku)“, Verlauf bleibt; geschlossen: 30 Schalter
  Schweizer STPRS, seit 02.09. behoben `8ae972f`; 25 Railway 171 Vorschlaege, ueberholt, weiter in 52), 15 Zeilen `Status_kurz.tsv`, fuenf abgeschlossene Eintraege
  aus „Aktive Bereiche“ der Koordinationsdatei in den Kurzindex. Beide `.xlsx` neu.
- **Offen bei Ingo:** Wochen_Todo 40 (wurde die Antwort 7c an Paola zum Lagerumfang versendet? Issue-Log sagt ja, Fachdatei nein).
- **Nicht eingearbeitet:** veraltete Stellen in Fachdateien, die die Pruefer nebenbei nennen (z. B. `docs/rag/PROJECT.md` J2/J3, `docs/rag/PURCHASING.md` Kopf,
  `zlo03/FIX26`/`FIX27` „nicht freigegeben“, Issue-Log ISS-003.4 „NICHT deployed“). Liste mit Belegen: `.tmp_tools/todo_pruefung_2026-11-10/befunde.json`.

## Nachtrag 2026-11-10 10:20: Qdrant-Reindex robust (Wochen_Todo 118), 111 und 120 nachgefuehrt

- **118:** `Tools/Qdrant/Reindex-Docs.ps1` startet den Indexer als eigenen Prozess (Protokolle `reindex.out.log`/`reindex.err.log` im
  Qdrant-Verzeichnis, nur der Exitcode zaehlt) und vergleicht danach Punktzahl und Abschnittszahl. Neu: `-NurPruefen` (0 aktuell, 3 veraltet),
  `-WennVeraltet`; `index_docs.py --pruefen`, Standdatei `reindex_stand_<Sammlung>.json`. Test mit `*>`: Pruefung „veraltet“ (4545 gegen 5242),
  Neuaufbau 5242 Punkte = 5242 Abschnitte (Exitcode 0). Kein App-Code, kein Deploy.
- **111** war schon seit 04.11. produktiv (`63b0d78`), **120**: Import-Werkzeug seit 22.10. produktiv; beide Zeilen und die alte aktive
  Koordinationszeile berichtigt. Fehler in `docs/rag/LEARNINGS.md`.

## Nachtrag 2026-11-10 09:45: Entscheid Rexx Hub, Transportstaende nachgefuehrt (nur Doku)

- **Entscheid Ingo: wir nehmen den Rexx Hub** (guenstiger, Ingo migriert die bestehenden Schnittstellen, Stempeluhrtransfer
  bleibt). Der Entscheid vom 30.09. („kein Hub“) ist ueberholt. Felder, die Rexx liefern muss: `saptasks/rexx_schnittstelle/NOTES.md` 12,
  Kurzfassung `docs/HR_KPI.md` 8.17. Mail an Nadja im Chat entworfen, Versand durch Ingo. Excel der Feldliste in Ingos Downloads.
  **Offen:** ob der Hub schon bestellt ist (Auftrag Nadja 29.07. laut `docs/HR_KPI.md` 8.3 gegen „vor dem Go“ am 19.08., NOTES 11).
- **Importliste P76 (Bild Ingo):** `T76K912718`, `728`, `736`, `740`, `738`, `742`, `752`, `777`, `781`, `785`, `793`, `797`, `799`,
  `825`, `839`, `858`, `837`, `860` sind importiert. Nachgefuehrt, wo die Statusquelle noch „offen“ sagte: Wochen_Todo 158
  (erledigt, live seit 09.11.), 9, 114, 119, 129 (Import erledigt, offen nur Rueckmeldungen und Tests), `Status_kurz.tsv` ZLO03.
  **Weiter offen (nicht in der Liste):** `T76K912866` (CO-PA), `T76K912853`/`856` (IC-Staffelbasis), `T76K912490` (ZZPRDAT).
- Wochen_Todo 22 und 55 als ueberholt geschlossen (weiter in 133), 133 in Arbeit. Kein Code, kein Deploy.

## Uebergabe Stand 2026-11-10 nachmittags (Ingo startet den PC neu; gilt vor allen Abschnitten darunter)

Produktiv weiter `436e8ad` (Deploy 08:46). **Alles Folgende ist committet, nicht gepusht und nicht deployed** (Zweig
`feature/supplier-overrides-trit`, rund 16 Commits vor `origin`).

**Code seit dem letzten Deploy (Deploy wartet auf Ingo, vorher kein Einkauf-Delta pruefen):**
- `c5c9b4a`, `19ca282`, `548b10e`: Shopfloor-404-Hinweis je Set, Trafag Reddit Einzahl, Railway-Anleitung, Einkauf ungenutzte Matrix-Abfrage entfernt.
- `1e99745`, `d99fbe6`: Rexx-Hub-Abruf #775/#777, **Schalter `HrKpi:RexxHub:Enabled` aus** (`saptasks/rexx_schnittstelle/NOTES.md` 14).
- Suite auf `d99fbe6` im Release-Worktree `C:\TMP\TrafagSalesExporter_release_eafd1c5`: 1547/1547.

**Unterbrochen, nach dem Neustart wieder aufnehmen:**
1. **P76 lesend: IC-Staffelbasis nach Import** (Freigabe Ingo „ja mache test“). Abgebrochen; letzter Zwischenbefund: `RV62A601` wird in P76 von
   keinem Programm eingebunden, die Routine wirkt dann noch nicht (VOFM 601 in P76 aktivieren, Ingo). Unbestaetigt, Pruefung ganz wiederholen.
   `saptasks/ic_preisfindung/NOTES.md` 8.9 (Pruefliste dort).

**Fragen an Ingo (offen):**
- Deploy und Push der Commits oben?
- HR-Leser absichern (ganze Datei verworfen bei Zahl ausserhalb des Datumsbereichs; Text-Zahlen nach Serverkultur, „4.5“ = 45)? `docs/HR_KPI.md` 8.18.
- Wochen_Todo 40: Antwort 7c an Paola zum Lagerumfang versendet? (Issue-Log sagt ja, Fachdatei nein.)
- Mail an Nadja zum Rexx Hub: Entwurf im Chat; Hub laut Mailverlauf **nicht bestellt** (Auftragsbestaetigung unterschreiben, IT-Pruefung Alex).
  Wochen_Todo 133 auf Wunsch Ingo unveraendert.

**Heute erledigt (Einzelheiten in den Nachtraegen unten):** Rexx-Hub-Entscheid und Lieferfelder; alle Todos gegen Code und Doku geprueft
(159 Zeilen, 70 korrigiert, Befunde `.tmp_tools/todo_pruefung_2026-11-10/befunde.json`, Excel in Downloads); Qdrant-Reindex robust
(`-NurPruefen`, `-WennVeraltet`); Transportstaende laut Importliste P76; Rexx-Hub-Mail und Produktbeschreibung gelesen (NOTES 13); IC-Staffelbasis
Abnahme Fabio und Import (853 mit Aufgabe 854, 856).

**Aufraeumen:** Worktrees `.claude/worktrees/wf_b5a7bdf0-027-1` und `wf_0ca7bf2b-68d-1` sind uebernommen und koennen weg. Im Hauptbaum liegen
fremde unkommittierte Aenderungen (Finance_All in `Program.cs`, `TimerBackgroundService.cs`, `.csproj`, dazu `CLAUDE.md`) unveraendert.

## Uebergabe Stand 2026-11-10 (Auftrag Ingo „md nachfuehren, committen, pushen, Chat neu starten“)

Gilt vor allen aelteren Uebergaben weiter unten. Produktiv: Deploy 08:46 `436e8ad`; Doku bis `88e9025` gepusht.

**SAP-Transporte (Freigabe und Import nur Ingo):**

| Transport | Inhalt | Stand | naechster Schritt |
| --- | --- | --- | --- |
| `T76K912866` | `CoPaSumSet` (CE11000 Monatssummen, Struktur `ZSTR_COPA_SUM`, MPC DEFINE, DPC GET_ENTITYSET) | T76 aktiv, rueckgelesen, Gateway 200; **nicht freigegeben** | Ingo: freigeben, importieren. Danach Claude: P76 `/IWBEP` (alle) dann `/IWFND` (`ZPOWERBI_EINKAUF_MDL`) in eigener Sitzung leeren, `GET shopfloor/api/sap/copa?tag=2026-10-01` gegen Sep (AE 6'531'197.94, Umsatz 7'878'114.69), mit Ingos OK `Shopfloor:SapCoPaEnabled = true`, Deploy, `POST sap/copa` fuer heute. `saptasks/copa_verkauf/NOTES.md` |
| `T76K912853` + `T76K912856` | IC-Staffelbasis (VOFM 601, V/06 PR00) | T76 aktiv, nicht transportiert *(ueberholt 2026-11-10 abends: beide Auftraege in P76 importiert, Routine 601 dort nicht eingebunden, siehe „Nachtrag 2026-11-10 abends“ oben)* | Fabio testet in T76; nur zusammen und erst nach seinem OK |
| `T76K912490` | ZZPRDAT | offen | P76-Vorpruefung, Georg, Fabio, Abnahme |

**Wartet auf andere:**
- **Fabio:** Verzug einschalten? Schalter `Shopfloor:SapVerzugEnabled` (aus) und `Shopfloor:VerzugMaterialFilter` (`SEH,TR5,TX,DW`, leer = ohne Filter) in `appsettings.json`; Ingo entscheidet nach Fabios Antwort, dann Wert aendern und deployen. Ausserdem TX `plan5` (Annahme: naechste 5 Arbeitstage) bestaetigen.
- **Andreas:** ISS-032 Gruppenumsatz (Regulierer `VBRK-KUNRG` ins Set, IC-Regel auf den Regulierer), siehe `docs/VERKAUF_2026-10-02.md` Nachtrag und Issue-Log.

**Kleinigkeit offen:** Die Vorschau `sap/copa` meldet bei 404 den generischen Hinweis mit den alten Auftraegen `T76K912740/738` (aus `ReadAsync`); fuer CoPa waere `T76K912866` richtig. Erledigt sich mit dem Import. *Erledigt 2026-11-10 (`c5c9b4a`, `548b10e`): der Hinweis nennt den Auftrag des abgefragten Sets und den Gateway-Cache.*

**Erledigt heute:** ISS-032 belegt, Q-Faelle Status/Produktlinie, DLQ72, Backend-Schutz Patch 07, plan5, Verzug-Schalter, CO-PA gebaut, fuenf Claude-Testreports in T76 geloescht (`Z_KOI_ZLO_FIX26_TEST`, `Z_KOI_VC_TXT_KOPIE`, `Z_KOI_VC_TXT_TESTID`, `Z_KOI_ZLO03_ALT`, `Z_KOI_ZLO_APP_TEST`). Koordinationseintrag abgeschlossen, keine Reservierung offen.

## Tag 10.11. (Auftrag Ingo „12, 13, 1 erledigt; 14, 2-6 durch dich, 9 Prod offen fuer Zaehlung“)

- **9 / ISS-032 belegt:** TRCH-Export nimmt den Auftraggeber (`VBRK-KUNAG`); bei 14.37 Mio ist der Regulierer eine Trafag-Gesellschaft (12.82 doppelt, 1.56 JP/PL); Gruppe rund 100 statt 116 Mio, betrifft auch Finance „Ist ohne IC“. Loesung (Regulierer ins Set, IC-Regel) wartet auf Ingo/Andreas.
- **6 Q-Faelle:** P76 hat fuer QM kein Anwenderstatus-Schema; Status jetzt aus dem Systemstatus-Lebenszyklus, Produktlinie ueber die Materialnummer (deployed 08:16).
- **3 DLQ72** live, Logistik-Soll DW darauf, Historie ab 01.10. nachgeladen; **5** Backend-Schutz Patch 07 und TX plan5 (Annahme) live; **4** CE11000 Skonto = VV240 (Sep exakt), Set-Bau offen; **2** Verzug weiter aus (Fabio).
- Deploy 08:16 `e6f6f70`. `docs/SHOPFLOOR_2026-10-07.md` 20, `docs/VERKAUF_2026-10-02.md`, Monthly-Review-Doku.
- **Vormittag 08:46 (`436e8ad`):** Verzug-Materialfilter schaltbar (`VerzugMaterialFilter`, `SapVerzugEnabled` aus); **CO-PA** gebaut: SAP `CoPaSumSet` in T76 (**`T76K912866`**, rueckgelesen, Test 200), Cockpit mit Schalter `SapCoPaEnabled` aus. **Ingo: `T76K912866` transportieren.** Vier weitere alte Claude-Testreports in T76 geloescht.
- **14** T76: `$TMP`-Report `Z_KOI_ZLO_FIX26_TEST` geloescht (SE38, eigene Sitzung). Weitere lokale Claude-Testobjekte aus Okt/Nov: `Z_KOI_VC_TXT_KOPIE`, `Z_KOI_VC_TXT_TESTID`, `Z_KOI_ZLO03_ALT`, `Z_KOI_ZLO_APP_TEST` (auf Ingos Wort am 10.11. ebenfalls geloescht); aeltere `Z_KOI_*`/`Z_IKO_*` gehoeren Ingo.

## Eingeschaltet 15:11 (Auftrag Ingo „aktiviere was geht und schalte frei“), 2026-11-09

- P76-Importqueue (Bild Ingo): `T76K912839`, `T76K912858`, `T76K912860` ok, `T76K912837` mit Warnung (RC 4).
- Shopfloor Auftragsvorrat eingeschaltet (`SapVorratEnabled`, SEH-Tagesmenge-Config), heute geschrieben (SEH 157'571, TR5 63'921, TX 50'342, DW 117'271). Verzug weiter aus (Fabio).
- Monthly Review Q-Faelle eingeschaltet (`QmEnabled`), Caches P76 geleert, 345 offene Q1-Meldungen; offen: Status und Produktlinie leer (pruefen).
- ZC22 Systemstatus ungekuerzt live (`T76K912858`); Kollege Produktion: Spalte im Layout breiter ziehen.
- Deploy 15:11 `4585392`. Uebergabe-Tabelle unten (09.11. abends) ist damit fuer 837/858/860 erledigt.

## Uebergabe Stand 2026-11-09 abends (Auftrag Ingo „alles in md, committen, pushen, falls ich neu starten will“)

Gilt vor der Uebergabe vom 09.11. morgens weiter unten.

**SAP-Transporte (Freigabe und Import nur Ingo):**

| Transport | Inhalt | Stand | nach dem Import / naechster Schritt |
| --- | --- | --- | --- |
| `T76K912839` | ZLO03 FIX 26 Basismenge + FIX 27 (Excel alt plus „je 1 Stk“) | **in P76**, Einkauf-Lauf 13:29: HW_ZUKAUFSORT 1.27 Mio. statt 957 Mio. | erledigt; Ann-Katrin/Sandro ueber die neuen Spalten informieren |
| `T76K912860` | Shopfloor `ShopAufSet` Typ VR (Auftragsvorrat) | **in P76**, Vorschau = ZC22 exakt (SEH 1'914 / 157'556, DW 1'976 / 117'271, alle Buckets) | Ingo-OK: `Shopfloor:SapVorratEnabled = true`, `config_seh_tagesmenge_sap.js` (--check), Deploy, `POST sap/vorrat` fuer heute |
| `T76K912837` | `QmMeldungSet` (Q-Faelle Monthly Review) | freigegeben 09.11.; Import P76 unbestaetigt *(ueberholt: in P76 importiert und am 09.11. 15:11 eingeschaltet, siehe „Eingeschaltet 15:11“ oben)* | `/IWBEP` dann `/IWFND` leeren, `MonthlyReview:QmEnabled` true, Deploy |
| `T76K912858` | ZC22 Systemstatus ungekuerzt (`ZMM_UEB_FAUF-STTXT` TEXT100, Form `STATUS_LANG`) | bereit in T76 *(ueberholt: in P76 und live seit 09.11. 15:11, Wochen_Todo 158)* | freigeben, importieren; wirkt sofort; Kollege Produktion: Spalte im Layout breiter ziehen |
| `T76K912853` + `T76K912856` | IC-Staffelbasis (VOFM 601, V/06 PR00) | in T76 aktiv, **nicht transportiert**; Fabio testet zuerst in T76 | nur zusammen und erst nach Fabios OK |
| `T76K912490` | ZZPRDAT | noch nicht | P76-Vorpruefung, Georg (`ZXCO1U06`, `T76K911783`), Fabio, Abnahme |

**Shopfloor (Mail Fabio 09.11., Ampel im Chat):** a) Vorrat bereit zum Einschalten (oben); b) Verzug = ZC22 exakt, Schalter aus bis Fabio zu
Material 1-99999 (SEH immer 0) und Layoutfilter `/ P00 TX` antwortet; c) DLQ72 und d) CE11000 nicht gebaut (naechste Aufgaben); TX `plan5`
Definition offen; Backend-Schutz Patch 07 offen. Antwortentwurf an Fabio im Chat (Ziel a) Mi 11.11.). `docs/SHOPFLOOR_2026-10-07.md` 19.

**Verkauf (Sitzung Patrick):** Sankey/Sparte deployed; ISS-032 offen (GFS/Poltraf als IC-Regel mit Andreas, Doppelzaehlung TRCH/Toechter
per P76-Lesepruefung Rechnung 90390762 belegen); Liste `Downloads/Sparte_fehlt_PRODH_2026-11-09.xlsx`; Entscheid zentrale Spartenliste
fuer Toechter offen. `docs/VERKAUF_2026-10-02.md` Abschnitt 2026-11-09.

**Einkauf (Armin):** Materialanteil abnahmebereit; offen Armins Entscheid Materialnummer 1-99999 im Dashboard; Fragen 1-6 (Mailentwurf
angeboten); V6 Lieferantenbewertung nicht gebaut (Logik SAP, Entscheide offen).

**T76-Aufraeumen:** `$TMP`-Report `Z_KOI_ZLO_FIX26_TEST` enthaelt zurzeit den VR-Test (`saptasks/shopfloor_sap/Z_KOI_SHOP_VR_TEST.abap`),
loeschen; meine T76/P76-Sitzungen sind geschlossen.

## Shopfloor Verzug P76 abgeglichen, Auftragsvorrat gebaut; Einkauf FIX 26 in P76 bestaetigt (Auftrag Ingo), 2026-11-09 nachmittags

- PPD-Verzug: Vorschau P76 = ZC22 exakt (DW 9/185, TR5 11/465, TX 11/310, SEH 0); offen fuer Fabio: Layoutfilter `/ P00 TX` (Arbeitsplatz, Systemstatus) und Material 1-99999 (SEH immer 0). Schalter aus.
- Auftragsvorrat: SAP Typ VR in T76 (`T76K912860`, SEH 1'877/179'981 gleich ZC22), Cockpit `ShopfloorSapVorrat` deployed 14:12 (`01fceec`), Schalter aus bis Import und Abgleich. `docs/SHOPFLOOR_2026-10-07.md` 19.
- Einkauf: `T76K912839` (ZLO03 FIX 26/27) von Ingo importiert; Einkauf-Delta 11:24–13:29: E11221 → E11283 0.000211 ROL, **HW_ZUKAUFSORT Materialanteil 1.27 Mio. statt 957 Mio.** (34.7 % von 3.67 Mio.).
- `T76K912837` (QmMeldungSet) von Ingo freigegeben; Import P76, Caches und `MonthlyReview:QmEnabled` noch offen.

## Verkauf Gruppenzahlen, Sankey und ZC22 (Auftrag Ingo), 2026-11-09

- Verkauf Interaktiv deployed 10:45 (`04b4faf`): Sankey ganz beschriftet, „Ohne Kundenland“, Sparte zusaetzlich ueber TRCH-Nummer (TRIN). Befund Gruppenumsatz zu hoch (GFS/Poltraf nicht als IC, vermutete Doppelzaehlung TRCH/Toechter, mindestens rund 6 Mio) als ISS-032; Liste fehlende Sparte/PRODH in Ingos Downloads. `docs/VERKAUF_2026-10-02.md` Abschnitt 2026-11-09.
- ZLO03 FIX 27 in T76 im selben Auftrag `T76K912839` wie FIX 26: Excel-VKNR-Spalten wieder mit der alten Menge je Basismenge, neue Menge je VKNR als Zusatzspalte „je 1 Stk“, Dashboard unveraendert neu (Wunsch Ingo). Test T76 bestanden. `zlo03/FIX27_ALTE_UND_NEUE_MENGE_2026-11-09.md`.
- ZC22 Systemstatus ungekuerzt in T76 (`T76K912858`: Struktur `ZMM_UEB_FAUF` STTXT TEXT100 und Form `STATUS_LANG`), nicht freigegeben. `docs/abap/README_ZM_OFFENE_FAUF_BEDARFSVERURSACHER.md`.

## Shopfloor-Paket Fabio 07.11. eingespielt (Auftrag Ingo), 2026-11-09

- Patch 06 (RS offen = ohne Abschlussdatum) und 07 (SAP/MES-Werte gesperrt) deployed 07:23 (`fd345e7`). Configs DW lieferbereit (DLQ72) und SEH Tagesziel warten auf die SAP-Felder; Backend-Schutz fuer Patch 07 offen. Wochen_Todo 156, `docs/SHOPFLOOR_2026-10-07.md` 18.

## Uebergabe Stand 2026-11-09 (vor Neustart des PCs, Auftrag Ingo „alles md nachfuehren“)

*Ueberholt durch die Uebergabe vom 09.11. abends oben (Transportstand).*

**Offene SAP-Transporte (alle T76, nicht freigegeben; Freigabe und Import nur Ingo):**

| Transport | Inhalt | bereit? | nach dem Import |
| --- | --- | --- | --- |
| `T76K912839` | ZLO03 FIX 26 Basismenge (Materialanteil Wafer), seit 09.11. mit FIX 27 (Excel alt plus neu) | ja | Einkauf-Lauf, Materialanteil pruefen (HW_ZUKAUFSORT ~1.27 Mio. statt 957 Mio.), Ann-Katrin/Sandro informieren |
| `T76K912837` | `QmMeldungSet` (Q-Faelle Monthly Review) | ja | `/IWBEP` dann `/IWFND` leeren, `MonthlyReview:QmEnabled` true committen und deployen |
| `T76K912853` + `T76K912856` | IC-Staffelbasis nach Bestellmenge (VOFM 601, V/06 PR00) | ja, mit Vorbehalt (S6 nur in P76 testbar, MIRO-Buchung in T76 offen) | nur zusammen; `RV62A601` in P76 aktiv pruefen; Fabio: endgueltige Unterlieferung bestaetigen |
| `T76K912490` | ZZPRDAT (BAdI, Reiter-Anzeige `ZXCO1U11`, Rueckgabe `ZXCO1U12` repariert, Georgs XCO1-Objekte) | noch nicht | vorher P76 lesend: Import `T76K911390`/`T76K911398` (Projekt ZPP00012), Append `ZCO_ORD1`; Georg zu `ZXCO1U06` und `T76K911783`; Fabio informieren; Abnahme Lucas/Marco/Adil (Testnachweis `saptasks/zzprdat/testdoku_2026-11-06/`) |

**T76:** Verbindung riss am 06.11. 15:27 ab, Ingo muss T76 neu anmelden. Offene Testbelege: MIRO 0051128478 (fehlerhaft, Buchung offen), Infosatz 5300015578 ohne SPEZ (Entscheid Ingo/Fabio). Hilfsreport `Z_KOI_ZLO_FIX26_TEST` aufraeumen.

**Mails/Teams offen:** Fabio Begleitmail zum Testnachweis ZZPRDAT und Stand Staffelbasis (Entwurf auf Wunsch); Johannes Zetterblom Teams-Nachricht zum CRM (Text im Chat vom 06.11., `docs/CRM_DYNAMICS365_2026-11-03.md`); Antwort an Fabio zu Kurs/Terminen (KW fuer Schritt 1/2 offen).

**Entscheide offen:** Einkauf-Doppelungen je Reiter (Wochen_Todo 154), FP_THERMOST.-Fix, Texte „Nachts laeuft automatisch ein Delta“, `segmente.png`/`Bahnmarkt_DE_Kundenzuordnung` in der Wurzel geloescht (nicht von Claude; `segmente.png` wird von der Seite Marktsegmente gebraucht — wiederherstellen?), neun Worktrees mit Aenderungen und Gruppe 3 (spartenlogic/.tmp_*, cinebbox, ersetzt, output) beim Aufraeumen.

*Ueberholt: Uebergabe vom 2026-11-05 15:00 weiter unten.*

## IC-Staffelbasis und ZZPRDAT-Nachweis (Auftraege Ingo), 2026-11-06

- ZZPRDAT: `ZXCO1U12` repariert (Reiterwechsel loeschte das Datum, CO02-Sichern haette es geloescht), Durchspieltest 1241850, Testnachweis Word mit 21 Screenshots (Auftrag 1241851), alles in `T76K912490`.
- IC-Staffelbasis bei Unterlieferung (Mail Fabio 06.11., Blatt 8 `IC_Umsetzung_V1.xlsx`): VOFM 601 nur ZICAA1, Test IV 90381202 (394.20 statt 453.90 EUR), S7 Stufe Bestellung = IV, S6 nicht testbar in T76. `saptasks/ic_preisfindung/NOTES.md` 8.
- CRM: Fragen an Johannes nur in der Doku, nie gestellt; Antworten teils abgeleitet (kein Navision in den bekannten Verkaufsquellen).

## ZZPRDAT Abnahmesitzung und Transport gebuendelt (Auftrag Ingo), 2026-11-05/06

- Abnahmetest: Logik schreibt korrekt (AUFK), Reiter „TRAFAG Daten“ zeigte nichts, weil `ZXCO1U11` in Georgs offenem Auftrag `T76K911783` (Mandant 090) auskommentiert war. Auf Ingos Entscheid Georgs Objekte entsperrt (`RSWBO099`, Aufgabe `T76K911784`), Anzeige eingebaut, CO03 1241847 zeigt 11.11.2026.
- Alle ZZPRDAT-Objekte in `T76K912490` (Aufgaben `T76K912491` BAdI-Loesung, `T76K912852` XCO1-Objekte inkl. `ZXCO1TOP` gegen Syntaxfehler in P76). Nicht freigegeben. Offen: P76-Importstand ZPP00012/ZCO_ORD1, zweiter Schreibweg `ZXCO1U06`, Georg informieren. `saptasks/ZZPRDAT_TRANSPORTPLAN.md`.

## Aufraeumen Wurzel und Arbeitskopien (Auftrag Ingo), 2026-11-05 nachmittags

- Logs nach `.tmp_logs/` (`032722f`), sechs versionierte Dateien ohne Code-Verweis nach `docs/daten/` und `docs/abap/` (`e27d8af`).
- Von Ingo selbst ausgefuehrt (Auto-Modus lehnte Loeschen ab), Skript von Claude: 20 saubere Worktrees entfernt (15 alte `C:\TMP\…release_*`, `dev_projekte*`, zwei Juni-Kopien, zwei Agenten-Worktrees vom 05.11.), Build-Reste geloescht (`build_verify`, `verify_probe_out*`, `bintest`, `Tests/bintest`, `.tmp_hb_build`, `binmon`, `__pycache__`), rund 20 GB.
- Stehen gelassen: `C:\TMP\TrafagSalesExporter_release_eafd1c5` (Deploy-Werkzeug), neun Worktrees mit nicht committeten Aenderungen (dev_hri, localization_verify, release_a471ce2, sechs Agenten-Worktrees aus dem Oktober), `output/` (Excel-Auswertungen), Ordner aus Gruppe 3 (spartenlogic/.tmp_*, .tmp_pbix_x_extract, cinebbox-connect-downloader, ersetzt, AlphaplanExportPackage/delta) bis Entscheid Ingo.

## Uebergabe Stand 2026-11-05 15:00 (falls die Sitzung abbricht) *(ueberholt durch Uebergabe 2026-11-09 oben)*

**Zuletzt:** Antwort an Fabio Palma (IC-Preisfindung, Mail „AW: IC-Preisfindung automatisieren …“ vom 05.11. 14:04, allen antworten) als **Entwurf in Outlook Web gespeichert 14:59, nicht gesendet**; Inhalt: Preisliste 14 kommt in der IV an, Korrektur Kurs (M -> EURX 0.94, Kurstyp P unnoetig), ERL fuer Z001/Z002, Schritt 2 Job 4 bis 6 PT. Offen: Ingo traegt „KW __“ ein und sendet (oder gibt OK zum Senden).

**IC-Preisfindung (Wochen_Todo 153):** T76 und P76 lesend geprueft, `saptasks/ic_preisfindung/NOTES.md` (Abschnitte bis 7). Naechste Schritte: Customizing ZICAA1 in T76 (Z999, Z000, Z001/Z002 mit ERL, Konditionsausschluss ZPR0/Z999/Z001, Reihenfolge neben PR00/IV01/IV02/VPRS klaeren), Tests T1-T10 mit Fabio/Sandro (`a.xlsx` Blatt 4), Baustein fuer Infosatzpflege in P76 pruefen (Schritt 2). Fabios Unterlagen liegen ungetrackt als `a.xlsx`/`b.pdf` in der Repo-Wurzel (Geschaeftsdaten, nicht committen).

**Offen bei Ingo:** Transporte freigeben und nach P76 importieren: `T76K912839` (ZLO03 FIX 26 Basismenge, danach Einkauf-Lauf, Materialanteil HW_ZUKAUFSORT erwartet rund 1.27 Mio. statt 957 Mio.; Ann-Katrin/Sandro informieren) und `T76K912837` (QmMeldungSet, danach Caches /IWBEP dann /IWFND, `MonthlyReview:QmEnabled` true committen und deployen). Einkauf-Delta wurde 12:38 fuer den Deploy bewusst abgebrochen; Nachholen pruefen (Datenservice). Entscheide: Doppelungen Einkauf-Reiter (Wochen_Todo 154), FP_THERMOST.-Fix, Texte „Nachts laeuft automatisch ein Delta“, Patrick: Q-Statusfilter/Produktlinie/Bearbeiter; T76 aufraeumen `Z_KOI_ZLO_FIX26_TEST`.

**Arbeitsweise neu:** Agenten nur mit `.claude/agents/` (sap-t76, einkauf, finance-verkauf, hr; wirken erst in einer neuen Sitzung als Typ, bis dahin Datei zuerst lesen lassen), SAP nie mit Passwort (`router.md` Abschnitt Agenten, LEARNINGS 05.11.).

## Einkauf-Fixes deployed, ZLO03 FIX 26 in T76, Agenten-Definitionen (Auftraege Ingo), 2026-11-05 nachmittags

- Einkauf F2, F3, F4, F6, F8, F9 (`01ab4b9`, Doku `a17d623`), 1460/1460, deployed 11:48, im Browser bestaetigt; F5: Delta haengt nicht an IsActive, laeuft im Timer-Slot (laut Doku 12:00).
- ZLO03 FIX 26 in T76 (`T76K912839`, `zlo03/FIX26_BASISMENGE_2026-11-05.md`), P76 lesend: dieselben 6 Stuecklisten; FP_THERMOST. andere Ursache (Selbststuecklisten).
- Agenten-Definitionen `.claude/agents/` (sap-t76, einkauf, finance-verkauf, hr), Verweis in `router.md`.
- Spend-Aufriss aufgeraeumt (Wunsch Ingo): Matrix Lieferant / Jahr (`08caa7a`) und Block „Bestellwert nach Lieferant und Jahr“ (`785325f`) entfernt, doppelt zu den Einstiegen; `/einkauf/spend` zeigt den Aufriss; Gehirn bleibt eingeklappt; Handbuch nachgefuehrt; deployed 12:37, Delta dafuer bewusst abgebrochen.
- Neu Wochen_Todo 154: Einkaufsseite aufraeumen (Wiederholungen, mehrere Aufrisse zu einem), erst wenn die Werte stimmen (Wunsch Ingo).

## Verkauf Monthly Review mit Q-Faellen, SAP QmMeldungSet, Einkauf-Pruefung, Mails (Auftraege Ingo), 2026-11-05

- **Monthly Review Gesellschaften** (Wunsch Patrick Hertach): Unterreiter `/verkauf/monthly-review`, Phase 1 und Abschnitt 4
  „Qualitaet · offene Q-Faelle“ live aus SAP (`26ab30b`, `8ff3685`, Korrektur Statuscodes `47abb30`), 1443/1443, deployed
  10:33 (`docs/rag/DEPLOYMENT.md`). Q-Faelle-Abruf aus, bis `T76K912837` in P76 ist. `docs/monthly_review/MONTHLY_REVIEW_2026-11-04.md`.
- **SAP T76:** `QmMeldungSet` in `ZPOWERBI_EINKAUF_SRV`, Auftrag `T76K912837` (313 offene Q1 in T76), `saptasks/qm_meldungen/NOTES.md`.
- **Einkauf:** Funktionspruefung aller Ansichten im Chrome, Fehlerliste `docs/EINKAUF_PRUEFUNG_2026-11-05.md`; Materialanteil
  957 Mio. durch ignorierte Stuecklisten-Basismenge (`zlo03/BEFUND_EINHEIT_WAFER_2026-11-05.md`), FIX 26 freigegeben, noch nicht umgesetzt.
- **Mails:** Antwort an Fabio Palma zur IC-Preisfindung (ZICAA1, Wochen_Todo 153) und an Adrian Jenni zur SPC-Adapterauswertung
  (Wochen_Todo 151) gesendet. Wochen_Todo 150 bis 153 samt `.xlsx`, LEARNINGS (4), Router Einkauf, baum, Koordination.

## Nachfuehrung zum Chatschluss (Auftrag Ingo „md nachfuehren“), 2026-11-04 13:10

- Verkauf Monthly Review: Auftrag, Entscheide, Phasen und T&P-Befund in `docs/monthly_review/MONTHLY_REVIEW_2026-11-04.md`,
  Spezifikation und Entwurf daneben (bisher nur im Scratchpad bzw. ungetrackt in der Wurzel). Router Finance, baum, VERKAUF (Marge),
  Wochen_Todo 150, Koordination (pausiert). HR: Ampelstand und Fragen an HR in `docs/HR_KPI.md` 8.16, rag/HR_KPI, Wochen_Todo 149.

## HR-Cockpit: Kachel „Unfalltage laufender Monat“ aus SAP- und Rexx-Faellen (Auftrag Ingo), 2026-11-04

- `AccidentDaysCurrentMonth`, hoeherer Wert aus SAP-Tagesdatei und Unfallfaellen; `dac3098` (12:36) und Korrektur `f86b0ed` (12:41,
  auch aus #775 ergaenzte Unfaelle), 3 Tests, Suite 1404/1404, produktiv, Kachel 4,2 statt 0,0. Rexx-Zeitmanagement lesend geprueft,
  Tab fror dreimal ein. Doku HR_KPI 8.15, rag/HR_KPI, rag/DEPLOYMENT, LEARNINGS, Koordination.

## HR-Cockpit: Rexx-Abwesenheiten je Fall #777, Interaktiv mit Datum (Auftrag Ingo „alles gruen“), 2026-11-04

- Rexx: private Abfrage #777 in der Antragsliste (Von/Bis, Art, Status, Tagessoll). Cockpit: `MergeRexxCases` ergaenzt SAP um
  1'203 bestaetigte Faelle mit Datum (nur nicht abgedeckte Tage). `4bdbc18`, 3 Tests, Suite 1401/1401, **produktiv seit 12:21**,
  in Chrome geprueft (Ferien am Stueck 101 von 260, Fit & Wohl 31). Offen: rund 100 direkt in Rexx gebuchte Krankheitstage ohne
  Datum (13 Personen). Doku HR_KPI 8.14, rag/HR_KPI, rag/DEPLOYMENT, Router HR, Schulungsseite, Handbuch, LEARNINGS, Wochen_Todo 149.

## HR-Cockpit: Rexx-Gesamtabfrage #775 statt vier Rexx-Dateien, das Aktuellere je Person (Auftrag Ingo), 2026-11-04

- Rexx: private Abfrage #775 „HR-Cockpit Gesamt (Dashboard)“ (50 Spalten). Cockpit liest `Hrcockpitgesamtdashboard.xlsx` statt
  #757/#732/#744/#381, wenn sie in `hrdata` liegt; je Person der hoehere Wert aus SAP und Rexx fuer 01.01. bis Exporttag;
  Rexx-Unfaelle ergaenzen SAP; Ferien bezogen direkt aus Rexx (alte Formel zog geplante Tage doppelt ab). `6dd1a56`, 12 Tests,
  Suite 1398/1398, **produktiv seit 11:46**, in Chrome geprueft. Vorher 11:23: Saldi-Export mit Stichtag 31.12.2024 in `hrdata`
  ersetzt. Doku `docs/HR_KPI.md` 8.13, rag/HR_KPI, rag/DEPLOYMENT, Router HR, Handbuch Kapitel HR, Schulungsseite, LEARNINGS (5),
  Wochen_Todo 149, Koordination.

## HR-Cockpit: Formeln geprueft, drei Rechenfehler behoben, neuer Reiter Interaktiv (Auftrag Ingo), 2026-11-04

- Pruefung aller Formeln (`docs/HR_KPI.md` 8.10). Behoben in `HrKpiDashboardBuilder`: Krankenquote-Nenner je Person erst ab
  Eintritt (`AbsenceDenominator`, `FteWorkdays`), Quote je Person mit Pensum, Prognose rechnet ein laufendes Quartal vorher auf
  die volle Laenge hoch (`QuarterProgress`, `ForecastFromQuarter`). Offen fuer Entscheid: 61-Tage-Regel, Vorjahresvergleich im
  laufenden Jahr, Kachelbeschriftung bei Von/Bis.
- Reiter Interaktiv im HR-Cockpit (`docs/HR_KPI.md` 9): `Services/HrKpi/HrKpiInteractive.cs`, `Components/HrKpi/Interactive/`
  (acht Ansichten), CSS `hri-`, 66 Texte in sieben Sprachkatalogen, Tests `HrKpiInteractiveTests` und drei neue in
  `HrKpiServiceTests`. Schulungsseite und Handbuch Kapitel HR nachgefuehrt. Router HR, rag/HR_KPI, Wochen_Todo 149,
  Status_kurz. *Ueberholt „nicht deployed“:* nach Nachbesserung Filtervertrag (`b65f059`) **produktiv seit 09:20**,
  mit Datenverantwortung und Shopfloor-Startseite (`docs/rag/DEPLOYMENT.md`). Um 09:30 Rexx-Dateien in `hrdata` durch
  Ingos Exporte vom 04.11. ersetzt (alte archiviert, `docs/HR_KPI.md` 8.2); dabei zwei offene Befunde (Sollzeit-Spalte
  nicht erkannt, nur Status „Aktiv“), 8.10. LEARNINGS: GIF-Aufnahme der Chrome-Erweiterung ist kein Video.
- Danach Uebergang bis zur Rexx-Schnittstelle (`docs/HR_KPI.md` 8.11, Auftrag Ingo „das Aktuellere nehmen“): Status
  „Vertrag endend“/„Langzeitkrank“ zaehlen, Sollzeit-Spalte „Ø tägliche …“ erkannt (Vollzeit = FTE 1,0), juengste
  Rexx-Krankmeldungen ergaenzen SAP (`BuildRexxGapSicknessRows`, am 04.11. 34 Faelle), SAP-Stand bei Ferien/Kompensation/
  Militaer sichtbar. Vier neue Tests, Suite 1381/1381, Probe gegen echte Serverdateien. Handbuch, Router, rag, LEARNINGS.
  **Produktiv seit 09:57** (`cfe2631`, Delta vorher geprueft).
- Danach HR-Vorgaben aus Mails und Teams umgesetzt (`docs/HR_KPI.md` 8.12; Mails Nadja 10/2025, Sonja 02/2026 und 09/2026
  in Outlook Web gelesen, Teams ohne Fachinhalt): Kurz/Lang je Krankheit ab dem 61. Tag (SAP-Faelle verbunden, 0270 lang),
  Unfall 0290 in Quote, Krankheitskalender und Fit &amp; Wohl, Kachel „Quote ohne Langzeit“, Gelb auf der Uebersichtskachel,
  Vorjahresvergleich gleicher Zeitraum, Von/Bis-Kachel mit Spanne und Headcount ueber die Spanne. Sechs neue Tests, Suite
  1386/1386, Probe mit echten Daten. Neue Frage an HR: Kontrollwerte 2025 passen nicht zum Rexx-Export. Handbuch, Router,
  rag, LEARNINGS, Wochen_Todo 149, Status_kurz. **Produktiv seit 10:21** (`69a63b6`, Delta vorher geprueft).


## CRM Dynamics 365 Sales: Befund und Idee lesende Anbindung festgehalten (Auftrag Ingo „notiere das alles in md“), 2026-11-03

- `docs/CRM_DYNAMICS365_2026-11-03.md` (neu): Scorecard Opportunity Pipeline (Johannes) kommt aus Dynamics 365 Sales, nicht Navision; Kette, IDs,
  Felder (`ava_sapid` = SAP-Kunde), Zahlen Stand 20.07., Vorschlag Dataverse Web API mit Leserolle, offene Fragen.
  Router Standortdaten, baum, LEARNINGS (Power BI schickt Zugriffsanfragen automatisch). Kein Code, kein Deploy.

## Wochen_Todo auf den Stand vom 03.11. gebracht (Frage Ingo), 2026-11-03

- Erledigt: 86 (ZC22-Layout P76), 116 (Abschlussrunde), 134 (Aufgaben Chef eingeordnet). Nachgetragen: 43, 45, 54, 131, 133, 145.
  Neu 148 (voller HR-Export fuer Sonja, Zusage heute).

## Inventar: Scan-Skript fuer Bericht-Excel und Power BI vorbereitet (Auftrag Ingo „bereite vor“), 2026-11-03

- `Tools/Inventar/Scan-BerichtDateien.ps1` mit README: nur Metadaten, Benutzer/Passwoerter werden entfernt; getestet auf eigenen
  Ordnern (Simons Excel als Power Query auf `tragvsqlc400` erkannt). Ausfuehrung erst nach OK von Alex. Wochen_Todo 138, PROJEKTSTATUS
  PM-14, baum, LEARNINGS.

## Doku nachgefuehrt: Status kurz fuer den Chef, Rexx-Hub-Sitzung, Ansprechpartner (Frage Ingo „alle md nachgefahren?“), 2026-11-03

- `Status_kurz.tsv` samt `.xlsx`: Rexx, Abwesenheiten, ZC22, Abschlussrunde, Trafag Projekte aktualisiert; neu ZREICHW, PM14,
  Seite Datenverantwortung, Berechtigungskonzept.
- Rexx-Hub-Sitzung naechste Woche (Alex, Artjom) in rag/HR_KPI, HR_KPI 8.9, Rexx-Notizen; ANSPRECHPARTNER mit Artjom und
  Abschnitt Datenverantwortliche.

## Datenverantwortung: neue Seite mit Eingabeformular fuer Dataowner (Wunsch Chef, Auftrag Ingo), 2026-11-03

- `/datenverantwortung`: Datentopf, System, Verantwortlich, Stellvertretung, Key User, Status vorlaeufig/bestaetigt, Bemerkung;
  lesen fuer alle, bearbeiten mit Admin-Login; Tabelle `DataOwners` mit Startbelegung aus der Abschlussrunde.
- Code `c267651`, Release-Worktree 1360/1360; Deploy nach dem Einkauf-Lauf. Doku Sitzungsprotokoll 6, Router Plattform,
  rag/ARCHITECTURE, Wochen_Todo 145.

## Projektmanagement: Aufgabenliste des Chefs vom 03.11. eingeordnet, 2026-11-03

- Neu Wochen_Todo 145 (Dataowner-Dashboard), 146 (Projektmanagement deaktivieren, Rueckfrage an den Chef), 147
  (Berechtigungskonzept mit AD-Abgleich); 138 und 139 ergaenzt. Sitzungsprotokoll Abschnitt 6, PROJEKTSTATUS PM-14. Nur Doku.

## Trafag Projekte: Projekt PM14 Datenqualitaet und Abloesung angelegt (Auftrag Ingo „ja anlegen“), 2026-11-03

- Ueber „Vorhaben aus Wochen_Todo uebernehmen“ mit nur dem Vorhaben PM-14 und `PM-14_TrafagProjekte_Import.tsv` (per Chrome,
  Adresse mit `/BiDashboard/`): 1 Projekt neu, 53 Aufgaben neu; privat, Leitung Ingo. Wochen_Todo 139 in Arbeit, PROJEKTSTATUS,
  LEARNINGS (Chrome-Sperre war die fehlende Pfadangabe).

## Projektmanagement: PM-14 fuer Trafag Projekte vorbereitet, Wochen_Todo 55/137, 2026-11-03

- `projektmanagement/PM-14_TrafagProjekte_Import.tsv`: 53 Aufgaben (DQ-01 bis DQ-30, INV-01 bis INV-19, Datenverantwortung, Aufwand) im Wochen_Todo-Format; Ingo laedt sie im Projekt PM14 ueber Abgleich Wochen_Todo hoch.
- Wochen_Todo 55: Sitzung Rexx Hub naechste Woche mit Alex und Artjom (Nadja/Sonja laut Alex nicht noetig); 137 erledigt (Link an Dragan).

## Projekte: Abschlussrunde Dashboard und Data Lake gehalten, Protokoll aus dem Transkript, 2026-11-03

- Rueckmeldungen Einkauf, Operations, HR, Finance, Produktion; Datenverantwortliche; Oscar: Power-BI-/Smartsheet-Inventar,
  Taskliste offener Wuensche mit Aufwand und Datenqualitaet in Trafag Projekte, abgeloeste Z-Objekte loeschen.
- `docs/SITZUNG_ABSCHLUSSRUNDE_DASHBOARD_2026-11-03.md` (neu), Wochen_Todo 55 und 137 bis 144, PROJEKTSTATUS PM-10/11/12/14,
  Router Projekt, rag/PROJECT, baum. Nur Doku.

## SAP ZC22: Banf-Abruf statt Kundenauftrag als Bedarfsverursacher bei Umlagerung (Wunsch Anh Nhi, Auftrag Ingo „mach du“), 2026-11-03

- P76 gelesen: ZC22 zeigte fuer 46690 KndAuftr 419042; der Bedarfsverursacher (`MD_PEGGING_NODIALOG`) liefert den Endbedarf
  `VC` in Werk 1200, nicht den Banf-Abruf; Layout `/ P00 TX` in P76 ohne Bedarfsverursacher-Spalten (nach dem 09.10. nie nachgezogen).
- T76: neue Form `UMLAGERUNGSBEDARF` in `ZM_OFFENE_FAUF` (offene Umlagerungsbestellung `U1` bzw. offene BANF `U2`, kontiert auf
  den Kundenauftrag des Endbedarfs, aus dem Werk des Auftrags), Auftrag `T76K912825`, aktiv, zurueckgelesen, Test 63502 `U1 45148422/10`.
- Am 03.11. in P76 importiert (Ingo); Layout `/ P00 TX` in P76 angepasst und gesichert (Ueberschreiben von Ingo bestaetigt);
  Pruefung 46690: `U1 45152789/10` (BANF inzwischen in Umlagerungsbestellung umgesetzt). Offen nur die Antwort an Anh Nhi.
  Doku `docs/abap/README_ZM_OFFENE_FAUF_BEDARFSVERURSACHER.md`, Router SAP, Wochen_Todo 136, LEARNINGS, SAP-Werkzeuge.

## HR Rexx: Mails zum Upgreat-Ticket und zum Rexx-Hub-Angebot gelesen (Outlook Web, Auftrag Ingo), 2026-11-03

- Upgreat schloss Ticket #12699584 am 19.10. mit „Unable to solve“ (Rexx betreut Trafag selbst); Alex leitete am 29.10. Nadjas
  Hub-Angebot weiter (8 Schnittstellen 11'200 EUR/Jahr, 4 weitere 5'600 EUR/Jahr, Hub 4'800 EUR/Jahr); Rexx +2 % ab 2027; Sonja:
  HR konnte noch nichts testen. Nachgefuehrt: `saptasks/rexx_schnittstelle/NOTES.md` 11, `HR_KPI.md` 8.9, rag/HR_KPI,
  ANSPRECHPARTNER (Rexx Systems Schweiz), Wochen_Todo 55, PM-14-Listen. Nur gelesen, nichts beantwortet.

## Projektmanagement: neues Vorhaben PM-14 Datenqualitaet und Abloesung (Sitzung Chef, Auftrag Ingo), 2026-11-03

- PM-14 in PROJEKTSTATUS, HERMES-Sicht und Wochen_Todo 134; zwei Listen im Ordner `projektmanagement`: 30 Befunde zur
  Datenqualitaet (wer bereinigt was) und Inventar von 19 Power-BI-, Smartsheet- und Excel-Ablaeufen, dazu
  `PM-14_Datenqualitaet_Abloesung.xlsx` (`pm14_listen_xlsx.py`). Eingesparte Stunden nirgends beziffert, Spalten zum Schaetzen.
- Neu Wochen_Todo 135: Klartext-Zugangsdaten der Tochtergesellschaften in der Produktiv-DB (Tabelle `Sites`). Kein Code, kein Deploy.

## SAP ZREICHW: schnelle Fassung ins Original uebernommen (Entscheid Ingo „ja uebernehmen“), 2026-11-03

- T76: `Z_KOI_REICHW_TURBO` durch die schnelle Fassung ersetzt, im selben Auftrag `T76K912799` (nur `REPS Z_KOI_REICHW_TURBO`),
  aktiv, per RFC zeilengleich zurueckgelesen. Vergleich ganzes Werk 1100: 152'280 Zellen, 0 Abweichungen, 2,3 s statt 155 s.
  Spool-Test 1265145 ohne Ausgabeauftrag. Lokale Kopien `Z_KOI_REICHW_OPT`/`_VGL` geloescht.
- Am 03.11. von Ingo nach P76 importiert; P76 gelesen: Haken da, Werk 1100 in rund 5 s. Laut Ingo erledigt und passend (03.11.). Doku `saptasks/zreichw/NOTES.md` 6, Router SAP, Wochen_Todo 132.

## HR: Rexx-Schnittstelle nach SAP gefunden, Idee eigenes Programm mit Z-Tabelle (Auftrag Ingo „alles in md festhalten“), 2026-11-03

- T76 nur gelesen: Eigenentwicklung der EXPRIS AG (Paket `Z_HR_PA_REXX`); Sonja Richter (`RIS`) importiert von Hand
  Stammdaten (`Z_HR_PA_REXX`), Absenzen (`Z_HR_REXX_PA_ABS`, Batch-Input PA30) und Kommen/Gehen (`Z_HR_REXX_PA_ZE`), kein Job.
- Krankheit/Unfall nimmt der Absenz-Import nicht mit; Ferien, Kompensation und Militaer kommen nur mit dem Import nach SAP,
  `HrAbsenzSet` hinkt dafuer nach (Aussage „P76 immer aktuell“ in `HR_KPI.md` 8.7 als ueberholt markiert).
- Mails (nur Betreff): Upgreat-Ticket #12699584 am 19.10. geschlossen, Angebot AG36071 Rexx Hub 29.10.; Inhalt ungelesen.
- Idee Ingo: eigenes Programm nur in Z-Tabellen, taeglicher Job; nicht gebaut. Doku `saptasks/rexx_schnittstelle/NOTES.md`
  (neu, mit Quelltexten aus T76), `docs/HR_KPI.md` 8.9, rag/HR_KPI, Router HR, ANSPRECHPARTNER (EXPRIS), PROJEKTSTATUS PM-07,
  Wochen_Todo 55/133, LEARNINGS (drei Zeilen). Kein Code, kein Deploy.

## SAP ZREICHW: nur Spoolauftrag statt Sofortdruck (Teams-Anfrage, Auftrag Ingo), 2026-11-02 abends

- `Z_KOI_REICHW_TURBO` druckte nie selbst; gedruckt wurde ueber den Druckdialog des Grids mit „Sofort ausgeben“. Neu: Ankreuzfeld
  „Nur Spoolauftrag (nicht drucken)“, die Liste geht ohne Sofortdruck in einen Spoolauftrag (SP01/SP02).
- T76 eigener Auftrag `T76K912799`, aktiv, zurueckgelesen zeilengleich; Test: Spool 1264623 ohne Ausgabeauftrag, alle Spalten.
- Offen: Freigabe und Import P76 (Ingo). Befunde zur Optimierung in `saptasks/zreichw/NOTES.md` Abschnitt 3.
- Danach optimierte Kopie `Z_KOI_REICHW_OPT` ($TMP) ohne Ergebnisaenderung: Werk 1100 von 155 s auf 2,2 s, gegen das Original
  zellgleich (5'076 Zeilen, 152'280 Zellen, auch mit allen Optionen und Januarbeginn; Debug-Liste gleich). Uebernahme ins Original offen.
- Uebergabe fuer den 03.11. mit Reihenfolge der naechsten Schritte, Antworttext fuer Teams und Werkzeugen in `saptasks/zreichw/NOTES.md`
  Abschnitt 5; Hilfsskripte aus dem Scratchpad nach `saptasks/zreichw/werkzeuge/` gesichert.

## Operations KPI: Rubrik Kunden auf OTD / OTIF / OTP (Spezifikation Fabio), 2026-11-02

- Zaehler je Auftraggeber und Monat im Monatsabruf, Namen fuer alle Kunden, Suche nach Nummer oder Name, Kacheln, Monatstabelle,
  Trefferliste, „Abweichungen dieser Kunden“, Excel Kunden YTD / pro Monat (`aa12a39`, Tests 1358/1358).
- Deploy 13:57 (`187f8a0`, mit dem PPD-Verzug bei ausgeschaltetem Schalter), Oktober um 14:02 neu abgerufen, Abnahme bestanden:
  alle Pruefwerte Fabios exakt (alle Kunden, Hitachi 32 Kunden / 2'462 / 94.2 %, 682, 24157, 9204, 24940, 682 im Oktober).

## Shopfloor: PPD-Verzug wie ZC22 gebaut (Mail Fabio), 2026-11-02 nachmittags

- Fabios Antwort: DW = `D*` ohne DW1/DL6, Eckendtermin ohne Untergrenze bis Vortag, positive Werte. ZC22-Logik aus dem Quelltext
  gelesen (Menge = Positionsmenge PSMNG/GSMNG, Planauftraege nur mit Umsetzkennzeichen).
- SAP T76, eigener Auftrag `T76K912797`: `ShopAufSet` Modus `Typ eq 'VZ'` (nur DPC, MPC unberuehrt). Abgleich gegen ZC22 je Auftrag
  gleich (DW 1'570 / 49'526, TR5 786 / 41'127, TX 1'349 / 73'895), 0,1 s statt 6 min.
- Cockpit `27badb2`: Verzug je Abteilung, Endpunkte `sap/verzug`, Schalter `SapVerzugEnabled` aus, Tests 1345/1345, nicht deployed.
- Offen: SEH ist mit Material 1-99999 immer 0 (alphanumerische Materialnummern), Rueckfrage an Fabio.
- Abends: Ingo hat `T76K912797` und `T76K912793` (Zeivr) nach P76 importiert, Gateway-Caches in P76 geleert. Vorschau 02.11. aus P76:
  TR5 507, TX 64, DW 60, SEH 0; `Zeivr` 200 erst nach einem zweiten `/IWFND/CACHE_CLEANUP` (LEARNINGS). Offen: Abgleich mit ZC22, dann Schalter an.

## Einkauf Phase 2 gebaut: Materialanteil je Produktgruppe nach Materialgruppe, 2026-11-02

- SAP T76 (Auftrag `T76K912785`, zusammen mit ZLO03 FIX 24): ZLO03 FIX 25 (`P_APP`, Ergebnis ins Memory), Struktur `ZSTR_ZLO_TOPDOWN`,
  `ZloTopDownSet` in MPC/DPC, Caches geleert; Test 41019 (H05039 3 ST zu 0.014133 CHF wie FIX 23). Ein Syntaxfehler in der ersten
  Fassung legte ZLO03 in T76 zehn Minuten lahm (LEARNINGS, Anleitung berichtigt).
- Dashboard `d6c0287`: Lader im Einkauf-Lauf (ZC23 je Monat, Absagegrund leer, ZLO03 je Block, Disponent 001 bis 006, Probe vor dem
  Lesen), eigene aufklappbare Tabelle mit rotem Knopf im Spend-Aufriss, Texte in allen Sprachen, Tests 1325/1325; deployed 11:18 (`f36e1e7`).
- Deploy 10:09 (`a445f30`): Disponent 006, Verkaufsabgleich Shopfloor, Handbuch. CLAUDE.md Abschnitt 5 (Deploy immer mit dem Werkzeug).

## Zeichnungsaenderung: SAP-Daten fuer die MS-Liste der Entwicklung, 2026-11-02

- Anfrage Simon Klaey (ueber Andreas Wild, Alex Donato, Lucas Castro): Benennung, Index und Materialstatus sollen sich in
  der MS-Liste nach Eingabe der Materialnummer fuellen. Simons Excel liest dafuer direkt `P76.MARA`/`P76.MAKT` per SQL;
  die Formel findet bei rund einem Drittel keinen Index und zieht teils die franzoesische Benennung.
- Entscheid (Mail Ingo, von Simon bestaetigt): `Zeivr` an `MARA001Set`, das Dashboard legt naechtlich eine Liste mit vier
  Spalten im SharePoint ab, die MS-Liste liest sie mit Standard-Connectoren. Suche nur ueber die Materialnummer.
- Offen: Zustimmung Lucas, SharePoint-Schreibrecht (Alex), Ablageort (Andreas). `saptasks/zeichnungsaenderung/NOTES.md`,
  Router SAP, `baum.md`, Wochen_Todo 131.
- T76, 12:05: `Zeivr` (MARA-ZEIVR, CHAR 2) an `MARA001Set` ueber einen Zusatz in `ZCL_ZPOWERBI_EINKAUF_MPC_EXT->DEFINE`
  (`docs/abap/ZMARA_ZEIVR_ADD.abap`), eigener Auftrag `T76K912793`, aktiv und zeilengleich rueckgelesen. Gateway Client:
  altes `$select=Matnr,Mstae,Matkl` weiter 200, neues Feld 200 mit Werten (Laengenvergleich), `ZloTopDownSet` und `MAKTSet`
  200. Lesecode unveraendert. `SapGuiSitzungSchliessen.vbs` korrigiert (Index per `CLng()`).
- P76, abends: Ingo hat `T76K912793` importiert, Caches auf `trasp76s` geleert (parallele Sitzung). `$metadata`, `Matnr,Zeivr`
  und die alte Abfrage `Matnr,Mstae,Matkl` 200. SAP-Seite damit fertig; der Dashboard-Abzug wartet auf Lucas, Alex, Andreas.

## Einkauf: Disponenten 001 bis 006 und Bauplan Materialanteil Phase 2, 2026-11-02

- Neuer Einkaufsdisponent 006 (Entscheid Ingo): `PurchasingPlanners` jetzt 001 bis 006, wirkt im Schalter „Nur Einkaufsteile“ aller
  Aufriss-Einstiege, im Lagerwert und im Verlauf je Disponent (rückwirkend, weil alle Disponenten gespeichert sind); Texte in allen
  Sprachen, Handbuch Einkauf. Commit `9a26acc`, Release-Worktree 1318/1318, **nicht deployed**.
- Bauplan `saptasks/zc23_materialanteil/BAUPLAN_PHASE2_MATERIALANTEIL_2026-11-02.md`: Schritt 1 über das vorhandene `Zc23Set`
  (Absagegrund im Dashboard filtern), Schritt 2 braucht `P_APP` in ZLO03 und ein neues Set; Armins Bedarfsmenge war der
  ZLO03-Verbrauch, nicht die ZC23-Menge (Frage F1). Entscheide Ingo und Absender Armin in `NOTES.md`.
- Koordinationsdatei repariert: Zeile „ZLO03 FIX 19“ war in `1209baf` mitgelöscht worden, wiederhergestellt; LEARNINGS.

## Einkauf: Entwurf Lieferantenbewertung V6 und Rechenweg Materialanteil Phase 2 festgehalten, 2026-11-02

- Nur Doku, kein Code. `a.txt`/`a.html` (Lieferantendashboard V6: vier Reiter, drei offene Entscheide A/B/C-Basis,
  Bewertungsrhythmus, zentrale Datenhaltung) in `docs/EINKAUF_SITZUNG_2026-10-22.md` Abschnitt 5, Demo abgelegt unter
  `docs/einkauf_lieferantenbewertung/`. `b.txt`/`b.png` (ZC23 Termin bestätigt -> ZLO03 Top-Down -> Disponenten 001 bis 006 je
  Materialgruppe, Standardpreis x Bedarfsmenge) als Nachtrag zu Phase 2 in `saptasks/zc23_materialanteil/NOTES.md`.
  Die vier Dateien in der Repo-Wurzel kann Ingo löschen. Wochen_Todo 122/123, Router Einkauf, `rag/PURCHASING.md`, `baum.md`.

## Shopfloor: Paket Fabio vom 01.11., Verkauf über Fiori (J4), KEA0, 2026-11-02

- Patch 04 (PPD Terminverschiebungen aus den Rückstandslisten, standardmässig aus) und Patch 05 mit Fabios `apply_patch.js`
  eingespielt, Verkaufs-Config mit `config_verkauf_fiori.js`: Transmitter = TR5 + TX (`ae_tr5`/`u_tr5` entfallen), erwarteter
  Umsatz aus Fiori statt ZC23. Je zuerst `--check`, Ersetzungsstellen und Feldnamen gegen den Port geprüft.
- J4 neu: `ShopfloorSapVerkauf` holt erwarteten Umsatz (Umsatz + offene Lieferungen + offene Aufträge) und Gutschriften aus
  `C_SALESANALYTICSQRY_1_CDS`, Monat des Vortags, Vorschau `GET shopfloor/api/sap/verkauf`, Schalter `SapVerkaufEnabled`.
- KEA0 in T76 gelesen: Ergebnisbereich 1000, `CE11000`, Produktsparte `WWPSP`, Wertfelder des Nettoerlöses (`CE11000`/`WWPSP`
  waren seit Juni aus dem Produktsparten-Mapping bekannt); offen nur Skonto `VV240`/`VV245`, das entscheiden wir selbst gegen
  Fabios Sep-Kontrollwerte. Commit `82f4f52`, Release-Worktree 1318/1318, produktiv 08:21 bitgleich ohne Alarm.
  Vorschau 10/2026 = Fabios Kontrollwerte (8'827'624.28 / 110'412.21 CHF), heute per POST geschrieben, danach
  `SapVerkaufEnabled = true` im Repo (wirkt nach dem nächsten Deploy). `docs/SHOPFLOOR_2026-10-07.md` Abschnitt 16, Wochen_Todo 130.

## ZLO03 FIX 24: Stückkosten ungerundet, Textpositionen nicht im Bedarf, Bottom-Up-Details in Stück, 2026-10-30

- Befunde G aus `zlo03/CLAUDE.md` umgesetzt, nachdem FIX 12 bis 23 am 29.10. in P76 importiert waren. Stückkosten jetzt mit
  10 Nachkommastellen (vorher 2: K50057 mit 0.02 je 10 ergab 0.00 und Wert 0), Feld „Verbräuche zurück in Tagen“ ausgeblendet
  (wirkungslos), Textpositionen zählen in Top-Down und Bottom-Up nicht mehr zum Bedarf, Bottom-Up-Details stehen in der
  Einheit der Stückliste. G4 Mehrstufigkeit nicht geändert, erst gegen CS12 prüfen.
- T76 aktiv in `T76K912785`, rückgelesen identisch. Fünf Läufe Zelle für Zelle gegen den Vortag: nur Stückkosten und Werte
  ändern sich (58944: K50057 0.002, Wert 19.67), 46123/B63485 1 statt 2 ST in beiden Richtungen, H05039-Details in ST.
  Freigabe und Import offen (Ingo). `zlo03/FIX24_RUNDUNG_TEXTPOS_DETAILS_2026-10-29.md`, Wochen_Todo 129.

## ZLO03 FIX 23: Top-Down in Stücklisten-Einheit (Meldung Armin), 2026-10-29

- H05039 in 41019: Basis KAR (1 KAR = 600 ST, Standardpreis je KAR), Stückliste in ST (Top-Down und Bottom-Up-Komponentenspalte); `UNIT_CONVERSION_SIMPLE` setzte ST 1:1
  in KAR um, Bedarfsmenge × Stückkosten war 600-mal zu hoch. Jetzt über MARM: Mengen und Stückkosten in der Einheit der
  Stückliste, Werte in CHF unverändert. T76 aktiv in `T76K912781`, rückgelesen identisch, Lauf 41019 geprüft (0.014133 CHF je ST).
  Freigabe und Import offen (Ingo). `zlo03/FIX23_EINHEIT_STUECKLISTE_2026-10-29.md`, Wochen_Todo 128.

## Trafag Projekte: Smartsheet-Ansicht, Risiken, PMO, Vorhaben, 2026-10-29

- Auftrag Ingo: Smartsheet-Modus mit gleicher Funktion, Balken ziehen mit automatischer Anpassung, Risikobewertung mit
  Kritikalität, alle Vorhaben der Wochen_Todo als Projekte; nach der Sitzung SAP PS zusätzlich Risiken in Franken mit
  Burndown und PMO-Vorlage, Vorlage Produktentwicklung, Projektstrukturplan für SAP, Decision Log.
- Neu: Reiter Raster & Gantt, Risiken, Projektstrukturplan, Entscheide; Vorlagen Produktentwicklung und IT-Projekt mit
  fertigem Plan; Knopf „Vorhaben aus Wochen_Todo übernehmen“ (Projekte PMnn, privat). Terminierung in Arbeitstagen ohne
  Zürcher Feiertage mit Ende-Anfang-Abhängigkeiten, Sammelzeilen und kritischem Pfad, ein Einstieg für alle Terminwege.
- Zusammengeführt als `1e3300d` (Branches `feature/projekte-smartsheet`, `-risk`, `-pmo`), Suite `1311/1311`, deployed
  2026-10-29 12:23 bitgleich ohne Alarm. Im Browser noch nicht gesehen (Chrome-Erweiterung, Edge headless ohne Blazor).
- Doku: `docs/TRAFAG_PROJEKTE_2026-10-09.md` Abschnitt „Smartsheet-Ansicht, Risiken und PMO“, Handbuch Kapitel 8,
  `docs/rag/DEPLOYMENT.md`. Offen: Phase 2 Guide, Phase 3 Auslastung/Automatisierungen/Formulare/Excel des Rasters,
  Abstimmung mit Philip vor breiter Ankündigung.

## Doku: Sitzung SAP PS und Projektmanagement-Werkzeug zusammengefasst, 2026-10-29

- Aufnahme mit Whisper `large-v3` transkribiert, Zusammenfassung als `docs/SITZUNG_SAP_PS_PROJEKTMANAGEMENT_2026-10-29.md`
  (Wunsch Ingo „für zukünftige Tasks“): Projektwerkzeug oben (Philip testet Project Power Pack, Entscheid offen), SAP PS nur
  Geld und Zeit, Schnittstelle Projektstrukturplan; Lucas fragt NTT Data an, Philip liefert Testprojekte 133.2 und 103.46.
  Router SAP, `baum.md`, PROJEKTSTATUS PM-10/PM-13, Wochen_Todo 43 und neu 126 samt `.xlsx`.

## Wuensche Armin: ZLO03 FIX 22 (Disponent, Warengruppe) und ZC23-Gruppe BM fuer Disponent 026, 2026-10-29

- ZLO03 Top-Down: Spalten Disponent (`MARC-DISPO`) und Warengruppe (`MARA-MATKL`) hinter PFAS-Gehalt, im .xlsx als Text
  (`026` bleibt `026`). T76 aktiv in neuem Auftrag `T76K912777` (`T76K912752` war schon freigegeben), rueckgelesen
  identisch (3'813 Zeilen), Lauf Typencode `8475*` 126 x 184. Freigabe und Import nach P76 offen (Ingo).
  `zlo03/FIX22_DISPO_MATKL_2026-10-29.md`.
- ZC23/ZDISPO: in P76 neue Gruppe `BM` = `BG_MECHANIK` und `026` -> `BM`; SM30 von Claude ausgefuellt, von Ingo gesichert,
  lesend gegengeprueft. `docs/PURCHASING_PRODUCT_GROUP_SAP_DIRECT_2026-08-11.md` Nachtrag 2026-10-29. Wochen_Todo 124, 125.

## Doku: Widersprueche in allen Markdown-Dateien aufgeloest, 2026-10-29

- Auftrag Ingo („alle 163“): alle getrackten `*.md` ausser Archiv und Koordinations-Historie in sieben Aesten geprueft
  (Einstieg/RAG, Finance, Einkauf/Logistik, SAP, HR/Operations/Plattform, Projektstatus, Handbuch). Aufgeloest nur mit
  Beleg (Code, git log, Deploy-Protokoll, Issue-Log, spaeterer datierter Eintrag); datierte Aussagen sind als
  *ueberholt am …* markiert, nicht umgeschrieben. Rund 110 Stellen in etwa 80 Dateien (`4b1f20c`). Nachpruefung im
  Folge-Commit: 21 Finance-Dateien, die der erste Durchgang nur per Suche gesehen hatte, vollstaendig gelesen (rund 60
  weitere Stellen), dazu `.tmp_sap_probe/ddic_lzcode/README.md` und die Befunde 1, 2, 7, 8 der Konsistenzpruefung vom 29.09.
  Neuer Befund: Kursumweg A2 (`ISS-018`) ist seit dem Budgetkurs-Standard vom 07.10. nach Code-Lesung wirksam, nicht latent.
- Schwerpunkte: „nicht deployed“ nach dem Deploy (Einkauf, Netzwerk, Logistik, Journal-Ausgleichsfelder); `T76K912530`
  seit 01.10. und `T76K912728/36/38/40` seit 16.10. in P76; Produktgruppe ohne 1/n seit 26.10.; Kader 8,2 h
  (Mitarbeiterkreis 10) seit 22.10.; `docs/HR_KPI.md` 8.6 neu betitelt, doppelte 8.7 als 8.8; Koordinations-/Abschlussregel
  in `AGENTS.md`, `docs/router/projekt.md`, `docs/rag/init.md` an `CLAUDE.md` (Kurzauskunft 3a, LEARNINGS) angeglichen;
  Handbuch-Kapitel Einleitung, Finance, Einkauf, HR, Projekte, Operations auf den App-Stand berichtigt (Reiter erst nach
  Deploy, Word nicht neu erzeugt; `UserManualContentTests` 4/4).
- Status: Issue-Log `ISS-004` erledigt (4.1 bis 4.3 erledigt), `ISS-006.1` Deploystand, `ISS-007.2` Paola-Antwort versendet;
  Wochen_Todo 9, 66, 93, 114 und Status_kurz (Kader, ZLO03, ZC12) nachgefuehrt, `.xlsx` neu erzeugt.
- Nicht geaendert, nur gemeldet: `zlo03/` und `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` (reserviert), `CLAUDE.md`
  und `Tools/Qdrant/README.md` (fremde unkommittierte Aenderungen). Offene Entscheide fuer Ingo im Chat vom 29.10.
- Qdrant: Index war seit dem abgebrochenen Lauf vom 27.10. nur zu 1'248 von rund 4'400 Abschnitten gefuellt. Neu aufgebaut nach dem Folge-Commit,
  direkt ueber `uv run --python 3.12 Tools/Qdrant/index_docs.py` (Reindex-Docs.ps1 bricht mit Umleitung unter PowerShell 5.1
  ab, Wochen_Todo 118): 165 Dateien, 4'467 Abschnitte, `points/count` = 4'467, Exitcode 0.

## Einkauf Spend-Aufriss: Produktgruppe nach Verwendung : Warengruppe, 2026-10-28

- Wunsch aus der Sitzung 28.10. (Kostenstruktur Material je Produktgruppe, Phase 1): neuer Einstieg Produktgruppe nach
  Verwendung -> Warengruppe -> Material, Basis Einkaufsvolumen, gewichtet nach verwendenden Nummern (ZLO03), Summe exakt.
  SAP-Regel-CTE gemeinsam mit der Produktgruppe genutzt. Commit `8653f6b`, Suite im Release-Worktree 1280/1280, deployed 2026-10-28. Phase 2
  (Materialanteil aus der Standardkalkulation, ZC23 erweitern) offen: `saptasks/zc23_materialanteil/NOTES.md`, Wochen_Todo 123.

## Einkauf: Matrix Lieferant / Jahr im Spend-Aufriss, Reiter Spend ausgeblendet, 2026-10-28

- Wunsch Ingo: Die Matrix „Kaskadierung Lieferant / Jahr“ (aufklappbar bis Warengruppe und Material) ist jetzt eine eigene
  Komponente `PurchasingSupplierYearMatrix` und steht unten im Spend-Aufriss. Der alte Reiter Spend wird beim Start einmalig
  ausgeblendet (Merker `migration-2026-10-28-einkauf-spend-ausblenden`), die Route `/einkauf/spend` bleibt; ein Admin kann ihn
  unter Menuestruktur wieder einblenden. Commit `45b2158`. Nachtrag: der ganze Inhalt des alten Reiters (Kacheln, Spend pro
  Lieferant, Datenstatus, Volumen nach Warengruppe/Region/Waehrung, Matrix, Quellen) steht jetzt unter dem Aufriss, Commit
  `c501053`, Suite im Release-Worktree 1279/1279, deployed 2026-10-28.

## Einkauf: Bild im Kopf, 2026-10-28

- Wunsch Ingo: Bild „Warenkorb mit Sensoren“ (`wwwroot/einkauf/warenkorb-sensoren.jpg`, 676x560, 88 KB) zwischen Titel
  und Ladestand, auf die Kopfhoehe eingepasst; der schwarze Bildgrund verschwindet per `mix-blend-mode: screen`, unter
  1250 px ausgeblendet. Commit `8a568e5`, Suite im Release-Worktree 1279/1279. Deployed 2026-10-28 10:05.

## Einkauf: Ladestatus im Kopf gekuerzt, voller Text im Popup, 2026-10-28

- Wunsch Ingo: Die lange Ladestatus-Meldung rechts im Kopf des Einkauf-Cockpits verzog den Kopfbereich. Sie ist jetzt auf
  drei Zeilen gekuerzt; der Knopf „Ladeprotokoll“ zeigt den vollen Text in einem Popup. Commits `ae67f7e`, `abf3a92`,
  `5c568b0` (Uebersetzungen inkl. Berndeutsch), Suite im Release-Worktree 1279/1279. Deployed 2026-10-28 09:45, siehe `docs/rag/DEPLOYMENT.md`.

## Handbestellwesen: neue Mitarbeitende nur aus dem AD, 2026-10-27

- Entscheid Ingo: AD- und Altbestand-Eintraege unterschieden (Spalte `AusAd`); neue nur aus dem AD, dort gepflegt (Name,
  Mail, Konto gesperrt, „Aus AD aktualisieren“); Altbestand voll pflegbar. Commit `3ff16f5`, Suite 1294/1294, deployed 13:45. Konzept 6.10.
  Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Einkauf Produktgruppe nur mit SAP-Text, 2026-10-27

- Wunsch Ingo: Spend-Aufriss zeigt „FP_DICHTEWÄCH“ statt „D2 - FP_DICHTEWÄCH“. Commit `0917d9e`, Suite 1293/1293.
  Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Handbestellwesen: Stammdaten loeschen, Mitarbeitende aus dem AD, 2026-10-27

- Loeschen nur ohne Bestellungen (Mitarbeitende mit Bestellungen werden deaktiviert), AD-Suche fuellt Name, Mail, Konto.
  Commit `a8e9eec`, Suite 1293/1293. Details Konzept 6.9. Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Handbestellwesen: Freigabe wie PHP, umschaltbar, 2026-10-27

- Entscheid Ingo (1a): Freigeben darf jede aktive Person mit genuegend Limit, gleiche Kostenstelle zuerst. Schalter
  `Handbestellwesen:FreigabeNurGleicheKostenstelle` fuer die strenge Regel. Grund: 929 von 1'032 Bestellungen der letzten
  12 Monate haetten sonst keinen Freigeber. PHP ist abgeschaltet, Reiter sichtbar. Commit `a0030e8`, Suite 1291/1291.
  Details Konzept 6.8. Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Handbestellwesen klassisch: Link „Zur neuen Version“, 2026-10-27

- Wunsch Ingo: Auf `/handbestellwesen` steht oben unter dem Logo ein blauer Link auf `/bestellwesen`. Commit `9ef7787`,
  Suite 1290/1290. Details `docs/HANDBESTELLWESEN_KONZEPT_2026-10-22.md` 6.6. Deploy siehe `docs/rag/DEPLOYMENT.md`.

## SAP ZZPRDAT: Live-Pruefung vor dem Abnahmetest, 2026-10-26

- In T76/100 nur lesend geprueft (RFC ueber SapProbe, in SAP nichts geaendert, P76 nicht beruehrt): Der aktive
  Quelltext ist identisch mit `saptasks/zzprdat/produktiv/`, die letzte Aenderung war am 07.09., das BAdI
  `Z_ZZPRDAT_UPDATE` ist aktiv, `T76K912490`/`T76K912491` sind offen, und es gibt keinen weiteren Transport.
- Neu dokumentiert sind sieben Testauftraege der Benutzer TAN/FPA (1241831 bis 1241844, 08. bis 23.09.). Sie passen
  zum Sollverhalten, die Aenderungsbelege wurden nicht gelesen.
- Nachgefuehrt: `saptasks/ZZPRDAT_TRANSPORTPLAN.md` (Abschnitt „Live-Pruefung vom 2026-10-26“), `saptasks/zzprdat-kontext.md`,
  `docs/rag/PROJECT.md`, `projektmanagement/PROJEKTSTATUS.md` (PM-03), `Status_kurz.tsv`, `Wochen_Todo.tsv` (Zeile 56)
  samt `.xlsx`, `docs/rag/LEARNINGS.md`. Ergebnis des Abnahmetests noch offen.

## Einkauf: Schalter „Nur Einkaufsteile (Disponent 001 bis 005)“ im Spend-Aufriss, 2026-10-26

- Entscheid Ingo: Schalter fuer alle Einstiege, Vorgabe an; ausgeschaltet ganzer Einkauf. Diagramme unveraendert ganzer
  Einkauf. Commit `c3147e1`, Suite 1290/1290. Deploy siehe `docs/rag/DEPLOYMENT.md`.

## Einkauf: Spend-Aufriss nach Disponent (zwei Sichten), 2026-10-26

- Wunsch Ingo: gruppieren nach ZC23-Feld DISPOKENNZ (eigener MARC-DISPO, 001, 002 ...) und zusaetzlich nach dem
  Disponenten der verkuerzten Nummer aus ZLO03 (1/n verteilt). Beide Summen exakt. Commit `bddf9f9`, Suite 1289/1289.
  Deployed 2026-10-26 14:38 (von Ingo gestartet). Bottom-Up-Abruf bleibt deshalb an.

## Einkauf: Produktgruppe aus eigenem Disponenten wie ZM_OFFENE_KDAUF, 2026-10-26

- Entscheid Ingo: MARC-DISPO (Werk 1100) des Einkaufsmaterials -> ZDISPO_GRP/ZDISPO_SPART -> genau eine Gruppe; keine
  Vererbung ueber die Verwendung, keine 1/n-Verteilung. Mehrdeutige SAP-Regeln (DS1/DS2, 016): kleinste Gruppennummer.
  Neue Spalte `MarcDispo`, fuellt der naechste Einkauf-Lauf. Suite 1288/1288. Deployed 2026-10-26 09:39, Delta danach von Ingo von Hand gestartet (Ergebnis nicht nachgeprueft).

## Einkauf: Produktgruppen-Sichten ohne „uebrige (n)“, 2026-10-23

- Wunsch Einkauf „alle Positionen anzeigen“: Produktgruppen-Einstiege waren als einzige noch auf 20/15/10 gedeckelt;
  jetzt ungedeckelt wie alle anderen seit 2026-08-27. Suite 1289/1289. Deployed 2026-10-23 09:05.

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
