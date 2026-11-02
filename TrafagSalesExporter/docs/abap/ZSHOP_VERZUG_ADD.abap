*&---------------------------------------------------------------------*
*& Operations (Shopfloor): PPD-Verzug im bestehenden ShopAufSet (Filter Typ eq 'VZ')
*& Stand    : 2026-11-02, T76 (Claude, Auftrag Ingo nach Mail Fabio Palma vom 02.11.)
*& Zweck    : Verzug je Abteilung wie ZC22 (ZM_OFFENE_FAUF) mit Fabios Selektion: offene Fertigungs-
*&            und Planauftraege, Eckendtermin vor dem Stichtag (bis Vortag), OHNE Untergrenze,
*&            ohne erledigte, Menge = Positionsmenge (ZC22-Spalte PSMNG bzw. PLAF-GSMNG).
*&            Disponenten- und Materialfilter macht das Cockpit (Services/Shopfloor/ShopfloorSapVerzug.cs).
*& Warum im ShopAufSet: gleiche Struktur ZSTR_SHOP_AUF, kein neues Property, also keine Aenderung an
*&            MPC_EXT (die lag am 02.11. im offenen Auftrag T76K912793) und kein Cache-Leeren.
*& Zeilen    : Typ VF (Fertigungsauftrag) bzw. VP (Planauftrag), Datum = Eckendtermin, Menge = PSMNG/GSMNG.
*&            Ein alter DPC ohne diesen Block liefert FE/PA-Zeilen; daran erkennt das Cockpit den fehlenden Import.
*& Abgleich  : T76 02.11.2026 gegen SUBMIT ZM_OFFENE_FAUF (Disponent S*/EK*/EL*/D* ohne DW1/DL6, Material
*&            1-99999, Eckendtermin <= Vortag, mit Planauftraegen, ohne erledigte): DW 1'570 / 49'526,
*&            TR5 786 / 41'127, TX 1'349 / 73'895, Unterschiede je Auftrag 0; Laufzeit 0,2 s statt 6 min.
*& Schutz    : Pflicht Werks und Datum (Stichtag hoechstens 7 Tage zurueck, 1 Tag voraus);
*&            mehr als 100'000 Zeilen je Art = Fehler statt stiller Kappung.
*&---------------------------------------------------------------------*

* ===== 1a) In der DATA-Kette des ShopAufSet-Blocks vor "lt_sa_plaf" ergaenzen =====
          lv_sa_typ   TYPE char2,
          lv_sa_msg   TYPE bapi_msg,

* ===== 1b) Nach "ls_sa_fa TYPE ty_sa_fa." ergaenzen =====================
    TYPES: BEGIN OF ty_sa_vz,
             aufnr TYPE afko-aufnr,
             posnr TYPE afpo-posnr,
             gltrp TYPE afko-gltrp,
             dispo TYPE afko-dispo,
             matnr TYPE afpo-matnr,
             psmng TYPE afpo-psmng,
             meins TYPE afpo-meins,
           END OF ty_sa_vz.
    DATA: lt_sa_vz TYPE STANDARD TABLE OF ty_sa_vz,
          ls_sa_vz TYPE ty_sa_vz.

* ===== 2) In der Filterschleife ergaenzen ==============================
          WHEN 'TYP'. lv_sa_typ = <ls_sa_o>-low.

* ===== 3) Direkt nach der Filterschleife, vor "lv_sa_bis = sy-datum - 60." ====
*   PPD-Verzug (Typ VZ), 2026-11-02: wie ZC22, Eckendtermin vor dem Stichtag ohne Untergrenze
    IF lv_sa_typ = 'VZ'.
      lv_sa_bis = sy-datum - 7.
      lv_sa_max = 0.
      IF lv_sa_werks IS INITIAL OR lv_sa_datum IS INITIAL OR lv_sa_datum < lv_sa_bis.
        lv_sa_max = 1.
      ENDIF.
      lv_sa_bis = sy-datum + 1.
      IF lv_sa_datum > lv_sa_bis.
        lv_sa_max = 1.
      ENDIF.
      IF lv_sa_max = 1.
        lv_sa_msg = 'ShopAufSet Typ VZ braucht Werks und Datum (Stichtag JJJJMMTT, hoechstens 7 Tage zurueck).'.
        RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
          EXPORTING
            textid  = /iwbep/cx_mgw_busi_exception=>business_error
            message = lv_sa_msg.
      ENDIF.
