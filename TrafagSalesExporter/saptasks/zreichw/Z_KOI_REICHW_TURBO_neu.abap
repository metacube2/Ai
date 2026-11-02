*&---------------------------------------------------------------------*
*& Report Z_KOI_REICHW_OPT
*&---------------------------------------------------------------------*
*& Optimierte Version - Direkte DB-Zugriffe, kein BDC/Spool
*& Performance: Sekunden statt Minuten
*& 2026-11-02 (Claude, Auftrag Ingo): Ankreuzfeld "Nur Spoolauftrag",
*&   die Liste geht als Spoolauftrag ohne Sofortdruck nach SP01.
*&---------------------------------------------------------------------*
REPORT z_koi_reichw_opt.

*----------------------------------------------------------------------*
* TYPEN
*----------------------------------------------------------------------*
TYPES:
  BEGIN OF ty_material_data,
    matnr                    TYPE matnr,
    maktx                    TYPE maktx,
    mstae                    TYPE mstae,
    bwtk                     TYPE bwkey,
    bme                      TYPE meins,
    waers                    TYPE waers,
    disponent                TYPE dispo,
    zzschrottrsk             TYPE mara-zzschrottrsk,

    " Bestände und Bewegungen (numerisch)
    anfangsbestand_n         TYPE p LENGTH 16 DECIMALS 3,
    summe_zugang_n           TYPE p LENGTH 16 DECIMALS 3,
    summe_abgang_n           TYPE p LENGTH 16 DECIMALS 3,
    summe_verschrottet_n     TYPE p LENGTH 16 DECIMALS 3,
    summe_abgang_minus_ver_n TYPE p LENGTH 16 DECIMALS 3,
    endbestand_n             TYPE p LENGTH 16 DECIMALS 3,

    " Werte (numerisch)
    anfangswert_n            TYPE p LENGTH 16 DECIMALS 2,
    summe_zugwert_n          TYPE p LENGTH 16 DECIMALS 2,
    summe_abgwert_n          TYPE p LENGTH 16 DECIMALS 2,
    endwert_n                TYPE p LENGTH 16 DECIMALS 2,
    preis_stk_n              TYPE p LENGTH 16 DECIMALS 2,

    " Offene Mengen
    me2m_offen_n             TYPE p LENGTH 16 DECIMALS 3,  " Offene Bestellungen
    me3m_offen_n             TYPE p LENGTH 16 DECIMALS 3,  " Offene Rahmenverträge

    " Reichweiten
    reichweite_n             TYPE p LENGTH 8 DECIMALS 1,
    reichw_m_verschr_n       TYPE p LENGTH 8 DECIMALS 1,

    " Flags und Klassifikation
    mc46_nomove              TYPE abap_bool,  " Langsamdreher-Flag
    xyz_klasse               TYPE c LENGTH 1,

    " Kommentare
    komment_supply           TYPE string,
    komment_sales            TYPE string,
    komment_finanzen         TYPE string,

    " Metadaten
    datum_von                TYPE datum,
    datum_bis                TYPE datum,
  END OF ty_material_data,

  tt_material_data TYPE STANDARD TABLE OF ty_material_data WITH KEY matnr,

  " Hilfstypen für Bewegungsdaten
  BEGIN OF ty_mseg_agg,
    matnr TYPE matnr,
    bwart TYPE bwart,
    shkzg TYPE shkzg,    " Soll/Haben Kennzeichen
    menge TYPE menge_d,
    dmbtr TYPE dmbtr,
    sobkz TYPE sobkz,    " Sonderbestandskennzeichen
    lgort TYPE lgort_d,  " Lagerort
  END OF ty_mseg_agg,

  tt_mseg_agg TYPE STANDARD TABLE OF ty_mseg_agg WITH DEFAULT KEY,

  " Für XYZ-Analyse
  BEGIN OF ty_mver_data,
    matnr TYPE matnr,
    gjahr TYPE gjahr,
    gsv01 TYPE mver-gsv01, gsv02 TYPE mver-gsv02, gsv03 TYPE mver-gsv03,
    gsv04 TYPE mver-gsv04, gsv05 TYPE mver-gsv05, gsv06 TYPE mver-gsv06,
    gsv07 TYPE mver-gsv07, gsv08 TYPE mver-gsv08, gsv09 TYPE mver-gsv09,
    gsv10 TYPE mver-gsv10, gsv11 TYPE mver-gsv11, gsv12 TYPE mver-gsv12,
  END OF ty_mver_data,

  tt_mver_data TYPE STANDARD TABLE OF ty_mver_data WITH DEFAULT KEY.

*----------------------------------------------------------------------*
* GLOBALE DATEN
*----------------------------------------------------------------------*
DATA: gt_material_data TYPE tt_material_data,
      gt_mseg_debug    TYPE tt_mseg_agg,      " Für Debug-Ausgabe
      gv_perioden      TYPE i,
      gv_spool         TYPE abap_bool.     " Ausgabe geht in einen Spoolauftrag

*----------------------------------------------------------------------*
* SELEKTIONSBILDSCHIRM
*----------------------------------------------------------------------*
TABLES: mara.

SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE TEXT-001.
  SELECT-OPTIONS: s_matnr FOR mara-matnr.
  PARAMETERS:     p_werks TYPE werks_d DEFAULT '1100' OBLIGATORY.
SELECTION-SCREEN END OF BLOCK b1.

SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE TEXT-002.
  PARAMETERS:
    p_dlow  TYPE datum DEFAULT sy-datum OBLIGATORY,
    p_dhigh TYPE datum DEFAULT sy-datum OBLIGATORY.
SELECTION-SCREEN END OF BLOCK b2.

SELECTION-SCREEN BEGIN OF BLOCK b3 WITH FRAME TITLE TEXT-003.
  PARAMETERS:
    p_alv   RADIOBUTTON GROUP out DEFAULT 'X',  " ALV Ausgabe
    p_write RADIOBUTTON GROUP out.              " WRITE Debug Ausgabe
  " Nur Spoolauftrag erzeugen, nicht drucken (2026-11-02)
  SELECTION-SCREEN BEGIN OF LINE.
    PARAMETERS p_spool AS CHECKBOX.
    SELECTION-SCREEN COMMENT 3(40) lbl_spo.
  SELECTION-SCREEN END OF LINE.
