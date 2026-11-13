REPORT z_koi_shop_layout_test LINE-SIZE 255 NO STANDARD PAGE HEADING.
* $TMP-Test 2026-11-13 (Claude), nur T76, schreibt nichts. Plan saptasks/shopfloor_sap/PLAN_LAYOUTFILTER_2026-11-13.md
* Abschnitt 3.7 "Vollstaendiger Vergleich", danach loeschen.
* Teil 1: ShopAufSet Typ VR ueber ZCL_ZPOWERBI_EINKAUF_DPC_EXT (wie das Gateway, ohne Paging) ohne und mit Filter
*   Avo eq 'X'. Zeilen, Typ, Menge und ein MD5 ueber die Grundfelder in Ausgabereihenfolge (vorher = nachher).
*   Die neuen Felder STTXT/ARBPL/VORNR/AVO per ASSIGN COMPONENT, weil es sie vor der Strukturerweiterung nicht gibt.
*   Dazu VF/VP je Seite von p_seite Zeilen (gleiche Sortierung wie der DPC: datum aufnr).
* Teil 2 (p_batch 1..8): ZC22 (ZM_OFFENE_FAUF) per SUBMIT mit P_AVO = 'X', P_PLAF = ' ', P_PEG = ' ' fuer eine
*   Disponentengruppe (S_DISPO als Einzelwerte; ohne jede Selektion liefert ZC22 nichts, FORM fill_iafpo),
*   ALV-Tabelle ueber cl_salv_bs_runtime_info. Vergleich je Fertigungsauftrag: Sttxt = ZC22 STTXT,
*   Arbpl/Vornr (Lauf mit Avo) = ZC22 ARBPL/VORNR.

PARAMETERS: p_datum TYPE sy-datum DEFAULT sy-datum,
            p_batch TYPE i DEFAULT 0,
            p_seite TYPE i DEFAULT 5000.

TYPES: BEGIN OF ty_z,
         aufnr TYPE c LENGTH 12,
         typ   TYPE c LENGTH 2,
         dispo TYPE c LENGTH 3,
         menge TYPE p LENGTH 15 DECIMALS 3,
         sttxt TYPE c LENGTH 100,
         arbpl TYPE c LENGTH 8,
         vornr TYPE c LENGTH 4,
         avo   TYPE c LENGTH 1,
       END OF ty_z,
       ty_zt TYPE STANDARD TABLE OF ty_z WITH DEFAULT KEY,
       ty_d  TYPE c LENGTH 3,
       ty_dt TYPE STANDARD TABLE OF ty_d WITH DEFAULT KEY.

DATA: gt_ohne TYPE ty_zt,
      gt_mit  TYPE ty_zt,
      gv_ok_o TYPE c LENGTH 1,
      gv_ok_m TYPE c LENGTH 1.

START-OF-SELECTION.
  WRITE: / 'KOPF|Datum', p_datum, '|Batch', p_batch, '|Seite', p_seite, '|Sprache', sy-langu, '|Start', sy-uzeit.
  PERFORM set_lesen USING ' ' CHANGING gt_ohne gv_ok_o.
  PERFORM set_lesen USING 'X' CHANGING gt_mit gv_ok_m.
  IF gv_ok_o = 'X' AND gv_ok_m = 'X'.
    PERFORM auswerten.
    IF p_batch > 0.
      PERFORM zc22_vergleich.
    ENDIF.
  ENDIF.
  WRITE: / 'ENDE|', sy-uzeit.

