*&---------------------------------------------------------------------*
*& DPC_EXT: Zc23Set, ZC23 (ZM_OFFENE_KDAUF) live mit Selektion fuer die Fiori-App ZC23
*& Klasse : ZCL_ZPOWERBI_EINKAUF_DPC_EXT
*& Methode: /IWBEP/IF_MGW_APPL_SRV_RUNTIME~GET_ENTITYSET (redefiniert)
*& Stand  : 2026-10-15, saptasks/fiori_zc23
*&
*& EINFUEGEN direkt NACH dem Logistik-live-Block (LogTaSet/LogLiefSet/LogRueckSet)
*& und VOR "IF iv_entity_set_name <> 'FinanzJournalSet'."
*&
*& Die Logik bleibt in ZM_OFFENE_KDAUF: SUBMIT mit P_APP = X legt das Ergebnis nach dem
*& Materialanteil ins ABAP-Memory und endet ohne ALV und ohne Fortschreibung (P_UPDBI aus).
*&
*& SCHUTZ (Dienst ist auch die Power-BI-Quelle): Pflichtfilter Werk, Datvon, Datbis (EQ),
*& Zeitraum hoechstens 366 Tage, sonst Fehler ohne einen Reportlauf.
*& Filter (alle EQ): Werk, Datvon, Datbis (JJJJMMTT, Liefertermin VBEP-EDATU = S_MBDAT),
*& Kunnr, Matnr, Vbeln (je mehrfach moeglich; Dispo nicht: S_DISPO ist AFKO-DISPO, die Spalte DISPO ein anderer Schluessel), Mata (X/leer, Vorgabe X),
*& Mjung (X = juengste statt freigegebene Kalkulation), Mfrem (X = Fremdleistung zum Material),
*& Korr (V = korrigiert [Vorgabe], A = Absagegrund, L = alte Logik).
*&---------------------------------------------------------------------*

* Fiori-App ZC23, saptasks/fiori_zc23/ZC23_DPC_GET_ENTITYSET_ADD.abap

  IF iv_entity_set_name = 'Zc23Set'.

    DATA: lt_z23_out   TYPE STANDARD TABLE OF zstr_zc23_app,
          ls_z23_out   TYPE zstr_zc23_app,
          lt_z23_tab   TYPE STANDARD TABLE OF zmm_ueb_kdauf,
          ls_z23_tab   TYPE zmm_ueb_kdauf,
          lt_z23_sel   TYPE STANDARD TABLE OF rsparams,
          ls_z23_sel   TYPE rsparams,
          lv_z23_werk  TYPE werks_d,
          lv_z23_von   TYPE datum,
          lv_z23_bis   TYPE datum,
          lv_z23_mata  TYPE c LENGTH 1 VALUE 'X',
          lv_z23_mjung TYPE c LENGTH 1,
          lv_z23_mfrem TYPE c LENGTH 1,
          lv_z23_korr  TYPE c LENGTH 1 VALUE 'V',
          lv_z23_kunnr TYPE kunnr,
          lv_z23_matnr TYPE matnr,
          lv_z23_vbeln TYPE vbeln_va,
          lv_z23_tage  TYPE i,
          lv_z23_skip  TYPE i,
          lv_z23_max   TYPE i,
          lv_z23_zeile TYPE i,
          lv_z23_msg   TYPE bapi_msg.

    FIELD-SYMBOLS: <ls_z23_f> LIKE LINE OF it_filter_select_options,
                   <ls_z23_o> TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_z23_f>.
      LOOP AT <ls_z23_f>-select_options ASSIGNING <ls_z23_o>
           WHERE sign = 'I' AND option = 'EQ'.
        CLEAR ls_z23_sel.
        ls_z23_sel-kind   = 'S'.
        ls_z23_sel-sign   = 'I'.
        ls_z23_sel-option = 'EQ'.
        CASE to_upper( <ls_z23_f>-property ).
          WHEN 'WERK'.
            lv_z23_werk = <ls_z23_o>-low.
          WHEN 'DATVON'.
            lv_z23_von = <ls_z23_o>-low.
          WHEN 'DATBIS'.
            lv_z23_bis = <ls_z23_o>-low.
          WHEN 'MATA'.
            lv_z23_mata = <ls_z23_o>-low.
          WHEN 'MJUNG'.
            lv_z23_mjung = <ls_z23_o>-low.
          WHEN 'MFREM'.
            lv_z23_mfrem = <ls_z23_o>-low.
          WHEN 'KORR'.
            lv_z23_korr = <ls_z23_o>-low.
          WHEN 'KUNNR'.
            CALL FUNCTION 'CONVERSION_EXIT_ALPHA_INPUT'
              EXPORTING input  = <ls_z23_o>-low
              IMPORTING output = lv_z23_kunnr.
            ls_z23_sel-selname = 'S_KUNDE'.
            ls_z23_sel-low     = lv_z23_kunnr.
            APPEND ls_z23_sel TO lt_z23_sel.
          WHEN 'MATNR'.
            CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
              EXPORTING  input        = <ls_z23_o>-low
              IMPORTING  output       = lv_z23_matnr
              EXCEPTIONS length_error = 1
                         OTHERS       = 2.
            IF sy-subrc <> 0.
              lv_z23_matnr = <ls_z23_o>-low.
            ENDIF.
            ls_z23_sel-selname = 'S_MATNR'.
            ls_z23_sel-low     = lv_z23_matnr.
            APPEND ls_z23_sel TO lt_z23_sel.
          WHEN 'VBELN'.
            CALL FUNCTION 'CONVERSION_EXIT_ALPHA_INPUT'
              EXPORTING input  = <ls_z23_o>-low
              IMPORTING output = lv_z23_vbeln.
            ls_z23_sel-selname = 'S_KDAUF'.
            ls_z23_sel-low     = lv_z23_vbeln.
            APPEND ls_z23_sel TO lt_z23_sel.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

