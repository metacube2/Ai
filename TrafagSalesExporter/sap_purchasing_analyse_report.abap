REPORT z_purchasing_analyse.

" ============================================================================
" Analyse-/Profiling-Report fuer das Einkaufsdashboard (BiDashboard)
" ----------------------------------------------------------------------------
" Zweck: Beantwortet in EINEM Lauf alle offenen Daten-/Metadatenfragen aus
"        docs/PURCHASING_DASHBOARD_VORBEREITUNG_INGO_2026-07-09.md, damit die
"        Weiterentwicklung ohne OData-Proben und ohne manuelle CSV-Exporte
"        weitergehen kann. Liest die SAP-Tabellen DIREKT (nicht ueber OData).
"
" Bedienung:
"   1. SE38 -> Programm Z_PURCHASING_ANALYSE anlegen -> diesen Code einfuegen.
"   2. Ausfuehren (F8). Parameter siehe Selektionsbild.
"   3. Die WRITE-Ausgabe komplett markieren und an Ingo/Analytics zurueckgeben.
"   4. Optional p_dl setzen: schreibt die grossen Referenzlisten zusaetzlich als
"      CSV-Dateien auf den Frontend-PC (nur in SAP GUI, nicht im Hintergrund).
"
" Kontext 2026-07-09: Der OData-Service ZPOWERBI_EINKAUF_SRV exponiert bereits
"   MARC, MBEW, EKBE, LFA1, QM (qmel/qmfe/...), EKAB, MDBS, MSEG usw. Diese
"   Objekte sind also NICHT das Problem. Die echten OData-Luecken sind die
"   Text-Tabellen T023T (Warengruppe) und T024D (Disponent) sowie RESB. Dieser
"   Report liefert genau diese beiden Text-CSVs plus die Datenprofilierung
"   (Waehrung/Konnr/Elikz/Dispo-Fuellgrad/MBEW/EKBE), damit die Dashboard-Logik
"   ohne SAP-Zugriff korrekt gebaut werden kann.
"
" Voraussetzung: NetWeaver 7.50+ (Inline-@DATA mit Aggregatfunktionen).
" Nur lesend. Keine Aenderung am System.
" ============================================================================

TABLES: ekko.

PARAMETERS:
  p_bedat TYPE ekko-bedat DEFAULT '20200101',   " Einkauf ab Bestelldatum
  p_spras TYPE spras      DEFAULT sy-langu,      " Sprache fuer Texte (Warengruppe/Disponent)
  p_smpl  TYPE i          DEFAULT 15,            " Beispielzeilen je Detailblock
  p_csv   TYPE i          DEFAULT 500.           " max. CSV-Zeilen in der WRITE-Ausgabe

SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE t_opt.
PARAMETERS:
  p_val AS CHECKBOX DEFAULT 'X',                 " offene Werte aufsummieren (schwer)
  p_dl  AS CHECKBOX DEFAULT ' ',                 " Referenzlisten als CSV herunterladen
  p_path TYPE string DEFAULT 'C:\temp\'.         " Zielordner fuer den Download
SELECTION-SCREEN END OF BLOCK b1.

INITIALIZATION.
  t_opt = 'Optionen'.

START-OF-SELECTION.

  WRITE: / '############################################################'.
  WRITE: / '# Einkaufsdashboard Analyse-Report'.
  WRITE: / '# System / Mandant:', sy-sysid, sy-mandt.
  WRITE: / '# Datum / Uhrzeit  :', sy-datum, sy-uzeit.
  WRITE: / '# Einkauf ab BEDAT :', p_bedat.
  WRITE: / '# Sprache Texte    :', p_spras.
  WRITE: / '############################################################'.

