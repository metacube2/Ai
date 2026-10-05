*&---------------------------------------------------------------------*
*& Logistik live, Teil C: Kapazitaet je Kapazitaet und Tag (LogKapSet)
*& Stand  : 2026-10-05, zweite Fassung nach dem Testreport Z_LOG_KAP_TEST ($TMP, T76).
*& Transport: eigener Auftrag (nicht T76K912658), damit Teil B nicht wartet.
*&
*& Warum je KAPAZITAET und nicht je Arbeitsplatz (gemessen in T76):
*& - MLE01 und MLE02 teilen sich die Personalkapazitaet 10000262 "LAG00" (KAPAR 002), daneben
*&   hat jeder Arbeitsplatz eine eigene Maschinenkapazitaet (CRCA). Angebot und Bedarf gehoeren
*&   deshalb zur Kapazitaet; die Arbeitsplaetze werden als Text mitgeliefert.
*& - CR_CAPACITY_AVAILABLE_PERIODS lieferte ohne vorbereiteten Puffer 0 Perioden. Fuer die
*&   MLE-Kapazitaeten gibt es KEINE Intervalle (KAPA leer), also gilt das SAP-Standardangebot aus
*&   KAKO: (ENDZT - BEGZT - PAUSE) x NGRAD % x AZNOR an Arbeitstagen des Fabrikkalenders
*&   (KAKO-KALID, sonst T001W-FABKL). Kapazitaeten MIT Intervallen werden NICHT gerechnet
*&   (ANGEBOT_H leer, KEIN_STANDARD = 'X'), statt ein falsches Angebot zu zeigen.
*& - KBED: Bedarf = KBEAREST + KRUEREST (Rest Bearbeiten + Ruesten) in der Einheit KEINH (H, MIN oder S,
*&   wird in Stunden umgerechnet). In T76 sind die Reste der vergangenen Tage 0 (erledigt), Soll vorhanden.
*&
*& Dritte Fassung 2026-10-05 (Review, Befund DWM00 1323 %):
*& - KBED legt den GANZEN Restbedarf eines Vorgangs auf FSTAD (Starttermin). Ein Vorgang, der ueber mehrere
*&   Tage laeuft, erschien deshalb an einem Tag mit seinem gesamten Bedarf. Jetzt wird der Bedarf gleichmaessig
*&   auf die Arbeitstage von FSTAD bis FENDD (Fabrikkalender der Kapazitaet) verteilt; im Fenster zaehlt nur der
*&   Anteil der Tage im Fenster. Hat der Vorgang kein Ende (FENDD leer), liegt alles auf FSTAD wie bisher.
*&   Rueckstand, dessen Zeitraum VOR dem Fenster endet, erscheint NICHT mehr auf dem ersten Tag.
*&   Genauer waere die tatsaechliche Verteilung je Tag aus KBEZ; die Feldnamen dort sind nicht geprueft.
*& - VORGAENGE zaehlt verschiedene Vorgaenge (KBEDID) je Kapazitaet und Tag, nicht KBED-Zeilen.
*& - VOR DEM EINFUEGEN IN SE11 PRUEFEN (KBED): FENDD (Ende), KEINH, KBEDID; sonst die Namen anpassen.
*& - is_paging (skip/top) wird angewendet wie bei den anderen Sets; Obergrenzen melden eine Warnung.
*&
*& DDIC-Struktur ZSTR_LOG_KAP (SE11, Paket ZPP):
*&   WERKS WERKS_D, DATUM CHAR8, TAGE CHAR2, KAPID KAPID, KAPNAME KAPNAME, KAPAR KAPART,
*&   ARBPL TEXT80 (Arbeitsplaetze der Kapazitaet), TAG CHAR8, BEDARF_H CY_KBEARES (FLTP),
*&   ANGEBOT_H CY_KBEARES (FLTP), VORGAENGE INT4, KEIN_STANDARD CHAR2
*&
*& SCHUTZ FUER P76 wie die anderen Log-Sets:
*& - nur lesend; Pflichtfilter Werks und Datum, sonst leere Antwort ohne DB-Zugriff;
*& - hoechstens 14 Tage, Datum zwischen heute - 30 und heute + 60;
*& - KBED ueber den Sekundaerindex 3 (KAPID), UP TO 20000 ROWS; hoechstens 200 Kapazitaeten.
*&---------------------------------------------------------------------*

