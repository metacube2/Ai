*&---------------------------------------------------------------------*
*& Class ZCL_LZCODE_PROVIDER  (korrigierte Fassung)
*& Webservice-Provider fuer den Report ZM_LZCODE20_OPT (zlo03.txt)
*&
*& DRAFT v2 fuer Lucas / SAP-Team - Syntaxcheck im System noch offen.
*& Aenderungen gegenueber v1:
*&   1. CLASS-POOL-Statement entfernt (wird von SE24/ADT generiert,
*&      darf nicht im Quelltext globaler Klassen stehen)
*&   2. ty_pair-menge bleibt Charfeld (TYPE zat_vc-menge) - verhindert
*&      CONVT_NO_NUMBER-Dump beim SELECT, Konvertierung erfolgt
*&      ausschliesslich ueber convert_menge (TRY/CATCH), wie im Report
*&   3. Kein FOR ALL ENTRIES mehr auf Range-Tabellen mit "-low"
*&      (Range-Semantik BT/CP/E wurde ignoriert -> stille Datenfehler).
*&      Interne Massenselektion laeuft jetzt ueber tt_matnr_list + FAE
*&      (vermeidet auch Statement-Explosion bei grossen IN-Ranges).
*&   4. get_parent_materials: neuer optionaler Parameter it_vknr -
*&      Report schraenkt Eltern auf die selektierten VKNRs ein
*&      (load_elternmaterial_cache: AND matnr IN lr_sel_vknr).
*&      Ohne it_vknr werden ALLE Verwendungen geliefert (dokumentiert).
*&   5. Bottom-Up: Paare ohne Stammsatz des Verwendungsmaterials werden
*&      uebersprungen (Report: CONTINUE bei fehlendem Cache-Eintrag /
*&      LVORM; Stammdaten-SELECT filtert lvorm = '' bereits).
*&   6. Dedup deterministisch: SORT ... menge DESCENDING, damit bei
*&      Duplikaten mit unterschiedlicher Menge immer dieselbe ueberlebt.
*&   7. Exklusivitaetspruefung: lt_usage als SORTED TABLE mit Key kompnr
*&      -> LOOP ... WHERE ist O(log n) statt O(n) pro Komponente.
*&   8. iv_days entfernt - war tot (Report nutzt gv_datum_von/bis seit
*&      Umstellung auf ZMD04_CALC nicht mehr; Verbrauchszeitraum wird
*&      vom naechtlichen Materializer bestimmt).
*&   9. Rueckgabetypen auf globale DDIC-Strukturen umgestellt
*&      (ZSTR_LZCODE_USAGE / ZSTR_LZCODE_PARENT) statt lokaler TYPES -
*&      noetig, damit SEGW die Entity Types per "DDIC-Struktur
*&      importieren" uebernehmen kann. VORAUSSETZUNG: beide Strukturen
*&      muessen in SE11 angelegt/aktiviert sein, BEVOR diese Klasse
*&      aktiviert wird - siehe docs/abap/README_LZCODE_WEBSERVICE.md,
*&      Abschnitt "SE11 - Benoetigte DDIC-Strukturen".
*&   10. Feld WAERS ergaenzt (2026-07-21): OWERT/OMKWR sind CURR-Felder
*&      und verlangen laut DDIC zwingend ein Referenzfeld fuer die
*&      Waehrung (sonst keine Aktivierung der Struktur moeglich).
*&      GC_WERKS = '1100' ist laut docs/FINANCE_STANDARDKOSTEN_2026-07-14.md
*&      Bewertungskreis Trafag AG/CH/CHF - deshalb hier als Konstante
*&      gesetzt, keine Herleitung noetig/vorhanden.
*&
*& Bewusst NICHT abgebildet (siehe README):
*&   - "Weitere VKNRs" (p_wvknr) - bei Bedarf drittes EntitySet
*&   - Excel-Formeln / Spalten-Details (Client-Sache)
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
    " Original-Report baut das als komma-separierten String aus einer
    " HASHED TABLE ohne SORT zusammen (FORM get_elternmaterial) -
    " nicht-deterministische Reihenfolge. Hier: sortiert + dedupliziert.
    "
    " WICHTIG: Der Report schraenkt die Eltern auf die selektierten
    " VKNRs ein. Der DPC MUSS dafuer it_vknr aus der Hauptselektion
    " mitgeben - ohne it_vknr liefert die Methode Eltern aus ALLEN
    " Verwendungen (mehr Zeilen als im Excel des Reports!).
    "
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
        it_vknr       TYPE tt_matnr_range OPTIONAL  " selektierte VKNRs (s.o.)
      RETURNING
        VALUE(rt_out) TYPE tt_parent_out.

  PRIVATE SECTION.

    " Fuer FOR ALL ENTRIES - vermeidet grosse IN-Ranges (Statement-Limit)
    " und die falsche Range-Semantik von "FAE ... = range-low".
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
    " FIX 2: menge/meins bleiben Charfelder wie in ZAT_VC.
    " menge_d hier haette beim SELECT INTO CORRESPONDING FIELDS
    " einen CONVT_NO_NUMBER-Dump bei nicht-numerischen Inhalten
    " ausgeloest, BEVOR convert_menge (TRY/CATCH) greift.
    "-------------------------------------------------------------
    TYPES: BEGIN OF ty_pair,
             vknr   TYPE matnr,
             kompnr TYPE matnr,
             menge  TYPE zat_vc-menge,          " Char, NICHT menge_d!
             meins  TYPE zat_vc-mengeneinheit,  " ME lt. ZAT_VC (vor Umrechnung)
           END OF ty_pair.
    TYPES tt_pair TYPE STANDARD TABLE OF ty_pair WITH DEFAULT KEY.

    METHODS load_stammdaten
      IMPORTING it_matnr        TYPE tt_matnr_list
      RETURNING VALUE(rt_stamm) TYPE tt_stamm.

    METHODS load_md04
      IMPORTING it_matnr       TYPE tt_matnr_list
      RETURNING VALUE(rt_md04) TYPE tt_md04.

    METHODS convert_menge
      IMPORTING
        iv_menge_str    TYPE any
        iv_meins_in     TYPE meins
        iv_meins_out    TYPE meins
      RETURNING
        VALUE(rv_menge) TYPE menge_d.

