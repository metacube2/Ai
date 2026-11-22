*&---------------------------------------------------------------------*
*& Report Z_PRODSPARTE_MAP_IMPORT
*&---------------------------------------------------------------------*
*& Zweck: Importiert die in KEDE/KEDR gepflegten Regeln
*&        PAPH1 von-bis -> WWPFA aus einem Regel-Export und expandiert
*&        Bereiche in Einzelwerte.
*&
*& Ergebnis in ZPRODSPARTE_MAP:
*&   PAPH1 -> WWPFA -> WWPSP
*&
*& Keine Tabellenerweiterung fuer PAPH1_BIS/GUELTAB erforderlich.
*& Die Von-bis-Information wird nur beim Import verarbeitet.
*&---------------------------------------------------------------------*
REPORT z_prodsparte_map_import.

PARAMETERS: p_file TYPE string LOWER CASE
                   DEFAULT 'C:\temp\kede_regeln.csv' OBLIGATORY.
PARAMETERS: p_sep  TYPE c LENGTH 1 DEFAULT ';'.
PARAMETERS: p_test TYPE abap_bool DEFAULT 'X' AS CHECKBOX.

TYPES: BEGIN OF ty_rule,
         paph1   TYPE zprodsparte_map-paph1,
         wwpfa   TYPE zprodsparte_map-wwpfa,
         gueltab TYPE dats,
       END OF ty_rule.

TYPES: BEGIN OF ty_fs,
         wwpfa TYPE ce11000-wwpfa,
         wwpsp TYPE ce11000-wwpsp,
         cnt   TYPE i,
       END OF ty_fs.

