*&---------------------------------------------------------------------*
*& Report Z_LO03_MD04_BATCH
*&---------------------------------------------------------------------*
*& Zweck : Ermittelt je Material/Werk die MD04-Kennzahlen sowie
*&         Bestands-, Bestell-, Kontrakt- und Verbrauchsdaten und
*&         schreibt sie nach ZMD04_CALC (Basis fuer ZLO03 und Power BI).
*&
*& Stand : 25.11.2026
*&
*** FIX 30 (25.11.2026): Abrufe (EKAB) wurden ohne Abrufbeleg gelesen.
***   FOR ALL ENTRIES entfernt gleiche Ergebniszeilen; zwei Abrufe eines
***   Kontrakts mit gleicher Menge zaehlten deshalb nur einmal und die
***   offene Kontraktmenge (MKMNG) war zu hoch. Beispiel B64344: 8'456
***   statt 2'120 (Meldung Armin Guentensperger 25.11.2026).
*** FIX 31 (25.11.2026): Schalter P_KTABG. Angekreuzt zaehlen auch
***   abgelaufene, nicht geloeschte Kontrakte mit Restmenge mit (wie
***   ME3M). Vorschlag an: Entscheid Armin Guentensperger 25.11.2026,
***   die Restmenge ist eine offene Verpflichtung gegenueber dem
***   Lieferanten. Nicht angekreuzt = Verhalten vor FIX 31 (nur heute
***   gueltige Kontrakte). Wirkt nur zusammen mit P_KTGUE.
*** FIX 32 (25.11.2026): MDBS ohne Einteilung (ETENR) gelesen. Zwei
***   Einteilungen einer Bestellposition mit gleicher Menge und gleichem
***   Wareneingang zaehlten nur einmal, die offene Bestellmenge (OMENG)
***   war zu tief. Gleiche Ursache wie FIX 30 (FOR ALL ENTRIES).
*** FIX 33 (25.11.2026): MSEG ohne Belegzeile (ZEILE) gelesen. Zwei
***   Zeilen eines Materialbelegs mit gleicher Menge zaehlten nur einmal,
***   der Verbrauch aus Kundeneinzelbestand (VERBR) war zu tief.
*** FIX 34 (25.11.2026): AFPO ohne Position (POSNR) gelesen. Zwei
***   Positionen eines Auftrags mit gleicher Menge haetten nur einmal
***   gezaehlt. Vorsorglich, in den Daten bisher kein Fall.
*&
*& Laufmodi:
*&   Vollstaendig  - alle Kennzahlen werden neu berechnet
*&   Nur Verbrauch - nur VERBR/VERBR_DAYS/VERBR_DATE
*&   Automatisch   - Modus wird aus ZMD04_CONTROL abgeleitet
*&                   (nicht fuer den Nachtlauf gedacht)
*&
*& Hinweis: String-Templates sind Ausdruecke und in PERFORM ... USING
*&          nicht zulaessig. Texte werden deshalb ueber Hilfsfelder
*&          uebergeben.
*&---------------------------------------------------------------------*
REPORT z_lo03_md04_batch.

*---------------------------------------------------------------------*
* Selektionsbild
*---------------------------------------------------------------------*
TABLES: mara.

SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE TEXT-b01.
SELECT-OPTIONS: s_matnr FOR mara-matnr.
PARAMETERS:     p_werks TYPE werks_d DEFAULT '1100' OBLIGATORY.
SELECTION-SCREEN END OF BLOCK b1.

SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE TEXT-b02.
PARAMETERS: p_full  RADIOBUTTON GROUP mode DEFAULT 'X',
            p_verbr RADIOBUTTON GROUP mode,
            p_auto  RADIOBUTTON GROUP mode.
SELECTION-SCREEN END OF BLOCK b2.

SELECTION-SCREEN BEGIN OF BLOCK b3 WITH FRAME TITLE TEXT-b03.
PARAMETERS: p_days  TYPE i         DEFAULT 360,
            p_ktgue TYPE abap_bool AS CHECKBOX DEFAULT abap_true,
*** FIX 31: abgelaufene Kontrakte mit Restmenge mitzaehlen (wie ME3M)
            p_ktabg TYPE abap_bool AS CHECKBOX DEFAULT abap_true,
            p_all   TYPE abap_bool AS CHECKBOX DEFAULT abap_false,
            p_max   TYPE i         DEFAULT 20000,
            p_pack  TYPE i         DEFAULT 5000,
            p_test  TYPE abap_bool AS CHECKBOX DEFAULT abap_false.
SELECTION-SCREEN END OF BLOCK b3.

*---------------------------------------------------------------------*
* Typen
*---------------------------------------------------------------------*
TYPES: BEGIN OF ty_matnr,
         matnr TYPE matnr,
       END OF ty_matnr.

TYPES: BEGIN OF ty_md04_sum,
         matnr      TYPE matnr,
         werks      TYPE werks_d,
         labst      TYPE labst,
         feste_zug  TYPE labst,
         gepl_zug   TYPE labst,
         feste_abg  TYPE labst,
         gepl_abg   TYPE labst,
         verbr      TYPE menge_d,
         lbkum      TYPE lbkum,
         salk3      TYPE salk3,
         omeng      TYPE menge_d,
         owert      TYPE salk3,
         mkmng      TYPE menge_d,
         omkwr      TYPE salk3,
         exklusiv   TYPE char1,
         exkl_count TYPE i,
         calc_date  TYPE dats,
         calc_time  TYPE tims,
         verbr_days TYPE i,
         verbr_date TYPE dats,
       END OF ty_md04_sum.

TYPES: BEGIN OF ty_mbew,
         matnr TYPE matnr,
         lbkum TYPE lbkum,
         salk3 TYPE salk3,
         verpr TYPE verpr,
         stprs TYPE stprs,
         peinh TYPE peinh,
         vprsv TYPE vprsv,
       END OF ty_mbew.

TYPES: BEGIN OF ty_mdbs,
         matnr TYPE matnr,
         ebeln TYPE ebeln,
         ebelp TYPE ebelp,
         menge TYPE menge_d,
         wemng TYPE menge_d,
         umrez TYPE umrez,
         umren TYPE umren,
*** FIX 32: Einteilung gehoert zur Zeile
         etenr TYPE mdbs-etenr,
       END OF ty_mdbs.

TYPES: BEGIN OF ty_afpo,
         matnr TYPE matnr,
         aufnr TYPE aufnr,
         psmng TYPE menge_d,
         wemng TYPE menge_d,
*** FIX 34: Auftragsposition gehoert zur Zeile
         posnr TYPE afpo-posnr,
       END OF ty_afpo.

