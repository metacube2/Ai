*&---------------------------------------------------------------------*
*& Report ZHR_KPI_CONSOLIDATE
*&---------------------------------------------------------------------*
*& Konsolidiert HR-Daten für Power BI Reporting
*& - Stammdaten (PA0001, PA0002, PA0007, PA0008)
*& - Abwesenheiten (PA2001)
*& - Stellenplan (HRP1000)
*& - Platzhalter für Rexx-Daten (manuell/Excel)
*&---------------------------------------------------------------------*
REPORT zhr_kpi_consolidate.

*----------------------------------------------------------------------*
* Tabellen
*----------------------------------------------------------------------*
TABLES: pa0001, pa0002, pernr.

*----------------------------------------------------------------------*
* Typen
*----------------------------------------------------------------------*
TYPES: BEGIN OF ty_kpi,
         pernr              TYPE pa0001-pernr,        "Personalnummer
         gjahr              TYPE gjahr,               "Jahr
         monat              TYPE monat,               "Monat
         bukrs              TYPE pa0001-bukrs,        "Buchungskreis
         werks              TYPE pa0001-werks,        "Personalbereich
         btrtl              TYPE pa0001-btrtl,        "Personalteilbereich
         kostl              TYPE pa0001-kostl,        "Kostenstelle
         orgeh              TYPE pa0001-orgeh,        "Org.einheit
         plans              TYPE pa0001-plans,        "Planstelle
         stell              TYPE pa0001-stell,        "Stelle

         persk              TYPE pa0001-persk,        "Mitarbeiterkreis
         abkrs              TYPE pa0001-abkrs,        "Abrechnungskreis  <-- NEU
         teilk              TYPE pa0007-teilk,        "Teilzeitkennzeichen

         persg              TYPE pa0001-persg,        "Mitarbeitergruppe
         "persk           TYPE pa0001-persk,        "Mitarbeiterkreis
         "teilk           TYPE pa0007-teilk,        "Teilzeitkennzeichen
         empct              TYPE pa0007-empct,        "Beschäftigungsgrad %
         vorna              TYPE pa0002-vorna,        "Vorname
         nachn              TYPE pa0002-nachn,        "Nachname
         gesch              TYPE pa0002-gesch,        "Geschlecht
         gbdat              TYPE pa0002-gbdat,        "Geburtsdatum
         eintr              TYPE datum,               "Eintrittsdatum
         austr              TYPE datum,               "Austrittsdatum
         lohn_brutto        TYPE pa0008-bet01,        "Bruttolohn
         kranktage          TYPE p LENGTH 7 DECIMALS 2, "Krankheitstage gesamt
         kranktage_kurz     TYPE p LENGTH 7 DECIMALS 2, "Krankheit < 60 Tage
         kranktage_lang     TYPE p LENGTH 7 DECIMALS 2, "Krankheit >= 60 Tage
         unfalltage_nbu     TYPE p LENGTH 7 DECIMALS 2, "NBU-Tage
         unfalltage_bu      TYPE p LENGTH 7 DECIMALS 2, "BU-Tage
         ferientage         TYPE p LENGTH 7 DECIMALS 2, "Ferientage
         sonstige_abw       TYPE p LENGTH 7 DECIMALS 2, "Sonstige Abwesenheiten
         soll_stelle        TYPE c LENGTH 1,          "Soll-Stelle vorhanden (X)
         " Platzhalter für Rexx/externe Daten
         rexx_puls_score    TYPE p LENGTH 5 DECIMALS 2, "Pulsumfrage Score
         rexx_zufried_score TYPE p LENGTH 5 DECIMALS 2, "MA-Zufriedenheit
         rexx_kununu_score  TYPE p LENGTH 3 DECIMALS 1, "Kununu
         rexx_time_to_hire  TYPE i,                     "Time to hire (Tage)
         " Systemfelder
         erdat              TYPE erdat,
         erzet              TYPE erzet,
         ernam              TYPE ernam,
       END OF ty_kpi.

