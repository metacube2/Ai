REPORT zppwr_class_setup.

* Einmaliger, idempotenter Aufbau der PPWR-/Compliance-Klassifizierung.
* Zielsystem: T76, Mandant 090. Keine Materialzuordnungen, keine P76-Aenderung.

PARAMETERS p_write AS CHECKBOX DEFAULT space.

TYPES: BEGIN OF ty_char_def,
         name     TYPE atnam,
         text     TYPE atbez,
         datatype TYPE c LENGTH 4,
         length   TYPE i,
         decimals TYPE i,
         class_id TYPE c LENGTH 1,
       END OF ty_char_def.

DATA: gt_defs       TYPE STANDARD TABLE OF ty_char_def,
      gs_def        TYPE ty_char_def,
      gs_detail     TYPE bapicharactdetail,
      gs_descr      TYPE bapicharactdescr,
      gt_descr      TYPE STANDARD TABLE OF bapicharactdescr,
      gs_val_char   TYPE bapicharactvalueschar,
      gt_val_char   TYPE STANDARD TABLE OF bapicharactvalueschar,
      gs_val_num    TYPE bapicharactvaluesnum,
      gt_val_num    TYPE STANDARD TABLE OF bapicharactvaluesnum,
      gs_val_descr  TYPE bapicharactvaluesdescr,
      gt_val_descr  TYPE STANDARD TABLE OF bapicharactvaluesdescr,
      gt_return     TYPE STANDARD TABLE OF bapiret2,
      gs_return     TYPE bapiret2,
      gv_error      TYPE c LENGTH 1,
      gv_exists     TYPE c LENGTH 1,
      gv_atinn      TYPE cabn-atinn,
      gv_tabix      TYPE sy-tabix.

DATA: gs_class_basic TYPE bapi1003_basic,
      gs_class_desc  TYPE bapi1003_catch,
      gt_class_desc  TYPE STANDARD TABLE OF bapi1003_catch,
      gs_class_char  TYPE bapi1003_charact,
      gt_class_char  TYPE STANDARD TABLE OF bapi1003_charact.

START-OF-SELECTION.
  IF sy-sysid <> 'T76' OR sy-mandt <> '090'.
    WRITE: / 'ABBRUCH: Report darf nur in T76/090 laufen.',
           / 'Aktuell:', sy-sysid, sy-mandt.
    RETURN.
  ENDIF.

  PERFORM build_catalog.

  IF p_write IS INITIAL.
    WRITE: / 'PRUEFLAUF: Es wird nichts geschrieben.',
           / 'Objektkatalog:'.
    LOOP AT gt_defs INTO gs_def.
      WRITE: / gs_def-class_id, gs_def-name, gs_def-datatype,
               gs_def-length, gs_def-decimals, gs_def-text.
    ENDLOOP.
    WRITE: / 'Klasse P: ZPPWR_PACKMITTEL',
           / 'Klasse C: ZCOMP_STOFF'.
    RETURN.
  ENDIF.

  LOOP AT gt_defs INTO gs_def.
    PERFORM create_characteristic USING gs_def.
    IF gv_error = 'X'.
      EXIT.
    ENDIF.
  ENDLOOP.

  IF gv_error IS INITIAL.
    CALL FUNCTION 'BAPI_TRANSACTION_COMMIT'
      EXPORTING
        wait = 'X'.
    WRITE: / 'Merkmalphase committed; Klassenphase startet.'.
  ELSE.
    CALL FUNCTION 'BAPI_TRANSACTION_ROLLBACK'.
    WRITE: / 'ABBRUCH in Merkmalphase; Rollback ausgefuehrt.' COLOR COL_NEGATIVE.
    RETURN.
  ENDIF.

  IF gv_error IS INITIAL.
    PERFORM create_class USING 'ZPPWR_PACKMITTEL' 'PPWR Packmittel' 'P'.
  ENDIF.
  IF gv_error IS INITIAL.
    PERFORM create_class USING 'ZCOMP_STOFF' 'Stoffcompliance Interim' 'C'.
  ENDIF.

  IF gv_error = 'X'.
    CALL FUNCTION 'BAPI_TRANSACTION_ROLLBACK'.
    WRITE: / 'ABBRUCH: Fehler erkannt, Rollback ausgefuehrt.' COLOR COL_NEGATIVE.
  ELSE.
    CALL FUNCTION 'BAPI_TRANSACTION_COMMIT'
      EXPORTING
        wait = 'X'.
    WRITE: / 'FERTIG: Merkmale und Klassen angelegt/geprueft.' COLOR COL_POSITIVE.
  ENDIF.

