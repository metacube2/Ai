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
                                  iv_bind_conversions = ' ' ).

  lo_entity_set = lo_entity_type->create_entity_set( 'Zc23Set' ).
  lo_entity_set->set_creatable( abap_false ).
  lo_entity_set->set_updatable( abap_false ).
  lo_entity_set->set_deletable( abap_false ).
  lo_entity_set->set_pageable( abap_true ).
  lo_entity_set->set_addressable( abap_true ).

