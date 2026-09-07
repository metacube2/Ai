

*&---------------------------------------------------------------------*
*& Report  Z_ABGLEICH_KTSCH
*&---------------------------------------------------------------------*
*& Arbeitspläne - Abgleich Textvorlagen / Vorgabewerte über KTSCH
*&
*& Historie:
*&   20.04.2026  A.Lahrach   Deaktiviert wegen Fehlfunktion (alt)
*&   28.04.2026  I.Kohler    Neuaufbau (PLNAL-Fix, ALV)
*&   29.04.2026  I.Kohler    Umbau auf CL_GUI_ALV_GRID + Splitter
*&   30.04.2026  I.Kohler    Variante B:
*&                           - Editor zeigt KTSCH-Vorlage (CA10)
*&                           - editierbar, geht an alle markierten
*&                           - kein pro-Zeile-Tracking mehr
*&---------------------------------------------------------------------*
report z_abgleich_ktsch.

*-Tabellen-------------------------------------------------------------*
tables: plpo, mara, zzpp_vc_vorgabe.

*-Typen----------------------------------------------------------------*
types:
  begin of ty_row,
    guid       type sysuuid_x16,
    sel        type c length 1,
    matnr      type mara-matnr,
    maktx      type makt-maktx,
    zztyp      type mara-zztyp,
    plnty      type plpo-plnty,
    plnnr      type plpo-plnnr,
    plnal      type mapl-plnal,
    plnkn      type plpo-plnkn,
    zaehl      type plpo-zaehl,
    vornr      type plpo-vornr,
    arbpl      type crhd-arbpl,
    vge01      type plpo-vge01,
    vgw01      type plpo-vgw01,
    vge02      type plpo-vge02,
    vgw02      type plpo-vgw02,
    vge03      type plpo-vge03,
    vgw03      type plpo-vgw03,
    vge04      type plpo-vge04,
    vgw04      type plpo-vgw04,
    mstae      type mara-mstae,
    mstav      type mara-mstav,


text_diff  type c length 1,
    ltxa1      type plpo-ltxa1,
    ktsch_txt  type c length 132,
    vorg_txt   type c length 132,
  end of ty_row.

types:
  begin of ty_freq,
    vge01 type plpo-vge01, vgw01 type plpo-vgw01,
    vge02 type plpo-vge02, vgw02 type plpo-vgw02,
    vge03 type plpo-vge03, vgw03 type plpo-vgw03,
    vge04 type plpo-vge04, vgw04 type plpo-vgw04,
    count type i,
  end of ty_freq.

types:
  begin of ty_plpo,
    plnty type plpo-plnty,
    plnnr type plpo-plnnr,
    plnkn type plpo-plnkn,
    zaehl type plpo-zaehl,
    vornr type plpo-vornr,
    arbid type plpo-arbid,
    vge01 type plpo-vge01, vgw01 type plpo-vgw01,
    vge02 type plpo-vge02, vgw02 type plpo-vgw02,
    vge03 type plpo-vge03, vgw03 type plpo-vgw03,
    vge04 type plpo-vge04, vgw04 type plpo-vgw04,
    ltxa1 type plpo-ltxa1,
  end of ty_plpo.

types:
  begin of ty_plas,
    plnty type plas-plnty,
    plnnr type plas-plnnr,
    plnkn type plas-plnkn,
    zaehl type plas-zaehl,
    plnal type plas-plnal,
  end of ty_plas.

types:
  begin of ty_mapl,
    matnr type mapl-matnr,
    plnnr type mapl-plnnr,
    plnal type mapl-plnal,
  end of ty_mapl.

types:
  begin of ty_mara_s,
    matnr type mara-matnr,
    zztyp type mara-zztyp,
    mstae type mara-mstae,
    mstav type mara-mstav,
  end of ty_mara_s.

types:
  begin of ty_makt_s,
    matnr type makt-matnr,
    maktx type makt-maktx,
  end of ty_makt_s.

types:
  begin of ty_crhd_s,
    objid type crhd-objid,
    arbpl type crhd-arbpl,
  end of ty_crhd_s.

*-Daten----------------------------------------------------------------*
data:
  gv_chg01 type c length 1,
  gv_chg02 type c length 1,
  gv_chg03 type c length 1,
  gv_chg04 type c length 1.

data:
  gv_trace_file type string,
  gv_trace_open type c length 1.

data:


  gt_err_log type standard table of string,
  gv_ok_cnt  type i,
  gv_err_cnt type i,
  gt_rows  type standard table of ty_row    with default key,
  gt_freq  type standard table of ty_freq   with default key,
  gs_row   type ty_row,

  gt_plpo  type standard table of ty_plpo   with default key,
  gt_plas  type standard table of ty_plas   with default key,
  gt_mapl  type standard table of ty_mapl   with default key,
  gt_mara  type standard table of ty_mara_s with default key,
  gt_makt  type standard table of ty_makt_s with default key,
  gt_crhd  type standard table of ty_crhd_s with default key,

  gs_plpo  type ty_plpo,
  gs_plas  type ty_plas,
  gs_mapl  type ty_mapl,
  gs_mara  type ty_mara_s,
  gs_makt  type ty_makt_s,
  gs_crhd  type ty_crhd_s,

  gt_ttab_o type standard table of tline,
  gt_ttab_v type standard table of tline,

  gv_xopen type c length 1,
  gv_tcnt  type i.

*-ALV------------------------------------------------------------------*
data:
  go_split     type ref to cl_gui_splitter_container,
  go_top       type ref to cl_gui_container,
  go_bot       type ref to cl_gui_container,
  go_grid      type ref to cl_gui_alv_grid,
  go_textedit  type ref to cl_gui_textedit,
  gs_layout    type lvc_s_layo,
  gt_fcat      type lvc_t_fcat,
  gs_variant   type disvariant,
  gv_exit      type c length 1.

data:
  bdcdata like bdcdata occurs 0 with header line.

*-Texteditor (Buffer für BDC) -----------------------------------------*
data:
  gt_edit  type standard table of tline.

*-Dialog 0900 ---------------------------------------------------------*
data:
  vge01_alt type plpo-vge01, vgw01_alt type plpo-vgw01,
  vge02_alt type plpo-vge02, vgw02_alt type plpo-vgw02,
  vge03_alt type plpo-vge03, vgw03_alt type plpo-vgw03,
  vge04_alt type plpo-vge04, vgw04_alt type plpo-vgw04,
  vge01_neu type plpo-vge01, vgw01_neu type plpo-vgw01,
  vge02_neu type plpo-vge02, vgw02_neu type plpo-vgw02,
  vge03_neu type plpo-vge03, vgw03_neu type plpo-vgw03,
  vge04_neu type plpo-vge04, vgw04_neu type plpo-vgw04,
  gv_xfirst type c length 1,
  gv_xwarn  type c length 1,
  gv_okc900 type sy-ucomm.

*-Selektion------------------------------------------------------------*
selection-screen begin of block b1 with frame title text-b01.
parameters:
  p_ktsch type plpo-ktsch obligatory,
  p_werks type t001w-werks obligatory default '1100',
  p_plnty type plpo-plnty default 'N',
  p_debug as checkbox default ' '.
selection-screen end of block b1.

selection-screen begin of block b2 with frame title text-b02.
select-options:
  s_zztyp for mara-zztyp.
selection-screen end of block b2.



*&---------------------------------------------------------------------*
*&  Klasse für ALV-Events
*&---------------------------------------------------------------------*
class lcl_alv_handler definition.
  public section.
    methods:
      handle_toolbar
        for event toolbar of cl_gui_alv_grid
        importing e_object e_interactive,
      handle_user_command
        for event user_command of cl_gui_alv_grid
        importing e_ucomm.
endclass.

class lcl_alv_handler implementation.

  method handle_toolbar.
    data: ls_button type stb_button.

*   Trenner
    clear ls_button.
    ls_button-butn_type = 3.
    append ls_button to e_object->mt_toolbar.

*   Texte abgleichen
    clear ls_button.
    ls_button-function  = 'SAVE_TX'.
    ls_button-icon      = '@04@'.
    ls_button-quickinfo = 'Markierte Texte gegen KTSCH-Vorlage abgleichen'.
    ls_button-text      = 'Texte abgleichen'.
    ls_button-butn_type = 0.
    append ls_button to e_object->mt_toolbar.

