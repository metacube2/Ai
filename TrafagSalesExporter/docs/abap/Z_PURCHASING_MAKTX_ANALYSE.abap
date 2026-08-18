REPORT z_purchasing_maktx_analyse.

" ============================================================================
" Analyse-Report: Materialtext (MAKT/MAKTX) fuer den Einkauf-Drilldown
" ----------------------------------------------------------------------------
" HINWEIS zum Programmnamen: Wird der Code in ein bestehendes Testprogramm
" (z.B. ZTEST55) eingefuegt, muss die REPORT-Zeile oben auf dessen Namen
" lauten - SE38 meldet sonst einen Namenskonflikt.
"
" Zweck: Beantwortet DIREKT auf der Datenbank (nicht ueber OData/Gateway, also
"        unabhaengig vom travp762-Login-Problem), ob und wie ein Materialtext
"        fuer den Drilldown Lieferant -> Warengruppe -> Material zur Verfuegung
"        steht. Hintergrund: MARA selbst hat KEIN Textfeld; der SAP-Standard
"        fuehrt Materialtexte sprachabhaengig in MAKT (Schluessel MATNR+SPRAS).
"        Ingo hat zusaetzlich live ein EntitySet "MAKTSet" im Gateway-Service
"        ZPOWERBI_EINKAUF_SRV auf travt762 (TEST) gefunden - dieser Report
"        prueft die zugrunde liegenden MAKT-Daten selbst, damit die
"        Weiterentwicklung (Feldname, Sprachfilter, Fuellgrad) unabhaengig
"        vom Gateway-Login weitergehen kann.
"
" Beantwortet:
"   1) Gibt es zu den im Einkauf verwendeten Materialien (EKPO.MATNR) ueberhaupt
"      einen MAKT-Satz, und zu wieviel Prozent?
"   2) In welcher/welchen Sprache(n) ist MAKTX gepflegt (SPRAS-Verteilung)?
"      Wichtig fuer den Join: MAKT ist je Material MEHRFACH vorhanden (eine
"      Zeile je Sprache) - ein Join ohne SPRAS-Filter wuerde Zeilen vervielfachen.
"   3) Stichprobe MAKTX fuer die zwei aus dem Chat bekannten Materialien
"      B64880 und B64336 (Lieferant BEPRO AG, Warengruppe 10.08.00).
"   4) Wieviele Materialien haben KEINEN Text (Luecke, die im Drilldown dann
"      auf "ohne Materialtext" faellt statt einen erfundenen Wert zu zeigen).
"
" Beantwortet NICHT (das kann kein SELECT klaeren, siehe Abschnitt 5):
"   - Ob das EntitySet "MAKTSet" im Gateway-Service auf travp762 (PROD) EXAKT
"     so existiert wie auf travt762 (TEST), inkl. Feldname fuer den Text.
"     Das ist eine SEGW-/Transport-Frage, kein Tabelleninhalt.
"
" Bedienung:
"   1. SE38 -> Programm anlegen -> diesen Code einfuegen (REPORT-Zeile anpassen).
"   2. Ausfuehren (F8). Parameter siehe Selektionsbild (Defaults passen i.d.R.).
"   3. Die WRITE-Ausgabe komplett markieren und an Claude/Analytics zurueckgeben.
"
" ABAP-Fallen, die hier bewusst vermieden werden (Syntaxfehler beim ersten Lauf):
"   - In WRITE darf NICHT gerechnet werden -> alle Prozentwerte vorher in
"     Variablen berechnen.
"   - GROUP BY ist zusammen mit FOR ALL ENTRIES NICHT erlaubt -> die
"     Sprachverteilung wird im ABAP-Code aggregiert, nicht per SQL.
"
" Nur lesend. Keine Aenderung am System.
" ============================================================================

TABLES: ekko.

PARAMETERS:
  p_bedat TYPE ekko-bedat DEFAULT '20200101',   " Einkauf ab Bestelldatum, bestimmt das Materialuniversum
  p_spras TYPE spras      DEFAULT sy-langu,      " zu pruefende Zielsprache fuer den Drilldown-Text
  p_smpl  TYPE i          DEFAULT 20.            " Beispielzeilen je Detailblock

" Prozentwerte: in ABAP nicht in WRITE berechenbar, daher eigene Variablen.
DATA: lv_pct_text   TYPE p LENGTH 8 DECIMALS 1,
      lv_pct_target TYPE p LENGTH 8 DECIMALS 1,
      lv_pct_help   TYPE p LENGTH 8 DECIMALS 1.

