
*----------------------------------------------------------------------*
* FLUSS_KURZ: Fakturazeilen (VBTYP_N = 'M') einer Auftragsposition direkt
* aus VBFA, abwaerts ueber die Positionsverknuepfung (VBELV/POSNV ->
* VBELN/POSNN), hoechstens 50 Stufen. Ersatz fuer RV_ORDER_FLOW_INFORMATION
* nur dort, wo FILL_FKDAT ihn braucht, und nur, wenn der Pruefmodus fuer
* alle Positionen dieselbe Folge (ERDAT, RFMNG) wie das Original zeigt.
*----------------------------------------------------------------------*
FORM fluss_kurz USING iv_vbeln TYPE vbeln
                      iv_posnr TYPE posnr
                CHANGING ct_vbfa TYPE STANDARD TABLE.
  TYPES: BEGIN OF lty_knoten,
           vbeln TYPE vbeln,
           posnr TYPE posnr,
         END OF lty_knoten.
  DATA: lt_front   TYPE STANDARD TABLE OF lty_knoten,
        lt_neu     TYPE STANDARD TABLE OF lty_knoten,
        lt_gesehen TYPE HASHED TABLE OF lty_knoten WITH UNIQUE KEY vbeln posnr,
        lt_zeilen  TYPE STANDARD TABLE OF vbfa,
        lt_ruuid   TYPE HASHED TABLE OF vbfa-ruuid WITH UNIQUE KEY table_line,
        ls_k       TYPE lty_knoten.

  CLEAR ct_vbfa.
  ls_k-vbeln = iv_vbeln.
  ls_k-posnr = iv_posnr.
  APPEND ls_k TO lt_front.
  INSERT ls_k INTO TABLE lt_gesehen.
  DO 50 TIMES.
    IF lt_front IS INITIAL.
      EXIT.
    ENDIF.
    CLEAR lt_zeilen.
    SELECT * FROM vbfa INTO TABLE lt_zeilen               "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_front
      WHERE vbelv = lt_front-vbeln
      AND   posnv = lt_front-posnr.
    CLEAR lt_neu.
    LOOP AT lt_zeilen INTO DATA(ls_z).
      IF ls_z-vbtyp_n = 'M'.
        READ TABLE lt_ruuid WITH TABLE KEY table_line = ls_z-ruuid TRANSPORTING NO FIELDS.
        IF sy-subrc <> 0.
          INSERT ls_z-ruuid INTO TABLE lt_ruuid.
          APPEND ls_z TO ct_vbfa.
        ENDIF.
      ENDIF.
      ls_k-vbeln = ls_z-vbeln.
      ls_k-posnr = ls_z-posnn.
      READ TABLE lt_gesehen WITH TABLE KEY vbeln = ls_k-vbeln posnr = ls_k-posnr
           TRANSPORTING NO FIELDS.
      IF sy-subrc <> 0.
        INSERT ls_k INTO TABLE lt_gesehen.
        APPEND ls_k TO lt_neu.
      ENDIF.
    ENDLOOP.
    lt_front = lt_neu.
  ENDDO.
ENDFORM.

*----------------------------------------------------------------------*
* FLUSS_VERGLEICH: Folge (ERDAT, RFMNG) so, wie FILL_FKDAT sie verarbeitet
* (SORT BY erdat, danach nur VBTYP_N = 'M'), Original gegen Kurz.
*   cv_menge: andere Fakturazeilen;  cv_folge: gleiche, andere Reihenfolge
*----------------------------------------------------------------------*
FORM fluss_vergleich USING it_orig TYPE STANDARD TABLE
                           it_kurz TYPE STANDARD TABLE
                     CHANGING cv_menge TYPE i
                              cv_folge TYPE i.
  TYPES: BEGIN OF lty_m,
           erdat TYPE vbfa-erdat,
           rfmng TYPE vbfa-rfmng,
         END OF lty_m.
  DATA: lt_o  TYPE STANDARD TABLE OF vbfa,
        lt_k  TYPE STANDARD TABLE OF vbfa,
        lt_mo TYPE STANDARD TABLE OF lty_m,
        lt_mk TYPE STANDARD TABLE OF lty_m.
  lt_o = it_orig.
  lt_k = it_kurz.
  SORT lt_o BY erdat ASCENDING.
  DELETE lt_o WHERE vbtyp_n <> 'M'.
  SORT lt_k BY erdat ASCENDING.
  DELETE lt_k WHERE vbtyp_n <> 'M'.
  LOOP AT lt_o INTO DATA(ls_o).
    APPEND VALUE #( erdat = ls_o-erdat rfmng = ls_o-rfmng ) TO lt_mo.
  ENDLOOP.
  LOOP AT lt_k INTO DATA(ls_kz).
    APPEND VALUE #( erdat = ls_kz-erdat rfmng = ls_kz-rfmng ) TO lt_mk.
  ENDLOOP.
  IF lt_mo = lt_mk.
    RETURN.
  ENDIF.
  SORT lt_mo BY erdat rfmng.
  SORT lt_mk BY erdat rfmng.
  IF lt_mo = lt_mk.
    cv_folge = cv_folge + 1.
  ELSE.
    cv_menge = cv_menge + 1.
  ENDIF.
ENDFORM.