*   Zeiten abgleichen
    clear ls_button.
    ls_button-function  = 'SAVE_ZT'.
    ls_button-icon      = '@AH@'.
    ls_button-quickinfo = 'Markierte Vorgabewerte abgleichen'.
    ls_button-text      = 'Zeiten abgleichen'.
    ls_button-butn_type = 0.
    append ls_button to e_object->mt_toolbar.

*   Trenner
    clear ls_button.
    ls_button-butn_type = 3.
    append ls_button to e_object->mt_toolbar.

*   Alle markieren
    clear ls_button.
    ls_button-function  = 'MALL'.
    ls_button-icon      = '@5B@'.
    ls_button-quickinfo = 'Alle Zeilen markieren'.
    ls_button-text      = 'Alle markieren'.
    ls_button-butn_type = 0.
    append ls_button to e_object->mt_toolbar.

*   Markierung aufheben
    clear ls_button.
    ls_button-function  = 'NONE'.
    ls_button-icon      = '@5C@'.
    ls_button-quickinfo = 'Alle Markierungen entfernen'.
    ls_button-text      = 'Markierung aufheben'.
    ls_button-butn_type = 0.
    append ls_button to e_object->mt_toolbar.

  endmethod.

  method handle_user_command.
    if go_grid is bound.
      call method go_grid->check_changed_data.
    endif.
    case e_ucomm.
      when 'SAVE_TX'.  perform sichern_texte.
      when 'SAVE_ZT'.  perform sichern_zeiten.
      when 'MALL'.     perform mark_all using abap_true.
      when 'NONE'.     perform mark_all using abap_false.
    endcase.
    if go_grid is bound.
      call method go_grid->refresh_table_display.
    endif.
  endmethod.

endclass.

data: go_handler type ref to lcl_alv_handler.

*&---------------------------------------------------------------------*
*&  START-OF-SELECTION
*&---------------------------------------------------------------------*
start-of-selection.


  perform trace_open.

  perform read_ktsch_template.
  perform select_data.

  if gt_rows is initial.
    perform trace using `Keine Vorgänge gefunden`.
    perform trace_close.
    message s368(00) with 'Keine Vorgänge zu' p_ktsch 'gefunden'.
    return.
  endif.

  call screen 100.

  perform trace_close.
*&---------------------------------------------------------------------*
*&  Form  read_ktsch_template
*&---------------------------------------------------------------------*
form read_ktsch_template.

  data: ls_thead type thead.

  refresh gt_ttab_o.
  ls_thead-tdname   = p_ktsch.
  ls_thead-tdobject = 'WORKST'.
  ls_thead-tdid     = 'SUBM'.

  call function 'READ_TEXT'
    exporting
      id                      = ls_thead-tdid
      language                = sy-langu
      name                    = ls_thead-tdname
      object                  = ls_thead-tdobject
    tables
      lines                   = gt_ttab_o
    exceptions
      not_found               = 4
      others                  = 8.

  if sy-subrc <> 0 and sy-subrc <> 4.
    message i368(00) with 'Fehler beim Lesen Vorlagentext' p_ktsch.
  endif.

endform.

*&---------------------------------------------------------------------*
*&  Form  select_data
*&---------------------------------------------------------------------*
form select_data.

  data: ls_oline type tline.

  select plnty plnnr plnkn zaehl vornr arbid
         vge01 vgw01 vge02 vgw02 vge03 vgw03 vge04 vgw04 ltxa1
    from plpo
    into table gt_plpo
    where ktsch = p_ktsch
      and plnty = p_plnty
      and loekz = space.

  if gt_plpo is initial.
    return.
  endif.

  select plnty plnnr plnkn zaehl plnal
    from plas
    into table gt_plas
    for all entries in gt_plpo
    where plnty = gt_plpo-plnty
      and plnnr = gt_plpo-plnnr
      and plnkn = gt_plpo-plnkn
      and zaehl = gt_plpo-zaehl
      and loekz = space.

  if gt_plas is initial.
    return.
  endif.

  select matnr plnnr plnal
    from mapl
    into table gt_mapl
    for all entries in gt_plas
    where plnnr = gt_plas-plnnr
      and plnal = gt_plas-plnal
      and werks = p_werks
      and loekz = space.

  if gt_mapl is initial.
    return.
  endif.


*
*  select matnr zztyp mstae mstav
*    from mara
*    into table gt_mara
*    for all entries in gt_mapl
*    where matnr = gt_mapl-matnr
*      and zztyp in s_zztyp.
*
*  if gt_mara is initial.
*    return.
*  endif.
*
  select mara~matnr mara~zztyp mara~mstae mara~mstav
    from mara
      inner join marc on marc~matnr = mara~matnr
    into table gt_mara
    for all entries in gt_mapl
    where mara~matnr = gt_mapl-matnr
      and mara~zztyp in s_zztyp
      and mara~mstae <> '99'
      and mara~mstav <> '99'
      and marc~werks = p_werks
      and marc~mmsta <> '99'.

  if gt_mara is initial.
    return.
  endif.


  select matnr maktx
    from makt
    into table gt_makt
    for all entries in gt_mara
    where matnr = gt_mara-matnr
      and spras = sy-langu.

  select objid arbpl
    from crhd
    into table gt_crhd
    for all entries in gt_plpo
    where objty = 'A'
      and objid = gt_plpo-arbid.

  loop at gt_plpo into gs_plpo.

    clear gs_crhd.
    read table gt_crhd into gs_crhd
      with key objid = gs_plpo-arbid.

    loop at gt_plas into gs_plas
      where plnty = gs_plpo-plnty
        and plnnr = gs_plpo-plnnr
        and plnkn = gs_plpo-plnkn
        and zaehl = gs_plpo-zaehl.

      loop at gt_mapl into gs_mapl
        where plnnr = gs_plas-plnnr
          and plnal = gs_plas-plnal.

        clear gs_mara.
        read table gt_mara into gs_mara
          with key matnr = gs_mapl-matnr.
        if sy-subrc <> 0.
          continue.
        endif.

        clear gs_makt.
        read table gt_makt into gs_makt
          with key matnr = gs_mapl-matnr.

        clear gs_row.
        gs_row-matnr = gs_mapl-matnr.
        gs_row-maktx = gs_makt-maktx.
        gs_row-zztyp = gs_mara-zztyp.
        gs_row-plnty = gs_plpo-plnty.
        gs_row-plnnr = gs_plpo-plnnr.
        gs_row-plnal = gs_mapl-plnal.
        gs_row-plnkn = gs_plpo-plnkn.
        gs_row-zaehl = gs_plpo-zaehl.
        gs_row-vornr = gs_plpo-vornr.
        gs_row-arbpl = gs_crhd-arbpl.
        gs_row-vge01 = gs_plpo-vge01.
        gs_row-vgw01 = gs_plpo-vgw01.
        gs_row-vge02 = gs_plpo-vge02.
        gs_row-vgw02 = gs_plpo-vgw02.
        gs_row-vge03 = gs_plpo-vge03.
        gs_row-vgw03 = gs_plpo-vgw03.
        gs_row-vge04 = gs_plpo-vge04.
        gs_row-vgw04 = gs_plpo-vgw04.
        gs_row-mstae = gs_mara-mstae.
        gs_row-mstav = gs_mara-mstav.
        gs_row-ltxa1 = gs_plpo-ltxa1.

*       Vorlagentext zusammenbauen (alle Zeilen mit Leerzeichen)
        clear gs_row-ktsch_txt.
        loop at gt_ttab_o into ls_oline.
          if gs_row-ktsch_txt is initial.
            gs_row-ktsch_txt = ls_oline-tdline.
          else.
            concatenate gs_row-ktsch_txt ls_oline-tdline
              into gs_row-ktsch_txt separated by space.
          endif.
        endloop.

        try.
            gs_row-guid = cl_system_uuid=>create_uuid_x16_static( ).
          catch cx_uuid_error.
        endtry.

       perform read_vorgang_text using gs_plpo
                                  changing gs_row-text_diff
                                           gs_row-vorg_txt.

        append gs_row to gt_rows.

        perform count_freq using gs_plpo.

      endloop.
    endloop.
  endloop.

endform.

