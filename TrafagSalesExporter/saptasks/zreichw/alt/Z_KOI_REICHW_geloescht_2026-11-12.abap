
*&---------------------------------------------------------------------*
*& Report Z_KOI_REICHW
*&---------------------------------------------------------------------*
*&
*&---------------------------------------------------------------------*
REPORT z_koi_reichw.



INCLUDE z_koi_mainfuncw.

TYPES: BEGIN OF ty_matnr,
         matnr TYPE matnr,
         mstae TYPE mstae,
       END OF ty_matnr,
       BEGIN OF ty_used_in,
         matnr TYPE matnr,
         stlnr TYPE mast-stlnr,
         mstae TYPE mstae,
       END OF ty_used_in,
       BEGIN OF ty_bom,
         matnr TYPE matnr,  " Materialnummer
         idnrk TYPE idnrk,  " Komponentennummer
         menge TYPE c LENGTH 2,      " Menge
       END OF ty_bom,
       BEGIN OF ty_material,
         matnr TYPE matnr,  " Materialnummer
         mstae TYPE mstae,
       END OF ty_material.
TABLES: mara, marc, mard, mbew.
TYPES: ty_bom_tab TYPE STANDARD TABLE OF ty_bom WITH DEFAULT KEY.
DATA: lv_konto     TYPE bseg-hkont,      " Konto
      lv_wert_fr   TYPE mbew-salk3,    " Wert in CHF
      lv_menge_lme TYPE mard-labst.  " Menge in LME

"==== TYPES: ty_material_data (vollständig, mit _N Feldern) ====================
TYPES: BEGIN OF ty_material_data,
         mstae                    TYPE mstae,
         report                   TYPE string,      " Report Name
         erstelldatum             TYPE dats,       " Erstellungsdatum
         bwtk                     TYPE matnr,      " Bewertungstyp
         matnr                    TYPE matnr,      " Material
         maktx                    TYPE maktx,      " Materialkurztext

         anfangsbestand           TYPE string,     " Anfangsbestand (Text)
         summe_zugang             TYPE string,     " Summe Zugang (Text)
         summe_abgang             TYPE string,     " Summe Abgang (Text)
         summe_abgang_minus_ver   TYPE string,     " Abgang minus Verschrottung (Text)
         reichw_m_verschr         TYPE string,     " Reichweite mit Verschrottung (Text)
         summe_verschrottet       TYPE string,     " Summe Verschrottet (Text)
         endbestand               TYPE string,     " Endbestand (Text)

         bme                      TYPE string,     " Basismengeneinheit
         anfangswert              TYPE string,     " Anfangswert (Text)
         summe_zugwert            TYPE string,     " Summe Zugangswert (Text)
         summe_abgwert            TYPE string,     " Summe Abgangswert (Text)
         endwert                  TYPE string,     " Endwert (Text)
         waers                    TYPE string,     " Währung

         datum_von                TYPE string,     " Datum von
         datum_bis                TYPE string,     " Datum bis

         preis_stk                TYPE string,     " Preis/Stück (Text)
         status                   TYPE string,     " Status
         disponent                TYPE string,     " Disponent

         me2m_offen               TYPE string,     " ME2M offene Menge (Text)
         me3m_offen               TYPE string,     " ME3M offene Menge (Text)
         mc46_nomove              TYPE string,     " MC46 - 365Tg no-mover

         reichweite               TYPE string,     " Reichweite MTE (Text)

         xyz_klasse               TYPE c LENGTH 1, " XYZ-Klasse
         komment_supply           TYPE string,     " Einkaufstext (ZEK_)
         komment_sales            TYPE string,     " Vertriebstext (ZVK_)
         komment_finanzen         TYPE string,     " Finanztext (ZFI_)
         zzschrottrsk             TYPE mara-zzschrottrsk, " Verschrottungsrisiko

         " --- NEU: numerische Spiegel-Felder für sauberes ALV/Export ---
         anfangsbestand_n         TYPE p LENGTH 16 DECIMALS 3,
         summe_zugang_n           TYPE p LENGTH 16 DECIMALS 3,
         summe_abgang_n           TYPE p LENGTH 16 DECIMALS 3,
         summe_abgang_minus_ver_n TYPE p LENGTH 16 DECIMALS 3,
         summe_verschrottet_n     TYPE p LENGTH 16 DECIMALS 3,
         endbestand_n             TYPE p LENGTH 16 DECIMALS 3,

         anfangswert_n            TYPE p LENGTH 16 DECIMALS 2,
         summe_zugwert_n          TYPE p LENGTH 16 DECIMALS 2,
         summe_abgwert_n          TYPE p LENGTH 16 DECIMALS 2,
         endwert_n                TYPE p LENGTH 16 DECIMALS 2,
         preis_stk_n              TYPE p LENGTH 16 DECIMALS 2,

         me2m_offen_n             TYPE p LENGTH 16 DECIMALS 3,
         me3m_offen_n             TYPE p LENGTH 16 DECIMALS 3,

         reichweite_n             TYPE p LENGTH 8  DECIMALS 1,
         reichw_m_verschr_n       TYPE p LENGTH 8  DECIMALS 1,
       END OF ty_material_data.


TYPES: BEGIN OF ty_mver_pivot,
         matnr TYPE mver-matnr,
         werks TYPE mver-werks, " .Werk ist essentiell für die Verknüpfung
         gjahr TYPE mver-gjahr,
         perkz TYPE mver-perkz, " .Periodenkennzeichen (z.B. 'M' für Monat)
         gsv01 TYPE mver-gsv01,
         gsv02 TYPE mver-gsv02,
         gsv03 TYPE mver-gsv03,
         gsv04 TYPE mver-gsv04,
         gsv05 TYPE mver-gsv05,
         gsv06 TYPE mver-gsv06,
         gsv07 TYPE mver-gsv07,
         gsv08 TYPE mver-gsv08,
         gsv09 TYPE mver-gsv09,
         gsv10 TYPE mver-gsv10,
         gsv11 TYPE mver-gsv11,
         gsv12 TYPE mver-gsv12,
         gsv13 TYPE mver-gsv13,
         " .Fügen Sie hier ggf. GSV14, GSV15, GSV16 hinzu, falls Ihr System mehr Perioden hat.
       END OF ty_mver_pivot.


TYPES: BEGIN OF ty_reservation,
         werks TYPE resb-werks,
         lgort TYPE resb-lgort,
         matnr TYPE resb-matnr,
         bdmng TYPE resb-bdmng,
       END OF ty_reservation.

TYPES: BEGIN OF ty_xtab1,
         werks LIKE resb-werks,
         lgort LIKE resb-lgort,
         charg LIKE resb-charg,
         matnr LIKE resb-matnr,
         bdmng LIKE resb-bdmng,
         bdmns LIKE resb-bdmng,
         erfme LIKE resb-erfme,
         erfmg LIKE resb-erfmg,
       END OF ty_xtab1.

DATA: lt_abc_result TYPE zz_cl_koi_mainfunc=>tt_abc_result, " .Typ aus der Klasse verwenden
      lv_werk       TYPE werks_d.                         " .Werk für die Analyse

FIELD-SYMBOLS: <fs_materialg> TYPE ty_material_data,
               <fs_abc>       LIKE LINE OF lt_abc_result.

DATA: gt_material_data TYPE TABLE OF ty_material_data,
      gs_material_data TYPE ty_material_data,
      itt_materials    TYPE TABLE OF ty_material.


DATA: gt_material_data_new TYPE TABLE OF ty_material_data.


SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE TEXT-001.

  SELECT-OPTIONS: s_matnr FOR mara-matnr
               "   s_werks FOR marc-werks,

                ."  s_lgort FOR mard-lgort.
  "PARAMETERS  p_zeitr TYPE string  DEFAULT '12'.
  " PARAMETERS  p_path TYPE string LOWER CASE DEFAULT 'c:\temp\down.csv'.

SELECTION-SCREEN END OF BLOCK b1.

SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE TEXT-002.

  PARAMETERS  p_act AS CHECKBOX DEFAULT ' '.
  PARAMETERS:
    p_dlow  TYPE datum DEFAULT sy-datum,  "Datum von
    p_dhigh TYPE datum DEFAULT sy-datum. "Datum bis

SELECTION-SCREEN END OF BLOCK b2.


SELECTION-SCREEN BEGIN OF BLOCK info WITH FRAME TITLE TEXT-t01 NO INTERVALS. " Optional: Rahmen drum
  SELECTION-SCREEN COMMENT /1(54) TEXT-i06. " Erste Zeile, Position 1, Länge 75
  SELECTION-SCREEN COMMENT /1(54) TEXT-i07. " Erste Zeile, Position 1, Länge 75
  SELECTION-SCREEN COMMENT /1(54) TEXT-i01. " Erste Zeile, Position 1, Länge 75
  SELECTION-SCREEN COMMENT /1(54) TEXT-i02. " Zweite Zeile
  SELECTION-SCREEN COMMENT /1(54) TEXT-i03. " Dritte Zeile
  SELECTION-SCREEN COMMENT /1(54) TEXT-i04. " Vierte Zeile
  SELECTION-SCREEN COMMENT /1(54) TEXT-i05. " Fünfte Zeile
SELECTION-SCREEN END OF BLOCK info. " Optional: Ende des Rahmens

START-OF-SELECTION.
  SET TITLEBAR 'Reichweitenreport'.

  DATA: go_koi_mainfunc TYPE REF TO zz_cl_koi_mainfunc.
  CREATE OBJECT go_koi_mainfunc.

*
*  PERFORM get_material_data.
*  IF gt_material_data IS NOT INITIAL. " .Nur ausführen, wenn Daten vorhanden
*    PERFORM load_material_texts.  " NEU
*    PERFORM calculate_xyz_analysis. " .NEU: XYZ-Analyse durchführen
*  ENDIF.
  " Prüfen ob berechnete Daten vorhanden sind (Fall 99)
  SELECT SINGLE feld1 FROM zlagcleanup
    INTO @DATA(lv_exists)
    WHERE fall = '99'.

  IF sy-subrc = 0 AND p_act = ' '. " Daten vorhanden und kein Neu-Ermitteln
    " Lade gespeicherte berechnete Daten
    PERFORM read_calcu_f_tlbk.
    MESSAGE i001(00) WITH 'Berechnete Daten aus ZLAGCLEANUP geladen'.
  ELSE.
    " Normale Verarbeitung
    PERFORM get_material_data.
    IF gt_material_data IS NOT INITIAL.
      PERFORM load_material_texts.
      PERFORM calculate_xyz_analysis.

      " NEU: Speichere berechnete Daten
      PERFORM save_calcc_da_to_lbk.
    ENDIF.
  ENDIF.


  " .ABC-Analyse durchführen über die neue Klassenmethode
  TRY.
      " Erst eine korrekte Material-Tabelle im passenden Format aufbauen
      DATA: lt_materials_for_abc TYPE zz_cl_koi_mainfunc=>ty_all_material.

      " Daten für ABC-Analyse umwandeln
      LOOP AT gt_material_data ASSIGNING FIELD-SYMBOL(<fs_mat>).
        APPEND INITIAL LINE TO lt_materials_for_abc ASSIGNING FIELD-SYMBOL(<fs_abc_mat>).
        <fs_abc_mat>-matnr = <fs_mat>-matnr.
        " Falls mstae in Datenstruktur vorhanden ist, hier auch kopieren
        " <fs_abc_mat>-mstae = <fs_mat>-mstae.
      ENDLOOP.

      " Standardwerk festlegen (falls keine Variable existiert)
      IF lv_werk IS INITIAL.
        lv_werk = '1100'.  " Standard-Werk für die Analyse
      ENDIF.

      " ABC-Analyse mit korrekten Parametern aufrufen
      " RICHTIG - mit Datumsgrenzen:
      lt_abc_result = go_koi_mainfunc->get_abc_classification(
                         it_materials = lt_materials_for_abc
                         iv_werks     = lv_werk
                         iv_date_from = p_dlow    " Datumsgrenzen übergeben
                         iv_date_to   = p_dhigh   " Datumsgrenzen übergeben
                      ).


      " Ergebnisse der ABC-Analyse in Haupttabelle integrieren
      LOOP AT lt_abc_result ASSIGNING <fs_abc>.
        " Hier direkt ein Feldymbol auf das passende Feld zuweisen
        LOOP AT gt_material_data ASSIGNING <fs_mat> WHERE matnr = <fs_abc>-matnr.
          " Direkt das ABC-Feld des Materials setzen (falls diese Komponente existiert)
          " Die Feldkomponente muss in ty_material_data definiert sein, z.B.:
          " abc_klasse TYPE c LENGTH 1,
          <fs_mat>-xyz_klasse = <fs_abc>-abc_klasse.  " Ich verwende hier xyz_klasse, da dies existiert
        ENDLOOP.
      ENDLOOP.

    CATCH cx_root INTO DATA(lx_abc_error).
      MESSAGE lx_abc_error->get_text( ) TYPE 'W'.
      " Verarbeitung fortsetzen, aber ABC-Klassen fehlen
  ENDTRY.




  PERFORM display_alv.
  "PERFORM create_csv_file.




