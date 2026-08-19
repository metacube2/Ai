*&---------------------------------------------------------------------*
*& METHODENRUMPF fuer die GET_ENTITYSET-Methode eines NEUEN, eigenen
*& EntitySets fuer den LAGERWERT der Einkaufsteile
*& (Gateway-Service ZPOWERBI_EINKAUF_SRV).
*&
*& VERSION 2026-08-19. Grundlage: docs/EINKAUF_LAGERWERT_2026-08-18.md
*& und der bereits gelaufene Analysereport
*& docs/abap/Z_PURCHASING_LAGERWERT_ANALYSE.abap.
*&
*& ===================================================================
*& WARUM UEBERHAUPT EIN AGGREGIERENDES SET
*& ===================================================================
*& Der uebliche Weg im Einkauf ist "ein ungepagter Request, alles in den
*& Cache" (MARA001Set, MAKTSet, mbewSet). Fuer die Bewertungshistorie
*& MBEWH scheitert das messbar:
*&   - mbewSet kostet mit 68'543 Zeilen bereits 124 MB und 28 Sekunden
*&     je Aufruf (gemessen 2026-07-28).
*&   - MBEWH hat im Bewertungskreis 1100 allein 5'371'798 Zeilen, also
*&     rund das 78-fache. Fuer die 7'261 Zielmaterialien sind es immer
*&     noch 1'132'402 Saetze.
*& Ein Full Load ist damit ausgeschlossen. Dieses Set rechnet deshalb
*& SERVERSEITIG und liefert nur wenige aggregierte Zeilen:
*& je Bewertungskreis, Periode und Disponent eine.
*&
*& ===================================================================
*& DIE ENTSCHEIDENDE RECHENREGEL (nicht vereinfachen!)
*& ===================================================================
*& MBEWH enthaelt fuer eine Periode NUR die Materialien, bei denen es
*& danach eine bewertungsrelevante Bewegung gab. Ein seit Maerz
*& unbewegtes Material hat fuer 2026/04 KEINEN Historiensatz; sein Wert
*& steht unveraendert in MBEW.
*&
*& Korrekte Logik (so rechnet auch MB5L):
*&   je Material den MBEWH-Satz der KLEINSTEN Periode >= Stichtag nehmen;
*&   existiert keiner, gilt der aktuelle MBEW-Wert.
*&
*& WARUM DAS SO WICHTIG IST: Die naive Summe nur ueber MBEWH weicht ab
*&   - bei Stichtag 2024/06 um  0,08 %  -> voellig unauffaellig
*&   - bei Stichtag 2026/06 um   66 %   -> offensichtlich kaputt
*& Je aelter die Periode, desto dichter die Historie. Wer die Umsetzung
*& an einem alten Stichtag testet, sieht den Fehler NICHT und baut ihn
*& produktiv ein, wo er ausgerechnet den aktuellen Monat trifft.
*& => ABNAHME IMMER MIT EINEM JUNGEN STICHTAG.
*&
*& ===================================================================
*& SAP-ANLAGE (manuell, VOR dem Einfuegen dieses Rumpfs)
*& ===================================================================
*&   1. SE11: Struktur ZSTR_PURCH_STOCKVAL anlegen mit den Komponenten
*&        BWKEY    Datenelement BWKEY
*&        LFGJA    Datenelement LFGJA      " Jahr der Periode
*&        LFMON    Datenelement LFMON      " Monat der Periode
*&        DISPO    Datenelement DISPO      " Disponent aus MARC
*&        SALK3    Datenelement SALK3      " Bestandswert
*&        LBKUM    Datenelement LBKUM      " Bestandsmenge
*&        MATANZ   Datenelement INT4       " Anzahl Materialien
*&        AUSHIST  Datenelement INT4       " davon aus MBEWH
*&        AUSMBEW  Datenelement INT4       " davon aus MBEW-Fallback
*&        WAERS    Datenelement WAERS      " Hauswaehrung, siehe Hinweis
*&      Aktivieren.
*&   2. SEGW im Service ZPOWERBI_EINKAUF_SRV: neuen EntityType auf Basis
*&      der Struktur anlegen. KEY: Bwkey, Lfgja, Lfmon, Dispo.
*&      EntitySet generieren, Service neu generieren.
*&   3. Diese GET_ENTITYSET-Methode im DPC_EXT redefinieren und den
*&      kompletten Block unten hineinkopieren.
*&   4. Klasse aktivieren, /IWFND/CACHE_CLEANUP.
*&
*& HINWEIS zu WAERS: Die Hauswaehrung wird aus T001 ueber T001K gelesen.
*& T001K (BWKEY -> BUKRS) ist im Projekt belegt im Einsatz. T001-WAERS
*& ist beim ersten Lauf zu VERIFIZIEREN und nicht blind zu uebernehmen;
*& liefert es leer, bleibt das Feld leer statt geraten zu werden.
*& Bewertungskreis 1100 = CH, 1200 = AT (per T001K bestaetigt). Eine
*& Summe ueber beide waere wegen unterschiedlicher Hauswaehrung falsch.
*&
*& ===================================================================
*& OData-Nutzung
*& ===================================================================
*&   .../<Set>?$filter=Bwkey eq '1100'
*&        -> aktuelle Periode, alle Disponenten
*&   .../<Set>?$filter=Bwkey eq '1100' and Lfgja eq '2026' and Lfmon eq '06'
*&        -> genau eine Periode
*&   .../<Set>?$filter=Bwkey eq '1100' and Dispo eq '001'
*&        -> nur ein Disponent
*&
*& BEWUSSTE ENTSCHEIDUNG: Das Set liefert je DISPONENT eine Zeile und
*& summiert NICHT ueber die Disponenten 001-005. Grund: die fachliche
*& Frage, ob Disponent 004 (Betriebsmaterial) zum Lagerwert der
*& Einkaufsteile gehoert, ist bei Armin offen. Liefert SAP je Disponent,
*& kann die Abgrenzung in der Anwendung geaendert werden, ohne dass SAP
*& erneut angefasst und transportiert werden muss.
*&---------------------------------------------------------------------*

