*&---------------------------------------------------------------------*
*& Einkauf: EinkKontraktSet (offene Mengenkontrakte wie ME3L) und EinkMatLzSet
*& (Lebenszyklus- und Sortiments-Code je Material) im Service ZPOWERBI_EINKAUF_SRV
*& Stand    : 2026-10-08, erste Fassung (T76). Wunsch Armin (Gespraech 2026-10-08).
*& Warum    : EKPOSet liefert fuer Kontraktpositionen keinen Preis (NETWR 0), keine Preiseinheit, keinen
*&            Zielwert und keine abgerufene Menge; MARA001Set hat ZZLZCOD, aber nicht ZZLZCODSORT.
*&            Der bisherige Wert "Offener Wert der Kontraktabrufe" (5.2 Mio) sind offene Bestellungen mit
*&            Kontraktbezug, nicht der offene Kontraktwert.
*&
*& DDIC (SE11, Paket ZPP), alle Zahlen als Text (string template, Punkt als Dezimalzeichen, fuehrendes
*& Minus), damit keine Waehrungs-/Mengenreferenzen noetig sind:
*&   ZSTR_EINK_KONTRAKT: EBELN EBELN, EBELP EBELP, BUKRS BUKRS, BSART ESART, LIFNR ELIFN, MATNR MATNR,
*&                       TXZ01 TXZ01, MATKL MATKL, WAERS CHAR5, WKURS CHAR20, KDATB CHAR8, KDATE CHAR8,
*&                       LOEKZ CHAR1, MEINS CHAR3, KTMNG CHAR20, NETPR CHAR20, PEINH CHAR20, ZWERT CHAR20,
*&                       ABMNG CHAR20, ABWRT CHAR20
*&   ZSTR_EINK_MATLZ   : MATNR MATNR, LZCODE ZZLZCOD, LZSORT ZZLZCODSORT
*&
*& Abgerufen = Summe EKAB (Abrufdokumentation) je Kontrakt/Position, ohne geloeschte Abrufe.
*& Alle Kontrakte (BSTYP K), auch abgelaufene; geloeschte Positionen/Koepfe nicht.
*&---------------------------------------------------------------------*

* ===== MPC_EXT DEFINE: vor dem ShopZd05-Block einfuegen =================
* ---------------------------------------------------------------------
* EinkKontrakt: EinkKontraktSet, optionaler Filter Bukrs
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'EinkKontrakt'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Ebeln' iv_abap_fieldname = 'EBELN' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Ebelp' iv_abap_fieldname = 'EBELP' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Bukrs' iv_abap_fieldname = 'BUKRS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Bsart' iv_abap_fieldname = 'BSART' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Lifnr' iv_abap_fieldname = 'LIFNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Txz01' iv_abap_fieldname = 'TXZ01' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matkl' iv_abap_fieldname = 'MATKL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 9 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Waers' iv_abap_fieldname = 'WAERS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Wkurs' iv_abap_fieldname = 'WKURS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kdatb' iv_abap_fieldname = 'KDATB' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kdate' iv_abap_fieldname = 'KDATE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Loekz' iv_abap_fieldname = 'LOEKZ' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Meins' iv_abap_fieldname = 'MEINS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Ktmng' iv_abap_fieldname = 'KTMNG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Netpr' iv_abap_fieldname = 'NETPR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Peinh' iv_abap_fieldname = 'PEINH' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Zwert' iv_abap_fieldname = 'ZWERT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Abmng' iv_abap_fieldname = 'ABMNG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Abwrt' iv_abap_fieldname = 'ABWRT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_EINK_KONTRAKT' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'EinkKontraktSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ---------------------------------------------------------------------
* EinkMatLz: EinkMatLzSet (Lebenszyklus- und Sortiments-Code je Material)
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'EinkMatLz'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Lzcode' iv_abap_fieldname = 'LZCODE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Lzsort' iv_abap_fieldname = 'LZSORT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_EINK_MATLZ' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'EinkMatLzSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ===== DPC_EXT GET_ENTITYSET: vor "* Operations (Shopfloor)" einfuegen =====
  IF iv_entity_set_name = 'EinkKontraktSet'.
    TYPES: BEGIN OF ty_ek_pos,
             ebeln TYPE ekko-ebeln,
             bukrs TYPE ekko-bukrs,
             bsart TYPE ekko-bsart,
             lifnr TYPE ekko-lifnr,
             waers TYPE ekko-waers,
             wkurs TYPE ekko-wkurs,
             kdatb TYPE ekko-kdatb,
             kdate TYPE ekko-kdate,
             ebelp TYPE ekpo-ebelp,
             matnr TYPE ekpo-matnr,
             txz01 TYPE ekpo-txz01,
             matkl TYPE ekpo-matkl,
             meins TYPE ekpo-meins,
             ktmng TYPE ekpo-ktmng,
             netpr TYPE ekpo-netpr,
             peinh TYPE ekpo-peinh,
             zwert TYPE ekpo-zwert,
           END OF ty_ek_pos.
    TYPES: BEGIN OF ty_ek_abr,
             ebeln TYPE ekab-ebeln,
             ebelp TYPE ekab-ebelp,
             konnr TYPE ekab-konnr,
             ktpnr TYPE ekab-ktpnr,
             menge TYPE ekab-menge,
             netwr TYPE ekab-netwr,
           END OF ty_ek_abr.
    TYPES: BEGIN OF ty_ek_sum,
             konnr TYPE ekab-konnr,
             ktpnr TYPE ekab-ktpnr,
             menge TYPE ekab-menge,
             netwr TYPE ekab-netwr,
           END OF ty_ek_sum.
    DATA: lt_ek_out   TYPE STANDARD TABLE OF zstr_eink_kontrakt,
          ls_ek_out   TYPE zstr_eink_kontrakt,
          lt_ek_pos   TYPE STANDARD TABLE OF ty_ek_pos,
          ls_ek_pos   TYPE ty_ek_pos,
          lt_ek_abr   TYPE STANDARD TABLE OF ty_ek_abr,
          ls_ek_abr   TYPE ty_ek_abr,
          lt_ek_sum   TYPE HASHED TABLE OF ty_ek_sum WITH UNIQUE KEY konnr ktpnr,
          ls_ek_sum   TYPE ty_ek_sum,
          lr_ek_bukrs TYPE RANGE OF bukrs,
          ls_ek_bukrs LIKE LINE OF lr_ek_bukrs,
          lv_ek_max   TYPE i.
    FIELD-SYMBOLS: <ls_ek_f> LIKE LINE OF it_filter_select_options,
                   <ls_ek_o> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_ek_f>.
      LOOP AT <ls_ek_f>-select_options ASSIGNING <ls_ek_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_ek_f>-property ).
          WHEN 'BUKRS'.
            ls_ek_bukrs-sign = 'I'.
            ls_ek_bukrs-option = 'EQ'.
            ls_ek_bukrs-low = <ls_ek_o>-low.
            APPEND ls_ek_bukrs TO lr_ek_bukrs.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

    SELECT k~ebeln k~bukrs k~bsart k~lifnr k~waers k~wkurs k~kdatb k~kdate
           p~ebelp p~matnr p~txz01 p~matkl p~meins p~ktmng p~netpr p~peinh p~zwert
      FROM ekko AS k INNER JOIN ekpo AS p ON p~ebeln = k~ebeln
      INTO TABLE lt_ek_pos UP TO 20000 ROWS
      WHERE k~bstyp = 'K'
        AND k~loekz = space
        AND p~loekz = space
        AND k~bukrs IN lr_ek_bukrs.

    IF lt_ek_pos IS NOT INITIAL.