FORM Clear_Spool.
  DATA: lt_spool_list    TYPE TABLE OF tsp01,
        ls_spool         TYPE tsp01,
        lv_spoolnr       TYPE rqident,
        lv_error_message TYPE string.

  "Alle Spoolaufträge des Users selektieren
  SELECT *
    FROM tsp01
    INTO TABLE lt_spool_list
    WHERE rqowner = sy-uname.

  "Jeden gefundenen Spoolauftrag löschen
  LOOP AT lt_spool_list INTO ls_spool.
    lv_spoolnr    = ls_spool-rqident.

    CALL FUNCTION 'RSPO_R_RDELETE_SPOOLREQ'
      EXPORTING
        spoolid = lv_spoolnr  "Korrekter Typ, keine Konvertierung nötig
      EXCEPTIONS
        OTHERS  = 1.

    IF sy-subrc <> 0.
      "Write: / 'Fehler beim Löschen des Spoolauftrags:', ls_spool-rqident.
    ENDIF.
  ENDLOOP.

  IF sy-subrc = 0.
    "Write: / 'Alle Spoolaufträge wurden gelöscht'.
  ENDIF.
ENDFORM.



**  " Daten aus DB lesen
TYPES: BEGIN OF ty_mb51_mb5b_ext,
         feld1      TYPE zlagcleanup-feld1,
         feld2      TYPE zlagcleanup-feld2,
         feld3      TYPE zlagcleanup-feld3,
         feld4      TYPE zlagcleanup-feld4,
         feld5      TYPE zlagcleanup-feld5,
         feld6      TYPE zlagcleanup-feld6,
         feld7      TYPE zlagcleanup-feld7,
         feld8      TYPE zlagcleanup-feld8,
         feld9      TYPE zlagcleanup-feld9,
         feld10     TYPE zlagcleanup-feld10,
         feld11     TYPE zlagcleanup-feld11,
         feld12     TYPE zlagcleanup-feld12,
         feld13     TYPE zlagcleanup-feld13,
         feld14     TYPE zlagcleanup-feld14,
         feld15     TYPE zlagcleanup-feld15,
         feld16     TYPE zlagcleanup-feld16,
         feld17     TYPE zlagcleanup-feld17,
         me2m_feld3 TYPE zlagcleanup-feld3,  "Fall 11
         me3m_feld3 TYPE zlagcleanup-feld3,  "Fall 12
         mc46_feld3 TYPE zlagcleanup-feld3,  "Fall 12
         mstae      TYPE mstae,     "Hier das neue Feld hinzufügen
       END OF ty_mb51_mb5b_ext.

















FORM get_verschrottung_summe
 USING
   iv_matnr      TYPE matnr
   iv_datum_von  TYPE datum
   iv_datum_bis  TYPE datum
 CHANGING
   cv_summe      TYPE mseg-menge.

  DATA: lv_matnr_in TYPE matnr.

  TYPES: BEGIN OF ty_mseg,
           mblnr TYPE mseg-mblnr,
           mjahr TYPE mseg-mjahr,
           zeile TYPE mseg-zeile,
           bwart TYPE mseg-bwart,
           menge TYPE mseg-menge,
           budat TYPE mkpf-budat,
         END OF ty_mseg.

  DATA: lt_mseg TYPE TABLE OF ty_mseg.

  CLEAR cv_summe.

  " Materialnummer konvertieren
  CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
    EXPORTING
      input  = iv_matnr
    IMPORTING
      output = lv_matnr_in.

  " Hauptselektion mit korrekter Struktur
  SELECT mseg~mblnr,
         mseg~mjahr,
         mseg~zeile,
         mseg~bwart,
         mseg~menge,
         mkpf~budat
    FROM mseg
    INNER JOIN mkpf ON mkpf~mblnr = mseg~mblnr
                   AND mkpf~mjahr = mseg~mjahr
    INTO TABLE @lt_mseg
    WHERE mseg~matnr = @lv_matnr_in
      AND mseg~bwart BETWEEN '551' AND '558'
      AND mkpf~budat BETWEEN @iv_datum_von AND @iv_datum_bis.

  " Debug-Ausgaben
  "Write: / 'Selektionsparameter:'.
  "Write: / 'Materialnummer (intern):', lv_matnr_in.
  "Write: / 'Datum von:', iv_datum_von DD/MM/YYYY.
  "Write: / 'Datum bis:', iv_datum_bis DD/MM/YYYY.
  "Write: / 'SQL Treffer:', sy-dbcnt.

  " Detaillierte Ausgabe der gefundenen Verschrottungsbelege
  "Write: / 'Gefundene Verschrottungsbelege:'.
  LOOP AT lt_mseg INTO DATA(ls_mseg).
    cv_summe = cv_summe + ls_mseg-menge.
    "Write: / 'Beleg:', ls_mseg-mblnr,
*             'Jahr:', ls_mseg-mjahr,
*             'Zeile:', ls_mseg-zeile,
*             'BWART:', ls_mseg-bwart,
*             'Datum:', ls_mseg-budat,
*             'Menge:', ls_mseg-menge.
  ENDLOOP.

  "Write: / 'Gesamtsumme:', cv_summe.

ENDFORM.


FORM load_material_texts.
  DATA: lt_stxh      TYPE TABLE OF stxh,
        lt_lines     TYPE TABLE OF tline,
        lv_full_text TYPE string,
        lv_tdname    TYPE thead-tdname,
        lv_prefix    TYPE char3.

  FIELD-SYMBOLS: <fs_material> TYPE ty_material_data.

  " Für jedes Material die Texte laden
  LOOP AT gt_material_data ASSIGNING <fs_material>.

    " Einkaufstext (ZEK_) laden
    lv_tdname = |ZEK_{ <fs_material>-matnr }|.
    PERFORM read_material_text USING lv_tdname
                              CHANGING <fs_material>-komment_supply.

    " Vertriebstext (ZVK_) laden
    lv_tdname = |ZVK_{ <fs_material>-matnr }|.
    PERFORM read_material_text USING lv_tdname
                              CHANGING <fs_material>-komment_sales.

    " Finanztext (ZFI_) laden
    lv_tdname = |ZFI_{ <fs_material>-matnr }|.
    PERFORM read_material_text USING lv_tdname
                              CHANGING <fs_material>-komment_finanzen.
  ENDLOOP.
ENDFORM.

FORM read_material_text USING iv_tdname TYPE thead-tdname
                     CHANGING cv_text TYPE string.

  DATA: lt_lines TYPE TABLE OF tline.

  CLEAR: cv_text, lt_lines.

  " Text lesen
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

  IF sy-subrc = 0 AND lt_lines IS NOT INITIAL.
    " Textzeilen zusammensetzen
    LOOP AT lt_lines ASSIGNING FIELD-SYMBOL(<fs_line>).
      IF cv_text IS INITIAL.
        cv_text = <fs_line>-tdline.
      ELSE.
        cv_text = |{ cv_text } { <fs_line>-tdline }|.
      ENDIF.
    ENDLOOP.

    " Text bereinigen
    CONDENSE cv_text.
  ENDIF.
ENDFORM.


