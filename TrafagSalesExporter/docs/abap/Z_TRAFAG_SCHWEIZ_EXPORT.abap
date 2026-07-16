*&---------------------------------------------------------------------*
*& Report  Z_TRAFAG_SCHWEIZ_EXPORT
*&---------------------------------------------------------------------*
*& Zweck
*&   Liest SD-Faktura-Positionen fuer Buchungskreise 1100 (CH) und
*&   1200 (AT) und schreibt sie per UPSERT in Tabelle ZSCHWEIZ, damit
*&   sie zusammen mit den Datenquellen der anderen Subsidiaries ueber
*&   den OData-Service in den TrafagSalesExporter / Azure Datalake
*&   einlaufen koennen.
*&
*& Voraussetzung
*&   In Tabelle ZSCHWEIZ muessen die Felder NETWR_DC, TAX_DC, NETWR_HC,
*&   TAX_HC vom Typ CURR (z.B. CURR 15,2) sein - NICHT CUKY. Pruefe in
*&   SE11 vor Ausfuehrung. WAERK / HWAER referenzieren als CUKY.
*&
*&   NEU 2026-07-16: Feld WAVWR_DC (Typ CURR, gleiche Laenge/Dezimalen
*&   wie NETWR_DC) muss vor dem ersten Lauf dieser Version in SE11 in
*&   ZSCHWEIZ ergaenzt werden (additiv, keine bestehende Spalte
*&   aendern). Quelle ist VBRP-WAVWR (Kostenwert Warenausgang, zum
*&   Zeitpunkt der Fakturierung eingefroren, in Belegwaehrung WAERK) -
*&   Basis fuer die CH/AT-Kostenbasis der Gruppenmarge im
*&   TrafagSalesExporter, Ersatz fuer den bisherigen MBEW-STPRS-Weg
*&   (mbewSet), der performanceseitig haengt. Nach diesem Deploy muss
*&   der Report einmal fuer den vollen historischen Bestand (relevante
*&   s_gjahr-Jahre) erneut laufen, sonst bleibt WAVWR_DC fuer bereits
*&   bestehende ZSCHWEIZ-Zeilen leer/0 (Report macht UPSERT, ergaenzt
*&   also nur bei erneutem Lauf ueber dieselben Zeilen).
*&
*&   NEU 2026-07-16 (Teil 2): Feld STPRS_HC (Typ CURR, Vorschlag
*&   CURR 15,4 wegen Nachkommastellen nach PEINH-Division) zusaetzlich
*&   in SE11 ergaenzen. Quelle ist MBEW-STPRS/PEINH (aktueller
*&   Standardpreis je Stueck, Hauswaehrung des Bewertungskreises) --
*&   gelesen per direktem ABAP-Join (schnell, indiziert ueber
*&   MATNR+BWKEY), NICHT ueber den haengenden mbewSet-OData-Weg.
*&   Grund fuer beide Felder nebeneinander: WAVWR_DC ist NUR gesetzt,
*&   wenn eine echte Warenausgangsbuchung mit der Fakturaposition
*&   verknuepft ist (siehe Live-Stichprobe 2026-07-16: bei
*&   auftragsbezogener Fakturierung ohne Lieferbezug bleibt WAVWR
*&   trotz gepflegtem STPRS bei 0). STPRS_HC ist als zusaetzliches,
*&   separates Feld gedacht (Diagnose/Fallback-Kandidat), NICHT als
*&   Ersatz fuer WAVWR_DC als fuehrende Kostenbasis der Gruppenmarge
*&   (siehe docs/FINANCE_VBRP_WAVWR_SPEZ_2026-07-16.md Abschnitt 4 zur
*&   fachlichen Begruendung WAVWR vs. STPRS).
*&
*& Stil
*&   - Modern OO mit lokaler Klasse lcl_export
*&   - Mittelweg: WRITE-basierte Summary, einfache Fehlerbehandlung
*&   - Keine FORMs, kein TYPE any
*&---------------------------------------------------------------------*
REPORT z_trafag_schweiz_export.

TABLES: vbrk.

*----------------------------------------------------------------------*
* Hilfsvariable nur fuer die Typisierung von SELECT-OPTIONS s_gjahr.
* So bekommt die Range-Tabelle den Typ gjahr (NUMC 4) und ist
* typkompatibel zum Methodenparameter it_gjahr TYPE RANGE OF gjahr.
*----------------------------------------------------------------------*
DATA gv_gjahr TYPE gjahr.

