*&---------------------------------------------------------------------*
*& Shopfloor Verkauf (Mail Fabio 09.11., Punkt d): AE und Umsatz IST aus CO-PA (CE11000)
*& CoPaSumSet im Service ZPOWERBI_EINKAUF_SRV
*& Stand    : 2026-11-10, erste Fassung (T76), Auftrag Ingo
*& Zweck    : Monatssummen je Satzart (A Auftragseingang, F Faktura) und Produktsparte WWPSP,
*&            Buchungskreis/Jahr/Periode als Pflichtfilter, Netto wie KE30-Bericht Z0001-000006:
*&            VV100 + VV110 - VV200 - VV210 - VV220 - VV250 - VV230 - VV240 (Skonto kalk.).
*&            Beleg P76 Periode 009.2026, Bukrs 1100: F 7'878'114.69, A 6'531'197.94 = Fabios
*&            Kontrollwerte (docs/SHOPFLOOR_2026-10-07.md Abschnitt 20).
*& Muster   : QmMeldungSet (eigener Zweig in GET_ENTITYSET vor der FinanzJournal-Weiche,
*&            Entity in MPC_EXT DEFINE, bind_conversions ' ').
*&
*& DDIC (SE11, Paket ZPP):
*&   ZSTR_COPA_SUM: BUKRS CHAR4, GJAHR CHAR4, PERDE CHAR3, VRGAR CHAR1, WWPSP CHAR10,
*&                  NETTO /SCMTMS/DECIMAL_VALUE, VV240 /SCMTMS/DECIMAL_VALUE, ANZAHL INT4
*&---------------------------------------------------------------------*

