function z_md04_next_operation.
*"----------------------------------------------------------------------
*"*"Lokale Schnittstelle:
*"  IMPORTING
*"     REFERENCE(I_AUFNR)
*"  EXPORTING
*"     REFERENCE(E_VORNR) LIKE  AFVC-VORNR
*"     REFERENCE(E_ARBPL) LIKE  CRHD-ARBPL
*"----------------------------------------------------------------------

  data: l_ist like afvv-lmnga,
        l_pln like afvv-mgvrg,
        l_aufpl like afko-aufpl.

  clear: e_vornr, e_arbpl.

  select single aufpl into l_aufpl from afko where aufnr = i_aufnr.
  check sy-subrc = 0.

  refresh: iafvc, iafvv.
  select * from afvc into table iafvc where aufpl = l_aufpl
                                      and   steus ne 'PP00'.
  select * from afvv into table iafvv where aufpl = l_aufpl.
  sort iafvc by vornr.
  loop at iafvc.
    read table iafvv with key aufpl = iafvc-aufpl
                              aplzl = iafvc-aplzl.
    l_ist = iafvv-lmnga + iafvv-xmnga.
    l_pln = iafvv-mgvrg + iafvv-asvrg.

    if l_ist ge l_pln.                                      "Vorgangsmenge erreicht
      delete iafvc.                                         "Vorgang fertig
    else.
      select count(*) from jest where objnr = iafvc-objnr   "Vorgang Endrückgemeldet?
                                and   ( stat = 'I0009' or
                                        stat = 'I0013' )    "oder gelöscht
                                and   inact = ' '.
      if sy-subrc = 0.
        delete iafvc.                                       "Vorgang fertig
      endif.
    endif.
  endloop.

  read table iafvc index 1.
  if sy-subrc = 0.
    e_vornr = iafvc-vornr.
    select single arbpl from crhd into e_arbpl where objty = 'A'
                                               and   objid = iafvc-arbid.
  endif.

endfunction.
