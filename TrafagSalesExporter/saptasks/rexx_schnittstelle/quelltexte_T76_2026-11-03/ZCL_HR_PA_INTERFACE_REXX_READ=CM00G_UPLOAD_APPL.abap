METHOD upload_appl.

  "Structures
  DATA: ls_rexx_import TYPE zhr_pa_rexx_import_s.

  "Helpfields
  DATA: lv_path TYPE string,
        lv_mess TYPE string.

  "Field-Symbols
  FIELD-SYMBOLS:
        <hex_container> TYPE x.

  CLEAR: me->mt_rexx_import.

  SELECT dirname INTO @DATA(lv_dirname)
         FROM user_dir
           UP TO 1 ROWS
        WHERE aliass EQ @me->mv_dir.
  ENDSELECT.

  lv_path = lv_dirname && '\' && me->mv_osfile.

  OPEN DATASET lv_path FOR INPUT IN BINARY MODE
                                      MESSAGE lv_mess.
  IF sy-subrc NE 0.
    DATA(ls_t100key) = VALUE scx_t100key(
       msgid = sy-msgid
       msgno = sy-msgno
       attr1 = sy-msgv1
       attr2 = sy-msgv2
       attr3 = sy-msgv3
       attr4 = sy-msgv4
    ).

    "Set message and raise
    RAISE EXCEPTION TYPE zcx_hr_pa_interface_rexx
      EXPORTING
        textid = ls_t100key.
  ENDIF.

  ASSIGN ls_rexx_import TO <hex_container> CASTING.

  DO.
    READ DATASET lv_path INTO <hex_container>.
    IF sy-subrc = 0.
      APPEND ls_rexx_import TO me->mt_rexx_import.
    ELSE.
      EXIT.
    ENDIF.
  ENDDO.

  CLOSE DATASET lv_path.

  "ABKRS_0001(2) = 99 0der '  ', nicht berücksichtigen
  "gem. Besrechung mit S. Wyden vom 18.04.2024
  DELETE me->mt_rexx_import WHERE abkrs_0001(2) EQ '99'
                               OR abkrs_0001(2) EQ '99'.

ENDMETHOD.
