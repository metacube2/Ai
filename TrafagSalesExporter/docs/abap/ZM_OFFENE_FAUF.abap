report zm_offene_fauf .

*-Tabellen-------------------------------------------------------------*
tables: mara, makt, afpo, afko, aufk, plaf, marc, plko,
        kbko,
        cabn, ausp,
        vakpa, kna1, vbak, vbbe, vbap, vbep, zckapa.
tables: zmm_ueb_fauf, crhd.

*-Selektionen----------------------------------------------------------*
selection-screen skip 1.
selection-screen begin of block b1 with frame title text-001.
select-options: s_aufnr for afpo-aufnr,
                s_auart for aufk-auart,
                s_dispo for afko-dispo,
                s_gltrp for afko-gltrp,
                s_woche for zmm_ueb_fauf-kwoche,
                s_fevor for afko-fevor.
selection-screen end of block b1.
selection-screen begin of block b2 with frame title text-002.
select-options: s_bismt for mara-bismt,
                s_matnr for mara-matnr,
                s_zztyp for mara-zztyp_f4,
                s_matgr for marc-matgr,
                s_ktext for plko-ktext.
selection-screen end of block b2.
selection-screen begin of block b3 with frame title text-003.
select-options: s_kdauf for afpo-kdauf,
                s_omeng for vbbe-omeng,
                s_kunde for kna1-kunnr,
                s_name1 for kna1-name1,
                s_mbdat for vbbe-mbdat.
selection-screen end of block b3.
selection-screen begin of block b4 with frame title text-004.
parameters: p_plaf as checkbox default 'X',
            p_elikz as checkbox,
            p_avo as checkbox default ' ',
            p_peg as checkbox default 'X'.   "Bedarfsverursacher lesen (2026-10-07)
selection-screen skip 1.
select-options: s_arbpl for crhd-arbpl.



selection-screen end of block b4.




*-Ranges---------------------------------------------------------------*
ranges: r_week for afko-gltrp.

*-Int. Tabellen--------------------------------------------------------*
data: gt_uebauf like zmm_ueb_fauf occurs 0 with header line.
data: iafpo like afpo occurs 0 with header line.
data: iaufm like aufm occurs 0 with header line.
data: begin of bptab occurs 0.
        include structure zzbptab.
data: end of bptab.

*-Diverses-------------------------------------------------------------*
data: xweek like scal-week,
      xdatum like sy-datum,
      atinn_lstfakt like cabn-atinn.

type-pools: slis.

data: gs_extract1 like disextract.
data: gs_extract2 like disextract.
data: gt_fieldcat type slis_t_fieldcat_alv.

*----------------------------------------------------------------------*
* F4-Suchhilfe für Arbeitsplatz (Werk 1100)
*----------------------------------------------------------------------*
at selection-screen on value-request for s_arbpl-low.
  perform f4_arbpl using 'S_ARBPL-LOW'.

at selection-screen on value-request for s_arbpl-high.
  perform f4_arbpl using 'S_ARBPL-HIGH'.

*----------------------------------------------------------------------*
initialization.
  call function 'REUSE_ALV_EXTRACT_AT_INIT'
    changing
      cs_extract1 = gs_extract1
      cs_extract2 = gs_extract2.

*----------------------------------------------------------------------*
* Verarbeitung                                                         *
*----------------------------------------------------------------------*
start-of-selection.

  perform e01_fieldcat_init  using gt_fieldcat[].
  perform bilden_selwochen.
  perform merkmal_leistungsfaktor.
  perform fill_iafpo.

  refresh gt_uebauf.
  perform fill_uebauf.
















  call function 'REUSE_ALV_LIST_DISPLAY'
    exporting
      i_callback_program = gs_extract1-report
      i_structure_name   = 'ZMM_UEB_FAUF'
      it_fieldcat        = gt_fieldcat[]


      i_save             = 'A'
    tables
      t_outtab           = gt_uebauf.


