START-OF-SELECTION.

  DATA lt_raw TYPE STANDARD TABLE OF string.

  cl_gui_frontend_services=>gui_upload(
    EXPORTING
      filename = p_file
      filetype = 'ASC'
    CHANGING
      data_tab = lt_raw
    EXCEPTIONS
      OTHERS   = 1 ).
  IF sy-subrc <> 0.
    MESSAGE |Datei { p_file } nicht lesbar, sy-subrc={ sy-subrc }| TYPE 'E'.
  ENDIF.

  DATA: lt_rule       TYPE STANDARD TABLE OF ty_rule,
        lt_fields     TYPE STANDARD TABLE OF string,
        lv_from       TYPE string,
        lv_to         TYPE string,
        lv_date       TYPE string,
        lv_del        TYPE string,
        lv_wwpfa      TYPE string,
        lv_gueltab    TYPE dats,
        lv_skipped    TYPE i,
        lv_expanded   TYPE i,
        lv_unsupported TYPE i.

  LOOP AT lt_raw INTO DATA(lv_line).
    CLEAR: lt_fields, lv_from, lv_to, lv_date, lv_del, lv_wwpfa, lv_gueltab.
    SPLIT lv_line AT p_sep INTO TABLE lt_fields.

    IF lines( lt_fields ) < 7.
      lv_skipped = lv_skipped + 1.
      CONTINUE.
    ENDIF.

    READ TABLE lt_fields INTO lv_from  INDEX 1.
    READ TABLE lt_fields INTO lv_to    INDEX 3.
    READ TABLE lt_fields INTO lv_date  INDEX 5.
    READ TABLE lt_fields INTO lv_del   INDEX 6.
    READ TABLE lt_fields INTO lv_wwpfa INDEX 7.

    CONDENSE: lv_from, lv_to, lv_date, lv_del, lv_wwpfa.

    IF lv_from IS INITIAL
    OR lv_wwpfa IS INITIAL
    OR lv_from CS 'ProdHierarchie'
    OR lv_from CS 'Seite'.
      lv_skipped = lv_skipped + 1.
      CONTINUE.
    ENDIF.

    IF lv_del = 'X' OR lv_del = 'x'.
      lv_skipped = lv_skipped + 1.
      CONTINUE.
    ENDIF.

    IF strlen( lv_date ) = 10
    AND lv_date+2(1) = '.'
    AND lv_date+5(1) = '.'.
      lv_gueltab = |{ lv_date+6(4) }{ lv_date+3(2) }{ lv_date(2) }|.
    ENDIF.

    IF lv_wwpfa CO '0123456789'
    AND strlen( lv_wwpfa ) < 4.
      lv_wwpfa = |{ lv_wwpfa ALIGN = RIGHT PAD = '0' WIDTH = 4 }|.
    ENDIF.

    IF lv_to IS INITIAL.
      APPEND VALUE ty_rule(
        paph1   = lv_from
        wwpfa   = lv_wwpfa
        gueltab = lv_gueltab ) TO lt_rule.
      CONTINUE.
    ENDIF.

    IF lv_from CO '0123456789'
    AND lv_to CO '0123456789'.
      DATA(lv_from_i) = CONV i( lv_from ).
      DATA(lv_to_i)   = CONV i( lv_to ).
      DATA(lv_width)  = strlen( lv_from ).

      IF lv_to_i < lv_from_i.
        lv_unsupported = lv_unsupported + 1.
        CONTINUE.
      ENDIF.

      DATA(lv_count) = lv_to_i - lv_from_i + 1.

      DO lv_count TIMES.
        DATA(lv_num) = lv_from_i + sy-index - 1.
        DATA(lv_paph1_num) =
          |{ lv_num ALIGN = RIGHT PAD = '0' WIDTH = lv_width }|.
        APPEND VALUE ty_rule(
          paph1   = lv_paph1_num
          wwpfa   = lv_wwpfa
          gueltab = lv_gueltab ) TO lt_rule.
        lv_expanded = lv_expanded + 1.
      ENDDO.
      CONTINUE.
    ENDIF.

    " Bekannter alphanumerischer Finance-Bereich: z.B. 09B0 bis 09M4.
    IF strlen( lv_from ) = 4
    AND strlen( lv_to ) = 4
    AND lv_from(2) = lv_to(2).
      DATA(lv_alpha) = `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ`.
      DATA(lv_prefix) = lv_from(2).

      DO strlen( lv_alpha ) TIMES.
        DATA(lv_off3) = sy-index - 1.
        DATA(lv_c3) = lv_alpha+lv_off3(1).

        DO strlen( lv_alpha ) TIMES.
          DATA(lv_off4) = sy-index - 1.
          DATA(lv_c4) = lv_alpha+lv_off4(1).
          DATA(lv_paph1_alpha) = |{ lv_prefix }{ lv_c3 }{ lv_c4 }|.

          IF lv_paph1_alpha >= lv_from
          AND lv_paph1_alpha <= lv_to.
            APPEND VALUE ty_rule(
              paph1   = lv_paph1_alpha
              wwpfa   = lv_wwpfa
              gueltab = lv_gueltab ) TO lt_rule.
            lv_expanded = lv_expanded + 1.
          ENDIF.
        ENDDO.
      ENDDO.
    ELSE.
      lv_unsupported = lv_unsupported + 1.
    ENDIF.
  ENDLOOP.

  SORT lt_rule BY paph1 gueltab DESCENDING.
  DELETE ADJACENT DUPLICATES FROM lt_rule COMPARING paph1.

  WRITE: / 'CSV-Zeilen gelesen          :', lines( lt_raw ).
  WRITE: / 'Einzelwerte nach Expansion  :', lines( lt_rule ).
  WRITE: / 'Expandierte Bereichswerte   :', lv_expanded.
  WRITE: / 'Uebersprungene Zeilen       :', lv_skipped.
  WRITE: / 'Nicht expandierbare Bereiche:', lv_unsupported.
  ULINE.

  IF lt_rule IS INITIAL.
    MESSAGE 'Keine gueltigen Regeln gefunden.' TYPE 'E'.
  ENDIF.

  DATA: lt_fs TYPE STANDARD TABLE OF ty_fs,
        ls_fs TYPE ty_fs.

  SELECT DISTINCT wwpfa, wwpsp
    FROM ce11000
    INTO TABLE @DATA(lt_combo)
    WHERE wwpfa <> @space
      AND wwpsp <> @space.

  LOOP AT lt_combo INTO DATA(ls_combo).
    READ TABLE lt_fs INTO ls_fs WITH KEY wwpfa = ls_combo-wwpfa.
    IF sy-subrc <> 0.
      ls_fs-wwpfa = ls_combo-wwpfa.
      ls_fs-wwpsp = ls_combo-wwpsp.
      ls_fs-cnt   = 1.
      APPEND ls_fs TO lt_fs.
    ELSEIF ls_fs-wwpsp <> ls_combo-wwpsp.
      ls_fs-cnt = ls_fs-cnt + 1.
      MODIFY lt_fs FROM ls_fs TRANSPORTING cnt
             WHERE wwpfa = ls_combo-wwpfa.
    ENDIF.
  ENDLOOP.

  LOOP AT lt_fs INTO ls_fs WHERE cnt > 1.
    WRITE: / 'WARNUNG: Familie', ls_fs-wwpfa,
             'hat mehrere Sparten in CE11000 - WWPSP bleibt leer.'.
  ENDLOOP.
  DELETE lt_fs WHERE cnt > 1.
  SORT lt_fs BY wwpfa.

  DATA: lt_insert TYPE STANDARD TABLE OF zprodsparte_map,
        lv_no_fs  TYPE i.

  LOOP AT lt_rule INTO DATA(ls_rule).
    READ TABLE lt_fs INTO ls_fs
         WITH KEY wwpfa = ls_rule-wwpfa BINARY SEARCH.

    DATA(ls_insert) = VALUE zprodsparte_map(
      paph1  = ls_rule-paph1
      wwpfa  = ls_rule-wwpfa
      crdate = sy-datum
      cruser = sy-uname ).

    IF sy-subrc = 0.
      ls_insert-wwpsp = ls_fs-wwpsp.
    ELSE.
      lv_no_fs = lv_no_fs + 1.
    ENDIF.

    APPEND ls_insert TO lt_insert.
  ENDLOOP.

  WRITE: / 'Familien ohne eindeutige Sparte:', lv_no_fs.
  ULINE.
  WRITE: / '=== Vorschau max. 30 ==='.
  WRITE: / 'PAPH1', 10 'WWPFA', 20 'WWPSP'.
  ULINE.

  DATA lv_i TYPE i.
  LOOP AT lt_insert INTO DATA(ls_preview).
    lv_i = lv_i + 1.
    IF lv_i > 30.
      EXIT.
    ENDIF.
    WRITE: / ls_preview-paph1,
             10 ls_preview-wwpfa,
             20 ls_preview-wwpsp.
  ENDLOOP.
  ULINE.

  IF p_test = abap_true.
    WRITE: / 'TESTLAUF - keine DB-Aenderung.'.
    RETURN.
  ENDIF.

  DELETE FROM zprodsparte_map.
  INSERT zprodsparte_map FROM TABLE lt_insert.

  IF sy-subrc = 0.
    COMMIT WORK.
    WRITE: / lines( lt_insert ), 'Einzelwerte in ZPRODSPARTE_MAP geschrieben.'.
  ELSE.
    ROLLBACK WORK.
    WRITE: / 'Fehler beim Schreiben, sy-subrc=', sy-subrc.
  ENDIF.