*----------------------------------------------------------------------*
* Selektionsbild
*----------------------------------------------------------------------*
SELECTION-SCREEN BEGIN OF BLOCK b01 WITH FRAME TITLE TEXT-t01.
  SELECT-OPTIONS: s_gjahr FOR gv_gjahr,      " Geschaeftsjahr-Range (NUMC 4)
                  s_bukrs FOR vbrk-bukrs,
                  s_fkart FOR vbrk-fkart,
                  s_vbeln FOR vbrk-vbeln.
  PARAMETERS: p_test  AS CHECKBOX DEFAULT abap_true,
              p_pkg   TYPE i      DEFAULT 1000.
SELECTION-SCREEN END OF BLOCK b01.

*----------------------------------------------------------------------*
* Lokale Typen fuer Selektionstabellen
*----------------------------------------------------------------------*
TYPES: ty_r_gjahr TYPE RANGE OF gjahr,
       ty_r_fkdat TYPE RANGE OF fkdat.

*----------------------------------------------------------------------*
* Lokale Klasse
*----------------------------------------------------------------------*
CLASS lcl_export DEFINITION FINAL.
  PUBLIC SECTION.
    TYPES:
      BEGIN OF ty_billing,
        bukrs         TYPE vbrk-bukrs,
        vbeln         TYPE vbrk-vbeln,
        fkdat         TYPE vbrk-fkdat,
        fkart         TYPE vbrk-fkart,
        vbtyp         TYPE vbrk-vbtyp,
        waerk         TYPE vbrk-waerk,
        kurrf         TYPE vbrk-kurrf,
        kunag         TYPE vbrk-kunag,
        erdat         TYPE vbrk-erdat,
        aedat         TYPE vbrk-aedat,
        posnr         TYPE vbrp-posnr,
        matnr         TYPE vbrp-matnr,
        arktx         TYPE vbrp-arktx,
        prodh         TYPE vbrp-prodh,
        fkimg         TYPE vbrp-fkimg,
        vrkme         TYPE vbrp-vrkme,
        netwr         TYPE vbrp-netwr,
        mwsbp         TYPE vbrp-mwsbp,
        wavwr         TYPE vbrp-wavwr,        " NEU 2026-07-16: Kostenwert Warenausgang
        stprs         TYPE mbew-stprs,        " NEU 2026-07-16 Teil 2: aktueller Standardpreis
        peinh         TYPE mbew-peinh,        " NEU 2026-07-16 Teil 2: Preiseinheit zu stprs
        customer_land TYPE kna1-land1,
        name1         TYPE kna1-name1,
        hwaer         TYPE t001-waers,
      END OF ty_billing,
      tt_billing  TYPE STANDARD TABLE OF ty_billing WITH EMPTY KEY,
      tt_zschweiz TYPE STANDARD TABLE OF zschweiz   WITH EMPTY KEY.

    METHODS:
      constructor
        IMPORTING it_gjahr TYPE ty_r_gjahr
                  iv_test  TYPE abap_bool
                  iv_pkg   TYPE i,
      run.

  PRIVATE SECTION.
    DATA:
      mt_gjahr    TYPE ty_r_gjahr,
      mt_fkdat    TYPE ty_r_fkdat,
      mv_test     TYPE abap_bool,
      mv_pkg      TYPE i,
      mt_billing  TYPE tt_billing,
      mt_zschweiz TYPE tt_zschweiz.

    METHODS:
      build_fkdat_range,
      read_billing,
      map_to_zschweiz,
      persist,
      write_summary,
      determine_sign
        IMPORTING is_billing       TYPE ty_billing
        RETURNING VALUE(rv_sign)   TYPE i,
      classify_party
        IMPORTING iv_name1         TYPE name1_gp
        RETURNING VALUE(rv_party)  TYPE char10,
      derive_country
        IMPORTING iv_bukrs         TYPE bukrs
                  iv_fallback      TYPE land1
        RETURNING VALUE(rv_land1)  TYPE land1,
      derive_tsc
        IMPORTING iv_bukrs         TYPE bukrs
        RETURNING VALUE(rv_tsc)    TYPE char10,
      to_house_currency
        IMPORTING iv_amount        TYPE p
                  iv_from          TYPE waerk
                  iv_to            TYPE waers
                  iv_date          TYPE fkdat
                  iv_kurrf         TYPE kurrf
       RETURNING VALUE(rv_amount) TYPE netwr.
