*&---------------------------------------------------------------------*
*& MPC_EXT: Entity Type und Entity Set fuer das Hauptbuch-Journal CH/AT
*& Klasse : ZCL_ZPOWERBI_EINKAUF_MPC_EXT
*& Methode: DEFINE  (redefiniert)
*& Stand  : 2026-09-10
*&
*& Warum im Code statt in SEGW (Entscheid Ingo, 2026-09-10, „mach den
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
*&---------------------------------------------------------------------*

METHOD define.

  DATA: lo_entity_type TYPE REF TO /iwbep/if_mgw_odata_entity_typ,
        lo_property    TYPE REF TO /iwbep/if_mgw_odata_property,
        lo_entity_set  TYPE REF TO /iwbep/if_mgw_odata_entity_set.

* Zuerst das vorhandene, in SEGW gepflegte Modell aufbauen.
  super->define( ).

* ---------------------------------------------------------------------
* Entity Type aus der DDIC-Struktur ZSTR_FIN_JOURNAL
* Die Struktur ist am 2026-09-10 in Paket ZPP angelegt und aktiv.
* bind_structure legt die Properties automatisch an; die Namen werden
* dabei von BUKRS auf Bukrs umgesetzt.
* ---------------------------------------------------------------------
  lo_entity_type = model->create_entity_type(
                     iv_entity_type_name = 'FinanzJournal'
                     iv_def_entity_set   = abap_false ).

  lo_entity_type->bind_structure( iv_structure_name = 'ZSTR_FIN_JOURNAL' ).

* ---------------------------------------------------------------------
* Schluessel: die Belegnummer allein ist nicht eindeutig. Erst
* Buchungskreis, Geschaeftsjahr und Buchungszeile machen sie eindeutig.
* Genau so setzt der Leser JournalEntryId zusammen.
* ---------------------------------------------------------------------
  lo_property = lo_entity_type->get_property( iv_property_name = 'Bukrs' ).
  lo_property->set_is_key( ).

  lo_property = lo_entity_type->get_property( iv_property_name = 'Belnr' ).
  lo_property->set_is_key( ).

  lo_property = lo_entity_type->get_property( iv_property_name = 'Gjahr' ).
  lo_property->set_is_key( ).

  lo_property = lo_entity_type->get_property( iv_property_name = 'Buzei' ).
  lo_property->set_is_key( ).

* ---------------------------------------------------------------------
* Filterbare Felder. Ohne diese Kennzeichen weist das Gateway $filter
* auf Bukrs, Gjahr und Budat zurueck, und dann kaeme bei jedem Aufruf
* der volle Bestand einschliesslich der Schweiz.
* ---------------------------------------------------------------------
  lo_property = lo_entity_type->get_property( iv_property_name = 'Budat' ).
  lo_property->set_filterable( ).

  lo_property = lo_entity_type->get_property( iv_property_name = 'Blart' ).
  lo_property->set_filterable( ).

* ---------------------------------------------------------------------
* Entity Set. Genau dieser Name ist die Vorgabe des Lesers; ein anderer
* muesste in Sites.SapEntitySet fuer ZSCHWEIZ eingetragen werden.
* ---------------------------------------------------------------------
  lo_entity_set = lo_entity_type->create_entity_set( iv_entity_set_name = 'FinanzJournalSet' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).

ENDMETHOD.