DATA: gt_kpi TYPE TABLE OF ty_kpi,
      gs_kpi TYPE ty_kpi.

*----------------------------------------------------------------------*
* Selektionsbild
*----------------------------------------------------------------------*
SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE TEXT-001.
  SELECT-OPTIONS: s_pernr FOR pernr-pernr,
                  s_bukrs FOR pa0001-bukrs,
                  s_werks FOR pa0001-werks,
                  s_kostl FOR pa0001-kostl,
                  s_orgeh FOR pa0001-orgeh.
  PARAMETERS: p_gjahr TYPE gjahr DEFAULT sy-datum+0(4),
              p_monat TYPE monat DEFAULT sy-datum+4(2).
SELECTION-SCREEN END OF BLOCK b1.

SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE TEXT-002.
  PARAMETERS: p_alv  AS CHECKBOX DEFAULT 'X',
              p_save AS CHECKBOX,
              p_csv  AS CHECKBOX.  "NEU: CSV Download
SELECTION-SCREEN END OF BLOCK b2.

*----------------------------------------------------------------------*
* Initialisierung
*----------------------------------------------------------------------*
INITIALIZATION.
* Texte werden in Textelementen gepflegt (SE38 -> Goto -> Text Elements)

*----------------------------------------------------------------------*
* Start
*----------------------------------------------------------------------*
START-OF-SELECTION.
  PERFORM get_data.

  IF gt_kpi IS INITIAL.
    MESSAGE 'Keine Daten gefunden' TYPE 'S' DISPLAY LIKE 'W'.
    RETURN.
  ENDIF.

  IF p_csv = 'X'.
    PERFORM download_csv.
  ENDIF.



  IF p_alv = 'X'.
    PERFORM show_alv.
  ENDIF.

  IF p_save = 'X'.
    PERFORM save_data.
  ENDIF.

