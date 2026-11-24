*&---------------------------------------------------------------------*
*& Report ZM_LZCODE20_OPT
*& Hochoptimierte Stuecklistenanalyse Top-Down / Bottom-Up
*& Kompatibel mit aelteren SAP-Versionen (kein Inline-DATA im neuen Code)
*&---------------------------------------------------------------------*
*& FIXES APPLIED (Altbestand):
*&   FIX 1: convert_menge - Rundung auf dec=0 entfernt
*&   FIX 2: DELETE ADJACENT DUPLICATES entfernt
*&   FIX 4: Textpositionen (Typ T) werden angezeigt
*&   FIX 5: convert_menge - leere Ziel-Einheit abgefangen
*&
*& FIXES 27.07.2026 (Kohler / Analyse Mail Michel-Moltisanti 23.07.2026):
*&   FIX 10: MENGE ist CHAR40 -> benutzerabhaengige Dezimalinterpretation.
*&           Deterministischer Parser (FORM parse_menge_str).
*&   FIX 11: KOMPNR (Datenelement SOBJID, KEIN Konvertierungsexit) wurde
*&           direkt gegen MARA-MATNR / ZMD04_CALC-MATNR (Exit MATN1)
*&           verglichen. Numerische Komponentennummern liefen dadurch ins
*&           Leere. Normalisierung ueber FORM norm_matnr, Datenbank-
*&           zugriffe zusaetzlich ueber beide Schreibweisen.
*&   FIX 12: KTAB wurde nur beim ERSTEN Treffer gefuellt. War das eine
*&           Textposition, blieben alle Kennzahlen 0 (u.a. offene MK).
*&           -> echte Position ueberschreibt, zusaetzlich Nachlauf
*&           FORM refresh_ktab_kennzahlen (reihenfolgeunabhaengig).
*&   FIX 13: CLEAR vor READ TABLE ... INTO ls_stamm_v (Altwert-Leck).
*&   FIX 14: DELETE gt_ktab WHERE maktx IS INITIAL -> WHERE postyp = 'T'.
*&           Materialien ohne MAKT-Satz in sy-langu wurden still geloescht.
*&   FIX 15: Exklusivitaet betrachtete nur MTART = 'FERT'. Verwendungen
*&           ueber HALB-Baugruppen fuehrten zu falschem 'X'.
*&           Neuer dritter Zustand '?' = nicht entscheidbar.
*&   FIX 16: sy-subrc-Ketten nach zwei aufeinanderfolgenden READ TABLE.
*&   FIX 17: Werk ist Selektionsparameter statt Konstante.
*&   FIX 18: Diagnosemodus p_diag zur Klaerung der offenen Fragen.
*&
*& OFFEN / NICHT GEFIXT:
*&   - KOM_MSTAE wird als Elternmaterial interpretiert, enthaelt laut
*&     Datenbestand aber einen Materialstatus. Spalte "Elternmaterial"
*&     bleibt dadurch leer. Quellfeld mit Fachbereich klaeren.
*&     Siehe FORM load_elternmaterial_cache.
*&   - Zeilen 2 und 3 der Excel-Ausgabe sind kuerzer als die Datenzeilen.
*&     Aktuell folgenlos, absichtlich unveraendert gelassen.
*&---------------------------------------------------------------------*
REPORT zm_lzcode20_opt LINE-SIZE 255.


*&---------------------------------------------------------------------*
*& Typen fuer optimierte Verarbeitung
*&---------------------------------------------------------------------*
TYPES:
  " Einfacher Materialnummer-Typ fuer FOR ALL ENTRIES
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
    kompnr_norm   TYPE matnr,          " FIX 11: normalisierte Komponente
  END OF ty_powerbi,

  " VTAB: Spalten-Materialien
  BEGIN OF ty_vtab,
    matnr TYPE matnr,
    meins TYPE meins,
    verbr TYPE menge_d,
    sp    TYPE char3,
    mstae TYPE mstae,
  END OF ty_vtab,

  " Fuer ZMD04_CALC Cache
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

    endbestand      TYPE menge_d,
    stueckkosten    TYPE p LENGTH 11 DECIMALS 2,
    wert_feste_zug  TYPE p LENGTH 11 DECIMALS 2,
    wert_gepl_zug   TYPE p LENGTH 11 DECIMALS 2,
    wert_feste_abg  TYPE p LENGTH 11 DECIMALS 2,
    wert_gepl_abg   TYPE p LENGTH 11 DECIMALS 2,
    wert_endbestand TYPE p LENGTH 11 DECIMALS 2,

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
    postyp          TYPE postp,
  END OF ty_ktab,

  " MTAB: Mengen-Zuordnung
  BEGIN OF ty_mtab,
    matnr TYPE matnr,
    idnrk TYPE matnr,
    menge TYPE menge_d,
  END OF ty_mtab,

  " Fuer Exklusivitaets-Berechnung
  BEGIN OF ty_exkl,
    kompnr   TYPE matnr,
    count    TYPE i,
    exklusiv TYPE char1,
  END OF ty_exkl,

  " Fuer Bulk-Verbrauchsermittlung
  BEGIN OF ty_mver,
    matnr TYPE matnr,
    gsv   TYPE menge_d,
  END OF ty_mver,

  " Fuer MD04-Daten
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
    mtart       TYPE mtart,
    has_bom     TYPE char1,
  END OF ty_stamm,

  " Fuer Stammdaten-Abfrage
  BEGIN OF ty_stamm_raw,
    matnr       TYPE matnr,
    maktx       TYPE maktx,
    meins       TYPE meins,
    mstae       TYPE mstae,
    mstav       TYPE mstav,
    lvorm       TYPE lvorm,
    disst       TYPE disst,
    mtart       TYPE mtart,
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
  END OF ty_stamm_raw,

  " Fuer BOM-Pruefung
  BEGIN OF ty_bom_raw,
    matnr TYPE matnr,
  END OF ty_bom_raw,

  " Fuer Exklusivitaet-Zaehlung
  BEGIN OF ty_count_raw,
    kompnr TYPE matnr,
    count  TYPE i,
  END OF ty_count_raw,

  " Fuer weitere VKNRs pro Komponente
  BEGIN OF ty_weitere_vknr,
    kompnr TYPE matnr,
    vknr   TYPE matnr,
    verbr  TYPE menge_d,
  END OF ty_weitere_vknr.

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
  tt_bom_raw    TYPE STANDARD TABLE OF ty_bom_raw WITH DEFAULT KEY,
  tt_count_raw  TYPE STANDARD TABLE OF ty_count_raw WITH DEFAULT KEY,
  tt_weit_vknr  TYPE STANDARD TABLE OF ty_weitere_vknr WITH DEFAULT KEY.


*&---------------------------------------------------------------------*
*& Globale Daten
*&---------------------------------------------------------------------*
DATA:
  " Haupttabellen
  gt_vtab         TYPE tt_vtab,
  gt_ktab         TYPE tt_ktab,
  gt_mtab         TYPE tt_mtab,

  gt_weitere_vknr TYPE tt_weit_vknr,
  gv_max_weitere  TYPE i,
  gt_eltern_cache TYPE HASHED TABLE OF ty_mtab WITH UNIQUE KEY matnr idnrk,

  " Cache-Tabellen
  gt_stamm_cache  TYPE tt_stamm,
  gt_exkl_cache   TYPE tt_exkl,
  gt_md04_cache   TYPE tt_md04,
  gt_verbr_cache  TYPE tt_mver,
  gt_md04_full_cache TYPE tt_md04_full,

  " Excel-Ausgabe
  gt_excel        TYPE TABLE OF string,

  " Hilfsvariablen
  gv_datum_von    TYPE datum,
  gv_datum_bis    TYPE datum,
  gv_sp_von       TYPE char3,
  gv_sp_bis       TYPE char3,
  gv_sp_sum       TYPE char3,
  gv_sp_vbr       TYPE char3,
  gv_line         TYPE i,
  gv_tab          TYPE c LENGTH 1.

" FIX 18: Zaehler fuer den Diagnosemodus
DATA:
  gv_d_powerbi    TYPE i,
  gv_d_lost_stamm TYPE i,
  gv_d_lost_lvorm TYPE i,
  gv_d_keyfail    TYPE i.


*&---------------------------------------------------------------------*
*& Selektionsbild
*&---------------------------------------------------------------------*
TABLES: mara.

SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE TEXT-s01.
  SELECT-OPTIONS: s_typcd FOR mara-zztyp_f4,
                  s_matnr FOR mara-matnr.
  " FIX 17: Werk war hart als Konstante '1100' verdrahtet
  PARAMETERS: p_werks TYPE werks_d DEFAULT '1100' OBLIGATORY.
SELECTION-SCREEN END OF BLOCK b1.

SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE TEXT-s02.
  PARAMETERS: p_topd RADIOBUTTON GROUP g01 DEFAULT 'X',
              p_botu RADIOBUTTON GROUP g01.
SELECTION-SCREEN END OF BLOCK b2.

SELECTION-SCREEN BEGIN OF BLOCK b3 WITH FRAME TITLE TEXT-s03.
  PARAMETERS: p_days  TYPE i DEFAULT 360.
  PARAMETERS: p_wvknr AS CHECKBOX DEFAULT 'X'.
  PARAMETERS: p_trans AS CHECKBOX DEFAULT ''.
  PARAMETERS: p_txtpo AS CHECKBOX DEFAULT ''.
  PARAMETERS: p_lvorm AS CHECKBOX DEFAULT ''.
  " FIX 18: Diagnosemodus - erzeugt KEINE Excel-Datei
  PARAMETERS: p_diag  AS CHECKBOX DEFAULT ''.
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

    " Wildcard-Handling: * oder + -> CP-Option
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

  " FIX 18: Diagnosemodus - Analyse ausgeben, kein Download
  IF p_diag = 'X'.
    PERFORM run_diagnose.
    RETURN.
  ENDIF.

  IF gt_ktab IS INITIAL.
    MESSAGE 'Keine Daten gefunden' TYPE 'S' DISPLAY LIKE 'W'.
    RETURN.
  ENDIF.

  PERFORM load_elternmaterial_cache.
  PERFORM ermittle_weitere_vknr.
  PERFORM create_excel_output.
  PERFORM ausgabe_weitere_vknr.

  IF p_trans = 'X'.
    PERFORM ausgabe_spalten_details.
  ENDIF.

  IF sy-batch = 'X'.
    PERFORM download_background.
  ELSE.
    PERFORM download_dialog.
  ENDIF.


