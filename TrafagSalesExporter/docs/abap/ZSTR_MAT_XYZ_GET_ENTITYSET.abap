*&---------------------------------------------------------------------*
*& METHODENRUMPF fuer die GET_ENTITYSET-Methode eines NEUEN, eigenen
*& EntitySets fuer die XYZ-Klassifizierung (Gateway-Service
*& ZPOWERBI_EINKAUF_SRV).
*&
*& VERSION 2026-07-23. Quelle live an travp762 gefunden/verifiziert:
*&   - Tabelle ZCA_MAT_ABC_XYZ (transparent), Key MANDT/MATNR/WERKS
*&   - XYZ-Kennzeichen: Feld /ITS/CA_M_MAXYZ (CHAR 1, Datenelement
*&     /ITS/CA_MAT_ABC_MAXYZ_D), Werk 1100 mit X/Y/Z gefuellt.
*& XYZ ist KEIN SAP-Standard (Standard hat nur ABC = MARC-MAABC),
*& sondern ein Add-on im /ITS/-Namensraum - deshalb ein eigenes Set
*& statt Erweiterung eines Standard-Sets.
*&
*& WARUM EIGENES SET (nicht ins MARC-Set): MARCSet liest die Tabelle
*& MARC automatisch. ABC (MARC-MAABC) liegt dort schon richtig. XYZ liegt
*& in einer ANDEREN Tabelle; es ins MARC-Set zu holen wuerde ein
*& Ueberschreiben des Auto-Reads erfordern und das bestehende Maabc
*& gefaehrden. Ein eigenes, schlankes Set ist risikofrei; der C#-Loader
*& fuehrt ABC (MARCSet) und XYZ (dieses Set) ueber die Materialnummer
*& zusammen (gleiches Muster wie MARA001Set-Statusmap).
*&
*& SAP-ANLAGE (manuell, VOR dem Einfuegen dieses Rumpfs):
*&   1. SE11: Struktur ZSTR_MAT_XYZ anlegen mit den Komponenten
*&        MATNR  Datenelement MATNR
*&        WERKS  Datenelement WERKS_D
*&        MAXYZ  Datenelement /ITS/CA_MAT_ABC_MAXYZ_D   (das XYZ-Kennz.)
*&      Aktivieren.
*&   2. SEGW im Service ZPOWERBI_EINKAUF_SRV: neuen EntityType auf Basis
*&      der Struktur ZSTR_MAT_XYZ anlegen (Key: Matnr, Werks), EntitySet
*&      generieren, Service neu generieren. Der Set-Name haengt von der
*&      SEGW-Kuerzung ab (vermutlich ZSTR_MAT_XYZSet) - die C#-Seite
*&      loest ihn dynamisch auf, exakter Name egal.
*&   3. Diese GET_ENTITYSET-Methode im DPC_EXT redefinieren und den
*&      kompletten Block unten (METHOD ... ENDMETHOD.) hineinkopieren.
*&   4. Klasse aktivieren, /IWFND/CACHE_CLEANUP.
*&
*& OData-Nutzung:
*&   .../<Set>?$format=json                         -> alle klassifizierten Materialien
*&   .../<Set>?$filter=Werks eq '1100'              -> nur ein Werk
*&   .../<Set>?$filter=Matnr eq '2217'              -> Einzelmaterial (kurz ODER 18-stellig)
*&---------------------------------------------------------------------*

