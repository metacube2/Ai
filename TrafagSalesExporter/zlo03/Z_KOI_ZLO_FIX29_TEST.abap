REPORT z_koi_zlo_fix29_test LINE-SIZE 1023 NO STANDARD PAGE HEADING.
* $TMP-Test 2026-11-23 (Claude) fuer ZLO03 FIX 29 (Schalter p_nofrm), nur T76, schreibt nichts in Tabellen.
* Grundlage: Z_KOI_ZLO_FIX28_TEST.
* Modus A: Z_ZLO03_TURBO2 direkt mit P_APP (wie das Gateway), P_NOFRM nur wenn p_nofrm = X; alle App-Zeilen.
* Modus D: ZloTopDownSet ueber ZCL_ZPOWERBI_EINKAUF_DPC_EXT (der echte Gateway-Weg); alle Zeilen.
* Modus B: Z_ZLO03_TURBO2 als Hintergrundjob (Excel-Inhalt geht im Batch nach
*          /usr/sap/trans/zlo03_analyse_<uname>.csv), Kopie nach /usr/sap/trans/zlo03_fix29_<vknr>_<tag>.csv,
*          Inhalt zeilenweise in die Liste (Tabulator als ~, in Stuecken zu 900 Zeichen).
* Modus L: die eigenen Kopien /usr/sap/trans/zlo03_fix29_<vknr>_<tag>.csv loeschen (Tags aus p_tag, p_tag2).

PARAMETERS: p_vknr  TYPE char40 DEFAULT '40731',
            p_modus TYPE char1 DEFAULT 'A',
            p_nofrm AS CHECKBOX DEFAULT ' ',
            p_tag   TYPE char10 DEFAULT 'REF1',
            p_tag2  TYPE char10 DEFAULT ' '.

DATA: gv_tab   TYPE c LENGTH 1,
      gv_matnr TYPE matnr.

START-OF-SELECTION.
  gv_tab = cl_abap_char_utilities=>horizontal_tab.
  CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
    EXPORTING
      input  = p_vknr
    IMPORTING
      output = gv_matnr
    EXCEPTIONS
      OTHERS = 1.
  IF sy-subrc <> 0.
    gv_matnr = p_vknr.
  ENDIF.
  CASE p_modus.
    WHEN 'A'.
      PERFORM modus_a.
    WHEN 'D'.
      PERFORM modus_d.
    WHEN 'B'.
      PERFORM modus_b.
    WHEN 'L'.
      PERFORM loeschen USING p_tag.
      IF p_tag2 IS NOT INITIAL.
        PERFORM loeschen USING p_tag2.
      ENDIF.
  ENDCASE.

*---------------------------------------------------------------------*
FORM modus_a.
  DATA: lt_sel TYPE STANDARD TABLE OF rsparams,
        ls_sel TYPE rsparams,
        lt_out TYPE STANDARD TABLE OF zstr_zlo_topdown.

  CLEAR ls_sel.
  ls_sel-kind = 'S'. ls_sel-selname = 'S_MATNR'. ls_sel-sign = 'I'. ls_sel-option = 'EQ'.
  ls_sel-low = gv_matnr. APPEND ls_sel TO lt_sel.
  CLEAR ls_sel.
  ls_sel-kind = 'P'.
  ls_sel-selname = 'P_TOPD'. ls_sel-low = 'X'.   APPEND ls_sel TO lt_sel.
  ls_sel-selname = 'P_BOTU'. ls_sel-low = space. APPEND ls_sel TO lt_sel.
  ls_sel-selname = 'P_APP'.  ls_sel-low = 'X'.   APPEND ls_sel TO lt_sel.
  IF p_nofrm = 'X'.
    ls_sel-selname = 'P_NOFRM'. ls_sel-low = 'X'. APPEND ls_sel TO lt_sel.
  ENDIF.

  FREE MEMORY ID 'ZZLO03_APP'.
  SUBMIT z_zlo03_turbo2 WITH SELECTION-TABLE lt_sel AND RETURN.
  IMPORT app_tab = lt_out FROM MEMORY ID 'ZZLO03_APP'.
  FREE MEMORY ID 'ZZLO03_APP'.
  PERFORM ausgabe USING 'A' lt_out.
ENDFORM.

