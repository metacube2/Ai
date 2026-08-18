REPORT z_purchasing_lagerwert_analyse.

" ============================================================================
" Analyse-Report: Lagerwert der Einkaufsteile (Wunsch Armin, 2026-08-18)
" ----------------------------------------------------------------------------
" HINWEIS zum Programmnamen: Wird der Code in ein bestehendes Testprogramm
" (z.B. ZTEST55) eingefuegt, muss die REPORT-Zeile oben auf dessen Namen
" lauten - SE38 meldet sonst einen Namenskonflikt.
"
" Zweck: Beantwortet DIREKT auf der Datenbank, ob und wie der von Armin
"        gewuenschte Lagerwert gebaut werden kann:
"           "Lagerwert per <bis Monat> (Werte dito Transaktion MB5L)"
"           "Abgrenzung: Einkaufsteile, d.h. Disponenten 001, 002, 003, 004, 005"
"
" Beantwortet:
"   1) Welche Disponenten (MARC-DISPO) gibt es, mit Materialzahl? Existieren
"      001 bis 005 ueberhaupt, und wie heissen sie (T024D)?
"   2) Aktueller Lagerwert (MBEW-SALK3) und Bestand (LBKUM) je Disponent und
"      Bewertungskreis, plus die Summe fuer 001-005 als direkte Vergleichszahl
"      gegen MB5L.
"   3) Verteilung der Preissteuerung (MBEW-VPRSV). Wichtig: bei 'V' (gleitender
"      Durchschnitt) waere eine eigene Rechnung LBKUM * STPRS FALSCH, es ist
"      immer SALK3 zu nehmen.
"   4) Ist die Bewertungshistorie MBEWH gefuellt? Das ist die Voraussetzung fuer
"      einen Stichtag in der Vergangenheit - MBEW selbst kennt nur HEUTE, und
"      MB5L liest fuer alte Stichtage genau MBEWH.
"   5) Historischer Lagerwert je Monatsende aus MBEWH, sofern vorhanden.
"
" ABGLEICH: Die Summe aus Abschnitt 2 gegen MB5L mit gleichem Bewertungskreis
"           und Stichtag laufen lassen. Erst wenn beide uebereinstimmen, ist die
"           Grundlage fuer eine Cockpit-Kachel belastbar.
"
" Bedienung: SE38 -> Programm anlegen -> Code einfuegen -> F8. Die WRITE-Ausgabe
"            komplett markieren und an Claude/Analytics zurueckgeben.
"
" ABAP-Fallen, die hier bewusst vermieden werden:
"   - In WRITE darf NICHT gerechnet werden -> Werte vorher in Variablen.
"   - GROUP BY ist mit FOR ALL ENTRIES NICHT erlaubt -> Aggregation im Code.
"
" Nur lesend. Keine Aenderung am System.
" ============================================================================

PARAMETERS:
  p_bwkey TYPE mbew-bwkey DEFAULT '1100',   " Bewertungskreis (CH = 1100, AT = 1200, per T001K)
  p_spras TYPE spras      DEFAULT sy-langu, " Sprache fuer Disponententexte
  p_perio TYPE i          DEFAULT 12,       " so viele Monatsperioden aus MBEWH zeigen
  " Stichtag fuer die Rueckrechnung in Abschnitt 5 (Jahr und Buchungsperiode).
  " Beispiel: 2026 / 06 = Lagerwert am Ende Juni 2026.
  p_sjahr TYPE mbewh-lfgja DEFAULT '2026',
  p_smon  TYPE mbewh-lfmon DEFAULT '06'.

" Zusammengefasste Werte je Disponent.
TYPES: BEGIN OF ty_dispo_sum,
         dispo TYPE marc-dispo,
         matnr_cnt TYPE i,
         lbkum TYPE p LENGTH 16 DECIMALS 3,
         salk3 TYPE p LENGTH 16 DECIMALS 2,
       END OF ty_dispo_sum.

DATA: lt_dispo_sum TYPE SORTED TABLE OF ty_dispo_sum WITH UNIQUE KEY dispo,
      ls_dispo_sum TYPE ty_dispo_sum.

" Summe ueber die von Armin genannten Disponenten.
DATA: lv_ziel_wert  TYPE p LENGTH 16 DECIMALS 2,
      lv_ziel_menge TYPE p LENGTH 16 DECIMALS 3,
      lv_ziel_mat   TYPE i.