*     Abrufbeleg (EBELN/EBELP) mitlesen: FOR ALL ENTRIES entfernt sonst gleiche Zeilen und zaehlt zu wenig.
      SELECT ebeln ebelp konnr ktpnr menge netwr FROM ekab INTO TABLE lt_ek_abr
        FOR ALL ENTRIES IN lt_ek_pos
        WHERE konnr = lt_ek_pos-ebeln
          AND ktpnr = lt_ek_pos-ebelp
          AND loekz = space.
      LOOP AT lt_ek_abr INTO ls_ek_abr.
        ls_ek_sum-konnr = ls_ek_abr-konnr.
        ls_ek_sum-ktpnr = ls_ek_abr-ktpnr.
        ls_ek_sum-menge = ls_ek_abr-menge.
        ls_ek_sum-netwr = ls_ek_abr-netwr.
        COLLECT ls_ek_sum INTO lt_ek_sum.
      ENDLOOP.
    ENDIF.

    LOOP AT lt_ek_pos INTO ls_ek_pos.
      CLEAR ls_ek_out.
      ls_ek_out-ebeln = ls_ek_pos-ebeln.
      ls_ek_out-ebelp = ls_ek_pos-ebelp.
      ls_ek_out-bukrs = ls_ek_pos-bukrs.
      ls_ek_out-bsart = ls_ek_pos-bsart.
      ls_ek_out-lifnr = ls_ek_pos-lifnr.
      ls_ek_out-matnr = ls_ek_pos-matnr.
      ls_ek_out-txz01 = ls_ek_pos-txz01.
      ls_ek_out-matkl = ls_ek_pos-matkl.
      ls_ek_out-waers = ls_ek_pos-waers.
      ls_ek_out-wkurs = |{ ls_ek_pos-wkurs }|.
      ls_ek_out-kdatb = ls_ek_pos-kdatb.
      ls_ek_out-kdate = ls_ek_pos-kdate.
      ls_ek_out-meins = ls_ek_pos-meins.
      ls_ek_out-ktmng = |{ ls_ek_pos-ktmng }|.
      ls_ek_out-netpr = |{ ls_ek_pos-netpr }|.
      ls_ek_out-peinh = |{ ls_ek_pos-peinh }|.
      ls_ek_out-zwert = |{ ls_ek_pos-zwert }|.
      CLEAR ls_ek_sum.
      READ TABLE lt_ek_sum INTO ls_ek_sum WITH TABLE KEY konnr = ls_ek_pos-ebeln ktpnr = ls_ek_pos-ebelp.
      ls_ek_out-abmng = |{ ls_ek_sum-menge }|.
      ls_ek_out-abwrt = |{ ls_ek_sum-netwr }|.
      APPEND ls_ek_out TO lt_ek_out.
    ENDLOOP.

    SORT lt_ek_out BY ebeln ebelp.
    IF is_paging-skip > 0.
      DELETE lt_ek_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_ek_max = is_paging-top + 1.
      DELETE lt_ek_out FROM lv_ek_max.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_ek_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

  IF iv_entity_set_name = 'EinkMatLzSet'.
    DATA: lt_lz_out TYPE STANDARD TABLE OF zstr_eink_matlz,
          lv_lz_max TYPE i.
    SELECT matnr zzlzcod zzlzcodsort FROM mara INTO TABLE lt_lz_out
      WHERE zzlzcod <> space OR zzlzcodsort <> space.
    SORT lt_lz_out BY matnr.
    IF is_paging-skip > 0.
      DELETE lt_lz_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_lz_max = is_paging-top + 1.
      DELETE lt_lz_out FROM lv_lz_max.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_lz_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.
