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
        ls_range LIKE LINE OF lt_bukrs,
        lv_tage  TYPE i,
        lv_max   TYPE i,
        lv_skip  TYPE i,
        lv_zeile TYPE i.

  FIELD-SYMBOLS: <ls_filter> LIKE LINE OF it_filter_select_options,
                 <ls_option> TYPE /iwbep/s_cod_select_option.

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
      CLEAR ls_range.
      ls_range-sign   = <ls_option>-sign.
      ls_range-option = <ls_option>-option.
      ls_range-low    = <ls_option>-low.
      ls_range-high   = <ls_option>-high.

      CASE to_upper( <ls_filter>-property ).
        WHEN 'BUKRS'. APPEND ls_range TO lt_bukrs.
        WHEN 'GJAHR'. APPEND ls_range TO lt_gjahr.
        WHEN 'BUDAT'. APPEND ls_range TO lt_budat.
        WHEN 'BLART'. APPEND ls_range TO lt_blart.
        WHEN OTHERS.  " bewusst ignoriert statt zu raten
      ENDCASE.
    ENDLOOP.
  ENDLOOP.

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

  lv_spras = sy-langu.
  IF lv_spras IS INITIAL.
    lv_spras = 'D'.
  ENDIF.

  TYPES: BEGIN OF ty_txt,
           ktopl TYPE skat-ktopl,
           saknr TYPE skat-saknr,
           txt50 TYPE skat-txt50,
         END OF ty_txt.

  DATA: lt_txt TYPE SORTED TABLE OF ty_txt
                WITH NON-UNIQUE KEY ktopl saknr,
        ls_txt TYPE ty_txt.

  IF lt_t001 IS NOT INITIAL.
    SELECT ktopl saknr txt50 FROM skat
      INTO CORRESPONDING FIELDS OF TABLE lt_txt
      FOR ALL ENTRIES IN lt_t001
      WHERE spras = lv_spras
        AND ktopl = lt_t001-ktopl.
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
      READ TABLE lt_txt INTO ls_txt
        WITH KEY ktopl = lv_ktopl
                 saknr = ls_pos-hkont
        BINARY SEARCH.
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
