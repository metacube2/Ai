*&---------------------------------------------------------------------*
*& Report Z_HR_PA_IMP_ZEITEREIGNISSE
*&---------------------------------------------------------------------*
*&
*&---------------------------------------------------------------------*
REPORT z_hr_pa_imp_absenzdaten.

TABLES: sscrfields, usr01.
TYPES: BEGIN OF ls_error,
         pos(4)    TYPE n,
         pernr(8)  TYPE n,
         text(40)  TYPE n,
         value(11) TYPE n,
       END OF ls_error.
TYPES lty_imp TYPE string.
DATA: lV_import TYPE string.
*-----------------------------------------------------------------------
* Internal tables
*-----------------------------------------------------------------------
DATA: BEGIN OF bdcdata OCCURS 20.
        INCLUDE STRUCTURE bdcdata.
DATA: END OF bdcdata.
DATA: lt_error TYPE TABLE OF ls_error.
DATA: lt_import TYPE TABLE OF lty_imp.
DATA: ls_imp TYPE lty_imp.
*-----------------------------------------------------------------------
* local variables
*-----------------------------------------------------------------------

DATA: nodata(1) VALUE ' '.          "nodata
DATA: lv_upload TYPE string.

DATA: BEGIN OF lt_tzabs OCCURS 100,
        datum TYPE dats,
      END OF lt_tzabs.

DATA: lv_begdac(10).
DATA: lv_lgart(4).
DATA: lv_lgartin(6).
DATA: lv_betrgc(10).
DATA lv_betrg LIKE pa0015-betrg.
DATA: lv_pernrz TYPE persno.
DATA ls_0015 TYPE pa0015.
DATA: lv_answer(1).
DATA: lv_absart(4).
DATA: lv_dynpro(4).
DATA: lv_infty(4).
DATA: lv_datum(10).
DATA: lv_zeit(8).


DATA: lv_ahvnr(20),
      lv_pernr(8),
      lv_id(10),
      lv_wf(20),
      lv_kostl(40),
      lv_bezug(20),
      lv_status(20),
      lv_antragstyp(20),
      lv_antragst(20),
      lv_absenz(20),
      lv_begda(10),
      lv_endda(10),
      lv_anztag(6),
      lv_halbtage(255),
      lv_zeitstempel(20),
      lv_datab01(10),
      lv_datab02(10),
      lv_datab03(10),
      lv_datab04(10),
      lv_datab05(10),
      lv_datab06(10),
      lv_datab07(10),
      lv_datab08(10),
      lv_datab09(10),
      lv_datab10(10),
      lv_datab11(10),
      lv_datab12(10),
      lv_datab13(10),
      lv_datab14(10),
      lv_datab15(10),
      lv_datab16(10),
      lv_datab17(10),
      lv_datab18(10),
      lv_datab19(10),
      lv_datab20(10),
      lv_datab21(10),
      lv_datab22(10),
      lv_datab23(10),
      lv_datab24(10),
      lv_datab25(10),
      lv_datab26(10),
      lv_datab27(10),
      lv_datab28(10),
      lv_datab29(10),
      lv_datab30(10),
      lv_firstht         TYPE dats,
      lv_lastht          TYPE dats,
      lv_begabs          TYPE dats,
      lv_endabs          TYPE dats,
      lv_datac(10),
      lv_arbst           TYPE stdtg,
      lv_arbstc(6),
      lv_vdatum type dats.


*-----------------------------------------------------------------------
* Selection Screen
*-----------------------------------------------------------------------
SELECTION-SCREEN BEGIN OF BLOCK 01 WITH FRAME TITLE TEXT-s01.

  PARAMETERS: p_file01 LIKE rlgrap-filename DEFAULT
                'c:\temp\test.csv' OBLIGATORY.

  SELECTION-SCREEN BEGIN OF LINE.
    PARAMETERS:     p_list1 TYPE rplxxxxx-list RADIOBUTTON GROUP list USER-COMMAND lst.
    SELECTION-SCREEN COMMENT  3(29) TEXT-p01.
  SELECTION-SCREEN END   OF LINE.


  SELECTION-SCREEN BEGIN OF LINE.
    PARAMETERS:     p_list2     RADIOBUTTON GROUP list.
    SELECTION-SCREEN COMMENT  3(29) TEXT-p03.

  SELECTION-SCREEN END   OF LINE.