*&---------------------------------------------------------------------*
*& FORM norm_matnr
*& FIX 11
*& KOMPNR / KOM_MSTAE / MAT_MSTAV in ZPOWERBI_VC_TXT sind mit dem
*& Datenelement SOBJID typisiert und haben damit KEINEN Konvertierungs-
*& exit. MARA-MATNR und ZMD04_CALC-MATNR nutzen MATN1. Rein numerische
*& Materialnummern werden dort mit fuehrenden Nullen abgelegt, in
*& ZPOWERBI_VC_TXT je nach erzeugendem Programm dagegen moeglicherweise
*& ohne. Jeder direkte Vergleich lief dann ins Leere.
*&
*& CONVERSION_EXIT_MATN1_INPUT ist idempotent: bereits interne Werte
*& kommen unveraendert zurueck. Der Aufruf ist damit in beiden Faellen
*& unschaedlich.
*&---------------------------------------------------------------------*
FORM norm_matnr
  USING    pv_raw   TYPE any
  CHANGING pv_matnr TYPE matnr.

  DATA: lv_in TYPE matnr.

  CLEAR pv_matnr.
  lv_in = pv_raw.

  IF lv_in IS INITIAL.
    RETURN.
  ENDIF.

  CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
    EXPORTING
      input        = lv_in
    IMPORTING
      output       = pv_matnr
    EXCEPTIONS
      length_error = 1
      OTHERS       = 2.

  IF sy-subrc <> 0.
    " Konvertierung nicht moeglich -> Rohwert weiterverwenden
    pv_matnr = lv_in.
  ENDIF.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM denorm_matnr
*& FIX 11 - interne Schreibweise -> externe (ohne fuehrende Nullen)
*&---------------------------------------------------------------------*
FORM denorm_matnr
  USING    pv_matnr TYPE matnr
  CHANGING pv_ext   TYPE matnr.

  CLEAR pv_ext.

  IF pv_matnr IS INITIAL.
    RETURN.
  ENDIF.

  CALL FUNCTION 'CONVERSION_EXIT_MATN1_OUTPUT'
    EXPORTING
      input  = pv_matnr
    IMPORTING
      output = pv_ext.

  CONDENSE pv_ext NO-GAPS.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM add_both_forms
*& FIX 11
*& Haengt eine Materialnummer in interner UND externer Schreibweise an
*& eine Treibertabelle an. Damit trifft ein FOR ALL ENTRIES auf KOMPNR
*& unabhaengig davon, in welchem Format die Quelltabelle gefuellt wurde.
*& Fuer alphanumerische Nummern sind beide Formen identisch, das
*& anschliessende DELETE ADJACENT DUPLICATES entfernt den Doppler.
*&---------------------------------------------------------------------*
FORM add_both_forms
  USING    pv_matnr TYPE matnr
  CHANGING pt_list  TYPE tt_matnr_list.

  DATA: ls_line TYPE ty_matnr_line,
        lv_ext  TYPE matnr.

  CHECK pv_matnr IS NOT INITIAL.

  ls_line-matnr = pv_matnr.
  APPEND ls_line TO pt_list.

  PERFORM denorm_matnr USING pv_matnr CHANGING lv_ext.

  IF lv_ext IS NOT INITIAL AND lv_ext <> pv_matnr.
    ls_line-matnr = lv_ext.
    APPEND ls_line TO pt_list.
  ENDIF.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM build_komp_range
*& FIX 11 - Range ueber beide Schreibweisen einer Komponente
*&---------------------------------------------------------------------*
FORM build_komp_range
  USING    pv_matnr TYPE matnr
  CHANGING pt_range TYPE ANY TABLE.

  DATA: lv_ext TYPE matnr.

  DATA: BEGIN OF ls_rng,
          sign   TYPE c LENGTH 1,
          option TYPE c LENGTH 2,
          low    TYPE matnr,
          high   TYPE matnr,
        END OF ls_rng.

  ls_rng-sign   = 'I'.
  ls_rng-option = 'EQ'.
  ls_rng-low    = pv_matnr.
  INSERT ls_rng INTO TABLE pt_range.

  PERFORM denorm_matnr USING pv_matnr CHANGING lv_ext.

  IF lv_ext IS NOT INITIAL AND lv_ext <> pv_matnr.
    ls_rng-low = lv_ext.
    INSERT ls_rng INTO TABLE pt_range.
  ENDIF.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM parse_menge_str
*& FIX 10
*& ZPOWERBI_VC_TXT-MENGE ist CHAR40 (Datenelement SOBJID) und enthaelt
*& Werte wie '1.000'. Die bisherige Zuweisung
*&     lv_menge_in = lv_str.
*& interpretiert den String nach der Dezimaldarstellung des Benutzers
*& (SU3). Bei Einstellung 1.234.567,89 wird '1.000' zu TAUSEND, bei
*& 1,234,567.89 zu EINS - derselbe Report liefert je nach Benutzer
*& Mengen um Faktor 1000 daneben, ohne jede Fehlermeldung.
*&
*& Dieser Parser ist benutzerunabhaengig:
*&   - der RECHTESTE Trenner ('.' oder ',') ist der Dezimaltrenner
*&   - alle uebrigen Trenner gelten als Tausendertrenner und entfallen
*&   - Ganzzahl- und Nachkommateil werden getrennt konvertiert; reine
*&     Ziffernfolgen sind eindeutig
*&---------------------------------------------------------------------*
FORM parse_menge_str
  USING    pv_str TYPE any
  CHANGING pv_num TYPE menge_d.

  DATA: lv_str    TYPE string,
        lv_int    TYPE string,
        lv_frac   TYPE string,
        lv_neg    TYPE c LENGTH 1,
        lv_len    TYPE i,
        lv_i      TYPE i,
        lv_dec    TYPE i,
        lv_char   TYPE c LENGTH 1,
        lv_int_p  TYPE p LENGTH 16 DECIMALS 0,
        lv_frac_p TYPE p LENGTH 16 DECIMALS 0.

  CLEAR pv_num.

  lv_str = pv_str.
  CONDENSE lv_str NO-GAPS.

  IF lv_str IS INITIAL.
    RETURN.
  ENDIF.

  " --- Vorzeichen (fuehrend oder nachgestellt) ---
  CLEAR lv_neg.
  IF lv_str CS '-'.
    lv_neg = 'X'.
  ENDIF.
  REPLACE ALL OCCURRENCES OF '-' IN lv_str WITH ''.
  REPLACE ALL OCCURRENCES OF '+' IN lv_str WITH ''.

  IF lv_str IS INITIAL.
    RETURN.
  ENDIF.

  " --- rechtesten Trenner suchen ---
  lv_len = strlen( lv_str ).
  lv_dec = -1.
  lv_i   = lv_len - 1.

  WHILE lv_i >= 0.
    lv_char = lv_str+lv_i(1).
    IF lv_char = '.' OR lv_char = ','.
      lv_dec = lv_i.
      EXIT.
    ENDIF.
    lv_i = lv_i - 1.
  ENDWHILE.

  IF lv_dec >= 0.
    IF lv_dec > 0.
      lv_int = lv_str(lv_dec).
    ELSE.
      lv_int = '0'.
    ENDIF.
    lv_i = lv_dec + 1.
    IF lv_i < lv_len.
      lv_frac = lv_str+lv_i.
    ELSE.
      CLEAR lv_frac.
    ENDIF.
  ELSE.
    lv_int = lv_str.
    CLEAR lv_frac.
  ENDIF.

  " --- Tausendertrenner entfernen ---
  REPLACE ALL OCCURRENCES OF '.' IN lv_int WITH ''.
  REPLACE ALL OCCURRENCES OF ',' IN lv_int WITH ''.
  REPLACE ALL OCCURRENCES OF '.' IN lv_frac WITH ''.
  REPLACE ALL OCCURRENCES OF ',' IN lv_frac WITH ''.

  IF lv_int IS INITIAL.
    lv_int = '0'.
  ENDIF.

  " --- Nachkommateil auf genau 3 Stellen bringen (MENGE_D hat 3) ---
  lv_len = strlen( lv_frac ).
  IF lv_len > 3.
    lv_frac = lv_frac(3).
  ELSE.
    WHILE lv_len < 3.
      CONCATENATE lv_frac '0' INTO lv_frac.
      lv_len = lv_len + 1.
    ENDWHILE.
  ENDIF.

  " --- Plausibilitaet: nur Ziffern ---
  IF lv_int CN '0123456789'.
    RETURN.
  ENDIF.
  IF lv_frac CN '0123456789'.
    RETURN.
  ENDIF.

  lv_int_p  = lv_int.
  lv_frac_p = lv_frac.

  pv_num = lv_int_p + lv_frac_p / 1000.

  IF lv_neg = 'X'.
    pv_num = pv_num * -1.
  ENDIF.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM convert_menge
