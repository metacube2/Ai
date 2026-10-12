          " OPT 2026-10-12: war je Zeile SELECT SINGLE * FROM vbak (mit kunnr/ernam/erdat/vbtyp)
          " und SELECT COUNT(*) FROM kna1 (werks, name1). Jetzt dieselben Bedingungen einmal fuer
          " alle Belege in der Datenbank; die Schleife loescht wie bisher, in derselben Reihenfolge.
          DATA: lt_o_vbeln   TYPE STANDARD TABLE OF ty_o_vbak_ok,
                lt_o_vbak_ok TYPE HASHED TABLE OF ty_o_vbak_ok WITH UNIQUE KEY vbeln,
                lt_o_kunnr   TYPE STANDARD TABLE OF ty_o_vbak_ok,
                lt_o_kna1_ok TYPE HASHED TABLE OF kunnr WITH UNIQUE KEY table_line,
                ls_o_vbak_ok TYPE ty_o_vbak_ok.

          LOOP AT ivbep[] INTO DATA(ls_o_ep).
            APPEND VALUE #( vbeln = ls_o_ep-vbeln ) TO lt_o_vbeln.
          ENDLOOP.
          SORT lt_o_vbeln BY vbeln.
          DELETE ADJACENT DUPLICATES FROM lt_o_vbeln COMPARING vbeln.
          IF lt_o_vbeln IS NOT INITIAL.
            SELECT vbeln kunnr FROM vbak INTO TABLE lt_o_vbak_ok
              FOR ALL ENTRIES IN lt_o_vbeln
              WHERE vbeln = lt_o_vbeln-vbeln
              AND   kunnr IN s_kunde
              AND   ernam IN s_ernam
              AND   erdat IN s_erdat
              AND   ( vbtyp = 'C' OR vbtyp = 'E' ).
          ENDIF.
          IF NOT s_name1[] IS INITIAL AND lt_o_vbak_ok IS NOT INITIAL.
            lt_o_kunnr = lt_o_vbak_ok.
            SORT lt_o_kunnr BY kunnr.
            DELETE ADJACENT DUPLICATES FROM lt_o_kunnr COMPARING kunnr.
            SELECT kunnr FROM kna1 INTO TABLE lt_o_kna1_ok
              FOR ALL ENTRIES IN lt_o_kunnr
              WHERE kunnr = lt_o_kunnr-kunnr
              AND   werks = p_werk
              AND   name1 IN s_name1.
          ENDIF.

          LOOP AT ivbep.
            READ TABLE lt_o_vbak_ok INTO ls_o_vbak_ok WITH TABLE KEY vbeln = ivbep-vbeln.
            IF sy-subrc NE 0.
              DELETE ivbep.
            ELSE.
              IF NOT s_name1[] IS INITIAL.
                READ TABLE lt_o_kna1_ok WITH TABLE KEY table_line = ls_o_vbak_ok-kunnr
                     TRANSPORTING NO FIELDS.
                IF sy-subrc NE 0.
                  DELETE ivbep.
                ENDIF.
              ENDIF.
            ENDIF.
          ENDLOOP.
