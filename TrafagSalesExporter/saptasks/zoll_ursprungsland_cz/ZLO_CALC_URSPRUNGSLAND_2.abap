report zlo_calc_ursprungsland_2 line-size 166.

*----------------------------------------------------------------------*
* Berechnung Ursprungsland                                             *
*----------------------------------------------------------------------*
*
*Entweder: Der Warenursprung «CH» beträgt mehr als 50% (bezogen auf den Ab-Werk-Preis), dann ist der Ursprung CH

*ODER: Der höchste «Eigenfertigungsanteil» des jeweiligen Landes bestimmt den Warenursprung.

*ODER: Ohne Eigenfertigungsanteil bestimmt der höchste ausländische Wert den Warenursprung.


*
*
*-Tabellen-------------------------------------------------------------*
*tables: marc, keko, a504, konp, vbap, zsd_herkl.
tables: marc, keko, zsd_herkl, t024d, mape,ZLO_URSPR_AUSN .

*-Selektionen----------------------------------------------------------*
selection-screen begin of block b1 with frame title text-t01.
select-options: s_matnr for marc-matnr obligatory memory id mat.
parameters: p_werks like marc-werks obligatory memory id wrk.
selection-screen end of block b1.
selection-screen begin of block b2 with frame title text-t02.
parameters: p_sttag like sy-datum obligatory default sy-datum.
selection-screen skip 1.
parameters: p_faktor(3) type p decimals 2 obligatory default '1.45'.
selection-screen end of block b2.
selection-screen begin of block b3 with frame title text-t03.
parameters: p_land as checkbox,
            p_detail as checkbox,
            p_praef as checkbox.
selection-screen skip 1.
parameters: p_landd as checkbox,
            p_tabup as checkbox,
            p_matup as checkbox.
selection-screen skip 1.
parameters: p_loekz as checkbox.
selection-screen skip 1.
parameters: p_ausn as checkbox.   "Ausnahmen ZLO_URSPR_AUSN aktiv
selection-screen end of block b3.

*-Interne Tabellen-----------------------------------------------------*
data: imarc like marc occurs 0 with header line.
data: ickis like ckis occurs 0 with header line.
data: icrhd like crhd occurs 0 with header line.
data: i005 like t005 occurs 0 with header line.
data: i005t like t005t occurs 0 with header line.
data: i604g like t604g occurs 0 with header line.
data: iherkl_ch like zsd_herkl_ch occurs 0 with header line.
data: w_proto like wrplchk_gs_proto occurs 0 with header line.
data: begin of htab occurs 0,
       sort,
       herkunft(3),
       fert_e like mbew-salk3,
       fert_f like mbew-salk3,
       material like mbew-salk3,
       gk like mbew-salk3,
       total like mbew-salk3,
      end of htab.
data: htab2 like htab occurs 0 with header line.
data: begin of kaltab occurs 0,
        matnr like marc-matnr,
        werks like marc-werks,
        menge like ckis-menge,
      end of kaltab.
data: begin of ptab occurs 0,
       gzolx like mape-gzolx,
       sort,
       herkunft(3),
       fert_e like mbew-salk3,
       fert_f like mbew-salk3,
       material like mbew-salk3,
       gk like mbew-salk3,
       gewinn like mbew-salk3,
      end of ptab.

*-Diverses-------------------------------------------------------------*
data: w_maktx like makt-maktx,
      w_ltext like ckit-ltext,
      w_herkl like marc-herkl,
      w_vpreis like mbew-stprs,
      w_faktor type p decimals 3,
      w_gewinn like mbew-salk3,
      w_gewinn2 like mbew-salk3,
      w_wert like mbew-salk3,
      w_twert like mbew-salk3,
      w_twert2 like mbew-salk3,
      w_proz(5) type p,
      w_proz_high(5) type p,
      w_meins like keko-meins,
      w_antfgk type p decimals 5,
      w_newmat,
      w_herkl_new like marc-herkl,
      w_stort_oben(2),
      w_stort_unten(2),
      w_hawa,
      w_write,
      w_prefe_old like mape-prefe,
      w_prefe_new like mape-prefe,
      w_counter type i,
      w_lines type p.

