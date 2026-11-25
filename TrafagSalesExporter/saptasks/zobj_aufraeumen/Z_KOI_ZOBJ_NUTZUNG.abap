REPORT z_koi_zobj_nutzung.
*&---------------------------------------------------------------------*
*& Z_KOI_ZOBJ_NUTZUNG
*& Nutzungsanalyse eigener Repository-Objekte (TADIR R3TR), nur lesend.
*& Liefert je Objekt Stammdaten, Verwender, Jobs, Rollen, Nutzung und
*& eine Einstufung: genutzt / indirekt / tot / unklar.
*& Der Report entscheidet nichts und loescht nichts.
*& Regeln und Quellen: Repo TrafagSalesExporter,
*& saptasks/zobj_aufraeumen/NOTES.md (angelegt 2026-11-25, KOI)
*&---------------------------------------------------------------------*

TABLES tadir.

SELECTION-SCREEN BEGIN OF BLOCK b1 WITH FRAME.
  SELECT-OPTIONS: s_name FOR tadir-obj_name,
                  s_pack FOR tadir-devclass,
                  s_type FOR tadir-object.
  PARAMETERS: p_mon  TYPE i DEFAULT 12,
              p_kand AS CHECKBOX DEFAULT ' '.
SELECTION-SCREEN END OF BLOCK b1.

TYPES: BEGIN OF ty_out,
         object     TYPE tadir-object,
         obj_name   TYPE tadir-obj_name,
         devclass   TYPE tadir-devclass,
         author     TYPE tadir-author,
         created_on TYPE tadir-created_on,
         subc       TYPE trdir-subc,
         chg_date   TYPE d,
         chg_src    TYPE char10,
         tcodes     TYPE char80,
         roles      TYPE i,
         jobs_plan  TYPE i,
         job_last   TYPE d,
         use_z      TYPE i,
         use_other  TYPE i,
         st03_cnt   TYPE p LENGTH 16 DECIMALS 0,
         st03_month TYPE char7,
         scmon_cnt  TYPE p LENGTH 16 DECIMALS 0,
         scmon_last TYPE d,
         susg_cnt   TYPE p LENGTH 16 DECIMALS 0,
         susg_last  TYPE d,
         klasse     TYPE char8,
         grund      TYPE char128,
         zeitraum   TYPE char80,
       END OF ty_out,
       tt_out TYPE STANDARD TABLE OF ty_out WITH DEFAULT KEY.

TYPES: BEGIN OF ty_dat,
         name TYPE progname,
         udat TYPE d,
       END OF ty_dat,
       BEGIN OF ty_ddic,
         object TYPE tadir-object,
         name   TYPE tadir-obj_name,
         udat   TYPE d,
       END OF ty_ddic,
       BEGIN OF ty_trdir,
         name TYPE progname,
         subc TYPE trdir-subc,
         udat TYPE d,
       END OF ty_trdir,
       BEGIN OF ty_tc,
         tcode TYPE tstc-tcode,
         pgmna TYPE tstc-pgmna,
       END OF ty_tc,
       BEGIN OF ty_fm,
         funcname TYPE tfdir-funcname,
         grp      TYPE tadir-obj_name,
       END OF ty_fm,
       BEGIN OF ty_use,
         object   TYPE tadir-object,
         obj_name TYPE tadir-obj_name,
         user     TYPE char40,
         kunde    TYPE abap_bool,
       END OF ty_use,
       BEGIN OF ty_st03,
         typ  TYPE char1,
         name TYPE char40,
         cnt  TYPE p LENGTH 16 DECIMALS 0,
         last TYPE d,
       END OF ty_st03,
       BEGIN OF ty_job,
         name TYPE progname,
         plan TYPE i,
         last TYPE d,
       END OF ty_job,
       BEGIN OF ty_agr,
         tcode    TYPE tstc-tcode,
         agr_name TYPE agr_tcodes-agr_name,
       END OF ty_agr,
       BEGIN OF ty_key,
         typ  TYPE cross-type,
         name TYPE cross-name,
       END OF ty_key,
       BEGIN OF ty_nutz,
         object TYPE tadir-object,
         name   TYPE tadir-obj_name,
         cnt    TYPE p LENGTH 16 DECIMALS 0,
         last   TYPE d,
       END OF ty_nutz,
       tt_dat   TYPE HASHED TABLE OF ty_dat WITH UNIQUE KEY name,
       tt_roles TYPE SORTED TABLE OF agr_tcodes-agr_name WITH UNIQUE KEY table_line.

DATA: gt_obj      TYPE HASHED TABLE OF ty_out WITH UNIQUE KEY object obj_name,
      gt_out      TYPE tt_out,
      gt_trdir    TYPE HASHED TABLE OF ty_trdir WITH UNIQUE KEY name,
      gt_clsdat   TYPE tt_dat,
      gt_fgdat    TYPE tt_dat,
      gt_ddic     TYPE HASHED TABLE OF ty_ddic WITH UNIQUE KEY object name,
      gt_tc_code  TYPE HASHED TABLE OF ty_tc WITH UNIQUE KEY tcode,
      gt_tc_pgm   TYPE SORTED TABLE OF ty_tc WITH NON-UNIQUE KEY pgmna,
      gt_fm       TYPE HASHED TABLE OF ty_fm WITH UNIQUE KEY funcname,
      gt_use      TYPE SORTED TABLE OF ty_use WITH UNIQUE KEY object obj_name user,
      gt_st03     TYPE HASHED TABLE OF ty_st03 WITH UNIQUE KEY typ name,
      gt_job      TYPE HASHED TABLE OF ty_job WITH UNIQUE KEY name,
      gt_agr      TYPE SORTED TABLE OF ty_agr WITH UNIQUE KEY tcode agr_name,
      gr_sapl     TYPE RANGE OF progname,
      gr_linc     TYPE RANGE OF progname,
      gv_cutoff   TYPE d,
      gv_st03_da  TYPE abap_bool,
      gv_code_da  TYPE abap_bool,
      gv_st03_von TYPE d,
      gv_st03_bis TYPE d,
      gv_zeitraum TYPE char80,
      gt_scmon    TYPE HASHED TABLE OF ty_nutz WITH UNIQUE KEY object name,
      gt_susg     TYPE HASHED TABLE OF ty_nutz WITH UNIQUE KEY object name,
      gv_scm_t    TYPE char30,
      gv_sus_t    TYPE char30.

INITIALIZATION.
  s_name[] = VALUE #( sign = 'I' option = 'CP' ( low = 'Z*' ) ( low = 'Y*' ) ).
  s_type[] = VALUE #( sign = 'I' option = 'EQ'
                      ( low = 'PROG' ) ( low = 'CLAS' ) ( low = 'INTF' ) ( low = 'FUGR' )
                      ( low = 'TRAN' ) ( low = 'TABL' ) ( low = 'VIEW' ) ( low = 'DTEL' )
                      ( low = 'DOMA' ) ( low = 'TTYP' ) ( low = 'SICF' ) ( low = 'IWSV' )
                      ( low = 'IWSG' ) ).