TYPES: BEGIN OF ty_ekpo_mk,
         matnr TYPE matnr,
         ebeln TYPE ebeln,
         ebelp TYPE ebelp,
         ktmng TYPE ktmng,
         umrez TYPE umrez,
         umren TYPE umren,
       END OF ty_ekpo_mk.

*** FIX 30: Abrufbeleg (EBELN/EBELP) gehoert zur Zeile, sonst fallen
***         gleiche Abrufmengen bei FOR ALL ENTRIES zusammen.
TYPES: BEGIN OF ty_ekab,
         konnr TYPE ebeln,
         ktpnr TYPE ebelp,
         menge TYPE menge_d,
         ebeln TYPE ebeln,
         ebelp TYPE ebelp,
       END OF ty_ekab.

TYPES: BEGIN OF ty_usage,
         kompnr TYPE matnr,
         matnr  TYPE matnr,
         mstae  TYPE mstae,
       END OF ty_usage.

TYPES: BEGIN OF ty_mstae,
         matnr TYPE matnr,
         mstae TYPE mstae,
       END OF ty_mstae.

TYPES: BEGIN OF ty_mseg_kde,
         mblnr TYPE mblnr,
         mjahr TYPE mjahr,
         matnr TYPE matnr,
         werks TYPE werks_d,
         bwart TYPE bwart,
         shkzg TYPE shkzg,
         menge TYPE menge_d,
*** FIX 33: Belegzeile gehoert zur Zeile
         zeile TYPE mseg-zeile,
       END OF ty_mseg_kde.

TYPES: BEGIN OF ty_delkz_log,
         delkz TYPE delkz,
         count TYPE i,
       END OF ty_delkz_log.

TYPES ty_delkz_tab TYPE HASHED TABLE OF delkz WITH UNIQUE KEY table_line.

*---------------------------------------------------------------------*
* Daten
*---------------------------------------------------------------------*
DATA: gt_matnr  TYPE STANDARD TABLE OF ty_matnr,
      gt_result TYPE STANDARD TABLE OF ty_md04_sum,
      gs_result TYPE ty_md04_sum.

DATA: gt_mdezx TYPE STANDARD TABLE OF mdez.

DATA: gt_mbew_cache  TYPE SORTED TABLE OF ty_mbew     WITH NON-UNIQUE KEY matnr,
      gt_mdbs_cache  TYPE SORTED TABLE OF ty_mdbs     WITH NON-UNIQUE KEY matnr,
      gt_afpo_cache  TYPE SORTED TABLE OF ty_afpo     WITH NON-UNIQUE KEY matnr,
      gt_ekpo_cache  TYPE SORTED TABLE OF ty_ekpo_mk  WITH NON-UNIQUE KEY matnr,
      gt_ekab_cache  TYPE SORTED TABLE OF ty_ekab     WITH NON-UNIQUE KEY konnr ktpnr,
      gt_usage_cache TYPE SORTED TABLE OF ty_usage    WITH NON-UNIQUE KEY kompnr,
      gt_mseg_cache  TYPE SORTED TABLE OF ty_mseg_kde WITH NON-UNIQUE KEY matnr,
      gt_mstae_cache TYPE HASHED TABLE OF ty_mstae    WITH UNIQUE KEY matnr.

DATA: gt_sel_vknr TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

DATA: gt_existing TYPE HASHED TABLE OF zmd04_calc WITH UNIQUE KEY matnr werks.

DATA: gt_zug_fest  TYPE ty_delkz_tab,
      gt_zug_gepl  TYPE ty_delkz_tab,
      gt_abg_fest  TYPE ty_delkz_tab,
      gt_abg_gepl  TYPE ty_delkz_tab,
      gt_delkz_unk TYPE SORTED TABLE OF ty_delkz_log WITH UNIQUE KEY delkz.

DATA: gv_total     TYPE i,
      gv_idx       TYPE i,
      gv_skipped   TYPE i,
      gv_written   TYPE i,
      gv_datum_von TYPE datum,
      gv_datum_bis TYPE datum,
      gv_text      TYPE string,
      gv_msg       TYPE string.

DATA: gv_update_mode TYPE char1.

*** FIX 31: fruehestes Laufzeitende, das noch als gueltig zaehlt
DATA: gv_kdate_ab TYPE datum.

CONSTANTS: c_mode_full  TYPE char1 VALUE 'F',
           c_mode_verbr TYPE char1 VALUE 'V'.

*---------------------------------------------------------------------*
* AT SELECTION-SCREEN
*   Fruehe Rueckmeldung im Dialog. Im Hintergrund wird dieses Ereignis
*   NICHT durchlaufen - die Pruefung laeuft deshalb zusaetzlich in
*   START-OF-SELECTION.
*---------------------------------------------------------------------*
AT SELECTION-SCREEN.

  PERFORM check_input.

*---------------------------------------------------------------------*
* START-OF-SELECTION
*---------------------------------------------------------------------*
START-OF-SELECTION.

  PERFORM check_input.
  PERFORM init_delkz.
  PERFORM set_update_mode.

  gv_datum_bis = sy-datum.
  gv_datum_von = sy-datum - p_days.
  gv_datum_von+6(2) = '01'.

  PERFORM get_materials.
  IF gt_matnr IS INITIAL.
    MESSAGE 'Keine Materialien gefunden.' TYPE 'S' DISPLAY LIKE 'I'.
    RETURN.
  ENDIF.

  IF gv_update_mode = c_mode_verbr.
    PERFORM load_existing_data.
    IF gt_existing IS INITIAL.
      MESSAGE 'Keine bestehenden Saetze gefunden - bitte zuerst einen vollstaendigen Lauf durchfuehren.'
              TYPE 'E'.
    ENDIF.
  ENDIF.

  CONCATENATE 'Modus' gv_update_mode ': Caches laden...' INTO gv_text
              SEPARATED BY space.
  PERFORM progress_text USING 10 gv_text.

  PERFORM load_caches_by_mode.

  IF p_test = abap_true.
    PERFORM debug_cache_sizes.
  ENDIF.

  gv_total = lines( gt_matnr ).

  LOOP AT gt_matnr INTO DATA(ls_matnr).
    gv_idx = sy-tabix.
    PERFORM progress USING gv_idx gv_total ls_matnr-matnr.

    CLEAR gs_result.

    IF gv_update_mode = c_mode_verbr.
      PERFORM calc_verbr_only USING ls_matnr-matnr p_werks CHANGING gs_result.
      IF gs_result-matnr IS INITIAL.
        gv_skipped = gv_skipped + 1.
        CONTINUE.
      ENDIF.
    ELSE.
      PERFORM calc_full USING ls_matnr-matnr p_werks CHANGING gs_result.
    ENDIF.

    APPEND gs_result TO gt_result.

    " Paketweise wegschreiben - haelt die LUW und den Speicher klein.
    " Im Testlauf wird gesammelt, damit die Liste vollstaendig bleibt.
    IF p_test = abap_false AND p_pack > 0 AND lines( gt_result ) >= p_pack.
      PERFORM flush_results.
    ENDIF.
  ENDLOOP.

  PERFORM update_database.
  PERFORM update_control_table.

  IF p_test = abap_true.
    PERFORM show_unknown_delkz.
  ENDIF.

  IF gv_skipped > 0.
    gv_msg = |{ gv_skipped } Materialien ohne bestehenden Satz uebersprungen - vollstaendiger Lauf noetig.|.
    MESSAGE gv_msg TYPE 'S' DISPLAY LIKE 'W'.
  ENDIF.

