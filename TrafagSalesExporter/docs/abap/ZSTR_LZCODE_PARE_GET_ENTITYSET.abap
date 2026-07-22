*&---------------------------------------------------------------------*
*& METHODENRUMPF fuer die redefinierte DPC_EXT-Methode des
*& Parent-EntitySets (ZSTR_LZCODE_PARENT) im Service ZPOWERBI_EINKAUF_SRV.
*& Der generierte Methodenname haengt von der SEGW-Kuerzung ab -
*& vermutlich ZSTR_LZCODE_PARE_GET_ENTITYSET (30-Zeichen-Limit, analog
*& ZSTR_LZCODE_USAG_GET_ENTITYSET) - bitte am generierten Stub pruefen.
*&
*& VERSION 2026-07-22c - ALPHA-Konvertierung fuer Kompnr-Filter ergaenzt.
*& Bestaetigter Befund (Vergleichstest gleicher Materialnummer): $filter
*& mit Kurzform ("2217") lieferte 0 Zeilen, dieselbe Nummer in
*& 18-stelliger Form ("000000000000002217") lieferte echte Daten. Eine
*& selbstgeschriebene GET_ENTITYSET-Methode bekommt
*& it_filter_select_options ROH (ohne die sonst automatische externe->
*& interne MATNR-Konvertierung); MARA/ZPOWERBI_VC_TXT speichern intern
*& padded. Ab dieser Version werden Low/High-Werte fuer Kompnr per
*& CONVERSION_EXIT_ALPHA_INPUT konvertiert, damit beide Schreibweisen
*& funktionieren.
*&
*& VERSION 2026-07-22a - an die NEUE Reportfassung angepasst
*& (Quelle: docs/abap/originalzlo03.txt, Report ZM_LZCODE20_OPT).
*& Die Vorfassung 2026-07-21 las noch aus ZAT_VC - diese Tabelle
*& existiert auf travp762 (PROD) nicht, wodurch die komplette
*& DPC_EXT-Klasse nicht kompilierte und JEDES EntitySet des Service
*& (auch EKKOSet) mit SYNTAX_ERROR abbrach (Befund 2026-07-22).
*& Einzige fachliche Aenderung: Quelltabelle ZAT_VC -> ZPOWERBI_VC_TXT.
*&
*& EINFUEGEN (wichtig, Fehlerquelle 2026-07-21): Den KOMPLETTEN Block
*& unten INKLUSIVE der Zeilen "METHOD ..." und "ENDMETHOD." nehmen und
*& damit den bestehenden Methodenrumpf 1:1 ersetzen. Keine
*& CLASS-Statements enthalten.
*&
*& NACH DEM ERSETZEN: Klasse aktivieren; danach Metadaten-Cache leeren
*& (/IWFND/CACHE_CLEANUP). AUF BEIDEN SYSTEMEN NACHZIEHEN (travt762
*& UND travp762), sonst bleibt der SYNTAX_ERROR auf P bestehen.
*&
*& Entspricht FORM load_elternmaterial_cache/get_elternmaterial in
*& originalzlo03.txt, aber deterministisch (SORT + DELETE ADJACENT
*& DUPLICATES statt HASHED-TABLE-Reihenfolge).
*&
*& EINSCHRAENKUNG (bewusst, dokumentiert): Der Original-Report begrenzt
*& die Eltern auf die im Hauptlauf selektierten VKNRs
*& (zpowerbi_vc_txt-matnr IN Selektion). Dieses Entity hat aber kein
*& Vknr-Property - ueber OData ist nur nach Kompnr filterbar. Ergebnis
*& kann daher MEHR Eltern-Zeilen enthalten als das Excel des Reports
*& (Eltern aus ALLEN Verwendungen). Der Client (C#-Seite) muss bei
*& Bedarf selbst auf seine Vknr-Menge einschraenken.
*&
*& OData-Nutzung:
*&   .../<EntitySet>?$filter=Kompnr eq 'R85012'
*&---------------------------------------------------------------------*

METHOD zstr_lzcode_pare_get_entityset.

    DATA: lt_out      TYPE STANDARD TABLE OF zstr_lzcode_parent,
          ls_out      TYPE zstr_lzcode_parent,
          lt_r_kompnr TYPE RANGE OF matnr.

    " OData-$filter auslesen: nur Kompnr wird unterstuetzt.
    " ALPHA-Konvertierung (Befund 2026-07-22c, siehe Header): roh
    " uebergebene Kurzform ("2217") matcht sonst nicht gegen die
    " intern padded gespeicherten Werte in ZPOWERBI_VC_TXT.
    LOOP AT it_filter_select_options INTO DATA(ls_filter).
      IF to_upper( ls_filter-property ) = 'KOMPNR'.
        LOOP AT ls_filter-select_options INTO DATA(ls_so).
          DATA lv_low  TYPE matnr.
          DATA lv_high TYPE matnr.
          CLEAR: lv_low, lv_high.
          IF ls_so-low IS NOT INITIAL.
            CALL FUNCTION 'CONVERSION_EXIT_ALPHA_INPUT'
              EXPORTING input  = ls_so-low
              IMPORTING output = lv_low.
          ENDIF.
          IF ls_so-high IS NOT INITIAL.
            CALL FUNCTION 'CONVERSION_EXIT_ALPHA_INPUT'
              EXPORTING input  = ls_so-high
              IMPORTING output = lv_high.
          ENDIF.
          APPEND VALUE #( sign   = ls_so-sign
                          option = ls_so-option
                          low    = lv_low
                          high   = lv_high ) TO lt_r_kompnr.
        ENDLOOP.
      ENDIF.
    ENDLOOP.

    " Ohne Kompnr-Filter abbrechen - sonst Vollselektion auf
    " ZPOWERBI_VC_TXT
    IF lt_r_kompnr IS INITIAL.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = 'Filter Kompnr angeben (z.B. $filter=Kompnr eq ''R85012'')'.
    ENDIF.

    " KOM_MSTAE ist trotz des Namens ein Elternmaterial (MATNR-Feld) -
    " so nutzt es auch der Report (load_elternmaterial_cache,
    " ausgabe_elternmaterial in originalzlo03.txt).
    SELECT kompnr, kom_mstae AS eltern_matnr
      FROM zpowerbi_vc_txt
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
