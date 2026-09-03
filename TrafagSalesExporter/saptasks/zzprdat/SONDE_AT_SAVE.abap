METHOD if_ex_workorder_update~at_save.
* Diagnosesonde vom 2026-09-03. Nur T76/100, Paket $TMP. Keine Zielfassung.
*
* Vorherige Diagnosefassungen markierten nur das DATUM. Das war zu schwach:
* Ist IS_HEADER_DIALOG-AUFNR leer, macht ALPHA daraus zwoelf Nullen. Die
* Nichtinitial-Pruefung im Baustein greift, das UPDATE laeuft und trifft null
* Saetze. Von aussen nicht unterscheidbar von "Methode lief nie".
*
* Diese Sonde schreibt deshalb in einen FESTEN Testauftrag, unabhaengig von
* jedem Eingabewert. Traegt 1241805 danach den 02.01.1900, ist AT_SAVE
* nachweislich gelaufen.
  CALL FUNCTION 'Z_PP_PRDDAT_SET' IN UPDATE TASK
    EXPORTING
      iv_aufnr  = '000001241805'
      iv_prddat = '19000102'.
ENDMETHOD.
