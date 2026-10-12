*----------------------------------------------------------------------*
***INCLUDE LZZLO1F01 .
*----------------------------------------------------------------------*

*&---------------------------------------------------------------------*
*&      Form  fill_bptab
*&---------------------------------------------------------------------*
form fill_bptab using typcd.
  clear fak_laenge.
  if laenge gt 600.
    while laenge gt 600.
      fak_laenge = fak_laenge + 1.
      laenge = laenge - 100.
    endwhile.
  endif.

  clear fak_laenge2.
  if laenge2 gt 600.
    while laenge2 gt 600.
      fak_laenge2 = fak_laenge2 + 1.
      laenge2 = laenge2 - 100.
    endwhile.
  endif.

  clear key.
  refresh zbtab.
  if typcd+3(1) = ' '.              "3-stellige Typen
    key-zwtyp = typcd(3).
    key-schalter = typcd+4(2).
    key-bereich = typcd+6(2).
    key-fuehler = typcd+9(3).
    key-kdwu = typcd+11(3).
    key-befest = typcd+13(2).
    key-sror = typcd+16(4).
    key-sror+4(4) = typcd+21(4).
    do 10 times varying zbtab-zub from typcd+26(2) next typcd+29(2) range typcd.
      if not zbtab-zub is initial.
        append zbtab.
      endif.
    enddo.
  else.
    key-zwtyp = typcd(4).               "4-stellige Typen
    key-bereich = typcd+5(2).
    key-fuehler = typcd+8(4).
    key-befest = typcd+13(2).
    do 10 times varying zbtab-zub from typcd+26(2) next typcd+29(2) range typcd.
      if not zbtab-zub is initial.
        append zbtab.
      endif.
    enddo.
  endif.

  clear: ztyp_kopf, ztyp_kopf_t.
  select single * from ztyp_kopf where typ = key-zwtyp.
  if mit_loe ne 'X' and
    ztyp_kopf-loekz eq 'X'.
    clear ztyp_kopf.
  endif.
  select single * from ztyp_kopf_t where typ = key-zwtyp
                                   and   spras = zwspras.
  clear zwbptab.
  zwbptab-code = key-zwtyp.
  zwbptab-text = ztyp_kopf_t-text.
  zwbptab-preis = ztyp_kopf-preis.
  zwbptab-waers = ztyp_kopf-waers.
  append zwbptab.

  perform read_table using ztyp_kopf-vw001 key-schalter 0
                           ztyp_kopf-fakt1.
  perform read_table using ztyp_kopf-vw002 key-bereich 0
                           ztyp_kopf-fakt2.
  perform read_table using ztyp_kopf-vw003 key-fuehler 0
                           ztyp_kopf-fakt3.
* perform read_table using ztyp_kopf-vw004 key-kdwu 0 1.
  perform read_table using ztyp_kopf-vw004 key-kdwu 0
                           ztyp_kopf-fakt3.
  perform read_table using ztyp_kopf-vw005 key-befest 0 1.
  perform read_table using ztyp_kopf-vw006 key-sror 0 1.
  loop at zbtab.
    perform read_table using ztyp_kopf-vw007 zbtab-zub sy-tabix 1.
  endloop.
endform.                    " fill_bptab

