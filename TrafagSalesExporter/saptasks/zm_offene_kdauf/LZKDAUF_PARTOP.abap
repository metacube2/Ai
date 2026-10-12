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

*----------------------------------------------------------------------*
* FLUSS_KURZ: Fakturazeilen (VBTYP_N = 'M') einer Auftragsposition direkt
* aus VBFA, so wie RV_ORDER_FLOW_INFORMATION sie liefert (Pruefmodus
* 2026-10-12): abwaerts nur ueber direkte Verknuepfungen (STUFE = '00'),
* Knoten immer mit Belegtyp (dieselbe Nummer gibt es in verschiedenen
* Belegarten, z. B. Handling Unit und Kundenauftrag), je Beleg auch die
* Kopfverknuepfung (Position 000000). Hoechstens 50 Stufen.
*----------------------------------------------------------------------*
FORM fluss_kurz USING iv_vbeln TYPE vbeln
                      iv_posnr TYPE posnr
                CHANGING ct_vbfa TYPE STANDARD TABLE.
  TYPES: BEGIN OF lty_knoten,
           vbeln TYPE vbeln,
           posnr TYPE posnr,
           vbtyp TYPE vbfa-vbtyp_v,
         END OF lty_knoten.
  DATA: lt_front   TYPE STANDARD TABLE OF lty_knoten,
        lt_neu     TYPE STANDARD TABLE OF lty_knoten,
        lt_gesehen TYPE HASHED TABLE OF lty_knoten WITH UNIQUE KEY vbeln posnr vbtyp,
        lt_zeilen  TYPE STANDARD TABLE OF vbfa,
        lt_ruuid   TYPE HASHED TABLE OF vbfa-ruuid WITH UNIQUE KEY table_line,
        ls_k       TYPE lty_knoten,
        lv_vbtyp   TYPE vbak-vbtyp.

  CLEAR ct_vbfa.
  SELECT SINGLE vbtyp FROM vbak INTO lv_vbtyp WHERE vbeln = iv_vbeln.
  CHECK sy-subrc = 0.
  ls_k-vbeln = iv_vbeln.
  ls_k-posnr = iv_posnr.
  ls_k-vbtyp = lv_vbtyp.
  APPEND ls_k TO lt_front.
  INSERT ls_k INTO TABLE lt_gesehen.
  DO 50 TIMES.
    IF lt_front IS INITIAL.
      EXIT.
    ENDIF.
    CLEAR lt_zeilen.
    SELECT * FROM vbfa INTO TABLE lt_zeilen               "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_front
      WHERE vbelv   = lt_front-vbeln
      AND   posnv   = lt_front-posnr
      AND   vbtyp_v = lt_front-vbtyp
      AND   stufe   = '00'.
    CLEAR lt_neu.
    LOOP AT lt_zeilen INTO DATA(ls_z).
      IF ls_z-vbtyp_n = 'M'.
        READ TABLE lt_ruuid WITH TABLE KEY table_line = ls_z-ruuid TRANSPORTING NO FIELDS.
        IF sy-subrc <> 0.
          INSERT ls_z-ruuid INTO TABLE lt_ruuid.
          APPEND ls_z TO ct_vbfa.
        ENDIF.
      ENDIF.
      " Nachfolger als Position und als Kopf weiterverfolgen
      DO 2 TIMES.
        ls_k-vbeln = ls_z-vbeln.
        ls_k-vbtyp = ls_z-vbtyp_n.
        IF sy-index = 1.
          ls_k-posnr = ls_z-posnn.
        ELSE.
          ls_k-posnr = '000000'.
        ENDIF.
        READ TABLE lt_gesehen WITH TABLE KEY vbeln = ls_k-vbeln posnr = ls_k-posnr vbtyp = ls_k-vbtyp
             TRANSPORTING NO FIELDS.
        IF sy-subrc <> 0.
          INSERT ls_k INTO TABLE lt_gesehen.
          APPEND ls_k TO lt_neu.
        ENDIF.
      ENDDO.
    ENDLOOP.
    lt_front = lt_neu.
  ENDDO.
ENDFORM.