*&---------------------------------------------------------------------*
*&  Form  read_vorgang_text
*&---------------------------------------------------------------------*
form read_vorgang_text
  using    is_plpo type ty_plpo
  changing cv_diff  type c
           cv_vorg  type c.

  data: ls_thead type thead,
        ls_o     type tline,
        ls_v     type tline,
        ls_line  type tline,
        lv_subrc type sy-subrc.

  clear cv_diff.
  clear cv_vorg.
  refresh gt_ttab_v.




  ls_thead-tdname        = sy-mandt.
  ls_thead-tdname+3(1)   = is_plpo-plnty.
  ls_thead-tdname+4(8)   = is_plpo-plnnr.
  ls_thead-tdname+12(8)  = is_plpo-plnkn.
  ls_thead-tdname+20(8)  = is_plpo-zaehl.
  ls_thead-tdobject      = 'ROUTING'.
  ls_thead-tdid          = 'PLPO'.

  call function 'READ_TEXT'
    exporting
      id                      = ls_thead-tdid
      language                = sy-langu
      name                    = ls_thead-tdname
      object                  = ls_thead-tdobject
    tables
      lines                   = gt_ttab_v
    exceptions
      not_found               = 4
      others                  = 8.
  lv_subrc = sy-subrc.

* Falls kein Vorgangs-Langtext: LTXA1 als Pseudo-Zeile
  if lv_subrc <> 0.
    refresh gt_ttab_v.
    read table gt_ttab_o into ls_o index 1.
    ls_v-tdformat = ls_o-tdformat.
    ls_v-tdline   = is_plpo-ltxa1.
    append ls_v to gt_ttab_v.
  endif.

  if gt_ttab_v <> gt_ttab_o.
    cv_diff = 'X'.
  endif.

  if gt_ttab_v <> gt_ttab_o.
    cv_diff = 'X'.
  endif.

* Vorgangstext zusammenbauen für Anzeige in ALV
  loop at gt_ttab_v into ls_line.
    if cv_vorg is initial.
      cv_vorg = ls_line-tdline.
    else.
      concatenate cv_vorg ls_line-tdline
        into cv_vorg separated by space.
    endif.
  endloop.

endform.



*&---------------------------------------------------------------------*
*&  Form  count_freq
*&---------------------------------------------------------------------*
form count_freq using is_plpo type ty_plpo.

  data: ls_freq type ty_freq.
  field-symbols: <freq> type ty_freq.

  read table gt_freq assigning <freq>
    with key vge01 = is_plpo-vge01 vgw01 = is_plpo-vgw01
             vge02 = is_plpo-vge02 vgw02 = is_plpo-vgw02
             vge03 = is_plpo-vge03 vgw03 = is_plpo-vgw03
             vge04 = is_plpo-vge04 vgw04 = is_plpo-vgw04.
  if sy-subrc = 0.
    <freq>-count = <freq>-count + 1.
  else.
    ls_freq-vge01 = is_plpo-vge01. ls_freq-vgw01 = is_plpo-vgw01.
    ls_freq-vge02 = is_plpo-vge02. ls_freq-vgw02 = is_plpo-vgw02.
    ls_freq-vge03 = is_plpo-vge03. ls_freq-vgw03 = is_plpo-vgw03.
    ls_freq-vge04 = is_plpo-vge04. ls_freq-vgw04 = is_plpo-vgw04.
    ls_freq-count = 1.
    append ls_freq to gt_freq.
  endif.

endform.

*&---------------------------------------------------------------------*
*&  Form  build_fcat
*&---------------------------------------------------------------------*
form build_fcat.

  data: ls_fcat type lvc_s_fcat.

  refresh gt_fcat.

* Sel - editierbar als Checkbox
  clear ls_fcat.
  ls_fcat-fieldname = 'SEL'.
  ls_fcat-coltext   = 'Sel'.
  ls_fcat-scrtext_s = 'Sel'.
  ls_fcat-scrtext_m = 'Sel'.
  ls_fcat-scrtext_l = 'Markierung'.
  ls_fcat-checkbox  = 'X'.
  ls_fcat-edit      = 'X'.
  ls_fcat-outputlen = 3.
  append ls_fcat to gt_fcat.

  define add_col.
    clear ls_fcat.
    ls_fcat-fieldname = &1.
    ls_fcat-coltext   = &2.
    ls_fcat-scrtext_s = &2.
    ls_fcat-scrtext_m = &2.
    ls_fcat-scrtext_l = &2.
    append ls_fcat to gt_fcat.
  end-of-definition.

* MATNR ohne führende Nullen
  clear ls_fcat.
  ls_fcat-fieldname = 'MATNR'.
  ls_fcat-coltext   = 'Material'.
  ls_fcat-scrtext_s = 'Material'.
  ls_fcat-scrtext_m = 'Material'.
  ls_fcat-scrtext_l = 'Material'.
  ls_fcat-no_zero   = 'X'.
  ls_fcat-lzero     = ' '.
  append ls_fcat to gt_fcat.

  add_col 'MAKTX'     'Materialkurztext'.
  add_col 'ZZTYP'     'Typencode'.
  add_col 'PLNTY'     'Plantyp'.
  add_col 'PLNNR'     'Plangruppe'.
  add_col 'PLNAL'     'PlGrZähler'.
  add_col 'VORNR'     'Vorgang'.
  add_col 'ARBPL'     'ArbPlatz'.
  add_col 'VGE01'     'VoWrtEinh.'.
  add_col 'VGW01'     'Vorgabe'.
  add_col 'VGE02'     'VoWrtEinh.'.
  add_col 'VGW02'     'Vorgabe'.
  add_col 'VGE03'     'VoWrtEinh.'.
  add_col 'VGW03'     'Vorgabe'.
  add_col 'VGE04'     'VoWrtEinh.'.
  add_col 'VGW04'     'Vorgabe'.
  add_col 'TEXT_DIFF' 'Text'.
  add_col 'MSTAE'     'XStatus'.
  add_col 'MSTAV'     'VStatus'.
clear ls_fcat.
  ls_fcat-fieldname = 'VORG_TXT'.
  ls_fcat-coltext   = 'Vorgangstext (ist)'.
  ls_fcat-scrtext_s = 'Vorgangstxt'.
  ls_fcat-scrtext_m = 'Vorgangstext'.
  ls_fcat-scrtext_l = 'Vorgangstext (ist)'.
  ls_fcat-outputlen = 60.
  append ls_fcat to gt_fcat.


  clear ls_fcat.
  ls_fcat-fieldname = 'VORG_TXT'.
  ls_fcat-coltext   = 'Vorgangstext (ist)'.
  ls_fcat-scrtext_s = 'Vorgangstxt'.
  ls_fcat-scrtext_m = 'Vorgangstext'.
  ls_fcat-scrtext_l = 'Vorgangstext (ist)'.
  ls_fcat-outputlen = 60.
  append ls_fcat to gt_fcat.

* KTSCH-Vorlagentext (SOLL) - sichtbar (vorher ausgeblendet, jetzt an)
  clear ls_fcat.
  ls_fcat-fieldname = 'KTSCH_TXT'.
  ls_fcat-coltext   = 'Vorlage (soll)'.
  ls_fcat-scrtext_s = 'Vorlage'.
  ls_fcat-scrtext_m = 'Vorlage soll'.
  ls_fcat-scrtext_l = 'KTSCH-Vorlage (soll)'.
  ls_fcat-outputlen = 60.
  append ls_fcat to gt_fcat.
* KTSCH-Vorlagentext - default ausgeblendet, im Spaltenvorrat einblendbar
  clear ls_fcat.
  ls_fcat-fieldname = 'KTSCH_TXT'.
  ls_fcat-coltext   = 'Text zu Vorlageschlüssel'.
  ls_fcat-scrtext_s = 'Vorlagentxt'.
  ls_fcat-scrtext_m = 'Text zu Vorlage'.
  ls_fcat-scrtext_l = 'Text zu Vorlageschlüssel'.
  ls_fcat-no_out    = 'X'.
  ls_fcat-outputlen = 60.
  append ls_fcat to gt_fcat.

* Technische Spalten ausblenden
  loop at gt_fcat into ls_fcat where fieldname = 'GUID'
                                  or fieldname = 'PLNKN'
                                  or fieldname = 'ZAEHL'
                                  or fieldname = 'LTXA1'.
    ls_fcat-no_out = 'X'.
    ls_fcat-tech   = 'X'.
    modify gt_fcat from ls_fcat.
  endloop.

endform.

*&---------------------------------------------------------------------*
*&  Module  status_0100  OUTPUT
*&---------------------------------------------------------------------*
module status_0100 output.
  set pf-status 'STATUS_0100'.
  set titlebar  'TITLE_0100'.

  if go_split is initial.
    perform create_alv.
  endif.
endmodule.