ENDCLASS.

*----------------------------------------------------------------------*
CLASS lcl_export IMPLEMENTATION.

  METHOD constructor.
    mt_gjahr = it_gjahr.
    mv_test  = iv_test.
    mv_pkg   = COND #( WHEN iv_pkg > 0 THEN iv_pkg ELSE 1000 ).
    build_fkdat_range( ).
  ENDMETHOD.

  METHOD build_fkdat_range.
*   Konvertiert die GJAHR-Range in eine FKDAT-Range. Es werden alle
*   Jahre aus mt_gjahr explizit aufgeloest (auch Bereiche und Listen),
*   damit die SELECT-Anweisung weiter sauber auf FKDAT filtert.
    DATA: lv_year      TYPE i,
          lv_year_low  TYPE i,
          lv_year_high TYPE i.

    CLEAR mt_fkdat.

    IF mt_gjahr IS INITIAL.
*     Fallback: aktuelles Jahr
      lv_year_low  = sy-datum(4).
      lv_year_high = lv_year_low.
      APPEND VALUE #( sign   = 'I'
                      option = 'BT'
                      low    = |{ lv_year_low }0101|
                      high   = |{ lv_year_high }1231| ) TO mt_fkdat.
      RETURN.
    ENDIF.

    LOOP AT mt_gjahr ASSIGNING FIELD-SYMBOL(<ls_g>).
      CASE <ls_g>-option.
        WHEN 'EQ'.
          APPEND VALUE #( sign   = <ls_g>-sign
                          option = 'BT'
                          low    = |{ <ls_g>-low }0101|
                          high   = |{ <ls_g>-low }1231| ) TO mt_fkdat.

        WHEN 'BT'.
          APPEND VALUE #( sign   = <ls_g>-sign
                          option = 'BT'
                          low    = |{ <ls_g>-low }0101|
                          high   = |{ <ls_g>-high }1231| ) TO mt_fkdat.

        WHEN 'GE'.
          APPEND VALUE #( sign   = <ls_g>-sign
                          option = 'GE'
                          low    = |{ <ls_g>-low }0101| ) TO mt_fkdat.

        WHEN 'GT'.
*         GT Jahr -> ab 01.01. des Folgejahres
          lv_year = <ls_g>-low + 1.
          APPEND VALUE #( sign   = <ls_g>-sign
                          option = 'GE'
                          low    = |{ lv_year }0101| ) TO mt_fkdat.

        WHEN 'LE'.
          APPEND VALUE #( sign   = <ls_g>-sign
                          option = 'LE'
                          low    = |{ <ls_g>-low }1231| ) TO mt_fkdat.

        WHEN 'LT'.
*         LT Jahr -> bis 31.12. des Vorjahres
          lv_year = <ls_g>-low - 1.
          APPEND VALUE #( sign   = <ls_g>-sign
                          option = 'LE'
                          low    = |{ lv_year }1231| ) TO mt_fkdat.

        WHEN 'NE'.
          APPEND VALUE #( sign   = <ls_g>-sign
                          option = 'NB'
                          low    = |{ <ls_g>-low }0101|
                          high   = |{ <ls_g>-low }1231| ) TO mt_fkdat.

        WHEN OTHERS.
*         Unbekannte Option -> als EQ behandeln
          APPEND VALUE #( sign   = <ls_g>-sign
                          option = 'BT'
                          low    = |{ <ls_g>-low }0101|
                          high   = |{ <ls_g>-low }1231| ) TO mt_fkdat.
      ENDCASE.
    ENDLOOP.
  ENDMETHOD.

  METHOD run.
    read_billing( ).
    IF mt_billing IS INITIAL.
      WRITE: / 'Keine Fakturapositionen gefunden.'.
      RETURN.
    ENDIF.
    map_to_zschweiz( ).
    persist( ).
    write_summary( ).
  ENDMETHOD.

  METHOD read_billing.
    SELECT
        h~bukrs,
        h~vbeln,
        h~fkdat,
        h~fkart,
        h~vbtyp,
        h~waerk,
        h~kurrf,
        h~kunag,
        h~erdat,
        h~aedat,
        i~posnr,
        i~matnr,
        i~arktx,
        i~prodh,
        i~fkimg,
        i~vrkme,
        i~netwr,
        i~mwsbp,
        i~wavwr,                              " NEU 2026-07-16
        m~stprs,                              " NEU 2026-07-16 Teil 2
        m~peinh,                              " NEU 2026-07-16 Teil 2
        k~land1 AS customer_land,
        k~name1,
        c~waers AS hwaer
      FROM vbrk AS h
      INNER JOIN vbrp AS i
        ON i~vbeln = h~vbeln
      LEFT OUTER JOIN kna1 AS k
        ON k~kunnr = h~kunag
      LEFT OUTER JOIN t001 AS c
        ON c~bukrs = h~bukrs
