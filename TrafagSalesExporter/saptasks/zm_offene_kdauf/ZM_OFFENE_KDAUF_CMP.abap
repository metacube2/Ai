REPORT zm_offene_kdauf_cmp LINE-SIZE 255 NO STANDARD PAGE HEADING.
*----------------------------------------------------------------------*
* Vergleich zweier Fassungen von ZM_OFFENE_KDAUF (2026-10-12, Claude)
*
* Startet Programm A und Programm B nacheinander mit derselben Selektion
* und dem versteckten Schalter P_VGL. Beide legen ihr Ergebnis (GT_UEBAUF)
* und ihre Laufzeiten im ABAP-Memory ab, statt die ALV-Liste zu zeigen.
* P_UPDBI ist immer aus: ZPOWERBI_KDAUF wird nicht angefasst.
*
* Die Selektion kommt aus einer Variante von ZM_OFFENE_KDAUF. Ein hier
* eingetragenes Lieferdatum ersetzt Datum, Woche und Monat der Variante,
* ein hier eingetragenes Werk ersetzt das Werk.
*
* Verglichen wird je Zeile ueber VBELN/POSNR/ETENR und dann jedes der
* Felder von ZMM_UEB_KDAUF. A = B gegen sich selbst (REF gegen REF) zeigt,
* ob das Original mit sich selbst uebereinstimmt.
*----------------------------------------------------------------------*

TYPES: ty_tab TYPE STANDARD TABLE OF zmm_ueb_kdauf WITH DEFAULT KEY,
       ty_par_tab TYPE STANDARD TABLE OF rsparams WITH DEFAULT KEY,
       BEGIN OF ty_info,
         prog       TYPE progname,
         ok         TYPE abap_bool,
         wand       TYPE p LENGTH 16 DECIMALS 3,
         n_ivbep    TYPE i,
         t_gesamt   TYPE p LENGTH 16 DECIMALS 3,
         t_ivbep    TYPE p LENGTH 16 DECIMALS 3,
         t_uebauf   TYPE p LENGTH 16 DECIMALS 3,
         t_prefetch TYPE p LENGTH 16 DECIMALS 3,
         t_bereich  TYPE p LENGTH 16 DECIMALS 3,
         t_fkdat    TYPE p LENGTH 16 DECIMALS 3,
         t_bezug    TYPE p LENGTH 16 DECIMALS 3,
         t_dispo    TYPE p LENGTH 16 DECIMALS 3,
         t_conv     TYPE p LENGTH 16 DECIMALS 3,
         n_mbew     TYPE i,
         n_flow_fm  TYPE i,
         n_flow_ohne TYPE i,
         n_flow_leer TYPE i,
       END OF ty_info,
       BEGIN OF ty_feld,
         name TYPE abap_compname,
         anz  TYPE i,
       END OF ty_feld,
       BEGIN OF ty_beisp,
         name   TYPE abap_compname,
         vbeln  TYPE vbeln,
         posnr  TYPE posnr,
         etenr  TYPE etenr,
         wert_a TYPE c LENGTH 40,
         wert_b TYPE c LENGTH 40,
       END OF ty_beisp,
       BEGIN OF ty_summe,
         zeilen  TYPE i,
         bmeng   TYPE p LENGTH 16 DECIMALS 3,
         netwr1  TYPE p LENGTH 16 DECIMALS 2,
         netwr2  TYPE p LENGTH 16 DECIMALS 2,
         netwr3n TYPE p LENGTH 16 DECIMALS 2,
         stdwert TYPE p LENGTH 16 DECIMALS 2,
       END OF ty_summe.

SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME.
  PARAMETERS: p_var TYPE rsvar-variant.
  PARAMETERS: p_werk TYPE werks_d.
  SELECT-OPTIONS: s_mbdat FOR sy-datum.
SELECTION-SCREEN END OF BLOCK b1.
SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME.
  PARAMETERS: p_prga  TYPE progname DEFAULT 'ZM_OFFENE_KDAUF_OPT',
              p_prgb  TYPE progname DEFAULT 'ZM_OFFENE_KDAUF_REF',
              p_beisp TYPE i DEFAULT 5.