*&---------------------------------------------------------------------*
*&  Form  create_alv
*&---------------------------------------------------------------------*
form create_alv.

  data: lt_lines type standard table of char256,
        ls_line  type char256,
        ls_t     type tline.

  perform build_fcat.

* Splitter direkt im Bildschirm (screen0)
  create object go_split
    exporting
      parent  = cl_gui_container=>screen0
      rows    = 2
      columns = 1
    exceptions
      others  = 1.
  if sy-subrc <> 0.
    message i368(00) with 'Splitter Fehler'.
    return.
  endif.

* Aufteilung 70 / 30
  call method go_split->set_row_height
    exporting id = 1 height = 70.
  call method go_split->set_row_height
    exporting id = 2 height = 30.

  go_top = go_split->get_container( row = 1 column = 1 ).
  go_bot = go_split->get_container( row = 2 column = 1 ).

* ALV oben
  create object go_grid
    exporting
      i_parent = go_top
    exceptions
      others   = 1.
  if sy-subrc <> 0.
    message i368(00) with 'ALV-Grid Fehler'.
    return.
  endif.

  gs_layout-cwidth_opt = 'X'.
  gs_layout-zebra      = 'X'.
  gs_layout-sel_mode   = 'A'.

  gs_variant-report    = sy-repid.

  create object go_handler.
  set handler go_handler->handle_toolbar      for go_grid.
  set handler go_handler->handle_user_command for go_grid.

  call method go_grid->set_table_for_first_display
    exporting
      is_layout                     = gs_layout
      is_variant                    = gs_variant
      i_save                        = 'A'
    changing
      it_outtab                     = gt_rows
      it_fieldcatalog               = gt_fcat
    exceptions
      others                        = 1.

  call method go_grid->set_toolbar_interactive.
  call method go_grid->set_ready_for_input
    exporting
      i_ready_for_input = 1.

* TextEdit unten
  create object go_textedit
    exporting
      parent        = go_bot
      wordwrap_mode = cl_gui_textedit=>wordwrap_at_windowborder
    exceptions
      others        = 1.
  if sy-subrc <> 0.
    message i368(00) with 'TextEdit Fehler'.
    return.
  endif.

* Editor mit KTSCH-Vorlage vorbelegen
  refresh lt_lines.
  loop at gt_ttab_o into ls_t.
    ls_line = ls_t-tdline.
    append ls_line to lt_lines.
  endloop.
  call method go_textedit->set_text_as_stream
    exporting text = lt_lines.

endform.

*&---------------------------------------------------------------------*
*&  Module  user_command_0100  INPUT
*&---------------------------------------------------------------------*
module user_command_0100 input.
  case sy-ucomm.
    when 'BACK' or 'EXIT' or 'CANC'.
      gv_exit = 'X'.
      leave to screen 0.
  endcase.
endmodule.

*&---------------------------------------------------------------------*
*&  Form  mark_all
*&---------------------------------------------------------------------*
form mark_all using iv_flag type abap_bool.
  data: lv_flag type c length 1.
  field-symbols: <r> type ty_row.

  if iv_flag = abap_true.
    lv_flag = 'X'.
  else.
    clear lv_flag.
  endif.
  loop at gt_rows assigning <r>.
    <r>-sel = lv_flag.
  endloop.
endform.


*&---------------------------------------------------------------------*
*&  Form  sichern_texte
*&---------------------------------------------------------------------*
form sichern_texte.

  data: lv_xmatnr     type c length 18,
        lv_xplnal     type c length 2,
        lv_xvornr     type c length 4,
        lv_fname      type c length 30,
        lv_zcnt       type i,
        lv_count      type i,
        lv_marked     type i,
        lv_idx        type i,
        lv_perc       type i,
        lv_mtxt       type c length 18,
        lv_ptext      type c length 70,
        lv_line       type string,
        ls_t          type tline,
        ls_o          type tline,
        lt_lines      type standard table of char256,
        ls_line       type char256,
        lv_first      type c length 1,
        lv_msg        type c length 100,
        lv_cok        type c length 10,
        lv_cer        type c length 10,
        lv_trace_line type string.

  field-symbols: <r> type ty_row.

* ----------------------------------------------------------------*
* Markierung prüfen
* ----------------------------------------------------------------*
  loop at gt_rows assigning <r> where sel = 'X'.
    lv_marked = lv_marked + 1.
  endloop.
  if lv_marked = 0.
    message s368(00) with 'Keine Zeilen markiert'.
    return.
  endif.

* ----------------------------------------------------------------*
* Editor-Inhalt holen und in gt_edit umwandeln
* ----------------------------------------------------------------*
  if go_textedit is bound.
    call method go_textedit->get_text_as_stream
      importing text = lt_lines.
  endif.

  refresh gt_edit.
  read table gt_ttab_o into ls_o index 1.
  lv_first = 'X'.
  loop at lt_lines into ls_line.
    clear ls_t.
    if lv_first = 'X'.
      ls_t-tdformat = ls_o-tdformat.
      if ls_t-tdformat is initial.
        ls_t-tdformat = '*'.
      endif.
      clear lv_first.
    else.
      ls_t-tdformat = '/'.
    endif.
    ls_t-tdline = ls_line.
    append ls_t to gt_edit.
  endloop.

  if gt_edit is initial.
    message s368(00) with 'Editor ist leer'.
    return.
  endif.

* ----------------------------------------------------------------*
* BDC-Zähler & Fehlerlog initialisieren
* ----------------------------------------------------------------*
  clear: gv_ok_cnt, gv_err_cnt, gv_tcnt.
  refresh gt_err_log.

  perform trace using `### START sichern_texte ###`.
  lv_trace_line = |Markierte Zeilen: { lv_marked }|.
  perform trace using lv_trace_line.

* ----------------------------------------------------------------*
* BDC für jede markierte Zeile mit demselben Text
* ----------------------------------------------------------------*
  loop at gt_rows assigning <r> where sel = 'X'.

    lv_idx = lv_idx + 1.
    lv_perc = ( lv_idx * 100 ) / lv_marked.

    write <r>-matnr to lv_mtxt no-zero.
    condense lv_mtxt.

*   -------------------------------------------------------------*
*   Materialstatus 99 überspringen
*   -------------------------------------------------------------*
    if <r>-mstae = '99' or <r>-mstav = '99'.
      gv_err_cnt = gv_err_cnt + 1.
      concatenate 'MATNR' lv_mtxt 'Vorgang' <r>-vornr
                  'übersprungen - MStatus X='
                  <r>-mstae 'V=' <r>-mstav
             into lv_line separated by space.
      append lv_line to gt_err_log.
      perform trace using lv_line.
      continue.
    endif.

*   -------------------------------------------------------------*
*   Progressbar
*   -------------------------------------------------------------*
    if lv_idx = 1 or lv_idx mod 25 = 0 or lv_idx = lv_marked.
      concatenate 'Texte abgleichen: MATNR' lv_mtxt
                  '/ Vorgang' <r>-vornr
             into lv_ptext separated by space.
      call function 'SAPGUI_PROGRESS_INDICATOR'
        exporting
          percentage = lv_perc
          text       = lv_ptext.
    endif.

    write <r>-matnr to lv_xmatnr.
    write <r>-plnal to lv_xplnal.
    write <r>-vornr to lv_xvornr.

    lv_trace_line = |=== MATNR { lv_mtxt } PLNAL { <r>-plnal } VORNR { <r>-vornr } ===|.
    perform trace using lv_trace_line.

*   -------------------------------------------------------------*
*   BDC-Sequenz CA02 - Langtext Vorgang
*   -------------------------------------------------------------*
    perform bdc_dynpro using 'SAPLCPDI' '1010'.
    perform bdc_field  using 'BDC_OKCODE'   '=VOUE'.
    perform bdc_field  using 'RC27M-MATNR'  lv_xmatnr.
    perform bdc_field  using 'RC27M-WERKS'  p_werks.
    perform bdc_field  using 'RC271-PLNAL'  lv_xplnal.

    perform bdc_dynpro using 'SAPLCPDI' '1400'.
    perform bdc_field  using 'BDC_OKCODE'   '=OSEA'.

    perform bdc_dynpro using 'SAPLCP02' '1010'.
    perform bdc_field  using 'BDC_OKCODE'   '=ENT1'.
    perform bdc_field  using 'RC27H-VORNR'  lv_xvornr.

    perform bdc_dynpro using 'SAPLCPDI' '1400'.
    perform bdc_field  using 'BDC_OKCODE'        '=LTXT'.
    perform bdc_field  using 'RC27X-FLG_SEL(01)' 'X'.

    perform bdc_dynpro using 'SAPLSTXX' '1100'.
    perform bdc_field  using 'BDC_OKCODE'   '=TXDE'.

    perform bdc_dynpro using 'SAPLSPO1' '0100'.
    perform bdc_field  using 'BDC_OKCODE'   '=YES'.