data: ivapma type vapma.

data: begin of fgk,
        ch like mbew-salk3,
        cz like mbew-salk3,
        tfgk like mbew-salk3,
      end of fgk.

ranges: r_gzolx for mape-gzolx.
ranges: r_ausn  for marc-matnr.

*-Int. Tabellen für Material-Update------------------------------------*
data: t_mara_ueb like mara_ueb occurs 0 with header line.
data: t_marc_ueb like marc_ueb occurs 0 with header line.
data: t_merrdat like merrdat occurs 0 with header line.
data: t_mfieldres like mfieldres occurs 0 with header line.
data: mape_ueb like mape occurs 0 with header line.

include: <icon>.

*----------------------------------------------------------------------*
* Verarbeitung                                                         *
*----------------------------------------------------------------------*
* Ausnahmen aus ZLO_URSPR_AUSN aufbauen (Material-Exklusion)
refresh r_ausn.
if p_ausn = 'X'.
  select matnr from zlo_urspr_ausn into table @data(lt_ausn)
         where werks = @p_werks.
  loop at lt_ausn into data(ls_ausn).
    r_ausn-sign   = 'I'.
    r_ausn-option = 'EQ'.
    r_ausn-low    = ls_ausn-matnr.
    append r_ausn.
  endloop.

  select * from marc into table imarc where matnr in s_matnr    "#EC CI_ALL_FIELDS_NEEDED
                                    and   matnr not in r_ausn  "Ausnahmen raus
                                    and   werks eq p_werks
                                    and   ( ( beskz = 'E' or beskz = 'X' ) or
                                          sobsl = '30' )
                                    and   sobsl ne '50'  ORDER BY PRIMARY KEY. "
else.
    select * from marc into table imarc where matnr in s_matnr    "#EC CI_ALL_FIELDS_NEEDED

                                    and   werks eq p_werks
                                    and   ( ( beskz = 'E' or beskz = 'X' ) or
                                          sobsl = '30' )
                                    and   sobsl ne '50'  ORDER BY PRIMARY KEY. "
  endif.




select * from crhd into table icrhd where arbpl like 'CZ%'.  "#EC CI_ALL_FIELDS_NEEDED "Tschechische Arbeitsplätze
select * from t005 into table i005.
select * from t005t into table i005t where spras = sy-langu.
select * from zsd_herkl_ch into table iherkl_ch.  "#EC CI_NOWHERE
select * from t604g into table i604g where land1 = 'CH'.
refresh r_gzolx.
loop at i604g.
  r_gzolx-sign = 'I'.
  r_gzolx-option = 'EQ'.
  r_gzolx-low = i604g-gzolx.
  append r_gzolx.
endloop.

loop at imarc.
  if p_loekz = ' '.
    check imarc-lvorm is initial.
  endif.
  if imarc-losgr is initial.
    imarc-losgr = 1.
  endif.
  refresh htab.
  refresh ptab.
  perform verkaufspreis.
  select single maktx into w_maktx from makt where matnr = imarc-matnr
                                   and   spras = sy-langu.
  clear keko.
  select * from keko where matnr = imarc-matnr    "#EC CI_ALL_FIELDS_NEEDED
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


  if p_detail = 'X'.
    summary.
    write: / text-011, p_sttag.
    write: / imarc-matnr,"#EC CI_FLDEXT_OK[2215424] "Added by NTT
     w_maktx,
             text-009, (8) keko-losgr unit keko-meins,
             text-010, imarc-stawn, imarc-herkl.
    uline.
  endif.



  refresh kaltab.
  w_faktor = 1.
  perform lesen_kalkulation.

  loop at kaltab.
    describe table kaltab lines w_lines.
    if w_lines gt 1000.
      summary.
      write: / imarc-matnr, "#EC CI_FLDEXT_OK[2215424] "Addedby NTT
       imarc-werks, 'Auflösung wird rekursiv - Abbruch'.
      uline.
      refresh htab.
      exit.
    endif.
    select * from keko where matnr = kaltab-matnr   "#EC CI_ALL_FIELDS_NEEDED
                       and   werks = kaltab-werks
                       and   kalka = '01'
                       and   kadky le p_sttag
                       and   tvers = 1
                       and   loekz = ' '
                       and   feh_sta ne 'KF'
                       order by primary key.
    endselect.
    perform standort using kaltab-matnr kaltab-werks w_stort_oben ' '.
    if p_detail = 'X'.
      uline.
      summary.
      write: / keko-kalnr, keko-matnr.  "#EC CI_FLDEXT_OK[2215424] "Addedby NTT
    endif.
    w_faktor = kaltab-menge / keko-losgr.
    perform lesen_kalkulation.
  endloop.

  if p_detail = 'X'.
    uline.
  endif.

  sort htab.
  htab2[] = htab[].
  loop at htab.

    at first.
      clear zsd_herkl.
      sum.
      w_twert = htab-total.
      w_wert = htab-total / imarc-losgr.
      clear w_gewinn.
      if w_hawa is initial.
        w_vpreis = w_wert * p_faktor.
      else.
