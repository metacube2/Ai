METHOD if_ex_workorder_update~at_release.
* Diagnosefassung vom 2026-09-03. Nur fuer T76/100, Paket $TMP.
*
* Die produktive Fassung schrieb in drei Testauftraegen nichts. Diese Fassung
* entfernt jede Vorbedingung und macht sichtbar, woran es liegt. Sie ist
* bewusst NICHT die Zielfassung.
*
* Zwei Aenderungen gegenueber der ersten Fassung:
*
* 1. Die drei CHECK-Zeilen sind weg. Sie koennten die Methode still verlassen
*    haben, ohne dass man es von aussen sieht.
* 2. Die Auftragsnummer wird per ALPHA normalisiert. Traegt die Dialogstruktur
*    die externe Darstellung ohne fuehrende Nullen, findet das UPDATE sonst
*    keinen Satz, weil AUFK intern mit fuehrenden Nullen speichert.
*
* Ist GLTRP an dieser Stelle leer, wird ersatzweise der 01.01.1900 gesetzt.
* Dieses Datum ist als Diagnosemarke gewaehlt: es kann fachlich nicht vorkommen
* und beweist, dass die Methode gelaufen ist.
*
* Auswertung am Ergebnis des naechsten Testauftrags:
*   ZZPRDAT = GLTRP        -> es lag an einer Pruefung oder am Zahlenformat
*   ZZPRDAT = 01.01.1900   -> Methode laeuft, aber GLTRP ist hier leer
*   ZZPRDAT bleibt leer    -> AT_RELEASE wird nicht gerufen, oder die
*                             Auftragsnummer passt nicht zur AUFK-Zeile
  DATA: lv_aufnr  TYPE aufk-aufnr,
        lv_prddat TYPE aufk-zzprdat.

  lv_aufnr = is_header_dialog-aufnr.
  CALL FUNCTION 'CONVERSION_EXIT_ALPHA_INPUT'
    EXPORTING
      input  = lv_aufnr
    IMPORTING
      output = lv_aufnr.

  lv_prddat = is_header_dialog-gltrp.
  IF lv_prddat IS INITIAL.
    lv_prddat = '19000101'.
  ENDIF.

  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = lv_aufnr
      iv_prddat = lv_prddat.
ENDMETHOD.
