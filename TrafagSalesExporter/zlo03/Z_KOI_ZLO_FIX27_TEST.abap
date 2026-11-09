REPORT z_koi_zlo_fix26_test LINE-SIZE 1023 NO STANDARD PAGE HEADING.
* $TMP-Test 2026-11-05/09 (Claude) fuer ZLO03 FIX 26 und FIX 27 (Basismenge STKO-BMENG), nur T76, schreibt nichts in Tabellen.
* Teil A: ZloTopDownSet ueber ZCL_ZPOWERBI_EINKAUF_DPC_EXT (derselbe SUBMIT mit P_APP wie das Gateway) je VKNR:
*         Zeilen, Summe Wert, Wafer-Zeilen und einige Vergleichskomponenten.
* Teil B: Z_ZLO03_TURBO2 als Hintergrundjob (Excel-Inhalt geht im Batch nach /usr/sap/trans/zlo03_analyse_<uname>.csv):
*         Bottom-Up E11283 und Top-Down E11221 ausgeben; Top-Down 41019 als VORHER bzw. NACHHER sichern und vergleichen.

DATA: gv_tab TYPE c LENGTH 1.

START-OF-SELECTION.
  gv_tab = cl_abap_char_utilities=>horizontal_tab.
  PERFORM teil_a USING 'E11221'.
  PERFORM teil_a USING '41019'.
  PERFORM teil_b USING 'E11283' 'B' ' '.
  PERFORM teil_b USING 'E11221' 'T' ' '.

*---------------------------------------------------------------------*
FORM teil_a USING pv_vknr TYPE clike.
  DATA: lo_dpc    TYPE REF TO zcl_zpowerbi_einkauf_dpc_ext,
        lt_filter TYPE /iwbep/t_mgw_select_option,
        ls_filter TYPE /iwbep/s_mgw_select_option,
        ls_so     TYPE /iwbep/s_cod_select_option,
        ls_paging TYPE /iwbep/s_mgw_paging,
        lr_data   TYPE REF TO data,
        lt_out    TYPE STANDARD TABLE OF zstr_zlo_topdown,
        ls_out    TYPE zstr_zlo_topdown,
        lv_n      TYPE i,
        lv_sum    TYPE p LENGTH 16 DECIMALS 6,
        lx        TYPE REF TO cx_root,
        lv_txt    TYPE string.
  FIELD-SYMBOLS <lt> TYPE STANDARD TABLE.

  ls_so-sign = 'I'. ls_so-option = 'EQ'.
  ls_filter-property = 'Werks'. ls_so-low = '1100'. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  CLEAR ls_filter. ls_filter-property = 'Vknr'. ls_so-low = pv_vknr. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  CREATE OBJECT lo_dpc.
  TRY.
      lo_dpc->/iwbep/if_mgw_appl_srv_runtime~get_entityset(
        EXPORTING iv_entity_name = 'ZloTopDown' iv_entity_set_name = 'ZloTopDownSet' iv_source_name = 'ZloTopDown'
                  it_filter_select_options = lt_filter is_paging = ls_paging
        IMPORTING er_entityset = lr_data ).
    CATCH cx_root INTO lx.
      lv_txt = lx->get_text( ). WRITE: / 'A', pv_vknr, 'Fehler:', lv_txt.
      RETURN.
  ENDTRY.
  ASSIGN lr_data->* TO <lt>.
  lt_out = <lt>.
  lv_n = lines( lt_out ).
  LOOP AT lt_out INTO ls_out.
    lv_sum = lv_sum + ls_out-wert.
  ENDLOOP.
  WRITE: / 'A|', pv_vknr, '|Zeilen|', lv_n, '|SummeWert|', lv_sum.
  LOOP AT lt_out INTO ls_out WHERE idnrk CP 'E112*' OR idnrk CP 'E113*' OR idnrk = 'H05039' OR idnrk = 'D85020'
                                  OR idnrk = 'H20201' OR idnrk = 'H17500'.
    WRITE: / 'A|', pv_vknr, '|', ls_out-idnrk(10), '|Menge|', ls_out-menge, '|', ls_out-meins,
             '|Stk|', ls_out-stk, '|Wert|', ls_out-wert.
  ENDLOOP.
ENDFORM.

