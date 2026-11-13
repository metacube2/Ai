# Plan „Layoutfilter ZC22/ZC27 im SAP“: `ShopRmSet` Materialausschluss und `ShopAufSet` Status/Arbeitsplatz (T76)

*Nachtrag 2026-11-13 abends: **Phase 2 ausgefuehrt** (Entscheide E1 bis E5 nach Empfehlung, gesetzt von der Hauptsitzung auf Grundlage der Entscheide Ingos), alle Erwartungswerte getroffen, Ergebnis `NOTES.md` Abschnitt 10; `T76K912906` offen, nicht freigegeben. Der Satz „Phase 1 von 2“ unten ist damit ueberholt.*

Stand: 2026-11-13 13:55, Agent sap-t76, **Phase 1 von 2 (Plan)**. Auftrag Ingo 13.11. (Teil 2 des Auftrags „CoPaSumSet Hzdat“):
Plan fuer zwei weitere Aenderungen im **selben** Transport `T76K912906`. Phase 2 erst nach Freigabe durch Ingo, dann genau
diese Schritte, bei jeder Abweichung anhalten (`saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` Abschnitt 7).

**Was in Phase 1 in T76 passiert ist:** nur gelesen (RFC `RunSapProbe.ps1` mit DPAPI-Ablage; GUI in eigener Sitzung
`/app/con[0]/ses[1]`), mit einer Ausnahme, die der Auftrag erlaubt: der lokale Testreport `Z_KOI_SHOP_STAT_TEST` (`$TMP`,
Quelltext `Z_KOI_SHOP_STAT_TEST.abap` in diesem Ordner) wurde 13:3x angelegt, zweimal ausgefuehrt und gegen 13:50 wieder geloescht
(`TRDIR`, `TADIR`, `REPOSRC` danach ohne Eintrag; Gegenprobe `TRDIR` `ZPP_RM_MENGEN` liefert die Zeile). ZC27 wurde zweimal
angezeigt, Filter nur in der Anzeige geloescht, kein Layout gesichert. P76 nicht beruehrt.

Grundlagen: `P76_BEFUND_2026-11-13.md` Abschnitte 1, 2.5, 3.2, 3.5; Cockpit-Seite schon gebaut (Commit `768ede8`, Schalter aus,
siehe 1.7); Teil 1 dieses Auftrags `saptasks/copa_verkauf/NOTES.md` Abschnitt 4.

Alle Zahlen aus T76 Mandant 100 (Datenkopie etwa Fruehjahr 2026). Fuer P76 sind sie Hinweise.

---

## 0. Kurzfassung

1. **`ShopRmSet` + Filter `Matex`** (Musterliste, wie das Cockpit sie schickt: `Matex eq 'E*,D*,F*,C*'`): Rueckmeldungen zu
   Auftraegen, deren Material (erste AFPO-Position, genau wie ZC27) auf ein Muster passt, fallen weg. Ohne `Matex` rechnet der Zweig
   unveraendert. Neues Strukturfeld `ZSTR_SHOP_RM-MATEX` (Echo der Liste).
2. **`ShopAufSet` Typ VR/VZ + `Sttxt`, `Arbpl`, `Vornr`, `Avo`:** `Sttxt` = ZC22-Statuszeile (Logik `FORM status_lang`) fuer jede
   ausgegebene Zeile Typ VF, **immer** (das Cockpit fordert sie nicht an); `Arbpl`/`Vornr` = `Z_MD04_NEXT_OPERATION` wie ZC22
   `FORM vorgangsdaten`, **nur mit Filter `Avo eq 'X'`**. Beides erst nach dem Paging, also nur fuer die Zeilen der Seite. Mengen,
   Zeilen und Reihenfolge bleiben gleich.
3. **Statuszeile nicht per `PERFORM status_lang IN PROGRAM zm_offene_fauf`, sondern gleichwertig gesammelt mit Sprache D fest.**
   Grund: Das Gateway laeuft nicht verlaesslich deutsch. Belege (1.4): `ShopAufSet` lieferte in T76 im Gateway Client
   `"Meins" : "PC"` fuer `AFPO-MEINS = 'ST'`, und Fehlerantworten derselben Sitzung wechselten zwischen `"lang" : "en"` und `"de"`.
   `status_lang` nimmt `sy-langu` und gaebe bei Englisch `REL PRT CNF …` statt `FREI DRUC RÜCK …`; der Cockpit-Filter
   `FREI DRUC RÜCK VOKL*` griffe dann nicht. Die gesammelte Fassung ist im Test **fuer alle 2'197 Auftraege gleich** `status_lang`
   (1.8) und 33-mal schneller.
