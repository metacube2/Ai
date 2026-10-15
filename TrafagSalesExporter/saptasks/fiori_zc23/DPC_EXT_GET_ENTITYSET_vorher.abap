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

* Logistik live, docs/abap/ZLOG_LIVE_DPC_GET_ENTITYSET_ADD.abap

* Einkauf Kontrakte und LZ-Codes, docs/abap/ZEINK_KONTRAKT_ADD.abap
  IF iv_entity_set_name = 'EinkKontraktSet'.
    TYPES: BEGIN OF ty_ek_pos,
             ebeln TYPE ekko-ebeln,
             bukrs TYPE ekko-bukrs,
             bsart TYPE ekko-bsart,
             lifnr TYPE ekko-lifnr,
             waers TYPE ekko-waers,
             wkurs TYPE ekko-wkurs,
             kdatb TYPE ekko-kdatb,
             kdate TYPE ekko-kdate,
             ebelp TYPE ekpo-ebelp,
             matnr TYPE ekpo-matnr,
             txz01 TYPE ekpo-txz01,
             matkl TYPE ekpo-matkl,
             meins TYPE ekpo-meins,
             ktmng TYPE ekpo-ktmng,
             netpr TYPE ekpo-netpr,
             peinh TYPE ekpo-peinh,
             zwert TYPE ekpo-zwert,
           END OF ty_ek_pos.
    TYPES: BEGIN OF ty_ek_abr,
             ebeln TYPE ekab-ebeln,
             ebelp TYPE ekab-ebelp,
             konnr TYPE ekab-konnr,
             ktpnr TYPE ekab-ktpnr,
             menge TYPE ekab-menge,
             netwr TYPE ekab-netwr,
           END OF ty_ek_abr.
    TYPES: BEGIN OF ty_ek_sum,
             konnr TYPE ekab-konnr,
             ktpnr TYPE ekab-ktpnr,
             menge TYPE ekab-menge,
             netwr TYPE ekab-netwr,
           END OF ty_ek_sum.
    DATA: lt_ek_out   TYPE STANDARD TABLE OF zstr_eink_kontrakt,
          ls_ek_out   TYPE zstr_eink_kontrakt,
          lt_ek_pos   TYPE STANDARD TABLE OF ty_ek_pos,
          ls_ek_pos   TYPE ty_ek_pos,
          lt_ek_abr   TYPE STANDARD TABLE OF ty_ek_abr,
          ls_ek_abr   TYPE ty_ek_abr,
          lt_ek_sum   TYPE HASHED TABLE OF ty_ek_sum WITH UNIQUE KEY konnr ktpnr,
          ls_ek_sum   TYPE ty_ek_sum,
          lr_ek_bukrs TYPE RANGE OF bukrs,
          ls_ek_bukrs LIKE LINE OF lr_ek_bukrs,
          lv_ek_max   TYPE i.
    FIELD-SYMBOLS: <ls_ek_f> LIKE LINE OF it_filter_select_options,
                   <ls_ek_o> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_ek_f>.
      LOOP AT <ls_ek_f>-select_options ASSIGNING <ls_ek_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_ek_f>-property ).
          WHEN 'BUKRS'.
            ls_ek_bukrs-sign = 'I'.
            ls_ek_bukrs-option = 'EQ'.
            ls_ek_bukrs-low = <ls_ek_o>-low.
            APPEND ls_ek_bukrs TO lr_ek_bukrs.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

    SELECT k~ebeln k~bukrs k~bsart k~lifnr k~waers k~wkurs k~kdatb k~kdate
           p~ebelp p~matnr p~txz01 p~matkl p~meins p~ktmng p~netpr p~peinh p~zwert
      FROM ekko AS k INNER JOIN ekpo AS p ON p~ebeln = k~ebeln
      INTO TABLE lt_ek_pos UP TO 20000 ROWS
      WHERE k~bstyp = 'K'
        AND k~loekz = space
        AND p~loekz = space
        AND k~bukrs IN lr_ek_bukrs.

    IF lt_ek_pos IS NOT INITIAL.
