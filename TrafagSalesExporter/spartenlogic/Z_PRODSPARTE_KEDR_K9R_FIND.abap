*&---------------------------------------------------------------------*
*& Report Z_PRODSPARTE_KEDR_K9R_FIND
*&---------------------------------------------------------------------*
*& Zweck: Technische KEDE/KEDR-Ableitungsregel finden.
*&
*& Hintergrund:
*&   Ableitungsregeln werden in generierten K9R*-Tabellen gespeichert.
*&   Die Feldnamen koennen generiert sein und muessen nicht PAPH1/WWPFA
*&   heissen. Deshalb sucht dieser Report:
*&     1) TKEDRS-Zeilen mit K9R-/Produkt-/PAPH-/WWPFA-Hinweisen
*&     2) alle DDIC-Tabellen K9R* mit Feldliste und Beispielwerten
*&
*& Bitte die Ausgabe der passenden K9R-Tabelle + Feldliste schicken.
*&---------------------------------------------------------------------*
REPORT z_prodsparte_kedr_k9r_find.

PARAMETERS: p_appl TYPE c LENGTH 2 DEFAULT 'KE',
            p_like TYPE dd02l-tabname DEFAULT 'K9R%',
            p_rows TYPE i DEFAULT 5.
PARAMETERS: p_all  TYPE abap_bool AS CHECKBOX.

FIELD-SYMBOLS: <ls_any> TYPE any,
               <lt_any> TYPE STANDARD TABLE,
               <lv_any> TYPE any.

START-OF-SELECTION.

  PERFORM show_tkedrs.
  SKIP 2.
  PERFORM show_k9r_tables.

FORM show_tkedrs.

  DATA lt_tkedrs TYPE STANDARD TABLE OF tkedrs.

  SELECT *
    FROM tkedrs
    INTO TABLE lt_tkedrs.

  WRITE: / '=== TKEDRS Diagnose ==='.
  WRITE: / 'Gelesene TKEDRS-Zeilen:', lines( lt_tkedrs ).
  ULINE.

  LOOP AT lt_tkedrs ASSIGNING <ls_any>.
    DATA(lv_skip) = abap_false.

    ASSIGN COMPONENT 'APPLCLASS' OF STRUCTURE <ls_any> TO <lv_any>.
    IF sy-subrc = 0 AND p_appl IS NOT INITIAL AND <lv_any> <> p_appl.
      lv_skip = abap_true.
    ENDIF.
    IF lv_skip = abap_true.
      CONTINUE.
    ENDIF.

    DATA(lv_interesting) = p_all.
    PERFORM row_contains USING <ls_any> 'K9R' CHANGING lv_interesting.
    PERFORM row_contains USING <ls_any> 'PAPH' CHANGING lv_interesting.
    PERFORM row_contains USING <ls_any> 'WWPFA' CHANGING lv_interesting.
    PERFORM row_contains USING <ls_any> 'PROD' CHANGING lv_interesting.
    PERFORM row_contains USING <ls_any> 'FAMIL' CHANGING lv_interesting.

    IF lv_interesting <> abap_true.
      CONTINUE.
    ENDIF.

    WRITE: / '--- TKEDRS Kandidat ---'.
    PERFORM print_non_initial_components USING <ls_any>.
    ULINE.
  ENDLOOP.

ENDFORM.

FORM show_k9r_tables.

  DATA: lt_dd02l TYPE STANDARD TABLE OF dd02l,
        lv_count TYPE i.

  SELECT *
    FROM dd02l
    INTO TABLE lt_dd02l
    WHERE tabname  LIKE p_like
      AND as4local = 'A'.

  SORT lt_dd02l BY tabname.

  WRITE: / '=== K9R Tabellen ==='.
  WRITE: / 'Gefundene Tabellen:', lines( lt_dd02l ).
  ULINE.

  LOOP AT lt_dd02l INTO DATA(ls_dd02l).
    CLEAR lv_count.
    TRY.
        SELECT COUNT( * )
          FROM (ls_dd02l-tabname)
          INTO lv_count.
      CATCH cx_sy_dynamic_osql_error.
        CONTINUE.
    ENDTRY.

    IF lv_count = 0 AND p_all <> abap_true.
      CONTINUE.
    ENDIF.

    WRITE: / '--- Tabelle:', ls_dd02l-tabname,
             'Klasse:', ls_dd02l-tabclass,
             'Zeilen:', lv_count.

    PERFORM show_fields USING ls_dd02l-tabname.
    PERFORM show_samples USING ls_dd02l-tabname p_rows.
    ULINE.
  ENDLOOP.