" ----------------------------------------------------------------------------
" 1) EKKO-Profiling: Waehrung (K1), Konnr (K4), Bstyp/Bsart (K4/Umlagerung)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 1) EKKO-PROFILING (Kopf) ==='.

  SELECT COUNT(*) FROM ekko WHERE bedat >= @p_bedat INTO @DATA(lv_ekko_total).
  WRITE: / 'EKKO-Belege ab BEDAT:', lv_ekko_total.

  " 1a) Waehrungsverteilung -> K1 (Spend CHF summiert Belegwaehrungen?)
  WRITE: / '--- 1a) Waehrungsverteilung (WAERS) [K1] ---'.
  SELECT waers, COUNT(*) AS cnt
    FROM ekko
    WHERE bedat >= @p_bedat
    GROUP BY waers
    INTO TABLE @DATA(lt_waers).
  SORT lt_waers BY cnt DESCENDING.
  LOOP AT lt_waers INTO DATA(ls_waers).
    WRITE: / '  WAERS=', ls_waers-waers, 'Belege=', ls_waers-cnt.
  ENDLOOP.

  " 1b) Buchungskreis x Waehrung -> Hauswaehrung je Bukrs klaeren (K1)
  WRITE: / '--- 1b) Buchungskreis x Waehrung (BUKRS/WAERS) [K1] ---'.
  SELECT bukrs, waers, COUNT(*) AS cnt
    FROM ekko
    WHERE bedat >= @p_bedat
    GROUP BY bukrs, waers
    INTO TABLE @DATA(lt_buk).
  SORT lt_buk BY cnt DESCENDING.
  LOOP AT lt_buk INTO DATA(ls_buk).
    WRITE: / '  BUKRS=', ls_buk-bukrs, 'WAERS=', ls_buk-waers, 'Belege=', ls_buk-cnt.
  ENDLOOP.

  " 1c) Wechselkurs-Stichprobe fuer Fremdwaehrung -> WKURS-Richtung (K1)
  WRITE: / '--- 1c) WKURS-Stichprobe Fremdwaehrung (Vorzeichen pruefen) [K1] ---'.
  SELECT ebeln, waers, wkurs
    FROM ekko
    WHERE bedat >= @p_bedat
      AND waers <> 'CHF' AND waers <> ''
    ORDER BY waers
    INTO TABLE @DATA(lt_fx)
    UP TO @p_smpl ROWS.
  IF lt_fx IS INITIAL.
    WRITE: / '  Keine Fremdwaehrungsbelege ab BEDAT -> K1 aktuell wirkungslos (alles CHF).'.
  ELSE.
    LOOP AT lt_fx INTO DATA(ls_fx).
      WRITE: / '  EBELN=', ls_fx-ebeln, 'WAERS=', ls_fx-waers, 'WKURS=', ls_fx-wkurs.
    ENDLOOP.
    WRITE: / '  Hinweis: WKURS > 0 => multiplizieren, WKURS < 0 => dividieren (indirekt).'.
  ENDIF.

  " 1d) Konnr (Rahmenkontrakt-Bezug) -> K4
  SELECT COUNT(*) FROM ekko
    WHERE bedat >= @p_bedat AND konnr <> ''
    INTO @DATA(lv_konnr).
  WRITE: / '--- 1d) Belege mit KONNR (Abruf zu Rahmenkontrakt) [K4]:', lv_konnr, 'von', lv_ekko_total.

  " 1e) Bstyp (F=Bestellung, K=Kontrakt, L=Lieferplan) -> K4-Abgrenzung
  WRITE: / '--- 1e) Belegtyp-Verteilung (BSTYP) [K4] ---'.
  SELECT bstyp, COUNT(*) AS cnt
    FROM ekko
    WHERE bedat >= @p_bedat
    GROUP BY bstyp
    INTO TABLE @DATA(lt_bstyp).
  SORT lt_bstyp BY cnt DESCENDING.
  LOOP AT lt_bstyp INTO DATA(ls_bstyp).
    WRITE: / '  BSTYP=', ls_bstyp-bstyp, 'Belege=', ls_bstyp-cnt,
             '(F=Bestellung K=Kontrakt L=Lieferplan)'.
  ENDLOOP.

  " 1f) Bsart (Belegart, u.a. UB-Umlagerung) -> K4-Zusatz / Spend-Bereinigung
  WRITE: / '--- 1f) Belegart-Verteilung (BSART, Top nach Anzahl) [K4/Umlagerung] ---'.
  SELECT bsart, COUNT(*) AS cnt
    FROM ekko
    WHERE bedat >= @p_bedat
    GROUP BY bsart
    INTO TABLE @DATA(lt_bsart).
  SORT lt_bsart BY cnt DESCENDING.
  DATA(lv_i) = 0.
  LOOP AT lt_bsart INTO DATA(ls_bsart).
    lv_i = lv_i + 1.
    IF lv_i > 30. EXIT. ENDIF.
    WRITE: / '  BSART=', ls_bsart-bsart, 'Belege=', ls_bsart-cnt.
  ENDLOOP.

