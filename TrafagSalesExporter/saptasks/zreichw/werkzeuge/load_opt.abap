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
