
*&---------------------------------------------------------------------*
*&      Form  MATERIALANTEIL_RECHNEN
*&---------------------------------------------------------------------*
* 2026-10-15 (Entscheid Ingo): Materialanteil je Position = Material je Basiseinheit x BMENG.
* Material wie ZM03 (ZLO_CALC_CZ_PREISE): Zukaufteile, Teile ohne eigene Kalkulation und Teile
* aus einem anderen Werk zaehlen als Material; Eigenfertigung wird ueber die Unterkalkulation
* der CKIS-Position aufgeloest. Eigenleistung (E) nie, Fremdleistung (F) nur mit P_MFREM.
* Laeuft nach VERGLEICH_EXPORT, damit der Vergleich mit ZM_OFFENE_KDAUF_CMP unberuehrt bleibt.
FORM materialanteil_rechnen.
  FIELD-SYMBOLS <ls_ueb> TYPE zmm_ueb_kdauf.
  DATA lv_wert TYPE p LENGTH 16 DECIMALS 6.

  LOOP AT gt_uebauf ASSIGNING <ls_ueb>.
    CHECK <ls_ueb>-matnr IS NOT INITIAL.
    PERFORM mata_material USING <ls_ueb>-matnr p_werk CHANGING lv_wert.
    <ls_ueb>-matpreis  = lv_wert.
    <ls_ueb>-matanteil = lv_wert * <ls_ueb>-bmeng.
    IF <ls_ueb>-standardwert > 0 AND <ls_ueb>-matanteil > <ls_ueb>-standardwert.
      gv_n_mata_gross = gv_n_mata_gross + 1.
    ENDIF.
  ENDLOOP.
ENDFORM.                    "materialanteil_rechnen

*&---------------------------------------------------------------------*
*&      Form  MATA_MATERIAL
*&---------------------------------------------------------------------*
* Material je Basiseinheit fuer ein Material, je Material und Werk einmal gerechnet.
FORM mata_material USING    iv_matnr TYPE matnr
                            iv_werks TYPE werks_d
                   CHANGING cv_wert  TYPE p.
  DATA: ls_mat  TYPE ty_mata_mat,
        ls_keko TYPE keko.

  READ TABLE gt_mata_mat INTO ls_mat WITH TABLE KEY matnr = iv_matnr werks = iv_werks.
  IF sy-subrc = 0.
    cv_wert = ls_mat-wert.
    RETURN.
  ENDIF.

  CLEAR ls_mat.
  ls_mat-matnr = iv_matnr.
  ls_mat-werks = iv_werks.

  IF p_mfrei = abap_true.
    " Freigegebene Standardkalkulation, die heute gilt (gehoert zum Standardpreis)
    SELECT * FROM keko INTO ls_keko UP TO 1 ROWS
      WHERE matnr = iv_matnr
        AND werks = iv_werks
        AND kalka = '01'
        AND tvers = '01'
        AND freig = 'X'
        AND loekz = space
        AND kadat <= sy-datum
      ORDER BY kadat DESCENDING.
    ENDSELECT.
  ELSE.
    " Juengste Standardkalkulation wie ZM03
    SELECT * FROM keko INTO ls_keko UP TO 1 ROWS
      WHERE matnr = iv_matnr
        AND werks = iv_werks
        AND kalka = '01'
        AND tvers = '01'
        AND loekz = space
        AND feh_sta <> 'KF'
        AND kadky <= sy-datum
      ORDER BY kadky DESCENDING.
    ENDSELECT.
  ENDIF.

  IF sy-subrc = 0.
    PERFORM mata_kalkulation USING ls_keko-kalnr ls_keko-kalka ls_keko-kadky
                                   ls_keko-tvers ls_keko-bwvar
                             CHANGING ls_mat-wert.
    ls_mat-ok = abap_true.
  ELSE.
    gv_n_mata_ohne = gv_n_mata_ohne + 1.
  ENDIF.

  INSERT ls_mat INTO TABLE gt_mata_mat.
  cv_wert = ls_mat-wert.
ENDFORM.                    "mata_material