*     Abrufbeleg (EBELN/EBELP) mitlesen: FOR ALL ENTRIES entfernt sonst gleiche Zeilen und zaehlt zu wenig.
      SELECT ebeln ebelp konnr ktpnr menge netwr FROM ekab INTO TABLE lt_ek_abr
        FOR ALL ENTRIES IN lt_ek_pos
        WHERE konnr = lt_ek_pos-ebeln
          AND ktpnr = lt_ek_pos-ebelp
          AND loekz = space.
      LOOP AT lt_ek_abr INTO ls_ek_abr.
        ls_ek_sum-konnr = ls_ek_abr-konnr.
        ls_ek_sum-ktpnr = ls_ek_abr-ktpnr.
        ls_ek_sum-menge = ls_ek_abr-menge.
        ls_ek_sum-netwr = ls_ek_abr-netwr.
        COLLECT ls_ek_sum INTO lt_ek_sum.
      ENDLOOP.
    ENDIF.

    LOOP AT lt_ek_pos INTO ls_ek_pos.
      CLEAR ls_ek_out.
      ls_ek_out-ebeln = ls_ek_pos-ebeln.
      ls_ek_out-ebelp = ls_ek_pos-ebelp.
      ls_ek_out-bukrs = ls_ek_pos-bukrs.
      ls_ek_out-bsart = ls_ek_pos-bsart.
      ls_ek_out-lifnr = ls_ek_pos-lifnr.
      ls_ek_out-matnr = ls_ek_pos-matnr.
      ls_ek_out-txz01 = ls_ek_pos-txz01.
      ls_ek_out-matkl = ls_ek_pos-matkl.
      ls_ek_out-waers = ls_ek_pos-waers.
      ls_ek_out-wkurs = |{ ls_ek_pos-wkurs }|.
      ls_ek_out-kdatb = ls_ek_pos-kdatb.
      ls_ek_out-kdate = ls_ek_pos-kdate.
      ls_ek_out-meins = ls_ek_pos-meins.
      ls_ek_out-ktmng = |{ ls_ek_pos-ktmng }|.
      ls_ek_out-netpr = |{ ls_ek_pos-netpr }|.
      ls_ek_out-peinh = |{ ls_ek_pos-peinh }|.
      ls_ek_out-zwert = |{ ls_ek_pos-zwert }|.
      CLEAR ls_ek_sum.
      READ TABLE lt_ek_sum INTO ls_ek_sum WITH TABLE KEY konnr = ls_ek_pos-ebeln ktpnr = ls_ek_pos-ebelp.
      ls_ek_out-abmng = |{ ls_ek_sum-menge }|.
      ls_ek_out-abwrt = |{ ls_ek_sum-netwr }|.
      APPEND ls_ek_out TO lt_ek_out.
    ENDLOOP.

    SORT lt_ek_out BY ebeln ebelp.
    IF is_paging-skip > 0.
      DELETE lt_ek_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_ek_max = is_paging-top + 1.
      DELETE lt_ek_out FROM lv_ek_max.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_ek_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

  IF iv_entity_set_name = 'EinkMatLzSet'.
    DATA: lt_lz_out TYPE STANDARD TABLE OF zstr_eink_matlz,
          lv_lz_max TYPE i.
    SELECT matnr zzlzcod zzlzcodsort FROM mara INTO TABLE lt_lz_out
      WHERE zzlzcod <> space OR zzlzcodsort <> space.
    SORT lt_lz_out BY matnr.
    IF is_paging-skip > 0.
      DELETE lt_lz_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_lz_max = is_paging-top + 1.
      DELETE lt_lz_out FROM lv_lz_max.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_lz_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

