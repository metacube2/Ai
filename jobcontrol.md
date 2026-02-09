*&---------------------------------------------------------------------*
*& Report ZMM_LIFECYCLE_CODE_INHERITANCE
*&---------------------------------------------------------------------*
*& Lebenszykluscode-Vererbung für Materialstammdaten
*& Optimiert für große Stücklisten und Massendatenverarbeitung
*&---------------------------------------------------------------------*
REPORT zmm_lifecycle_code_inheritance.
TYPES: ty_percentage TYPE p LENGTH 8 DECIMALS 2. " <-- NEUE ZEILE HINZUFÜGEN
*----------------------------------------------------------------------*
* Typdefinitionen
*----------------------------------------------------------------------*
TYPES: BEGIN OF ty_material,
         matnr       TYPE matnr,
         mtart       TYPE mtart,
         zzlzcod     TYPE char4,
         zzlzcodsort TYPE char4,      " NEU: Sortiments-Code
         zztyp_f4    TYPE char1,
         pstat       TYPE pstat_d,
         mstae       TYPE mstae,
         level       TYPE i,
         disst       TYPE dismm,
       END OF ty_material.
TYPES: BEGIN OF ty_mattab,
         matnr     TYPE matnr,
         mtart     TYPE mtart,
         disst     TYPE dismm,
         lzcod     TYPE char4,
         lzcodsort TYPE char4,
         matnr_top TYPE matnr,
         menge     TYPE stpo-menge,
       END OF ty_mattab.

DATA: mattab       TYPE STANDARD TABLE OF ty_mattab WITH HEADER LINE,
      mattab2      TYPE STANDARD TABLE OF ty_mattab WITH HEADER LINE,
      mattab_dummy TYPE ty_mattab,
      imara        TYPE mara,
      stb          TYPE STANDARD TABLE OF stpox,
      bg           TYPE STANDARD TABLE OF cscmat,
      c_werks      TYPE werks_d VALUE '1100'.
TYPES: BEGIN OF ty_bom_relation,
         parent TYPE matnr,
         child  TYPE matnr,
         menge  TYPE stpo-menge,
         level  TYPE i,
       END OF ty_bom_relation.

TYPES: BEGIN OF ty_usage,
         matnr       TYPE matnr,
         parent      TYPE matnr,
         parent_code TYPE char4,
         usage_qty   TYPE kmpmg,
       END OF ty_usage.

TYPES: BEGIN OF ty_result,
         matnr         TYPE matnr,
         old_code      TYPE char4,
         new_code      TYPE char4,
         old_code_sort TYPE char4,    " NEU
         new_code_sort TYPE char4,    " NEU

         changed_sort  TYPE abap_bool, " NEU
         changed       TYPE abap_bool,
         message       TYPE string,
       END OF ty_result.

TYPES: BEGIN OF ty_consumption,
         matnr    TYPE matnr,
         menge    TYPE menge_d,

         gsv01    TYPE mver-gsv01, " NEU: Originalverbrauch aus VERBRAUCH_SUMMIEREN
         gsv_korr TYPE mver-gsv01, " NEU: Korrigierter Verbrauch nach MSEG-Logik

       END OF ty_consumption.

* Hash-Tabellen für Performance
TYPES: tt_material_hash TYPE HASHED TABLE OF ty_material
                        WITH UNIQUE KEY matnr.
TYPES: tt_bom_hash TYPE HASHED TABLE OF ty_bom_relation
                   WITH UNIQUE KEY parent child.
TYPES: tt_usage_std TYPE STANDARD TABLE OF ty_usage.
TYPES: tt_result TYPE STANDARD TABLE OF ty_result.

*----------------------------------------------------------------------*
* Konstanten
*----------------------------------------------------------------------*
CONSTANTS: gc_max_hierarchy_level TYPE i VALUE 4,
           gc_batch_size          TYPE i VALUE 5000,
           gc_max_iterations      TYPE i VALUE 10,
           gc_code_initial        TYPE char4 VALUE 'ZZZZ',
           gc_code_normal         TYPE char1 VALUE 'N',
           gc_code_auslauf        TYPE char1 VALUE 'A',
           gc_code_ersatz         TYPE char1 VALUE 'E',
           gc_code_sonder         TYPE char1 VALUE 'S'.

*----------------------------------------------------------------------*
* Globale Datendeklarationen
*----------------------------------------------------------------------*
DATA: gt_materials     TYPE tt_material_hash,
      gt_bom_relations TYPE tt_bom_hash,
      gt_usages        TYPE tt_usage_std,
      gt_results       TYPE tt_result,
      gt_consumption   TYPE HASHED TABLE OF ty_consumption
                       WITH UNIQUE KEY matnr.


DATA: p_mm    TYPE abap_bool,
      p_di    TYPE abap_bool,
      p_gozin TYPE abap_bool.

TABLES mara.
*----------------------------------------------------------------------*
* Selektionsbildschirm
*----------------------------------------------------------------------*
SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE TEXT-001.
  SELECT-OPTIONS: s_matnr FOR mara-matnr,
                  s_typcd FOR mara-mtart.
  PARAMETERS: "p_alles TYPE abap_bool AS CHECKBOX DEFAULT '',
    " p_gozin TYPE abap_bool AS CHECKBOX DEFAULT '',
    p_upda TYPE abap_bool AS CHECKBOX DEFAULT 'X',
    p_test TYPE abap_bool AS CHECKBOX DEFAULT ' '.
SELECTION-SCREEN END OF BLOCK b1.
*
*SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE TEXT-002.
*  PARAMETERS: p_batch TYPE i DEFAULT 5000,
*              p_maxlv TYPE i DEFAULT 4.
*SELECTION-SCREEN END OF BLOCK b2.



* HIER NEUEN BLOCK EINFÜGEN
SELECTION-SCREEN BEGIN OF BLOCK b3 WITH FRAME TITLE TEXT-003.
  PARAMETERS: p_lzc  TYPE abap_bool RADIOBUTTON GROUP mode , "Lebenszykluscode
              p_sort TYPE abap_bool RADIOBUTTON GROUP mode DEFAULT 'X'.             "Sortimentscode
SELECTION-SCREEN END OF BLOCK b3.
* SELECTION-SCREEN BEGIN OF BLOCK b4 WITH FRAME TITLE TEXT-004.
*   PARAMETERS: p_mm TYPE abap_bool RADIOBUTTON GROUP moe2 , "Lebenszykluscode
*               p_di TYPE abap_bool RADIOBUTTON GROUP moe2 DEFAULT 'X'.             "Sortimentscode
* SELECTION-SCREEN END OF BLOCK b4.
*----------------------------------------------------------------------*
* Klassendeklaration für Hauptlogik
*----------------------------------------------------------------------*
CLASS lcl_lifecycle_processor DEFINITION.
  PUBLIC SECTION.
    METHODS: constructor,
      execute,

      get_code_percentage
        IMPORTING iv_code              TYPE char4
        RETURNING VALUE(rv_percentage) TYPE ty_percentage ,

      display_gozinto_graph IMPORTING iv_top_material TYPE matnr,

      display_results.

  PRIVATE SECTION.
    " ==================== TYPDEFINITIONEN ====================

    " Node-Struktur für Gozinto-Graph
    TYPES: BEGIN OF ty_node,
             node_key       TYPE matnr,
             parent_key     TYPE matnr,
             matnr          TYPE matnr,
             menge          TYPE kmpmg,
             zzlzcod        TYPE char4,
             level          TYPE i,
             zzlzcodsort    TYPE char4,
             verbrauch_text TYPE string,
           END OF ty_node.


    TYPES: tt_nodes TYPE STANDARD TABLE OF ty_node WITH EMPTY KEY.

    " Material-Level für hierarchische Sortierung
    TYPES: BEGIN OF ty_material_level,
             matnr TYPE matnr,
             level TYPE i,
           END OF ty_material_level.


    TYPES: tt_material_level TYPE STANDARD TABLE OF ty_material_level
                             WITH NON-UNIQUE DEFAULT KEY.



    " ==================== DATENDEKLARATIONEN ====================




    " BDC-Daten für MM02-Transaktion
    DATA: gt_bdcdata TYPE TABLE OF bdcdata,
          gt_bdcmsg  TYPE TABLE OF bdcmsgcoll.

    " Progress-Indicator (optional)
    DATA: mo_progress TYPE REF TO cl_progress_indicator.







    " ==================== METHODENDEKLARATIONEN ====================

    " === HAUPTMETHODEN ===
    METHODS: load_materials,
      load_consumption_data,
      calculate_inheritance,
      calculate_sortiment_inhe,
      update_database,
      update_database_sortiment.

    " === HIERARCHIE-METHODEN ===
    METHODS:

      build_hierarchy_from_stb
        IMPORTING iv_top_material TYPE matnr OPTIONAL,







      transfer_to_new_structures
        IMPORTING iv_top_material TYPE matnr OPTIONAL,

      build_graph_nodes
        IMPORTING iv_matnr      TYPE matnr
                  iv_parent_key TYPE matnr
                  iv_menge      TYPE kmpmg
                  iv_level      TYPE i
        CHANGING  ct_nodes      TYPE tt_nodes.

    " === VERWENDUNGS-METHODEN ===
    METHODS: load_usages_for_components .


    " === BERECHNUNGS-METHODEN ===
    METHODS: apply_dominance_rules
      IMPORTING iv_matnr       TYPE matnr
      RETURNING VALUE(rv_code) TYPE char4,

      apply_sortiment_rules
        IMPORTING iv_matnr       TYPE matnr
        RETURNING VALUE(rv_code) TYPE char4,

      calculate_sonder_percentage
        IMPORTING iv_matnr             TYPE matnr
                  iv_type              TYPE char2 OPTIONAL
        RETURNING VALUE(rv_percentage) TYPE ty_percentage.



    " === STÜCKLISTEN-METHODEN ===
    METHODS: read_bill_of_material
      IMPORTING iv_matnr TYPE matnr,


      " === DATENBANK-UPDATE-METHODEN ===




      bdc_dynpro
        IMPORTING
          iv_program TYPE bdc_prog
          iv_dynpro  TYPE bdc_dynr,

      bdc_field
        IMPORTING
          iv_fnam TYPE fnam_____4
          iv_fval TYPE bdc_fval,

      bdc_transaction
        IMPORTING
          iv_tcode TYPE tcode,

      Write_to_journal
        IMPORTING
          iv_matnr         TYPE matnr
          iv_old_lzcod     TYPE char4
          iv_new_lzcod     TYPE char4
          iv_old_lzcodsort TYPE char4 OPTIONAL
          iv_new_lzcodsort TYPE char4 OPTIONAL.

    " === DEBUG-METHODEN ===
    METHODS: debug_show_relations,

      is_debug_material
        IMPORTING iv_matnr        TYPE matnr
        RETURNING VALUE(rv_debug) TYPE abap_bool,

      debug_problem_material
        IMPORTING
          iv_matnr TYPE matnr
          iv_phase TYPE string,

      debug_collect_info
        IMPORTING iv_matnr       TYPE matnr
        RETURNING VALUE(rt_info) TYPE string_table.



    " === HILFS-METHODEN ===

ENDCLASS.

