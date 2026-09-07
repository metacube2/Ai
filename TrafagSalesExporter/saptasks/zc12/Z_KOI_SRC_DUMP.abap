report z_koi_src_dump.
*&---------------------------------------------------------------------*
*& Hilfsreport, rein lesend. Schreibt den Quelltext eines Programms
*& originalgetreu auf einen festen Pfad, weil der ABAP-Editor ueber die
*& GUI-Scripting-Schnittstelle nicht auslesbar ist.
*&
*& Bewusst ohne Selektionsbild und ohne Dateidialog: beides waere ueber
*& die Fernsteuerung zusaetzlicher Aufwand, und der Report ist ein
*& Wegwerfobjekt in $TMP.
*&---------------------------------------------------------------------*

data: lt_src  type standard table of string,
      lv_cnt  type i,
      lv_file type string.

start-of-selection.

  lv_file = 'C:\Users\koi\source\repos\Ai\TrafagSalesExporter\saptasks\zc12\ZM_ABGLEICH_KTSCH_vorher.abap'.

  read report 'ZM_ABGLEICH_KTSCH' into lt_src.
  if sy-subrc <> 0.
    write: / 'FEHLER: READ REPORT subrc', sy-subrc.
    return.
  endif.

  describe table lt_src lines lv_cnt.

  call method cl_gui_frontend_services=>gui_download
    exporting
      filename = lv_file
      filetype = 'ASC'
      codepage = '4110'
    changing
      data_tab = lt_src
    exceptions
      others   = 1.

  if sy-subrc = 0.
    write: / 'OK', lv_cnt, 'Zeilen geschrieben'.
  else.
    write: / 'FEHLER: GUI_DOWNLOAD subrc', sy-subrc.
  endif.
