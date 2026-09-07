report z_koi_src_dump.
*&---------------------------------------------------------------------*
*& Hilfsreport, rein lesend. Schreibt Quelltext originalgetreu auf einen
*& festen Pfad, weil der ABAP-Editor ueber die GUI-Scripting-Schnittstelle
*& nicht auslesbar ist.
*&
*& Zwei Betriebsarten ueber p_modus:
*&   R = Report/Programm, Name in p_prog
*&   M = Methodenrumpf einer Klasse, p_clas und p_meth
*&
*& Bewusst mit Selektionsbild, aber mit Vorbelegung: so laesst er sich per
*& Fernsteuerung ohne Dateidialog fuellen und ausfuehren. Wegwerfobjekt in $TMP.
*&---------------------------------------------------------------------*

parameters:
  p_modus type c length 1 default 'M',
  p_prog  type programm   default 'ZM_ABGLEICH_KTSCH',
  p_clas  type seoclsname default 'ZCL_IM__ZZPRDAT_UPDATE',
  p_meth  type seocpdname default 'IF_EX_WORKORDER_UPDATE~BEFORE_UPDATE',
  p_file  type string lower case
          default 'C:\Users\koi\source\repos\Ai\TrafagSalesExporter\saptasks\zzprdat\system\dump.abap'.

data: lt_src  type standard table of string,
      lv_cnt  type i,
      lv_prog type programm,
      ls_key  type seocpdkey.

start-of-selection.

  if p_modus = 'M'.
    ls_key-clsname = p_clas.
    ls_key-cpdname = p_meth.
    lv_prog = cl_oo_classname_service=>get_method_include( mtdkey = ls_key ).
    if lv_prog is initial.
      write: / 'FEHLER: kein Methodeninclude zu', p_clas, p_meth.
      return.
    endif.
    write: / 'Include:', lv_prog.
  else.
    lv_prog = p_prog.
  endif.

  read report lv_prog into lt_src.
  if sy-subrc <> 0.
    write: / 'FEHLER: READ REPORT subrc', sy-subrc, 'fuer', lv_prog.
    return.
  endif.

  describe table lt_src lines lv_cnt.

  call method cl_gui_frontend_services=>gui_download
    exporting
      filename = p_file
      filetype = 'ASC'
      codepage = '4110'
    changing
      data_tab = lt_src
    exceptions
      others   = 1.

  if sy-subrc = 0.
    write: / 'OK', lv_cnt, 'Zeilen geschrieben nach', p_file.
  else.
    write: / 'FEHLER: GUI_DOWNLOAD subrc', sy-subrc.
  endif.
