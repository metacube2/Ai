*&---------------------------------------------------------------------*
*& MPC_EXT: Logistik live (Kommissionierung, Lieferungen, Rueckmeldungen)
*& Klasse : ZCL_ZPOWERBI_EINKAUF_MPC_EXT, Methode DEFINE (redefiniert)
*& Stand  : 2026-10-01. EINFUEGEN nach dem HrAbsenz-Teil, VOR ENDMETHOD.
*& Entscheid Ingo 2026-10-01: keine Personenfelder. *Ueberholt 2026-10-05:* TA-Benutzer Bname, Ename,
*& Qname mit HR-Freigabe laut Ingo; Anzeige nur nach Anmeldung. Kein PERNR.
*&---------------------------------------------------------------------*

* ---------------------------------------------------------------------
* LogTa: LogTaSet, Pflichtfilter Datum (genau ein Tag)
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'LogTa'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Lgnum'
                  iv_abap_fieldname = 'LGNUM' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Tanum'
                  iv_abap_fieldname = 'TANUM' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Tapos'
                  iv_abap_fieldname = 'TAPOS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Datum'
                  iv_abap_fieldname = 'DATUM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Abzeit'
                  iv_abap_fieldname = 'ABZEIT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bwlvs'
                  iv_abap_fieldname = 'BWLVS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Vbeln'
                  iv_abap_fieldname = 'VBELN' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Matnr'
                  iv_abap_fieldname = 'MATNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
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
                  iv_property_name  = 'Vltyp'
                  iv_abap_fieldname = 'VLTYP' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Vlpla'
                  iv_abap_fieldname = 'VLPLA' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Nltyp'
                  iv_abap_fieldname = 'NLTYP' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Nlpla'
                  iv_abap_fieldname = 'NLPLA' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Menge'
                  iv_abap_fieldname = 'MENGE' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 14 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Meins'
                  iv_abap_fieldname = 'MEINS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bdatu'
                  iv_abap_fieldname = 'BDATU' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bzeit'
                  iv_abap_fieldname = 'BZEIT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Pquit'
                  iv_abap_fieldname = 'PQUIT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Qdatu'
                  iv_abap_fieldname = 'QDATU' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Qzeit'
                  iv_abap_fieldname = 'QZEIT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

* 2026-10-05: TA-Benutzer (HR-Freigabe laut Ingo), Anzeige im Cockpit nur nach Anmeldung.
  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bname'
                  iv_abap_fieldname = 'BNAME' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 12 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Ename'
                  iv_abap_fieldname = 'ENAME' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 12 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Qname'
                  iv_abap_fieldname = 'QNAME' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 12 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_LOG_TA'
                                  iv_bind_conversions = 'X' ).

  lo_entity_set = lo_entity_type->create_entity_set( 'LogTaSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ---------------------------------------------------------------------
* LogLief: LogLiefSet, Pflichtfilter Datum (genau ein Tag)
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'LogLief'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Vbeln'
                  iv_abap_fieldname = 'VBELN' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Datum'
                  iv_abap_fieldname = 'DATUM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Lfart'
                  iv_abap_fieldname = 'LFART' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Vstel'
                  iv_abap_fieldname = 'VSTEL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Lgnum'
                  iv_abap_fieldname = 'LGNUM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Kunnr'
                  iv_abap_fieldname = 'KUNNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Name1'
                  iv_abap_fieldname = 'NAME1' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 35 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Kostk'
                  iv_abap_fieldname = 'KOSTK' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Wbstk'
                  iv_abap_fieldname = 'WBSTK' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Erzet'
                  iv_abap_fieldname = 'ERZET' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'PosGes'
                  iv_abap_fieldname = 'POS_GES' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'PosKomm'
                  iv_abap_fieldname = 'POS_KOMM' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'PosTeil'
                  iv_abap_fieldname = 'POS_TEIL' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

* 2026-10-05: Warenausgang geplant und ist, Uhrzeit der Warenbewegung.
  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Wadat'
                  iv_abap_fieldname = 'WADAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'WadatIst'
                  iv_abap_fieldname = 'WADAT_IST' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'WaZeit'
                  iv_abap_fieldname = 'WA_ZEIT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

* Nachtrag 2026-10-05: Filter "ueberfaellig" (ZSTR_LOG_LIEF-UEBERF, CHAR1); nur als Filter 'X' benutzt.
  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Ueberf'
                  iv_abap_fieldname = 'UEBERF' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_LOG_LIEF'
                                  iv_bind_conversions = 'X' ).

  lo_entity_set = lo_entity_type->create_entity_set( 'LogLiefSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ---------------------------------------------------------------------
* LogRueck: LogRueckSet, Pflichtfilter Datum (genau ein Tag)
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'LogRueck'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Rueck'
                  iv_abap_fieldname = 'RUECK' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Rmzhl'
                  iv_abap_fieldname = 'RMZHL' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Datum'
                  iv_abap_fieldname = 'DATUM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Abzeit'
                  iv_abap_fieldname = 'ABZEIT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Aufnr'
                  iv_abap_fieldname = 'AUFNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 12 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Vornr'
                  iv_abap_fieldname = 'VORNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Arbpl'
                  iv_abap_fieldname = 'ARBPL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
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
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Ersda'
                  iv_abap_fieldname = 'ERSDA' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Erzet'
                  iv_abap_fieldname = 'ERZET' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Lmnga'
                  iv_abap_fieldname = 'LMNGA' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 14 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Xmnga'
                  iv_abap_fieldname = 'XMNGA' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 14 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Meinh'
                  iv_abap_fieldname = 'MEINH' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Aueru'
                  iv_abap_fieldname = 'AUERU' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Stokz'
                  iv_abap_fieldname = 'STOKZ' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

* Nachtrag 2026-10-05: Stornozaehler (ZSTR_LOG_RUECK-STZHL, NUMC 8); das Cockpit liest ihn optional.
  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Stzhl'
                  iv_abap_fieldname = 'STZHL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Matnr'
                  iv_abap_fieldname = 'MATNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Gamng'
                  iv_abap_fieldname = 'GAMNG' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 14 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Igmng'
                  iv_abap_fieldname = 'IGMNG' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 14 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_LOG_RUECK'
                                  iv_bind_conversions = 'X' ).

  lo_entity_set = lo_entity_type->create_entity_set( 'LogRueckSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).
