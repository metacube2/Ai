*&---------------------------------------------------------------------*
*#HEADER---------------------------------------------------------------*
* DESCRIPTION:                                                         *
*                                                                      *
* Process ID : N/A                                                     *
* Reason     : Project   : REXX                                        *
*            : OSS Note  : N/A                                         *
*            : Business Owner :                                        *
*            : other      : N/A                                        *
* Give a short description...........                                  *
*----------------------------------------------------------------------*
* Change Log:                                                          *
*                                                                      *
* Who        Date       Version      Text                              *
* EXPRIS AG  01.10.2023 <000>        Initial Version                   *
*                                                                      *
*&HEADER---------------------------------------------------------------*
REPORT z_hr_pa_interface_rexx.

SELECTION-SCREEN BEGIN OF BLOCK bl1 WITH FRAME TITLE TEXT-t01.
  SELECTION-SCREEN BEGIN OF LINE.
    PARAMETERS: p_appl TYPE boole_d RADIOBUTTON GROUP gr1.
    SELECTION-SCREEN COMMENT  5(27) TEXT-s01.
    PARAMETERS: p_dir  TYPE dirname.
  SELECTION-SCREEN END   OF LINE.

  SELECTION-SCREEN BEGIN OF LINE.
    SELECTION-SCREEN COMMENT  5(27) TEXT-s03.
    PARAMETERS:     p_osfile     TYPE localfile.
  SELECTION-SCREEN END   OF LINE.

  SELECTION-SCREEN BEGIN OF LINE.
    PARAMETERS: p_fron TYPE boole_d RADIOBUTTON GROUP gr1 DEFAULT 'X'.
    SELECTION-SCREEN COMMENT  5(27) TEXT-s02.
  SELECTION-SCREEN END   OF LINE.
SELECTION-SCREEN END OF BLOCK bl1.

*SELECTION-SCREEN BEGIN OF BLOCK bl2 WITH FRAME TITLE TEXT-t02.
*  PARAMETERS p_wf TYPE boole_d AS CHECKBOX.
*SELECTION-SCREEN END OF BLOCK bl2.

*SELECTION-SCREEN BEGIN OF BLOCK bl3 WITH FRAME TITLE TEXT-t03.
*  PARAMETERS: p_begda TYPE begda,
*              p_sydat TYPE boole_d AS CHECKBOX DEFAULT 'X'.
*SELECTION-SCREEN END OF BLOCK bl3.

SELECTION-SCREEN BEGIN OF BLOCK bl4 WITH FRAME TITLE TEXT-t04.
  PARAMETERS p_alv TYPE boole_d AS CHECKBOX.
SELECTION-SCREEN END OF BLOCK bl4.

INITIALIZATION.

  "Get instance of class
  DATA(gr_interface_rexx_read) = NEW zcl_hr_pa_interface_rexx_read( sy-cprog ) ##NEEDED.

AT SELECTION-SCREEN ON VALUE-REQUEST FOR p_dir.

  CALL METHOD zcl_hr_pa_interface_rexx_read=>show_user_directory
    CHANGING
      cv_dir = p_dir.

AT SELECTION-SCREEN ON VALUE-REQUEST FOR p_osfile.

  CALL METHOD zcl_hr_pa_interface_rexx_read=>server_verzeichnis
    CHANGING
      cv_dir    = p_dir
      cv_osfile = p_osfile.

START-OF-SELECTION.

  TRY.
      "Start processing
      gr_interface_rexx_read->start_process( ).
    CATCH zcx_hr_pa_interface_rexx INTO DATA(gr_cx) ##NEEDED.
      MESSAGE ID gr_cx->if_t100_message~t100key-msgid
         TYPE 'S'
       NUMBER gr_cx->if_t100_message~t100key-msgno
         WITH gr_cx->if_t100_message~t100key-attr1 gr_cx->if_t100_message~t100key-attr2
              gr_cx->if_t100_message~t100key-attr3 gr_cx->if_t100_message~t100key-attr4
      DISPLAY LIKE gr_cx->if_t100_dyn_msg~msgty.
  ENDTRY.
