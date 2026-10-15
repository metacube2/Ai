* Materialanteil 2026-10-15: Material je Basiseinheit, einmal je Kalkulation gerechnet.
TYPES: BEGIN OF ty_mata_kalk,
         kalnr TYPE keko-kalnr,
         kalka TYPE keko-kalka,
         kadky TYPE keko-kadky,
         tvers TYPE keko-tvers,
         bwvar TYPE keko-bwvar,
         wert  TYPE p LENGTH 16 DECIMALS 6,
       END OF ty_mata_kalk,
       BEGIN OF ty_mata_mat,
         matnr TYPE matnr,
         werks TYPE werks_d,
         wert  TYPE p LENGTH 16 DECIMALS 6,
         ok    TYPE abap_bool,
       END OF ty_mata_mat.
DATA: gt_mata_kalk  TYPE HASHED TABLE OF ty_mata_kalk WITH UNIQUE KEY kalnr kalka kadky tvers bwvar,
      gt_mata_mat   TYPE HASHED TABLE OF ty_mata_mat WITH UNIQUE KEY matnr werks,
      gv_mata_tiefe TYPE i,
      gv_n_mata_ohne TYPE i,   " Materialien ohne passende Kalkulation
      gv_n_mata_gross TYPE i.  " Positionen mit Materialanteil > Standardwert
