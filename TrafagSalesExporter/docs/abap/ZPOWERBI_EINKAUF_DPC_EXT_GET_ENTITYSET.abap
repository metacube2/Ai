*&---------------------------------------------------------------------*
*& DPC_EXT: Daten fuer das EntitySet FinanzJournalSet
*& Klasse : ZCL_ZPOWERBI_EINKAUF_DPC_EXT
*& Methode: /IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET  (redefiniert)
*& Stand  : 2026-09-10
*&
*& ACHTUNG, das ist die GENERISCHE Methode. Ueber sie laufen ALLE rund 25
*& EntitySets dieses Service. Deshalb behandelt der CASE ausschliesslich
*& FinanzJournalSet und reicht jeden anderen Aufruf unveraendert an
*& super-> durch. Wer den WHEN OTHERS-Zweig entfernt oder einen Parameter
*& vergisst, legt den ganzen Service lahm.
*&
*& Die Signatur ist am 2026-09-10 aus /IWBEP/IF_MGW_APPL_SRV_RUNTIMEiu
*& gelesen und nicht geraten.
*&
*& Fachliche Vorgabe Andreas, 2026-09-10: Die Schweiz hat ueber 5 Mio
*& Zeitbuchungszeilen je Jahr. Sie laufen als Belegart CO und bleiben
*& hier grundsaetzlich draussen. Fuer den ersten Entwurf fragt der Client
*& ohnehin nur Buchungskreis 1200 ab.
*&---------------------------------------------------------------------*

METHOD /iwbep/if_mgw_appl_srv_runtime~get_entityset.

  DATA: lt_out   TYPE STANDARD TABLE OF zstr_fin_journal,
        ls_out   TYPE zstr_fin_journal,
        lt_bukrs TYPE RANGE OF bkpf-bukrs,
        lt_gjahr TYPE RANGE OF bkpf-gjahr,
        lt_budat TYPE RANGE OF bkpf-budat,
        lt_blart TYPE RANGE OF bkpf-blart,
        lt_monat TYPE RANGE OF bkpf-monat,
        ls_r_bukrs LIKE LINE OF lt_bukrs,
        ls_r_gjahr LIKE LINE OF lt_gjahr,
        ls_r_budat LIKE LINE OF lt_budat,
        ls_r_blart LIKE LINE OF lt_blart,
        ls_r_monat LIKE LINE OF lt_monat,
        lv_budat_lo TYPE bkpf-budat,
        lv_budat_hi TYPE bkpf-budat,
        lv_tage  TYPE i,
        lv_max   TYPE i,
        lv_skip  TYPE i,
        lv_zeile TYPE i.

  FIELD-SYMBOLS: <ls_filter> LIKE LINE OF it_filter_select_options,
                 <ls_option> TYPE /iwbep/s_cod_select_option.

* HR-Kennzahlen, docs/abap/ZHR_KPI_DPC_GET_ENTITYSET_ADD.abap

  IF iv_entity_set_name = 'HrKpiSet'.

    DATA: lt_hr_out   TYPE STANDARD TABLE OF zstr_hr_kpi,
          ls_hr_out   TYPE zstr_hr_kpi,
          lt_hr_0001  TYPE STANDARD TABLE OF pa0001,
          ls_hr_0001  TYPE pa0001,
          lt_hr_0002  TYPE SORTED TABLE OF pa0002 WITH NON-UNIQUE KEY pernr,
          ls_hr_0002  TYPE pa0002,
          lt_hr_0007  TYPE SORTED TABLE OF pa0007 WITH NON-UNIQUE KEY pernr,
          ls_hr_0007  TYPE pa0007,
          lt_hr_2001  TYPE STANDARD TABLE OF pa2001,
          ls_hr_2001  TYPE pa2001,
          lv_hr_gjahr TYPE gjahr,
          lv_hr_monat TYPE monat,
          lv_hr_von   TYPE datum,
          lv_hr_bis   TYPE datum,
          lv_hr_avon  TYPE datum,
          lv_hr_abis  TYPE datum,
          lv_hr_tage  TYPE p LENGTH 7 DECIMALS 2,
          lv_hr_skip  TYPE i,
          lv_hr_max   TYPE i,
          lv_hr_zeile TYPE i.

    FIELD-SYMBOLS: <ls_hr_filter> LIKE LINE OF it_filter_select_options,
                   <ls_hr_option> TYPE /iwbep/s_cod_select_option.

