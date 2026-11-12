# Plan ZC12 (`ZM_ABGLEICH_KTSCH`): Wert 0 bei Vorgabewerten, Popup-Ausrichtung, Laufzeit (Phase 1, 2026-11-12)

Stand: 2026-11-12 vormittags, Agent sap-t76, Auftrag der Hauptsitzung (PM-02, Auftrag Ingo 12.11.). Phase 1 von 2: Diese
Datei ist der Ausfuehrungsplan fuer Phase 2. **In Phase 1 wurde in SAP nichts geaendert**: kein Auftrag angelegt, nichts
gesichert, nichts aktiviert, keine GUI-Sitzung geoeffnet. P76 wurde weder gelesen noch beruehrt.

Zugang: ausschliesslich RFC `.tmp_sap_probe/RunSapProbe.ps1` (DPAPI-Ablage T76, `Test-SapPassword`: vorhanden seit
10.09.2026), nur `table-read`, `abap-read` und `system-info`. Alle Werte stammen aus T76 Mandant 100 (Datenkopie etwa
Fruehjahr 2026). Fuer P76 sind sie Hinweise, nicht Belege.

Zeilennummern: „T76-Zeile“ meint `saptasks/zc12/ZM_ABGLEICH_KTSCH_T76_2026-11-12.abap`, „Entwurf-Zeile“ meint
`saptasks/zc12/ZM_ABGLEICH_KTSCH_entwurf_2026-11-12.abap`. Bei Standard-Includes ist die Zeile im Include gemeint (ohne die
sechs Kopfzeilen der SapProbe-Ausgabe).

**Entscheid Ingo (ueber die Hauptsitzung, 12.11.):** kein neuer Sekundaerindex auf SAP-Standardtabellen (PLPO, MAPL usw.),
nur im Notfall. Alle Laufzeitmassnahmen in diesem Plan liegen deshalb im Report selbst. Der Index erscheint nur noch in
Abschnitt 8 als Notfalloption.

---

## 0. Kurzfassung

1. **T76 entspricht genau der Repo-Datei `_vorher.abap`.** `diff` Exitcode 0, 1941 Zeilen, 58'852 Bytes. Die
   `p_debug`-Fassung `_nachher.abap` ist nie eingespielt worden (`diff` Exitcode 1, 15 Zeilen Unterschied).
2. **Problem 1 (Wert 0)** liegt allein an der Pruefung in `sichern_zeiten` (T76-Zeilen 1355 bis 1361). `fmt_quan` macht
   aus 0 den Text `'0'` und nicht etwa einen leeren Wert; der BDC wuerde die alte Zahl also ueberschreiben. Die Aenderung ist
   klein und steht im Entwurf.
3. **Problem 2 (Ausrichtung) laesst sich nicht so loesen, wie im Auftrag vermutet.** Der Baustein `POPUP_GET_VALUES`
   formatiert Mengenfelder selbst mit `WRITE ... UNIT` (Include `LSPO4F60`), jede Vorformatierung im Report wird
   ueberschrieben. Mit der Einheit `MIN` (0 Dezimalen) entsteht vermutlich genau das „mittige“ Bild. Es gibt drei echte
   Wege, die Ingo entscheiden muss (E3).