SELECTION-SCREEN END OF BLOCK b2.
* Grosse Varianten laufen im Dialog in den TIME_OUT (600 s je Dialogschritt). P_BG plant
* denselben Vergleich als Hintergrundjob ein; der Job legt seine Liste in INDX(ZK) ab, und
* P_ZEIG zeigt die zuletzt abgelegte Liste im Dialog.
SELECTION-SCREEN BEGIN OF BLOCK b3 WITH FRAME.
  PARAMETERS: p_bg   AS CHECKBOX,
              p_zeig AS CHECKBOX.
SELECTION-SCREEN END OF BLOCK b3.
PARAMETERS: p_job NO-DISPLAY.

TABLES: indx.

DATA: gt_par TYPE STANDARD TABLE OF rsparams,
      gt_a   TYPE ty_tab,
      gt_b   TYPE ty_tab,
      gs_ia  TYPE ty_info,
      gs_ib  TYPE ty_info.

START-OF-SELECTION.
  IF p_zeig = abap_true.
    PERFORM ergebnis_zeigen.
    RETURN.
  ENDIF.
  IF p_bg = abap_true.
    PERFORM job_einplanen.
    RETURN.
  ENDIF.
  IF p_job = abap_true.
    PERFORM job_lauf.
    RETURN.
  ENDIF.
  PERFORM selektion_bauen.
  PERFORM selektion_zeigen.
  PERFORM lauf USING p_prga CHANGING gt_a gs_ia.
  PERFORM lauf USING p_prgb CHANGING gt_b gs_ib.
  PERFORM laufzeiten_zeigen.
  IF gs_ia-ok = abap_true AND gs_ib-ok = abap_true.
    PERFORM vergleichen.
  ELSE.
    WRITE: / 'ERGEBNIS: KEIN VERGLEICH, mindestens ein Programm hat kein Ergebnis abgelegt'.
  ENDIF.

*&---------------------------------------------------------------------*
FORM selektion_bauen.
  DATA: ls_par TYPE rsparams,
        lv_datum TYPE abap_bool.

  REFRESH gt_par.
  IF p_var IS NOT INITIAL.
    CALL FUNCTION 'RS_VARIANT_CONTENTS'
      EXPORTING
        report               = 'ZM_OFFENE_KDAUF'
        variant              = p_var
        move_or_write        = 'M'
      TABLES
        valutab              = gt_par
      EXCEPTIONS
        variant_non_existent = 1
        variant_obsolete     = 2
        OTHERS               = 3.
    IF sy-subrc <> 0.
      MESSAGE |Variante { p_var } von ZM_OFFENE_KDAUF nicht lesbar (sy-subrc { sy-subrc })| TYPE 'E'.
    ENDIF.
  ENDIF.

  " leere Zeilen von Select-Options entfernen
  DELETE gt_par WHERE kind = 'S' AND option IS INITIAL.

  IF s_mbdat[] IS NOT INITIAL.
    DELETE gt_par WHERE selname = 'S_MBDAT' OR selname = 'S_WOCHE1' OR selname = 'S_MONAT'.
    LOOP AT s_mbdat.
      CLEAR ls_par.
      ls_par-selname = 'S_MBDAT'.
      ls_par-kind    = 'S'.
      ls_par-sign    = s_mbdat-sign.
      ls_par-option  = s_mbdat-option.
      ls_par-low     = s_mbdat-low.
      ls_par-high    = s_mbdat-high.
      APPEND ls_par TO gt_par.
    ENDLOOP.
  ENDIF.

  IF p_werk IS NOT INITIAL.
    DELETE gt_par WHERE selname = 'P_WERK'.
    CLEAR ls_par.
    ls_par-selname = 'P_WERK'.
    ls_par-kind    = 'P'.
    ls_par-low     = p_werk.
    APPEND ls_par TO gt_par.
  ENDIF.

  " nie in ZPOWERBI_KDAUF schreiben, immer Vergleichslauf
  DELETE gt_par WHERE selname = 'P_UPDBI' OR selname = 'P_VGL'.
  CLEAR ls_par.
  ls_par-selname = 'P_UPDBI'.
  ls_par-kind    = 'P'.
  ls_par-low     = ' '.
  APPEND ls_par TO gt_par.
  CLEAR ls_par.
  ls_par-selname = 'P_VGL'.
  ls_par-kind    = 'P'.
  ls_par-low     = 'X'.
  APPEND ls_par TO gt_par.

  LOOP AT gt_par INTO ls_par WHERE selname = 'S_MBDAT' OR selname = 'S_WOCHE1' OR selname = 'S_MONAT'.
    lv_datum = abap_true.
  ENDLOOP.
  IF lv_datum = abap_false.
    MESSAGE 'Keine Datumsselektion: Variante mit Datum waehlen oder Lieferdatum eintragen' TYPE 'E'.
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
FORM selektion_zeigen.
  DATA ls_par TYPE rsparams.
  WRITE: / 'Vergleich ZM_OFFENE_KDAUF', sy-datum, sy-uzeit, 'System', sy-sysid, 'Mandant', sy-mandt.
  WRITE: / 'Variante:', p_var, ' A =', p_prga, ' B =', p_prgb.
  WRITE: / 'Selektion, mit der beide Programme gestartet werden:'.
  LOOP AT gt_par INTO ls_par.
    WRITE: / '  ', ls_par-selname, ls_par-kind, ls_par-sign, ls_par-option,
             ls_par-low(30), ls_par-high(30).
  ENDLOOP.
  ULINE.