*     Fertigungsauftraege: erste offene Position je Auftrag (ZC22: ELIKZ und DNREL leer), Menge PSMNG
      SELECT k~aufnr p~posnr k~gltrp k~dispo p~matnr p~psmng p~meins
        FROM afko AS k INNER JOIN afpo AS p ON p~aufnr = k~aufnr
        INTO TABLE lt_sa_vz UP TO 100001 ROWS
        WHERE p~dwerk = lv_sa_werks
          AND k~gltrp < lv_sa_datum
          AND p~elikz = space
          AND p~dnrel = space.
      SORT lt_sa_vz BY aufnr posnr.
      DELETE ADJACENT DUPLICATES FROM lt_sa_vz COMPARING aufnr.
*     Planauftraege wie ZC22 FILL_IAFPO_FROM_PLAF: umsetzbar (UMSKZ), Eigenfertigung (BESKZ <> F), Menge GSMNG
      SELECT * FROM plaf INTO TABLE lt_sa_plaf UP TO 100001 ROWS
        WHERE plwrk = lv_sa_werks
          AND pedtr < lv_sa_datum
          AND umskz = 'X'
          AND beskz <> 'F'.
      IF lines( lt_sa_vz ) > 100000 OR lines( lt_sa_plaf ) > 100000.
        lv_sa_msg = 'ShopAufSet Typ VZ: mehr als 100000 Auftraege, Abbruch statt Kappung.'.
        RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
          EXPORTING
            textid  = /iwbep/cx_mgw_busi_exception=>business_error
            message = lv_sa_msg.
      ENDIF.
      LOOP AT lt_sa_vz INTO ls_sa_vz.
        CLEAR ls_sa_out.
        ls_sa_out-werks = lv_sa_werks.
        ls_sa_out-aufnr = ls_sa_vz-aufnr.
        ls_sa_out-typ   = 'VF'.
        ls_sa_out-matnr = ls_sa_vz-matnr.
        ls_sa_out-dispo = ls_sa_vz-dispo.
        ls_sa_out-datum = ls_sa_vz-gltrp.
        ls_sa_out-menge = ls_sa_vz-psmng.
        ls_sa_out-meins = ls_sa_vz-meins.
        APPEND ls_sa_out TO lt_sa_out.
      ENDLOOP.
      LOOP AT lt_sa_plaf INTO ls_sa_plaf.
        CLEAR ls_sa_out.
        ls_sa_out-werks = ls_sa_plaf-plwrk.
        ls_sa_out-aufnr = ls_sa_plaf-plnum.
        ls_sa_out-typ   = 'VP'.
        ls_sa_out-matnr = ls_sa_plaf-matnr.
        ls_sa_out-dispo = ls_sa_plaf-dispo.
        ls_sa_out-datum = ls_sa_plaf-pedtr.
        ls_sa_out-menge = ls_sa_plaf-gsmng.
        ls_sa_out-meins = ls_sa_plaf-meins.
        APPEND ls_sa_out TO lt_sa_out.
      ENDLOOP.
      SORT lt_sa_out BY datum aufnr.
      IF is_paging-skip > 0.
        DELETE lt_sa_out TO is_paging-skip.
      ENDIF.
      IF is_paging-top > 0.
        lv_sa_max = is_paging-top + 1.
        DELETE lt_sa_out FROM lv_sa_max.
      ENDIF.
      copy_data_to_ref( EXPORTING is_data = lt_sa_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.
* ===== Ende ============================================================
