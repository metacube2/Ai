
*&---------------------------------------------------------------------*
*&      Form  MATERIALANTEIL_RECHNEN
*&---------------------------------------------------------------------*
* 2026-10-15 (Entscheid Ingo): Materialanteil je Position = Material je Basiseinheit x BMENG.
* Material wie ZM03 (ZLO_CALC_CZ_PREISE): Zukaufteile, Teile ohne eigene Kalkulation und Teile
* aus einem anderen Werk zaehlen als Material; Eigenfertigung wird ueber die Unterkalkulation
* der CKIS-Position aufgeloest. Eigenleistung (E) nie, Fremdleistung (F) nur mit P_MFREM.
* Laeuft nach VERGLEICH_EXPORT, damit der Vergleich mit ZM_OFFENE_KDAUF_CMP unberuehrt bleibt.
* Schnelle Fassung: Kalkulationen stufenweise gesammelt lesen (MATA_LADEN), dann im Speicher rechnen.
FORM materialanteil_rechnen.
  FIELD-SYMBOLS <ls_ueb> TYPE zmm_ueb_kdauf.
  DATA: ls_mat     TYPE ty_mata_mat,
        lv_rt0     TYPE i,
        lv_rt1     TYPE i.

  CHECK p_mata = abap_true.
  GET RUN TIME FIELD lv_rt0.

  PERFORM mata_laden.

  LOOP AT gt_mata_mat INTO ls_mat.
    PERFORM mata_wert USING ls_mat-kalnr ls_mat-kalka ls_mat-kadky ls_mat-tvers ls_mat-bwvar
                      CHANGING ls_mat-wert ls_mat-cz.
    MODIFY TABLE gt_mata_mat FROM ls_mat.
  ENDLOOP.

  LOOP AT gt_uebauf ASSIGNING <ls_ueb>.
    CHECK <ls_ueb>-matnr IS NOT INITIAL.
    READ TABLE gt_mata_mat INTO ls_mat WITH TABLE KEY matnr = <ls_ueb>-matnr.
    CHECK sy-subrc = 0.
    <ls_ueb>-matpreis  = ls_mat-wert.
    <ls_ueb>-matanteil = ls_mat-wert * <ls_ueb>-bmeng.
    <ls_ueb>-arbeit_cz = ls_mat-cz * <ls_ueb>-bmeng.   " Arbeit CZ wie ZM03 (2026-10-15)
    IF <ls_ueb>-standardwert > 0 AND <ls_ueb>-matanteil > <ls_ueb>-standardwert.
      gv_n_mata_gross = gv_n_mata_gross + 1.
    ENDIF.
  ENDLOOP.

  GET RUN TIME FIELD lv_rt1.
  gv_t_mata = ( lv_rt1 - lv_rt0 ) / 1000.
  gv_n_mata_kalk = lines( gt_mata_kopf ).
ENDFORM.                    "materialanteil_rechnen

