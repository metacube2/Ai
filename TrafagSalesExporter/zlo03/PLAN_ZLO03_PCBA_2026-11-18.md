# ZLO03 mit PCBA-Stuecklisten (Verwendung 9): Plan fuer eine Kopie mit neuer Transaktion

Stand: 2026-11-18, Phase 1 (nur lesend in T76/100, eigene GUI-Sitzung `/app/con[0]/ses[1]`, kein RFC, kein Passwort).
Nichts in SAP geschrieben, keine Datei im Repository geaendert. Das Original `ZLO03` / `Z_ZLO03_TURBO2` bleibt unveraendert.

Anlass: Wunsch aus dem Projektmanagement (Mail 18.11.): In ZLO03 soll auch die Stuecklistenverwendung 9 „PCBA
Stuecklisten“ ausgewaehlt werden koennen, weil PCBA-Stuecklisten weitere Einkaufsteile enthalten (Beispiel C31804
Erdungslitze). Entscheid Ingo: Kopie, neue Transaktion.

Kennzeichnung: **[B]** = im System gelesen (Objekt, Tabelle, Feld, Wert genannt), **[R]** = im Repository belegt
(Datei:Zeile), **[V]** = Vermutung, nicht belegt.

---

## 1. Ergebnis in fuenf Saetzen

1. Die Verwendung 9 kommt heute in `ZPOWERBI_VC_TXT` und `ZPOWERBI_VC` nicht vor: beide Generatoren loesen mit der
   Anwendung `PP01` auf, und `PP01` sucht nur Verwendung 1 und danach 3 (TC04/TCS41). Materialien, die nur eine
   Verwendung-9-Stueckliste haben, stehen als Blatt in der Tabelle; ihr PCBA-Inhalt fehlt in ZLO03 vollstaendig.
2. In T76 gibt es 881 Verwendung-9-Stuecklisten zu 814 Materialien; 808 davon haben **kein Werk** (Gruppenstueckliste),
   73 Werk 1100. 660 der 814 Materialien haben in Werk 1100 keine Stueckliste mit Verwendung 1 oder 3, sind also heute
   Blaetter; 154 haben beides.
3. Zum Beispiel C31804: In T76 ist C31804 **Kopf** einer Verwendung-9-Stueckliste (C34857 Presskabelschuh, C31747 Litze),
   als Komponente steht es nur in Verwendung-3-Stuecklisten. Der Testfall der Mail passt also nur in dieser Lesart; sie
   ist als Entscheid E1 offen.
4. Das groesste fachliche Risiko ist „Exklusiv = X“: ZLO03 setzt jede Komponente zuerst auf „X“ und nimmt es nur zurueck,
   wenn `ZPOWERBI_VC_TXT` eine andere aktive FERT-VKNR liefert. Eine Komponente, die nur ueber Verwendung 9
   hinzukommt, haette dort keine Zeile und wuerde faelschlich exklusiv. Die Kopie muss deshalb an **allen** Lesestellen
   auch die Rueckwaertssicht ueber Verwendung 9 bekommen, nicht nur im Top-Down.
5. Empfehlung: Variante A (die Kopie loest die Verwendung-9-Stuecklisten je Lauf selbst auf, mit `CS_BOM_EXPL_MAT_V2`
   und Anwendung `PCBA`, erst Werk 1100, sonst ohne Werk) und haengt das Ergebnis an die Lesestellen: Von den 9
   SELECTs auf `ZPOWERBI_VC_TXT` brauchen 6 die Ergaenzung, dazu der FIX-26-Block (zusammen 7 Einhaengestellen); die
   zwei Text-SELECTs in `fill_ktab` liefern fuer PCBA-Textpositionen nur den Ersatztext, `ausgabe_elternmaterial` wird
   nicht aufgerufen. Keine neue
   Tabelle, kein Job, kein Customizing, kein Eingriff in die Generatoren. Laufzeit und Vollstaendigkeit werden in T76
   gemessen; reicht die Laufzeit nicht, ist Variante B2 (kleine Nachttabelle nur fuer Verwendung 9) der Ausweg.

---

## 2. Ist-Befund mit Belegen

### 2.1 Generatoren: welche Verwendung, welche Startmaterialien, welches Ziel