*   Schutz: ohne Werk und Zeitraum kein Reportlauf
    lv_z23_tage = lv_z23_bis - lv_z23_von.
    IF lv_z23_werk IS INITIAL OR lv_z23_von IS INITIAL OR lv_z23_bis IS INITIAL
    OR lv_z23_tage < 0 OR lv_z23_tage > 366.
      lv_z23_msg = 'Zc23Set braucht Werk, Datvon und Datbis (JJJJMMTT), hoechstens 366 Tage.'.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = lv_z23_msg.
    ENDIF.

    CLEAR ls_z23_sel.
    ls_z23_sel-kind = 'P'.
    ls_z23_sel-selname = 'P_WERK'.  ls_z23_sel-low = lv_z23_werk.  APPEND ls_z23_sel TO lt_z23_sel.
    ls_z23_sel-selname = 'P_UPDBI'. ls_z23_sel-low = space.        APPEND ls_z23_sel TO lt_z23_sel.
    ls_z23_sel-selname = 'P_APP'.   ls_z23_sel-low = 'X'.          APPEND ls_z23_sel TO lt_z23_sel.
    ls_z23_sel-selname = 'P_MATA'.  ls_z23_sel-low = lv_z23_mata.  APPEND ls_z23_sel TO lt_z23_sel.
    ls_z23_sel-selname = 'P_MFREM'. ls_z23_sel-low = lv_z23_mfrem. APPEND ls_z23_sel TO lt_z23_sel.
*   Radiogruppen: genau ein X je Gruppe
    ls_z23_sel-selname = 'P_MFREI'. ls_z23_sel-low = COND #( WHEN lv_z23_mjung = 'X' THEN space ELSE 'X' ).
    APPEND ls_z23_sel TO lt_z23_sel.
    ls_z23_sel-selname = 'P_MJUNG'. ls_z23_sel-low = COND #( WHEN lv_z23_mjung = 'X' THEN 'X' ELSE space ).
    APPEND ls_z23_sel TO lt_z23_sel.
    ls_z23_sel-selname = 'P_KVOLL'. ls_z23_sel-low = COND #( WHEN lv_z23_korr = 'A' OR lv_z23_korr = 'L' THEN space ELSE 'X' ).
    APPEND ls_z23_sel TO lt_z23_sel.
    ls_z23_sel-selname = 'P_KABGR'. ls_z23_sel-low = COND #( WHEN lv_z23_korr = 'A' THEN 'X' ELSE space ).
    APPEND ls_z23_sel TO lt_z23_sel.
    ls_z23_sel-selname = 'P_KALT'.  ls_z23_sel-low = COND #( WHEN lv_z23_korr = 'L' THEN 'X' ELSE space ).
    APPEND ls_z23_sel TO lt_z23_sel.

    CLEAR ls_z23_sel.
    ls_z23_sel-kind    = 'S'.
    ls_z23_sel-selname = 'S_MBDAT'.
    ls_z23_sel-sign    = 'I'.
    ls_z23_sel-option  = 'BT'.
    ls_z23_sel-low     = lv_z23_von.
    ls_z23_sel-high    = lv_z23_bis.
    APPEND ls_z23_sel TO lt_z23_sel.

    FREE MEMORY ID 'ZM_OFFENE_KDAUF_APP'.
    SUBMIT zm_offene_kdauf WITH SELECTION-TABLE lt_z23_sel AND RETURN.
    IMPORT tab = lt_z23_tab FROM MEMORY ID 'ZM_OFFENE_KDAUF_APP'.
    FREE MEMORY ID 'ZM_OFFENE_KDAUF_APP'.

    LOOP AT lt_z23_tab INTO ls_z23_tab.
      CLEAR ls_z23_out.
      MOVE-CORRESPONDING ls_z23_tab TO ls_z23_out.
      CLEAR ls_z23_out-ernam.
      ls_z23_out-rownr  = sy-tabix.
      ls_z23_out-datvon = lv_z23_von.
      ls_z23_out-datbis = lv_z23_bis.
      ls_z23_out-mata   = lv_z23_mata.
      ls_z23_out-mjung  = lv_z23_mjung.
      ls_z23_out-mfrem  = lv_z23_mfrem.
      ls_z23_out-korr   = lv_z23_korr.
      APPEND ls_z23_out TO lt_z23_out.
    ENDLOOP.

    IF is_paging-skip > 0.
      lv_z23_skip = is_paging-skip.
      DELETE lt_z23_out TO lv_z23_skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_z23_max = is_paging-top + 1.
      DESCRIBE TABLE lt_z23_out LINES lv_z23_zeile.
      IF lv_z23_zeile >= lv_z23_max.
        DELETE lt_z23_out FROM lv_z23_max.
      ENDIF.
    ENDIF.

    copy_data_to_ref( EXPORTING is_data = lt_z23_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