* ===== MPC_EXT DEFINE: vor dem LogTa-Block einfuegen =================
* ---------------------------------------------------------------------
* LogKap: LogKapSet, Pflichtfilter Werks und Datum, Tage 1..14
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'LogKap'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kapid' iv_abap_fieldname = 'KAPID' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Tag' iv_abap_fieldname = 'TAG' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Datum' iv_abap_fieldname = 'DATUM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Tage' iv_abap_fieldname = 'TAGE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kapname' iv_abap_fieldname = 'KAPNAME' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kapar' iv_abap_fieldname = 'KAPAR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Arbpl' iv_abap_fieldname = 'ARBPL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 80 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'BedarfH' iv_abap_fieldname = 'BEDARF_H' ).
  lo_property->set_type_edm_double( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'AngebotH' iv_abap_fieldname = 'ANGEBOT_H' ).
  lo_property->set_type_edm_double( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Vorgaenge' iv_abap_fieldname = 'VORGAENGE' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'KeinStandard' iv_abap_fieldname = 'KEIN_STANDARD' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_LOG_KAP'
                                  iv_bind_conversions = 'X' ).

  lo_entity_set = lo_entity_type->create_entity_set( 'LogKapSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ===== DPC_EXT GET_ENTITYSET: vor "IF iv_entity_set_name = 'LogTaSet'" einfuegen =====
* Logistik live Teil C, docs/abap/ZLOG_KAP_ADD.abap
  IF iv_entity_set_name = 'LogKapSet'.
    TYPES: BEGIN OF ty_kp_kako,
             kapid TYPE kapid,
             name  TYPE kapname,
             kapar TYPE kapart,
             begzt TYPE kapbegzt,
             endzt TYPE kapbegzt,
             pause TYPE kapbegzt,
             ngrad TYPE nutzgrad,
             aznor TYPE kapanzahl,
             kalid TYPE cr_wfcid,
           END OF ty_kp_kako,
           BEGIN OF ty_kp_bed,
             kapid    TYPE kapid,
             kbedid   TYPE kbed-kbedid,
             fstad    TYPE kbed-fstad,
             fendd    TYPE kbed-fendd,
             keinh    TYPE kbed-keinh,
             kbearest TYPE cy_kbeares,
             kruerest TYPE cy_krueres,
           END OF ty_kp_bed,
           BEGIN OF ty_kp_wd,
             kal    TYPE cr_wfcid,
             datum  TYPE datum,
             arbeit TYPE c LENGTH 1,
           END OF ty_kp_wd,
           BEGIN OF ty_kp_agg,
             kapid TYPE kapid,
             tag   TYPE datum,
             h     TYPE f,
             ops   TYPE i,
           END OF ty_kp_agg,
           BEGIN OF ty_kp_opk,
             kapid  TYPE kapid,
             tag    TYPE datum,
             kbedid TYPE kbed-kbedid,
           END OF ty_kp_opk,
           BEGIN OF ty_kp_ca,
             objid TYPE cr_objid,
             kapid TYPE kapid,
           END OF ty_kp_ca,
           BEGIN OF ty_kp_ap,
             objid TYPE cr_objid,
             arbpl TYPE arbpl,
           END OF ty_kp_ap,
           BEGIN OF ty_kp_int,
             kapid TYPE kapid,
           END OF ty_kp_int.
    DATA: lv_kp_werks TYPE werks_d,
          lv_kp_von   TYPE datum,
          lv_kp_bis   TYPE datum,
          lv_kp_tag   TYPE datum,
          lv_kp_tage  TYPE i VALUE 7,
          lv_kp_i     TYPE i,
          lv_kp_fabkl TYPE fabkl,
          lv_kp_kal   TYPE cr_wfcid,
          lv_kp_ind   TYPE scal-indicator,
          lv_kp_std   TYPE f,
          lt_kp_kako  TYPE STANDARD TABLE OF ty_kp_kako,
          ls_kp_kako  TYPE ty_kp_kako,
          lt_kp_bed   TYPE STANDARD TABLE OF ty_kp_bed,
          ls_kp_bed   TYPE ty_kp_bed,
          lt_kp_wd    TYPE HASHED TABLE OF ty_kp_wd WITH UNIQUE KEY kal datum,
          ls_kp_wd    TYPE ty_kp_wd,
          lt_kp_agg   TYPE SORTED TABLE OF ty_kp_agg WITH UNIQUE KEY kapid tag,
          ls_kp_agg   TYPE ty_kp_agg,
          lt_kp_opk   TYPE HASHED TABLE OF ty_kp_opk WITH UNIQUE KEY kapid tag kbedid,
          ls_kp_opk   TYPE ty_kp_opk,
          lt_kp_days  TYPE STANDARD TABLE OF datum,
          lv_kp_s     TYPE datum,
          lv_kp_e     TYPE datum,
          lv_kp_d     TYPE datum,
          lv_kp_n     TYPE i,
          lv_kp_h     TYPE f,
          lv_kp_fak   TYPE f,
          lv_kp_zeile TYPE i,
          lv_kp_skip  TYPE i,
          lv_kp_max   TYPE i,
          lv_kp_grenze TYPE i,
          lt_kp_ca    TYPE STANDARD TABLE OF ty_kp_ca,
          ls_kp_ca    TYPE ty_kp_ca,
          lt_kp_ap    TYPE SORTED TABLE OF ty_kp_ap WITH UNIQUE KEY objid,
          ls_kp_ap    TYPE ty_kp_ap,
          lt_kp_int   TYPE SORTED TABLE OF ty_kp_int WITH UNIQUE KEY kapid,
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

*   Ohne Werk und gueltiges Datum kein Datenbankzugriff (Schutz fuer P76).
    lv_kp_tag = sy-datum - 30.
    lv_kp_bis = sy-datum + 60.
    IF lv_kp_werks IS INITIAL OR lv_kp_von IS INITIAL OR lv_kp_von < lv_kp_tag OR lv_kp_von > lv_kp_bis.
      copy_data_to_ref( EXPORTING is_data = lt_kp_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.
    IF lv_kp_tage < 1 OR lv_kp_tage > 14.
      lv_kp_tage = 7.
    ENDIF.
    lv_kp_bis = lv_kp_von + lv_kp_tage - 1.

    SELECT kapid name kapar begzt endzt pause ngrad aznor kalid FROM kako
      INTO CORRESPONDING FIELDS OF TABLE lt_kp_kako UP TO 200 ROWS
      WHERE werks = lv_kp_werks.
    IF lt_kp_kako IS INITIAL.
      copy_data_to_ref( EXPORTING is_data = lt_kp_out CHANGING cr_data = er_entityset ).
      RETURN.
    ENDIF.
    SELECT SINGLE fabkl FROM t001w INTO lv_kp_fabkl WHERE werks = lv_kp_werks.

*   Kapazitaeten mit Intervallen (KAPA): Standardangebot gilt dort nicht.
    SELECT kapid FROM kapa INTO TABLE lt_kp_int
      FOR ALL ENTRIES IN lt_kp_kako
      WHERE kapid = lt_kp_kako-kapid.

*   Bedarf ueber den Index KAPID: Vorgaenge, deren Zeitraum FSTAD..FENDD das Fenster beruehrt (FENDD leer =
*   nur FSTAD). Eine Zeile mehr als erlaubt, damit Abschneiden erkennbar ist.
    SELECT kapid kbedid fstad fendd keinh kbearest kruerest FROM kbed
      INTO CORRESPONDING FIELDS OF TABLE lt_kp_bed UP TO 20001 ROWS
      FOR ALL ENTRIES IN lt_kp_kako
      WHERE kapid = lt_kp_kako-kapid
        AND fstad <= lv_kp_bis
        AND ( fendd >= lv_kp_von OR ( fendd = '00000000' AND fstad >= lv_kp_von ) ).
    DESCRIBE TABLE lt_kp_bed LINES lv_kp_zeile.
    IF lv_kp_zeile > 20000.
      lv_kp_grenze = 20001.
      DELETE lt_kp_bed FROM lv_kp_grenze.
      mo_context->get_message_container( )->add_message_text_only(
        iv_msg_type = 'W'
        iv_msg_text = 'LogKapSet: Obergrenze 20000 Bedarfssaetze erreicht, Antwort unvollstaendig'
        iv_add_to_response_header = abap_true ).
    ENDIF.

*   Bedarf je Vorgang auf die Arbeitstage seines Zeitraums verteilen, nur Tage im Fenster zaehlen.
    LOOP AT lt_kp_bed INTO ls_kp_bed.
      CASE ls_kp_bed-keinh.
        WHEN 'MIN'.
          lv_kp_fak = '0.0166666667'.
        WHEN 'S'.
          lv_kp_fak = '0.000277777778'.
        WHEN OTHERS.   " H (und unbekannt)
          lv_kp_fak = 1.
      ENDCASE.
      lv_kp_h = ( ls_kp_bed-kbearest + ls_kp_bed-kruerest ) * lv_kp_fak.
      lv_kp_s = ls_kp_bed-fstad.
      lv_kp_e = ls_kp_bed-fendd.
      IF lv_kp_e < lv_kp_s.
        lv_kp_e = lv_kp_s.
      ENDIF.
      IF lv_kp_e - lv_kp_s > 400.
        lv_kp_e = lv_kp_s + 400.
      ENDIF.
      READ TABLE lt_kp_kako INTO ls_kp_kako WITH KEY kapid = ls_kp_bed-kapid.
      lv_kp_kal = ls_kp_kako-kalid.
      IF lv_kp_kal IS INITIAL.
        lv_kp_kal = lv_kp_fabkl.
      ENDIF.
      CLEAR lt_kp_days.
      lv_kp_d = lv_kp_s.
      WHILE lv_kp_d <= lv_kp_e.
        READ TABLE lt_kp_wd INTO ls_kp_wd WITH TABLE KEY kal = lv_kp_kal datum = lv_kp_d.
        IF sy-subrc <> 0.
          ls_kp_wd-kal = lv_kp_kal.
          ls_kp_wd-datum = lv_kp_d.
          ls_kp_wd-arbeit = 'X'.   " ohne Kalender oder bei Fehler: Arbeitstag
          IF lv_kp_kal IS NOT INITIAL.
            CLEAR lv_kp_ind.
            CALL FUNCTION 'DATE_CONVERT_TO_FACTORYDATE'
              EXPORTING
                date                 = lv_kp_d
                factory_calendar_id  = lv_kp_kal
                correct_option       = '+'
              IMPORTING
                workingday_indicator = lv_kp_ind
              EXCEPTIONS
                OTHERS               = 1.
            IF sy-subrc = 0 AND lv_kp_ind IS NOT INITIAL.
              CLEAR ls_kp_wd-arbeit.
            ENDIF.
          ENDIF.
          INSERT ls_kp_wd INTO TABLE lt_kp_wd.
        ENDIF.
        IF ls_kp_wd-arbeit = 'X'.
          APPEND lv_kp_d TO lt_kp_days.
        ENDIF.
        lv_kp_d = lv_kp_d + 1.
      ENDWHILE.
      IF lt_kp_days IS INITIAL.   " Zeitraum nur an freien Tagen: alles auf den Start
        APPEND lv_kp_s TO lt_kp_days.
      ENDIF.
      DESCRIBE TABLE lt_kp_days LINES lv_kp_n.
      lv_kp_h = lv_kp_h / lv_kp_n.
      LOOP AT lt_kp_days INTO lv_kp_d.
        CHECK lv_kp_d >= lv_kp_von AND lv_kp_d <= lv_kp_bis.
        CLEAR ls_kp_agg.
        ls_kp_agg-kapid = ls_kp_bed-kapid.
        ls_kp_agg-tag   = lv_kp_d.
        ls_kp_agg-h     = lv_kp_h.
        ls_kp_opk-kapid  = ls_kp_bed-kapid.
        ls_kp_opk-tag    = lv_kp_d.
        ls_kp_opk-kbedid = ls_kp_bed-kbedid.
        INSERT ls_kp_opk INTO TABLE lt_kp_opk.
        IF sy-subrc = 0.
          ls_kp_agg-ops = 1.   " verschiedener Vorgang an diesem Tag
        ENDIF.
        COLLECT ls_kp_agg INTO lt_kp_agg.
      ENDLOOP.
    ENDLOOP.

*   Arbeitsplaetze je Kapazitaet (Text).
    SELECT objid kapid FROM crca INTO CORRESPONDING FIELDS OF TABLE lt_kp_ca
      FOR ALL ENTRIES IN lt_kp_kako
      WHERE objty = 'A' AND kapid = lt_kp_kako-kapid.
    IF lt_kp_ca IS NOT INITIAL.
      SELECT objid arbpl FROM crhd INTO CORRESPONDING FIELDS OF TABLE lt_kp_ap
        FOR ALL ENTRIES IN lt_kp_ca
        WHERE objty = 'A' AND objid = lt_kp_ca-objid.
    ENDIF.

*   Nur Kapazitaeten mit Bedarf im Fenster ausgeben.
    LOOP AT lt_kp_kako INTO ls_kp_kako.
      READ TABLE lt_kp_agg TRANSPORTING NO FIELDS WITH KEY kapid = ls_kp_kako-kapid.
      CHECK sy-subrc = 0.
      lv_kp_kal = ls_kp_kako-kalid.
      IF lv_kp_kal IS INITIAL.
        lv_kp_kal = lv_kp_fabkl.
      ENDIF.
      CLEAR ls_kp_out.
      ls_kp_out-werks   = lv_kp_werks.
      ls_kp_out-datum   = lv_kp_von.
      ls_kp_out-tage    = lv_kp_tage.
      ls_kp_out-kapid   = ls_kp_kako-kapid.
      ls_kp_out-kapname = ls_kp_kako-name.
      ls_kp_out-kapar   = ls_kp_kako-kapar.
      LOOP AT lt_kp_ca INTO ls_kp_ca WHERE kapid = ls_kp_kako-kapid.
        READ TABLE lt_kp_ap INTO ls_kp_ap WITH TABLE KEY objid = ls_kp_ca-objid.
        IF sy-subrc = 0 AND strlen( ls_kp_out-arbpl ) < 70.
          CONCATENATE ls_kp_out-arbpl ls_kp_ap-arbpl INTO ls_kp_out-arbpl SEPARATED BY space.
        ENDIF.
      ENDLOOP.
      CONDENSE ls_kp_out-arbpl.
      READ TABLE lt_kp_int TRANSPORTING NO FIELDS WITH TABLE KEY kapid = ls_kp_kako-kapid.
      IF sy-subrc = 0.
        ls_kp_out-kein_standard = 'X'.
      ENDIF.
      DO lv_kp_tage TIMES.
        lv_kp_i = sy-index - 1.
        lv_kp_tag = lv_kp_von + lv_kp_i.
        ls_kp_out-tag = lv_kp_tag.
        CLEAR: ls_kp_out-bedarf_h, ls_kp_out-angebot_h, ls_kp_out-vorgaenge.
        READ TABLE lt_kp_agg INTO ls_kp_agg WITH TABLE KEY kapid = ls_kp_kako-kapid tag = lv_kp_tag.
        IF sy-subrc = 0.
          ls_kp_out-bedarf_h  = ls_kp_agg-h.
          ls_kp_out-vorgaenge = ls_kp_agg-ops.
        ENDIF.
        IF ls_kp_out-kein_standard IS INITIAL AND lv_kp_kal IS NOT INITIAL.
          CLEAR lv_kp_ind.
          CALL FUNCTION 'DATE_CONVERT_TO_FACTORYDATE'
            EXPORTING
              date                 = lv_kp_tag
              factory_calendar_id  = lv_kp_kal
              correct_option       = '+'
            IMPORTING
              workingday_indicator = lv_kp_ind
            EXCEPTIONS
              OTHERS               = 1.
          IF sy-subrc = 0 AND lv_kp_ind IS INITIAL.
            lv_kp_std = ( ls_kp_kako-endzt - ls_kp_kako-begzt - ls_kp_kako-pause ) / 3600.
            ls_kp_out-angebot_h = lv_kp_std * ls_kp_kako-ngrad / 100 * ls_kp_kako-aznor.
          ENDIF.
        ENDIF.
        APPEND ls_kp_out TO lt_kp_out.
      ENDDO.
    ENDLOOP.

    SORT lt_kp_out BY kapid tag.

*   Paging wie bei den anderen Sets (skip/top); der Client holt Seiten zu 2000 Zeilen.
    IF is_paging-skip > 0.
      lv_kp_skip = is_paging-skip.
      DELETE lt_kp_out TO lv_kp_skip.
    ENDIF.
    IF is_paging-top > 0.
      lv_kp_max = is_paging-top.
      DESCRIBE TABLE lt_kp_out LINES lv_kp_zeile.
      IF lv_kp_zeile > lv_kp_max.
        lv_kp_max = lv_kp_max + 1.
        DELETE lt_kp_out FROM lv_kp_max.
      ENDIF.
    ENDIF.
    copy_data_to_ref( EXPORTING is_data = lt_kp_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.
