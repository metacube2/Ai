*&---------------------------------------------------------------------*
*& Report Z_KOI_VC_TXT_TESTID  (lokal $TMP, NUR T76)
*& Legt die kuenstliche Textposition B63485/46123 aus Z_KOI_VC_TXT_KOPIE
*& auf die ID 00000000, damit sie in ID-Reihenfolge VOR der echten
*& Position (ID 01210783) liegt. Test FIX 12, 2026-10-19, Auftrag Ingo.
*&---------------------------------------------------------------------*
REPORT z_koi_vc_txt_testid.

DATA: ls_t TYPE zpowerbi_vc_txt,
      lv_n TYPE i.

IF sy-sysid <> 'T76'.
  WRITE: / 'ABBRUCH: nur in T76 erlaubt, System', sy-sysid.
  RETURN.
ENDIF.

SELECT SINGLE * FROM zpowerbi_vc_txt INTO @ls_t
  WHERE kompnr = 'B63485' AND matnr = '000000000000046123' AND postyp = 'T'.
IF sy-subrc <> 0.
  WRITE: / 'ABBRUCH: Testzeile nicht gefunden'.
  RETURN.
ENDIF.
WRITE: / 'Testzeile gefunden, ID', ls_t-id.
IF ls_t-id = 0.
  WRITE: / 'liegt schon auf ID 0, nichts zu tun'.
  RETURN.
ENDIF.
SELECT COUNT(*) FROM zpowerbi_vc_txt INTO @lv_n WHERE id = 0.
IF lv_n > 0.
  WRITE: / 'ABBRUCH: ID 0 ist belegt'.
  RETURN.
ENDIF.
DELETE FROM zpowerbi_vc_txt WHERE id = @ls_t-id.
WRITE: / 'geloescht', sy-dbcnt.
ls_t-id = 0.
INSERT zpowerbi_vc_txt FROM @ls_t.
WRITE: / 'neu auf ID 0, Rueckgabe', sy-subrc.
COMMIT WORK.