*& FIX 1 : Rundung auf dec=0 entfernt
*& FIX 5 : Leere Ziel-Einheit abgefangen
*& FIX 10: deterministischer Parser statt impliziter Zuweisung
*&---------------------------------------------------------------------*
FORM convert_menge
  USING    pv_str       TYPE any
           pv_meins_in  TYPE meins
           pv_meins_out TYPE meins
  CHANGING pv_num       TYPE menge_d.

  DATA: lv_menge_in  TYPE menge_d,
        lv_menge_out TYPE menge_d.

  CLEAR pv_num.

  PERFORM parse_menge_str USING pv_str CHANGING lv_menge_in.

  IF lv_menge_in IS INITIAL.
    RETURN.
  ENDIF.

  IF pv_meins_in <> pv_meins_out
     AND pv_meins_in  IS NOT INITIAL
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

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM process_main
*&---------------------------------------------------------------------*
FORM process_main.

  DATA: lt_powerbi    TYPE tt_powerbi,
        lt_matnr_sel  TYPE tt_matnr_list,
        lt_komp_drv   TYPE tt_matnr_list,
        lt_all_matnr  TYPE tt_matnr_list,
        lt_mara_sel   TYPE STANDARD TABLE OF matnr,
        ls_matnr_line TYPE ty_matnr_line,
        ls_vtab       TYPE ty_vtab,
        ls_ktab       TYPE ty_ktab,
        ls_ktab_exist TYPE ty_ktab,
        ls_mtab       TYPE ty_mtab,
        ls_mtab_exist TYPE ty_mtab,
        ls_stamm_v    TYPE ty_stamm,
        ls_md04_full  TYPE ty_md04_full,
        ls_powerbi    TYPE ty_powerbi,
        lv_menge      TYPE menge_d,
        lv_meins_komp TYPE meins,
        lv_matnr      TYPE matnr,
        lv_subrc      TYPE sy-subrc.

  DATA: lt_matnr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

  FIELD-SYMBOLS: <fs_pbi> TYPE ty_powerbi.

  " ===================================================================
  " SCHRITT 1: Materialnummern aus Selektion
  " ===================================================================
  DATA: lr_lvorm TYPE RANGE OF mara-lvorm,
        ls_lvorm LIKE LINE OF lr_lvorm.

  IF p_lvorm = ' '.
    ls_lvorm-sign   = 'I'.
    ls_lvorm-option = 'EQ'.
    ls_lvorm-low    = ''.
    APPEND ls_lvorm TO lr_lvorm.
  ENDIF.

  IF s_typcd IS NOT INITIAL.
    SELECT matnr FROM mara
      INTO TABLE lt_mara_sel
      WHERE zztyp_f4 IN s_typcd
        AND matnr    IN s_matnr
        AND lvorm    IN lr_lvorm.
  ELSEIF s_matnr IS NOT INITIAL.
    SELECT matnr FROM mara
      INTO TABLE lt_mara_sel
      WHERE matnr IN s_matnr
        AND lvorm IN lr_lvorm.
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
    " MATNR ist beidseitig MATN1-typisiert -> direkter Vergleich ok
    SELECT id stufe kompnr menge mengeneinheit baugruppe matnr
           kom_mstae postyp
      FROM zpowerbi_vc_txt
      INTO CORRESPONDING FIELDS OF TABLE lt_powerbi
      FOR ALL ENTRIES IN lt_matnr_sel
      WHERE matnr         = lt_matnr_sel-matnr
        AND menge        <> ''
        AND mengeneinheit <> ''.
  ELSE.
    "*** FIX 11: KOMPNR hat keinen Konvertierungsexit.
    "*** Treiber ueber beide Schreibweisen aufbauen.
    LOOP AT lt_matnr_sel INTO ls_matnr_line.
      PERFORM add_both_forms USING ls_matnr_line-matnr
                             CHANGING lt_komp_drv.
    ENDLOOP.
    SORT lt_komp_drv BY matnr.
    DELETE ADJACENT DUPLICATES FROM lt_komp_drv COMPARING matnr.

    SELECT id stufe kompnr menge mengeneinheit baugruppe matnr
           kom_mstae postyp
      FROM zpowerbi_vc_txt
      INTO CORRESPONDING FIELDS OF TABLE lt_powerbi
      FOR ALL ENTRIES IN lt_komp_drv
      WHERE kompnr        = lt_komp_drv-matnr
        AND menge        <> ''
        AND mengeneinheit <> ''.
  ENDIF.

  gv_d_powerbi = lines( lt_powerbi ).

  IF lt_powerbi IS INITIAL.
    RETURN.
  ENDIF.

  "*** FIX 11: Komponentennummer einmal zentral normalisieren.
  "*** Ab hier arbeitet das gesamte Programm mit kompnr_norm.
  LOOP AT lt_powerbi ASSIGNING <fs_pbi>.
    PERFORM norm_matnr USING <fs_pbi>-kompnr
                       CHANGING <fs_pbi>-kompnr_norm.
  ENDLOOP.

  "=========================================================
  " FIX 2: DELETE ADJACENT DUPLICATES bleibt entfernt.
  " COLLECT auf gt_mtab summiert Mehrfachverwendungen korrekt.
  "=========================================================

  " ===================================================================
  " SCHRITT 3: Alle Materialnummern sammeln
  " ===================================================================
  LOOP AT lt_powerbi INTO ls_powerbi.
    INSERT ls_powerbi-matnr       INTO TABLE lt_matnr_temp.
    INSERT ls_powerbi-kompnr_norm INTO TABLE lt_matnr_temp.
  ENDLOOP.

  LOOP AT lt_matnr_sel INTO ls_matnr_line.
    INSERT ls_matnr_line-matnr INTO TABLE lt_matnr_temp.
  ENDLOOP.

  DELETE lt_matnr_temp WHERE table_line IS INITIAL.

  LOOP AT lt_matnr_temp INTO lv_matnr.
    ls_matnr_line-matnr = lv_matnr.
    APPEND ls_matnr_line TO lt_all_matnr.
  ENDLOOP.

  " ===================================================================
  " SCHRITT 4-7: Caches laden
  " ===================================================================
  PERFORM load_stammdaten_cache USING lt_all_matnr.
  PERFORM load_md04_bulk        USING lt_all_matnr.
  PERFORM load_verbrauch_bulk   USING lt_all_matnr.

  IF p_topd = 'X'.
    PERFORM load_exclusivity_topdown USING lt_powerbi.
  ELSE.
    PERFORM load_global_exclusivity USING lt_powerbi.
  ENDIF.

  " ===================================================================
  " SCHRITT 8: Haupttabellen befuellen
  " ===================================================================
  LOOP AT lt_powerbi INTO ls_powerbi.

    "*** FIX 13: CLEAR vor READ - sonst steht bei erfolglosem READ
    "*** noch der Satz des vorigen Schleifendurchlaufs in ls_stamm_v
    "*** und die Mengeneinheit des FALSCHEN Materials wird fuer die
    "*** Umrechnung verwendet.
    CLEAR ls_stamm_v.
    READ TABLE gt_stamm_cache INTO ls_stamm_v
      WITH TABLE KEY matnr = ls_powerbi-kompnr_norm.
    IF sy-subrc = 0.
      lv_meins_komp = ls_stamm_v-meins.
    ELSE.
      lv_meins_komp = ls_powerbi-mengeneinheit.
      gv_d_keyfail  = gv_d_keyfail + 1.
    ENDIF.

    PERFORM convert_menge
      USING    ls_powerbi-menge
               ls_powerbi-mengeneinheit
               lv_meins_komp
      CHANGING lv_menge.

    IF p_topd = 'X'.
      " ----------------- TOP-DOWN -----------------

      " VTAB: Kopfmaterialien (Spalten)
      READ TABLE gt_vtab WITH TABLE KEY matnr = ls_powerbi-matnr
        TRANSPORTING NO FIELDS.
      IF sy-subrc <> 0.
        CLEAR ls_vtab.
        ls_vtab-matnr = ls_powerbi-matnr.

        CLEAR ls_stamm_v.
        READ TABLE gt_stamm_cache INTO ls_stamm_v
          WITH TABLE KEY matnr = ls_powerbi-matnr.
        IF sy-subrc = 0.
          ls_vtab-meins = ls_stamm_v-meins.
          ls_vtab-mstae = ls_stamm_v-mstae.
        ENDIF.

        CLEAR ls_md04_full.
        READ TABLE gt_md04_full_cache INTO ls_md04_full
          WITH TABLE KEY matnr = ls_powerbi-matnr.
        IF sy-subrc = 0.
          ls_vtab-verbr = ls_md04_full-verbr.
        ENDIF.

        INSERT ls_vtab INTO TABLE gt_vtab.
      ENDIF.

      "*** FIX 12: KTAB wurde bisher NUR beim ersten Treffer gefuellt.
      "*** War der erste Treffer einer Komponente eine Textposition,
      "*** stieg fill_ktab wegen postyp='T' sofort mit RETURN aus und
      "*** saemtliche Kennzahlen blieben 0 - auch offene MK. Spaetere
      "*** echte Positionen derselben Komponente korrigierten das nie.
      READ TABLE gt_ktab INTO ls_ktab_exist
        WITH TABLE KEY idnrk = ls_powerbi-kompnr_norm.
      IF sy-subrc <> 0.
        PERFORM fill_ktab USING ls_powerbi-kompnr_norm
                                ls_powerbi-matnr
                                ls_powerbi-baugruppe
                                ls_powerbi-postyp
                          CHANGING ls_ktab.
        INSERT ls_ktab INTO TABLE gt_ktab.
      ELSEIF ls_ktab_exist-postyp = 'T' AND ls_powerbi-postyp <> 'T'.
        " echte Position gewinnt gegen Textposition
        PERFORM fill_ktab USING ls_powerbi-kompnr_norm
                                ls_powerbi-matnr
                                ls_powerbi-baugruppe
                                ls_powerbi-postyp
                          CHANGING ls_ktab.
        MODIFY TABLE gt_ktab FROM ls_ktab.
      ENDIF.

      " MTAB: Zuordnung
      IF ls_powerbi-postyp <> 'T' OR p_txtpo = 'X'.
        CLEAR ls_mtab.
        ls_mtab-matnr = ls_powerbi-matnr.
        ls_mtab-idnrk = ls_powerbi-kompnr_norm.
        ls_mtab-menge = lv_menge.
        COLLECT ls_mtab INTO gt_mtab.
      ENDIF.

    ELSE.
      " ----------------- BOTTOM-UP -----------------

      "*** FIX 13: CLEAR ergaenzt
      CLEAR ls_stamm_v.
      READ TABLE gt_stamm_cache INTO ls_stamm_v
        WITH TABLE KEY matnr = ls_powerbi-matnr.
      lv_subrc = sy-subrc.

      IF lv_subrc <> 0.
        gv_d_lost_stamm = gv_d_lost_stamm + 1.
        IF ls_powerbi-postyp <> 'T' OR p_txtpo <> 'X'.
          CONTINUE.
        ENDIF.
      ENDIF.

      IF ls_stamm_v-lvorm IS NOT INITIAL AND p_lvorm = ' '.
        gv_d_lost_lvorm = gv_d_lost_lvorm + 1.
        CONTINUE.
      ENDIF.

      " VTAB: Komponenten (Spalten)
      READ TABLE gt_vtab WITH TABLE KEY matnr = ls_powerbi-kompnr_norm
        TRANSPORTING NO FIELDS.
      IF sy-subrc <> 0.
        CLEAR ls_vtab.
        ls_vtab-matnr = ls_powerbi-kompnr_norm.

        CLEAR ls_stamm_v.
        READ TABLE gt_stamm_cache INTO ls_stamm_v
          WITH TABLE KEY matnr = ls_powerbi-kompnr_norm.
        IF sy-subrc = 0.
          ls_vtab-meins = ls_stamm_v-meins.
          ls_vtab-mstae = ls_stamm_v-mstae.
        ENDIF.

        INSERT ls_vtab INTO TABLE gt_vtab.
      ENDIF.

      "*** FIX 12: analog Top-Down
      READ TABLE gt_ktab INTO ls_ktab_exist
        WITH TABLE KEY idnrk = ls_powerbi-matnr.
      IF sy-subrc <> 0.
        PERFORM fill_ktab USING ls_powerbi-matnr
                                ls_powerbi-kompnr_norm
                                ''
                                ls_powerbi-postyp
                          CHANGING ls_ktab.
        INSERT ls_ktab INTO TABLE gt_ktab.
      ELSEIF ls_ktab_exist-postyp = 'T' AND ls_powerbi-postyp <> 'T'.
        PERFORM fill_ktab USING ls_powerbi-matnr
                                ls_powerbi-kompnr_norm
                                ''
                                ls_powerbi-postyp
                          CHANGING ls_ktab.
        MODIFY TABLE gt_ktab FROM ls_ktab.
      ENDIF.

      " MTAB: Zuordnung
      CLEAR ls_mtab.
      ls_mtab-matnr = ls_powerbi-kompnr_norm.
      ls_mtab-idnrk = ls_powerbi-matnr.

      READ TABLE gt_mtab INTO ls_mtab_exist
        WITH TABLE KEY matnr = ls_mtab-matnr
                       idnrk = ls_mtab-idnrk.
      IF sy-subrc = 0.
        ls_mtab-menge = ls_mtab_exist-menge + lv_menge.
        MODIFY TABLE gt_mtab FROM ls_mtab.
      ELSE.
        ls_mtab-menge = lv_menge.
        INSERT ls_mtab INTO TABLE gt_mtab.
      ENDIF.

    ENDIF.

  ENDLOOP.

  "*** FIX 12: Sicherheitsnetz - Kennzahlen fuer ALLE KTAB-Zeilen
  "*** reihenfolgeunabhaengig nachziehen.
  PERFORM refresh_ktab_kennzahlen.

  "=========================================================
  "*** FIX 14: bisher DELETE gt_ktab WHERE maktx IS INITIAL.
  "*** Das loeschte auch echte Materialien ohne MAKT-Satz in
  "*** sy-langu. Gemeint waren Textpositionen - der Positionstyp
  "*** steht im Satz und ist das eindeutige Kriterium.
  "=========================================================
  IF p_txtpo = ' '.
    DELETE gt_ktab WHERE postyp = 'T'.
  ENDIF.

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
*& FORM refresh_ktab_kennzahlen
*& FIX 12
*& Zieht Stammdaten und MD04-/MK-Kennzahlen fuer ALLE KTAB-Zeilen nach.
*& Damit ist das Ergebnis unabhaengig davon, in welcher Reihenfolge eine
*& Komponente in lt_powerbi zum ersten Mal auftaucht.
*& gt_ktab ist SORTED mit UNIQUE KEY idnrk - das Aendern von
*& Nichtschluesselfeldern ueber Feldsymbol ist zulaessig.
*&---------------------------------------------------------------------*
FORM refresh_ktab_kennzahlen.

  DATA: ls_md04_full TYPE ty_md04_full,
        ls_stamm     TYPE ty_stamm.

  FIELD-SYMBOLS: <fs_ktab> TYPE ty_ktab.

  LOOP AT gt_ktab ASSIGNING <fs_ktab>.

    " Echte Textpositionen bleiben bewusst ohne Kennzahlen
    IF <fs_ktab>-postyp = 'T'.
      CONTINUE.
    ENDIF.

    " --- Stammdaten nachziehen, falls beim ersten Treffer leer ---
    IF <fs_ktab>-meins IS INITIAL.
      CLEAR ls_stamm.
      READ TABLE gt_stamm_cache INTO ls_stamm
        WITH TABLE KEY matnr = <fs_ktab>-idnrk.
      IF sy-subrc = 0.
        <fs_ktab>-maktx       = ls_stamm-maktx.
        <fs_ktab>-meins       = ls_stamm-meins.
        <fs_ktab>-disst       = ls_stamm-disst.
        <fs_ktab>-mstae       = ls_stamm-mstae.
        <fs_ktab>-mstav       = ls_stamm-mstav.
        <fs_ktab>-zzlzcod     = ls_stamm-zzlzcod.
        <fs_ktab>-zzlzcodsort = ls_stamm-zzlzcodsort.
        <fs_ktab>-dismm       = ls_stamm-dismm.
        <fs_ktab>-minbe       = ls_stamm-minbe.
        <fs_ktab>-disls       = ls_stamm-disls.
        <fs_ktab>-bstfe       = ls_stamm-bstfe.
        <fs_ktab>-eisbe       = ls_stamm-eisbe.
        <fs_ktab>-beskz       = ls_stamm-beskz.
        <fs_ktab>-lbkum       = ls_stamm-lbkum.
        <fs_ktab>-verpr       = ls_stamm-verpr.
        <fs_ktab>-stprs       = ls_stamm-stprs.
        <fs_ktab>-peinh       = ls_stamm-peinh.
        <fs_ktab>-vprsv       = ls_stamm-vprsv.
        <fs_ktab>-salk3       = ls_stamm-salk3.

        IF ls_stamm-has_bom = 'X' AND ls_stamm-beskz <> 'F'.
          <fs_ktab>-baugr = 'X'.
        ENDIF.
      ENDIF.
    ENDIF.

    IF <fs_ktab>-peinh IS INITIAL.
      <fs_ktab>-peinh = 1.
    ENDIF.

    IF <fs_ktab>-stueckkosten IS INITIAL.
      IF <fs_ktab>-vprsv = 'V'.
        <fs_ktab>-stueckkosten = <fs_ktab>-verpr / <fs_ktab>-peinh.
      ELSE.
        <fs_ktab>-stueckkosten = <fs_ktab>-stprs / <fs_ktab>-peinh.
      ENDIF.
    ENDIF.

    " --- MD04 / MK immer aus dem Cache setzen ---
    CLEAR ls_md04_full.
    READ TABLE gt_md04_full_cache INTO ls_md04_full
      WITH TABLE KEY matnr = <fs_ktab>-idnrk.
    CHECK sy-subrc = 0.

    <fs_ktab>-labst     = ls_md04_full-labst.
    <fs_ktab>-feste_zug = ls_md04_full-feste_zug.
    <fs_ktab>-gepl_zug  = ls_md04_full-gepl_zug.
    <fs_ktab>-feste_abg = ls_md04_full-feste_abg.
    <fs_ktab>-gepl_abg  = ls_md04_full-gepl_abg.
    <fs_ktab>-verbr     = ls_md04_full-verbr.
    <fs_ktab>-omeng     = ls_md04_full-omeng.
    <fs_ktab>-owert     = ls_md04_full-owert.
    <fs_ktab>-mkmng     = ls_md04_full-mkmng.
    <fs_ktab>-omkwr     = ls_md04_full-omkwr.

    <fs_ktab>-endbestand = <fs_ktab>-labst
                         + <fs_ktab>-feste_zug
                         + <fs_ktab>-gepl_zug
                         - <fs_ktab>-feste_abg
                         - <fs_ktab>-gepl_abg.

    <fs_ktab>-wert_feste_zug  = <fs_ktab>-feste_zug  * <fs_ktab>-stueckkosten.
    <fs_ktab>-wert_gepl_zug   = <fs_ktab>-gepl_zug   * <fs_ktab>-stueckkosten.
    <fs_ktab>-wert_feste_abg  = <fs_ktab>-feste_abg  * <fs_ktab>-stueckkosten.
    <fs_ktab>-wert_gepl_abg   = <fs_ktab>-gepl_abg   * <fs_ktab>-stueckkosten.
    <fs_ktab>-wert_endbestand = <fs_ktab>-endbestand * <fs_ktab>-stueckkosten.

  ENDLOOP.

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

  SELECT
    m~matnr
    t~maktx
    m~meins
    m~mstae
    m~mstav
    m~lvorm
    m~disst
    m~mtart
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
  FROM mara AS m
  LEFT JOIN makt AS t ON t~matnr = m~matnr AND t~spras = sy-langu
  LEFT JOIN marc AS c ON c~matnr = m~matnr AND c~werks = p_werks
  LEFT JOIN mbew AS b ON b~matnr = m~matnr AND b~bwkey = p_werks
                                           AND b~bwtar = ''
  INTO TABLE lt_stamm_raw
  FOR ALL ENTRIES IN pt_matnr
  WHERE m~matnr = pt_matnr-matnr.

  LOOP AT lt_stamm_raw INTO ls_stamm_raw.
    CLEAR ls_stamm.
    ls_stamm-matnr       = ls_stamm_raw-matnr.
    ls_stamm-maktx       = ls_stamm_raw-maktx.
    ls_stamm-meins       = ls_stamm_raw-meins.
    ls_stamm-mstae       = ls_stamm_raw-mstae.
    ls_stamm-mstav       = ls_stamm_raw-mstav.
    ls_stamm-disst       = ls_stamm_raw-disst.
    ls_stamm-mtart       = ls_stamm_raw-mtart.
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
      AND werks = p_werks
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
*& FORM load_md04_bulk - liest aus vorberechneter ZMD04_CALC
*&---------------------------------------------------------------------*
FORM load_md04_bulk USING pt_matnr TYPE tt_matnr_list.

  DATA: lt_md04_db   TYPE STANDARD TABLE OF zmd04_calc,
        ls_md04_db   TYPE zmd04_calc,
        ls_md04      TYPE ty_md04,
        ls_md04_full TYPE ty_md04_full.

  CLEAR: gt_md04_cache, gt_md04_full_cache.

  CHECK pt_matnr IS NOT INITIAL.

  SELECT matnr werks labst feste_zug gepl_zug feste_abg gepl_abg
         verbr omeng owert mkmng omkwr exklusiv exkl_count
    FROM zmd04_calc
    INTO CORRESPONDING FIELDS OF TABLE lt_md04_db
    FOR ALL ENTRIES IN pt_matnr
    WHERE matnr = pt_matnr-matnr
      AND werks = p_werks.

  LOOP AT lt_md04_db INTO ls_md04_db.

    CLEAR ls_md04.
    ls_md04-matnr     = ls_md04_db-matnr.
    ls_md04-labst     = ls_md04_db-labst.
    ls_md04-feste_zug = ls_md04_db-feste_zug.
    ls_md04-gepl_zug  = ls_md04_db-gepl_zug.
    ls_md04-feste_abg = ls_md04_db-feste_abg.
    ls_md04-gepl_abg  = ls_md04_db-gepl_abg.
    INSERT ls_md04 INTO TABLE gt_md04_cache.

    CLEAR ls_md04_full.
    MOVE-CORRESPONDING ls_md04_db TO ls_md04_full.
    INSERT ls_md04_full INTO TABLE gt_md04_full_cache.

  ENDLOOP.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM load_verbrauch_bulk
