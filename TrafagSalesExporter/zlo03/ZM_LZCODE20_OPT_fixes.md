# ZM_LZCODE20_OPT – Bugfixes

Stand: 27.07.2026. Bezug: Mail Michel/Moltisanti vom 23.07.2026, Datei `b63485-1.xlsx`.

---

## Befund aus der Datei

Die Datei ist ein **Bottom-Up** auf B63485 (Kopf `Verwendet in`, Summenzeile `Summe Bauteil`).

* Zeilen 3–14: 12 VKNR – das ist der Report-Output.
* Zeilen 19–39: 21 VKNR – die Vollliste.
* Differenz: 46105, 46123, 46124, 46125, 46126, 46127, 46128, 46130, 46835.

Wichtig für die MK-Frage: im Bottom-Up-Raster stehen in der Spalte `offene MK`
die Werte der **Kopfmaterialien**, nicht die der Komponente. Für alle 12 Zeilen
steht dort 0 – korrekt, VKNRs haben keinen MK. Die 3100 Stk können also nur aus
dem transponierten Block (`p_trans = 'X'`) stammen, der `gt_md04_full_cache`
über die **Spalten** (= B63485) liest. Genau dieselbe Quelle liest der Top-Down
über die **Zeilen**. Dass beide unterschiedliche Werte liefern, ist ein Bug im
Programm, nicht in ZMD04_CALC.

---

## Bug 1 – KTAB wird nur beim ersten Treffer gefüllt (Ursache „offene MK = 0")

`process_main`, Schritt 8:

```abap
READ TABLE gt_ktab WITH TABLE KEY idnrk = ls_powerbi-kompnr
  TRANSPORTING NO FIELDS.
IF sy-subrc <> 0.
  PERFORM fill_ktab USING ... CHANGING ls_ktab.
  INSERT ls_ktab INTO TABLE gt_ktab.
ENDIF.
```

Eine Komponente wird genau einmal befüllt – beim ersten Vorkommen in
`lt_powerbi`. `fill_ktab` steigt bei `postyp = 'T'` sofort aus:

```abap
IF pv_postyp = 'T'.
  ...
  RETURN.
ENDIF.
```

Trifft der erste Treffer einer Komponente auf eine Textposition, bleiben
**alle** Kennzahlen 0 – Lagerbestand, Endbestand, Stückkosten, `mkmng`, `omkwr`.
Spätere echte Positionen (`postyp = 'L'`) derselben Komponente korrigieren das
nie, weil der Eintrag schon existiert. Bei 20 VKNR mit gemischten Positionstypen
ist das reine Reihenfolge – und erklärt die Asymmetrie zum Bottom-Up, wo B63485
als Spalte direkt aus dem Cache gelesen wird.

Dasselbe Muster greift, wenn der erste Treffer aus einem anderen Grund
unvollständig war.

### Patch 1a – echte Position gewinnt gegen Textposition

Top-Down-Zweig:

```abap
      " KTAB: Komponenten (Zeilen)
      READ TABLE gt_ktab INTO ls_ktab_exist
        WITH TABLE KEY idnrk = ls_powerbi-kompnr.
      IF sy-subrc <> 0.
        PERFORM fill_ktab USING ls_powerbi-kompnr ls_powerbi-matnr
                                ls_powerbi-baugruppe ls_powerbi-postyp
                          CHANGING ls_ktab.
        INSERT ls_ktab INTO TABLE gt_ktab.
      ELSEIF ls_ktab_exist-postyp = 'T' AND ls_powerbi-postyp <> 'T'.
        " Bestehender Eintrag stammt aus einer Textposition und ist leer.
        " Echte Position gewinnt -> Zeile neu aufbauen.
        PERFORM fill_ktab USING ls_powerbi-kompnr ls_powerbi-matnr
                                ls_powerbi-baugruppe ls_powerbi-postyp
                          CHANGING ls_ktab.
        MODIFY TABLE gt_ktab FROM ls_ktab.
      ENDIF.
```

Bottom-Up-Zweig analog, mit `ls_powerbi-matnr` als `idnrk`:

```abap
      READ TABLE gt_ktab INTO ls_ktab_exist
        WITH TABLE KEY idnrk = ls_powerbi-matnr.
      IF sy-subrc <> 0.
        PERFORM fill_ktab USING ls_powerbi-matnr ls_powerbi-kompnr ''
                                ls_powerbi-postyp CHANGING ls_ktab.
        INSERT ls_ktab INTO TABLE gt_ktab.
      ELSEIF ls_ktab_exist-postyp = 'T' AND ls_powerbi-postyp <> 'T'.
        PERFORM fill_ktab USING ls_powerbi-matnr ls_powerbi-kompnr ''
                                ls_powerbi-postyp CHANGING ls_ktab.
        MODIFY TABLE gt_ktab FROM ls_ktab.
      ENDIF.
```

Deklaration in `process_main` ergänzen:

```abap
        ls_ktab_exist TYPE ty_ktab,
```

### Patch 1b – Sicherheitsnetz, reihenfolgeunabhängig

Deckt auch Fälle ab, in denen die Zeile aus einem anderen Grund leer blieb.
Neue Form, Aufruf am Ende von `process_main` direkt vor Schritt 9:

```abap
  PERFORM refresh_ktab_kennzahlen.
```

```abap
*&---------------------------------------------------------------------*
*& FORM refresh_ktab_kennzahlen
*& Zieht MD04-/MK-/Wertkennzahlen für ALLE KTAB-Zeilen nach.
*& Macht das Ergebnis unabhängig davon, in welcher Reihenfolge eine
*& Komponente in lt_powerbi zum ersten Mal auftaucht.
*&---------------------------------------------------------------------*
FORM refresh_ktab_kennzahlen.

  DATA: ls_md04_full TYPE ty_md04_full,
        ls_stamm     TYPE ty_stamm.

  FIELD-SYMBOLS: <fs_ktab> TYPE ty_ktab.

  LOOP AT gt_ktab ASSIGNING <fs_ktab>.

    " Echte Textpositionen bleiben bewusst leer
    IF <fs_ktab>-postyp = 'T'.
      CONTINUE.
    ENDIF.

    " --- Stammdaten nachziehen, falls leer ---
    IF <fs_ktab>-meins IS INITIAL.
      CLEAR ls_stamm.
      READ TABLE gt_stamm_cache INTO ls_stamm
        WITH TABLE KEY matnr = <fs_ktab>-idnrk.
      IF sy-subrc = 0.
        <fs_ktab>-maktx       = ls_stamm-maktx.
        <fs_ktab>-meins       = ls_stamm-meins.
        <fs_ktab>-disst       = ls_stamm-disst.
        <fs_ktab>-mstae       = ls_stamm-mstae.
        <fs_ktab>-mstav       = ls_stamm-mstav.
        <fs_ktab>-zzlzcod     = ls_stamm-zzlzcod.
        <fs_ktab>-zzlzcodsort = ls_stamm-zzlzcodsort.
        <fs_ktab>-dismm       = ls_stamm-dismm.
        <fs_ktab>-minbe       = ls_stamm-minbe.
        <fs_ktab>-disls       = ls_stamm-disls.
        <fs_ktab>-bstfe       = ls_stamm-bstfe.
        <fs_ktab>-eisbe       = ls_stamm-eisbe.
        <fs_ktab>-beskz       = ls_stamm-beskz.
        <fs_ktab>-lbkum       = ls_stamm-lbkum.
        <fs_ktab>-verpr       = ls_stamm-verpr.
        <fs_ktab>-stprs       = ls_stamm-stprs.
        <fs_ktab>-peinh       = ls_stamm-peinh.
        <fs_ktab>-vprsv       = ls_stamm-vprsv.
        <fs_ktab>-salk3       = ls_stamm-salk3.

        IF <fs_ktab>-peinh IS INITIAL.
          <fs_ktab>-peinh = 1.
        ENDIF.
        IF <fs_ktab>-vprsv = 'V'.
          <fs_ktab>-stueckkosten = <fs_ktab>-verpr / <fs_ktab>-peinh.
        ELSE.
          <fs_ktab>-stueckkosten = <fs_ktab>-stprs / <fs_ktab>-peinh.
        ENDIF.
      ENDIF.
    ENDIF.

    " --- MD04 / MK immer aus dem Cache setzen ---
    CLEAR ls_md04_full.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = <fs_ktab>-idnrk.
    CHECK sy-subrc = 0.

    <fs_ktab>-labst     = ls_md04_full-labst.
    <fs_ktab>-feste_zug = ls_md04_full-feste_zug.
    <fs_ktab>-gepl_zug  = ls_md04_full-gepl_zug.
    <fs_ktab>-feste_abg = ls_md04_full-feste_abg.
    <fs_ktab>-gepl_abg  = ls_md04_full-gepl_abg.
    <fs_ktab>-verbr     = ls_md04_full-verbr.
    <fs_ktab>-omeng     = ls_md04_full-omeng.
    <fs_ktab>-owert     = ls_md04_full-owert.
    <fs_ktab>-mkmng     = ls_md04_full-mkmng.
    <fs_ktab>-omkwr     = ls_md04_full-omkwr.

    <fs_ktab>-endbestand = <fs_ktab>-labst
                         + <fs_ktab>-feste_zug
                         + <fs_ktab>-gepl_zug
                         - <fs_ktab>-feste_abg
                         - <fs_ktab>-gepl_abg.

    <fs_ktab>-wert_feste_zug  = <fs_ktab>-feste_zug  * <fs_ktab>-stueckkosten.
    <fs_ktab>-wert_gepl_zug   = <fs_ktab>-gepl_zug   * <fs_ktab>-stueckkosten.
    <fs_ktab>-wert_feste_abg  = <fs_ktab>-feste_abg  * <fs_ktab>-stueckkosten.
    <fs_ktab>-wert_gepl_abg   = <fs_ktab>-gepl_abg   * <fs_ktab>-stueckkosten.
    <fs_ktab>-wert_endbestand = <fs_ktab>-endbestand * <fs_ktab>-stueckkosten.

  ENDLOOP.

ENDFORM.
```

