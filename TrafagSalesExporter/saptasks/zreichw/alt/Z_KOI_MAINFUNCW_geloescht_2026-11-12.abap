*&---------------------------------------------------------------------*
*& Include          Z_KOI_MAINFUNCW
*&---------------------------------------------------------------------*
*&---------------------------------------------------------------------*

*& Include Z_INCLUDE_KOI_MAINFUNC
*&---------------------------------------------------------------------*
CLASS zz_cl_koi_mainfunc DEFINITION FINAL.
  PUBLIC SECTION.
    " Type-Definitionen
    TYPES: BEGIN OF ty_matnr,
             matnr TYPE matnr,
             mstae TYPE mstae,
           END OF ty_matnr,

           BEGIN OF ty_used_in,
             matnr TYPE matnr,
             stlnr TYPE mast-stlnr,
             mstae TYPE mstae,
           END OF ty_used_in,

           BEGIN OF ty_bom,
             matnr TYPE matnr,
             idnrk TYPE idnrk,
             menge TYPE c LENGTH 2,
           END OF ty_bom,

           BEGIN OF ty_material,
             matnr TYPE matnr,
             mstae TYPE mstae,
           END OF ty_material,

           ty_spool_data TYPE c LENGTH 20000,
           tt_spool_data TYPE STANDARD TABLE OF ty_spool_data WITH DEFAULT KEY,
           slis_t_fieldcat_al TYPE slis_fieldcat_alv,
           ty_all_material TYPE TABLE OF ty_material,
           ty_bom_tab TYPE STANDARD TABLE OF ty_bom WITH DEFAULT KEY,

           BEGIN OF ty_abc_result,
             matnr      TYPE matnr,
             abc_klasse TYPE maabc,
           END OF ty_abc_result,

           tt_abc_result TYPE STANDARD TABLE OF ty_abc_result WITH KEY matnr.

    " Öffentliche Datendeklarationen
    DATA: lt_materials     TYPE TABLE OF zlagcleanup,
          lt_all_materials TYPE TABLE OF ty_material,
          gt_spool_data    TYPE TABLE OF ty_spool_data,
          gs_bdcdata       TYPE bdcdata,
          lt_bom           TYPE TABLE OF ty_bom,
          gt_bdcdata       TYPE TABLE OF bdcdata,
          gt_messtab       TYPE TABLE OF bdcmsgcoll,
          gs_messtab       TYPE bdcmsgcoll.

    " Methoden
    METHODS:
      constructor,

      z_download_and_delete_spool
        IMPORTING
          iv_data_type   TYPE c
          lt_all_materia TYPE ty_all_material,

      set_materials
        IMPORTING
          it_materials TYPE ty_all_material,

      calculate_month_difference
        IMPORTING
          iv_date_from TYPE datum
          iv_date_to   TYPE datum
        RETURNING
          VALUE(rv_months) TYPE i,

      read_table_for_ls_magic
        IMPORTING
          lt_fields      TYPE STANDARD TABLE
          iv_table_index TYPE i
          iv_append      TYPE abap_bool,

      bdc_dynpro
        IMPORTING
          a TYPE string
          b TYPE string,

      bdc_field
        IMPORTING
          a TYPE string
          b TYPE string,

      execute_bdc
        IMPORTING
          p_tcode TYPE tcode,

      set_column_names
        IMPORTING
          po_columns  TYPE REF TO cl_salv_columns_table
          pt_fieldcat TYPE slis_t_fieldcat_alv,

      run_batch_input_mb51
        IMPORTING
          t_materials LIKE lt_all_materials
        CHANGING
          ct_bom LIKE lt_bom,

      run_batch_input_me2m
        IMPORTING
          t_materials LIKE lt_all_materials
        CHANGING
          ct_bom LIKE lt_bom,

      run_batch_input_mb5b
        IMPORTING
          t_materials LIKE lt_all_materials
          p_dlow      TYPE datum OPTIONAL
          p_dhigh     TYPE datum OPTIONAL
        CHANGING
          ct_bom LIKE lt_bom,

      run_batch_input_mb5l
        IMPORTING
          t_materials LIKE lt_all_materials
        CHANGING
          ct_bom LIKE lt_bom,

      run_batch_input_me3m
        IMPORTING
          t_materials LIKE lt_all_materials
        CHANGING
          ct_bom LIKE lt_bom,

      bdc_transaction
        IMPORTING
          t_code TYPE c,

      download_spool_to_table
        IMPORTING
          iv_spoolno   TYPE tsp01-rqident
          iv_data_type TYPE c
          iv_filename  TYPE rlgrap-filename
        CHANGING
          nomove TYPE i,

      run_batch_input_mc46
        IMPORTING
          t_materials LIKE lt_all_materials
        CHANGING
          ct_bom LIKE lt_bom,

      get_abc_classification
        IMPORTING
          it_materials    TYPE ty_all_material
          iv_werks        TYPE werks_d
          iv_date_from    TYPE datum OPTIONAL
          iv_date_to      TYPE datum OPTIONAL
        RETURNING
          VALUE(rt_result) TYPE tt_abc_result
        RAISING
          cx_static_check.

  PRIVATE SECTION.
    DATA: mt_all_materials TYPE ty_all_material,
          mt_bom           TYPE ty_bom_tab,
          lt_materials_s   TYPE TABLE OF ty_material.

ENDCLASS.


CLASS zz_cl_koi_mainfunc IMPLEMENTATION.
  METHOD constructor.
    " Initialisierung, falls nötig
  ENDMETHOD.
METHOD calculate_month_difference.
  " Die Parameter sind bereits in der Definition deklariert!
  DATA: lv_year_diff  TYPE i,
        lv_month_diff TYPE i.

  " Jahre und Monate berechnen
  lv_year_diff = iv_date_to(4) - iv_date_from(4).
  lv_month_diff = iv_date_to+4(2) - iv_date_from+4(2).

  " Gesamtmonate
  rv_months = ( lv_year_diff * 12 ) + lv_month_diff.

  " Tag berücksichtigen (wenn To-Tag < From-Tag, dann -1 Monat)
  IF iv_date_to+6(2) < iv_date_from+6(2).
    rv_months = rv_months - 1.
  ENDIF.

  " Mindestens 1 Monat
  IF rv_months <= 0.
    rv_months = 1.
  ENDIF.
