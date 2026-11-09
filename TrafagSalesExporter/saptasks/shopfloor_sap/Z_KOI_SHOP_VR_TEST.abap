REPORT z_koi_shop_vr_test LINE-SIZE 255 NO STANDARD PAGE HEADING.
* $TMP-Test 2026-11-09 (Claude), nur T76: ShopAufSet Typ VR (Auftragsvorrat) gegen ZC22 (ZM_OFFENE_FAUF).
* Schreibt nichts. Teil 1: Set ueber ZCL_ZPOWERBI_EINKAUF_DPC_EXT (wie das Gateway). Teil 2: ZC22 per SUBMIT mit
* abgefangener ALV-Tabelle (cl_salv_bs_runtime_info), Disponent wie Parameter, mit Planauftraegen, ohne Materialfilter.
* Vergleich je Auftrag: Anzahl, Menge, Abweichungen.

PARAMETERS: p_dvon TYPE afko-dispo DEFAULT 'S00',
            p_dbis TYPE afko-dispo DEFAULT 'SZZ'.

TYPES: BEGIN OF ty_a,
         aufnr TYPE char12,
         menge TYPE p LENGTH 15 DECIMALS 3,
         datum TYPE sy-datum,
         dispo TYPE char3,
       END OF ty_a.
DATA: lt_set TYPE SORTED TABLE OF ty_a WITH UNIQUE KEY aufnr,
      lt_z22 TYPE SORTED TABLE OF ty_a WITH UNIQUE KEY aufnr,
      ls_a   TYPE ty_a,
      ls_b   TYPE ty_a.

START-OF-SELECTION.
  PERFORM teil_set.
  PERFORM teil_z22.
  PERFORM vergleich.

*---------------------------------------------------------------------*
FORM teil_set.
  DATA: lo_dpc    TYPE REF TO zcl_zpowerbi_einkauf_dpc_ext,
        lt_filter TYPE /iwbep/t_mgw_select_option,
        ls_filter TYPE /iwbep/s_mgw_select_option,
        ls_so     TYPE /iwbep/s_cod_select_option,
        ls_paging TYPE /iwbep/s_mgw_paging,
        lr_data   TYPE REF TO data,
        lx        TYPE REF TO cx_root,
        lv_txt    TYPE string,
        lv_n      TYPE i,
        lv_alle   TYPE i.
  FIELD-SYMBOLS: <lt> TYPE STANDARD TABLE,
                 <ls> TYPE any,
                 <f>  TYPE any.
  ls_so-sign = 'I'. ls_so-option = 'EQ'.
  ls_filter-property = 'Werks'. ls_so-low = '1100'. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  CLEAR ls_filter. ls_filter-property = 'Datum'. ls_so-low = sy-datum. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  CLEAR ls_filter. ls_filter-property = 'Typ'. ls_so-low = 'VR'. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  CREATE OBJECT lo_dpc.
  TRY.
      lo_dpc->/iwbep/if_mgw_appl_srv_runtime~get_entityset(
        EXPORTING iv_entity_name = 'ShopAuf' iv_entity_set_name = 'ShopAufSet' iv_source_name = 'ShopAuf'
                  it_filter_select_options = lt_filter is_paging = ls_paging
        IMPORTING er_entityset = lr_data ).
    CATCH cx_root INTO lx.
      lv_txt = lx->get_text( ). WRITE: / 'SET Fehler:', lv_txt.
      RETURN.
  ENDTRY.
  ASSIGN lr_data->* TO <lt>.
  lv_alle = lines( <lt> ).
  LOOP AT <lt> ASSIGNING <ls>.
    CLEAR ls_a.
    ASSIGN COMPONENT 'DISPO' OF STRUCTURE <ls> TO <f>. ls_a-dispo = <f>.
    CHECK ls_a-dispo >= p_dvon AND ls_a-dispo <= p_dbis.
    ASSIGN COMPONENT 'AUFNR' OF STRUCTURE <ls> TO <f>. ls_a-aufnr = <f>.
    SHIFT ls_a-aufnr LEFT DELETING LEADING '0'.
    ASSIGN COMPONENT 'MENGE' OF STRUCTURE <ls> TO <f>. ls_a-menge = <f>.
    ASSIGN COMPONENT 'DATUM' OF STRUCTURE <ls> TO <f>. ls_a-datum = <f>.
    INSERT ls_a INTO TABLE lt_set.
  ENDLOOP.
  lv_n = lines( lt_set ).
  WRITE: / 'SET|Zeilen gesamt|', lv_alle, '|im Disponentenbereich|', lv_n.
ENDFORM.

