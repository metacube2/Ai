*&---------------------------------------------------------------------*
*& Report Z_HR_PA_IMP_ZEITEREIGNISSE
*&---------------------------------------------------------------------*
*&
*&---------------------------------------------------------------------*
REPORT z_hr_pa_imp_zeitereignisse.

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
DATA: ls_teven TYPE teven.
DATA: lv_time TYPE time,
      lv_date TYPE date.

*-----------------------------------------------------------------------
* local variables
*-----------------------------------------------------------------------

DATA: nodata(1) VALUE ' '.          "nodata
DATA: lv_upload TYPE string.

DATA: lv_begda TYPE dats.
DATA: lv_begdac(10).
DATA: lv_lgart(4).
DATA: lv_lgartin(6).
DATA: lv_betrgc(10).
DATA lv_betrg LIKE pa0015-betrg.
DATA: lv_pernrz TYPE persno.
DATA ls_0015 TYPE pa0015.
DATA: lv_answer(1).

DATA: BEGIN OF lt_imp OCCURS 100,
        pernr(8),
        zeit(8),
        datum(10),
        typ(6),
        anzahl    TYPE p,

      END OF lt_imp.


DATA: lv_pernr(8),
      lv_zeit(8),
      lv_datum(10),
      lv_erstellung(20),
      lv_status(20),
      lv_typ(6),
      lv_typ_sap(3),
      lv_zeitstempel(20),
      lv_pnrsap TYPE persno.
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

SELECTION-SCREEN END OF BLOCK 01.
SELECTION-SCREEN BEGIN OF BLOCK 02 WITH FRAME TITLE TEXT-s02.
  PARAMETERS:
    p_mapnam LIKE rptaxxxx-map_name DEFAULT 'ZEII2011' OBLIGATORY,
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

  REFRESH lt_imp.

  LOOP AT lt_import INTO ls_imp.
    IF sy-tabix > 1.
      SPLIT ls_imp AT ';'
      INTO lv_pernr
           lv_zeit
           lv_datum
           lv_erstellung
           lv_status
           lv_typ
           lv_zeitstempel.
      IF lv_status = 'IST' OR lv_status = 'Ist'.
        lt_imp-pernr = lv_pernr.
        lt_imp-zeit = lv_zeit.
        lt_imp-datum = lv_datum.
        lt_imp-typ = lv_typ.
        lt_imp-anzahl = 1.
        COLLECT lt_imp.
      ENDIF.
    ENDIF.
  ENDLOOP.
  LOOP AT lt_imp.   "* WHERE anzahl = 1
*OR anzahl = 3
*OR anzahl = 5
*OR anzahl = 7.



    CASE lt_imp-typ.
      WHEN 'KOMMEN'.
        lv_typ_sap = 'P10'.
      WHEN 'GEHEN'.
        lv_typ_sap = 'P20'.
    ENDCASE.

* Personalnummer REXX -> SAP
    SELECT SINGLE pernr INTO lv_pnrsap FROM pa0032 WHERE pnalt = lt_imp-pernr. "#EC CI_NOORDER
    IF sy-subrc = 0.
      lt_imp-pernr    = lv_pnrsap.
    ENDIF.

    CLEAR   bdcdata.
    REFRESH bdcdata.
    lv_Datum = lt_imp-datum.
    lv_zeit = lt_imp-zeit.
    lv_typ = lt_imp-typ.

* Prüfe, ob Record bereits im System
    lv_date(4)  = lv_datum+6(4).
    lv_Date+4(2) = lv_datum+3(2).
    lv_date+6(2) = lv_datum(2).

    lv_time(2) = lv_zeit(2).
    lv_time+2(2) = lv_zeit+3(2).
    lv_time+4(2) = lv_zeit+6(2).
    SELECT SINGLE * FROM teven INTO ls_teven WHERE
    pernr = lv_pernr
    AND ldate = lv_date
    AND ltime = lv_time
    AND satza = lv_typ_sap.
    IF sy-subrc = 0.
      WRITE: /  'ERR',lt_imp-pernr,
 lv_datum, lv_zeit, lv_typ, lv_typ_sap,
        'Datensatz schon im System'.

    ELSE.
      IF p_test IS INITIAL.
**
*
*       Einstiegsbild Personalstammdaten pflegen (IT2011)



        PERFORM bdc_dynpro      USING 'SAPMP50A'    '1000'.
        PERFORM bdc_field:      USING 'RP50G-PERNR' lt_imp-pernr,
                                using 'RP50G-BEGDA' lv_datum,
                                USING 'RP50G-CHOIC' '2011',
                                USING 'BDC_OKCODE'  '=INS'.
        PERFORM bdc_dynpro      USING 'MP200000'    '2500'.
        PERFORM bdc_field:      USING 'P2011-LDATE'  lv_datum,
                                USING 'P2011-LTIME'  lv_zeit,
                                USING 'P2011-SATZA'  lv_typ_sap,
                                USING 'BDC_OKCODE'  '=UPD'.

*         Tansaktion aufrufen
        PERFORM bdc_transaction USING 'PA30'.
      ENDIF.
      WRITE: / 'OK ', lt_imp-pernr, lv_datum, lv_zeit, lv_typ, lv_typ_sap.
    ENDIF.
  ENDLOOP.



  IF p_test IS INITIAL.
*   Mappe für BI schliessen
    PERFORM close_group.

*   Meldung Mappe wurde angelegt
*    MESSAGE s012 WITH p_mapnam.
  ENDIF.



END-OF-SELECTION.

*&---------------------------------------------------------------------*
*&  Include           ZHR_INCL_UKA_IMPORT_0015_FORMS
*&---------------------------------------------------------------------*
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
