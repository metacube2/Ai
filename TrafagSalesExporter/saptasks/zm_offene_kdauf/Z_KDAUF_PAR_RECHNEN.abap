FUNCTION z_kdauf_par_rechnen.
*"----------------------------------------------------------------------
*"*"Lokale Schnittstelle:
*"  IMPORTING
*"     VALUE(IV_IN) TYPE  XSTRING
*"  EXPORTING
*"     VALUE(EV_OUT) TYPE  XSTRING
*"----------------------------------------------------------------------
* Paketarbeit fuer ZM_OFFENE_KDAUF_OPT (2026-10-12, Claude), aufgerufen mit
* STARTING NEW TASK; gerechnet wird weiter im Report.
*   POS   -> RV_ORDER_FLOW_INFORMATION je Position (wie FILL_FKDAT)
*   MATNR -> Basispreisliste je Material (wie BEREICH)
* BPMOD (Basispreisliste):
*   K = Kurzfassung (unten): rechnet wie Z_BASISPREISLISTE / FILL_BPTAB /
*       READ_TABLE (SAPLZZLO1), hoert aber nach der dritten Zeile auf. Der
*       Report liest nur Zeile 2 oder 3 (BEREICH).
*   P = Pruefen: beide Fassungen, Rueckgabe des Originals, BP_DIFF zaehlt die
*       Materialien, deren erste drei Zeilen (Code, Text) abweichen.
*   sonst Original Z_BASISPREISLISTE.
* Ein- und Ausgabe als Datencluster in XSTRING, damit die Schnittstelle
* RFC-faehig bleibt und keine eigenen DDIC-Typen braucht.
  TYPES: BEGIN OF lty_pos,
           vbeln TYPE vbeln,
           posnr TYPE posnr,
         END OF lty_pos,
         lty_vbfa_tab  TYPE STANDARD TABLE OF vbfa WITH DEFAULT KEY,
         lty_bptab_tab TYPE STANDARD TABLE OF zzbptab WITH DEFAULT KEY,
         BEGIN OF lty_fluss,
           vbeln TYPE vbeln,
           posnr TYPE posnr,
           vbfa  TYPE lty_vbfa_tab,
         END OF lty_fluss,
         BEGIN OF lty_bp,
           matnr TYPE matnr,
           bptab TYPE lty_bptab_tab,
         END OF lty_bp,
         BEGIN OF lty_aufruf,
           vw     TYPE ztyp_kopf-vw001,
           xkey   TYPE c LENGTH 8,
           cnt    TYPE i,
           faktor TYPE ztyp_kopf-fakt1,
         END OF lty_aufruf.
  DATA: lt_pos     TYPE STANDARD TABLE OF lty_pos,
        lt_matnr   TYPE STANDARD TABLE OF matnr,
        lv_bpmod   TYPE c LENGTH 1,
        lt_fluss   TYPE STANDARD TABLE OF lty_fluss,
        ls_fluss   TYPE lty_fluss,
        lt_bp      TYPE STANDARD TABLE OF lty_bp,
        ls_bp      TYPE lty_bp,
        lt_vbfa    TYPE TABLE OF vbfa,
        lt_bptab   TYPE STANDARD TABLE OF zzbptab,
        lt_kurz    TYPE STANDARD TABLE OF zzbptab,
        lv_bp_diff TYPE i,
        ls_vbco6   TYPE vbco6.

  IMPORT pos = lt_pos matnr = lt_matnr bpmod = lv_bpmod FROM DATA BUFFER iv_in.

  LOOP AT lt_pos INTO DATA(ls_pos).
    CLEAR lt_vbfa.
    ls_vbco6 = VALUE vbco6( vbeln = ls_pos-vbeln  posnr = ls_pos-posnr ).
    CALL FUNCTION 'RV_ORDER_FLOW_INFORMATION'        "#EC CI_USAGE_OK[2198647]
      EXPORTING
        comwa         = ls_vbco6
      TABLES
        vbfa_tab      = lt_vbfa
      EXCEPTIONS
        no_vbfa       = 1
        no_vbuk_found = 2
        OTHERS        = 3.
    ls_fluss-vbeln = ls_pos-vbeln.
    ls_fluss-posnr = ls_pos-posnr.
    ls_fluss-vbfa  = lt_vbfa.
    APPEND ls_fluss TO lt_fluss.
  ENDLOOP.

  LOOP AT lt_matnr INTO DATA(lv_matnr).
    CLEAR: lt_bptab, lt_kurz.
    IF lv_bpmod = 'K' OR lv_bpmod = 'P'.
      PERFORM bp_kurz USING lv_matnr CHANGING lt_kurz.
    ENDIF.
    IF lv_bpmod <> 'K'.
      CALL FUNCTION 'Z_BASISPREISLISTE'
        EXPORTING
          i_matnr   = lv_matnr
          i_mit_loe = 'X'
        TABLES
          bptab     = lt_bptab
        EXCEPTIONS
          no_object = 1
          no_typcd  = 2
          OTHERS    = 3.
    ELSE.
      lt_bptab = lt_kurz.
    ENDIF.
    IF lv_bpmod = 'P'.
      PERFORM bp_vergleich USING lt_bptab lt_kurz CHANGING lv_bp_diff.
    ENDIF.
    ls_bp-matnr = lv_matnr.
    ls_bp-bptab = lt_bptab.
    APPEND ls_bp TO lt_bp.
  ENDLOOP.

  EXPORT fluss = lt_fluss bp = lt_bp bp_diff = lv_bp_diff TO DATA BUFFER ev_out.
ENDFUNCTION.