4. **Laufzeit:** Die PLPO-Abfrage auf `KTSCH` ist nach allem, was lesend messbar war, **nicht** der Haupttreiber (HANA,
   Spaltenspeicher; das Lesen von 21'568 Saetzen ueber RFC dauerte samt Anmeldung 2,4 s). Die Hebel liegen im ABAP:
   lineare Suchen in verschachtelten Schleifen (rund 100 Mio. Vergleiche fuer KTSCH `07201`, gerechnet), `READ_TEXT`
   je Zeile statt je Vorgang (bei Sammelplaenen bis 7'719-fach), und beim Sichern ein CA02-Lauf je Material, auch wenn
   viele Materialien denselben Vorgang teilen.

---

## 1. Ist-Stand mit Belegen

### 1.1 Quelltext und Programmeintrag

| Pruefung | Ergebnis | Beleg |
| --- | --- | --- |
| Quelltext T76 gegen `_vorher.abap` | identisch, `diff` Exitcode 0 (selbst ausgewertet, nicht ueber `head`) | `abap-read ZM_ABGLEICH_KTSCH`, sechs Kopfzeilen der SapProbe-Ausgabe entfernt, gespeichert als `ZM_ABGLEICH_KTSCH_T76_2026-11-12.abap` |
| Quelltext T76 gegen `_nachher.abap` | verschieden, `diff` Exitcode 1, 15 Zeilen | `p_debug` ist weiterhin auskommentiert (T76-Zeile 195) |
| TRDIR | `CNAM I001067`, `CDAT 20030303`, `UNAM KOI`, `UDAT 20260527`, `SDATE 20260527`, `STIME 062102`, `VERN 000077`, `SUBC 1`, `RSTAT K`, `FIXPT X`, `UCCHECK X` | `table-read TRDIR --where "NAME = 'ZM_ABGLEICH_KTSCH'"` |
| TADIR | `R3TR PROG ZM_ABGLEICH_KTSCH`, Paket `ZPP1` | `table-read TADIR` |
| Dynpros | 0100, 0900, 1000 (Selektionsbild) | `D020S PROG = 'ZM_ABGLEICH_KTSCH'` |
| Aufruf der Dynpros | nur `call screen 100` (T76-Zeile 309); 0900 und die Form `run_debug` werden nirgends aufgerufen | `grep` im Quelltext |

### 1.2 Transporte und Sperren

Alle Auftraege mit dem Objekt sind freigegeben (`E070 TRSTATUS R`), bis auf einen:

| Auftrag | Typ | Status | Besitzer | Datum | Text | Inhalt zum Objekt |
| --- | --- | --- | --- | --- | --- | --- |
| `T76K911958` | K | **D (aenderbar)** | KOI | 11.05.2026 | `zv` | — |
| `T76K911959` (Aufgabe) | S | **D** | KOI | 11.05.2026 | — | `LIMU DYNP ZM_ABGLEICH_KTSCH 0900` |

`TLOCK` zeigt genau eine Sperre: `PROG`, `HIKEY ZM_ABGLEICH_KTSCH ... DYNP0900`, Auftrag `T76K911959`. Der Quelltext
(`REPS`) ist also nicht gesperrt; das Dynpro 0900 schon. Weil 0900 nicht aufgerufen wird und der Plan es nicht anfasst,
stoert die Sperre Phase 2 nicht. Was mit dem alten offenen Auftrag geschieht, entscheidet Ingo (E7).

Letzte freigegebene Auftraege mit Quelltext (`LIMU REPS`): `T76K912063` (KOI, 27.05.2026), davor `T76K911927` (LAA,
18.05.2026). Der Struktur-Auftrag fuer `ZSKTSCH_TIME_PO` ist `T76K912063` (freigegeben).

### 1.3 Datentypen

| Objekt | Wert | Beleg |
| --- | --- | --- |
| `PLPO-VGW01..04` | Datenelement `VGWRT`, Domaene `VGWRT`, `QUAN` Laenge 9, **3 Dezimalen**, intern `P` Laenge 5, Ausgabelaenge 11, ohne Vorzeichen, kein Konvertierungsexit | DD03L, DD04L, DD01L |
| Referenzfeld | `VGW0n` → `PLPO-VGE0n` | DD03L `REFTABLE/REFFIELD` |
| `PLPO-VGE01..04` | Datenelement `VGWRTEH`, Domaene `MEINS`, `UNIT` 3 | DD03L |
| `PLPO-KTSCH` | Datenelement `KTSCH`, `CHAR 7` | DD03L |
| `ZSKTSCH_TIME_PO` | `CHG01..04`, Datenelement `XFELD`, `CHAR 1`; Paket `ZPP`, angelegt KOI 26.05.2026, transportiert in `T76K912063`; kein Eintrag in `D010TAB` (nur dynamisch ueber den Namen im Popup benutzt) | DD03L, TADIR, E071, D010TAB |
| Einheiten in den Daten | KTSCH `07201`: 19'670 von 21'567 Vorgaengen `MIN,MIN,MIN,MIN`, 1'876 `MIN,MIN,MIN,leer`, 8 `H,MIN,MIN,H` | PLPO `VGE01..04` |
| T006 | `MIN` DECAN 0, `H` DECAN 3, `STD` DECAN 1 | T006 |

Hinweis zur Fachsprache: Ingo spricht von Arbeitsstunden, in den T76-Daten sind die Vorgabewerte ganz ueberwiegend in
Minuten gepflegt. Fuer den Code ist das gleichgueltig, fuer die Testwerte nicht (Abschnitt 6 nimmt `MIN`).

### 1.4 System und Tabellen

* Datenbank **SAP HANA** (`system-info`: `DB System HDB`, Kernel 793).
* `PLPO`, `MAPL`, `PLAS`, `STXH`, `STXL`, `MAKT`, `MARA`, `MARC`, `CRHD`: alle **Spaltenspeicher** (`DD09L ROWORCOLST C`),
  keine Tabellenpufferung.

### 1.5 Indizes (laut DDIC; der physische Stand auf der Datenbank ist nicht gelesen)

| Tabelle | Primaerschluessel (DD03L KEYFLAG) | Sekundaerindizes (DD12L/DD17S, aktiv) |
| --- | --- | --- |
| PLPO | MANDT, PLNTY, PLNNR, PLNKN, ZAEHL | `A` (AENNR), `B` (ARBID), `HWP` (KNOBJ), `HJI` (VPLNR), **`Z01` (KTSCH)** |
| PLAS | MANDT, PLNTY, PLNNR, PLNAL, PLNFL, PLNKN, ZAEHL | `A` (AENNR) |
| MAPL | MANDT, MATNR, WERKS, PLNTY, PLNNR, PLNAL, ZKRIZ, ZAEHL | **keine** |
| STXH | (Standard) | `H66` (TDREFNAME) |
| CRHD | MANDT, OBJTY, OBJID | nicht weiter geprueft |

**Den Index auf `KTSCH` gibt es bereits im DDIC, aber ausdruecklich nicht auf der Datenbank.** `DD12L` fuer `PLPO Z01`:
zuletzt geaendert bzw. aktiviert von `DDIC` am 17.09.2024 um 10:12:51 (Aenderungsstempel `AS4USER/AS4DATE/AS4TIME`, nicht
das Anlagedatum; der Index stand schon im alten Auftrag `T76K901131`), `DBSTATE O`; die Festwerte der Domaene `DDIXDBSTAT` (DD07T) sagen dazu
„Nicht auf der Datenbank anlegen“. Text (DD12T): „Index auf Vorlagentext“. Dasselbe gilt fuer `PLKO Z01` (WERKS, ARBID).
Transportiert war er in `T76K901131` und `SAPKCCC750` (E071 `LIMU INDX PLPO Z01`). *Vermutung:* Der Status O stammt aus der
HANA-Umstellung, bei der Sekundaerindizes auf Spaltentabellen ueblicherweise abgeschaltet werden; das Datum passt, ist
aber nicht belegt.

### 1.6 Mengen in T76

| Groesse | Wert | Beleg |
| --- | --- | --- |
| PLPO gesamt | 999'972 (PLNTY `N` 999'971, `Q` 1) | `table-read PLPO --fields PLNTY`, CSV gezaehlt |
| PLPO mit KTSCH | 998'133, alle PLNTY `N`, alle `LOEKZ` leer | dito, `KTSCH > ' '` |
| verschiedene KTSCH | 10'345; Median 4 Vorgaenge, 90 % ≤ 122, Maximum 21'567 | dito |
| groesste KTSCH | `07201` 21'567, `05000` 18'694, `07202` 18'074, `WEAUTO` 15'873, `10591` 13'398 | dito |
| PLAS PLNTY `N` | 999'971 | `table-read PLAS` |
| MAPL PLNTY `N` | 61'053 (aktiv Werk 1100: 50'454); andere Plantypen: 2 Saetze `Q` | `table-read MAPL` |
| STXH Vorgangslangtexte | 789'425 (`ROUTING/PLPO`, alle Sprache D) | `table-read STXH` |

**Durchgerechnet fuer KTSCH `07201`, Werk 1100, PLNTY N** (lokal aus den gelesenen Tabellen verknuepft, so wie der Report
verknuepft, aber **ohne** den MARA/MARC-Filter auf Status 99 und ohne Typencode; die echte Zeilenzahl kann nur kleiner sein):

| Stufe im Report | Anzahl | Bemerkung |
| --- | --- | --- |
| `gt_plpo` (T76-Zeile 349) | 21'567 | |
| davon mit aktivem PLAS | 3'783 | 17'784 PLAS-Saetze haben `LOEKZ X` (Vorgang aus der Folge geloescht, PLPO selbst nicht) |
| `gt_plas` | 3'783 | PLAS-ZAEHL = PLPO-ZAEHL in allen 21'567 Faellen, der Join-Schluessel ist also richtig |
| verschiedene PLNNR/PLNAL | 3'768 | |
| `gt_mapl` (Werk 1100, aktiv) | 2'831 | |
| ALV-Zeilen | **2'846** | |
| verschiedene CA02-Ziele (PLNNR/PLNAL/PLNKN/ZAEHL) | 2'846 | bei 07201 teilt kein Material einen Vorgang mit einem anderen |
| verschiedene Arbeitsplaetze (ARBID) | 122 | Treiber der CRHD-Abfrage heute: 21'567 Zeilen |
| Vorgaenge mit Langtext (STXH) | 3'783 von 3'783 | Vorabpruefung ueber STXH spart hier nichts |

**Sammelplaene vervielfachen die Zeilen.** Plangruppe `50038715/01` haengt in Werk 1100 an **7'719 Materialien**
(`50038716/01`: 774, `50044583/01`: 205, `50038717/01`: 163). Fuer alle 8'981 KTSCH mit Treffern in Werk 1100 gerechnet
(ALV-Zeilen gegen verschiedene CA02-Ziele, ohne Statusfilter):