*     NEU 2026-07-16 Teil 2: Bewertungskreis = Buchungskreis fuer
*     1100/1200, bestaetigt per T001K im ABAP-Analysebericht vom
*     2026-07-14. MBEW ist je Material UND Bewertungskreis
*     verschluesselt - falscher Join wuerde CH-Zeilen den
*     oesterreichischen Preis geben (und umgekehrt).
      LEFT OUTER JOIN mbew AS m
        ON m~matnr = i~matnr
       AND m~bwkey = h~bukrs
      WHERE h~bukrs IN @s_bukrs
        AND h~fkdat IN @mt_fkdat
        AND h~vbeln IN @s_vbeln
        AND h~fkart IN @s_fkart
        AND h~fksto = @space
      INTO TABLE @mt_billing.

    WRITE: / 'Gelesene Fakturapositionen:', lines( mt_billing ).
  ENDMETHOD.

  METHOD map_to_zschweiz.
    DATA: lv_timestamp TYPE timestampl,
          lv_gjahr     TYPE gjahr.

    GET TIME STAMP FIELD lv_timestamp.

    LOOP AT mt_billing ASSIGNING FIELD-SYMBOL(<ls_b>).
      DATA(lv_sign) = determine_sign( <ls_b> ).

      DATA(lv_netwr_hc) = to_house_currency(
        iv_amount = <ls_b>-netwr
        iv_from   = <ls_b>-waerk
        iv_to     = <ls_b>-hwaer
        iv_date   = <ls_b>-fkdat
        iv_kurrf  = <ls_b>-kurrf ).

      DATA(lv_tax_hc) = to_house_currency(
        iv_amount = <ls_b>-mwsbp
        iv_from   = <ls_b>-waerk
        iv_to     = <ls_b>-hwaer
        iv_date   = <ls_b>-fkdat
        iv_kurrf  = <ls_b>-kurrf ).

