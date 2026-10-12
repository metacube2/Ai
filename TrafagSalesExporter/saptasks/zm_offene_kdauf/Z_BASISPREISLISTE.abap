FUNCTION Z_BASISPREISLISTE.
*"----------------------------------------------------------------------
*"*"Lokale Schnittstelle:
*"  IMPORTING
*"     VALUE(I_MATNR) LIKE  MARA-MATNR OPTIONAL
*"     VALUE(I_TYPCD) LIKE  MARA-ZZTYP OPTIONAL
*"     VALUE(I_LANGU) LIKE  SY-LANGU OPTIONAL
*"     VALUE(I_LAENGE) TYPE  ZZLAENGE OPTIONAL
*"     VALUE(I_LAENGE2) TYPE  ZZLAENGE2 OPTIONAL
*"     VALUE(I_MIT_LOE) TYPE  CHAR1 OPTIONAL
*"  EXPORTING
*"     VALUE(E_TYPCD) LIKE  MARA-ZZTYP
*"  TABLES
*"      BPTAB STRUCTURE  ZZBPTAB
*"  EXCEPTIONS
*"      NO_OBJECT
*"      NO_TYPCD
*"----------------------------------------------------------------------
  if i_matnr is initial and
    i_typcd is initial.
    raise no_object.
  endif.

  if not i_typcd is initial.
    e_typcd = i_typcd.
    if not i_matnr is initial.
*{   REPLACE        Q76K900036                                        1
*\      select single * from mara where matnr = i_matnr.
      select single * from mara where matnr = i_matnr. "#EC CI_ALL_FIELDS_NEEDED
*}   REPLACE
    endif.
  else.
*{   REPLACE        Q76K900036                                        2
*\    select single * from mara where matnr = i_matnr.
    select single * from mara where matnr = i_matnr.    "#EC CI_ALL_FIELDS_NEEDED
*}   REPLACE
    if sy-subrc ne 0 or
      mara-zztyp is initial.
      raise no_typcd.
    else.
      e_typcd = mara-zztyp.
    endif.
  endif.

  if i_langu is initial.
    zwspras = sy-langu.
  else.
    zwspras = i_langu.
  endif.

  refresh: bptab, zwbptab.
  laenge = i_laenge.
  laenge2 = i_laenge2.
  mit_loe = i_mit_loe.
  perform fill_bptab using e_typcd.
  bptab[] = zwbptab[].

ENDFUNCTION.