*---------------------------------------------------------------------*
* FORM check_input
*---------------------------------------------------------------------*
FORM check_input.

  DATA: lv_msg TYPE string.

  IF s_matnr[] IS INITIAL AND p_all = abap_false.
    MESSAGE 'Ohne Materialeinschraenkung bitte Kennzeichen "Vollselektion" setzen.'
            TYPE 'E'.
  ENDIF.

  IF p_days <= 0.
    MESSAGE 'Der Verbrauchszeitraum muss groesser als 0 Tage sein.' TYPE 'E'.
  ENDIF.

  IF p_max < 0 OR p_pack < 0.
    MESSAGE 'Mengengrenze und Paketgroesse duerfen nicht negativ sein.' TYPE 'E'.
  ENDIF.

  AUTHORITY-CHECK OBJECT 'M_MATE_WRK'
                  ID 'ACTVT' FIELD '03'
                  ID 'WERKS' FIELD p_werks.
  IF sy-subrc <> 0.
    CONCATENATE 'Keine Berechtigung fuer Werk' p_werks INTO lv_msg
                SEPARATED BY space.
    MESSAGE lv_msg TYPE 'E'.
  ENDIF.

ENDFORM.

*---------------------------------------------------------------------*
* FORM set_update_mode
*---------------------------------------------------------------------*
FORM set_update_mode.

  IF p_full = abap_true.
    gv_update_mode = c_mode_full.
  ELSEIF p_verbr = abap_true.
    gv_update_mode = c_mode_verbr.
  ELSE.
    PERFORM determine_update_mode.
  ENDIF.

ENDFORM.

*---------------------------------------------------------------------*
* FORM determine_update_mode
*---------------------------------------------------------------------*
FORM determine_update_mode.

  DATA: lt_control       TYPE STANDARD TABLE OF zmd04_control,
        ls_ctrl          TYPE zmd04_control,
        lv_val           TYPE char50,
        lv_last_days     TYPE i,
        lv_last_werks    TYPE werks_d,
        lv_last_full_run TYPE dats.

  SELECT * FROM zmd04_control INTO TABLE @lt_control.

  LOOP AT lt_control INTO ls_ctrl.
    lv_val = ls_ctrl-param_value.
    CONDENSE lv_val.

    CASE ls_ctrl-param_name.
      WHEN 'LAST_DAYS'.
        lv_last_days = lv_val.
      WHEN 'LAST_FULL_RUN'.
        lv_last_full_run = lv_val.
      WHEN 'LAST_WERKS'.
        lv_last_werks = lv_val.
    ENDCASE.
  ENDLOOP.

  IF lv_last_full_run IS INITIAL
     OR lv_last_werks <> p_werks
     OR lv_last_full_run <> sy-datum.
    gv_update_mode = c_mode_full.
    RETURN.
  ENDIF.

  IF lv_last_days <> p_days.
    gv_update_mode = c_mode_verbr.
  ELSE.
    gv_update_mode = c_mode_full.
  ENDIF.

ENDFORM.

*---------------------------------------------------------------------*
* FORM init_delkz
*   Zuordnung der MD04-Elementkennzeichen. Alles, was hier nicht
*   steht, landet im Auffangzweig und wird protokolliert.
*---------------------------------------------------------------------*
FORM init_delkz.

  gt_zug_fest = VALUE #( ( 'BE' ) ( 'LA' ) ( 'FE' ) ( 'QM' ) ( 'U2' ) ( 'U4' ) ).
  gt_zug_gepl = VALUE #( ( 'BA' ) ( 'PA' ) ( 'PL' ) ( 'AN' ) ( 'U3' ) ).
  gt_abg_fest = VALUE #( ( 'AR' ) ( 'KB' ) ( 'U1' ) ( 'MR' ) ( 'VJ' ) ( 'VC' ) ).
  gt_abg_gepl = VALUE #( ( 'SB' ) ( 'PP' ) ( 'PB' ) ( 'PE' ) ( 'BB' ) ).

ENDFORM.

*---------------------------------------------------------------------*
* FORM get_materials
*---------------------------------------------------------------------*
FORM get_materials.

  DATA: lt_matnr_sel TYPE STANDARD TABLE OF matnr,
        lv_matnr     TYPE matnr,
        lv_count     TYPE i,
        lv_msg       TYPE string.

  CLEAR: gt_matnr, gt_sel_vknr.

  SELECT matnr
    FROM mara
    INTO TABLE @lt_matnr_sel
    WHERE matnr IN @s_matnr
      AND lvorm = @space.

  IF lt_matnr_sel IS INITIAL.
    RETURN.
  ENDIF.

  lv_count = lines( lt_matnr_sel ).
  IF s_matnr[] IS INITIAL AND p_max > 0 AND lv_count > p_max.
    lv_msg = |Vollselektion mit { lv_count } Materialien abgebrochen (Grenze { p_max }).|.
    MESSAGE lv_msg TYPE 'E'.
  ENDIF.

  LOOP AT lt_matnr_sel INTO lv_matnr.
    INSERT lv_matnr INTO TABLE gt_sel_vknr.
  ENDLOOP.

  SELECT kompnr AS matnr
    FROM zpowerbi_vc_txt
    INTO TABLE @gt_matnr
    FOR ALL ENTRIES IN @lt_matnr_sel
    WHERE matnr         = @lt_matnr_sel-table_line
      AND menge        <> 0
      AND mengeneinheit <> @space.

  LOOP AT lt_matnr_sel INTO lv_matnr.
    APPEND VALUE #( matnr = lv_matnr ) TO gt_matnr.
  ENDLOOP.

  SORT gt_matnr BY matnr.
  DELETE ADJACENT DUPLICATES FROM gt_matnr COMPARING matnr.

