*&---------------------------------------------------------------------*
*& Report ZM_LZCODE20_OPT
*& Hochoptimierte Stücklistenanalyse Top-Down / Bottom-Up
*& Kompatibel mit älteren SAP-Versionen (kein Inline-DATA)
*&---------------------------------------------------------------------*
*& FIXES APPLIED:
*&   FIX 1: convert_menge - Rundung auf dec=0 entfernt (killt kleine Mengen)
*&   FIX 2: DELETE ADJACENT DUPLICATES entfernt (killt Mehrfachverwendungen)
*&   FIX 4: Textpositionen (Typ T) werden jetzt angezeigt
*&          - DELETE gt_ktab WHERE maktx IS INITIAL entfernt
*&          - fill_ktab: Fallback-Text aus ZPOWERBI_VC für Textpositionen
*&   FIX 5: convert_menge - leere Ziel-Einheit abgefangen
*&   FIX 12 (2026-10-16): KTAB-Kennzahlen neu, wenn der erste Treffer eine Textposition war
*&   FIX 19 (2026-10-20): Spalten REACH, SVHC, PFAS-Gehalt aus Klasse Z_PACK_REGULATION
*&   FIX 19 Nachtrag (2026-10-22): Spalte RoHS (Merkmal ROHS, angelegt 21.10.)
*&   FIX 20 (2026-10-22): Download als .txt und Oeffnen mit Excel statt .xls
*&   FIX 21 (2026-10-22): Download als echte Excel-Datei (.xlsx) statt Tab-Text
*&---------------------------------------------------------------------*
REPORT zm_lzcode20_opt LINE-SIZE 132.



*&---------------------------------------------------------------------*
*& Konstanten
*&---------------------------------------------------------------------*
CONSTANTS: gc_werks TYPE werks_d VALUE '1100'.

*&---------------------------------------------------------------------*
*& Typen für optimierte Verarbeitung
*&---------------------------------------------------------------------*
TYPES:
  " Einfacher Materialnummer-Typ für FOR ALL ENTRIES
  BEGIN OF ty_matnr_line,
    matnr TYPE matnr,
  END OF ty_matnr_line,

  " Hauptdaten aus zpowerbi_vc_txt
  BEGIN OF ty_powerbi,
    id            TYPE zpowerbi_vc_txt-id,
    stufe         TYPE zpowerbi_vc_txt-stufe,
    kompnr        TYPE zpowerbi_vc_txt-kompnr,
    menge         TYPE zpowerbi_vc_txt-menge,
    mengeneinheit TYPE zpowerbi_vc_txt-mengeneinheit,
    baugruppe     TYPE zpowerbi_vc_txt-baugruppe,
    matnr         TYPE zpowerbi_vc_txt-matnr,
    kom_mstae     TYPE zpowerbi_vc_txt-kom_mstae,
    postyp        TYPE zpowerbi_vc_txt-postyp,
  END OF ty_powerbi,

  " VTAB: Spalten-Materialien
  BEGIN OF ty_vtab,
    matnr TYPE matnr,
    meins TYPE meins,
    verbr TYPE menge_d,
    sp    TYPE char3,
    mstae TYPE mstae,      " NEU: Materialstatus der VKNR
  END OF ty_vtab,

  " Für ZMD04_CALC Cache
  BEGIN OF ty_md04_full,
    matnr      TYPE matnr,
    werks      TYPE werks_d,
    labst      TYPE labst,
    feste_zug  TYPE labst,
    gepl_zug   TYPE labst,
    feste_abg  TYPE labst,
    gepl_abg   TYPE labst,
    verbr      TYPE menge_d,
    omeng      TYPE menge_d,
    owert      TYPE salk3,
    mkmng      TYPE menge_d,
    omkwr      TYPE salk3,
    exklusiv   TYPE char1,
    exkl_count TYPE i,
  END OF ty_md04_full,

  tt_md04_full TYPE HASHED TABLE OF ty_md04_full WITH UNIQUE KEY matnr,


  " KTAB: Zeilen-Materialien mit allen Stammdaten
  BEGIN OF ty_ktab,
    idnrk           TYPE matnr,
    matnr           TYPE matnr,
    maktx           TYPE maktx,
    meins           TYPE meins,
    disst           TYPE disst,
    baugr           TYPE char1,
    verbr           TYPE menge_d,
    labst           TYPE labst,
    feste_zug       TYPE menge_d,
    gepl_zug        TYPE menge_d,
    feste_abg       TYPE menge_d,
    gepl_abg        TYPE menge_d,
    lbkum           TYPE lbkum,
    omeng           TYPE menge_d,
    mkmng           TYPE menge_d,
    verpr           TYPE verpr,
    stprs           TYPE stprs,
    peinh           TYPE peinh,
    vprsv           TYPE vprsv,
    salk3           TYPE salk3,
    owert           TYPE salk3,
    omkwr           TYPE salk3,

    endbestand      TYPE menge_d,      " NEU: Summe D bis H
    stueckkosten    TYPE p LENGTH 11 DECIMALS 2,  " NEU: Stückkosten (VERPR oder STPRS / PEINH)
    wert_feste_zug  TYPE p LENGTH 11 DECIMALS 2, " NEU: Wert Feste Zugänge
    wert_gepl_zug   TYPE p LENGTH 11 DECIMALS 2, " NEU: Wert Geplante Zugänge
    wert_feste_abg  TYPE p LENGTH 11 DECIMALS 2, " NEU: Wert Feste Abgänge
    wert_gepl_abg   TYPE p LENGTH 11 DECIMALS 2, " NEU: Wert Geplante Abgänge
    wert_endbestand TYPE p LENGTH 11 DECIMALS 2, " NEU: Wert Endbestand



    dismm           TYPE dismm,
    minbe           TYPE minbe,
    disls           TYPE disls,
    bstfe           TYPE bstfe,
    eisbe           TYPE eisbe,
    mstae           TYPE mstae,
    mstav           TYPE mstav,
    beskz           TYPE beskz,
    zzlzcod         TYPE mara-zzlzcod,
    zzlzcodsort     TYPE mara-zzlzcodsort,
    postyp          TYPE postp,            " Positionstyp (L/T/...)
    dispo           TYPE marc-dispo,       " FIX 22: Disponent (Werk p_werks)
    matkl           TYPE mara-matkl,       " FIX 22: Warengruppe
  END OF ty_ktab,

  " MTAB: Mengen-Zuordnung
  BEGIN OF ty_mtab,
    matnr TYPE matnr,
    idnrk TYPE matnr,
    menge TYPE menge_d,
  END OF ty_mtab,

  " Für Exklusivitäts-Berechnung
  BEGIN OF ty_exkl,
    kompnr   TYPE matnr,
    count    TYPE i,
    exklusiv TYPE char1,
  END OF ty_exkl,

  " Für Bulk-Verbrauchsermittlung
  BEGIN OF ty_mver,
    matnr TYPE matnr,
    gsv   TYPE menge_d,
  END OF ty_mver,

  " Für MD04-Daten
  BEGIN OF ty_md04,
    matnr     TYPE matnr,
    labst     TYPE labst,
    feste_zug TYPE menge_d,
    gepl_zug  TYPE menge_d,
    feste_abg TYPE menge_d,
    gepl_abg  TYPE menge_d,
  END OF ty_md04,

  " Stammdaten-Cache
  BEGIN OF ty_stamm,
    matnr       TYPE matnr,
    maktx       TYPE maktx,
    meins       TYPE meins,
    mstae       TYPE mstae,
    mstav       TYPE mstav,
    lvorm       TYPE lvorm,
    disst       TYPE disst,
    zzlzcod     TYPE mara-zzlzcod,
    zzlzcodsort TYPE mara-zzlzcodsort,
    dismm       TYPE dismm,
    minbe       TYPE minbe,
    disls       TYPE disls,
    bstfe       TYPE bstfe,
    eisbe       TYPE eisbe,
    beskz       TYPE beskz,
    lbkum       TYPE lbkum,
    verpr       TYPE verpr,

    stprs       TYPE stprs,
    peinh       TYPE peinh,
    vprsv       TYPE vprsv,
    salk3       TYPE salk3,
    has_bom     TYPE char1,
    dispo       TYPE marc-dispo,   " FIX 22
    matkl       TYPE mara-matkl,   " FIX 22
  END OF ty_stamm,

  " Für Stammdaten-Abfrage
  BEGIN OF ty_stamm_raw,
    matnr       TYPE matnr,
    maktx       TYPE maktx,
    meins       TYPE meins,
    mstae       TYPE mstae,
    mstav       TYPE mstav,
    lvorm       TYPE lvorm,
    disst       TYPE disst,
    zzlzcod     TYPE mara-zzlzcod,
    zzlzcodsort TYPE mara-zzlzcodsort,
    dismm       TYPE dismm,
    minbe       TYPE minbe,
    disls       TYPE disls,
    bstfe       TYPE bstfe,
    eisbe       TYPE eisbe,
    beskz       TYPE beskz,

    lbkum       TYPE lbkum,
    verpr       TYPE verpr,
    stprs       TYPE stprs,
    peinh       TYPE peinh,
    vprsv       TYPE vprsv,
    salk3       TYPE salk3,
    dispo       TYPE marc-dispo,   " FIX 22, Reihenfolge wie im SELECT
    matkl       TYPE mara-matkl,   " FIX 22
  END OF ty_stamm_raw,

  " Für MVER-Abfrage
  BEGIN OF ty_mver_raw,
    matnr TYPE matnr,
    gsv01 TYPE menge_d,
    gsv02 TYPE menge_d,
    gsv03 TYPE menge_d,
    gsv04 TYPE menge_d,
    gsv05 TYPE menge_d,
    gsv06 TYPE menge_d,
    gsv07 TYPE menge_d,
    gsv08 TYPE menge_d,
    gsv09 TYPE menge_d,
    gsv10 TYPE menge_d,
    gsv11 TYPE menge_d,
    gsv12 TYPE menge_d,
  END OF ty_mver_raw,

  " Für KDE-Abfrage
  BEGIN OF ty_kde_raw,
    matnr TYPE matnr,
    shkzg TYPE shkzg,
    menge TYPE menge_d,
  END OF ty_kde_raw,

  " Für MARD-Abfrage
  BEGIN OF ty_mard_raw,
    matnr TYPE matnr,
    labst TYPE labst,
  END OF ty_mard_raw,

  " Für EKPO/EKET-Abfrage
  BEGIN OF ty_ekpo_raw,
    matnr TYPE matnr,
    menge TYPE menge_d,
    wemng TYPE menge_d,
  END OF ty_ekpo_raw,

  " Für AFPO-Abfrage
  BEGIN OF ty_afpo_raw,
    matnr TYPE matnr,
    psmng TYPE menge_d,
    wemng TYPE menge_d,
  END OF ty_afpo_raw,

  " Für PLAF-Abfrage
  BEGIN OF ty_plaf_raw,
    matnr TYPE matnr,
    gsmng TYPE menge_d,
  END OF ty_plaf_raw,

  " Für RESB-Abfrage
  BEGIN OF ty_resb_raw,
    matnr TYPE matnr,
    bdmng TYPE menge_d,
    enmng TYPE menge_d,
  END OF ty_resb_raw,

  " Für BOM-Prüfung
  BEGIN OF ty_bom_raw,
    matnr TYPE matnr,
  END OF ty_bom_raw,

  " Für Exklusivität-Zählung
  BEGIN OF ty_count_raw,
    kompnr TYPE matnr,
    count  TYPE i,
  END OF ty_count_raw.

*&---------------------------------------------------------------------*
*& Tabellentypen
*&---------------------------------------------------------------------*
TYPES:
  tt_matnr_list TYPE STANDARD TABLE OF ty_matnr_line WITH DEFAULT KEY,
  tt_powerbi    TYPE STANDARD TABLE OF ty_powerbi WITH DEFAULT KEY,
  tt_vtab       TYPE SORTED TABLE OF ty_vtab WITH UNIQUE KEY matnr,
  tt_ktab       TYPE SORTED TABLE OF ty_ktab WITH UNIQUE KEY idnrk,


  tt_mtab       TYPE HASHED TABLE OF ty_mtab WITH UNIQUE KEY matnr idnrk,



  tt_exkl       TYPE HASHED TABLE OF ty_exkl WITH UNIQUE KEY kompnr,
  tt_mver       TYPE HASHED TABLE OF ty_mver WITH UNIQUE KEY matnr,
  tt_md04       TYPE HASHED TABLE OF ty_md04 WITH UNIQUE KEY matnr,
  tt_stamm      TYPE HASHED TABLE OF ty_stamm WITH UNIQUE KEY matnr,
  tt_stamm_raw  TYPE STANDARD TABLE OF ty_stamm_raw WITH DEFAULT KEY,
  tt_mver_raw   TYPE STANDARD TABLE OF ty_mver_raw WITH DEFAULT KEY,
  tt_kde_raw    TYPE STANDARD TABLE OF ty_kde_raw WITH DEFAULT KEY,
  tt_mard_raw   TYPE STANDARD TABLE OF ty_mard_raw WITH DEFAULT KEY,
  tt_ekpo_raw   TYPE STANDARD TABLE OF ty_ekpo_raw WITH DEFAULT KEY,
  tt_afpo_raw   TYPE STANDARD TABLE OF ty_afpo_raw WITH DEFAULT KEY,
  tt_plaf_raw   TYPE STANDARD TABLE OF ty_plaf_raw WITH DEFAULT KEY,
  tt_resb_raw   TYPE STANDARD TABLE OF ty_resb_raw WITH DEFAULT KEY,
  tt_bom_raw    TYPE STANDARD TABLE OF ty_bom_raw WITH DEFAULT KEY,
  tt_count_raw  TYPE STANDARD TABLE OF ty_count_raw WITH DEFAULT KEY.


*&---------------------------------------------------------------------*
*& Typen für "Weitere VKNRs" Funktion
*&---------------------------------------------------------------------*
TYPES:
  " Für weitere VKNRs pro Komponente
  BEGIN OF ty_weitere_vknr,
    kompnr TYPE matnr,       " Komponente
    vknr   TYPE matnr,       " Weitere VKNR
    verbr  TYPE menge_d,     " Verbrauch der VKNR (für Sortierung)
  END OF ty_weitere_vknr.


*&---------------------------------------------------------------------*
*& Globale Daten
*&---------------------------------------------------------------------*
DATA:
  " Haupttabellen
  gt_vtab        TYPE tt_vtab,
  gt_ktab        TYPE tt_ktab,
  gt_mtab        TYPE tt_mtab,