" ----------------------------------------------------------------------------
" 2) EKPO-Profiling: Elikz (M7), Datenqualitaet
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 2) EKPO-PROFILING (Position) ==='.

  " 2a) Endlieferungskennzeichen bei OFFENEN Einteilungen -> M7
  WRITE: / '--- 2a) ELIKZ bei offenen Einteilungen (Menge > Wemng) [M7] ---'.
  SELECT p~elikz, COUNT(*) AS cnt
    FROM ekpo AS p
    INNER JOIN eket AS e ON e~ebeln = p~ebeln AND e~ebelp = p~ebelp
    INNER JOIN ekko AS h ON h~ebeln = p~ebeln
    WHERE h~bedat >= @p_bedat
      AND e~menge > e~wemng
    GROUP BY p~elikz
    INTO TABLE @DATA(lt_elikz).
  SORT lt_elikz BY cnt DESCENDING.
  LOOP AT lt_elikz INTO DATA(ls_elikz).
    WRITE: / '  ELIKZ=', ls_elikz-elikz, 'offene Einteilungen=', ls_elikz-cnt,
             '(X = endgeliefert -> zaehlt faelschlich als offen)'.
  ENDLOOP.

  " 2b) Datenqualitaet EKPO
  SELECT COUNT(*) FROM ekpo AS p INNER JOIN ekko AS h ON h~ebeln = p~ebeln
    WHERE h~bedat >= @p_bedat AND p~matkl = '' INTO @DATA(lv_no_matkl).
  SELECT COUNT(*) FROM ekpo AS p INNER JOIN ekko AS h ON h~ebeln = p~ebeln
    WHERE h~bedat >= @p_bedat AND p~menge = 0 INTO @DATA(lv_no_menge).
  WRITE: / '--- 2b) Datenqualitaet ---'.
  WRITE: / '  Positionen ohne Warengruppe (MATKL leer):', lv_no_matkl.
  WRITE: / '  Positionen mit MENGE = 0            :', lv_no_menge.

