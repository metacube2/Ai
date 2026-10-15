report zlo_calc_cz_preise line-size 166.

*----------------------------------------------------------------------*
* Berechnung Werksabgabepreise CZ                                      *
*----------------------------------------------------------------------*

*-Tabellen-------------------------------------------------------------*
tables: marc, keko, t024d.

*-Selektionen----------------------------------------------------------*
selection-screen begin of block b1 with frame title text-t01.
select-options: s_matnr for marc-matnr obligatory memory id mat.
parameters: p_werks like marc-werks obligatory memory id wrk.
selection-screen end of block b1.
selection-screen begin of block b2 with frame title text-t02.
parameters: p_sttag like sy-datum obligatory default sy-datum.
selection-screen end of block b2.
selection-screen begin of block b3 with frame title text-t03.
parameters: p_matup as checkbox.
selection-screen begin of line.
selection-screen comment 01(31) text-001.
parameters: p_datab like sy-datum obligatory default sy-datum.
selection-screen comment 57(10) text-002.
parameters: p_datbi like sy-datum obligatory.
selection-screen end of line.
selection-screen end of block b3.

*-Interne Tabellen-----------------------------------------------------*
data: imarc like marc occurs 0 with header line.
data: ickis like ckis occurs 0 with header line.
data: icrhd like crhd occurs 0 with header line.
data: begin of htab occurs 0,
       herkunft(3),
       fert_e like mbew-salk3,
       fert_f like mbew-salk3,
       material like mbew-salk3,
       gk like mbew-salk3,
       total like mbew-salk3,
      end of htab.
data: begin of kaltab occurs 0,
        matnr like marc-matnr,
        werks like marc-werks,
        menge like ckis-menge,
      end of kaltab.
data: bdcdata like bdcdata    occurs 0 with header line.
data: bdcdata2 like bdcdata    occurs 0 with header line.

*-Diverses-------------------------------------------------------------*
data: w_maktx like makt-maktx,
      w_herkl like marc-herkl,
      w_faktor type p decimals 3,
      w_wert like mbew-salk3,
      w_matwert like mbew-salk3,
      w_meins like keko-meins,
      w_antfgk type p decimals 5,
      w_stort_oben(2),
      w_stort_unten(2),
      w_counter type i,
      w_lines type p,
      w_mstae like mara-mstae,
      m_count type p,
      fieldname(15),
      w_datab(10),
      w_datbi(10).

data: begin of fgk,
        ch like mbew-salk3,
        cz like mbew-salk3,
        tfgk like mbew-salk3,
      end of fgk.

initialization.
  p_datbi = sy-datum.
  p_datbi+4(4) = '1231'.