START-OF-SELECTION.

  WRITE: / '############################################################'.
  WRITE: / '# Lagerwert Einkaufsteile - Analyse (Wunsch Armin)'.
  WRITE: / '# System / Mandant  :', sy-sysid, sy-mandt.
  WRITE: / '# Datum / Uhrzeit   :', sy-datum, sy-uzeit.
  WRITE: / '# Bewertungskreis   :', p_bwkey.
  WRITE: / '############################################################'.
  WRITE: / 'WICHTIG: MBEW kennt nur den AKTUELLEN Bestand. Ein Stichtag in der'.
  WRITE: / 'Vergangenheit braucht MBEWH - siehe Abschnitt 4.'.

" ----------------------------------------------------------------------------
" 1) Disponenten aus dem Werksstamm
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 1) DISPONENTEN (MARC-DISPO) im Bewertungskreis ==='.

  " Werk und Bewertungskreis sind bei Standardbewertung identisch. Falls das
  " hier abweicht, den Parameter entsprechend setzen.
  SELECT matnr, werks, dispo
    FROM marc
    WHERE werks = @p_bwkey
    INTO TABLE @DATA(lt_marc).

  DATA(lv_marc_total) = lines( lt_marc ).
  WRITE: / '  MARC-Saetze im Werk', p_bwkey, ':', lv_marc_total.

  IF lt_marc IS INITIAL.
    WRITE: / '  KEINE MARC-Saetze - Parameter p_bwkey pruefen (Werk vs. Bewertungskreis).'.
    RETURN.
  ENDIF.

  " Materialzahl je Disponent im Code aggregieren (GROUP BY spaeter nicht noetig).
  LOOP AT lt_marc INTO DATA(ls_marc).
    READ TABLE lt_dispo_sum INTO ls_dispo_sum WITH KEY dispo = ls_marc-dispo.
    IF sy-subrc = 0.
      ls_dispo_sum-matnr_cnt = ls_dispo_sum-matnr_cnt + 1.
      MODIFY TABLE lt_dispo_sum FROM ls_dispo_sum.
    ELSE.
      CLEAR ls_dispo_sum.
      ls_dispo_sum-dispo = ls_marc-dispo.
      ls_dispo_sum-matnr_cnt = 1.
      INSERT ls_dispo_sum INTO TABLE lt_dispo_sum.
    ENDIF.
  ENDLOOP.

  WRITE: / '--- Alle vorkommenden Disponenten mit Bezeichnung ---'.
  LOOP AT lt_dispo_sum INTO ls_dispo_sum.
    SELECT SINGLE dsnam FROM t024d
      WHERE werks = @p_bwkey AND dispo = @ls_dispo_sum-dispo
      INTO @DATA(lv_dsnam).
    IF sy-subrc <> 0.
      CLEAR lv_dsnam.
    ENDIF.
    WRITE: / '  DISPO=', ls_dispo_sum-dispo,
             '| Materialien=', ls_dispo_sum-matnr_cnt,
             '|', lv_dsnam.
  ENDLOOP.

  WRITE: / '--- Pruefung der von Armin genannten Disponenten 001 bis 005 ---'.
  DATA lt_ziel TYPE STANDARD TABLE OF marc-dispo.
  APPEND '001' TO lt_ziel.
  APPEND '002' TO lt_ziel.
  APPEND '003' TO lt_ziel.
  APPEND '004' TO lt_ziel.
  APPEND '005' TO lt_ziel.

  LOOP AT lt_ziel INTO DATA(lv_ziel).
    READ TABLE lt_dispo_sum INTO ls_dispo_sum WITH KEY dispo = lv_ziel.
    IF sy-subrc = 0.
      WRITE: / '  ', lv_ziel, 'vorhanden, Materialien=', ls_dispo_sum-matnr_cnt.
    ELSE.
      WRITE: / '  ', lv_ziel, 'NICHT vorhanden in diesem Werk.'.
    ENDIF.
  ENDLOOP.