" ----------------------------------------------------------------------------
" 3) Offene / ueberfaellige Werte -> Abgleich 18 Mio (K2/K3/M7)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 3) OFFENE / UEBERFAELLIGE POSITIONEN ==='.

  SELECT COUNT(*) FROM eket AS e INNER JOIN ekko AS h ON h~ebeln = e~ebeln
    WHERE h~bedat >= @p_bedat AND e~menge > e~wemng AND e~eindt < @sy-datum
    INTO @DATA(lv_overdue_cnt).
  SELECT COUNT(*) FROM eket AS e INNER JOIN ekko AS h ON h~ebeln = e~ebeln
    WHERE h~bedat >= @p_bedat AND e~menge > e~wemng AND e~eindt >= @sy-datum
    INTO @DATA(lv_future_cnt).
  WRITE: / '  Offene Einteilungen ueberfaellig (Eindt < heute):', lv_overdue_cnt.
  WRITE: / '  Offene Einteilungen zukuenftig  (Eindt >= heute):', lv_future_cnt.

  IF p_val = 'X'.
    " Offenen Wert in Belegwaehrung aufsummieren (Stueckwert = netwr/menge).
    " Achtung: Summe in Belegwaehrung (CHF-Annahme bis K1 verifiziert ist).
    DATA: lv_open_val    TYPE p LENGTH 16 DECIMALS 2,
          lv_overdue_val TYPE p LENGTH 16 DECIMALS 2,
          lv_elikz_val   TYPE p LENGTH 16 DECIMALS 2,
          lv_unit        TYPE p LENGTH 16 DECIMALS 6,
          lv_openqty     TYPE p LENGTH 16 DECIMALS 3.

    SELECT p~menge AS pmenge, p~netwr AS netwr, p~elikz AS elikz,
           e~menge AS emenge, e~wemng AS wemng, e~eindt AS eindt
      FROM eket AS e
      INNER JOIN ekpo AS p ON p~ebeln = e~ebeln AND p~ebelp = e~ebelp
      INNER JOIN ekko AS h ON h~ebeln = e~ebeln
      WHERE h~bedat >= @p_bedat
        AND e~menge > e~wemng
        AND p~loekz = ''
      INTO TABLE @DATA(lt_open).

    LOOP AT lt_open INTO DATA(ls_open).
      IF ls_open-pmenge = 0.
        lv_unit = 0.
      ELSE.
        lv_unit = ls_open-netwr / ls_open-pmenge.
      ENDIF.
      lv_openqty = ls_open-emenge - ls_open-wemng.
      IF lv_openqty < 0. lv_openqty = 0. ENDIF.
      DATA(lv_line_val) = lv_openqty * lv_unit.
      lv_open_val = lv_open_val + lv_line_val.
      IF ls_open-eindt < sy-datum.
        lv_overdue_val = lv_overdue_val + lv_line_val.
      ENDIF.
      IF ls_open-elikz = 'X'.
        lv_elikz_val = lv_elikz_val + lv_line_val.
      ENDIF.
    ENDLOOP.

    WRITE: / '  Offener Wert gesamt (Belegwaehrung):', lv_open_val.
    WRITE: / '  davon ueberfaellig                 :', lv_overdue_val.
    WRITE: / '  davon auf ELIKZ=X (M7-Ueberzaehlung):', lv_elikz_val.
    WRITE: / '  Vergleich: erwarteter Offenwert ~18 Mio (Review 2026-07-08).'.
  ELSE.
    WRITE: / '  (Wertsumme uebersprungen; p_val setzen fuer 18-Mio-Abgleich.)'.
  ENDIF.

" ----------------------------------------------------------------------------
" 4) LFA1-Adresse -> Beschaffungsregion (Aufriss 5, Phase 2.1)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 4) LIEFERANTEN-ADRESSE (LFA1) [Aufriss 5] ==='.

  SELECT DISTINCT h~lifnr
    FROM ekko AS h
    WHERE h~bedat >= @p_bedat AND h~lifnr <> ''
    INTO TABLE @DATA(lt_lif).
  DATA(lv_lif_total) = lines( lt_lif ).

  DATA: lv_land1_ok TYPE i,
        lv_regio_ok TYPE i,
        lv_ort_ok   TYPE i.
  IF lt_lif IS NOT INITIAL.
    SELECT lifnr, land1, regio, ort01
      FROM lfa1
      FOR ALL ENTRIES IN @lt_lif
      WHERE lifnr = @lt_lif-lifnr
      INTO TABLE @DATA(lt_lfa1).
    LOOP AT lt_lfa1 INTO DATA(ls_lfa1).
      IF ls_lfa1-land1 <> ''. lv_land1_ok = lv_land1_ok + 1. ENDIF.
      IF ls_lfa1-regio <> ''. lv_regio_ok = lv_regio_ok + 1. ENDIF.
      IF ls_lfa1-ort01 <> ''. lv_ort_ok   = lv_ort_ok   + 1. ENDIF.
    ENDLOOP.
  ENDIF.
  WRITE: / '  Lieferanten im Zeitraum:', lv_lif_total.
  WRITE: / '  davon mit LAND1 gefuellt:', lv_land1_ok.
  WRITE: / '  davon mit REGIO gefuellt:', lv_regio_ok.
  WRITE: / '  davon mit ORT01 gefuellt:', lv_ort_ok.

  WRITE: / '--- 4a) Laenderverteilung (LAND1) ---'.
  DATA lt_land TYPE SORTED TABLE OF lfa1-land1 WITH NON-UNIQUE KEY table_line.
  LOOP AT lt_lfa1 INTO ls_lfa1.
    INSERT ls_lfa1-land1 INTO TABLE lt_land.
  ENDLOOP.
  DATA lv_prev TYPE lfa1-land1.
  DATA lv_landcnt TYPE i.
  CLEAR: lv_prev, lv_landcnt.
  LOOP AT lt_land INTO DATA(lv_land).
    IF lv_land <> lv_prev AND lv_landcnt > 0.
      WRITE: / '  LAND1=', lv_prev, 'Lieferanten=', lv_landcnt.
      lv_landcnt = 0.
    ENDIF.
    lv_prev = lv_land.
    lv_landcnt = lv_landcnt + 1.
  ENDLOOP.
  IF lv_landcnt > 0.
    WRITE: / '  LAND1=', lv_prev, 'Lieferanten=', lv_landcnt.
  ENDIF.