ENDMETHOD.





  METHOD read_table_for_ls_magic.



    DATA: ls_zlagcleanup    TYPE zlagcleanup,
          lv_IDcounter      TYPE i,
          "lv_feld1_char     TYPE zlagcleanup-feld1,
          lt_temp_fields    TYPE TABLE OF string,
          lv_contains_digit TYPE abap_bool,
          lv_digit          TYPE c LENGTH 1,
          p_fall            TYPE zlagcleanup-fall.
    FIELD-SYMBOLS: <wa_line> TYPE any.

    " Holen Sie den höchsten Wert aus feld1 der Tabelle ZLAGCLEANUP

    p_fall = iv_table_index.

    CONDENSE p_fall.
    DELETE FROM zlagcleanup
        WHERE fall EQ p_fall.
    COMMIT WORK.

    IF sy-subrc = 0.
      COMMIT WORK.
      WRITE: 'Datensätze erfolgreich gelöscht'.
    ELSE.
      ROLLBACK WORK.
      WRITE: 'Fehler beim Löschen der Datensätze'.
    ENDIF.


    SELECT MAX( feld1 ) INTO @DATA(lv_feld1_char)
      FROM zlagcleanup
    ."  WHERE fall = @p_fall.

    " Wenn kein Wert gefunden wurde, initialisiere mit '0'
    IF sy-subrc <> 0.
      lv_feld1_char = '0'.
    ENDIF.

    " Entferne führende Nullen für die numerische Konvertierung
    CONDENSE lv_feld1_char NO-GAPS.

    " Konvertiere zu numerischem Wert direkt in die vorhandene Variable
    lv_feld1_char = |{ CONV i( lv_feld1_char ) }|.


    " Konvertieren Sie den CHAR-Wert in einen Integer
    lv_IDcounter = CONV i( lv_feld1_char ).

    " Wenn kein Wert gefunden wurde oder die Konvertierung fehlschlägt, starten Sie bei 0
    IF sy-subrc <> 0 OR lv_IDcounter IS INITIAL.
      lv_IDcounter = 0.
    ENDIF.

    LOOP AT lt_fields ASSIGNING <wa_line>.
      CLEAR: lt_temp_fields, ls_zlagcleanup, lv_contains_digit.
      " Prüfen, ob <wa_line> den String "Fehler" enthält
      IF <wa_line> CS 'Fehler'.
        EXIT.  " Beendet die LOOP und somit die Methode
      ENDIF.
      SPLIT <wa_line> AT '|' INTO TABLE lt_temp_fields.
      CASE iv_table_index.
        WHEN 1.
          IF lines( lt_temp_fields ) < 7.
            CONTINUE.
          ENDIF.
        WHEN 2.
          IF lines( lt_temp_fields ) < 3.
            CONTINUE.
          ENDIF.
      ENDCASE.
      IF iv_table_index <> 12.
        READ TABLE lt_temp_fields INTO DATA(lv_correct_line_test) INDEX 2.
      ELSE.
        READ TABLE lt_temp_fields INTO  lv_correct_line_test  INDEX 3.
      ENDIF.
      CONDENSE lv_correct_line_test.

      " Prüfen, ob lv_correct_line_test mindestens eine Zahl enthält
      DO 10 TIMES.
        lv_digit = sy-index - 1.  " 0 bis 9
        IF lv_correct_line_test CS lv_digit.
          lv_contains_digit = abap_true.
          EXIT.
        ENDIF.
      ENDDO.

      " Nur fortfahren, wenn eine Zahl enthalten ist
      CHECK lv_contains_digit = abap_true.

      CASE iv_table_index.


        WHEN 6 OR 7 OR 8.
          lv_IDcounter = lv_IDcounter + 1.
          lv_feld1_char = lv_IDcounter.
          CONDENSE lv_feld1_char NO-GAPS.
          ls_zlagcleanup-feld1 = lv_feld1_char.

          READ TABLE lt_temp_fields INTO DATA(lv_line) INDEX 2.

          " Logik zum Aufteilen der Zeile
          DATA: lv_temp_line TYPE string,
                lt_parts     TYPE TABLE OF string.

          lv_temp_line = lv_line.

          " Teile die Zeile an Stellen mit mindestens 10 Leerzeichen
          SPLIT lv_temp_line AT '|' INTO TABLE lt_parts.

          " Entferne leere Einträge und führende/nachfolgende Leerzeichen
          DELETE lt_parts WHERE table_line IS INITIAL.
          LOOP AT lt_parts ASSIGNING FIELD-SYMBOL(<fs_part>).
            CONDENSE <fs_part>.
          ENDLOOP.

          " Zuweisen der Teile zu den Feldern

          " Zuerst alle Felder in lt_temp_fields bereinigen
          LOOP AT lt_temp_fields ASSIGNING FIELD-SYMBOL(<fs_temp_field>).
            CONDENSE <fs_temp_field> NO-GAPS.
          ENDLOOP.

          READ TABLE lt_temp_fields INDEX 2 INTO ls_zlagcleanup-feld2.
          READ TABLE lt_temp_fields INDEX 3 INTO ls_zlagcleanup-feld3.
          READ TABLE lt_temp_fields INDEX 4 ASSIGNING FIELD-SYMBOL(<fs_temp2>).
          IF sy-subrc = 0.
            CONDENSE <fs_temp2>.
            ls_zlagcleanup-feld4 = <fs_temp2>.
          ENDIF.

          READ TABLE lt_temp_fields INDEX 5 INTO ls_zlagcleanup-feld5.
          READ TABLE lt_temp_fields INDEX 6 INTO ls_zlagcleanup-feld6.
          READ TABLE lt_temp_fields INDEX 7 INTO ls_zlagcleanup-feld7.
          READ TABLE lt_temp_fields INDEX 8 INTO ls_zlagcleanup-feld8.
          READ TABLE lt_temp_fields INDEX 9 INTO ls_zlagcleanup-feld9.
          READ TABLE lt_temp_fields INDEX 10 INTO ls_zlagcleanup-feld10.
          READ TABLE lt_temp_fields INDEX 11 INTO ls_zlagcleanup-feld11.
          READ TABLE lt_temp_fields INDEX 12 INTO ls_zlagcleanup-feld12.
          READ TABLE lt_temp_fields INDEX 13 INTO ls_zlagcleanup-feld13.
          READ TABLE lt_temp_fields INDEX 14 INTO ls_zlagcleanup-feld14.
          READ TABLE lt_temp_fields INDEX 15 INTO ls_zlagcleanup-feld15.
          READ TABLE lt_temp_fields INDEX 16 INTO ls_zlagcleanup-feld16.

          READ TABLE lt_temp_fields INDEX 17 INTO ls_zlagcleanup-feld17.
          READ TABLE lt_temp_fields INDEX 18 INTO ls_zlagcleanup-feld18.
          "  READ TABLE lt_temp_fields INDEX 19 INTO ls_zlagcleanup-feld19.

        WHEN 9.
          READ TABLE lt_temp_fields INDEX 2 INTO ls_zlagcleanup-feld2.
          READ TABLE lt_temp_fields INDEX 3 INTO ls_zlagcleanup-feld3.


        WHEN 11    . "ME3M ME2M

          lv_IDcounter = lv_IDcounter + 1.
          lv_feld1_char = lv_IDcounter.
          CONDENSE lv_feld1_char NO-GAPS.
          ls_zlagcleanup-feld1 = lv_feld1_char.

          READ TABLE lt_temp_fields INDEX 2 INTO ls_zlagcleanup-feld2.
          READ TABLE lt_temp_fields INDEX 3 INTO ls_zlagcleanup-feld3.
          READ TABLE lt_temp_fields INDEX 4 ASSIGNING FIELD-SYMBOL(<fs_temp>).
          IF sy-subrc = 0.
            CONDENSE <fs_temp>.
            ls_zlagcleanup-feld4 = <fs_temp>.
          ENDIF.

          READ TABLE lt_temp_fields INDEX 5 INTO ls_zlagcleanup-feld5.
          READ TABLE lt_temp_fields INDEX 6 INTO ls_zlagcleanup-feld6.

        WHEN  12  . "ME3M ME2M

          lv_IDcounter = lv_IDcounter + 1.
          lv_feld1_char = lv_IDcounter.
          CONDENSE lv_feld1_char NO-GAPS.
          ls_zlagcleanup-feld1 = lv_feld1_char.

          READ TABLE lt_temp_fields INDEX 20 INTO ls_zlagcleanup-feld2. "Matnr
          READ TABLE lt_temp_fields INDEX 10 INTO ls_zlagcleanup-feld3.   " Offene Zielmenge
