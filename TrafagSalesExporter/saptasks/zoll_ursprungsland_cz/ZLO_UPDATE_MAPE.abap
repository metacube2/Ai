function zlo_update_mape.
*"----------------------------------------------------------------------
*"*"Lokale Schnittstelle:
*"  TABLES
*"      MAPE_UEB STRUCTURE  MAPE
*"      PROTO STRUCTURE  WRPLCHK_GS_PROTO
*"----------------------------------------------------------------------

  refresh proto.

  write sy-datum to w_datum dd/mm/yy.
  select * from t604g into table i604g where land1 = 'CH' order by primary key.

  sort mape_ueb.
  clear w_open.
  loop at mape_ueb.

    at new werks.
      refresh imape.
    endat.

    clear w_prefe.
    clear w_mape.
    select single * from mape into w_mape where matnr = mape_ueb-matnr
                                   and   werks = mape_ueb-werks
                                   and   gzolx = mape_ueb-gzolx.
    if ( w_mape-prefe ne mape_ueb-prefe and not mape_ueb-prefe is initial ) or
      ( w_mape-preda ne mape_ueb-preda and not mape_ueb-preda is initial ) or
      ( w_mape-prene ne mape_ueb-prene and not mape_ueb-prene is initial ) or
      ( w_mape-preng ne mape_ueb-preng and not mape_ueb-preng is initial ).
      imape = mape_ueb.
      append imape.
    endif.

    at end of werks.
      if not imape[] is initial.
        refresh bdcdata.
        perform transaktion_mm02.
        clear proto.
        proto-matnr = w_mape-matnr.
        if w_error is initial.
          proto-msgty = 'S'.
        else.
          proto-msgty = 'E'.
        endif.
        append proto.
      endif.
    endat.

  endloop.

  perform close_group.

endfunction.
