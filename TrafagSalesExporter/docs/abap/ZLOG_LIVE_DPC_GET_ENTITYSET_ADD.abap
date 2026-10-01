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
*& - keine Personenfelder (kein PERNR, kein Benutzer; Entscheid Ingo).
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
           END OF ty_lg_lf_kopf,
           BEGIN OF ty_lg_lf_pos,
             vbeln TYPE vbeln_vl,
             posnr TYPE posnr_vl,
             kosta TYPE kosta,
           END OF ty_lg_lf_pos,
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
          WHEN 'WERKS'.
            ls_lg_werks-sign = 'I'. ls_lg_werks-option = 'EQ'. ls_lg_werks-low = <ls_lg_option>-low.
            APPEND ls_lg_werks TO lr_lg_werks.
          WHEN OTHERS.  " bewusst ignoriert statt zu raten
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

*   Ohne gueltiges Datum kein einziger Datenbankzugriff (Schutz fuer P76).
    lv_lg_frueh = sy-datum - 400.
    IF lv_lg_datum IS INITIAL OR lv_lg_datum > sy-datum OR lv_lg_datum < lv_lg_frueh.
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
        SELECT lgnum tanum FROM ltak INTO TABLE lt_lg_ta_keys UP TO 5000 ROWS
          WHERE lgnum IN lr_lg_lgnum
            AND bdatu = lv_lg_datum
            AND bzeit >= lv_lg_abzeit.
        SELECT lgnum tanum FROM ltap APPENDING TABLE lt_lg_ta_keys UP TO 5000 ROWS
          WHERE lgnum IN lr_lg_lgnum
            AND qdatu = lv_lg_datum
            AND qzeit >= lv_lg_abzeit.
        SORT lt_lg_ta_keys BY lgnum tanum.
        DELETE ADJACENT DUPLICATES FROM lt_lg_ta_keys COMPARING lgnum tanum.

        IF lt_lg_ta_keys IS NOT INITIAL.
          SELECT lgnum tanum bwlvs bdatu bzeit vbeln FROM ltak
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_ta_kopf
            FOR ALL ENTRIES IN lt_lg_ta_keys
            WHERE lgnum = lt_lg_ta_keys-lgnum
              AND tanum = lt_lg_ta_keys-tanum.
          SELECT lgnum tanum tapos matnr werks vltyp vlpla nltyp nlpla nsolm meins
                 pquit qdatu qzeit vbeln FROM ltap
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
          READ TABLE lt_lg_ta_kopf INTO ls_lg_ta_kopf
               WITH TABLE KEY lgnum = ls_lg_ta_pos-lgnum tanum = ls_lg_ta_pos-tanum.
          IF sy-subrc = 0.
            ls_lg_ta_out-bwlvs = ls_lg_ta_kopf-bwlvs.
            ls_lg_ta_out-bdatu = ls_lg_ta_kopf-bdatu.
            ls_lg_ta_out-bzeit = ls_lg_ta_kopf-bzeit.
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
        SELECT vbeln lfart vstel lgnum kunnr kostk wbstk erzet FROM likp
          INTO CORRESPONDING FIELDS OF TABLE lt_lg_lf_kopf UP TO 2000 ROWS
          WHERE wadat = lv_lg_datum
            AND lgnum IN lr_lg_lgnum.

        IF lt_lg_lf_kopf IS NOT INITIAL.
          SELECT vbeln posnr kosta FROM lips
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_lf_pos
            FOR ALL ENTRIES IN lt_lg_lf_kopf
            WHERE vbeln = lt_lg_lf_kopf-vbeln.
          SELECT kunnr name1 FROM kna1
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_kunde
            FOR ALL ENTRIES IN lt_lg_lf_kopf
            WHERE kunnr = lt_lg_lf_kopf-kunnr.
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
        SELECT rueck rmzhl aufnr vornr arbid werks ersda erzet lmnga xmnga meinh aueru stokz
          FROM afru INTO CORRESPONDING FIELDS OF TABLE lt_lg_ru UP TO 10000 ROWS
          WHERE ersda = lv_lg_datum
            AND erzet >= lv_lg_abzeit
            AND werks IN lr_lg_werks.

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