*        w_vpreis = w_wert.
      endif.
      if w_vpreis gt w_wert.
        w_gewinn = ( w_vpreis - w_wert ) * imarc-losgr.
        w_twert2 = w_twert + w_gewinn.
      else.
        w_gewinn = 0.
        w_twert2 = w_twert.
      endif.
      w_gewinn2 = w_gewinn.
      w_newmat = 'X'.
      zsd_herkl-matnr = imarc-matnr.
      zsd_herkl-werks = imarc-werks.
      zsd_herkl-datum = p_sttag.
      zsd_herkl-stprs = w_wert.
      zsd_herkl-vpreis = w_vpreis.
      zsd_herkl-marge = w_gewinn / imarc-losgr.
    endat.

    w_proz = ( htab-fert_e + htab-fert_f + htab-material +
           htab-gk + w_gewinn ) / w_twert2 * 100.
    htab-fert_e = htab-fert_e / imarc-losgr.
    htab-fert_f = htab-fert_f / imarc-losgr.
    htab-material = htab-material / imarc-losgr.
    htab-gk = htab-gk / imarc-losgr.
    w_gewinn = w_gewinn / imarc-losgr.

    if w_newmat = 'X'.
      clear w_newmat.
      perform ermittlung_herkl_neu.


  if w_write = 'X' and
        p_detail is initial.
        summary.
        write: / text-011, p_sttag.
        write: / imarc-matnr, "#EC CI_FLDEXT_OK[2215424] "Addedby NTT
         w_maktx,
                 text-009, (8) imarc-losgr unit w_meins,
                 text-010, imarc-stawn, w_herkl_new.
        uline.
      endif.

      if w_write = 'X'.
        write: / text-000, w_twert, '       pro 1 :', (10) w_wert.
        if not w_vpreis is initial.
          write: text-007, (10) w_vpreis.
        endif.
        skip.
        summary.
        write: /14 text-005.
        detail.
      endif.
    endif.

    if w_write = 'X'.
      case htab-herkunft.
        when '1'.
          write: / text-001.
          zsd_herkl-eigen_ch = htab-fert_e.
          zsd_herkl-fremd_ch = htab-fert_f.
          zsd_herkl-mater_ch = htab-material.
          zsd_herkl-gmkst_ch = htab-gk.
          zsd_herkl-prozt_ch = w_proz.
        when '2'.
          write: / text-002.
          zsd_herkl-eigen_eu = htab-fert_e.
          zsd_herkl-fremd_eu = htab-fert_f.
          zsd_herkl-mater_eu = htab-material.
          zsd_herkl-gmkst_eu = htab-gk.
          zsd_herkl-prozt_eu = w_proz.
        when '3'.
          write: / text-003.
          zsd_herkl-eigen_ueb = htab-fert_e.
          zsd_herkl-fremd_ueb = htab-fert_f.
          zsd_herkl-mater_ueb = htab-material.
          zsd_herkl-gmkst_ueb = htab-gk.
          zsd_herkl-prozt_ueb = w_proz.
        when '4'.
          write: / text-004.
          zsd_herkl-eigen_und = htab-fert_e.
          zsd_herkl-fremd_und = htab-fert_f.
          zsd_herkl-mater_und = htab-material.
          zsd_herkl-gmkst_und = htab-gk.
          zsd_herkl-prozt_und = w_proz.
        when others.
          clear zsd_herkl.
          read table i005t with key land1 = htab-herkunft.
          if sy-subrc = 0.
            write: / i005t-landx.
          else.
            write: / text-004.
          endif.
      endcase.

      write: htab-fert_e,
             htab-fert_f,
             htab-material,
             htab-gk,
             w_gewinn no-zero,
             w_proz.

      loop at ptab.
        check ptab-herkunft = htab-herkunft.
        ptab-fert_e = htab-fert_e.
        ptab-fert_f = htab-fert_f.
        ptab-gk = htab-gk.
        ptab-gewinn  = w_gewinn.
        ptab-material = ptab-material / imarc-losgr.
        modify ptab.
      endloop.

      if not w_herkl_new is initial .
       " and w_herkl_new ne imarc-herkl.
        write: '-->', w_herkl_new.
        if p_matup = 'X' and
          imarc-herkl ne w_herkl_new.
          perform material_update.
        endif.
      endif.
      clear w_herkl_new.

      if p_tabup = 'X' and
        not zsd_herkl-matnr is initial.
        modify zsd_herkl.
      endif.
    endif.
    clear w_gewinn.

  endloop.

  if w_write = 'X'.
    w_twert2 = w_twert2 / imarc-losgr.
    sort ptab.
    loop at ptab.
      at new gzolx.
        skip 1.
        clear: w_prefe_old, w_prefe_new.
        select single prefe from mape into w_prefe_old where matnr = imarc-matnr
                                                       and   werks = imarc-werks
                                                       and   gzolx = ptab-gzolx.
        summary.
        write:  / text-008, ptab-gzolx, 130 w_prefe_old.
      endat.

      detail.
      read table i005t with key land1 = ptab-herkunft.
      if sy-subrc = 0.
        write: / i005t-landx.
      else.
        write: / text-004.
      endif.

      w_proz = ( ptab-fert_e + ptab-fert_f + ptab-material +
             ptab-gk + ptab-gewinn ) / w_twert2 * 100.

      write: ptab-fert_e,
             ptab-fert_f,
             ptab-material,
             ptab-gk,
             ptab-gewinn no-zero,
             w_proz.

      perform ermittlung_prefe_neu .

    endloop.

    if not mape_ueb[] is initial.
      call function 'ZLO_UPDATE_MAPE'
        tables
          mape_ueb = mape_ueb
          proto    = w_proto.

      skip 1.
      read table w_proto with key matnr = imarc-matnr.
      if sy-subrc = 0 and
        w_proto-msgty = 'S'.
        write: / 'Materialstamm geändert', icon_okay as icon.
      else.
        write: / 'MM02 gescheitert. Mappe ZOLL-PRAEF (SM35)', icon_failure as icon.
      endif.

      refresh mape_ueb.
    endif.

    uline.

  endif.
