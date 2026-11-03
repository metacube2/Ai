METHOD start_process.

  "Helpfields
  DATA: lv_method          TYPE seocpdname.

  "Read value from selection-screen to a report
  me->get_values_from_sel_screen( ).

  "Infos to Reportselection-parameters
  me->set_reportselections_info( ).

  zif_hr_pa_interface_rexx~mv_begda = me->mv_begda.
  zif_hr_pa_interface_rexx~mv_sydat = me->mv_sydat.

  IF zif_hr_pa_interface_rexx~mv_begda IS INITIAL
 AND zif_hr_pa_interface_rexx~mv_sydat IS NOT INITIAL.
    zif_hr_pa_interface_rexx~mv_begda = me->mv_begda = sy-datum. "sy-datum verwenden
  ENDIF.

  "Upload
  IF me->mv_appl IS NOT INITIAL
  OR sy-batch    IS NOT INITIAL.
    lv_method = 'UPLOAD_APPL'.
  ELSE.
    lv_method = 'UPLOAD_FRON'.
  ENDIF.

  "Upload
  CALL METHOD (lv_method).

  IF lines( me->mt_rexx_import ) = 0.
    "write log and raise exception

    DATA(ls_msg) = VALUE bal_s_msg(
            msgty     = 'E'
            msgid     = 'Z_HR_PA_REXX'
            msgno     = '001'
            detlevel  = 1
            probclass = 4
         ).

    "Add a message to Log
    me->add_message( ls_msg ).

    DATA(ls_t100key) = VALUE scx_t100key(
       msgid = 'Z_HR_PA_REXX'
       msgno = '001'
    ).

    "Set message and raise
    RAISE EXCEPTION TYPE zcx_hr_pa_interface_rexx
      EXPORTING
        textid = ls_t100key.
  ENDIF.

  "Daten aus DB zu allen Infotypen lesen
  me->read_infotype_datas( ).

  "Pro Person (dann inerhalb Schleife) oder doch alle zusammen (dann hier)
  DATA(lr_interface_rexx_comp) = NEW zcl_hr_pa_interface_rexx_comp( me ).

  "Start Process (compare)
  lr_interface_rexx_comp->start_process( ).

  CALL FUNCTION 'BAPI_TRANSACTION_COMMIT'
    EXPORTING
      wait = abap_true.

  IF sy-batch   IS INITIAL
 AND me->mv_alv IS INITIAL.
    me->show_message( ).

  ENDIF.

ENDMETHOD.