FORM get_material_data.
  DATA: lv_string  TYPE string,
        lv_number  TYPE p LENGTH 8 DECIMALS 2,
        gt_results TYPE TABLE OF ty_mb51_mb5b_ext.

  " Verschrottungsmenge ermitteln und abziehen
  DATA: lv_verschrottung TYPE mseg-menge,
        lv_datum_von     TYPE datum,
        lv_datum_bis     TYPE datum,
        lv_abgang        TYPE p LENGTH 16 DECIMALS 3.

  PERFORM clear_spool.

  " Befüllen von itt_materials
  LOOP AT s_matnr INTO DATA(ls_matnr).
    APPEND INITIAL LINE TO itt_materials ASSIGNING FIELD-SYMBOL(<fs_material>).
    <fs_material>-matnr = ls_matnr-low.
  ENDLOOP.

  DATA: lt_mb51 TYPE ty_bom_tab,
        lt_mb5b TYPE ty_bom_tab,
        lt_me2m TYPE ty_bom_tab,
        lt_me3m TYPE ty_bom_tab,
        lt_mc46 TYPE ty_bom_tab.

  IF p_act = 'X'.
    go_koi_mainfunc->run_batch_input_mb5B(
      EXPORTING
        t_materials = itt_materials
        p_dlow      = p_dlow
        p_dhigh     = p_dhigh
      CHANGING
        ct_bom      = lt_mb5b ).

    go_koi_mainfunc->z_download_and_delete_spool(
      EXPORTING
        iv_data_type     = 'MB5B'
        lt_all_materia   = itt_materials ).

    go_koi_mainfunc->run_batch_input_me3m(
      EXPORTING
        t_materials = itt_materials
      CHANGING
        ct_bom      = lt_ME3M ).

    go_koi_mainfunc->z_download_and_delete_spool(
      EXPORTING
        iv_data_type     = 'ME3M'
        lt_all_materia   = itt_materials ).

    go_koi_mainfunc->run_batch_input_me2m(
      EXPORTING
        t_materials = itt_materials
      CHANGING
        ct_bom      = lt_ME2M ).

    go_koi_mainfunc->z_download_and_delete_spool(
      EXPORTING
        iv_data_type     = 'ME2M'
        lt_all_materia   = itt_materials ).

    go_koi_mainfunc->run_batch_input_mc46(
      EXPORTING
        t_materials = itt_materials
      CHANGING
        ct_bom      = lt_MC46 ).

    go_koi_mainfunc->z_download_and_delete_spool(
      EXPORTING
        iv_data_type     = 'MC46'
        lt_all_materia   = itt_materials ).
  ENDIF.

  " Daten aus DB lesen
  DATA: lt_mb51_mb5b    TYPE TABLE OF ty_mb51_mb5b_ext,
        lv_package_size TYPE i VALUE 10000,
        lv_offset       TYPE i VALUE 0,
        lv_jahr         TYPE c LENGTH 4.

  DATA: lv_menge_string TYPE string,
        lv_is_negative  TYPE abap_bool.

  DATA: lt_fall11 TYPE SORTED TABLE OF zlagcleanup WITH NON-UNIQUE KEY feld2,
        lt_fall12 TYPE SORTED TABLE OF zlagcleanup WITH NON-UNIQUE KEY feld2,
        lt_fall13 TYPE SORTED TABLE OF zlagcleanup WITH NON-UNIQUE KEY feld2,
        lt_fall8  TYPE SORTED TABLE OF zlagcleanup WITH NON-UNIQUE KEY feld3.

  DATA: lv_sum11 TYPE p LENGTH 16 DECIMALS 3,
        lv_sum12 TYPE p LENGTH 16 DECIMALS 3,
        lv_sum13 TYPE p LENGTH 16 DECIMALS 3,
        lv_temp  TYPE p LENGTH 16 DECIMALS 3.

  " Daten selektieren
  SELECT * FROM zlagcleanup
    WHERE fall = '11'
    INTO TABLE @lt_fall11.

  SELECT * FROM zlagcleanup
    WHERE fall = '12'
    INTO TABLE @lt_fall12.

  SELECT * FROM zlagcleanup
    WHERE fall = '13'
    INTO TABLE @lt_fall13.

  SELECT * FROM zlagcleanup
    WHERE fall = '8' AND feld3 IN @s_matnr
    INTO TABLE @lt_fall8.

  " Summierung mit sicherer Konvertierung
  LOOP AT lt_fall8 ASSIGNING FIELD-SYMBOL(<fs_fall8>).
    CLEAR: lv_sum11, lv_sum12.

    " Summe für Fall 11
    LOOP AT lt_fall11 ASSIGNING FIELD-SYMBOL(<fs_fall11>)
      WHERE feld2 = <fs_fall8>-feld3.

      IF <fs_fall11>-feld3 IS NOT INITIAL.
        lv_string = <fs_fall11>-feld3.
        CONDENSE lv_string NO-GAPS.

        IF lv_string CS ',' AND lv_string CS '.'.
          DATA(lv_comma_pos) = 0.
          DATA(lv_dot_pos)   = 0.
          FIND ',' IN lv_string MATCH OFFSET lv_comma_pos.
          FIND '.' IN lv_string MATCH OFFSET lv_dot_pos.
          IF lv_comma_pos < lv_dot_pos.
            REPLACE ALL OCCURRENCES OF ',' IN lv_string WITH ''.
          ELSE.
            REPLACE ALL OCCURRENCES OF '.' IN lv_string WITH ''.
            REPLACE ALL OCCURRENCES OF ',' IN lv_string WITH '.'.
          ENDIF.
        ELSEIF lv_string CS ','.
          REPLACE ALL OCCURRENCES OF ',' IN lv_string WITH '.'.
        ENDIF.

        IF lv_string CS '-'.
          REPLACE ALL OCCURRENCES OF '-' IN lv_string WITH ''.
          lv_string = '-' && lv_string.
        ENDIF.

        TRY.
            lv_temp = CONV #( lv_string ).
            lv_sum11 = lv_sum11 + lv_temp.
          CATCH cx_root INTO DATA(lx_root).
            CONTINUE.
        ENDTRY.
      ENDIF.
    ENDLOOP.

    " Summe für Fall 12
    LOOP AT lt_fall12 ASSIGNING FIELD-SYMBOL(<fs_fall12>)
      WHERE feld2 = <fs_fall8>-feld3.

      IF <fs_fall12>-feld3 IS NOT INITIAL.
        CLEAR: lv_string, lv_temp.

        lv_string = <fs_fall12>-feld3.
        CONDENSE lv_string NO-GAPS.

        IF lv_string CS ',' AND lv_string CS '.'.
          lv_comma_pos = 0.
          lv_dot_pos   = 0.
          FIND ',' IN lv_string MATCH OFFSET lv_comma_pos.
          FIND '.' IN lv_string MATCH OFFSET lv_dot_pos.
          IF lv_comma_pos < lv_dot_pos.
            REPLACE ALL OCCURRENCES OF ',' IN lv_string WITH ''.
          ELSE.
            REPLACE ALL OCCURRENCES OF '.' IN lv_string WITH ''.
            REPLACE ALL OCCURRENCES OF ',' IN lv_string WITH '.'.
          ENDIF.
        ELSEIF lv_string CS ','.
          REPLACE ALL OCCURRENCES OF ',' IN lv_string WITH '.'.
        ENDIF.

        TRY.
            lv_temp = CONV #( lv_string ).
            lv_sum12 = lv_sum12 + lv_temp.
          CATCH cx_root INTO DATA(lx_root12).
            CONTINUE.
        ENDTRY.
      ENDIF.
    ENDLOOP.

    " Ergebniszeile aufbauen
    APPEND INITIAL LINE TO gt_results ASSIGNING FIELD-SYMBOL(<fs_result>).
    MOVE-CORRESPONDING <fs_fall8> TO <fs_result>.

    " MC46 Status setzen (Fall 13)
    READ TABLE lt_fall13 WITH KEY feld2 = <fs_fall8>-feld3 TRANSPORTING NO FIELDS.
    IF sy-subrc = 0.
      <fs_result>-mc46_feld3 = 'X'.
    ELSE.
      <fs_result>-mc46_feld3 = ''.
    ENDIF.

    <fs_result>-me2m_feld3 = |{ lv_sum11 NUMBER = USER }|.
    <fs_result>-me3m_feld3 = |{ lv_sum12 NUMBER = USER }|.

    SELECT SINGLE mstae
      FROM mara
      WHERE matnr = @<fs_fall8>-feld3
      INTO @<fs_result>-feld17.
  ENDLOOP.

  " === HAUPTSCHLEIFE: Materialien verarbeiten ===
  IF sy-subrc = 0.
    LOOP AT gt_results ASSIGNING FIELD-SYMBOL(<fs_mb51_mb5b>).
      APPEND INITIAL LINE TO gt_material_data ASSIGNING FIELD-SYMBOL(<fs_materialg>).

      " === 1. BASIS-FELDER (String) ===
      <fs_materialg>-report       = sy-repid.
      <fs_materialg>-erstelldatum = sy-datum.
      <fs_materialg>-bwtk         = CONV string( <fs_mb51_mb5b>-feld2 ).
      <fs_materialg>-matnr        = <fs_mb51_mb5b>-feld3.
      <fs_materialg>-maktx        = <fs_mb51_mb5b>-feld4.
      <fs_materialg>-bme          = <fs_mb51_mb5b>-feld9.
      <fs_materialg>-waers        = <fs_mb51_mb5b>-feld14.
      <fs_materialg>-datum_von    = <fs_mb51_mb5b>-feld15.
      <fs_materialg>-datum_bis    = <fs_mb51_mb5b>-feld16.
      <fs_materialg>-status       = CONV mmsta( <fs_mb51_mb5b>-feld17 ).

      " String-Felder für Anzeige (werden später überschrieben)
      <fs_materialg>-anfangsbestand = <fs_mb51_mb5b>-feld5.
      <fs_materialg>-summe_zugang   = <fs_mb51_mb5b>-feld6.
      <fs_materialg>-summe_abgang   = <fs_mb51_mb5b>-feld7.
      <fs_materialg>-endbestand     = <fs_mb51_mb5b>-feld8.
      <fs_materialg>-anfangswert    = <fs_mb51_mb5b>-feld10.
      <fs_materialg>-summe_zugwert  = <fs_mb51_mb5b>-feld11.
      <fs_materialg>-summe_abgwert  = <fs_mb51_mb5b>-feld12.
      <fs_materialg>-endwert        = <fs_mb51_mb5b>-feld13.
      <fs_materialg>-me2m_offen     = <fs_mb51_mb5b>-me2m_feld3.
      <fs_materialg>-me3m_offen     = <fs_mb51_mb5b>-me3m_feld3.
      <fs_materialg>-mc46_nomove    = <fs_mb51_mb5b>-mc46_feld3.

      " === 2. NUMERISCHE FELDER BEFÜLLEN (ZUERST!) ===
      " Mengen
      PERFORM parse_quan USING <fs_materialg>-anfangsbestand CHANGING <fs_materialg>-anfangsbestand_n.
      PERFORM parse_quan USING <fs_materialg>-summe_zugang   CHANGING <fs_materialg>-summe_zugang_n.
      PERFORM parse_quan USING <fs_materialg>-endbestand     CHANGING <fs_materialg>-endbestand_n.
      PERFORM parse_quan USING <fs_materialg>-me2m_offen     CHANGING <fs_materialg>-me2m_offen_n.
      PERFORM parse_quan USING <fs_materialg>-me3m_offen     CHANGING <fs_materialg>-me3m_offen_n.

      " Werte
      PERFORM parse_curr USING <fs_materialg>-anfangswert   CHANGING <fs_materialg>-anfangswert_n.
      PERFORM parse_curr USING <fs_materialg>-summe_zugwert CHANGING <fs_materialg>-summe_zugwert_n.
      PERFORM parse_curr USING <fs_materialg>-summe_abgwert CHANGING <fs_materialg>-summe_abgwert_n.
      PERFORM parse_curr USING <fs_materialg>-endwert       CHANGING <fs_materialg>-endwert_n.

      " === 3. VERSCHROTTUNG BERECHNEN ===
     " === 3. VERSCHROTTUNG BERECHNEN ===
IF strlen( <fs_materialg>-datum_von ) >= 10.
  lv_datum_von = |{ <fs_materialg>-datum_von+6(4) }{ <fs_materialg>-datum_von+3(2) }{ <fs_materialg>-datum_von(2) }|.
ELSE.
  lv_datum_von = p_dlow.
ENDIF.

IF strlen( <fs_materialg>-datum_bis ) >= 10.
  lv_datum_bis = |{ <fs_materialg>-datum_bis+6(4) }{ <fs_materialg>-datum_bis+3(2) }{ <fs_materialg>-datum_bis(2) }|.
ELSE.
  lv_datum_bis = p_dhigh.
ENDIF.

      PERFORM get_verschrottung_summe
        USING <fs_materialg>-matnr lv_datum_von lv_datum_bis
        CHANGING lv_verschrottung.

      " Verschrottung in BEIDE Felder schreiben
      <fs_materialg>-summe_verschrottet_n = lv_verschrottung.
      <fs_materialg>-summe_verschrottet   = |{ lv_verschrottung NUMBER = USER }|.

      " === 4. ABGANG BEREINIGEN ===
      lv_is_negative = xsdbool( <fs_mb51_mb5b>-feld7 CS '-' ).
      lv_menge_string = <fs_mb51_mb5b>-feld7.
      REPLACE ALL OCCURRENCES OF '-' IN lv_menge_string WITH ''.

      PERFORM parse_quan USING lv_menge_string CHANGING lv_abgang.
      IF lv_is_negative = abap_true.
        lv_abgang = lv_abgang * -1.
      ENDIF.

      <fs_materialg>-summe_abgang_n = lv_abgang.
      <fs_materialg>-summe_abgang   = |{ lv_abgang NUMBER = USER }|.

      " Bereinigter Abgang
      DATA(lv_abgang_bereinigt) = lv_abgang - lv_verschrottung.
      <fs_materialg>-summe_abgang_minus_ver_n = lv_abgang_bereinigt.
      <fs_materialg>-summe_abgang_minus_ver   = |{ lv_abgang_bereinigt NUMBER = USER }|.

      " === 5. PREIS PRO STÜCK ===
      DATA: lv_preis_stk TYPE p LENGTH 16 DECIMALS 2.

      IF <fs_materialg>-endbestand_n > 0 AND <fs_materialg>-endwert_n IS NOT INITIAL.
        lv_preis_stk = <fs_materialg>-endwert_n / <fs_materialg>-endbestand_n.
        <fs_materialg>-preis_stk_n = lv_preis_stk.
        <fs_materialg>-preis_stk   = |{ lv_preis_stk NUMBER = USER DECIMALS = 2 }|.
      ELSE.
        <fs_materialg>-preis_stk_n = 0.
        <fs_materialg>-preis_stk   = '0.00'.
      ENDIF.

      " === 6. REICHWEITE BERECHNEN (nutzt jetzt korrekte _N-Felder!) ===
      DATA: lv_reichweite TYPE p LENGTH 16 DECIMALS 1,
            lv_reichw_verschr TYPE p LENGTH 16 DECIMALS 1.

      IF lv_abgang_bereinigt <> 0.
        DATA(lv_perioden_anzahl) = go_koi_mainfunc->calculate_month_difference(
                                      iv_date_from = p_dlow
                                      iv_date_to   = p_dhigh ).

        lv_reichweite = ( <fs_materialg>-endbestand_n +
                          <fs_materialg>-me2m_offen_n +
                          <fs_materialg>-me3m_offen_n ) /
                        ( - ( lv_abgang_bereinigt / lv_perioden_anzahl ) ).

        <fs_materialg>-reichweite_n = lv_reichweite.
        <fs_materialg>-reichweite   = |{ lv_reichweite NUMBER = USER DECIMALS = 1 }|.

        " Reichweite MIT Verschrottung
        IF lv_abgang <> 0.
          lv_reichw_verschr = ( <fs_materialg>-endbestand_n +
                                <fs_materialg>-me2m_offen_n +
                                <fs_materialg>-me3m_offen_n ) /
                              ( - ( lv_abgang / lv_perioden_anzahl ) ).
          <fs_materialg>-reichw_m_verschr_n = lv_reichw_verschr.
          <fs_materialg>-reichw_m_verschr   = |{ lv_reichw_verschr NUMBER = USER DECIMALS = 1 }|.
        ENDIF.
      ELSE.
        IF lv_verschrottung <> 0.
          <fs_materialg>-reichweite = 'NUR VERSCHR'.
        ELSE.
          <fs_materialg>-reichweite = 'DIV/0'.
        ENDIF.
        <fs_materialg>-reichweite_n = 0.
      ENDIF.

      " === 7. STAMMDATEN ===
      SELECT SINGLE zzschrottrsk INTO @<fs_materialg>-zzschrottrsk
        FROM mara WHERE matnr = @<fs_materialg>-matnr.

      SELECT SINGLE dispo INTO @<fs_materialg>-disponent
        FROM marc WHERE matnr = @<fs_materialg>-matnr.

    ENDLOOP.
    CLEAR gt_results.
  ENDIF.

ENDFORM.




