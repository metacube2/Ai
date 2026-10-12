FORM get_standard_price USING    p_matnr TYPE matnr
                                 p_werks TYPE werks_d
                        CHANGING p_stdprice TYPE p.
  DATA: lv_bwkey TYPE bwkey.

  " Bewertungsbereich ist in diesem Fall gleich dem Werk
  lv_bwkey = p_werks.

  " Standardpreis aus MBEW holen
  " OPT 2026-10-12: war SELECT SINGLE stprs INTO p_stdprice FROM mbew WHERE matnr bwkey;
  " nicht gefunden -> P_STDPRICE bleibt wie bisher unveraendert
  READ TABLE gt_o_mbew INTO DATA(ls_o_mbew) WITH TABLE KEY matnr = p_matnr bwkey = lv_bwkey.
  IF sy-subrc = 0.
    p_stdprice = ls_o_mbew-stprs.
  ENDIF.
ENDFORM.


FORM fill_fkdat USING p_vbeln TYPE vbeln
                       p_posnr TYPE posnr
                       p_menge TYPE vbrp-fkimg
                CHANGING ct_uebauf TYPE zmm_ueb_kdauf.
  DATA: lt_vbfa       TYPE TABLE OF vbfa,
        ls_vbfa       TYPE vbfa,
        lt_vbrk       TYPE TABLE OF vbrk,
        ls_vbrk       TYPE vbrk,
        lv_cum_qty    TYPE vbrp-fkimg,
        lv_menge      TYPE vbrp-fkimg,
        lv_cnt        TYPE i,
        lv_needed_qty TYPE vbrp-fkimg,
        lv_leaveall   TYPE bool,
        lv_date       TYPE vbrk-fkdat.


  IF p_vbeln <> gv_lastvbeln.
    gv_cursor = 0.
  ENDIF.
  IF p_posnr <> gv_lastpos.
    " Counter reseten , neuer beleg beginn von oben
    gv_cursor = 0.
  ENDIF.
  " OPT 2026-10-12: Belegfluss je Position nur einmal lesen. IVBEP ist nach VBELN/POSNR
  " sortiert, die Einteilungen einer Position kommen also direkt hintereinander; dann wird
  " das unveraenderte Ergebnis des Bausteins vom Aufruf davor wiederverwendet.
  " Hat der Beleg gar keinen Belegfluss (kein VBFA-Satz als Vorgaenger oder Nachfolger), kann
  " der Baustein keine Fakturazeile (VBTYP_N = 'M') liefern; die Schleife unten taete nichts.
  READ TABLE gt_o_flow_beleg WITH TABLE KEY table_line = p_vbeln TRANSPORTING NO FIELDS.
  IF sy-subrc <> 0.
    CLEAR lt_vbfa.
    gv_n_flow_ohne = gv_n_flow_ohne + 1.
  ELSEIF gv_o_flow_da = abap_true AND gv_o_flow_vbeln = p_vbeln AND gv_o_flow_posnr = p_posnr.
    lt_vbfa = gt_o_flow_vbfa.
  ELSE.
    gv_n_flow_fm = gv_n_flow_fm + 1.
    DATA(lv_vbco6) = VALUE vbco6( vbeln = p_vbeln  posnr =  p_posnr ).
    CALL FUNCTION 'RV_ORDER_FLOW_INFORMATION'        "#EC CI_USAGE_OK[2198647]
      EXPORTING
        comwa         = lv_vbco6
      " BELEGTYP      = 'M'
      TABLES
        vbfa_tab      = lt_vbfa
      EXCEPTIONS
        no_vbfa       = 1
        no_vbuk_found = 2
        OTHERS        = 3.
    gt_o_flow_vbfa  = lt_vbfa.
    gv_o_flow_vbeln = p_vbeln.
    gv_o_flow_posnr = p_posnr.
    gv_o_flow_da    = abap_true.
  ENDIF.
  CLEAR lv_date.
  CLEAR lv_menge.
  SORT lt_vbfa BY erdat ASCENDING.
  " Vorfiltern der Tabelle lt_vbfa für Einträge mit vbtyp_n = 'M'
  DELETE lt_vbfa WHERE vbtyp_n <> 'M'.
  " Wenn nur eine Auslieferung stattgefunden hat kann diese Rechnung das erste mal die volle Menge enthalten.
  LOOP AT lt_vbfa INTO DATA(ls_vbkr_ext).
    CLEAR lv_cum_qty.
    " die erstel 2 überspringen weil obeen schon 2 abgezogen wurden und enddatum ermitteln
    lv_cnt = lv_cnt + 1.
    IF ( lv_cnt > gv_cursor ) OR   (  lines( lt_vbfa ) = 1 ).
      lv_menge = lv_menge + ls_vbkr_ext-rfmng.
      IF ( lv_menge  = p_menge ) OR (  lines( lt_vbfa ) = 1 )  . "menge erreicht, datum wegschreiben cursor in index
        gv_cursor =  lv_cnt.
        ct_uebauf-fkdat = ls_vbkr_ext-erdat.
        lv_leaveall = abap_true.
        gv_lastpos =  p_posnr.
        gv_lastvbeln =  p_vbeln.
        EXIT.
      ENDIF.
    ENDIF.
    IF ( lv_leaveall = abap_true ).
      EXIT.
    ENDIF.
  ENDLOOP.
ENDFORM.
