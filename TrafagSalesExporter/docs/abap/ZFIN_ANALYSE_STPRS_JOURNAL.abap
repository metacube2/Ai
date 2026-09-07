*&---------------------------------------------------------------------*
*& Report  ZFIN_ANALYSE_STPRS_JOURNAL
*&---------------------------------------------------------------------*
*& Zweck : Analyse-Report (NUR LESEND) fuer das Trafag BI/Finance Dashboard.
*&         Beantwortet die offenen Fragen zu
*&           (A) Standardpreis / Kostenbasis CH+AT  -> Gruppenmarge
*&           (B) Hauptbuch-Journal CH+AT            -> Journal Import
*&           (C) Datenverfuegbarkeit 2026           -> fehlende Umsatzzeilen
*&
*& WICHTIG: Der Report schreibt NICHTS. Nur SELECTs und WRITEs.
*&          Bitte auf dem PRODUKTIVsystem (travp762) ausfuehren,
*&          nicht auf travt762.
*&
*& Kontakt: Ingo Kohler (IT Analytics)
*& Stand  : 2026-07-14
*&---------------------------------------------------------------------*
REPORT zfin_analyse_stprs_journal.

TABLES: t001, mbew, vbrk, bkpf.

*----------------------------------------------------------------------*
* Selektion
*----------------------------------------------------------------------*
SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME TITLE text-t01.
SELECT-OPTIONS: s_bukrs FOR t001-bukrs.          " leer = alle Buchungskreise
SELECT-OPTIONS: s_gjahr FOR bkpf-gjahr.          " z.B. 2025 bis 2026
SELECTION-SCREEN END OF BLOCK b1.

SELECTION-SCREEN BEGIN OF BLOCK b2 WITH FRAME TITLE text-t02.
PARAMETERS: p_abschn TYPE i DEFAULT 4.           " Detailzeilen je Stichprobe
SELECTION-SCREEN END OF BLOCK b2.

*----------------------------------------------------------------------*
* Typen
*----------------------------------------------------------------------*
TYPES: BEGIN OF ty_bukrs,
         bukrs TYPE t001-bukrs,
         butxt TYPE t001-butxt,
         waers TYPE t001-waers,
         land1 TYPE t001-land1,
         ktopl TYPE t001-ktopl,
       END OF ty_bukrs.

TYPES: BEGIN OF ty_werk,
         werks TYPE t001w-werks,
         name1 TYPE t001w-name1,
         bwkey TYPE t001w-bwkey,
       END OF ty_werk.

TYPES: BEGIN OF ty_bwk,
         bwkey TYPE t001k-bwkey,
         bukrs TYPE t001k-bukrs,
       END OF ty_bwk.

TYPES: BEGIN OF ty_mbew,
         matnr TYPE mbew-matnr,
         bwkey TYPE mbew-bwkey,
         vprsv TYPE mbew-vprsv,
         stprs TYPE mbew-stprs,
         verpr TYPE mbew-verpr,
         peinh TYPE mbew-peinh,
         bklas TYPE mbew-bklas,
       END OF ty_mbew.

TYPES: BEGIN OF ty_sold,
         vbeln TYPE vbrp-vbeln,
         posnr TYPE vbrp-posnr,
         matnr TYPE vbrp-matnr,
         werks TYPE vbrp-werks,
         fkimg TYPE vbrp-fkimg,
         netwr TYPE vbrp-netwr,
         wavwr TYPE vbrp-wavwr,     " Kostenwert der Fakturaposition
         bukrs TYPE vbrk-bukrs,
         fkdat TYPE vbrk-fkdat,
         waerk TYPE vbrk-waerk,
       END OF ty_sold.

DATA: gt_bukrs   TYPE STANDARD TABLE OF ty_bukrs,
      gs_bukrs   TYPE ty_bukrs,
      gt_werk    TYPE STANDARD TABLE OF ty_werk,
      gs_werk    TYPE ty_werk,
      gt_bwk     TYPE STANDARD TABLE OF ty_bwk,
      gs_bwk     TYPE ty_bwk,
      gt_mbew    TYPE STANDARD TABLE OF ty_mbew,
      gs_mbew    TYPE ty_mbew,
      gt_sold    TYPE STANDARD TABLE OF ty_sold,
      gs_sold    TYPE ty_sold.

" Alle relevanten MBEW-Saetze EINMAL lesen und sortiert halten.
" Wichtig: ein SELECT SINGLE je Fakturazeile waere bei sechsstelligen
" Zeilenzahlen viel zu langsam.
DATA: gt_mbew_all TYPE SORTED TABLE OF ty_mbew
                       WITH UNIQUE KEY matnr bwkey.

