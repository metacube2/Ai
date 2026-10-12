REPORT zm_offene_fauf .

* 24.03.2011    ERP_EDI
*               Unterdrückung der Intervallprüfungen für die Select-
*               Options der Eingabefelder 'Woche' und 'Monat' mit Ein-
*               gabemaske '/' da sonst Selekionen wie 12/10 bis 01/11 zu
*               Fehlermeldungen (Untergrenze grösser Obergrenze) führen.

*-Tabellen-------------------------------------------------------------*
TABLES: zpowerbi_kdauf. " Für Typ-Referenzen

TABLES: mara, marc, makt, afpo, afko, aufk,
        vakpa, kna1, vbak, vbbe, vbap, vbfa, vbep,zdispo_spart.
TABLES: zmm_ueb_kdauf.

*-Selektionen----------------------------------------------------------*


CONSTANTS: gc_nr_object_kdaufid TYPE c LENGTH 10 VALUE 'ZPOWERBIID', " Name des Nummernkreisobjekts
           gc_nr_range_kdaufid  TYPE c LENGTH 2  VALUE '01'.         " Intervallnummer



SELECTION-SCREEN BEGIN OF BLOCK b0 WITH FRAME TITLE TEXT-t00.
  PARAMETERS: p_werk TYPE werks_d DEFAULT 1100.  " oder der passende Datentyp für das Werk
SELECTION-SCREEN END OF BLOCK b0.
SELECTION-SCREEN SKIP 1.
SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE TEXT-t01.
  SELECT-OPTIONS:  s_mbdat  FOR sy-datum,
                 s_woche1 FOR zmm_ueb_kdauf-kwoche1,
                 s_monat  FOR zmm_ueb_kdauf-kmonat,
                 s_fkdat  FOR vbfa-erdat,
                 s_kdauf  FOR vbap-vbeln,
                 p_descr FOR zdispo_spart-descr,
                 p_abgru FOR vbap-abgru,
                 s_posnr  FOR vbap-posnr.
  PARAMETERS: p_pstyv LIKE vbap-pstyv.


  SELECT-OPTIONS:  s_kunde  FOR kna1-kunnr,
                   s_name1  FOR kna1-name1,
                   s_ernam  FOR vbak-ernam,
                   s_erdat  FOR vbak-erdat.
  "  PARAMETERS:      p_offen  AS CHECKBOX.  DISABLED KOI
SELECTION-SCREEN END OF BLOCK b1.
SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE TEXT-t02.
  SELECT-OPTIONS:  s_bismt  FOR mara-bismt,
                   s_matnr  FOR mara-matnr,
                   s_zztyp  FOR mara-zztyp_f4.
SELECTION-SCREEN END OF BLOCK b2.
SELECTION-SCREEN BEGIN OF BLOCK b3 WITH FRAME TITLE TEXT-t03.
  SELECT-OPTIONS:  s_aufnr  FOR afpo-aufnr,
                   s_auart  FOR aufk-auart,
                   s_dispo  FOR afko-dispo,
                   s_gltrp  FOR afko-gltrp,
                   s_woche2 FOR zmm_ueb_kdauf-kwoche2,
                   s_fevor FOR afko-fevor.
SELECTION-SCREEN END OF BLOCK b3.



" NEU: Block für den PowerBI Aktualisierungsbutton

" Nachher: Block mit Checkbox
SELECTION-SCREEN BEGIN OF BLOCK b4 WITH FRAME TITLE TEXT-T04. " TEXT-T04: 'Datenbank Aktualisierung'
  PARAMETERS: p_updbi AS CHECKBOX. " Text über Selektionstexte: 'PowerBI Tabelle aktualisieren'
SELECTION-SCREEN END OF BLOCK b4.
PARAMETERS p_vgl NO-DISPLAY. " Vergleichslauf aus ZM_OFFENE_KDAUF_CMP: Ergebnis ins Memory statt ALV

" NEU: Globale Variable, um den Button-Klick zu signalisieren
DATA: gv_update_powerbi_table TYPE abap_bool VALUE abap_false.

* Vergleichslauf 2026-10-12 (Claude): Laufzeitmessung und Export ins ABAP-Memory.
* Aendert keine Logik. P_VGL setzt nur ZM_OFFENE_KDAUF_CMP; dann gibt es keine
* ALV-Liste und keine Fortschreibung von ZPOWERBI_KDAUF.
DATA: gv_ts_start  TYPE timestampl,
      gv_ts_a      TYPE timestampl,
      gv_ts_b      TYPE timestampl,
      gv_rt0       TYPE i,
      gv_rt1       TYPE i,
      gv_n_ivbep   TYPE i,
      gv_t_gesamt   TYPE p LENGTH 16 DECIMALS 3,
      gv_t_ivbep    TYPE p LENGTH 16 DECIMALS 3,
      gv_t_uebauf   TYPE p LENGTH 16 DECIMALS 3,
      gv_t_prefetch TYPE p LENGTH 16 DECIMALS 3,
      gv_t_bereich  TYPE p LENGTH 16 DECIMALS 3,
      gv_t_fkdat    TYPE p LENGTH 16 DECIMALS 3,
      gv_t_bezug    TYPE p LENGTH 16 DECIMALS 3,
      gv_t_dispo    TYPE p LENGTH 16 DECIMALS 3,
      gv_t_conv     TYPE p LENGTH 16 DECIMALS 3,
      gv_n_mbew_mehrfach TYPE i,
      gv_n_flow_fm       TYPE i,
      gv_n_flow_ohne     TYPE i,
      gv_n_flow_leer     TYPE i.

* OPT 2026-10-12: Puffer fuer FILL_UEBAUF, gefuellt in PREFETCH_OPT.
TYPES: BEGIN OF ty_o_vbkd,
         vbeln TYPE vbkd-vbeln,
         posnr TYPE vbkd-posnr,
         bstdk TYPE vbkd-bstdk,
       END OF ty_o_vbkd,
       BEGIN OF ty_o_marc,
         matnr TYPE marc-matnr,
         dzeit TYPE marc-dzeit,
         dispo TYPE marc-dispo,
       END OF ty_o_marc,
       BEGIN OF ty_o_vbfa,
         vbelv TYPE vbfa-vbelv,
         posnv TYPE vbfa-posnv,
         ruuid TYPE vbfa-ruuid,
         erdat TYPE vbfa-erdat,
       END OF ty_o_vbfa,
       BEGIN OF ty_o_mbew,
         matnr TYPE mbew-matnr,
         bwkey TYPE mbew-bwkey,
         bwtar TYPE mbew-bwtar,
         stprs TYPE mbew-stprs,
       END OF ty_o_mbew,
       BEGIN OF ty_o_aufm,
         aufnr TYPE aufm-aufnr,
         bldat TYPE aufm-bldat,
       END OF ty_o_aufm,
       BEGIN OF ty_o_bereich,
         matnr   TYPE vbap-matnr,
         gesetzt TYPE abap_bool,
         wert    TYPE zmm_ueb_kdauf-range,
       END OF ty_o_bereich,
       BEGIN OF ty_o_vbak_ok,
         vbeln TYPE vbak-vbeln,
         kunnr TYPE vbak-kunnr,
       END OF ty_o_vbak_ok.

DATA: gt_o_vbak    TYPE HASHED TABLE OF vbak WITH UNIQUE KEY vbeln,
      gt_o_kna1    TYPE HASHED TABLE OF kna1 WITH UNIQUE KEY kunnr,
      gt_o_vbap    TYPE HASHED TABLE OF vbap WITH UNIQUE KEY vbeln posnr,
      gt_o_vbbe    TYPE HASHED TABLE OF vbbe WITH UNIQUE KEY vbeln posnr etenr,
      gt_o_mara    TYPE HASHED TABLE OF mara WITH UNIQUE KEY matnr,
      gt_o_makt    TYPE HASHED TABLE OF makt WITH UNIQUE KEY matnr,
      gt_o_vbep0   TYPE SORTED TABLE OF vbep WITH UNIQUE KEY vbeln posnr etenr,
      gt_o_vbkd    TYPE HASHED TABLE OF ty_o_vbkd WITH UNIQUE KEY vbeln posnr,
      gt_o_marc    TYPE HASHED TABLE OF ty_o_marc WITH UNIQUE KEY matnr,
      gt_o_vbfa    TYPE HASHED TABLE OF ty_o_vbfa WITH UNIQUE KEY vbelv posnv,
      gt_o_mbew    TYPE HASHED TABLE OF ty_o_mbew WITH UNIQUE KEY matnr bwkey,
      gt_o_afpo    TYPE SORTED TABLE OF afpo WITH NON-UNIQUE KEY kdauf kdpos aufnr posnr,
      gt_o_afko    TYPE HASHED TABLE OF afko WITH UNIQUE KEY aufnr,
      gt_o_aufk    TYPE HASHED TABLE OF aufk WITH UNIQUE KEY aufnr,
      gt_o_aufm    TYPE HASHED TABLE OF ty_o_aufm WITH UNIQUE KEY aufnr,
      gt_o_bereich TYPE HASHED TABLE OF ty_o_bereich WITH UNIQUE KEY matnr,
      gt_o_aufnr_gesehen TYPE HASHED TABLE OF aufnr WITH UNIQUE KEY table_line,
      gt_o_flow_vbfa  TYPE STANDARD TABLE OF vbfa,
      gv_o_flow_vbeln TYPE vbeln,
      gv_o_flow_posnr TYPE posnr,
      gv_o_flow_da    TYPE abap_bool,
      gt_o_flow_beleg TYPE HASHED TABLE OF vbeln WITH UNIQUE KEY table_line.