ENDFORM.

*&---------------------------------------------------------------------*
FORM lauf USING iv_prog TYPE progname
          CHANGING ct_tab TYPE ty_tab
                   cs_info TYPE ty_info.
  DATA: lv_t0 TYPE timestampl,
        lv_t1 TYPE timestampl.

  CLEAR: ct_tab, cs_info.
  cs_info-prog = iv_prog.
  FREE MEMORY ID 'ZM_OFFENE_KDAUF_VGL'.
  GET TIME STAMP FIELD lv_t0.
  SUBMIT (iv_prog) WITH SELECTION-TABLE gt_par AND RETURN.
  GET TIME STAMP FIELD lv_t1.
  cs_info-wand = cl_abap_tstmp=>subtract( tstmp1 = lv_t1 tstmp2 = lv_t0 ).
  IMPORT tab        = ct_tab
         n_ivbep    = cs_info-n_ivbep
         t_gesamt   = cs_info-t_gesamt
         t_ivbep    = cs_info-t_ivbep
         t_uebauf   = cs_info-t_uebauf
         t_prefetch = cs_info-t_prefetch
         t_bereich  = cs_info-t_bereich
         t_fkdat    = cs_info-t_fkdat
         t_bezug    = cs_info-t_bezug
         t_dispo    = cs_info-t_dispo
         t_conv     = cs_info-t_conv
         n_mbew     = cs_info-n_mbew
         n_flow_fm  = cs_info-n_flow_fm
         n_flow_ohne = cs_info-n_flow_ohne
         n_flow_leer = cs_info-n_flow_leer
    FROM MEMORY ID 'ZM_OFFENE_KDAUF_VGL'.
  IF sy-subrc = 0.
    cs_info-ok = abap_true.
  ENDIF.
  FREE MEMORY ID 'ZM_OFFENE_KDAUF_VGL'.
ENDFORM.