*&---------------------------------------------------------------------*
*&      Form  MATA_LADEN
*&---------------------------------------------------------------------*
* 1. Je Kopfmaterial die passende Standardkalkulation (freigegeben oder juengste).
* 2. Stufenweise: Koepfe und Positionen aller offenen Kalkulationen, Beschaffungsart der
*    Positionsmaterialien, dann die Unterkalkulationen der Eigenfertigung als naechste Stufe.
FORM mata_laden.
  TYPES: BEGIN OF lty_matnr,
           matnr TYPE matnr,
         END OF lty_matnr,
         BEGIN OF lty_keko,
           matnr   TYPE keko-matnr,
           kalnr   TYPE keko-kalnr,
           kalka   TYPE keko-kalka,
           kadky   TYPE keko-kadky,
           tvers   TYPE keko-tvers,
           bwvar   TYPE keko-bwvar,
           kadat   TYPE keko-kadat,
           freig   TYPE keko-freig,
           feh_sta TYPE keko-feh_sta,
         END OF lty_keko,
         BEGIN OF lty_mw,
           matnr TYPE marc-matnr,
           werks TYPE marc-werks,
         END OF lty_mw.
  DATA: lt_matnr TYPE SORTED TABLE OF lty_matnr WITH UNIQUE KEY matnr,
        lt_keko  TYPE STANDARD TABLE OF lty_keko,
        ls_keko  TYPE lty_keko,
        ls_mat   TYPE ty_mata_mat,
        lt_offen TYPE SORTED TABLE OF ty_mata_key WITH UNIQUE KEY kalnr kalka kadky tvers bwvar,
        lt_neu   TYPE SORTED TABLE OF ty_mata_key WITH UNIQUE KEY kalnr kalka kadky tvers bwvar,
        ls_key   TYPE ty_mata_key,
        lt_kopf  TYPE STANDARD TABLE OF ty_mata_kopf,
        ls_kopf  TYPE ty_mata_kopf,
        lt_pos   TYPE STANDARD TABLE OF ty_mata_pos,
        ls_pos   TYPE ty_mata_pos,
        lt_mw    TYPE SORTED TABLE OF lty_mw WITH UNIQUE KEY matnr werks,
        ls_mw    TYPE lty_mw,
        lt_beskz TYPE STANDARD TABLE OF ty_mata_beskz,
        ls_beskz TYPE ty_mata_beskz,
        lv_kenn  TYPE c LENGTH 12.
  FIELD-SYMBOLS <ls_ueb> TYPE zmm_ueb_kdauf.

  " CZ-Arbeitsplaetze einmal laden (ZM03: CRHD ARBPL LIKE 'CZ%', Objekttyp A)
  SELECT objid FROM crhd INTO TABLE gt_mata_czap WHERE objty = 'A' AND arbpl LIKE 'CZ%'.

  " 1. Kopfmaterialien der Liste
  LOOP AT gt_uebauf ASSIGNING <ls_ueb> WHERE matnr IS NOT INITIAL.
    INSERT VALUE #( matnr = <ls_ueb>-matnr ) INTO TABLE lt_matnr.
  ENDLOOP.
  CHECK lt_matnr IS NOT INITIAL.

  SELECT matnr kalnr kalka kadky tvers bwvar kadat freig feh_sta
    FROM keko INTO TABLE lt_keko
    FOR ALL ENTRIES IN lt_matnr
    WHERE matnr = lt_matnr-matnr
      AND werks = p_werk
      AND kalka = '01'
      AND tvers = '01'
      AND loekz = space.

  IF p_mfrei = abap_true.
    " freigegebene, die heute gilt: juengstes Gueltig-ab bis heute
    DELETE lt_keko WHERE freig <> 'X' OR kadat > sy-datum.
    SORT lt_keko BY matnr ASCENDING kadat DESCENDING kadky DESCENDING.
  ELSE.
    " juengste wie ZM03
    DELETE lt_keko WHERE feh_sta = 'KF' OR kadky > sy-datum.
    SORT lt_keko BY matnr ASCENDING kadky DESCENDING.
  ENDIF.
  DELETE ADJACENT DUPLICATES FROM lt_keko COMPARING matnr.

  LOOP AT lt_keko INTO ls_keko.
    CLEAR ls_mat.
    MOVE-CORRESPONDING ls_keko TO ls_mat.
    INSERT ls_mat INTO TABLE gt_mata_mat.
    MOVE-CORRESPONDING ls_keko TO ls_key.
    INSERT ls_key INTO TABLE lt_offen.
  ENDLOOP.
  gv_n_mata_ohne = lines( lt_matnr ) - lines( lt_keko ).

  " 2. Stufenweise laden, hoechstens 30 Stufen
  DO 30 TIMES.
    IF lt_offen IS INITIAL.
      EXIT.
    ENDIF.

    CLEAR lt_kopf.
    SELECT kalnr kalka kadky tvers bwvar losgr werks
      FROM keko INTO TABLE lt_kopf
      FOR ALL ENTRIES IN lt_offen
      WHERE bzobj = '0'
        AND kalnr = lt_offen-kalnr
        AND kalka = lt_offen-kalka
        AND kadky = lt_offen-kadky
        AND tvers = lt_offen-tvers
        AND bwvar = lt_offen-bwvar.
    SORT lt_kopf BY kalnr kalka kadky tvers bwvar.
    " Jeder offene Schluessel gilt danach als geladen, auch ohne Treffer (Losgroesse 0 = Wert 0)
    LOOP AT lt_offen INTO ls_key.
      CLEAR ls_kopf.
      MOVE-CORRESPONDING ls_key TO ls_kopf.
      READ TABLE lt_kopf INTO ls_kopf WITH KEY kalnr = ls_key-kalnr kalka = ls_key-kalka
                                               kadky = ls_key-kadky tvers = ls_key-tvers
                                               bwvar = ls_key-bwvar BINARY SEARCH.
      IF sy-subrc <> 0.
        CLEAR ls_kopf.
        MOVE-CORRESPONDING ls_key TO ls_kopf.
      ENDIF.
      INSERT ls_kopf INTO TABLE gt_mata_kopf.
    ENDLOOP.

    CLEAR lt_pos.
    SELECT kalnr kalka kadky tvers bwvar posnr typps matnr werks menge wrtfw_kpf arbid
           ukaln ukalka ukadky utvers ubwvar
      FROM ckis INTO TABLE lt_pos
      FOR ALL ENTRIES IN lt_offen
      WHERE lednr = '00'
        AND bzobj = '0'
        AND kalnr = lt_offen-kalnr
        AND kalka = lt_offen-kalka
        AND kadky = lt_offen-kadky
        AND tvers = lt_offen-tvers
        AND bwvar = lt_offen-bwvar.
    DELETE lt_pos WHERE wrtfw_kpf = 0.
    INSERT LINES OF lt_pos INTO TABLE gt_mata_pos.

    " Beschaffungsart der Positionsmaterialien, die noch nicht bekannt ist
    CLEAR lt_mw.
    LOOP AT lt_pos INTO ls_pos WHERE ( typps = 'M' OR typps = 'I' ) AND matnr IS NOT INITIAL.
      READ TABLE gt_mata_beskz TRANSPORTING NO FIELDS WITH TABLE KEY matnr = ls_pos-matnr werks = ls_pos-werks.
      IF sy-subrc <> 0.
        INSERT VALUE #( matnr = ls_pos-matnr werks = ls_pos-werks ) INTO TABLE lt_mw.
      ENDIF.
    ENDLOOP.
    IF lt_mw IS NOT INITIAL.
      CLEAR lt_beskz.
      SELECT matnr werks beskz FROM marc INTO TABLE lt_beskz
        FOR ALL ENTRIES IN lt_mw
        WHERE matnr = lt_mw-matnr AND werks = lt_mw-werks.
      SORT lt_beskz BY matnr werks.
      LOOP AT lt_mw INTO ls_mw.
        READ TABLE lt_beskz INTO ls_beskz WITH KEY matnr = ls_mw-matnr werks = ls_mw-werks BINARY SEARCH.
        IF sy-subrc <> 0.
          CLEAR ls_beskz.
          ls_beskz-matnr = ls_mw-matnr.
          ls_beskz-werks = ls_mw-werks.
        ENDIF.
        INSERT ls_beskz INTO TABLE gt_mata_beskz.
      ENDLOOP.
    ENDIF.

    " Naechste Stufe: Unterkalkulationen der Eigenfertigung, die noch nicht geladen sind
    CLEAR lt_neu.
    LOOP AT lt_pos INTO ls_pos WHERE ( typps = 'M' OR typps = 'I' ) AND ukaln IS NOT INITIAL.
      PERFORM mata_ist_material USING ls_pos CHANGING lv_kenn.
      CHECK lv_kenn IS INITIAL.   " leer = nicht Material, also aufloesen
      ls_key-kalnr = ls_pos-ukaln.
      ls_key-kalka = ls_pos-ukalka.
      ls_key-kadky = ls_pos-ukadky.
      ls_key-tvers = ls_pos-utvers.
      ls_key-bwvar = ls_pos-ubwvar.
      READ TABLE gt_mata_kopf TRANSPORTING NO FIELDS WITH TABLE KEY kalnr = ls_key-kalnr kalka = ls_key-kalka
                                                                    kadky = ls_key-kadky tvers = ls_key-tvers
                                                                    bwvar = ls_key-bwvar.
      IF sy-subrc <> 0.
        INSERT ls_key INTO TABLE lt_neu.
      ENDIF.
    ENDLOOP.
    lt_offen = lt_neu.
  ENDDO.
