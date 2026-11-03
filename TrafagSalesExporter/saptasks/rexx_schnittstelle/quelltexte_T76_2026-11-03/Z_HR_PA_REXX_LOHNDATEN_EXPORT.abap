*&---------------------------------------------------------------------*
*& Report Z_HR_PA_REXX_LOHNDATEN_EXPORT
*&---------------------------------------------------------------------*
*&
*&---------------------------------------------------------------------*
REPORT z_hr_pa_rexx_lohndaten_export.
TABLES: pernr.
INFOTYPES: 0000,                       "Massnahmen
           0001.                      "Organisatorische Zuordnung
*.................................Zwischenspeicher......................
DATA:
  wpbpcheck(1).                        "Test ob WPBP Zuordnung OK
DATA:
  init-version(2) VALUE 'I '.          "Init Versions-Param.
DATA: BEGIN OF gt_data OCCURS 100,
        pernr(15),
        lgart(15),
        abrdat(10),
        betrg      TYPE betrg,
        Waers(10),
      END OF gt_data.

DATA: gv_betrgc(12),
      gv_Abrdatc(10).
DATA: gv_filename TYPE string.
DATA: BEGIN OF gt_out OCCURS 100,
        line(100),
      END OF gt_out.




CONSTANTS:
  c_molga(2) VALUE '02'.               "Molga Schweiz
INCLUDE rplperc1.
SELECT-OPTIONS so_lgart FOR rt-lgart.

SELECTION-SCREEN BEGIN OF LINE.
SELECTION-SCREEN COMMENT 1(25) TEXT-s02.
SELECTION-SCREEN POSITION 33.
PARAMETERS: r_unixf RADIOBUTTON GROUP outp USER-COMMAND loc.
SELECTION-SCREEN COMMENT 35(15) TEXT-s03.
SELECTION-SCREEN POSITION 53.
PARAMETERS: r_loclf RADIOBUTTON GROUP outp DEFAULT 'X'.
SELECTION-SCREEN COMMENT 55(15) TEXT-s04.
SELECTION-SCREEN END   OF LINE.





parameters: p_file like rlgrap-filename.

*.................................Schalter..............................
DATA:
  BEGIN OF sw,
    stop(1),                           "Prog-Abbruchs
  END OF sw.

START-OF-SELECTION.

GET pernr.
  zw-pernr = pernr-pernr.
  rp-provide-from-last p0001 space pn-begda pn-endda.

* Abrechnungsergebnisse uebernehmen
  PERFORM einlesen-ergebnisse.
*---------------------------------------------------------------------*
*



END-OF-SELECTION.
gt_out-line =
'Personalnummer;Lohnart ID;Abrechnungsdatum;Betrag;Currency'.
append gt_out.


  LOOP AT gt_data where betrg > 0.
    WRITE: /  gt_data-pernr,
             gt_data-lgart,
             gt_data-abrdat,
             gt_data-betrg,
             gt_data-Waers.
    WRITE gt_data-betrg TO gv_betrgc NO-GROUPING.
    WRITE gt_data-abrdat TO gv_abrdatc.
    CONCATENATE  gt_data-pernr
             gt_data-lgart
             gv_abrdatc
             gv_betrgc
             gt_data-Waers
    INTO gt_out-line SEPARATED BY ';'.
    APPEND gt_out.
  ENDLOOP.

 if r_loclf = 'X'.

 CALL FUNCTION 'DOWNLOAD'
         EXPORTING
              FILENAME = p_file
              FILETYPE = 'ASC'
         TABLES
              DATA_TAB = gt_out.
  else.
    perform download_server.
  endif.
*                                                                     *
*---------------------------------------------------------------------*

*---------------------------------------------------------------------*
*                                                                     *
*       Unterroutinen                                                 *
*                                                                     *
*---------------------------------------------------------------------*
*---------------------------------------------------------------------*
*       FORM RT                                                       *
*---------------------------------------------------------------------*
*       Verarbeiten der Ergebnistabelle                               *
*---------------------------------------------------------------------*
*  -->  VORZEICHEN Art der Betragsbehandlung                          *
*---------------------------------------------------------------------*
FORM rt USING vorzeichen.
*

* RT nach Lohnarten sortieren
  SORT rt BY lgart.
  READ TABLE wpbp INDEX 1.
  IF wpbp-begda BETWEEN pn-begda AND pn-endda.
* Über alle Lohnarten loopen
    LOOP AT rt WHERE lgart IN so_lgart.
*Vorzeichen ggf. umdrehen
      IF vorzeichen = '-'.
        rt-betrg = 0 - rt-betrg.         "Vorzeichen umdrehen.
      ENDIF.
      CLEAR gt_data.
      gt_data-pernr = pernr-pernr.
      gt_data-lgart = rt-lgart.
      gt_data-waers = 'CHF'.
      WRITE wpbp-begda TO gt_data-abrdat.
      gt_data-betrg = rt-betrg.
      COLLECT gt_data.
    ENDLOOP.                             "XQTALRK012846
  ENDIF.

ENDFORM.                               "RT.

*                                                                     *
*       FORM SV                                                       *
*                                                                     *
*       Dummy                                                         *
*                                                                     *
FORM sv.
ENDFORM.
INCLUDE rplperc0.



*&---------------------------------------------------------------------*
*&      Form  DOWNLOAD_SERVER
*&---------------------------------------------------------------------*
*       text
*----------------------------------------------------------------------*
*  -->  p1        text
*  <--  p2        text
*----------------------------------------------------------------------*
FORM download_server .
  replace 'jjjjmmtt' with sy-datum into  p_file.
   replace 'hhmmss' with sy-uzeit into  p_file.
  OPEN DATASET p_file FOR OUTPUT IN TEXT MODE
  ENCODING DEFAULT.
  IF sy-subrc EQ 0.
    LOOP AT gt_out.
      TRANSFER gt_out TO p_file. "gv_filename.
    ENDLOOP.
    CLOSE DATASET p_file.
    WRITE: 'File wurde auf den Server geschrieben.'.
  ELSE.
    MESSAGE 'File konnte nicht auf den Server geschrieben werden' TYPE 'I'.
  ENDIF.
ENDFORM.                    " DOWNLOAD_SERVER