*&---------------------------------------------------------------------*
FORM load_verbrauch_bulk USING pt_matnr TYPE tt_matnr_list.

  DATA: ls_verbr     TYPE ty_mver,
        ls_md04_full TYPE ty_md04_full.

  CLEAR gt_verbr_cache.

  LOOP AT gt_md04_full_cache INTO ls_md04_full.
    ls_verbr-matnr = ls_md04_full-matnr.
    ls_verbr-gsv   = ls_md04_full-verbr.
    INSERT ls_verbr INTO TABLE gt_verbr_cache.
  ENDLOOP.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM load_exclusivity_topdown
*& FIX 11: KOMPNR-Normalisierung
*& FIX 15: dritter Zustand '?' fuer nicht entscheidbare Faelle
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
        lt_vknr_list      TYPE tt_matnr_list,
        lt_kompnr_drv     TYPE tt_matnr_list,
        ls_matnr_line     TYPE ty_matnr_line,
        ls_usage          TYPE ty_vknr_usage,
        ls_vknr_mara      TYPE ty_vknr_mara.

  FIELD-SYMBOLS: <fs_usage> TYPE ty_vknr_usage.

  LOOP AT pt_powerbi INTO ls_powerbi.
    INSERT ls_powerbi-kompnr_norm INTO TABLE lt_kompnr_temp.
    INSERT ls_powerbi-matnr       INTO TABLE lt_sel_vknr.
  ENDLOOP.

  DELETE lt_kompnr_temp WHERE table_line IS INITIAL.

  IF lt_kompnr_temp IS INITIAL.
    RETURN.
  ENDIF.

  "*** FIX 11: Treiber ueber beide Schreibweisen
  LOOP AT lt_kompnr_temp INTO lv_kompnr.
    PERFORM add_both_forms USING lv_kompnr CHANGING lt_kompnr_drv.
  ENDLOOP.
  SORT lt_kompnr_drv BY matnr.
  DELETE ADJACENT DUPLICATES FROM lt_kompnr_drv COMPARING matnr.

  SELECT kompnr, matnr AS vknr
    FROM zpowerbi_vc_txt
    INTO TABLE @lt_all_usage
    FOR ALL ENTRIES IN @lt_kompnr_drv
    WHERE kompnr = @lt_kompnr_drv-matnr.

  "*** FIX 11: gelesene KOMPNR normalisieren
  LOOP AT lt_all_usage ASSIGNING <fs_usage>.
    PERFORM norm_matnr USING <fs_usage>-kompnr
                       CHANGING <fs_usage>-kompnr.
  ENDLOOP.

  SORT lt_all_usage BY kompnr vknr.
  DELETE ADJACENT DUPLICATES FROM lt_all_usage COMPARING kompnr vknr.

  LOOP AT lt_all_usage INTO ls_usage.
    INSERT ls_usage-vknr INTO TABLE lt_all_vknr.
  ENDLOOP.

  DELETE lt_all_vknr WHERE table_line IS INITIAL.

  IF lt_all_vknr IS NOT INITIAL.
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
    IF sy-subrc = 0 AND ls_vknr_mara-mtart = 'FERT'.
      APPEND ls_usage TO lt_all_usage_fert.
    ENDIF.
  ENDLOOP.

  LOOP AT lt_kompnr_temp INTO lv_kompnr.

    lv_is_exklusiv = 'X'.

    " --- Schritt 1: FERT-Eltern ausserhalb der Selektion ---
    LOOP AT lt_all_usage_fert INTO ls_usage WHERE kompnr = lv_kompnr.

      READ TABLE lt_sel_vknr WITH TABLE KEY table_line = ls_usage-vknr
        TRANSPORTING NO FIELDS.
      IF sy-subrc = 0.
        CONTINUE.
      ENDIF.

      READ TABLE lt_vknr_mara INTO ls_vknr_mara
        WITH TABLE KEY matnr = ls_usage-vknr.
      IF sy-subrc = 0 AND ls_vknr_mara-mstae = '99'.
        " auslaufendes Geraet zaehlt nicht als weitere Verwendung
        CONTINUE.
      ENDIF.

      lv_is_exklusiv = ''.
      EXIT.

    ENDLOOP.

    "*** FIX 15: Bisher wurden ausschliesslich FERT-Eltern geprueft.
    "*** Eine Komponente, die zusaetzlich in einer HALB-Baugruppe steckt
    "*** und ueber diese in nicht selektierten Geraeten laeuft, bekam
    "*** trotzdem ein 'X'. Beim Phase Out ist das die gefaehrliche
    "*** Fehlerrichtung: Mengenkontrakt gekuendigt, Teil weiter benoetigt.
    "*** Eine saubere Antwort braucht eine rekursive Aufloesung. Bis
    "*** dahin wird der Fall als '?' = manuell pruefen gekennzeichnet
    "*** statt als Exklusivitaet behauptet.
    IF lv_is_exklusiv = 'X'.
      LOOP AT lt_all_usage INTO ls_usage WHERE kompnr = lv_kompnr.

        READ TABLE lt_vknr_mara INTO ls_vknr_mara
          WITH TABLE KEY matnr = ls_usage-vknr.
        IF sy-subrc <> 0.
          CONTINUE.
        ENDIF.
        IF ls_vknr_mara-mtart = 'FERT'.
          CONTINUE.
        ENDIF.

        READ TABLE lt_sel_vknr WITH TABLE KEY table_line = ls_usage-vknr
          TRANSPORTING NO FIELDS.
        IF sy-subrc <> 0.
          lv_is_exklusiv = '?'.
          EXIT.
        ENDIF.

      ENDLOOP.
    ENDIF.

    CLEAR ls_exkl.
    ls_exkl-kompnr   = lv_kompnr.
    ls_exkl-exklusiv = lv_is_exklusiv.
    ls_exkl-count    = 0.
    INSERT ls_exkl INTO TABLE gt_exkl_cache.

  ENDLOOP.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM load_global_exclusivity