METHOD zstr_purch_stockval_get_entityset.

    " ===================================================================
    " Lokale Typen / Arbeitsvariablen
    " ===================================================================
    TYPES: BEGIN OF ty_zielmat,
             matnr TYPE matnr,
             dispo TYPE dispo,
           END OF ty_zielmat.

    TYPES: BEGIN OF ty_hist,
             matnr TYPE matnr,
             lfgja TYPE lfgja,
             lfmon TYPE lfmon,
             salk3 TYPE salk3,
             lbkum TYPE lbkum,
           END OF ty_hist.

    TYPES: BEGIN OF ty_mbew_idx,
             matnr TYPE matnr,
             salk3 TYPE salk3,
             lbkum TYPE lbkum,
           END OF ty_mbew_idx.

    TYPES: BEGIN OF ty_best,
             matnr TYPE matnr,
             perio TYPE i,
             salk3 TYPE salk3,
             lbkum TYPE lbkum,
           END OF ty_best.

    " Aggregationsschluessel: je Periode und Disponent eine Ausgabezeile.
    TYPES: BEGIN OF ty_agg,
             lfgja   TYPE lfgja,
             lfmon   TYPE lfmon,
             dispo   TYPE dispo,
             salk3   TYPE salk3,
             lbkum   TYPE lbkum,
             matanz  TYPE i,
             aushist TYPE i,
             ausmbew TYPE i,
           END OF ty_agg.

    DATA: lt_zielmat TYPE SORTED TABLE OF ty_zielmat WITH UNIQUE KEY matnr,
          ls_zielmat TYPE ty_zielmat,
          lt_hist    TYPE STANDARD TABLE OF ty_hist,
          lt_mbew    TYPE SORTED TABLE OF ty_mbew_idx WITH UNIQUE KEY matnr,
          ls_mbew    TYPE ty_mbew_idx,
          lt_best    TYPE SORTED TABLE OF ty_best WITH UNIQUE KEY matnr,
          ls_best    TYPE ty_best,
          lt_agg     TYPE SORTED TABLE OF ty_agg
                       WITH UNIQUE KEY lfgja lfmon dispo,
          ls_agg     TYPE ty_agg,
          ls_out     TYPE zstr_purch_stockval.

    DATA: lt_r_bwkey TYPE RANGE OF bwkey,
          lt_r_lfgja TYPE RANGE OF lfgja,
          lt_r_lfmon TYPE RANGE OF lfmon,
          lt_r_dispo TYPE RANGE OF dispo.

    DATA: lv_bwkey    TYPE bwkey,
          lv_waers    TYPE waers,
          lv_perio    TYPE i,
          lv_stichtag TYPE i,
          lv_min_gja  TYPE lfgja,
          lv_min_mon  TYPE lfmon.

    " ===================================================================
    " Schritt 0: OData-$filter auslesen
    " ===================================================================
    " Bwkey ist PFLICHT. Ohne ihn wuerde ueber Bewertungskreise mit
    " unterschiedlicher Hauswaehrung summiert - das Ergebnis waere
    " fachlich falsch, nicht nur ungenau.
    TRY.
        lt_r_bwkey = io_tech_request_context->get_filter( )->get_ranges_for_property(
                       iv_property_name = 'BWKEY' ).
        lt_r_lfgja = io_tech_request_context->get_filter( )->get_ranges_for_property(
                       iv_property_name = 'LFGJA' ).
        lt_r_lfmon = io_tech_request_context->get_filter( )->get_ranges_for_property(
                       iv_property_name = 'LFMON' ).
        lt_r_dispo = io_tech_request_context->get_filter( )->get_ranges_for_property(
                       iv_property_name = 'DISPO' ).
      CATCH cx_root.
        CLEAR: lt_r_bwkey, lt_r_lfgja, lt_r_lfmon, lt_r_dispo.
    ENDTRY.

    IF lt_r_bwkey IS INITIAL.
      " Ohne Bewertungskreis wird bewusst NICHTS geliefert, statt eine
      " fachlich falsche Gesamtsumme ueber mehrere Waehrungen zu bilden.
      RETURN.
    ENDIF.

    READ TABLE lt_r_bwkey INTO DATA(ls_r_bwkey) INDEX 1.
    lv_bwkey = ls_r_bwkey-low.

    " ===================================================================
    " Schritt 1: Stichtag bestimmen
    " ===================================================================
    " Ohne Periodenfilter gilt die laufende Periode. Das ist der Fall,
    " den die KPI-Kachel am haeufigsten zeigt.
    IF lt_r_lfgja IS INITIAL.
      lv_min_gja = sy-datum(4).
      lv_min_mon = sy-datum+4(2).
      APPEND VALUE #( sign = 'I' option = 'EQ' low = lv_min_gja ) TO lt_r_lfgja.
      APPEND VALUE #( sign = 'I' option = 'EQ' low = lv_min_mon ) TO lt_r_lfmon.
    ELSE.
      READ TABLE lt_r_lfgja INTO DATA(ls_r_gja) INDEX 1.
      lv_min_gja = ls_r_gja-low.
      IF lt_r_lfmon IS INITIAL.
        lv_min_mon = '01'.
      ELSE.
        READ TABLE lt_r_lfmon INTO DATA(ls_r_mon) INDEX 1.
        lv_min_mon = ls_r_mon-low.
      ENDIF.
    ENDIF.

    lv_stichtag = lv_min_gja * 100 + lv_min_mon.

    " ===================================================================
    " Schritt 2: Zielmaterialien aus MARC (Werk = Bewertungskreis)
    " ===================================================================
    " Der Analysereport hat am 2026-08-18 belegt, dass es KEIN bewertetes
    " Material ohne MARC-Satz gibt (0 Zeilen, 0 CHF) - der Join ist
    " lueckenlos und ein LEFT-Join waere unnoetiger Aufwand.
    IF lt_r_dispo IS INITIAL.
      SELECT matnr, dispo FROM marc
        WHERE werks = @lv_bwkey
          AND dispo <> @space
        INTO TABLE @lt_zielmat.
    ELSE.
      SELECT matnr, dispo FROM marc
        WHERE werks = @lv_bwkey
          AND dispo IN @lt_r_dispo
        INTO TABLE @lt_zielmat.
    ENDIF.

    IF lt_zielmat IS INITIAL.
      RETURN.
    ENDIF.

    " ===================================================================
    " Schritt 3: Aktuellen Bestand aus MBEW (Fallback-Basis)
    " ===================================================================
    " BWTAR-Hinweis: Am 2026-08-18 wurden im Bewertungskreis 1100
    " 65'498 Kopfsaetze und 0 Teilsaetze gemessen, getrennte Bewertung
    " gibt es dort also nicht. Die Einschraenkung auf BWTAR = space
    " steht trotzdem hier, damit ein spaeter eingefuehrter Teilsatz den
    " Wert nicht still verdoppelt.
    SELECT matnr, salk3, lbkum FROM mbew
      FOR ALL ENTRIES IN @lt_zielmat
      WHERE bwkey = @lv_bwkey
        AND matnr = @lt_zielmat-matnr
        AND bwtar = @space
      INTO TABLE @lt_mbew.

    " ===================================================================
    " Schritt 4: Historie EINMAL lesen, ab der kleinsten Periode
    " ===================================================================
    " Die Periodeneinschraenkung gehoert in die WHERE-Klausel, nicht in
    " den Code: ohne sie kamen am 2026-08-18 fuer 7'261 Materialien
    " 1'132'402 Saetze zurueck, von denen fast alle sofort verworfen
    " wurden (Historie reicht bis 2000).
    SELECT matnr, lfgja, lfmon, salk3, lbkum
      FROM mbewh
      FOR ALL ENTRIES IN @lt_zielmat
      WHERE bwkey = @lv_bwkey
        AND matnr = @lt_zielmat-matnr
        AND bwtar = @space
        AND ( lfgja > @lv_min_gja
           OR ( lfgja = @lv_min_gja AND lfmon >= @lv_min_mon ) )
      INTO TABLE @lt_hist.

    " ===================================================================
    " Schritt 5: Je angeforderter Periode zurueckrechnen und aggregieren
    " ===================================================================
    " Die Perioden werden aus den Filter-Ranges abgeleitet. Fuer jede
    " Periode laeuft dieselbe Regel: kleinste Historienperiode >=
    " Stichtag, sonst MBEW.
    LOOP AT lt_r_lfgja INTO ls_r_gja.
      DATA(lv_gja) = CONV lfgja( ls_r_gja-low ).

      LOOP AT lt_r_lfmon INTO ls_r_mon.
        DATA(lv_mon) = CONV lfmon( ls_r_mon-low ).
        lv_stichtag = lv_gja * 100 + lv_mon.

        " --- 5a) Besten Historiensatz je Material fuer DIESEN Stichtag
        CLEAR lt_best.
        LOOP AT lt_hist INTO DATA(ls_hist).
          lv_perio = ls_hist-lfgja * 100 + ls_hist-lfmon.
          IF lv_perio < lv_stichtag.
            CONTINUE.   " liegt vor dem Stichtag, irrelevant
          ENDIF.

          READ TABLE lt_best INTO ls_best WITH TABLE KEY matnr = ls_hist-matnr.
          IF sy-subrc = 0.
            IF lv_perio < ls_best-perio.
              ls_best-perio = lv_perio.
              ls_best-salk3 = ls_hist-salk3.
              ls_best-lbkum = ls_hist-lbkum.
              MODIFY TABLE lt_best FROM ls_best.
            ENDIF.
          ELSE.
            CLEAR ls_best.
            ls_best-matnr = ls_hist-matnr.
            ls_best-perio = lv_perio.
            ls_best-salk3 = ls_hist-salk3.
            ls_best-lbkum = ls_hist-lbkum.
            INSERT ls_best INTO TABLE lt_best.
          ENDIF.
        ENDLOOP.

        " --- 5b) Je Material den gueltigen Wert waehlen und aufaddieren
        LOOP AT lt_zielmat INTO ls_zielmat.
          CLEAR ls_agg.
          READ TABLE lt_agg INTO ls_agg
            WITH TABLE KEY lfgja = lv_gja
                           lfmon = lv_mon
                           dispo = ls_zielmat-dispo.
          DATA(lv_neu) = xsdbool( sy-subrc <> 0 ).
          IF lv_neu = abap_true.
            CLEAR ls_agg.
            ls_agg-lfgja = lv_gja.
            ls_agg-lfmon = lv_mon.
            ls_agg-dispo = ls_zielmat-dispo.
          ENDIF.

          READ TABLE lt_best INTO ls_best
            WITH TABLE KEY matnr = ls_zielmat-matnr.
          IF sy-subrc = 0.
            ls_agg-salk3   = ls_agg-salk3   + ls_best-salk3.
            ls_agg-lbkum   = ls_agg-lbkum   + ls_best-lbkum.
            ls_agg-aushist = ls_agg-aushist + 1.
            ls_agg-matanz  = ls_agg-matanz  + 1.
          ELSE.
            READ TABLE lt_mbew INTO ls_mbew
              WITH TABLE KEY matnr = ls_zielmat-matnr.
            IF sy-subrc = 0.
              ls_agg-salk3   = ls_agg-salk3   + ls_mbew-salk3.
              ls_agg-lbkum   = ls_agg-lbkum   + ls_mbew-lbkum.
              ls_agg-ausmbew = ls_agg-ausmbew + 1.
              ls_agg-matanz  = ls_agg-matanz  + 1.
            ENDIF.
            " Kein MBEW-Satz: Material ist im Werk angelegt, aber nicht
            " bewertet. Es traegt keinen Wert und wird bewusst NICHT
            " mitgezaehlt, damit MATANZ die bewerteten Materialien meint.
          ENDIF.

          IF lv_neu = abap_true.
            INSERT ls_agg INTO TABLE lt_agg.
          ELSE.
            MODIFY TABLE lt_agg FROM ls_agg.
          ENDIF.
        ENDLOOP.

      ENDLOOP.
    ENDLOOP.

    " ===================================================================
    " Schritt 6: Hauswaehrung ermitteln
    " ===================================================================
    " Ueber T001K (BWKEY -> BUKRS) auf T001. T001K ist im Projekt belegt
    " im Einsatz. Liefert die Kette nichts, bleibt WAERS leer - lieber
    " leer als geraten.
    SELECT SINGLE t1~waers
      FROM t001k AS tk
      INNER JOIN t001 AS t1 ON t1~bukrs = tk~bukrs
      WHERE tk~bwkey = @lv_bwkey
      INTO @lv_waers.
    IF sy-subrc <> 0.
      CLEAR lv_waers.
    ENDIF.

    " ===================================================================
    " Schritt 7: Ausgabe fuellen
    " ===================================================================
    LOOP AT lt_agg INTO ls_agg.
      CLEAR ls_out.
      ls_out-bwkey   = lv_bwkey.
      ls_out-lfgja   = ls_agg-lfgja.
      ls_out-lfmon   = ls_agg-lfmon.
      ls_out-dispo   = ls_agg-dispo.
      ls_out-salk3   = ls_agg-salk3.
      ls_out-lbkum   = ls_agg-lbkum.
      ls_out-matanz  = ls_agg-matanz.
      ls_out-aushist = ls_agg-aushist.
      ls_out-ausmbew = ls_agg-ausmbew.
      ls_out-waers   = lv_waers.
      APPEND ls_out TO et_entityset.
    ENDLOOP.

  ENDMETHOD.
