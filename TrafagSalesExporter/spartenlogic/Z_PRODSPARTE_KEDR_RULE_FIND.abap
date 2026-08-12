*&---------------------------------------------------------------------*
*& Report Z_PRODSPARTE_KEDR_RULE_FIND
*&---------------------------------------------------------------------*
*& Zweck: Findet die technische Ablage der KEDE/KEDR-Regel
*&        "Produktfamilie aus Produkthierarchie 1".
*&
*& Hintergrund:
*&   In KEDE/KEDR ist fachlich PAPH1 von-bis -> WWPFA gepflegt.
*&   Der Name der generierten Regeltabelle ist systemabhaengig.
*&   Dieses Diagnoseprogramm sucht DDIC-Tabellen mit PAPH1/WWPFA
*&   und zeigt Beispielzeilen inkl. moeglicher BIS-/Datumsfelder.
*&---------------------------------------------------------------------*
REPORT z_prodsparte_kedr_rule_find.

PARAMETERS: p_max TYPE i DEFAULT 20.

TYPES: BEGIN OF ty_tab,
         tabname TYPE dd03l-tabname,
       END OF ty_tab.

TYPES: BEGIN OF ty_sample_exact,
         paph1 TYPE ce11000-paph1,
         wwpfa TYPE ce11000-wwpfa,
       END OF ty_sample_exact.

TYPES: BEGIN OF ty_sample_range,
         paph1     TYPE ce11000-paph1,
         paph1_bis TYPE ce11000-paph1,
         wwpfa     TYPE ce11000-wwpfa,
       END OF ty_sample_range.

START-OF-SELECTION.

  DATA: lt_paph1 TYPE STANDARD TABLE OF ty_tab,
        lt_wwpfa TYPE STANDARD TABLE OF ty_tab,
        lt_tabs  TYPE STANDARD TABLE OF ty_tab.

  SELECT DISTINCT tabname
    FROM dd03l
    INTO TABLE @lt_paph1
    WHERE fieldname = 'PAPH1'
      AND as4local  = 'A'.

  SELECT DISTINCT tabname
    FROM dd03l
    INTO TABLE @lt_wwpfa
    WHERE fieldname = 'WWPFA'
      AND as4local  = 'A'.

  SORT lt_paph1 BY tabname.
  SORT lt_wwpfa BY tabname.

  LOOP AT lt_paph1 INTO DATA(ls_paph1).
    READ TABLE lt_wwpfa TRANSPORTING NO FIELDS
         WITH KEY tabname = ls_paph1-tabname BINARY SEARCH.
    IF sy-subrc = 0.
      APPEND ls_paph1 TO lt_tabs.
    ENDIF.
  ENDLOOP.

  SORT lt_tabs BY tabname.
  DELETE ADJACENT DUPLICATES FROM lt_tabs COMPARING tabname.

  WRITE: / 'Tabellen mit Feldern PAPH1 und WWPFA:', lines( lt_tabs ).
  ULINE.

  LOOP AT lt_tabs INTO DATA(ls_tab).
    PERFORM show_candidate USING ls_tab-tabname p_max.
  ENDLOOP.

FORM show_candidate USING iv_tab TYPE dd03l-tabname
                          iv_max TYPE i.

  DATA: lt_dfies TYPE STANDARD TABLE OF dfies,
        lv_count TYPE i,
        lv_has_bis TYPE abap_bool.

  CALL FUNCTION 'DDIF_FIELDINFO_GET'
    EXPORTING
      tabname   = iv_tab
    TABLES
      dfies_tab = lt_dfies
    EXCEPTIONS
      OTHERS    = 1.
  IF sy-subrc <> 0.
    RETURN.
  ENDIF.

  READ TABLE lt_dfies TRANSPORTING NO FIELDS
       WITH KEY fieldname = 'PAPH1_BIS'.
  IF sy-subrc = 0.
    lv_has_bis = abap_true.
  ENDIF.

  TRY.
      SELECT COUNT( * )
        FROM (iv_tab)
        INTO @lv_count
        WHERE paph1 <> @space
          AND wwpfa <> @space.
    CATCH cx_sy_dynamic_osql_error.
      RETURN.
  ENDTRY.

  IF lv_count = 0.
    RETURN.
  ENDIF.

  WRITE: / '--- Kandidat:', iv_tab, 'Saetze mit PAPH1/WWPFA:', lv_count.
  WRITE: / 'Relevante Felder:'.
  LOOP AT lt_dfies INTO DATA(ls_f)
       WHERE fieldname CS 'PAPH'
          OR fieldname CS 'WWPFA'
          OR fieldname CS 'BIS'
          OR fieldname CS 'LOW'
          OR fieldname CS 'HIGH'
          OR fieldname CS 'VON'
          OR fieldname CS 'DAT'
          OR fieldname CS 'DEL'
          OR fieldname CS 'LOE'.
    WRITE: / ls_f-fieldname, 18 ls_f-datatype, 28 ls_f-leng, 36 ls_f-fieldtext.
  ENDLOOP.
  ULINE.

  IF lv_has_bis = abap_true.
    DATA lt_range TYPE STANDARD TABLE OF ty_sample_range.
    TRY.
        SELECT paph1, paph1_bis, wwpfa
          FROM (iv_tab)
          INTO TABLE @lt_range
          UP TO @iv_max ROWS
          WHERE paph1 <> @space
            AND wwpfa <> @space.
        WRITE: / 'Beispiele PAPH1 / PAPH1_BIS / WWPFA:'.
        LOOP AT lt_range INTO DATA(ls_range).
          WRITE: / ls_range-paph1, 12 ls_range-paph1_bis, 24 ls_range-wwpfa.
        ENDLOOP.
      CATCH cx_sy_dynamic_osql_error.
        WRITE: / 'Beispiele mit PAPH1_BIS konnten nicht gelesen werden.'.
    ENDTRY.
  ELSE.
    DATA lt_exact TYPE STANDARD TABLE OF ty_sample_exact.
    TRY.
        SELECT paph1, wwpfa
          FROM (iv_tab)
          INTO TABLE @lt_exact
          UP TO @iv_max ROWS
          WHERE paph1 <> @space
            AND wwpfa <> @space.
        WRITE: / 'Beispiele PAPH1 / WWPFA:'.
        LOOP AT lt_exact INTO DATA(ls_exact).
          WRITE: / ls_exact-paph1, 12 ls_exact-wwpfa.
        ENDLOOP.
      CATCH cx_sy_dynamic_osql_error.
        WRITE: / 'Beispiele konnten nicht gelesen werden.'.
    ENDTRY.
  ENDIF.

  ULINE.

ENDFORM.
