*&---------------------------------------------------------------------*
*& DPC_EXT: Logistik live (Kommissionierung, Lieferungen, Rueckmeldungen)
*& Klasse : ZCL_ZPOWERBI_EINKAUF_DPC_EXT
*& Methode: /IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET (redefiniert)
*& Stand  : 2026-10-01
*&
*& EINFUEGEN direkt NACH dem HrAbsenzSet-Block und VOR
*& "IF iv_entity_set_name <> 'FinanzJournalSet'."
*&
*& SCHUTZ FUER P76 (Produktion steht, wenn SAP steht; Ingo 2026-10-01):
*& - nur lesend, nur genannte Spalten, kein SELECT *;
*& - Pflichtfilter Datum (genau ein Tag), hoechstens ein Jahr zurueck, nicht Zukunft,
*&   sonst leere Antwort ohne einen einzigen Datenbankzugriff;
*& - optional AbZeit (HHMMSS): nur was ab dieser Uhrzeit neu ist; das Cockpit
*&   fragt damit nach dem ersten Abruf nur noch die letzten Minuten ab;
*& - feste Obergrenzen (UP TO n ROWS);
*& - Personenfelder: seit 2026-10-05 nur die TA-Benutzer BNAME, ENAME, QNAME (HR-Freigabe
*&   laut Ingo 2026-10-05); das Cockpit zeigt sie nur nach Anmeldung. Kein PERNR.
*& - LogLiefSet ab 2026-10-05 bis heute + 14 Tage (Warenausgang nach Termin), weiterhin ein Tag.
*&
*& Nachtrag 2026-10-05 (Review, naechster Transport nach T76K912662):
*& - DDIC ZSTR_LOG_RUECK + Feld STZHL (AFRU-STZHL, NUMC 8), ZSTR_LOG_LIEF + Feld UEBERF (CHAR1);
*&   MPC: Property Stzhl (LogRueck) und Ueberf (LogLief, filterable) siehe ZLOG_LIVE_MPC_DEFINE_ADD.abap.
*&   Das Cockpit bleibt gegen den alten Stand lauffaehig (fehlt Stzhl, gilt nur STOKZ; Ueberf wird erst gefragt,
*&   nachdem P76 es kennt, ein HTTP 400 schaltet die Abfrage bis zum Neustart ab).
*& - LogLiefSet: Auswahl WADAT = Tag ODER WADAT_IST = Tag; Uhrzeit des Warenausgangs aus der VBFA-Zeile (Typ R)
*&   mit ERDAT = WADAT_IST, sonst CPUTM aus MKPF; ohne gebuchten Warenausgang keine Uhrzeit.
*& - LogLiefSet mit Ueberf = X: Termin in den 30 Tagen vor dem Datum, WBSTK <> C.
*& - LogTaSet: beim ersten Abruf des laufenden Tages auch offene Positionen (PQUIT leer) der letzten 14 Tage.
*& - Obergrenzen: jede Auswahl holt eine Zeile mehr; ueberschritten wird abgeschnitten und eine Warnung
*&   (sap-message Antwortkopf) gesetzt, statt still zu kuerzen. Signatur von ADD_MESSAGE_TEXT_ONLY in SE24 pruefen.
*&---------------------------------------------------------------------*