*---------------------------------------------------------------------*
FORM set_lesen USING iv_avo TYPE c CHANGING ct_z TYPE ty_zt cv_ok TYPE c.
  DATA: lo_dpc    TYPE REF TO zcl_zpowerbi_einkauf_dpc_ext,
        lt_filter TYPE /iwbep/t_mgw_select_option,
        ls_filter TYPE /iwbep/s_mgw_select_option,
        ls_so     TYPE /iwbep/s_cod_select_option,
        ls_paging TYPE /iwbep/s_mgw_paging,
        lr_data   TYPE REF TO data,
        lx        TYPE REF TO cx_root,
        lv_txt    TYPE string,
        lv_alle   TYPE string,
        lv_zeile  TYPE string,
        lv_c      TYPE string,
        lv_hash   TYPE string,
        lv_t0     TYPE i,
        lv_t1     TYPE i,
        lv_ms     TYPE i,
        lv_n      TYPE i,
        lv_vf     TYPE i,
        lv_vp     TYPE i,
        lv_menge  TYPE p LENGTH 15 DECIMALS 3,
        lv_f_st   TYPE c LENGTH 1,
        lv_f_ar   TYPE c LENGTH 1,
        lv_f_vo   TYPE c LENGTH 1,
        lv_f_av   TYPE c LENGTH 1,
        ls_z      TYPE ty_z.
  FIELD-SYMBOLS: <lt> TYPE STANDARD TABLE,
                 <ls> TYPE any,
                 <f>  TYPE any.

  CLEAR: ct_z, cv_ok.
  ls_so-sign = 'I'. ls_so-option = 'EQ'.
  ls_filter-property = 'Werks'. ls_so-low = '1100'. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  CLEAR ls_filter. ls_filter-property = 'Datum'. ls_so-low = p_datum. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  CLEAR ls_filter. ls_filter-property = 'Typ'. ls_so-low = 'VR'. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  IF iv_avo = 'X'.
    CLEAR ls_filter. ls_filter-property = 'Avo'. ls_so-low = 'X'. APPEND ls_so TO ls_filter-select_options. APPEND ls_filter TO lt_filter.
  ENDIF.
  CREATE OBJECT lo_dpc.
  GET RUN TIME FIELD lv_t0.
  TRY.
      lo_dpc->/iwbep/if_mgw_appl_srv_runtime~get_entityset(
        EXPORTING iv_entity_name = 'ShopAuf' iv_entity_set_name = 'ShopAufSet' iv_source_name = 'ShopAuf'
                  it_filter_select_options = lt_filter is_paging = ls_paging
        IMPORTING er_entityset = lr_data ).
    CATCH cx_root INTO lx.
      lv_txt = lx->get_text( ).
      WRITE: / 'SET_FEHLER|Avo', iv_avo, '|', lv_txt.
      RETURN.
  ENDTRY.
  GET RUN TIME FIELD lv_t1.
  lv_ms = ( lv_t1 - lv_t0 ) / 1000.
  ASSIGN lr_data->* TO <lt>.
  LOOP AT <lt> ASSIGNING <ls>.
    CLEAR: ls_z, lv_zeile.
    ASSIGN COMPONENT 'AUFNR' OF STRUCTURE <ls> TO <f>. ls_z-aufnr = <f>. lv_c = <f>. CONCATENATE lv_zeile lv_c '|' INTO lv_zeile.
    ASSIGN COMPONENT 'TYP' OF STRUCTURE <ls> TO <f>. ls_z-typ = <f>. lv_c = <f>. CONCATENATE lv_zeile lv_c '|' INTO lv_zeile.
    ASSIGN COMPONENT 'WERKS' OF STRUCTURE <ls> TO <f>. lv_c = <f>. CONCATENATE lv_zeile lv_c '|' INTO lv_zeile.
    ASSIGN COMPONENT 'MATNR' OF STRUCTURE <ls> TO <f>. lv_c = <f>. CONCATENATE lv_zeile lv_c '|' INTO lv_zeile.
    ASSIGN COMPONENT 'DISPO' OF STRUCTURE <ls> TO <f>. ls_z-dispo = <f>. lv_c = <f>. CONCATENATE lv_zeile lv_c '|' INTO lv_zeile.
    ASSIGN COMPONENT 'DATUM' OF STRUCTURE <ls> TO <f>. lv_c = <f>. CONCATENATE lv_zeile lv_c '|' INTO lv_zeile.
    ASSIGN COMPONENT 'MENGE' OF STRUCTURE <ls> TO <f>. ls_z-menge = <f>. lv_c = <f>. CONCATENATE lv_zeile lv_c '|' INTO lv_zeile.
    ASSIGN COMPONENT 'MEINS' OF STRUCTURE <ls> TO <f>. lv_c = <f>. CONCATENATE lv_zeile lv_c INTO lv_zeile.
    CONCATENATE lv_alle lv_zeile cl_abap_char_utilities=>newline INTO lv_alle.
    ASSIGN COMPONENT 'STTXT' OF STRUCTURE <ls> TO <f>.
    IF sy-subrc = 0. ls_z-sttxt = <f>. lv_f_st = 'X'. ENDIF.
    ASSIGN COMPONENT 'ARBPL' OF STRUCTURE <ls> TO <f>.
    IF sy-subrc = 0. ls_z-arbpl = <f>. lv_f_ar = 'X'. ENDIF.
    ASSIGN COMPONENT 'VORNR' OF STRUCTURE <ls> TO <f>.
    IF sy-subrc = 0. ls_z-vornr = <f>. lv_f_vo = 'X'. ENDIF.
    ASSIGN COMPONENT 'AVO' OF STRUCTURE <ls> TO <f>.
    IF sy-subrc = 0. ls_z-avo = <f>. lv_f_av = 'X'. ENDIF.
    SHIFT ls_z-aufnr LEFT DELETING LEADING '0'.
    APPEND ls_z TO ct_z.
    lv_n = lv_n + 1.
    lv_menge = lv_menge + ls_z-menge.
    IF ls_z-typ = 'VF'.
      lv_vf = lv_vf + 1.
    ELSEIF ls_z-typ = 'VP'.
      lv_vp = lv_vp + 1.
    ENDIF.
  ENDLOOP.
  TRY.
      cl_abap_message_digest=>calculate_hash_for_char(
        EXPORTING if_algorithm = 'MD5' if_data = lv_alle
        IMPORTING ef_hashstring = lv_hash ).
    CATCH cx_abap_message_digest.
      lv_hash = 'HASHFEHLER'.
  ENDTRY.
  WRITE: / 'SET|Avo', iv_avo, '|ms', lv_ms, '|Zeilen', lv_n, '|VF', lv_vf, '|VP', lv_vp.
  WRITE: / 'SUM|Avo', iv_avo, '|Menge', lv_menge.
  WRITE: / 'MD5|Avo', iv_avo, '|', lv_hash.
  WRITE: / 'FELDER|Avo', iv_avo, '|STTXT', lv_f_st, '|ARBPL', lv_f_ar, '|VORNR', lv_f_vo, '|AVO', lv_f_av.
  cv_ok = 'X'.