* ===== MPC_EXT DEFINE: am Ende, direkt vor ENDMETHOD einfuegen =========
* ---------------------------------------------------------------------
* CoPaSum: CoPaSumSet (CO-PA Monatssummen, Shopfloor Verkauf), Pflichtfilter Bukrs/Gjahr/Perde
* saptasks/copa_verkauf/ZCOPA_SUM_ADD.abap
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'CoPaSum'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Bukrs' iv_abap_fieldname = 'BUKRS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Gjahr' iv_abap_fieldname = 'GJAHR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Perde' iv_abap_fieldname = 'PERDE' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Vrgar' iv_abap_fieldname = 'VRGAR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Wwpsp' iv_abap_fieldname = 'WWPSP' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Netto' iv_abap_fieldname = 'NETTO' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 6 ).
  lo_property->set_maxlength( iv_max_length = 28 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Vv240' iv_abap_fieldname = 'VV240' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 6 ).
  lo_property->set_maxlength( iv_max_length = 28 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Anzahl' iv_abap_fieldname = 'ANZAHL' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_COPA_SUM' iv_bind_conversions = ' ' ).
  lo_entity_set = lo_entity_type->create_entity_set( 'CoPaSumSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
* ===== Ende MPC ========================================================

* ===== DPC_EXT GET_ENTITYSET: nach dem QmMeldungSet-Block, VOR "IF iv_entity_set_name <> 'FinanzJournalSet'." =====
* Shopfloor Verkauf AE / Umsatz IST aus CE11000, saptasks/copa_verkauf/ZCOPA_SUM_ADD.abap

  IF iv_entity_set_name = 'CoPaSumSet'.

    TYPES: BEGIN OF ty_cp_sum,
             vrgar  TYPE ce11000-vrgar,
             wwpsp  TYPE ce11000-wwpsp,
             vv100  TYPE ce11000-vv100,
             vv110  TYPE ce11000-vv110,
             vv200  TYPE ce11000-vv200,
             vv210  TYPE ce11000-vv210,
             vv220  TYPE ce11000-vv220,
             vv230  TYPE ce11000-vv230,
             vv240  TYPE ce11000-vv240,
             vv250  TYPE ce11000-vv250,
             anzahl TYPE i,
           END OF ty_cp_sum.

    DATA: lt_cp_sum   TYPE STANDARD TABLE OF ty_cp_sum,
          ls_cp_sum   TYPE ty_cp_sum,
          lt_cp_out   TYPE STANDARD TABLE OF zstr_copa_sum,
          ls_cp_out   TYPE zstr_copa_sum,
          lv_cp_bukrs TYPE ce11000-bukrs,
          lv_cp_gjahr TYPE ce11000-gjahr,
          lv_cp_perde TYPE ce11000-perde,
          lv_cp_vrgar TYPE ce11000-vrgar,
          lv_cp_msg   TYPE bapi_msg.

    FIELD-SYMBOLS: <ls_cp_f> LIKE LINE OF it_filter_select_options,
                   <ls_cp_o> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_cp_f>.
      LOOP AT <ls_cp_f>-select_options ASSIGNING <ls_cp_o>
           WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_cp_f>-property ).
          WHEN 'BUKRS'.
            lv_cp_bukrs = <ls_cp_o>-low.
          WHEN 'GJAHR'.
            lv_cp_gjahr = <ls_cp_o>-low.
          WHEN 'PERDE'.
            lv_cp_perde = <ls_cp_o>-low.
          WHEN 'VRGAR'.
            lv_cp_vrgar = <ls_cp_o>-low.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

    IF lv_cp_bukrs IS INITIAL OR lv_cp_gjahr IS INITIAL OR lv_cp_perde IS INITIAL.
      lv_cp_msg = 'CoPaSumSet braucht Bukrs, Gjahr und Perde (z. B. 1100, 2026, 009).'.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = lv_cp_msg.
    ENDIF.

*   Waehrungstyp 01 (Buchungskreiswaehrung), nur Auftragseingang und Faktura
    IF lv_cp_vrgar IS INITIAL.
      SELECT vrgar wwpsp SUM( vv100 ) SUM( vv110 ) SUM( vv200 ) SUM( vv210 )
             SUM( vv220 ) SUM( vv230 ) SUM( vv240 ) SUM( vv250 ) COUNT( * )
        FROM ce11000
        INTO TABLE lt_cp_sum
        WHERE paledger = '01'
          AND bukrs    = lv_cp_bukrs
          AND gjahr    = lv_cp_gjahr
          AND perde    = lv_cp_perde
          AND ( vrgar = 'A' OR vrgar = 'F' )
        GROUP BY vrgar wwpsp.
    ELSE.
      SELECT vrgar wwpsp SUM( vv100 ) SUM( vv110 ) SUM( vv200 ) SUM( vv210 )
             SUM( vv220 ) SUM( vv230 ) SUM( vv240 ) SUM( vv250 ) COUNT( * )
        FROM ce11000
        INTO TABLE lt_cp_sum
        WHERE paledger = '01'
          AND bukrs    = lv_cp_bukrs
          AND gjahr    = lv_cp_gjahr
          AND perde    = lv_cp_perde
          AND vrgar    = lv_cp_vrgar
        GROUP BY vrgar wwpsp.
    ENDIF.

    LOOP AT lt_cp_sum INTO ls_cp_sum.
      CLEAR ls_cp_out.
      ls_cp_out-bukrs  = lv_cp_bukrs.
      ls_cp_out-gjahr  = lv_cp_gjahr.
      ls_cp_out-perde  = lv_cp_perde.
      ls_cp_out-vrgar  = ls_cp_sum-vrgar.
      ls_cp_out-wwpsp  = ls_cp_sum-wwpsp.
      ls_cp_out-netto  = ls_cp_sum-vv100 + ls_cp_sum-vv110 - ls_cp_sum-vv200 - ls_cp_sum-vv210
                       - ls_cp_sum-vv220 - ls_cp_sum-vv250 - ls_cp_sum-vv230 - ls_cp_sum-vv240.
      ls_cp_out-vv240  = ls_cp_sum-vv240.
      ls_cp_out-anzahl = ls_cp_sum-anzahl.
      APPEND ls_cp_out TO lt_cp_out.
    ENDLOOP.

    SORT lt_cp_out BY vrgar wwpsp.
    copy_data_to_ref( EXPORTING is_data = lt_cp_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.
* ===== Ende DPC ========================================================