DATA: gv_cnt      TYPE i,
      gv_cnt2     TYPE i,
      gv_cnt3     TYPE i,
      gv_pct      TYPE p DECIMALS 1,
      gv_gjahr    TYPE bkpf-gjahr,
      gv_jahr_von TYPE bkpf-gjahr,
      gv_jahr_bis TYPE bkpf-gjahr,
      gv_jahr_c   TYPE c LENGTH 4,
      gv_shown    TYPE i.

*----------------------------------------------------------------------*
START-OF-SELECTION.

  WRITE: / '=========================================================='.
  WRITE: / 'ZFIN_ANALYSE_STPRS_JOURNAL  -  Trafag Finance Dashboard'.
  WRITE: / 'System:', sy-sysid, ' Mandant:', sy-mandt, ' User:', sy-uname.
  WRITE: / 'Datum :', sy-datum.
  WRITE: / 'Dieser Report ist NUR LESEND.'.
  WRITE: / '=========================================================='.

*----------------------------------------------------------------------*
* TEIL 1 - Welche Buchungskreise gibt es (CH / AT) und in welcher Waehrung?
* Frage: Wie trennen wir Schweiz von Oesterreich, und was ist die
*        jeweilige Hauswaehrung? (Spalte CompanyCode im Dashboard)
*----------------------------------------------------------------------*
  SKIP 1.
  WRITE: / '### TEIL 1: Buchungskreise / Gesellschaften ###'.
  ULINE.

  SELECT bukrs butxt waers land1 ktopl
    FROM t001
    INTO TABLE gt_bukrs
   WHERE bukrs IN s_bukrs.

  IF gt_bukrs IS INITIAL.
    WRITE: / 'KEINE Buchungskreise gefunden. Selektion pruefen.'.
  ELSE.
    WRITE: / 'BUKRS', 20 'Bezeichnung', 60 'Hauswaehrung', 76 'Land', 84 'Kontenplan'.
    ULINE.
    LOOP AT gt_bukrs INTO gs_bukrs.
      WRITE: / gs_bukrs-bukrs, 20 gs_bukrs-butxt, 60 gs_bukrs-waers,
               76 gs_bukrs-land1, 84 gs_bukrs-ktopl.
    ENDLOOP.
  ENDIF.

*----------------------------------------------------------------------*
* TEIL 2 - Bewertungskreise und Werke je Buchungskreis
* Frage: Ueber welchen Schluessel (BWKEY) haengt der Standardpreis
*        an den Buchungskreisen? MBEW ist je MATNR + BWKEY.
*----------------------------------------------------------------------*
  SKIP 1.
  WRITE: / '### TEIL 2: Bewertungskreise (BWKEY) und Werke ###'.
  ULINE.

  SELECT bwkey bukrs
    FROM t001k
    INTO TABLE gt_bwk
   WHERE bukrs IN s_bukrs.

  SELECT werks name1 bwkey
    FROM t001w
    INTO TABLE gt_werk.

  IF gt_bwk IS INITIAL.
    WRITE: / 'KEINE Bewertungskreise zu diesen Buchungskreisen gefunden.'.
  ELSE.
    WRITE: / 'BWKEY', 12 'BUKRS', 22 'Werke in diesem Bewertungskreis'.
    ULINE.
    LOOP AT gt_bwk INTO gs_bwk.
      WRITE: / gs_bwk-bwkey, 12 gs_bwk-bukrs, 22 ''.
      LOOP AT gt_werk INTO gs_werk WHERE bwkey = gs_bwk-bwkey.
        WRITE: gs_werk-werks, '/', gs_werk-name1(20), ' '.
      ENDLOOP.
    ENDLOOP.
  ENDIF.