* Logistik live, docs/abap/ZLOG_LIVE_DPC_GET_ENTITYSET_ADD.abap

  IF iv_entity_set_name = 'LogTaSet'
  OR iv_entity_set_name = 'LogLiefSet'
  OR iv_entity_set_name = 'LogRueckSet'.

    TYPES: BEGIN OF ty_lg_ta_key,
             lgnum TYPE lgnum,
             tanum TYPE tanum,
           END OF ty_lg_ta_key,
           BEGIN OF ty_lg_ta_kopf,
             lgnum TYPE lgnum,
             tanum TYPE tanum,
             bwlvs TYPE bwlvs,
             bdatu TYPE ltak_bdatu,
             bzeit TYPE ltak_bzeit,
             vbeln TYPE vbeln,
             bname TYPE lvs_bname,
           END OF ty_lg_ta_kopf,
           BEGIN OF ty_lg_ta_pos,
             lgnum TYPE lgnum,
             tanum TYPE tanum,
             tapos TYPE tapos,
             matnr TYPE matnr,
             werks TYPE werks_d,
             vltyp TYPE ltap_vltyp,
             vlpla TYPE ltap_vlpla,
             nltyp TYPE ltap_nltyp,
             nlpla TYPE ltap_nlpla,
             nsolm TYPE ltap_nsolm,
             meins TYPE meins,
             pquit TYPE ltap_pquit,
             qdatu TYPE ltap_qdatu,
             qzeit TYPE ltap_qzeit,
             vbeln TYPE vbeln_vl,
             ename TYPE ltap_ename,
             qname TYPE ltap_qname,
           END OF ty_lg_ta_pos,
           BEGIN OF ty_lg_lf_kopf,
             vbeln TYPE vbeln_vl,
             lfart TYPE lfart,
             vstel TYPE vstel,
             lgnum TYPE lgnum,
             kunnr TYPE kunwe,
             kostk TYPE kostk,
             wbstk TYPE wbstk,
             erzet TYPE erzet,
             wadat TYPE wadak,
             wadat_ist TYPE wadat_ist,
           END OF ty_lg_lf_kopf,
           BEGIN OF ty_lg_lf_pos,
             vbeln TYPE vbeln_vl,
             posnr TYPE posnr_vl,
             kosta TYPE kosta,
           END OF ty_lg_lf_pos,
           BEGIN OF ty_lg_vbfa,
             vbelv TYPE vbeln_von,
             vbeln TYPE vbeln_nach,
             mjahr TYPE mjahr,
             erdat TYPE erdat,
             erzet TYPE erzet,
           END OF ty_lg_vbfa,
           BEGIN OF ty_lg_mkpf,
             mblnr TYPE mblnr,
             mjahr TYPE mjahr,
             cpudt TYPE cpudt,
             cputm TYPE cputm,
           END OF ty_lg_mkpf,
           BEGIN OF ty_lg_kunde,
             kunnr TYPE kunnr,
             name1 TYPE name1_gp,
           END OF ty_lg_kunde,
           BEGIN OF ty_lg_ru,
             rueck TYPE co_rueck,
             rmzhl TYPE co_rmzhl,
             aufnr TYPE aufnr,
             vornr TYPE vornr,
             arbid TYPE objektid,
             werks TYPE werks_d,
             ersda TYPE ru_ersda,
             erzet TYPE ru_erzet,
             lmnga TYPE ru_lmnga,
             xmnga TYPE ru_xmnga,
             meinh TYPE ru_vorme,
             aueru TYPE aueru_vs,
             stokz TYPE co_stokz,
             stzhl TYPE afru-stzhl,
           END OF ty_lg_ru,
           BEGIN OF ty_lg_ap,
             objid TYPE cr_objid,
             arbpl TYPE arbpl,
           END OF ty_lg_ap,
           BEGIN OF ty_lg_afko,
             aufnr TYPE aufnr,
             gamng TYPE gamng,
             igmng TYPE co_igmng,
           END OF ty_lg_afko,
           BEGIN OF ty_lg_afpo,
             aufnr TYPE aufnr,
             matnr TYPE co_matnr,
           END OF ty_lg_afpo,
           BEGIN OF ty_lg_aufnr,
             aufnr TYPE aufnr,
           END OF ty_lg_aufnr,
           BEGIN OF ty_lg_arbid,
             arbid TYPE objektid,
           END OF ty_lg_arbid.

    DATA: lv_lg_datum   TYPE datum,
          lv_lg_abzeit  TYPE uzeit VALUE '000000',
          lr_lg_lgnum   TYPE RANGE OF lgnum,
          ls_lg_lgnum   LIKE LINE OF lr_lg_lgnum,
          lr_lg_werks   TYPE RANGE OF werks_d,
          ls_lg_werks   LIKE LINE OF lr_lg_werks,
          lv_lg_frueh   TYPE datum,
          lv_lg_spaet   TYPE datum,
          lt_lg_vbfa    TYPE STANDARD TABLE OF ty_lg_vbfa,
          ls_lg_vbfa    TYPE ty_lg_vbfa,
          ls_lg_vbfa_gi TYPE ty_lg_vbfa,
          lt_lg_mkpf    TYPE SORTED TABLE OF ty_lg_mkpf WITH UNIQUE KEY mblnr mjahr,
          ls_lg_mkpf    TYPE ty_lg_mkpf,
          lv_lg_ueberf  TYPE c LENGTH 1,
          lv_lg_von     TYPE datum,
          lv_lg_vortag  TYPE datum,
          lv_lg_grenze  TYPE i,
          lv_lg_wazeit  TYPE uzeit,
          lt_lg_ta_alt  TYPE STANDARD TABLE OF ty_lg_ta_key,
          lv_lg_skip    TYPE i,
          lv_lg_max     TYPE i,
          lv_lg_zeile   TYPE i,
          lt_lg_ta_out  TYPE STANDARD TABLE OF zstr_log_ta,
          ls_lg_ta_out  TYPE zstr_log_ta,
          lt_lg_ta_keys TYPE STANDARD TABLE OF ty_lg_ta_key,
          lt_lg_ta_kopf TYPE SORTED TABLE OF ty_lg_ta_kopf WITH UNIQUE KEY lgnum tanum,
          ls_lg_ta_kopf TYPE ty_lg_ta_kopf,
          lt_lg_ta_pos  TYPE STANDARD TABLE OF ty_lg_ta_pos,
          ls_lg_ta_pos  TYPE ty_lg_ta_pos,
          lt_lg_lf_out  TYPE STANDARD TABLE OF zstr_log_lief,
          ls_lg_lf_out  TYPE zstr_log_lief,
          lt_lg_lf_kopf TYPE STANDARD TABLE OF ty_lg_lf_kopf,
          ls_lg_lf_kopf TYPE ty_lg_lf_kopf,
          lt_lg_lf_pos  TYPE SORTED TABLE OF ty_lg_lf_pos WITH NON-UNIQUE KEY vbeln,
          ls_lg_lf_pos  TYPE ty_lg_lf_pos,
          lt_lg_kunde   TYPE SORTED TABLE OF ty_lg_kunde WITH UNIQUE KEY kunnr,
          ls_lg_kunde   TYPE ty_lg_kunde,
          lt_lg_ru_out  TYPE STANDARD TABLE OF zstr_log_rueck,
          ls_lg_ru_out  TYPE zstr_log_rueck,
          lt_lg_ru      TYPE STANDARD TABLE OF ty_lg_ru,
          ls_lg_ru      TYPE ty_lg_ru,
          lt_lg_ap      TYPE SORTED TABLE OF ty_lg_ap WITH UNIQUE KEY objid,
          ls_lg_ap      TYPE ty_lg_ap,
          lt_lg_afko    TYPE SORTED TABLE OF ty_lg_afko WITH UNIQUE KEY aufnr,
          ls_lg_afko    TYPE ty_lg_afko,
          lt_lg_afpo    TYPE SORTED TABLE OF ty_lg_afpo WITH NON-UNIQUE KEY aufnr,
          ls_lg_afpo    TYPE ty_lg_afpo,
          lt_lg_aufnr   TYPE STANDARD TABLE OF ty_lg_aufnr,
          lt_lg_arbid   TYPE STANDARD TABLE OF ty_lg_arbid.

    FIELD-SYMBOLS: <ls_lg_filter> LIKE LINE OF it_filter_select_options,
                   <ls_lg_option> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_lg_filter>.
      LOOP AT <ls_lg_filter>-select_options ASSIGNING <ls_lg_option>
           WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_lg_filter>-property ).
          WHEN 'DATUM'.
            lv_lg_datum = <ls_lg_option>-low.
          WHEN 'ABZEIT'.
            lv_lg_abzeit = <ls_lg_option>-low.
          WHEN 'LGNUM'.
            ls_lg_lgnum-sign = 'I'. ls_lg_lgnum-option = 'EQ'. ls_lg_lgnum-low = <ls_lg_option>-low.
            APPEND ls_lg_lgnum TO lr_lg_lgnum.
          WHEN 'UEBERF'.   " nur LogLiefSet: ueberfaellige, noch nicht ausgelieferte Lieferungen
            lv_lg_ueberf = <ls_lg_option>-low.
          WHEN 'WERKS'.
            ls_lg_werks-sign = 'I'. ls_lg_werks-option = 'EQ'. ls_lg_werks-low = <ls_lg_option>-low.
            APPEND ls_lg_werks TO lr_lg_werks.
          WHEN OTHERS.  " bewusst ignoriert statt zu raten
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