tt_weitere_vknr TYPE STANDARD TABLE OF ty_weitere_vknr WITH DEFAULT KEY,
        gt_weitere_vknr TYPE STANDARD TABLE OF ty_weitere_vknr WITH DEFAULT KEY,
  gv_max_weitere  TYPE i,  " Maximale Anzahl weiterer VKNRs (für Spaltenanzahl)
  gt_eltern_cache TYPE HASHED TABLE OF ty_mtab WITH UNIQUE KEY matnr idnrk,

  " Cache-Tabellen
  gt_stamm_cache TYPE tt_stamm,
  gt_exkl_cache  TYPE tt_exkl,
  gt_md04_cache  TYPE tt_md04,
  gt_verbr_cache TYPE tt_mver,

  " Excel-Ausgabe
  gt_excel       TYPE TABLE OF string,

  " Hilfsvariablen
  gv_datum_von   TYPE datum,
  gv_datum_bis   TYPE datum,
  gv_sp_von      TYPE char3,
  gv_sp_bis      TYPE char3,
  gv_sp_sum      TYPE char3,
  gv_sp_vbr      TYPE char3,
  gv_line        TYPE i,
  gv_tab         TYPE c LENGTH 1.

DATA: gt_md04_full_cache TYPE tt_md04_full.

*** FIX 19: Stoff-Compliance je Material (Wunsch Florian Waechter 2026-10-20). Bisher gab ZLO03
*** die Merkmale der Klasse Z_PACK_REGULATION gar nicht aus. Nachtrag 2026-10-22: RoHS, seit es
*** das Merkmal ROHS gibt (Adil, 21.10.).
TYPES: BEGIN OF ty_compl,
         matnr TYPE matnr,
         rohs  TYPE atwrt,
         reach TYPE atwrt,
         svhc  TYPE atwrt,
         pfas  TYPE string,
       END OF ty_compl,
       tt_compl TYPE HASHED TABLE OF ty_compl WITH UNIQUE KEY matnr.
DATA: gt_compl_cache TYPE tt_compl.

*&---------------------------------------------------------------------*
*& Selektionsbild
*&---------------------------------------------------------------------*
TABLES: mara.

SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE TEXT-s01.
  SELECT-OPTIONS: s_typcd FOR mara-zztyp_f4,
                  s_matnr FOR mara-matnr.
SELECTION-SCREEN END OF BLOCK b1.

SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE TEXT-s02.
  PARAMETERS: p_topd RADIOBUTTON GROUP g01 DEFAULT 'X',
              p_botu RADIOBUTTON GROUP g01.
SELECTION-SCREEN END OF BLOCK b2.

SELECTION-SCREEN BEGIN OF BLOCK b3 WITH FRAME TITLE TEXT-s03.
  PARAMETERS: p_days TYPE i DEFAULT 360.
  PARAMETERS: p_wvknr AS CHECKBOX DEFAULT 'X'.  " Weitere VKNRs anzeigen
  PARAMETERS: p_trans AS CHECKBOX DEFAULT ''.
  PARAMETERS: p_txtpo AS CHECKBOX DEFAULT ''.    " Textpositionen sichtbar
  PARAMETERS: p_lvorm AS CHECKBOX DEFAULT ''.
SELECTION-SCREEN END OF BLOCK b3.





*&---------------------------------------------------------------------*
*& Initialisierung
*&---------------------------------------------------------------------*
INITIALIZATION.
  gv_tab = cl_abap_char_utilities=>horizontal_tab.

*&---------------------------------------------------------------------*
*& AT SELECTION-SCREEN
*&---------------------------------------------------------------------*
AT SELECTION-SCREEN.
  LOOP AT s_typcd.
    CONDENSE s_typcd-low NO-GAPS.
    CONDENSE s_typcd-high NO-GAPS.

    " Wildcard-Handling: * oder + → CP-Option
    IF s_typcd-low CA '*+' OR s_typcd-high CA '*+'.
      s_typcd-option = 'CP'.
    ENDIF.

    MODIFY s_typcd.
  ENDLOOP.


*&---------------------------------------------------------------------*
*& START-OF-SELECTION
*&---------------------------------------------------------------------*
START-OF-SELECTION.

  gv_datum_bis = sy-datum.
  gv_datum_von = sy-datum - p_days.
  gv_datum_von+6(2) = '01'.

  PERFORM process_main.

  IF gt_ktab IS INITIAL.
    MESSAGE 'Keine Daten gefunden' TYPE 'S' DISPLAY LIKE 'W'.
    RETURN.
  ENDIF.


  " NEU: Elternmaterial-Cache laden (einmalig für Performance)
  PERFORM load_elternmaterial_cache.

  PERFORM load_compliance_cache.  " FIX 19




  " NEU: Weitere VKNRs ermitteln (vor Excel-Ausgabe)
  PERFORM ermittle_weitere_vknr.

  PERFORM create_excel_output.



  " NEU: Weitere VKNRs an Excel anhängen
  PERFORM ausgabe_weitere_vknr.


if p_trans = 'X'.
   " NEU: Transponierte Spalten-Details am Ende ausgeben
   PERFORM ausgabe_spalten_details.
endif.


  "PERFORM ausgabe_elternmaterial.

  IF sy-batch = 'X'.
    PERFORM download_background.
  ELSE.
    PERFORM download_dialog.
  ENDIF.






*&---------------------------------------------------------------------*
*& FORM process_main - Angepasst für ZMD04_CALC
*&---------------------------------------------------------------------*
FORM process_main.

  DATA: lt_powerbi    TYPE tt_powerbi,
        lt_matnr_sel  TYPE tt_matnr_list,
        lt_all_matnr  TYPE tt_matnr_list,
        lt_mara_sel   TYPE STANDARD TABLE OF matnr,
        ls_matnr_line TYPE ty_matnr_line,
        ls_vtab       TYPE ty_vtab,
        ls_ktab       TYPE ty_ktab,
        ls_mtab       TYPE ty_mtab,
        ls_mtab_exist TYPE ty_mtab,
        ls_stamm_v    TYPE ty_stamm,
        ls_md04_full  TYPE ty_md04_full,
        ls_powerbi    TYPE ty_powerbi,
        lv_menge      TYPE menge_d,
        lv_matnr      TYPE matnr.

  DATA: lt_matnr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

*  " ===================================================================
*  " SCHRITT 1: Materialnummern aus Selektion
*  " ===================================================================
*  " ===================================================================
*  " SCHRITT 1: Materialnummern aus Selektion
*  " ===================================================================
*  IF s_typcd IS NOT INITIAL.
*    "*** Top-Down: Nur FERT (VKNRs), Bottom-Up: Alle ***
*    IF p_topd = 'X'.
*      SELECT matnr FROM mara
*        INTO TABLE lt_mara_sel
*        WHERE zztyp_f4 IN s_typcd
*          AND matnr IN s_matnr
*          AND lvorm = ''
*         ." AND mtart = 'FERT'.  " disabled 2.3.26
*    ELSE.
*      SELECT matnr FROM mara
*        INTO TABLE lt_mara_sel
*        WHERE zztyp_f4 IN s_typcd
*          AND matnr IN s_matnr
*          AND lvorm = ''.
*    ENDIF.
*  ELSEIF s_matnr IS NOT INITIAL.
*    "*** Top-Down: Nur FERT (VKNRs), Bottom-Up: Alle ***
*    IF p_topd = 'X'.
*      SELECT matnr FROM mara
*        INTO TABLE lt_mara_sel
*        WHERE matnr IN s_matnr
*          AND lvorm = ''
*       ."   AND mtart = 'FERT'.  " disabled 2.3.26
*    ELSE.
*      SELECT matnr FROM mara
*        INTO TABLE lt_mara_sel
*        WHERE matnr IN s_matnr
*          AND lvorm = ''.
*    ENDIF.
*  ELSE.
*    MESSAGE 'Bitte Selektion eingeben' TYPE 'S' DISPLAY LIKE 'W'.
*    RETURN.
*  ENDIF.
*

" ===================================================================
  " SCHRITT 1: Materialnummern aus Selektion
  " ===================================================================

  " --- LVORM-Filter aufbauen (gesteuert über p_lvorm) ---
  " p_lvorm = ' ' : nur nicht-löschvorgemerkte (Range = leer-EQ)
  " p_lvorm = 'X' : alle inkl. löschvorgemerkte (Range bleibt leer => IN trifft alles)
  DATA: lr_lvorm TYPE RANGE OF mara-lvorm,
        ls_lvorm LIKE LINE OF lr_lvorm.

  IF p_lvorm = ' '.
    ls_lvorm-sign   = 'I'.
    ls_lvorm-option = 'EQ'.
    ls_lvorm-low    = ''.
    APPEND ls_lvorm TO lr_lvorm.
  ENDIF.

  IF s_typcd IS NOT INITIAL.
    "*** Top-Down: Nur FERT (VKNRs), Bottom-Up: Alle ***
    IF p_topd = 'X'.
      SELECT matnr FROM mara
        INTO TABLE lt_mara_sel
        WHERE zztyp_f4 IN s_typcd
          AND matnr IN s_matnr
          AND lvorm IN lr_lvorm
         ." AND mtart = 'FERT'.  " disabled 2.3.26
    ELSE.
      SELECT matnr FROM mara
        INTO TABLE lt_mara_sel
        WHERE zztyp_f4 IN s_typcd
          AND matnr IN s_matnr
          AND lvorm IN lr_lvorm.
    ENDIF.
  ELSEIF s_matnr IS NOT INITIAL.
    "*** Top-Down: Nur FERT (VKNRs), Bottom-Up: Alle ***
    IF p_topd = 'X'.
      SELECT matnr FROM mara
        INTO TABLE lt_mara_sel
        WHERE matnr IN s_matnr
          AND lvorm IN lr_lvorm
       ."   AND mtart = 'FERT'.  " disabled 2.3.26
    ELSE.
      SELECT matnr FROM mara
        INTO TABLE lt_mara_sel
        WHERE matnr IN s_matnr
          AND lvorm IN lr_lvorm.
    ENDIF.
  ELSE.
    MESSAGE 'Bitte Selektion eingeben' TYPE 'S' DISPLAY LIKE 'W'.
    RETURN.
  ENDIF.



















  LOOP AT lt_mara_sel INTO lv_matnr.
    ls_matnr_line-matnr = lv_matnr.
    APPEND ls_matnr_line TO lt_matnr_sel.
  ENDLOOP.

  IF lt_matnr_sel IS INITIAL.
    RETURN.
  ENDIF.
  " ===================================================================
  " SCHRITT 2: zpowerbi_vc_txt lesen
  " ===================================================================
  IF p_topd = 'X'.
    SELECT id stufe kompnr menge mengeneinheit baugruppe matnr kom_mstae postyp
      FROM zpowerbi_vc_txt
      INTO TABLE lt_powerbi
      FOR ALL ENTRIES IN lt_matnr_sel
      WHERE matnr = lt_matnr_sel-matnr
        AND menge <> ''
        AND mengeneinheit <> ''.
  ELSE.
    SELECT id stufe kompnr menge mengeneinheit baugruppe matnr kom_mstae postyp
      FROM zpowerbi_vc_txt
      INTO TABLE lt_powerbi
      FOR ALL ENTRIES IN lt_matnr_sel
      WHERE kompnr = lt_matnr_sel-matnr
        AND menge <> ''
        AND mengeneinheit <> ''.
  ENDIF.

  IF lt_powerbi IS INITIAL.
    RETURN.
  ENDIF.

  "=========================================================
  " FIX 2: DELETE ADJACENT DUPLICATES ENTFERNT
  " Die Duplikat-Entfernung auf matnr/kompnr hat legitime
  " Mehrfachverwendungen (gleiche Komponente auf verschiedenen
  " Pfaden in derselben VKNR) gelöscht.
  " COLLECT auf gt_mtab summiert die Mengen korrekt.
  "=========================================================
  " ALT (ENTFERNT):
  " SORT lt_powerbi BY matnr kompnr.
  " DELETE ADJACENT DUPLICATES FROM lt_powerbi COMPARING matnr kompnr.
  "=========================================================








  " ===================================================================
  " SCHRITT 3: Alle Materialnummern sammeln
  " ===================================================================
  LOOP AT lt_powerbi INTO ls_powerbi.
    INSERT ls_powerbi-matnr INTO TABLE lt_matnr_temp.
    INSERT ls_powerbi-kompnr INTO TABLE lt_matnr_temp.
  ENDLOOP.

  LOOP AT lt_matnr_sel INTO ls_matnr_line.
    INSERT ls_matnr_line-matnr INTO TABLE lt_matnr_temp.
  ENDLOOP.

  LOOP AT lt_matnr_temp INTO lv_matnr.
    ls_matnr_line-matnr = lv_matnr.
    APPEND ls_matnr_line TO lt_all_matnr.
  ENDLOOP.

  " ===================================================================
  " SCHRITT 4: Stammdaten-Cache laden
  " ===================================================================
  PERFORM load_stammdaten_cache USING lt_all_matnr.

  " ===================================================================
  " SCHRITT 5: ZMD04_CALC laden (enthält ALLES: MD04, Verbr, OMENG, MK)
  " ===================================================================
  PERFORM load_md04_bulk USING lt_all_matnr.

  " ===================================================================
  " SCHRITT 6: Verbrauchs-Cache aus ZMD04_CALC befüllen
  " ===================================================================
  PERFORM load_verbrauch_bulk USING lt_all_matnr.

  " ===================================================================
  " SCHRITT 7: Exklusivität aus ZMD04_CALC laden
  " ===================================================================
  IF p_topd = 'X'.
    PERFORM load_exclusivity_topdown USING lt_powerbi.
  ELSE.
    " Bottom-Up: Globale Zählung (bleibt wie bisher)
    PERFORM load_global_exclusivity USING lt_powerbi.
  ENDIF.

  " ===================================================================
  " SCHRITT 8: Haupttabellen befüllen
  " ===================================================================
  LOOP AT lt_powerbi INTO ls_powerbi.






    " NEU:
    DATA: lv_meins_komp TYPE meins.
    DATA: ls_ktab_fix12 TYPE ty_ktab.  " FIX 12
    " Hole Mengeneinheit der Komponente aus Cache
    READ TABLE gt_stamm_cache INTO ls_stamm_v
      WITH TABLE KEY matnr = ls_powerbi-kompnr.
    IF sy-subrc = 0.
      lv_meins_komp = ls_stamm_v-meins.
    ELSE.
      lv_meins_komp = ls_powerbi-mengeneinheit.
    ENDIF.

    PERFORM convert_menge
      USING ls_powerbi-menge
            ls_powerbi-mengeneinheit
            lv_meins_komp
      CHANGING lv_menge.









    IF p_topd = 'X'.
      " ----- TOP-DOWN -----

      " VTAB: Kopfmaterialien (Spalten)
      READ TABLE gt_vtab WITH TABLE KEY matnr = ls_powerbi-matnr
        TRANSPORTING NO FIELDS.
      IF sy-subrc <> 0.
        CLEAR ls_vtab.
        ls_vtab-matnr = ls_powerbi-matnr.
        READ TABLE gt_stamm_cache INTO ls_stamm_v
          WITH TABLE KEY matnr = ls_powerbi-matnr.
        IF sy-subrc = 0.
          ls_vtab-meins = ls_stamm_v-meins.
          ls_vtab-mstae = ls_stamm_v-mstae.
        ENDIF.
        " Verbrauch des Kopfmaterials aus Cache
        READ TABLE gt_md04_full_cache INTO ls_md04_full
          WITH TABLE KEY matnr = ls_powerbi-matnr.
        IF sy-subrc = 0.
          ls_vtab-verbr = ls_md04_full-verbr.
        ENDIF.
        INSERT ls_vtab INTO TABLE gt_vtab.
      ENDIF.

      " KTAB: Komponenten (Zeilen)
      " FIX 12 (2026-10-16): War der erste Treffer einer Komponente eine Textposition, blieben
      " alle Kennzahlen (MK, Bestand, Kosten) 0, auch wenn spaeter eine echte Position kam.
      " Fall B63485 in 46123/46124/46126/46128 (je eine L- und eine T-Position).
      READ TABLE gt_ktab INTO ls_ktab_fix12 WITH TABLE KEY idnrk = ls_powerbi-kompnr.
      IF sy-subrc <> 0.
        PERFORM fill_ktab USING ls_powerbi-kompnr ls_powerbi-matnr
                                ls_powerbi-baugruppe ls_powerbi-postyp CHANGING ls_ktab.
        INSERT ls_ktab INTO TABLE gt_ktab.
      ELSEIF ls_ktab_fix12-postyp = 'T' AND ls_powerbi-postyp <> 'T'.
        PERFORM fill_ktab USING ls_powerbi-kompnr ls_powerbi-matnr
                                ls_powerbi-baugruppe ls_powerbi-postyp CHANGING ls_ktab.
        MODIFY TABLE gt_ktab FROM ls_ktab.
      ENDIF.






      " MTAB: Zuordnung - NUR EINMAL pro matnr/idnrk (wie COLLECT im alten Programm)
      " Textpositionen (Typ T) werden NICHT in die Bedarfsberechnung aufgenommen
        IF ls_powerbi-postyp <> 'T' OR p_txtpo = 'X'. " IF ls_powerbi-postyp <> 'T'.
        ls_mtab-matnr = ls_powerbi-matnr.
        ls_mtab-idnrk = ls_powerbi-kompnr.
        READ TABLE gt_mtab WITH TABLE KEY matnr = ls_mtab-matnr
                                          idnrk = ls_mtab-idnrk
          TRANSPORTING NO FIELDS.
        ls_mtab-menge = lv_menge.




        COLLECT ls_mtab INTO gt_mtab.
      ENDIF.

    ELSE.
      " ----- BOTTOM-UP -----