*   --- Texteditor-Dynpro: ZUERST Zeilen, DANN =TXBA ---
    perform bdc_dynpro using 'SAPLSTXX' '1100'.

    lv_zcnt = 1.
    loop at gt_edit into ls_t.
      lv_zcnt = lv_zcnt + 1.
      lv_fname = 'RSTXT-TXPARGRAPH(..)'.
      unpack lv_zcnt to lv_fname+17(2).
      perform bdc_field using lv_fname ls_t-tdformat.
      lv_fname = 'RSTXT-TXLINE(..)'.
      unpack lv_zcnt to lv_fname+13(2).
      perform bdc_field using lv_fname ls_t-tdline.
    endloop.

*   JETZT erst =TXBA (Sichern Text & zurück)
    perform bdc_field using 'BDC_OKCODE' '=TXBA'.

    perform bdc_dynpro using 'SAPLCPDI' '1400'.
    perform bdc_field  using 'BDC_OKCODE' '=BU'.

    perform bdc_transaction using 'CA02'.
    lv_count = lv_count + 1.
  endloop.

* ----------------------------------------------------------------*
* Zusammenfassung
* ----------------------------------------------------------------*
  lv_cok = gv_ok_cnt.
  lv_cer = gv_err_cnt.
  condense lv_cok. condense lv_cer.

  concatenate 'Texte verarbeitet: OK=' lv_cok ' Fehler/Skip=' lv_cer
         into lv_msg separated by space.
  message lv_msg type 'S'.

  lv_trace_line = |### ENDE sichern_texte OK={ gv_ok_cnt } ERR={ gv_err_cnt } ###|.
  perform trace using lv_trace_line.

  if gv_err_cnt > 0.
    perform show_err_log.
  endif.

endform.
*&---------------------------------------------------------------------*
*&  Form  show_err_log
*&---------------------------------------------------------------------*
form show_err_log.

  data: ls_err type string.

  write: / 'Fehler-Protokoll:',
         / sy-uline(80).

  loop at gt_err_log into ls_err.
    write: / ls_err.
  endloop.

endform.






*&---------------------------------------------------------------------*
*&  Form  popup_get_zeiten
*&  Werte aus oberster markierter ALV-Zeile vorfüllen.
*&  Alle vier Änderungs-Checkboxen sind initial auf X.
*&  User kann im Popup einzelne Haken entfernen.
*&---------------------------------------------------------------------*
form popup_get_zeiten changing cv_ok type c.

  data: lt_fields type standard table of sval,
        ls_field  type sval,
        lv_rc     type c length 1,
        ls_top    type ty_freq,
        lv_info   type c length 200,
        lv_w1     type c length 14,
        lv_w2     type c length 14,
        lv_w3     type c length 14,
        lv_w4     type c length 14,
        lv_t1     type c length 30,
        lv_t2     type c length 30,
        lv_t3     type c length 30,
        lv_t4     type c length 30,
        lv_found  type c length 1.

  field-symbols: <m> type ty_row.

  clear cv_ok.

* ----------------------------------------------------------------*
* ALT-Werte, häufigster Vorgabewert-Satz - nur für Info
* ----------------------------------------------------------------*
  sort gt_freq by count descending.
  read table gt_freq into ls_top index 1.

  if sy-subrc = 0.
    vgw01_alt = ls_top-vgw01.
    vge01_alt = ls_top-vge01.
    vgw02_alt = ls_top-vgw02.
    vge02_alt = ls_top-vge02.
    vgw03_alt = ls_top-vgw03.
    vge03_alt = ls_top-vge03.
    vgw04_alt = ls_top-vgw04.
    vge04_alt = ls_top-vge04.

    write vgw01_alt to lv_w1 left-justified.
    write vgw02_alt to lv_w2 left-justified.
    write vgw03_alt to lv_w3 left-justified.
    write vgw04_alt to lv_w4 left-justified.

    concatenate 'IST häufig:'
                lv_w1 vge01_alt '/'
                lv_w2 vge02_alt '/'
                lv_w3 vge03_alt '/'
                lv_w4 vge04_alt
           into lv_info separated by space.

    message lv_info type 'S'.
  endif.

* ----------------------------------------------------------------*
* Labels fix setzen
* ----------------------------------------------------------------*
  lv_t1 = 'Rüsten Masch'.
  lv_t2 = 'Maschine'.
  lv_t3 = 'Person'.
  lv_t4 = 'Rüsten Person'.

* ----------------------------------------------------------------*
* Werte initialisieren
* ----------------------------------------------------------------*
  clear: vge01_neu, vgw01_neu,
         vge02_neu, vgw02_neu,
         vge03_neu, vgw03_neu,
         vge04_neu, vgw04_neu,
         gv_chg01, gv_chg02, gv_chg03, gv_chg04.

* ----------------------------------------------------------------*
* Oberste markierte Zeile suchen.
* Reihenfolge ist aktuelle interne GT_ROWS-Reihenfolge.
* ----------------------------------------------------------------*
  loop at gt_rows assigning <m> where sel = 'X'.

    vgw01_neu = <m>-vgw01.
    vge01_neu = <m>-vge01.

    vgw02_neu = <m>-vgw02.
    vge02_neu = <m>-vge02.

    vgw03_neu = <m>-vgw03.
    vge03_neu = <m>-vge03.

    vgw04_neu = <m>-vgw04.
    vge04_neu = <m>-vge04.

    lv_found = 'X'.
    exit.

  endloop.

  if lv_found <> 'X'.
    message s368(00) with 'Keine markierte Zeile für Vorgabewerte gefunden'.
    return.
  endif.

* ----------------------------------------------------------------*
* Alle vier Änderungs-Haken vorbelegen.
* User kann im Popup Haken entfernen.
* ----------------------------------------------------------------*
  gv_chg01 = 'X'.
  gv_chg02 = 'X'.
  gv_chg03 = 'X'.
  gv_chg04 = 'X'.

* ----------------------------------------------------------------*
* Felder fürs Popup
* Reihenfolge pro Block: [Checkbox] [Wert] [Einheit]
* Checkbox-Felder kommen aus DDIC-Struktur ZSKTSCH_TIME_PO
* FIELD_ATTR bleibt leer, sonst readonly/protected.
* ----------------------------------------------------------------*
  define _add.
    clear ls_field.
    ls_field-tabname    = &1.
    ls_field-fieldname  = &2.
    ls_field-fieldtext  = &3.
    ls_field-field_attr = &4.
    ls_field-value      = &5.
    append ls_field to lt_fields.
  end-of-definition.

* Block 1: Rüstzeit Maschine
  _add 'ZSKTSCH_TIME_PO' 'CHG01' lv_t1     ''  gv_chg01.
  _add 'PLPO'            'VGW01' 'Wert'    ''  vgw01_neu.
  _add 'PLPO'            'VGE01' 'Einheit' ''  vge01_neu.

* Block 2: Maschine
  _add 'ZSKTSCH_TIME_PO' 'CHG02' lv_t2     ''  gv_chg02.
  _add 'PLPO'            'VGW02' 'Wert'    ''  vgw02_neu.
  _add 'PLPO'            'VGE02' 'Einheit' ''  vge02_neu.

* Block 3: Person
  _add 'ZSKTSCH_TIME_PO' 'CHG03' lv_t3     ''  gv_chg03.
  _add 'PLPO'            'VGW03' 'Wert'    ''  vgw03_neu.
  _add 'PLPO'            'VGE03' 'Einheit' ''  vge03_neu.

* Block 4: Rüstzeit Person
  _add 'ZSKTSCH_TIME_PO' 'CHG04' lv_t4     ''  gv_chg04.
  _add 'PLPO'            'VGW04' 'Wert'    ''  vgw04_neu.
  _add 'PLPO'            'VGE04' 'Einheit' ''  vge04_neu.

  call function 'POPUP_GET_VALUES'
    exporting
      popup_title     = 'Vorgabewerte ändern - Werte aus markierter Zeile'
      start_column    = 30
      start_row       = 5
    importing
      returncode      = lv_rc
    tables
      fields          = lt_fields
    exceptions
      error_in_fields = 1
      others          = 2.

  if sy-subrc <> 0.
    message s368(00) with 'POPUP_GET_VALUES Fehler in Felddefinition, SUBRC=' sy-subrc.
    return.
  endif.

  if lv_rc = 'A'.
    return.
  endif.