*   Ohne gueltiges Datum kein einziger Datenbankzugriff (Schutz fuer P76).
    lv_lg_frueh = sy-datum - 400.
*   LogLiefSet darf bis 14 Tage voraus (Warenausgang nach Termin), weiterhin genau ein Tag.
    lv_lg_spaet = sy-datum.
    IF iv_entity_set_name = 'LogLiefSet'.
      lv_lg_spaet = sy-datum + 14.
    ENDIF.
    IF lv_lg_datum IS INITIAL OR lv_lg_datum > lv_lg_spaet OR lv_lg_datum < lv_lg_frueh.
      CASE iv_entity_set_name.
        WHEN 'LogTaSet'.
          copy_data_to_ref( EXPORTING is_data = lt_lg_ta_out CHANGING cr_data = er_entityset ).
        WHEN 'LogLiefSet'.
          copy_data_to_ref( EXPORTING is_data = lt_lg_lf_out CHANGING cr_data = er_entityset ).
        WHEN OTHERS.
          copy_data_to_ref( EXPORTING is_data = lt_lg_ru_out CHANGING cr_data = er_entityset ).
      ENDCASE.
      RETURN.
    ENDIF.

    CASE iv_entity_set_name.

      WHEN 'LogTaSet'.
*       Transportauftraege, die ab AbZeit angelegt ODER quittiert wurden.
*       Jede Auswahl holt eine Zeile mehr als erlaubt (5001): so ist Abschneiden erkennbar statt still.
        SELECT lgnum tanum FROM ltak INTO TABLE lt_lg_ta_keys UP TO 5001 ROWS
          WHERE lgnum IN lr_lg_lgnum
            AND bdatu = lv_lg_datum
            AND bzeit >= lv_lg_abzeit.
        SELECT lgnum tanum FROM ltap APPENDING TABLE lt_lg_ta_keys UP TO 5001 ROWS
          WHERE lgnum IN lr_lg_lgnum
            AND qdatu = lv_lg_datum
            AND qzeit >= lv_lg_abzeit.
