*&---------------------------------------------------------------------*
*& Logistik live, Teil C: Kapazitaet je Arbeitsplatz und Tag (LogKapSet)
*& Stand  : 2026-10-05, ENTWURF, in T76 noch nicht angelegt.
*& Transport: zweiter Auftrag (eigener Transport, damit Teil B nicht wartet).
*&
*& DDIC-Struktur ZSTR_LOG_KAP (SE11, Paket ZPP):
*&   WERKS     WERKS_D    Werk (Pflichtfilter)
*&   DATUM     CHAR8      Tag JJJJMMTT (Pflichtfilter: erster Tag)
*&   TAGE      CHAR2      Anzahl Tage 1..14 (Filter, Standard 7)
*&   ARBPL     ARBPL      Arbeitsplatz
*&   TAG       CHAR8      Tag der Zeile JJJJMMTT
*&   BEDARF_H  DEC 13,2 (Datenelement z.B. CO_ISMNG-artig, sonst DEC13_2) Bedarf Stunden (KBED Rest Bearbeiten + Ruesten)
*&   ANGEBOT_H DEC 13,2  Angebot Stunden (CR_CAPACITY_AVAILABLE_PERIODS, Einsatzzeit x Anzahl)
*&   AUFTRAEGE INT4      Anzahl Vorgaenge mit Bedarf am Tag
*&
*& SCHUTZ FUER P76 wie die anderen Log-Sets:
*& - nur lesend; Pflichtfilter WERKS und DATUM, sonst leere Antwort ohne DB-Zugriff;
*& - hoechstens 14 Tage, DATUM nicht aelter als 30 Tage und nicht weiter als 60 Tage voraus;
*& - UP TO 20000 ROWS auf KBED, Angebot nur fuer Arbeitsplaetze mit Bedarf im Fenster
*&   (hoechstens 300 Aufrufe des Standardbausteins).
*& T76: Daten enden im Maerz; Bedarf in der Zukunft ist dort voraussichtlich leer.
*&---------------------------------------------------------------------*

* ---- MPC_EXT DEFINE: Entity LogKap ---------------------------------
*  lo_entity_type = model->create_entity_type( iv_entity_type_name = 'LogKap' iv_def_entity_set = abap_false ).
*  Schluessel: Werks, Arbpl, Tag (Edm.String); Filter: Werks, Datum, Tage.
*  Felder BedarfH, AngebotH als Edm.Decimal (precision 13, scale 2), Auftraege Edm.Int32.
*  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_LOG_KAP' iv_bind_conversions = 'X' ).
*  lo_entity_set = lo_entity_type->create_entity_set( 'LogKapSet' ). (nicht schreibbar, pageable)