FORM add_def USING VALUE(iv_name) TYPE atnam
                   VALUE(iv_text) TYPE atbez
                   VALUE(iv_type) TYPE bapicharactdetail-data_type
                   VALUE(iv_len)  TYPE i
                   VALUE(iv_dec)  TYPE i
                   VALUE(iv_cls)  TYPE c.
  CLEAR gs_def.
  gs_def-name     = iv_name.
  gs_def-text     = iv_text.
  gs_def-datatype = iv_type.
  gs_def-length   = iv_len.
  gs_def-decimals = iv_dec.
  gs_def-class_id = iv_cls.
  APPEND gs_def TO gt_defs.
ENDFORM.

FORM build_catalog.
  PERFORM add_def USING 'ZPPWR_RECYCL_CLASS'  'Recyclability Class'             'CHAR' 1  0 'P'.
  PERFORM add_def USING 'ZPPWR_RECYCLAT_PCT'  'Total Recycled Content %'        'NUM'  5  2 'P'.
  PERFORM add_def USING 'ZPPWR_PCR_PCT'       'PCR Content %'                   'NUM'  5  2 'P'.
  PERFORM add_def USING 'ZPPWR_DECL_STATUS'   'Lieferantenerklaerung Status'    'CHAR' 9  0 'P'.
  PERFORM add_def USING 'ZPPWR_DECL_DATE'     'Lieferantenerklaerung Datum'     'DATE' 8  0 'P'.
  PERFORM add_def USING 'ZPPWR_VALID_TO'      'Liefererklaerung gueltig bis'      'DATE' 8 0 'P'.
  PERFORM add_def USING 'ZPPWR_DECL_REF'      'Lieferantenerklaerung Referenz'  'CHAR' 30 0 'P'.
  PERFORM add_def USING 'ZPPWR_DATA_DATE'     'Datenstand Verpackung'           'DATE' 8  0 'P'.
  PERFORM add_def USING 'ZPPWR_FOOD_CONTACT'  'Lebensmittelkontakt'             'CHAR' 9  0 'P'.

  PERFORM add_def USING 'ZCOMP_REACH_STATUS'  'REACH Status'                    'CHAR' 13 0 'C'.
  PERFORM add_def USING 'ZCOMP_REACH_DATE'    'REACH Bewertungsstand'           'DATE' 8  0 'C'.
  PERFORM add_def USING 'ZCOMP_SVHC_STATUS'   'SVHC Status'                     'CHAR' 13 0 'C'.
  PERFORM add_def USING 'ZCOMP_SVHC_LISTDAT'  'SVHC Kandidatenliste Stand'      'DATE' 8  0 'C'.
  PERFORM add_def USING 'ZCOMP_ROHS_STATUS'   'RoHS Status'                     'CHAR' 13 0 'C'.
  PERFORM add_def USING 'ZCOMP_ROHS_DATE'     'RoHS Bewertungsstand'            'DATE' 8  0 'C'.
  PERFORM add_def USING 'ZCOMP_PFAS_STATUS'   'PFAS Status'                     'CHAR' 13 0 'C'.
  PERFORM add_def USING 'ZCOMP_PFAS_DATE'     'PFAS Bewertungsstand'            'DATE' 8  0 'C'.
  PERFORM add_def USING 'ZCOMP_DECL_STATUS'   'Lieferantenerklaerung Status'    'CHAR' 9  0 'C'.
  PERFORM add_def USING 'ZCOMP_DECL_DATE'     'Lieferantenerklaerung Datum'     'DATE' 8  0 'C'.
  PERFORM add_def USING 'ZCOMP_VALID_TO'      'Liefererklaerung gueltig bis'      'DATE' 8 0 'C'.
  PERFORM add_def USING 'ZCOMP_DECL_REF'      'Lieferantenerklaerung Referenz'  'CHAR' 30 0 'C'.
