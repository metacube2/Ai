*&---------------------------------------------------------------------*
*& Verkauf, Monthly Review Gesellschaften, Abschnitt 4 "Qualitaet · offene Q-Faelle":
*& QmMeldungSet im Service ZPOWERBI_EINKAUF_SRV
*& Stand    : 2026-11-05, erste Fassung (T76), Wunsch Patrick Hertach, Auftrag Ingo
*& Zweck    : alle offenen QM-Meldungen der Art Q1 (Kundenreklamation) als Einzelfaelle,
*&            ohne Pflichtfilter, nur GET Set. Doku: saptasks/qm_meldungen/NOTES.md
*& Muster   : ShopAufSet / ZloTopDownSet (eigener Zweig in GET_ENTITYSET vor der
*&            FinanzJournal-Weiche, Entity in MPC_EXT DEFINE, bind_conversions ' ').
*&
*& DDIC (SE11, Paket ZPP wie ZSTR_ZLO_TOPDOWN):
*&   ZSTR_QM_MELDUNG: QMNUM CHAR12, QMART CHAR2, STATUS CHAR80, SYSSTAT CHAR40,
*&                    KUNNR CHAR10, KUNDE CHAR35, MATNR CHAR40, MAKTX CHAR40,
*&                    MENGE /SCMTMS/DECIMAL_VALUE (DEC 28,6, ohne Mengenreferenz),
*&                    MENGENEINHEIT CHAR3, FEHLERBILD CHAR40, ANGELEGT CHAR8,
*&                    RUECKLIEFERUNG CHAR8, PRODUKTION CHAR8, MASSNAHME CHAR8,
*&                    BEARBEITER CHAR40, PRODUKTLINIE CHAR40
*&   Alle Datenelemente ohne Konvertierungsexit; Schluessel, Material und Kunde werden
*&   im Code ohne fuehrende Nullen geschrieben, Datumsfelder als JJJJMMTT oder leer.
*&
*& "offen" (gelesen in T76 am 2026-11-05):
*&   QMEL-QMART = 'Q1', QMEL-KZLOESCH = ' ' und in JEST weder I0072 (MMAB, Meldung
*&   abgeschlossen) noch I0076 (LOEVM, Loeschvormerkung) aktiv. Damit zaehlen MOFN
*&   (I0068), MIAR (I0070) und MRST (I0069, zurueckgestellt) als offen; MRST steht
*&   in Sysstat und laesst sich im Dashboard ausblenden.
*&---------------------------------------------------------------------*