*---------------------------------------------------------------------*
FORM teil_b USING pv_matnr TYPE clike pv_dir TYPE c pv_sichern TYPE c.
  DATA: lt_f TYPE STANDARD TABLE OF string, lv_nf TYPE i, lv_k TYPE i, lv_f1 TYPE string, lv_f5 TYPE string, lv_fl1 TYPE string, lv_fl2 TYPE string.
  DATA: lv_jobname  TYPE tbtcjob-jobname VALUE 'Z_KOI_ZLO_FIX26',
        lv_jobcount TYPE tbtcjob-jobcount,
        lv_status   TYPE tbtco-status,
        lv_i        TYPE i,
        lv_file     TYPE string,
        lv_vorher   TYPE string,
        lv_nachher  TYPE string,
        lv_line     TYPE string,
        lv_show     TYPE string,
        lt_lines    TYPE STANDARD TABLE OF string,
        lt_alt      TYPE STANDARD TABLE OF string,
        lv_topd     TYPE c LENGTH 1,
        lv_botu     TYPE c LENGTH 1,
        lv_matnr    TYPE matnr,
        lv_n        TYPE i.
  DATA: lr_matnr TYPE RANGE OF matnr,
        ls_matnr LIKE LINE OF lr_matnr.

  CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT' EXPORTING input = pv_matnr IMPORTING output = lv_matnr EXCEPTIONS OTHERS = 1.
  ls_matnr-sign = 'I'. ls_matnr-option = 'EQ'. ls_matnr-low = lv_matnr. APPEND ls_matnr TO lr_matnr.
  IF pv_dir = 'T'. lv_topd = 'X'. ELSE. lv_botu = 'X'. ENDIF.

  CALL FUNCTION 'JOB_OPEN' EXPORTING jobname = lv_jobname IMPORTING jobcount = lv_jobcount EXCEPTIONS OTHERS = 1.
  IF sy-subrc <> 0. WRITE: / 'B', pv_matnr, 'JOB_OPEN Fehler', sy-subrc. RETURN. ENDIF.
  SUBMIT z_zlo03_turbo2 WITH s_matnr IN lr_matnr WITH p_topd = lv_topd WITH p_botu = lv_botu
         VIA JOB lv_jobname NUMBER lv_jobcount AND RETURN.
  CALL FUNCTION 'JOB_CLOSE' EXPORTING jobcount = lv_jobcount jobname = lv_jobname strtimmed = 'X' EXCEPTIONS OTHERS = 1.
  IF sy-subrc <> 0. WRITE: / 'B', pv_matnr, 'JOB_CLOSE Fehler', sy-subrc. RETURN. ENDIF.
  DO 150 TIMES.
    WAIT UP TO 2 SECONDS.
    SELECT SINGLE status FROM tbtco INTO lv_status WHERE jobname = lv_jobname AND jobcount = lv_jobcount.
    IF lv_status = 'F' OR lv_status = 'A'.
      EXIT.
    ENDIF.
  ENDDO.
  WRITE: / 'B|', pv_matnr, '|', pv_dir, '|Job', lv_jobcount, '|Status', lv_status.
  IF lv_status <> 'F'. RETURN. ENDIF.

  CONCATENATE '/usr/sap/trans/zlo03_analyse_' sy-uname '.csv' INTO lv_file.
  OPEN DATASET lv_file FOR INPUT IN TEXT MODE ENCODING DEFAULT.
  IF sy-subrc <> 0. WRITE: / 'B', pv_matnr, 'Datei nicht lesbar', lv_file. RETURN. ENDIF.
  DO.
    READ DATASET lv_file INTO lv_line.
    IF sy-subrc <> 0. EXIT. ENDIF.
    APPEND lv_line TO lt_lines.
  ENDDO.
  CLOSE DATASET lv_file.
  lv_n = lines( lt_lines ).
  WRITE: / 'B|', pv_matnr, '|Zeilen', lv_n.

  IF pv_sichern IS INITIAL.
    LOOP AT lt_lines INTO lv_line.
      " FIX 27: erste Spalte, Spalte 5 (VKNR alt) und die letzten zwei Spalten (je 1 Stk)
      SPLIT lv_line AT gv_tab INTO TABLE lt_f.
      lv_nf = lines( lt_f ).
      CLEAR: lv_f1, lv_f5, lv_fl1, lv_fl2.
      READ TABLE lt_f INDEX 1 INTO lv_f1.
      READ TABLE lt_f INDEX 5 INTO lv_f5.
      IF lv_nf > 1.
        lv_k = lv_nf - 1.
        READ TABLE lt_f INDEX lv_k INTO lv_fl1.
      ENDIF.
      READ TABLE lt_f INDEX lv_nf INTO lv_fl2.
      WRITE: / 'B|', pv_matnr, '|Sp', lv_nf, '|', lv_f1, '|5:', lv_f5, '|vorletzte:', lv_fl1, '|letzte:', lv_fl2.
    ENDLOOP.
    RETURN.
  ENDIF.

  " 41019: VORHER sichern, beim zweiten Lauf NACHHER sichern und vergleichen
  CONCATENATE '/usr/sap/trans/zlo03_fix26_' pv_matnr '_vorher.csv' INTO lv_vorher.
  CONCATENATE '/usr/sap/trans/zlo03_fix26_' pv_matnr '_nachher.csv' INTO lv_nachher.
  OPEN DATASET lv_vorher FOR INPUT IN TEXT MODE ENCODING DEFAULT.
  IF sy-subrc <> 0.
    OPEN DATASET lv_vorher FOR OUTPUT IN TEXT MODE ENCODING DEFAULT.
    LOOP AT lt_lines INTO lv_line. TRANSFER lv_line TO lv_vorher. ENDLOOP.
    CLOSE DATASET lv_vorher.
    WRITE: / 'B|', pv_matnr, '|gesichert als VORHER', lv_vorher.
    RETURN.
  ENDIF.
  DO.
    READ DATASET lv_vorher INTO lv_line.
    IF sy-subrc <> 0. EXIT. ENDIF.
    APPEND lv_line TO lt_alt.
  ENDDO.
  CLOSE DATASET lv_vorher.
  OPEN DATASET lv_nachher FOR OUTPUT IN TEXT MODE ENCODING DEFAULT.
  LOOP AT lt_lines INTO lv_line. TRANSFER lv_line TO lv_nachher. ENDLOOP.
  CLOSE DATASET lv_nachher.
  PERFORM vergleich USING pv_matnr lt_alt lt_lines.
