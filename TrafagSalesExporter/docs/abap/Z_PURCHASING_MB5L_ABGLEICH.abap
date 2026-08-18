REPORT z_purchasing_mb5l_abgleich.

" ============================================================================
" MB5L-Abgleich fuer den Lagerwert der Einkaufsteile
" ----------------------------------------------------------------------------
" HINWEIS zum Programmnamen: Wird der Code in ein bestehendes Testprogramm
" (z.B. ZTEST55) eingefuegt, muss die REPORT-Zeile oben auf dessen Namen
" lauten - SE38 meldet sonst einen Namenskonflikt.
"
" Zweck: Die eigene Lagerwert-Rechnung aus Z_PURCHASING_LAGERWERT_ANALYSE
"        gegen die SAP-Transaktion MB5L gegenrechnen, OHNE die Zahl von Hand
"        abzulesen. Der Report ruft MB5L selbst auf und vergleicht.
"
" WARUM UEBERHAUPT: Die eigene Rueckrechnung ist bisher nur in sich stimmig
"        (zwei Stichtage, beide Rechenpfade geprueft). Ob sie FACHLICH richtig
"        ist, beweist erst der Vergleich mit dem SAP-Standard.
"
" WAS DIESER REPORT BEWUSST NICHT TUT: Er raet keinen Programmnamen und keine
"        Selektionsfeldnamen. Der Programmname kommt aus TSTC, die
"        Selektionsparameter aus der Selektionsbild-Definition des Programms.
"        Gefuellt wird nur, was dort auch wirklich existiert.
"
" ABLAUF
"   Stufe 1 (immer): Programmname zu MB5L ermitteln, Selektionsparameter
"                    auflisten, eigene Vergleichszahl aus MBEW rechnen.
"                    Rein lesend, kostet nichts.
"   Stufe 2 (p_run): MB5L per SUBMIT ausfuehren, Ergebnis einsammeln und die
"                    Summe gegen die eigene Zahl stellen.
"
" ERST OHNE p_run laufen lassen. Dann sieht man die Parameternamen und kann
" beurteilen, ob die Zuordnung unten passt, bevor ein grosser Lauf startet.
"
" MB5L ist eine reine Anzeigetransaktion; der SUBMIT aendert nichts.
"
" ABAP-Fallen, die hier vermieden werden: keine Arithmetik in WRITE, kein
" GROUP BY zusammen mit FOR ALL ENTRIES.
" ============================================================================

PARAMETERS:
  p_tcode TYPE tcode        DEFAULT 'MB5L',   " Transaktion, deren Programm gerufen wird
  p_bwkey TYPE mbew-bwkey   DEFAULT '1100',   " Bewertungskreis (CH = 1100, AT = 1200)
  p_run   AS CHECKBOX       DEFAULT ' ',      " erst ' ' zum Anschauen, dann 'X'
  p_zeil  TYPE i            DEFAULT 40.       " so viele Listzeilen am Ende zeigen

DATA: lv_prog TYPE progname.

START-OF-SELECTION.

  WRITE: / '############################################################'.
  WRITE: / '# MB5L-Abgleich Lagerwert'.
  WRITE: / '# System / Mandant :', sy-sysid, sy-mandt.
  WRITE: / '# Datum / Uhrzeit  :', sy-datum, sy-uzeit.
  WRITE: / '# Transaktion      :', p_tcode.
  WRITE: / '# Bewertungskreis  :', p_bwkey.
  WRITE: / '############################################################'.

" ----------------------------------------------------------------------------
" 1) Programmnamen zur Transaktion ermitteln (nicht raten)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 1) PROGRAMM HINTER DER TRANSAKTION ==='.

  SELECT SINGLE pgmna FROM tstc WHERE tcode = @p_tcode INTO @lv_prog.
  IF sy-subrc <> 0 OR lv_prog IS INITIAL.
    WRITE: / '  Kein Programm zu Transaktion', p_tcode, 'in TSTC gefunden.'.
    WRITE: / '  Moeglich: Transaktion ist ein Parameter-/Variantentransaktion.'.
    WRITE: / '  Dann in SE93 nachsehen, auf welches Programm sie zeigt.'.
    RETURN.
  ENDIF.
  WRITE: / '  Transaktion', p_tcode, '-> Programm', lv_prog.

