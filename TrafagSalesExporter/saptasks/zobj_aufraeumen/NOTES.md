# Z-Objekte aufraeumen: Analysereport `Z_KOI_ZOBJ_NUTZUNG`

Stand: 2026-11-26 14:10 (Abschnitt 9: erster Lauf in P76, 3'379 Objekte, genutzt 175 / indirekt 1'012 / tot 304 / unklar 1'888, Liste in
Downloads); davor 2026-11-26 (Abschnitt 8: P76 gelesen, ST03N nur 08–11/2026, SCMON seit 08.08.2024 aus, SUSG leer; Entscheid Ingo: Basis
SCMON/Aufbewahrung, Report einmal in P76, Loeschen fruehestens 2027). Davor 2026-11-25 14:40: `T76K912944` von Ingo freigegeben (13:47) und in P76 importiert (25.11. 13:49:12, beendet 13:49:14, RC 0; aktive Version von `Z_KOI_ZOBJ_NUTZUNG` in P76 traegt den Auftrag, SE01-Protokoll, Agent nur lesend). Davor 07:10 (Agent, Auftrag Ingo 24.11. „ja bau es in t100“): Report in T76 gebaut, aktiv und getestet; ~~Transport `T76K912944` offen, nicht freigegeben~~.

## 0. Zweck

Lesender ABAP-Report fuer T76/100, spaeter per Transport nach P76. Er liefert je Objekt aus `TADIR` (`PGMID = 'R3TR'`)
eine Zeile mit Stammdaten, Verwendung, Jobs, Rollen, Nutzung und einer Einstufung (genutzt, indirekt, tot, unklar).
Er entscheidet nicht und loescht nichts.

## 1. Was in T76 schon angelegt ist

Vorher (24.11.): `TADIR` und `E071` ohne Eintrag `Z_KOI_ZOBJ_NUTZUNG`, also keine Sperre.

25.11. 06:30, eigene Sitzung `/app/con[0]/ses[1]` (Ingos `ses[0]` unberuehrt):
- Report `Z_KOI_ZOBJ_NUTZUNG` in SE38 angelegt, Typ 1 (ausfuehrbar), Titel „Nutzungsanalyse Z-Objekte (Aufraeumen, nur
  lesend)“, Paket `ZPP`.
- Neuer Workbench-Auftrag **`T76K912944`** (Aufgabe `T76K912945`), Text „Z_KOI_ZOBJ_NUTZUNG: Nutzungsanalyse Z-Objekte
  (lesend)“, Ziel P76, Inhaber KOI; vor dem Bestaetigen in `E070` gelesen. Eigenschaften gesichert.
- 06:32 Quelltext eingespielt und mit `T76K912944` gesichert. Erste Syntaxpruefung: Fehler in `rollen_lesen`
  (FOR ALL ENTRIES verlangt gleichen Typ und Laenge, `TSTC-TCODE` 20 gegen `AGR_TCODES-TCODE` 48); korrigiert, zweite
  Pruefung „keine Syntaxfehler“. Aktiviert mit Strg+F3 direkt aus dem Editor, kein Dialog „Fehler beim Aktivieren“,
  Meldung „Objekt(e) wurde(n) aktiviert“.
- Ruecklesen: `REPOSRC` R3STATE `A`, SUBC 1, UDAT 20261125 06:32:29; `TADIR` PROG, Paket ZPP, Autor KOI, CREATED_ON
  20261125; `E071` Aufgabe `T76K912945` enthaelt nur `R3TR PROG Z_KOI_ZOBJ_NUTZUNG` (gesperrt). `abap-read` gegen die
  Repo-Datei: 980 zu 980 Zeilen, nach Entfernen von CR und Leerzeichen am Zeilenende identisch (`diff` ohne Ausgabe).
- 06:40 Selektionstexte (5) gepflegt und aktiviert, ohne Auftragsabfrage (Teil von `R3TR PROG` im selben Auftrag);
  `E071` danach unveraendert nur `R3TR PROG Z_KOI_ZOBJ_NUTZUNG`, keine inaktiven Objekte (`DWINACTIV` leer).
- 06:45 Korrektur FUGR-Aenderungsdatum (Abschnitt 5, Lauf 4), gleicher Auftrag. Eigene Sitzungen `ses[1]` und `ses[2]`
  jeweils mit `zu` geschlossen; danach nur Ingos `ses[0]` offen.