START-OF-SELECTION.

  WRITE: / '############################################################'.
  WRITE: / '# Materialtext-Analyse (MAKT/MAKTX) fuer den Einkauf-Drilldown'.
  WRITE: / '# System / Mandant:', sy-sysid, sy-mandt.
  WRITE: / '# Datum / Uhrzeit  :', sy-datum, sy-uzeit.
  WRITE: / '# Einkauf ab BEDAT :', p_bedat.
  WRITE: / '# Zielsprache SPRAS:', p_spras.
  WRITE: / '############################################################'.

" ----------------------------------------------------------------------------
" 1) Materialuniversum aus dem Einkauf (gleiche Abgrenzung wie das Dashboard)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 1) MATERIALUNIVERSUM AUS EKPO ==='.

  SELECT DISTINCT p~matnr
    FROM ekpo AS p
    INNER JOIN ekko AS h ON h~ebeln = p~ebeln
    WHERE h~bedat >= @p_bedat AND p~matnr <> ''
    INTO TABLE @DATA(lt_matnr).
  DATA(lv_matnr_total) = lines( lt_matnr ).
  WRITE: / '  Distinkte Materialien im Einkauf ab BEDAT:', lv_matnr_total.

  IF lt_matnr IS INITIAL.
    WRITE: / '  Keine Materialien gefunden - Report bricht ab.'.
    RETURN.
  ENDIF.

" ----------------------------------------------------------------------------
" 2) MAKT lesen: Fuellgrad, Sprachverteilung, Mehrfachzeilen je Material
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 2) MAKT-FUELLGRAD UND SPRACHVERTEILUNG ==='.

  " Alle Sprachen lesen (kein SPRAS-Filter), damit die Verteilung sichtbar wird.
  SELECT matnr, spras, maktx
    FROM makt
    FOR ALL ENTRIES IN @lt_matnr
    WHERE matnr = @lt_matnr-matnr
    INTO TABLE @DATA(lt_makt).
  DATA(lv_makt_total) = lines( lt_makt ).
  WRITE: / '  MAKT-Zeilen gesamt (alle Sprachen) zu diesen Materialien:', lv_makt_total.

  " 2a) Distinkte Materialien mit mindestens einem MAKT-Satz.
  "     Zaehlung ueber eine sortierte Tabelle mit UNIQUE KEY: doppelte
  "     Sprachzeilen desselben Materials werden dabei automatisch verworfen.
  DATA lt_matnr_with_text TYPE SORTED TABLE OF matnr WITH UNIQUE KEY table_line.
  LOOP AT lt_makt INTO DATA(ls_makt_all).
    INSERT ls_makt_all-matnr INTO TABLE lt_matnr_with_text.
  ENDLOOP.
  DATA(lv_with_text) = lines( lt_matnr_with_text ).

  CLEAR lv_pct_text.
  IF lv_matnr_total > 0.
    lv_pct_text = lv_with_text.
    lv_pct_text = lv_pct_text * 100 / lv_matnr_total.
  ENDIF.
  WRITE: / '  Materialien MIT mindestens einem MAKT-Satz (irgendeine Sprache):',
           lv_with_text, 'von', lv_matnr_total.
  WRITE: / '  Anteil in Prozent:', lv_pct_text.

  " 2b) Sprachverteilung. GROUP BY geht bei FOR ALL ENTRIES nicht,
  "     daher Aggregation im Code ueber die bereits gelesene lt_makt.
  WRITE: / '--- 2b) Sprachverteilung (SPRAS) ---'.
  DATA: BEGIN OF ls_spras_cnt,
          spras TYPE spras,
          cnt   TYPE i,
        END OF ls_spras_cnt,
        lt_spras_cnt LIKE SORTED TABLE OF ls_spras_cnt WITH UNIQUE KEY spras.

  LOOP AT lt_makt INTO DATA(ls_makt_s).
    READ TABLE lt_spras_cnt INTO ls_spras_cnt WITH KEY spras = ls_makt_s-spras.
    IF sy-subrc = 0.
      ls_spras_cnt-cnt = ls_spras_cnt-cnt + 1.
      MODIFY TABLE lt_spras_cnt FROM ls_spras_cnt.
    ELSE.
      CLEAR ls_spras_cnt.
      ls_spras_cnt-spras = ls_makt_s-spras.
      ls_spras_cnt-cnt   = 1.
      INSERT ls_spras_cnt INTO TABLE lt_spras_cnt.
    ENDIF.
  ENDLOOP.

  LOOP AT lt_spras_cnt INTO ls_spras_cnt.
    WRITE: / '  SPRAS=', ls_spras_cnt-spras, 'Zeilen=', ls_spras_cnt-cnt.
  ENDLOOP.

  " 2c) Fuellgrad speziell fuer die gewuenschte Zielsprache p_spras.
  WRITE: / '--- 2c) Fuellgrad fuer Zielsprache ---'.
  DATA lv_target_cnt TYPE i.
  LOOP AT lt_makt TRANSPORTING NO FIELDS WHERE spras = p_spras.
    lv_target_cnt = lv_target_cnt + 1.
  ENDLOOP.

  CLEAR lv_pct_target.
  IF lv_matnr_total > 0.
    lv_pct_target = lv_target_cnt.
    lv_pct_target = lv_pct_target * 100 / lv_matnr_total.
  ENDIF.
  WRITE: / '  Zielsprache:', p_spras.
  WRITE: / '  Materialien mit Text in dieser Sprache:', lv_target_cnt, 'von', lv_matnr_total.
  WRITE: / '  Anteil in Prozent:', lv_pct_target.

  IF lv_target_cnt < lv_with_text.
    WRITE: / '  Hinweis: In der Zielsprache fehlen Texte, die in einer ANDEREN Sprache'.
    WRITE: / '           vorhanden waeren -> Fallback-Reihenfolge (z.B. Zielsprache,'.
    WRITE: / '           dann EN, dann irgendeine) mit Ingo klaeren.'.
  ENDIF.

  " 2d) Mehrfachzeilen je Material - Join-Risiko fuer den C#-Loader.
  WRITE: / '--- 2d) Materialien mit MEHR ALS EINER Sprache gepflegt ---'.
  DATA: BEGIN OF ls_cnt,
          matnr TYPE matnr,
          cnt   TYPE i,
        END OF ls_cnt,
        lt_cnt LIKE SORTED TABLE OF ls_cnt WITH UNIQUE KEY matnr.

  LOOP AT lt_makt INTO DATA(ls_makt_c).
    READ TABLE lt_cnt INTO ls_cnt WITH KEY matnr = ls_makt_c-matnr.
    IF sy-subrc = 0.
      ls_cnt-cnt = ls_cnt-cnt + 1.
      MODIFY TABLE lt_cnt FROM ls_cnt.
    ELSE.
      CLEAR ls_cnt.
      ls_cnt-matnr = ls_makt_c-matnr.
      ls_cnt-cnt   = 1.
      INSERT ls_cnt INTO TABLE lt_cnt.
    ENDIF.
  ENDLOOP.

  DATA lv_multi TYPE i.
  LOOP AT lt_cnt INTO ls_cnt WHERE cnt > 1.
    lv_multi = lv_multi + 1.
  ENDLOOP.
  WRITE: / '  Materialien mit mehr als einer MAKT-Zeile (mehrsprachig gepflegt):', lv_multi.
  WRITE: / '  (von', lv_with_text, 'Materialien mit Text.)'.
  WRITE: / '  -> Ohne SPRAS-Filter im Join wuerden genau diese Materialien im'.
  WRITE: / '     Drilldown vervielfacht und die Spend-Summe verfaelschen.'.