parameters: p_datumv type dats,
            p_datumb type dats.


SELECTION-SCREEN END OF BLOCK 01.
SELECTION-SCREEN BEGIN OF BLOCK 02 WITH FRAME TITLE TEXT-s02.
  PARAMETERS:
    p_mapnam LIKE rptaxxxx-map_name DEFAULT 'ABS_2001' OBLIGATORY,
    p_test   AS CHECKBOX DEFAULT 'X'.
SELECTION-SCREEN END OF BLOCK 02.
SELECTION-SCREEN BEGIN OF BLOCK 03 WITH FRAME TITLE TEXT-s03.
  SELECTION-SCREEN:
  SKIP,
  PUSHBUTTON 2(18) TEXT-s04 USER-COMMAND cli1
    VISIBLE LENGTH 18.
SELECTION-SCREEN END OF BLOCK 03.


*-----------------------------------------------------------------------
TOP-OF-PAGE.
*-----------------------------------------------------------------------



*-----------------------------------------------------------------------
AT SELECTION-SCREEN.
*-----------------------------------------------------------------------
  CASE sscrfields.
    WHEN 'CLI1'.
      SET PARAMETER ID 'MPN' FIELD p_mapnam.
      CALL TRANSACTION 'SM35' AND SKIP FIRST SCREEN.
  ENDCASE.



*-----------------------------------------------------------------------
AT SELECTION-SCREEN ON VALUE-REQUEST FOR p_file01.
*-----------------------------------------------------------------------
* Navigationsfenster
  PERFORM get_filename CHANGING p_file01.

*-----------------------------------------------------------------------
START-OF-SELECTION.
*-----------------------------------------------------------------------
*----------------------------------------------------------------------*
* Mappen-Identifkation (Anmelde-Dynpro) ausgeben.                      *
*----------------------------------------------------------------------*

  IF p_list2 = 'X'.
    OPEN DATASET p_file01 FOR INPUT IN TEXT MODE ENCODING DEFAULT.
    DO.
      READ DATASET p_file01 INTO ls_imp.
      APPEND  ls_imp TO  lt_import.
      IF sy-subrc <> 0. EXIT. ENDIF.

    ENDDO.
    CLOSE DATASET P_file01.

  ELSE.
* File upload
    lv_upload = p_file01.
    CALL FUNCTION 'GUI_UPLOAD'
      EXPORTING
        filename                = lv_upload
        filetype                = 'ASC'
        has_field_separator     = space
*       HEADER_LENGTH           = 0
*       READ_BY_LINE            = 'X'
*       DAT_MODE                = ' '
*       CODEPAGE                = ' '
*       IGNORE_CERR             = ABAP_TRUE
*       REPLACEMENT             = '#'
*       CHECK_BOM               = ' '
*       VIRUS_SCAN_PROFILE      =
*       NO_AUTH_CHECK           = ' '
* IMPORTING
*       FILELENGTH              =
*       HEADER                  =
      TABLES
        data_tab                = lt_import
      EXCEPTIONS
        file_open_error         = 1
        file_read_error         = 2
        no_batch                = 3
        gui_refuse_filetransfer = 4
        invalid_type            = 5
        no_authority            = 6
        unknown_error           = 7
        bad_data_format         = 8
        header_not_allowed      = 9
        separator_not_allowed   = 10
        header_too_long         = 11
        unknown_dp_error        = 12
        access_denied           = 13
        dp_out_of_memory        = 14
        disk_full               = 15
        dp_timeout              = 16
        OTHERS                  = 17.
    IF sy-subrc <> 0.
      MESSAGE ID sy-msgid TYPE sy-msgty NUMBER sy-msgno
              WITH sy-msgv1 sy-msgv2 sy-msgv3 sy-msgv4.
    ENDIF.
  ENDIF.

  IF p_test IS INITIAL.