SELECTION-SCREEN END OF BLOCK b3.

* Berechnungsoptionen
SELECTION-SCREEN BEGIN OF BLOCK b4 WITH FRAME TITLE TEXT-004.

  " Periodenberechnung
  SELECTION-SCREEN BEGIN OF LINE.
    SELECTION-SCREEN COMMENT 1(25) lbl_per.
    PARAMETERS p_perdif RADIOBUTTON GROUP per DEFAULT 'X'.
    SELECTION-SCREEN COMMENT 30(20) lbl_pd1.
    PARAMETERS p_perinc RADIOBUTTON GROUP per.
    SELECTION-SCREEN COMMENT 55(20) lbl_pd2.
  SELECTION-SCREEN END OF LINE.

  SELECTION-SCREEN SKIP.

  " Reichweite RW Berechnung
  SELECTION-SCREEN BEGIN OF LINE.
    SELECTION-SCREEN COMMENT 1(25) lbl_rw.
    PARAMETERS p_rwohne RADIOBUTTON GROUP rw DEFAULT 'X'.
    SELECTION-SCREEN COMMENT 30(20) lbl_rw1.
    PARAMETERS p_rwmit  RADIOBUTTON GROUP rw.
    SELECTION-SCREEN COMMENT 55(20) lbl_rw2.
  SELECTION-SCREEN END OF LINE.

  SELECTION-SCREEN SKIP.

  " Anfangswert Berechnung
  SELECTION-SCREEN BEGIN OF LINE.
    SELECTION-SCREEN COMMENT 1(25) lbl_anw.
    PARAMETERS p_anwakt RADIOBUTTON GROUP anw DEFAULT 'X'.
    SELECTION-SCREEN COMMENT 30(20) lbl_aw1.
    PARAMETERS p_anwrec RADIOBUTTON GROUP anw.
    SELECTION-SCREEN COMMENT 55(20) lbl_aw2.
  SELECTION-SCREEN END OF LINE.

SELECTION-SCREEN END OF BLOCK b4.

*----------------------------------------------------------------------*
* LOKALE HILFSKLASSE
*----------------------------------------------------------------------*
CLASS lcl_helper DEFINITION FINAL.
  PUBLIC SECTION.
    CLASS-METHODS calculate_months
      IMPORTING iv_from          TYPE datum
                iv_to            TYPE datum
      RETURNING VALUE(rv_months) TYPE i.
ENDCLASS.

CLASS lcl_helper IMPLEMENTATION.
  METHOD calculate_months.
    rv_months = ( iv_to+0(4) - iv_from+0(4) ) * 12
              + ( iv_to+4(2) - iv_from+4(2) ).
    IF rv_months <= 0.
      rv_months = 1.
    ENDIF.
  ENDMETHOD.
ENDCLASS.

*----------------------------------------------------------------------*
* INITIALISIERUNG
*----------------------------------------------------------------------*
INITIALIZATION.
  " Standardzeitraum: 12 Monate zurück
  p_dhigh = sy-datum.
  p_dlow = sy-datum.
  p_dlow+0(4) = p_dlow+0(4) - 1.  " Ein Jahr zurück

  " Labels für Selektionsbild
  lbl_per = 'Perioden:'.
  lbl_pd1 = 'Differenz (9)'.
  lbl_pd2 = 'Inklusiv (10)'.
  lbl_rw  = 'RW Basis:'.
  lbl_rw1 = 'ohne Verschr.'.
  lbl_rw2 = 'mit Verschr.'.
  lbl_anw = 'Anfangswert:'.
  lbl_aw1 = 'akt. Preis'.
  lbl_aw2 = 'Rückrechnung'.
  lbl_spo = 'Nur Spoolauftrag (nicht drucken)'.

*----------------------------------------------------------------------*
* START-OF-SELECTION
*----------------------------------------------------------------------*
START-OF-SELECTION.

  " Periodenanzahl berechnen
  gv_perioden = lcl_helper=>calculate_months( iv_from = p_dlow iv_to = p_dhigh ).

  " Bei inklusiver Berechnung: +1 Monat
  IF p_perinc = abap_true.
    gv_perioden = gv_perioden + 1.
  ENDIF.

  " Hauptverarbeitung
  PERFORM: load_all_data,
           calculate_reichweite,
           calculate_xyz_analysis,
           load_material_texts.

  " Nur Spoolauftrag: Ausgabe in einen Spoolauftrag ohne Sofortdruck.
  " Im Hintergrund (F9) gelten die Druckparameter des Jobs.
  IF p_spool = abap_true AND sy-batch = abap_false.
    PERFORM spool_ein CHANGING gv_spool.
  ENDIF.

  " Ausgabe je nach Auswahl
  IF p_alv = 'X'.
    PERFORM display_alv.
  ELSE.
    PERFORM display_write_debug.
  ENDIF.

  IF gv_spool = abap_true.
    NEW-PAGE PRINT OFF.
    MESSAGE |Spoolauftrag { sy-spono } erzeugt (nicht gedruckt), siehe SP01| TYPE 'S'.
  ENDIF.

