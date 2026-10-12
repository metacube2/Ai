                          " Korrektur: die Zeile ist geloescht; ohne CONTINUE liefe fuer sie
                          " gleich noch ein zweites DELETE (Ergebnis dann undefiniert)
                          IF p_kalt = abap_false.
                            CONTINUE.
                          ENDIF.