*&---------------------------------------------------------------------*
*& Form GET_DATA
*&---------------------------------------------------------------------*
FORM get_data.
  DATA: lt_pa0001    TYPE TABLE OF pa0001,
        lt_pa0002    TYPE TABLE OF pa0002,
        lt_pa0007    TYPE TABLE OF pa0007,
        lt_pa0008    TYPE TABLE OF pa0008,
        lt_pa0000    TYPE TABLE OF pa0000,
        lt_pa2001    TYPE TABLE OF pa2001,
        lt_hrp1000   TYPE TABLE OF hrp1000,
        ls_pa0001    TYPE pa0001,
        ls_pa0002    TYPE pa0002,
        ls_pa0007    TYPE pa0007,
        ls_pa0008    TYPE pa0008,
        ls_pa0000    TYPE pa0000,
        ls_pa2001    TYPE pa2001,
        ls_hrp1000   TYPE hrp1000,
        lv_datum_von TYPE datum,
        lv_datum_bis TYPE datum,
        lv_kaltag    TYPE p LENGTH 7 DECIMALS 2.

  " Zeitraum für Monat berechnen
  CONCATENATE p_gjahr p_monat '01' INTO lv_datum_von.
  CALL FUNCTION 'RP_LAST_DAY_OF_MONTHS'
    EXPORTING
      day_in            = lv_datum_von
    IMPORTING
      last_day_of_month = lv_datum_bis.

  " Aktive Mitarbeiter aus PA0001 (Stichtag = letzter Tag des Monats)
  SELECT * FROM pa0001 INTO TABLE lt_pa0001
    WHERE pernr IN s_pernr
      AND bukrs IN s_bukrs
      AND werks IN s_werks
      AND kostl IN s_kostl
      AND orgeh IN s_orgeh
      AND begda <= lv_datum_bis
      AND endda >= lv_datum_bis.

  IF lt_pa0001 IS INITIAL.
    RETURN.
  ENDIF.

  " Persönliche Daten PA0002
  SELECT * FROM pa0002 INTO TABLE lt_pa0002
    FOR ALL ENTRIES IN lt_pa0001
    WHERE pernr = lt_pa0001-pernr
      AND begda <= lv_datum_bis
      AND endda >= lv_datum_bis.

  " Sollarbeitszeit PA0007
  SELECT * FROM pa0007 INTO TABLE lt_pa0007
    FOR ALL ENTRIES IN lt_pa0001
    WHERE pernr = lt_pa0001-pernr
      AND begda <= lv_datum_bis
      AND endda >= lv_datum_bis.

  " Basisbezüge PA0008
  SELECT * FROM pa0008 INTO TABLE lt_pa0008
    FOR ALL ENTRIES IN lt_pa0001
    WHERE pernr = lt_pa0001-pernr
      AND begda <= lv_datum_bis
      AND endda >= lv_datum_bis.

  " Massnahmen PA0000 (für Ein-/Austritt)
  SELECT * FROM pa0000 INTO TABLE lt_pa0000
    FOR ALL ENTRIES IN lt_pa0001
    WHERE pernr = lt_pa0001-pernr
      AND massn IN ('01', '02', 'Z1', 'Z2'). "Eintritt/Austritt

  " Abwesenheiten PA2001 für den Monat
  SELECT * FROM pa2001 INTO TABLE lt_pa2001
    FOR ALL ENTRIES IN lt_pa0001
    WHERE pernr = lt_pa0001-pernr
      AND begda <= lv_datum_bis
      AND endda >= lv_datum_von.

  " Planstellen HRP1000
  SELECT * FROM hrp1000 INTO TABLE lt_hrp1000
    WHERE otype = 'S'
      AND begda <= lv_datum_bis
      AND endda >= lv_datum_bis.

  " Daten zusammenführen
  LOOP AT lt_pa0001 INTO ls_pa0001.
    CLEAR gs_kpi.

    " Grunddaten
    gs_kpi-pernr = ls_pa0001-pernr.
    gs_kpi-gjahr = p_gjahr.
    gs_kpi-monat = p_monat.
    gs_kpi-bukrs = ls_pa0001-bukrs.
    gs_kpi-werks = ls_pa0001-werks.
    gs_kpi-btrtl = ls_pa0001-btrtl.
    gs_kpi-kostl = ls_pa0001-kostl.
    gs_kpi-orgeh = ls_pa0001-orgeh.
    gs_kpi-plans = ls_pa0001-plans.
    gs_kpi-stell = ls_pa0001-stell.
    gs_kpi-persg = ls_pa0001-persg.
    gs_kpi-persk = ls_pa0001-persk.


    gs_kpi-abkrs = ls_pa0001-abkrs.

    " Persönliche Daten
    READ TABLE lt_pa0002 INTO ls_pa0002 WITH KEY pernr = ls_pa0001-pernr.
    IF sy-subrc = 0.
      gs_kpi-vorna = ls_pa0002-vorna.
      gs_kpi-nachn = ls_pa0002-nachn.
      gs_kpi-gesch = ls_pa0002-gesch.
      gs_kpi-gbdat = ls_pa0002-gbdat.
    ENDIF.

    " Sollarbeitszeit / Beschäftigungsgrad
    READ TABLE lt_pa0007 INTO ls_pa0007 WITH KEY pernr = ls_pa0001-pernr.
    IF sy-subrc = 0.
      gs_kpi-teilk = ls_pa0007-teilk.
      gs_kpi-empct = ls_pa0007-empct.
    ENDIF.

    " Bruttolohn
    READ TABLE lt_pa0008 INTO ls_pa0008 WITH KEY pernr = ls_pa0001-pernr.
    IF sy-subrc = 0.
      gs_kpi-lohn_brutto = ls_pa0008-bet01.
    ENDIF.

    " Ein-/Austrittsdatum
    LOOP AT lt_pa0000 INTO ls_pa0000 WHERE pernr = ls_pa0001-pernr.
      IF ls_pa0000-massn = '01' OR ls_pa0000-massn = 'Z1'. "Eintritt
        IF gs_kpi-eintr IS INITIAL OR ls_pa0000-begda < gs_kpi-eintr.
          gs_kpi-eintr = ls_pa0000-begda.
        ENDIF.
      ELSEIF ls_pa0000-massn = '02' OR ls_pa0000-massn = 'Z2'. "Austritt
        IF gs_kpi-austr IS INITIAL OR ls_pa0000-begda > gs_kpi-austr.
          gs_kpi-austr = ls_pa0000-begda.
        ENDIF.
      ENDIF.
    ENDLOOP.

    " Abwesenheiten aggregieren
    LOOP AT lt_pa2001 INTO ls_pa2001 WHERE pernr = ls_pa0001-pernr.
      " Kalendertage berechnen (nur für den Monat)
      DATA: lv_abw_von TYPE datum,
            lv_abw_bis TYPE datum.

      lv_abw_von = ls_pa2001-begda.
      lv_abw_bis = ls_pa2001-endda.

      " Auf Monatsgrenzen beschränken
      IF lv_abw_von < lv_datum_von.
        lv_abw_von = lv_datum_von.
      ENDIF.
      IF lv_abw_bis > lv_datum_bis.
        lv_abw_bis = lv_datum_bis.
      ENDIF.

      lv_kaltag = lv_abw_bis - lv_abw_von + 1.
      IF lv_kaltag < 0.
        lv_kaltag = 0.
      ENDIF.

      CASE ls_pa2001-awart.
          " Krankheit
        WHEN '0220' OR '0230' OR '0240' OR '0260'. "Kurz (< 60 Tage)
          gs_kpi-kranktage_kurz = gs_kpi-kranktage_kurz + lv_kaltag.
          gs_kpi-kranktage = gs_kpi-kranktage + lv_kaltag.
        WHEN '0270'. "Lang (>= 60 Tage)
          gs_kpi-kranktage_lang = gs_kpi-kranktage_lang + lv_kaltag.
          gs_kpi-kranktage = gs_kpi-kranktage + lv_kaltag.
          " NBU (Nichtberufsunfall)
        WHEN '0350' OR '0360'.
          gs_kpi-unfalltage_nbu = gs_kpi-unfalltage_nbu + lv_kaltag.
          " BU (Berufsunfall)
        WHEN '0280' OR '0290' OR '0300' OR '0310'.
          gs_kpi-unfalltage_bu = gs_kpi-unfalltage_bu + lv_kaltag.
          " Ferien
        WHEN '0100'.
          gs_kpi-ferientage = gs_kpi-ferientage + lv_kaltag.
          " Sonstige
        WHEN OTHERS.
          gs_kpi-sonstige_abw = gs_kpi-sonstige_abw + lv_kaltag.
      ENDCASE.
    ENDLOOP.

    " Prüfen ob Soll-Stelle existiert
    READ TABLE lt_hrp1000 INTO ls_hrp1000 WITH KEY objid = ls_pa0001-plans.
    IF sy-subrc = 0.
      gs_kpi-soll_stelle = 'X'.
    ENDIF.

    " Systemfelder
    gs_kpi-erdat = sy-datum.
    gs_kpi-erzet = sy-uzeit.
    gs_kpi-ernam = sy-uname.

    APPEND gs_kpi TO gt_kpi.
  ENDLOOP.