*----------------------------------------------------------------------*
* TEIL 3 - STANDARDPREIS: ist MBEW-STPRS ueberhaupt gefuellt?
* Frage (Kernfrage A): Koennen wir die Kostenbasis der Gruppenmarge
*        aus dem Materialstamm ziehen? Wie hoch ist die Abdeckung?
*        Und ACHTUNG: PEINH (Preiseinheit) - STPRS gilt pro PEINH Stueck!
*        VPRSV = 'S' -> Standardpreis, 'V' -> gleitender Durchschnittspreis.
*----------------------------------------------------------------------*
  SKIP 1.
  WRITE: / '### TEIL 3: Standardpreis im Materialstamm (MBEW) ###'.
  ULINE.

  LOOP AT gt_bwk INTO gs_bwk.

    CLEAR: gt_mbew, gv_cnt, gv_cnt2, gv_cnt3.

    SELECT matnr bwkey vprsv stprs verpr peinh bklas
      FROM mbew
      INTO TABLE gt_mbew
     WHERE bwkey = gs_bwk-bwkey.

    DESCRIBE TABLE gt_mbew LINES gv_cnt.

    LOOP AT gt_mbew INTO gs_mbew.
      IF gs_mbew-stprs > 0.
        gv_cnt2 = gv_cnt2 + 1.
      ENDIF.
      IF gs_mbew-vprsv = 'S'.
        gv_cnt3 = gv_cnt3 + 1.
      ENDIF.
      " fuer die schnellen Lookups in Teil 4 merken
      INSERT gs_mbew INTO TABLE gt_mbew_all.
    ENDLOOP.

    SKIP 1.
    WRITE: / 'Bewertungskreis', gs_bwk-bwkey, '(BUKRS', gs_bwk-bukrs, ')'.
    WRITE: / '  Materialien in MBEW gesamt :', gv_cnt.
    IF gv_cnt > 0.
      gv_pct = gv_cnt2 * 100 / gv_cnt.
      WRITE: / '  davon STPRS > 0            :', gv_cnt2, '(', gv_pct, '% )'.
      gv_pct = gv_cnt3 * 100 / gv_cnt.
      WRITE: / '  davon VPRSV = S (Standard) :', gv_cnt3, '(', gv_pct, '% )'.
    ENDIF.

    " Stichprobe: zeigt Preiseinheit und Bewertungsklasse
    WRITE: / '  Stichprobe (Preiseinheit beachten!):'.
    WRITE: / '    MATNR', 30 'VPRSV', 38 'STPRS', 58 'PEINH', 66 'VERPR', 86 'BKLAS'.
    gv_shown = 0.
    LOOP AT gt_mbew INTO gs_mbew WHERE stprs > 0.
      WRITE: / '   ', gs_mbew-matnr, 30 gs_mbew-vprsv, 38 gs_mbew-stprs,
               58 gs_mbew-peinh, 66 gs_mbew-verpr, 86 gs_mbew-bklas.
      gv_shown = gv_shown + 1.
      IF gv_shown >= p_abschn.
        EXIT.
      ENDIF.
    ENDLOOP.
    IF gv_shown = 0.
      WRITE: / '    >>> KEIN Material mit STPRS > 0 in diesem Bewertungskreis!'.
    ENDIF.

  ENDLOOP.