* ----------------------------------------------------------------*
* Werte zurücklesen
* Reihenfolge: Checkbox, Wert, Einheit je Block
* ----------------------------------------------------------------*
  read table lt_fields into ls_field index 1.
  gv_chg01 = ls_field-value.
  read table lt_fields into ls_field index 2.
  vgw01_neu = ls_field-value.
  read table lt_fields into ls_field index 3.
  vge01_neu = ls_field-value.

  read table lt_fields into ls_field index 4.
  gv_chg02 = ls_field-value.
  read table lt_fields into ls_field index 5.
  vgw02_neu = ls_field-value.
  read table lt_fields into ls_field index 6.
  vge02_neu = ls_field-value.

  read table lt_fields into ls_field index 7.
  gv_chg03 = ls_field-value.
  read table lt_fields into ls_field index 8.
  vgw03_neu = ls_field-value.
  read table lt_fields into ls_field index 9.
  vge03_neu = ls_field-value.

  read table lt_fields into ls_field index 10.
  gv_chg04 = ls_field-value.
  read table lt_fields into ls_field index 11.
  vgw04_neu = ls_field-value.
  read table lt_fields into ls_field index 12.
  vge04_neu = ls_field-value.

* ----------------------------------------------------------------*
* Checkbox-Werte normalisieren
* ----------------------------------------------------------------*
  translate gv_chg01 to upper case.
  translate gv_chg02 to upper case.
  translate gv_chg03 to upper case.
  translate gv_chg04 to upper case.

  if gv_chg01 <> 'X'.
    clear gv_chg01.
  endif.
  if gv_chg02 <> 'X'.
    clear gv_chg02.
  endif.
  if gv_chg03 <> 'X'.
    clear gv_chg03.
  endif.
  if gv_chg04 <> 'X'.
    clear gv_chg04.
  endif.

  cv_ok = 'X'.

endform.



*&---------------------------------------------------------------------*
*&  Form  sichern_zeiten
*&  Selektiver Abgleich: nur angehakte Zeiten werden geändert
*&---------------------------------------------------------------------*
form sichern_zeiten.

  data: lv_xmatnr     type c length 18,
        lv_xplnal     type c length 2,
        lv_xvornr     type c length 4,
        lv_xvgw       type c length 14,
        lv_xvge       type c length 3,
        lv_xsttag     type c length 10,
        lv_subscr     type c length 80,
        lv_count      type i,
        lv_marked     type i,
        lv_ok         type c length 1,
        lv_msg        type c length 100,
        lv_cok        type c length 10,
        lv_cer        type c length 10,
        lv_idx        type i,
        lv_perc       type i,
        lv_mtxt       type c length 18,
        lv_ptext      type c length 70,
        lv_line       type string,
        lv_trace_line type string,
        lv_chg_list   type string.

  field-symbols: <r> type ty_row.

  loop at gt_rows assigning <r> where sel = 'X'.
    lv_marked = lv_marked + 1.
  endloop.
  if lv_marked = 0.
    message s368(00) with 'Keine Zeilen markiert'.
    return.
  endif.

  perform popup_get_zeiten changing lv_ok.
  if lv_ok <> 'X'.
    return.
  endif.

* ----------------------------------------------------------------*
* Mindestens ein Haken
* ----------------------------------------------------------------*
  if gv_chg01 <> 'X' and gv_chg02 <> 'X' and
     gv_chg03 <> 'X' and gv_chg04 <> 'X'.
    message s368(00) with 'Keine Zeit zum Ändern markiert'.
    return.
  endif.

* ----------------------------------------------------------------*
* Konsistenz pro angehaktem Paar: Wert UND Einheit gefüllt
* ----------------------------------------------------------------*
  if ( gv_chg01 = 'X' and ( vgw01_neu is initial or vge01_neu is initial ) ) or
     ( gv_chg02 = 'X' and ( vgw02_neu is initial or vge02_neu is initial ) ) or
     ( gv_chg03 = 'X' and ( vgw03_neu is initial or vge03_neu is initial ) ) or
     ( gv_chg04 = 'X' and ( vgw04_neu is initial or vge04_neu is initial ) ).
    message s368(00) with 'Angehaktes Paar: Wert UND Einheit erforderlich'.
    return.
  endif.

* ----------------------------------------------------------------*
* Übersicht was geändert wird
* ----------------------------------------------------------------*
  clear lv_chg_list.
  if gv_chg01 = 'X'. concatenate lv_chg_list 'VGW01 ' into lv_chg_list. endif.
  if gv_chg02 = 'X'. concatenate lv_chg_list 'VGW02 ' into lv_chg_list. endif.
  if gv_chg03 = 'X'. concatenate lv_chg_list 'VGW03 ' into lv_chg_list. endif.
  if gv_chg04 = 'X'. concatenate lv_chg_list 'VGW04 ' into lv_chg_list. endif.

* ----------------------------------------------------------------*
* Subscreen-String für Vorgabewerte einmal aufbauen
* ----------------------------------------------------------------*
  clear lv_subscr.
  lv_subscr       = 'SAPLCPDO'.
  lv_subscr+40(4) = '1211'.
  lv_subscr+44    = 'DEFAULTVAL'.

* Stichtag formatieren
  write sy-datum to lv_xsttag dd/mm/yyyy.

* ----------------------------------------------------------------*
* BDC-Zähler & Fehlerlog initialisieren
* ----------------------------------------------------------------*
  clear: gv_ok_cnt, gv_err_cnt, gv_tcnt.
  refresh gt_err_log.

  perform trace using `### START sichern_zeiten ###`.
  lv_trace_line = |Markierte Zeilen: { lv_marked }|.
  perform trace using lv_trace_line.
  lv_trace_line = |Wird geändert: { lv_chg_list }|.
  perform trace using lv_trace_line.
  lv_trace_line = |SOLL CHG01={ gv_chg01 } VGW01={ vgw01_neu } VGE01={ vge01_neu } | &&
                  |CHG02={ gv_chg02 } VGW02={ vgw02_neu } VGE02={ vge02_neu } | &&
                  |CHG03={ gv_chg03 } VGW03={ vgw03_neu } VGE03={ vge03_neu } | &&
                  |CHG04={ gv_chg04 } VGW04={ vgw04_neu } VGE04={ vge04_neu }|.
  perform trace using lv_trace_line.

  loop at gt_rows assigning <r> where sel = 'X'.

    lv_idx = lv_idx + 1.
    lv_perc = ( lv_idx * 100 ) / lv_marked.

    write <r>-matnr to lv_mtxt no-zero.
    condense lv_mtxt.

*   -------------------------------------------------------------*
*   Materialstatus 99 überspringen
*   -------------------------------------------------------------*
    if <r>-mstae = '99' or <r>-mstav = '99'.
      gv_err_cnt = gv_err_cnt + 1.
      concatenate 'MATNR' lv_mtxt 'Vorgang' <r>-vornr
                  'übersprungen - MStatus X='
                  <r>-mstae 'V=' <r>-mstav
             into lv_line separated by space.
      append lv_line to gt_err_log.
      perform trace using lv_line.
      continue.
    endif.

    concatenate 'Zeiten abgleichen: MATNR' lv_mtxt
                '/ Vorgang' <r>-vornr
           into lv_ptext separated by space.

    call function 'SAPGUI_PROGRESS_INDICATOR'
      exporting
        percentage = lv_perc
        text       = lv_ptext.

    write <r>-matnr to lv_xmatnr.
    write <r>-plnal to lv_xplnal.
    write <r>-vornr to lv_xvornr.

    lv_trace_line = |=== MATNR { lv_mtxt } PLNAL { <r>-plnal } VORNR { <r>-vornr } ===|.
    perform trace using lv_trace_line.

*   -------------------------------------------------------------*
*   BDC-Sequenz CA02 - Vorgabewerte
*   -------------------------------------------------------------*

*   --- Einstiegsbild ---
    perform bdc_dynpro using 'SAPLCPDI' '1010'.
    perform bdc_field  using 'BDC_OKCODE'  '=VOUE'.
    perform bdc_field  using 'RC27M-MATNR' lv_xmatnr.
    perform bdc_field  using 'RC27M-WERKS' p_werks.
    perform bdc_field  using 'RC271-STTAG' lv_xsttag.
    perform bdc_field  using 'RC271-PLNAL' lv_xplnal.