ENDFORM.

*&---------------------------------------------------------------------*
*& Form SHOW_ALV
*&---------------------------------------------------------------------*
FORM show_alv.
  DATA: lo_alv       TYPE REF TO cl_salv_table,
        lo_columns   TYPE REF TO cl_salv_columns_table,
        lo_column    TYPE REF TO cl_salv_column,
        lo_functions TYPE REF TO cl_salv_functions_list,
        lo_display   TYPE REF TO cl_salv_display_settings,
        lx_msg       TYPE REF TO cx_salv_msg.

  TRY.
      cl_salv_table=>factory(
        IMPORTING
          r_salv_table = lo_alv
        CHANGING
          t_table      = gt_kpi ).

      " Funktionen aktivieren (Export, Filter, Sort)
      lo_functions = lo_alv->get_functions( ).
      lo_functions->set_all( abap_true ).

      " Spaltenüberschriften
      lo_columns = lo_alv->get_columns( ).
      lo_columns->set_optimize( abap_true ).

      TRY.
          lo_column = lo_columns->get_column( 'PERNR' ).
          lo_column->set_short_text( 'PNR' ).
          lo_column->set_medium_text( 'Pers.Nr.' ).
          lo_column->set_long_text( 'Personalnummer' ).

          lo_column = lo_columns->get_column( 'KRANKTAGE' ).
          lo_column->set_short_text( 'Krank' ).
          lo_column->set_medium_text( 'Krankheitstage' ).
          lo_column->set_long_text( 'Krankheitstage gesamt' ).

          lo_column = lo_columns->get_column( 'KRANKTAGE_KURZ' ).
          lo_column->set_short_text( 'KrKurz' ).
          lo_column->set_medium_text( 'Krank <60T' ).
          lo_column->set_long_text( 'Krankheit < 60 Tage' ).

          lo_column = lo_columns->get_column( 'KRANKTAGE_LANG' ).
          lo_column->set_short_text( 'KrLang' ).
          lo_column->set_medium_text( 'Krank >=60T' ).
          lo_column->set_long_text( 'Krankheit >= 60 Tage (LZK)' ).

          lo_column = lo_columns->get_column( 'UNFALLTAGE_NBU' ).
          lo_column->set_short_text( 'NBU' ).
          lo_column->set_medium_text( 'NBU-Tage' ).
          lo_column->set_long_text( 'Nichtberufsunfall Tage' ).

          lo_column = lo_columns->get_column( 'UNFALLTAGE_BU' ).
          lo_column->set_short_text( 'BU' ).
          lo_column->set_medium_text( 'BU-Tage' ).
          lo_column->set_long_text( 'Berufsunfall Tage' ).

          lo_column = lo_columns->get_column( 'EMPCT' ).
          lo_column->set_short_text( 'BG%' ).
          lo_column->set_medium_text( 'Besch.grad' ).
          lo_column->set_long_text( 'Beschäftigungsgrad %' ).

          lo_column = lo_columns->get_column( 'LOHN_BRUTTO' ).
          lo_column->set_short_text( 'Lohn' ).
          lo_column->set_medium_text( 'Bruttolohn' ).
          lo_column->set_long_text( 'Bruttolohn Monat' ).

          lo_column = lo_columns->get_column( 'SOLL_STELLE' ).
          lo_column->set_short_text( 'Soll' ).
          lo_column->set_medium_text( 'Soll-Stelle' ).
          lo_column->set_long_text( 'Soll-Stelle vorhanden' ).

          " Rexx-Felder (leer, aber sichtbar)
          lo_column = lo_columns->get_column( 'REXX_PULS_SCORE' ).
          lo_column->set_short_text( 'Puls' ).
          lo_column->set_medium_text( 'Pulsumfrage' ).
          lo_column->set_long_text( 'Pulsumfrage Score (Rexx)' ).

          lo_column = lo_columns->get_column( 'REXX_ZUFRIED_SCORE' ).
          lo_column->set_short_text( 'Zufr.' ).
          lo_column->set_medium_text( 'Zufriedenh.' ).
          lo_column->set_long_text( 'MA-Zufriedenheit (Rexx)' ).

          lo_column = lo_columns->get_column( 'REXX_KUNUNU_SCORE' ).
          lo_column->set_short_text( 'Kununu' ).
          lo_column->set_medium_text( 'Kununu' ).
          lo_column->set_long_text( 'Kununu Score' ).

        CATCH cx_salv_not_found.
      ENDTRY.

      " Titel
      lo_display = lo_alv->get_display_settings( ).
      lo_display->set_list_header( 'HR KPI Konsolidierung' ).
      lo_display->set_striped_pattern( abap_true ).

      lo_alv->display( ).

    CATCH cx_salv_msg INTO lx_msg.
      MESSAGE lx_msg TYPE 'E'.
  ENDTRY.

