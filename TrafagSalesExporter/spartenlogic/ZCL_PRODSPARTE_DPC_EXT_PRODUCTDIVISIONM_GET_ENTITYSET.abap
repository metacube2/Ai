  METHOD productdivisionm_get_entityset.
    TYPES: BEGIN OF ty_out,
             paph1        TYPE zprodsparte_map-paph1,
             paph1_text   TYPE t179t-vtext,
             wwpfa        TYPE zprodsparte_map-wwpfa,
             wwpfa_text   TYPE t25a0-bezek,
             wwpsp        TYPE zprodsparte_map-wwpsp,
             wwpsp_text   TYPE t25a1-bezek,
             is_assigned  TYPE abap_bool,
           END OF ty_out.

    DATA: lv_spras TYPE spras.
    lv_spras = sy-langu.

    LOOP AT it_filter_select_options INTO DATA(ls_filter).
      READ TABLE ls_filter-select_options INTO DATA(ls_so) INDEX 1.
      IF sy-subrc <> 0.
        CONTINUE.
      ENDIF.

      DATA(lv_property) = ls_filter-property.
      TRANSLATE lv_property TO UPPER CASE.

      CASE lv_property.
        WHEN 'SPRAS'.
          lv_spras = ls_so-low.
      ENDCASE.
    ENDLOOP.

    SELECT paph1, wwpfa, wwpsp
      FROM zprodsparte_map
      INTO TABLE @DATA(lt_map)
      WHERE paph1 <> @space.

    IF lt_map IS INITIAL.
      RETURN.
    ENDIF.

    SORT lt_map BY paph1.
    DELETE ADJACENT DUPLICATES FROM lt_map COMPARING paph1.

    SELECT prodh, vtext
      FROM t179t
      INTO TABLE @DATA(lt_h)
      WHERE spras = @lv_spras.
    SORT lt_h BY prodh.

    SELECT wwpfa, bezek
      FROM t25a0
      INTO TABLE @DATA(lt_fam)
      WHERE spras = @lv_spras.
    SORT lt_fam BY wwpfa.

    SELECT wwpsp, bezek
      FROM t25a1
      INTO TABLE @DATA(lt_spa)
      WHERE spras = @lv_spras.
    SORT lt_spa BY wwpsp.

    DATA lt_out TYPE STANDARD TABLE OF ty_out WITH DEFAULT KEY.

    LOOP AT lt_map INTO DATA(ls_map).
      DATA(ls_out) = VALUE ty_out(
        paph1       = ls_map-paph1
        wwpfa       = ls_map-wwpfa
        wwpsp       = ls_map-wwpsp
        is_assigned = abap_true ).

      READ TABLE lt_h INTO DATA(ls_h)
           WITH KEY prodh = ls_map-paph1 BINARY SEARCH.
      IF sy-subrc = 0.
        ls_out-paph1_text = ls_h-vtext.
      ENDIF.

      READ TABLE lt_fam INTO DATA(ls_f)
           WITH KEY wwpfa = ls_map-wwpfa BINARY SEARCH.
      IF sy-subrc = 0.
        ls_out-wwpfa_text = ls_f-bezek.
      ENDIF.

      READ TABLE lt_spa INTO DATA(ls_s)
           WITH KEY wwpsp = ls_map-wwpsp BINARY SEARCH.
      IF sy-subrc = 0.
        ls_out-wwpsp_text = ls_s-bezek.
      ENDIF.

      APPEND ls_out TO lt_out.
    ENDLOOP.

    et_entityset = CORRESPONDING #( lt_out ).
  ENDMETHOD.