" *** FILTER: Löschvormerkung prüfen ***
      READ TABLE gt_stamm_cache INTO ls_stamm_v
        WITH TABLE KEY matnr = ls_powerbi-matnr.
      IF sy-subrc <> 0.
        IF ls_powerbi-postyp <> 'T' OR p_txtpo <> 'X'.
          CONTINUE.  " Material nicht in Stammdaten (Textpos. durchlassen wenn p_txtpo)
        ENDIF.
      ENDIF.
       IF ls_stamm_v-lvorm IS NOT INITIAL AND p_lvorm = ' '. "IF ls_stamm_v-lvorm IS NOT INITIAL.
        CONTINUE.  " Material hat Löschvormerkung
      ENDIF.

      " VTAB: Komponenten (Spalten)
      READ TABLE gt_vtab WITH TABLE KEY matnr = ls_powerbi-kompnr
        TRANSPORTING NO FIELDS.







      IF sy-subrc <> 0.
        CLEAR ls_vtab.
        ls_vtab-matnr = ls_powerbi-kompnr.
        READ TABLE gt_stamm_cache INTO ls_stamm_v
          WITH TABLE KEY matnr = ls_powerbi-kompnr.
        IF sy-subrc = 0.
          ls_vtab-meins = ls_stamm_v-meins.
          ls_vtab-mstae = ls_stamm_v-mstae.
        ENDIF.
        INSERT ls_vtab INTO TABLE gt_vtab.
      ENDIF.

      " KTAB: Kopfmaterialien wo verbaut (Zeilen)
      " FIX 12 (2026-10-16): wie im Top-Down, echte Position ersetzt einen Eintrag aus einer Textposition
      READ TABLE gt_ktab INTO ls_ktab_fix12 WITH TABLE KEY idnrk = ls_powerbi-matnr.
      IF sy-subrc <> 0.
        PERFORM fill_ktab USING ls_powerbi-matnr ls_powerbi-kompnr '' ls_powerbi-postyp
          CHANGING ls_ktab.
        INSERT ls_ktab INTO TABLE gt_ktab.
      ELSEIF ls_ktab_fix12-postyp = 'T' AND ls_powerbi-postyp <> 'T'.
        PERFORM fill_ktab USING ls_powerbi-matnr ls_powerbi-kompnr '' ls_powerbi-postyp
          CHANGING ls_ktab.
        MODIFY TABLE gt_ktab FROM ls_ktab.
      ENDIF.

      " MTAB: Zuordnung
      ls_mtab-matnr = ls_powerbi-kompnr.
      ls_mtab-idnrk = ls_powerbi-matnr.
      READ TABLE gt_mtab WITH TABLE KEY matnr = ls_mtab-matnr
                                        idnrk = ls_mtab-idnrk
        INTO ls_mtab_exist.
      IF sy-subrc = 0.
        ls_mtab-menge = ls_mtab_exist-menge + lv_menge.
        MODIFY TABLE gt_mtab FROM ls_mtab.
      ELSE.
        ls_mtab-menge = lv_menge.
        INSERT ls_mtab INTO TABLE gt_mtab.
      ENDIF.

    ENDIF.

  ENDLOOP.

  "=========================================================
  " FIX 4: DELETE gt_ktab WHERE maktx IS INITIAL
  " Gesteuert über p_txtpo:
  "   p_txtpo = ' ' → DELETE aktiv (altes Verhalten, Textpos. raus)
  "   p_txtpo = 'X' → DELETE inaktiv (Textpos. sichtbar)
  "=========================================================
  IF p_txtpo = ' '.
    DELETE gt_ktab WHERE maktx IS INITIAL.
  ENDIF.
  "=========================================================


  " ===================================================================
  " SCHRITT 9: Sortierung der VTAB
  " ===================================================================
  DATA: lt_vtab_sorted TYPE STANDARD TABLE OF ty_vtab,
        ls_vtab_sort   TYPE ty_vtab.

  LOOP AT gt_vtab INTO ls_vtab_sort.
    APPEND ls_vtab_sort TO lt_vtab_sorted.
  ENDLOOP.

  SORT lt_vtab_sorted BY matnr.

  CLEAR gt_vtab.
  LOOP AT lt_vtab_sorted INTO ls_vtab_sort.
    INSERT ls_vtab_sort INTO TABLE gt_vtab.
  ENDLOOP.

  FREE lt_vtab_sorted.

ENDFORM.




















*&---------------------------------------------------------------------*
*& FORM load_stammdaten_cache
*&---------------------------------------------------------------------*
FORM load_stammdaten_cache USING pt_matnr TYPE tt_matnr_list.

  DATA: lt_stamm_raw TYPE tt_stamm_raw,
        lt_bom_raw   TYPE tt_bom_raw,
        ls_stamm_raw TYPE ty_stamm_raw,
        ls_bom_raw   TYPE ty_bom_raw,
        ls_stamm     TYPE ty_stamm.

  FIELD-SYMBOLS: <fs_stamm> TYPE ty_stamm.

  CHECK pt_matnr IS NOT INITIAL.

  " Großer JOIN für alle Stammdaten
  SELECT
    m~matnr
    t~maktx
    m~meins
    m~mstae
    m~mstav
    m~lvorm
    m~disst
    m~zzlzcod
    m~zzlzcodsort
    c~dismm
    c~minbe
    c~disls
    c~bstfe
    c~eisbe
    c~beskz
    b~lbkum
    b~verpr
    b~stprs
    b~peinh
    b~vprsv
    b~salk3
    c~dispo      " FIX 22: Disponent und Warengruppe fuer das Top-Down (Wunsch Armin 2026-10-29)
    m~matkl
  FROM mara AS m
  LEFT JOIN makt AS t ON t~matnr = m~matnr AND t~spras = sy-langu
  LEFT JOIN marc AS c ON c~matnr = m~matnr AND c~werks = gc_werks
  LEFT JOIN mbew AS b ON b~matnr = m~matnr AND b~bwkey = gc_werks AND b~bwtar = ''
  INTO TABLE lt_stamm_raw
  FOR ALL ENTRIES IN pt_matnr
  WHERE m~matnr = pt_matnr-matnr
   ." AND m~lvorm = ''.

  " In Cache übertragen
  LOOP AT lt_stamm_raw INTO ls_stamm_raw.
    CLEAR ls_stamm.
    ls_stamm-matnr       = ls_stamm_raw-matnr.
    ls_stamm-maktx       = ls_stamm_raw-maktx.
    ls_stamm-meins       = ls_stamm_raw-meins.
    ls_stamm-mstae       = ls_stamm_raw-mstae.
    ls_stamm-mstav       = ls_stamm_raw-mstav.
    ls_stamm-disst       = ls_stamm_raw-disst.
    ls_stamm-zzlzcod     = ls_stamm_raw-zzlzcod.
    ls_stamm-zzlzcodsort = ls_stamm_raw-zzlzcodsort.
    ls_stamm-dismm       = ls_stamm_raw-dismm.
    ls_stamm-lvorm       = ls_stamm_raw-lvorm.
    ls_stamm-minbe       = ls_stamm_raw-minbe.
    ls_stamm-disls       = ls_stamm_raw-disls.
    ls_stamm-bstfe       = ls_stamm_raw-bstfe.
    ls_stamm-eisbe       = ls_stamm_raw-eisbe.
    ls_stamm-beskz       = ls_stamm_raw-beskz.
    ls_stamm-lbkum       = ls_stamm_raw-lbkum.
    ls_stamm-verpr       = ls_stamm_raw-verpr.
    ls_stamm-stprs       = ls_stamm_raw-stprs.
    ls_stamm-peinh       = ls_stamm_raw-peinh.
    ls_stamm-vprsv       = ls_stamm_raw-vprsv.
    ls_stamm-salk3       = ls_stamm_raw-salk3.
    ls_stamm-dispo       = ls_stamm_raw-dispo.   " FIX 22
    ls_stamm-matkl       = ls_stamm_raw-matkl.   " FIX 22
    IF ls_stamm-peinh IS INITIAL.
      ls_stamm-peinh = 1.
    ENDIF.
    INSERT ls_stamm INTO TABLE gt_stamm_cache.
  ENDLOOP.

  " Baugruppen-Flag ermitteln
  SELECT matnr FROM mast
    INTO TABLE lt_bom_raw
    FOR ALL ENTRIES IN pt_matnr
    WHERE matnr = pt_matnr-matnr
      AND werks = gc_werks
      AND stlan = '1'.

  LOOP AT lt_bom_raw INTO ls_bom_raw.
    READ TABLE gt_stamm_cache ASSIGNING <fs_stamm>
      WITH TABLE KEY matnr = ls_bom_raw-matnr.
    IF sy-subrc = 0.
      <fs_stamm>-has_bom = 'X'.
    ENDIF.
  ENDLOOP.

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM load_verbrauch_bulk - liest VERBR aus ZMD04_CALC Cache
*&---------------------------------------------------------------------*
FORM load_verbrauch_bulk USING pt_matnr TYPE tt_matnr_list.

  DATA: ls_verbr     TYPE ty_mver,
        ls_md04_full TYPE ty_md04_full.

  " Cache leeren
  CLEAR gt_verbr_cache.

  " Verbrauch aus dem bereits geladenen gt_md04_full_cache übernehmen
  LOOP AT gt_md04_full_cache INTO ls_md04_full.
    ls_verbr-matnr = ls_md04_full-matnr.
    ls_verbr-gsv   = ls_md04_full-verbr.
    INSERT ls_verbr INTO TABLE gt_verbr_cache.
  ENDLOOP.

ENDFORM.
























































































































































*&---------------------------------------------------------------------*
*& FORM load_md04_bulk - liest aus vorberechneter ZMD04_CALC
*&---------------------------------------------------------------------*
FORM load_md04_bulk USING pt_matnr TYPE tt_matnr_list.

  DATA: lt_md04_db   TYPE STANDARD TABLE OF zmd04_calc,
        ls_md04_db   TYPE zmd04_calc,
        ls_md04      TYPE ty_md04,
        ls_md04_full TYPE ty_md04_full,
        ls_line      TYPE ty_matnr_line.

  " Cache leeren
  CLEAR: gt_md04_cache, gt_md04_full_cache.

  CHECK pt_matnr IS NOT INITIAL.

  " Daten aus vorberechneter Tabelle lesen
  SELECT matnr werks labst feste_zug gepl_zug feste_abg gepl_abg
         verbr omeng owert mkmng omkwr exklusiv exkl_count
    FROM zmd04_calc
    INTO CORRESPONDING FIELDS OF TABLE lt_md04_db
    FOR ALL ENTRIES IN pt_matnr
    WHERE matnr = pt_matnr-matnr
      AND werks = gc_werks.

  " In Cache übernehmen
  LOOP AT lt_md04_db INTO ls_md04_db.

    " Standard MD04-Cache (für Kompatibilität)
    CLEAR ls_md04.
    ls_md04-matnr     = ls_md04_db-matnr.
    ls_md04-labst     = ls_md04_db-labst.
    ls_md04-feste_zug = ls_md04_db-feste_zug.
    ls_md04-gepl_zug  = ls_md04_db-gepl_zug.
    ls_md04-feste_abg = ls_md04_db-feste_abg.
    ls_md04-gepl_abg  = ls_md04_db-gepl_abg.
    INSERT ls_md04 INTO TABLE gt_md04_cache.

    " Erweiterter Cache (mit allen Feldern)
    CLEAR ls_md04_full.
    MOVE-CORRESPONDING ls_md04_db TO ls_md04_full.
    INSERT ls_md04_full INTO TABLE gt_md04_full_cache.

  ENDLOOP.

ENDFORM.







*&---------------------------------------------------------------------*
*& FORM load_exclusivity_topdown - Schritte 1-5 Exklusivitätsprüfung






