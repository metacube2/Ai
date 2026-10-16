*&---------------------------------------------------------------------*
*& Operations (Shopfloor): ShopRmSet und ShopProdSet im Service ZPOWERBI_EINKAUF_SRV
*& Stand    : 2026-10-16, erste Fassung (T76), Wunsch Fabio (Mail 16.10., Datenabfragen J5, J8, J11).
*& Zweck    : - ShopRmSet   = Logik von ZC27 (Report ZPP_RM_MENGEN, gelesen in T76 am 2026-10-16) ohne ALV,
*&                            Gutmenge je Arbeitsplatz und Erfassungsdatum (J5 Logistik, J8 TR5)
*&            - ShopProdSet = Fertigungsauftraege je Istendtermin und Disponent: Gesamtmenge und WE-Menge
*&                            (J8 SEH-Tagesmenge wie COOIS /SEH 001, J11 FPY SEH = WE-Menge / Gesamtmenge)
*& Muster   : wie ShopAufSet (docs/abap/ZSHOP_ADD.abap): nur lesend, Pflichtfilter, Obergrenzen.
*&
*& DDIC (SE11, Paket ZPP):
*&   ZSTR_SHOP_RM  : WERKS WERKS_D, ARBPL ARBPL, ERSDA CHAR8, TYPCD CHAR10, LMNGA INT4, ANZAHL INT4
*&   ZSTR_SHOP_PROD: WERKS WERKS_D, DISPO DISPO, DATUM CHAR8, GAMNG INT4, WEMNG INT4, ANZAHL INT4
*&
*& Fachlogik ShopRmSet = ZPP_RM_MENGEN, Forms BILDEN_RANGE und LESEN_AFRU:
*&   ARBID aus CRHD (Werk, ARBPL in der Auswahl); AFRU mit ERSDA in der Auswahl, Werk, ARBID,
*&   STOKZ = ' ' und STZHL = 0; Material ueber AFPO, Typencode MARA-ZZTYP (nur wenn Typcd gefiltert);
*&   es zaehlt nur die Rueckmeldung zum hoechsten Vorgang (AFVC-VORNR) des Auftrags an den GEWAEHLTEN
*&   Arbeitsplaetzen. Die Auswahl der Arbeitsplaetze bestimmt also mit, welcher Vorgang der letzte ist;
*&   ein Aufruf entspricht einem ZC27-Lauf mit genau diesen Arbeitsplaetzen.
*&   Summiert wird die Gutmenge LMNGA (ZC27 zeigt die Einzelzeilen, die Summe bildet der Teamleiter).
*&
*& Filter (alle EQ, mehrfach = ODER):
*&   ShopRmSet  : Werks (Pflicht), Arbpl (Pflicht, 1..30; ZC27-Variante /PPA EXX72 hat 11), Ersda (Pflicht, JJJJMMTT, 1..7 Tage, hoechstens
*&                400 Tage zurueck), Typcd (optional; mit * als Muster, z. B. 8*)
*&   ShopProdSet: Werks (Pflicht), Datum (Pflicht, Istendtermin AFKO-GETRI, 1..7 Tage, hoechstens 400 Tage
*&                zurueck), Dispo (optional; mit * als Muster, z. B. S*)
*&   Ohne Pflichtfilter: HTTP 400 ohne Datenbankzugriff (Dienst ist auch die Power-BI-Quelle).
*&---------------------------------------------------------------------*

