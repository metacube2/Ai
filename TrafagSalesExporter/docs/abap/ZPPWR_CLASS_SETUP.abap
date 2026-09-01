REPORT zppwr_class_setup.

* Einmaliger, idempotenter Aufbau der PPWR-/Compliance-Klassifizierung.
* Zielsystem: T76, Mandant 100. Keine Materialzuordnungen, keine P76-Aenderung.
*
* Aenderung 18.08.2026 (Ingo/Claude):
*  - Zielmandant von 090 auf 100 umgestellt. 090 traegt keine Daten; der Pilot
*    braucht den Testmandanten mit echten Materialien.
*  - FEHLERBEHEBUNG Klassenanlage: die Existenzpruefung lief ueber
*    BAPI_CLASS_EXISTENCECHECK mit Vorbelegung gv_exists = 'X'. Genau dieser
*    Prueftyp lieferte in diesem System schon bei den Merkmalen keinen
*    auswertbaren Fehler vom Typ E oder A (siehe Anlageprotokoll Abschnitt 14
*    Punkt 1). Der Report meldete deshalb 'SKIP Klasse vorhanden' und rief
*    BAPI_CLASS_CREATE nie auf, obwohl keine Klasse existierte. Die Pruefung
*    laeuft jetzt wie bei den Merkmalen ueber einen direkten SELECT, hier auf
*    KLAH.
*  - Neue Ruecklesephase am Ende: nach dem Commit wird aus CABN, KLAH und KSML
*    gezaehlt, was wirklich in der Datenbank steht. Die Schlussmeldung FERTIG
*    erscheint nur noch, wenn die Istzahlen den Sollzahlen entsprechen.

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
      gv_atinn      TYPE cabn-atinn,
      gv_clint      TYPE klah-clint,
      gv_tabix      TYPE sy-tabix.

* Sollzahlen fuer die Ruecklesekontrolle am Ende.
CONSTANTS: gc_soll_ppwr  TYPE i VALUE 9,
           gc_soll_comp  TYPE i VALUE 12.

DATA: gv_ist_ppwr   TYPE i,
      gv_ist_comp   TYPE i,
      gv_ist_class  TYPE i,
      gv_ist_ksml_p TYPE i,
      gv_ist_ksml_c TYPE i,
      gv_verify_ok  TYPE c LENGTH 1.

DATA: gs_class_basic TYPE bapi1003_basic,
      gs_class_desc  TYPE bapi1003_catch,
      gt_class_desc  TYPE STANDARD TABLE OF bapi1003_catch,
      gs_class_char  TYPE bapi1003_charact,
      gt_class_char  TYPE STANDARD TABLE OF bapi1003_charact.

START-OF-SELECTION.
  IF sy-sysid <> 'T76' OR sy-mandt <> '100'.
    WRITE: / 'ABBRUCH: Report darf nur in T76/100 laufen.',
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

*   Auch ohne Schreibzugriff den Iststand aus der Datenbank zeigen. So laesst
*   sich vor jeder Anlage sehen, was in diesem Mandanten wirklich existiert.
    PERFORM verify_counts.
    SKIP.
    IF gv_verify_ok = 'X'.
      WRITE: / 'Iststand vollstaendig. Es ist nichts anzulegen.' COLOR COL_POSITIVE.
    ELSE.
      WRITE: / 'Iststand unvollstaendig. Mit P_WRITE = X anlegen.' COLOR COL_TOTAL.
    ENDIF.
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
    RETURN.
  ENDIF.

  CALL FUNCTION 'BAPI_TRANSACTION_COMMIT'
    EXPORTING
      wait = 'X'.

  PERFORM verify_counts.

  SKIP.
  IF gv_verify_ok = 'X'.
    WRITE: / 'FERTIG: Anlage in der Datenbank nachgewiesen.' COLOR COL_POSITIVE.
    WRITE: / 'Naechster Schritt: CL03 und CT04 im SAP GUI sichtpruefen.'.
  ELSE.
    WRITE: / 'WARNUNG: Istzahlen weichen vom Soll ab.' COLOR COL_NEGATIVE.
    WRITE: / 'Der Lauf gilt NICHT als erfolgreich. Nicht weitermelden,',
           / 'bevor die Abweichung geklaert ist.' COLOR COL_NEGATIVE.
  ENDIF.