ENDFORM.

*---------------------------------------------------------------------*
FORM auswerten.
  DATA: ls_o      TYPE ty_z,
        ls_m      TYPE ty_z,
        lv_i      TYPE i,
        lv_n      TYPE i,
        lv_basis  TYPE i,
        lv_st_vf  TYPE i,
        lv_st_lr  TYPE i,
        lv_st_vp  TYPE i,
        lv_st_om  TYPE i,
        lv_o_ar   TYPE i,
        lv_m_ar   TYPE i,
        lv_m_vo   TYPE i,
        lv_m_av   TYPE i,
        lv_m_avp  TYPE i,
        lv_mu     TYPE i,
        lv_mm     TYPE i,
        lv_beide  TYPE i,
        lv_aus    TYPE c LENGTH 1,
        lv_ek     TYPE i,
        lv_ek_aus TYPE i,
        lv_el     TYPE i,
        lv_el_aus TYPE i,
        lv_s      TYPE i,
        lv_s_aus  TYPE i,
        lv_r      TYPE i,
        lv_r_aus  TYPE i,
        lv_seite  TYPE i,
        lv_von    TYPE i,
        lv_bis    TYPE i,
        lv_svf    TYPE i,
        lv_svp    TYPE i,
        lv_zeig   TYPE i.