* ===== MPC_EXT DEFINE: am Ende, direkt vor ENDMETHOD einfuegen =========
* ---------------------------------------------------------------------
* ShopRm: ShopRmSet (ZC27), Pflichtfilter Werks, Arbpl, Ersda
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'ShopRm'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Arbpl' iv_abap_fieldname = 'ARBPL' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Ersda' iv_abap_fieldname = 'ERSDA' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Typcd' iv_abap_fieldname = 'TYPCD' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Lmnga' iv_abap_fieldname = 'LMNGA' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Anzahl' iv_abap_fieldname = 'ANZAHL' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_SHOP_RM' iv_bind_conversions = ' ' ).
  lo_entity_set = lo_entity_type->create_entity_set( 'ShopRmSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).

* ---------------------------------------------------------------------
* ShopProd: ShopProdSet (Fertigungsauftraege je Istendtermin), Pflichtfilter Werks, Datum
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'ShopProd'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Dispo' iv_abap_fieldname = 'DISPO' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Datum' iv_abap_fieldname = 'DATUM' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Gamng' iv_abap_fieldname = 'GAMNG' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Wemng' iv_abap_fieldname = 'WEMNG' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Anzahl' iv_abap_fieldname = 'ANZAHL' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_SHOP_PROD' iv_bind_conversions = ' ' ).
  lo_entity_set = lo_entity_type->create_entity_set( 'ShopProdSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
* ===== Ende MPC ========================================================

* ===== DPC_EXT GET_ENTITYSET: nach dem Zc23-Block, VOR "IF iv_entity_set_name <> 'FinanzJournalSet'." =====
* Shopfloor J5/J8/J11, docs/abap/ZSHOP_RM_ADD.abap

  IF iv_entity_set_name = 'ShopRmSet'.
    TYPES: BEGIN OF ty_rm_ru,
             aufnr TYPE afru-aufnr,
             aufpl TYPE afru-aufpl,
             vornr TYPE afru-vornr,
             arbid TYPE afru-arbid,
             ersda TYPE afru-ersda,
             lmnga TYPE afru-lmnga,
           END OF ty_rm_ru,
           BEGIN OF ty_rm_vg,
             aufpl TYPE afvc-aufpl,
             vornr TYPE afvc-vornr,
           END OF ty_rm_vg.
    DATA: lt_rm_out   TYPE STANDARD TABLE OF zstr_shop_rm,
          ls_rm_out   TYPE zstr_shop_rm,
          lt_rm_ru    TYPE STANDARD TABLE OF ty_rm_ru,
          ls_rm_ru    TYPE ty_rm_ru,
          lt_rm_vg    TYPE SORTED TABLE OF ty_rm_vg WITH UNIQUE KEY aufpl,
          ls_rm_vg    TYPE ty_rm_vg,
          lt_rm_afvc  TYPE STANDARD TABLE OF ty_rm_vg,
          lv_rm_werks TYPE werks_d,
          lv_rm_matnr TYPE matnr,
          lv_rm_zztyp TYPE mara-zztyp,
          lv_rm_arbpl TYPE arbpl,
          lv_rm_tag   TYPE d,
          lv_rm_alt   TYPE d,
          lv_rm_n     TYPE i,
          lv_rm_m     TYPE i,
          lv_rm_msg   TYPE bapi_msg,
          lr_rm_arbpl TYPE RANGE OF arbpl,
          ls_rm_arbpl LIKE LINE OF lr_rm_arbpl,
          lr_rm_arbid TYPE RANGE OF afru-arbid,
          ls_rm_arbid LIKE LINE OF lr_rm_arbid,
          lr_rm_ersda TYPE RANGE OF afru-ersda,
          ls_rm_ersda LIKE LINE OF lr_rm_ersda,
          lr_rm_typ   TYPE RANGE OF mara-zztyp,
          ls_rm_typ   LIKE LINE OF lr_rm_typ,
          lv_rm_typcd TYPE char10.
    FIELD-SYMBOLS: <ls_rm_f> LIKE LINE OF it_filter_select_options,
                   <ls_rm_o> TYPE /iwbep/s_cod_select_option,
                   <ls_rm_a> TYPE zstr_shop_rm.

    LOOP AT it_filter_select_options ASSIGNING <ls_rm_f>.
      LOOP AT <ls_rm_f>-select_options ASSIGNING <ls_rm_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_rm_f>-property ).
          WHEN 'WERKS'. lv_rm_werks = <ls_rm_o>-low.
          WHEN 'ARBPL'.
            ls_rm_arbpl-sign = 'I'. ls_rm_arbpl-option = 'EQ'. ls_rm_arbpl-low = to_upper( <ls_rm_o>-low ).
            APPEND ls_rm_arbpl TO lr_rm_arbpl.
          WHEN 'ERSDA'.
            ls_rm_ersda-sign = 'I'. ls_rm_ersda-option = 'EQ'. ls_rm_ersda-low = <ls_rm_o>-low.
            APPEND ls_rm_ersda TO lr_rm_ersda.
          WHEN 'TYPCD'.
            lv_rm_typcd = <ls_rm_o>-low.
            ls_rm_typ-sign = 'I'. ls_rm_typ-low = <ls_rm_o>-low.
            IF <ls_rm_o>-low CA '*'.
              ls_rm_typ-option = 'CP'.
            ELSE.
              ls_rm_typ-option = 'EQ'.
            ENDIF.
            APPEND ls_rm_typ TO lr_rm_typ.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

*   Schutz: Pflichtfilter, hoechstens 30 Arbeitsplaetze und 7 Tage, nicht aelter als 400 Tage
    DESCRIBE TABLE lr_rm_arbpl LINES lv_rm_n.
    DESCRIBE TABLE lr_rm_ersda LINES lv_rm_m.
    lv_rm_alt = sy-datum - 400.
    LOOP AT lr_rm_ersda INTO ls_rm_ersda.
      lv_rm_tag = ls_rm_ersda-low.
      IF lv_rm_tag < lv_rm_alt OR lv_rm_tag > sy-datum.
        lv_rm_m = 0.
      ENDIF.
    ENDLOOP.
    IF lv_rm_werks IS INITIAL OR lv_rm_n < 1 OR lv_rm_n > 30 OR lv_rm_m < 1 OR lv_rm_m > 7.
      lv_rm_msg = 'ShopRmSet braucht Werks, Arbpl (1-30) und Ersda (JJJJMMTT, 1-7 Tage, max. 400 Tage zurueck).'.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = lv_rm_msg.
    ENDIF.

