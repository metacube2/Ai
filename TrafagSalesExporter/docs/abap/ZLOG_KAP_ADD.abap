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
*& - KBED: Einheit H; Bedarf = KBEAREST + KRUEREST (Rest Bearbeiten + Ruesten). In T76 sind die
*&   Reste der vergangenen Tage 0 (erledigt), Soll vorhanden.
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
             fstad    TYPE fstad,
             kbearest TYPE cy_kbeares,
             kruerest TYPE cy_krueres,
           END OF ty_kp_bed,
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

*   Bedarf ueber den Index KAPID, Rest Bearbeiten + Ruesten im Fenster.
    SELECT kapid fstad kbearest kruerest FROM kbed
      INTO CORRESPONDING FIELDS OF TABLE lt_kp_bed UP TO 20000 ROWS
      FOR ALL ENTRIES IN lt_kp_kako
      WHERE kapid = lt_kp_kako-kapid
        AND fstad BETWEEN lv_kp_von AND lv_kp_bis.

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
      READ TABLE lt_kp_bed TRANSPORTING NO FIELDS WITH KEY kapid = ls_kp_kako-kapid.
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
        LOOP AT lt_kp_bed INTO ls_kp_bed WHERE kapid = ls_kp_kako-kapid AND fstad = lv_kp_tag.
          ls_kp_out-bedarf_h  = ls_kp_out-bedarf_h + ls_kp_bed-kbearest + ls_kp_bed-kruerest.
          ls_kp_out-vorgaenge = ls_kp_out-vorgaenge + 1.
        ENDLOOP.
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
    copy_data_to_ref( EXPORTING is_data = lt_kp_out CHANGING cr_data = er_entityset ).
    RETURN.
  ENDIF.