*&---------------------------------------------------------------------*
FORM laufzeiten_zeigen.
  WRITE: / 'Laufzeiten in Sekunden', 50 'A', 74 'B'.
  WRITE: / 'Programm', AT 34(22) gs_ia-prog, AT 58(22) gs_ib-prog.
  WRITE: / 'Ergebnis abgelegt', AT 50 gs_ia-ok, AT 74 gs_ib-ok.
  WRITE: / 'Wanduhr ganzer SUBMIT', AT 34(20) gs_ia-wand, AT 58(20) gs_ib-wand.
  WRITE: / 'Programm gesamt', AT 34(20) gs_ia-t_gesamt, AT 58(20) gs_ib-t_gesamt.
  WRITE: / 'FILL_IVBEP', AT 34(20) gs_ia-t_ivbep, AT 58(20) gs_ib-t_ivbep.
  WRITE: / 'FILL_UEBAUF', AT 34(20) gs_ia-t_uebauf, AT 58(20) gs_ib-t_uebauf.
  WRITE: / '  davon Vorablesen', AT 34(20) gs_ia-t_prefetch, AT 58(20) gs_ib-t_prefetch.
  WRITE: / '  davon Z_BASISPREISLISTE', AT 34(20) gs_ia-t_bereich, AT 58(20) gs_ib-t_bereich.
  WRITE: / '  davon FILL_FKDAT (Belegfluss)', AT 34(20) gs_ia-t_fkdat, AT 58(20) gs_ib-t_fkdat.
  WRITE: / '  davon ERSTER_BEZUG (AUFM)', AT 34(20) gs_ia-t_bezug, AT 58(20) gs_ib-t_bezug.
  WRITE: / '  davon Dispo-Gruppen-Select', AT 34(20) gs_ia-t_dispo, AT 58(20) gs_ib-t_dispo.
  WRITE: / '  davon Waehrungsumrechnung', AT 34(20) gs_ia-t_conv, AT 58(20) gs_ib-t_conv.
  WRITE: / 'Einteilungen nach FILL_IVBEP', AT 34(20) gs_ia-n_ivbep, AT 58(20) gs_ib-n_ivbep.
  WRITE: / 'MBEW mehrfach je Material', AT 34(20) gs_ia-n_mbew, AT 58(20) gs_ib-n_mbew.
  WRITE: / 'Belegfluss-Aufrufe', AT 34(20) gs_ia-n_flow_fm, AT 58(20) gs_ib-n_flow_fm.
  WRITE: / 'Belegfluss gespart (kein VBFA)', AT 34(20) gs_ia-n_flow_ohne, AT 58(20) gs_ib-n_flow_ohne.
  WRITE: / 'Belegfluss ohne Fakturazeile', AT 34(20) gs_ia-n_flow_leer, AT 58(20) gs_ib-n_flow_leer.
  ULINE.
ENDFORM.

*&---------------------------------------------------------------------*
FORM summe USING it_tab TYPE ty_tab CHANGING cs_summe TYPE ty_summe.
  FIELD-SYMBOLS <z> TYPE zmm_ueb_kdauf.
  CLEAR cs_summe.
  cs_summe-zeilen = lines( it_tab ).
  LOOP AT it_tab ASSIGNING <z>.
    cs_summe-bmeng   = cs_summe-bmeng   + <z>-bmeng.
    cs_summe-netwr1  = cs_summe-netwr1  + <z>-netwr1.
    cs_summe-netwr2  = cs_summe-netwr2  + <z>-netwr2.
    cs_summe-netwr3n = cs_summe-netwr3n + <z>-netwr3n.
    cs_summe-stdwert = cs_summe-stdwert + <z>-standardwert.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