*&---------------------------------------------------------------------*
*&      Form  fill_iafpo
*&---------------------------------------------------------------------*
form fill_iafpo.
  refresh iafpo.
  if not s_aufnr[] is initial.
    if p_elikz is initial.
      select * from afpo into table iafpo where aufnr in s_aufnr
                                          and   dnrel = ' '
                                          and   elikz = ' '.
    else.
      select * from afpo into table iafpo where aufnr in s_aufnr.
    endif.
    exit.
  endif.
  if not s_dispo[] is initial.
    select * from afko where dispo in s_dispo.
*      select single
      SELECT * "Added by NTT
         from afpo
         UP TO 1 ROWS "Added by NTT
         where aufnr = afko-aufnr
        ORDER BY PRIMARY KEY."Added by NTT
      ENDSELECT."Added by NTT
      iafpo = afpo.
      append iafpo.
    endselect.
    if p_plaf = 'X'.
      select * from plaf where dispo in s_dispo.
        perform fill_iafpo_from_plaf.
      endselect.
    endif.
    exit.
  endif.
  if not s_kdauf[] is initial.
    select * from afpo into table iafpo where kdauf in s_kdauf.
    if p_plaf = 'X'.
      select * from plaf where kdauf in s_kdauf.
        perform fill_iafpo_from_plaf.
      endselect.
    endif.
    exit.
  endif.
  if not s_zztyp[] is initial.
    select * from mara where zztyp_f4 in s_zztyp.
      select * from afpo where matnr = mara-matnr.
        iafpo = afpo.
        append iafpo.
      endselect.
      if p_plaf = 'X'.
        select * from plaf where matnr = mara-matnr.
          perform fill_iafpo_from_plaf.
        endselect.
      endif.
    endselect.
    exit.
  endif.
  if not s_matnr[] is initial.
    select * from mara where matnr in s_matnr.
      select * from afpo where matnr = mara-matnr.
        iafpo = afpo.
        append iafpo.
      endselect.
      if p_plaf = 'X'.
        select * from plaf where matnr = mara-matnr.
          perform fill_iafpo_from_plaf.
        endselect.
      endif.
    endselect.
    exit.
  endif.
  if not s_bismt[] is initial.
    select * from mara where bismt in s_bismt.
      select * from afpo where matnr = mara-matnr.
        iafpo = afpo.
        append iafpo.
      endselect.
      if p_plaf = 'X'.
        select * from plaf where matnr = mara-matnr.
          perform fill_iafpo_from_plaf.
        endselect.
      endif.
    endselect.
    exit.
  endif.
  if not s_kunde[] is initial.
    select * from vakpa where kunde in s_kunde.
      select * from afpo where kdauf = vakpa-vbeln.
        iafpo = afpo.
        append iafpo.
      endselect.
      if p_plaf = 'X'.
        select * from plaf where kdauf = vakpa-vbeln.
          perform fill_iafpo_from_plaf.
        endselect.
      endif.
    endselect.
    exit.
  endif.
  if not s_name1[] is initial.
    select * from kna1 where name1 in s_name1.
      select * from vakpa where kunde = kna1-kunnr.
        select * from afpo where kdauf = vakpa-vbeln.
          iafpo = afpo.
          append iafpo.
        endselect.
        if p_plaf = 'X'.
          select * from plaf where kdauf = vakpa-vbeln.
            perform fill_iafpo_from_plaf.
          endselect.
        endif.
      endselect.
    endselect.
    exit.
  endif.
endform.                    " fill_iafpo


*&---------------------------------------------------------------------*





















































































































































*&      Form  FILL_IAFPO_FROM_PLAF
*&---------------------------------------------------------------------*
form fill_iafpo_from_plaf .
  check plaf-umskz = 'X'.
  check plaf-beskz ne 'F'.
  clear iafpo.
  iafpo-aufnr = plaf-plnum.
  iafpo-matnr = plaf-matnr.
  iafpo-dwerk = plaf-plwrk.
  iafpo-kdauf = plaf-kdauf.
  iafpo-kdpos = plaf-kdpos.
  iafpo-pwerk = plaf-paart.
  iafpo-psmng = plaf-gsmng.
  iafpo-meins = plaf-meins.
  if plaf-auffx = 'X'.
    iafpo-pwerk+2(2) = 'FX'.
  endif.
  iafpo-objnp = 'PLAF'.
  append iafpo.