AT SELECTION-SCREEN.
  IF p_mon < 1 OR p_mon > 120.
    MESSAGE 'Nutzungszeitraum 1 bis 120 Monate' TYPE 'E'.
  ENDIF.

START-OF-SELECTION.
  PERFORM zeitraum_bestimmen.
  PERFORM objekte_lesen.
  IF gt_obj IS INITIAL.
    MESSAGE 'Keine Objekte zur Selektion gefunden' TYPE 'S'.
    RETURN.
  ENDIF.
  PERFORM programme_lesen.
  PERFORM ddic_lesen.
  PERFORM tcodes_lesen.
  PERFORM rollen_lesen.
  PERFORM jobs_lesen.
  PERFORM verwender_lesen.
  PERFORM st03n_lesen.
  PERFORM scmon_susg_lesen.
  PERFORM zusammenfuehren.
  PERFORM ausgeben.

*&---------------------------------------------------------------------*
*& Erster Tag des Zeitraums: aktueller Monat zaehlt mit (p_mon Monate)
*&---------------------------------------------------------------------*
FORM zeitraum_bestimmen.
  DATA: lv_y   TYPE i,
        lv_m   TYPE i,
        lv_tot TYPE i.
  lv_y = sy-datum(4).
  lv_m = sy-datum+4(2).
  lv_tot = lv_y * 12 + lv_m - 1 - ( p_mon - 1 ).
  lv_y = lv_tot DIV 12.
  lv_m = lv_tot MOD 12 + 1.
  gv_cutoff = |{ lv_y WIDTH = 4 ALIGN = RIGHT PAD = '0' }{ lv_m WIDTH = 2 ALIGN = RIGHT PAD = '0' }01|.
ENDFORM.

*&---------------------------------------------------------------------*
*& Objektliste aus TADIR, dazu Namensbereiche fuer Funktionsgruppen
*&---------------------------------------------------------------------*
FORM objekte_lesen.
  SELECT object, obj_name, devclass, author, created_on
    FROM tadir
    WHERE pgmid    = 'R3TR'
      AND object   IN @s_type
      AND obj_name IN @s_name
      AND devclass IN @s_pack
      AND delflag  = @space
    INTO CORRESPONDING FIELDS OF TABLE @gt_obj.

  LOOP AT s_name INTO DATA(ls_n).
    IF ls_n-option = 'EQ'.
      APPEND VALUE #( sign = ls_n-sign option = 'EQ' low = |SAPL{ ls_n-low }| ) TO gr_sapl.
      APPEND VALUE #( sign = ls_n-sign option = 'CP' low = |L{ ls_n-low }+++| ) TO gr_linc.
    ELSEIF ls_n-option = 'BT' OR ls_n-option = 'NB'.
      APPEND VALUE #( sign = ls_n-sign option = ls_n-option
                      low = |SAPL{ ls_n-low }| high = |SAPL{ ls_n-high }| ) TO gr_sapl.
      APPEND VALUE #( sign = ls_n-sign option = ls_n-option
                      low = |L{ ls_n-low }| high = |L{ ls_n-high }| ) TO gr_linc.
    ELSE.
*     CP/NP/NE: HIGH muss leer bleiben
      APPEND VALUE #( sign = ls_n-sign option = ls_n-option low = |SAPL{ ls_n-low }| ) TO gr_sapl.
      APPEND VALUE #( sign = ls_n-sign option = ls_n-option low = |L{ ls_n-low }| ) TO gr_linc.
    ENDIF.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*& TRDIR: Programmart und letzte Aenderung fuer PROG, Klassen-Includes
*& (Name bis Stelle 30 mit '=' aufgefuellt) und Funktionsgruppen
*& (SAPL<grp>, L<grp>xxx)
*&---------------------------------------------------------------------*
FORM programme_lesen.
  DATA: lv_cls TYPE progname,
        lv_grp TYPE progname,
        lv_len TYPE i.

  SELECT name, subc, udat
    FROM trdir
    WHERE name IN @s_name
       OR name IN @gr_sapl
       OR name IN @gr_linc
    INTO TABLE @DATA(lt_trdir).

  LOOP AT lt_trdir INTO DATA(ls_t).
    INSERT VALUE #( name = ls_t-name subc = ls_t-subc udat = ls_t-udat ) INTO TABLE gt_trdir.
    lv_len = strlen( ls_t-name ).
    IF lv_len > 30.
      lv_cls = replace( val = ls_t-name(30) sub = '=' with = `` occ = 0 ).
      PERFORM datum_max USING lv_cls ls_t-udat CHANGING gt_clsdat.
    ENDIF.
    IF ls_t-name CP 'SAPL*'.
      lv_grp = ls_t-name+4.
      PERFORM datum_max USING lv_grp ls_t-udat CHANGING gt_fgdat.
    ELSEIF ls_t-name(1) = 'L' AND lv_len > 4.
      lv_grp = substring( val = ls_t-name off = 1 len = lv_len - 4 ).
      PERFORM datum_max USING lv_grp ls_t-udat CHANGING gt_fgdat.
    ENDIF.
  ENDLOOP.
ENDFORM.

FORM datum_max USING iv_name TYPE progname iv_udat TYPE d CHANGING ct_dat TYPE tt_dat.
  FIELD-SYMBOLS <ls_d> TYPE ty_dat.
  READ TABLE ct_dat ASSIGNING <ls_d> WITH TABLE KEY name = iv_name.
  IF sy-subrc <> 0.
    INSERT VALUE #( name = iv_name udat = iv_udat ) INTO TABLE ct_dat.
  ELSEIF iv_udat > <ls_d>-udat.
    <ls_d>-udat = iv_udat.
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
*& DDIC-Koepfe: AS4DATE der aktiven Fassung
*&---------------------------------------------------------------------*
FORM ddic_lesen.
  SELECT tabname, as4date FROM dd02l
    WHERE tabname IN @s_name AND as4local = 'A' INTO TABLE @DATA(lt_02).
  LOOP AT lt_02 INTO DATA(ls_02).
    INSERT VALUE #( object = 'TABL' name = ls_02-tabname udat = ls_02-as4date ) INTO TABLE gt_ddic.
  ENDLOOP.
  SELECT viewname, as4date FROM dd25l
    WHERE viewname IN @s_name AND as4local = 'A' INTO TABLE @DATA(lt_25).
  LOOP AT lt_25 INTO DATA(ls_25).
    INSERT VALUE #( object = 'VIEW' name = ls_25-viewname udat = ls_25-as4date ) INTO TABLE gt_ddic.
  ENDLOOP.
  SELECT rollname, as4date FROM dd04l
    WHERE rollname IN @s_name AND as4local = 'A' INTO TABLE @DATA(lt_04).
  LOOP AT lt_04 INTO DATA(ls_04).
    INSERT VALUE #( object = 'DTEL' name = ls_04-rollname udat = ls_04-as4date ) INTO TABLE gt_ddic.
  ENDLOOP.
  SELECT domname, as4date FROM dd01l
    WHERE domname IN @s_name AND as4local = 'A' INTO TABLE @DATA(lt_01).
  LOOP AT lt_01 INTO DATA(ls_01).
    INSERT VALUE #( object = 'DOMA' name = ls_01-domname udat = ls_01-as4date ) INTO TABLE gt_ddic.
  ENDLOOP.
  SELECT typename, as4date FROM dd40l
    WHERE typename IN @s_name AND as4local = 'A' INTO TABLE @DATA(lt_40).
  LOOP AT lt_40 INTO DATA(ls_40).
    INSERT VALUE #( object = 'TTYP' name = ls_40-typename udat = ls_40-as4date ) INTO TABLE gt_ddic.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*& Transaktionen (TSTC): eigene Codes, Codes auf Z-Programme und auf