* Grundfelder und Sttxt ohne/mit Avo zeilenweise vergleichen
  lv_n = lines( gt_ohne ).
  IF lines( gt_mit ) <> lv_n.
    WRITE: / 'OHNE_MIT|Zeilenzahl verschieden'.
  ENDIF.
  DO lv_n TIMES.
    lv_i = sy-index.
    READ TABLE gt_ohne INTO ls_o INDEX lv_i.
    READ TABLE gt_mit INTO ls_m INDEX lv_i.
    IF sy-subrc <> 0.
      EXIT.
    ENDIF.
    IF ls_o-aufnr <> ls_m-aufnr OR ls_o-typ <> ls_m-typ OR ls_o-menge <> ls_m-menge OR ls_o-dispo <> ls_m-dispo.
      lv_basis = lv_basis + 1.
    ENDIF.
    IF ls_o-sttxt <> ls_m-sttxt.
      lv_st_om = lv_st_om + 1.
    ENDIF.
    IF ls_o-arbpl IS NOT INITIAL OR ls_o-vornr IS NOT INITIAL OR ls_o-avo IS NOT INITIAL.
      lv_o_ar = lv_o_ar + 1.
    ENDIF.
  ENDDO.
  WRITE: / 'OHNE_MIT|Grundfelder ungleich', lv_basis, '|Sttxt ungleich', lv_st_om.
  WRITE: / 'OHNE_AVO|Arbpl/Vornr/Avo gefuellt', lv_o_ar.

* Lauf mit Avo: Fuellung und Layoutmuster
  LOOP AT gt_mit INTO ls_m.
    IF ls_m-typ = 'VF'.
      IF ls_m-sttxt IS INITIAL.
        lv_st_lr = lv_st_lr + 1.
      ELSE.
        lv_st_vf = lv_st_vf + 1.
      ENDIF.
      IF ls_m-arbpl IS NOT INITIAL.
        lv_m_ar = lv_m_ar + 1.
      ENDIF.
      IF ls_m-vornr IS NOT INITIAL.
        lv_m_vo = lv_m_vo + 1.
      ENDIF.
      IF ls_m-avo = 'X'.
        lv_m_av = lv_m_av + 1.
      ENDIF.
      CLEAR lv_aus.
      IF ls_m-sttxt CP 'FREI DRUC RÜCK VOKL*'.
        lv_mu = lv_mu + 1.
        lv_aus = 'X'.
      ENDIF.
      IF ls_m-arbpl CP 'M*'.
        lv_mm = lv_mm + 1.
        IF lv_aus = 'X'.
          lv_beide = lv_beide + 1.
        ENDIF.
        lv_aus = 'X'.
      ENDIF.
      IF ls_m-dispo CP 'EK*'.
        lv_ek = lv_ek + 1.
        IF lv_aus = 'X'. lv_ek_aus = lv_ek_aus + 1. ENDIF.
      ELSEIF ls_m-dispo CP 'EL*'.
        lv_el = lv_el + 1.
        IF lv_aus = 'X'. lv_el_aus = lv_el_aus + 1. ENDIF.
      ELSEIF ls_m-dispo CP 'S*'.
        lv_s = lv_s + 1.
        IF lv_aus = 'X'. lv_s_aus = lv_s_aus + 1. ENDIF.
      ELSE.
        lv_r = lv_r + 1.
        IF lv_aus = 'X'. lv_r_aus = lv_r_aus + 1. ENDIF.
      ENDIF.
    ELSE.
      IF ls_m-sttxt IS NOT INITIAL OR ls_m-arbpl IS NOT INITIAL OR ls_m-vornr IS NOT INITIAL OR ls_m-avo IS NOT INITIAL.
        lv_st_vp = lv_st_vp + 1.
      ENDIF.
    ENDIF.
  ENDLOOP.
  WRITE: / 'MIT_AVO|VF Sttxt gefuellt', lv_st_vf, '|VF Sttxt leer', lv_st_lr, '|VP mit Feldern', lv_st_vp.
  WRITE: / 'MIT_AVO|VF Arbpl', lv_m_ar, '|VF Vornr', lv_m_vo, '|VF Avo X', lv_m_av.
  WRITE: / 'MUSTER|Status FREI DRUC RUECK VOKL*', lv_mu, '|Arbpl M*', lv_mm, '|beide', lv_beide.
  WRITE: / 'GRUPPEN|EK', lv_ek, lv_ek_aus, '|EL', lv_el, lv_el_aus.
  WRITE: / 'GRUPPEN|S', lv_s, lv_s_aus, '|uebrige', lv_r, lv_r_aus.

