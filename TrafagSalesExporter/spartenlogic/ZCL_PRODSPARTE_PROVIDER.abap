CLASS-POOL zcl_prodsparte_provider.

CLASS zcl_prodsparte_provider DEFINITION
  PUBLIC
  FINAL
  CREATE PUBLIC.

  PUBLIC SECTION.
    TYPES: BEGIN OF ty_out,
             matnr       TYPE mvke-matnr,
             maktx       TYPE makt-maktx,
             paph1       TYPE ce11000-paph1,
             paph1_text  TYPE t179t-vtext,
             wwpfa       TYPE ce11000-wwpfa,
             wwpfa_text  TYPE t25a0-bezek,
             wwpsp       TYPE ce11000-wwpsp,
             wwpsp_text  TYPE t25a1-bezek,
             is_assigned TYPE abap_bool,
           END OF ty_out.
    TYPES tt_out TYPE STANDARD TABLE OF ty_out WITH DEFAULT KEY.

    CLASS-METHODS get_data
      IMPORTING
        iv_vkorg    TYPE vkorg
        iv_vtweg    TYPE vtweg OPTIONAL
        iv_spras    TYPE spras DEFAULT sy-langu
        iv_fallback TYPE t25a1-bezek DEFAULT 'Nicht zugeordnet'
      RETURNING
        VALUE(rt_out) TYPE tt_out.
ENDCLASS.