" ----------------------------------------------------------------------------
" 2) Aktueller Lagerwert je Disponent (MBEW)
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 2) AKTUELLER LAGERWERT (MBEW-SALK3) JE DISPONENT ==='.
  WRITE: / '  Hinweis: SALK3 ist der gebuchte Bestandswert. NICHT LBKUM * STPRS'.
  WRITE: / '  rechnen - bei gleitendem Durchschnittspreis waere das falsch.'.

  SELECT matnr, bwkey, lbkum, salk3, stprs, vprsv, bwtar
    FROM mbew
    WHERE bwkey = @p_bwkey
    INTO TABLE @DATA(lt_mbew).
  WRITE: / '  MBEW-Saetze im Bewertungskreis:', lines( lt_mbew ).

  " Disponent je Material aus MARC in eine Hilfstabelle, damit der Join im Code
  " ueber eine sortierte Tabelle laeuft statt ueber verschachtelte SELECTs.
  TYPES: BEGIN OF ty_matdispo,
           matnr TYPE marc-matnr,
           dispo TYPE marc-dispo,
         END OF ty_matdispo.
  DATA lt_matdispo TYPE SORTED TABLE OF ty_matdispo WITH UNIQUE KEY matnr.
  DATA ls_matdispo TYPE ty_matdispo.

  LOOP AT lt_marc INTO ls_marc.
    CLEAR ls_matdispo.
    ls_matdispo-matnr = ls_marc-matnr.
    ls_matdispo-dispo = ls_marc-dispo.
    INSERT ls_matdispo INTO TABLE lt_matdispo.
  ENDLOOP.

  " Bewertete Saetze ohne Disponentenzuordnung getrennt ausweisen, damit die
  " Summe nachvollziehbar bleibt.
  DATA: lv_ohne_dispo_wert TYPE p LENGTH 16 DECIMALS 2,
        lv_ohne_dispo_cnt  TYPE i.

  LOOP AT lt_mbew INTO DATA(ls_mbew).
    READ TABLE lt_matdispo INTO ls_matdispo WITH TABLE KEY matnr = ls_mbew-matnr.
    IF sy-subrc <> 0.
      lv_ohne_dispo_wert = lv_ohne_dispo_wert + ls_mbew-salk3.
      lv_ohne_dispo_cnt  = lv_ohne_dispo_cnt + 1.
      CONTINUE.
    ENDIF.

    READ TABLE lt_dispo_sum INTO ls_dispo_sum WITH KEY dispo = ls_matdispo-dispo.
    IF sy-subrc = 0.
      ls_dispo_sum-lbkum = ls_dispo_sum-lbkum + ls_mbew-lbkum.
      ls_dispo_sum-salk3 = ls_dispo_sum-salk3 + ls_mbew-salk3.
      MODIFY TABLE lt_dispo_sum FROM ls_dispo_sum.
    ENDIF.
  ENDLOOP.

  WRITE: / '--- Lagerwert je Disponent ---'.
  LOOP AT lt_dispo_sum INTO ls_dispo_sum.
    WRITE: / '  DISPO=', ls_dispo_sum-dispo,
             '| Menge=', ls_dispo_sum-lbkum,
             '| Wert=', ls_dispo_sum-salk3.
  ENDLOOP.
  WRITE: / '  Bewertete Materialien OHNE MARC-Satz in diesem Werk:', lv_ohne_dispo_cnt,
           '| Wert=', lv_ohne_dispo_wert.

  " Zielsumme 001-005: das ist die Zahl fuer den MB5L-Abgleich.
  LOOP AT lt_ziel INTO lv_ziel.
    READ TABLE lt_dispo_sum INTO ls_dispo_sum WITH KEY dispo = lv_ziel.
    IF sy-subrc = 0.
      lv_ziel_wert  = lv_ziel_wert  + ls_dispo_sum-salk3.
      lv_ziel_menge = lv_ziel_menge + ls_dispo_sum-lbkum.
      lv_ziel_mat   = lv_ziel_mat   + ls_dispo_sum-matnr_cnt.
    ENDIF.
  ENDLOOP.

  ULINE.
  WRITE: / '*** ZIELZAHL FUER DEN MB5L-ABGLEICH ***'.
  WRITE: / '  Disponenten 001-005, Bewertungskreis', p_bwkey.
  WRITE: / '  Materialien :', lv_ziel_mat.
  WRITE: / '  Menge       :', lv_ziel_menge.
  WRITE: / '  LAGERWERT   :', lv_ziel_wert.
  WRITE: / '  Bitte MB5L mit gleichem Bewertungskreis und Stichtag HEUTE laufen'.
  WRITE: / '  lassen und die Summe vergleichen.'.