endloop.









*---------------------------------------------------------------------*
*       FORM lesen_kalkulation                                         *
*---------------------------------------------------------------------*
FORM lesen_kalkulation.

  DATA p_stawn_komp TYPE marc-stawn.  " STAWN der Komponente (nur Anzeige)

  REFRESH ickis.
  SELECT * FROM ckis INTO TABLE ickis
    WHERE lednr = '00'
      AND bzobj = '0'
      AND kalnr = keko-kalnr
      AND kalka = keko-kalka
      AND kadky = keko-kadky
      AND tvers = keko-tvers
      AND bwvar = keko-bwvar
    ORDER BY PRIMARY KEY.

  CLEAR fgk.

  LOOP AT ickis.
    CHECK ickis-wrtfw_kpf NE 0.

    IF p_detail = 'X'.
      SELECT SINGLE ltext INTO w_ltext
        FROM ckit
        WHERE lednr = ickis-lednr
          AND bzobj = ickis-bzobj
          AND kalnr = ickis-kalnr
          AND kalka = ickis-kalka
          AND kadky = ickis-kadky
          AND tvers = ickis-tvers
          AND bwvar = ickis-bwvar
          AND kkzma = ickis-kkzma
          AND posnr = ickis-posnr
          AND spras = sy-langu.
    ENDIF.

    ickis-wrtfw_kpf = ickis-wrtfw_kpf * w_faktor.

    CLEAR htab.
    CLEAR w_herkl.
    CLEAR p_stawn_komp.

    CASE ickis-typps.

      WHEN 'M' OR 'I'.
        SELECT SINGLE * FROM marc
          WHERE matnr = ickis-matnr
            AND werks = ickis-werks.

        " STAWN der Komponente für Anzeige merken (Aggregation nicht beeinflussen)
        p_stawn_komp = marc-stawn.
        IF p_stawn_komp IS INITIAL.
          p_stawn_komp = '---'.
        ENDIF.

        PERFORM standort USING marc-matnr marc-werks w_stort_unten ' '.
        IF ickis-ukaln IS INITIAL OR marc-beskz = 'F' OR w_stort_oben NE w_stort_unten.
          PERFORM herkunft USING marc-herkl.
          w_herkl       = marc-herkl.
          htab-material = ickis-wrtfw_kpf.
        ELSE.
          kaltab-matnr = ickis-matnr.
          kaltab-menge = ickis-menge * w_faktor.
          kaltab-werks = ickis-werks.
          APPEND kaltab.
          CLEAR ickis-wrtfw_kpf.
        ENDIF.

      WHEN 'E'.
        READ TABLE icrhd WITH KEY objty = 'A' objid = ickis-arbid.
        IF sy-subrc NE 0.
          PERFORM herkunft USING 'CH'.
          w_herkl  = 'CH'.
          fgk-ch   = fgk-ch  + ickis-wrtfw_kpf.
          fgk-tfgk = fgk-tfgk + ickis-wrtfw_kpf.
        ELSE.
          PERFORM herkunft USING 'CZ'.
          w_herkl  = 'CZ'.
          fgk-cz   = fgk-cz  + ickis-wrtfw_kpf.
          fgk-tfgk = fgk-tfgk + ickis-wrtfw_kpf.
        ENDIF.
        htab-fert_e = ickis-wrtfw_kpf.
        " Eigenleistung: keine Materialkomponente -> kein STAWN in Anzeige
        CLEAR p_stawn_komp.

      WHEN 'F'.
        SELECT SINGLE land1 INTO w_herkl FROM lfa1 WHERE lifnr = ickis-lifnr.
        PERFORM herkunft USING w_herkl.
        htab-fert_f = ickis-wrtfw_kpf.
        " Fremdleistung: keine Materialkomponente -> kein STAWN in Anzeige
        CLEAR p_stawn_komp.

      WHEN OTHERS.
        IF ickis-kstar = '0000093100'.         " Fertigungsgemeinkosten
          " Anteil CH
          w_antfgk   = fgk-ch / fgk-tfgk.
          w_herkl    = 'CH'.
          PERFORM herkunft USING w_herkl.
          htab-gk    = ickis-wrtfw_kpf * w_antfgk.
          htab-total = ickis-wrtfw_kpf * w_antfgk.
          IF p_detail = 'X'.
            WRITE: / ickis-typps, ickis-matnr, w_ltext, (2) space, htab-gk NO-ZERO, w_herkl.
          ENDIF.
          COLLECT htab.          " Aggregation wie im alten Programm

          " Anteil CZ (zweite Sammelbuchung)
          CLEAR htab.
          w_antfgk   = fgk-cz / fgk-tfgk.
          w_herkl    = 'CZ'.
          PERFORM herkunft USING w_herkl.
          htab-gk    = ickis-wrtfw_kpf * w_antfgk.
          ickis-wrtfw_kpf = htab-gk. " damit nachfolgend ggf. weiterverrechnet werden kann
          " Für GK gibt es keinen STAWN
          CLEAR p_stawn_komp.
        ELSE.
          w_herkl = 'CH'.
          PERFORM herkunft USING w_herkl.
          htab-gk  = ickis-wrtfw_kpf.
          " Für sonstige GK kein STAWN
          CLEAR p_stawn_komp.
        ENDIF.

    ENDCASE.

    IF ickis-wrtfw_kpf GT 0.
      htab-total = ickis-wrtfw_kpf.
      COLLECT htab.   " Aggregation NUR nach sort/herkunft (wie früher)

      IF p_detail = 'X'.
        " STAWN nur in der Anzeige zeigen, nicht in der Aggregation führen
        WRITE: / ickis-typps, ickis-matnr,
                 w_ltext, ickis-wrtfw_kpf NO-ZERO, w_herkl, p_stawn_komp.
        IF ickis-wrtfw_kpf = 0.
          WRITE: '--->', ickis-ukaln.
        ENDIF.
      ENDIF.

      IF p_land = 'X' AND p_praef = 'X' AND NOT ickis-matnr IS INITIAL.
        PERFORM ermitteln_praef.
      ENDIF.
    ENDIF.

  ENDLOOP.

  IF ickis[] IS INITIAL.
    IF p_detail = 'X'.
      WRITE: / 'Keine Erzeugniskalkulation durchgeführt!'.
    ENDIF.
  ENDIF.