*&---------------------------------------------------------------------*
*& Form LOAD_ALL_DATA
*&---------------------------------------------------------------------*
FORM load_all_data.
  " Hilfstyp für letzte Bewegung
  TYPES: BEGIN OF ty_last_move,
           matnr    TYPE matnr,
           last_mov TYPE datum,
         END OF ty_last_move.

  " Hilfstyp für offene Mengen
  TYPES: BEGIN OF ty_open_qty,
           matnr TYPE matnr,
           menge TYPE menge_d,
         END OF ty_open_qty.

  DATA: lt_matnr_range TYPE RANGE OF matnr,
        lt_mara        TYPE SORTED TABLE OF mara WITH UNIQUE KEY matnr,
        lt_marc        TYPE SORTED TABLE OF marc WITH UNIQUE KEY matnr werks,
        lt_mbew        TYPE SORTED TABLE OF mbew WITH UNIQUE KEY matnr bwkey,
        lt_makt        TYPE SORTED TABLE OF makt WITH UNIQUE KEY matnr spras,
        lt_mard        TYPE SORTED TABLE OF mard WITH UNIQUE KEY matnr werks lgort,
        lt_mseg_raw    TYPE STANDARD TABLE OF ty_mseg_agg,
        lt_ekpo_open   TYPE STANDARD TABLE OF ty_open_qty,
        lt_ekab_open   TYPE STANDARD TABLE OF ty_open_qty,
        lt_last_move   TYPE SORTED TABLE OF ty_last_move WITH UNIQUE KEY matnr.

  FIELD-SYMBOLS: <fs_mat> TYPE ty_material_data.

  " ====== 1. MATERIALSTAMM LADEN ======
  IF s_matnr IS INITIAL.
    " Alle Materialien mit Bestand im Werk
    SELECT DISTINCT matnr FROM mard
      INTO TABLE @DATA(lt_matnr_all)
      WHERE werks = @p_werks
        AND labst > 0.

    LOOP AT lt_matnr_all INTO DATA(ls_matnr_tmp).
      APPEND VALUE #( sign = 'I' option = 'EQ' low = ls_matnr_tmp-matnr ) TO lt_matnr_range.
    ENDLOOP.
  ELSE.
    lt_matnr_range = s_matnr[].
  ENDIF.

  CHECK lt_matnr_range IS NOT INITIAL.

  " MARA - Materialstamm
  SELECT * FROM mara
    INTO TABLE @lt_mara
    WHERE matnr IN @lt_matnr_range.

  " MARC - Werksdaten
  SELECT * FROM marc
    INTO TABLE @lt_marc
    WHERE matnr IN @lt_matnr_range
      AND werks = @p_werks.

  " MBEW - Bewertung
  SELECT * FROM mbew
    INTO TABLE @lt_mbew
    WHERE matnr IN @lt_matnr_range
      AND bwkey = @p_werks.

  " MAKT - Texte
  SELECT * FROM makt
    INTO TABLE @lt_makt
    WHERE matnr IN @lt_matnr_range
      AND spras = @sy-langu.

  " MARD - Lagerbestände (aktuell)
  SELECT * FROM mard
    INTO TABLE @lt_mard
    WHERE matnr IN @lt_matnr_range
      AND werks = @p_werks.

  " MARDH - Historische Bestände für Anfangsbestand
  " Wir brauchen den Bestand VOR dem Analysezeitraum
  TYPES: BEGIN OF ty_mardh,
           matnr TYPE matnr,
           werks TYPE werks_d,
           lgort TYPE lgort_d,
           lfgja TYPE lfgja,
           lfmon TYPE lfmon,
           labst TYPE labst,
         END OF ty_mardh.
  DATA: lt_mardh TYPE SORTED TABLE OF ty_mardh WITH NON-UNIQUE KEY matnr werks lgort lfgja lfmon.

  " Historischen Bestand zum Monatsende VOR Startdatum holen
  DATA: lv_hist_year  TYPE lfgja,
        lv_hist_month TYPE lfmon.
  lv_hist_year  = p_dlow+0(4).
  lv_hist_month = p_dlow+4(2).
  " Vormonat berechnen
  IF lv_hist_month = '01'.
    lv_hist_month = '12'.
    lv_hist_year = lv_hist_year - 1.
  ELSE.
    lv_hist_month = lv_hist_month - 1.
  ENDIF.

  SELECT matnr, werks, lgort, lfgja, lfmon, labst
    FROM mardh
    INTO TABLE @lt_mardh
    WHERE matnr IN @lt_matnr_range
      AND werks = @p_werks
      AND lfgja = @lv_hist_year
      AND lfmon = @lv_hist_month.

  " ====== 2. BEWEGUNGSDATEN LADEN ======
  " ALLE Bewegungen laden
  SELECT mseg~matnr, mseg~bwart, mseg~shkzg,
         mseg~menge AS menge,
         mseg~dmbtr AS dmbtr,
         mseg~sobkz,
         mseg~lgort
    FROM mseg
    INNER JOIN mkpf ON mkpf~mblnr = mseg~mblnr
                   AND mkpf~mjahr = mseg~mjahr
    INTO TABLE @lt_mseg_raw
    WHERE mseg~matnr IN @lt_matnr_range
      AND mseg~werks = @p_werks
      AND mkpf~budat BETWEEN @p_dlow AND @p_dhigh.

  " Für Debug-Ausgabe global speichern
  gt_mseg_debug = lt_mseg_raw.

  " ====== 3. OFFENE BESTELLUNGEN (ME2M) ======
  " EKET enthält Einteilungen mit offenen Mengen
  " EKET-MENGE = Einteilungsmenge, EKET-WEMNG = bereits gelieferte Menge
  SELECT ekpo~matnr,
         SUM( eket~menge - eket~wemng ) AS menge
    FROM eket
    INNER JOIN ekpo ON ekpo~ebeln = eket~ebeln
                   AND ekpo~ebelp = eket~ebelp
    INNER JOIN ekko ON ekko~ebeln = ekpo~ebeln
    INTO TABLE @lt_ekpo_open
    WHERE ekpo~matnr IN @lt_matnr_range
      AND ekpo~werks = @p_werks
      AND ekpo~loekz = @space           " Position nicht gelöscht
      AND ekpo~elikz = @space           " Position nicht erledigt
      AND ekko~loekz = @space           " Bestellung nicht gelöscht
      AND ekko~bstyp = 'F'              " Nur Normalbestellungen
      AND eket~menge > eket~wemng       " Nur wenn noch offen
    GROUP BY ekpo~matnr.

  " ====== 4. OFFENE RAHMENVERTRÄGE/LIEFERPLANABRUFE (ME3M) ======
  SELECT ekpo~matnr,
         SUM( ekab~menge ) AS menge
    FROM ekab
    INNER JOIN ekpo ON ekpo~ebeln = ekab~ebeln
                   AND ekpo~ebelp = ekab~ebelp
    INTO TABLE @lt_ekab_open
    WHERE ekpo~matnr IN @lt_matnr_range
      AND ekpo~werks = @p_werks
      AND ekpo~loekz = @space
      AND ekab~menge > 0
    GROUP BY ekpo~matnr.

  " ====== 5. LETZTE BEWEGUNG PRO MATERIAL (MC46-Check) ======
  SELECT mseg~matnr,
         MAX( mkpf~budat ) AS last_mov
    FROM mseg
    INNER JOIN mkpf ON mkpf~mblnr = mseg~mblnr
                   AND mkpf~mjahr = mseg~mjahr
    INTO TABLE @lt_last_move
    WHERE mseg~matnr IN @lt_matnr_range
      AND mseg~werks = @p_werks
    GROUP BY mseg~matnr.

  " ====== 6. DATEN ZUSAMMENFÜHREN ======
  LOOP AT lt_mara ASSIGNING FIELD-SYMBOL(<fs_mara>).
    APPEND INITIAL LINE TO gt_material_data ASSIGNING <fs_mat>.

    " Stammdaten
    <fs_mat>-matnr        = <fs_mara>-matnr.
    <fs_mat>-mstae        = <fs_mara>-mstae.
    <fs_mat>-bme          = <fs_mara>-meins.
    <fs_mat>-zzschrottrsk = <fs_mara>-zzschrottrsk.
    <fs_mat>-datum_von    = p_dlow.
    <fs_mat>-datum_bis    = p_dhigh.

    " Materialkurztext
    READ TABLE lt_makt WITH KEY matnr = <fs_mara>-matnr
                                spras = sy-langu
                       ASSIGNING FIELD-SYMBOL(<fs_makt>).
    IF sy-subrc = 0.
      <fs_mat>-maktx = <fs_makt>-maktx.
    ENDIF.

    " Werksdaten
    READ TABLE lt_marc WITH KEY matnr = <fs_mara>-matnr
                                werks = p_werks
                       ASSIGNING FIELD-SYMBOL(<fs_marc>).
    IF sy-subrc = 0.
      <fs_mat>-disponent = <fs_marc>-dispo.
    ENDIF.

    " Bewertung
    READ TABLE lt_mbew WITH KEY matnr = <fs_mara>-matnr
                                bwkey = p_werks
                       ASSIGNING FIELD-SYMBOL(<fs_mbew>).
    IF sy-subrc = 0.
      <fs_mat>-bwtk  = <fs_mbew>-bwkey.
      <fs_mat>-waers = 'CHF'.  " Lokale Währung
    ENDIF.

    " ====== ANFANGSBESTAND AUS MARDH (historisch) ======
    DATA(lv_anfangsbestand) = CONV menge_d( 0 ).
    LOOP AT lt_mardh ASSIGNING FIELD-SYMBOL(<fs_mardh>)
                     WHERE matnr = <fs_mara>-matnr.
      lv_anfangsbestand = lv_anfangsbestand + <fs_mardh>-labst.
    ENDLOOP.

    " Falls kein historischer Bestand gefunden, aktuellen aus MARD als Fallback
    IF lv_anfangsbestand = 0.
      LOOP AT lt_mard ASSIGNING FIELD-SYMBOL(<fs_mard>)
                      WHERE matnr = <fs_mara>-matnr.
        lv_anfangsbestand = lv_anfangsbestand + <fs_mard>-labst.
      ENDLOOP.
    ENDIF.

    " ====== BEWEGUNGEN AGGREGIEREN ======
    " SHKZG = 'S' (Soll) = Zugang zum Bestand
    " SHKZG = 'H' (Haben) = Abgang vom Bestand
    DATA: lv_zugang      TYPE p LENGTH 16 DECIMALS 3,
          lv_abgang      TYPE p LENGTH 16 DECIMALS 3,
          lv_verschrott  TYPE p LENGTH 16 DECIMALS 3,
          lv_zugwert     TYPE p LENGTH 16 DECIMALS 2,
          lv_abgwert     TYPE p LENGTH 16 DECIMALS 2.

    CLEAR: lv_zugang, lv_abgang, lv_verschrott, lv_zugwert, lv_abgwert.

    LOOP AT lt_mseg_raw ASSIGNING FIELD-SYMBOL(<fs_mseg>)
                        WHERE matnr = <fs_mara>-matnr.

      " Basierend auf Soll/Haben-Kennzeichen (der einfachste und korrekteste Weg!)
      IF <fs_mseg>-shkzg = 'S'.  " Soll = Zugang
        lv_zugang  = lv_zugang + <fs_mseg>-menge.
        lv_zugwert = lv_zugwert + <fs_mseg>-dmbtr.
      ELSE.  " Haben = Abgang
        lv_abgang  = lv_abgang + <fs_mseg>-menge.
        lv_abgwert = lv_abgwert + <fs_mseg>-dmbtr.
      ENDIF.

      " Verschrottung separat tracken (BWARTs 551-558)
      IF <fs_mseg>-bwart BETWEEN '551' AND '558'.
        lv_verschrott = lv_verschrott + <fs_mseg>-menge.
      ENDIF.

    ENDLOOP.

    <fs_mat>-summe_zugang_n        = lv_zugang.
    <fs_mat>-summe_abgang_n        = lv_abgang.
    <fs_mat>-summe_verschrottet_n  = lv_verschrott.
    <fs_mat>-summe_abgang_minus_ver_n = lv_abgang - lv_verschrott.
    <fs_mat>-summe_zugwert_n       = lv_zugwert.
    <fs_mat>-summe_abgwert_n       = lv_abgwert.

    " ====== BESTÄNDE BERECHNEN ======
    " Anfangsbestand aus MARDH (bereits oben ermittelt)
    <fs_mat>-anfangsbestand_n = lv_anfangsbestand.

    " Endbestand = Anfangsbestand + Zugänge - Abgänge
    <fs_mat>-endbestand_n = lv_anfangsbestand + lv_zugang - lv_abgang.

    " ====== WERTE BERECHNEN ======
    IF p_anwakt = abap_true.
      " Option 1: Anfangswert mit aktuellem Preis approximieren
      IF <fs_mbew> IS ASSIGNED AND <fs_mbew>-verpr > 0.
        <fs_mat>-anfangswert_n = lv_anfangsbestand * ( <fs_mbew>-verpr / <fs_mbew>-peinh ).
      ENDIF.
      " Endwert = Anfangswert + Zugangswert - Abgangswert
      <fs_mat>-endwert_n = <fs_mat>-anfangswert_n + lv_zugwert - lv_abgwert.
    ELSE.
      " Option 2: Endwert aus MBEW, Anfangswert rückrechnen
      IF <fs_mbew> IS ASSIGNED.
        <fs_mat>-endwert_n = <fs_mbew>-salk3.
        " Anfangswert = Endwert - Zugangswert + Abgangswert
        <fs_mat>-anfangswert_n = <fs_mbew>-salk3 - lv_zugwert + lv_abgwert.
      ENDIF.
    ENDIF.

    " Preis pro Stück
    IF <fs_mat>-endbestand_n > 0.
      <fs_mat>-preis_stk_n = <fs_mat>-endwert_n / <fs_mat>-endbestand_n.
    ENDIF.

    " ====== OFFENE MENGEN ======
    READ TABLE lt_ekpo_open ASSIGNING FIELD-SYMBOL(<fs_ekpo>)
                            WITH KEY matnr = <fs_mara>-matnr.
    IF sy-subrc = 0.
      <fs_mat>-me2m_offen_n = <fs_ekpo>-menge.
    ENDIF.

    READ TABLE lt_ekab_open ASSIGNING FIELD-SYMBOL(<fs_ekab>)
                            WITH KEY matnr = <fs_mara>-matnr.
    IF sy-subrc = 0.
      <fs_mat>-me3m_offen_n = <fs_ekab>-menge.
    ENDIF.

    " ====== MC46 - LANGSAMDREHER CHECK ======
    DATA(lv_cutoff_date) = CONV datum( sy-datum - 365 ).
    READ TABLE lt_last_move ASSIGNING FIELD-SYMBOL(<fs_last>)
                            WITH TABLE KEY matnr = <fs_mara>-matnr.
    IF sy-subrc = 0.
      IF <fs_last>-last_mov < lv_cutoff_date.
        <fs_mat>-mc46_nomove = abap_true.
      ENDIF.
    ELSE.
      " Keine Bewegung gefunden = Langsamdreher
      <fs_mat>-mc46_nomove = abap_true.
    ENDIF.

  ENDLOOP.