*       Offene Positionen von frueheren Tagen (hoechstens 14 Tage zurueck): nur beim ersten Abruf des Tages
*       (AbZeit 000000) und nur fuer den laufenden Tag. Spaeter bleiben sie im Cockpit gemerkt; wird eine davon
*       quittiert, kommt sie ueber die Auswahl nach QDATU oben.
        IF lv_lg_abzeit = '000000' AND lv_lg_datum = sy-datum.
          lv_lg_von = lv_lg_datum - 14.
          lv_lg_vortag = lv_lg_datum - 1.
          SELECT DISTINCT ltak~lgnum ltak~tanum FROM ltak
            INNER JOIN ltap ON ltap~lgnum = ltak~lgnum AND ltap~tanum = ltak~tanum
            APPENDING CORRESPONDING FIELDS OF TABLE lt_lg_ta_alt UP TO 5001 ROWS
            WHERE ltak~lgnum IN lr_lg_lgnum
              AND ltak~bdatu BETWEEN lv_lg_von AND lv_lg_vortag
              AND ltap~pquit = space.
          APPEND LINES OF lt_lg_ta_alt TO lt_lg_ta_keys.
        ENDIF.
        SORT lt_lg_ta_keys BY lgnum tanum.
        DELETE ADJACENT DUPLICATES FROM lt_lg_ta_keys COMPARING lgnum tanum.
        DESCRIBE TABLE lt_lg_ta_keys LINES lv_lg_zeile.
        IF lv_lg_zeile > 5000.
          lv_lg_grenze = 5001.
          DELETE lt_lg_ta_keys FROM lv_lg_grenze.
          mo_context->get_message_container( )->add_message_text_only(
            iv_msg_type = 'W'
            iv_msg_text = 'LogTaSet: Obergrenze 5000 Transportauftraege erreicht, Antwort unvollstaendig'
            iv_add_to_response_header = abap_true ).
        ENDIF.

        IF lt_lg_ta_keys IS NOT INITIAL.
          SELECT lgnum tanum bwlvs bdatu bzeit vbeln bname FROM ltak
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_ta_kopf
            FOR ALL ENTRIES IN lt_lg_ta_keys
            WHERE lgnum = lt_lg_ta_keys-lgnum
              AND tanum = lt_lg_ta_keys-tanum.
          SELECT lgnum tanum tapos matnr werks vltyp vlpla nltyp nlpla nsolm meins
                 pquit qdatu qzeit vbeln ename qname FROM ltap
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_ta_pos
            FOR ALL ENTRIES IN lt_lg_ta_keys
            WHERE lgnum = lt_lg_ta_keys-lgnum
              AND tanum = lt_lg_ta_keys-tanum.
        ENDIF.

        LOOP AT lt_lg_ta_pos INTO ls_lg_ta_pos.
          CLEAR ls_lg_ta_out.
          ls_lg_ta_out-lgnum  = ls_lg_ta_pos-lgnum.
          ls_lg_ta_out-tanum  = ls_lg_ta_pos-tanum.
          ls_lg_ta_out-tapos  = ls_lg_ta_pos-tapos.
          ls_lg_ta_out-datum  = lv_lg_datum.
          ls_lg_ta_out-abzeit = lv_lg_abzeit.
          ls_lg_ta_out-matnr  = ls_lg_ta_pos-matnr.
          ls_lg_ta_out-werks  = ls_lg_ta_pos-werks.
          ls_lg_ta_out-vltyp  = ls_lg_ta_pos-vltyp.
          ls_lg_ta_out-vlpla  = ls_lg_ta_pos-vlpla.
          ls_lg_ta_out-nltyp  = ls_lg_ta_pos-nltyp.
          ls_lg_ta_out-nlpla  = ls_lg_ta_pos-nlpla.
          ls_lg_ta_out-menge  = ls_lg_ta_pos-nsolm.
          ls_lg_ta_out-meins  = ls_lg_ta_pos-meins.
          ls_lg_ta_out-pquit  = ls_lg_ta_pos-pquit.
          ls_lg_ta_out-qdatu  = ls_lg_ta_pos-qdatu.
          ls_lg_ta_out-qzeit  = ls_lg_ta_pos-qzeit.
          ls_lg_ta_out-vbeln  = ls_lg_ta_pos-vbeln.
          ls_lg_ta_out-ename  = ls_lg_ta_pos-ename.
          ls_lg_ta_out-qname  = ls_lg_ta_pos-qname.
          READ TABLE lt_lg_ta_kopf INTO ls_lg_ta_kopf
               WITH TABLE KEY lgnum = ls_lg_ta_pos-lgnum tanum = ls_lg_ta_pos-tanum.
          IF sy-subrc = 0.
            ls_lg_ta_out-bwlvs = ls_lg_ta_kopf-bwlvs.
            ls_lg_ta_out-bdatu = ls_lg_ta_kopf-bdatu.
            ls_lg_ta_out-bzeit = ls_lg_ta_kopf-bzeit.
            ls_lg_ta_out-bname = ls_lg_ta_kopf-bname.
            IF ls_lg_ta_out-vbeln IS INITIAL.
              ls_lg_ta_out-vbeln = ls_lg_ta_kopf-vbeln.
            ENDIF.
          ENDIF.
          APPEND ls_lg_ta_out TO lt_lg_ta_out.
        ENDLOOP.
        SORT lt_lg_ta_out BY lgnum tanum tapos.

        IF is_paging-skip > 0.
          lv_lg_skip = is_paging-skip.
          DELETE lt_lg_ta_out TO lv_lg_skip.
        ENDIF.
        IF is_paging-top > 0.
          lv_lg_max = is_paging-top.
          DESCRIBE TABLE lt_lg_ta_out LINES lv_lg_zeile.
          IF lv_lg_zeile > lv_lg_max.
            lv_lg_max = lv_lg_max + 1.
            DELETE lt_lg_ta_out FROM lv_lg_max.
          ENDIF.
        ENDIF.
        copy_data_to_ref( EXPORTING is_data = lt_lg_ta_out CHANGING cr_data = er_entityset ).

      WHEN 'LogLiefSet'.
