*&---------------------------------------------------------------------*
*& Report  Z_ZZPRDAT_CHECK
*&---------------------------------------------------------------------*
*& Nachweisreport fuer das Produktionsdatum AUFK-ZZPRDAT.
*&
*& Der Report ist ausschliesslich lesend. Er legt keinen Auftrag an, gibt
*& keinen frei und aendert kein Feld. Er stellt je Fertigungsauftrag den
*& Eckendtermin, das Produktionsdatum und den Freigabestatus gegenueber und
*& faellt daraus ein Urteil.
*&
*& Fachlicher Sollzustand: ZZPRDAT wird bei der erstmaligen Freigabe einmalig
*& aus AFKO-GLTRP gesetzt und bleibt danach unveraendert, auch wenn der
*& Eckendtermin spaeter verschoben wird.
*&
*& Gedacht fuer T76/100 zur Abnahme des Prototyps Z_ZZPRDAT_AT_RELEASE.
*& Paket $TMP, kein Transport.
*&---------------------------------------------------------------------*
REPORT z_zzprdat_check LINE-SIZE 210 NO STANDARD PAGE HEADING.

TABLES aufk.

SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME.
SELECT-OPTIONS s_aufnr FOR aufk-aufnr.
PARAMETERS: p_werks TYPE aufk-werks,
            p_erdat TYPE aufk-erdat,
            p_leer  AS CHECKBOX,
            p_max   TYPE i DEFAULT 100.
SELECTION-SCREEN END OF BLOCK b1.

TYPES: BEGIN OF ty_zeile,
         aufnr   TYPE aufk-aufnr,
         auart   TYPE aufk-auart,
         autyp   TYPE aufk-autyp,
         werks   TYPE aufk-werks,
         erdat   TYPE aufk-erdat,
         objnr   TYPE aufk-objnr,
         zzprdat TYPE aufk-zzprdat,
         gltrp   TYPE afko-gltrp,
         gltrs   TYPE afko-gltrs,
         ftrmi   TYPE afko-ftrmi,
       END OF ty_zeile.

DATA: gt_zeilen TYPE STANDARD TABLE OF ty_zeile,
      gs_zeile  TYPE ty_zeile,
      gv_anz    TYPE i,
      gv_rel    TYPE c LENGTH 1,
      gv_urteil TYPE c LENGTH 34,
      gv_ok     TYPE i,
      gv_fehlt  TYPE i,
      gv_frozen TYPE i,
      gv_offen  TYPE i.

INITIALIZATION.
* Standardmaessig die Auftraege der letzten 30 Tage, damit ohne Eingabe
* nicht die ganze Tabelle gelesen wird.
  p_erdat = sy-datum - 30.

START-OF-SELECTION.

  SELECT a~aufnr a~auart a~autyp a~werks a~erdat a~objnr a~zzprdat
         k~gltrp k~gltrs k~ftrmi
    INTO CORRESPONDING FIELDS OF TABLE gt_zeilen
    UP TO p_max ROWS
    FROM aufk AS a
    INNER JOIN afko AS k ON k~aufnr = a~aufnr
    WHERE a~aufnr IN s_aufnr
      AND a~erdat GE p_erdat
      AND a~autyp EQ '10'
    ORDER BY a~aufnr DESCENDING.

  IF gt_zeilen IS INITIAL.
    WRITE: / 'Keine Fertigungsauftraege zur Selektion gefunden.'.
    WRITE: / 'Hinweis: ohne Auftragsnummer wird ab Anlagedatum', p_erdat, 'gelesen.'.
    RETURN.
  ENDIF.

  WRITE: /  'Produktionsdatum-Nachweis  (nur lesend)',
         AT 120 'System', sy-sysid, sy-mandt, 'Stand', sy-datum.
  ULINE.
  WRITE: /   'Auftrag'      COLOR COL_HEADING,
          14 'Art'          COLOR COL_HEADING,
          19 'Werk'         COLOR COL_HEADING,
          25 'Angelegt'     COLOR COL_HEADING,
          37 'GLTRP'        COLOR COL_HEADING,
          49 'GLTRS'        COLOR COL_HEADING,
          61 'FTRMI'        COLOR COL_HEADING,
          73 'ZZPRDAT'      COLOR COL_HEADING,
          85 'REL'          COLOR COL_HEADING,
          90 'Urteil'       COLOR COL_HEADING.
  ULINE.

  LOOP AT gt_zeilen INTO gs_zeile.

