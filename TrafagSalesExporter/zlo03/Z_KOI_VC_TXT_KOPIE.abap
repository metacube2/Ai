*&---------------------------------------------------------------------*
*& Report Z_KOI_VC_TXT_KOPIE  (lokal $TMP, NUR T76)
*& Testdaten fuer ZLO03: kopiert ZPOWERBI_VC nach ZPOWERBI_VC_TXT
*& (gleiche Struktur) und fuegt fuer den Test von FIX 12 eine kuenstliche
*& Textposition B63485 in VKNR 46123 ein, wie sie in P76 vorkommt.
*& Auftrag Ingo 2026-10-16. Doku zlo03/MK_B63485_FIX12_2026-10-16.md
*&
*& Ablauf: p_neu = X leert die Zieltabelle und beginnt vorne. Jeder Lauf
*& kopiert in ID-Reihenfolge ab der hoechsten schon kopierten ID und hoert
*& nach p_sek Sekunden auf (Dialog-Zeitlimit). So oft starten, bis
*& "fertig" erscheint; erst dann setzt p_test die Testzeile.
*& Historie: INSERT ... FROM ( SELECT ... ) endete mit DBSQL_DBPRT_STATEMENT,
*& ein Lauf ueber alles mit TIME_OUT (2026-10-16).
*&---------------------------------------------------------------------*
REPORT z_koi_vc_txt_kopie.

PARAMETERS: p_neu  AS CHECKBOX DEFAULT ' ',
            p_sek  TYPE i DEFAULT 420,
            p_test AS CHECKBOX DEFAULT 'X'.

DATA: lv_n     TYPE i,
      lv_id    TYPE zpowerbi_vc_txt-id,
      lv_ab    TYPE zpowerbi_vc_txt-id,
      lv_sum   TYPE i,
      lv_start TYPE i,
      lv_jetzt TYPE i,
      lv_fertig TYPE abap_bool,
      lc_cur   TYPE cursor,
      lt_pak   TYPE STANDARD TABLE OF zpowerbi_vc_txt,
      ls_test  TYPE zpowerbi_vc_txt.

IF sy-sysid <> 'T76'.
  WRITE: / 'ABBRUCH: nur in T76 erlaubt, System', sy-sysid.
  RETURN.
ENDIF.

IF p_neu = 'X'.
  DELETE FROM zpowerbi_vc_txt.
  WRITE: / 'Geloescht in ZPOWERBI_VC_TXT:', sy-dbcnt.
  COMMIT WORK.
ENDIF.

" Testzeile (ID unter der kleinsten) nicht als Fortsetzungspunkt nehmen
SELECT MAX( id ) FROM zpowerbi_vc_txt INTO @lv_ab WHERE postyp <> 'T' OR kompnr <> 'B63485'.
WRITE: / 'Fortsetzung ab ID:', lv_ab.

GET RUN TIME FIELD lv_start.
lv_fertig = abap_true.
OPEN CURSOR WITH HOLD @lc_cur FOR
  SELECT * FROM zpowerbi_vc WHERE id > @lv_ab ORDER BY id.
DO.
  FETCH NEXT CURSOR @lc_cur INTO TABLE @lt_pak PACKAGE SIZE 20000.
  IF sy-subrc <> 0.
    EXIT.
  ENDIF.
  INSERT zpowerbi_vc_txt FROM TABLE @lt_pak ACCEPTING DUPLICATE KEYS.
  lv_sum = lv_sum + sy-dbcnt.
  CALL FUNCTION 'DB_COMMIT'.
  GET RUN TIME FIELD lv_jetzt.
  IF ( lv_jetzt - lv_start ) / 1000000 > p_sek.
    lv_fertig = abap_false.
    EXIT.
  ENDIF.
ENDDO.
CLOSE CURSOR @lc_cur.
WRITE: / 'Kopiert in diesem Lauf:', lv_sum.
IF lv_fertig = abap_true.
  WRITE: / 'fertig'.
ELSE.
  WRITE: / 'NICHT fertig, nochmals starten'.
ENDIF.

IF p_test = 'X' AND lv_fertig = abap_true.
  SELECT SINGLE * FROM zpowerbi_vc_txt INTO @ls_test
    WHERE kompnr = 'B63485' AND matnr = '000000000000046123' AND postyp = 'T'.
  IF sy-subrc = 0.
    WRITE: / 'Testzeile schon vorhanden, ID', ls_test-id.
  ELSE.
    " Vorlage: echte Position B63485 in 46123
    SELECT SINGLE * FROM zpowerbi_vc_txt INTO @ls_test
      WHERE kompnr = 'B63485' AND matnr = '000000000000046123' AND postyp = 'L'.
    IF sy-subrc <> 0.
      WRITE: / 'ABBRUCH: Vorlage B63485/46123 nicht gefunden'.
      RETURN.
    ENDIF.
    " freie ID unterhalb der kleinsten, damit die Textposition vorne liegt
    SELECT MIN( id ) FROM zpowerbi_vc_txt INTO @lv_id.
    lv_id = lv_id - 1.
    IF lv_id <= 0.
      SELECT MAX( id ) FROM zpowerbi_vc_txt INTO @lv_id.
      lv_id = lv_id + 1.
    ENDIF.
    ls_test-id     = lv_id.
    ls_test-postyp = 'T'.
    INSERT zpowerbi_vc_txt FROM @ls_test.
    WRITE: / 'Testzeile Textposition B63485/46123, ID', lv_id, 'Rueckgabe', sy-subrc.
    COMMIT WORK.
  ENDIF.
ENDIF.

SELECT COUNT(*) FROM zpowerbi_vc_txt INTO @lv_n.
WRITE: / 'ZPOWERBI_VC_TXT Zeilen jetzt:', lv_n.