*---------------------------------------------------------------------*
FORM modus_d.
  DATA: lo_dpc    TYPE REF TO zcl_zpowerbi_einkauf_dpc_ext,
        lt_filter TYPE /iwbep/t_mgw_select_option,
        ls_filter TYPE /iwbep/s_mgw_select_option,
        ls_so     TYPE /iwbep/s_cod_select_option,
        ls_paging TYPE /iwbep/s_mgw_paging,
        lr_data   TYPE REF TO data,
        lt_out    TYPE STANDARD TABLE OF zstr_zlo_topdown,
        lx        TYPE REF TO cx_root,
        lv_txt    TYPE string.
  FIELD-SYMBOLS <lt> TYPE STANDARD TABLE.

  ls_so-sign = 'I'. ls_so-option = 'EQ'.
  ls_filter-property = 'Werks'. ls_so-low = '1100'. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  CLEAR ls_filter. ls_filter-property = 'Vknr'. ls_so-low = p_vknr. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  CREATE OBJECT lo_dpc.
  TRY.
      lo_dpc->/iwbep/if_mgw_appl_srv_runtime~get_entityset(
        EXPORTING iv_entity_name = 'ZloTopDown' iv_entity_set_name = 'ZloTopDownSet' iv_source_name = 'ZloTopDown'
                  it_filter_select_options = lt_filter is_paging = ls_paging
        IMPORTING er_entityset = lr_data ).
    CATCH cx_root INTO lx.
      lv_txt = lx->get_text( ).
      WRITE: / 'D|Fehler|', lv_txt.
      RETURN.
  ENDTRY.
  ASSIGN lr_data->* TO <lt>.
  lt_out = <lt>.
  PERFORM ausgabe USING 'D' lt_out.
ENDFORM.

*---------------------------------------------------------------------*
FORM ausgabe USING pv_kz TYPE c pt_out TYPE STANDARD TABLE.
  DATA: ls_out   TYPE zstr_zlo_topdown,
        lv_n     TYPE i,
        lv_sum   TYPE p LENGTH 16 DECIMALS 6,
        lv_sum6  TYPE p LENGTH 16 DECIMALS 6,
        lv_menge TYPE c LENGTH 30,
        lv_stk   TYPE c LENGTH 30,
        lv_wert  TYPE c LENGTH 30.

  LOOP AT pt_out INTO ls_out.
    lv_n = lv_n + 1.
    lv_sum = lv_sum + ls_out-wert.
    IF ls_out-dispo >= '001' AND ls_out-dispo <= '006'.
      lv_sum6 = lv_sum6 + ls_out-wert.
    ENDIF.
    WRITE ls_out-menge TO lv_menge LEFT-JUSTIFIED NO-GROUPING.
    WRITE ls_out-stk TO lv_stk LEFT-JUSTIFIED NO-GROUPING.
    WRITE ls_out-wert TO lv_wert LEFT-JUSTIFIED NO-GROUPING.
    WRITE: / 'R|', pv_kz, '|', ls_out-vknr, '|', ls_out-idnrk, '|', lv_menge, '|', ls_out-meins, '|',
             lv_stk, '|', lv_wert, '|', ls_out-dispo, '|', ls_out-matkl, '|'.
  ENDLOOP.
  WRITE: / 'S|', pv_kz, '|nofrm', p_nofrm, '|Zeilen', lv_n, '|SummeWert', lv_sum, '|SummeWert001bis006', lv_sum6.
ENDFORM.