METHOD zstr_mat_xyz_get_entityset.

    " ===================================================================
    " Lokale Typen / Arbeitsvariablen
    " ===================================================================
    TYPES: BEGIN OF ty_xyz,
             matnr TYPE matnr,
             werks TYPE werks_d,
             maxyz TYPE /its/ca_mat_abc_maxyz_d,
           END OF ty_xyz.

    DATA: lt_rows     TYPE STANDARD TABLE OF ty_xyz,
          ls_out      TYPE zstr_mat_xyz,
          lt_r_matnr  TYPE RANGE OF matnr,
          lt_r_werks  TYPE RANGE OF werks_d.

    " ===================================================================
    " Schritt 0: OData-$filter auslesen (Matnr, Werks). Beide optional.
    " Matnr wie in den ZLO03-Methoden robust behandeln: Rohwert IMMER
    " aufnehmen (App schickt bereits 18-stellig gepaddet) PLUS die
    " MATN1-konvertierte Form fuer kurze manuelle Eingaben - ZCA_MAT_ABC_XYZ
    " speichert MATNR intern zero-padded, die reine Kurzform ("2217")
    " wuerde sonst nicht matchen.
    " ===================================================================
    LOOP AT it_filter_select_options INTO DATA(ls_filter).
      CASE to_upper( ls_filter-property ).
        WHEN 'MATNR'.
          LOOP AT ls_filter-select_options INTO DATA(ls_so).
            DATA lv_m1_low  TYPE matnr.
            DATA lv_m1_high TYPE matnr.

            APPEND VALUE #( sign   = ls_so-sign
                            option = ls_so-option
                            low    = ls_so-low
                            high   = ls_so-high ) TO lt_r_matnr.

            CLEAR: lv_m1_low, lv_m1_high.
            IF ls_so-low IS NOT INITIAL.
              CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
                EXPORTING input        = ls_so-low
                IMPORTING output       = lv_m1_low
                EXCEPTIONS length_error = 1
                           OTHERS       = 2.
              IF sy-subrc <> 0.
                lv_m1_low = ls_so-low.
              ENDIF.
            ENDIF.
            IF ls_so-high IS NOT INITIAL.
              CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
                EXPORTING input        = ls_so-high
                IMPORTING output       = lv_m1_high
                EXCEPTIONS length_error = 1
                           OTHERS       = 2.
              IF sy-subrc <> 0.
                lv_m1_high = ls_so-high.
              ENDIF.
            ENDIF.
            IF lv_m1_low <> ls_so-low OR lv_m1_high <> ls_so-high.
              APPEND VALUE #( sign   = ls_so-sign
                              option = ls_so-option
                              low    = lv_m1_low
                              high   = lv_m1_high ) TO lt_r_matnr.
            ENDIF.
          ENDLOOP.
        WHEN 'WERKS'.
          LOOP AT ls_filter-select_options INTO DATA(ls_so_w).
            APPEND VALUE #( sign   = ls_so_w-sign
                            option = ls_so_w-option
                            low    = ls_so_w-low
                            high   = ls_so_w-high ) TO lt_r_werks.
          ENDLOOP.
      ENDCASE.
    ENDLOOP.

    " ===================================================================
    " Schritt 1: Tabelle lesen. Feld /ITS/CA_M_MAXYZ auf maxyz mappen.
    " Beide RANGEs sind leer, wenn nicht gefiltert -> IN trifft dann alles.
    " ===================================================================
    SELECT matnr, werks, /its/ca_m_maxyz AS maxyz
      FROM zca_mat_abc_xyz
      INTO CORRESPONDING FIELDS OF TABLE @lt_rows
      WHERE matnr IN @lt_r_matnr
        AND werks IN @lt_r_werks.

    SORT lt_rows BY matnr werks.

    " ===================================================================
    " Schritt 2: $skip/$top anwenden, dann in et_entityset uebertragen.
    " ===================================================================
    IF is_paging-skip > 0.
      DELETE lt_rows TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      DELETE lt_rows FROM is_paging-top + 1.
    ENDIF.

    LOOP AT lt_rows INTO DATA(ls_row).
      CLEAR ls_out.
      ls_out-matnr = ls_row-matnr.
      ls_out-werks = ls_row-werks.
      ls_out-maxyz = ls_row-maxyz.
      APPEND INITIAL LINE TO et_entityset ASSIGNING FIELD-SYMBOL(<fs>).
      MOVE-CORRESPONDING ls_out TO <fs>.
    ENDLOOP.

ENDMETHOD.
