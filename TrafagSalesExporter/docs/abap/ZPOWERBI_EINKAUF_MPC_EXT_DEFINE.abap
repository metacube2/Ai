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

ENDMETHOD.
