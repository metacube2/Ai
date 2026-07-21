*&---------------------------------------------------------------------*
*& METHODENRUMPF fuer die redefinierte DPC_EXT-Methode des
*& Parent-EntitySets (ZSTR_LZCODE_PARENT) im Service ZPOWERBI_EINKAUF_SRV.
*& Der generierte Methodenname haengt von der SEGW-Kuerzung ab -
*& vermutlich ZSTR_LZCODE_PARE_GET_ENTITYSET (30-Zeichen-Limit, analog
*& ZSTR_LZCODE_USAG_GET_ENTITYSET) - bitte am generierten Stub pruefen.
*&
*& EINFUEGEN (wichtig, Fehlerquelle 2026-07-21): Den KOMPLETTEN Block
*& unten INKLUSIVE der Zeilen "METHOD ..." und "ENDMETHOD." nehmen und
*& damit den generierten Stub 1:1 ersetzen - im Editor alles von
*& "method zstr_lzcode_pare_get_entityset." bis zum zugehoerigen
*& "endmethod." markieren und ueberschreiben. Landet der Rumpf ausserhalb
*& des METHOD-Rahmens, meldet der Syntaxcheck je Zeile "Zwischen CLASS
*& ... IMPLEMENTATION und ENDCLASS duerfen nur Methoden definiert
*& werden". Keine CLASS-Statements enthalten.
*& Entspricht ZCL_LZCODE_PROVIDER=>GET_PARENT_MATERIALS
*& bzw. FORM load_elternmaterial_cache/get_elternmaterial in zlo03.txt,
*& aber deterministisch (SORT + DELETE ADJACENT DUPLICATES statt
*& HASHED-TABLE-Reihenfolge).
*&
*& DRAFT 2026-07-21 - Syntaxcheck im System offen.
*&
*& EINSCHRAENKUNG (bewusst, dokumentiert): Der Original-Report begrenzt
*& die Eltern auf die im Hauptlauf selektierten VKNRs (zat_vc-matnr IN
*& Selektion). Dieses Entity hat aber kein Vknr-Property - ueber OData
*& ist nur nach Kompnr filterbar. Ergebnis kann daher MEHR Eltern-Zeilen
*& enthalten als das Excel des Reports (Eltern aus ALLEN Verwendungen).
*& Der Client (C#-Seite) muss bei Bedarf selbst auf seine Vknr-Menge
*& einschraenken.
*&
*& OData-Nutzung:
*&   .../<EntitySet>?$filter=Kompnr eq 'R85012'
*&---------------------------------------------------------------------*

METHOD zstr_lzcode_pare_get_entityset.

    DATA: lt_out      TYPE STANDARD TABLE OF zstr_lzcode_parent,
          ls_out      TYPE zstr_lzcode_parent,
          lt_r_kompnr TYPE RANGE OF matnr.

    " OData-$filter auslesen: nur Kompnr wird unterstuetzt
    LOOP AT it_filter_select_options INTO DATA(ls_filter).
      IF to_upper( ls_filter-property ) = 'KOMPNR'.
        LOOP AT ls_filter-select_options INTO DATA(ls_so).
          APPEND VALUE #( sign   = ls_so-sign
                          option = ls_so-option
                          low    = ls_so-low
                          high   = ls_so-high ) TO lt_r_kompnr.
        ENDLOOP.
      ENDIF.
    ENDLOOP.

    " Ohne Kompnr-Filter abbrechen - sonst Vollselektion auf ZAT_VC
    IF lt_r_kompnr IS INITIAL.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = 'Filter Kompnr angeben (z.B. $filter=Kompnr eq ''R85012'')'.
    ENDIF.

    " KOM_MSTAE ist trotz des Namens ein Elternmaterial (MATNR-Feld,
    " live verifiziert 2026-07-21, siehe README Live-Verifikation).
    SELECT kompnr, kom_mstae AS eltern_matnr
      FROM zat_vc
      INTO CORRESPONDING FIELDS OF TABLE @lt_out
      WHERE kompnr IN @lt_r_kompnr
        AND kom_mstae <> @space.

    SORT lt_out BY kompnr eltern_matnr.
    DELETE ADJACENT DUPLICATES FROM lt_out COMPARING kompnr eltern_matnr.

    " $skip/$top anwenden, dann in et_entityset uebertragen
    IF is_paging-skip > 0.
      DELETE lt_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      DELETE lt_out FROM is_paging-top + 1.
    ENDIF.

    LOOP AT lt_out INTO ls_out.
      APPEND INITIAL LINE TO et_entityset ASSIGNING FIELD-SYMBOL(<fs_entity>).
      MOVE-CORRESPONDING ls_out TO <fs_entity>.
    ENDLOOP.

ENDMETHOD.
