*&---------------------------------------------------------------------*
*& Report Z_PRODSPARTE_REPORT
*&---------------------------------------------------------------------*
*& Zweck: ALV-Testreport fuer die flache Referenztabelle:
*&        MATNR, MAKTX, PAPH1, PAPH1_TEXT,
*&        WWPFA, WWPFA_TEXT, WWPSP, WWPSP_TEXT, IS_ASSIGNED.
*&
*& Kernlogik liegt in ZCL_PRODSPARTE_PROVIDER->GET_DATA( ).
*& Ein spaeterer SAP-Gateway/OData-Service soll dieselbe Methode nutzen.
*&---------------------------------------------------------------------*
REPORT z_prodsparte_report.

PARAMETERS p_vkorg TYPE vkorg OBLIGATORY.
PARAMETERS p_vtweg TYPE vtweg.
PARAMETERS p_spras TYPE spras DEFAULT sy-langu.
PARAMETERS p_fallb TYPE bezek DEFAULT 'Nicht zugeordnet'.

START-OF-SELECTION.

  DATA(lo_provider) = NEW zcl_prodsparte_provider( ).
  DATA(lt_result) = lo_provider->get_data(
    iv_vkorg    = p_vkorg
    iv_vtweg    = p_vtweg
    iv_spras    = p_spras
    iv_fallback = p_fallb ).

  IF lt_result IS INITIAL.
    MESSAGE 'Keine Daten - VKORG/VTWEG pruefen.' TYPE 'I'.
    RETURN.
  ENDIF.

  cl_salv_table=>factory(
    IMPORTING
      r_salv_table = DATA(lo_alv)
    CHANGING
      t_table      = lt_result ).

  lo_alv->get_functions( )->set_all( abap_true ).
  lo_alv->get_columns( )->set_optimize( abap_true ).
  lo_alv->display( ).
