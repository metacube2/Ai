*&---------------------------------------------------------------------*
*& Operations (Shopfloor): ShopZd05Set und ShopAufSet im Service ZPOWERBI_EINKAUF_SRV
*& Stand    : 2026-10-07, erste Fassung (T76).
*& Zweck    : Was das PPA-Shopfloor-Excel heute von Hand holt, kommt aus SAP:
*&            - ShopZd05Set = Report ZD05 (ZMM_CHECK_UMTERMINIERUNG) ohne ALV, je Werk und Disponent
*&            - ShopAufSet  = offene Plan- und Fertigungsauftraege je Eckendtermin (Forecast statt COOIS-Export)
*& Muster   : wie LogKapSet (docs/abap/ZLOG_KAP_ADD.abap): nur lesend, Pflichtfilter, Obergrenzen.
*&
*& DDIC (SE11, Paket ZPP):
*&   ZSTR_SHOP_ZD05: WERKS WERKS_D, MATNR MATNR, DISPO DISPO, MAKTX MAKTX, UDEK CHAR8, DISMM DISMM,
*&                   LZCODE ZZLZCOD, VERBR_WBZ INT4, SIBE_OPT INT4, SIBE_AKT INT4, ANT_PLAN INT4,
*&                   TENDENZ CHAR1 (G/Y/R wie die Ampel in ZD05)
*&   ZSTR_SHOP_AUF : WERKS WERKS_D, AUFNR AUFNR, TYP CHAR2 (PA/FE), MATNR MATNR, DISPO DISPO,
*&                   DATUM CHAR8 (Eckendtermin), MENGE INT4 (offene Menge), MEINS MEINS
*&
*& Fachlogik ShopZd05Set = ZMM_CHECK_UMTERMINIERUNG Forms READ_MATERIAL, CHECK_UNTERDECKUNG,
*& ANREICHERN_ITAB, VERBRAUCH (gelesen per RFC 2026-10-07): MARC Werk + Disponent + DISGR '0011';
*& Bestand MBEW-LBKUM; Abgaenge RESB (Rest) und VBBE bis heute + Planlieferzeit; Zugaenge offene
*& EKET; erstes Datum mit negativem Bestand = Unterdeckung, nur wenn danach noch ein Zugang kommt
*& (= Umterminierung moeglich); Verbrauch 12 Monate (VERBRAUCH_LESEN), Verbrauch in WBZ =
*& Verbrauch x 2 x PLIFZ / 365; opt. SiBe = Verbrauch WBZ - Bedarf in WBZ (min. 0); Anteil Plan in %.
*&
*& SCHUTZ FUER P76: Pflichtfilter Werks und Dispo (ZD05) bzw. Werks und Datum (Auf), sonst leer;
*& ZD05 hoechstens 4000 Materialien je Aufruf; Auf hoechstens 60 Tage und 20000 Zeilen.
*&---------------------------------------------------------------------*