RANGES: r_week1 FOR vbep-edatu,
        r_week2 FOR afko-gltrs.

*-Int. Tabellen--------------------------------------------------------*


DATA:
  gv_lastvbeln       TYPE vbrp-vbeln.
DATA: gv_cursor  TYPE i,
      gv_lastpos TYPE vbrp-posnr,
      gv_index   TYPE sy-tabix.



DATA: gt_uebauf LIKE zmm_ueb_kdauf OCCURS 0 WITH HEADER LINE.
DATA: ivbep LIKE vbep OCCURS 0 WITH HEADER LINE.
DATA: iaufm LIKE aufm OCCURS 0 WITH HEADER LINE.
DATA: BEGIN OF bptab OCCURS 0.
        INCLUDE STRUCTURE zzbptab.
DATA: END OF bptab.
DATA: BEGIN OF iafpo OCCURS 0,
        aufnr LIKE afpo-aufnr,
      END OF iafpo.

*-Diverses-------------------------------------------------------------*

DATA: BEGIN OF lt_result OCCURS 0,
        mandt    TYPE mandt,
        dispo    TYPE dispo,
        dispo_kz TYPE z_disp_kz,

        descr    TYPE z_disp_kz,
      END OF lt_result.

DATA: w_datum LIKE sy-datum,
      w_netwr LIKE gt_uebauf-netwr1,
      dat_lo  LIKE sy-datum,
      dat_hi  LIKE sy-datum,
      xweek   LIKE scal-week,
      xdatum  LIKE sy-datum.

TYPE-POOLS: slis.

DATA: gs_extract1 LIKE disextract.
DATA: gs_extract2 LIKE disextract.
DATA: gt_fieldcat TYPE slis_t_fieldcat_alv.

* 24.03.2011, ERP_EDI, begin of insert

DATA: z_selop        LIKE rsldbselop OCCURS 0 WITH HEADER LINE.
DATA: z_rsvar_report LIKE rsvar-report.

* 24.03.2011, ERP_EDI, end of insert

*---------------------------------------------------------------------*
INITIALIZATION.



  CALL FUNCTION 'REUSE_ALV_EXTRACT_AT_INIT'
    CHANGING
      cs_extract1 = gs_extract1
      cs_extract2 = gs_extract2.

* 24.03.2011, ERP_EDI, begin of insert

  z_selop-name   = 'S_MONAT'.  APPEND z_selop.
  z_selop-name   = 'S_WOCHE1'. APPEND z_selop.
  z_selop-name   = 'S_WOCHE2'. APPEND z_selop.

  z_rsvar_report = sy-repid.

  CALL FUNCTION 'RS_SELOPT_NO_INTERVAL_CHECK'
    EXPORTING
      program        = z_rsvar_report
    TABLES
      selop          = z_selop
    EXCEPTIONS
      no_programname = 1
      OTHERS         = 2.

  IF sy-subrc <> 0.
    MESSAGE ID sy-msgid TYPE sy-msgty NUMBER sy-msgno
    WITH sy-msgv1 sy-msgv2 sy-msgv3 sy-msgv4.
  ENDIF.

* 24.03.2011, ERP_EDI, end of insert

*at selection-screen output.
*  if s_mbdat[] is initial.
*    w_datum = sy-datum.
*    w_datum+6(2) = '01'.
*    s_mbdat-low = w_datum.
*    w_datum = w_datum + 35.
*    w_datum+6(2) = '01'.
*    w_datum = w_datum - 1.
*    s_mbdat-high = w_datum.
*    append s_mbdat.
*  endif.

AT SELECTION-SCREEN.





  IF s_mbdat[] IS INITIAL AND
    s_woche1[] IS INITIAL AND
    s_monat[]  IS INITIAL.
    MESSAGE e008(zlo).
  ENDIF.

*----------------------------------------------------------------------*
* Verarbeitung                                                         *
*----------------------------------------------------------------------*

START-OF-SELECTION.
  GET TIME STAMP FIELD gv_ts_start.
  PERFORM e01_fieldcat_init  USING gt_fieldcat[].
  PERFORM bilden_selwochen.
  GET TIME STAMP FIELD gv_ts_a.
  PERFORM fill_ivbep.
  GET TIME STAMP FIELD gv_ts_b.
  gv_t_ivbep = cl_abap_tstmp=>subtract( tstmp1 = gv_ts_b tstmp2 = gv_ts_a ).
  gv_n_ivbep = lines( ivbep[] ).

  REFRESH gt_uebauf.
  PERFORM fill_uebauf.
  PERFORM vergleich_export.

  WRITE: dat_lo TO sy-tvar0 DD/MM/YYYY.
  WRITE: dat_hi TO sy-tvar1 DD/MM/YYYY.


  DATA: ls_layout TYPE slis_layout_alv.

  ls_layout-no_totalline = ' '.


  SET TITLEBAR 'TIT' WITH sy-tvar0 sy-tvar1.

  CALL FUNCTION 'REUSE_ALV_LIST_DISPLAY'
    EXPORTING
      i_callback_program = gs_extract1-report
      i_structure_name   = 'zmm_ueb_kdauf'(001)
      is_layout          = ls_layout
      it_fieldcat        = gt_fieldcat[]
      i_save             = 'A'
    TABLES
      t_outtab           = gt_uebauf.


 IF p_updbi = abap_true. " abap_true ist 'X'
    IF gt_uebauf IS NOT INITIAL.
      MESSAGE 'Checkbox "PowerBI Tabelle aktualisieren" ist aktiv. Starte Update...' TYPE 'S' DISPLAY LIKE 'I'.
      PERFORM write_to_zpowerbi_kdauf.
    ELSE.
      MESSAGE 'Checkbox "PowerBI Tabelle aktualisieren" ist aktiv, aber keine Daten für ALV gefunden. Tabelle ZPOWERBI_KDAUF wird geleert.' TYPE 'S' DISPLAY LIKE 'I'.
      PERFORM clear_zpowerbi_kdauf.
    ENDIF.
  ELSE.
    MESSAGE 'Nur ALV-Anzeige. Für Datenbank-Update bitte Checkbox aktivieren.' TYPE 'S' DISPLAY LIKE 'I'.
  ENDIF.

