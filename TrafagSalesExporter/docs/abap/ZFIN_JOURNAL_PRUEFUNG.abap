*&---------------------------------------------------------------------*
*& Report  ZFIN_JOURNAL_PRUEFUNG
*&---------------------------------------------------------------------*
*& Zweck : Vorpruefung (NUR LESEND) fuer das geplante Journal-EntitySet
*&         im Gateway-Service ZPOWERBI_EINKAUF_SRV.
*&
*&         Das Finance Dashboard hat den CH/AT-Journalleser seit dem
*&         2026-09-08 produktiv, es fehlt nur die OData-Quelle. Bevor das
*&         EntitySet gebaut wird, beantwortet dieser Report an der Quelle:
*&
*&           1. Welche Buchungskreise sind CH bzw. AT, welche Hauswaehrung
*&              und welcher Kontenplan?
*&           2. Gibt es ueberhaupt Belege im gewuenschten Zeitraum?
*&           3. Wie gut sind die 22 benoetigten Felder gefuellt? Besonders
*&              PRCTR, KOSTL, SGTXT, AUGDT, AUGBL und die Grundlage fuer
*&              das Nettofaelligkeitsdatum (ZFBDT plus ZBD1T/ZBD2T/ZBD3T).
*&           4. Liefert SKAT zu jedem bebuchten Sachkonto einen Text?
*&           5. Stichprobe echter Zeilen zur Sichtkontrolle.
*&
*& Vorgabe Andreas, 2026-09-10: Die Schweiz hat sehr viele Zeitbuchungen,
*& ueber 5 Mio Positionen je Jahr. Da CH und AT gleich laufen werden, wird
*& ZUERST nur Oesterreich umgesetzt, Buchungskreis 1200. Deshalb steht 1200
*& als Vorgabe in der Selektion. Zeitbuchungen laufen als Belegart CO und
*& werden ueber das Ankreuzfeld standardmaessig weggelassen.
*&
*& WICHTIG: Der Report schreibt NICHTS. Nur SELECTs und WRITEs.
*&
*& Hintergrund, warum die vorhandenen EntitySets nicht taugen:
*&   FinanzdataSchweizOeSet = Faktura (Vbeln/Posnr/Matnr), kein Journal
*&   bkpfSet                = nur Belegkoepfe, ohne Buzei kein Journal
*&   bsisSet                = nur OFFENE Posten, damit stimmt kein Saldo
*&
*& Kontakt: Ingo Kohler (IT Analytics)
*& Stand  : 2026-09-10
*&---------------------------------------------------------------------*
REPORT zfin_journal_pruefung.

TABLES: t001, bkpf.

*----------------------------------------------------------------------*
* Selektion
*----------------------------------------------------------------------*
SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE tit01.
SELECT-OPTIONS: s_bukrs FOR t001-bukrs DEFAULT '1200'.  " Vorgabe AT, siehe unten
SELECT-OPTIONS: s_gjahr FOR bkpf-gjahr.   " z.B. 2025 bis 2026
SELECT-OPTIONS: s_blart FOR bkpf-blart.   " leer = alle Belegarten
SELECTION-SCREEN END OF BLOCK b1.

SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE tit02.
PARAMETERS: p_stich TYPE i DEFAULT 5.     " Stichprobenzeilen
PARAMETERS: p_oco   TYPE abap_bool AS CHECKBOX DEFAULT 'X'. " CO-Belege weglassen
SELECTION-SCREEN END OF BLOCK b2.

* Die Titelvariablen tit01 und tit02 werden von SELECTION-SCREEN selbst
* deklariert. Eine eigene DATA-Deklaration dafuer bricht mit
* "TIT01 was already declared" ab, und zwar erst zur LAUFZEIT beim Aufbau
* des Selektionsbildes, nicht beim Aktivieren. Gefunden am 2026-09-10.
INITIALIZATION.
  tit01 = 'Selektion'.
  tit02 = 'Ausgabe'.

