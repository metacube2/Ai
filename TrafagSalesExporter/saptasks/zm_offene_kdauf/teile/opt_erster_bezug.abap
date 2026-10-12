*&---------------------------------------------------------------------*
*&      Form  ERSTER_BEZUG
*&---------------------------------------------------------------------*
* OPT 2026-10-12: war SELECT * FROM aufm ... ORDER BY bldat, erste Zeile.
* Das ist das kleinste BLDAT je Auftrag; PREFETCH_OPT liest es einmal vorab.
FORM erster_bezug .
  READ TABLE gt_o_aufm INTO DATA(ls_o_aufm) WITH TABLE KEY aufnr = iafpo-aufnr.
  IF sy-subrc = 0.
    gt_uebauf-zzmbdat = ls_o_aufm-bldat.
  ENDIF.
ENDFORM.                    " ERSTER_BEZUG