*     GJAHR pro Zeile aus FKDAT ableiten -> erlaubt mehrere Jahre
      lv_gjahr = <ls_b>-fkdat(4).

      APPEND VALUE zschweiz(
        mandt         = sy-mandt
        bukrs         = <ls_b>-bukrs
        gjahr         = lv_gjahr
        vbeln         = <ls_b>-vbeln
        posnr         = <ls_b>-posnr
        land1         = derive_country( iv_bukrs    = <ls_b>-bukrs
                                        iv_fallback = <ls_b>-customer_land )
        customer_land = <ls_b>-customer_land
        tsc           = derive_tsc( <ls_b>-bukrs )
        fkdat         = <ls_b>-fkdat
        fkart         = <ls_b>-fkart
        vbtyp         = <ls_b>-vbtyp
        kunnr         = <ls_b>-kunag
        name1         = <ls_b>-name1
        matnr         = <ls_b>-matnr
        arktx         = <ls_b>-arktx
        prodh         = <ls_b>-prodh
        fkimg         = <ls_b>-fkimg * lv_sign
        vrkme         = <ls_b>-vrkme
        waerk         = <ls_b>-waerk
        hwaer         = <ls_b>-hwaer
        netwr_dc      = <ls_b>-netwr * lv_sign
        tax_dc        = <ls_b>-mwsbp * lv_sign
        netwr_hc      = lv_netwr_hc  * lv_sign
        tax_hc        = lv_tax_hc    * lv_sign
        wavwr_dc      = <ls_b>-wavwr * lv_sign  " NEU 2026-07-16: gleiches Vorzeichen wie netwr_dc/tax_dc,
                                                 " sonst zeigt die Kostenbasis bei Gutschriften das falsche
                                                 " Vorzeichen, waehrend der Umsatz schon negativ ist.
        stprs_hc      = COND #( WHEN <ls_b>-peinh > 0 THEN <ls_b>-stprs / <ls_b>-peinh ELSE <ls_b>-stprs )
                                                 " NEU 2026-07-16 Teil 2: Stueckpreis, PEINH-Falle beachtet
                                                 " (STPRS gilt je PEINH Stueck). BEWUSST OHNE lv_sign: das ist
                                                 " ein aktueller Stueckpreis, keine Zeilensumme - Vorzeichen
                                                 " bei Gutschriften wird App-seitig ueber Menge/Umsatz
                                                 " hergeleitet, wie beim bisherigen MBEW-STPRS-Weg auch schon.
        kurrf         = <ls_b>-kurrf
        is_credit     = COND #( WHEN lv_sign < 0 THEN abap_true ELSE abap_false )
        party_class   = classify_party( <ls_b>-name1 )
        erdat_src     = <ls_b>-erdat
        aedat_src     = COND #( WHEN <ls_b>-aedat IS NOT INITIAL THEN <ls_b>-aedat ELSE sy-datum )
        created_at    = lv_timestamp
        changed_at    = lv_timestamp
        created_by    = sy-uname
        changed_by    = sy-uname
      ) TO mt_zschweiz.
    ENDLOOP.

    WRITE: / 'Aufbereitete ZSCHWEIZ-Zeilen:', lines( mt_zschweiz ).
  ENDMETHOD.

  METHOD determine_sign.
    rv_sign = 1.
    IF is_billing-vbtyp = 'O'
       OR is_billing-vbtyp = 'N'
       OR is_billing-fkart CP 'G*'
       OR is_billing-fkart CP 'S*'.
      rv_sign = -1.
    ENDIF.
  ENDMETHOD.

  METHOD classify_party.
    DATA(lv_name) = to_upper( iv_name1 ).
    IF lv_name CS 'TRAFAG'
       OR lv_name CS 'MAGNETIC SENSE'
       OR lv_name CS 'MAGNETS SENSE'
       OR lv_name CS 'GESELLSCHAFT FUER SENSORIK'
       OR lv_name CS 'GESELLSCHAFT FUR SENSORIK'.
      rv_party = '2ND'.
    ELSE.
      rv_party = '3RD'.
    ENDIF.
  ENDMETHOD.

  METHOD derive_country.
    rv_land1 = SWITCH #( iv_bukrs
                          WHEN '1100' THEN 'CH'
                          WHEN '1200' THEN 'AT'
                          ELSE iv_fallback ).
  ENDMETHOD.

  METHOD derive_tsc.
    rv_tsc = SWITCH #( iv_bukrs
                        WHEN '1100' THEN 'TRCH'
                        WHEN '1200' THEN 'TRAT'
                        ELSE CONV char10( iv_bukrs ) ).
  ENDMETHOD.

  METHOD to_house_currency.
    DATA: lv_local TYPE bapicurr-bapicurr.

    IF iv_from = iv_to OR iv_from IS INITIAL OR iv_to IS INITIAL.
      rv_amount = iv_amount.
      RETURN.
    ENDIF.

    CALL FUNCTION 'CONVERT_TO_LOCAL_CURRENCY'
      EXPORTING
        date             = iv_date
        foreign_amount   = iv_amount
        foreign_currency = iv_from
        local_currency   = iv_to
        rate             = iv_kurrf
      IMPORTING
        local_amount     = lv_local
      EXCEPTIONS
        no_rate_found    = 1
        overflow         = 2
        no_factors_found = 3
        no_spread_found  = 4
        derived_2_times  = 5
        OTHERS           = 6.

    IF sy-subrc = 0.
      rv_amount = lv_local.
    ELSEIF iv_kurrf IS NOT INITIAL.
      rv_amount = iv_amount * iv_kurrf.
    ELSE.
      rv_amount = 0.
    ENDIF.
  ENDMETHOD.

  METHOD persist.
    IF mv_test = abap_true.
      WRITE: / 'Testlauf aktiv: keine Daten in ZSCHWEIZ geschrieben.'.
      RETURN.
    ENDIF.

    DATA: lv_from   TYPE i VALUE 1,
          lv_to     TYPE i,
          lv_total  TYPE i,
          lt_chunk  TYPE tt_zschweiz.

    lv_total = lines( mt_zschweiz ).

    WHILE lv_from <= lv_total.
      lv_to = lv_from + mv_pkg - 1.
      IF lv_to > lv_total.
        lv_to = lv_total.
      ENDIF.

      CLEAR lt_chunk.
      LOOP AT mt_zschweiz INTO DATA(ls_row) FROM lv_from TO lv_to.
        APPEND ls_row TO lt_chunk.
      ENDLOOP.

      MODIFY zschweiz FROM TABLE lt_chunk.
      IF sy-subrc <> 0.
        ROLLBACK WORK.
        MESSAGE |ZSCHWEIZ Upsert fehlgeschlagen bei Paket { lv_from }-{ lv_to }| TYPE 'E'.
        RETURN.
      ENDIF.
      COMMIT WORK AND WAIT.

      WRITE: / |Paket geschrieben: { lv_from } - { lv_to } ({ lines( lt_chunk ) } Zeilen)|.
      lv_from = lv_to + 1.
    ENDWHILE.

    WRITE: / 'ZSCHWEIZ Upsert abgeschlossen. Gesamtzeilen:', lv_total.
  ENDMETHOD.

  METHOD write_summary.
    TYPES: BEGIN OF ty_total,
             gjahr    TYPE gjahr,
             land1    TYPE land1,
             hwaer    TYPE waers,
             netwr_hc TYPE p LENGTH 16 DECIMALS 2,
             tax_hc   TYPE p LENGTH 16 DECIMALS 2,
             rows     TYPE i,
           END OF ty_total.

    DATA lt_totals TYPE HASHED TABLE OF ty_total
                   WITH UNIQUE KEY gjahr land1 hwaer.

    LOOP AT mt_zschweiz ASSIGNING FIELD-SYMBOL(<ls_z>).
      ASSIGN lt_totals[ gjahr = <ls_z>-gjahr
                        land1 = <ls_z>-land1
                        hwaer = <ls_z>-hwaer ] TO FIELD-SYMBOL(<ls_t>).
      IF sy-subrc <> 0.
        INSERT VALUE #( gjahr = <ls_z>-gjahr
                        land1 = <ls_z>-land1
                        hwaer = <ls_z>-hwaer )
          INTO TABLE lt_totals ASSIGNING <ls_t>.
      ENDIF.
      <ls_t>-netwr_hc = <ls_t>-netwr_hc + <ls_z>-netwr_hc.
      <ls_t>-tax_hc   = <ls_t>-tax_hc   + <ls_z>-tax_hc.
      <ls_t>-rows     = <ls_t>-rows + 1.
    ENDLOOP.

    SKIP.
    WRITE: / 'Summen nach Jahr / Land / Hauswaehrung'.
    WRITE: / sy-uline(90).
    LOOP AT lt_totals ASSIGNING FIELD-SYMBOL(<ls_s>).
      WRITE: / <ls_s>-gjahr,
               <ls_s>-land1,
               <ls_s>-hwaer,
               'Netto:',  <ls_s>-netwr_hc,
               'Steuer:', <ls_s>-tax_hc,
               'Zeilen:', <ls_s>-rows.
    ENDLOOP.
  ENDMETHOD.

ENDCLASS.

*----------------------------------------------------------------------*
* Defaults fuer s_bukrs und s_gjahr nur wenn User nichts eintraegt
*----------------------------------------------------------------------*
INITIALIZATION.
  IF s_gjahr[] IS INITIAL.
    s_gjahr = VALUE #( sign   = 'I'
                       option = 'EQ'
                       low    = sy-datum(4) ).
    APPEND s_gjahr.
  ENDIF.

AT SELECTION-SCREEN OUTPUT.
  IF s_bukrs[] IS INITIAL.
    s_bukrs = VALUE #( sign = 'I' option = 'EQ' low = '1100' ).
    APPEND s_bukrs.
    s_bukrs-low = '1200'.
    APPEND s_bukrs.
  ENDIF.

*----------------------------------------------------------------------*
START-OF-SELECTION.
  DATA(lo_export) = NEW lcl_export(
    it_gjahr = s_gjahr[]
    iv_test  = p_test
    iv_pkg   = p_pkg ).
  lo_export->run( ).