*&---------------------------------------------------------------------*
FORM load_exclusivity_topdown USING pt_powerbi TYPE tt_powerbi.

  TYPES: BEGIN OF ty_vknr_usage,
           kompnr TYPE matnr,
           vknr   TYPE matnr,
         END OF ty_vknr_usage.


  TYPES: BEGIN OF ty_vknr_mara,
           matnr TYPE matnr,
           mtart TYPE mtart,
           mstae TYPE mstae,
         END OF ty_vknr_mara.

  DATA: ls_exkl        TYPE ty_exkl,
        ls_powerbi     TYPE ty_powerbi,
        lv_kompnr      TYPE matnr,
        lv_is_exklusiv TYPE c LENGTH 1,
        lv_vknr        TYPE matnr.

  DATA: lt_kompnr_temp    TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
        lt_sel_vknr       TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
        lt_all_usage      TYPE STANDARD TABLE OF ty_vknr_usage,

        lt_all_usage_fert TYPE STANDARD TABLE OF ty_vknr_usage,
        lt_all_vknr       TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,

        lt_vknr_mara      TYPE HASHED TABLE OF ty_vknr_mara WITH UNIQUE KEY matnr,
        lt_kompnr_list    TYPE tt_matnr_list,
        ls_matnr_line     TYPE ty_matnr_line,
        ls_usage          TYPE ty_vknr_usage,

        ls_vknr_mara      TYPE ty_vknr_mara.




  LOOP AT pt_powerbi INTO ls_powerbi.
    INSERT ls_powerbi-kompnr INTO TABLE lt_kompnr_temp.
  ENDLOOP.

  IF lt_kompnr_temp IS INITIAL.
    RETURN.
  ENDIF.


  LOOP AT pt_powerbi INTO ls_powerbi.
    INSERT ls_powerbi-matnr INTO TABLE lt_sel_vknr.
  ENDLOOP.


  LOOP AT lt_kompnr_temp INTO lv_kompnr.
    ls_matnr_line-matnr = lv_kompnr.
    APPEND ls_matnr_line TO lt_kompnr_list.
  ENDLOOP.




  SELECT kompnr, matnr AS vknr
    FROM zpowerbi_vc_txt
    INTO TABLE @lt_all_usage
    FOR ALL ENTRIES IN @lt_kompnr_list
    WHERE kompnr = @lt_kompnr_list-matnr.


  SORT lt_all_usage BY kompnr vknr.
  DELETE ADJACENT DUPLICATES FROM lt_all_usage COMPARING kompnr vknr.





  LOOP AT lt_all_usage INTO ls_usage.
    INSERT ls_usage-vknr INTO TABLE lt_all_vknr.
  ENDLOOP.


  IF lt_all_vknr IS NOT INITIAL.
    DATA: lt_vknr_list TYPE tt_matnr_list.
    LOOP AT lt_all_vknr INTO lv_vknr.
      ls_matnr_line-matnr = lv_vknr.
      APPEND ls_matnr_line TO lt_vknr_list.
    ENDLOOP.


    SELECT matnr, mtart, mstae
      FROM mara
      INTO TABLE @lt_vknr_mara
      FOR ALL ENTRIES IN @lt_vknr_list
      WHERE matnr = @lt_vknr_list-matnr.
  ENDIF.




  LOOP AT lt_all_usage INTO ls_usage.
    READ TABLE lt_vknr_mara INTO ls_vknr_mara
      WITH TABLE KEY matnr = ls_usage-vknr.
    IF sy-subrc = 0.


      IF ls_vknr_mara-mtart = 'FERT'.
        APPEND ls_usage TO lt_all_usage_fert.
      ENDIF.
    ENDIF.
  ENDLOOP.




  LOOP AT lt_kompnr_temp INTO lv_kompnr.
    lv_is_exklusiv = 'X'.


    LOOP AT lt_all_usage_fert INTO ls_usage WHERE kompnr = lv_kompnr.


      READ TABLE lt_sel_vknr WITH TABLE KEY table_line = ls_usage-vknr
        TRANSPORTING NO FIELDS.

      IF sy-subrc <> 0.

        READ TABLE lt_vknr_mara INTO ls_vknr_mara
          WITH TABLE KEY matnr = ls_usage-vknr.

        IF sy-subrc = 0 AND ls_vknr_mara-mstae = '99'.

          CONTINUE.
        ELSE.

          lv_is_exklusiv = ''.
          EXIT.
        ENDIF.
      ENDIF.


    ENDLOOP.


    ls_exkl-kompnr   = lv_kompnr.
    ls_exkl-exklusiv = lv_is_exklusiv.
    ls_exkl-count    = 0.
    INSERT ls_exkl INTO TABLE gt_exkl_cache.

  ENDLOOP.

ENDFORM.












*&---------------------------------------------------------------------*
*& FORM load_global_exclusivity
*&---------------------------------------------------------------------*
FORM load_global_exclusivity USING pt_powerbi TYPE tt_powerbi.

  DATA: lt_kompnr    TYPE tt_matnr_list,
        lt_count_raw TYPE tt_count_raw,
        ls_kompnr    TYPE ty_matnr_line,
        ls_count_raw TYPE ty_count_raw,
        ls_exkl      TYPE ty_exkl,
        ls_powerbi   TYPE ty_powerbi,
        lv_kompnr    TYPE matnr.

  DATA: lt_kompnr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.


  LOOP AT pt_powerbi INTO ls_powerbi.
    INSERT ls_powerbi-kompnr INTO TABLE lt_kompnr_temp.
  ENDLOOP.

  LOOP AT lt_kompnr_temp INTO lv_kompnr.
    ls_kompnr-matnr = lv_kompnr.
    APPEND ls_kompnr TO lt_kompnr.
  ENDLOOP.

  CHECK lt_kompnr IS NOT INITIAL.



  DATA: lt_zpowerbi TYPE STANDARD TABLE OF ty_powerbi,
        ls_zpowerbi TYPE ty_powerbi.

  SELECT kompnr matnr
    FROM zpowerbi_vc_txt
    INTO CORRESPONDING FIELDS OF TABLE lt_zpowerbi
    FOR ALL ENTRIES IN lt_kompnr
    WHERE kompnr = lt_kompnr-matnr.


  DATA: lt_count_temp       TYPE HASHED TABLE OF ty_count_raw WITH UNIQUE KEY kompnr,
        lt_matnr_per_kompnr TYPE HASHED TABLE OF ty_mtab WITH UNIQUE KEY matnr idnrk.

  LOOP AT lt_zpowerbi INTO ls_zpowerbi.

    DATA: ls_temp TYPE ty_mtab.
    ls_temp-matnr = ls_zpowerbi-kompnr.
    ls_temp-idnrk = ls_zpowerbi-matnr.
    INSERT ls_temp INTO TABLE lt_matnr_per_kompnr.
  ENDLOOP.


  LOOP AT lt_matnr_per_kompnr INTO ls_temp.
    READ TABLE lt_count_temp INTO ls_count_raw WITH TABLE KEY kompnr = ls_temp-matnr.
    IF sy-subrc = 0.
      ls_count_raw-count = ls_count_raw-count + 1.
      MODIFY TABLE lt_count_temp FROM ls_count_raw.
    ELSE.
      ls_count_raw-kompnr = ls_temp-matnr.
      ls_count_raw-count = 1.
      INSERT ls_count_raw INTO TABLE lt_count_temp.
    ENDIF.
  ENDLOOP.


  LOOP AT lt_count_temp INTO ls_count_raw.
    ls_exkl-kompnr = ls_count_raw-kompnr.
    ls_exkl-count = ls_count_raw-count.
    ls_exkl-exklusiv = ''.
    INSERT ls_exkl INTO TABLE gt_exkl_cache.
  ENDLOOP.

ENDFORM.



*&---------------------------------------------------------------------*
*& FORM fill_ktab
*& FIX 4: Fallback-Text aus zpowerbi_vc_txt für Textpositionen
*& postyp wird übernommen zur Kennzeichnung in der Ausgabe
*&---------------------------------------------------------------------*
FORM fill_ktab
  USING pv_idnrk TYPE matnr
        pv_matnr TYPE matnr
        pv_baugruppe TYPE any
        pv_postyp TYPE postp
  CHANGING ps_ktab TYPE ty_ktab.

  DATA: ls_stamm     TYPE ty_stamm,
        ls_md04_full TYPE ty_md04_full,
        lv_preis     TYPE p DECIMALS 2.

  CLEAR ps_ktab.
  ps_ktab-idnrk = pv_idnrk.
  ps_ktab-matnr = pv_matnr.
  ps_ktab-postyp = pv_postyp.

  " Textpositionen: NUR Bezeichnung, keine Werte/Stammdaten
  " Alles bleibt 0/leer — es geht nur um den Text
  IF pv_postyp = 'T'.
    IF p_txtpo = 'X'.
      SELECT SINGLE mat_txt FROM zpowerbi_vc_txt
        INTO @ps_ktab-maktx
        WHERE kompnr = @pv_idnrk
          AND mat_txt <> ''
          AND matnr = @pv_matnr.
      IF ps_ktab-maktx IS INITIAL.
        CALL FUNCTION 'CONVERSION_EXIT_ALPHA_OUTPUT'
          EXPORTING input  = pv_idnrk
          IMPORTING output = ps_ktab-maktx.
      ENDIF.
    ENDIF.
    RETURN.
  ENDIF.

  " Stammdaten aus Cache
  READ TABLE gt_stamm_cache INTO ls_stamm WITH TABLE KEY matnr = pv_idnrk.
  IF sy-subrc = 0.
    ps_ktab-maktx       = ls_stamm-maktx.
    ps_ktab-meins       = ls_stamm-meins.
    ps_ktab-disst       = ls_stamm-disst.
    ps_ktab-mstae       = ls_stamm-mstae.
    ps_ktab-mstav       = ls_stamm-mstav.
    ps_ktab-zzlzcod     = ls_stamm-zzlzcod.
    ps_ktab-zzlzcodsort = ls_stamm-zzlzcodsort.
    ps_ktab-dismm       = ls_stamm-dismm.
    ps_ktab-minbe       = ls_stamm-minbe.
    ps_ktab-disls       = ls_stamm-disls.
    ps_ktab-bstfe       = ls_stamm-bstfe.
    ps_ktab-eisbe       = ls_stamm-eisbe.
    ps_ktab-beskz       = ls_stamm-beskz.
    ps_ktab-lbkum       = ls_stamm-lbkum.
    ps_ktab-verpr       = ls_stamm-verpr.
    ps_ktab-stprs       = ls_stamm-stprs.
    ps_ktab-peinh       = ls_stamm-peinh.
    ps_ktab-vprsv       = ls_stamm-vprsv.
    ps_ktab-salk3       = ls_stamm-salk3.
    ps_ktab-dispo       = ls_stamm-dispo.   " FIX 22
    ps_ktab-matkl       = ls_stamm-matkl.   " FIX 22

    " Baugruppe
    IF pv_baugruppe = 'X' OR ls_stamm-has_bom = 'X'.
      IF ls_stamm-beskz <> 'F'.
        ps_ktab-baugr = 'X'.
      ENDIF.
    ENDIF.

    IF ps_ktab-peinh IS INITIAL.
      ps_ktab-peinh = 1.
    ENDIF.


    " Stückkosten berechnen (abhängig von VPRSV)

    IF ls_stamm-vprsv = 'V'.

      ps_ktab-stueckkosten = ls_stamm-verpr / ls_stamm-peinh.
    ELSE.

      ps_ktab-stueckkosten = ls_stamm-stprs / ls_stamm-peinh.
    ENDIF.

  ELSE.
    "=========================================================
    " FIX 4: Textposition oder Material nicht in MARA
    " Fallback-Text nur wenn p_txtpo = 'X' (Textpos. sichtbar)
    "=========================================================
    IF p_txtpo = 'X'.
      SELECT SINGLE mat_txt FROM zpowerbi_vc_txt
        INTO @ps_ktab-maktx
        WHERE kompnr = @pv_idnrk
          AND mat_txt <> ''
          AND matnr = @pv_matnr.
      IF ps_ktab-maktx IS INITIAL.
        " Letzter Fallback: Materialnummer als Text
        CALL FUNCTION 'CONVERSION_EXIT_ALPHA_OUTPUT'
          EXPORTING
            input  = pv_idnrk
          IMPORTING
            output = ps_ktab-maktx.
      ENDIF.
    ENDIF.
    "=========================================================
  ENDIF.

  " *** ALLE Werte aus ZMD04_CALC Cache ***
  READ TABLE gt_md04_full_cache INTO ls_md04_full
    WITH TABLE KEY matnr = pv_idnrk.

  IF sy-subrc = 0.
    " MD04-Felder
    ps_ktab-labst     = ls_md04_full-labst.
    ps_ktab-feste_zug = ls_md04_full-feste_zug.
    ps_ktab-gepl_zug  = ls_md04_full-gepl_zug.
    ps_ktab-feste_abg = ls_md04_full-feste_abg.
    ps_ktab-gepl_abg  = ls_md04_full-gepl_abg.

    " Verbrauch
    ps_ktab-verbr     = ls_md04_full-verbr.

    " OMENG/OWERT
    ps_ktab-omeng     = ls_md04_full-omeng.
    ps_ktab-owert     = ls_md04_full-owert.

    " MKMNG/OMKWR
    ps_ktab-mkmng     = ls_md04_full-mkmng.
    ps_ktab-omkwr     = ls_md04_full-omkwr.


    " Endbestand berechnen


    ps_ktab-endbestand = ps_ktab-labst
                       + ps_ktab-feste_zug
                       + ps_ktab-gepl_zug
                       - ps_ktab-feste_abg
                       - ps_ktab-gepl_abg.


    " Wert-Spalten berechnen

    ps_ktab-wert_feste_zug  = ps_ktab-feste_zug * ps_ktab-stueckkosten.
    ps_ktab-wert_gepl_zug   = ps_ktab-gepl_zug * ps_ktab-stueckkosten.
    ps_ktab-wert_feste_abg  = ps_ktab-feste_abg * ps_ktab-stueckkosten.
    ps_ktab-wert_gepl_abg   = ps_ktab-gepl_abg * ps_ktab-stueckkosten.
    ps_ktab-wert_endbestand = ps_ktab-endbestand * ps_ktab-stueckkosten.


  ENDIF.

ENDFORM.








*&---------------------------------------------------------------------*
*& FORM convert_menge
*& FIX 1: Rundung auf dec=0 entfernt
*& FIX 5: Leere Ziel-Einheit abgefangen