ENDFORM.

*---------------------------------------------------------------------*
* FORM load_existing_data
*---------------------------------------------------------------------*
FORM load_existing_data.

  CHECK gt_matnr IS NOT INITIAL.

  SELECT *
    FROM zmd04_calc
    INTO TABLE @gt_existing
    FOR ALL ENTRIES IN @gt_matnr
    WHERE matnr = @gt_matnr-matnr
      AND werks = @p_werks.

ENDFORM.

*---------------------------------------------------------------------*
* FORM load_caches_by_mode
*   Im Verbrauchsmodus wird nur MSEG benoetigt.
*---------------------------------------------------------------------*
FORM load_caches_by_mode.

  CHECK gt_matnr IS NOT INITIAL.

  CLEAR: gt_mbew_cache, gt_mdbs_cache, gt_afpo_cache, gt_ekpo_cache,
         gt_ekab_cache, gt_usage_cache, gt_mseg_cache, gt_mstae_cache.

  "-------------------------------------------------------------------
  " MSEG/MKPF - Verbrauch Kundeneinzelbestand, in beiden Modi noetig
  " Buchungsdatum wird direkt im JOIN eingeschraenkt
  "-------------------------------------------------------------------
  PERFORM progress_text USING 15 'Cache: MSEG laden...'.

*** FIX 33: ZEILE mitlesen, damit jede Belegzeile eine eigene Zeile ist
  SELECT m~mblnr, m~mjahr, m~matnr, m~werks, m~bwart, m~shkzg, m~menge,
         m~zeile
    FROM mseg AS m
    INNER JOIN mkpf AS k ON  k~mblnr = m~mblnr
                         AND k~mjahr = m~mjahr
    INTO TABLE @gt_mseg_cache
    FOR ALL ENTRIES IN @gt_matnr
    WHERE m~matnr = @gt_matnr-matnr
      AND m~werks = @p_werks
      AND m~bwart IN ( '601', '602', '651', '654' )
      AND m~sobkz = 'E'
      AND k~budat >= @gv_datum_von
      AND k~budat <= @gv_datum_bis.

  CHECK gv_update_mode = c_mode_full.

  "-------------------------------------------------------------------
  " MBEW - bwtar = space liefert bei Split-Bewertung den Summensatz
  "-------------------------------------------------------------------
  PERFORM progress_text USING 25 'Cache: MBEW laden...'.

  SELECT matnr, lbkum, salk3, verpr, stprs, peinh, vprsv
    FROM mbew
    INTO TABLE @gt_mbew_cache
    FOR ALL ENTRIES IN @gt_matnr
    WHERE matnr = @gt_matnr-matnr
      AND bwkey = @p_werks
      AND bwtar = @space.

  "-------------------------------------------------------------------
  " MDBS - offene Bestellpositionen
  "-------------------------------------------------------------------
  PERFORM progress_text USING 35 'Cache: MDBS laden...'.

*** FIX 32: ETENR mitlesen, damit jede Einteilung eine eigene Zeile ist
  SELECT matnr, ebeln, ebelp, menge, wemng, umrez, umren, etenr
    FROM mdbs
    INTO TABLE @gt_mdbs_cache
    FOR ALL ENTRIES IN @gt_matnr
    WHERE matnr = @gt_matnr-matnr
      AND werks = @p_werks
      AND bstyp = 'F'
      AND ( loekz = @space OR loekz = 'S' )
      AND elikz = @space
      AND stapo = @space.

  "-------------------------------------------------------------------
  " AFPO - offene Fertigungsauftragspositionen
  "-------------------------------------------------------------------
  PERFORM progress_text USING 45 'Cache: AFPO laden...'.

*** FIX 34: POSNR mitlesen, damit jede Position eine eigene Zeile ist
  SELECT matnr, aufnr, psmng, wemng, posnr
    FROM afpo
    INTO TABLE @gt_afpo_cache
    FOR ALL ENTRIES IN @gt_matnr
    WHERE matnr = @gt_matnr-matnr
      AND pwerk = @p_werks
      AND dnrel = @space
      AND elikz = @space.

  "-------------------------------------------------------------------
  " EKPO/EKKO - Mengenkontrakte des Werks
  "-------------------------------------------------------------------
  PERFORM progress_text USING 55 'Cache: Kontrakte laden...'.

*** FIX 31: mit P_KTABG zaehlt jedes Laufzeitende (Vergleich gegen
***         00000000 ist immer erfuellt), sonst nur ab heute.
  gv_kdate_ab = sy-datum.
  IF p_ktabg = abap_true.
    CLEAR gv_kdate_ab.
  ENDIF.

  IF p_ktgue = abap_true.
    SELECT p~matnr, p~ebeln, p~ebelp, p~ktmng, p~umrez, p~umren
      FROM ekpo AS p
      INNER JOIN ekko AS k ON k~ebeln = p~ebeln
      INTO TABLE @gt_ekpo_cache
      FOR ALL ENTRIES IN @gt_matnr
      WHERE p~matnr  = @gt_matnr-matnr
        AND p~werks  = @p_werks
        AND p~bstyp  = 'K'
        AND ( p~loekz = @space OR p~loekz = 'S' )
        AND p~elikz  = @space
        AND p~stapo  = @space
        AND k~loekz  = @space
        AND ( k~kdatb = '00000000' OR k~kdatb <= @sy-datum )
        AND ( k~kdate = '00000000' OR k~kdate >= @gv_kdate_ab ).
  ELSE.
    SELECT matnr, ebeln, ebelp, ktmng, umrez, umren
      FROM ekpo
      INTO TABLE @gt_ekpo_cache
      FOR ALL ENTRIES IN @gt_matnr
      WHERE matnr = @gt_matnr-matnr
        AND werks = @p_werks
        AND bstyp = 'K'
        AND ( loekz = @space OR loekz = 'S' )
        AND elikz = @space
        AND stapo = @space.
  ENDIF.

  "-------------------------------------------------------------------
  " EKAB - Abrufe zu den gefundenen Kontraktpositionen
  "-------------------------------------------------------------------
  PERFORM progress_text USING 60 'Cache: EKAB laden...'.

  IF gt_ekpo_cache IS NOT INITIAL.
*** FIX 30: EBELN/EBELP mitlesen, damit jeder Abruf eine eigene Zeile ist
    SELECT konnr, ktpnr, menge, ebeln, ebelp
      FROM ekab
      INTO TABLE @gt_ekab_cache
      FOR ALL ENTRIES IN @gt_ekpo_cache
      WHERE konnr = @gt_ekpo_cache-ebeln
        AND ktpnr = @gt_ekpo_cache-ebelp.
  ENDIF.

  "-------------------------------------------------------------------
  " Verwendungsnachweis und Materialstatus
  "-------------------------------------------------------------------
  PERFORM progress_text USING 70 'Cache: Verwendung laden...'.
  PERFORM load_usage_cache.

  PERFORM progress_text USING 85 'Cache: Materialstatus laden...'.
  PERFORM load_mstae_cache.