* VF/VP je Seite (Ausgabereihenfolge des DPC, $skip/$top wie im Gateway)
  IF p_seite > 0.
    lv_von = 1.
    WHILE lv_von <= lv_n.
      lv_seite = lv_seite + 1.
      lv_bis = lv_von + p_seite - 1.
      IF lv_bis > lv_n.
        lv_bis = lv_n.
      ENDIF.
      CLEAR: lv_svf, lv_svp.
      LOOP AT gt_ohne INTO ls_o FROM lv_von TO lv_bis.
        IF ls_o-typ = 'VF'.
          lv_svf = lv_svf + 1.
        ELSE.
          lv_svp = lv_svp + 1.
        ENDIF.
      ENDLOOP.
      WRITE: / 'SEITE|', lv_seite, '|Zeilen', lv_von, lv_bis, '|VF', lv_svf, '|VP', lv_svp.
      lv_von = lv_bis + 1.
    ENDWHILE.
  ENDIF.

* Beispiele: die Auftraege aus dem Plan (1.8) und die ersten drei VF der Liste
  LOOP AT gt_mit INTO ls_m WHERE typ = 'VF'.
    IF ls_m-aufnr = '1170555' OR ls_m-aufnr = '1209407' OR ls_m-aufnr = '1213839' OR ls_m-aufnr = '986726'
       OR ls_m-aufnr = '1173719' OR lv_zeig < 3.
      WRITE: / 'BSP|', ls_m-aufnr, ls_m-dispo, '|Vornr', ls_m-vornr, '|Arbpl', ls_m-arbpl, '|Avo', ls_m-avo.
      WRITE: / 'BST|', ls_m-aufnr, '|', ls_m-sttxt(80).
      lv_zeig = lv_zeig + 1.
    ENDIF.
  ENDLOOP.
ENDFORM.

*---------------------------------------------------------------------*
FORM batch_dispos USING iv_b TYPE i CHANGING ct_d TYPE ty_dt.
  DATA lv_l TYPE string.
  CLEAR ct_d.
  CASE iv_b.
    WHEN 1. lv_l = 'SB1 SB2 SB3 SB4 SE1 SE2 SE4 SK3 SL1 SL2 SL3 SL4 SPB'.
    WHEN 2. lv_l = 'D10 D11 D12 D20 D21 D22 D23 D30 DEF DS1 DS2 DW1 TL1'.
    WHEN 3. lv_l = 'EK1 EK2 EK3 EK4 EK5 EK6 EK7 EK8 EK9 EKB'.
    WHEN 4. lv_l = 'EL1 EL2 EL3 EL4 EL5 EL6 EL7 EL8 ELP'.
    WHEN 5. lv_l = '001 003 004 012 013 016 018 021 022 026 027 028 031'.
    WHEN 6. lv_l = '019'.
    WHEN 7. lv_l = '020 024'.
    WHEN 8. lv_l = '025'.
  ENDCASE.
  IF lv_l IS NOT INITIAL.
    SPLIT lv_l AT space INTO TABLE ct_d.
  ENDIF.
ENDFORM.

