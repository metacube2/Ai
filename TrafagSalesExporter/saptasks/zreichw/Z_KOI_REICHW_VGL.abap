*&---------------------------------------------------------------------*
*& Report Z_KOI_REICHW_VGL
*&---------------------------------------------------------------------*
*& Vergleich Z_KOI_REICHW_TURBO (ZREICHW) gegen die optimierte Kopie
*& Z_KOI_REICHW_OPT: gleiche Selektion, Ergebnistabelle des ALV ueber
*& cl_salv_bs_runtime_info abgreifen, jede Zeile Feld fuer Feld
*& vergleichen, Laufzeit messen. Lokales Testobjekt ($TMP), 2026-11-02.
*&---------------------------------------------------------------------*
REPORT z_koi_reichw_vgl.

TABLES: mara.

SELECT-OPTIONS: s_matnr FOR mara-matnr.
PARAMETERS: p_werks  TYPE werks_d DEFAULT '1100' OBLIGATORY,
            p_dlow   TYPE datum,
            p_dhigh  TYPE datum,
            p_perinc AS CHECKBOX,
            p_rwmit  AS CHECKBOX,
            p_anwrec AS CHECKBOX,
            p_reihe  AS CHECKBOX,   " X = zuerst OPT, dann alt
            p_liste  AS CHECKBOX.   " X = Ausgabeart Write (Debug-Liste) vergleichen

DATA: gv_dauer TYPE p LENGTH 10 DECIMALS 3.

INITIALIZATION.
  p_dhigh = sy-datum.
  p_dlow = sy-datum.
  p_dlow+0(4) = p_dlow+0(4) - 1.

START-OF-SELECTION.
  IF p_liste = abap_true.
    PERFORM listenvergleich.
    RETURN.
  ENDIF.

  DATA: lr_alt   TYPE REF TO data,
        lr_neu   TYPE REF TO data,
        lv_d_alt TYPE p LENGTH 10 DECIMALS 3,
        lv_d_neu TYPE p LENGTH 10 DECIMALS 3.

  IF p_reihe = abap_true.
    PERFORM lauf USING 'Z_KOI_REICHW_OPT' CHANGING lr_neu lv_d_neu.
    PERFORM lauf USING 'Z_KOI_REICHW_TURBO' CHANGING lr_alt lv_d_alt.
  ELSE.
    PERFORM lauf USING 'Z_KOI_REICHW_TURBO' CHANGING lr_alt lv_d_alt.
    PERFORM lauf USING 'Z_KOI_REICHW_OPT' CHANGING lr_neu lv_d_neu.
  ENDIF.

  WRITE: / 'Werk', p_werks, 'Zeitraum', p_dlow, p_dhigh.
  WRITE: / 'Optionen inkl/mitVerschr/Rueckrechnung:', p_perinc, p_rwmit, p_anwrec.
  WRITE: / 'Laufzeit alt (s):', lv_d_alt.
  WRITE: / 'Laufzeit neu (s):', lv_d_neu.
  PERFORM vergleich USING lr_alt lr_neu.

