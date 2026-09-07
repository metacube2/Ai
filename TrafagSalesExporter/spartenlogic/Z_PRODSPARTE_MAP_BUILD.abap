*&---------------------------------------------------------------------*
*& Report Z_PRODSPARTE_MAP_BUILD
*&---------------------------------------------------------------------*
*& Zweck: Baut ZPRODSPARTE_MAP direkt aus den KEDE/KEDR-Regeltabellen.
*&
*& Quelle:
*&   K9RT761000002  PAPH1 von-bis      -> Produktfamilie WWPFA
*&   K9RT761000003  Produktfamilie von-bis -> Produktsparte WWPSP
*&
*& Ergebnis:
*&   ZPRODSPARTE_MAP bleibt eine flache Einzelwert-Tabelle:
*&   PAPH1 -> WWPFA -> WWPSP
*&
*& Die Von-bis-Regeln werden vollstaendig in Einzel-PAPH1 expandiert,
*& damit ZPRODSPARTE_MAP mindestens alle KEDE-/Data(4)-Referenzcodes
*& enthaelt. Reale PAPH1-Codes aus MVKE/CE11000 werden optional
*& zusaetzlich aufgenommen, aendern aber nicht die Referenzabdeckung.
*&---------------------------------------------------------------------*
REPORT z_prodsparte_map_build.

PARAMETERS: p_vkorg TYPE vkorg,
            p_vtweg TYPE vtweg.
PARAMETERS: p_ce   TYPE abap_bool DEFAULT 'X' AS CHECKBOX.
PARAMETERS: p_test TYPE abap_bool DEFAULT 'X' AS CHECKBOX.

TYPES: BEGIN OF ty_code,
         paph1 TYPE zprodsparte_map-paph1,
       END OF ty_code.

TYPES: BEGIN OF ty_pfa_rule,
         sour1_from TYPE k9rt761000002-sour1_from,
         sour1_to   TYPE k9rt761000002-sour1_to,
         valid_from TYPE k9rt761000002-valid_from,
         target1    TYPE k9rt761000002-target1,
       END OF ty_pfa_rule.

TYPES: BEGIN OF ty_spa_rule,
         sour1_from TYPE k9rt761000003-sour1_from,
         sour1_to   TYPE k9rt761000003-sour1_to,
         valid_from TYPE k9rt761000003-valid_from,
         target1    TYPE k9rt761000003-target1,
       END OF ty_spa_rule.

TYPES tt_code TYPE STANDARD TABLE OF ty_code WITH DEFAULT KEY.
TYPES tt_map  TYPE STANDARD TABLE OF zprodsparte_map WITH DEFAULT KEY.

DATA: gt_code TYPE SORTED TABLE OF ty_code WITH UNIQUE KEY paph1,
      gt_pfa  TYPE STANDARD TABLE OF ty_pfa_rule WITH DEFAULT KEY,
      gt_spa  TYPE STANDARD TABLE OF ty_spa_rule WITH DEFAULT KEY.

DATA: gv_rule_expanded TYPE i,
      gv_real_added    TYPE i,
      gv_unsupported   TYPE i.

START-OF-SELECTION.

  PERFORM load_rules.
  PERFORM load_codes.
  PERFORM build_map.

