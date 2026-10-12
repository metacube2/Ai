FORM fill_uebauf.

  " Sort for correct fkdat assigning on fill_fkdat
  "insert 24.5.24
  " 2026-10-12: ETENR als letztes Sortierfeld. Ohne es ist die Reihenfolge von Einteilungen
  " mit gleichem Lieferdatum zufaellig (REF gegen REF belegt), und mit ihr die Zuordnung der
  " Fertigungsauftraege, die Auftragsfelder und das Fakturadatum dieser Einteilungen.
  SORT   ivbep BY vbeln posnr  edatu etenr ASCENDING.
  "end insert 24.5.24

  " OPT 2026-10-12: Alle Lesezugriffe der Schleife einmal vorab, mit denselben Bedingungen.
  " Die Schleife selbst ist Anweisung fuer Anweisung die alte; jeder ersetzte Zugriff steht
  " unten als Kommentar "war: ..." daneben.
  PERFORM prefetch_opt.

  LOOP AT ivbep. "Für jede Position in VBAP kann es mehrere Einteilungsdaten in VBEP geben.
    IF ivbep-edatu LT dat_lo OR
      dat_lo IS INITIAL.
      dat_lo = ivbep-edatu.
    ENDIF.
    IF ivbep-edatu GT dat_hi OR
      dat_hi IS INITIAL.
      dat_hi = ivbep-edatu.
    ENDIF.


    CLEAR vbak. CLEAR kna1.CLEAR vbap.CLEAR vbbe. CLEAR mara.CLEAR makt.

    " war: SELECT SINGLE * FROM vbak WHERE vbeln = ivbep-vbeln
    READ TABLE gt_o_vbak INTO vbak WITH TABLE KEY vbeln = ivbep-vbeln.
    " war: SELECT SINGLE * FROM kna1 erst mit werks = p_werk, sonst ohne. KUNNR ist der ganze
    "      Schluessel, beide Zugriffe liefern also dieselbe Zeile oder beide keine.
    READ TABLE gt_o_kna1 INTO kna1 WITH TABLE KEY kunnr = vbak-kunnr.
    IF sy-subrc <> 0.
      kna1-name1 = 'Kunde nicht gefunden'.
      " Optional: Protokollieren Sie den Fehler
    ENDIF.


    " war: SELECT SINGLE * FROM vbap WHERE vbeln posnr werks = p_werk (Puffer hat nur Werk p_werk)
    READ TABLE gt_o_vbap INTO vbap WITH TABLE KEY vbeln = ivbep-vbeln posnr = ivbep-posnr.
    CLEAR vbbe.
    " war: SELECT SINGLE * FROM vbbe WHERE vbeln posnr etenr werks = p_werk
    READ TABLE gt_o_vbbe INTO vbbe WITH TABLE KEY vbeln = ivbep-vbeln posnr = ivbep-posnr etenr = ivbep-etenr.
    " war: SELECT SINGLE * FROM mara WHERE matnr = vbap-matnr
    READ TABLE gt_o_mara INTO mara WITH TABLE KEY matnr = vbap-matnr.
    " war: SELECT SINGLE * FROM makt WHERE matnr = mara-matnr AND spras = sy-langu
    READ TABLE gt_o_makt INTO makt WITH TABLE KEY matnr = mara-matnr.

    CLEAR gt_uebauf.
    MOVE-CORRESPONDING ivbep TO gt_uebauf.
    MOVE-CORRESPONDING kna1 TO gt_uebauf.
    MOVE-CORRESPONDING vbak TO gt_uebauf.
    MOVE-CORRESPONDING vbap TO gt_uebauf.
    MOVE-CORRESPONDING mara TO gt_uebauf.
    MOVE-CORRESPONDING makt TO gt_uebauf.
    gt_uebauf-dispo = ''.






    gt_uebauf-omeng = vbbe-omeng.
    gt_uebauf-omengn = ivbep-ocdqty_bu. "KOI
    gt_uebauf-kunnr = vbak-kunnr.
    gt_uebauf-ernam = vbak-ernam.
    gt_uebauf-erdat = vbap-erdat.
    gt_uebauf-erdat = vbap-abgru.


    IF ivbep-wmeng = ivbep-bmeng.
      WRITE ivbep-edatu TO gt_uebauf-ketdat DD/MM/YYYY.
    ELSE.
      " war: SELECT * FROM vbep WHERE vbeln posnr bmeng = 0 AND edatu LT ivbep-edatu
      "      ORDER BY PRIMARY KEY. ENDSELECT.  -> letzte Zeile nach ETENR, nicht das spaeteste Datum
      LOOP AT gt_o_vbep0 INTO vbep WHERE vbeln = ivbep-vbeln
                                    AND   posnr = ivbep-posnr
                                    AND   edatu LT ivbep-edatu.
      ENDLOOP.
      IF sy-subrc = 0.
        WRITE vbep-edatu TO gt_uebauf-ketdat DD/MM/YYYY.
      ENDIF.
    ENDIF.
    " war: SELECT SINGLE bstdk INTO gt_uebauf-bstdk FROM vbkd (Position, sonst Kopf posnr = 0);
    "      nicht gefunden -> Feld behaelt den Wert aus MOVE-CORRESPONDING vbak
    READ TABLE gt_o_vbkd INTO DATA(ls_o_vbkd) WITH TABLE KEY vbeln = vbap-vbeln posnr = vbap-posnr.
    IF sy-subrc = 0.
      gt_uebauf-bstdk = ls_o_vbkd-bstdk.
    ELSE.
      READ TABLE gt_o_vbkd INTO ls_o_vbkd WITH TABLE KEY vbeln = vbap-vbeln posnr = '000000'.
      IF sy-subrc = 0.
        gt_uebauf-bstdk = ls_o_vbkd-bstdk.
      ENDIF.
    ENDIF.

    GET RUN TIME FIELD gv_rt0.
    CALL FUNCTION 'CONVERT_TO_LOCAL_CURRENCY'
      EXPORTING
        date             = sy-datum
        foreign_amount   = vbap-netwr
        foreign_currency = vbak-waerk
        local_currency   = 'CHF'
      IMPORTING
        local_amount     = w_netwr.
    GET RUN TIME FIELD gv_rt1.
    gv_t_conv = gv_t_conv + ( gv_rt1 - gv_rt0 ) / 1000000.

    gt_uebauf-netwr1  = w_netwr * gt_uebauf-bmeng / vbap-kwmeng.
    gt_uebauf-netwr2  = w_netwr * gt_uebauf-omeng / vbap-kwmeng.
    gt_uebauf-netwr3n = w_netwr * gt_uebauf-omengn / vbap-kwmeng.
    gt_uebauf-waers = 'CHF'.


    " Berechnung des Standardwerts
    "    gt_uebauf-standardwert = gt_uebauf-bmeng * gt_uebauf-netwr1 .


    gt_uebauf-vbeln = ivbep-vbeln.
    GET RUN TIME FIELD gv_rt0.
    PERFORM bereich USING gt_uebauf-range.
    GET RUN TIME FIELD gv_rt1.
    gv_t_bereich = gv_t_bereich + ( gv_rt1 - gv_rt0 ) / 1000000.

    " war: SELECT SINGLE dzeit FROM marc INTO gt_uebauf-dzeit WHERE matnr = mara-matnr AND werks = p_werk
    READ TABLE gt_o_marc INTO DATA(ls_o_marc) WITH TABLE KEY matnr = mara-matnr.
    IF sy-subrc = 0.
      gt_uebauf-dzeit = ls_o_marc-dzeit.
    ENDIF.

    " war: SELECT * FROM vbfa WHERE vbelv = vbap-vbeln AND posnv = vbap-posnr AND vbtyp_n = 'M'
    "      ORDER BY PRIMARY KEY. ENDSELECT.  Schluessel ist MANDT/RUUID, die letzte Zeile ist
    "      also die mit der groessten RUUID (im Vorabpuffer so ausgewaehlt).
    READ TABLE gt_o_vbfa INTO DATA(ls_o_vbfa) WITH TABLE KEY vbelv = vbap-vbeln posnv = vbap-posnr.
    IF sy-subrc = 0.
      gt_uebauf-fkdat =  ls_o_vbfa-erdat.
    ENDIF.

    CLEAR xweek.
    CALL FUNCTION 'DATE_GET_WEEK'
      EXPORTING
        date = ivbep-edatu
      IMPORTING
        week = xweek.
    gt_uebauf-kwoche1 = '../..'.
    gt_uebauf-kwoche1+0(2) = xweek+4(2).
    gt_uebauf-kwoche1+3(2) = xweek+2(2).

    gt_uebauf-kmonat+0(2) = ivbep-edatu+4(2).
    gt_uebauf-kmonat+2(1) = '/'.
    gt_uebauf-kmonat+3(2) = ivbep-edatu+2(2).

    CLEAR aufk.

    " Standardpreis holen
    PERFORM get_standard_price USING vbap-matnr
                                     vbap-werks
                              CHANGING gt_uebauf-standardpreis.



    " war: SELECT * FROM afpo WHERE kdauf = ivbep-vbeln AND kdpos = ivbep-posnr
    "      ORDER BY PRIMARY KEY. ... ENDSELECT.  Der Puffer ist nach KDAUF/KDPOS/AUFNR/POSNR
    "      sortiert, die Schleife laeuft also in derselben Reihenfolge.
    LOOP AT gt_o_afpo INTO afpo WHERE kdauf = ivbep-vbeln
                                AND   kdpos = ivbep-posnr.
      " war: READ TABLE iafpo WITH KEY aufnr = afpo-aufnr (lineare Suche ueber alle bisherigen)
      READ TABLE gt_o_aufnr_gesehen WITH TABLE KEY table_line = afpo-aufnr TRANSPORTING NO FIELDS.
      IF sy-subrc NE 0.
        iafpo-aufnr = afpo-aufnr.
        APPEND iafpo.
        INSERT afpo-aufnr INTO TABLE gt_o_aufnr_gesehen.

        " war: SELECT SINGLE * FROM afko WHERE aufnr = afpo-aufnr
        "      (nicht gefunden -> AFKO behaelt wie bisher den Wert der Vorzeile)
        READ TABLE gt_o_afko INTO afko WITH TABLE KEY aufnr = afpo-aufnr.
        " war: SELECT SINGLE * FROM aufk WHERE aufnr = afpo-aufnr AND werks = p_werk
        READ TABLE gt_o_aufk INTO aufk WITH TABLE KEY aufnr = afpo-aufnr.
        CLEAR xweek.
        CALL FUNCTION 'DATE_GET_WEEK'
          EXPORTING
            date = afko-gltrp
          IMPORTING
            week = xweek.
        gt_uebauf-kwoche2 = '../..'.
        gt_uebauf-kwoche2+0(2) = xweek+4(2).
        gt_uebauf-kwoche2+3(2) = xweek+2(2).

        "     gt_uebauf-spart = mara-spart.

        MOVE-CORRESPONDING aufk TO gt_uebauf.
        MOVE-CORRESPONDING afko TO gt_uebauf.
        MOVE-CORRESPONDING afpo TO gt_uebauf.
        gt_uebauf-gltri = afpo-ltrmi.
        GET RUN TIME FIELD gv_rt0.
        PERFORM erster_bezug.
        GET RUN TIME FIELD gv_rt1.
        gv_t_bezug = gv_t_bezug + ( gv_rt1 - gv_rt0 ) / 1000000.
        EXIT.
      ENDIF.
    ENDLOOP.

    gt_uebauf-werk = vbap-werks.

    IF aufk-auart IS INITIAL.
      CLEAR gt_uebauf-auart.
    ENDIF.
    gt_uebauf-posnr = ivbep-posnr.

    gt_uebauf-dispo = ''. "KOI


    gt_uebauf-standardwert = gt_uebauf-standardpreis * gt_uebauf-bmeng.




    "insert 24.5.24
    GET RUN TIME FIELD gv_rt0.
    PERFORM fill_fkdat USING ivbep-vbeln ivbep-posnr ivbep-bmeng CHANGING gt_uebauf.
    GET RUN TIME FIELD gv_rt1.
    gv_t_fkdat = gv_t_fkdat + ( gv_rt1 - gv_rt0 ) / 1000000.
    "end inser 24.5.24

    "KOI INSERT
    " Abrufen des Disponenten aus der MARC-Tabelle
    " war: SELECT SINGLE dispo FROM marc INTO gt_uebauf-dispo WHERE matnr = mara-matnr AND werks = p_werk
    READ TABLE gt_o_marc INTO ls_o_marc WITH TABLE KEY matnr = mara-matnr.
    IF sy-subrc = 0.
      gt_uebauf-dispo = ls_o_marc-dispo.
    ENDIF.

    " war: SELECT SINGLE * FROM makt WHERE matnr = mara-matnr AND spras = sy-langu (zweites Mal)
    READ TABLE gt_o_makt INTO makt WITH TABLE KEY matnr = mara-matnr.




    DATA: lv_search_pattern TYPE string,
          lv_pos            TYPE i.

    " war: SELECT a~dispo a~dispo_kz b~descr INTO CORRESPONDING FIELDS OF TABLE lt_result
    "      FROM zdispo_grp JOIN zdispo_spart, in jeder Zeile neu. LT_RESULT wird jetzt einmal
    "      in PREFETCH_OPT mit derselben Anweisung gefuellt.


    LOOP AT lt_result INTO DATA(wa_result)."alle


      FIND FIRST OCCURRENCE OF '*' IN wa_result-dispo_kz MATCH OFFSET lv_pos.
      IF sy-subrc = 0.
        " Alles vor dem Stern abschneiden und Suchmuster erstellen
        lv_search_pattern = wa_result-dispo_kz(lv_pos)  && '*'.
      ELSE.
        " Wenn kein Stern vorhanden ist, dann den gesamten Wert verwenden
        lv_search_pattern = wa_result-dispo_kz.
      ENDIF.

      IF gt_uebauf-dispo CP lv_search_pattern.
        gt_uebauf-dispo_DES = wa_result-descr.
      ENDIF.

    ENDLOOP.

    "KOI END

    " Anteil berechnen

    IF gt_uebauf-standardwert > 0 AND  gt_uebauf-netwr1 > 0 .
      gt_uebauf-anteil =  gt_uebauf-netwr1 /  gt_uebauf-standardwert."( gt_uebauf-standardpreis * gt_uebauf-bmeng ) / vbap-kpein.
    ENDIF.

    APPEND gt_uebauf.
  ENDLOOP.


  LOOP AT gt_uebauf INTO DATA(wa_uebauf) WHERE fkdat NOT IN s_fkdat.
    DELETE gt_uebauf.
  ENDLOOP.

  LOOP AT gt_uebauf INTO DATA(wa_uebauf2).
    IF wa_uebauf2-werk NE p_werk.
      DELETE gt_uebauf.
    ENDIF.
  ENDLOOP.

  "AND abgru IN p_abgru "KOI
  LOOP AT gt_uebauf INTO wa_uebauf2  WHERE abgru NOT   IN p_abgru.
    IF wa_uebauf2-abgru NE p_werk.
      DELETE gt_uebauf.
    ENDIF.
  ENDLOOP.


  LOOP AT gt_uebauf INTO  wa_uebauf2 WHERE dispo_des NOT IN p_descr.
    IF p_descr IS NOT INITIAL.
      DELETE gt_uebauf.
    ENDIF.
  ENDLOOP.