ENDFORM.

*&---------------------------------------------------------------------*
*& Form CALCULATE_REICHWEITE
*&---------------------------------------------------------------------*
FORM calculate_reichweite.
  DATA: lv_verfuegbar        TYPE p LENGTH 16 DECIMALS 3,
        lv_monatl_abgang     TYPE p LENGTH 16 DECIMALS 3,
        lv_monatl_abg_total  TYPE p LENGTH 16 DECIMALS 3,
        lv_rw_ohne_ver       TYPE p LENGTH 16 DECIMALS 1,
        lv_rw_mit_ver        TYPE p LENGTH 16 DECIMALS 1.

  FIELD-SYMBOLS: <fs_mat> TYPE ty_material_data.

  LOOP AT gt_material_data ASSIGNING <fs_mat>.
    CLEAR: lv_verfuegbar, lv_monatl_abgang, lv_monatl_abg_total,
           lv_rw_ohne_ver, lv_rw_mit_ver.

    " Verfügbare Menge = Endbestand + offene Bestellungen + offene Kontrakte
    lv_verfuegbar = <fs_mat>-endbestand_n +
                    <fs_mat>-me2m_offen_n +
                    <fs_mat>-me3m_offen_n.

    " Reichweite OHNE Verschrottung im Nenner (längere RW)
    IF <fs_mat>-summe_abgang_minus_ver_n > 0 AND gv_perioden > 0.
      lv_monatl_abgang = <fs_mat>-summe_abgang_minus_ver_n / gv_perioden.
      IF lv_monatl_abgang > 0.
        lv_rw_ohne_ver = lv_verfuegbar / lv_monatl_abgang.
      ENDIF.
    ENDIF.

    " Reichweite MIT Verschrottung im Nenner (kürzere RW)
    IF <fs_mat>-summe_abgang_n > 0 AND gv_perioden > 0.
      lv_monatl_abg_total = <fs_mat>-summe_abgang_n / gv_perioden.
      IF lv_monatl_abg_total > 0.
        lv_rw_mit_ver = lv_verfuegbar / lv_monatl_abg_total.
      ENDIF.
    ENDIF.

    " Zuweisung basierend auf Benutzerauswahl
    IF p_rwohne = abap_true.
      " Standard: RW = ohne Verschrottung (länger), RWV = mit Verschrottung (kürzer)
      <fs_mat>-reichweite_n      = lv_rw_ohne_ver.
      <fs_mat>-reichw_m_verschr_n = lv_rw_mit_ver.
    ELSE.
      " Alternativ: RW = mit Verschrottung (kürzer), RWV = ohne Verschrottung (länger)
      <fs_mat>-reichweite_n      = lv_rw_mit_ver.
      <fs_mat>-reichw_m_verschr_n = lv_rw_ohne_ver.
    ENDIF.

  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*& Form CALCULATE_XYZ_ANALYSIS
