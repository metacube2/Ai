METHOD if_ex_workorder_update~at_release.
* Diagnosesonde vom 2026-09-03. Nur T76/100, Paket $TMP. Keine Zielfassung.
* Gegenstueck zur Sonde in AT_SAVE, aber mit eigenem Zielauftrag und eigener
* Marke, damit ein einziger Testlauf beide Methoden gleichzeitig beantwortet.
* Traegt 1241802 danach den 01.01.1900, ist AT_RELEASE gelaufen.
  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = '000001241802'
      iv_prddat = '19000101'.
ENDMETHOD.