*   Mappe für BI anlegen
    PERFORM open_group USING sy-mandt p_mapnam sy-uname.
  ENDIF.

* Init


  REFRESH lt_error.

* hole die Abrechnungsperiode

  LOOP AT lt_import INTO ls_imp.
    IF sy-tabix > 1.
      SPLIT ls_imp AT ';'
     INTO lv_ahvnr
      lv_pernr
      lv_id
      lv_wf
      lv_kostl
      lv_bezug
      lv_status
      lv_antragstyp
      lv_antragst
      lv_absenz
      lv_zeitstempel
      lv_begda
      lv_endda
      lv_anztag
      lv_halbtage
      lv_zeitstempel.

      lv_vdatum(4) = lv_endda+6(4).
      lv_vdatum+4(2) = lv_endda+3(2).
      lv_vdatum+6(2) = lv_endda(2).
* Nur genehmigte verbuchen.
      IF lv_status = 'Genehmigt' and lv_vdatum > p_datumv and lv_vdatum < p_datumb.
        IF lv_absenz NE 'Krankheit'
           AND lv_absenz NE 'Kind-Krank'
           AND lv_absenz NE 'Unfall'.


*  Halbtage?

          IF lv_halbtage = space.

            CASE lv_absenz.
              WHEN 'Ferien'.
                lv_absart = '0100'.
                lv_dynpro = '2001'.
                lv_infty  = '2001'.
*            WHEN 'Krankheit' OR 'Kind-Krank'.
*              lv_absart = '0260'.
*              lv_dynpro = '2000'.
*              lv_infty  = '2001'.
              WHEN 'Aus- und Weiterbildung'.
                lv_dynpro = '2050'.
                lv_infty  = '2002'.
                lv_absart = '0710'.
              WHEN 'Bezahlte Absenzen'.
                lv_absart = '0500'.
                lv_dynpro = '2000'.
                lv_infty  = '2001'.
              WHEN 'Dienstreise'.
                lv_absart = '0700'.
                lv_dynpro = '2051'.
                lv_infty  = '2002'.
              WHEN 'Kompensation' OR'Sonderurlaub'.
                lv_absart = '0400'.
                lv_dynpro = '2001'.
                lv_infty  = '2001'.
              WHEN 'Militär / Zivildienst' OR 'Mutterschaftsurlaub'.
                lv_absart = '0600'.
                lv_dynpro = '2000'.
                lv_infty  = '2001'.
              WHEN 'Unbezahlter Urlaub'.
                lv_absart = '0900'.
                lv_dynpro = '2000'.
                lv_infty  = '2001'.
                "            WHEN 'Unfall'.
                "                lv_absart = '0290'.
                "                lv_dynpro = '2000'.
                "                lv_infty  = '2001'.
*        WHEN 'Vaterschaftsurlaub'.
*          lv_absart = '0500'.
            ENDCASE.
            CLEAR   bdcdata.
            REFRESH bdcdata.
            WRITE: / 'OK  Absenz. Ganztag', lv_pernr, lv_absenz, lv_absart, lv_begda,lv_endda.
            IF p_test IS INITIAL.

*       Einstiegsbild Personalstammdaten pflegen



              PERFORM bdc_dynpro      USING 'SAPMP50A'    '1000'.
              PERFORM bdc_field:      USING 'RP50G-PERNR' lv_pernr,
                                      USING 'RP50G-BEGDA' lv_begda,
                                       USING 'RP50G-ENDDA' lv_endda,
                                      USING 'RP50G-CHOIC' lv_infty,
                                       USING 'RP50G-SUBTY' lv_absart,
                                      USING 'BDC_OKCODE'  '=INS'.
              PERFORM bdc_dynpro      USING 'MP200000'    lv_dynpro.
              PERFORM bdc_field:      USING 'BDC_OKCODE'  '=UPD'.

