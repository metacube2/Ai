*&---------------------------------------------------------------------*
*& MPC_EXT: Entity Type und Entity Set fuer HR-Kennzahlen je Person
*& Klasse : ZCL_ZPOWERBI_EINKAUF_MPC_EXT
*& Methode: DEFINE  (redefiniert, dort liegt schon FinanzJournalSet)
*& Stand  : 2026-09-30, Entwurf, NOCH NICHT im System
*&
*& EINFUEGEN, nicht ersetzen: dieser Block kommt in die bestehende
*& Redefinition von DEFINE, NACH dem FinanzJournal-Teil und VOR ENDMETHOD.
*& super->define( ) steht dort schon am Anfang und darf nicht doppelt kommen.
*& Die Variablen lo_entity_type, lo_property und lo_entity_set sind in der
*& Methode schon deklariert.
*&
*& Entscheid Ingo 2026-09-30: Live aus den PA-Tabellen, im Service
*& ZPOWERBI_EINKAUF_SRV, nur die Felder, die das Cockpit liest. Keine Namen,
*& kein Geburtsdatum, kein Lohn.
*&
*& Struktur ZSTR_HR_KPI (SE11, Paket ZPP), Felder und Datenelemente:
*&   PERNR PERSNO   GJAHR GJAHR   MONAT MONAT   BUKRS BUKRS   WERKS PERSA
*&   BTRTL BTRTL    PERSG PERSG   PERSK PERSK   TEILK TEILK   EMPCT EMPCT
*&   GESCH GESCH    PLANS PLANS   STELL STELL   NBU_TAGE ABWTG
*&   BU_TAGE ABWTG  ABKRS ABKRS
*&
*& VOR DEM AKTIVIEREN PRUEFEN: Laengen und Nachkommastellen von EMPCT und
*& ABWTG aus DD04L/DD01L lesen und die Decimal-Angaben unten daran angleichen.
*& Fuer CURR erzeugt SEGW in dieser Klasse precision 3 / maxlength Laenge+1;
*& fuer DEC ist das noch nicht gemessen (README_FIN_JOURNAL_ENTITYSET.md, 6a).
*&---------------------------------------------------------------------*

* ---------------------------------------------------------------------
* HrKpi: eine Zeile je Personalnummer und Monat
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'HrKpi'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Pernr'
                  iv_abap_fieldname = 'PERNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Gjahr'
                  iv_abap_fieldname = 'GJAHR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Monat'
                  iv_abap_fieldname = 'MONAT' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
* Ein Filter je Feld, wie beim Journal: zwei Bedingungen auf demselben
* Feld kommen beim Data Provider gar nicht an.
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bukrs'
                  iv_abap_fieldname = 'BUKRS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Werks'
                  iv_abap_fieldname = 'WERKS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Btrtl'
                  iv_abap_fieldname = 'BTRTL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Persg'
                  iv_abap_fieldname = 'PERSG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Persk'
                  iv_abap_fieldname = 'PERSK' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Teilk'
                  iv_abap_fieldname = 'TEILK' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Empct'
                  iv_abap_fieldname = 'EMPCT' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Gesch'
                  iv_abap_fieldname = 'GESCH' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Plans'
                  iv_abap_fieldname = 'PLANS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Stell'
                  iv_abap_fieldname = 'STELL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'NbuTage'
                  iv_abap_fieldname = 'NBU_TAGE' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'BuTage'
                  iv_abap_fieldname = 'BU_TAGE' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Abkrs'
                  iv_abap_fieldname = 'ABKRS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_HR_KPI'
                                  iv_bind_conversions = 'X' ).

* Name ist die Vorgabe des Cockpit-Lesers (SapGatewayHrKpiReader.EntitySet).
  lo_entity_set = lo_entity_type->create_entity_set( 'HrKpiSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).