ENDFORM.                    " lesen_kalkulation










*&---------------------------------------------------------------------*
*&      Form  verkaufspreis
*&---------------------------------------------------------------------*
form verkaufspreis.
*  if p_vpreis is initial.
*    select * from vapma into ivapma up to 1 rows
*                        where matnr = imarc-matnr
*                        and   trvog = '0'
*                        and   werks = p_werks
*                        and   audat le p_sttag
*                        order by audat descending.
*    endselect.
*    if sy-subrc = 0.
*      select single * from vbap where vbeln = ivapma-vbeln
*                                and   posnr = ivapma-posnr.
*
*      if sy-subrc = 0.
*        call function 'CONVERT_TO_LOCAL_CURRENCY'
*          exporting
*            date             = p_sttag
*           FOREIGN_CURRENCY = konp-konwa
*            foreign_currency = vbap-waerk
*           FOREIGN_AMOUNT   = konp-kbetr
*            foreign_amount   = vbap-netpr
*            local_currency   = 'CHF'
*            type_of_rate     = 'M   '
*          importing
*            local_amount     = konp-kbetr
*          exceptions
*            no_rate_found    = 1
*            overflow         = 2
*            no_factors_found = 3
*            no_spread_found  = 4
*            others           = 5.
*        w_vpreis = konp-kbetr.
*      endif.
*    endif.
*  else.
*    w_vpreis = p_vpreis.
*  endif.
endform.                    " verkaufspreis