*& Funktionsgruppen (SAPL<grp>)
*&---------------------------------------------------------------------*
FORM tcodes_lesen.
  SELECT tcode, pgmna FROM tstc
    WHERE tcode IN @s_name
       OR pgmna IN @s_name
       OR pgmna IN @gr_sapl
    INTO TABLE @DATA(lt_tc).
  LOOP AT lt_tc INTO DATA(ls_tc).
    INSERT VALUE #( tcode = ls_tc-tcode pgmna = ls_tc-pgmna ) INTO TABLE gt_tc_code.
    INSERT VALUE #( tcode = ls_tc-tcode pgmna = ls_tc-pgmna ) INTO TABLE gt_tc_pgm.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*& Rollen mit diesen Transaktionen (AGR_TCODES, Typ TR, Anmeldemandant)
*&---------------------------------------------------------------------*
FORM rollen_lesen.
  TYPES: BEGIN OF ty_atc,
           tcode TYPE agr_tcodes-tcode,
         END OF ty_atc.
  DATA lt_tc TYPE STANDARD TABLE OF ty_atc WITH DEFAULT KEY.
  LOOP AT gt_tc_code INTO DATA(ls_tc).
    APPEND VALUE #( tcode = ls_tc-tcode ) TO lt_tc.
  ENDLOOP.
  IF lt_tc IS INITIAL.
    RETURN.
  ENDIF.
  SELECT tcode, agr_name FROM agr_tcodes
    FOR ALL ENTRIES IN @lt_tc
    WHERE tcode = @lt_tc-tcode
      AND type  = 'TR'
    INTO TABLE @DATA(lt_agr).
  LOOP AT lt_agr INTO DATA(ls_agr).
    INSERT VALUE #( tcode = ls_agr-tcode agr_name = ls_agr-agr_name ) INTO TABLE gt_agr.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*& Hintergrundjobs (TBTCP/TBTCO). Status laut LBTCHDEF:
*& S freigegeben, P eingeplant, Z aktiv gesetzt, Y bereit, R laeuft,
*& F beendet, A abgebrochen
*&---------------------------------------------------------------------*
FORM jobs_lesen.
  FIELD-SYMBOLS <ls_j> TYPE ty_job.
  SELECT p~progname, p~jobname, p~jobcount, o~status, o~enddate
    FROM tbtcp AS p
    INNER JOIN tbtco AS o ON o~jobname  = p~jobname
                         AND o~jobcount = p~jobcount
    WHERE p~progname IN @s_name
    INTO TABLE @DATA(lt_job).
  SORT lt_job BY progname jobname jobcount.
  DELETE ADJACENT DUPLICATES FROM lt_job COMPARING progname jobname jobcount.
  LOOP AT lt_job INTO DATA(ls_job).
    READ TABLE gt_job ASSIGNING <ls_j> WITH TABLE KEY name = ls_job-progname.
    IF sy-subrc <> 0.
      INSERT VALUE #( name = ls_job-progname ) INTO TABLE gt_job ASSIGNING <ls_j>.
    ENDIF.
    IF ls_job-status CA 'SPZYR'.
      <ls_j>-plan = <ls_j>-plan + 1.
    ENDIF.
    IF ls_job-status CA 'FA' AND ls_job-enddate > <ls_j>-last.
      <ls_j>-last = ls_job-enddate.
    ENDIF.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*& Verwendungsnachweis: CROSS (R/T/F), WBCROSSGT, D010INC, DD03L,
*& DD04L, DD26S. Eigenverweise werden nicht gezaehlt.
*&---------------------------------------------------------------------*
FORM verwender_lesen.
  DATA: lt_key  TYPE STANDARD TABLE OF ty_key WITH DEFAULT KEY,
        lt_fg   TYPE STANDARD TABLE OF progname WITH DEFAULT KEY,
        lt_inc  TYPE STANDARD TABLE OF progname WITH DEFAULT KEY,
        lv_base TYPE tadir-obj_name,
        lv_pos  TYPE i,
        lv_name TYPE tadir-obj_name,
        lv_prog TYPE progname.

  LOOP AT gt_obj INTO DATA(ls_o).
    CASE ls_o-object.
      WHEN 'PROG'.
        IF strlen( ls_o-obj_name ) <= 30.
          APPEND VALUE #( typ = 'R' name = ls_o-obj_name ) TO lt_key.
        ENDIF.
        READ TABLE gt_trdir INTO DATA(ls_tr) WITH TABLE KEY name = ls_o-obj_name.
        IF sy-subrc = 0 AND ls_tr-subc = 'I'.
          APPEND ls_o-obj_name TO lt_inc.
        ENDIF.
      WHEN 'FUGR'.
        IF ls_o-obj_name(1) <> '/'.
          APPEND |SAPL{ ls_o-obj_name }| TO lt_fg.
        ENDIF.
    ENDCASE.
  ENDLOOP.
  LOOP AT gt_tc_code INTO DATA(ls_tc).
    APPEND VALUE #( typ = 'T' name = ls_tc-tcode ) TO lt_key.
  ENDLOOP.

* Funktionsbausteine der Gruppen
  IF lt_fg IS NOT INITIAL.
    SELECT funcname, pname FROM tfdir
      FOR ALL ENTRIES IN @lt_fg
      WHERE pname = @lt_fg-table_line
      INTO TABLE @DATA(lt_tfdir).
    LOOP AT lt_tfdir INTO DATA(ls_tf).
      INSERT VALUE #( funcname = ls_tf-funcname grp = ls_tf-pname+4 ) INTO TABLE gt_fm.
      APPEND VALUE #( typ = 'F' name = ls_tf-funcname ) TO lt_key.
    ENDLOOP.
  ENDIF.

