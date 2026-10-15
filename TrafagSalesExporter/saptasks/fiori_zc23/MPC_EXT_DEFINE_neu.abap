*&---------------------------------------------------------------------*
*& MPC_EXT: Entity Type und Entity Set fuer das Hauptbuch-Journal CH/AT
*& Klasse : ZCL_ZPOWERBI_EINKAUF_MPC_EXT
*& Methode: DEFINE  (redefiniert)
*& Stand  : 2026-09-10, zweite Fassung
*&
*& Warum im Code statt in SEGW (Entscheid Ingo, 2026-09-10, "mach den
*& schnellsten weg"): SEGW haette Assistentendialoge in einer
*& Baumoberflaeche gebraucht. Der Codeweg ist schneller.
*&
*& PREIS, den man kennen muss: die uebrigen rund 25 EntitySets dieses
*& Service sind in SEGW gepflegt und stehen dort im Baum. Dieses eine
*& steht NUR hier. Wer den Service in SEGW oeffnet, findet es nicht.
*& Deshalb ist es in docs/abap/README_FIN_JOURNAL_ENTITYSET.md vermerkt.
*&
*& super->define( ) MUSS zuerst laufen, sonst faellt das gesamte in SEGW
*& gepflegte Modell weg und der Service liefert nur noch dieses eine Set.
*&
*& FEHLER DER ERSTEN FASSUNG, 2026-09-10: sie hat sich auf
*& bind_structure( ) verlassen und danach get_property( 'Bukrs' )
*& gerufen. bind_structure legt KEINE Properties an, es bindet nur die
*& ABAP-Struktur an bereits vorhandene. $metadata lief deshalb auf
*& HTTP 500 mit "Eigenschaft (externer Name) 'Bukrs' fuer Entitaet
*& 'FinanzJournal' nicht gefunden". Jede Property wird jetzt einzeln mit
*& create_property( ) angelegt, genau wie es SEGW im generierten
*& DEFINE_FINANZDATASCHWEIZOE derselben Klasse tut.
*&
*& Typen, Laengen und Nachkommastellen sind aus DD03L zu
*& ZSTR_FIN_JOURNAL gelesen, nicht geschaetzt. Die Umrechnung in die
*& OData-Angaben folgt dem generierten Vorbild in dieser Klasse:
*&   CHAR/NUMC/CUKY -> set_type_edm_string   + set_maxlength( Laenge )
*&   DATS           -> set_type_edm_datetime + set_precison( 7 )
*&   CURR           -> set_type_edm_decimal  + set_precison( 3 )
*&                     + set_maxlength( Laenge + 1 )
*& Das "+1" und die 3 bei CURR sind keine Erfindung: SEGW erzeugt fuer
*& NETWR_DC (CURR 15,2) genau 3/16 und fuer WAVWR_DC (CURR 13,2) 3/14.
*&---------------------------------------------------------------------*

METHOD define.

  DATA: lo_entity_type TYPE REF TO /iwbep/if_mgw_odata_entity_typ,
        lo_property    TYPE REF TO /iwbep/if_mgw_odata_property,
        lo_entity_set  TYPE REF TO /iwbep/if_mgw_odata_entity_set.

* Zuerst das vorhandene, in SEGW gepflegte Modell aufbauen.
  super->define( ).

  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'FinanzJournal'
                     iv_def_entity_set   = abap_false ).