*&---------------------------------------------------------------------*
*&      Form  fill_ivbep
*&---------------------------------------------------------------------*
FORM fill_ivbep.
  REFRESH ivbep.
  SELECT * FROM vbep INTO TABLE ivbep WHERE edatu IN r_week1 "#EC CI_ALL_FIELDS_NEEDED
                                      AND   vbeln IN s_kdauf
                                       AND   posnr IN s_posnr
                                  AND   lfrel = 'X'
                                      AND   bmeng NE 0.

    IF NOT p_pstyv IS INITIAL OR
      NOT s_matnr[] IS INITIAL OR
      NOT s_dispo[] IS INITIAL OR
      NOT s_zztyp[] IS INITIAL OR
      NOT s_bismt[] IS INITIAL.
      LOOP AT ivbep.
        SELECT SINGLE * FROM vbap WHERE vbeln = ivbep-vbeln "#EC CI_ALL_FIELDS_NEEDED
                                  AND   posnr = ivbep-posnr
                                 AND   werks = p_werk
                                  AND   matnr IN s_matnr.
          IF ( vbap-pstyv NE p_pstyv AND NOT p_pstyv IS INITIAL ) OR
            sy-subrc NE 0.
            DELETE ivbep.
          ELSE.
            SELECT COUNT(*) FROM mara WHERE matnr = vbap-matnr
                                      AND   zztyp_f4 IN s_zztyp
                                      AND   bismt IN s_bismt.
              IF sy-subrc NE 0.
                DELETE ivbep.

              ELSE.
                SELECT COUNT(*) FROM marc WHERE matnr = vbap-matnr  AND   werks = p_werk
                                          AND   dispo IN s_dispo.
                  IF sy-subrc NE 0.
                    DELETE ivbep.
                  ENDIF.
                ENDIF.
              ENDIF.
            ENDLOOP.
          ENDIF.

          " OPT 2026-10-12: war je Zeile SELECT SINGLE * FROM vbak (mit kunnr/ernam/erdat/vbtyp)
          " und SELECT COUNT(*) FROM kna1 (werks, name1). Jetzt dieselben Bedingungen einmal fuer
          " alle Belege in der Datenbank; die Schleife loescht wie bisher, in derselben Reihenfolge.
          DATA: lt_o_vbeln   TYPE STANDARD TABLE OF ty_o_vbak_ok,
                lt_o_vbak_ok TYPE HASHED TABLE OF ty_o_vbak_ok WITH UNIQUE KEY vbeln,
                lt_o_kunnr   TYPE STANDARD TABLE OF ty_o_vbak_ok,
                lt_o_kna1_ok TYPE HASHED TABLE OF kunnr WITH UNIQUE KEY table_line,
                ls_o_vbak_ok TYPE ty_o_vbak_ok.

          LOOP AT ivbep[] INTO DATA(ls_o_ep).
            APPEND VALUE #( vbeln = ls_o_ep-vbeln ) TO lt_o_vbeln.
          ENDLOOP.
          SORT lt_o_vbeln BY vbeln.
          DELETE ADJACENT DUPLICATES FROM lt_o_vbeln COMPARING vbeln.
          IF lt_o_vbeln IS NOT INITIAL.
            SELECT vbeln kunnr FROM vbak INTO TABLE lt_o_vbak_ok
              FOR ALL ENTRIES IN lt_o_vbeln
              WHERE vbeln = lt_o_vbeln-vbeln
              AND   kunnr IN s_kunde
              AND   ernam IN s_ernam
              AND   erdat IN s_erdat
              AND   ( vbtyp = 'C' OR vbtyp = 'E' ).
          ENDIF.
          IF NOT s_name1[] IS INITIAL AND lt_o_vbak_ok IS NOT INITIAL.
            lt_o_kunnr = lt_o_vbak_ok.
            SORT lt_o_kunnr BY kunnr.
            DELETE ADJACENT DUPLICATES FROM lt_o_kunnr COMPARING kunnr.
            SELECT kunnr FROM kna1 INTO TABLE lt_o_kna1_ok
              FOR ALL ENTRIES IN lt_o_kunnr
              WHERE kunnr = lt_o_kunnr-kunnr
              AND   werks = p_werk
              AND   name1 IN s_name1.
          ENDIF.

          LOOP AT ivbep.
            READ TABLE lt_o_vbak_ok INTO ls_o_vbak_ok WITH TABLE KEY vbeln = ivbep-vbeln.
            IF sy-subrc NE 0.
              DELETE ivbep.
            ELSE.
              IF NOT s_name1[] IS INITIAL.
                READ TABLE lt_o_kna1_ok WITH TABLE KEY table_line = ls_o_vbak_ok-kunnr
                     TRANSPORTING NO FIELDS.
                IF sy-subrc NE 0.
                  DELETE ivbep.
                ENDIF.
              ENDIF.
            ENDIF.
          ENDLOOP.

              IF NOT s_aufnr[] IS INITIAL.
                LOOP AT ivbep.
                  SELECT COUNT(*) FROM afpo WHERE aufnr IN s_aufnr
                                     AND   kdauf = ivbep-vbeln
                                     AND   kdpos = ivbep-posnr
                                     AND   kdpos GT 0.
                    IF sy-subrc NE 0.
                      DELETE ivbep.
                    ENDIF.
                  ENDLOOP.
                ENDIF.

                IF NOT r_week2[] IS INITIAL OR
                  NOT s_auart[] IS INITIAL OR
                  NOT s_fevor[] IS INITIAL.
                  LOOP AT ivbep.
                    SELECT * FROM afpo WHERE kdauf = ivbep-vbeln "#EC CI_ALL_FIELDS_NEEDED
                                       AND   kdpos = ivbep-posnr
                                       AND   dgltp IN r_week2
              ORDER BY PRIMARY KEY.
                      "Added by IN23914 during code remediation
                    ENDSELECT.
                    IF sy-subrc NE 0.
                      DELETE ivbep.
                    ELSE.
                      SELECT COUNT(*) FROM aufk WHERE aufnr = afpo-aufnr    AND   werks = p_werk "#EC CI_ALL_FIELDS_NEEDED
                                                AND   auart IN s_auart.
                        IF sy-subrc NE 0.
                          DELETE ivbep.
                        ENDIF.
                        SELECT COUNT(*) FROM afko WHERE aufnr = afpo-aufnr "#EC CI_ALL_FIELDS_NEEDED
                                                  AND   fevor IN s_fevor.
                          IF sy-subrc NE 0.
                            DELETE ivbep.
                          ENDIF.
                        ENDIF.
                      ENDLOOP.
                    ENDIF.

                    DATA p_offen TYPE string.
                    p_offen = ''.

                    IF p_offen = 'X'.
                      LOOP AT ivbep.
                        SELECT COUNT(*) FROM vbbe WHERE vbeln = ivbep-vbeln    AND   werks = p_werk "#EC CI_ALL_FIELDS_NEEDED
                                                  AND   posnr = ivbep-posnr
                                                  AND   etenr = ivbep-etenr
                                                   AND   werks = p_werk
                                                  AND   omeng NE 0.
                          IF sy-subrc NE 0.
                            DELETE ivbep.
                          ENDIF.
                        ENDLOOP.
                      ENDIF.

ENDFORM.                    "fill_ivbep

