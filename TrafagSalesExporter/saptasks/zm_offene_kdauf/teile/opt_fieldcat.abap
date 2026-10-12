  " Korrektur 2026-10-12: NETWR gibt es in ZMM_UEB_KDAUF nicht. Im Hintergrund bricht die
  " ALV-Liste daran mit GETWA_NOT_ASSIGNED (SAPLKKBL, GEN_FIELD_OUT2) ab, noch bevor
  " ZPOWERBI_KDAUF fortgeschrieben wird.
  IF p_kalt = abap_false.
    DELETE e01_lt_fieldcat WHERE fieldname = 'NETWR'.
  ENDIF.