endform.                    " FILL_IAFPO_FROM_PLAF

*&---------------------------------------------------------------------*
*&      Form  fill_uebauf
*&---------------------------------------------------------------------*
form fill_uebauf.
  loop at iafpo.
    clear: afko, mara, aufk, marc, plko.
    afpo = iafpo.
    if p_elikz is initial.
      check: afpo-dnrel = ' '.
      check: afpo-elikz = ' '.
    endif.
    if iafpo-objnp ne 'PLAF'.
      check: s_aufnr.
    endif.
    check: s_kdauf.

    if iafpo-objnp ne 'PLAF'.
      select single * from afko where aufnr = iafpo-aufnr.
    else.
      select single * from plaf where plnum = iafpo-aufnr.
      afko-aufnr = iafpo-aufnr.
      afko-dispo = plaf-dispo.
      afko-gltrp = plaf-pedtr.
      afko-fevor = plaf-plgrp.
      select single * from kbko
        where bedid = plaf-bedid."#EC CI_USAGE_OK[2380568] "Addedby NTT
      if sy-subrc = 0.
        afko-plnty = kbko-plnty. "#EC CI_USAGE_OK[2380568] "Addedby NTT
        afko-plnnr = kbko-plnnr. "#EC CI_USAGE_OK[2380568] "Addedby NTT
        afko-plnal = kbko-plnal. "#EC CI_USAGE_OK[2380568] "Addedby NTT
      endif.
    endif.
    check: s_gltrp.
    check afko-gltrp in r_week.
    check afko-fevor in s_fevor.

    clear mara.
    select single * from mara where matnr = iafpo-matnr.
    check: s_bismt, s_zztyp, s_matnr.

    select single * from marc where matnr = iafpo-matnr
                              and   werks = iafpo-dwerk.
    check s_matgr.

    if iafpo-objnp ne 'PLAF'.
      select single * from aufk where aufnr = iafpo-aufnr.
    else.
      aufk-aufnr = iafpo-aufnr.
      aufk-auart = iafpo-pwerk.
      aufk-kdauf = iafpo-kdauf.
      aufk-kdpos = iafpo-kdpos.
    endif.
    check s_auart.

    clear: vbak, kna1.
    select single * from vbak where vbeln = iafpo-kdauf.
    select single * from kna1 where kunnr = vbak-kunnr.
    check: s_kunde, s_name1.

    clear gt_uebauf.
    if not mara-matnr is initial.
      move-corresponding mara to gt_uebauf.
      move-corresponding marc to gt_uebauf.
    endif.
    if not kna1-kunnr is initial.
      move-corresponding kna1 to gt_uebauf.
    endif.
    move-corresponding iafpo to gt_uebauf.
    perform leistungsmenge.
    move-corresponding afko to gt_uebauf.
    move-corresponding aufk to gt_uebauf.
    gt_uebauf-ernam = vbak-ernam.
    gt_uebauf-gltri = iafpo-ltrmi.

    select single * from makt where matnr = mara-matnr
                              and   spras = sy-langu.
    gt_uebauf-maktx = makt-maktx.