4. **Laufzeit T76 (Werk 1100, 2'197 Fertigungsauftraege der VR-Menge):** Statuszeile gesammelt 117 ms (0,05 ms je Auftrag),
   `Z_MD04_NEXT_OPERATION` 16,2 s (7,4 ms je Auftrag). Der Arbeitsplatz ist der teure Teil, deshalb nur auf Anforderung.

---

## 1. Ist-Stand mit Belegen

### 1.1 ZC27: welches Material das Layout `/NIN` filtert

`ZPP_RM_MENGEN` in T76 per RFC gelesen (`ZPP_RM_MENGEN_T76_2026-11-13.abap`, 141 Zeilen; laut `P76_BEFUND` 3.1 zeilengleich mit
P76). FORM `lesen_afru`:

| Zeilen | Was |
| --- | --- |
| 85 bis 90 | `select matnr into l_matnr from afpo up to 1 rows where aufnr = iafru-aufnr order by primary key` (erste Position des Auftrags) |
| 91 | `select single * from mara where matnr = l_matnr` |
| 104 | `t_ausg-matnr = mara-matnr` (Ausgabespalte Material) |
| 129 bis 139 | `REUSE_ALV_GRID_DISPLAY`, `i_save = 'A'`, ohne `is_variant`: das Standardlayout gilt |

Das Material der Liste ist also `AFPO-MATNR` der ersten Position (ueber MARA, gleicher Wert). Nebenbefund: `l_matnr` wird je
Rueckmeldung nicht geleert; faende AFPO nichts, stuende das Material der vorigen Zeile da (in der Praxis hat jede Rueckmeldung
einen Auftrag mit Position; Vermutung, nicht gemessen).

Standardlayout in T76: `LTDXD` `REPORT = 'ZPP_RM_MENGEN'` ein Eintrag, global (`USERNAME` leer), `VARIANT = '/NIN'`, wie in P76
(`P76_BEFUND` 3.2: Filter Material „Einzelwerte ausschliessen“ `E*`, `D*`, `F*`, `C*`, Option `E CP`).

**Nachweis in T76 (ZC27 in eigener Sitzung, 13.11. 13:4x, DLQ72, Werk 1100, 14.04.2026):**

| | Zeilen | Stueck | Material-Anfang |
| --- | ---: | ---: | --- |
| ZC27 mit Standardlayout `/NIN` | 21 | **394** | 4: 165, 5: 59, 6: 170 |
| ZC27 nach „Filter loeschen“ (`tbar[1]/btn[38]`, nur Anzeige) | 28 | **494** | dazu C: 7 Zeilen, 100 |
| `ShopRmSet` heute (Gateway Client, `Arbpl eq 'DLQ72'`, `Ersda eq '20260414'`) | 28 (`Anzahl`) | 494 (`Lmnga`) | |

Gegenfall MLE04 14.04.2026: ZC27 mit `/NIN` 34 Zeilen / 579, `ShopRmSet` 34 / 579 (kein Material E/D/F/C an diesem Tag; RFC
AFRU/AFPO: an MLE04 im April keine solche Rueckmeldung).

Falle fuer Tests: Ein `ShopRmSet`-Aufruf ist ein ZC27-Lauf mit genau diesen Arbeitsplaetzen. `Arbpl eq 'DLQ72' or Arbpl eq 'MLE04'`
liefert fuer DLQ72 nur 100 / 7, weil dann der hoechste Vorgang oft an MLE04 liegt (`saptasks/shopfloor_sap/NOTES.md` Abschnitt 2).

### 1.2 `ShopRmSet` im DPC heute

`ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CM01W`, Live-Stand nach Teil 1 = `saptasks/copa_verkauf/DPC_EXT_GET_ENTITYSET_neu_2026-11-13.abap`
(2'737 Zeilen); fuer die Zeilen bis 2'270 gleich `DPC_EXT_GET_ENTITYSET_2026-11-09_vorrat_neu.abap` (Diff: nur Zugaenge CoPaSum).
Zweig `ShopRmSet` Zeilen 1607 bis 1753, Quelle `docs/abap/ZSHOP_RM_ADD.abap`:

| Zeile | Inhalt (Anker fuer Phase 2) |
| --- | --- |
| 1644 | `          lv_rm_typcd TYPE char10.` (Ende der DATA-Kette) |
| 1652 bis 1670 | Filterschleife, nur `sign = 'I' AND option = 'EQ'`; 1666 `            APPEND ls_rm_typ TO lr_rm_typ.`, 1667 `          WHEN OTHERS.` |
| 1709 bis 1717 | Typencode-Block (`IF lr_rm_typ IS NOT INITIAL` … 1716 `CHECK lv_rm_zztyp IN lr_rm_typ.`, 1717 `        ENDIF.`) |
| 1719 bis 1732 | hoechster Vorgang an den gewaehlten Arbeitsplaetzen |
| 1743 | `          ls_rm_out-typcd = lv_rm_typcd.` (neue Ausgabezeile) |

MPC `ZCL_ZPOWERBI_EINKAUF_MPC_EXT==CM001` (Live = `saptasks/copa_verkauf/MPC_EXT_DEFINE_neu_2026-11-13.abap`): Entity `ShopRm`
Zeilen 2586 bis 2643, `bind_structure( iv_structure_name = 'ZSTR_SHOP_RM' iv_bind_conversions = ' ' )` in Zeile 2637.

### 1.3 ZC22: Statuszeile und naechster Arbeitsplatz

`ZM_OFFENE_FAUF` in T76 per RFC gelesen, zeilengleich mit `docs/abap/ZM_OFFENE_FAUF.abap` (`diff` rc 0 nach Entfernen von
Leerzeichen am Zeilenende).

| Zeilen | Was |
| --- | --- |
| 37 | `p_avo as checkbox default ' '` („Mit Vorgangsdaten“) |
| 597 bis 604 | nur wenn `iafpo-objnp ne 'PLAF'` (Fertigungsauftrag): `perform status_lang using aufk-objnr changing gt_uebauf-sttxt`, `perform vorgangsdaten` |
| 683 bis 694 | FORM `vorgangsdaten`: `check p_avo = 'X' or not s_arbpl[] is initial`, dann `Z_MD04_NEXT_OPERATION` mit `i_aufnr = afko-aufnr`, `e_vornr`, `e_arbpl` |
| 1079 bis 1142 | FORM `status_lang` (Parameter `p_objnr type j_objnr`, `p_txt type c`; nur lokale Daten, keine Globals): `JSTO-OBTYP`; `JEST` aktiv (`inact = space`), `stat like 'I%'`; je Status `TJ02-NODIS` leer, Kuerzel `TJ02T-TXT04` in **`sy-langu`**, sonst `D` (Zeilen 1110 bis 1115); `TJ04-LINEP/STATP` je `OBTYP`, fehlend 99; sortiert `linep statp txt04`, je `LINEP` < 99 nur der erste; mit Leerzeichen verkettet |

`Z_MD04_NEXT_OPERATION` (TFDIR: `SAPLZZMD04`, Include `LZZMD04U01`, nicht RFC-faehig; Quelltext
`Z_MD04_NEXT_OPERATION_T76_2026-11-13.abap`, 51 Zeilen): `AFKO-AUFPL`; alle `AFVC` ausser `STEUS = 'PP00'`, alle `AFVV`; je
Vorgang fertig, wenn `LMNGA + XMNGA >= MGVRG + ASVRG` oder `JEST` `I0009`/`I0013` aktiv (eine `COUNT`-Abfrage je offenem Vorgang);
der erste uebrige nach `VORNR` liefert `E_VORNR` und `CRHD-ARBPL`. Kosten je Auftrag: 4 Lesezugriffe plus einer je offenem Vorgang.

Standardlayout ZC22 in T76: `LTDXD` `REPORT = 'ZM_OFFENE_FAUF'`: global `/ P00 TX`, dazu `DIGITAL3` und `RP` benutzereigen, wie in
P76 (`P76_BEFUND` 1.1). Filter laut P76 (1.2): Status `E CP 'FREI DRUC RÜCK VOKL*'`, Arbeitsplatz `I NP 'M*'`.

### 1.4 Sprache im Gateway

- T76 Gateway Client 13.11. 13:49, `ShopAufSet?$filter=Werks eq '1100' and Datum eq '20261113' and Typ eq 'VR'&$top=3`: erste Zeile
  Auftrag 1173719, `"Meins" : "PC"`. RFC `AFPO` 000001173719 Position 0001: `MEINS = 'ST'`. `T006A` `ST`: D `ST`, E `PC`. Die
  Ausgabekonvertierung (`iv_bind_conversions = abap_true` bei ShopAuf, MPC Zeile 973) lief bei diesem Aufruf also mit Englisch,
  obwohl die Sitzung (KOI) deutsch angemeldet ist.
- Die gespeicherten 400-Antworten derselben Sitzung tragen abwechselnd `"lang" : "en"` und `"de"`: 13:24:55 en, 13:24:56 de,
  13:31:24 en, 13:31:25 de, 13:49:12 en, 13:49:43 en. Die Sprache wechselt also von Aufruf zu Aufruf. Welchen Wert `sy-langu` im
  DPC dabei hat, ist nicht gemessen (Vermutung: dieselbe Sprache wie die Antwort).
- `TJ02T`: I0002 D `FREI` / E `REL`, I0007 `DRUC`/`PRT`, I0009 `RÜCK`/`CNF`, I0010 `TRÜC`/`PCNF`.
- Folgerung: `status_lang` (sy-langu) kann im Gateway englische Kuerzel liefern, je nach Aufruf. Die bestehende Regel steht schon in
  `saptasks/SAP_ARBEITSWEISE_UND_WERKZEUGE.md` („`sy-langu` ist im Gateway kein verlaesslicher Wert“), `QmMeldungSet` liest deshalb
  fest `D` (`saptasks/qm_meldungen/ZQM_MELDUNG_ADD.abap` Zeilen 315, 333). Die Teamleiter fahren ZC22 deutsch (Layoutmuster deutsch).

### 1.5 `ShopAufSet` im DPC heute

Zweig `ShopAufSet` Zeilen 646 bis 841 (Live = beide Dateien wie in 1.2), Typ VR/VZ 695 bis 775 (`docs/abap/ZSHOP_VERZUG_ADD.abap`,
VR-Erweiterung `DPC_EXT_GET_ENTITYSET_2026-11-09_vorrat_neu.abap`):

| Zeile | Inhalt (Anker) |
| --- | --- |
| 678 | `          ls_sa_vz TYPE ty_sa_vz.` (Ende DATA), 679 `FIELD-SYMBOLS: <ls_sa_f> …` |
| 682 bis 691 | Filterschleife, nur `I EQ`; 687 `          WHEN 'TYP'. lv_sa_typ = <ls_sa_o>-low.`, 688 `WHEN OTHERS.` |
| 695 | `    IF lv_sa_typ = 'VZ' OR lv_sa_typ = 'VR'.` |
| 741 bis 752 | Zeilen Typ VF (Fertigungsauftrag, `aufnr` intern mit Nullen), 753 bis 764 Typ VP (Planauftrag) |
| 765 bis 772 | `SORT lt_sa_out BY datum aufnr`, `$skip`, `$top` (Zeilen werden geloescht) |
| 773 | `      copy_data_to_ref( EXPORTING is_data = lt_sa_out CHANGING cr_data = er_entityset ).` (erste nach Zeile 695) |

MPC: Entity `ShopAuf` Zeilen 913 bis 982, `bind_structure( iv_structure_name = 'ZSTR_SHOP_AUF' iv_bind_conversions = abap_true )`
in Zeile 973.

### 1.6 Strukturen und Verwendung

| Struktur (Paket `ZPP`) | Felder heute (`DD03L`, aktiv) | Verwendung (`D010TAB`) | Include in Tabellen (`DD03L PRECFIELD`) |
| --- | --- | --- | --- |
| `ZSTR_SHOP_RM` | WERKS WERKS_D, ARBPL ARBPL, ERSDA CHAR8, TYPCD CHAR10, LMNGA INT4, ANZAHL INT4 | nur `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CP` | keine |
| `ZSTR_SHOP_AUF` | WERKS WERKS_D, AUFNR AUFNR, TYP CHAR2, MATNR MATNR, DISPO DISPO, DATUM CHAR8, MENGE INT4, MEINS MEINS | nur `ZCL_ZPOWERBI_EINKAUF_DPC_EXT==CP` | keine |

Im DPC werden beide Strukturen nur feldweise gefuellt (kein `SELECT … INTO TABLE lt_sa_out`), Anhaengen am Ende ist also
unschaedlich. Datenelemente fuer die neuen Felder (`DD04L`/`DD01L`): `TEXT100` CHAR 100 (Kleinbuchstaben erlaubt, keine
Konvertierung), `ARBPL` CHAR 8 ohne Konvertierung, `CHAR4`, `CHAR1`, `CHAR40`. **Nicht** `VORNR`: dessen Domaene hat die
Konvertierung `NUMCV`, und ShopAuf bindet mit Konvertierung; deshalb `CHAR4`.

Sperren: `TLOCK` ohne Eintrag fuer `ZCL_ZPOWERBI%`, `ZSTR_COPA%`, `ZSTR_SHOP%` (13.11. vor Teil 1). Seit Teil 1 sind beide
Methoden und `ZSTR_COPA_SUM` in `T76K912906`/`T76K912907` gesperrt (gewollt).

### 1.7 Was das Cockpit schon erwartet (Commit `768ede8`, Schalter aus)

- `Services/Shopfloor/ShopfloorSapMengen.cs` Zeilen 66, 72 bis 77: `… and Matex eq '<Liste>'`, Liste kommagetrennt
  (`ShopfloorLayoutFilter.Muster` trennt an `,` und `;`, verbindet mit `,`). **Ein** `eq` mit der ganzen Liste, keine Oder-Kette.
- `ShopfloorSapVorrat.cs` Zeile 70 und `ShopfloorSapVerzug.cs` Zeile 43: `… and Typ eq 'VR'|'VZ'` plus `and Avo eq 'X'`, wenn der
  Arbeitsplatzteil aktiv ist.
- `ShopfloorLayoutFilter.cs`: liest `Sttxt` und `Arbpl` je Zeile; `Sttxt` wird **nicht** angefordert, muss also immer kommen;
  `PruefeFelder` bricht ab, wenn bei Typ VF die Eigenschaft fehlt. Muster wie ABAP `CP` ohne Gross/Klein.
- Leser `ShopfloorSapSync.ReadAsync` liest Felder nur nach Namen (`TryGetProperty`), Seiten zu 5'000 Zeilen, Zeitlimit 5 Minuten
  (Zeile 388). Zusaetzliche Felder stoeren den Import nicht.
- Vor dem Transport: `Matex` und `Avo` gibt es nicht; T76 13:49 Gateway Client: `ShopRmSet … and Matex eq 'E*,D*,F*,C*'` 400
  „Property Matex not found in type ShopRm“, `ShopAufSet … and Avo eq 'X'` 400 „Property Avo not found in type ShopAuf“. Das
  Cockpit bekommt also einen Fehler statt einer Fehlzahl, solange P76 den Transport nicht hat.

### 1.8 Laufzeitmessung T76 (Testreport `Z_KOI_SHOP_STAT_TEST`, 13.11.)

Auswahl wie der DPC-Zweig VR (AFKO/AFPO, `DWERK = '1100'`, `ELIKZ`/`DNREL` leer, erste Position je Auftrag), nur Fertigungsauftraege
(Planauftraege bekommen in ZC22 keinen Status und keinen Vorgang). Dialog, `sy-langu` DE.

| Lauf | Auftraege | A `PERFORM status_lang IN PROGRAM zm_offene_fauf` | B gesammelt, Sprache D | C `Z_MD04_NEXT_OPERATION` | A gleich B |
| --- | ---: | ---: | ---: | ---: | --- |
| Stichprobe (`p_max` 200) | 200 | 411 ms (2,06 ms je Auftrag) | 142 ms (0,71 ms) | 1'410 ms (7,05 ms) | 200 von 200 |
| alle (`p_max` 0), Wanduhr 22,3 s | **2'197** | 3'885 ms (**1,77 ms**) | 117 ms (**0,053 ms**) | 16'246 ms (**7,40 ms**) | **2'197 von 2'197** |

Auswahl selbst 7 ms (zweiter Lauf). Heutiger Gateway-Aufruf VR `$top=3` (baut alle Zeilen, gibt 3 aus): 0,78 s.

Was der ZC22-Layoutfilter in T76 ausblenden wuerde (Statuszeile aus A, Arbeitsplatz aus C): Status `FREI DRUC RÜCK VOKL*` **63**,
naechster Arbeitsplatz `M*` **165**, beides 0, zusammen 228 von 2'197. Je Disponentengruppe (ausgeblendet / alle): EK* 8 / 162,
EL* 50 / 135, S* 1 / 224, uebrige 169 / 1'676. Beispiele: 1170555 (Dispo 025) `FREI DRUC RÜCK VOKL ABGR ABRV MABS WABE`, kein
Vorgang; 1209407 (SE1) `FREI DRUC RÜCK VOKL SPER ABRV MABS WABE` (auch in P76 ausgeblendet, `P76_BEFUND` 2.3); 1213839 (D11)
Vorgang 0800, MLE04, `FREI DRUC TRÜC VOKL ABGR ABRV MABS WABE`; 986726 (001) 0010 PTA00 `FREI DRUC VOKL ABRV MABS` (sichtbar).

Hochrechnung fuer P76 (Vermutung, gleiche Kosten je Auftrag wie T76 angenommen): je 1'000 Fertigungsauftraege der Seite Statuszeile
rund 0,05 s, Arbeitsplatz rund 7,4 s. Wie viele Fertigungsauftraege die VR-Menge in P76 hat, ist nicht gemessen (`P76_BEFUND` 2.1: die
nach Datum sortierte VR-Liste hatte bei 2'500 Zeilen erst Eckende 30.11.2026, Plan- und Fertigungsauftraege zusammen).

---

## 2. Entwurf

### 2.1 `ShopRmSet`: Filter `Matex`

- Neue Property `Matex` (Edm.String 40, filterbar), Strukturfeld `MATEX CHAR40` (Echo der gesendeten Liste, ohne Filter leer).
- Filterschleife: `WHEN 'MATEX'`: Text merken, an `,` teilen, je Teil `CONDENSE`, leere weglassen. Mehrere `Matex eq` (Oder-Kette)
  werden zusammengefuehrt, das Cockpit schickt aber eine Liste.
- In der Schleife ueber die Rueckmeldungen direkt nach dem Typencode-Block: nur wenn Muster da sind, Material wie ZC27 (erste
  AFPO-Position, je Auftrag einmal gelesen und gemerkt); passt es auf ein Muster (`CP`, ohne Gross/Klein), `CHECK` faellt die
  Rueckmeldung weg. Die Regel „hoechster Vorgang“ bleibt unberuehrt; beide sind Zeilenfilter, die Reihenfolge aendert das Ergebnis
  nicht (ZC27 filtert die fertige Liste).
- Ohne `Matex`: `lt_rm_mx` leer, der neue Block wird uebersprungen; einziger Unterschied in der Antwort ist `"Matex":""`.
- Vergleich gegen das interne Materialfeld. Fuer `E*`/`D*`/`F*`/`C*` gleich der Anzeige (alphanumerische Nummern stehen
  linksbuendig ohne Nullen, z. B. `F90183`); ein Muster fuer rein numerische Materialien muesste die interne Form treffen (Entscheid E4).

### 2.2 `ShopAufSet` Typ VR/VZ: `Sttxt`, `Arbpl`, `Vornr`, `Avo`

- Neue Properties `Sttxt` (100), `Arbpl` (8), `Vornr` (4), `Avo` (1, filterbar); Strukturfelder `STTXT TEXT100`, `ARBPL ARBPL`,
  `VORNR CHAR4`, `AVO CHAR1`.
- Filterschleife: `WHEN 'AVO'. lv_sa_avo = to_upper( <ls_sa_o>-low ).`
- Nach dem Paging, vor `copy_data_to_ref` (Zeile 773): fuer die Zeilen Typ VF der Seite die Statuszeile gesammelt (AUFK, JSTO, JEST je
  `FOR ALL ENTRIES`, TJ02/TJ02T(D)/TJ04 je einmal), Bildung genau wie `status_lang`; mit `lv_sa_avo = 'X'` zusaetzlich je Auftrag
  `Z_MD04_NEXT_OPERATION` (unveraendert der ZC22-Baustein) und Echo `Avo = 'X'`.
- Typ VP: alle vier Felder leer (wie ZC22). Die alten Typen FE/PA (Zweig ab Zeile 776) bleiben unberuehrt.
- Alternative A (`PERFORM status_lang IN PROGRAM zm_offene_fauf … IF FOUND`): genau die ZC22-Routine, aber `sy-langu` (1.4) und
  1,77 ms je Auftrag; nur als Entscheid E2.

---

## 3. Ausfuehrungsplan Phase 2 (T76, Mandant 100)

### 3.0 Rahmen

- Auftrag **`T76K912906`** (Workbench, Ziel P76, Text „Shopfloor: CoPaSumSet Hzdat, ShopRm/ShopAuf Filter“), Aufgabe `T76K912907`.
  Vor jedem Sichern im Dialog `KO008-TRKORR` ausdruecklich auf `T76K912906` setzen und vorher auslesen. Nicht freigeben.
- Eigene Sitzung: `SapGuiT76Hilfe.vbs neu` (vorher/nachher `info`, genau eine neue Sitzung), alle Bildschritte mit
  `SapGuiT76Hilfe.vbs` (Id-geprueft). Fuer SE24 `SapGuiKlasseAendernMitAuftrag.vbs` und `SapGuiKlassenMethodeQuelltext.vbs` nur mit dem
  Index aus `neu` und unmittelbar davor `info` (diese Skripte pruefen die Id nicht). Am Ende `zu`.
- Quelltexte als ganze Methoden aus dem Live-Stand bauen (Skript wie `saptasks/copa_verkauf/methoden_bauen_hzdat_2026-11-13.py`: nur
  Einfuegungen, jeder Anker genau einmal, ASCII, CRLF). Dateien in diesem Ordner:
  `MPC_EXT_DEFINE_vorher_<Datum>_layout.abap`, `…_neu_…`, `DPC_EXT_GET_ENTITYSET_vorher_<Datum>_layout.abap`, `…_neu_…`.
- Im Dialog „Fehler beim Aktivieren“ nur `Abbrechen`; Fehlerliste in SE38 steht im Grid
  `wnd[0]/shellcont/shell/shellcont[0]/shell/shellcont[0]/shell` (`SapGuiT76Hilfe.vbs grid`).

### 3.1 Schritt 0: Vorpruefung (nur lesen)

| Pruefung | Erwartet | sonst |
| --- | --- | --- |
| `E070` `T76K912906`, `T76K912907` | `TRSTATUS = 'D'` | anhalten (freigegeben: neuer Auftrag nur nach Entscheid Ingo) |
| `E071` `T76K912907` | genau `LIMU TABD ZSTR_COPA_SUM`, `LIMU METH … MPC_EXT DEFINE`, `LIMU METH … DPC_EXT …GET_ENTITYSET` | anhalten |
| `TLOCK` `HIKEY LIKE 'ZSTR_SHOP%'` | leer | anhalten (fremder Auftrag) |
| `abap-read …MPC_EXT==CM001` / `…DPC_EXT==CM01W` | zeilengleich mit `saptasks/copa_verkauf/*_neu_2026-11-13.abap` (3'027 / 2'737) | anhalten, Unterschied zeigen |
| `DD03L` `ZSTR_SHOP_RM` / `ZSTR_SHOP_AUF` | 6 / 8 Felder wie 1.6 | anhalten |
| Vorher-Werte Gateway (eigene Sitzung) | siehe 3.7 Spalte „vorher“ | Abweichung notieren, gegen ZC27/ZC22 neu messen |

### 3.2 Schritt 1: SE11 `ZSTR_SHOP_RM` (Zeile 6, nullbasiert)

`/nSE11`, Radiobutton `RSRD1-DDTYPE`, `ZSTR_SHOP_RM`, F6. Zeile 5 muss `ANZAHL` zeigen, Zeile 6 leer, sonst anhalten.
`txtDD03P_D-FIELDNAME[0,6]` = `MATEX`, `ctxtDD03P_D-ROLLNAME[2,6]` = `CHAR40`, Enter, Sichern (Auftragsdialog: `T76K912906`),
Aktivieren (`tbar[1]/btn[27]`). Rueckleseprobe `DD03L`: 7 Felder, `MATEX,0007,CHAR40,CHAR,000040,A`.

### 3.3 Schritt 2: SE11 `ZSTR_SHOP_AUF` (Zeilen 8 bis 11)

Zeile 7 muss `MEINS` zeigen, 8 leer (sichtbare Zeilen vorher per `dump` pruefen; reicht die Sicht nicht, anhalten statt blaettern).

| Zeile | `FIELDNAME` | `ROLLNAME` |
| --- | --- | --- |
| 8 | `STTXT` | `TEXT100` |
| 9 | `ARBPL` | `ARBPL` |
| 10 | `VORNR` | `CHAR4` |
| 11 | `AVO` | `CHAR1` |

Enter, Sichern (`T76K912906`), Aktivieren. Rueckleseprobe `DD03L`: 12 Felder, Positionen 0009 bis 0012 wie oben, Typen CHAR 100 / 8 /
4 / 1, die acht alten unveraendert. Danach `E071` `T76K912907`: zusaetzlich `LIMU TABD ZSTR_SHOP_RM` und `LIMU TABD ZSTR_SHOP_AUF`
(oder `R3TR TABL`; beides ist ein Transport der Struktur).

### 3.4 Schritt 3: MPC `DEFINE`, zwei Einfuegungen

(a) Entity `ShopAuf`: direkt vor der einzigen Zeile mit `bind_structure( iv_structure_name = 'ZSTR_SHOP_AUF'` (heute 973), danach
eine Leerzeile:

```abap
* ZC22-Layoutfelder (saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md): Sttxt immer bei Typ VF (VR/VZ),
* Arbpl/Vornr nur mit Filter Avo eq 'X'
  lo_property = lo_entity_type->create_property( iv_property_name = 'Sttxt' iv_abap_fieldname = 'STTXT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 100 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Arbpl' iv_abap_fieldname = 'ARBPL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Vornr' iv_abap_fieldname = 'VORNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Avo' iv_abap_fieldname = 'AVO' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).
```

(b) Entity `ShopRm`: direkt vor der einzigen Zeile mit `bind_structure( iv_structure_name = 'ZSTR_SHOP_RM'` (heute 2637), danach
eine Leerzeile:

```abap
* Matex: optionaler Materialausschluss wie ZC27-Layout /NIN, Musterliste (PLAN_LAYOUTFILTER_2026-11-13)
  lo_property = lo_entity_type->create_property( iv_property_name = 'Matex' iv_abap_fieldname = 'MATEX' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).
```

Erwartete Zeilenzahl: 3'027 + 27 + 8 = 3'062 (genau nach dem Bau-Skript nachzaehlen, die Zahl hier ist eine Rechnung).

### 3.5 Schritt 4: DPC `GET_ENTITYSET`, acht Einfuegungen (Zeilennummern heute)

**ShopRm (a)** nach Zeile 1644 `          lv_rm_typcd TYPE char10.`:

```abap
*   ZC27-Layout /NIN nachbilden: optionaler Materialausschluss Matex (PLAN_LAYOUTFILTER_2026-11-13)
    TYPES: BEGIN OF ty_rm_mat,
             aufnr TYPE afpo-aufnr,
             matnr TYPE afpo-matnr,
           END OF ty_rm_mat.
    DATA: lt_rm_mat   TYPE SORTED TABLE OF ty_rm_mat WITH UNIQUE KEY aufnr,
          ls_rm_mat   TYPE ty_rm_mat,
          lt_rm_teil  TYPE STANDARD TABLE OF string,
          lt_rm_mx    TYPE STANDARD TABLE OF string,
          lv_rm_mx    TYPE string,
          lv_rm_aus   TYPE abap_bool,
          lv_rm_matex TYPE char40.
```

**ShopRm (b)** nach Zeile 1666 `            APPEND ls_rm_typ TO lr_rm_typ.` (vor `WHEN OTHERS.`):

```abap
          WHEN 'MATEX'.
            lv_rm_matex = <ls_rm_o>-low.
            SPLIT <ls_rm_o>-low AT ',' INTO TABLE lt_rm_teil.
            LOOP AT lt_rm_teil INTO lv_rm_mx.
              CONDENSE lv_rm_mx.
              IF lv_rm_mx IS NOT INITIAL.
                APPEND lv_rm_mx TO lt_rm_mx.
              ENDIF.
            ENDLOOP.
```

**ShopRm (c)** nach Zeile 1717 `        ENDIF.` (Ende Typencode-Block, Anker: die Zeile nach `CHECK lv_rm_zztyp IN lr_rm_typ.`):

```abap

*       Materialausschluss wie ZC27-Standardlayout /NIN (E CP); Material wie ZC27: erste AFPO-Position (ZPP_RM_MENGEN 85-91, 104)
        IF lt_rm_mx IS NOT INITIAL.
          READ TABLE lt_rm_mat INTO ls_rm_mat WITH TABLE KEY aufnr = ls_rm_ru-aufnr.
          IF sy-subrc <> 0.
            CLEAR ls_rm_mat.
            ls_rm_mat-aufnr = ls_rm_ru-aufnr.
            SELECT matnr INTO ls_rm_mat-matnr FROM afpo UP TO 1 ROWS
              WHERE aufnr = ls_rm_ru-aufnr ORDER BY PRIMARY KEY.
            ENDSELECT.
            INSERT ls_rm_mat INTO TABLE lt_rm_mat.
          ENDIF.
          lv_rm_aus = abap_false.
          LOOP AT lt_rm_mx INTO lv_rm_mx.
            IF ls_rm_mat-matnr CP lv_rm_mx.
              lv_rm_aus = abap_true.
              EXIT.
            ENDIF.
          ENDLOOP.
          CHECK lv_rm_aus = abap_false.
        ENDIF.
```

**ShopRm (d)** nach Zeile 1743 `          ls_rm_out-typcd = lv_rm_typcd.`:

```abap
          ls_rm_out-matex = lv_rm_matex.
```

**ShopAuf (a)** nach Zeile 678 `          ls_sa_vz TYPE ty_sa_vz.`:

```abap
*   ZC22-Layoutfelder (PLAN_LAYOUTFILTER_2026-11-13): Statuszeile wie ZC22 FORM status_lang, Sprache D fest
    TYPES: BEGIN OF ty_sa_ao,
             aufnr TYPE aufk-aufnr,
             objnr TYPE aufk-objnr,
           END OF ty_sa_ao,
           BEGIN OF ty_sa_st,
             linep TYPE tj04-linep,
             statp TYPE tj04-statp,
             txt04 TYPE tj02t-txt04,
           END OF ty_sa_st.
    DATA: lv_sa_avo   TYPE char1,
          lt_sa_ao    TYPE SORTED TABLE OF ty_sa_ao WITH UNIQUE KEY aufnr,
          ls_sa_ao    TYPE ty_sa_ao,
          lt_sa_af    TYPE STANDARD TABLE OF ty_sa_ao,
          lt_sa_jsto  TYPE SORTED TABLE OF jsto WITH UNIQUE KEY objnr,
          ls_sa_jsto  TYPE jsto,
          lt_sa_jest  TYPE SORTED TABLE OF jest WITH NON-UNIQUE KEY objnr,
          ls_sa_jest  TYPE jest,
          lt_sa_tj02  TYPE SORTED TABLE OF tj02 WITH UNIQUE KEY istat,
          ls_sa_tj02  TYPE tj02,
          lt_sa_tj02t TYPE SORTED TABLE OF tj02t WITH UNIQUE KEY istat spras,
          ls_sa_tj02t TYPE tj02t,
          lt_sa_tj04  TYPE SORTED TABLE OF tj04 WITH UNIQUE KEY obtyp istat,
          ls_sa_tj04  TYPE tj04,
          lt_sa_st    TYPE STANDARD TABLE OF ty_sa_st,
          ls_sa_st    TYPE ty_sa_st,
          lv_sa_linep TYPE tj04-linep.
    FIELD-SYMBOLS <ls_sa_a> TYPE zstr_shop_auf.
```

**ShopAuf (b)** nach Zeile 687 `          WHEN 'TYP'. lv_sa_typ = <ls_sa_o>-low.`:

```abap
          WHEN 'AVO'. lv_sa_avo = to_upper( <ls_sa_o>-low ).
```

**ShopAuf (c)** vor der ersten Zeile `      copy_data_to_ref( EXPORTING is_data = lt_sa_out CHANGING cr_data = er_entityset ).` nach Zeile
695 (heute 773), also nach dem Paging:

```abap
*     ZC22-Layoutfelder (PLAN_LAYOUTFILTER_2026-11-13), nur fuer Fertigungsauftraege der ausgegebenen Seite:
*     Sttxt = ZC22 FORM status_lang (Sprache D fest, das Gateway laeuft nicht sicher deutsch), immer;
*     Arbpl/Vornr = Z_MD04_NEXT_OPERATION wie ZC22 FORM vorgangsdaten, nur mit Filter Avo eq 'X' (teuer).
      CLEAR lt_sa_af.
      LOOP AT lt_sa_out ASSIGNING <ls_sa_a> WHERE typ = 'VF'.
        ls_sa_ao-aufnr = <ls_sa_a>-aufnr.
        APPEND ls_sa_ao TO lt_sa_af.
      ENDLOOP.
      IF lt_sa_af IS NOT INITIAL.
        SELECT aufnr objnr FROM aufk INTO TABLE lt_sa_ao
          FOR ALL ENTRIES IN lt_sa_af WHERE aufnr = lt_sa_af-aufnr.
        IF lt_sa_ao IS NOT INITIAL.
          SELECT * FROM jsto INTO TABLE lt_sa_jsto
            FOR ALL ENTRIES IN lt_sa_ao WHERE objnr = lt_sa_ao-objnr.
          SELECT * FROM jest INTO TABLE lt_sa_jest
            FOR ALL ENTRIES IN lt_sa_ao
            WHERE objnr = lt_sa_ao-objnr AND inact = space AND stat LIKE 'I%'.
        ENDIF.
        SELECT * FROM tj02 INTO TABLE lt_sa_tj02 WHERE istat LIKE 'I%'.
        SELECT * FROM tj02t INTO TABLE lt_sa_tj02t WHERE istat LIKE 'I%' AND spras = 'D'.
        SELECT * FROM tj04 INTO TABLE lt_sa_tj04 WHERE istat LIKE 'I%'.
      ENDIF.
      LOOP AT lt_sa_out ASSIGNING <ls_sa_a> WHERE typ = 'VF'.
        READ TABLE lt_sa_ao INTO ls_sa_ao WITH TABLE KEY aufnr = <ls_sa_a>-aufnr.
        IF sy-subrc = 0.
          CLEAR: lt_sa_st, ls_sa_jsto.
          READ TABLE lt_sa_jsto INTO ls_sa_jsto WITH TABLE KEY objnr = ls_sa_ao-objnr.
          LOOP AT lt_sa_jest INTO ls_sa_jest WHERE objnr = ls_sa_ao-objnr.
            CLEAR ls_sa_st.
            READ TABLE lt_sa_tj02 INTO ls_sa_tj02 WITH TABLE KEY istat = ls_sa_jest-stat.
            IF sy-subrc = 0 AND ls_sa_tj02-nodis IS NOT INITIAL.
              CONTINUE.
            ENDIF.
            READ TABLE lt_sa_tj02t INTO ls_sa_tj02t WITH TABLE KEY istat = ls_sa_jest-stat spras = 'D'.
            IF sy-subrc = 0.
              ls_sa_st-txt04 = ls_sa_tj02t-txt04.
            ENDIF.
            READ TABLE lt_sa_tj04 INTO ls_sa_tj04 WITH TABLE KEY obtyp = ls_sa_jsto-obtyp istat = ls_sa_jest-stat.
            IF sy-subrc = 0.
              ls_sa_st-linep = ls_sa_tj04-linep.
              ls_sa_st-statp = ls_sa_tj04-statp.
            ENDIF.
            IF ls_sa_st-linep IS INITIAL.
              ls_sa_st-linep = '99'.
            ENDIF.
            IF ls_sa_st-statp IS INITIAL.
              ls_sa_st-statp = '99'.
            ENDIF.
            APPEND ls_sa_st TO lt_sa_st.
          ENDLOOP.
          SORT lt_sa_st BY linep statp txt04.
          CLEAR lv_sa_linep.
          LOOP AT lt_sa_st INTO ls_sa_st WHERE linep NE '99'.
            IF lv_sa_linep = ls_sa_st-linep.
              DELETE lt_sa_st INDEX sy-tabix.
            ELSE.
              lv_sa_linep = ls_sa_st-linep.
            ENDIF.
          ENDLOOP.
          LOOP AT lt_sa_st INTO ls_sa_st.
            IF <ls_sa_a>-sttxt IS INITIAL.
              <ls_sa_a>-sttxt = ls_sa_st-txt04.
            ELSE.
              CONCATENATE <ls_sa_a>-sttxt ls_sa_st-txt04 INTO <ls_sa_a>-sttxt SEPARATED BY space.
            ENDIF.
          ENDLOOP.
        ENDIF.
        IF lv_sa_avo = 'X'.
          CALL FUNCTION 'Z_MD04_NEXT_OPERATION'
            EXPORTING
              i_aufnr = <ls_sa_a>-aufnr
            IMPORTING
              e_vornr = <ls_sa_a>-vornr
              e_arbpl = <ls_sa_a>-arbpl.
          <ls_sa_a>-avo = 'X'.
        ENDIF.
      ENDLOOP.
```

Der Statusteil ist Zeile fuer Zeile die Variante B aus dem Testreport (gegen `status_lang` an 2'197 Auftraegen gleich, 1.8); neu ist
nur die Herkunft der Auftragsliste (`lt_sa_out` statt eigener Auswahl). Alle neuen Namen kommen in der Methode bisher nicht vor
(geprueft 13.11. per `grep` am Live-Stand). Erwartete Zeilenzahl DPC: 2'737 + 12 + 9 + 21 + 1 + 28 + 1 + 77 = 2'886 (Rechnung,
nach dem Bau-Skript nachzaehlen).

### 3.6 Schritt 5: Sichern, pruefen, aktivieren, zuruecklesen

MPC, dann DPC je: Klasse oeffnen (`SapGuiKlasseAendernMitAuftrag.vbs <ses> <Klasse> T76K912906`), Methode schreiben, Auftragsdialog
`T76K912906`, Syntaxpruefung („keine Syntaxfehler“, sonst anhalten), Klasse vom SE24-Einstieg aktivieren. Danach `abap-read` beider
Methoden, `diff` gegen `*_neu_…_layout.abap` rc 0 (Exitcode von `diff` selbst), gegen `*_vorher_…_layout.abap` nur Zugaenge. `E071`
`T76K912907`: genau die drei LIMU-Zeilen von Teil 1 plus die zwei Strukturen.

### 3.7 Schritt 6: Caches und Tests (Gateway Client, eigene Sitzung, Rumpf lesen)

Caches: `/IWBEP/CACHE_CLEANUP` alle Modelle, dann `/IWFND/CACHE_CLEANUP` `ZPOWERBI_EINKAUF_MDL`.

| Test | vorher (13.11. 13:49) | erwartet nachher |
| --- | --- | --- |
| `$metadata` | 200, 386'192 Byte (nach Teil 1) | 200, ShopRm mit `Matex`, ShopAuf mit `Sttxt`, `Arbpl`, `Vornr`, `Avo` |
| `ShopRmSet` `Werks eq '1100' and Arbpl eq 'DLQ72' and Ersda eq '20260414'` | 200, `Lmnga` 494, `Anzahl` 28 | gleich, `Matex` leer |
| dasselbe `and Matex eq 'E*,D*,F*,C*'` | 400 „Property Matex not found“ | 200, **394 / 21** (= ZC27 T76 mit `/NIN`), `Matex` = `E*,D*,F*,C*` |
| dasselbe mit `Matex eq 'e*,d*,f*,c*'` | – | 394 / 21 (CP ohne Gross/Klein) |
| `ShopRmSet` MLE04 14.04.2026 ohne / mit Matex | 579 / 34 | 579 / 34 und 579 / 34 |
| `ShopRmSet` MLE01 14.04.2026 | 197 / 6 | 197 / 6 |
| `ShopAufSet` VR `Werks eq '1100' and Datum eq '<heute>' and Typ eq 'VR'&$top=3` | 200, 1'254 Byte, Aufnr 1173719, 1213839, … | gleiche Aufnr/Menge/Datum; `Sttxt` bei VF gefuellt und **deutsch** (`FREI`, `TFRE`, nicht `REL`), `Arbpl`/`Vornr`/`Avo` leer |
| dasselbe `and Avo eq 'X'` | 400 „Property Avo not found“ | 200; 1213839: `Vornr` 0800, `Arbpl` MLE04, `Avo` X |
| `ShopAufSet` VZ `… Typ eq 'VZ'&$top=3` | 200 | 200, `Sttxt` gefuellt |
| `CoPaSumSet` mit `Hzdat le '20260315'` (Teil 1), `QmMeldungSet?$top=2`, `ShopProdSet` | 200 | 200 unveraendert |
| ST22 „Heute“ | – | keine Kurzdumps |

**Vollstaendiger Vergleich (`$TMP`-Testreport, danach loeschen):** DPC `get_entityset` direkt wie `Z_KOI_SHOP_VR_TEST.abap` (VR,
Werk 1100, ohne und mit `Avo`), dazu ZC22 per `SUBMIT ZM_OFFENE_FAUF` mit `P_AVO = 'X'`, ohne Disponentenfilter oder je Gruppe
(`cl_salv_bs_runtime_info`). Erwartet: Zeilen, Typ und Menge ohne `Avo` gleich vorher (vor Schritt 1 mit demselben Report messen);
je Fertigungsauftrag `Sttxt` = ZC22 `STTXT`, mit `Avo` `Arbpl`/`Vornr` = ZC22 `ARBPL`/`VORNR`, 0 Abweichungen; Status-Muster 63,
`Arbpl` M* 165 (Stand 13.11.; aendert jemand in T76 Auftraege, gilt der ZC22-Vergleich, nicht diese Zahl). Laufzeit im Gateway Client
je Seite messen und die Zahl der VF-Zeilen der Seite dazuschreiben: Die VR-Menge hat in T76 rund 11'000 Zeilen (2'197 Fertigungs-,
gut 8'700 Planauftraege, nach Datum gemischt), eine Seite `$top=5000` enthaelt also nur einen Teil der Fertigungsauftraege. Erwartet
je Seite ohne `Avo` rund 0,05 ms je VF-Zeile mehr als vorher, mit `Avo` rund 7,4 ms je VF-Zeile; ueber alle Seiten zusammen mit `Avo`
rund 16 s.

### 3.8 Rueckbau (nur auf Anweisung Ingos)

Der Auftrag ist nicht freigegeben. Methoden: `*_vorher_…_layout.abap` per `SapGuiKlassenMethodeQuelltext.vbs` zurueckschreiben
(stellt den Stand nach Teil 1 her), aktivieren, zuruecklesen. Strukturen: in SE11 die neuen Zeilen loeschen (`MATEX`; `STTXT`,
`ARBPL`, `VORNR`, `AVO`), aktivieren (nur der DPC verwendet sie, 1.6). Danach Caches wie 3.7. Die Strukturzeilen bleiben im Auftrag;
sie transportieren dann den alten Stand.

---

## 4. Risiken

1. **Laufzeit mit `Avo` in P76 unbekannt.** T76 7,4 ms je Fertigungsauftrag. Bei mehreren tausend Auftraegen auf einer Seite sind
   das 20 bis 40 s je Gateway-Aufruf (Vermutung). Grenzen: Cockpit-Zeitlimit 5 Minuten (1.7), Dialoggrenze 600 s
   (`rdisp/scheduler/prio_high/max_runtime`, `docs/rag/LEARNINGS.md` 2026-10-12); ob die ICM-Zeitgrenze fuer HTTP kuerzer ist, ist
   nicht gelesen. Deshalb nach dem Import in P76 zuerst im Gateway Client `Typ eq 'VR' and Avo eq 'X'&$top=1` messen (Ingo, nur GET),
   erst dann `Shopfloor:Zc22ArbplAusschluss` setzen.
2. **Statuszeile ohne Anforderung:** jede VR/VZ-Abfrage rechnet sie (T76 0,05 ms je Auftrag). Will man das vermeiden, braucht das
   Cockpit einen Anforderungsfilter (Aenderung im C#, Entscheid E3).
3. **ZC22 selbst bleibt sprachabhaengig** (`sy-langu`, Rueckfall D). Wer ZC22 englisch faehrt, sieht englische Kuerzel; das Set
   liefert immer deutsche. Fuer den Vergleich mit dem Excel der Teamleiter (deutsch) ist D richtig.
4. **Materialmuster gegen interne Nummer** (2.1, E4).
5. **Ein Aufruf = ein ZC27-Lauf** (1.1): Das Cockpit fragt die Arbeitsplatzgruppen schon getrennt ab; wer im Test Arbeitsplaetze
   zusammenlegt, bekommt andere Zahlen, ohne dass `Matex` schuld ist.

## 5. Offene Entscheide und Fragen (vor Phase 2)

| Nr. | Frage | Empfehlung |
| --- | --- | --- |
| E1 | Beides in `T76K912906` zusammen mit `Hzdat` (so der Auftrag) oder `T76K912906` zuerst freigeben und Teil 2 in einen neuen Auftrag? Dieselben zwei Methoden gehen als ganze Methoden mit; getrennt geht es nur nacheinander. | wie beauftragt in `T76K912906`; Hzdat wartet dann auf Teil 2 |
| E2 | Statuszeile gesammelt mit Sprache D (B) oder `PERFORM status_lang IN PROGRAM zm_offene_fauf … IF FOUND` (A, `sy-langu`)? | B (1.4, 1.8) |
| E3 | `Sttxt` immer (Cockpit-Vertrag `768ede8`) oder nur auf Anforderung? | immer, Kosten klein |
| E4 | Materialmuster gegen interne `MATNR` (reicht fuer `E*`/`D*`/`F*`/`C*`) oder Ausgabeform (`CONVERSION_EXIT_MATN1_OUTPUT`, zusaetzliche Kosten)? | intern |
| E5 | `Vornr` mitliefern (kostenlos aus demselben Baustein, ZC22 zeigt ihn auch)? | ja |
| F1 | Fahren die Teamleiter ZC22 mit „Mit Vorgangsdaten“? Nur dann wirkt der Arbeitsplatzteil (`P76_BEFUND` 4 Punkt 3). | Fabio fragen; betrifft nur den Cockpit-Schalter |
| F2 | Cockpit: `Avo`-Echo pruefen? `PruefeFelder` prueft nur, ob `Arbpl` als Eigenschaft da ist; nach dem Import ist sie immer da, auch leer. | `Avo = 'X'` bei Typ VF pruefen (Hauptsitzung, C#) |

## 6. Kandidaten fuer `docs/rag/LEARNINGS.md` (nicht eingetragen)

- Das Gateway laeuft nicht verlaesslich deutsch, auch im T76-Gateway-Client mit deutsch angemeldeter Sitzung: `Meins` kam als `PC`
  statt `ST`, und Fehlerantworten wechselten von Aufruf zu Aufruf zwischen `"lang" : "en"` und `"de"`. Texte und Kuerzel ueber
  `sy-langu` (TJ02T, Einheiten) sind damit zufaellig englisch; im DPC immer mit fester Sprache lesen (wie `QmMeldungSet`).
- SE38: Das Ergebnis von „Pruefen“ steht in einem angedockten Grid `wnd[0]/shellcont/shell/shellcont[0]/shell/shellcont[0]/shell`
  (Zeile, Text), lesbar mit `SapGuiT76Hilfe.vbs grid`; die Statusleiste bleibt dabei leer. Damit ist der Fehler hinter „Fehler beim
  Aktivieren“ ohne Screenshot lesbar.
- Wiederholt (Fall 2026-10-20): `SapGuiT76Hilfe.vbs set … ''` mit leerem Argument brach ab („Index ausserhalb des gueltigen
  Bereichs“), das Feld `S_ERSDA-HIGH` blieb auf dem Vorschlag 13.11.2026; der erste ZC27-Lauf lief deshalb ueber 14.04. bis 13.11.
  (90 statt 34 Zeilen) und wurde verworfen. Ein Bis-Datum immer setzen, nie leeren.
- ABAP: `LOOP AT itab … WHERE comp1 <> comp2` (zwei Komponenten derselben Zeile) ist hier ein Syntaxfehler („Das Feld STTB ist
  unbekannt“); im Rumpf mit `CHECK` vergleichen.
- Vollbild-ALV (`REUSE_ALV_GRID_DISPLAY`): Das Grid hat keine eigene Toolbar (`ToolbarButtonCount` 0); „Filter loeschen“ ist
  `wnd[0]/tbar[1]/btn[38]`.