* CROSS: SUBMIT (R), CALL TRANSACTION (T), CALL FUNCTION (F)
  IF lt_key IS NOT INITIAL.
    SELECT type, name, include FROM cross
      FOR ALL ENTRIES IN @lt_key
      WHERE type = @lt_key-typ
        AND name = @lt_key-name
      INTO TABLE @DATA(lt_cross).
    LOOP AT lt_cross INTO DATA(ls_c).
      CASE ls_c-type.
        WHEN 'R'.
          lv_name = ls_c-name.
          PERFORM use_add USING 'PROG' lv_name ls_c-include.
        WHEN 'T'.
          lv_name = ls_c-name.
          PERFORM use_add USING 'TRAN' lv_name ls_c-include.
          READ TABLE gt_tc_code INTO DATA(ls_tc2) WITH TABLE KEY tcode = ls_c-name.
          IF sy-subrc = 0.
            IF ls_tc2-pgmna CP 'SAPL*'.
              lv_name = ls_tc2-pgmna+4.
              PERFORM use_add USING 'FUGR' lv_name ls_c-include.
            ELSE.
              lv_name = ls_tc2-pgmna.
              PERFORM use_add USING 'PROG' lv_name ls_c-include.
            ENDIF.
          ENDIF.
        WHEN 'F'.
          READ TABLE gt_fm INTO DATA(ls_fm) WITH TABLE KEY funcname = ls_c-name.
          IF sy-subrc = 0.
            PERFORM use_add USING 'FUGR' ls_fm-grp ls_c-include.
          ENDIF.
      ENDCASE.
    ENDLOOP.
    FREE lt_cross.
  ENDIF.

* WBCROSSGT: globale Typen (Klassen, Interfaces, DDIC-Typen)
  SELECT DISTINCT name, include FROM wbcrossgt
    WHERE name IN @s_name
    INTO TABLE @DATA(lt_wb).
  LOOP AT lt_wb INTO DATA(ls_wb).
    FIND FIRST OCCURRENCE OF '\' IN ls_wb-name MATCH OFFSET lv_pos.
    IF sy-subrc = 0.
      lv_base = ls_wb-name(lv_pos).
    ELSE.
      lv_base = ls_wb-name.
    ENDIF.
    PERFORM use_add_typen USING lv_base ls_wb-include.
  ENDLOOP.
  FREE lt_wb.

* D010INC: Rahmenprogramme der Includes
  IF lt_inc IS NOT INITIAL.
    SELECT master, include FROM d010inc
      FOR ALL ENTRIES IN @lt_inc
      WHERE include = @lt_inc-table_line
      INTO TABLE @DATA(lt_d010).
    LOOP AT lt_d010 INTO DATA(ls_d).
      lv_name = ls_d-include.
      PERFORM use_add USING 'PROG' lv_name ls_d-master.
    ENDLOOP.
  ENDIF.

* TNAPR: Verarbeitungsprogramme der Nachrichtensteuerung (Aufruf per
* PERFORM ... IN PROGRAM aus dem Standard, weder in ST03N noch in CROSS)
  SELECT kappl, kschl, nacha, pgnam, pgnam2, pgnam3, pgnam4, pgnam5 FROM tnapr
    WHERE pgnam  IN @s_name OR pgnam2 IN @s_name OR pgnam3 IN @s_name
       OR pgnam4 IN @s_name OR pgnam5 IN @s_name
    INTO TABLE @DATA(lt_napr).
  LOOP AT lt_napr INTO DATA(ls_na).
    lv_prog = |TNAPR { ls_na-kappl } { ls_na-kschl } { ls_na-nacha }|.
    PERFORM use_add USING 'PROG' ls_na-pgnam  lv_prog.
    PERFORM use_add USING 'PROG' ls_na-pgnam2 lv_prog.
    PERFORM use_add USING 'PROG' ls_na-pgnam3 lv_prog.
    PERFORM use_add USING 'PROG' ls_na-pgnam4 lv_prog.
    PERFORM use_add USING 'PROG' ls_na-pgnam5 lv_prog.
  ENDLOOP.

* DD03L: Feldtyp (ROLLNAME) und Include-Strukturen (PRECFIELD)
  SELECT DISTINCT tabname, rollname FROM dd03l
    WHERE rollname IN @s_name AND as4local = 'A'
    INTO TABLE @DATA(lt_roll).
  LOOP AT lt_roll INTO DATA(ls_r).
    lv_base = ls_r-rollname.
    lv_prog = ls_r-tabname.
    PERFORM use_add_typen USING lv_base lv_prog.
  ENDLOOP.
  FREE lt_roll.
  SELECT DISTINCT tabname, precfield FROM dd03l
    WHERE precfield IN @s_name AND as4local = 'A'
    INTO TABLE @DATA(lt_prec).
  LOOP AT lt_prec INTO DATA(ls_p).
    lv_name = ls_p-precfield.
    lv_prog = ls_p-tabname.
    PERFORM use_add USING 'TABL' lv_name lv_prog.
  ENDLOOP.
  FREE lt_prec.

* DD04L: Datenelemente zu Domaenen
  SELECT DISTINCT rollname, domname FROM dd04l
    WHERE domname IN @s_name AND as4local = 'A'
    INTO TABLE @DATA(lt_dom).
  LOOP AT lt_dom INTO DATA(ls_dm).
    lv_name = ls_dm-domname.
    lv_prog = ls_dm-rollname.
    PERFORM use_add USING 'DOMA' lv_name lv_prog.
  ENDLOOP.

* DD26S: Views auf Tabellen
  SELECT DISTINCT viewname, tabname FROM dd26s
    WHERE tabname IN @s_name AND as4local = 'A'
    INTO TABLE @DATA(lt_vw).
  LOOP AT lt_vw INTO DATA(ls_vw).
    lv_name = ls_vw-tabname.
    lv_prog = ls_vw-viewname.
    PERFORM use_add USING 'TABL' lv_name lv_prog.
  ENDLOOP.
ENDFORM.

* Name aus WBCROSSGT/DD03L kann Klasse, Interface oder DDIC-Typ sein
FORM use_add_typen USING iv_name TYPE tadir-obj_name iv_user TYPE csequence.
  DATA lt_typ TYPE STANDARD TABLE OF tadir-object WITH DEFAULT KEY.
  lt_typ = VALUE #( ( 'CLAS' ) ( 'INTF' ) ( 'TABL' ) ( 'VIEW' ) ( 'DTEL' ) ( 'TTYP' ) ).
  LOOP AT lt_typ INTO DATA(lv_typ).
    IF line_exists( gt_obj[ object = lv_typ obj_name = iv_name ] ).
      PERFORM use_add USING lv_typ iv_name iv_user.
    ENDIF.
  ENDLOOP.
