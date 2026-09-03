FUNCTION z_pp_prddat_set.
*"----------------------------------------------------------------------
*" Lokale Schnittstelle:
*"   IMPORTING
*"     VALUE(iv_aufnr)  TYPE aufnr
*"     VALUE(iv_prddat) TYPE zco_gltrp
*"----------------------------------------------------------------------
* Nur als Update-Funktionsbaustein verwenden. Kein COMMIT WORK hier.
* Die WHERE-Bedingung erzwingt die fachliche Write-once-Regel auch dann,
* wenn der Baustein mehrfach fuer denselben Auftrag registriert wird.

  CHECK iv_aufnr IS NOT INITIAL.
  CHECK iv_prddat IS NOT INITIAL.

  UPDATE aufk
    SET zzprdat = iv_prddat
    WHERE aufnr   = iv_aufnr
      AND zzprdat = '00000000'.

ENDFUNCTION.