*&---------------------------------------------------------------------*
FORM convert_menge
  USING pv_str TYPE any
        pv_meins_in TYPE meins      " Mengeneinheit aus ZPOWERBI_VC
        pv_meins_out TYPE meins     " Mengeneinheit aus MARA (Komponente)
  CHANGING pv_num TYPE menge_d.

  DATA: lv_str       TYPE string,
        lv_menge_in  TYPE menge_d,
        lv_menge_out TYPE menge_d.

  lv_str = pv_str.
  CONDENSE lv_str NO-GAPS.

  IF lv_str IS INITIAL.
    pv_num = 0.
    RETURN.
  ENDIF.

  " String zu Zahl konvertieren
  TRY.
      lv_menge_in = lv_str.
    CATCH cx_sy_conversion_error.
      pv_num = 0.
      RETURN.
  ENDTRY.

  "=========================================================
  " FIX 5: Leere Ziel-Einheit abfangen (Textpositionen)
  " Wenn Ziel-Einheit leer ist, keine Konvertierung versuchen
  "=========================================================
  IF pv_meins_in <> pv_meins_out
     AND pv_meins_in IS NOT INITIAL
     AND pv_meins_out IS NOT INITIAL.
    CALL FUNCTION 'UNIT_CONVERSION_SIMPLE'
      EXPORTING
        input                = lv_menge_in
        unit_in              = pv_meins_in
        unit_out             = pv_meins_out
      IMPORTING
        output               = lv_menge_out
      EXCEPTIONS
        conversion_not_found = 1
        division_by_zero     = 2
        input_invalid        = 3
        output_invalid       = 4
        overflow             = 5
        type_invalid         = 6
        units_missing        = 7
        unit_in_not_found    = 8
        unit_out_not_found   = 9
        OTHERS               = 10.
    IF sy-subrc = 0.
      pv_num = lv_menge_out.
    ELSE.
      pv_num = lv_menge_in.
    ENDIF.
  ELSE.
    pv_num = lv_menge_in.
  ENDIF.

  "=========================================================
  " FIX 1: RUNDUNG ENTFERNT
  " ALT: pv_num = round( val = pv_num dec = 0 ).
  " 0.070 M wurde zu 0 gerundet → Menge verschwand komplett
  " Rundung ist hier sachlich nicht begründet.
  " Wenn gerundete Werte in der Ausgabe gewünscht sind,
  " kann das dort gemacht werden.
  "=========================================================

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM get_excel_col - Zahl zu Excel-Spalte
*&---------------------------------------------------------------------*
FORM get_excel_col
  USING pv_num TYPE i
  CHANGING pv_col TYPE char3.

  DATA: lv_num  TYPE i,
        lv_rest TYPE i,
        lv_temp TYPE char3.

  lv_num = pv_num.
  CLEAR pv_col.

  WHILE lv_num > 0.
    lv_rest = ( lv_num - 1 ) MOD 26.
    lv_temp = pv_col.
    pv_col = sy-abcde+lv_rest(1).
    CONCATENATE pv_col lv_temp INTO pv_col.
    lv_num = ( lv_num - 1 ) DIV 26.
  ENDWHILE.

  CONDENSE pv_col NO-GAPS.

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM ausgabe_weitere_vknr


*&---------------------------------------------------------------------*
FORM ausgabe_weitere_vknr.

  DATA:
    lv_line_idx    TYPE i,
    lv_header      TYPE string,
    lv_data_line   TYPE string,
    lv_vknr_str    TYPE string,
    lv_col_num     TYPE i,
    lv_col_text    TYPE string,
    ls_ktab        TYPE ty_ktab,
    ls_weitere     TYPE ty_weitere_vknr,
    lt_vknr_zeile  TYPE STANDARD TABLE OF matnr,
    lv_vknr        TYPE matnr,
    lv_ktab_idx    TYPE i.

  FIELD-SYMBOLS: <fs_line> TYPE string.


  CHECK p_topd = 'X'.
  CHECK p_wvknr = 'X'.
  CHECK gv_max_weitere > 0.
  CHECK gt_excel IS NOT INITIAL.




  READ TABLE gt_excel ASSIGNING <fs_line> INDEX 1.
  IF sy-subrc = 0.
    DO gv_max_weitere TIMES.
      lv_col_num = sy-index.
      lv_col_text = lv_col_num.
      CONDENSE lv_col_text.
      CONCATENATE 'Weitere VKNR ' lv_col_text INTO lv_header.
      CONCATENATE <fs_line> gv_tab lv_header INTO <fs_line>.
    ENDDO.
  ENDIF.




  READ TABLE gt_excel ASSIGNING <fs_line> INDEX 2.
  IF sy-subrc = 0.
    DO gv_max_weitere TIMES.
      CONCATENATE <fs_line> gv_tab '' INTO <fs_line>.
    ENDDO.
  ENDIF.




  READ TABLE gt_excel ASSIGNING <fs_line> INDEX 3.
  IF sy-subrc = 0.
    DO gv_max_weitere TIMES.
      CONCATENATE <fs_line> gv_tab '' INTO <fs_line>.
    ENDDO.
  ENDIF.




  lv_ktab_idx = 0.

  LOOP AT gt_ktab INTO ls_ktab.

    lv_ktab_idx = lv_ktab_idx + 1.
    lv_line_idx = lv_ktab_idx + 3.


    READ TABLE gt_excel ASSIGNING <fs_line> INDEX lv_line_idx.
    IF sy-subrc <> 0.
      CONTINUE.
    ENDIF.


    CLEAR lt_vknr_zeile.
    LOOP AT gt_weitere_vknr INTO ls_weitere
      WHERE kompnr = ls_ktab-idnrk.
      APPEND ls_weitere-vknr TO lt_vknr_zeile.
    ENDLOOP.


    lv_col_num = 0.
    DO gv_max_weitere TIMES.
      lv_col_num = lv_col_num + 1.

      READ TABLE lt_vknr_zeile INTO lv_vknr INDEX lv_col_num.
      IF sy-subrc = 0.
        lv_vknr_str = lv_vknr.
      ELSE.
        CLEAR lv_vknr_str.
      ENDIF.

      CONCATENATE <fs_line> gv_tab lv_vknr_str INTO <fs_line>.
    ENDDO.

  ENDLOOP.

ENDFORM.



*&---------------------------------------------------------------------*
*& FORM ermittle_weitere_vknr


*&---------------------------------------------------------------------*
FORM ermittle_weitere_vknr.

  TYPES:

    BEGIN OF lty_usage,
      kompnr TYPE matnr,
      vknr   TYPE matnr,
    END OF lty_usage,


    BEGIN OF lty_mara,
      matnr TYPE matnr,
      mtart TYPE mtart,
      mstae TYPE mstae,
    END OF lty_mara.

  DATA:
    lt_all_usage    TYPE STANDARD TABLE OF lty_usage,
    lt_kompnr_list  TYPE tt_matnr_list,
    lt_vknr_list    TYPE tt_matnr_list,
    lt_mara_data    TYPE HASHED TABLE OF lty_mara WITH UNIQUE KEY matnr,
    lt_sel_vknr     TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
    lt_verbr_temp   TYPE HASHED TABLE OF ty_md04_full WITH UNIQUE KEY matnr,
    lt_result_temp  TYPE STANDARD TABLE OF ty_weitere_vknr,
    lt_count_temp   TYPE HASHED TABLE OF ty_count_raw WITH UNIQUE KEY kompnr,
    ls_usage        TYPE lty_usage,
    ls_mara         TYPE lty_mara,
    ls_weitere      TYPE ty_weitere_vknr,
    ls_matnr_line   TYPE ty_matnr_line,
    ls_ktab         TYPE ty_ktab,
    ls_vtab         TYPE ty_vtab,
    ls_exkl         TYPE ty_exkl,
    ls_md04_full    TYPE ty_md04_full,
    ls_count        TYPE ty_count_raw,
    lv_kompnr       TYPE matnr,
    lv_vknr         TYPE matnr,
    lv_count        TYPE i.


  CLEAR: gt_weitere_vknr, gv_max_weitere.


  CHECK p_topd = 'X'.
  CHECK p_wvknr = 'X'.
  CHECK gt_ktab IS NOT INITIAL.




  LOOP AT gt_ktab INTO ls_ktab.
    ls_matnr_line-matnr = ls_ktab-idnrk.
    APPEND ls_matnr_line TO lt_kompnr_list.
  ENDLOOP.




  LOOP AT gt_vtab INTO ls_vtab.
    INSERT ls_vtab-matnr INTO TABLE lt_sel_vknr.
  ENDLOOP.




  SELECT kompnr matnr AS vknr
    FROM zpowerbi_vc_txt
    INTO TABLE lt_all_usage
    FOR ALL ENTRIES IN lt_kompnr_list
    WHERE kompnr = lt_kompnr_list-matnr.

  IF lt_all_usage IS INITIAL.
    RETURN.
  ENDIF.


  SORT lt_all_usage BY kompnr vknr.
  DELETE ADJACENT DUPLICATES FROM lt_all_usage COMPARING kompnr vknr.




  DATA: lt_vknr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

  LOOP AT lt_all_usage INTO ls_usage.
    INSERT ls_usage-vknr INTO TABLE lt_vknr_temp.
  ENDLOOP.

  LOOP AT lt_vknr_temp INTO lv_vknr.
    ls_matnr_line-matnr = lv_vknr.
    APPEND ls_matnr_line TO lt_vknr_list.
  ENDLOOP.




  IF lt_vknr_list IS NOT INITIAL.
    SELECT matnr mtart mstae
      FROM mara
      INTO TABLE lt_mara_data
      FOR ALL ENTRIES IN lt_vknr_list
      WHERE matnr = lt_vknr_list-matnr.
  ENDIF.




  IF lt_vknr_list IS NOT INITIAL.
    SELECT matnr werks verbr
      FROM zmd04_calc
      INTO CORRESPONDING FIELDS OF TABLE lt_verbr_temp
      FOR ALL ENTRIES IN lt_vknr_list
      WHERE matnr = lt_vknr_list-matnr
        AND werks = gc_werks.
  ENDIF.




  LOOP AT lt_all_usage INTO ls_usage.


    READ TABLE lt_sel_vknr WITH TABLE KEY table_line = ls_usage-vknr
      TRANSPORTING NO FIELDS.
    IF sy-subrc = 0.
      CONTINUE.
    ENDIF.


    READ TABLE lt_mara_data INTO ls_mara
      WITH TABLE KEY matnr = ls_usage-vknr.
    IF sy-subrc <> 0.
      CONTINUE.
    ENDIF.


    IF ls_mara-mtart <> 'FERT'.
      CONTINUE.
    ENDIF.


    IF ls_mara-mstae = '99'.
      CONTINUE.
    ENDIF.


    READ TABLE gt_exkl_cache INTO ls_exkl
      WITH TABLE KEY kompnr = ls_usage-kompnr.
    IF sy-subrc = 0 AND ls_exkl-exklusiv = 'X'.
      CONTINUE.
    ENDIF.


    CLEAR ls_weitere.
    ls_weitere-kompnr = ls_usage-kompnr.
    ls_weitere-vknr   = ls_usage-vknr.


    READ TABLE lt_verbr_temp INTO ls_md04_full
      WITH TABLE KEY matnr = ls_usage-vknr.
    IF sy-subrc = 0.
      ls_weitere-verbr = ls_md04_full-verbr.
    ELSE.
      ls_weitere-verbr = 0.
    ENDIF.

    APPEND ls_weitere TO lt_result_temp.

  ENDLOOP.




  SORT lt_result_temp BY kompnr verbr DESCENDING.




  LOOP AT lt_result_temp INTO ls_weitere.


    READ TABLE lt_count_temp INTO ls_count
      WITH TABLE KEY kompnr = ls_weitere-kompnr.
    IF sy-subrc = 0.
      IF ls_count-count >= 100.
        CONTINUE.
      ENDIF.
      ls_count-count = ls_count-count + 1.
      MODIFY TABLE lt_count_temp FROM ls_count.
    ELSE.
      ls_count-kompnr = ls_weitere-kompnr.
      ls_count-count = 1.
      INSERT ls_count INTO TABLE lt_count_temp.
    ENDIF.


    APPEND ls_weitere TO gt_weitere_vknr.


    IF ls_count-count > gv_max_weitere.
      gv_max_weitere = ls_count-count.
    ENDIF.

  ENDLOOP.

ENDFORM.




*&---------------------------------------------------------------------*
*& FORM ausgabe_elternmaterial



*&---------------------------------------------------------------------*
FORM ausgabe_elternmaterial.

  TYPES:
    BEGIN OF lty_parent,
      kompnr    TYPE matnr,
      matnr     TYPE matnr,
      kom_mstae TYPE matnr,
    END OF lty_parent.

  DATA:
    lt_parents     TYPE STANDARD TABLE OF lty_parent,
    lt_kompnr_list TYPE tt_matnr_list,
    ls_parent      TYPE lty_parent,
    ls_matnr_line  TYPE ty_matnr_line,
    ls_ktab        TYPE ty_ktab,
    ls_vtab        TYPE ty_vtab,
    lv_eltern      TYPE string,
    lv_matnr_str   TYPE string,
    lv_line_idx    TYPE i,
    lv_ktab_idx    TYPE i.


  DATA: lr_sel_vknr TYPE RANGE OF matnr,
        ls_sel_vknr LIKE LINE OF lr_sel_vknr.

  DATA: lt_eltern_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

  FIELD-SYMBOLS: <fs_line> TYPE string.


  CHECK p_topd = 'X'.
  CHECK gt_excel IS NOT INITIAL.
  CHECK gt_ktab IS NOT INITIAL.




  LOOP AT gt_ktab INTO ls_ktab.
    ls_matnr_line-matnr = ls_ktab-idnrk.
    APPEND ls_matnr_line TO lt_kompnr_list.
  ENDLOOP.




  LOOP AT gt_vtab INTO ls_vtab.
    ls_sel_vknr-sign   = 'I'.
    ls_sel_vknr-option = 'EQ'.
    ls_sel_vknr-low    = ls_vtab-matnr.
    APPEND ls_sel_vknr TO lr_sel_vknr.
  ENDLOOP.




  IF lt_kompnr_list IS NOT INITIAL AND lr_sel_vknr IS NOT INITIAL.
    SELECT kompnr matnr kom_mstae
      FROM zpowerbi_vc_txt
      INTO TABLE lt_parents
      FOR ALL ENTRIES IN lt_kompnr_list
      WHERE kompnr = lt_kompnr_list-matnr
        AND matnr IN lr_sel_vknr.
  ENDIF.




  READ TABLE gt_excel ASSIGNING <fs_line> INDEX 1.
  IF sy-subrc = 0.
    CONCATENATE <fs_line> gv_tab 'Elternmaterial' INTO <fs_line>.
  ENDIF.




  READ TABLE gt_excel ASSIGNING <fs_line> INDEX 2.
  IF sy-subrc = 0.
    CONCATENATE <fs_line> gv_tab '' INTO <fs_line>.
  ENDIF.




  READ TABLE gt_excel ASSIGNING <fs_line> INDEX 3.
  IF sy-subrc = 0.
    CONCATENATE <fs_line> gv_tab '' INTO <fs_line>.
  ENDIF.




  lv_ktab_idx = 0.

  LOOP AT gt_ktab INTO ls_ktab.
    lv_ktab_idx = lv_ktab_idx + 1.
    lv_line_idx = lv_ktab_idx + 3.

    READ TABLE gt_excel ASSIGNING <fs_line> INDEX lv_line_idx.
    IF sy-subrc <> 0.
      CONTINUE.
    ENDIF.


    CLEAR: lv_eltern, lt_eltern_temp.


    LOOP AT lt_parents INTO ls_parent WHERE kompnr = ls_ktab-idnrk.
      IF ls_parent-kom_mstae IS NOT INITIAL.
        INSERT ls_parent-kom_mstae INTO TABLE lt_eltern_temp.
      ENDIF.
    ENDLOOP.


    LOOP AT lt_eltern_temp INTO DATA(lv_parent_matnr).

      CALL FUNCTION 'CONVERSION_EXIT_ALPHA_OUTPUT'
        EXPORTING
          input  = lv_parent_matnr
        IMPORTING
          output = lv_matnr_str.


      CONDENSE lv_matnr_str NO-GAPS.

      IF lv_eltern IS INITIAL.
        lv_eltern = lv_matnr_str.
      ELSE.
        CONCATENATE lv_eltern ', ' lv_matnr_str INTO lv_eltern RESPECTING BLANKS.
      ENDIF.
    ENDLOOP.


    CONCATENATE <fs_line> gv_tab lv_eltern INTO <fs_line>.

  ENDLOOP.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM ausgabe_spalten_details





