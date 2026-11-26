* Shopfloor Welle 2 (Nenner MATDOC 201/261, VF/WE-Termine und Auftragsdaten je Kundenauftragsposition)
* Quelle: saptasks/shopfloor_sap/PLAN_WELLE2_2026-11-25.md Abschnitt 11.3 (MPC_EXT->DEFINE) und 11.4 (DPC_EXT->GET_ENTITYSET)
* Erzeugt mit saptasks/shopfloor_sap/methoden_bauen_welle2_2026-11-26.py

* ===== MPC_EXT DEFINE: vor der Trennlinie ueber '* LogKap: LogKapSet ...' einfuegen =====
* ---------------------------------------------------------------------
* ShopMatBezug: ShopMatBezugSet, Pflichtfilter Werks, Von, Bis (Shopfloor Welle 2, Nenner 201/261)
* saptasks/shopfloor_sap/PLAN_WELLE2_2026-11-25.md Abschnitt 11
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'ShopMatBezug'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Von' iv_abap_fieldname = 'VON' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Bis' iv_abap_fieldname = 'BIS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Anzahl' iv_abap_fieldname = 'ANZAHL' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Zeilen' iv_abap_fieldname = 'ZEILEN' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_SHOP_MATBEZ' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'ShopMatBezugSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ---------------------------------------------------------------------
* ShopKdTermin: ShopKdTerminSet, Pflichtfilter Werks, optional Vbeln und Posnr (Shopfloor Welle 2, VF/WE, Punkt 9)
* saptasks/shopfloor_sap/PLAN_WELLE2_2026-11-25.md Abschnitt 11
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'ShopKdTermin'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Vbeln' iv_abap_fieldname = 'VBELN' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Posnr' iv_abap_fieldname = 'POSNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Typ' iv_abap_fieldname = 'TYP' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Aufnr' iv_abap_fieldname = 'AUFNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 12 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Menge' iv_abap_fieldname = 'MENGE' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Meins' iv_abap_fieldname = 'MEINS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Wunsch' iv_abap_fieldname = 'WUNSCH' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Bereit' iv_abap_fieldname = 'BEREIT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Liefer' iv_abap_fieldname = 'LIEFER' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Bmeng' iv_abap_fieldname = 'BMENG' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Ende' iv_abap_fieldname = 'ENDE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Webaz' iv_abap_fieldname = 'WEBAZ' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Verfueg' iv_abap_fieldname = 'VERFUEG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Beskz' iv_abap_fieldname = 'BESKZ' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Zztyp' iv_abap_fieldname = 'ZZTYP' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 60 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kwmeng' iv_abap_fieldname = 'KWMENG' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Vrkme' iv_abap_fieldname = 'VRKME' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Sobkz' iv_abap_fieldname = 'SOBKZ' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'BmengLetzte' iv_abap_fieldname = 'BMENG_LETZTE' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Elikz' iv_abap_fieldname = 'ELIKZ' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Dnrel' iv_abap_fieldname = 'DNREL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_SHOP_KDTERM' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'ShopKdTerminSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ===== DPC_EXT GET_ENTITYSET: vor '* Logistik live Teil C, docs/abap/ZLOG_KAP_ADD.abap' einfuegen =====
* Shopfloor Welle 2: Nenner Materialverfuegbarkeit (Fabio 24.11.), saptasks/shopfloor_sap/PLAN_WELLE2_2026-11-25.md Abschnitt 11
  IF iv_entity_set_name = 'ShopMatBezugSet'.
    DATA: lt_mb_out TYPE STANDARD TABLE OF zstr_shop_matbez,
          ls_mb_out TYPE zstr_shop_matbez,
          lv_mb_werks TYPE werks_d, lv_mb_von TYPE d, lv_mb_bis TYPE d,
          lv_mb_anz TYPE i, lv_mb_zei TYPE i.
    FIELD-SYMBOLS: <ls_mb_f> LIKE LINE OF it_filter_select_options,
                   <ls_mb_o> TYPE /iwbep/s_cod_select_option.
    LOOP AT it_filter_select_options ASSIGNING <ls_mb_f>.
      LOOP AT <ls_mb_f>-select_options ASSIGNING <ls_mb_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_mb_f>-property ).
          WHEN 'WERKS'. lv_mb_werks = <ls_mb_o>-low.
          WHEN 'VON'.   lv_mb_von   = <ls_mb_o>-low.
          WHEN 'BIS'.   lv_mb_bis   = <ls_mb_o>-low.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.