*----------------------------------------------------------------------*
* Klassenimplementierung
*----------------------------------------------------------------------*
CLASS lcl_lifecycle_processor IMPLEMENTATION.

  METHOD constructor.
    "CREATE OBJECT mo_progress.
  ENDMETHOD.

  METHOD execute.
    DATA: lv_start_time TYPE timestampl,
          lv_end_time   TYPE timestampl.

    GET TIME STAMP FIELD lv_start_time.

    " *** NEU: Job deaktivieren (nur produktiv) ***"
    PERFORM deactivate_job.

    " Phase 1: Datenladen"
    ""Write: / 'Phase 1: Lade Materialstammdaten...'.
    load_materials( ).

    " ===== HIER EINFÜGEN: Debug nach dem Laden ====="
    me->debug_show_relations( ).
    ""Write: / 'Phase 2: Lade Stücklistenbeziehungen...'.


    ""Write: / 'Phase 3: Erweitere Hierarchie...'.
    "expand_hierarchy( ).

    ""Write: / 'Phase 4: Lade Verbrauchsdaten...'.
    load_consumption_data( ).

    " Phase 5: Berechnungen basierend auf Auswahl durchführen"
    IF p_lzc = abap_true.
      " Modus: Nur Lebenszykluscode (ZZLZCOD)"
      ""Write: / 'Phase 5: Berechne Lebenszykluscode-Vererbung (ZZLZCOD)...'.
      PERFORM save_vknr_codes.
      calculate_inheritance( ).
      PERFORM restore_vknr_codes.

      " PERFORM protect_vknr_from_update USING '1'."
      IF p_upda = abap_true AND p_test = abap_false.
        ""Write: / 'Phase 6: Aktualisiere Datenbank für ZZLZCOD...'.
        update_database( ).
      ENDIF.

    ELSEIF p_sort = abap_true.
      " Modus: Nur Sortimentscode (ZZLZCODSORT)"
      ""Write: / 'Phase 5: Berechne Sortiments-Lebenszykluscode-Vererbung (ZZLZCODSORT)...'.
      calculate_sortiment_inhe( ).

      IF p_upda = abap_true AND p_test = abap_false.
        ""Write: / 'Phase 6: Aktualisiere Datenbank für ZZLZCODSORT...'.
        update_database_sortiment( ).
      ENDIF.
    ENDIF.


    GET TIME STAMP FIELD lv_end_time.
    DATA(lv_runtime) = lv_end_time - lv_start_time.
    ""Write: / |Gesamtlaufzeit: { lv_runtime } Sekunden|.

    " *** NEU: Job reaktivieren (nur produktiv) ***"
    PERFORM reactivate_job.

  ENDMETHOD.










  METHOD transfer_to_new_structures.
    " Parameter-Deklaration in der Klassendeklaration hinzufügen:
    " IMPORTING iv_top_material TYPE matnr OPTIONAL

    DATA: lv_material_count TYPE i,
          lv_relation_count TYPE i.

    ""Write: / '=== START transfer_to_new_structures ==='.

    " Zeige welches TOP-Material übergeben wurde
    IF iv_top_material IS NOT INITIAL.
      ""Write: / 'TOP-Material aus Parameter:', iv_top_material.
    ELSE.
      ""Write: / 'WARNUNG: Kein TOP-Material übergeben!'.
    ENDIF.

    " Schritt 1: Baue Hierarchie mit dem KORREKTEN TOP-Material
    me->build_hierarchy_from_stb( iv_top_material = iv_top_material ).



    " Validierung
    lv_material_count = lines( gt_materials ).
    lv_relation_count = lines( gt_bom_relations ).

    ""Write: / 'Materialien in gt_materials:', lv_material_count.
    ""Write: / 'Beziehungen in gt_bom_relations:', lv_relation_count.
    ""Write: / '=== ENDE transfer_to_new_structures ==='.
  ENDMETHOD.





  METHOD debug_show_relations.
    DATA: lt_sorted        TYPE STANDARD TABLE OF ty_bom_relation,
          lv_current_child TYPE matnr,
          lv_parent_count  TYPE i,
          lt_parent_codes  TYPE STANDARD TABLE OF char4.

    ""Write: / ''.
    ""Write: / '╔══════════════════════════════════════════════════╗'.
    ""Write: / '║      VOLLSTÄNDIGE PARENT-CHILD ANALYSE          ║'.
    ""Write: / '╚══════════════════════════════════════════════════╝'.

    lt_sorted = gt_bom_relations.
    SORT lt_sorted BY child parent.

    " Fokus auf die kritischen Materialien
    DATA: lt_critical_mats TYPE STANDARD TABLE OF matnr.
    APPEND 'AR13025' TO lt_critical_mats.
    APPEND 'AR13026' TO lt_critical_mats.
    APPEND 'AR15117' TO lt_critical_mats.

    LOOP AT lt_critical_mats INTO DATA(lv_critical_mat).
      ""Write: / ''.
      ""Write: / '▶▶▶ MATERIAL:', lv_critical_mat.

      CLEAR: lv_parent_count, lt_parent_codes.

      LOOP AT lt_sorted INTO DATA(ls_rel) WHERE child = lv_critical_mat.
        ADD 1 TO lv_parent_count.

        READ TABLE gt_materials WITH KEY matnr = ls_rel-parent
             INTO DATA(ls_parent).

        READ TABLE gt_consumption WITH KEY matnr = ls_rel-parent
             INTO DATA(ls_cons).

        IF sy-subrc = 0.
          ""Write: / '  ← Parent:', ls_rel-parent,
          "'Code:', ls_parent-zzlzcod,
          "'Menge:', ls_rel-menge,
          "'Verbrauch:', ls_cons-gsv_korr.
        ELSE.
          ""Write: / '  ← Parent:', ls_rel-parent,
          "'Code:', ls_parent-zzlzcod,
          "'Menge:', ls_rel-menge,
          "'Verbrauch: 0'.
        ENDIF.

        APPEND ls_parent-zzlzcod TO lt_parent_codes.
      ENDLOOP.

      ""Write: / '  └─ Anzahl Parents:', lv_parent_count.

      " Erwartete Berechnung
      IF lv_critical_mat = 'AR13025'.
        ""Write: / '  ERWARTUNG: Sollte A8T0 werden (mit 46781, 47187 als Parents)'.
      ELSEIF lv_critical_mat = 'AR15117'.
        ""Write: / '  ERWARTUNG: Sollte A3T0 werden (mit 46781, 47187, 48006)'.
      ENDIF.
    ENDLOOP.

    ""Write: / '══════════════════════════════════════════════════'.
  ENDMETHOD.



  METHOD read_bill_of_material.

  ENDMETHOD.

  METHOD load_materials.
    DATA: lt_selected_mats TYPE STANDARD TABLE OF ty_material,
          lt_all_relations TYPE STANDARD TABLE OF ty_bom_relation.

    " 1. Lade selektierte Materialien inkl. Status
    SELECT matnr, mtart, zzlzcod, zzlzcodsort, zztyp_f4, pstat, disst, mstae
      FROM mara
      INTO CORRESPONDING FIELDS OF TABLE @lt_selected_mats
      WHERE matnr IN @s_matnr
        AND mtart IN @s_typcd.

    " ========================================
    " *** NEU: STATUS 99 BEHANDLUNG ***
    " ========================================
    DATA: lv_status99_count TYPE i.

    LOOP AT lt_selected_mats ASSIGNING FIELD-SYMBOL(<fs_mat_status>).
      IF <fs_mat_status>-mstae = '99'.
        " Status 99 = Ausgelaufen → ZZLZCODSORT leeren
        <fs_mat_status>-zzlzcodsort = ''.
        ADD 1 TO lv_status99_count.
      ENDIF.
    ENDLOOP.

    IF lv_status99_count > 0.
      WRITE: / 'Status 99 (Ausgelaufen):', lv_status99_count, 'Materialien → ZZLZCODSORT geleert'.
    ENDIF.

    " ========================================
    " *** FILTER NUR TOP-MATERIALIEN ***
    " ========================================
    DATA: lt_top_mats      TYPE STANDARD TABLE OF ty_material,
          lv_filtered_mats TYPE i.

    LOOP AT lt_selected_mats INTO DATA(ls_mat_check).

      " Prüfe: Existiert Material als KOMPNR (Kind) in ZPOWERBI_VC?
      SELECT COUNT(*) FROM zpowerbi_vc
        INTO @DATA(lv_child_count)
        WHERE kompnr = @ls_mat_check-matnr.

      IF lv_child_count = 0.
        " Material hat KEINE Parents → IST TOP-Material
        APPEND ls_mat_check TO lt_top_mats.
      ELSE.
        " Material hat Parents → AUSSCHLIESSEN
        ADD 1 TO lv_filtered_mats.
      ENDIF.

    ENDLOOP.

    " *** Ersetze lt_selected_mats durch lt_top_mats ***
    CLEAR lt_selected_mats.
    lt_selected_mats = lt_top_mats.

    " 2. Füge zu gt_materials hinzu
    LOOP AT lt_selected_mats INTO DATA(ls_mat).
      INSERT ls_mat INTO TABLE gt_materials.
    ENDLOOP.

    " 3. Für jedes Material: Lade Stückliste UND Verwendungen
    LOOP AT lt_selected_mats INTO ls_mat.

      " 3a. Top-Down: Stückliste lesen
      me->read_bill_of_material( iv_matnr = ls_mat-matnr ).
      me->build_hierarchy_from_stb( iv_top_material = ls_mat-matnr ).

      CLEAR: stb, bg.

      me->debug_problem_material(
        iv_matnr = ls_mat-matnr
        iv_phase = 'LOAD' ).
    ENDLOOP.

    me->load_usages_for_components( ).

  ENDMETHOD.


  METHOD load_usages_for_components.
    DATA: lt_all_components TYPE SORTED TABLE OF matnr WITH UNIQUE KEY table_line,
          lt_vc_raw         TYPE STANDARD TABLE OF zpowerbi_vc,
          ls_bom_rel        TYPE ty_bom_relation,
          ls_material       TYPE ty_material,
          lv_added_count    TYPE i,
          lv_vc_count       TYPE i.

    " ========================================
    " SCHRITT 1: SAMMLE ALLE KOMPONENTEN
    " ========================================
    LOOP AT gt_bom_relations INTO DATA(ls_rel).
      INSERT ls_rel-child INTO TABLE lt_all_components.
    ENDLOOP.

    " ========================================
    " SCHRITT 2: FÜR JEDE KOMPONENTE - BOTTOM-UP VIA ZPOWERBI_VC
    " ========================================
    LOOP AT lt_all_components INTO DATA(lv_component).

      CLEAR lt_vc_raw.

      SELECT matnr, kompnr, menge, stufe
        FROM zpowerbi_vc
        INTO CORRESPONDING FIELDS OF TABLE @lt_vc_raw
        WHERE kompnr = @lv_component.

      IF sy-subrc = 0.
        lv_vc_count = lv_vc_count + lines( lt_vc_raw ).
      ENDIF.

      " ========================================
      " SCHRITT 3: VERARBEITE JEDE VERWENDUNG
      " ========================================
      LOOP AT lt_vc_raw INTO DATA(ls_vc_raw).

        READ TABLE gt_bom_relations
             WITH KEY parent = ls_vc_raw-matnr
                      child  = lv_component
             TRANSPORTING NO FIELDS.

        IF sy-subrc <> 0.
          CLEAR ls_bom_rel.
          ls_bom_rel-parent = ls_vc_raw-matnr.
          ls_bom_rel-child  = lv_component.

          DATA(lv_menge_char) = ls_vc_raw-menge.
          DATA(lv_menge_numeric) = CONV menge_d( lv_menge_char ).
          ls_bom_rel-menge = lv_menge_numeric.

          DATA(lv_stufe_char) = ls_vc_raw-stufe.
          DATA(lv_stufe_int) = CONV i( lv_stufe_char ).
          ls_bom_rel-level = lv_stufe_int.

          INSERT ls_bom_rel INTO TABLE gt_bom_relations.
          ADD 1 TO lv_added_count.

          " Parent-Material zu gt_materials hinzufügen falls unbekannt
          READ TABLE gt_materials WITH KEY matnr = ls_bom_rel-parent
               TRANSPORTING NO FIELDS.
          IF sy-subrc <> 0.
            CLEAR ls_material.
            SELECT SINGLE matnr, mtart, zzlzcod, zzlzcodsort,
                          zztyp_f4, pstat, disst, mstae
              FROM mara
              INTO CORRESPONDING FIELDS OF @ls_material
              WHERE matnr = @ls_bom_rel-parent.

            IF sy-subrc = 0.
              " *** NEU: Status 99 prüfen ***
              IF ls_material-mstae = '99'.
                ls_material-zzlzcodsort = ''.
              ENDIF.
              INSERT ls_material INTO TABLE gt_materials.
            ENDIF.
          ENDIF.
        ENDIF.
      ENDLOOP.

    ENDLOOP.

  ENDMETHOD.


  METHOD load_consumption_data.
    DATA: w_datum_von      LIKE sy-datum,
          w_datum_bis      LIKE sy-datum,
          w_mblnr_von      TYPE mkpf-mblnr,
          w_mblnr_bis      TYPE mkpf-mblnr,
          lt_all_materials TYPE SORTED TABLE OF matnr WITH UNIQUE KEY table_line,
          ls_consumption   TYPE ty_consumption,
          lt_mseg          TYPE STANDARD TABLE OF mseg.

    ""Write: / 'Lade Verbrauchsdaten...'.

    " Sammle ALLE Materialien (Parents UND Children)
    LOOP AT gt_bom_relations INTO DATA(ls_rel).
      INSERT ls_rel-parent INTO TABLE lt_all_materials.
      INSERT ls_rel-child INTO TABLE lt_all_materials.
    ENDLOOP.

    " Zusätzlich: Alle Materialien aus gt_materials
    LOOP AT gt_materials INTO DATA(ls_mat).
      INSERT ls_mat-matnr INTO TABLE lt_all_materials.
    ENDLOOP.

    IF lt_all_materials IS INITIAL.
      ""Write: / 'Keine Materialien für Verbrauchsermittlung gefunden.'.
      RETURN.
    ENDIF.

    " Datumsbereiche
    w_datum_bis = sy-datum.
    WHILE w_datum_bis+4(2) = sy-datum+4(2).
      w_datum_bis = w_datum_bis - 1.
    ENDWHILE.
    w_datum_von = w_datum_bis - 360.
    w_datum_von+6(2) = '01'.

    " MKPF-Belegnummern-Bereich
    SELECT MIN( mblnr ) INTO @w_mblnr_von
      FROM mkpf
      WHERE budat >= @w_datum_von AND blart = 'WL'.

    SELECT MAX( mblnr ) INTO @w_mblnr_bis
      FROM mkpf
      WHERE budat <= @w_datum_bis AND blart = 'WL'.

    " Verbrauch für ALLE Materialien ermitteln
    LOOP AT lt_all_materials INTO DATA(lv_matnr).
      CLEAR ls_consumption.
      ls_consumption-matnr = lv_matnr.

      " Basis-Verbrauch
      CALL FUNCTION 'VERBRAUCH_SUMMIEREN'
        EXPORTING
          abdatum         = w_datum_von
          bisdatum        = w_datum_bis
          matnr           = lv_matnr
          periv           = ' '
          perkz           = 'M'
          werks           = '1100'
        IMPORTING
          gesamtverbrauch = ls_consumption-gsv01
        EXCEPTIONS
          OTHERS          = 1.

      " MSEG-Korrekturen
      CLEAR lt_mseg.
      SELECT matnr, menge, bwart, shkzg
        FROM mseg
        INTO CORRESPONDING FIELDS OF TABLE @lt_mseg
        WHERE matnr = @lv_matnr
          AND werks = '1100'
          AND lgort = '0001'
          AND sobkz = 'E'
          AND mblnr BETWEEN @w_mblnr_von AND @w_mblnr_bis
          AND bwart IN ('601', '602', '651', '654').

      LOOP AT lt_mseg INTO DATA(ls_mseg).
        DATA(lv_menge) = ls_mseg-menge.
        IF ls_mseg-shkzg = 'S'.
          lv_menge = lv_menge * -1.
        ENDIF.
        ls_consumption-gsv01 = ls_consumption-gsv01 + lv_menge.
      ENDLOOP.

      ls_consumption-gsv_korr = ls_consumption-gsv01.
      INSERT ls_consumption INTO TABLE gt_consumption.
    ENDLOOP.



    " !!! DEBUG: Verbrauch für kritische Materialien
    IF lv_matnr = 'B58383' OR lv_matnr = 'B53618' OR
       lv_matnr = 'B56383' OR lv_matnr = 'B69327'.
      "Write: / '!!! VERBRAUCH für', lv_matnr, ':'.
      "Write: / '!!!   gsv01 (Original):', ls_consumption-gsv01.
      "Write: / '!!!   gsv_korr (Korrigiert):', ls_consumption-gsv_korr.
      "Write: / '!!!   Anzahl MSEG-Korrekturen:', lines( lt_mseg ).
    ENDIF.
    ""Write: / |{ lines( gt_consumption ) } Verbrauchsdatensätze geladen|.
  ENDMETHOD.




  METHOD calculate_inheritance.
    DATA: lt_materials_sorted TYPE STANDARD TABLE OF ty_material,


          ls_material         TYPE ty_material,
          ls_child            TYPE ty_material,
          lv_changed          TYPE abap_bool,
          lv_iteration        TYPE i.

    "Write: / ''.
    "Write: / '╔════════════════════════════════════════════════════════════╗'.
    "Write: / '║           STARTE VERERBUNGSBERECHNUNG                      ║'.
    "Write: / '╚════════════════════════════════════════════════════════════╝'.

    " ========================================
    " SCHRITT 1: INITIALISIERE ALLE MATERIALIEN MIT LEVEL 999
    " ========================================
    LOOP AT gt_materials INTO ls_material.
      ls_material-level = 999.
      MODIFY TABLE gt_materials FROM ls_material.
    ENDLOOP.

    " ========================================
    " SCHRITT 2: SETZE LEVEL FÜR TOP-MATERIALIEN
    " ========================================
    "Write: / ''.
    "Write: / '╔════════════════════════════════════════════════════════════╗'.
    "Write: / '║           HIERARCHIE-LEVEL BESTIMMUNG                      ║'.
    "Write: / '╚════════════════════════════════════════════════════════════╝'.

    "Write: / 'Materialien initialisiert:', lines( gt_materials ).

    LOOP AT gt_materials INTO ls_material.
      READ TABLE gt_bom_relations WITH KEY child = ls_material-matnr
           TRANSPORTING NO FIELDS.

      IF sy-subrc <> 0.
        ls_material-level = 0.
        MODIFY TABLE gt_materials FROM ls_material.
        "Write: / '  TOP-Material (Level 0):', ls_material-matnr.
      ENDIF.
    ENDLOOP.

    " ========================================
    " SCHRITT 3: ÜBERNEHME LEVEL AUS gt_bom_relations
    " ========================================
    DATA lv_level_changes TYPE i VALUE 0.

    LOOP AT gt_bom_relations INTO DATA(ls_rel).
      READ TABLE gt_materials WITH KEY matnr = ls_rel-child
           INTO ls_child.

      IF sy-subrc = 0.
        IF ls_rel-level < ls_child-level.
          ls_child-level = ls_rel-level.
          MODIFY TABLE gt_materials FROM ls_child.
          ADD 1 TO lv_level_changes.

          IF lv_level_changes <= 10.
            "Write: / '  Level gesetzt:', ls_child-matnr, 'Level:', ls_child-level.
          ENDIF.
        ENDIF.
      ENDIF.
    ENDLOOP.

    "Write: / 'Level-Änderungen:', lv_level_changes.

    " ========================================
    " SCHRITT 4: SORTIERE NACH LEVEL
    " ========================================
    "Write: / ''.
    "Write: / '═══ FINALE VERARBEITUNGSREIHENFOLGE ═══'.

    LOOP AT gt_materials INTO ls_material.
      APPEND ls_material TO lt_materials_sorted.
    ENDLOOP.

    SORT lt_materials_sorted BY level ASCENDING matnr ASCENDING.

    "Write: / 'Verarbeite', lines( lt_materials_sorted ), 'Materialien in hierarchischer Reihenfolge'.

    LOOP AT lt_materials_sorted INTO ls_material.
      IF sy-tabix <= 10.
        "Write: / '  Material:', ls_material-matnr, 'Level:', ls_material-level.
      ENDIF.
    ENDLOOP.

    " ========================================
    " SCHRITT 5: VERARBEITE HIERARCHISCH + BEFÜLLE gt_results
    " ========================================








    DO 10 TIMES.
      lv_iteration = sy-index.
      lv_changed = abap_false.

      "Write: / ''.
      "Write: / '──── Iteration', lv_iteration, '────'.

      DATA lv_change_count TYPE i VALUE 0.

      LOOP AT lt_materials_sorted INTO ls_material.
        " Hole aktuellen Code
        READ TABLE gt_materials WITH KEY matnr = ls_material-matnr
             INTO DATA(ls_current).

        " Speichere alten Code





        DATA(lv_old_code) = ls_current-zzlzcod.


        " Berechne neuen Code
        DATA(lv_new_code) = me->apply_dominance_rules( iv_matnr = ls_material-matnr ).

        " Prüfe ob Änderung






        IF lv_new_code <> ls_current-zzlzcod.
          ls_current-zzlzcod = lv_new_code.
          MODIFY TABLE gt_materials FROM ls_current.
          lv_changed = abap_true.
          ADD 1 TO lv_change_count.
        ENDIF.

        " *** NEU: BEFÜLLE gt_results ***
        DATA(ls_result) = VALUE ty_result(
          matnr     = ls_material-matnr
          old_code  = lv_old_code
          new_code  = lv_new_code
          changed   = COND #( WHEN lv_new_code <> lv_old_code THEN 'X' ELSE '-' )
          message   = ''
        ).

        " Prüfe ob bereits in gt_results
        READ TABLE gt_results WITH KEY matnr = ls_material-matnr
             TRANSPORTING NO FIELDS.

        IF sy-subrc = 0.


          MODIFY TABLE gt_results FROM ls_result.

        ELSE.

          INSERT ls_result INTO TABLE gt_results.
        ENDIF.

      ENDLOOP.

      "Write: / 'Iteration', lv_iteration, ':', lv_change_count, 'Änderungen'.

      IF lv_changed = abap_false.
        "Write: / '*** Keine Änderungen mehr - Vererbung abgeschlossen ***'.
        EXIT.
      ENDIF.
    ENDDO.

    "Write: / '════════════════════════════════════════════════'.

    " ========================================
    " SCHRITT 6: DEBUG - ZEIGE gt_results
    " ========================================
    "Write: / ''.
    "Write: / '╔════════════════════════════════════════════════════════════╗'.
    "Write: / '║           gt_results BEFÜLLT                               ║'.
    "Write: / '╚════════════════════════════════════════════════════════════╝'.
    "Write: / 'Anzahl Einträge in gt_results:', lines( gt_results ).

    DATA lv_changed_count TYPE i VALUE 0.
    LOOP AT gt_results INTO ls_result WHERE changed = 'X'.
      ADD 1 TO lv_changed_count.
    ENDLOOP.

    "Write: / 'Davon geändert:', lv_changed_count.


















  ENDMETHOD.






  METHOD apply_dominance_rules.
    DATA: lt_parents            TYPE tt_usage_std,
          lv_has_a              TYPE abap_bool,
          lv_has_e              TYPE abap_bool,
          lv_has_n              TYPE abap_bool,
          lv_has_s              TYPE abap_bool,
          lv_count_a            TYPE i,
          lv_count_e            TYPE i,
          lv_count_n            TYPE i,
          lv_position_4         TYPE char1 VALUE '0',
          lv_total_verbrauch    TYPE p DECIMALS 2,
          lv_a_verbrauch        TYPE p DECIMALS 2,
          lv_anteil             TYPE p DECIMALS 2,
          lv_parent_verbrauch   TYPE p DECIMALS 2,
          lv_effektive_menge    TYPE p DECIMALS 3,
          lv_mara_code          TYPE char4,
          lv_parent_count       TYPE i,
          lv_all_n0x0           TYPE abap_bool VALUE abap_true,
          lv_parent_count_debug TYPE i.

    " ========================================
    " DEBUG: START
    " ========================================
    "Write: / ''.
    "Write: / '╔══════════════════════════════════════════════════╗'.
    "Write: / '║ apply_dominance_rules für Material:', iv_matnr.
    "Write: / '╚══════════════════════════════════════════════════╝'.

    " ========================================
    " SCHRITT 1: HOLE AKTUELLES MATERIAL
    " ========================================
    READ TABLE gt_materials WITH KEY matnr = iv_matnr
         INTO DATA(ls_current_mat).
    IF sy-subrc <> 0.
      "Write: / '*** FEHLER: Material nicht in gt_materials gefunden!'.
      rv_code = 'N0X0'.
      RETURN.
    ENDIF.

    "Write: / '  Aktueller Code:', ls_current_mat-zzlzcod.
    "Write: / '  Level:', ls_current_mat-level.

    " Position 4 Behandlung
    IF strlen( ls_current_mat-zzlzcod ) >= 4.
      lv_position_4 = ls_current_mat-zzlzcod+3(1).
    ENDIF.
    "Write: / '  Position 4:', lv_position_4.

    " ========================================
    " SCHRITT 2: ZZZZ-SPEZIALREGEL
    " ========================================
    IF ls_current_mat-zzlzcod = 'ZZZZ'.
      SELECT SINGLE zzlzcod FROM mara
        INTO @lv_mara_code
        WHERE matnr = @iv_matnr.
      IF lv_mara_code = 'ZZZZ'.
        "Write: / '  -> ZZZZ-Material (unveränderbar)'.
        rv_code = 'ZZZZ'.
        RETURN.
      ENDIF.
    ENDIF.

    " ========================================
    " SCHRITT 3: N0X1 SPEZIALFALL
    " ========================================
    IF ls_current_mat-zzlzcod = 'N0X1'.
      LOOP AT gt_bom_relations INTO DATA(ls_rel_check)
           WHERE child = iv_matnr.
        ADD 1 TO lv_parent_count.
      ENDLOOP.

      IF lv_parent_count = 1.
        READ TABLE gt_bom_relations INTO ls_rel_check
             WITH KEY child = iv_matnr.
        IF sy-subrc = 0.
          READ TABLE gt_materials INTO DATA(ls_single_parent)
               WITH KEY matnr = ls_rel_check-parent.
          IF sy-subrc = 0 AND ls_single_parent-zzlzcod(1) = 'A'.
            "Write: / '  -> N0X1 Spezialfall: Bleibt N0X1'.
            rv_code = 'N0X1'.
            RETURN.
          ENDIF.
        ENDIF.
      ENDIF.
    ENDIF.

    " ========================================
    " SCHRITT 4: ZÄHLE PARENTS (DEBUG)
    " ========================================
    CLEAR lv_parent_count_debug.
    LOOP AT gt_bom_relations INTO DATA(ls_rel_debug)
         WHERE child = iv_matnr.
      ADD 1 TO lv_parent_count_debug.
    ENDLOOP.

    "Write: / '  Anzahl Parents in gt_bom_relations:', lv_parent_count_debug.


    "INS  18.1.26
    IF lv_parent_count_debug = 0.
      " *** TOP-MATERIAL (keine Parents) → NICHT ANFASSEN ***
      " Defensive Variante: Prüft alle Fälle explizit

      IF ls_current_mat-zzlzcod = 'ZZZZ'.
        " ZZZZ bleibt ZZZZ
        rv_code = 'ZZZZ'.
      ELSEIF ls_current_mat-zzlzcod IS INITIAL.
        " Leerer Code bleibt leer
        rv_code = ''.
      ELSE.
        " Vorhandener Code bleibt unverändert
        rv_code = ls_current_mat-zzlzcod.
      ENDIF.

      RETURN.
    ENDIF.


    " DEL   18.1.26