ENDFORM.

FORM append_value USING VALUE(iv_value) TYPE atwrt
                        VALUE(iv_text)  TYPE atwtb.
  CLEAR gs_val_char.
  gs_val_char-value_char = iv_value.
  APPEND gs_val_char TO gt_val_char.

  CLEAR gs_val_descr.
  gs_val_descr-value_char = iv_value.
  gs_val_descr-language_int = sy-langu.
  gs_val_descr-language_iso = 'DE'.
  gs_val_descr-description = iv_text.
  APPEND gs_val_descr TO gt_val_descr.
ENDFORM.

FORM fill_values USING is_def TYPE ty_char_def.
  REFRESH: gt_val_char, gt_val_num, gt_val_descr.

  IF is_def-datatype = 'NUM'.
    CLEAR gs_val_num.
    gs_val_num-value_from = 0.
    gs_val_num-value_to   = 100.
    APPEND gs_val_num TO gt_val_num.
  ELSEIF is_def-datatype = 'DATE'.
* BAPI_CHARACT_CREATE verlangt fuer DATE einen numerischen Wertebereich.
    CLEAR gs_val_num.
    gs_val_num-value_from = 19000101.
    gs_val_num-value_to   = 99991231.
    APPEND gs_val_num TO gt_val_num.
  ENDIF.

  CASE is_def-name.
    WHEN 'ZPPWR_RECYCL_CLASS'.
      PERFORM append_value USING 'A' 'Klasse A'.
      PERFORM append_value USING 'B' 'Klasse B'.
      PERFORM append_value USING 'C' 'Klasse C'.
      PERFORM append_value USING 'D' 'Klasse D'.
      PERFORM append_value USING 'E' 'Klasse E'.
    WHEN 'ZPPWR_DECL_STATUS' OR 'ZPPWR_FOOD_CONTACT' OR 'ZCOMP_DECL_STATUS'.
      PERFORM append_value USING 'YES'       'Ja'.
      PERFORM append_value USING 'NO'        'Nein'.
      PERFORM append_value USING 'UNDEFINED' 'Ungeprueft'.
    WHEN 'ZCOMP_REACH_STATUS' OR 'ZCOMP_SVHC_STATUS'
      OR 'ZCOMP_ROHS_STATUS' OR 'ZCOMP_PFAS_STATUS'.
      PERFORM append_value USING 'COMPLIANT'     'Konform'.
      PERFORM append_value USING 'NON_COMPLIANT' 'Nicht konform'.
      PERFORM append_value USING 'UNDEFINED'     'Ungeprueft'.
    WHEN 'ZPPWR_DECL_REF' OR 'ZCOMP_DECL_REF'.
* Technischer Initialwert; weitere Referenzen bleiben frei eingebbar.
      PERFORM append_value USING '-' 'Keine Referenz'.
  ENDCASE.
ENDFORM.

FORM print_return.
  LOOP AT gt_return INTO gs_return.
    WRITE: / gs_return-type, gs_return-id, gs_return-number,
             gs_return-message.
    IF gs_return-type = 'E' OR gs_return-type = 'A'.
      gv_error = 'X'.
    ENDIF.
  ENDLOOP.
ENDFORM.

