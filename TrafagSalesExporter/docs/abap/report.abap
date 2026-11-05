*&---------------------------------------------------------------------*
*& Report  ZTRAFAG_SCHWEIZ_EXPORT
*&---------------------------------------------------------------------*
*& Zweck
*&   Ermittelt SD-Faktura-Positionen fuer Schweiz/Oesterreich aus
*&   Buchungskreis 1100/1200 und schreibt sie per Upsert in Tabelle
*&   ZSCHWEIZ.
*&   ZSCHWEIZ kann danach aus SAP HANA in TrafagSalesExporter geladen
*&   werden.
*&
*& HANA-Anbindung
*&   - Tabelle/View-Basis: ZSCHWEIZ
*&   - im .NET-Programm Standort mit Quellsystem SAP_HANA verwenden
*&   - HANA-Schema auf das ABAP/HANA-Schema setzen
*&   - grafische Quelle: Alias Z, Tabelle/View ZSCHWEIZ
*&   - grafische Feldmappings koennen auf die unten vorgeschlagenen
*&     Felder gemappt werden.
*&
*& Fachliche Annahmen
*&   - Hauswaehrung ist fuehrend.
*&   - Nettofakturawert wird pro Belegposition ermittelt.
*&   - Gutschriften/Stornos werden ueber den Fakturatyp negativ bewertet.
*&   - Buchungskreis 1100 = Schweiz.
*&   - Buchungskreis 1200 = Oesterreich.
*&   - TSC/Reporting-Land werden aus BUKRS abgeleitet; Kundenland
*&     (KNA1-LAND1) bleibt als Infofeld erhalten.
*&
*& DDIC-Vorschlag fuer ZSCHWEIZ
*&   Client-dependent Tabelle, Auslieferungsklasse A, Datenpflege erlaubt.
*&
*&   Schluesselfelder:
*&     MANDT        MANDT        SAP Mandant
*&     BUKRS        BUKRS        Buchungskreis
*&     GJAHR        GJAHR        Geschaeftsjahr aus FKDAT
*&     VBELN        VBELN_VF     Fakturanummer
*&     POSNR        POSNR_VF     Fakturaposition
*&
*&   Datenfelder:
*&     LAND1        LAND1        Reporting-Land aus BUKRS, z.B. CH/AT
*&     CUSTOMER_LAND LAND1       Kundenland aus KNA1-LAND1
*&     TSC          CHAR10       Reporting-Standort, z.B. TRCH/TRAT
*&     FKDAT        FKDAT        Fakturadatum
*&     FKART        FKART        Fakturatyp
*&     VBTYP        VBTYP        SD-Belegkategorie
*&     KUNNR        KUNNR        Auftraggeber/Sold-to
*&     NAME1        NAME1_GP     Kundenname
*&     MATNR        MATNR        Material
*&     ARKTX        ARKTX        Positionsbezeichnung
*&     PRODH        PRODH_D      Produkthierarchie
*&     FKIMG        FKIMG        Fakturamenge
*&     VRKME        VRKME        Verkaufsmengeneinheit
*&     WAERK        WAERK        Belegwaehrung
*&     HWAER        WAERS        Hauswaehrung aus T001
*&     NETWR_DC     CURR 23,2    Positions-Netto in Belegwaehrung
*&     TAX_DC       CURR 23,2    Positions-Steuer in Belegwaehrung
*&     NETWR_HC     CURR 23,2    Positions-Netto in Hauswaehrung
*&     TAX_HC       CURR 23,2    Positions-Steuer in Hauswaehrung
*&     KURRF        KURRF        Rechnungsumrechnungskurs
*&     IS_CREDIT    BOOLE_D      X = Gutschrift/Storno negativ bewertet
*&     PARTY_CLASS  CHAR10       2ND/3RD fuer spaetere IC-Abgrenzung
*&     ERDAT_SRC    ERDAT        Anlage-/Quell-Erfassungsdatum
*&     AEDAT_SRC    AEDAT        Aenderungsdatum, falls vorhanden
*&     CREATED_AT   TIMESTAMPL   Insert-Zeitpunkt
*&     CHANGED_AT   TIMESTAMPL   Update-Zeitpunkt
*&     CREATED_BY   SYUNAME      Insert-User
*&     CHANGED_BY   SYUNAME      Update-User
*&
*&   Sekundaerindex empfohlen:
*&     Z01: BUKRS, LAND1, GJAHR, FKDAT
*&     Z02: KUNNR, GJAHR
*&---------------------------------------------------------------------*

REPORT ztrafag_schweiz_export.

TABLES: vbrk, vbrp, kna1.