FORM verify_counts.
* Zaehlt ausschliesslich, was tatsaechlich in der Datenbank steht, und setzt
* gv_verify_ok. Das Urteil formuliert der Aufrufer, weil dieselbe Messung im
* Prueflauf und nach dem Schreiben verwendet wird.
* Die Selbstmeldung der BAPIs ist ausdruecklich KEIN Nachweis: am 13.08.2026
* meldete dieser Report 'FERTIG', obwohl keine Klasse angelegt worden war.
  gv_verify_ok = 'X'.

  SELECT COUNT( DISTINCT atnam ) FROM cabn
    INTO gv_ist_ppwr
    WHERE atnam LIKE 'ZPPWR%'.

  SELECT COUNT( DISTINCT atnam ) FROM cabn
    INTO gv_ist_comp
    WHERE atnam LIKE 'ZCOMP%'.

  SELECT COUNT( * ) FROM klah
    INTO gv_ist_class
    WHERE klart = '001'
      AND ( class = 'ZPPWR_PACKMITTEL' OR class = 'ZCOMP_STOFF' ).

* Zugeordnete Merkmale je Klasse ueber die interne Klassennummer zaehlen.
  CLEAR: gv_clint, gv_ist_ksml_p.
  SELECT SINGLE clint FROM klah INTO gv_clint
    WHERE klart = '001' AND class = 'ZPPWR_PACKMITTEL'.
  IF sy-subrc = 0.
    SELECT COUNT( * ) FROM ksml INTO gv_ist_ksml_p WHERE clint = gv_clint.
  ENDIF.

  CLEAR: gv_clint, gv_ist_ksml_c.
  SELECT SINGLE clint FROM klah INTO gv_clint
    WHERE klart = '001' AND class = 'ZCOMP_STOFF'.
  IF sy-subrc = 0.
    SELECT COUNT( * ) FROM ksml INTO gv_ist_ksml_c WHERE clint = gv_clint.
  ENDIF.

  SKIP.
  WRITE: / '--- Ruecklesekontrolle aus der Datenbank ---'.
  WRITE: / 'Merkmale ZPPWR* in CABN :', gv_ist_ppwr,
           'Soll', gc_soll_ppwr.
  WRITE: / 'Merkmale ZCOMP* in CABN :', gv_ist_comp,
           'Soll', gc_soll_comp.
  WRITE: / 'Klassen in KLAH (001)   :', gv_ist_class, 'Soll', 2.
  WRITE: / 'Merkmale an ZPPWR_PACKMITTEL (KSML):', gv_ist_ksml_p,
           'Soll', gc_soll_ppwr.
  WRITE: / 'Merkmale an ZCOMP_STOFF (KSML)     :', gv_ist_ksml_c,
           'Soll', gc_soll_comp.

  IF gv_ist_ppwr <> gc_soll_ppwr OR gv_ist_comp <> gc_soll_comp.
    CLEAR gv_verify_ok.
  ENDIF.
  IF gv_ist_class <> 2.
    CLEAR gv_verify_ok.
  ENDIF.
  IF gv_ist_ksml_p <> gc_soll_ppwr OR gv_ist_ksml_c <> gc_soll_comp.
    CLEAR gv_verify_ok.
  ENDIF.
ENDFORM.

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
* Existenz idempotent ueber die Tabelle pruefen, NICHT ueber
* BAPI_CLASS_EXISTENCECHECK. Der BAPI meldete eine fehlende Klasse in diesem
* System nicht als Fehler vom Typ E oder A. Mit der frueheren Vorbelegung
* gv_exists = 'X' galt die Klasse dadurch faelschlich als vorhanden, die Anlage
* wurde still uebersprungen und der Report meldete trotzdem FERTIG.
  CLEAR gv_clint.
  SELECT SINGLE clint
    FROM klah
    INTO gv_clint
    WHERE klart = '001'
      AND class = iv_class.

  IF sy-subrc = 0.
    WRITE: / 'SKIP Klasse vorhanden:', iv_class, 'CLINT', gv_clint.
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