| KTSCH | ALV-Zeilen | verschiedene Vorgaenge (Ziele) |
| --- | --- | --- |
| `STOP001` | 144'547 | 43 |
| `WEAUTO` | 16'622 | 15'783 |
| `05000` | 10'213 | 10'051 |
| `23360` | 7'762 | 43 |
| `28078` | 7'734 | 16 |
| `07201` | 2'846 | 2'846 |

87 KTSCH haben mehr Zeilen als Ziele; zusammen sind es 264'440 ueberzaehlige Zeilen. Fuer die typische KTSCH (Median 4
Vorgaenge) gibt es keinen Unterschied.

### 1.7 Pflegeregeln der Arbeitsplaetze (fuer die Frage „nimmt CA02 eine 0?“)

* Die 122 Arbeitsplaetze der KTSCH `07201` haben alle den Vorgabewertschluessel `ZAP1` (CRHD `VGWTS`).
* Die Pflegeregel je Vorgabewert steht in `CRHD-VGM01..06` (Domaene `AP_VGW_FLG`: leer = „keine Verprobung“, 0 = „darf
  nicht“, 1 = „soll nicht“, 2 = „soll“, 3 = „muss angegeben werden“; DD07T). Gelesen: die 122 Arbeitsplaetze von `07201`
  haben dort alle leer. Fuer ganz T76 je Feld einzeln gegengeprueft (`CRHD OBJTY = 'A' AND VGMnn IN ('0','1','2','3')`,
  n = 01 bis 04: je keine Treffer; Positivprobe mit derselben Schreibweise `VGWTS IN ('ZAP1','SAP1')` liefert Treffer). Es
  gibt in T76 also keinen Arbeitsplatz mit einer Pflegeregel fuer VGW01 bis VGW04.
* Arbeitsplatz des Testfalls T1: `CRHD OBJID 10000695` = `ETS09`, Werk 1100, `VGWTS ZAP1`, `VGM01..04` leer.
* 0 als gespeicherter Vorgabewert ist in PLPO alltaeglich (z. B. `50021773` Vorgang 0081: `VGW01 0.000 MIN`).

**Vermutung, nicht belegt:** CA02 nimmt `0` fuer `PLPOD-VGWnn` ohne Fehlermeldung an. Belegt wird das erst durch Test T1
in Phase 2.

### 1.8 Dezimalformat der Benutzer

`USR01-DCPFM`: KOI `X` (1,234,567.89). Alle T76-Benutzer: 264 `X`, **64 leer** (1.234.567,89), 5 `Y` (1 234 567,89).
Bedeutung fuer Abschnitt 3.4.

### 1.9 Verhalten von `POPUP_GET_VALUES` (Quelltext gelesen, LEARNINGS 2026-11-09)

TFDIR: `POPUP_GET_VALUES` liegt in `SAPLSPO4`, Include `LSPO4U01`. Gelesen: `LSPO4U01`, `LSPO4F00`, `LSPO4F10`, `LSPO4F60`.

* **Anzeige (PBO, `FORM p100_show`, LSPO4F60 ab Zeile 252):** Bei internem Typ `P` wird der uebergebene Text zuerst in eine
  Packed-Variable gestellt (`<p> = svali-value.`, Zeile 325) und dann **vom Baustein selbst** formatiert. Fuer `QUAN` mit
  einem Referenzfeld, das ebenfalls im Popup steht (hier `PLPO-VGE0n`), geschieht das mit
  `WRITE <p> TO svald-value(svali-outputlen) UNIT wa_svali-value.` (Zeilen 363 bis 371). Die Feldlaenge auf dem Bild wird
  auf die Ausgabelaenge 11 gesetzt (Zeile 454).
* **Rueckgabe (PAI, `convert_and_update_svali`, LSPO4F10 ab Zeile 139):** Eine Eingabe wird ueber `conversion_input_new`
  (LSPO4F60 Zeile 69) mit `RS_CONV_EX_2_IN` gegen das DDIC-Feld `PLPO-VGW0n` gewandelt (Zeile 116); dabei gilt das
  Benutzerformat, Fehler werden als Meldung im Popup gezeigt (Zeile 145 ff.). Das interne Ergebnis geht mit
  `MOVE <f1> TO svali-value` (Zeile 184) zurueck, also immer mit Punkt als Dezimaltrenner. **Ein geleertes Feld** ergibt
  `CLEAR svali-value` (LSPO4F10 Zeile 204), `FIELDS-VALUE` ist dann leer.
* Folgerung fuer den Report: `vgwNN_neu = ls_field-value` (T76-Zeilen 1249, 1256, 1263, 1270) ist **unabhaengig vom
  Benutzerformat robust**, Komma-Benutzer eingeschlossen. Leer und 0 sind nur **vor** diesem Uebertrag unterscheidbar
  (`ls_field-value` leer gegen `'0.000'`).

---

## 2. Aenderung 1: Wert 0 bei gesetztem Haken

### 2.1 Ursache

T76-Zeilen 1355 bis 1361 (`sichern_zeiten`):

```abap
  if ( gv_chg01 = 'X' and ( vgw01_neu is initial or vge01_neu is initial ) ) or
     ( gv_chg02 = 'X' and ( vgw02_neu is initial or vge02_neu is initial ) ) or
     ( gv_chg03 = 'X' and ( vgw03_neu is initial or vge03_neu is initial ) ) or
     ( gv_chg04 = 'X' and ( vgw04_neu is initial or vge04_neu is initial ) ).
    message s368(00) with 'Angehaktes Paar: Wert UND Einheit erforderlich'.
    return.
  endif.
```

Ein Packed-Feld mit Wert 0 ist `initial`. Mit Haken und 0 bricht die Form deshalb vor dem ersten CA02-Aufruf ab, die
Meldung erscheint nur in der Statuszeile, und der alte Wert bleibt in PLPO. Das erklaert den Ausweg der Nutzer ueber 0.0001.

### 2.2 Liefert `fmt_quan` fuer 0 etwas Brauchbares? Ja, `'0'`.

Schritt fuer Schritt fuer `iv_val = 0.000` (T76-Zeilen 1529 bis 1570, `cv_str` ist `lv_xvgw`, `c` Laenge 14):

| Schritt | Zeile | Inhalt `cv_str` |
| --- | --- | --- |
| `cv_str = iv_val.` (P nach C: Punkt, rechtsbuendig) | 1538 | `'         0.000'` |
| `condense cv_str no-gaps.` | 1539 | `'0.000'` |
| Nullen hinten abschneiden, bis ein Zeichen ≠ `0` kommt (Laenge 5 → 4 → 3 → 2, Zeichen an Stelle 2 ist `.`) | 1542 bis 1556 | `'0.'` |
| abschliessenden Punkt entfernen | 1559 bis 1566 | `'0'` |

Zum Vergleich: 4.000 → `'4'`, 10.000 → `'10'` (die Schleife haelt am Punkt an, die Null vor dem Punkt bleibt), 0.200 →
`'0.2'`. `fmt_quan` liefert also **nie** einen leeren Text fuer 0, und `bdc_field 'PLPOD-VGW01' '0'` ueberschreibt den
alten Wert. Die Annahme im Projektstatus vom 14.08. (CA02 lehne die nackte 0 je nach Vorgabewertschluessel ab) ist durch
die Pflegeregeln in 1.7 unwahrscheinlicher geworden, aber erst mit Test T1 belegt.