## 2. Quellen in T76, gelesen 24.11. per RFC (`RunSapProbe.ps1`, DPAPI-Ablage)

### 2.1 Objektliste

- `TADIR` (DD03L, AS4LOCAL A): `PGMID, OBJECT, OBJ_NAME, DEVCLASS, AUTHOR, CREATED_ON (DATS 8, CREATIONDT), SRCSYSTEM,
  GENFLAG, DELFLAG` u. a. vorhanden. `CREATED_ON` existiert.

### 2.2 Nutzung (ST03N, SCMON, SUSG)

- Bausteine in `TFDIR`: `SWNC_COLLECTOR_GET_AGGREGATES` (Gruppe `SAPLSCSM_COLLECTOR`, RFC-faehig), `SWNC_GET_WORKLOAD_STATISTIC`,
  `SCMON_*` (z. B. `SCMON_DATA_ITERATOR_CREATE`, `SCMON_IS_ACTIVE`, `SCMON_GET_STATUS`), `SUSG_API_*` (z. B.
  `SUSG_API_DATA_ITERATOR`, `SUSG_API_EXPORT`). Alle vorhanden.
- Schnittstelle `SWNC_COLLECTOR_GET_AGGREGATES` (`FUPARAREF`): Import `COMPONENT` (SWNCHOSTNAME), `PERIODTYPE`, `PERIODSTRT`,
  optional `ASSIGNDSYS` (Vorschlag SY-SYSID), `STORAGE_TYPE` ('A'), `FACTOR` (1000); Tabelle `USERTCODE` (SWNCAGGUSERTCODE)
  und 30 weitere; Ausnahme `NO_DATA_FOUND`.