" ----------------------------------------------------------------------------
" 2) Selektionsparameter des Programms auflisten
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 2) SELEKTIONSPARAMETER DES PROGRAMMS ==='.
  WRITE: / '  (S = Select-Option, P = Parameter)'.

  DATA lt_sel TYPE TABLE OF rsparams.
  CALL FUNCTION 'RS_REFRESH_FROM_SELECTOPTIONS'
    EXPORTING
      curr_report     = lv_prog
    TABLES
      selection_table = lt_sel
    EXCEPTIONS
      not_found       = 1
      no_report       = 2
      OTHERS          = 3.

  IF sy-subrc <> 0.
    WRITE: / '  Selektionsparameter nicht lesbar (sy-subrc =', sy-subrc, ').'.
    WRITE: / '  Ohne diese Liste kann der SUBMIT nicht sicher gebaut werden.'.
    WRITE: / '  Bitte das Selektionsbild in SE38 ansehen (F1 je Feld zeigt den'.
    WRITE: / '  technischen Namen) und zurueckmelden.'.
    RETURN.
  ENDIF.

  DATA lv_par_cnt TYPE i.
  LOOP AT lt_sel INTO DATA(ls_sel).
    lv_par_cnt = lv_par_cnt + 1.
    WRITE: / '  ', ls_sel-kind, ls_sel-selname.
  ENDLOOP.
  WRITE: / '  Anzahl Selektionsfelder:', lv_par_cnt.

  IF lv_par_cnt = 0.
    WRITE: / '  Keine Selektionsfelder gefunden - SUBMIT waere blind. Abbruch.'.
    RETURN.
  ENDIF.

" ----------------------------------------------------------------------------
" 3) Eigene Vergleichszahl aus MBEW (gleiche Basis wie der Lagerwert-Report)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 3) EIGENE VERGLEICHSZAHL AUS MBEW ==='.

  DATA: lv_eigen_alle   TYPE p LENGTH 16 DECIMALS 2,
        lv_eigen_ziel   TYPE p LENGTH 16 DECIMALS 2,
        lv_cnt_alle     TYPE i,
        lv_cnt_ziel     TYPE i.

  " Disponenten der Zielgruppe (Armins Abgrenzung).
  DATA lt_ziel TYPE STANDARD TABLE OF marc-dispo.
  APPEND '001' TO lt_ziel.
  APPEND '002' TO lt_ziel.
  APPEND '003' TO lt_ziel.
  APPEND '004' TO lt_ziel.
  APPEND '005' TO lt_ziel.

  SELECT matnr, dispo FROM marc
    WHERE werks = @p_bwkey
    INTO TABLE @DATA(lt_marc).

  TYPES: BEGIN OF ty_md,
           matnr TYPE marc-matnr,
           dispo TYPE marc-dispo,
         END OF ty_md.
  DATA: lt_md TYPE SORTED TABLE OF ty_md WITH UNIQUE KEY matnr,
        ls_md TYPE ty_md.
  LOOP AT lt_marc INTO DATA(ls_marc).
    CLEAR ls_md.
    ls_md-matnr = ls_marc-matnr.
    ls_md-dispo = ls_marc-dispo.
    INSERT ls_md INTO TABLE lt_md.
  ENDLOOP.

  SELECT matnr, salk3 FROM mbew
    WHERE bwkey = @p_bwkey
    INTO TABLE @DATA(lt_mbew).

  LOOP AT lt_mbew INTO DATA(ls_mbew).
    lv_eigen_alle = lv_eigen_alle + ls_mbew-salk3.
    lv_cnt_alle   = lv_cnt_alle + 1.

    READ TABLE lt_md INTO ls_md WITH TABLE KEY matnr = ls_mbew-matnr.
    IF sy-subrc <> 0.
      CONTINUE.
    ENDIF.
    READ TABLE lt_ziel TRANSPORTING NO FIELDS WITH KEY table_line = ls_md-dispo.
    IF sy-subrc = 0.
      lv_eigen_ziel = lv_eigen_ziel + ls_mbew-salk3.
      lv_cnt_ziel   = lv_cnt_ziel + 1.
    ENDIF.
  ENDLOOP.

  WRITE: / '  Alle Materialien im Bewertungskreis:', lv_cnt_alle.
  WRITE: / '  SALK3-Summe GESAMT                 :', lv_eigen_alle.
  WRITE: / '  Materialien Disponenten 001-005    :', lv_cnt_ziel.
  WRITE: / '  SALK3-Summe 001-005                :', lv_eigen_ziel.
  WRITE: / '  Hinweis: MB5L kennt keinen Disponentenfilter. Der belastbare'.
  WRITE: / '  Vergleich ist deshalb die GESAMTSUMME. Stimmt die, ist die'.
  WRITE: / '  Bewertungslogik (SALK3) bestaetigt; die Disponentenabgrenzung'.
  WRITE: / '  ist danach nur noch ein Filter auf derselben Basis.'.