*   Optionale Nachfilter, damit die Datenbankabfrage einfach und schnell bleibt.
    IF p_werks IS NOT INITIAL AND gs_zeile-werks NE p_werks.
      CONTINUE.
    ENDIF.
    IF p_leer EQ 'X' AND gs_zeile-zzprdat IS NOT INITIAL.
      CONTINUE.
    ENDIF.

*   Freigabestatus: I0002 ist "freigegeben". INACT initial heisst aktiv gesetzt.
    CLEAR gv_anz.
    SELECT COUNT( * ) INTO gv_anz
      FROM jest
      WHERE objnr EQ gs_zeile-objnr
        AND stat  EQ 'I0002'
        AND inact EQ space.
*   FTRMI ist das Ist-Freigabedatum und der verlaessliche Nachweis. Der Status
*   I0002 allein genuegt nicht: bei abgeschlossenen Auftraegen ist er inaktiv
*   gesetzt, gemessen am 2026-09-03 an Auftrag 1194970 (freigegeben 03.07.2025,
*   I0002 inaktiv). Ein Report, der nur JEST liest, meldet solche Auftraege
*   faelschlich als nie freigegeben und verdeckt damit genau den Fehlerfall.
    IF gv_anz GT 0 OR gs_zeile-ftrmi IS NOT INITIAL.
      gv_rel = 'X'.
    ELSE.
      CLEAR gv_rel.
    ENDIF.

*   Urteil. Die dritte Zeile ist der eigentliche Write-once-Nachweis:
*   ZZPRDAT gefuellt, aber ungleich GLTRP bedeutet, dass der Eckendtermin
*   nach der Freigabe verschoben wurde und das Produktionsdatum stehen blieb.
    IF gs_zeile-zzprdat IS INITIAL AND gv_rel EQ 'X'.
      gv_urteil = 'FEHLT trotz Freigabe'.
      gv_fehlt = gv_fehlt + 1.
    ELSEIF gs_zeile-zzprdat IS INITIAL.
      gv_urteil = 'noch nicht freigegeben'.
      gv_offen = gv_offen + 1.
    ELSEIF gs_zeile-zzprdat EQ gs_zeile-gltrp.
      gv_urteil = 'gesetzt, gleich GLTRP'.
      gv_ok = gv_ok + 1.
    ELSE.
      gv_urteil = 'eingefroren, GLTRP verschoben'.
      gv_frozen = gv_frozen + 1.
    ENDIF.

    WRITE: /   gs_zeile-aufnr,
            14 gs_zeile-auart,
            19 gs_zeile-werks,
            25 gs_zeile-erdat,
            37 gs_zeile-gltrp,
            49 gs_zeile-gltrs,
            61 gs_zeile-ftrmi,
            73 gs_zeile-zzprdat,
            85 gv_rel,
            90 gv_urteil.

  ENDLOOP.

  ULINE.
  WRITE: / 'Zusammenfassung'.
  WRITE: / '  gesetzt und gleich GLTRP        :', gv_ok.
  WRITE: / '  eingefroren, GLTRP verschoben   :', gv_frozen, '  <- Write-once nachgewiesen'.
  WRITE: / '  FEHLT trotz Freigabe            :', gv_fehlt, '  <- hier greift die Logik nicht'.
  WRITE: / '  noch nicht freigegeben          :', gv_offen.