*&---------------------------------------------------------------------*
FORM lauf USING    iv_prog  TYPE syrepid
          CHANGING cr_daten TYPE REF TO data
                   cv_dauer TYPE p.
  DATA: lv_t0  TYPE timestampl,
        lv_t1  TYPE timestampl,
        lr_ref TYPE REF TO data,
        lv_perdif TYPE abap_bool,
        lv_rwohne TYPE abap_bool,
        lv_anwakt TYPE abap_bool.
  FIELD-SYMBOLS: <lt_quelle> TYPE ANY TABLE,
                 <lt_kopie>  TYPE ANY TABLE.

  lv_perdif = boolc( p_perinc = abap_false ).
  lv_rwohne = boolc( p_rwmit = abap_false ).
  lv_anwakt = boolc( p_anwrec = abap_false ).

  cl_salv_bs_runtime_info=>clear_all( ).
  cl_salv_bs_runtime_info=>set( display  = abap_false
                                metadata = abap_false
                                data     = abap_true ).
  GET TIME STAMP FIELD lv_t0.
  SUBMIT (iv_prog)
    WITH s_matnr IN s_matnr
    WITH p_werks  = p_werks
    WITH p_dlow   = p_dlow
    WITH p_dhigh  = p_dhigh
    WITH p_alv    = abap_true
    WITH p_write  = abap_false
    WITH p_spool  = abap_false
    WITH p_perdif = lv_perdif
    WITH p_perinc = p_perinc
    WITH p_rwohne = lv_rwohne
    WITH p_rwmit  = p_rwmit
    WITH p_anwakt = lv_anwakt
    WITH p_anwrec = p_anwrec
    AND RETURN.
  GET TIME STAMP FIELD lv_t1.
  cv_dauer = cl_abap_tstmp=>subtract( tstmp1 = lv_t1 tstmp2 = lv_t0 ).

  TRY.
      cl_salv_bs_runtime_info=>get_data_ref( IMPORTING r_data = lr_ref ).
    CATCH cx_salv_bs_sc_runtime_info.
      WRITE: / 'Keine Daten von', iv_prog.
  ENDTRY.
  cl_salv_bs_runtime_info=>clear_all( ).

  " Kopie ziehen, bevor der naechste Lauf den Speicher wiederverwendet
  IF lr_ref IS BOUND.
    ASSIGN lr_ref->* TO <lt_quelle>.
    CREATE DATA cr_daten LIKE <lt_quelle>.
    ASSIGN cr_daten->* TO <lt_kopie>.
    <lt_kopie> = <lt_quelle>.
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
FORM vergleich USING ir_alt TYPE REF TO data
                     ir_neu TYPE REF TO data.
  DATA: lv_n_alt   TYPE i,
        lv_n_neu   TYPE i,
        lv_zeile   TYPE i,
        lv_feld    TYPE i,
        lv_felder  TYPE i,
        lv_diff    TYPE i,
        lv_zellen  TYPE i,
        lv_name    TYPE string,
        lo_struct  TYPE REF TO cl_abap_structdescr.
  FIELD-SYMBOLS: <lt_alt> TYPE STANDARD TABLE,
                 <lt_neu> TYPE STANDARD TABLE,
                 <ls_alt> TYPE any,
                 <ls_neu> TYPE any,
                 <lv_alt> TYPE any,
                 <lv_neu> TYPE any.

  IF ir_alt IS NOT BOUND OR ir_neu IS NOT BOUND.
    WRITE: / 'ABBRUCH: eine Ergebnistabelle fehlt'.
    RETURN.
  ENDIF.
  ASSIGN ir_alt->* TO <lt_alt>.
  ASSIGN ir_neu->* TO <lt_neu>.
  lv_n_alt = lines( <lt_alt> ).
  lv_n_neu = lines( <lt_neu> ).
  WRITE: / 'Zeilen alt:', lv_n_alt.
  WRITE: / 'Zeilen neu:', lv_n_neu.
  IF lv_n_alt <> lv_n_neu.
    WRITE: / 'UNGLEICH: Zeilenzahl'.
  ENDIF.

  LOOP AT <lt_alt> ASSIGNING <ls_alt>.
    lv_zeile = sy-tabix.
    READ TABLE <lt_neu> INDEX lv_zeile ASSIGNING <ls_neu>.
    IF sy-subrc <> 0.
      EXIT.
    ENDIF.
    IF lo_struct IS NOT BOUND.
      lo_struct ?= cl_abap_typedescr=>describe_by_data( <ls_alt> ).
      lv_felder = lines( lo_struct->components ).
    ENDIF.
    lv_feld = 0.
    DO.
      lv_feld = lv_feld + 1.
      ASSIGN COMPONENT lv_feld OF STRUCTURE <ls_alt> TO <lv_alt>.
      IF sy-subrc <> 0.
        EXIT.
      ENDIF.
      ASSIGN COMPONENT lv_feld OF STRUCTURE <ls_neu> TO <lv_neu>.
      lv_zellen = lv_zellen + 1.
      IF <lv_alt> <> <lv_neu>.
        lv_diff = lv_diff + 1.
        IF lv_diff <= 30.
          lv_name = lo_struct->components[ lv_feld ]-name.
          WRITE: / 'DIFF Zeile', lv_zeile, lv_name.
          WRITE: / '   alt:', <lv_alt>.
          WRITE: / '   neu:', <lv_neu>.
        ENDIF.
      ENDIF.
    ENDDO.
  ENDLOOP.

  WRITE: / 'Felder je Zeile:', lv_felder.
  WRITE: / 'Zellen verglichen:', lv_zellen.
  WRITE: / 'Abweichende Zellen:', lv_diff.
  IF lv_diff = 0 AND lv_n_alt = lv_n_neu.
    WRITE: / 'ERGEBNIS: IDENTISCH'.
  ELSE.
    WRITE: / 'ERGEBNIS: UNGLEICH'.
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
*& Ausgabeart Write: beide Listen ins Memory, als Text, sortiert vergleichen
*& (die Reihenfolge der Einzelbewegungen war schon bisher die der Datenbank)
*&---------------------------------------------------------------------*
FORM listenvergleich.
  TYPES ty_zeile TYPE c LENGTH 255.
  DATA: lt_alt   TYPE STANDARD TABLE OF ty_zeile WITH DEFAULT KEY,
        lt_neu   TYPE STANDARD TABLE OF ty_zeile WITH DEFAULT KEY,
        lv_n_alt TYPE i,
        lv_n_neu TYPE i,
        lv_diff  TYPE i,
        lv_i     TYPE i.

  PERFORM liste USING 'Z_KOI_REICHW_TURBO' CHANGING lt_alt.
  PERFORM liste USING 'Z_KOI_REICHW_OPT' CHANGING lt_neu.
  SORT lt_alt.
  SORT lt_neu.
  lv_n_alt = lines( lt_alt ).
  lv_n_neu = lines( lt_neu ).
  WRITE: / 'Listenzeilen alt:', lv_n_alt.
  WRITE: / 'Listenzeilen neu:', lv_n_neu.
  LOOP AT lt_alt ASSIGNING FIELD-SYMBOL(<lv_alt>).
    lv_i = sy-tabix.
    READ TABLE lt_neu INDEX lv_i ASSIGNING FIELD-SYMBOL(<lv_neu>).
    IF sy-subrc <> 0 OR <lv_alt> <> <lv_neu>.
      lv_diff = lv_diff + 1.
      IF lv_diff <= 20.
        WRITE: / 'DIFF', lv_i.
        WRITE: / '   alt:', <lv_alt>(120).
        IF <lv_neu> IS ASSIGNED.
          WRITE: / '   neu:', <lv_neu>(120).
        ENDIF.
      ENDIF.
    ENDIF.
  ENDLOOP.
  WRITE: / 'Abweichende Listenzeilen:', lv_diff.
  IF lv_diff = 0 AND lv_n_alt = lv_n_neu.
    WRITE: / 'ERGEBNIS LISTE: IDENTISCH'.
  ELSE.
    WRITE: / 'ERGEBNIS LISTE: UNGLEICH'.
  ENDIF.