*----------------------------------------------------------------------*
* TEIL 4 - DIE ENTSCHEIDENDE FRAGE:
*        Haben die tatsaechlich FAKTURIERTEN Materialien einen Standardpreis?
*        (Eine hohe MBEW-Abdeckung nuetzt nichts, wenn ausgerechnet die
*         verkauften Artikel keinen Preis haben.)
*        Zusaetzlich: VBRP-WAVWR ist der Kostenwert direkt auf der
*        Fakturaposition - wenn der gefuellt ist, brauchen wir MBEW evtl.
*        gar nicht.
*----------------------------------------------------------------------*
  SKIP 1.
  WRITE: / '### TEIL 4: Kostenbasis fuer die tatsaechlich fakturierten Zeilen ###'.
  ULINE.

  CLEAR gt_sold.

  SELECT p~vbeln p~posnr p~matnr p~werks p~fkimg p~netwr p~wavwr
         k~bukrs k~fkdat k~waerk
    FROM vbrp AS p
    INNER JOIN vbrk AS k ON k~vbeln = p~vbeln
    INTO TABLE gt_sold
   WHERE k~bukrs IN s_bukrs
     AND k~fkdat GE '20250101'.

  DESCRIBE TABLE gt_sold LINES gv_cnt.
  WRITE: / 'Fakturapositionen ab 01.01.2025 :', gv_cnt.

  IF gv_cnt > 0.

    " 4a) Wie oft ist der Kostenwert direkt auf der Position gefuellt?
    CLEAR gv_cnt2.
    LOOP AT gt_sold INTO gs_sold WHERE wavwr <> 0.
      gv_cnt2 = gv_cnt2 + 1.
    ENDLOOP.
    gv_pct = gv_cnt2 * 100 / gv_cnt.
    SKIP 1.
    WRITE: / '4a) Kostenwert direkt auf der Fakturaposition (VBRP-WAVWR):'.
    WRITE: / '    gefuellt (<> 0):', gv_cnt2, '(', gv_pct, '% )'.
    WRITE: / '    >>> Ist das hoch, ist WAVWR die einfachste Kostenquelle'.
    WRITE: / '        und wir brauchen KEINEN MBEW-Join.'.

    " 4b) Deckung ueber den Materialstamm
    CLEAR: gv_cnt2, gv_cnt3.
    LOOP AT gt_sold INTO gs_sold.
      CLEAR gs_werk.
      READ TABLE gt_werk INTO gs_werk WITH KEY werks = gs_sold-werks.
      IF sy-subrc <> 0.
        gv_cnt3 = gv_cnt3 + 1.
        CONTINUE.
      ENDIF.
      CLEAR gs_mbew.
      READ TABLE gt_mbew_all INTO gs_mbew
           WITH TABLE KEY matnr = gs_sold-matnr
                          bwkey = gs_werk-bwkey.
      IF sy-subrc = 0 AND gs_mbew-stprs > 0.
        gv_cnt2 = gv_cnt2 + 1.
      ELSE.
        gv_cnt3 = gv_cnt3 + 1.
      ENDIF.
    ENDLOOP.

    SKIP 1.
    WRITE: / '4b) Kostenwert ueber Materialstamm (MBEW-STPRS je MATNR+BWKEY):'.
    gv_pct = gv_cnt2 * 100 / gv_cnt.
    WRITE: / '    Fakturazeilen MIT Standardpreis :', gv_cnt2, '(', gv_pct, '% )'.
    gv_pct = gv_cnt3 * 100 / gv_cnt.
    WRITE: / '    Fakturazeilen OHNE Standardpreis:', gv_cnt3, '(', gv_pct, '% )'.

    " 4c) Stichprobe zum Nachrechnen von Hand
    SKIP 1.
    WRITE: / '4c) Stichprobe zum Nachrechnen (Menge x Preis / Preiseinheit):'.
    WRITE: / '    MATNR', 26 'WERKS', 34 'Menge', 50 'Netto', 68 'WAVWR', 86 'STPRS', 104 'PEINH'.
    gv_shown = 0.
    LOOP AT gt_sold INTO gs_sold WHERE fkimg > 1.
      CLEAR gs_werk.
      READ TABLE gt_werk INTO gs_werk WITH KEY werks = gs_sold-werks.
      CLEAR gs_mbew.
      IF sy-subrc = 0.
        READ TABLE gt_mbew_all INTO gs_mbew
             WITH TABLE KEY matnr = gs_sold-matnr
                            bwkey = gs_werk-bwkey.
      ENDIF.
      WRITE: / '   ', gs_sold-matnr, 26 gs_sold-werks, 34 gs_sold-fkimg,
               50 gs_sold-netwr, 68 gs_sold-wavwr, 86 gs_mbew-stprs,
               104 gs_mbew-peinh.
      gv_shown = gv_shown + 1.
      IF gv_shown >= p_abschn.
        EXIT.
      ENDIF.
    ENDLOOP.

  ENDIF.

*----------------------------------------------------------------------*
* TEIL 5 - DATENVERFUEGBARKEIT: gibt es 2026er Fakturen auf PRODUKTIV?
* Frage (C): Das Dashboard sieht fuer CH/AT null Zeilen fuer 2026.
*        Liegt das an den Daten - oder daran, dass wir am falschen
*        System (travt statt travp) haengen?
*----------------------------------------------------------------------*
  SKIP 1.
  WRITE: / '### TEIL 5: Fakturen je Jahr (Datenverfuegbarkeit) ###'.
  ULINE.
  WRITE: / 'Jahr', 12 'Buchungskreis', 30 'Fakturapositionen'.
  ULINE.

  SORT gt_sold BY bukrs fkdat.
  LOOP AT gt_bukrs INTO gs_bukrs.
    DO 2 TIMES.
      IF sy-index = 1.
        gv_jahr_c = '2025'.
      ELSE.
        gv_jahr_c = '2026'.
      ENDIF.
      CLEAR gv_cnt.
      LOOP AT gt_sold INTO gs_sold WHERE bukrs = gs_bukrs-bukrs.
        IF gs_sold-fkdat(4) = gv_jahr_c.
          gv_cnt = gv_cnt + 1.
        ENDIF.
      ENDLOOP.
      WRITE: / gv_jahr_c, 12 gs_bukrs-bukrs, 30 gv_cnt.
      IF gv_jahr_c = '2026' AND gv_cnt = 0.
        WRITE: '   <<< KEINE 2026-Daten in diesem System!'.
      ENDIF.
    ENDDO.
  ENDLOOP.