*&---------------------------------------------------------------------*
FORM calculate_xyz_analysis.
  CONSTANTS: gc_limit_x TYPE p LENGTH 8 DECIMALS 2 VALUE '0.20',
             gc_limit_y TYPE p LENGTH 8 DECIMALS 2 VALUE '0.50'.

  TYPES: ty_p16_3 TYPE p LENGTH 16 DECIMALS 3.

  DATA: lt_mver      TYPE SORTED TABLE OF ty_mver_data WITH NON-UNIQUE KEY matnr gjahr,
        lt_matnr_rng TYPE RANGE OF matnr.

  FIELD-SYMBOLS: <fs_mat> TYPE ty_material_data.

  CHECK gt_material_data IS NOT INITIAL.

  " Material-Range aufbauen
  LOOP AT gt_material_data ASSIGNING <fs_mat>.
    APPEND VALUE #( sign = 'I' option = 'EQ' low = <fs_mat>-matnr ) TO lt_matnr_rng.
  ENDLOOP.

  " Jahre für Selektion
  DATA(lv_year_from) = p_dlow+0(4).
  DATA(lv_year_to)   = p_dhigh+0(4).

  " MVER-Daten laden
  SELECT matnr, gjahr,
         gsv01, gsv02, gsv03, gsv04, gsv05, gsv06,
         gsv07, gsv08, gsv09, gsv10, gsv11, gsv12
    FROM mver
    INTO TABLE @lt_mver
    WHERE matnr IN @lt_matnr_rng
      AND werks = @p_werks
      AND perkz = 'M'
      AND gjahr BETWEEN @lv_year_from AND @lv_year_to.

  " XYZ pro Material berechnen
  LOOP AT gt_material_data ASSIGNING <fs_mat>.
    DATA: lt_values  TYPE STANDARD TABLE OF ty_p16_3,
          lv_sum     TYPE p LENGTH 16 DECIMALS 3,
          lv_mean    TYPE p LENGTH 16 DECIMALS 3,
          lv_var     TYPE p LENGTH 16 DECIMALS 6,
          lv_stddev  TYPE p LENGTH 16 DECIMALS 3,
          lv_vk      TYPE p LENGTH 16 DECIMALS 3,
          lv_diff    TYPE p LENGTH 16 DECIMALS 3,
          lv_diff_sq TYPE p LENGTH 16 DECIMALS 6.

    CLEAR: lt_values, lv_sum, lv_mean, lv_var, lv_stddev, lv_vk.

    " Werte für dieses Material sammeln
    LOOP AT lt_mver ASSIGNING FIELD-SYMBOL(<fs_mver>)
                    WHERE matnr = <fs_mat>-matnr.
      " Alle Monatswerte addieren
      APPEND <fs_mver>-gsv01 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv01.
      APPEND <fs_mver>-gsv02 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv02.
      APPEND <fs_mver>-gsv03 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv03.
      APPEND <fs_mver>-gsv04 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv04.
      APPEND <fs_mver>-gsv05 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv05.
      APPEND <fs_mver>-gsv06 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv06.
      APPEND <fs_mver>-gsv07 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv07.
      APPEND <fs_mver>-gsv08 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv08.
      APPEND <fs_mver>-gsv09 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv09.
      APPEND <fs_mver>-gsv10 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv10.
      APPEND <fs_mver>-gsv11 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv11.
      APPEND <fs_mver>-gsv12 TO lt_values. lv_sum = lv_sum + <fs_mver>-gsv12.
    ENDLOOP.

    DATA(lv_count) = lines( lt_values ).

    IF lv_count > 0.
      lv_mean = lv_sum / lv_count.

      IF lv_mean > 0.
        " Varianz berechnen - explizit typisiert
        LOOP AT lt_values INTO DATA(lv_val).
          lv_diff = lv_val - lv_mean.
          lv_diff_sq = lv_diff * lv_diff.
          lv_var = lv_var + lv_diff_sq.
        ENDLOOP.

        IF lv_count > 1.
          lv_var = lv_var / ( lv_count - 1 ).
          IF lv_var > 0.
            lv_stddev = sqrt( lv_var ).
          ENDIF.
          IF lv_mean <> 0.
            lv_vk = lv_stddev / lv_mean.
          ENDIF.
        ENDIF.

        " Klassifizierung
        IF lv_vk <= gc_limit_x.
          <fs_mat>-xyz_klasse = 'X'.
        ELSEIF lv_vk <= gc_limit_y.
          <fs_mat>-xyz_klasse = 'Y'.
        ELSE.
          <fs_mat>-xyz_klasse = 'Z'.
        ENDIF.
      ELSE.
        <fs_mat>-xyz_klasse = 'Z'.
      ENDIF.
    ELSE.
      <fs_mat>-xyz_klasse = 'Z'.
    ENDIF.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*& Form LOAD_MATERIAL_TEXTS