ENDFORM.

FORM use_add USING iv_object TYPE csequence iv_name TYPE csequence iv_user TYPE csequence.
  DATA: lv_obj   TYPE tadir-object,
        lv_name  TYPE tadir-obj_name,
        lv_eigen TYPE abap_bool,
        lv_kunde TYPE abap_bool.
  lv_obj  = iv_object.
  lv_name = iv_name.
  IF iv_user IS INITIAL OR NOT line_exists( gt_obj[ object = lv_obj obj_name = lv_name ] ).
    RETURN.
  ENDIF.
  PERFORM ist_eigen USING lv_obj lv_name iv_user CHANGING lv_eigen.
  IF lv_eigen = abap_true.
    RETURN.
  ENDIF.
  PERFORM ist_kunde USING iv_user CHANGING lv_kunde.
  INSERT VALUE #( object = lv_obj obj_name = lv_name user = iv_user kunde = lv_kunde ) INTO TABLE gt_use.
ENDFORM.

* Verwender gehoert zum Objekt selbst (Klassen-Include, FG-Include ...)
FORM ist_eigen USING iv_object TYPE csequence iv_name TYPE csequence iv_user TYPE csequence
               CHANGING cv_eigen TYPE abap_bool.
  DATA: lv_user TYPE string,
        lv_name TYPE string,
        lv_rest TYPE string,
        lv_prog TYPE string.
  cv_eigen = abap_false.
  lv_user = iv_user.
  lv_name = iv_name.
  IF lv_user = lv_name.
    cv_eigen = abap_true.
    RETURN.
  ENDIF.
  CASE iv_object.
    WHEN 'CLAS' OR 'INTF'.
      IF strlen( lv_user ) > 30.
        lv_rest = replace( val = substring( val = lv_user len = 30 ) sub = '=' with = `` occ = 0 ).
        IF lv_rest = lv_name.
          cv_eigen = abap_true.
        ENDIF.
      ENDIF.
    WHEN 'FUGR'.
      IF lv_user = |SAPL{ lv_name }|.
        cv_eigen = abap_true.
      ELSEIF strlen( lv_user ) = strlen( lv_name ) + 4
         AND substring( val = lv_user len = 1 ) = 'L'
         AND substring( val = lv_user off = 1 len = strlen( lv_name ) ) = lv_name.
        cv_eigen = abap_true.
      ENDIF.
    WHEN 'PROG'.
      IF lv_name CP 'SAPM*' AND strlen( lv_name ) > 4.
        lv_rest = |M{ substring( val = lv_name off = 4 ) }|.
        IF strlen( lv_user ) >= strlen( lv_rest )
           AND substring( val = lv_user len = strlen( lv_rest ) ) = lv_rest.
          cv_eigen = abap_true.
        ENDIF.
      ENDIF.
    WHEN 'TRAN'.
      READ TABLE gt_tc_code INTO DATA(ls_tc) WITH TABLE KEY tcode = iv_name.
      IF sy-subrc = 0 AND ls_tc-pgmna IS NOT INITIAL.
        lv_prog = ls_tc-pgmna.
        IF lv_prog CP 'SAPL*'.
          lv_rest = substring( val = lv_prog off = 4 ).
          PERFORM ist_eigen USING 'FUGR' lv_rest iv_user CHANGING cv_eigen.
        ELSE.
          PERFORM ist_eigen USING 'PROG' lv_prog iv_user CHANGING cv_eigen.
        ENDIF.
      ENDIF.
  ENDCASE.
ENDFORM.

* Kundennamensraum: Z*, Y*, LZ*, LY*, SAPLZ*, SAPLY*, SAPMZ*, SAPMY*, MZ*, MY*
FORM ist_kunde USING iv_name TYPE csequence CHANGING cv_kunde TYPE abap_bool.
  DATA lv TYPE string.
  lv = iv_name.
  IF lv CP 'Z*' OR lv CP 'Y*' OR lv CP 'LZ*' OR lv CP 'LY*'
     OR lv CP 'SAPLZ*' OR lv CP 'SAPLY*' OR lv CP 'SAPMZ*' OR lv CP 'SAPMY*'
     OR lv CP 'MZ*' OR lv CP 'MY*'.
    cv_kunde = abap_true.
  ELSE.
    cv_kunde = abap_false.
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
*& ST03N: Monatsaggregate (COMPONENT TOTAL) aus SWNCMONIINDEX,
*& Baustein SWNC_COLLECTOR_GET_AGGREGATES, Tabelle USERTCODE.
*& ENTRY_ID: Stelle 1-40 Programm/Transaktion, 41-72 Jobname,
*& 73 Typ (T Transaktion, R Report)
*&---------------------------------------------------------------------*
FORM st03n_lesen.
  DATA: lt_ut  TYPE STANDARD TABLE OF swncaggusertcode,
        lt_per TYPE STANDARD TABLE OF d WITH DEFAULT KEY,
        ls_key TYPE ty_st03.
  FIELD-SYMBOLS <ls_s> TYPE ty_st03.

  SELECT DISTINCT periodstrt FROM swncmoniindex
    WHERE component  = 'TOTAL'
      AND periodtype = 'M'
      AND assigndsys = @sy-sysid
      AND periodstrt >= @gv_cutoff
    ORDER BY periodstrt
    INTO TABLE @lt_per.

  LOOP AT lt_per INTO DATA(lv_per).
    CLEAR lt_ut.
    CALL FUNCTION 'SWNC_COLLECTOR_GET_AGGREGATES'
      EXPORTING
        component     = 'TOTAL'
        periodtype    = 'M'
        periodstrt    = lv_per
      TABLES
        usertcode     = lt_ut
      EXCEPTIONS
        no_data_found = 1
        OTHERS        = 2.
    IF sy-subrc <> 0.
      CONTINUE.
    ENDIF.
    gv_st03_da = abap_true.
    IF gv_st03_von IS INITIAL OR lv_per < gv_st03_von.
      gv_st03_von = lv_per.
    ENDIF.
    IF lv_per > gv_st03_bis.
      gv_st03_bis = lv_per.
    ENDIF.
    LOOP AT lt_ut INTO DATA(ls_ut).
      CLEAR ls_key.
      ls_key-typ  = ls_ut-entry_id+72(1).
      ls_key-name = ls_ut-entry_id(40).
      IF ls_key-name NOT IN s_name OR ( ls_key-typ <> 'T' AND ls_key-typ <> 'R' ).
        IF ls_key-typ <> 'T'.
          CONTINUE.
        ENDIF.
        IF NOT line_exists( gt_tc_code[ tcode = ls_key-name ] ).
          CONTINUE.
        ENDIF.
      ENDIF.
      READ TABLE gt_st03 ASSIGNING <ls_s> WITH TABLE KEY typ = ls_key-typ name = ls_key-name.
      IF sy-subrc <> 0.
        INSERT ls_key INTO TABLE gt_st03 ASSIGNING <ls_s>.
      ENDIF.
      <ls_s>-cnt = <ls_s>-cnt + ls_ut-count.
      IF lv_per > <ls_s>-last.
        <ls_s>-last = lv_per.
      ENDIF.
    ENDLOOP.
    FREE lt_ut.
  ENDLOOP.