*&---------------------------------------------------------------------*
FORM ausgabe_spalten_details.

  DATA:
    ls_vtab       TYPE ty_vtab,
    ls_md04_full  TYPE ty_md04_full,
    ls_stamm      TYPE ty_stamm,
    lv_line       TYPE string,
    lv_wert       TYPE string,
    lv_stueck     TYPE p LENGTH 11 DECIMALS 2,
    lv_endbestand TYPE menge_d,
    lv_wert_calc  TYPE p LENGTH 11 DECIMALS 2,
    lv_peinh      TYPE peinh.

  CHECK gt_vtab IS NOT INITIAL.
  CHECK gt_excel IS NOT INITIAL.




  APPEND '' TO gt_excel.





  " --- Lagerbestand ---
  lv_line = 'Lagerbestand'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_md04_full-labst.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Feste Zugänge ---
  lv_line = 'Feste Zugänge'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_md04_full-feste_zug.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Geplante Zugänge ---
  lv_line = 'Geplante Zugänge'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_md04_full-gepl_zug.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Feste Abgänge (mit Minus) ---
  lv_line = 'Feste Abgänge'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0 AND ls_md04_full-feste_abg > 0.
      lv_wert = ls_md04_full-feste_abg.
      CONDENSE lv_wert.
      CONCATENATE '-' lv_wert INTO lv_wert.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Geplante Abgänge (mit Minus) ---
  lv_line = 'Geplante Abgänge'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0 AND ls_md04_full-gepl_abg > 0.
      lv_wert = ls_md04_full-gepl_abg.
      CONDENSE lv_wert.
      CONCATENATE '-' lv_wert INTO lv_wert.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Endbestand (berechnet) ---
  lv_line = 'Endbestand'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_endbestand = ls_md04_full-labst
                    + ls_md04_full-feste_zug
                    + ls_md04_full-gepl_zug
                    - ls_md04_full-feste_abg
                    - ls_md04_full-gepl_abg.
      lv_wert = lv_endbestand.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- offene MK ---
  lv_line = 'offene MK'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      IF ls_md04_full-mkmng < 0.
        lv_wert = ls_md04_full-mkmng * -1.
        CONDENSE lv_wert.
        CONCATENATE '-' lv_wert INTO lv_wert.
      ELSE.
        lv_wert = ls_md04_full-mkmng.
        CONDENSE lv_wert.
      ENDIF.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Stückkosten Lager ---
  lv_line = 'Stückkosten Lager'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_peinh = ls_stamm-peinh.
      IF lv_peinh IS INITIAL.
        lv_peinh = 1.
      ENDIF.
      IF ls_stamm-vprsv = 'V'.
        lv_stueck = ls_stamm-verpr / lv_peinh.
      ELSE.
        lv_stueck = ls_stamm-stprs / lv_peinh.
      ENDIF.
      lv_wert = lv_stueck.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Wert Lagerbest. (SALK3) ---
  lv_line = 'Wert Lagerbest.'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-salk3.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Wert Feste Zugänge ---
  lv_line = 'Wert Feste Zugänge'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    CLEAR: ls_md04_full, ls_stamm, lv_stueck.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_peinh = ls_stamm-peinh.
      IF lv_peinh IS INITIAL.
        lv_peinh = 1.
      ENDIF.
      IF ls_stamm-vprsv = 'V'.
        lv_stueck = ls_stamm-verpr / lv_peinh.
      ELSE.
        lv_stueck = ls_stamm-stprs / lv_peinh.
      ENDIF.
      lv_wert_calc = ls_md04_full-feste_zug * lv_stueck.
      lv_wert = lv_wert_calc.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Wert Geplante Zugänge ---
  lv_line = 'Wert Geplante Zugänge'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    CLEAR: ls_md04_full, ls_stamm, lv_stueck.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_peinh = ls_stamm-peinh.
      IF lv_peinh IS INITIAL.
        lv_peinh = 1.
      ENDIF.
      IF ls_stamm-vprsv = 'V'.
        lv_stueck = ls_stamm-verpr / lv_peinh.
      ELSE.
        lv_stueck = ls_stamm-stprs / lv_peinh.
      ENDIF.
      lv_wert_calc = ls_md04_full-gepl_zug * lv_stueck.
      lv_wert = lv_wert_calc.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Wert Feste Abgänge (mit Minus) ---
  lv_line = 'Wert Feste Abgänge'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    CLEAR: ls_md04_full, ls_stamm, lv_stueck.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_peinh = ls_stamm-peinh.
      IF lv_peinh IS INITIAL.
        lv_peinh = 1.
      ENDIF.
      IF ls_stamm-vprsv = 'V'.
        lv_stueck = ls_stamm-verpr / lv_peinh.
      ELSE.
        lv_stueck = ls_stamm-stprs / lv_peinh.
      ENDIF.
      lv_wert_calc = ls_md04_full-feste_abg * lv_stueck.
      IF lv_wert_calc > 0.
        lv_wert = lv_wert_calc.
        CONDENSE lv_wert.
        CONCATENATE '-' lv_wert INTO lv_wert.
      ELSE.
        lv_wert = '0'.
      ENDIF.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Wert Geplante Abgänge (mit Minus) ---
  lv_line = 'Wert Geplante Abgänge'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    CLEAR: ls_md04_full, ls_stamm, lv_stueck.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_peinh = ls_stamm-peinh.
      IF lv_peinh IS INITIAL.
        lv_peinh = 1.
      ENDIF.
      IF ls_stamm-vprsv = 'V'.
        lv_stueck = ls_stamm-verpr / lv_peinh.
      ELSE.
        lv_stueck = ls_stamm-stprs / lv_peinh.
      ENDIF.
      lv_wert_calc = ls_md04_full-gepl_abg * lv_stueck.
      IF lv_wert_calc > 0.
        lv_wert = lv_wert_calc.
        CONDENSE lv_wert.
        CONCATENATE '-' lv_wert INTO lv_wert.
      ELSE.
        lv_wert = '0'.
      ENDIF.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Wert Endbestand ---
  lv_line = 'Wert Endbestand'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    CLEAR: ls_md04_full, ls_stamm, lv_stueck, lv_endbestand.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_peinh = ls_stamm-peinh.
      IF lv_peinh IS INITIAL.
        lv_peinh = 1.
      ENDIF.
      IF ls_stamm-vprsv = 'V'.
        lv_stueck = ls_stamm-verpr / lv_peinh.
      ELSE.
        lv_stueck = ls_stamm-stprs / lv_peinh.
      ENDIF.
      lv_endbestand = ls_md04_full-labst
                    + ls_md04_full-feste_zug
                    + ls_md04_full-gepl_zug
                    - ls_md04_full-feste_abg
                    - ls_md04_full-gepl_abg.
      lv_wert_calc = lv_endbestand * lv_stueck.
      lv_wert = lv_wert_calc.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Wert offene MK ---
  lv_line = 'Wert offene MK'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      IF ls_md04_full-omkwr < 0.
        lv_wert = ls_md04_full-omkwr * -1.
        CONDENSE lv_wert.
        CONCATENATE '-' lv_wert INTO lv_wert.
      ELSE.
        lv_wert = ls_md04_full-omkwr.
        CONDENSE lv_wert.
      ENDIF.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Dispom. ---
  lv_line = 'Dispom.'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-dismm.
    ELSE.
      lv_wert = ''.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Meldebest. ---
  lv_line = 'Meldebest.'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-minbe.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Dispolg. ---
  lv_line = 'Dispolg.'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-disls.
    ELSE.
      lv_wert = ''.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Feste Losgr. ---
  lv_line = 'Feste Losgr.'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-bstfe.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Sibe ---
  lv_line = 'Sibe'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-eisbe.
    ELSE.
      lv_wert = '0'.
    ENDIF.
    CONDENSE lv_wert.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Matst. ---
  lv_line = 'Matst.'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-mstae.
    ELSE.
      lv_wert = ''.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Verk.st. ---
  lv_line = 'Verk.st.'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-mstav.
    ELSE.
      lv_wert = ''.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- LZ-Code ---
  lv_line = 'LZ-Code'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-zzlzcod.
    ELSE.
      lv_wert = ''.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

  " --- Sort-Code ---
  lv_line = 'Sort-Code'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      lv_wert = ls_stamm-zzlzcodsort.
    ELSE.
      lv_wert = ''.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.
  APPEND lv_line TO gt_excel.

ENDFORM.



*&---------------------------------------------------------------------*
*& FORM load_elternmaterial_cache


*&---------------------------------------------------------------------*
FORM load_elternmaterial_cache.

  TYPES:
    BEGIN OF lty_parent_raw,
      kompnr    TYPE matnr,
      kom_mstae TYPE matnr,
    END OF lty_parent_raw.

  DATA:
    lt_parents     TYPE STANDARD TABLE OF lty_parent_raw,
    lt_kompnr_list TYPE tt_matnr_list,
    ls_parent      TYPE lty_parent_raw,
    ls_matnr_line  TYPE ty_matnr_line,
    ls_ktab        TYPE ty_ktab,
    ls_vtab        TYPE ty_vtab,
    ls_cache       TYPE ty_mtab.


  DATA: lr_sel_vknr TYPE RANGE OF matnr,
        ls_sel_vknr LIKE LINE OF lr_sel_vknr.


  CHECK p_topd = 'X'.
  CHECK gt_ktab IS NOT INITIAL.


  LOOP AT gt_ktab INTO ls_ktab.
    ls_matnr_line-matnr = ls_ktab-idnrk.
    APPEND ls_matnr_line TO lt_kompnr_list.
  ENDLOOP.


  LOOP AT gt_vtab INTO ls_vtab.
    ls_sel_vknr-sign   = 'I'.
    ls_sel_vknr-option = 'EQ'.
    ls_sel_vknr-low    = ls_vtab-matnr.
    APPEND ls_sel_vknr TO lr_sel_vknr.
  ENDLOOP.

  CHECK lt_kompnr_list IS NOT INITIAL.
  CHECK lr_sel_vknr IS NOT INITIAL.


  SELECT kompnr kom_mstae
    FROM zpowerbi_vc_txt
    INTO TABLE lt_parents
    FOR ALL ENTRIES IN lt_kompnr_list
    WHERE kompnr = lt_kompnr_list-matnr
      AND matnr IN lr_sel_vknr.


  LOOP AT lt_parents INTO ls_parent.
    IF ls_parent-kom_mstae IS NOT INITIAL.
      ls_cache-matnr = ls_parent-kompnr.
      ls_cache-idnrk = ls_parent-kom_mstae.
      ls_cache-menge = 0.
      INSERT ls_cache INTO TABLE gt_eltern_cache.
    ENDIF.
  ENDLOOP.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM get_elternmaterial

*&---------------------------------------------------------------------*
FORM get_elternmaterial
  USING    pv_kompnr TYPE matnr
  CHANGING pv_eltern TYPE string.

  DATA:
    ls_cache     TYPE ty_mtab,
    lv_matnr_str TYPE string,
    lt_distinct  TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

  CLEAR pv_eltern.


  LOOP AT gt_eltern_cache INTO ls_cache WHERE matnr = pv_kompnr.
    INSERT ls_cache-idnrk INTO TABLE lt_distinct.
  ENDLOOP.


  LOOP AT lt_distinct INTO DATA(lv_parent_matnr).

    CALL FUNCTION 'CONVERSION_EXIT_ALPHA_OUTPUT'
      EXPORTING
        input  = lv_parent_matnr
      IMPORTING
        output = lv_matnr_str.

    CONDENSE lv_matnr_str NO-GAPS.

    IF pv_eltern IS INITIAL.
      pv_eltern = lv_matnr_str.
    ELSE.
      CONCATENATE pv_eltern ', ' lv_matnr_str INTO pv_eltern.
    ENDIF.
  ENDLOOP.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM create_excel_output


