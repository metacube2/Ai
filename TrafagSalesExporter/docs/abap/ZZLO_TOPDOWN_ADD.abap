*&---------------------------------------------------------------------*
*& Einkauf, Materialanteil je Produktgruppe (Phase 2): ZloTopDownSet im Service ZPOWERBI_EINKAUF_SRV
*& Stand    : 2026-11-02, erste Fassung (T76), Auftrag Ingo, Bauplan
*&            saptasks/zc23_materialanteil/BAUPLAN_PHASE2_MATERIALANTEIL_2026-11-02.md
*& Zweck    : ZLO03 Top-Down (Z_ZLO03_TURBO2) fuer eine Liste von VKNR, ohne Excel: je VKNR und
*&            Komponente die Stuecklistenmenge je 1 VKNR, Stueckkosten und Wert in der Einheit der
*&            Stueckliste, Disponent und Warengruppe der Komponente.
*& Muster   : wie Zc23Set (saptasks/fiori_zc23/ZC23_DPC_GET_ENTITYSET_ADD.abap): SUBMIT mit P_APP,
*&            Ergebnis aus dem ABAP-Memory. Die Rechnung bleibt in Z_ZLO03_TURBO2 (FIX 25, FORM app_export).
*&
*& DDIC (SE11, Paket wie ZSTR_ZC23_APP):
*&   ZSTR_ZLO_TOPDOWN: WERKS WERKS_D, VKNR MATNR, IDNRK MATNR, MENGE /SCMTMS/DECIMAL_VALUE,
*&                     MEINS MEINS, STK /SCMTMS/DECIMAL_VALUE, WERT /SCMTMS/DECIMAL_VALUE,
*&                     DISPO DISPO, MATKL MATKL, MAKTX MAKTX
*&   /SCMTMS/DECIMAL_VALUE = DEC 28,6 (DD04L T76 2026-11-02); bewusst kein QUAN-Feld, damit keine
*&   Mengenreferenz noetig ist (Fall 2026-09-30, Aktivierung ohne Referenz).
*&
*& Filter (alle EQ, mehrfach = ODER):
*&   Werks (Pflicht, nur 1100: ZLO03 rechnet fest mit Werk 1100), Vknr (Pflicht, 1 bis 200),
*&   Dispo (optional, Disponent der Komponente, z. B. 001 bis 006).
*&   Ohne Pflichtfilter: HTTP 400 ohne Reportlauf (Dienst ist auch die Power-BI-Quelle).
*&---------------------------------------------------------------------*