### 2.3 Aenderung (im Entwurf umgesetzt)

**a) Popup: leer merken, bevor es 0 wird.** Neue globale Felder (Entwurf-Zeilen 125 bis 130):

```abap
data:
  gv_leer01 type c length 1,
  gv_leer02 type c length 1,
  gv_leer03 type c length 1,
  gv_leer04 type c length 1.
```

In `popup_get_zeiten` mit zuruecksetzen (Entwurf-Zeilen 1203/1204) und beim Zuruecklesen setzen, je Block eine Zeile vor
dem Uebertrag (Entwurf-Zeilen 1311, 1319, 1327, 1335), zum Beispiel:

```abap
  read table lt_fields into ls_field index 2.
  if ls_field-value is initial. gv_leer01 = 'X'. endif.
  vgw01_neu = ls_field-value.
```

**b) `sichern_zeiten`: nur die Einheit ist Pflicht** (ersetzt T76-Zeilen 1352 bis 1361, Entwurf-Zeilen 1416 bis 1443):

```abap
  if ( gv_chg01 = 'X' and vge01_neu is initial ) or
     ( gv_chg02 = 'X' and vge02_neu is initial ) or
     ( gv_chg03 = 'X' and vge03_neu is initial ) or
     ( gv_chg04 = 'X' and vge04_neu is initial ).
    message s368(00) with 'Angehakte Zeit: Einheit erforderlich'.
    return.
  endif.

* Leeres Wertfeld bei gesetztem Haken: Entscheid E2 (Ingo).
* Variante A (aktiv im Entwurf): leer gilt als 0, keine Pruefung.
* Variante B: Abbruch mit klarer Meldung - dann diesen Block einkommentieren:
*  if ( gv_chg01 = 'X' and gv_leer01 = 'X' ) or
*     ( gv_chg02 = 'X' and gv_leer02 = 'X' ) or
*     ( gv_chg03 = 'X' and gv_leer03 = 'X' ) or
*     ( gv_chg04 = 'X' and gv_leer04 = 'X' ).
*    message s368(00) with 'Angehakte Zeit ohne Wert: 0 eintragen'
*                          'oder Haken entfernen'.
*    return.
*  endif.
```

Begruendung fuer Variante A als Vorschlag: Das Popup ist mit den Werten der obersten markierten Zeile vorbelegt
(T76-Zeilen 1151 bis 1168). Ein leeres Feld entsteht nur, wenn der Nutzer den Wert selbst geloescht hat. Variante B ist
die vorsichtigere, kostet aber einen Klick mehr. Ingo entscheidet (E2).

`fmt_quan` bleibt in dieser Aenderung unveraendert (siehe 3.4 und E4).

### 2.4 Rechenbeispiel vorher/nachher

Vorgang `50000611/01`, Vorgang 0075 (KTSCH `13457`), PLPO heute `VGW01 4.000 MIN`. Eingabe im Popup: Haken 1 gesetzt,
Wert `0`, Einheit `MIN`, Haken 2 bis 4 entfernt.

| | Popup-Rueckgabe `ls_field-value` | `vgw01_neu` | Pruefung | BDC `PLPOD-VGW01` | PLPO `VGW01` danach |
| --- | --- | --- | --- | --- | --- |
| vorher | `'0.000'` | 0.000 (initial) | Abbruch „Wert UND Einheit erforderlich“ | — | 4.000 |
| nachher | `'0.000'` | 0.000 | bestanden (Einheit `MIN` da) | `'0'` | **0.000** (erwartet, Test T1) |

---

## 3. Aenderung 2: Vorschlagswerte im Popup linksbuendig

### 3.1 Befund: die im Auftrag vermutete Loesung wirkt nicht

Der Report uebergibt die Packed-Werte direkt (T76-Zeilen 1202, 1207, 1212, 1217: `_add 'PLPO' 'VGW01' 'Wert' '' vgw01_neu`).
Formatiert wird aber im Baustein (1.9): `<p> = svali-value` und `WRITE <p> TO svald-value(11) UNIT <Einheit>`. Folgen:

* `WRITE ... LEFT-JUSTIFIED` oder ein String-Template im Report aendern **nichts** an der Anzeige, weil der Text sofort
  wieder in eine Zahl zurueckgewandelt und neu formatiert wird.
* Schlimmer: Ein Text im Benutzerformat (`4,5` bei `DCPFM` leer oder `Y`, oder `1,234.000` mit Tausendertrenner) wuerde bei
  `<p> = svali-value` (LSPO4F60 Zeile 325) mit hoher Wahrscheinlichkeit einen Laufzeitfehler (`CONVT_NO_NUMBER`) ausloesen.
  Diesen Weg nicht gehen.

**Vermutung zur Ursache des „mittigen“ Bildes:** Mit `UNIT MIN` (T006 DECAN 0) laesst `WRITE` die drei Nullstellen nach
dem Komma weg, haelt aber die Ausrichtung der Dezimalstelle; in einem 11 Zeichen breiten Feld steht `4` dann etwa in der
Mitte. Pruefen in Phase 2 Schritt 1 (rein lesend: Popup oeffnen, Feldinhalt mit Leerzeichen auslesen, abbrechen).

### 3.2 Wege (Entscheid E3)

| Weg | Was | DDIC | Aufwand und Risiko |
| --- | --- | --- | --- |
| **3a** | Wertfelder im Popup auf vier neue `CHAR`-Felder legen (`ZSKTSCH_TIME_PO-VAL01..04`), Wert im Report mit `WRITE ... UNIT ... LEFT-JUSTIFIED NO-GROUPING` als Text vorbelegen, nach dem Popup selbst mit `RS_CONV_EX_2_IN` gegen `PLPO-VGW0n` zurueckwandeln (mit Fehlerbehandlung) | Erweiterung der **eigenen** Z-Struktur (kein SAP-Standard); Datenelement vor Anlage in DD04L pruefen (z. B. ein vorhandenes `CHAR`-Element der Laenge 15, ungeprueft) | mittel; zweites Objekt im Auftrag; die Eingabepruefung macht dann der Report statt der Baustein; F1/F4 zum Wertfeld entfallen |
| 3b | wie 3a, aber ohne DDIC-Aenderung auf vorhandene Standard-Textfelder (z. B. `SYST-MSGV1..4`, `CHAR 50`) | keine | klein, aber unsauber: breites Feld, F1 zeigt „Meldungsvariable“ |
| 3c | Ausrichtung so lassen | keine | keiner; Nutzer sehen weiter die Standardanzeige von SAP |

Vorschlag: 3a, falls Ingo die Ausrichtung wichtig ist; sonst 3c. Der Code fuer 3a ist **nicht** im Entwurf, weil er von E3
abhaengt. Skizze (nur nach Freigabe E3 einbauen):