*    IF lv_parent_count_debug = 0.
*      "Write: / '  *** KEINE PARENTS GEFUNDEN! ***'.
*      "Write: / '  Material behält Code:', ls_current_mat-zzlzcod.
*
*      IF ls_current_mat-zzlzcod IS NOT INITIAL AND
*         ls_current_mat-zzlzcod <> 'ZZZZ'.
*        rv_code = ls_current_mat-zzlzcod.
*      ELSE.
*        rv_code = 'N0X0'.
*      ENDIF.
*
*      "Write: / '  Rückgabe:', rv_code.
*      "Write: / '╚══════════════════════════════════════════════════╝'.
*      RETURN.
*    ENDIF.

    " ========================================
    " SCHRITT 5: SAMMLE ALLE PARENTS
    " ========================================
    "Write: / '  Sammle Parents:'.

    LOOP AT gt_bom_relations INTO DATA(ls_relation)
         WHERE child = iv_matnr.

      READ TABLE gt_materials WITH KEY matnr = ls_relation-parent
           INTO DATA(ls_parent).

      IF sy-subrc = 0.
        "Write: / '    Parent:', ls_relation-parent,
        "'Code:', ls_parent-zzlzcod,
        "'Menge:', ls_relation-menge.

        IF ls_parent-zzlzcod = 'ZZZZ'.
          "Write: '      -> ZZZZ übersprungen'.
          CONTINUE.
        ENDIF.

        " *** KORREKTUR: Verwende Default-Code statt zu überspringen ***
        IF ls_parent-zzlzcod IS INITIAL.
          "Write: '      -> Code ist leer, übersprungen'.
          CONTINUE.  " ❌ ÜBERSPRINGT PARENT!
        ENDIF.



        APPEND VALUE #( matnr = iv_matnr
                       parent = ls_relation-parent
                       parent_code = ls_parent-zzlzcod
                       usage_qty = ls_relation-menge ) TO lt_parents.

        " Verbrauchslogik mit Fallbacks
        READ TABLE gt_consumption WITH KEY matnr = ls_relation-parent
             INTO DATA(ls_consumption).

        IF sy-subrc = 0 AND ls_consumption-gsv_korr > 0.
          lv_parent_verbrauch = ls_consumption-gsv_korr.
          lv_effektive_menge = ls_relation-menge.
          "Write: '      -> Verbrauch:', lv_parent_verbrauch.

        ELSEIF sy-subrc = 0 AND ls_consumption-gsv_korr = 0.
          lv_parent_verbrauch = 1.
          lv_effektive_menge = ls_relation-menge.
          "Write: '      -> Verbrauch=0, verwende Menge'.

        ELSE.
          lv_parent_verbrauch = 1.
          IF ls_relation-menge > 0.
            lv_effektive_menge = ls_relation-menge.
          ELSE.
            lv_effektive_menge = 1.
          ENDIF.
          "Write: '      -> Kein Verbrauch, Fallback'.
        ENDIF.

        " Gesamtverbrauch akkumulieren
        lv_total_verbrauch = lv_total_verbrauch +
                            ( lv_effektive_menge * lv_parent_verbrauch ).

        " A-Anteil berechnen
        CASE ls_parent-zzlzcod+0(1).
          WHEN 'A'.
            lv_has_a = abap_true.
            ADD 1 TO lv_count_a.

            DATA: lv_percent_digit     TYPE c,
                  lv_mittlerer_prozent TYPE p DECIMALS 2.
            lv_percent_digit = ls_parent-zzlzcod+1(1).

            IF lv_percent_digit = '0' AND ls_parent-zzlzcod+2(1) = 'X'.
              lv_a_verbrauch = lv_a_verbrauch +
                               ( lv_effektive_menge * lv_parent_verbrauch ).
              "Write: '      -> A0X: 100% A-Anteil'.

            ELSEIF lv_percent_digit BETWEEN '1' AND '9'.
              CASE lv_percent_digit.
                WHEN '0'. lv_mittlerer_prozent = '2'.     " 0-4%   → 2%
                WHEN '1'. lv_mittlerer_prozent = '9.5'.   " 5-14%  → 9.5%  ✅
                WHEN '2'. lv_mittlerer_prozent = '19.5'.  " 15-24% → 19.5% ✅
                WHEN '3'. lv_mittlerer_prozent = '29.5'.  " 25-34% → 29.5% ✅
                WHEN '4'. lv_mittlerer_prozent = '39.5'.  " 35-44% → 39.5% ✅
                WHEN '5'. lv_mittlerer_prozent = '49.5'.  " 45-54% → 49.5% ✅
                WHEN '6'. lv_mittlerer_prozent = '59.5'.  " 55-64% → 59.5% ✅
                WHEN '7'. lv_mittlerer_prozent = '69.5'.  " 65-74% → 69.5% ✅
                WHEN '8'. lv_mittlerer_prozent = '79.5'.  " 75-84% → 79.5% ✅
                WHEN '9'. lv_mittlerer_prozent = '92'.    " 85-99% → 92%   ✅
              ENDCASE.
















              lv_a_verbrauch = lv_a_verbrauch +
                              ( lv_effektive_menge * lv_parent_verbrauch *
                                lv_mittlerer_prozent / 100 ).
              "Write: '      -> A-Anteil:', lv_mittlerer_prozent, '%'.

            ELSEIF lv_percent_digit = '0' AND ls_parent-zzlzcod+2(1) = 'T'.
              lv_a_verbrauch = lv_a_verbrauch +
                              ( lv_effektive_menge * lv_parent_verbrauch * 2 / 100 ).
              "Write: '      -> A0T: 2% A-Anteil'.
            ENDIF.

          WHEN 'E'.
            lv_has_e = abap_true.
            ADD 1 TO lv_count_e.
            "Write: '      -> E-Code'.

          WHEN 'N'.
            lv_has_n = abap_true.
            ADD 1 TO lv_count_n.
            "Write: '      -> N-Code'.

          WHEN 'S'.
            lv_has_s = abap_true.
            "Write: '      -> S-Code'.
        ENDCASE.

      ELSE.
        "Write: / '    Parent:', ls_relation-parent, '-> NICHT IN gt_materials!'.
      ENDIF.
    ENDLOOP.

    "Write: / '  Gültige Parents gesammelt:', lines( lt_parents ).
    "Write: / '  Counts: A=', lv_count_a, 'E=', lv_count_e, 'N=', lv_count_n.

    " ========================================
    " SCHRITT 6: FEHLERFALL - KEINE GÜLTIGEN PARENTS
    " ========================================
    IF lines( lt_parents ) = 0.
      "Write: / '  *** KEINE GÜLTIGEN PARENTS! ***'.

      IF ls_current_mat-zzlzcod IS NOT INITIAL AND
         ls_current_mat-zzlzcod <> 'ZZZZ'.
        rv_code = ls_current_mat-zzlzcod.
      ELSE.
        rv_code = 'N0X0'.
      ENDIF.

      "Write: / '  Rückgabe:', rv_code.
      "Write: / '╚══════════════════════════════════════════════════╝'.
      RETURN.
    ENDIF.

    " ========================================
    " SCHRITT 7: BERECHNE A-ANTEIL
    " ========================================
    IF lv_total_verbrauch > 0.
      lv_anteil = ( lv_a_verbrauch / lv_total_verbrauch ) * 100.
    ELSE.
      lv_anteil = 0.
    ENDIF.

    "Write: / '  Total-Verbrauch:', lv_total_verbrauch.
    "Write: / '  A-Verbrauch:', lv_a_verbrauch.
    "Write: / '  A-Anteil:', lv_anteil, '%'.







    " ========================================
    " SCHRITT 8: VERERBUNGSLOGIK (KORRIGIERT)
    " ========================================

    " REGEL 1: E dominiert (nur wenn KEIN N dabei ist)
    IF lv_has_e = abap_true AND lv_has_n = abap_false.
      "Write: / '  REGEL 1: E dominiert (ohne N)'.
      rv_code = 'E0X' && lv_position_4.

      " REGEL 2: N + A (ohne E) → Auslauf-Teilmenge
    ELSEIF lv_has_n = abap_true AND lv_has_a = abap_true AND lv_has_e = abap_false.
      "Write: / '  REGEL 2: N + A (ohne E) = Auslauf-Teilmenge'.

      IF lv_anteil = 100.
        rv_code = 'A0X' && lv_position_4.
      ELSEIF lv_anteil >= 85.
        rv_code = 'A9T' && lv_position_4.
      ELSEIF lv_anteil >= 75.
        rv_code = 'A8T' && lv_position_4.
      ELSEIF lv_anteil >= 65.
        rv_code = 'A7T' && lv_position_4.
      ELSEIF lv_anteil >= 55.
        rv_code = 'A6T' && lv_position_4.
      ELSEIF lv_anteil >= 45.
        rv_code = 'A5T' && lv_position_4.
      ELSEIF lv_anteil >= 35.
        rv_code = 'A4T' && lv_position_4.
      ELSEIF lv_anteil >= 25.
        rv_code = 'A3T' && lv_position_4.
      ELSEIF lv_anteil >= 15.
        rv_code = 'A2T' && lv_position_4.
      ELSEIF lv_anteil >= 5.
        rv_code = 'A1T' && lv_position_4.
      ELSEIF lv_anteil > 0.
        rv_code = 'A0T' && lv_position_4.
      ELSE.
        rv_code = 'N0X' && lv_position_4.
      ENDIF.

      " REGEL 3: N dominiert IMMER (auch mit E/A)
    ELSEIF lv_has_n = abap_true.
      "Write: / '  REGEL 3: N dominiert (mit E/A)'.
      rv_code = 'N0X' && lv_position_4.

      " REGEL 4: Nur A (ohne N/E)
    ELSEIF lv_has_a = abap_true AND lv_has_n = abap_false AND lv_has_e = abap_false.
      "Write: / '  REGEL 4: Nur A'.

      IF lv_anteil = 100.
        rv_code = 'A0X' && lv_position_4.
      ELSEIF lv_anteil >= 85.
        rv_code = 'A9T' && lv_position_4.
      ELSEIF lv_anteil >= 75.
        rv_code = 'A8T' && lv_position_4.
      ELSEIF lv_anteil >= 65.
        rv_code = 'A7T' && lv_position_4.
      ELSEIF lv_anteil >= 55.
        rv_code = 'A6T' && lv_position_4.
      ELSEIF lv_anteil >= 45.
        rv_code = 'A5T' && lv_position_4.
      ELSEIF lv_anteil >= 35.
        rv_code = 'A4T' && lv_position_4.
      ELSEIF lv_anteil >= 25.
        rv_code = 'A3T' && lv_position_4.
      ELSEIF lv_anteil >= 15.
        rv_code = 'A2T' && lv_position_4.
      ELSEIF lv_anteil >= 5.
        rv_code = 'A1T' && lv_position_4.
      ELSEIF lv_anteil > 0.
        rv_code = 'A0T' && lv_position_4.
      ELSE.
        rv_code = 'N0X' && lv_position_4.
      ENDIF.

      " REGEL 5: Fallback
    ELSE.
      "Write: / '  REGEL 5: Fallback (keine gültigen Parents)'.
      rv_code = 'N0X' && lv_position_4.
    ENDIF.


  ENDMETHOD.




















































































































































































































































































































































































































  METHOD Write_to_journal.
    DATA: ls_journal TYPE zlzcod_journl.

    ls_journal-mandt = sy-mandt.
    ls_journal-matnr = iv_matnr.
    ls_journal-aenderungsdatum = sy-datum.
    ls_journal-aenderungszeit = sy-uzeit.
    ls_journal-aenderungsuser = sy-uname.
    ls_journal-old_lzcod = iv_old_lzcod.
    ls_journal-new_lzcod = iv_new_lzcod.
    ls_journal-old_lzcodsort = iv_old_lzcodsort.
    ls_journal-new_lzcodsort = iv_new_lzcodsort.
    ls_journal-programm = sy-repid.

    " TOP-Material aus aktueller Selektion (da Wrapper nur 1 übergibt)
    READ TABLE s_matnr INDEX 1 INTO DATA(ls_sel).
    IF sy-subrc = 0 AND ls_sel-sign = 'I' AND ls_sel-option = 'EQ'.
      ls_journal-top_matnr = ls_sel-low.
    ELSE.
      ls_journal-top_matnr = iv_matnr.  " Fallback
    ENDIF.

    " In Tabelle schreiben
    INSERT zlzcod_journl FROM ls_journal.

    IF sy-subrc <> 0.
      " Bei Fehler: Update statt Insert (falls Eintrag schon existiert)
      UPDATE zlzcod_journl FROM ls_journal.
    ENDIF.

    " Für Debug-Zwecke:
    IF p_test = abap_true.
      ""Write: / 'Journal:', ls_journal-matnr,
      "'TOP:', ls_journal-top_matnr,
      "'LZCOD:', ls_journal-old_lzcod, '->', ls_journal-new_lzcod,
      " 'LZCODSORT:', ls_journal-old_lzcodsort, '->', ls_journal-new_lzcodsort.
    ENDIF.
  ENDMETHOD.