*&---------------------------------------------------------------------*
*&      Form  read_table
*&---------------------------------------------------------------------*
form read_table using vw  xkey cnt faktor.
  check not vw is initial.
  check not xkey is initial.
  clear zwbptab.

  if cnt ge 4 and
    not mara-matnr is initial and
    xkey = '01'.
    unpack cnt to xcode.
    select single * from ztyp_zub where matnr = mara-matnr
                                  and   code = xcode.
    if sy-subrc = 0.
      zwbptab-code = xkey.
      zwbptab-text = ztyp_zub-text1.
      zwbptab-preis = ztyp_zub-preis.
      zwbptab-waers = ztyp_zub-waers.
      append zwbptab.
      if not ztyp_zub-text2 is initial.
        clear zwbptab.
        zwbptab-text = ztyp_zub-text2.
        append zwbptab.
      endif.
    else.
      zwbptab-code = xkey.
      zwbptab-text = '*** NOT FOUND ***'.
      append zwbptab.
    endif.
  else.
    zwxkey = xkey.
    if zwxkey+4(4) ne space.         "Bei Schutzrohr: BISWERT !!
      zwxkey+4(4) = '%   '.
      clear ztyp_pos.
      select * from ztyp_pos where verw = vw
                             and   code like zwxkey.
        if mit_loe ne 'X'.
          check ztyp_pos-loekz = space.
        endif.
        if ztyp_pos-code+4(4) ge xkey+4(4).
          zwxkey = ztyp_pos-code.
          exit. "#EC CI_NOORDER "Added by NTT
        endif.
      endselect.
    endif.

    select single * from ztyp_pos where verw = vw
                                  and   code = zwxkey.
    if sy-subrc = 0 and
       ( mit_loe eq 'X' or
      ztyp_pos-loekz is initial ).
      sy-subrc = 0.
    else.
      sy-subrc = 4.
    endif.

    if sy-subrc = 0.
      clear ztyp_pos_t.
      select single * from ztyp_pos_t where verw = vw     "#EC CI_ALL_FIELDS_NEEDED
                                      and   code = ztyp_pos-code
                                      and   spras = zwspras.
      zwbptab-code = xkey.
      zwbptab-text = ztyp_pos_t-text.
      zwbptab-preis = ztyp_pos-preis * faktor.
      zwbptab-waers = ztyp_pos-waers.

      if fak_laenge gt 0 and
        not ztyp_pos-zusch is initial.
        zwbptab-preis = zwbptab-preis +
                        ( fak_laenge * ztyp_pos-zusch * faktor ).
        zwbptab-zusch = fak_laenge * ztyp_pos-zusch * faktor.
      endif.

      if fak_laenge2 gt 0 and
        not ztyp_pos-zusch2 is initial.
        zwbptab-preis = zwbptab-preis +
                        ( fak_laenge2 * ztyp_pos-zusch2 * faktor ).
        zwbptab-zusch = fak_laenge2 * ztyp_pos-zusch2 * faktor.
      endif.

      append zwbptab.
    else.
      if not xkey co '0 ' and
         xkey ne key-kdwu.
        zwbptab-code = xkey.
        zwbptab-text = '*** NOT FOUND ***'.
        append zwbptab.
      endif.
    endif.
  endif.

endform.                    " read_table

*&---------------------------------------------------------------------*
*&      Form  read_mat_by_typcd
*&---------------------------------------------------------------------*
form read_mat_by_typcd.

  data: l_search like mara-zztyp_f4.
  l_search = w_search.
  condense l_search no-gaps.

  if l_search+5 eq space.
    select * from mara into corresponding fields of table zwmttab   "#EC CI_NOFIELD
                                          up to w_max rows
                   where zztyp_f4 like l_search
                   and   bismt ne space."#EC CI_NOORDER "Addedby NTT
  else.
    select * from mara into corresponding fields of table zwmttab   "#EC CI_NOFIELD
                                          up to w_max rows
                                          where zztyp_f4 like l_search
      . "#EC CI_NOORDER "Added by NTT
  endif.
endform.                    " read_mat_by_typcd

*&---------------------------------------------------------------------*
*&      Form  read_mat_by_bismt
*&---------------------------------------------------------------------*
form read_mat_by_bismt.
  w_bismt = w_search. "#EC CI_FLDEXT_OK[2215424]  "Addedby NTT
  select * from mara into corresponding fields of table zwmttab
                                        up to w_max rows
                where bismt like w_bismt. "#EC CI_NOORDER "Addedby NTT
  if sy-subrc ne 0.
    w_maktx = w_search.
    select * from marav into corresponding fields of table zwmttab
                          up to w_max rows
                        where maktg like w_maktx
                          and   spras = w_langu
                          and   bismt ne space."#EC CI_NOORDER "Added
  endif.