" ----------------------------------------------------------------------------
" 3) Gezielte Stichprobe: bekannte Materialien aus dem Chat (BEPRO AG)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 3) STICHPROBE BEKANNTE MATERIALIEN (BEPRO AG / 10.08.00) ==='.

  " MATNR wird intern konvertiert gespeichert (numerische Nummern zero-padded).
  " Deshalb zusaetzlich die MATN1-konvertierte Form pruefen, wie in den
  " ZLO03-/ZSTR_MAT_XYZ-Methoden auch.
  DATA: lt_r_sample TYPE RANGE OF matnr,
        lv_conv     TYPE matnr.

  DATA lt_sample_in TYPE STANDARD TABLE OF matnr.
  APPEND 'B64880' TO lt_sample_in.
  APPEND 'B64336' TO lt_sample_in.

  LOOP AT lt_sample_in INTO DATA(lv_raw).
    APPEND VALUE #( sign = 'I' option = 'EQ' low = lv_raw ) TO lt_r_sample.

    CLEAR lv_conv.
    CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
      EXPORTING  input        = lv_raw
      IMPORTING  output       = lv_conv
      EXCEPTIONS length_error = 1
                 OTHERS       = 2.
    IF sy-subrc = 0 AND lv_conv <> lv_raw.
      APPEND VALUE #( sign = 'I' option = 'EQ' low = lv_conv ) TO lt_r_sample.
    ENDIF.
  ENDLOOP.

  SELECT matnr, spras, maktx
    FROM makt
    WHERE matnr IN @lt_r_sample
    INTO TABLE @DATA(lt_sample).

  IF lt_sample IS INITIAL.
    WRITE: / '  Keine MAKT-Zeile fuer B64880/B64336 gefunden.'.
    WRITE: / '  Moegliche Gruende: abweichende interne MATNR-Schreibweise oder'.
    WRITE: / '  die Nummer ist eine reine Beleg-/Fremdnummer ohne eigenen MARA-Satz.'.
  ELSE.
    LOOP AT lt_sample INTO DATA(ls_sample).
      WRITE: / '  MATNR=', ls_sample-matnr, 'SPRAS=', ls_sample-spras,
               'MAKTX=', ls_sample-maktx.
    ENDLOOP.
  ENDIF.

  " Zusaetzlich ein paar beliebige Beispiele mit Text, damit die Textqualitaet
  " (Laenge, Sprache, Aussagekraft) im Drilldown beurteilt werden kann.
  WRITE: / '--- 3a) Beliebige Beispieltexte in der Zielsprache ---'.
  DATA lv_shown_sample TYPE i.
  LOOP AT lt_makt INTO DATA(ls_makt_x) WHERE spras = p_spras.
    IF lv_shown_sample >= p_smpl.
      EXIT.
    ENDIF.
    WRITE: / '  MATNR=', ls_makt_x-matnr, 'MAKTX=', ls_makt_x-maktx.
    lv_shown_sample = lv_shown_sample + 1.
  ENDLOOP.

