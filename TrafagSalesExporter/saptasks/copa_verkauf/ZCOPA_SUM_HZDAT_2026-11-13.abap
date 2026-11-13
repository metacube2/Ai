*&---------------------------------------------------------------------*
*& CoPaSumSet: Stichtag nach HZDAT (Paket Fabio 13.11.2026, Punkt 8)
*& Stand    : 2026-11-13, Aenderungsvorgabe fuer T76 (Auftrag Ingo „bitte umsetzen“)
*& Zweck    : Fabio: „Stichtag bis Vortag nach HZDAT (Angelegt am), nicht BUDAT“. Das Cockpit
*&            (ShopfloorSapCoPa.Filter, Schalter Shopfloor:SapCoPaHzdat) haengt dann
*&            "and Hzdat le 'JJJJMMTT'" (Vortag) an den Pflichtfilter Bukrs/Gjahr/Perde.
*&            Ohne Hzdat-Filter rechnet das Set wie bisher (ganze Periode).
*& Grundlage: ZCOPA_SUM_ADD.abap (Stand 10.11., T76K912866, in P76). Vor dem Einspielen den
*&            Live-Stand beider Methoden per RFC lesen und nur die markierten Stellen aendern.
*& Pruefen  : Feld CE11000-HZDAT in DD03L (T76) vorhanden und Typ DATS; sonst anhalten.
*&---------------------------------------------------------------------*

* ===== 1. DDIC: Struktur ZSTR_COPA_SUM, Feld am Ende anhaengen ==========
*   HZDAT  CHAR8   (Stichtag des Filters, JJJJMMTT; leer ohne Filter)

* ===== 2. MPC_EXT DEFINE: im CoPaSum-Block nach der Property 'Anzahl', vor bind_structure =====
  lo_property = lo_entity_type->create_property( iv_property_name = 'Hzdat' iv_abap_fieldname = 'HZDAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

* ===== 3. DPC_EXT GET_ENTITYSET, Zweig CoPaSumSet ========================
* 3a. DATA-Block des Zweigs ergaenzen:
    DATA: lr_cp_hzdat TYPE RANGE OF ce11000-hzdat,
          ls_cp_hzdat LIKE LINE OF lr_cp_hzdat,
          lv_cp_hzdat TYPE char8.

* 3b. Direkt nach der bestehenden Filterschleife (die nur sign I / option EQ liest) einfuegen:
*     Hzdat mit jeder Option uebernehmen (das Cockpit schickt "le", also I LE).
    LOOP AT it_filter_select_options ASSIGNING <ls_cp_f>.
      CHECK to_upper( <ls_cp_f>-property ) = 'HZDAT'.
      LOOP AT <ls_cp_f>-select_options ASSIGNING <ls_cp_o>.
        CLEAR ls_cp_hzdat.
        ls_cp_hzdat-sign   = <ls_cp_o>-sign.
        ls_cp_hzdat-option = <ls_cp_o>-option.
        ls_cp_hzdat-low    = <ls_cp_o>-low.
        ls_cp_hzdat-high   = <ls_cp_o>-high.
        APPEND ls_cp_hzdat TO lr_cp_hzdat.
        lv_cp_hzdat = <ls_cp_o>-low.
      ENDLOOP.
    ENDLOOP.

* 3c. In BEIDEN SELECTs (mit und ohne Vrgar) nach "AND perde = lv_cp_perde" ergaenzen:
*          AND hzdat    IN lr_cp_hzdat
*     (leere Range = keine Einschraenkung, Verhalten ohne Filter unveraendert)

* 3d. In der Ausgabeschleife nach "ls_cp_out-anzahl = ls_cp_sum-anzahl." ergaenzen:
      ls_cp_out-hzdat  = lv_cp_hzdat.

* ===== Abnahme T76 ======================================================
* - Gateway Client T76 (nach Cache /IWBEP, dann /IWFND):
*   CoPaSumSet?$filter=Bukrs eq '1100' and Gjahr eq '2026' and Perde eq '003'            -> wie vorher (Summen gleich)
*   ... and Hzdat le '20260315'                                                          -> kleiner oder gleich, Hzdat = 20260315
*   ohne Pflichtfilter                                                                    -> 400 wie vorher
* - Vergleich je Satzart/Sparte gegen SE16 CE11000 (Bukrs 1100, Periode 003.2026, PALEDGER 01, HZDAT <= 15.03.2026).
* ===== Abnahme P76 (nach Import und Cache, Ingo) =========================
* - Vorschau Cockpit mit Schalter an: Fabios Kontrollwerte je Stichtag
*   (docs/shopfloor_paket_2026-11-13/2026-11-12_verkauf/Kontrollwerte_Verkauf_CE11000_2026-11.csv),
*   z. B. Stand bis 10.11.: umsatz_ist 2'391'295.84, u_tx 807'998.72, u_dw 1'219'138.35.