*    select single
      SELECT  *  "Addedby NTT
        from plko
        UP TO 1 ROWS "Added by NTT
        where plnty = afko-plnty
               and   plnnr = afko-plnnr
               and   plnal = afko-plnal
        ORDER BY PRIMARY KEY."Added by NTT
      ENDSELECT."Addedby NTT
    if makt-maktx = plko-ktext.
      clear plko-ktext.
    endif.
    check s_ktext.
    gt_uebauf-ktext = plko-ktext.

    clear vbap.
    select single * from vbap where vbeln = iafpo-kdauf
                              and   posnr = iafpo-kdpos.
    if sy-subrc = 0.
      gt_uebauf-kwmeng  = vbap-kwmeng.
      gt_uebauf-zzlaenge = vbap-zzlaenge.
      gt_uebauf-zzlaenge_2 = vbap-zzlaenge_2.
      gt_uebauf-zzsp001 = vbap-zzsp001.
      gt_uebauf-zzsp002 = vbap-zzsp002.
      gt_uebauf-zzsp003 = vbap-zzsp003.
      gt_uebauf-zzsp004 = vbap-zzsp004.
      gt_uebauf-zzsp005 = vbap-zzsp005.
      gt_uebauf-zzsondb = vbap-zzsondb.

      call function 'CONVERT_TO_LOCAL_CURRENCY'
        exporting
          date             = sy-datum
          foreign_amount   = vbap-netwr
          foreign_currency = vbak-waerk
          local_currency   = 'CHF  '
        importing
          local_amount     = gt_uebauf-netwr.
    endif.

    gt_uebauf-erdat = vbap-erdat.
    select single bstdk into gt_uebauf-bstdk from vbkd where vbeln = vbap-vbeln
                                                       and   posnr = vbap-posnr.
    if sy-subrc ne 0.
      select single bstdk into gt_uebauf-bstdk from vbkd where vbeln = vbap-vbeln
                                                         and   posnr = 0.
    endif.


*    select single dzeit from marc into gt_uebauf-dzeit where matnr = mara-matnr
*                                                       and   werks = iafpo-pwerk.

    perform bereich using gt_uebauf-range.

    clear xweek.
    call function 'DATE_GET_WEEK'
      exporting
        date = afko-gltrp
      importing
        week = xweek.
    gt_uebauf-kwoche = '../..'.
    gt_uebauf-kwoche+0(2) = xweek+4(2).
    gt_uebauf-kwoche+3(2) = xweek+2(2).

    perform kdauf_termine.
    check gt_uebauf-datum_b in s_mbdat.

    if gt_uebauf-kwmeng ne 0.
      gt_uebauf-netwr = gt_uebauf-netwr * gt_uebauf-omeng /
                        gt_uebauf-kwmeng.
    endif.

    if iafpo-objnp ne 'PLAF'.
      call function 'STATUS_TEXT_EDIT'
        exporting
          objnr = aufk-objnr
          spras = sy-langu
        importing
          line  = gt_uebauf-sttxt.
      perform vorgangsdaten.
      select single gltrp into gt_uebauf-gltrp1 from zcfauf_gltrp
                                                where aufnr = gt_uebauf-aufnr.
    endif.
*   Arbeitsplatz-Filter: Auftrag überspringen wenn nicht in Selektion
    if not s_arbpl[] is initial.
      check gt_uebauf-arbpl in s_arbpl.
    endif.

    perform kapa_bedarfe.

    perform erster_bezug.

    perform bedarfsverursacher.

    append gt_uebauf.
  endloop.
  sort gt_uebauf by gltrp.
endform.                    " fill_uebauf

*&---------------------------------------------------------------------*
*&      Form  bilden_selwochen
*&---------------------------------------------------------------------*
form bilden_selwochen.
  refresh r_week.
  check not s_woche[] is initial.
  loop at s_woche.
    if s_woche-low is initial and
      s_woche-option = 'BT'.
      s_woche-low = '0101'.
    endif.

    if s_woche-option = 'EQ'.
      perform wochentag using s_woche-low r_week-low '0'.
      perform wochentag using s_woche-low r_week-high '7'.
    endif.
    if s_woche-option = 'BT'.
      perform wochentag using s_woche-low r_week-low '0'.
      perform wochentag using s_woche-high r_week-high '7'.
    endif.
    r_week-sign = s_woche-sign.
    r_week-option = 'BT'.
    append r_week.
  endloop.
endform.                    " bilden_selwochen