" ----------------------------------------------------------------------------
" 4) Materialien OHNE jeden Text (Luecke fuer den Drilldown-Fallback)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 4) MATERIALIEN OHNE MAKT-SATZ (Beispiele) ==='.

  DATA: lv_missing TYPE i,
        lv_shown   TYPE i.

  LOOP AT lt_matnr INTO DATA(ls_matnr).
    READ TABLE lt_matnr_with_text TRANSPORTING NO FIELDS
      WITH KEY table_line = ls_matnr-matnr.
    IF sy-subrc <> 0.
      lv_missing = lv_missing + 1.
      IF lv_shown < p_smpl.
        WRITE: / '  ohne Text: MATNR=', ls_matnr-matnr.
        lv_shown = lv_shown + 1.
      ENDIF.
    ENDIF.
  ENDLOOP.

  CLEAR lv_pct_help.
  IF lv_matnr_total > 0.
    lv_pct_help = lv_missing.
    lv_pct_help = lv_pct_help * 100 / lv_matnr_total.
  ENDIF.
  WRITE: / '  Materialien ganz ohne MAKT-Satz:', lv_missing, 'von', lv_matnr_total.
  WRITE: / '  Anteil in Prozent:', lv_pct_help.

" ----------------------------------------------------------------------------
" 5) Was dieser Report NICHT beantworten kann - manuell pruefen
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 5) OFFEN - BITTE MANUELL PRUEFEN (kein SELECT moeglich) ==='.
  WRITE: / '  a) Existiert "MAKTSet" im Gateway-Service ZPOWERBI_EINKAUF_SRV auch auf'.
  WRITE: / '     PROD (travp762), nicht nur auf TEST (travt762)? Pruefen in SEGW/SE80'.
  WRITE: / '     Projekt ZPOWERBI_EINKAUF_SRV auf BEIDEN Systemen, oder /IWFND/MAINT_SERVICE'.
  WRITE: / '     auf travp762 (Service dort aktiviert + gleicher Objektstand?).'.
  WRITE: / '  b) Exakter Feldname des Texts im generierten EntitySet (z.B. "Maktx" oder'.
  WRITE: / '     anders benannt) - in SEGW/SE11 der zugehoerigen DDIC-Struktur nachsehen,'.
  WRITE: / '     oder per Browser testen: <PROD-URL>/MAKTSet?$top=1&$format=json'.
  WRITE: / '     (mit gueltigem PROD-Login - der bisherige 401 war vermutlich ein falsches'.
  WRITE: / '     oder nur auf TEST gueltiges Passwort, keine Systemaussage).'.
  WRITE: / '  c) Liefert MAKTSet ALLE Sprachen oder filtert die GET_ENTITYSET-Methode'.
  WRITE: / '     bereits auf eine Sprache? Entscheidend fuer den Loader (siehe 2d).'.
  WRITE: / '  d) Falls MAKTSet NUR auf travt762 existiert: ist es ein lokales ($TMP-)'.
  WRITE: / '     Testobjekt oder transportfaehig angelegt? Falls lokal, muss es wie bei'.
  WRITE: / '     ZSTR_MAT_XYZ (docs/abap/ZSTR_MAT_XYZ_GET_ENTITYSET.abap) regulaer in'.
  WRITE: / '     einem Paket angelegt und nach PROD transportiert werden.'.

  ULINE.
  WRITE: / '=== ENDE. Bitte komplette Ausgabe an Claude/Analytics zurueckgeben. ==='.
