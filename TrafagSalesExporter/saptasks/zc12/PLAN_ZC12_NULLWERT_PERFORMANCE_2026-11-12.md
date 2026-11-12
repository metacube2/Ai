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

*Nachtrag 2026-11-12 (nach Ingos Entscheiden, Abschnitt 11):* Eingespielt wird in Schritt 5 **unveraendert** die Datei
`ZM_ABGLEICH_KTSCH_einspiel_2026-11-12.abap`. Sie setzt um: E1 = ja (P7 in `sichern_zeiten` und `sichern_texte`), E2 = A,
E3 = 3a (`ZSKTSCH_TIME_PO-VAL01..04`, Rueckwandlung mit `RS_CONV_EX_2_IN`), E4 = nein, E5 = nicht eingebaut. Inhalt und
Selbstpruefung: Abschnitt 12. Die Entwurfsdatei ist damit nur noch Zwischenstand. Der folgende Absatz beschreibt den Stand vor
den Entscheiden:

~~**Was die Entwurfsdatei genau enthaelt.** `ZM_ABGLEICH_KTSCH_entwurf_2026-11-12.abap` setzt diese Entscheide um und wird in
Schritt 5 unveraendert eingespielt: E1 = nein, E2 = A, E3 = 3c, E4 = nein, E5 = spaeter.~~ (ueberholt)

Waehrend Phase 2 wird kein Code geschrieben. **Die Einspieldatei ist nicht syntaxgeprueft** (in Phase 1 nicht moeglich,
`abap-check --source-file` ist unbrauchbar, Arbeitsweise Abschnitt 3); die erste verbindliche Pruefung ist das Aktivieren in
Schritt 6.

