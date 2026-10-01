*&---------------------------------------------------------------------*
*& DPC_EXT: Abwesenheiten je Fall aus PA2001 fuer das Cockpit
*& Klasse : ZCL_ZPOWERBI_EINKAUF_DPC_EXT
*& Methode: /IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET (redefiniert)
*& Stand  : 2026-10-01
*&
*& EINFUEGEN direkt NACH dem HrKpiSet-Block (dessen ENDIF) und VOR
*& "IF iv_entity_set_name <> 'FinanzJournalSet'." Alle anderen Sets laufen
*& unveraendert weiter.
*&
*& Filter: nur Gjahr eq 'JJJJ' (Vorgabe laufendes Jahr). Geliefert wird jeder
*& Fall, der das Jahr beruehrt, mit seinem vollen Von/Bis; das Zuschneiden auf
*& Zeitraeume macht das Cockpit. Gesperrte Saetze (SPRPS = X, nicht
*& freigegeben) bleiben draussen.
*&---------------------------------------------------------------------*

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
