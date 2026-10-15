
  " Materialanteil 2026-10-15
  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'MATPREIS'.
  ls_fieldcat-seltext_l = 'Materialanteil je Einheit'.
  ls_fieldcat-seltext_m = 'Mat.anteil/E'.
  ls_fieldcat-seltext_s = 'MatE'.
  ls_fieldcat-no_zero   = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.

  CLEAR ls_fieldcat.
  ls_fieldcat-fieldname = 'MATANTEIL'.
  ls_fieldcat-seltext_l = 'Materialanteil'.
  ls_fieldcat-seltext_m = 'Mat.anteil'.
  ls_fieldcat-seltext_s = 'MatA'.
  ls_fieldcat-no_zero   = 'X'.
  ls_fieldcat-do_sum    = 'X'.
  APPEND ls_fieldcat TO e01_lt_fieldcat.