FORM load_codes.

  DATA lt_prodh TYPE STANDARD TABLE OF mvke-prodh WITH DEFAULT KEY.
  DATA lt_ce_paph1 TYPE STANDARD TABLE OF ce11000-paph1 WITH DEFAULT KEY.

  LOOP AT gt_pfa INTO DATA(ls_pfa_rule).
    PERFORM add_paph1_range USING ls_pfa_rule-sour1_from
                                  ls_pfa_rule-sour1_to.
  ENDLOOP.

  IF p_vkorg IS INITIAL AND p_vtweg IS INITIAL.
    SELECT DISTINCT prodh
      FROM mvke
      INTO TABLE @lt_prodh
      WHERE prodh <> @space.
  ELSEIF p_vkorg IS NOT INITIAL AND p_vtweg IS INITIAL.
    SELECT DISTINCT prodh
      FROM mvke
      INTO TABLE @lt_prodh
      WHERE vkorg = @p_vkorg
        AND prodh <> @space.
  ELSEIF p_vkorg IS INITIAL AND p_vtweg IS NOT INITIAL.
    SELECT DISTINCT prodh
      FROM mvke
      INTO TABLE @lt_prodh
      WHERE vtweg = @p_vtweg
        AND prodh <> @space.
  ELSE.
    SELECT DISTINCT prodh
      FROM mvke
      INTO TABLE @lt_prodh
      WHERE vkorg = @p_vkorg
        AND vtweg = @p_vtweg
        AND prodh <> @space.
  ENDIF.

  LOOP AT lt_prodh INTO DATA(lv_prodh).
    INSERT VALUE ty_code( paph1 = lv_prodh(5) ) INTO TABLE gt_code.
    gv_real_added = gv_real_added + 1.
  ENDLOOP.

  IF p_ce = abap_true.
    SELECT DISTINCT paph1
      FROM ce11000
      INTO TABLE @lt_ce_paph1
      WHERE paph1 <> @space.

    LOOP AT lt_ce_paph1 INTO DATA(lv_paph1).
      INSERT VALUE ty_code( paph1 = lv_paph1 ) INTO TABLE gt_code.
      gv_real_added = gv_real_added + 1.
    ENDLOOP.
  ENDIF.

  WRITE: / 'PAPH1-Codes aus KEDE-Expansion :', gv_rule_expanded.
  WRITE: / 'Zusaetzliche reale Code-Versuche:', gv_real_added.
  WRITE: / 'Nicht expandierbare Bereiche   :', gv_unsupported.
  WRITE: / 'PAPH1-Codes gesamt eindeutig   :', lines( gt_code ).

  IF gt_code IS INITIAL.
    MESSAGE 'Keine PAPH1-Codes gefunden.' TYPE 'E'.
  ENDIF.

ENDFORM.

FORM add_paph1_range USING iv_from TYPE k9rt761000002-sour1_from
                           iv_to   TYPE k9rt761000002-sour1_to.

  DATA: lv_from TYPE string,
        lv_to   TYPE string.

  lv_from = iv_from.
  lv_to   = iv_to.
  CONDENSE: lv_from, lv_to.

  IF lv_from IS INITIAL.
    gv_unsupported = gv_unsupported + 1.
    RETURN.
  ENDIF.

  IF lv_to IS INITIAL.
    lv_to = lv_from.
  ENDIF.

  IF lv_from CO '0123456789'
  AND lv_to   CO '0123456789'.
    DATA(lv_from_i) = CONV i( lv_from ).
    DATA(lv_to_i)   = CONV i( lv_to ).
    DATA(lv_width)  = strlen( lv_from ).

    IF lv_to_i < lv_from_i.
      gv_unsupported = gv_unsupported + 1.
      RETURN.
    ENDIF.

    DATA(lv_count) = lv_to_i - lv_from_i + 1.

    DO lv_count TIMES.
      DATA(lv_num) = lv_from_i + sy-index - 1.
      DATA(lv_paph1_num) =
        |{ lv_num ALIGN = RIGHT PAD = '0' WIDTH = lv_width }|.
      INSERT VALUE ty_code( paph1 = lv_paph1_num ) INTO TABLE gt_code.
      gv_rule_expanded = gv_rule_expanded + 1.
    ENDDO.
    RETURN.
  ENDIF.

  IF strlen( lv_from ) = 4
  AND strlen( lv_to ) = 4
  AND lv_from(2) = lv_to(2).
    DATA(lv_alpha) = `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ`.
    DATA(lv_alpha_len) = strlen( lv_alpha ).
    DATA(lv_prefix) = lv_from(2).

    DO lv_alpha_len TIMES.
      DATA(lv_off3) = sy-index - 1.
      DATA(lv_c3) = lv_alpha+lv_off3(1).

      DO lv_alpha_len TIMES.
        DATA(lv_off4) = sy-index - 1.
        DATA(lv_c4) = lv_alpha+lv_off4(1).
        DATA(lv_paph1_alpha) = |{ lv_prefix }{ lv_c3 }{ lv_c4 }|.

        IF lv_paph1_alpha >= lv_from
        AND lv_paph1_alpha <= lv_to.
          INSERT VALUE ty_code( paph1 = lv_paph1_alpha ) INTO TABLE gt_code.
          gv_rule_expanded = gv_rule_expanded + 1.
        ENDIF.
      ENDDO.
    ENDDO.
    RETURN.
  ENDIF.

  gv_unsupported = gv_unsupported + 1.