ENDFORM.

*---------------------------------------------------------------------*
FORM vergleich USING pv_matnr TYPE clike pt_alt TYPE STANDARD TABLE pt_neu TYPE STANDARD TABLE.
  DATA: lv_alt   TYPE string,
        lv_neu   TYPE string,
        lt_ca    TYPE STANDARD TABLE OF string,
        lt_cn    TYPE STANDARD TABLE OF string,
        lv_ca    TYPE string,
        lv_cn    TYPE string,
        lv_row   TYPE i,
        lv_col   TYPE i,
        lv_nc    TYPE i,
        lv_na    TYPE i,
        lv_nn    TYPE i,
        lv_fmt   TYPE i,
        lv_real  TYPE i,
        lv_pa    TYPE p LENGTH 16 DECIMALS 10,
        lv_pn    TYPE p LENGTH 16 DECIMALS 10,
        lv_numok TYPE c LENGTH 1.

  lv_na = lines( pt_alt ). lv_nn = lines( pt_neu ).
  WRITE: / 'V|', pv_matnr, '|Zeilen vorher', lv_na, '|nachher', lv_nn.
  LOOP AT pt_alt INTO lv_alt.
    lv_row = sy-tabix.
    CLEAR lv_neu.
    READ TABLE pt_neu INTO lv_neu INDEX lv_row.
    IF lv_alt = lv_neu. CONTINUE. ENDIF.
    SPLIT lv_alt AT gv_tab INTO TABLE lt_ca.
    SPLIT lv_neu AT gv_tab INTO TABLE lt_cn.
    lv_nc = lines( lt_ca ).
    IF lines( lt_cn ) > lv_nc. lv_nc = lines( lt_cn ). ENDIF.
    DO lv_nc TIMES.
      lv_col = sy-index.
      CLEAR: lv_ca, lv_cn.
      READ TABLE lt_ca INTO lv_ca INDEX lv_col.
      READ TABLE lt_cn INTO lv_cn INDEX lv_col.
      IF lv_ca = lv_cn. CONTINUE. ENDIF.
      CLEAR lv_numok.
      IF lv_ca CO '0123456789.- ' AND lv_cn CO '0123456789.- ' AND lv_ca IS NOT INITIAL AND lv_cn IS NOT INITIAL.
        TRY.
            lv_pa = lv_ca. lv_pn = lv_cn.
            IF lv_pa = lv_pn. lv_numok = 'X'. ENDIF.
          CATCH cx_root.
        ENDTRY.
      ENDIF.
      IF lv_numok = 'X'.
        lv_fmt = lv_fmt + 1.
      ELSE.
        lv_real = lv_real + 1.
        WRITE: / 'V|', pv_matnr, '|Zeile', lv_row, '|Spalte', lv_col, '|vorher', lv_ca(40), '|nachher', lv_cn(40).
      ENDIF.
    ENDDO.
  ENDLOOP.
  WRITE: / 'V|', pv_matnr, '|Zellen nur Format (zahlengleich)', lv_fmt, '|Zellen anders', lv_real.
ENDFORM.
