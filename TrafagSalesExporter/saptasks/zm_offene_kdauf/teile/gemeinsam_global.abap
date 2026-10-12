
* Vergleichslauf 2026-10-12 (Claude): Laufzeitmessung und Export ins ABAP-Memory.
* Aendert keine Logik. P_VGL setzt nur ZM_OFFENE_KDAUF_CMP; dann gibt es keine
* ALV-Liste und keine Fortschreibung von ZPOWERBI_KDAUF.
DATA: gv_ts_start  TYPE timestampl,
      gv_ts_a      TYPE timestampl,
      gv_ts_b      TYPE timestampl,
      gv_rt0       TYPE i,
      gv_rt1       TYPE i,
      gv_n_ivbep   TYPE i,
      gv_t_gesamt   TYPE p LENGTH 16 DECIMALS 3,
      gv_t_ivbep    TYPE p LENGTH 16 DECIMALS 3,
      gv_t_uebauf   TYPE p LENGTH 16 DECIMALS 3,
      gv_t_prefetch TYPE p LENGTH 16 DECIMALS 3,
      gv_t_bereich  TYPE p LENGTH 16 DECIMALS 3,
      gv_t_fkdat    TYPE p LENGTH 16 DECIMALS 3,
      gv_t_bezug    TYPE p LENGTH 16 DECIMALS 3,
      gv_t_dispo    TYPE p LENGTH 16 DECIMALS 3,
      gv_t_conv     TYPE p LENGTH 16 DECIMALS 3,
      gv_n_mbew_mehrfach TYPE i,
      gv_n_flow_fm       TYPE i,
      gv_n_flow_ohne     TYPE i,
      gv_n_flow_leer     TYPE i,
      gv_t_par           TYPE p LENGTH 16 DECIMALS 3,
      gv_n_par_pakete    TYPE i,
      gv_n_par_fehler    TYPE i,
      gv_n_par_treffer   TYPE i,
      gv_n_bp_diff       TYPE i.