" ----------------------------------------------------------------------------
" 3) Preissteuerung
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 3) PREISSTEUERUNG (MBEW-VPRSV) ==='.
  DATA: lv_vprsv_s TYPE i,
        lv_vprsv_v TYPE i,
        lv_vprsv_x TYPE i.
  LOOP AT lt_mbew INTO ls_mbew.
    CASE ls_mbew-vprsv.
      WHEN 'S'. lv_vprsv_s = lv_vprsv_s + 1.
      WHEN 'V'. lv_vprsv_v = lv_vprsv_v + 1.
      WHEN OTHERS. lv_vprsv_x = lv_vprsv_x + 1.
    ENDCASE.
  ENDLOOP.
  WRITE: / '  S (Standardpreis)          :', lv_vprsv_s.
  WRITE: / '  V (gleitender Durchschnitt):', lv_vprsv_v.
  WRITE: / '  anderes / leer             :', lv_vprsv_x.
  IF lv_vprsv_v > 0.
    WRITE: / '  -> Es gibt V-Materialien. SALK3 ist damit zwingend; eine eigene'.
    WRITE: / '     Rechnung LBKUM * STPRS wuerde diese Materialien falsch bewerten.'.
  ENDIF.

" ----------------------------------------------------------------------------
" 4) Bewertungshistorie MBEWH - Voraussetzung fuer "per bis Monat"
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 4) BEWERTUNGSHISTORIE (MBEWH) ==='.
  WRITE: / '  Das ist der entscheidende Punkt fuer Armins "per <bis Monat>".'.

  SELECT COUNT(*) FROM mbewh WHERE bwkey = @p_bwkey INTO @DATA(lv_mbewh_cnt).
  WRITE: / '  MBEWH-Saetze im Bewertungskreis:', lv_mbewh_cnt.

  IF lv_mbewh_cnt = 0.
    WRITE: / '  KEINE Historie vorhanden -> ein Stichtag in der Vergangenheit ist'.
    WRITE: / '  aus diesen Tabellen NICHT ableitbar. Dann bleibt nur "Stand heute"'.
    WRITE: / '  oder ein anderer Weg (z.B. periodischer Snapshot in der App).'.
  ELSE.
    WRITE: / '  Historie vorhanden -> "per <bis Monat>" ist grundsaetzlich machbar.'.

    " Perioden absteigend zeigen, damit die juengsten oben stehen.
    SELECT lfgja, lfmon, COUNT(*) AS cnt
      FROM mbewh
      WHERE bwkey = @p_bwkey
      GROUP BY lfgja, lfmon
      ORDER BY lfgja DESCENDING, lfmon DESCENDING
      INTO TABLE @DATA(lt_perioden)
      UP TO @p_perio ROWS.

    WRITE: / '--- Gefuellte Perioden (juengste zuerst) ---'.
    LOOP AT lt_perioden INTO DATA(ls_periode).
      WRITE: / '  Jahr', ls_periode-lfgja, 'Periode', ls_periode-lfmon,
               '| Saetze=', ls_periode-cnt.
    ENDLOOP.
  ENDIF.