* ---------------------------------------------------------------------
* Schluessel: die Belegnummer allein ist nicht eindeutig. Erst
* Buchungskreis, Geschaeftsjahr und Buchungszeile machen sie eindeutig.
* Genau so setzt der Leser JournalEntryId zusammen.
* ---------------------------------------------------------------------
  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bukrs'
                  iv_abap_fieldname = 'BUKRS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Belnr'
                  iv_abap_fieldname = 'BELNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_conversion_exit( 'ALPHA' ).
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
  lo_property->set_conversion_exit( 'GJAHR' ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Buzei'
                  iv_abap_fieldname = 'BUZEI' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

* ---------------------------------------------------------------------
* Belegkopf
* ---------------------------------------------------------------------
  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Budat'
                  iv_abap_fieldname = 'BUDAT' ).
  lo_property->set_type_edm_datetime( ).
  lo_property->set_precison( iv_precision = 7 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Monat'
                  iv_abap_fieldname = 'MONAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
* Filterbar seit 2026-09-11, damit der Leser monatsweise laden kann.
* Warum ueber die Buchungsperiode statt ueber zwei Datumsgrenzen: das Gateway
* liefert bei ZWEI Bedingungen auf DEMSELBEN Feld gar keine Filteroptionen
* aus, der Filter faellt dann komplett weg und der Data Provider liest alles.
* Gemessen am 2026-09-11: `Budat ge A and Budat lt B` lief 97 Sekunden auf
* HTTP 500, waehrend zwei Bedingungen auf VERSCHIEDENEN Feldern in 1,5
* Sekunden antworten. Deshalb ein Filter je Feld: `Gjahr` und `Monat`.
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Blart'
                  iv_abap_fieldname = 'BLART' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Xblnr'
                  iv_abap_fieldname = 'XBLNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 16 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Stblg'
                  iv_abap_fieldname = 'STBLG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_conversion_exit( 'ALPHA' ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Hwaer'
                  iv_abap_fieldname = 'HWAER' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Waers'
                  iv_abap_fieldname = 'WAERS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

* ---------------------------------------------------------------------
* Belegposition
* ---------------------------------------------------------------------
  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Hkont'
                  iv_abap_fieldname = 'HKONT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_conversion_exit( 'ALPHA' ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Hkonttxt'
                  iv_abap_fieldname = 'HKONTTXT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 50 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Shkzg'
                  iv_abap_fieldname = 'SHKZG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Dmbtr'
                  iv_abap_fieldname = 'DMBTR' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 24 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Wrbtr'
                  iv_abap_fieldname = 'WRBTR' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 24 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Kostl'
                  iv_abap_fieldname = 'KOSTL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_conversion_exit( 'ALPHA' ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Prctr'
                  iv_abap_fieldname = 'PRCTR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_conversion_exit( 'ALPHA' ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Sgtxt'
                  iv_abap_fieldname = 'SGTXT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 50 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

* ---------------------------------------------------------------------
* Faelligkeit und Ausgleich
* ---------------------------------------------------------------------
  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Faedt'
                  iv_abap_fieldname = 'FAEDT' ).
  lo_property->set_type_edm_datetime( ).
  lo_property->set_precison( iv_precision = 7 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_true ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Augdt'
                  iv_abap_fieldname = 'AUGDT' ).
  lo_property->set_type_edm_datetime( ).
  lo_property->set_precison( iv_precision = 7 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_true ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Augbl'
                  iv_abap_fieldname = 'AUGBL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_conversion_exit( 'ALPHA' ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

* ---------------------------------------------------------------------
* Erst jetzt die ABAP-Struktur binden. bind_structure legt keine
* Properties an, es verknuepft die vorhandenen mit den Feldern.
* ---------------------------------------------------------------------
  lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_FIN_JOURNAL'
                                  iv_bind_conversions = 'X' ).