*&---------------------------------------------------------------------*
FORM create_excel_output.

  DATA: lv_line          TYPE string,
        lv_col           TYPE char3,
        lv_pos           TYPE i VALUE 0,
        ls_vtab          TYPE ty_vtab,
        ls_ktab          TYPE ty_ktab,
        ls_mtab          TYPE ty_mtab,
        ls_exkl          TYPE ty_exkl,
        lv_menge         TYPE string,
        lv_formel        TYPE string,
        lv_line_str      TYPE string,
        lv_last_row      TYPE i,



        lv_feste_abg_str TYPE string,
        lv_gepl_abg_str  TYPE string,
        lv_wert_str      TYPE string,
        ls_compl         TYPE ty_compl.     " FIX 19


  FIELD-SYMBOLS: <fs_vtab_upd> TYPE ty_vtab.

  gv_line = 1.




  IF p_topd = 'X'.
    lv_line = TEXT-001.
  ELSE.
    lv_line = TEXT-021.
  ENDIF.

  CONCATENATE lv_line gv_tab TEXT-002 INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-024 INTO lv_line.


  IF p_topd = 'X'.
    CONCATENATE lv_line gv_tab TEXT-030 INTO lv_line.
  ELSE.
    CONCATENATE lv_line gv_tab TEXT-001 INTO lv_line.
  ENDIF.


  LOOP AT gt_vtab INTO ls_vtab.
    lv_pos = lv_pos + 1.
    DATA q TYPE i.
    q = lv_pos + 4.
    PERFORM get_excel_col USING q CHANGING lv_col.


    READ TABLE gt_vtab ASSIGNING <fs_vtab_upd> WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      <fs_vtab_upd>-sp = lv_col.
    ENDIF.

    IF lv_pos = 1.
      gv_sp_von = lv_col.
    ENDIF.
    gv_sp_bis = lv_col.

    CONCATENATE lv_line gv_tab ls_vtab-matnr INTO lv_line.
  ENDLOOP.


  IF p_topd = 'X'.
    CONCATENATE lv_line gv_tab 'Exklusiv' INTO lv_line.
  ENDIF.

  lv_pos = lv_pos + 1.
  CLEAR q.
  q = lv_pos + 4.
  IF p_topd = 'X'.
    q = q + 1.
  ENDIF.
  PERFORM get_excel_col USING q CHANGING gv_sp_sum.
  CONCATENATE lv_line gv_tab TEXT-005 INTO lv_line.

  lv_pos = lv_pos + 1.
  q = lv_pos + 4.
  IF p_topd = 'X'.
    q = q + 1.
  ENDIF.
  PERFORM get_excel_col USING q CHANGING gv_sp_vbr.
  CONCATENATE lv_line gv_tab TEXT-006 INTO lv_line.


  CONCATENATE lv_line gv_tab 'Lagerbestand' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Feste Zugänge' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Geplante Zugänge' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Feste Abgänge' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Geplante Abgänge' INTO lv_line.








  CONCATENATE lv_line gv_tab 'Endbestand' INTO lv_line.


  CONCATENATE lv_line gv_tab TEXT-009 INTO lv_line.




  CONCATENATE lv_line gv_tab 'Stückkosten Lager' INTO lv_line.


  CONCATENATE lv_line gv_tab TEXT-010 INTO lv_line.





  CONCATENATE lv_line gv_tab 'Wert Feste Zugänge' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Wert Geplante Zugänge' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Wert Feste Abgänge' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Wert Geplante Abgänge' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Wert Endbestand' INTO lv_line.


  CONCATENATE lv_line gv_tab TEXT-012 INTO lv_line.


  CONCATENATE lv_line gv_tab TEXT-013 INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-014 INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-015 INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-016 INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-017 INTO lv_line.


  CONCATENATE lv_line gv_tab TEXT-018 INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-019 INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-020 INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-025 INTO lv_line.
  CONCATENATE lv_line gv_tab 'Postyp' INTO lv_line.
  CONCATENATE lv_line gv_tab 'RoHS' gv_tab 'REACH' gv_tab 'SVHC' gv_tab 'PFAS-Gehalt' INTO lv_line.  " FIX 19

  " FIX 22: Disponent (MARC-DISPO) und Warengruppe (MARA-MATKL) nur im Top-Down; bisher fehlten
  " beide, fuer die Warengruppenauswertung musste man sie je Komponente nachschlagen (Wunsch Armin).
  IF p_topd = 'X'.
    CONCATENATE lv_line gv_tab 'Disponent' gv_tab 'Warengruppe' INTO lv_line.
  ENDIF.

  APPEND lv_line TO gt_excel.
  gv_line = gv_line + 1.


  " MSTAE-ZEILE (nur Top-Down)

  IF p_topd = 'X'.
    CLEAR lv_line.
    lv_line = 'Mat.Status'.


    CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.


    LOOP AT gt_vtab INTO ls_vtab.
      CONCATENATE lv_line gv_tab ls_vtab-mstae INTO lv_line.
    ENDLOOP.


    CONCATENATE lv_line gv_tab '' INTO lv_line.

    APPEND lv_line TO gt_excel.
    gv_line = gv_line + 1.
  ENDIF.


  " VERBRAUCHSZEILE

  CLEAR lv_line.
  IF p_topd = 'X'.
    lv_line = TEXT-022.
  ELSE.
    lv_line = TEXT-024.
  ENDIF.

  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.

  LOOP AT gt_vtab INTO ls_vtab.
    IF p_topd = 'X'.
      lv_menge = ls_vtab-verbr.
      CONDENSE lv_menge.
    ELSE.
      lv_menge = ls_vtab-meins.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.
  ENDLOOP.


  IF p_topd = 'X'.
    CONCATENATE lv_line gv_tab '' INTO lv_line.
  ENDIF.

  APPEND lv_line TO gt_excel.
  gv_line = gv_line + 1.


  " DATENZEILEN

  LOOP AT gt_ktab INTO ls_ktab.





    lv_line = ls_ktab-idnrk.
    CONCATENATE lv_line gv_tab ls_ktab-maktx INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-meins INTO lv_line.


    IF p_topd = 'X'.
      DATA: lv_eltern TYPE string.
      PERFORM get_elternmaterial USING ls_ktab-idnrk CHANGING lv_eltern.
      CONCATENATE lv_line gv_tab lv_eltern INTO lv_line.
    ELSE.
      CONCATENATE lv_line gv_tab ls_ktab-matnr INTO lv_line.
    ENDIF.








    LOOP AT gt_vtab INTO ls_vtab.
      READ TABLE gt_mtab INTO ls_mtab
        WITH TABLE KEY matnr = ls_vtab-matnr idnrk = ls_ktab-idnrk.
      IF sy-subrc = 0.
        lv_menge = ls_mtab-menge.
      ELSE.
        lv_menge = '0'.
      ENDIF.
      CONDENSE lv_menge.
      CONCATENATE lv_line gv_tab lv_menge INTO lv_line.
    ENDLOOP.


    IF p_topd = 'X'.
      READ TABLE gt_exkl_cache INTO ls_exkl WITH TABLE KEY kompnr = ls_ktab-idnrk.
      IF sy-subrc = 0 AND ls_exkl-exklusiv = 'X'.
        CONCATENATE lv_line gv_tab 'X' INTO lv_line.
      ELSE.
        CONCATENATE lv_line gv_tab '' INTO lv_line.
      ENDIF.
    ENDIF.


    lv_line_str = gv_line.
    CONDENSE lv_line_str.
    IF p_topd = 'X'.



      CONCATENATE '=SUMMENPRODUKT($' gv_sp_von '$3:$' gv_sp_bis '$3;'
                  gv_sp_von lv_line_str ':' gv_sp_bis lv_line_str ')' INTO lv_formel.
    ELSE.
      IF ls_ktab-baugr IS INITIAL.
        CONCATENATE '=SUMME(' gv_sp_von INTO lv_formel.
        CONCATENATE lv_formel lv_line_str ':' gv_sp_bis lv_line_str ')' INTO lv_formel.
      ELSE.
        lv_formel = '0'.
      ENDIF.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_formel INTO lv_line.


    lv_menge = ls_ktab-verbr. CONDENSE lv_menge.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.


    lv_menge = ls_ktab-labst. CONDENSE lv_menge.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.
    lv_menge = ls_ktab-feste_zug. CONDENSE lv_menge.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.
    lv_menge = ls_ktab-gepl_zug. CONDENSE lv_menge.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.




    IF ls_ktab-feste_abg > 0.
      lv_feste_abg_str = ls_ktab-feste_abg.
      CONDENSE lv_feste_abg_str.
      CONCATENATE '-' lv_feste_abg_str INTO lv_feste_abg_str.
    ELSE.
      lv_feste_abg_str = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_feste_abg_str INTO lv_line.





    IF ls_ktab-gepl_abg > 0.
      lv_gepl_abg_str = ls_ktab-gepl_abg.
      CONDENSE lv_gepl_abg_str.
      CONCATENATE '-' lv_gepl_abg_str INTO lv_gepl_abg_str.
    ELSE.
      lv_gepl_abg_str = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_gepl_abg_str INTO lv_line.











    lv_menge = ls_ktab-endbestand. CONDENSE lv_menge.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.






    IF ls_ktab-mkmng < 0.
      lv_menge = ls_ktab-mkmng * -1.
      CONDENSE lv_menge.
      CONCATENATE '-' lv_menge INTO lv_menge.
    ELSE.
      lv_menge = ls_ktab-mkmng.
      CONDENSE lv_menge.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.







    lv_wert_str = ls_ktab-stueckkosten. CONDENSE lv_wert_str.
    CONCATENATE lv_line gv_tab lv_wert_str INTO lv_line.



    lv_menge = ls_ktab-salk3. CONDENSE lv_menge.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.








    lv_wert_str = ls_ktab-wert_feste_zug. CONDENSE lv_wert_str.
    CONCATENATE lv_line gv_tab lv_wert_str INTO lv_line.





    lv_wert_str = ls_ktab-wert_gepl_zug. CONDENSE lv_wert_str.
    CONCATENATE lv_line gv_tab lv_wert_str INTO lv_line.





    IF ls_ktab-wert_feste_abg > 0.
      lv_wert_str = ls_ktab-wert_feste_abg.
      CONDENSE lv_wert_str.
      CONCATENATE '-' lv_wert_str INTO lv_wert_str.
    ELSE.
      lv_wert_str = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert_str INTO lv_line.





    IF ls_ktab-wert_gepl_abg > 0.
      lv_wert_str = ls_ktab-wert_gepl_abg.
      CONDENSE lv_wert_str.
      CONCATENATE '-' lv_wert_str INTO lv_wert_str.
    ELSE.
      lv_wert_str = '0'.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert_str INTO lv_line.





    lv_wert_str = ls_ktab-wert_endbestand. CONDENSE lv_wert_str.
    CONCATENATE lv_line gv_tab lv_wert_str INTO lv_line.







    IF ls_ktab-omkwr < 0.
      lv_wert_str = ls_ktab-omkwr * -1.
      CONDENSE lv_wert_str.
      CONCATENATE '-' lv_wert_str INTO lv_wert_str.
    ELSE.
      lv_wert_str = ls_ktab-omkwr.
      CONDENSE lv_wert_str.
    ENDIF.
    CONCATENATE lv_line gv_tab lv_wert_str INTO lv_line.



    CONCATENATE lv_line gv_tab ls_ktab-dismm INTO lv_line.
    lv_menge = ls_ktab-minbe. CONDENSE lv_menge.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-disls INTO lv_line.
    lv_menge = ls_ktab-bstfe. CONDENSE lv_menge.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.
    lv_menge = ls_ktab-eisbe. CONDENSE lv_menge.
    CONCATENATE lv_line gv_tab lv_menge INTO lv_line.


    CONCATENATE lv_line gv_tab ls_ktab-mstae INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-mstav INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-zzlzcod INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-zzlzcodsort INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-postyp INTO lv_line.

    " FIX 19: Stoff-Compliance des Zeilenmaterials, leer = nicht gepflegt
    PERFORM get_compliance USING ls_ktab-idnrk CHANGING ls_compl.
    CONCATENATE lv_line gv_tab ls_compl-rohs gv_tab ls_compl-reach gv_tab ls_compl-svhc gv_tab ls_compl-pfas
      INTO lv_line.

    " FIX 22: Disponent und Warengruppe der Komponente; Textpositionen bleiben leer
    IF p_topd = 'X'.
      CONCATENATE lv_line gv_tab ls_ktab-dispo gv_tab ls_ktab-matkl INTO lv_line.
    ENDIF.

    APPEND lv_line TO gt_excel.
    gv_line = gv_line + 1.

  ENDLOOP.


  " SUMMENZEILE (nur Bottom-Up)

  IF p_botu = 'X'.
    CLEAR lv_line.
    lv_line = TEXT-023.

    CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.

    lv_last_row = gv_line - 1.

    LOOP AT gt_vtab INTO ls_vtab.
      lv_line_str = lv_last_row.
      CONDENSE lv_line_str.
      CONCATENATE '=SUMME(' ls_vtab-sp '3:' ls_vtab-sp INTO lv_formel.
      CONCATENATE lv_formel lv_line_str ')' INTO lv_formel.
      CONCATENATE lv_line gv_tab lv_formel INTO lv_line.
    ENDLOOP.

    APPEND lv_line TO gt_excel.
  ENDIF.

ENDFORM.












*&---------------------------------------------------------------------*
*& FORM load_compliance_cache (FIX 19)
*& Liest ROHS, REACHCOM, SVHCCOMP und PFAS (Klasse Z_PACK_REGULATION, Klassenart
*& 001) aus AUSP fuer alle Zeilenmaterialien. Die Merkmale werden ueber den
*& Namen gesucht, weil ATINN in T76 und P76 verschieden ist.
*&---------------------------------------------------------------------*
FORM load_compliance_cache.

  TYPES: BEGIN OF ty_obj,
           objek TYPE ausp-objek,
         END OF ty_obj,
         BEGIN OF ty_cabn,
           atinn TYPE cabn-atinn,
           atnam TYPE cabn-atnam,
         END OF ty_cabn,
         BEGIN OF ty_ausp,
           objek TYPE ausp-objek,
           atinn TYPE ausp-atinn,
           atwrt TYPE ausp-atwrt,
           atflv TYPE ausp-atflv,
         END OF ty_ausp.

  DATA: lt_obj   TYPE STANDARD TABLE OF ty_obj WITH DEFAULT KEY,
        ls_obj   TYPE ty_obj,
        lt_cabn  TYPE STANDARD TABLE OF ty_cabn WITH DEFAULT KEY,
        ls_cabn  TYPE ty_cabn,
        lt_ausp  TYPE STANDARD TABLE OF ty_ausp WITH DEFAULT KEY,
        ls_ausp  TYPE ty_ausp,
        lr_atinn TYPE RANGE OF cabn-atinn,
        ls_atinn LIKE LINE OF lr_atinn,
        ls_ktab  TYPE ty_ktab,
        ls_compl TYPE ty_compl,
        lv_matnr TYPE matnr,
        lv_pfas  TYPE p LENGTH 13 DECIMALS 3,
        lv_text  TYPE string.

  FIELD-SYMBOLS: <ls_compl> TYPE ty_compl.

  CLEAR gt_compl_cache.
  CHECK gt_ktab IS NOT INITIAL.

  SELECT atinn atnam FROM cabn INTO TABLE lt_cabn
    WHERE atnam IN ('ROHS', 'REACHCOM', 'SVHCCOMP', 'PFAS').
  CHECK lt_cabn IS NOT INITIAL.

  LOOP AT lt_cabn INTO ls_cabn.
    ls_atinn-sign   = 'I'.
    ls_atinn-option = 'EQ'.
    ls_atinn-low    = ls_cabn-atinn.
    APPEND ls_atinn TO lr_atinn.
  ENDLOOP.

  " Zeilenmaterial in beiden Schreibweisen: KOMPNR hat keinen Konvertierungsexit
  LOOP AT gt_ktab INTO ls_ktab.
    ls_obj-objek = ls_ktab-idnrk.
    APPEND ls_obj TO lt_obj.
    CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
      EXPORTING
        input  = ls_ktab-idnrk
      IMPORTING
        output = lv_matnr
      EXCEPTIONS
        OTHERS = 1.
    IF sy-subrc = 0 AND lv_matnr <> ls_ktab-idnrk.
      ls_obj-objek = lv_matnr.
      APPEND ls_obj TO lt_obj.
    ENDIF.
  ENDLOOP.
  SORT lt_obj BY objek.
  DELETE ADJACENT DUPLICATES FROM lt_obj COMPARING objek.

  SELECT objek atinn atwrt atflv FROM ausp INTO TABLE lt_ausp
    FOR ALL ENTRIES IN lt_obj
    WHERE objek = lt_obj-objek
      AND atinn IN lr_atinn
      AND klart = '001'
      AND mafid = 'O'
      AND lkenz = space.

  LOOP AT lt_ausp INTO ls_ausp.
    READ TABLE lt_cabn INTO ls_cabn WITH KEY atinn = ls_ausp-atinn.
    CHECK sy-subrc = 0.

    lv_matnr = ls_ausp-objek.
    READ TABLE gt_compl_cache ASSIGNING <ls_compl> WITH TABLE KEY matnr = lv_matnr.
    IF sy-subrc <> 0.
      CLEAR ls_compl.
      ls_compl-matnr = lv_matnr.
      INSERT ls_compl INTO TABLE gt_compl_cache ASSIGNING <ls_compl>.
    ENDIF.

    CASE ls_cabn-atnam.
      WHEN 'ROHS'.
        IF <ls_compl>-rohs IS INITIAL.
          <ls_compl>-rohs = ls_ausp-atwrt.
        ENDIF.
      WHEN 'REACHCOM'.
        IF <ls_compl>-reach IS INITIAL.
          <ls_compl>-reach = ls_ausp-atwrt.
        ENDIF.
      WHEN 'SVHCCOMP'.
        IF <ls_compl>-svhc IS INITIAL.
          <ls_compl>-svhc = ls_ausp-atwrt.
        ENDIF.
      WHEN 'PFAS'.
        IF <ls_compl>-pfas IS INITIAL.
          lv_pfas = ls_ausp-atflv.
          lv_text = lv_pfas.
          CONDENSE lv_text.
          IF lv_text CA '.'.
            REPLACE REGEX '\.?0+$' IN lv_text WITH ''.
          ENDIF.
          <ls_compl>-pfas = lv_text.
        ENDIF.
    ENDCASE.
  ENDLOOP.

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM get_compliance (FIX 19)
*&---------------------------------------------------------------------*
FORM get_compliance USING    pv_matnr TYPE matnr
                    CHANGING ps_compl TYPE ty_compl.

  DATA: lv_matnr TYPE matnr.

  CLEAR ps_compl.
  READ TABLE gt_compl_cache INTO ps_compl WITH TABLE KEY matnr = pv_matnr.
  CHECK sy-subrc <> 0.

  CLEAR ps_compl.
  CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
    EXPORTING
      input  = pv_matnr
    IMPORTING
      output = lv_matnr
    EXCEPTIONS
      OTHERS = 1.
  IF sy-subrc = 0.
    READ TABLE gt_compl_cache INTO ps_compl WITH TABLE KEY matnr = lv_matnr.
    IF sy-subrc <> 0.
      CLEAR ps_compl.
    ENDIF.
  ENDIF.

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM download_dialog
*&---------------------------------------------------------------------*
FORM download_dialog.

  DATA: lv_filename TYPE string,
        lv_path     TYPE string,
        lv_fullpath TYPE string,
        lv_zip      TYPE xstring,
        lv_len      TYPE i,
        lt_solix    TYPE solix_tab.

  IF gt_excel IS INITIAL.
    MESSAGE 'Keine Daten vorhanden' TYPE 'S' DISPLAY LIKE 'W'.
    RETURN.
  ENDIF.