*---------------------------------------------------------------------*
FORM zc22_vergleich.
  TYPES: BEGIN OF ty_k,
           aufnr TYPE c LENGTH 12,
           sttxt TYPE c LENGTH 100,
           arbpl TYPE c LENGTH 8,
           vornr TYPE c LENGTH 4,
         END OF ty_k.
  DATA: lt_d      TYPE ty_dt,
        lt_alle   TYPE ty_dt,
        lt_b      TYPE ty_dt,
        lv_d      TYPE ty_d,
        lv_b      TYPE i,
        lr_dispo  TYPE RANGE OF afko-dispo,
        ls_dispo  LIKE LINE OF lr_dispo,
        lr_data   TYPE REF TO data,
        lx        TYPE REF TO cx_root,
        lv_txt    TYPE string,
        lt_k      TYPE SORTED TABLE OF ty_k WITH UNIQUE KEY aufnr,
        ls_k      TYPE ty_k,
        ls_k2     TYPE ty_k,
        ls_m      TYPE ty_z,
        lv_t0     TYPE i,
        lv_t1     TYPE i,
        lv_ms     TYPE i,
        lv_u0     TYPE sy-uzeit,
        lv_u1     TYPE sy-uzeit,
        lv_z22    TYPE i,
        lv_auf    TYPE i,
        lv_mehr   TYPE i,
        lv_set    TYPE i,
        lv_nurset TYPE i,
        lv_gl     TYPE i,
        lv_d_st   TYPE i,
        lv_d_ar   TYPE i,
        lv_d_vo   TYPE i,
        lv_zmu    TYPE i,
        lv_zmm    TYPE i,
        lv_smu    TYPE i,
        lv_smm    TYPE i,
        lv_nicht  TYPE i,
        lv_zeig   TYPE i.
  FIELD-SYMBOLS: <lt> TYPE STANDARD TABLE,
                 <ls> TYPE any,
                 <f>  TYPE any.