start-of-selection.
*----------------------------------------------------------------------*
* Verarbeitung                                                         *
*----------------------------------------------------------------------*
  select * from marc into table imarc where matnr in s_matnr
                                      and   werks eq p_werks
                                      and   ( ( beskz = 'E' or beskz = 'X' ) or
                                            sobsl = '30' )
                                      and   sobsl ne '50'
                                      and   lvorm = ' '.

  select * from crhd into table icrhd where arbpl like 'CZ%'.  "Tschechische Arbeitsplätze

  if p_matup = 'X'.
    perform open_group.
  endif.

  loop at imarc.
    select single mstae into w_mstae from mara where matnr = imarc-matnr.
    check w_mstae ne '99'.
    if imarc-losgr is initial.
      imarc-losgr = 1.
    endif.
    refresh htab.
    select single maktx into w_maktx from makt where matnr = imarc-matnr
                                     and   spras = sy-langu.
    clear keko.
    select * from keko where matnr = imarc-matnr
                       and   werks = imarc-werks
                       and   kalka = '01'
                       and   kadky le p_sttag
                       and   tvers = 1
                       and   loekz = ' '
                       and   feh_sta ne 'KF'
                       order by primary key.
    endselect.
    perform standort using imarc-matnr imarc-werks w_stort_oben 'X'.
    if not keko-losgr is initial.
      imarc-losgr = keko-losgr.
    endif.
    modify imarc.
    w_meins = keko-meins.

    refresh kaltab.
    w_faktor = 1.
    perform lesen_kalkulation.

    loop at kaltab.
      describe table kaltab lines w_lines.
      if w_lines gt 1000.
        summary.
        write: / "#EC CI_NOORDER "Added by NTT
         imarc-matnr,  "#EC CI_FLDEXT_OK[2215424] "Addedby NTT
         imarc-werks,  "#EC CI_NOORDER "Addedby NTT
          'Auflösung wird rekursiv - Abbruch'.
        uline.
        refresh htab.
        exit.
      endif.
      select * from keko where matnr = kaltab-matnr
                         and   werks = kaltab-werks
                         and   kalka = '01'
                         and   kadky le p_sttag
                         and   tvers = 1
                         and   loekz = ' '
                         and   feh_sta ne 'KF'
                         order by primary key.
      endselect.
      perform standort using kaltab-matnr kaltab-werks w_stort_oben ' '.
      w_faktor = kaltab-menge / keko-losgr.
      perform lesen_kalkulation.
    endloop.

    clear htab.
    read table htab with key herkunft = 'CZ'.
    htab-fert_e = htab-fert_e / imarc-losgr.
    htab-gk = htab-gk / imarc-losgr.
    w_wert = htab-fert_e + htab-gk.

    clear w_matwert.
    loop at htab.
      w_matwert = w_matwert + htab-material.
    endloop.
    w_matwert = w_matwert / imarc-losgr.

    if not w_wert is initial.
      detail.
      write: / "#EC CI_NOORDER "Addedby NTT
      imarc-matnr, "#EC CI_FLDEXT_OK[2215424] "Addedby NTT
                  w_maktx, "#EC CI_NOORDER
               (8) imarc-losgr "#EC CI_NOORDER
                unit keko-meins,
                w_wert, w_matwert.
      if p_matup = 'X'.
        perform mek1.
      endif.
    endif.
  endloop.

  if p_matup = 'X'.
    perform close_group.
    uline.
    summary.
    write:/ 'Batch Input Mappe PREISE_CZ abspielen mittels SM35.'.
    uline.
  endif.

*---------------------------------------------------------------------*
*       FORM lesen_kalulation                                         *
*---------------------------------------------------------------------*
form lesen_kalkulation.
  refresh ickis.
  select * from ckis into table ickis
                     where lednr = '00'
                     and   bzobj = '0'
                     and   kalnr = keko-kalnr
                     and   kalka = keko-kalka
                     and   kadky = keko-kadky
                     and   tvers = keko-tvers
                     and   bwvar = keko-bwvar.

  clear fgk.
  loop at ickis.
    check ickis-wrtfw_kpf ne 0.

    ickis-wrtfw_kpf = ickis-wrtfw_kpf * w_faktor.
    clear htab.
    clear w_herkl.
    case ickis-typps.

      when 'M' or 'I'.
        select single * from marc where matnr = ickis-matnr
                                  and   werks = ickis-werks.
        perform standort using marc-matnr marc-werks w_stort_unten ' '.
        if ickis-ukaln is initial or
          marc-beskz = 'F' or
          w_stort_oben ne w_stort_unten.
          perform herkunft using marc-herkl.
          w_herkl = marc-herkl.
          htab-material = ickis-wrtfw_kpf.
        else.
          kaltab-matnr = ickis-matnr.
          kaltab-menge = ickis-menge * w_faktor.
          kaltab-werks = ickis-werks.
          append kaltab.
          clear ickis-wrtfw_kpf.
        endif.

      when 'E'.
        read table icrhd with key objty = 'A'
                                  objid = ickis-arbid.
        if sy-subrc ne 0.
          perform herkunft using 'CH'.
          w_herkl = 'CH'.
          fgk-ch = fgk-ch + ickis-wrtfw_kpf.
          fgk-tfgk = fgk-tfgk + ickis-wrtfw_kpf.
        else.
          perform herkunft using 'CZ'.          "Sonderfall Tschechien
          w_herkl = 'CZ'.
          fgk-cz = fgk-cz + ickis-wrtfw_kpf.
          fgk-tfgk = fgk-tfgk + ickis-wrtfw_kpf.
        endif.

        htab-fert_e = ickis-wrtfw_kpf.

      when 'F'.
        select single land1 into w_herkl from lfa1 where lifnr = ickis-lifnr.
        perform herkunft using w_herkl.
        htab-fert_f = ickis-wrtfw_kpf.

      when others.
    endcase.

    if ickis-wrtfw_kpf gt 0.
      htab-total = ickis-wrtfw_kpf.
      collect htab.
    endif.

  endloop.

  if ickis[] is initial.
    detail.
    write: / 'Keine Erzeugniskalkulation durchgeführt!'.
  endif.