*----------------------------------------------------------------------*
* Typen
*----------------------------------------------------------------------*
TYPES: BEGIN OF ty_kopf,
         bukrs TYPE bkpf-bukrs,
         belnr TYPE bkpf-belnr,
         gjahr TYPE bkpf-gjahr,
         budat TYPE bkpf-budat,
         monat TYPE bkpf-monat,
         blart TYPE bkpf-blart,
         xblnr TYPE bkpf-xblnr,
         stblg TYPE bkpf-stblg,
         hwaer TYPE bkpf-hwaer,
         waers TYPE bkpf-waers,
       END OF ty_kopf.

TYPES: BEGIN OF ty_pos,
         bukrs TYPE bseg-bukrs,
         belnr TYPE bseg-belnr,
         gjahr TYPE bseg-gjahr,
         buzei TYPE bseg-buzei,
         hkont TYPE bseg-hkont,
         shkzg TYPE bseg-shkzg,
         dmbtr TYPE bseg-dmbtr,
         wrbtr TYPE bseg-wrbtr,
         kostl TYPE bseg-kostl,
         prctr TYPE bseg-prctr,
         sgtxt TYPE bseg-sgtxt,
         zfbdt TYPE bseg-zfbdt,
         zbd1t TYPE bseg-zbd1t,
         augdt TYPE bseg-augdt,
         augbl TYPE bseg-augbl,
       END OF ty_pos.

DATA: gt_kopf TYPE STANDARD TABLE OF ty_kopf,
      gt_pos  TYPE STANDARD TABLE OF ty_pos,
      gs_kopf TYPE ty_kopf,
      gs_pos  TYPE ty_pos.

DATA: gv_zeilen  TYPE i,
      gv_prozent TYPE p DECIMALS 1.

*----------------------------------------------------------------------*
* Hilfsroutine: Fuellgrad einer Spalte ausgeben
*----------------------------------------------------------------------*
DEFINE zeige_fuellgrad.
  IF gv_zeilen > 0.
    gv_prozent = &2 * 100 / gv_zeilen.
  ELSE.
    gv_prozent = 0.
  ENDIF.
  WRITE: / &1, 30 &2, 45 gv_zeilen, 60 gv_prozent, '%'.
END-OF-DEFINITION.

*----------------------------------------------------------------------*
START-OF-SELECTION.

  WRITE: / '=================================================='.
  WRITE: / 'ZFIN_JOURNAL_PRUEFUNG  -  nur lesend'.
  WRITE: / 'System:', sy-sysid, ' Mandant:', sy-mandt, ' Datum:', sy-datum.
  WRITE: / '=================================================='.

*----------------------------------------------------------------------*
* Teil 1: Buchungskreise
*----------------------------------------------------------------------*
  WRITE: / ''.
  WRITE: / '--- Teil 1: Buchungskreise, Waehrung, Kontenplan ---'.
  WRITE: / 'BUKRS', 10 'Bezeichnung', 45 'Land', 52 'Waehrung', 63 'KTOPL'.

  DATA: lt_t001 TYPE STANDARD TABLE OF t001,
        ls_t001 TYPE t001.

  SELECT * FROM t001 INTO TABLE lt_t001
    WHERE bukrs IN s_bukrs
    ORDER BY bukrs.

  LOOP AT lt_t001 INTO ls_t001.
    WRITE: / ls_t001-bukrs, 10 ls_t001-butxt, 45 ls_t001-land1,
             52 ls_t001-waers, 63 ls_t001-ktopl.
  ENDLOOP.

  WRITE: / 'Buchungskreise gesamt:', sy-tfill.

