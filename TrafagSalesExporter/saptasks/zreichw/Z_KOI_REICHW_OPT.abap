*&---------------------------------------------------------------------*
*& Report Z_KOI_REICHW_OPT
*&---------------------------------------------------------------------*
*& Kopie von Z_KOI_REICHW_TURBO (ZREICHW), nur schneller, Ergebnis gleich
*& (Auftrag Ingo 2026-11-02, Vergleich mit Z_KOI_REICHW_VGL).
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

  tt_mseg_agg TYPE STANDARD TABLE OF ty_mseg_agg WITH DEFAULT KEY
                WITH NON-UNIQUE SORTED KEY mat COMPONENTS matnr,

  " Materialliste der Auswertung (geht als Tabelle in die Abfragen)
  BEGIN OF ty_mat,
    matnr TYPE matnr,
  END OF ty_mat,

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
      gv_spool         TYPE abap_bool,     " Ausgabe geht in einen Spoolauftrag
      gt_mat           TYPE SORTED TABLE OF ty_mat WITH UNIQUE KEY matnr.

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
  " Optimiert 2026-11-02: Materialliste als interne Tabelle direkt in den
  " Abfragen (kein IN mit tausenden Einzelwerten), Bewegungen in der
  " Datenbank verdichtet, nur sortierte/Hash-Zugriffe. Ergebnis wie bisher.
  TYPES: BEGIN OF ty_mara,
           matnr        TYPE matnr,
           mstae        TYPE mstae,
           meins        TYPE meins,
           zzschrottrsk TYPE mara-zzschrottrsk,
         END OF ty_mara,
         BEGIN OF ty_makt,
           matnr TYPE matnr,
           maktx TYPE maktx,
         END OF ty_makt,
         BEGIN OF ty_marc,
           matnr TYPE matnr,
           dispo TYPE dispo,
         END OF ty_marc,
         BEGIN OF ty_mbew,
           matnr TYPE matnr,
           bwkey TYPE bwkey,
           verpr TYPE mbew-verpr,
           peinh TYPE mbew-peinh,
           salk3 TYPE mbew-salk3,
         END OF ty_mbew,
         BEGIN OF ty_bestand,
           matnr TYPE matnr,
           labst TYPE p LENGTH 16 DECIMALS 3,
         END OF ty_bestand,
         BEGIN OF ty_bew,
           matnr TYPE matnr,
           shkzg TYPE shkzg,
           bwart TYPE bwart,
           menge TYPE p LENGTH 16 DECIMALS 3,
           dmbtr TYPE p LENGTH 16 DECIMALS 2,
         END OF ty_bew,
         BEGIN OF ty_open_qty,
           matnr TYPE matnr,
           menge TYPE menge_d,
         END OF ty_open_qty.

  DATA: lt_mara      TYPE SORTED TABLE OF ty_mara WITH UNIQUE KEY matnr,
        lt_makt      TYPE HASHED TABLE OF ty_makt WITH UNIQUE KEY matnr,
        lt_marc      TYPE HASHED TABLE OF ty_marc WITH UNIQUE KEY matnr,
        lt_mbew      TYPE HASHED TABLE OF ty_mbew WITH UNIQUE KEY matnr,
        lt_mard_sum  TYPE HASHED TABLE OF ty_bestand WITH UNIQUE KEY matnr,
        lt_mardh_sum TYPE HASHED TABLE OF ty_bestand WITH UNIQUE KEY matnr,
        lt_bew       TYPE SORTED TABLE OF ty_bew WITH NON-UNIQUE KEY matnr,
        lt_ekpo_open TYPE HASHED TABLE OF ty_open_qty WITH UNIQUE KEY matnr,
        lt_ekab_open TYPE HASHED TABLE OF ty_open_qty WITH UNIQUE KEY matnr,
        lt_bewegt    TYPE HASHED TABLE OF ty_mat WITH UNIQUE KEY matnr.

  FIELD-SYMBOLS: <fs_mat> TYPE ty_material_data.

  " ====== 1. MATERIALLISTE ======
  IF s_matnr IS INITIAL.
    " Alle Materialien mit Bestand im Werk
    SELECT DISTINCT matnr FROM mard
      WHERE werks = @p_werks
        AND labst > 0
      INTO TABLE @gt_mat.
  ELSE.
    SELECT matnr FROM mara
      WHERE matnr IN @s_matnr
      INTO TABLE @gt_mat.
  ENDIF.

  CHECK gt_mat IS NOT INITIAL.

  " MARA - Materialstamm
  SELECT a~matnr, a~mstae, a~meins, a~zzschrottrsk
    FROM mara AS a
    INNER JOIN @gt_mat AS m ON m~matnr = a~matnr
    INTO TABLE @lt_mara.

  " MARC - Werksdaten
  SELECT c~matnr, c~dispo
    FROM marc AS c
    INNER JOIN @gt_mat AS m ON m~matnr = c~matnr
    WHERE c~werks = @p_werks
    INTO TABLE @lt_marc.

  " MBEW - Bewertung
  SELECT b~matnr, b~bwkey, b~verpr, b~peinh, b~salk3
    FROM mbew AS b
    INNER JOIN @gt_mat AS m ON m~matnr = b~matnr
    WHERE b~bwkey = @p_werks
    INTO TABLE @lt_mbew.

  " MAKT - Texte
  SELECT t~matnr, t~maktx
    FROM makt AS t
    INNER JOIN @gt_mat AS m ON m~matnr = t~matnr
    WHERE t~spras = @sy-langu
    INTO TABLE @lt_makt.

  " MARD - aktueller Bestand je Material (Summe ueber Lagerorte)
  SELECT d~matnr, SUM( d~labst ) AS labst
    FROM mard AS d
    INNER JOIN @gt_mat AS m ON m~matnr = d~matnr
    WHERE d~werks = @p_werks
    GROUP BY d~matnr
    INTO TABLE @lt_mard_sum.

  " MARDH - Bestand zum Ende des Vormonats von p_dlow
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

  SELECT h~matnr, SUM( h~labst ) AS labst
    FROM mardh AS h
    INNER JOIN @gt_mat AS m ON m~matnr = h~matnr
    WHERE h~werks = @p_werks
      AND h~lfgja = @lv_hist_year
      AND h~lfmon = @lv_hist_month
    GROUP BY h~matnr
    INTO TABLE @lt_mardh_sum.

  " ====== 2. BEWEGUNGEN, in der Datenbank verdichtet ======
  " Je Material, Soll/Haben und Bewegungsart. BUDAT_MKPF ist das
  " Buchungsdatum des Kopfs (S/4: beides aus MATDOC), der Join auf MKPF
  " entfaellt.
  SELECT s~matnr, s~shkzg, s~bwart,
         SUM( s~menge ) AS menge,
         SUM( s~dmbtr ) AS dmbtr
    FROM mseg AS s
    INNER JOIN @gt_mat AS m ON m~matnr = s~matnr
    WHERE s~werks = @p_werks
      AND s~budat_mkpf BETWEEN @p_dlow AND @p_dhigh
    GROUP BY s~matnr, s~shkzg, s~bwart
    INTO TABLE @lt_bew.

  " Einzelbewegungen nur fuer die Debug-Ausgabe
  IF p_write = abap_true.
    SELECT s~matnr, s~bwart, s~shkzg, s~menge, s~dmbtr, s~sobkz, s~lgort
      FROM mseg AS s
      INNER JOIN @gt_mat AS m ON m~matnr = s~matnr
      WHERE s~werks = @p_werks
        AND s~budat_mkpf BETWEEN @p_dlow AND @p_dhigh
      INTO TABLE @gt_mseg_debug.
  ENDIF.

  " ====== 3. OFFENE BESTELLUNGEN (ME2M) ======
  " EKET enthält Einteilungen mit offenen Mengen
  " EKET-MENGE = Einteilungsmenge, EKET-WEMNG = bereits gelieferte Menge
  SELECT p~matnr,
         SUM( e~menge - e~wemng ) AS menge
    FROM eket AS e
    INNER JOIN ekpo AS p ON p~ebeln = e~ebeln
                        AND p~ebelp = e~ebelp
    INNER JOIN ekko AS k ON k~ebeln = p~ebeln
    INNER JOIN @gt_mat AS m ON m~matnr = p~matnr
    WHERE p~werks = @p_werks
      AND p~loekz = @space           " Position nicht gelöscht
      AND p~elikz = @space           " Position nicht erledigt
      AND k~loekz = @space           " Bestellung nicht gelöscht
      AND k~bstyp = 'F'              " Nur Normalbestellungen
      AND e~menge > e~wemng          " Nur wenn noch offen
    GROUP BY p~matnr
    INTO TABLE @lt_ekpo_open.

  " ====== 4. OFFENE RAHMENVERTRÄGE/LIEFERPLANABRUFE (ME3M) ======
  SELECT p~matnr,
         SUM( a~menge ) AS menge
    FROM ekab AS a
    INNER JOIN ekpo AS p ON p~ebeln = a~ebeln
                        AND p~ebelp = a~ebelp
    INNER JOIN @gt_mat AS m ON m~matnr = p~matnr
    WHERE p~werks = @p_werks
      AND p~loekz = @space
      AND a~menge > 0
    GROUP BY p~matnr
    INTO TABLE @lt_ekab_open.

  " ====== 5. MC46: BEWEGUNG IN DEN LETZTEN 365 TAGEN? ======
  " Bisher MAX(Buchungsdatum) ueber die ganze Historie und Vergleich mit
  " heute - 365; gleichwertig: gibt es eine Bewegung ab heute - 365.
  DATA(lv_cutoff_date) = CONV datum( sy-datum - 365 ).
  SELECT DISTINCT s~matnr
    FROM mseg AS s
    INNER JOIN @gt_mat AS m ON m~matnr = s~matnr
    WHERE s~werks = @p_werks
      AND s~budat_mkpf >= @lv_cutoff_date
    INTO TABLE @lt_bewegt.

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
    READ TABLE lt_makt WITH TABLE KEY matnr = <fs_mara>-matnr
                       ASSIGNING FIELD-SYMBOL(<fs_makt>).
    IF sy-subrc = 0.
      <fs_mat>-maktx = <fs_makt>-maktx.
    ENDIF.

    " Werksdaten
    READ TABLE lt_marc WITH TABLE KEY matnr = <fs_mara>-matnr
                       ASSIGNING FIELD-SYMBOL(<fs_marc>).
    IF sy-subrc = 0.
      <fs_mat>-disponent = <fs_marc>-dispo.
    ENDIF.

    " Bewertung
    " Bewusst wie bisher: Ohne MBEW-Zeile bleibt <fs_mbew> auf dem vorigen
    " Material stehen und wird unten weiterverwendet (bekannter Fehler,
    " saptasks/zreichw/NOTES.md Abschnitt 3 Punkt 5).
    READ TABLE lt_mbew WITH TABLE KEY matnr = <fs_mara>-matnr
                       ASSIGNING FIELD-SYMBOL(<fs_mbew>).
    IF sy-subrc = 0.
      <fs_mat>-bwtk  = <fs_mbew>-bwkey.
      <fs_mat>-waers = 'CHF'.  " Lokale Währung
    ENDIF.

    " ====== ANFANGSBESTAND AUS MARDH (historisch) ======
    DATA(lv_anfangsbestand) = CONV menge_d( 0 ).
    READ TABLE lt_mardh_sum WITH TABLE KEY matnr = <fs_mara>-matnr
                            ASSIGNING FIELD-SYMBOL(<fs_mardh>).
    IF sy-subrc = 0.
      lv_anfangsbestand = <fs_mardh>-labst.
    ENDIF.

    " Falls kein historischer Bestand gefunden, aktuellen aus MARD als Fallback
    IF lv_anfangsbestand = 0.
      READ TABLE lt_mard_sum WITH TABLE KEY matnr = <fs_mara>-matnr
                             ASSIGNING FIELD-SYMBOL(<fs_mard>).
      IF sy-subrc = 0.
        lv_anfangsbestand = <fs_mard>-labst.
      ENDIF.
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

    LOOP AT lt_bew ASSIGNING FIELD-SYMBOL(<fs_bew>)
                   WHERE matnr = <fs_mara>-matnr.

      IF <fs_bew>-shkzg = 'S'.  " Soll = Zugang
        lv_zugang  = lv_zugang + <fs_bew>-menge.
        lv_zugwert = lv_zugwert + <fs_bew>-dmbtr.
      ELSE.  " Haben = Abgang
        lv_abgang  = lv_abgang + <fs_bew>-menge.
        lv_abgwert = lv_abgwert + <fs_bew>-dmbtr.
      ENDIF.

      " Verschrottung separat tracken (BWARTs 551-558)
      IF <fs_bew>-bwart BETWEEN '551' AND '558'.
        lv_verschrott = lv_verschrott + <fs_bew>-menge.
      ENDIF.

    ENDLOOP.

    <fs_mat>-summe_zugang_n        = lv_zugang.
    <fs_mat>-summe_abgang_n        = lv_abgang.
    <fs_mat>-summe_verschrottet_n  = lv_verschrott.
    <fs_mat>-summe_abgang_minus_ver_n = lv_abgang - lv_verschrott.
    <fs_mat>-summe_zugwert_n       = lv_zugwert.
    <fs_mat>-summe_abgwert_n       = lv_abgwert.

    " ====== BESTÄNDE BERECHNEN ======
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
    READ TABLE lt_ekpo_open WITH TABLE KEY matnr = <fs_mara>-matnr
                            ASSIGNING FIELD-SYMBOL(<fs_ekpo>).
    IF sy-subrc = 0.
      <fs_mat>-me2m_offen_n = <fs_ekpo>-menge.
    ENDIF.

    READ TABLE lt_ekab_open WITH TABLE KEY matnr = <fs_mara>-matnr
                            ASSIGNING FIELD-SYMBOL(<fs_ekab>).
    IF sy-subrc = 0.
      <fs_mat>-me3m_offen_n = <fs_ekab>-menge.
    ENDIF.

    " ====== MC46 - LANGSAMDREHER CHECK ======
    " Keine Bewegung seit heute - 365 = Langsamdreher
    IF NOT line_exists( lt_bewegt[ matnr = <fs_mara>-matnr ] ).
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

  DATA: lt_mver      TYPE SORTED TABLE OF ty_mver_data WITH NON-UNIQUE KEY matnr gjahr.

  FIELD-SYMBOLS: <fs_mat> TYPE ty_material_data.

  CHECK gt_material_data IS NOT INITIAL.

  " Jahre für Selektion
  DATA(lv_year_from) = p_dlow+0(4).
  DATA(lv_year_to)   = p_dhigh+0(4).

  " MVER-Daten laden
  " Materialliste der Auswertung statt eines Range mit Einzelwerten
  SELECT v~matnr, v~gjahr,
         v~gsv01, v~gsv02, v~gsv03, v~gsv04, v~gsv05, v~gsv06,
         v~gsv07, v~gsv08, v~gsv09, v~gsv10, v~gsv11, v~gsv12
    FROM mver AS v
    INNER JOIN @gt_mat AS m ON m~matnr = v~matnr
    WHERE v~werks = @p_werks
      AND v~perkz = 'M'
      AND v~gjahr BETWEEN @lv_year_from AND @lv_year_to
    INTO TABLE @lt_mver.

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
  DATA: lv_tdname TYPE thead-tdname,
        lt_namen  TYPE HASHED TABLE OF thead-tdname WITH UNIQUE KEY table_line.

  FIELD-SYMBOLS: <fs_mat> TYPE ty_material_data.

  CHECK gt_material_data IS NOT INITIAL.

  " Welche Texte gibt es ueberhaupt? READ_TEXT nur fuer vorhandene Namen,
  " fehlende Texte bleiben wie bisher leer.
  SELECT DISTINCT tdname FROM stxh
    WHERE tdobject = 'TEXT'
      AND tdid     = 'ST'
      AND tdspras  = @sy-langu
      AND ( tdname LIKE 'ZEK%' OR tdname LIKE 'ZVK%' OR tdname LIKE 'ZFI%' )
    INTO TABLE @lt_namen.

  LOOP AT gt_material_data ASSIGNING <fs_mat>.
    " Einkaufstext
    lv_tdname = |ZEK_{ <fs_mat>-matnr }|.
    IF line_exists( lt_namen[ table_line = lv_tdname ] ).
      PERFORM read_text USING lv_tdname CHANGING <fs_mat>-komment_supply.
    ELSE.
      CLEAR <fs_mat>-komment_supply.
    ENDIF.

    " Vertriebstext
    lv_tdname = |ZVK_{ <fs_mat>-matnr }|.
    IF line_exists( lt_namen[ table_line = lv_tdname ] ).
      PERFORM read_text USING lv_tdname CHANGING <fs_mat>-komment_sales.
    ELSE.
      CLEAR <fs_mat>-komment_sales.
    ENDIF.

    " Finanztext
    lv_tdname = |ZFI_{ <fs_mat>-matnr }|.
    IF line_exists( lt_namen[ table_line = lv_tdname ] ).
      PERFORM read_text USING lv_tdname CHANGING <fs_mat>-komment_finanzen.
    ELSE.
      CLEAR <fs_mat>-komment_finanzen.
    ENDIF.
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
                          USING KEY mat
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
