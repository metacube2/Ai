*&---------------------------------------------------------------------*
*& Report Z_PRODSPARTE_ALL
*&---------------------------------------------------------------------*
*& Zweck: Einziger ausfuehrbarer Report rund um die Provider-Logik.
*&        Ruft ZCL_PRODSPARTE_PROVIDER=>GET_DATA und kann das Ergebnis
*&        als ALV anzeigen oder als tab-getrennte CSV exportieren.
*&---------------------------------------------------------------------*
REPORT z_prodsparte_all.

PARAMETERS: p_vkorg TYPE vkorg OBLIGATORY.
PARAMETERS: p_vtweg TYPE vtweg.
PARAMETERS: p_spras TYPE spras DEFAULT sy-langu.
PARAMETERS: p_fallb TYPE t25a1-bezek DEFAULT 'Nicht zugeordnet'.

SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME.
PARAMETERS: p_alv  TYPE abap_bool DEFAULT 'X' AS CHECKBOX.
PARAMETERS: p_csv  TYPE abap_bool AS CHECKBOX.
PARAMETERS: p_file TYPE string LOWER CASE
                   DEFAULT 'C:\temp\prodsparte_export.csv'.
SELECTION-SCREEN END OF BLOCK b1.

START-OF-SELECTION.

  DATA(lt_data) = zcl_prodsparte_provider=>get_data(
                    iv_vkorg    = p_vkorg
                    iv_vtweg    = p_vtweg
                    iv_spras    = p_spras
                    iv_fallback = p_fallb ).

  IF lt_data IS INITIAL.
    MESSAGE 'Keine Daten - VKORG/VTWEG pruefen.' TYPE 'I'.
    RETURN.
  ENDIF.

  WRITE: / 'Gelesene Saetze:', lines( lt_data ).

  IF p_csv = abap_true.
    PERFORM export_csv USING lt_data p_file.
  ENDIF.

  IF p_alv = abap_true.
    PERFORM show_alv USING lt_data.
  ENDIF.

FORM export_csv USING it_data TYPE zcl_prodsparte_provider=>tt_out
                      iv_file TYPE string.

  DATA: lt_csv TYPE STANDARD TABLE OF string,
        lv_sep TYPE c LENGTH 1.

  lv_sep = cl_abap_char_utilities=>horizontal_tab.

  APPEND |MATNR{ lv_sep }PAPH1{ lv_sep }PAPH1_TEXT{ lv_sep }WWPFA{ lv_sep }|
      && |WWPFA_TEXT{ lv_sep }WWPSP{ lv_sep }WWPSP_TEXT{ lv_sep }|
      && |IS_ASSIGNED{ lv_sep }MAKTX| TO lt_csv.

  LOOP AT it_data INTO DATA(ls).
    APPEND |{ ls-matnr }{ lv_sep }{ ls-paph1 }{ lv_sep }{ ls-paph1_text }|
        && |{ lv_sep }{ ls-wwpfa }{ lv_sep }{ ls-wwpfa_text }|
        && |{ lv_sep }{ ls-wwpsp }{ lv_sep }{ ls-wwpsp_text }|
        && |{ lv_sep }{ ls-is_assigned }{ lv_sep }{ ls-maktx }|
        TO lt_csv.
  ENDLOOP.

  cl_gui_frontend_services=>gui_download(
    EXPORTING
      filename = iv_file
      filetype = 'ASC'
    CHANGING
      data_tab = lt_csv
    EXCEPTIONS
      OTHERS   = 1 ).

  IF sy-subrc = 0.
    WRITE: / lines( it_data ), 'Saetze exportiert nach', iv_file.
  ELSE.
    WRITE: / 'Download-Fehler, sy-subrc=', sy-subrc.
  ENDIF.

ENDFORM.

FORM show_alv USING it_data TYPE zcl_prodsparte_provider=>tt_out.

  DATA lt_alv TYPE zcl_prodsparte_provider=>tt_out.

  lt_alv = it_data.

  cl_salv_table=>factory(
    IMPORTING
      r_salv_table = DATA(lo_alv)
    CHANGING
      t_table      = lt_alv ).

  lo_alv->get_functions( )->set_all( abap_true ).
  lo_alv->get_columns( )->set_optimize( abap_true ).
  lo_alv->display( ).

ENDFORM.
