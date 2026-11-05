# ZLO03 / Materialanteil: Wafer um Faktor 3'738 bis 4'750 zu hoch (Befund 2026-11-05)

Stand: 2026-11-05. Nur lesende Diagnose in T76/100 (SapProbe `table-read`, `abap-read`), P76 nicht angefasst, nichts in SAP
geändert, kein Report ausgeführt. Noch nichts umgesetzt; FIX 26 unten ist ein Vorschlag mit Codeskizze.
Zurück: `zlo03/CLAUDE.md`.

## 1. Meldung

Produktivdashboard, Einkauf › Spend-Aufriss › „Materialanteil je Produktgruppe“, 01.2026 bis 11.2026: Produktgruppe
HW_ZUKAUFSORT hat Umsatz bestätigt CHF 3'643'696 und Materialanteil CHF 957'060'506 (26'266 %). Treiber in Warengruppe 10.01.00:

| Komponente | Bedarfsmenge | Einheit | Stückkosten | Wert |
| --- | ---: | --- | ---: | ---: |
| E11283 Wafer XC018-R3191 - TX2a | 487'824 | RO | 1'239.86 | 604'833'465 |
| E11282 Wafer XC018-R3191 - TX1b | 64'856 | RO | 3'207.28 | 208'011'352 |
| E11301 WAFER XC018-R3191-TX2D | 115'465 | RO | 1'239.86 | 143'160'435 |

Die drei Wafer machen CHF 956'005'252 aus, alle übrigen Teile der Gruppe zusammen CHF 1'055'254.

## 2. Ergebnis in einem Satz

Die Hypothese „MARM/PEINH greift nicht“ trifft **nicht** zu. Ursache ist die **Basismenge der Stückliste (STKO-BMENG)**: Die
Chip-Stücklisten sind auf 3'738 bis 4'750 Stück bezogen und enthalten 1 Rolle (= 1 Wafer). `ZPOWERBI_VC_TXT` speichert nur die
Positionsmenge 1.000 ROL ohne Basismenge, ZLO03 liest sie als „1 Wafer je 1 Chip“. Jeder verkaufte Chip zählt dadurch einen
ganzen Wafer.

## 3. Belege (T76/100, gelesen am 2026-11-05)

### 3.1 Material, Einheit, Preis

| Material | `MARA-MEINS` | `MTART` | `MATKL` | `MARM` | `MBEW` 1100 (VPRSV, STPRS, PEINH) |
| --- | --- | --- | --- | --- | --- |
| E11283 | ROL | HALB | 10.01.00 | ROL 1/1; **ST 1/4548** (1 ROL = 4'548 ST) | S, 1'186.28, 1 |
| E11282 | ROL | HALB | 10.01.00 | nur ROL 1/1, **kein ST** | S, 3'207.28, 1 |
| E11301 | ROL | HALB | 10.01.00 | ROL 1/1; **ST 1/4548** | S, 1'239.86, 1 |

Basiseinheit ist die Rolle (intern `ROL`, angezeigt `RO`), PEINH ist 1, der Preis gilt je Rolle. Stückkosten im Dashboard sind
also richtig (T76 hat für E11283 noch den älteren Preis 1'186.28, P76 zeigt 1'239.86).

### 3.2 Stücklisten, in denen die Wafer stehen

| Kopfmaterial (HALB, ST) | `MAST` (Werk, STLAN, STLNR) | `STKO-BMENG` | `STPO` Wafer-Position |
| --- | --- | ---: | --- |
| E11220 | 1100, 3, 00026504 | **3'738 ST** | E11282, 1.000 ROL |
| E11221 | 1100, 3, 00026939 | **4'750 ST** | E11283, 1.000 ROL |
| E11224 | 1100, 3, 00026940 | **4'548 ST** | E11283, 1.000 ROL |
| E11228 | 1100, 3, 00038115 | **4'548 ST** | E11301, 1.000 ROL |
| E11229 | 1100, 3, 00038116 | **4'548 ST** | E11301, 1.000 ROL |

Fachlich heisst das: Aus einem Wafer werden 3'738 bis 4'750 Chips. Die Stückliste ist richtig gepflegt; nur die Auswertung
ignoriert die Basismenge.

### 3.3 Was in `ZPOWERBI_VC_TXT` steht

Für `KOMPNR` E11282, E11283, E11301 gibt es in T76 genau fünf Zeilen, alle `STUFE 01`, `MENGE 1.000`, `MENGENEINHEIT ROL`,
Kopfmaterial jeweils der Chip (E11220, E11221, E11224, E11228, E11229). Die Tabelle hat kein Feld für die Basismenge (`DD03L`:
ID, STUFE, KPOSITION, OBJ, KOMPNR, MENGE, MENGENEINHEIT, KOM_MSTAV, BAUGRUPPE, MATERIALART, POSTYP, MATNR, KOM_MSTAE, MAT_MSTAE,
MAT_MSTAV, MAT_TXT, MAMI).

### 3.4 Wie die Chips ins Dashboard kommen

Die Chips werden selbst verkauft: `VBAP` in T76 hat seit 2025 direkte Positionen für E11221, E11224, E11228, E11229 und E11220
in ST (z. B. 207'000 ST E11221 in 0020028821, laufend 3'000 bis 12'000 ST je Auftrag zu rund CHF 2.05 bzw. 0.80 je Stück).
Damit sind die Chips selbst die verkürzten Nummern (VKNR) in ZC23, und der Wafer steht direkt auf Stufe 01 darunter.

Die Baugruppen, in denen die Chips sonst stecken (B53157, B53158, B54911, B53501, B53145, C42698 u. a.), haben nur
Stücklisten mit Verwendung 9 ohne Werk; `CS_BOM_EXPL_MAT_V2` mit `PP01`/Werk 1100 löst sie nicht auf (in VKNR 45726 steht
C42698 als Blatt). Über Geräte kommt also in T76 kein Wafer in den Bedarf.

### 3.5 Rechnung, die das Dashboard ergibt

Das Dashboard rechnet je Teil `Σ ZC23-Menge × Menge je 1 VKNR × Stückkosten` (`Services/PurchasingMaterialShareQuery.cs`,
`Build`: `part.Quantity += bmeng * row.Menge`, `part.Value += bmeng * row.Wert`).

- 487'824 × 1'239.86 = 604'833'464.64, also genau der angezeigte Wert. Die Menge je verkauftem Chip ist 1.000 ROL.
- 64'856 × 3'207.28 = 208'011'351.68; 115'465 × 1'239.86 = 143'160'434.90. Ebenso Menge 1.000.

Richtig wäre Menge = 1 / BMENG ROL je Chip:

| Wafer | Rollen richtig | Wert richtig (CHF) |
| --- | ---: | ---: |
| E11283 (Anteil E11221 zu 4'750 und E11224 zu 4'548 unbekannt) | 102.7 bis 107.3 | 127'333 bis 132'989 |
| E11282 (BMENG 3'738) | 17.35 | 55'648 |
| E11301 (BMENG 4'548) | 25.39 | 31'478 |

HW_ZUKAUFSORT läge damit bei rund CHF 1.27 Mio. Materialanteil auf CHF 3.64 Mio. Umsatz (rund 35 %), statt 957 Mio.
Die Wafer kosten je Chip CHF 0.26 bis 0.86 (z. B. 1'239.86 / 4'548 = 0.2726), bei Verkaufspreisen von 0.80 bis 2.05 je Chip.

## 4. Codestellen

### 4.1 Generator: Positionsmenge statt berechneter Menge

Beide Kopien des Generators (`Z_VC_ZLO03` → `ZPOWERBI_VC`, `Z_VC_ZLO03_TXT` → `ZAT_VC_TXT`, beide `REPORT z_vc_batch`, in T76
per `abap-read` gelesen) rufen `CS_BOM_EXPL_MAT_V2` mit `mehrs = 'X'`, `emeng = 1` und schreiben

```abap
menge = ls_stb-menge                  " Z_VC_ZLO03 Zeile 345 (STPOX-MENGE, Datenelement KMPMG)
...
menge = |{ ls_bfs-menge }|            " Zeile 435
```

`STPOX-MENGE` ist die Positionsmenge bezogen auf die Basismenge der Stückliste, nicht die auf `EMENG = 1` umgerechnete Menge
(`STPOX-MNGKO`). Hinweis: Die T76-Fassung von `Z_VC_ZLO03_TXT` schreibt `ZAT_VC_TXT`; `ZPOWERBI_VC_TXT` ist in T76 eine Kopie von
`ZPOWERBI_VC` (`Z_KOI_VC_TXT_KOPIE`, siehe `BEFUND_SYSTEMABGLEICH_2026-08-03.md`). Welches Programm in P76 `ZPOWERBI_VC_TXT`
füllt, ist nicht geprüft (P76 war ausgeschlossen); die Dashboardrechnung in 3.5 zeigt aber, dass P76 dieselbe Menge 1.000 liefert.

`MNGKO` hätte nicht geholfen: das Feld ist `QUAN 13,3`, 1 / 4'750 = 0.000211 würde zu 0.000.

### 4.2 `Z_ZLO03_TURBO2` (T76-Stand = `Z_ZLO03_TURBO2_FIX25_2026-11-02.abap`, `diff` Exitcode 0)

| Zeile | Stelle | Wirkung |
| --- | --- | --- |
| 712–718 | `SELECT ... FROM zpowerbi_vc_txt` (Top-Down) | liest MENGE als Text, kein Basismengenbezug |
| 820–837 | FIX 23: `load_bom_units` / `bom_unit_menge` oder `convert_menge` | für die Wafer wirkungslos und richtig so: Stücklisteneinheit ROL = Basiseinheit ROL, also kein Eintrag in `gt_bom_unit`, `convert_menge` ROL → ROL gibt 1 |
| 896–905 | Top-Down `ls_mtab-menge = lv_menge`, `COLLECT ... INTO gt_mtab` | 1.000 ROL je VKNR |
| 970–978 | Bottom-Up, gleiche Zuordnung | ebenso |
| 138, 584 | `ty_mtab-menge` und `lv_menge` `TYPE menge_d` (3 Nachkommastellen) | eine richtige Menge 0.000211 würde zu 0.000 |
| 532–564 | FIX 25 `app_export`: `ls_app-menge = ls_mtab-menge`, `ls_app-wert = ls_app-menge * ls_app-stk` | gibt 1.000 und 1'239.86 je Chip an `ZloTopDownSet` |
| 3446–3454 | Excel-Spalte je VKNR | dieselbe Menge 1 auch in der ZLO03-Excel-Ausgabe |

Damit ist der Fehler nicht nur im Dashboard: ZLO03 Top-Down auf E11221 zeigt in Excel ebenfalls „1“ Rolle je Chip.

### 4.3 Dashboard

`Services/PurchasingMaterialShareLoader.cs` und `...Query.cs` rechnen korrekt mit dem, was `ZloTopDownSet` liefert. Eine Korrektur
dort wäre falsch platziert (kein BMENG verfügbar, Excel bliebe falsch). Nach dem Import des Fixes genügt ein neuer Einkauf-Lauf
(Tabelle `PurchasingMaterialShareBom` wird vollständig ersetzt).

## 5. Betroffene Materialien (Muster)

**Muster:** Kopfmaterial mit Stückliste, deren Basismenge ≠ 1 ist (`STKO-BMENG <> 1`), und dessen Komponenten über ZLO03 oder
`ZloTopDownSet` gerechnet werden. Fehlerfaktor = BMENG (zu hoch bei BMENG > 1, zu tief bei BMENG < 1).

In T76 liefert `STKO` über alle Stücklistentypen genau sechs Köpfe mit `BMENG <> 1`: die fünf Chip-Stücklisten aus 3.2 und
00026393 (PLM8252, Verwendung 8, ohne Positionen). Betroffen sind in T76 also genau **E11282, E11283, E11301** über die Chips
E11220, E11221, E11224, E11228, E11229.

Vorbehalt: T76 ist eine Kopie (Stand etwa März 2026). Ob P76 inzwischen weitere Stücklisten mit `BMENG <> 1` hat, muss vor
dem Transport in P76 geprüft werden, z. B. SE16 `STKO` mit `STLTY = M` und `BMENG <> 1` (P76 hier nicht angefasst).

Nebenbefunde, die nicht die Ursache sind:

1. **E11226, E11227 (Status 99)**: Stückliste mit BMENG 1 ST und 1 ROL Wafer E11300 (Status 99). Ein Chip je Wafer ist
   offensichtlich falsch gepflegt; wegen Status 99 ohne Verkauf, für das Dashboard folgenlos.
2. **MARM ist nicht die richtige Quelle**: E11282 hat gar keine Einheit ST, E11283 hat 1 ROL = 4'548 ST, die Stückliste von
   E11221 aber 4'750 ST je Rolle. Die Ausbeute steht in STKO, nicht in MARM.
3. **Mehrstufigkeit (bekannter Punkt G4)**: `ZPOWERBI_VC_TXT` hat in T76 94'143 Baugruppenzeilen mit Menge ≠ 1 (80'547 in ST,
   52'120 davon auf Stufe 02). Die Mengen ihrer Komponenten werden nicht mit der Baugruppenmenge multipliziert, im Materialanteil
   also zu tief. Beispiel VKNR 55374: D34576 → D34545 2 ST → dessen Komponenten zählen einfach. Dazu enthält die Stückliste
   00045625 von D34545 sich selbst (D34545 auf Stufe 02 mit 2 ST und auf Stufe 03 mit 1 ST, COLLECT ergibt 3). Eigenes Thema,
   nicht Teil von FIX 26.
4. **`KOM_MSTAE` enthält das Elternmaterial**, `MAT_MSTAE` die ID der Elternzeile (Generator: `kom_mstae = lv_parent_matnr_full`,
   MATN1-Ausgabeformat; in VKNR 45726 z. B. H05028 unter E01835). Das widerspricht `zlo03/CLAUDE.md` Punkt A („Feld enthält einen
   Materialstatus, in allen geprüften Sätzen leer“), zumindest für die T76-Kopie. Für G4 wäre das die Grundlage einer kumulierten
   Menge. Doku nicht geändert.
5. `load_stammdaten_cache` setzt `has_bom` nur bei `MAST-STLAN = '1'`; die Werk-1100-Stücklisten in den Belegen haben Verwendung 3.
   Betrifft nur das Baugruppen-Kennzeichen, nicht die Mengen.

## 6. Vorschlag FIX 26: Basismenge der Elternstückliste in ZLO03

**Wo:** `Z_ZLO03_TURBO2`, nicht im Generator. Gründe: `ZPOWERBI_VC_TXT` hat kein Basismengenfeld, der P76-Befüller ist nicht
geklärt, eine Generatoränderung träfe alle Leser der Tabelle (ZLO03, Exklusivität, Power BI), und `MNGKO` hätte zu wenige Stellen.
In ZLO03 wirkt die Korrektur gleichzeitig auf Top-Down-Excel, Bottom-Up und `app_export`/`ZloTopDownSet`.

**Regel:** Menge je 1 Kopfmaterial = Positionsmenge / BMENG der Stückliste des direkten Elternmaterials. Eltern sind auf Stufe 01
`MATNR` (die VKNR), ab Stufe 02 `KOM_MSTAE`. BMENG nur verwenden, wenn die Stückliste im Werk 1100 eindeutig ist und `BMEIN` der
Basiseinheit des Elternmaterials entspricht; sonst bleibt es wie bisher (falsch-negativ ist hier die vorsichtige Seite).

**Codeskizze** (klassische Syntax wie im Bestand; vor dem Einbau gegen den Live-Stand legen und Syntaxprüfung lesen):

```abap
*** FIX 26: Typen und Cache (zu den FIX-23-Typen)
  BEGIN OF ty_bmeng,
    matnr      TYPE matnr,
    bmeng      TYPE stko-bmeng,
    bmein      TYPE stko-bmein,
    mehrdeutig TYPE char1,
  END OF ty_bmeng,
  tt_bmeng TYPE HASHED TABLE OF ty_bmeng WITH UNIQUE KEY matnr.
DATA: gt_bmeng TYPE tt_bmeng.   " FIX 26

*** FIX 26: ty_mtab-menge mit 10 Stellen statt menge_d (3 Stellen): 1/4750 = 0.000211 wuerde 0.000
  BEGIN OF ty_mtab,
    matnr TYPE matnr,
    idnrk TYPE matnr,
    menge TYPE p LENGTH 16 DECIMALS 10,
  END OF ty_mtab,

*&---------------------------------------------------------------------*
*& FORM load_bmeng (FIX 26)
*& Basismenge der gueltigen Stueckliste je Material im Werk gc_werks. Nur Saetze mit
*& BMENG <> 1 werden behalten. Mehrere Stuecklisten mit verschiedener Basismenge:
*& 'mehrdeutig', dann wird nicht geteilt.
*&---------------------------------------------------------------------*
FORM load_bmeng USING pt_matnr TYPE tt_matnr_list.

  TYPES: BEGIN OF lty_raw,
           matnr TYPE matnr,
           stlan TYPE mast-stlan,
           stlal TYPE mast-stlal,
           datuv TYPE stko-datuv,
           bmeng TYPE stko-bmeng,
           bmein TYPE stko-bmein,
         END OF lty_raw.
  DATA: lt_raw  TYPE STANDARD TABLE OF lty_raw,
        ls_raw  TYPE lty_raw,
        ls_last TYPE lty_raw,
        ls_bm   TYPE ty_bmeng.
  FIELD-SYMBOLS: <fs_bm> TYPE ty_bmeng.

  CLEAR gt_bmeng.
  CHECK pt_matnr IS NOT INITIAL.

  SELECT a~matnr a~stlan a~stlal k~datuv k~bmeng k~bmein
    FROM mast AS a
    INNER JOIN stko AS k ON k~stlnr = a~stlnr AND k~stlal = a~stlal
    INTO TABLE lt_raw
    FOR ALL ENTRIES IN pt_matnr
    WHERE a~matnr = pt_matnr-matnr
      AND a~werks = gc_werks
      AND k~stlty = 'M'
      AND k~lkenz = ' '
      AND k~loekz = ' '
      AND k~datuv <= sy-datum.

  " je Material/Verwendung/Alternative nur der juengste gueltige Kopf
  SORT lt_raw BY matnr stlan stlal datuv DESCENDING.
  CLEAR ls_last.
  LOOP AT lt_raw INTO ls_raw.
    IF ls_raw-matnr = ls_last-matnr AND ls_raw-stlan = ls_last-stlan
       AND ls_raw-stlal = ls_last-stlal.
      CONTINUE.
    ENDIF.
    ls_last = ls_raw.
    READ TABLE gt_bmeng ASSIGNING <fs_bm> WITH TABLE KEY matnr = ls_raw-matnr.
    IF sy-subrc <> 0.
      CLEAR ls_bm.
      ls_bm-matnr = ls_raw-matnr.
      ls_bm-bmeng = ls_raw-bmeng.
      ls_bm-bmein = ls_raw-bmein.
      INSERT ls_bm INTO TABLE gt_bmeng.
    ELSEIF <fs_bm>-bmeng <> ls_raw-bmeng OR <fs_bm>-bmein <> ls_raw-bmein.
      <fs_bm>-mehrdeutig = 'X'.
    ENDIF.
  ENDLOOP.

  DELETE gt_bmeng WHERE bmeng = 1 AND mehrdeutig = ' '.

ENDFORM.

*** FIX 26: Aufruf in process_main, direkt nach load_bom_units (Zeile 786)
  PERFORM load_bmeng USING lt_all_matnr.
  " lt_all_matnr enthaelt alle MATNR und KOMPNR, also auch jedes Elternmaterial ab Stufe 02

*** FIX 26: in process_main nach dem FIX-23-Block (nach Zeile 837), vor IF p_topd = 'X'
    DATA: lv_menge_fix26  TYPE p LENGTH 16 DECIMALS 10,   " FIX 26
          lv_eltern_fix26 TYPE matnr,                     " FIX 26
          ls_bmeng_fix26  TYPE ty_bmeng,                  " FIX 26
          ls_stamm_fix26  TYPE ty_stamm.                  " FIX 26
    lv_menge_fix26 = lv_menge.
    IF ls_powerbi-stufe = '01' OR ls_powerbi-kom_mstae IS INITIAL.
      lv_eltern_fix26 = ls_powerbi-matnr.
    ELSE.
      " KOM_MSTAE steht im MATN1-Ausgabeformat (numerische Nummern ohne fuehrende Nullen)
      CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
        EXPORTING  input  = ls_powerbi-kom_mstae
        IMPORTING  output = lv_eltern_fix26
        EXCEPTIONS OTHERS = 1.
      IF sy-subrc <> 0.
        CLEAR lv_eltern_fix26.
      ENDIF.
    ENDIF.
    READ TABLE gt_bmeng INTO ls_bmeng_fix26 WITH TABLE KEY matnr = lv_eltern_fix26.
    IF sy-subrc = 0 AND ls_bmeng_fix26-mehrdeutig IS INITIAL AND ls_bmeng_fix26-bmeng > 0.
      READ TABLE gt_stamm_cache INTO ls_stamm_fix26 WITH TABLE KEY matnr = lv_eltern_fix26.
      IF sy-subrc = 0 AND ls_stamm_fix26-meins = ls_bmeng_fix26-bmein.
        lv_menge_fix26 = lv_menge_fix26 / ls_bmeng_fix26-bmeng.
      ENDIF.
    ENDIF.

*** FIX 26: Top-Down (Zeile 900) und Bottom-Up (Zeilen 974 und 977) mit lv_menge_fix26 statt lv_menge
        ls_mtab-menge = lv_menge_fix26.
        ...
          ls_mtab-menge = ls_mtab_exist-menge + lv_menge_fix26.
        ...
          ls_mtab-menge = lv_menge_fix26.

*** FIX 26: app_export (Zeilen 548 und 555): Wert aus der ungerundeten Menge
      ls_app-menge = ls_mtab-menge.                 " DEC 28,6 rundet auf 6 Stellen
      ...
      ls_app-wert  = ls_mtab-menge * ls_app-stk.   " vorher ls_app-menge * ls_app-stk

*** FIX 26: Excel-Spalte je VKNR (Zeile 3449): Zahl ohne Nullen am Ende (FORM zahl_text, FIX 24)
        PERFORM zahl_text USING ls_mtab-menge CHANGING lv_menge.
```

Hinweise zur Skizze:

- `lv_menge` bleibt `menge_d`, weil `convert_menge` und `bom_unit_menge` es so erwarten; geteilt wird erst danach in die breitere
  Variable.
- `zahl_text` rundet auf 6 Stellen; 1 / 4'750 erscheint als 0.000211. Ganze Mengen erscheinen wie bisher ohne Nachkommastellen
  (bisher „1.000“, danach „1“): kleine Formatänderung in der Excel-Spalte, Ann-Katrin und Sandro kurz sagen.
- Weitere Leser von `ty_mtab`: `gt_eltern_cache` (Zeile 368) und `lt_matnr_per_kompnr` (Zeile 1570) nutzen nur die Schlüssel;
  nach dem Typwechsel trotzdem per `grep ty_mtab` und Syntaxprüfung kontrollieren (LEARNINGS 2026-10-30: alle Ausgabepfade).
- Bottom-Up-Details (`ausgabe_spalten_details`) und Exklusivität rechnen nicht mit `gt_mtab-menge`; dort ändert sich nichts.
- G4 (Mehrstufigkeit) bleibt bewusst draussen. Mit `MAT_MSTAE` (ID der Elternzeile) wäre eine kumulierte Menge möglich
  (Menge × Menge der Elternzeile / BMENG), aber nur nach dem Vergleich gegen CS12, wie in `zlo03/CLAUDE.md` G4 festgehalten.

**Test in T76 nach dem Einspielen** (eigene Sitzung, eigener Auftrag, Ablauf wie FIX 23/24):

| Fall | Auswahl | Erwartung |
| --- | --- | --- |
| A | Top-Down E11221 | E11283: Menge 0.000211 ROL je Chip, Stückkosten 1'186.28 (T76), Wert ≈ 0.2497 CHF je Chip |
| B | Top-Down E11224, E11220, E11228 | 1 / 4'548, 1 / 3'738, 1 / 4'548 |
| C | Top-Down 41019 (FIX-23-Beleg) | zellgleich mit dem FIX-24-Lauf bis auf das Format der Mengenspalte (alle BMENG = 1) |
| D | `Z_KOI_ZLO_APP_TEST` 41019 | weiter 94 Komponenten, 216.73 CHF je Stück |
| E | Bottom-Up E11283 | Spalte E11283: E11221 0.000211, E11224 0.00022 |
| F | `ZloTopDownSet` im Gateway Client für E11221 | `Menge` 0.000211, `Wert` 0.249743 |

**Danach in P76 (Ingo):** vor dem Transport `STKO` mit `BMENG <> 1` lesen (weitere betroffene Köpfe?), nach dem Import
Einkauf-Lauf neu starten und HW_ZUKAUFSORT prüfen: erwartet rund CHF 1.27 Mio. statt 957 Mio.

## 7. Wie gelesen wurde (reproduzierbar, nur T76)

```powershell
.\.tmp_sap_probe\RunSapProbe.ps1 table-read MARA --fields 'MATNR,MEINS,MTART,MATKL,BSTME' --where "MATNR IN ('E11283','E11282','E11301')" --format csv
.\.tmp_sap_probe\RunSapProbe.ps1 table-read MARM --fields 'MATNR,MEINH,UMREZ,UMREN' --where "MATNR IN ('E11283','E11282','E11301')" --format csv
.\.tmp_sap_probe\RunSapProbe.ps1 table-read MBEW --fields 'MATNR,BWKEY,BWTAR,VPRSV,STPRS,VERPR,PEINH,LBKUM,SALK3' --where "MATNR IN ('E11283','E11282','E11301')" --format csv
.\.tmp_sap_probe\RunSapProbe.ps1 table-read STPO --fields 'STLTY,STLNR,STLKN,IDNRK,POSTP,MENGE,MEINS,DATUV,LKENZ' --where "IDNRK IN ('E11283','E11282','E11301')" --rowcount 5000 --format csv
.\.tmp_sap_probe\RunSapProbe.ps1 table-read STKO --fields 'STLTY,STLNR,STLAL,BMENG,BMEIN' --where "BMENG > 1 OR BMENG < 1" --rowcount 99999 --format csv --quiet
.\.tmp_sap_probe\RunSapProbe.ps1 table-read ZPOWERBI_VC_TXT --fields 'MATNR,STUFE,KOMPNR,MENGE,MENGENEINHEIT,POSTYP' --where "KOMPNR LIKE 'E1128%' OR KOMPNR LIKE 'E1130%'" --rowcount 2000 --format csv
.\.tmp_sap_probe\RunSapProbe.ps1 abap-read Z_VC_ZLO03 > datei      # dazu Z_VC_ZLO03_TXT, Z_ZLO03_TURBO2
```

Nicht gelaufen: `table-read TC05` (`TABLE_NOT_AVAILABLE`, nicht weiter probiert; die Auswahlpriorität von PP01 wird für die Skizze
nicht gebraucht, weil jede Chip-Stückliste genau einen MAST-Satz hat).