*---------------------------------------------------------------------*
*       FORM herkunft                                                 *
*---------------------------------------------------------------------*
form herkunft using lnd.
  htab-sort = 'B'.
  if p_land = 'X'.
    htab-herkunft = lnd.
    if htab-herkunft = 'CH'.
      htab-sort = 'A'.
    endif.
  else.
    if lnd = 'CH'.
      htab-herkunft = '1'.
    else.
      if lnd is initial.
        htab-herkunft = '4'.
      else.
        read table i005 with key land1 = lnd.
        if i005-xegld is initial.
          htab-herkunft = '3'.
        else.
          htab-herkunft = '2'.
        endif.
      endif.
    endif.
  endif.
endform.                    "herkunft

*&---------------------------------------------------------------------*
*&      Form  ermittlung_herkl_neu
*&---------------------------------------------------------------------*
form ermittlung_herkl_neu .

  data: l_proz like w_proz.

  clear w_herkl_new.

  read table iherkl_ch with key stawn = imarc-stawn
                                gzolx = space.
  if sy-subrc ne 0.
    read table iherkl_ch with key stawn = 'DUMMY'.
  endif.
  if sy-subrc = 0 and
    w_proz gt iherkl_ch-prozch.
    w_herkl_new = 'CH'.
  endif.