ENDFORM.

*---------------------------------------------------------------------*
* FORM load_usage_cache
*---------------------------------------------------------------------*
FORM load_usage_cache.

  DATA: lt_chunk      TYPE STANDARD TABLE OF ty_matnr,
        ls_chunk      TYPE ty_matnr,
        lv_chunk_size TYPE i VALUE 1000,
        lv_from       TYPE i VALUE 1,
        lv_to         TYPE i,
        lv_total      TYPE i,
        lv_pct        TYPE i,
        lv_text       TYPE string.

  lv_total = lines( gt_matnr ).

  WHILE lv_from <= lv_total.
    lv_to = lv_from + lv_chunk_size - 1.
    IF lv_to > lv_total.
      lv_to = lv_total.
    ENDIF.

    CLEAR lt_chunk.
    LOOP AT gt_matnr INTO ls_chunk FROM lv_from TO lv_to.
      APPEND ls_chunk TO lt_chunk.
    ENDLOOP.

    lv_pct  = 70 + ( lv_to * 10 / lv_total ).
    lv_text = |Cache: Verwendung { lv_to }/{ lv_total }...|.
    PERFORM progress_text USING lv_pct lv_text.

    SELECT kompnr, matnr
      FROM zpowerbi_vc_txt
      APPENDING CORRESPONDING FIELDS OF TABLE @gt_usage_cache
      FOR ALL ENTRIES IN @lt_chunk
      WHERE kompnr = @lt_chunk-matnr.

    lv_from = lv_to + 1.
  ENDWHILE.

ENDFORM.

*---------------------------------------------------------------------*
* FORM load_mstae_cache
*   Materialstatus der verwendenden Materialien nachladen und in den
*   Verwendungs-Cache uebernehmen.
*---------------------------------------------------------------------*
FORM load_mstae_cache.

  DATA: lt_use_matnr TYPE STANDARD TABLE OF ty_matnr,
        ls_usage     TYPE ty_usage,
        ls_mstae     TYPE ty_mstae.

  FIELD-SYMBOLS: <ls_usage> TYPE ty_usage.

  CHECK gt_usage_cache IS NOT INITIAL.

  LOOP AT gt_usage_cache INTO ls_usage.
    APPEND VALUE #( matnr = ls_usage-matnr ) TO lt_use_matnr.
  ENDLOOP.

  SORT lt_use_matnr BY matnr.
  DELETE ADJACENT DUPLICATES FROM lt_use_matnr COMPARING matnr.
  CHECK lt_use_matnr IS NOT INITIAL.

  SELECT matnr, mstae
    FROM mara
    INTO TABLE @gt_mstae_cache
    FOR ALL ENTRIES IN @lt_use_matnr
    WHERE matnr = @lt_use_matnr-matnr.

  LOOP AT gt_usage_cache ASSIGNING <ls_usage>.
    READ TABLE gt_mstae_cache INTO ls_mstae
      WITH TABLE KEY matnr = <ls_usage>-matnr.
    IF sy-subrc = 0.
      <ls_usage>-mstae = ls_mstae-mstae.
    ENDIF.
  ENDLOOP.

ENDFORM.

*---------------------------------------------------------------------*
* FORM calc_full - vollstaendige Berechnung
*---------------------------------------------------------------------*
FORM calc_full
  USING    iv_matnr TYPE matnr
           iv_werks TYPE werks_d
  CHANGING cs_sum   TYPE ty_md04_sum.

  cs_sum-matnr     = iv_matnr.
  cs_sum-werks     = iv_werks.
  cs_sum-calc_date = sy-datum.
  cs_sum-calc_time = sy-uzeit.

  PERFORM get_mbew_data          USING iv_matnr          CHANGING cs_sum.
  PERFORM calc_md04_for_material USING iv_matnr iv_werks CHANGING cs_sum.
  PERFORM calc_verbrauch         USING iv_matnr          CHANGING cs_sum.
  PERFORM calc_omeng             USING iv_matnr          CHANGING cs_sum.
  PERFORM calc_mkmng             USING iv_matnr          CHANGING cs_sum.
  PERFORM calc_exclusivity       USING iv_matnr          CHANGING cs_sum.

  cs_sum-verbr_days = p_days.
  cs_sum-verbr_date = sy-datum.

ENDFORM.

*---------------------------------------------------------------------*
* FORM calc_verbr_only
*   Uebernimmt den bestehenden Satz und rechnet nur den Verbrauch neu.
*   Ohne bestehenden Satz bleibt CS_SUM leer - der Aufrufer
*   ueberspringt das Material dann, weil die uebrigen Caches im
*   Verbrauchsmodus nicht geladen sind.
*---------------------------------------------------------------------*
FORM calc_verbr_only
  USING    iv_matnr TYPE matnr
           iv_werks TYPE werks_d
  CHANGING cs_sum   TYPE ty_md04_sum.

  DATA: ls_existing TYPE zmd04_calc.

  READ TABLE gt_existing INTO ls_existing
    WITH TABLE KEY matnr = iv_matnr werks = iv_werks.
  IF sy-subrc <> 0.
    CLEAR cs_sum.
    RETURN.
  ENDIF.

  MOVE-CORRESPONDING ls_existing TO cs_sum.

  " CALC_DATE/CALC_TIME bleiben unveraendert: sie dokumentieren den
  " letzten vollstaendigen Lauf
  CLEAR cs_sum-verbr.
  PERFORM calc_verbrauch USING iv_matnr CHANGING cs_sum.

  cs_sum-verbr_days = p_days.
  cs_sum-verbr_date = sy-datum.

ENDFORM.

*---------------------------------------------------------------------*
* FORM get_mbew_data
*---------------------------------------------------------------------*
FORM get_mbew_data
  USING    iv_matnr TYPE matnr
  CHANGING cs_sum   TYPE ty_md04_sum.

  DATA: ls_mbew TYPE ty_mbew.

  READ TABLE gt_mbew_cache INTO ls_mbew
    WITH KEY matnr = iv_matnr BINARY SEARCH.

  IF sy-subrc = 0.
    cs_sum-lbkum = ls_mbew-lbkum.
    cs_sum-salk3 = ls_mbew-salk3.
  ENDIF.

ENDFORM.

