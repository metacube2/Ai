METHOD if_ex_workorder_update~at_release.
* Prototyp fuer T76/100. IS_HEADER_DIALOG-GLTRP vor Aktivierung im
* Methodeneditor pruefen; AUFNR und AUTYP sind live bzw. offiziell belegt.

  CHECK is_header_dialog-autyp = '10'. " Fertigungsauftrag
  CHECK is_header_dialog-aufnr IS NOT INITIAL.
  CHECK is_header_dialog-gltrp IS NOT INITIAL.

  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = is_header_dialog-aufnr
      iv_prddat = is_header_dialog-gltrp.

ENDMETHOD.
