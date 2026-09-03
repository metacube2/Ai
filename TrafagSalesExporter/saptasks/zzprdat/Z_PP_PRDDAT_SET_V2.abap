FUNCTION z_pp_prddat_set.
*"----------------------------------------------------------------------
*" Lokale Schnittstelle:
*"   IMPORTING
*"     VALUE(iv_aufnr)  TYPE aufnr
*"     VALUE(iv_prddat) TYPE zco_gltrp
*"----------------------------------------------------------------------
* Verbuchungsbaustein, Verarbeitungsart "Start verzoegert" (V2).
*
* V2 ist zwingend. Als V1 schreibt der Baustein zwar korrekt, die
* SAP-Standardverbuchung schreibt die AUFK-Zeile danach aber vollstaendig aus
* ihrem eigenen Puffer und deckt den Wert wieder zu. V2 laeuft erst, wenn alle
* V1-Bausteine durch sind.
*
* Hier stehen beide fachlichen Regeln zentral, damit sie unabhaengig davon
* greifen, aus welcher BAdI-Methode registriert wurde:
*
* 1. Nur bei freigegebenem Auftrag. Gemessen wird an AFKO-FTRMI, dem
*    Ist-Freigabedatum. Der Status I0002 allein taugt nicht: bei
*    abgeschlossenen Auftraegen ist er inaktiv gesetzt, gemessen am 2026-09-03
*    an Auftrag 1194970. Weil V2 nach der Standardverbuchung laeuft, ist FTRMI
*    zu diesem Zeitpunkt bereits festgeschrieben.
* 2. Write-once ueber die WHERE-Bedingung. Ein bereits gesetztes
*    Produktionsdatum wird nie ueberschrieben, auch nicht bei mehrfacher
*    Registrierung oder spaeterer Terminverschiebung.

  DATA lv_ftrmi TYPE afko-ftrmi.

  CHECK iv_aufnr IS NOT INITIAL.
  CHECK iv_prddat IS NOT INITIAL.

  SELECT SINGLE ftrmi FROM afko INTO lv_ftrmi
    WHERE aufnr = iv_aufnr.
  CHECK sy-subrc = 0.
  CHECK lv_ftrmi IS NOT INITIAL.

  UPDATE aufk
    SET zzprdat = iv_prddat
    WHERE aufnr   = iv_aufnr
      AND zzprdat = '00000000'.

ENDFUNCTION.
