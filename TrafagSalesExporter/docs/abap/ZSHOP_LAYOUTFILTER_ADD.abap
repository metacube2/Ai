*&---------------------------------------------------------------------*
*& ZSHOP_LAYOUTFILTER_ADD: Layoutfilter ZC22 / P00 TX und ZC27 /NIN im ZPOWERBI_EINKAUF_SRV
*& Plan saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md (Abschnitt 3.4/3.5), T76 2026-11-13,
*& Auftrag T76K912906 (Aufgabe T76K912907). Quelle der Wahrheit fuer die Einfuegungen; die ganzen Methoden
*& stehen in saptasks/shopfloor_sap/*_neu_2026-11-13_layout.abap (erzeugt mit methoden_bauen_layout_2026-11-13.py).
*& DDIC: ZSTR_SHOP_RM + MATEX CHAR40; ZSTR_SHOP_AUF + STTXT TEXT100, ARBPL ARBPL, VORNR CHAR4, AVO CHAR1.
*&---------------------------------------------------------------------*

*---------------------------------------------------------------------
* MPC_EXT->DEFINE, Entity ShopAuf: direkt vor bind_structure( 'ZSTR_SHOP_AUF' ), danach Leerzeile
*---------------------------------------------------------------------
* ZC22-Layoutfelder (saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md): Sttxt immer bei Typ VF (VR/VZ),
* Arbpl/Vornr nur mit Filter Avo eq 'X'
  lo_property = lo_entity_type->create_property( iv_property_name = 'Sttxt' iv_abap_fieldname = 'STTXT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 100 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Arbpl' iv_abap_fieldname = 'ARBPL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Vornr' iv_abap_fieldname = 'VORNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Avo' iv_abap_fieldname = 'AVO' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

*---------------------------------------------------------------------
* MPC_EXT->DEFINE, Entity ShopRm: direkt vor bind_structure( 'ZSTR_SHOP_RM' ), danach Leerzeile
*---------------------------------------------------------------------
* Matex: optionaler Materialausschluss wie ZC27-Layout /NIN, Musterliste (PLAN_LAYOUTFILTER_2026-11-13)
  lo_property = lo_entity_type->create_property( iv_property_name = 'Matex' iv_abap_fieldname = 'MATEX' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

*---------------------------------------------------------------------
* DPC_EXT->GET_ENTITYSET, ShopRmSet (a): nach 'lv_rm_typcd TYPE char10.'
*---------------------------------------------------------------------
*   ZC27-Layout /NIN nachbilden: optionaler Materialausschluss Matex (PLAN_LAYOUTFILTER_2026-11-13)
    TYPES: BEGIN OF ty_rm_mat,
             aufnr TYPE afpo-aufnr,
             matnr TYPE afpo-matnr,
           END OF ty_rm_mat.
    DATA: lt_rm_mat   TYPE SORTED TABLE OF ty_rm_mat WITH UNIQUE KEY aufnr,
          ls_rm_mat   TYPE ty_rm_mat,
          lt_rm_teil  TYPE STANDARD TABLE OF string,
          lt_rm_mx    TYPE STANDARD TABLE OF string,
          lv_rm_mx    TYPE string,
          lv_rm_aus   TYPE abap_bool,
          lv_rm_matex TYPE char40.

*---------------------------------------------------------------------
* DPC_EXT->GET_ENTITYSET, ShopRmSet (b): nach 'APPEND ls_rm_typ TO lr_rm_typ.' (vor WHEN OTHERS)
*---------------------------------------------------------------------
          WHEN 'MATEX'.
            lv_rm_matex = <ls_rm_o>-low.
            SPLIT <ls_rm_o>-low AT ',' INTO TABLE lt_rm_teil.
            LOOP AT lt_rm_teil INTO lv_rm_mx.
              CONDENSE lv_rm_mx.
              IF lv_rm_mx IS NOT INITIAL.
                APPEND lv_rm_mx TO lt_rm_mx.
              ENDIF.
            ENDLOOP.

*---------------------------------------------------------------------
* DPC_EXT->GET_ENTITYSET, ShopRmSet (c): nach dem ENDIF des Typencode-Blocks (beginnt mit Leerzeile)
*---------------------------------------------------------------------

*       Materialausschluss wie ZC27-Standardlayout /NIN (E CP); Material wie ZC27: erste AFPO-Position (ZPP_RM_MENGEN 85-91, 104)
        IF lt_rm_mx IS NOT INITIAL.
          READ TABLE lt_rm_mat INTO ls_rm_mat WITH TABLE KEY aufnr = ls_rm_ru-aufnr.
          IF sy-subrc <> 0.
            CLEAR ls_rm_mat.
            ls_rm_mat-aufnr = ls_rm_ru-aufnr.
            SELECT matnr INTO ls_rm_mat-matnr FROM afpo UP TO 1 ROWS
              WHERE aufnr = ls_rm_ru-aufnr ORDER BY PRIMARY KEY.
            ENDSELECT.
            INSERT ls_rm_mat INTO TABLE lt_rm_mat.
          ENDIF.
          lv_rm_aus = abap_false.
          LOOP AT lt_rm_mx INTO lv_rm_mx.
            IF ls_rm_mat-matnr CP lv_rm_mx.
              lv_rm_aus = abap_true.
              EXIT.
            ENDIF.
          ENDLOOP.
          CHECK lv_rm_aus = abap_false.
        ENDIF.

*---------------------------------------------------------------------
* DPC_EXT->GET_ENTITYSET, ShopRmSet (d): nach 'ls_rm_out-typcd = lv_rm_typcd.'
*---------------------------------------------------------------------
          ls_rm_out-matex = lv_rm_matex.

*---------------------------------------------------------------------
* DPC_EXT->GET_ENTITYSET, ShopAufSet (a): nach 'ls_sa_vz TYPE ty_sa_vz.'
*---------------------------------------------------------------------
*   ZC22-Layoutfelder (PLAN_LAYOUTFILTER_2026-11-13): Statuszeile wie ZC22 FORM status_lang, Sprache D fest
    TYPES: BEGIN OF ty_sa_ao,
             aufnr TYPE aufk-aufnr,
             objnr TYPE aufk-objnr,
           END OF ty_sa_ao,
           BEGIN OF ty_sa_st,
             linep TYPE tj04-linep,
             statp TYPE tj04-statp,
             txt04 TYPE tj02t-txt04,
           END OF ty_sa_st.
    DATA: lv_sa_avo   TYPE char1,
          lt_sa_ao    TYPE SORTED TABLE OF ty_sa_ao WITH UNIQUE KEY aufnr,
          ls_sa_ao    TYPE ty_sa_ao,
          lt_sa_af    TYPE STANDARD TABLE OF ty_sa_ao,
          lt_sa_jsto  TYPE SORTED TABLE OF jsto WITH UNIQUE KEY objnr,
          ls_sa_jsto  TYPE jsto,
          lt_sa_jest  TYPE SORTED TABLE OF jest WITH NON-UNIQUE KEY objnr,
          ls_sa_jest  TYPE jest,
          lt_sa_tj02  TYPE SORTED TABLE OF tj02 WITH UNIQUE KEY istat,
          ls_sa_tj02  TYPE tj02,
          lt_sa_tj02t TYPE SORTED TABLE OF tj02t WITH UNIQUE KEY istat spras,
          ls_sa_tj02t TYPE tj02t,
          lt_sa_tj04  TYPE SORTED TABLE OF tj04 WITH UNIQUE KEY obtyp istat,
          ls_sa_tj04  TYPE tj04,
          lt_sa_st    TYPE STANDARD TABLE OF ty_sa_st,
          ls_sa_st    TYPE ty_sa_st,
          lv_sa_linep TYPE tj04-linep.
    FIELD-SYMBOLS <ls_sa_a> TYPE zstr_shop_auf.

*---------------------------------------------------------------------
* DPC_EXT->GET_ENTITYSET, ShopAufSet (b): nach WHEN 'TYP'.
*---------------------------------------------------------------------
          WHEN 'AVO'. lv_sa_avo = to_upper( <ls_sa_o>-low ).

*---------------------------------------------------------------------
* DPC_EXT->GET_ENTITYSET, ShopAufSet (c): vor dem ersten copy_data_to_ref nach IF lv_sa_typ = 'VZ' OR 'VR' (nach dem Paging)
*---------------------------------------------------------------------
*     ZC22-Layoutfelder (PLAN_LAYOUTFILTER_2026-11-13), nur fuer Fertigungsauftraege der ausgegebenen Seite:
*     Sttxt = ZC22 FORM status_lang (Sprache D fest, das Gateway laeuft nicht sicher deutsch), immer;
*     Arbpl/Vornr = Z_MD04_NEXT_OPERATION wie ZC22 FORM vorgangsdaten, nur mit Filter Avo eq 'X' (teuer).
      CLEAR lt_sa_af.
      LOOP AT lt_sa_out ASSIGNING <ls_sa_a> WHERE typ = 'VF'.
        ls_sa_ao-aufnr = <ls_sa_a>-aufnr.
        APPEND ls_sa_ao TO lt_sa_af.
      ENDLOOP.
      IF lt_sa_af IS NOT INITIAL.
        SELECT aufnr objnr FROM aufk INTO TABLE lt_sa_ao
          FOR ALL ENTRIES IN lt_sa_af WHERE aufnr = lt_sa_af-aufnr.
        IF lt_sa_ao IS NOT INITIAL.
          SELECT * FROM jsto INTO TABLE lt_sa_jsto
            FOR ALL ENTRIES IN lt_sa_ao WHERE objnr = lt_sa_ao-objnr.
          SELECT * FROM jest INTO TABLE lt_sa_jest
            FOR ALL ENTRIES IN lt_sa_ao
            WHERE objnr = lt_sa_ao-objnr AND inact = space AND stat LIKE 'I%'.
        ENDIF.
        SELECT * FROM tj02 INTO TABLE lt_sa_tj02 WHERE istat LIKE 'I%'.
        SELECT * FROM tj02t INTO TABLE lt_sa_tj02t WHERE istat LIKE 'I%' AND spras = 'D'.
        SELECT * FROM tj04 INTO TABLE lt_sa_tj04 WHERE istat LIKE 'I%'.
      ENDIF.
      LOOP AT lt_sa_out ASSIGNING <ls_sa_a> WHERE typ = 'VF'.
        READ TABLE lt_sa_ao INTO ls_sa_ao WITH TABLE KEY aufnr = <ls_sa_a>-aufnr.
        IF sy-subrc = 0.
          CLEAR: lt_sa_st, ls_sa_jsto.
          READ TABLE lt_sa_jsto INTO ls_sa_jsto WITH TABLE KEY objnr = ls_sa_ao-objnr.
          LOOP AT lt_sa_jest INTO ls_sa_jest WHERE objnr = ls_sa_ao-objnr.
            CLEAR ls_sa_st.
            READ TABLE lt_sa_tj02 INTO ls_sa_tj02 WITH TABLE KEY istat = ls_sa_jest-stat.
            IF sy-subrc = 0 AND ls_sa_tj02-nodis IS NOT INITIAL.
              CONTINUE.
            ENDIF.
            READ TABLE lt_sa_tj02t INTO ls_sa_tj02t WITH TABLE KEY istat = ls_sa_jest-stat spras = 'D'.
            IF sy-subrc = 0.
              ls_sa_st-txt04 = ls_sa_tj02t-txt04.
            ENDIF.
            READ TABLE lt_sa_tj04 INTO ls_sa_tj04 WITH TABLE KEY obtyp = ls_sa_jsto-obtyp istat = ls_sa_jest-stat.
            IF sy-subrc = 0.
              ls_sa_st-linep = ls_sa_tj04-linep.
              ls_sa_st-statp = ls_sa_tj04-statp.
            ENDIF.
            IF ls_sa_st-linep IS INITIAL.
              ls_sa_st-linep = '99'.
            ENDIF.
            IF ls_sa_st-statp IS INITIAL.
              ls_sa_st-statp = '99'.
            ENDIF.
            APPEND ls_sa_st TO lt_sa_st.
          ENDLOOP.
          SORT lt_sa_st BY linep statp txt04.
          CLEAR lv_sa_linep.
          LOOP AT lt_sa_st INTO ls_sa_st WHERE linep NE '99'.
            IF lv_sa_linep = ls_sa_st-linep.
              DELETE lt_sa_st INDEX sy-tabix.
            ELSE.
              lv_sa_linep = ls_sa_st-linep.
            ENDIF.
          ENDLOOP.
          LOOP AT lt_sa_st INTO ls_sa_st.
            IF <ls_sa_a>-sttxt IS INITIAL.
              <ls_sa_a>-sttxt = ls_sa_st-txt04.
            ELSE.
              CONCATENATE <ls_sa_a>-sttxt ls_sa_st-txt04 INTO <ls_sa_a>-sttxt SEPARATED BY space.
            ENDIF.
          ENDLOOP.
        ENDIF.
        IF lv_sa_avo = 'X'.
          CALL FUNCTION 'Z_MD04_NEXT_OPERATION'
            EXPORTING
              i_aufnr = <ls_sa_a>-aufnr
            IMPORTING
              e_vornr = <ls_sa_a>-vornr
              e_arbpl = <ls_sa_a>-arbpl.
          <ls_sa_a>-avo = 'X'.
        ENDIF.
      ENDLOOP.