`gt_ktab` ist SORTED mit UNIQUE KEY `idnrk` – das Ändern von Nichtschlüsselfeldern
über Feldsymbol ist zulässig.

---

## Bug 2 – `ls_stamm_v` wird vor dem READ nicht geleert

Bottom-Up-Zweig in `process_main`:

```abap
      READ TABLE gt_stamm_cache INTO ls_stamm_v
        WITH TABLE KEY matnr = ls_powerbi-matnr.
      IF sy-subrc <> 0.
        ...
      ENDIF.
      IF ls_stamm_v-lvorm IS NOT INITIAL AND p_lvorm = ' '.
        CONTINUE.
      ENDIF.
```

Schlägt der READ fehl, steht in `ls_stamm_v` noch der Satz aus dem
vorhergehenden Schleifendurchlauf, und die Löschvormerkung wird gegen das
falsche Material geprüft.

```abap
      CLEAR ls_stamm_v.
      READ TABLE gt_stamm_cache INTO ls_stamm_v
        WITH TABLE KEY matnr = ls_powerbi-matnr.
```

Dasselbe im Top-Down-Zweig vor der Ermittlung von `lv_meins_komp` – dort wird
sonst die Mengeneinheit des vorherigen Materials für die Umrechnung benutzt,
was still falsche Mengen erzeugt:

```abap
    CLEAR ls_stamm_v.
    READ TABLE gt_stamm_cache INTO ls_stamm_v
      WITH TABLE KEY matnr = ls_powerbi-kompnr.
```

---

## Bug 3 – `DELETE gt_ktab WHERE maktx IS INITIAL` trifft zu viel

```abap
  IF p_txtpo = ' '.
    DELETE gt_ktab WHERE maktx IS INITIAL.
  ENDIF.
```

Gemeint sind Textpositionen, gelöscht wird aber jedes Material ohne
MAKT-Eintrag in `sy-langu`. Der Positionstyp steht inzwischen im Satz:

```abap
  IF p_txtpo = ' '.
    DELETE gt_ktab WHERE postyp = 'T'.
  ENDIF.
```

---

## Bug 4 – Exklusivität ignoriert Verwendungen unterhalb von FERT

`load_exclusivity_topdown` verwirft alle Elternmaterialien, die nicht `FERT`
sind:

```abap
      IF ls_vknr_mara-mtart = 'FERT'.
        APPEND ls_usage TO lt_all_usage_fert.
      ENDIF.
```

Eine Komponente, die zusätzlich in einer HALB-Baugruppe steckt und über diese
in nicht selektierten Geräten läuft, wird trotzdem als exklusiv markiert. Für
den Phase-Out heisst das: Kennzeichen `X`, obwohl der Mengenkontrakt weiter
gebraucht wird. Das ist die riskantere Fehlerrichtung von beiden.

Sauber wäre eine rekursive Auflösung. Minimal-invasiv und ehrlich ist ein
dritter Zustand statt einer Falschaussage:

