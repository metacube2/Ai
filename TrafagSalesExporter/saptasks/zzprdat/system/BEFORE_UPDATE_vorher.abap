METHOD if_ex_workorder_update~before_update.
* Zielfassung vom 2026-09-03 fuer T76/100, Paket $TMP.
*
* Warum hier und nicht nur in AT_RELEASE:
* AT_RELEASE bekommt beim Anlegen und Freigeben in einem Vorgang (CO01) noch die
* temporaere Auftragsnummer %00000000001. Das UPDATE findet damit keinen Satz.
* BEFORE_UPDATE laeuft unmittelbar vor der Verbuchung; in IT_HEADER stehen die
* endgueltigen Auftragskoepfe. Damit sind CO01, CO02, CO40, COHV, MD04 und CO41
* gleichermassen abgedeckt.
*
* Warum dynamischer Komponentenzugriff:
* Die Zeilenstruktur von COBAI_T_HEADER liegt in einer Typgruppe und war nicht
* auslesbar. ASSIGN COMPONENT braucht die Struktur nicht zur Uebersetzungszeit.
* Fehlt eine Komponente, wird die Zeile still uebersprungen statt abzubrechen.
*
* Die Freigabepruefung steht bewusst NICHT hier, sondern im Verbuchungsbaustein.
* Dort ist sie an AFKO-FTRMI messbar, und zwar erst dann, wenn die
* Standardverbuchung sie festgeschrieben hat.
  DATA: lv_aufnr  TYPE aufnr,
        lv_prddat TYPE zco_gltrp.

  FIELD-SYMBOLS: <ls_kopf>  TYPE any,
                 <lv_aufnr> TYPE any,
                 <lv_gltrp> TYPE any.

  LOOP AT it_header ASSIGNING <ls_kopf>.

    UNASSIGN: <lv_aufnr>, <lv_gltrp>.

    ASSIGN COMPONENT 'AUFNR' OF STRUCTURE <ls_kopf> TO <lv_aufnr>.
    CHECK sy-subrc = 0.
    ASSIGN COMPONENT 'GLTRP' OF STRUCTURE <ls_kopf> TO <lv_gltrp>.
    CHECK sy-subrc = 0.

    CLEAR: lv_aufnr, lv_prddat.
    lv_aufnr  = <lv_aufnr>.
    lv_prddat = <lv_gltrp>.

    CHECK lv_aufnr IS NOT INITIAL.
    CHECK lv_prddat IS NOT INITIAL.
*   Temporaere Nummern beginnen mit %; sie zeigen auf keinen AUFK-Satz.
    CHECK lv_aufnr(1) <> '%'.

    CALL FUNCTION 'Z_ZZPRDAT_SET' IN UPDATE TASK
      EXPORTING
        iv_aufnr  = lv_aufnr
        iv_prddat = lv_prddat.

  ENDLOOP.
ENDMETHOD.
