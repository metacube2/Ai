*&---------------------------------------------------------------------*
*& Report Z_PRODSPARTE_MAP_BUILD
*&---------------------------------------------------------------------*
*& Zweck: Einmal-/periodischer Lauf. Zieht die eindeutige Zuordnung
*&        PAPH1 -> WWPFA (Produktfamilie) -> WWPSP (Produktsparte)
*&        aus den CO-PA-Einzelposten CE11000 und schreibt sie in
*&        ZPRODSPARTE_MAP.
*&
*& Quelle der Wahrheit: CO-PA-Belege, in denen KEDR bereits abgeleitet hat.
*& PAPH1 mit mehreren Familie/Sparte-Kombinationen wird protokolliert und
*& nicht geschrieben.
*&
*& Hinweis: Dieser Report ist bewusst fix fuer CE11000 geschrieben.
*& Falls der Ergebnisbereich variabel sein soll, muss die CE1xxxx-Tabelle
*& dynamisch aus ERKRS gebildet werden.
*&---------------------------------------------------------------------*
REPORT z_prodsparte_map_build.

PARAMETERS p_test TYPE abap_bool DEFAULT 'X' AS CHECKBOX.

TYPES: BEGIN OF ty_combo,
         paph1 TYPE ce11000-paph1,
         wwpfa TYPE ce11000-wwpfa,
         wwpsp TYPE ce11000-wwpsp,
       END OF ty_combo.

TYPES: BEGIN OF ty_map,
         paph1 TYPE ce11000-paph1,
         wwpfa TYPE ce11000-wwpfa,
         wwpsp TYPE ce11000-wwpsp,
         cnt   TYPE i,
       END OF ty_map.

DATA gt_combo TYPE SORTED TABLE OF ty_combo
                    WITH UNIQUE KEY paph1 wwpfa wwpsp.
DATA gt_map   TYPE STANDARD TABLE OF ty_map WITH DEFAULT KEY.
DATA gt_ambig TYPE STANDARD TABLE OF ty_combo WITH DEFAULT KEY.

START-OF-SELECTION.

  SELECT DISTINCT paph1, wwpfa, wwpsp
    FROM ce11000
    INTO TABLE @gt_combo
    WHERE paph1 <> @space
      AND wwpfa <> @space
      AND wwpsp <> @space.                         "#EC CI_NOFIELD

  IF gt_combo IS INITIAL.
    WRITE: / 'Keine Kombinationen in CE11000 gefunden. Abbruch.'.
    RETURN.
  ENDIF.

  LOOP AT gt_combo INTO DATA(ls_combo).
    READ TABLE gt_map INTO DATA(ls_map)
         WITH KEY paph1 = ls_combo-paph1.
    IF sy-subrc <> 0.
      ls_map = VALUE ty_map(
        paph1 = ls_combo-paph1
        wwpfa = ls_combo-wwpfa
        wwpsp = ls_combo-wwpsp
        cnt   = 1 ).
      APPEND ls_map TO gt_map.
    ELSEIF ls_map-wwpfa <> ls_combo-wwpfa
        OR ls_map-wwpsp <> ls_combo-wwpsp.
      ls_map-cnt = ls_map-cnt + 1.
      MODIFY gt_map FROM ls_map TRANSPORTING cnt WHERE paph1 = ls_combo-paph1.
      APPEND ls_combo TO gt_ambig.
    ENDIF.
  ENDLOOP.

  IF gt_ambig IS NOT INITIAL.
    WRITE: / '=== WARNUNG: mehrdeutige PAPH1 (mehrere Familie/Sparte) ==='.
    WRITE: / 'Diese werden NICHT in die Mapping-Tabelle geschrieben:'.
    ULINE.
    LOOP AT gt_ambig INTO ls_combo.
      WRITE: / ls_combo-paph1, 12 ls_combo-wwpfa, 22 ls_combo-wwpsp.
    ENDLOOP.
    ULINE.
  ENDIF.

  DATA lt_insert TYPE STANDARD TABLE OF zprodsparte_map WITH DEFAULT KEY.

  LOOP AT gt_map INTO ls_map WHERE cnt = 1.
    APPEND VALUE zprodsparte_map(
      paph1  = ls_map-paph1
      wwpfa  = ls_map-wwpfa
      wwpsp  = ls_map-wwpsp
      crdate = sy-datum
      cruser = sy-uname ) TO lt_insert.
  ENDLOOP.

  WRITE: / '=== Eindeutige Zuordnungen ==='.
  WRITE: / 'PAPH1', 12 'Familie', 22 'Sparte'.
  ULINE.
  LOOP AT lt_insert INTO DATA(ls_insert).
    WRITE: / ls_insert-paph1, 12 ls_insert-wwpfa, 22 ls_insert-wwpsp.
  ENDLOOP.
  ULINE.
  WRITE: / 'Eindeutige Saetze :', lines( lt_insert ).
  WRITE: / 'Mehrdeutige PAPH1 :', lines( gt_ambig ).

  IF lt_insert IS INITIAL.
    WRITE: / 'Keine eindeutigen Saetze, Tabelle wird nicht geloescht.'.
    RETURN.
  ENDIF.

  IF p_test = abap_true.
    WRITE: / 'TESTLAUF - keine DB-Aenderung. Haken entfernen zum Schreiben.'.
    RETURN.
  ENDIF.

  DELETE FROM zprodsparte_map.                      "#EC CI_NOWHERE
  INSERT zprodsparte_map FROM TABLE lt_insert.
  IF sy-subrc = 0.
    COMMIT WORK.
    WRITE: / lines( lt_insert ), 'Saetze in ZPRODSPARTE_MAP geschrieben.'.
  ELSE.
    ROLLBACK WORK.
    WRITE: / 'Fehler beim Schreiben, sy-subrc=', sy-subrc.
  ENDIF.