*
*  METHOD update_database.
*    DATA: lt_update_batch TYPE STANDARD TABLE OF ty_result,
*          lv_batch_count  TYPE i,
*          lv_updated      TYPE i,
*          lv_datum_str    TYPE dats.
*
*    lv_datum_str =  sy-datum.
*
*    " Batch-Update für Performance
*    LOOP AT gt_results INTO DATA(ls_result) WHERE changed = abap_true.
*      APPEND ls_result TO lt_update_batch.
*
*
*      " Führe Batch-Update durch
*      LOOP AT lt_update_batch INTO DATA(ls_update).
*
*        UPDATE mara SET zzlzcod = @ls_update-new_code,
*                       zzlzdat = @lv_datum_str
*              WHERE matnr = @ls_update-matnr.
*        COMMIT WORK.
*
*        " Verwende MM02 für Update
*
*        " Journal-Eintrag mit TOP-Material
**        me->""Write_to_journal(
**          iv_matnr = ls_update-matnr
**          iv_old_lzcod = ls_update-old_code
**          iv_new_lzcod = ls_update-new_code ).
*
*        ADD 1 TO lv_updated.
*      ENDLOOP.
*
*      COMMIT WORK.
*      CLEAR lt_update_batch.
*      ADD 1 TO lv_batch_count.
*
*      " Progress-Anzeige
*      cl_progress_indicator=>progress_indicate(
*        i_text = |Batch { lv_batch_count } aktualisiert|
*        i_processed = lv_updated
*        i_total = lines( gt_results ) ).
*
*    ENDLOOP.
*
*    " Letzter Batch
*    IF lines( lt_update_batch ) > 0.
*      LOOP AT lt_update_batch INTO ls_update.
*        IF p_mm = abap_true.
*          UPDATE mara SET zzlzcod = @ls_update-new_code,
*                         zzlzdat = @lv_datum_str
*                WHERE matnr = @ls_update-matnr. COMMIT WORK.
*        ELSE.
*
*
*
*
*        ENDIF.
*
*        " Journal-Eintrag mit TOP-Material
*        me->Write_to_journal(
*          iv_matnr = ls_update-matnr
*          iv_old_lzcod = ls_update-old_code
*          iv_new_lzcod = ls_update-new_code ).
*
*        ADD 1 TO lv_updated.
*      ENDLOOP.
*      COMMIT WORK.
*    ENDIF.
*
*    ""Write: / |{ lv_updated } Materialien über MM02 aktualisiert (ZZLZCOD)|.
*  ENDMETHOD.

  METHOD update_database.
    DATA: lv_updated   TYPE i,
          lv_datum_str TYPE dats.

    lv_datum_str = sy-datum.

    LOOP AT gt_results INTO DATA(ls_result) WHERE changed = abap_true.

      " Update durchführen
      UPDATE mara SET zzlzcod = @ls_result-new_code,
                     zzlzdat = @lv_datum_str
            WHERE matnr = @ls_result-matnr.
      COMMIT WORK.

      " Journal-Eintrag
      me->write_to_journal(
        iv_matnr     = ls_result-matnr
        iv_old_lzcod = ls_result-old_code
        iv_new_lzcod = ls_result-new_code ).

      ADD 1 TO lv_updated.

    ENDLOOP.

    WRITE: / lv_updated, 'Materialien aktualisiert (ZZLZCOD)'.

  ENDMETHOD.




  METHOD update_database_sortiment.
    DATA: lv_updated   TYPE i,
          lv_errors    TYPE i,
          lv_status99  TYPE i,
          lv_datum_str TYPE dats.

    lv_datum_str = sy-datum.

    WRITE: / '=================================================='.
    WRITE: / 'START: Update Database Sortiment'.
    WRITE: / 'Anzahl zu aktualisieren:', lines( gt_results ).

    LOOP AT gt_results INTO DATA(ls_result)
         WHERE changed_sort = abap_true.

      " *** DIREKTES UPDATE MARA (auch leerer Code für Status 99) ***
      UPDATE mara
        SET zzlzcodsort = @ls_result-new_code_sort,
            zzlzdat     = @lv_datum_str
        WHERE matnr = @ls_result-matnr.

      IF sy-subrc = 0.
        ADD 1 TO lv_updated.

        " Zähle Status 99 separat
        IF ls_result-new_code_sort IS INITIAL.
          ADD 1 TO lv_status99.
        ENDIF.

        " Journal-Eintrag
        me->write_to_journal(
          iv_matnr         = ls_result-matnr
          iv_old_lzcod     = ls_result-old_code
          iv_new_lzcod     = ls_result-old_code
          iv_old_lzcodsort = ls_result-old_code_sort
          iv_new_lzcodsort = ls_result-new_code_sort ).

      ELSE.
        ADD 1 TO lv_errors.
        WRITE: / 'ERROR: Update fehlgeschlagen für', ls_result-matnr.
      ENDIF.
    ENDLOOP.

    " Commit nach allen Updates
    IF lv_updated > 0.
      COMMIT WORK AND WAIT.
      WRITE: / 'ERFOLG:', lv_updated, 'Sortiments-Codes aktualisiert'.
      WRITE: / '  Davon Status 99 (geleert):', lv_status99.
    ENDIF.

    IF lv_errors > 0.
      WRITE: / 'FEHLER:', lv_errors, 'Updates fehlgeschlagen'.
    ENDIF.

    WRITE: / 'ENDE: Update Database Sortiment'.
    WRITE: / '=================================================='.
  ENDMETHOD.



