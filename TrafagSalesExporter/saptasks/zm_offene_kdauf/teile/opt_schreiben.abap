  " 2. Daten aus gt_uebauf in die neue Struktur mappen und ID generieren
  " OPT 2026-10-12 (Entscheid Ingo): alle IDs mit einem Sammelaufruf statt je Zeile.
  " NUMBER_GET_NEXT liefert bei QUANTITY > 1 die LETZTE Nummer des Blocks (in T76
  " gemessen: 25 angefordert -> 98241..98265). Liefert er weniger als angefordert,
  " wird der Rest nachgefordert. Die Nummern im Hauptspeicherpuffer werden dabei
  " uebersprungen; Luecken im Nummernkreis sind zulaessig.
  DATA: lt_o_ids    TYPE STANDARD TABLE OF zpowerbi_kdauf-id,
        lv_o_anz    TYPE inri-quantity,
        lv_o_letzte TYPE n LENGTH 10,
        lv_o_nr10   TYPE n LENGTH 10,
        lv_o_wert   TYPE p LENGTH 12,
        lv_o_soll   TYPE i,
        lv_o_id     TYPE zpowerbi_kdauf-id.

  lv_o_soll = lines( gt_uebauf ).
  WHILE lines( lt_o_ids ) < lv_o_soll.
    lv_o_anz = lv_o_soll - lines( lt_o_ids ).
    CALL FUNCTION 'NUMBER_GET_NEXT'
      EXPORTING
        nr_range_nr = gc_nr_range_kdaufid
        object      = gc_nr_object_kdaufid
        quantity    = lv_o_anz
      IMPORTING
        number      = lv_o_letzte
        quantity    = lv_o_anz
      EXCEPTIONS
        interval_not_found        = 1
        number_range_not_intern   = 2
        object_not_found          = 3
        quantity_is_0             = 4
        quantity_is_not_1         = 5
        interval_overflow         = 6
        buffer_overflow           = 7
        OTHERS                    = 8.

    IF sy-subrc <> 0 OR lv_o_anz <= 0.
      MESSAGE |Fehler beim Abrufen der nächsten ID für ZPOWERBI_KDAUF (Obj: { gc_nr_object_kdaufid } Int: { gc_nr_range_kdaufid }). SY-SUBRC: { sy-subrc }| TYPE 'E'.
      ROLLBACK WORK.
      RETURN.
    ENDIF.
    " Block erste .. letzte; Umweg ueber NUMC 10 wie bisher (NUMBER -> ID)
    lv_o_wert = lv_o_letzte - lv_o_anz + 1.
    DO lv_o_anz TIMES.
      lv_o_nr10 = lv_o_wert.
      lv_o_id   = lv_o_nr10.
      APPEND lv_o_id TO lt_o_ids.
      lv_o_wert = lv_o_wert + 1.
    ENDDO.
  ENDWHILE.

  LOOP AT gt_uebauf INTO DATA(ls_uebauf).
    CLEAR: ls_zpowerbi_kdauf, lv_next_id.
    READ TABLE lt_o_ids INTO lv_next_id INDEX sy-tabix.

    ls_zpowerbi_kdauf-id     = lv_next_id. " NEU: Generierte ID
    ls_zpowerbi_kdauf-ketdat = ls_uebauf-ketdat.
    ls_zpowerbi_kdauf-vbeln  = ls_uebauf-vbeln.
    ls_zpowerbi_kdauf-posnr  = ls_uebauf-posnr.
    ls_zpowerbi_kdauf-matnr  = ls_uebauf-matnr.
    ls_zpowerbi_kdauf-fkdat  = ls_uebauf-fkdat.
    ls_zpowerbi_kdauf-kunnr  = ls_uebauf-kunnr.
    ls_zpowerbi_kdauf-gltrp  = ls_uebauf-gltrp.
    ls_zpowerbi_kdauf-gltri  = ls_uebauf-gltri.
    ls_zpowerbi_kdauf-edatu  = ls_uebauf-edatu.
    " MANDT wird automatisch gefüllt

    APPEND ls_zpowerbi_kdauf TO lt_zpowerbi_kdauf.
  ENDLOOP.