* Operations (Shopfloor), docs/abap/ZSHOP_ADD.abap
  IF iv_entity_set_name = 'ShopZd05Set'.
    TYPES: BEGIN OF ty_sh_bd,
             bdter TYPE d,
             plumi TYPE c LENGTH 1,
             bdmng TYPE p LENGTH 13 DECIMALS 3,
           END OF ty_sh_bd.
    DATA: lt_sh_out   TYPE STANDARD TABLE OF zstr_shop_zd05,
          ls_sh_out   TYPE zstr_shop_zd05,
          lv_sh_werks TYPE werks_d,
          lv_sh_dispo TYPE dispo,
          lt_sh_marc  TYPE STANDARD TABLE OF marc,
          ls_sh_marc  TYPE marc,
          lt_sh_bd    TYPE STANDARD TABLE OF ty_sh_bd,
          ls_sh_bd    TYPE ty_sh_bd,
          lt_sh_resb  TYPE STANDARD TABLE OF resb,
          ls_sh_resb  TYPE resb,
          lt_sh_vbbe  TYPE STANDARD TABLE OF vbbe,
          ls_sh_vbbe  TYPE vbbe,
          lt_sh_ekpo  TYPE STANDARD TABLE OF ekpo,
          ls_sh_ekpo  TYPE ekpo,
          lt_sh_eket  TYPE STANDARD TABLE OF eket,
          ls_sh_eket  TYPE eket,
          lt_sh_verb  TYPE STANDARD TABLE OF iverb,
          ls_sh_verb  TYPE iverb,
          lv_sh_lbkum TYPE mbew-lbkum,
          lv_sh_ende  TYPE d,
          lv_sh_udek  TYPE d,
          lv_sh_umt   TYPE abap_bool,
          lv_sh_von   TYPE d,
          lv_sh_bis   TYPE d,
          lv_sh_verbr TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_q1    TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_q2    TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_q3    TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_q4    TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_vbwbz TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_opt   TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_ant   TYPE p LENGTH 15 DECIMALS 3,
          lv_sh_idx   TYPE i,
          lv_sh_skip  TYPE i,
          lv_sh_max   TYPE i.
    FIELD-SYMBOLS: <ls_sh_f> LIKE LINE OF it_filter_select_options,
                   <ls_sh_o> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_sh_f>.
      LOOP AT <ls_sh_f>-select_options ASSIGNING <ls_sh_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_sh_f>-property ).
          WHEN 'WERKS'. lv_sh_werks = <ls_sh_o>-low.
          WHEN 'DISPO'. lv_sh_dispo = <ls_sh_o>-low.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.
    IF lv_sh_werks IS INITIAL OR lv_sh_dispo IS INITIAL.
      copy_data_to_ref( EXPORTING is_data = lt_sh_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.

*   Verbrauchszeitraum wie ZD05: Ende Vormonat, 360 Tage zurueck, auf Monatsanfang
    lv_sh_bis = sy-datum.
    WHILE lv_sh_bis+4(2) = sy-datum+4(2).
      lv_sh_bis = lv_sh_bis - 1.
    ENDWHILE.
    lv_sh_von = lv_sh_bis - 360.
    lv_sh_von+6(2) = '01'.

    SELECT * FROM marc INTO TABLE lt_sh_marc UP TO 4000 ROWS
      WHERE werks = lv_sh_werks
        AND dispo = lv_sh_dispo
        AND disgr = '0011'.

    LOOP AT lt_sh_marc INTO ls_sh_marc.
      CLEAR: ls_sh_out, lv_sh_udek, lv_sh_umt.
      lv_sh_ende = sy-datum + ls_sh_marc-plifz.
      SELECT SINGLE lbkum FROM mbew INTO lv_sh_lbkum
        WHERE matnr = ls_sh_marc-matnr AND bwkey = ls_sh_marc-werks AND bwtar = space.
      CHECK sy-subrc = 0.

      REFRESH lt_sh_bd.
      SELECT * FROM resb INTO TABLE lt_sh_resb
        WHERE matnr = ls_sh_marc-matnr AND werks = ls_sh_marc-werks
          AND xloek = space AND kzear = space AND bdter <= lv_sh_ende
          AND schgt = space AND dumps = space AND shkzg = 'H'.
      LOOP AT lt_sh_resb INTO ls_sh_resb.
        ls_sh_bd-bdter = ls_sh_resb-bdter.
        ls_sh_bd-bdmng = ls_sh_resb-bdmng - ls_sh_resb-enmng.
        ls_sh_bd-plumi = '2'.
        APPEND ls_sh_bd TO lt_sh_bd.
      ENDLOOP.
      SELECT * FROM vbbe INTO TABLE lt_sh_vbbe
        WHERE matnr = ls_sh_marc-matnr AND werks = ls_sh_marc-werks
          AND sobkz = space AND mbdat <= lv_sh_ende.
      LOOP AT lt_sh_vbbe INTO ls_sh_vbbe.
        ls_sh_bd-bdter = ls_sh_vbbe-mbdat.
        ls_sh_bd-bdmng = ls_sh_vbbe-omeng.
        ls_sh_bd-plumi = '2'.
        APPEND ls_sh_bd TO lt_sh_bd.
      ENDLOOP.
      SELECT * FROM ekpo INTO TABLE lt_sh_ekpo
        WHERE matnr = ls_sh_marc-matnr AND werks = ls_sh_marc-werks
          AND ( bstyp = 'F' OR bstyp = 'L' ) AND loekz = space AND elikz = space.
      LOOP AT lt_sh_ekpo INTO ls_sh_ekpo.
        SELECT * FROM eket INTO TABLE lt_sh_eket
          WHERE ebeln = ls_sh_ekpo-ebeln AND ebelp = ls_sh_ekpo-ebelp.
        LOOP AT lt_sh_eket INTO ls_sh_eket.
          CHECK ls_sh_eket-wemng < ls_sh_eket-menge.
          ls_sh_bd-bdter = ls_sh_eket-eindt.
          ls_sh_bd-bdmng = ls_sh_eket-menge - ls_sh_eket-wemng.
          ls_sh_bd-plumi = '1'.
          APPEND ls_sh_bd TO lt_sh_bd.
        ENDLOOP.
      ENDLOOP.

      SORT lt_sh_bd BY bdter plumi.
      LOOP AT lt_sh_bd INTO ls_sh_bd.
        IF ls_sh_bd-plumi = '1'.
          lv_sh_lbkum = lv_sh_lbkum + ls_sh_bd-bdmng.
        ELSE.
          lv_sh_lbkum = lv_sh_lbkum - ls_sh_bd-bdmng.
        ENDIF.
        IF lv_sh_lbkum < 0.
          lv_sh_udek = ls_sh_bd-bdter.
          EXIT.
        ENDIF.
      ENDLOOP.
      CHECK lv_sh_udek IS NOT INITIAL.
*     Nur Materialien, bei denen nach der Unterdeckung noch ein Zugang kommt (umterminierbar)
      LOOP AT lt_sh_bd INTO ls_sh_bd WHERE plumi = '1' AND bdter > lv_sh_udek.
        lv_sh_umt = abap_true.
        EXIT.
      ENDLOOP.
      CHECK lv_sh_umt = abap_true.

      ls_sh_out-werks  = ls_sh_marc-werks.
      ls_sh_out-matnr  = ls_sh_marc-matnr.
      ls_sh_out-dispo  = ls_sh_marc-dispo.
      ls_sh_out-dismm  = ls_sh_marc-dismm.
      ls_sh_out-udek   = lv_sh_udek.
      SELECT SINGLE maktx FROM makt INTO ls_sh_out-maktx
        WHERE matnr = ls_sh_marc-matnr AND spras = sy-langu.
      SELECT SINGLE zzlzcod FROM mara INTO ls_sh_out-lzcode
        WHERE matnr = ls_sh_marc-matnr.

      CLEAR: lv_sh_verbr, lv_sh_q1, lv_sh_q2, lv_sh_q3, lv_sh_q4.
      REFRESH lt_sh_verb.
      CALL FUNCTION 'VERBRAUCH_LESEN'
        EXPORTING
          abdatum                  = lv_sh_von
          bisdatum                 = lv_sh_bis
          matnr                    = ls_sh_marc-matnr
          periv                    = ls_sh_marc-periv
          werks                    = ls_sh_marc-werks
          kzgek                    = 'X'
        TABLES
          ges_verb                 = lt_sh_verb
          ges_verb_kor             = lt_sh_verb
          ung_verb                 = lt_sh_verb
          ung_verb_kor             = lt_sh_verb
        EXCEPTIONS
          abdatum_before_range     = 1
          abdatum_greater_bisdatum = 2
          bisdatum_in_future       = 3
          calendar_not_complete    = 4
          consumption_not_found    = 5
          fv_not_found             = 6
          fv_period_error          = 7
          no_material              = 8
          no_plant                 = 9
          OTHERS                   = 10.
      LOOP AT lt_sh_verb INTO ls_sh_verb.
        lv_sh_idx = sy-tabix.
        lv_sh_verbr = lv_sh_verbr + ls_sh_verb-verb1.
        IF lv_sh_idx <= 3.
          lv_sh_q1 = lv_sh_q1 + ls_sh_verb-verb1.
        ELSEIF lv_sh_idx <= 6.
          lv_sh_q2 = lv_sh_q2 + ls_sh_verb-verb1.
        ELSEIF lv_sh_idx <= 9.
          lv_sh_q3 = lv_sh_q3 + ls_sh_verb-verb1.
        ELSEIF lv_sh_idx <= 12.
          lv_sh_q4 = lv_sh_q4 + ls_sh_verb-verb1.
        ENDIF.
      ENDLOOP.

      lv_sh_vbwbz = lv_sh_verbr * ( ls_sh_marc-plifz + ls_sh_marc-plifz ) / 365.
      lv_sh_opt = lv_sh_vbwbz.
      CLEAR lv_sh_ant.
      LOOP AT lt_sh_bd INTO ls_sh_bd WHERE plumi = '2' AND bdter <= lv_sh_ende.
        lv_sh_opt = lv_sh_opt - ls_sh_bd-bdmng.
        lv_sh_ant = lv_sh_ant + ls_sh_bd-bdmng.
      ENDLOOP.
      IF lv_sh_vbwbz > 0.
        lv_sh_ant = lv_sh_ant * 100 / lv_sh_vbwbz.
      ENDIF.
      IF lv_sh_opt < 0.
        lv_sh_opt = 0.
      ENDIF.
      ls_sh_out-verbr_wbz = lv_sh_vbwbz.
      ls_sh_out-sibe_opt  = lv_sh_opt.
      ls_sh_out-sibe_akt  = ls_sh_marc-eisbe.
      ls_sh_out-ant_plan  = lv_sh_ant.

      ls_sh_out-tendenz = 'G'.
      IF lv_sh_q4 < lv_sh_q3 AND lv_sh_q3 < lv_sh_q2 AND lv_sh_q2 < lv_sh_q1.
        ls_sh_out-tendenz = 'R'.
      ELSEIF lv_sh_q3 + lv_sh_q4 < lv_sh_q1 + lv_sh_q2.
        ls_sh_out-tendenz = 'Y'.
      ENDIF.
      APPEND ls_sh_out TO lt_sh_out.
    ENDLOOP.

    SORT lt_sh_out BY matnr.
    IF is_paging-skip > 0.
      DELETE lt_sh_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_sh_max = is_paging-top + 1.
      DELETE lt_sh_out FROM lv_sh_max.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_sh_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

  IF iv_entity_set_name = 'ShopAufSet'.
    DATA: lt_sa_out   TYPE STANDARD TABLE OF zstr_shop_auf,
          ls_sa_out   TYPE zstr_shop_auf,
          lv_sa_werks TYPE werks_d,
          lv_sa_datum TYPE d,
          lv_sa_bis   TYPE d,
          lv_sa_max   TYPE i,
          lt_sa_plaf  TYPE STANDARD TABLE OF plaf,
          ls_sa_plaf  TYPE plaf.
    TYPES: BEGIN OF ty_sa_fa,
             aufnr TYPE afko-aufnr,
             gltrp TYPE afko-gltrp,
             dispo TYPE afko-dispo,
             matnr TYPE afpo-matnr,
             psmng TYPE afpo-psmng,
             wemng TYPE afpo-wemng,
             meins TYPE afpo-meins,
           END OF ty_sa_fa.
    DATA: lt_sa_fa TYPE STANDARD TABLE OF ty_sa_fa,
          ls_sa_fa TYPE ty_sa_fa.
    FIELD-SYMBOLS: <ls_sa_f> LIKE LINE OF it_filter_select_options,
                   <ls_sa_o> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_sa_f>.
      LOOP AT <ls_sa_f>-select_options ASSIGNING <ls_sa_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_sa_f>-property ).
          WHEN 'WERKS'. lv_sa_werks = <ls_sa_o>-low.
          WHEN 'DATUM'. lv_sa_datum = <ls_sa_o>-low.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.
    lv_sa_bis = sy-datum - 60.
    lv_sa_max = 0.
    IF lv_sa_werks IS INITIAL OR lv_sa_datum IS INITIAL OR lv_sa_datum < lv_sa_bis.
      lv_sa_max = 1.
    ENDIF.
    lv_sa_bis = sy-datum + 120.
    IF lv_sa_datum > lv_sa_bis.
      lv_sa_max = 1.
    ENDIF.
    IF lv_sa_max = 1.
      copy_data_to_ref( EXPORTING is_data = lt_sa_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.
    lv_sa_bis = lv_sa_datum + 60.

*   Planauftraege Eigenfertigung (wie ZM_OFFENE_FAUF: BESKZ <> 'F'), Eckendtermin PEDTR
    SELECT * FROM plaf INTO TABLE lt_sa_plaf UP TO 20000 ROWS
      WHERE plwrk = lv_sa_werks
        AND pedtr BETWEEN lv_sa_datum AND lv_sa_bis
        AND beskz <> 'F'.
    LOOP AT lt_sa_plaf INTO ls_sa_plaf.
      CLEAR ls_sa_out.
      ls_sa_out-werks = ls_sa_plaf-plwrk.
      ls_sa_out-aufnr = ls_sa_plaf-plnum.
      ls_sa_out-typ   = 'PA'.
      ls_sa_out-matnr = ls_sa_plaf-matnr.
      ls_sa_out-dispo = ls_sa_plaf-dispo.
      ls_sa_out-datum = ls_sa_plaf-pedtr.
      ls_sa_out-menge = ls_sa_plaf-gsmng.
      ls_sa_out-meins = ls_sa_plaf-meins.
      APPEND ls_sa_out TO lt_sa_out.
    ENDLOOP.

*   Fertigungsauftraege offen (ohne Endlieferung, nicht geloescht), Eckendtermin GLTRP
    SELECT k~aufnr k~gltrp k~dispo p~matnr p~psmng p~wemng p~meins
      FROM afko AS k INNER JOIN afpo AS p ON p~aufnr = k~aufnr
      INTO TABLE lt_sa_fa UP TO 20000 ROWS
      WHERE p~dwerk = lv_sa_werks
        AND k~gltrp BETWEEN lv_sa_datum AND lv_sa_bis
        AND p~elikz = space
        AND p~dnrel = space.
    LOOP AT lt_sa_fa INTO ls_sa_fa.
      CHECK ls_sa_fa-psmng > ls_sa_fa-wemng.
      CLEAR ls_sa_out.
      ls_sa_out-werks = lv_sa_werks.
      ls_sa_out-aufnr = ls_sa_fa-aufnr.
      ls_sa_out-typ   = 'FE'.
      ls_sa_out-matnr = ls_sa_fa-matnr.
      ls_sa_out-dispo = ls_sa_fa-dispo.
      ls_sa_out-datum = ls_sa_fa-gltrp.
      ls_sa_out-menge = ls_sa_fa-psmng - ls_sa_fa-wemng.
      ls_sa_out-meins = ls_sa_fa-meins.
      APPEND ls_sa_out TO lt_sa_out.
    ENDLOOP.

    SORT lt_sa_out BY datum aufnr.
    IF is_paging-skip > 0.
      DELETE lt_sa_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_sa_max = is_paging-top + 1.
      DELETE lt_sa_out FROM lv_sa_max.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_sa_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

* Logistik live Teil C, docs/abap/ZLOG_KAP_ADD.abap
  IF iv_entity_set_name = 'LogKapSet'.
    TYPES: BEGIN OF ty_kp_kako,
             kapid TYPE kapid,
             name  TYPE kapname,
             kapar TYPE kapart,
             begzt TYPE kapbegzt,
             endzt TYPE kapbegzt,
             pause TYPE kapbegzt,
             ngrad TYPE nutzgrad,
             aznor TYPE kapanzahl,
             kalid TYPE cr_wfcid,
           END OF ty_kp_kako,
           BEGIN OF ty_kp_bed,
             kapid    TYPE kapid,
             fstad    TYPE fstad,
             kbearest TYPE cy_kbeares,
             kruerest TYPE cy_krueres,
           END OF ty_kp_bed,
           BEGIN OF ty_kp_ca,
             objid TYPE cr_objid,
             kapid TYPE kapid,
           END OF ty_kp_ca,
           BEGIN OF ty_kp_ap,
             objid TYPE cr_objid,
             arbpl TYPE arbpl,
           END OF ty_kp_ap,
           BEGIN OF ty_kp_int,
             kapid TYPE kapid,
           END OF ty_kp_int.
    DATA: lv_kp_werks TYPE werks_d,
          lv_kp_von   TYPE datum,
          lv_kp_bis   TYPE datum,
          lv_kp_tag   TYPE datum,
          lv_kp_tage  TYPE i VALUE 7,
          lv_kp_i     TYPE i,
          lv_kp_fabkl TYPE fabkl,
          lv_kp_kal   TYPE cr_wfcid,
          lv_kp_ind   TYPE scal-indicator,
          lv_kp_std   TYPE f,
          lt_kp_kako  TYPE STANDARD TABLE OF ty_kp_kako,
          ls_kp_kako  TYPE ty_kp_kako,
          lt_kp_bed   TYPE STANDARD TABLE OF ty_kp_bed,
          ls_kp_bed   TYPE ty_kp_bed,
          lt_kp_ca    TYPE STANDARD TABLE OF ty_kp_ca,
          ls_kp_ca    TYPE ty_kp_ca,
          lt_kp_ap    TYPE SORTED TABLE OF ty_kp_ap WITH UNIQUE KEY objid,
          ls_kp_ap    TYPE ty_kp_ap,
          lt_kp_int   TYPE SORTED TABLE OF ty_kp_int WITH UNIQUE KEY kapid,
          lt_kp_out   TYPE STANDARD TABLE OF zstr_log_kap,
          ls_kp_out   TYPE zstr_log_kap.
    FIELD-SYMBOLS: <ls_kp_out> TYPE zstr_log_kap,
                   <ls_kp_f>   LIKE LINE OF it_filter_select_options,
                   <ls_kp_o>   TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_kp_f>.
      LOOP AT <ls_kp_f>-select_options ASSIGNING <ls_kp_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_kp_f>-property ).
          WHEN 'WERKS'. lv_kp_werks = <ls_kp_o>-low.
          WHEN 'DATUM'. lv_kp_von   = <ls_kp_o>-low.
          WHEN 'TAGE'.  lv_kp_tage  = <ls_kp_o>-low.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