ENDFORM.

FORM load_rules.

  SELECT sour1_from, sour1_to, valid_from, target1
    FROM k9rt761000002
    INTO TABLE @gt_pfa
    WHERE sour1_from <> @space
      AND target1    <> @space
      AND delete_flg <> 'X'.

  SELECT sour1_from, sour1_to, valid_from, target1
    FROM k9rt761000003
    INTO TABLE @gt_spa
    WHERE sour1_from <> @space
      AND target1    <> @space
      AND delete_flg <> 'X'.

  SORT gt_pfa BY valid_from DESCENDING sour1_from sour1_to.
  SORT gt_spa BY valid_from DESCENDING sour1_from sour1_to.

  WRITE: / 'KEDR-Regeln PAPH1->WWPFA     :', lines( gt_pfa ).
  WRITE: / 'KEDR-Regeln WWPFA->WWPSP     :', lines( gt_spa ).
  ULINE.

  IF gt_pfa IS INITIAL.
    MESSAGE 'Keine KEDE-Regeln in K9RT761000002 gefunden.' TYPE 'E'.
  ENDIF.

  IF gt_spa IS INITIAL.
    MESSAGE 'Keine KEDE-Regeln in K9RT761000003 gefunden.' TYPE 'E'.
  ENDIF.

ENDFORM.

FORM build_map.

  DATA: lt_insert TYPE tt_map,
        lt_no_pfa TYPE tt_code,
        lt_no_spa TYPE tt_code.

  LOOP AT gt_code INTO DATA(ls_code).
    DATA: lv_wwpfa       TYPE zprodsparte_map-wwpfa,
          lv_wwpsp       TYPE zprodsparte_map-wwpsp,
          lv_found_pfa   TYPE abap_bool,
          lv_found_spa   TYPE abap_bool,
          lv_best_date   TYPE dats,
          lv_best_exact  TYPE abap_bool.

    CLEAR: lv_wwpfa,
           lv_wwpsp,
           lv_found_pfa,
           lv_found_spa,
           lv_best_date,
           lv_best_exact.

    LOOP AT gt_pfa INTO DATA(ls_pfa).
      DATA(lv_pfa_to) = ls_pfa-sour1_to.
      IF lv_pfa_to IS INITIAL.
        lv_pfa_to = ls_pfa-sour1_from.
      ENDIF.

      IF ls_code-paph1 < ls_pfa-sour1_from
      OR ls_code-paph1 > lv_pfa_to.
        CONTINUE.
      ENDIF.

      DATA(lv_exact_pfa) = abap_false.
      IF ls_code-paph1 = ls_pfa-sour1_from
      AND ls_code-paph1 = lv_pfa_to.
        lv_exact_pfa = abap_true.
      ENDIF.

      IF lv_found_pfa = abap_false
      OR ls_pfa-valid_from > lv_best_date
      OR ( ls_pfa-valid_from = lv_best_date
           AND lv_best_exact = abap_false
           AND lv_exact_pfa  = abap_true ).
        lv_found_pfa  = abap_true.
        lv_best_date  = ls_pfa-valid_from.
        lv_best_exact = lv_exact_pfa.
        lv_wwpfa      = ls_pfa-target1.
      ENDIF.
    ENDLOOP.

    IF lv_found_pfa = abap_false.
      APPEND ls_code TO lt_no_pfa.
      CONTINUE.
    ENDIF.

    CLEAR: lv_best_date, lv_best_exact.

    LOOP AT gt_spa INTO DATA(ls_spa).
      DATA(lv_spa_to) = ls_spa-sour1_to.
      IF lv_spa_to IS INITIAL.
        lv_spa_to = ls_spa-sour1_from.
      ENDIF.

      IF lv_wwpfa < ls_spa-sour1_from
      OR lv_wwpfa > lv_spa_to.
        CONTINUE.
      ENDIF.

      DATA(lv_exact_spa) = abap_false.
      IF lv_wwpfa = ls_spa-sour1_from
      AND lv_wwpfa = lv_spa_to.
        lv_exact_spa = abap_true.
      ENDIF.

      IF lv_found_spa = abap_false
      OR ls_spa-valid_from > lv_best_date
      OR ( ls_spa-valid_from = lv_best_date
           AND lv_best_exact = abap_false
           AND lv_exact_spa  = abap_true ).
        lv_found_spa  = abap_true.
        lv_best_date  = ls_spa-valid_from.
        lv_best_exact = lv_exact_spa.
        lv_wwpsp      = ls_spa-target1.
      ENDIF.
    ENDLOOP.

    IF lv_found_spa = abap_false.
      APPEND ls_code TO lt_no_spa.
    ENDIF.

    APPEND VALUE zprodsparte_map(
      paph1  = ls_code-paph1
      wwpfa  = lv_wwpfa
      wwpsp  = lv_wwpsp
      crdate = sy-datum
      cruser = sy-uname ) TO lt_insert.
  ENDLOOP.

  WRITE: / 'Saetze fuer ZPRODSPARTE_MAP   :', lines( lt_insert ).
  WRITE: / 'PAPH1 ohne Produktfamilie     :', lines( lt_no_pfa ).
  WRITE: / 'PAPH1 ohne Produktsparte      :', lines( lt_no_spa ).
  ULINE.

  PERFORM show_preview USING lt_insert lt_no_pfa lt_no_spa.

  IF lt_insert IS INITIAL.
    WRITE: / 'Keine Saetze zum Schreiben. Tabelle bleibt unveraendert.'.
    RETURN.
  ENDIF.

  IF p_test = abap_true.
    WRITE: / 'TESTLAUF - keine DB-Aenderung.'.
    RETURN.
  ENDIF.

  DELETE FROM zprodsparte_map.
  INSERT zprodsparte_map FROM TABLE lt_insert.

  IF sy-subrc = 0.
    COMMIT WORK.
    WRITE: / lines( lt_insert ), 'Saetze in ZPRODSPARTE_MAP geschrieben.'.
  ELSE.
    ROLLBACK WORK.
    WRITE: / 'Fehler beim Schreiben, sy-subrc=', sy-subrc.
  ENDIF.