*
*  Die Formel für l_proz berechnet einen Prozentsatz, der die Verteilung von verschiedenen
"  Kosten- und Wertkomponenten für ein bestimmtes Material oder Produkt repräsentiert.

*Die einzelnen Variablen könnten wie folgt interpretiert werden:
*
*htab2-fert_e: Die Kosten oder der Wert der Eigenfertigung des Materials oder Produkts.
*htab2-fert_f: Die Kosten oder der Wert der Fremdfertigung des Materials oder Produkts.
*htab2-material: Die Kosten oder der Wert des Materials selbst.
*htab2-gk: Gemeinkosten oder zusätzliche Kosten, die dem Material oder Produkt zugeordnet sind.
*w_gewinn2: Wahrscheinlich der Gewinn oder ein anderer Faktor, der dem Material oder Produkt zugeordnet ist.
*w_twert2: Ein Gesamtwert, der wahrscheinlich als Nenner dient, um den prozentualen Anteil der oben genannten Komponenten zu berechnen.
*Die Formel multipliziert das Ergebnis mit 100, um es als Prozentsatz darzustellen.

*  IF W_HERKL_NEW IS INITIAL AND
*    P_LAND = 'x'.
*    CLEAR W_PROZ_HIGH.
*    LOOP AT HTAB2.
*      L_PROZ = ( HTAB2-FERT_E + HTAB2-FERT_F + HTAB2-MATERIAL +
*             HTAB2-GK + W_GEWINN2 ) / W_TWERT2 * 100.
*      CLEAR W_GEWINN2.
*      CHECK: HTAB2-HERKUNFT NE 'ch' AND
*             NOT HTAB2-HERKUNFT IS INITIAL.
*      IF L_PROZ GE W_PROZ_HIGH.
*        W_PROZ_HIGH = L_PROZ.
*        W_HERKL_NEW = HTAB2-HERKUNFT.
*      ENDIF.
*    ENDLOOP.
*  ENDIF.



  IF w_herkl_new IS INITIAL AND p_land = 'X'.
  CLEAR w_proz_high. " Diese Variable wird nicht mehr benötigt
  CLEAR w_herkl_new. " Zur Sicherheit zurücksetzen
  DATA(l_highest_fert_e) = 0. " Neue lokale Variable für den höchsten Wert von htab2-fert_e

  LOOP AT htab2.
    " Entfernen der alten Logik für l_proz
    " Überprüfen, ob der aktuelle Wert von htab2-fert_e höher ist als der bisher höchste
    IF htab2-fert_e > l_highest_fert_e.
      l_highest_fert_e = htab2-fert_e.
      w_herkl_new = htab2-herkunft. " Speichern der zugehörigen Herkunft (wenn das noch erforderlich ist)
    ENDIF.
  ENDLOOP.
ENDIF.




  if p_landd is initial or             "Nicht nur mit Unterschied ausgeben
    imarc-herkl ne w_herkl_new or      "oder Herkunftsland ungleich
    imarc-herkl is initial or          "oder gar noch nicht gepflegt
    p_detail = 'X'.                    "oder mit Details
    w_write = 'X'.                     "Dann gib die Berechnung aus
  else.
    clear w_write.                     "Ansonsten keine Ausgabe nötig
  endif.
endform.                    " ermittlung_herkl_neu

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

  if p_hawa = 'X'.
    clear w_hawa.
    if t024d-dsnam cs 'Einkauf' or
      t024d-dsnam cs 'CZ'.
      w_hawa = 'X'.
    endif.
  endif.
endform.                    " STANDORT