ENDCLASS.


CLASS zcl_lzcode_provider IMPLEMENTATION.

  METHOD get_data.

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
    " FIX 6: Deterministisches Dedup - menge DESCENDING sorgt dafuer,
    " dass bei Duplikaten mit abweichender Menge immer dieselbe Zeile
    " ueberlebt (Report laesst das per HASHED-Zufall offen).
    " -----------------------------------------------------------
    SORT lt_pairs_raw BY vknr kompnr menge DESCENDING.
    DELETE ADJACENT DUPLICATES FROM lt_pairs_raw COMPARING vknr kompnr.
    lt_pairs = lt_pairs_raw.

    " -----------------------------------------------------------
    " Schritt 3: Alle beteiligten Materialnummern sammeln (Vknr + Kompnr)
    " als tt_matnr_list fuer FAE (FIX 3: keine Riesen-Ranges mehr)
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

    " -----------------------------------------------------------
    " Schritt 4/5/6: Stamm- und Bestandsdaten bulk laden
    " -----------------------------------------------------------
    lt_stamm = load_stammdaten( lt_all_matnr ).
    lt_md04  = load_md04( lt_all_matnr ).

    " -----------------------------------------------------------
    " Schritt 7: Kopfmaterial-Zusatzfelder je Vknr einmalig ermitteln
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
    " Schritt 8: Exklusivitaet - nur fachlich belegt fuer Top-Down
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

      " FIX 7: SORTED TABLE mit Key kompnr -> LOOP ... WHERE optimiert
      TYPES: BEGIN OF ty_usage,
               kompnr TYPE matnr,
               vknr   TYPE matnr,
             END OF ty_usage.
      DATA lt_usage_raw TYPE STANDARD TABLE OF ty_usage.
      DATA lt_usage     TYPE SORTED TABLE OF ty_usage
                          WITH NON-UNIQUE KEY kompnr vknr.

      " FIX 3: FAE auf Liste mit benanntem Feld statt "range-low"
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

    " -----------------------------------------------------------
    " Schritt 9: Ausgabezeilen je Vknr/Kompnr-Paar zusammenbauen
    " -----------------------------------------------------------
    LOOP AT lt_pairs INTO DATA(ls_pair).

      " FIX 5 (Verhaltensangleichung Bottom-Up): Report ueberspringt
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
      " fuer die CURR-Felder OWERT/OMKWR, siehe Klassenkopf-Kommentar Fix 10.
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
      READ TABLE lt_stamm INTO DATA(ls_stamm)
        WITH TABLE KEY matnr = ls_pair-kompnr.
      IF sy-subrc = 0.
        lv_meins_komp        = ls_stamm-meins.
        ls_out-kompnr_maktx  = ls_stamm-maktx.
        ls_out-kompnr_meins  = ls_stamm-meins.
        ls_out-dismm         = ls_stamm-dismm.
        ls_out-minbe         = ls_stamm-minbe.
        ls_out-disls         = ls_stamm-disls.
        ls_out-bstfe         = ls_stamm-bstfe.
        ls_out-eisbe         = ls_stamm-eisbe.
        ls_out-mstae         = ls_stamm-mstae.
        ls_out-mstav         = ls_stamm-mstav.
        ls_out-beskz         = ls_stamm-beskz.
        ls_out-zzlzcod       = ls_stamm-zzlzcod.
        ls_out-zzlzcodsort   = ls_stamm-zzlzcodsort.
        ls_out-stueckkosten  = ls_stamm-stueckkosten.
        ls_out-baugruppe     = ls_stamm-has_bom.
      ELSE.
        lv_meins_komp = ls_pair-meins.
      ENDIF.

      ls_out-menge = convert_menge( iv_menge_str = ls_pair-menge
                                    iv_meins_in  = ls_pair-meins
                                    iv_meins_out = lv_meins_komp ).

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
    " sortiert und dedupliziert.
    " FIX 3: direktes IN statt FAE auf range-low - so bleibt die volle
    " Range-Semantik (EQ/BT/CP/E) erhalten. Die Ranges kommen aus dem
    " OData-$filter und sind ueberschaubar; sollte der DPC hier jemals
    " tausende EQ-Zeilen uebergeben, muss auf FAE-Liste umgebaut werden.
    " FIX 4: it_vknr schraenkt wie im Report auf die selektierten VKNRs
    " ein (leer = keine Einschraenkung, dann MEHR Zeilen als im Excel).
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


  METHOD load_stammdaten.

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
    DATA lt_raw TYPE STANDARD TABLE OF ty_stamm_raw.
    DATA lt_bom TYPE STANDARD TABLE OF matnr.

    CHECK it_matnr IS NOT INITIAL.

    " FIX 3: FAE auf tt_matnr_list statt IN auf potentiell riesiger Range
    SELECT m~matnr, t~maktx, m~meins, m~mstae, m~mstav, m~lvorm,
           m~zzlzcod, m~zzlzcodsort,
           c~dismm, c~minbe, c~disls, c~bstfe, c~eisbe, c~beskz,
           b~verpr, b~stprs, b~peinh, b~vprsv
      FROM mara AS m
      LEFT JOIN makt AS t ON t~matnr = m~matnr AND t~spras = @sy-langu
      LEFT JOIN marc AS c ON c~matnr = m~matnr AND c~werks = @gc_werks
      LEFT JOIN mbew AS b ON b~matnr = m~matnr AND b~bwkey = @gc_werks
                         AND b~bwtar = @space
      INTO CORRESPONDING FIELDS OF TABLE @lt_raw
      FOR ALL ENTRIES IN @it_matnr
      WHERE m~matnr = @it_matnr-matnr
        AND m~lvorm = @space.

    SELECT matnr FROM mast
      INTO TABLE @lt_bom
      FOR ALL ENTRIES IN @it_matnr
      WHERE matnr = @it_matnr-matnr
        AND werks = @gc_werks
        AND stlan = '1'.
    SORT lt_bom.
    DELETE ADJACENT DUPLICATES FROM lt_bom.

    LOOP AT lt_raw INTO DATA(ls_raw).
      DATA(ls_stamm) = VALUE ty_stamm(
        matnr       = ls_raw-matnr
        maktx       = ls_raw-maktx
        meins       = ls_raw-meins
        mstae       = ls_raw-mstae
        mstav       = ls_raw-mstav
        lvorm       = ls_raw-lvorm
        zzlzcod     = ls_raw-zzlzcod
        zzlzcodsort = ls_raw-zzlzcodsort
        dismm       = ls_raw-dismm
        minbe       = ls_raw-minbe
        disls       = ls_raw-disls
        bstfe       = ls_raw-bstfe
        eisbe       = ls_raw-eisbe
        beskz       = ls_raw-beskz
        verpr       = ls_raw-verpr
        stprs       = ls_raw-stprs
        peinh       = COND #( WHEN ls_raw-peinh IS INITIAL THEN 1
                              ELSE ls_raw-peinh )
        vprsv       = ls_raw-vprsv ).

      READ TABLE lt_bom TRANSPORTING NO FIELDS
        WITH KEY table_line = ls_raw-matnr BINARY SEARCH.
      ls_stamm-has_bom = COND #( WHEN sy-subrc = 0 THEN abap_true
                                 ELSE abap_false ).

      " Stueckkosten: gleitender Durchschnitt (VPRSV = 'V'), sonst
      " Standardpreis - wie fill_ktab in zlo03.txt.
      ls_stamm-stueckkosten =
        COND #( WHEN ls_stamm-vprsv = 'V'
                THEN ls_stamm-verpr / ls_stamm-peinh
                ELSE ls_stamm-stprs / ls_stamm-peinh ).

      INSERT ls_stamm INTO TABLE rt_stamm.
    ENDLOOP.

  ENDMETHOD.


  METHOD load_md04.

    DATA lt_raw TYPE STANDARD TABLE OF zmd04_calc.

    CHECK it_matnr IS NOT INITIAL.

    " Wie in zlo03.txt FORM load_md04_bulk: liest ausschliesslich die
    " vorberechnete Tabelle ZMD04_CALC, kein Live-MD04-Aufbau.
    " FIX 3: FAE auf tt_matnr_list statt IN auf Range.
    SELECT matnr, werks, labst, feste_zug, gepl_zug, feste_abg, gepl_abg,
           verbr, omeng, owert, mkmng, omkwr
      FROM zmd04_calc
      INTO CORRESPONDING FIELDS OF TABLE @lt_raw
      FOR ALL ENTRIES IN @it_matnr
      WHERE matnr = @it_matnr-matnr
        AND werks = @gc_werks.

    LOOP AT lt_raw INTO DATA(ls_raw).
      INSERT VALUE ty_md04( matnr     = ls_raw-matnr
                            labst     = ls_raw-labst
                            feste_zug = ls_raw-feste_zug
                            gepl_zug  = ls_raw-gepl_zug
                            feste_abg = ls_raw-feste_abg
                            gepl_abg  = ls_raw-gepl_abg
                            verbr     = ls_raw-verbr
                            omeng     = ls_raw-omeng
                            owert     = ls_raw-owert
                            mkmng     = ls_raw-mkmng
                            omkwr     = ls_raw-omkwr )
        INTO TABLE rt_md04.
    ENDLOOP.

  ENDMETHOD.


  METHOD convert_menge.

    DATA lv_str       TYPE string.
    DATA lv_menge_in  TYPE menge_d.
    DATA lv_menge_out TYPE menge_d.

    lv_str = iv_menge_str.
    CONDENSE lv_str NO-GAPS.

    IF lv_str IS INITIAL.
      rv_menge = 0.
      RETURN.
    ENDIF.

    TRY.
        lv_menge_in = lv_str.
      CATCH cx_sy_conversion_error.
        rv_menge = 0.
        RETURN.
    ENDTRY.

    IF iv_meins_in <> iv_meins_out
       AND iv_meins_in IS NOT INITIAL
       AND iv_meins_out IS NOT INITIAL.
      CALL FUNCTION 'UNIT_CONVERSION_SIMPLE'
        EXPORTING
          input                = lv_menge_in
          unit_in              = iv_meins_in
          unit_out             = iv_meins_out
        IMPORTING
          output               = lv_menge_out
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
      rv_menge = COND #( WHEN sy-subrc = 0 THEN lv_menge_out
                         ELSE lv_menge_in ).
    ELSE.
      rv_menge = lv_menge_in.
    ENDIF.

    rv_menge = round( val = rv_menge dec = 0 ).

  ENDMETHOD.

ENDCLASS.
