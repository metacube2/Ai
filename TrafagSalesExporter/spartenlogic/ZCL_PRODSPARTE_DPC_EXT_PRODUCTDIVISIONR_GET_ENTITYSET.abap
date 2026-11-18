  METHOD productdivisionr_get_entityset.
    DATA: lv_vkorg TYPE vkorg,
          lv_vtweg TYPE vtweg,
          lv_spras TYPE spras.

*   ProductDivisionRef enthaelt laut Gateway-Metadata kein VKORG-Feld.
*   Der Dashboard-Import kann deshalb keinen VKORG-Filter senden.
*   Default ist die fuehrende TR-AG-Verkaufsorganisation.
    lv_vkorg = '1100'.
    lv_spras = sy-langu.

    LOOP AT it_filter_select_options INTO DATA(ls_filter).
      READ TABLE ls_filter-select_options INTO DATA(ls_so) INDEX 1.
      IF sy-subrc <> 0.
        CONTINUE.
      ENDIF.

      DATA(lv_property) = ls_filter-property.
      TRANSLATE lv_property TO UPPER CASE.

      CASE lv_property.
        WHEN 'VKORG'.
          lv_vkorg = ls_so-low.
        WHEN 'VTWEG'.
          lv_vtweg = ls_so-low.
        WHEN 'SPRAS'.
          lv_spras = ls_so-low.
      ENDCASE.
    ENDLOOP.

    DATA(lt_data) = zcl_prodsparte_provider=>get_data(
                      iv_vkorg = lv_vkorg
                      iv_vtweg = lv_vtweg
                      iv_spras = lv_spras ).

    et_entityset = CORRESPONDING #( lt_data ).
  ENDMETHOD.