FORM adjust_alv_grid USING lo_alv TYPE REF TO cl_salv_table.
  DATA: lo_columns TYPE REF TO cl_salv_columns_table,
        lo_column  TYPE REF TO cl_salv_column.

  lo_columns = lo_alv->get_columns( ).

  " Sicht & Texte für Ident-/Metadaten -------------------------------------------------
  TRY.
      lo_column ?= lo_columns->get_column( 'REPORT' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Report' ).
      lo_column->set_medium_text( 'Report' ).
      lo_column->set_short_text( 'Report' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'ERSTELLDATUM' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Erstellungsdatum' ).
      lo_column->set_medium_text( 'Erst.datum' ).
      lo_column->set_short_text( 'Erst.dat' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'BWTK' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'BwtK' ).
      lo_column->set_medium_text( 'BwtK' ).
      lo_column->set_short_text( 'BwtK' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'MATNR' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Material' ).
      lo_column->set_medium_text( 'Material' ).
      lo_column->set_short_text( 'Mat.' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'MAKTX' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Materialkurztext' ).
      lo_column->set_medium_text( 'Kurztext' ).
      lo_column->set_short_text( 'Kurzt.' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  " Datums-/Stammfelder ----------------------------------------------------------------
  TRY.
      lo_column ?= lo_columns->get_column( 'DATUM_VON' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Datum von' ).
      lo_column->set_medium_text( 'Dat.von' ).
      lo_column->set_short_text( 'Von' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'DATUM_BIS' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Datum bis' ).
      lo_column->set_medium_text( 'Dat.bis' ).
      lo_column->set_short_text( 'Bis' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'BME' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'BME' ).
      lo_column->set_medium_text( 'BME' ).
      lo_column->set_short_text( 'BME' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'WAERS' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Währung' ).
      lo_column->set_medium_text( 'Währung' ).
      lo_column->set_short_text( 'Wäh.' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  " --- WICHTIG: Textbasierte Mengen-/Wertspalten ausblenden ----------------------------
  " (damit Sortierung/Autosummen nicht auf Strings erfolgen)
  DATA lt_hide_txt TYPE STANDARD TABLE OF string.

  lt_hide_txt = VALUE #(
    ( CONV string( 'ANFANGSB' ) )
    ( CONV string( 'SUMME_ZUGANG' ) )
    ( CONV string( 'SUMME_ABGANG' ) )
    ( CONV string( 'SUMME_ABGANG_MINUS_VER' ) )
    ( CONV string( 'SUMME_VERSCHROTTET' ) )
    ( CONV string( 'ENDBESTAND' ) )
    ( CONV string( 'ANFANGSWERT' ) )
    ( CONV string( 'SUMME_ZUGWERT' ) )
    ( CONV string( 'SUMME_ABGWERT' ) )
    ( CONV string( 'ENDWERT' ) )
    ( CONV string( 'PREIS_STK' ) )
    ( CONV string( 'ME2M_OFFEN' ) )
    ( CONV string( 'ME3M_OFFEN' ) )
    ( CONV string( 'REICHWEITE' ) )
    ( CONV string( 'REICHW_M_VERSCHR' ) )
  ).


  LOOP AT lt_hide_txt ASSIGNING FIELD-SYMBOL(<ls_hide>).
    DATA(lv_col) = CONV lvc_fname( <ls_hide> ).
    TRY.
        lo_column ?= lo_columns->get_column( lv_col ).
        lo_column->set_visible( abap_false ).
      CATCH cx_salv_not_found.
    ENDTRY.
  ENDLOOP.

  " --- Numerische Spiegelspalten (_N) sichtbar und benamst -----------------------------
  " Mengen (QUAN)
  TRY.
      lo_column ?= lo_columns->get_column( 'ANFANGSBESTAND_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Anfangsbestand' ).
      lo_column->set_medium_text( 'Anf.best.' ).
      lo_column->set_short_text( 'Anf.' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'SUMME_ZUGANG_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Summe Zugang' ).
      lo_column->set_medium_text( 'Sum.Zug.' ).
      lo_column->set_short_text( 'Sum.Z' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'SUMME_ABGANG_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Summe Abgang' ).
      lo_column->set_medium_text( 'Sum.Abg.' ).
      lo_column->set_short_text( 'Sum.A' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'SUMME_ABGANG_MINUS_VER_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Summe Abgang minus Verschr' ).
      lo_column->set_medium_text( 'Sum.AbVer' ).
      lo_column->set_short_text( 'Sum.AV' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'SUMME_VERSCHROTTET_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Summe Verschrottet' ).
      lo_column->set_medium_text( 'Sum.Ver' ).
      lo_column->set_short_text( 'Sum.V' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'ENDBESTAND_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Endbestand' ).
      lo_column->set_medium_text( 'Endbestand' ).
      lo_column->set_short_text( 'End.' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  " Werte (CURR)
  TRY.
      lo_column ?= lo_columns->get_column( 'ANFANGSWERT_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Anfangswert' ).
      lo_column->set_medium_text( 'Anf.wert' ).
      lo_column->set_short_text( 'Anf.W' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'SUMME_ZUGWERT_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Summe Zugangswert' ).
      lo_column->set_medium_text( 'Sum.Zug.W' ).
      lo_column->set_short_text( 'Sum.ZW' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'SUMME_ABGWERT_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Summe Abgangswert' ).
      lo_column->set_medium_text( 'Sum.Abg.W' ).
      lo_column->set_short_text( 'Sum.AW' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'ENDWERT_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Endwert' ).
      lo_column->set_medium_text( 'Endwert' ).
      lo_column->set_short_text( 'End.W' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  " Preis / Stück
  TRY.
      lo_column ?= lo_columns->get_column( 'PREIS_STK_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Preis/Stk' ).
      lo_column->set_medium_text( 'Preis/Stk' ).
      lo_column->set_short_text( 'Pr/Stk' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  " Offene Mengen (ME2M/ME3M)
  TRY.
      lo_column ?= lo_columns->get_column( 'ME2M_OFFEN_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'ME2M offene Bestell ME' ).
      lo_column->set_medium_text( 'ME2M offen' ).
      lo_column->set_short_text( 'ME2M' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'ME3M_OFFEN_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'ME3M offene MK ME' ).
      lo_column->set_medium_text( 'ME3M offen' ).
      lo_column->set_short_text( 'ME3M' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  " Reichweiten (in Monaten, numerisch)
  TRY.
      lo_column ?= lo_columns->get_column( 'REICHWEITE_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Reichweite MTE' ).
      lo_column->set_medium_text( 'Reichweite' ).
      lo_column->set_short_text( 'Reic.' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'REICHW_M_VERSCHR_N' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Reichweite m. Verschrottung' ).
      lo_column->set_medium_text( 'ReicVer' ).
      lo_column->set_short_text( 'ReicV' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  " Status/Flag-/Klassifikations- und Freitextspalten ----------------------------------
  TRY.
      lo_column ?= lo_columns->get_column( 'STATUS' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Status' ).
      lo_column->set_medium_text( 'Status' ).
      lo_column->set_short_text( 'Stat.' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'DISPONENT' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Disponent' ).
      lo_column->set_medium_text( 'Disponent' ).
      lo_column->set_short_text( 'Disp.' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'MC46_NOMOVE' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'MC46 - 365Tg no-mover' ).
      lo_column->set_medium_text( 'MC46 nomove' ).
      lo_column->set_short_text( 'MC46' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'XYZ_KLASSE' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'XYZ-Klasse' ).
      lo_column->set_medium_text( 'XYZ' ).
      lo_column->set_short_text( 'XYZ' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'KOMMENT_SUPPLY' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Einkaufstext' ).
      lo_column->set_medium_text( 'Eink.text' ).
      lo_column->set_short_text( 'EK-Txt' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'KOMMENT_SALES' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Vertriebstext' ).
      lo_column->set_medium_text( 'Vertr.text' ).
      lo_column->set_short_text( 'VK-Txt' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'KOMMENT_FINANZEN' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Finanztext' ).
      lo_column->set_medium_text( 'Finanztext' ).
      lo_column->set_short_text( 'FI-Txt' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  TRY.
      lo_column ?= lo_columns->get_column( 'ZZSCHROTTRSK' ).
      lo_column->set_visible( abap_true ).
      lo_column->set_long_text(  'Verschrottungsrisiko' ).
      lo_column->set_medium_text( 'Verschr.risiko' ).
      lo_column->set_short_text( 'V-Risk' ).
    CATCH cx_salv_not_found.
  ENDTRY.

  " Auto-Optimierung der Spaltenbreite -------------------------------------------------
  lo_columns->set_optimize( abap_true ).

  " (Optional) Aggregationen einschalten – falls gewünscht: Summen für Mengen/Werte
  " DATA(lo_aggr) = lo_alv->get_aggregations( ).
  " TRY. lo_aggr->add_aggregation( 'ANFANGSBESTAND_N' ). CATCH cx_salv_not_found. ENDTRY.
  " TRY. lo_aggr->add_aggregation( 'SUMME_ZUGANG_N'     ). CATCH cx_salv_not_found. ENDTRY.
  " TRY. lo_aggr->add_aggregation( 'SUMME_ABGANG_N'     ). CATCH cx_salv_not_found. ENDTRY.
  " TRY. lo_aggr->add_aggregation( 'ENDBESTAND_N'       ). CATCH cx_salv_not_found. ENDTRY.
  " TRY. lo_aggr->add_aggregation( 'ANFANGSWERT_N'      ). CATCH cx_salv_not_found. ENDTRY.
  " TRY. lo_aggr->add_aggregation( 'SUMME_ZUGWERT_N'    ). CATCH cx_salv_not_found. ENDTRY.
  " TRY. lo_aggr->add_aggregation( 'SUMME_ABGWERT_N'    ). CATCH cx_salv_not_found. ENDTRY.
  " TRY. lo_aggr->add_aggregation( 'ENDWERT_N'          ). CATCH cx_salv_not_found. ENDTRY.

ENDFORM.



FORM get_mb51Finance_data USING i_matnr TYPE mara-matnr.

  DATA: lv_matnr TYPE mara-matnr.

  lv_matnr = i_matnr.

  " Prüfen, ob das Material existiert
  SELECT SINGLE * FROM mara WHERE matnr = lv_matnr.
  IF sy-subrc <> 0.
    MESSAGE 'Materialnummer existiert nicht.' TYPE 'E'.
  ENDIF.

  " Konto aus BSEG-Tabelle holen
  SELECT SINGLE hkont INTO lv_konto    "#EC CI_DB_OPERATION_OK[2431747]
    FROM bseg
    WHERE matnr = lv_matnr.
  IF sy-subrc = 0.
    "Write: / 'Konto:', lv_konto.
  ELSE.
    "Write: / 'Kein Konto für Material gefunden.'.
  ENDIF.

  " Wert in CHF (lokale Währung) aus der MBEW-Tabelle holen
  SELECT SINGLE salk3 INTO lv_wert_fr
    FROM mbew
    WHERE matnr = lv_matnr.
  IF sy-subrc = 0.
    "Write: / 'Wert in CHF:', lv_wert_fr.
  ELSE.
    "Write: / 'Kein Wert in CHF für Material gefunden.'.
  ENDIF.

  " Menge in LME (Lagermaßeinheit) aus der MARD-Tabelle holen
  SELECT SINGLE labst INTO lv_menge_lme
    FROM mard
    WHERE matnr = lv_matnr.
  IF sy-subrc = 0.
    "Write: / 'Menge in LME:', lv_menge_lme.
  ELSE.
    "Write: / 'Keine Menge für Material gefunden.'.
  ENDIF.

ENDFORM.


FORM get_last_movement USING p_matnr TYPE matnr
                             p_werks TYPE werks_d
                             p_lgort TYPE lgort_d
                        CHANGING p_last_move TYPE dats.

  SELECT MAX( mkpf~budat ) INTO @p_last_move
    FROM mseg
    INNER JOIN mkpf ON mkpf~mblnr = mseg~mblnr
                   AND mkpf~mjahr = mseg~mjahr
    WHERE mseg~matnr = @p_matnr
      AND mseg~werks = @p_werks
      AND mseg~lgort = @p_lgort.

ENDFORM.

FORM get_last_consumption USING p_matnr TYPE matnr
                                p_werks TYPE werks_d
                                p_lgort TYPE lgort_d
                       CHANGING p_last_cons TYPE menge_d.
  DATA: lv_last_year TYPE mjahr.

  lv_last_year = sy-datum(4) - 1.

  SELECT SUM( menge )
    FROM mseg
    INTO @p_last_cons
    WHERE matnr = @p_matnr
      AND werks = @p_werks
      AND lgort = @p_lgort
      AND bwart IN ('261', '262', '601', '602')
      AND mjahr = @lv_last_year.
ENDFORM.

FORM display_alv.
  DATA: lo_alv TYPE REF TO cl_salv_table.

  TRY.
      cl_salv_table=>factory(
        IMPORTING
          r_salv_table = lo_alv
        CHANGING
          t_table      = gt_material_data ).

      lo_alv->get_functions( )->set_all( abap_true ).
      lo_alv->get_columns( )->set_optimize( abap_true ).

      " Call the new form to adjust the ALV grid
      PERFORM adjust_alv_grid USING lo_alv.

      lo_alv->display( ).
    CATCH cx_salv_msg.
      MESSAGE 'Fehler bei der ALV-Anzeige' TYPE 'E'.
  ENDTRY.
ENDFORM.

FORM create_csv_file.
  DATA: lv_filename TYPE string,
        lt_data     TYPE truxs_t_text_data,
        lv_csv_line TYPE string.

  " Create header
  lv_csv_line = 'Report,Erstellungsdatum,BwtK,Material,Materialkurztext,' &&
                'Anfangsbestand,Summe Zugang,Summe Abgang,Endbestand,BME,' &&
                'Anfangswert,Summe Zugangswert,Summe Abgangswert,Endwert,' &&
                'Währung,Datum von,Datum bis,Preis/Stk,STATUS,DISPONENT,' &&
                'ME2M offene Bestell ME,ME3M offene MK ME,' &&
                'MC46 - 365Tg no-mover,Reichweite MTE'.
  APPEND lv_csv_line TO lt_data.

  " Create data lines
  LOOP AT gt_material_data INTO gs_material_data.
    CLEAR lv_csv_line.
    lv_csv_line = |{ gs_material_data-report },| &&
                  |{ gs_material_data-erstelldatum },| &&
                  "|{ gs_material_data-bwtk },| &&
                  |{ gs_material_data-matnr },| &&
                  |{ gs_material_data-maktx },| &&
                  |{ gs_material_data-anfangsbestand },| &&
                  |{ gs_material_data-summe_zugang },| &&
                  |{ gs_material_data-summe_abgang },| &&
                  |{ gs_material_data-endbestand },| &&
                  |{ gs_material_data-bme },| &&
                  |{ gs_material_data-anfangswert },| &&
                  |{ gs_material_data-summe_zugwert },| &&
                  |{ gs_material_data-summe_abgwert },| &&
                  |{ gs_material_data-endwert },| &&
                  |{ gs_material_data-waers },| &&
                  |{ gs_material_data-datum_von },| &&
                  |{ gs_material_data-datum_bis },| &&
                  |{ gs_material_data-preis_stk },| &&
                  |{ gs_material_data-status },| &&
                  |{ gs_material_data-disponent },| &&
                  |{ gs_material_data-me2m_offen },| &&
                  |{ gs_material_data-me3m_offen },| &&
                  |{ gs_material_data-mc46_nomove },| &&
                  |{ gs_material_data-reichweite }|.
    APPEND lv_csv_line TO lt_data.
  ENDLOOP.

  " GUI Download
  CALL FUNCTION 'GUI_DOWNLOAD'
    EXPORTING
      "filename = p_path
      filetype = 'ASC'
    TABLES
      data_tab = lt_data
    EXCEPTIONS
      OTHERS   = 1.

  IF sy-subrc <> 0.
    MESSAGE 'Fehler beim Datei-Download' TYPE 'E'.
  ELSE.
    MESSAGE 'CSV-Datei erfolgreich erstellt' TYPE 'S'.
  ENDIF.
ENDFORM.














*&---------------------------------------------------------------------*
*& Form CALCULATE_XYZ_ANALYSIS
*&---------------------------------------------------------------------*
FORM calculate_xyz_analysis.

  " .Konstanten für XYZ-Analyse
  CONSTANTS: gc_xyz_period       TYPE i     VALUE 12,      " .Analyseperiode in Monaten
             gc_xyz_limit_x      TYPE p DECIMALS 2 VALUE '0.20', "0.4
             gc_xyz_limit_y      TYPE p DECIMALS 2 VALUE '0.20', " 1.0
             gc_period_indicator TYPE mver-perkz VALUE 'M'. " .Wir analysieren Monatsdaten

  " .NEU: Struktur für MVER-Daten passend zur DB-Tabelle
  TYPES: BEGIN OF ty_mver_pivot,
           matnr TYPE mver-matnr,
           werks TYPE mver-werks, " .Werk hinzugefügt, oft relevant
           gjahr TYPE mver-gjahr,
           perkz TYPE mver-perkz,
           gsv01 TYPE mver-gsv01, gsv02 TYPE mver-gsv02, gsv03 TYPE mver-gsv03,
           gsv04 TYPE mver-gsv04, gsv05 TYPE mver-gsv05, gsv06 TYPE mver-gsv06,
           gsv07 TYPE mver-gsv07, gsv08 TYPE mver-gsv08, gsv09 TYPE mver-gsv09,
           gsv10 TYPE mver-gsv10, gsv11 TYPE mver-gsv11, gsv12 TYPE mver-gsv12,
           gsv13 TYPE mver-gsv13, " .Ggf. bis Periode 16 erweitern, falls nötig
         END OF ty_mver_pivot.

  " .Lokale Daten
  DATA: lt_mver_pivot  TYPE STANDARD TABLE OF ty_mver_pivot,
        ls_mver_pivot  LIKE LINE OF lt_mver_pivot,
        lt_matnr_range TYPE RANGE OF matnr,
        ls_matnr_range LIKE LINE OF lt_matnr_range,
        lt_years_range TYPE RANGE OF gjahr, " .Range für Jahre
        ls_years_range LIKE LINE OF lt_years_range,
        lv_date_from   TYPE dats,
        lv_year_from   TYPE gjahr,
        lv_period_from TYPE i, " .Periode als Zahl 1-12
        lv_year_curr   TYPE gjahr,
        lv_period_curr TYPE i. " .Periode als Zahl 1-12

  " .Für dynamischen Feldzugriff
  DATA: lv_period_char(2) TYPE c,
        lv_fieldname(5)   TYPE c. " .z.B. 'GSV01', 'GSV12'
  FIELD-SYMBOLS: <fs_consumption> TYPE any. " .Für dynamischen Zugriff auf GSVxx

  FIELD-SYMBOLS: <fs_materialg> TYPE ty_material_data.

  " .1. Prüfen, ob Materialdaten vorhanden sind (wie vorher)
  IF gt_material_data IS INITIAL.
    MESSAGE 'Keine Materialdaten für XYZ-Analyse vorhanden.' TYPE 'W'.
    EXIT.
  ENDIF.

  " .2. Relevante Materialien für MVER-Selektion sammeln (wie vorher)
  LOOP AT gt_material_data ASSIGNING <fs_materialg>.
    <fs_materialg>-xyz_klasse = ' '.
    ls_matnr_range-sign   = 'I'.
    ls_matnr_range-option = 'EQ'. ls_matnr_range-low = <fs_materialg>-matnr.
    APPEND ls_matnr_range TO lt_matnr_range.
  ENDLOOP.
  SORT lt_matnr_range BY low. DELETE ADJACENT DUPLICATES FROM lt_matnr_range COMPARING low.
  IF lt_matnr_range IS INITIAL. MESSAGE 'Keine Materialien zur Analyse ausgewählt.' TYPE 'I'. EXIT. ENDIF.

  " .3. Analysezeitraum berechnen (Start-/Endjahr und -periode als Zahl)
  lv_year_curr   = sy-datum(4).
  lv_period_curr = sy-datum+4(2). " .Aktueller Kalendermonat




  DATA: lv_months_numc TYPE t5a4a-dlymo, " .NEU: Variable passend zu FM-Interface (NUMC(2))
        lv_sign        TYPE t5a4a-split.  " .NEU: Variable für Vorzeichen

  lv_months_numc = gc_xyz_period. " .Anzahl Monate (positiv), gc_xyz_period ist I, Konvertierung nach NUMC(2)
  lv_sign        = '-'.          " .Vorzeichen für Subtraktion über SIGNUM steuern


  CALL FUNCTION 'RP_CALC_DATE_IN_INTERVAL'
    EXPORTING
      date      = sy-datum          " .Passt (Dats)
      days      = 0                 " .Passt (Integer-Literal)
      months    = lv_months_numc    " .Korrekter Typ übergeben
      signum    = lv_sign           " .Korrekter Signum für Subtraktion
      years     = 0                 " .Passt (Integer-Literal)
    IMPORTING
      calc_date = lv_date_from      " .Empfangendes Feld passt (Dats)
    EXCEPTIONS
      OTHERS    = 1.                " .Standard-Fehlerbehandlung
  IF sy-subrc <> 0.
    " .Verbesserte Fehlermeldung
    MESSAGE |Fehler (SY-SUBRC={ sy-subrc }) bei RP_CALC_DATE_IN_INTERVAL.| TYPE 'E'.
    EXIT. " .Oder andere geeignete Fehlerbehandlung
  ENDIF.

  lv_year_from   = lv_date_from(4).
  lv_period_from = lv_date_from+4(2). " .Start-Kalendermonat

  " .Benötigte Jahre für Selektion ermitteln (kann aktuelles und vorheriges sein)
  CLEAR lt_years_range.
  ls_years_range-sign = 'I'. ls_years_range-option = 'EQ'. ls_years_range-low = lv_year_curr.
  APPEND ls_years_range TO lt_years_range.
  IF lv_year_curr <> lv_year_from.
    ls_years_range-low = lv_year_from.
    APPEND ls_years_range TO lt_years_range.
  ENDIF.
  SORT lt_years_range BY low. DELETE ADJACENT DUPLICATES FROM lt_years_range COMPARING low.

  " .Debug-Ausgabe für Zeitraum
  "Write: / 'XYZ-Analyse Zeitraum: Von', lv_period_from, '/', lv_year_from,
  "'Bis', lv_period_curr, '/', lv_year_curr, '(Monate)'.

  " .4. Verbrauchsdaten aus MVER selektieren (Pivot-Struktur)
  " .HINWEIS: Selektion braucht MATNR, WERKS, GJAHR. Annahme: Analyse pro Werk?
  " .Wenn s_werks existiert, könnte man das hier nutzen.
  " .Ohne Werk-Selektion wird pro Material/Jahr aggregiert, was falsch sein kann.
  " .Wir brauchen das Werk pro Material aus gt_material_data oder einem Parameter.
  " .Annahme: Wir haben das Werk (z.B. in <fs_materialg>-werks, falls hinzugefügt)
  " .ODER wir lesen es aus MARC. Hier erstmal ohne Werk für Einfachheit.
  SELECT matnr, werks, gjahr, perkz, " .Wichtige Felder
         gsv01, gsv02, gsv03, gsv04, gsv05, gsv06, gsv07, " .Alle Periodenfelder
         gsv08, gsv09, gsv10, gsv11, gsv12, gsv13
    FROM mver
    INTO TABLE @lt_mver_pivot
    FOR ALL ENTRIES IN @lt_matnr_range
    WHERE matnr = @lt_matnr_range-low
      AND gjahr IN @lt_years_range  " .Selektiere relevante Jahre
      AND perkz = @gc_period_indicator " .Nur Monatsdaten
      AND werks = '1100' .".??? '" .Hier Werk hinzufügen, falls nötig

  " .Fehlerbehandlung, wenn keine MVER-Daten gefunden wurden (wie vorher, aber mit Hinweis auf PERKZ)
  IF sy-subrc <> 0 AND lt_mver_pivot IS INITIAL.
    MESSAGE |Keine Verbrauchsdaten (MVER) für PERKZ='{ gc_period_indicator }' gefunden. Alle als Z klassifiziert.| TYPE 'W'.
    LOOP AT gt_material_data ASSIGNING <fs_materialg>. <fs_materialg>-xyz_klasse = 'Z'. ENDLOOP.
    EXIT.
  ENDIF.

  SORT lt_mver_pivot BY matnr werks gjahr. " .Sortieren für READ TABLE

  " .5. XYZ-Klasse pro Material berechnen (mit neuer Logik)
  DATA: lt_consumption_values TYPE STANDARD TABLE OF mver-gsv01 WITH DEFAULT KEY, " .Passt jetzt
        lv_consumption        TYPE mver-gsv01,
        lv_sum                TYPE p LENGTH 16 DECIMALS 3,
        lv_mean               TYPE p LENGTH 16 DECIMALS 3,
        lv_stddev             TYPE p LENGTH 16 DECIMALS 3,
        lv_variance           TYPE p LENGTH 16 DECIMALS 3,
        lv_vk                 TYPE p LENGTH 16 DECIMALS 3,
        lv_data_points        TYPE i,
        lv_sum_sq_diff        TYPE p LENGTH 16 DECIMALS 3,
        lv_diff               TYPE p LENGTH 16 DECIMALS 3,
        lv_divisor            TYPE p DECIMALS 0.

  LOOP AT gt_material_data ASSIGNING <fs_materialg>.
    CLEAR: lt_consumption_values, lv_sum, lv_mean, lv_stddev, lv_variance,
           lv_vk, lv_data_points, lv_sum_sq_diff, lv_diff, lv_divisor.

    " .Werk für das Material ermitteln (Beispiel: Annahme aus MARC oder gt_material_data)
    DATA(lv_werks) = ''. " .Platzhalter - MUSS KORREKT ERMITTELT WERDEN!
    " SELECT SINGLE werks INTO lv_werks FROM marc WHERE matnr = <fs_materialg>-matnr. (Beispiel)
    " IF lv_werks IS INITIAL AND gt_material_data hat eine Werksspalte:
    "   lv_werks = <fs_materialg>-werks. (Beispiel)
    IF lv_werks IS INITIAL.
      "Write: / 'Warnung: Werk für Material', <fs_materialg>-matnr, 'konnte nicht ermittelt werden. XYZ übersprungen.'.
      <fs_materialg>-xyz_klasse = 'Z'. " .Ohne Werk keine sinnvolle Analyse
      CONTINUE.
    ENDIF.


    " .Verbrauchswerte für das aktuelle Material über den Zeitraum sammeln
    lv_data_points = gc_xyz_period.
    DATA(lv_loop_year)   = lv_year_from.
    DATA(lv_loop_period) = lv_period_from.

    DO lv_data_points TIMES.
      " .Suche MVER-Eintrag für aktuelles Material/Werk/Jahr
      READ TABLE lt_mver_pivot INTO ls_mver_pivot
          WITH KEY matnr = <fs_materialg>-matnr
                   werks = lv_werks " .Werk mitlesen
                   gjahr = lv_loop_year
          BINARY SEARCH.

      IF sy-subrc = 0.
        " .Eintrag für das Jahr gefunden, jetzt den Wert für die Periode holen
        lv_period_char = lv_loop_period. " .Periode zu Char (z.B. '04', '11')
        lv_fieldname = 'GSV' && lv_period_char.
        CONDENSE lv_fieldname NO-GAPS.

        ASSIGN COMPONENT lv_fieldname OF STRUCTURE ls_mver_pivot TO <fs_consumption>. "#EC CI_FLDEXT_OK[2215424]  "#EC CI_NOORDER
        IF sy-subrc = 0 AND <fs_consumption> IS ASSIGNED. "#EC CI_NOORDER
          lv_consumption = CONV #( <fs_consumption> ). " .Wert holen
          APPEND lv_consumption TO lt_consumption_values.
          lv_sum = lv_sum + lv_consumption.
        ELSE.
          " .Feld nicht gefunden (z.B. Periode 13 nicht in Struktur) oder Fehler
          APPEND 0 TO lt_consumption_values.
        ENDIF.
      ELSE.
        " .Kein Eintrag für das ganze Jahr gefunden -> Verbrauch 0 für diese Periode
        APPEND 0 TO lt_consumption_values.
      ENDIF.

      " .Nächsten Monat/Jahr berechnen (Kalendermonat)
      ADD 1 TO lv_loop_period.
      IF lv_loop_period > 12.
        lv_loop_period = 1.
        ADD 1 TO lv_loop_year.
      ENDIF.
    ENDDO.

    " .Restliche Berechnung (Mittelwert, StdAbw, VK, Klassifizierung) bleibt gleich wie vorher
    " .Prüfen, ob Datenpunkte vorhanden sind (wie vorher)
    IF lines( lt_consumption_values ) = 0. <fs_materialg>-xyz_klasse = 'Z'. CONTINUE. ENDIF.

    " .Mittelwert berechnen (wie vorher)
    IF lv_data_points > 0. lv_mean = lv_sum / lv_data_points. ELSE. lv_mean = 0. ENDIF.

    " .Prüfen, ob Mittelwert 0 ist (wie vorher)
    IF lv_mean = 0. <fs_materialg>-xyz_klasse = 'Z'. CONTINUE. ENDIF.

    " .Standardabweichung berechnen (wie vorher)
    LOOP AT lt_consumption_values INTO lv_consumption.
      lv_diff = lv_consumption - lv_mean. lv_sum_sq_diff = lv_sum_sq_diff + lv_diff ** 2.
    ENDLOOP.
    IF lv_data_points > 1. lv_divisor = lv_data_points - 1. IF lv_divisor <> 0. lv_variance = lv_sum_sq_diff / lv_divisor. ELSE. lv_variance = 0. ENDIF. ELSE. lv_variance = 0. ENDIF.
    IF lv_variance > 0. lv_stddev = sqrt( lv_variance ). ELSE. lv_stddev = 0. ENDIF.

    " .Variationskoeffizient (VK) berechnen (wie vorher)
    IF lv_mean <> 0. lv_vk = lv_stddev / lv_mean. ELSE. <fs_materialg>-xyz_klasse = 'Z'. CONTINUE. ENDIF.

    " .XYZ-Klasse zuweisen (wie vorher)
    IF lv_vk <= gc_xyz_limit_x.
      <fs_materialg>-xyz_klasse = 'X'.
    ELSEIF lv_vk <= gc_xyz_limit_y.
      <fs_materialg>-xyz_klasse = 'Y'.
    ELSE.
      <fs_materialg>-xyz_klasse = 'Z'.
    ENDIF.

    " .Debug-Ausgabe (optional)
*     "Write: / 'Material:', <fs_materialg>-matnr, 'Werk:', lv_werks, 'Mean:', lv_mean, 'StdDev:', lv_stddev, 'VK:', lv_vk, 'Class:', <fs_materialg>-xyz_klasse.

  ENDLOOP.

ENDFORM. " CALCULATE_XYZ_ANALYSIS


FORM save_calcc_da_to_lbk.
  DATA: ls_zlagcleanup TYPE zlagcleanup,
        lv_counter     TYPE i VALUE 0,
        lv_fall        TYPE zlagcleanup-fall VALUE '99'. " Neuer Fall für berechnete Daten

  " Alte berechnete Daten löschen (Fall 99)
  DELETE FROM zlagcleanup WHERE fall = lv_fall.
  COMMIT WORK.

  " Durchlaufe alle berechneten Materialdaten
  LOOP AT gt_material_data ASSIGNING FIELD-SYMBOL(<fs_mat>).
    lv_counter = lv_counter + 1.
    CLEAR ls_zlagcleanup.

    " Key-Felder
    ls_zlagcleanup-fall = lv_fall.
    ls_zlagcleanup-feld1 = lv_counter. " Zeilennummer

    " Materialdaten
    ls_zlagcleanup-feld2 = <fs_mat>-bwtk.
    ls_zlagcleanup-feld3 = <fs_mat>-matnr.
    ls_zlagcleanup-feld4 = <fs_mat>-maktx.
    ls_zlagcleanup-feld5 = <fs_mat>-anfangsbestand.
    ls_zlagcleanup-feld6 = <fs_mat>-summe_zugang.
    ls_zlagcleanup-feld7 = <fs_mat>-summe_abgang.
    ls_zlagcleanup-feld8 = <fs_mat>-endbestand.
    ls_zlagcleanup-feld9 = <fs_mat>-bme.
    ls_zlagcleanup-feld10 = <fs_mat>-anfangswert.
    ls_zlagcleanup-feld11 = <fs_mat>-summe_zugwert.
    ls_zlagcleanup-feld12 = <fs_mat>-summe_abgwert.
    ls_zlagcleanup-feld13 = <fs_mat>-endwert.
    ls_zlagcleanup-feld14 = <fs_mat>-waers.
    ls_zlagcleanup-feld15 = <fs_mat>-datum_von.
    ls_zlagcleanup-feld16 = <fs_mat>-datum_bis.
    ls_zlagcleanup-feld17 = <fs_mat>-status.

    " Weitere berechnete Felder in neue Zeilen
    INSERT zlagcleanup FROM ls_zlagcleanup.

    " Zweite Zeile mit weiteren Daten
    lv_counter = lv_counter + 1.
    ls_zlagcleanup-feld1 = lv_counter.
    ls_zlagcleanup-feld2 = 'CALC_DATA'.
    ls_zlagcleanup-feld3 = <fs_mat>-matnr.
    ls_zlagcleanup-feld4 = <fs_mat>-preis_stk.
    ls_zlagcleanup-feld5 = <fs_mat>-disponent.
    ls_zlagcleanup-feld6 = <fs_mat>-me2m_offen.
    ls_zlagcleanup-feld7 = <fs_mat>-me3m_offen.
    ls_zlagcleanup-feld8 = <fs_mat>-mc46_nomove.
    ls_zlagcleanup-feld9 = <fs_mat>-reichweite.
    ls_zlagcleanup-feld10 = <fs_mat>-reichw_m_verschr.
    ls_zlagcleanup-feld11 = <fs_mat>-summe_verschrottet.
    ls_zlagcleanup-feld12 = <fs_mat>-summe_abgang_minus_ver.
    ls_zlagcleanup-feld13 = <fs_mat>-xyz_klasse.
    ls_zlagcleanup-feld14 = <fs_mat>-komment_supply.
    ls_zlagcleanup-feld15 = <fs_mat>-komment_sales.
    ls_zlagcleanup-feld16 = <fs_mat>-komment_finanzen.
    ls_zlagcleanup-feld17 = <fs_mat>-zzschrottrsk.

    INSERT zlagcleanup FROM ls_zlagcleanup.
  ENDLOOP.

  COMMIT WORK.

  MESSAGE i001(00) WITH 'Berechnete Daten in ZLAGCLEANUP gespeichert:' lv_counter 'Zeilen'.
ENDFORM.

FORM read_calcu_f_tlbk.
  DATA: lt_zlagcleanup TYPE TABLE OF zlagcleanup,
        ls_material    TYPE ty_material_data.

  " Lese berechnete Daten (Fall 99)
  SELECT * FROM zlagcleanup
    INTO TABLE lt_zlagcleanup
    WHERE fall = '99'
    ORDER BY feld1.

  IF sy-subrc = 0.
    " Konvertiere zurück in gt_material_data Format
    LOOP AT lt_zlagcleanup INTO DATA(ls_zlag).
      IF ls_zlag-feld2 NE 'CALC_DATA'. " Erste Zeile mit Basisdaten
        CLEAR ls_material.

        " === BASIS-FELDER ===
        ls_material-bwtk = ls_zlag-feld2.
        ls_material-matnr = ls_zlag-feld3.
        ls_material-maktx = ls_zlag-feld4.
        ls_material-bme = ls_zlag-feld9.
        ls_material-waers = ls_zlag-feld14.
        ls_material-datum_von = ls_zlag-feld15.
        ls_material-datum_bis = ls_zlag-feld16.
        ls_material-status = ls_zlag-feld17.

        " === STRING-FELDER ===
        ls_material-anfangsbestand = ls_zlag-feld5.
        ls_material-summe_zugang = ls_zlag-feld6.
        ls_material-summe_abgang = ls_zlag-feld7.
        ls_material-endbestand = ls_zlag-feld8.
        ls_material-anfangswert = ls_zlag-feld10.
        ls_material-summe_zugwert = ls_zlag-feld11.
        ls_material-summe_abgwert = ls_zlag-feld12.
        ls_material-endwert = ls_zlag-feld13.

        " === NUMERISCHE FELDER BEFÜLLEN (NEU!) ===
        PERFORM parse_quan USING ls_material-anfangsbestand CHANGING ls_material-anfangsbestand_n.
        PERFORM parse_quan USING ls_material-summe_zugang   CHANGING ls_material-summe_zugang_n.
        PERFORM parse_quan USING ls_material-summe_abgang   CHANGING ls_material-summe_abgang_n.
        PERFORM parse_quan USING ls_material-endbestand     CHANGING ls_material-endbestand_n.

        PERFORM parse_curr USING ls_material-anfangswert   CHANGING ls_material-anfangswert_n.
        PERFORM parse_curr USING ls_material-summe_zugwert CHANGING ls_material-summe_zugwert_n.
        PERFORM parse_curr USING ls_material-summe_abgwert CHANGING ls_material-summe_abgwert_n.
        PERFORM parse_curr USING ls_material-endwert       CHANGING ls_material-endwert_n.

        " Lese nächste Zeile mit berechneten Daten
        READ TABLE lt_zlagcleanup INTO DATA(ls_calc)
          WITH KEY feld2 = 'CALC_DATA'
                   feld3 = ls_material-matnr.

        IF sy-subrc = 0.
          " String-Felder
          ls_material-preis_stk = ls_calc-feld4.
          ls_material-disponent = ls_calc-feld5.
          ls_material-me2m_offen = ls_calc-feld6.
          ls_material-me3m_offen = ls_calc-feld7.
          ls_material-mc46_nomove = ls_calc-feld8.
          ls_material-reichweite = ls_calc-feld9.
          ls_material-reichw_m_verschr = ls_calc-feld10.
          ls_material-summe_verschrottet = ls_calc-feld11.
          ls_material-summe_abgang_minus_ver = ls_calc-feld12.
          ls_material-xyz_klasse = ls_calc-feld13.
          ls_material-komment_supply = ls_calc-feld14.
          ls_material-komment_sales = ls_calc-feld15.
          ls_material-komment_finanzen = ls_calc-feld16.
          ls_material-zzschrottrsk = ls_calc-feld17.

          " === NUMERISCHE FELDER BEFÜLLEN (NEU!) ===
          PERFORM parse_curr USING ls_material-preis_stk CHANGING ls_material-preis_stk_n.
          PERFORM parse_quan USING ls_material-me2m_offen CHANGING ls_material-me2m_offen_n.
          PERFORM parse_quan USING ls_material-me3m_offen CHANGING ls_material-me3m_offen_n.
          PERFORM parse_quan USING ls_material-reichweite CHANGING ls_material-reichweite_n.
          PERFORM parse_quan USING ls_material-reichw_m_verschr CHANGING ls_material-reichw_m_verschr_n.
          PERFORM parse_quan USING ls_material-summe_verschrottet CHANGING ls_material-summe_verschrottet_n.
          PERFORM parse_quan USING ls_material-summe_abgang_minus_ver CHANGING ls_material-summe_abgang_minus_ver_n.
        ENDIF.

        APPEND ls_material TO gt_material_data.
      ENDIF.
    ENDLOOP.
  ELSE.
    " Keine Daten gefunden
    MESSAGE i001(00) WITH 'Keine gespeicherten Daten in ZLAGCLEANUP (Fall 99) gefunden.'.
  ENDIF.
ENDFORM.





**&---------------------------------------------------------------------*
**& Report Z_MATERIAL_ANALYSIS_FOR_LLM
**&---------------------------------------------------------------------*
**&
**&---------------------------------------------------------------------*
*REPORT Z_MATERIAL_ANALYSIS_FOR_LLM.
*
*TYPES: BEGIN OF ty_material_analysis,
*         lgort        TYPE lgort_d,
*         matnr        TYPE matnr,
*         maktx        TYPE maktx,
*         labst        TYPE labst,
*         meins        TYPE meins,
*         verpr        TYPE verpr,
*         gesamt_wert  TYPE dmbtr,
*         last_move    TYPE dats,
*         abc_category TYPE char1,
*         days_no_move TYPE i,           " Neu: Tage ohne Bewegung
*         turnover     TYPE p LENGTH 5 DECIMALS 2, " Neu: Umschlagshäufigkeit
*       END OF ty_material_analysis,
*
*       BEGIN OF ty_storage_places,
*         lgort           TYPE lgort_d,
*         total_places    TYPE i,
*         used_places     TYPE i,
*         free_places     TYPE i,
*         usage_pct       TYPE p LENGTH 5 DECIMALS 2,
*         critical_status TYPE char1,    " Neu: A=kritisch, B=hoch, C=normal
*       END OF ty_storage_places,
*
*       " Neu: Analyse für Lagerplätze mit Mischbelegung
*       BEGIN OF ty_mixed_storage,
*         lgort        TYPE lgort_d,
*         lgpla        TYPE lgpla,
*         material_cnt TYPE i,
*         materials    TYPE string,
*       END OF ty_mixed_storage,
*
*       " Neu: Analyse für potentielle Abschreibungen
*       BEGIN OF ty_write_off_potential,
*         lgort          TYPE lgort_d,
*         matnr          TYPE matnr,
*         maktx          TYPE maktx,
*         months_no_move TYPE i,
*         gesamt_wert    TYPE dmbtr,
*         write_off_pct  TYPE p LENGTH 5 DECIMALS 2, " Abschreibungsprozentsatz
*         write_off_val  TYPE dmbtr,                 " Abschreibungswert
*       END OF ty_write_off_potential.
*
*CONSTANTS: c_abc_class_a TYPE p LENGTH 5 DECIMALS 2 VALUE '0.80',
*           c_abc_class_b TYPE p LENGTH 5 DECIMALS 2 VALUE '0.95',
*           c_crit_usage  TYPE p LENGTH 5 DECIMALS 2 VALUE '90.00',  " Neu: Kritische Lagerplatznutzung
*           c_high_usage  TYPE p LENGTH 5 DECIMALS 2 VALUE '75.00',  " Neu: Hohe Lagerplatznutzung
*           c_month_12    TYPE i VALUE 12,                           " Neu: 12 Monate
*           c_month_24    TYPE i VALUE 24,                           " Neu: 24 Monate
*           c_month_48    TYPE i VALUE 48.                           " Neu: 48 Monate (für ICs)
*
*DATA: gt_analysis           TYPE TABLE OF ty_material_analysis,
*      gt_storage            TYPE TABLE OF ty_storage_places,
*      gt_mixed_storage      TYPE TABLE OF ty_mixed_storage,        " Neu
*      gt_write_off          TYPE TABLE OF ty_write_off_potential,  " Neu
*      gt_no_move_12         TYPE TABLE OF ty_material_analysis,    " Neu
*      gt_no_move_24         TYPE TABLE OF ty_material_analysis,    " Neu
*      gs_analysis           TYPE ty_material_analysis,
*      gs_storage            TYPE ty_storage_places,
*      gv_date_cutoff        TYPE dats,
*      gv_date_cutoff_12m    TYPE dats,                            " Neu
*      gv_date_cutoff_24m    TYPE dats,                            " Neu
*      gv_total_stock_value  TYPE dmbtr,                           " Neu: Gesamtbestandswert
*      gv_write_off_value    TYPE dmbtr.                           " Neu: Gesamtabschreibungswert
*
*" Selektionsbild
*SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE TEXT-001.
*  SELECT-OPTIONS: s_lgort FOR gs_analysis-lgort,
*                 s_matnr FOR gs_analysis-matnr.
*  PARAMETERS: p_days TYPE i DEFAULT 90 OBLIGATORY.
*
*  " Neu: Parameter für Abschreibungsrichtlinien
*  PARAMETERS: p_ic_mon TYPE i DEFAULT 48 OBLIGATORY,     " Monate für IC-Abschreibung
*             p_stdm TYPE i DEFAULT 24 OBLIGATORY.    " Standardmonate für Abschreibung
*SELECTION-SCREEN END OF BLOCK b1.
*
*INITIALIZATION.
*  "TEXT-001 = 'Materialdatenanalyse'.
*
*START-OF-SELECTION.
*  gv_date_cutoff = sy-datum - p_days.
*  " Neu: Berechne Cutoff-Daten für 12 und 24 Monate
*  gv_date_cutoff_12m = sy-datum - ( 30 * c_month_12 ).
*  gv_date_cutoff_24m = sy-datum - ( 30 * c_month_24 ).
*
*  " 1. Lagerplatzanalyse (korrekte Berechnung freier Lagerplätze)
*  SELECT q~lgort,
*         COUNT( DISTINCT l~lgpla ) AS total_places,
*         COUNT( DISTINCT CASE WHEN q~matnr IS NOT NULL THEN l~lgpla END ) AS used_places,
*         COUNT( DISTINCT CASE WHEN q~matnr IS NULL THEN l~lgpla END ) AS free_places
*    FROM lagp AS l
*    LEFT OUTER JOIN lqua AS q ON q~lgpla = l~lgpla
*    WHERE q~lgort IN @s_lgort
*    GROUP BY q~lgort
*    ORDER BY q~lgort
*    INTO CORRESPONDING FIELDS OF TABLE @gt_storage.
*
*  " Prozentberechnung nachträglich in ABAP durchführen
*  LOOP AT gt_storage ASSIGNING FIELD-SYMBOL(<fs_storage>).
*    IF <fs_storage>-total_places > 0.
*      <fs_storage>-usage_pct = <fs_storage>-used_places * 100 / <fs_storage>-total_places.
*
*      " Neu: Status für kritische Füllstände
*      IF <fs_storage>-usage_pct >= c_crit_usage.
*        <fs_storage>-critical_status = 'A'. " Kritisch
*      ELSEIF <fs_storage>-usage_pct >= c_high_usage.
*        <fs_storage>-critical_status = 'B'. " Hoch
*      ELSE.
*        <fs_storage>-critical_status = 'C'. " Normal
*      ENDIF.
*    ENDIF.
*  ENDLOOP.
*
*" 2. Materialbestandsanalyse mit korrigierter Verknüpfung
*SELECT m~lgort, m~matnr, mk~maktx, m~labst, ma~meins, mb~stprs AS verpr,
*       CASE WHEN mb~stprs IS NOT NULL AND mb~stprs > 0
*            THEN m~labst * mb~stprs
*            ELSE 0 END AS gesamt_wert,
*       CASE WHEN MAX( k~budat ) IS NOT NULL
*            THEN CAST( MAX( k~budat ) AS DATS )
*            ELSE CAST( '00000000' AS DATS ) END AS last_move
*  FROM mard AS m
*  INNER JOIN mara AS ma ON ma~matnr = m~matnr
*  INNER JOIN makt AS mk ON mk~matnr = m~matnr
*                       AND mk~spras = @sy-langu
*  LEFT OUTER JOIN mbew AS mb ON mb~matnr = m~matnr
*                            AND mb~bwkey = m~werks
*  LEFT OUTER JOIN mseg AS b ON b~matnr = m~matnr
*                          AND b~lgort = m~lgort
*  LEFT OUTER JOIN mkpf AS k ON k~mblnr = b~mblnr
*                          AND k~mjahr = b~mjahr
*                          AND k~budat >= @gv_date_cutoff
*  WHERE m~lgort IN @s_lgort
*    AND m~matnr IN @s_matnr
*    AND m~labst > 0
*  GROUP BY m~lgort, m~matnr, mk~maktx, m~labst, ma~meins, mb~stprs
*  ORDER BY m~lgort, m~matnr
*  INTO CORRESPONDING FIELDS OF TABLE @gt_analysis.
*
*  " Neu: Berechne Tage ohne Bewegung und Umschlagshäufigkeit
*  LOOP AT gt_analysis ASSIGNING FIELD-SYMBOL(<fs_analysis>).
*    " Gesamtbestandswert aktualisieren
*    gv_total_stock_value = gv_total_stock_value + <fs_analysis>-gesamt_wert.
*
*    " Tage ohne Bewegung berechnen
*    IF <fs_analysis>-last_move = '00000000'.
*      " Wenn keine Bewegung erfasst wurde, nehmen wir an, dass seit Systemeinführung keine Bewegung stattfand
*      <fs_analysis>-days_no_move = 999. " Platzhalter für "sehr lange"
*    ELSE.
*      <fs_analysis>-days_no_move = sy-datum - <fs_analysis>-last_move.
*    ENDIF.
*
*    " Umschlagshäufigkeit berechnen (vereinfacht als Bewegungen pro Jahr)
*    IF <fs_analysis>-days_no_move > 0 AND <fs_analysis>-days_no_move < 999.
*      <fs_analysis>-turnover = 365 / <fs_analysis>-days_no_move.
*    ELSE.
*      <fs_analysis>-turnover = 0.
*    ENDIF.
*  ENDLOOP.
*
*  " Neu: Identifiziere Materialien ohne Bewegung für 12 und 24 Monate
*  gt_no_move_12 = VALUE #( FOR ls_mat IN gt_analysis
*                         WHERE ( last_move < gv_date_cutoff_12m OR last_move = '00000000' )
*                         ( ls_mat ) ).
*
*  gt_no_move_24 = VALUE #( FOR ls_mat IN gt_analysis
*                         WHERE ( last_move < gv_date_cutoff_24m OR last_move = '00000000' )
*                         ( ls_mat ) ).
*
*  " Neu: Mischbelegungsanalyse (ohne STRING_AGG und DISTINCT)
*  SELECT q~lgort, q~lgpla, COUNT( DISTINCT q~matnr ) AS material_cnt
*    FROM lqua AS q
*    WHERE q~lgort IN @s_lgort
*    GROUP BY q~lgort, q~lgpla
*    HAVING COUNT( DISTINCT q~matnr ) > 1
*    ORDER BY q~lgort, q~lgpla
*    INTO CORRESPONDING FIELDS OF TABLE @gt_mixed_storage.
*
*  " Materialien für Mischbelegung nachträglich ermitteln
*  LOOP AT gt_mixed_storage ASSIGNING FIELD-SYMBOL(<fs_mixed>).
*    DATA: lt_materials TYPE TABLE OF matnr,
*          lv_matnr     TYPE matnr.
*
*    SELECT q~matnr FROM lqua AS q
*      WHERE q~lgort = @<fs_mixed>-lgort
*        AND q~lgpla = @<fs_mixed>-lgpla
*      INTO TABLE @lt_materials.
*
*    " Liste der Materialien als String zusammenbauen
*    LOOP AT lt_materials INTO lv_matnr.
*      IF <fs_mixed>-materials IS INITIAL.
*        <fs_mixed>-materials = lv_matnr.
*      ELSE.
*        <fs_mixed>-materials = |{ <fs_mixed>-materials }, { lv_matnr }|.
*      ENDIF.
*    ENDLOOP.
*  ENDLOOP.
*
*  " ABC-Analyse durchführen
*  PERFORM calculate_abc_categories.
*
*  " Neu: Potenzielle Abschreibungen basierend auf Bewegungsdaten
*  LOOP AT gt_no_move_24 INTO DATA(ls_old_material).
*    DATA: ls_write_off TYPE ty_write_off_potential,
*          lv_months_no_move TYPE i,
*          lv_is_ic TYPE abap_bool VALUE abap_false.
*
*    " Monate ohne Bewegung berechnen
*    IF ls_old_material-last_move = '00000000'.
*      lv_months_no_move = 99. " Platzhalter für "sehr lange"
*    ELSE.
*      lv_months_no_move = ( sy-datum - ls_old_material-last_move ) / 30.
*    ENDIF.
*
*    " Prüfen ob es sich um einen IC handelt (hier vereinfacht)
*    SELECT SINGLE mtart INTO @DATA(lv_mtart) FROM mara WHERE matnr = @ls_old_material-matnr.
*    IF lv_mtart = 'ROH' OR lv_mtart = 'HAWA'.  " Beispielhafte IC-Materialarten
*      lv_is_ic = abap_true.
*    ENDIF.
*
*    " Abschreibungsdetails erfassen
*    ls_write_off-lgort = ls_old_material-lgort.
*    ls_write_off-matnr = ls_old_material-matnr.
*    ls_write_off-maktx = ls_old_material-maktx.
*    ls_write_off-months_no_move = lv_months_no_move.
*    ls_write_off-gesamt_wert = ls_old_material-gesamt_wert.
*
*    " Abschreibungsregeln anwenden
*    IF lv_is_ic = abap_true.
*      " ICs haben längere Abschreibungsfristen
*      IF lv_months_no_move >= p_ic_mon.
*        ls_write_off-write_off_pct = 100.
*      ELSEIF lv_months_no_move >= p_ic_mon * 3 / 4.
*        ls_write_off-write_off_pct = 75.
*      ELSEIF lv_months_no_move >= p_ic_mon / 2.
*        ls_write_off-write_off_pct = 50.
*      ELSEIF lv_months_no_move >= p_ic_mon / 4.
*        ls_write_off-write_off_pct = 25.
*      ELSE.
*        ls_write_off-write_off_pct = 0.
*      ENDIF.
*    ELSE.
*      " Standardmaterialien
*      IF lv_months_no_move >= p_stdm.
*        ls_write_off-write_off_pct = 100.
*      ELSEIF lv_months_no_move >= p_stdm * 3 / 4.
*        ls_write_off-write_off_pct = 75.
*      ELSEIF lv_months_no_move >= p_stdm / 2.
*        ls_write_off-write_off_pct = 50.
*      ELSEIF lv_months_no_move >= p_stdm / 4.
*        ls_write_off-write_off_pct = 25.
*      ELSE.
*        ls_write_off-write_off_pct = 0.
*      ENDIF.
*    ENDIF.
*
*    " Abschreibungswert berechnen
*    ls_write_off-write_off_val = ls_write_off-gesamt_wert * ls_write_off-write_off_pct / 100.
*
*    " Gesamtabschreibungswert aktualisieren
*    gv_write_off_value = gv_write_off_value + ls_write_off-write_off_val.
*
*    " In Tabelle speichern
*    APPEND ls_write_off TO gt_write_off.
*  ENDLOOP.
*
*  " Ausgabe der Analysen
*  PERFORM display_results.
*
**&---------------------------------------------------------------------*
**& Form CALCULATE_ABC_CATEGORIES
**&---------------------------------------------------------------------*
*FORM calculate_abc_categories.
*  DATA: lv_total_value TYPE dmbtr,
*        lv_cum_value   TYPE dmbtr,
*        lv_threshold_a TYPE dmbtr,
*        lv_threshold_b TYPE dmbtr.
*
*  " Gesamtwert berechnen
*  LOOP AT gt_analysis ASSIGNING FIELD-SYMBOL(<fs_analysis>).
*    lv_total_value = lv_total_value + <fs_analysis>-gesamt_wert.
*  ENDLOOP.
*
*  " Schwellenwerte berechnen
*  IF lv_total_value > 0.
*    lv_threshold_a = lv_total_value * c_abc_class_a.
*    lv_threshold_b = lv_total_value * c_abc_class_b.
*
*    " Sortieren nach Wert absteigend
*    SORT gt_analysis BY gesamt_wert DESCENDING.
*
*    " ABC-Klassen zuordnen
*    LOOP AT gt_analysis ASSIGNING <fs_analysis>.
*      lv_cum_value = lv_cum_value + <fs_analysis>-gesamt_wert.
*
*      IF lv_cum_value <= lv_threshold_a.
*        <fs_analysis>-abc_category = 'A'.
*      ELSEIF lv_cum_value <= lv_threshold_b.
*        <fs_analysis>-abc_category = 'B'.
*      ELSE.
*        <fs_analysis>-abc_category = 'C'.
*      ENDIF.
*    ENDLOOP.
*  ENDIF.
*ENDFORM.
*
**&---------------------------------------------------------------------*
**& Form DISPLAY_RESULTS
**&---------------------------------------------------------------------*
*FORM display_results.
*  " Gesamtstatistiken
*  cl_demo_output=>begin_section( 'Materialbestandsübersicht' ).
*  cl_demo_output=>write_text( |Gesamtwert aller Materialien: { gv_total_stock_value } EUR| ).
*  cl_demo_output=>write_text( |Potentieller Abschreibungswert: { gv_write_off_value } EUR| ).
*  cl_demo_output=>write_text( |Analysezeitraum: { p_days } Tage (seit { gv_date_cutoff })| ).
*
*  " 1. Lagerplatzauslastung
*  cl_demo_output=>begin_section( 'Lagerplatzauslastung pro Lagerort' ).
*  cl_demo_output=>write_data(
*    value = gt_storage
*    name  = 'Lagerplatzauslastung'
*  ).
*
*  " Kritische Lagerplätze hervorheben
*  DATA lt_critical_storage LIKE gt_storage.
*  lt_critical_storage = VALUE #( FOR ls_store IN gt_storage
*                               WHERE ( critical_status = 'A' ) ( ls_store ) ).
*
*  IF lines( lt_critical_storage ) > 0.
*    cl_demo_output=>begin_section( 'Kritische Lagerplatzauslastung (>90%)' ).
*    cl_demo_output=>write_data(
*      value = lt_critical_storage
*      name  = 'Kritische Lagerplätze'
*    ).
*  ENDIF.
*
*  " 2. Materialien mit höchstem Bestandswert
*  DATA lt_top_materials LIKE gt_analysis.
*  lt_top_materials = gt_analysis.
*  SORT lt_top_materials BY gesamt_wert DESCENDING.
*
*  cl_demo_output=>begin_section( 'Top 10 Materialien nach Bestandswert' ).
*
*  " Korrekte Syntax für ABAP: Top 10 Materialien extrahieren
*  DATA: lt_top10 LIKE gt_analysis.
*  DATA: lv_count TYPE i VALUE 0.
*
*  LOOP AT lt_top_materials INTO DATA(ls_top_mat).
*    lv_count = lv_count + 1.
*    APPEND ls_top_mat TO lt_top10.
*    IF lv_count >= 10.
*      EXIT.
*    ENDIF.
*  ENDLOOP.
*
*  cl_demo_output=>write_data(
*    value = lt_top10
*    name  = 'Top 10 nach Wert'
*  ).
*
*  " 3. Materialien ohne Bewegung
*  DATA lt_no_movement LIKE gt_analysis.
*  lt_no_movement = VALUE #( FOR ls_mat IN gt_analysis
*                          WHERE ( last_move = '00000000' ) ( ls_mat ) ).
*
*  cl_demo_output=>begin_section( |Materialien ohne Bewegung seit { p_days } Tagen| ).
*  cl_demo_output=>write_data(
*    value = lt_no_movement
*    name  = 'Keine Bewegung'
*  ).
*
*  " Neu: 12 Monate ohne Bewegung
*  IF lines( gt_no_move_12 ) > 0.
*    cl_demo_output=>begin_section( 'Materialien ohne Bewegung seit 12 Monaten' ).
*    cl_demo_output=>write_data(
*      value = gt_no_move_12
*      name  = 'Keine Bewegung 12M'
*    ).
*  ENDIF.
*
*  " Neu: 24 Monate ohne Bewegung
*  IF lines( gt_no_move_24 ) > 0.
*    cl_demo_output=>begin_section( 'Materialien ohne Bewegung seit 24 Monaten' ).
*    cl_demo_output=>write_data(
*      value = gt_no_move_24
*      name  = 'Keine Bewegung 24M'
*    ).
*  ENDIF.
*
*  " 4. ABC Analyse
*  DATA: lt_class_a LIKE gt_analysis,
*        lt_class_b LIKE gt_analysis,
*        lt_class_c LIKE gt_analysis.
*
*  lt_class_a = VALUE #( FOR ls_mat IN gt_analysis
*                       WHERE ( abc_category = 'A' ) ( ls_mat ) ).
*  lt_class_b = VALUE #( FOR ls_mat IN gt_analysis
*                       WHERE ( abc_category = 'B' ) ( ls_mat ) ).
*  lt_class_c = VALUE #( FOR ls_mat IN gt_analysis
*                       WHERE ( abc_category = 'C' ) ( ls_mat ) ).
*
*  cl_demo_output=>begin_section( 'ABC Analyse' ).
*  cl_demo_output=>write_data( value = lt_class_a name = 'A-Klasse' ).
*  cl_demo_output=>write_data( value = lt_class_b name = 'B-Klasse' ).
*  cl_demo_output=>write_data( value = lt_class_c name = 'C-Klasse' ).
*
*  " Neu: 5. Lagerplätze mit Mischbelegung
*  IF lines( gt_mixed_storage ) > 0.
*    cl_demo_output=>begin_section( 'Lagerplätze mit Mischbelegung' ).
*    cl_demo_output=>write_data(
*      value = gt_mixed_storage
*      name  = 'Mischbelegung'
*    ).
*  ENDIF.
*
*  " Neu: 6. Top 10 nach Lagerdauer (längste Zeit ohne Bewegung)
*  DATA lt_top_duration LIKE gt_analysis.
*  lt_top_duration = gt_analysis.
*  SORT lt_top_duration BY days_no_move DESCENDING.
*
*  cl_demo_output=>begin_section( 'Top 10 Materialien nach Lagerdauer' ).
*
*  " Korrekte Syntax für ABAP: Top 10 nach Lagerdauer
*  DATA: lt_top10_duration LIKE gt_analysis.
*  lv_count = 0.
*
*  LOOP AT lt_top_duration INTO DATA(ls_duration).
*    lv_count = lv_count + 1.
*    APPEND ls_duration TO lt_top10_duration.
*    IF lv_count >= 10.
*      EXIT.
*    ENDIF.
*  ENDLOOP.
*
*  cl_demo_output=>write_data(
*    value = lt_top10_duration
*    name  = 'Top 10 nach Lagerdauer'
*  ).
*
*  " Neu: 7. Top 10 nach Umschlagshäufigkeit
*  DATA lt_top_turnover LIKE gt_analysis.
*  lt_top_turnover = gt_analysis.
*  SORT lt_top_turnover BY turnover DESCENDING.
*
*  cl_demo_output=>begin_section( 'Top 10 Materialien nach Umschlagshäufigkeit' ).
*
*  " Korrekte Syntax für ABAP: Top 10 nach Umschlagshäufigkeit
*  DATA: lt_top10_turnover LIKE gt_analysis.
*  lv_count = 0.
*
*  LOOP AT lt_top_turnover INTO DATA(ls_turnover).
*    lv_count = lv_count + 1.
*    APPEND ls_turnover TO lt_top10_turnover.
*    IF lv_count >= 10.
*      EXIT.
*    ENDIF.
*  ENDLOOP.
*
*  cl_demo_output=>write_data(
*    value = lt_top10_turnover
*    name  = 'Top 10 nach Umschlagshäufigkeit'
*  ).
*
*  " Neu: 8. Potenzielle Abschreibungen
*  IF lines( gt_write_off ) > 0.
*    cl_demo_output=>begin_section( 'Potenzielle Abschreibungen' ).
*    cl_demo_output=>write_data(
*      value = gt_write_off
*      name  = 'Abschreibungspotenzial'
*    ).
*  ENDIF.
*
*  cl_demo_output=>display( ).
*ENDFORM.