ENDFORM.                    "mata_laden

*&---------------------------------------------------------------------*
*&      Form  MATA_IST_MATERIAL
*&---------------------------------------------------------------------*
* Regel wie ZM03 fuer eine Materialposition: Material (Ergebnis 'X'), wenn ohne Unterkalkulation,
* Zukaufteil (BESKZ F) oder aus einem anderen Werk als die Kalkulation; sonst leer (aufloesen).
FORM mata_ist_material USING    is_pos   TYPE ty_mata_pos
                       CHANGING cv_kenn  TYPE clike.
  DATA: ls_kopf  TYPE ty_mata_kopf,
        ls_beskz TYPE ty_mata_beskz.

  CLEAR cv_kenn.
  READ TABLE gt_mata_beskz INTO ls_beskz WITH TABLE KEY matnr = is_pos-matnr werks = is_pos-werks.
  READ TABLE gt_mata_kopf INTO ls_kopf WITH TABLE KEY kalnr = is_pos-kalnr kalka = is_pos-kalka
                                                     kadky = is_pos-kadky tvers = is_pos-tvers
                                                     bwvar = is_pos-bwvar.
  IF is_pos-ukaln IS INITIAL OR ls_beskz-beskz = 'F' OR is_pos-werks <> ls_kopf-werks.
    cv_kenn = 'X'.
  ENDIF.