*& FIX 11: KOMPNR-Normalisierung
*&---------------------------------------------------------------------*
FORM load_global_exclusivity USING pt_powerbi TYPE tt_powerbi.

  TYPES: BEGIN OF ty_usage,
           kompnr TYPE matnr,
           matnr  TYPE matnr,
         END OF ty_usage.

  DATA: lt_kompnr_drv  TYPE tt_matnr_list,
        lt_usage       TYPE STANDARD TABLE OF ty_usage,
        lt_usage_uniq  TYPE HASHED TABLE OF ty_usage WITH UNIQUE KEY kompnr matnr,
        lt_count_temp  TYPE HASHED TABLE OF ty_count_raw WITH UNIQUE KEY kompnr,
        ls_count_raw   TYPE ty_count_raw,
        ls_exkl        TYPE ty_exkl,
        ls_powerbi     TYPE ty_powerbi,
        ls_usage       TYPE ty_usage,
        lv_kompnr      TYPE matnr.

  DATA: lt_kompnr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

  FIELD-SYMBOLS: <fs_usage> TYPE ty_usage.

  LOOP AT pt_powerbi INTO ls_powerbi.
    INSERT ls_powerbi-kompnr_norm INTO TABLE lt_kompnr_temp.
  ENDLOOP.

  DELETE lt_kompnr_temp WHERE table_line IS INITIAL.
  CHECK lt_kompnr_temp IS NOT INITIAL.

  LOOP AT lt_kompnr_temp INTO lv_kompnr.
    PERFORM add_both_forms USING lv_kompnr CHANGING lt_kompnr_drv.
  ENDLOOP.
  SORT lt_kompnr_drv BY matnr.
  DELETE ADJACENT DUPLICATES FROM lt_kompnr_drv COMPARING matnr.

  SELECT kompnr matnr
    FROM zpowerbi_vc_txt
    INTO CORRESPONDING FIELDS OF TABLE lt_usage
    FOR ALL ENTRIES IN lt_kompnr_drv
    WHERE kompnr = lt_kompnr_drv-matnr.

  LOOP AT lt_usage ASSIGNING <fs_usage>.
    PERFORM norm_matnr USING <fs_usage>-kompnr
                       CHANGING <fs_usage>-kompnr.
  ENDLOOP.

  LOOP AT lt_usage INTO ls_usage.
    INSERT ls_usage INTO TABLE lt_usage_uniq.
  ENDLOOP.

  LOOP AT lt_usage_uniq INTO ls_usage.
    READ TABLE lt_count_temp INTO ls_count_raw
      WITH TABLE KEY kompnr = ls_usage-kompnr.
    IF sy-subrc = 0.
      ls_count_raw-count = ls_count_raw-count + 1.
      MODIFY TABLE lt_count_temp FROM ls_count_raw.
    ELSE.
      CLEAR ls_count_raw.
      ls_count_raw-kompnr = ls_usage-kompnr.
      ls_count_raw-count  = 1.
      INSERT ls_count_raw INTO TABLE lt_count_temp.
    ENDIF.
  ENDLOOP.

  LOOP AT lt_count_temp INTO ls_count_raw.
    CLEAR ls_exkl.
    ls_exkl-kompnr   = ls_count_raw-kompnr.
    ls_exkl-count    = ls_count_raw-count.
    ls_exkl-exklusiv = ''.
    INSERT ls_exkl INTO TABLE gt_exkl_cache.
  ENDLOOP.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM fill_ktab