| Schritt | Was | Werkzeug | Erwartet / Rueckleseprobe |
| --- | --- | --- | --- |
| 0 | Quelltext T76 erneut lesen und gegen `ZM_ABGLEICH_KTSCH_T76_2026-11-12.abap` vergleichen; `E070` fuer `T76K911958/959` und `TLOCK` erneut lesen | `RunSapProbe.ps1 abap-read ... > datei`, `diff` mit eigenem Exitcode | `diff` Exitcode 0; Sperre nur `DYNP0900`. Sonst anhalten |
| 1 | Eigene T76-Sitzung oeffnen (vorher/nachher `info`, nur weiter, wenn genau eine neue Sitzung dazukam); ZC12 mit `P_KTSCH 13457`, `P_WERKS 1100`, `P_PLNTY N`; eine Zeile markieren, „Zeiten abgleichen“, Popup-Feldinhalt `VGW01` samt Leerzeichen auslesen, **Abbrechen** (nichts sichern). Danach im ALV-Grid die Export-Funktion bestimmen (Kontextmenue/Toolbar-Ids per `dump`, Tooltips lesen) und festhalten, welche Funktion eine lokale Datei schreibt | nur `SapGuiT76Hilfe.vbs` (`neu`, `info`, `dump`, `press`, `grid`) | Beleg fuer 3.1 (Lage der `4` im Feld); Export-Weg fuer Schritt 2 bekannt. Nichts geaendert. Ohne Export-Weg: Spalten ueber `SapGuiT76Hilfe.vbs grid` zeilenweise lesen |
| 2 | Basismessung **alter** Stand, je zweimal: ZC12 mit `07201`/1100/N und mit `23360`/1100/N bis zur fertigen ALV-Liste; Zeit im Skript stoppen; Zeilenzahl und Spalten `MATNR, PLNNR, PLNAL, VORNR, VGW01..04, TEXT_DIFF, VORG_TXT` ueber den Weg aus Schritt 1 sichern. `STOP001` **nicht** verwenden (144'547 Zeilen, Gefahr des Dialogabbruchs nach 600 s, LEARNINGS 2026-10-12) | `SapGuiT76Hilfe.vbs` | Zwei Laeufe alt gegen alt als **sortierte Liste** gleich (LEARNINGS 2026-10-12); sonst erst klaeren |
| 3 | Neuen Workbench-Auftrag anlegen, Text: `ZC12: Wert 0 bei Vorgabewerten, Laufzeit (PM-02)` (49 Zeichen); nicht `T76K911958` nehmen | `SapGuiAuftragNeu.vbs` | neue Nummer `T76K9xxxxx`, `E070` Status D, Besitzer KOI |
| 4 | `ZSKTSCH_TIME_PO` in SE11 aendern: hinter `CHG04` vier Felder anhaengen, **Position 5 `VAL01`, 6 `VAL02`, 7 `VAL03`, 8 `VAL04`, je Datenelement `CHAR15`** (SAP-Standard, Paket `SZS`; CHAR 15, Ausgabelaenge 15, keine Konvertierungsroutine, keine Kleinbuchstaben, keine Festwerte; DD04L/DD01L/TADIR gelesen 12.11.). Bestehende Felder `CHG01..04` (`XFELD`) nicht anfassen. Sichern in den Auftrag aus Schritt 3 (Vorschlag im Dialog pruefen, LEARNINGS 2026-09-10), aktivieren. **Vor Schritt 5**, sonst bricht das Popup mit `ERROR_IN_FIELDS` ab | SE11 per Skript (Vorlage `SapGuiSe11Aendern.vbs`, `SapGuiStrukturFelder.vbs`; Seitengrenze beachten, LEARNINGS 2026-09-30) | `table-read DD03L --where "TABNAME = 'ZSKTSCH_TIME_PO' AND AS4LOCAL = 'A'"`: genau 8 Felder, Positionen 0001 bis 0008, `CHG01..04 XFELD`, `VAL01..04 CHAR15 CHAR 000015`; keine Zeile mit `AS4LOCAL N`; `E071` des Auftrags enthaelt `R3TR TABL ZSKTSCH_TIME_PO` |
| 5 | Quelltext setzen: `ZM_ABGLEICH_KTSCH_einspiel_2026-11-12.abap` unveraendert in `ZM_ABGLEICH_KTSCH`, Transportabfrage mit dem Auftrag aus Schritt 3 | `SapGuiSetReportSourceAuftrag.vbs <Sitzung> ZM_ABGLEICH_KTSCH <Datei> <Auftrag>` | Dialog „Inaktive Objekte“ zeigt nur `ZM_ABGLEICH_KTSCH` als eigenes Objekt |
| 6 | Aktivieren; erscheint „Fehler beim Aktivieren“: **Abbrechen**, Syntaxpruefung lesen, nicht blind aktivieren (LEARNINGS 2026-11-02) | SE38 `tbar[1]/btn[27]` | Status aktiv ohne Dialog |
| 7 | Zuruecklesen und vergleichen: aktiver Quelltext gegen die Einspieldatei, und gegen T76-Stand: nur die Bloecke aus Abschnitt 12.2 duerfen abweichen; `E071` des neuen Auftrags | `abap-read`, `diff` | `diff` gegen Einspieldatei Exitcode 0; `E071` enthaelt nur `LIMU REPS ZM_ABGLEICH_KTSCH` und `R3TR TABL ZSKTSCH_TIME_PO` |
| 8 | Tests T1 bis T9 (Abschnitt 7) in dieser Reihenfolge: T7, T8, T1, T2, T3a, T3, T4, T9, T9r, T5, T6 | GUI-Skript, `table-read PLPO` | wie in Abschnitt 7 |
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
| **T7** Popup linksbuendig (3a) | ZC12 `13457`/1100/N; Zeile Material `30346` markieren; „Zeiten abgleichen“; Inhalt der vier Wertfelder samt Leerzeichen auslesen; **Abbrechen** | Felder heissen „Wert“, Inhalt beginnt in Spalte 1 ohne fuehrende Leerzeichen: `VAL01` `4`, `VAL04` `4`; `VAL02`/`VAL03` `0.2`/`0.3` oder `0.200`/`0.300` (ob `WRITE ... UNIT MIN` die Nullen hinter 2 bzw. 3 kappt, ist nicht belegt; beides ist richtig) | nichts geaendert, PLPO wie vorher |
| **T8** ungueltige Eingabe | dieselbe Zeile; Haken 1, Wert `abc`, Einheit `MIN`; Haken 2 bis 4 entfernt; OK | Statusmeldung „Ungültiger Wert bei Rüsten Masch : ABC“ (Grossbuchstaben, weil `CHAR15` keine Kleinbuchstaben hat); kein CA02, kein Fehlerprotokoll | PLPO `50000611/24` unveraendert (`VGW01 4.000`) |
| **T9** Sammelplan, ein CA02 | ZC12 `50503`/1100/N; nur die **zwei** Zeilen der Materialien `D50538` und `D50540` markieren (beide Plangruppe `50044282/01`, Vorgang 5050, PLNKN 2); Popup: Haken 1, Wert `2`, Einheit `MIN`; Haken 2 bis 4 entfernt; Sichern | Zaehler OK 1, Fehler/Skip 0, „gleicher Vorgang übersprungen: 1“ | `table-read PLPO --where "PLNTY = 'N' AND PLNNR = '50044282' AND PLNKN = '00000002'"`: `VGW01 2.000 MIN`, `VGW02 1.800`, `VGW03 1.800`, `VGW04 1.000`; die drei anderen Vorgaenge mit KTSCH `50503` (`50044013/3`, `50044015/3`, `50047957/2`) unveraendert |
| **T9r** Rueckbau T9 | nur Zeile `D50538` markieren; Haken 1, Wert `1`, Einheit `MIN`; Haken 2 bis 4 entfernt | OK 1, uebersprungen 0 | `50044282/2` wieder `VGW01 1.000 MIN` |

Testdaten-Beleg T9 (heute gelesen): KTSCH `50503` hat 4 Vorgaenge (PLPO), alle Vorgang 5050, Arbeitsplatz-ID `10001024` =
`CZPCE10`, Werk 1100, `VGWTS ZAP1`, `VGM01..04` leer, `AENNR` leer. `50044282/2`: `1.000/1.800/1.800/1.000 MIN`, PLAS
`50044282/01/2` aktiv; MAPL Werk 1100 aktiv: `D50538` und `D50540` an `50044282/01`, die uebrigen Vorgaenge je ein Material
(`D50508`, `D50568`, `D50539`). `D50538`/`D50540`: `MSTAE`/`MSTAV` leer, `MARC-MMSTA` leer. Plankopf `50044282/01`
`STATU 4`, `VERWE 1`, ohne Aenderungsnummer. Vorlagentext `WORKST/SUBM 50503` vorhanden. Gerechnet: 5 ALV-Zeilen fuer 4 Ziele.

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

* **Daten:** T4 stellt `50000611/24` wieder auf `VGW01 4.000 MIN`, T9r stellt `50044282/2` wieder auf `VGW01 1.000 MIN`.
  Rueckleseprobe je mit `table-read PLPO` wie in Abschnitt 7.
* **Quelltext:** Inhalt von `ZM_ABGLEICH_KTSCH_T76_2026-11-12.abap` mit `SapGuiSetReportSourceAuftrag.vbs` in denselben
  Auftrag zurueckspielen und aktivieren, danach `abap-read` und `diff` gegen diese Datei (Exitcode 0). Alternativ in SE38
  die Version vom 27.05.2026 (`T76K912063`) aus der Versionsverwaltung zurueckholen.
* **Struktur (E3 = 3a):** **erst** den Quelltext zurueckspielen und aktivieren (der neue Report braucht `VAL01..04`, sonst
  `ERROR_IN_FIELDS` im Popup), **dann** in SE11 die Felder `VAL01..04` (Positionen 5 bis 8) loeschen, sichern im selben
  Auftrag, aktivieren. Rueckleseprobe: `DD03L` zeigt wieder genau `CHG01..04` (Positionen 1 bis 4, `XFELD`), kein Eintrag mit
  `AS4LOCAL N`. Die Struktur wird nur von `ZM_ABGLEICH_KTSCH` benutzt (Abschnitt 12.1), andere Objekte sind nicht betroffen.
* **Auftrag:** bleibt offen; Objekte koennen von Ingo in SE09 entfernt oder der Auftrag geloescht werden. P76 ist nie
  betroffen, solange nichts freigegeben wird.

## 10. Offene Entscheide fuer Ingo

*Nachtrag 2026-11-12: alle entschieden, siehe Abschnitt 11. Die Tabelle zeigt den Stand der Fragen vor dem Entscheid.*

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

## 11. Entscheide Ingo (2026-11-12, im Chat)

| Nr. | Entscheid |
| --- | --- |
| E1 | **ja**: CA02 je Vorgang einmal (P7, Abschnitt 5), Zaehler in der Schlussmeldung zaehlen Vorgaenge, dazu „gleicher Vorgang uebersprungen: n“ |
| E2 | **A**: leeres Wertfeld mit Haken gilt als 0 |
| E3 | **3a**: vier neue Textfelder `VAL01..04` in der eigenen Struktur `ZSKTSCH_TIME_PO`, Vorbelegung linksbuendig, Rueckwandlung mit `RS_CONV_EX_2_IN` |
| E4 | **nein**: `fmt_quan` bleibt mit Punkt (nur ganze Zahlen im Gebrauch) |
| E5 | offen, Vorschlag gilt: P5/P6 erst nach der Messung |
| E6 | nein (kein Index) |
| E7 | Altauftrag `T76K911958` wird nicht angefasst |
| E8 | Freigabe und Import nach P76 macht Ingo nach dem Test in T76 |

*(Ueberholt 2026-11-12 09:55: die Einspieldatei `ZM_ABGLEICH_KTSCH_einspiel_2026-11-12.abap` liegt vor, Abschnitt 12.)* Folge: Der Entwurf vom 12.11. setzt E1 und E3 noch nicht um. Vor Phase 2 entsteht eine neue Einspieldatei (Abschnitt 6, „Was die
Entwurfsdatei genau enthaelt“) samt Nachtrag; Phase 2 startet erst nach ausdruecklicher Freigabe Ingos fuer die Schreibschritte.

*Nachtrag 2026-11-12: Die Einspieldatei ist erstellt (Abschnitt 12); der Satz oben „Vor Phase 2 entsteht eine neue
Einspieldatei“ ist damit erledigt. Phase 2 wartet weiter auf Ingos Freigabe der Schreibschritte.*

---

## 12. Einspieldatei `ZM_ABGLEICH_KTSCH_einspiel_2026-11-12.abap` (nach den Entscheiden, 2026-11-12)

Weiterhin nur lesend erarbeitet, nichts in SAP geschrieben. Die Datei hat 2'171 Zeilen (CRLF, UTF-8) und ist **nicht
syntaxgeprueft**; die erste verbindliche Pruefung ist Schritt 6.

### 12.1 Belege fuer 3a (Struktur und Datenelement)

| Pruefung | Ergebnis | Beleg |
| --- | --- | --- |
| Felder heute | `CHG01` 0001, `CHG02` 0002, `CHG03` 0003, `CHG04` 0004, alle `XFELD`, `CHAR 1`; nur aktive Version (keine `AS4LOCAL N`-Zeilen) | `DD03L TABNAME = 'ZSKTSCH_TIME_PO'` ohne Versionsfilter |
| Struktur | `DD02L TABCLASS INTTAB`, `EXCLASS 1`, zuletzt KOI 26.05.2026; Kurztext (DD02T) „we“ | DD02L, DD02T |
| Objektkatalog | `R3TR TABL ZSKTSCH_TIME_PO`, Paket `ZPP`, Autor KOI, 26.05.2026 | TADIR |
| Auftraege | nur `T76K912063` und Aufgabe `T76K912064`, beide freigegeben (`E070 R`, Abschnitt 1.2); kein offener Auftrag | E071 `OBJ_NAME LIKE 'ZSKTSCH_TIME_PO%'` |
| Verwendung | keine Treffer in `WBCROSSGT`, `CROSS`, `D010TAB`; kein DDIC-Objekt verweist darauf (`DD03L ROLLNAME`/`PRECFIELD`: keine Treffer). Der Report benutzt den Namen nur als Text im Aufruf von `POPUP_GET_VALUES`; deshalb findet ihn kein Verwendungsnachweis. Eine Volltextsuche ueber alle Quelltexte (z. B. `RS_ABAP_SOURCE_SCAN`) ist **nicht** gelaufen | WBCROSSGT, CROSS, D010TAB, DD03L |
| Datenelement fuer `VAL01..04` | **`CHAR15`**: Domaene `CHAR15`, `CHAR` 15, Ausgabelaenge 15, keine Konvertierungsroutine, `LOWERCASE` leer, keine Festwerte, keine Werttabelle; Text „Char 15“; SAP-Standard, Paket `SZS` | DD04L, DD01L, DD04T, TADIR |
| Laenge reicht | `VGWRT` hat Ausgabelaenge 11; groesster Wert `999999.999` sind 10 Zeichen ohne Tausendertrenner. `RS_CONV_EX_2_IN` weist laengere Eingaben ab (`l_olength > l_convert-olength`, Fehler `INPUT_TOO_LONG`, LSSELU20) | DD04L, LSSELU20 |
| Alternativen gelesen | `CHAR16`, `CHAR17`, `CHAR18`, `CHAR20` (je ohne Kleinbuchstaben), `TEXT15`/`TEXT20` (mit Kleinbuchstaben) | DD04L |

**Schnittstelle `RS_CONV_EX_2_IN`** (FUPARAREF und Quelltext `LSSELU20`, Funktionsgruppe `SSEL`): `INPUT_EXTERNAL` per Wert
`TYPE C`, `TABLE_FIELD LIKE TABFIELD` (Felder `TABNAME`, `FIELDNAME`, `LFIELDNAME`), optional `CURRENCY`, `NO_TCURX`;
Export `OUTPUT_INTERNAL` ohne Typ (per Wert). Der Baustein prueft nur, dass der Typ des Zielfelds zum DDIC-Feld passt
(`l_type ne l_convert-type`, Laengenpruefung auskommentiert): `vgwNN_neu` ist `P`, `PLPO-VGWnn` ist `P`. Alle Fehler gehen
ueber `MESSAGE E... RAISING` (Form `CONVERT_ERROR`, LSSELFXX ab Zeile 2106), sind also mit `EXCEPTIONS others = 1`
abfangbar und erzeugen keinen Abbruch.

### 12.2 Selbstpruefung: jede Abweichung vom T76-Abzug ist einer Massnahme zugeordnet

`diff --strip-trailing-cr ZM_ABGLEICH_KTSCH_T76_2026-11-12.abap ZM_ABGLEICH_KTSCH_einspiel_2026-11-12.abap`: Exitcode 1,
52 entfernte und 282 neue Zeilen in 52 Abschnitten. Zuordnung (T76-Zeilen → Einspiel-Zeilen):

| T76 | Einspiel | Inhalt | Massnahme |
| --- | --- | --- | --- |
| nach 15 | 16 bis 20 | Kopfhistorie 12.11. | Doku |
| nach 114 | 120 bis 129 | Typ `ty_done` | P7 (E1) |
| nach 121 | 137 bis 143 | `gv_leer01..04` | 2.3 (E2) |
| 137 bis 141 | 159 bis 166 | `gt_plas/gt_mapl/gt_mara/gt_makt/gt_crhd` sortiert | P1 |
| 347 | 372 bis 389 | lokale Daten `select_data`, Treiber-Tabellen | P2, P3, P4 |
| nach 374, 378 bis 380, nach 400 | 417 bis 423, 427 bis 430, 451 bis 455 | MAPL-Treiber `lt_plkey` mit `plnty = p_plnty`, MARA-Treiber `lt_matnr` aufbauen | P4 |
| 405/406, nach 424, 428, 430 | 460/461, 480 bis 484, 488, 490 bis 501 | MARA ueber `lt_matnr`, CRHD ueber `lt_arbid`, Vorlagentext einmal | P4, P3 |
| 436, 450, 457 | 507 bis 511, 525, 532 | `with table key`, Text-Merker zuruecksetzen | P1, P2 |
| 481 bis 491 | 556 | Vorlagentext aus der Variablen | P3 |
| 498 bis 500 | 563 bis 570 | `read_vorgang_text` einmal je PLPO-Satz | P2 |
| 878, nach 880, nach 882 | 948, 951, 954 bis 960 | `sichern_texte`: `lv_msg` 200, `lv_cdup`, `lt_done` | P7 (E1) |
| nach 962 | 1041 bis 1058 | `sichern_texte`: Pruefung „gleicher Vorgang“ | P7 |
| nach 1027, nach 1029 | 1124, 1127 bis 1131 | `sichern_texte`: Ziel nach Erfolg merken | P7 |
| 1037, nach 1039, 1043 | 1139/1140, 1143, 1147 | `sichern_texte`: Schlussmeldung und Trace mit „gleicher Vorgang übersprungen“ | P7 |
| nach 1094 | 1199 bis 1207 | `popup_get_zeiten`: `lv_v1..4` (C 15), `lv_fehler` | 3a (E3) |
| 1145 | 1258/1259 | `gv_leer01..04` mit zuruecksetzen | 2.3 (E2) |
| nach 1199, 1202, 1207, 1212, 1217 | 1314 bis 1319, 1322, 1327, 1332, 1337 | Vorbelegung `WRITE ... UNIT ... LEFT-JUSTIFIED NO-GROUPING`, Popup-Felder `ZSKTSCH_TIME_PO-VAL01..04` statt `PLPO-VGW01..04` | 3a |
| nach 1244, 1249, 1256, 1263, 1270, nach 1273 | 1365 bis 1368, 1373/1374, 1381/1382, 1389/1390, 1397/1398, 1402 bis 1405 | Rueckwandlung ueber `wert_aus_popup`, Abbruch bei `lv_fehler` | 3a, 2.3 |
| nach 1298 | 1431 bis 1478 | neue Form `wert_aus_popup` (`RS_CONV_EX_2_IN`, leer → 0 mit Kennzeichen, Fehler → Meldung) | 3a, E2 |
| 1317, nach 1319, nach 1327 | 1497, 1500, 1509 bis 1515 | `sichern_zeiten`: `lv_msg` 200, `lv_cdup`, `lt_done` | P7 |
| 1353 bis 1359, nach 1362 | 1541 bis 1550, 1554 bis 1566 | Pruefung nur noch Einheit; Variante B als Kommentar | 2.3 (E2 = A) |
| nach 1421 | 1626 bis 1643 | `sichern_zeiten`: Pruefung „gleicher Vorgang“ | P7 |
| nach 1497, nach 1499 | 1720, 1723 bis 1727 | `sichern_zeiten`: Ziel nach Erfolg merken | P7 |
| 1507, nach 1510, 1514 | 1735/1736, 1740, 1744 | `sichern_zeiten`: Schlussmeldung und Trace | P7 |

Unveraendert (gegengeprueft im diff): Infozeile „IST häufig“ (`lv_w1..4`, T76-Zeilen 1102 bis 1128), `fmt_quan` (E4 = nein),
`read_vorgang_text`, `count_freq`, `build_fcat`, `create_alv`, alle BDC-Sequenzen, `bdc_transaction`, Dynpro-0900-Module,
`run_debug`, Trace-Formen.

### 12.3 Typpruefung von Hand (ersetzt keine Syntaxpruefung)

* `move-corresponding <r> to ls_done`: `ty_row` hat `plnty` (`plpo-plnty`), `plnnr` (`plpo-plnnr`), `plnal` (`mapl-plnal`),
  `plnkn` (`plpo-plnkn`), `zaehl` (`plpo-zaehl`) mit denselben Typen wie `ty_done` (T76-Zeilen 30 bis 34).
* `lt_done` ist `hashed ... unique key`; `insert` eines vorhandenen Schluessels setzt nur `sy-subrc 4`, kein Abbruch. Die
  Pruefung „gleicher Vorgang“ steht vor dem Aufbau der BDC-Daten, es bleiben also keine halben BDC-Saetze in `bdcdata` liegen.
* `perform wert_aus_popup ... changing vgw02_neu`: Formalparameter `type plpo-vgw01`, Aktualparameter `plpo-vgw02`; beide
  Datenelement `VGWRT` (P 5, 3 Dezimalen), also kompatibel.
* `write <p> to <c15> unit <meins> left-justified no-grouping`: Quelle `P`, Ziel `C 15`. *Annahme:* Bei leerer oder
  unbekannter Einheit (z. B. `VGE04` leer bei 1'876 Vorgaengen von `07201`) ignoriert `WRITE` den Zusatz `UNIT`; durch T7
  nicht abgedeckt (dort sind alle Einheiten `MIN`), beim ersten Popup mit leerer `VGE04` beobachten.
* `message lv_msg type 'S'` mit `C 200`: laengste Meldung rund 110 Zeichen.
* `RS_CONV_EX_2_IN` mit `input_external = iv_value` (generisch `C`, aus `SVAL-VALUE` C 132): fuehrende Leerzeichen werden fuer
  Packed-Felder uebersprungen (LSSELU20 `strlen( input_external+sy-fdpos )`).

### 12.4 Neue Risiken durch E1 und E3

* **Eingabepruefung wandert aus dem Popup in den Report.** Bei `PLPO-VGWnn` hat das Popup eine falsche Zahl sofort im Popup
  gemeldet; mit `VAL01..04` schliesst sich das Popup, und der Report meldet den Fehler in der Statuszeile (T8). Der Nutzer muss
  „Zeiten abgleichen“ neu starten. Geprueft werden wie bisher alle vier Wertfelder, auch ohne Haken.
* **Gross/klein:** `CHAR15` hat keine Kleinbuchstaben; harmlos fuer Zahlen.
* **F1/F4 zum Wertfeld** zeigen jetzt „Char 15“ statt der Hilfe zum Vorgabewert.
* **Zaehler (E1):** „OK“ und „Fehler/Skip“ zaehlen jetzt CA02-Laeufe je Vorgang, dazu kommt „gleicher Vorgang übersprungen“.
  Schlaegt ein Lauf fehl, versucht die naechste markierte Zeile desselben Vorgangs es erneut; derselbe Fehler kann dann
  mehrfach im Protokoll stehen.
* **Reihenfolge Rueckbau:** erst Report, dann Struktur (Abschnitt 9).