* ===== MPC_EXT DEFINE: am Ende, direkt vor ENDMETHOD einfuegen =========
* ---------------------------------------------------------------------
* QmMeldung: QmMeldungSet (offene Q1-Meldungen, Monthly Review), ohne Filter
* saptasks/qm_meldungen/ZQM_MELDUNG_ADD.abap
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'QmMeldung'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Qmnum' iv_abap_fieldname = 'QMNUM' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 12 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Qmart' iv_abap_fieldname = 'QMART' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Status' iv_abap_fieldname = 'STATUS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 80 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Sysstat' iv_abap_fieldname = 'SYSSTAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kunnr' iv_abap_fieldname = 'KUNNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kunde' iv_abap_fieldname = 'KUNDE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 35 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Maktx' iv_abap_fieldname = 'MAKTX' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Menge' iv_abap_fieldname = 'MENGE' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 6 ).
  lo_property->set_maxlength( iv_max_length = 28 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Mengeneinheit' iv_abap_fieldname = 'MENGENEINHEIT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Fehlerbild' iv_abap_fieldname = 'FEHLERBILD' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Angelegt' iv_abap_fieldname = 'ANGELEGT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Ruecklieferung' iv_abap_fieldname = 'RUECKLIEFERUNG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Produktion' iv_abap_fieldname = 'PRODUKTION' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Massnahme' iv_abap_fieldname = 'MASSNAHME' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Bearbeiter' iv_abap_fieldname = 'BEARBEITER' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Produktlinie' iv_abap_fieldname = 'PRODUKTLINIE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_QM_MELDUNG' iv_bind_conversions = ' ' ).
  lo_entity_set = lo_entity_type->create_entity_set( 'QmMeldungSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
* ===== Ende MPC ========================================================

* ===== DPC_EXT GET_ENTITYSET: nach dem ZloTopDownSet-Block, VOR "IF iv_entity_set_name <> 'FinanzJournalSet'." =====
* Monthly Review, offene Q1-Meldungen, saptasks/qm_meldungen/ZQM_MELDUNG_ADD.abap

  IF iv_entity_set_name = 'QmMeldungSet'.

    TYPES: BEGIN OF ty_qm_kopf,
             qmnum   TYPE qmel-qmnum,
             qmart   TYPE qmel-qmart,
             objnr   TYPE qmel-objnr,
             erdat   TYPE qmel-erdat,
             qmtxt   TYPE qmel-qmtxt,
             kunum   TYPE qmel-kunum,
             matnr   TYPE qmel-matnr,
             rkmng   TYPE qmel-rkmng,
             mgein   TYPE qmel-mgein,
             rkdat   TYPE qmel-rkdat,
             proddat TYPE qmel-proddat,
           END OF ty_qm_kopf.
    TYPES: BEGIN OF ty_qm_sm,
             qmnum TYPE qmsm-qmnum,
             erdat TYPE qmsm-erdat,
           END OF ty_qm_sm.
    TYPES: BEGIN OF ty_qm_pa,
             objnr   TYPE ihpa-objnr,
             counter TYPE ihpa-counter,
             parnr   TYPE ihpa-parnr,
           END OF ty_qm_pa.
    TYPES: BEGIN OF ty_qm_name,
             pernr TYPE pa0001-pernr,
             endda TYPE pa0001-endda,
             ename TYPE pa0001-ename,
           END OF ty_qm_name.
    TYPES: BEGIN OF ty_qm_ust,
             objnr TYPE jest-objnr,
             stat  TYPE jest-stat,
           END OF ty_qm_ust.
    TYPES: BEGIN OF ty_qm_jsto,
             objnr TYPE jsto-objnr,
             stsma TYPE jsto-stsma,
           END OF ty_qm_jsto.
    TYPES: BEGIN OF ty_qm_utxt,
             stsma TYPE tj30-stsma,
             estat TYPE tj30-estat,
             stonr TYPE tj30-stonr,
             txt30 TYPE tj30t-txt30,
           END OF ty_qm_utxt.
    TYPES: BEGIN OF ty_qm_kna1,
             kunnr TYPE kna1-kunnr,
             name1 TYPE kna1-name1,
           END OF ty_qm_kna1.
    TYPES: BEGIN OF ty_qm_makt,
             matnr TYPE makt-matnr,
             spras TYPE makt-spras,
             maktx TYPE makt-maktx,
           END OF ty_qm_makt.

    DATA: lt_qm_out   TYPE STANDARD TABLE OF zstr_qm_meldung,
          ls_qm_out   TYPE zstr_qm_meldung,
          lt_qm_kopf  TYPE STANDARD TABLE OF ty_qm_kopf,
          ls_qm_kopf  TYPE ty_qm_kopf,
          lt_qm_sm    TYPE STANDARD TABLE OF ty_qm_sm,
          ls_qm_sm    TYPE ty_qm_sm,
          lt_qm_pa    TYPE STANDARD TABLE OF ty_qm_pa,
          ls_qm_pa    TYPE ty_qm_pa,
          lt_qm_pernr TYPE STANDARD TABLE OF pa0001-pernr,
          lv_qm_pernr TYPE pa0001-pernr,
          lt_qm_name  TYPE STANDARD TABLE OF ty_qm_name,
          ls_qm_name  TYPE ty_qm_name,
          lt_qm_ust   TYPE STANDARD TABLE OF ty_qm_ust,
          ls_qm_ust   TYPE ty_qm_ust,
          lt_qm_jsto  TYPE STANDARD TABLE OF ty_qm_jsto,
          ls_qm_jsto  TYPE ty_qm_jsto,
          lt_qm_utxt  TYPE STANDARD TABLE OF ty_qm_utxt,
          ls_qm_utxt  TYPE ty_qm_utxt,
          lt_qm_kna1  TYPE STANDARD TABLE OF ty_qm_kna1,
          ls_qm_kna1  TYPE ty_qm_kna1,
          lt_qm_makt  TYPE STANDARD TABLE OF ty_qm_makt,
          ls_qm_makt  TYPE ty_qm_makt,
          lv_qm_line  TYPE bsvx-sttxt,
          lv_qm_unum  TYPE string,
          lv_qm_ualle TYPE string,
          lv_qm_skip  TYPE i,
          lv_qm_max   TYPE i,
          lv_qm_zeile TYPE i.

*   Offene Q1-Meldungen: nicht abgeschlossen (I0072), nicht zum Loeschen vorgemerkt (I0076)
    SELECT q~qmnum q~qmart q~objnr q~erdat q~qmtxt q~kunum q~matnr
           q~rkmng q~mgein q~rkdat q~proddat
      FROM qmel AS q
      INTO CORRESPONDING FIELDS OF TABLE lt_qm_kopf
      WHERE q~qmart    = 'Q1'
        AND q~kzloesch = ' '
        AND NOT EXISTS ( SELECT objnr FROM jest
                           WHERE objnr = q~objnr
                             AND stat  IN ('I0072','I0076')
                             AND inact = ' ' ).
    SORT lt_qm_kopf BY qmnum.

    IF lt_qm_kopf IS NOT INITIAL.
*     Erste Massnahme: kleinstes Anlagedatum der nicht geloeschten Massnahmen
      SELECT qmnum erdat FROM qmsm INTO TABLE lt_qm_sm
        FOR ALL ENTRIES IN lt_qm_kopf
        WHERE qmnum    = lt_qm_kopf-qmnum
          AND kzloesch = ' '.
      SORT lt_qm_sm BY qmnum erdat.
      DELETE ADJACENT DUPLICATES FROM lt_qm_sm COMPARING qmnum.

*     Bearbeiter: Partnerrolle VW (Verantwortlicher, TQ80-PARVW_VERA, Personalnummer)
      SELECT objnr counter parnr FROM ihpa INTO TABLE lt_qm_pa
        FOR ALL ENTRIES IN lt_qm_kopf
        WHERE objnr    = lt_qm_kopf-objnr
          AND parvw    = 'VW'
          AND kzloesch = ' '.
      SORT lt_qm_pa BY objnr counter.
      DELETE ADJACENT DUPLICATES FROM lt_qm_pa COMPARING objnr.
      LOOP AT lt_qm_pa INTO ls_qm_pa.
        lv_qm_pernr = ls_qm_pa-parnr.
        APPEND lv_qm_pernr TO lt_qm_pernr.
      ENDLOOP.
      SORT lt_qm_pernr.
      DELETE ADJACENT DUPLICATES FROM lt_qm_pernr.

*     Anwenderstatus (nur wenn der Meldung ein Statusschema zugeordnet ist)
      SELECT objnr stat FROM jest INTO TABLE lt_qm_ust
        FOR ALL ENTRIES IN lt_qm_kopf
        WHERE objnr = lt_qm_kopf-objnr
          AND stat  LIKE 'E%'
          AND inact = ' '.

      SELECT kunnr name1 FROM kna1 INTO TABLE lt_qm_kna1
        FOR ALL ENTRIES IN lt_qm_kopf
        WHERE kunnr = lt_qm_kopf-kunum.
      SORT lt_qm_kna1 BY kunnr.

      SELECT matnr spras maktx FROM makt INTO TABLE lt_qm_makt
        FOR ALL ENTRIES IN lt_qm_kopf
        WHERE matnr = lt_qm_kopf-matnr
          AND spras IN ('D','E').
      SORT lt_qm_makt BY matnr spras.
    ENDIF.

    IF lt_qm_pernr IS NOT INITIAL.
      SELECT pernr endda ename FROM pa0001 INTO TABLE lt_qm_name
        FOR ALL ENTRIES IN lt_qm_pernr
        WHERE pernr = lt_qm_pernr-table_line.
*     juengster Satz je Person (heute gueltig oder zuletzt gueltig)
      SORT lt_qm_name BY pernr ASCENDING endda DESCENDING.
      DELETE ADJACENT DUPLICATES FROM lt_qm_name COMPARING pernr.
    ENDIF.

    IF lt_qm_ust IS NOT INITIAL.
      SELECT objnr stsma FROM jsto INTO TABLE lt_qm_jsto
        FOR ALL ENTRIES IN lt_qm_ust
        WHERE objnr = lt_qm_ust-objnr.
      SORT lt_qm_jsto BY objnr.
      DELETE lt_qm_jsto WHERE stsma IS INITIAL.
    ENDIF.
    IF lt_qm_jsto IS NOT INITIAL.
      SELECT a~stsma a~estat a~stonr b~txt30
        FROM tj30 AS a INNER JOIN tj30t AS b
          ON  b~stsma = a~stsma
          AND b~estat = a~estat
        INTO TABLE lt_qm_utxt
        FOR ALL ENTRIES IN lt_qm_jsto
        WHERE a~stsma = lt_qm_jsto-stsma
          AND b~spras = 'D'.
      SORT lt_qm_utxt BY stsma estat.
    ENDIF.
    SORT lt_qm_ust BY objnr stat.

    LOOP AT lt_qm_kopf INTO ls_qm_kopf.
      CLEAR ls_qm_out.

      CALL FUNCTION 'CONVERSION_EXIT_ALPHA_OUTPUT'
        EXPORTING input  = ls_qm_kopf-qmnum
        IMPORTING output = ls_qm_out-qmnum.
      ls_qm_out-qmart = ls_qm_kopf-qmart.

*     Systemstatus-Kurztexte wie in IW53/QM03 (z. B. "MIAR FRDV OFMA"), Sprache DE
      CLEAR lv_qm_line.
      CALL FUNCTION 'STATUS_TEXT_EDIT'
        EXPORTING
          objnr            = ls_qm_kopf-objnr
          spras            = 'D'
          flg_user_stat    = ' '
          only_active      = 'X'
        IMPORTING
          line             = lv_qm_line
        EXCEPTIONS
          object_not_found = 1
          OTHERS           = 2.
      IF sy-subrc = 0.
        ls_qm_out-sysstat = lv_qm_line.
      ENDIF.

*     Anwenderstatus: Langtext (TXT30, DE); bevorzugt der Status mit Statusnummer
      CLEAR: lv_qm_unum, lv_qm_ualle.
      READ TABLE lt_qm_jsto INTO ls_qm_jsto WITH KEY objnr = ls_qm_kopf-objnr BINARY SEARCH.
      IF sy-subrc = 0.
        LOOP AT lt_qm_ust INTO ls_qm_ust WHERE objnr = ls_qm_kopf-objnr.
          READ TABLE lt_qm_utxt INTO ls_qm_utxt
               WITH KEY stsma = ls_qm_jsto-stsma estat = ls_qm_ust-stat BINARY SEARCH.
          IF sy-subrc <> 0.
            CONTINUE.
          ENDIF.
          IF ls_qm_utxt-stonr > 0.
            IF lv_qm_unum IS INITIAL.
              lv_qm_unum = ls_qm_utxt-txt30.
            ELSE.
              CONCATENATE lv_qm_unum ls_qm_utxt-txt30 INTO lv_qm_unum SEPARATED BY ' / '.
            ENDIF.
          ENDIF.
          IF lv_qm_ualle IS INITIAL.
            lv_qm_ualle = ls_qm_utxt-txt30.
          ELSE.
            CONCATENATE lv_qm_ualle ls_qm_utxt-txt30 INTO lv_qm_ualle SEPARATED BY ' / '.
          ENDIF.
        ENDLOOP.
      ENDIF.
      IF lv_qm_unum IS NOT INITIAL.
        ls_qm_out-status = lv_qm_unum.
      ELSE.
        ls_qm_out-status = lv_qm_ualle.
      ENDIF.

      IF ls_qm_kopf-kunum IS NOT INITIAL.
        CALL FUNCTION 'CONVERSION_EXIT_ALPHA_OUTPUT'
          EXPORTING input  = ls_qm_kopf-kunum
          IMPORTING output = ls_qm_out-kunnr.
        READ TABLE lt_qm_kna1 INTO ls_qm_kna1 WITH KEY kunnr = ls_qm_kopf-kunum BINARY SEARCH.
        IF sy-subrc = 0.
          ls_qm_out-kunde = ls_qm_kna1-name1.
        ENDIF.
      ENDIF.

      IF ls_qm_kopf-matnr IS NOT INITIAL.
        CALL FUNCTION 'CONVERSION_EXIT_MATN1_OUTPUT'
          EXPORTING  input  = ls_qm_kopf-matnr
          IMPORTING  output = ls_qm_out-matnr
          EXCEPTIONS OTHERS = 1.
        IF sy-subrc <> 0.
          ls_qm_out-matnr = ls_qm_kopf-matnr.
        ENDIF.
        READ TABLE lt_qm_makt INTO ls_qm_makt WITH KEY matnr = ls_qm_kopf-matnr spras = 'D' BINARY SEARCH.
        IF sy-subrc <> 0.
          READ TABLE lt_qm_makt INTO ls_qm_makt WITH KEY matnr = ls_qm_kopf-matnr spras = 'E' BINARY SEARCH.
        ENDIF.
        IF sy-subrc = 0.
          ls_qm_out-maktx = ls_qm_makt-maktx.
        ENDIF.
      ENDIF.

      ls_qm_out-menge = ls_qm_kopf-rkmng.
      IF ls_qm_kopf-mgein IS NOT INITIAL.
        CALL FUNCTION 'CONVERSION_EXIT_CUNIT_OUTPUT'
          EXPORTING
            input          = ls_qm_kopf-mgein
            language       = 'D'
          IMPORTING
            output         = ls_qm_out-mengeneinheit
          EXCEPTIONS
            unit_not_found = 1
            OTHERS         = 2.
        IF sy-subrc <> 0.
          ls_qm_out-mengeneinheit = ls_qm_kopf-mgein.
        ENDIF.
      ENDIF.

*     Fehlerbild = Kurztext der Meldung (QMEL-QMTXT)
      ls_qm_out-fehlerbild = ls_qm_kopf-qmtxt.

      IF ls_qm_kopf-erdat IS NOT INITIAL.
        ls_qm_out-angelegt = ls_qm_kopf-erdat.
      ENDIF.
      IF ls_qm_kopf-rkdat IS NOT INITIAL.
        ls_qm_out-ruecklieferung = ls_qm_kopf-rkdat.
      ENDIF.
      IF ls_qm_kopf-proddat IS NOT INITIAL.
        ls_qm_out-produktion = ls_qm_kopf-proddat.
      ENDIF.

      READ TABLE lt_qm_sm INTO ls_qm_sm WITH KEY qmnum = ls_qm_kopf-qmnum BINARY SEARCH.
      IF sy-subrc = 0 AND ls_qm_sm-erdat IS NOT INITIAL.
        ls_qm_out-massnahme = ls_qm_sm-erdat.
      ENDIF.

      READ TABLE lt_qm_pa INTO ls_qm_pa WITH KEY objnr = ls_qm_kopf-objnr BINARY SEARCH.
      IF sy-subrc = 0.
        lv_qm_pernr = ls_qm_pa-parnr.
        READ TABLE lt_qm_name INTO ls_qm_name WITH KEY pernr = lv_qm_pernr BINARY SEARCH.
        IF sy-subrc = 0.
          ls_qm_out-bearbeiter = ls_qm_name-ename.
        ENDIF.
      ENDIF.

*     Produktlinie: im System nicht eindeutig bestimmbar, bleibt leer (NOTES.md Abschnitt 3)
      APPEND ls_qm_out TO lt_qm_out.
    ENDLOOP.

    IF is_paging-skip > 0.
      lv_qm_skip = is_paging-skip.
      DELETE lt_qm_out TO lv_qm_skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_qm_max = is_paging-top + 1.
      DESCRIBE TABLE lt_qm_out LINES lv_qm_zeile.
      IF lv_qm_zeile >= lv_qm_max.
        DELETE lt_qm_out FROM lv_qm_max.
      ENDIF.
    ENDIF.

    copy_data_to_ref( EXPORTING is_data = lt_qm_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.
* ===== Ende DPC ========================================================