*       Lieferungen mit geplantem Warenausgang am Datum, Kommissionierstand je Position.
        IF lv_lg_ueberf = 'X'.
*         Ueberfaellig: geplanter Warenausgang in den letzten 30 Tagen vor dem Datum, Warenausgang nicht gebucht.
          lv_lg_von = lv_lg_datum - 30.
          lv_lg_vortag = lv_lg_datum - 1.
          SELECT vbeln lfart vstel lgnum kunnr kostk wbstk erzet wadat wadat_ist FROM likp
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_lf_kopf UP TO 2001 ROWS
            WHERE wadat BETWEEN lv_lg_von AND lv_lg_vortag
              AND wbstk <> 'C'
              AND lgnum IN lr_lg_lgnum.
        ELSE.
*         Geplanter Warenausgang am Datum ODER tatsaechlich gebucht am Datum (WADAT_IST): eine Lieferung, die
*         an einem anderen Tag geplant war, erscheint sonst am Tag ihrer Buchung nicht. Zwei Auswahlen statt OR,
*         damit jede ihren Index nutzt.
          SELECT vbeln lfart vstel lgnum kunnr kostk wbstk erzet wadat wadat_ist FROM likp
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_lf_kopf UP TO 2001 ROWS
            WHERE wadat = lv_lg_datum
              AND lgnum IN lr_lg_lgnum.
          IF lv_lg_datum <= sy-datum.
            SELECT vbeln lfart vstel lgnum kunnr kostk wbstk erzet wadat wadat_ist FROM likp
              APPENDING CORRESPONDING FIELDS OF TABLE lt_lg_lf_kopf UP TO 2001 ROWS
              WHERE wadat_ist = lv_lg_datum
                AND lgnum IN lr_lg_lgnum.
          ENDIF.
          SORT lt_lg_lf_kopf BY vbeln.
          DELETE ADJACENT DUPLICATES FROM lt_lg_lf_kopf COMPARING vbeln.
        ENDIF.
        DESCRIBE TABLE lt_lg_lf_kopf LINES lv_lg_zeile.
        IF lv_lg_zeile > 2000.
          lv_lg_grenze = 2001.
          DELETE lt_lg_lf_kopf FROM lv_lg_grenze.
          mo_context->get_message_container( )->add_message_text_only(
            iv_msg_type = 'W'
            iv_msg_text = 'LogLiefSet: Obergrenze 2000 Lieferungen erreicht, Antwort unvollstaendig'
            iv_add_to_response_header = abap_true ).
        ENDIF.

        IF lt_lg_lf_kopf IS NOT INITIAL.
          SELECT vbeln posnr kosta FROM lips
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_lf_pos
            FOR ALL ENTRIES IN lt_lg_lf_kopf
            WHERE vbeln = lt_lg_lf_kopf-vbeln.
          SELECT kunnr name1 FROM kna1
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_kunde
            FOR ALL ENTRIES IN lt_lg_lf_kopf
            WHERE kunnr = lt_lg_lf_kopf-kunnr.