*** FIX 21: Statt Tab-Text (bis FIX 20 unter .xls, mit FIX 20 unter .txt) entsteht eine echte
*** Excel-Arbeitsmappe (.xlsx, Office Open XML) mit demselben Inhalt, siehe build_xlsx.
*** Grund: Excel verweigert Text unter .xls (Gruppenrichtlinie ExtensionHardening = 2), und
*** .txt ist fuer die Benutzer keine Excel-Datei (Wunsch Ingo 2026-10-22).
  CALL FUNCTION 'GUI_FILE_SAVE_DIALOG'
    EXPORTING
      default_extension = 'xlsx'
      default_file_name = 'Material_Verwendung'
      initial_directory = 'C:\temp'
      file_filter       = 'Excel-Arbeitsmappe (*.xlsx)|*.xlsx'
    IMPORTING
      filename          = lv_filename
      path              = lv_path
      fullpath          = lv_fullpath.

  CHECK lv_fullpath IS NOT INITIAL.

  PERFORM build_xlsx CHANGING lv_zip.

  CALL FUNCTION 'SCMS_XSTRING_TO_BINARY'
    EXPORTING
      buffer        = lv_zip
    IMPORTING
      output_length = lv_len
    TABLES
      binary_tab    = lt_solix.

  CALL FUNCTION 'GUI_DOWNLOAD'
    EXPORTING
      filename     = lv_fullpath
      filetype     = 'BIN'
      bin_filesize = lv_len
    TABLES
      data_tab     = lt_solix
    EXCEPTIONS
      OTHERS       = 1.
  IF sy-subrc <> 0.
    MESSAGE 'Datei konnte nicht gespeichert werden' TYPE 'S' DISPLAY LIKE 'E'.
    RETURN.
  ENDIF.

  CALL METHOD cl_gui_frontend_services=>execute
    EXPORTING
      document = lv_fullpath
    EXCEPTIONS
      OTHERS   = 1.

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM build_xlsx (FIX 21)
*& Baut aus gt_excel (Tab-getrennte Zeilen) eine minimale .xlsx-Datei:
*& Zahlen wie beim frueheren Text-Import (-?Ziffern[.Ziffern]) als Zahl,
*& Formeln mit englischen Funktionsnamen und Komma, Rest als Text.
*& Kopfzeile fett und fixiert. Formeln rechnet Excel beim Oeffnen.
*&---------------------------------------------------------------------*
FORM build_xlsx CHANGING pv_zip TYPE xstring.

  DATA: lo_zip    TYPE REF TO cl_abap_zip,
        lt_xml    TYPE TABLE OF string,
        lt_cells  TYPE TABLE OF string,
        lv_line   TYPE string,
        lv_val    TYPE string,
        lv_cell   TYPE string,
        lv_ref    TYPE string,
        lv_style  TYPE string,
        lv_rowstr TYPE string,
        lv_part   TYPE string,
        lv_row    TYPE i,
        lv_colnr  TYPE i,
        lv_col    TYPE char3,
        lv_c1     TYPE c LENGTH 1,
        lv_isnum  TYPE abap_bool,
        lt_txtcol TYPE TABLE OF i.   " FIX 22: Spalten, die immer Text bleiben

  CREATE OBJECT lo_zip.

  " [Content_Types].xml
  CLEAR lv_part.
  CONCATENATE `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`
    `<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">`
    `<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>`
    `<Default Extension="xml" ContentType="application/xml"/>`
    `<Override PartName="/xl/workbook.xml" `
    `ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>`
    `<Override PartName="/xl/worksheets/sheet1.xml" `
    `ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>`
    `<Override PartName="/xl/styles.xml" `
    `ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>`
    `</Types>` INTO lv_part.
  PERFORM xlsx_add USING lo_zip `[Content_Types].xml` lv_part.

  " _rels/.rels
  CLEAR lv_part.
  CONCATENATE `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`
    `<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">`
    `<Relationship Id="rId1" `
    `Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" `
    `Target="xl/workbook.xml"/></Relationships>` INTO lv_part.
  PERFORM xlsx_add USING lo_zip `_rels/.rels` lv_part.

  " xl/workbook.xml
  CLEAR lv_part.
  CONCATENATE `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`
    `<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" `
    `xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">`
    `<sheets><sheet name="ZLO03" sheetId="1" r:id="rId1"/></sheets>`
    `<calcPr calcId="124519" fullCalcOnLoad="1"/></workbook>` INTO lv_part.
  PERFORM xlsx_add USING lo_zip `xl/workbook.xml` lv_part.

  " xl/_rels/workbook.xml.rels
  CLEAR lv_part.
  CONCATENATE `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`
    `<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">`
    `<Relationship Id="rId1" `
    `Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" `
    `Target="worksheets/sheet1.xml"/>`
    `<Relationship Id="rId2" `
    `Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" `
    `Target="styles.xml"/></Relationships>` INTO lv_part.
  PERFORM xlsx_add USING lo_zip `xl/_rels/workbook.xml.rels` lv_part.

  " xl/styles.xml: Stil 0 normal, Stil 1 fett (Kopfzeile)
  CLEAR lv_part.
  CONCATENATE `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`
    `<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">`
    `<fonts count="2"><font><sz val="11"/><name val="Calibri"/><family val="2"/></font>`
    `<font><b/><sz val="11"/><name val="Calibri"/><family val="2"/></font></fonts>`
    `<fills count="2"><fill><patternFill patternType="none"/></fill>`
    `<fill><patternFill patternType="gray125"/></fill></fills>`
    `<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>`
    `<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>`
    `<cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>`
    `<xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>`
    `<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>`
    `</styleSheet>` INTO lv_part.
  PERFORM xlsx_add USING lo_zip `xl/styles.xml` lv_part.

  " xl/worksheets/sheet1.xml
  CLEAR lv_part.
  CONCATENATE `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`
    `<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">`
    `<sheetViews><sheetView workbookViewId="0">`
    `<pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/>`
    `</sheetView></sheetViews><sheetData>` INTO lv_part.
  APPEND lv_part TO lt_xml.

  LOOP AT gt_excel INTO lv_line.
    lv_row = sy-tabix.
    lv_rowstr = lv_row.
    CONDENSE lv_rowstr.
    IF lv_row = 1.
      lv_style = ` s="1"`.
    ELSE.
      CLEAR lv_style.
    ENDIF.
    CONCATENATE `<row r="` lv_rowstr `">` INTO lv_cell.
    APPEND lv_cell TO lt_xml.

    SPLIT lv_line AT gv_tab INTO TABLE lt_cells.
    LOOP AT lt_cells INTO lv_val.
      lv_colnr = sy-tabix.
      " FIX 22: Disponent '026' wuerde als Zahl 26 abgelegt; diese Spalten bleiben Text
      IF lv_row = 1 AND ( lv_val = 'Disponent' OR lv_val = 'Warengruppe' ).
        APPEND lv_colnr TO lt_txtcol.
      ENDIF.
      CHECK lv_val IS NOT INITIAL.
      PERFORM get_excel_col USING lv_colnr CHANGING lv_col.
      CONCATENATE lv_col lv_rowstr INTO lv_ref.
      lv_c1 = lv_val.

      IF lv_c1 = '='.
        " Formel: ohne '=', englische Namen, Komma als Trenner
        SHIFT lv_val LEFT BY 1 PLACES.
        REPLACE ALL OCCURRENCES OF 'SUMMENPRODUKT(' IN lv_val WITH 'SUMPRODUCT('.
        REPLACE ALL OCCURRENCES OF 'SUMME(' IN lv_val WITH 'SUM('.
        REPLACE ALL OCCURRENCES OF ';' IN lv_val WITH ','.
        PERFORM xml_escape CHANGING lv_val.
        CONCATENATE `<c r="` lv_ref `"` lv_style `><f>` lv_val `</f></c>` INTO lv_cell.
      ELSE.
        PERFORM ist_zahl USING lv_val CHANGING lv_isnum.
        READ TABLE lt_txtcol WITH KEY table_line = lv_colnr TRANSPORTING NO FIELDS.  " FIX 22
        IF sy-subrc = 0.
          lv_isnum = abap_false.
        ENDIF.
        IF lv_isnum = abap_true.
          CONCATENATE `<c r="` lv_ref `"` lv_style `><v>` lv_val `</v></c>` INTO lv_cell.
        ELSE.
          PERFORM xml_escape CHANGING lv_val.
          CONCATENATE `<c r="` lv_ref `"` lv_style ` t="inlineStr"><is><t xml:space="preserve">`
                      lv_val `</t></is></c>` INTO lv_cell.
        ENDIF.
      ENDIF.
      APPEND lv_cell TO lt_xml.
    ENDLOOP.

    APPEND `</row>` TO lt_xml.
  ENDLOOP.

  APPEND `</sheetData></worksheet>` TO lt_xml.
  CLEAR lv_part.
  CONCATENATE LINES OF lt_xml INTO lv_part.
  FREE lt_xml.
  PERFORM xlsx_add USING lo_zip `xl/worksheets/sheet1.xml` lv_part.

  CALL METHOD lo_zip->save
    RECEIVING
      zip = pv_zip.

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM xlsx_add (FIX 21): Text als UTF-8 in die ZIP-Datei legen
*&---------------------------------------------------------------------*
FORM xlsx_add USING po_zip  TYPE REF TO cl_abap_zip
                    pv_name TYPE string
                    pv_text TYPE string.

  DATA: lv_xs TYPE xstring.

  CALL METHOD cl_abap_codepage=>convert_to
    EXPORTING
      source   = pv_text
      codepage = `UTF-8`
    RECEIVING
      result   = lv_xs.

  CALL METHOD po_zip->add
    EXPORTING
      name    = pv_name
      content = lv_xs.

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM ist_zahl (FIX 21): wie der fruehere Text-Import -?Ziffern[.Ziffern]
*&---------------------------------------------------------------------*
FORM ist_zahl USING    pv_val   TYPE string
              CHANGING pv_isnum TYPE abap_bool.

  DATA: lv_tmp TYPE string,
        lv_c1  TYPE c LENGTH 1,
        lv_cl  TYPE c LENGTH 1,
        lv_off TYPE i,
        lv_cnt TYPE i.

  pv_isnum = abap_false.
  CHECK pv_val CO '0123456789.-'.

  lv_tmp = pv_val.
  lv_c1 = lv_tmp.
  IF lv_c1 = '-'.
    SHIFT lv_tmp LEFT BY 1 PLACES.
  ENDIF.
  CHECK lv_tmp IS NOT INITIAL.
  CHECK lv_tmp CO '0123456789.'.

  FIND ALL OCCURRENCES OF '.' IN lv_tmp MATCH COUNT lv_cnt.
  CHECK lv_cnt <= 1.
  lv_c1 = lv_tmp.
  lv_off = strlen( lv_tmp ) - 1.
  lv_cl = lv_tmp+lv_off(1).
  CHECK lv_c1 <> '.' AND lv_cl <> '.'.

  pv_isnum = abap_true.

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM xml_escape (FIX 21): & < > fuer XML maskieren
*&---------------------------------------------------------------------*
FORM xml_escape CHANGING pv_val TYPE string.

  REPLACE ALL OCCURRENCES OF '&' IN pv_val WITH '&amp;'.
  REPLACE ALL OCCURRENCES OF '<' IN pv_val WITH '&lt;'.
  REPLACE ALL OCCURRENCES OF '>' IN pv_val WITH '&gt;'.

ENDFORM.

*&---------------------------------------------------------------------*
*& FORM download_background
*&---------------------------------------------------------------------*
FORM download_background.

  DATA: lv_file TYPE string,
        lv_line TYPE string.

  CONCATENATE '/usr/sap/trans/zlo03_analyse_' sy-uname '.csv' INTO lv_file.

  OPEN DATASET lv_file FOR OUTPUT IN TEXT MODE ENCODING DEFAULT.
  IF sy-subrc <> 0.
    WRITE: / 'Fehler beim Öffnen:', lv_file.
    RETURN.
  ENDIF.

  LOOP AT gt_excel INTO lv_line.
    TRANSFER lv_line TO lv_file.
  ENDLOOP.

  CLOSE DATASET lv_file.

  WRITE: / 'Datei erstellt:', lv_file.

ENDFORM.