| Punkt | Befund | Beleg |
| --- | --- | --- |
| Programme | `Z_VC_ZLO03` (590 Zeilen) schreibt `ZPOWERBI_VC` und `ZSTULIMAT_VC`; `Z_VC_ZLO03_TXT` (670 Zeilen) schreibt `ZAT_VC_TXT` und `ZSTULIMAT_VC`; beide `REPORT z_vc_batch.` | [B] T76-Quelltext per SE38 gelesen (Kopie im Scratchpad `Z_VC_ZLO03*.abap`); deckt sich mit [R] `zlo03/BEFUND_SYSTEMABGLEICH_2026-08-03.md` 3 |
| Aufruf | `CS_BOM_EXPL_MAT_V2` mit `capid = 'PP01'`, `datuv = sy-datum`, `mehrs = 'X'`, `werks = '1100'`, `emeng = 1`; `delnl = 'X'` (Z_VC_ZLO03) bzw. `' '` (Z_VC_ZLO03_TXT, FIX 7); **kein STLAN-Parameter** | [B] Z_VC_ZLO03 Zeilen 258-266; Z_VC_ZLO03_TXT Zeilen 285-306 |
| Startmaterialien | jedes Material aus `P_MATNR` (MARA); ohne Selektion nur der Bereich `'000000000000000001'` bis `'999999999999999999'` | [B] Z_VC_ZLO03_TXT Zeilen 85-112 |
| Variante `ALLES` | beide Programme: `P_MATNR` I BT `000000000000000001` bis `Z410` (MATN1), `P_OPTION` = `VC3`; damit auch alphanumerische Kopfmaterialien (passt zu Koepfen wie D50508 in der Tabelle) | [B] SE38 > Varianten > ALLES > Werte, beide Programme; VARID: je eine Variante `ALLES` |
| Ziel | `ZAT_VC_TXT` (TXT-Generator), nicht `ZPOWERBI_VC_TXT`. `ZPOWERBI_VC_TXT` ist in T76 eine Kopie von `ZPOWERBI_VC` (`Z_KOI_VC_TXT_KOPIE`, Oktober) | [B] Quelltext Zeilen 48, 537; [R] `zlo03/Z_KOI_VC_TXT_KOPIE.abap`, `BEFUND_EINHEIT_WAFER_2026-11-05.md` 4.1 |
| Spalte Verwendung | `ZPOWERBI_VC_TXT` hat kein STLAN-Feld | [R] `BEFUND_EINHEIT_WAFER_2026-11-05.md` 3.3 (DD03L-Liste) |
| Zeilen T76 | `ZPOWERBI_VC_TXT` 1'908'396 Zeilen | [B] SE16N Anzahl |
| Einplanung T76 | kein Job mit Name `ZLO03*` und kein Schritt mit `Z_VC_ZLO03*` oder `Z_LO03_MD04*` (TBTCO hat mindestens 99'999 Saetze, also lesbar) | [B] SE16N TBTCO/TBTCP |
| Einplanung P76 | `ZLO03_MATERIALISIEREN_ALLES` 01:00, etwa 4 h 43; `ZLO03_PRECALC` 06:00, etwa 32 min (SM37-Liste vom 13.10.) | [R] `zlo03/TAGESZEIT_ABHAENGIGKEIT_2026-10-13.md` 2. Welche Programme diese Jobs in P76 starten und welches Programm dort `ZPOWERBI_VC_TXT` fuellt, ist **nicht geprueft** (P76 ausgeschlossen) |

**Antwort auf Frage 1:** Eine Stueckliste mit Verwendung 9 kommt in `ZPOWERBI_VC_TXT` / `ZPOWERBI_VC` nicht vor, weil
`PP01` sie nie auswaehlt (2.2). Das gilt fuer die T76-Generatoren und die T76-Tabelle [B]. Fuer P76 ist es eine
**Vermutung** [V]: dort ist weder der Generator noch die Tabelle gelesen; der Befund C42698 als Blatt in VKNR 45726
(`BEFUND_EINHEIT_WAFER` 3.4) stammt ebenfalls aus T76.

### 2.2 Customizing der Verwendung 9 und der Anwendungen

| Tabelle | Schluessel | Werte | Beleg |
| --- | --- | --- | --- |
| T416 | STLAN 9 | PMPFE `.`, PMPKO `+`, PMPKA `.`, PMPRV `-`, PMPVS `-`, PMPIN `-`, PMPER `.`; Text „PCBA Stuecklisten“ | [B] SE16N T416 |
| T416 | STLAN 1 / 3 | 1 „Fertigung“ (PMPFE `+`), 3 „universal“ (alles `.` bzw. `-`) | [B] SE16N T416 |
| T416T | DE, 9 | „PCBA Stuecklisten“ | [B] SE16N T416T |
| TC04 | CAPID `PP01` | CSLID `01`, „Fertigung - allgemein“ | [B] SE16N TC04 |
| TC04 | CAPID `PCBA` | CSLID `99`, „PCBA Stuecklisten“ (Anwendung existiert schon) | [B] SE16N TC04 |
| TCS41 | CSLID 01 | Prio 1 STLAN 1, Prio 2 STLAN 3 | [B] SE16N TCS41 |
| TCS41 | CSLID 99 | Prio 1 STLAN 9 (sonst nichts) | [B] SE16N TCS41 |

Bedeutung der T416-Zeichen (`+` muss, `.` kann, `-` darf nicht) ist SAP-Standard, hier nicht nachgelesen [V].
Folge: Mit `PCBA` findet `CS_BOM_EXPL_MAT_V2` **nur** Verwendung 9; ein Geraet (Verwendung 3) hat damit keine
Stueckliste. Eine gemischte Aufloesung 1/3/9 in einem Aufruf ginge nur mit neuem Customizing (eigene Anwendung mit
Auswahl-ID 1, 3, 9); das ist im Plan nicht vorgesehen.

**Werk:** CS12 in T76 (nur Anzeige, eigene Sitzung) [B]:

| Material | Werk | Anwendung | Ergebnis |
| --- | --- | --- | --- |
| C31804 (Verw. 9 ohne Werk) | 1100 | PCBA | „Zu C31804/1100/PCBA ist keine Stueckliste vorhanden“ |
| C31804 | leer | PCBA | C34857 1 ST (Pos. 0010), C31747 1 ST (Pos. 0020) |
| C27940 (Verw. 9 nur Werk 1100) | leer | PCBA | „Zu C27940//PCBA ist keine Stueckliste vorhanden“ |
| C27940 | 1100 | PCBA | H10199 52.5 MM |
| 53707 | 1100 | PP01 | 29 Zeilen, C31804 auf Stufe 1, Pos. 0360, ohne Unterstruktur |

Regel fuer die Kopie: je Verwendung-9-Kopf erst mit Werk 1100, und nur wenn dort nichts gefunden wird, ohne Werk
aufloesen. Dass `CS_BOM_EXPL_MAT_V2` sich wie CS12 verhaelt, ist plausibel, aber erst im Test T6 belegt [V].
Nach den CS12-Proben wurde das Einstiegsbild auf `PP01` / Werk 1100 zurueckgestellt (Benutzerparameter der Sitzung).

### 2.3 Mengengeruest Verwendung 9 in T76

| Messung | Wert | Beleg |
| --- | --- | --- |
| MAST mit STLAN 9 | 881 Zeilen, 814 Materialien | [B] SE16N MAST, Grid vollstaendig gelesen |
| davon ohne Werk / Werk 1100 | 808 / 73 | [B] |
| Materialien nur ohne Werk / nur 1100 / beides | 741 / 6 / 67 (die 67 haben verschiedene STLNR je Werk) | [B] |
| davon mit Verw. 1 oder 3 in Werk 1100 | 154; ohne: 660, davon 1 ungeprueft (PB59130, Praefix P nicht gelesen) | [B] Abgleich mit MAST Werk 1100 der Praefixe A, B, C, D, E, Q, R, V (11'908 Zeilen) |
| Praefixe der 814 Materialien | B 611, C 104, R 30, Q 22, D 16, E 14, V 12, A 4, P 1 | [B] aus dem vollstaendigen zweiten Grid-Lauf |
| MAST Werk 1100 STLAN 1 / 3 | 17 / 50'303 | [B] SE16N Anzahl |
| STKO mit BMENG <> 1 (STLTY M) | 6 Stuecklisten, keine davon Verwendung 9 | [B] SE16N STKO BMENG 1.001 bis 999999999 und 0.001 bis 0.999 |
| Stichprobe 20 Verwendung-9-Koepfe | siehe 2.6 | [B] |

### 2.4 C31804 und die Beispiele

| Punkt | Befund | Beleg |
| --- | --- | --- |
| Stammdaten | MARA HALB, ST, ohne Loeschvormerkung; MAKT „ERDUNGSLITZE 9X5“; MARC 1100 BESKZ `F`, DISPO `003` | [B] |
| eigene Stueckliste | MAST C31804, Werk leer, STLAN 9, STLNR 00038057, Alt. 1; STKO BMENG 1 ST, STLST 1, gueltig ab 24.01.2020 | [B] |
| Positionen 00038057 | 0010 C34857 1 ST (L), 0020 C31747 1 ST (L) | [B] STPO |
| C34857 | MARA HALB, ST, MAKT „PRESSKABELSCHU M2.5 - 9X5“, **kein MARC in Werk 1100**, 0 Zeilen in `ZPOWERBI_VC_TXT` | [B] |
| C31747 | MARA HALB, ST, MAKT „LITZE GELB/GRUEN, 9X5; L=45MM“, MARC 1100 BESKZ `F`, DISPO `003`, 0 Zeilen in `ZPOWERBI_VC_TXT` | [B] |
| C31804 als Komponente | 11 STPO-Zeilen, je 1 ST (L), alle in Stuecklisten mit **STLAN 3, Werk 1100**: 53707, 53709, 53710, D50508, D50522, D50538, D50539, D50540, D50552, D50568, D50582; **in keiner Verwendung-9-Stueckliste** | [B] STPO IDNRK, MAST je STLNR |
| in `ZPOWERBI_VC_TXT` | KOMPNR C31804: 221 Zeilen zu 221 verschiedenen Kopfmaterialien (11 auf Stufe 01, 210 auf Stufe 02, Eltern D50538/D50552/D50568/D50539), alle 1.000 ST, MATERIALART HALB, BAUGRUPPE leer; KOM_MSTAE C31804: **0 Zeilen** (nichts darunter) | [B] SE16N |
| Testgeraete | 56861: FERT, ohne Loeschvormerkung, Status leer, enthaelt C31804 ueber D50568 (Stufe 02). 53707/53709/53710: FERT, Status 99; 55555: FERT mit Loeschvormerkung (ungeeignet) | [B] MARA |
| B63485 (beide Verwendungen) | Verw. 9 ohne Werk, STLNR 00021070: 27 Positionen (B63480, VS0019, E13178 2 ST, ... E11155 1 ST ...); Verw. 3 Werk 1100, STLNR 00022234: **1 Position E11155 1 ST**; MARC 1100 BESKZ `F` | [B] STPO, MAST, MARC |

### 2.5 Wo Z_ZLO03_TURBO2 die Stuecklisten liest (T76 = Repo-Stand FIX 28)

T76-Quelltext `Z_ZLO03_TURBO2` (4'433 Zeilen, per SE38 gelesen) ist zeilengleich mit
`zlo03/Z_ZLO03_TURBO2_FIX28_2026-11-12.abap` (`diff` Exitcode 0 nach Entfernen der BOM am Dateianfang) [B].
Zeilennummern unten beziehen sich auf diese Datei [R].

| Zeile | Stelle | Was gelesen wird | Bedeutung fuer Verwendung 9 |
| --- | --- | --- | --- |
| 743-749 | `process_main` Top-Down | `ZPOWERBI_VC_TXT` WHERE matnr = VKNR | PCBA-Inhalt fehlt (Blatt) |
| 751-757 | `process_main` Bottom-Up | `ZPOWERBI_VC_TXT` WHERE kompnr = Komponente | Komponente nur in PCBA: „Keine Daten gefunden“ |
| 870-889 | FIX 26 | Basismenge des Elternmaterials ueber `load_bmeng` | `load_bmeng` (2097-2106) liest nur `werks = gc_werks`; Gruppenstuecklisten (808 von 881) findet es nie |
| 1186-1191 | `load_stammdaten_cache` | MAST `werks = '1100' AND stlan = '1'` fuer `has_bom` | eigener Befund, siehe 2.7 |
| 1495-1499 | `load_exclusivity_topdown` | alle VKNR je Komponente | **Exklusiv-Vorgabe `X` (1548)**, zurueck nur bei anderer aktiver FERT (1551-1570) |
| 1628-1632 | `load_global_exclusivity` | Anzahl VKNR je Komponente (Bottom-Up) | zaehlt PCBA-Wege nicht |
| 1696, 1765 | `fill_ktab` | Text fuer Textpositionen | nur Text |
| 2503-2507 | `ermittle_weitere_vknr` | weitere VKNR je Komponente | PCBA-Wege fehlen |
| 2710 | `ausgabe_elternmaterial` | nicht aufgerufen (534 auskommentiert) | keine |
| 3381-3386 | `load_elternmaterial_cache` | Eltern je Komponente | PCBA-Kopf als Elternmaterial fehlt |

`ZPOWERBI_VC_TXT` hat kein Verwendungsfeld; ein Filter „STLAN“ auf dem Selektionsbild koennte also nichts filtern.

**Exklusiv-Logik [R] 1547-1580:** `lv_is_exklusiv = 'X'`, dann je Verwendung in einer FERT-VKNR ausserhalb der
Selektion: Status 99 ueberspringen, sonst `''`. Eine Komponente ohne jede Zeile in `ZPOWERBI_VC_TXT` bleibt `X`.
Rechenbeispiel (Variante „nur Top-Down erweitern“, falsch): Top-Down 56861 mit PCBA zeigt C31747. Die Exklusivpruefung
liest `ZPOWERBI_VC_TXT` mit KOMPNR C31747: 0 Zeilen, also `X`. Tatsaechlich steckt C31747 in C31804, und C31804 steht in
221 Kopfmaterialien, darunter 56935, 56936, 56937 (FERT, aktiv, Status leer [B]). Ein Kontrakt fuer C31747
waere nach dieser Liste kuendbar, obwohl das Teil weiter gebraucht wird. Richtig ist: C31747 erbt die Verwendungen von
C31804, Exklusiv leer.

### 2.5a Objekte, Namen, Transporte, Berechtigung (Frage 5)

| Punkt | Befund | Beleg |
| --- | --- | --- |
| Transaktion ZLO03 | TSTC: PGMNA `Z_ZLO03_TURBO2`, DYPNO 1000, CINFO 84; SE93: „Reporttransaktion“, Start ohne Variante, Klassifikation Professional User Transaction, GUI-Faehigkeit (HTML, Java, Windows) alle aus, Text „§“ | [B] SE16N TSTC, SE93 Anzeige |
| Parameter | TSTCP: kein Eintrag fuer ZLO03 (nur ZLO_WE_ADMIN hat einen) | [B] |
| Berechtigungsobjekt | TSTCA: ZLO03 `S_TCODE` / `TCD` = `ZV00` (ebenso ZLO01, ZLO02) | [B] |
| Pakete | PROG `Z_ZLO03_TURBO2` Paket `ZPP`; TRAN `ZLO03` Paket `ZLO1`; TDEVC: beide Transportschicht `ZT76` | [B] TADIR, TDEVC |
| Programmattribute | TRDIR `Z_ZLO03_TURBO2`: SUBC 1 (ausfuehrbar), FIXPT X, UCCHECK X, SECU leer (keine Berechtigungsgruppe), RLOAD DE, VERN 000067, zuletzt 12.11.2026 13:33:53 | [B] |
| Bestandteile | keine `INCLUDE`, kein `SET PF-STATUS`/`SET TITLEBAR`, kein `CALL SCREEN`; 24 Textsymbole, 9 Selektionstexte; VARID: eine Variante `8474_8475` | [R] FIX-28-Datei (grep); [B] SE38 Textelemente, VARID |
| Sperren | E071: 61 Zeilen fuer `Z_ZLO03_TURBO2` (bis `T76K912902/903`), 9 fuer TRAN `ZLO03`; **keine** davon in einem der 147 offenen Auftraege (E070 Status D, Nummern T76K911414 bis T76K912999). Offen ist u. a. ein fremder Auftrag `T76K912920` vom 18.11. (nicht verwenden) | [B] |
| freie Namen | TADIR PROG `Z_ZLO03*`: nur `Z_ZLO03FAST`, `Z_ZLO03_TURBO`, `Z_ZLO03_TURBO2`, `Z_ZLO03_VERIFY`, also `Z_ZLO03_PCBA` frei; TSTC/TADIR TRAN `ZLO*`: 15 Eintraege, `ZLO03P`, `ZLO04`, `ZLO05` frei | [B] |
| Rollen | S_TCODE ZLO03 in 9 Rollen (u. a. `Z_LO_ZLO03`, `Z_MM_LEBENSZYKLUS`, `Z_MM_LEBENSZYKLUS_AUSWERTEN`, `Z_SD_SUPAUS_RW`), S_TCODE ZV00 in 12 Rollen (u. a. `Z_LOGI_ALLES_RW`, `Z_SD_SUPAUS_RW`) | [B] AGR_1251 T76 |
| Aufrufer | Dashboard ruft fest `SUBMIT z_zlo03_turbo2` mit Memory-ID `ZZLO03_APP` | [R] `docs/abap/ZZLO_TOPDOWN_ADD.abap` 178-180 |

C31804 steht in keiner Stueckliste mit Verwendung 1 (alle 11 Verwendungen sind Verwendung 3) [B].

### 2.5b Selektionsbild (Frage 6)

Ein Radiobutton „Fertigung (1) / PCBA (9) / beide“ passt nicht: In Werk 1100 gibt es nur 17 Stuecklisten mit
Verwendung 1 gegen 50'303 mit Verwendung 3, und `PP01` nimmt 1, dann 3 [B]; „Fertigung (1)“ waere falsch beschriftet.
„Nur 9“ waere im Top-Down leer, weil Geraete keine Verwendung-9-Stueckliste haben (alle 814 Koepfe sind
alphanumerisch, keine VKNR [B]). Eine Select-Option STLAN koennte nichts filtern, weil `ZPOWERBI_VC_TXT` kein
Verwendungsfeld hat. Vorschlag deshalb ein einziges Ankreuzfeld `P_PCBA` „PCBA-Stuecklisten (Verwendung 9)
einbeziehen“, Vorgabe `X`. Wirkung: Top-Down zeigt unter jedem PCBA-Kopf dessen Verwendung-9-Inhalt, Bottom-Up findet
Geraete auch ueber PCBA-Koepfe, Exklusiv und „Weitere VKNR“ beruecksichtigen die PCBA-Wege. Ohne Haken rechnet die Kopie
wie ZLO03.

### 2.6 Stichprobe 20 Verwendung-9-Koepfe (nur Verw. 9, ohne Werk)

Ausgewaehlt: 16 Zufallskoepfe aus den 660 Materialien mit Verwendung 9 ohne Werk und ohne Verw. 1/3 in 1100, dazu
B63491, B64404, B53157, C42698 (aus fruehen Befunden). Gelesen je Kopf STPO (alle Zeilen der STLNR, also inklusive
aelterer Aenderungsstaende; obere Grenze der Positionszahl) und Anzahl Zeilen mit KOMPNR = Kopf in `ZPOWERBI_VC_TXT` [B].

| Kopf | STLNR | STPO-Zeilen | VC_TXT-Zeilen (Kopf als Komponente) |
| --- | --- | ---: | ---: |
| C23322 | 00024850 | 3 | 13 |
| B62540 | 00021250 | 15 | 4 |
| B52825 | 00046262 | 4 | 1 |
| B66643 | 00036915 | 1 | 0 |
| B53147 | 00041398 | 22 | 0 |
| C32509 | 00036571 | 2 | 0 |
| B53503 | 00037144 | 0 | 0 |
| B54641 | 00022678 | 27 | 0 |
| B55002 | 00030282 | 18 | 15 |
| B61206 | 00021212 | 40 | 31 |
| C18060 | 00021128 | 20 | 0 |
| QB53042 | 00037809 | 42 | 0 |
| B53136 | 00041843 | 39 | 0 |
| B53121 | 00034747 | 42 | 0 |
| B24382 | 00025378 | 5 | 240 |
| B53044 | 00041942 | 44 | 0 |
| B63491 | 00021071 | 40 | 251 |
| B64404 | 00021107 | 40 | 12 |
| B53157 | 00043579 | 38 | 0 |
| C42698 | 00045701 | 24 | 1'613 |

Ergebnis: 466 STPO-Zeilen, alle Positionstyp L, keine Textposition; 253 verschiedene Komponenten, **keine davon ist
selbst Verwendung-9-Kopf** (in der Stichprobe keine Schachtelung; die Rekursion bleibt trotzdem im Plan). 9 von 20
Koepfen stehen in `ZPOWERBI_VC_TXT` (zusammen 2'180 Zeilen), 11 nicht (Altteile ohne aktive Verwendung). Hochgerechnet
[V]: rund 23 Positionen je Kopf, also rund 19'000 Inhaltszeilen fuer alle 814 Materialien; das ist im Speicher klein.
Laufzeit der rund 814 Bausteinaufrufe ist nicht gemessen [V], Test T9.

**Exklusiv-Pflichtfall (Grundlage fuer T7) [B]:** Von den 62 Komponenten der Koepfe B63491 und C42698 stehen 47 in
keiner Zeile von `ZPOWERBI_VC_TXT` (zum Beispiel E00881, E06849, E13157, E13201), 15 haben Zeilen. E13185 steht mit
**4 ST** in B63491 (STLNR 00021071) und direkt nur in 5 Zeilen von `ZPOWERBI_VC_TXT`, alle auf Stufe 01 unter
B61283, B61284, B61285, B61288, B61289 (1 bzw. 3 ST). B63491 selbst steht in 251 Kopfmaterialien. Im Original kommt
E13185 in keinem Geraet-Top-Down vor; in der Kopie erscheint es unter jedem Geraet mit B63491. Ohne Rueckwaertssicht
ueber Verwendung 9 wuerde die Exklusivpruefung nur B6128x finden (keine FERT laut Namensmuster [V]) und **X**
setzen. Ob unter den 251 Kopfmaterialien von B63491 aktive FERT sind, ist nur fuer 3 gelesen: 56729, 55081, 54956
sind FERT mit Status 99 (zaehlen also nicht) [B]. Vor T7 in Phase 2 alle 251 per MARA pruefen. Sind alle Status 99
oder geloescht, ist „X“ fuer E13185 richtig, und T7 braucht einen anderen Fall (dann anhalten und melden).

Fuer C31747 (T2) ist die Lage belegt: C31804 steckt unter anderem in 56935, 56936, 56937 (FERT, ohne
Loeschvormerkung, Status leer) [B]; bei Auswahl nur 56861 muss C31747 deshalb **nicht** exklusiv sein.

### 2.7 Nebenbefunde (nicht Teil dieses Vorhabens, nichts mitfixen)

1. `has_bom` (Baugruppen-Kennzeichen) prueft nur `stlan = '1'` in Werk 1100 [R] 1186-1191; dort gibt es nur 17
   Stuecklisten mit Verwendung 1 gegen 50'303 mit Verwendung 3 [B]. Wirkung nur in `fill_ktab` 1736 (`baugr`), dort
   zusaetzlich `BESKZ <> 'F'`. Das Kennzeichen ist damit praktisch nur ueber `BAUGRUPPE` aus der Tabelle gesetzt.
2. Transaktionstext von ZLO03 ist ein einzelnes Zeichen „§“ (U+00A7) [B] SE93.
3. In P76 ist offen, welches Programm `ZPOWERBI_VC_TXT` fuellt (T76-Generator schreibt `ZAT_VC_TXT`).

---

## 3. Varianten

### Variante A (empfohlen): Die Kopie loest Verwendung 9 je Lauf selbst auf

Ablauf in `Z_ZLO03_PCBA`, nur wenn der neue Schalter `P_PCBA` gesetzt ist:

1. Einmal je Lauf alle MAST-Zeilen mit STLAN 9 lesen (881 Zeilen) und je Material merken, ob es in Werk 1100 eine
   Stueckliste mit Verwendung 1 oder 3 hat (fuer Regel R1, Entscheid E2).
2. Einmal je Lauf **alle** 814 Verwendung-9-Materialien einstufig aufloesen (`CS_BOM_EXPL_MAT_V2`, `capid = 'PCBA'`,
   `datuv = sy-datum`, `mehrs = ' '`, `emeng = 1`, `delnl = ' '`; erst `werks = '1100'`, bei Ausnahme ohne Werk), eigene
   Rekursion fuer verschachtelte Verwendung-9-Koepfe (Tiefe hoechstens 10, Material nie unter sich selbst). Ergebnis
   `gt_pcba_inhalt` (Kopf, Komponente, Menge je Basismenge, Einheit, Basismenge des Kopfs, Positionstyp, Text). Die
   Rueckwaertssicht (Komponente -> Koepfe) ist die Umkehrung dieser Tabelle; damit gilt fuer Vorwaerts- und
   Rueckwaertsrichtung dieselbe Gueltigkeitslogik von SAP (kein eigenes Lesen von STPO-Aenderungsstaenden).
3. **Mengen (Top-Down, Bottom-Up, Dashboard-Werte):** fuer jede Zeile aus `ZPOWERBI_VC_TXT`, deren Komponente ein
   zugelassener Verwendung-9-Kopf ist, werden die Inhaltszeilen als zusaetzliche Zeilen `ty_powerbi` erzeugt
   (Quelle `9`), eine Stufe tiefer, `KOM_MSTAE` = Kopf (MATN1-Ausgabeform wie der Generator). Damit laufen Einheit
   (FIX 23), Basismenge (FIX 26, mit der Basismenge aus `gt_pcba_inhalt` statt `load_bmeng`), FIX 27, FIX 28 und die
   Excel-Ausgabe unveraendert.
4. **Verwendungen (Exklusiv, Anzahl VKNR, weitere VKNR, Elternmaterial):** an allen Lesestellen aus 2.5 kommen zu den
   Paaren (Komponente, VKNR) aus `ZPOWERBI_VC_TXT` die Paare ueber Verwendung 9 dazu: Komponente -> alle Koepfe (auch
   mehrstufig nach oben) -> alle VKNR, in denen ein Kopf laut `ZPOWERBI_VC_TXT` steht. Fuer die Verwendungen gilt
   **immer die Vereinigung** aller 814 Koepfe, unabhaengig von R1: mehr Verwendungen machen „X“ seltener; falsch-negativ
   ist harmlos, falsch-positiv nicht (`zlo03/CLAUDE.md`, Fachlicher Hintergrund).

| Gesichtspunkt | Bewertung |
| --- | --- |
| Aufwand | mittel: neuer Schalter, 4 neue FORMs, Einhaengen an 7 Stellen (743, 751, 870, 1495, 1628, 2503, 3381); etwa 250 bis 350 Zeilen [V] |
| Laufzeit | einmal etwa 814 bis 1'000 Bausteinaufrufe je Lauf (nur mit `P_PCBA`); Grenze 600 s Dialog (LEARNINGS 2026-10-12). Schaetzung 10 bis 60 s [V], Messung Test T9 |
| Risiko Exklusiv | gering, weil die Rueckwaertssicht vollstaendig aus derselben Aufloesung kommt; Pflichttest T2 |
| Abhaengigkeiten | keine: kein DDIC, kein Job, kein Customizing; Generatoren und P76-Fuellung bleiben unberuehrt |
| Grenze | Aenderungen an Verwendung-9-Stuecklisten wirken sofort, die Verwendung-3-Sicht erst nach dem Nachtjob (wie heute) |

**Rechenbeispiel vorher/nachher (Top-Down 56861, Werte aus T76 [B]):**

| Zeile | Stufe | Elternmaterial | vorher | nachher |
| --- | --- | --- | --- | --- |
| C31804 | 02 | D50568 | 1.000 ST (VC_TXT ID 1672187) | unveraendert 1.000 ST |
| C34857 | 03 | C31804 | fehlt | 1 ST = STPO 00038057 Pos. 0010 MENGE 1 / STKO BMENG 1 ST |
| C31747 | 03 | C31804 | fehlt | 1 ST = Pos. 0020 MENGE 1 / BMENG 1 |

Einheit: Position ST = Basiseinheit ST (MARA), also kein FIX-23-Umrechnen; FIX 26 teilt durch 1. Die Menge gilt wie
bei allen Zeilen ab Stufe 02 je Elternteil und wird nicht mit der Menge des Elternteils multipliziert (bekannter
offener Punkt G4 in `zlo03/CLAUDE.md`); das gilt fuer PCBA-Zeilen genauso. Beispiel mit Faktor: E13185 steht mit 4 ST
in B63491 (Stufe darunter 4 ST je B63491); steckt B63491 mit 1 ST im Geraet, ist der Geraetebedarf 4 ST.

### Variante B: Eigener Generator in eine eigene Tabelle

- **B1 Vereinigungstabelle gleicher Struktur** (Kopie von `Z_VC_ZLO03_TXT` -> neue Tabelle mit Struktur von
  `ZPOWERBI_VC_TXT`, Verwendung 1/3 und 9 gemischt): Die Kopie von ZLO03 muesste nur den Tabellennamen an 9 Stellen
  tauschen (kleinster Diff), und die Exklusivpruefung waere von selbst vollstaendig. Dagegen: neue Tabelle (in T76
  1.9 Mio. Zeilen), zweiter Nachtlauf von rund 4 h 43 (P76, [R] TAGESZEIT 2), eigene Werklogik (Gruppenstuecklisten
  findet `PP01`/1100 nicht, also eigene Rekursion oder neues Customizing mit Auswahl-ID 1, 3, 9), und der Generator,
  der in P76 `ZPOWERBI_VC_TXT` fuellt, ist nicht gelesen. Hoher Aufwand, Abhaengigkeit von Unbekanntem.
- **B2 kleine Nachttabelle nur fuer Verwendung 9** (814 Koepfe, Minuten): Die ZLO03-Kopie haengt sie an dieselben 7
  Stellen wie in Variante A. Nur sinnvoll, wenn A in T76 zu langsam ist (Test T9). Zusaetzlich DDIC-Tabelle, Job in P76
  (Ingo), Daten bis zum naechsten Lauf veraltet.

Empfehlung: **Variante A**. Ausweg bei Laufzeitproblem: B2, Code der Kopie fast gleich (nur Quelle der Aufloesung).

---

## 4. Offene Entscheide fuer Ingo (vor Phase 2)

| Nr | Frage | Vorschlag |
| --- | --- | --- |
| E1 | Was ist mit dem Beispiel C31804 gemeint? In T76 ist C31804 Kopf einer PCBA-Stueckliste (C34857, C31747) und steckt selbst nur in Verwendung-3-Stuecklisten. | Lesart „C31804 hat eine PCBA-Stueckliste, deren Teile ZLO03 nicht zeigt“; die Testfaelle T1 bis T3 sind darauf ausgelegt. Bei der anderen Lesart (C31804 in einer PCBA-Stueckliste) bitte ein P76-Beispiel nennen; in T76 gibt es keins. |
| E2 | Materialien mit Verwendung 9 **und** 1/3 in Werk 1100 (154, z. B. B63485). Rechenbeispiel B63485 in einem Geraet: Verw. 3 liefert E11155 1 ST, Verw. 9 liefert 27 Positionen, darunter E11155 1 ST. „Beide addieren“ ergibt **E11155 2 ST** je B63485, richtig ist 1. | R1 (Vorschlag fuer die erste Fassung): Verwendung 9 nur dort aufloesen, wo es in 1100 keine Verwendung 1/3 gibt (660 Materialien); B63485 bleibt wie heute (E11155 1 ST, PCBA-Teile nicht sichtbar). R2 (spaeter moeglich): Verwendung 9 ersetzt die 1/3-Unterstruktur (B63485 zeigt 27 Positionen, E11155 1 ST); braucht das Entfernen der Unterstruktur ueber `MAT_MSTAE`-Kette, groesserer Diff. Exklusiv in beiden Faellen aus der Vereinigung. |
| E3 | Programm- und Transaktionsname | `Z_ZLO03_PCBA` (frei, TADIR/TRDIR) und `ZLO03P` (frei, TSTC/TADIR). Alternativ `ZLO04`/`ZLO05` (ebenfalls frei). |
| E4 | Paket | Programm wie das Original in `ZPP`, Transaktion wie ZLO03 in `ZLO1`; beide Transportschicht `ZT76` (TDEVC). Vorschlag: so lassen. |
| E5 | Berechtigung | ZLO03 prueft zusaetzlich `S_TCODE` mit `TCD = ZV00` (TSTCA). Vorschlag: gleich fuer ZLO03P. Wer die Transaktion bekommt (Rolle `Z_LO_ZLO03` oder andere der 9 Rollen mit ZLO03), entscheidet Ingo; Rollen pflegt der Rollenverantwortliche, Ingo ordnet zu. |
| E6 | Variante `8474_8475` mitkopieren? | ja (harmlos); nach dem Kopieren pruefen, dass `P_PCBA` beim Laden der Variante gesetzt bleibt (Test T0b). |
| E7 | Dashboard-Schalter `P_APP` / `app_export` / Memory-ID `ZZLO03_APP` in der Kopie | unveraendert lassen; das Dashboard ruft fest `z_zlo03_turbo2` ([R] `docs/abap/ZZLO_TOPDOWN_ADD.abap` 179). Die Kopie wird nie per SUBMIT gerufen. |
| E8 | Kennzeichnung im Excel | keine neue Spalte; die Herkunft steht in der vorhandenen Spalte „Elternmaterial“ (PCBA-Kopf). Wunsch nach eigener Spalte „ueber PCBA“ waere ein eigener Schritt (Layout von FIX 27). |
| E9 | Vorgabe des Schalters | `P_PCBA` vorbelegt mit `X` (Zweck der Kopie). Ohne Haken muss die Kopie Zelle fuer Zelle wie ZLO03 rechnen (Test T0). |

---

## 5. Phase 2: Schritte (nur T76/100, eigene Sitzung, genau in dieser Reihenfolge)

Grundregeln: vor dem ersten Schritt `docs/rag/LEARNINGS.md` (SAP) und `zlo03/CLAUDE.md` lesen; `Get-Process saplogon`;
`SapGuiT76Hilfe.vbs info`, dann `neu`, dann `info` (genau eine neue Sitzung); jede cscript-Ausgabe in eine Datei.
Bei jeder Abweichung vom erwarteten Wert anhalten. Original `Z_ZLO03_TURBO2` und `ZLO03` werden nicht geoeffnet im
Aendern-Modus.

### Schritt 0: Ausgangsstand festhalten (lesen)

| Pruefung | Erwartung |
| --- | --- |
| TRDIR `Z_ZLO03_TURBO2` | UDAT 12.11.2026, SDATE/STIME 12.11.2026 133353, VERN 000067 |
| Quelltext `Z_ZLO03_TURBO2` lesen und mit `zlo03/Z_ZLO03_TURBO2_FIX28_2026-11-12.abap` vergleichen | `diff` Exitcode 0 |
| TADIR PROG `Z_ZLO03_PCBA`, TSTC `ZLO03P` | nicht vorhanden |
| E071 OBJ_NAME `Z_ZLO03_PCBA` / `ZLO03P` | keine Zeile |
| Vorher-Laeufe ZLO03 (Original) als Hintergrundjob, Excel sichern | T-Faelle unten, Spalte „vorher“ |

### Schritt 0b: Machbarkeitsprobe vor jeder Kopie (Stopp-Kriterium)

Die Empfehlung fuer Variante A haengt an drei unbelegten Punkten. Vor Schritt 1 ein lokaler Testreport
`Z_KOI_ZLO_PCBA_PROBE` (`$TMP`, nur T76, nach dem Muster `zlo03/Z_KOI_ZLO_FIX28_TEST.abap`, nach dem Test loeschen):

| Punkt | Probe | weiter nur, wenn |
| --- | --- | --- |
| Baustein ohne Werk | `CS_BOM_EXPL_MAT_V2` `capid = 'PCBA'`, `mehrs = ' '` fuer C31804 mit `werks = '1100'` und mit `werks = space`, C27940 ebenso | Ergebnis wie CS12 in 2.2 (C31804 nur ohne Werk: C34857, C31747 je 1 ST; C27940 nur mit 1100: H10199 52.5 MM) |
| Basismenge | `topmat-bmeng` fuer C31804 und einen BMENG-<>1-Kopf (z. B. E11221, STKO 00026939 BMENG 4'750) | Feld vorhanden (vorher DD03L `CSTMAT` lesen) und Werte 1 bzw. 4'750 |
| Laufzeit | alle Verwendung-9-Materialien (MAST STLAN 9) einmal aufloesen, `GET RUN TIME` | unter 60 s im Dialog; sonst anhalten und B2 vorlegen |

Faellt ein Punkt durch: anhalten, nichts kopieren, Ergebnis an Ingo.

### Schritt 1: Programm kopieren (SE38)

1. `/nSE38`, `RS38M-PROGRAMM` = `Z_ZLO03_TURBO2`, Kopieren (Umsch+F1).
2. Dialog „Programm kopieren“: Zielprogramm `Z_ZLO03_PCBA`. Haken: **Textelemente** ja, **Varianten** ja (E6),
   Dokumentation, Benutzeroberflaeche, Dynpros, Includes nein (das Programm hat keine Includes, kein `SET PF-STATUS`,
   kein `CALL SCREEN` [R]; Dynpro 1000 ist das generierte Selektionsbild). Die genaue Beschriftung der Haken vorher mit
   `dump` lesen und hier eintragen; weicht der Dialog ab: anhalten.
3. Objektkatalog: Paket `ZPP`.
4. Transportabfrage: **nicht** den Vorschlag uebernehmen (in T76 offen u. a. `T76K912920` vom 18.11., fremd), sondern
   neuen Auftrag anlegen (`btn[8]`), Text (hoechstens 60 Zeichen): `ZLO03P Kopie Z_ZLO03_PCBA mit PCBA Verw. 9 (Claude)`.
   Auftragsnummer notieren.
5. Rueckleseprobe: TADIR `R3TR PROG Z_ZLO03_PCBA` Paket `ZPP`; Quelltext `Z_ZLO03_PCBA` lesen, `diff` gegen
   `Z_ZLO03_TURBO2` (T76): Exitcode 0; Textelemente: 24 Textsymbole, 9 Selektionstexte (wie Original [B]); VARID:
   Variante `8474_8475`; REPOSRC/TRDIR-Status pruefen, falls inaktiv: nur `Z_ZLO03_PCBA` aktivieren.
6. Test T0 (Kopie ohne Aenderung = Original), siehe Abschnitt 6.

### Schritt 2: Transaktion anlegen (SE93)

1. `/nSE93`, `TSTC-TCODE` = `ZLO03`, Kopieren (Umsch+F1), Ziel `ZLO03P`.
2. Danach `ZLO03P` aendern: Programm `Z_ZLO03_PCBA`, Selektionsbild `1000`, Start mit Variante leer,
   Transaktionstext `ZLO03 mit PCBA-Stuecklisten (Verw. 9)`, Berechtigungsobjekt `S_TCODE` / `TCD` = `ZV00` (E5),
   Klassifikation „Professional User Transaction“, GUI-Faehigkeit wie ZLO03 (alle drei Haken aus) [B].
3. Paket `ZLO1` (E4), Auftrag aus Schritt 1 ausdruecklich setzen.
4. Rueckleseprobe: TSTC `ZLO03P` -> PGMNA `Z_ZLO03_PCBA`, DYPNO 1000; TSTCA `ZLO03P S_TCODE TCD ZV00`; TSTCT Text;
   TADIR `R3TR TRAN ZLO03P` Paket `ZLO1`; E071: Auftrag enthaelt `R3TR PROG Z_ZLO03_PCBA`, `R3TR TRAN ZLO03P` (und je
   nach Dialog die Variante); nichts sonst. TSTC `ZLO03` unveraendert (PGMNA `Z_ZLO03_TURBO2`).

### Schritt 3: Code-Aenderungen in Z_ZLO03_PCBA (FIX 29, klassische Syntax)

Die Aenderung kommt als vollstaendige Datei `zlo03/Z_ZLO03_PCBA_FIX29_<Datum>.abap` aus dem Repo (vorher lokal aus der
Kopie erzeugt, Diff gegen `Z_ZLO03_TURBO2_FIX28_2026-11-12.abap` nur in den FIX-29-Bloecken). Einspielen mit
`SapGuiSetReportSourceAuftrag.vbs <Sitzung> Z_ZLO03_PCBA <Datei> <Auftrag>`. Keine Inline-Deklarationen, kein
`VALUE #`, keine String-Templates; alle DATA am Anfang der FORM. Jede Stelle mit `*** FIX 29:` und Begruendung.
Zeilen beziehen sich auf die FIX-28-Datei.

Pseudodiff:

```abap
* Kopf (nach Zeile 26):
*&   FIX 29 (2026-11-.., nur Z_ZLO03_PCBA / ZLO03P): Stuecklistenverwendung 9 (PCBA) zusaetzlich.
*&          PP01 waehlt nur Verwendung 1 und 3 (TCS41 CSLID 01); PCBA-Inhalt fehlte in ZPOWERBI_VC_TXT.

* Typen (Zeile 47-57): ty_powerbi am ENDE erweitern. [V] Dass "SELECT <Feldliste> INTO TABLE lt_powerbi" mit zwei
* Zusatzfeldern am Ende syntaktisch durchgeht (Felder links fuellen, Rest initial), ist nicht geprueft. Ausweich:
* eigener Lesetyp ty_powerbi_db (alte 9 Felder) fuer die beiden SELECTs in Zeile 743/751, danach umfuellen.
    postyp        TYPE zpowerbi_vc_txt-postyp,
    quelle        TYPE char1,          " *** FIX 29: ' ' = ZPOWERBI_VC_TXT, '9' = PCBA-Aufloesung
    bmeng9        TYPE stko-bmeng,     " *** FIX 29: Basismenge der Verwendung-9-Stueckliste des Elternteils
  END OF ty_powerbi,

* neue globale Typen/Tabellen:
*   ty_pcba_kopf   (matnr, has13 TYPE char1)                  HASHED KEY matnr
*   ty_pcba_inhalt (kopf, idnrk, menge TYPE kmpmg, meins, bmeng, postp, ojtxp)  STANDARD, SORTED-Zugriff je kopf
*   ty_usage       (kompnr TYPE matnr, vknr TYPE matnr)        gemeinsamer Typ fuer die Verwendungspaare
*   gt_pcba_kopf, gt_pcba_inhalt, gt_pcba_zeilen (TYPE tt_powerbi, erzeugte Zeilen dieses Laufs), gv_pcba_id

* Selektionsbild (nach Zeile 442), Text ohne neuen Selektionstext:
SELECTION-SCREEN BEGIN OF BLOCK b4 WITH FRAME.
  SELECTION-SCREEN BEGIN OF LINE.
    PARAMETERS: p_pcba AS CHECKBOX DEFAULT 'X'.     " *** FIX 29
    SELECTION-SCREEN COMMENT 3(60) c_pcba.           " deklariert c_pcba selbst, kein DATA dafuer
  SELECTION-SCREEN END OF LINE.
SELECTION-SCREEN END OF BLOCK b4.
* INITIALIZATION (nach Zeile 466):
  c_pcba = 'PCBA-Stuecklisten (Verwendung 9) einbeziehen'.   " *** FIX 29

* process_main, nach den beiden SELECTs von SCHRITT 2 (nach Zeile 758) und VOR der Pruefung
* "IF lt_powerbi IS INITIAL. RETURN." (Zeilen 760-762):
  " *** FIX 29: PCBA-Inhalt (Verwendung 9) als zusaetzliche Zeilen
  IF p_pcba = 'X'.
    PERFORM pcba_laden.                                   " MAST STLAN 9 + Aufloesung aller Koepfe, einmal
    IF p_topd = 'X'.
      PERFORM pcba_topdown_zeilen CHANGING lt_powerbi.     " unter jedem zugelassenen Kopf in den VKNR
    ELSE.
      PERFORM pcba_bottomup_zeilen USING lt_matnr_sel CHANGING lt_powerbi.  " Komponente -> Koepfe -> VKNR
    ENDIF.
  ENDIF.
  " Deshalb vor Zeile 760: sonst endet eine reine PCBA-Komponente (C31747, 0 Zeilen in
  " ZPOWERBI_VC_TXT) im Bottom-Up mit "Keine Daten gefunden".

* process_main FIX-26-Block (Zeile 876-889):
    IF ls_powerbi-quelle = '9'.                           " *** FIX 29
      IF ls_powerbi-bmeng9 > 0 AND ls_powerbi-bmeng9 <> 1.
        lv_menge_fix26 = lv_menge_fix26 / ls_powerbi-bmeng9.
      ENDIF.
    ELSEIF gt_bmeng IS NOT INITIAL AND ls_powerbi-postyp <> 'T'.
      ... (unveraendert)

* load_exclusivity_topdown (nach Zeile 1499), load_global_exclusivity (nach 1632),
* ermittle_weitere_vknr (nach 2507):
  IF p_pcba = 'X'.                                        " *** FIX 29
    PERFORM pcba_verwendungen USING lt_kompnr_list CHANGING lt_all_usage.
  ENDIF.
  " (in load_global_exclusivity in lt_zpowerbi-Form; dort Felder kompnr/matnr)

* load_elternmaterial_cache (nach Zeile 3386):
  IF p_pcba = 'X'.                                        " *** FIX 29: PCBA-Kopf als Elternmaterial
    LOOP AT gt_pcba_zeilen ... WHERE matnr IN lr_sel_vknr -> APPEND kompnr/kom_mstae TO lt_parents.
  ENDIF.
```

Neue FORMs (am Ende des Programms):

| FORM | Inhalt |
| --- | --- |
| `pcba_laden` | einmal je Lauf: `SELECT matnr werks FROM mast WHERE stlan = '9' AND ( werks = '1100' OR werks = ' ' )`; je Material `has13` aus `SELECT matnr FROM mast WHERE werks = '1100' AND stlan IN ('1','3')` (FOR ALL ENTRIES). Danach je Material `pcba_aufloesen`. |
| `pcba_aufloesen` | `CS_BOM_EXPL_MAT_V2` mit `capid = 'PCBA'`, `datuv = sy-datum`, `mehrs = ' '`, `mtnrv = Kopf`, `werks = gc_werks`, `emeng = 1`, `delnl = ' '`, TABLES `stb`, `matcat`, IMPORTING `topmat`; bei sy-subrc <> 0 noch einmal mit `werks = space`. Positionen mit `postp = 'T'` und leerem `idnrk`: Nummer aus `potx1` (wie FIX 8 des Generators). Basismenge aus `topmat-bmeng` [V: Feld in CSTMAT vor dem Bau in DD03L pruefen]. Zeile `idnrk = Kopf` auslassen (Selbst-Stueckliste, vgl. FIX 28). |
| `pcba_topdown_zeilen` | fuer jede Zeile `ls_pb` mit `postyp <> 'T'`, keine Selbstzeile (`ist_selbstzeile`), Komponente in `gt_pcba_kopf` und (R1) `has13 = ' '`: je Inhaltszeile eine neue Zeile `id = gv_pcba_id` (ab 90000001), `stufe = ls_pb-stufe + 1`, `kompnr = idnrk`, `menge` = Text der Positionsmenge mit Punkt als Dezimalzeichen (wie `|{ menge }|` im Generator; klassisch: Zuweisung an CHAR, `CONDENSE`), `mengeneinheit = meins`, `baugruppe = 'X'` wenn die Komponente selbst zugelassener Kopf ist, `matnr = ls_pb-matnr`, `kom_mstae` = `CONVERSION_EXIT_MATN1_OUTPUT( Kopf )`, `postyp = postp`, `quelle = '9'`, `bmeng9`. Verschachtelte Koepfe rekursiv (Tiefe <= 10). Zeilen auch in `gt_pcba_zeilen`. |
| `pcba_bottomup_zeilen` | fuer jede selektierte Komponente: alle Koepfe aus `gt_pcba_inhalt`, rekursiv nach oben bis zu einem Kopf, der in `ZPOWERBI_VC_TXT` als KOMPNR steht; dann `SELECT matnr stufe FROM zpowerbi_vc_txt FOR ALL ENTRIES ... WHERE kompnr = Kopf`; je Treffer eine Zeile wie oben (`matnr` = VKNR, `kompnr` = Komponente, `kom_mstae` = direkter Kopf). Nur zugelassene Koepfe (R1) fuer Mengen. |
| `pcba_verwendungen` | fuer die Komponenten der Liste: Koepfe aus `gt_pcba_inhalt` (alle 814, ohne R1), rekursiv nach oben; VKNR je Kopf aus `ZPOWERBI_VC_TXT` (KOMPNR = Kopf) und, falls ein Kopf selbst als MATNR (VKNR) in der Tabelle steht, der Kopf selbst; Paare (Komponente, VKNR) anhaengen. Danach sortiert die aufrufende FORM wie bisher und entfernt Duplikate. |

### Schritt 4: Syntaxpruefung, Aktivieren

1. Strg+F2: nur die bekannte Warnung „Regex-Standard POSIX abgekuendigt“ (FIX 21) ist erlaubt; jeder Fehler: anhalten.
2. Im Dialog „Fehler beim Aktivieren“ nie „Aktivieren“, sondern „Abbrechen“ (LEARNINGS 2026-11-02).
3. Dialog „Inaktive Objekte“: nur `Z_ZLO03_PCBA` markieren (`SapGuiInaktiveMarkieren.vbs ... pruefen`, dann
   `aktivieren`); fremde inaktive Objekte (z. B. frueher `Z_REICHW`) nicht.
4. Rueckleseprobe: Quelltext aus T76 lesen und mit der Repo-Datei vergleichen (`diff` Exitcode 0); REPOSRC nur Status A;
   `Z_ZLO03_TURBO2` unveraendert (TRDIR wie Schritt 0, Quelltext-`diff` 0); E071 wie Schritt 2.
5. ST22 des Tages fuer den eigenen Benutzer in der eigenen Sitzung (`SapGuiT76Hilfe.vbs ok <ses> /nST22`), nicht `SapGuiSt22.vbs`
   (LEARNINGS 2026-11-12).

### Schritt 5: Tests (Abschnitt 6), danach Sitzung mit `zu` schliessen

---

## 6. Testfaelle T76 (vorher = Original ZLO03, nachher = ZLO03P)

Laeufe als Hintergrundjob mit Excel-Ablage wie bei FIX 28 (Zellvergleich), oder im Dialog mit Download in den
Scratchpad. T76-Daten sind eine Kopie (Stand etwa Fruehjahr 2026) mit Luecken; Ergebnisse sind fuer P76 nur Hinweise.

| Nr | Lauf | Erwartung |
| --- | --- | --- |
| T0 | ZLO03P mit `P_PCBA` leer gegen ZLO03: Top-Down 56861, Top-Down `S_TYPCD` = `8475*`, Bottom-Up C31804 | Excel Zelle fuer Zelle gleich (Kopie vor und nach FIX 29) |
| T0b | ZLO03P mit Variante `8474_8475` laden | `P_PCBA` steht auf `X` (oder Ergebnis notieren und E6/E9 nachfragen) |
| T1 | Top-Down 56861, `P_PCBA` = X | wie T0 plus zwei Zeilen: C34857 1 ST und C31747 1 ST, Elternmaterial C31804 (Stufe 03 unter C31804 auf Stufe 02); C34857 ohne Bestand/Preis (kein MARC 1100, MBEW nicht geprueft [V]). Bestehende Zeilen: Mengen und Werte gleich; „Exklusiv“ darf nur von `X` nach leer wechseln und „Weitere VKNR“ nur wachsen, jede solche Aenderung mit dem Paar Kopf/VKNR belegen (die Rueckwaertssicht gilt fuer alle Komponenten) |
| T2 | wie T1, Spalte Exklusiv | C31747 und C34857 **nicht** `X` (C31804 steckt auch in 56935, 56936, 56937, FERT aktiv, Status leer [B]). Gegenprobe: ohne `pcba_verwendungen` waere es `X` |
| T3 | Bottom-Up C31747 | vorher „Keine Daten gefunden“; nachher dieselben VKNR wie Bottom-Up C31804 (ohne Loeschvorgemerkte bei `P_LVORM` leer), je 1 ST |
| T4 | Bottom-Up C31804 | gleich wie vorher (C31804 steht in keiner Verwendung-9-Stueckliste) |
| T5 | Top-Down eines aktiven Geraets mit B63485 (vorher per `ZPOWERBI_VC_TXT`/MARA bestimmen, sonst anderen der 154 Koepfe nehmen) | R1: Mengen wie vorher (E11155 1 ST, nicht 2 ST); Exklusiv-Aenderungen nur wie bei T1 erlaubt |
| T6 | Werkregel: Geraet mit C27940 (Verw. 9 nur Werk 1100, Geraet vorher bestimmen, sonst einen der anderen 5 Koepfe nur mit Werk 1100) und Geraet mit Gruppenstueckliste (C31804) | C27940 -> H10199 52.5 MM; C31804 -> C34857/C31747 |
| T7 | Exklusiv-Pflichtfall aus 2.6: Top-Down auf ein aktives FERT-Geraet mit B63491 (vorher unter den 251 suchen), Komponente E13185 | erscheint mit 4 ST unter B63491; Exklusiv leer, sofern ein zweites aktives FERT-Geraet B63491 enthaelt; sonst `X` und Fall wechseln |
| T8 | Dashboard-Weg: `ZloTopDownSet` 56861 im T76-Gateway | unveraendert (ruft `Z_ZLO03_TURBO2`) |
| T9 | Laufzeit Top-Down `8475*` mit und ohne `P_PCBA` | Unterschied notieren; ueber 120 s Mehrzeit: anhalten, B2 vorlegen |
| T10 | ST22 | 0 Laufzeitfehler |

---

## 7. Rueckbau

Das Original wird nicht angefasst, es gibt nichts zurueckzustellen. Rueckbau der Kopie in T76 (vor Freigabe):

1. SE93 `ZLO03P` loeschen, SE38 `Z_ZLO03_PCBA` loeschen (Variante faellt mit), jeweils im eigenen Auftrag.
2. Ist der Auftrag noch nicht freigegeben: Objektliste leeren bzw. Auftrag loeschen (SE01/SE09). Ist er freigegeben,
   bleibt der Loeschauftrag als eigener Transport (Ingo).
3. Rueckleseprobe: TADIR/TSTC/TRDIR ohne `Z_ZLO03_PCBA`/`ZLO03P`; `Z_ZLO03_TURBO2`/`ZLO03` wie Schritt 0.

---

## 8. Was Ingo nach Phase 2 tun muss

1. Entscheide E1 bis E9 (vor Phase 2).
2. Auftrag freigeben und nach P76 importieren (nicht der Agent).
3. Rolle(n) fuer `ZLO03P` festlegen (E5); Pflege durch den Rollenverantwortlichen, Zuordnung durch Ingo.
4. In P76 erste Laeufe pruefen lassen (Anwender); P76 kann andere Verwendung-9-Stuecklisten haben als T76.

## 9. Wie gelesen wurde (reproduzierbar, nur T76)

Eigene Sitzung `/app/con[0]/ses[1]` ueber `SapGuiT76Hilfe.vbs neu`; Quelltexte mit `SapGuiQuelltextLesen.vbs`
(Index 1); Tabellen mit SE16N ueber ein Leseskript im Scratchpad (`se16n_lesen.vbs`, prueft die eigene Sitzungs-Id,
setzt nie „Eintraege pflegen“, liest das Ergebnis-Grid `wnd[0]/shellcont/shell` zeilenweise); CS12, SE93, SE38
(Varianten, Textelemente) nur im Anzeigemodus. Kein RFC, kein Passwort, kein Eintrag in Repository-Dateien
(`SapGuiT76Hilfe.vbs neu` schreibt `.tmp_sap_probe/t76_eigene_sitzung.txt`, die Datei ist per `.gitignore` ausgenommen).

Nebenwirkungen: CS12 merkt sich die Anwendung als Benutzerparameter; nach den Proben mit `PCBA` wurde mit `PP01`,
Werk 1100 und Material 53707 einmal ausgefuehrt, das Einstiegsbild zeigt wieder `PP01` / 1100. Im Werkfeld stand dabei
kurz ein Strich (ungueltig, nichts ausgefuehrt), danach leer. Die eigene Sitzung ist mit `/i` geschlossen; Ingos
Sitzung `ses[0]` (SESSION_MANAGER) wurde nicht benutzt. SE16N erzeugt keine Selektionsreports wie SE16.