*         Uhrzeit der Warenbewegung (Folgebeleg Typ R = Materialbeleg), juengste zuerst. Gewaehlt wird weiter unten
*         die Zeile, deren ERDAT dem tatsaechlichen Warenausgangsdatum (WADAT_IST) entspricht; Teilbuchungen oder
*         Stornos an anderen Tagen liefern sonst die falsche Uhrzeit. Fehlt ERZET dort, kommt CPUTM aus MKPF.
          SELECT vbelv vbeln mjahr erdat erzet FROM vbfa
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_vbfa
            FOR ALL ENTRIES IN lt_lg_lf_kopf
            WHERE vbelv = lt_lg_lf_kopf-vbeln
              AND vbtyp_n = 'R'.
          SORT lt_lg_vbfa BY vbelv ASCENDING erdat DESCENDING erzet DESCENDING.
          IF lt_lg_vbfa IS NOT INITIAL.
            SELECT mblnr mjahr cpudt cputm FROM mkpf
              INTO CORRESPONDING FIELDS OF TABLE lt_lg_mkpf
              FOR ALL ENTRIES IN lt_lg_vbfa
              WHERE mblnr = lt_lg_vbfa-vbeln
                AND mjahr = lt_lg_vbfa-mjahr.
          ENDIF.
        ENDIF.

        LOOP AT lt_lg_lf_kopf INTO ls_lg_lf_kopf.
          CLEAR ls_lg_lf_out.
          ls_lg_lf_out-vbeln = ls_lg_lf_kopf-vbeln.
          ls_lg_lf_out-datum = lv_lg_datum.
          ls_lg_lf_out-lfart = ls_lg_lf_kopf-lfart.
          ls_lg_lf_out-vstel = ls_lg_lf_kopf-vstel.
          ls_lg_lf_out-lgnum = ls_lg_lf_kopf-lgnum.
          ls_lg_lf_out-kunnr = ls_lg_lf_kopf-kunnr.
          ls_lg_lf_out-kostk = ls_lg_lf_kopf-kostk.
          ls_lg_lf_out-wbstk = ls_lg_lf_kopf-wbstk.
          ls_lg_lf_out-erzet = ls_lg_lf_kopf-erzet.
          ls_lg_lf_out-wadat = ls_lg_lf_kopf-wadat.
          ls_lg_lf_out-wadat_ist = ls_lg_lf_kopf-wadat_ist.
          ls_lg_lf_out-ueberf = lv_lg_ueberf.