*----------------------------------------------------------------------*
* TEIL 6 - JOURNAL / HAUPTBUCH: welche Tabellen tragen die Buchungszeilen?
* Frage (B): Reicht BSIS (Sachkonto-Einzelposten) fuer das Journal,
*        oder brauchen wir BSEG (Clustertabelle, teuer im Zugriff)?
*        BSIS deckt nur Sachkonten ab; Debitoren/Kreditoren liegen in
*        BSID/BSIK. Die Zahlen zeigen, ob eine Luecke entsteht.
*----------------------------------------------------------------------*
  SKIP 1.
  WRITE: / '### TEIL 6: Hauptbuch-Journal (Belegkoepfe und Zeilen) ###'.
  ULINE.
  WRITE: / 'BUKRS', 12 'GJAHR', 22 'BKPF Koepfe', 40 'BSIS (Sachkto)',
           60 'BSID (Debit)', 78 'BSIK (Kredit)'.
  ULINE.

  " Jahresbereich robust bestimmen (auch wenn s_gjahr als Bereich gefuellt ist)
  gv_jahr_von = '2025'.
  gv_jahr_bis = '2026'.
  IF NOT s_gjahr[] IS INITIAL.
    READ TABLE s_gjahr INDEX 1.
    IF s_gjahr-low IS NOT INITIAL.
      gv_jahr_von = s_gjahr-low.
    ENDIF.
    IF s_gjahr-high IS NOT INITIAL.
      gv_jahr_bis = s_gjahr-high.
    ELSE.
      gv_jahr_bis = gv_jahr_von.
    ENDIF.
  ENDIF.

  LOOP AT gt_bukrs INTO gs_bukrs.
    gv_gjahr = gv_jahr_von.
    WHILE gv_gjahr <= gv_jahr_bis.

      SELECT COUNT( * ) FROM bkpf INTO gv_cnt
       WHERE bukrs = gs_bukrs-bukrs AND gjahr = gv_gjahr.

      SELECT COUNT( * ) FROM bsis INTO gv_cnt2
       WHERE bukrs = gs_bukrs-bukrs AND gjahr = gv_gjahr.

      SELECT COUNT( * ) FROM bsid INTO gv_cnt3
       WHERE bukrs = gs_bukrs-bukrs AND gjahr = gv_gjahr.

      WRITE: / gs_bukrs-bukrs, 12 gv_gjahr, 22 gv_cnt, 40 gv_cnt2, 60 gv_cnt3.

      SELECT COUNT( * ) FROM bsik INTO gv_cnt3
       WHERE bukrs = gs_bukrs-bukrs AND gjahr = gv_gjahr.
      WRITE: 78 gv_cnt3.

      gv_gjahr = gv_gjahr + 1.
    ENDWHILE.
  ENDLOOP.

  SKIP 1.
  WRITE: / 'Hinweis: BSIS/BSID/BSIK enthalten nur OFFENE Posten.'.
  WRITE: / 'Ausgeglichene Posten liegen in BSAS/BSAD/BSAK.'.
  WRITE: / 'Fuer ein vollstaendiges Journal muessen daher entweder'.
  WRITE: / 'beide Seiten (BSIS+BSAS ...) gelesen werden ODER BSEG.'.
  WRITE: / '>>> Genau das ist die Frage an den SAP-Kollegen.'.

*----------------------------------------------------------------------*
* TEIL 7 - Belegarten: was ist eine "manuelle" Buchung?
* Frage: Wir nehmen aktuell BLART = 'SA' als manuell an. Stimmt das?
*----------------------------------------------------------------------*
  SKIP 1.
  WRITE: / '### TEIL 7: Belegarten (BLART) im Hauptbuch ###'.
  ULINE.
  WRITE: / 'BUKRS', 12 'GJAHR', 22 'BLART', 32 'Anzahl Belege'.
  ULINE.

  SELECT bukrs gjahr blart COUNT( * ) AS anzahl
    FROM bkpf
    INTO (gs_bukrs-bukrs, gv_gjahr, bkpf-blart, gv_cnt)
   WHERE bukrs IN s_bukrs
     AND gjahr IN s_gjahr
   GROUP BY bukrs gjahr blart
   ORDER BY bukrs gjahr blart.
    WRITE: / gs_bukrs-bukrs, 12 gv_gjahr, 22 bkpf-blart, 32 gv_cnt.
  ENDSELECT.

  SKIP 1.
  WRITE: / 'Frage an Finance: welche dieser Belegarten gelten als'.
  WRITE: / '"manuelle Buchung"? (Annahme im Dashboard bisher: SA)'.

*----------------------------------------------------------------------*
  SKIP 1.
  ULINE.
  WRITE: / 'ENDE. Bitte die komplette Liste als Datei sichern'.
  WRITE: / '(System > Liste > Sichern > Lokale Datei) und an Ingo geben.'.
