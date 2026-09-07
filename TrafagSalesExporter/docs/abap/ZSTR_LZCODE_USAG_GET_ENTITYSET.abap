*&---------------------------------------------------------------------*
*& METHODENRUMPF fuer die redefinierte DPC_EXT-Methode
*& ZSTR_LZCODE_USAG_GET_ENTITYSET  (Gateway-Service ZPOWERBI_EINKAUF_SRV)
*&
*& VERSION 2026-07-23b - neues Feld VknrDispo (Disponent des Kopfmaterials,
*& MARC-DISPO Werk 1100) fuer den geplanten Produktgruppen-Aufriss im
*& Einkaufsdashboard. Die Produktgruppe haengt im Trafag-Modell am
*& Disponenten des FERT-Endprodukts (nicht der Komponente); dieses Feld
*& liefert je (Vknr,Kompnr)-Zeile den Disponenten des Vknr. Live gegen
*& travp762 verifiziert: die Bottom-Up-VKNRs einer Komponente sind FERT
*& und haben MARC-DISPO gefuellt (z.B. Disponent "019").
*&   WICHTIG (DDIC): Die Struktur ZSTR_LZCODE_USAGE muss in SE11 zuerst um
*&   ein Feld VKNR_DISPO (Datenelement DISPO) erweitert werden, sonst
*&   Syntaxfehler bei "ls_out-vknr_dispo". Danach diesen Rumpf einfuegen.
*&   Das Feld allein macht die Produktgruppe NICHT fertig: es fehlt weiter
*&   die Referenzliste Disponent -> Produktgruppe (ZC23) als lesbare Daten,
*&   und die Zurechnung des Spends bei Komponenten, die in mehreren
*&   Produktgruppen verbaut sind, ist fachlich noch offen.
*&
*& VERSION 2026-07-23 - numerische Materialnummern werden gefunden.
*& Befund (SapProbe/RFC gegen travp762 + OData-Testbatterie): Top-Down
*& fuer rein NUMERISCHE Vknr (z.B. "2217") lieferte IMMER 0 Zeilen -
*& auch mit der 18-stelligen Form und auch mit include_deleted (LVORM-
*& Filter aus). Alphanumerische Vknr (z.B. "D15019") funktionierten.
*& Direkt verifiziert: MARA hat "000000000000002217" mit LEEREM LVORM
*& (also NICHT loeschvorgemerkt - die 22d-Theorie war falsch), und
*& ZPOWERBI_VC_TXT hat die Zeilen mit gefuellter Menge/Einheit. Ursache:
*& Schritt 1 (SELECT matnr FROM mara) fand die numerische Nummer nicht,
*& weil CONVERSION_EXIT_ALPHA_INPUT (Version c) den Wert NICHT
*& zuverlaessig auf "000000000000002217" brachte - es zerstoerte sogar
*& die bereits gepaddete Eingabe. FIX: (1) den Rohwert IMMER in die
*& RANGE aufnehmen (die App schickt jetzt bereits 18-stellig gepaddet,
*& siehe MaterialUsageDataRefreshService.NormalizeMaterialToken -> immer
*& ein Treffer), (2) zusaetzlich die MATN1-konvertierte Form fuer kurze
*& manuelle Eingaben (CONVERSION_EXIT_MATN1_INPUT statt ALPHA - die
*& materialnummern-spezifische Konvertierung). Alphanumerische Nummern
*& bleiben unveraendert.
*&
*& VERSION 2026-07-22d - optionale Einbeziehung loeschvorgemerkter
*& Materialien (Wunsch Ingo). Nach dem ALPHA-Fix (Version c) lieferte
*& Top-Down fuer alte, numerische Vknr wie "2217" weiterhin 0 Zeilen,
*& obwohl Bottom-Up mit derselben Komponente (Kompnr=C34882) diese
*& Vknr korrekt als echten Treffer zurueckgab UND Top-Down fuer ein
*& "normales" Material (z.B. D15019) einwandfrei funktionierte -
*& eingegrenzt auf Schritt 1 (Materialselektion gegen MARA), die per
*& Default nur nicht-loeschvorgemerkte Materialien (`LVORM = ' '`)
*& zulaesst, genau wie der Original-Report per Default (`p_lvorm = ' '`).
*& Analog zur Report-Checkbox `p_lvorm` akzeptiert die Methode jetzt
*& einen Suffix "ALLE" am Richtung-Wert (`TOPDOWNALLE`/`BOTTOMUPALLE`)
*& - ohne DDIC-/SEGW-Aenderung, nur ueber den bestehenden String-Wert
*& transportiert. Das ausgegebene `Richtung`-Feld bleibt normalisiert
*& (`TOPDOWN`/`BOTTOMUP`, ohne Suffix).
*&
*& VERSION 2026-07-22c - siehe ALPHA-Konvertierung im Schritt-0-Block
*& unten (Vknr/Kompnr-Filterwerte). Bestaetigter Befund: Kurzform
*& ("2217") fand in Schritt 1 keinen MARA-Treffer, 18-stellige Form
*& ("000000000000002217") schon - MARA/ZPOWERBI_VC_TXT speichern
*& intern padded, eine selbstgeschriebene GET_ENTITYSET-Methode bekommt
*& Filterwerte aber roh. Ab dieser Version werden beide Schreibweisen
*& akzeptiert.
*&
*& VERSION 2026-07-22b - Zeilen-Drop bei fehlendem MAKTX entfernt.
*& Befund (Full Load gegen travp762, identischer $filter wie ein
*& interaktiver Browser-Test): App-Full-Load lieferte 0 Zeilen fuer
*& Vknr=2217/TOPDOWN, der Browser-Test mit demselben Filter lieferte
*& eine echte Zeile (Kompnr=C34882). Ursache: Version 2026-07-22a
*& uebernahm FIX 4 des Reports 1:1 und verwarf Zeilen ohne MAKTX
*& (Materialkurztext aus MAKT, gejoint ueber t~spras = sy-langu) - der
*& technische Service-User (Basic-Auth) kann eine andere SAP-
*& Anmeldesprache haben als ein interaktiver User, wodurch der Text
*& fehlt, obwohl Menge/Kosten/Bestand real sind. Fuer eine Excel-
*& Ausgabe ist das Wegfiltern sinnvoll (siehe Report), fuer einen
*& maschinell konsumierten Webservice nicht: die Zeile wird jetzt
*& IMMER ausgegeben, `KompnrMaktx` bleibt im Zweifel leer statt die
*& Zeile zu killen.
*&
*& VERSION 2026-07-22a - an die NEUE Reportfassung angepasst
*& (Quelle: docs/abap/originalzlo03.txt, Report ZM_LZCODE20_OPT mit
*& FIXES 1/2/4/5). Die Vorfassung 2026-07-21 las noch aus ZAT_VC -
*& diese Tabelle existiert auf travp762 (PROD) nicht, wodurch die
*& komplette DPC_EXT-Klasse nicht kompilierte und JEDES EntitySet des
*& Service (auch EKKOSet) mit SYNTAX_ERROR abbrach (Befund 2026-07-22).
*& Aenderungen gegenueber 2026-07-21:
*&   - Quelltabelle ZAT_VC -> ZPOWERBI_VC_TXT (matnr=VKNR, kompnr,
*&     menge, mengeneinheit, baugruppe, postyp, kom_mstae)
*&   - FIX 1: Rundung der Menge auf dec=0 ENTFERNT (0.070 M wurde 0)
*&   - FIX 2: kein DELETE ADJACENT DUPLICATES mehr - Mehrfach-
*&     verwendungen derselben Komponente werden SUMMIERT (COLLECT-
*&     Semantik des Reports), deterministisch ueber SORTED TABLE
*&   - FIX 4: Textpositionen (postyp = 'T') im Default ausgeschlossen
*&     (Report-Default p_txtpo = ' '); Zeilen-Drop bei fehlendem MAKTX
*&     in Version 2026-07-22b wieder entfernt (siehe oben)
*&   - Baugruppen-Kennzeichen wie fill_ktab: (VC-Baugruppe ODER
*&     Stueckliste in MAST) UND beskz <> 'F'
*&   - Stammdaten-JOIN ohne LVORM-Filter (Report laedt alle; LVORM
*&     wirkt nur auf die Materialselektion, Default p_lvorm = ' ')
*&
*& EINFUEGEN (wichtig, Fehlerquelle 2026-07-21 beim Parent-Set): Den
*& KOMPLETTEN Block unten INKLUSIVE "METHOD ..." und "ENDMETHOD." nehmen
*& und damit den bestehenden Methodenrumpf 1:1 ersetzen - im Editor
*& alles von "method zstr_lzcode_usag_get_entityset." bis zum
*& zugehoerigen "endmethod." markieren und ueberschreiben. KEINE
*& CLASS-Statements, alles lokale TYPES/DATA.
*&
*& NACH DEM ERSETZEN: Klasse aktivieren; danach Metadaten-Cache leeren
*& (/IWFND/CACHE_CLEANUP), sonst bleiben alte Laufzeitobjekte aktiv.
*& AUF BEIDEN SYSTEMEN NACHZIEHEN, auf denen die Methoden angelegt
*& wurden (travt762 UND travp762) - der SYNTAX_ERROR auf P verschwindet
*& erst, wenn dort kein ZAT_VC-Bezug mehr in der Klasse steht.
*&
*& OData-Nutzung:
*&   .../ZSTR_LZCODE_USAGESet?$filter=Richtung eq 'TOPDOWN' and Vknr eq 'E01758'
*&   - Richtung: 'TOPDOWN' (Default) oder 'BOTTOMUP'
*&   - Vknr/Kompnr: schraenken die Materialselektion ein (entspricht
*&     S_MATNR im Report; bei TOPDOWN fachlich Vknr filtern, bei
*&     BOTTOMUP Kompnr - technisch werden beide zusammengefuehrt)
*&   - OHNE Vknr/Kompnr-Filter wird abgebrochen (Report: "Bitte
*&     Selektion eingeben") - sonst wuerde ganz MARA selektiert.
*&   - S_TYPCD (ZZTYP_F4) ist NICHT filterbar: die Struktur hat kein
*&     solches Property. Bei Bedarf Feld in ZSTR_LZCODE_USAGE ergaenzen.
*&---------------------------------------------------------------------*

METHOD zstr_lzcode_usag_get_entityset.

    " ===================================================================
    " Lokale Typen
    " ===================================================================
    CONSTANTS lc_werks TYPE werks_d VALUE '1100'.

    TYPES: BEGIN OF ty_matnr_line,
             matnr TYPE matnr,
           END OF ty_matnr_line.
    TYPES tt_matnr_list TYPE STANDARD TABLE OF ty_matnr_line WITH DEFAULT KEY.

    TYPES: BEGIN OF ty_stamm,
             matnr        TYPE matnr,
             maktx        TYPE maktx,
             meins        TYPE meins,
             mstae        TYPE mstae,
             mstav        TYPE mstav,
             lvorm        TYPE lvorm,
             zzlzcod      TYPE mara-zzlzcod,
             zzlzcodsort  TYPE mara-zzlzcodsort,
             dismm        TYPE dismm,
             dispo        TYPE dispo,
             minbe        TYPE minbe,
             disls        TYPE disls,
             bstfe        TYPE bstfe,
             eisbe        TYPE eisbe,
             beskz        TYPE beskz,
             verpr        TYPE verpr,
             stprs        TYPE stprs,
             peinh        TYPE peinh,
             vprsv        TYPE vprsv,
             has_bom      TYPE abap_bool,
             stueckkosten TYPE p LENGTH 11 DECIMALS 2,
           END OF ty_stamm.
    TYPES tt_stamm TYPE HASHED TABLE OF ty_stamm WITH UNIQUE KEY matnr.

    TYPES: BEGIN OF ty_stamm_raw,
             matnr       TYPE matnr,
             maktx       TYPE maktx,
             meins       TYPE meins,
             mstae       TYPE mstae,
             mstav       TYPE mstav,
             lvorm       TYPE lvorm,
             zzlzcod     TYPE mara-zzlzcod,
             zzlzcodsort TYPE mara-zzlzcodsort,
             dismm       TYPE dismm,
             dispo       TYPE dispo,
             minbe       TYPE minbe,
             disls       TYPE disls,
             bstfe       TYPE bstfe,
             eisbe       TYPE eisbe,
             beskz       TYPE beskz,
             verpr       TYPE verpr,
             stprs       TYPE stprs,
             peinh       TYPE peinh,
             vprsv       TYPE vprsv,
           END OF ty_stamm_raw.

    TYPES: BEGIN OF ty_md04,
             matnr     TYPE matnr,
             labst     TYPE labst,
             feste_zug TYPE menge_d,
             gepl_zug  TYPE menge_d,
             feste_abg TYPE menge_d,
             gepl_abg  TYPE menge_d,
             verbr     TYPE menge_d,
             omeng     TYPE menge_d,
             owert     TYPE salk3,
             mkmng     TYPE menge_d,
             omkwr     TYPE salk3,
           END OF ty_md04.
    TYPES tt_md04 TYPE HASHED TABLE OF ty_md04 WITH UNIQUE KEY matnr.

    " Rohzeilen aus ZPOWERBI_VC_TXT: menge/meins bleiben Charfelder wie
    " in der Tabelle (kein CONVT_NO_NUMBER-Dump beim SELECT;
    " Konvertierung unten per TRY/CATCH).
    TYPES: BEGIN OF ty_pair_raw,
             vknr      TYPE matnr,
             kompnr    TYPE matnr,
             menge     TYPE zpowerbi_vc_txt-menge,
             meins     TYPE zpowerbi_vc_txt-mengeneinheit,
             baugruppe TYPE zpowerbi_vc_txt-baugruppe,
             postyp    TYPE zpowerbi_vc_txt-postyp,
           END OF ty_pair_raw.
    TYPES tt_pair_raw TYPE STANDARD TABLE OF ty_pair_raw WITH DEFAULT KEY.

    " Aggregierte Paare (FIX 2: Mengen je vknr/kompnr SUMMIERT statt
    " dedupliziert; SORTED TABLE => deterministische Ausgabereihenfolge)
    TYPES: BEGIN OF ty_pair,
             vknr      TYPE matnr,
             kompnr    TYPE matnr,
             menge     TYPE menge_d,
             baugruppe TYPE abap_bool,
           END OF ty_pair.
    TYPES tt_pair TYPE SORTED TABLE OF ty_pair WITH UNIQUE KEY vknr kompnr.

    " ===================================================================
    " Arbeitsvariablen
    " ===================================================================
    DATA: lt_mara_sel       TYPE STANDARD TABLE OF matnr,
          lt_pairs_raw      TYPE tt_pair_raw,
          lt_pairs          TYPE tt_pair,
          ls_pair_agg       TYPE ty_pair,
          lt_all_matnr      TYPE tt_matnr_list,
          ls_matnr_line     TYPE ty_matnr_line,
          lt_stamm          TYPE tt_stamm,
          lt_md04           TYPE tt_md04,
          lt_all_matnr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
          lv_matnr          TYPE matnr,
          lv_richtung       TYPE char10,
          lt_out            TYPE STANDARD TABLE OF zstr_lzcode_usage,
          ls_out            TYPE zstr_lzcode_usage,
          lt_r_matnr        TYPE RANGE OF matnr,
          lv_topdown        TYPE abap_bool VALUE abap_true,
          lv_include_deleted TYPE abap_bool VALUE abap_false.

    " ===================================================================
    " Schritt 0 (gateway-spezifisch): OData-$filter auslesen.
    " ===================================================================
    LOOP AT it_filter_select_options INTO DATA(ls_filter).
      CASE to_upper( ls_filter-property ).
        WHEN 'RICHTUNG'.
          READ TABLE ls_filter-select_options INTO DATA(ls_so_r) INDEX 1.
          IF sy-subrc = 0.
            DATA(lv_richtung_raw) = to_upper( ls_so_r-low ).
            CONDENSE lv_richtung_raw NO-GAPS.
            " "ALLE"-Suffix (Befund 2026-07-22, Wunsch Ingo, ohne DDIC-
            " Aenderung transportiert): bezieht auch loeschvorgemerkte
            " Kopf-/Filtermaterialien mit ein, analog Report-Checkbox
            " p_lvorm. Werte: TOPDOWN, TOPDOWNALLE, BOTTOMUP, BOTTOMUPALLE.
            " Robust gegen Laengen-Truncation (RICHTUNG ist im DDIC
            " CHAR10, "TOPDOWNALLE" hat 11 Zeichen): JEDER Suffix hinter
            " dem reinen Richtungswort aktiviert die Option - auch ein
            " ggf. auf "TOPDOWNALL"/"BOTTOMUPAL" gekappter Wert.
            IF lv_richtung_raw CP 'BOTTOMUP*'.
              lv_topdown = abap_false.
              IF lv_richtung_raw <> 'BOTTOMUP'.
                lv_include_deleted = abap_true.
              ENDIF.
            ELSEIF lv_richtung_raw CP 'TOPDOWN*'.
              IF lv_richtung_raw <> 'TOPDOWN'.
                lv_include_deleted = abap_true.
              ENDIF.
            ENDIF.
          ENDIF.
        WHEN 'VKNR' OR 'KOMPNR'.
          LOOP AT ls_filter-select_options INTO DATA(ls_so).
            " MATNR-Konvertierung (FIX, Befund 2026-07-23 an travp762):
            " Eine selbstgeschriebene GET_ENTITYSET-Methode bekommt
            " it_filter_select_options ROH, d.h. OHNE die MATNR-
            " Konvertierung. MARA/ZPOWERBI_VC_TXT speichern intern
            " 18-stellig mit fuehrenden Nullen ("000000000000002217").
            " Eine rein numerische Kurzform ("2217") fand in Schritt 1
            " (SELECT matnr FROM mara) sonst NICHTS und brach mit 0
            " Zeilen ab (alphanumerische Nummern wie "D15019" gehen, weil
            " MARA sie linksbuendig speichert). Die Vorversion nutzte
            " CONVERSION_EXIT_ALPHA_INPUT - das hat hier NICHT zuverlaessig
            " zero-gepaddet (auch die bereits gepaddete Eingabe wurde
            " zerstoert, live verifiziert). Deshalb jetzt:
            "  (1) den ROHWERT immer aufnehmen (deckt die von der App
            "      bereits 18-stellig gepaddete Eingabe ab -> sicherer
            "      Treffer, unabhaengig von jeder Konvertierung), UND
            "  (2) zusaetzlich die MATN1-konvertierte Form fuer kurze
            "      manuelle Eingaben. MATN1 ist die materialnummern-
            "      spezifische Konvertierung (respektiert das Material-
            "      nummern-Customizing), nicht die generische ALPHA.
            DATA lv_m1_low  TYPE matnr.
            DATA lv_m1_high TYPE matnr.

            " (1) Rohwert immer aufnehmen
            APPEND VALUE #( sign   = ls_so-sign
                            option = ls_so-option
                            low    = ls_so-low
                            high   = ls_so-high ) TO lt_r_matnr.

            " (2) MATN1-Form zusaetzlich, wenn sie sich unterscheidet
            CLEAR: lv_m1_low, lv_m1_high.
            IF ls_so-low IS NOT INITIAL.
              CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
                EXPORTING input        = ls_so-low
                IMPORTING output       = lv_m1_low
                EXCEPTIONS length_error = 1
                           OTHERS       = 2.
              IF sy-subrc <> 0.
                lv_m1_low = ls_so-low.
              ENDIF.
            ENDIF.
            IF ls_so-high IS NOT INITIAL.
              CALL FUNCTION 'CONVERSION_EXIT_MATN1_INPUT'
                EXPORTING input        = ls_so-high
                IMPORTING output       = lv_m1_high
                EXCEPTIONS length_error = 1
                           OTHERS       = 2.
              IF sy-subrc <> 0.
                lv_m1_high = ls_so-high.
              ENDIF.
            ENDIF.
            IF lv_m1_low <> ls_so-low OR lv_m1_high <> ls_so-high.
              APPEND VALUE #( sign   = ls_so-sign
                              option = ls_so-option
                              low    = lv_m1_low
                              high   = lv_m1_high ) TO lt_r_matnr.
            ENDIF.
          ENDLOOP.
      ENDCASE.
    ENDLOOP.

    lv_richtung = COND #( WHEN lv_topdown = abap_true THEN 'TOPDOWN' ELSE 'BOTTOMUP' ).

    " Ohne Materialeinschraenkung abbrechen - Gegenstueck zum Report
    " ("Bitte Selektion eingeben"); verhindert Vollselektion auf MARA.
    IF lt_r_matnr IS INITIAL.
      RAISE EXCEPTION TYPE /iwbep/cx_mgw_busi_exception
        EXPORTING
          textid  = /iwbep/cx_mgw_busi_exception=>business_error
          message = 'Filter Vknr oder Kompnr angeben (z.B. $filter=Vknr eq ''E01758'')'.
    ENDIF.

    " -----------------------------------------------------------
    " Schritt 1: Materialselektion (process_main Schritt 1).
    " Kein MTART-'FERT'-Filter (im Report 2.3.26 deaktiviert).
    " LVORM-Default wie p_lvorm = ' ': nur nicht-loeschvorgemerkte -
    " AUSSER lv_include_deleted (Richtung-Suffix "ALLE", siehe oben).
    " Befund 2026-07-22: alte, numerische Vknr (z.B. "2217") sind
    " loeschvorgemerkt und lieferten ohne diese Option 0 Zeilen, obwohl
    " die Verwendung in ZPOWERBI_VC_TXT noch vorhanden ist (per
    " Bottom-Up bestaetigt).
    " -----------------------------------------------------------
    IF lv_include_deleted = abap_true.
      SELECT matnr FROM mara
        INTO TABLE @lt_mara_sel
        WHERE matnr IN @lt_r_matnr.
    ELSE.
      SELECT matnr FROM mara
        INTO TABLE @lt_mara_sel
        WHERE matnr IN @lt_r_matnr
          AND lvorm = @space.
    ENDIF.

    IF lt_mara_sel IS INITIAL.
      RETURN.
    ENDIF.

    " -----------------------------------------------------------
    " Schritt 2: ZPOWERBI_VC_TXT lesen (Rollen je Richtung getauscht;
    " matnr = VKNR/Kopfmaterial, kompnr = Komponente)
    " -----------------------------------------------------------
    IF lv_topdown = abap_true.
      SELECT matnr AS vknr, kompnr, menge, mengeneinheit AS meins,
             baugruppe, postyp
        FROM zpowerbi_vc_txt
        INTO CORRESPONDING FIELDS OF TABLE @lt_pairs_raw
        FOR ALL ENTRIES IN @lt_mara_sel
        WHERE matnr = @lt_mara_sel-table_line
          AND menge <> @space
          AND mengeneinheit <> @space.
    ELSE.
      SELECT matnr AS vknr, kompnr, menge, mengeneinheit AS meins,
             baugruppe, postyp
        FROM zpowerbi_vc_txt
        INTO CORRESPONDING FIELDS OF TABLE @lt_pairs_raw
        FOR ALL ENTRIES IN @lt_mara_sel
        WHERE kompnr = @lt_mara_sel-table_line
          AND menge <> @space
          AND mengeneinheit <> @space.
    ENDIF.

    IF lt_pairs_raw IS INITIAL.
      RETURN.
    ENDIF.

    " -----------------------------------------------------------
    " Schritt 3: Alle beteiligten Materialnummern sammeln
    " -----------------------------------------------------------
    LOOP AT lt_pairs_raw INTO DATA(ls_pair_collect).
      INSERT ls_pair_collect-vknr INTO TABLE lt_all_matnr_temp.
      INSERT ls_pair_collect-kompnr INTO TABLE lt_all_matnr_temp.
    ENDLOOP.

    LOOP AT lt_all_matnr_temp INTO lv_matnr.
      ls_matnr_line-matnr = lv_matnr.
      APPEND ls_matnr_line TO lt_all_matnr.
    ENDLOOP.
    SORT lt_all_matnr BY matnr.

    " ===================================================================
    " Schritt 4: Stammdaten bulk laden (load_stammdaten_cache).
    " OHNE LVORM-Filter - der Report laedt alle Stammsaetze und prueft
    " LVORM nur im Bottom-Up-Zweig.
    " ===================================================================
    IF lt_all_matnr IS NOT INITIAL.
      DATA lt_stamm_raw TYPE STANDARD TABLE OF ty_stamm_raw.
      DATA lt_bom       TYPE STANDARD TABLE OF matnr.

      SELECT m~matnr, t~maktx, m~meins, m~mstae, m~mstav, m~lvorm,
             m~zzlzcod, m~zzlzcodsort,
             c~dismm, c~dispo, c~minbe, c~disls, c~bstfe, c~eisbe, c~beskz,
             b~verpr, b~stprs, b~peinh, b~vprsv
        FROM mara AS m
        LEFT JOIN makt AS t ON t~matnr = m~matnr AND t~spras = @sy-langu
        LEFT JOIN marc AS c ON c~matnr = m~matnr AND c~werks = @lc_werks
        LEFT JOIN mbew AS b ON b~matnr = m~matnr AND b~bwkey = @lc_werks
                           AND b~bwtar = @space
        INTO CORRESPONDING FIELDS OF TABLE @lt_stamm_raw
        FOR ALL ENTRIES IN @lt_all_matnr
        WHERE m~matnr = @lt_all_matnr-matnr.

      SELECT matnr FROM mast
        INTO TABLE @lt_bom
        FOR ALL ENTRIES IN @lt_all_matnr
        WHERE matnr = @lt_all_matnr-matnr
          AND werks = @lc_werks
          AND stlan = '1'.
      SORT lt_bom.
      DELETE ADJACENT DUPLICATES FROM lt_bom.

      LOOP AT lt_stamm_raw INTO DATA(ls_stamm_raw).
        DATA(ls_stamm) = VALUE ty_stamm(
          matnr       = ls_stamm_raw-matnr
          maktx       = ls_stamm_raw-maktx
          meins       = ls_stamm_raw-meins
          mstae       = ls_stamm_raw-mstae
          mstav       = ls_stamm_raw-mstav
          lvorm       = ls_stamm_raw-lvorm
          zzlzcod     = ls_stamm_raw-zzlzcod
          zzlzcodsort = ls_stamm_raw-zzlzcodsort
          dismm       = ls_stamm_raw-dismm
          dispo       = ls_stamm_raw-dispo
          minbe       = ls_stamm_raw-minbe
          disls       = ls_stamm_raw-disls
          bstfe       = ls_stamm_raw-bstfe
          eisbe       = ls_stamm_raw-eisbe
          beskz       = ls_stamm_raw-beskz
          verpr       = ls_stamm_raw-verpr
          stprs       = ls_stamm_raw-stprs
          peinh       = COND #( WHEN ls_stamm_raw-peinh IS INITIAL THEN 1
                                ELSE ls_stamm_raw-peinh )
          vprsv       = ls_stamm_raw-vprsv ).

        READ TABLE lt_bom TRANSPORTING NO FIELDS
          WITH KEY table_line = ls_stamm_raw-matnr BINARY SEARCH.
        ls_stamm-has_bom = COND #( WHEN sy-subrc = 0 THEN abap_true
                                   ELSE abap_false ).

        " Stueckkosten wie fill_ktab: VERPR bei VPRSV='V', sonst STPRS
        ls_stamm-stueckkosten =
          COND #( WHEN ls_stamm-vprsv = 'V'
                  THEN ls_stamm-verpr / ls_stamm-peinh
                  ELSE ls_stamm-stprs / ls_stamm-peinh ).

        INSERT ls_stamm INTO TABLE lt_stamm.
      ENDLOOP.
    ENDIF.

    " ===================================================================
    " Schritt 5: ZMD04_CALC bulk laden (kein Live-MD04-Aufbau)
    " ===================================================================
    IF lt_all_matnr IS NOT INITIAL.
      DATA lt_md04_raw TYPE STANDARD TABLE OF zmd04_calc.

      SELECT matnr, werks, labst, feste_zug, gepl_zug, feste_abg, gepl_abg,
             verbr, omeng, owert, mkmng, omkwr
        FROM zmd04_calc
        INTO CORRESPONDING FIELDS OF TABLE @lt_md04_raw
        FOR ALL ENTRIES IN @lt_all_matnr
        WHERE matnr = @lt_all_matnr-matnr
          AND werks = @lc_werks.

      LOOP AT lt_md04_raw INTO DATA(ls_md04_raw).
        INSERT VALUE ty_md04( matnr     = ls_md04_raw-matnr
                              labst     = ls_md04_raw-labst
                              feste_zug = ls_md04_raw-feste_zug
                              gepl_zug  = ls_md04_raw-gepl_zug
                              feste_abg = ls_md04_raw-feste_abg
                              gepl_abg  = ls_md04_raw-gepl_abg
                              verbr     = ls_md04_raw-verbr
                              omeng     = ls_md04_raw-omeng
                              owert     = ls_md04_raw-owert
                              mkmng     = ls_md04_raw-mkmng
                              omkwr     = ls_md04_raw-omkwr )
          INTO TABLE lt_md04.
      ENDLOOP.
    ENDIF.

    " ===================================================================
    " Schritt 6: Rohzeilen konvertieren und je vknr/kompnr AGGREGIEREN
    " (FIX 2: COLLECT-Semantik des Reports - Mengen summieren).
    " Mengenkonvertierung je Rohzeile VOR der Summierung (wie im Report:
    " convert_menge je lt_powerbi-Zeile, dann COLLECT).
    " ===================================================================
    LOOP AT lt_pairs_raw INTO DATA(ls_pair_raw).

      IF lv_topdown = abap_true.
        " FIX 4 (Report-Default p_txtpo = ' '): Textpositionen nicht
        " in die Bedarfsrechnung aufnehmen.
        IF ls_pair_raw-postyp = 'T'.
          CONTINUE.
        ENDIF.
      ELSE.
        " Bottom-Up (Report): Kopfmaterial ohne Stammsatz oder mit
        " Loeschvormerkung ueberspringen (p_lvorm-Default ' '), AUSSER
        " lv_include_deleted ist gesetzt (Richtung-Suffix "ALLE").
        READ TABLE lt_stamm INTO DATA(ls_stamm_bu)
          WITH TABLE KEY matnr = ls_pair_raw-vknr.
        IF sy-subrc <> 0.
          CONTINUE.
        ENDIF.
        IF ls_stamm_bu-lvorm IS NOT INITIAL AND lv_include_deleted = abap_false.
          CONTINUE.
        ENDIF.
      ENDIF.

      " Ziel-Mengeneinheit = Einheit der Komponente (Fallback: Quelle)
      DATA lv_meins_komp TYPE meins.
      CLEAR lv_meins_komp.
      READ TABLE lt_stamm INTO DATA(ls_stamm_k)
        WITH TABLE KEY matnr = ls_pair_raw-kompnr.
      IF sy-subrc = 0.
        lv_meins_komp = ls_stamm_k-meins.
      ELSE.
        lv_meins_komp = ls_pair_raw-meins.
      ENDIF.

      " ---- Mengenkonvertierung (convert_menge; FIX 1: KEINE Rundung,
      "      FIX 5: leere Einheiten abgefangen; kein RETURN!) ----
      DATA lv_conv_str       TYPE string.
      DATA lv_conv_menge_in  TYPE menge_d.
      DATA lv_conv_menge_out TYPE menge_d.
      DATA lv_conv_ok        TYPE abap_bool.
      DATA lv_menge          TYPE menge_d.

      CLEAR: lv_conv_str, lv_conv_menge_in, lv_conv_menge_out, lv_menge.
      lv_conv_ok = abap_true.

      lv_conv_str = ls_pair_raw-menge.
      CONDENSE lv_conv_str NO-GAPS.

      IF lv_conv_str IS NOT INITIAL.
        TRY.
            lv_conv_menge_in = lv_conv_str.
          CATCH cx_sy_conversion_error.
            lv_conv_ok = abap_false.
        ENDTRY.

        IF lv_conv_ok = abap_true.
          IF lv_meins_komp <> ls_pair_raw-meins
             AND lv_meins_komp IS NOT INITIAL
             AND ls_pair_raw-meins IS NOT INITIAL.
            CALL FUNCTION 'UNIT_CONVERSION_SIMPLE'
              EXPORTING
                input                = lv_conv_menge_in
                unit_in              = ls_pair_raw-meins
                unit_out             = lv_meins_komp
              IMPORTING
                output               = lv_conv_menge_out
              EXCEPTIONS
                conversion_not_found = 1
                division_by_zero     = 2
                input_invalid        = 3
                output_invalid       = 4
                overflow             = 5
                type_invalid         = 6
                units_missing        = 7
                unit_in_not_found    = 8
                unit_out_not_found   = 9
                OTHERS               = 10.
            lv_menge = COND #( WHEN sy-subrc = 0 THEN lv_conv_menge_out
                               ELSE lv_conv_menge_in ).
          ELSE.
            lv_menge = lv_conv_menge_in.
          ENDIF.
          " FIX 1: Rundung auf dec=0 ENTFERNT (0.070 M wurde zu 0)
        ENDIF.
      ENDIF.
      " ---- Ende Mengenkonvertierung ----

      " Aggregieren: Menge summieren, Baugruppen-Flag ODER-verknuepfen
      " (Baugruppe aus der VC-Zeile nur Top-Down, wie fill_ktab-Aufruf)
      READ TABLE lt_pairs INTO ls_pair_agg
        WITH TABLE KEY vknr   = ls_pair_raw-vknr
                       kompnr = ls_pair_raw-kompnr.
      IF sy-subrc = 0.
        ls_pair_agg-menge = ls_pair_agg-menge + lv_menge.
        IF lv_topdown = abap_true AND ls_pair_raw-baugruppe = 'X'.
          ls_pair_agg-baugruppe = abap_true.
        ENDIF.
        MODIFY TABLE lt_pairs FROM ls_pair_agg.
      ELSE.
        ls_pair_agg = VALUE ty_pair(
          vknr      = ls_pair_raw-vknr
          kompnr    = ls_pair_raw-kompnr
          menge     = lv_menge
          baugruppe = COND #( WHEN lv_topdown = abap_true
                                   AND ls_pair_raw-baugruppe = 'X'
                              THEN abap_true ELSE abap_false ) ).
        INSERT ls_pair_agg INTO TABLE lt_pairs.
      ENDIF.

    ENDLOOP.

    IF lt_pairs IS INITIAL.
      RETURN.
    ENDIF.

    " -----------------------------------------------------------
    " Schritt 7: Kopfmaterial-Zusatzfelder je Vknr (VTAB im Report)
    " -----------------------------------------------------------
    " VknrDispo (Disponent des Kopfmaterials, MARC-DISPO Werk 1100) - Schluessel fuer die
    " Produktgruppen-Zuordnung (Disponent -> Produktgruppe ueber ZC23-Referenzliste). Nur
    " Top-Down fachlich belegt (Vknr ist dann das FERT-Kopfmaterial); bei Bottom-Up ist Vknr
    " das Verwendungsmaterial, dessen Disponent hier ebenfalls durchgereicht wird (Client
    " entscheidet, ob er ihn nutzt).
    TYPES: BEGIN OF ty_vknr_info,
             vknr  TYPE matnr,
             mstae TYPE mstae,
             dispo TYPE dispo,
             verbr TYPE menge_d,
           END OF ty_vknr_info.
    DATA lt_vknr_info TYPE HASHED TABLE OF ty_vknr_info WITH UNIQUE KEY vknr.

    LOOP AT lt_pairs INTO DATA(ls_pair_vknr).
      READ TABLE lt_vknr_info TRANSPORTING NO FIELDS
        WITH TABLE KEY vknr = ls_pair_vknr-vknr.
      IF sy-subrc <> 0.
        DATA(ls_vknr_info) = VALUE ty_vknr_info( vknr = ls_pair_vknr-vknr ).
        READ TABLE lt_stamm INTO DATA(ls_stamm_v)
          WITH TABLE KEY matnr = ls_pair_vknr-vknr.
        IF sy-subrc = 0.
          ls_vknr_info-mstae = ls_stamm_v-mstae.
          ls_vknr_info-dispo = ls_stamm_v-dispo.
        ENDIF.
        READ TABLE lt_md04 INTO DATA(ls_md04_v)
          WITH TABLE KEY matnr = ls_pair_vknr-vknr.
        IF sy-subrc = 0.
          ls_vknr_info-verbr = ls_md04_v-verbr.
        ENDIF.
        INSERT ls_vknr_info INTO TABLE lt_vknr_info.
      ENDIF.
    ENDLOOP.

    " -----------------------------------------------------------
    " Schritt 8: Exklusivitaet - nur Top-Down fachlich belegt
    " (load_exclusivity_topdown; Quelle ZPOWERBI_VC_TXT)
    " -----------------------------------------------------------
    DATA lt_exklusiv TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.

    IF lv_topdown = abap_true.

      DATA: lt_kompnr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
            lt_kompnr_list TYPE tt_matnr_list.
      LOOP AT lt_pairs INTO DATA(ls_pair_k).
        INSERT ls_pair_k-kompnr INTO TABLE lt_kompnr_temp.
      ENDLOOP.
      LOOP AT lt_kompnr_temp INTO lv_matnr.
        ls_matnr_line-matnr = lv_matnr.
        APPEND ls_matnr_line TO lt_kompnr_list.
      ENDLOOP.

      DATA lt_sel_vknr TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line.
      LOOP AT lt_pairs INTO DATA(ls_pair_v).
        INSERT ls_pair_v-vknr INTO TABLE lt_sel_vknr.
      ENDLOOP.

      TYPES: BEGIN OF ty_usage,
               kompnr TYPE matnr,
               vknr   TYPE matnr,
             END OF ty_usage.
      DATA lt_usage_raw TYPE STANDARD TABLE OF ty_usage.
      DATA lt_usage     TYPE SORTED TABLE OF ty_usage
                          WITH NON-UNIQUE KEY kompnr vknr.

      SELECT kompnr, matnr AS vknr
        FROM zpowerbi_vc_txt
        INTO CORRESPONDING FIELDS OF TABLE @lt_usage_raw
        FOR ALL ENTRIES IN @lt_kompnr_list
        WHERE kompnr = @lt_kompnr_list-matnr.

      SORT lt_usage_raw BY kompnr vknr.
      DELETE ADJACENT DUPLICATES FROM lt_usage_raw COMPARING kompnr vknr.
      lt_usage = lt_usage_raw.
      FREE lt_usage_raw.

      DATA: lt_usage_vknr_temp TYPE HASHED TABLE OF matnr WITH UNIQUE KEY table_line,
            lt_usage_vknr_list TYPE tt_matnr_list.
      LOOP AT lt_usage INTO DATA(ls_usage_collect).
        INSERT ls_usage_collect-vknr INTO TABLE lt_usage_vknr_temp.
      ENDLOOP.
      LOOP AT lt_usage_vknr_temp INTO lv_matnr.
        ls_matnr_line-matnr = lv_matnr.
        APPEND ls_matnr_line TO lt_usage_vknr_list.
      ENDLOOP.

      TYPES: BEGIN OF ty_vknr_mara,
               matnr TYPE matnr,
               mtart TYPE mtart,
               mstae TYPE mstae,
             END OF ty_vknr_mara.
      DATA lt_vknr_mara TYPE HASHED TABLE OF ty_vknr_mara WITH UNIQUE KEY matnr.
      IF lt_usage_vknr_list IS NOT INITIAL.
        SELECT matnr, mtart, mstae FROM mara
          INTO CORRESPONDING FIELDS OF TABLE @lt_vknr_mara
          FOR ALL ENTRIES IN @lt_usage_vknr_list
          WHERE matnr = @lt_usage_vknr_list-matnr.
      ENDIF.

      LOOP AT lt_kompnr_list INTO DATA(ls_kompnr_l).
        DATA(lv_is_exklusiv) = abap_true.
        LOOP AT lt_usage INTO DATA(ls_usage)
             WHERE kompnr = ls_kompnr_l-matnr.
          READ TABLE lt_sel_vknr TRANSPORTING NO FIELDS
            WITH TABLE KEY table_line = ls_usage-vknr.
          IF sy-subrc <> 0.
            READ TABLE lt_vknr_mara INTO DATA(ls_vm)
              WITH TABLE KEY matnr = ls_usage-vknr.
            IF sy-subrc = 0 AND ls_vm-mtart = 'FERT' AND ls_vm-mstae <> '99'.
              lv_is_exklusiv = abap_false.
              EXIT.
            ENDIF.
          ENDIF.
        ENDLOOP.
        IF lv_is_exklusiv = abap_true.
          INSERT ls_kompnr_l-matnr INTO TABLE lt_exklusiv.
        ENDIF.
      ENDLOOP.

    ENDIF.

    " ===================================================================
    " Schritt 9: Ausgabezeilen aus den aggregierten Paaren bauen
    " ===================================================================
    LOOP AT lt_pairs INTO DATA(ls_pair).

      " NICHT uebernommen aus dem Report: FIX 4 loescht dort
      " Excel-Zeilen ohne MAKTX (Report: DELETE gt_ktab WHERE maktx IS
      " INITIAL), weil eine Excel-Zeile ohne erkennbaren Materialtext
      " fuer einen Leser wertlos ist. Fuer diesen Webservice waere das
      " FALSCH: die MAKT-Textsuche joint ueber t~spras = sy-langu, also
      " sprachabhaengig vom aufrufenden User. Der technische Service-
      " User kann eine andere SAP-Anmeldesprache haben als ein
      " interaktiver Tester - dann fehlt der Text zwar, aber Menge/
      " Kosten/Bestand sind trotzdem echt und duerfen nicht durch einen
      " reinen Sprachzufall verschwinden (Befund 2026-07-22: identischer
      " $filter lieferte im Full Load 0 Zeilen, im interaktiven Browser-
      " Test mit demselben Vknr/Kompnr eine echte Zeile). KompnrMaktx
      " bleibt in diesem Fall einfach leer statt die Zeile zu killen.

      " WAERS fest 'CHF' (Werk 1100 = Trafag AG/CH/CHF) - Referenzfeld
      " der CURR-Felder OWERT/OMKWR.
      ls_out = VALUE zstr_lzcode_usage( richtung = lv_richtung
                                        vknr     = ls_pair-vknr
                                        kompnr   = ls_pair-kompnr
                                        waers    = 'CHF'
                                        menge    = ls_pair-menge ).

      READ TABLE lt_vknr_info INTO DATA(ls_vi)
        WITH TABLE KEY vknr = ls_pair-vknr.
      IF sy-subrc = 0.
        ls_out-vknr_mstae     = ls_vi-mstae.
        ls_out-vknr_verbrauch = ls_vi-verbr.
        ls_out-vknr_dispo     = ls_vi-dispo.
      ENDIF.

      READ TABLE lt_stamm INTO DATA(ls_stamm2)
        WITH TABLE KEY matnr = ls_pair-kompnr.
      IF sy-subrc = 0.
        ls_out-kompnr_maktx  = ls_stamm2-maktx.
        ls_out-kompnr_meins  = ls_stamm2-meins.
        ls_out-dismm         = ls_stamm2-dismm.
        ls_out-minbe         = ls_stamm2-minbe.
        ls_out-disls         = ls_stamm2-disls.
        ls_out-bstfe         = ls_stamm2-bstfe.
        ls_out-eisbe         = ls_stamm2-eisbe.
        ls_out-mstae         = ls_stamm2-mstae.
        ls_out-mstav         = ls_stamm2-mstav.
        ls_out-beskz         = ls_stamm2-beskz.
        ls_out-zzlzcod       = ls_stamm2-zzlzcod.
        ls_out-zzlzcodsort   = ls_stamm2-zzlzcodsort.
        ls_out-stueckkosten  = ls_stamm2-stueckkosten.

        " Baugruppen-Kennzeichen wie fill_ktab (neue Fassung):
        " (VC-Baugruppe ODER Stueckliste vorhanden) UND beskz <> 'F'
        IF ( ls_pair-baugruppe = abap_true OR ls_stamm2-has_bom = abap_true )
           AND ls_stamm2-beskz <> 'F'.
          ls_out-baugruppe = abap_true.
        ELSE.
          ls_out-baugruppe = abap_false.
        ENDIF.
      ENDIF.

      READ TABLE lt_exklusiv TRANSPORTING NO FIELDS
        WITH TABLE KEY table_line = ls_pair-kompnr.
      ls_out-exklusiv = COND #( WHEN sy-subrc = 0 THEN abap_true ELSE abap_false ).

      READ TABLE lt_md04 INTO DATA(ls_md04)
        WITH TABLE KEY matnr = ls_pair-kompnr.
      IF sy-subrc = 0.
        ls_out-verbrauch    = ls_md04-verbr.
        ls_out-labst        = ls_md04-labst.
        ls_out-feste_zugang = ls_md04-feste_zug.
        ls_out-gepl_zugang  = ls_md04-gepl_zug.
        ls_out-feste_abgang = ls_md04-feste_abg.
        ls_out-gepl_abgang  = ls_md04-gepl_abg.
        ls_out-omeng        = ls_md04-omeng.
        ls_out-owert        = ls_md04-owert.
        ls_out-mkmng        = ls_md04-mkmng.
        ls_out-omkwr        = ls_md04-omkwr.

        ls_out-endbestand = ls_out-labst
                          + ls_out-feste_zugang + ls_out-gepl_zugang
                          - ls_out-feste_abgang - ls_out-gepl_abgang.

        ls_out-wert_feste_zug  = ls_out-feste_zugang * ls_out-stueckkosten.
        ls_out-wert_gepl_zug   = ls_out-gepl_zugang  * ls_out-stueckkosten.
        ls_out-wert_feste_abg  = ls_out-feste_abgang * ls_out-stueckkosten.
        ls_out-wert_gepl_abg   = ls_out-gepl_abgang  * ls_out-stueckkosten.
        ls_out-wert_endbestand = ls_out-endbestand   * ls_out-stueckkosten.
      ENDIF.

      APPEND ls_out TO lt_out.
    ENDLOOP.

    " ===================================================================
    " Schritt 10 (gateway-spezifisch): $skip/$top anwenden und in
    " et_entityset uebertragen. MOVE-CORRESPONDING, weil der generierte
    " Entity-Zeilentyp dieselben Feldnamen wie ZSTR_LZCODE_USAGE hat.
    " ===================================================================
    IF is_paging-skip > 0.
      DELETE lt_out TO is_paging-skip.
    ENDIF.
    IF is_paging-top > 0.
      DELETE lt_out FROM is_paging-top + 1.
    ENDIF.

    LOOP AT lt_out INTO ls_out.
      APPEND INITIAL LINE TO et_entityset ASSIGNING FIELD-SYMBOL(<fs_entity>).
      MOVE-CORRESPONDING ls_out TO <fs_entity>.
    ENDLOOP.

ENDMETHOD.
