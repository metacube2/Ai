REPORT z_koi_shop_stat_test LINE-SIZE 200 NO STANDARD PAGE HEADING.
* $TMP-Test 2026-11-13 (Claude), nur T76, schreibt nichts. Laufzeit fuer den Plan
* saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md (ShopAufSet Typ VR: Systemstatus und naechster Arbeitsplatz
* wie ZC22). Auswahl der Fertigungsauftraege genau wie der DPC-Zweig VR (AFKO/AFPO, DWERK, ELIKZ/DNREL leer,
* erste Position je Auftrag). Gemessen:
*   A  Status je Auftrag mit PERFORM status_lang IN PROGRAM zm_offene_fauf (genau die ZC22-Routine)
*   B  Status gesammelt (JEST/JSTO FOR ALL ENTRIES, TJ02/TJ02T/TJ04 einmal), Sprache D fest; Vergleich mit A
*   C  Z_MD04_NEXT_OPERATION je Auftrag (ZC22 FORM vorgangsdaten)
* Dazu: wie viele Auftraege der Filter des ZC22-Layouts / P00 TX ausblenden wuerde (Status FREI DRUC RUECK VOKL*,
* naechster Arbeitsplatz M*).

PARAMETERS: p_werks TYPE werks_d DEFAULT '1100',
            p_max   TYPE i DEFAULT 200,
            p_avo   AS CHECKBOX DEFAULT 'X'.

TYPES: BEGIN OF ty_fa,
         aufnr TYPE afko-aufnr,
         posnr TYPE afpo-posnr,
         dispo TYPE afko-dispo,
         objnr TYPE aufk-objnr,
         stta  TYPE text100,
         sttb  TYPE text100,
         vornr TYPE afvc-vornr,
         arbpl TYPE crhd-arbpl,
       END OF ty_fa,
       BEGIN OF ty_obj,
         aufnr TYPE aufk-aufnr,
         objnr TYPE aufk-objnr,
       END OF ty_obj,
       BEGIN OF ty_st,
         linep TYPE tj04-linep,
         statp TYPE tj04-statp,
         txt04 TYPE tj02t-txt04,
       END OF ty_st.

DATA: lt_fa     TYPE STANDARD TABLE OF ty_fa,
      lt_obj    TYPE SORTED TABLE OF ty_obj WITH UNIQUE KEY aufnr,
      ls_obj    TYPE ty_obj,
      lt_jsto   TYPE SORTED TABLE OF jsto WITH UNIQUE KEY objnr,
      ls_jsto   TYPE jsto,
      lt_jest   TYPE SORTED TABLE OF jest WITH NON-UNIQUE KEY objnr,
      ls_jest   TYPE jest,
      lt_tj02   TYPE SORTED TABLE OF tj02 WITH UNIQUE KEY istat,
      ls_tj02   TYPE tj02,
      lt_tj02t  TYPE SORTED TABLE OF tj02t WITH UNIQUE KEY istat spras,
      ls_tj02t  TYPE tj02t,
      lt_tj04   TYPE SORTED TABLE OF tj04 WITH UNIQUE KEY obtyp istat,
      ls_tj04   TYPE tj04,
      lt_st     TYPE STANDARD TABLE OF ty_st,
      ls_st     TYPE ty_st,
      lv_linep  TYPE tj04-linep,
      lv_t0     TYPE i,
      lv_t1     TYPE i,
      lv_ms_sel TYPE i,
      lv_ms_a   TYPE i,
      lv_ms_b   TYPE i,
      lv_ms_c   TYPE i,
      lv_n      TYPE i,
      lv_us_je  TYPE i,
      lv_diff   TYPE i,
      lv_rueck  TYPE i,
      lv_marb   TYPE i,
      lv_beide  TYPE i,
      lv_leer   TYPE i,
      lv_ek     TYPE i,
      lv_el     TYPE i,
      lv_s      TYPE i,
      lv_ek_aus TYPE i,
      lv_el_aus TYPE i,
      lv_s_aus  TYPE i,
      lv_aus    TYPE c LENGTH 1,
      lv_zeig   TYPE i.

FIELD-SYMBOLS: <ls_fa> TYPE ty_fa.

START-OF-SELECTION.

* ---- Auswahl wie DPC ShopAufSet Typ VR ----
  GET RUN TIME FIELD lv_t0.
  SELECT k~aufnr p~posnr k~dispo
    FROM afko AS k INNER JOIN afpo AS p ON p~aufnr = k~aufnr
    INTO CORRESPONDING FIELDS OF TABLE lt_fa
    WHERE p~dwerk = p_werks
      AND k~gltrp < '99991231'
      AND p~elikz = space
      AND p~dnrel = space.
  SORT lt_fa BY aufnr posnr.
  DELETE ADJACENT DUPLICATES FROM lt_fa COMPARING aufnr.
  IF p_max > 0.
    lv_n = p_max + 1.
    DELETE lt_fa FROM lv_n.
  ENDIF.
  GET RUN TIME FIELD lv_t1.
  lv_ms_sel = ( lv_t1 - lv_t0 ) / 1000.
  lv_n = lines( lt_fa ).