" ----------------------------------------------------------------------------
" 5) Warengruppen-Texte T023T -> Referenzliste (Phase 1.3, Aufriss 3)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 5) WARENGRUPPEN-TEXTE (T023T) [Phase 1.3] ==='.

  SELECT DISTINCT p~matkl
    FROM ekpo AS p
    INNER JOIN ekko AS h ON h~ebeln = p~ebeln
    WHERE h~bedat >= @p_bedat AND p~matkl <> ''
    INTO TABLE @DATA(lt_matkl).
  DATA(lv_wg_total) = lines( lt_matkl ).

  DATA: lt_wg TYPE STANDARD TABLE OF t023t.
  IF lt_matkl IS NOT INITIAL.
    SELECT matkl, wgbez
      FROM t023t
      FOR ALL ENTRIES IN @lt_matkl
      WHERE spras = @p_spras AND matkl = @lt_matkl-matkl
      INTO CORRESPONDING FIELDS OF TABLE @lt_wg.
  ENDIF.
  SORT lt_wg BY matkl.

  WRITE: / '  Warengruppen im Einkauf:', lv_wg_total, '| mit Text:', lines( lt_wg ).
  WRITE: / '--- BEGIN CSV WARENGRUPPEN ---'.
  WRITE: / 'Matkl;Wgbez'.
  DATA: lt_dl_wg TYPE STANDARD TABLE OF string,
        lv_row   TYPE string.
  APPEND 'Matkl;Wgbez' TO lt_dl_wg.
  lv_i = 0.
  LOOP AT lt_wg INTO DATA(ls_wg).
    lv_row = |{ ls_wg-matkl };{ ls_wg-wgbez }|.
    APPEND lv_row TO lt_dl_wg.
    lv_i = lv_i + 1.
    IF lv_i <= p_csv.
      WRITE: / lv_row.
    ENDIF.
  ENDLOOP.
  IF lv_i > p_csv.
    WRITE: / '  ... (in WRITE gekuerzt auf', p_csv, 'Zeilen; p_dl fuer Vollexport)'.
  ENDIF.
  WRITE: / '--- END CSV WARENGRUPPEN ---'.

