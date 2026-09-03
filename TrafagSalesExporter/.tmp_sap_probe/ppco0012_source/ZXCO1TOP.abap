*----------------------------------------------------------------------*
*   INCLUDE ZXCO1TOP                                                   *
*----------------------------------------------------------------------*
TABLES: marc, mard, zmat_pverg, *marc, vbap.
TABLES: plko, crhd, t024d, pkps.
TABLES: zcfauf_dru, zcfauf_sg, zcfauf_gltrp.
tables: caufv, *caufv, caufvd, ci_aufk.

DATA: insel(10).

CONSTANTS: h_lgort_ch LIKE mard-lgort VALUE '0001',   "Hauptlager CH
           p_lgort_ch LIKE mard-lgort VALUE '0002',   "Prod.-Lager CH
           h_lgort_cz LIKE mard-lgort VALUE '0003',   "Hauptlager CZ
           p_lgort_cz LIKE mard-lgort VALUE '0004'.   "Prod.-Lager CZ

DATA: h_lgort     LIKE mard-lgort,
      h_lgort_alt LIKE mard-lgort,
      p_lgort     LIKE mard-lgort.

DATA: seltab LIKE rsparams OCCURS 0 WITH HEADER LINE.

DATA: zprdat LIKE aufk-zzprdat.  "Produktionsdatum

DATA: gs_aufk TYPE ci_aufk.
