*&---------------------------------------------------------------------*
*& MPC_EXT: Entity Type und Entity Set fuer Abwesenheiten je Fall
*& Klasse : ZCL_ZPOWERBI_EINKAUF_MPC_EXT
*& Methode: DEFINE  (redefiniert, dort liegen FinanzJournalSet und HrKpiSet)
*& Stand  : 2026-10-01
*&
*& EINFUEGEN, nicht ersetzen: dieser Block kommt in die bestehende
*& Redefinition von DEFINE, NACH dem HrKpi-Teil und VOR ENDMETHOD.
*&
*& Entscheid Ingo 2026-10-01: Krankheit und Ferien aus SAP statt Rexx, als
*& zweites Set mit einer Zeile je Abwesenheit (docs/HR_KPI.md 8.7). Geliefert
*& werden alle Abwesenheitsarten; welche als Krankheit oder Ferien zaehlen,
*& entscheidet das Cockpit, damit dafuer kein Transport noetig ist.
*&
*& Struktur ZSTR_HR_ABSENZ (SE11, Paket ZPP), Felder und Datenelemente:
*&   PERNR PERSNO   GJAHR GJAHR   AWART AWART   BEGDA CHAR8   ENDDA CHAR8
*&   SEQNR SEQNR    ABWTG ABWTG   STDAZ ABSTD   KALTG KALTG
*& BEGDA/ENDDA bewusst CHAR8 (JJJJMMTT) statt DATS: kein Edm.DateTime mit
*& Zeitzonenfragen, der Cockpit-Leser parst das Datum selbst.
*&---------------------------------------------------------------------*

* ---------------------------------------------------------------------
* HrAbsenz: eine Zeile je Abwesenheit aus PA2001
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'HrAbsenz'
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
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Awart'
                  iv_abap_fieldname = 'AWART' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Begda'
                  iv_abap_fieldname = 'BEGDA' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Endda'
                  iv_abap_fieldname = 'ENDDA' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Seqnr'
                  iv_abap_fieldname = 'SEQNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Abwtg'
                  iv_abap_fieldname = 'ABWTG' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Stdaz'
                  iv_abap_fieldname = 'STDAZ' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 7 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Kaltg'
                  iv_abap_fieldname = 'KALTG' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_HR_ABSENZ'
                                  iv_bind_conversions = 'X' ).

* Name ist die Vorgabe des Cockpit-Lesers (SapGatewayHrAbsenceReader).
  lo_entity_set = lo_entity_type->create_entity_set( 'HrAbsenzSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).