endform.                    " read_mat_by_bismt

*&---------------------------------------------------------------------*
*&      Form  complete_zmttab
*&---------------------------------------------------------------------*
form complete_zmttab.
  loop at zwmttab.
    if zwmttab-maktx is initial.
      select single maktx from makt
                          into zwmttab-maktx
                          where matnr = zwmttab-matnr
                          and   spras = w_langu.
    endif.
    if zwmttab-zztyp is initial.
      select single zztyp from mara
                          into zwmttab-zztyp
                          where matnr = zwmttab-matnr.
    endif.
    select * from a503 where kappl = 'V '
                       and   kschl = 'Z000'
                       and   vkorg = '1100'
                       and   pltyp = '02'
                       and   matnr = zwmttab-matnr
                       and   datbi ge sy-datum
                       and   datab le sy-datum.
      select * from konp where knumh = a503-knumh   "#EC CI_ALL_FIELDS_NEEDED
                         and   loevm_ko = ' '.
        zwmttab-preis = konp-kbetr / konp-kpein.
        zwmttab-waers = konp-kwaeh.
        exit.
      endselect.
      exit.
    endselect.
    modify zwmttab.
  endloop.
endform.                    " complete_zmttab

*&---------------------------------------------------------------------*
*&      Form  complete_zmttab_cust_spec
*&---------------------------------------------------------------------*
form complete_zmttab_cust_spec.
  loop at zwmttab.
    if zwmttab-bismt is initial or
      zwmttab-bismt(1) = '*'.
      delete zwmttab.
      continue.
    endif.
    clear zwmttab2.
    if zwmttab-maktx is initial.
      select single maktx from makt
                          into zwmttab-maktx
                          where matnr = zwmttab-matnr
                          and   spras = w_langu.
    endif.
    if zwmttab-zztyp is initial.
      select single zztyp from mara
                          into zwmttab-zztyp
                          where matnr = zwmttab-matnr.
    endif.
    move-corresponding zwmttab to zwmttab2.
    select single disgr into zwmttab2-dispg from marc
                        where matnr = zwmttab2-matnr
                        and   werks = w_werks.

    perform material_status using zwmttab2-matnr zwmttab2-status.

    perform preisfindung_cust_spec.
    append zwmttab2.
  endloop.
endform.                    " complete_zmttab2

*---------------------------------------------------------------------*
*       FORM material_status                                          *
*---------------------------------------------------------------------*
form material_status using p_matnr p_status.
  clear w_vmsta.
  select single vmsta into w_vmsta from mvke
                                   where matnr = p_matnr
                                   and   vkorg = w_vkorg
                                   and   vtweg = w_vtweg.
  if w_vmsta is initial.
    select single mstav into w_vmsta from mara
                                     where matnr = p_matnr.
  endif.
  if not w_vmsta is initial.
*   select single spvbc into w_status from tvms
*                       where vmsta = w_vmsta.
*   if w_status = 'B'.               "Bei jedem Status 'X' bringen!!
    p_status = 'X'.
*   endif.
  endif.
endform.                    "material_status