*&---------------------------------------------------------------------*
*&      Form  wochentag
*&---------------------------------------------------------------------*
form wochentag using w_in w_out anztag.

  data: xw_in(5).

  xw_in = w_in.
  xweek = sy-datum.
  xweek+4(2) = xw_in+0(2).
  if xw_in+4(1) = ' '.
    xweek+2(2) = xw_in+2(2).
  else.
    xweek+2(2) = xw_in+3(2).
  endif.

  call function 'WEEK_GET_FIRST_DAY'
    exporting
      week         = xweek
    importing
      date         = xdatum
    exceptions
      week_invalid = 1
      others       = 2.

  if sy-subrc = 0.
    xdatum = xdatum + anztag.
    w_out = xdatum.
  endif.

endform.                    " wochentag

*&---------------------------------------------------------------------*
*&      Form  vorgangsdaten
*&---------------------------------------------------------------------*

form vorgangsdaten.
* Vorgangsdaten holen wenn Checkbox aktiv ODER Arbeitsplatz-Filter gesetzt
  check p_avo = 'X' or not s_arbpl[] is initial.

  call function 'Z_MD04_NEXT_OPERATION'
    exporting
      i_aufnr = afko-aufnr
    importing
      e_vornr = gt_uebauf-vornr
      e_arbpl = gt_uebauf-arbpl.

endform.                    "vorgangsdaten


*---------------------------------------------------------------------*
*       FORM bereich                                                  *
*---------------------------------------------------------------------*
form bereich using n.
  refresh bptab.
  call function 'Z_BASISPREISLISTE'
    exporting
      i_matnr   = iafpo-matnr
      i_mit_loe = 'X'
    tables
      bptab     = bptab
    exceptions
      no_object = 1
      no_typcd  = 2
      others    = 3.

  if mara-zztyp(2) = '87'.             "kein Range !!!
    exit.
  endif.

  if mara-zztyp(1) = '8'.
    read table bptab index 2.
  else.
    read table bptab index 3.
  endif.

  if sy-subrc = 0.
    while bptab-text ne space.
      if bptab-text(1) co '+-0123456789'.
        exit.
      endif.
      shift bptab-text.
    endwhile.
    if bptab-text(1) co '+-0123456789'.
      n = bptab-text.
      exit.
    endif.
  endif.
endform.                    "bereich

*&---------------------------------------------------------------------*
*&      Form  kdauf_termine
*&---------------------------------------------------------------------*
form kdauf_termine.
  clear vbbe.
  check not iafpo-kdauf is initial.
  select * from vbbe where vbeln = iafpo-kdauf
                     and   posnr = iafpo-kdpos.
    if vbbe-omeng gt 0.
      gt_uebauf-omeng = gt_uebauf-omeng + vbbe-omeng.
      gt_uebauf-datum_w = vbbe-mbdat.
    else.
      gt_uebauf-datum_b = vbbe-mbdat.
    endif.
    if gt_uebauf-datum_b is initial.
      gt_uebauf-datum_b = gt_uebauf-datum_w.
    endif.
  endselect.
  if sy-subrc ne 0.
    select * from vbep where vbeln = iafpo-kdauf
                       and   posnr = iafpo-kdpos
                       and   lfrel = 'X'.
      if vbep-wmeng gt 0.
        gt_uebauf-datum_w = vbep-edatu.
      else.
        gt_uebauf-datum_b = vbep-edatu.
      endif.
      if gt_uebauf-datum_b is initial.
        gt_uebauf-datum_b = gt_uebauf-datum_w.
      endif.
    endselect.
  endif.
endform.                    " kdauf_termine

*&---------------------------------------------------------------------*
*&      Form  KAPA_BEDARFE
*&---------------------------------------------------------------------*
form kapa_bedarfe .
  select single * from zckapa where matnr = gt_uebauf-matnr
                              and   werks = iafpo-dwerk.
  if sy-subrc ne 0.                            "Falls noch nicht berechnet,
    call function 'Z_PP_FILL_ZCKAPA'           "Aufruf Berechnung
      exporting
        matnr      = gt_uebauf-matnr
        werks      = iafpo-dwerk
      changing
        zckapa     = zckapa
      exceptions
        no_routing = 1
        others     = 2.
    if sy-subrc = 0.
      modify zckapa.
      move-corresponding zckapa to gt_uebauf.
    endif.
  else.
    move-corresponding zckapa to gt_uebauf.
  endif.
