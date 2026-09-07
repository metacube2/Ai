REPORT ztest_powerbi_ekpo_eket.

" Kleines Diagnoseprogramm fuer die SAP Einkaufs-OData-Tabellen.
" Ziel:
" - Pruefen, ob EKKO, EKPO und EKET direkt in SAP Daten liefern.
" - Pruefen, ob zu einem EKKO-Beleg passende EKPO/EKET-Zeilen existieren.
" - Ausgabe per WRITE, damit sie einfach kopiert werden kann.

PARAMETERS:
  p_ebeln TYPE ekko-ebeln,
  p_bedat TYPE ekko-bedat DEFAULT '20260101',
  p_max   TYPE i DEFAULT 20.

DATA:
  lv_ebeln       TYPE ekko-ebeln,
  lv_ekko_count  TYPE i,
  lv_ekpo_count  TYPE i,
  lv_eket_count  TYPE i,
  lt_ekko        TYPE STANDARD TABLE OF ekko,
  lt_ekpo        TYPE STANDARD TABLE OF ekpo,
  lt_eket        TYPE STANDARD TABLE OF eket.

START-OF-SELECTION.

  lv_ebeln = p_ebeln.
  IF lv_ebeln IS NOT INITIAL.
    lv_ebeln = |{ lv_ebeln ALPHA = IN }|.
  ENDIF.

  WRITE: / '=== Einkaufsdaten Test EKKO / EKPO / EKET ==='.
  WRITE: / 'Input EBELN:', p_ebeln, 'ALPHA:', lv_ebeln.
  WRITE: / 'Input BEDAT ab:', p_bedat.
  WRITE: / 'Max Zeilen:', p_max.
  ULINE.

  " 1) Grundzaehlung ohne Join
  SELECT COUNT( * )
    FROM ekko
    INTO @lv_ekko_count
    WHERE bedat >= @p_bedat.

  SELECT COUNT( * )
    FROM ekpo
    INTO @lv_ekpo_count.

  SELECT COUNT( * )
    FROM eket
    INTO @lv_eket_count.

  WRITE: / 'COUNT EKKO ab BEDAT:', lv_ekko_count.
  WRITE: / 'COUNT EKPO gesamt  :', lv_ekpo_count.
  WRITE: / 'COUNT EKET gesamt  :', lv_eket_count.
  ULINE.

  " 2) Beispiel-EKKO suchen, falls kein Beleg mitgegeben wurde
  IF lv_ebeln IS INITIAL.
    SELECT *
      FROM ekko
      WHERE bedat >= @p_bedat
      ORDER BY bedat DESCENDING, ebeln DESCENDING
      INTO TABLE @lt_ekko
      UP TO 1 ROWS.

    READ TABLE lt_ekko INDEX 1 INTO DATA(ls_first_ekko).
    IF sy-subrc = 0.
      lv_ebeln = ls_first_ekko-ebeln.
      WRITE: / 'Kein EBELN mitgegeben, verwende ersten EKKO-Beleg:', lv_ebeln.
    ELSE.
      WRITE: / 'Kein EKKO-Beleg ab BEDAT gefunden.'.
      RETURN.
    ENDIF.
  ENDIF.

  ULINE.
  WRITE: / '=== Detailtest fuer EBELN ===', lv_ebeln.

  " 3) EKKO Detail
  CLEAR lt_ekko.
  SELECT *
    FROM ekko
    WHERE ebeln = @lv_ebeln
    INTO TABLE @lt_ekko
    UP TO @p_max ROWS.

  WRITE: / 'EKKO Zeilen fuer EBELN:', lines( lt_ekko ).
  LOOP AT lt_ekko INTO DATA(ls_ekko).
    WRITE: / 'EKKO',
             'EBELN=', ls_ekko-ebeln,
             'BEDAT=', ls_ekko-bedat,
             'AEDAT=', ls_ekko-aedat,
             'LIFNR=', ls_ekko-lifnr,
             'BUKRS=', ls_ekko-bukrs,
             'BSART=', ls_ekko-bsart.
  ENDLOOP.

  ULINE.

  " 4) EKPO Detail
  CLEAR lt_ekpo.
  SELECT *
    FROM ekpo
    WHERE ebeln = @lv_ebeln
    ORDER BY ebeln, ebelp
    INTO TABLE @lt_ekpo
    UP TO @p_max ROWS.

  WRITE: / 'EKPO Zeilen fuer EBELN:', lines( lt_ekpo ).
  LOOP AT lt_ekpo INTO DATA(ls_ekpo).
    WRITE: / 'EKPO',
             'EBELN=', ls_ekpo-ebeln,
             'EBELP=', ls_ekpo-ebelp,
             'MATNR=', ls_ekpo-matnr,
             'MATKL=', ls_ekpo-matkl,
             'MENGE=', ls_ekpo-menge,
             'MEINS=', ls_ekpo-meins,
             'NETWR=', ls_ekpo-netwr,
             'LOEKZ=', ls_ekpo-loekz.
  ENDLOOP.

  ULINE.

  " 5) EKET Detail
  CLEAR lt_eket.
  SELECT *
    FROM eket
    WHERE ebeln = @lv_ebeln
    ORDER BY ebeln, ebelp, etenr
    INTO TABLE @lt_eket
    UP TO @p_max ROWS.

  WRITE: / 'EKET Zeilen fuer EBELN:', lines( lt_eket ).
  LOOP AT lt_eket INTO DATA(ls_eket).
    WRITE: / 'EKET',
             'EBELN=', ls_eket-ebeln,
             'EBELP=', ls_eket-ebelp,
             'ETENR=', ls_eket-etenr,
             'EINDT=', ls_eket-eindt,
             'MENGE=', ls_eket-menge,
             'WEMNG=', ls_eket-wemng.
  ENDLOOP.

  ULINE.

  " 6) Join-Pruefung: existieren EKPO/EKET zu den aktuellen EKKO-Belegen?
  SELECT COUNT( * )
    FROM ekko AS h
    INNER JOIN ekpo AS p
      ON p~ebeln = h~ebeln
    INTO @lv_ekpo_count
    WHERE h~bedat >= @p_bedat.

  SELECT COUNT( * )
    FROM ekko AS h
    INNER JOIN eket AS e
      ON e~ebeln = h~ebeln
    INTO @lv_eket_count
    WHERE h~bedat >= @p_bedat.

  WRITE: / 'JOIN EKKO->EKPO ab BEDAT:', lv_ekpo_count.
  WRITE: / 'JOIN EKKO->EKET ab BEDAT:', lv_eket_count.

  ULINE.
  WRITE: / 'Interpretation:'.
  WRITE: / '- Wenn EKPO/EKET gesamt > 0, aber fuer EBELN 0: Beleg hat keine Positionen/Termine oder falscher Beleg.'.
  WRITE: / '- Wenn JOIN ab BEDAT > 0, dann muss OData EKPO/EKET mit korrektem SELECT auch Daten liefern.'.
  WRITE: / '- Wenn JOIN ab BEDAT = 0, dann gibt es fuer aktuelle EKKO-Belege keine EKPO/EKET-Zuordnung im getesteten Zeitraum.'.
