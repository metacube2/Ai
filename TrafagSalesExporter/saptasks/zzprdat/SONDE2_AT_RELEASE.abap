METHOD if_ex_workorder_update~at_release.
* Diagnosesonde 2 vom 2026-09-03. Nur T76/100, Paket $TMP. Keine Zielfassung.
*
* Sonde 1 hat bewiesen, dass die Methode laeuft: der Verbuchungsauftrag wurde
* registriert und ausgefuehrt. Er ist dann an CONNE_IMPORT_WRONG_FIELD_TYPE
* gescheitert, weil ein Zeichenliteral '19000101' uebergeben wurde, der
* Bausteinparameter aber ZCO_GLTRP (Datumstyp) ist. Beim Registrieren
* serialisiert SAP die Parameter, beim Ausfuehren liest es sie zurueck; dort
* muss der Typ exakt passen. Deshalb hier ausschliesslich getypte Variablen.
*
* Diese Sonde misst, was in IS_HEADER_DIALOG tatsaechlich steht. Sie kodiert das
* Ergebnis in das Datum, das sie in den festen Auftrag 1241802 schreibt:
*
*   01.01.1900  AUFNR leer,   GLTRP leer
*   02.01.1900  AUFNR gefuellt, GLTRP leer
*   03.01.1900  AUFNR leer,   GLTRP gefuellt
*   04.01.1900  beide gefuellt
*
* Der vierte Fall waere die Ueberraschung: dann stimmen die Daten, und die
* Ursache liegt woanders.
  DATA: lv_ziel   TYPE aufnr,
        lv_marke  TYPE zco_gltrp,
        lv_aufnr  TYPE aufnr,
        lv_gltrp  TYPE zco_gltrp.

  lv_aufnr = is_header_dialog-aufnr.
  lv_gltrp = is_header_dialog-gltrp.

  IF lv_aufnr IS INITIAL AND lv_gltrp IS INITIAL.
    lv_marke = '19000101'.
  ELSEIF lv_aufnr IS NOT INITIAL AND lv_gltrp IS INITIAL.
    lv_marke = '19000102'.
  ELSEIF lv_aufnr IS INITIAL AND lv_gltrp IS NOT INITIAL.
    lv_marke = '19000103'.
  ELSE.
    lv_marke = '19000104'.
  ENDIF.

  lv_ziel = '000001241802'.

  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = lv_ziel
      iv_prddat = lv_marke.
ENDMETHOD.
