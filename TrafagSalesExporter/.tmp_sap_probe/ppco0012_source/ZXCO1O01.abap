*----------------------------------------------------------------------*
***INCLUDE ZXCO1O01.
*----------------------------------------------------------------------*
*&---------------------------------------------------------------------*
*& Module STATUS_0100 OUTPUT
*&---------------------------------------------------------------------*
*&
*&---------------------------------------------------------------------*
MODULE status_0100 OUTPUT.
* SET PF-STATUS 'xxxxxxxx'.
* SET TITLEBAR 'xxx'.

* Production date fill
  PERFORM  Fill_Prod_Date.


ENDMODULE.

FORM  Fill_Prod_Date.

*  MOVE gs_aufk-zzprdat TO caufvd-zzprdat.

"  IF sy-tcode EQ 'CO03'.
"    LOOP AT SCREEN.
"      IF screen-group1 = 'G1'.
"        screen-input = 0.
"        MODIFY SCREEN.
"      ENDIF.
"    ENDLOOP.
"  ENDIF.

ENDFORM.