ENDFORM.

FORM show_preview USING it_insert TYPE tt_map
                        it_no_pfa TYPE tt_code
                        it_no_spa TYPE tt_code.

  FIELD-SYMBOLS: <ls_insert> TYPE zprodsparte_map,
                 <ls_code>   TYPE ty_code.

  WRITE: / '=== Vorschau Mapping max. 30 ==='.
  WRITE: / 'PAPH1', 10 'WWPFA', 20 'WWPSP'.
  ULINE.

  DATA lv_i TYPE i.
  LOOP AT it_insert ASSIGNING <ls_insert>.
    lv_i = lv_i + 1.
    IF lv_i > 30.
      EXIT.
    ENDIF.
    WRITE: / <ls_insert>-paph1,
             10 <ls_insert>-wwpfa,
             20 <ls_insert>-wwpsp.
  ENDLOOP.

  IF it_no_pfa IS NOT INITIAL.
    ULINE.
    WRITE: / '=== Erste PAPH1 ohne Produktfamilie ==='.
    CLEAR lv_i.
    LOOP AT it_no_pfa ASSIGNING <ls_code>.
      lv_i = lv_i + 1.
      IF lv_i > 30.
        EXIT.
      ENDIF.
      WRITE: / <ls_code>-paph1.
    ENDLOOP.
  ENDIF.

  IF it_no_spa IS NOT INITIAL.
    ULINE.
    WRITE: / '=== Erste PAPH1 ohne Produktsparte ==='.
    CLEAR lv_i.
    LOOP AT it_no_spa ASSIGNING <ls_code>.
      lv_i = lv_i + 1.
      IF lv_i > 30.
        EXIT.
      ENDIF.
      WRITE: / <ls_code>-paph1.
    ENDLOOP.
  ENDIF.

  ULINE.

ENDFORM.
