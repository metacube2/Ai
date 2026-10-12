
*&---------------------------------------------------------------------*
*&      Form  PARALLEL_RECHNEN
*&---------------------------------------------------------------------*
* OPT 2026-10-12: RV_ORDER_FLOW_INFORMATION (je Position mit Belegfluss)
* und Z_BASISPREISLISTE (je Material) laufen paketweise parallel ueber
* Z_KDAUF_PAR_RECHNEN (STARTING NEW TASK, Gruppe DEFAULT). Die Ergebnisse
* sind die unveraenderten Rueckgaben der Bausteine; FILL_FKDAT und BEREICH
* verwenden sie statt eines eigenen Aufrufs. Fehlt ein Ergebnis (kein
* Prozess frei, Abbruch), rufen sie den Baustein wie bisher selbst auf.
*----------------------------------------------------------------------*
FORM parallel_rechnen.
  TYPES: BEGIN OF lty_pos,
           vbeln TYPE vbeln,
           posnr TYPE posnr,
         END OF lty_pos.
  DATA: lt_pos     TYPE STANDARD TABLE OF lty_pos,
        lt_matnr   TYPE STANDARD TABLE OF matnr,
        lt_pak_pos TYPE STANDARD TABLE OF lty_pos,
        lt_pak_mat TYPE STANDARD TABLE OF matnr,
        lv_in      TYPE xstring,
        lv_task    TYPE c LENGTH 32,
        lv_i       TYPE i,
        lv_pos_n   TYPE i,
        lv_mat_n   TYPE i,
        lv_versuch TYPE i,
        lv_ts0     TYPE timestampl,
        lv_ts1     TYPE timestampl.

  CLEAR: gt_o_par_fluss, gt_o_par_bp, gv_par_offen, gv_n_par_pakete, gv_n_par_fehler.
  CHECK p_ptask > 0 AND p_ppak > 0.
  GET TIME STAMP FIELD lv_ts0.

  " Positionen genau so, wie FILL_FKDAT sie braucht: jede Position aus IVBEP,
  " deren Beleg ueberhaupt einen Belegfluss hat
  LOOP AT ivbep[] INTO DATA(ls_ep).
    READ TABLE gt_o_flow_beleg WITH TABLE KEY table_line = ls_ep-vbeln TRANSPORTING NO FIELDS.
    IF sy-subrc = 0.
      APPEND VALUE #( vbeln = ls_ep-vbeln posnr = ls_ep-posnr ) TO lt_pos.
    ENDIF.
  ENDLOOP.
  SORT lt_pos BY vbeln posnr.
  DELETE ADJACENT DUPLICATES FROM lt_pos COMPARING vbeln posnr.

  " Materialien genau so, wie BEREICH sie braucht: VBAP-MATNR der gepufferten Positionen
  LOOP AT gt_o_vbap INTO DATA(ls_vbap) WHERE matnr IS NOT INITIAL.
    READ TABLE gt_o_mara INTO DATA(ls_mara) WITH TABLE KEY matnr = ls_vbap-matnr.
    IF sy-subrc = 0 AND ls_mara-zztyp(2) = '87'.
      CONTINUE.            " BEREICH braucht fuer Typ 87 kein Ergebnis
    ENDIF.
    APPEND ls_vbap-matnr TO lt_matnr.
  ENDLOOP.
  SORT lt_matnr.
  DELETE ADJACENT DUPLICATES FROM lt_matnr.

  lv_pos_n = lines( lt_pos ).
  lv_mat_n = lines( lt_matnr ).
  lv_i = 0.
  WHILE lv_i < lv_pos_n OR lv_i < lv_mat_n.
    CLEAR: lt_pak_pos, lt_pak_mat.
    IF lv_i < lv_pos_n.
      APPEND LINES OF lt_pos FROM lv_i + 1 TO lv_i + p_ppak TO lt_pak_pos.
    ENDIF.
    IF lv_i < lv_mat_n.
      APPEND LINES OF lt_matnr FROM lv_i + 1 TO lv_i + p_ppak TO lt_pak_mat.
    ENDIF.
    lv_i = lv_i + p_ppak.

    CLEAR lv_in.
    EXPORT pos = lt_pak_pos matnr = lt_pak_mat TO DATA BUFFER lv_in.
    gv_n_par_pakete = gv_n_par_pakete + 1.
    lv_task = |ZKDAUF{ gv_n_par_pakete }|.

    " Kein freier Prozess: kurz warten, bis eine laufende Aufgabe fertig ist, und erneut
    " versuchen. Klappt es gar nicht, bleibt das Paket ohne Ergebnis und wird spaeter in
    " der Schleife wie bisher einzeln gerechnet.
    lv_versuch = 0.
    DO.
      CALL FUNCTION 'Z_KDAUF_PAR_RECHNEN'
        STARTING NEW TASK lv_task
        DESTINATION IN GROUP DEFAULT
        PERFORMING par_ende ON END OF TASK
        EXPORTING
          iv_in                 = lv_in
        EXCEPTIONS
          resource_failure      = 1
          communication_failure = 2
          system_failure        = 3
          OTHERS                = 4.
      IF sy-subrc = 0.
        gv_par_offen = gv_par_offen + 1.
        EXIT.
      ENDIF.
      lv_versuch = lv_versuch + 1.
      IF sy-subrc <> 1 OR lv_versuch > 30.
        gv_n_par_fehler = gv_n_par_fehler + 1.
        EXIT.
      ENDIF.
      WAIT UNTIL gv_par_offen = 0 UP TO 1 SECONDS.
    ENDDO.

    WAIT UNTIL gv_par_offen < p_ptask.
  ENDWHILE.
  WAIT UNTIL gv_par_offen = 0.

  GET TIME STAMP FIELD lv_ts1.
  gv_t_par = cl_abap_tstmp=>subtract( tstmp1 = lv_ts1 tstmp2 = lv_ts0 ).
ENDFORM.                    " parallel_rechnen

FORM par_ende USING p_task TYPE clike.
  DATA: lv_out   TYPE xstring,
        lt_fluss TYPE STANDARD TABLE OF ty_o_par_fluss,
        lt_bp    TYPE STANDARD TABLE OF ty_o_par_bp.

  RECEIVE RESULTS FROM FUNCTION 'Z_KDAUF_PAR_RECHNEN'
    IMPORTING
      ev_out                = lv_out
    EXCEPTIONS
      communication_failure = 1
      system_failure        = 2
      OTHERS                = 3.
  gv_par_offen = gv_par_offen - 1.
  IF sy-subrc <> 0 OR lv_out IS INITIAL.
    gv_n_par_fehler = gv_n_par_fehler + 1.
    RETURN.
  ENDIF.
  IMPORT fluss = lt_fluss bp = lt_bp FROM DATA BUFFER lv_out.
  INSERT LINES OF lt_fluss INTO TABLE gt_o_par_fluss.
  INSERT LINES OF lt_bp INTO TABLE gt_o_par_bp.
ENDFORM.                    " par_ende