*---------------------------------------------------------------------*
* FORM get_preis
*---------------------------------------------------------------------*
FORM get_preis
  USING    iv_matnr TYPE matnr
  CHANGING cv_preis TYPE p
           cv_peinh TYPE peinh.

  DATA: ls_mbew TYPE ty_mbew.

  CLEAR cv_preis.
  cv_peinh = 1.

  READ TABLE gt_mbew_cache INTO ls_mbew
    WITH KEY matnr = iv_matnr BINARY SEARCH.
  CHECK sy-subrc = 0.

  IF ls_mbew-vprsv = 'V'.
    cv_preis = ls_mbew-verpr.
  ELSE.
    cv_preis = ls_mbew-stprs.
  ENDIF.

  IF ls_mbew-peinh > 0.
    cv_peinh = ls_mbew-peinh.
  ENDIF.

ENDFORM.

*---------------------------------------------------------------------*
* FORM calc_md04_for_material
*   Hinweis fuer den Massenlauf: der Baustein puffert intern. Falls
*   eure Schnittstelle den Parameter NOBUFFER kennt, hier setzen -
*   das haelt den Speicherbedarf ueber zehntausende Aufrufe stabil.
*---------------------------------------------------------------------*
FORM calc_md04_for_material
  USING    iv_matnr TYPE matnr
           iv_werks TYPE werks_d
  CHANGING cs_sum   TYPE ty_md04_sum.

  DATA: ls_mdez  TYPE mdez,
        lv_menge TYPE mdez-mng01,
        lv_abs   TYPE mdez-mng01.

  REFRESH gt_mdezx.

  CALL FUNCTION 'MD_STOCK_REQUIREMENTS_LIST_API'
    EXPORTING
      matnr                    = iv_matnr
      werks                    = iv_werks
    TABLES
      mdezx                    = gt_mdezx
    EXCEPTIONS
      material_plant_not_found = 1
      plant_not_found          = 2
      OTHERS                   = 3.

  IF sy-subrc <> 0.
    RETURN.
  ENDIF.

  LOOP AT gt_mdezx INTO ls_mdez.
    lv_menge = ls_mdez-mng01.

    " Werksbestand
    IF ls_mdez-delkz = 'WB'.
      cs_sum-labst = cs_sum-labst + lv_menge.
      CONTINUE.
    ENDIF.

    " Sicherheitsbestand ist kein Zu-/Abgang
    IF ls_mdez-delkz = 'SH'.
      CONTINUE.
    ENDIF.

    IF lv_menge > 0.
      IF line_exists( gt_zug_fest[ table_line = ls_mdez-delkz ] ).
        cs_sum-feste_zug = cs_sum-feste_zug + lv_menge.
      ELSEIF line_exists( gt_zug_gepl[ table_line = ls_mdez-delkz ] ).
        cs_sum-gepl_zug = cs_sum-gepl_zug + lv_menge.
      ELSE.
        " Auffangzweig: unbekanntes Element als fest werten
        cs_sum-feste_zug = cs_sum-feste_zug + lv_menge.
        PERFORM log_unknown_delkz USING ls_mdez-delkz.
      ENDIF.

    ELSEIF lv_menge < 0.
      lv_abs = abs( lv_menge ).
      IF line_exists( gt_abg_fest[ table_line = ls_mdez-delkz ] ).
        cs_sum-feste_abg = cs_sum-feste_abg + lv_abs.
      ELSEIF line_exists( gt_abg_gepl[ table_line = ls_mdez-delkz ] ).
        cs_sum-gepl_abg = cs_sum-gepl_abg + lv_abs.
      ELSE.
        cs_sum-feste_abg = cs_sum-feste_abg + lv_abs.
        PERFORM log_unknown_delkz USING ls_mdez-delkz.
      ENDIF.
    ENDIF.
  ENDLOOP.

ENDFORM.

*---------------------------------------------------------------------*
* FORM log_unknown_delkz
*---------------------------------------------------------------------*
FORM log_unknown_delkz USING iv_delkz TYPE delkz.

  DATA: ls_unk_new TYPE ty_delkz_log.

  FIELD-SYMBOLS: <ls_unk> TYPE ty_delkz_log.

  READ TABLE gt_delkz_unk ASSIGNING <ls_unk>
    WITH TABLE KEY delkz = iv_delkz.
  IF sy-subrc = 0.
    <ls_unk>-count = <ls_unk>-count + 1.
  ELSE.
    ls_unk_new-delkz = iv_delkz.
    ls_unk_new-count = 1.
    INSERT ls_unk_new INTO TABLE gt_delkz_unk.
  ENDIF.

ENDFORM.

*---------------------------------------------------------------------*
* FORM calc_omeng - offene Bestell- und Fertigungsmengen
*---------------------------------------------------------------------*
FORM calc_omeng
  USING    iv_matnr TYPE matnr
  CHANGING cs_sum   TYPE ty_md04_sum.

  DATA: ls_mdbs  TYPE ty_mdbs,
        ls_afpo  TYPE ty_afpo,
        lv_preis TYPE p DECIMALS 2,
        lv_peinh TYPE peinh,
        lv_umrez TYPE umrez,
        lv_umren TYPE umren,
        lv_add   TYPE menge_d.

  PERFORM get_preis USING iv_matnr CHANGING lv_preis lv_peinh.

  " Bestellungen
  LOOP AT gt_mdbs_cache INTO ls_mdbs WHERE matnr = iv_matnr.
    lv_umrez = ls_mdbs-umrez.
    lv_umren = ls_mdbs-umren.
    IF lv_umrez IS INITIAL.
      lv_umrez = 1.
    ENDIF.
    IF lv_umren IS INITIAL.
      lv_umren = 1.
    ENDIF.

    lv_add = ( ls_mdbs-menge - ls_mdbs-wemng ) * lv_umrez / lv_umren.
    IF lv_add > 0.
      cs_sum-omeng = cs_sum-omeng + lv_add.
    ENDIF.
  ENDLOOP.

  " Fertigungsauftraege
  LOOP AT gt_afpo_cache INTO ls_afpo WHERE matnr = iv_matnr.
    lv_add = ls_afpo-psmng - ls_afpo-wemng.
    IF lv_add > 0.
      cs_sum-omeng = cs_sum-omeng + lv_add.
    ENDIF.
  ENDLOOP.

  cs_sum-owert = cs_sum-omeng * lv_preis / lv_peinh.

ENDFORM.