*&---------------------------------------------------------------------*
FORM load_material_texts.
  DATA: lt_lines  TYPE TABLE OF tline,
        lv_tdname TYPE thead-tdname.

  FIELD-SYMBOLS: <fs_mat> TYPE ty_material_data.

  LOOP AT gt_material_data ASSIGNING <fs_mat>.
    " Einkaufstext
    lv_tdname = |ZEK_{ <fs_mat>-matnr }|.
    PERFORM read_text USING lv_tdname CHANGING <fs_mat>-komment_supply.

    " Vertriebstext
    lv_tdname = |ZVK_{ <fs_mat>-matnr }|.
    PERFORM read_text USING lv_tdname CHANGING <fs_mat>-komment_sales.

    " Finanztext
    lv_tdname = |ZFI_{ <fs_mat>-matnr }|.
    PERFORM read_text USING lv_tdname CHANGING <fs_mat>-komment_finanzen.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*& Form READ_TEXT
*&---------------------------------------------------------------------*
FORM read_text USING    iv_tdname TYPE thead-tdname
               CHANGING cv_text   TYPE string.
  DATA: lt_lines TYPE TABLE OF tline.

  CLEAR cv_text.

  CALL FUNCTION 'READ_TEXT'
    EXPORTING
      id       = 'ST'
      language = sy-langu
      name     = iv_tdname
      object   = 'TEXT'
    TABLES
      lines    = lt_lines
    EXCEPTIONS
      OTHERS   = 8.

  IF sy-subrc = 0.
    LOOP AT lt_lines ASSIGNING FIELD-SYMBOL(<fs_line>).
      cv_text = |{ cv_text } { <fs_line>-tdline }|.
    ENDLOOP.
    CONDENSE cv_text.
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
*& Form DISPLAY_ALV
*&---------------------------------------------------------------------*
FORM display_alv.
  DATA: lo_alv     TYPE REF TO cl_salv_table,
        lo_columns TYPE REF TO cl_salv_columns_table,
        lo_column  TYPE REF TO cl_salv_column.

  TRY.
      " Im Spoolmodus als Liste, damit die Ausgabe in den Spoolauftrag geht
      cl_salv_table=>factory(
        EXPORTING list_display = gv_spool
        IMPORTING r_salv_table = lo_alv
        CHANGING  t_table      = gt_material_data ).

      " Funktionen aktivieren
      lo_alv->get_functions( )->set_all( abap_true ).

      " Spalten konfigurieren
      lo_columns = lo_alv->get_columns( ).
      lo_columns->set_optimize( abap_true ).

      " Spaltenüberschriften setzen
      PERFORM set_column_text USING lo_columns 'MATNR' 'Material' 'Material' 'Mat.'.
      PERFORM set_column_text USING lo_columns 'MAKTX' 'Materialkurztext' 'Kurztext' 'Text'.
      PERFORM set_column_text USING lo_columns 'MSTAE' 'Status' 'Status' 'Stat'.
      PERFORM set_column_text USING lo_columns 'DISPONENT' 'Disponent' 'Dispo' 'Disp'.
      PERFORM set_column_text USING lo_columns 'BME' 'Mengeneinheit' 'ME' 'ME'.
      PERFORM set_column_text USING lo_columns 'WAERS' 'Währung' 'Währ' 'Wäh'.

      PERFORM set_column_text USING lo_columns 'ANFANGSBESTAND_N' 'Anfangsbestand' 'Anf.Best' 'Anf'.
      PERFORM set_column_text USING lo_columns 'SUMME_ZUGANG_N' 'Summe Zugang' 'Sum.Zug' 'Zug'.
      PERFORM set_column_text USING lo_columns 'SUMME_ABGANG_N' 'Summe Abgang' 'Sum.Abg' 'Abg'.
      PERFORM set_column_text USING lo_columns 'SUMME_VERSCHROTTET_N' 'Verschrottet' 'Verschr' 'Ver'.
      PERFORM set_column_text USING lo_columns 'SUMME_ABGANG_MINUS_VER_N' 'Abgang bereinigt' 'Abg.ber' 'AbB'.
      PERFORM set_column_text USING lo_columns 'ENDBESTAND_N' 'Endbestand' 'Endbestand' 'End'.

      PERFORM set_column_text USING lo_columns 'ANFANGSWERT_N' 'Anfangswert' 'Anf.Wert' 'AnW'.
      PERFORM set_column_text USING lo_columns 'SUMME_ZUGWERT_N' 'Zugangswert' 'Zug.Wert' 'ZuW'.
      PERFORM set_column_text USING lo_columns 'SUMME_ABGWERT_N' 'Abgangswert' 'Abg.Wert' 'AbW'.
      PERFORM set_column_text USING lo_columns 'ENDWERT_N' 'Endwert' 'Endwert' 'EnW'.
      PERFORM set_column_text USING lo_columns 'PREIS_STK_N' 'Preis/Stk' 'Pr/Stk' 'P/S'.

      PERFORM set_column_text USING lo_columns 'ME2M_OFFEN_N' 'Offene Bestellungen' 'Off.Best' 'OBe'.
      PERFORM set_column_text USING lo_columns 'ME3M_OFFEN_N' 'Offene Rahmenvertr.' 'Off.Rahm' 'ORa'.

      PERFORM set_column_text USING lo_columns 'REICHWEITE_N' 'Reichweite (Mte)' 'Reichw.' 'RW'.
      PERFORM set_column_text USING lo_columns 'REICHW_M_VERSCHR_N' 'Reichw. m. Verschr.' 'RW.Vers' 'RWV'.

      PERFORM set_column_text USING lo_columns 'MC46_NOMOVE' 'Langsamdreher' 'Langsam' 'LD'.
      PERFORM set_column_text USING lo_columns 'XYZ_KLASSE' 'XYZ-Klasse' 'XYZ' 'XYZ'.
      PERFORM set_column_text USING lo_columns 'ZZSCHROTTRSK' 'Verschr.Risiko' 'Vers.Risk' 'VR'.

      PERFORM set_column_text USING lo_columns 'KOMMENT_SUPPLY' 'Einkaufskommentar' 'EK-Komm' 'EK'.
      PERFORM set_column_text USING lo_columns 'KOMMENT_SALES' 'Vertriebskommentar' 'VK-Komm' 'VK'.
      PERFORM set_column_text USING lo_columns 'KOMMENT_FINANZEN' 'Finanzkommentar' 'FI-Komm' 'FI'.

      PERFORM set_column_text USING lo_columns 'DATUM_VON' 'Datum von' 'Dat.von' 'Von'.
      PERFORM set_column_text USING lo_columns 'DATUM_BIS' 'Datum bis' 'Dat.bis' 'Bis'.

      " Spalte BWTK ausblenden (nur intern relevant)
      TRY.
          lo_column = lo_columns->get_column( 'BWTK' ).
          lo_column->set_visible( abap_false ).
        CATCH cx_salv_not_found.
      ENDTRY.

      lo_alv->display( ).

    CATCH cx_salv_msg INTO DATA(lx_msg).
      MESSAGE lx_msg TYPE 'E'.
  ENDTRY.