*---------------------------------------------------------------------*
*       FORM ERMITTELN_PRAEF                                          *
*---------------------------------------------------------------------*
FORM ermitteln_praef.

  LOOP AT i604g.
    CLEAR mape.
    SELECT SINGLE * FROM mape
      WHERE matnr = ickis-matnr
        AND werks = ickis-werks
        AND gzolx = i604g-gzolx.

    CLEAR ptab.
    ptab-gzolx    = i604g-gzolx.
    ptab-herkunft = htab-herkunft.
    ptab-sort     = htab-sort.

    IF mape-prefe = 'E'.
      ptab-material = ickis-wrtfw_kpf.
      IF p_detail = 'X'.
        WRITE mape-gzolx.
      ENDIF.
    ENDIF.

    COLLECT ptab.   " Aggregation wie früher (ohne STAWN-Feld)

  ENDLOOP.

ENDFORM.                    " ermitteln_praef



*&---------------------------------------------------------------------*
*&      Form  material_update
*&---------------------------------------------------------------------*
form material_update .

  clear t_mara_ueb. refresh t_mara_ueb.
  clear t_marc_ueb.  refresh t_marc_ueb.
  clear t_mfieldres. refresh t_mfieldres.
  clear w_counter.

  w_counter = w_counter + 1.
  t_mara_ueb-mandt = sy-mandt.     "Tabelleneinträge für Material-
  t_mara_ueb-matnr = imarc-matnr.  "update via
  t_mara_ueb-tcode = 'MM02'.       "MATERIAL_MAINTAIN_DARK
  t_mara_ueb-tranc = w_counter.    "füllen
  append t_mara_ueb.
  t_marc_ueb-mandt = sy-mandt.
  t_marc_ueb-matnr = imarc-matnr.
  t_marc_ueb-werks = imarc-werks.
  t_marc_ueb-tranc = w_counter.
  if not w_herkl_new is initial.
    t_marc_ueb-herkl = w_herkl_new.
  else.
    t_mfieldres-fname = 'MARC-HERKL'.
    t_mfieldres-tranc = w_counter.   "Feld reseten
    append t_mfieldres.
  endif.
  append t_marc_ueb.

  call function 'MATERIAL_MAINTAIN_DARK'       "#EC CI_USAGE_OK[2438006]
    exporting
      sperrmodus      = 'E'
      p_kz_no_warn    = 'N'
      kz_prf          = ' '
      kz_aend         = 'X'
      kz_dispo        = 'X'
    tables
      amara_ueb       = t_mara_ueb
      amarc_ueb       = t_marc_ueb
      amfieldres      = t_mfieldres
      amerrdat        = t_merrdat
    exceptions
      kstatus_empty   = 1
      tkstatus_empty  = 2
      t130m_error     = 3
      internal_error  = 4
      too_many_errors = 5
      update_error    = 6
      others          = 7.

  if sy-subrc ne 0.
    summary.
    write: / sy-subrc, 'Problem bei MATERIAL_MAINTAIN_DARK'.
  endif.

  call function 'DEQUEUE_ALL'.

  delete t_merrdat where msgty ne 'E'
                   and   msgty ne 'A'.

  if not t_merrdat[] is initial.
    write: icon_failure as icon.
  else.
    write: icon_okay as icon.
  endif.

endform.                    "material_update

*&---------------------------------------------------------------------*
*&      Form  ERMITTLUNG_PREFE_NEU
*&---------------------------------------------------------------------*
form ermittlung_prefe_neu .
  if w_prefe_new is initial.
    w_prefe_new = 'D'.
    read table iherkl_ch with key stawn = imarc-stawn
                                  gzolx = ptab-gzolx.
    if sy-subrc ne 0.
      read table iherkl_ch with key stawn = 'DUMMY'.
    endif.
    if sy-subrc = 0 and
      w_proz ge iherkl_ch-prozch.
      w_prefe_new = 'C'.
    endif.

    if w_prefe_new ne w_prefe_old.
      write:  130 '-->', w_prefe_new.
      if p_matup = 'X'.
        mape_ueb-matnr = imarc-matnr.
        mape_ueb-werks = imarc-werks.
        mape_ueb-gzolx = ptab-gzolx.
        mape_ueb-prefe = w_prefe_new.
        append mape_ueb.
      endif.
    endif.
  endif.
endform.                    " ERMITTLUNG_PREFE_NEU