FORM vergleichen.
  DATA: lo_struct     TYPE REF TO cl_abap_structdescr,
        lt_feld       TYPE STANDARD TABLE OF ty_feld,
        ls_feld       TYPE ty_feld,
        lt_beisp      TYPE STANDARD TABLE OF ty_beisp,
        ls_beisp      TYPE ty_beisp,
        ls_sa         TYPE ty_summe,
        ls_sb         TYPE ty_summe,
        lv_ia         TYPE i VALUE 1,
        lv_ib         TYPE i VALUE 1,
        lv_nur_a      TYPE i,
        lv_nur_b      TYPE i,
        lv_paare      TYPE i,
        lv_paare_diff TYPE i,
        lv_dup_a      TYPE i,
        lv_dup_b      TYPE i,
        lv_diff       TYPE abap_bool,
        lv_a_kleiner  TYPE abap_bool,
        lv_reihe      TYPE abap_bool VALUE abap_true,
        lv_idx        TYPE i.
  FIELD-SYMBOLS: <a>  TYPE zmm_ueb_kdauf,
                 <b>  TYPE zmm_ueb_kdauf,
                 <fa> TYPE any,
                 <fb> TYPE any,
                 <f>  TYPE ty_feld,
                 <c>  TYPE abap_compdescr.

  " 1. Summen
  PERFORM summe USING gt_a CHANGING ls_sa.
  PERFORM summe USING gt_b CHANGING ls_sb.
  WRITE: / 'Summen', 50 'A', 76 'B'.
  WRITE: / 'Zeilen', AT 34(22) ls_sa-zeilen, AT 58(22) ls_sb-zeilen.
  WRITE: / 'BMENG', AT 34(22) ls_sa-bmeng, AT 58(22) ls_sb-bmeng.
  WRITE: / 'NETWR1', AT 34(22) ls_sa-netwr1, AT 58(22) ls_sb-netwr1.
  WRITE: / 'NETWR2', AT 34(22) ls_sa-netwr2, AT 58(22) ls_sb-netwr2.
  WRITE: / 'NETWR3N', AT 34(22) ls_sa-netwr3n, AT 58(22) ls_sb-netwr3n.
  WRITE: / 'STANDARDWERT', AT 34(22) ls_sa-stdwert, AT 58(22) ls_sb-stdwert.
  ULINE.

  " 2. Reihenfolge der Liste (so wie das Programm sie zeigen wuerde)
  IF lines( gt_a ) <> lines( gt_b ).
    lv_reihe = abap_false.
  ELSE.
    LOOP AT gt_a ASSIGNING <a>.
      lv_idx = sy-tabix.
      READ TABLE gt_b ASSIGNING <b> INDEX lv_idx.
      IF <a>-vbeln <> <b>-vbeln OR <a>-posnr <> <b>-posnr OR <a>-etenr <> <b>-etenr.
        lv_reihe = abap_false.
        EXIT.
      ENDIF.
    ENDLOOP.
  ENDIF.

  " 3. Zeilen ueber VBELN/POSNR/ETENR zuordnen, dann Feld fuer Feld
  lo_struct ?= cl_abap_typedescr=>describe_by_name( 'ZMM_UEB_KDAUF' ).
  LOOP AT lo_struct->components ASSIGNING <c>.
    CLEAR ls_feld.
    ls_feld-name = <c>-name.
    APPEND ls_feld TO lt_feld.
  ENDLOOP.

  SORT gt_a BY vbeln posnr etenr.
  SORT gt_b BY vbeln posnr etenr.
  PERFORM doppelte USING gt_a CHANGING lv_dup_a.
  PERFORM doppelte USING gt_b CHANGING lv_dup_b.

  WHILE lv_ia <= lines( gt_a ) OR lv_ib <= lines( gt_b ).
    UNASSIGN: <a>, <b>.
    READ TABLE gt_a ASSIGNING <a> INDEX lv_ia.
    READ TABLE gt_b ASSIGNING <b> INDEX lv_ib.

    IF <a> IS ASSIGNED AND <b> IS ASSIGNED
       AND <a>-vbeln = <b>-vbeln AND <a>-posnr = <b>-posnr AND <a>-etenr = <b>-etenr.
      lv_paare = lv_paare + 1.
      lv_diff = abap_false.
      LOOP AT lt_feld ASSIGNING <f>.
        ASSIGN COMPONENT <f>-name OF STRUCTURE <a> TO <fa>.
        ASSIGN COMPONENT <f>-name OF STRUCTURE <b> TO <fb>.
        IF <fa> <> <fb>.
          lv_diff = abap_true.
          <f>-anz = <f>-anz + 1.
          IF <f>-anz <= p_beisp.
            CLEAR ls_beisp.
            ls_beisp-name   = <f>-name.
            ls_beisp-vbeln  = <a>-vbeln.
            ls_beisp-posnr  = <a>-posnr.
            ls_beisp-etenr  = <a>-etenr.
            ls_beisp-wert_a = |{ <fa> }|.
            ls_beisp-wert_b = |{ <fb> }|.
            APPEND ls_beisp TO lt_beisp.
          ENDIF.
        ENDIF.
      ENDLOOP.
      IF lv_diff = abap_true.
        lv_paare_diff = lv_paare_diff + 1.
      ENDIF.
      lv_ia = lv_ia + 1.
      lv_ib = lv_ib + 1.
      CONTINUE.
    ENDIF.

    " welche Seite hat den kleineren Schluessel?
    IF <b> IS NOT ASSIGNED.
      lv_a_kleiner = abap_true.
    ELSEIF <a> IS NOT ASSIGNED.
      lv_a_kleiner = abap_false.
    ELSEIF <a>-vbeln < <b>-vbeln
        OR ( <a>-vbeln = <b>-vbeln AND <a>-posnr < <b>-posnr )
        OR ( <a>-vbeln = <b>-vbeln AND <a>-posnr = <b>-posnr AND <a>-etenr < <b>-etenr ).
      lv_a_kleiner = abap_true.
    ELSE.
      lv_a_kleiner = abap_false.
    ENDIF.

    IF lv_a_kleiner = abap_true.
      lv_nur_a = lv_nur_a + 1.
      IF lv_nur_a <= p_beisp.
        WRITE: / 'nur in A:', <a>-vbeln, <a>-posnr, <a>-etenr, <a>-edatu, <a>-werk, <a>-matnr(18).
      ENDIF.
      lv_ia = lv_ia + 1.
    ELSE.
      lv_nur_b = lv_nur_b + 1.
      IF lv_nur_b <= p_beisp.
        WRITE: / 'nur in B:', <b>-vbeln, <b>-posnr, <b>-etenr, <b>-edatu, <b>-werk, <b>-matnr(18).
      ENDIF.
      lv_ib = lv_ib + 1.
    ENDIF.
  ENDWHILE.

  WRITE: / 'Zeilen nur in A', AT 50 lv_nur_a.
  WRITE: / 'Zeilen nur in B', AT 50 lv_nur_b.
  WRITE: / 'Zeilen in beiden', AT 50 lv_paare.
  WRITE: / '  davon mit mindestens einer Feldabweichung', AT 50 lv_paare_diff.
  WRITE: / 'Doppelte Schluessel VBELN/POSNR/ETENR in A', AT 50 lv_dup_a.
  WRITE: / 'Doppelte Schluessel VBELN/POSNR/ETENR in B', AT 50 lv_dup_b.
  WRITE: / 'Reihenfolge der Liste gleich', AT 50 lv_reihe.
  ULINE.

  WRITE: / 'Abweichungen je Feld (Anzahl Zeilen):'.
  LOOP AT lt_feld INTO ls_feld WHERE anz > 0.
    WRITE: / '  ', ls_feld-name(20), ls_feld-anz.
    LOOP AT lt_beisp INTO ls_beisp WHERE name = ls_feld-name.
      WRITE: / '      ', ls_beisp-vbeln, ls_beisp-posnr, ls_beisp-etenr,
               'A=', ls_beisp-wert_a, 'B=', ls_beisp-wert_b.
    ENDLOOP.
  ENDLOOP.
  ULINE.

  IF lv_nur_a = 0 AND lv_nur_b = 0 AND lv_paare_diff = 0 AND lv_dup_a = 0 AND lv_dup_b = 0.
    WRITE: / 'ERGEBNIS: IDENTISCH', lv_paare, 'Zeilen, alle Felder gleich'.
  ELSE.
    WRITE: / 'ERGEBNIS: ABWEICHUNGEN'.
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
FORM doppelte USING it_tab TYPE ty_tab CHANGING cv_anz TYPE i.
  FIELD-SYMBOLS: <z>   TYPE zmm_ueb_kdauf.
  DATA: ls_vor TYPE zmm_ueb_kdauf,
        lv_erst TYPE abap_bool VALUE abap_true.
  CLEAR cv_anz.
  LOOP AT it_tab ASSIGNING <z>.
    IF lv_erst = abap_false AND <z>-vbeln = ls_vor-vbeln AND <z>-posnr = ls_vor-posnr
       AND <z>-etenr = ls_vor-etenr.
      cv_anz = cv_anz + 1.
    ENDIF.
    ls_vor = <z>.
    lv_erst = abap_false.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