*   Schutz: alle drei Pflicht, Bis >= Von, hoechstens 62 Tage (Last; eine KW braucht 6)
    IF lv_mb_werks IS INITIAL OR lv_mb_von IS INITIAL OR lv_mb_bis IS INITIAL
       OR lv_mb_bis < lv_mb_von OR lv_mb_bis - lv_mb_von > 62.
      copy_data_to_ref( EXPORTING is_data = lt_mb_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.
    SELECT COUNT( DISTINCT matnr ) FROM matdoc INTO lv_mb_anz
      WHERE werks = lv_mb_werks AND budat BETWEEN lv_mb_von AND lv_mb_bis
        AND ( bwart = '201' OR bwart = '261' ).
    SELECT COUNT( * ) FROM matdoc INTO lv_mb_zei
      WHERE werks = lv_mb_werks AND budat BETWEEN lv_mb_von AND lv_mb_bis
        AND ( bwart = '201' OR bwart = '261' ).
    ls_mb_out-werks = lv_mb_werks. ls_mb_out-von = lv_mb_von. ls_mb_out-bis = lv_mb_bis.
    ls_mb_out-anzahl = lv_mb_anz.  ls_mb_out-zeilen = lv_mb_zei.
    APPEND ls_mb_out TO lt_mb_out.
    copy_data_to_ref( EXPORTING is_data = lt_mb_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

* Shopfloor Welle 2: VF/WE-Termine und Auftragsdaten je Kundenauftragsposition (Fabio 24.11. Punkt 7, 26.11. Punkt 9)
* Mit Vbeln: Basis VBAP (alle Positionen im Werk, auch ohne offenen Bedarf; keine Zeile = Auftrag im Werk unbekannt).
* Ohne Vbeln: Basis VBBE (offener Bedarf im Werk), nur Positionen mit VBAP (VBBE fuehrt auch Lieferungen, VBTYP J).
* Keine fachlichen Ausschluesse: BESKZ (PA), ELIKZ und DNREL (FA) gehen als Felder mit, das Board filtert.
* saptasks/shopfloor_sap/PLAN_WELLE2_2026-11-25.md Abschnitt 11
  IF iv_entity_set_name = 'ShopKdTerminSet'.
    TYPES: BEGIN OF ty_kt_pos,
             vbeln TYPE vbap-vbeln,
             posnr TYPE vbap-posnr,
           END OF ty_kt_pos,
           BEGIN OF ty_kt_vbap,
             vbeln  TYPE vbap-vbeln,
             posnr  TYPE vbap-posnr,
             matnr  TYPE vbap-matnr,
             kwmeng TYPE vbap-kwmeng,
             vrkme  TYPE vbap-vrkme,
             meins  TYPE vbap-meins,
             sobkz  TYPE vbap-sobkz,
           END OF ty_kt_vbap,
           BEGIN OF ty_kt_vbbe,
             vbeln TYPE vbbe-vbeln,
             posnr TYPE vbbe-posnr,
             etenr TYPE vbbe-etenr,
             mbdat TYPE vbbe-mbdat,
             omeng TYPE vbbe-omeng,
           END OF ty_kt_vbbe,
           BEGIN OF ty_kt_vbep,
             vbeln TYPE vbep-vbeln,
             posnr TYPE vbep-posnr,
             etenr TYPE vbep-etenr,
             edatu TYPE vbep-edatu,
             mbdat TYPE vbep-mbdat,
             bmeng TYPE vbep-bmeng,
           END OF ty_kt_vbep,
           BEGIN OF ty_kt_mara,
             matnr TYPE mara-matnr,
             zztyp TYPE mara-zztyp,
           END OF ty_kt_mara,
           BEGIN OF ty_kt_plaf,
             plnum TYPE plaf-plnum,
             kdauf TYPE plaf-kdauf,
             kdpos TYPE plaf-kdpos,
             matnr TYPE plaf-matnr,
             gsmng TYPE plaf-gsmng,
             meins TYPE plaf-meins,
             pedtr TYPE plaf-pedtr,
             webaz TYPE plaf-webaz,
             beskz TYPE plaf-beskz,
           END OF ty_kt_plaf,
           BEGIN OF ty_kt_fa,
             aufnr TYPE afpo-aufnr,
             posnr TYPE afpo-posnr,
             kdauf TYPE afpo-kdauf,
             kdpos TYPE afpo-kdpos,
             matnr TYPE afpo-matnr,
             psmng TYPE afpo-psmng,
             wemng TYPE afpo-wemng,
             amein TYPE afpo-amein,
             webaz TYPE afpo-webaz,
             elikz TYPE afpo-elikz,
             dnrel TYPE afpo-dnrel,
             gltrp TYPE afko-gltrp,
           END OF ty_kt_fa.
    DATA: lt_kt_out    TYPE STANDARD TABLE OF zstr_shop_kdterm,
          ls_kt_out    TYPE zstr_shop_kdterm,
          lv_kt_werks  TYPE werks_d,
          lv_kt_vbeln  TYPE vbeln_va,
          lr_kt_posnr  TYPE RANGE OF vbap-posnr,
          ls_kt_rpos   LIKE LINE OF lr_kt_posnr,
          lv_kt_max    TYPE i,
          lv_kt_msg    TYPE bapi_msg,
          lv_kt_fabkl  TYPE t001w-fabkl,
          lv_kt_fdat   TYPE scal-facdate,
          lv_kt_datum  TYPE d,
          lv_kt_menge  TYPE vbbe-omeng,
          lt_kt_pos    TYPE STANDARD TABLE OF ty_kt_pos,
          ls_kt_pos    TYPE ty_kt_pos,
          lt_kt_vbap   TYPE SORTED TABLE OF ty_kt_vbap WITH UNIQUE KEY vbeln posnr,
          ls_kt_vbap   TYPE ty_kt_vbap,
          lt_kt_vbbe   TYPE SORTED TABLE OF ty_kt_vbbe WITH NON-UNIQUE KEY vbeln posnr etenr,
          ls_kt_vbbe   TYPE ty_kt_vbbe,
          lt_kt_vbep   TYPE SORTED TABLE OF ty_kt_vbep WITH NON-UNIQUE KEY vbeln posnr edatu etenr,
          ls_kt_vbep   TYPE ty_kt_vbep,
          lt_kt_mat    TYPE STANDARD TABLE OF ty_kt_mara,
          lt_kt_mara   TYPE SORTED TABLE OF ty_kt_mara WITH UNIQUE KEY matnr,
          ls_kt_mara   TYPE ty_kt_mara,
          lt_kt_plaf   TYPE SORTED TABLE OF ty_kt_plaf WITH NON-UNIQUE KEY kdauf kdpos plnum,
          ls_kt_plaf   TYPE ty_kt_plaf,
          lt_kt_fa     TYPE SORTED TABLE OF ty_kt_fa WITH NON-UNIQUE KEY kdauf kdpos aufnr posnr,
          ls_kt_fa     TYPE ty_kt_fa.
    FIELD-SYMBOLS: <ls_kt_f> LIKE LINE OF it_filter_select_options,
                   <ls_kt_o> TYPE /iwbep/s_cod_select_option,
                   <ls_kt_a> TYPE zstr_shop_kdterm.

    LOOP AT it_filter_select_options ASSIGNING <ls_kt_f>.
      LOOP AT <ls_kt_f>-select_options ASSIGNING <ls_kt_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_kt_f>-property ).
          WHEN 'WERKS'.
            lv_kt_werks = <ls_kt_o>-low.
          WHEN 'VBELN'.
            CALL FUNCTION 'CONVERSION_EXIT_ALPHA_INPUT'
              EXPORTING input  = <ls_kt_o>-low
              IMPORTING output = lv_kt_vbeln.
          WHEN 'POSNR'.
            ls_kt_rpos-sign   = 'I'.
            ls_kt_rpos-option = 'EQ'.
            ls_kt_rpos-low    = <ls_kt_o>-low.
            APPEND ls_kt_rpos TO lr_kt_posnr.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.
    IF lv_kt_werks IS INITIAL.
      lv_kt_msg = 'ShopKdTerminSet braucht den Filter Werks (optional Vbeln, Posnr).'.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = lv_kt_msg.
    ENDIF.

*   Positionen: mit Vbeln aus VBAP, ohne Vbeln aus VBBE des Werks (danach nur die mit VBAP)
    IF lv_kt_vbeln IS INITIAL.
      SELECT vbeln posnr etenr mbdat omeng FROM vbbe INTO TABLE lt_kt_vbbe
        WHERE werks = lv_kt_werks.
      LOOP AT lt_kt_vbbe INTO ls_kt_vbbe.
        ls_kt_pos-vbeln = ls_kt_vbbe-vbeln.
        ls_kt_pos-posnr = ls_kt_vbbe-posnr.
        APPEND ls_kt_pos TO lt_kt_pos.
      ENDLOOP.
      SORT lt_kt_pos BY vbeln posnr.
      DELETE ADJACENT DUPLICATES FROM lt_kt_pos COMPARING vbeln posnr.
      IF lt_kt_pos IS NOT INITIAL.
        SELECT vbeln posnr matnr kwmeng vrkme meins sobkz FROM vbap INTO TABLE lt_kt_vbap
          FOR ALL ENTRIES IN lt_kt_pos
          WHERE vbeln = lt_kt_pos-vbeln AND posnr = lt_kt_pos-posnr.
      ENDIF.
    ELSE.
      SELECT vbeln posnr matnr kwmeng vrkme meins sobkz FROM vbap INTO TABLE lt_kt_vbap
        WHERE vbeln = lv_kt_vbeln AND posnr IN lr_kt_posnr AND werks = lv_kt_werks.
    ENDIF.
    CLEAR lt_kt_pos.
    LOOP AT lt_kt_vbap INTO ls_kt_vbap.
      ls_kt_pos-vbeln = ls_kt_vbap-vbeln.
      ls_kt_pos-posnr = ls_kt_vbap-posnr.
      APPEND ls_kt_pos TO lt_kt_pos.
      ls_kt_mara-matnr = ls_kt_vbap-matnr.
      APPEND ls_kt_mara TO lt_kt_mat.
    ENDLOOP.
    IF lt_kt_pos IS NOT INITIAL.
      IF lv_kt_vbeln IS NOT INITIAL.
        SELECT vbeln posnr etenr mbdat omeng FROM vbbe INTO TABLE lt_kt_vbbe
          FOR ALL ENTRIES IN lt_kt_pos
          WHERE vbeln = lt_kt_pos-vbeln AND posnr = lt_kt_pos-posnr.
      ENDIF.
*     FOR ALL ENTRIES: Schluessel (ETENR, PLNUM, AUFNR/POSNR) in der Feldliste, sonst fallen gleiche Zeilen weg
      SELECT vbeln posnr etenr edatu mbdat bmeng FROM vbep INTO TABLE lt_kt_vbep
        FOR ALL ENTRIES IN lt_kt_pos
        WHERE vbeln = lt_kt_pos-vbeln AND posnr = lt_kt_pos-posnr AND bmeng > 0.
      SELECT plnum kdauf kdpos matnr gsmng meins pedtr webaz beskz FROM plaf INTO TABLE lt_kt_plaf
        FOR ALL ENTRIES IN lt_kt_pos
        WHERE kdauf = lt_kt_pos-vbeln AND kdpos = lt_kt_pos-posnr AND plwrk = lv_kt_werks.
      SELECT p~aufnr p~posnr p~kdauf p~kdpos p~matnr p~psmng p~wemng p~amein p~webaz p~elikz p~dnrel k~gltrp
        FROM afpo AS p INNER JOIN afko AS k ON k~aufnr = p~aufnr
        INTO TABLE lt_kt_fa
        FOR ALL ENTRIES IN lt_kt_pos
        WHERE p~kdauf = lt_kt_pos-vbeln AND p~kdpos = lt_kt_pos-posnr AND p~dwerk = lv_kt_werks.
      SORT lt_kt_mat BY matnr.
      DELETE ADJACENT DUPLICATES FROM lt_kt_mat COMPARING matnr.
      SELECT matnr zztyp FROM mara INTO TABLE lt_kt_mara
        FOR ALL ENTRIES IN lt_kt_mat
        WHERE matnr = lt_kt_mat-matnr.
    ENDIF.

    LOOP AT lt_kt_vbap INTO ls_kt_vbap.
      CLEAR ls_kt_out.
      ls_kt_out-werks  = lv_kt_werks.
      ls_kt_out-vbeln  = ls_kt_vbap-vbeln.
      ls_kt_out-posnr  = ls_kt_vbap-posnr.
      ls_kt_out-typ    = 'KD'.
      ls_kt_out-matnr  = ls_kt_vbap-matnr.
      ls_kt_out-meins  = ls_kt_vbap-meins.
      ls_kt_out-kwmeng = ls_kt_vbap-kwmeng.
      ls_kt_out-vrkme  = ls_kt_vbap-vrkme.
      ls_kt_out-sobkz  = ls_kt_vbap-sobkz.
      READ TABLE lt_kt_mara INTO ls_kt_mara WITH TABLE KEY matnr = ls_kt_vbap-matnr.
      IF sy-subrc = 0.
        ls_kt_out-zztyp = ls_kt_mara-zztyp.
      ENDIF.
*     Wunschtermin: erste Einteilung mit offenem Bedarf (OMENG > 0); Menge: Summe OMENG in der Basiseinheit
      CLEAR lv_kt_menge.
      LOOP AT lt_kt_vbbe INTO ls_kt_vbbe WHERE vbeln = ls_kt_vbap-vbeln AND posnr = ls_kt_vbap-posnr.
        IF ls_kt_vbbe-omeng > 0.
          lv_kt_menge = lv_kt_menge + ls_kt_vbbe-omeng.
          IF ls_kt_out-wunsch IS INITIAL.
            ls_kt_out-wunsch = ls_kt_vbbe-mbdat.
          ENDIF.
        ENDIF.
      ENDLOOP.
      ls_kt_out-menge = lv_kt_menge.
*     E1 (Fabio 25.11.): letzte bestaetigte Einteilung nach EDATU, dann ETENR; Bmeng = Summe aller bestaetigten
      CLEAR lv_kt_menge.
      LOOP AT lt_kt_vbep INTO ls_kt_vbep WHERE vbeln = ls_kt_vbap-vbeln AND posnr = ls_kt_vbap-posnr.
        lv_kt_menge = lv_kt_menge + ls_kt_vbep-bmeng.
        ls_kt_out-bereit       = ls_kt_vbep-mbdat.
        ls_kt_out-liefer       = ls_kt_vbep-edatu.
        ls_kt_out-bmeng_letzte = ls_kt_vbep-bmeng.
      ENDLOOP.
      ls_kt_out-bmeng = lv_kt_menge.
      APPEND ls_kt_out TO lt_kt_out.

      LOOP AT lt_kt_plaf INTO ls_kt_plaf WHERE kdauf = ls_kt_vbap-vbeln AND kdpos = ls_kt_vbap-posnr.
        CLEAR ls_kt_out.
        ls_kt_out-werks = lv_kt_werks.
        ls_kt_out-vbeln = ls_kt_vbap-vbeln.
        ls_kt_out-posnr = ls_kt_vbap-posnr.
        ls_kt_out-typ   = 'PA'.
        ls_kt_out-aufnr = ls_kt_plaf-plnum.
        ls_kt_out-matnr = ls_kt_plaf-matnr.
        ls_kt_out-menge = ls_kt_plaf-gsmng.
        ls_kt_out-meins = ls_kt_plaf-meins.
        ls_kt_out-ende  = ls_kt_plaf-pedtr.
        ls_kt_out-webaz = ls_kt_plaf-webaz.
        ls_kt_out-beskz = ls_kt_plaf-beskz.
        APPEND ls_kt_out TO lt_kt_out.
      ENDLOOP.

      LOOP AT lt_kt_fa INTO ls_kt_fa WHERE kdauf = ls_kt_vbap-vbeln AND kdpos = ls_kt_vbap-posnr.
        CLEAR ls_kt_out.
        ls_kt_out-werks = lv_kt_werks.
        ls_kt_out-vbeln = ls_kt_vbap-vbeln.
        ls_kt_out-posnr = ls_kt_vbap-posnr.
        ls_kt_out-typ   = 'FA'.
        ls_kt_out-aufnr = ls_kt_fa-aufnr.
        ls_kt_out-matnr = ls_kt_fa-matnr.
        IF ls_kt_fa-psmng > ls_kt_fa-wemng.
          ls_kt_out-menge = ls_kt_fa-psmng - ls_kt_fa-wemng.
        ENDIF.
        ls_kt_out-meins = ls_kt_fa-amein.
        ls_kt_out-ende  = ls_kt_fa-gltrp.
        ls_kt_out-webaz = ls_kt_fa-webaz.
        ls_kt_out-elikz = ls_kt_fa-elikz.
        ls_kt_out-dnrel = ls_kt_fa-dnrel.
        APPEND ls_kt_out TO lt_kt_out.
      ENDLOOP.
    ENDLOOP.

*   Verfuegbarkeit = Endtermin + WEBAZ Arbeitstage nach Fabrikkalender des Werks (wie MD04); faellt der
*   Endtermin auf einen arbeitsfreien Tag, zaehlt ab dem naechsten Arbeitstag (CORRECT_OPTION '+')
    SELECT SINGLE fabkl FROM t001w INTO lv_kt_fabkl WHERE werks = lv_kt_werks.
    IF lv_kt_fabkl IS NOT INITIAL.
      LOOP AT lt_kt_out ASSIGNING <ls_kt_a> WHERE typ <> 'KD'.
        IF <ls_kt_a>-ende IS INITIAL OR <ls_kt_a>-ende = '00000000'.
          CONTINUE.
        ENDIF.
        lv_kt_datum = <ls_kt_a>-ende.
        CALL FUNCTION 'DATE_CONVERT_TO_FACTORYDATE'
          EXPORTING
            correct_option      = '+'
            date                = lv_kt_datum
            factory_calendar_id = lv_kt_fabkl
          IMPORTING
            factorydate         = lv_kt_fdat
          EXCEPTIONS
            OTHERS              = 1.
        IF sy-subrc <> 0.
          CONTINUE.
        ENDIF.
        lv_kt_fdat = lv_kt_fdat + <ls_kt_a>-webaz.
        CALL FUNCTION 'FACTORYDATE_CONVERT_TO_DATE'
          EXPORTING
            factorydate         = lv_kt_fdat
            factory_calendar_id = lv_kt_fabkl
          IMPORTING
            date                = lv_kt_datum
          EXCEPTIONS
            OTHERS              = 1.
        IF sy-subrc = 0.
          <ls_kt_a>-verfueg = lv_kt_datum.
        ENDIF.
      ENDLOOP.
    ENDIF.

    SORT lt_kt_out BY vbeln posnr typ aufnr.
    IF is_paging-skip > 0.
      DELETE lt_kt_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_kt_max = is_paging-top + 1.
      DELETE lt_kt_out FROM lv_kt_max.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_kt_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