*---------------------------------------------------------------------*
* FORM calc_mkmng - offene Mengenkontrakte
*   Ueberabruf begrenzt die offene Menge je Position auf 0, wirkt aber
*   nicht negativ auf andere Kontraktpositionen desselben Materials.
*---------------------------------------------------------------------*
FORM calc_mkmng
  USING    iv_matnr TYPE matnr
  CHANGING cs_sum   TYPE ty_md04_sum.

  DATA: ls_ekpo     TYPE ty_ekpo_mk,
        ls_ekab     TYPE ty_ekab,
        lv_preis    TYPE p DECIMALS 2,
        lv_peinh    TYPE peinh,
        lv_umrez    TYPE umrez,
        lv_umren    TYPE umren,
        lv_ekab_sum TYPE menge_d,
        lv_offen    TYPE menge_d.

  PERFORM get_preis USING iv_matnr CHANGING lv_preis lv_peinh.

  LOOP AT gt_ekpo_cache INTO ls_ekpo WHERE matnr = iv_matnr.
    lv_umrez = ls_ekpo-umrez.
    lv_umren = ls_ekpo-umren.
    IF lv_umrez IS INITIAL.
      lv_umrez = 1.
    ENDIF.
    IF lv_umren IS INITIAL.
      lv_umren = 1.
    ENDIF.

    " Abrufe je Kontraktposition summieren
    CLEAR lv_ekab_sum.
    LOOP AT gt_ekab_cache INTO ls_ekab
      WHERE konnr = ls_ekpo-ebeln
        AND ktpnr = ls_ekpo-ebelp.
      lv_ekab_sum = lv_ekab_sum + ls_ekab-menge.
    ENDLOOP.

    lv_offen = ls_ekpo-ktmng - lv_ekab_sum.
    IF lv_offen < 0.
      CLEAR lv_offen.
    ENDIF.

    cs_sum-mkmng = cs_sum-mkmng + ( lv_offen * lv_umrez / lv_umren ).
  ENDLOOP.

  cs_sum-omkwr = cs_sum-mkmng * lv_preis / lv_peinh.

ENDFORM.

*---------------------------------------------------------------------*
* FORM calc_verbrauch
*---------------------------------------------------------------------*
FORM calc_verbrauch
  USING    iv_matnr TYPE matnr
  CHANGING cs_sum   TYPE ty_md04_sum.

  DATA: ls_mseg  TYPE ty_mseg_kde,
        lv_verbr TYPE menge_d.

  " 1. Verbrauch aus den fortgeschriebenen Verbrauchswerten
  CALL FUNCTION 'VERBRAUCH_SUMMIEREN'
    EXPORTING
      abdatum         = gv_datum_von
      bisdatum        = gv_datum_bis
      matnr           = iv_matnr
      periv           = ' '
      perkz           = 'M'
      werks           = p_werks
    IMPORTING
      gesamtverbrauch = lv_verbr
    EXCEPTIONS
      OTHERS          = 9.

  IF sy-subrc = 0.
    cs_sum-verbr = lv_verbr.
  ENDIF.

  " 2. Verbrauch aus Kundeneinzelbestand
  "    Der Cache enthaelt bereits nur Belege im Auswertungszeitraum
  LOOP AT gt_mseg_cache INTO ls_mseg WHERE matnr = iv_matnr.
    IF ls_mseg-shkzg = 'S'.
      cs_sum-verbr = cs_sum-verbr - ls_mseg-menge.
    ELSE.
      cs_sum-verbr = cs_sum-verbr + ls_mseg-menge.
    ENDIF.
  ENDLOOP.

ENDFORM.

*---------------------------------------------------------------------*
* FORM calc_exclusivity
*   Exklusiv = die Komponente wird ausserhalb der Selektion nur von
*   Materialien mit Status 99 verwendet.
*---------------------------------------------------------------------*
FORM calc_exclusivity
  USING    iv_matnr TYPE matnr
  CHANGING cs_sum   TYPE ty_md04_sum.

  DATA: ls_usage   TYPE ty_usage,
        lv_count   TYPE i,
        lv_is_excl TYPE char1 VALUE 'X'.

  LOOP AT gt_usage_cache INTO ls_usage WHERE kompnr = iv_matnr.
    lv_count = lv_count + 1.

    " Verwendendes Material ist selbst selektiert
    READ TABLE gt_sel_vknr WITH TABLE KEY table_line = ls_usage-matnr
      TRANSPORTING NO FIELDS.
    IF sy-subrc = 0.
      CONTINUE.
    ENDIF.

    " Auslaufmaterial
    IF ls_usage-mstae = '99'.
      CONTINUE.
    ENDIF.

    lv_is_excl = ''.
    EXIT.
  ENDLOOP.

  IF lv_count = 0.
    lv_is_excl = ''.
  ENDIF.

  cs_sum-exklusiv   = lv_is_excl.
  cs_sum-exkl_count = lv_count.

ENDFORM.

*---------------------------------------------------------------------*
* FORM flush_results
*   Schreibt das aktuelle Paket und leert GT_RESULT. Jedes Paket ist
*   eine eigene LUW - bei einem Abbruch bleibt das bereits
*   Geschriebene erhalten.
*---------------------------------------------------------------------*
FORM flush_results.

  DATA: ls_res TYPE ty_md04_sum,
        ls_db  TYPE zmd04_calc,
        lt_db  TYPE STANDARD TABLE OF zmd04_calc.

  CHECK gt_result IS NOT INITIAL.

  LOOP AT gt_result INTO ls_res.
    CLEAR ls_db.
    MOVE-CORRESPONDING ls_res TO ls_db.
    APPEND ls_db TO lt_db.
  ENDLOOP.

  SORT lt_db BY matnr werks.
  DELETE ADJACENT DUPLICATES FROM lt_db COMPARING matnr werks.

  MODIFY zmd04_calc FROM TABLE lt_db.
  gv_written = gv_written + sy-dbcnt.
  COMMIT WORK AND WAIT.

  CLEAR gt_result.

ENDFORM.

*---------------------------------------------------------------------*
* FORM update_database - schreibt das letzte Paket
*---------------------------------------------------------------------*
FORM update_database.

  DATA: lv_msg TYPE string.

  IF p_test = abap_true.
    IF gt_result IS INITIAL.
      MESSAGE 'Keine Ergebnisse.' TYPE 'S' DISPLAY LIKE 'I'.
      RETURN.
    ENDIF.
    PERFORM show_testlist.
    MESSAGE 'Testlauf: keine Datenbankaktualisierung.' TYPE 'S' DISPLAY LIKE 'I'.
    RETURN.
  ENDIF.

  PERFORM flush_results.

  IF gv_written = 0.
    MESSAGE 'Keine Saetze geschrieben.' TYPE 'S' DISPLAY LIKE 'I'.
    RETURN.
  ENDIF.

  lv_msg = |Modus { gv_update_mode }: { gv_written } Saetze geschrieben.|.
  MESSAGE lv_msg TYPE 'S'.