*   ZC27 BILDEN_RANGE
    SELECT objid FROM crhd INTO ls_rm_arbid-low
      WHERE werks = lv_rm_werks
        AND arbpl IN lr_rm_arbpl.
      ls_rm_arbid-sign = 'I'. ls_rm_arbid-option = 'EQ'.
      APPEND ls_rm_arbid TO lr_rm_arbid.
    ENDSELECT.

    IF lr_rm_arbid IS NOT INITIAL.
*     ZC27 LESEN_AFRU
      SELECT aufnr aufpl vornr arbid ersda lmnga FROM afru INTO TABLE lt_rm_ru
        WHERE ersda IN lr_rm_ersda
          AND werks = lv_rm_werks
          AND arbid IN lr_rm_arbid
          AND stokz = ' '
          AND stzhl = 0.

      LOOP AT lt_rm_ru INTO ls_rm_ru.
*       Typencode wie ZC27 (CHECK s_typcd), nur wenn gefiltert
        IF lr_rm_typ IS NOT INITIAL.
          CLEAR: lv_rm_matnr, lv_rm_zztyp.
          SELECT matnr INTO lv_rm_matnr FROM afpo UP TO 1 ROWS
            WHERE aufnr = ls_rm_ru-aufnr ORDER BY PRIMARY KEY.
          ENDSELECT.
          SELECT SINGLE zztyp FROM mara INTO lv_rm_zztyp WHERE matnr = lv_rm_matnr.
          CHECK lv_rm_zztyp IN lr_rm_typ.
        ENDIF.

*       Hoechster Vorgang des Auftrags an den gewaehlten Arbeitsplaetzen (je Arbeitsplan einmal gelesen)
        READ TABLE lt_rm_vg INTO ls_rm_vg WITH TABLE KEY aufpl = ls_rm_ru-aufpl.
        IF sy-subrc <> 0.
          CLEAR lt_rm_afvc.
          SELECT aufpl vornr FROM afvc INTO TABLE lt_rm_afvc
            WHERE aufpl = ls_rm_ru-aufpl
              AND arbid IN lr_rm_arbid.
          SORT lt_rm_afvc BY vornr DESCENDING.
          CLEAR ls_rm_vg.
          READ TABLE lt_rm_afvc INTO ls_rm_vg INDEX 1.
          ls_rm_vg-aufpl = ls_rm_ru-aufpl.
          INSERT ls_rm_vg INTO TABLE lt_rm_vg.
        ENDIF.
        CHECK ls_rm_vg-vornr = ls_rm_ru-vornr.

        SELECT SINGLE arbpl FROM crhd INTO lv_rm_arbpl
          WHERE objty = 'A' AND objid = ls_rm_ru-arbid.
        READ TABLE lt_rm_out ASSIGNING <ls_rm_a>
          WITH KEY arbpl = lv_rm_arbpl ersda = ls_rm_ru-ersda.
        IF sy-subrc <> 0.
          CLEAR ls_rm_out.
          ls_rm_out-werks = lv_rm_werks.
          ls_rm_out-arbpl = lv_rm_arbpl.
          ls_rm_out-ersda = ls_rm_ru-ersda.
          ls_rm_out-typcd = lv_rm_typcd.
          APPEND ls_rm_out TO lt_rm_out ASSIGNING <ls_rm_a>.
        ENDIF.
        <ls_rm_a>-lmnga  = <ls_rm_a>-lmnga + round( val = ls_rm_ru-lmnga dec = 0 ).
        <ls_rm_a>-anzahl = <ls_rm_a>-anzahl + 1.
      ENDLOOP.
    ENDIF.

    SORT lt_rm_out BY ersda arbpl.
    copy_data_to_ref( EXPORTING is_data = lt_rm_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

  IF iv_entity_set_name = 'ShopProdSet'.
    TYPES: BEGIN OF ty_pd_fa,
             aufnr TYPE afko-aufnr,
             getri TYPE afko-getri,
             dispo TYPE afko-dispo,
             gamng TYPE afko-gamng,
             wemng TYPE afpo-wemng,
             posnr TYPE afpo-posnr,
           END OF ty_pd_fa.
    DATA: lt_pd_out   TYPE STANDARD TABLE OF zstr_shop_prod,
          ls_pd_out   TYPE zstr_shop_prod,
          lt_pd_fa    TYPE STANDARD TABLE OF ty_pd_fa,
          ls_pd_fa    TYPE ty_pd_fa,
          lv_pd_werks TYPE werks_d,
          lv_pd_tag   TYPE d,
          lv_pd_alt   TYPE d,
          lv_pd_m     TYPE i,
          lv_pd_last  TYPE afko-aufnr,
          lv_pd_msg   TYPE bapi_msg,
          lr_pd_dat   TYPE RANGE OF afko-getri,
          ls_pd_dat   LIKE LINE OF lr_pd_dat,
          lr_pd_disp  TYPE RANGE OF afko-dispo,
          ls_pd_disp  LIKE LINE OF lr_pd_disp.
    FIELD-SYMBOLS: <ls_pd_f> LIKE LINE OF it_filter_select_options,
                   <ls_pd_o> TYPE /iwbep/s_cod_select_option,
                   <ls_pd_a> TYPE zstr_shop_prod.

    LOOP AT it_filter_select_options ASSIGNING <ls_pd_f>.
      LOOP AT <ls_pd_f>-select_options ASSIGNING <ls_pd_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_pd_f>-property ).
          WHEN 'WERKS'. lv_pd_werks = <ls_pd_o>-low.
          WHEN 'DATUM'.
            ls_pd_dat-sign = 'I'. ls_pd_dat-option = 'EQ'. ls_pd_dat-low = <ls_pd_o>-low.
            APPEND ls_pd_dat TO lr_pd_dat.
          WHEN 'DISPO'.
            ls_pd_disp-sign = 'I'. ls_pd_disp-low = to_upper( <ls_pd_o>-low ).
            IF <ls_pd_o>-low CA '*'.
              ls_pd_disp-option = 'CP'.
            ELSE.
              ls_pd_disp-option = 'EQ'.
            ENDIF.
            APPEND ls_pd_disp TO lr_pd_disp.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

    DESCRIBE TABLE lr_pd_dat LINES lv_pd_m.
    lv_pd_alt = sy-datum - 400.
    LOOP AT lr_pd_dat INTO ls_pd_dat.
      lv_pd_tag = ls_pd_dat-low.
      IF lv_pd_tag < lv_pd_alt OR lv_pd_tag > sy-datum.
        lv_pd_m = 0.
      ENDIF.
    ENDLOOP.
    IF lv_pd_werks IS INITIAL OR lv_pd_m < 1 OR lv_pd_m > 7.
      lv_pd_msg = 'ShopProdSet braucht Werks und Datum (Istendtermin JJJJMMTT, 1-7 Tage, max. 400 Tage zurueck).'.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = lv_pd_msg.
    ENDIF.