- `SWNCAGGUSERTCODE`: `TASKTYPE` (RAW 1), `ACCOUNT` (Benutzer), `ENTRY_ID` (CHAR 73), `COUNT` (DEC) u. a.
- **Aufbau `ENTRY_ID`, gemessen** am Aufruf fuer Oktober 2026 (`COMPONENT=TOTAL, PERIODTYPE=M, PERIODSTRT=20261001`,
  33'963 Zeilen): Stelle 1-40 Programm bzw. Transaktion, 41-72 Jobname (nur bei Hintergrund), Stelle 73 Typ `T`
  (Transaktion) oder `R` (Report). Belegzeilen: `ZLO03 ... T`, `ZLO_ZEIVR ... ZEICHNUNGSAENDERUNG R`, `Z_ZLO03_TURBO2 ... R`.
  39 Zeilen mit Z-Namen im Oktober; `Z_KOI_REICHW_TURBO` und `ZREICHW` kamen im Oktober nicht vor.
- Verfuegbare Aggregate (`SWNCMONIINDEX`, COMPONENT `TOTAL`): Monate M 20260801, 20260901, 20261001, 20261101; Wochen ab
  20261012; Tage 20261110 bis 20261123.
- Aufbewahrung (`SWNCCOLLPARAM`): `KEEP_MONT`/`KEEP_TMONT` meist 2, bei RELID `WA` 3 Monate. **ST03N-Monate reichen in T76
  also nur 2 bis 4 Monate zurueck.** Fuer P76 nicht gelesen (P76 nicht beruehrt); Ingo muss das dort pruefen.
- Tabelle `TABLEREC` (Tabellenstatistik) im Oktober-Aufruf: keine Zeilen, also Tabellenstatistik inaktiv; keine
  Nutzungsquelle fuer Tabellen.
- SCMON: Tabellen `SCMON_DATA` (SLICEID, SUBID, TRIGID, COUNTER), `SCMON_PROC` (TRIGID, PROGID, PROCTYPE, PROCNAME, CLASSNAME),
  `SCMON_PROG` (PROGID, OBJECT, OBJ_NAME, PROGNAME), `SCMON_SUB`, `SCMON_SLICE` (SLICEID, STARTDATE, ENDDATE), `SCMON_CONFIG`
  vorhanden (DD02L/DD03L), ohne MANDT. **In T76 leer** (`SCMON_SLICE`, `SCMON_DATA` mit Einzelfeld gegengeprueft,
  `SCMON_CONFIG` leer): SCMON ist in T76 nicht aktiv.
- SUSG: `SUSG_ADMIN` (USGID, SYSID, KIND, DATE_FROM, DATE_TO, ...), `SUSG_DATA` (USGID, SUBID, TRIGID, COUNTER, LAST_USED DATS),
  `SUSG_PROC` (USGID, TRIGID, PROGID, SUB_TYPE, SUB_NAME, CLASSNAME), `SUSG_PROG` (USGID, PROGID, OBJ_TYPE, OBJ_NAME, PROGNAME)
  vorhanden. **In T76 leer** (`SUSG_ADMIN` 0 Zeilen).
- Folge: Der SCMON/SUSG-Zweig im Report ist in T76 **ungetestet** (keine Daten). Die Verkettung `DATA.TRIGID -> PROC.PROGID ->
  PROG.OBJECT/OBJ_NAME` ist aus den Feldnamen abgeleitet (Vermutung, an Daten nicht pruefbar).

### 2.3 Verwendungsnachweis

- `CROSS` (TYPE, NAME CHAR 30, STATE, PROG, INCLUDE): Typen gemessen fuer `NAME LIKE 'Z%'` (1'526 Zeilen): `R` Report
  (SUBMIT; Beleg `R,Z_ZLO03_TURBO2,,ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W`), `T` Transaktion (Beleg
  `T,Z005_PA42_001,,Z005_CALL_Z005_PA42_001`), `F` Funktionsbaustein (Beleg `F,Z_PP_FAR,,ZXCO1U01`), dazu N, P, S, U, M, B, A
  u. a. (nicht genutzt). Feld `PROG` in den Stichproben leer.
- `WBCROSSGT` (OTYPE, NAME CHAR 120, INCLUDE): fuer `NAME LIKE 'Z%'` 23'981 Zeilen, OTYPE `TY` 12'216, `DA` 10'072, `ME` 1'692.
  NAME traegt Unterkomponenten nach `\` (`ZHLG1\TY:MNDID`, `ZCL_CA_APPL_LOG\ME:ADD_TEXT`), also bis zum ersten `\` kuerzen.
- `D010INC` (MASTER, INCLUDE): Includes und ihre Rahmenprogramme.
- DDIC: `DD03L-ROLLNAME`, `DD03L-PRECFIELD`, `DD04L-DOMNAME` (kein Schluessel), `DD26S-TABNAME/VIEWNAME` vorhanden.

### 2.4 Transaktionen, Rollen, Jobs, Aenderungsdatum

- `TSTC` (TCODE, PGMNA, DYPNO, CINFO, ...).
- `AGR_TCODES` (MANDT, AGR_NAME, TYPE, TCODE CHAR 48, ...): TYPE `TR` 134'609 Zeilen, `OT` 7'595.
- `TBTCP` (JOBNAME, JOBCOUNT, STEPCOUNT, PROGNAME, ...), `TBTCO` (JOBNAME, JOBCOUNT, STATUS, PERIODIC, SDLSTRTDT, STRTDATE,
  ENDDATE). Domaene `BTCSTATUS` ohne Festwerte; Statuswerte aus Include `LBTCHDEF` Zeilen 302-311: R laeuft, Y bereit,
  P eingeplant, S freigegeben, A abgebrochen, F beendet, Z aktiv gesetzt, X unbekannt.
- `TRDIR` (NAME, SUBC, UDAT, UNAM, CDAT, CNAM, ...): letzte Aenderung fuer PROG, Klassen-Includes, Funktionsgruppen-Includes.
- DDIC-Koepfe mit `AS4DATE` (DATS) und `AS4LOCAL`: `DD02L`, `DD04L`, `DD01L`, `DD25L`, `DD40L`, `DD30L`.
- `VRSD` bewusst nicht verwendet (Felder und Typzuordnung nicht geprueft).

## 3. Paket

Gewaehlt **`ZPP`** („Trafag Entwicklungen PPS“, Transportschicht `ZT76`, `TDEVC`): Dort liegen schon 32 der Z_KOI-Programme
(`TADIR`, darunter `Z_KOI_REICHW_TURBO`), und der Weg nach P76 ist mit ZREICHW belegt (`saptasks/zreichw/NOTES.md` Zeile 12).
`ZCA` („Eigenentwicklung anwendungsuebergreifende Komponenten“) passte inhaltlich besser, hat aber die Transportschicht `SAP`,
deren Weg nach P76 nicht belegt ist. Kein neues Paket.

## 4. Einstufungsregel (so im Report umgesetzt, FORM `zusammenfuehren`; Grundsatz: lieber „unklar“ als falsch „tot“)

Zeitraum: erster Tag des Monats, `P_MON` Monate zurueck, der laufende Monat zaehlt mit (Vorschlag 12). Die Spalte
„Zeitraum“ und der Listenkopf nennen, welche ST03N-Monate tatsaechlich gelesen wurden und ob SCMON/SUSG Daten hatten;
„tot“ heisst also immer „nicht genutzt in den gelesenen Monaten“.

1. **genutzt**: Ausfuehrungen im Zeitraum (ST03N, SCMON oder SUSG) > 0 oder ein Joblauf mit Ende im Zeitraum.
2. **indirekt**: sonst, wenn Verwender (ohne Eigenverweise) > 0, ein Job eingeplant (Status S, P, Z, Y, R)
   oder eine Rolle eine zugehoerige Transaktion enthaelt.
3. **tot**: sonst, wenn die Nutzung fuer dieses Objekt messbar ist: ausfuehrbares Programm (SUBC 1) oder Modulpool (M) und
   Transaktionen bei vorhandenen ST03N-Monaten; Include (SUBC I) ohne Rahmenprogramm in `D010INC`; jedes Codeobjekt, wenn
   SCMON/SUSG Daten haben.
4. **unklar**: sonst (Laufzeitnutzung fuer den Typ nicht messbar, z. B. CLAS, FUGR, DDIC, SICF, IWSV/IWSG ohne SCMON; oder
   gar keine ST03N-Monate vorhanden).

## 4a. Quellen je Spalte (im Report)

| Spalte | Quelle | Bemerkung |
| --- | --- | --- |
| Typ, Name, Paket, Autor, Anlagedatum | `TADIR` (`PGMID R3TR`, `DELFLAG` leer) | `CREATED_ON` bei alten Objekten leer |
| Programmart | `TRDIR-SUBC` | nur PROG |
| Letzte Aenderung, Quelle | PROG `TRDIR-UDAT`; CLAS/INTF groesstes `TRDIR-UDAT` der Includes `<Name mit = auf 30>…`; FUGR groesstes `UDAT` von `SAPL<grp>` und `L<grp>xxx`; TABL/VIEW/DTEL/DOMA/TTYP `AS4DATE` aus DD02L/DD25L/DD04L/DD01L/DD40L | TRAN, SICF, IWSV, IWSG ohne Datum |
| Transaktionen | `TSTC` (Code selbst, `PGMNA` = Programm, `PGMNA` = `SAPL<grp>`) | OO- und Parametertransaktionen (`TSTCP`) nicht ausgewertet |
| Rollen | `AGR_TCODES` Typ `TR`, verschiedene Rollen ueber alle Transaktionen des Objekts | Anmeldemandant |
| Jobs eingeplant, letzter Joblauf | `TBTCP-PROGNAME` mit `TBTCO`: eingeplant = Status S, P, Z, Y, R; Lauf = Ende bei F oder A | Status laut `LBTCHDEF` 302-311 |
| Verwender Z / fremd | `CROSS` R (SUBMIT), T (CALL TRANSACTION, zaehlt auch fuer das Programm dahinter), F (Bausteine der Gruppe ueber `TFDIR-PNAME`); `WBCROSSGT` (Name bis zum ersten `\`, fuer CLAS/INTF/TABL/VIEW/DTEL/TTYP); `D010INC` (Rahmenprogramme eines Includes); `DD03L` ROLLNAME und PRECFIELD; `DD04L` DOMNAME; `DD26S`; `TNAPR` PGNAM bis PGNAM5 (Verarbeitungsprogramme der Nachrichtensteuerung, zaehlen als fremd) | gezaehlt wird je verschiedenem Include bzw. DDIC-Objekt; Z = Name beginnt mit Z, Y, LZ, LY, SAPLZ, SAPLY, SAPMZ, SAPMY, MZ, MY; Eigenverweise (eigene Klassen- und FG-Includes, Programm hinter der eigenen Transaktion) zaehlen nicht |
| ST03N Ausfuehrungen, letzter Monat | `SWNC_COLLECTOR_GET_AGGREGATES`, COMPONENT `TOTAL`, Monate aus `SWNCMONIINDEX`, Tabelle `USERTCODE`, `ENTRY_ID` 1-40 und Stelle 73 (R/T); PROG = eigene R-Zeilen plus T-Zeilen seiner Transaktionen | nur Monat, kein Tag |
| SCMON, SUSG | Verkettung DATA → PROC → PROG, nur wenn Daten vorhanden | in T76 leer, ungetestet |

## 5. Tests in T76 (25.11., eigene Sitzung, Liste per `SapGuiT76Hilfe.vbs gridcols` in eine Datei, dann gezaehlt)

T76 ist eine Datenkopie; die Nutzungszahlen sind dort nicht aussagekraeftig, geprueft wurde die Logik.

**Lauf 1, Vorschlagswerte** (Z*, Y*, 13 Typen, 12 Monate, Zeitraum ab 2025-12-01): Laufzeit rund 6 s, 3'648 Zeilen.
Tabelle = Stand Lauf 1; der Endstand nach den Korrekturen steht unter Lauf 5 (nur PROG anders).
Gelesene ST03N-Monate 2026/08 bis 2026/11 (mehr gibt es in T76 nicht), SCMON und SUSG ohne Daten.

| Typ | genutzt | indirekt | tot | unklar | Summe |
| --- | ---: | ---: | ---: | ---: | ---: |
| CLAS | 0 | 61 | 0 | 78 | 139 |
| DOMA | 0 | 75 | 0 | 16 | 91 |
| DTEL | 0 | 240 | 0 | 44 | 284 |
| FUGR | 0 | 23 | 0 | 64 | 87 |
| INTF | 0 | 2 | 0 | 3 | 5 |
| IWSG | 0 | 0 | 0 | 1'387 | 1'387 |
| IWSV | 0 | 0 | 0 | 18 | 18 |
| PROG | 31 | 263 | 429 | 8 | 731 |
| SICF | 0 | 0 | 0 | 297 | 297 |
| TABL | 0 | 315 | 0 | 27 | 342 |
| TRAN | 25 | 120 | 57 | 0 | 202 |
| TTYP | 0 | 40 | 0 | 2 | 42 |
| VIEW | 0 | 16 | 0 | 7 | 23 |
| **Summe** | **56** | **1'155** | **486** | **1'951** | **3'648** |

PROG nach Programmart: SUBC 1 genutzt 28, indirekt 101, tot 411; SUBC I indirekt 149, tot 16 (Include ohne
Rahmenprogramm); SUBC M genutzt 3, indirekt 13, tot 2; SUBC S unklar 8. „tot“ kommt nur bei PROG und TRAN vor, wie die
Regel es will. Ohne Aenderungsdatum: IWSG, SICF, TRAN, IWSV (gewollt) und CLAS 6, siehe Abschnitt 6. In Lauf 1 fehlte es noch bei allen
87 FUGR, siehe Lauf 4.
`CREATED_ON` ist bei 1'203 Objekten leer (alte Objekte).

**Lauf 2, „nur Kandidaten“:** 2'437 Zeilen = 486 tot + 1'951 unklar aus Lauf 1. Stimmt.

**Lauf 3, Paket ZPP:** 358 Zeilen; Gegenprobe `TADIR` (R3TR, Paket ZPP, Z*/Y*, die 13 Typen, DELFLAG leer) per RFC: 358. Stimmt.

**Stichproben**

| Objekt | Ergebnis im Report | Gegenprobe |
| --- | --- | --- |
| PROG `Z_ZLO03_TURBO2` | genutzt, ST03N 106, letzter Monat 2026/11, TCode ZLO03, 9 Rollen, 1 Z-Verwender, letzter Joblauf 23.11.2026 | ST03N je Monat per RFC: R-Zeilen 50 (10/2026) + 10 + 6 + 4 (11/2026, Jobs Z_KOI_ZLO_FIX26/28/29) = 70, T-Zeilen ZLO03 8 + 26 + 2 = 36, zusammen 106. `AGR_TCODES` ZLO03 Typ TR: 9 Rollen. Verwender `CROSS R Z_ZLO03_TURBO2` aus `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W`. Jobs Z_KOI_ZLO_FIX28/29 in `TBTCP` |
| TRAN `ZLO03` | genutzt, ST03N 36, 9 Rollen | wie oben, 36 |
| PROG `Z_KOI_REICHW_TURBO` / TRAN `ZREICHW` | genutzt, ST03N 6 (2026/11), 1 Rolle | ST03N 11/2026 `ZREICHW T` 6; keine R-Zeile; `AGR_TCODES` ZREICHW 1 Rolle |
| PROG `Z_KOI_TEST2` ($TMP, 2023) | tot, letzte Aenderung 17.06.2024 | keine ST03N-Zeile in 08-11/2026, kein TCode, kein Job |
| PROG `ZCV_ERFASS_DATA` (Include) | tot, „Include ohne Rahmenprogramm“ | `D010INC` ohne Eintrag; das naheliegende Rahmenprogramm `ZCV_ERFASSUNGAUF` bindet nur `ZCV_ERFASSUNGAUF_F01/_S01/_TOP` ein |
| CLAS `ZCL_ZPOWERBI_EINKAUF_DPC_EXT` | unklar, 0 Verwender, letzte Aenderung 23.11.2026 (TRDIR-Incl) | richtig im Sinn der Regel: Gateway-Klassen werden ueber Konfiguration gerufen, nicht ueber Verwendungsnachweis; ohne SCMON nicht messbar |
| PROG `Z_KOI_ZOBJ_NUTZUNG` | tot | neu angelegt, noch nie in ST03N; zeigt, dass frisch angelegte Objekte als tot erscheinen (Anlagedatum beachten) |

**Korrektur und Lauf 4 (06:45):** In Lauf 1 war „Letzte Aenderung“ bei allen 87 Funktionsgruppen leer, obwohl `TRDIR`
`SAPLZ005` (20190428) und `LZ005*` hat. Ursache im eigenen Report: Die aus `S_NAME` abgeleiteten Bereiche `gr_sapl`/`gr_linc`
bekamen auch bei Option CP ein gefuelltes HIGH (`'SAPL'`); damit lieferte der `TRDIR`-Zugriff keine FG-Programme (genaue
Wirkung von HIGH bei CP in Open SQL nicht untersucht, Vermutung). Korrigiert: HIGH nur bei BT/NB. Neu gesichert in
`T76K912944`, Syntax ohne Fehler, aktiviert, `abap-read` 984 zu 984 Zeilen identisch mit der Repo-Datei, `REPOSRC` nur
Version A (06:45:16). Lauf 4 mit Vorschlagswerten: 3'648 Zeilen, Einstufung und uebrige Spalten zeilengleich mit Lauf 1
(`diff` der ganzen Zeilen ohne Aenderungsdatum und dessen Quelle: 0 Abweichungen); alle 87 FUGR haben jetzt ein Datum (Quelle TRDIR-FG), z. B.
`Z005` 28.04.2019 = groesstes Datum aus `SAPLZ005` 20190428 und `LZ005*` 20020925.

**Korrektur und Lauf 5 (06:53), Pruefung auf falsch „tot“:** Von den 411 toten ausfuehrbaren Programmen aus Lauf 4 standen
11 als Verarbeitungsprogramm in `TNAPR` (88 Zeilen mit PGNAM Z*/Y*, 14 verschiedene Programme), z. B. `Z_RVADOR01`,
`Z_SDPACKDR_00`, `ZTRIM_SEND_MAIL_DISPO`. Sie werden aus dem Standard per `PERFORM ... IN PROGRAM` gerufen und stehen weder
in ST03N noch in `CROSS`. `TNAPR` (PGNAM bis PGNAM5) ist jetzt Verwenderquelle. Neu gesichert in `T76K912944`, Syntax ohne
Fehler, aktiviert, `abap-read` 999 zu 999 Zeilen identisch, `REPOSRC` nur Version A (06:53:51), `E071` weiter nur
`R3TR PROG Z_KOI_ZOBJ_NUTZUNG`. Lauf 5: 15 Zeilen anders als Lauf 4, alle jetzt „indirekt“ (PROG tot 429 -> 416, unklar
8 -> 7, indirekt 263 -> 277); Ueberschneidung `TNAPR` mit „tot“ danach 0. Stichprobe `Z_RVADOR01`: indirekt, 16 fremde
Verwender (TNAPR-Zeilen). Stand nach Lauf 5: genutzt 56, indirekt 1'169, tot 473, unklar 1'950 (Summe 3'648); alle
anderen Typen wie in der Tabelle oben.

ST22: keine Kurzdumps am 25.11. nach den Laeufen.

## 6. Grenzen

- **ST03N-Aufbewahrung:** In T76 gibt es nur die Monate 2026/08 bis 2026/11 (`SWNCCOLLPARAM` KEEP_MONT 2, RELID WA 3).
  Ist es in P76 aehnlich, heisst „tot“ nur „in den letzten 2 bis 4 Monaten nicht gestartet“; Quartals- und
  Jahresabschlussreports erscheinen dann tot. Der Report zeigt die gelesenen Monate im Kopf und in der Spalte „Zeitraum“.
- **ST03N sieht nur Einstiege:** Reports (R) und Transaktionen (T). Klassen, Funktionsgruppen, DDIC, SICF und
  Gateway-Dienste bekommen keine Nutzungszahl; ohne SCMON/SUSG bleiben sie bei fehlenden Verwendern „unklar“.
- **Dynamische Aufrufe** (`SUBMIT (lv_prog)`, `CALL FUNCTION lv_fb`, `CREATE OBJECT TYPE (lv_cls)`), Aufrufe ueber
  Konfiguration (BAdI, Erweiterungen, CMOD-Exits, Gateway-DPC/MPC, Formulare, Nachrichtenfindung) und Zugriffe von aussen
  (RFC, `RFC_READ_TABLE`, SE16) stehen nicht im Verwendungsnachweis.
- **Weitere Aufrufe ueber Customizing** (z. B. Formularroutinen in VOFM, Business Transaction Events, Smart Forms,
  Druckprogramme ausserhalb `TNAPR`) sind nicht als Quelle eingebaut; nur `TNAPR` ist geprueft und aufgenommen.
- **Ketten** werden nicht aufgeloest: eine Tabelle, die nur ein totes Programm verwendet, ist „indirekt“.
- **Parameter-, Varianten- und OO-Transaktionen** (`TSTCP`, OO-Transaktion auf Klasse) werden dem Programm bzw. der Klasse
  dahinter nicht zugeordnet; ST03N fuehrt sie unter ihrem eigenen Code (eigene TRAN-Zeile).
- **Modulpools `SAPMZ*`/`SAPMY*`** fallen nicht unter die Vorschlagsmuster Z*/Y*; bei Bedarf im Namensmuster ergaenzen.
- **Letzte Aenderung CLAS:** bei 6 Klassen leer (keine Includes in `TRDIR` gefunden; Ursache nicht untersucht).
- **SCMON/SUSG-Zweig ungetestet**, weil T76 keine Daten hat; die Verkettung ist aus den Feldnamen abgeleitet (Vermutung).
- Zaehlung SUSG ueber mehrere Sammlungen (USGID) kann doppelt zaehlen; aussagekraeftig ist „zuletzt benutzt“.
- Rollen zaehlen im Anmeldemandanten (in P76 der produktive Mandant).

## 7. Was Ingo fuer P76 tun muss

1. Transport `T76K912944` (Aufgabe `T76K912945`, nur `R3TR PROG Z_KOI_ZOBJ_NUTZUNG` samt Texten) freigeben und nach P76
   importieren.
2. In P76 vorher oder nachher pruefen, wie viele ST03N-Monate es gibt (ST03N bzw. `SWNCMONIINDEX` COMPONENT TOTAL,
   Periodentyp M) und wie lange sie aufbewahrt werden (`SWNCCOLLPARAM`); bei Bedarf mit Basis die Aufbewahrung der
   Monatsaggregate verlaengern.
3. Report einmal mit Vorschlagswerten ausfuehren (in T76 rund 6 s; in P76 bei Bedarf als Hintergrundjob), Liste nach Excel
   exportieren und ablegen.
4. Mit Basis klaeren, ob SCMON (oder die SUSG-Verdichtung) in P76 aktiv ist oder eingeschaltet werden soll; erst damit
   werden CLAS, FUGR und Includes messbar. Nach einigen Monaten Laufzeit den Report erneut ausfuehren.
5. Der Report setzt nichts um. Loeschentscheide trifft Ingo je Objekt.

*Erledigt 2026-11-25 13:49:* Punkt 1 (Import, siehe Kopf).

## 8. P76 gelesen 2026-11-26 14:00–14:15 (Agent sap-t76, eigene SE16-Sitzung, nur lesen; von der Hauptsitzung an den Rohdateien nachgeprueft)

*Erledigt 2026-11-26 14:15:* Punkte 2 und 4 aus Abschnitt 7 geprueft.

- **ST03N:** `SWNCMONIINDEX` COMPONENT TOTAL, PERIODTYPE M: 4 Monate **08/2026 bis 11/2026** (November laufend, bis 25.11.), kein aelterer
  Monat (auch ohne COMPONENT-Filter). `SWNCCOLLPARAM` 33 Zeilen, Zeile fuer Zeile gleich wie T76 (KEEP_MONT 2, WA 3). „Tot“ heisst in
  P76 also nur: **seit dem 01.08.2026 nicht gestartet**. KEEP_MONT 2 ergibt trotzdem 4 Monate im Index; Monatszahl immer aus
  `SWNCMONIINDEX` lesen, nicht aus dem Parameter ableiten.
- **SCMON:** `SCMON_SLICE` 3 Zeilen, 06., 07. und 08.08.2024, seither keine; `SCMON_CONFIG` 0 Zeilen. SCMON lief offenbar einmal drei
  Tage und ist seither aus (Status in Transaktion SCMON nicht angesehen, Vermutung). `SCMON_DATA`/`RDATA` bewusst nicht geoeffnet.
- **SUSG:** `SUSG_ADMIN` 0 Zeilen, keine Verdichtung.
- In P76 nichts geaendert, keine Sicherheitsabfrage; SE16 hat vermutlich `/1BCDWB/DB<Tabelle>` erzeugt (belegt fuer `SCMON_CONFIG`).

**Entscheid Ingo 2026-11-26 (Vorschlag der Hauptsitzung, „ja mach das“):**
1. Basis: SCMON in P76 dauerhaft einschalten und die Aufbewahrung der ST03N-Monatswerte verlaengern (Vorschlag 24 Monate); Mailtext an
   die Basis im Chat (Empfaenger offen). Erst dann werden Quartals- und Jahresabschlussreports und die rund 1'950 „unklar“ (CLAS, FUGR,
   Includes) messbar.
2. Report jetzt einmal in P76 ausfuehren, Liste nach Downloads; belastbar ist nur „genutzt“ (bleibt sicher), „tot“ ist Kandidat.
3. **Geloescht wird erst nach einem Jahreswechsel mit SCMON-Daten, fruehestens Anfang 2027**; bis dahin nur Objekte, die Ingo ohnehin
   kennt (z. B. alte Tests in `$TMP`), und auch die nur auf Entscheid je Objekt.

## 9. Erster Lauf in P76 (2026-11-26 14:04, Agent sap-t76, Freigabe Ingo; von der Hauptsitzung an der Datei nachgeprueft)

*Erledigt 2026-11-26 14:10:* Punkt 3 aus Abschnitt 7.

- Dialoglauf SA38 mit Vorschlagswerten (Z*/Y*, 13 Typen, 12 Monate), rund 6 s, ohne Abbruch, Popup oder Sicherheitsabfrage; eigene
  Sitzung ueber `SapGuiP76Hilfe.vbs` (`P76Lesen.vbs` laesst SA38 nicht zu, Sperre nicht angetastet), geschlossen mit `ok <n> /i`.
  Liste mit `SapGuiGridAusgeben.vbs` vollstaendig ausgelesen (244 s), kein `%PC`. Nebeneffekt: SA38 hat den Programmnamen als
  letzten Wert vorbelegt (Benutzerparameter).
- **Ablage:** `Downloads\Z_Objekte_Nutzung_P76_2026-11-26.txt` (Tab, 3'379 Zeilen x 23 Spalten, 4 Kopfzeilen) und `.xlsx`
  (Blaetter Liste, Kopf, Zaehlung; aus der Textdatei gebaut). Nicht im Repo.
- **Zeitraum laut Report:** ST03N 2026/08–2026/11, SCMON keine Daten, SUSG keine Daten.
- **Ergebnis:** 3'379 Objekte: **genutzt 175, indirekt 1'012, tot 304, unklar 1'888**. „tot“ nur bei PROG (252) und TRAN (52);
  unklar vor allem IWSG 1'370, SICF 291, FUGR 63, CLAS 52. PROG ausfuehrbar: 81 genutzt, 56 indirekt, 239 tot.
- **Stichproben:** `Z_ZLO03_TURBO2`/`ZLO03` genutzt (ST03N 2'966, 9 Rollen); `ZREICHW` genutzt (5); `Z_RVADOR01` indirekt (16 TNAPR-Verwender);
  in `$TMP` nur 2 tote (`Z_TEST_LF`, `Z_VC_BATCH_2`); `Z_KOI_ZOBJ_NUTZUNG` selbst tot (erst seit 25.11. vorhanden).
- **Bewertung:** belastbar ist „genutzt“ (bleibt); die 304 „tot“ sind Kandidaten fuer „seit August 2026 nicht gestartet“, keine
  Loeschliste. Naechster Schritt Ingo (Admin): SCMON in P76 dauerhaft einschalten und Aufbewahrung der ST03N-Monate verlaengern,
  dann den Report nach dem Jahreswechsel erneut laufen lassen.
