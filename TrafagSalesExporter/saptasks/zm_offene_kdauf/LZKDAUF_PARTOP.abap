FUNCTION-POOL ZKDAUF_PAR.                   "MESSAGE-ID ..

* INCLUDE LZKDAUF_PARD...                    " Local class definition

*----------------------------------------------------------------------*
* BP_KURZ: Basispreisliste wie Z_BASISPREISLISTE (I_MATNR, I_MIT_LOE = 'X',
* ohne Laenge, Sprache = SY-LANGU), Ablauf von FILL_BPTAB und READ_TABLE
* aus LZZLO1F01 Schritt fuer Schritt uebernommen. Einziger Unterschied:
* sobald drei Zeilen vorliegen, hoert sie auf. Der Report braucht nur
* Zeile 2 oder 3; die Zeilen kommen in derselben Reihenfolge zustande.
* Arbeitsbereiche sind lokal: das Original nutzt Werte aus frueheren
* Aufrufen an keiner Stelle, die hier durchlaufen wird (je Zugriff geleert
* oder nur nach erfolgreichem Lesen verwendet).
*----------------------------------------------------------------------*
FORM bp_kurz USING iv_matnr TYPE matnr
             CHANGING ct_bptab TYPE STANDARD TABLE.
  TYPES: BEGIN OF lty_aufruf,
           vw     TYPE ztyp_kopf-vw001,
           xkey   TYPE c LENGTH 8,
           cnt    TYPE i,
           faktor TYPE ztyp_kopf-fakt1,
         END OF lty_aufruf.
  DATA: ls_mara    TYPE mara,
        lv_typcd   TYPE mara-zztyp,
        lv_off     TYPE i,
        lv_zub     TYPE c LENGTH 2,
        lt_zub     TYPE STANDARD TABLE OF char2,
        ls_kopf    TYPE ztyp_kopf,
        ls_kopf_t  TYPE ztyp_kopf_t,
        ls_pos     TYPE ztyp_pos,
        ls_pos_t   TYPE ztyp_pos_t,
        ls_zubt    TYPE ztyp_zub,
        lt_aufruf  TYPE STANDARD TABLE OF lty_aufruf,
        ls_aufruf  TYPE lty_aufruf,
        ls_zeile   TYPE zzbptab,
        lv_zwxkey  TYPE ztyp_pos-code,
        lv_xcode   TYPE ztyp_zub-code,
        lv_gefunden TYPE abap_bool,
        lv_spras   TYPE sy-langu.
  DATA: BEGIN OF ls_key,
          zwtyp(4),
          schalter(2),
          bereich(2),
          fuehler(4),
          kdwu(1),
          befest(2),
          sror(8),
        END OF ls_key.

  CLEAR ct_bptab.
  " Z_BASISPREISLISTE: ohne Material RAISE no_object, ohne Typcode RAISE no_typcd
  CHECK iv_matnr IS NOT INITIAL.
  SELECT SINGLE * FROM mara INTO ls_mara WHERE matnr = iv_matnr. "#EC CI_ALL_FIELDS_NEEDED
  IF sy-subrc <> 0 OR ls_mara-zztyp IS INITIAL.
    RETURN.
  ENDIF.
  lv_typcd = ls_mara-zztyp.
  lv_spras = sy-langu.

  " FILL_BPTAB (Laenge initial, daher kein Laengenzuschlag)
  IF lv_typcd+3(1) = ' '.              "3-stellige Typen
    ls_key-zwtyp = lv_typcd(3).
    ls_key-schalter = lv_typcd+4(2).
    ls_key-bereich = lv_typcd+6(2).
    ls_key-fuehler = lv_typcd+9(3).
    ls_key-kdwu = lv_typcd+11(3).
    ls_key-befest = lv_typcd+13(2).
    ls_key-sror = lv_typcd+16(4).
    ls_key-sror+4(4) = lv_typcd+21(4).
  ELSE.
    ls_key-zwtyp = lv_typcd(4).               "4-stellige Typen
    ls_key-bereich = lv_typcd+5(2).
    ls_key-fuehler = lv_typcd+8(4).
    ls_key-befest = lv_typcd+13(2).
  ENDIF.
  " DO 10 TIMES VARYING zbtab-zub FROM typcd+26(2) NEXT typcd+29(2)
  DO 10 TIMES.
    lv_off = 26 + ( sy-index - 1 ) * 3.
    lv_zub = lv_typcd+lv_off(2).
    IF NOT lv_zub IS INITIAL.
      APPEND lv_zub TO lt_zub.
    ENDIF.
  ENDDO.

  SELECT SINGLE * FROM ztyp_kopf INTO ls_kopf WHERE typ = ls_key-zwtyp.
  " mit_loe = 'X': geloeschte Koepfe bleiben
  SELECT SINGLE * FROM ztyp_kopf_t INTO ls_kopf_t WHERE typ = ls_key-zwtyp
                                                  AND   spras = lv_spras.
  CLEAR ls_zeile.
  ls_zeile-code = ls_key-zwtyp.
  ls_zeile-text = ls_kopf_t-text.
  ls_zeile-preis = ls_kopf-preis.
  ls_zeile-waers = ls_kopf-waers.
  APPEND ls_zeile TO ct_bptab.

  " die READ_TABLE-Aufrufe in derselben Reihenfolge
  APPEND VALUE #( vw = ls_kopf-vw001 xkey = ls_key-schalter cnt = 0 faktor = ls_kopf-fakt1 ) TO lt_aufruf.
  APPEND VALUE #( vw = ls_kopf-vw002 xkey = ls_key-bereich  cnt = 0 faktor = ls_kopf-fakt2 ) TO lt_aufruf.
  APPEND VALUE #( vw = ls_kopf-vw003 xkey = ls_key-fuehler  cnt = 0 faktor = ls_kopf-fakt3 ) TO lt_aufruf.
  APPEND VALUE #( vw = ls_kopf-vw004 xkey = ls_key-kdwu     cnt = 0 faktor = ls_kopf-fakt3 ) TO lt_aufruf.
  APPEND VALUE #( vw = ls_kopf-vw005 xkey = ls_key-befest   cnt = 0 faktor = 1 ) TO lt_aufruf.
  APPEND VALUE #( vw = ls_kopf-vw006 xkey = ls_key-sror     cnt = 0 faktor = 1 ) TO lt_aufruf.
  LOOP AT lt_zub INTO lv_zub.
    APPEND VALUE #( vw = ls_kopf-vw007 xkey = lv_zub cnt = sy-tabix faktor = 1 ) TO lt_aufruf.
  ENDLOOP.

  LOOP AT lt_aufruf INTO ls_aufruf.
    IF lines( ct_bptab ) >= 3.
      EXIT.                            " Kurzfassung: mehr braucht BEREICH nicht
    ENDIF.
    " READ_TABLE
    CHECK NOT ls_aufruf-vw IS INITIAL.
    CHECK NOT ls_aufruf-xkey IS INITIAL.
    CLEAR ls_zeile.

    IF ls_aufruf-cnt GE 4 AND
       NOT ls_mara-matnr IS INITIAL AND
       ls_aufruf-xkey = '01'.
      UNPACK ls_aufruf-cnt TO lv_xcode.
      SELECT SINGLE * FROM ztyp_zub INTO ls_zubt WHERE matnr = ls_mara-matnr
                                                 AND   code = lv_xcode.
      IF sy-subrc = 0.
        ls_zeile-code = ls_aufruf-xkey.
        ls_zeile-text = ls_zubt-text1.
        ls_zeile-preis = ls_zubt-preis.
        ls_zeile-waers = ls_zubt-waers.
        APPEND ls_zeile TO ct_bptab.
        IF NOT ls_zubt-text2 IS INITIAL.
          CLEAR ls_zeile.
          ls_zeile-text = ls_zubt-text2.
          APPEND ls_zeile TO ct_bptab.
        ENDIF.
      ELSE.
        ls_zeile-code = ls_aufruf-xkey.
        ls_zeile-text = '*** NOT FOUND ***'.
        APPEND ls_zeile TO ct_bptab.
      ENDIF.
    ELSE.
      lv_zwxkey = ls_aufruf-xkey.
      IF lv_zwxkey+4(4) NE space.         "Bei Schutzrohr: BISWERT !!
        lv_zwxkey+4(4) = '%   '.
        CLEAR ls_pos.
        SELECT * FROM ztyp_pos INTO ls_pos WHERE verw = ls_aufruf-vw
                                           AND   code LIKE lv_zwxkey.
          IF ls_pos-code+4(4) GE ls_aufruf-xkey+4(4).
            lv_zwxkey = ls_pos-code.
            EXIT.                                         "#EC CI_NOORDER
          ENDIF.
        ENDSELECT.
      ENDIF.

      SELECT SINGLE * FROM ztyp_pos INTO ls_pos WHERE verw = ls_aufruf-vw
                                              AND   code = lv_zwxkey.
      lv_gefunden = boolc( sy-subrc = 0 ).         " mit_loe = 'X'

      IF lv_gefunden = abap_true.
        CLEAR ls_pos_t.
        SELECT SINGLE * FROM ztyp_pos_t INTO ls_pos_t WHERE verw = ls_aufruf-vw "#EC CI_ALL_FIELDS_NEEDED
                                                      AND   code = ls_pos-code
                                                      AND   spras = lv_spras.
        ls_zeile-code = ls_aufruf-xkey.
        ls_zeile-text = ls_pos_t-text.
        ls_zeile-preis = ls_pos-preis * ls_aufruf-faktor.
        ls_zeile-waers = ls_pos-waers.
        APPEND ls_zeile TO ct_bptab.
      ELSE.
        IF NOT ls_aufruf-xkey CO '0 ' AND
           ls_aufruf-xkey NE ls_key-kdwu.
          ls_zeile-code = ls_aufruf-xkey.
          ls_zeile-text = '*** NOT FOUND ***'.
          APPEND ls_zeile TO ct_bptab.
        ENDIF.
      ENDIF.
    ENDIF.
  ENDLOOP.
ENDFORM.

*----------------------------------------------------------------------*
* BP_VERGLEICH: erste drei Zeilen (Code, Text) gleich? Sonst Zaehler + 1
*----------------------------------------------------------------------*
FORM bp_vergleich USING it_orig TYPE STANDARD TABLE
                        it_kurz TYPE STANDARD TABLE
                  CHANGING cv_diff TYPE i.
  DATA: ls_o TYPE zzbptab,
        ls_k TYPE zzbptab,
        lv_i TYPE i.
  DO 3 TIMES.
    lv_i = sy-index.
    CLEAR: ls_o, ls_k.
    READ TABLE it_orig INTO ls_o INDEX lv_i.
    READ TABLE it_kurz INTO ls_k INDEX lv_i.
    IF ls_o-code <> ls_k-code OR ls_o-text <> ls_k-text.
      cv_diff = cv_diff + 1.
      EXIT.
    ENDIF.
  ENDDO.
ENDFORM.
