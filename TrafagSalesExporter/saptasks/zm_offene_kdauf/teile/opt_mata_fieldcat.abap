
  " Materialanteil 2026-10-15: zwei Spalten direkt hinter STANDARDWERT, mit Hauswaehrung des Werks
  " (Bewertungskreis = Werk -> T001K-BUKRS -> T001-WAERS; Werk 1100 = CHF). Gespeicherte Layouts
  " kennen die Spalten nicht und blenden sie aus; dort muessen sie einmal eingeblendet werden.
  DATA: lv_mata_waers TYPE t001-waers,
        lv_mata_idx   TYPE sy-tabix,
        lt_mata_fc    LIKE e01_lt_fieldcat.

  SELECT SINGLE t001~waers FROM t001k INNER JOIN t001 ON t001~bukrs = t001k~bukrs
    INTO lv_mata_waers
    WHERE t001k~bwkey = p_werk.

  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'MATPREIS'.
  ls_fieldcat-seltext_l = |Materialanteil je Einheit { lv_mata_waers }|.
  ls_fieldcat-seltext_m = |Mat.ant./E { lv_mata_waers }|.
  ls_fieldcat-seltext_s = 'MatE'.
  ls_fieldcat-reptext_ddic = ls_fieldcat-seltext_l.
  ls_fieldcat-ddictxt   = 'L'.
  ls_fieldcat-no_zero   = 'X'.
  APPEND ls_fieldcat TO lt_mata_fc.

  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'MATANTEIL'.
  ls_fieldcat-seltext_l = |Materialanteil { lv_mata_waers }|.
  ls_fieldcat-seltext_m = |Mat.anteil { lv_mata_waers }|.
  ls_fieldcat-seltext_s = 'MatA'.
  ls_fieldcat-reptext_ddic = ls_fieldcat-seltext_l.
  ls_fieldcat-ddictxt   = 'L'.
  ls_fieldcat-currency  = lv_mata_waers.
  ls_fieldcat-no_zero   = 'X'.
  ls_fieldcat-do_sum    = 'X'.
  APPEND ls_fieldcat TO lt_mata_fc.

  READ TABLE e01_lt_fieldcat TRANSPORTING NO FIELDS WITH KEY fieldname = 'STANDARDWERT'.
  IF sy-subrc = 0.
    lv_mata_idx = sy-tabix + 1.
    INSERT LINES OF lt_mata_fc INTO e01_lt_fieldcat INDEX lv_mata_idx.
  ELSE.
    APPEND LINES OF lt_mata_fc TO e01_lt_fieldcat.
  ENDIF.
