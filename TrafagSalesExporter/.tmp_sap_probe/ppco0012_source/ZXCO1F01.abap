*----------------------------------------------------------------------*
***INCLUDE ZXCO1F01 .
*----------------------------------------------------------------------*

*&---------------------------------------------------------------------*
*&      Form  CHECK_STANDORTWECHSEL
*&---------------------------------------------------------------------*
*form check_standortwechsel tables   p_avos structure afvgb
*                           using    p_aufnr
*                                    p_dispo
*                                    p_aufpl.
*  data: begin of l_avo occurs 0,
*          vornr like afvgb-vornr,
*          arbid like afvgb-arbid,
*        end of l_avo.
*
*  refresh l_avo.
*  loop at p_avos where aufpl = p_aufpl.
*    l_avo-vornr = p_avos-vornr.
*    l_avo-arbid = p_avos-arbid.
*    select single count(*) from crhd where objty = 'A'
*                                     and   objid = p_avos-arbid
*                                     and   arbpl like 'OPA%'.
*    if sy-subrc ne 0.            "nicht OPA
*      append l_avo.
*    endif.
*  endloop.
*
*  sort l_avo by vornr.
*
*  select single * from t024d where dispo = p_dispo.
*  if t024d-dsnam cs 'CZ'.
*    w_von = '2'.
*  else.
*    w_von = '1'.
*  endif.
*
*  read table l_avo index 1.         "erster Vorgang.
*  check sy-subrc = 0.
*
*  select single * from crhd where objty = 'A'
*                            and   objid = l_avo-arbid.
*  if crhd-arbpl(2) = 'CZ'.
*    w_nach = '2'.
*  else.
*    w_nach = '1'.
*  endif.
*
*  check: not w_von is initial and
*         not w_nach is initial.
*  check w_von ne w_nach.
*
*  clear zmm_fbas.
*  zmm_fbas-zzweg = w_von.
*  zmm_fbas-aufnr = p_aufnr.
*  modify zmm_fbas.
*
*endform.                    " CHECK_STANDORTWECHSEL
