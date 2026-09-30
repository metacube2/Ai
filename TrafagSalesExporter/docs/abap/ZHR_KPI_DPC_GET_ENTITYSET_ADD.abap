*&---------------------------------------------------------------------*
*& DPC_EXT: Daten fuer das EntitySet HrKpiSet
*& Klasse : ZCL_ZPOWERBI_EINKAUF_DPC_EXT
*& Methode: /IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET  (redefiniert,
*&          dort liegt schon FinanzJournalSet)
*& Stand  : 2026-09-30, Entwurf, NOCH NICHT im System
*&
*& EINFUEGEN, nicht ersetzen: dieser Block kommt ganz an den ANFANG der
*& Methode, VOR "IF iv_entity_set_name <> 'FinanzJournalSet'". Er behandelt
*& nur HrKpiSet und kehrt danach mit RETURN zurueck; alle anderen Sets
*& laufen unveraendert weiter in den bestehenden Code.
*&
*& Fachlich dieselbe Rechnung wie Z_HR_KPI_CONS (docs/abap/Z_HR_KPI_CONS.abap),
*& FORM get_data: Stichtag Monatsende, PA0001/PA0002/PA0007 zum Stichtag,
*& Unfalltage aus PA2001 auf den Monat beschnitten, in Kalendertagen.
*& Bewusst NICHT: Namen, Geburtsdatum, Lohn, Ein-/Austritt, Krankheit,
*& Ferien (Entscheid Ingo 2026-09-30, nur was das Cockpit liest).
*&
*& Filter: Gjahr und Monat, je hoechstens EQ. Ohne Filter gilt der
*& laufende Monat. Die Tabellen werden per SELECT gelesen, ohne
*& HR-Berechtigungspruefung (P_ORGIN); wer den Service lesen darf, sieht
*& diese Felder. Das ist mit dem Entscheid fuer ZPOWERBI_EINKAUF_SRV so
*& in Kauf genommen und in docs/HR_KPI.md 8.6 vermerkt.
*&---------------------------------------------------------------------*

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