```abap
* Vorbelegung (statt _add 'PLPO' 'VGW01' 'Wert' '' vgw01_neu)
  write vgw01_neu to lv_w1 unit vge01_neu left-justified no-grouping.
  _add 'ZSKTSCH_TIME_PO' 'VAL01' 'Wert' '' lv_w1.
* Rueckwandlung (statt vgw01_neu = ls_field-value)
  read table lt_fields into ls_field index 2.
  if ls_field-value is initial.
    gv_leer01 = 'X'. clear vgw01_neu.
  else.
    ls_tf-tabname = 'PLPO'. ls_tf-fieldname = 'VGW01'.
    call function 'RS_CONV_EX_2_IN'
      exporting input_external  = ls_field-value
                table_field     = ls_tf
      importing output_internal = vgw01_neu
      exceptions others = 1.
    if sy-subrc <> 0.
      message s368(00) with 'Ungültiger Wert Rüsten Masch:' ls_field-value.
      return.            " cv_ok bleibt leer, nichts wird geändert
    endif.
  endif.
```

### 3.3 Rueckschreiben bleibt heute schon robust

Fuer den heutigen Stand und fuer 3c gilt: `vgwNN_neu = ls_field-value` ist auch fuer Komma-Benutzer richtig, weil der
Baustein intern zurueckwandelt (1.9). Ingos Format ist `X` (Punkt), das Testen mit KOI deckt den Komma-Fall also nicht ab.

### 3.4 Nebenbefund: Dezimaltrenner im BDC (ungeprueft, Entscheid E4)

`fmt_quan` schreibt immer einen **Punkt** in `PLPOD-VGWnn` (2.2). Beim `CALL TRANSACTION ... USING` liest CA02 die Eingabe
nach den Einstellungen des aufrufenden Benutzers (*Annahme*, nicht getestet). Fuer die 69 T76-Benutzer mit Komma
(`DCPFM` leer oder `Y`) kaeme aus `0.2` vermutlich eine Fehlermeldung oder eine falsch gelesene Zahl; ganze Zahlen wie `0`
und `4` sind nicht betroffen. Ob ZC12 in P76 von Komma-Benutzern benutzt wird, ist nicht bekannt (P76 nicht gelesen).
Moegliche Abhilfe, nur nach E4:

```abap
form fmt_quan using iv_val type plpo-vgw01 changing cv_str type c.
  clear cv_str.
  write iv_val to cv_str left-justified no-grouping.   " Benutzerformat
endform.
```

Das ergibt `0.000` bzw. `0,000` und `4.500` bzw. `4,500`, passend zum Format desselben Benutzers. Testbar nur mit einem
T76-Benutzer mit Komma oder mit einer voruebergehenden Aenderung von KOI in SU3 (Entscheid Ingo).

---

## 4. Aenderung 3: Laufzeit

### 4.1 Hotspots im Quelltext, mit Mengen fuer KTSCH `07201` (gerechnet, nicht gemessen)

| Nr. | Stelle (T76-Zeilen) | Was passiert | Menge 07201 | Bewertung |
| --- | --- | --- | --- | --- |
| H1 | 438 bis 442: `loop at gt_plas ... where` in `loop at gt_plpo` | `gt_plas` ist Standardtabelle, jede der 21'567 Aussenzeilen prueft alle 3'783 Zeilen | 21'567 × 3'783 = **81,6 Mio.** Vergleiche | **groesster Hebel fuer 07201** |
| H2 | 444 bis 446: `loop at gt_mapl ... where` | je PLAS-Treffer alle MAPL-Zeilen | 3'783 × 2'831 = 10,7 Mio. | Hebel |
| H3 | 449 bis 457: `read table gt_mara/gt_makt with key` | lineare Suche je Zeile | rund 2'846 × 2'831 ≈ 8 Mio. (im Mittel die Haelfte) | Hebel |
| H4 | 498 bis 500: `read_vorgang_text` (darin `READ_TEXT`) je ALV-Zeile | ein Datenbankzugriff je Material × Vorgang | 2'846 Aufrufe fuer 2'846 Vorgaenge | bei 07201 **kein** Gewinn durch Puffern; bei `23360` 7'762 → ≤ 43, bei `STOP001` 144'547 → ≤ 43 |
| H5 | 482 bis 491: Vorlagentext je Zeile zusammenbauen | gleiche Schleife ueber `gt_ttab_o` je Zeile | 2'846 × Zeilen der Vorlage | klein |
| H6 | 425 bis 430: CRHD `for all entries in gt_plpo` | Treiber mit 21'567 Zeilen fuer 122 Arbeitsplaetze | 21'567 → 122 | klein bis mittel, je nach Paketgroesse der FAE |
| H7 | 375 bis 382: MAPL ohne `plnty` | MAPL-Schluessel beginnt mit MATNR, ohne MATNR hilft er nicht; auf HANA-Spaltenspeicher ohnehin Spaltensuche. Andere Plantypen in MAPL: nur 2 Saetze `Q` | — | **Konsistenz**, kaum Laufzeit |
| H8 | 361 bis 369: PLAS-FAE mit 21'567 Treibern, davon 17'784 auf geloeschte PLAS | Datenbankpakete fuer Saetze, die danach verworfen werden | 21'567 Treiber | mittel, optional P5 |
| H9 | 349 bis 355: PLPO `where ktsch` | ohne DB-Index, HANA-Spaltenspeicher | RFC-Lesen von 21'568 Saetzen samt Anmeldung und Uebertragung 2,4 s; Gegenprobe: 999'972 Saetze PLAS ueber RFC 54 s (Uebertragung) | **nicht** der Haupttreiber (Hinweis, kein SQL-Trace) |
| H10 | 1400 bis 1500 und 940 bis 1030: ein CA02 je markierter Zeile | bei Sammelplaenen derselbe Vorgang bis 7'719-mal | 07201: 2'846 Laeufe fuer 2'846 Ziele | bei 07201 kein Gewinn; bei Sammelplaenen sehr gross (optional P7) |
| H11 | 494: UUID je Zeile | `GUID` wird nirgends gelesen (nur technische Spalte, T76-Zeile 711) | 2'846 | vernachlaessigbar, bleibt |

Grobe Zeitschaetzung (ausdruecklich Schaetzung): Bei 5 bis 20 ns je Vergleich kosten H1 bis H3 zusammen rund 0,5 bis 2 s;
2'846 `READ_TEXT` bei 0,3 bis 1,5 ms je Aufruf rund 1 bis 4 s; ein CA02-Lauf im Hintergrund rund 0,3 bis 1 s, beim Sichern
von 2'846 Zeilen also 15 bis 45 Minuten. Belastbar wird das erst mit der Basismessung in Phase 2 Schritt 2.

### 4.2 Massnahmen im Entwurf (alle nur im Report, kein DDIC)

