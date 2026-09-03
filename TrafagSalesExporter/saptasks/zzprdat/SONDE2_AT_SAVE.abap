METHOD if_ex_workorder_update~at_save.
* Diagnosesonde 2 vom 2026-09-03. Nur T76/100, Paket $TMP. Keine Zielfassung.
* Gegenstueck zur Sonde in AT_RELEASE, mit eigenem Zielauftrag 1241805.
* Gleiche Kodierung, aber im Jahr 1901, damit beide Methoden unterscheidbar
* bleiben:
*   01.01.1901  AUFNR leer,   GLTRP leer
*   02.01.1901  AUFNR gefuellt, GLTRP leer
*   03.01.1901  AUFNR leer,   GLTRP gefuellt
*   04.01.1901  beide gefuellt
  DATA: lv_ziel   TYPE aufnr,
        lv_marke  TYPE zco_gltrp,
        lv_aufnr  TYPE aufnr,
        lv_gltrp  TYPE zco_gltrp.

  lv_aufnr = is_header_dialog-aufnr.
  lv_gltrp = is_header_dialog-gltrp.

  IF lv_aufnr IS INITIAL AND lv_gltrp IS INITIAL.
    lv_marke = '19010101'.
  ELSEIF lv_aufnr IS NOT INITIAL AND lv_gltrp IS INITIAL.
    lv_marke = '19010102'.
  ELSEIF lv_aufnr IS INITIAL AND lv_gltrp IS NOT INITIAL.
    lv_marke = '19010103'.
  ELSE.
    lv_marke = '19010104'.
  ENDIF.

  lv_ziel = '000001241805'.

  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = lv_ziel
      iv_prddat = lv_marke.
ENDMETHOD.
