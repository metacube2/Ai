*&---------------------------------------------------------------------*
*&  Include           ZXCO1U06
*&---------------------------------------------------------------------*

* ---------------------------------------------------------------------*
* Eckendtermin als Produktionsdatum in AUFK-ZZPRDAT (CAUFVD) sichern

IF header_imp-ftrmi = sy-datum.     "heute freigegeben
* wenn der FAUF jetzt freigeben wurde und ZZPRDAT noch nicht gefüllt ist, dann schreiben;
* erster Eckendetermin d.h. bei erster Freigabe wird als Produktionsdatum für Typenschilder verwendet
  IF header_imp-zzprdat IS INITIAL.

    MOVE header_imp-gltrp  TO header_exp-zzprdat.
    no_update = ' '.
  ENDIF.
ENDIF.

* ---------------------------------------------------------------------*
CHECK NOT header_imp-kdauf_aufk IS INITIAL.   "Kundeneinzelfertigung?
CHECK header_imp-igmng = 0.                   "Prüfen nur falls nicht geliefert

*-Prüfen Vollständigkeit Kundenauftrag---------------------------------*
SELECT COUNT(*) FROM vbuv WHERE vbeln = header_imp-kdauf_aufk
                          AND   posnr = header_imp-kdpos_aufk
                          AND   fdnam LIKE 'ZZ%'.
IF sy-subrc = 0.                              "Kundenauftrag ist unvollständig
  MESSAGE e052(zlo) WITH header_imp-kdauf_aufk header_imp-kdpos_aufk.
ENDIF.

*-Prüfen Auftragsmenge bei aufsteigender Slave-Id----------------------*
SELECT SINGLE * FROM vbap WHERE vbeln = header_imp-kdauf_aufk
                          AND   posnr = header_imp-kdpos_aufk.
IF vbap-mvgr5 = 'A'.       "Slave-Id aufsteigend ?
  IF vbap-kwmeng NE header_imp-gamng.         "Auftragmenge Fertigung muss gleich SD-Menge sein
    MESSAGE w053(zlo) WITH  header_imp-kdauf_aufk header_imp-kdpos_aufk vbap-kwmeng.
    no_update = 'X'.
  ENDIF.
ENDIF.

*-Prüfen Fertigungshinweis vorhanden-----------------------------------*
IF header_imp-aufnr(1) = '%'.              "Nur bei neuem Auftrag
  CALL FUNCTION 'Z_PP_FERTIGUNSHINWEIS'
    EXPORTING
      i_vbeln   = header_imp-kdauf_aufk
      i_posnr   = header_imp-kdpos_aufk
      i_matnr   = header_imp-matnr
    IMPORTING
      no_update = no_update.
ENDIF.