ENDFORM.                    "mata_ist_material

*&---------------------------------------------------------------------*
*&      Form  MATA_WERT
*&---------------------------------------------------------------------*
* Material und Arbeit CZ je Basiseinheit einer Kalkulation, nur aus den geladenen Tabellen, je Kalkulation einmal.
FORM mata_wert USING    iv_kalnr TYPE keko-kalnr
                        iv_kalka TYPE keko-kalka
                        iv_kadky TYPE keko-kadky
                        iv_tvers TYPE keko-tvers
                        iv_bwvar TYPE keko-bwvar
               CHANGING cv_wert  TYPE p
                        cv_cz    TYPE p.
  DATA: ls_kalk TYPE ty_mata_kalk,
        ls_kopf TYPE ty_mata_kopf,
        ls_pos  TYPE ty_mata_pos,
        lv_kenn TYPE c LENGTH 12,
        lv_sub  TYPE p LENGTH 16 DECIMALS 6,
        lv_subcz TYPE p LENGTH 16 DECIMALS 6,
        lv_sum  TYPE p LENGTH 16 DECIMALS 6,
        lv_cz   TYPE p LENGTH 16 DECIMALS 6.

  READ TABLE gt_mata_kalk INTO ls_kalk WITH TABLE KEY kalnr = iv_kalnr kalka = iv_kalka
                                                     kadky = iv_kadky tvers = iv_tvers
                                                     bwvar = iv_bwvar.
  IF sy-subrc = 0.
    cv_wert = ls_kalk-wert.
    cv_cz   = ls_kalk-cz.
    RETURN.
  ENDIF.

  " Vorab mit 0 eintragen: schuetzt vor Zyklen in der Kalkulationsstruktur
  CLEAR ls_kalk.
  ls_kalk-kalnr = iv_kalnr.
  ls_kalk-kalka = iv_kalka.
  ls_kalk-kadky = iv_kadky.
  ls_kalk-tvers = iv_tvers.
  ls_kalk-bwvar = iv_bwvar.
  INSERT ls_kalk INTO TABLE gt_mata_kalk.

  gv_mata_tiefe = gv_mata_tiefe + 1.
  READ TABLE gt_mata_kopf INTO ls_kopf WITH TABLE KEY kalnr = iv_kalnr kalka = iv_kalka
                                                     kadky = iv_kadky tvers = iv_tvers
                                                     bwvar = iv_bwvar.
  IF sy-subrc = 0 AND ls_kopf-losgr > 0 AND gv_mata_tiefe <= 30.
    LOOP AT gt_mata_pos INTO ls_pos WHERE kalnr = iv_kalnr AND kalka = iv_kalka
                                      AND kadky = iv_kadky AND tvers = iv_tvers
                                      AND bwvar = iv_bwvar.
      CASE ls_pos-typps.
        WHEN 'M' OR 'I'.
          PERFORM mata_ist_material USING ls_pos CHANGING lv_kenn.
          IF lv_kenn = 'X'.
            lv_sum = lv_sum + ls_pos-wrtfw_kpf.
          ELSE.
            PERFORM mata_wert USING ls_pos-ukaln ls_pos-ukalka ls_pos-ukadky
                                    ls_pos-utvers ls_pos-ubwvar
                              CHANGING lv_sub lv_subcz.
            lv_sum = lv_sum + lv_sub * ls_pos-menge.
            lv_cz  = lv_cz + lv_subcz * ls_pos-menge.
          ENDIF.
        WHEN 'F'.
          IF p_mfrem = abap_true.
            lv_sum = lv_sum + ls_pos-wrtfw_kpf.
          ENDIF.
        WHEN 'E'.
          " Eigenleistung ist kein Material; an CZ-Arbeitsplaetzen zaehlt sie als Arbeit CZ (wie ZM03)
          READ TABLE gt_mata_czap TRANSPORTING NO FIELDS WITH TABLE KEY objid = ls_pos-arbid.
          IF sy-subrc = 0.
            lv_cz = lv_cz + ls_pos-wrtfw_kpf.
          ENDIF.
        WHEN OTHERS.
          " Zuschlaege und uebrige Positionen sind kein Material
      ENDCASE.
    ENDLOOP.
    ls_kalk-wert = lv_sum / ls_kopf-losgr.
    ls_kalk-cz   = lv_cz / ls_kopf-losgr.
  ENDIF.
  gv_mata_tiefe = gv_mata_tiefe - 1.

  MODIFY TABLE gt_mata_kalk FROM ls_kalk.
  cv_wert = ls_kalk-wert.
  cv_cz   = ls_kalk-cz.
ENDFORM.                    "mata_wert