* ---- A: ZC22-Routine je Auftrag ----
  GET RUN TIME FIELD lv_t0.
  LOOP AT lt_fa ASSIGNING <ls_fa>.
    SELECT SINGLE objnr FROM aufk INTO <ls_fa>-objnr WHERE aufnr = <ls_fa>-aufnr.
    PERFORM status_lang IN PROGRAM zm_offene_fauf USING <ls_fa>-objnr CHANGING <ls_fa>-stta IF FOUND.
  ENDLOOP.
  GET RUN TIME FIELD lv_t1.
  lv_ms_a = ( lv_t1 - lv_t0 ) / 1000.

* ---- B: gesammelt, Sprache D ----
  GET RUN TIME FIELD lv_t0.
  IF lt_fa IS NOT INITIAL.
    SELECT aufnr objnr FROM aufk INTO TABLE lt_obj
      FOR ALL ENTRIES IN lt_fa WHERE aufnr = lt_fa-aufnr.
    IF lt_obj IS NOT INITIAL.
      SELECT * FROM jsto INTO TABLE lt_jsto
        FOR ALL ENTRIES IN lt_obj WHERE objnr = lt_obj-objnr.
      SELECT * FROM jest INTO TABLE lt_jest
        FOR ALL ENTRIES IN lt_obj
        WHERE objnr = lt_obj-objnr AND inact = space AND stat LIKE 'I%'.
    ENDIF.
    SELECT * FROM tj02 INTO TABLE lt_tj02 WHERE istat LIKE 'I%'.
    SELECT * FROM tj02t INTO TABLE lt_tj02t WHERE istat LIKE 'I%' AND spras = 'D'.
    SELECT * FROM tj04 INTO TABLE lt_tj04 WHERE istat LIKE 'I%'.
  ENDIF.
  LOOP AT lt_fa ASSIGNING <ls_fa>.
    CLEAR: lt_st, ls_jsto.
    READ TABLE lt_obj INTO ls_obj WITH TABLE KEY aufnr = <ls_fa>-aufnr.
    CHECK sy-subrc = 0.
    READ TABLE lt_jsto INTO ls_jsto WITH TABLE KEY objnr = ls_obj-objnr.
    LOOP AT lt_jest INTO ls_jest WHERE objnr = ls_obj-objnr.
      CLEAR ls_st.
      READ TABLE lt_tj02 INTO ls_tj02 WITH TABLE KEY istat = ls_jest-stat.
      IF sy-subrc = 0 AND ls_tj02-nodis IS NOT INITIAL.
        CONTINUE.
      ENDIF.
      READ TABLE lt_tj02t INTO ls_tj02t WITH TABLE KEY istat = ls_jest-stat spras = 'D'.
      IF sy-subrc = 0.
        ls_st-txt04 = ls_tj02t-txt04.
      ENDIF.
      READ TABLE lt_tj04 INTO ls_tj04 WITH TABLE KEY obtyp = ls_jsto-obtyp istat = ls_jest-stat.
      IF sy-subrc = 0.
        ls_st-linep = ls_tj04-linep.
        ls_st-statp = ls_tj04-statp.
      ENDIF.
      IF ls_st-linep IS INITIAL.
        ls_st-linep = '99'.
      ENDIF.
      IF ls_st-statp IS INITIAL.
        ls_st-statp = '99'.
      ENDIF.
      APPEND ls_st TO lt_st.
    ENDLOOP.
    SORT lt_st BY linep statp txt04.
    CLEAR lv_linep.
    LOOP AT lt_st INTO ls_st WHERE linep NE '99'.
      IF lv_linep = ls_st-linep.
        DELETE lt_st INDEX sy-tabix.
      ELSE.
        lv_linep = ls_st-linep.
      ENDIF.
    ENDLOOP.
    LOOP AT lt_st INTO ls_st.
      IF <ls_fa>-sttb IS INITIAL.
        <ls_fa>-sttb = ls_st-txt04.
      ELSE.
        CONCATENATE <ls_fa>-sttb ls_st-txt04 INTO <ls_fa>-sttb SEPARATED BY space.
      ENDIF.
    ENDLOOP.
  ENDLOOP.
  GET RUN TIME FIELD lv_t1.
  lv_ms_b = ( lv_t1 - lv_t0 ) / 1000.