endform.                    "lesen_kalkulation


*---------------------------------------------------------------------*
*       FORM herkunft                                                 *
*---------------------------------------------------------------------*
form herkunft using lnd.
  htab-herkunft = lnd.
endform.                    "herkunft

*&---------------------------------------------------------------------*
*&      Form  STANDORT
*&---------------------------------------------------------------------*
form standort  using p_matnr p_werks p_stort p_hawa.

  data: l_dispo like marc-dispo.

  select single dispo from marc into l_dispo where matnr = p_matnr
                                             and   werks = p_werks.

  select single * from t024d where werks = p_werks
                             and   dispo = l_dispo.
  if t024d-dsnam cs 'CZ'.
    p_stort = 'CZ'.
  else.
    p_stort = 'CH'.
  endif.
endform.                    " STANDORT

*&---------------------------------------------------------------------*
*&      Form  MEK1
*&---------------------------------------------------------------------*
form mek1 .

  data: l_matnr(18),
        l_wert(12).

  m_count = m_count + 1.
  if m_count gt 10.
    perform transaktion_schreiben.
    m_count = 1.
  endif.
  if m_count = 1.
    perform bdc_dynpro      using 'SAPMV13A' '0100'.
    perform bdc_field       using 'BDC_OKCODE' '/00'.
    perform bdc_field       using 'RV13A-KSCHL' 'ZW01'.
    perform bdc_dynpro      using 'SAPMV13A' '1445'."#EC CI_USAGE_OK[2689873]
    perform bdc_field       using 'BDC_OKCODE' 'SICH'."#EC CI_USAGE_OK[2689873]

    perform bdc_dynpro2      using 'SAPMV13A' '0100'.
    perform bdc_field2       using 'BDC_OKCODE' '/00'.
    perform bdc_field2       using 'RV13A-KSCHL' 'ZW02'.
    perform bdc_dynpro2      using 'SAPMV13A' '1445'."#EC CI_USAGE_OK[2689873]
    perform bdc_field2       using 'BDC_OKCODE' 'SICH'."#EC CI_USAGE_OK[2689873]
  endif.

  fieldname = 'KOMG-MATNR(..)'. "#EC CI_FLDEXT_OK[2215424] "Addedby NTT
  unpack m_count to fieldname+11(2).
  write imarc-matnr to l_matnr."#EC CI_FLDEXT_OK[2215424] "Addedby NTT
  perform bdc_field       using fieldname l_matnr.
  fieldname = 'KONP-KBETR(..)'.
  unpack m_count to fieldname+11(2).
  write w_wert to l_wert decimals 2.
  perform bdc_field       using fieldname l_wert.
  fieldname = 'KONP-KONWA(..)'.
  unpack m_count to fieldname+11(2).
  perform bdc_field       using fieldname 'CHF'.
  fieldname = 'RV13A-DATAB(01)'.
  unpack m_count to fieldname+12(2).
  perform bdc_field       using fieldname w_datab.
  fieldname = 'RV13A-DATBI(01)'.
  unpack m_count to fieldname+12(2).
  perform bdc_field       using fieldname w_datbi.

  fieldname = 'KOMG-MATNR(..)'."#EC CI_FLDEXT_OK[2215424] "Addedby NTT
  unpack m_count to fieldname+11(2).
  write imarc-matnr to l_matnr. "#EC CI_FLDEXT_OK[2215424] "Addedby nTT
  perform bdc_field2      using fieldname l_matnr.
  fieldname = 'KONP-KBETR(..)'.
  unpack m_count to fieldname+11(2).
  write w_matwert to l_wert decimals 2.
  perform bdc_field2       using fieldname l_wert.
  fieldname = 'KONP-KONWA(..)'.
  unpack m_count to fieldname+11(2).
  perform bdc_field2       using fieldname 'CHF'.
  fieldname = 'RV13A-DATAB(01)'.
  unpack m_count to fieldname+12(2).
  perform bdc_field2       using fieldname w_datab.
  fieldname = 'RV13A-DATBI(01)'.
  unpack m_count to fieldname+12(2).
  perform bdc_field2       using fieldname w_datbi.
