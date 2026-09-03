*----------------------------------------------------------------------*
*   INCLUDE ZXCO1U11   code for user exit EXIT_SAPLCOKO1_001
*----------------------------------------------------------------------*

**          name area and used the user exit PPCO0012.
*
"SELECT zzprdat FROM caufv INTO zprdat
"  WHERE aufnr = i_caufvd-aufnr.

"  i_caufvd-zzprdat = zprdat.

"ENDSELECT.

"* ProdDatum zprdat for Screen SAPLXCO1 0100

"* gs_aufk-zzprdat = i_caufvd-zzprdat. "Produktionsdatum

"Move i_caufvd-zzprdat to ci_aufk-zzprdat.   "Produktionsdatum

"**Eckendtermin AUFK-GLTRP in Tabellenfeld AUFK-ZZPRDAT als Produktionsdatum einmalig sichern---------------------*
"IF i_caufvd-ftrmi = sy-datum.     "heute freigegeben
"* wenn der FAUF jetzt freigeben wurde und ZZPRDAT noch nicht gefüllt ist, dann schreiben;
"* erster Eckendetermin d.h. bei erster Freigabe wird als Produktionsdatum für Typenschilder verwendet
"  IF ci_aufk-zzprdat IS INITIAL.
"*    MOVE i_caufvd-gltrp  TO zprdat.
"    MOVE i_caufvd-gltrp  TO ci_aufk-zzprdat.

"  ENDIF.
"ENDIF.