*&---------------------------------------------------------------------*
*&      Form  fill_uebauf
*&---------------------------------------------------------------------*
FORM fill_uebauf.

  " Sort for correct fkdat assigning on fill_fkdat
  "insert 24.5.24
  " 2026-10-12: ETENR als letztes Sortierfeld. Ohne es ist die Reihenfolge von Einteilungen
  " mit gleichem Lieferdatum zufaellig (REF gegen REF belegt), und mit ihr die Zuordnung der
  " Fertigungsauftraege, die Auftragsfelder und das Fakturadatum dieser Einteilungen.
  SORT   ivbep BY vbeln posnr  edatu etenr ASCENDING.
  "end insert 24.5.24

  " OPT 2026-10-12: Alle Lesezugriffe der Schleife einmal vorab, mit denselben Bedingungen.
  " Die Schleife selbst ist Anweisung fuer Anweisung die alte; jeder ersetzte Zugriff steht
  " unten als Kommentar "war: ..." daneben.
  PERFORM prefetch_opt.

  LOOP AT ivbep. "Für jede Position in VBAP kann es mehrere Einteilungsdaten in VBEP geben.
    IF ivbep-edatu LT dat_lo OR
      dat_lo IS INITIAL.
      dat_lo = ivbep-edatu.
    ENDIF.
    IF ivbep-edatu GT dat_hi OR
      dat_hi IS INITIAL.
      dat_hi = ivbep-edatu.
    ENDIF.


    CLEAR vbak. CLEAR kna1.CLEAR vbap.CLEAR vbbe. CLEAR mara.CLEAR makt.

    " war: SELECT SINGLE * FROM vbak WHERE vbeln = ivbep-vbeln
    READ TABLE gt_o_vbak INTO vbak WITH TABLE KEY vbeln = ivbep-vbeln.
    " war: SELECT SINGLE * FROM kna1 erst mit werks = p_werk, sonst ohne. KUNNR ist der ganze
    "      Schluessel, beide Zugriffe liefern also dieselbe Zeile oder beide keine.
    READ TABLE gt_o_kna1 INTO kna1 WITH TABLE KEY kunnr = vbak-kunnr.
    IF sy-subrc <> 0.
      kna1-name1 = 'Kunde nicht gefunden'.
      " Optional: Protokollieren Sie den Fehler
    ENDIF.


    " war: SELECT SINGLE * FROM vbap WHERE vbeln posnr werks = p_werk (Puffer hat nur Werk p_werk)
    READ TABLE gt_o_vbap INTO vbap WITH TABLE KEY vbeln = ivbep-vbeln posnr = ivbep-posnr.
    CLEAR vbbe.
    " war: SELECT SINGLE * FROM vbbe WHERE vbeln posnr etenr werks = p_werk
    READ TABLE gt_o_vbbe INTO vbbe WITH TABLE KEY vbeln = ivbep-vbeln posnr = ivbep-posnr etenr = ivbep-etenr.
    " war: SELECT SINGLE * FROM mara WHERE matnr = vbap-matnr
    READ TABLE gt_o_mara INTO mara WITH TABLE KEY matnr = vbap-matnr.
    " war: SELECT SINGLE * FROM makt WHERE matnr = mara-matnr AND spras = sy-langu
    READ TABLE gt_o_makt INTO makt WITH TABLE KEY matnr = mara-matnr.

    CLEAR gt_uebauf.
    MOVE-CORRESPONDING ivbep TO gt_uebauf.
    MOVE-CORRESPONDING kna1 TO gt_uebauf.
    MOVE-CORRESPONDING vbak TO gt_uebauf.
    MOVE-CORRESPONDING vbap TO gt_uebauf.
    MOVE-CORRESPONDING mara TO gt_uebauf.
    MOVE-CORRESPONDING makt TO gt_uebauf.
    gt_uebauf-dispo = ''.






    gt_uebauf-omeng = vbbe-omeng.
    gt_uebauf-omengn = ivbep-ocdqty_bu. "KOI
    gt_uebauf-kunnr = vbak-kunnr.
    gt_uebauf-ernam = vbak-ernam.
    gt_uebauf-erdat = vbap-erdat.
    gt_uebauf-erdat = vbap-abgru.


    IF ivbep-wmeng = ivbep-bmeng.
      WRITE ivbep-edatu TO gt_uebauf-ketdat DD/MM/YYYY.
    ELSE.
      " war: SELECT * FROM vbep WHERE vbeln posnr bmeng = 0 AND edatu LT ivbep-edatu
      "      ORDER BY PRIMARY KEY. ENDSELECT.  -> letzte Zeile nach ETENR, nicht das spaeteste Datum
      LOOP AT gt_o_vbep0 INTO vbep WHERE vbeln = ivbep-vbeln
                                    AND   posnr = ivbep-posnr
                                    AND   edatu LT ivbep-edatu.
      ENDLOOP.
      IF sy-subrc = 0.
        WRITE vbep-edatu TO gt_uebauf-ketdat DD/MM/YYYY.
      ENDIF.
    ENDIF.
    " war: SELECT SINGLE bstdk INTO gt_uebauf-bstdk FROM vbkd (Position, sonst Kopf posnr = 0);
    "      nicht gefunden -> Feld behaelt den Wert aus MOVE-CORRESPONDING vbak
    READ TABLE gt_o_vbkd INTO DATA(ls_o_vbkd) WITH TABLE KEY vbeln = vbap-vbeln posnr = vbap-posnr.
    IF sy-subrc = 0.
      gt_uebauf-bstdk = ls_o_vbkd-bstdk.
    ELSE.
      READ TABLE gt_o_vbkd INTO ls_o_vbkd WITH TABLE KEY vbeln = vbap-vbeln posnr = '000000'.
      IF sy-subrc = 0.
        gt_uebauf-bstdk = ls_o_vbkd-bstdk.
      ENDIF.
    ENDIF.

    GET RUN TIME FIELD gv_rt0.
    CALL FUNCTION 'CONVERT_TO_LOCAL_CURRENCY'
      EXPORTING
        date             = sy-datum
        foreign_amount   = vbap-netwr
        foreign_currency = vbak-waerk
        local_currency   = 'CHF'
      IMPORTING
        local_amount     = w_netwr.
    GET RUN TIME FIELD gv_rt1.
    gv_t_conv = gv_t_conv + ( gv_rt1 - gv_rt0 ) / 1000000.

    gt_uebauf-netwr1  = w_netwr * gt_uebauf-bmeng / vbap-kwmeng.
    gt_uebauf-netwr2  = w_netwr * gt_uebauf-omeng / vbap-kwmeng.
    gt_uebauf-netwr3n = w_netwr * gt_uebauf-omengn / vbap-kwmeng.
    gt_uebauf-waers = 'CHF'.


    " Berechnung des Standardwerts
    "    gt_uebauf-standardwert = gt_uebauf-bmeng * gt_uebauf-netwr1 .


    gt_uebauf-vbeln = ivbep-vbeln.
    GET RUN TIME FIELD gv_rt0.
    PERFORM bereich USING gt_uebauf-range.
    GET RUN TIME FIELD gv_rt1.
    gv_t_bereich = gv_t_bereich + ( gv_rt1 - gv_rt0 ) / 1000000.

    " war: SELECT SINGLE dzeit FROM marc INTO gt_uebauf-dzeit WHERE matnr = mara-matnr AND werks = p_werk
    READ TABLE gt_o_marc INTO DATA(ls_o_marc) WITH TABLE KEY matnr = mara-matnr.
    IF sy-subrc = 0.
      gt_uebauf-dzeit = ls_o_marc-dzeit.
    ENDIF.

    " war: SELECT * FROM vbfa WHERE vbelv = vbap-vbeln AND posnv = vbap-posnr AND vbtyp_n = 'M'
    "      ORDER BY PRIMARY KEY. ENDSELECT.  Schluessel ist MANDT/RUUID, die letzte Zeile ist
    "      also die mit der groessten RUUID (im Vorabpuffer so ausgewaehlt).
    READ TABLE gt_o_vbfa INTO DATA(ls_o_vbfa) WITH TABLE KEY vbelv = vbap-vbeln posnv = vbap-posnr.
    IF sy-subrc = 0.
      gt_uebauf-fkdat =  ls_o_vbfa-erdat.
    ENDIF.

    CLEAR xweek.
    CALL FUNCTION 'DATE_GET_WEEK'
      EXPORTING
        date = ivbep-edatu
      IMPORTING
        week = xweek.
    gt_uebauf-kwoche1 = '../..'.
    gt_uebauf-kwoche1+0(2) = xweek+4(2).
    gt_uebauf-kwoche1+3(2) = xweek+2(2).

    gt_uebauf-kmonat+0(2) = ivbep-edatu+4(2).
    gt_uebauf-kmonat+2(1) = '/'.
    gt_uebauf-kmonat+3(2) = ivbep-edatu+2(2).

    CLEAR aufk.

    " Standardpreis holen
    PERFORM get_standard_price USING vbap-matnr
                                     vbap-werks
                              CHANGING gt_uebauf-standardpreis.



    " war: SELECT * FROM afpo WHERE kdauf = ivbep-vbeln AND kdpos = ivbep-posnr
    "      ORDER BY PRIMARY KEY. ... ENDSELECT.  Der Puffer ist nach KDAUF/KDPOS/AUFNR/POSNR
    "      sortiert, die Schleife laeuft also in derselben Reihenfolge.
    LOOP AT gt_o_afpo INTO afpo WHERE kdauf = ivbep-vbeln
                                AND   kdpos = ivbep-posnr.
      " war: READ TABLE iafpo WITH KEY aufnr = afpo-aufnr (lineare Suche ueber alle bisherigen)
      READ TABLE gt_o_aufnr_gesehen WITH TABLE KEY table_line = afpo-aufnr TRANSPORTING NO FIELDS.
      IF sy-subrc NE 0.
        iafpo-aufnr = afpo-aufnr.
        APPEND iafpo.
        INSERT afpo-aufnr INTO TABLE gt_o_aufnr_gesehen.

        " war: SELECT SINGLE * FROM afko WHERE aufnr = afpo-aufnr
        "      (nicht gefunden -> AFKO behaelt wie bisher den Wert der Vorzeile)
        READ TABLE gt_o_afko INTO afko WITH TABLE KEY aufnr = afpo-aufnr.
        " war: SELECT SINGLE * FROM aufk WHERE aufnr = afpo-aufnr AND werks = p_werk
        READ TABLE gt_o_aufk INTO aufk WITH TABLE KEY aufnr = afpo-aufnr.
        CLEAR xweek.
        CALL FUNCTION 'DATE_GET_WEEK'
          EXPORTING
            date = afko-gltrp
          IMPORTING
            week = xweek.
        gt_uebauf-kwoche2 = '../..'.
        gt_uebauf-kwoche2+0(2) = xweek+4(2).
        gt_uebauf-kwoche2+3(2) = xweek+2(2).

        "     gt_uebauf-spart = mara-spart.

        MOVE-CORRESPONDING aufk TO gt_uebauf.
        MOVE-CORRESPONDING afko TO gt_uebauf.
        MOVE-CORRESPONDING afpo TO gt_uebauf.
        gt_uebauf-gltri = afpo-ltrmi.
        GET RUN TIME FIELD gv_rt0.
        PERFORM erster_bezug.
        GET RUN TIME FIELD gv_rt1.
        gv_t_bezug = gv_t_bezug + ( gv_rt1 - gv_rt0 ) / 1000000.
        EXIT.
      ENDIF.
    ENDLOOP.

    gt_uebauf-werk = vbap-werks.

    IF aufk-auart IS INITIAL.
      CLEAR gt_uebauf-auart.
    ENDIF.
    gt_uebauf-posnr = ivbep-posnr.

    gt_uebauf-dispo = ''. "KOI


    gt_uebauf-standardwert = gt_uebauf-standardpreis * gt_uebauf-bmeng.




    "insert 24.5.24
    GET RUN TIME FIELD gv_rt0.
    PERFORM fill_fkdat USING ivbep-vbeln ivbep-posnr ivbep-bmeng CHANGING gt_uebauf.
    GET RUN TIME FIELD gv_rt1.
    gv_t_fkdat = gv_t_fkdat + ( gv_rt1 - gv_rt0 ) / 1000000.
    "end inser 24.5.24

    "KOI INSERT
    " Abrufen des Disponenten aus der MARC-Tabelle
    " war: SELECT SINGLE dispo FROM marc INTO gt_uebauf-dispo WHERE matnr = mara-matnr AND werks = p_werk
    READ TABLE gt_o_marc INTO ls_o_marc WITH TABLE KEY matnr = mara-matnr.
    IF sy-subrc = 0.
      gt_uebauf-dispo = ls_o_marc-dispo.
    ENDIF.

    " war: SELECT SINGLE * FROM makt WHERE matnr = mara-matnr AND spras = sy-langu (zweites Mal)
    READ TABLE gt_o_makt INTO makt WITH TABLE KEY matnr = mara-matnr.




    DATA: lv_search_pattern TYPE string,
          lv_pos            TYPE i.

    " war: SELECT a~dispo a~dispo_kz b~descr INTO CORRESPONDING FIELDS OF TABLE lt_result
    "      FROM zdispo_grp JOIN zdispo_spart, in jeder Zeile neu. LT_RESULT wird jetzt einmal
    "      in PREFETCH_OPT mit derselben Anweisung gefuellt.


    LOOP AT lt_result INTO DATA(wa_result)."alle


      FIND FIRST OCCURRENCE OF '*' IN wa_result-dispo_kz MATCH OFFSET lv_pos.
      IF sy-subrc = 0.
        " Alles vor dem Stern abschneiden und Suchmuster erstellen
        lv_search_pattern = wa_result-dispo_kz(lv_pos)  && '*'.
      ELSE.
        " Wenn kein Stern vorhanden ist, dann den gesamten Wert verwenden
        lv_search_pattern = wa_result-dispo_kz.
      ENDIF.

      IF gt_uebauf-dispo CP lv_search_pattern.
        gt_uebauf-dispo_DES = wa_result-descr.
      ENDIF.

    ENDLOOP.

    "KOI END

    " Anteil berechnen

    IF gt_uebauf-standardwert > 0 AND  gt_uebauf-netwr1 > 0 .
      gt_uebauf-anteil =  gt_uebauf-netwr1 /  gt_uebauf-standardwert."( gt_uebauf-standardpreis * gt_uebauf-bmeng ) / vbap-kpein.
    ENDIF.

    APPEND gt_uebauf.
  ENDLOOP.


  LOOP AT gt_uebauf INTO DATA(wa_uebauf) WHERE fkdat NOT IN s_fkdat.
    DELETE gt_uebauf.
  ENDLOOP.

  LOOP AT gt_uebauf INTO DATA(wa_uebauf2).
    IF wa_uebauf2-werk NE p_werk.
      DELETE gt_uebauf.
    ENDIF.
  ENDLOOP.

  "AND abgru IN p_abgru "KOI
  LOOP AT gt_uebauf INTO wa_uebauf2  WHERE abgru NOT   IN p_abgru.
    IF wa_uebauf2-abgru NE p_werk.
      DELETE gt_uebauf.
    ENDIF.
  ENDLOOP.


  LOOP AT gt_uebauf INTO  wa_uebauf2 WHERE dispo_des NOT IN p_descr.
    IF p_descr IS NOT INITIAL.
      DELETE gt_uebauf.
    ENDIF.
  ENDLOOP.

