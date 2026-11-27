*&---------------------------------------------------------------------*
*& Report  ZMM_CHECK_UMTERMINIERUNG                                     *
*&                                                                     *
*&---------------------------------------------------------------------*
*&                                                                     *
*&                                                                     *
*&---------------------------------------------------------------------*

report zmm_check_umterminierung.

tables: mara, marc, makt, ekpo, eket, resb, vbbe.

selection-screen begin of block b1 with frame title text-t01.
select-options: s_dispo for marc-dispo obligatory memory id dgr.
parameters: p_werks like t001w-werks obligatory memory id wrk.
selection-screen end of block b1.

*&---------------------------------------------------------------------*
data: imarc like marc occurs 0 with header line.
data: iverb like iverb occurs 0 with header line.

data: begin of bdtab occurs 0,
        bdter like resb-bdter,
        plumi(1),
        bdmng like resb-bdmng,
      end of bdtab.

data: w_lbkum like mbew-lbkum,
      w_ende_wbz like sy-datum,
      w_verbr like mver-gsv01,
      w_verbr_q1 like mver-gsv01,
      w_verbr_q2 like mver-gsv01,
      w_verbr_q3 like mver-gsv01,
      w_verbr_q4 like mver-gsv01,
      w_m1 like mver-gsv01,
      w_m2 like mver-gsv01,
      w_datum_von like sy-datum,
      w_datum_bis like sy-datum.

data it_matnr type standard table of zmm_check_umterm.
data wa_matnr type zmm_check_umterm.

data okcode type sy-ucomm.
data oksave like okcode.

data: rahmen type ref to cl_gui_custom_container,
      inhalt type ref to cl_gui_alv_grid,
      gs_variant type disvariant.

include <icon>.

*&---------------------------------------------------------------------*
*& Klassen
*&---------------------------------------------------------------------*
* LOCAL CLASSES: Definition
class lcl_event_receiver definition.
  public section.
    methods:
    handle_toolbar
        for event toolbar of cl_gui_alv_grid
            importing e_object e_interactive,
    handle_user_command
        for event user_command of cl_gui_alv_grid
            importing e_ucomm.
  private section.
endclass.                    "lcl_event_receiver DEFINITION

* LOCAL CLASSES: Implementationen
class lcl_event_receiver implementation.
  method handle_toolbar.

    data: ls_toolbar  type stb_button.

    clear ls_toolbar.
    move 3 to ls_toolbar-butn_type.
    append ls_toolbar to e_object->mt_toolbar.

    clear ls_toolbar.
    move 'MD04' to ls_toolbar-function.
    move icon_select_detail to ls_toolbar-icon.
    move text-anz to ls_toolbar-quickinfo.
    move text-anz to ls_toolbar-text.
    move ' ' to ls_toolbar-disabled.
    append ls_toolbar to e_object->mt_toolbar.

  endmethod.                    "handle_toolbar

  method handle_user_command.

    data: lt_rows type lvc_t_row.

    case e_ucomm.
      when 'MD04'.
        call method inhalt->get_selected_rows
          importing
            et_index_rows = lt_rows.
        call method cl_gui_cfw=>flush.
        perform details_anzeigen tables lt_rows.
    endcase.
  endmethod.                           "handle_user_command
endclass.                    "lcl_event_receiver IMPLEMENTATION

data: event_receiver type ref to lcl_event_receiver.

*&---------------------------------------------------------------------*
*& Verarbeitung
*&---------------------------------------------------------------------*
start-of-selection.
  w_datum_bis = sy-datum.
  while w_datum_bis+4(2) = sy-datum+4(2).
    w_datum_bis = w_datum_bis - 1.
  endwhile.
  w_datum_von = w_datum_bis - 360.
  w_datum_von+6(2) = '01'.

  refresh it_matnr.
  perform read_material.
  perform check_unterdeckung.

  if it_matnr[] is initial.
    message s451(se).
    leave to transaction sy-tcode.
  else.
    sort it_matnr by matnr.
    perform listausgabe.
  endif.

*&---------------------------------------------------------------------*
*&      Form  read_material
*&---------------------------------------------------------------------*
form read_material .
  perform fortschritt using text-005.
  refresh: imarc.
  select * from marc into table imarc where werks = p_werks
                                      and   dispo in s_dispo
                                      and   disgr = '0011'.
endform.                    " read_material