ENDFORM.

FORM liste USING iv_prog TYPE syrepid CHANGING ct_text TYPE STANDARD TABLE.
  DATA: lt_abaplist TYPE STANDARD TABLE OF abaplist,
        lv_perdif   TYPE abap_bool,
        lv_rwohne   TYPE abap_bool,
        lv_anwakt   TYPE abap_bool.

  lv_perdif = boolc( p_perinc = abap_false ).
  lv_rwohne = boolc( p_rwmit = abap_false ).
  lv_anwakt = boolc( p_anwrec = abap_false ).

  SUBMIT (iv_prog)
    WITH s_matnr IN s_matnr
    WITH p_werks  = p_werks
    WITH p_dlow   = p_dlow
    WITH p_dhigh  = p_dhigh
    WITH p_alv    = abap_false
    WITH p_write  = abap_true
    WITH p_spool  = abap_false
    WITH p_perdif = lv_perdif
    WITH p_perinc = p_perinc
    WITH p_rwohne = lv_rwohne
    WITH p_rwmit  = p_rwmit
    WITH p_anwakt = lv_anwakt
    WITH p_anwrec = p_anwrec
    EXPORTING LIST TO MEMORY
    AND RETURN.

  CALL FUNCTION 'LIST_FROM_MEMORY'
    TABLES
      listobject = lt_abaplist
    EXCEPTIONS
      OTHERS     = 1.
  IF sy-subrc = 0.
    CALL FUNCTION 'LIST_TO_ASCI'
      TABLES
        listasci   = ct_text
        listobject = lt_abaplist
      EXCEPTIONS
        OTHERS     = 1.
  ENDIF.
  CALL FUNCTION 'LIST_FREE_MEMORY'.
ENDFORM.