* KOI INSERT
  SELECT SINGLE * FROM mara WHERE matnr = vbap-matnr. "#EC CI_ALL_FIELDS_NEEDED




*
  " SORT gt_uebauf.
  " DELETE ADJACENT DUPLICATES FROM gt_uebauf.





  " sort gt_uebauf by vbeln posnr edatu.
  SORT gt_uebauf BY edatu vbeln posnr.
ENDFORM.                    " fill_uebauf

*&---------------------------------------------------------------------*
*&      Form  PREFETCH_OPT
*&---------------------------------------------------------------------*
* OPT 2026-10-12: liest alles, was FILL_UEBAUF bisher Zeile fuer Zeile
* gelesen hat, einmal fuer alle Einteilungen aus IVBEP. Bedingungen wie
* bisher; FOR ALL ENTRIES immer mit dem ganzen Schluessel in der
* Feldliste (sonst entfernt die Datenbank gleiche Zeilen) und nie mit
* leerer Treibertabelle.
*----------------------------------------------------------------------*
FORM prefetch_opt.
  TYPES: BEGIN OF lty_pos,
           vbeln TYPE vbep-vbeln,
           posnr TYPE vbep-posnr,
         END OF lty_pos,
         BEGIN OF lty_ein,
           vbeln TYPE vbep-vbeln,
           posnr TYPE vbep-posnr,
           etenr TYPE vbep-etenr,
         END OF lty_ein,
         BEGIN OF lty_matnr,
           matnr TYPE mara-matnr,
         END OF lty_matnr,
         BEGIN OF lty_kunnr,
           kunnr TYPE kna1-kunnr,
         END OF lty_kunnr,
         BEGIN OF lty_aufnr,
           aufnr TYPE afpo-aufnr,
         END OF lty_aufnr.
  DATA: lt_pos   TYPE STANDARD TABLE OF lty_pos,
        lt_ein   TYPE STANDARD TABLE OF lty_ein,
        lt_matnr TYPE STANDARD TABLE OF lty_matnr,
        lt_kunnr TYPE STANDARD TABLE OF lty_kunnr,
        lt_aufnr TYPE STANDARD TABLE OF lty_aufnr,
        lt_vbak  TYPE STANDARD TABLE OF vbak,
        lt_kna1  TYPE STANDARD TABLE OF kna1,
        lt_vbap  TYPE STANDARD TABLE OF vbap,
        lt_vbbe  TYPE STANDARD TABLE OF vbbe,
        lt_mara  TYPE STANDARD TABLE OF mara,
        lt_makt  TYPE STANDARD TABLE OF makt,
        lt_vbep0 TYPE STANDARD TABLE OF vbep,
        lt_vbkd  TYPE STANDARD TABLE OF ty_o_vbkd,
        lt_marc  TYPE STANDARD TABLE OF ty_o_marc,
        lt_vbfa  TYPE STANDARD TABLE OF ty_o_vbfa,
        lt_mbew  TYPE STANDARD TABLE OF ty_o_mbew,
        lt_afpo  TYPE STANDARD TABLE OF afpo,
        lt_afko  TYPE STANDARD TABLE OF afko,
        lt_aufk  TYPE STANDARD TABLE OF aufk,
        lt_aufm  TYPE STANDARD TABLE OF ty_o_aufm,
        lv_ts0   TYPE timestampl,
        lv_ts1   TYPE timestampl.

  GET TIME STAMP FIELD lv_ts0.

  CLEAR: gt_o_vbak, gt_o_kna1, gt_o_vbap, gt_o_vbbe, gt_o_mara, gt_o_makt,
         gt_o_vbep0, gt_o_vbkd, gt_o_marc, gt_o_vbfa, gt_o_mbew, gt_o_afpo,
         gt_o_afko, gt_o_aufk, gt_o_aufm, gt_o_bereich, gt_o_aufnr_gesehen,
         gt_o_flow_vbfa, gv_o_flow_vbeln, gv_o_flow_posnr, gv_o_flow_da, gt_o_flow_beleg,
         gv_n_mbew_mehrfach.

  " Disponentengruppen: dieselbe Anweisung wie bisher in der Schleife, jetzt einmal
  SELECT a~dispo a~dispo_kz b~descr
    INTO CORRESPONDING FIELDS OF TABLE lt_result
    FROM zdispo_grp AS a
    JOIN zdispo_spart AS b ON a~dispo = b~dispo.

  LOOP AT ivbep[] INTO DATA(ls_ep).
    APPEND VALUE #( vbeln = ls_ep-vbeln posnr = ls_ep-posnr ) TO lt_pos.
    APPEND VALUE #( vbeln = ls_ep-vbeln posnr = ls_ep-posnr etenr = ls_ep-etenr ) TO lt_ein.
  ENDLOOP.
  SORT lt_pos BY vbeln posnr.
  DELETE ADJACENT DUPLICATES FROM lt_pos COMPARING vbeln posnr.

  IF lt_pos IS NOT INITIAL.
    SELECT * FROM vbak INTO TABLE lt_vbak            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln.
    gt_o_vbak = lt_vbak.

    SELECT * FROM vbap INTO TABLE lt_vbap            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln
      AND   posnr = lt_pos-posnr
      AND   werks = p_werk.
    gt_o_vbap = lt_vbap.

    SELECT * FROM vbep INTO TABLE lt_vbep0           "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln
      AND   posnr = lt_pos-posnr
      AND   bmeng = 0.
    gt_o_vbep0 = lt_vbep0.

    SELECT vbeln posnr bstdk FROM vbkd INTO TABLE lt_vbkd
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln.
    gt_o_vbkd = lt_vbkd.

    SELECT vbelv posnv ruuid erdat FROM vbfa INTO TABLE lt_vbfa
      FOR ALL ENTRIES IN lt_pos
      WHERE vbelv   = lt_pos-vbeln
      AND   posnv   = lt_pos-posnr
      AND   vbtyp_n = 'M'.
    " letzte Zeile nach Primaerschluessel (RUUID) je Position behalten
    SORT lt_vbfa BY vbelv posnv ruuid DESCENDING.
    DELETE ADJACENT DUPLICATES FROM lt_vbfa COMPARING vbelv posnv.
    gt_o_vbfa = lt_vbfa.

    " Belege, die ueberhaupt einen Belegfluss haben (als Vorgaenger oder als Nachfolger).
    " Nur fuer sie kann RV_ORDER_FLOW_INFORMATION Fakturazeilen liefern, siehe FILL_FKDAT.
    DATA: lt_fluss TYPE STANDARD TABLE OF vbeln.
    SELECT DISTINCT vbelv FROM vbfa INTO TABLE lt_fluss
      FOR ALL ENTRIES IN lt_pos
      WHERE vbelv = lt_pos-vbeln.
    LOOP AT lt_fluss INTO DATA(lv_fluss).
      INSERT lv_fluss INTO TABLE gt_o_flow_beleg.
    ENDLOOP.
    CLEAR lt_fluss.
    SELECT DISTINCT vbeln FROM vbfa INTO TABLE lt_fluss
      FOR ALL ENTRIES IN lt_pos
      WHERE vbeln = lt_pos-vbeln.
    LOOP AT lt_fluss INTO lv_fluss.
      INSERT lv_fluss INTO TABLE gt_o_flow_beleg.
    ENDLOOP.

    SELECT * FROM afpo INTO TABLE lt_afpo            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_pos
      WHERE kdauf = lt_pos-vbeln
      AND   kdpos = lt_pos-posnr.
    gt_o_afpo = lt_afpo.
  ENDIF.

  IF lt_ein IS NOT INITIAL.
    SELECT * FROM vbbe INTO TABLE lt_vbbe            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_ein
      WHERE vbeln = lt_ein-vbeln
      AND   posnr = lt_ein-posnr
      AND   etenr = lt_ein-etenr
      AND   werks = p_werk.
    gt_o_vbbe = lt_vbbe.
  ENDIF.

  LOOP AT lt_vbak INTO DATA(ls_vbak) WHERE kunnr IS NOT INITIAL.
    APPEND VALUE #( kunnr = ls_vbak-kunnr ) TO lt_kunnr.
  ENDLOOP.
  SORT lt_kunnr BY kunnr.
  DELETE ADJACENT DUPLICATES FROM lt_kunnr COMPARING kunnr.
  IF lt_kunnr IS NOT INITIAL.
    SELECT * FROM kna1 INTO TABLE lt_kna1            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_kunnr
      WHERE kunnr = lt_kunnr-kunnr.
    gt_o_kna1 = lt_kna1.
  ENDIF.

  LOOP AT lt_vbap INTO DATA(ls_vbap) WHERE matnr IS NOT INITIAL.
    APPEND VALUE #( matnr = ls_vbap-matnr ) TO lt_matnr.
  ENDLOOP.
  SORT lt_matnr BY matnr.
  DELETE ADJACENT DUPLICATES FROM lt_matnr COMPARING matnr.
  IF lt_matnr IS NOT INITIAL.
    SELECT * FROM mara INTO TABLE lt_mara            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_matnr
      WHERE matnr = lt_matnr-matnr.
    gt_o_mara = lt_mara.

    SELECT * FROM makt INTO TABLE lt_makt            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_matnr
      WHERE matnr = lt_matnr-matnr
      AND   spras = sy-langu.
    gt_o_makt = lt_makt.

    SELECT matnr dzeit dispo FROM marc INTO TABLE lt_marc
      FOR ALL ENTRIES IN lt_matnr
      WHERE matnr = lt_matnr-matnr
      AND   werks = p_werk.
    gt_o_marc = lt_marc.

    " Bewertungsbereich = Werk der Position; der VBAP-Puffer hat nur Werk p_werk
    SELECT matnr bwkey bwtar stprs FROM mbew INTO TABLE lt_mbew
      FOR ALL ENTRIES IN lt_matnr
      WHERE matnr = lt_matnr-matnr
      AND   bwkey = p_werk.
    SORT lt_mbew BY matnr bwkey bwtar.
    gv_n_mbew_mehrfach = lines( lt_mbew ).
    DELETE ADJACENT DUPLICATES FROM lt_mbew COMPARING matnr bwkey.
    " Anzahl Materialien mit mehr als einer MBEW-Zeile (getrennte Bewertung): dort war
    " SELECT SINGLE ohne BWTAR nicht eindeutig. 0 heisst: kein Unterschied moeglich.
    gv_n_mbew_mehrfach = gv_n_mbew_mehrfach - lines( lt_mbew ).
    gt_o_mbew = lt_mbew.
  ENDIF.

  LOOP AT lt_afpo INTO DATA(ls_afpo).
    APPEND VALUE #( aufnr = ls_afpo-aufnr ) TO lt_aufnr.
  ENDLOOP.
  SORT lt_aufnr BY aufnr.
  DELETE ADJACENT DUPLICATES FROM lt_aufnr COMPARING aufnr.
  IF lt_aufnr IS NOT INITIAL.
    SELECT * FROM afko INTO TABLE lt_afko            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_aufnr
      WHERE aufnr = lt_aufnr-aufnr.
    gt_o_afko = lt_afko.

    SELECT * FROM aufk INTO TABLE lt_aufk            "#EC CI_ALL_FIELDS_NEEDED
      FOR ALL ENTRIES IN lt_aufnr
      WHERE aufnr = lt_aufnr-aufnr
      AND   werks = p_werk.
    gt_o_aufk = lt_aufk.

    " erster Bezug: bisher SELECT * FROM aufm ... ORDER BY bldat, erste Zeile = kleinstes BLDAT
    SELECT aufnr bldat FROM aufm INTO TABLE lt_aufm
      FOR ALL ENTRIES IN lt_aufnr
      WHERE aufnr = lt_aufnr-aufnr
      AND   werks = p_werk
      AND   shkzg = 'H'.
    SORT lt_aufm BY aufnr bldat.
    DELETE ADJACENT DUPLICATES FROM lt_aufm COMPARING aufnr.
    gt_o_aufm = lt_aufm.
  ENDIF.

  GET TIME STAMP FIELD lv_ts1.
  gv_t_prefetch = cl_abap_tstmp=>subtract( tstmp1 = lv_ts1 tstmp2 = lv_ts0 ).