ENDFORM.

*&---------------------------------------------------------------------*
*& SCMON / SUSG (in T76 ohne Daten, daher ungetestet)
*& Kette: DATA-TRIGID -> PROC-PROGID -> PROG-OBJECT/OBJ_NAME
*&---------------------------------------------------------------------*
FORM scmon_susg_lesen.
  DATA lv_n TYPE i.

  SELECT COUNT(*) FROM scmon_slice WHERE enddate >= @gv_cutoff INTO @lv_n.
  IF lv_n > 0.
    gv_code_da = abap_true.
    SELECT MIN( startdate ) AS von, MAX( enddate ) AS bis FROM scmon_slice
      WHERE enddate >= @gv_cutoff INTO @DATA(ls_sl).
    gv_scm_t = |SCMON { ls_sl-von DATE = ISO }-{ ls_sl-bis DATE = ISO }|.
    SELECT g~object, g~obj_name, SUM( d~counter ) AS cnt, MAX( s~enddate ) AS bis
      FROM scmon_prog AS g
      INNER JOIN scmon_proc  AS c ON c~progid  = g~progid
      INNER JOIN scmon_data  AS d ON d~trigid  = c~trigid
      INNER JOIN scmon_slice AS s ON s~sliceid = d~sliceid
      WHERE g~obj_name IN @s_name
        AND s~enddate >= @gv_cutoff
      GROUP BY g~object, g~obj_name
      INTO TABLE @DATA(lt_scm).
    LOOP AT lt_scm INTO DATA(ls_scm).
      INSERT VALUE #( object = ls_scm-object name = ls_scm-obj_name cnt = ls_scm-cnt last = ls_scm-bis )
        INTO TABLE gt_scmon.
    ENDLOOP.
  ELSE.
    gv_scm_t = 'SCMON keine Daten'.
  ENDIF.

  CLEAR lv_n.
  SELECT COUNT(*) FROM susg_admin WHERE sysid = @sy-sysid INTO @lv_n.
  IF lv_n > 0.
    gv_code_da = abap_true.
    SELECT MIN( date_from ) AS von, MAX( date_to ) AS bis FROM susg_admin
      WHERE sysid = @sy-sysid INTO @DATA(ls_sa).
    gv_sus_t = |SUSG { ls_sa-von DATE = ISO }-{ ls_sa-bis DATE = ISO }|.
    SELECT g~obj_type, g~obj_name, SUM( d~counter ) AS cnt, MAX( d~last_used ) AS bis
      FROM susg_prog AS g
      INNER JOIN susg_proc  AS c ON c~usgid = g~usgid AND c~progid = g~progid
      INNER JOIN susg_data  AS d ON d~usgid = c~usgid AND d~trigid = c~trigid
      INNER JOIN susg_admin AS a ON a~usgid = g~usgid
      WHERE g~obj_name IN @s_name
        AND a~sysid = @sy-sysid
        AND d~last_used >= @gv_cutoff
      GROUP BY g~obj_type, g~obj_name
      INTO TABLE @DATA(lt_sus).
    LOOP AT lt_sus INTO DATA(ls_sus).
      INSERT VALUE #( object = ls_sus-obj_type name = ls_sus-obj_name cnt = ls_sus-cnt last = ls_sus-bis )
        INTO TABLE gt_susg.
    ENDLOOP.
  ELSE.
    gv_sus_t = 'SUSG keine Daten'.
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
*& Spalten fuellen und einstufen
*&---------------------------------------------------------------------*
FORM zusammenfuehren.
  DATA: lt_roles   TYPE tt_roles,
        lv_pgm     TYPE tstc-pgmna,
        lv_tclist  TYPE string,
        lv_st03_t  TYPE string,
        lv_grund   TYPE string,
        lv_messbar TYPE abap_bool,
        lv_genutzt TYPE abap_bool,
        lv_indir   TYPE abap_bool,
        lv_key     TYPE char40.
  FIELD-SYMBOLS <ls_o> TYPE ty_out.

  IF gv_st03_da = abap_true.
    lv_st03_t = |ST03N { gv_st03_von(4) }/{ gv_st03_von+4(2) }-{ gv_st03_bis(4) }/{ gv_st03_bis+4(2) }|.
  ELSE.
    lv_st03_t = 'ST03N keine Monate'.
  ENDIF.
  gv_zeitraum = |ab { gv_cutoff DATE = ISO }; { lv_st03_t }; { gv_scm_t }; { gv_sus_t }|.

  LOOP AT gt_obj ASSIGNING <ls_o>.
    CLEAR: lt_roles, lv_tclist, lv_grund, lv_pgm.
    <ls_o>-zeitraum = gv_zeitraum.

*   Programmart und letzte Aenderung
    CASE <ls_o>-object.
      WHEN 'PROG'.
        READ TABLE gt_trdir INTO DATA(ls_tr) WITH TABLE KEY name = <ls_o>-obj_name.
        IF sy-subrc = 0.
          <ls_o>-subc     = ls_tr-subc.
          <ls_o>-chg_date = ls_tr-udat.
          <ls_o>-chg_src  = 'TRDIR'.
        ENDIF.
        lv_pgm = <ls_o>-obj_name.
      WHEN 'CLAS' OR 'INTF'.
        READ TABLE gt_clsdat INTO DATA(ls_cd) WITH TABLE KEY name = <ls_o>-obj_name.
        IF sy-subrc = 0.
          <ls_o>-chg_date = ls_cd-udat.
          <ls_o>-chg_src  = 'TRDIR-Incl'.
        ENDIF.
      WHEN 'FUGR'.
        READ TABLE gt_fgdat INTO DATA(ls_fd) WITH TABLE KEY name = <ls_o>-obj_name.
        IF sy-subrc = 0.
          <ls_o>-chg_date = ls_fd-udat.
          <ls_o>-chg_src  = 'TRDIR-FG'.
        ENDIF.
        lv_pgm = |SAPL{ <ls_o>-obj_name }|.
      WHEN 'TABL' OR 'VIEW' OR 'DTEL' OR 'DOMA' OR 'TTYP'.
        READ TABLE gt_ddic INTO DATA(ls_dd) WITH TABLE KEY object = <ls_o>-object name = <ls_o>-obj_name.
        IF sy-subrc = 0.
          <ls_o>-chg_date = ls_dd-udat.
          <ls_o>-chg_src  = 'DDIC'.
        ENDIF.
    ENDCASE.