*&---------------------------------------------------------------------*
*&      Form  check_unterdeckung
*&---------------------------------------------------------------------*
form check_unterdeckung.
  perform fortschritt using text-002.
  loop at imarc.
    clear wa_matnr.
    w_ende_wbz = sy-datum + imarc-plifz.
    select single lbkum into w_lbkum from mbew where matnr = imarc-matnr
                                               and   bwkey = imarc-werks
                                               and   bwtar = space.
    check sy-subrc = 0.

    refresh bdtab.
    select * from resb where matnr = imarc-matnr
                       and   werks = imarc-werks
                       and   xloek = ' '
                       and   kzear = ' '
                       and   bdter le w_ende_wbz
                       and   schgt = ' '
                       and   dumps = ' '
                       and   shkzg = 'H'.
      bdtab-bdter = resb-bdter.
      bdtab-bdmng = resb-bdmng - resb-enmng.
      bdtab-plumi = '2'.
      append bdtab.
    endselect.

    select * from vbbe where matnr = imarc-matnr
                       and   werks = imarc-werks
                       and   sobkz = ' '
                       and   mbdat le w_ende_wbz.
      bdtab-bdter = vbbe-mbdat.
      bdtab-bdmng = vbbe-omeng.
      bdtab-plumi = '2'.
      append bdtab.
    endselect.

    select * from ekpo where matnr = imarc-matnr
                       and   werks = imarc-werks
                       and   ( bstyp = 'F' or bstyp = 'L' )
                       and   loekz = space
                       and   elikz = space.
      select * from eket where ebeln = ekpo-ebeln
                         and   ebelp = ekpo-ebelp.
        check eket-wemng lt eket-menge.
        bdtab-bdter = eket-eindt.
        bdtab-bdmng = eket-menge - eket-wemng.
        bdtab-plumi = '1'.
        append bdtab.
      endselect.
    endselect.

    sort bdtab by bdter plumi.
    loop at bdtab.
      if bdtab-plumi = '1'.
        w_lbkum = w_lbkum + bdtab-bdmng.
      else.
        w_lbkum = w_lbkum - bdtab-bdmng.
      endif.
      if w_lbkum lt 0.
        wa_matnr-zzudek = bdtab-bdter.
        exit.
      endif.
    endloop.

    check not wa_matnr-zzudek is initial.

    loop at bdtab.
      if bdtab-bdter gt wa_matnr-zzudek and
       bdtab-plumi = '1'.
        wa_matnr-matnr = imarc-matnr.
        exit.
      endif.
    endloop.
    check not wa_matnr is initial.
    perform anreichern_itab.

    append wa_matnr to it_matnr.
  endloop.
endform.                    "check_unterdeckung

*&---------------------------------------------------------------------*
*&      Form  anreichern_itab
*&---------------------------------------------------------------------*
form anreichern_itab.
  wa_matnr-matnr = imarc-matnr.
  wa_matnr-dismm = imarc-dismm.
  select single maktx into wa_matnr-maktx from makt where matnr = imarc-matnr
                                                    and   spras = sy-langu.
  select single zzlzcod into wa_matnr-zzlzcod from mara where matnr = imarc-matnr.
  perform verbrauch.
  wa_matnr-zzvbwbz = w_verbr * ( imarc-plifz + imarc-plifz ) / 365.
  wa_matnr-zzsibe_akt = imarc-eisbe.
  wa_matnr-zzsibe_opt = wa_matnr-zzvbwbz.
  wa_matnr-zzantpl = 0.
  loop at bdtab.
    if bdtab-bdter le w_ende_wbz and
      bdtab-plumi = '2'.
      wa_matnr-zzsibe_opt = wa_matnr-zzsibe_opt - bdtab-bdmng.
      wa_matnr-zzantpl = wa_matnr-zzantpl + bdtab-bdmng.
    endif.
  endloop.
  if wa_matnr-zzvbwbz gt 0.
    wa_matnr-zzantpl = wa_matnr-zzantpl * 100 / wa_matnr-zzvbwbz.
  endif.
  if wa_matnr-zzsibe_opt lt 0.
    wa_matnr-zzsibe_opt = 0.
  endif.

  wa_matnr-zztendenz = icon_green_light.
  if w_verbr_q4 lt w_verbr_q3 and
    w_verbr_q3 lt w_verbr_q2 and
    w_verbr_q2 lt w_verbr_q1.
    wa_matnr-zztendenz = icon_red_light.
  else.
    w_m1 = w_verbr_q1 + w_verbr_q2.
    w_m2 = w_verbr_q3 + w_verbr_q4.
    if w_m2 lt w_m1.
      wa_matnr-zztendenz = icon_yellow_light.
    endif.
  endif.
endform.                    "anreichern_itab