endform.                    " MEK1

*&---------------------------------------------------------------------*
*&      Form  transaktion_schreiben
*&---------------------------------------------------------------------*
form transaktion_schreiben.
  if m_count gt 0.
    perform bdc_transaction using 'MEK1' 'ZW01'.
    perform bdc_transaction using 'MEK1' 'ZW02'.
  endif.
endform.                    "transaktion_schreiben

*&---------------------------------------------------------------------*
*&      Form  OPEN_GROUP
*&---------------------------------------------------------------------*
form open_group .
  write p_datab to w_datab dd/mm/yyyy.
  write p_datbi to w_datbi dd/mm/yyyy.
  call function 'BDC_OPEN_GROUP'
    exporting
      client = sy-mandt
      group  = 'PREISE_CZ'
      user   = sy-uname.
endform.                    " OPEN_GROUP

*&---------------------------------------------------------------------*
*&      Form  CLOSE_GROUP
*&---------------------------------------------------------------------*
form close_group .
  perform transaktion_schreiben.
  call function 'BDC_CLOSE_GROUP'.
endform.                    " CLOSE_GROUP

*----------------------------------------------------------------------*
*        Start new transaction                                         *
*----------------------------------------------------------------------*
form bdc_transaction using tcode kschl .
  if kschl = 'ZW01'.
    call function 'BDC_INSERT'
      exporting
        tcode     = tcode
      tables
        dynprotab = bdcdata.
    refresh bdcdata.
  endif.
  if kschl = 'ZW02'.
    call function 'BDC_INSERT'
      exporting
        tcode     = tcode
      tables
        dynprotab = bdcdata2.
    refresh bdcdata2.
  endif.
endform.                    "BDC_TRANSACTION

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

*&---------------------------------------------------------------------*
*&      Form  bdc_dynpro2
*&---------------------------------------------------------------------*
form bdc_dynpro2 using program dynpro.
  clear bdcdata2.
  bdcdata2-program  = program.
  bdcdata2-dynpro   = dynpro.
  bdcdata2-dynbegin = 'X'.
  append bdcdata2.
endform.                    "bdc_dynpro2

*----------------------------------------------------------------------*
*        Insert field                                                  *
*----------------------------------------------------------------------*
form bdc_field using fnam fval.
  clear bdcdata.
  bdcdata-fnam = fnam.
  bdcdata-fval = fval.
  append bdcdata.
endform.                    "BDC_FIELD

*&---------------------------------------------------------------------*
*&      Form  bdc_field2
*&---------------------------------------------------------------------*
form bdc_field2 using fnam fval.
  clear bdcdata2.
  bdcdata2-fnam = fnam.
  bdcdata2-fval = fval.
  append bdcdata2.
endform.                    "bdc_field2