*   Transaktionen, Rollen, ST03N-Nutzung ueber Transaktionen
    IF <ls_o>-object = 'TRAN'.
      lv_tclist = <ls_o>-obj_name.
      PERFORM tcode_auswerten USING <ls_o>-obj_name CHANGING <ls_o> lt_roles.
    ELSEIF lv_pgm IS NOT INITIAL.
      LOOP AT gt_tc_pgm INTO DATA(ls_tp) WHERE pgmna = lv_pgm.
        IF lv_tclist IS INITIAL.
          lv_tclist = ls_tp-tcode.
        ELSE.
          lv_tclist = |{ lv_tclist }, { ls_tp-tcode }|.
        ENDIF.
        PERFORM tcode_auswerten USING ls_tp-tcode CHANGING <ls_o> lt_roles.
      ENDLOOP.
    ENDIF.
    <ls_o>-tcodes = lv_tclist.
    <ls_o>-roles  = lines( lt_roles ).

*   ST03N als Report
    IF <ls_o>-object = 'PROG'.
      lv_key = <ls_o>-obj_name.
      READ TABLE gt_st03 INTO DATA(ls_st) WITH TABLE KEY typ = 'R' name = lv_key.
      IF sy-subrc = 0.
        <ls_o>-st03_cnt = <ls_o>-st03_cnt + ls_st-cnt.
        PERFORM monat_merken USING ls_st-last CHANGING <ls_o>-st03_month.
      ENDIF.
    ENDIF.

*   Jobs
    READ TABLE gt_job INTO DATA(ls_job) WITH TABLE KEY name = lv_pgm.
    IF sy-subrc = 0 AND <ls_o>-object = 'PROG'.
      <ls_o>-jobs_plan = ls_job-plan.
      <ls_o>-job_last  = ls_job-last.
    ENDIF.

*   SCMON / SUSG
    READ TABLE gt_scmon INTO DATA(ls_sc) WITH TABLE KEY object = <ls_o>-object name = <ls_o>-obj_name.
    IF sy-subrc = 0.
      <ls_o>-scmon_last = ls_sc-last.
      <ls_o>-scmon_cnt  = ls_sc-cnt.
    ENDIF.
    READ TABLE gt_susg INTO DATA(ls_su) WITH TABLE KEY object = <ls_o>-object name = <ls_o>-obj_name.
    IF sy-subrc = 0.
      <ls_o>-susg_last = ls_su-last.
      <ls_o>-susg_cnt  = ls_su-cnt.
    ENDIF.

*   Verwender
    LOOP AT gt_use INTO DATA(ls_u) WHERE object = <ls_o>-object AND obj_name = <ls_o>-obj_name.
      IF ls_u-kunde = abap_true.
        <ls_o>-use_z = <ls_o>-use_z + 1.
      ELSE.
        <ls_o>-use_other = <ls_o>-use_other + 1.
      ENDIF.
    ENDLOOP.

*   Einstufung
    lv_genutzt = xsdbool( <ls_o>-st03_cnt > 0 OR <ls_o>-scmon_cnt > 0 OR <ls_o>-susg_cnt > 0
                          OR <ls_o>-job_last >= gv_cutoff ).
    lv_indir = xsdbool( <ls_o>-use_z > 0 OR <ls_o>-use_other > 0 OR <ls_o>-jobs_plan > 0
                        OR <ls_o>-roles > 0 ).
    lv_messbar = abap_false.
    IF gv_code_da = abap_true AND ( <ls_o>-object = 'PROG' OR <ls_o>-object = 'CLAS'
                                    OR <ls_o>-object = 'FUGR' ).
      lv_messbar = abap_true.
    ENDIF.
    IF gv_st03_da = abap_true AND ( <ls_o>-object = 'TRAN'
       OR ( <ls_o>-object = 'PROG' AND ( <ls_o>-subc = '1' OR <ls_o>-subc = 'M' ) ) ).
      lv_messbar = abap_true.
    ENDIF.
    IF <ls_o>-object = 'PROG' AND <ls_o>-subc = 'I'.
      lv_messbar = abap_true.
    ENDIF.

    IF lv_genutzt = abap_true.
      <ls_o>-klasse = 'genutzt'.
      IF <ls_o>-st03_cnt > 0.
        lv_grund = |ST03N { <ls_o>-st03_cnt } Ausf.|.
      ENDIF.
      IF <ls_o>-scmon_cnt > 0.
        lv_grund = |{ lv_grund } SCMON { <ls_o>-scmon_cnt }|.
      ENDIF.
      IF <ls_o>-susg_cnt > 0.
        lv_grund = |{ lv_grund } SUSG { <ls_o>-susg_cnt }|.
      ENDIF.
      IF <ls_o>-job_last >= gv_cutoff.
        lv_grund = |{ lv_grund } Joblauf { <ls_o>-job_last DATE = ISO }|.
      ENDIF.
    ELSEIF lv_indir = abap_true.
      <ls_o>-klasse = 'indirekt'.
      lv_grund = |Verwender Z { <ls_o>-use_z }, fremd { <ls_o>-use_other }, Jobs eingeplant |
              && |{ <ls_o>-jobs_plan }, Rollen { <ls_o>-roles }|.
    ELSEIF lv_messbar = abap_true.
      <ls_o>-klasse = 'tot'.
      IF <ls_o>-object = 'PROG' AND <ls_o>-subc = 'I'.
        lv_grund = 'Include ohne Rahmenprogramm (D010INC), keine Verwender'.
      ELSE.
        lv_grund = |Keine Nutzung, Verwender, Jobs, Rollen im Zeitraum ({ lv_st03_t })|.
      ENDIF.
    ELSE.
      <ls_o>-klasse = 'unklar'.
      IF gv_st03_da = abap_false AND gv_code_da = abap_false.
        lv_grund = 'Keine Nutzungsdaten (ST03N, SCMON, SUSG) im Zeitraum'.
      ELSE.
        lv_grund = |Nutzung fuer { <ls_o>-object } nicht messbar, keine Verwender/Jobs/Rollen|.
      ENDIF.
    ENDIF.
    <ls_o>-grund = condense( lv_grund ).
  ENDLOOP.

  LOOP AT gt_obj INTO DATA(ls_o).
    IF p_kand = abap_true AND ls_o-klasse <> 'tot' AND ls_o-klasse <> 'unklar'.
      CONTINUE.
    ENDIF.
    APPEND ls_o TO gt_out.
  ENDLOOP.
  SORT gt_out BY object obj_name.