TYPES: BEGIN OF ty_billing,
         bukrs TYPE vbrk-bukrs,
         vbeln TYPE vbrk-vbeln,
         fkdat TYPE vbrk-fkdat,
         fkart TYPE vbrk-fkart,
         vbtyp TYPE vbrk-vbtyp,
         waerk TYPE vbrk-waerk,
         kurrf TYPE vbrk-kurrf,
         kunag TYPE vbrk-kunag,
         erdat TYPE vbrk-erdat,
         posnr TYPE vbrp-posnr,
         matnr TYPE vbrp-matnr,
         arktx TYPE vbrp-arktx,
         prodh TYPE vbrp-prodh,
         fkimg TYPE vbrp-fkimg,
         vrkme TYPE vbrp-vrkme,
         netwr TYPE vbrp-netwr,
         mwsbp TYPE vbrp-mwsbp,
         customer_land TYPE kna1-land1,
         name1 TYPE kna1-name1,
         hwaer TYPE t001-waers,
       END OF ty_billing.

TYPES: BEGIN OF ty_zschweiz,
         mandt       TYPE mandt,
         bukrs       TYPE bukrs,
         gjahr       TYPE gjahr,
         vbeln       TYPE vbeln_vf,
         posnr       TYPE posnr_vf,
         land1       TYPE land1,
         customer_land TYPE land1,
         tsc         TYPE c LENGTH 10,
         fkdat       TYPE fkdat,
         fkart       TYPE fkart,
         vbtyp       TYPE vbtyp,
         kunnr       TYPE kunnr,
         name1       TYPE name1_gp,
         matnr       TYPE matnr,
         arktx       TYPE arktx,
         prodh       TYPE prodh_d,
         fkimg       TYPE fkimg,
         vrkme       TYPE vrkme,
         waerk       TYPE waerk,
         hwaer       TYPE waers,
         netwr_dc    TYPE p LENGTH 23 DECIMALS 2,
         tax_dc      TYPE p LENGTH 23 DECIMALS 2,
         netwr_hc    TYPE p LENGTH 23 DECIMALS 2,
         tax_hc      TYPE p LENGTH 23 DECIMALS 2,
         kurrf       TYPE kurrf,
         is_credit   TYPE boole_d,
         party_class TYPE c LENGTH 10,
         erdat_src   TYPE erdat,
         aedat_src   TYPE aedat,
         created_at  TYPE timestampl,
         changed_at  TYPE timestampl,
         created_by  TYPE syuname,
         changed_by  TYPE syuname,
       END OF ty_zschweiz.

DATA: gt_billing  TYPE STANDARD TABLE OF ty_billing WITH EMPTY KEY,
      gt_zschweiz TYPE STANDARD TABLE OF zschweiz WITH EMPTY KEY,
      gs_zschweiz TYPE zschweiz.

SELECTION-SCREEN BEGIN OF BLOCK b01 WITH FRAME TITLE TEXT-t01.
  PARAMETERS: p_gjahr TYPE gjahr DEFAULT sy-datum(4) OBLIGATORY.
  SELECT-OPTIONS: s_bukrs FOR vbrk-bukrs,
                  s_fkart FOR vbrk-fkart,
                  s_vbeln FOR vbrk-vbeln.
  PARAMETERS: p_test AS CHECKBOX DEFAULT abap_true.
SELECTION-SCREEN END OF BLOCK b01.

INITIALIZATION.
  TEXT-t01 = 'Finance Export Selektion'.
  s_bukrs-sign = 'I'.
  s_bukrs-option = 'EQ'.
  s_bukrs-low = '1100'.
  APPEND s_bukrs.
  s_bukrs-low = '1200'.
  APPEND s_bukrs.

START-OF-SELECTION.
  PERFORM read_billing_data.
  PERFORM map_to_zschweiz.
  PERFORM persist_zschweiz.

FORM read_billing_data.
  DATA(lv_date_from) = CONV fkdat( |{ p_gjahr }0101| ).
  DATA(lv_date_to)   = CONV fkdat( |{ p_gjahr }1231| ).

  SELECT
      h~bukrs,
      h~vbeln,
      h~fkdat,
      h~fkart,
      h~vbtyp,
      h~waerk,
      h~kurrf,
      h~kunag,
      h~erdat,
      i~posnr,
      i~matnr,
      i~arktx,
      i~prodh,
      i~fkimg,
      i~vrkme,
      i~netwr,
      i~mwsbp,
      k~land1 AS customer_land,
      k~name1,
      c~waers AS hwaer
    FROM vbrk AS h
    INNER JOIN vbrp AS i
      ON i~vbeln = h~vbeln
    LEFT OUTER JOIN kna1 AS k
      ON k~kunnr = h~kunag
    LEFT OUTER JOIN t001 AS c
      ON c~bukrs = h~bukrs
    WHERE h~bukrs IN @s_bukrs
      AND h~fkdat BETWEEN @lv_date_from AND @lv_date_to
      AND h~vbeln IN @s_vbeln
      AND h~fkart IN @s_fkart
      AND h~fksto = @space
    INTO TABLE @gt_billing.

  WRITE: / 'Gelesene Fakturapositionen:', lines( gt_billing ).