*         Nur wenn der Warenausgang gebucht ist: Zeile mit ERDAT = WADAT_IST, sonst keine Uhrzeit.
          CLEAR: ls_lg_vbfa_gi, lv_lg_wazeit.
          IF ls_lg_lf_kopf-wadat_ist IS NOT INITIAL.
            LOOP AT lt_lg_vbfa INTO ls_lg_vbfa
                 WHERE vbelv = ls_lg_lf_kopf-vbeln AND erdat = ls_lg_lf_kopf-wadat_ist.
              ls_lg_vbfa_gi = ls_lg_vbfa.   " juengste zuerst: die erste Zeile genuegt
              EXIT.
            ENDLOOP.
            IF ls_lg_vbfa_gi-vbelv IS NOT INITIAL.
              lv_lg_wazeit = ls_lg_vbfa_gi-erzet.
              IF lv_lg_wazeit IS INITIAL.
                READ TABLE lt_lg_mkpf INTO ls_lg_mkpf
                     WITH TABLE KEY mblnr = ls_lg_vbfa_gi-vbeln mjahr = ls_lg_vbfa_gi-mjahr.
                IF sy-subrc = 0 AND ls_lg_mkpf-cpudt = ls_lg_lf_kopf-wadat_ist.
                  lv_lg_wazeit = ls_lg_mkpf-cputm.
                ENDIF.
              ENDIF.
            ENDIF.
          ENDIF.
          ls_lg_lf_out-wa_zeit = lv_lg_wazeit.
          READ TABLE lt_lg_kunde INTO ls_lg_kunde WITH TABLE KEY kunnr = ls_lg_lf_kopf-kunnr.
          IF sy-subrc = 0.
            ls_lg_lf_out-name1 = ls_lg_kunde-name1.
          ENDIF.
*         Nur kommissionierrelevante Positionen zaehlen (KOSTA nicht leer).
          LOOP AT lt_lg_lf_pos INTO ls_lg_lf_pos WHERE vbeln = ls_lg_lf_kopf-vbeln.
            IF ls_lg_lf_pos-kosta IS INITIAL.
              CONTINUE.
            ENDIF.
            ls_lg_lf_out-pos_ges = ls_lg_lf_out-pos_ges + 1.
            CASE ls_lg_lf_pos-kosta.
              WHEN 'C'.
                ls_lg_lf_out-pos_komm = ls_lg_lf_out-pos_komm + 1.
              WHEN 'B'.
                ls_lg_lf_out-pos_teil = ls_lg_lf_out-pos_teil + 1.
              WHEN OTHERS.
            ENDCASE.
          ENDLOOP.
          APPEND ls_lg_lf_out TO lt_lg_lf_out.
        ENDLOOP.
        SORT lt_lg_lf_out BY vbeln.

        IF is_paging-skip > 0.
          lv_lg_skip = is_paging-skip.
          DELETE lt_lg_lf_out TO lv_lg_skip.
        ENDIF.
        IF is_paging-top > 0.
          lv_lg_max = is_paging-top.
          DESCRIBE TABLE lt_lg_lf_out LINES lv_lg_zeile.
          IF lv_lg_zeile > lv_lg_max.
            lv_lg_max = lv_lg_max + 1.
            DELETE lt_lg_lf_out FROM lv_lg_max.
          ENDIF.
        ENDIF.
        copy_data_to_ref( EXPORTING is_data = lt_lg_lf_out CHANGING cr_data = er_entityset ).

      WHEN OTHERS.  " LogRueckSet
