*&---------------------------------------------------------------------*
*& Class ZCL_LZCODE_PROVIDER  (Variante: GET_DATA vollstaendig inline)
*& Webservice-Provider fuer den Report ZM_LZCODE20_OPT (zlo03.txt)
*&
*& DRAFT fuer Lucas / SAP-Team - Syntaxcheck im System noch offen.
*&
*& Unterschied zu ZCL_LZCODE_PROVIDER.abap (Fables korrigierte v2):
*& Fachlich UNVERAENDERT (alle 8 Fixes aus v2 sind uebernommen), aber
*& strukturell zusammengelegt: GET_DATA enthaelt die komplette Logik
*& von load_stammdaten/load_md04/convert_menge direkt inline - keine
*& privaten Hilfsmethoden mehr. Grund: einfacher 1:1 in eine einzelne
*& redefinierte DPC-Methode (*_GET_ENTITYSET) zu uebernehmen, falls
*& keine zusaetzliche eigene Klasse angelegt werden soll.
*&
*& ACHTUNG beim Inlinen beruecksichtigt: load_stammdaten/load_md04/
*& convert_menge hatten in v2 eigene RETURN-Anweisungen (CHECK, TRY/
*& CATCH-RETURN). Als eigene Methoden war das sicher, weil RETURN nur
*& die jeweilige Methode verlaesst. Inline INNERHALB der Pro-Zeile-
*& Schleife (Schritt 9) waere ein RETURN falsch - es wuerde GET_DATA
*& komplett abbrechen und alle folgenden Vknr/Kompnr-Paare verschlucken.
*& Deshalb ist die Mengenkonvertierung hier als verschachteltes IF/ELSE
*& ohne RETURN nachgebaut (siehe Schritt 9), fachlich identisch zu v2.
*&
*& GET_PARENT_MATERIALS ist NICHT betroffen - die Methode war schon in
*& v2 vollstaendig self-contained (kein Aufruf von load_*/convert_menge)
*& und bleibt hier unveraendert als zweite oeffentliche Methode stehen.
*&
*& Rueckgabetypen auf globale DDIC-Strukturen (ZSTR_LZCODE_USAGE /
*& ZSTR_LZCODE_PARENT) umgestellt - MUESSEN in SE11 angelegt/aktiviert
*& sein, BEVOR diese Klasse aktiviert wird. Siehe
*& docs/abap/README_LZCODE_WEBSERVICE.md, Abschnitt "SE11 - Benoetigte
*& DDIC-Strukturen" (Feldliste, Typen, Warnungen zu ABAP_BOOL und
*& ZZLZCOD/ZZLZCODSORT).
*&
*& Feld WAERS ergaenzt (2026-07-21): OWERT/OMKWR sind CURR-Felder und
*& verlangen laut DDIC zwingend ein Referenzfeld fuer die Waehrung
*& (sonst keine Aktivierung der Struktur moeglich). GC_WERKS = '1100'
*& ist laut docs/FINANCE_STANDARDKOSTEN_2026-07-14.md Bewertungskreis
*& Trafag AG/CH/CHF - deshalb hier als Konstante gesetzt.
*&
*& Spezifikation, Feldbegruendung und Gateway-Registrierung:
*& docs/abap/README_LZCODE_WEBSERVICE.md
*&---------------------------------------------------------------------*
CLASS zcl_lzcode_provider DEFINITION
  PUBLIC
  FINAL
  CREATE PUBLIC.

  PUBLIC SECTION.

    CONSTANTS gc_werks TYPE werks_d VALUE '1100'.

    TYPES tt_matnr_range TYPE RANGE OF matnr.
    TYPES tt_typcd_range TYPE RANGE OF mara-zztyp_f4.

    "-------------------------------------------------------------
    " Haupt-Entity: eine Zeile je Kopfmaterial(Vknr)/Komponente(Kompnr)-Paar.
    " Ersetzt die dynamische Pivot-Matrix (VTAB x KTAB) des Reports durch
    " ein normalisiertes Zeilenformat - siehe README, Abschnitt "Warum
    " nicht die Pivot-Spalten 1:1 abbilden".
    "
    " ZSTR_LZCODE_USAGE ist eine GLOBALE DDIC-Struktur (SE11) - MUSS vor
    " dieser Klasse angelegt/aktiviert sein, siehe README Abschnitt
    " "SE11 - Benoetigte DDIC-Strukturen" (Feldliste, Typen, zwei
    " Warnungen zu ABAP_BOOL und ZZLZCOD/ZZLZCODSORT).
    "-------------------------------------------------------------
    TYPES tt_out TYPE STANDARD TABLE OF zstr_lzcode_usage WITH DEFAULT KEY.

    "-------------------------------------------------------------
    " Zweites Entity: Elternmaterialien je Komponente (Top-Down).
    " Unveraendert gegenueber v2 - siehe dort fuer Begruendung.
    " ZSTR_LZCODE_PARENT ist ebenfalls eine globale DDIC-Struktur (SE11).
    "-------------------------------------------------------------
    TYPES tt_parent_out TYPE STANDARD TABLE OF zstr_lzcode_parent WITH DEFAULT KEY.

    METHODS get_data
      IMPORTING
        iv_topdown    TYPE abap_bool DEFAULT abap_true
        it_typcd      TYPE tt_typcd_range OPTIONAL
        it_matnr      TYPE tt_matnr_range OPTIONAL
      RETURNING
        VALUE(rt_out) TYPE tt_out.

    METHODS get_parent_materials
      IMPORTING
        it_kompnr     TYPE tt_matnr_range OPTIONAL
        it_vknr       TYPE tt_matnr_range OPTIONAL  " selektierte VKNRs, siehe README
      RETURNING
        VALUE(rt_out) TYPE tt_parent_out.