ENDFORM.

* Eine Transaktion: Rollen sammeln, ST03N-Nutzung addieren
FORM tcode_auswerten USING iv_tcode TYPE csequence
                     CHANGING cs_o TYPE ty_out
                              ct_roles TYPE tt_roles.
  DATA: lv_tc  TYPE tstc-tcode,
        lv_key TYPE char40.
  lv_tc  = iv_tcode.
  lv_key = iv_tcode.
  LOOP AT gt_agr INTO DATA(ls_a) WHERE tcode = lv_tc.
    INSERT ls_a-agr_name INTO TABLE ct_roles.
  ENDLOOP.
  READ TABLE gt_st03 INTO DATA(ls_st) WITH TABLE KEY typ = 'T' name = lv_key.
  IF sy-subrc = 0.
    cs_o-st03_cnt = cs_o-st03_cnt + ls_st-cnt.
    PERFORM monat_merken USING ls_st-last CHANGING cs_o-st03_month.
  ENDIF.
ENDFORM.

FORM monat_merken USING iv_per TYPE d CHANGING cv_mon TYPE char7.
  DATA lv_mon TYPE char7.
  lv_mon = |{ iv_per(4) }/{ iv_per+4(2) }|.
  IF lv_mon > cv_mon.
    cv_mon = lv_mon.
  ENDIF.
ENDFORM.

*&---------------------------------------------------------------------*
*& ALV-Ausgabe (CL_SALV_TABLE, Standardfunktionen inkl. Excel-Export)
*&---------------------------------------------------------------------*
FORM ausgeben.
  DATA: lo_alv    TYPE REF TO cl_salv_table,
        lo_cols   TYPE REF TO cl_salv_columns_table,
        lv_header TYPE lvc_title.

  TRY.
      cl_salv_table=>factory( IMPORTING r_salv_table = lo_alv
                              CHANGING  t_table      = gt_out ).
    CATCH cx_salv_msg.
      MESSAGE 'ALV konnte nicht erzeugt werden' TYPE 'E'.
  ENDTRY.

  lo_alv->get_functions( )->set_all( abap_true ).
  lo_cols = lo_alv->get_columns( ).
  lo_cols->set_optimize( abap_true ).

  PERFORM spalte USING lo_cols 'OBJECT'     'Typ'        'Objekttyp'            'Objekttyp (TADIR)'.
  PERFORM spalte USING lo_cols 'OBJ_NAME'   'Name'       'Objektname'           'Objektname'.
  PERFORM spalte USING lo_cols 'SUBC'       'Prg.Art'    'Programmart'          'Programmart (TRDIR-SUBC)'.
  PERFORM spalte USING lo_cols 'CHG_DATE'   'Geaendert'  'Letzte Aenderung'     'Letzte Aenderung'.
  PERFORM spalte USING lo_cols 'CHG_SRC'    'Quelle Aen' 'Quelle Aenderung'     'Quelle letzte Aenderung'.
  PERFORM spalte USING lo_cols 'TCODES'     'TCodes'     'Transaktionen'        'Transaktionen (TSTC)'.
  PERFORM spalte USING lo_cols 'ROLES'      'Rollen'     'Rollen mit TCode'     'Rollen mit TCode (AGR_TCODES)'.
  PERFORM spalte USING lo_cols 'JOBS_PLAN'  'Jobs eing.' 'Jobs eingeplant'      'Jobs eingeplant (TBTCO S/P/Z/Y/R)'.
  PERFORM spalte USING lo_cols 'JOB_LAST'   'Joblauf'    'Letzter Joblauf'      'Letzter Joblauf (Ende, F/A)'.
  PERFORM spalte USING lo_cols 'USE_Z'      'Verw. Z'    'Verwender Z/Y'        'Verwender im Kundennamensraum'.
  PERFORM spalte USING lo_cols 'USE_OTHER'  'Verw.fremd' 'Verwender fremd'      'Verwender ausserhalb Z/Y'.
  PERFORM spalte USING lo_cols 'ST03_CNT'   'ST03N Anz.' 'ST03N Ausfuehrungen'  'ST03N Ausfuehrungen im Zeitraum'.
  PERFORM spalte USING lo_cols 'ST03_MONTH' 'ST03N Mon.' 'ST03N letzter Monat'  'Letzter Monat mit Nutzung (ST03N)'.
  PERFORM spalte USING lo_cols 'SCMON_CNT'  'SCMON Anz.' 'SCMON Aufrufe'        'SCMON Aufrufe im Zeitraum'.
  PERFORM spalte USING lo_cols 'SCMON_LAST' 'SCMON bis'  'SCMON letzte Periode' 'SCMON Ende letzte Periode'.
  PERFORM spalte USING lo_cols 'SUSG_CNT'   'SUSG Anz.'  'SUSG Aufrufe'         'SUSG Aufrufe im Zeitraum'.
  PERFORM spalte USING lo_cols 'SUSG_LAST'  'SUSG zul.'  'SUSG zuletzt'         'SUSG zuletzt benutzt'.
  PERFORM spalte USING lo_cols 'KLASSE'     'Einstufung' 'Einstufung'           'Einstufung'.
  PERFORM spalte USING lo_cols 'GRUND'      'Grund'      'Grund'                'Grund der Einstufung'.
  PERFORM spalte USING lo_cols 'ZEITRAUM'   'Zeitraum'   'Datenquellen'         'Zeitraum und Abdeckung der Quellen'.

  lv_header = |Z-Objekte: { lines( gt_out ) } Zeilen, { gv_zeitraum }|.
  lo_alv->get_display_settings( )->set_list_header( lv_header ).
  lo_alv->get_display_settings( )->set_striped_pattern( abap_true ).
  lo_alv->display( ).
ENDFORM.

FORM spalte USING io_cols TYPE REF TO cl_salv_columns_table
                  iv_col  TYPE csequence
                  iv_s    TYPE csequence
                  iv_m    TYPE csequence
                  iv_l    TYPE csequence.
  DATA: lv_col TYPE lvc_fname,
        lv_s   TYPE scrtext_s,
        lv_m   TYPE scrtext_m,
        lv_l   TYPE scrtext_l,
        lo_col TYPE REF TO cl_salv_column.
  lv_col = iv_col.
  lv_s   = iv_s.
  lv_m   = iv_m.
  lv_l   = iv_l.
  TRY.
      lo_col = io_cols->get_column( lv_col ).
      lo_col->set_short_text( lv_s ).
      lo_col->set_medium_text( lv_m ).
      lo_col->set_long_text( lv_l ).
    CATCH cx_salv_not_found.
  ENDTRY.
ENDFORM.