*
*  METHOD transaktion_mm02.
*    DATA: lv_matnr_str     TYPE bdc_fval,  " Geändert zu bdc_fval
*          lv_datum_str     TYPE bdc_fval,  " Geändert zu bdc_fval
*          lv_lzcod_str     TYPE bdc_fval,  " NEU für iv_lzcod
*          lv_lzcodsort_str TYPE bdc_fval. " NEU für iv_lzcodsort
*
*    CLEAR: gt_bdcdata, gt_bdcmsg.
*
*    " Material und Datum formatieren - als bdc_fval
*    lv_matnr_str = |{ iv_matnr ALPHA = OUT }|.
*    lv_datum_str = |{ sy-datum DATE = USER }|.
*    lv_lzcod_str = iv_lzcod.  " Direkte Zuweisung, da char4 -> bdc_fval kompatibel
*
*    " Optional: ZZLZCODSORT konvertieren
*    IF iv_lzcodsort IS NOT INITIAL.
*      lv_lzcodsort_str = iv_lzcodsort.
*    ENDIF.
*
*    " Einstiegsbild MM02
*    me->bdc_dynpro( iv_program = 'SAPLMGMM' iv_dynpro = '0060' ).
*    me->bdc_field( iv_fnam = 'BDC_OKCODE' iv_fval = 'AUSW' ).
*    me->bdc_field( iv_fnam = 'RMMG1-MATNR' iv_fval = lv_matnr_str ).
*
*    " Sichtenauswahl
*    me->bdc_dynpro( iv_program = 'SAPLMGMM' iv_dynpro = '0070' ).
*    me->bdc_field( iv_fnam = 'BDC_OKCODE' iv_fval = '=ENTR' ).
*    me->bdc_field( iv_fnam = 'MSICHTAUSW-KZSEL(01)' iv_fval = 'X' ).
*
*    " Grunddaten - beide Felder setzen
*    me->bdc_dynpro( iv_program = 'SAPLMGMM' iv_dynpro = '4004' ).
*    me->bdc_field( iv_fnam = 'BDC_OKCODE' iv_fval = '/11' ).
*
*    " ZZLZCOD setzen
*    me->bdc_field( iv_fnam = 'MARA-ZZLZCOD' iv_fval = lv_lzcod_str ).
*    me->bdc_field( iv_fnam = 'MARA-ZZLZDAT' iv_fval = lv_datum_str ).
*
*    " ZZLZCODSORT setzen falls übergeben
*    IF iv_lzcodsort IS NOT INITIAL.
*      me->bdc_field( iv_fnam = 'MARA-ZZLZCODSORT' iv_fval = lv_lzcodsort_str ).
*    ENDIF.
*
*    me->bdc_transaction( iv_tcode = 'MM02' ).
*  ENDMETHOD.
*




















  METHOD is_debug_material.
    " NUR diese 4 Materialien debuggen!
    rv_debug = COND #(
      WHEN iv_matnr = 'B53618' OR
           iv_matnr = 'B56383' OR
           iv_matnr = 'B58383' OR
           iv_matnr = 'B69327'
      THEN abap_true
      ELSE abap_false
    ).
  ENDMETHOD.

  METHOD debug_problem_material.
    DATA: lt_info TYPE string_table,
          lv_line TYPE string.

    " Nur debuggen wenn es ein Problem-Material ist
    IF is_debug_material( iv_matnr ) = abap_false.
      RETURN.
    ENDIF.

    "Write: / ''.
    "Write: / '╔════════════════════════════════════════════════════════════╗'.
    "Write: / '║ DEBUG PROBLEM-MATERIAL:', iv_matnr, 'Phase:', iv_phase.
    "Write: / '╚════════════════════════════════════════════════════════════╝'.

    " Je nach Phase verschiedene Informationen sammeln
    CASE iv_phase.
      WHEN 'LOAD'.
        " Nach dem Laden: Zeige Stammdaten
        READ TABLE gt_materials WITH KEY matnr = iv_matnr
             INTO DATA(ls_mat).
        IF sy-subrc = 0.
          "Write: / '  Stammdaten gefunden:'.
          "Write: / '    MTART:', ls_mat-mtart.
          "Write: / '    ZZLZCOD (aktuell):', ls_mat-zzlzcod.
          "Write: / '    ZZLZCODSORT:', ls_mat-zzlzcodsort.
          "Write: / '    DISST:', ls_mat-disst.
        ELSE.
          "Write: / '  *** FEHLER: Material NICHT in gt_materials!'.
        ENDIF.

      WHEN 'RELATIONS'.
        " Parent-Child Beziehungen
        DATA: lv_parent_count TYPE i,
              lv_child_count  TYPE i.

        "Write: / '  === PARENT-BEZIEHUNGEN ==='.
        LOOP AT gt_bom_relations INTO DATA(ls_rel)
             WHERE child = iv_matnr.
          ADD 1 TO lv_parent_count.

          READ TABLE gt_materials WITH KEY matnr = ls_rel-parent
               INTO DATA(ls_parent).
          IF sy-subrc = 0.
            "Write: / '    Parent', lv_parent_count, ':', ls_rel-parent.
            "Write: / '      Code:', ls_parent-zzlzcod.
            "Write: / '      Menge:', ls_rel-menge.
          ELSE.
            "Write: / '    Parent', lv_parent_count, ':', ls_rel-parent,
            "'*** NICHT in gt_materials!'.
          ENDIF.
        ENDLOOP.

        IF lv_parent_count = 0.
          "Write: / '  *** KEINE PARENTS GEFUNDEN! ***'.
        ELSE.
          "Write: / '  Anzahl Parents:', lv_parent_count.
        ENDIF.

        "Write: / '  === CHILD-BEZIEHUNGEN ==='.
        LOOP AT gt_bom_relations INTO ls_rel
             WHERE parent = iv_matnr.
          ADD 1 TO lv_child_count.
          "Write: / '    Child:', ls_rel-child, 'Menge:', ls_rel-menge.
        ENDLOOP.

        IF lv_child_count = 0.
          "Write: / '  Keine Children (Endprodukt oder Komponente ohne Stückliste)'.
        ELSE.
          "Write: / '  Anzahl Children:', lv_child_count.
        ENDIF.

      WHEN 'CONSUMPTION'.
        " Verbrauchsdaten
        "Write: / '  === VERBRAUCHSDATEN ==='.

        " Eigener Verbrauch
        READ TABLE gt_consumption WITH KEY matnr = iv_matnr
             INTO DATA(ls_cons).
        IF sy-subrc = 0.
          "Write: / '    Eigener Verbrauch:'.
          "Write: / '      gsv01:', ls_cons-gsv01.
          "Write: / '      gsv_korr:', ls_cons-gsv_korr.
        ELSE.
          "Write: / '    *** KEIN Verbrauch gefunden!'.
        ENDIF.

        " Parent-Verbräuche
        "Write: / '    Parent-Verbräuche:'.
        LOOP AT gt_bom_relations INTO ls_rel WHERE child = iv_matnr.
          READ TABLE gt_consumption WITH KEY matnr = ls_rel-parent
               INTO ls_cons.
          IF sy-subrc = 0.
            "Write: / '      Parent', ls_rel-parent,
            "'gsv01:', ls_cons-gsv01,
            "'gsv_korr:', ls_cons-gsv_korr.
          ELSE.
            "Write: / '      Parent', ls_rel-parent, 'KEIN Verbrauch'.
          ENDIF.
        ENDLOOP.

      WHEN 'BEFORE_CALC'.
        " Vor der Berechnung
        "Write: / '  === VOR BERECHNUNG ==='.
        READ TABLE gt_materials WITH KEY matnr = iv_matnr INTO ls_mat.
        IF sy-subrc = 0.
          "Write: / '    Aktueller Code:', ls_mat-zzlzcod.

          " Zeige Parent-Codes
          "Write: / '    Parent-Codes:'.
          LOOP AT gt_bom_relations INTO ls_rel WHERE child = iv_matnr.
            READ TABLE gt_materials WITH KEY matnr = ls_rel-parent
                 INTO ls_parent.
            IF sy-subrc = 0.
              "Write: / '      ', ls_rel-parent, '=', ls_parent-zzlzcod.
            ELSE.
              "Write: / '      ', ls_rel-parent, '= NICHT GEFUNDEN'.
            ENDIF.
          ENDLOOP.
        ENDIF.

      WHEN 'AFTER_CALC'.
        " Nach der Berechnung
        "Write: / '  === NACH BERECHNUNG ==='.
        READ TABLE gt_results WITH KEY matnr = iv_matnr
             INTO DATA(ls_result).
        IF sy-subrc = 0.
          "Write: / '    Alter Code:', ls_result-old_code.
          "Write: / '    Neuer Code:', ls_result-new_code.
          "Write: / '    Geändert:', ls_result-changed.
        ELSE.
          "Write: / '    *** KEIN ERGEBNIS in gt_results!'.
        ENDIF.

    ENDCASE.

    "Write: / '══════════════════════════════════════════════════════════'.
  ENDMETHOD.

  METHOD build_hierarchy_from_stb.
    DATA: lt_vc_data      TYPE STANDARD TABLE OF zpowerbi_vc,
          ls_bom_rel      TYPE ty_bom_relation,
          ls_material     TYPE ty_material,
          lv_top_material TYPE matnr,
          lv_rel_count    TYPE i,
          lv_mat_count    TYPE i.

    " ========================================
    " SCHRITT 1: TOP-Material bestimmen
    " ========================================
    IF iv_top_material IS NOT INITIAL.
      lv_top_material = iv_top_material.
    ELSE.
      RETURN.
    ENDIF.

    " ========================================
    " SCHRITT 2: Prüfe ob Material in ZPOWERBI_VC existiert
    " ========================================
    SELECT COUNT(*) FROM zpowerbi_vc
      INTO @DATA(lv_row_count)
      WHERE matnr = @lv_top_material.

    IF lv_row_count = 0.
      RETURN.
    ENDIF.

    " ========================================
    " SCHRITT 3: Lade Hierarchie aus ZPOWERBI_VC
    " ========================================
    SELECT * FROM zpowerbi_vc
      INTO TABLE @lt_vc_data
      WHERE matnr = @lv_top_material
      ORDER BY mat_mstav.

    IF sy-subrc <> 0.
      RETURN.
    ENDIF.

    " ========================================
    " SCHRITT 4: TOP-Material zu gt_materials hinzufügen
    " ========================================
    READ TABLE gt_materials WITH KEY matnr = lv_top_material
         TRANSPORTING NO FIELDS.
    IF sy-subrc <> 0.
      SELECT SINGLE matnr, mtart, zzlzcod, zzlzcodsort, zztyp_f4, pstat, disst, mstae
        FROM mara
        INTO CORRESPONDING FIELDS OF @ls_material
        WHERE matnr = @lv_top_material.
      IF sy-subrc = 0.
        " *** NEU: Status 99 prüfen ***
        IF ls_material-mstae = '99'.
          ls_material-zzlzcodsort = ''.
        ENDIF.
        INSERT ls_material INTO TABLE gt_materials.
        ADD 1 TO lv_mat_count.
      ENDIF.
    ENDIF.

    " ========================================
    " SCHRITT 5: Verarbeite ZPOWERBI_VC-Einträge
    " ========================================
    LOOP AT lt_vc_data INTO DATA(ls_vc).

      DATA(lv_parent_matnr) = VALUE matnr( ).

      IF ls_vc-mat_mstae = 0.
        lv_parent_matnr = lv_top_material.
      ELSE.
        lv_parent_matnr = ls_vc-kom_mstae.
      ENDIF.

      IF lv_parent_matnr IS NOT INITIAL.
        ls_bom_rel-parent = lv_parent_matnr.
        ls_bom_rel-child  = ls_vc-kompnr.
        ls_bom_rel-menge  = ls_vc-menge.
        ls_bom_rel-level  = ls_vc-stufe.

        INSERT ls_bom_rel INTO TABLE gt_bom_relations.
        ADD 1 TO lv_rel_count.
      ENDIF.

      " Füge Child-Material zu gt_materials hinzu
      READ TABLE gt_materials WITH KEY matnr = ls_vc-kompnr
           TRANSPORTING NO FIELDS.
      IF sy-subrc <> 0.
        CLEAR ls_material.
        ls_material-matnr = ls_vc-kompnr.
        ls_material-mtart = ls_vc-materialart.
        ls_material-disst = ''.

        " Hole ZZLZCOD/ZZLZCODSORT/MSTAE aus MARA
        SELECT SINGLE zzlzcod, zzlzcodsort, zztyp_f4, pstat, mstae
          FROM mara
          INTO CORRESPONDING FIELDS OF @ls_material
          WHERE matnr = @ls_vc-kompnr.

        IF sy-subrc = 0.
          " *** NEU: Status 99 prüfen ***
          IF ls_material-mstae = '99'.
            ls_material-zzlzcodsort = ''.
          ENDIF.
          INSERT ls_material INTO TABLE gt_materials.
          ADD 1 TO lv_mat_count.
        ENDIF.
      ENDIF.
    ENDLOOP.

  ENDMETHOD.

  METHOD debug_collect_info.
    DATA: lv_info TYPE string.

    CLEAR rt_info.

    " Sammle alle Informationen für ein Material
    READ TABLE gt_materials WITH KEY matnr = iv_matnr INTO DATA(ls_mat).
    IF sy-subrc = 0.
      lv_info = |Material: { iv_matnr } Code: { ls_mat-zzlzcod }|.
      APPEND lv_info TO rt_info.
    ENDIF.

    " Parents
    LOOP AT gt_bom_relations INTO DATA(ls_rel) WHERE child = iv_matnr.
      READ TABLE gt_materials WITH KEY matnr = ls_rel-parent
           INTO DATA(ls_parent).
      IF sy-subrc = 0.
        lv_info = |  Parent: { ls_rel-parent } Code: { ls_parent-zzlzcod }|.
      ELSE.
        lv_info = |  Parent: { ls_rel-parent } NICHT GEFUNDEN|.
      ENDIF.
      APPEND lv_info TO rt_info.
    ENDLOOP.
  ENDMETHOD.














  METHOD bdc_dynpro.
    DATA: ls_bdcdata TYPE bdcdata.

    ls_bdcdata-program = iv_program.
    ls_bdcdata-dynpro = iv_dynpro.
    ls_bdcdata-dynbegin = 'X'.
    APPEND ls_bdcdata TO gt_bdcdata.
  ENDMETHOD.

  METHOD bdc_field.
    DATA: ls_bdcdata TYPE bdcdata.

    ls_bdcdata-fnam = iv_fnam.
    ls_bdcdata-fval = iv_fval.
    APPEND ls_bdcdata TO gt_bdcdata.
  ENDMETHOD.

  METHOD bdc_transaction.
    DATA: ls_options TYPE ctu_params.

    ls_options-dismode = 'N'.  " Kein Bildschirm
    ls_options-updmode = 'S'.  " Synchron
    ls_options-defsize = 'X'.  " Standardgröße

    CALL TRANSACTION iv_tcode
      USING gt_bdcdata
      OPTIONS FROM ls_options
      MESSAGES INTO gt_bdcmsg.

    " Fehlerbehandlung
    LOOP AT gt_bdcmsg INTO DATA(ls_msg)
         WHERE msgtyp = 'E' OR msgtyp = 'A'.
      MESSAGE ID ls_msg-msgid TYPE 'I' NUMBER ls_msg-msgnr
        WITH ls_msg-msgv1 ls_msg-msgv2 ls_msg-msgv3 ls_msg-msgv4.
    ENDLOOP.
  ENDMETHOD.


  METHOD calculate_sortiment_inhe.
    DATA: lv_iteration     TYPE i,
          lv_changes       TYPE i,
          lt_material_list TYPE STANDARD TABLE OF matnr.

    " Sammle alle zu verarbeitenden Materialien
    LOOP AT gt_materials INTO DATA(ls_material).
      APPEND ls_material-matnr TO lt_material_list.
    ENDLOOP.

    " Iterative Vererbungsberechnung (max. 10 Iterationen)
    DO 10 TIMES.
      lv_iteration = sy-index.
      CLEAR lv_changes.

      " Verarbeite alle Materialien
      LOOP AT lt_material_list INTO DATA(lv_matnr).

        " Prüfe auf Änderung
        READ TABLE gt_materials WITH KEY matnr = lv_matnr
             ASSIGNING FIELD-SYMBOL(<fs_mat>).

        IF sy-subrc <> 0.
          CONTINUE.
        ENDIF.

        " *** NEU: Status 99 = Ausgelaufen → Überspringen ***
        IF <fs_mat>-mstae = '99'.
          " Nur beim ersten Durchlauf in Ergebnis aufnehmen
          IF lv_iteration = 1.
            READ TABLE gt_results WITH KEY matnr = lv_matnr
                 TRANSPORTING NO FIELDS.
            IF sy-subrc <> 0.
              APPEND VALUE #(
                matnr         = lv_matnr
                old_code_sort = <fs_mat>-zzlzcodsort
                new_code_sort = ''
                changed_sort  = COND #( WHEN <fs_mat>-zzlzcodsort IS NOT INITIAL
                                        THEN abap_true ELSE abap_false )
                message       = 'Status 99 - Ausgelaufen'
              ) TO gt_results.
            ENDIF.
          ENDIF.
          CONTINUE.  " Nächstes Material
        ENDIF.

        " Prüfe manuelle Sperre (Position 4 = '1')
        IF strlen( <fs_mat>-zzlzcodsort ) >= 4 AND
           <fs_mat>-zzlzcodsort+3(1) = '1'.
          CONTINUE.
        ENDIF.

        " Berechne neuen Sortimentscode
        DATA(lv_new_code) = apply_sortiment_rules( lv_matnr ).

        " Prüfe ob sich der Code geändert hat
        IF <fs_mat>-zzlzcodsort <> lv_new_code.
          " Speichere Änderung in Ergebnistabelle
          READ TABLE gt_results ASSIGNING FIELD-SYMBOL(<fs_result>)
               WITH KEY matnr = lv_matnr.

          IF sy-subrc = 0.
            <fs_result>-old_code_sort = <fs_mat>-zzlzcodsort.
            <fs_result>-new_code_sort = lv_new_code.
            <fs_result>-changed_sort = abap_true.
          ELSE.
            APPEND VALUE #(
              matnr = lv_matnr
              old_code_sort = <fs_mat>-zzlzcodsort
              new_code_sort = lv_new_code
              changed_sort = abap_true
            ) TO gt_results.
          ENDIF.

          " Update Material mit neuem Code
          <fs_mat>-zzlzcodsort = lv_new_code.
          ADD 1 TO lv_changes.
        ENDIF.
      ENDLOOP.

      " Beende wenn keine Änderungen mehr
      IF lv_changes = 0.
        EXIT.
      ENDIF.
    ENDDO.
  ENDMETHOD.

  METHOD apply_sortiment_rules.
    DATA: lv_position_4      TYPE char1,
          lv_has_s           TYPE abap_bool,
          lv_has_o           TYPE abap_bool,
          lv_has_e           TYPE abap_bool,
          lv_has_n           TYPE abap_bool,
          lv_has_a           TYPE abap_bool,
          lv_has_c           TYPE abap_bool,
          lv_parent_count    TYPE i,
          lv_sonder_weighted TYPE p DECIMALS 3,
          lv_core_weighted   TYPE p DECIMALS 3,
          lv_total_weighted  TYPE p DECIMALS 3,
          lv_percentage      TYPE p DECIMALS 2,
          lv_pct_int         TYPE i,
          ls_current_mat     TYPE ty_material,
          ls_parent          TYPE ty_material,
          ls_relation        TYPE ty_bom_relation,
          ls_consumption     TYPE ty_consumption.

    WRITE: / ''.
    WRITE: / '╔══════════════════════════════════════════════════════════╗'.
    WRITE: / '║ apply_sortiment_rules (NEU) für:', iv_matnr.
    WRITE: / '╚══════════════════════════════════════════════════════════╝'.

    " ══════════════════════════════════════════════════════════════════
    " SCHRITT 1: HOLE AKTUELLES MATERIAL
    " ══════════════════════════════════════════════════════════════════
    READ TABLE gt_materials WITH TABLE KEY matnr = iv_matnr INTO ls_current_mat.
    IF sy-subrc <> 0.
      WRITE: / '  ERROR: Material nicht in gt_materials!'.
      rv_code = 'N0X0'.
      RETURN.
    ENDIF.

    " *** NEU: Status 99 = Ausgelaufen → Leerer Code ***
    IF ls_current_mat-mstae = '99'.
      WRITE: / '  Status 99 (Ausgelaufen) → Code wird geleert'.
      rv_code = ''.
      RETURN.
    ENDIF.

    " Initialisiere Code falls leer
    IF ls_current_mat-zzlzcodsort IS INITIAL.
      ls_current_mat-zzlzcodsort = 'N0X0'.
      MODIFY TABLE gt_materials FROM ls_current_mat.
    ENDIF.

    WRITE: / '  Aktueller Code:', ls_current_mat-zzlzcodsort.

    " ══════════════════════════════════════════════════════════════════
    " SCHRITT 2: POSITION 4 (MANUELLE SPERRE)
    " ══════════════════════════════════════════════════════════════════
    IF strlen( ls_current_mat-zzlzcodsort ) >= 4.
      lv_position_4 = ls_current_mat-zzlzcodsort+3(1).
    ELSE.
      lv_position_4 = '0'.
    ENDIF.

    IF lv_position_4 = '1'.
      WRITE: / '  -> GESPERRT (Position 4 = 1), Code bleibt'.
      rv_code = ls_current_mat-zzlzcodsort.
      RETURN.
    ENDIF.

    " ══════════════════════════════════════════════════════════════════
    " SCHRITT 3: SAMMLE PARENTS UND ANALYSIERE TYPEN
    " ══════════════════════════════════════════════════════════════════
    WRITE: / '  Analysiere Parents:'.
    CLEAR: lv_parent_count, lv_has_s, lv_has_o, lv_has_e,
           lv_has_n, lv_has_a, lv_has_c.

    LOOP AT gt_bom_relations INTO ls_relation WHERE child = iv_matnr.
      ADD 1 TO lv_parent_count.

      READ TABLE gt_materials WITH TABLE KEY matnr = ls_relation-parent INTO ls_parent.
      IF sy-subrc <> 0.
        WRITE: / '    Parent', ls_relation-parent, 'NICHT GEFUNDEN!'.
        CONTINUE.
      ENDIF.

      " *** NEU: Parent mit Status 99 überspringen ***
      IF ls_parent-mstae = '99'.
        WRITE: / '    Parent', ls_relation-parent, 'Status 99 → übersprungen'.
        SUBTRACT 1 FROM lv_parent_count.
        CONTINUE.
      ENDIF.

      " Überspringe ZZZZ und leere Codes
      IF ls_parent-zzlzcodsort = 'ZZZZ' OR ls_parent-zzlzcodsort IS INITIAL.
        WRITE: / '    Parent', ls_relation-parent, 'übersprungen (ZZZZ/leer)'.
        CONTINUE.
      ENDIF.

      WRITE: / '    Parent:', ls_relation-parent,
               'Code:', ls_parent-zzlzcodsort,
               'Typ:', ls_parent-zzlzcodsort(1).

      " Setze Flags basierend auf 1. Stelle
      CASE ls_parent-zzlzcodsort(1).
        WHEN 'S'. lv_has_s = abap_true.
        WHEN 'O'. lv_has_o = abap_true.
        WHEN 'E'. lv_has_e = abap_true.
          "  WHEN 'N'. lv_has_n = abap_true.
        WHEN 'A'. lv_has_a = abap_true.
        WHEN 'C'. lv_has_c = abap_true.
      ENDCASE.
    ENDLOOP.

    WRITE: / '  Anzahl gültige Parents:', lv_parent_count.
    WRITE: / '  Flags: S=', lv_has_s, 'O=', lv_has_o, 'E=', lv_has_e,
             'C=', lv_has_c, 'N=', lv_has_n, 'A=', lv_has_a.

    " ══════════════════════════════════════════════════════════════════
    " SCHRITT 4: KEINE PARENTS → CODE BLEIBT
    " ══════════════════════════════════════════════════════════════════
    IF lv_parent_count = 0.
      rv_code = ls_current_mat-zzlzcodsort.
      WRITE: / '  -> Keine Parents, Code bleibt:', rv_code.
      RETURN.
    ENDIF.

    " ══════════════════════════════════════════════════════════════════
    " SCHRITT 5: REINE FÄLLE (NUR EIN TYP VORHANDEN)
    " ══════════════════════════════════════════════════════════════════

    " REGEL 1: Nur C → C0X*
    IF lv_has_c = abap_true AND
       lv_has_s = abap_false AND lv_has_o = abap_false AND
       lv_has_e = abap_false   AND lv_has_a = abap_false.
      rv_code = 'C0X' && lv_position_4.
      WRITE: / '  REGEL 1: Nur C → C0X*'.
      RETURN.
    ENDIF.

    " REGEL 2: Nur E → E0X*
    IF lv_has_e = abap_true AND
       lv_has_s = abap_false AND lv_has_o = abap_false AND
       lv_has_c = abap_false   AND lv_has_a = abap_false.
      rv_code = 'E0X' && lv_position_4.
      WRITE: / '  REGEL 2: Nur E → E0X*'.
      RETURN.
    ENDIF.

    " REGEL 3: Nur O → O0X*
    IF lv_has_o = abap_true AND
       lv_has_s = abap_false AND lv_has_e = abap_false AND
       lv_has_c = abap_false  AND lv_has_a = abap_false.
      rv_code = 'O0X' && lv_position_4.
      WRITE: / '  REGEL 3: Nur O → O0X*'.
      RETURN.
    ENDIF.

    " REGEL 4: Nur N → N0X*