ENDFORM.

FORM map_to_zschweiz.
  DATA: lv_sign       TYPE i,
        lv_netwr_hc   TYPE p LENGTH 23 DECIMALS 2,
        lv_tax_hc     TYPE p LENGTH 23 DECIMALS 2,
        lv_timestamp  TYPE timestampl,
        lv_party      TYPE c LENGTH 10.

  GET TIME STAMP FIELD lv_timestamp.

  LOOP AT gt_billing ASSIGNING FIELD-SYMBOL(<ls_billing>).
    CLEAR: gs_zschweiz, lv_netwr_hc, lv_tax_hc, lv_party.

    lv_sign = 1.
    IF <ls_billing>-vbtyp = 'O'
       OR <ls_billing>-vbtyp = 'N'
       OR <ls_billing>-fkart CP 'G*'
       OR <ls_billing>-fkart CP 'S*'.
      lv_sign = -1.
    ENDIF.

    PERFORM convert_to_house_currency
      USING <ls_billing>-netwr <ls_billing>-waerk <ls_billing>-hwaer <ls_billing>-fkdat <ls_billing>-kurrf
      CHANGING lv_netwr_hc.

    PERFORM convert_to_house_currency
      USING <ls_billing>-mwsbp <ls_billing>-waerk <ls_billing>-hwaer <ls_billing>-fkdat <ls_billing>-kurrf
      CHANGING lv_tax_hc.

    PERFORM classify_party
      USING <ls_billing>-kunnr <ls_billing>-name1
      CHANGING lv_party.

    gs_zschweiz-mandt       = sy-mandt.
    gs_zschweiz-bukrs       = <ls_billing>-bukrs.
    gs_zschweiz-gjahr       = p_gjahr.
    gs_zschweiz-vbeln       = <ls_billing>-vbeln.
    gs_zschweiz-posnr       = <ls_billing>-posnr.
    gs_zschweiz-land1       = SWITCH #( <ls_billing>-bukrs WHEN '1100' THEN 'CH' WHEN '1200' THEN 'AT' ELSE <ls_billing>-customer_land ).
    gs_zschweiz-customer_land = <ls_billing>-customer_land.
    gs_zschweiz-tsc         = SWITCH #( <ls_billing>-bukrs WHEN '1100' THEN 'TRCH' WHEN '1200' THEN 'TRAT' ELSE <ls_billing>-bukrs ).
    gs_zschweiz-fkdat       = <ls_billing>-fkdat.
    gs_zschweiz-fkart       = <ls_billing>-fkart.
    gs_zschweiz-vbtyp       = <ls_billing>-vbtyp.
    gs_zschweiz-kunnr       = <ls_billing>-kunag.
    gs_zschweiz-name1       = <ls_billing>-name1.
    gs_zschweiz-matnr       = <ls_billing>-matnr.
    gs_zschweiz-arktx       = <ls_billing>-arktx.
    gs_zschweiz-prodh       = <ls_billing>-prodh.
    gs_zschweiz-fkimg       = <ls_billing>-fkimg * lv_sign.
    gs_zschweiz-vrkme       = <ls_billing>-vrkme.
    gs_zschweiz-waerk       = <ls_billing>-waerk.
    gs_zschweiz-hwaer       = <ls_billing>-hwaer.
    gs_zschweiz-netwr_dc    = <ls_billing>-netwr * lv_sign.
    gs_zschweiz-tax_dc      = <ls_billing>-mwsbp * lv_sign.
    gs_zschweiz-netwr_hc    = lv_netwr_hc * lv_sign.
    gs_zschweiz-tax_hc      = lv_tax_hc * lv_sign.
    gs_zschweiz-kurrf       = <ls_billing>-kurrf.
    gs_zschweiz-is_credit   = COND #( WHEN lv_sign < 0 THEN abap_true ELSE abap_false ).
    gs_zschweiz-party_class = lv_party.
    gs_zschweiz-erdat_src   = <ls_billing>-erdat.
    gs_zschweiz-aedat_src   = sy-datum.
    gs_zschweiz-created_at  = lv_timestamp.
    gs_zschweiz-changed_at  = lv_timestamp.
    gs_zschweiz-created_by  = sy-uname.
    gs_zschweiz-changed_by  = sy-uname.

    APPEND gs_zschweiz TO gt_zschweiz.
  ENDLOOP.

  WRITE: / 'Aufbereitete ZSCHWEIZ-Zeilen:', lines( gt_zschweiz ).