*&---------------------------------------------------------------------*
*&      Form  preisfindung_cust_spec
*&---------------------------------------------------------------------*
form preisfindung_cust_spec.
  clear: zwmttab2-preis,
         zwmttab2-waers,
         zwmttab2-unit.
  w_kschl = 'Z999'.
  do 2 times.
    if zwmttab2-preis is initial.
      select * from a502 where kappl = 'V '
                         and   kschl = w_kschl
                         and   vkorg = w_vkorg
                         and   kunnr = w_kunnr
                         and   matnr = zwmttab2-matnr
                         and   datbi ge sy-datum
                         and   datab le sy-datum.
        perform read_konp using a502-knumh.
        exit.
      endselect.
    endif.
    if zwmttab2-preis is initial.
      select * from a503 where kappl = 'V '
                         and   kschl = w_kschl
                         and   vkorg = w_vkorg
                         and   pltyp = w_pltyp
                         and   matnr = zwmttab2-matnr
                         and   datbi ge sy-datum
                         and   datab le sy-datum.
        perform read_konp using a503-knumh.
        exit.
      endselect.
    endif.
    if zwmttab2-preis is initial.
      select * from a504 where kappl = 'V '
                         and   kschl = w_kschl
                         and   vkorg = w_vkorg
                         and   matnr = zwmttab2-matnr
                         and   datbi ge sy-datum
                         and   datab le sy-datum.
        perform read_konp using a504-knumh.
        exit.
      endselect.
    endif.
    w_kschl = 'Z000'.
  enddo.

  if w_waers ne zwmttab2-waers.
    call function 'CONVERT_TO_FOREIGN_CURRENCY'
      exporting
        date             = sy-datum
        foreign_currency = w_waers
        local_amount     = zwmttab2-preis
        local_currency   = zwmttab2-waers
      importing
        foreign_amount   = zwmttab2-preis
      exceptions
        no_rate_found    = 1
        overflow         = 2
        no_factors_found = 3
        no_spread_found  = 4
        derived_2_times  = 5
        others           = 6.

    if sy-subrc = 0.
      zwmttab2-waers = w_waers.
    endif.
  endif.

endform.                    " preisfindung_cust_spec

*---------------------------------------------------------------------*
*       FORM read_konp                                                *
*---------------------------------------------------------------------*
form read_konp using p_knumh.
  select * from konp where knumh = p_knumh    "#EC CI_ALL_FIELDS_NEEDED
                     and   loevm_ko = ' '.
    zwmttab2-preis = konp-kbetr / konp-kpein.
    zwmttab2-waers = konp-kwaeh.
    zwmttab2-unit = konp-kmein.
    exit.
  endselect.
endform.                    "read_konp

*&---------------------------------------------------------------------*
*&      Form  sales_order_sim
*&---------------------------------------------------------------------*
form sales_order_sim.

  data: l_posnr type p.

  clear sdhead.
  sdhead-doc_type = 'TA'.
  sdhead-sales_org = w_vkorg.
  sdhead-distr_chan = w_vtweg.
  sdhead-division = w_spart.
  sdhead-purch_no = 'WEB'.

  refresh: sditem, sdpart, sditemex, sdcond, sdmess.

  clear sdpart.
  sdpart-partn_role = 'AG'.
  sdpart-partn_numb = w_kunnr.
  append sdpart.

  l_posnr = 0.
  loop at zwmttab4.
    l_posnr = l_posnr + 1.
    clear sditem.
    sditem-itm_number = l_posnr.
    sditem-material = zwmttab4-matnr.
    sditem-req_qty = zwmttab4-meng * 1000.
    append sditem.
  endloop.

  call function 'BAPI_SALESORDER_SIMULATE' "#EC CI_USAGE_OK[2438131] "Add
    exporting
      order_header_in    = sdhead
    tables
      order_items_in     = sditem
      order_partners     = sdpart
      order_items_out    = sditemex
      order_condition_ex = sdcond
      messagetable       = sdmess.

  if not sdmess[] is initial.
    loop at sdmess.
      move-corresponding sdmess to ret1.
      append ret1.
    endloop.
    exit.
  endif.

  loop at sditemex.
    read table zwmttab4 index sditemex-itm_number.
    zwmttab4-netpreis = sditemex-net_value / zwmttab4-meng / 100.
    zwmttab4-waers = sditemex-currency.
*    loop at sdcond where itm_number = sditemex-itm_number
*                   and   cond_type = 'Z000'.
*      zwmttab4-preis = sdcond-cond_value.
*    endloop.
*    if zwmttab4-preis is initial.
*      loop at sdcond where itm_number = sditemex-itm_number
*                     and   cond_type = 'Z999'.
*        zwmttab4-preis = sdcond-cond_value.
*      endloop.
*    endif.
    modify zwmttab4 index sditemex-itm_number.
  endloop.
