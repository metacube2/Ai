*&---------------------------------------------------------------------*
*& Report  ZPP_RM_MENGEN
*&
*&---------------------------------------------------------------------*
*&
*&
*&---------------------------------------------------------------------*
report  zpp_rm_mengen.

*-Tabellen-------------------------------------------------------------*
tables: mara, crhd.

*-Selektionen----------------------------------------------------------*
selection-screen begin of block b1 with frame.
select-options: s_arbpl for crhd-arbpl obligatory memory id agr matchcode object cram.
parameters: p_werks like afru-werks obligatory memory id wrk.
selection-screen end of block b1.
selection-screen begin of block b2 with frame.
select-options: s_ersda for sy-datum obligatory default sy-datum to sy-datum,
                s_erzet for sy-uzeit.
selection-screen skip 1.
select-options: s_typcd for mara-zztyp.
selection-screen end of block b2.

*-Ranges---------------------------------------------------------------*
ranges: r_arbid for afru-arbid.

*-Int. Tabellen--------------------------------------------------------*
data: iafru like afru occurs 0 with header line.
data: iafvc like afvc occurs 0 with header line.

*-ALV------------------------------------------------------------------*
type-pools: slis.
data: t_ausg type zpp_rm_mengen occurs 0 with header line.
data: it_fieldcat type slis_t_fieldcat_alv.
data: wa_fieldcat type slis_fieldcat_alv.

*-Diverses-------------------------------------------------------------*
data: w_title type lvc_title.

*----------------------------------------------------------------------*
* Verarbeitung                                                         *
*----------------------------------------------------------------------*
start-of-selection.
  perform bilden_range.
  perform lesen_afru.
  perform ausgeben_liste.

*&---------------------------------------------------------------------*
*&      Form  BILDEN_RANGE
*&---------------------------------------------------------------------*
form bilden_range .
  refresh r_arbid.
  clear r_arbid.
  select objid into r_arbid-low from crhd where werks = p_werks
                                          and   arbpl in s_arbpl.
    r_arbid-option = 'EQ'.
    r_arbid-sign = 'I'.
    append r_arbid.
  endselect.
endform.                    " BILDEN_RANGE

*&---------------------------------------------------------------------*
*&      Form  lesen_afru
*&---------------------------------------------------------------------*
form lesen_afru.

  data: l_matnr like mara-matnr.

  check not r_arbid[] is initial.
  select * from afru into table iafru
                     where ersda in s_ersda
                     and   werks = p_werks
                     and   arbid in r_arbid
                     and   erzet in s_erzet
                     and   stokz = ' '
                     and   stzhl = 0.

  sort iafru by ersda erzet.

  refresh t_ausg.
  loop at iafru.
*    select single matnr into l_matnr from afpo where aufnr = iafru-aufnr.
"Commented by IN23914 during code remediation
    select  matnr into l_matnr from afpo
"Added by IN23914 during code remediation
UP TO 1 ROWS
 where aufnr = iafru-aufnr
ORDER BY PRIMARY KEY.
ENDSELECT."Added by IN23914 during code remediation
    select single * from mara where matnr = l_matnr.
    check s_typcd.

    refresh iafvc.
    select * from afvc into table iafvc where aufpl = iafru-aufpl
                                        and   arbid in r_arbid
                                        order by vornr descending.

    read table iafvc index 1.
    check iafvc-vornr = iafru-vornr.

    clear t_ausg.
    move-corresponding iafru to t_ausg.
    t_ausg-matnr = mara-matnr.
    t_ausg-zztyp = mara-zztyp.
    select single maktx into t_ausg-maktx from makt
                                          where matnr = mara-matnr
                                          and   spras = sy-langu.
    select single arbpl into t_ausg-arbpl from crhd where objty = 'A'
                                                    and   objid = iafru-arbid.
    append t_ausg.

  endloop.
endform.                    "lesen_afru

*&---------------------------------------------------------------------*
*&      Form  AUSGEBEN_LISTE
*&---------------------------------------------------------------------*
form ausgeben_liste .
  check not t_ausg[] is initial.

  read table r_arbid index 2.
  if sy-subrc = 0.
    w_title = syst-title.
  else.
    concatenate syst-title s_arbpl-low into w_title separated by space.
  endif.

  call function 'REUSE_ALV_GRID_DISPLAY'
    exporting
      i_callback_program = 'ZPP_RM_MENGEN'
      i_structure_name   = 'ZPP_RM_MENGEN'
      i_grid_title       = w_title
      i_save             = 'A'
    tables
      t_outtab           = t_ausg
    exceptions
      program_error      = 1
      others             = 2.

endform.                    " AUSGEBEN_LISTE