*---------------------------------------------------------------------*
FORM modus_b.
  DATA: lv_jobname  TYPE tbtcjob-jobname VALUE 'Z_KOI_ZLO_FIX29',
        lv_jobcount TYPE tbtcjob-jobcount,
        lv_status   TYPE tbtco-status,
        lv_file     TYPE string,
        lv_ziel     TYPE string,
        lv_line     TYPE string,
        lv_teil     TYPE string,
        lv_len      TYPE i,
        lv_off      TYPE i,
        lv_rest     TYPE i,
        lv_stueck   TYPE i,
        lv_zeile    TYPE i,
        lv_n        TYPE i,
        lt_lines    TYPE STANDARD TABLE OF string,
        lt_sel      TYPE STANDARD TABLE OF rsparams,
        ls_sel      TYPE rsparams.

  CLEAR ls_sel.
  ls_sel-kind = 'S'. ls_sel-selname = 'S_MATNR'. ls_sel-sign = 'I'. ls_sel-option = 'EQ'.
  ls_sel-low = gv_matnr. APPEND ls_sel TO lt_sel.
  CLEAR ls_sel.
  ls_sel-kind = 'P'.
  ls_sel-selname = 'P_TOPD'. ls_sel-low = 'X'.   APPEND ls_sel TO lt_sel.
  ls_sel-selname = 'P_BOTU'. ls_sel-low = space. APPEND ls_sel TO lt_sel.
  IF p_nofrm = 'X'.
    ls_sel-selname = 'P_NOFRM'. ls_sel-low = 'X'. APPEND ls_sel TO lt_sel.
  ENDIF.

  CALL FUNCTION 'JOB_OPEN'
    EXPORTING
      jobname  = lv_jobname
    IMPORTING
      jobcount = lv_jobcount
    EXCEPTIONS
      OTHERS   = 1.
  IF sy-subrc <> 0.
    WRITE: / 'B|JOB_OPEN Fehler'.
    RETURN.
  ENDIF.
  SUBMIT z_zlo03_turbo2 WITH SELECTION-TABLE lt_sel
         VIA JOB lv_jobname NUMBER lv_jobcount AND RETURN.
  CALL FUNCTION 'JOB_CLOSE'
    EXPORTING
      jobcount  = lv_jobcount
      jobname   = lv_jobname
      strtimmed = 'X'
    EXCEPTIONS
      OTHERS    = 1.
  IF sy-subrc <> 0.
    WRITE: / 'B|JOB_CLOSE Fehler'.
    RETURN.
  ENDIF.
  DO 150 TIMES.
    WAIT UP TO 2 SECONDS.
    SELECT SINGLE status FROM tbtco INTO lv_status
      WHERE jobname = lv_jobname AND jobcount = lv_jobcount.
    IF lv_status = 'F' OR lv_status = 'A'.
      EXIT.
    ENDIF.
  ENDDO.
  WRITE: / 'B|', p_vknr, '|nofrm', p_nofrm, '|Job', lv_jobcount, '|Status', lv_status.
  IF lv_status <> 'F'.
    RETURN.
  ENDIF.

  CONCATENATE '/usr/sap/trans/zlo03_analyse_' sy-uname '.csv' INTO lv_file.
  OPEN DATASET lv_file FOR INPUT IN TEXT MODE ENCODING DEFAULT.
  IF sy-subrc <> 0.
    WRITE: / 'B|Datei nicht lesbar', lv_file.
    RETURN.
  ENDIF.
  DO.
    READ DATASET lv_file INTO lv_line.
    IF sy-subrc <> 0.
      EXIT.
    ENDIF.
    APPEND lv_line TO lt_lines.
  ENDDO.
  CLOSE DATASET lv_file.
  lv_n = lines( lt_lines ).

  CONCATENATE '/usr/sap/trans/zlo03_fix29_' p_vknr '_' p_tag '.csv' INTO lv_ziel.
  CONDENSE lv_ziel NO-GAPS.
  OPEN DATASET lv_ziel FOR OUTPUT IN TEXT MODE ENCODING DEFAULT.
  IF sy-subrc = 0.
    LOOP AT lt_lines INTO lv_line.
      TRANSFER lv_line TO lv_ziel.
    ENDLOOP.
    CLOSE DATASET lv_ziel.
    WRITE: / 'B|gesichert|', lv_ziel, '|Zeilen', lv_n.
  ELSE.
    WRITE: / 'B|nicht gesichert|', lv_ziel.
  ENDIF.

  LOOP AT lt_lines INTO lv_line.
    lv_zeile = sy-tabix.
    REPLACE ALL OCCURRENCES OF gv_tab IN lv_line WITH '~'.
    lv_len = strlen( lv_line ).
    lv_off = 0.
    lv_stueck = 0.
    DO.
      lv_rest = lv_len - lv_off.
      IF lv_rest <= 0 AND lv_stueck > 0.
        EXIT.
      ENDIF.
      IF lv_rest > 900.
        lv_rest = 900.
      ENDIF.
      CLEAR lv_teil.
      IF lv_rest > 0.
        lv_teil = lv_line+lv_off(lv_rest).
      ENDIF.
      lv_stueck = lv_stueck + 1.
      WRITE: / 'L|', lv_zeile, '|', lv_stueck, '|', lv_teil.
      lv_off = lv_off + lv_rest.
      IF lv_rest = 0.
        EXIT.
      ENDIF.
    ENDDO.
  ENDLOOP.
  WRITE: / 'E|Zeilen', lv_n.
ENDFORM.

*---------------------------------------------------------------------*
FORM loeschen USING pv_tag TYPE clike.
  DATA: lv_f TYPE string.
  CONCATENATE '/usr/sap/trans/zlo03_fix29_' p_vknr '_' pv_tag '.csv' INTO lv_f.
  CONDENSE lv_f NO-GAPS.
  DELETE DATASET lv_f.
  WRITE: / 'D|', lv_f, '|subrc', sy-subrc.
ENDFORM.
