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
* FLMOD (Belegfluss):
*   K = eigene Lesung FLUSS_KURZ statt RV_ORDER_FLOW_INFORMATION
*   P = Pruefen: beide, Rueckgabe des Originals; FL_MENGE und FL_FOLGE zaehlen
*       Positionen mit anderen Fakturazeilen bzw. anderer Reihenfolge
*   H = Mischform: eigene Lesung; haben zwei Fakturazeilen dasselbe ERDAT,
*       fuer diese Position doch RV_ORDER_FLOW_INFORMATION (die Reihenfolge
*       gleich datierter Zeilen haengt am ganzen Baum des Originals).
*       FL_FM zaehlt diese Rueckfaelle.
*   sonst Original RV_ORDER_FLOW_INFORMATION.
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
        lv_flmod   TYPE c LENGTH 1,
        lt_fl_kurz TYPE TABLE OF vbfa,
        lv_fl_menge TYPE i,
        lv_fl_folge TYPE i,
        lv_fl_vor   TYPE i,
        lv_fl_fm    TYPE i,
        lv_gleich   TYPE abap_bool,
        lt_beisp    TYPE string_table,
        lv_beisp_n  TYPE i,
        ls_vbco6   TYPE vbco6.

  IMPORT pos = lt_pos matnr = lt_matnr bpmod = lv_bpmod flmod = lv_flmod FROM DATA BUFFER iv_in.

  LOOP AT lt_pos INTO DATA(ls_pos).
    CLEAR: lt_vbfa, lt_fl_kurz.
    IF lv_flmod = 'K' OR lv_flmod = 'P' OR lv_flmod = 'H'.
      PERFORM fluss_kurz USING ls_pos-vbeln ls_pos-posnr CHANGING lt_fl_kurz.
    ENDIF.
    lv_gleich = abap_false.
    IF lv_flmod = 'H'.
      PERFORM fluss_gleiches_datum USING lt_fl_kurz CHANGING lv_gleich.
      IF lv_gleich = abap_true.
        lv_fl_fm = lv_fl_fm + 1.
      ENDIF.
    ENDIF.
    IF ( lv_flmod <> 'K' AND lv_flmod <> 'H' ) OR lv_gleich = abap_true.
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
    ELSE.
      lt_vbfa = lt_fl_kurz.
    ENDIF.
    IF lv_flmod = 'P'.
      lv_fl_vor = lv_fl_menge + lv_fl_folge.
      PERFORM fluss_vergleich USING lt_vbfa lt_fl_kurz CHANGING lv_fl_menge lv_fl_folge.
      IF lv_fl_menge + lv_fl_folge > lv_fl_vor AND lv_beisp_n < 3.
        lv_beisp_n = lv_beisp_n + 1.
        PERFORM fluss_beispiel USING ls_pos-vbeln ls_pos-posnr lt_vbfa lt_fl_kurz CHANGING lt_beisp.
      ENDIF.
    ENDIF.
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

  EXPORT fluss = lt_fluss bp = lt_bp bp_diff = lv_bp_diff
         fl_menge = lv_fl_menge fl_folge = lv_fl_folge fl_beisp = lt_beisp fl_fm = lv_fl_fm TO DATA BUFFER ev_out.
ENDFUNCTION.