*   Vorgabe: laufender Monat.
    lv_hr_gjahr = sy-datum(4).
    lv_hr_monat = sy-datum+4(2).

    LOOP AT it_filter_select_options ASSIGNING <ls_hr_filter>.
      LOOP AT <ls_hr_filter>-select_options ASSIGNING <ls_hr_option>
           WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_hr_filter>-property ).
          WHEN 'GJAHR'.
            lv_hr_gjahr = <ls_hr_option>-low.
          WHEN 'MONAT'.
            lv_hr_monat = <ls_hr_option>-low.
          WHEN OTHERS.  " bewusst ignoriert statt zu raten
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

    CONCATENATE lv_hr_gjahr lv_hr_monat '01' INTO lv_hr_von.
    CALL FUNCTION 'RP_LAST_DAY_OF_MONTHS'
      EXPORTING
        day_in            = lv_hr_von
      IMPORTING
        last_day_of_month = lv_hr_bis
      EXCEPTIONS
        day_in_no_date    = 1
        OTHERS            = 2.
    IF sy-subrc <> 0.
*     Ungueltiger Monat im Filter: leere Antwort statt Kurzdump.
      copy_data_to_ref( EXPORTING is_data = lt_hr_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.

*   Organisatorische Zuordnung zum Stichtag Monatsende.
    SELECT * FROM pa0001 INTO TABLE lt_hr_0001
      WHERE begda <= lv_hr_bis
        AND endda >= lv_hr_bis.

    IF lt_hr_0001 IS NOT INITIAL.
      SELECT * FROM pa0002 INTO TABLE lt_hr_0002
        FOR ALL ENTRIES IN lt_hr_0001
        WHERE pernr = lt_hr_0001-pernr
          AND begda <= lv_hr_bis
          AND endda >= lv_hr_bis.

      SELECT * FROM pa0007 INTO TABLE lt_hr_0007
        FOR ALL ENTRIES IN lt_hr_0001
        WHERE pernr = lt_hr_0001-pernr
          AND begda <= lv_hr_bis
          AND endda >= lv_hr_bis.

*     Nur die Unfallarten; Krankheit und Ferien liest das Cockpit aus Rexx.
      SELECT * FROM pa2001 INTO TABLE lt_hr_2001
        FOR ALL ENTRIES IN lt_hr_0001
        WHERE pernr = lt_hr_0001-pernr
          AND begda <= lv_hr_bis
          AND endda >= lv_hr_von
          AND awart IN ('0280','0290','0300','0310','0350','0360').
    ENDIF.

    LOOP AT lt_hr_0001 INTO ls_hr_0001.
      CLEAR ls_hr_out.
      ls_hr_out-pernr = ls_hr_0001-pernr.
      ls_hr_out-gjahr = lv_hr_gjahr.
      ls_hr_out-monat = lv_hr_monat.
      ls_hr_out-bukrs = ls_hr_0001-bukrs.
      ls_hr_out-werks = ls_hr_0001-werks.
      ls_hr_out-btrtl = ls_hr_0001-btrtl.
      ls_hr_out-persg = ls_hr_0001-persg.
      ls_hr_out-persk = ls_hr_0001-persk.
      ls_hr_out-plans = ls_hr_0001-plans.
      ls_hr_out-stell = ls_hr_0001-stell.
      ls_hr_out-abkrs = ls_hr_0001-abkrs.

      READ TABLE lt_hr_0002 INTO ls_hr_0002 WITH TABLE KEY pernr = ls_hr_0001-pernr.
      IF sy-subrc = 0.
        ls_hr_out-gesch = ls_hr_0002-gesch.
      ENDIF.

      READ TABLE lt_hr_0007 INTO ls_hr_0007 WITH TABLE KEY pernr = ls_hr_0001-pernr.
      IF sy-subrc = 0.
        ls_hr_out-teilk = ls_hr_0007-teilk.
        ls_hr_out-empct = ls_hr_0007-empct.
      ENDIF.

*     Unfalltage wie im Report: Kalendertage, auf den Monat beschnitten.
      LOOP AT lt_hr_2001 INTO ls_hr_2001 WHERE pernr = ls_hr_0001-pernr.
        lv_hr_avon = ls_hr_2001-begda.
        lv_hr_abis = ls_hr_2001-endda.
        IF lv_hr_avon < lv_hr_von.
          lv_hr_avon = lv_hr_von.
        ENDIF.
        IF lv_hr_abis > lv_hr_bis.
          lv_hr_abis = lv_hr_bis.
        ENDIF.
        lv_hr_tage = lv_hr_abis - lv_hr_avon + 1.
        IF lv_hr_tage < 0.
          lv_hr_tage = 0.
        ENDIF.
        CASE ls_hr_2001-awart.
          WHEN '0350' OR '0360'.
            ls_hr_out-nbu_tage = ls_hr_out-nbu_tage + lv_hr_tage.
          WHEN OTHERS.  " 0280, 0290, 0300, 0310
            ls_hr_out-bu_tage = ls_hr_out-bu_tage + lv_hr_tage.
        ENDCASE.
      ENDLOOP.

      APPEND ls_hr_out TO lt_hr_out.
    ENDLOOP.

    SORT lt_hr_out BY pernr.
*   PA0001 kann zum Stichtag mehr als einen Satz je Person haben (Splits);
*   der Leser erwartet eine Zeile je Person.
    DELETE ADJACENT DUPLICATES FROM lt_hr_out COMPARING pernr.

    IF is_paging-skip > 0.
      lv_hr_skip = is_paging-skip.
      DELETE lt_hr_out TO lv_hr_skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_hr_max = is_paging-top.
      DESCRIBE TABLE lt_hr_out LINES lv_hr_zeile.
      IF lv_hr_zeile > lv_hr_max.
        lv_hr_max = lv_hr_max + 1.
        DELETE lt_hr_out FROM lv_hr_max.
      ENDIF.
    ENDIF.

    copy_data_to_ref( EXPORTING is_data = lt_hr_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

* HR-Abwesenheiten, docs/abap/ZHR_ABSENZ_DPC_GET_ENTITYSET_ADD.abap

  IF iv_entity_set_name = 'HrAbsenzSet'.

    DATA: lt_ha_out   TYPE STANDARD TABLE OF zstr_hr_absenz,
          ls_ha_out   TYPE zstr_hr_absenz,
          lt_ha_2001  TYPE STANDARD TABLE OF pa2001,
          ls_ha_2001  TYPE pa2001,
          lv_ha_gjahr TYPE gjahr,
          lv_ha_von   TYPE datum,
          lv_ha_bis   TYPE datum,
          lv_ha_skip  TYPE i,
          lv_ha_max   TYPE i,
          lv_ha_zeile TYPE i.

    FIELD-SYMBOLS: <ls_ha_filter> LIKE LINE OF it_filter_select_options,
                   <ls_ha_option> TYPE /iwbep/s_cod_select_option.

*   Vorgabe: laufendes Jahr.
    lv_ha_gjahr = sy-datum(4).

    LOOP AT it_filter_select_options ASSIGNING <ls_ha_filter>.
      LOOP AT <ls_ha_filter>-select_options ASSIGNING <ls_ha_option>
           WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_ha_filter>-property ).
          WHEN 'GJAHR'.
            lv_ha_gjahr = <ls_ha_option>-low.
          WHEN OTHERS.  " bewusst ignoriert statt zu raten
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

    CONCATENATE lv_ha_gjahr '0101' INTO lv_ha_von.
    CONCATENATE lv_ha_gjahr '1231' INTO lv_ha_bis.

    SELECT * FROM pa2001 INTO TABLE lt_ha_2001
      WHERE begda <= lv_ha_bis
        AND endda >= lv_ha_von
        AND sprps = space.

    LOOP AT lt_ha_2001 INTO ls_ha_2001.
      CLEAR ls_ha_out.
      ls_ha_out-pernr = ls_ha_2001-pernr.
      ls_ha_out-gjahr = lv_ha_gjahr.
      ls_ha_out-awart = ls_ha_2001-awart.
      ls_ha_out-begda = ls_ha_2001-begda.
      ls_ha_out-endda = ls_ha_2001-endda.
      ls_ha_out-seqnr = ls_ha_2001-seqnr.
      ls_ha_out-abwtg = ls_ha_2001-abwtg.
      ls_ha_out-stdaz = ls_ha_2001-stdaz.
      ls_ha_out-kaltg = ls_ha_2001-kaltg.
      APPEND ls_ha_out TO lt_ha_out.
    ENDLOOP.

*   Feste Reihenfolge, damit $skip/$top ueber die Seiten stabil bleiben.
    SORT lt_ha_out BY pernr begda awart endda seqnr.

    IF is_paging-skip > 0.
      lv_ha_skip = is_paging-skip.
      DELETE lt_ha_out TO lv_ha_skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_ha_max = is_paging-top.
      DESCRIBE TABLE lt_ha_out LINES lv_ha_zeile.
      IF lv_ha_zeile > lv_ha_max.
        lv_ha_max = lv_ha_max + 1.
        DELETE lt_ha_out FROM lv_ha_max.
      ENDIF.
    ENDIF.

    copy_data_to_ref( EXPORTING is_data = lt_ha_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

  IF iv_entity_set_name <> 'FinanzJournalSet'.
*   Jeder andere Aufruf geht unveraendert an die generierte Basisklasse.
    super->/iwbep/if_mgw_appl_srv_runtime~get_entityset(
      EXPORTING
        iv_entity_name           = iv_entity_name
        iv_entity_set_name       = iv_entity_set_name
        iv_source_name           = iv_source_name
        it_filter_select_options = it_filter_select_options
        it_order                 = it_order
        is_paging                = is_paging
        it_navigation_path       = it_navigation_path
        it_key_tab               = it_key_tab
        iv_filter_string         = iv_filter_string
        iv_search_string         = iv_search_string
        io_tech_request_context  = io_tech_request_context
      IMPORTING
        er_entityset             = er_entityset
        es_response_context      = es_response_context ).
    RETURN.
  ENDIF.

* ---------------------------------------------------------------------
* 1. Filter uebernehmen
* ---------------------------------------------------------------------
  LOOP AT it_filter_select_options ASSIGNING <ls_filter>.
    LOOP AT <ls_filter>-select_options ASSIGNING <ls_option>.

*     Je Zielbereich eine eigene Arbeitsstruktur. Eine gemeinsame waere
*     bequemer, wuerde aber beim APPEND in einen anders getypten Bereich
*     umsetzen: LOW von BUKRS ist vier Zeichen lang, LOW von BUDAT acht.
*     Ein Datumsfilter kaeme dabei abgeschnitten an und wuerde still
*     falsch selektieren.
      CASE to_upper( <ls_filter>-property ).
        WHEN 'BUKRS'.
          CLEAR ls_r_bukrs.
          ls_r_bukrs-sign   = <ls_option>-sign.
          ls_r_bukrs-option = <ls_option>-option.
          ls_r_bukrs-low    = <ls_option>-low.
          ls_r_bukrs-high   = <ls_option>-high.
          APPEND ls_r_bukrs TO lt_bukrs.
        WHEN 'GJAHR'.
          CLEAR ls_r_gjahr.
          ls_r_gjahr-sign   = <ls_option>-sign.
          ls_r_gjahr-option = <ls_option>-option.
          ls_r_gjahr-low    = <ls_option>-low.
          ls_r_gjahr-high   = <ls_option>-high.
          APPEND ls_r_gjahr TO lt_gjahr.
        WHEN 'BUDAT'.
*         Grenzen sammeln und danach zu EINEM Bereich zusammenfassen, statt
*         sie anzuhaengen: ein ABAP-Bereich ist ODER-verknuepft, "ab A ODER
*         vor B" waere der gesamte Bestand.
*
*         WICHTIG, gemessen am 2026-09-11: das rettet einen zweiseitigen
*         Datumsfilter NICHT. Bei zwei Bedingungen auf DEMSELBEN Feld
*         liefert das Gateway ueberhaupt keine Filteroptionen aus; hier
*         kommt dann gar nichts an, und der Filter faellt vollstaendig weg
*         (HTTP 500 nach 97 Sekunden, auch mit dieser Zusammenfassung).
*         Zwei Bedingungen auf VERSCHIEDENEN Feldern gehen dagegen in 1,5
*         Sekunden durch. Deshalb laedt der Leser ueber `Gjahr` und `Monat`,
*         also einen Filter je Feld. Der Code hier bleibt trotzdem richtig
*         und greift bei `EQ` und `BT` sowie bei einer einzelnen Grenze.
          CASE <ls_option>-option.
            WHEN 'GE'.
              lv_budat_lo = <ls_option>-low.
            WHEN 'GT'.
              lv_budat_lo = <ls_option>-low.
              lv_budat_lo = lv_budat_lo + 1.
            WHEN 'LE'.
              lv_budat_hi = <ls_option>-low.
            WHEN 'LT'.
              lv_budat_hi = <ls_option>-low.
              lv_budat_hi = lv_budat_hi - 1.
            WHEN 'EQ'.
              lv_budat_lo = <ls_option>-low.
              lv_budat_hi = <ls_option>-low.
            WHEN 'BT'.
              lv_budat_lo = <ls_option>-low.
              lv_budat_hi = <ls_option>-high.
            WHEN OTHERS.
*             Unbekannte Option unveraendert durchreichen, statt sie still
*             zu verschlucken.
              CLEAR ls_r_budat.
              ls_r_budat-sign   = <ls_option>-sign.
              ls_r_budat-option = <ls_option>-option.
              ls_r_budat-low    = <ls_option>-low.
              ls_r_budat-high   = <ls_option>-high.
              APPEND ls_r_budat TO lt_budat.
          ENDCASE.
        WHEN 'BLART'.
          CLEAR ls_r_blart.
          ls_r_blart-sign   = <ls_option>-sign.
          ls_r_blart-option = <ls_option>-option.
          ls_r_blart-low    = <ls_option>-low.
          ls_r_blart-high   = <ls_option>-high.
          APPEND ls_r_blart TO lt_blart.
        WHEN 'MONAT'.
*         Buchungsperiode. Darueber laedt der Leser monatsweise, weil zwei
*         Bedingungen auf demselben Feld nicht funktionieren, siehe unten.
          CLEAR ls_r_monat.
          ls_r_monat-sign   = <ls_option>-sign.
          ls_r_monat-option = <ls_option>-option.
          ls_r_monat-low    = <ls_option>-low.
          ls_r_monat-high   = <ls_option>-high.
          APPEND ls_r_monat TO lt_monat.
        WHEN OTHERS.  " bewusst ignoriert statt zu raten
      ENDCASE.
    ENDLOOP.
  ENDLOOP.

* Die gesammelten Datumsgrenzen zu genau einem Bereich zusammenfassen.
  IF lv_budat_lo IS NOT INITIAL OR lv_budat_hi IS NOT INITIAL.
    CLEAR ls_r_budat.
    ls_r_budat-sign = 'I'.
    IF lv_budat_lo IS NOT INITIAL AND lv_budat_hi IS NOT INITIAL.
      ls_r_budat-option = 'BT'.
      ls_r_budat-low    = lv_budat_lo.
      ls_r_budat-high   = lv_budat_hi.
    ELSEIF lv_budat_lo IS NOT INITIAL.
      ls_r_budat-option = 'GE'.
      ls_r_budat-low    = lv_budat_lo.
    ELSE.
      ls_r_budat-option = 'LE'.
      ls_r_budat-low    = lv_budat_hi.
    ENDIF.
    APPEND ls_r_budat TO lt_budat.
  ENDIF.

* ---------------------------------------------------------------------
* 2. Belegkoepfe. Belegart CO bleibt immer draussen, siehe Kopf.
* ---------------------------------------------------------------------
  TYPES: BEGIN OF ty_kopf,
           bukrs TYPE bkpf-bukrs,
           belnr TYPE bkpf-belnr,
           gjahr TYPE bkpf-gjahr,
           budat TYPE bkpf-budat,
           monat TYPE bkpf-monat,
           blart TYPE bkpf-blart,
           xblnr TYPE bkpf-xblnr,
           stblg TYPE bkpf-stblg,
           hwaer TYPE bkpf-hwaer,
           waers TYPE bkpf-waers,
         END OF ty_kopf.

  DATA: lt_kopf TYPE STANDARD TABLE OF ty_kopf,
        ls_kopf TYPE ty_kopf.

  SELECT bukrs belnr gjahr budat monat blart xblnr stblg hwaer waers
    FROM bkpf INTO TABLE lt_kopf
    WHERE bukrs IN lt_bukrs
      AND gjahr IN lt_gjahr
      AND monat IN lt_monat
      AND budat IN lt_budat
      AND blart IN lt_blart
      AND blart <> 'CO'.

  IF lt_kopf IS INITIAL.
    copy_data_to_ref( EXPORTING is_data = lt_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

  SORT lt_kopf BY bukrs belnr gjahr.

* ---------------------------------------------------------------------
* 3. Belegpositionen
* ---------------------------------------------------------------------
  TYPES: BEGIN OF ty_pos,
           bukrs TYPE bseg-bukrs,
           belnr TYPE bseg-belnr,
           gjahr TYPE bseg-gjahr,
           buzei TYPE bseg-buzei,
           hkont TYPE bseg-hkont,
           shkzg TYPE bseg-shkzg,
           dmbtr TYPE bseg-dmbtr,
           wrbtr TYPE bseg-wrbtr,
           kostl TYPE bseg-kostl,
           prctr TYPE bseg-prctr,
           sgtxt TYPE bseg-sgtxt,
           zfbdt TYPE bseg-zfbdt,
           zbd1t TYPE bseg-zbd1t,
           zbd2t TYPE bseg-zbd2t,
           zbd3t TYPE bseg-zbd3t,
           augdt TYPE bseg-augdt,
           augbl TYPE bseg-augbl,
         END OF ty_pos.

  DATA: lt_pos TYPE STANDARD TABLE OF ty_pos,
        ls_pos TYPE ty_pos.

  SELECT bukrs belnr gjahr buzei hkont shkzg dmbtr wrbtr
         kostl prctr sgtxt zfbdt zbd1t zbd2t zbd3t augdt augbl
    FROM bseg INTO TABLE lt_pos
    FOR ALL ENTRIES IN lt_kopf
    WHERE bukrs = lt_kopf-bukrs
      AND belnr = lt_kopf-belnr
      AND gjahr = lt_kopf-gjahr.

  IF lt_pos IS INITIAL.
    copy_data_to_ref( EXPORTING is_data = lt_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

* ---------------------------------------------------------------------
* 4. Kontotexte einmalig lesen, nicht je Position
* ---------------------------------------------------------------------
  DATA: lt_t001  TYPE STANDARD TABLE OF t001,
        ls_t001  TYPE t001,
        lv_ktopl TYPE t001-ktopl,
        lv_spras TYPE sy-langu.

  SELECT * FROM t001 INTO TABLE lt_t001
    FOR ALL ENTRIES IN lt_kopf
    WHERE bukrs = lt_kopf-bukrs.

* Die Sprache der Anmeldung ist bei einem OData-Aufruf NICHT zwangslaeufig
* Deutsch, und in SKAT stehen die Kontotexte hier ausschliesslich auf 'D'
* (gemessen am 2026-09-11: Konten 10230 und 30901 haben genau einen Satz,
* Sprache D). Die erste Fassung las nur mit sy-langu und lieferte deshalb
* durchgehend einen leeren Kontotext. Es werden nun beide Sprachen geladen und
* beim Lesen wird die Anmeldesprache bevorzugt, Deutsch ist der Rueckfall.
  lv_spras = sy-langu.
  IF lv_spras IS INITIAL.
    lv_spras = 'D'.
  ENDIF.

  TYPES: BEGIN OF ty_txt,
           ktopl TYPE skat-ktopl,
           saknr TYPE skat-saknr,
           spras TYPE skat-spras,
           txt50 TYPE skat-txt50,
         END OF ty_txt.

  DATA: lt_txt TYPE SORTED TABLE OF ty_txt
                WITH UNIQUE KEY ktopl saknr spras,
        ls_txt TYPE ty_txt.

  IF lt_t001 IS NOT INITIAL.
    SELECT ktopl saknr spras txt50 FROM skat
      INTO CORRESPONDING FIELDS OF TABLE lt_txt
      FOR ALL ENTRIES IN lt_t001
      WHERE ktopl = lt_t001-ktopl
        AND ( spras = lv_spras OR spras = 'D' ).
  ENDIF.

* ---------------------------------------------------------------------
* 5. Ausgabe zusammenstellen
* ---------------------------------------------------------------------
  LOOP AT lt_pos INTO ls_pos.

    READ TABLE lt_kopf INTO ls_kopf
      WITH KEY bukrs = ls_pos-bukrs
               belnr = ls_pos-belnr
               gjahr = ls_pos-gjahr
      BINARY SEARCH.
    IF sy-subrc <> 0.
      CONTINUE.
    ENDIF.

    CLEAR ls_out.
    ls_out-bukrs = ls_pos-bukrs.
    ls_out-belnr = ls_pos-belnr.
    ls_out-gjahr = ls_pos-gjahr.
    ls_out-buzei = ls_pos-buzei.
    ls_out-budat = ls_kopf-budat.
    ls_out-monat = ls_kopf-monat.
    ls_out-blart = ls_kopf-blart.
    ls_out-xblnr = ls_kopf-xblnr.
    ls_out-stblg = ls_kopf-stblg.
    ls_out-hwaer = ls_kopf-hwaer.

*   Transaktionswaehrung nur, wenn sie von der Hauswaehrung abweicht.
    IF ls_kopf-waers <> ls_kopf-hwaer.
      ls_out-waers = ls_kopf-waers.
    ENDIF.

    ls_out-hkont = ls_pos-hkont.
    ls_out-shkzg = ls_pos-shkzg.
    ls_out-dmbtr = ls_pos-dmbtr.
    ls_out-wrbtr = ls_pos-wrbtr.
    ls_out-kostl = ls_pos-kostl.
    ls_out-prctr = ls_pos-prctr.
    ls_out-sgtxt = ls_pos-sgtxt.
    ls_out-augdt = ls_pos-augdt.
    ls_out-augbl = ls_pos-augbl.

    CLEAR ls_txt.
    READ TABLE lt_t001 INTO ls_t001 WITH KEY bukrs = ls_pos-bukrs.
    IF sy-subrc = 0.
      lv_ktopl = ls_t001-ktopl.
*     Kein BINARY SEARCH: der Zusatz ist nur fuer Standardtabellen
*     erlaubt, lt_txt ist eine SORTED TABLE und sucht ohnehin binaer.
      READ TABLE lt_txt INTO ls_txt
        WITH KEY ktopl = lv_ktopl
                 saknr = ls_pos-hkont
                 spras = lv_spras.
      IF sy-subrc <> 0 AND lv_spras <> 'D'.
        READ TABLE lt_txt INTO ls_txt
          WITH KEY ktopl = lv_ktopl
                   saknr = ls_pos-hkont
                   spras = 'D'.
      ENDIF.
      IF sy-subrc = 0.
        ls_out-hkonttxt = ls_txt-txt50.
      ENDIF.
    ENDIF.

*   Nettofaelligkeit: FAEDT ist kein Tabellenfeld, sondern Basisdatum
*   plus Zahlungsziel. Ohne ZFBDT bleibt es leer; das betrifft rund vier
*   Fuenftel der Zeilen und ist richtig, weil Faelligkeit nur bei offenen
*   Posten eine Aussage hat.
    CLEAR ls_out-faedt.
    IF ls_pos-zfbdt IS NOT INITIAL.
      lv_tage = ls_pos-zbd1t.
      IF ls_pos-zbd2t > lv_tage.
        lv_tage = ls_pos-zbd2t.
      ENDIF.
      IF ls_pos-zbd3t > lv_tage.
        lv_tage = ls_pos-zbd3t.
      ENDIF.
      ls_out-faedt = ls_pos-zfbdt + lv_tage.
    ENDIF.

    APPEND ls_out TO lt_out.
  ENDLOOP.

  SORT lt_out BY bukrs gjahr belnr buzei.

* ---------------------------------------------------------------------
* 6. Paging. Der Leser blaettert in 1000er-Seiten.
* ---------------------------------------------------------------------
  IF is_paging-skip > 0.
    lv_skip = is_paging-skip.
    DELETE lt_out TO lv_skip.
  ENDIF.

  IF is_paging-top > 0.
    lv_max = is_paging-top.
    DESCRIBE TABLE lt_out LINES lv_zeile.
    IF lv_zeile > lv_max.
      lv_max = lv_max + 1.
      DELETE lt_out FROM lv_max.
    ENDIF.
  ENDIF.

  copy_data_to_ref( EXPORTING is_data = lt_out CHANGING cr_data = er_entityset ).

ENDMETHOD.