* ---------------------------------------------------------------------
* Entity Set. Genau dieser Name ist die Vorgabe des Lesers; ein anderer
* muesste in Sites.SapEntitySet fuer ZSCHWEIZ eingetragen werden.
* pageable, weil der Data Provider $skip und $top selbst auswertet.
* ---------------------------------------------------------------------
  lo_entity_set = lo_entity_type->create_entity_set( 'FinanzJournalSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).


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

* Einkauf Kontrakte und LZ-Codes, docs/abap/ZEINK_KONTRAKT_ADD.abap
* ---------------------------------------------------------------------
* EinkKontrakt: EinkKontraktSet, optionaler Filter Bukrs
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'EinkKontrakt'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Ebeln' iv_abap_fieldname = 'EBELN' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Ebelp' iv_abap_fieldname = 'EBELP' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Bukrs' iv_abap_fieldname = 'BUKRS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Bsart' iv_abap_fieldname = 'BSART' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Lifnr' iv_abap_fieldname = 'LIFNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Txz01' iv_abap_fieldname = 'TXZ01' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matkl' iv_abap_fieldname = 'MATKL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 9 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Waers' iv_abap_fieldname = 'WAERS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Wkurs' iv_abap_fieldname = 'WKURS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kdatb' iv_abap_fieldname = 'KDATB' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Kdate' iv_abap_fieldname = 'KDATE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Loekz' iv_abap_fieldname = 'LOEKZ' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Meins' iv_abap_fieldname = 'MEINS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Ktmng' iv_abap_fieldname = 'KTMNG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Netpr' iv_abap_fieldname = 'NETPR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Peinh' iv_abap_fieldname = 'PEINH' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Zwert' iv_abap_fieldname = 'ZWERT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Abmng' iv_abap_fieldname = 'ABMNG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Abwrt' iv_abap_fieldname = 'ABWRT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_EINK_KONTRAKT' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'EinkKontraktSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ---------------------------------------------------------------------
* EinkMatLz: EinkMatLzSet (Lebenszyklus- und Sortiments-Code je Material)
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'EinkMatLz'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Lzcode' iv_abap_fieldname = 'LZCODE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Lzsort' iv_abap_fieldname = 'LZSORT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_EINK_MATLZ' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'EinkMatLzSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* Operations (Shopfloor), docs/abap/ZSHOP_ADD.abap
* ---------------------------------------------------------------------
* ShopZd05: ShopZd05Set, Pflichtfilter Werks und Dispo
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'ShopZd05'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Dispo' iv_abap_fieldname = 'DISPO' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Maktx' iv_abap_fieldname = 'MAKTX' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Udek' iv_abap_fieldname = 'UDEK' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Dismm' iv_abap_fieldname = 'DISMM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Lzcode' iv_abap_fieldname = 'LZCODE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'VerbrWbz' iv_abap_fieldname = 'VERBR_WBZ' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'SibeOpt' iv_abap_fieldname = 'SIBE_OPT' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'SibeAkt' iv_abap_fieldname = 'SIBE_AKT' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'AntPlan' iv_abap_fieldname = 'ANT_PLAN' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Tendenz' iv_abap_fieldname = 'TENDENZ' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_SHOP_ZD05' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'ShopZd05Set' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

* ---------------------------------------------------------------------
* ShopAuf: ShopAufSet, Pflichtfilter Werks und Datum, Tage 1..60
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'ShopAuf'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Aufnr' iv_abap_fieldname = 'AUFNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 12 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Typ' iv_abap_fieldname = 'TYP' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_nullable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Werks' iv_abap_fieldname = 'WERKS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Datum' iv_abap_fieldname = 'DATUM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 8 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Matnr' iv_abap_fieldname = 'MATNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Dispo' iv_abap_fieldname = 'DISPO' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Menge' iv_abap_fieldname = 'MENGE' ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_property = lo_entity_type->create_property( iv_property_name = 'Meins' iv_abap_fieldname = 'MEINS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_SHOP_AUF' iv_bind_conversions = abap_true ).
  lo_entity_set = lo_entity_type->create_entity_set( 'ShopAufSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).
  lo_entity_set->set_has_ftxt_search( abap_false ).
  lo_entity_set->set_subscribable( abap_false ).
  lo_entity_set->set_filter_required( abap_false ).

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

* ---------------------------------------------------------------------
* LogTa: LogTaSet, Pflichtfilter Datum (heute bis 7 Tage zurueck)
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
* LogLief: LogLiefSet, Pflichtfilter Datum (heute bis 7 Tage zurueck)
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
* LogRueck: LogRueckSet, Pflichtfilter Datum (heute bis 7 Tage zurueck)
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


*----------------------------------------------------------------------*
* ZPOWERBI_KDAUF: Standardwert, Materialanteil, Arbeit CZ (2026-10-15, ZC23,
* Entscheid Ingo, Transport T76K912736). Die Entity ist in SEGW gepflegt
* (DEFINE_ZPOWERBI_KDAUF); die zwei neuen Tabellenfelder (RKE_FIELDVALUE,
* DEC 15,2) kommen hier dazu, danach bind_structure erneut. Der Lesecode
* (SELECT * INTO CORRESPONDING) braucht keine Aenderung.
* Doku: saptasks/zc23_materialanteil/NOTES.md
*----------------------------------------------------------------------*
  lo_entity_type = model->get_entity_type( iv_entity_name = 'ZPOWERBI_KDAUF' ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Standardwert'
                  iv_abap_fieldname = 'STANDARDWERT' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Matanteil'
                  iv_abap_fieldname = 'MATANTEIL' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  " Arbeit CZ (Eigenleistung an CZ-Arbeitsplaetzen wie ZM03), 2026-10-15
  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'ArbeitCz'
                  iv_abap_fieldname = 'ARBEIT_CZ' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name   = 'ZPOWERBI_KDAUF'
                                  iv_bind_conversions = 'X' ).

* ---------------------------------------------------------------------
* Zc23Set: ZC23 (ZM_OFFENE_KDAUF) live mit Selektion, fuer die Fiori-App ZC23
* (saptasks/fiori_zc23, erzeugt mit erzeugen.py, Stand 2026-10-15).
* Pflichtfilter Werk, Datvon, Datbis (EQ); der DPC_EXT startet den Report mit P_APP.
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'Zc23'
                     iv_def_entity_set   = abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Rownr'
                  iv_abap_fieldname = 'ROWNR' ).
  lo_property->set_is_key( ).
  lo_property->set_type_edm_int32( ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Kwoche1'
                  iv_abap_fieldname = 'KWOCHE1' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Werk'
                  iv_abap_fieldname = 'WERK' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Spart'
                  iv_abap_fieldname = 'SPART' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Kmonat'
                  iv_abap_fieldname = 'KMONAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 7 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Edatu'
                  iv_abap_fieldname = 'EDATU' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Ketdat'
                  iv_abap_fieldname = 'KETDAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Vbeln'
                  iv_abap_fieldname = 'VBELN' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Posnr'
                  iv_abap_fieldname = 'POSNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Etenr'
                  iv_abap_fieldname = 'ETENR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Pstyv'
                  iv_abap_fieldname = 'PSTYV' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Matnr'
                  iv_abap_fieldname = 'MATNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bismt'
                  iv_abap_fieldname = 'BISMT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zztyp'
                  iv_abap_fieldname = 'ZZTYP' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 60 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Maktx'
                  iv_abap_fieldname = 'MAKTX' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 40 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bmeng'
                  iv_abap_fieldname = 'BMENG' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 13 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Netwr1'
                  iv_abap_fieldname = 'NETWR1' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Omeng'
                  iv_abap_fieldname = 'OMENG' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Netwr2'
                  iv_abap_fieldname = 'NETWR2' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Omengn'
                  iv_abap_fieldname = 'OMENGN' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Netwr3n'
                  iv_abap_fieldname = 'NETWR3N' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Kunnr'
                  iv_abap_fieldname = 'KUNNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Name1'
                  iv_abap_fieldname = 'NAME1' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 30 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Erdat'
                  iv_abap_fieldname = 'ERDAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Fkdat'
                  iv_abap_fieldname = 'FKDAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Dispo'
                  iv_abap_fieldname = 'DISPO' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 13 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'DispoDes'
                  iv_abap_fieldname = 'DISPO_DES' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 13 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Aufnr'
                  iv_abap_fieldname = 'AUFNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 12 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Auart'
                  iv_abap_fieldname = 'AUART' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Psmng'
                  iv_abap_fieldname = 'PSMNG' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 3 ).
  lo_property->set_maxlength( iv_max_length = 13 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Meins'
                  iv_abap_fieldname = 'MEINS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Gltri'
                  iv_abap_fieldname = 'GLTRI' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Gltrp'
                  iv_abap_fieldname = 'GLTRP' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Kwoche2'
                  iv_abap_fieldname = 'KWOCHE2' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Range'
                  iv_abap_fieldname = 'RANGE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 14 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zzlaenge'
                  iv_abap_fieldname = 'ZZLAENGE' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zzlaenge2'
                  iv_abap_fieldname = 'ZZLAENGE_2' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zzsp001'
                  iv_abap_fieldname = 'ZZSP001' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zzsp002'
                  iv_abap_fieldname = 'ZZSP002' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zzsp003'
                  iv_abap_fieldname = 'ZZSP003' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zzsp004'
                  iv_abap_fieldname = 'ZZSP004' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zzsp005'
                  iv_abap_fieldname = 'ZZSP005' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 6 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zzsondb'
                  iv_abap_fieldname = 'ZZSONDB' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 20 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bstdk'
                  iv_abap_fieldname = 'BSTDK' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Dzeit'
                  iv_abap_fieldname = 'DZEIT' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 0 ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Ftrmi'
                  iv_abap_fieldname = 'FTRMI' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zzmbdat'
                  iv_abap_fieldname = 'ZZMBDAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Fevor'
                  iv_abap_fieldname = 'FEVOR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 3 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Abgru'
                  iv_abap_fieldname = 'ABGRU' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Waers'
                  iv_abap_fieldname = 'WAERS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 5 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Belnr'
                  iv_abap_fieldname = 'BELNR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Gjahr'
                  iv_abap_fieldname = 'GJAHR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 4 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Blart'
                  iv_abap_fieldname = 'BLART' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Budat'
                  iv_abap_fieldname = 'BUDAT' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Wrbtr'
                  iv_abap_fieldname = 'WRBTR' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 23 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Augbl'
                  iv_abap_fieldname = 'AUGBL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Bschl'
                  iv_abap_fieldname = 'BSCHL' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Standardpreis'
                  iv_abap_fieldname = 'STANDARDPREIS' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 4 ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Anteil'
                  iv_abap_fieldname = 'ANTEIL' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 4 ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Standardwert'
                  iv_abap_fieldname = 'STANDARDWERT' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Matpreis'
                  iv_abap_fieldname = 'MATPREIS' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 4 ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Matanteil'
                  iv_abap_fieldname = 'MATANTEIL' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'ArbeitCz'
                  iv_abap_fieldname = 'ARBEIT_CZ' ).
  lo_property->set_type_edm_decimal( ).
  lo_property->set_precison( iv_precision = 2 ).
  lo_property->set_maxlength( iv_max_length = 15 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Datvon'
                  iv_abap_fieldname = 'DATVON' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Datbis'
                  iv_abap_fieldname = 'DATBIS' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 10 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Mata'
                  iv_abap_fieldname = 'MATA' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Mjung'
                  iv_abap_fieldname = 'MJUNG' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Mfrem'
                  iv_abap_fieldname = 'MFREM' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Korr'
                  iv_abap_fieldname = 'KORR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 1 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_true ).

  lo_entity_type->bind_structure( iv_structure_name   = 'ZSTR_ZC23_APP'
                                  iv_bind_conversions = 'X' ).

  lo_entity_set = lo_entity_type->create_entity_set( 'Zc23Set' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).

ENDMETHOD.