*  IF lv_has_n = abap_true AND
*     lv_has_s = abap_false AND lv_has_o = abap_false AND
*     lv_has_e = abap_false AND lv_has_c = abap_false AND lv_has_a = abap_false.
*    rv_code = 'N0X' && lv_position_4.
*    WRITE: / '  REGEL 4: Nur N → N0X*'.
*    RETURN.
*  ENDIF.

    " REGEL 5: Nur A → A0X*
    IF lv_has_a = abap_true AND
       lv_has_s = abap_false AND lv_has_o = abap_false AND
       lv_has_e = abap_false AND lv_has_c = abap_false  .
      rv_code = 'A0X' && lv_position_4.
      WRITE: / '  REGEL 5: Nur A → A0X*'.
      RETURN.
    ENDIF.

    " ══════════════════════════════════════════════════════════════════
    " SCHRITT 6: GEMISCHTE FÄLLE → PROZENTBERECHNUNG
    " ══════════════════════════════════════════════════════════════════
    WRITE: / '  GEMISCHTER FALL → Prozentberechnung'.
    WRITE: / '  Berechne Sonder-Anteil (O + E = 100%, C/N/A = 0%):'.

    CLEAR: lv_sonder_weighted, lv_core_weighted, lv_total_weighted.

    " Durchlaufe alle Parents und berechne gewichtete Anteile
    LOOP AT gt_bom_relations INTO ls_relation WHERE child = iv_matnr.
      READ TABLE gt_materials WITH TABLE KEY matnr = ls_relation-parent INTO ls_parent.
      IF sy-subrc <> 0 OR ls_parent-zzlzcodsort IS INITIAL OR
         ls_parent-zzlzcodsort = 'ZZZZ'.
        CONTINUE.
      ENDIF.

      " *** NEU: Parent mit Status 99 überspringen ***
      IF ls_parent-mstae = '99'.
        CONTINUE.
      ENDIF.

      " Hole Verbrauch (mit Fallback)
      READ TABLE gt_consumption WITH KEY matnr = ls_parent-matnr INTO ls_consumption.
      DATA: lv_parent_verbrauch TYPE p DECIMALS 2.

      IF sy-subrc = 0 AND ls_consumption-gsv_korr > 0.
        lv_parent_verbrauch = ls_consumption-gsv_korr.
      ELSE.
        lv_parent_verbrauch = 1.
      ENDIF.

      " Gewichtete Menge = Menge × Verbrauch
      DATA(lv_gewichtete_menge) = ls_relation-menge * lv_parent_verbrauch.

      WRITE: / '    Parent:', ls_parent-matnr,
               '    Code:', ls_parent-zzlzcodsort,
               '    Menge:', ls_relation-menge,
               '    Verbrauch:', lv_parent_verbrauch,
               '    Gewicht:', lv_gewichtete_menge.

      " Klassifiziere Parent-Code
      CASE ls_parent-zzlzcodsort(1).
        WHEN 'O' OR 'E'.
          " O und E = 100% Sonder
          lv_sonder_weighted = lv_sonder_weighted + lv_gewichtete_menge.
          WRITE: '      → 100% Sonder'.

        WHEN 'S'.
          " S-Code: Hole Prozentsatz aus Code
          DATA(lv_s_prozent) = get_code_percentage( ls_parent-zzlzcodsort ).
          lv_sonder_weighted = lv_sonder_weighted +
                              ( lv_gewichtete_menge * lv_s_prozent / 100 ).
          lv_core_weighted = lv_core_weighted +
                            ( lv_gewichtete_menge * ( 100 - lv_s_prozent ) / 100 ).
          WRITE: '      → ', lv_s_prozent, '% Sonder'.

        WHEN 'C'  OR 'A'.
          " C, N, A = 0% Sonder (100% Core)
          lv_core_weighted = lv_core_weighted + lv_gewichtete_menge.
          WRITE: '      → 0% Sonder (Core)'.
      ENDCASE.
    ENDLOOP.

    " Berechne Gesamtprozentsatz
    lv_total_weighted = lv_sonder_weighted + lv_core_weighted.

    IF lv_total_weighted > 0.
      lv_percentage = ( lv_sonder_weighted / lv_total_weighted ) * 100.
    ELSE.
      lv_percentage = 0.
    ENDIF.

    WRITE: / '  Sonder-Gewicht:', lv_sonder_weighted.
    WRITE: / '  Core-Gewicht:', lv_core_weighted.
    WRITE: / '  Total-Gewicht:', lv_total_weighted.
    WRITE: / '  Sonder-Prozentsatz:', lv_percentage, '%'.

    " ══════════════════════════════════════════════════════════════════
    " SCHRITT 7: SCHWELLENWERTE (FLOOR-LOGIK)
    " ══════════════════════════════════════════════════════════════════

    " Mini-Schwelle: 0 < % < 3 → Core
    IF lv_percentage > 0 AND lv_percentage < 3.
      rv_code = 'C0X' && lv_position_4.
      WRITE: / '  Schwelle: <3% → C0X*'.
      RETURN.
    ENDIF.

    " Floor-Konvertierung (14.73% → 14)
    lv_pct_int = lv_percentage.

    rv_code = COND #(
      WHEN lv_pct_int >= 100 THEN 'S0X'
      WHEN lv_pct_int >=  85 THEN 'S9T'
      WHEN lv_pct_int >=  75 THEN 'S8T'
      WHEN lv_pct_int >=  65 THEN 'S7T'
      WHEN lv_pct_int >=  55 THEN 'S6T'
      WHEN lv_pct_int >=  45 THEN 'S5T'
      WHEN lv_pct_int >=  35 THEN 'S4T'
      WHEN lv_pct_int >=  25 THEN 'S3T'
      WHEN lv_pct_int >=  15 THEN 'S2T'
      WHEN lv_pct_int >=   5 THEN 'S1T'
      WHEN lv_pct_int >=   3 THEN 'S0T'
      ELSE 'S0X'
    ) && lv_position_4.

    WRITE: / '  Neuer Code (nach Schwellen):', rv_code.
    WRITE: / '╚══════════════════════════════════════════════════════════╝'.

  ENDMETHOD.



  METHOD calculate_sonder_percentage.
    DATA: lv_sonder_weighted  TYPE p DECIMALS 3,
          lv_core_weighted    TYPE p DECIMALS 3,
          lv_total_weighted   TYPE p DECIMALS 3,
          lv_parent_verbrauch TYPE p DECIMALS 3,
          lv_gewichtete_menge TYPE p DECIMALS 3,
          lv_sonder_prozent   TYPE p DECIMALS 2,
          ls_parent           TYPE ty_material,
          ls_relation         TYPE ty_bom_relation,
          ls_consumption      TYPE ty_consumption.

    CLEAR: rv_percentage, lv_sonder_weighted, lv_core_weighted, lv_total_weighted.

    LOOP AT gt_bom_relations INTO ls_relation WHERE child = iv_matnr.

      READ TABLE gt_materials WITH TABLE KEY matnr = ls_relation-parent INTO ls_parent.
      IF sy-subrc <> 0.
        CONTINUE.
      ENDIF.

      READ TABLE gt_consumption WITH KEY matnr = ls_parent-matnr INTO ls_consumption.
      IF sy-subrc = 0 AND ls_consumption-gsv_korr > 0.
        lv_parent_verbrauch = ls_consumption-gsv_korr.
      ELSE.
        lv_parent_verbrauch = 1.
      ENDIF.



      lv_gewichtete_menge = ls_relation-menge * lv_parent_verbrauch.

      " S, O und E zählen als Sonder
      CASE ls_parent-zzlzcodsort(1).
        WHEN 'S' OR 'O' OR 'E'.
          " *** NEU: Prüfe Position 3 für 'X' (100% Sonder) ***
          IF strlen( ls_parent-zzlzcodsort ) >= 3 AND ls_parent-zzlzcodsort+2(1) = 'X'.
            " 100% Sonder (O0X, E0X, S0X)
            lv_sonder_weighted = lv_sonder_weighted + lv_gewichtete_menge.
          ELSE.
            " Prozent-Anteil berechnen
            DATA(lv_parent_code) = ls_parent-zzlzcodsort.
            DATA(lv_sonder_prozent_temp) = get_code_percentage( lv_parent_code ).
            lv_sonder_prozent = lv_sonder_prozent_temp.

            lv_sonder_weighted = lv_sonder_weighted +
                                ( lv_gewichtete_menge * lv_sonder_prozent / 100 ).
            lv_core_weighted = lv_core_weighted +
                              ( lv_gewichtete_menge * ( 100 - lv_sonder_prozent ) / 100 ).
          ENDIF.

        WHEN 'C' OR 'N' OR 'A'.
          " 100% Core
          lv_core_weighted = lv_core_weighted + lv_gewichtete_menge.
      ENDCASE.
    ENDLOOP.

    lv_total_weighted = lv_sonder_weighted + lv_core_weighted.

    IF lv_total_weighted > 0.
      rv_percentage = ( lv_sonder_weighted / lv_total_weighted ) * 100.
    ELSE.
      rv_percentage = 0.
    ENDIF.
  ENDMETHOD.


  METHOD get_code_percentage.
    " %-Wert aus S/O/E-Code ableiten (…nT / …0X) – Mittelwerte je Range
    DATA: lv_digit TYPE c.
    lv_digit = iv_code+1(1).

    rv_percentage = COND ty_percentage(
      WHEN iv_code+2(1) = 'X' AND lv_digit = '0' THEN 100     " *0X = 100%
      WHEN lv_digit = '9' THEN '92'                           " 85–99 → 92.0
      WHEN lv_digit = '8' THEN '79.5'                         " 75–84 → 79.5
      WHEN lv_digit = '7' THEN '69.5'                         " 65–74 → 69.5
      WHEN lv_digit = '6' THEN '59.5'
      WHEN lv_digit = '5' THEN '49.5'
      WHEN lv_digit = '4' THEN '39.5'
      WHEN lv_digit = '3' THEN '29.5'
      WHEN lv_digit = '2' THEN '19.5'
      WHEN lv_digit = '1' THEN '9.5'
      WHEN lv_digit = '0' AND iv_code+2(1) = 'T' THEN '2'     " *0T = ~2%
      ELSE '0'
    ).
  ENDMETHOD.



  METHOD build_graph_nodes.
    DATA: lv_verbrauch_text  TYPE string,
          lv_child_count     TYPE i,
          lv_verbrauch_monat TYPE p DECIMALS 2,
          lv_is_debug        TYPE abap_bool.

    " Prüfe ob Debug-Material
    lv_is_debug = me->is_debug_material( iv_matnr ).

    " Debug-Ausgabe für TOP-Material
    IF iv_level = 0 AND lv_is_debug = abap_true.
      "Write: / ''.
      "Write: / '╔════════════════════════════════════════════════════════════╗'.
      "Write: / '║ BUILD_GRAPH_NODES für TOP-Material:', iv_matnr.
      "Write: / '╚════════════════════════════════════════════════════════════╝'.
      "Write: / 'Anzahl Beziehungen in gt_bom_relations:', lines( gt_bom_relations ).
    ENDIF.

    " Sicherheitsabbruch bei zu tiefer Rekursion
    IF iv_level > 20.
      IF lv_is_debug = abap_true.
        "Write: / '*** ABBRUCH: Level > 20 erreicht ***'.
      ENDIF.
      RETURN.
    ENDIF.

    " Hole Stammdaten des aktuellen Knotens
    READ TABLE gt_materials WITH KEY matnr = iv_matnr INTO DATA(ls_material).
    IF sy-subrc <> 0.
      IF lv_is_debug = abap_true.
        "Write: / '*** Material', iv_matnr, 'nicht in gt_materials gefunden ***'.
      ENDIF.
      RETURN.
    ENDIF.

    " ===== VERBRAUCHSDATEN MIT FORMATIERUNG =====
    READ TABLE gt_consumption WITH KEY matnr = iv_matnr INTO DATA(ls_consumption).

    IF sy-subrc = 0 AND ls_consumption-gsv_korr > 0.
      " Berechne Durchschnitt pro Monat
      lv_verbrauch_monat = ls_consumption-gsv_korr / 12.

      " Formatiere mit Tausender-Trennzeichen und ohne Dezimalstellen
      lv_verbrauch_text = |{ lv_verbrauch_monat NUMBER = USER DECIMALS = 0 } / Mon|.

      IF lv_is_debug = abap_true.
        "Write: / 'Material', iv_matnr, ':'.
        "Write: / '  Jahresverbrauch:', ls_consumption-gsv_korr.
        "Write: / '  Monatsverbrauch:', lv_verbrauch_monat.
        "Write: / '  Formatiert:', lv_verbrauch_text.
      ENDIF.
    ELSE.
      " Kein Verbrauch vorhanden
      lv_verbrauch_text = '0'.

      IF lv_is_debug = abap_true.
        "Write: / 'Material', iv_matnr, ': KEIN Verbrauch in gt_consumption'.
      ENDIF.
    ENDIF.

    " ===== KNOTEN ZUR AUSGABE HINZUFÜGEN =====
    APPEND VALUE #(
      node_key       = iv_matnr
      parent_key     = iv_parent_key
      matnr          = ls_material-matnr
      menge          = iv_menge
      zzlzcod        = ls_material-zzlzcod
      zzlzcodsort    = ls_material-zzlzcodsort
      level          = iv_level
      verbrauch_text = lv_verbrauch_text
    ) TO ct_nodes.

    " ===== DEBUG: ZÄHLE KINDER =====
    IF lv_is_debug = abap_true.
      CLEAR lv_child_count.
      LOOP AT gt_bom_relations INTO DATA(ls_check) WHERE parent = iv_matnr.
        ADD 1 TO lv_child_count.
      ENDLOOP.

      IF lv_child_count > 0.
        "Write: / '  Material', iv_matnr, 'hat', lv_child_count, 'Komponenten'.
      ELSE.
        "Write: / '  Material', iv_matnr, 'hat KEINE Komponenten (Endprodukt)'.
      ENDIF.
    ENDIF.

    " ===== REKURSION: VERARBEITE ALLE KINDER =====
    LOOP AT gt_bom_relations INTO DATA(ls_bom_rel) WHERE parent = iv_matnr.

      IF lv_is_debug = abap_true.

        DATA: lv_next_level TYPE i.
        lv_next_level = iv_level + 1.
        "Write: / '    → Rekursion für Kind:', ls_bom_rel-child,
        "'Menge:', ls_bom_rel-menge,
        "'Level:', lv_next_level.




      ENDIF.

      " Rekursiver Aufruf für jedes Kind
      me->build_graph_nodes(
        EXPORTING
          iv_matnr      = ls_bom_rel-child
          iv_parent_key = iv_matnr
          iv_menge      = ls_bom_rel-menge
          iv_level      = iv_level + 1
        CHANGING
          ct_nodes      = ct_nodes
      ).
    ENDLOOP.

    " ===== ABSCHLUSS FÜR TOP-MATERIAL =====
    IF iv_level = 0 AND lv_is_debug = abap_true.
      "Write: / ''.
      "Write: / '╔════════════════════════════════════════════════════════════╗'.
      "Write: / '║ BUILD_GRAPH_NODES ABGESCHLOSSEN'.
      "Write: / '╚════════════════════════════════════════════════════════════╝'.
      "Write: / 'Anzahl Knoten im Baum:', lines( ct_nodes ).
    ENDIF.

  ENDMETHOD.




  " SORTIMENTSCODE ENDE





































































































  METHOD display_gozinto_graph.
    " Zeigt ALLE selektierten TOP-Materialien in EINEM ALV
    " mit Code-Änderungen und Berechnungsformel

    TYPES: BEGIN OF ty_tree_extended,
             top_material   TYPE matnr,
             hierarchy_text TYPE string,
             matnr          TYPE matnr,
             menge          TYPE kmpmg,
             code_alt       TYPE char4,
             code_neu       TYPE char4,
             zzlzcodsort    TYPE char4,
             verbrauch      TYPE string,
             formel         TYPE string,
             verwendungen   TYPE string,
             level          TYPE i,
           END OF ty_tree_extended.

    DATA: lt_all_display TYPE STANDARD TABLE OF ty_tree_extended,
          lt_nodes       TYPE tt_nodes,
          lv_mat_count   TYPE i.

    "Write: / ''.
    "Write: / '╔════════════════════════════════════════════════════════════╗'.
    "Write: / '║  KOMBINIERTE GRAPH-ANZEIGE (ALLE TOP-MATERIALIEN)         ║'.
    "Write: / '╚════════════════════════════════════════════════════════════╝'.

    " ========================================
    " FÜR JEDES SELEKTIERTE MATERIAL
    " ========================================
    LOOP AT s_matnr INTO DATA(ls_matnr_range).
      IF ls_matnr_range-sign = 'I' AND ls_matnr_range-option = 'EQ'.

        ADD 1 TO lv_mat_count.
        DATA(lv_top_mat) = ls_matnr_range-low.

        "Write: / 'Verarbeite TOP-Material', lv_mat_count, ':', lv_top_mat.

        " Baue Hierarchie
        CLEAR lt_nodes.
        me->build_graph_nodes(
          EXPORTING
            iv_matnr      = lv_top_mat
            iv_parent_key = ''
            iv_menge      = 1
            iv_level      = 0
          CHANGING
            ct_nodes      = lt_nodes ).

        IF lt_nodes IS INITIAL.
          "Write: / '  -> Keine Hierarchie gefunden'.
          CONTINUE.
        ENDIF.

        " ========================================
        " TRENNLINIE EINFÜGEN (außer beim ersten Material)
        " ========================================
        IF lv_mat_count > 1.
          APPEND VALUE #(
            top_material   = lv_top_mat
            hierarchy_text = '────────────────────────────────────────────────'
            level          = -1
          ) TO lt_all_display.
        ENDIF.

        " ========================================
        " VERARBEITE JEDEN KNOTEN
        " ========================================
        LOOP AT lt_nodes INTO DATA(ls_node).

          " *** CODE VORHER/NACHHER ***
          DATA: lv_code_alt TYPE char4,
                lv_code_neu TYPE char4.

          READ TABLE gt_results INTO DATA(ls_result)
               WITH KEY matnr = ls_node-matnr.
          IF sy-subrc = 0.
            lv_code_alt = ls_result-old_code.
            lv_code_neu = ls_result-new_code.
          ELSE.
            lv_code_alt = ls_node-zzlzcod.
            lv_code_neu = ls_node-zzlzcod.
          ENDIF.

          " *** FORMEL BERECHNEN ***
          DATA: lv_formel TYPE string.
          CLEAR lv_formel.

          IF ls_node-matnr CA 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.
            " Nur für Komponenten (B-Materialien)

            DATA: lt_parent_info  TYPE STANDARD TABLE OF string,
                  lv_total_verb   TYPE p DECIMALS 2,
                  lv_a_verb       TYPE p DECIMALS 2,
                  lv_parent_count TYPE i,
                  lv_has_a        TYPE abap_bool,
                  lv_has_n        TYPE abap_bool,
                  lv_has_e        TYPE abap_bool.

            CLEAR: lt_parent_info, lv_total_verb, lv_a_verb, lv_parent_count,
                   lv_has_a, lv_has_n, lv_has_e.

            " Sammle Parent-Informationen
            LOOP AT gt_bom_relations INTO DATA(ls_rel_form)
                 WHERE child = ls_node-matnr.

              ADD 1 TO lv_parent_count.

              READ TABLE gt_materials WITH KEY matnr = ls_rel_form-parent
                   INTO DATA(ls_parent_mat).
              IF sy-subrc <> 0.
                CONTINUE.
              ENDIF.

              " Hole Verbrauch
              READ TABLE gt_consumption WITH KEY matnr = ls_rel_form-parent
                   INTO DATA(ls_cons_form).

              DATA: lv_parent_verb TYPE p DECIMALS 2.
              IF sy-subrc = 0 AND ls_cons_form-gsv_korr > 0.
                lv_parent_verb = ls_cons_form-gsv_korr.
              ELSE.
                lv_parent_verb = 1.
              ENDIF.

              DATA: lv_gewicht TYPE p DECIMALS 2.
              lv_gewicht = ls_rel_form-menge * lv_parent_verb.
              lv_total_verb = lv_total_verb + lv_gewicht.

              " Prüfe Code-Typ
              CASE ls_parent_mat-zzlzcod+0(1).
                WHEN 'A'.
                  lv_has_a = abap_true.

                  DATA: lv_prozent TYPE p DECIMALS 2.
                  DATA(lv_digit) = ls_parent_mat-zzlzcod+1(1).

                  IF lv_digit = '0' AND ls_parent_mat-zzlzcod+2(1) = 'X'.
                    lv_prozent = 100.
                  ELSEIF lv_digit BETWEEN '1' AND '9'.
                    CASE lv_digit.
                      WHEN '0'. lv_prozent = 2.
                      WHEN '1'. lv_prozent = 9.
                      WHEN '2'. lv_prozent = 19.
                      WHEN '3'. lv_prozent = 29.
                      WHEN '4'. lv_prozent = 39.
                      WHEN '5'. lv_prozent = 49.
                      WHEN '6'. lv_prozent = 59.
                      WHEN '7'. lv_prozent = 69.
                      WHEN '8'. lv_prozent = 79.
                      WHEN '9'. lv_prozent = 92.
                    ENDCASE.
                  ELSEIF lv_digit = '0' AND ls_parent_mat-zzlzcod+2(1) = 'T'.
                    lv_prozent = 2.
                  ENDIF.

                  lv_a_verb = lv_a_verb + ( lv_gewicht * lv_prozent / 100 ).

                  DATA: lv_parent_text TYPE string.
                  lv_parent_text = |{ ls_rel_form-parent }({ ls_parent_mat-zzlzcod }):{ lv_parent_verb NUMBER = USER DECIMALS = 0 }×{ lv_prozent }%|.
                  APPEND lv_parent_text TO lt_parent_info.

                WHEN 'E'.
                  lv_has_e = abap_true.
                  lv_parent_text = |{ ls_rel_form-parent }(E0X):{ lv_parent_verb NUMBER = USER DECIMALS = 0 }|.
                  APPEND lv_parent_text TO lt_parent_info.

                WHEN 'N'.
                  lv_has_n = abap_true.
                  lv_parent_text = |{ ls_rel_form-parent }(N0X):{ lv_parent_verb NUMBER = USER DECIMALS = 0 }×0%|.
                  APPEND lv_parent_text TO lt_parent_info.

              ENDCASE.

            ENDLOOP.

            " Erstelle Formel-Text
            IF lv_parent_count > 0.
              DATA: lv_regel TYPE string.

              IF lv_has_e = abap_true AND lv_has_n = abap_true.
                lv_regel = 'E+N→N0X'.
                lv_formel = |Regel:{ lv_regel } (N dominiert)|.

              ELSEIF lv_has_e = abap_true AND lv_has_n = abap_false.
                lv_regel = 'E→E0X'.
                lv_formel = |Regel:{ lv_regel } (E dominiert)|.

              ELSEIF lv_has_n = abap_true AND lv_has_a = abap_true AND lv_has_e = abap_false.
                lv_regel = 'N+A→A0T'.
                lv_formel = |Regel:{ lv_regel } (fix A0T)|.

              ELSEIF lv_has_a = abap_true AND lv_has_n = abap_false AND lv_has_e = abap_false.
                DATA: lv_anteil TYPE p DECIMALS 2.
                IF lv_total_verb > 0.
                  lv_anteil = ( lv_a_verb / lv_total_verb ) * 100.
                ELSE.
                  lv_anteil = 0.
                ENDIF.

                lv_formel = |A-Anteil={ lv_anteil NUMBER = USER DECIMALS = 1 }% (|.

                DATA: lv_count TYPE i.
                LOOP AT lt_parent_info INTO DATA(lv_pinfo).
                  lv_count = lv_count + 1.
                  IF lv_count = 1.
                    lv_formel = |{ lv_formel }{ lv_pinfo }|.
                  ELSE.
                    lv_formel = |{ lv_formel }+{ lv_pinfo }|.
                  ENDIF.
                  IF lv_count >= 3.
                    IF lines( lt_parent_info ) > 3.
                      DATA(lv_rest) = lines( lt_parent_info ) - 3.
                      lv_formel = |{ lv_formel }+{ lv_rest }mehr|.
                    ENDIF.
                    EXIT.
                  ENDIF.
                ENDLOOP.

                lv_formel = |{ lv_formel })→{ lv_code_neu }|.

              ELSEIF lv_has_n = abap_true AND lv_has_a = abap_false AND lv_has_e = abap_false.
                lv_regel = 'Nur N'.
                lv_formel = |Regel:{ lv_regel } → N0X|.

              ELSE.
                lv_formel = 'Keine Vererbung'.
              ENDIF.

            ELSE.
              lv_formel = 'TOP-Material'.
            ENDIF.

          ELSE.
            lv_formel = 'TOP-Material (keine Vererbung)'.
          ENDIF.

          " *** VERWENDUNGEN ***
          DATA: lv_usage_text TYPE string.
          CLEAR lv_usage_text.

          IF ls_node-matnr CA 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.
            DATA: lt_usage_info   TYPE SORTED TABLE OF string
                                  WITH UNIQUE KEY table_line,
                  lv_usage_count  TYPE i,
                  lv_total_usages TYPE i.

            CLEAR lt_usage_info.

            LOOP AT gt_bom_relations INTO DATA(ls_rel_use)
                 WHERE child = ls_node-matnr.

              READ TABLE gt_consumption WITH KEY matnr = ls_rel_use-parent
                   INTO DATA(ls_cons_use).

              IF sy-subrc = 0 AND ls_cons_use-gsv_korr > 0 AND
                 ls_rel_use-parent NA 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.

                DATA: lv_verbrauch_monat TYPE p DECIMALS 0.
                lv_verbrauch_monat = ls_cons_use-gsv_korr / 12.

                DATA: lv_usage_entry TYPE string.
                lv_usage_entry = |{ ls_rel_use-parent }({ lv_verbrauch_monat NUMBER = USER DECIMALS = 0 })|.

                INSERT lv_usage_entry INTO TABLE lt_usage_info.
                lv_usage_count = lines( lt_usage_info ).

                IF lv_usage_count >= 10.
                  EXIT.
                ENDIF.
              ENDIF.
            ENDLOOP.

            IF lt_usage_info IS NOT INITIAL.
              lv_usage_text = 'Verwendet in: '.
              DATA: lv_first TYPE abap_bool VALUE abap_true.
              LOOP AT lt_usage_info INTO DATA(lv_usage_entry_text).
                IF lv_first = abap_true.
                  lv_usage_text = |{ lv_usage_text }{ lv_usage_entry_text }|.
                  lv_first = abap_false.
                ELSE.
                  lv_usage_text = |{ lv_usage_text }, { lv_usage_entry_text }|.
                ENDIF.
              ENDLOOP.

              CLEAR lv_total_usages.
              LOOP AT gt_bom_relations TRANSPORTING NO FIELDS
                   WHERE child = ls_node-matnr.
                ADD 1 TO lv_total_usages.
              ENDLOOP.

              IF lv_total_usages > 10.
                DATA: lv_remaining TYPE i.
                lv_remaining = lv_total_usages - 10.
                lv_usage_text = |{ lv_usage_text } (+{ lv_remaining } weitere)|.
              ENDIF.
            ELSE.
              lv_usage_text = ''.
            ENDIF.
          ENDIF.

          " *** ERSTELLE ANZEIGE-EINTRAG ***
          DATA(lv_indent) = repeat( val = '  ' occ = ls_node-level ).
          DATA(lv_prefix) = COND string(
            WHEN ls_node-level = 0 THEN '►'
            WHEN ls_node-level = 1 THEN '├─'
            WHEN ls_node-level = 2 THEN '│ └─'
            ELSE '│   └─' ).

          APPEND VALUE #(
            top_material   = lv_top_mat
            hierarchy_text = |{ lv_indent }{ lv_prefix } { ls_node-matnr }|
            matnr          = ls_node-matnr
            menge          = ls_node-menge
            code_alt       = lv_code_alt
            code_neu       = lv_code_neu
            zzlzcodsort    = ls_node-zzlzcodsort
            verbrauch      = ls_node-verbrauch_text
            formel         = lv_formel
            verwendungen   = lv_usage_text
            level          = ls_node-level
          ) TO lt_all_display.

        ENDLOOP.

      ENDIF.
    ENDLOOP.

    " ========================================
    " ANZEIGE ALS EIN ALV
    " ========================================
    IF lt_all_display IS INITIAL.
      MESSAGE 'Keine Daten für Graph-Anzeige' TYPE 'I'.
      RETURN.
    ENDIF.

    TRY.
        cl_salv_table=>factory(
          IMPORTING r_salv_table = DATA(lo_alv)
          CHANGING  t_table      = lt_all_display ).

        DATA(lo_columns) = lo_alv->get_columns( ).

        lo_columns->get_column( 'TOP_MATERIAL' )->set_long_text( 'TOP-Material' ).
        lo_columns->get_column( 'TOP_MATERIAL' )->set_output_length( 12 ).

        lo_columns->get_column( 'HIERARCHY_TEXT' )->set_long_text( 'Hierarchie' ).
        lo_columns->get_column( 'HIERARCHY_TEXT' )->set_output_length( 40 ).
        lo_columns->get_column( 'MATNR' )->set_visible( abap_false ).
        lo_columns->get_column( 'LEVEL' )->set_visible( abap_false ).
        lo_columns->get_column( 'MENGE' )->set_long_text( 'Menge' ).

        lo_columns->get_column( 'CODE_ALT' )->set_long_text( 'Code VORHER' ).
        lo_columns->get_column( 'CODE_NEU' )->set_long_text( 'Code NACHHER' ).
        lo_columns->get_column( 'CODE_ALT' )->set_output_length( 10 ).
        lo_columns->get_column( 'CODE_NEU' )->set_output_length( 10 ).

        lo_columns->get_column( 'ZZLZCODSORT' )->set_long_text( 'Sort-Code' ).
        lo_columns->get_column( 'VERBRAUCH' )->set_long_text( 'Verbrauch/Mon' ).

        lo_columns->get_column( 'FORMEL' )->set_long_text( 'Berechnungsformel' ).
        lo_columns->get_column( 'FORMEL' )->set_output_length( 60 ).

        lo_columns->get_column( 'VERWENDUNGEN' )->set_long_text( 'Verwendungen' ).
        lo_columns->get_column( 'VERWENDUNGEN' )->set_output_length( 80 ).
        lo_columns->set_optimize( ).

        lo_alv->get_display_settings( )->set_list_header(
          |Kombinierte Hierarchie: { lv_mat_count } TOP-Materialien| ).

        lo_alv->get_functions( )->set_all( ).

        lo_alv->display( ).

      CATCH cx_salv_msg.
        MESSAGE 'Fehler bei kombinierter Graph-Anzeige' TYPE 'E'.
    ENDTRY.

    "Write: / ''.
    "Write: / '╔════════════════════════════════════════════════════════════╗'.
    "Write: / '║  KOMBINIERTE ANZEIGE ABGESCHLOSSEN                         ║'.
    "Write: / '╚════════════════════════════════════════════════════════════╝'.
    "Write: / 'Anzahl TOP-Materialien:', lv_mat_count.
    "Write: / 'Anzahl Zeilen im ALV:', lines( lt_all_display ).

  ENDMETHOD.



  METHOD display_results.
    DATA: lo_alv TYPE REF TO cl_salv_table.

    TRY.
        cl_salv_table=>factory(
          IMPORTING
            r_salv_table = lo_alv
          CHANGING
            t_table = gt_results ).

        " Spaltenoptimierung
        lo_alv->get_columns( )->set_optimize( abap_true ).

        " Funktionen aktivieren
        lo_alv->get_functions( )->set_all( abap_true ).

        " Anzeige
        lo_alv->display( ).

      CATCH cx_salv_msg.
        MESSAGE 'Fehler bei ALV-Anzeige' TYPE 'E'.
    ENDTRY.
  ENDMETHOD.