endform.                    " KAPA_BEDARFE

*&---------------------------------------------------------------------*
*&      Form  ERSTER_BEZUG
*&---------------------------------------------------------------------*
form erster_bezug .
  refresh iaufm.
  select * from aufm into table iaufm
                     where aufnr = iafpo-aufnr
                     and   shkzg = 'H'
                     order by bldat ascending.
  if sy-subrc = 0.
    read table iaufm index 1.
    gt_uebauf-zzmbdat = iaufm-bldat.
  endif.
endform.                    " ERSTER_BEZUG
*&---------------------------------------------------------------------*
*&      Form  F4_ARBPL
*&---------------------------------------------------------------------*
form f4_arbpl using p_field type any.
  data: begin of ls_arbpl,
          arbpl type crhd-arbpl,
          werks type crhd-werks,
        end of ls_arbpl.
  data: lt_arbpl like table of ls_arbpl,
        lt_return type table of ddshretval,
        ls_return type ddshretval.

  select arbpl werks from crhd
    into table lt_arbpl
    where werks = '1100'.

  sort lt_arbpl by arbpl.
  delete adjacent duplicates from lt_arbpl comparing arbpl.

  call function 'F4IF_INT_TABLE_VALUE_REQUEST'
    exporting
      retfield   = 'ARBPL'
      value_org  = 'S'
    tables
      value_tab  = lt_arbpl
      return_tab = lt_return
    exceptions
      others     = 1.

  if sy-subrc = 0.
    read table lt_return into ls_return index 1.
    if sy-subrc = 0.
      case p_field.
        when 'S_ARBPL-LOW'.
          s_arbpl-low = ls_return-fieldval.
        when 'S_ARBPL-HIGH'.
          s_arbpl-high = ls_return-fieldval.
      endcase.
    endif.
  endif.
endform.                    "F4_ARBPL




*&---------------------------------------------------------------------*
*&      Form  MERKMAL_LEISTUNGSFAKTOR
*&---------------------------------------------------------------------*
form merkmal_leistungsfaktor .
*-Einlesen Schlüssel Merkmal 'MAT_ALLG_LEISTUNGSFAKTOR' ---------------*
  clear atinn_lstfakt.
  select *  "#EC CI_NOORDER "Addedby NTT
    from cabn where atnam = 'MAT_ALLG_LEISTUNGFAKTOR'.
  endselect.
  if sy-subrc = 0.
    atinn_lstfakt = cabn-atinn.
  endif.
endform.                    " MERKMAL_LEISTUNGSFAKTOR

*&---------------------------------------------------------------------*
*&      Form  LEISTUNGSMENGE
*&---------------------------------------------------------------------*
form leistungsmenge .
  data: l_objek(50),
        l_faktor type p decimals 3.

  check not atinn_lstfakt is initial.
  l_objek = gt_uebauf-matnr.
  clear ausp.
  select *  "#EC CI_NOORDER "Addedby NTT
    from ausp
    where objek = l_objek
     and   atinn = atinn_lstfakt . "#EC CI_FLDEXT_OK[2215424] "AddedbyNTT
  endselect.
  if sy-subrc = 0.
    l_faktor = ausp-atflv.
  else.
    l_faktor = 1.
  endif.
  gt_uebauf-zzmng_lst = gt_uebauf-psmng * l_faktor.
endform.                    " LEISTUNGSMENGE