```abap
  " nach dem bestehenden FERT-Loop:
  IF lv_is_exklusiv = 'X'.
    " gibt es Nicht-FERT-Eltern ausserhalb der Selektion?
    LOOP AT lt_all_usage INTO ls_usage WHERE kompnr = lv_kompnr.
      READ TABLE lt_vknr_mara INTO ls_vknr_mara
        WITH TABLE KEY matnr = ls_usage-vknr.
      CHECK sy-subrc = 0.
      CHECK ls_vknr_mara-mtart <> 'FERT'.
      READ TABLE lt_sel_vknr WITH TABLE KEY table_line = ls_usage-vknr
        TRANSPORTING NO FIELDS.
      IF sy-subrc <> 0.
        lv_is_exklusiv = '?'.   " nicht entscheidbar, manuell prüfen
        EXIT.
      ENDIF.
    ENDLOOP.
  ENDIF.
```

`ty_exkl-exklusiv` ist `char1`, `?` passt. Die Ausgabe in
`create_excel_output` schreibt heute hart `'X'`; dort stattdessen
`ls_exkl-exklusiv` durchreichen:

```abap
      READ TABLE gt_exkl_cache INTO ls_exkl
        WITH TABLE KEY kompnr = ls_ktab-idnrk.
      IF sy-subrc = 0.
        CONCATENATE lv_line gv_tab ls_exkl-exklusiv INTO lv_line.
      ELSE.
        CONCATENATE lv_line gv_tab '' INTO lv_line.
      ENDIF.
```

---

## Bug 5 – `sy-subrc` nach zwei READs

In `ausgabe_spalten_details`, Blöcke „Wert Feste Zugänge" bis „Wert Endbestand":

```abap
    READ TABLE gt_md04_full_cache INTO ls_md04_full ...
    READ TABLE gt_stamm_cache INTO ls_stamm ...
    IF sy-subrc = 0.
```

Geprüft wird nur der zweite READ. Aktuell harmlos, weil vorher `CLEAR`
läuft – aber die Konstruktion bricht bei der nächsten Änderung. `sy-subrc`
direkt nach jedem READ in eine eigene Variable sichern.

---

## 12 statt 21 VKNR im Bottom-Up

Die neun fehlenden Nummern (46105, 46123–46128, 46130, 46835) sind ein
Filtereffekt, kein Absturz. Kandidaten, in dieser Reihenfolge zu prüfen:

1. **Löschvormerkung.** `p_lvorm = ' '` filtert alle Köpfe mit `MARA-LVORM`.
   Ein Blick in SE16 auf die neun Nummern klärt das in einer Minute. Falls das
   die Ursache ist: `p_lvorm = 'X'` setzen, und die Vorbelegung überdenken –
   beim Phase Out sind löschvorgemerkte Geräte gerade die interessanten.
2. **`menge` / `mengeneinheit`.** Schritt 2 filtert
   `menge <> '' AND mengeneinheit <> ''`. Positionen ohne gepflegte Menge
   fallen raus, ohne dass es jemand sieht.
3. **Bestand in `zpowerbi_vc_txt`.** Wenn die neun Nummern dort gar nicht
   stehen, liegt es am Aufbau dieser Tabelle, nicht am Report.

Prüfabfrage:

```
SE16 -> ZPOWERBI_VC_TXT
  KOMPNR = B63485
  -> Anzahl distinkter MATNR
SE16 -> MARA
  MATNR in (46105, 46123, 46124, 46125, 46126, 46127, 46128, 46130, 46835)
  -> LVORM, MTART, ZZTYP_F4
```

---

## CS15 42 vs. ZLO03 21

Die beiden Auswertungen sind nicht vergleichbar aufgebaut. Unterschiede, die
Nummern kosten:

| | CS15 | ZLO03 |
|---|---|---|
| Datenquelle | STPO/MAST direkt | `zpowerbi_vc_txt`, VC-aufgelöst |
| Werk | wählbar | `gc_werks = '1100'` hart |
| Materialart | alle | `mtart = 'FERT'` bei Exklusivität und „Weitere VKNR" |
| Löschvormerkung | wählbar | über `p_lvorm`, Vorbelegung aus |
| Positionen ohne Menge | enthalten | ausgefiltert |
| Materialstatus 99 | enthalten | bei „Weitere VKNR" ausgefiltert |

Vor einer Codeänderung: die 42 aus CS15 und die 21 aus ZLO03 gegenüberstellen
und die 21 Differenznummern durch die obige Tabelle laufen lassen. Erst dann ist
klar, welcher Filter wie viel kostet.

Das Werk als Konstante ist unabhängig davon einen Selektionsparameter wert.