ENDFORM.

FORM convert_to_house_currency
  USING    iv_amount TYPE any
           iv_from   TYPE waerk
           iv_to     TYPE waers
           iv_date   TYPE fkdat
           iv_kurrf  TYPE kurrf
  CHANGING cv_amount TYPE any.

  IF iv_from = iv_to OR iv_from IS INITIAL OR iv_to IS INITIAL.
    cv_amount = iv_amount.
    RETURN.
  ENDIF.

  CALL FUNCTION 'CONVERT_TO_LOCAL_CURRENCY'
    EXPORTING
      date             = iv_date
      foreign_amount   = iv_amount
      foreign_currency = iv_from
      local_currency   = iv_to
      rate             = iv_kurrf
    IMPORTING
      local_amount     = cv_amount
    EXCEPTIONS
      no_rate_found    = 1
      overflow         = 2
      no_factors_found = 3
      no_spread_found  = 4
      derived_2_times  = 5
      OTHERS           = 6.

  IF sy-subrc <> 0.
    "Fallback: Wenn SD bereits einen Rechnungsumrechnungskurs liefert,
    "verwenden wir diesen, damit die Position nicht verloren geht.
    IF iv_kurrf IS NOT INITIAL.
      cv_amount = iv_amount * iv_kurrf.
    ELSE.
      cv_amount = 0.
    ENDIF.
  ENDIF.
ENDFORM.

FORM classify_party
  USING    iv_kunnr TYPE kunnr
           iv_name1 TYPE name1_gp
  CHANGING cv_party TYPE c.

  DATA(lv_name) = to_upper( iv_name1 ).

  IF lv_name CS 'TRAFAG'
     OR lv_name CS 'MAGNETIC SENSE'
     OR lv_name CS 'MAGNETS SENSE'
     OR lv_name CS 'GESELLSCHAFT FUER SENSORIK'
     OR lv_name CS 'GESELLSCHAFT FUR SENSORIK'.
    cv_party = '2ND'.
  ELSE.
    cv_party = '3RD'.
  ENDIF.
ENDFORM.

FORM persist_zschweiz.
  IF p_test = abap_true.
    WRITE: / 'Testlauf aktiv: keine Daten in ZSCHWEIZ geschrieben.'.
    PERFORM write_totals.
    RETURN.
  ENDIF.

  MODIFY zschweiz FROM TABLE gt_zschweiz.

  IF sy-subrc = 0.
    COMMIT WORK AND WAIT.
    WRITE: / 'ZSCHWEIZ Upsert erfolgreich. Zeilen:', lines( gt_zschweiz ).
  ELSE.
    ROLLBACK WORK.
    MESSAGE 'ZSCHWEIZ Upsert fehlgeschlagen' TYPE 'E'.
  ENDIF.

  PERFORM write_totals.
ENDFORM.

FORM write_totals.
  TYPES: BEGIN OF ty_total,
           land1    TYPE land1,
           hwaer    TYPE waers,
           netwr_hc TYPE p LENGTH 23 DECIMALS 2,
           tax_hc   TYPE p LENGTH 23 DECIMALS 2,
           rows     TYPE i,
         END OF ty_total.

  DATA lt_totals TYPE HASHED TABLE OF ty_total WITH UNIQUE KEY land1 hwaer.

  LOOP AT gt_zschweiz ASSIGNING FIELD-SYMBOL(<ls_fin>).
    ASSIGN lt_totals[ land1 = <ls_fin>-land1 hwaer = <ls_fin>-hwaer ] TO FIELD-SYMBOL(<ls_total>).
    IF sy-subrc <> 0.
      INSERT VALUE #( land1 = <ls_fin>-land1 hwaer = <ls_fin>-hwaer ) INTO TABLE lt_totals ASSIGNING <ls_total>.
    ENDIF.

    <ls_total>-netwr_hc = <ls_total>-netwr_hc + <ls_fin>-netwr_hc.
    <ls_total>-tax_hc   = <ls_total>-tax_hc + <ls_fin>-tax_hc.
    <ls_total>-rows     = <ls_total>-rows + 1.
  ENDLOOP.

  SKIP.
  WRITE: / 'Summen nach Land/Hauswaehrung'.
  LOOP AT lt_totals ASSIGNING FIELD-SYMBOL(<ls_sum>).
    WRITE: / <ls_sum>-land1,
             <ls_sum>-hwaer,
             'Netto:', <ls_sum>-netwr_hc,
             'Steuer:', <ls_sum>-tax_hc,
             'Zeilen:', <ls_sum>-rows.
  ENDLOOP.
ENDFORM.