*& FIX 11: Zugriff auf zpowerbi_vc_txt ueber beide Schreibweisen
*&---------------------------------------------------------------------*
FORM fill_ktab
  USING    pv_idnrk     TYPE matnr
           pv_matnr     TYPE matnr
           pv_baugruppe TYPE any
           pv_postyp    TYPE postp
  CHANGING ps_ktab      TYPE ty_ktab.

  DATA: ls_stamm     TYPE ty_stamm,
        ls_md04_full TYPE ty_md04_full.

  DATA: lr_komp TYPE RANGE OF zpowerbi_vc_txt-kompnr.

  CLEAR ps_ktab.
  ps_ktab-idnrk  = pv_idnrk.
  ps_ktab-matnr  = pv_matnr.
  ps_ktab-postyp = pv_postyp.

  PERFORM build_komp_range USING pv_idnrk CHANGING lr_komp.

  " Textpositionen: nur Bezeichnung, keine Werte/Stammdaten
  IF pv_postyp = 'T'.
    IF p_txtpo = 'X'.
      SELECT SINGLE mat_txt FROM zpowerbi_vc_txt
        INTO @ps_ktab-maktx
        WHERE kompnr  IN @lr_komp
          AND mat_txt <> ''
          AND matnr    = @pv_matnr.
      IF ps_ktab-maktx IS INITIAL.
        CALL FUNCTION 'CONVERSION_EXIT_MATN1_OUTPUT'
          EXPORTING
            input  = pv_idnrk
          IMPORTING
            output = ps_ktab-maktx.
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

    IF pv_baugruppe = 'X' OR ls_stamm-has_bom = 'X'.
      IF ls_stamm-beskz <> 'F'.
        ps_ktab-baugr = 'X'.
      ENDIF.
    ENDIF.

    IF ps_ktab-peinh IS INITIAL.
      ps_ktab-peinh = 1.
    ENDIF.

    IF ls_stamm-vprsv = 'V'.
      ps_ktab-stueckkosten = ls_stamm-verpr / ps_ktab-peinh.
    ELSE.
      ps_ktab-stueckkosten = ls_stamm-stprs / ps_ktab-peinh.
    ENDIF.

  ELSE.
    " Material nicht in MARA / nicht aufloesbar
    IF p_txtpo = 'X'.
      SELECT SINGLE mat_txt FROM zpowerbi_vc_txt
        INTO @ps_ktab-maktx
        WHERE kompnr  IN @lr_komp
          AND mat_txt <> ''
          AND matnr    = @pv_matnr.
      IF ps_ktab-maktx IS INITIAL.
        CALL FUNCTION 'CONVERSION_EXIT_MATN1_OUTPUT'
          EXPORTING
            input  = pv_idnrk
          IMPORTING
            output = ps_ktab-maktx.
      ENDIF.
    ENDIF.
  ENDIF.

  " *** Kennzahlen aus ZMD04_CALC Cache ***
  CLEAR ls_md04_full.
  READ TABLE gt_md04_full_cache INTO ls_md04_full
    WITH TABLE KEY matnr = pv_idnrk.

  IF sy-subrc = 0.
    ps_ktab-labst     = ls_md04_full-labst.
    ps_ktab-feste_zug = ls_md04_full-feste_zug.
    ps_ktab-gepl_zug  = ls_md04_full-gepl_zug.
    ps_ktab-feste_abg = ls_md04_full-feste_abg.
    ps_ktab-gepl_abg  = ls_md04_full-gepl_abg.
    ps_ktab-verbr     = ls_md04_full-verbr.
    ps_ktab-omeng     = ls_md04_full-omeng.
    ps_ktab-owert     = ls_md04_full-owert.
    ps_ktab-mkmng     = ls_md04_full-mkmng.
    ps_ktab-omkwr     = ls_md04_full-omkwr.

    ps_ktab-endbestand = ps_ktab-labst
                       + ps_ktab-feste_zug
                       + ps_ktab-gepl_zug
                       - ps_ktab-feste_abg
                       - ps_ktab-gepl_abg.

    ps_ktab-wert_feste_zug  = ps_ktab-feste_zug  * ps_ktab-stueckkosten.
    ps_ktab-wert_gepl_zug   = ps_ktab-gepl_zug   * ps_ktab-stueckkosten.
    ps_ktab-wert_feste_abg  = ps_ktab-feste_abg  * ps_ktab-stueckkosten.
    ps_ktab-wert_gepl_abg   = ps_ktab-gepl_abg   * ps_ktab-stueckkosten.
    ps_ktab-wert_endbestand = ps_ktab-endbestand * ps_ktab-stueckkosten.
  ENDIF.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM get_excel_col - Zahl zu Excel-Spalte
*&---------------------------------------------------------------------*
FORM get_excel_col
  USING    pv_num TYPE i
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
*& FORM ermittle_weitere_vknr
*& FIX 11: KOMPNR-Normalisierung
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
    lt_all_usage   TYPE STANDARD TABLE OF lty_usage,
    lt_kompnr_drv  TYPE tt_matnr_list,
    lt_vknr_list   TYPE tt_matnr_list,
    lt_mara_data   TYPE HASHED TABLE OF lty_mara WITH UNIQUE KEY matnr,
    lt_sel_vknr    TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
    lt_verbr_temp  TYPE HASHED TABLE OF ty_md04_full WITH UNIQUE KEY matnr,
    lt_result_temp TYPE STANDARD TABLE OF ty_weitere_vknr,
    lt_count_temp  TYPE HASHED TABLE OF ty_count_raw WITH UNIQUE KEY kompnr,
    lt_vknr_temp   TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
    ls_usage       TYPE lty_usage,
    ls_mara        TYPE lty_mara,
    ls_weitere     TYPE ty_weitere_vknr,
    ls_matnr_line  TYPE ty_matnr_line,
    ls_ktab        TYPE ty_ktab,
    ls_vtab        TYPE ty_vtab,
    ls_exkl        TYPE ty_exkl,
    ls_md04_full   TYPE ty_md04_full,
    ls_count       TYPE ty_count_raw,
    lv_vknr        TYPE matnr.

  FIELD-SYMBOLS: <fs_usage> TYPE lty_usage.

  CLEAR: gt_weitere_vknr, gv_max_weitere.

  CHECK p_topd = 'X'.
  CHECK p_wvknr = 'X'.
  CHECK gt_ktab IS NOT INITIAL.

  LOOP AT gt_ktab INTO ls_ktab.
    PERFORM add_both_forms USING ls_ktab-idnrk CHANGING lt_kompnr_drv.
  ENDLOOP.
  SORT lt_kompnr_drv BY matnr.
  DELETE ADJACENT DUPLICATES FROM lt_kompnr_drv COMPARING matnr.

  CHECK lt_kompnr_drv IS NOT INITIAL.

  LOOP AT gt_vtab INTO ls_vtab.
    INSERT ls_vtab-matnr INTO TABLE lt_sel_vknr.
  ENDLOOP.

  SELECT kompnr matnr AS vknr
    FROM zpowerbi_vc_txt
    INTO TABLE lt_all_usage
    FOR ALL ENTRIES IN lt_kompnr_drv
    WHERE kompnr = lt_kompnr_drv-matnr.

  IF lt_all_usage IS INITIAL.
    RETURN.
  ENDIF.

  LOOP AT lt_all_usage ASSIGNING <fs_usage>.
    PERFORM norm_matnr USING <fs_usage>-kompnr
                       CHANGING <fs_usage>-kompnr.
  ENDLOOP.

  SORT lt_all_usage BY kompnr vknr.
  DELETE ADJACENT DUPLICATES FROM lt_all_usage COMPARING kompnr vknr.

  LOOP AT lt_all_usage INTO ls_usage.
    INSERT ls_usage-vknr INTO TABLE lt_vknr_temp.
  ENDLOOP.

  DELETE lt_vknr_temp WHERE table_line IS INITIAL.

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

    SELECT matnr werks verbr
      FROM zmd04_calc
      INTO CORRESPONDING FIELDS OF TABLE lt_verbr_temp
      FOR ALL ENTRIES IN lt_vknr_list
      WHERE matnr = lt_vknr_list-matnr
        AND werks = p_werks.
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

    "*** FIX 15: nur bei gesicherter Exklusivitaet ('X') unterdruecken.
    "*** Bei '?' sollen die weiteren VKNRs gerade sichtbar sein.
    READ TABLE gt_exkl_cache INTO ls_exkl
      WITH TABLE KEY kompnr = ls_usage-kompnr.
    IF sy-subrc = 0 AND ls_exkl-exklusiv = 'X'.
      CONTINUE.
    ENDIF.

    CLEAR ls_weitere.
    ls_weitere-kompnr = ls_usage-kompnr.
    ls_weitere-vknr   = ls_usage-vknr.

    CLEAR ls_md04_full.
    READ TABLE lt_verbr_temp INTO ls_md04_full
      WITH TABLE KEY matnr = ls_usage-vknr.
    IF sy-subrc = 0.
      ls_weitere-verbr = ls_md04_full-verbr.
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
      CLEAR ls_count.
      ls_count-kompnr = ls_weitere-kompnr.
      ls_count-count  = 1.
      INSERT ls_count INTO TABLE lt_count_temp.
    ENDIF.

    APPEND ls_weitere TO gt_weitere_vknr.

    IF ls_count-count > gv_max_weitere.
      gv_max_weitere = ls_count-count.
    ENDIF.

  ENDLOOP.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM ausgabe_weitere_vknr