ENDFORM.

*&---------------------------------------------------------------------*
*& Form SAVE_DATA
*&---------------------------------------------------------------------*
*& Aktivieren wenn Tabelle ZHRKPI_CONSOLIDATED in SE11 angelegt wurde
*&---------------------------------------------------------------------*
FORM save_data.
*  DATA: lv_count TYPE i.
*
*  " Alte Daten für den Monat löschen
*  DELETE FROM zhrkpi_consolidated
*    WHERE gjahr = p_gjahr
*      AND monat = p_monat.
*
*  " Neue Daten einfügen
*  INSERT zhrkpi_consolidated FROM TABLE gt_kpi.
*
*  IF sy-subrc = 0.
*    lv_count = lines( gt_kpi ).
*    COMMIT WORK.
*    MESSAGE |{ lv_count } Datensätze in ZHRKPI_CONSOLIDATED gespeichert| TYPE 'S'.
*  ELSE.
*    ROLLBACK WORK.
*    MESSAGE 'Fehler beim Speichern' TYPE 'E'.
*  ENDIF.
  MESSAGE 'Tabelle ZHRKPI_CONSOLIDATED muss erst in SE11 angelegt werden' TYPE 'S' DISPLAY LIKE 'W'.
ENDFORM.



  FORM download_csv.
    DATA: lt_csv      TYPE TABLE OF string,
          lv_line     TYPE string,
          lv_filename TYPE string,
          lv_path     TYPE string,
          lv_fullpath TYPE string,
          lv_action   TYPE i,
          lv_datum    TYPE string,
          lv_decimal  TYPE string.

    FIELD-SYMBOLS: <fs_kpi> TYPE ty_kpi.

    " Dateiname vorschlagen
    lv_filename = |HR_KPI_{ p_gjahr }{ p_monat }.csv|.

    " Speicherdialog
    cl_gui_frontend_services=>file_save_dialog(
      EXPORTING
        default_file_name = lv_filename
        default_extension = 'csv'
        file_filter       = 'CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*'
      CHANGING
        filename          = lv_filename
        path              = lv_path
        fullpath          = lv_fullpath
        user_action       = lv_action ).

    IF lv_action <> cl_gui_frontend_services=>action_ok.
      MESSAGE 'Download abgebrochen' TYPE 'S'.
      RETURN.
    ENDIF.

    " UTF-8 BOM
    APPEND cl_abap_char_utilities=>byte_order_mark_utf8 TO lt_csv.

    " Spaltenüberschriften
    lv_line = 'Personalnummer;Jahr;Monat;Buchungskreis;Personalbereich;' &&
              'Personalteilbereich;Kostenstelle;Organisationseinheit;' &&
              'Planstelle;Stelle;Mitarbeitergruppe;Mitarbeiterkreis;' &&
              'Abrechnungskreis;Teilzeitkennzeichen;Beschaeftigungsgrad_Prozent;' &&
              'Vorname;Nachname;Geschlecht;Geburtsdatum;' &&
              'Eintrittsdatum;Austrittsdatum;Bruttolohn;' &&
              'Krankheitstage_Gesamt;Krankheitstage_Kurz;Krankheitstage_Lang;' &&
              'NBU_Tage;BU_Tage;Ferientage;Sonstige_Abwesenheiten;' &&
              'Soll_Stelle;Pulsumfrage_Score;Zufriedenheit_Score;' &&
              'Kununu_Score;Time_to_Hire'.
    APPEND lv_line TO lt_csv.

    " Datenzeilen
    LOOP AT gt_kpi ASSIGNING <fs_kpi>.
      CLEAR lv_line.

      lv_line = <fs_kpi>-pernr && ';' &&
                <fs_kpi>-gjahr && ';' &&
                <fs_kpi>-monat && ';' &&
                <fs_kpi>-bukrs && ';' &&
                <fs_kpi>-werks && ';' &&
                <fs_kpi>-btrtl && ';' &&
                <fs_kpi>-kostl && ';' &&
                <fs_kpi>-orgeh && ';' &&
                <fs_kpi>-plans && ';' &&
                <fs_kpi>-stell && ';' &&
                <fs_kpi>-persg && ';' &&
                <fs_kpi>-persk && ';' &&
                <fs_kpi>-abkrs && ';' &&
                <fs_kpi>-teilk && ';'.

      " Beschäftigungsgrad mit Komma
      PERFORM format_decimal USING <fs_kpi>-empct CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      lv_line = lv_line &&
                <fs_kpi>-vorna && ';' &&
                <fs_kpi>-nachn && ';' &&
                <fs_kpi>-gesch && ';'.

      " Geburtsdatum
      PERFORM format_date USING <fs_kpi>-gbdat CHANGING lv_datum.
      lv_line = lv_line && lv_datum && ';'.

      " Eintrittsdatum
      PERFORM format_date USING <fs_kpi>-eintr CHANGING lv_datum.
      lv_line = lv_line && lv_datum && ';'.

      " Austrittsdatum
      PERFORM format_date USING <fs_kpi>-austr CHANGING lv_datum.
      lv_line = lv_line && lv_datum && ';'.

      " Bruttolohn mit Komma
      PERFORM format_decimal USING <fs_kpi>-lohn_brutto CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      " Abwesenheitstage mit Komma
      PERFORM format_decimal USING <fs_kpi>-kranktage CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      PERFORM format_decimal USING <fs_kpi>-kranktage_kurz CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      PERFORM format_decimal USING <fs_kpi>-kranktage_lang CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      PERFORM format_decimal USING <fs_kpi>-unfalltage_nbu CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      PERFORM format_decimal USING <fs_kpi>-unfalltage_bu CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      PERFORM format_decimal USING <fs_kpi>-ferientage CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      PERFORM format_decimal USING <fs_kpi>-sonstige_abw CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      " Soll-Stelle
      lv_line = lv_line && <fs_kpi>-soll_stelle && ';'.

      " Rexx-Platzhalter
      PERFORM format_decimal USING <fs_kpi>-rexx_puls_score CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      PERFORM format_decimal USING <fs_kpi>-rexx_zufried_score CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      PERFORM format_decimal USING <fs_kpi>-rexx_kununu_score CHANGING lv_decimal.
      lv_line = lv_line && lv_decimal && ';'.

      lv_line = lv_line && <fs_kpi>-rexx_time_to_hire.

      APPEND lv_line TO lt_csv.
    ENDLOOP.

    " Datei speichern
    cl_gui_frontend_services=>gui_download(
      EXPORTING
        filename = lv_fullpath
        filetype = 'ASC'
        codepage = '4110'
      CHANGING
        data_tab = lt_csv ).

    IF sy-subrc = 0.
      MESSAGE |CSV exportiert: { lv_fullpath }| TYPE 'S'.
    ELSE.
      MESSAGE 'Fehler beim Export' TYPE 'E'.
    ENDIF.

  ENDFORM.