FORM create_characteristic USING is_def TYPE ty_char_def.
  CLEAR gv_atinn.
  SELECT SINGLE atinn
    FROM cabn
    INTO gv_atinn
    WHERE atnam = is_def-name.

  IF sy-subrc = 0.
    WRITE: / 'SKIP Merkmal vorhanden:', is_def-name.
    RETURN.
  ENDIF.

  CLEAR gs_detail.
  gs_detail-charact_name      = is_def-name.
  gs_detail-data_type         = is_def-datatype.
  gs_detail-length            = is_def-length.
  gs_detail-decimals          = is_def-decimals.
  gs_detail-status            = '1'.
  gs_detail-value_assignment  = '1'.
  gs_detail-additional_values = space.
  gs_detail-display_values    = 'X'.

* Datumswerte und Dokumentreferenzen haben keine feste Werteliste.
  IF is_def-datatype = 'DATE'
     OR is_def-name = 'ZPPWR_DECL_REF'
     OR is_def-name = 'ZCOMP_DECL_REF'.
    gs_detail-additional_values = 'X'.
  ENDIF.

  IF is_def-datatype = 'NUM'.
    gs_detail-with_sign        = space.
    gs_detail-interval_allowed = space.
  ENDIF.

  REFRESH gt_descr.
  CLEAR gs_descr.
  gs_descr-language_int = sy-langu.
  gs_descr-language_iso = 'DE'.
  gs_descr-description  = is_def-text.
  APPEND gs_descr TO gt_descr.

  PERFORM fill_values USING is_def.
  REFRESH gt_return.

  CALL FUNCTION 'BAPI_CHARACT_CREATE'
    EXPORTING
      charactdetail     = gs_detail
    TABLES
      charactdescr      = gt_descr
      charactvaluesnum  = gt_val_num
      charactvalueschar = gt_val_char
      charactvaluesdescr = gt_val_descr
      return            = gt_return.

  WRITE: / 'CREATE Merkmal:', is_def-name.
  PERFORM print_return.
ENDFORM.

FORM create_class USING VALUE(iv_class) TYPE klasse_d
                        VALUE(iv_text)  TYPE klschl
                        VALUE(iv_id)    TYPE c.
  REFRESH gt_return.
  CALL FUNCTION 'BAPI_CLASS_EXISTENCECHECK'
    EXPORTING
      classtype = '001'
      classnum  = iv_class
    TABLES
      return    = gt_return.

  gv_exists = 'X'.
  LOOP AT gt_return INTO gs_return.
    IF gs_return-type = 'E' OR gs_return-type = 'A'.
      CLEAR gv_exists.
    ENDIF.
  ENDLOOP.

  IF gv_exists = 'X'.
    WRITE: / 'SKIP Klasse vorhanden:', iv_class.
    RETURN.
  ENDIF.

  CLEAR gs_class_basic.
  gs_class_basic-status = '1'.

  REFRESH gt_class_desc.
  CLEAR gs_class_desc.
  gs_class_desc-langu     = sy-langu.
  gs_class_desc-langu_iso = 'DE'.
  gs_class_desc-catchword = iv_text.
  APPEND gs_class_desc TO gt_class_desc.

  REFRESH gt_class_char.
  LOOP AT gt_defs INTO gs_def WHERE class_id = iv_id.
    CLEAR gs_class_char.
    gs_class_char-name_char = gs_def-name.
    APPEND gs_class_char TO gt_class_char.
  ENDLOOP.

  REFRESH gt_return.
  CALL FUNCTION 'BAPI_CLASS_CREATE'
    EXPORTING
      classnumnew         = iv_class
      classtypenew        = '001'
      classbasicdata      = gs_class_basic
    TABLES
      classdescriptions   = gt_class_desc
      classcharacteristics = gt_class_char
      return              = gt_return.

  WRITE: / 'CREATE Klasse:', iv_class.
  PERFORM print_return.
ENDFORM.
