CLASS-POOL zcl_prodsparte_provider.

CLASS zcl_prodsparte_provider DEFINITION
  PUBLIC
  FINAL
  CREATE PUBLIC.

  PUBLIC SECTION.
    TYPES: BEGIN OF ty_out,
             matnr       TYPE matnr,
             maktx       TYPE maktx,
             paph1       TYPE ce11000-paph1,
             paph1_text  TYPE vtext,
             wwpfa       TYPE ce11000-wwpfa,
             wwpfa_text  TYPE bezek,
             wwpsp       TYPE ce11000-wwpsp,
             wwpsp_text  TYPE bezek,
             is_assigned TYPE abap_bool,
           END OF ty_out.
    TYPES tt_out TYPE STANDARD TABLE OF ty_out WITH DEFAULT KEY.

    METHODS get_data
      IMPORTING
        iv_vkorg    TYPE vkorg
        iv_vtweg    TYPE vtweg OPTIONAL
        iv_spras    TYPE spras DEFAULT sy-langu
        iv_fallback TYPE bezek DEFAULT 'Nicht zugeordnet'
      RETURNING
        VALUE(rt_out) TYPE tt_out.
ENDCLASS.

CLASS zcl_prodsparte_provider IMPLEMENTATION.
  METHOD get_data.
    TYPES: BEGIN OF ty_base,
             matnr TYPE mvke-matnr,
             vtweg TYPE mvke-vtweg,
             prodh TYPE mvke-prodh,
             maktx TYPE makt-maktx,
           END OF ty_base.

    DATA lt_base TYPE STANDARD TABLE OF ty_base WITH DEFAULT KEY.

    IF iv_vtweg IS INITIAL.
      SELECT mvke~matnr,
             mvke~vtweg,
             mvke~prodh,
             makt~maktx
        FROM mvke
        LEFT OUTER JOIN makt
          ON  makt~matnr = mvke~matnr
          AND makt~spras = @iv_spras
        INTO TABLE @lt_base
        WHERE mvke~vkorg = @iv_vkorg
          AND mvke~prodh <> @space.                "#EC CI_NOFIELD
    ELSE.
      SELECT mvke~matnr,
             mvke~vtweg,
             mvke~prodh,
             makt~maktx
        FROM mvke
        LEFT OUTER JOIN makt
          ON  makt~matnr = mvke~matnr
          AND makt~spras = @iv_spras
        INTO TABLE @lt_base
        WHERE mvke~vkorg = @iv_vkorg
          AND mvke~vtweg = @iv_vtweg
          AND mvke~prodh <> @space.                "#EC CI_NOFIELD
    ENDIF.

    IF lt_base IS INITIAL.
      RETURN.
    ENDIF.

    "Falls mehrere Vertriebswege gelesen werden, gewinnt bewusst der kleinste VTWEG.
    SORT lt_base BY matnr vtweg.
    DELETE ADJACENT DUPLICATES FROM lt_base COMPARING matnr.

    SELECT paph1, wwpfa, wwpsp
      FROM zprodsparte_map
      INTO TABLE @DATA(lt_map).                    "#EC CI_NOWHERE
    SORT lt_map BY paph1.

    SELECT prodh, vtext
      FROM t179t
      INTO TABLE @DATA(lt_h)
      WHERE spras = @iv_spras.                     "#EC CI_NOFIELD
    SORT lt_h BY prodh.

    SELECT wwpfa, bezek
      FROM t25a0
      INTO TABLE @DATA(lt_fam)
      WHERE spras = @iv_spras.                     "#EC CI_NOFIELD
    SORT lt_fam BY wwpfa.

    SELECT wwpsp, bezek
      FROM t25a1
      INTO TABLE @DATA(lt_spa)
      WHERE spras = @iv_spras.                     "#EC CI_NOFIELD
    SORT lt_spa BY wwpsp.

    LOOP AT lt_base INTO DATA(ls_base).
      DATA(ls_out) = VALUE ty_out(
        matnr       = ls_base-matnr
        maktx       = ls_base-maktx
        paph1       = ls_base-prodh(5)
        wwpsp       = 'UNASS'
        wwpsp_text  = iv_fallback
        is_assigned = abap_false ).

      READ TABLE lt_h INTO DATA(ls_h)
           WITH KEY prodh = ls_base-prodh BINARY SEARCH.
      IF sy-subrc = 0.
        ls_out-paph1_text = ls_h-vtext.
      ENDIF.

      READ TABLE lt_map INTO DATA(ls_m)
           WITH KEY paph1 = ls_out-paph1 BINARY SEARCH.
      IF sy-subrc = 0.
        ls_out-wwpfa = ls_m-wwpfa.
        ls_out-wwpsp = ls_m-wwpsp.
        ls_out-is_assigned = abap_true.

        READ TABLE lt_fam INTO DATA(ls_f)
             WITH KEY wwpfa = ls_m-wwpfa BINARY SEARCH.
        IF sy-subrc = 0.
          ls_out-wwpfa_text = ls_f-bezek.
        ENDIF.

        READ TABLE lt_spa INTO DATA(ls_s)
             WITH KEY wwpsp = ls_m-wwpsp BINARY SEARCH.
        IF sy-subrc = 0.
          ls_out-wwpsp_text = ls_s-bezek.
        ENDIF.
      ENDIF.

      APPEND ls_out TO rt_out.
    ENDLOOP.
  ENDMETHOD.
ENDCLASS.