*          READ TABLE lt_temp_fields INDEX 4 ASSIGNING FIELD-SYMBOL(<fs_temp>).
*          IF sy-subrc = 0.
*            CONDENSE <fs_temp>.
*            ls_zlagcleanup-feld4 = <fs_temp>.
*          ENDIF.

          READ TABLE lt_temp_fields INDEX 5 INTO ls_zlagcleanup-feld5.
          READ TABLE lt_temp_fields INDEX 6 INTO ls_zlagcleanup-feld6.


        WHEN 13.



          lv_IDcounter = lv_IDcounter + 1.
          lv_feld1_char = lv_IDcounter.
          CONDENSE lv_feld1_char NO-GAPS.
          ls_zlagcleanup-feld1 = lv_feld1_char.

          READ TABLE lt_temp_fields INTO DATA(lv_line2) INDEX 2.

          " Logik zum Aufteilen der Zeile
          DATA: lv_part      TYPE string.

          lv_temp_line = lv_line2.

          " Teile die Zeile an Stellen mit mindestens 10 Leerzeichen
          SPLIT lv_temp_line AT '          ' INTO TABLE lt_parts.

          " Entferne leere Einträge und führende/nachfolgende Leerzeichen
          DELETE lt_parts WHERE table_line IS INITIAL.
          LOOP AT lt_parts ASSIGNING FIELD-SYMBOL(<fs_part2>).
            CONDENSE <fs_part2>.
          ENDLOOP.

          " Zuweisen der Teile zu den Feldern
          READ TABLE lt_parts INDEX 1 INTO ls_zlagcleanup-feld2.
          READ TABLE lt_parts INDEX 2 INTO ls_zlagcleanup-feld3.
          READ TABLE lt_parts INDEX 3 INTO ls_zlagcleanup-feld4.


        WHEN 6 OR  7 OR 8.

          " Entfernen von führenden und nachfolgenden Leerzeichen
      ENDCASE.
      " Entfernen von führenden und nachfolgenden Leerzeichen

      ls_zlagcleanup-fall = iv_table_index.
      CONDENSE  ls_zlagcleanup-fall.

      INSERT zlagcleanup FROM ls_zlagcleanup.


      IF sy-subrc <> 0.
        WRITE: / 'Fehler beim Aktualisieren/Einfügen in Tabelle ZLAGCLEANUP: ', sy-subrc.
      ENDIF.
    ENDLOOP.

    COMMIT WORK.
  ENDMETHOD.