ENDFORM.

FORM show_fields USING iv_tab TYPE dd02l-tabname.

  DATA lt_dfies TYPE STANDARD TABLE OF dfies.

  CALL FUNCTION 'DDIF_FIELDINFO_GET'
    EXPORTING
      tabname   = iv_tab
    TABLES
      dfies_tab = lt_dfies
    EXCEPTIONS
      OTHERS    = 1.
  IF sy-subrc <> 0.
    WRITE: / 'Feldliste nicht lesbar.'.
    RETURN.
  ENDIF.

  WRITE: / 'Felder:'.
  LOOP AT lt_dfies INTO DATA(ls_f).
    WRITE: / ls_f-position,
             6 ls_f-fieldname,
             28 ls_f-datatype,
             38 ls_f-leng,
             48 ls_f-fieldtext.
  ENDLOOP.

ENDFORM.

FORM show_samples USING iv_tab  TYPE dd02l-tabname
                        iv_rows TYPE i.

  DATA lr_table TYPE REF TO data.

  CREATE DATA lr_table TYPE STANDARD TABLE OF (iv_tab).
  ASSIGN lr_table->* TO <lt_any>.
  IF sy-subrc <> 0.
    RETURN.
  ENDIF.

  TRY.
      SELECT *
        FROM (iv_tab)
        INTO TABLE <lt_any>
        UP TO iv_rows ROWS.
    CATCH cx_sy_dynamic_osql_error.
      WRITE: / 'Beispielzeilen nicht lesbar.'.
      RETURN.
  ENDTRY.

  WRITE: / 'Beispielzeilen:'.
  LOOP AT <lt_any> ASSIGNING <ls_any>.
    PERFORM print_non_initial_components USING <ls_any>.
    ULINE.
  ENDLOOP.

ENDFORM.

FORM row_contains USING    is_row     TYPE any
                           iv_pattern TYPE string
                  CHANGING cv_found   TYPE abap_bool.

  IF cv_found = abap_true.
    RETURN.
  ENDIF.

  DATA lo_desc TYPE REF TO cl_abap_typedescr.
  DATA lo_str  TYPE REF TO cl_abap_structdescr.

  lo_desc = cl_abap_typedescr=>describe_by_data( is_row ).
  lo_str ?= lo_desc.

  LOOP AT lo_str->components INTO DATA(ls_comp).
    ASSIGN COMPONENT ls_comp-name OF STRUCTURE is_row TO <lv_any>.
    IF sy-subrc <> 0 OR <lv_any> IS INITIAL.
      CONTINUE.
    ENDIF.

    DATA(lv_value) = |{ <lv_any> }|.
    TRANSLATE lv_value TO UPPER CASE.

    IF lv_value CS iv_pattern.
      cv_found = abap_true.
      RETURN.
    ENDIF.
  ENDLOOP.

ENDFORM.

FORM print_non_initial_components USING is_row TYPE any.

  DATA lo_desc TYPE REF TO cl_abap_typedescr.
  DATA lo_str  TYPE REF TO cl_abap_structdescr.

  lo_desc = cl_abap_typedescr=>describe_by_data( is_row ).
  lo_str ?= lo_desc.

  LOOP AT lo_str->components INTO DATA(ls_comp).
    ASSIGN COMPONENT ls_comp-name OF STRUCTURE is_row TO <lv_any>.
    IF sy-subrc <> 0 OR <lv_any> IS INITIAL.
      CONTINUE.
    ENDIF.

    WRITE: / ls_comp-name, 32 '=', 35 <lv_any>.
  ENDLOOP.

ENDFORM.