" ----------------------------------------------------------------------------
" 6) Disponenten MARC / T024D -> Referenzliste (Phase 1.4, Aufriss 2)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 6) DISPONENTEN (MARC/T024D) [Phase 1.4] ==='.

  " Materialstamm-Dispo je Material/Werk aus dem Einkaufsscope
  SELECT DISTINCT p~matnr, p~werks
    FROM ekpo AS p
    INNER JOIN ekko AS h ON h~ebeln = p~ebeln
    WHERE h~bedat >= @p_bedat AND p~matnr <> ''
    INTO TABLE @DATA(lt_mw).
  DATA(lv_mw_total) = lines( lt_mw ).

  DATA: BEGIN OF ls_marc_row,
          matnr TYPE marc-matnr,
          werks TYPE marc-werks,
          dispo TYPE marc-dispo,
          eisbe TYPE marc-eisbe,
        END OF ls_marc_row.
  DATA lt_marc TYPE STANDARD TABLE OF marc.
  DATA lt_dl_disp TYPE STANDARD TABLE OF string.
  DATA lv_dispo_ok TYPE i.

  IF lt_mw IS NOT INITIAL.
    SELECT matnr, werks, dispo, eisbe
      FROM marc
      FOR ALL ENTRIES IN @lt_mw
      WHERE matnr = @lt_mw-matnr AND werks = @lt_mw-werks
      INTO CORRESPONDING FIELDS OF TABLE @lt_marc.
  ENDIF.

  " Disponentengruppen (Werk/Dispo) mit Text + Materialzahl
  DATA: BEGIN OF ls_grp,
          werks TYPE marc-werks,
          dispo TYPE marc-dispo,
          cnt   TYPE i,
        END OF ls_grp,
        lt_grp LIKE SORTED TABLE OF ls_grp WITH UNIQUE KEY werks dispo.

  LOOP AT lt_marc INTO DATA(ls_marc).
    IF ls_marc-dispo <> ''. lv_dispo_ok = lv_dispo_ok + 1. ENDIF.
    READ TABLE lt_grp INTO ls_grp WITH KEY werks = ls_marc-werks dispo = ls_marc-dispo.
    IF sy-subrc = 0.
      ls_grp-cnt = ls_grp-cnt + 1.
      MODIFY TABLE lt_grp FROM ls_grp.
    ELSE.
      ls_grp-werks = ls_marc-werks.
      ls_grp-dispo = ls_marc-dispo.
      ls_grp-cnt   = 1.
      INSERT ls_grp INTO TABLE lt_grp.
    ENDIF.
    " Vollmapping fuer Download aufbauen
    APPEND |{ ls_marc-matnr };{ ls_marc-werks };{ ls_marc-dispo };{ ls_marc-eisbe }| TO lt_dl_disp.
  ENDLOOP.
  INSERT |Matnr;Werks;Dispo;Eisbe| INTO lt_dl_disp INDEX 1.

  WRITE: / '  Material/Werk im Einkauf:', lv_mw_total, '| mit MARC-Satz:', lines( lt_marc ),
           '| mit DISPO gefuellt:', lv_dispo_ok.

  WRITE: / '--- BEGIN CSV DISPONENTENGRUPPEN ---'.
  WRITE: / 'Werks;Dispo;Bezeichnung;Materialzahl'.
  LOOP AT lt_grp INTO ls_grp.
    SELECT SINGLE dsnam FROM t024d
      WHERE werks = @ls_grp-werks AND dispo = @ls_grp-dispo
      INTO @DATA(lv_dsnam).
    IF sy-subrc <> 0. CLEAR lv_dsnam. ENDIF.
    WRITE: / |{ ls_grp-werks };{ ls_grp-dispo };{ lv_dsnam };{ ls_grp-cnt }|.
  ENDLOOP.
  WRITE: / '--- END CSV DISPONENTENGRUPPEN ---'.
  WRITE: / '  Hinweis: Vollmapping Material->Dispo nur per Download (p_dl) bzw.'.
  WRITE: / '           SE16N-Export von MARC (MATNR,WERKS,DISPO,EISBE).'.

" ----------------------------------------------------------------------------
" 7) MBEW Lagerbestand / Standardkosten (Phase 2.3)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 7) MBEW LAGER/KOSTEN [Phase 2.3] ==='.

  DATA lt_mbew TYPE STANDARD TABLE OF mbew.
  DATA: lv_stprs_ok TYPE i, lv_salk3_ok TYPE i.
  DATA lt_dl_mbew TYPE STANDARD TABLE OF string.
  APPEND |Matnr;Bwkey;Stprs;Verpr;Lbkum;Salk3| TO lt_dl_mbew.

  IF lt_mw IS NOT INITIAL.
    SELECT matnr, bwkey, stprs, verpr, lbkum, salk3
      FROM mbew
      FOR ALL ENTRIES IN @lt_mw
      WHERE matnr = @lt_mw-matnr
      INTO CORRESPONDING FIELDS OF TABLE @lt_mbew.
  ENDIF.
  lv_i = 0.
  LOOP AT lt_mbew INTO DATA(ls_mbew).
    IF ls_mbew-stprs <> 0. lv_stprs_ok = lv_stprs_ok + 1. ENDIF.
    IF ls_mbew-salk3 <> 0. lv_salk3_ok = lv_salk3_ok + 1. ENDIF.
    APPEND |{ ls_mbew-matnr };{ ls_mbew-bwkey };{ ls_mbew-stprs };{ ls_mbew-verpr };{ ls_mbew-lbkum };{ ls_mbew-salk3 }| TO lt_dl_mbew.
    lv_i = lv_i + 1.
    IF lv_i <= p_smpl.
      WRITE: / '  MATNR=', ls_mbew-matnr, 'BWKEY=', ls_mbew-bwkey,
               'STPRS=', ls_mbew-stprs, 'LBKUM=', ls_mbew-lbkum, 'SALK3=', ls_mbew-salk3.
    ENDIF.
  ENDLOOP.
  WRITE: / '  MBEW-Saetze:', lines( lt_mbew ), '| STPRS>0:', lv_stprs_ok, '| SALK3>0:', lv_salk3_ok.
  WRITE: / '  (STPRS = Standardpreis fuer Gruppenmarge; LBKUM/SALK3 = Bestand Menge/Wert.)'.