* Hintergrund: eigene Selektion ohne P_BG/P_ZEIG, mit P_JOB = X
FORM eigene_selektion CHANGING ct_self TYPE ty_par_tab.
  CALL FUNCTION 'RS_REFRESH_FROM_SELECTOPTIONS'
    EXPORTING
      curr_report     = sy-repid
    TABLES
      selection_table = ct_self
    EXCEPTIONS
      OTHERS          = 1.
  DELETE ct_self WHERE selname = 'P_BG' OR selname = 'P_ZEIG' OR selname = 'P_JOB'.
ENDFORM.

FORM job_einplanen.
  DATA: lt_self  TYPE ty_par_tab,
        ls_par   TYPE rsparams,
        lv_name  TYPE btcjob VALUE 'ZM_OFFENE_KDAUF_CMP',
        lv_nr    TYPE btcjobcnt.

  PERFORM eigene_selektion CHANGING lt_self.
  ls_par-selname = 'P_JOB'.
  ls_par-kind    = 'P'.
  ls_par-low     = 'X'.
  APPEND ls_par TO lt_self.

  CALL FUNCTION 'JOB_OPEN'
    EXPORTING
      jobname  = lv_name
    IMPORTING
      jobcount = lv_nr
    EXCEPTIONS
      OTHERS   = 1.
  IF sy-subrc <> 0.
    WRITE: / 'JOB_OPEN fehlgeschlagen', sy-subrc.
    RETURN.
  ENDIF.
  SUBMIT zm_offene_kdauf_cmp WITH SELECTION-TABLE lt_self
         VIA JOB lv_name NUMBER lv_nr AND RETURN.
  CALL FUNCTION 'JOB_CLOSE'
    EXPORTING
      jobcount  = lv_nr
      jobname   = lv_name
      strtimmed = 'X'
    EXCEPTIONS
      OTHERS    = 1.
  WRITE: / 'JOB EINGEPLANT', lv_name, lv_nr, 'sy-subrc', sy-subrc.