" ----------------------------------------------------------------------------
" 5) Historischer Lagerwert je Periode fuer die Zieldisponenten
" ----------------------------------------------------------------------------
  IF lv_mbewh_cnt > 0.
    ULINE.
    WRITE: / '=== 5) LAGERWERT 001-005 PER STICHTAG (Rueckrechnung) ==='.
    WRITE: / '  Stichtag: Jahr', p_sjahr, 'Periode', p_smon.

    " ==================================================================
    " WARUM NICHT EINFACH SUM(MBEWH-SALK3) FUER DIE PERIODE?
    "
    " Genau das hat die erste Fassung dieses Reports getan und dabei am
    " 2026-08-18 offensichtlich falsche Zahlen geliefert: 2026/02 noch
    " 8'355'679 CHF, 2026/04 nur noch 383'904 CHF. Der Bestand ist nicht
    " gefallen - MBEWH enthaelt fuer eine Periode NUR die Materialien, bei
    " denen es DANACH eine bewertungsrelevante Bewegung gab. Ein Material
    " ohne Bewegung seit Maerz hat fuer 2026/04 keinen Historiensatz; sein
    " Wert steht unveraendert in MBEW.
    "
    " KORREKTE LOGIK (das macht MB5L auch): je Material den MBEWH-Satz der
    " KLEINSTEN Periode >= Stichtag nehmen. Existiert keiner, gilt der
    " aktuelle MBEW-Wert, weil sich seit dem Stichtag nichts geaendert hat.
    " ==================================================================

    DATA lv_stichtag TYPE i.
    lv_stichtag = p_sjahr * 100 + p_smon.

    " Zielmaterialien (Disponenten 001-005) einmal aufbauen.
    TYPES: BEGIN OF ty_zielmat,
             matnr TYPE marc-matnr,
           END OF ty_zielmat.
    DATA: lt_zielmat TYPE SORTED TABLE OF ty_zielmat WITH UNIQUE KEY matnr,
          ls_zielmat TYPE ty_zielmat.

    LOOP AT lt_matdispo INTO ls_matdispo.
      READ TABLE lt_ziel TRANSPORTING NO FIELDS
        WITH KEY table_line = ls_matdispo-dispo.
      IF sy-subrc <> 0.
        CONTINUE.
      ENDIF.
      CLEAR ls_zielmat.
      ls_zielmat-matnr = ls_matdispo-matnr.
      INSERT ls_zielmat INTO TABLE lt_zielmat.
    ENDLOOP.

    WRITE: / '  Zielmaterialien (Disponenten 001-005):', lines( lt_zielmat ).

    IF lt_zielmat IS INITIAL.
      WRITE: / '  Keine Zielmaterialien - Abschnitt uebersprungen.'.
      RETURN.
    ENDIF.

    " Historie NUR fuer die Zielmaterialien und NUR ab dem Stichtag lesen.
    " Das begrenzt die Menge erheblich (im Bewertungskreis liegen insgesamt
    " ueber 5 Mio Saetze) und liefert genau die Kandidaten fuer "kleinste
    " Periode >= Stichtag".
    DATA lt_hist TYPE STANDARD TABLE OF mbewh.
    SELECT matnr, lfgja, lfmon, lbkum, salk3
      FROM mbewh
      FOR ALL ENTRIES IN @lt_zielmat
      WHERE bwkey = @p_bwkey
        AND matnr = @lt_zielmat-matnr
      INTO CORRESPONDING FIELDS OF TABLE @lt_hist.

    WRITE: / '  MBEWH-Saetze zu diesen Materialien (alle Perioden):', lines( lt_hist ).

    " Je Material den besten Kandidaten bestimmen: kleinste Periode >= Stichtag.
    TYPES: BEGIN OF ty_best,
             matnr TYPE mbewh-matnr,
             perio TYPE i,
             salk3 TYPE mbewh-salk3,
             lbkum TYPE mbewh-lbkum,
           END OF ty_best.
    DATA: lt_best TYPE SORTED TABLE OF ty_best WITH UNIQUE KEY matnr,
          ls_best TYPE ty_best,
          lv_perio TYPE i.

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

    " Summieren: Historienwert wenn vorhanden, sonst aktueller MBEW-Wert.
    DATA: lv_hist_wert  TYPE p LENGTH 16 DECIMALS 2,
          lv_hist_menge TYPE p LENGTH 16 DECIMALS 3,
          lv_aus_hist   TYPE i,
          lv_aus_mbew   TYPE i.

    " MBEW einmal nach Material indizieren, damit der Fallback schnell ist.
    TYPES: BEGIN OF ty_mbew_idx,
             matnr TYPE mbew-matnr,
             salk3 TYPE mbew-salk3,
             lbkum TYPE mbew-lbkum,
           END OF ty_mbew_idx.
    DATA: lt_mbew_idx TYPE SORTED TABLE OF ty_mbew_idx WITH UNIQUE KEY matnr,
          ls_mbew_idx TYPE ty_mbew_idx.

    LOOP AT lt_mbew INTO ls_mbew.
      CLEAR ls_mbew_idx.
      ls_mbew_idx-matnr = ls_mbew-matnr.
      ls_mbew_idx-salk3 = ls_mbew-salk3.
      ls_mbew_idx-lbkum = ls_mbew-lbkum.
      INSERT ls_mbew_idx INTO TABLE lt_mbew_idx.
    ENDLOOP.

    LOOP AT lt_zielmat INTO ls_zielmat.
      READ TABLE lt_best INTO ls_best WITH TABLE KEY matnr = ls_zielmat-matnr.
      IF sy-subrc = 0.
        lv_hist_wert  = lv_hist_wert  + ls_best-salk3.
        lv_hist_menge = lv_hist_menge + ls_best-lbkum.
        lv_aus_hist   = lv_aus_hist + 1.
      ELSE.
        READ TABLE lt_mbew_idx INTO ls_mbew_idx
          WITH TABLE KEY matnr = ls_zielmat-matnr.
        IF sy-subrc = 0.
          lv_hist_wert  = lv_hist_wert  + ls_mbew_idx-salk3.
          lv_hist_menge = lv_hist_menge + ls_mbew_idx-lbkum.
          lv_aus_mbew   = lv_aus_mbew + 1.
        ENDIF.
      ENDIF.
    ENDLOOP.

    WRITE: / '--- Zusammensetzung ---'.
    WRITE: / '  Materialien mit Historiensatz ab Stichtag:', lv_aus_hist.
    WRITE: / '  Materialien ohne Historiensatz (MBEW-Fallback):', lv_aus_mbew.
    ULINE.
    WRITE: / '*** LAGERWERT PER STICHTAG ***'.
    WRITE: / '  Jahr', p_sjahr, 'Periode', p_smon, 'Bewertungskreis', p_bwkey.
    WRITE: / '  Menge     :', lv_hist_menge.
    WRITE: / '  LAGERWERT :', lv_hist_wert.
    WRITE: / '  Bitte MB5L mit genau diesem Stichtag und Bewertungskreis'.
    WRITE: / '  gegenrechnen. Erst bei Uebereinstimmung ist die Logik belegt.'.

    " Zur Einordnung zusaetzlich die naive Summe zeigen, damit der Unterschied
    " zur falschen Methode sichtbar bleibt.
    DATA lv_naiv TYPE p LENGTH 16 DECIMALS 2.
    LOOP AT lt_hist INTO ls_hist.
      IF ls_hist-lfgja = p_sjahr AND ls_hist-lfmon = p_smon.
        lv_naiv = lv_naiv + ls_hist-salk3.
      ENDIF.
    ENDLOOP.
    WRITE: / '  Zum Vergleich, NAIVE Summe nur ueber MBEWH dieser Periode:', lv_naiv.
    WRITE: / '  (Diese Zahl ist FALSCH und steht hier nur zur Abgrenzung.)'.
  ENDIF.