*   --- Vorgangsübersicht: über =OSEA zum Vorgang ---
    perform bdc_dynpro using 'SAPLCPDI' '1400'.
    perform bdc_field  using 'BDC_OKCODE'  '=OSEA'.

    perform bdc_dynpro using 'SAPLCP02' '1010'.
    perform bdc_field  using 'BDC_OKCODE'  '=ENT1'.
    perform bdc_field  using 'RC27H-VORNR' lv_xvornr.

*   --- Vorgangsübersicht: PICK auf gefundene Zeile 1 ---
    perform bdc_dynpro using 'SAPLCPDI' '1400'.
    perform bdc_field  using 'BDC_OKCODE'      '=PICK'.
    perform bdc_field  using 'RC27X-ENTRY_ACT' '1'.

*   --- Vorgangs-Detail: Subscreen + nur angehakte Felder ---
    perform bdc_dynpro using 'SAPLCPDO' '1200'.
    perform bdc_field  using 'BDC_SUBSCR' lv_subscr.

    if gv_chg01 = 'X'.
      perform fmt_quan using vgw01_neu changing lv_xvgw.
      perform bdc_field using 'PLPOD-VGW01' lv_xvgw.
      lv_xvge = vge01_neu.
      perform bdc_field using 'PLPOD-VGE01' lv_xvge.
    endif.

    if gv_chg02 = 'X'.
      perform fmt_quan using vgw02_neu changing lv_xvgw.
      perform bdc_field using 'PLPOD-VGW02' lv_xvgw.
      lv_xvge = vge02_neu.
      perform bdc_field using 'PLPOD-VGE02' lv_xvge.
    endif.

    if gv_chg03 = 'X'.
      perform fmt_quan using vgw03_neu changing lv_xvgw.
      perform bdc_field using 'PLPOD-VGW03' lv_xvgw.
      lv_xvge = vge03_neu.
      perform bdc_field using 'PLPOD-VGE03' lv_xvge.
    endif.

    if gv_chg04 = 'X'.
      perform fmt_quan using vgw04_neu changing lv_xvgw.
      perform bdc_field using 'PLPOD-VGW04' lv_xvgw.
      lv_xvge = vge04_neu.
      perform bdc_field using 'PLPOD-VGE04' lv_xvge.
    endif.

*   Sichern
    perform bdc_field using 'BDC_OKCODE' '=BU'.

    perform bdc_transaction using 'CA02'.
    lv_count = lv_count + 1.
  endloop.

* ----------------------------------------------------------------*
* Zusammenfassung
* ----------------------------------------------------------------*
  lv_cok = gv_ok_cnt.
  lv_cer = gv_err_cnt.
  condense lv_cok. condense lv_cer.

  concatenate 'Zeiten verarbeitet: OK=' lv_cok ' Fehler/Skip=' lv_cer
              ' Geändert:' lv_chg_list
         into lv_msg separated by space.
  message lv_msg type 'S'.

  lv_trace_line = |### ENDE sichern_zeiten OK={ gv_ok_cnt } ERR={ gv_err_cnt } ###|.
  perform trace using lv_trace_line.

  if gv_err_cnt > 0.
    perform show_err_log.
  endif.

endform.


*&---------------------------------------------------------------------*
*&  Form  fmt_quan
*&  Konvertiert QUAN (9,3) ohne Tausendertrenner, mit Punkt,
*&  in BDC-taugliches Char-Format
*&---------------------------------------------------------------------*
form fmt_quan
  using    iv_val type plpo-vgw01
  changing cv_str type c.

  data: lv_len  type i,
        lv_pos  type i,
        lv_last type c length 1.

  clear cv_str.
  cv_str = iv_val.
  condense cv_str no-gaps.

* Trailing-Nullen nach Dezimalpunkt entfernen
  if cv_str ca '.'.

    lv_len = strlen( cv_str ).
    do.
      if lv_len <= 0.
        exit.
      endif.
      lv_pos = lv_len - 1.
      lv_last = cv_str+lv_pos(1).
      if lv_last <> '0'.
        exit.
      endif.
      lv_len = lv_pos.
    enddo.
    cv_str = cv_str(lv_len).

*   abschliessenden Punkt killen
    lv_len = strlen( cv_str ).
    if lv_len > 0.
      lv_pos = lv_len - 1.
      lv_last = cv_str+lv_pos(1).
      if lv_last = '.'.
        cv_str = cv_str(lv_pos).
      endif.
    endif.

  endif.

endform.

*&---------------------------------------------------------------------*
*&  Module  STATUS_0900  OUTPUT
*&---------------------------------------------------------------------*
module status_0900 output.

  data: ls_top  type ty_freq,
        ls_vorg type zzpp_vc_vorgabe.

  set pf-status 'TIME'.
  set titlebar  'TIME'.

  if gv_xfirst = 'X'.
    clear gv_xfirst.
    sort gt_freq by count descending.
    read table gt_freq into ls_top index 1.
    if sy-subrc = 0.
      vgw01_alt = ls_top-vgw01. vge01_alt = ls_top-vge01.
      vgw02_alt = ls_top-vgw02. vge02_alt = ls_top-vge02.
      vgw03_alt = ls_top-vgw03. vge03_alt = ls_top-vge03.
      vgw04_alt = ls_top-vgw04. vge04_alt = ls_top-vge04.
    endif.
    clear: vge01_neu, vgw01_neu, vge02_neu, vgw02_neu,
           vge03_neu, vgw03_neu, vge04_neu, vgw04_neu.

    select single * from zzpp_vc_vorgabe into ls_vorg
      where ktsch = p_ktsch.
    if sy-subrc = 0.
      vgw01_neu = ls_vorg-vgw01. vge01_neu = ls_vorg-vge01.
      vgw02_neu = ls_vorg-vgw02. vge02_neu = ls_vorg-vge02.
      vgw03_neu = ls_vorg-vgw03. vge03_neu = ls_vorg-vge03.
      vgw04_neu = ls_vorg-vgw04. vge04_neu = ls_vorg-vge04.
    endif.
  endif.
endmodule.

*&---------------------------------------------------------------------*
*&  Module  zeiten  INPUT
*&---------------------------------------------------------------------*
module zeiten input.


  data: lv_dbg type c length 60.

  concatenate 'DBG ucomm=' sy-ucomm
              ' okc='     gv_okc900
              ' warn='    gv_xwarn
         into lv_dbg.
  message lv_dbg type 'S'.

  if gv_okc900 <> 'SICH'.
    exit.
  endif.

  if ( vgw01_neu is not initial and vge01_neu is initial ) or
     ( vgw01_neu is initial     and vge01_neu is not initial ) or
     ( vgw02_neu is not initial and vge02_neu is initial ) or
     ( vgw02_neu is initial     and vge02_neu is not initial ) or
     ( vgw03_neu is not initial and vge03_neu is initial ) or
     ( vgw03_neu is initial     and vge03_neu is not initial ) or
     ( vgw04_neu is not initial and vge04_neu is initial ) or
     ( vgw04_neu is initial     and vge04_neu is not initial ).
    message s368(00) with 'Vorgabewert und Einheit müssen zusammen gefüllt werden'.
    clear gv_okc900.
    exit.
  endif.

  if gv_xwarn is initial.
    gv_xwarn = 'X'.
    message s368(00) with 'Bitte Eingaben prüfen, dann nochmals Sichern'.
    clear gv_okc900.
    exit.
  endif.

  set screen 0.
  leave screen.

endmodule.

*&---------------------------------------------------------------------*
*&  Module  user_command_0900  INPUT
*&---------------------------------------------------------------------*
module user_command_0900 input.
  gv_okc900 = sy-ucomm.
  case sy-ucomm.
    when 'ABBR' or 'CANC' or 'BACK'.
      gv_okc900 = 'ABBR'.
      set screen 0. leave screen.
  endcase.
endmodule.

*&---------------------------------------------------------------------*
*&  Form  trace_open
*&---------------------------------------------------------------------*
form trace_open.

  data: lv_ts   type c length 14,
        lv_user type c length 12,
        lv_line type string.

  if gv_trace_open = 'X'.
    return.
  endif.