ENDFORM.

*&---------------------------------------------------------------------*
*& Form SPOOL_EIN
*&---------------------------------------------------------------------*
*& Druckparameter aus dem Benutzerstamm, aber ohne Sofortdruck.
*&---------------------------------------------------------------------*
FORM spool_ein CHANGING cv_spool TYPE abap_bool.
  DATA: ls_params TYPE pri_params,
        lv_valid  TYPE c LENGTH 1.

  cv_spool = abap_false.

  CALL FUNCTION 'GET_PRINT_PARAMETERS'
    EXPORTING
      immediately    = space
      release        = space
      new_list_id    = 'X'
      list_name      = 'ZREICHW'
      list_text      = 'Reichweitenanalyse'
      line_count     = 65
      line_size      = 255
      no_dialog      = 'X'
    IMPORTING
      out_parameters = ls_params
      valid          = lv_valid
    EXCEPTIONS
      OTHERS         = 4.

  IF sy-subrc <> 0 OR lv_valid IS INITIAL.
    MESSAGE 'Kein Ausgabegerät im Benutzerstamm (SU3, Festwerte), Spoolauftrag nicht möglich' TYPE 'E'.
  ENDIF.

  " Sicherheitshalber: nie sofort ausgeben
  ls_params-primm = space.

  NEW-PAGE PRINT ON PARAMETERS ls_params NO DIALOG.
  cv_spool = abap_true.
ENDFORM.