* ---- C: naechster Vorgang ----
  IF p_avo = 'X'.
    GET RUN TIME FIELD lv_t0.
    LOOP AT lt_fa ASSIGNING <ls_fa>.
      CALL FUNCTION 'Z_MD04_NEXT_OPERATION'
        EXPORTING
          i_aufnr = <ls_fa>-aufnr
        IMPORTING
          e_vornr = <ls_fa>-vornr
          e_arbpl = <ls_fa>-arbpl.
    ENDLOOP.
    GET RUN TIME FIELD lv_t1.
    lv_ms_c = ( lv_t1 - lv_t0 ) / 1000.
  ENDIF.

* ---- Auswertung ----
  LOOP AT lt_fa ASSIGNING <ls_fa>.
    IF <ls_fa>-stta <> <ls_fa>-sttb.
      lv_diff = lv_diff + 1.
    ENDIF.
    IF <ls_fa>-stta IS INITIAL.
      lv_leer = lv_leer + 1.
    ENDIF.
    CLEAR lv_aus.
    IF <ls_fa>-stta CP 'FREI DRUC RÜCK VOKL*'.
      lv_rueck = lv_rueck + 1.
      lv_aus = 'X'.
    ENDIF.
    IF <ls_fa>-arbpl CP 'M*'.
      lv_marb = lv_marb + 1.
      IF lv_aus = 'X'.
        lv_beide = lv_beide + 1.
      ENDIF.
      lv_aus = 'X'.
    ENDIF.
    IF <ls_fa>-dispo CP 'EK*'.
      lv_ek = lv_ek + 1.
      IF lv_aus = 'X'.
        lv_ek_aus = lv_ek_aus + 1.
      ENDIF.
    ELSEIF <ls_fa>-dispo CP 'EL*'.
      lv_el = lv_el + 1.
      IF lv_aus = 'X'.
        lv_el_aus = lv_el_aus + 1.
      ENDIF.
    ELSEIF <ls_fa>-dispo CP 'S*'.
      lv_s = lv_s + 1.
      IF lv_aus = 'X'.
        lv_s_aus = lv_s_aus + 1.
      ENDIF.
    ENDIF.
  ENDLOOP.

  WRITE: / 'ERGEBNIS Werk', p_werks, 'Max', p_max, 'Avo', p_avo, 'Sprache', sy-langu.
  WRITE: / 'AUFTRAEGE', lv_n.
  WRITE: / 'MS_AUSWAHL', lv_ms_sel.
  IF lv_n > 0.
    lv_us_je = lv_ms_a * 1000 / lv_n.
  ENDIF.
  WRITE: / 'MS_A_STATUS_LANG', lv_ms_a, 'US_JE_AUFTRAG', lv_us_je.
  IF lv_n > 0.
    lv_us_je = lv_ms_b * 1000 / lv_n.
  ENDIF.
  WRITE: / 'MS_B_GESAMMELT', lv_ms_b, 'US_JE_AUFTRAG', lv_us_je.
  IF lv_n > 0.
    lv_us_je = lv_ms_c * 1000 / lv_n.
  ENDIF.
  WRITE: / 'MS_C_NEXT_OP', lv_ms_c, 'US_JE_AUFTRAG', lv_us_je.
  WRITE: / 'A_UNGLEICH_B', lv_diff, 'STATUS_LEER', lv_leer.
  WRITE: / 'FILTER_STATUS', lv_rueck, 'FILTER_ARBPL_M', lv_marb, 'BEIDE', lv_beide.
  WRITE: / 'EK', lv_ek, 'EK_AUS', lv_ek_aus, 'EL', lv_el, 'EL_AUS', lv_el_aus, 'S', lv_s, 'S_AUS', lv_s_aus.
  LOOP AT lt_fa ASSIGNING <ls_fa>.
    IF lv_zeig < 12 AND ( <ls_fa>-stta CP 'FREI DRUC RÜCK VOKL*' OR <ls_fa>-arbpl CP 'M*' OR lv_zeig < 4 ).
      WRITE: / 'BSP', <ls_fa>-aufnr, <ls_fa>-dispo, <ls_fa>-vornr, <ls_fa>-arbpl, <ls_fa>-stta(70).
      lv_zeig = lv_zeig + 1.
    ENDIF.
  ENDLOOP.
  LOOP AT lt_fa ASSIGNING <ls_fa>.
    CHECK <ls_fa>-stta <> <ls_fa>-sttb.
    WRITE: / 'DIFF', <ls_fa>-aufnr, <ls_fa>-stta(60), <ls_fa>-sttb(60).
    lv_zeig = lv_zeig + 1.
    IF lv_zeig > 20.
      EXIT.
    ENDIF.
  ENDLOOP.