" ----------------------------------------------------------------------------
" 4) MB5L ausfuehren und Summe einsammeln
" ----------------------------------------------------------------------------
  IF p_run <> 'X'.
    ULINE.
    WRITE: / '=== 4) MB5L-LAUF UEBERSPRUNGEN ==='.
    WRITE: / '  p_run ist nicht gesetzt. Bitte zuerst die Parameterliste aus'.
    WRITE: / '  Abschnitt 2 pruefen, danach mit p_run = X erneut starten.'.
    RETURN.
  ENDIF.

  ULINE.
  WRITE: / '=== 4) MB5L-LAUF ==='.

  " Selektionstabelle fuellen - AUSSCHLIESSLICH mit Feldern, die laut
  " Abschnitt 2 wirklich existieren. Unbekannte Namen werden nicht erfunden.
  DATA: lt_run  TYPE TABLE OF rsparams,
        ls_run  TYPE rsparams,
        lv_hit  TYPE abap_bool.

  LOOP AT lt_sel INTO ls_sel.
    CLEAR ls_run.
    ls_run-selname = ls_sel-selname.
    ls_run-kind    = ls_sel-kind.

    " Bewertungskreis setzen. Der Feldname kann je nach Release BWKEY oder
    " (bei aelteren Staenden) anders heissen - deshalb Vergleich statt Annahme.
    IF ls_sel-selname = 'BWKEY'.
      ls_run-sign   = 'I'.
      ls_run-option = 'EQ'.
      ls_run-low    = p_bwkey.
      APPEND ls_run TO lt_run.
      lv_hit = abap_true.
      WRITE: / '  Selektion gesetzt: BWKEY =', p_bwkey.
    ENDIF.
  ENDLOOP.

  IF lv_hit = abap_false.
    WRITE: / '  ACHTUNG: Kein Feld BWKEY im Selektionsbild gefunden.'.
    WRITE: / '  MB5L laeuft dann OHNE Einschraenkung auf den Bewertungskreis;'.
    WRITE: / '  die Summe waere nicht vergleichbar. Bitte den richtigen'.
    WRITE: / '  Feldnamen aus Abschnitt 2 melden, dann wird er hier ergaenzt.'.
    RETURN.
  ENDIF.

  " ALV-Ergebnis abgreifen. MB5L ist ALV-basiert; ein reines
  " "EXPORTING LIST TO MEMORY" liefert dabei oft nichts Brauchbares.
  " cl_salv_bs_runtime_info faengt die Daten direkt ab, ohne Bildschirmausgabe.
  cl_salv_bs_runtime_info=>set(
    EXPORTING
      display  = abap_false
      metadata = abap_false
      data     = abap_true ).

  SUBMIT (lv_prog) WITH SELECTION-TABLE lt_run AND RETURN.

  DATA lr_data TYPE REF TO data.
  FIELD-SYMBOLS <lt_alv> TYPE ANY TABLE.

  TRY.
      cl_salv_bs_runtime_info=>get_data_ref( IMPORTING r_data = lr_data ).
      ASSIGN lr_data->* TO <lt_alv>.
    CATCH cx_salv_bs_sc_runtime_info.
      WRITE: / '  ALV-Daten konnten nicht abgegriffen werden.'.
      WRITE: / '  Der Report laeuft vermutlich im klassischen Listmodus.'.
      WRITE: / '  Dann den Wert bitte einmal von Hand aus MB5L ablesen.'.
  ENDTRY.
  cl_salv_bs_runtime_info=>clear_all( ).

  IF <lt_alv> IS NOT ASSIGNED.
    WRITE: / '  Kein Ergebnis eingesammelt - Abgleich nicht moeglich.'.
    RETURN.
  ENDIF.

  DATA(lv_rows) = lines( <lt_alv> ).
  WRITE: / '  Von MB5L gelieferte Zeilen:', lv_rows.

  IF lv_rows = 0.
    WRITE: / '  MB5L hat keine Zeilen geliefert. Selektion pruefen.'.
    RETURN.
  ENDIF.

  " Feldnamen der ersten Zeile zeigen, damit sichtbar ist, was MB5L liefert
  " und auf welchem Feld summiert wird.
  WRITE: / '--- Felder der Ergebniszeile ---'.
  LOOP AT <lt_alv> ASSIGNING FIELD-SYMBOL(<ls_first>).
    DATA(lo_struc) = CAST cl_abap_structdescr(
      cl_abap_typedescr=>describe_by_data( <ls_first> ) ).
    LOOP AT lo_struc->components INTO DATA(ls_comp).
      WRITE: / '    ', ls_comp-name.
    ENDLOOP.
    EXIT.
  ENDLOOP.

  " Summe ueber das Wertfeld bilden. SALK3 ist der Bestandswert; falls MB5L
  " das Feld anders benennt, wird das oben sichtbar und hier gemeldet.
  DATA: lv_mb5l_sum TYPE p LENGTH 16 DECIMALS 2,
        lv_found    TYPE abap_bool.

  LOOP AT <lt_alv> ASSIGNING FIELD-SYMBOL(<ls_row>).
    ASSIGN COMPONENT 'SALK3' OF STRUCTURE <ls_row> TO FIELD-SYMBOL(<lv_val>).
    IF sy-subrc = 0.
      lv_found    = abap_true.
      lv_mb5l_sum = lv_mb5l_sum + <lv_val>.
    ENDIF.
  ENDLOOP.

  IF lv_found = abap_false.
    WRITE: / '  Kein Feld SALK3 im Ergebnis. Bitte aus der Feldliste oben den'.
    WRITE: / '  richtigen Wertfeldnamen melden, dann wird er hier eingesetzt.'.
    RETURN.
  ENDIF.

