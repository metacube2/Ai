
*----------------------------------------------------------------------*
* FLUSS_KURZ: Fakturazeilen (VBTYP_N = 'M') einer Auftragsposition direkt
* aus VBFA, so wie RV_ORDER_FLOW_INFORMATION sie liefert (Pruefmodus
* 2026-10-12): abwaerts nur ueber direkte Verknuepfungen (STUFE = '00'),
* Knoten immer mit Belegtyp (dieselbe Nummer gibt es in verschiedenen
* Belegarten, z. B. Handling Unit und Kundenauftrag), je Beleg auch die
* Kopfverknuepfung (Position 000000). Hoechstens 50 Stufen.
*----------------------------------------------------------------------*
FORM fluss_kurz USING iv_vbeln TYPE vbeln
                      iv_posnr TYPE posnr
                CHANGING ct_vbfa TYPE STANDARD TABLE.
  TYPES: BEGIN OF lty_knoten,
           vbeln TYPE vbeln,
           posnr TYPE posnr,
           vbtyp TYPE vbfa-vbtyp_v,
         END OF lty_knoten.
  DATA: lt_front   TYPE STANDARD TABLE OF lty_knoten,
        lt_neu     TYPE STANDARD TABLE OF lty_knoten,
        lt_gesehen TYPE HASHED TABLE OF lty_knoten WITH UNIQUE KEY vbeln posnr vbtyp,
        lt_zeilen  TYPE STANDARD TABLE OF vbfa,
        lt_ruuid   TYPE HASHED TABLE OF vbfa-ruuid WITH UNIQUE KEY table_line,
        ls_k       TYPE lty_knoten,
        lv_vbtyp   TYPE vbak-vbtyp.

  CLEAR ct_vbfa.
  SELECT SINGLE vbtyp FROM vbak INTO lv_vbtyp WHERE vbeln = iv_vbeln.
  CHECK sy-subrc = 0.
  ls_k-vbeln = iv_vbeln.
  ls_k-posnr = iv_posnr.
  ls_k-vbtyp = lv_vbtyp.
  APPEND ls_k TO lt_front.
  INSERT ls_k INTO TABLE lt_gesehen.
  DO 50 TIMES.
    IF lt_front IS INITIAL.
      EXIT.
    ENDIF.
    CLEAR lt_zeilen.
    SELECT * FROM vbfa INTO TABLE lt_zeilen               "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_front
      WHERE vbelv   = lt_front-vbeln
      AND   posnv   = lt_front-posnr
      AND   vbtyp_v = lt_front-vbtyp
      AND   stufe   = '00'.
    CLEAR lt_neu.
    LOOP AT lt_zeilen INTO DATA(ls_z).
      IF ls_z-vbtyp_n = 'M'.
        READ TABLE lt_ruuid WITH TABLE KEY table_line = ls_z-ruuid TRANSPORTING NO FIELDS.
        IF sy-subrc <> 0.
          INSERT ls_z-ruuid INTO TABLE lt_ruuid.
          APPEND ls_z TO ct_vbfa.
        ENDIF.
      ENDIF.
      " Nachfolger als Position und als Kopf weiterverfolgen
      DO 2 TIMES.
        ls_k-vbeln = ls_z-vbeln.
        ls_k-vbtyp = ls_z-vbtyp_n.
        IF sy-index = 1.
          ls_k-posnr = ls_z-posnn.
        ELSE.
          ls_k-posnr = '000000'.
        ENDIF.
        READ TABLE lt_gesehen WITH TABLE KEY vbeln = ls_k-vbeln posnr = ls_k-posnr vbtyp = ls_k-vbtyp
             TRANSPORTING NO FIELDS.
        IF sy-subrc <> 0.
          INSERT ls_k INTO TABLE lt_gesehen.
          APPEND ls_k TO lt_neu.
        ENDIF.
      ENDDO.
    ENDLOOP.
    lt_front = lt_neu.
  ENDDO.
ENDFORM.