" ----------------------------------------------------------------------------
" 6) Was dieser Report NICHT beantwortet
" ----------------------------------------------------------------------------
  ULINE.
  WRITE: / '=== 6) OFFEN - BITTE MANUELL PRUEFEN ==='.
  WRITE: / '  a) Liefert der Gateway-Service ZPOWERBI_EINKAUF_SRV ein Set fuer'.
  WRITE: / '     MBEWH? mbewSet (MBEW) ist bereits im Einsatz, MBEWH nirgends'.
  WRITE: / '     dokumentiert. ACHTUNG: mbewSet kostet mit 68 Tsd Zeilen bereits'.
  WRITE: / '     124 MB und 28 s je Aufruf; MBEWH hat hier ueber 5 Mio Zeilen.'.
  WRITE: / '     Ein Full Load ist damit ausgeschlossen - es braucht ein eigenes,'.
  WRITE: / '     serverseitig AGGREGIERENDES Set (Wert je Periode/Disponentengruppe).'.
  WRITE: / '  b) Sollen Sonderbestaende (Konsignation MSKU, Kundenauftrag MSKA,'.
  WRITE: / '     Projekt MSPR) mitzaehlen? MBEW deckt nur den Eigenbestand.'.
  WRITE: / '     In der Projektdokumentation bisher nirgends behandelt.'.
  WRITE: / '  c) Gehoert Disponent 004 fachlich zum Lagerwert der Einkaufsteile?'.
  WRITE: / '     SAP-Text ist "Betriebsmat/Einkau", organisatorisch also Einkauf.'.
  WRITE: / '     Entscheidung liegt bei Armin.'.
  WRITE: / '  d) BEANTWORTET, nicht mehr offen: Bewertungskreis 1100 = CH,'.
  WRITE: / '     1200 = AT, per T001K bestaetigt (docs/FINANCE_STANDARDKOSTEN.md).'.
  WRITE: / '     Eine Summe ueber beide waere wegen Hauswaehrung falsch.'.

  ULINE.
  WRITE: / '=== ENDE. Bitte komplette Ausgabe an Claude/Analytics zurueckgeben. ==='.
