*&---------------------------------------------------------------------*
*& Report  Z_ZZPRDAT_CHECK
*&---------------------------------------------------------------------*
*& Diagnosebericht fuer das Produktionsdatum AUFK-ZZPRDAT.
*&
*& Der Report ist ausschliesslich lesend. Er legt keinen Auftrag an, gibt
*& keinen frei und aendert kein Feld. Er stellt je Fertigungsauftrag den
*& Eckendtermin, das Produktionsdatum und den Freigabestatus gegenueber und
*& beschreibt den aktuellen Zustand, ohne dessen Historie zu behaupten.
*&
*& Fachlicher Sollzustand: ZZPRDAT wird bei der erstmaligen Freigabe einmalig
*& aus AFKO-GLTRP gesetzt und bleibt danach unveraendert, auch wenn der
*& Eckendtermin spaeter verschoben wird.
*&
*& T76/100, Paket ZPP1, Transport T76K912490.
*& Write-once ist nur mit einem Vorher-Nachher-Vergleich nachweisbar.
*&---------------------------------------------------------------------*
REPORT z_zzprdat_check LINE-SIZE 210 NO STANDARD PAGE HEADING.

TABLES aufk.

RANGES: gr_werks FOR aufk-werks,
        gr_prddat FOR aufk-zzprdat.

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
      gv_gleich TYPE i,
      gv_leer   TYPE i,
      gv_anders TYPE i,
      gv_ohne_rel TYPE i,
      gv_offen  TYPE i.

INITIALIZATION.
* Standardmaessig die Auftraege der letzten 30 Tage, damit ohne Eingabe
* nicht die ganze Tabelle gelesen wird.
  p_erdat = sy-datum - 30.

AT SELECTION-SCREEN.
  IF p_max LE 0.
    MESSAGE 'Maximale Trefferzahl muss groesser als 0 sein' TYPE 'E'.
  ENDIF.

START-OF-SELECTION.

* Optionale Filter VOR der Begrenzung anwenden. Leere Ranges erlauben alle
* Werte; sonst koennten die ersten p_max Auftraege das gesuchte Werk oder
* die gesuchten leeren Felder aus der Ausgabe verdraengen.
  IF p_werks IS NOT INITIAL.
    gr_werks-sign = 'I'.
    gr_werks-option = 'EQ'.
    gr_werks-low = p_werks.
    APPEND gr_werks.
  ENDIF.
  IF p_leer EQ 'X'.
    gr_prddat-sign = 'I'.
    gr_prddat-option = 'EQ'.
    CLEAR gr_prddat-low.
    APPEND gr_prddat.
  ENDIF.

  SELECT a~aufnr a~auart a~autyp a~werks a~erdat a~objnr a~zzprdat
         k~gltrp k~gltrs k~ftrmi
    INTO CORRESPONDING FIELDS OF TABLE gt_zeilen
    UP TO p_max ROWS
    FROM aufk AS a
    INNER JOIN afko AS k ON k~aufnr = a~aufnr
    WHERE a~aufnr IN s_aufnr
      AND a~erdat GE p_erdat
      AND a~autyp EQ '10'
      AND a~werks IN gr_werks
      AND a~zzprdat IN gr_prddat
    ORDER BY a~aufnr DESCENDING.

  IF gt_zeilen IS INITIAL.
    WRITE: / 'Keine Fertigungsauftraege zur Selektion gefunden.'.
    WRITE: / 'Anlagedatum ab', p_erdat, 'gilt auch bei eingegebener Auftragsnummer.'.
    RETURN.
  ENDIF.

  WRITE: /  'Produktionsdatum - Momentaufnahme (nur lesend)',
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
          90 'Beobachtung'  COLOR COL_HEADING.
  ULINE.

  LOOP AT gt_zeilen INTO gs_zeile.

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

*   Eine Momentaufnahme beweist weder den Erstfreigabetermin noch Write-once.
*   Auch ein falscher Wert kann gleich oder ungleich GLTRP sein. Leere
*   Altauftraege bleiben bewusst leer; V2 kann ausserdem noch ausstehen.
    IF gs_zeile-zzprdat IS INITIAL AND gv_rel EQ 'X'.
      gv_urteil = 'Leer; Freigabe vorhanden'.
      gv_leer = gv_leer + 1.
    ELSEIF gs_zeile-zzprdat IS INITIAL.
      gv_urteil = 'Leer; kein Freigabenachweis'.
      gv_offen = gv_offen + 1.
    ELSEIF gv_rel IS INITIAL.
      gv_urteil = 'Datum ohne Freigabenachweis'.
      gv_ohne_rel = gv_ohne_rel + 1.
    ELSEIF gs_zeile-zzprdat EQ gs_zeile-gltrp.
      gv_urteil = 'Datum gleich aktuellem GLTRP'.
      gv_gleich = gv_gleich + 1.
    ELSE.
      gv_urteil = 'Datum weicht von GLTRP ab'.
      gv_anders = gv_anders + 1.
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
  WRITE: / '  Datum gleich aktuellem GLTRP    :', gv_gleich.
  WRITE: / '  Datum weicht von GLTRP ab       :', gv_anders.
  WRITE: / '  Leer; Freigabe vorhanden        :', gv_leer.
  WRITE: / '  Leer; kein Freigabenachweis     :', gv_offen.
  WRITE: / '  Datum ohne Freigabenachweis     :', gv_ohne_rel.
  SKIP.
  WRITE: / 'Kein automatischer Nachweis fuer Erstfreigabetermin oder Write-once.'.
  WRITE: / 'Dafuer Werte vor Freigabe, danach und nach Terminverschiebung vergleichen.'.
  WRITE: / 'Leer trotz Freigabe: Altbestand oder ausstehende/fehlgeschlagene V2 pruefen.'.
  WRITE: / 'Gefuelltes Datum ohne Freigabenachweis: Herkunft gesondert pruefen.'.
  WRITE: / 'Selektion: Anlagedatum ab', p_erdat, '(auch bei Auftragsnummer), maximal', p_max, 'Treffer.'.