ENDFORM.

FORM job_lauf.
  DATA: lt_self  TYPE ty_par_tab,
        lt_liste TYPE STANDARD TABLE OF abaplist,
        lt_txt   TYPE STANDARD TABLE OF char255,
        lv_txt   TYPE char255,
        lv_stand TYPE timestamp.

  PERFORM eigene_selektion CHANGING lt_self.
  SUBMIT zm_offene_kdauf_cmp WITH SELECTION-TABLE lt_self
         EXPORTING LIST TO MEMORY AND RETURN.
  CALL FUNCTION 'LIST_FROM_MEMORY'
    TABLES
      listobject = lt_liste
    EXCEPTIONS
      OTHERS     = 1.
  IF sy-subrc = 0.
    CALL FUNCTION 'LIST_TO_ASCI'
      TABLES
        listasci   = lt_txt
        listobject = lt_liste
      EXCEPTIONS
        OTHERS     = 1.
  ENDIF.
  IF lt_txt IS INITIAL.
    APPEND 'KEINE LISTE vom Vergleichslauf erhalten' TO lt_txt.
  ENDIF.
  GET TIME STAMP FIELD lv_stand.
  EXPORT liste = lt_txt stand = lv_stand TO DATABASE indx(zk) ID 'ZM_OFFENE_KDAUF_CMP'.
  COMMIT WORK.
  LOOP AT lt_txt INTO lv_txt.
    WRITE: / lv_txt.
  ENDLOOP.
ENDFORM.

FORM ergebnis_zeigen.
  DATA: lt_txt   TYPE STANDARD TABLE OF char255,
        lv_txt   TYPE char255,
        lv_stand TYPE timestamp.
  IMPORT liste = lt_txt stand = lv_stand FROM DATABASE indx(zk) ID 'ZM_OFFENE_KDAUF_CMP'.
  IF sy-subrc <> 0.
    WRITE: / 'KEIN ABGELEGTES ERGEBNIS'.
    RETURN.
  ENDIF.
  WRITE: / 'ABGELEGT', lv_stand TIME ZONE sy-zonlo.
  LOOP AT lt_txt INTO lv_txt.
    WRITE: / lv_txt.
  ENDLOOP.
ENDFORM.