*---------------------------------------------------------------------*
FORM teil_z22.
  DATA: lr_data  TYPE REF TO data,
        lr_dispo TYPE RANGE OF afko-dispo,
        ls_dispo LIKE LINE OF lr_dispo,
        lx       TYPE REF TO cx_root,
        lv_txt   TYPE string,
        lv_n     TYPE i.
  FIELD-SYMBOLS: <lt> TYPE STANDARD TABLE,
                 <ls> TYPE any,
                 <f>  TYPE any.
  ls_dispo-sign = 'I'. ls_dispo-option = 'BT'. ls_dispo-low = p_dvon. ls_dispo-high = p_dbis. APPEND ls_dispo TO lr_dispo.
  cl_salv_bs_runtime_info=>set( display = abap_false metadata = abap_false data = abap_true ).
  SUBMIT zm_offene_fauf WITH s_dispo IN lr_dispo WITH p_plaf = 'X' WITH p_peg = ' ' WITH p_avo = ' ' AND RETURN.
  TRY.
      cl_salv_bs_runtime_info=>get_data_ref( IMPORTING r_data = lr_data ).
    CATCH cx_root INTO lx.
      lv_txt = lx->get_text( ). WRITE: / 'ZC22 Fehler:', lv_txt.
      cl_salv_bs_runtime_info=>clear_all( ).
      RETURN.
  ENDTRY.
  cl_salv_bs_runtime_info=>clear_all( ).
  ASSIGN lr_data->* TO <lt>.
  LOOP AT <lt> ASSIGNING <ls>.
    CLEAR ls_a.
    ASSIGN COMPONENT 'AUFNR' OF STRUCTURE <ls> TO <f>. ls_a-aufnr = <f>.
    SHIFT ls_a-aufnr LEFT DELETING LEADING '0'.
    ASSIGN COMPONENT 'PSMNG' OF STRUCTURE <ls> TO <f>. ls_a-menge = <f>.
    ASSIGN COMPONENT 'GLTRP' OF STRUCTURE <ls> TO <f>. ls_a-datum = <f>.
    READ TABLE lt_z22 INTO ls_b WITH TABLE KEY aufnr = ls_a-aufnr.
    IF sy-subrc = 0.
      ls_b-menge = ls_b-menge + ls_a-menge.
      MODIFY TABLE lt_z22 FROM ls_b.
    ELSE.
      INSERT ls_a INTO TABLE lt_z22.
    ENDIF.
  ENDLOOP.
  lv_n = lines( lt_z22 ).
  WRITE: / 'ZC22|Auftraege|', lv_n.
ENDFORM.

*---------------------------------------------------------------------*
FORM vergleich.
  DATA: lv_s1 TYPE p LENGTH 15 DECIMALS 3, lv_s2 TYPE p LENGTH 15 DECIMALS 3,
        lv_nur_set TYPE i, lv_nur_z22 TYPE i, lv_diff TYPE i, lv_w TYPE i.
  LOOP AT lt_set INTO ls_a. lv_s1 = lv_s1 + ls_a-menge. ENDLOOP.
  LOOP AT lt_z22 INTO ls_a. lv_s2 = lv_s2 + ls_a-menge. ENDLOOP.
  WRITE: / 'SUMME|Set|', lv_s1, '|ZC22|', lv_s2.
  LOOP AT lt_set INTO ls_a.
    READ TABLE lt_z22 INTO ls_b WITH TABLE KEY aufnr = ls_a-aufnr.
    IF sy-subrc <> 0.
      lv_nur_set = lv_nur_set + 1.
      IF lv_w < 15. WRITE: / 'NUR_SET|', ls_a-aufnr, ls_a-dispo, ls_a-datum, ls_a-menge. lv_w = lv_w + 1. ENDIF.
    ELSEIF ls_b-menge <> ls_a-menge.
      lv_diff = lv_diff + 1.
      IF lv_w < 15. WRITE: / 'MENGE|', ls_a-aufnr, ls_a-menge, ls_b-menge. lv_w = lv_w + 1. ENDIF.
    ENDIF.
  ENDLOOP.
  LOOP AT lt_z22 INTO ls_b.
    READ TABLE lt_set TRANSPORTING NO FIELDS WITH TABLE KEY aufnr = ls_b-aufnr.
    IF sy-subrc <> 0.
      lv_nur_z22 = lv_nur_z22 + 1.
      IF lv_w < 30. WRITE: / 'NUR_ZC22|', ls_b-aufnr, ls_b-datum, ls_b-menge. lv_w = lv_w + 1. ENDIF.
    ENDIF.
  ENDLOOP.
  WRITE: / 'ERGEBNIS|nur Set|', lv_nur_set, '|nur ZC22|', lv_nur_z22, '|Menge anders|', lv_diff.
ENDFORM.
