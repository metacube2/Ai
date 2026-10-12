REPORT z_kdauf_nr_test.
* Test 2026-10-12 (Claude), nur T76: Was liefert NUMBER_GET_NEXT mit QUANTITY > 1
* fuer ZPOWERBIID/01 (im Hauptspeicher gepuffert), und wie lange dauern
* Einzelaufrufe gegenueber einem Sammelaufruf? Verbraucht Nummern in T76.
PARAMETERS: p_menge TYPE i DEFAULT 25,
            p_einz  TYPE i DEFAULT 2000.

DATA: lv_nr     TYPE n LENGTH 10,
      lv_anz    TYPE inri-quantity,
      lv_rc     TYPE inri-returncode,
      lv_vor    TYPE n LENGTH 10,
      lv_nach   TYPE n LENGTH 10,
      lv_t0     TYPE i,
      lv_t1     TYPE i,
      lv_i      TYPE i.

START-OF-SELECTION.
  " Vorher: naechste Nummer einzeln holen
  CALL FUNCTION 'NUMBER_GET_NEXT'
    EXPORTING nr_range_nr = '01' object = 'ZPOWERBIID'
    IMPORTING number = lv_vor
    EXCEPTIONS OTHERS = 1.
  WRITE: / 'Einzeln vorher:', lv_vor, 'sy-subrc', sy-subrc.

  " Sammelaufruf
  lv_anz = p_menge.
  CALL FUNCTION 'NUMBER_GET_NEXT'
    EXPORTING nr_range_nr = '01' object = 'ZPOWERBIID' quantity = lv_anz
    IMPORTING number = lv_nr quantity = lv_anz returncode = lv_rc
    EXCEPTIONS OTHERS = 1.
  WRITE: / 'Sammel: angefordert', p_menge, 'erhalten', lv_anz, 'NUMBER', lv_nr, 'RC', lv_rc, 'sy-subrc', sy-subrc.

  " Nachher: naechste Nummer einzeln holen
  CALL FUNCTION 'NUMBER_GET_NEXT'
    EXPORTING nr_range_nr = '01' object = 'ZPOWERBIID'
    IMPORTING number = lv_nach
    EXCEPTIONS OTHERS = 1.
  WRITE: / 'Einzeln nachher:', lv_nach.
  WRITE: / 'Deutung: NUMBER = erste Nummer, wenn NUMBER = vorher + 1; letzte, wenn NUMBER = nachher - 1'.

  " Zeit: Einzelaufrufe
  GET RUN TIME FIELD lv_t0.
  DO p_einz TIMES.
    CALL FUNCTION 'NUMBER_GET_NEXT'
      EXPORTING nr_range_nr = '01' object = 'ZPOWERBIID'
      IMPORTING number = lv_nr
      EXCEPTIONS OTHERS = 1.
  ENDDO.
  GET RUN TIME FIELD lv_t1.
  lv_i = ( lv_t1 - lv_t0 ) / 1000.
  WRITE: / 'Einzelaufrufe:', p_einz, 'Dauer ms', lv_i.

  " Zeit: Sammelaufrufe fuer dieselbe Menge
  GET RUN TIME FIELD lv_t0.
  lv_i = 0.
  WHILE lv_i < p_einz.
    lv_anz = p_einz - lv_i.
    CALL FUNCTION 'NUMBER_GET_NEXT'
      EXPORTING nr_range_nr = '01' object = 'ZPOWERBIID' quantity = lv_anz
      IMPORTING number = lv_nr quantity = lv_anz
      EXCEPTIONS OTHERS = 1.
    IF sy-subrc <> 0 OR lv_anz = 0.
      EXIT.
    ENDIF.
    lv_i = lv_i + lv_anz.
  ENDWHILE.
  GET RUN TIME FIELD lv_t1.
  lv_t0 = ( lv_t1 - lv_t0 ) / 1000.
  WRITE: / 'Sammelaufrufe fuer', lv_i, 'Nummern, Dauer ms', lv_t0.