endform.                    " sales_order_sim

*&---------------------------------------------------------------------*
*&      Form  fill_vltab
*&---------------------------------------------------------------------*
form fill_vltab using  p_line.
  case p_line.
    when 0.
      perform hilfe_typ.
    when 1.
      if ztyp_kopf-typ+3(1) is initial.
        perform hilfe_ssbe.
      else.
        perform hilfe using ztyp_kopf-vw002.
      endif.
    when 2.
      perform hilfe using ztyp_kopf-vw003.
    when 3.
      perform hilfe using ztyp_kopf-vw005.
    when 4.
      perform hilfe using ztyp_kopf-vw006.
    when others.
      perform hilfe using ztyp_kopf-vw007.
  endcase.
endform.                    " fill_vltab

*&---------------------------------------------------------------------*
*&      Form  hilfe_typ
*&---------------------------------------------------------------------*
form hilfe_typ.
  select * from ztyp_kopf_t where spras = sy-langu.
    select count(*) from ztyp_kopf where typ = ztyp_kopf_t-typ
                                   and   loekz = space.
    if sy-subrc = 0.
      zwvltab-code = ztyp_kopf_t-typ.
      zwvltab-text = ztyp_kopf_t-text.
      append zwvltab.
    endif.
  endselect.
endform.                    "hilfe_typ

*&---------------------------------------------------------------------*
*&      Form  hilfe
*&---------------------------------------------------------------------*
form hilfe using    p_vw.
  select code text
    from  ztyp_pos_t
    into  corresponding fields of table zwvltab
    where verw = p_vw
    and   spras = w_langu.

  loop at zwvltab.
    select count(*) from ztyp_pos where verw = p_vw
                                  and   code = zwvltab-code
                                  and   loekz = space.
    if sy-subrc ne 0.
      delete zwvltab.
    endif.
  endloop.

endform.                    " hilfe

*&---------------------------------------------------------------------*
*&      Form  hilfe_ssbe
*&---------------------------------------------------------------------*
form hilfe_ssbe.
  if w_value is initial.
    select * from ztyp_pos_t
      where verw = ztyp_kopf-vw001
      and   spras = w_langu.
      select count(*) from ztyp_pos where verw = ztyp_pos_t-verw
                                    and   code = ztyp_pos_t-code
                                    and   loekz = space.
      if sy-subrc = 0.
        zwvltab-code  = ztyp_pos_t-code.
        zwvltab-text = ztyp_pos_t-text.
        append zwvltab.
      endif.
    endselect.
  else.
    select * from ztyp_pos_t
      where verw = ztyp_kopf-vw002
      and   spras = w_langu.
      select count(*) from ztyp_pos where verw = ztyp_pos_t-verw
                                    and   code = ztyp_pos_t-code
                                    and   loekz = space.
      if sy-subrc = 0.
        zwvltab-code  = w_value.
        zwvltab-code+2(2) = ztyp_pos_t-code.
        zwvltab-text = ztyp_pos_t-text.
        append zwvltab.
      endif.
    endselect.
  endif.

endform.                    " hilfe_ssbe

*----------------------------------------------------------------------*
*        Start new screen                                              *
*----------------------------------------------------------------------*
form bdc_dynpro using program dynpro.
  clear bdcdata.
  bdcdata-program  = program.
  bdcdata-dynpro   = dynpro.
  bdcdata-dynbegin = 'X'.
  append bdcdata.
endform.                    "BDC_DYNPRO

*----------------------------------------------------------------------*
*        Insert field                                                  *
*----------------------------------------------------------------------*
form bdc_field using fnam fval.
  clear bdcdata.
  bdcdata-fnam = fnam.
  bdcdata-fval = fval.
  append bdcdata.
endform.                    "BDC_FIELD