*----------------------------------------------------------------------*
* Teil 2: Belegkoepfe im Zeitraum
*----------------------------------------------------------------------*
  WRITE: / ''.
  WRITE: / '--- Teil 2: Belegkoepfe BKPF im gewaehlten Zeitraum ---'.

  SELECT bukrs belnr gjahr budat monat blart xblnr stblg hwaer waers
    FROM bkpf INTO TABLE gt_kopf
    WHERE bukrs IN s_bukrs
      AND gjahr IN s_gjahr
      AND blart IN s_blart.

  " Andreas am 2026-09-10: die Schweiz hat ueber 5 Mio Zeitbuchungszeilen je Jahr.
  " Sie laufen als CO-Belege und wuerden das EntitySet erschlagen. Fuer den ersten
  " Entwurf bleiben sie draussen; die Kennzahl unten zeigt, wie viel das ausmacht.
  IF p_oco = abap_true.
    DELETE gt_kopf WHERE blart = 'CO'.
  ENDIF.

  DESCRIBE TABLE gt_kopf LINES gv_zeilen.
  WRITE: / 'Belegkoepfe gefunden:', gv_zeilen.

  IF gv_zeilen = 0.
    WRITE: / 'ABBRUCH: keine Belege im Zeitraum. Selektion pruefen.'.
    RETURN.
  ENDIF.

  " Belegarten zaehlen, damit die IsManual-Annahme Blart = SA belegbar wird
  WRITE: / ''.
  WRITE: / 'Belegarten (BLART):'.
  DATA: lt_blart TYPE SORTED TABLE OF ty_kopf
                 WITH NON-UNIQUE KEY blart,
        lv_blart TYPE bkpf-blart,
        lv_anz   TYPE i.
  lt_blart = gt_kopf.
  LOOP AT lt_blart INTO gs_kopf.
    IF gs_kopf-blart <> lv_blart AND lv_blart IS NOT INITIAL.
      WRITE: / '  ', lv_blart, 15 lv_anz.
      lv_anz = 0.
    ENDIF.
    lv_blart = gs_kopf-blart.
    lv_anz = lv_anz + 1.
  ENDLOOP.
  IF lv_blart IS NOT INITIAL.
    WRITE: / '  ', lv_blart, 15 lv_anz.
  ENDIF.

*----------------------------------------------------------------------*
* Teil 3: Belegpositionen und Fuellgrade
*----------------------------------------------------------------------*
  WRITE: / ''.
  WRITE: / '--- Teil 3: Belegpositionen BSEG und Fuellgrade ---'.

  SELECT bukrs belnr gjahr buzei hkont shkzg dmbtr wrbtr
         kostl prctr sgtxt zfbdt zbd1t augdt augbl
    FROM bseg INTO TABLE gt_pos
    FOR ALL ENTRIES IN gt_kopf
    WHERE bukrs = gt_kopf-bukrs
      AND belnr = gt_kopf-belnr
      AND gjahr = gt_kopf-gjahr.

  DESCRIBE TABLE gt_pos LINES gv_zeilen.
  WRITE: / 'Belegpositionen gefunden:', gv_zeilen.
  WRITE: / ''.
  WRITE: / 'Feld', 30 'gefuellt', 45 'von', 60 'Anteil'.

  " Pflichtfelder des Lesers, die leer sein koennen
  DATA: lv_kostl TYPE i, lv_prctr TYPE i, lv_sgtxt TYPE i,
        lv_zfbdt TYPE i, lv_zbd1t TYPE i,
        lv_augdt TYPE i, lv_augbl TYPE i,
        lv_wrbtr TYPE i, lv_shkzg_s TYPE i, lv_shkzg_h TYPE i.

  LOOP AT gt_pos INTO gs_pos.
    IF gs_pos-kostl IS NOT INITIAL. lv_kostl = lv_kostl + 1. ENDIF.
    IF gs_pos-prctr IS NOT INITIAL. lv_prctr = lv_prctr + 1. ENDIF.
    IF gs_pos-sgtxt IS NOT INITIAL. lv_sgtxt = lv_sgtxt + 1. ENDIF.
    IF gs_pos-zfbdt IS NOT INITIAL. lv_zfbdt = lv_zfbdt + 1. ENDIF.
    IF gs_pos-zbd1t IS NOT INITIAL. lv_zbd1t = lv_zbd1t + 1. ENDIF.
    IF gs_pos-augdt IS NOT INITIAL. lv_augdt = lv_augdt + 1. ENDIF.
    IF gs_pos-augbl IS NOT INITIAL. lv_augbl = lv_augbl + 1. ENDIF.
    IF gs_pos-wrbtr IS NOT INITIAL. lv_wrbtr = lv_wrbtr + 1. ENDIF.
    IF gs_pos-shkzg = 'S'. lv_shkzg_s = lv_shkzg_s + 1. ENDIF.
    IF gs_pos-shkzg = 'H'. lv_shkzg_h = lv_shkzg_h + 1. ENDIF.
  ENDLOOP.

  zeige_fuellgrad 'KOSTL (Kostenstelle)'  lv_kostl.
  zeige_fuellgrad 'PRCTR (Profitcenter)'  lv_prctr.
  zeige_fuellgrad 'SGTXT (Buchungstext)'  lv_sgtxt.
  zeige_fuellgrad 'ZFBDT (Basisdatum)'    lv_zfbdt.
  zeige_fuellgrad 'ZBD1T (Zahlungsziel)'  lv_zbd1t.
  zeige_fuellgrad 'AUGDT (Ausgleichsdat)' lv_augdt.
  zeige_fuellgrad 'AUGBL (Ausgleichsbel)' lv_augbl.
  zeige_fuellgrad 'WRBTR (Betrag TW)'     lv_wrbtr.

  WRITE: / ''.
  WRITE: / 'Soll-/Haben-Verteilung: S =', lv_shkzg_s, ' H =', lv_shkzg_h.
  WRITE: / 'HINWEIS: Faedt ist KEIN Tabellenfeld. Das Nettofaelligkeits-'.
  WRITE: / 'datum wird aus ZFBDT plus Zahlungsbedingung berechnet.'.
  WRITE: / 'Ist ZFBDT schlecht gefuellt, bleibt DueDate im Dashboard leer.'.