" ----------------------------------------------------------------------------
" 5) Gegenueberstellung
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 5) ERGEBNIS DES ABGLEICHS ==='.
  DATA lv_diff TYPE p LENGTH 16 DECIMALS 2.
  lv_diff = lv_mb5l_sum - lv_eigen_alle.

  WRITE: / '  MB5L-Summe (SALK3)        :', lv_mb5l_sum.
  WRITE: / '  Eigene Summe aus MBEW     :', lv_eigen_alle.
  WRITE: / '  Differenz (MB5L - eigene) :', lv_diff.

  IF lv_diff = 0.
    WRITE: / '  ERGEBNIS: IDENTISCH. Die Bewertungslogik ist bestaetigt.'.
    WRITE: / '  Damit ist auch die Disponentenabgrenzung 001-005 belastbar,'.
    WRITE: / '  weil sie nur ein Filter auf derselben Basis ist:'.
    WRITE: / '  Lagerwert Einkaufsteile =', lv_eigen_ziel.
  ELSE.
    WRITE: / '  ERGEBNIS: ABWEICHUNG. Nicht als bestaetigt behandeln.'.
    WRITE: / '  Uebliche Ursachen, in dieser Reihenfolge pruefen:'.
    WRITE: / '   - MB5L zeigt Sonderbestaende (Konsignation, Kundenauftrag,'.
    WRITE: / '     Projekt) mit an; MBEW allein fuehrt nur den Eigenbestand.'.
    WRITE: / '   - Bewertungsart (BWTAR) / getrennte Bewertung.'.
    WRITE: / '   - MB5L-Variante schraenkt anders ein als hier gesetzt.'.
  ENDIF.

  ULINE.
  WRITE: / '=== ENDE. Bitte komplette Ausgabe zurueckgeben. ==='.