* ===== MPC_EXT DEFINE: vor dem LogKap-Block einfuegen =================
* ---------------------------------------------------------------------
* ShopZd05: ShopZd05Set, Pflichtfilter Werks und Dispo
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'ShopZd05'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Dispo' iv_abap_fieldname = 'DISPO' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Maktx' iv_abap_fieldname = 'MAKTX' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Udek' iv_abap_fieldname = 'UDEK' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Dismm' iv_abap_fieldname = 'DISMM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Lzcode' iv_abap_fieldname = 'LZCODE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'VerbrWbz' iv_abap_fieldname = 'VERBR_WBZ' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'SibeOpt' iv_abap_fieldname = 'SIBE_OPT' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'SibeAkt' iv_abap_fieldname = 'SIBE_AKT' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'AntPlan' iv_abap_fieldname = 'ANT_PLAN' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Tendenz' iv_abap_fieldname = 'TENDENZ' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_SHOP_ZD05' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'ShopZd05Set' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ---------------------------------------------------------------------
* ShopAuf: ShopAufSet, Pflichtfilter Werks und Datum, Tage 1..60
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'ShopAuf'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Aufnr' iv_abap_fieldname = 'AUFNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 12 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Typ' iv_abap_fieldname = 'TYP' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Datum' iv_abap_fieldname = 'DATUM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Dispo' iv_abap_fieldname = 'DISPO' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
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

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_SHOP_AUF' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'ShopAufSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ===== DPC_EXT GET_ENTITYSET: vor "IF iv_entity_set_name = 'LogKapSet'" einfuegen =====
  IF iv_entity_set_name = 'ShopZd05Set'.
    TYPES: BEGIN OF ty_sh_bd,
             bdter TYPE d,
             plumi TYPE c LENGTH 1,
             bdmng TYPE p LENGTH 13 DECIMALS 3,
           END OF ty_sh_bd.
    DATA: lt_sh_out   TYPE STANDARD TABLE OF zstr_shop_zd05,
          ls_sh_out   TYPE zstr_shop_zd05,
          lv_sh_werks TYPE werks_d,
          lv_sh_dispo TYPE dispo,
          lt_sh_marc  TYPE STANDARD TABLE OF marc,
          ls_sh_marc  TYPE marc,
          lt_sh_bd    TYPE STANDARD TABLE OF ty_sh_bd,
          ls_sh_bd    TYPE ty_sh_bd,
          lt_sh_resb  TYPE STANDARD TABLE OF resb,
          ls_sh_resb  TYPE resb,
          lt_sh_vbbe  TYPE STANDARD TABLE OF vbbe,
          ls_sh_vbbe  TYPE vbbe,
          lt_sh_ekpo  TYPE STANDARD TABLE OF ekpo,
          ls_sh_ekpo  TYPE ekpo,
          lt_sh_eket  TYPE STANDARD TABLE OF eket,
          ls_sh_eket  TYPE eket,
          lt_sh_verb  TYPE STANDARD TABLE OF iverb,
          ls_sh_verb  TYPE iverb,
          lv_sh_lbkum TYPE mbew-lbkum,
          lv_sh_ende  TYPE d,
          lv_sh_udek  TYPE d,
          lv_sh_umt   TYPE abap_bool,
          lv_sh_von   TYPE d,
          lv_sh_bis   TYPE d,
          lv_sh_verbr TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_q1    TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_q2    TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_q3    TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_q4    TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_vbwbz TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_opt   TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_ant   TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_idx   TYPE i,
          lv_sh_skip  TYPE i,
          lv_sh_max   TYPE i.
    FIELD-SYMBOLS: <ls_sh_f> LIKE LINE OF it_filter_select_options,
                   <ls_sh_o> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_sh_f>.
      LOOP AT <ls_sh_f>-select_options ASSIGNING <ls_sh_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_sh_f>-property ).
          WHEN 'WERKS'. lv_sh_werks = <ls_sh_o>-low.
          WHEN 'DISPO'. lv_sh_dispo = <ls_sh_o>-low.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.
    IF lv_sh_werks IS INITIAL OR lv_sh_dispo IS INITIAL.
      copy_data_to_ref( EXPORTING is_data = lt_sh_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.

*   Verbrauchszeitraum wie ZD05: Ende Vormonat, 360 Tage zurueck, auf Monatsanfang
    lv_sh_bis = sy-datum.
    WHILE lv_sh_bis+4(2) = sy-datum+4(2).
      lv_sh_bis = lv_sh_bis - 1.
    ENDWHILE.
    lv_sh_von = lv_sh_bis - 360.
    lv_sh_von+6(2) = '01'.

    SELECT * FROM marc INTO TABLE lt_sh_marc UP TO 4000 ROWS
      WHERE werks = lv_sh_werks
        AND dispo = lv_sh_dispo
        AND disgr = '0011'.

    LOOP AT lt_sh_marc INTO ls_sh_marc.
      CLEAR: ls_sh_out, lv_sh_udek, lv_sh_umt.
      lv_sh_ende = sy-datum + ls_sh_marc-plifz.
      SELECT SINGLE lbkum FROM mbew INTO lv_sh_lbkum
        WHERE matnr = ls_sh_marc-matnr AND bwkey = ls_sh_marc-werks AND bwtar = space.
      CHECK sy-subrc = 0.

      REFRESH lt_sh_bd.
      SELECT * FROM resb INTO TABLE lt_sh_resb
        WHERE matnr = ls_sh_marc-matnr AND werks = ls_sh_marc-werks
          AND xloek = space AND kzear = space AND bdter <= lv_sh_ende
          AND schgt = space AND dumps = space AND shkzg = 'H'.
      LOOP AT lt_sh_resb INTO ls_sh_resb.
        ls_sh_bd-bdter = ls_sh_resb-bdter.
        ls_sh_bd-bdmng = ls_sh_resb-bdmng - ls_sh_resb-enmng.
        ls_sh_bd-plumi = '2'.
        APPEND ls_sh_bd TO lt_sh_bd.
      ENDLOOP.
      SELECT * FROM vbbe INTO TABLE lt_sh_vbbe
        WHERE matnr = ls_sh_marc-matnr AND werks = ls_sh_marc-werks
          AND sobkz = space AND mbdat <= lv_sh_ende.
      LOOP AT lt_sh_vbbe INTO ls_sh_vbbe.
        ls_sh_bd-bdter = ls_sh_vbbe-mbdat.
        ls_sh_bd-bdmng = ls_sh_vbbe-omeng.
        ls_sh_bd-plumi = '2'.
        APPEND ls_sh_bd TO lt_sh_bd.
      ENDLOOP.
      SELECT * FROM ekpo INTO TABLE lt_sh_ekpo
        WHERE matnr = ls_sh_marc-matnr AND werks = ls_sh_marc-werks
          AND ( bstyp = 'F' OR bstyp = 'L' ) AND loekz = space AND elikz = space.
      LOOP AT lt_sh_ekpo INTO ls_sh_ekpo.
        SELECT * FROM eket INTO TABLE lt_sh_eket
          WHERE ebeln = ls_sh_ekpo-ebeln AND ebelp = ls_sh_ekpo-ebelp.
        LOOP AT lt_sh_eket INTO ls_sh_eket.
          CHECK ls_sh_eket-wemng < ls_sh_eket-menge.
          ls_sh_bd-bdter = ls_sh_eket-eindt.
          ls_sh_bd-bdmng = ls_sh_eket-menge - ls_sh_eket-wemng.
          ls_sh_bd-plumi = '1'.
          APPEND ls_sh_bd TO lt_sh_bd.
        ENDLOOP.
      ENDLOOP.

      SORT lt_sh_bd BY bdter plumi.
      LOOP AT lt_sh_bd INTO ls_sh_bd.
        IF ls_sh_bd-plumi = '1'.
          lv_sh_lbkum = lv_sh_lbkum + ls_sh_bd-bdmng.
        ELSE.
          lv_sh_lbkum = lv_sh_lbkum - ls_sh_bd-bdmng.
        ENDIF.
        IF lv_sh_lbkum < 0.
          lv_sh_udek = ls_sh_bd-bdter.
          EXIT.
        ENDIF.
      ENDLOOP.
      CHECK lv_sh_udek IS NOT INITIAL.
*     Nur Materialien, bei denen nach der Unterdeckung noch ein Zugang kommt (umterminierbar)
      LOOP AT lt_sh_bd INTO ls_sh_bd WHERE plumi = '1' AND bdter > lv_sh_udek.
        lv_sh_umt = abap_true.
        EXIT.
      ENDLOOP.
      CHECK lv_sh_umt = abap_true.

      ls_sh_out-werks  = ls_sh_marc-werks.
      ls_sh_out-matnr  = ls_sh_marc-matnr.
      ls_sh_out-dispo  = ls_sh_marc-dispo.
      ls_sh_out-dismm  = ls_sh_marc-dismm.
      ls_sh_out-udek   = lv_sh_udek.
      SELECT SINGLE maktx FROM makt INTO ls_sh_out-maktx
        WHERE matnr = ls_sh_marc-matnr AND spras = sy-langu.
      SELECT SINGLE zzlzcod FROM mara INTO ls_sh_out-lzcode
        WHERE matnr = ls_sh_marc-matnr.

      CLEAR: lv_sh_verbr, lv_sh_q1, lv_sh_q2, lv_sh_q3, lv_sh_q4.
      REFRESH lt_sh_verb.
      CALL FUNCTION 'VERBRAUCH_LESEN'
        EXPORTING
          abdatum                  = lv_sh_von
          bisdatum                 = lv_sh_bis
          matnr                    = ls_sh_marc-matnr
          periv                    = ls_sh_marc-periv
          werks                    = ls_sh_marc-werks
          kzgek                    = 'X'
        TABLES
          ges_verb                 = lt_sh_verb
          ges_verb_kor             = lt_sh_verb
          ung_verb                 = lt_sh_verb
          ung_verb_kor             = lt_sh_verb
        EXCEPTIONS
          abdatum_before_range     = 1
          abdatum_greater_bisdatum = 2
          bisdatum_in_future       = 3
          calendar_not_complete    = 4
          consumption_not_found    = 5
          fv_not_found             = 6
          fv_period_error          = 7
          no_material              = 8
          no_plant                 = 9
          OTHERS                   = 10.
      LOOP AT lt_sh_verb INTO ls_sh_verb.
        lv_sh_idx = sy-tabix.
        lv_sh_verbr = lv_sh_verbr + ls_sh_verb-verb1.
        IF lv_sh_idx <= 3.
          lv_sh_q1 = lv_sh_q1 + ls_sh_verb-verb1.
        ELSEIF lv_sh_idx <= 6.
          lv_sh_q2 = lv_sh_q2 + ls_sh_verb-verb1.
        ELSEIF lv_sh_idx <= 9.
          lv_sh_q3 = lv_sh_q3 + ls_sh_verb-verb1.
        ELSEIF lv_sh_idx <= 12.
          lv_sh_q4 = lv_sh_q4 + ls_sh_verb-verb1.
        ENDIF.
      ENDLOOP.

      lv_sh_vbwbz = lv_sh_verbr * ( ls_sh_marc-plifz + ls_sh_marc-plifz ) / 365.
      lv_sh_opt = lv_sh_vbwbz.
      CLEAR lv_sh_ant.
      LOOP AT lt_sh_bd INTO ls_sh_bd WHERE plumi = '2' AND bdter <= lv_sh_ende.
        lv_sh_opt = lv_sh_opt - ls_sh_bd-bdmng.
        lv_sh_ant = lv_sh_ant + ls_sh_bd-bdmng.
      ENDLOOP.
      IF lv_sh_vbwbz > 0.
        lv_sh_ant = lv_sh_ant * 100 / lv_sh_vbwbz.
      ENDIF.
      IF lv_sh_opt < 0.
        lv_sh_opt = 0.
      ENDIF.
      ls_sh_out-verbr_wbz = lv_sh_vbwbz.
      ls_sh_out-sibe_opt  = lv_sh_opt.
      ls_sh_out-sibe_akt  = ls_sh_marc-eisbe.
      ls_sh_out-ant_plan  = lv_sh_ant.

      ls_sh_out-tendenz = 'G'.
      IF lv_sh_q4 < lv_sh_q3 AND lv_sh_q3 < lv_sh_q2 AND lv_sh_q2 < lv_sh_q1.
        ls_sh_out-tendenz = 'R'.
      ELSEIF lv_sh_q3 + lv_sh_q4 < lv_sh_q1 + lv_sh_q2.
        ls_sh_out-tendenz = 'Y'.
      ENDIF.
      APPEND ls_sh_out TO lt_sh_out.
    ENDLOOP.

    SORT lt_sh_out BY matnr.
    IF is_paging-skip > 0.
      DELETE lt_sh_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_sh_max = is_paging-top + 1.
      DELETE lt_sh_out FROM lv_sh_max.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_sh_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

  IF iv_entity_set_name = 'ShopAufSet'.
    DATA: lt_sa_out   TYPE STANDARD TABLE OF zstr_shop_auf,
          ls_sa_out   TYPE zstr_shop_auf,
          lv_sa_werks TYPE werks_d,
          lv_sa_datum TYPE d,
          lv_sa_bis   TYPE d,
          lv_sa_max   TYPE i,
          lt_sa_plaf  TYPE STANDARD TABLE OF plaf,
          ls_sa_plaf  TYPE plaf.
    TYPES: BEGIN OF ty_sa_fa,
             aufnr TYPE afko-aufnr,
             gltrp TYPE afko-gltrp,
             dispo TYPE afko-dispo,
             matnr TYPE afpo-matnr,
             psmng TYPE afpo-psmng,
             wemng TYPE afpo-wemng,
             meins TYPE afpo-meins,
           END OF ty_sa_fa.
    DATA: lt_sa_fa TYPE STANDARD TABLE OF ty_sa_fa,
          ls_sa_fa TYPE ty_sa_fa.
    FIELD-SYMBOLS: <ls_sa_f> LIKE LINE OF it_filter_select_options,
                   <ls_sa_o> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_sa_f>.
      LOOP AT <ls_sa_f>-select_options ASSIGNING <ls_sa_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_sa_f>-property ).
          WHEN 'WERKS'. lv_sa_werks = <ls_sa_o>-low.
          WHEN 'DATUM'. lv_sa_datum = <ls_sa_o>-low.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.
    lv_sa_bis = sy-datum - 60.
    lv_sa_max = 0.
    IF lv_sa_werks IS INITIAL OR lv_sa_datum IS INITIAL OR lv_sa_datum < lv_sa_bis.
      lv_sa_max = 1.
    ENDIF.
    lv_sa_bis = sy-datum + 120.
    IF lv_sa_datum > lv_sa_bis.
      lv_sa_max = 1.
    ENDIF.
    IF lv_sa_max = 1.
      copy_data_to_ref( EXPORTING is_data = lt_sa_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.
    lv_sa_bis = lv_sa_datum + 60.

*   Planauftraege Eigenfertigung (wie ZM_OFFENE_FAUF: BESKZ <> 'F'), Eckendtermin PEDTR
    SELECT * FROM plaf INTO TABLE lt_sa_plaf UP TO 20000 ROWS
      WHERE plwrk = lv_sa_werks
        AND pedtr BETWEEN lv_sa_datum AND lv_sa_bis
        AND beskz <> 'F'.
    LOOP AT lt_sa_plaf INTO ls_sa_plaf.
      CLEAR ls_sa_out.
      ls_sa_out-werks = ls_sa_plaf-plwrk.
      ls_sa_out-aufnr = ls_sa_plaf-plnum.
      ls_sa_out-typ   = 'PA'.
      ls_sa_out-matnr = ls_sa_plaf-matnr.
      ls_sa_out-dispo = ls_sa_plaf-dispo.
      ls_sa_out-datum = ls_sa_plaf-pedtr.
      ls_sa_out-menge = ls_sa_plaf-gsmng.
      ls_sa_out-meins = ls_sa_plaf-meins.
      APPEND ls_sa_out TO lt_sa_out.
    ENDLOOP.

*   Fertigungsauftraege offen (ohne Endlieferung, nicht geloescht), Eckendtermin GLTRP
    SELECT k~aufnr k~gltrp k~dispo p~matnr p~psmng p~wemng p~meins
      FROM afko AS k INNER JOIN afpo AS p ON p~aufnr = k~aufnr
      INTO TABLE lt_sa_fa UP TO 20000 ROWS
      WHERE p~dwerk = lv_sa_werks
        AND k~gltrp BETWEEN lv_sa_datum AND lv_sa_bis
        AND p~elikz = space
        AND p~dnrel = space.
    LOOP AT lt_sa_fa INTO ls_sa_fa.
      CHECK ls_sa_fa-psmng > ls_sa_fa-wemng.
      CLEAR ls_sa_out.
      ls_sa_out-werks = lv_sa_werks.
      ls_sa_out-aufnr = ls_sa_fa-aufnr.
      ls_sa_out-typ   = 'FE'.
      ls_sa_out-matnr = ls_sa_fa-matnr.
      ls_sa_out-dispo = ls_sa_fa-dispo.
      ls_sa_out-datum = ls_sa_fa-gltrp.
      ls_sa_out-menge = ls_sa_fa-psmng - ls_sa_fa-wemng.
      ls_sa_out-meins = ls_sa_fa-meins.
      APPEND ls_sa_out TO lt_sa_out.
    ENDLOOP.

    SORT lt_sa_out BY datum aufnr.
    IF is_paging-skip > 0.
      DELETE lt_sa_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_sa_max = is_paging-top + 1.
      DELETE lt_sa_out FROM lv_sa_max.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_sa_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.
