*&---------------------------------------------------------------------*
*& METHODENRUMPF fuer die redefinierte DPC_EXT-Methode
*& ZSTR_LZCODE_USAG_GET_ENTITYSET  (Gateway-Service ZPOWERBI_EINKAUF_SRV)
*&
*& EINFUEGEN (wichtig, Fehlerquelle 2026-07-21 beim Parent-Set): Den
*& KOMPLETTEN Block unten INKLUSIVE "METHOD ..." und "ENDMETHOD." nehmen
*& und damit den generierten Stub 1:1 ersetzen - im Editor alles von
*& "method zstr_lzcode_usag_get_entityset." bis zum zugehoerigen
*& "endmethod." markieren und ueberschreiben. Landet der Rumpf ausserhalb
*& des METHOD-Rahmens, meldet der Syntaxcheck je Zeile "Zwischen CLASS
*& ... IMPLEMENTATION und ENDCLASS duerfen nur Methoden definiert werden".
*& KEINE CLASS-Statements, alles lokale TYPES/DATA. Ersetzt die Varianten
*& mit separater Klasse ZCL_LZCODE_PROVIDER (die dann NICHT angelegt
*& werden muss). Fachliche Logik identisch mit
*& docs/abap/ZCL_LZCODE_PROVIDER_INLINE.abap (alle Fixes v2 enthalten).
*&
*& DRAFT 2026-07-21 - Syntaxcheck im System offen. NEU und UNVERIFIZIERT
*& gegenueber der INLINE-Variante ist ausschliesslich der Filter-Teil am
*& Anfang (it_filter_select_options auslesen) und die Uebergabe nach
*& et_entityset am Ende - der Rest ist der mehrfach gegengepruefte Kern.
*&
*& OData-Nutzung:
*&   .../ZSTR_LZCODE_USAGE?$filter=Richtung eq 'TOPDOWN' and Vknr eq 'E01758'
*&   - Richtung: 'TOPDOWN' (Default) oder 'BOTTOMUP'
*&   - Vknr/Kompnr: schraenken die Materialselektion ein (entspricht
*&     S_MATNR im Report; bei TOPDOWN fachlich Vknr filtern, bei
*&     BOTTOMUP Kompnr - technisch werden beide zusammengefuehrt)
*&   - OHNE Vknr/Kompnr-Filter wird abgebrochen (Report: "Bitte
*&     Selektion eingeben") - sonst wuerde ganz MARA selektiert.
*&   - S_TYPCD (ZZTYP_F4) ist NICHT filterbar: die Struktur hat kein
*&     solches Property. Bei Bedarf Feld in ZSTR_LZCODE_USAGE ergaenzen.
*&---------------------------------------------------------------------*

METHOD zstr_lzcode_usag_get_entityset.

    " ===================================================================
    " Lokale Typen (identisch zur INLINE-Variante)
    " ===================================================================
    CONSTANTS lc_werks TYPE werks_d VALUE '1100'.

    TYPES: BEGIN OF ty_matnr_line,
             matnr TYPE matnr,
           END OF ty_matnr_line.
    TYPES tt_matnr_list TYPE STANDARD TABLE OF ty_matnr_line WITH DEFAULT KEY.

    TYPES: BEGIN OF ty_stamm,
             matnr        TYPE matnr,
             maktx        TYPE maktx,
             meins        TYPE meins,
             mstae        TYPE mstae,
             mstav        TYPE mstav,
             lvorm        TYPE lvorm,
             zzlzcod      TYPE mara-zzlzcod,
             zzlzcodsort  TYPE mara-zzlzcodsort,
             dismm        TYPE dismm,
             minbe        TYPE minbe,
             disls        TYPE disls,
             bstfe        TYPE bstfe,
             eisbe        TYPE eisbe,
             beskz        TYPE beskz,
             verpr        TYPE verpr,
             stprs        TYPE stprs,
             peinh        TYPE peinh,
             vprsv        TYPE vprsv,
             has_bom      TYPE abap_bool,
             stueckkosten TYPE p LENGTH 11 DECIMALS 2,
           END OF ty_stamm.
    TYPES tt_stamm TYPE HASHED TABLE OF ty_stamm WITH UNIQUE KEY matnr.

    TYPES: BEGIN OF ty_stamm_raw,
             matnr       TYPE matnr,
             maktx       TYPE maktx,
             meins       TYPE meins,
             mstae       TYPE mstae,
             mstav       TYPE mstav,
             lvorm       TYPE lvorm,
             zzlzcod     TYPE mara-zzlzcod,
             zzlzcodsort TYPE mara-zzlzcodsort,
             dismm       TYPE dismm,
             minbe       TYPE minbe,
             disls       TYPE disls,
             bstfe       TYPE bstfe,
             eisbe       TYPE eisbe,
             beskz       TYPE beskz,
             verpr       TYPE verpr,
             stprs       TYPE stprs,
             peinh       TYPE peinh,
             vprsv       TYPE vprsv,
           END OF ty_stamm_raw.

    TYPES: BEGIN OF ty_md04,
             matnr     TYPE matnr,
             labst     TYPE labst,
             feste_zug TYPE menge_d,
             gepl_zug  TYPE menge_d,
             feste_abg TYPE menge_d,
             gepl_abg  TYPE menge_d,
             verbr     TYPE menge_d,
             omeng     TYPE menge_d,
             owert     TYPE salk3,
             mkmng     TYPE menge_d,
             omkwr     TYPE salk3,
           END OF ty_md04.
    TYPES tt_md04 TYPE HASHED TABLE OF ty_md04 WITH UNIQUE KEY matnr.

    " FIX 2: menge/meins bleiben Charfelder wie in ZAT_VC (kein
    " CONVT_NO_NUMBER-Dump beim SELECT; Konvertierung unten per TRY/CATCH).
    TYPES: BEGIN OF ty_pair,
             vknr   TYPE matnr,
             kompnr TYPE matnr,
             menge  TYPE zat_vc-menge,
             meins  TYPE zat_vc-mengeneinheit,
           END OF ty_pair.
    TYPES tt_pair TYPE STANDARD TABLE OF ty_pair WITH DEFAULT KEY.

    " ===================================================================
    " Arbeitsvariablen
    " ===================================================================
    DATA: lt_mara_sel       TYPE STANDARD TABLE OF matnr,
          lt_pairs_raw      TYPE tt_pair,
          lt_pairs          TYPE tt_pair,
          lt_all_matnr      TYPE tt_matnr_list,
          ls_matnr_line     TYPE ty_matnr_line,
          lt_stamm          TYPE tt_stamm,
          lt_md04           TYPE tt_md04,
          lt_all_matnr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
          lv_matnr          TYPE matnr,
          lv_richtung       TYPE char10,
          lt_out            TYPE STANDARD TABLE OF zstr_lzcode_usage,
          ls_out            TYPE zstr_lzcode_usage,
          lt_r_matnr        TYPE RANGE OF matnr,
          lv_topdown        TYPE abap_bool VALUE abap_true.

    " ===================================================================
    " Schritt 0 (NEU, gateway-spezifisch): OData-$filter auslesen.
    " it_filter_select_options: eine Zeile je gefiltertem Property,
    " select_options ist eine Range-Tabelle (SIGN/OPTION/LOW/HIGH).
    " ===================================================================
    LOOP AT it_filter_select_options INTO DATA(ls_filter).
      CASE to_upper( ls_filter-property ).
        WHEN 'RICHTUNG'.
          READ TABLE ls_filter-select_options INTO DATA(ls_so_r) INDEX 1.
          IF sy-subrc = 0 AND to_upper( ls_so_r-low ) = 'BOTTOMUP'.
            lv_topdown = abap_false.
          ENDIF.
        WHEN 'VKNR' OR 'KOMPNR'.
          LOOP AT ls_filter-select_options INTO DATA(ls_so).
            APPEND VALUE #( sign   = ls_so-sign
                            option = ls_so-option
                            low    = ls_so-low
                            high   = ls_so-high ) TO lt_r_matnr.
          ENDLOOP.
      ENDCASE.
    ENDLOOP.

    lv_richtung = COND #( WHEN lv_topdown = abap_true THEN 'TOPDOWN' ELSE 'BOTTOMUP' ).

    " Ohne Materialeinschraenkung abbrechen - Gegenstueck zum Report
    " ("Bitte Selektion eingeben"); verhindert Vollselektion auf MARA.
    IF lt_r_matnr IS INITIAL.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = 'Filter Vknr oder Kompnr angeben (z.B. $filter=Vknr eq ''E01758'')'.
    ENDIF.

    " -----------------------------------------------------------
    " Schritt 1: Materialselektion (mirrort zlo03.txt process_main).
    " Kein MTART-'FERT'-Filter - im Report am 2.3.26 deaktiviert.
    " -----------------------------------------------------------
    SELECT matnr FROM mara
      INTO TABLE @lt_mara_sel
      WHERE matnr IN @lt_r_matnr
        AND lvorm = @space.

    IF lt_mara_sel IS INITIAL.
      RETURN.
    ENDIF.

    " -----------------------------------------------------------
    " Schritt 2: ZAT_VC lesen (Rollen je Richtung getauscht)
    " -----------------------------------------------------------
    IF lv_topdown = abap_true.
      SELECT matnr AS vknr, kompnr, menge, mengeneinheit AS meins
        FROM zat_vc
        INTO CORRESPONDING FIELDS OF TABLE @lt_pairs_raw
        FOR ALL ENTRIES IN @lt_mara_sel
        WHERE matnr = @lt_mara_sel-table_line
          AND menge <> @space
          AND mengeneinheit <> @space.
    ELSE.
      SELECT matnr AS vknr, kompnr, menge, mengeneinheit AS meins
        FROM zat_vc
        INTO CORRESPONDING FIELDS OF TABLE @lt_pairs_raw
        FOR ALL ENTRIES IN @lt_mara_sel
        WHERE kompnr = @lt_mara_sel-table_line
          AND menge <> @space
          AND mengeneinheit <> @space.
    ENDIF.

    IF lt_pairs_raw IS INITIAL.
      RETURN.
    ENDIF.

    " FIX 6: Deterministisches Dedup
    SORT lt_pairs_raw BY vknr kompnr menge DESCENDING.
    DELETE ADJACENT DUPLICATES FROM lt_pairs_raw COMPARING vknr kompnr.
    lt_pairs = lt_pairs_raw.

    " -----------------------------------------------------------
    " Schritt 3: Alle beteiligten Materialnummern sammeln
    " -----------------------------------------------------------
    LOOP AT lt_pairs INTO DATA(ls_pair_collect).
      INSERT ls_pair_collect-vknr INTO TABLE lt_all_matnr_temp.
      INSERT ls_pair_collect-kompnr INTO TABLE lt_all_matnr_temp.
    ENDLOOP.

    LOOP AT lt_all_matnr_temp INTO lv_matnr.
      ls_matnr_line-matnr = lv_matnr.
      APPEND ls_matnr_line TO lt_all_matnr.
    ENDLOOP.
    SORT lt_all_matnr BY matnr.

    " ===================================================================
    " Schritt 4: Stammdaten bulk laden (FAE auf Liste, FIX 3)
    " ===================================================================
    IF lt_all_matnr IS NOT INITIAL.
      DATA lt_stamm_raw TYPE STANDARD TABLE OF ty_stamm_raw.
      DATA lt_bom       TYPE STANDARD TABLE OF matnr.

      SELECT m~matnr, t~maktx, m~meins, m~mstae, m~mstav, m~lvorm,
             m~zzlzcod, m~zzlzcodsort,
             c~dismm, c~minbe, c~disls, c~bstfe, c~eisbe, c~beskz,
             b~verpr, b~stprs, b~peinh, b~vprsv
        FROM mara AS m
        LEFT JOIN makt AS t ON t~matnr = m~matnr AND t~spras = @sy-langu
        LEFT JOIN marc AS c ON c~matnr = m~matnr AND c~werks = @lc_werks
        LEFT JOIN mbew AS b ON b~matnr = m~matnr AND b~bwkey = @lc_werks
                           AND b~bwtar = @space
        INTO CORRESPONDING FIELDS OF TABLE @lt_stamm_raw
        FOR ALL ENTRIES IN @lt_all_matnr
        WHERE m~matnr = @lt_all_matnr-matnr
          AND m~lvorm = @space.

      SELECT matnr FROM mast
        INTO TABLE @lt_bom
        FOR ALL ENTRIES IN @lt_all_matnr
        WHERE matnr = @lt_all_matnr-matnr
          AND werks = @lc_werks
          AND stlan = '1'.
      SORT lt_bom.
      DELETE ADJACENT DUPLICATES FROM lt_bom.

      LOOP AT lt_stamm_raw INTO DATA(ls_stamm_raw).
        DATA(ls_stamm) = VALUE ty_stamm(
          matnr       = ls_stamm_raw-matnr
          maktx       = ls_stamm_raw-maktx
          meins       = ls_stamm_raw-meins
          mstae       = ls_stamm_raw-mstae
          mstav       = ls_stamm_raw-mstav
          lvorm       = ls_stamm_raw-lvorm
          zzlzcod     = ls_stamm_raw-zzlzcod
          zzlzcodsort = ls_stamm_raw-zzlzcodsort
          dismm       = ls_stamm_raw-dismm
          minbe       = ls_stamm_raw-minbe
          disls       = ls_stamm_raw-disls
          bstfe       = ls_stamm_raw-bstfe
          eisbe       = ls_stamm_raw-eisbe
          beskz       = ls_stamm_raw-beskz
          verpr       = ls_stamm_raw-verpr
          stprs       = ls_stamm_raw-stprs
          peinh       = COND #( WHEN ls_stamm_raw-peinh IS INITIAL THEN 1
                                ELSE ls_stamm_raw-peinh )
          vprsv       = ls_stamm_raw-vprsv ).

        READ TABLE lt_bom TRANSPORTING NO FIELDS
          WITH KEY table_line = ls_stamm_raw-matnr BINARY SEARCH.
        ls_stamm-has_bom = COND #( WHEN sy-subrc = 0 THEN abap_true
                                   ELSE abap_false ).

        " Stueckkosten wie fill_ktab: VERPR bei VPRSV='V', sonst STPRS
        ls_stamm-stueckkosten =
          COND #( WHEN ls_stamm-vprsv = 'V'
                  THEN ls_stamm-verpr / ls_stamm-peinh
                  ELSE ls_stamm-stprs / ls_stamm-peinh ).

        INSERT ls_stamm INTO TABLE lt_stamm.
      ENDLOOP.
    ENDIF.

    " ===================================================================
    " Schritt 5: ZMD04_CALC bulk laden (kein Live-MD04-Aufbau)
    " ===================================================================
    IF lt_all_matnr IS NOT INITIAL.
      DATA lt_md04_raw TYPE STANDARD TABLE OF zmd04_calc.

      SELECT matnr, werks, labst, feste_zug, gepl_zug, feste_abg, gepl_abg,
             verbr, omeng, owert, mkmng, omkwr
        FROM zmd04_calc
        INTO CORRESPONDING FIELDS OF TABLE @lt_md04_raw
        FOR ALL ENTRIES IN @lt_all_matnr
        WHERE matnr = @lt_all_matnr-matnr
          AND werks = @lc_werks.

      LOOP AT lt_md04_raw INTO DATA(ls_md04_raw).
        INSERT VALUE ty_md04( matnr     = ls_md04_raw-matnr
                              labst     = ls_md04_raw-labst
                              feste_zug = ls_md04_raw-feste_zug
                              gepl_zug  = ls_md04_raw-gepl_zug
                              feste_abg = ls_md04_raw-feste_abg
                              gepl_abg  = ls_md04_raw-gepl_abg
                              verbr     = ls_md04_raw-verbr
                              omeng     = ls_md04_raw-omeng
                              owert     = ls_md04_raw-owert
                              mkmng     = ls_md04_raw-mkmng
                              omkwr     = ls_md04_raw-omkwr )
          INTO TABLE lt_md04.
      ENDLOOP.
    ENDIF.

    " -----------------------------------------------------------
    " Schritt 6: Kopfmaterial-Zusatzfelder je Vknr (VTAB im Report)
    " -----------------------------------------------------------
    TYPES: BEGIN OF ty_vknr_info,
             vknr  TYPE matnr,
             mstae TYPE mstae,
             verbr TYPE menge_d,
           END OF ty_vknr_info.
    DATA lt_vknr_info TYPE HASHED TABLE OF ty_vknr_info WITH UNIQUE KEY vknr.

    LOOP AT lt_pairs INTO DATA(ls_pair_vknr).
      READ TABLE lt_vknr_info TRANSPORTING NO FIELDS
        WITH TABLE KEY vknr = ls_pair_vknr-vknr.
      IF sy-subrc <> 0.
        DATA(ls_vknr_info) = VALUE ty_vknr_info( vknr = ls_pair_vknr-vknr ).
        READ TABLE lt_stamm INTO DATA(ls_stamm_v)
          WITH TABLE KEY matnr = ls_pair_vknr-vknr.
        IF sy-subrc = 0.
          ls_vknr_info-mstae = ls_stamm_v-mstae.
        ENDIF.
        READ TABLE lt_md04 INTO DATA(ls_md04_v)
          WITH TABLE KEY matnr = ls_pair_vknr-vknr.
        IF sy-subrc = 0.
          ls_vknr_info-verbr = ls_md04_v-verbr.
        ENDIF.
        INSERT ls_vknr_info INTO TABLE lt_vknr_info.
      ENDIF.
    ENDLOOP.

    " -----------------------------------------------------------
    " Schritt 7: Exklusivitaet - nur Top-Down fachlich belegt
    " -----------------------------------------------------------
    DATA lt_exklusiv TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

    IF lv_topdown = abap_true.

      DATA: lt_kompnr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
            lt_kompnr_list TYPE tt_matnr_list.
      LOOP AT lt_pairs INTO DATA(ls_pair_k).
        INSERT ls_pair_k-kompnr INTO TABLE lt_kompnr_temp.
      ENDLOOP.
      LOOP AT lt_kompnr_temp INTO lv_matnr.
        ls_matnr_line-matnr = lv_matnr.
        APPEND ls_matnr_line TO lt_kompnr_list.
      ENDLOOP.

      DATA lt_sel_vknr TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.
      LOOP AT lt_pairs INTO DATA(ls_pair_v).
        INSERT ls_pair_v-vknr INTO TABLE lt_sel_vknr.
      ENDLOOP.

      " FIX 7: SORTED TABLE -> LOOP WHERE optimiert
      TYPES: BEGIN OF ty_usage,
               kompnr TYPE matnr,
               vknr   TYPE matnr,
             END OF ty_usage.
      DATA lt_usage_raw TYPE STANDARD TABLE OF ty_usage.
      DATA lt_usage     TYPE SORTED TABLE OF ty_usage
                          WITH NON-UNIQUE KEY kompnr vknr.

      SELECT kompnr, matnr AS vknr
        FROM zat_vc
        INTO CORRESPONDING FIELDS OF TABLE @lt_usage_raw
        FOR ALL ENTRIES IN @lt_kompnr_list
        WHERE kompnr = @lt_kompnr_list-matnr.

      SORT lt_usage_raw BY kompnr vknr.
      DELETE ADJACENT DUPLICATES FROM lt_usage_raw COMPARING kompnr vknr.
      lt_usage = lt_usage_raw.
      FREE lt_usage_raw.

      DATA: lt_usage_vknr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
            lt_usage_vknr_list TYPE tt_matnr_list.
      LOOP AT lt_usage INTO DATA(ls_usage_collect).
        INSERT ls_usage_collect-vknr INTO TABLE lt_usage_vknr_temp.
      ENDLOOP.
      LOOP AT lt_usage_vknr_temp INTO lv_matnr.
        ls_matnr_line-matnr = lv_matnr.
        APPEND ls_matnr_line TO lt_usage_vknr_list.
      ENDLOOP.

      TYPES: BEGIN OF ty_vknr_mara,
               matnr TYPE matnr,
               mtart TYPE mtart,
               mstae TYPE mstae,
             END OF ty_vknr_mara.
      DATA lt_vknr_mara TYPE HASHED TABLE OF ty_vknr_mara WITH UNIQUE KEY matnr.
      IF lt_usage_vknr_list IS NOT INITIAL.
        SELECT matnr, mtart, mstae FROM mara
          INTO CORRESPONDING FIELDS OF TABLE @lt_vknr_mara
          FOR ALL ENTRIES IN @lt_usage_vknr_list
          WHERE matnr = @lt_usage_vknr_list-matnr.
      ENDIF.

      LOOP AT lt_kompnr_list INTO DATA(ls_kompnr_l).
        DATA(lv_is_exklusiv) = abap_true.
        LOOP AT lt_usage INTO DATA(ls_usage)
             WHERE kompnr = ls_kompnr_l-matnr.
          READ TABLE lt_sel_vknr TRANSPORTING NO FIELDS
            WITH TABLE KEY table_line = ls_usage-vknr.
          IF sy-subrc <> 0.
            READ TABLE lt_vknr_mara INTO DATA(ls_vm)
              WITH TABLE KEY matnr = ls_usage-vknr.
            IF sy-subrc = 0 AND ls_vm-mtart = 'FERT' AND ls_vm-mstae <> '99'.
              lv_is_exklusiv = abap_false.
              EXIT.
            ENDIF.
          ENDIF.
        ENDLOOP.
        IF lv_is_exklusiv = abap_true.
          INSERT ls_kompnr_l-matnr INTO TABLE lt_exklusiv.
        ENDIF.
      ENDLOOP.

    ENDIF.

    " ===================================================================
    " Schritt 8: Ausgabezeilen bauen. Mengenkonvertierung inline OHNE
    " RETURN (wuerde sonst die ganze Methode abbrechen).
    " ===================================================================
    LOOP AT lt_pairs INTO DATA(ls_pair).

      " FIX 5: Bottom-Up ohne gueltigen Stammsatz ueberspringen
      IF lv_topdown = abap_false.
        READ TABLE lt_stamm TRANSPORTING NO FIELDS
          WITH TABLE KEY matnr = ls_pair-vknr.
        IF sy-subrc <> 0.
          CONTINUE.
        ENDIF.
      ENDIF.

      " WAERS fest 'CHF' (Werk 1100 = Trafag AG/CH/CHF) - Referenzfeld
      " der CURR-Felder OWERT/OMKWR.
      ls_out = VALUE zstr_lzcode_usage( richtung = lv_richtung
                                        vknr     = ls_pair-vknr
                                        kompnr   = ls_pair-kompnr
                                        waers    = 'CHF' ).

      READ TABLE lt_vknr_info INTO DATA(ls_vi)
        WITH TABLE KEY vknr = ls_pair-vknr.
      IF sy-subrc = 0.
        ls_out-vknr_mstae     = ls_vi-mstae.
        ls_out-vknr_verbrauch = ls_vi-verbr.
      ENDIF.

      DATA lv_meins_komp TYPE meins.
      CLEAR lv_meins_komp.
      READ TABLE lt_stamm INTO DATA(ls_stamm2)
        WITH TABLE KEY matnr = ls_pair-kompnr.
      IF sy-subrc = 0.
        lv_meins_komp        = ls_stamm2-meins.
        ls_out-kompnr_maktx  = ls_stamm2-maktx.
        ls_out-kompnr_meins  = ls_stamm2-meins.
        ls_out-dismm         = ls_stamm2-dismm.
        ls_out-minbe         = ls_stamm2-minbe.
        ls_out-disls         = ls_stamm2-disls.
        ls_out-bstfe         = ls_stamm2-bstfe.
        ls_out-eisbe         = ls_stamm2-eisbe.
        ls_out-mstae         = ls_stamm2-mstae.
        ls_out-mstav         = ls_stamm2-mstav.
        ls_out-beskz         = ls_stamm2-beskz.
        ls_out-zzlzcod       = ls_stamm2-zzlzcod.
        ls_out-zzlzcodsort   = ls_stamm2-zzlzcodsort.
        ls_out-stueckkosten  = ls_stamm2-stueckkosten.
        ls_out-baugruppe     = ls_stamm2-has_bom.
      ELSE.
        lv_meins_komp = ls_pair-meins.
      ENDIF.

      " ---- Mengenkonvertierung inline (kein RETURN!) ----
      DATA lv_conv_str       TYPE string.
      DATA lv_conv_menge_in  TYPE menge_d.
      DATA lv_conv_menge_out TYPE menge_d.
      DATA lv_conv_ok        TYPE abap_bool.

      CLEAR: lv_conv_str, lv_conv_menge_in, lv_conv_menge_out.
      lv_conv_ok = abap_true.
      ls_out-menge = 0.

      lv_conv_str = ls_pair-menge.
      CONDENSE lv_conv_str NO-GAPS.

      IF lv_conv_str IS NOT INITIAL.
        TRY.
            lv_conv_menge_in = lv_conv_str.
          CATCH cx_sy_conversion_error.
            lv_conv_ok = abap_false.
        ENDTRY.

        IF lv_conv_ok = abap_true.
          IF lv_meins_komp <> ls_pair-meins
             AND lv_meins_komp IS NOT INITIAL
             AND ls_pair-meins IS NOT INITIAL.
            CALL FUNCTION 'UNIT_CONVERSION_SIMPLE'
              EXPORTING
                input                = lv_conv_menge_in
                unit_in              = ls_pair-meins
                unit_out             = lv_meins_komp
              IMPORTING
                output               = lv_conv_menge_out
              EXCEPTIONS
                conversion_not_found = 1
                division_by_zero     = 2
                input_invalid        = 3
                output_invalid       = 4
                overflow             = 5
                type_invalid         = 6
                units_missing        = 7
                unit_in_not_found    = 8
                unit_out_not_found   = 9
                OTHERS               = 10.
            ls_out-menge = COND #( WHEN sy-subrc = 0 THEN lv_conv_menge_out
                                   ELSE lv_conv_menge_in ).
          ELSE.
            ls_out-menge = lv_conv_menge_in.
          ENDIF.
          ls_out-menge = round( val = ls_out-menge dec = 0 ).
        ENDIF.
      ENDIF.
      " ---- Ende Mengenkonvertierung ----

      READ TABLE lt_exklusiv TRANSPORTING NO FIELDS
        WITH TABLE KEY table_line = ls_pair-kompnr.
      ls_out-exklusiv = COND #( WHEN sy-subrc = 0 THEN abap_true ELSE abap_false ).

      READ TABLE lt_md04 INTO DATA(ls_md04)
        WITH TABLE KEY matnr = ls_pair-kompnr.
      IF sy-subrc = 0.
        ls_out-verbrauch    = ls_md04-verbr.
        ls_out-labst        = ls_md04-labst.
        ls_out-feste_zugang = ls_md04-feste_zug.
        ls_out-gepl_zugang  = ls_md04-gepl_zug.
        ls_out-feste_abgang = ls_md04-feste_abg.
        ls_out-gepl_abgang  = ls_md04-gepl_abg.
        ls_out-omeng        = ls_md04-omeng.
        ls_out-owert        = ls_md04-owert.
        ls_out-mkmng        = ls_md04-mkmng.
        ls_out-omkwr        = ls_md04-omkwr.

        ls_out-endbestand = ls_out-labst
                          + ls_out-feste_zugang + ls_out-gepl_zugang
                          - ls_out-feste_abgang - ls_out-gepl_abgang.

        ls_out-wert_feste_zug  = ls_out-feste_zugang * ls_out-stueckkosten.
        ls_out-wert_gepl_zug   = ls_out-gepl_zugang  * ls_out-stueckkosten.
        ls_out-wert_feste_abg  = ls_out-feste_abgang * ls_out-stueckkosten.
        ls_out-wert_gepl_abg   = ls_out-gepl_abgang  * ls_out-stueckkosten.
        ls_out-wert_endbestand = ls_out-endbestand   * ls_out-stueckkosten.
      ENDIF.

      APPEND ls_out TO lt_out.
    ENDLOOP.

    " ===================================================================
    " Schritt 9 (NEU, gateway-spezifisch): $skip/$top anwenden und in
    " et_entityset uebertragen. MOVE-CORRESPONDING, weil der generierte
    " Entity-Zeilentyp dieselben Feldnamen wie ZSTR_LZCODE_USAGE hat.
    " ===================================================================
    IF is_paging-skip > 0.
      DELETE lt_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      DELETE lt_out FROM is_paging-top + 1.
    ENDIF.

    LOOP AT lt_out INTO ls_out.
      APPEND INITIAL LINE TO et_entityset ASSIGNING FIELD-SYMBOL(<fs_entity>).
      MOVE-CORRESPONDING ls_out TO <fs_entity>.
    ENDLOOP.

ENDMETHOD.