" ----------------------------------------------------------------------------
" 8) EKBE Wareneingaenge -> Liefertermintreue (Phase 3.1)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 8) EKBE WARENEINGAENGE [Phase 3.1] ==='.

  SELECT COUNT(*) FROM ekbe AS b INNER JOIN ekko AS h ON h~ebeln = b~ebeln
    WHERE h~bedat >= @p_bedat AND b~bewtp = 'E'
    INTO @DATA(lv_ekbe_cnt).
  WRITE: / '  EKBE-Wareneingangszeilen (BEWTP=E) ab BEDAT:', lv_ekbe_cnt.

  IF lv_ekbe_cnt > 0.
    SELECT b~ebeln, b~ebelp, b~budat, b~menge, b~bwart, e~eindt
      FROM ekbe AS b
      INNER JOIN ekko AS h ON h~ebeln = b~ebeln
      LEFT OUTER JOIN eket AS e ON e~ebeln = b~ebeln AND e~ebelp = b~ebelp
      WHERE h~bedat >= @p_bedat AND b~bewtp = 'E'
      ORDER BY b~ebeln, b~ebelp
      INTO TABLE @DATA(lt_ekbe)
      UP TO @p_smpl ROWS.
    LOOP AT lt_ekbe INTO DATA(ls_ekbe).
      WRITE: / '  EBELN=', ls_ekbe-ebeln, 'EBELP=', ls_ekbe-ebelp,
               'WE-BUDAT=', ls_ekbe-budat, 'PLAN-EINDT=', ls_ekbe-eindt,
               'MENGE=', ls_ekbe-menge, 'BWART=', ls_ekbe-bwart.
    ENDLOOP.
    WRITE: / '  Termintreue = Vergleich WE-BUDAT gegen PLAN-EINDT (Toleranz mit Einkauf definieren).'.
  ELSE.
    WRITE: / '  Keine EKBE-WE-Zeilen gefunden -> Termintreue nicht berechenbar.'.
  ENDIF.

" ----------------------------------------------------------------------------
" 9) Optionaler Download der Referenzlisten
" ----------------------------------------------------------------------------
  IF p_dl = 'X'.
    ULINE.
    WRITE: / '=== 9) DOWNLOAD REFERENZLISTEN ==='.
    PERFORM download USING lt_dl_wg   |{ p_path }warengruppen_texte.csv|.
    PERFORM download USING lt_dl_disp |{ p_path }disponenten.csv|.
    PERFORM download USING lt_dl_mbew |{ p_path }mbew_kosten.csv|.
  ENDIF.

  ULINE.
  WRITE: / '=== ENDE. Bitte komplette Ausgabe an Ingo/Analytics zurueckgeben. ==='.

" ----------------------------------------------------------------------------
FORM download USING pt_data TYPE STANDARD TABLE pv_file TYPE string.
  DATA lt_str TYPE STANDARD TABLE OF string.
  lt_str = pt_data.
  cl_gui_frontend_services=>gui_download(
    EXPORTING
      filename = pv_file
      filetype = 'ASC'
    CHANGING
      data_tab = lt_str
    EXCEPTIONS
      OTHERS   = 1 ).
  IF sy-subrc = 0.
    WRITE: / '  geschrieben:', pv_file.
  ELSE.
    WRITE: / '  Download fehlgeschlagen (nur in SAP GUI moeglich):', pv_file.
  ENDIF.
ENDFORM.