*&---------------------------------------------------------------------*
*&      Form  E01_FIELDCAT_INIT
*&---------------------------------------------------------------------*
form e01_fieldcat_init using e01_lt_fieldcat type slis_t_fieldcat_alv.
  data: ls_fieldcat type slis_fieldcat_alv.

  clear ls_fieldcat.
  ls_fieldcat-fieldname = 'NETWR'.
  ls_fieldcat-no_zero  = 'X'.
  append ls_fieldcat to e01_lt_fieldcat.

  clear ls_fieldcat.
  ls_fieldcat-fieldname = 'KWMENG'.
  ls_fieldcat-no_zero  = 'X'.
  append ls_fieldcat to e01_lt_fieldcat.
  clear ls_fieldcat.

  ls_fieldcat-fieldname = 'OMENG'.
  ls_fieldcat-no_zero  = 'X'.
  append ls_fieldcat to e01_lt_fieldcat.

* 2026-10-07: Bedarfsverursacher standardmaessig ausgeblendet, ueber Layout einblendbar
  data: lt_peg type standard table of slis_fieldname,
        l_peg  type slis_fieldname.
  append 'ZZPEG_DELKZ' to lt_peg. append 'ZZPEG_DELNR' to lt_peg.
  append 'ZZPEG_DELPS' to lt_peg. append 'ZZPEG_DELET' to lt_peg.
  append 'ZZPEG_EXTRA' to lt_peg. append 'ZZPEG_BERID' to lt_peg.
  append 'ZZPEG_DAT00' to lt_peg. append 'ZZPEG_MNG01' to lt_peg.
  append 'ZZPEG_ANZ'   to lt_peg.
  loop at lt_peg into l_peg.
    clear ls_fieldcat.
    ls_fieldcat-fieldname = l_peg.
    ls_fieldcat-no_out    = 'X'.
    ls_fieldcat-no_zero   = 'X'.
    append ls_fieldcat to e01_lt_fieldcat.
  endloop.
endform.                    " E01_FIELDCAT_INIT

*&---------------------------------------------------------------------*
*&      Form  BEDARFSVERURSACHER
*&---------------------------------------------------------------------*
* 2026-10-07 (Ingo Kohler): erster Bedarfsverursacher wie MD04 >
* Bedarfsverursacher (Dispoelement, Nummer, Position, Einteilung,
* Daten zum Dispoelement, Dispobereich, Termin, Menge) plus Anzahl.
* Felder ZZPEG_* in ZMM_UEB_FAUF, im Layout standardmaessig ausgeblendet.
form bedarfsverursacher.
  data: lt_mdrq  type standard table of mdrq,
        ls_mdrq  type mdrq,
        lt_mdps  type standard table of mdps,
        l_delkz  type mdps-delkz,
        l_berid  type mt61d-berid,
        l_anz    type i.

  check p_peg = 'X'.
  clear l_berid.
  if iafpo-objnp = 'PLAF'.
    l_delkz = 'PA'.
    l_berid = plaf-berid.
  else.
    l_delkz = 'FE'.
  endif.

  call function 'MD_PEGGING_NODIALOG'
    exporting
      edelkz                = l_delkz
      edelnr                = iafpo-aufnr
      ematnr                = iafpo-matnr
      ewerks                = iafpo-dwerk
      eberid                = l_berid
    tables
      imdrqx                = lt_mdrq
      emdpsx                = lt_mdps
    exceptions
      error                 = 1
      no_requirements_found = 2
      order_not_found       = 3
      others                = 4.
  check sy-subrc = 0.

  describe table lt_mdrq lines l_anz.
  gt_uebauf-zzpeg_anz = l_anz.
  read table lt_mdrq into ls_mdrq index 1.
  check sy-subrc = 0.
  gt_uebauf-zzpeg_delkz = ls_mdrq-delkz.
  gt_uebauf-zzpeg_delnr = ls_mdrq-delnr.
  gt_uebauf-zzpeg_delps = ls_mdrq-delps.
  gt_uebauf-zzpeg_delet = ls_mdrq-delet.
  gt_uebauf-zzpeg_extra = ls_mdrq-extra.
  gt_uebauf-zzpeg_berid = ls_mdrq-berid.
  gt_uebauf-zzpeg_dat00 = ls_mdrq-dat00.
  gt_uebauf-zzpeg_mng01 = ls_mdrq-mng01.
endform.                    " BEDARFSVERURSACHER