*&---------------------------------------------------------------------*
*& Form SET_COLUMN_TEXT
*&---------------------------------------------------------------------*
FORM set_column_text USING io_columns TYPE REF TO cl_salv_columns_table
                           iv_colname TYPE lvc_fname
                           iv_long    TYPE string
                           iv_medium  TYPE string
                           iv_short   TYPE string.
  DATA: lo_column TYPE REF TO cl_salv_column.

  TRY.
      lo_column = io_columns->get_column( iv_colname ).
      lo_column->set_long_text( CONV #( iv_long ) ).
      lo_column->set_medium_text( CONV #( iv_medium ) ).
      lo_column->set_short_text( CONV #( iv_short ) ).
    CATCH cx_salv_not_found.
  ENDTRY.
ENDFORM.

*&---------------------------------------------------------------------*
*& Form DISPLAY_WRITE_DEBUG
*&---------------------------------------------------------------------*
*& Debug-Ausgabe mit WRITE - zeigt alle Bewegungen im Detail
*&---------------------------------------------------------------------*
FORM display_write_debug.
  DATA: lv_sum_zugang  TYPE p LENGTH 16 DECIMALS 3,
        lv_sum_abgang  TYPE p LENGTH 16 DECIMALS 3,
        lv_count       TYPE i,
        lv_calc        TYPE p LENGTH 16 DECIMALS 3,
        lv_verfueg     TYPE p LENGTH 16 DECIMALS 3.

  WRITE: / '================================================================'.
  WRITE: / 'DEBUG AUSGABE - Bewegungsanalyse'.
  WRITE: / '================================================================'.
  WRITE: / 'Werk:', p_werks.
  WRITE: / 'Zeitraum:', p_dlow, '-', p_dhigh.
  WRITE: / 'Berechnete Perioden:', gv_perioden, 'Monate'.
  WRITE: / 'Anzahl Materialien:', lines( gt_material_data ).
  WRITE: / 'Anzahl Bewegungen gesamt:', lines( gt_mseg_debug ).
  SKIP.
  WRITE: / '--- GEWÄHLTE OPTIONEN ---'.
  IF p_perdif = abap_true.
    WRITE: / 'Perioden: Differenz (z.B. Jan-Okt = 9)'.
  ELSE.
    WRITE: / 'Perioden: Inklusiv (z.B. Jan-Okt = 10)'.
  ENDIF.
  IF p_rwohne = abap_true.
    WRITE: / 'RW: ohne Verschr. im Nenner (längere RW), RWV: mit Verschr.'.
  ELSE.
    WRITE: / 'RW: mit Verschr. im Nenner (kürzere RW), RWV: ohne Verschr.'.
  ENDIF.
  IF p_anwakt = abap_true.
    WRITE: / 'Anfangswert: Approximation mit aktuellem Preis'.
  ELSE.
    WRITE: / 'Anfangswert: Rückrechnung aus MBEW-Endwert'.
  ENDIF.
  WRITE: / '================================================================'.
  SKIP.

  " Pro Material ausgeben
  LOOP AT gt_material_data ASSIGNING FIELD-SYMBOL(<fs_mat>).

    WRITE: / '----------------------------------------------------------------'.
    WRITE: / 'Material:', <fs_mat>-matnr, <fs_mat>-maktx.
    WRITE: / '----------------------------------------------------------------'.
    WRITE: / 'Anfangsbestand:', <fs_mat>-anfangsbestand_n.
    WRITE: / 'Endbestand:    ', <fs_mat>-endbestand_n.
    WRITE: / 'Zugang (Summe):', <fs_mat>-summe_zugang_n.
    WRITE: / 'Abgang (Summe):', <fs_mat>-summe_abgang_n.
    SKIP.

    " Alle Bewegungen für dieses Material
    WRITE: / 'Einzelne Bewegungen aus MSEG:'.
    WRITE: / '  BWART  SHKZG     MENGE  LGORT  SOBKZ      DMBTR'.
    WRITE: / '  -----  -----  --------  -----  -----  ---------'.

    CLEAR: lv_sum_zugang, lv_sum_abgang, lv_count, lv_calc, lv_verfueg.

    LOOP AT gt_mseg_debug ASSIGNING FIELD-SYMBOL(<fs_mseg>)
                          WHERE matnr = <fs_mat>-matnr.
      lv_count = lv_count + 1.

      WRITE: / '  ', <fs_mseg>-bwart,
               '     ', <fs_mseg>-shkzg,
               '  ', <fs_mseg>-menge,
               '  ', <fs_mseg>-lgort,
               '  ', <fs_mseg>-sobkz,
               '  ', <fs_mseg>-dmbtr.

      IF <fs_mseg>-shkzg = 'S'.
        lv_sum_zugang = lv_sum_zugang + <fs_mseg>-menge.
      ELSE.
        lv_sum_abgang = lv_sum_abgang + <fs_mseg>-menge.
      ENDIF.

    ENDLOOP.

    SKIP.
    WRITE: / 'Anzahl Bewegungen gefunden:', lv_count.
    WRITE: / 'Summe Zugang (SHKZG=S):    ', lv_sum_zugang.
    WRITE: / 'Summe Abgang (SHKZG=H):    ', lv_sum_abgang.
    lv_calc = <fs_mat>-anfangsbestand_n + lv_sum_zugang - lv_sum_abgang.
    WRITE: / 'Berechnet: Anf + Zug - Abg =', lv_calc.
    SKIP.
    WRITE: / '--- WERTE ---'.
    WRITE: / 'Anfangswert:', <fs_mat>-anfangswert_n.
    WRITE: / 'Zugangswert:', <fs_mat>-summe_zugwert_n.
    WRITE: / 'Abgangswert:', <fs_mat>-summe_abgwert_n.
    WRITE: / 'Endwert:    ', <fs_mat>-endwert_n.
    WRITE: / 'Preis/Stk:  ', <fs_mat>-preis_stk_n.
    SKIP.
    WRITE: / '--- REICHWEITE ---'.
    lv_verfueg = <fs_mat>-endbestand_n + <fs_mat>-me2m_offen_n + <fs_mat>-me3m_offen_n.
    WRITE: / 'Verfügbar (End+ME2M+ME3M):', lv_verfueg.
    WRITE: / 'Abgang minus Verschr.:    ', <fs_mat>-summe_abgang_minus_ver_n.
    WRITE: / 'Verschrottet:             ', <fs_mat>-summe_verschrottet_n.
    WRITE: / 'RW (Reichweite):          ', <fs_mat>-reichweite_n, 'Monate'.
    WRITE: / 'RWV (mit Verschrottung):  ', <fs_mat>-reichw_m_verschr_n, 'Monate'.
    WRITE: / 'XYZ-Klasse:               ', <fs_mat>-xyz_klasse.
    SKIP 2.

  ENDLOOP.

  WRITE: / '================================================================'.
  WRITE: / 'ENDE DEBUG AUSGABE'.
  WRITE: / '================================================================'.

ENDFORM.

*&---------------------------------------------------------------------*
*& Hilfsfunktion: Monate berechnen (als FORM für Kompatibilität)
*&---------------------------------------------------------------------*
FORM calc_months USING    iv_from        TYPE datum
                          iv_to          TYPE datum
                 CHANGING cv_months      TYPE i.
  cv_months = lcl_helper=>calculate_months( iv_from = iv_from iv_to = iv_to ).
ENDFORM.