*---------------------------------------------------------------------*
*       FORM verbrauch                                                *
*---------------------------------------------------------------------*
form verbrauch.
  clear: w_verbr, w_verbr_q1, w_verbr_q2, w_verbr_q3, w_verbr_q4.
  call function 'VERBRAUCH_LESEN'
    exporting
      abdatum                  = w_datum_von
      bisdatum                 = w_datum_bis
      matnr                    = imarc-matnr
      periv                    = imarc-periv
      werks                    = imarc-werks
      kzgek                    = 'X'
    tables
      ges_verb                 = iverb
      ges_verb_kor             = iverb
      ung_verb                 = iverb
      ung_verb_kor             = iverb
    exceptions
      abdatum_before_range     = 1
      abdatum_greater_bisdatum = 2
      bisdatum_in_future       = 3
      calendar_not_complete    = 4
      consumption_not_found    = 5
      fv_not_found             = 6
      fv_period_error          = 7
      no_material              = 8
      no_plant                 = 9
      others                   = 10.

  loop at iverb.
    w_verbr = w_verbr + iverb-verb1.
    case sy-tabix.
      when '1' or '2' or '3'.
        w_verbr_q1 = w_verbr_q1 + iverb-verb1.
      when '4' or '5' or '6'.
        w_verbr_q2 = w_verbr_q2 + iverb-verb1.
      when '7' or '8' or '9'.
        w_verbr_q3 = w_verbr_q3 + iverb-verb1.
      when '10' or '11' or '12'.
        w_verbr_q4 = w_verbr_q4 + iverb-verb1.
    endcase.
  endloop.
endform.                    "verbrauch

*&---------------------------------------------------------------------*
*&      Form  listausgabe
*&---------------------------------------------------------------------*
form listausgabe.

  gs_variant = sy-repid.

  create object:
    rahmen exporting container_name = 'INHALT',
    inhalt exporting i_parent = rahmen.

  call method inhalt->set_table_for_first_display
    exporting
      i_structure_name = 'ZMM_CHECK_UMTERM'
      is_variant       = gs_variant
      i_save           = 'A'
    changing
      it_outtab        = it_matnr.
* ->Create Object to receive events and link them to handler methods.
* When the ALV Control raises the event for the specified instance
* the corresponding method is automatically called.
*
  create object event_receiver.
  set handler event_receiver->handle_user_command for inhalt.
  set handler event_receiver->handle_toolbar for inhalt.
*
********

* § 4.Call method 'set_toolbar_interactive' to raise event TOOLBAR.
  call method inhalt->set_toolbar_interactive.
  call screen 1100.
endform.                    " listausgabe

*&--------------------------------------------------------------------*
*&      Form  fortschritt
*&--------------------------------------------------------------------*
form fortschritt using text.
  call function 'SAPGUI_PROGRESS_INDICATOR'
    exporting
      percentage = 0
      text       = text.
endform.                    "fortschritt

*&---------------------------------------------------------------------*
*&      Module  pbo1100_aufbereitung  OUTPUT
*&---------------------------------------------------------------------*
module pbo1100_aufbereitung output.
  set titlebar 'TITLE1100'.
  set pf-status 'STATUS1100'.
endmodule.                 " pbo1100_aufbereitung  OUTPUT

*&---------------------------------------------------------------------*
*&      Module  pai1100_okcodesicherung  INPUT
*&---------------------------------------------------------------------*
module pai1100_okcodesicherung input.
  oksave = okcode.
  clear okcode.
endmodule.                 " pai1100_okcodesicherung  INPUT

*&---------------------------------------------------------------------*
*&      Module  pai1100_verarbeitung  INPUT
*&---------------------------------------------------------------------*
module pai1100_verarbeitung input.
  case oksave.
    when 'EXIT'.
      leave program.
    when 'BACK'.
      leave to transaction sy-tcode.
    when 'RW'.
      leave to transaction sy-tcode.
    when 'ANZ'.
  endcase.
endmodule.                 " pai1100_verarbeitung  INPUT

*&---------------------------------------------------------------------*
*&      Form  details_anzeigen
*&---------------------------------------------------------------------*
form details_anzeigen tables p_row structure lvc_s_row.
  data: ls_sel_line like lvc_s_row.

  read table p_row index 1 into ls_sel_line.
  check ls_sel_line-rowtype is initial.

  read table it_matnr index ls_sel_line-index into wa_matnr.
  set parameter id 'MAT' field wa_matnr-matnr."#EC CI_FLDEXT_OK[2215424] during CRR
  call transaction 'MD04' and skip first screen.
endform.                    " details_anzeigen
Exitcode: 0