* Nur im Debug-Modus tracen
  if p_debug <> 'X'.
    return.
  endif.

  lv_user = sy-uname.
  write sy-datum to lv_ts+0(8).
  write sy-uzeit to lv_ts+8(6).
  condense lv_ts no-gaps.

  gv_trace_file = |/tmp/z_abgleich_ktsch_{ lv_user }_{ lv_ts }.log|.

  open dataset gv_trace_file for output in text mode encoding default.
  if sy-subrc <> 0.
    message i368(00) with 'Trace-Datei konnte nicht geöffnet werden'
                          gv_trace_file.
    clear gv_trace_file.
    return.
  endif.

  gv_trace_open = 'X'.

  lv_line = |=== Z_ABGLEICH_KTSCH Trace { sy-datum } { sy-uzeit } | &&
            |User { sy-uname } KTSCH { p_ktsch } Werk { p_werks } ===|.
  transfer lv_line to gv_trace_file.

endform.














*&---------------------------------------------------------------------*
*&  Form  trace
*&---------------------------------------------------------------------*
form trace using iv_text type string.

  if gv_trace_open <> 'X'.
    return.
  endif.

  transfer iv_text to gv_trace_file.

endform.


*&---------------------------------------------------------------------*
*&  Form  trace_close
*&---------------------------------------------------------------------*
form trace_close.

  if gv_trace_open <> 'X'.
    return.
  endif.

  close dataset gv_trace_file.
  clear gv_trace_open.

  message s368(00) with 'Trace geschrieben:' gv_trace_file.

endform.
*&---------------------------------------------------------------------*
*&  Form  bdc_transaction
*&---------------------------------------------------------------------*
form bdc_transaction using tcode.

  data: lt_msg  type standard table of bdcmsgcoll,
        ls_msg  type bdcmsgcoll,
        ls_opt  type ctu_params,
        lv_txt  type c length 200,
        lv_line type string,
        ls_bdc  like line of bdcdata.

  ls_opt-dismode  = 'N'.
  ls_opt-updmode  = 'L'.
  ls_opt-defsize  = 'X'.
  ls_opt-racommit = 'X'.

  call transaction tcode
    using   bdcdata
    options from ls_opt
    messages into lt_msg.

* ----------------------------------------------------------------*
* Trace: BDC-Sequenz + alle Messages + subrc
* ----------------------------------------------------------------*
  perform trace using `--- BDCDATA ---`.
  loop at bdcdata into ls_bdc.
    if ls_bdc-dynbegin = 'X'.
      lv_line = |  >> DYNPRO { ls_bdc-program } { ls_bdc-dynpro }|.
    else.
      lv_line = |     FIELD { ls_bdc-fnam } = { ls_bdc-fval }|.
    endif.
    perform trace using lv_line.
  endloop.

  perform trace using `--- MESSAGES ---`.
  loop at lt_msg into ls_msg.
    call function 'FORMAT_MESSAGE'
      exporting
        id   = ls_msg-msgid
        lang = sy-langu
        no   = ls_msg-msgnr
        v1   = ls_msg-msgv1
        v2   = ls_msg-msgv2
        v3   = ls_msg-msgv3
        v4   = ls_msg-msgv4
      importing
        msg  = lv_txt.
    lv_line = |  [{ ls_msg-msgtyp } { ls_msg-msgid } { ls_msg-msgnr }] { lv_txt }|.
    perform trace using lv_line.
  endloop.

  lv_line = |  call transaction subrc = { sy-subrc }|.
  perform trace using lv_line.
  perform trace using ``.


* Zählung & Fehler-Sammlung
* Nur echte E/A/X-Meldungen gelten als Fehler.
* CALL TRANSACTION sy-subrc allein ist bei BDC zu ungenau.
* ----------------------------------------------------------------*
  data: lv_has_error type c length 1.

  clear lv_has_error.

  loop at lt_msg into ls_msg
    where msgtyp = 'E'
       or msgtyp = 'A'
       or msgtyp = 'X'.

    lv_has_error = 'X'.

    clear lv_txt.
    call function 'FORMAT_MESSAGE'
      exporting
        id   = ls_msg-msgid
        lang = sy-langu
        no   = ls_msg-msgnr
        v1   = ls_msg-msgv1
        v2   = ls_msg-msgv2
        v3   = ls_msg-msgv3
        v4   = ls_msg-msgv4
      importing
        msg  = lv_txt.

    append lv_txt to gt_err_log.
  endloop.

  if lv_has_error = 'X'.
    gv_err_cnt = gv_err_cnt + 1.
  else.
    gv_ok_cnt = gv_ok_cnt + 1.
  endif.

  refresh bdcdata.
  gv_tcnt = gv_tcnt + 1.

endform.
*&---------------------------------------------------------------------*
*&  Form  run_debug
*&  Läuft ohne ALV durch, schreibt komplettes Trace-Log
*&---------------------------------------------------------------------*
form run_debug.

  data: lv_ok    type c length 1,
        lv_idx   type i,
        lv_total type i,
        ls_err   type string,
        lv_line  type string.

  field-symbols: <r> type ty_row.

* Alle Zeilen markieren
  loop at gt_rows assigning <r>.
    <r>-sel = 'X'.
  endloop.

  lv_total = lines( gt_rows ).
  write: / 'Debug-Lauf für KTSCH', p_ktsch.
  write: / 'Plantyp ', p_plnty, ' Werk ', p_werks.
  write: / 'Anzahl Zeilen:', lv_total.
  write: / sy-uline(80).

*------------------------------------------------------------------*
* Vorgabewerte aus zzpp_vc_vorgabe holen (kein Popup im Debug)
*------------------------------------------------------------------*
  data: ls_vorg type zzpp_vc_vorgabe.
  select single * from zzpp_vc_vorgabe into ls_vorg
    where ktsch = p_ktsch.
  if sy-subrc <> 0.
    write: / 'ABBRUCH: Keine Vorlage in zzpp_vc_vorgabe für', p_ktsch.
    return.
  endif.

  vgw01_neu = ls_vorg-vgw01. vge01_neu = ls_vorg-vge01.
  vgw02_neu = ls_vorg-vgw02. vge02_neu = ls_vorg-vge02.
  vgw03_neu = ls_vorg-vgw03. vge03_neu = ls_vorg-vge03.
  vgw04_neu = ls_vorg-vgw04. vge04_neu = ls_vorg-vge04.

  write: / 'Vorgabe SOLL:'.
  write: /  '  VGW01=', vgw01_neu, ' VGE01=', vge01_neu.
  write: /  '  VGW02=', vgw02_neu, ' VGE02=', vge02_neu.
  write: /  '  VGW03=', vgw03_neu, ' VGE03=', vge03_neu.
  write: /  '  VGW04=', vgw04_neu, ' VGE04=', vge04_neu.
  write: / sy-uline(80).

*------------------------------------------------------------------*
* TEXTE
*------------------------------------------------------------------*
  write: / 'TEXTE ABGLEICHEN'.
  clear: gv_ok_cnt, gv_err_cnt, gv_tcnt.
  refresh gt_err_log.

  perform sichern_texte.

  write: / 'Texte OK=', gv_ok_cnt, ' Fehler=', gv_err_cnt.
  write: / '--- TEXT-LOG ---'.
  loop at gt_err_log into ls_err.
    write: / ls_err.
  endloop.
  write: / sy-uline(80).

*------------------------------------------------------------------*
* ZEITEN
*------------------------------------------------------------------*
  write: / 'ZEITEN ABGLEICHEN'.
  clear: gv_ok_cnt, gv_err_cnt, gv_tcnt.
  refresh gt_err_log.

* sichern_zeiten ruft popup_get_zeiten — im Debug überspringen wir das,
* indem wir direkt die BDC-Schleife duplizieren oder einen Flag setzen.
* Saubere Lösung: popup_get_zeiten kennt Debug-Mode (siehe unten)
  perform sichern_zeiten.

  write: / 'Zeiten OK=', gv_ok_cnt, ' Fehler=', gv_err_cnt.
  write: / '--- ZEITEN-LOG ---'.
  loop at gt_err_log into ls_err.
    write: / ls_err.
  endloop.

endform.

form bdc_dynpro using program dynpro.
  clear bdcdata.
  bdcdata-program  = program.
  bdcdata-dynpro   = dynpro.
  bdcdata-dynbegin = 'X'.
  append bdcdata.
endform.

form bdc_field using fnam fval.
  clear bdcdata.
  bdcdata-fnam = fnam.
  bdcdata-fval = fval.
  append bdcdata.
endform.
