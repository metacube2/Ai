report z_peg_test.
* Pruefreport $TMP 2026-10-07: Bedarfsverursacher wie ZM_OFFENE_FAUF (Form BEDARFSVERURSACHER)
data: lt_plaf type standard table of plaf,
      ls_plaf type plaf,
      lt_mdrq type standard table of mdrq,
      ls_mdrq type mdrq,
      lt_mdps type standard table of mdps,
      l_anz   type i,
      l_nr    type mdps-del12.
select * from plaf into table lt_plaf up to 10 rows
  where matnr = '000000000000063500'.
loop at lt_plaf into ls_plaf.
  clear: lt_mdrq, lt_mdps.
  l_nr = ls_plaf-plnum.
  call function 'MD_PEGGING_NODIALOG'
    exporting
      edelkz = 'PA'
      edelnr = l_nr
      ematnr = ls_plaf-matnr
      ewerks = ls_plaf-plwrk
      eberid = ls_plaf-berid
    tables
      imdrqx = lt_mdrq
      emdpsx = lt_mdps
    exceptions
      error = 1 no_requirements_found = 2 order_not_found = 3 others = 4.
  describe table lt_mdrq lines l_anz.
  write: / ls_plaf-plnum, 'RC', sy-subrc, 'ANZ', l_anz.
  loop at lt_mdrq into ls_mdrq.
    write: / '   ', ls_mdrq-delkz, ls_mdrq-delnr, ls_mdrq-delps, ls_mdrq-delet,
             ls_mdrq-extra, ls_mdrq-berid, ls_mdrq-dat00, ls_mdrq-mng01.
  endloop.
endloop.