*&---------------------------------------------------------------------*
*& Form FORMAT_DATE
*&---------------------------------------------------------------------*
*& Datum in DD.MM.YYYY formatieren
*&---------------------------------------------------------------------*
FORM format_date USING iv_date TYPE datum
                 CHANGING cv_result TYPE string.
  IF iv_date IS INITIAL OR iv_date = '00000000'.
    cv_result = ''.
  ELSE.
    cv_result = iv_date+6(2) && '.' && iv_date+4(2) && '.' && iv_date+0(4).
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
*& Form FORMAT_DECIMAL
*&---------------------------------------------------------------------*
*& Dezimalzahl mit Komma formatieren (CH-Standard)
*&---------------------------------------------------------------------*
FORM format_decimal USING iv_value TYPE any
                    CHANGING cv_result TYPE string.
  DATA: lv_temp TYPE string.

  lv_temp = iv_value.
  CONDENSE lv_temp NO-GAPS.

  " Punkt durch Komma ersetzen
  REPLACE ALL OCCURRENCES OF '.' IN lv_temp WITH ','.

  " Führende Nullen bei 0-Werten
  IF lv_temp = '0,00' OR lv_temp = '0'.
    lv_temp = '0'.
  ENDIF.

  cv_result = lv_temp.
ENDFORM.