*----------------------------------------------------------------------*
* Teil 4: Kontotexte aus SKAT
*----------------------------------------------------------------------*
  WRITE: / ''.
  WRITE: / '--- Teil 4: Kontotext SKAT je bebuchtem Sachkonto ---'.

  DATA: lt_konten TYPE SORTED TABLE OF bseg-hkont
                  WITH UNIQUE KEY table_line,
        lv_ohne   TYPE i,
        lv_mit    TYPE i,
        lv_txt50  TYPE skat-txt50,
        lv_ktopl  TYPE t001-ktopl.

  LOOP AT gt_pos INTO gs_pos.
    INSERT gs_pos-hkont INTO TABLE lt_konten.
  ENDLOOP.

  " Kontenplan des ersten selektierten Buchungskreises
  READ TABLE lt_t001 INTO ls_t001 INDEX 1.
  lv_ktopl = ls_t001-ktopl.
  WRITE: / 'Geprueft gegen Kontenplan:', lv_ktopl, ' Sprache: D'.

  DATA: lv_konto TYPE bseg-hkont.
  LOOP AT lt_konten INTO lv_konto.
    CLEAR lv_txt50.
    SELECT SINGLE txt50 FROM skat INTO lv_txt50
      WHERE spras = 'D' AND ktopl = lv_ktopl AND saknr = lv_konto.
    IF sy-subrc = 0 AND lv_txt50 IS NOT INITIAL.
      lv_mit = lv_mit + 1.
    ELSE.
      lv_ohne = lv_ohne + 1.
      IF lv_ohne <= 10.
        WRITE: / '  ohne Text:', lv_konto.
      ENDIF.
    ENDIF.
  ENDLOOP.

  " WRITE vertraegt keinen Rechenausdruck, deshalb vorher summieren.
  DATA lv_konten_gesamt TYPE i.
  lv_konten_gesamt = lv_mit + lv_ohne.
  WRITE: / 'Sachkonten bebucht:', lv_konten_gesamt,
         ' mit Text:', lv_mit, ' ohne Text:', lv_ohne.

*----------------------------------------------------------------------*
* Teil 5: Stichprobe
*----------------------------------------------------------------------*
  WRITE: / ''.
  WRITE: / '--- Teil 5: Stichprobe echter Journalzeilen ---'.
  WRITE: / 'BUKRS BELNR      GJAHR BUZEI HKONT      S/H     DMBTR PRCTR'.

  DATA: lv_i TYPE i.
  LOOP AT gt_pos INTO gs_pos.
    lv_i = lv_i + 1.
    IF lv_i > p_stich. EXIT. ENDIF.
    WRITE: / gs_pos-bukrs, 7 gs_pos-belnr, 18 gs_pos-gjahr,
             24 gs_pos-buzei, 30 gs_pos-hkont, 41 gs_pos-shkzg,
             45 gs_pos-dmbtr, 60 gs_pos-prctr.
  ENDLOOP.

  WRITE: / ''.
  WRITE: / '=== Ende. Ausgabe bitte an Ingo geben. ==='.
