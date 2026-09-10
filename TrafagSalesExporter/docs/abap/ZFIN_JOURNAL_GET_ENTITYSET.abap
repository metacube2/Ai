*&---------------------------------------------------------------------*
*& SAP OData: Hauptbuch-Journal CH/AT auf Positionsebene
*& Service: ZPOWERBI_EINKAUF_SRV
*& EntitySet: FinanzJournalSet
*& Stand: 2026-09-10
*&
*& SEGW-Voraussetzung:
*& - DDIC-Struktur ZSTR_FIN_JOURNAL anlegen (Felder siehe unten)
*& - Entity Type daraus importieren, Name FinanzJournal
*& - Key: BUKRS + BELNR + GJAHR + BUZEI
*&   (BELNR allein ist nicht eindeutig, erst mit Buchungskreis und Jahr)
*& - Related Entity Set anlegen: FinanzJournalSet
*&   Genau dieser Name ist die Vorgabe des Lesers; jeder andere muss in
*&   Sites.SapEntitySet fuer ZSCHWEIZ eingetragen werden.
*& - Runtime Objects generieren, danach diese Methode im DPC_EXT
*&   redefinieren und den Rumpf hier einsetzen.
*&
*& Warum die vorhandenen Sets nicht taugen (gemessen am 2026-09-08):
*&   FinanzdataSchweizOeSet = Faktura, kein Journal
*&   bkpfSet                = nur Belegkoepfe, ohne BUZEI kein Journal
*&   bsisSet                = nur OFFENE Posten, damit stimmt kein Saldo
*&
*& Fachliche Vorgabe Andreas, 2026-09-10: Die Schweiz hat ueber 5 Mio
*& Zeitbuchungszeilen je Jahr. Sie laufen als Belegart CO und werden hier
*& grundsaetzlich ausgeschlossen. Fuer den ersten Entwurf liefert der
*& Client ohnehin nur Buchungskreis 1200 (Oesterreich).
*&
*& Gemessene Fuellgrade Oesterreich 2025/2026 (17'364 Positionen):
*&   PRCTR 0.0 %, KOSTL 13.6 %, SGTXT 22.5 %,
*&   AUGDT/AUGBL je 22.1 %, ZFBDT 20.8 %, WRBTR 98.5 %
*& PRCTR bleibt also leer. Das ist der Quellzustand, kein Ladefehler.
*&---------------------------------------------------------------------*

METHOD finanzjournalset_get_entityset.

  DATA: lt_bukrs TYPE RANGE OF bkpf-bukrs,
        lt_gjahr TYPE RANGE OF bkpf-gjahr,
        lt_budat TYPE RANGE OF bkpf-budat,
        ls_range LIKE LINE OF lt_bukrs.

  DATA: lv_max   TYPE i,
        lv_skip  TYPE i,
        lv_zeile TYPE i.

  FIELD-SYMBOLS: <ls_filter> LIKE LINE OF it_filter_select_options,
                 <ls_option> TYPE /iwbep/s_cod_select_option.

* ---------------------------------------------------------------------
* 1. Filter aus der Anfrage uebernehmen
*    Ohne Filter wuerde die Schweiz mit Millionen Zeilen mitkommen.
*    Der Client filtert auf Bukrs und Budat; beides wird hier ausgewertet.
* ---------------------------------------------------------------------
  LOOP AT it_filter_select_options ASSIGNING <ls_filter>.
    LOOP AT <ls_filter>-select_options ASSIGNING <ls_option>.
      CLEAR ls_range.
      ls_range-sign   = <ls_option>-sign.
      ls_range-option = <ls_option>-option.
      ls_range-low    = <ls_option>-low.
      ls_range-high   = <ls_option>-high.

      CASE to_upper( <ls_filter>-property ).
        WHEN 'BUKRS'.
          APPEND ls_range TO lt_bukrs.
        WHEN 'GJAHR'.
          APPEND ls_range TO lt_gjahr.
        WHEN 'BUDAT'.
          APPEND ls_range TO lt_budat.
        WHEN OTHERS.
          " Andere Properties werden bewusst ignoriert statt zu raten.
      ENDCASE.
    ENDLOOP.
  ENDLOOP.

* ---------------------------------------------------------------------
* 2. Belegkoepfe lesen
*    Belegart CO ist ausgeschlossen: das sind die Zeitbuchungen, die den
*    Schweizer Bestand auf ueber 5 Mio Zeilen je Jahr treiben.
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
      AND blart <> 'CO'.

  IF lt_kopf IS INITIAL.
    RETURN.
  ENDIF.

  SORT lt_kopf BY bukrs belnr gjahr.

* ---------------------------------------------------------------------
* 3. Belegpositionen lesen
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
    RETURN.
  ENDIF.

* ---------------------------------------------------------------------
* 4. Kontotexte einmalig lesen
*    Ein SELECT je Position waere bei 17'000 Zeilen nicht vertretbar.
*    Kontenplan kommt aus T001, Sprache aus der Anmeldung mit Rueckfall D.
* ---------------------------------------------------------------------
  DATA: lt_ktopl TYPE STANDARD TABLE OF t001,
        ls_t001  TYPE t001,
        lv_ktopl TYPE t001-ktopl,
        lv_spras TYPE sy-langu.

  SELECT * FROM t001 INTO TABLE lt_ktopl
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

  IF lt_ktopl IS NOT INITIAL.
    SELECT ktopl saknr txt50 FROM skat
      INTO CORRESPONDING FIELDS OF TABLE lt_txt
      FOR ALL ENTRIES IN lt_ktopl
      WHERE spras = lv_spras
        AND ktopl = lt_ktopl-ktopl.
  ENDIF.

* ---------------------------------------------------------------------
* 5. Ausgabe zusammenstellen
* ---------------------------------------------------------------------
  DATA: ls_out  TYPE zstr_fin_journal,
        lv_tage TYPE i.

  SORT lt_kopf BY bukrs belnr gjahr.

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

    " Transaktionswaehrung nur, wenn sie von der Hauswaehrung abweicht.
    " Sonst stuende in jeder Zeile derselbe Wert zweimal.
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

    " Kontotext
    CLEAR ls_txt.
    READ TABLE lt_ktopl INTO ls_t001
      WITH KEY bukrs = ls_pos-bukrs.
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

    " Nettofaelligkeitsdatum: FAEDT ist kein Tabellenfeld, sondern wird
    " aus dem Basisdatum plus Zahlungsziel gerechnet. Ohne ZFBDT bleibt
    " es leer; das betrifft rund vier Fuenftel der Zeilen und ist richtig,
    " weil Faelligkeit nur bei offenen Posten eine Aussage hat.
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

    APPEND ls_out TO et_entityset.
  ENDLOOP.

  SORT et_entityset BY bukrs gjahr belnr buzei.

* ---------------------------------------------------------------------
* 6. Paging
*    Ohne $top/$skip liefert das Set alles. Der Leser blaettert in
*    1000er-Seiten; das wird hier bedient.
* ---------------------------------------------------------------------
  IF is_paging-skip > 0.
    lv_skip = is_paging-skip.
    DELETE et_entityset TO lv_skip.
  ENDIF.

  IF is_paging-top > 0.
    lv_max = is_paging-top.
    DESCRIBE TABLE et_entityset LINES lv_zeile.
    IF lv_zeile > lv_max.
      lv_max = lv_max + 1.
      DELETE et_entityset FROM lv_max.
    ENDIF.
  ENDIF.

ENDMETHOD.