| Nr. | Massnahme | Entwurf-Zeilen | Erwarteter Nutzen |
| --- | --- | --- | --- |
| P1 | `gt_plas`, `gt_mapl`, `gt_mara`, `gt_makt`, `gt_crhd` als **sortierte** Tabellen mit Schluessel; `LOOP ... WHERE` auf die fuehrenden Schluesselfelder und `READ TABLE ... WITH TABLE KEY` werden dadurch binaer | 147 bis 154, 495, 513, 520 | H1 bis H3: von rund 100 Mio. auf rund 0,1 Mio. Vergleiche (gerechnet) |
| P2 | Vorgangstext je PLPO-Satz **einmal** lesen (beim ersten ausgegebenen Material), Ergebnis fuer weitere Materialien desselben Vorgangs wiederverwenden | 499, 551 bis 558 | H4: 07201 gleich, Sammelplaene bis 7'719-fach weniger `READ_TEXT` |
| P3 | Vorlagentext einmal vor der Schleife bauen | 481 bis 490, 544 | H5 |
| P4 | Eindeutige Treiber fuer FOR ALL ENTRIES: MAPL ueber `lt_plkey` (PLNNR/PLNAL) **mit `plnty = p_plnty`**, MARA ueber `lt_matnr`, CRHD ueber `lt_arbid` | 360 bis 377, 405 bis 418, 439 bis 449, 468 bis 478 | H6, H7; CRHD-Treiber 21'567 → 122 |

Unveraendert bleiben: die PLPO-Abfrage, die PLAS-Abfrage, `count_freq` (zaehlt weiter je ALV-Zeile, damit die Angabe
„IST haeufig“ gleich bleibt), die Reihenfolge der Aussenschleife ueber `gt_plpo`.

**Bekannte Verhaltensaenderung durch P1:** Innerhalb einer Plangruppe kommen die Materialien jetzt nach Materialnummer
sortiert (vorher in der Reihenfolge der Datenbank, die ohne `ORDER BY` auf HANA ohnehin nicht festgelegt ist). Die
Vorbelegung des Popups nimmt die oberste markierte Zeile (T76-Zeile 1151); bei mehreren markierten Zeilen mit
unterschiedlichen Werten kann sich die Vorbelegung deshalb aendern. Der Vergleich alt gegen neu muss als Mengenvergleich
laufen, nicht zeilenweise.

### 4.3 Optionale Massnahmen (nicht im Entwurf, je ein Entscheid)

* **P5, PLPO und PLAS in einer Abfrage (E5a).** `select ... from plpo as p inner join plas as a on a~plnty = p~plnty and
  a~plnnr = p~plnnr and a~plnkn = p~plnkn and a~zaehl = p~zaehl where p~ktsch = p_ktsch and p~plnty = p_plnty and p~loekz =
  space and a~loekz = space`. Spart die PLAS-FAE mit 21'567 Treibern und die 17'784 Aussenzeilen ohne aktive Folge. Das ist
  die belegte Alternative ohne DDIC-Aenderung, falls die PLPO/PLAS-Strecke in der Basismessung doch ins Gewicht faellt.
  Umbau der Schleife noetig, deshalb erst nach Messung.
* **P6, `READ_MULTIPLE_TEXTS` statt `READ_TEXT` je Vorgang (E5b).** Der Baustein existiert in T76 (TFDIR: `SAPLSTXD`,
  Include 36) und nimmt Namensbereiche (`NAME_RANGES`). Damit liessen sich die 2'846 Einzelzugriffe fuer 07201 in wenigen
  Paketen lesen. Verhalten bei Referenztexten und Formatzeilen muss gegen `READ_TEXT` verglichen werden. Nur, wenn die
  Basismessung `READ_TEXT` als groessten Posten zeigt.
* **P7, CA02 je Vorgang statt je Material (E1).** Siehe Abschnitt 5.
* **Nicht empfohlen:** STXH-Vorabpruefung. Bei 07201 haben 3'783 von 3'783 aktiven Vorgaengen einen Langtext, insgesamt
  789'425 Langtexte auf rund 1 Mio. Vorgaenge; der Gewinn waere nahe null.

### 4.4 Nebenbefunde (nicht Teil der Aenderung, nur zur Kenntnis)

* Feldkatalog enthaelt `VORG_TXT` und `KTSCH_TXT` je zweimal (T76-Zeilen 671 bis 708). Unveraendert.
* `read_vorgang_text` vergleicht `gt_ttab_v <> gt_ttab_o` zweimal hintereinander (T76-Zeilen 563 bis 569). Unveraendert.
* Die CA02-Sequenz sucht den Vorgang ueber `RC27H-VORNR` und nimmt den ersten Treffer (`RC27X-ENTRY_ACT 1`, T76-Zeilen
  1454 bis 1461). Eine Vorgangsnummer ist in einer Plangruppe nicht immer eindeutig: `50040931` hat Vorgang `0900` sechsmal
  (PLNKN 20, 40, 60, 74, 88, 102). *Vermutung:* parallele oder alternative Folgen; dann aendert der BDC nicht sicher den
  Vorgang der markierten Zeile. Bestehendes Verhalten, nicht Teil dieses Plans.

---

## 5. Optional P7: CA02 je Vorgang statt je Material (Entscheid E1)

Wenn mehrere markierte Zeilen auf dasselbe Ziel zeigen, aendert heute jeder CA02-Lauf denselben Vorgang mit denselben
Werten. Vorschlag: Schluessel **PLNTY/PLNNR/PLNAL/PLNKN/ZAEHL** (nicht VORNR, siehe 4.4); ein Ziel wird nur einmal
erfolgreich geaendert, weitere Zeilen desselben Ziels gelten als erledigt. Schlaegt der Lauf fehl, bleibt das Ziel offen
und die naechste Zeile desselben Ziels versucht es erneut (anderes Material, gleiche Aenderung). Status 99 wird wie bisher
vorher uebersprungen. Fuer `sichern_zeiten` und gleich fuer `sichern_texte`:

```abap
  types: begin of ty_done,
           plnty type plpo-plnty, plnnr type plpo-plnnr, plnal type mapl-plnal,
           plnkn type plpo-plnkn, zaehl type plpo-zaehl,
         end of ty_done.
  data: lt_done type hashed table of ty_done
                  with unique key plnty plnnr plnal plnkn zaehl,
        ls_done type ty_done,
        lv_err_vor type i,
        lv_dup     type i.
  ...
  loop at gt_rows assigning <r> where sel = 'X'.
    ...  " Status-99-Pruefung wie bisher
    move-corresponding <r> to ls_done.
    read table lt_done with table key plnty = ls_done-plnty plnnr = ls_done-plnnr
         plnal = ls_done-plnal plnkn = ls_done-plnkn zaehl = ls_done-zaehl
         transporting no fields.
    if sy-subrc = 0.
      lv_dup = lv_dup + 1.          " gleicher Vorgang, schon geaendert
      continue.
    endif.
    ...  " BDC wie bisher
    lv_err_vor = gv_err_cnt.
    perform bdc_transaction using 'CA02'.
    if gv_err_cnt = lv_err_vor.
      insert ls_done into table lt_done.
    endif.
  endloop.
```

Folgen, die Ingo kennen muss: Die Zaehler in der Schlussmeldung bedeuten dann „Vorgaenge geaendert“ statt „Materialzeilen
verarbeitet“; die Meldung bekommt einen dritten Wert „gleicher Vorgang uebersprungen: n“. Fuer 07201 aendert sich nichts
(2'846 Ziele fuer 2'846 Zeilen), fuer `23360` waeren es hoechstens 43 statt 7'762 CA02-Laeufe.

---

## 6. Ablauf Phase 2 (genau diese Schritte, bei Abweichung anhalten)

