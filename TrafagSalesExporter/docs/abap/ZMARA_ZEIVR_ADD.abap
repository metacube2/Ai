* ===== MPC_EXT DEFINE: am Ende, direkt vor ENDMETHOD einfuegen =========
*----------------------------------------------------------------------*
* MARA (MARA001Set): Zeichnungsindex ZEIVR (CHAR 2) fuer die
* Zeichnungsaenderung der Entwicklung (Simon Klaey, MS-Liste), 2026-11-02,
* Entscheid Ingo. Die Entity ist in SEGW gepflegt (DEFINE_MARA); das Feld
* kommt hier dazu, danach bind_structure erneut, wie bei ZPOWERBI_KDAUF.
* Der Lesecode (MARA001SET_GET_ENTITYSET, SELECT * INTO CORRESPONDING)
* braucht keine Aenderung. Doku: saptasks/zeichnungsaenderung/NOTES.md
*----------------------------------------------------------------------*
  lo_entity_type = model->get_entity_type( iv_entity_name = 'MARA' ).

  lo_property = lo_entity_type->create_property(
                  iv_property_name  = 'Zeivr'
                  iv_abap_fieldname = 'ZEIVR' ).
  lo_property->set_type_edm_string( ).
  lo_property->set_maxlength( iv_max_length = 2 ).
  lo_property->set_creatable( abap_false ).
  lo_property->set_updatable( abap_false ).
  lo_property->set_sortable( abap_false ).
  lo_property->set_nullable( abap_false ).
  lo_property->set_filterable( abap_false ).

  lo_entity_type->bind_structure( iv_structure_name   = 'MARA'
                                  iv_bind_conversions = 'X' ).
* ===== Ende MPC ========================================================
