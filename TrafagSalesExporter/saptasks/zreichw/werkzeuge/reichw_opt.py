import sys
src, load_block, out = sys.argv[1:4]
t = open(src, encoding="utf-8").read().replace("\r\n", "\n")
load = open(load_block, encoding="utf-8").read().replace("\r\n", "\n")


def rep(old, new):
    global t
    assert t.count(old) == 1, old[:70]
    t = t.replace(old, new)


# Kopf
rep("""*& Report Z_KOI_REICHW_OPT
*&---------------------------------------------------------------------*
*& Optimierte Version - Direkte DB-Zugriffe, kein BDC/Spool
*& Performance: Sekunden statt Minuten
""", """*& Report Z_KOI_REICHW_OPT
*&---------------------------------------------------------------------*
*& Kopie von Z_KOI_REICHW_TURBO (ZREICHW), nur schneller, Ergebnis gleich
*& (Auftrag Ingo 2026-11-02, Vergleich mit Z_KOI_REICHW_VGL).
*& Optimierte Version - Direkte DB-Zugriffe, kein BDC/Spool
*& Performance: Sekunden statt Minuten
""")

# Typ und globale Materialliste, Sekundaerschluessel fuer die Debug-Ausgabe
rep("""  tt_mseg_agg TYPE STANDARD TABLE OF ty_mseg_agg WITH DEFAULT KEY,
""", """  tt_mseg_agg TYPE STANDARD TABLE OF ty_mseg_agg WITH DEFAULT KEY
                WITH NON-UNIQUE SORTED KEY mat COMPONENTS matnr,

  " Materialliste der Auswertung (geht als Tabelle in die Abfragen)
  BEGIN OF ty_mat,
    matnr TYPE matnr,
  END OF ty_mat,
""")
rep("""      gv_spool         TYPE abap_bool.     " Ausgabe geht in einen Spoolauftrag
""", """      gv_spool         TYPE abap_bool,     " Ausgabe geht in einen Spoolauftrag
      gt_mat           TYPE SORTED TABLE OF ty_mat WITH UNIQUE KEY matnr.
""")

# load_all_data ersetzen
a = t.index("*& Form LOAD_ALL_DATA")
a = t.index("FORM load_all_data.", a)
b = t.index("ENDFORM.", a) + len("ENDFORM.\n")
t = t[:a] + load + t[b:]

# XYZ: Materialliste statt Range
rep("""  DATA: lt_mver      TYPE SORTED TABLE OF ty_mver_data WITH NON-UNIQUE KEY matnr gjahr,
        lt_matnr_rng TYPE RANGE OF matnr.
""", """  DATA: lt_mver      TYPE SORTED TABLE OF ty_mver_data WITH NON-UNIQUE KEY matnr gjahr.
""")
rep("""  " Material-Range aufbauen
  LOOP AT gt_material_data ASSIGNING <fs_mat>.
    APPEND VALUE #( sign = 'I' option = 'EQ' low = <fs_mat>-matnr ) TO lt_matnr_rng.
  ENDLOOP.

""", "")
rep("""  SELECT matnr, gjahr,
         gsv01, gsv02, gsv03, gsv04, gsv05, gsv06,
         gsv07, gsv08, gsv09, gsv10, gsv11, gsv12
    FROM mver
    INTO TABLE @lt_mver
    WHERE matnr IN @lt_matnr_rng
      AND werks = @p_werks
      AND perkz = 'M'
      AND gjahr BETWEEN @lv_year_from AND @lv_year_to.
""", """  " Materialliste der Auswertung statt eines Range mit Einzelwerten
  SELECT v~matnr, v~gjahr,
         v~gsv01, v~gsv02, v~gsv03, v~gsv04, v~gsv05, v~gsv06,
         v~gsv07, v~gsv08, v~gsv09, v~gsv10, v~gsv11, v~gsv12
    FROM mver AS v
    INNER JOIN @gt_mat AS m ON m~matnr = v~matnr
    WHERE v~werks = @p_werks
      AND v~perkz = 'M'
      AND v~gjahr BETWEEN @lv_year_from AND @lv_year_to
    INTO TABLE @lt_mver.
""")

# Texte: vorhandene Namen einmal aus STXH, READ_TEXT nur fuer vorhandene
rep("""FORM load_material_texts.
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
ENDFORM.""", """FORM load_material_texts.
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
ENDFORM.""")

# Debug-Ausgabe: Bewegungen je Material ueber den sortierten Schluessel
rep("""    LOOP AT gt_mseg_debug ASSIGNING FIELD-SYMBOL(<fs_mseg>)
                          WHERE matnr = <fs_mat>-matnr.""",
    """    LOOP AT gt_mseg_debug ASSIGNING FIELD-SYMBOL(<fs_mseg>)
                          USING KEY mat
                          WHERE matnr = <fs_mat>-matnr.""")

open(out, "w", encoding="utf-8", newline="\n").write(t)
print("ok", t.count("\n"))