ENDCLASS.

*----------------------------------------------------------------------*
* Hauptprogramm
*----------------------------------------------------------------------*
START-OF-SELECTION.

  "Ersetzte Parameter 251217
  DATA(p_batch) = CONV i( 5000 ).
  DATA(p_maxlv) = CONV i( 4 ).



  p_mm = abap_false.
  p_di = abap_true.    "Default wie vorher
  p_gozin = abap_false.
  "END




  " Validierungen
  " IF p_alles = abap_true AND s_matnr[] IS NOT INITIAL.
  "   MESSAGE 'Entweder "Alle Materialien" oder Selektion angeben' TYPE 'E'.
  " ENDIF.

  IF p_batch > 50000.
    MESSAGE 'Batch-Größe zu groß (max. 50000)' TYPE 'W'.
    p_batch = 50000.
  ENDIF.

  " Hauptverarbeitung
  DATA(lo_processor) = NEW lcl_lifecycle_processor( ).
  lo_processor->execute( ).

END-OF-SELECTION.
  " Ergebnisanzeige

  IF p_gozin = 'X'.
    lo_processor->display_results( ).
    " Graph-Anzeige für ein ausgewähltes Material
    " NEU: Graph-Anzeige für ALLE selektierten Materialien (max. 4 Stufen)
    DATA: lv_material_count TYPE i.

    LOOP AT s_matnr INTO DATA(ls_matnr_range).
      " Verarbeite jeden Eintrag im Range
      IF ls_matnr_range-sign = 'I' AND ls_matnr_range-option = 'EQ'.
        " Einzelnes Material
        ADD 1 TO lv_material_count.
        ""Write: / ''.
        ""Write: / '================================================'.
        ""Write: / 'Gozinto-Graph für Material:', ls_matnr_range-low.
        ""Write: / '================================================'.
        " lo_processor->display_gozinto_graph( iv_top_material = ls_matnr_range-low ).

      ELSEIF ls_matnr_range-sign = 'I' AND ls_matnr_range-option = 'BT'.
        " Range von Materialien
        SELECT matnr FROM mara
          INTO @DATA(lv_matnr)
          WHERE matnr BETWEEN @ls_matnr_range-low AND @ls_matnr_range-high.

          ADD 1 TO lv_material_count.
          ""Write: / ''.
          ""Write: / '================================================'.
          ""Write: / 'Gozinto-Graph für Material:', lv_matnr.
          ""Write: / '================================================'.
          lo_processor->display_gozinto_graph( iv_top_material = lv_matnr ).

          " Optional: Begrenzung auf z.B. max 10 Materialien
          IF lv_material_count >= 10.
            ""Write: / 'WARNUNG: Maximal 10 Graphen angezeigt. Weitere Materialien übersprungen.'.
            EXIT.
          ENDIF.
        ENDSELECT.
      ENDIF.
    ENDLOOP.


    lo_processor->display_gozinto_graph( iv_top_material = ls_matnr_range-low ).


    IF lv_material_count = 0.
      ""Write: / 'Keine Materialien für Gozinto-Graph gefunden.'.
    ELSE.
      ""Write: / ''.
      ""Write: / '================================================'.
      ""Write: / 'Gesamt', lv_material_count, 'Gozinto-Graphen angezeigt.'.
    ENDIF.



  ENDIF.
*&---------------------------------------------------------------------*
*& Form DEACTIVATE_JOB
*& Entfernt Startbedingung des Jobs VC_AUFLOESUNG_ZLO
*& Nur im produktiven Modus (p_test = abap_false)
*&---------------------------------------------------------------------*
FORM deactivate_job.
  DATA: lt_joblist TYPE STANDARD TABLE OF tbtcjob,
        ls_job     TYPE tbtcjob.

  " Nur im produktiven Modus
  IF p_test = abap_true.
    WRITE: / 'Testmodus: Job-Deaktivierung übersprungen'.
    RETURN.
  ENDIF.

  CLEAR: gv_job_was_active, gv_job_count.

  " Suche freigegebenen Job (Status 'S' = Scheduled/Released)
  CALL FUNCTION 'BP_JOB_SELECT'
    EXPORTING
      jobselect_dialog  = abap_false
      jobname           = gc_job_name
      username          = gc_job_user
    TABLES
      jobselect_joblist = lt_joblist
    EXCEPTIONS
      OTHERS            = 1.

  IF sy-subrc <> 0.
    WRITE: / 'WARNUNG: Job', gc_job_name, 'nicht gefunden'.
    RETURN.
  ENDIF.

  " Finde freigegebenen Job (Status S = Scheduled)
  LOOP AT lt_joblist INTO ls_job WHERE status = 'S'.
    EXIT.
  ENDLOOP.

  IF sy-subrc <> 0.
    WRITE: / 'INFO: Kein freigegebener Job', gc_job_name, 'gefunden'.
    RETURN.
  ENDIF.

  " Sichere Job-Count für spätere Reaktivierung
  gv_job_count = ls_job-jobcount.
  gv_job_was_active = abap_true.

  " Entferne Startbedingung (Job auf 'P' = Planned setzen)
  CALL FUNCTION 'BP_JOB_MODIFY'
    EXPORTING
      jobname    = gc_job_name
      jobcount   = gv_job_count
      new_status = 'P'
    EXCEPTIONS
      OTHERS     = 1.

  IF sy-subrc = 0.
    WRITE: / 'Job', gc_job_name, 'deaktiviert (Startbedingung entfernt)'.
  ELSE.
    WRITE: / 'FEHLER: Job', gc_job_name, 'konnte nicht deaktiviert werden'.
    CLEAR gv_job_was_active.
  ENDIF.