ENDCLASS.


CLASS zcl_lzcode_provider IMPLEMENTATION.

  METHOD get_data.

    " ===================================================================
    " Lokale Typen - frueher PRIVATE SECTION bzw. Signatur der privaten
    " Hilfsmethoden, jetzt nur noch innerhalb dieser Methode bekannt.
    " ===================================================================
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

    "-------------------------------------------------------------
    " FIX 2 (aus v2): menge/meins bleiben Charfelder wie in ZAT_VC.
    " menge_d haette beim SELECT INTO CORRESPONDING FIELDS einen
    " CONVT_NO_NUMBER-Dump bei nicht-numerischen Inhalten ausgeloest,
    " BEVOR die Mengenkonvertierung (TRY/CATCH, Schritt 9) greift.
    "-------------------------------------------------------------
    TYPES: BEGIN OF ty_pair,
             vknr   TYPE matnr,
             kompnr TYPE matnr,
             menge  TYPE zat_vc-menge,          " Char, NICHT menge_d!
             meins  TYPE zat_vc-mengeneinheit,  " ME lt. ZAT_VC (vor Umrechnung)
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
          lv_richtung       TYPE char10.

    lv_richtung = COND #( WHEN iv_topdown = abap_true THEN 'TOPDOWN' ELSE 'BOTTOMUP' ).

    " -----------------------------------------------------------
    " Schritt 1: Materialselektion (mirrort zlo03.txt process_main).
    " Top-Down bewusst nicht auf MTART = 'FERT' eingeschraenkt - der
    " Original-Report hat diesen Filter am 2.3.26 deaktiviert.
    " it_typcd/it_matnr kommen aus dem OData-$filter und sind ueber-
    " schaubar gross -> direktes IN ist hier ok.
    " -----------------------------------------------------------
    IF it_typcd IS NOT INITIAL.
      SELECT matnr FROM mara
        INTO TABLE @lt_mara_sel
        WHERE zztyp_f4 IN @it_typcd
          AND matnr IN @it_matnr
          AND lvorm = @space.
    ELSEIF it_matnr IS NOT INITIAL.
      SELECT matnr FROM mara
        INTO TABLE @lt_mara_sel
        WHERE matnr IN @it_matnr
          AND lvorm = @space.
    ELSE.
      RETURN.
    ENDIF.

    IF lt_mara_sel IS INITIAL.
      RETURN.
    ENDIF.

    " -----------------------------------------------------------
    " Schritt 2: ZAT_VC lesen (Rollen je Richtung getauscht, wie im Report)
    " -----------------------------------------------------------
    IF iv_topdown = abap_true.
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

    " -----------------------------------------------------------
    " FIX 6 (aus v2): Deterministisches Dedup - menge DESCENDING sorgt
    " dafuer, dass bei Duplikaten mit abweichender Menge immer dieselbe
    " Zeile ueberlebt (Report laesst das per HASHED-Zufall offen).
    " -----------------------------------------------------------
    SORT lt_pairs_raw BY vknr kompnr menge DESCENDING.
    DELETE ADJACENT DUPLICATES FROM lt_pairs_raw COMPARING vknr kompnr.
    lt_pairs = lt_pairs_raw.

    " -----------------------------------------------------------
    " Schritt 3: Alle beteiligten Materialnummern sammeln (Vknr + Kompnr)
    " als tt_matnr_list fuer FAE (FIX 3 aus v2: keine Riesen-Ranges mehr)
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
    " Schritt 4: Stammdaten bulk laden (frueher METHOD load_stammdaten,
    " jetzt inline - unveraendertes FIX-3-Verhalten: FAE auf tt_matnr_list
    " statt IN auf potentiell riesiger Range).
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
        LEFT JOIN marc AS c ON c~matnr = m~matnr AND c~werks = @gc_werks
        LEFT JOIN mbew AS b ON b~matnr = m~matnr AND b~bwkey = @gc_werks
                           AND b~bwtar = @space
        INTO CORRESPONDING FIELDS OF TABLE @lt_stamm_raw
        FOR ALL ENTRIES IN @lt_all_matnr
        WHERE m~matnr = @lt_all_matnr-matnr
          AND m~lvorm = @space.

      SELECT matnr FROM mast
        INTO TABLE @lt_bom
        FOR ALL ENTRIES IN @lt_all_matnr
        WHERE matnr = @lt_all_matnr-matnr
          AND werks = @gc_werks
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

        " Stueckkosten: gleitender Durchschnitt (VPRSV = 'V'), sonst
        " Standardpreis - wie fill_ktab in zlo03.txt.
        ls_stamm-stueckkosten =
          COND #( WHEN ls_stamm-vprsv = 'V'
                  THEN ls_stamm-verpr / ls_stamm-peinh
                  ELSE ls_stamm-stprs / ls_stamm-peinh ).

        INSERT ls_stamm INTO TABLE lt_stamm.
      ENDLOOP.
    ENDIF.

    " ===================================================================
    " Schritt 5: MD04-Bestandsdaten bulk laden (frueher METHOD load_md04,
    " jetzt inline). Liest ausschliesslich die vorberechnete Tabelle
    " ZMD04_CALC, kein Live-MD04-Aufbau - wie zlo03.txt FORM load_md04_bulk.
    " ===================================================================
    IF lt_all_matnr IS NOT INITIAL.
      DATA lt_md04_raw TYPE STANDARD TABLE OF zmd04_calc.

      SELECT matnr, werks, labst, feste_zug, gepl_zug, feste_abg, gepl_abg,
             verbr, omeng, owert, mkmng, omkwr
        FROM zmd04_calc
        INTO CORRESPONDING FIELDS OF TABLE @lt_md04_raw
        FOR ALL ENTRIES IN @lt_all_matnr
        WHERE matnr = @lt_all_matnr-matnr
          AND werks = @gc_werks.

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
    " Schritt 6: Kopfmaterial-Zusatzfelder je Vknr einmalig ermitteln
    " (entspricht VTAB im Report)
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
    " Schritt 7: Exklusivitaet - nur fachlich belegt fuer Top-Down
    " (im Original-Report ist Bottom-Up-Exklusivitaet immer leer)
    " -----------------------------------------------------------
    DATA lt_exklusiv TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

    IF iv_topdown = abap_true.

      " Komponentenliste (distinct) fuer FAE
      DATA: lt_kompnr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
            lt_kompnr_list TYPE tt_matnr_list.
      LOOP AT lt_pairs INTO DATA(ls_pair_k).
        INSERT ls_pair_k-kompnr INTO TABLE lt_kompnr_temp.
      ENDLOOP.
      LOOP AT lt_kompnr_temp INTO lv_matnr.
        ls_matnr_line-matnr = lv_matnr.
        APPEND ls_matnr_line TO lt_kompnr_list.
      ENDLOOP.

      " Selektierte VKNRs
      DATA lt_sel_vknr TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.
      LOOP AT lt_pairs INTO DATA(ls_pair_v).
        INSERT ls_pair_v-vknr INTO TABLE lt_sel_vknr.
      ENDLOOP.

      " FIX 7 (aus v2): SORTED TABLE mit Key kompnr -> LOOP ... WHERE optimiert
      TYPES: BEGIN OF ty_usage,
               kompnr TYPE matnr,
               vknr   TYPE matnr,
             END OF ty_usage.
      DATA lt_usage_raw TYPE STANDARD TABLE OF ty_usage.
      DATA lt_usage     TYPE SORTED TABLE OF ty_usage
                          WITH NON-UNIQUE KEY kompnr vknr.

      " FIX 3 (aus v2): FAE auf Liste mit benanntem Feld statt "range-low"
      SELECT kompnr, matnr AS vknr
        FROM zat_vc
        INTO CORRESPONDING FIELDS OF TABLE @lt_usage_raw
        FOR ALL ENTRIES IN @lt_kompnr_list
        WHERE kompnr = @lt_kompnr_list-matnr.

      SORT lt_usage_raw BY kompnr vknr.
      DELETE ADJACENT DUPLICATES FROM lt_usage_raw COMPARING kompnr vknr.
      lt_usage = lt_usage_raw.
      FREE lt_usage_raw.

      " MTART/MSTAE der gefundenen VKNRs (nur FERT zaehlen, MSTAE 99 ignorieren)
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
    " Schritt 8: Ausgabezeilen je Vknr/Kompnr-Paar zusammenbauen.
    " Mengenkonvertierung (frueher METHOD convert_menge) ist HIER inline,
    " bewusst OHNE RETURN (siehe Header) - stattdessen verschachteltes
    " IF/ELSE, fachlich identisch zu v2/zum Report.
    " ===================================================================
    LOOP AT lt_pairs INTO DATA(ls_pair).

      " FIX 5 (aus v2, Verhaltensangleichung Bottom-Up): Report ueberspringt
      " Verwendungsmaterialien ohne (gueltigen) Stammsatz - der
      " Stammdaten-SELECT filtert lvorm = '' bereits, d.h. fehlender
      " Cache-Eintrag deckt Loeschvormerkung mit ab. Entspricht auch
      " dem "DELETE gt_ktab WHERE maktx IS INITIAL" des Reports.
      IF iv_topdown = abap_false.
        READ TABLE lt_stamm TRANSPORTING NO FIELDS
          WITH TABLE KEY matnr = ls_pair-vknr.
        IF sy-subrc <> 0.
          CONTINUE.
        ENDIF.
      ENDIF.

      " WAERS fest 'CHF' (GC_WERKS 1100 = Trafag AG/CH/CHF) - Referenzfeld
      " fuer die CURR-Felder OWERT/OMKWR, siehe Klassenkopf-Kommentar.
      DATA(ls_out) = VALUE zstr_lzcode_usage( richtung = lv_richtung
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

      " ---- Mengenkonvertierung inline (frueher METHOD convert_menge) ----
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
                overflow              = 5
                type_invalid          = 6
                units_missing         = 7
                unit_in_not_found     = 8
                unit_out_not_found    = 9
                OTHERS                = 10.
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

      APPEND ls_out TO rt_out.
    ENDLOOP.

  ENDMETHOD.


  METHOD get_parent_materials.

    DATA lt_usage TYPE STANDARD TABLE OF zstr_lzcode_parent.

    IF it_kompnr IS INITIAL.
      RETURN.
    ENDIF.

    " Entspricht load_elternmaterial_cache in zlo03.txt, deterministisch
    " sortiert und dedupliziert (unveraendert gegenueber v2).
    " it_vknr schraenkt wie im Report auf die selektierten VKNRs ein
    " (leer = keine Einschraenkung, dann MEHR Zeilen als im Excel).
    SELECT kompnr, kom_mstae AS eltern_matnr
      FROM zat_vc
      INTO CORRESPONDING FIELDS OF TABLE @lt_usage
      WHERE kompnr IN @it_kompnr
        AND matnr IN @it_vknr
        AND kom_mstae <> @space.

    SORT lt_usage BY kompnr eltern_matnr.
    DELETE ADJACENT DUPLICATES FROM lt_usage COMPARING kompnr eltern_matnr.

    rt_out = lt_usage.

  ENDMETHOD.

ENDCLASS.
