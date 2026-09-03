*----------------------------------------------------------------------*
***INCLUDE ZXCO1F02 .
*----------------------------------------------------------------------*

*&---------------------------------------------------------------------*
*&      Form  CALL_TRANSACTION_PKBC
*&---------------------------------------------------------------------*
form call_transaction_pkbc using p_pk.
  refresh seltab.
  clear seltab.
  seltab-selname = 'P_PK'.
  seltab-kind = 'P'.
  seltab-low = p_pk.
  append seltab.

  call function 'ZZ_JOB_SUBMIT' in background task
    exporting
      jobname   = 'ZZ_KANBAN_AUS_CO11N'
      repname   = 'ZPP_PKBC'
      strtimmed = 'X'
    tables
      seltab    = seltab.

endform.                    " CALL_TRANSACTION_PKBC