*   Fertigungsauftraege mit Istendtermin (AFKO-GETRI) im Datum, wie COOIS: Gesamtmenge AFKO-GAMNG je Auftrag
*   einmal, WE-Menge AFPO-WEMNG je Position
    SELECT k~aufnr k~getri k~dispo k~gamng p~wemng p~posnr
      FROM afko AS k INNER JOIN afpo AS p ON p~aufnr = k~aufnr
      INTO TABLE lt_pd_fa UP TO 50000 ROWS
      WHERE p~dwerk = lv_pd_werks
        AND k~getri IN lr_pd_dat
        AND k~dispo IN lr_pd_disp.
    SORT lt_pd_fa BY aufnr posnr.
    CLEAR lv_pd_last.
    LOOP AT lt_pd_fa INTO ls_pd_fa.
      READ TABLE lt_pd_out ASSIGNING <ls_pd_a>
        WITH KEY dispo = ls_pd_fa-dispo datum = ls_pd_fa-getri.
      IF sy-subrc <> 0.
        CLEAR ls_pd_out.
        ls_pd_out-werks = lv_pd_werks.
        ls_pd_out-dispo = ls_pd_fa-dispo.
        ls_pd_out-datum = ls_pd_fa-getri.
        APPEND ls_pd_out TO lt_pd_out ASSIGNING <ls_pd_a>.
      ENDIF.
      IF ls_pd_fa-aufnr <> lv_pd_last.
        <ls_pd_a>-gamng  = <ls_pd_a>-gamng + round( val = ls_pd_fa-gamng dec = 0 ).
        <ls_pd_a>-anzahl = <ls_pd_a>-anzahl + 1.
        lv_pd_last = ls_pd_fa-aufnr.
      ENDIF.
      <ls_pd_a>-wemng = <ls_pd_a>-wemng + round( val = ls_pd_fa-wemng dec = 0 ).
    ENDLOOP.

    SORT lt_pd_out BY datum dispo.
    copy_data_to_ref( EXPORTING is_data = lt_pd_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.
* ===== Ende DPC ========================================================