METHOD get_abc_classification.
  DATA: lt_seltab     TYPE TABLE OF rsparams,
        ls_seltab     LIKE LINE OF lt_seltab,
        lt_listobject TYPE TABLE OF abaplist,
        lt_list_ascii TYPE TABLE OF soli,
        ls_result     LIKE LINE OF rt_result,
        lv_perioden   TYPE int1.  " Lokal berechnet!


  FIELD-SYMBOLS: <ls_material> LIKE LINE OF it_materials.


  " NEU: Perioden aus Datumsdifferenz berechnen
  IF iv_date_from IS NOT INITIAL AND iv_date_to IS NOT INITIAL.
    lv_perioden = me->calculate_month_difference(
                    iv_date_from = iv_date_from
                    iv_date_to   = iv_date_to ).
  ELSE.
    lv_perioden = 12.  " Default wenn keine Daten übergeben
  ENDIF.


  " Rest der Methode bleibt gleich...
  CLEAR lt_seltab.


  " Materialien übergeben
  LOOP AT it_materials ASSIGNING <ls_material>.
    CLEAR ls_seltab.
    ls_seltab-selname = 'S_MATNR'.
    ls_seltab-kind    = 'S'.
    ls_seltab-sign    = 'I'.
    ls_seltab-option  = 'EQ'.
    ls_seltab-low     = <ls_material>-matnr.
    APPEND ls_seltab TO lt_seltab.
  ENDLOOP.


  " Werk übergeben
  CLEAR ls_seltab.
  ls_seltab-selname = 'P_WERKS'.
  ls_seltab-kind    = 'P'.
  ls_seltab-sign    = 'I'.
  ls_seltab-low     = iv_werks.
  APPEND ls_seltab TO lt_seltab.


  " Analysezeitraum - jetzt mit berechneten Perioden!
  CLEAR ls_seltab.
  ls_seltab-selname = 'P_MONVRE'.
  ls_seltab-kind    = 'P'.
  ls_seltab-low     = lv_perioden.  " Berechneter Wert
  APPEND ls_seltab TO lt_seltab.



    " Weitere Parameter für Hintergrundlauf ohne Dialog/Update
    CLEAR ls_seltab.
    ls_seltab-selname = 'P_DISPLA'. " Kein Protokoll anzeigen (Display Mode)
    ls_seltab-kind    = 'P'.
    ls_seltab-low     = ' '.
    APPEND ls_seltab TO lt_seltab.

    CLEAR ls_seltab.
    ls_seltab-selname = 'P_UPDATE'. " Kein Update MARC-MAABC
    ls_seltab-kind    = 'P'.
    ls_seltab-low     = ' '.
    APPEND ls_seltab TO lt_seltab.

    " 2. Standardreport aufrufen
    SUBMIT rmcbab20 " Report für ABC-Analyse
           WITH SELECTION-TABLE lt_seltab
           EXPORTING LIST TO MEMORY
           AND RETURN.

    IF sy-subrc <> 0.
      " Fehler beim SUBMIT selbst
      MESSAGE e000(oo) WITH 'Fehler beim SUBMIT von RMCVA010'.
      RETURN.
    ENDIF.

    " 3. Ergebnis aus dem Speicher holen und parsen
    DATA: lr_data TYPE REF TO data.

    TRY.
        " Versuch mit modernem ALV
        CALL METHOD cl_salv_bs_runtime_info=>get_data_ref
          IMPORTING
            r_data = lr_data.

        IF lr_data IS BOUND.
          ASSIGN lr_data->* TO FIELD-SYMBOL(<lt_mc40_data>).
          IF <lt_mc40_data> IS ASSIGNED AND lines( <lt_mc40_data> ) > 0.
            " Datenstruktur prüfen und Mappen
            FIELD-SYMBOLS: <fs_mc40_line> TYPE any,
                           <fv_matnr>     TYPE matnr,
                           <fv_abc>       TYPE maabc.

            LOOP AT <lt_mc40_data> ASSIGNING <fs_mc40_line>.
              ASSIGN COMPONENT 'MATNR' OF STRUCTURE <fs_mc40_line> TO <fv_matnr>.
              ASSIGN COMPONENT 'ABCIN' OF STRUCTURE <fs_mc40_line> TO <fv_abc>. " Korrekter Feldname für ABC-Indikator

              IF sy-subrc = 0 AND <fv_matnr> IS ASSIGNED AND <fv_abc> IS ASSIGNED.
                CLEAR ls_result.
                ls_result-matnr      = <fv_matnr>.
                ls_result-abc_klasse = <fv_abc>.
                APPEND ls_result TO rt_result.
              ENDIF.
            ENDLOOP.
          ENDIF.
        ELSE.
          " Fallback: Klassische Liste parsen
          CALL FUNCTION 'LIST_FROM_MEMORY'
            TABLES
              listobject = lt_listobject
            EXCEPTIONS
              not_found  = 1
              OTHERS     = 2.

          IF sy-subrc = 0 AND lt_listobject IS NOT INITIAL.
            CALL FUNCTION 'LIST_TO_ASCI'
              TABLES
                listasci   = lt_list_ascii
                listobject = lt_listobject
              EXCEPTIONS
                OTHERS     = 1.

            IF sy-subrc = 0 AND lt_list_ascii IS NOT INITIAL.
              " Liste parsen
              DATA: lv_matnr TYPE matnr,
                    lv_abc   TYPE maabc,
                    lv_line2 TYPE string.

              LOOP AT lt_list_ascii INTO DATA(ls_list_ascii).
                lv_line2 = ls_list_ascii.

                " Prüfen, ob Zeile eine Materialzeile ist (nicht Header/Footer)
                IF lv_line2 IS INITIAL OR
                   lv_line2(1) = '-' OR
                   lv_line2(1) = '=' OR
                   lv_line2 CP '*Material*' OR
                   lv_line2 CP '*Gesamt*'.
                  CONTINUE.
                ENDIF.

                " Matnr und ABC-Klasse extrahieren
                " Annahme: Format der Liste ist [Material] [...] [ABC-Klasse]
                " Der genaue Offset muss angepasst werden basierend auf der tatsächlichen Ausgabe

                " Versuche Materialnummer zu extrahieren (normalerweise erste Spalte)
                DATA(lv_matnr_str) = lv_line2(18).
                CONDENSE lv_matnr_str NO-GAPS.

                " Versuche ABC-Klasse zu extrahieren (normalerweise in einer der rechten Spalten)
                DATA(lv_abc_pos) = find( val = lv_line2 sub = 'A' ) DIV 10 * 10.

                IF lv_abc_pos > 0.
                  DATA(lv_abc_str) = lv_line2+lv_abc_pos(1).

                  " Prüfen, ob es sich um gültige ABC-Klasse handelt
                  IF lv_abc_str CA 'ABC'.
                    CLEAR ls_result.
                    ls_result-matnr = lv_matnr_str.
                    ls_result-abc_klasse = lv_abc_str.
                    APPEND ls_result TO rt_result.
                  ENDIF.
                ENDIF.
              ENDLOOP.
            ENDIF.
          ENDIF.
        ENDIF.

        " Doppelte Einträge entfernen
        SORT rt_result BY matnr.
        DELETE ADJACENT DUPLICATES FROM rt_result COMPARING matnr.

        " Laufzeitinfo wieder freigeben
        cl_salv_bs_runtime_info=>clear_all( ).

      CATCH cx_root INTO DATA(lx_error).
        " Fehler beim Datenzugriff/-verarbeitung
        cl_salv_bs_runtime_info=>clear_all( ).
        MESSAGE e000(oo) WITH lx_error->get_text( ).
    ENDTRY.
  ENDMETHOD.

















  METHOD set_materials.
    mt_all_materials = it_materials.
  ENDMETHOD.

  METHOD bdc_dynpro.
    CLEAR gs_bdcdata.
    gs_bdcdata-program  = a.
    gs_bdcdata-dynpro   = b.
    gs_bdcdata-dynbegin = 'X'.
    APPEND gs_bdcdata TO gt_bdcdata.
  ENDMETHOD.

  METHOD bdc_field.
    CLEAR gs_bdcdata.
    gs_bdcdata-fnam = a.
    gs_bdcdata-fval = b.
    APPEND gs_bdcdata TO gt_bdcdata.
  ENDMETHOD.

  METHOD bdc_transaction.
    DATA: l_subrc           TYPE sy-subrc,
          lx_auth_check     TYPE REF TO cx_root,
          l_auth_check_text TYPE string.

    TRY.
        CALL TRANSACTION t_code WITH AUTHORITY-CHECK USING gt_bdcdata
                         MODE   'S'
                         UPDATE 'E'.
      CATCH cx_sy_authorization_error INTO lx_auth_check.
        l_auth_check_text = lx_auth_check->get_text( ).
        sy-subrc = 99.
    ENDTRY.
    l_subrc = sy-subrc.

    REFRESH gt_bdcdata.
  ENDMETHOD.

  METHOD z_download_and_delete_spool.
    DATA: lt_spool_list          TYPE TABLE OF tsp01,
          ls_spool               TYPE tsp01,
          lv_filename            TYPE rlgrap-filename,
          lv_spoolid             TYPE rspoid,
          lv_rc                  TYPE sy-subrc,
          ld_rc                  TYPE rspotype-rc,
          ld_status              TYPE sy-subrc,
          ld_error_message       TYPE rspoemsg,
          slv_spoolid            TYPE tsp01_sp0r-rqid_char,
          lv_error_message       TYPE string,
          nomove                 TYPE i,
          go                     TYPE i,
          lv_FileToDelete        TYPE eps2filnam,
          gv_global_filepath     TYPE string,
          gv_global_filename     TYPE string,
          gv_global_fullfilepath TYPE string,
          tmpsel                 TYPE string,
          gv_global_seperator    TYPE string.

    DATA: lv_max_id TYPE objid.
    lv_filename = gv_global_filename.

    DATA: ls_spool_list  TYPE tsp01,
          lv_max_rqident TYPE tsp01-rqident.

    SELECT *
      FROM tsp01
      INTO ls_spool_list
      WHERE rqowner = sy-uname
      ORDER BY rqident DESCENDING.
      EXIT.
    ENDSELECT.

    IF sy-subrc = 0.
      APPEND ls_spool_list TO lt_spool_list.
    ENDIF.

    IF lt_spool_list IS NOT INITIAL.
      LOOP AT lt_spool_list INTO ls_spool.
        me->download_spool_to_table(
          EXPORTING
            iv_spoolno   = ls_spool-rqident
            iv_data_type = iv_data_type
            iv_filename  = lv_filename
          CHANGING
            nomove       = nomove
        ).

        slv_Spoolid = ls_spool-rqident.

        TRY.
            CALL FUNCTION 'RSPO_R_RDELETE_SPOOLREQ'
              EXPORTING
                spoolid = slv_Spoolid.
          CATCH cx_root INTO DATA(lx_exception).
            lv_error_message = lx_exception->get_text( ).
            WRITE: / 'Fehler bei der Ausführung: ', lv_error_message.
        ENDTRY.

        IF sy-subrc <> 0.
          WRITE: / 'Fehler beim Löschen des Spoolauftrags:', ls_spool-rqident.
        ENDIF.
      ENDLOOP.
    ELSE.
      go = 1.
    ENDIF.
  ENDMETHOD.

  METHOD run_batch_input_mb51.
    DATA: ii          TYPE i,
          wa_material LIKE LINE OF lt_materials_s.  "Korrekte Deklaration als Struktur


    CLEAR gt_bdcdata.  "BDC-Daten für jedes Material neu initialisieren
    READ TABLE t_materials INTO wa_material INDEX 1.

    me->bdc_dynpro( a = 'RM07DOCS' b = '1000' ).
    me->bdc_field( a = 'BDC_CURSOR' b = 'MATNR-LOW' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=%001' ).
    me->bdc_field( a = 'MATNR-LOW'  b =  CONV string( wa_material-matnr ) ).
    me->bdc_field( a = 'WERKS-LOW'  b = '1100' ).
    me->bdc_field( a = 'RHIER_L'    b = 'X' ).
    me->bdc_field( a = 'ALV_DEF'    b = '/1100' ).
    me->bdc_field( a = 'DATABASE'   b = 'X' ).

    " Multiple Selection Screen
    me->bdc_dynpro( a = 'SAPLALDB' b = '3000' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=ACPT' ).
    me->bdc_field( a = 'BDC_CURSOR' b = 'RSCSEL_255-SLOW_I(02)' ).
    me->bdc_field( a = 'RSCSEL_255-SLOW_I(01)' b = CONV string( wa_material-matnr ) ).
    "    me->bdc_field( a = 'RSCSEL_255-SLOW_I(02)' b = '55986' ).
    "   me->bdc_field( a = 'RSCSEL_255-SLOW_I(03)' b = '55987' ).



    " Materialien dynamisch hinzufügen
    " Multiple Selection Screen

    " Materialien dynamisch hinzufügen
    DATA: lv_index TYPE i VALUE 1,
          lv_field TYPE string.
    lv_index = 2.
    LOOP AT t_materials INTO wa_material.
      " Feldnamen mit führender Null erstellen
      IF lv_index < 10.
        lv_field = 'RSCSEL_255-SLOW_I(0' && lv_index && ')'.
      ELSE.
        lv_field = 'RSCSEL_255-SLOW_I(' && lv_index && ')'.
      ENDIF.



      " Führende Nullen entfernen
      DATA lv_matnr    TYPE string.

      lv_matnr = CONV string( wa_material-matnr ).
      SHIFT lv_matnr LEFT DELETING LEADING '0'.

      " Material zur Mehrfachselektion hinzufügen
      me->bdc_field(
        a = lv_field
        b = CONV string( lv_matnr )
      ).

      lv_index = lv_index + 1.
    ENDLOOP.




*    " Empty fields for remaining selection entries
*    DO 6 TIMES.
*      DATA(lv_field) = |RSCSEL_255-SLOW_I({ sy-index + 2 PADDING = '0' LENGTH = 2 })|.
*      me->bdc_field( iv_fnam = lv_field iv_fval = '' ).
*    ENDDO.

    " Layout Selection
    me->bdc_dynpro( a = 'RM07DOCS' b = '1000' ).
    me->bdc_field( a = 'BDC_CURSOR' b = 'RFLAT_L' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=LITY' ).
    me->bdc_field( a = 'MATNR-LOW' b = CONV string( wa_material-matnr ) ).
    me->bdc_field( a = 'WERKS-LOW' b = '1100' ).
    me->bdc_field( a = 'RHIER_L'   b = '' ).
    me->bdc_field( a = 'RFLAT_L'   b = 'X' ).
    me->bdc_field( a = 'ALV_DEF'   b = '/1100' ).
    me->bdc_field( a = 'DATABASE'  b = 'X' ).

    " Execute Screea               b
    me->bdc_dynpro( a  = 'RM07DOCS' b  = '1000' ).
    me->bdc_field( a = 'BDC_CURSOR' b = 'RFLAT_L' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=ONLI' ).
    me->bdc_field( a = 'MATNR-LOW' b = CONV string( wa_material-matnr ) ).
    me->bdc_field( a = 'WERKS-LOW' b = '1100' ).
    me->bdc_field( a = 'RFLAT_L'   b = 'X' ).
    me->bdc_field( a = 'ALV_DEF'   b = '/GYS' ).
    me->bdc_field( a = 'DATABASE'  b = 'X' ).

    " System Screens
    me->bdc_dynpro(  a = 'SAPMSSY0'  b = '0120' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&OAD' ).

    me->bdc_dynpro( a  = 'SAPMSSY0' b = '0120' ).
    me->bdc_field( a = 'BDC_CURSOR' b = '06/09' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&IC1' ).

    me->bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&RNT' ).



    " Hier beginnt der neue Druckcode
    me->bdc_dynpro( a = 'SAPLSPRI' b = '0100' ).
    me->bdc_field( a = 'BDC_CURSOR' b = 'PRI_PARAMS-PDEST' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=PRIN' ).
    me->bdc_field( a = 'PRI_PARAMS-PDEST' b = 'TR03' ).
    me->bdc_field( a = 'PRI_PARAMS-PRCOP' b = '1' ).
    me->bdc_field( a = 'RADIO0500_1' b = 'X' ).
    me->bdc_field( a = 'BDC_SUBSCR' b = 'SAPLSPRI                                0600SUBSCREEN' ).

    me->bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&RNT' ).

    me->bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&F15' ).

    " Exit Screen
    me->bdc_dynpro( a = 'RM07DOCS' b = '1000' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '/EECAN' ).
    me->bdc_field( a = 'BDC_CURSOR' b = 'MATNR-LOW' ).

    me->execute_bdc( p_tcode = 'MB51' ).

  ENDMETHOD.


*
*  METHOD run_batch_input_me2m.
*      CLEAR: gt_bdcdata.
*" Startbildschirm
*    me->bdc_dynpro( a = 'RM06EM00' b = '1000' ).
*    me->bdc_field( a = 'BDC_CURSOR'    b = 'EM_MATNR-HIGH' ).
*    me->bdc_field( a = 'BDC_OKCODE'    b = '=ONLI' ).
*    me->bdc_field( a = 'EM_MATNR-LOW'  b = '' ).
*
*    me->bdc_field( a = 'EM_WERKS-LOW'  b = '1100' ).
*    me->bdc_field( a = 'EM_EKORG-LOW'  b = '1100' ).
*    me->bdc_field( a = 'LISTU'         b = 'BEST' ).
*
*    " ALV Liste
*    me->bdc_dynpro( a = 'SAPLSLVC_FULLSCREEN' b = '0500' ).
*    me->bdc_field( a = 'BDC_OKCODE' b = '=&OAD' ).
*
*    " Layout Konfiguration
*    me->bdc_dynpro( a = 'SAPLSALV_CUL_CONFIGURATION' b = '1500' ).
*    me->bdc_field( a = 'BDC_OKCODE' b = '=DTC_CONT' ).
*    me->bdc_field( a = 'BDC_SUBSCR' b = 'SAPLSALV_CUL_LAYOUT_CHOOSE              0500SUB_CONFIGURATION' ).
*    me->bdc_field( a = 'G51_SCREEN-USPEC_LBOX' b = 'A' ).
*
*    " Zurück zur ALV Liste
*    me->bdc_dynpro( a = 'SAPLSLVC_FULLSCREEN' b = '0500' ).
*    me->bdc_field( a = 'BDC_OKCODE' b = '=&RNT' ).
*
*    " Druckparameter
*    me->bdc_dynpro( a = 'SAPLSPRI' b = '0100' ).
*    me->bdc_field( a = 'BDC_CURSOR' b = 'PRI_PARAMS-PDEST' ).
*    me->bdc_field( a = 'BDC_OKCODE' b = '=PRIN' ).
*    me->bdc_field( a = 'PRI_PARAMS-PDEST' b = 'TR03' ).
*    me->bdc_field( a = 'PRI_PARAMS-PRCOP' b = '1' ).
*    me->bdc_field( a = 'RADIO0500_1' b = 'X' ).
*    me->bdc_field( a = 'BDC_SUBSCR' b = 'SAPLSPRI                                0600SUBSCREEN' ).
*
*    " Beenden
*    me->bdc_dynpro( a = 'SAPLSLVC_FULLSCREEN' b = '0500' ).
*    me->bdc_field( a = 'BDC_OKCODE' b = '=&F12' ).
*
*    " Transaktion ausführen
*    me->execute_bdc( p_tcode = 'ME2M' ).
*
*
*  ENDMETHOD.

  METHOD run_batch_input_me3m.
    " Hauptscreen


    " Hauptscreen
    me->bdc_dynpro( a = 'RM06EM00' b = '1000' ).
    me->bdc_field( a = 'BDC_CURSOR'    b = 'EM_MATNR-LOW' ).
    me->bdc_field( a = 'BDC_OKCODE'    b = '=ONLI' ).
    me->bdc_field( a = 'EM_WERKS-LOW'  b = '1100' ).
    me->bdc_field( a = 'EM_EKORG-LOW'  b = '1100' ).
    me->bdc_field( a = 'EM_MATNR-LOW'  b = '1' ).
    me->bdc_field( a = 'EM_MATNR-HIGH' b = 'ZE00823' ).
    me->bdc_field( a = 'LISTU'         b = 'RAHM' ).

    " ALV Screen
    me->bdc_dynpro( a = 'SAPLSLVC_FULLSCREEN' b = '0500' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&RNT' ).

    " Druckparameter
    me->bdc_dynpro( a = 'SAPLSPRI' b = '0100' ).
    me->bdc_field( a = 'BDC_CURSOR'     b = 'PRI_PARAMS-PDEST' ).
    me->bdc_field( a = 'BDC_OKCODE'     b = '=PRIN' ).
    me->bdc_field( a = 'PRI_PARAMS-PDEST' b = 'TR03' ).
    me->bdc_field( a = 'PRI_PARAMS-PRCOP' b = '1' ).
    me->bdc_field( a = 'RADIO0500_1'    b = 'X' ).
    me->bdc_field( a = 'BDC_SUBSCR'     b = 'SAPLSPRI                                0600SUBSCREEN' ).

    " Zurück zum ALV und Beenden
    me->bdc_dynpro( a = 'SAPLSLVC_FULLSCREEN' b = '0500' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&F12' ).

    " Transaktion ausführen
    me->execute_bdc( p_tcode = 'ME3M' ).


  ENDMETHOD.

  METHOD run_batch_input_me2m.
    CLEAR: gt_bdcdata.


    " Hauptscreen - erste Eingabe
    me->bdc_dynpro( a = 'RM06EM00' b = '1000' ).
    me->bdc_field( a = 'BDC_CURSOR'    b = 'EM_MATNR-LOW' ).
    me->bdc_field( a = 'BDC_OKCODE'    b = '=ONLI' ).        "Matchcode für Einzelauswahl
    me->bdc_field( a = 'EM_MATNR-LOW'  b = '1' ).
    me->bdc_field( a = 'EM_MATNR-HIGH' b = 'ZE00823' ).
    me->bdc_field( a = 'EM_WERKS-LOW'  b = '1100' ).
    me->bdc_field( a = 'EM_EKORG-LOW'  b = '1100' ).
    me->bdc_field( a = 'LISTU'         b = 'BEST' ).




    " ALV Screen
    me->bdc_dynpro( a = 'SAPLSLVC_FULLSCREEN' b = '0500' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&RNT' ).

    " Druckparameter
    me->bdc_dynpro( a = 'SAPLSPRI' b = '0100' ).
    me->bdc_field( a = 'BDC_CURSOR'     b = 'PRI_PARAMS-PDEST' ).
    me->bdc_field( a = 'BDC_OKCODE'     b = '=PRIN' ).
    me->bdc_field( a = 'PRI_PARAMS-PDEST' b = 'TR03' ).
    me->bdc_field( a = 'PRI_PARAMS-PRCOP' b = '1' ).
    me->bdc_field( a = 'RADIO0500_1'    b = 'X' ).

    " Zurück zum ALV und Beenden
    me->bdc_dynpro( a = 'SAPLSLVC_FULLSCREEN' b = '0500' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&F12' ).

    " Transaktion ausführen
    me->execute_bdc( p_tcode = 'ME2M' ).

  ENDMETHOD.




  " ACHTUNG --> Wenn ALV Feld zu lang ist beim dfrucken wird nichts gedruckt und spool bleibt leer !!!!!!

  METHOD run_batch_input_mb5b.
    " Öffnen der Gruppe
    CLEAR: gt_bdcdata.
    me->bdc_dynpro( a = 'RM07MLBD' b = '1000' ).
    .
    bdc_field( a = 'BDC_OKCODE' b = '/EGET' ).
    bdc_field( a = 'BDC_CURSOR' b = 'MATNR-LOW' ).
    bdc_field( a = 'MATNR-LOW'     b = '1' ).
    bdc_field( a = 'MATNR-HIGH'     b = 'ZE00823' ).
    bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
    bdc_field( a = 'BDC_CURSOR' b = '04/05' ).
    bdc_field( a = 'BDC_OKCODE' b = '=PICK' ).

    bdc_dynpro( a = 'RM07MLBD' b = '1000' ).
    bdc_field( a = 'BDC_CURSOR' b = 'MATNR-LOW' ).
    bdc_field( a = 'BDC_OKCODE' b = '=ONLI' ).
    bdc_field( a = 'MATNR-LOW'     b = '1' ).
    bdc_field( a = 'MATNR-HIGH'     b = 'ZE00823' ).
    bdc_field( a = 'BUKRS-LOW' b = '1100' ).
    bdc_field( a = 'WERKS-LOW' b = '1100' ).


    IF p_dlow IS NOT INITIAL AND p_dhigh IS NOT INITIAL.
      me->bdc_field( a = 'DATUM-LOW'  b = |{ p_dlow DATE = USER }| ).
      me->bdc_field( a = 'DATUM-HIGH' b = |{ p_dhigh DATE = USER }| ).
    ELSE.
      DATA: lv_date_low_calc TYPE dats.

      " Berechne Datum "Heute vor einem Jahr"
      lv_date_low_calc = sy-datum.
      lv_date_low_calc(4) = lv_date_low_calc(4) - 1. " Jahr um 1 reduzieren



      bdc_field( a = 'DATUM-LOW' b = |{ p_dlow DATE = USER }| ).
      bdc_field( a = 'DATUM-HIGH' b = |{ p_dhigh DATE = USER }| ).

*      bdc_field( a = 'DATUM-LOW' b = |{ lv_date_low_calc DATE = USER }| ).
*      bdc_field( a = 'DATUM-HIGH' b = |{ sy-datum DATE = USER }| ).
    ENDIF.
    bdc_field( a = 'BWBST' b = 'X' ).
    bdc_field( a = 'PA_SUMFL' b = 'X' ).

    me->bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&OAD' ).


    me->bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
    bdc_field( a = 'BDC_CURSOR' b = '04/08' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&IC1' ).

    me->bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&RNT' ).

    me->bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=&F12' ).

    bdc_dynpro( a = 'RM07MLBD' b = '1000' ).
    bdc_field( a = 'BDC_OKCODE' b = '/EECAN' ).
    bdc_field( a = 'BDC_CURSOR' b = 'MATNR-LOW' ).

    " Transaktion ausführen
    me->execute_bdc( p_tcode = 'MB5B' ).
  ENDMETHOD.




  METHOD run_batch_input_mb5L.
*    " Öffnen der Gruppe
*    me->bdc_dynpro( a = 'RM07MLBD' b = '1000' ).
*    bdc_field( a = 'BDC_OKCODE' b = '/EGET' ).
*    bdc_field( a = 'BDC_CURSOR' b = 'MATNR-LOW' ).
*
*    bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
*    bdc_field( a = 'BDC_CURSOR' b = '04/05' ).
*    bdc_field( a = 'BDC_OKCODE' b = '=PICK' ).
*
*    bdc_dynpro( a = 'RM07MLBD' b = '1000' ).
*    bdc_field( a = 'BDC_CURSOR' b = 'MATNR-LOW' ).
*    bdc_field( a = 'BDC_OKCODE' b = '=ONLI' ).
*    bdc_field( a = 'BUKRS-LOW' b = '1100' ).
*    bdc_field( a = 'WERKS-LOW' b = '1100' ).
*    bdc_field( a = 'DATUM-LOW' b = '01.06.2024' ).
*    bdc_field( a = 'DATUM-HIGH' b = '02.06.2024' ).
*    bdc_field( a = 'BWBST' b = 'X' ).
*    bdc_field( a = 'PA_SUMFL' b = 'X' ).
*
*    bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
*    bdc_field( a = 'BDC_OKCODE' b = '=&RNT' ).
*
*    bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
*    bdc_field( a = 'BDC_OKCODE' b = '=&F12' ).
*
*    bdc_dynpro( a = 'RM07MLBD' b = '1000' ).
*    bdc_field( a = 'BDC_OKCODE' b = '/EECAN' ).
*    bdc_field( a = 'BDC_CURSOR' b = 'MATNR-LOW' ).
*
*    " Transaktion ausführen
*    me->execute_bdc( p_tcode = 'MB5B' ).
  ENDMETHOD.

  METHOD run_batch_input_mc46.
    DATA: ii          TYPE i,
          wa_material LIKE  lt_materials_s.

    CLEAR gt_bdcdata.
    me->bdc_dynpro( a = 'RMCBLH30' b = '1000' ).
    me->bdc_field( a = 'BDC_CURSOR' b = 'ANZTAGE' ).
    me->bdc_field( a = 'BDC_OKCODE' b = 'ONLI' ).
    me->bdc_field( a = 'ANZTAGE'      b = '365' ).
    me->bdc_field( a = 'MATERIAL-LOW' b = '' ).
    me->bdc_field( a = 'NULLBSTD' b = 'X' ).
    me->bdc_field( a = 'FLGDISPO' b = 'X' ).
    me->bdc_field( a = 'FLG_WERT' b = 'X' ).
    me->bdc_field( a = 'WAHL0' b = 'X' ).
    me->bdc_field( a = 'LHVON' b = '90' ).
    me->bdc_field( a = 'AMMIN' b = '5' ).
    me->bdc_field( a = 'AMMAX' b = '5' ).
    me->bdc_field( a = 'VKORG-LOW' b = '1100' ).

    me->bdc_dynpro( a = 'SAPMSSY0' b = '0120' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=%PRI' ).

    me->bdc_dynpro( a = 'SAPLSPRI' b = '0100' ).
    me->bdc_field( a  = 'BDC_CURSOR' b = 'PRI_PARAMS-PDEST' ).
    me->bdc_field( a = 'BDC_OKCODE' b = '=PRIN' ).
    me->bdc_field( a = 'PRI_PARAMS-PDEST' b = 'TR03' ).
    me->bdc_field( a = 'PRI_PARAMS-PRCOP' b = '1' ).
    me->bdc_field( a = 'RADIO0500_1' b = 'X' ).
    me->bdc_field( a = 'BDC_SUBSCR' b = 'SAPLSPRI 0600SUBSCREEN' ).

    me->bdc_dynpro( a = 'SAPMSSY0'     b    = '0120' ).
    me->bdc_field( a    = 'BDC_OKCODE' b  =  '=RW' ).

    me->bdc_dynpro( a   = 'RMCBLH30'     b = '1000' ).
    me->bdc_field( a     = 'BDC_OKCODE' b  = '/EECAN' ).
    me->bdc_field( a  = 'BDC_CURSOR'    b    = 'VKORG-LOW' ).

    me->execute_bdc( p_tcode = 'MC46' ).
  ENDMETHOD.





  METHOD execute_bdc.

    DATA: lv_mode TYPE c LENGTH 1.

    lv_mode = 'N'. " N = Hintergrundverarbeitung ohne Bildschirmanzeige

    CALL TRANSACTION p_tcode USING gt_bdcdata
                     MODE 'E'"lv_mode'
                     UPDATE 'S'
                     MESSAGES INTO gt_messtab.
  ENDMETHOD.

  METHOD set_column_names.
    DATA: lo_column TYPE REF TO cl_salv_column.

    LOOP AT pt_fieldcat ASSIGNING FIELD-SYMBOL(<fs_fieldcat>).
      TRY.
          lo_column = po_columns->get_column( <fs_fieldcat>-fieldname ).
          lo_column->set_short_text( <fs_fieldcat>-seltext_s ).
          lo_column->set_medium_text( <fs_fieldcat>-seltext_m ).
          lo_column->set_long_text( <fs_fieldcat>-seltext_l ).
        CATCH cx_salv_not_found.
          " Fehlerbehandlung
      ENDTRY.
    ENDLOOP.
  ENDMETHOD.

  METHOD download_spool_to_table.
    DATA: lv_line     TYPE c LENGTH 20000,
          x           TYPE tdline,
          lv_filename TYPE string,
          lv_fullpath TYPE string,
          lv_counter  TYPE int8,
          lv_material TYPE matnr.

    TYPES: tt_stringtab TYPE STANDARD TABLE OF string WITH DEFAULT KEY.
    TYPES: BEGIN OF ty_result,
             matnr       TYPE matnr,
             komponente  TYPE string,
             materialien TYPE tt_stringtab,
           END OF ty_result,
           tt_result TYPE STANDARD TABLE OF ty_result WITH DEFAULT KEY.

    DATA: lt_temp_table TYPE TABLE OF ty_spool_data,
          lt_result     TYPE TABLE OF ty_result,
          wa_temp_table TYPE ty_spool_data.

    DATA: lt_all_materials TYPE TABLE OF ty_material.

    " Spoolauftrag lesen
    CALL FUNCTION 'RSPO_RETURN_ABAP_SPOOLJOB'
      EXPORTING
        rqident   = iv_spoolno
      TABLES
        buffer    = gt_spool_data
      EXCEPTIONS
        not_found = 1
        OTHERS    = 2.

    IF sy-subrc <> 0.
      WRITE: / 'Fehler beim Lesen des Spoolauftrags', iv_spoolno.
      RETURN.
    ENDIF.

    IF iv_data_type = 'MB51'.

      me->read_table_for_ls_magic(
       EXPORTING
        lt_fields   = gt_spool_data
         iv_table_index = 6 " Verwendngszweck der Tabellenzeile
         iv_append = abap_true ) .
    ENDIF.


    IF iv_data_type = 'MB5L'.

      me->read_table_for_ls_magic(
       EXPORTING
        lt_fields   = gt_spool_data
         iv_table_index = 7 " Verwendngszweck der Tabellenzeile
         iv_append = abap_true ) .
    ENDIF.
    IF iv_data_type = 'MC46'.

      me->read_table_for_ls_magic(
       EXPORTING
        lt_fields   = gt_spool_data
         iv_table_index = 13 " Verwendngszweck der Tabellenzeile
         iv_append = abap_true ) .
    ENDIF.

    IF iv_data_type = 'MB5B'.

      me->read_table_for_ls_magic(
       EXPORTING
        lt_fields   = gt_spool_data
         iv_table_index = 8 " Verwendngszweck der Tabellenzeile
         iv_append = abap_true ) .
    ENDIF.


    IF iv_data_type = 'ME2M'.

      me->read_table_for_ls_magic(
       EXPORTING
        lt_fields   = gt_spool_data
         iv_table_index = 11 " Verwendngszweck der Tabellenzeile
         iv_append = abap_true ) .
    ENDIF.

    IF iv_data_type = 'ME3M'.

      me->read_table_for_ls_magic(
       EXPORTING
        lt_fields   = gt_spool_data
         iv_table_index = 12 " Verwendngszweck der Tabellenzeile
         iv_append = abap_true ) .
    ENDIF.

    COMMIT WORK.

    CLEAR gt_spool_data.
  ENDMETHOD.

ENDCLASS.

FORM parse_curr USING    iv_str TYPE string
                CHANGING cv_val TYPE p.
  DATA: lv_tmp TYPE p LENGTH 16 DECIMALS 2.

  PERFORM parse_quan USING iv_str CHANGING lv_tmp.
  cv_val = lv_tmp.
ENDFORM.
FORM parse_quan USING    iv_str TYPE string
                CHANGING cv_val TYPE p.
  DATA: lv TYPE string,
        lv_minus TYPE abap_bool,
        lv_result TYPE p LENGTH 16 DECIMALS 3.

  CLEAR cv_val.
  lv = iv_str.

  " Bereinigung
  CONDENSE lv.

  IF lv IS INITIAL OR lv = '0' OR lv = '0.00' OR lv = '0,00'.
    cv_val = 0.
    RETURN.
  ENDIF.

  " Minus-Zeichen
  IF lv CA '-'.
    REPLACE ALL OCCURRENCES OF '-' IN lv WITH ''.
    lv_minus = abap_true.
  ENDIF.

  " ✅ VEREINFACHT für Schweizer Format
  REPLACE ALL OCCURRENCES OF `'` IN lv WITH ''.  " Tausendertrenner entfernen
  REPLACE ALL OCCURRENCES OF ',' IN lv WITH '.'. " Komma -> Punkt (falls vorhanden)

  " Konvertierung
  TRY.
      lv_result = lv.
      cv_val = lv_result.
    CATCH cx_root.
      cv_val = 0.
  ENDTRY.

  IF lv_minus = abap_true.
    cv_val = - cv_val.
  ENDIF.
ENDFORM.



* FORM Routine für den Texteditor
FORM call_zz_langtext_editor.
  DATA: lv_textname TYPE tdobname.

  " Angepasst an die Bedarfs-/Bestandsliste
  CONCATENATE 'ZBESTAND_' sy-datum '_' sy-uzeit INTO lv_textname.

  CALL FUNCTION 'EDIT_TEXT'
    EXPORTING
      display_mode   = 'U'
      text_name      = lv_textname
      text_type      = 'Z1'
    EXCEPTIONS
      text_not_found = 1
      OTHERS         = 2.

  IF sy-subrc <> 0.
    MESSAGE 'Text konnte nicht bearbeitet werden' TYPE 'I'.
  ENDIF.
ENDFORM.