*   Ohne Werk und gueltiges Datum kein Datenbankzugriff (Schutz fuer P76).
    lv_kp_tag = sy-datum - 30.
    lv_kp_bis = sy-datum + 60.
    IF lv_kp_werks IS INITIAL OR lv_kp_von IS INITIAL OR lv_kp_von < lv_kp_tag OR lv_kp_von > lv_kp_bis.
      copy_data_to_ref( EXPORTING is_data = lt_kp_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.
    IF lv_kp_tage < 1 OR lv_kp_tage > 14.
      lv_kp_tage = 7.
    ENDIF.
    lv_kp_bis = lv_kp_von + lv_kp_tage - 1.

    SELECT kapid name kapar begzt endzt pause ngrad aznor kalid FROM kako
      INTO CORRESPONDING FIELDS OF TABLE lt_kp_kako UP TO 200 ROWS
      WHERE werks = lv_kp_werks.
    IF lt_kp_kako IS INITIAL.
      copy_data_to_ref( EXPORTING is_data = lt_kp_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.
    SELECT SINGLE fabkl FROM t001w INTO lv_kp_fabkl WHERE werks = lv_kp_werks.

*   Kapazitaeten mit Intervallen (KAPA): Standardangebot gilt dort nicht.
    SELECT kapid FROM kapa INTO TABLE lt_kp_int
      FOR ALL ENTRIES IN lt_kp_kako
      WHERE kapid = lt_kp_kako-kapid.

*   Bedarf ueber den Index KAPID, Rest Bearbeiten + Ruesten im Fenster.
    SELECT kapid fstad kbearest kruerest FROM kbed
      INTO CORRESPONDING FIELDS OF TABLE lt_kp_bed UP TO 20000 ROWS
      FOR ALL ENTRIES IN lt_kp_kako
      WHERE kapid = lt_kp_kako-kapid
        AND fstad BETWEEN lv_kp_von AND lv_kp_bis.

*   Arbeitsplaetze je Kapazitaet (Text).
    SELECT objid kapid FROM crca INTO CORRESPONDING FIELDS OF TABLE lt_kp_ca
      FOR ALL ENTRIES IN lt_kp_kako
      WHERE objty = 'A' AND kapid = lt_kp_kako-kapid.
    IF lt_kp_ca IS NOT INITIAL.
      SELECT objid arbpl FROM crhd INTO CORRESPONDING FIELDS OF TABLE lt_kp_ap
        FOR ALL ENTRIES IN lt_kp_ca
        WHERE objty = 'A' AND objid = lt_kp_ca-objid.
    ENDIF.

*   Nur Kapazitaeten mit Bedarf im Fenster ausgeben.
    LOOP AT lt_kp_kako INTO ls_kp_kako.
      READ TABLE lt_kp_bed TRANSPORTING NO FIELDS WITH KEY kapid = ls_kp_kako-kapid.
      CHECK sy-subrc = 0.
      lv_kp_kal = ls_kp_kako-kalid.
      IF lv_kp_kal IS INITIAL.
        lv_kp_kal = lv_kp_fabkl.
      ENDIF.
      CLEAR ls_kp_out.
      ls_kp_out-werks   = lv_kp_werks.
      ls_kp_out-datum   = lv_kp_von.
      ls_kp_out-tage    = lv_kp_tage.
      ls_kp_out-kapid   = ls_kp_kako-kapid.
      ls_kp_out-kapname = ls_kp_kako-name.
      ls_kp_out-kapar   = ls_kp_kako-kapar.
      LOOP AT lt_kp_ca INTO ls_kp_ca WHERE kapid = ls_kp_kako-kapid.
        READ TABLE lt_kp_ap INTO ls_kp_ap WITH TABLE KEY objid = ls_kp_ca-objid.
        IF sy-subrc = 0 AND strlen( ls_kp_out-arbpl ) < 70.
          CONCATENATE ls_kp_out-arbpl ls_kp_ap-arbpl INTO ls_kp_out-arbpl SEPARATED BY space.
        ENDIF.
      ENDLOOP.
      CONDENSE ls_kp_out-arbpl.
      READ TABLE lt_kp_int TRANSPORTING NO FIELDS WITH TABLE KEY kapid = ls_kp_kako-kapid.
      IF sy-subrc = 0.
        ls_kp_out-kein_standard = 'X'.
      ENDIF.
      DO lv_kp_tage TIMES.
        lv_kp_i = sy-index - 1.
        lv_kp_tag = lv_kp_von + lv_kp_i.
        ls_kp_out-tag = lv_kp_tag.
        CLEAR: ls_kp_out-bedarf_h, ls_kp_out-angebot_h, ls_kp_out-vorgaenge.
        LOOP AT lt_kp_bed INTO ls_kp_bed WHERE kapid = ls_kp_kako-kapid AND fstad = lv_kp_tag.
          ls_kp_out-bedarf_h  = ls_kp_out-bedarf_h + ls_kp_bed-kbearest + ls_kp_bed-kruerest.
          ls_kp_out-vorgaenge = ls_kp_out-vorgaenge + 1.
        ENDLOOP.
        IF ls_kp_out-kein_standard IS INITIAL AND lv_kp_kal IS NOT INITIAL.
          CLEAR lv_kp_ind.
          CALL FUNCTION 'DATE_CONVERT_TO_FACTORYDATE'
            EXPORTING
              date                 = lv_kp_tag
              factory_calendar_id  = lv_kp_kal
              correct_option       = '+'
            IMPORTING
              workingday_indicator = lv_kp_ind
            EXCEPTIONS
              OTHERS               = 1.
          IF sy-subrc = 0 AND lv_kp_ind IS INITIAL.
            lv_kp_std = ( ls_kp_kako-endzt - ls_kp_kako-begzt - ls_kp_kako-pause ) / 3600.
            ls_kp_out-angebot_h = lv_kp_std * ls_kp_kako-ngrad / 100 * ls_kp_kako-aznor.
          ENDIF.
        ENDIF.
        APPEND ls_kp_out TO lt_kp_out.
      ENDDO.
    ENDLOOP.

    SORT lt_kp_out BY kapid tag.
    copy_data_to_ref( EXPORTING is_data = lt_kp_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

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
             erdat TYPE erdat,
             erzet TYPE erzet,
           END OF ty_lg_vbfa,
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
          lv_lg_spaet   TYPE datum,
          lt_lg_vbfa    TYPE STANDARD TABLE OF ty_lg_vbfa,
          ls_lg_vbfa    TYPE ty_lg_vbfa,
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
        SELECT vbeln lfart vstel lgnum kunnr kostk wbstk erzet wadat wadat_ist FROM likp
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
*         Uhrzeit der Warenbewegung (Folgebeleg Typ R), juengste zuerst.
          SELECT vbelv erdat erzet FROM vbfa
            INTO CORRESPONDING FIELDS OF TABLE lt_lg_vbfa
            FOR ALL ENTRIES IN lt_lg_lf_kopf
            WHERE vbelv = lt_lg_lf_kopf-vbeln
              AND vbtyp_n = 'R'.
          SORT lt_lg_vbfa BY vbelv ASCENDING erdat DESCENDING erzet DESCENDING.
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
          READ TABLE lt_lg_vbfa INTO ls_lg_vbfa WITH KEY vbelv = ls_lg_lf_kopf-vbeln BINARY SEARCH.
          IF sy-subrc = 0.
            ls_lg_lf_out-wa_zeit = ls_lg_vbfa-erzet.
          ENDIF.
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
