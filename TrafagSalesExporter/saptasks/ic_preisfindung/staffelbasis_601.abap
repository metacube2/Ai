FORM frm_staffelbas_601.
*---------------------------------------------------------------------*
* Staffelbasis = Bestellmenge der IC-Bestellung (interne Verrechnung)
* Trafag AG, 06.11.2026, KOI (Anfrage Fabio Palma, Auftrag Ingo Kohler)
*
* Nur Kalkulationsschema ZICAA1 und nur Mengenstaffel (KZBZG C):
* Faktura -> Lieferposition (KOMP-VGBEL/VGPOS) -> Bestellposition
* (LIPS-VGBEL/VGPOS, LIPS-VGTYP V) -> EKPO-MENGE. Die Staffelbasis
* wird der groessere Wert aus Bestellmenge und bisheriger Staffelbasis
* (fakturierte Menge). Der Konditionswert bleibt bei der fakturierten
* Menge. Wird keine Bestellung gefunden oder ist die Mengeneinheit
* nicht umrechenbar, bleibt die Staffelbasis unveraendert (Standard).
*
* SAPLV61A laeuft ohne Festpunktarithmetik. XKWERT kommt aus
* XKOMV-KSTBS, das der Standard aus KOMP-MGAME (QUAN, 3 Dezimalen)
* fuellt (LV61AA73). Die Bestellmenge wird deshalb ebenfalls in
* Feldern mit 3 Dezimalen gefuehrt (EKPO-MENGE / KOMP-MGAME) und ohne
* Literale verglichen und uebertragen, genau wie der Standard.
*---------------------------------------------------------------------*
  CONSTANTS: lc_kalsm_ic   TYPE kalsm_d VALUE 'ZICAA1',
             lc_kzbzg_meng TYPE kzbzg   VALUE 'C',
             lc_vgtyp_best TYPE vbtypl  VALUE 'V'.

  DATA: lv_best_vbeln TYPE lips-vgbel,
        lv_best_posnr TYPE lips-vgpos,
        lv_best_vgtyp TYPE lips-vgtyp,
        lv_ebeln      TYPE ekpo-ebeln,
        lv_ebelp      TYPE ekpo-ebelp,
        lv_best_menge TYPE ekpo-menge,
        lv_best_meins TYPE ekpo-meins,
        lv_best_matnr TYPE ekpo-matnr,
        lv_matnr      TYPE mara-matnr,
        lv_in_me      TYPE mara-meins,
        lv_out_me     TYPE mara-meins,
        lv_konv_menge TYPE ekpo-menge,
        lv_basis_neu  TYPE komp-mgame.

* Nur interne Verrechnung (Schema ZICAA1) und nur Mengenstaffel
  CHECK komk-kalsm = lc_kalsm_ic.
  CHECK xkomv-kzbzg = lc_kzbzg_meng.
* Vorlage der Fakturaposition muss eine Lieferposition sein
  CHECK komp-vgbel IS NOT INITIAL.
  CHECK komp-vgpos IS NOT INITIAL.

  SELECT SINGLE vgbel vgpos vgtyp
    FROM lips
    INTO (lv_best_vbeln, lv_best_posnr, lv_best_vgtyp)
    WHERE vbeln = komp-vgbel
      AND posnr = komp-vgpos.
  CHECK sy-subrc = 0.
* Lieferposition mit Bezug auf eine Bestellung (Umlagerung)
  CHECK lv_best_vgtyp = lc_vgtyp_best.
  CHECK lv_best_vbeln IS NOT INITIAL.

  lv_ebeln = lv_best_vbeln.
* LIPS-VGPOS ist NUMC 6 (000010), EKPO-EBELP NUMC 5 (00010)
  lv_ebelp = lv_best_posnr.

  SELECT SINGLE menge meins matnr
    FROM ekpo
    INTO (lv_best_menge, lv_best_meins, lv_best_matnr)
    WHERE ebeln = lv_ebeln
      AND ebelp = lv_ebelp
      AND loekz = space.
  CHECK sy-subrc = 0.
  CHECK lv_best_menge IS NOT INITIAL.

* Einheit der Staffelbasis: Staffeleinheit, sonst Verkaufsmengeneinheit
* (wie STAFFELBASIS_ERMITTELN in LV61AA73)
  IF xkomv-konms IS NOT INITIAL.
    lv_out_me = xkomv-konms.
  ELSE.
    lv_out_me = komp-vrkme.
  ENDIF.
  CHECK lv_out_me IS NOT INITIAL.

  IF lv_best_meins = lv_out_me.
    lv_konv_menge = lv_best_menge.
  ELSE.
    CHECK lv_best_matnr IS NOT INITIAL.
    lv_matnr = lv_best_matnr.
    lv_in_me = lv_best_meins.
    CALL FUNCTION 'MD_CONVERT_MATERIAL_UNIT'
      EXPORTING
        i_matnr              = lv_matnr
        i_in_me              = lv_in_me
        i_out_me             = lv_out_me
        i_menge              = lv_best_menge
      IMPORTING
        e_menge              = lv_konv_menge
      EXCEPTIONS
        error_in_application = 1
        error                = 2
        OTHERS               = 3.
    CHECK sy-subrc = 0.
  ENDIF.

  lv_basis_neu = lv_konv_menge.
  IF lv_basis_neu > xkwert.
    xkwert = lv_basis_neu.
  ENDIF.

ENDFORM.