ENDFORM.

*---------------------------------------------------------------------*
* FORM update_control_table
*---------------------------------------------------------------------*
FORM update_control_table.

  DATA: lt_control TYPE STANDARD TABLE OF zmd04_control,
        lv_value   TYPE char50.

  CHECK p_test = abap_false.
  CHECK gv_written > 0.

  lv_value = p_days.
  CONDENSE lv_value.
  PERFORM add_control USING 'LAST_DAYS' lv_value CHANGING lt_control.

  lv_value = p_werks.
  CONDENSE lv_value.
  PERFORM add_control USING 'LAST_WERKS' lv_value CHANGING lt_control.

  IF gv_update_mode = c_mode_full.
    lv_value = sy-datum.
    CONDENSE lv_value.
    PERFORM add_control USING 'LAST_FULL_RUN' lv_value CHANGING lt_control.
  ENDIF.

  MODIFY zmd04_control FROM TABLE lt_control.
  COMMIT WORK AND WAIT.

ENDFORM.

*---------------------------------------------------------------------*
* FORM add_control
*---------------------------------------------------------------------*
FORM add_control USING    iv_name    TYPE char30
                          iv_value   TYPE char50
                 CHANGING ct_control TYPE STANDARD TABLE.

  DATA: ls_control TYPE zmd04_control.

  ls_control-param_name  = iv_name.
  ls_control-param_value = iv_value.
  ls_control-changed_on  = sy-datum.
  ls_control-changed_at  = sy-uzeit.
  APPEND ls_control TO ct_control.

ENDFORM.

*---------------------------------------------------------------------*
* FORM show_testlist
*---------------------------------------------------------------------*
FORM show_testlist.

  DATA: ls_debug TYPE ty_md04_sum.

  FORMAT COLOR COL_HEADING.
  WRITE: /1 'MATNR', 22 'LABST', 35 'VERBR', 48 'DAYS',
         58 'OMENG', 73 'MKMNG', 88 'EXKL', 95 'MODUS'.
  FORMAT RESET.
  ULINE.

  LOOP AT gt_result INTO ls_debug.
    WRITE: /1  ls_debug-matnr,
           22  ls_debug-labst DECIMALS 0,
           35  ls_debug-verbr DECIMALS 0,
           48  ls_debug-verbr_days,
           58  ls_debug-omeng DECIMALS 0,
           73  ls_debug-mkmng DECIMALS 0,
           88  ls_debug-exklusiv,
           95  gv_update_mode.
  ENDLOOP.

  ULINE.

ENDFORM.

*---------------------------------------------------------------------*
* FORM show_unknown_delkz
*---------------------------------------------------------------------*
FORM show_unknown_delkz.

  DATA: ls_unk TYPE ty_delkz_log.

  CHECK gt_delkz_unk IS NOT INITIAL.

  SKIP.
  WRITE: / 'Nicht klassifizierte MD04-Elemente (als "fest" gewertet):'.
  LOOP AT gt_delkz_unk INTO ls_unk.
    WRITE: / '  DELKZ', ls_unk-delkz, 'Anzahl', ls_unk-count.
  ENDLOOP.
  ULINE.

ENDFORM.

*---------------------------------------------------------------------*
* FORM debug_cache_sizes
*---------------------------------------------------------------------*
FORM debug_cache_sizes.

  DATA: lv_cnt TYPE i.

  WRITE: / '=== Cache-Uebersicht (Modus', gv_update_mode, ') ==='.

  lv_cnt = lines( gt_matnr ).        WRITE: / 'Materialien   :', lv_cnt.
  lv_cnt = lines( gt_existing ).     WRITE: / 'Bestand DB    :', lv_cnt.
  lv_cnt = lines( gt_mbew_cache ).   WRITE: / 'MBEW          :', lv_cnt.
  lv_cnt = lines( gt_mdbs_cache ).   WRITE: / 'MDBS          :', lv_cnt.
  lv_cnt = lines( gt_afpo_cache ).   WRITE: / 'AFPO          :', lv_cnt.
  lv_cnt = lines( gt_ekpo_cache ).   WRITE: / 'EKPO (Kontr.) :', lv_cnt.
  lv_cnt = lines( gt_ekab_cache ).   WRITE: / 'EKAB (Abrufe) :', lv_cnt.
  lv_cnt = lines( gt_usage_cache ).  WRITE: / 'Verwendung    :', lv_cnt.
  lv_cnt = lines( gt_mseg_cache ).   WRITE: / 'MSEG (KDE)    :', lv_cnt.

  ULINE.

ENDFORM.

*---------------------------------------------------------------------*
* FORM progress_text
*   IV_TEXT ist TYPE any, damit sowohl Literale als auch Textfelder
*   uebergeben werden koennen.
*---------------------------------------------------------------------*
FORM progress_text USING iv_pct  TYPE i
                         iv_text TYPE any.

  DATA: lv_text TYPE string.

  lv_text = iv_text.

  IF sy-batch IS NOT INITIAL.
    " Im Hintergrund ins Job-Protokoll statt Fortschrittsbalken
    MESSAGE lv_text TYPE 'S'.
    RETURN.
  ENDIF.

  CALL FUNCTION 'SAPGUI_PROGRESS_INDICATOR'
    EXPORTING
      percentage = iv_pct
      text       = lv_text.

ENDFORM.

*---------------------------------------------------------------------*
* FORM progress
*   Dialog: Fortschrittsbalken alle 50 Materialien.
*   Hintergrund: Meldung alle 1000 Materialien ins Job-Protokoll.
*---------------------------------------------------------------------*
FORM progress USING iv_current TYPE i
                    iv_total   TYPE i
                    iv_matnr   TYPE matnr.

  DATA: lv_pct TYPE i,
        lv_txt TYPE string.

  CHECK iv_total > 0.

  IF sy-batch IS NOT INITIAL.
    CHECK iv_current MOD 1000 = 0 OR iv_current = iv_total.
    lv_txt = |Verarbeitet: { iv_current }/{ iv_total }|.
    MESSAGE lv_txt TYPE 'S'.
    RETURN.
  ENDIF.

  CHECK iv_current MOD 50 = 0 OR iv_current = 1 OR iv_current = iv_total.

  lv_pct = iv_current * 100 / iv_total.
  lv_txt = |MD04 { iv_current }/{ iv_total } ({ lv_pct }%) - { iv_matnr }|.

  CALL FUNCTION 'SAPGUI_PROGRESS_INDICATOR'
    EXPORTING
      percentage = lv_pct
      text       = lv_txt.

ENDFORM.