ENDFORM.                    " prefetch_opt




*----------------------------------------------------------------------*
* Form CLEAR_ZPOWERBI_KDAUF
*----------------------------------------------------------------------*
FORM clear_zpowerbi_kdauf.
  DELETE FROM zpowerbi_kdauf. " Keine WHERE-Bedingung, löscht alle Einträge
  IF sy-subrc = 0.
    MESSAGE 'Alle alten Einträge in ZPOWERBI_KDAUF gelöscht.' TYPE 'S'.
  ELSEIF sy-subrc = 4. " Heißt i.d.R. "keine Daten gefunden zum Löschen"
    MESSAGE 'Keine Einträge in ZPOWERBI_KDAUF zum Löschen vorhanden.' TYPE 'S'.
  ELSE.
    MESSAGE |Fehler (SY-SUBRC: { sy-subrc }) beim Löschen aus ZPOWERBI_KDAUF.| TYPE 'E'.
  ENDIF.
ENDFORM.                    "clear_zpowerbi_kdauf




*----------------------------------------------------------------------*
* Form WRITE_TO_ZPOWERBI_KDAUF
*----------------------------------------------------------------------*
FORM write_to_zpowerbi_kdauf.
  DATA: lt_zpowerbi_kdauf TYPE STANDARD TABLE OF zpowerbi_kdauf,
        ls_zpowerbi_kdauf TYPE zpowerbi_kdauf,
        lv_next_id        TYPE zpowerbi_kdauf-id. " Typ des neuen ID-Feldes

  " 1. Alte Daten aus ZPOWERBI_KDAUF löschen
  PERFORM clear_zpowerbi_kdauf.
  IF sy-subrc <> 0 AND sy-subrc <> 4. " Fehler beim Löschen, außer "nicht gefunden"
    RETURN.
  ENDIF.

  " 2. Daten aus gt_uebauf in die neue Struktur mappen und ID generieren
  LOOP AT gt_uebauf INTO DATA(ls_uebauf).
    CLEAR: ls_zpowerbi_kdauf, lv_next_id.

    " Nächste ID aus Nummernkreis holen
    CALL FUNCTION 'NUMBER_GET_NEXT'
      EXPORTING
        nr_range_nr = gc_nr_range_kdaufid
        object      = gc_nr_object_kdaufid
      IMPORTING
        number      = lv_next_id
      EXCEPTIONS
        interval_not_found        = 1
        number_range_not_intern   = 2
        object_not_found          = 3
        quantity_is_0             = 4
        quantity_is_not_1         = 5
        interval_overflow         = 6
        buffer_overflow           = 7
        OTHERS                    = 8.

    IF sy-subrc <> 0.
      MESSAGE |Fehler beim Abrufen der nächsten ID für ZPOWERBI_KDAUF (Obj: { gc_nr_object_kdaufid } Int: { gc_nr_range_kdaufid }). SY-SUBRC: { sy-subrc }| TYPE 'E'.
      " Hier entscheiden, ob abgebrochen oder mit Fehler fortgefahren werden soll.
      " Fürs Erste brechen wir den Loop ab, um nicht unvollständige Daten zu schreiben.
      ROLLBACK WORK. " Wichtig, um ggf. schon gezogene Nummern zurückzugeben (wenn Pufferung aktiv)
      RETURN.
    ENDIF.

    ls_zpowerbi_kdauf-id     = lv_next_id. " NEU: Generierte ID
    ls_zpowerbi_kdauf-ketdat = ls_uebauf-ketdat.
    ls_zpowerbi_kdauf-vbeln  = ls_uebauf-vbeln.
    ls_zpowerbi_kdauf-posnr  = ls_uebauf-posnr.
    ls_zpowerbi_kdauf-matnr  = ls_uebauf-matnr.
    ls_zpowerbi_kdauf-fkdat  = ls_uebauf-fkdat.
    ls_zpowerbi_kdauf-kunnr  = ls_uebauf-kunnr.
    ls_zpowerbi_kdauf-gltrp  = ls_uebauf-gltrp.
    ls_zpowerbi_kdauf-gltri  = ls_uebauf-gltri.
    ls_zpowerbi_kdauf-edatu  = ls_uebauf-edatu.
    " MANDT wird automatisch gefüllt

    APPEND ls_zpowerbi_kdauf TO lt_zpowerbi_kdauf.
  ENDLOOP.

  " 3. Neue Daten einfügen, falls vorhanden
  IF lt_zpowerbi_kdauf IS NOT INITIAL.
    TRY.
        INSERT zpowerbi_kdauf FROM TABLE lt_zpowerbi_kdauf.
        IF sy-subrc = 0.
          MESSAGE |{ lines( lt_zpowerbi_kdauf ) } Einträge erfolgreich in ZPOWERBI_KDAUF geschrieben.| TYPE 'S'.
          COMMIT WORK. " Nummernkreis-Fortschreibung und INSERT bestätigen
        ELSE.
          MESSAGE |Fehler (SY-SUBRC: { sy-subrc }) beim Schreiben in ZPOWERBI_KDAUF. ROLLBACK.| TYPE 'E'.
          ROLLBACK WORK. " Änderungen rückgängig machen
        ENDIF.
      CATCH cx_sy_open_sql_db INTO DATA(lx_sql_error).
        MESSAGE |Datenbankfehler beim Zugriff auf ZPOWERBI_KDAUF: { lx_sql_error->get_text( ) }. ROLLBACK.| TYPE 'E'.
        ROLLBACK WORK.
    ENDTRY.
  ELSE.
    MESSAGE 'Keine Daten zum Schreiben in ZPOWERBI_KDAUF vorhanden (nach Mapping).' TYPE 'S'.
    " COMMIT WORK hier nicht nötig, da nichts geändert wurde, aber auch nicht schädlich.
  ENDIF.
ENDFORM.                    "write_to_zpowerbi_kdauf






