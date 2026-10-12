*---------------------------------------------------------------------*
*       FORM bereich                                                  *
*---------------------------------------------------------------------*
* OPT 2026-10-12: Ergebnis je Material zwischengespeichert. Z_BASISPREISLISTE
* bekommt nur das Material, MARA-ZZTYP haengt ebenfalls nur daran. N wird wie
* bisher nur auf dem einen Weg unten gesetzt, sonst bleibt es unveraendert.
FORM bereich USING n.
  DATA: ls_o_bereich TYPE ty_o_bereich.

  READ TABLE gt_o_bereich INTO ls_o_bereich WITH TABLE KEY matnr = vbap-matnr.
  IF sy-subrc <> 0.
    CLEAR ls_o_bereich.
    ls_o_bereich-matnr = vbap-matnr.
    PERFORM bereich_rechnen CHANGING ls_o_bereich.
    INSERT ls_o_bereich INTO TABLE gt_o_bereich.
  ENDIF.
  IF ls_o_bereich-gesetzt = abap_true.
    n = ls_o_bereich-wert.
  ENDIF.
ENDFORM.                    "bereich

* Bisheriger Inhalt von BEREICH, unveraendert bis auf das Ziel der Zuweisung.
FORM bereich_rechnen CHANGING cs_bereich TYPE ty_o_bereich.
  REFRESH bptab.
  " Typ 87: das Original ruft Z_BASISPREISLISTE und verwirft das Ergebnis gleich danach
  " ("kein Range"). Der Baustein setzt alle seine Werte bei jedem Aufruf neu (geprueft in
  " LZZLO1F01), der Aufruf kann also entfallen.
  IF mara-zztyp(2) = '87'.
    EXIT.
  ENDIF.
  " parallel vorab gerechnet (PARALLEL_RECHNEN), sonst wie bisher selbst aufrufen
  READ TABLE gt_o_par_bp INTO DATA(ls_o_par_bp) WITH TABLE KEY matnr = vbap-matnr.
  IF sy-subrc = 0.
    bptab[] = ls_o_par_bp-bptab.
  ELSE.
  CALL FUNCTION 'Z_BASISPREISLISTE'
    EXPORTING
      i_matnr   = vbap-matnr
      i_mit_loe = 'X'
    TABLES
      bptab     = bptab
    EXCEPTIONS
      no_object = 1
      no_typcd  = 2
      OTHERS    = 3.
  ENDIF.

  IF mara-zztyp(2) = '87'.             "kein Range !!!
    EXIT.
  ENDIF.

  IF mara-zztyp(1) = '8'.
    READ TABLE bptab INDEX 2.
  ELSE.
    READ TABLE bptab INDEX 3.
  ENDIF.

  IF sy-subrc = 0.
    WHILE bptab-text NE space.
      IF bptab-text(1) CO '+-0123456789'.
        EXIT.
      ENDIF.
      SHIFT bptab-text.
    ENDWHILE.
    IF bptab-text(1) CO '+-0123456789'.
      cs_bereich-wert    = bptab-text.
      cs_bereich-gesetzt = abap_true.
      EXIT.
    ENDIF.
  ENDIF.
ENDFORM.                    "bereich_rechnen