*&---------------------------------------------------------------------*
FORM ausgabe_weitere_vknr.

  DATA:
    lv_line_idx  TYPE i,
    lv_header    TYPE string,
    lv_vknr_str  TYPE string,
    lv_col_num   TYPE i,
    lv_col_text  TYPE string,
    ls_ktab      TYPE ty_ktab,
    ls_weitere   TYPE ty_weitere_vknr,
    lt_vknr_zeile TYPE STANDARD TABLE OF matnr,
    lv_vknr      TYPE matnr,
    lv_ktab_idx  TYPE i.

  FIELD-SYMBOLS: <fs_line> TYPE string.

  CHECK p_topd = 'X'.
  CHECK p_wvknr = 'X'.
  CHECK gv_max_weitere > 0.
  CHECK gt_excel IS NOT INITIAL.

  READ TABLE gt_excel ASSIGNING <fs_line> INDEX 1.
  IF sy-subrc = 0.
    DO gv_max_weitere TIMES.
      lv_col_num  = sy-index.
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
*& FORM load_elternmaterial_cache
*&
*& ACHTUNG - OFFENER PUNKT:
*& Die Spalte "Elternmaterial" wird aus ZPOWERBI_VC_TXT-KOM_MSTAE
*& gefuellt. Das Feld ist zwar mit dem Datenelement MATNR typisiert,
*& enthaelt laut Datenbestand aber einen MATERIALSTATUS und ist in den
*& geprueften Saetzen leer. Die Spalte bleibt dadurch leer.
*& ZPOWERBI_VC_TXT enthaelt kein Feld fuer die uebergeordnete
*& Baugruppe - eine korrekte Ermittlung braucht entweder eine
*& Erweiterung der Tabelle oder eine Aufloesung ueber STPO/MAST.
*& Bewusst unveraendert gelassen, bis das Quellfeld geklaert ist.
*&---------------------------------------------------------------------*
FORM load_elternmaterial_cache.

  TYPES:
    BEGIN OF lty_parent_raw,
      kompnr    TYPE matnr,
      kom_mstae TYPE matnr,
    END OF lty_parent_raw.

  DATA:
    lt_parents    TYPE STANDARD TABLE OF lty_parent_raw,
    lt_kompnr_drv TYPE tt_matnr_list,
    ls_parent     TYPE lty_parent_raw,
    ls_ktab       TYPE ty_ktab,
    ls_vtab       TYPE ty_vtab,
    ls_cache      TYPE ty_mtab.

  DATA: lr_sel_vknr TYPE RANGE OF matnr,
        ls_sel_vknr LIKE LINE OF lr_sel_vknr.

  FIELD-SYMBOLS: <fs_parent> TYPE lty_parent_raw.

  CHECK p_topd = 'X'.
  CHECK gt_ktab IS NOT INITIAL.

  LOOP AT gt_ktab INTO ls_ktab.
    PERFORM add_both_forms USING ls_ktab-idnrk CHANGING lt_kompnr_drv.
  ENDLOOP.
  SORT lt_kompnr_drv BY matnr.
  DELETE ADJACENT DUPLICATES FROM lt_kompnr_drv COMPARING matnr.

  LOOP AT gt_vtab INTO ls_vtab.
    ls_sel_vknr-sign   = 'I'.
    ls_sel_vknr-option = 'EQ'.
    ls_sel_vknr-low    = ls_vtab-matnr.
    APPEND ls_sel_vknr TO lr_sel_vknr.
  ENDLOOP.

  CHECK lt_kompnr_drv IS NOT INITIAL.
  CHECK lr_sel_vknr   IS NOT INITIAL.

  SELECT kompnr kom_mstae
    FROM zpowerbi_vc_txt
    INTO TABLE lt_parents
    FOR ALL ENTRIES IN lt_kompnr_drv
    WHERE kompnr = lt_kompnr_drv-matnr
      AND matnr IN lr_sel_vknr.

  LOOP AT lt_parents ASSIGNING <fs_parent>.
    PERFORM norm_matnr USING <fs_parent>-kompnr
                       CHANGING <fs_parent>-kompnr.
  ENDLOOP.

  LOOP AT lt_parents INTO ls_parent.
    IF ls_parent-kom_mstae IS NOT INITIAL.
      CLEAR ls_cache.
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
    lv_matnr_str TYPE matnr,
    lv_parent    TYPE matnr,
    lt_distinct  TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

  CLEAR pv_eltern.

  LOOP AT gt_eltern_cache INTO ls_cache WHERE matnr = pv_kompnr.
    INSERT ls_cache-idnrk INTO TABLE lt_distinct.
  ENDLOOP.

  LOOP AT lt_distinct INTO lv_parent.

    PERFORM denorm_matnr USING lv_parent CHANGING lv_matnr_str.

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
        lv_q             TYPE i,
        ls_vtab          TYPE ty_vtab,
        ls_ktab          TYPE ty_ktab,
        ls_mtab          TYPE ty_mtab,
        ls_exkl          TYPE ty_exkl,
        lv_menge         TYPE string,
        lv_formel        TYPE string,
        lv_line_str      TYPE string,
        lv_last_row      TYPE i,
        lv_eltern        TYPE string,
        lv_feste_abg_str TYPE string,
        lv_gepl_abg_str  TYPE string,
        lv_wert_str      TYPE string.

  FIELD-SYMBOLS: <fs_vtab_upd> TYPE ty_vtab.

  gv_line = 1.

  " ---------------- KOPFZEILE ----------------
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
    lv_q   = lv_pos + 4.
    PERFORM get_excel_col USING lv_q CHANGING lv_col.

    READ TABLE gt_vtab ASSIGNING <fs_vtab_upd>
      WITH TABLE KEY matnr = ls_vtab-matnr.
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
  lv_q   = lv_pos + 4.
  IF p_topd = 'X'.
    lv_q = lv_q + 1.
  ENDIF.
  PERFORM get_excel_col USING lv_q CHANGING gv_sp_sum.
  CONCATENATE lv_line gv_tab TEXT-005 INTO lv_line.

  lv_pos = lv_pos + 1.
  lv_q   = lv_pos + 4.
  IF p_topd = 'X'.
    lv_q = lv_q + 1.
  ENDIF.
  PERFORM get_excel_col USING lv_q CHANGING gv_sp_vbr.
  CONCATENATE lv_line gv_tab TEXT-006 INTO lv_line.

  CONCATENATE lv_line gv_tab 'Lagerbestand'          INTO lv_line.
  CONCATENATE lv_line gv_tab 'Feste Zugänge'         INTO lv_line.
  CONCATENATE lv_line gv_tab 'Geplante Zugänge'      INTO lv_line.
  CONCATENATE lv_line gv_tab 'Feste Abgänge'         INTO lv_line.
  CONCATENATE lv_line gv_tab 'Geplante Abgänge'      INTO lv_line.
  CONCATENATE lv_line gv_tab 'Endbestand'            INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-009                INTO lv_line.
  CONCATENATE lv_line gv_tab 'Stückkosten Lager'     INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-010                INTO lv_line.
  CONCATENATE lv_line gv_tab 'Wert Feste Zugänge'    INTO lv_line.
  CONCATENATE lv_line gv_tab 'Wert Geplante Zugänge' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Wert Feste Abgänge'    INTO lv_line.
  CONCATENATE lv_line gv_tab 'Wert Geplante Abgänge' INTO lv_line.
  CONCATENATE lv_line gv_tab 'Wert Endbestand'       INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-012                INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-013                INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-014                INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-015                INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-016                INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-017                INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-018                INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-019                INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-020                INTO lv_line.
  CONCATENATE lv_line gv_tab TEXT-025                INTO lv_line.
  CONCATENATE lv_line gv_tab 'Postyp'                INTO lv_line.

  APPEND lv_line TO gt_excel.
  gv_line = gv_line + 1.

  " ---------------- MSTAE-ZEILE (nur Top-Down) ----------------
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

  " ---------------- VERBRAUCHSZEILE ----------------
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

  " ---------------- DATENZEILEN ----------------
  LOOP AT gt_ktab INTO ls_ktab.

    lv_line = ls_ktab-idnrk.
    CONCATENATE lv_line gv_tab ls_ktab-maktx INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-meins INTO lv_line.

    IF p_topd = 'X'.
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

    "*** FIX 15: Kennzeichen durchreichen statt hart 'X'
    IF p_topd = 'X'.
      CLEAR ls_exkl.
      READ TABLE gt_exkl_cache INTO ls_exkl
        WITH TABLE KEY kompnr = ls_ktab-idnrk.
      IF sy-subrc = 0.
        CONCATENATE lv_line gv_tab ls_exkl-exklusiv INTO lv_line.
      ELSE.
        CONCATENATE lv_line gv_tab '' INTO lv_line.
      ENDIF.
    ENDIF.

    lv_line_str = gv_line.
    CONDENSE lv_line_str.
    IF p_topd = 'X'.
      CONCATENATE '=SUMMENPRODUKT($' gv_sp_von '$3:$' gv_sp_bis '$3;'
                  gv_sp_von lv_line_str ':' gv_sp_bis lv_line_str ')'
                  INTO lv_formel.
    ELSE.
      IF ls_ktab-baugr IS INITIAL.
        CONCATENATE '=SUMME(' gv_sp_von INTO lv_formel.
        CONCATENATE lv_formel lv_line_str ':' gv_sp_bis lv_line_str ')'
                    INTO lv_formel.
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

    CONCATENATE lv_line gv_tab ls_ktab-mstae       INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-mstav       INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-zzlzcod     INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-zzlzcodsort INTO lv_line.
    CONCATENATE lv_line gv_tab ls_ktab-postyp      INTO lv_line.

    APPEND lv_line TO gt_excel.
    gv_line = gv_line + 1.

  ENDLOOP.

  " ---------------- SUMMENZEILE (nur Bottom-Up) ----------------
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
*& FORM ausgabe_spalten_details
*& FIX 16: sy-subrc wird nach JEDEM READ gesichert
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
    lv_peinh      TYPE peinh,
    lv_sub_md04   TYPE sy-subrc,
    lv_sub_stamm  TYPE sy-subrc.

  CHECK gt_vtab  IS NOT INITIAL.
  CHECK gt_excel IS NOT INITIAL.

  APPEND '' TO gt_excel.

  " --- Lagerbestand ---
  lv_line = 'Lagerbestand'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    CLEAR ls_md04_full.
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
    CLEAR ls_md04_full.
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
    CLEAR ls_md04_full.
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
    CLEAR ls_md04_full.
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
    CLEAR ls_md04_full.
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

  " --- Endbestand ---
  lv_line = 'Endbestand'.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.
  LOOP AT gt_vtab INTO ls_vtab.
    CLEAR ls_md04_full.
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
    CLEAR ls_md04_full.
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
    CLEAR ls_stamm.
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
    CLEAR ls_stamm.
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
    lv_sub_md04 = sy-subrc.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    lv_sub_stamm = sy-subrc.

    IF lv_sub_md04 = 0 AND lv_sub_stamm = 0.
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
    lv_sub_md04 = sy-subrc.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    lv_sub_stamm = sy-subrc.

    IF lv_sub_md04 = 0 AND lv_sub_stamm = 0.
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
    lv_sub_md04 = sy-subrc.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    lv_sub_stamm = sy-subrc.

    IF lv_sub_md04 = 0 AND lv_sub_stamm = 0.
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
    lv_sub_md04 = sy-subrc.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    lv_sub_stamm = sy-subrc.

    IF lv_sub_md04 = 0 AND lv_sub_stamm = 0.
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
    lv_sub_md04 = sy-subrc.
    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    lv_sub_stamm = sy-subrc.

    IF lv_sub_md04 = 0 AND lv_sub_stamm = 0.
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
    CLEAR ls_md04_full.
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

  " --- Stammdatenzeilen ---
  PERFORM detail_stamm_zeile USING 'Dispom.'      'DISMM'.
  PERFORM detail_stamm_zeile USING 'Meldebest.'   'MINBE'.
  PERFORM detail_stamm_zeile USING 'Dispolg.'     'DISLS'.
  PERFORM detail_stamm_zeile USING 'Feste Losgr.' 'BSTFE'.
  PERFORM detail_stamm_zeile USING 'Sibe'         'EISBE'.
  PERFORM detail_stamm_zeile USING 'Matst.'       'MSTAE'.
  PERFORM detail_stamm_zeile USING 'Verk.st.'     'MSTAV'.
  PERFORM detail_stamm_zeile USING 'LZ-Code'      'ZZLZCOD'.
  PERFORM detail_stamm_zeile USING 'Sort-Code'    'ZZLZCODSORT'.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM detail_stamm_zeile