*         Tansaktion aufrufen
              PERFORM bdc_transaction USING 'PA30'.
            ENDIF.


          ELSE.
            REFRESH lt_tzabs.
* Absenzen in Halbagen aufteilen.
            SPLIT lv_halbtage AT ','
            INTO lv_datab01
                 lv_datab02
                 lv_datab03
                 lv_datab04
                 lv_datab05
                 lv_datab06
                 lv_datab07
                 lv_datab08
                 lv_datab09
                 lv_datab10
                 lv_datab11
                 lv_datab12
                 lv_datab13
                 lv_datab14
                 lv_datab15
                 lv_datab16
                 lv_datab17
                 lv_datab18
                 lv_datab19
                 lv_datab20
                 lv_datab21
                 lv_datab22
                 lv_datab23
                 lv_datab24
                 lv_datab25
                 lv_datab26
                 lv_datab27
                 lv_datab28
                 lv_datab29
                 lv_datab30.

            IF NOT lv_datab01 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab01+6(4).
              lt_tzabs-datum+4(2) = lv_datab01+3(2).
              lt_tzabs-datum+6(2) = lv_datab01(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab02 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab02+6(4).
              lt_tzabs-datum+4(2) = lv_datab02+3(2).
              lt_tzabs-datum+6(2) = lv_datab02(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab03 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab03+6(4).
              lt_tzabs-datum+4(2) = lv_datab03+3(2).
              lt_tzabs-datum+6(2) = lv_datab03(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab04 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab04+6(4).
              lt_tzabs-datum+4(2) = lv_datab04+3(2).
              lt_tzabs-datum+6(2) = lv_datab04(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab05 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab05+6(4).
              lt_tzabs-datum+4(2) = lv_datab05+3(2).
              lt_tzabs-datum+6(2) = lv_datab05(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab06 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab06+6(4).
              lt_tzabs-datum+4(2) = lv_datab06+3(2).
              lt_tzabs-datum+6(2) = lv_datab06(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab07 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab07+6(4).
              lt_tzabs-datum+4(2) = lv_datab07+3(2).
              lt_tzabs-datum+6(2) = lv_datab07(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab08 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab08+6(4).
              lt_tzabs-datum+4(2) = lv_datab08+3(2).
              lt_tzabs-datum+6(2) = lv_datab08(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab09 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab09+6(4).
              lt_tzabs-datum+4(2) = lv_datab09+3(2).
              lt_tzabs-datum+6(2) = lv_datab09(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab10 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab10+6(4).
              lt_tzabs-datum+4(2) = lv_datab10+3(2).
              lt_tzabs-datum+6(2) = lv_datab10(2).
              APPEND lt_tzabs.
            ENDIF.

            IF NOT lv_datab11 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab11+6(4).
              lt_tzabs-datum+4(2) = lv_datab11+3(2).
              lt_tzabs-datum+6(2) = lv_datab11(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab12 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab12+6(4).
              lt_tzabs-datum+4(2) = lv_datab12+3(2).
              lt_tzabs-datum+6(2) = lv_datab12(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab13 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab13+6(4).
              lt_tzabs-datum+4(2) = lv_datab13+3(2).
              lt_tzabs-datum+6(2) = lv_datab13(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab14 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab14+6(4).
              lt_tzabs-datum+4(2) = lv_datab14+3(2).
              lt_tzabs-datum+6(2) = lv_datab14(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab15 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab15+6(4).
              lt_tzabs-datum+4(2) = lv_datab15+3(2).
              lt_tzabs-datum+6(2) = lv_datab15(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab16 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab16+6(4).
              lt_tzabs-datum+4(2) = lv_datab16+3(2).
              lt_tzabs-datum+6(2) = lv_datab16(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab17 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab17+6(4).
              lt_tzabs-datum+4(2) = lv_datab17+3(2).
              lt_tzabs-datum+6(2) = lv_datab17(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab18 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab18+6(4).
              lt_tzabs-datum+4(2) = lv_datab18+3(2).
              lt_tzabs-datum+6(2) = lv_datab18(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab19 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab19+6(4).
              lt_tzabs-datum+4(2) = lv_datab19+3(2).
              lt_tzabs-datum+6(2) = lv_datab19(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab20 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab20+6(4).
              lt_tzabs-datum+4(2) = lv_datab20+3(2).
              lt_tzabs-datum+6(2) = lv_datab20(2).
              APPEND lt_tzabs.
            ENDIF.


            IF NOT lv_datab21 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab21+6(4).
              lt_tzabs-datum+4(2) = lv_datab21+3(2).
              lt_tzabs-datum+6(2) = lv_datab21(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab22 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab22+6(4).
              lt_tzabs-datum+4(2) = lv_datab22+3(2).
              lt_tzabs-datum+6(2) = lv_datab22(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab23 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab23+6(4).
              lt_tzabs-datum+4(2) = lv_datab23+3(2).
              lt_tzabs-datum+6(2) = lv_datab23(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab24 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab24+6(4).
              lt_tzabs-datum+4(2) = lv_datab24+3(2).
              lt_tzabs-datum+6(2) = lv_datab24(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab25 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab25+6(4).
              lt_tzabs-datum+4(2) = lv_datab25+3(2).
              lt_tzabs-datum+6(2) = lv_datab25(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab26 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab26+6(4).
              lt_tzabs-datum+4(2) = lv_datab26+3(2).
              lt_tzabs-datum+6(2) = lv_datab26(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab27 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab27+6(4).
              lt_tzabs-datum+4(2) = lv_datab27+3(2).
              lt_tzabs-datum+6(2) = lv_datab27(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab28 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab28+6(4).
              lt_tzabs-datum+4(2) = lv_datab28+3(2).
              lt_tzabs-datum+6(2) = lv_datab28(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab29 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab29+6(4).
              lt_tzabs-datum+4(2) = lv_datab29+3(2).
              lt_tzabs-datum+6(2) = lv_datab29(2).
              APPEND lt_tzabs.
            ENDIF.
            IF NOT lv_datab30 IS INITIAL.
              lt_tzabs-datum(4) = lv_datab30+6(4).
              lt_tzabs-datum+4(2) = lv_datab30+3(2).
              lt_tzabs-datum+6(2) = lv_datab30(2).
              APPEND lt_tzabs.
            ENDIF.
*  beginn absenz

            lv_begabs(4) =   lv_begda+6(4).
            lv_begabs+4(2) = lv_begda+3(2).
            lv_begabs+6(2) = lv_begda(2).

*  ende absenz.
            lv_endabs(4) =   lv_endda+6(4).
            lv_endabs+4(2) = lv_endda+3(2).
            lv_endabs+6(2) = lv_endda(2).

            lv_begabs = lv_begabs - 1.
            DO.
              lv_begabs = lv_begabs + 1.
              IF lv_begabs > lv_endabs.
                EXIT.
              ENDIF.

              READ TABLE lt_tzabs WITH KEY datum = lv_begabs.
              IF sy-subrc = 0.
* Halbtag.
                SELECT SINGLE arbst FROM pa0007 INTO lv_arbst WHERE pernr = lv_pernr "#EC CI_NOORDER
                AND begda LE lv_begabs AND endda GE lv_begabs.
                lv_arbst = lv_arbst / 2.
                WRITE lv_arbst TO lv_arbstc.
                WRITE lv_begabs TO lv_datac.
                CASE lv_absenz.
                  WHEN 'Ferien'.
                    lv_absart = '0100'.
                    lv_dynpro = '2001'.
                    lv_infty  = '2001'.
*                WHEN 'Krankheit' OR 'Kind-Krank'.
*                  lv_absart = '0260'.
*                  lv_dynpro = '2000'.
*                  lv_infty  = '2001'.
                  WHEN 'Aus- und Weiterbildung'.
                    lv_dynpro = '2050'.
                    lv_infty  = '2002'.
                    lv_absart = '0710'.
                  WHEN 'Bezahlte Absenzen'.
                    lv_absart = '0500'.
                    lv_dynpro = '2000'.
                    lv_infty  = '2001'.
                  WHEN 'Dienstreise'.
                    lv_absart = '0700'.
                    lv_dynpro = '2051'.
                    lv_infty  = '2002'.
                  WHEN 'Kompensation' OR'Sonderurlaub'.
                    lv_absart = '0400'.
                    lv_dynpro = '2001'.
                    lv_infty  = '2001'.
                  WHEN 'Militär / Zivildienst' OR 'Mutterschaftsurlaub'.
                    lv_absart = '0600'.
                    lv_dynpro = '2000'.
                    lv_infty  = '2001'.
                  WHEN 'Unbezahlter Urlaub'.
                    lv_absart = '0900'.
                    lv_dynpro = '2000'.
                    lv_infty  = '2001'.
*                  WHEN 'Unfall'.
*                    lv_absart = '0290'.
*                    lv_dynpro = '2000'.
*                    lv_infty  = '2001'.
*        WHEN 'Vaterschaftsurlaub'.
*          lv_absart = '0500'.
                ENDCASE.
                CLEAR   bdcdata.
                REFRESH bdcdata.
                WRITE: / 'OK  Absenz. Halbtag', lv_pernr, lv_absenz, lv_absart, lv_datac,lv_datac.
                IF p_test IS INITIAL.

*       Einstiegsbild Personalstammdaten pflegen
                  IF lv_infty = '2001'.


                    PERFORM bdc_dynpro      USING 'SAPMP50A'    '1000'.
                    PERFORM bdc_field:      USING 'RP50G-PERNR' lv_pernr,
                                            USING 'RP50G-BEGDA' lv_datac,
                                             USING 'RP50G-ENDDA' lv_datac,
                                            USING 'RP50G-CHOIC' lv_infty,
                                            USING 'RP50G-SUBTY' lv_absart,
                                            USING 'BDC_OKCODE'  '=INS'.
                    PERFORM bdc_dynpro      USING 'MP200000'    lv_dynpro.

                    PERFORM bdc_field:      USING 'P2001-STDAZ'  lv_arbstc,
                                            USING 'BDC_OKCODE'  '=UPD'.
*         Tansaktion aufrufen
                    PERFORM bdc_transaction USING 'PA30'.
                  ELSE.
                    PERFORM bdc_dynpro      USING 'SAPMP50A'    '1000'.
                    PERFORM bdc_field:      USING 'RP50G-PERNR' lv_pernr,
                                            USING 'RP50G-BEGDA' lv_datac,
                                             USING 'RP50G-ENDDA' lv_datac,
                                            USING 'RP50G-CHOIC' lv_infty,
                                            USING 'RP50G-SUBTY' lv_absart,
                                            USING 'BDC_OKCODE'  '=INS'.
                    PERFORM bdc_dynpro      USING 'MP200000'    lv_dynpro.

                    PERFORM bdc_field:      USING 'P2002-STDAZ'  lv_arbstc,
                                            USING 'BDC_OKCODE'  '=UPD'.
*         Tansaktion aufrufen
                    PERFORM bdc_transaction USING 'PA30'.
                  ENDIF.


                ENDIF.









              ELSE.
                WRITE lv_begabs TO lv_datac.
                CASE lv_absenz.
                  WHEN 'Ferien'.
                    lv_absart = '0100'.
                    lv_dynpro = '2001'.
                    lv_infty  = '2001'.
*                WHEN 'Krankheit' OR 'Kind-Krank'.
*                  lv_absart = '0260'.
*                  lv_dynpro = '2000'.
*                  lv_infty  = '2001'.
                  WHEN 'Aus- und Weiterbildung'.
                    lv_dynpro = '2050'.
                    lv_infty  = '2002'.
                    lv_absart = '0710'.
                  WHEN 'Bezahlte Absenzen'.
                    lv_absart = '0500'.
                    lv_dynpro = '2000'.
                    lv_infty  = '2001'.
                  WHEN 'Dienstreise'.
                    lv_absart = '0700'.
                    lv_dynpro = '2051'.
                    lv_infty  = '2002'.
                  WHEN 'Kompensation' OR 'Sonderurlaub'.
                    lv_absart = '0400'.
                    lv_dynpro = '2001'.
                    lv_infty  = '2001'.
                  WHEN 'Militär / Zivildienst' OR 'Mutterschaftsurlaub'.
                    lv_absart = '0600'.
                    lv_dynpro = '2000'.
                    lv_infty  = '2001'.
                  WHEN 'Unbezahlter Urlaub'.
                    lv_absart = '0900'.
                    lv_dynpro = '2000'.
                    lv_infty  = '2001'.
*                  WHEN 'Unfall'.
*                    lv_absart = '0290'.
*                    lv_dynpro = '2000'.
*                    lv_infty  = '2001'.
                  WHEN 'Vaterschaftsurlaub'.
                    lv_infty  = '2001'.
                    lv_absart = '0250'.
                    lv_dynpro = '2000'.
                    lv_infty  = '2001'.

                ENDCASE.
                CLEAR   bdcdata.
                REFRESH bdcdata.
                WRITE: / 'OK  Absenz. Ganztag', lv_pernr, lv_absenz, lv_absart, lv_datac,lv_datac.
                IF p_test IS INITIAL.

*       Einstiegsbild Personalstammdaten pflegen



                  PERFORM bdc_dynpro      USING 'SAPMP50A'    '1000'.
                  PERFORM bdc_field:      USING 'RP50G-PERNR' lv_pernr,
                                          USING 'RP50G-BEGDA' lv_datac,
                                           USING 'RP50G-ENDDA' lv_datac,
                                          USING 'RP50G-CHOIC' lv_infty,
                                          USING 'RP50G-SUBTY' lv_absart,
                                          USING 'BDC_OKCODE'  '=INS'.
                  PERFORM bdc_dynpro      USING 'MP200000'    lv_dynpro.
                  PERFORM bdc_field:      USING 'BDC_OKCODE'  '=UPD'.

*         Tansaktion aufrufen
                  PERFORM bdc_transaction USING 'PA30'.
                ENDIF.
              ENDIF.
            ENDDO.
          ENDIF.
        ENDIF.
      ENDIF.
    ENDIF.
  ENDLOOP.


  IF p_test IS INITIAL.
*   Mappe für BI schliessen
    PERFORM close_group.

*   Meldung Mappe wurde angelegt
*    MESSAGE s012 WITH p_mapnam.
  ENDIF.

* Fehlerliste ausgeben
*  LOOP AT it_error INTO wa_error.
*    WRITE:/ wa_error-pos,
*    AT 12   wa_error-pernr,
*    AT 30   wa_error-text,
*    AT 80   wa_error-value.
*  ENDLOOP.

END-OF-SELECTION.

*
*----------------------------------------------------------------------*
*       FORM DYNPRO.                                                   *
*----------------------------------------------------------------------*
*  -->  DYNBEGIN                                                       *
*  -->  NAME                                                           *
*  -->  VALUE                                                          *
*----------------------------------------------------------------------*
FORM dynpro USING dynbegin name value.
  IF dynbegin = 'X'.
    CLEAR bdcdata.
    MOVE: name TO bdcdata-program,
    value TO bdcdata-dynpro,
    'X' TO bdcdata-dynbegin.
    APPEND bdcdata.
  ELSE.
    CLEAR bdcdata.
    MOVE: name TO bdcdata-fnam,
    value TO bdcdata-fval.
    APPEND bdcdata.
  ENDIF.
ENDFORM.                    "DYNPRO



*&---------------------------------------------------------------------*
*&      Form  open_group
*&---------------------------------------------------------------------*
FORM open_group  USING    p_mandt
                          p_mapnam
                          p_uname.

  CALL FUNCTION 'BDC_OPEN_GROUP'
    EXPORTING
      client = p_mandt
      group  = p_mapnam
      user   = p_uname.

ENDFORM.                    " open_group
*&---------------------------------------------------------------------*
*&      Form  close_group
*&---------------------------------------------------------------------*
FORM close_group .

  CALL FUNCTION 'BDC_CLOSE_GROUP'.

ENDFORM.                    " close_group

*----------------------------------------------------------------------*
*        Insert field                                                  *
*----------------------------------------------------------------------*
FORM bdc_field USING fnam fval.
  IF fval <> nodata.
    CLEAR bdcdata.
    bdcdata-fnam = fnam.
    bdcdata-fval = fval.
    APPEND bdcdata.
  ENDIF.
ENDFORM.                    "BDC_FIELD

*----------------------------------------------------------------------*
*        Start new screen                                              *
*----------------------------------------------------------------------*
FORM bdc_dynpro USING program dynpro.
  CLEAR bdcdata.
  bdcdata-program  = program.
  bdcdata-dynpro   = dynpro.
  bdcdata-dynbegin = 'X'.
  APPEND bdcdata.
ENDFORM.                    "BDC_DYNPRO

*----------------------------------------------------------------------*
*        Start new transaction according to parameters                 *
*----------------------------------------------------------------------*
FORM bdc_transaction USING tcode.
  CALL FUNCTION 'BDC_INSERT'
    EXPORTING
      tcode     = tcode
    TABLES
      dynprotab = bdcdata.
  REFRESH bdcdata. CLEAR bdcdata.

ENDFORM.                                                    "PA30
*&---------------------------------------------------------------------*
*&      Form  change_sign
*&---------------------------------------------------------------------*
FORM change_sign  CHANGING p_wert.

* Dezimalzeichen umsetzen
  TRANSLATE p_wert USING '.,'.

ENDFORM.                    " change_sign
*&---------------------------------------------------------------------*
*&      Form  create_error_list
*&---------------------------------------------------------------------*
FORM create_error_list  USING p_pernr p_number p_value.

*  ADD 1 TO wf_pos.
*  wa_error-pos = wf_pos.
*  wa_error-pernr = p_pernr.
*  MESSAGE ID 'ZPYCMPDB01' TYPE 'E' NUMBER p_number
*            INTO wa_error-text.
*  wa_error-value = p_value.
*
*  APPEND wa_error TO it_error.

ENDFORM.                    " create_error_list
*&---------------------------------------------------------------------*
*&      Form  get_filename
*&---------------------------------------------------------------------*
FORM get_filename  CHANGING p_filename.

  DATA: repid LIKE sy-repid,
        dynnr LIKE sy-dynnr,
        idynp LIKE dynpread OCCURS 0 WITH HEADER LINE.

* Dynpro Infos ziehen
  repid = sy-repid.
  dynnr = sy-dynnr.

* Pfad lesen
  CALL FUNCTION 'WS_FILENAME_GET'
    EXPORTING
      def_filename     = space
      def_path         = p_filename
      mask             = ',*.*  ,*.*.'
      mode             = 'O'
    IMPORTING
      filename         = p_filename
    EXCEPTIONS
      inv_winsys       = 04
      no_batch         = 08
      selection_cancel = 12
      selection_error  = 16.

* Pfad auf Dynpro zurückschreiben
  CALL FUNCTION 'DYNP_VALUES_UPDATE'
    EXPORTING
      dyname     = repid
      dynumb     = dynnr
    TABLES
      dynpfields = idynp.


ENDFORM.                    " get_filename
