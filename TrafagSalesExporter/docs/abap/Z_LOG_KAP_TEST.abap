*&---------------------------------------------------------------------*
*& Z_LOG_KAP_TEST  ($TMP, nur lesend, 2026-10-05)
*& Prueft fuer Teil C (LogKapSet), was CR_CAPACITY_AVAILABLE_PERIODS und KBED liefern:
*& Perioden, Einheit, ob ANGEB ein Tageswert ist, Bedarf je Tag.
*&---------------------------------------------------------------------*
REPORT z_log_kap_test.

PARAMETERS: p_werks TYPE werks_d DEFAULT '1100',
            p_von   TYPE datum DEFAULT '20260301',
            p_tage  TYPE i DEFAULT 14.

TYPES: BEGIN OF ty_ap,
         objid TYPE cr_objid,
         arbpl TYPE arbpl,
       END OF ty_ap.
DATA: lt_ap    TYPE STANDARD TABLE OF ty_ap,
      ls_ap    TYPE ty_ap,
      lt_avail TYPE STANDARD TABLE OF rc65k,
      ls_avail TYPE rc65k,
      lv_bis   TYPE datum,
      lv_n     TYPE i,
      lv_sum   TYPE p LENGTH 15 DECIMALS 3,
      lv_keinh TYPE cy_keinh.

lv_bis = p_von + p_tage - 1.

SELECT objid arbpl FROM crhd INTO TABLE lt_ap
  WHERE objty = 'A' AND werks = p_werks
    AND ( arbpl = 'MLE01' OR arbpl = 'MLE02' OR arbpl = 'MLE04' ).

LOOP AT lt_ap INTO ls_ap.
  WRITE: / '=== Arbeitsplatz', ls_ap-arbpl, ls_ap-objid.
  CLEAR lt_avail.
  CALL FUNCTION 'CR_CAPACITY_AVAILABLE_PERIODS'
    EXPORTING
      objid_a = ls_ap-objid
      single  = 'X'
    TABLES
      t_avail = lt_avail
    EXCEPTIONS
      OTHERS  = 1.
  WRITE: / 'subrc', sy-subrc.
  DESCRIBE TABLE lt_avail LINES lv_n.
  WRITE: / 'Perioden gesamt', lv_n.
  LOOP AT lt_avail INTO ls_avail WHERE datub >= p_von AND datuv <= lv_bis.
    WRITE: / 'AVAIL', ls_avail-kapid, ls_avail-datuv, ls_avail-datub,
             ls_avail-ptage, ls_avail-gtage, ls_avail-keinh.
    lv_sum = ls_avail-angeb.
    WRITE: 'ANGEB', lv_sum.
    lv_sum = ls_avail-einzt.
    WRITE: 'EINZT', lv_sum.
    lv_sum = ls_avail-belas.
    WRITE: 'BELAS', lv_sum.
  ENDLOOP.

  SELECT COUNT(*) FROM kbed INTO lv_n
    WHERE arbid = ls_ap-objid AND fstad BETWEEN p_von AND lv_bis.
  WRITE: / 'KBED Saetze im Fenster', lv_n.
  SELECT SUM( kbearest ) FROM kbed INTO lv_sum
    WHERE arbid = ls_ap-objid AND fstad BETWEEN p_von AND lv_bis.
  WRITE: / 'KBED Rest Bearbeiten', lv_sum.
  SELECT SUM( kruerest ) FROM kbed INTO lv_sum
    WHERE arbid = ls_ap-objid AND fstad BETWEEN p_von AND lv_bis.
  WRITE: / 'KBED Rest Ruesten', lv_sum.
  SELECT SINGLE keinh FROM kbed INTO lv_keinh
    WHERE arbid = ls_ap-objid AND fstad BETWEEN p_von AND lv_bis.
  WRITE: / 'KBED Einheit', lv_keinh.
ENDLOOP.