*       Rueckmeldungen ab AbZeit, ohne Personalnummer.
*       STZHL: Stornozaehler; ungleich 0 heisst storniert bzw. Stornosatz (zeigt auf das Original, dessen
*       Zeitstempel alt ist und das ein inkrementeller Abruf deshalb nicht mehr liefert).
        SELECT rueck rmzhl aufnr vornr arbid werks ersda erzet lmnga xmnga meinh aueru stokz stzhl
          FROM afru INTO CORRESPONDING FIELDS OF TABLE lt_lg_ru UP TO 10001 ROWS
          WHERE ersda = lv_lg_datum
            AND erzet >= lv_lg_abzeit
            AND werks IN lr_lg_werks.
        DESCRIBE TABLE lt_lg_ru LINES lv_lg_zeile.
        IF lv_lg_zeile > 10000.
          lv_lg_grenze = 10001.
          DELETE lt_lg_ru FROM lv_lg_grenze.
          mo_context->get_message_container( )->add_message_text_only(
            iv_msg_type = 'W'
            iv_msg_text = 'LogRueckSet: Obergrenze 10000 Rueckmeldungen erreicht, Antwort unvollstaendig'
            iv_add_to_response_header = abap_true ).
        ENDIF.

        IF lt_lg_ru IS NOT INITIAL.
          LOOP AT lt_lg_ru INTO ls_lg_ru.
            APPEND VALUE ty_lg_aufnr( aufnr = ls_lg_ru-aufnr ) TO lt_lg_aufnr.
            APPEND VALUE ty_lg_arbid( arbid = ls_lg_ru-arbid ) TO lt_lg_arbid.
          ENDLOOP.
          SORT lt_lg_aufnr BY aufnr.
          DELETE ADJACENT DUPLICATES FROM lt_lg_aufnr COMPARING aufnr.
          SORT lt_lg_arbid BY arbid.
          DELETE ADJACENT DUPLICATES FROM lt_lg_arbid COMPARING arbid.

          SELECT objid arbpl FROM crhd INTO CORRESPONDING FIELDS OF TABLE lt_lg_ap
            FOR ALL ENTRIES IN lt_lg_arbid
            WHERE objty = 'A'
              AND objid = lt_lg_arbid-arbid.
          SELECT aufnr gamng igmng FROM afko INTO CORRESPONDING FIELDS OF TABLE lt_lg_afko
            FOR ALL ENTRIES IN lt_lg_aufnr
            WHERE aufnr = lt_lg_aufnr-aufnr.
          SELECT aufnr matnr FROM afpo INTO CORRESPONDING FIELDS OF TABLE lt_lg_afpo
            FOR ALL ENTRIES IN lt_lg_aufnr
            WHERE aufnr = lt_lg_aufnr-aufnr
              AND posnr = '0001'.
        ENDIF.

        LOOP AT lt_lg_ru INTO ls_lg_ru.
          CLEAR ls_lg_ru_out.
          ls_lg_ru_out-rueck  = ls_lg_ru-rueck.
          ls_lg_ru_out-rmzhl  = ls_lg_ru-rmzhl.
          ls_lg_ru_out-datum  = lv_lg_datum.
          ls_lg_ru_out-abzeit = lv_lg_abzeit.
          ls_lg_ru_out-aufnr  = ls_lg_ru-aufnr.
          ls_lg_ru_out-vornr  = ls_lg_ru-vornr.
          ls_lg_ru_out-werks  = ls_lg_ru-werks.
          ls_lg_ru_out-ersda  = ls_lg_ru-ersda.
          ls_lg_ru_out-erzet  = ls_lg_ru-erzet.
          ls_lg_ru_out-lmnga  = ls_lg_ru-lmnga.
          ls_lg_ru_out-xmnga  = ls_lg_ru-xmnga.
          ls_lg_ru_out-meinh  = ls_lg_ru-meinh.
          ls_lg_ru_out-aueru  = ls_lg_ru-aueru.
          ls_lg_ru_out-stokz  = ls_lg_ru-stokz.
          ls_lg_ru_out-stzhl  = ls_lg_ru-stzhl.
          READ TABLE lt_lg_ap INTO ls_lg_ap WITH TABLE KEY objid = ls_lg_ru-arbid.
          IF sy-subrc = 0.
            ls_lg_ru_out-arbpl = ls_lg_ap-arbpl.
          ENDIF.
          READ TABLE lt_lg_afko INTO ls_lg_afko WITH TABLE KEY aufnr = ls_lg_ru-aufnr.
          IF sy-subrc = 0.
            ls_lg_ru_out-gamng = ls_lg_afko-gamng.
            ls_lg_ru_out-igmng = ls_lg_afko-igmng.
          ENDIF.
          READ TABLE lt_lg_afpo INTO ls_lg_afpo WITH KEY aufnr = ls_lg_ru-aufnr.
          IF sy-subrc = 0.
            ls_lg_ru_out-matnr = ls_lg_afpo-matnr.
          ENDIF.
          APPEND ls_lg_ru_out TO lt_lg_ru_out.
        ENDLOOP.
        SORT lt_lg_ru_out BY ersda erzet rueck rmzhl.

        IF is_paging-skip > 0.
          lv_lg_skip = is_paging-skip.
          DELETE lt_lg_ru_out TO lv_lg_skip.
        ENDIF.
        IF is_paging-top > 0.
          lv_lg_max = is_paging-top.
          DESCRIBE TABLE lt_lg_ru_out LINES lv_lg_zeile.
          IF lv_lg_zeile > lv_lg_max.
            lv_lg_max = lv_lg_max + 1.
            DELETE lt_lg_ru_out FROM lv_lg_max.
          ENDIF.
        ENDIF.
        copy_data_to_ref( EXPORTING is_data = lt_lg_ru_out CHANGING cr_data = er_entityset ).
    ENDCASE.
    RETURN.
  ENDIF.