*----------------------------------------------------------------------*
* FLUSS_VERGLEICH: Folge (ERDAT, RFMNG) so, wie FILL_FKDAT sie verarbeitet
* (SORT BY erdat, danach nur VBTYP_N = 'M'), Original gegen Kurz.
*   cv_menge: andere Fakturazeilen;  cv_folge: gleiche, andere Reihenfolge
*----------------------------------------------------------------------*
FORM fluss_vergleich USING it_orig TYPE STANDARD TABLE
                           it_kurz TYPE STANDARD TABLE
                     CHANGING cv_menge TYPE i
                              cv_folge TYPE i.
  TYPES: BEGIN OF lty_m,
           erdat TYPE vbfa-erdat,
           rfmng TYPE vbfa-rfmng,
         END OF lty_m.
  DATA: lt_o  TYPE STANDARD TABLE OF vbfa,
        lt_k  TYPE STANDARD TABLE OF vbfa,
        lt_mo TYPE STANDARD TABLE OF lty_m,
        lt_mk TYPE STANDARD TABLE OF lty_m.
  lt_o = it_orig.
  lt_k = it_kurz.
  SORT lt_o BY erdat ASCENDING.
  DELETE lt_o WHERE vbtyp_n <> 'M'.
  SORT lt_k BY erdat ASCENDING.
  DELETE lt_k WHERE vbtyp_n <> 'M'.
  LOOP AT lt_o INTO DATA(ls_o).
    APPEND VALUE #( erdat = ls_o-erdat rfmng = ls_o-rfmng ) TO lt_mo.
  ENDLOOP.
  LOOP AT lt_k INTO DATA(ls_kz).
    APPEND VALUE #( erdat = ls_kz-erdat rfmng = ls_kz-rfmng ) TO lt_mk.
  ENDLOOP.
  IF lt_mo = lt_mk.
    RETURN.
  ENDIF.
  SORT lt_mo BY erdat rfmng.
  SORT lt_mk BY erdat rfmng.
  IF lt_mo = lt_mk.
    cv_folge = cv_folge + 1.
  ELSE.
    cv_menge = cv_menge + 1.
  ENDIF.
ENDFORM.

*----------------------------------------------------------------------*
* FLUSS_BEISPIEL: beide Zeilenlisten einer abweichenden Position als Text
* (nur Pruefmodus, hoechstens einige Positionen je Paket)
*----------------------------------------------------------------------*
FORM fluss_beispiel USING iv_vbeln TYPE vbeln
                          iv_posnr TYPE posnr
                          it_orig  TYPE STANDARD TABLE
                          it_kurz  TYPE STANDARD TABLE
                    CHANGING ct_text TYPE string_table.
  DATA: ls_v   TYPE vbfa,
        lv_txt TYPE string.
  APPEND |POSITION { iv_vbeln } { iv_posnr } ORIGINAL { lines( it_orig ) } KURZ { lines( it_kurz ) }| TO ct_text.
  LOOP AT it_orig INTO ls_v.
    lv_txt = |  O { ls_v-stufe } { ls_v-vbelv } { ls_v-posnv } { ls_v-vbtyp_v } -> { ls_v-vbeln } { ls_v-posnn } { ls_v-vbtyp_n } { ls_v-erdat } { ls_v-rfmng }|.
    APPEND lv_txt TO ct_text.
  ENDLOOP.
  LOOP AT it_kurz INTO ls_v.
    lv_txt = |  K { ls_v-vbelv } { ls_v-posnv } { ls_v-vbtyp_v } -> { ls_v-vbeln } { ls_v-posnn } { ls_v-vbtyp_n } { ls_v-erdat } { ls_v-rfmng }|.
    APPEND lv_txt TO ct_text.
  ENDLOOP.
ENDFORM.

*----------------------------------------------------------------------*
* FLUSS_GLEICHES_DATUM: haben zwei Fakturazeilen dasselbe ERDAT?
*----------------------------------------------------------------------*
FORM fluss_gleiches_datum USING it_vbfa TYPE STANDARD TABLE
                          CHANGING cv_gleich TYPE abap_bool.
  DATA: lt_d   TYPE STANDARD TABLE OF vbfa-erdat,
        ls_v   TYPE vbfa,
        lv_n   TYPE i.
  cv_gleich = abap_false.
  LOOP AT it_vbfa INTO ls_v WHERE ('VBTYP_N = ''M''').
    APPEND ls_v-erdat TO lt_d.
  ENDLOOP.
  lv_n = lines( lt_d ).
  SORT lt_d.
  DELETE ADJACENT DUPLICATES FROM lt_d.
  IF lines( lt_d ) < lv_n.
    cv_gleich = abap_true.
  ENDIF.
ENDFORM.