* KOI INSERT
  SELECT SINGLE * FROM mara WHERE matnr = vbap-matnr. "#EC CI_ALL_FIELDS_NEEDED




*
  " SORT gt_uebauf.
  " DELETE ADJACENT DUPLICATES FROM gt_uebauf.





  " sort gt_uebauf by vbeln posnr edatu.
  SORT gt_uebauf BY edatu vbeln posnr.
ENDFORM.                    " fill_uebauf

*&---------------------------------------------------------------------*
*&      Form  PREFETCH_OPT
*&---------------------------------------------------------------------*
* OPT 2026-10-12: liest alles, was FILL_UEBAUF bisher Zeile fuer Zeile
* gelesen hat, einmal fuer alle Einteilungen aus IVBEP. Bedingungen wie
* bisher; FOR ALL ENTRIES immer mit dem ganzen Schluessel in der
* Feldliste (sonst entfernt die Datenbank gleiche Zeilen) und nie mit
* leerer Treibertabelle.
*----------------------------------------------------------------------*
FORM prefetch_opt.
  TYPES: BEGIN OF lty_pos,
           vbeln TYPE vbep-vbeln,
           posnr TYPE vbep-posnr,
         END OF lty_pos,
         BEGIN OF lty_ein,
           vbeln TYPE vbep-vbeln,
           posnr TYPE vbep-posnr,
           etenr TYPE vbep-etenr,
         END OF lty_ein,
         BEGIN OF lty_matnr,
           matnr TYPE mara-matnr,
         END OF lty_matnr,
         BEGIN OF lty_kunnr,
           kunnr TYPE kna1-kunnr,
         END OF lty_kunnr,
         BEGIN OF lty_aufnr,
           aufnr TYPE afpo-aufnr,
         END OF lty_aufnr.
  DATA: lt_pos   TYPE STANDARD TABLE OF lty_pos,
        lt_ein   TYPE STANDARD TABLE OF lty_ein,
        lt_matnr TYPE STANDARD TABLE OF lty_matnr,
        lt_kunnr TYPE STANDARD TABLE OF lty_kunnr,
        lt_aufnr TYPE STANDARD TABLE OF lty_aufnr,
        lt_vbak  TYPE STANDARD TABLE OF vbak,
        lt_kna1  TYPE STANDARD TABLE OF kna1,
        lt_vbap  TYPE STANDARD TABLE OF vbap,
        lt_vbbe  TYPE STANDARD TABLE OF vbbe,
        lt_mara  TYPE STANDARD TABLE OF mara,
        lt_makt  TYPE STANDARD TABLE OF makt,
        lt_vbep0 TYPE STANDARD TABLE OF vbep,
        lt_vbkd  TYPE STANDARD TABLE OF ty_o_vbkd,
        lt_marc  TYPE STANDARD TABLE OF ty_o_marc,
        lt_vbfa  TYPE STANDARD TABLE OF ty_o_vbfa,
        lt_mbew  TYPE STANDARD TABLE OF ty_o_mbew,
        lt_afpo  TYPE STANDARD TABLE OF afpo,
        lt_afko  TYPE STANDARD TABLE OF afko,
        lt_aufk  TYPE STANDARD TABLE OF aufk,
        lt_aufm  TYPE STANDARD TABLE OF ty_o_aufm,
        lv_ts0   TYPE timestampl,
        lv_ts1   TYPE timestampl.

  GET TIME STAMP FIELD lv_ts0.

  CLEAR: gt_o_vbak, gt_o_kna1, gt_o_vbap, gt_o_vbbe, gt_o_mara, gt_o_makt,
         gt_o_vbep0, gt_o_vbkd, gt_o_marc, gt_o_vbfa, gt_o_mbew, gt_o_afpo,
         gt_o_afko, gt_o_aufk, gt_o_aufm, gt_o_bereich, gt_o_aufnr_gesehen,
         gt_o_flow_vbfa, gv_o_flow_vbeln, gv_o_flow_posnr, gv_o_flow_da, gt_o_flow_beleg,
         gv_n_mbew_mehrfach.

  " Disponentengruppen: dieselbe Anweisung wie bisher in der Schleife, jetzt einmal
  SELECT a~dispo a~dispo_kz b~descr
    INTO CORRESPONDING FIELDS OF TABLE lt_result
    FROM zdispo_grp AS a
    JOIN zdispo_spart AS b ON a~dispo = b~dispo.

  LOOP AT ivbep[] INTO DATA(ls_ep).
    APPEND VALUE #( vbeln = ls_ep-vbeln posnr = ls_ep-posnr ) TO lt_pos.
    APPEND VALUE #( vbeln = ls_ep-vbeln posnr = ls_ep-posnr etenr = ls_ep-etenr ) TO lt_ein.
  ENDLOOP.
  SORT lt_pos BY vbeln posnr.
  DELETE ADJACENT DUPLICATES FROM lt_pos COMPARING vbeln posnr.

  IF lt_pos IS NOT INITIAL.
    SELECT * FROM vbak INTO TABLE lt_vbak            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln.
    gt_o_vbak = lt_vbak.

    SELECT * FROM vbap INTO TABLE lt_vbap            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln
      AND   posnr = lt_pos-posnr
      AND   werks = p_werk.
    gt_o_vbap = lt_vbap.

    SELECT * FROM vbep INTO TABLE lt_vbep0           "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln
      AND   posnr = lt_pos-posnr
      AND   bmeng = 0.
    gt_o_vbep0 = lt_vbep0.

    SELECT vbeln posnr bstdk FROM vbkd INTO TABLE lt_vbkd
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln.
    gt_o_vbkd = lt_vbkd.

    SELECT vbelv posnv ruuid erdat FROM vbfa INTO TABLE lt_vbfa
      FOR ALL ENTRIES IN lt_pos
      WHERE vbelv   = lt_pos-vbeln
      AND   posnv   = lt_pos-posnr
      AND   vbtyp_n = 'M'.
    " letzte Zeile nach Primaerschluessel (RUUID) je Position behalten
    SORT lt_vbfa BY vbelv posnv ruuid DESCENDING.
    DELETE ADJACENT DUPLICATES FROM lt_vbfa COMPARING vbelv posnv.
    gt_o_vbfa = lt_vbfa.

    " Belege, die ueberhaupt einen Belegfluss haben (als Vorgaenger oder als Nachfolger).
    " Nur fuer sie kann RV_ORDER_FLOW_INFORMATION Fakturazeilen liefern, siehe FILL_FKDAT.
    DATA: lt_fluss TYPE STANDARD TABLE OF vbeln.
    SELECT DISTINCT vbelv FROM vbfa INTO TABLE lt_fluss
      FOR ALL ENTRIES IN lt_pos
      WHERE vbelv = lt_pos-vbeln.
    LOOP AT lt_fluss INTO DATA(lv_fluss).
      INSERT lv_fluss INTO TABLE gt_o_flow_beleg.
    ENDLOOP.
    CLEAR lt_fluss.
    SELECT DISTINCT vbeln FROM vbfa INTO TABLE lt_fluss
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln.
    LOOP AT lt_fluss INTO lv_fluss.
      INSERT lv_fluss INTO TABLE gt_o_flow_beleg.
    ENDLOOP.

    SELECT * FROM afpo INTO TABLE lt_afpo            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_pos
      WHERE kdauf = lt_pos-vbeln
      AND   kdpos = lt_pos-posnr.
    gt_o_afpo = lt_afpo.
  ENDIF.

  IF lt_ein IS NOT INITIAL.
    SELECT * FROM vbbe INTO TABLE lt_vbbe            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_ein
      WHERE vbeln = lt_ein-vbeln
      AND   posnr = lt_ein-posnr
      AND   etenr = lt_ein-etenr
      AND   werks = p_werk.
    gt_o_vbbe = lt_vbbe.
  ENDIF.

  LOOP AT lt_vbak INTO DATA(ls_vbak) WHERE kunnr IS NOT INITIAL.
    APPEND VALUE #( kunnr = ls_vbak-kunnr ) TO lt_kunnr.
  ENDLOOP.
  SORT lt_kunnr BY kunnr.
  DELETE ADJACENT DUPLICATES FROM lt_kunnr COMPARING kunnr.
  IF lt_kunnr IS NOT INITIAL.
    SELECT * FROM kna1 INTO TABLE lt_kna1            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_kunnr
      WHERE kunnr = lt_kunnr-kunnr.
    gt_o_kna1 = lt_kna1.
  ENDIF.

  LOOP AT lt_vbap INTO DATA(ls_vbap) WHERE matnr IS NOT INITIAL.
    APPEND VALUE #( matnr = ls_vbap-matnr ) TO lt_matnr.
  ENDLOOP.
  SORT lt_matnr BY matnr.
  DELETE ADJACENT DUPLICATES FROM lt_matnr COMPARING matnr.
  IF lt_matnr IS NOT INITIAL.
    SELECT * FROM mara INTO TABLE lt_mara            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_matnr
      WHERE matnr = lt_matnr-matnr.
    gt_o_mara = lt_mara.

    SELECT * FROM makt INTO TABLE lt_makt            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_matnr
      WHERE matnr = lt_matnr-matnr
      AND   spras = sy-langu.
    gt_o_makt = lt_makt.

    SELECT matnr dzeit dispo FROM marc INTO TABLE lt_marc
      FOR ALL ENTRIES IN lt_matnr
      WHERE matnr = lt_matnr-matnr
      AND   werks = p_werk.
    gt_o_marc = lt_marc.

    " Bewertungsbereich = Werk der Position; der VBAP-Puffer hat nur Werk p_werk
    SELECT matnr bwkey bwtar stprs FROM mbew INTO TABLE lt_mbew
      FOR ALL ENTRIES IN lt_matnr
      WHERE matnr = lt_matnr-matnr
      AND   bwkey = p_werk.
    SORT lt_mbew BY matnr bwkey bwtar.
    gv_n_mbew_mehrfach = lines( lt_mbew ).
    DELETE ADJACENT DUPLICATES FROM lt_mbew COMPARING matnr bwkey.
    " Anzahl Materialien mit mehr als einer MBEW-Zeile (getrennte Bewertung): dort war
    " SELECT SINGLE ohne BWTAR nicht eindeutig. 0 heisst: kein Unterschied moeglich.
    gv_n_mbew_mehrfach = gv_n_mbew_mehrfach - lines( lt_mbew ).
    gt_o_mbew = lt_mbew.
  ENDIF.

  LOOP AT lt_afpo INTO DATA(ls_afpo).
    APPEND VALUE #( aufnr = ls_afpo-aufnr ) TO lt_aufnr.
  ENDLOOP.
  SORT lt_aufnr BY aufnr.
  DELETE ADJACENT DUPLICATES FROM lt_aufnr COMPARING aufnr.
  IF lt_aufnr IS NOT INITIAL.
    SELECT * FROM afko INTO TABLE lt_afko            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_aufnr
      WHERE aufnr = lt_aufnr-aufnr.
    gt_o_afko = lt_afko.

    SELECT * FROM aufk INTO TABLE lt_aufk            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_aufnr
      WHERE aufnr = lt_aufnr-aufnr
      AND   werks = p_werk.
    gt_o_aufk = lt_aufk.

    " erster Bezug: bisher SELECT * FROM aufm ... ORDER BY bldat, erste Zeile = kleinstes BLDAT
    SELECT aufnr bldat FROM aufm INTO TABLE lt_aufm
      FOR ALL ENTRIES IN lt_aufnr
      WHERE aufnr = lt_aufnr-aufnr
      AND   werks = p_werk
      AND   shkzg = 'H'.
    SORT lt_aufm BY aufnr bldat.
    DELETE ADJACENT DUPLICATES FROM lt_aufm COMPARING aufnr.
    gt_o_aufm = lt_aufm.
  ENDIF.

  GET TIME STAMP FIELD lv_ts1.
  gv_t_prefetch = cl_abap_tstmp=>subtract( tstmp1 = lv_ts1 tstmp2 = lv_ts0 ).
ENDFORM.                    " prefetch_opt