CLASS zcl_prodsparte_provider IMPLEMENTATION.
  METHOD get_data.
    TYPES: BEGIN OF ty_base,
             matnr TYPE mvke-matnr,
             vtweg TYPE mvke-vtweg,
             prodh TYPE mvke-prodh,
             paph1 TYPE ce11000-paph1,
             maktx TYPE makt-maktx,
           END OF ty_base.

    TYPES: BEGIN OF ty_map,
             paph1 TYPE zprodsparte_map-paph1,
             wwpfa TYPE zprodsparte_map-wwpfa,
             wwpsp TYPE zprodsparte_map-wwpsp,
           END OF ty_map.

    TYPES: BEGIN OF ty_component_candidate,
             kompnr      TYPE mvke-matnr,
             stufe       TYPE zpowerbi_vc_txt-stufe,
             paph1       TYPE ce11000-paph1,
             paph1_text  TYPE t179t-vtext,
             wwpfa       TYPE ce11000-wwpfa,
             wwpfa_text  TYPE t25a0-bezek,
             wwpsp       TYPE ce11000-wwpsp,
             wwpsp_text  TYPE t25a1-bezek,
           END OF ty_component_candidate.

    DATA: lt_base TYPE STANDARD TABLE OF ty_base WITH DEFAULT KEY,
          lt_map  TYPE STANDARD TABLE OF ty_map WITH DEFAULT KEY.

    IF iv_vtweg IS INITIAL.
      SELECT mvke~matnr,
             mvke~vtweg,
             mvke~prodh,
             makt~maktx
        FROM mvke
        LEFT OUTER JOIN makt
          ON  makt~matnr = mvke~matnr
          AND makt~spras = @iv_spras
        INTO CORRESPONDING FIELDS OF TABLE @lt_base
        WHERE mvke~vkorg = @iv_vkorg
          AND mvke~prodh <> @space.
    ELSE.
      SELECT mvke~matnr,
             mvke~vtweg,
             mvke~prodh,
             makt~maktx
        FROM mvke
        LEFT OUTER JOIN makt
          ON  makt~matnr = mvke~matnr
          AND makt~spras = @iv_spras
        INTO CORRESPONDING FIELDS OF TABLE @lt_base
        WHERE mvke~vkorg = @iv_vkorg
          AND mvke~vtweg = @iv_vtweg
          AND mvke~prodh <> @space.
    ENDIF.

    IF lt_base IS INITIAL.
      RETURN.
    ENDIF.

    LOOP AT lt_base ASSIGNING FIELD-SYMBOL(<ls_base>).
      <ls_base>-paph1 = <ls_base>-prodh(5).
    ENDLOOP.

    SORT lt_base BY matnr vtweg.
    DELETE ADJACENT DUPLICATES FROM lt_base COMPARING matnr.

    SELECT paph1, wwpfa, wwpsp
      FROM zprodsparte_map
      INTO TABLE @lt_map
      WHERE wwpfa <> @space.
    SORT lt_map BY paph1.

    SELECT prodh, vtext
      FROM t179t
      INTO TABLE @DATA(lt_h)
      WHERE spras = @iv_spras.
    SORT lt_h BY prodh.

    SELECT wwpfa, bezek
      FROM t25a0
      INTO TABLE @DATA(lt_fam)
      WHERE spras = @iv_spras.
    SORT lt_fam BY wwpfa.

    SELECT wwpsp, bezek
      FROM t25a1
      INTO TABLE @DATA(lt_spa)
      WHERE spras = @iv_spras.
    SORT lt_spa BY wwpsp.

    LOOP AT lt_base INTO DATA(ls_base).
      DATA(ls_out) = VALUE ty_out(
        matnr       = ls_base-matnr
        maktx       = ls_base-maktx
        paph1       = ls_base-paph1
        wwpsp       = 'UNASS'
        wwpsp_text  = iv_fallback
        is_assigned = abap_false ).

      READ TABLE lt_h INTO DATA(ls_h)
           WITH KEY prodh = ls_base-prodh BINARY SEARCH.
      IF sy-subrc <> 0.
        DATA(lv_prodh_key) = VALUE t179t-prodh( ).
        lv_prodh_key = ls_out-paph1.
        READ TABLE lt_h INTO ls_h
             WITH KEY prodh = lv_prodh_key BINARY SEARCH.
      ENDIF.
      IF sy-subrc = 0.
        ls_out-paph1_text = ls_h-vtext.
      ENDIF.

      READ TABLE lt_map INTO DATA(ls_map)
           WITH KEY paph1 = ls_out-paph1 BINARY SEARCH.
      IF sy-subrc = 0.
        ls_out-wwpfa       = ls_map-wwpfa.
        ls_out-wwpsp       = ls_map-wwpsp.
        ls_out-is_assigned = abap_true.

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
      ENDIF.

      APPEND ls_out TO rt_out.
    ENDLOOP.

    " Komponenten-Fallback: Komponente ueber eindeutige Produktsparte des Kopfartikels zuordnen.
    DATA lt_ref TYPE SORTED TABLE OF ty_out WITH UNIQUE KEY matnr.
    lt_ref = rt_out.

    SELECT DISTINCT stufe, kompnr, matnr
      FROM zpowerbi_vc_txt
      INTO TABLE @DATA(lt_component)
      WHERE kompnr <> @space
        AND matnr  <> @space.

    IF lt_component IS INITIAL.
      RETURN.
    ENDIF.

    DATA lt_candidate TYPE STANDARD TABLE OF ty_component_candidate WITH DEFAULT KEY.

    LOOP AT lt_component INTO DATA(ls_component).
      DATA(lv_kompnr_raw) = CONV string( ls_component-kompnr ).
      DATA(lv_head_raw)   = CONV string( ls_component-matnr ).
      CONDENSE: lv_kompnr_raw, lv_head_raw.

      IF lv_kompnr_raw IS INITIAL
      OR lv_head_raw   IS INITIAL
      OR lv_head_raw   CN '0123456789'.
        CONTINUE.
      ENDIF.

      DATA(lv_kompnr) = VALUE mvke-matnr( ).
      IF lv_kompnr_raw CO '0123456789'.
        lv_kompnr = |{ lv_kompnr_raw ALPHA = IN WIDTH = 18 }|.
      ELSE.
        lv_kompnr = lv_kompnr_raw.
      ENDIF.

      READ TABLE lt_ref TRANSPORTING NO FIELDS
           WITH KEY matnr = lv_kompnr.
      IF sy-subrc = 0.
        CONTINUE.
      ENDIF.

      DATA(lv_head_matnr) = VALUE mvke-matnr( ).
      lv_head_matnr = |{ lv_head_raw ALPHA = IN WIDTH = 18 }|.

      READ TABLE lt_ref INTO DATA(ls_parent)
           WITH KEY matnr = lv_head_matnr.
      IF sy-subrc <> 0
      OR ls_parent-is_assigned <> abap_true.
        CONTINUE.
      ENDIF.

      APPEND VALUE ty_component_candidate(
        kompnr     = lv_kompnr
        stufe      = ls_component-stufe
        paph1      = ls_parent-paph1
        paph1_text = ls_parent-paph1_text
        wwpfa      = ls_parent-wwpfa
        wwpfa_text = ls_parent-wwpfa_text
        wwpsp      = ls_parent-wwpsp
        wwpsp_text = ls_parent-wwpsp_text ) TO lt_candidate.
    ENDLOOP.

    SORT lt_candidate BY kompnr stufe paph1 wwpfa wwpsp.
    DELETE ADJACENT DUPLICATES FROM lt_candidate
      COMPARING kompnr stufe paph1 wwpfa wwpsp.

    DATA lv_candidate_count TYPE i.
    DATA ls_component_out TYPE ty_out.
    DATA lv_current_kompnr TYPE mvke-matnr.
    DATA lv_has_group TYPE abap_bool.
    DATA lv_group_wwpsp TYPE ce11000-wwpsp.
    DATA lv_is_ambiguous TYPE abap_bool.

    LOOP AT lt_candidate INTO DATA(ls_candidate).
      IF lv_has_group = abap_false
      OR lv_current_kompnr <> ls_candidate-kompnr.
        IF lv_has_group = abap_true
        AND lv_candidate_count > 0
        AND lv_is_ambiguous = abap_false.
          INSERT ls_component_out INTO TABLE lt_ref.
          IF sy-subrc = 0.
            APPEND ls_component_out TO rt_out.
          ENDIF.
        ENDIF.

        lv_has_group = abap_true.
        lv_current_kompnr = ls_candidate-kompnr.
        CLEAR: lv_candidate_count,
               ls_component_out,
               lv_group_wwpsp,
               lv_is_ambiguous.
      ENDIF.

      lv_candidate_count = lv_candidate_count + 1.

      IF lv_candidate_count = 1.
        lv_group_wwpsp = ls_candidate-wwpsp.
        ls_component_out = VALUE ty_out(
          matnr       = ls_candidate-kompnr
          paph1       = ls_candidate-paph1
          paph1_text  = ls_candidate-paph1_text
          wwpfa       = ls_candidate-wwpfa
          wwpfa_text  = ls_candidate-wwpfa_text
          wwpsp       = ls_candidate-wwpsp
          wwpsp_text  = ls_candidate-wwpsp_text
          is_assigned = abap_true ).
      ELSEIF ls_candidate-wwpsp <> lv_group_wwpsp.
        lv_is_ambiguous = abap_true.
      ENDIF.
    ENDLOOP.

    IF lv_has_group = abap_true
    AND lv_candidate_count > 0
    AND lv_is_ambiguous = abap_false.
      INSERT ls_component_out INTO TABLE lt_ref.
      IF sy-subrc = 0.
        APPEND ls_component_out TO rt_out.
      ENDIF.
    ENDIF.
  ENDMETHOD.
ENDCLASS.
