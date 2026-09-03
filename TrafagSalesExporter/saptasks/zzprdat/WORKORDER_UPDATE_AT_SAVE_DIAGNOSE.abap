METHOD if_ex_workorder_update~at_save.
* Diagnosefassung vom 2026-09-03. Nur T76/100, Paket $TMP. Nicht die Zielfassung.
*
* AT_RELEASE wird im Einzeldialog von CO01 und CO02 nachweislich nicht gerufen.
* AT_SAVE hat exakt dieselbe Signatur (IS_HEADER_DIALOG TYPE COBAI_S_HEADER_DIALOG),
* der bereits syntaxgepruefte Code laesst sich also unveraendert uebernehmen. Damit
* laesst sich ohne neues Risiko messen, ob diese Methode ueberhaupt laeuft.
*
* Bewusste Einschraenkung dieser Fassung: AT_SAVE laeuft bei JEDEM Sichern, nicht nur
* bei der Freigabe. Fachlich ist das falsch, denn das Produktionsdatum soll erst mit
* der erstmaligen Freigabe entstehen. Fuer den Diagnoseschritt ist es richtig, weil es
* die Frage "laeuft die Methode?" von der Frage "erkennt sie die Freigabe?" trennt.
* Die Write-once-Regel im Verbuchungsbaustein verhindert, dass ein spaeter gesetztes
* Datum ueberschrieben wird.
*
* Marke 02.01.1900 unterscheidet einen Treffer hier vom Treffer in AT_RELEASE,
* der mit 01.01.1900 markiert ist.
*
* Auswertung am naechsten Testauftrag:
*   ZZPRDAT = GLTRP        -> AT_SAVE laeuft, der Mechanismus stimmt.
*                             Danach die Freigabebedingung ergaenzen.
*   ZZPRDAT = 02.01.1900   -> AT_SAVE laeuft, aber GLTRP ist hier leer.
*   ZZPRDAT bleibt leer    -> auch AT_SAVE laeuft nicht. Dann bleibt nur
*                             BEFORE_UPDATE mit IT_HEADER und IT_STATUS.
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
    lv_prddat = '19000102'.
  ENDIF.

  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = lv_aufnr
      iv_prddat = lv_prddat.
ENDMETHOD.