*&---------------------------------------------------------------------*
*&      Form  bilden_selwochen
*&---------------------------------------------------------------------*
FORM bilden_selwochen.
  REFRESH r_week1.
  r_week1[] = s_mbdat[].
  IF NOT s_woche1[] IS INITIAL OR
    NOT s_monat[] IS INITIAL.                             .
    LOOP AT s_woche1.
      IF s_woche1-low IS INITIAL AND
        s_woche1-option = 'BT'.
        s_woche1-low = '0101'.
      ENDIF.

      IF s_woche1-option = 'EQ'.
        PERFORM wochentag USING s_woche1-low r_week1-low '0'.
        PERFORM wochentag USING s_woche1-low r_week1-high '7'.
      ENDIF.
      IF s_woche1-option = 'BT'.
        PERFORM wochentag USING s_woche1-low r_week1-low '0'.
        PERFORM wochentag USING s_woche1-high r_week1-high '7'.
      ENDIF.
      r_week1-sign = s_woche1-sign.
      r_week1-option = 'BT'.
      APPEND r_week1.
    ENDLOOP.

    LOOP AT s_monat.
      IF s_monat-low IS INITIAL AND
        s_monat-option = 'BT'.
        s_monat-low = '0101'.
      ENDIF.

      IF s_monat-option = 'EQ'.
        PERFORM monat USING s_monat-low r_week1-low '01'.
        PERFORM monat USING s_monat-low r_week1-high '31'.
      ENDIF.
      IF s_monat-option = 'BT'.
        PERFORM monat USING s_monat-low r_week1-low '01'.
        PERFORM monat USING s_monat-high r_week1-high '31'.
      ENDIF.
      r_week1-sign = s_monat-sign.
      r_week1-option = 'BT'.
      APPEND r_week1.
    ENDLOOP.
  ENDIF.

  REFRESH r_week2.
  r_week2[] = s_gltrp[].
  IF NOT s_woche2[] IS INITIAL.
    LOOP AT s_woche2.
      IF s_woche2-low IS INITIAL AND
        s_woche2-option = 'BT'.
        s_woche2-low = '0101'.
      ENDIF.

      IF s_woche2-option = 'EQ'.
        PERFORM wochentag USING s_woche2-low r_week2-low '0'.
        PERFORM wochentag USING s_woche2-low r_week2-high '7'.
      ENDIF.
      IF s_woche2-option = 'BT'.
        PERFORM wochentag USING s_woche2-low r_week2-low '0'.
        PERFORM wochentag USING s_woche2-high r_week2-high '7'.
      ENDIF.
      r_week2-sign = s_woche2-sign.
      r_week2-option = 'BT'.
      APPEND r_week2.
    ENDLOOP.
  ENDIF.
ENDFORM.                    " bilden_selwochen

*&---------------------------------------------------------------------*
*&      Form  wochentag
*&---------------------------------------------------------------------*
FORM wochentag USING w_in w_out anztag.

  DATA: xw_in(5).

  xw_in = w_in.
  xweek = sy-datum.
  xweek+4(2) = xw_in+0(2).
  IF xw_in+4(1) = ' '.
    xweek+2(2) = xw_in+2(2).
  ELSE.
    xweek+2(2) = xw_in+3(2).
  ENDIF.

  CALL FUNCTION 'WEEK_GET_FIRST_DAY'
    EXPORTING
      week         = xweek
    IMPORTING
      date         = xdatum
    EXCEPTIONS
      week_invalid = 1
      OTHERS       = 2.

  IF sy-subrc = 0.
    xdatum = xdatum + anztag.
    w_out = xdatum.
  ENDIF.

ENDFORM.                    " wochentag

*&---------------------------------------------------------------------*
*&      Form  monat
*&---------------------------------------------------------------------*
FORM monat USING m_in m_out m_tag.

  DATA: xm_in(7).

  xm_in = m_in.
  REPLACE '/' WITH ' ' INTO xm_in.
  CONDENSE xm_in NO-GAPS.
  xdatum+4(2) = xm_in(2).
  xdatum+6(2) = m_tag.
  IF xm_in+4(2) = '  '.
    xdatum(2) = '20'.
    xdatum+2(2) = xm_in+2(2).
  ELSE.
    xdatum+0(4) = xm_in+2(4).
  ENDIF.

  xdatum = xdatum + 1.
  xdatum = xdatum - 1.
  m_out = xdatum.
ENDFORM.                    " monat

*---------------------------------------------------------------------*
*       FORM bereich                                                  *
*---------------------------------------------------------------------*
* OPT 2026-10-12: Ergebnis je Material zwischengespeichert. Z_BASISPREISLISTE
* bekommt nur das Material, MARA-ZZTYP haengt ebenfalls nur daran. N wird wie
* bisher nur auf dem einen Weg unten gesetzt, sonst bleibt es unveraendert.
FORM bereich USING n.
  DATA: ls_o_bereich TYPE ty_o_bereich.

  READ TABLE gt_o_bereich INTO ls_o_bereich WITH TABLE KEY matnr = vbap-matnr.
  IF sy-subrc <> 0.
    CLEAR ls_o_bereich.
    ls_o_bereich-matnr = vbap-matnr.
    PERFORM bereich_rechnen CHANGING ls_o_bereich.
    INSERT ls_o_bereich INTO TABLE gt_o_bereich.
  ENDIF.
  IF ls_o_bereich-gesetzt = abap_true.
    n = ls_o_bereich-wert.
  ENDIF.
ENDFORM.                    "bereich

* Bisheriger Inhalt von BEREICH, unveraendert bis auf das Ziel der Zuweisung.
FORM bereich_rechnen CHANGING cs_bereich TYPE ty_o_bereich.
  REFRESH bptab.
  CALL FUNCTION 'Z_BASISPREISLISTE'
    EXPORTING
      i_matnr   = vbap-matnr
      i_mit_loe = 'X'
    TABLES
      bptab     = bptab
    EXCEPTIONS
      no_object = 1
      no_typcd  = 2
      OTHERS    = 3.

  IF mara-zztyp(2) = '87'.             "kein Range !!!
    EXIT.
  ENDIF.

  IF mara-zztyp(1) = '8'.
    READ TABLE bptab INDEX 2.
  ELSE.
    READ TABLE bptab INDEX 3.
  ENDIF.

  IF sy-subrc = 0.
    WHILE bptab-text NE space.
      IF bptab-text(1) CO '+-0123456789'.
        EXIT.
      ENDIF.
      SHIFT bptab-text.
    ENDWHILE.
    IF bptab-text(1) CO '+-0123456789'.
      cs_bereich-wert    = bptab-text.
      cs_bereich-gesetzt = abap_true.
      EXIT.
    ENDIF.
  ENDIF.
ENDFORM.                    "bereich_rechnen

*&---------------------------------------------------------------------*
*&      Form  ERSTER_BEZUG
*&---------------------------------------------------------------------*
* OPT 2026-10-12: war SELECT * FROM aufm ... ORDER BY bldat, erste Zeile.
* Das ist das kleinste BLDAT je Auftrag; PREFETCH_OPT liest es einmal vorab.
FORM erster_bezug .
  READ TABLE gt_o_aufm INTO DATA(ls_o_aufm) WITH TABLE KEY aufnr = iafpo-aufnr.
  IF sy-subrc = 0.
    gt_uebauf-zzmbdat = ls_o_aufm-bldat.
  ENDIF.
ENDFORM.                    " ERSTER_BEZUG


