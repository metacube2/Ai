METHOD read_infotype_datas.

  "Internal tables
  DATA: lt_persnumbers TYPE STANDARD TABLE OF me->zif_hr_pa_interface_rexx~mty_persnumber_s.

  "Helpfields
  DATA: lf_abkrs_0001 TYPE numc2.

  "Helpfields
  DATA: lv_infty     TYPE infty,
        lv_infty_old TYPE infty,
        lv_nahvn     TYPE p02_nahvnr,
        lv_subty     TYPE subty,
        lv_begda     TYPE begda,
        lv_endda     TYPE endda,
        lv_fieldname TYPE char30,
        lv_times     TYPE i.

  "Initialize global tables
  me->initialize_tables( ).

  LOOP AT me->mt_rexx_import REFERENCE INTO DATA(lr_s_rexx_import).

    lv_nahvn = me->convert_nahvn_to_internal( lr_s_rexx_import->nahvn_0036 ).

    SELECT DISTINCT _36~pernr, _36~nahvn, _01~abkrs, _00~endda, _00~begda, _00~stat2
           FROM pa0036    AS _36
           JOIN pa0001    AS _01
             ON _01~pernr EQ _36~pernr
           JOIN pa0000    AS _00
             ON _00~pernr EQ _36~pernr
      APPENDING TABLE @me->zif_hr_pa_interface_rexx~mt_0036_key
          WHERE _36~subty EQ @abap_false
            AND _36~objps EQ @abap_false
            AND _36~sprps EQ @abap_false
            AND _36~endda GE @me->mv_begda
            AND _36~begda LE '99991231'
            AND _36~nahvn EQ @lv_nahvn.

  ENDLOOP.

  SORT me->zif_hr_pa_interface_rexx~mt_0036_key BY nahvn ASCENDING abkrs ASCENDING pernr DESCENDING begda DESCENDING.

  LOOP AT me->mt_rexx_import REFERENCE INTO lr_s_rexx_import.

    lf_abkrs_0001 = lr_s_rexx_import->abkrs_0001.

    lv_nahvn = me->convert_nahvn_to_internal( lr_s_rexx_import->nahvn_0036 ).

    "Ist Neueintritt?
    LOOP AT me->zif_hr_pa_interface_rexx~mt_0036_key REFERENCE INTO DATA(lr_s_0036_key)
       WHERE nahvn EQ lv_nahvn
         AND abkrs EQ lf_abkrs_0001
         AND endda GE me->mv_begda
         AND begda LE '99991231'.

      DATA(lv_pernr) = lr_s_0036_key->pernr.
      EXIT.
    ENDLOOP.
    IF sy-subrc NE 0.
      "Ist Wiedereintritt?
      LOOP AT me->zif_hr_pa_interface_rexx~mt_0036_key REFERENCE INTO lr_s_0036_key
        WHERE nahvn EQ lv_nahvn
          AND abkrs EQ lf_abkrs_0001
          AND endda LE me->mv_begda.

*        lv_pernr = lr_s_0036_key->pernr. "NICHT überschreiben bei Wiedereintritt
        EXIT.
      ENDLOOP.
      IF sy-subrc NE 0.
        "Neueintritt
        CONTINUE.
      ENDIF.
    ENDIF.

    "Merken für InfTyp PA90001-ZREXXN
    APPEND INITIAL LINE TO lt_persnumbers REFERENCE INTO DATA(lr_s_persnumber).
    lr_s_persnumber->pernr  = lv_pernr.
    lr_s_persnumber->zrexxn = lr_s_rexx_import->pernr.

*    "Wenn gefunden, richtige PERNR in me->mt_rexx_import übertragen - das ist beim Vergleich hilfreich!
*    lr_s_rexx_import->pernr = lv_pernr.

    LOOP AT me->mr_struc_ref->components ASSIGNING FIELD-SYMBOL(<ls_components>)
      WHERE name NE 'PERNR'.

      WRITE <ls_components>-name TO lv_fieldname RIGHT-JUSTIFIED.
      lv_infty = lv_fieldname+26(4).

      IF lv_infty <> lv_infty_old
     AND lv_infty IS NOT INITIAL.
        lv_infty_old = lv_infty.

        CLEAR: lv_begda, lv_endda, lv_subty.

        lv_times = 1.

        lv_fieldname = 'lr_s_rexx_import->begda_' && lv_infty.
        ASSIGN (lv_fieldname) TO FIELD-SYMBOL(<lv_begda>).
        IF sy-subrc EQ 0.
          lv_begda = <lv_begda>.
        ENDIF.

        lv_fieldname = 'lr_s_rexx_import->endda_' && lv_infty.
        ASSIGN (lv_fieldname) TO FIELD-SYMBOL(<lv_endda>).
        IF sy-subrc EQ 0.
          lv_endda = <lv_endda>.
        ENDIF.

        lv_fieldname = 'lr_s_rexx_import->subty_' && lv_infty.
        ASSIGN (lv_fieldname) TO FIELD-SYMBOL(<lv_subty>).
        IF sy-subrc EQ 0.
          lv_subty = <lv_subty>.
        ENDIF.

        IF lv_infty = '0105'.
          lv_times = 3.
          lv_subty = '0001'.
        ENDIF.

        IF lv_begda IS INITIAL.
          lv_begda = sy-datum.
        ENDIF.

        IF lv_infty = '0007'
       AND lr_s_rexx_import->begda_0007 IS NOT INITIAL.
          lv_begda = lr_s_rexx_import->begda_0007.
        ENDIF.

        IF lv_infty = '0008'
       AND lr_s_rexx_import->begda_0008 IS NOT INITIAL.
          lv_begda = lr_s_rexx_import->begda_0008.
        ENDIF.

        IF lv_endda IS INITIAL.
          lv_endda = '99991231'.
        ENDIF.

        DO lv_times TIMES.

          IF lv_infty = '0105'.
            IF sy-index = 2.
              lv_subty = '0030'. "Mail USRID_LONG_0105

            ELSEIF sy-index = 3.
              lv_subty = '0010'. "Mail USRID_LONG_0105_0010

            ENDIF.
          ENDIF.

          TRY.
              CALL METHOD me->read_infotype_dynamic "Methode /XPR/728_CL_HANDLE_DATA=>READ_INFOTYPE_DYNAMIC()
                EXPORTING
                  iv_pernr = lv_pernr
                  iv_begda = lv_begda
                  iv_endda = lv_endda
                  iv_infty = lv_infty
                  iv_subty = lv_subty.

            CATCH zcx_hr_pa_interface_rexx ##NO_HANDLER.

              "Fehler bereits in Log geschrieben
              CONTINUE.
          ENDTRY.

        ENDDO.

      ENDIF.

    ENDLOOP.

  ENDLOOP.

  "To sorted table
  me->zif_hr_pa_interface_rexx~mt_persnumbers = lt_persnumbers.

ENDMETHOD.