* Abdeckung: alle VF-Disponenten der Set-Menge muessen in einer der acht Gruppen stehen
  DO 8 TIMES.
    lv_b = sy-index.
    PERFORM batch_dispos USING lv_b CHANGING lt_b.
    APPEND LINES OF lt_b TO lt_alle.
  ENDDO.
  LOOP AT gt_mit INTO ls_m WHERE typ = 'VF'.
    READ TABLE lt_alle TRANSPORTING NO FIELDS WITH KEY table_line = ls_m-dispo.
    IF sy-subrc <> 0.
      lv_nicht = lv_nicht + 1.
      IF lv_zeig < 10.
        WRITE: / 'NICHT_ABGEDECKT|', ls_m-aufnr, ls_m-dispo.
        lv_zeig = lv_zeig + 1.
      ENDIF.
    ENDIF.
  ENDLOOP.
  WRITE: / 'ABDECKUNG|VF ohne Gruppe', lv_nicht.

  PERFORM batch_dispos USING p_batch CHANGING lt_d.
  IF lt_d IS INITIAL.
    WRITE: / 'Z22|Batch unbekannt', p_batch.
    RETURN.
  ENDIF.
  LOOP AT lt_d INTO lv_d.
    ls_dispo-sign = 'I'. ls_dispo-option = 'EQ'. ls_dispo-low = lv_d.
    APPEND ls_dispo TO lr_dispo.
  ENDLOOP.

  cl_salv_bs_runtime_info=>set( display = abap_false metadata = abap_false data = abap_true ).
  lv_u0 = sy-uzeit.
  GET RUN TIME FIELD lv_t0.
  SUBMIT zm_offene_fauf WITH s_dispo IN lr_dispo WITH p_plaf = ' ' WITH p_peg = ' ' WITH p_avo = 'X' AND RETURN.
  GET RUN TIME FIELD lv_t1.
  GET TIME.
  lv_u1 = sy-uzeit.
  lv_ms = ( lv_t1 - lv_t0 ) / 1000.
  TRY.
      cl_salv_bs_runtime_info=>get_data_ref( IMPORTING r_data = lr_data ).
    CATCH cx_root INTO lx.
      lv_txt = lx->get_text( ).
      WRITE: / 'Z22_FEHLER|', lv_txt.
      cl_salv_bs_runtime_info=>clear_all( ).
      RETURN.
  ENDTRY.
  cl_salv_bs_runtime_info=>clear_all( ).
  ASSIGN lr_data->* TO <lt>.
  LOOP AT <lt> ASSIGNING <ls>.
    CLEAR ls_k.
    lv_z22 = lv_z22 + 1.
    ASSIGN COMPONENT 'AUFNR' OF STRUCTURE <ls> TO <f>. ls_k-aufnr = <f>.
    ASSIGN COMPONENT 'STTXT' OF STRUCTURE <ls> TO <f>. ls_k-sttxt = <f>.
    ASSIGN COMPONENT 'ARBPL' OF STRUCTURE <ls> TO <f>. ls_k-arbpl = <f>.
    ASSIGN COMPONENT 'VORNR' OF STRUCTURE <ls> TO <f>. ls_k-vornr = <f>.
    SHIFT ls_k-aufnr LEFT DELETING LEADING '0'.
    READ TABLE lt_k INTO ls_k2 WITH TABLE KEY aufnr = ls_k-aufnr.
    IF sy-subrc = 0.
      IF ls_k2-sttxt <> ls_k-sttxt OR ls_k2-arbpl <> ls_k-arbpl OR ls_k2-vornr <> ls_k-vornr.
        lv_mehr = lv_mehr + 1.
      ENDIF.
    ELSE.
      INSERT ls_k INTO TABLE lt_k.
    ENDIF.
  ENDLOOP.
  lv_auf = lines( lt_k ).
  WRITE: / 'Z22|Batch', p_batch, '|ms', lv_ms, '|von', lv_u0, '|bis', lv_u1.
  WRITE: / 'Z22|Zeilen', lv_z22, '|Auftraege', lv_auf, '|Mehrfach abweichend', lv_mehr.

  CLEAR lv_zeig.
  LOOP AT gt_mit INTO ls_m WHERE typ = 'VF'.
    READ TABLE lt_d TRANSPORTING NO FIELDS WITH KEY table_line = ls_m-dispo.
    CHECK sy-subrc = 0.
    lv_set = lv_set + 1.
    IF ls_m-sttxt CP 'FREI DRUC RÜCK VOKL*'.
      lv_smu = lv_smu + 1.
    ENDIF.
    IF ls_m-arbpl CP 'M*'.
      lv_smm = lv_smm + 1.
    ENDIF.
    READ TABLE lt_k INTO ls_k WITH TABLE KEY aufnr = ls_m-aufnr.
    IF sy-subrc <> 0.
      lv_nurset = lv_nurset + 1.
      IF lv_zeig < 10.
        WRITE: / 'NUR_SET|', ls_m-aufnr, ls_m-dispo.
        lv_zeig = lv_zeig + 1.
      ENDIF.
      CONTINUE.
    ENDIF.
    IF ls_k-sttxt CP 'FREI DRUC RÜCK VOKL*'.
      lv_zmu = lv_zmu + 1.
    ENDIF.
    IF ls_k-arbpl CP 'M*'.
      lv_zmm = lv_zmm + 1.
    ENDIF.
    IF ls_k-sttxt = ls_m-sttxt AND ls_k-arbpl = ls_m-arbpl AND ls_k-vornr = ls_m-vornr.
      lv_gl = lv_gl + 1.
    ELSE.
      IF ls_k-sttxt <> ls_m-sttxt.
        lv_d_st = lv_d_st + 1.
      ENDIF.
      IF ls_k-arbpl <> ls_m-arbpl.
        lv_d_ar = lv_d_ar + 1.
      ENDIF.
      IF ls_k-vornr <> ls_m-vornr.
        lv_d_vo = lv_d_vo + 1.
      ENDIF.
      IF lv_zeig < 10.
        WRITE: / 'DIFF|', ls_m-aufnr, '|Set', ls_m-vornr, ls_m-arbpl, '|Z22', ls_k-vornr, ls_k-arbpl.
        WRITE: / 'DST|Set', ls_m-sttxt(80).
        WRITE: / 'DST|Z22', ls_k-sttxt(80).
        lv_zeig = lv_zeig + 1.
      ENDIF.
    ENDIF.
  ENDLOOP.
  WRITE: / 'VGL|Batch', p_batch, '|Set-VF', lv_set, '|nur Set', lv_nurset, '|gleich', lv_gl.
  WRITE: / 'VGL|Sttxt anders', lv_d_st, '|Arbpl anders', lv_d_ar, '|Vornr anders', lv_d_vo.
  WRITE: / 'VGL_MUSTER|Set Status', lv_smu, '|Z22 Status', lv_zmu, '|Set M*', lv_smm, '|Z22 M*', lv_zmm.
ENDFORM.