*&---------------------------------------------------------------------*
*&      Form  E01_FIELDCAT_INIT
*&---------------------------------------------------------------------*
FORM e01_fieldcat_init USING e01_lt_fieldcat TYPE slis_t_fieldcat_alv.
  DATA: ls_fieldcat TYPE slis_fieldcat_alv.

  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'BMENG'.
  ls_fieldcat-no_zero  = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.

  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'NETWR1'.
  ls_fieldcat-seltext_l   = 'Netto'.  " Hier ändern
  ls_fieldcat-seltext_m   = 'Nett '.  " Hier ändern
  ls_fieldcat-seltext_s   = 'Netto'.  " Hier ändern
  ls_fieldcat-no_zero  = 'X'.
  ls_fieldcat-do_sum = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.
  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'NETWR'.
  ls_fieldcat-seltext_l   = 'Netto'.  " Hier ändern
  ls_fieldcat-seltext_m   = 'Nett '.  " Hier ändern
  ls_fieldcat-seltext_s   = 'Netto'.  " Hier ändern
  ls_fieldcat-no_zero  = 'X'.
  ls_fieldcat-do_sum = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.
  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'OMENG'.
  ls_fieldcat-seltext_l   = 'Alt.Menge '.  " Hier ändern
  ls_fieldcat-seltext_m   = 'A.Menge '.  " Hier ändern
  ls_fieldcat-seltext_s   = 'Alt.Menge '.  " Hier ändern
  ls_fieldcat-no_zero  = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.



  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'NAME1'.
  ls_fieldcat-seltext_l = 'Debitorenname'.
  ls_fieldcat-seltext_m = 'Deb.Name'.
  ls_fieldcat-seltext_s = 'Name'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.



  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'STANDARDWERT'.
  ls_fieldcat-seltext_l = 'Standardwert'.
  ls_fieldcat-seltext_m = 'Std.wert'.
  ls_fieldcat-seltext_s = 'StdW'.
  ls_fieldcat-no_zero = 'X'.
  ls_fieldcat-do_sum = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.





  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'ABGRU'.
  ls_fieldcat-seltext_l   = 'Absagegrund'.  " Hier ändern
  ls_fieldcat-seltext_m   = 'A.Grund'.  " Hier ändern
  ls_fieldcat-seltext_s   = 'A.GR'.  " Hier ändern
  ls_fieldcat-no_zero  = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.

  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'DISPO_DES'.
  ls_fieldcat-seltext_l   = 'DISPOGRUPPE'.  " Hier ändern
  ls_fieldcat-seltext_m   = 'D.Gruppe'.  " Hier ändern
  ls_fieldcat-seltext_s   = 'D.GRP'.  " Hier ändern
  ls_fieldcat-no_zero  = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.
  CLEAR ls_fieldcat.


  ls_fieldcat-fieldname = 'OMENGN'.
  ls_fieldcat-seltext_l   = 'Neu.Menge '.  " Hier ändern
  ls_fieldcat-seltext_m   = 'N.Menge'.  " Hier ändern
  ls_fieldcat-seltext_s   = 'N.Meg'.  " Hier ändern
  ls_fieldcat-no_zero  = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.






  "Std Preis Feld
  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'STANDARDPREIS'.
  ls_fieldcat-seltext_l = 'Standardpreis'.
  ls_fieldcat-seltext_m = 'Std.preis'.
  ls_fieldcat-seltext_s = 'Std.pr'.
  ls_fieldcat-no_zero   = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.

  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'ANTEIL'.
  ls_fieldcat-seltext_l = 'Anteil'.
  ls_fieldcat-seltext_m = 'Anteil'.
  ls_fieldcat-seltext_s = 'Anteil'.
  ls_fieldcat-no_zero   = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.





  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'NETWR2'.
  ls_fieldcat-no_zero  = 'X'.
  ls_fieldcat-seltext_l   = 'Alt.Nettowert'.  " Hier ändern
  ls_fieldcat-seltext_m   = 'A.Nettow'.  " Hier ändern
  ls_fieldcat-seltext_s   = 'A.Nettow'.  " Hier ändern
  APPEND ls_fieldcat TO e01_lt_fieldcat.
  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'NETWR3N'.
  ls_fieldcat-no_zero  = 'X'.
  ls_fieldcat-seltext_l   = 'Neu.Nettowert'.  " Hier ändern
  ls_fieldcat-seltext_m   = 'n.Nettow'.  " Hier ändern
  ls_fieldcat-seltext_s   = 'n.Nettow'.  " Hier ändern
  APPEND ls_fieldcat TO e01_lt_fieldcat.
  CLEAR ls_fieldcat.

  ls_fieldcat-fieldname = 'PSMNG'.
  ls_fieldcat-no_zero  = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.
ENDFORM.
" E01_FIELDCAT_INIT
FORM get_standard_price USING    p_matnr TYPE matnr
                                 p_werks TYPE werks_d
                        CHANGING p_stdprice TYPE p.
  DATA: lv_bwkey TYPE bwkey.

  " Bewertungsbereich ist in diesem Fall gleich dem Werk
  lv_bwkey = p_werks.

  " Standardpreis aus MBEW holen
  " OPT 2026-10-12: war SELECT SINGLE stprs INTO p_stdprice FROM mbew WHERE matnr bwkey;
  " nicht gefunden -> P_STDPRICE bleibt wie bisher unveraendert
  READ TABLE gt_o_mbew INTO DATA(ls_o_mbew) WITH TABLE KEY matnr = p_matnr bwkey = lv_bwkey.
  IF sy-subrc = 0.
    p_stdprice = ls_o_mbew-stprs.
  ENDIF.
ENDFORM.


FORM fill_fkdat USING p_vbeln TYPE vbeln
                       p_posnr TYPE posnr
                       p_menge TYPE vbrp-fkimg
                CHANGING ct_uebauf TYPE zmm_ueb_kdauf.
  DATA: lt_vbfa       TYPE TABLE OF vbfa,
        ls_vbfa       TYPE vbfa,
        lt_vbrk       TYPE TABLE OF vbrk,
        ls_vbrk       TYPE vbrk,
        lv_cum_qty    TYPE vbrp-fkimg,
        lv_menge      TYPE vbrp-fkimg,
        lv_cnt        TYPE i,
        lv_needed_qty TYPE vbrp-fkimg,
        lv_leaveall   TYPE bool,
        lv_date       TYPE vbrk-fkdat.


  IF p_vbeln <> gv_lastvbeln.
    gv_cursor = 0.
  ENDIF.
  IF p_posnr <> gv_lastpos.
    " Counter reseten , neuer beleg beginn von oben
    gv_cursor = 0.
  ENDIF.
  " OPT 2026-10-12: Belegfluss je Position nur einmal lesen. IVBEP ist nach VBELN/POSNR
  " sortiert, die Einteilungen einer Position kommen also direkt hintereinander; dann wird
  " das unveraenderte Ergebnis des Bausteins vom Aufruf davor wiederverwendet.
  " Hat der Beleg gar keinen Belegfluss (kein VBFA-Satz als Vorgaenger oder Nachfolger), kann
  " der Baustein keine Fakturazeile (VBTYP_N = 'M') liefern; die Schleife unten taete nichts.
  READ TABLE gt_o_flow_beleg WITH TABLE KEY table_line = p_vbeln TRANSPORTING NO FIELDS.
  IF sy-subrc <> 0.
    CLEAR lt_vbfa.
    gv_n_flow_ohne = gv_n_flow_ohne + 1.
  ELSEIF gv_o_flow_da = abap_true AND gv_o_flow_vbeln = p_vbeln AND gv_o_flow_posnr = p_posnr.
    lt_vbfa = gt_o_flow_vbfa.
  ELSE.
    gv_n_flow_fm = gv_n_flow_fm + 1.
    DATA(lv_vbco6) = VALUE vbco6( vbeln = p_vbeln  posnr =  p_posnr ).
    CALL FUNCTION 'RV_ORDER_FLOW_INFORMATION'        "#EC CI_USAGE_OK[2198647]
      EXPORTING
        comwa         = lv_vbco6
      " BELEGTYP      = 'M'
      TABLES
        vbfa_tab      = lt_vbfa
      EXCEPTIONS
        no_vbfa       = 1
        no_vbuk_found = 2
        OTHERS        = 3.
    gt_o_flow_vbfa  = lt_vbfa.
    gv_o_flow_vbeln = p_vbeln.
    gv_o_flow_posnr = p_posnr.
    gv_o_flow_da    = abap_true.
  ENDIF.
  CLEAR lv_date.
  CLEAR lv_menge.
  SORT lt_vbfa BY erdat ASCENDING.
  " Vorfiltern der Tabelle lt_vbfa für Einträge mit vbtyp_n = 'M'
  DELETE lt_vbfa WHERE vbtyp_n <> 'M'.
  IF lt_vbfa IS INITIAL.
    gv_n_flow_leer = gv_n_flow_leer + 1.
  ENDIF.
  " Wenn nur eine Auslieferung stattgefunden hat kann diese Rechnung das erste mal die volle Menge enthalten.
  LOOP AT lt_vbfa INTO DATA(ls_vbkr_ext).
    CLEAR lv_cum_qty.
    " die erstel 2 überspringen weil obeen schon 2 abgezogen wurden und enddatum ermitteln
    lv_cnt = lv_cnt + 1.
    IF ( lv_cnt > gv_cursor ) OR   (  lines( lt_vbfa ) = 1 ).
      lv_menge = lv_menge + ls_vbkr_ext-rfmng.
      IF ( lv_menge  = p_menge ) OR (  lines( lt_vbfa ) = 1 )  . "menge erreicht, datum wegschreiben cursor in index
        gv_cursor =  lv_cnt.
        ct_uebauf-fkdat = ls_vbkr_ext-erdat.
        lv_leaveall = abap_true.
        gv_lastpos =  p_posnr.
        gv_lastvbeln =  p_vbeln.
        EXIT.
      ENDIF.
    ENDIF.
    IF ( lv_leaveall = abap_true ).
      EXIT.
    ENDIF.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*&      Form  VERGLEICH_EXPORT
*&---------------------------------------------------------------------*
* Nur im Vergleichslauf (P_VGL): Ergebnis und Laufzeiten ins ABAP-Memory,
* dann Programmende ohne ALV und ohne Fortschreibung von ZPOWERBI_KDAUF.
FORM vergleich_export.
  CHECK p_vgl = 'X'.
  GET TIME STAMP FIELD gv_ts_a.
  gv_t_uebauf = cl_abap_tstmp=>subtract( tstmp1 = gv_ts_a tstmp2 = gv_ts_b ).
  gv_t_gesamt = cl_abap_tstmp=>subtract( tstmp1 = gv_ts_a tstmp2 = gv_ts_start ).
  EXPORT tab        = gt_uebauf[]
         n_ivbep    = gv_n_ivbep
         t_gesamt   = gv_t_gesamt
         t_ivbep    = gv_t_ivbep
         t_uebauf   = gv_t_uebauf
         t_prefetch = gv_t_prefetch
         t_bereich  = gv_t_bereich
         t_fkdat    = gv_t_fkdat
         t_bezug    = gv_t_bezug
         t_dispo    = gv_t_dispo
         t_conv     = gv_t_conv
         n_mbew     = gv_n_mbew_mehrfach
         n_flow_fm  = gv_n_flow_fm
         n_flow_ohne = gv_n_flow_ohne
         n_flow_leer = gv_n_flow_leer
    TO MEMORY ID 'ZM_OFFENE_KDAUF_VGL'.
  LEAVE PROGRAM.
ENDFORM.                    "vergleich_export