* ===== MPC_EXT DEFINE: am Ende, direkt vor ENDMETHOD einfuegen =========
* ---------------------------------------------------------------------
* ZloTopDown: ZloTopDownSet (ZLO03 Top-Down), Pflichtfilter Werks, Vknr
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'ZloTopDown'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Vknr' iv_abap_fieldname = 'VKNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Idnrk' iv_abap_fieldname = 'IDNRK' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Menge' iv_abap_fieldname = 'MENGE' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 6 ).
  lo_property->set_maxlength( iv_max_length = 28 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Meins' iv_abap_fieldname = 'MEINS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Stk' iv_abap_fieldname = 'STK' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 6 ).
  lo_property->set_maxlength( iv_max_length = 28 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Wert' iv_abap_fieldname = 'WERT' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 6 ).
  lo_property->set_maxlength( iv_max_length = 28 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Dispo' iv_abap_fieldname = 'DISPO' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matkl' iv_abap_fieldname = 'MATKL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 9 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Maktx' iv_abap_fieldname = 'MAKTX' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_ZLO_TOPDOWN' iv_bind_conversions = ' ' ).
  lo_entity_set = lo_entity_type->create_entity_set( 'ZloTopDownSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
* ===== Ende MPC ========================================================

* ===== DPC_EXT GET_ENTITYSET: nach dem Zc23-Block, VOR "IF iv_entity_set_name <> 'FinanzJournalSet'." =====
* Einkauf Materialanteil Phase 2, docs/abap/ZZLO_TOPDOWN_ADD.abap

  IF iv_entity_set_name = 'ZloTopDownSet'.

    DATA: lt_zlo_out   TYPE STANDARD TABLE OF zstr_zlo_topdown,
          ls_zlo_out   TYPE zstr_zlo_topdown,
          lt_zlo_sel   TYPE STANDARD TABLE OF rsparams,
          ls_zlo_sel   TYPE rsparams,
          lt_zlo_dispo TYPE STANDARD TABLE OF dispo,
          lv_zlo_dispo TYPE dispo,
          lv_zlo_werk  TYPE werks_d,
          lv_zlo_matnr TYPE matnr,
          lv_zlo_anz   TYPE i,
          lv_zlo_skip  TYPE i,
          lv_zlo_max   TYPE i,
          lv_zlo_zeile TYPE i,
          lv_zlo_msg   TYPE bapi_msg.

    FIELD-SYMBOLS: <ls_zlo_f> LIKE LINE OF it_filter_select_options,
                   <ls_zlo_o> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_zlo_f>.
      LOOP AT <ls_zlo_f>-select_options ASSIGNING <ls_zlo_o>
           WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_zlo_f>-property ).
          WHEN 'WERKS'.
            lv_zlo_werk = <ls_zlo_o>-low.
          WHEN 'VKNR'.
            CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
              EXPORTING  input        = <ls_zlo_o>-low
              IMPORTING  output       = lv_zlo_matnr
              EXCEPTIONS length_error = 1
                         OTHERS       = 2.
            IF sy-subrc <> 0.
              lv_zlo_matnr = <ls_zlo_o>-low.
            ENDIF.
            CLEAR ls_zlo_sel.
            ls_zlo_sel-kind    = 'S'.
            ls_zlo_sel-selname = 'S_MATNR'.
            ls_zlo_sel-sign    = 'I'.
            ls_zlo_sel-option  = 'EQ'.
            ls_zlo_sel-low     = lv_zlo_matnr.
            APPEND ls_zlo_sel TO lt_zlo_sel.
            lv_zlo_anz = lv_zlo_anz + 1.
          WHEN 'DISPO'.
            lv_zlo_dispo = <ls_zlo_o>-low.
            APPEND lv_zlo_dispo TO lt_zlo_dispo.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

*   Schutz: ohne Werk 1100 und 1 bis 200 VKNR kein Reportlauf
    IF lv_zlo_werk <> '1100' OR lv_zlo_anz < 1 OR lv_zlo_anz > 200.
      lv_zlo_msg = 'ZloTopDownSet braucht Werks = 1100 und 1 bis 200 Vknr (EQ).'.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = lv_zlo_msg.
    ENDIF.

    CLEAR ls_zlo_sel.
    ls_zlo_sel-kind = 'P'.
    ls_zlo_sel-selname = 'P_TOPD'.  ls_zlo_sel-low = 'X'.   APPEND ls_zlo_sel TO lt_zlo_sel.
    ls_zlo_sel-selname = 'P_BOTU'.  ls_zlo_sel-low = space. APPEND ls_zlo_sel TO lt_zlo_sel.
    ls_zlo_sel-selname = 'P_APP'.   ls_zlo_sel-low = 'X'.   APPEND ls_zlo_sel TO lt_zlo_sel.

    FREE MEMORY ID 'ZZLO03_APP'.
    SUBMIT z_zlo03_turbo2 WITH SELECTION-TABLE lt_zlo_sel AND RETURN.
    IMPORT app_tab = lt_zlo_out FROM MEMORY ID 'ZZLO03_APP'.
    FREE MEMORY ID 'ZZLO03_APP'.

    LOOP AT lt_zlo_out INTO ls_zlo_out.
      lv_zlo_zeile = sy-tabix.
      IF lt_zlo_dispo IS NOT INITIAL.
        READ TABLE lt_zlo_dispo WITH KEY table_line = ls_zlo_out-dispo TRANSPORTING NO FIELDS.
        IF sy-subrc <> 0.
          DELETE lt_zlo_out INDEX lv_zlo_zeile.
          CONTINUE.
        ENDIF.
      ENDIF.
      ls_zlo_out-werks = lv_zlo_werk.
      MODIFY lt_zlo_out FROM ls_zlo_out INDEX lv_zlo_zeile TRANSPORTING werks.
    ENDLOOP.

    IF is_paging-skip > 0.
      lv_zlo_skip = is_paging-skip.
      DELETE lt_zlo_out TO lv_zlo_skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_zlo_max = is_paging-top + 1.
      DESCRIBE TABLE lt_zlo_out LINES lv_zlo_zeile.
      IF lv_zlo_zeile >= lv_zlo_max.
        DELETE lt_zlo_out FROM lv_zlo_max.
      ENDIF.
    ENDIF.

    copy_data_to_ref( EXPORTING is_data = lt_zlo_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.
* ===== Ende DPC ========================================================