Voraussetzungen: Ingo hat E1 bis E8 entschieden; SAP Logon laeuft (`Get-Process saplogon`) und Ingo ist in T76/100
angemeldet; `docs/AGENT_COORDINATION.md` ist von der Hauptsitzung auf „schreibend“ gesetzt.

**Was die Entwurfsdatei genau enthaelt.** `ZM_ABGLEICH_KTSCH_entwurf_2026-11-12.abap` setzt diese Entscheide um und wird in
Schritt 5 **unveraendert** eingespielt: E1 = nein (kein CA02-Dedup), E2 = Variante A (leer gilt als 0), E3 = 3c (Ausrichtung
bleibt), E4 = nein (`fmt_quan` unveraendert), E5 = spaeter (kein P5/P6). Entscheidet Ingo anders, wird **vor** Phase 2 eine
neue, fertige Einspieldatei samt Nachtrag zu diesem Plan erstellt; waehrend Phase 2 wird kein Code geschrieben.
**Der Entwurf ist nicht syntaxgeprueft** (in Phase 1 nicht moeglich, `abap-check --source-file` ist unbrauchbar, Arbeitsweise
Abschnitt 3); die erste verbindliche Pruefung ist das Aktivieren in Schritt 6.

| Schritt | Was | Werkzeug | Erwartet / Rueckleseprobe |
| --- | --- | --- | --- |
| 0 | Quelltext T76 erneut lesen und gegen `ZM_ABGLEICH_KTSCH_T76_2026-11-12.abap` vergleichen; `E070` fuer `T76K911958/959` und `TLOCK` erneut lesen | `RunSapProbe.ps1 abap-read ... > datei`, `diff` mit eigenem Exitcode | `diff` Exitcode 0; Sperre nur `DYNP0900`. Sonst anhalten |
| 1 | Eigene T76-Sitzung oeffnen (vorher/nachher `info`, nur weiter, wenn genau eine neue Sitzung dazukam); ZC12 mit `P_KTSCH 13457`, `P_WERKS 1100`, `P_PLNTY N`; eine Zeile markieren, „Zeiten abgleichen“, Popup-Feldinhalt `VGW01` samt Leerzeichen auslesen, **Abbrechen** (nichts sichern). Danach im ALV-Grid die Export-Funktion bestimmen (Kontextmenue/Toolbar-Ids per `dump`, Tooltips lesen) und festhalten, welche Funktion eine lokale Datei schreibt | nur `SapGuiT76Hilfe.vbs` (`neu`, `info`, `dump`, `press`, `grid`) | Beleg fuer 3.1 (Lage der `4` im Feld); Export-Weg fuer Schritt 2 bekannt. Nichts geaendert. Ohne Export-Weg: Spalten ueber `SapGuiT76Hilfe.vbs grid` zeilenweise lesen |
| 2 | Basismessung **alter** Stand, je zweimal: ZC12 mit `07201`/1100/N und mit `23360`/1100/N bis zur fertigen ALV-Liste; Zeit im Skript stoppen; Zeilenzahl und Spalten `MATNR, PLNNR, PLNAL, VORNR, VGW01..04, TEXT_DIFF, VORG_TXT` ueber den Weg aus Schritt 1 sichern. `STOP001` **nicht** verwenden (144'547 Zeilen, Gefahr des Dialogabbruchs nach 600 s, LEARNINGS 2026-10-12) | `SapGuiT76Hilfe.vbs` | Zwei Laeufe alt gegen alt als **sortierte Liste** gleich (LEARNINGS 2026-10-12); sonst erst klaeren |
| 3 | Neuen Workbench-Auftrag anlegen, Text: `ZC12: Wert 0 bei Vorgabewerten, Laufzeit (PM-02)` (49 Zeichen); nicht `T76K911958` nehmen | `SapGuiAuftragNeu.vbs` | neue Nummer `T76K9xxxxx`, `E070` Status D, Besitzer KOI |
| 4 | Nur bei E3 = 3a: `ZSKTSCH_TIME_PO` um `VAL01..VAL04` erweitern (Datenelement vorher in DD04L festlegen), in den neuen Auftrag, aktivieren | SE11 per Skript | DD03L zeigt 8 Felder; Auftrag aus Schritt 3 |
| 5 | Quelltext setzen: Entwurfsdatei unveraendert (bzw. die vor Phase 2 freigegebene Einspieldatei) in `ZM_ABGLEICH_KTSCH`, Transportabfrage mit dem Auftrag aus Schritt 3 | `SapGuiSetReportSourceAuftrag.vbs <Sitzung> ZM_ABGLEICH_KTSCH <Datei> <Auftrag>` | Dialog „Inaktive Objekte“ zeigt nur `ZM_ABGLEICH_KTSCH` als eigenes Objekt |
| 6 | Aktivieren; erscheint „Fehler beim Aktivieren“: **Abbrechen**, Syntaxpruefung lesen, nicht blind aktivieren (LEARNINGS 2026-11-02) | SE38 `tbar[1]/btn[27]` | Status aktiv ohne Dialog |
| 7 | Zuruecklesen und vergleichen: aktiver Quelltext gegen die eingespielte Datei, und gegen T76-Stand: nur die Bloecke aus Abschnitt 2.3 und 4.2 (und ggf. 3.2, 5) duerfen abweichen; `E071` des neuen Auftrags | `abap-read`, `diff` | `diff` gegen Einspieldatei Exitcode 0; `E071` enthaelt nur `REPS ZM_ABGLEICH_KTSCH` (und bei 3a `TABL ZSKTSCH_TIME_PO`) |
| 8 | Tests T1 bis T6 (Abschnitt 7) | GUI-Skript, `table-read PLPO` | wie in Abschnitt 7 |
| 9 | ST22 fuer KOI heute lesen | GUI | kein Dump |
| 10 | Eigene Sitzung mit `/i` schliessen; Auftrag **nicht** freigeben | `SapGuiT76Hilfe.vbs zu` | — |

---

## 7. Testplan T76

| Test | Eingabe | Erwartet | Rueckleseprobe |
| --- | --- | --- | --- |
| **T1** alter Wert 4, neu 0 | ZC12 `13457`/1100/N; nur Zeile Material `30346` markieren (Plangruppe `50000611/01`, Vorgang 0075, PLNKN 24); Popup: Haken 1, Wert `0`, Einheit `MIN`; Haken 2 bis 4 entfernen; Sichern | Zaehler in der Statusmeldung OK 1, Fehler/Skip 0, geaendert `VGW01` (der Wortlaut hat Leerzeichen aus `concatenate ... separated by space` und wird nicht buchstabengetreu verglichen); kein Fehlerprotokoll | **verbindlich:** `table-read PLPO --where "PLNTY = 'N' AND PLNNR = '50000611' AND PLNKN = '00000024'"`: `VGW01 0.000 MIN`, `VGW02 0.200 MIN`, `VGW03 0.300 MIN`, `VGW04 4.000 MIN`, `ZAEHL 00000024` unveraendert, keine neue PLPO-Zeile; Gegenzeile `50011535/00000020` unveraendert `4.000/0.200/0.300/4.000` |
| **T2** Haken ohne Einheit | dieselbe Zeile; Haken 1, Wert `0`, Einheit geleert; Haken 2 bis 4 entfernt | Meldung „Angehakte Zeit: Einheit erforderlich“, kein CA02 | PLPO unveraendert gegen den Stand nach T1 (`VGW01 0.000`) |
| **T3a** Ruecksetzen vor T3 | dieselbe Zeile; Haken 1, Wert `4`, Einheit `MIN`; Haken 2 bis 4 entfernt | OK 1, Fehler 0 | `VGW01 4.000 MIN` |
| **T3** leeres Wertfeld mit Haken | dieselbe Zeile; Haken 1, Wertfeld geleert, Einheit `MIN`; Haken 2 bis 4 entfernt | E2 = A: OK 1, PLPO `VGW01 0.000`; E2 = B: Meldung „Angehakte Zeit ohne Wert ...“, kein CA02 | PLPO lesen (A: 0.000, B: 4.000) |
| **T4** Rueckbau der Testdaten | dieselbe Zeile; Haken 1, Wert `4`, Einheit `MIN`; Haken 2 bis 4 entfernt (bei E2 = B nur, wenn `VGW01` nicht schon 4.000 ist) | OK 1 | `VGW01 4.000 MIN`, uebrige Werte wie vor T1 |
| **T5** Regression Lesepfad | ZC12 `07201` und `23360` neu, je zweimal, gleicher Export wie Schritt 2 | Zeilenzahl gleich; die exportierten Zeilen **sortiert als Liste** gleich (Reihenfolge darf abweichen, 4.2). Nicht als Menge vergleichen: bei `23360` gibt es in `50038715` den Vorgang 0370 zweimal (PLNKN 7 und 30) mit gleichen Exportspalten, eine Menge wuerde sie zusammenlegen | sortierter Vergleich alt gegen neu |
| **T6** Laufzeit | Zeiten aus T5 gegen Schritt 2 | neue Zeit kleiner; Erwartung je nach Anteil von H1 bis H4 (Schaetzung 4.1) | Zeiten im Bericht; optional SAT durch Ingo fuer die Aufteilung DB/ABAP |

Testdaten-Beleg (heute gelesen): KTSCH `13457` hat genau 2 Vorgaenge (`50000611/24` und `50011535/20`, beide Vorgang 0075,
Arbeitsplatz-ID `10000695` = `ETS09`, `VGWTS ZAP1`, Pflegeregeln `VGM01..04` leer, `AENNR` leer), je ein Material in Werk 1100 (`30346` bzw. `30337`, MAPL aktiv), `MSTAE`/`MSTAV`
leer, `MARC-MMSTA` leer, Plankopf `STATU 4`, `VERWE 1`, ohne Aenderungsnummer; Vorlagentext `WORKST/SUBM 13457` vorhanden.
Erstes Kandidatenpaar `00435` verworfen: beide Materialien haben `MSTAE/MSTAV 99` und wuerden vom Report ausgefiltert.

---

## 8. Risiken

* **CA02 nimmt 0 doch nicht an** (1.7 ist ein Hinweis, kein Beweis): T1 zeigt es. Dann Fehlertext aus dem Protokoll lesen
  und anhalten; keine Variante ausprobieren.
* **Komma-Benutzer** (3.4): betrifft gebrochene Werte schon heute; durch Aenderung 1 nicht schlechter, aber auch nicht
  besser. Entscheid E4.
* **Reihenfolge der Zeilen** (4.2): Vorbelegung des Popups kann sich bei Mehrfachmarkierung aendern.
* **Sortierte Tabellen mit nicht eindeutigem Schluessel**: bewusst gewaehlt, damit doppelte Saetze aus FOR ALL ENTRIES
  keinen Laufzeitfehler ausloesen (bei einem eindeutigen Schluessel waere das ein Dump).
* **Offener Altauftrag `T76K911958`** mit Dynpro 0900: wird bei Freigabe durch Ingo mit transportiert, wenn er ihn mit
  freigibt; Phase 2 fasst ihn nicht an.
* **Notfalloption Index (nur falls die Basismessung die PLPO-Abfrage eindeutig als Haupttreiber zeigt und P5 nicht reicht):**
  Der DDIC-Index `PLPO~Z01` auf `KTSCH` existiert bereits mit „Nicht auf der Datenbank anlegen“. Ihn auf der Datenbank
  anzulegen waere eine Aenderung an einer SAP-Standardtabelle (Basis, alle Systeme, Platz und Schreiblast auf rund 1 Mio.
  Zeilen) und ist nach Ingos Entscheid **keine** Massnahme dieses Plans.

## 9. Rueckbau

* **Daten:** T4 stellt den Testvorgang wieder auf 4.000 MIN.
* **Quelltext:** Inhalt von `ZM_ABGLEICH_KTSCH_T76_2026-11-12.abap` mit `SapGuiSetReportSourceAuftrag.vbs` in denselben
  Auftrag zurueckspielen und aktivieren, danach `abap-read` und `diff` gegen diese Datei (Exitcode 0). Alternativ in SE38
  die Version vom 27.05.2026 (`T76K912063`) aus der Versionsverwaltung zurueckholen.
* **Struktur (nur bei 3a):** `VAL01..04` wieder entfernen und aktivieren.
* **Auftrag:** bleibt offen; Objekte koennen von Ingo in SE09 entfernt oder der Auftrag geloescht werden. P76 ist nie
  betroffen, solange nichts freigegeben wird.

## 10. Offene Entscheide fuer Ingo

| Nr. | Frage | Vorschlag |
| --- | --- | --- |
| E1 | CA02 je Vorgang statt je Material (P7, Abschnitt 5)? | ja, weil Sammelplaene bis 7'719-fach dieselbe Aenderung schreiben; Zaehler aendern ihre Bedeutung |
| E2 | Leeres Wertfeld bei gesetztem Haken: als 0 schreiben (A) oder mit Meldung abbrechen (B)? | A |
| E3 | Popup-Ausrichtung: 3a (Z-Struktur um Textfelder erweitern), 3b (Standard-Textfelder), 3c (lassen)? | 3a, wenn es Ingo wichtig ist; sonst 3c |
| E4 | Dezimaltrenner im BDC an das Benutzerformat anpassen (3.4)? Wer nutzt ZC12 in P76, mit welchem Format? | ja, wenn Komma-Benutzer ZC12 nutzen; Test braucht einen Komma-Benutzer in T76 |
| E5 | P5 (PLPO+PLAS-Join) und P6 (`READ_MULTIPLE_TEXTS`) gleich mitnehmen oder erst nach der Messung? | erst nach der Messung |
| E6 | Sekundaerindex auf `PLPO-KTSCH` | **entschieden 12.11.: nein**, nur Notfall (Abschnitt 8) |
| E7 | Was geschieht mit dem offenen Altauftrag `T76K911958` (KOI, 11.05.2026, „zv“, nur Dynpro 0900)? | Ingo sieht ihn sich an; Phase 2 fasst ihn nicht an |
| E8 | Transport nach P76: wann und durch wen? | Freigabe und Import macht Ingo nach dem Test in T76 |