*& Zusammenfassung der neun identisch aufgebauten Stammdatenzeilen
*&---------------------------------------------------------------------*
FORM detail_stamm_zeile
  USING pv_label TYPE string
        pv_feld  TYPE string.

  DATA: ls_vtab  TYPE ty_vtab,
        ls_stamm TYPE ty_stamm,
        lv_line  TYPE string,
        lv_wert  TYPE string.

  FIELD-SYMBOLS: <fs_val> TYPE any.

  lv_line = pv_label.
  CONCATENATE lv_line gv_tab gv_tab gv_tab INTO lv_line.

  LOOP AT gt_vtab INTO ls_vtab.
    CLEAR: ls_stamm, lv_wert.

    READ TABLE gt_stamm_cache INTO ls_stamm
      WITH TABLE KEY matnr = ls_vtab-matnr.
    IF sy-subrc = 0.
      ASSIGN COMPONENT pv_feld OF STRUCTURE ls_stamm TO <fs_val>.
      IF sy-subrc = 0.
        lv_wert = <fs_val>.
        CONDENSE lv_wert.
      ENDIF.
    ENDIF.

    CONCATENATE lv_line gv_tab lv_wert INTO lv_line.
  ENDLOOP.

  APPEND lv_line TO gt_excel.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM run_diagnose
*& FIX 18
*& Beantwortet die offenen Fragen aus der Analyse ohne separates
*& Testprogramm. Erzeugt KEINE Excel-Datei.
*&---------------------------------------------------------------------*
FORM run_diagnose.

  TYPES: BEGIN OF lty_md04_dup,
           matnr TYPE matnr,
           werks TYPE werks_d,
           mkmng TYPE menge_d,
           labst TYPE labst,
         END OF lty_md04_dup.

  DATA: lt_dup      TYPE STANDARD TABLE OF lty_md04_dup,
        ls_dup      TYPE lty_md04_dup,
        lt_drv      TYPE tt_matnr_list,
        ls_line     TYPE ty_matnr_line,
        ls_stamm    TYPE ty_stamm,
        ls_md04     TYPE ty_md04_full,
        ls_ktab     TYPE ty_ktab,
        ls_vtab     TYPE ty_vtab,
        ls_exkl     TYPE ty_exkl,
        lv_ext      TYPE matnr,
        lv_cnt      TYPE i,
        lv_prev     TYPE matnr,
        lv_dupcnt   TYPE i,
        lv_menge    TYPE menge_d,
        lv_i        TYPE i.

  SKIP.
  WRITE: / '=== ZM_LZCODE20_OPT Diagnose ==='.
  WRITE: / 'Werk                :', p_werks.
  IF p_topd = 'X'.
    WRITE: / 'Richtung            : Top-Down'.
  ELSE.
    WRITE: / 'Richtung            : Bottom-Up'.
  ENDIF.
  WRITE: / 'Saetze ZPOWERBI_VC_TXT:', gv_d_powerbi.
  WRITE: / 'KTAB-Zeilen         :', lines( gt_ktab ).
  WRITE: / 'VTAB-Spalten        :', lines( gt_vtab ).

  SKIP.
  WRITE: / '--- 1) Schluesselformat KOMPNR ---'.
  WRITE: / 'Komponenten ohne MARA-Treffer nach Normalisierung:',
           gv_d_keyfail.
  WRITE: / '(Wert > 0 = KOMPNR und MARA-MATNR sind unterschiedlich',
           'formatiert oder Material existiert nicht)'.

  SKIP.
  WRITE: / '--- 2) Kennzahlen je KTAB-Zeile ---'.
  WRITE: /  'Material', 45 'MK', 65 'Lagerbest.', 85 'Stueckk.',
            105 'Postyp', 115 'Exkl'.
  ULINE.

  lv_i = 0.
  LOOP AT gt_ktab INTO ls_ktab.
    lv_i = lv_i + 1.
    IF lv_i > 200.
      WRITE: / '... weitere Zeilen unterdrueckt'.
      EXIT.
    ENDIF.

    PERFORM denorm_matnr USING ls_ktab-idnrk CHANGING lv_ext.

    CLEAR ls_exkl.
    READ TABLE gt_exkl_cache INTO ls_exkl
      WITH TABLE KEY kompnr = ls_ktab-idnrk.

    WRITE: /  lv_ext,
           45 ls_ktab-mkmng,
           65 ls_ktab-labst,
           85 ls_ktab-stueckkosten,
          105 ls_ktab-postyp,
          115 ls_exkl-exklusiv.
  ENDLOOP.

  SKIP.
  WRITE: / '--- 3) ZMD04_CALC: Saetze ueber alle Werke ---'.

  CLEAR lt_drv.
  LOOP AT gt_ktab INTO ls_ktab.
    ls_line-matnr = ls_ktab-idnrk.
    APPEND ls_line TO lt_drv.
  ENDLOOP.
  LOOP AT gt_vtab INTO ls_vtab.
    ls_line-matnr = ls_vtab-matnr.
    APPEND ls_line TO lt_drv.
  ENDLOOP.
  SORT lt_drv BY matnr.
  DELETE ADJACENT DUPLICATES FROM lt_drv COMPARING matnr.

  IF lt_drv IS NOT INITIAL.
    SELECT matnr werks mkmng labst
      FROM zmd04_calc
      INTO CORRESPONDING FIELDS OF TABLE lt_dup
      FOR ALL ENTRIES IN lt_drv
      WHERE matnr = lt_drv-matnr.

    SORT lt_dup BY matnr werks.

    CLEAR: lv_prev, lv_cnt, lv_dupcnt.
    LOOP AT lt_dup INTO ls_dup.
      IF ls_dup-matnr = lv_prev.
        lv_cnt = lv_cnt + 1.
      ELSE.
        IF lv_cnt > 1.
          lv_dupcnt = lv_dupcnt + 1.
        ENDIF.
        lv_prev = ls_dup-matnr.
        lv_cnt  = 1.
      ENDIF.
    ENDLOOP.
    IF lv_cnt > 1.
      lv_dupcnt = lv_dupcnt + 1.
    ENDIF.

    WRITE: / 'Materialien mit mehr als einem Satz (Werk offen):',
             lv_dupcnt.
    WRITE: / '(Wert > 0 = der HASHED-INSERT in load_md04_bulk',
             'verwirft still Saetze)'.

    SKIP.
    WRITE: / 'Saetze mit MKMNG <> 0:'.
    LOOP AT lt_dup INTO ls_dup WHERE mkmng <> 0.
      PERFORM denorm_matnr USING ls_dup-matnr CHANGING lv_ext.
      WRITE: / lv_ext, 45 ls_dup-werks, 55 ls_dup-mkmng.
    ENDLOOP.
    IF sy-subrc <> 0.
      WRITE: / 'keine'.
    ENDIF.
  ENDIF.

  SKIP.
  WRITE: / '--- 4) Filterverluste Bottom-Up ---'.
  WRITE: / 'Koepfe ohne MARA-Satz      :', gv_d_lost_stamm.
  WRITE: / 'Koepfe mit Loeschvormerkung:', gv_d_lost_lvorm.
  IF p_lvorm = ' '.
    WRITE: / '(p_lvorm ist AUS - mit p_lvorm = X gegenpruefen)'.
  ENDIF.

  SKIP.
  WRITE: / '--- 5) Mengenparser (FIX 10) ---'.
  WRITE: / 'Dezimaldarstellung Benutzer:', sy-uname.
  PERFORM parse_menge_str USING '1.000'     CHANGING lv_menge.
  WRITE: / '  1.000     ->', lv_menge.
  PERFORM parse_menge_str USING '0.070'     CHANGING lv_menge.
  WRITE: / '  0.070     ->', lv_menge.
  PERFORM parse_menge_str USING '1.234,567' CHANGING lv_menge.
  WRITE: / '  1.234,567 ->', lv_menge.
  PERFORM parse_menge_str USING '1,234.567' CHANGING lv_menge.
  WRITE: / '  1,234.567 ->', lv_menge.
  WRITE: / '(Ergebnisse muessen fuer JEDEN Benutzer identisch sein)'.

ENDFORM.


*&---------------------------------------------------------------------*
*& FORM download_dialog
*&---------------------------------------------------------------------*
FORM download_dialog.

  DATA: lv_filename TYPE string,
        lv_path     TYPE string,
        lv_fullpath TYPE string.

  IF gt_excel IS INITIAL.
    MESSAGE 'Keine Daten vorhanden' TYPE 'S' DISPLAY LIKE 'W'.
    RETURN.
  ENDIF.

  CALL FUNCTION 'GUI_FILE_SAVE_DIALOG'
    EXPORTING
      default_extension = 'xls'
      default_file_name = 'Material_Verwendung'
      initial_directory = 'C:\temp'
    IMPORTING
      filename          = lv_filename
      path              = lv_path
      fullpath          = lv_fullpath.

  CHECK lv_fullpath IS NOT INITIAL.

  CALL FUNCTION 'GUI_DOWNLOAD'
    EXPORTING
      filename = lv_fullpath
      filetype = 'ASC'
    TABLES
      data_tab = gt_excel.

  CALL METHOD cl_gui_frontend_services=>execute
    EXPORTING
      document = lv_fullpath.

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