*&---------------------------------------------------------------------*
*&      Form  MATA_KALKULATION
*&---------------------------------------------------------------------*
* Material je Basiseinheit einer Kalkulation (Summe Material / Kalkulationslosgroesse),
* je Kalkulation einmal gerechnet. Rekursiv ueber die Unterkalkulationen der Positionen.
FORM mata_kalkulation USING    iv_kalnr TYPE keko-kalnr
                               iv_kalka TYPE keko-kalka
                               iv_kadky TYPE keko-kadky
                               iv_tvers TYPE keko-tvers
                               iv_bwvar TYPE keko-bwvar
                      CHANGING cv_wert  TYPE p.
  TYPES: BEGIN OF lty_pos,
           typps     TYPE ckis-typps,
           matnr     TYPE ckis-matnr,
           werks     TYPE ckis-werks,
           menge     TYPE ckis-menge,
           wrtfw_kpf TYPE ckis-wrtfw_kpf,
           ukaln     TYPE ckis-ukaln,
           ukalka    TYPE ckis-ukalka,
           ukadky    TYPE ckis-ukadky,
           utvers    TYPE ckis-utvers,
           ubwvar    TYPE ckis-ubwvar,
         END OF lty_pos.
  DATA: lt_pos   TYPE STANDARD TABLE OF lty_pos,
        ls_pos   TYPE lty_pos,
        ls_kalk  TYPE ty_mata_kalk,
        lv_losgr TYPE keko-losgr,
        lv_werks TYPE keko-werks,
        lv_beskz TYPE marc-beskz,
        lv_sub   TYPE p LENGTH 16 DECIMALS 6,
        lv_sum   TYPE p LENGTH 16 DECIMALS 6.

  READ TABLE gt_mata_kalk INTO ls_kalk WITH TABLE KEY kalnr = iv_kalnr kalka = iv_kalka
                                                     kadky = iv_kadky tvers = iv_tvers
                                                     bwvar = iv_bwvar.
  IF sy-subrc = 0.
    cv_wert = ls_kalk-wert.
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
  IF gv_mata_tiefe <= 30.
    SELECT SINGLE losgr werks FROM keko INTO (lv_losgr, lv_werks)
      WHERE bzobj = '0'
        AND kalnr = iv_kalnr
        AND kalka = iv_kalka
        AND kadky = iv_kadky
        AND tvers = iv_tvers
        AND bwvar = iv_bwvar.
    IF sy-subrc = 0 AND lv_losgr > 0.
      SELECT typps matnr werks menge wrtfw_kpf ukaln ukalka ukadky utvers ubwvar
        FROM ckis INTO TABLE lt_pos
        WHERE lednr = '00'
          AND bzobj = '0'
          AND kalnr = iv_kalnr
          AND kalka = iv_kalka
          AND kadky = iv_kadky
          AND tvers = iv_tvers
          AND bwvar = iv_bwvar.

      LOOP AT lt_pos INTO ls_pos WHERE wrtfw_kpf <> 0.
        CASE ls_pos-typps.
          WHEN 'M' OR 'I'.
            CLEAR lv_beskz.
            SELECT SINGLE beskz FROM marc INTO lv_beskz
              WHERE matnr = ls_pos-matnr AND werks = ls_pos-werks.
            IF ls_pos-ukaln IS INITIAL OR lv_beskz = 'F' OR ls_pos-werks <> lv_werks.
              lv_sum = lv_sum + ls_pos-wrtfw_kpf.
            ELSE.
              PERFORM mata_kalkulation USING ls_pos-ukaln ls_pos-ukalka ls_pos-ukadky
                                             ls_pos-utvers ls_pos-ubwvar
                                       CHANGING lv_sub.
              lv_sum = lv_sum + lv_sub * ls_pos-menge.
            ENDIF.
          WHEN 'F'.
            IF p_mfrem = abap_true.
              lv_sum = lv_sum + ls_pos-wrtfw_kpf.
            ENDIF.
          WHEN OTHERS.
            " Eigenleistung (E), Zuschlaege und uebrige Positionen sind kein Material
        ENDCASE.
      ENDLOOP.
      ls_kalk-wert = lv_sum / lv_losgr.
    ENDIF.
  ENDIF.
  gv_mata_tiefe = gv_mata_tiefe - 1.

  MODIFY TABLE gt_mata_kalk FROM ls_kalk.
  cv_wert = ls_kalk-wert.
ENDFORM.                    "mata_kalkulation
