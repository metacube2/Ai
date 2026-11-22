*&---------------------------------------------------------------------*
*& Report Z_PRODSPARTE_MAP_EXPORT
*&---------------------------------------------------------------------*
*& Zweck: Exportiert die flache Mapping-Tabelle ZPRODSPARTE_MAP zur
*&        Kontrolle gegen Data(4)/KEDE-Referenz.
*&
*& Erwartung nach Z_PRODSPARTE_MAP_BUILD:
*&   Alle Data(4)-Referenzcodes muessen hier mit gleicher WWPFA stehen.
*&---------------------------------------------------------------------*
REPORT z_prodsparte_map_export.

PARAMETERS: p_file TYPE string LOWER CASE
                   DEFAULT 'C:\temp\zprodsparte_map_export.csv'.

START-OF-SELECTION.

  SELECT paph1, wwpfa, wwpsp
    FROM zprodsparte_map
    INTO TABLE @DATA(lt_map)
    WHERE paph1 <> @space.

  SORT lt_map BY paph1.

  DATA: lt_csv TYPE STANDARD TABLE OF string,
        lv_sep TYPE c LENGTH 1.

  lv_sep = ';'.

  APPEND |PAPH1{ lv_sep }WWPFA{ lv_sep }WWPSP| TO lt_csv.

  LOOP AT lt_map INTO DATA(ls_map).
    APPEND |{ ls_map-paph1 }{ lv_sep }{ ls_map-wwpfa }{ lv_sep }{ ls_map-wwpsp }|
      TO lt_csv.
  ENDLOOP.

  cl_gui_frontend_services=>gui_download(
    EXPORTING
      filename = p_file
      filetype = 'ASC'
    CHANGING
      data_tab = lt_csv
    EXCEPTIONS
      OTHERS   = 1 ).

  IF sy-subrc = 0.
    WRITE: / lines( lt_map ), 'Mapping-Saetze exportiert nach', p_file.
  ELSE.
    WRITE: / 'Download-Fehler, sy-subrc=', sy-subrc.
  ENDIF.