* ---- DPC_EXT GET_ENTITYSET: Block vor dem Log*-Block einfuegen --------
  IF iv_entity_set_name = 'LogKapSet'.
    TYPES: BEGIN OF ty_kp_bed,
             arbid    TYPE objektid,
             fstad    TYPE fstad,
             kbearest TYPE cy_kbeares,
             kruerest TYPE cy_krueres,
             keinh    TYPE cy_keinh,
           END OF ty_kp_bed,
           BEGIN OF ty_kp_ap,
             objid TYPE cr_objid,
             arbpl TYPE arbpl,
             werks TYPE werks_d,
           END OF ty_kp_ap,
           BEGIN OF ty_kp_ca,
             objid TYPE cr_objid,
             kapid TYPE kapid,
           END OF ty_kp_ca.
    DATA: lv_kp_werks TYPE werks_d,
          lv_kp_von   TYPE datum,
          lv_kp_bis   TYPE datum,
          lv_kp_tage  TYPE i VALUE 7,
          lv_kp_std   TYPE p LENGTH 13 DECIMALS 2,
          lv_kp_n     TYPE i,
          lt_kp_bed   TYPE STANDARD TABLE OF ty_kp_bed,
          ls_kp_bed   TYPE ty_kp_bed,
          lt_kp_ap    TYPE SORTED TABLE OF ty_kp_ap WITH UNIQUE KEY objid,
          ls_kp_ap    TYPE ty_kp_ap,
          lt_kp_ca    TYPE STANDARD TABLE OF ty_kp_ca,
          ls_kp_ca    TYPE ty_kp_ca,
          lt_kp_avail TYPE STANDARD TABLE OF rc65k,
          ls_kp_avail TYPE rc65k,
          lt_kp_out   TYPE STANDARD TABLE OF zstr_log_kap,
          ls_kp_out   TYPE zstr_log_kap.
    FIELD-SYMBOLS: <ls_kp_out> TYPE zstr_log_kap,
                   <ls_kp_f>   LIKE LINE OF it_filter_select_options,
                   <ls_kp_o>   TYPE /iwbep/s_cod_select_option.

    LOOP AT it_filter_select_options ASSIGNING <ls_kp_f>.
      LOOP AT <ls_kp_f>-select_options ASSIGNING <ls_kp_o> WHERE sign = 'I' AND option = 'EQ'.
        CASE to_upper( <ls_kp_f>-property ).
          WHEN 'WERKS'. lv_kp_werks = <ls_kp_o>-low.
          WHEN 'DATUM'. lv_kp_von   = <ls_kp_o>-low.
          WHEN 'TAGE'.  lv_kp_tage  = <ls_kp_o>-low.
          WHEN OTHERS.
        ENDCASE.
      ENDLOOP.
    ENDLOOP.

    IF lv_kp_werks IS INITIAL OR lv_kp_von IS INITIAL
       OR lv_kp_von < sy-datum - 30 OR lv_kp_von > sy-datum + 60.
      copy_data_to_ref( EXPORTING is_data = lt_kp_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.
    IF lv_kp_tage < 1 OR lv_kp_tage > 14. lv_kp_tage = 7. ENDIF.
    lv_kp_bis = lv_kp_von + lv_kp_tage - 1.

*   Arbeitsplaetze des Werks (CRHD), dann Bedarf mit Rest im Fenster.
    SELECT objid arbpl werks FROM crhd INTO CORRESPONDING FIELDS OF TABLE lt_kp_ap
      WHERE objty = 'A' AND werks = lv_kp_werks.
    IF lt_kp_ap IS NOT INITIAL.
      SELECT arbid fstad kbearest kruerest keinh FROM kbed
        INTO CORRESPONDING FIELDS OF TABLE lt_kp_bed UP TO 20000 ROWS
        FOR ALL ENTRIES IN lt_kp_ap
        WHERE arbid = lt_kp_ap-objid
          AND fstad BETWEEN lv_kp_von AND lv_kp_bis.
    ENDIF.

    LOOP AT lt_kp_bed INTO ls_kp_bed WHERE kbearest > 0 OR kruerest > 0.
      READ TABLE lt_kp_ap INTO ls_kp_ap WITH TABLE KEY objid = ls_kp_bed-arbid.
      CHECK sy-subrc = 0.
*     Einheit in Stunden umrechnen (KBED fuehrt Sekunden, Minuten oder Stunden).
      lv_kp_std = ls_kp_bed-kbearest + ls_kp_bed-kruerest.
      CASE ls_kp_bed-keinh.
        WHEN 'S'.   lv_kp_std = lv_kp_std / 3600.
        WHEN 'MIN'. lv_kp_std = lv_kp_std / 60.
        WHEN OTHERS.
      ENDCASE.
      READ TABLE lt_kp_out ASSIGNING <ls_kp_out>
           WITH KEY arbpl = ls_kp_ap-arbpl tag = ls_kp_bed-fstad.
      IF sy-subrc <> 0.
        CLEAR ls_kp_out.
        ls_kp_out-werks = lv_kp_werks. ls_kp_out-datum = lv_kp_von.
        ls_kp_out-tage  = lv_kp_tage.  ls_kp_out-arbpl = ls_kp_ap-arbpl.
        ls_kp_out-tag   = ls_kp_bed-fstad.
        APPEND ls_kp_out TO lt_kp_out ASSIGNING <ls_kp_out>.
      ENDIF.
      <ls_kp_out>-bedarf_h  = <ls_kp_out>-bedarf_h + lv_kp_std.
      <ls_kp_out>-auftraege = <ls_kp_out>-auftraege + 1.
    ENDLOOP.

*   Angebot nur fuer Arbeitsplaetze mit Bedarf, ueber den SAP-Standard (Schichtprogramm, Kalender, Anzahl).
    SORT lt_kp_out BY arbpl tag.
    LOOP AT lt_kp_ap INTO ls_kp_ap.
      READ TABLE lt_kp_out TRANSPORTING NO FIELDS WITH KEY arbpl = ls_kp_ap-arbpl BINARY SEARCH.
      CHECK sy-subrc = 0.
      lv_kp_n = lv_kp_n + 1.
      IF lv_kp_n > 300. EXIT. ENDIF.
      CLEAR lt_kp_avail.
      CALL FUNCTION 'CR_CAPACITY_AVAILABLE_PERIODS'
        EXPORTING
          objid_a   = ls_kp_ap-objid
          single    = 'X'
        TABLES
          t_avail   = lt_kp_avail
        EXCEPTIONS
          OTHERS    = 1.
      CHECK sy-subrc = 0.
      LOOP AT lt_kp_out ASSIGNING <ls_kp_out> WHERE arbpl = ls_kp_ap-arbpl.
        LOOP AT lt_kp_avail INTO ls_kp_avail
             WHERE datuv <= <ls_kp_out>-tag AND datub >= <ls_kp_out>-tag.
          lv_kp_std = ls_kp_avail-angeb.
          CASE ls_kp_avail-keinh.
            WHEN 'S'.   lv_kp_std = lv_kp_std / 3600.
            WHEN 'MIN'. lv_kp_std = lv_kp_std / 60.
            WHEN OTHERS.
          ENDCASE.
          <ls_kp_out>-angebot_h = <ls_kp_out>-angebot_h + lv_kp_std.
        ENDLOOP.
      ENDLOOP.
    ENDLOOP.

    copy_data_to_ref( EXPORTING is_data = lt_kp_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.

* OFFEN VOR DEM ANLEGEN (in T76 pruefen, nicht raten):
* - Liefert CR_CAPACITY_AVAILABLE_PERIODS ohne Zeitraum-Parameter die Perioden des Planungshorizonts,
*   oder braucht es einen vorgelagerten Aufruf (CR_CAPACITY_PRE_READ / Puffer)? Mit einem Arbeitsplatz
*   in SE37 testen (Testumgebung, rein lesend).
* - Ist ANGEB je Periode bereits der Tageswert oder die Summe ueber DATUV..DATUB? Mit PTAGE/GTAGE abgleichen.
* - Einheit KEINH in KBED und RC65K an echten Saetzen pruefen.