ENDFORM.

*&---------------------------------------------------------------------*
*& Form REACTIVATE_JOB
*& Setzt Startbedingung des Jobs wieder (01:00 nächster Tag, täglich)
*& Nur wenn Job vorher aktiv war und produktiver Modus
*&---------------------------------------------------------------------*
FORM reactivate_job.
  DATA: lv_next_date  TYPE sy-datum,
        lv_start_time TYPE sy-uzeit VALUE '010000'.

  " Nur im produktiven Modus und wenn Job vorher aktiv war
  IF p_test = abap_true.
    WRITE: / 'Testmodus: Job-Reaktivierung übersprungen'.
    RETURN.
  ENDIF.

  IF gv_job_was_active = abap_false.
    WRITE: / 'INFO: Job war nicht aktiv, keine Reaktivierung nötig'.
    RETURN.
  ENDIF.

  " Berechne nächsten Tag
  lv_next_date = sy-datum + 1.

  " Setze Startbedingung wieder (täglich um 01:00)
  CALL FUNCTION 'BP_JOB_MODIFY'
    EXPORTING
      jobname       = gc_job_name
      jobcount      = gv_job_count
      new_status    = 'S'
      new_sdlstrtdt = lv_next_date
      new_sdlstrttm = lv_start_time
      new_prddays   = 1
      new_periodic  = abap_true
    EXCEPTIONS
      OTHERS        = 1.

  IF sy-subrc = 0.
    WRITE: / 'Job', gc_job_name, 'reaktiviert für', lv_next_date, lv_start_time.
  ELSE.
    WRITE: / 'FEHLER: Job', gc_job_name, 'konnte nicht reaktiviert werden!'.
    WRITE: / 'ACHTUNG: Job muss manuell in SM37 freigegeben werden!'.
  ENDIF.

ENDFORM.

*----------------------------------------------------------------------*
* Unit-Tests (wenn p_test = 'X')
*----------------------------------------------------------------------*
AT SELECTION-SCREEN.
  IF p_test = abap_true AND sy-ucomm = 'ONLI'.
    " Test-Framework aktivieren
    PERFORM run_unit_tests.
  ENDIF.

*&---------------------------------------------------------------------*
*& Form run_unit_tests
*&---------------------------------------------------------------------*
FORM run_unit_tests.
  DATA: lv_test_result TYPE string.

  ""Write: / '=== UNIT TESTS ==='.

  " Test 1: Dominanzmatrix
  lv_test_result = 'Test Dominanzmatrix: '.
  " Implementierung der Testlogik
  ""Write: / lv_test_result, 'PASSED'.

  " Test 2: Zirkularitätsprüfung
  lv_test_result = 'Test Zirkularität: '.
  " Implementierung der Testlogik
  ""Write: / lv_test_result, 'PASSED'.
  "'KMAT', 'ZHAL', 'ZFER', 'HALB', 'FERT')
  "      AND   zzlzcod NE 'ZZZZ'.

  " Test 3: Batch-Verarbeitung
  lv_test_result = 'Test Batch-Verarbeitung: '.
  " Implementierung der Testlogik
  ""Write: / lv_test_result, 'PASSED'.

  ""Write: / '=================='.
ENDFORM.


*&---------------------------------------------------------------------*
*& Form PROTECT_VKNR_FROM_UPDATE
*& Schützt Materialien vor Update im LZCode-Modus
*&
*& Parameter iv_mode:
*&   'T' = Nur TOP-Materialien (ohne Parents in gt_bom_relations)
*&   '1' = Alle mit Position 4 = '1' im Code
*&   'B' = Beides kombiniert
*&---------------------------------------------------------------------*
FORM protect_vknr_from_update USING iv_mode TYPE char1.
  DATA: lv_removed_count TYPE i,
        lv_code_char4    TYPE char4.

  " Nur im LZCode-Modus aktiv
  IF p_lzc <> abap_true.
    RETURN.
  ENDIF.

  " Durchlaufe gt_results und schütze Materialien
  LOOP AT gt_results ASSIGNING FIELD-SYMBOL(<fs_result>).

    DATA(lv_protect) = abap_false.

    " ══════════════════════════════════════════════════════════════
    " PRÜFUNG 1: TOP-Material? (keine Parents)
    " ══════════════════════════════════════════════════════════════
    IF iv_mode = 'T' OR iv_mode = 'B'.
      READ TABLE gt_bom_relations WITH KEY child = <fs_result>-matnr
           TRANSPORTING NO FIELDS.
      IF sy-subrc <> 0.
        " Keine Parents → TOP-Material
        lv_protect = abap_true.
      ENDIF.
    ENDIF.

    " ══════════════════════════════════════════════════════════════
    " PRÜFUNG 2: Position 4 = '1'?
    " ══════════════════════════════════════════════════════════════
    IF iv_mode = '1' OR iv_mode = 'B'.
      lv_code_char4 = <fs_result>-new_code.
      IF strlen( lv_code_char4 ) >= 4.
        IF lv_code_char4+3(1) = '1'.
          lv_protect = abap_true.
        ENDIF.
      ENDIF.
    ENDIF.

    " ══════════════════════════════════════════════════════════════
    " SCHÜTZEN: changed = false, Code bleibt
    " ══════════════════════════════════════════════════════════════
    IF lv_protect = abap_true.
      <fs_result>-changed = abap_false.
      <fs_result>-new_code = <fs_result>-old_code.
      ADD 1 TO lv_removed_count.
    ENDIF.

  ENDLOOP.

  IF lv_removed_count > 0.
    WRITE: / 'VKNR-Schutz (Modus:', iv_mode, '):',
             lv_removed_count, 'Materialien geschützt'.
  ENDIF.

ENDFORM.

*&---------------------------------------------------------------------*
*& Globale Tabelle für gesicherte VKNR-Codes
*&---------------------------------------------------------------------*
TYPES: BEGIN OF ty_vknr_backup,
         matnr    TYPE matnr,
         old_code TYPE char4,
       END OF ty_vknr_backup.

DATA: gt_vknr_backup TYPE STANDARD TABLE OF ty_vknr_backup.

*&---------------------------------------------------------------------*
*& Globale Variablen für Job-Steuerung
*&---------------------------------------------------------------------*
DATA: gv_job_was_active TYPE abap_bool,
      gv_job_count      TYPE tbtcjob-jobcount.

CONSTANTS: gc_job_name   TYPE tbtcjob-jobname VALUE 'VC_AUFLOESUNG_ZLO',
           gc_job_user   TYPE tbtcjob-authcknam VALUE 'KOI'.

*&---------------------------------------------------------------------*
*& Form SAVE_VKNR_CODES
*& Sichert Codes von Materialien mit Position 4 = '1'
*& UND Materialien ohne Buchstaben (nur numerisch = VKNR)
*&---------------------------------------------------------------------*
FORM save_vknr_codes.
  DATA: lv_code_char4 TYPE char4,
        lv_matnr_str  TYPE string.

  CLEAR gt_vknr_backup.

  LOOP AT gt_materials INTO DATA(ls_mat).

    DATA(lv_save) = abap_false.

    " ══════════════════════════════════════════════════════════════
    " PRÜFUNG 1: Position 4 = '1'
    " ══════════════════════════════════════════════════════════════
    lv_code_char4 = ls_mat-zzlzcod.
    IF strlen( lv_code_char4 ) >= 4 AND lv_code_char4+3(1) = '1'.
      lv_save = abap_true.
    ENDIF.

    " ══════════════════════════════════════════════════════════════
    " PRÜFUNG 2: MATNR nur numerisch (keine Buchstaben = VKNR)
    " ══════════════════════════════════════════════════════════════
    lv_matnr_str = ls_mat-matnr.
    CONDENSE lv_matnr_str NO-GAPS.

    IF lv_matnr_str CO '0123456789'.
      lv_save = abap_true.
    ENDIF.

    " ══════════════════════════════════════════════════════════════
    " SICHERN
    " ══════════════════════════════════════════════════════════════
    IF lv_save = abap_true.
      APPEND VALUE #( matnr    = ls_mat-matnr
                      old_code = ls_mat-zzlzcod ) TO gt_vknr_backup.
    ENDIF.

  ENDLOOP.

  IF lines( gt_vknr_backup ) > 0.
    WRITE: / 'VKNR-Codes gesichert:', lines( gt_vknr_backup ), 'Materialien'.
  ENDIF.

ENDFORM.

*&---------------------------------------------------------------------*
*& Form RESTORE_VKNR_CODES
*& Rekonstruiert gesicherte VKNR-Codes in gt_results
*&---------------------------------------------------------------------*
FORM restore_vknr_codes.
  DATA: lv_restored TYPE i.

  LOOP AT gt_vknr_backup INTO DATA(ls_backup).

    " Finde Material in gt_results
    READ TABLE gt_results ASSIGNING FIELD-SYMBOL(<fs_result>)
         WITH KEY matnr = ls_backup-matnr.

    IF sy-subrc = 0.
      " Rekonstruiere: new_code = old_code, changed = false
      <fs_result>-new_code = ls_backup-old_code.
      <fs_result>-changed  = abap_false.
      ADD 1 TO lv_restored.
    ENDIF.

  ENDLOOP.

  IF lv_restored > 0.
    WRITE: / 'VKNR-Codes rekonstruiert:', lv_restored, 'Materialien'.
  ENDIF.

ENDFORM. ///  bitte das einbauen, weisst du wie ?Perfekt, jetzt habe ich alle Informationen. Hier ist der komplette, einbaufertige Code:

Zusammenfassung der Änderungen



Was
Wo
Beschreibung




Globale Variablen
Nach DATA: gt_vknr_backup...
Job-Steuerungsvariablen


FORM deactivate_job
Am Ende des Programms
Entfernt Startbedingung vor Lauf


FORM reactivate_job
Am Ende des Programms
Setzt Startbedingung nach Lauf


Aufruf in execute
In Methode execute
Aufrufe am Anfang und Ende




Code zum Einfügen
1. Globale Variablen (nach Zeile DATA: gt_vknr_backup TYPE STANDARD TABLE OF ty_vknr_backup.)
*&---------------------------------------------------------------------*
*& Globale Variablen für Job-Steuerung
*&---------------------------------------------------------------------*
DATA: gv_job_was_active TYPE abap_bool,
      gv_job_count      TYPE tbtcjob-jobcount.

CONSTANTS: gc_job_name   TYPE tbtcjob-jobname VALUE 'VC_AUFLOESUNG_ZLO',
           gc_job_user   TYPE tbtcjob-authcknam VALUE 'KOI'.


2. Änderung in Methode execute (Klasse lcl_lifecycle_processor)
Ersetze die komplette Methode execute durch:
  METHOD execute.
    DATA: lv_start_time TYPE timestampl,
          lv_end_time   TYPE timestampl.

    GET TIME STAMP FIELD lv_start_time.

    " *** NEU: Job deaktivieren (nur produktiv) ***
    PERFORM deactivate_job.

    " Phase 1: Datenladen
    load_materials( ).

    me->debug_show_relations( ).

    load_consumption_data( ).

    " Phase 5: Berechnungen basierend auf Auswahl durchführen
    IF p_lzc = abap_true.
      PERFORM save_vknr_codes.
      calculate_inheritance( ).
      PERFORM restore_vknr_codes.

      IF p_upda = abap_true AND p_test = abap_false.
        update_database( ).
      ENDIF.

    ELSEIF p_sort = abap_true.
      calculate_sortiment_inhe( ).

      IF p_upda = abap_true AND p_test = abap_false.
        update_database_sortiment( ).
      ENDIF.
    ENDIF.

    GET TIME STAMP FIELD lv_end_time.
    DATA(lv_runtime) = lv_end_time - lv_start_time.

    " *** NEU: Job reaktivieren (nur produktiv) ***
    PERFORM reactivate_job.

  ENDMETHOD.


3. Neue FORMs (am Ende des Programms, vor AT SELECTION-SCREEN)
*&---------------------------------------------------------------------*
*& Form DEACTIVATE_JOB
*& Entfernt Startbedingung des Jobs VC_AUFLOESUNG_ZLO
*& Nur im produktiven Modus (p_test = abap_false)
*&---------------------------------------------------------------------*
FORM deactivate_job.
  DATA: lt_joblist TYPE STANDARD TABLE OF tbtcjob,
        ls_job     TYPE tbtcjob.

  " Nur im produktiven Modus
  IF p_test = abap_true.
    WRITE: / 'Testmodus: Job-Deaktivierung übersprungen'.
    RETURN.
  ENDIF.

  CLEAR: gv_job_was_active, gv_job_count.

  " Suche freigegebenen Job (Status 'S' = Scheduled/Released)
  CALL FUNCTION 'BP_JOB_SELECT'
    EXPORTING
      jobselect_dialog  = abap_false
      jobname           = gc_job_name
      username          = gc_job_user
    TABLES
      jobselect_joblist = lt_joblist
    EXCEPTIONS
      OTHERS            = 1.

  IF sy-subrc <> 0.
    WRITE: / 'WARNUNG: Job', gc_job_name, 'nicht gefunden'.
    RETURN.
  ENDIF.

  " Finde freigegebenen Job (Status S = Scheduled)
  LOOP AT lt_joblist INTO ls_job WHERE status = 'S'.
    EXIT.
  ENDLOOP.

  IF sy-subrc <> 0.
    WRITE: / 'INFO: Kein freigegebener Job', gc_job_name, 'gefunden'.
    RETURN.
  ENDIF.

  " Sichere Job-Count für spätere Reaktivierung
  gv_job_count = ls_job-jobcount.
  gv_job_was_active = abap_true.

  " Entferne Startbedingung (Job auf 'P' = Planned setzen)
  CALL FUNCTION 'BP_JOB_MODIFY'
    EXPORTING
      jobname    = gc_job_name
      jobcount   = gv_job_count
      new_status = 'P'
    EXCEPTIONS
      OTHERS     = 1.

  IF sy-subrc = 0.
    WRITE: / 'Job', gc_job_name, 'deaktiviert (Startbedingung entfernt)'.
  ELSE.
    WRITE: / 'FEHLER: Job', gc_job_name, 'konnte nicht deaktiviert werden'.
    CLEAR gv_job_was_active.
  ENDIF.

ENDFORM.

*&---------------------------------------------------------------------*
*& Form REACTIVATE_JOB
*& Setzt Startbedingung des Jobs wieder (01:00 nächster Tag, täglich)
*& Nur wenn Job vorher aktiv war und produktiver Modus
*&---------------------------------------------------------------------*
FORM reactivate_job.
  DATA: lv_next_date  TYPE sy-datum,
        lv_start_time TYPE sy-uzeit VALUE '010000'.

  " Nur im produktiven Modus und wenn Job vorher aktiv war
  IF p_test = abap_true.
    WRITE: / 'Testmodus: Job-Reaktivierung übersprungen'.
    RETURN.
  ENDIF.

  IF gv_job_was_active = abap_false.
    WRITE: / 'INFO: Job war nicht aktiv, keine Reaktivierung nötig'.
    RETURN.
  ENDIF.

  " Berechne nächsten Tag
  lv_next_date = sy-datum + 1.

  " Setze Startbedingung wieder (täglich um 01:00)
  CALL FUNCTION 'BP_JOB_MODIFY'
    EXPORTING
      jobname       = gc_job_name
      jobcount      = gv_job_count
      new_status    = 'S'
      new_sdlstrtdt = lv_next_date
      new_sdlstrttm = lv_start_time
      new_prddays   = 1
      new_periodic  = abap_true
    EXCEPTIONS
      OTHERS        = 1.

  IF sy-subrc = 0.
    WRITE: / 'Job', gc_job_name, 'reaktiviert für', lv_next_date, lv_start_time.
  ELSE.
    WRITE: / 'FEHLER: Job', gc_job_name, 'konnte nicht reaktiviert werden!'.
    WRITE: / 'ACHTUNG: Job muss manuell in SM37 freigegeben werden!'.
  ENDIF.

ENDFORM.


Verhalten



Situation
Verhalten




Testmodus (p_test = 'X')
Job wird NICHT deaktiviert/reaktiviert


Produktiv (p_test = ' ')
Job wird vor Start deaktiviert, nach Ende reaktiviert


Job nicht gefunden
Warnung, Programm läuft weiter


Programmabbruch
Job bleibt deaktiviert (gewünscht)


Reaktivierung
Täglich 01:00 Uhr, periodisch




Einbau-Anleitung

Globale Variablen nach DATA: gt_vknr_backup... einfügen
Methode execute ersetzen (2 neue PERFORM-Aufrufe)
Zwei FORMs am Ende einfügen (vor AT SELECTION-SCREEN)
Aktivieren und testen (erst mit p_test = 'X')
